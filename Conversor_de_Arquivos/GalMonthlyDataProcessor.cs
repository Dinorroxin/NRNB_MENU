using System.Globalization;
using System.Text;
using OfficeOpenXml;
using UglyToad.PdfPig;

namespace Conversor_de_Arquivos
{
    public class ProcessamentoGalMensalResult
    {
        public bool Sucesso { get; set; }
        public string? Erro { get; set; }
        public int LinhasProcessadas { get; set; }
        public int MunicipiosEncontrados { get; set; }
    }

    public class GalMonthlyDataProcessor
    {
        private static readonly Dictionary<string, (string Nome, int Ordem)> _mesMap = new()
        {
            ["Jan"]  = ("Janeiro",    1),
            ["Fev"]  = ("Fevereiro",  2),
            ["Mar"]  = ("Março",      3),
            ["Abr"]  = ("Abril",      4),
            ["Maio"] = ("Maio",       5),
            ["Jun"]  = ("Junho",      6),
            ["Jul"]  = ("Julho",      7),
            ["Ago"]  = ("Agosto",     8),
            ["Set"]  = ("Setembro",   9),
            ["Out"]  = ("Outubro",   10),
            ["Nov"]  = ("Novembro",  11),
            ["Dez"]  = ("Dezembro",  12),
        };

        private static readonly Dictionary<string, int> _ordemPorNome =
            _mesMap.ToDictionary(kv => kv.Value.Nome, kv => kv.Value.Ordem);

        private sealed class LinhaGal
        {
            public string Municipio { get; init; } = string.Empty;
            public string Regional  { get; init; } = string.Empty;
            public int    Ano       { get; init; }
            public string Mes       { get; init; } = string.Empty;
            public int    OrdemMes  { get; init; }
            public int    Tot       { get; init; }
            public int    Sat       { get; init; }
            public int    Ins       { get; init; }
        }


        public async Task<ProcessamentoGalMensalResult> ProcessarAsync(
            string pathPdf,
            string pathMestre,
            IProgress<string>? progress = null)
        {
            var result = new ProcessamentoGalMensalResult();
            try
            {
                progress?.Report("Lendo PDF...");
                var linhasPdf = ExtrairLinhasPdf(pathPdf);

                progress?.Report("Identificando meses do cabeçalho...");
                var meses = ExtrairMeses(linhasPdf);
                if (meses.Count == 0)
                    throw new InvalidDataException(
                        "Não foi possível identificar os meses no cabeçalho do PDF.");

                progress?.Report(
                    $"Meses: {string.Join(", ", meses.Select(m => m.Abrev + "/" + m.Ano))}");

                progress?.Report("Parseando municípios...");
                int numValores = meses.Count * 3; // e.g. 12 months → 36 values
                var registros = ParsearMunicipios(linhasPdf, meses, numValores);
                result.MunicipiosEncontrados = registros.Count;

                progress?.Report($"Encontrados {registros.Count} municípios. Gerando linhas...");
                var novasLinhas = GerarLinhasLong(registros, meses);
                result.LinhasProcessadas = novasLinhas.Count;

                progress?.Report("Atualizando arquivo mestre...");
                await AtualizarMestreAsync(pathMestre, novasLinhas, meses, progress);

                result.Sucesso = true;
                progress?.Report("Concluído.");
            }
            catch (Exception ex)
            {
                result.Sucesso = false;
                result.Erro = ex.Message;
            }
            return result;
        }

        private static List<string> ExtrairLinhasPdf(string pathPdf)
        {
            var allLines = new List<string>();
            using var document = PdfDocument.Open(pathPdf);

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                if (words.Count == 0) continue;


                var grupos = words
                    .GroupBy(w => (int)Math.Round(w.BoundingBox.Bottom))
                    .OrderByDescending(g => g.Key)
                    .Select(g =>
                        string.Join(" ",
                            g.OrderBy(w => w.BoundingBox.Left)
                             .Select(w => LimparTexto(w.Text))
                             .Where(t => !string.IsNullOrWhiteSpace(t))))
                    .Where(l => !string.IsNullOrWhiteSpace(l));

                allLines.AddRange(grupos);
            }
            return allLines;
        }

        private static List<(string Abrev, string Nome, int Ano, int Ordem)> ExtrairMeses(
            List<string> linhas)
        {
            foreach (var linha in linhas)
            {
                var tokens = linha.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var encontrados = new List<(string Abrev, string Nome, int Ano, int Ordem)>();

                foreach (var token in tokens)
                {
                    int sep = token.IndexOf('/');
                    if (sep <= 0) continue;

                    string abrev  = token[..sep];
                    string anoStr = token[(sep + 1)..];

                    if (_mesMap.TryGetValue(abrev, out var info)
                        && int.TryParse(anoStr, out int ano))
                    {
                        encontrados.Add((abrev, info.Nome, ano, info.Ordem));
                    }
                }

                // A genuine month-header line will have at least 2 month/year tokens.
                if (encontrados.Count >= 2)
                    return encontrados;
            }
            return [];
        }

        private static List<(string Municipio, int[] Valores)> ParsearMunicipios(
            List<string> linhas,
            List<(string Abrev, string Nome, int Ano, int Ordem)> meses,
            int numValores)
        {
            var result      = new List<(string Municipio, int[] Valores)>();
            string preBuffer = string.Empty;   // text fragments before first pending record
            bool inData      = false;

            string? pendingMunicipio = null;
            int[]?  pendingValores   = null;

            void CommitPending()
            {
                if (pendingMunicipio is null) return;
                string nome = CanonizarMunicipio(NormalizarNomeMunicipio(pendingMunicipio));
                if (!string.IsNullOrWhiteSpace(nome)
                    && !nome.Equals("TOTAL", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add((nome, pendingValores!));
                }
                pendingMunicipio = null;
                pendingValores   = null;
            }

            foreach (var rawLinha in linhas)
            {
                string linha = rawLinha.Trim();
                if (string.IsNullOrWhiteSpace(linha)) continue;

                if (!inData)
                {
                    if (EhLinhaMeses(linha)) inData = true;
                    continue;
                }

                // Page N+1 month header — keep pendingMunicipio alive across page break;
                // only reset the pre-data fragment accumulator.
                if (EhLinhaMeses(linha)) { preBuffer = string.Empty; continue; }

                if (EhLinhaIgnorada(linha)) continue;

                var tokens = linha.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // TOT/SAT/INS sub-header. May carry a municipality name fragment at the
                // right edge (e.g. "… TOT SAT IN ALTA FLORESTA") when PdfPig groups the
                // column header and the first data-row name at the same Y-coordinate.
                if (EhCabecalhoTotSatIns(tokens, out string fragmento))
                {
                    if (!string.IsNullOrWhiteSpace(fragmento))
                    {
                        CommitPending();
                        preBuffer = fragmento;
                    }
                    continue;
                }

                int firstNum = EncontrarPrimeiroNumero(tokens);

                if (firstNum == -1)
                {
                    // Name-only line: no numeric token found.
                    if (!tokens.Any(t => t.Any(char.IsLetter))) continue;

                    if (pendingMunicipio != null)
                    {
                        // Continuation of the pending municipality (e.g. "D'OESTE" after
                        // "ALTA FLORESTA 12 7 5…"). Extend the name instead of orphaning it.
                        pendingMunicipio = (pendingMunicipio + " " + linha.Trim()).Trim();
                    }
                    else
                    {
                        preBuffer = preBuffer.Length == 0
                            ? linha.Trim()
                            : preBuffer + " " + linha.Trim();
                    }
                    continue;
                }

                // Line with numbers: commit the previous pending and open a new one.
                CommitPending();

                string thisName = firstNum > 0
                    ? string.Join(" ", tokens.Take(firstNum)).Trim()
                    : string.Empty;

                string fullName;
                if (preBuffer.Length > 0)
                {
                    fullName = thisName.Length > 0
                        ? (preBuffer.Trim() + " " + thisName).Trim()
                        : preBuffer.Trim();
                    preBuffer = string.Empty;
                }
                else
                {
                    fullName = thisName;
                }

                if (string.IsNullOrWhiteSpace(fullName)) continue;

                var nums = new List<int>();
                for (int i = firstNum; i < tokens.Length; i++)
                {
                    if (int.TryParse(tokens[i], out int v))
                        nums.Add(v);
                }

                if (nums.Count >= numValores)
                {
                    pendingMunicipio = fullName;
                    pendingValores   = [.. nums.Take(numValores)];
                }
            }

            // Flush the last municipality (not followed by another numbers-line).
            CommitPending();

            return result;
        }

        // Matches the parsed municipality name against every entry in RondoniaRegionais.RegionalMap
        // and returns the canonical form if a sufficiently strong match is found.
        // Score = matched_words² / canonical_word_count.  Threshold ≥ 1.0 avoids false positives
        // from orphaned single-word fragments (e.g. a stray "D'OESTE").
        private static string CanonizarMunicipio(string nomeParsed)
        {
            if (string.IsNullOrWhiteSpace(nomeParsed)) return nomeParsed;

            // Normalise for comparison: remove accents, upper-case, replace ' and - with space.
            static string Norm(string s) =>
                RemoverAcentos(s).ToUpperInvariant()
                    .Replace('\'', ' ').Replace('-', ' ');

            var parsedWords = new HashSet<string>(
                Norm(nomeParsed).Split(' ', StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);

            if (parsedWords.Count == 0) return nomeParsed;

            string? bestKey  = null;
            double  bestScore = 0;

            foreach (var (key, _) in RondoniaRegionais.RegionalMap)
            {
                string[] keyWords = Norm(key)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                int matched = keyWords.Count(w => parsedWords.Contains(w));
                if (matched == 0) continue;

                double score = (double)(matched * matched) / keyWords.Length;
                if (score > bestScore) { bestScore = score; bestKey = key; }
            }

            return bestKey != null && bestScore >= 1.0 ? bestKey : nomeParsed;
        }

        private static bool EhLinhaMeses(string linha)
        {
            int found = 0;
            foreach (var token in linha.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                int sep = token.IndexOf('/');
                if (sep <= 0) continue;
                if (_mesMap.ContainsKey(token[..sep])
                    && int.TryParse(token[(sep + 1)..], out _))
                    found++;
            }
            return found >= 2;
        }

        private static bool EhLinhaIgnorada(string linha) =>
            linha.StartsWith("Relatório gerado em",         StringComparison.OrdinalIgnoreCase) ||
            linha.StartsWith("Período:",                    StringComparison.OrdinalIgnoreCase) ||
            linha.StartsWith("Site:",                       StringComparison.OrdinalIgnoreCase) ||
            linha.StartsWith("E-mail:",                    StringComparison.OrdinalIgnoreCase) ||
            linha.StartsWith("Telefone:",                  StringComparison.OrdinalIgnoreCase) ||
            linha.StartsWith("Governo",                    StringComparison.OrdinalIgnoreCase) ||
            linha.StartsWith("Secretaria",                 StringComparison.OrdinalIgnoreCase) ||
            linha.StartsWith("SESAU",                      StringComparison.OrdinalIgnoreCase) ||
            linha.StartsWith("LACEN",                      StringComparison.OrdinalIgnoreCase) ||
            linha.StartsWith("Relatório de Acompanhamento",StringComparison.OrdinalIgnoreCase) ||
            linha.Contains("Por Data de Cadastro",         StringComparison.OrdinalIgnoreCase) ||
            linha.Contains("lacen.ro.gov.br",              StringComparison.OrdinalIgnoreCase) ||
            linha.Contains("@");  // qualquer endereço de e-mail

        // Returns true when the line is the TOT/SAT/INS column sub-header.
        // Accepts "IN" as a truncated form of "INS" (PDF clips the rightmost column label).
        // Requires at least 3 such tokens — avoids false-positive on municipality words.
        // 'fragmento' receives any letter-containing tokens that appear AFTER the last
        // TSI token (municipality name text caught on the same PDF line as the header).
        private static bool EhCabecalhoTotSatIns(string[] tokens, out string fragmento)
        {
            fragmento = string.Empty;
            if (tokens.Length < 3) return false;

            static bool IsTsi(string t) =>
                t.Equals("TOT", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("SAT", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("INS", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("IN",  StringComparison.OrdinalIgnoreCase);

            int tsiCount  = 0;
            int lastTsiIdx = -1;
            for (int i = 0; i < tokens.Length; i++)
            {
                if (IsTsi(tokens[i])) { tsiCount++; lastTsiIdx = i; }
            }

            if (tsiCount < 3) return false;

            // Collect any letter-bearing tokens after the last TSI token.
            if (lastTsiIdx < tokens.Length - 1)
            {
                var nameTokens = new List<string>();
                for (int i = lastTsiIdx + 1; i < tokens.Length; i++)
                    if (tokens[i].Any(char.IsLetter)) nameTokens.Add(tokens[i]);
                if (nameTokens.Count > 0)
                    fragmento = string.Join(" ", nameTokens);
            }

            return true;
        }

        private static int EncontrarPrimeiroNumero(string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
                if (int.TryParse(tokens[i], out _)) return i;
            return -1;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Long-format row generation
        // ──────────────────────────────────────────────────────────────────────

        private static List<LinhaGal> GerarLinhasLong(
            List<(string Municipio, int[] Valores)> registros,
            List<(string Abrev, string Nome, int Ano, int Ordem)> meses)
        {
            var linhas = new List<LinhaGal>();

            foreach (var (municipio, valores) in registros)
            {
                string chave    = RemoverAcentos(municipio.ToUpper());
                string regional = RondoniaRegionais.ObterRegional(chave);

                for (int m = 0; m < meses.Count; m++)
                {
                    int baseIdx = m * 3;
                    if (baseIdx + 2 >= valores.Length) break;

                    linhas.Add(new LinhaGal
                    {
                        Municipio = municipio,
                        Regional  = regional,
                        Ano       = meses[m].Ano,
                        Mes       = meses[m].Nome,
                        OrdemMes  = meses[m].Ordem,
                        Tot       = valores[baseIdx],
                        Sat       = valores[baseIdx + 1],
                        Ins       = valores[baseIdx + 2],
                    });
                }
            }

            return linhas;
        }

        private static async Task AtualizarMestreAsync(
            string pathMestre,
            List<LinhaGal> novasLinhas,
            List<(string Abrev, string Nome, int Ano, int Ordem)> meses,
            IProgress<string>? progress)
        {
            static int LerInt(ExcelWorksheet ws, int row, int col)
            {
                object? v = ws.Cells[row, col].Value;
                return v switch
                {
                    int    i => i,
                    double d => (int)Math.Round(d),
                    _        => int.TryParse(ws.Cells[row, col].Text, out int n) ? n : 0,
                };
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var anoMesPdf = new HashSet<(int Ano, string Mes)>(
                meses.Select(m => (m.Ano, m.Nome)));

            var linhasExistentes = new List<LinhaGal>();

            if (File.Exists(pathMestre))
            {
                progress?.Report("Lendo linhas existentes do mestre...");
                using var pkgRead = new ExcelPackage(new FileInfo(pathMestre));
                var wsRead   = pkgRead.Workbook.Worksheets[0];
                int lastRow  = wsRead.Dimension?.End.Row ?? 1;

                for (int r = 2; r <= lastRow; r++)
                {
                    string municipio = wsRead.Cells[r, 1].Text;
                    if (string.IsNullOrWhiteSpace(municipio)) continue;

                    string regional = wsRead.Cells[r, 2].Text;
                    int    ano      = LerInt(wsRead, r, 3);
                    string mes      = wsRead.Cells[r, 4].Text;

                    if (ano == 0 || string.IsNullOrWhiteSpace(mes)) continue;

                    if (anoMesPdf.Contains((ano, mes))) continue;

                    _ordemPorNome.TryGetValue(mes, out int ordemMes);

                    linhasExistentes.Add(new LinhaGal
                    {
                        Municipio = municipio,
                        Regional  = regional,
                        Ano       = ano,
                        Mes       = mes,
                        OrdemMes  = ordemMes,
                        Tot       = LerInt(wsRead, r, 5),
                        Sat       = LerInt(wsRead, r, 6),
                        Ins       = LerInt(wsRead, r, 7),
                    });
                }
            }

            var todasLinhas = linhasExistentes
                .Concat(novasLinhas)
                .OrderBy(l => l.Municipio)
                .ThenBy(l => l.Ano)
                .ThenBy(l => l.OrdemMes)
                .ToList();

            progress?.Report($"Salvando {todasLinhas.Count} linhas no mestre...");

            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Dados");

            ws.Cells[1, 1].Value = "Município";
            ws.Cells[1, 2].Value = "Regional";
            ws.Cells[1, 3].Value = "Ano";
            ws.Cells[1, 4].Value = "Mês";
            ws.Cells[1, 5].Value = "TOT";
            ws.Cells[1, 6].Value = "SAT";
            ws.Cells[1, 7].Value = "INS";

            for (int r = 0; r < todasLinhas.Count; r++)
            {
                var l   = todasLinhas[r];
                int row = r + 2;
                ws.Cells[row, 1].Value = l.Municipio;
                ws.Cells[row, 2].Value = l.Regional;
                ws.Cells[row, 3].Value = l.Ano;
                ws.Cells[row, 4].Value = l.Mes;
                ws.Cells[row, 5].Value = l.Tot;
                ws.Cells[row, 6].Value = l.Sat;
                ws.Cells[row, 7].Value = l.Ins;
            }

            if (ws.Dimension != null)
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

            await pkg.SaveAsAsync(new FileInfo(pathMestre));
        }

        // Strips control characters (including \n, \r) from a single PDF word.
        private static string LimparTexto(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            var sb = new StringBuilder(texto.Length);
            foreach (char c in texto)
                if (!char.IsControl(c)) sb.Append(c);
            return sb.ToString();
        }

        // Normalizes municipality names:
        //  • strips control chars
        //  • collapses any run of apostrophe-like characters (with optional spaces between
        //    them) into a single ASCII apostrophe — handles D''OESTE, D"OESTE, D ' ' OESTE
        //  • removes the space that sometimes appears right before or after the apostrophe
        private static string NormalizarNomeMunicipio(string nome)
        {
            var sb        = new StringBuilder(nome.Length);
            bool prevApas = false;   // last meaningful char was an apostrophe
            bool prevSpc  = false;   // last char appended was a space

            foreach (char c in nome)
            {
                if (char.IsControl(c)) { prevApas = false; prevSpc = false; continue; }

                bool isApas = c is '\'' or '"'
                    or '‘' or '’'   // ' '
                    or '“' or '”'   // " "
                    or '′' or '″'   // ′ ″
                    or 'ʼ' or '´';  // ʼ ´

                if (isApas)
                {
                    // Remove the trailing space that was written before this apostrophe
                    // (handles "NOME D 'OESTE" → "NOME D'OESTE").
                    if (prevSpc && sb.Length > 0 && sb[sb.Length - 1] == ' ')
                        sb.Length--;

                    if (!prevApas)
                        sb.Append('\'');

                    prevApas = true;
                    prevSpc  = false;
                }
                else if (c == ' ')
                {
                    if (!prevApas)  // skip space immediately after an apostrophe
                    {
                        sb.Append(' ');
                        prevSpc = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    prevApas = false;
                    prevSpc  = false;
                }
            }

            return sb.ToString().Trim();
        }

        private static string RemoverAcentos(string texto)
        {
            string normalizado = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalizado.Length);
            foreach (char c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
