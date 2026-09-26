using System.IO;
using ClosedXML.Excel;
using glps.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace glps.Tests.Services
{
    [TestClass]
    public class BchnExcelImporterTest
    {
        private static MemoryStream Workbook(params object[][] rows)
        {
            var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Sheet1");
                for (var r = 0; r < rows.Length; r++)
                    for (var c = 0; c < rows[r].Length; c++)
                        sheet.Cell(r + 1, c + 1).Value = rows[r][c];
                workbook.SaveAs(stream);
            }
            stream.Position = 0;
            return stream;
        }

        private static readonly object[] Header =
        {
            "Fore_Nane", "Family Name", "Gender", "DOB", "Nationality", "Passport_number",
            "Terrorism", "Narcotics", "Smuggling", "Illegal_Immigration", "Revenue"
        };

        [TestMethod]
        public void ReadsValidRows()
        {
            var result = BchnExcelImporter.Read(Workbook(
                Header,
                new object[] { "Ann", "Lee", "F", "1990-01-01", "GB", "P1", "10", "No", "", "", "Yes" }));

            Assert.AreEqual(0, result.Errors.Count, string.Join("\n", result.Errors));
            Assert.AreEqual(1, result.Rows.Count);
            Assert.AreEqual("Ann", result.Rows[0].Fore_Nane);
            Assert.AreEqual("Lee", result.Rows[0].Family_Name);
            Assert.AreEqual("P1", result.Rows[0].Passport_number);
            Assert.IsNull(result.Rows[0].Smuggling);
        }

        [TestMethod]
        public void ReportsRowErrorsAndDuplicates()
        {
            var result = BchnExcelImporter.Read(Workbook(
                Header,
                new object[] { "Ann", "Lee", "F", "", "GB", "P1" },
                new object[] { "", "Kim", "M", "", "KR", "P2" },
                new object[] { "Bo", "Ng", "M", "", "SG", "p1" }));

            Assert.AreEqual(1, result.Rows.Count);
            Assert.AreEqual(2, result.Errors.Count);
            StringAssert.StartsWith(result.Errors[0], "Row 3: Fore Name is required");
            StringAssert.Contains(result.Errors[1], "more than once");
        }

        [TestMethod]
        public void ReportsMissingColumns()
        {
            var result = BchnExcelImporter.Read(Workbook(new object[] { "Fore_Name", "Family_Name" }, new object[] { "A", "B" }));
            Assert.AreEqual(0, result.Rows.Count);
            StringAssert.StartsWith(result.Errors[0], "Missing column(s): Gender");
        }

        [TestMethod]
        public void RejectsNonExcelContent()
        {
            var result = BchnExcelImporter.Read(new MemoryStream(new byte[] { 1, 2, 3 }));
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void TemplateHasAllHeaders()
        {
            var result = BchnExcelImporter.Read(new MemoryStream(BchnExcelImporter.CreateTemplate()));
            Assert.AreEqual(0, result.Rows.Count);
            Assert.AreEqual("The worksheet has headers but no data rows.", result.Errors[0]);
        }
    }
}
