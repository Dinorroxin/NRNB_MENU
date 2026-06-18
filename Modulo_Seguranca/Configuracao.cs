namespace Modulo_Seguranca
{
    // Setando as propriedades com get; set;
    public class ConfiguracaoVigiagua 
    {
        public const string UrlLogin = "https://sisagua.saude.gov.br/sisagua/paginaExterna.jsf";
        public string Email { get; set; } = string.Empty;   // string.Empty; Para não dar aviso de que a propriedade pode ser nula
        public string Senha { get; set; } = string.Empty;
        public string PastaCumprimentoDaDiretrizMensal { get; set; } = string.Empty;
        public string PastaCumprimentoDaDiretrizAnual { get; set; } = string.Empty;
        public string PastaControle { get; set; } = string.Empty;
    }

    public class ConfiguracaoGal
    {
        public const string UrlLogin = "https://gal.rondonia.sus.gov.br";
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Laboratorio { get; set; } = string.Empty;
    }

    public class Configuracao
    {
        public List<string> ExtensoesBloqueadas { get; set; } = []; // Como é uma lista, só dizer que ele é lista vazia
        public ConfiguracaoVigiagua Vigiagua { get; set; } = new ConfiguracaoVigiagua(); // Criando novo objeto
        public ConfiguracaoGal Gal { get; set; } = new ConfiguracaoGal();
    }
}
