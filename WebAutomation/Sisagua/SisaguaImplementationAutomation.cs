using Microsoft.Playwright;

namespace WebAutomation
{
    public class SisaguaImplementationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> DownloadedFiles { get; set; } = [];
    }

    public class SisaguaImplementationAutomation
    {
        private const string UrlLogin = "https://sisagua.saude.gov.br/sisagua/paginaExterna.jsf";

        public async Task<SisaguaImplementationResult> DownloadAnnualReportsAsync(
            string email,
            string password,
            string destinationFolder,
            List<int> years,
            IProgress<string>? progress = null)
        {
            var result = new SisaguaImplementationResult();

            try
            {
                progress?.Report("Iniciando navegador...");
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions { Headless = false });

                var context = await browser.NewContextAsync(
                    new BrowserNewContextOptions { AcceptDownloads = true });
                var page = await context.NewPageAsync();

                // Login — idêntico ao SisaguaDiretrizAutomation
                progress?.Report("Fazendo login no Sisagua...");
                await page.GotoAsync(UrlLogin);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.ClickAsync("#j_idt35\\:btnEntrar");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.FillAsync("#email", email);
                await page.FillAsync("#senha", password);
                await page.ClickAsync("button:has-text('Entrar')");
                await page.WaitForURLAsync(url => !url.Contains("paginaExterna"),
                    new PageWaitForURLOptions { Timeout = 60000 });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Navega até o relatório de implementação
                progress?.Report("Navegando para relatórios...");
                var linkImplementacao = page.Locator(
                    "a.ui-menuitem-link[href*='relImplementacaoVigiaguaDetalhado.jsf']");
                string? href = await linkImplementacao.GetAttributeAsync("href");
                string fullUrl = href!.StartsWith("http")
                    ? href : "https://sisagua.saude.gov.br" + href;
                await page.GotoAsync(fullUrl);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Abrangência — fixo em Unidade Federativa (a conta já é escopada a Rondônia)
                await page.ClickAsync("#idAbrangencia_label");
                await Task.Delay(500);
                await page.ClickAsync("li[data-label='Unidade Federativa']");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                foreach (var year in years)
                {
                    progress?.Report($"Baixando ano {year}...");

                    await page.ClickAsync("#ano_label");
                    await Task.Delay(500);
                    await page.ClickAsync($"li[data-label='{year}']");
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await Task.Delay(1000);

                    var downloadTask = page.WaitForDownloadAsync(new PageWaitForDownloadOptions
                    {
                        Timeout = 120000
                    });
                    await page.EvaluateAsync(
                        "document.getElementById('gerarRelatorioExcel').click()");
                    var download = await downloadTask;

                    string fileName = $"IMPLEMENTACAO_SISAGUA_BRUTO_{year}.xls";
                    string destination = Path.Combine(destinationFolder, fileName);

                    if (File.Exists(destination)) File.Delete(destination);
                    await download.SaveAsAsync(destination);
                    result.DownloadedFiles.Add(destination);
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