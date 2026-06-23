using Microsoft.Playwright;

namespace WebAutomation
{
    public class SisaguaDiretrizAnualResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> DownloadedFiles { get; set; } = [];
    }

    public class SisaguaDiretrizAnualAutomation
    {
        private const string UrlLogin = "https://sisagua.saude.gov.br/sisagua/paginaExterna.jsf";

        private static readonly List<string> Parametros =
        [
            "Cloro Residual Livre",
            "Turbidez",
            "Coliformes Totais/E. coli"
        ];

        public async Task<SisaguaDiretrizAnualResult> BaixarRelatoriosAnuaisAsync(
            string email,
            string senha,
            string pastaDestino,
            int anoInicial,
            int anoFinal,
            IProgress<string>? progress = null)
        {
            var result = new SisaguaDiretrizAnualResult();

            try
            {
                progress?.Report("Iniciando navegador...");
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions { Headless = false });

                var context = await browser.NewContextAsync(
                    new BrowserNewContextOptions { AcceptDownloads = true });
                var page = await context.NewPageAsync();

                // Login
                progress?.Report("Fazendo login no Sisagua...");
                await page.GotoAsync(UrlLogin);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.ClickAsync("#j_idt35\\:btnEntrar");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.FillAsync("#email", email);
                await page.FillAsync("#senha", senha);
                await page.ClickAsync("button:has-text('Entrar')");
                await page.WaitForURLAsync(url => !url.Contains("paginaExterna"),
                    new PageWaitForURLOptions { Timeout = 60000 });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Navega para diretriz
                progress?.Report("Navegando para relatórios...");
                var menuRelatorios = page.Locator("span:has-text('RELATÓRIOS')");
                await menuRelatorios.HoverAsync();
                await Task.Delay(1000);

                var linkDiretriz = page.Locator(
                    "a.ui-menuitem-link[href*='relDiretrizNacionalParametrosBasicos.jsf']");
                string? href = await linkDiretriz.GetAttributeAsync("href");
                string urlCompleta = href!.StartsWith("http")
                    ? href : "https://sisagua.saude.gov.br" + href;
                await page.GotoAsync(urlCompleta);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Abrangência
                await page.ClickAsync("#idAbrangencia_label");
                await Task.Delay(500);
                await page.ClickAsync("li[data-label='Unidade Federativa']");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Radio ANUAL
                var radioAnual = page.Locator("input[type='radio'][value='ANUAL']");
                await radioAnual.EvaluateAsync("el => el.click()");
                await Task.Delay(3000);

                // Ano inicial
                progress?.Report($"Selecionando período {anoInicial} a {anoFinal}...");
                await page.ClickAsync("#anoInicial_label");
                await Task.Delay(500);
                await page.ClickAsync($"#anoInicial_panel li[data-label='{anoInicial}']");
                await Task.Delay(500);

                // Ano final
                await page.ClickAsync("#anoFinal_label");
                await Task.Delay(500);
                await page.ClickAsync($"#anoFinal_panel li[data-label='{anoFinal}']");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(1000);

                // Loop parâmetros
                int total = Parametros.Count;
                for (int i = 0; i < total; i++)
                {
                    var parametro = Parametros[i];
                    progress?.Report($"Baixando {parametro} ({i + 1}/{total})...");

                    await page.ClickAsync("#j_idt145_label");
                    await Task.Delay(500);
                    var itemParametro = page.Locator($"li[data-label='{parametro}']");
                    await itemParametro.EvaluateAsync("el => el.click()");
                    await Task.Delay(500);

                    var downloadTask = page.WaitForDownloadAsync(
                        new PageWaitForDownloadOptions { Timeout = 120000 });
                    await page.EvaluateAsync(
                        "document.getElementById('gerarRelatorioExcel').click()");
                    var download = await downloadTask;

                    string parametroSafe = string.Concat(
                        parametro.Split(Path.GetInvalidFileNameChars()));
                    string nomeArquivo =
                        $"Relatório Diretriz Nacional(ANUAL) - {parametroSafe} - {anoInicial}-{anoFinal}.xls";
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