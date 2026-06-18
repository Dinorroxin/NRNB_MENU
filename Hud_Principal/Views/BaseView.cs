using System.Windows;
using System.Windows.Controls;

namespace Hud_Principal.Views
{
    public class BaseView : UserControl
    {
        public event EventHandler? InvokeHomePanel;

        protected void BtnHome_click(object sender, EventArgs e)
        
            => InvokeHomePanel?.Invoke(this, EventArgs.Empty);
        
    }
}
