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
            var config = new ConfiguracaoService().Carregar();

            var errors = PathVerifier.Verify(
            [
                (config.Vigiar.PastaQueimadas, "Pasta de queimadas não configurada.")
            ]);

            if (errors.Count > 0)
            {
                AppendStatus(string.Join("\n", errors));
                return;
            }

            string caminhoMaster = Path.Combine(config.Vigiar.PastaQueimadas, "FOCOS_CALOR_MESTRE.xlsx");

            BtnStart.IsEnabled = false;
            TxtStatus.Text     = string.Empty;

            var progress = new Progress<string>(msg =>
            {
                TxtStatus.Text += (TxtStatus.Text.Length > 0 ? "\n" : string.Empty) + msg;
                StatusScroll.ScrollToBottom();
            });

            try
            {
                // Playwright abre o browser UMA vez para toda a duração do lote.
                await using var automation = new VigiarFocosCalorAutomation();
                await automation.InicializarAsync(progress);

                // O serviço chama o delegate por semana — fetch() roda dentro do browser real.
                await new VigiarFocosCalorService().ExecutarAsync(
                    caminhoMaster,
                    (inicio, fim) => automation.BuscarSemanaJsonAsync(inicio, fim),
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
