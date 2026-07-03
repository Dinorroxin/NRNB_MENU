using System.Globalization;
using System.Text;
using OfficeOpenXml;

namespace Conversor_de_Arquivos
{
    public class IqArDataProcessor
    {
        public async Task ProcessAsync(
            string csvPath,
            string masterPath,
            IProgress<string>? progress = null)
        {
            var lines = await File.ReadAllLinesAsync(csvPath, Encoding.UTF8);
            if (lines.Length < 2)
            {
                progress?.Report("CSV vazio ou sem linhas de dados.");
                return;
            }

            var header = lines[0].Split(';');
            int idxName  = IndexOf(header, "name_muni");
            int idxDate  = IndexOf(header, "date");
            int idxIqar  = IndexOf(header, "iqar");
            int idxLabel = IndexOf(header, "label");

            var rows       = new List<(string Municipality, string Region, DateTime Date, double Iqar, string Classification, string Quality)>();
            int discarded = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var cols = lines[i].Split(';');

                string dateStr = Column(cols, idxDate);
                if (dateStr.Equals("NA", StringComparison.OrdinalIgnoreCase))
                {
                    discarded++;
                    continue;
                }

                if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime date))
                    continue;

                string name = Column(cols, idxName);
                if (name.EndsWith(" - RO", StringComparison.OrdinalIgnoreCase))
                    name = name[..^5].Trim();

                string key    = StripAccents(name.Trim().ToUpperInvariant());
                string region = RondoniaRegionais.GetRegion(key);
                if (region == "Não identificado") region = "Sem Registro";

                string iqarStr = Column(cols, idxIqar).Replace(',', '.');
                double.TryParse(iqarStr, NumberStyles.Number, CultureInfo.InvariantCulture, out double iqar);

                string label = Column(cols, idxLabel);
                string classification, quality;
                int sep = label.IndexOf(" - ", StringComparison.Ordinal);
                if (sep >= 0)
                {
                    classification = label[..sep].Trim();
                    quality        = label[(sep + 3)..].Trim();
                }
                else
                {
                    classification = label;
                    quality        = string.Empty;
                }

                rows.Add((name, region, date, iqar, classification, quality));
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var fileInfo = new FileInfo(masterPath);
            fileInfo.Directory?.Create();

            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Dados");

            ws.Cells[1, 1].Value = "Município";
            ws.Cells[1, 2].Value = "Regional";
            ws.Cells[1, 3].Value = "Data";
            ws.Cells[1, 4].Value = "Hora";
            ws.Cells[1, 5].Value = "IQAr";
            ws.Cells[1, 6].Value = "Classificação";
            ws.Cells[1, 7].Value = "Qualidade";

            for (int r = 0; r < rows.Count; r++)
            {
                var (municipality, region, date, iqar, classification, quality) = rows[r];
                int row = r + 2;
                ws.Cells[row, 1].Value = municipality;
                ws.Cells[row, 2].Value = region;
                ws.Cells[row, 3].Value = date.Date;
                ws.Cells[row, 3].Style.Numberformat.Format = "dd/MM/yyyy";
                ws.Cells[row, 4].Value = date.ToString("HH:mm");
                ws.Cells[row, 5].Value = iqar;
                ws.Cells[row, 6].Value = classification;
                ws.Cells[row, 7].Value = quality;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            await pkg.SaveAsAsync(fileInfo);

            string summary = $"Master atualizado: {rows.Count} linha(s) gravada(s).";
            if (discarded > 0)
                summary += $" {discarded} linha(s) descartada(s) por date=NA.";
            progress?.Report(summary);
        }

        private static string Column(string[] cols, int idx)
            => idx >= 0 && idx < cols.Length ? cols[idx].Trim() : string.Empty;

        private static int IndexOf(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
                if (header[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private static string StripAccents(string text)
        {
            string norm = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(norm.Length);
            foreach (char c in norm)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
