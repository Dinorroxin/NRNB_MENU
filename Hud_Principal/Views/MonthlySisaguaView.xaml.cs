using Conversor_de_Arquivos;
using Modulo_Seguranca;
using System.Windows;
using System.Windows.Controls;
using WebAutomation;
using System.IO;
using MessageBox = System.Windows.MessageBox;

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
        (config.Vigiagua.PastaArquivosBrutos, "Pasta de arquivos brutos não informada"),
        (config.Vigiagua.PastaCumprimentoDaDiretrizMensal, "Pasta da Diretriz Mensal não informada"),
    });

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", errors), "Verificação falhou",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StartMonthlySisagua.IsEnabled = false;
            StartMonthlySisagua.Content = "Executando...";
            TxtStatus.Text = "";

            var progress = new Progress<string>(msg => TxtStatus.Text = msg);

            // Automação
            //Gravidade	Código	Descrição	Projeto	Arquivo	Linha	Estado de Supressão
            //Erro(ativo)    CS1501 Nenhuma sobrecarga para o método "BaixarRelatoriosMensaisAsync" leva 4 argumentos Hud_Principal   D:\NRNB_MENU\Hud_Principal\Views\MonthlySisaguaView.xaml.cs 44
            // Está retornando esse erro, darei commit porque já está tarde, amanhã continuamos

            var automation = new SisaguaDiretrizAutomation();
            var result = await automation.BaixarRelatoriosMensaisAsync(
                config.Vigiagua.Email,
                config.Vigiagua.Senha,
                config.Vigiagua.PastaArquivosBrutos,
                progress);

            if (!result.Success)
            {
                TxtStatus.Text = "Erro na automação.";
                MessageBox.Show($"Erro:\n{result.ErrorMessage}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StartMonthlySisagua.IsEnabled = true;
                StartMonthlySisagua.Content = "Iniciar Processo";
                return;
            }

            // Processamento
            var processor = new SisaguaDataProcessor();
            var errosProcessamento = new List<string>();

            foreach (var pathBruto in result.DownloadedFiles)
            {
                string nomeMestre = Path.GetFileNameWithoutExtension(pathBruto) + "_MESTRE.xlsx";
                string pathMestre = Path.Combine(
                    config.Vigiagua.PastaCumprimentoDaDiretrizMensal, nomeMestre);

                var proc = await processor.ProcessarAsync(pathBruto, pathMestre, progress);

                if (!proc.Success)
                    errosProcessamento.Add($"{proc.Parametro}: {proc.ErrorMessage}");
            }

            StartMonthlySisagua.IsEnabled = true;
            StartMonthlySisagua.Content = "Iniciar Processo";

            if (errosProcessamento.Count > 0)
            {
                TxtStatus.Text = "Concluído com erros.";
                MessageBox.Show(string.Join("\n", errosProcessamento), "Erros no processamento",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                TxtStatus.Text = "Concluído com sucesso!";
            }
        }
    }
}