using Microsoft.Playwright;
using System.Collections.Concurrent;

namespace WebAutomation
{
    public class VigiarFocosCalorAutomation : IAsyncDisposable
    {
        private const string BdQueimadasUrl        = "https://terrabrasilis.dpi.inpe.br/queimadas/bdqueimadas/#graficos";
        private const int    FiresByCityTimeoutMs   = 10_000;

        private IPlaywright?       _playwright;
        private IBrowser?          _browser;
        private IPage?             _page;
        private IProgress<string>? _progress;

        public async Task InitializeAsync(IProgress<string>? progress = null)
        {
            _progress = progress;

            progress?.Report("Iniciando browser (Playwright headless)...");
            _playwright = await Playwright.CreateAsync();
            _browser    = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await _browser.NewContextAsync();
            _page = await context.NewPageAsync();

            progress?.Report("Abrindo BDQueimadas (#graficos)...");
            await _page.GotoAsync(BdQueimadasUrl);
            try
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = 30_000 });
            }
            catch (TimeoutException) { }

            progress?.Report("Configurando filtros: América do Sul → Brasil → Rondônia...");
            await _page.SelectOptionAsync("#continents", "8");
            await _page.WaitForTimeoutAsync(1500);
            await _page.SelectOptionAsync("#countries", "33");
            await _page.WaitForTimeoutAsync(1500);
            await _page.SelectOptionAsync("#states", "03311");
            await _page.SelectOptionAsync("#filter-satellite", "AQUA_M-T");

            progress?.Report("Browser pronto. Filtros configurados.");
        }

        // Flow per week:
        // (1) set dates via jQuery datepicker
        // (2) click #filter-button
        // (3) wait for firesByState — confirms filter was processed
        // (4) click #box-firesByCity .layer-title — expands lazy-loaded accordion
        // (5) capture graphicsfirescount?id=firesByCity via RouteAsync
        public async Task<string> FetchWeekJsonAsync(DateTime start, DateTime end)
        {
            var tcsCity       = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var capturedUrls  = new ConcurrentBag<string>();

            void OnRequest(object? _, IRequest req) => capturedUrls.Add(req.Url);
            _page!.Request += OnRequest;

            Func<IRoute, Task> handler = async route =>
            {
                if (!route.Request.Url.Contains("id=firesByCity"))
                {
                    await route.ContinueAsync();
                    return;
                }
                try
                {
                    var apiResponse = await route.FetchAsync();
                    string body = await apiResponse.TextAsync();
                    tcsCity.TrySetResult(body);
                    await route.FulfillAsync(new RouteFulfillOptions
                    {
                        Status      = apiResponse.Status,
                        Body        = body,
                        ContentType = "application/json; charset=utf-8"
                    });
                }
                catch (Exception ex)
                {
                    tcsCity.TrySetException(ex);
                    await route.AbortAsync();
                }
            };

            await _page.RouteAsync("**/graphicsfirescount**", handler);

            await _page.EvaluateAsync(
                $"$('#filter-date-from').val('{start:dd/MM/yyyy}')" +
                $".datepicker('setDate', new Date({start.Year},{start.Month - 1},{start.Day}))");
            await _page.EvaluateAsync(
                $"$('#filter-date-to').val('{end:dd/MM/yyyy}')" +
                $".datepicker('setDate', new Date({end.Year},{end.Month - 1},{end.Day}))");

            await _page.EvaluateAsync(
                "document.getElementById('filter-button')" +
                ".dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true}))");

            try
            {
                await _page.WaitForResponseAsync(
                    r => r.Url.Contains("id=firesByState"),
                    new PageWaitForResponseOptions { Timeout = 15_000 });
            }
            catch (TimeoutException) { }

            bool isOpen = await _page.EvaluateAsync<bool>(
                "document.querySelector('#box-firesByCity div[style]').style.display !== 'none'");
            _progress?.Report($"[accordion] firesByCity aberto={isOpen}; " +
                              (isOpen ? "aguardando refresh automático" : "clicando para abrir"));
            if (!isOpen)
                await _page.ClickAsync("#box-firesByCity .layer-title");

            try
            {
                string json = await tcsCity.Task.WaitAsync(TimeSpan.FromMilliseconds(FiresByCityTimeoutMs));
                _page.Request -= OnRequest;
                await _page.UnrouteAsync("**/graphicsfirescount**", handler);
                return json;
            }
            catch (TimeoutException)
            {
                _page.Request -= OnRequest;
                await _page.UnrouteAsync("**/graphicsfirescount**", handler);

                string urlList = capturedUrls.Count == 0
                    ? "(nenhuma URL disparada)"
                    : string.Join("\n  ", capturedUrls);

                throw new InvalidOperationException(
                    $"Timeout: firesByCity não interceptado após expandir accordion.\n" +
                    $"URLs capturadas:\n  {urlList}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
            _browser    = null;
            _playwright = null;
            _page       = null;
        }
    }
}
