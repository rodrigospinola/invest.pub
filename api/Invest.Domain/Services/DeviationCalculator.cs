using Invest.Domain.Enums;
using Invest.Domain.ValueObjects;

namespace Invest.Domain.Services;

public class DeviationCalculator
{
    public List<Deviation> Calculate(
        Dictionary<ClasseAtivo, decimal> currentValues,
        List<(string classe, decimal percentual)> targetAllocation)
    {
        var totalValue = currentValues.Values.Sum();
        var deviations = new List<Deviation>();

        // Map targetAllocation to a dictionary for easier lookup
        var targets = targetAllocation.ToDictionary(
            x => x.classe,
            x => x.percentual
        );

        // Get all possible classes from both current and target
        var allClasses = targets.Keys.Union(currentValues.Keys.Select(MapToString)).Distinct();

        foreach (var classeStr in allClasses)
        {
            var classeEnum = MapToEnum(classeStr);
            var realValue = currentValues.GetValueOrDefault(classeEnum, 0);
            var realPercent = totalValue > 0 ? (realValue / totalValue) * 100 : 0;
            var targetPercent = targets.GetValueOrDefault(classeStr, 0);

            deviations.Add(new Deviation(classeStr, Math.Round(realPercent, 2), Math.Round(targetPercent, 2)));
        }

        return deviations;
    }

    private ClasseAtivo MapToEnum(string classe) => classe switch
    {
        "RF Dinâmica" => ClasseAtivo.RFDinamica,
        "RF Pós" => ClasseAtivo.RFPos,
        "Fundos imobiliários" => ClasseAtivo.FundosImobiliarios,
        "Ações" => ClasseAtivo.Acoes,
        "Internacional" => ClasseAtivo.Internacional,
        "Fundos multimercados" => ClasseAtivo.FundosMultimercados,
        "Alternativos" => ClasseAtivo.Alternativos,
        _ => Enum.Parse<ClasseAtivo>(classe.Replace(" ", ""))
    };

    private string MapToString(ClasseAtivo classe) => classe switch
    {
        ClasseAtivo.RFDinamica => "RF Dinâmica",
        ClasseAtivo.RFPos => "RF Pós",
        ClasseAtivo.FundosImobiliarios => "Fundos imobiliários",
        ClasseAtivo.Acoes => "Ações",
        ClasseAtivo.Internacional => "Internacional",
        ClasseAtivo.FundosMultimercados => "Fundos multimercados",
        ClasseAtivo.Alternativos => "Alternativos",
        _ => classe.ToString()
    };
}
