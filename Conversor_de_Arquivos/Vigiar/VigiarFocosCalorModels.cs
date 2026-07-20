namespace Conversor_de_Arquivos
{
    public record CityHotspot(string Municipality, string State, int Hotspots);

    public record WeekHotspots(int Year, int Week, DateTime Start, DateTime End, List<CityHotspot> Hotspots);
}
