using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper.Models;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper;
using YnabBancoIndustrialConnector.Infrastructure.YnabController;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Models;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Repositories;

namespace YnabBancoIndustrialConnector.Application.Commands;

public class
  NewMobileNotificationTransactionCommandHandler : IRequestHandler<
    NewMobileNotificationTransactionCommand, MobileNotificationTransaction?>
{
  private readonly ApplicationOptions _options;
  private readonly ILogger<NewMobileNotificationTransactionCommandHandler>
    _logger;
  private readonly BancoIndustrialScraperService _bancoIndustrialScraperService;
  private readonly YnabTransactionRepository _ynabTransactionRepository;
  private readonly YnabControllerService _ynabControllerService;

  public NewMobileNotificationTransactionCommandHandler(
    IOptions<ApplicationOptions> options,
    ILogger<NewMobileNotificationTransactionCommandHandler> logger,
    BancoIndustrialScraperService bancoIndustrialScraperService,
    YnabTransactionRepository ynabTransactionRepository,
    YnabControllerService ynabControllerService)
  {
    _options = options.Value;
    _logger = logger;
    _ynabTransactionRepository = ynabTransactionRepository;
    _ynabControllerService = ynabControllerService;
    _bancoIndustrialScraperService = bancoIndustrialScraperService;
  }

  public async Task<MobileNotificationTransaction?> Handle(
    NewMobileNotificationTransactionCommand request,
    CancellationToken cancellationToken)
  {
    var mobileNotificationTx =
      MobileNotificationTransaction.FromMessage(request
        .MobileNotificationText);

    _logger.LogInformation("Parsed mobile notification transaction: {Parsed}",
      mobileNotificationTx);

    // we aren't going to handle transactions with origin: Agency, because
    // the reference numbers for those will always change eventually
    // in the bank statement.
    if (mobileNotificationTx != null
        && mobileNotificationTx.Origin == TransactionOrigin.Establishment
        && mobileNotificationTx.Account == _options
          .BancoIndustrialMobileNotificationAccountNameForEstablishmentTransactions) {
      var amount = mobileNotificationTx.Currency == "Q"
        ? 0
        : mobileNotificationTx.Type == TransactionType.Debit
          ? -mobileNotificationTx.Amount
          : mobileNotificationTx.Amount;
      var wasCreated = await _ynabTransactionRepository.CreateTransaction(
        reference: mobileNotificationTx.Reference,
        amount: amount,
        date: DateOnly.FromDateTime(mobileNotificationTx.DateTime),
        cleared: YnabTransactionCleared.Uncleared,
        description: mobileNotificationTx.Description);
      if (wasCreated) {
        await _ynabTransactionRepository.CommitChanges();
        var reservedBankTxs =
          await _bancoIndustrialScraperService.ScrapeReservedTransactions(
            cancellationToken);
        if (reservedBankTxs != null) {
          await _ynabControllerService.ProcessReservedBankTransactions(
            reservedBankTxs,
            cancellationToken);
        }
      }
    }

    return mobileNotificationTx;
  }
}
