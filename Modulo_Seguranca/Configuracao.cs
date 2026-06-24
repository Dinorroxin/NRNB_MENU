namespace Modulo_Seguranca
{
    // Setando as propriedades com get; set;
    public class ConfiguracaoVigiagua 
    {
        public const string UrlLogin = "https://sisagua.saude.gov.br/sisagua/paginaExterna.jsf";
        public const string UrlDiretrizMensal = "https://sisagua.saude.gov.br/sisagua/paginas/seguro/relatorioDiretrizNacional/relDiretrizNacionalParametrosBasicos.jsf?faces-redirect=true";
        public string Email { get; set; } = string.Empty;   // string.Empty; Para não dar aviso de que a propriedade pode ser nula
        public string Senha { get; set; } = string.Empty;
        public string PastaArquivosBrutos { get; set; } = string.Empty;
        public string PastaCumprimentoDaDiretrizMensal { get; set; } = string.Empty;
        public string PastaCumprimentoDaDiretrizAnual { get; set; } = string.Empty;
        public string PastaControle { get; set; } = string.Empty;
    }

    public class ConfiguracaoVigiar
    {
        public const string UrlIqAr = "https://shiny.icict.fiocruz.br/alertarsaude/";
        public const string UrlBdQueimadas = "https://terrabrasilis.dpi.inpe.br/queimadas/bdqueimadas/#exportar-dados";
        public string PastaArquivosBrutos { get; set; } = string.Empty;
        public string PastaQueimadas { get; set; } = string.Empty;
        public string PastaIqAr { get; set; } = string.Empty;
    }

    public class ConfiguracaoGal
    {
        public const string UrlLogin = "https://gal.rondonia.sus.gov.br";
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Laboratorio { get; set; } = string.Empty;
        public string PastaBrutaRelaotriosMensalGal { get; set; } = string.Empty;
        public string PastaTratadaRelatorioMensalGal{ get; set; } = string.Empty;
    }

    public class Configuracao
    {
        public List<string> ExtensoesBloqueadas { get; set; } = []; // Como é uma lista, só dizer que ele é lista vazia
        public ConfiguracaoVigiagua Vigiagua { get; set; } = new ConfiguracaoVigiagua(); // Criando novo objeto
        public ConfiguracaoGal Gal { get; set; } = new ConfiguracaoGal();
        public ConfiguracaoVigiar Vigiar { get; set; } = new ConfiguracaoVigiar();
    }
}