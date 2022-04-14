using YnabBancoIndustrialConnector.Infrastructure.BIScraper.Commands;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Repositories;
using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Models;

namespace YnabBancoIndustrialConnector.Application.Commands;

public class NewMobileNotificationTransactionCommand : IRequest
{
  public string MobileNotificationText { get; init; } = default!;
}

public class
  NewMobileNotificationTransactionCommandHandler : IRequestHandler<
    NewMobileNotificationTransactionCommand>
{
  private readonly ApplicationOptions _options;

  private readonly ILogger<NewMobileNotificationTransactionCommandHandler>
    _logger;

  private readonly IMediator _mediator;

  private readonly YnabTransactionRepository _ynabTransactionRepository;

  public NewMobileNotificationTransactionCommandHandler(
    IOptions<ApplicationOptions> options,
    ILogger<NewMobileNotificationTransactionCommandHandler> logger,
    IMediator mediator,
    YnabTransactionRepository ynabTransactionRepository
  )
  {
    _options = options.Value;
    _logger = logger;
    _mediator = mediator;
    _ynabTransactionRepository = ynabTransactionRepository;
  }

  public async Task<Unit> Handle(
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
      if (await _ynabTransactionRepository.CreateTransaction(
            reference: mobileNotificationTx.Reference,
            amount: amount,
            date: DateOnly.FromDateTime(mobileNotificationTx.DateTime),
            cleared: YnabTransactionCleared.Uncleared,
            description: mobileNotificationTx.Description)) {
        await _ynabTransactionRepository.CommitChanges();
        await _mediator.Send(
          new RequestReadTransactionsCommand(ReadTransactionsType.Reserved),
          cancellationToken);
      }
    }

    return Unit.Value;
  }
}
