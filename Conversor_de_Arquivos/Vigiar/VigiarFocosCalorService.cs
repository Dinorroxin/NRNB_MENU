using System.IO;
using System.Text.Json;

namespace Conversor_de_Arquivos
{
    public class VigiarFocosCalorService
    {
        private const int DelayBetweenCallsMs = 400;

        private readonly FocosCalorDataProcessor _processor = new();

        public async Task ExecuteAsync(
            string masterPath,
            List<int> years,
            Func<DateTime, DateTime, Task<string>> fetchWeekJson,
            IProgress<string> progress)
        {
            var lastClosed = EpidemiologicalWeek.LastClosedWeek(DateTime.Now);
            var pending = BuildPendingListForYears(years, lastClosed);

            if (pending.Count == 0)
            {
                progress.Report("Nenhuma semana pendente. Master já está atualizada.");
                return;
            }

            progress.Report(FormatSummary(pending));

            for (int i = 0; i < pending.Count; i++)
            {
                var w = pending[i];
                progress.Report($"Consultando semana {w.Week}/{w.Year} " +
                                $"({w.Start:dd/MM/yyyy} a {w.End:dd/MM/yyyy})...");

                WeekHotspots data;
                try
                {
                    string json = await fetchWeekJson(w.Start, w.End);
                    data = ParseJson(json, w.Year, w.Week, w.Start, w.End);
                }
                catch (Exception ex)
                {
                    progress.Report($"Erro na semana {w.Week}/{w.Year}: {ex.Message}");
                    return;
                }

                await _processor.AppendWeekAsync(data, masterPath, progress);
                progress.Report($"Semana {w.Week}/{w.Year} gravada.");

                if (i < pending.Count - 1)
                    await Task.Delay(DelayBetweenCallsMs);
            }

            progress.Report("Coleta concluída. Master está atualizada.");
        }

        private static WeekHotspots ParseJson(
            string json, int year, int week, DateTime start, DateTime end)
        {
            using var doc = JsonDocument.Parse(json);
            var hotspots = new List<CityHotspot>();

            if (doc.RootElement.TryGetProperty("firesCount", out var firesCount) &&
                firesCount.TryGetProperty("rows", out var rows))
            {
                foreach (var row in rows.EnumerateArray())
                {
                    string municipality = row.GetProperty("municipio").GetString() ?? string.Empty;
                    string state        = row.GetProperty("estado").GetString()    ?? string.Empty;
                    string countStr     = row.GetProperty("count").GetString()     ?? "0";
                    int count = int.TryParse(countStr, out int c) ? c : 0;
                    hotspots.Add(new CityHotspot(municipality, state, count));
                }
            }

            return new WeekHotspots(year, week, start, end, hotspots);
        }

        private static List<EpidemiologicalWeek.WeekInfo> BuildPendingListForYears(
            List<int> years, EpidemiologicalWeek.WeekInfo lastClosed)
        {
            var list = new List<EpidemiologicalWeek.WeekInfo>();
            foreach (int year in years.OrderBy(y => y))
            {
                var d = EpidemiologicalWeek.WeekStart(year, 1);
                while (true)
                {
                    var w = EpidemiologicalWeek.Calculate(d);
                    if (w.Year > year) break;
                    if (w.Year > lastClosed.Year || (w.Year == lastClosed.Year && w.Week > lastClosed.Week)) break;
                    list.Add(w);
                    d = d.AddDays(7);
                }
            }
            return list;
        }

        private static string FormatSummary(List<EpidemiologicalWeek.WeekInfo> pending)
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
                string list = string.Join(", ", pending.Select(w => $"{w.Week}/{w.Year}"));
                return $"{total} semana(s) pendente(s): {list}";
            }

            var first = pending.First();
            var last  = pending.Last();
            return $"{total} semana(s) pendente(s): semana {first.Week}/{first.Year} " +
                   $"até semana {last.Week}/{last.Year}.";
        }
    }
}
