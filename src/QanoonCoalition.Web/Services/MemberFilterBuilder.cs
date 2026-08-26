using System.Linq.Expressions;
using QanoonCoalition.Web.Models;

// System.Reflection غير مستورد كليّاً لأن MemberFilter فيه يتعارض مع الموديل
using MethodInfo = System.Reflection.MethodInfo;

namespace QanoonCoalition.Web.Services;

/// <summary>
/// يحوّل شروط الفلترة القادمة من الواجهة إلى تعبير واحد قابل للترجمة إلى SQL.
/// التعبيرات مبنية يدوياً (لا Invoke) لأن EF Core يترجم الوصول المباشر للخصائص فقط.
/// </summary>
public static class MemberFilterBuilder
{
    private static readonly MethodInfo Contains   = typeof(string).GetMethod(nameof(string.Contains),   new[] { typeof(string) })!;
    private static readonly MethodInfo StartsWith = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
    private static readonly MethodInfo EndsWith   = typeof(string).GetMethod(nameof(string.EndsWith),   new[] { typeof(string) })!;

    public static Expression<Func<Member, bool>>? Build(IEnumerable<MemberFilter>? filters, FilterMatch match)
    {
        if (filters == null) return null;

        Expression<Func<Member, bool>>? combined = null;

        foreach (var filter in filters)
        {
            if (filter == null || filter.IsEmpty) continue;

            var field = MemberFilterFields.Find(filter.Field);
            if (field == null) continue;

            var predicate = field.Kind switch
            {
                MemberFilterKind.Gender => BuildGender(field.Property, filter.Op, filter.Val),
                MemberFilterKind.Date   => BuildDate(field.Property, filter.Op, filter.Val),
                _                       => BuildText(field.Property, filter.Op, filter.Val)
            };
            if (predicate == null) continue;

            combined = combined == null
                ? predicate
                : (match == FilterMatch.Any ? Or(combined, predicate) : And(combined, predicate));
        }

        return combined;
    }

    private static Expression<Func<Member, bool>>? BuildText(string property, string? op, string? val)
    {
        var p = Expression.Parameter(typeof(Member), "m");
        Expression member = Expression.Property(p, property);
        var nullConst  = Expression.Constant(null, typeof(string));
        var emptyConst = Expression.Constant(string.Empty, typeof(string));

        Expression body;
        switch (op)
        {
            case "empty":
                body = Expression.OrElse(Expression.Equal(member, nullConst), Expression.Equal(member, emptyConst));
                break;

            case "notempty":
                body = Expression.AndAlso(Expression.NotEqual(member, nullConst), Expression.NotEqual(member, emptyConst));
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
                    break;
                }

                var method = op switch
                {
                    "startswith" => StartsWith,
                    "endswith"   => EndsWith,
                    _            => Contains
                };

                // الحقول القابلة لأن تكون NULL تحتاج حماية قبل استدعاء دالة النص
                body = Expression.AndAlso(
                    Expression.NotEqual(member, nullConst),
                    Expression.Call(member, method, value));

                if (op == "notcontains") body = Expression.Not(body);
                break;
        }

        return Expression.Lambda<Func<Member, bool>>(body, p);
    }

    private static Expression<Func<Member, bool>>? BuildGender(string property, string? op, string? val)
    {
        var p = Expression.Parameter(typeof(Member), "m");
        Expression member = Expression.Property(p, property);          // Gender?
        var nullConst = Expression.Constant(null, typeof(Gender?));

        Expression body;
        switch (op)
        {
            case "empty":
                body = Expression.Equal(member, nullConst);
                break;
            case "notempty":
                body = Expression.NotEqual(member, nullConst);
                break;
            default:
                if (!int.TryParse(val, out var raw) || !Enum.IsDefined(typeof(Gender), raw)) return null;
                body = Expression.Equal(member, Expression.Constant((Gender?)(Gender)raw, typeof(Gender?)));
                break;
        }

        return Expression.Lambda<Func<Member, bool>>(body, p);
    }

    private static Expression<Func<Member, bool>>? BuildDate(string property, string? op, string? val)
    {
        var p = Expression.Parameter(typeof(Member), "m");
        Expression member = Expression.Property(p, property);
        var type = member.Type;
        var isNullable = type == typeof(DateOnly?) || type == typeof(DateTime?);
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (op is "empty" or "notempty")
        {
            if (!isNullable) return null;
            var nullConst = Expression.Constant(null, type);
            Expression nullBody = op == "empty"
                ? Expression.Equal(member, nullConst)
                : Expression.NotEqual(member, nullConst);
            return Expression.Lambda<Func<Member, bool>>(nullBody, p);
        }

        if (!DateOnly.TryParse(val, out var day)) return null;

        // CreatedAt مخزّن UTC كـ datetime2، بينما تواريخ الميلاد والخدمة من نوع date
        Expression lower, upper;
        if (underlying == typeof(DateTime))
        {
            lower = Expression.Constant(day.ToDateTime(TimeOnly.MinValue), typeof(DateTime));
            upper = Expression.Constant(day.AddDays(1).ToDateTime(TimeOnly.MinValue), typeof(DateTime));
        }
        else
        {
            lower = Expression.Constant(day, typeof(DateOnly));
            upper = Expression.Constant(day.AddDays(1), typeof(DateOnly));
        }

        // المقارنة تحتاج القيمة الفعلية عندما يكون الحقل Nullable
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

        return Expression.Lambda<Func<Member, bool>>(body, p);
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
        var p = Expression.Parameter(typeof(T), "m");
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
