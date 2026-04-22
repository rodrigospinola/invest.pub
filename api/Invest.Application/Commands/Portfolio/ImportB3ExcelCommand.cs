using Microsoft.AspNetCore.Http;

namespace Invest.Application.Commands.Portfolio;

public record ImportB3ExcelCommand(Guid UserId, IFormFile File);
