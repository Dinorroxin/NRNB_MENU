using Microsoft.Playwright;

namespace WebAutomation
{
    public class SisaguaDiretrizResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> DownloadedFiles { get; set; } = [];
    }

    public class SisaguaDiretrizAutomation
    {
        private const string UrlLogin = "https://sisagua.saude.gov.br/sisagua/paginaExterna.jsf";

        private static readonly List<string> Parametros =
        [
            "Cloro Residual Livre",
            "Turbidez",
            "Coliformes Totais/E. coli"
        ];

        public async Task<SisaguaDiretrizResult> BaixarRelatoriosMensaisAsync(
            string email,
            string senha,
            string pastaDestino
            )
        {
            var result = new SisaguaDiretrizResult();

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false
                });

                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    AcceptDownloads = true
                });
                var page = await context.NewPageAsync();

                // 1. Login
                await page.GotoAsync(UrlLogin);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await page.ClickAsync("#j_idt35\\:btnEntrar");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await page.FillAsync("#email", email);
                await page.FillAsync("#senha", senha);

                await page.ClickAsync("button:has-text('Entrar')");
                await page.WaitForURLAsync(url => !url.Contains("paginaExterna"), new PageWaitForURLOptions
                {
                    Timeout = 60000
                });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // 2. Hover no menu RELATÓRIOS e navega para a página de diretriz
                var menuRelatorios = page.Locator("span:has-text('RELATÓRIOS')");
                await menuRelatorios.HoverAsync();
                await Task.Delay(500);

                var linkDiretriz = page.Locator("a.ui-menuitem-link[href*='relDiretrizNacionalParametrosBasicos.jsf']");
                string? href = await linkDiretriz.GetAttributeAsync("href");

                string urlCompleta = href!.StartsWith("http") ? href : "https://sisagua.saude.gov.br" + href;
                await page.GotoAsync(urlCompleta);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // 3. Abrangência = Unidade Federativa (PrimeFaces dropdown)
                await page.ClickAsync("#idAbrangencia_label");
                await Task.Delay(500);
                await page.ClickAsync("li[data-label='Unidade Federativa']");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // 4. Tipo = Mensal (radio via JS)
                var radioMensal = page.Locator("input[type='radio'][value='MENSAL']");
                await radioMensal.EvaluateAsync("el => el.click()");
                await Task.Delay(3000);

                // 5. Loop pelos parâmetros
                foreach (var parametro in Parametros)
                {
                    // Abre dropdown de parâmetro
                    await page.ClickAsync("#j_idt145_label");
                    await Task.Delay(500);

                    var itemParametro = page.Locator($"li[data-label='{parametro}']");
                    await itemParametro.EvaluateAsync("el => el.click()");
                    await Task.Delay(500);

                    // Captura download
                    var downloadTask = page.WaitForDownloadAsync();
                    await page.EvaluateAsync("document.getElementById('gerarRelatorioExcel').click()");
                    var download = await downloadTask;

                    // Salva com mesmo padrão de nome do script Python
                    string parametroSafe = string.Concat(parametro.Split(Path.GetInvalidFileNameChars()));
                    string dataStr = DateTime.Now.ToString("dd-MM-yyyy");
                    string nomeArquivo = $"Relatório Diretriz Nacional(MESES) - {parametroSafe} - {dataStr}.xls";
                    string destino = Path.Combine(pastaDestino, nomeArquivo);

                    if (File.Exists(destino)) File.Delete(destino);
                    await download.SaveAsAsync(destino);
                    result.DownloadedFiles.Add(destino);
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }
}