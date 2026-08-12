namespace Conversor_de_Arquivos.Models
{
    public enum SisamPoluente { Pm25, Pm10, O3, No2, So2, Co }

    public static class SisamPoluenteExtensions
    {
        public static string ToUrlSegment(this SisamPoluente p) => p switch
        {
            SisamPoluente.Pm25 => "pm25",
            SisamPoluente.Pm10 => "pm10",
            SisamPoluente.O3   => "o3",
            SisamPoluente.No2  => "no2",
            SisamPoluente.So2  => "so2",
            SisamPoluente.Co   => "co",
            _                  => throw new ArgumentOutOfRangeException(nameof(p))
        };

        public static string ToDisplayName(this SisamPoluente p) => p switch
        {
            SisamPoluente.Pm25 => "MP2.5",
            SisamPoluente.Pm10 => "MP10",
            SisamPoluente.O3   => "O3",
            SisamPoluente.No2  => "NO2",
            SisamPoluente.So2  => "SO2",
            SisamPoluente.Co   => "CO",
            _                  => throw new ArgumentOutOfRangeException(nameof(p))
        };
    }

    public record SisamNivel(string Nome, string Descricao, string CodigoCor);
    public record SisamFaixa(double Min, double Max, SisamNivel Nivel);
    public record SisamRecord(
        string  Data,
        string  Estado,
        string? Municipio,
        double  Valor,
        string  ClassificacaoNome,
        string  ClassificacaoDescricao,
        string  Poluente,
        int     DiasPrevisao);
}
