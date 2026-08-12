using Conversor_de_Arquivos.Models;
using Conversor_de_Arquivos.Sisam;
using Modulo_Seguranca;
using System.IO;
using System.Net.Http;
using System.Windows;

namespace Hud_Principal.Views
{
    public partial class SisamPrevisaoView : BaseView
    {
        private static readonly HttpClient    _http        = new();
        private static readonly SisamApiClient _sisamClient = new(_http);

        public SisamPrevisaoView()
        {
            InitializeComponent();
        }

        private List<SisamPoluente> ObterPoluentesMarcados()
        {
            var lista = new List<SisamPoluente>();
            if (ChkPm25.IsChecked == true) lista.Add(SisamPoluente.Pm25);
            if (ChkPm10.IsChecked == true) lista.Add(SisamPoluente.Pm10);
            if (ChkO3.IsChecked   == true) lista.Add(SisamPoluente.O3);
            if (ChkNo2.IsChecked  == true) lista.Add(SisamPoluente.No2);
            if (ChkSo2.IsChecked  == true) lista.Add(SisamPoluente.So2);
            if (ChkCo.IsChecked   == true) lista.Add(SisamPoluente.Co);
            return lista;
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var config = new ConfigurationService().Load();

            var errors = PathVerifier.Verify(
            [
                (config.Sisam.PastaSisamMestre, "Pasta do arquivo mestre SISAM não configurada.")
            ]);

            if (errors.Count > 0)
            {
                AppendStatus(string.Join("\n", errors));
                return;
            }

            var poluentesSelecionados = ObterPoluentesMarcados();
            if (poluentesSelecionados.Count == 0)
            {
                AppendStatus("Selecione ao menos um poluente.");
                return;
            }

            BtnStart.IsEnabled = false;
            TxtStatus.Text     = string.Empty;
            TxtProgresso.Visibility = Visibility.Visible;

            string caminhoMestre = Path.Combine(config.Sisam.PastaSisamMestre, "SISAM_MESTRE.xlsx");
            string logPath       = Path.Combine(Path.GetTempPath(), "nrnb_sisam_debug.log");
            File.WriteAllText(logPath, string.Empty);

            IProgress<string> progress = new Progress<string>(msg =>
            {
                TxtStatus.Text += (TxtStatus.Text.Length > 0 ? "\n" : string.Empty) + msg;
                StatusScroll.ScrollToBottom();
                File.AppendAllText(logPath, msg + "\n");
            });

            var dataBase      = DateOnly.FromDateTime(DateTime.Today);
            var horasForecast = new[] { 0, 24, 48, 72, 96 };
            var falhas        = new List<string>();
            var processor     = new SisamPrevisaoDataProcessor();

            for (int i = 0; i < poluentesSelecionados.Count; i++)
            {
                var poluente = poluentesSelecionados[i];
                TxtProgresso.Text = $"Poluente {i + 1} de {poluentesSelecionados.Count}: {poluente.ToDisplayName()}";

                try
                {
                    progress.Report($"Processando {poluente.ToDisplayName()}...");
                    var registrosPoluente = new List<SisamRecord>();

                    foreach (var horas in horasForecast)
                    {
                        progress.Report($"  {poluente.ToDisplayName()} — previsão +{horas / 24}d...");
                        var registros = await _sisamClient.GetPollutionAsync(poluente, dataBase, horas);
                        registrosPoluente.AddRange(registros);
                        await Task.Delay(300);
                    }

                    processor.SalvarRegistros(registrosPoluente, caminhoMestre);
                    progress.Report($"{poluente.ToDisplayName()} salvo com sucesso.");
                }
                catch (Exception ex)
                {
                    string msg = $"FALHA em {poluente.ToDisplayName()}: {ex.GetType().Name} — {ex.Message}";
                    progress.Report(msg);
                    File.AppendAllText(logPath, msg + "\n" + ex.StackTrace + "\n");
                    falhas.Add(poluente.ToDisplayName());
                }
            }

            TxtProgresso.Visibility = Visibility.Collapsed;

            if (falhas.Count > 0)
                progress.Report($"Concluído com falhas em: {string.Join(", ", falhas)}. Demais poluentes já salvos.");
            else
                progress.Report("Todos os poluentes processados com sucesso.");

            BtnStart.IsEnabled = true;
        }

        private void AppendStatus(string msg)
        {
            TxtStatus.Text += (TxtStatus.Text.Length > 0 ? "\n" : string.Empty) + msg;
            StatusScroll.ScrollToBottom();
        }
    }
}
