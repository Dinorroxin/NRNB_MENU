using Conversor_de_Arquivos;
using Modulo_Seguranca;
using System.IO;
using System.Windows;
using WebAutomation;

namespace Hud_Principal.Views
{
    public partial class VigiarFocosCalorView : BaseView
    {
        public VigiarFocosCalorView()
        {
            InitializeComponent();
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var config = new ConfigurationService().Load();

            var errors = PathVerifier.Verify(
            [
                (config.Vigiar.WildfiresFolder, "Pasta de queimadas não configurada.")
            ]);

            if (errors.Count > 0)
            {
                AppendStatus(string.Join("\n", errors));
                return;
            }

            string masterPath = Path.Combine(config.Vigiar.WildfiresFolder, "FOCOS_CALOR_MESTRE.xlsx");

            BtnStart.IsEnabled = false;
            TxtStatus.Text     = string.Empty;

            string logPath = Path.Combine(Path.GetTempPath(), "nrnb_vigiar_debug.log");
            File.WriteAllText(logPath, string.Empty);

            var progress = new Progress<string>(msg =>
            {
                TxtStatus.Text += (TxtStatus.Text.Length > 0 ? "\n" : string.Empty) + msg;
                StatusScroll.ScrollToBottom();
                File.AppendAllText(logPath, msg + "\n");
            });

            try
            {
                await using var automation = new VigiarFocosCalorAutomation();
                await automation.InitializeAsync(progress);

                await new VigiarFocosCalorService().ExecuteAsync(
                    masterPath,
                    (start, end) => automation.FetchWeekJsonAsync(start, end),
                    progress);
            }
            catch (Exception ex)
            {
                AppendStatus($"Erro inesperado: {ex.Message}");
            }
            finally
            {
                BtnStart.IsEnabled = true;
            }
        }

        private void AppendStatus(string msg)
        {
            TxtStatus.Text += (TxtStatus.Text.Length > 0 ? "\n" : string.Empty) + msg;
            StatusScroll.ScrollToBottom();
        }
    }
}
