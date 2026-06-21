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
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                string text = File.ReadAllText(path);
                Configuracao config = JsonSerializer.Deserialize<Configuracao>(text) ?? new Configuracao();
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
    }
}
