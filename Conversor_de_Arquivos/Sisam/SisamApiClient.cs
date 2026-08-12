using System.Net.Http;
using System.Text.Json;
using Conversor_de_Arquivos.Models;

namespace Conversor_de_Arquivos.Sisam
{
    public class SisamApiClient
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "https://data.inpe.br/queimadas/sisam/api/pollution";

        public SisamApiClient(HttpClient http) => _http = http;

        public async Task<List<SisamRecord>> GetPollutionAsync(
            SisamPoluente poluente, DateOnly dataBase, int forecastHours)
        {
            string url = $"{BaseUrl}/{poluente.ToUrlSegment()}?data={dataBase:yyyy-MM-dd}&forecast=forecast{forecastHours}";

            using var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            var faixas = ExtractFaixas(root);

            var dataProp = root.EnumerateObject().FirstOrDefault(p => p.Name.EndsWith("_data"));
            if (dataProp.Value.ValueKind == JsonValueKind.Undefined)
                return [];

            var dataEl        = dataProp.Value;
            var records       = new List<SisamRecord>();
            string dataStr    = dataBase.ToString("yyyy-MM-dd");
            int    diasPrevis = forecastHours / 24;
            string polNome    = poluente.ToDisplayName();

            if (dataEl.TryGetProperty("estados", out var estadosEl))
                foreach (var est in estadosEl.EnumerateArray())
                    AddRecord(records, est, null, dataStr, polNome, diasPrevis, faixas);

            if (dataEl.TryGetProperty("municipios", out var municipiosEl))
                foreach (var mun in municipiosEl.EnumerateArray())
                {
                    string? municipio = mun.TryGetProperty("municipio", out var munEl)
                        ? munEl.GetString()
                        : null;
                    AddRecord(records, mun, municipio, dataStr, polNome, diasPrevis, faixas);
                }

            return records;
        }

        private static void AddRecord(
            List<SisamRecord> records,
            JsonElement element,
            string? municipio,
            string dataStr,
            string polNome,
            int diasPrevisao,
            List<SisamFaixa> faixas)
        {
            string estado = element.TryGetProperty("estado", out var estEl)
                ? estEl.GetString() ?? string.Empty
                : string.Empty;

            var forecastProp = element.EnumerateObject()
                .FirstOrDefault(p => p.Name.Contains("_forecast_"));

            if (forecastProp.Value.ValueKind == JsonValueKind.Undefined) return;

            double valor = forecastProp.Value.ValueKind == JsonValueKind.Number
                ? forecastProp.Value.GetDouble()
                : 0;

            var (nome, desc) = Classificar(valor, faixas);
            records.Add(new SisamRecord(dataStr, estado, municipio, valor, nome, desc, polNome, diasPrevisao));
        }

        private static List<SisamFaixa> ExtractFaixas(JsonElement root)
        {
            var faixas = new List<SisamFaixa>();

            if (!root.TryGetProperty("faixas", out var outer)) return faixas;
            if (!outer.TryGetProperty("faixas", out var arr))  return faixas;

            foreach (var f in arr.EnumerateArray())
            {
                double min = f.TryGetProperty("concentracao_min", out var minEl) && minEl.ValueKind == JsonValueKind.Number
                    ? minEl.GetDouble() : 0;
                double max = f.TryGetProperty("concentracao_max", out var maxEl) && maxEl.ValueKind == JsonValueKind.Number
                    ? maxEl.GetDouble() : double.MaxValue;

                string nome = string.Empty, desc = string.Empty, cor = string.Empty;
                if (f.TryGetProperty("nivel", out var nivel))
                {
                    if (nivel.TryGetProperty("nome",       out var nEl)) nome = nEl.GetString() ?? string.Empty;
                    if (nivel.TryGetProperty("descricao",  out var dEl)) desc = dEl.GetString() ?? string.Empty;
                    if (nivel.TryGetProperty("codigo_cor", out var cEl)) cor  = cEl.GetString() ?? string.Empty;
                }

                faixas.Add(new SisamFaixa(min, max, new SisamNivel(nome, desc, cor)));
            }

            return faixas;
        }

        private static (string Nome, string Descricao) Classificar(double valor, List<SisamFaixa> faixas)
        {
            foreach (var f in faixas)
                if (valor >= f.Min && valor <= f.Max)
                    return (f.Nivel.Nome, f.Nivel.Descricao);

            var worst = faixas.OrderByDescending(f => f.Max).FirstOrDefault();
            return worst is null ? (string.Empty, string.Empty) : (worst.Nivel.Nome, worst.Nivel.Descricao);
        }
    }
}
