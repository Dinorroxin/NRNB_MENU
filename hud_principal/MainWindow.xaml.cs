using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

using Conversor_de_Arquivos;

namespace Hud_Principal
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Selecionar Arquivo de Entrada
        private void BtnSelecionarEntrada_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Documentos Word (*.doc;*.docx)|*.doc;*.docx|Todos os arquivos (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                TxtEntrada.Text = openFileDialog.FileName;
            }
        }

        // Selecionar Pasta de Saída
        private void BtnSelecionarSaida_Click(object sender, RoutedEventArgs e)
        {
            // O WPF puro é chato com pastas, então usamos essa manha:
            var dialog = new OpenFolderDialog(); // Disponível no .NET moderno
            dialog.Title = "Selecione a pasta de destino";

            if (dialog.ShowDialog() == true)
            {
                TxtSaida.Text = dialog.FolderName;
            }
        }

        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtEntrada.Text) || string.IsNullOrEmpty(TxtSaida.Text))
            {
                MessageBox.Show("Por favor, selecione a entrada e a saída!");
                return;
            }

            // Aqui chamamos o seu identificador que já criamos
            Identificador iden = new Identificador();
            string msg = iden.VerificarTipo(TxtEntrada.Text);

            MessageBox.Show($"{msg}\nDestino salvo em: {TxtSaida.Text}");
        }
    }
}