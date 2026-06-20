using System.Text.Json;

namespace Modulo_Seguranca
{
    public class ConfiguracaoService
    {

        public void Salvar(Configuracao config)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(config, options);

            File.WriteAllText(path, json);
        }

        public Configuracao Carregar()
        {
            try
            {
                // Caminho absoluto do arquivo json
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

                // Vai ler o arquivo de configuração
                string text = File.ReadAllText(path);

                // Converte o texto em um objeto da classe Configuracao, guardando as variaveis que estão definidas
                Configuracao config = JsonSerializer.Deserialize<Configuracao>(text) ?? new Configuracao();

                // Retorna o objeto de configuração com as keys do json
                return config;
            }

            catch (FileNotFoundException)
            {
                throw new Exception("Arquivo de configuração não foi encontrado");
            }

            catch (JsonException)
            {
                throw new Exception("Erro, arquivo de configuração está com formato inválido ou dados inválidos");
            }
        }

        public List<string> Validar(Configuracao config)
        {
            // Cria a lista erros
            List<string> erros = [];

            // Adiciona um dicionário com as chaves e mensagens de erro para cada campo obrigatório
            var fields = new List<(string Valor, string Mensagem)>
            {
                (config.Vigiagua.Email, "Email do Vigiagua não informado"),
                (config.Vigiagua.Senha, "Senha do Vigiagua não informada"),
                (config.Vigiagua.PastaArquivosBrutos, "Pasta de arquivos brutos do Vigiagua não informada"),
                (config.Vigiagua.PastaCumprimentoDaDiretrizMensal, "Pasta da Diretriz Mensal não informada"),
                (config.Vigiagua.PastaCumprimentoDaDiretrizAnual, "Pasta da Diretriz Anual não informada"),
                (config.Vigiagua.PastaControle, "Pasta de saída do Controle não informada"),

                (config.Vigiar.PastaArquivosBrutos, "Pasta de arquivos brutos do Vigiar não informada"),
                (config.Vigiar.PastaQueimadas, "Pasta de saída do Queimadas não informada"),
                (config.Vigiar.PastaIqAr, "Pasta de saída do IqAr não informada"),

                (config.Gal.Usuario, "Usuário do Gal não informado"),
                (config.Gal.Senha, "Senha do Gal não informada"),
                (config.Gal.Modulo, "Módulo do Gal não informado"),
                (config.Gal.Laboratorio, "Laboratório do Gal não informado"),
                (config.Gal.PastaArquivosBrutosAmostras, "Pasta de arquivos brutos do GAL não informada"),
                (config.Gal.PastaAmostras, "Pasta de saída do GAL não informada")
            };

            // Para cada índice em fields, se a chave for vazia, ele vai retornar o valor daquela chave que é a mensagem de erro
            foreach (var (valor, mensagem) in fields)
            {
                if (string.IsNullOrEmpty(valor))
                    erros.Add(mensagem);
            }

            // Retorna a lista de erros
            return erros;
            
        }
    }
}
