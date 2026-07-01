using System.Globalization;
using System.Text;
using OfficeOpenXml;

namespace Conversor_de_Arquivos
{
    public class FocosCalorDataProcessor
    {
        // Returns the highest (Ano, Semana) pair already recorded, or null if the file is empty/absent.
        public (int Ano, int Semana)? ReadMaxWeek(string pathMestre)
        {
            if (!File.Exists(pathMestre)) return null;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage(new FileInfo(pathMestre));
            if (pkg.Workbook.Worksheets.Count == 0) return null;

            var ws = pkg.Workbook.Worksheets[0];
            int lastRow = ws.Dimension?.End.Row ?? 1;

            int maxAno = 0, maxSem = 0;
            bool found = false;

            for (int r = 2; r <= lastRow; r++)
            {
                int ano = GetInt(ws, r, 3);
                int sem = GetInt(ws, r, 4);
                if (ano == 0) continue;

                if (!found || ano > maxAno || (ano == maxAno && sem > maxSem))
                {
                    maxAno  = ano;
                    maxSem  = sem;
                    found   = true;
                }
            }

            return found ? (maxAno, maxSem) : null;
        }

        // Appends the week's rows to the master file (append-only).
        // Skips silently if (Ano, Semana) already exists in the file.
        public async Task AppendSemanaAsync(SemanaFocos semana, string pathMestre, IProgress<string>? progress = null)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var fileInfo = new FileInfo(pathMestre);

            // Ensure directory exists
            fileInfo.Directory?.Create();

            using var pkg = fileInfo.Exists
                ? new ExcelPackage(fileInfo)
                : new ExcelPackage();

            ExcelWorksheet ws;
            if (pkg.Workbook.Worksheets.Count == 0)
            {
                ws = pkg.Workbook.Worksheets.Add("Dados");
                ws.Cells[1, 1].Value = "Município";
                ws.Cells[1, 2].Value = "Regional";
                ws.Cells[1, 3].Value = "Ano";
                ws.Cells[1, 4].Value = "Semana";
                ws.Cells[1, 5].Value = "Data_Inicio_Semana";
                ws.Cells[1, 6].Value = "Data_Final_Semana";
                ws.Cells[1, 7].Value = "Focos";
            }
            else
            {
                ws = pkg.Workbook.Worksheets[0];
            }

            int lastRow = ws.Dimension?.End.Row ?? 1;

            // Defensive duplicate check by (Ano, Semana)
            for (int r = 2; r <= lastRow; r++)
            {
                if (GetInt(ws, r, 3) == semana.Ano && GetInt(ws, r, 4) == semana.Semana)
                    return;
            }

            int nextRow = lastRow + 1;

            if (semana.Focos.Count == 0)
            {
                WriteRow(ws, nextRow, "Sem Registro", "Sem Registro", semana);
            }
            else
            {
                foreach (var foco in semana.Focos)
                {
                    string municipioNorm = RemoverAcentos(foco.Municipio.Trim().ToUpperInvariant());
                    string regional = RondoniaRegionais.ObterRegional(municipioNorm);

                    // Fallback when ObterRegional returns its own "Não identificado" sentinel
                    if (string.IsNullOrWhiteSpace(regional) || regional == "Não identificado")
                        regional = "Sem Registro";

                    WriteRow(ws, nextRow, foco.Municipio, regional, semana, foco.Focos);
                    nextRow++;
                }
            }

            await pkg.SaveAsAsync(fileInfo);
        }

        private static void WriteRow(ExcelWorksheet ws, int row,
            string municipio, string regional, SemanaFocos semana, int focos = 0)
        {
            ws.Cells[row, 1].Value = municipio;
            ws.Cells[row, 2].Value = regional;
            ws.Cells[row, 3].Value = semana.Ano;
            ws.Cells[row, 4].Value = semana.Semana;
            ws.Cells[row, 5].Value = semana.Inicio.Date;
            ws.Cells[row, 5].Style.Numberformat.Format = "dd/MM/yyyy";
            ws.Cells[row, 6].Value = semana.Fim.Date;
            ws.Cells[row, 6].Style.Numberformat.Format = "dd/MM/yyyy";
            ws.Cells[row, 7].Value = focos;
        }

        private static int GetInt(ExcelWorksheet ws, int row, int col)
        {
            object? v = ws.Cells[row, col].Value;
            return v switch
            {
                int    i => i,
                double d => (int)Math.Round(d),
                _        => int.TryParse(ws.Cells[row, col].Text, out int n) ? n : 0,
            };
        }

        private static string RemoverAcentos(string texto)
        {
            string normalizado = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalizado.Length);
            foreach (char c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
