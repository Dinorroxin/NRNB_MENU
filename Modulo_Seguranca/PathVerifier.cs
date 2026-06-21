namespace Modulo_Seguranca
{
    public static class PathVerifier
    {
        // Recebe somente as pastas relevantes para a automação chamada.
        // Cada item: (Path = caminho configurado, Message = mensagem de erro caso esteja vazio ou não exista)
        public static List<string> Verify(List<(string Path, string Message)> paths)
        {
            var errors = new List<string>();

            foreach (var (path, message) in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    errors.Add(message);
                    continue;
                }

                if (!Directory.Exists(path))
                    errors.Add($"{message} (pasta não encontrada: {path})");
            }

            return errors;
        }
    }
}
