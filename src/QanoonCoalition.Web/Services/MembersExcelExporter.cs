using ClosedXML.Excel;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

/// <summary>
/// يصدّر الأعضاء إلى ملف Excel مطابق لقالب "استمارة البيانات الهيكلية لأعضاء الحركة".
/// </summary>
public static class MembersExcelExporter
{
    // ─── ألوان القالب ────────────────────────────────────────────────────────
    private const string GreenDark   = "#14532D";  // العنوان ومجموعات الأعمدة
    private const string GreenMid    = "#2E7D32";  // التسميات + الأعمدة الفردية في الرؤوس
    private const string GreenBright = "#43A048";  // الأعمدة الزوجية في الرؤوس
    private const string GreenLight  = "#DCF2DD";  // الملاحظات + الخلايا المحسوبة تلقائياً
    private const string GreenTint   = "#F3FBF4";  // تظليل الصفوف المتناوبة
    private const string TextGreen   = "#1F4E20";  // نص قيم معلومات الحركة
    private const string BorderSoft  = "#C3DDC4";  // حدود خلايا البيانات
    private const string BorderMid   = "#9CC59D";  // حدود صف الرؤوس

    private const int LastCol = 23;

    /// <summary>الأعمدة المحسوبة تلقائياً (ت، العمر، سنوات الخدمة) — مظللة بالأخضر الفاتح</summary>
    private static readonly int[] AutoCalcColumns = [1, 5, 17];

    private static readonly string[] Headers =
    [
        "ت",
        "الاسم الرباعي واللقب",
        "الجنس",
        "تاريخ الميلاد",
        "العمر",
        "رقم الهاتف",
        "المحافظة",
        "القضاء",
        "الناحية",
        "العنوان التفصيلي",
        "التحصيل الدراسي",
        "الاختصاص",
        "المهنة",
        "العنوان الوظيفي",
        "مكان العمل",
        "تاريخ المباشرة بالوظيفة",
        "سنوات الخدمة",
        "المهارات",
        "الخبرات",
        "الدورات التدريبية",
        "اللغات",
        "مجال الاستفادة من العضو",
        "الملاحظات"
    ];

    /// <summary>مجموعات الأعمدة في الصف 7 — نطاقات الدمج مطابقة للقالب</summary>
    private static readonly (string Title, int From, int To)[] Groups =
    [
        ("البيانات الشخصية",      1,  2),   // A7:B7
        ("",                      3,  6),   // C7:F7  (امتداد فارغ بنفس اللون)
        ("بيانات السكن",          7,  10),  // G7:J7
        ("البيانات العلمية",      11, 12),  // K7:L7
        ("البيانات المهنية",      13, 15),  // M7:O7
        ("سنوات الخدمة الوظيفية", 16, 17),  // P7:Q7
        ("الطاقات والخبرات",      18, 23)   // R7:W7
    ];

    private static readonly double[] ColumnWidths =
    [
        4.86, 21.29, 7.71, 12.29, 6.71, 14.29, 11.71, 11.71, 11.71, 17.29,
        13.29, 13.29, 12.29, 12.29, 14.29, 12.29, 8.29, 13.29, 13.29, 13.29,
        10.29, 12.29, 13.29
    ];

    public static byte[] Build(IEnumerable<Member> members, string movementName,
        string? movementManager = null, DateTime? movementCreatedAt = null)
    {
        // الأقدم أولاً
        var list = members.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("الاستمارة");
        ws.RightToLeft = true;
        ws.ShowGridLines = false;
        ws.Style.Font.FontName = "Arial";

        BuildTitleBand(ws);
        BuildInfoBand(ws, movementName, movementManager, movementCreatedAt, list.Count);
        BuildGroupRow(ws);
        BuildHeaderRow(ws);
        BuildDataRows(ws, list);
        ApplyLayout(ws, list.Count);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ─── الصفوف 1-2: شريط العنوان ────────────────────────────────────────────
    private static void BuildTitleBand(IXLWorksheet ws)
    {
        // A1:B2 و P1:W2 خانات فارغة بنفس اللون لإكمال الشريط
        foreach (var (from, to) in new[] { (1, 2), (16, LastCol) })
        {
            var block = ws.Range(1, from, 2, to);
            block.Merge();
            block.Style.Fill.SetBackgroundColor(XLColor.FromHtml(GreenDark));
        }

        SetBandCell(ws, 1, 3, 2, 10, "استمارة البيانات الهيكلية لأعضاء الحركة", 20);
        SetBandCell(ws, 1, 11, 2, 15, "نموذج موحد للاستخدام التنظيمي الرسمي", 13);

        ws.Row(1).Height = 30;
        ws.Row(2).Height = 24;
        ws.Row(3).Height = 6.75;
    }

    private static void SetBandCell(IXLWorksheet ws, int rowFrom, int colFrom, int rowTo, int colTo,
        string text, double fontSize)
    {
        var range = ws.Range(rowFrom, colFrom, rowTo, colTo);
        range.Merge();
        var cell = ws.Cell(rowFrom, colFrom);
        cell.Value = text;
        cell.Style.Font.SetBold().Font.SetFontSize(fontSize).Font.SetFontColor(XLColor.White);
        cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml(GreenDark));
        Center(cell);
    }

    // ─── الصفوف 4-5: معلومات الحركة والملاحظات ───────────────────────────────
    private static void BuildInfoBand(IXLWorksheet ws, string movementName, string? manager,
        DateTime? createdAt, int memberCount)
    {
        // A4:B5 خانة فارغة
        ws.Range(4, 1, 5, 2).Merge();

        InfoPair(ws, 4, "اسم الحركة", movementName, "مسؤول الحركة", manager ?? "—");
        InfoPair(ws, 5, "تاريخ تشكيل الحركة", createdAt?.ToString("yyyy/MM/dd") ?? "—",
                        "عدد الأعضاء", memberCount.ToString());

        Note(ws, 11, 15, "مفتاح التعبئة: الخانات البيضاء تُملأ يدوياً، "
                       + "والخانات ذات التظليل الأخضر الفاتح محسوبة تلقائياً");
        Note(ws, 16, LastCol, "أعمدة (الجنس، المحافظة، التحصيل الدراسي، مجال الاستفادة) "
                            + "تحتوي قوائم منسدلة، ورؤوس الجدول مثبتة ومزودة بالفرز والتصفية");

        ws.Row(4).Height = 27.75;
        ws.Row(5).Height = 27.75;
        ws.Row(6).Height = 6.75;
    }

    /// <summary>يكتب زوجين من (تسمية، قيمة) في الأعمدة C-D، E-F، G-H، I-J</summary>
    private static void InfoPair(IXLWorksheet ws, int row, string label1, string value1,
        string label2, string value2)
    {
        WriteLabel(ws, row, 3, label1);
        WriteValue(ws, row, 5, value1);
        WriteLabel(ws, row, 7, label2);
        WriteValue(ws, row, 9, value2);
    }

    private static void WriteLabel(IXLWorksheet ws, int row, int col, string text)
    {
        ws.Range(row, col, row, col + 1).Merge();
        var cell = ws.Cell(row, col);
        cell.Value = text;
        cell.Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.White);
        cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml(GreenMid));
        Center(cell);
        Outline(cell, BorderMid, XLBorderStyleValues.Thin);
    }

    private static void WriteValue(IXLWorksheet ws, int row, int col, string text)
    {
        ws.Range(row, col, row, col + 1).Merge();
        var cell = ws.Cell(row, col);
        cell.Value = text;
        cell.Style.Font.SetBold().Font.SetFontSize(12)
            .Font.SetFontColor(XLColor.FromHtml(TextGreen));
        cell.Style.Fill.SetBackgroundColor(XLColor.White);
        Center(cell);
        Outline(cell, BorderMid, XLBorderStyleValues.Thin);
    }

    private static void Note(IXLWorksheet ws, int colFrom, int colTo, string text)
    {
        var range = ws.Range(4, colFrom, 5, colTo);
        range.Merge();
        var cell = ws.Cell(4, colFrom);
        cell.Value = text;
        cell.Style.Font.SetBold().Font.SetFontSize(10.5)
            .Font.SetFontColor(XLColor.FromHtml(GreenDark));
        cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml(GreenLight));
        Center(cell);
        cell.Style.Alignment.SetWrapText(true);
        Outline(cell, BorderMid, XLBorderStyleValues.Thin);
    }

    // ─── الصف 7: مجموعات الأعمدة ─────────────────────────────────────────────
    private static void BuildGroupRow(IXLWorksheet ws)
    {
        foreach (var (title, from, to) in Groups)
        {
            var range = ws.Range(7, from, 7, to);
            range.Merge();
            var cell = ws.Cell(7, from);
            if (title.Length > 0)
            {
                cell.Value = title;
                cell.Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.White);
            }
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml(GreenDark));
            Center(cell);
        }
        ws.Row(7).Height = 25.5;
    }

    // ─── الصف 8: رؤوس الأعمدة ────────────────────────────────────────────────
    private static void BuildHeaderRow(IXLWorksheet ws)
    {
        for (var i = 0; i < Headers.Length; i++)
        {
            var col = i + 1;
            var cell = ws.Cell(8, col);
            cell.Value = Headers[i];
            cell.Style.Font.SetBold().Font.SetFontSize(11).Font.SetFontColor(XLColor.White);
            // تبديل اللون بين الأعمدة الفردية والزوجية كما في القالب
            cell.Style.Fill.SetBackgroundColor(
                XLColor.FromHtml(col % 2 == 1 ? GreenMid : GreenBright));
            Center(cell);
            cell.Style.Alignment.SetWrapText(true);
            Outline(cell, BorderMid, XLBorderStyleValues.Thin);
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml(GreenDark);
        }
        ws.Row(8).Height = 42;
    }

    // ─── الصفوف 9+: بيانات الأعضاء ───────────────────────────────────────────
    private static void BuildDataRows(IXLWorksheet ws, List<Member> list)
    {
        var row = 9;
        foreach (var m in list)
        {
            var c = 1;
            ws.Cell(row, c++).Value = row - 8;
            ws.Cell(row, c++).Value = m.FullName;
            ws.Cell(row, c++).Value = GenderText(m.Gender);
            SetDate(ws.Cell(row, c++), m.BirthDate);
            SetNumber(ws.Cell(row, c++), CalculateAge(m.BirthDate));
            ws.Cell(row, c++).Value = m.Phone;
            ws.Cell(row, c++).Value = m.Province ?? "";
            ws.Cell(row, c++).Value = m.District ?? "";
            ws.Cell(row, c++).Value = m.SubDistrict ?? "";
            ws.Cell(row, c++).Value = m.Address ?? "";
            ws.Cell(row, c++).Value = m.EducationLevel ?? "";
            ws.Cell(row, c++).Value = m.Specialization ?? "";
            ws.Cell(row, c++).Value = m.Occupation ?? "";
            ws.Cell(row, c++).Value = m.JobTitle ?? "";
            ws.Cell(row, c++).Value = m.WorkPlace ?? "";
            SetDate(ws.Cell(row, c++), m.ServiceStartDate);
            SetNumber(ws.Cell(row, c++), m.ServiceYears);
            ws.Cell(row, c++).Value = m.Skills ?? "";
            ws.Cell(row, c++).Value = m.Experiences ?? "";
            ws.Cell(row, c++).Value = m.TrainingCourses ?? "";
            ws.Cell(row, c++).Value = m.Languages ?? "";
            ws.Cell(row, c++).Value = m.BenefitField ?? "";
            ws.Cell(row, c).Value = m.Notes ?? "";

            StyleDataRow(ws, row, isAlternate: (row - 9) % 2 == 1);
            ws.Row(row).Height = 21.75;
            row++;
        }
    }

    private static void StyleDataRow(IXLWorksheet ws, int row, bool isAlternate)
    {
        var range = ws.Range(row, 1, row, LastCol);
        range.Style.Font.SetFontSize(11);
        range.Style.Fill.SetBackgroundColor(
            XLColor.FromHtml(isAlternate ? GreenTint : "#FFFFFF"));
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.FromHtml(BorderSoft);
        range.Style.Border.InsideBorderColor = XLColor.FromHtml(BorderSoft);
        range.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

        // الأعمدة المحسوبة تلقائياً: تظليل أخضر فاتح + خط عريض
        foreach (var col in AutoCalcColumns)
        {
            var cell = ws.Cell(row, col);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml(GreenLight));
            cell.Style.Font.SetBold().Font.SetFontColor(XLColor.FromHtml(GreenDark));
        }

        // توسيط الأعمدة القصيرة
        foreach (var col in new[] { 1, 3, 4, 5, 6, 16, 17 })
            ws.Cell(row, col).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    // ─── التنسيق النهائي ─────────────────────────────────────────────────────
    private static void ApplyLayout(IXLWorksheet ws, int memberCount)
    {
        for (var i = 0; i < ColumnWidths.Length; i++)
            ws.Column(i + 1).Width = ColumnWidths[i];

        if (memberCount > 0)
            ws.Range(8, 1, 8 + memberCount, LastCol).SetAutoFilter();

        ws.SheetView.FreezeRows(8);
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.SetRowsToRepeatAtTop(7, 8);
    }

    // ─── مساعدات ─────────────────────────────────────────────────────────────
    private static void Center(IXLCell cell) =>
        cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

    private static void Outline(IXLCell cell, string colorHex, XLBorderStyleValues style)
    {
        cell.Style.Border.OutsideBorder = style;
        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml(colorHex);
    }

    private static void SetDate(IXLCell cell, DateOnly? date)
    {
        if (date.HasValue)
        {
            cell.Value = date.Value.ToDateTime(TimeOnly.MinValue);
            cell.Style.DateFormat.Format = "yyyy/mm/dd";
        }
    }

    private static void SetNumber(IXLCell cell, int? number)
    {
        if (number.HasValue) cell.Value = number.Value;
    }

    private static int? CalculateAge(DateOnly? birthDate)
    {
        if (!birthDate.HasValue) return null;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value > today.AddYears(-age)) age--;
        return age < 0 ? null : age;
    }

    private static string GenderText(Gender? gender) => gender switch
    {
        Models.Gender.Male   => "ذكر",
        Models.Gender.Female => "أنثى",
        _                    => ""
    };
}
