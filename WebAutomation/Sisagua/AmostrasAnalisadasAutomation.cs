using Microsoft.Playwright;
using System.IO;

namespace WebAutomation
{
    public class AmostrasAnalisadasResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> DownloadedFiles { get; set; } = [];
    }

    public class AmostrasAnalisadasAutomation
    {
        private const string UrlLogin = "https://sisagua.saude.gov.br/sisagua/paginaExterna.jsf";
        private const string UrlReport = "https://sisagua.saude.gov.br/sisagua/paginas/seguro/relatorioVigilanciaAmostrasAnalisadas/relVigilanciaAmostrasAnalisadas.jsf?faces-redirect=true";

        public async Task<AmostrasAnalisadasResult> DownloadAsync(
            string email,
            string password,
            string rawFilesFolder,
            List<int> years,
            List<string> infoLabels,
            List<string> basicosLabels,
            List<string> demaisLabels,
            IProgress<string>? progress = null)
        {
            var result = new AmostrasAnalisadasResult();

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
                await page.FillAsync("#senha", password);
                await page.ClickAsync("button:has-text('Entrar')");
                await page.WaitForURLAsync(url => !url.Contains("paginaExterna"),
                    new PageWaitForURLOptions { Timeout = 60_000 });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Navegar para a página do relatório
                progress?.Report("Navegando para Amostras Analisadas...");
                await page.GotoAsync(UrlReport);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Abrangência: Unidade Federativa (fixo)
                progress?.Report("Configurando abrangência: Unidade Federativa...");
                await page.ClickAsync("#idAbrangencia_label");
                await Task.Delay(500);
                await page.ClickAsync("li[data-label='Unidade Federativa']");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(1000);


                // Loop por ano — cada ano é processado de forma completa e independente
                foreach (var year in years.OrderBy(y => y))
                {
                    progress?.Report($"Selecionando ano {year}...");
                    await page.ClickAsync("#ano2_label");
                    await Task.Delay(500);
                    await page.ClickAsync($"li[data-label='{year}']");
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await Task.Delay(1000);

                    // Marcar campos — Informações gerais da coleta
                    if (infoLabels.Count > 0)
                    {
                        progress?.Report($"[{year}] Selecionando informações gerais ({infoLabels.Count} campos)...");
                        foreach (var label in infoLabels)
                            await MarkCheckboxByLabelAsync(page, "#camposInfGeraisColeta", label);
                    }

                    // Marcar campos — Resultado (parâmetros básicos ou demais, mutuamente exclusivos)
                    if (basicosLabels.Count > 0)
                    {
                        progress?.Report($"[{year}] Selecionando parâmetros básicos ({basicosLabels.Count} campos)...");
                        foreach (var label in basicosLabels)
                            await MarkCheckboxByLabelAsync(page, "#camposResultadosParametros", label);
                    }
                    else if (demaisLabels.Count > 0)
                    {
                        progress?.Report($"[{year}] Selecionando demais parâmetros ({demaisLabels.Count} campos)...");
                        foreach (var label in demaisLabels)
                            await MarkCheckboxByLabelAsync(page, "#camposResultados", label);
                    }

                    // Gerar relatório e iniciar download em um único clique.
                    // O botão #gerarRelatorioExcel usa PrimeFaces.monitorDownload — mesmo padrão
                    // já validado em SisaguaDiretrizAutomation.
                    progress?.Report($"[{year}] Gerando relatório...");
                    var downloadTask = page.WaitForDownloadAsync(
                        new PageWaitForDownloadOptions { Timeout = 120_000 });
                    await page.EvaluateAsync("document.getElementById('gerarRelatorioExcel').click()");
                    var download = await downloadTask;

                    string ext = Path.GetExtension(download.SuggestedFilename);
                    if (string.IsNullOrEmpty(ext)) ext = ".xlsx";
                    string fileName = $"PARAMETROS_SISAGUA_BRUTO_{year}{ext}";
                    string destination = Path.Combine(rawFilesFolder, fileName);

                    if (File.Exists(destination)) File.Delete(destination);
                    await download.SaveAsAsync(destination);
                    result.DownloadedFiles.Add(destination);

                    progress?.Report($"[{year}] Salvo: {fileName}");
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

        // Localiza um checkbox pelo texto exato do <label> dentro de uma seção do formulário
        // e o clica via div.ui-chkbox (o input real é oculto por ui-helper-hidden-accessible).
        // Estrutura esperada:
        //   dl.checkboxREL > dt > div.ui-chkbox > input[type=checkbox]
        //   dl.checkboxREL > dd > label (texto do parâmetro)
        //
        // GetByText(Exact=true) faz matching exato — evita que "Área" bata em
        // "Categoria da Área" pelo comportamento de substring do HasText simples.
        private static async Task MarkCheckboxByLabelAsync(IPage page, string sectionSelector, string labelText)
        {
            // Localiza o <dd> cujo texto interno é exatamente labelText, depois sobe para o <dl> pai.
            var dd = page.Locator($"{sectionSelector} dl.checkboxREL dd")
                         .GetByText(labelText, new LocatorGetByTextOptions { Exact = true });

            var dl = dd.Locator("xpath=ancestor::dl[1]");
            var input = dl.Locator("input[type='checkbox']");

            bool isChecked = await input.IsCheckedAsync();
            if (!isChecked)
                await dl.Locator("dt .ui-chkbox").ClickAsync();

            await page.WaitForTimeoutAsync(1000);
        }
    }
}
