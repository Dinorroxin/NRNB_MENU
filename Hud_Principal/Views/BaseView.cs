using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace Hud_Principal.Views
{
    public class BaseView : UserControl
    {
        public event EventHandler? RequestHome;

        protected void BtnHome_click(object sender, EventArgs e)
        
            => RequestHome?.Invoke(this, EventArgs.Empty);
        
    }
}
