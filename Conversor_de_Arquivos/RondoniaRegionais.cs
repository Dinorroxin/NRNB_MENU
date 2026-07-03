namespace Conversor_de_Arquivos
{
    public static class RondoniaRegionais
    {
        public static readonly IReadOnlyDictionary<string, string> RegionalMap = new Dictionary<string, string>
        {
            ["GUAJARA-MIRIM"] = "Madeira Mamoré",
            ["NOVA MAMORE"] = "Madeira Mamoré",
            ["PORTO VELHO"] = "Madeira Mamoré",
            ["ITAPUA DO OESTE"] = "Madeira Mamoré",
            ["CANDEIAS DO JAMARI"] = "Madeira Mamoré",
            ["CAMPO NOVO DE RONDONIA"] = "Vale do Jamari",
            ["BURITIS"] = "Vale do Jamari",
            ["MONTE NEGRO"] = "Vale do Jamari",
            ["CACAULANDIA"] = "Vale do Jamari",
            ["ARIQUEMES"] = "Vale do Jamari",
            ["ALTO PARAISO"] = "Vale do Jamari",
            ["RIO CRESPO"] = "Vale do Jamari",
            ["CUJUBIM"] = "Vale do Jamari",
            ["MACHADINHO D'OESTE"] = "Vale do Jamari",
            ["SAO MIGUEL DO GUAPORE"] = "Central",
            ["ALVORADA D'OESTE"] = "Central",
            ["GOVERNADOR JORGE TEIXEIRA"] = "Central",
            ["JARU"] = "Central",
            ["THEOBROMA"] = "Central",
            ["PRESIDENTE MEDICI"] = "Central",
            ["OURO PRETO DO OESTE"] = "Central",
            ["JI-PARANA"] = "Central",
            ["VALE DO ANARI"] = "Central",
            ["VALE DO PARAISO"] = "Central",
            ["NOVA UNIAO"] = "Central",
            ["TEIXEIROPOLIS"] = "Central",
            ["URUPA"] = "Central",
            ["MIRANTE DA SERRA"] = "Central",
            ["ALTA FLORESTA D'OESTE"] = "Zona da Mata",
            ["ALTO ALEGRE DOS PARECIS"] = "Zona da Mata",
            ["SANTA LUZIA D'OESTE"] = "Zona da Mata",
            ["PARECIS"] = "Zona da Mata",
            ["ROLIM DE MOURA"] = "Zona da Mata",
            ["CASTANHEIRAS"] = "Zona da Mata",
            ["NOVO HORIZONTE DO OESTE"] = "Zona da Mata",
            ["NOVA BRASILANDIA D'OESTE"] = "Zona da Mata",
            ["MINISTRO ANDREAZZA"] = "Café",
            ["SAO FELIPE D'OESTE"] = "Café",
            ["PRIMAVERA DE RONDONIA"] = "Café",
            ["CACOAL"] = "Café",
            ["PIMENTA BUENO"] = "Café",
            ["ESPIGAO D'OESTE"] = "Café",
            ["PIMENTEIRAS DO OESTE"] = "Cone Sul",
            ["CABIXI"] = "Cone Sul",
            ["CEREJEIRAS"] = "Cone Sul",
            ["CORUMBIARA"] = "Cone Sul",
            ["COLORADO DO OESTE"] = "Cone Sul",
            ["CHUPINGUAIA"] = "Cone Sul",
            ["VILHENA"] = "Cone Sul",
            ["COSTA MARQUES"] = "Vale do Guaporé",
            ["SERINGUEIRAS"] = "Vale do Guaporé",
            ["SAO FRANCISCO DO GUAPORE"] = "Vale do Guaporé",
        };

        public static string GetRegion(string municipality)
        {
            string key = municipality.Trim().ToUpper();
            return RegionalMap.TryGetValue(key, out var region) ? region : "Não identificado";
        }
    }
}
