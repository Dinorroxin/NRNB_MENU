using Conversor_de_Arquivos;
using Modulo_Seguranca;
using System.IO;
using System.Windows;
using WebAutomation;

namespace Hud_Principal.Views
{
    public partial class VigiarIqArView : BaseView
    {
        public VigiarIqArView()
        {
            InitializeComponent();
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var config = new ConfigurationService().Load();

            var errors = PathVerifier.Verify(
            [
                (config.Vigiar.RawFilesFolder, "Pasta de arquivos brutos (Vigiar) não configurada."),
                (config.Vigiar.IqArFolder,     "Pasta IQAr não configurada.")
            ]);

            if (errors.Count > 0)
            {
                AppendStatus(string.Join("\n", errors));
                return;
            }

            BtnStart.IsEnabled = false;
            TxtStatus.Text = string.Empty;

            string logPath = Path.Combine(Path.GetTempPath(), "nrnb_vigiar_iqar_debug.log");
            File.WriteAllText(logPath, string.Empty);

            IProgress<string> progress = new Progress<string>(msg =>
            {
                TxtStatus.Text += (TxtStatus.Text.Length > 0 ? "\n" : string.Empty) + msg;
                StatusScroll.ScrollToBottom();
                File.AppendAllText(logPath, msg + "\n");
            });

            string masterPath = Path.Combine(config.Vigiar.IqArFolder, "IQAR_MESTRE.xlsx");

            try
            {
                await using var automation = new IqArAutomation();
                string csvPath = await automation.ExecuteAsync(config.Vigiar.RawFilesFolder, progress);

                await new IqArDataProcessor().ProcessAsync(csvPath, masterPath, progress);
            }
            catch (Exception ex)
            {
                string detalhe = $"Erro inesperado: {ex.GetType().Name}: {ex.Message}";
                AppendStatus(detalhe);
                File.AppendAllText(logPath, detalhe + "\n" + ex.StackTrace + "\n");
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