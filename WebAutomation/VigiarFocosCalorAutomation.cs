using Microsoft.Playwright;
using System.Collections.Concurrent;

namespace WebAutomation
{
    public class VigiarFocosCalorAutomation : IAsyncDisposable
    {
        private const string BdQueimadasUrl = "https://terrabrasilis.dpi.inpe.br/queimadas/bdqueimadas/#graficos";

        private IPlaywright?       _playwright;
        private IBrowser?          _browser;
        private IPage?             _page;
        private IProgress<string>? _progress;

        public async Task InicializarAsync(IProgress<string>? progress = null)
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

            // Dropdowns encadeados: cada seleção dispara AJAX para popular o próximo.
            progress?.Report("Configurando filtros: América do Sul → Brasil → Rondônia...");
            await _page.SelectOptionAsync("#continents", "8");
            await _page.WaitForTimeoutAsync(1500);
            await _page.SelectOptionAsync("#countries", "33");
            await _page.WaitForTimeoutAsync(1500);
            await _page.SelectOptionAsync("#states", "03311");
            await _page.SelectOptionAsync("#filter-satellite", "AQUA_M-T");

            progress?.Report("Browser pronto. Filtros configurados.");
        }

        // Fluxo por semana:
        // (1) setar datas via jQuery datepicker
        // (2) clicar #filter-button
        // (3) aguardar firesByState — confirma que o filtro foi processado
        // (4) clicar #box-firesByCity .layer-title — expande accordion lazy-loaded
        // (5) capturar graphicsfirescount?id=firesByCity via RouteAsync
        public async Task<string> BuscarSemanaJsonAsync(DateTime inicio, DateTime fim)
        {
            var tcsCity       = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var urlsCapturadas = new ConcurrentBag<string>();

            void OnRequest(object? _, IRequest req) => urlsCapturadas.Add(req.Url);
            _page!.Request += OnRequest;

            // Rota registrada antes do accordion: captura firesByCity, deixa os demais passar.
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

            // (1) Setar datas — val() define o texto; datepicker('setDate') sincroniza o widget.
            await _page.EvaluateAsync(
                $"$('#filter-date-from').val('{inicio:dd/MM/yyyy}')" +
                $".datepicker('setDate', new Date({inicio.Year},{inicio.Month - 1},{inicio.Day}))");
            await _page.EvaluateAsync(
                $"$('#filter-date-to').val('{fim:dd/MM/yyyy}')" +
                $".datepicker('setDate', new Date({fim.Year},{fim.Month - 1},{fim.Day}))");

            // (2) Clicar Aplicar via DOM event.
            await _page.EvaluateAsync(
                "document.getElementById('filter-button')" +
                ".dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true}))");

            // (3) Aguardar firesByState — sinal de que o filtro foi processado pelo servidor.
            try
            {
                await _page.WaitForResponseAsync(
                    r => r.Url.Contains("id=firesByState"),
                    new PageWaitForResponseOptions { Timeout = 15_000 });
            }
            catch (TimeoutException) { }

            // (4) Expandir o accordion só se estiver fechado — ele é um toggle.
            // A partir da 2ª semana já está aberto; clicar novamente o fecharia.
            bool aberto = await _page.EvaluateAsync<bool>(
                "document.querySelector('#box-firesByCity div[style]').style.display !== 'none'");
            if (!aberto)
                await _page.ClickAsync("#box-firesByCity .layer-title");

            // (5) Capturar firesByCity.
            try
            {
                string json = await tcsCity.Task.WaitAsync(TimeSpan.FromSeconds(15));
                _page.Request -= OnRequest;
                await _page.UnrouteAsync("**/graphicsfirescount**", handler);
                return json;
            }
            catch (TimeoutException)
            {
                _page.Request -= OnRequest;
                await _page.UnrouteAsync("**/graphicsfirescount**", handler);

                string lista = urlsCapturadas.Count == 0
                    ? "(nenhuma URL disparada)"
                    : string.Join("\n  ", urlsCapturadas);

                throw new InvalidOperationException(
                    $"Timeout: firesByCity não interceptado após expandir accordion.\n" +
                    $"URLs capturadas:\n  {lista}");
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
