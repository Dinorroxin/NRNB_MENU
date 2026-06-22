// MonthlySisaguaView.xaml.cs
using Modulo_Seguranca;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using WebAutomation;

namespace Hud_Principal.Views
{
    public partial class MonthlySisaguaView : BaseView
    {
        public MonthlySisaguaView()
        {
            InitializeComponent();
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var config = new ConfiguracaoService().Carregar();

            var errors = PathVerifier.Verify(new List<(string, string)>
            {
                (config.Vigiagua.PastaArquivosBrutos, "Pasta de arquivos brutos do Vigiagua não informada"),
                (config.Vigiagua.PastaCumprimentoDaDiretrizMensal, "Pasta da Diretriz Mensal não informada"),
            });

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", errors), "Verificação falhou", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Desabilita botão durante execução
            StartMonthlySisagua.IsEnabled = false;
            StartMonthlySisagua.Content = "Executando...";

            var automation = new SisaguaDiretrizAutomation();

            var result = await Task.Run(() => automation.BaixarRelatoriosMensaisAsync(
                config.Vigiagua.Email,
                config.Vigiagua.Senha,
                config.Vigiagua.PastaArquivosBrutos
            ));

            StartMonthlySisagua.IsEnabled = true;
            StartMonthlySisagua.Content = "Iniciar Processo";

            if (result.Success)
            {
                string arquivos = string.Join("\n", result.DownloadedFiles);
                MessageBox.Show($"Downloads concluídos:\n{arquivos}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Erro durante a automação:\n{result.ErrorMessage}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}