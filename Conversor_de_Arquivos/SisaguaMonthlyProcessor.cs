using System.Data;
using ExcelDataReader;
using OfficeOpenXml;

namespace Conversor_de_Arquivos
{
    public class ProcessamentoResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int RowsAdded { get; set; }
        public string Parametro { get; set; } = string.Empty;
    }

    public class SisaguaDataProcessor
    {
        private static readonly string[] Meses =
            ["JAN", "FEV", "MAR", "ABR", "MAI", "JUN",
             "JUL", "AGO", "SET", "OUT", "NOV", "DEZ"];

        private static readonly Dictionary<string, string> RegionalMap = new()
        {
            // Madeira Mamoré
            ["GUAJARA-MIRIM"] = "Madeira Mamoré",
            ["NOVA MAMORE"] = "Madeira Mamoré",
            ["PORTO VELHO"] = "Madeira Mamoré",
            ["ITAPUA DO OESTE"] = "Madeira Mamoré",
            ["CANDEIAS DO JAMARI"] = "Madeira Mamoré",
            // Vale do Jamari
            ["CAMPO NOVO DE RONDONIA"] = "Vale do Jamari",
            ["BURITIS"] = "Vale do Jamari",
            ["MONTE NEGRO"] = "Vale do Jamari",
            ["CACAULANDIA"] = "Vale do Jamari",
            ["ARIQUEMES"] = "Vale do Jamari",
            ["ALTO PARAISO"] = "Vale do Jamari",
            ["RIO CRESPO"] = "Vale do Jamari",
            ["CUJUBIM"] = "Vale do Jamari",
            ["MACHADINHO D'OESTE"] = "Vale do Jamari",
            // Central
            ["SAO MIGUEL DO GUAPORE"] = "Central",
            ["ALVORADA D'OESTE"] = "Central",
            ["GOVERNADOR JORGE TEIXEIRA"] = "Central",
            ["JARU"] = "Central",
            ["THEOBROMA"] = "Central",
            ["PRESIDENTE MEDICI"] = "Central",
            ["OURO PRETO DO OESTE"] = "Central",
            ["JI-PARANA"] = "Central",
            ["VALE DO ANARI"] = "Central",
            ["VALE DO PARAISO"] = "Central",
            ["NOVA UNIAO"] = "Central",
            ["TEIXEIROPOLIS"] = "Central",
            ["URUPA"] = "Central",
            ["MIRANTE DA SERRA"] = "Central",
            // Zona da Mata
            ["ALTA FLORESTA D'OESTE"] = "Zona da Mata",
            ["ALTO ALEGRE DOS PARECIS"] = "Zona da Mata",
            ["SANTA LUZIA D'OESTE"] = "Zona da Mata",
            ["PARECIS"] = "Zona da Mata",
            ["ROLIM DE MOURA"] = "Zona da Mata",
            ["CASTANHEIRAS"] = "Zona da Mata",
            ["NOVO HORIZONTE DO OESTE"] = "Zona da Mata",
            ["NOVA BRASILANDIA D'OESTE"] = "Zona da Mata",
            // Café
            ["MINISTRO ANDREAZZA"] = "Café",
            ["SAO FELIPE D'OESTE"] = "Café",
            ["PRIMAVERA DE RONDONIA"] = "Café",
            ["CACOAL"] = "Café",
            ["PIMENTA BUENO"] = "Café",
            ["ESPIGAO D'OESTE"] = "Café",
            // Cone Sul
            ["PIMENTEIRAS DO OESTE"] = "Cone Sul",
            ["CABIXI"] = "Cone Sul",
            ["CEREJEIRAS"] = "Cone Sul",
            ["CORUMBIARA"] = "Cone Sul",
            ["COLORADO DO OESTE"] = "Cone Sul",
            ["CHUPINGUAIA"] = "Cone Sul",
            ["VILHENA"] = "Cone Sul",
            // Vale do Guaporé
            ["COSTA MARQUES"] = "Vale do Guaporé",
            ["SERINGUEIRAS"] = "Vale do Guaporé",
            ["SAO FRANCISCO DO GUAPORE"] = "Vale do Guaporé",
        };

        public async Task<ProcessamentoResult> ProcessarAsync(
            string pathArquivoBruto,
            string pathMestre,
            IProgress<string>? progress = null)
        {
            var result = new ProcessamentoResult();

            try
            {
                progress?.Report("Lendo arquivo bruto...");
                System.Text.Encoding.RegisterProvider(
                    System.Text.CodePagesEncodingProvider.Instance);

                DataSet ds;
                using (var stream = File.Open(pathArquivoBruto,
                           FileMode.Open, FileAccess.Read))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                    ds = reader.AsDataSet();

                var sheet = ds.Tables[0];

                // Metadados
                string ano = sheet.Rows[2][1]?.ToString()
                                   ?? DateTime.Now.Year.ToString();
                string parametro = sheet.Rows[4][1]?.ToString()
                                   ?? string.Empty;
                result.Parametro = parametro;

                progress?.Report($"Processando {parametro} ({ano})...");

                // Monta linhas long-format
                var rows = new List<string[]>();

                for (int i = 0; i < 52; i++)
                {
                    var rowPerc = sheet.Rows[8 + i]; // Tabela 1 (%)
                    var rowN = sheet.Rows[63 + i]; // Tabela 2 (N)

                    string municipio = rowPerc[0]?.ToString() ?? string.Empty;
                    string codIbge = rowPerc[1]?.ToString() ?? string.Empty;
                    string populacao = rowPerc[2]?.ToString() ?? string.Empty;
                    string regional = RegionalMap.TryGetValue(municipio, out var reg)
                                       ? reg : "Não mapeado";

                    for (int m = 0; m < 12; m++)
                    {
                        string rawPerc = rowPerc[5 + m]?.ToString() ?? string.Empty;
                        string rawN = rowN[5 + m]?.ToString() ?? string.Empty;

                        // Mês futuro — ambos nulos — pula
                        bool semDados = string.IsNullOrWhiteSpace(rawPerc)
                                     && string.IsNullOrWhiteSpace(rawN);
                        if (semDados) continue;

                        rows.Add([
                            municipio,
                            codIbge,
                            populacao,
                            regional,
                            ano,
                            Meses[m],
                            LimparPercentual(rawPerc), // "%" → " ", "-" → "0"
                            LimparN(rawN),             // "-" → "0"
                            parametro
                        ]);
                    }
                }

                // Atualiza master
                progress?.Report("Atualizando planilha mestre...");
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage package;
                ExcelWorksheet ws;

                if (File.Exists(pathMestre))
                {
                    package = new ExcelPackage(new FileInfo(pathMestre));
                    ws = package.Workbook.Worksheets[0];

                    // Remove linhas do ano atual (de trás pra frente)
                    int lastRow = ws.Dimension?.End.Row ?? 1;
                    for (int r = lastRow; r >= 2; r--)
                    {
                        if (ws.Cells[r, 5].Text == ano)
                            ws.DeleteRow(r);
                    }
                }
                else
                {
                    package = new ExcelPackage();
                    ws = package.Workbook.Worksheets.Add("Dados");

                    string[] headers =
                        ["Município", "CodIBGE", "Populacao", "Regional",
                         "Ano", "Mes", "Percentual", "N", "Parametro"];
                    for (int c = 0; c < headers.Length; c++)
                        ws.Cells[1, c + 1].Value = headers[c];
                }

                // Append
                int nextRow = (ws.Dimension?.End.Row ?? 1) + 1;
                foreach (var row in rows)
                {
                    for (int c = 0; c < row.Length; c++)
                        ws.Cells[nextRow, c + 1].Value = row[c];
                    nextRow++;
                }

                await package.SaveAsAsync(new FileInfo(pathMestre));
                package.Dispose();

                result.RowsAdded = rows.Count;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private static string LimparPercentual(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Trim() == "-") return "0";
            return raw.Replace("%", " ").Trim();
        }

        private static string LimparN(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Trim() == "-") return "0";
            return raw.Trim();
        }
    }
}