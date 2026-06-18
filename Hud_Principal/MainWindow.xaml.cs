using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Navigation;


namespace Hud_Principal
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Lista com os ids dos paineis
            _panels = [HomePanel, ConfigPanel];

            // Puxa a função para o view.ConfigPanel para entrar
            ConfigPanel.InvokeHomePanel += (s, e) => ShowPanel(HomePanel);
        }

        // Função que dará animação e delay de abertura e fechamento de submenu (sidepanel) para dar mais fluidez
        private void ToggleSubmenu(StackPanel submenu)
        {
            if (submenu.MaxHeight == 0)
            {
                submenu.Visibility = Visibility.Visible;
                var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 200, TimeSpan.FromSeconds(0.25));
                submenu.BeginAnimation(FrameworkElement.MaxHeightProperty, anim);
            }
            else
            {
                var anim = new System.Windows.Media.Animation.DoubleAnimation(200, 0, TimeSpan.FromSeconds(0.2));
                anim.Completed += (s, e) => submenu.Visibility = Visibility.Collapsed;
                submenu.BeginAnimation(FrameworkElement.MaxHeightProperty, anim);
            }
        }

        // Informando que a função acima (ToggleSubmenu) estará nos ementos de id SisaguaSubmenu....
        private void BtnSisagua_Click(object sender, RoutedEventArgs e)
            => ToggleSubmenu(SisaguaSubmenu);

        private void BtnGal_Click(object sender, RoutedEventArgs e)
            => ToggleSubmenu(GalSubmenu);

        private void BtnIdaron_Click(object sender, RoutedEventArgs e)
            => ToggleSubmenu(IdaronSubmenu);

        // Lógica para navegação de links na página xaml
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });

            e.Handled = true;
        }

        // Lista com todos os painéis da grid (x:Name="[Nome]")
        private List<FrameworkElement> _panels;

        // Colapsa todos e abre somente o solicitado
        private void ShowPanel(FrameworkElement panel)
        {
            foreach (var i in _panels)
                i.Visibility = Visibility.Collapsed;

            panel.Visibility = Visibility.Visible;   
        }

        // Acessa a grid das configurações
        private void BtnConfig_Click(object sender, RoutedEventArgs e)
            => ShowPanel(ConfigPanel);

        // Função para as setas para voltar ao homepage
        private void BtnHome_click(object sender, RoutedEventArgs e)
            => ShowPanel(HomePanel);
    }
}