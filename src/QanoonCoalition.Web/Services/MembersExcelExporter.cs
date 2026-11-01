using System.Reflection;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using QanoonCoalition.Web.Models;
using Member = QanoonCoalition.Web.Models.Member;

namespace QanoonCoalition.Web.Services;

/// <summary>
/// يصدّر الأعضاء إلى قالب "استمارة البيانات الهيكلية" باستخدام OpenXML SDK مباشرةً
/// دون تغيير أي تنسيق أو هيكل في القالب.
/// </summary>
public static class MembersExcelExporter
{
    private const string TemplateResourceName =
        "QanoonCoalition.Web.Resources.members_template.xlsx";

    // ─── خريطة أعمدة القالب (Row 8) ────────────────────────────────────────
    // A = رقم الناخب    B = ت       C = الاسم         D = الجنس
    // E = تاريخ الميلاد  F = العمر(معادلة)  G = الهاتف
    // H = المحافظة      I = القضاء  J = الناحية       K = العنوان التفصيلي
    // L = التحصيل       M = الاختصاص  N = المهنة       O = العنوان الوظيفي
    // P = مكان العمل    Q = تاريخ المباشرة  R = سنوات الخدمة(معادلة)
    // S = المهارات      T = الخبرات  U = الدورات       V = اللغات
    // W = مجال الاستفادة  X = الملاحظات

    public static byte[] Build(IEnumerable<Member> members, string movementName,
        string? movementManager = null, DateTime? movementCreatedAt = null)
    {
        var list = members.OrderBy(m => m.FullName).ToList();

        // تحميل القالب من الـ Assembly
        var assembly = Assembly.GetExecutingAssembly();
        var resourceStream = assembly.GetManifestResourceStream(TemplateResourceName)
            ?? throw new FileNotFoundException(
                "قالب Excel غير موجود. الموارد: " +
                string.Join(", ", assembly.GetManifestResourceNames()));

        var ms = new MemoryStream();
        resourceStream.CopyTo(ms);
        resourceStream.Dispose();

        // فتح الملف بوضع التحرير مع OpenXML SDK
        using var doc = SpreadsheetDocument.Open(ms, isEditable: true);
        var wbPart  = doc.WorkbookPart!;
        var wsPart  = GetFirstWorksheetPart(wbPart);
        var wsData  = wsPart.Worksheet.GetFirstChild<SheetData>()!;
        var sst     = GetOrCreateSharedStringTable(wbPart);

        // ── معلومات الحركة في الصفوف 4-5 ─────────────────────────────────
        SetString(wsData, sst, "E4", movementName);
        SetString(wsData, sst, "I4", movementManager ?? "—");
        if (movementCreatedAt.HasValue)
            SetString(wsData, sst, "E5",
                movementCreatedAt.Value.ToLocalTime().ToString("yyyy/MM/dd"));

        // ── بيانات الأعضاء من الصف 9 ──────────────────────────────────────
        for (int i = 0; i < list.Count; i++)
        {
            var m   = list[i];
            int row = 9 + i;

            SetString(wsData, sst, $"A{row}", m.VoterNumber ?? "");
            SetNumber(wsData, $"B{row}", i + 1);
            SetString(wsData, sst, $"C{row}", m.FullName);
            SetString(wsData, sst, $"D{row}", GenderText(m.Gender));
            if (m.BirthDate.HasValue)
                SetDate(wsData, sst, $"E{row}", m.BirthDate.Value);
            // F = العمر — معادلة تلقائية، لا تُلمس
            SetString(wsData, sst, $"G{row}", m.Phone ?? "");
            SetString(wsData, sst, $"H{row}", m.Province ?? "");
            SetString(wsData, sst, $"I{row}", m.District ?? "");
            SetString(wsData, sst, $"J{row}", m.SubDistrict ?? "");
            SetString(wsData, sst, $"K{row}", m.Address ?? "");
            SetString(wsData, sst, $"L{row}", m.EducationLevel ?? "");
            SetString(wsData, sst, $"M{row}", m.Specialization ?? "");
            SetString(wsData, sst, $"N{row}", m.Occupation ?? "");
            SetString(wsData, sst, $"O{row}", m.JobTitle ?? "");
            SetString(wsData, sst, $"P{row}", m.WorkPlace ?? "");
            if (m.ServiceStartDate.HasValue)
                SetDate(wsData, sst, $"Q{row}", m.ServiceStartDate.Value);
            // R = سنوات الخدمة — معادلة تلقائية، لا تُلمس
            SetString(wsData, sst, $"S{row}", m.Skills ?? "");
            SetString(wsData, sst, $"T{row}", m.Experiences ?? "");
            SetString(wsData, sst, $"U{row}", m.TrainingCourses ?? "");
            SetString(wsData, sst, $"V{row}", m.Languages ?? "");
            SetString(wsData, sst, $"W{row}", m.BenefitField ?? "");
            SetString(wsData, sst, $"X{row}", m.Notes ?? "");
        }

        sst.Save();
        wsPart.Worksheet.Save();
        doc.Dispose();

        ms.Position = 0;
        return ms.ToArray();
    }

    // ─── مساعدات OpenXML ─────────────────────────────────────────────────────

    private static WorksheetPart GetFirstWorksheetPart(WorkbookPart wbPart)
    {
        var firstSheet = wbPart.Workbook.Sheets!.Elements<Sheet>().First();
        return (WorksheetPart)wbPart.GetPartById(firstSheet.Id!.Value!);
    }

    private static SharedStringTable GetOrCreateSharedStringTable(WorkbookPart wbPart)
    {
        SharedStringTablePart sstPart;
        if (wbPart.GetPartsOfType<SharedStringTablePart>().Any())
            sstPart = wbPart.GetPartsOfType<SharedStringTablePart>().First();
        else
            sstPart = wbPart.AddNewPart<SharedStringTablePart>();

        // ضمان تهيئة الجدول (قد يكون null عند الفتح)
        sstPart.SharedStringTable ??= new SharedStringTable();
        return sstPart.SharedStringTable;
    }

    private static int UpsertSharedString(SharedStringTable sst, string text)
    {
        var items = sst.Elements<SharedStringItem>().ToList();
        for (int i = 0; i < items.Count; i++)
            if (items[i].InnerText == text) return i;
        sst.AppendChild(new SharedStringItem(new Text(text) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }));
        return items.Count;
    }

    private static Cell? FindCell(SheetData sheetData, string cellRef)
    {
        uint rowIndex = GetRowIndex(cellRef);
        var row = sheetData.Elements<Row>()
            .FirstOrDefault(r => r.RowIndex?.Value == rowIndex);
        return row?.Elements<Cell>()
            .FirstOrDefault(c => c.CellReference?.Value == cellRef);
    }

    private static Cell EnsureCell(SheetData sheetData, string cellRef)
    {
        uint rowIndex = GetRowIndex(cellRef);
        string colName = GetColumnName(cellRef);

        // البحث عن الصف أو إنشاؤه
        var row = sheetData.Elements<Row>()
            .FirstOrDefault(r => r.RowIndex?.Value == rowIndex);
        if (row == null)
        {
            row = new Row { RowIndex = rowIndex };
            // إدراج بترتيب صحيح
            var nextRow = sheetData.Elements<Row>()
                .FirstOrDefault(r => r.RowIndex?.Value > rowIndex);
            if (nextRow != null)
                sheetData.InsertBefore(row, nextRow);
            else
                sheetData.AppendChild(row);
        }

        // البحث عن الخلية أو إنشاؤها
        var cell = row.Elements<Cell>()
            .FirstOrDefault(c => c.CellReference?.Value == cellRef);
        if (cell == null)
        {
            cell = new Cell { CellReference = cellRef };
            var nextCell = row.Elements<Cell>()
                .FirstOrDefault(c => CompareColumnNames(GetColumnName(c.CellReference!.Value!), colName) > 0);
            if (nextCell != null)
                row.InsertBefore(cell, nextCell);
            else
                row.AppendChild(cell);
        }
        return cell;
    }

    private static void SetString(SheetData sheetData, SharedStringTable sst,
        string cellRef, string value)
    {
        var cell = EnsureCell(sheetData, cellRef);
        // مسح أي معادلة موجودة
        cell.CellFormula = null;
        if (string.IsNullOrEmpty(value))
        {
            cell.CellValue = null;
            cell.DataType  = null;
        }
        else
        {
            int idx = UpsertSharedString(sst, value);
            cell.CellValue = new CellValue(idx.ToString());
            cell.DataType  = CellValues.SharedString;
        }
    }

    private static void SetNumber(SheetData sheetData, string cellRef, double value)
    {
        var cell = EnsureCell(sheetData, cellRef);
        cell.CellFormula = null;
        cell.CellValue   = new CellValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        cell.DataType    = null; // رقم عادي
    }

    private static void SetDate(SheetData sheetData, SharedStringTable sst,
        string cellRef, DateOnly date)
    {
        var cell = EnsureCell(sheetData, cellRef);
        cell.CellFormula = null;
        var dt = date.ToDateTime(TimeOnly.MinValue);
        if (dt >= new DateTime(1900, 1, 1))
        {
            // Excel OLE Automation date value
            double oadate = dt.ToOADate();
            cell.CellValue = new CellValue(oadate.ToString(System.Globalization.CultureInfo.InvariantCulture));
            cell.DataType  = null; // رقم بتنسيق تاريخ (يحتفظ بالتنسيق الموجود في القالب)
        }
        else
        {
            // تاريخ قبل 1900 → نص
            SetString(sheetData, sst, cellRef, date.ToString("yyyy/MM/dd"));
        }
    }

    // ─── مساعدات عامة ────────────────────────────────────────────────────────
    private static uint GetRowIndex(string cellRef) =>
        uint.Parse(new string(cellRef.Where(char.IsDigit).ToArray()));

    private static string GetColumnName(string cellRef) =>
        new string(cellRef.Where(char.IsLetter).ToArray());

    private static int CompareColumnNames(string a, string b)
    {
        if (a.Length != b.Length) return a.Length.CompareTo(b.Length);
        return string.Compare(a, b, StringComparison.Ordinal);
    }

    private static string GenderText(Gender? gender) => gender switch
    {
        Models.Gender.Male   => "ذكر",
        Models.Gender.Female => "أنثى",
        _                    => ""
    };
}
