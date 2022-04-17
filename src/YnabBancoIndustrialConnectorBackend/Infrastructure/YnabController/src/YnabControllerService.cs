using Microsoft.Extensions.Logging;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.Models;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Models;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Repositories;

namespace YnabBancoIndustrialConnector.Infrastructure.YnabController;

public class YnabControllerService
{
  private readonly ILogger<YnabControllerService> _logger;

  private readonly YnabTransactionRepository _ynabTransactionRepository;

  public YnabControllerService
  (
    ILogger<YnabControllerService> logger,
    YnabTransactionRepository ynabTransactionRepository
  )
  {
    _logger = logger;
    _ynabTransactionRepository = ynabTransactionRepository;
  }

  public async Task ProcessReservedBankTransactions(
    IList<ReservedBankTransaction> reservedBankTransactions,
    CancellationToken stoppingToken)
  {
    _logger.LogInformation("Processing {Count} reserved bank transactions",
      reservedBankTransactions.Count);
    var recentYnabTransactions = await _ynabTransactionRepository.GetRecent();
    foreach (var reservedTx in reservedBankTransactions) {
      var (reference, date, amount) = reservedTx;
      var ynabTx =
        await _ynabTransactionRepository.FindByReference(reference,
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
          -amount,
          date,
          YnabTransactionCleared.Uncleared,
          recentTransactions: recentYnabTransactions);
      }
    }

    await _ynabTransactionRepository.CommitChanges();
  }

  public async Task ProcessConfirmedBankTransactions(
    IList<ConfirmedBankTransaction> confirmedBankTransactions,
    CancellationToken stoppingToken)
  {
    _logger.LogInformation("Processing {Count} confirmed bank transactions",
      confirmedBankTransactions.Count);
    var recentYnabTransactions = await _ynabTransactionRepository.GetRecent();
    foreach (var confirmedTx in confirmedBankTransactions) {
      var ynabTx =
        await _ynabTransactionRepository.FindByReference(
          confirmedTx.Reference,
          recentYnabTransactions);

      // if reserved tx not in ynab, create it
      if (ynabTx == null) {
        if (confirmedTx.Date >= new DateOnly(2022, 2, 20)) {
          await _ynabTransactionRepository.CreateTransaction(
            reference: confirmedTx.Reference,
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

      if (ynabTx.IsOpen) {
        if (confirmedTx.Amount != ynabTx.Amount) {
          _ynabTransactionRepository.UpdateTransactionAmount(ynabTx.Id,
            confirmedTx.Amount);
        }

        if (ynabTx.Cleared == YnabTransactionCleared.Uncleared) {
          _ynabTransactionRepository.UpdateTransactionSetToCleared(
            ynabTx.Id);
        }
      }
    }
    await _ynabTransactionRepository.CommitChanges();
  }
}
