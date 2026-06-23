using Conversor_de_Arquivos;
using Modulo_Seguranca;
using System.IO;
using System.Linq;
using System.Windows;
using WebAutomation;
using MessageBox = System.Windows.MessageBox;

namespace Hud_Principal.Views
{
    public partial class AnnualSisaguaView : BaseView
    {
        public AnnualSisaguaView()
        {
            InitializeComponent();

            for (int ano = 2014; ano <= DateTime.Now.Year; ano++)
            {
                CmbAnoInicial.Items.Add(ano);
                CmbAnoFinal.Items.Add(ano);
            }

            CmbAnoInicial.SelectedItem = 2014;
            CmbAnoFinal.SelectedItem = DateTime.Now.Year;
        }

        private async void BtnStartAnnual_Click(object sender, RoutedEventArgs e)
        {
            if (CmbAnoInicial.SelectedItem is not int anoInicial ||
                CmbAnoFinal.SelectedItem is not int anoFinal)
            {
                MessageBox.Show("Selecione os anos.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (anoInicial > anoFinal)
            {
                MessageBox.Show("Ano inicial não pode ser maior que o ano final.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var config = new ConfiguracaoService().Carregar();

            var errors = PathVerifier.Verify(new List<(string, string)>
            {
                (config.Vigiagua.PastaArquivosBrutos, "Pasta de arquivos brutos não informada"),
                (config.Vigiagua.PastaCumprimentoDaDiretrizAnual, "Pasta da Diretriz Anual não informada"),
            });

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", errors), "Verificação falhou",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StartAnnualSisagua.IsEnabled = false;
            StartAnnualSisagua.Content = "Executando...";
            TxtStatus.Text = "";

            var progress = new Progress<string>(msg => TxtStatus.Text = msg);
            var automation = new SisaguaDiretrizAnualAutomation();

            var result = await automation.BaixarRelatoriosAnuaisAsync(
                config.Vigiagua.Email,
                config.Vigiagua.Senha,
                config.Vigiagua.PastaArquivosBrutos,
                anoInicial,
                anoFinal,
                progress);

            if (!result.Success)
            {
                TxtStatus.Text = "Erro na automação.";
                MessageBox.Show($"Erro:\n{result.ErrorMessage}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StartAnnualSisagua.IsEnabled = true;
                StartAnnualSisagua.Content = "Iniciar Processo";
                return;
            }

            var processor = new SisaguaAnualDataProcessor();
            var erros = new List<string>();

            foreach (var pathBruto in result.DownloadedFiles)
            {
                string nomeMestre = Path.GetFileNameWithoutExtension(pathBruto)
                    .Split('-')[1].Trim() + "_ANUAL_MESTRE.xlsx";
                string pathMestre = Path.Combine(
                    config.Vigiagua.PastaCumprimentoDaDiretrizAnual, nomeMestre);

                var proc = await processor.ProcessarAsync(pathBruto, pathMestre, progress);

                if (!proc.Success)
                    erros.Add($"{proc.Parametro}: {proc.ErrorMessage}");
            }

            StartAnnualSisagua.IsEnabled = true;
            StartAnnualSisagua.Content = "Iniciar Processo";

            if (erros.Count > 0)
            {
                TxtStatus.Text = "Concluído com erros.";
                MessageBox.Show(string.Join("\n", erros), "Erros",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                TxtStatus.Text = "Concluído com sucesso!";
            }
        }
    }
}