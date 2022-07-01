using MediatR;
using Microsoft.Extensions.Logging;
using YnabBancoIndustrialConnector.Domain;
using YnabBancoIndustrialConnector.Infrastructure.YnabController;

namespace YnabBancoIndustrialConnector.Application.Commands;

public class UpdateBankReservedTransactionsCommand : IRequest
{
}

public class UpdateBankReservedTransactionsCommandHandler :
  IRequestHandler<UpdateBankReservedTransactionsCommand>
{
  private readonly ILogger<UpdateBankReservedTransactionsCommandHandler>
    _logger;
  private readonly BancoIndustrialScraperService _bancoIndustrialScraperService;
  private readonly YnabControllerService _ynabControllerService;

  public UpdateBankReservedTransactionsCommandHandler(
    ILogger<UpdateBankReservedTransactionsCommandHandler> logger,
    BancoIndustrialScraperService bancoIndustrialScraperService,
    YnabControllerService ynabControllerService)
  {
    _bancoIndustrialScraperService = bancoIndustrialScraperService;
    _ynabControllerService = ynabControllerService;
    _logger = logger;
  }

  public async Task<Unit> Handle(UpdateBankReservedTransactionsCommand request,
    CancellationToken cancellationToken)
  {
    _logger.LogInformation("Start");
    var reservedTxs =
      await _bancoIndustrialScraperService.ScrapeReservedTransactions(
        cancellationToken);
    if (reservedTxs != null) {
      await _ynabControllerService.ProcessReservedBankTransactions(
        reservedTxs,
        cancellationToken);
    }
    _logger.LogInformation("End");
    return Unit.Value;
  }
}
