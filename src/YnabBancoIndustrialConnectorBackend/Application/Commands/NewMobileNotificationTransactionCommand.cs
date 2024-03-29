using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Models;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Repositories;
using YnabBancoIndustrialConnector.Interfaces;

namespace YnabBancoIndustrialConnector.Application.Commands;

public class
  NewMobileNotificationTransactionCommand : IRequest<
    MobileNotificationTransaction?>
{
  public string MobileNotificationText { get; init; } = default!;
}

public class
  NewMobileNotificationTransactionCommandHandler : IRequestHandler<
    NewMobileNotificationTransactionCommand, MobileNotificationTransaction?>
{
  private readonly ApplicationOptions _options;
  private readonly ILogger<NewMobileNotificationTransactionCommandHandler>
    _logger;
  private readonly YnabTransactionRepository _ynabTransactionRepository;
  private readonly ICurrencyConverterService _currencyConverterService;
  private readonly IMessageQueueService _messageQueue;

  public NewMobileNotificationTransactionCommandHandler(
    IOptions<ApplicationOptions> options,
    ILogger<NewMobileNotificationTransactionCommandHandler> logger,
    YnabTransactionRepository ynabTransactionRepository,
    ICurrencyConverterService currencyConverterService,
    IMessageQueueService messageQueue)
  {
    _options = options.Value;
    _logger = logger;
    _ynabTransactionRepository = ynabTransactionRepository;
    _currencyConverterService = currencyConverterService;
    _messageQueue = messageQueue;
  }

  public async Task<MobileNotificationTransaction?> Handle(
    NewMobileNotificationTransactionCommand request,
    CancellationToken cancellationToken)
  {
    _logger.LogInformation("Start");
    var mobileNotificationTx =
      MobileNotificationTransaction.FromMessage(request
        .MobileNotificationText);

    _logger.LogInformation("Parsed mobile notification transaction: {Parsed}",
      mobileNotificationTx);

    // we aren't going to handle transactions with origin: Agency, because
    // the reference numbers for those will always change eventually
    // in the bank statement.
    if (mobileNotificationTx is { Origin: TransactionOrigin.Establishment } &&
        (mobileNotificationTx.Account == _options
           .BancoIndustrialMobileNotificationDebitCardAccountName
         || mobileNotificationTx.Account == _options
           .BancoIndustrialMobileNotificationCreditCardAccountName)) {
      var amount = mobileNotificationTx.Currency switch {
        "USD" => mobileNotificationTx.Amount,
        // if not USD, temporarily convert to USD using an external conversion
        // rates api
        _ => decimal.Round(await _currencyConverterService.ToUsd(
                             mobileNotificationTx.Currency,
                             mobileNotificationTx.Amount) *
                           1.035m /* bank commission */,
          2)
      };
      if (mobileNotificationTx.Type == TransactionType.Debit) {
        amount *= -1;
      }
      var accountType = mobileNotificationTx.Account == _options
        .BancoIndustrialMobileNotificationDebitCardAccountName
        ? AccountType.Debit
        : AccountType.Credit;

      // create the ynab transaction in the appropriate account.
      // if currency was not USD, will use temporary conversion to USD
      // but we will attempt to get the actual amount from our bank
      // by web scraping "reserved transactions". this sadly will only work
      // with local transactions in Guatemala made in GTQ, at the moment
      var wasCreated = await _ynabTransactionRepository.CreateTransaction(
        reference: mobileNotificationTx.Reference,
        accountType: accountType,
        amount: amount,
        date: DateOnly.FromDateTime(mobileNotificationTx.DateTime),
        cleared: YnabTransactionCleared.Uncleared,
        description: mobileNotificationTx.Description);
      _logger.LogInformation("YNAB tx was created: {WasCreated}", wasCreated);
      if (wasCreated) {
        await _ynabTransactionRepository.CommitChanges();
        // scraping reserved txs is not entirely necessary. it doesn't work for
        // currencies != GTQ since they are not recorded on the website.
        // for USD we just record the dollars directly
        // for all other currencies we just convert them to USD and record that.
        // eventually that amount will be adjusted to the real value when the tx
        // is added to the bank statement

        if (mobileNotificationTx.Currency == "GTQ") {
          await _messageQueue.SendScrapeReservedTransactionsMessage(
            mobileNotificationTx.Reference,
            cancellationToken);
        }
      }
    }
    _logger.LogInformation("End");
    return mobileNotificationTx;
  }
}
