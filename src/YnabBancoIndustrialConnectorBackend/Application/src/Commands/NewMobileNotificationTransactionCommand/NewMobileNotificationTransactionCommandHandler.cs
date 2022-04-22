using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper.Models;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper;
using YnabBancoIndustrialConnector.Infrastructure.YnabController;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Models;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Repositories;
using YnabBancoIndustrialConnector.Interfaces;

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
  private readonly ICurrencyConverterService _currencyConverterService;

  public NewMobileNotificationTransactionCommandHandler(
    IOptions<ApplicationOptions> options,
    ILogger<NewMobileNotificationTransactionCommandHandler> logger,
    BancoIndustrialScraperService bancoIndustrialScraperService,
    YnabTransactionRepository ynabTransactionRepository,
    YnabControllerService ynabControllerService,
    ICurrencyConverterService currencyConverterService)
  {
    _options = options.Value;
    _logger = logger;
    _ynabTransactionRepository = ynabTransactionRepository;
    _ynabControllerService = ynabControllerService;
    _currencyConverterService = currencyConverterService;
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
        && (mobileNotificationTx.Account == _options
              .BancoIndustrialMobileNotificationDebitCardAccountNameForEstablishmentTransactions
            || mobileNotificationTx.Account == _options
              .BancoIndustrialMobileNotificationCreditCardAccountNameForEstablishmentTransactions)) {
      var amount = mobileNotificationTx.Currency switch {
        "USD" => mobileNotificationTx.Amount,
        _ => await _currencyConverterService.ToUsd(
          mobileNotificationTx.Currency, mobileNotificationTx.Amount)
      };
      if (mobileNotificationTx.Type == TransactionType.Debit) {
        amount *= -1;
      }
      var accountType = mobileNotificationTx.Account == _options
        .BancoIndustrialMobileNotificationDebitCardAccountNameForEstablishmentTransactions
        ? AccountType.Debit
        : AccountType.Credit;
      var wasCreated = await _ynabTransactionRepository.CreateTransaction(
        reference: mobileNotificationTx.Reference,
        accountType: accountType,
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
