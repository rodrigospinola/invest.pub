namespace Invest.Domain.Entities;

public class AssetHistory
{
    public Guid Id { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public DateTime Data { get; private set; }
    public decimal PrecoFechamento { get; private set; }
    public decimal DividendoNoDia { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AssetHistory() { }

    public static AssetHistory Create(
        string ticker,
        DateTime data,
        decimal precoFechamento,
        decimal dividendoNoDia)
    {
        return new AssetHistory
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            Data = data.Date,
            PrecoFechamento = precoFechamento,
            DividendoNoDia = dividendoNoDia,
            CreatedAt = DateTime.UtcNow
        };
    }
}
