using Microsoft.Playwright;
using System.Globalization;

namespace WebAutomation
{
    public class VigiarFocosCalorAutomation : IAsyncDisposable
    {
        private const string BdQueimadasUrl = "https://terrabrasilis.dpi.inpe.br/queimadas/bdqueimadas/";
        private const string DataUrl        = "https://terrabrasilis.dpi.inpe.br/queimadas/bdqueimadas/graphicsfirescount";

        private IPlaywright? _playwright;
        private IBrowser?    _browser;
        private IPage?       _page;

        public async Task InicializarAsync(IProgress<string>? progress = null)
        {
            progress?.Report("Iniciando browser (Playwright headless)...");

            _playwright = await Playwright.CreateAsync();
            _browser    = await _playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions { Headless = true });

            var context = await _browser.NewContextAsync();
            _page = await context.NewPageAsync();

            progress?.Report("Navegando para BDQueimadas (estabelecendo sessão)...");
            await _page.GotoAsync(BdQueimadasUrl);

            // Aguarda a página carregar — cookies de sessão são definidos neste ponto.
            try
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = 20_000 });
            }
            catch (TimeoutException) { }

            progress?.Report("Browser pronto. Sessão ativa.");
        }

        // Para cada semana: registra rota, navega para a URL completa com os parâmetros,
        // aguarda o Chromium real fazer a requisição e captura o JSON via route handler.
        public async Task<string> BuscarSemanaJsonAsync(DateTime inicio, DateTime fim)
        {
            string url = DataUrl + "?" + BuildQueryString(inicio, fim);

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            Func<IRoute, Task> handler = async route =>
            {
                try
                {
                    var apiResponse = await route.FetchAsync();
                    string body = await apiResponse.TextAsync();
                    tcs.TrySetResult(body);
                    await route.FulfillAsync(new RouteFulfillOptions
                    {
                        Status      = apiResponse.Status,
                        Body        = body,
                        ContentType = "application/json; charset=utf-8"
                    });
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                    await route.AbortAsync();
                }
            };

            await _page!.RouteAsync("**/graphicsfirescount**", handler);

            try
            {
                await _page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout   = 30_000
                });
            }
            catch (TimeoutException) { }

            // GotoAsync aguarda NetworkIdle, que só fecha após o handler cumprir a rota.
            // O WaitAsync é uma segurança caso o handler tenha demorado além do esperado.
            string json = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));

            await _page.UnrouteAsync("**/graphicsfirescount**", handler);

            return json;
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
            _browser    = null;
            _playwright = null;
            _page       = null;
        }

        private static string BuildQueryString(DateTime inicio, DateTime fim)
        {
            static string V(string v) => Uri.EscapeDataString(v);

            string dateFrom = inicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " 00:00:00";
            string dateTo   = fim.ToString("yyyy-MM-dd",   CultureInfo.InvariantCulture)   + " 23:59:59";

            return "id=firesByCity"
                + $"&y={V("RONDÔNIA (RONDÔNIA)")}"
                + "&key=municipio"
                + "&limit="
                + $"&title={V("Focos por Município")}"
                + "&satellites=AQUA_M-T"
                + "&biomes="
                + "&risk="
                + "&continent=8"
                + "&countries=33"
                + "&states=03311"
                + "&specialRegions="
                + "&industrialFires=false"
                + $"&dateTimeFrom={V(dateFrom)}"
                + $"&dateTimeTo={V(dateTo)}"
                + "&filterRules%5BignoreCountryFilter%5D=false"
                + "&filterRules%5BignoreStateFilter%5D=false"
                + "&filterRules%5BignoreCityFilter%5D=false"
                + "&filterRules%5BshowOnlyIfThereIsACountryFiltered%5D=false"
                + "&filterRules%5BshowOnlyIfThereIsNoCountryFiltered%5D=false"
                + "&filterRules%5BshowOnlyIfThereIsAStateFiltered%5D=true"
                + "&filterRules%5BshowOnlyIfThereIsNoStateFiltered%5D=false";
        }
    }
}
