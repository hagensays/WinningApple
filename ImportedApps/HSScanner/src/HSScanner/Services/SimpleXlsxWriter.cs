using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace HSScanner.Services
{
    internal static class SimpleXlsxWriter
    {
        private const string S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string PackageRels = "http://schemas.openxmlformats.org/package/2006/relationships";

        internal sealed class Sheet
        {
            public string Name { get; set; }
            public string[] Headers { get; set; }
            public int RowCount { get; set; }
            public Func<int, object[]> RowFactory { get; set; }
        }

        internal static Sheet FromRows(string name, string[] headers, IList<object[]> rows)
        {
            return new Sheet { Name = name, Headers = headers, RowCount = rows.Count, RowFactory = i => rows[i] };
        }

        internal static void Write(string outputPath, IList<Sheet> sheets)
        {
            using (var stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
            {
                WriteContentTypes(zip, sheets.Count);
                WriteRootRelationships(zip);
                WriteWorkbook(zip, sheets);
                WriteWorkbookRelationships(zip, sheets.Count);
                for (var i = 0; i < sheets.Count; i++) WriteSheet(zip, i + 1, sheets[i]);
            }
        }

        private static void WriteContentTypes(ZipArchive zip, int sheetCount)
        {
            WriteXml(zip, "[Content_Types].xml", w =>
            {
                w.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
                TypeDefault(w, "rels", "application/vnd.openxmlformats-package.relationships+xml");
                TypeDefault(w, "xml", "application/xml");
                TypeOverride(w, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                for (var i = 1; i <= sheetCount; i++)
                    TypeOverride(w, "/xl/worksheets/sheet" + i + ".xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
                w.WriteEndElement();
            });
        }

        private static void TypeDefault(XmlWriter w, string extension, string contentType)
        {
            w.WriteStartElement("Default", "http://schemas.openxmlformats.org/package/2006/content-types");
            w.WriteAttributeString("Extension", extension);
            w.WriteAttributeString("ContentType", contentType);
            w.WriteEndElement();
        }

        private static void TypeOverride(XmlWriter w, string partName, string contentType)
        {
            w.WriteStartElement("Override", "http://schemas.openxmlformats.org/package/2006/content-types");
            w.WriteAttributeString("PartName", partName);
            w.WriteAttributeString("ContentType", contentType);
            w.WriteEndElement();
        }

        private static void WriteRootRelationships(ZipArchive zip)
        {
            WriteXml(zip, "_rels/.rels", w =>
            {
                w.WriteStartElement("Relationships", PackageRels);
                Relationship(w, "rId1", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument", "xl/workbook.xml");
                w.WriteEndElement();
            });
        }

        private static void WriteWorkbook(ZipArchive zip, IList<Sheet> sheets)
        {
            WriteXml(zip, "xl/workbook.xml", w =>
            {
                w.WriteStartElement("workbook", S);
                w.WriteAttributeString("xmlns", "r", "http://www.w3.org/2000/xmlns/", R);
                w.WriteStartElement("sheets", S);
                for (var i = 0; i < sheets.Count; i++)
                {
                    w.WriteStartElement("sheet", S);
                    w.WriteAttributeString("name", sheets[i].Name);
                    w.WriteAttributeString("sheetId", (i + 1).ToString(CultureInfo.InvariantCulture));
                    w.WriteAttributeString("r", "id", R, "rId" + (i + 1));
                    w.WriteEndElement();
                }
                w.WriteEndElement();
                w.WriteEndElement();
            });
        }

        private static void WriteWorkbookRelationships(ZipArchive zip, int sheetCount)
        {
            WriteXml(zip, "xl/_rels/workbook.xml.rels", w =>
            {
                w.WriteStartElement("Relationships", PackageRels);
                for (var i = 1; i <= sheetCount; i++)
                    Relationship(w, "rId" + i, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet", "worksheets/sheet" + i + ".xml");
                w.WriteEndElement();
            });
        }

        private static void Relationship(XmlWriter w, string id, string type, string target)
        {
            w.WriteStartElement("Relationship", PackageRels);
            w.WriteAttributeString("Id", id);
            w.WriteAttributeString("Type", type);
            w.WriteAttributeString("Target", target);
            w.WriteEndElement();
        }

        private static void WriteSheet(ZipArchive zip, int sheetNumber, Sheet sheet)
        {
            WriteXml(zip, "xl/worksheets/sheet" + sheetNumber + ".xml", w =>
            {
                w.WriteStartElement("worksheet", S);
                w.WriteStartElement("sheetViews", S);
                w.WriteStartElement("sheetView", S);
                w.WriteAttributeString("workbookViewId", "0");
                w.WriteStartElement("pane", S);
                w.WriteAttributeString("ySplit", "1");
                w.WriteAttributeString("topLeftCell", "A2");
                w.WriteAttributeString("activePane", "bottomLeft");
                w.WriteAttributeString("state", "frozen");
                w.WriteEndElement();
                w.WriteEndElement();
                w.WriteEndElement();

                w.WriteStartElement("sheetData", S);
                WriteRow(w, 1, sheet.Headers);
                for (var i = 0; i < sheet.RowCount; i++) WriteRow(w, i + 2, sheet.RowFactory(i));
                w.WriteEndElement();

                if (sheet.Headers != null && sheet.Headers.Length > 0)
                {
                    w.WriteStartElement("autoFilter", S);
                    w.WriteAttributeString("ref", "A1:" + ColumnName(sheet.Headers.Length) + Math.Max(1, sheet.RowCount + 1).ToString(CultureInfo.InvariantCulture));
                    w.WriteEndElement();
                }
                w.WriteEndElement();
            });
        }

        private static void WriteRow(XmlWriter w, int rowNumber, object[] values)
        {
            w.WriteStartElement("row", S);
            w.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));
            for (var i = 0; i < values.Length; i++) WriteCell(w, rowNumber, i + 1, values[i]);
            w.WriteEndElement();
        }

        private static void WriteRow(XmlWriter w, int rowNumber, string[] values)
        {
            var objects = new object[values.Length];
            for (var i = 0; i < values.Length; i++) objects[i] = values[i];
            WriteRow(w, rowNumber, objects);
        }

        private static void WriteCell(XmlWriter w, int row, int column, object value)
        {
            w.WriteStartElement("c", S);
            w.WriteAttributeString("r", ColumnName(column) + row.ToString(CultureInfo.InvariantCulture));

            if (value is byte || value is short || value is int || value is long || value is float || value is double || value is decimal)
            {
                w.WriteStartElement("v", S);
                w.WriteString(Convert.ToString(value, CultureInfo.InvariantCulture));
                w.WriteEndElement();
            }
            else
            {
                w.WriteAttributeString("t", "inlineStr");
                w.WriteStartElement("is", S);
                w.WriteStartElement("t", S);
                var text = value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
                if (text.StartsWith(" ", StringComparison.Ordinal) || text.EndsWith(" ", StringComparison.Ordinal))
                    w.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                w.WriteString(text);
                w.WriteEndElement();
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }

        private static void WriteXml(ZipArchive zip, string path, Action<XmlWriter> body)
        {
            var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                CloseOutput = false,
                CheckCharacters = true
            }))
            {
                writer.WriteStartDocument(true);
                body(writer);
                writer.WriteEndDocument();
            }
        }

        private static string ColumnName(int number)
        {
            var result = string.Empty;
            while (number > 0)
            {
                var modulo = (number - 1) % 26;
                result = Convert.ToChar(65 + modulo) + result;
                number = (number - modulo) / 26;
            }
            return result;
        }
    }
}
