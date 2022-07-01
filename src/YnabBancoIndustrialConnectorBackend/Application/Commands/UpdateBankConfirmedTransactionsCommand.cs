using MediatR;
using Microsoft.Extensions.Logging;
using YnabBancoIndustrialConnector.Domain;
using YnabBancoIndustrialConnector.Infrastructure.YnabController;

namespace YnabBancoIndustrialConnector.Application.Commands;

public class UpdateBankConfirmedTransactionsCommand : IRequest
{
}

public class UpdateBankConfirmedTransactionsCommandHandler :
  IRequestHandler<UpdateBankConfirmedTransactionsCommand>
{
  private readonly ILogger<UpdateBankConfirmedTransactionsCommandHandler>
    _logger;
  private readonly BancoIndustrialScraperService _bancoIndustrialScraperService;
  private readonly YnabControllerService _ynabControllerService;

  public UpdateBankConfirmedTransactionsCommandHandler(
    ILogger<UpdateBankConfirmedTransactionsCommandHandler> logger,
    BancoIndustrialScraperService bancoIndustrialScraperService,
    YnabControllerService ynabControllerService)
  {
    _logger = logger;
    _bancoIndustrialScraperService = bancoIndustrialScraperService;
    _ynabControllerService = ynabControllerService;
  }

  public async Task<Unit> Handle(UpdateBankConfirmedTransactionsCommand request,
    CancellationToken cancellationToken)
  {
    _logger.LogInformation("Start");
    var confirmedTxs =
      await _bancoIndustrialScraperService.ScrapeConfirmedTransactions(
        cancellationToken);
    if (confirmedTxs != null) {
      await _ynabControllerService.ProcessConfirmedBankTransactions(
        confirmedTxs, cancellationToken);
    }
    _logger.LogInformation("End");
    return Unit.Value;
  }
}
