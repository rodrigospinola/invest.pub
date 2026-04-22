namespace Invest.Application.Commands.Alert;

public record MarkAlertReadCommand(Guid AlertId, Guid UserId);
