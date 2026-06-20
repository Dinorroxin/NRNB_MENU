using Modulo_Seguranca;
using System.Windows;
using System.Windows.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace Hud_Principal.Views
{
    public partial class ConfigView : BaseView
    {
        public ConfigView()
        {
            InitializeComponent();
            Loaded += ConfigView_Loaded;
        }

        private void ConfigView_Loaded(object sender, RoutedEventArgs e)
        {
            var service = new ConfiguracaoService();

            try
            {
                var config = service.Carregar();

                TxtEmail.Text = config.Vigiagua.Email;
                TxtPassword.Password = config.Vigiagua.Senha;
                TxtRawFolder.Text = config.Vigiagua.PastaArquivosBrutos;
                TxtMonthlyDirectiveFolder.Text = config.Vigiagua.PastaCumprimentoDaDiretrizMensal;
                TxtAnnualDirectiveFolder.Text = config.Vigiagua.PastaCumprimentoDaDiretrizAnual;
                TxtControlFolder.Text = config.Vigiagua.PastaControle;
            }
            catch
            {
                // Se não existir ainda, deixa os campos em branco (comportamento padrão já é esse)
            }
        }





        //                              TESTE DE VALIDAÇÃO, VAI MUDAR FUTURAMENTE
        private List<string> ValidarVigiagua()
        {
            var campos = new List<(string Valor, string Mensagem)>
            {
                (TxtEmail.Text, "Email do Vigiagua não informado"),
                (TxtPassword.Password, "Senha do Vigiagua não informada"),
                (TxtRawFolder.Text, "Pasta de arquivos brutos não informada"),
                (TxtMonthlyDirectiveFolder.Text, "Pasta da Diretriz Mensal não informada"),
                (TxtAnnualDirectiveFolder.Text, "Pasta da Diretriz Anual não informada"),
                (TxtControlFolder.Text, "Pasta de saída do Controle não informada")
            };

            return campos
                .Where(c => string.IsNullOrEmpty(c.Valor))
                .Select(c => c.Mensagem)
                .ToList();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var erros = ValidarVigiagua();

            if (erros.Count > 0)
            {
                MessageBox.Show(string.Join("\n", erros), "Campos pendentes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var service = new ConfiguracaoService();
            Configuracao config;
            try { config = service.Carregar(); }
            catch { config = new Configuracao(); }

            config.Vigiagua.Email = TxtEmail.Text;
            config.Vigiagua.Senha = TxtPassword.Password;
            config.Vigiagua.PastaArquivosBrutos = TxtRawFolder.Text;
            config.Vigiagua.PastaCumprimentoDaDiretrizMensal = TxtMonthlyDirectiveFolder.Text;
            config.Vigiagua.PastaCumprimentoDaDiretrizAnual = TxtAnnualDirectiveFolder.Text;
            config.Vigiagua.PastaControle = TxtControlFolder.Text;

            service.Salvar(config);
            MessageBox.Show("Configurações salvas com sucesso!");
        }









        private void BtnVigiaguaSection_Click(object sender, RoutedEventArgs e)
            => ToggleSection(VigiaguaSection);

        private void ToggleSection(StackPanel section)
        {
            if (section.MaxHeight == 0)
            {
                section.Visibility = Visibility.Visible;
                var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 400, TimeSpan.FromSeconds(0.25));
                section.BeginAnimation(FrameworkElement.MaxHeightProperty, anim);
            }
            else
            {
                var anim = new System.Windows.Media.Animation.DoubleAnimation(400, 0, TimeSpan.FromSeconds(0.2));
                anim.Completed += (s, e) => section.Visibility = Visibility.Collapsed;
                section.BeginAnimation(FrameworkElement.MaxHeightProperty, anim);
            }
        }

        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (TxtPasswordVisible.Visibility == Visibility.Collapsed)
            {
                TxtPasswordVisible.Text = TxtPassword.Password; // copia o que tem no PasswordBox
                TxtPasswordVisible.Visibility = Visibility.Visible;
                TxtPassword.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtPassword.Password = TxtPasswordVisible.Text; // copia de volta
                TxtPassword.Visibility = Visibility.Visible;
                TxtPasswordVisible.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnRawFolderDirective_Click(object sender, RoutedEventArgs e)
            => SelectFolder(TxtRawFolder);

        private void BtnMonthlyDirectiveFolder_Click(object sender, RoutedEventArgs e)
            => SelectFolder(TxtMonthlyDirectiveFolder);

        private void BtnAnnualDirectiveFolder_Click(object sender, RoutedEventArgs e)
            => SelectFolder(TxtAnnualDirectiveFolder);

        private void BtnControlFolder_Click(object sender, RoutedEventArgs e)
            => SelectFolder(TxtControlFolder);

        private void SelectFolder(TextBox field)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                field.Text = dialog.SelectedPath;
        }
    }
}