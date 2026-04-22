namespace Invest.Domain.Entities;

public class Benchmark
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public DateTime Data { get; private set; }
    public decimal Valor { get; private set; }
    public decimal VariacaoNoDia { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Benchmark() { }

    public static Benchmark Create(
        string nome,
        DateTime data,
        decimal valor,
        decimal variacaoNoDia)
    {
        return new Benchmark
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Data = data.Date,
            Valor = valor,
            VariacaoNoDia = variacaoNoDia,
            CreatedAt = DateTime.UtcNow
        };
    }
}
