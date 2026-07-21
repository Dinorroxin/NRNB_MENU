using System.IO;
using System.Windows;
using Application = System.Windows.Application;

namespace hud_principal
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Redireciona browsers do Playwright para pasta relativa ao .exe,
            // mas só quando essa pasta existir (só existe em publish single-file).
            // Em Debug/F5 a pasta não existe: mantém o cache padrão do Playwright
            // (%LOCALAPPDATA%\ms-playwright), evitando quebrar execução local.
            var browsersPath = Path.Combine(AppContext.BaseDirectory, "playwright-browsers");
            if (Directory.Exists(browsersPath))
            {
                Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browsersPath);
            }

            base.OnStartup(e);
        }
    }
}