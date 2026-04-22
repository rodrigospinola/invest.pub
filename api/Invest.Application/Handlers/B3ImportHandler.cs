using System.Data;
using ExcelDataReader;
using Invest.Application.Commands.Portfolio;
using Invest.Application.Common;
using Invest.Application.Responses;
using Invest.Domain.Enums;
using System.Text.RegularExpressions;

namespace Invest.Application.Handlers;

/// <summary>Resultado interno do parse de um DataSet B3.</summary>
internal record ParseResult(List<ParsedAsset> Assets, List<string> Errors, int TotalRows);

public partial class B3ImportHandler
{
    public B3ImportHandler()
    {
        // Necessário pro ExcelDataReader funcionar no .NET Core
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    // ── Ponto de entrada público ──────────────────────────────────────────────

    public Task<Result<B3ImportResultResponse>> HandleAsync(ImportB3ExcelCommand command)
    {
        if (command.File == null || command.File.Length == 0)
            return Task.FromResult(Result<B3ImportResultResponse>.Failure("invalid_file", "Arquivo inválido ou vazio."));

        try
        {
            using var stream = command.File.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataset = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            if (dataset.Tables.Count == 0)
                return Task.FromResult(Result<B3ImportResultResponse>.Failure("empty_file", "Nenhuma planilha encontrada."));

            var parsed = ProcessDataSet(dataset);
            return Task.FromResult(Result<B3ImportResultResponse>.Success(new B3ImportResultResponse
            {
                ParsedAssets       = parsed.Assets,
                Errors             = parsed.Errors,
                TotalRowsProcessed = parsed.TotalRows,
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<B3ImportResultResponse>.Failure("read_error", $"Falha ao ler o arquivo: {ex.Message}"));
        }
    }

    // ── Parse do DataSet (internal → testável sem depender de arquivo real) ───

    /// <summary>
    /// Itera as planilhas do DataSet, parseia cada tipo reconhecido e agrupa
    /// ativos duplicados (fracionário + inteiro, mesma ação em corretoras distintas).
    /// </summary>
    internal ParseResult ProcessDataSet(DataSet dataset)
    {
        var parsedAssets = new List<ParsedAsset>();
        var errors       = new List<string>();
        int totalRows    = 0;

        foreach (DataTable table in dataset.Tables)
        {
            var sheet = NormalizeText(table.TableName);

            // Pular empréstimos — não são posições próprias
            if (sheet.Contains("emprestimo"))
                continue;

            if (sheet == "acoes")
                ParseAcoesOuFundoSheet(table, parsedAssets, errors, ref totalRows, ClasseAtivo.Acoes);
            else if (sheet.Contains("fundo de investimento"))
                ParseAcoesOuFundoSheet(table, parsedAssets, errors, ref totalRows, ClasseAtivo.FundosImobiliarios);
            else if (sheet.Contains("renda fixa"))
                ParseRendaFixaSheet(table, parsedAssets, errors, ref totalRows);
            else if (sheet.Contains("tesouro direto"))
                ParseTesouroDiretoSheet(table, parsedAssets, errors, ref totalRows);
        }

        // Agrupar por ticker (mesma ação em corretoras diferentes, ou fracionário + inteiro)
        var grouped = parsedAssets
            .GroupBy(a => a.Ticker)
            .Select(g =>
            {
                var totalQtd  = g.Sum(x => x.Quantidade);
                var withPrice = g.Where(x => x.PrecoMedio.HasValue && x.PrecoMedio > 0).ToList();
                decimal? precoMedio = withPrice.Count > 0
                    ? Math.Round(withPrice.Sum(x => x.PrecoMedio!.Value * x.Quantidade) / withPrice.Sum(x => x.Quantidade), 2)
                    : null;
                return new ParsedAsset
                {
                    Ticker      = g.Key,
                    Classe      = g.First().Classe,
                    Quantidade  = totalQtd,
                    Instituicao = g.Select(x => x.Instituicao).Distinct().Count() > 1
                                    ? "Múltiplas Corretoras" : g.First().Instituicao,
                    PrecoMedio  = precoMedio,
                };
            })
            .ToList();

        return new ParseResult(grouped, errors, totalRows);
    }

    // ── Ações / Fundo de Investimento ──────────────────────────────────────────
    // Estrutura B3:
    //   A=Produto  B=Instituição  C=Conta  D=Código de Negociação  E=CNPJ
    //   F=ISIN     G=Tipo         H=Escriturador/Administrador
    //   I=Quantidade  J=Qtd Disponível  K=Qtd Indisponível  L=Motivo
    //   M=Preço de Fechamento  N=Valor Atualizado
    private void ParseAcoesOuFundoSheet(DataTable table, List<ParsedAsset> result,
        List<string> errors, ref int totalRows, ClasseAtivo defaultClasse)
    {
        int tickerCol = ColIndex(table, c =>
            c.Contains("codigo de negociacao") || c == "ativo" || c == "ticker");
        int qtdCol   = ColIndex(table, c =>
            c.StartsWith("quantidade") && !c.Contains("disponivel") && !c.Contains("indisponivel"));
        int instCol  = ColIndex(table, c => c.Contains("instituicao") || c == "corretora");
        int precoCol = ColIndex(table, c =>
            c.Contains("preco de fechamento") || c == "preco fechamento");

        if (tickerCol == -1 || qtdCol == -1) return;

        for (int row = 0; row < table.Rows.Count; row++)
        {
            totalRows++;
            try
            {
                var tickerRaw = Cell(table, row, tickerCol);
                var qtdRaw    = Cell(table, row, qtdCol);

                if (IsEmpty(tickerRaw) || IsEmpty(qtdRaw)) continue;
                if (tickerRaw!.ToLower().Contains("total")) continue;

                var ticker = LimparTicker(tickerRaw);
                if (string.IsNullOrWhiteSpace(ticker)) continue;

                if (!TryParseDecimal(qtdRaw!, out decimal quantidade) || quantidade <= 0) continue;

                var instituicao = Cell(table, row, instCol) ?? "B3";
                if (IsEmpty(instituicao)) instituicao = "B3";

                var classe = InferirClasseAcao(tickerRaw, defaultClasse);
                var preco  = precoCol != -1 ? TryParseDecimalCell(table, row, precoCol) : null;

                result.Add(new ParsedAsset
                {
                    Ticker      = ticker,
                    Quantidade  = quantidade,
                    Instituicao = instituicao!,
                    Classe      = classe,
                    PrecoMedio  = preco,
                });
            }
            catch (Exception ex)
            {
                errors.Add($"[{table.TableName}] Linha {row + 2}: {ex.Message}");
            }
        }
    }

    // ── Renda Fixa ─────────────────────────────────────────────────────────────
    // Estratégia de importação: Quantidade=1, PrecoMedio=Valor total (Fechamento ou MTM)
    private void ParseRendaFixaSheet(DataTable table, List<ParsedAsset> result,
        List<string> errors, ref int totalRows)
    {
        int codigoCol    = ColIndex(table, c => c == "codigo" || c == "codigo de negociacao");
        int produtoCol   = ColIndex(table, c => c == "produto");
        int instCol      = ColIndex(table, c => c.Contains("instituicao") || c == "corretora");
        int indexadorCol = ColIndex(table, c => c == "indexador");

        // Preferir Valor Atualizado FECHAMENTO; fallback para MTM
        int valFechCol   = ColIndex(table, c => c.Contains("valor atualizado fechamento"));
        int valMtmCol    = ColIndex(table, c => c == "valor atualizado mtm" || c == "valor atualizado");

        int tickerCol = codigoCol != -1 ? codigoCol : produtoCol;
        if (tickerCol == -1) return;

        for (int row = 0; row < table.Rows.Count; row++)
        {
            totalRows++;
            try
            {
                var tickerRaw = Cell(table, row, tickerCol);
                if (IsEmpty(tickerRaw)) continue;
                if (tickerRaw!.ToLower().Contains("total")) continue;

                var instituicao = Cell(table, row, instCol) ?? "B3";
                if (IsEmpty(instituicao)) instituicao = "B3";

                var indexador = Cell(table, row, indexadorCol) ?? "";

                // Valor total da posição
                decimal? valorTotal = valFechCol != -1 ? TryParseDecimalCell(table, row, valFechCol) : null;
                if ((valorTotal ?? 0) <= 0 && valMtmCol != -1)
                    valorTotal = TryParseDecimalCell(table, row, valMtmCol);
                if ((valorTotal ?? 0) <= 0) continue;

                var classe = InferirClasseRF(indexador.Length > 0 ? indexador : tickerRaw);
                var ticker = LimparTicker(tickerRaw);

                result.Add(new ParsedAsset
                {
                    Ticker      = ticker,
                    Quantidade  = 1m,          // 1 posição; valor total fica no PrecoMedio
                    Instituicao = instituicao!,
                    Classe      = classe,
                    PrecoMedio  = Math.Round(valorTotal!.Value, 2),
                });
            }
            catch (Exception ex)
            {
                errors.Add($"[{table.TableName}] Linha {row + 2}: {ex.Message}");
            }
        }
    }

    // ── Tesouro Direto ─────────────────────────────────────────────────────────
    // Preco por título = Valor Atualizado / Quantidade
    private void ParseTesouroDiretoSheet(DataTable table, List<ParsedAsset> result,
        List<string> errors, ref int totalRows)
    {
        int produtoCol   = ColIndex(table, c => c == "produto");
        int instCol      = ColIndex(table, c => c.Contains("instituicao") || c == "corretora");
        int qtdCol       = ColIndex(table, c =>
            c.StartsWith("quantidade") && !c.Contains("disponivel") && !c.Contains("indisponivel"));
        int indexadorCol = ColIndex(table, c => c == "indexador");
        int valAtualCol  = ColIndex(table, c => c == "valor atualizado");

        if (produtoCol == -1 || qtdCol == -1) return;

        for (int row = 0; row < table.Rows.Count; row++)
        {
            totalRows++;
            try
            {
                var produtoRaw = Cell(table, row, produtoCol);
                var qtdRaw     = Cell(table, row, qtdCol);

                if (IsEmpty(produtoRaw) || IsEmpty(qtdRaw)) continue;
                if (produtoRaw!.ToLower().Contains("total")) continue;

                if (!TryParseDecimal(qtdRaw!, out decimal quantidade) || quantidade <= 0) continue;

                var instituicao = Cell(table, row, instCol) ?? "B3";
                if (IsEmpty(instituicao)) instituicao = "B3";

                var indexador  = Cell(table, row, indexadorCol) ?? "";
                var valorTotal = valAtualCol != -1 ? TryParseDecimalCell(table, row, valAtualCol) : null;

                decimal? preco = (valorTotal ?? 0) > 0 && quantidade > 0
                    ? Math.Round(valorTotal!.Value / quantidade, 2)
                    : null;

                var classe = InferirClasseRF(indexador.Length > 0 ? indexador : produtoRaw!);
                var ticker = GerarTickerTesouro(produtoRaw!);

                result.Add(new ParsedAsset
                {
                    Ticker      = ticker,
                    Quantidade  = quantidade,
                    Instituicao = instituicao!,
                    Classe      = classe,
                    PrecoMedio  = preco,
                });
            }
            catch (Exception ex)
            {
                errors.Add($"[{table.TableName}] Linha {row + 2}: {ex.Message}");
            }
        }
    }

    // ── Utilitários ────────────────────────────────────────────────────────────

    /// Remove acentos e normaliza para minúsculas — usado na detecção de colunas e sheets.
    internal static string NormalizeText(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Trim().ToLowerInvariant()
            .Replace("ç", "c").Replace("ã", "a").Replace("â", "a").Replace("á", "a").Replace("à", "a")
            .Replace("é", "e").Replace("ê", "e").Replace("è", "e")
            .Replace("í", "i").Replace("î", "i").Replace("ì", "i")
            .Replace("ó", "o").Replace("ô", "o").Replace("ò", "o").Replace("õ", "o")
            .Replace("ú", "u").Replace("û", "u").Replace("ù", "u")
            .Replace("ü", "u").Replace("ñ", "n");
    }

    private static int ColIndex(DataTable table, Func<string, bool> predicate)
    {
        for (int i = 0; i < table.Columns.Count; i++)
        {
            if (predicate(NormalizeText(table.Columns[i].ColumnName))) return i;
        }
        return -1;
    }

    private static string? Cell(DataTable table, int row, int col)
    {
        if (col < 0 || col >= table.Columns.Count) return null;
        return table.Rows[row][col]?.ToString()?.Trim();
    }

    private static bool IsEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == "-" || value == "--";

    private static bool TryParseDecimal(string raw, out decimal value)
    {
        value = 0;
        if (IsEmpty(raw)) return false;
        // Formato BR: "1.234,56" → remove "." (milhar) e troca "," por "."
        var cleaned = raw.Replace("R$", "").Replace(" ", "");
        if (cleaned.Contains(",") && cleaned.Contains("."))
        {
            // Ex: "1.234,56" → "1234.56"
            cleaned = cleaned.Replace(".", "").Replace(",", ".");
        }
        else if (cleaned.Contains(","))
        {
            // Ex: "28,50" → "28.50"
            cleaned = cleaned.Replace(",", ".");
        }
        else if (cleaned.Contains("."))
        {
            // Sem vírgula: "1.234" pode ser milhar BR (3 dígitos após o ponto)
            // vs "28.50" que é decimal.
            // Regra: se todos os segmentos após cada ponto tiverem exatamente 3 dígitos
            // → separador de milhar. Ex: "1.234" → "1234", "1.234.567" → "1234567".
            // Caso contrário (ex: "28.50", "2.5") mantém como decimal.
            var parts = cleaned.Split('.');
            if (parts.Length > 1 && parts.Skip(1).All(p => p.Length == 3))
                cleaned = cleaned.Replace(".", "");
        }
        return decimal.TryParse(cleaned,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static decimal? TryParseDecimalCell(DataTable table, int row, int col)
    {
        var raw = Cell(table, row, col);
        return !IsEmpty(raw) && TryParseDecimal(raw!, out decimal v) && v > 0 ? v : null;
    }

    private string LimparTicker(string raw)
    {
        var trimmed = raw.Trim().ToUpper();
        var match = FracionarioRegex().Match(trimmed);
        return match.Success ? match.Groups[1].Value : trimmed;
    }

    private string InferirClasseAcao(string tickerRaw, ClasseAtivo defaultClasse)
    {
        var clean = LimparTicker(tickerRaw);
        if (clean.EndsWith("11")) return ClasseAtivo.FundosImobiliarios.ToString();
        if (clean.EndsWith("39")) return ClasseAtivo.Internacional.ToString(); // BDRs
        if (clean.Length >= 5 && (clean.EndsWith("3") || clean.EndsWith("4") || clean.EndsWith("5")
            || clean.EndsWith("6") || clean.EndsWith("7") || clean.EndsWith("8")))
            return ClasseAtivo.Acoes.ToString();
        return defaultClasse.ToString();
    }

    private static string InferirClasseRF(string hint)
    {
        var n = NormalizeText(hint);
        if (n.Contains("ipca") || n.Contains("igpm") || n.Contains("igp-m") || n.Contains("igp"))
            return ClasseAtivo.RFDinamica.ToString();
        // CDI, Selic, Pré, DI → RFPos (pós-fixado)
        return ClasseAtivo.RFPos.ToString();
    }

    internal static string GerarTickerTesouro(string produto)
    {
        // "Tesouro IPCA+ 2035" → "TD_IPCA+2035"
        // "Tesouro Selic 2027" → "TD_Selic2027"
        var clean = produto.Trim()
            .Replace("Tesouro ", "TD_")
            .Replace(" ", "");
        return clean.Length > 20 ? clean[..20] : clean;
    }

    [GeneratedRegex("^([A-Z]{4}\\d{1,2})F$")]
    private static partial Regex FracionarioRegex();
}
