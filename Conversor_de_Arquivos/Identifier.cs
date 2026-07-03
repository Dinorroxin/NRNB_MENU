namespace Conversor_de_Arquivos
{
    public class Identifier
    {
        public string CheckType(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "Caminho vazio";
            string ext = System.IO.Path.GetExtension(path).ToLower();

            return $"O tipo do arquivo é {ext}";
        }
    }
}
