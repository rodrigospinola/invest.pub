namespace Invest.Application.Queries.Dashboard;

public record GetHistoryQuery(Guid UserId, int LastDays = 30);
