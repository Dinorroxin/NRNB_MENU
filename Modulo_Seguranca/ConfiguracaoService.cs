using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;

namespace Modulo_Seguranca
{
    public class ConfiguracaoService
    {
        public Configuracao Carregar() 
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

        public List<string> Validar(Configuracao config)
        {
            // Cria a lista erros
            List<string> erros = [];

            // Adiciona um dicionário com as chaves e mensagens de erro para cada campo obrigatório
            var fields = new Dictionary<string, string>
            {
                {config.Vigiagua.Email, "Email do Vigiagua não informado"},
                {config.Vigiagua.Senha, "Senha do Vigiagua não informada"},
                {config.Gal.Usuario, "Usuário do Gal não informado"},
                {config.Gal.Senha, "Senha do Gal não informada"},
                {config.Gal.Modulo, "Módulo do Gal não informado" },
                {config.Gal.Laboratorio, "Laboratório do Gal não informado"}
            };

            // Para cada índice em fields, se a chave for vazia, ele vai retornar o valor daquuela chave que é a mensagem de erro
            foreach (var i in fields) 
            {
                if (string.IsNullOrEmpty(i.Key))
                {
                    erros.Add(i.Value);
                }
            }
            // Retorna a lista de erros
            return erros;
        }
    }
}
