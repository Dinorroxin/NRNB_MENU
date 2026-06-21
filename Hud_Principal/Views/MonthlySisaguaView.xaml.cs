using Modulo_Seguranca;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Hud_Principal.Views
{
    /// <summary>
    /// Interação lógica para MonthlySisaguaView.xaml
    /// </summary>
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
                // Acrescentar depois o resto das paths
            });

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", errors), "Verificação falhou", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // a partir daqui entra o async/await + Task.Run da automação em si
        }
    }
}
