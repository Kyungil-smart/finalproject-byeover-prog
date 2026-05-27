// 담당자 : 정승우
// 설명   : Excel → JSON 변환 에디터 도구 (외부 DLL 불필요)
// 수정자 : Codex
// 수정내용 : JSON 출력 인코딩을 UTF-8 BOM 없음으로 고정

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// xlsx 파일의 데이터 시트를 JSON으로 변환한다.
/// EPPlus 등 외부 DLL 없이 System.IO.Compression + System.Xml만 사용한다.
/// 행 1 = 영문 키, 행 2 = 한글 설명(무시), 행 3 = 타입, 행 4~ = 데이터.
/// #으로 시작하는 컬럼은 인간용이므로 자동 제외한다.
/// </summary>
public static class ExcelToJsonConverter
{
    // ---------- 경로 설정 ----------

    private const string EXCEL_FOLDER = "Assets/_Project/DataSource/";
    private const string JSON_OUTPUT  = "Assets/Resources/Data/Tables/";

    // ---------- 지원 타입 ----------

    private static readonly HashSet<string> ValidTypes = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "int", "float", "string", "enum", "bool", "int[]"
    };

    // ---------- 메뉴 ----------

    [MenuItem("Tools/Data/Convert Excel to JSON")]
    public static void ConvertAll()
    {
        EnsureDirectory(JSON_OUTPUT);

        if (!Directory.Exists(EXCEL_FOLDER))
        {
            Debug.LogError($"[DataConverter] 폴더가 없습니다 -> {EXCEL_FOLDER}");
            return;
        }

        string[] excelFiles = Directory.GetFiles(EXCEL_FOLDER, "*.xlsx");
        if (excelFiles.Length == 0)
        {
            Debug.LogWarning($"[DataConverter] {EXCEL_FOLDER}에 xlsx 파일이 없습니다.");
            return;
        }

        int totalSheets = 0;
        int totalSkipped = 0;
        int failCount = 0;

        foreach (string filePath in excelFiles)
        {
            if (Path.GetFileName(filePath).StartsWith("~$"))
                continue;

            try
            {
                var result = ConvertFile(filePath);
                totalSheets += result.converted;
                totalSkipped += result.skipped;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[DataConverter] {Path.GetFileName(filePath)} 변환 실패: {e.Message}\n{e.StackTrace}");
                failCount++;
            }
        }

        int validationErrors = DataValidator.ValidateAll(JSON_OUTPUT);

        if (validationErrors == 0)
        {
            Debug.Log($"[DataConverter] 완료: {totalSheets}개 시트 변환, " +
                      $"{totalSkipped}개 시트 스킵, {failCount}개 파일 실패. 검증 통과.");
        }
        else
        {
            Debug.LogError($"[DataConverter] 완료: {totalSheets}개 시트 변환, " +
                           $"검증 에러 {validationErrors}개 — 반드시 수정 필요!");
        }

        AssetDatabase.Refresh();
    }

    // ---------- 파일 단위 변환 ----------

    private static (int converted, int skipped) ConvertFile(string excelPath)
    {
        int converted = 0;
        int skipped = 0;

        var workbook = XlsxReader.Read(excelPath);

        foreach (var sheet in workbook)
        {
            if (!IsDataSheet(sheet))
            {
                skipped++;
                continue;
            }

            string json = ConvertSheet(sheet);
            string jsonName = DataTableSchemaRegistry.TryGetBySheetName(sheet.Name, out var schema)
                ? schema.JsonName
                : DataTableSchemaRegistry.ToDefaultJsonName(sheet.Name);
            string fileName = jsonName + ".json";
            string outputPath = Path.Combine(JSON_OUTPUT, fileName);

            File.WriteAllText(outputPath, json, new UTF8Encoding(false));
            Debug.Log($"[DataConverter] {sheet.Name} -> {fileName}");

            if (!DataTableSchemaRegistry.TryGetByJsonName(jsonName, out _))
                Debug.LogWarning($"[DataConverter] 등록되지 않은 데이터 시트입니다: {sheet.Name}. JSON은 생성했지만 SO Import 대상은 아닙니다.");

            converted++;
        }

        return (converted, skipped);
    }

    // ---------- 데이터 시트 판별 ----------

    private static bool IsDataSheet(XlsxSheet sheet)
    {
        if (sheet.RowCount < 4 || sheet.ColCount == 0)
            return false;

        int validCount = 0;
        int totalCount = 0;

        for (int c = 0; c < sheet.ColCount; c++)
        {
            string header = sheet.GetCell(0, c);
            if (string.IsNullOrEmpty(header) || header.StartsWith("#"))
                continue;

            string typeStr = sheet.GetCell(2, c);
            totalCount++;

            if (ValidTypes.Contains(typeStr))
                validCount++;
        }

        return totalCount > 0 && (float)validCount / totalCount >= 0.5f;
    }

    // ---------- 시트 -> JSON 변환 ----------

    private static string ConvertSheet(XlsxSheet sheet)
    {
        var columns = new List<ColumnInfo>();

        for (int c = 0; c < sheet.ColCount; c++)
        {
            string header = sheet.GetCell(0, c);
            if (string.IsNullOrEmpty(header) || header.StartsWith("#"))
                continue;

            string typeStr = sheet.GetCell(2, c).ToLower();
            if (!ValidTypes.Contains(typeStr))
                continue;

            columns.Add(new ColumnInfo { ColIndex = c, Key = header.Trim(), Type = typeStr });
        }

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"data\": [");

        bool firstRow = true;

        for (int r = 3; r < sheet.RowCount; r++)
        {
            if (columns.Count == 0) continue;

            string firstVal = sheet.GetCell(r, columns[0].ColIndex);
            if (string.IsNullOrEmpty(firstVal))
                continue;

            if (!firstRow) sb.AppendLine(",");
            firstRow = false;

            sb.Append("    {");
            bool firstCol = true;

            foreach (var col in columns)
            {
                if (!firstCol) sb.Append(",");
                firstCol = false;

                string raw = sheet.GetCell(r, col.ColIndex);
                sb.Append($"\"{col.Key}\": {FormatValue(raw, col.Type)}");
            }

            sb.Append("}");
        }

        sb.AppendLine();
        sb.AppendLine("  ]");
        sb.Append("}");

        return sb.ToString();
    }

    // ---------- 값 포맷팅 ----------

    private static string FormatValue(string raw, string type)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return type switch
            {
                "int"    => "0",
                "float"  => "0.0",
                "string" => "\"\"",
                "enum"   => "\"\"",
                "bool"   => "false",
                "int[]"  => "[]",
                _        => "null"
            };
        }

        switch (type)
        {
            case "int":
                if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double dInt))
                    return ((int)dInt).ToString();
                return "0";

            case "float":
                if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double dFloat))
                    return dFloat.ToString("G", CultureInfo.InvariantCulture);
                return "0.0";

            case "string":
            case "enum":
                return $"\"{EscapeJson(raw.Trim())}\"";

            case "bool":
                string upper = raw.Trim().ToUpper();
                return (upper == "TRUE" || upper == "1" || upper == "YES") ? "true" : "false";

            case "int[]":
                string[] parts = raw.Split(',');
                var nums = new List<string>();
                foreach (string p in parts)
                {
                    if (double.TryParse(p.Trim(), NumberStyles.Any,
                                        CultureInfo.InvariantCulture, out double n))
                        nums.Add(((int)n).ToString());
                }
                return $"[{string.Join(", ", nums)}]";

            default:
                return $"\"{EscapeJson(raw)}\"";
        }
    }

    // ---------- 유틸리티 ----------

    private static string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "")
                .Replace("\t", "\\t");
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    private struct ColumnInfo
    {
        public int ColIndex;
        public string Key;
        public string Type;
    }
}

// ============================================================
// xlsx 리더 — System.IO.Compression + System.Xml 전용
// xlsx는 내부적으로 XML을 담은 zip 파일이다.
// ============================================================

/// <summary>
/// 하나의 xlsx 시트 데이터를 담는 클래스.
/// </summary>
public class XlsxSheet
{
    public string Name;
    public int RowCount;
    public int ColCount;

    private Dictionary<(int row, int col), string> _cells
        = new Dictionary<(int, int), string>();

    public void SetCell(int row, int col, string value)
    {
        _cells[(row, col)] = value;
        if (row + 1 > RowCount) RowCount = row + 1;
        if (col + 1 > ColCount) ColCount = col + 1;
    }

    public string GetCell(int row, int col)
    {
        return _cells.TryGetValue((row, col), out string val) ? val : "";
    }
}

/// <summary>
/// 외부 라이브러리 없이 xlsx 파일을 읽는 정적 유틸리티.
/// xlsx = zip { xl/sharedStrings.xml, xl/worksheets/sheet1.xml, ... }
/// </summary>
public static class XlsxReader
{
    public static List<XlsxSheet> Read(string filePath)
    {
        var sheets = new List<XlsxSheet>();

        using (var zip = ZipFile.OpenRead(filePath))
        {
            // 1. 공유 문자열 테이블 로드
            var sharedStrings = LoadSharedStrings(zip);

            // 2. workbook.xml에서 시트 이름 목록 추출
            var sheetNames = LoadSheetNames(zip);

            // 3. workbook.xml.rels에서 시트 파일 경로 매핑
            var sheetPaths = LoadSheetPaths(zip);

            // 4. 각 시트 파싱
            foreach (var (sheetId, sheetName) in sheetNames)
            {
                if (!sheetPaths.TryGetValue(sheetId, out string sheetPath))
                    continue;

                var entry = zip.GetEntry(sheetPath);
                if (entry == null)
                {
                    // 경로 보정 시도
                    sheetPath = sheetPath.TrimStart('/');
                    entry = zip.GetEntry(sheetPath);
                    if (entry == null) continue;
                }

                var sheet = ParseSheet(entry, sheetName, sharedStrings);
                sheets.Add(sheet);
            }
        }

        return sheets;
    }

    // ---------- 공유 문자열 ----------

    private static List<string> LoadSharedStrings(ZipArchive zip)
    {
        var strings = new List<string>();

        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return strings;

        using (var stream = entry.Open())
        {
            var doc = new XmlDocument();
            doc.Load(stream);

            var nsMgr = CreateNsManager(doc);
            var nodes = doc.SelectNodes("//ns:si", nsMgr);

            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    // <si> 안에 <t> 하나이거나, <r><t>...</t></r> 여러 개
                    var sb = new StringBuilder();
                    var tNodes = node.SelectNodes(".//ns:t", nsMgr);
                    if (tNodes != null)
                    {
                        foreach (XmlNode t in tNodes)
                            sb.Append(t.InnerText);
                    }
                    strings.Add(sb.ToString());
                }
            }
        }

        return strings;
    }

    // ---------- 시트 이름 목록 ----------

    private static List<(string id, string name)> LoadSheetNames(ZipArchive zip)
    {
        var names = new List<(string, string)>();

        var entry = zip.GetEntry("xl/workbook.xml");
        if (entry == null) return names;

        using (var stream = entry.Open())
        {
            var doc = new XmlDocument();
            doc.Load(stream);

            var nsMgr = CreateNsManager(doc);
            var nodes = doc.SelectNodes("//ns:sheet", nsMgr);

            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    string name = node.Attributes?["name"]?.Value ?? "";
                    string rId = node.Attributes?["r:id"]?.Value ?? "";
                    names.Add((rId, name));
                }
            }
        }

        return names;
    }

    // ---------- 시트 파일 경로 ----------

    private static Dictionary<string, string> LoadSheetPaths(ZipArchive zip)
    {
        var paths = new Dictionary<string, string>();

        var entry = zip.GetEntry("xl/_rels/workbook.xml.rels");
        if (entry == null) return paths;

        using (var stream = entry.Open())
        {
            var doc = new XmlDocument();
            doc.Load(stream);

            var nodes = doc.SelectNodes("//*[local-name()='Relationship']");

            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    string id = node.Attributes?["Id"]?.Value ?? "";
                    string target = node.Attributes?["Target"]?.Value ?? "";

                    if (target.Contains("worksheets/"))
                    {
                        // 상대 경로 -> xl/ 기준 절대 경로
                        if (!target.StartsWith("xl/"))
                            target = "xl/" + target.TrimStart('/');

                        paths[id] = target;
                    }
                }
            }
        }

        return paths;
    }

    // ---------- 시트 파싱 ----------

    private static XlsxSheet ParseSheet(ZipArchiveEntry entry, string sheetName,
                                         List<string> sharedStrings)
    {
        var sheet = new XlsxSheet { Name = sheetName };

        using (var stream = entry.Open())
        {
            var doc = new XmlDocument();
            doc.Load(stream);

            var nsMgr = CreateNsManager(doc);
            var rowNodes = doc.SelectNodes("//ns:row", nsMgr);

            if (rowNodes == null) return sheet;

            foreach (XmlNode rowNode in rowNodes)
            {
                var cellNodes = rowNode.SelectNodes("ns:c", nsMgr);
                if (cellNodes == null) continue;

                foreach (XmlNode cell in cellNodes)
                {
                    string cellRef = cell.Attributes?["r"]?.Value ?? "";
                    string cellType = cell.Attributes?["t"]?.Value ?? "";

                    var (row, col) = ParseCellRef(cellRef);

                    // 셀 값 추출
                    var vNode = cell.SelectSingleNode("ns:v", nsMgr);
                    if (vNode == null)
                    {
                        // 인라인 문자열
                        var isNode = cell.SelectSingleNode("ns:is/ns:t", nsMgr);
                        if (isNode != null)
                            sheet.SetCell(row, col, isNode.InnerText);
                        continue;
                    }

                    string rawValue = vNode.InnerText;

                    if (cellType == "s")
                    {
                        // 공유 문자열 참조
                        if (int.TryParse(rawValue, out int ssIndex) &&
                            ssIndex < sharedStrings.Count)
                        {
                            sheet.SetCell(row, col, sharedStrings[ssIndex]);
                        }
                    }
                    else if (cellType == "b")
                    {
                        sheet.SetCell(row, col, rawValue == "1" ? "TRUE" : "FALSE");
                    }
                    else
                    {
                        // 숫자 또는 날짜
                        sheet.SetCell(row, col, rawValue);
                    }
                }
            }
        }

        return sheet;
    }

    // ---------- 셀 참조 파싱 (A1 -> row=0, col=0) ----------

    private static (int row, int col) ParseCellRef(string cellRef)
    {
        int col = 0;
        int row = 0;
        int i = 0;

        // 알파벳 -> 열 번호
        while (i < cellRef.Length && char.IsLetter(cellRef[i]))
        {
            col = col * 26 + (char.ToUpper(cellRef[i]) - 'A' + 1);
            i++;
        }
        col--;

        // 숫자 -> 행 번호 (0-indexed)
        while (i < cellRef.Length && char.IsDigit(cellRef[i]))
        {
            row = row * 10 + (cellRef[i] - '0');
            i++;
        }
        row--;

        return (row, col);
    }

    // ---------- XML 네임스페이스 ----------

    private static XmlNamespaceManager CreateNsManager(XmlDocument doc)
    {
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        string ns = doc.DocumentElement?.NamespaceURI ?? "";
        if (!string.IsNullOrEmpty(ns))
            nsMgr.AddNamespace("ns", ns);

        // r: 네임스페이스 (workbook.xml에서 사용)
        nsMgr.AddNamespace("r",
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships");

        return nsMgr;
    }
}
#endif
