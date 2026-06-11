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


namespace Hud_Principal
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

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

        private void BtnSisagua_Click(object sender, RoutedEventArgs e)
            => ToggleSubmenu(SisaguaSubmenu);

        private void BtnGal_Click(object sender, RoutedEventArgs e)
            => ToggleSubmenu(GalSubmenu);

        private void BtnIdaron_Click(object sender, RoutedEventArgs e)
            => ToggleSubmenu(IdaronSubmenu);

    }
}