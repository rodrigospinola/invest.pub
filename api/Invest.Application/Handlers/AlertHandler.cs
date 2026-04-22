using Invest.Application.Commands.Alert;
using Invest.Application.Queries.Alert;
using Invest.Application.Responses;
using Invest.Application.Common;
using Invest.Domain.Interfaces;

namespace Invest.Application.Handlers;

public class AlertHandler
{
    private readonly IAlertRepository _alertRepository;

    public AlertHandler(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<List<AlertResponse>> GetAlertsAsync(GetAlertsQuery query)
    {
        var alerts = await _alertRepository.GetUnreadByUserIdAsync(query.UserId);
        return alerts.Select(a => new AlertResponse(
            a.Id, a.Titulo, a.Mensagem, a.Tipo.ToString(), a.Status.ToString(), a.MetadataJson, a.CreatedAt)).ToList();
    }

    public async Task<Result<MessageResponse>> MarkAsReadAsync(MarkAlertReadCommand command)
    {
        var alert = await _alertRepository.GetByIdAsync(command.AlertId);
        if (alert == null || alert.UserId != command.UserId)
            return Result<MessageResponse>.Failure("NOT_FOUND", "Alerta não encontrado");

        alert.MarkAsRead();
        await _alertRepository.UpdateAsync(alert);

        return Result<MessageResponse>.Success(new MessageResponse("Alerta marcado como lido"));
    }
}
