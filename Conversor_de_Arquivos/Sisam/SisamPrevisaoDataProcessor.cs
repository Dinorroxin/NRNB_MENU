using System.Globalization;
using System.Text;
using Conversor_de_Arquivos.Models;
using OfficeOpenXml;

namespace Conversor_de_Arquivos.Sisam
{
    public class SisamPrevisaoDataProcessor
    {
        public void SalvarRegistros(List<SisamRecord> registros, string caminhoMestre)
        {
            var lote = registros
                .Where(r => NormalizarEstado(r.Estado) == "RONDONIA" && r.Municipio != null)
                .ToList();

            var chavesBatch = lote
                .Select(r => (r.Poluente, r.DiasPrevisao, r.Data))
                .ToHashSet();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var fileInfo = new FileInfo(caminhoMestre);
            fileInfo.Directory?.Create();

            var linhasExistentes = new List<LinhaExistente>();

            if (fileInfo.Exists)
            {
                using var pkgRead = new ExcelPackage(fileInfo);
                var wsRead  = pkgRead.Workbook.Worksheets[0];
                int lastRow = wsRead.Dimension?.End.Row ?? 1;

                for (int r = 2; r <= lastRow; r++)
                {
                    string mun = wsRead.Cells[r, 1].Text;
                    if (string.IsNullOrWhiteSpace(mun)) continue;

                    string pol  = wsRead.Cells[r, 3].Text;
                    string data = wsRead.Cells[r, 4].Text;
                    int    dias = int.TryParse(wsRead.Cells[r, 5].Text, out int d) ? d : -1;

                    if (chavesBatch.Contains((pol, dias, data))) continue;

                    linhasExistentes.Add(new LinhaExistente(
                        mun,
                        wsRead.Cells[r, 2].Text,
                        pol,
                        data,
                        dias,
                        LerDouble(wsRead, r, 6),
                        wsRead.Cells[r, 7].Text,
                        wsRead.Cells[r, 8].Text
                    ));
                }
            }

            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Dados");

            ws.Cells[1, 1].Value = "Município";
            ws.Cells[1, 2].Value = "Regional";
            ws.Cells[1, 3].Value = "Poluente";
            ws.Cells[1, 4].Value = "Data";
            ws.Cells[1, 5].Value = "DiasPrevisao";
            ws.Cells[1, 6].Value = "Valor";
            ws.Cells[1, 7].Value = "Unidade";
            ws.Cells[1, 8].Value = "Classificação";

            int row = 2;

            foreach (var e in linhasExistentes)
            {
                ws.Cells[row, 1].Value = e.Municipio;
                ws.Cells[row, 2].Value = e.Regional;
                ws.Cells[row, 3].Value = e.Poluente;
                ws.Cells[row, 4].Value = e.Data;
                ws.Cells[row, 5].Value = e.DiasPrevisao;
                ws.Cells[row, 6].Value = e.Valor;
                ws.Cells[row, 7].Value = e.Unidade;
                ws.Cells[row, 8].Value = e.Classificacao;
                row++;
            }

            foreach (var r in lote)
            {
                string chave    = StripAccents(r.Municipio!.ToUpperInvariant());
                string regional = RondoniaRegionais.GetRegion(chave);

                ws.Cells[row, 1].Value = r.Municipio;
                ws.Cells[row, 2].Value = regional;
                ws.Cells[row, 3].Value = r.Poluente;
                ws.Cells[row, 4].Value = r.Data;
                ws.Cells[row, 5].Value = r.DiasPrevisao;
                ws.Cells[row, 6].Value = r.Valor;
                ws.Cells[row, 7].Value = Unidade(r.Poluente);
                ws.Cells[row, 8].Value = r.ClassificacaoNome;
                row++;
            }

            if (ws.Dimension != null)
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

            pkg.SaveAs(fileInfo);
        }

        private static string NormalizarEstado(string estado)
            => StripAccents(estado.Trim().ToUpperInvariant());

        private static string Unidade(string poluente) => poluente switch
        {
            "CO" => "mg/m³",
            _    => "μg/m³"
        };

        private static double LerDouble(ExcelWorksheet ws, int row, int col)
        {
            object? v = ws.Cells[row, col].Value;
            return v switch
            {
                double d => d,
                int    i => i,
                _        => double.TryParse(ws.Cells[row, col].Text, NumberStyles.Number,
                                CultureInfo.InvariantCulture, out double r) ? r : 0
            };
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

        private sealed record LinhaExistente(
            string Municipio,
            string Regional,
            string Poluente,
            string Data,
            int    DiasPrevisao,
            double Valor,
            string Unidade,
            string Classificacao);
    }
}
