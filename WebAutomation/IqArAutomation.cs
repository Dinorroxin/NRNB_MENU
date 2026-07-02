using Microsoft.Playwright;
using System.IO;

namespace WebAutomation
{
    public class IqArAutomation : IAsyncDisposable
    {
        private const string ShinyUrl = "https://shiny.icict.fiocruz.br/alertarsaude/";
        private const string NomeArquivo = "IQAR_ESTADO.csv";

        private IPlaywright? _playwright;
        private IBrowser?    _browser;

        // Retorna o caminho completo do CSV baixado.
        public async Task<string> ExecutarAsync(
            string pastaArquivosBrutos,
            IProgress<string>? progress = null)
        {
            progress?.Report("Iniciando browser (Playwright headless)...");
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions { Headless = true });

            var context = await _browser.NewContextAsync(
                new BrowserNewContextOptions { AcceptDownloads = true });
            var page = await context.NewPageAsync();

            // 1. Navegar para o Shiny app e aguardar inicialização da sessão.
            //    O Shiny pode demorar consideravelmente; aguardamos a aba IQAr aparecer no DOM.
            progress?.Report("Abrindo Observatório Fiocruz — aguardando inicialização do Shiny (pode levar até 90s)...");
            await page.GotoAsync(ShinyUrl);

            await page.WaitForSelectorAsync(
                "a[data-value='IQAr']",
                new PageWaitForSelectorOptions { Timeout = 90_000 });

            // 2. Clicar na aba IQAr.
            progress?.Report("Selecionando aba IQAr...");
            await page.ClickAsync("a[data-value='IQAr']");

            // Aguarda o conteúdo da aba carregar (mapa + gráfico inicializam via Shiny).
            try
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = 30_000 });
            }
            catch (TimeoutException) { }

            // 3. Selecionar "RO" no bootstrap-select de estado.
            //    selectpicker('val') atualiza o widget e dispara o change para o Shiny.
            progress?.Report("Selecionando estado Rondônia (RO)...");
            await page.EvaluateAsync("$('#uf').selectpicker('val', 'RO')");

            // Aguarda o Shiny reprocessar os dados para RO.
            try
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = 30_000 });
            }
            catch (TimeoutException) { }

            // 4. Expandir o accordion "Download" dentro da aba IQAr.
            //    Usa seletor relativo ao data-value para não depender de IDs dinâmicos do bslib.
            progress?.Report("Expandindo seção Download...");
            await page.ClickAsync(
                ".tab-pane[data-value='IQAr'] .accordion-item[data-value='Download'] .accordion-button");

            // 5. Aguardar o link de município ser habilitado pelo Shiny (remove class 'disabled').
            progress?.Report("Aguardando link de download ficar disponível...");
            await page.WaitForSelectorAsync(
                "#download_data_iqar_uf:not(.disabled)",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            // 6. Baixar o CSV de municípios.
            //    O href contém o session ID dinâmico do Shiny — não precisamos extraí-lo,
            //    pois o Playwright segue o link nativo com o contexto de sessão correto.
            progress?.Report("Baixando CSV do estado...");
            string caminhoDestino = Path.Combine(pastaArquivosBrutos, NomeArquivo);

            var download = await page.RunAndWaitForDownloadAsync(
                () => page.ClickAsync("#download_data_iqar_uf"));
            await download.SaveAsAsync(caminhoDestino);

            progress?.Report($"Download concluído: {NomeArquivo}");
            return caminhoDestino;
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
            _browser    = null;
            _playwright = null;
        }
    }
}
