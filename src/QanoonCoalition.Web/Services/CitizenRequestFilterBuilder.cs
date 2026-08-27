using System.Linq.Expressions;
using QanoonCoalition.Web.Models;

using MethodInfo = System.Reflection.MethodInfo;

namespace QanoonCoalition.Web.Services;

/// <summary>
/// يحوّل شروط الفلترة القادمة من الواجهة إلى تعبير واحد قابل للترجمة إلى SQL.
/// يدعم المسارات المركّبة (Status.Name) ومجموعة المرفقات (نوع الوثيقة).
/// </summary>
public static class CitizenRequestFilterBuilder
{
    private static readonly MethodInfo Contains   = typeof(string).GetMethod(nameof(string.Contains),   new[] { typeof(string) })!;
    private static readonly MethodInfo StartsWith = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
    private static readonly MethodInfo EndsWith   = typeof(string).GetMethod(nameof(string.EndsWith),   new[] { typeof(string) })!;

    public static Expression<Func<CitizenRequest, bool>>? Build(
        IEnumerable<MemberFilter>? filters, FilterMatch match)
    {
        if (filters == null) return null;

        Expression<Func<CitizenRequest, bool>>? combined = null;

        foreach (var filter in filters)
        {
            if (filter == null || filter.IsEmpty) continue;

            var field = CitizenRequestFilterFields.Find(filter.Field);
            if (field == null) continue;

            var predicate = field.Key == CitizenRequestFilterFields.DocTypeKey
                ? BuildDocType(filter.Op, filter.Val)
                : field.Kind == MemberFilterKind.Date
                    ? BuildDate(field.Property, filter.Op, filter.Val)
                    : BuildText(field.Property, filter.Op, filter.Val);

            if (predicate == null) continue;

            combined = combined == null
                ? predicate
                : (match == FilterMatch.Any ? Or(combined, predicate) : And(combined, predicate));
        }

        return combined;
    }

    /// <summary>
    /// يبني الوصول لخاصية عبر مسار منقّط، ويجمع فحوص عدم الـ null لكل حلقة تنقّل.
    /// مثال: "Destination.Name" ينتج (r.Destination != null) كشرط حماية.
    /// </summary>
    private static Expression ResolvePath(
        Expression root, string path, out Expression? navGuard)
    {
        navGuard = null;
        Expression current = root;
        var parts = path.Split('.');

        for (var i = 0; i < parts.Length; i++)
        {
            // الأجزاء الوسيطة هي خصائص تنقّل قد تكون null
            if (i > 0)
            {
                var check = Expression.NotEqual(current, Expression.Constant(null, current.Type));
                navGuard = navGuard == null ? check : Expression.AndAlso(navGuard, check);
            }
            current = Expression.Property(current, parts[i]);
        }

        return current;
    }

    private static Expression<Func<CitizenRequest, bool>>? BuildText(string path, string? op, string? val)
    {
        var p = Expression.Parameter(typeof(CitizenRequest), "r");
        var member = ResolvePath(p, path, out var navGuard);

        var nullConst  = Expression.Constant(null, typeof(string));
        var emptyConst = Expression.Constant(string.Empty, typeof(string));

        Expression body;
        switch (op)
        {
            case "empty":
                body = Expression.OrElse(
                    Expression.Equal(member, nullConst),
                    Expression.Equal(member, emptyConst));
                // مسار تنقّل مفقود يُعتبر فارغاً أيضاً
                if (navGuard != null)
                    body = Expression.OrElse(Expression.Not(navGuard), body);
                return Expression.Lambda<Func<CitizenRequest, bool>>(body, p);

            case "notempty":
                body = Expression.AndAlso(
                    Expression.NotEqual(member, nullConst),
                    Expression.NotEqual(member, emptyConst));
                break;

            default:
                if (string.IsNullOrWhiteSpace(val)) return null;
                var value = Expression.Constant(val.Trim(), typeof(string));

                if (op == "eq")
                {
                    body = Expression.Equal(member, value);
                    break;
                }

                if (op == "neq")
                {
                    // الصفوف الفارغة تُعتبر "لا تساوي" أيضاً، وإلا استبعدها SQL بسبب NULL
                    body = Expression.OrElse(
                        Expression.Equal(member, nullConst),
                        Expression.NotEqual(member, value));
                    if (navGuard != null)
                        return Expression.Lambda<Func<CitizenRequest, bool>>(
                            Expression.OrElse(Expression.Not(navGuard), body), p);
                    return Expression.Lambda<Func<CitizenRequest, bool>>(body, p);
                }

                var method = op switch
                {
                    "startswith" => StartsWith,
                    "endswith"   => EndsWith,
                    _            => Contains
                };

                body = Expression.AndAlso(
                    Expression.NotEqual(member, nullConst),
                    Expression.Call(member, method, value));

                if (op == "notcontains")
                {
                    body = Expression.Not(body);
                    if (navGuard != null)
                        return Expression.Lambda<Func<CitizenRequest, bool>>(
                            Expression.OrElse(Expression.Not(navGuard), body), p);
                    return Expression.Lambda<Func<CitizenRequest, bool>>(body, p);
                }
                break;
        }

        if (navGuard != null) body = Expression.AndAlso(navGuard, body);
        return Expression.Lambda<Func<CitizenRequest, bool>>(body, p);
    }

    private static Expression<Func<CitizenRequest, bool>>? BuildDate(string path, string? op, string? val)
    {
        var p = Expression.Parameter(typeof(CitizenRequest), "r");
        var member = ResolvePath(p, path, out _);

        var type = member.Type;
        var isNullable = Nullable.GetUnderlyingType(type) != null;

        if (op is "empty" or "notempty")
        {
            if (!isNullable) return null;
            var nullConst = Expression.Constant(null, type);
            Expression nullBody = op == "empty"
                ? Expression.Equal(member, nullConst)
                : Expression.NotEqual(member, nullConst);
            return Expression.Lambda<Func<CitizenRequest, bool>>(nullBody, p);
        }

        if (!DateOnly.TryParse(val, out var day)) return null;

        var lower = Expression.Constant(day.ToDateTime(TimeOnly.MinValue), typeof(DateTime));
        var upper = Expression.Constant(day.AddDays(1).ToDateTime(TimeOnly.MinValue), typeof(DateTime));

        Expression value = isNullable ? Expression.Property(member, "Value") : member;
        Expression body = op switch
        {
            "before" => Expression.LessThan(value, lower),
            "after"  => Expression.GreaterThanOrEqual(value, upper),
            _        => Expression.AndAlso(
                            Expression.GreaterThanOrEqual(value, lower),
                            Expression.LessThan(value, upper))
        };

        if (isNullable)
            body = Expression.AndAlso(Expression.Property(member, "HasValue"), body);

        return Expression.Lambda<Func<CitizenRequest, bool>>(body, p);
    }

    /// <summary>
    /// نوع الوثيقة يمر عبر مجموعة المرفقات، فيُبنى كـ Attachments.Any(...)
    /// بدلاً من الوصول المباشر لخاصية.
    /// </summary>
    private static Expression<Func<CitizenRequest, bool>>? BuildDocType(string? op, string? val)
    {
        switch (op)
        {
            case "empty":
                return r => !r.Attachments.Any(a => a.DocumentTypeId != null);
            case "notempty":
                return r => r.Attachments.Any(a => a.DocumentTypeId != null);
        }

        if (string.IsNullOrWhiteSpace(val)) return null;
        var name = val.Trim();

        return op switch
        {
            "neq"      => r => !r.Attachments.Any(a => a.DocumentType != null && a.DocumentType.Name == name),
            "contains" => r => r.Attachments.Any(a => a.DocumentType != null && a.DocumentType.Name.Contains(name)),
            _          => r => r.Attachments.Any(a => a.DocumentType != null && a.DocumentType.Name == name)
        };
    }

    private static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b) =>
        Merge(a, b, Expression.AndAlso);

    private static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b) =>
        Merge(a, b, Expression.OrElse);

    private static Expression<Func<T, bool>> Merge<T>(
        Expression<Func<T, bool>> a,
        Expression<Func<T, bool>> b,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        var p = Expression.Parameter(typeof(T), "r");
        var left  = new ParameterRebinder(a.Parameters[0], p).Visit(a.Body)!;
        var right = new ParameterRebinder(b.Parameters[0], p).Visit(b.Body)!;
        return Expression.Lambda<Func<T, bool>>(merge(left, right), p);
    }

    private sealed class ParameterRebinder : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterRebinder(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _from ? _to : base.VisitParameter(node);
    }
}
