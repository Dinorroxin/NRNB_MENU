namespace Conversor_de_Arquivos
{
    public record FocoCidade(string Municipio, string Estado, int Focos);

    public record SemanaFocos(int Ano, int Semana, DateTime Inicio, DateTime Fim, List<FocoCidade> Focos);
}
