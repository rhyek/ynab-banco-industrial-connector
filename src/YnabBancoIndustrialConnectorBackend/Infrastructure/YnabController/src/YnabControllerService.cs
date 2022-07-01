using Microsoft.Extensions.Logging;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.Models;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Models;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Repositories;
using YnabBancoIndustrialConnector.Interfaces;

namespace YnabBancoIndustrialConnector.Infrastructure.YnabController;

public class YnabControllerService
{
  private readonly ILogger<YnabControllerService> _logger;
  private readonly IMessageQueueService _messageQueue;
  private readonly YnabTransactionRepository _ynabTransactionRepository;

  public YnabControllerService
  (
    ILogger<YnabControllerService> logger,
    IMessageQueueService messageQueue,
    YnabTransactionRepository ynabTransactionRepository
  )
  {
    _logger = logger;
    _ynabTransactionRepository = ynabTransactionRepository;
    _messageQueue = messageQueue;
  }

  public async Task ProcessReservedBankTransactions(
    IList<ReservedBankTransaction> reservedBankTransactions,
    CancellationToken stoppingToken)
  {
    _logger.LogInformation("Processing {Count} reserved bank transactions",
      reservedBankTransactions.Count);
    var recentYnabTransactions =
      await _ynabTransactionRepository.GetRecent(AccountType.Debit);
    foreach (var reservedTx in reservedBankTransactions) {
      var (reference, date, amount) = reservedTx;
      var ynabTx =
        await _ynabTransactionRepository.FindByReference(reference,
          AccountType.Debit,
          recentYnabTransactions);
      if (ynabTx != null) {
        if (amount != Math.Abs(ynabTx.Amount)) {
          if (ynabTx.IsOpen) {
            _ynabTransactionRepository.UpdateTransactionAmount(
              ynabTx.Id,
              -amount);
          }
        }
      }
      else {
        await _ynabTransactionRepository.CreateTransaction(
          reference,
          AccountType.Debit,
          -amount,
          date,
          YnabTransactionCleared.Uncleared,
          recentTransactions: recentYnabTransactions);
      }
    }

    await _ynabTransactionRepository.CommitChanges();
  }

  private IList<ConfirmedBankTransaction> _ConfirmedTxsWithoutDuplicates(
    IEnumerable<ConfirmedBankTransaction> confirmedTxs)
  {
    var grouped = confirmedTxs
      .GroupBy((tx) => tx.Reference)
      .ToList();
    var duplicates = grouped
      .Where(g => g.Count() > 1)
      .Select(g => g.Key)
      .ToList();
    _messageQueue.SendDuplicateConfirmedReferences(
      references: duplicates.ToArray());
    var unique = grouped
      .Where(g => g.Count() == 1)
      .Select(g => g.First())
      .ToList();
    return unique;
  }

  public async Task ProcessConfirmedBankTransactions(
    IList<ConfirmedBankTransaction> confirmedBankTransactions,
    CancellationToken stoppingToken)
  {
    confirmedBankTransactions =
      _ConfirmedTxsWithoutDuplicates(confirmedBankTransactions);
    _logger.LogInformation("Processing {Count} confirmed bank transactions",
      confirmedBankTransactions.Count);
    var recentYnabTransactions =
      await _ynabTransactionRepository.GetRecent(AccountType.Debit);
    foreach (var confirmedTx in confirmedBankTransactions) {
      if (confirmedTx is
          {Reference: "179680"}) {
        // this reference is duplicated. TODO: handle reference duplications
        continue;
      }
      var ynabTx =
        await _ynabTransactionRepository.FindByReference(
          confirmedTx.Reference,
          AccountType.Debit,
          recentYnabTransactions);

      // if reserved tx not in ynab, create it
      if (ynabTx == null) {
        if ((DateTime.Today -
             confirmedTx.Date.ToDateTime(TimeOnly.Parse("12:00AM")))
            .TotalDays <= 60) {
          await _ynabTransactionRepository.CreateTransaction(
            reference: confirmedTx.Reference,
            accountType: AccountType.Debit,
            amount: confirmedTx
              .Amount, // in confirmed transactions we know the sign, so do not convert here
            date: confirmedTx.Date,
            cleared: YnabTransactionCleared.Cleared,
            description: confirmedTx.Description,
            recentTransactions: recentYnabTransactions);
        }

        continue;
      }

      var finalDescription =
        confirmedTx.Description != "MOVIM. EN DOLARES ELECTRON"
          ? confirmedTx.Description
          : ynabTx.Metadata.Description;
      var whatMetadataShouldBe = new YnabTransactionMetadata(
        reference: confirmedTx.Reference,
        auto: ynabTx.Metadata.Auto,
        description: finalDescription,
        myDescription: ynabTx.Metadata.MyDescription,
        overrideDate: ynabTx.Metadata.OverrideDate
      );
      if (ynabTx.Metadata != whatMetadataShouldBe) {
        _ynabTransactionRepository.UpdateTransactionMetadata(ynabTx.Id,
          whatMetadataShouldBe);
      }

      // if (ynabTx.Metadata.OverrideDate.HasValue) {
      //   if (DateOnly.FromDateTime(ynabTx.Date) !=
      //       ynabTx.Metadata.OverrideDate.Value) {
      //     _ynabTransactionRepository.UpdateTransactionDate(ynabTx.Id,
      //       ynabTx.Metadata.OverrideDate.Value);
      //   }
      // }
      // else if (DateOnly.FromDateTime(ynabTx.Date) != confirmedTx.Date) {
      //   _ynabTransactionRepository.UpdateTransactionDate(ynabTx.Id,
      //     confirmedTx.Date);
      // }

      var (payeeId, categoryId) =
        YnabTransactionRepository.GetPayeeAndCategoryForDescription(
          finalDescription,
          recentYnabTransactions);
      if (ynabTx.PayeeId == null && payeeId != null) {
        _ynabTransactionRepository.UpdateTransactionPayee(ynabTx.Id, payeeId);
      }
      if (ynabTx.CategoryId == null && categoryId != null) {
        _ynabTransactionRepository.UpdateTransactionCategory(ynabTx.Id,
          categoryId);
      }

      // if (ynabTx.IsOpen) {
      if (confirmedTx.Amount != ynabTx.Amount) {
        _ynabTransactionRepository.UpdateTransactionAmount(ynabTx.Id,
          confirmedTx.Amount);
      }

      if (ynabTx.Cleared == YnabTransactionCleared.Uncleared) {
        _ynabTransactionRepository.UpdateTransactionSetToCleared(
          ynabTx.Id);
      }
      // }
    }
    await _ynabTransactionRepository.CommitChanges();
  }
}
