using System.IO;
using System.Text.Json;

namespace Conversor_de_Arquivos
{
    public class VigiarFocosCalorService
    {
        private const int DELAY_ENTRE_CHAMADAS_MS     = 400;
        private const int ANO_INICIAL_CARGA_HISTORICA = 2021;

        private readonly FocosCalorDataProcessor _processor = new();

        // fetchSemanaJson: delegate fornecido pela view (WebAutomation) que
        // executa o fetch real dentro do Playwright e retorna o JSON bruto.
        public async Task ExecutarAsync(
            string caminhoMaster,
            Func<DateTime, DateTime, Task<string>> fetchSemanaJson,
            IProgress<string> progress)
        {
            // 1. Determine start week
            var lastRecorded = _processor.ReadMaxWeek(caminhoMaster);
            DateTime startDate = lastRecorded.HasValue
                ? EpidemiologicalWeek.WeekStart(lastRecorded.Value.Ano, lastRecorded.Value.Semana).AddDays(7)
                : EpidemiologicalWeek.WeekStart(ANO_INICIAL_CARGA_HISTORICA, 1);

            // 2. Determine end week
            var lastClosed = EpidemiologicalWeek.LastClosedWeek(DateTime.Now);

            // 3. Build full pending list
            var pending = BuildPendingList(startDate, lastClosed);

            if (pending.Count == 0)
            {
                progress.Report("Nenhuma semana pendente. Master já está atualizada.");
                return;
            }

            // 4. Report initial summary
            progress.Report(FormatResumo(pending));

            // 5. Process all pending weeks
            for (int i = 0; i < pending.Count; i++)
            {
                var w = pending[i];
                progress.Report($"Consultando semana {w.Week}/{w.Year} " +
                                $"({w.Start:dd/MM/yyyy} a {w.End:dd/MM/yyyy})...");

                SemanaFocos dados;
                try
                {
                    string json = await fetchSemanaJson(w.Start, w.End);
                    dados = ParseJson(json, w.Year, w.Week, w.Start, w.End);
                }
                catch (Exception ex)
                {
                    progress.Report($"Erro na semana {w.Week}/{w.Year}: {ex.Message}");
                    return;
                }

                await _processor.AppendSemanaAsync(dados, caminhoMaster, progress);
                progress.Report($"Semana {w.Week}/{w.Year} gravada.");

                if (i < pending.Count - 1)
                    await Task.Delay(DELAY_ENTRE_CHAMADAS_MS);
            }

            progress.Report("Coleta concluída. Master está atualizada.");
        }

        private static SemanaFocos ParseJson(
            string json, int ano, int semana, DateTime inicio, DateTime fim)
        {
            using var doc = JsonDocument.Parse(json);
            var focos = new List<FocoCidade>();

            if (doc.RootElement.TryGetProperty("firesCount", out var firesCount) &&
                firesCount.TryGetProperty("rows", out var rows))
            {
                foreach (var row in rows.EnumerateArray())
                {
                    string municipio = row.GetProperty("municipio").GetString() ?? string.Empty;
                    string estado    = row.GetProperty("estado").GetString()    ?? string.Empty;
                    string countStr  = row.GetProperty("count").GetString()     ?? "0";
                    int count = int.TryParse(countStr, out int c) ? c : 0;
                    focos.Add(new FocoCidade(municipio, estado, count));
                }
            }

            return new SemanaFocos(ano, semana, inicio, fim, focos);
        }

        private static List<EpidemiologicalWeek.WeekInfo> BuildPendingList(
            DateTime startDate, EpidemiologicalWeek.WeekInfo lastClosed)
        {
            var list = new List<EpidemiologicalWeek.WeekInfo>();
            var d = startDate;
            while (true)
            {
                var w = EpidemiologicalWeek.Calculate(d);
                if (w.Year > lastClosed.Year || (w.Year == lastClosed.Year && w.Week > lastClosed.Week))
                    break;
                list.Add(w);
                d = d.AddDays(7);
            }
            return list;
        }

        private static string FormatResumo(List<EpidemiologicalWeek.WeekInfo> pending)
        {
            int total = pending.Count;

            if (total <= 10)
            {
                var groups = pending.GroupBy(w => w.Year).ToList();
                if (groups.Count == 1)
                {
                    string nums = string.Join(", ", pending.Select(w => w.Week));
                    return $"{total} semana(s) pendente(s): {nums} de {groups[0].Key}";
                }
                string lista = string.Join(", ", pending.Select(w => $"{w.Week}/{w.Year}"));
                return $"{total} semana(s) pendente(s): {lista}";
            }

            var first = pending.First();
            var last  = pending.Last();
            return $"{total} semana(s) pendente(s): semana {first.Week}/{first.Year} " +
                   $"até semana {last.Week}/{last.Year}.";
        }
    }
}
