using Modulo_Seguranca;
using System.Windows;
using System.Windows.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

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

                TxtVigiarPastaQueimadas.Text     = config.Vigiar.PastaQueimadas;
                TxtVigiarPastaArquivosBrutos.Text = config.Vigiar.PastaArquivosBrutos;
                TxtVigiarPastaIqAr.Text           = config.Vigiar.PastaIqAr;

                TxtGalUsuario.Text = config.Gal.Usuario;
                TxtGalSenha.Password = config.Gal.Senha;
                TxtGalModulo.Text = config.Gal.Modulo;
                TxtGalLaboratorio.Text = config.Gal.Laboratorio;
                TxtGalPastaBrutaRelatoriosMensal.Text = config.Gal.PastaBrutaRelatoriosMensalGal;
                TxtGalPastaRelatoriosMensal.Text = config.Gal.PastaRelatoriosMensalGal;
            }
            catch
            {
                // Se não existir ainda, deixa os campos em branco (comportamento padrão já é esse)
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
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

            config.Vigiar.PastaQueimadas      = TxtVigiarPastaQueimadas.Text;
            config.Vigiar.PastaArquivosBrutos = TxtVigiarPastaArquivosBrutos.Text;
            config.Vigiar.PastaIqAr           = TxtVigiarPastaIqAr.Text;

            config.Gal.Usuario = TxtGalUsuario.Text;
            config.Gal.Senha = TxtGalSenha.Password;
            config.Gal.Modulo = TxtGalModulo.Text;
            config.Gal.Laboratorio = TxtGalLaboratorio.Text;
            config.Gal.PastaBrutaRelatoriosMensalGal = TxtGalPastaBrutaRelatoriosMensal.Text;
            config.Gal.PastaRelatoriosMensalGal = TxtGalPastaRelatoriosMensal.Text;

            service.Salvar(config);
            MessageBox.Show("Configurações salvas com sucesso!");
        }

        private void BtnVigiaguaSection_Click(object sender, RoutedEventArgs e)
            => ToggleSection(VigiaguaSection);

        private void BtnGalSection_Click(object sender, RoutedEventArgs e)
            => ToggleSection(GalSection);

        private void BtnVigiarSection_Click(object sender, RoutedEventArgs e)
            => ToggleSection(VigiarSection);

        private void BtnVigiarQueimadasFolder_Click(object sender, RoutedEventArgs e)
            => SelectFolder(TxtVigiarPastaQueimadas);

        private void BtnVigiarArquivosBrutosFolder_Click(object sender, RoutedEventArgs e)
            => SelectFolder(TxtVigiarPastaArquivosBrutos);

        private void BtnVigiarIqArFolder_Click(object sender, RoutedEventArgs e)
            => SelectFolder(TxtVigiarPastaIqAr);

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

        private void BtnToggleGalPassword_Click(object sender, RoutedEventArgs e)
        {
            if (TxtGalSenhaVisible.Visibility == Visibility.Collapsed)
            {
                TxtGalSenhaVisible.Text = TxtGalSenha.Password;
                TxtGalSenhaVisible.Visibility = Visibility.Visible;
                TxtGalSenha.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtGalSenha.Password = TxtGalSenhaVisible.Text;
                TxtGalSenha.Visibility = Visibility.Visible;
                TxtGalSenhaVisible.Visibility = Visibility.Collapsed;
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

        private void BtnGalBrutaRelatoriosMensalFolder_Click(object sender, RoutedEventArgs e)
            => SelectFolder(TxtGalPastaBrutaRelatoriosMensal);

        private void BtnGalRelatoriosMensalFolder_Click(object sender, RoutedEventArgs e)
            => SelectFolder(TxtGalPastaRelatoriosMensal);


        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private void SelectFolder(TextBox field)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                field.Text = dialog.SelectedPath;
        }
    }
}