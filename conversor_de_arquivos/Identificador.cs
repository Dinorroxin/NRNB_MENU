using System;
using System.Collections.Generic;
using System.Text;

namespace Conversor_de_Arquivos 
{
    public class Identificador  
    {
        public string VerificarTipo(string path) 
        {
            if (string.IsNullOrEmpty(path)) 
                return "Caminho vazio";
            string ext = System.IO.Path.GetExtension(path).ToLower();

            return $"O tipo do arquivo é {ext}";

        }
    }
}
