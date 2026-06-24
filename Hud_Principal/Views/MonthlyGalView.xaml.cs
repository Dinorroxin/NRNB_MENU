using Modulo_Seguranca;
using System.Windows;
using System.Windows.Controls;
using WebAutomation;

namespace Hud_Principal.Views
{
    public partial class MonthlyGalView : BaseView
    {
        public MonthlyGalView()
        {
            InitializeComponent();
            TxtDataInicio.Text = $"01/01/{DateTime.Now.Year}";
            TxtDataFim.Text = DateTime.Now.ToString("dd/MM/yyyy");
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // Validação básica
            if (string.IsNullOrWhiteSpace(TxtDataInicio.Text) ||
                string.IsNullOrWhiteSpace(TxtDataFim.Text) ||
                string.IsNullOrWhiteSpace(TxtIbge.Text))
            {
                TxtStatus.Text = "Preencha todos os campos antes de iniciar.";
                return;
            }

            var config = new ConfiguracaoService().Carregar();

            var errors = new List<(string Path, string Message)>();
            if (string.IsNullOrWhiteSpace(config.Gal.Usuario))
                errors.Add(("", "Usuário do GAL não configurado."));
            if (string.IsNullOrWhiteSpace(config.Gal.Senha))
                errors.Add(("", "Senha do GAL não configurada."));
            if (string.IsNullOrWhiteSpace(config.Gal.PastaBrutaRelaotriosMensalGal))
                errors.Add(("", "Pasta de download do GAL não configurada."));

            if (errors.Count > 0)
            {
                TxtStatus.Text = string.Join("\n", errors.Select(e => e.Message));
                return;
            }

            BtnStart.IsEnabled = false;

            var referencia = ((ComboBoxItem)CmbReferencia.SelectedItem).Content.ToString()!;
            var progress = new Progress<string>(msg => TxtStatus.Text = msg);

            bool sucesso = await Task.Run(() =>
                new GalMonthlyAutomation().BaixarRelatorioMensalAsync(
                    config.Gal.Usuario,
                    config.Gal.Senha,
                    config.Gal.Modulo,
                    config.Gal.Laboratorio,
                    config.Gal.PastaBrutaRelaotriosMensalGal,
                    referencia,
                    TxtDataInicio.Text,
                    TxtDataFim.Text,
                    TxtIbge.Text,
                    progress));

            TxtStatus.Text = sucesso
                ? "Relatório salvo com sucesso."
                : "Falha na coleta. Verifique o status acima.";

            BtnStart.IsEnabled = true;
        }
    }
}