using Microsoft.Playwright;
using System.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WebAutomation
{
    public class GalMonthlyAutomation
    {
        public async Task<bool> BaixarRelatorioMensalAsync(
            string usuario,
            string senha,
            string modulo,
            string laboratorio,
            string pastaDownload,
            string referencia,
            string dataInicio,
            string dataFim,
            string codIbge,
            IProgress<string> progress)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--ignore-certificate-errors",
                    "--ignore-ssl-errors",
                    "--allow-insecure-localhost"
                }
            });

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            bool loginSucesso = false;
            int tentativa = 1;
            const int maxTentativas = 100;

            while (tentativa <= maxTentativas && !loginSucesso)
            {
                try
                {
                    progress.Report($"Tentativa {tentativa} — carregando página...");
                    await page.GotoAsync("https://gal.rondonia.sus.gov.br/");

                    // Usuário e Senha
                    await page.FillAsync("[name='login']", usuario);
                    await page.FillAsync("[name='senha']", senha);

                    // Módulo
                    await page.FillAsync("[name='modulo']", modulo);
                    await page.Keyboard.PressAsync("Enter");
                    await page.WaitForTimeoutAsync(1500);

                    // Laboratório (TAB → limpa → digita → Enter)
                    await page.Keyboard.PressAsync("Tab");
                    await page.WaitForTimeoutAsync(500);
                    await page.Keyboard.PressAsync("Backspace");
                    await page.WaitForTimeoutAsync(300);
                    await page.Keyboard.TypeAsync(laboratorio);
                    await page.WaitForTimeoutAsync(500);
                    await page.Keyboard.PressAsync("Enter");

                    progress.Report($"Tentativa {tentativa} — clicando em Entrar...");

                    // Clica em Entrar para exibir o captcha
                    await page.ClickAsync("//button[text()='Entrar']");

                    // Aguarda imagem do captcha
                    var captchaImg = await page.WaitForSelectorAsync("img[src*='captcha']");
                    await page.WaitForTimeoutAsync(1000);

                    // Captura a imagem como bytes e converte para base64
                    var imgBytes = await captchaImg!.ScreenshotAsync();

                    // Resolve captcha via Claude Vision API
                    string txtCaptcha = await ResolverCaptchaAsync(imgBytes, progress);
                    progress.Report($"Tentativa {tentativa} — captcha lido: {txtCaptcha}");

                    // Insere captcha
                    var inputCap = await page.QuerySelectorAsync("//*[contains(text(), 'Informe o código')]/following::input[1]");
                    await page.EvaluateAsync("(el, val) => el.value = val", new object[] { inputCap!, txtCaptcha });

                    // Confirmar
                    await page.EvaluateAsync("document.querySelector(\"button[text()='Confirmar']\")?.click() ?? document.evaluate(\"//button[text()='Confirmar']\", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue?.click()");
                    await page.WaitForTimeoutAsync(5000);

                    // Verifica se login funcionou (campo login sumiu)
                    var loginField = await page.QuerySelectorAsync("[name='login']");
                    if (loginField == null)
                    {
                        progress.Report("Login realizado com sucesso.");
                        loginSucesso = true;
                    }
                    else
                    {
                        progress.Report($"Captcha incorreto — tentativa {tentativa}");
                        tentativa++;
                    }
                }
                catch (Exception ex)
                {
                    progress.Report($"Erro na tentativa {tentativa}: {ex.Message}");
                    tentativa++;
                    await page.WaitForTimeoutAsync(2000);
                }
            }

            if (!loginSucesso)
            {
                progress.Report("Falha: número máximo de tentativas atingido.");
                return false;
            }

            try
            {
                // --- NAVEGAÇÃO PÓS-LOGIN (ÁRVORE) ---
                async Task ClicarNaArvore(string textoAlvo, bool ehPasta)
                {
                    string xpath = ehPasta
                        ? $"//span[text()='{textoAlvo}']/../../img[contains(@class, 'x-tree-ec-icon')]"
                        : $"//span[text()='{textoAlvo}']";

                    var elemento = await page.WaitForSelectorAsync($"xpath={xpath}");
                    await page.EvaluateAsync("el => el.scrollIntoView({block: 'center'})", elemento);
                    await page.WaitForTimeoutAsync(1000);
                    await page.EvaluateAsync("el => el.click()", elemento);
                    await page.WaitForTimeoutAsync(2000);
                }

                progress.Report("Navegando na árvore...");
                await ClicarNaArvore("Ambiental", true);
                await ClicarNaArvore("Relatórios", true);
                await ClicarNaArvore("Gestão", false);

                // Entra no iframe
                var frame = await page.WaitForSelectorAsync("#content-panel");
                var contentFrame = await frame!.ContentFrameAsync();

                // Double-click no relatório Acompanhamento
                progress.Report("Selecionando relatório...");
                var gridItem = await contentFrame!.WaitForSelectorAsync(
                    "xpath=//div[contains(@class, 'x-grid3-cell-inner') and contains(text(), 'Acompanhamento')]");
                await contentFrame.EvaluateAsync(
                    "el => { var ev = new MouseEvent('dblclick', {bubbles:true}); el.dispatchEvent(ev); }",
                    gridItem);

                // Referência
                progress.Report("Preenchendo filtros...");
                var trigger = await contentFrame.WaitForSelectorAsync(
                    "xpath=//label[contains(text(), 'Referência')]/following::img[1]");
                await trigger!.ClickAsync();
                await contentFrame.ClickAsync($"xpath=//div[text()='{referencia}']");

                // Datas e IBGE
                var campos = new (string Label, string Valor)[]
                {
                    ("Início", dataInicio),
                    ("Fim",    dataFim),
                    ("Cód. IBGE", codIbge)
                };

                foreach (var (label, valor) in campos)
                {
                    var campo = await contentFrame.QuerySelectorAsync(
                        $"xpath=//label[contains(text(), '{label}')]/following::input[1]");
                    await contentFrame.EvaluateAsync("el => { el.value = ''; }", campo);
                    await campo!.FillAsync(valor);
                    await page.Keyboard.PressAsync("Tab");
                    await page.WaitForTimeoutAsync(1000);
                }

                // Gerar PDF
                progress.Report("Gerando PDF...");
                var btnGerar = await contentFrame.QuerySelectorAsync("xpath=//button[text()='Gerar']");
                await contentFrame.EvaluateAsync("el => el.click()", btnGerar);

                // Nova aba com o PDF
                var newPage = await context.WaitForPageAsync();
                await newPage.WaitForSelectorAsync("xpath=//*[contains(text(), 'Município')]");

                // Imprime como PDF via Playwright (sem abrir visualizador)
                var pdfBytes = await newPage.PdfAsync(new PagePdfOptions
                {
                    PrintBackground = true,
                    PreferCSSPageSize = true
                });

                string dIni = dataInicio.Replace("/", "");
                string dFim = dataFim.Replace("/", "");
                string nomeArquivo = $"Relatorio_GAL_{dIni}_a_{dFim}.pdf";
                string caminhoFinal = Path.Combine(pastaDownload, nomeArquivo);

                await File.WriteAllBytesAsync(caminhoFinal, pdfBytes);

                progress.Report($"PDF salvo: {nomeArquivo}");
                return true;
            }
            catch (Exception ex)
            {
                progress.Report($"Erro no fluxo pós-login: {ex.Message}");
                return false;
            }
        }

        private static Task<string> ResolverCaptchaAsync(byte[] imgBytes, IProgress<string> progress)
        {
            string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

            using var engine = new Tesseract.TesseractEngine(tessDataPath, "por", Tesseract.EngineMode.Default);

            // Captcha é N,L,N,L,N — só alfanuméricos, sem espaços
            engine.SetVariable("tessedit_char_whitelist", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");

            using var img = Tesseract.Pix.LoadFromMemory(imgBytes);
            using var page = engine.Process(img);

            string texto = page.GetText()
                .Replace(" ", "")
                .Replace("\n", "")
                .Trim()
                .ToUpper();

            progress.Report($"OCR leu: {texto}");
            return Task.FromResult(texto);
        }
    }
}