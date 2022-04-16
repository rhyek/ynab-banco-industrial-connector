using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper.Models;
using MediatR;

namespace YnabBancoIndustrialConnector.Application.Commands;

public class
  NewMobileNotificationTransactionCommand : IRequest<
    MobileNotificationTransaction?>
{
  public string MobileNotificationText { get; init; } = default!;
}
