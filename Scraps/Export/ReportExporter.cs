using OfficeOpenXml;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Data;
using System.IO;

namespace Scraps.Export
{
    /// <summary>
    /// Экспорт DataTable в Excel/PDF.
    /// </summary>
    public static class ReportExporter
    {
        /// <summary>
        /// Экспортировать DataTable в Excel.
        /// </summary>
        public static void ExportToExcel(DataTable data, string filePath, string sheetName = "Report")
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var excelPackage = new ExcelPackage())
            {
                var worksheet = excelPackage.Workbook.Worksheets.Add(sheetName);
                worksheet.Cells["A1"].LoadFromDataTable(data, true);
                worksheet.Cells.AutoFitColumns();
                excelPackage.SaveAs(new FileInfo(filePath));
            }
        }

        /// <summary>
        /// Экспортировать DataTable в PDF.
        /// </summary>
        public static void ExportToPdf(DataTable data, string filePath, string title = "Отчёт")
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                var document = new Document();
                var writer = PdfWriter.GetInstance(document, fs);
                var baseFont = BaseFont.CreateFont("c:/windows/fonts/arial.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                document.Open();

                var titleParagraph = new Paragraph(title, new Font(baseFont, 14, Font.BOLD))
                {
                    Alignment = Element.ALIGN_CENTER
                };
                document.Add(titleParagraph);
                document.Add(new Paragraph(" "));

                var table = new PdfPTable(data.Columns.Count)
                {
                    WidthPercentage = 100
                };

                foreach (DataColumn column in data.Columns)
                {
                    var cell = new PdfPCell(new Phrase(column.ColumnName, new Font(baseFont, 10, Font.BOLD)))
                    {
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                foreach (DataRow row in data.Rows)
                {
                    foreach (var item in row.ItemArray)
                    {
                        table.AddCell(new Phrase(item?.ToString() ?? "", new Font(baseFont, 10)));
                    }
                }

                document.Add(table);
                document.Close();
                writer.Close();
            }
        }
    }
}




