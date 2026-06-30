using System.Globalization;
using System.Text;
using OfficeOpenXml;

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
                var (meses, registros) = ExtrairComPython(pathPdf, progress);
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

        private static (
            List<(string Abrev, string Nome, int Ano, int Ordem)> Meses,
            List<(string Municipio, int[] Valores)> Registros
        ) ExtrairComPython(string pathPdf, IProgress<string>? progress)
        {
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gal_extract.exe");
            string pyPath  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gal_extract.py");

            string procFile, procArgs;
            if (File.Exists(exePath))
            {
                procFile = exePath;
                procArgs = $"\"{pathPdf}\"";
            }
            else if (File.Exists(pyPath))
            {
                procFile = EncontrarPython();
                procArgs = $"\"{pyPath}\" \"{pathPdf}\"";
            }
            else
            {
                throw new FileNotFoundException(
                    "Extrator GAL não encontrado. Gere gal_extract.exe com PyInstaller " +
                    "ou mantenha gal_extract.py com Python instalado.");
            }

            progress?.Report("Extraindo tabela do PDF via pdfplumber...");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = procFile,
                Arguments              = procArgs,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
            };

            using var proc = System.Diagnostics.Process.Start(psi)
                ?? throw new InvalidOperationException("Falha ao iniciar Python.");

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
                throw new InvalidOperationException(
                    $"gal_extract.py falhou (código {proc.ExitCode}): {stderr}");

            // JSON: lista de listas de strings; índice 0 = cabeçalho com meses
            using var doc = System.Text.Json.JsonDocument.Parse(stdout);
            var todasLinhas = doc.RootElement.EnumerateArray()
                .Select(row => row.EnumerateArray()
                    .Select(cell => cell.GetString() ?? string.Empty)
                    .ToList())
                .ToList();

            if (todasLinhas.Count < 2)
                throw new InvalidDataException(
                    "Tabela retornada pelo extrator Python não contém dados.");

            var headerRow = todasLinhas[0];
            var dataRows  = todasLinhas.Skip(1).ToList();

            // Identifica meses e índices de coluna no cabeçalho
            var meses         = new List<(string Abrev, string Nome, int Ano, int Ordem)>();
            var mesColIndices = new List<int>();

            for (int i = 0; i < headerRow.Count; i++)
            {
                foreach (var token in
                    headerRow[i].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    int sep = token.IndexOf('/');
                    if (sep <= 0) continue;

                    string abrev  = token[..sep];
                    string anoStr = token[(sep + 1)..];

                    if (_mesMap.TryGetValue(abrev, out var info)
                        && int.TryParse(anoStr, out int ano))
                    {
                        meses.Add((abrev, info.Nome, ano, info.Ordem));
                        mesColIndices.Add(i);
                        break;
                    }
                }
            }

            if (meses.Count == 0)
                throw new InvalidDataException(
                    "Nenhum mês encontrado no cabeçalho da tabela extraída.");

            progress?.Report(
                $"Meses: {string.Join(", ", meses.Select(m => m.Abrev + "/" + m.Ano))}");

            int numValores = meses.Count * 3;
            var registros  = new List<(string Municipio, int[] Valores)>();

            foreach (var row in dataRows)
            {
                if (row.Count == 0) continue;

                string nome = row[0].Trim();
                if (string.IsNullOrWhiteSpace(nome)) continue;

                // Lê TOT, SAT, INS para cada mês (3 colunas a partir do índice do mês)
                var vals = new List<int>();
                foreach (int col in mesColIndices)
                {
                    for (int offset = 0; offset < 3; offset++)
                    {
                        int c = col + offset;
                        vals.Add(c < row.Count && int.TryParse(row[c], out int v) ? v : 0);
                    }
                }

                if (vals.Count < numValores) continue;

                string canonico = CanonizarMunicipio(NormalizarNomeMunicipio(nome));
                if (string.IsNullOrWhiteSpace(canonico)
                    || canonico.Equals("TOTAL", StringComparison.OrdinalIgnoreCase))
                    continue;

                registros.Add((canonico, [.. vals.Take(numValores)]));
            }

            return (meses, registros);
        }

        private static string EncontrarPython()
        {
            foreach (var candidato in new[] { "py", "python", "python3" })
            {
                try
                {
                    using var p = System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo
                        {
                            FileName               = candidato,
                            Arguments              = "--version",
                            RedirectStandardOutput = true,
                            RedirectStandardError  = true,
                            UseShellExecute        = false,
                            CreateNoWindow         = true,
                        })!;
                    p.WaitForExit(3000);
                    if (p.ExitCode == 0) return candidato;
                }
                catch { /* tenta próximo */ }
            }
            throw new InvalidOperationException(
                "Python não encontrado no PATH. Instale o Python e tente novamente.");
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
