using System.IO;
using System.Windows;
using Application = System.Windows.Application;

namespace hud_principal
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Redireciona browsers do Playwright para pasta relativa ao .exe.
            // Necessário em publish single-file: sem isso, Playwright busca em
            // %LOCALAPPDATA%\ms-playwright, que não existe em máquina limpa.
            Environment.SetEnvironmentVariable(
                "PLAYWRIGHT_BROWSERS_PATH",
                Path.Combine(AppContext.BaseDirectory, "playwright-browsers"));

            base.OnStartup(e);
        }
    }
}
