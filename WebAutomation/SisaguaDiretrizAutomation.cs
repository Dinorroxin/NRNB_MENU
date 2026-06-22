using Microsoft.Playwright;
using Modulo_Seguranca;

namespace WebAutomation
{
    public class SisaguaDiretrizResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> DownloadedFiles { get; set; } = [];
    }


    public class SisaguaDiretrizAutomation
    {
        public async Task<SisaguaDiretrizResult> BaixarRelatoriosMensaisAsync(
            string email,
            string senha,
            string pastaDestino)
        {
            var result = new SisaguaDiretrizResult();

            try
            {
                // 1. Abre navegador (Playwright)
                // 2. Login usando ConfiguracaoVigiagua.UrlLogin
                // 3. Navega pra UrlDiretrizMensal
                // 4. Seleciona Abrangência + Mensal
                // 5. Loop pelos 3 parâmetros: seleciona, gera, espera download, salva renomeado
                // 6. Fecha navegador

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