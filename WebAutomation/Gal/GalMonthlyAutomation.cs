using Microsoft.Playwright;

namespace WebAutomation
{
    public class GalMonthlyAutomation
    {
        public async Task<bool> BaixarRelatorioMensalAsync(
            string username,
            string password,
            string module,
            string laboratory,
            string downloadFolder,
            string reference,
            string startDate,
            string endDate,
            string ibgeCode,
            IProgress<string> progress)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[]
                {
                    "--ignore-certificate-errors",
                    "--ignore-ssl-errors",
                    "--allow-insecure-localhost"
                }
            });

            var context = await browser.NewContextAsync();

            // Supresses window.print() at the context level so any tab opened by window.open()
            // has the override injected before its onload fires.
            await context.AddInitScriptAsync("window.print = () => {};");

            var page = await context.NewPageAsync();

            bool loginSucceeded = false;
            int attempt = 1;
            const int maxAttempts = 100;

            while (attempt <= maxAttempts && !loginSucceeded)
            {
                try
                {
                    progress.Report($"Tentativa {attempt} — carregando página...");
                    await page.GotoAsync("https://gal.rondonia.sus.gov.br/");

                    await page.WaitForTimeoutAsync(200);
                    await page.FillAsync("[name='login']", username);
                    await page.FillAsync("[name='senha']", password);

                    await page.ClickAsync("[name='modulo']");
                    await page.WaitForTimeoutAsync(300);
                    await page.Keyboard.TypeAsync(module);
                    await page.WaitForTimeoutAsync(1000);
                    await page.Keyboard.PressAsync("Enter");
                    await page.WaitForTimeoutAsync(2500);

                    await page.Keyboard.PressAsync("Tab");
                    await page.WaitForTimeoutAsync(500);
                    await page.Keyboard.PressAsync("Control+A");
                    await page.Keyboard.PressAsync("Backspace");
                    await page.WaitForTimeoutAsync(300);
                    await page.Keyboard.TypeAsync(laboratory);
                    await page.WaitForTimeoutAsync(800);
                    await page.Keyboard.PressAsync("Enter");
                    await page.WaitForTimeoutAsync(1000);
                    progress.Report($"Tentativa {attempt} — clicando em Entrar...");

                    await page.ClickAsync("//button[text()='Entrar']");

                    var captchaImg = await page.WaitForSelectorAsync("img[src*='captcha']");
                    await page.WaitForTimeoutAsync(1000);

                    var imgBytes = await captchaImg!.ScreenshotAsync();

                    string captchaText = await SolveCaptchaAsync(imgBytes, progress);
                    progress.Report($"Tentativa {attempt} — captcha lido: {captchaText}");

                    await page.ClickAsync("[name='ext-comp-1031']");
                    await page.WaitForTimeoutAsync(300);
                    await page.Keyboard.TypeAsync(captchaText);
                    await page.WaitForTimeoutAsync(500);

                    await page.ClickAsync("xpath=//button[text()='Confirmar']");
                    await page.WaitForTimeoutAsync(5000);

                    var loginField = await page.QuerySelectorAsync("[name='login']");
                    if (loginField == null)
                    {
                        progress.Report("Login realizado com sucesso.");
                        loginSucceeded = true;
                    }
                    else
                    {
                        progress.Report($"Captcha incorreto — tentativa {attempt}");
                        attempt++;

                        await page.GotoAsync("about:blank");
                        await page.WaitForTimeoutAsync(500);
                    }
                }
                catch (Exception ex)
                {
                    progress.Report($"Erro na tentativa {attempt}: {ex.Message}");
                    attempt++;
                    await page.WaitForTimeoutAsync(2000);
                }
            }

            if (!loginSucceeded)
            {
                progress.Report("Falha: número máximo de tentativas atingido.");
                return false;
            }

            try
            {
                async Task ClickTree(string targetText, bool isFolder)
                {
                    string xpath = isFolder
                        ? $"//span[text()='{targetText}']/../../img[contains(@class, 'x-tree-ec-icon')]"
                        : $"//span[text()='{targetText}']";

                    var element = await page.WaitForSelectorAsync($"xpath={xpath}");
                    await page.EvaluateAsync("el => el.scrollIntoView({block: 'center'})", element);
                    await page.WaitForTimeoutAsync(1000);
                    await page.EvaluateAsync("el => el.click()", element);
                    await page.WaitForTimeoutAsync(2000);
                }

                progress.Report("Navegando na árvore...");
                await ClickTree("Ambiental", true);
                await ClickTree("Relatórios", true);
                await ClickTree("Gestão", false);

                var frame = await page.WaitForSelectorAsync("#content-panel");
                var contentFrame = await frame!.ContentFrameAsync();

                progress.Report("Selecionando relatório...");
                var gridItem = await contentFrame!.WaitForSelectorAsync(
                    "xpath=//div[contains(@class, 'x-grid3-cell-inner') and contains(text(), 'Acompanhamento')]");
                await contentFrame.EvaluateAsync(
                    "el => { var ev = new MouseEvent('dblclick', {bubbles:true}); el.dispatchEvent(ev); }",
                    gridItem);

                progress.Report("Preenchendo filtros...");
                var trigger = await contentFrame.WaitForSelectorAsync(
                    "xpath=//label[contains(text(), 'Referência')]/following::img[1]");
                await trigger!.ClickAsync();
                await contentFrame.ClickAsync($"xpath=//div[text()='{reference}']");

                var fields = new (string Label, string Value)[]
                {
                    ("Início", startDate),
                    ("Fim",    endDate),
                    ("Cód. IBGE", ibgeCode)
                };

                foreach (var (label, value) in fields)
                {
                    var field = await contentFrame.QuerySelectorAsync(
                        $"xpath=//label[contains(text(), '{label}')]/following::input[1]");
                    await contentFrame.EvaluateAsync("el => { el.value = ''; }", field);
                    await field!.FillAsync(value);
                    await page.Keyboard.PressAsync("Tab");
                    await page.WaitForTimeoutAsync(1000);
                }

                progress.Report("Gerando PDF...");
                var btnGenerate = await contentFrame.QuerySelectorAsync("xpath=//button[text()='Gerar']");
                await contentFrame.EvaluateAsync("el => el.click()", btnGenerate);

                progress.Report("Aguardando nova aba...");
                var newPage = await context.WaitForPageAsync(new BrowserContextWaitForPageOptions
                {
                    Timeout = 60000
                });

                await newPage.AddInitScriptAsync("window.print = () => {}");

                progress.Report("Aguardando conteúdo carregar...");
                await newPage.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                {
                    Timeout = 60000
                });

                await newPage.WaitForSelectorAsync("xpath=//*[contains(text(), 'Município')]", new PageWaitForSelectorOptions
                {
                    Timeout = 60000
                });

                progress.Report("Gerando PDF...");
                var pdfBytes = await newPage.PdfAsync(new PagePdfOptions
                {
                    PrintBackground = true,
                    PreferCSSPageSize = true
                });

                string dStart = startDate.Replace("/", "");
                string dEnd   = endDate.Replace("/", "");
                string fileName = $"Relatorio_GAL_{dStart}_a_{dEnd}.pdf";
                string finalPath = Path.Combine(downloadFolder, fileName);

                await File.WriteAllBytesAsync(finalPath, pdfBytes);

                progress.Report($"PDF salvo: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                progress.Report($"Erro no fluxo pós-login: {ex.Message}");
                return false;
            }
        }

        private static async Task<string> SolveCaptchaAsync(byte[] imgBytes, IProgress<string> progress)
        {
            string tempImg = Path.Combine(Path.GetTempPath(), "captcha_temp.png");
            await File.WriteAllBytesAsync(tempImg, imgBytes);

            progress.Report($"Imagem salva em: {tempImg} | Existe: {File.Exists(tempImg)} | Tamanho: {imgBytes.Length}");

            string solverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captcha_solver.exe");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = solverPath,
                Arguments = $"\"{tempImg}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi)!;
            string output = await process.StandardOutput.ReadToEndAsync();
            string error  = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(error))
                progress.Report($"Erro solver: {error}");

            string text = output.Trim().ToUpper();
            progress.Report($"OCR leu: '{text}'");
            return text;
        }
    }
}
