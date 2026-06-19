using System.IO.Compression;
using System.Text;

namespace PineAI.Shared.Tools;

/// <summary>
/// Builds a minimal but valid .xlsx file from a list of phone numbers
/// using only the System.IO.Compression and System.Text APIs that ship with .NET.
/// No third-party package is required.
/// </summary>
public static class XlsxBuilder
{
    public static byte[] BuildPhoneNumbers(IReadOnlyList<string> phoneNumbers)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", ContentTypesXml());
            AddEntry(archive, "_rels/.rels", RootRelsXml());
            AddEntry(archive, "xl/workbook.xml", WorkbookXml());
            AddEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelsXml());
            AddEntry(archive, "xl/worksheets/sheet1.xml", SheetXml(phoneNumbers));
        }
        return ms.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static string ContentTypesXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
        "</Types>";

    private static string RootRelsXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";

    private static string WorkbookXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
        "<sheets><sheet name=\"Phone Numbers\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
        "</workbook>";

    private static string WorkbookRelsXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
        "</Relationships>";

    private static string SheetXml(IReadOnlyList<string> phoneNumbers)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        sb.Append("<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>Phone Number</t></is></c></row>");

        for (int i = 0; i < phoneNumbers.Count; i++)
        {
            int row = i + 2;
            sb.Append($"<row r=\"{row}\"><c r=\"A{row}\" t=\"inlineStr\"><is><t>{EscapeXml(phoneNumbers[i])}</t></is></c></row>");
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
             .Replace("\"", "&quot;").Replace("'", "&apos;");
}
