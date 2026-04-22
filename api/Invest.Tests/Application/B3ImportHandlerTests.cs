using System.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Invest.Application.Handlers;

namespace Invest.Tests.Application;

public class B3ImportHandlerTests
{
    private readonly B3ImportHandler _sut = new();

    // =========================================================
    // Helpers — construtores de DataTable com estrutura B3
    // =========================================================

    /// Planilha de ações/FIIs com as colunas padrão do extrato B3.
    private static DataTable CreateAcoesSheet(
        string sheetName = "Ações",
        bool includePreco = true)
    {
        var table = new DataTable(sheetName);
        table.Columns.Add("Código de Negociação");
        table.Columns.Add("Quantidade");
        table.Columns.Add("Instituição");
        if (includePreco) table.Columns.Add("Preço de Fechamento");
        return table;
    }

    private static void AddAcoesRow(DataTable t, string ticker, string qty,
        string inst = "XP Investimentos", string preco = "")
    {
        if (t.Columns.Count >= 4)
            t.Rows.Add(ticker, qty, inst, preco);
        else
            t.Rows.Add(ticker, qty, inst);
    }

    /// Planilha de Renda Fixa com as colunas padrão do extrato B3.
    private static DataTable CreateRendaFixaSheet(string sheetName = "Renda Fixa")
    {
        var table = new DataTable(sheetName);
        table.Columns.Add("Produto");
        table.Columns.Add("Instituição");
        table.Columns.Add("Indexador");
        table.Columns.Add("Valor Atualizado Fechamento");
        return table;
    }

    private static void AddRendaFixaRow(DataTable t, string produto, string inst,
        string indexador, string valor)
        => t.Rows.Add(produto, inst, indexador, valor);

    /// Planilha de Tesouro Direto com as colunas padrão do extrato B3.
    private static DataTable CreateTesouroDiretoSheet(string sheetName = "Tesouro Direto")
    {
        var table = new DataTable(sheetName);
        table.Columns.Add("Produto");
        table.Columns.Add("Instituição");
        table.Columns.Add("Quantidade");
        table.Columns.Add("Indexador");
        table.Columns.Add("Valor Atualizado");
        return table;
    }

    private static void AddTesouroDiretoRow(DataTable t, string produto, string qty,
        string inst, string indexador, string valor)
        => t.Rows.Add(produto, inst, qty, indexador, valor);

    private static DataSet ToDataSet(params DataTable[] tables)
    {
        var ds = new DataSet();
        foreach (var t in tables) ds.Tables.Add(t);
        return ds;
    }

    // =========================================================
    // HandleAsync — validação de arquivo
    // =========================================================

    [Fact]
    public async Task HandleAsync_ArquivoNulo_RetornaFailure()
    {
        var command = new Invest.Application.Commands.Portfolio.ImportB3ExcelCommand(
            Guid.NewGuid(), null!);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("invalid_file");
    }

    [Fact]
    public async Task HandleAsync_ArquivoComTamanhoZero_RetornaFailure()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        var command = new Invest.Application.Commands.Portfolio.ImportB3ExcelCommand(
            Guid.NewGuid(), fileMock.Object);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("invalid_file");
    }

    // =========================================================
    // ProcessDataSet — planilha de Ações
    // =========================================================

    [Fact]
    public void ProcessDataSet_PlanilhaAcoesUmaLinha_RetornaUmAtivo()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "PETR4", "100", "XP Investimentos", "28.50");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets.Should().HaveCount(1);
        result.Assets[0].Ticker.Should().Be("PETR4");
        result.Assets[0].Quantidade.Should().Be(100m);
        result.Assets[0].PrecoMedio.Should().Be(28.50m);
        result.Assets[0].Instituicao.Should().Be("XP Investimentos");
        result.Assets[0].Classe.Should().Be("Acoes");
    }

    [Fact]
    public void ProcessDataSet_TickerFracionario_NormalizaSemSufixoF()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "PETR4F", "50", "Clear", "28.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets.Should().HaveCount(1);
        result.Assets[0].Ticker.Should().Be("PETR4");
    }

    [Fact]
    public void ProcessDataSet_TickerEmMinusculas_NormalizaParaMaiusculas()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "wege3", "200", "BTG", "45.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Ticker.Should().Be("WEGE3");
    }

    [Fact]
    public void ProcessDataSet_LinhaComQuantidadeZero_EhDescartada()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "VALE3", "0", "XP", "70.00");
        AddAcoesRow(table, "PETR4", "100", "XP", "28.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets.Should().HaveCount(1);
        result.Assets[0].Ticker.Should().Be("PETR4");
    }

    [Fact]
    public void ProcessDataSet_LinhaComNomeTotal_EhDescartada()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "PETR4", "100", "XP", "28.00");
        AddAcoesRow(table, "Total", "100", "XP", "28.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets.Should().HaveCount(1);
    }

    [Fact]
    public void ProcessDataSet_SemColunaDePreco_PrecoMedioEhNulo()
    {
        var table = CreateAcoesSheet(includePreco: false);
        AddAcoesRow(table, "PETR4", "100", "XP");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets.Should().HaveCount(1);
        result.Assets[0].PrecoMedio.Should().BeNull();
    }

    [Fact]
    public void ProcessDataSet_TickerSufixo11_MapeiaParaFII()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "MXRF11", "50", "XP", "10.20");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Classe.Should().Be("FundosImobiliarios");
    }

    [Fact]
    public void ProcessDataSet_TickerSufixo39_MapeiaParaInternacional()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "AAPL39", "10", "XP", "150.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Classe.Should().Be("Internacional");
    }

    [Fact]
    public void ProcessDataSet_PlanilhaFundoDeInvestimento_MapeiaParaFII()
    {
        // Nome da aba contém "fundo de investimento" → ClasseAtivo.FundosImobiliarios como default
        var table = CreateAcoesSheet("Fundo de Investimento Imobiliário");
        AddAcoesRow(table, "XPML11", "30", "XP", "100.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Classe.Should().Be("FundosImobiliarios");
    }

    [Fact]
    public void ProcessDataSet_PlanilhaEmprestimo_EhIgnorada()
    {
        var emprestimo = CreateAcoesSheet("Empréstimo de Ativos");
        AddAcoesRow(emprestimo, "PETR4", "100", "XP", "28.00");

        var acoes = CreateAcoesSheet();
        AddAcoesRow(acoes, "VALE3", "50", "XP", "70.00");

        var result = _sut.ProcessDataSet(ToDataSet(emprestimo, acoes));

        // Apenas a planilha de ações deve aparecer
        result.Assets.Should().HaveCount(1);
        result.Assets[0].Ticker.Should().Be("VALE3");
    }

    // =========================================================
    // ProcessDataSet — agrupamento de ativos
    // =========================================================

    [Fact]
    public void ProcessDataSet_MesmoTickerEmDuasCorretoras_AgrupaEmUmAtivoComMultiplasCorretoras()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "PETR4", "100", "XP Investimentos", "28.00");
        AddAcoesRow(table, "PETR4", "50",  "Clear",            "28.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets.Should().HaveCount(1);
        result.Assets[0].Quantidade.Should().Be(150m);
        result.Assets[0].Instituicao.Should().Be("Múltiplas Corretoras");
    }

    [Fact]
    public void ProcessDataSet_TickerFracionarioEInteiroJuntos_AgrupaComSomaDeQuantidades()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "PETR4",  "100", "XP", "28.00");
        AddAcoesRow(table, "PETR4F", "25",  "XP", "28.00"); // fracionário → PETR4

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets.Should().HaveCount(1);
        result.Assets[0].Ticker.Should().Be("PETR4");
        result.Assets[0].Quantidade.Should().Be(125m);
    }

    [Fact]
    public void ProcessDataSet_MesmoTickerComPrecosDiferentes_CalculaPrecoMedioPonderado()
    {
        // 100 cotas a 20.00 + 50 cotas a 26.00 → PM = (100×20 + 50×26) / 150 = 22.00
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "PETR4",  "100", "XP",    "20.00");
        AddAcoesRow(table, "PETR4F", "50",  "Clear", "26.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].PrecoMedio.Should().Be(22.00m);
    }

    [Fact]
    public void ProcessDataSet_TickerUnicoEmUmaCorretora_MantemInstituicaoOriginal()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "WEGE3", "200", "BTG Pactual", "45.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Instituicao.Should().Be("BTG Pactual");
    }

    [Fact]
    public void ProcessDataSet_MultiplasPlanilhasAcoes_CombinaTodosOsAtivos()
    {
        var acoes = CreateAcoesSheet();
        AddAcoesRow(acoes, "PETR4", "100", "XP", "28.00");

        var fiis = CreateAcoesSheet("Fundo de Investimento Imobiliário");
        AddAcoesRow(fiis, "MXRF11", "50", "XP", "10.20");

        var result = _sut.ProcessDataSet(ToDataSet(acoes, fiis));

        result.Assets.Should().HaveCount(2);
    }

    // =========================================================
    // ProcessDataSet — Renda Fixa
    // =========================================================

    [Fact]
    public void ProcessDataSet_RendaFixaIndexadorIPCA_MapeiaParaRFDinamica()
    {
        var table = CreateRendaFixaSheet();
        AddRendaFixaRow(table, "CDB Banco X", "XP", "IPCA", "5000.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets.Should().HaveCount(1);
        result.Assets[0].Classe.Should().Be("RFDinamica");
    }

    [Fact]
    public void ProcessDataSet_RendaFixaIndexadorCDI_MapeiaParaRFPos()
    {
        var table = CreateRendaFixaSheet();
        AddRendaFixaRow(table, "CDB Banco Y", "Clear", "CDI", "3000.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Classe.Should().Be("RFPos");
    }

    [Fact]
    public void ProcessDataSet_RendaFixa_QuantidadeSempreIgualA1()
    {
        var table = CreateRendaFixaSheet();
        AddRendaFixaRow(table, "LCI XP", "XP", "IPCA", "12000.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Quantidade.Should().Be(1m);
    }

    [Fact]
    public void ProcessDataSet_RendaFixa_PrecoMedioIgualAoValorTotal()
    {
        var table = CreateRendaFixaSheet();
        AddRendaFixaRow(table, "LCA Clear", "Clear", "CDI", "7500.50");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].PrecoMedio.Should().Be(7500.50m);
    }

    [Fact]
    public void ProcessDataSet_RendaFixaSemValor_LinhaDescartada()
    {
        var table = CreateRendaFixaSheet();
        AddRendaFixaRow(table, "CDB Sem Valor", "XP", "CDI", "0");
        AddRendaFixaRow(table, "CDB Com Valor", "XP", "CDI", "1000.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets.Should().HaveCount(1);
        result.Assets[0].Ticker.Should().Be("CDB COM VALOR");
    }

    // =========================================================
    // ProcessDataSet — Tesouro Direto
    // =========================================================

    [Fact]
    public void ProcessDataSet_TesouroDiretoIPCA_TickerFormatadoCorretamente()
    {
        var table = CreateTesouroDiretoSheet();
        AddTesouroDiretoRow(table, "Tesouro IPCA+ 2035", "2.5", "XP", "IPCA", "15000.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets.Should().HaveCount(1);
        result.Assets[0].Ticker.Should().Be("TD_IPCA+2035");
    }

    [Fact]
    public void ProcessDataSet_TesouroDiretoSelic_TickerFormatadoCorretamente()
    {
        var table = CreateTesouroDiretoSheet();
        AddTesouroDiretoRow(table, "Tesouro Selic 2027", "1", "XP", "Selic", "8000.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Ticker.Should().Be("TD_Selic2027");
    }

    [Fact]
    public void ProcessDataSet_TesouroDireto_PrecoCalculadoPorTitulo()
    {
        // 2,5 títulos × valor total R$15.000 → preço por título = R$6.000
        var table = CreateTesouroDiretoSheet();
        AddTesouroDiretoRow(table, "Tesouro IPCA+ 2035", "2.5", "XP", "IPCA", "15000.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Quantidade.Should().Be(2.5m);
        result.Assets[0].PrecoMedio.Should().Be(6000.00m);
    }

    [Fact]
    public void ProcessDataSet_TesouroDireto_IndexadorIPCA_MapeiaParaRFDinamica()
    {
        var table = CreateTesouroDiretoSheet();
        AddTesouroDiretoRow(table, "Tesouro IPCA+ 2029", "1", "XP", "IPCA", "5000.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Classe.Should().Be("RFDinamica");
    }

    [Fact]
    public void ProcessDataSet_TesouroDireto_IndexadorSelic_MapeiaParaRFPos()
    {
        var table = CreateTesouroDiretoSheet();
        AddTesouroDiretoRow(table, "Tesouro Selic 2027", "1", "XP", "Selic", "5000.00");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Classe.Should().Be("RFPos");
    }

    // =========================================================
    // ProcessDataSet — formato de números BR
    // =========================================================

    [Fact]
    public void ProcessDataSet_QuantidadeFormatoBrasileiro_ParseadaCorretamente()
    {
        // "1.234" = 1234 no formato BR sem vírgula decimal
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "PETR4", "1.234", "XP", "28,50");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].Quantidade.Should().Be(1234m);
    }

    [Fact]
    public void ProcessDataSet_PrecoFormatoBrasileiroComVirgula_ParseadoCorretamente()
    {
        var table = CreateAcoesSheet();
        AddAcoesRow(table, "PETR4", "100", "XP", "28,50");

        var result = _sut.ProcessDataSet(ToDataSet(table));

        result.Assets[0].PrecoMedio.Should().Be(28.50m);
    }

    // =========================================================
    // NormalizeText (internal static — testado diretamente)
    // =========================================================

    [Theory]
    [InlineData("Ações",              "acoes")]
    [InlineData("Código de Negociação", "codigo de negociacao")]
    [InlineData("Instituição",        "instituicao")]
    [InlineData("Preço de Fechamento", "preco de fechamento")]
    [InlineData("Fundo de Investimento Imobiliário", "fundo de investimento imobiliario")]
    [InlineData("",                   "")]
    [InlineData(null,                 "")]
    public void NormalizeText_RemoveAcentosEConvertePraMinusculas(string? input, string expected)
    {
        B3ImportHandler.NormalizeText(input).Should().Be(expected);
    }

    // =========================================================
    // GerarTickerTesouro (internal static — testado diretamente)
    // =========================================================

    [Theory]
    [InlineData("Tesouro IPCA+ 2035",     "TD_IPCA+2035")]
    [InlineData("Tesouro Selic 2027",     "TD_Selic2027")]
    [InlineData("Tesouro Prefixado 2026", "TD_Prefixado2026")]
    [InlineData("Tesouro IPCA+ com Juros Semestrais 2035", "TD_IPCA+comJurosSeme")] // truncado a 20 chars
    public void GerarTickerTesouro_FormatacaoCorreta(string produto, string expected)
    {
        B3ImportHandler.GerarTickerTesouro(produto).Should().Be(expected);
    }
}
