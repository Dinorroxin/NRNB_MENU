using System.Globalization;
using System.Text;
using OfficeOpenXml;

namespace Conversor_de_Arquivos
{
    public class IqArDataProcessor
    {
        // Sobrescreve IQAR_MESTRE.xlsx inteiro a partir do CSV bruto do Fiocruz Shiny.
        public async Task ProcessarAsync(
            string csvPath,
            string pathMestre,
            IProgress<string>? progress = null)
        {
            var linhas = await File.ReadAllLinesAsync(csvPath, Encoding.UTF8);
            if (linhas.Length < 2)
            {
                progress?.Report("CSV vazio ou sem linhas de dados.");
                return;
            }

            // Mapear colunas pelo header para não depender de ordem fixa.
            var header = linhas[0].Split(';');
            int idxName  = IndexOf(header, "name_muni");
            int idxDate  = IndexOf(header, "date");
            int idxIqar  = IndexOf(header, "iqar");
            int idxLabel = IndexOf(header, "label");

            var rows       = new List<(string Municipio, string Regional, DateTime Date, double Iqar, string Classificacao, string Qualidade)>();
            int descartados = 0;

            for (int i = 1; i < linhas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(linhas[i])) continue;

                var cols = linhas[i].Split(';');

                string dateStr = Coluna(cols, idxDate);
                if (dateStr.Equals("NA", StringComparison.OrdinalIgnoreCase))
                {
                    descartados++;
                    continue;
                }

                if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime date))
                    continue;

                // Remove sufixo " - RO" do nome do município.
                string nome = Coluna(cols, idxName);
                if (nome.EndsWith(" - RO", StringComparison.OrdinalIgnoreCase))
                    nome = nome[..^5].Trim();

                string chave    = RemoverAcentos(nome.Trim().ToUpperInvariant());
                string regional = RondoniaRegionais.ObterRegional(chave);
                if (regional == "Não identificado") regional = "Sem Registro";

                // IQAr: aceita vírgula ou ponto como separador decimal.
                string iqarStr = Coluna(cols, idxIqar).Replace(',', '.');
                double.TryParse(iqarStr, NumberStyles.Number, CultureInfo.InvariantCulture, out double iqar);

                // Decompõe "N1 - Boa" → Classificação = "N1", Qualidade = "Boa".
                string label = Coluna(cols, idxLabel);
                string classificacao, qualidade;
                int sep = label.IndexOf(" - ", StringComparison.Ordinal);
                if (sep >= 0)
                {
                    classificacao = label[..sep].Trim();
                    qualidade     = label[(sep + 3)..].Trim();
                }
                else
                {
                    classificacao = label;
                    qualidade     = string.Empty;
                }

                rows.Add((nome, regional, date, iqar, classificacao, qualidade));
            }

            // Overwrite total: parte sempre de um pacote em branco.
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var fileInfo = new FileInfo(pathMestre);
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
                var (municipio, regional, date, iqar, classificacao, qualidade) = rows[r];
                int row = r + 2;
                ws.Cells[row, 1].Value = municipio;
                ws.Cells[row, 2].Value = regional;
                ws.Cells[row, 3].Value = date.Date;
                ws.Cells[row, 3].Style.Numberformat.Format = "dd/MM/yyyy";
                ws.Cells[row, 4].Value = date.ToString("HH:mm");
                ws.Cells[row, 5].Value = iqar;
                ws.Cells[row, 6].Value = classificacao;
                ws.Cells[row, 7].Value = qualidade;
            }

            await pkg.SaveAsAsync(fileInfo);

            string resumo = $"Master atualizado: {rows.Count} linha(s) gravada(s).";
            if (descartados > 0)
                resumo += $" {descartados} linha(s) descartada(s) por date=NA.";
            progress?.Report(resumo);
        }

        private static string Coluna(string[] cols, int idx)
            => idx >= 0 && idx < cols.Length ? cols[idx].Trim() : string.Empty;

        private static int IndexOf(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
                if (header[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private static string RemoverAcentos(string texto)
        {
            string norm = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(norm.Length);
            foreach (char c in norm)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
