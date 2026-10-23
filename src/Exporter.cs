using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CyberClub
{
    public static class Exporter
    {
        static string X(string s)
        {
            return SecurityElement.Escape(s ?? "");
        }

        static void Part(ZipArchive zip, string name, string text)
        {
            using (var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)))
                w.Write(text);
        }

        public static void Save(string path, string title, DataTable data)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".docx":
                    Word(path, title, data);
                    break;
                case ".xlsx":
                    Excel(path, title, data);
                    break;
                case ".pdf":
                    Pdf(path, title, data);
                    break;
                default:
                    throw new Exception("Выберите формат docx, pdf или xlsx.");
            }
        }

        static string Paragraph(string text, int size = 20, bool bold = false)
        {
            return "<w:p><w:r><w:rPr><w:rFonts w:ascii=\"Arial\" w:hAnsi=\"Arial\"/><w:sz w:val=\"" + size + "\"/>" + (bold ? "<w:b/>" : "") + "</w:rPr><w:t xml:space=\"preserve\">" + X(text) + "</w:t></w:r></w:p>";
        }

        public static void Word(string path, string title, DataTable data)
        {
            using (var f = File.Create(path))
            using (var zip = new ZipArchive(f, ZipArchiveMode.Create))
            {
                Part(zip, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/><Override PartName=\"/word/footer1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml\"/></Types>");
                Part(zip, "_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
                Part(zip, "word/_rels/document.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rIdFooter\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer\" Target=\"footer1.xml\"/></Relationships>");
                Part(zip, "word/footer1.xml", "<w:ftr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:fldSimple w:instr=\"PAGE\"/></w:p></w:ftr>");
                var xml = new StringBuilder("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><w:body>");
                xml.Append(Paragraph("CyberClub • " + title, 30, true));
                xml.Append(Paragraph("Дата отчёта: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + ". Записей: " + data.Rows.Count));
                xml.Append("<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/><w:tblBorders>");
                foreach (var side in new[]
                {
                    "top",
                    "left",
                    "bottom",
                    "right",
                    "insideH",
                    "insideV"
                }

                )
                    xml.Append("<w:" + side + " w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/>");
                xml.Append("</w:tblBorders><w:tblCellMar><w:top w:w=\"90\" w:type=\"dxa\"/><w:left w:w=\"90\" w:type=\"dxa\"/><w:bottom w:w=\"90\" w:type=\"dxa\"/><w:right w:w=\"90\" w:type=\"dxa\"/></w:tblCellMar></w:tblPr>");
                int width = 14500 / Math.Max(1, data.Columns.Count);
                xml.Append("<w:tblGrid>");
                foreach (DataColumn c in data.Columns)
                    xml.Append("<w:gridCol w:w=\"" + width + "\"/>");
                xml.Append("</w:tblGrid><w:tr><w:trPr><w:tblHeader/></w:trPr>");
                foreach (DataColumn c in data.Columns)
                    xml.Append("<w:tc><w:tcPr><w:tcW w:w=\"" + width + "\" w:type=\"dxa\"/><w:shd w:fill=\"EAEAED\"/></w:tcPr>" + Paragraph(c.ColumnName, 18, true) + "</w:tc>");
                xml.Append("</w:tr>");
                foreach (DataRow r in data.Rows)
                {
                    xml.Append("<w:tr><w:trPr><w:cantSplit/></w:trPr>");
                    foreach (DataColumn c in data.Columns)
                        xml.Append("<w:tc>" + Paragraph(MainWindow.Format(r[c]), 18) + "</w:tc>");
                    xml.Append("</w:tr>");
                }

                xml.Append("</w:tbl>");
                foreach (DataColumn c in data.Columns)
                    if (c.DataType == typeof(decimal))
                        xml.Append(Paragraph("Итого «" + c.ColumnName + "»: " + data.Rows.Cast<DataRow>().Sum(r => r.IsNull(c) ? 0 : Convert.ToDecimal(r[c])).ToString("N2")));
                xml.Append("<w:sectPr><w:footerReference w:type=\"default\" r:id=\"rIdFooter\"/><w:pgSz w:w=\"16838\" w:h=\"11906\" w:orient=\"landscape\"/><w:pgMar w:top=\"850\" w:right=\"850\" w:bottom=\"850\" w:left=\"850\" w:footer=\"400\"/></w:sectPr></w:body></w:document>");
                Part(zip, "word/document.xml", xml.ToString());
            }
        }

        public static void Excel(string path, string title, DataTable data)
        {
            using (var f = File.Create(path))
            using (var zip = new ZipArchive(f, ZipArchiveMode.Create))
            {
                Part(zip, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
                Part(zip, "_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                Part(zip, "xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Отчёт\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                Part(zip, "xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                var xml = new StringBuilder("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"3\" topLeftCell=\"A4\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><cols>");
                for (int i = 0; i < data.Columns.Count; i++)
                    xml.Append("<col min=\"" + (i + 1) + "\" max=\"" + (i + 1) + "\" width=\"28\" customWidth=\"1\"/>");
                xml.Append("</cols><sheetData><row r=\"1\">" + Cell("A1", title) + "</row><row r=\"2\">" + Cell("A2", "Дата: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm")) + "</row><row r=\"3\">");
                for (int i = 0; i < data.Columns.Count; i++)
                    xml.Append(Cell(Column(i) + "3", data.Columns[i].ColumnName));
                xml.Append("</row>");
                int row = 4;
                foreach (DataRow r in data.Rows)
                {
                    xml.Append("<row r=\"" + row + "\">");
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        object value = r[i];
                        string reference = Column(i) + row;
                        if (value is decimal || value is int || value is long)
                            xml.Append("<c r=\"" + reference + "\"><v>" + Convert.ToString(value, CultureInfo.InvariantCulture) + "</v></c>");
                        else
                            xml.Append(Cell(reference, MainWindow.Format(value)));
                    }

                    xml.Append("</row>");
                    row++;
                }

                xml.Append("</sheetData><autoFilter ref=\"A3:" + Column(data.Columns.Count - 1) + Math.Max(3, row - 1) + "\"/><pageMargins left=\"0.3\" right=\"0.3\" top=\"0.5\" bottom=\"0.5\" header=\"0.2\" footer=\"0.2\"/><pageSetup orientation=\"landscape\" paperSize=\"9\"/></worksheet>");
                Part(zip, "xl/worksheets/sheet1.xml", xml.ToString());
            }
        }

        static string Cell(string reference, string value)
        {
            return "<c r=\"" + reference + "\" t=\"inlineStr\"><is><t xml:space=\"preserve\">" + X(value) + "</t></is></c>";
        }

        static string Column(int n)
        {
            string s = "";
            do
            {
                s = (char)('A' + n % 26) + s;
                n = n / 26 - 1;
            }
            while (n >= 0);
            return s;
        }

        static FormattedText Text(string s, double size, double width, bool bold = false)
        {
            var t = new FormattedText(s ?? "", CultureInfo.GetCultureInfo("ru-RU"), FlowDirection.LeftToRight, new Typeface(new FontFamily("Arial"), FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal), size, Brushes.Black);
            t.MaxTextWidth = Math.Max(10, width);
            return t;
        }

        // кириллица сохраняется независимо от шрифтов PDF
        public static void Pdf(string path, string title, DataTable data)
        {
            const int w = 1122, h = 794;
            var pages = new List<byte[]>();
            int cursor = 0, page = 1;
            bool first = true;
            do
            {
                var v = new DrawingVisual();
                using (var dc = v.RenderOpen())
                {
                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
                    dc.DrawText(Text("CyberClub • " + title, 20, w - 70, true), new Point(35, 24));
                    dc.DrawText(Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm") + " • Записей: " + data.Rows.Count, 11, w - 70), new Point(35, 58));
                    double x = 35, y = 90, cw = (w - 70.0) / Math.Max(1, data.Columns.Count), header = 28;
                    foreach (DataColumn c in data.Columns)
                        header = Math.Max(header, Text(c.ColumnName, 10, cw - 12, true).Height + 12);
                    foreach (DataColumn c in data.Columns)
                    {
                        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(234, 234, 237)), new Pen(Brushes.Gray, .5), new Rect(x, y, cw, header));
                        dc.DrawText(Text(c.ColumnName, 10, cw - 12, true), new Point(x + 6, y + 6));
                        x += cw;
                    }

                    y += header;
                    while (cursor < data.Rows.Count)
                    {
                        var r = data.Rows[cursor];
                        double rh = 25;
                        foreach (object val in r.ItemArray)
                            rh = Math.Max(rh, Text(MainWindow.Format(val), 10, cw - 12).Height + 12);
                        if (y + rh > h - 50)
                            break;
                        x = 35;
                        foreach (object val in r.ItemArray)
                        {
                            dc.DrawRectangle(cursor % 2 == 0 ? Brushes.White : new SolidColorBrush(Color.FromRgb(245, 245, 245)), new Pen(Brushes.Gray, .5), new Rect(x, y, cw, rh));
                            dc.DrawText(Text(MainWindow.Format(val), 10, cw - 12), new Point(x + 6, y + 6));
                            x += cw;
                        }

                        y += rh;
                        cursor++;
                    }

                    dc.DrawText(Text("Страница " + page, 11, w - 70), new Point(35, h - 32));
                }

                var bmp = new RenderTargetBitmap(w * 2, h * 2, 192, 192, PixelFormats.Pbgra32);
                bmp.Render(v);
                var enc = new JpegBitmapEncoder
                {
                    QualityLevel = 90
                };
                enc.Frames.Add(BitmapFrame.Create(bmp));
                using (var stream = new MemoryStream())
                {
                    enc.Save(stream);
                    pages.Add(stream.ToArray());
                }

                page++;
                first = false;
            }
            while (cursor < data.Rows.Count || first);
            var objects = new List<byte[]>();
            Action<string> add = s => objects.Add(Encoding.ASCII.GetBytes(s));
            add("<< /Type /Catalog /Pages 2 0 R >>");
            add("<< /Type /Pages /Count " + pages.Count + " /Kids [" + String.Join(" ", Enumerable.Range(0, pages.Count).Select(i => (3 + i * 3) + " 0 R")) + "] >>");
            for (int i = 0; i < pages.Count; i++)
            {
                int id = 3 + i * 3;
                add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /XObject << /Im0 " + (id + 2) + " 0 R >> >> /Contents " + (id + 1) + " 0 R >>");
                string content = "q 842 0 0 595 0 0 cm /Im0 Do Q";
                add("<< /Length " + content.Length + " >>\nstream\n" + content + "\nendstream");
                using (var stream = new MemoryStream())
                {
                    var header = Encoding.ASCII.GetBytes("<< /Type /XObject /Subtype /Image /Width " + w * 2 + " /Height " + h * 2 + " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " + pages[i].Length + " >>\nstream\n");
                    stream.Write(header, 0, header.Length);
                    stream.Write(pages[i], 0, pages[i].Length);
                    var end = Encoding.ASCII.GetBytes("\nendstream");
                    stream.Write(end, 0, end.Length);
                    objects.Add(stream.ToArray());
                }
            }

            using (var f = File.Create(path))
            {
                Action<string> write = s =>
                {
                    var b = Encoding.ASCII.GetBytes(s);
                    f.Write(b, 0, b.Length);
                };
                write("%PDF-1.4\n");
                var positions = new List<long>
                {
                    0
                };
                for (int i = 0; i < objects.Count; i++)
                {
                    positions.Add(f.Position);
                    write((i + 1) + " 0 obj\n");
                    f.Write(objects[i], 0, objects[i].Length);
                    write("\nendobj\n");
                }

                long xref = f.Position;
                write("xref\n0 " + (objects.Count + 1) + "\n0000000000 65535 f \n");
                foreach (long pos in positions.Skip(1))
                    write(pos.ToString("0000000000") + " 00000 n \n");
                write("trailer\n<< /Size " + (objects.Count + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF");
            }
        }
    }
}
