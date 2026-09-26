using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using glps.Models;

namespace glps.Services
{
    /// <summary>
    /// Reads passenger risk rows (bchn_data) from the first worksheet of an .xlsx file.
    /// The first row must hold the column headers; matching ignores case, spaces and underscores.
    /// </summary>
    public static class BchnExcelImporter
    {
        public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private class Column
        {
            public string Header;
            public string[] Aliases;
            public Action<bchn_data, string> Set;
        }

        private static readonly Column[] Columns =
        {
            new Column { Header = "Fore_Name", Aliases = new[] { "forenane", "forename", "firstname" }, Set = (b, v) => b.Fore_Nane = v },
            new Column { Header = "Family_Name", Aliases = new[] { "familyname", "surname", "lastname" }, Set = (b, v) => b.Family_Name = v },
            new Column { Header = "Gender", Aliases = new[] { "gender", "sex" }, Set = (b, v) => b.Gender = v },
            new Column { Header = "DOB", Aliases = new[] { "dob", "dateofbirth" }, Set = (b, v) => b.DOB = v },
            new Column { Header = "Nationality", Aliases = new[] { "nationality" }, Set = (b, v) => b.Nationality = v },
            new Column { Header = "Passport_number", Aliases = new[] { "passportnumber", "passportno", "passport" }, Set = (b, v) => b.Passport_number = v },
            new Column { Header = "Terrorism", Aliases = new[] { "terrorism" }, Set = (b, v) => b.Terrorism = v },
            new Column { Header = "Narcotics", Aliases = new[] { "narcotics" }, Set = (b, v) => b.Narcotics = v },
            new Column { Header = "Smuggling", Aliases = new[] { "smuggling" }, Set = (b, v) => b.Smuggling = v },
            new Column { Header = "Illegal_Immigration", Aliases = new[] { "illegalimmigration" }, Set = (b, v) => b.Illegal_Immigration = v },
            new Column { Header = "Revenue", Aliases = new[] { "revenue" }, Set = (b, v) => b.Revenue = v }
        };

        private static readonly string[] RequiredHeaders = { "Fore_Name", "Family_Name", "Passport_number" };

        public class Result
        {
            public List<bchn_data> Rows { get; } = new List<bchn_data>();
            public List<string> Errors { get; } = new List<string>();
        }

        public static Result Read(Stream stream)
        {
            var result = new Result();

            IXLWorkbook workbook;
            try
            {
                workbook = new XLWorkbook(stream);
            }
            catch (Exception)
            {
                result.Errors.Add("The file could not be read. Please upload an Excel workbook saved as .xlsx.");
                return result;
            }

            using (workbook)
            {
                var sheet = workbook.Worksheets.FirstOrDefault();
                var used = sheet == null ? null : sheet.RangeUsed();
                if (used == null)
                {
                    result.Errors.Add("The workbook is empty.");
                    return result;
                }

                var headerRow = used.FirstRow();
                var map = new Dictionary<int, Column>();
                foreach (var cell in headerRow.Cells())
                {
                    var header = Normalize(cell.GetString());
                    var column = Columns.FirstOrDefault(c => c.Aliases.Contains(header));
                    if (column != null && !map.Values.Contains(column))
                    {
                        map[cell.Address.ColumnNumber] = column;
                    }
                }

                var missing = Columns.Where(c => !map.Values.Contains(c)).Select(c => c.Header).ToList();
                if (missing.Count > 0)
                {
                    result.Errors.Add("Missing column(s): " + string.Join(", ", missing) +
                        ". Download the template to see the expected layout.");
                    return result;
                }

                var seenPassports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in used.RowsUsed().Skip(1))
                {
                    var rowNumber = row.RowNumber();
                    var item = new bchn_data();
                    var values = new Dictionary<string, string>();
                    foreach (var pair in map)
                    {
                        var value = row.WorksheetRow().Cell(pair.Key).GetFormattedString().Trim();
                        values[pair.Value.Header] = value;
                        pair.Value.Set(item, value.Length == 0 ? null : value);
                    }

                    if (values.Values.All(string.IsNullOrEmpty)) continue;

                    var rowErrors = RequiredHeaders
                        .Where(h => string.IsNullOrEmpty(values[h]))
                        .Select(h => h.Replace("_", " ") + " is required")
                        .ToList();

                    if (!string.IsNullOrEmpty(item.Passport_number) && !seenPassports.Add(item.Passport_number))
                    {
                        rowErrors.Add("passport number " + item.Passport_number + " appears more than once in the file");
                    }

                    if (rowErrors.Count > 0)
                    {
                        result.Errors.Add("Row " + rowNumber + ": " + string.Join("; ", rowErrors) + ".");
                    }
                    else
                    {
                        result.Rows.Add(item);
                    }
                }

                if (result.Rows.Count == 0 && result.Errors.Count == 0)
                {
                    result.Errors.Add("The worksheet has headers but no data rows.");
                }
            }

            return result;
        }

        /// <summary>Creates an empty .xlsx with the expected header row.</summary>
        public static byte[] CreateTemplate()
        {
            using (var workbook = new XLWorkbook())
            using (var output = new MemoryStream())
            {
                var sheet = workbook.Worksheets.Add("Sheet1");
                for (var i = 0; i < Columns.Length; i++)
                {
                    sheet.Cell(1, i + 1).Value = Columns[i].Header;
                }
                sheet.Row(1).Style.Font.Bold = true;
                sheet.Columns(1, Columns.Length).Width = 20;
                workbook.SaveAs(output);
                return output.ToArray();
            }
        }

        private static string Normalize(string header)
        {
            return (header ?? "").Replace("_", "").Replace(" ", "").Trim().ToLowerInvariant();
        }
    }
}
