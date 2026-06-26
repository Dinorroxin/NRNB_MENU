using Microsoft.Playwright;

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
                Headless = false,
                Args = new[]
                {
                    "--ignore-certificate-errors",
                    "--ignore-ssl-errors",
                    "--allow-insecure-localhost"
                }
            });

            var context = await browser.NewContextAsync();

            // Suprime o window.print() nativo no nível do contexto.
            // O GAL abre a aba do relatório com window.open() e dispara window.print()
            // no onload — antes do WaitForPageAsync retornar. Registrar aqui garante
            // que o override seja injetado em qualquer aba nova antes do onload executar.
            await context.AddInitScriptAsync("window.print = () => {};");

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

                    // Usuário e Senha
                    await page.FillAsync("[name='login']", usuario);
                    await page.FillAsync("[name='senha']", senha);

                    // Módulo
                    await page.ClickAsync("[name='modulo']");
                    await page.WaitForTimeoutAsync(300);
                    await page.Keyboard.TypeAsync(modulo);
                    await page.WaitForTimeoutAsync(1000);
                    await page.Keyboard.PressAsync("Enter");
                    await page.WaitForTimeoutAsync(2500);

                    // Laboratório — TAB para ir ao próximo campo, limpa e digita
                    await page.Keyboard.PressAsync("Tab");
                    await page.WaitForTimeoutAsync(500);
                    await page.Keyboard.PressAsync("Control+A");
                    await page.Keyboard.PressAsync("Backspace");
                    await page.WaitForTimeoutAsync(300);
                    await page.Keyboard.TypeAsync(laboratorio);
                    await page.WaitForTimeoutAsync(800);
                    await page.Keyboard.PressAsync("Enter");
                    await page.WaitForTimeoutAsync(1000);
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
                    await page.ClickAsync("[name='ext-comp-1031']");
                    await page.WaitForTimeoutAsync(300);
                    await page.Keyboard.TypeAsync(txtCaptcha);
                    await page.WaitForTimeoutAsync(500);

                    // Confirmar — clica no botão via XPath
                    await page.ClickAsync("xpath=//button[text()='Confirmar']");
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

                        await page.GotoAsync("about:blank");
                        await page.WaitForTimeoutAsync(500);
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

                progress.Report("Gerando PDF...");
                var btnGerar = await contentFrame.QuerySelectorAsync("xpath=//button[text()='Gerar']");
                await contentFrame.EvaluateAsync("el => el.click()", btnGerar);

                // Nova aba com o PDF — timeout maior para períodos longos
                progress.Report("Aguardando nova aba...");
                var newPage = await context.WaitForPageAsync(new BrowserContextWaitForPageOptions
                {
                    Timeout = 60000
                });

                // Neutraliza o window.print() automático da nova aba
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
        private static async Task<string> ResolverCaptchaAsync(byte[] imgBytes, IProgress<string> progress)
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
            string resultado = await process.StandardOutput.ReadToEndAsync();
            string erro = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(erro))
                progress.Report($"Erro solver: {erro}");

            string texto = resultado.Trim().ToUpper();
            progress.Report($"OCR leu: '{texto}'");
            return texto;
        }

        private static string ForcarPadrao(string texto)
        {
            // Remove caracteres não alfanuméricos
            string limpo = new string(texto.Where(char.IsLetterOrDigit).ToArray());

            if (limpo.Length != 5)
                return limpo; // retorna como está se não tiver 5 chars

            char[] resultado = limpo.ToCharArray();

            // Posições 0,2,4 = número; posições 1,3 = letra
            for (int i = 0; i < 5; i++)
            {
                if (i % 2 == 0) // deve ser número
                {
                    if (!char.IsDigit(resultado[i]))
                    {
                        // Tenta corrigir confusões comuns: O→0, I→1, S→5, B→8, Z→2
                        resultado[i] = CorrigirParaNumero(resultado[i]);
                    }
                }
                else // deve ser letra
                {
                    if (!char.IsLetter(resultado[i]))
                    {
                        // Tenta corrigir confusões comuns: 0→O, 1→I, 5→S, 8→B
                        resultado[i] = CorrigirParaLetra(resultado[i]);
                    }
                }
            }

            return new string(resultado);
        }

        private static char CorrigirParaNumero(char c) => c switch
        {
            'O' or 'Q' => '0',
            'I' or 'L' => '1',
            'Z' => '2',
            'S' => '5',
            'B' => '8',
            'G' => '6',
            _ => '0' // fallback
        };

        private static char CorrigirParaLetra(char c) => c switch
        {
            '0' => 'O',
            '1' => 'I',
            '5' => 'S',
            '8' => 'B',
            '6' => 'G',
            _ => 'A' // fallback
        };
    }
}