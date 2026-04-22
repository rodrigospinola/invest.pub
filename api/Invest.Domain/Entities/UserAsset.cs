using Invest.Domain.Enums;

namespace Invest.Domain.Entities;

public class UserAsset
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? PortfolioDesignId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public string Nome { get; private set; } = string.Empty;
    public ClasseAtivo Classe { get; private set; }
    public string? SubEstrategia { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal PrecoMedio { get; private set; }
    public OrigemAtivo Origem { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserAsset() { }

    public static UserAsset Create(
        Guid userId,
        Guid? portfolioDesignId,
        string ticker,
        string nome,
        ClasseAtivo classe,
        string? subEstrategia,
        decimal quantidade,
        decimal precoMedio,
        OrigemAtivo origem)
    {
        return new UserAsset
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PortfolioDesignId = portfolioDesignId,
            Ticker = ticker,
            Nome = nome,
            Classe = classe,
            SubEstrategia = subEstrategia,
            Quantidade = quantidade,
            PrecoMedio = precoMedio,
            Origem = origem,
            Ativo = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarcarVendido()
    {
        Ativo = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AtualizarPrecoMedio(decimal novoPreco, decimal novaQuantidade)
    {
        var totalAnterior = PrecoMedio * Quantidade;
        var totalNovo = novoPreco * novaQuantidade;
        var quantidadeTotal = Quantidade + novaQuantidade;

        PrecoMedio = quantidadeTotal > 0
            ? (totalAnterior + totalNovo) / quantidadeTotal
            : novoPreco;

        Quantidade = quantidadeTotal;
        UpdatedAt = DateTime.UtcNow;
    }
}
