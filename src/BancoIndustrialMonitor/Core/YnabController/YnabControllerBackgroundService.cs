using System.Threading.Channels;
using BancoIndustrialMonitor.Application.BIScraper.Events;
using BancoIndustrialMonitor.Application.YnabController.Models;
using BancoIndustrialMonitor.Application.YnabController.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BancoIndustrialMonitor.Application.YnabController;

public class YnabControllerBackgroundService : BackgroundService
{
  private readonly ILogger<YnabControllerBackgroundService> _logger;

  private readonly Channel<ReadReservedTransactionsEvent>
    _readReservedTransactionsEventChannel;

  private readonly Channel<ReadConfirmedTransactionsEvent>
    _readConfirmedTransactionsEventChannel;

  private readonly YnabTransactionRepository _ynabTransactionRepository;

  public YnabControllerBackgroundService
  (
    ILogger<YnabControllerBackgroundService> logger,
    YnabTransactionRepository ynabTransactionRepository,
    Channel<ReadReservedTransactionsEvent> readReservedTransactionsEventChannel,
    Channel<ReadConfirmedTransactionsEvent>
      readConfirmedTransactionsEventChannel
  )
  {
    _logger = logger;
    _ynabTransactionRepository = ynabTransactionRepository;
    _readReservedTransactionsEventChannel =
      readReservedTransactionsEventChannel;
    _readConfirmedTransactionsEventChannel =
      readConfirmedTransactionsEventChannel;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await Task.WhenAll(
      ProcessReservedTransactions(stoppingToken),
      ProcessConfirmedTransactions(stoppingToken));
  }

  private async Task ProcessReservedTransactions(
    CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested) {
      var readReservedTransactionsEvent =
        await _readReservedTransactionsEventChannel.Reader.ReadAsync(
          stoppingToken);
      var recentYnabTransactions = await _ynabTransactionRepository.GetRecent();
      foreach (var reservedTx in readReservedTransactionsEvent
                 .ReservedTransactions) {
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
  }

  private async Task ProcessConfirmedTransactions(
    CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested) {
      var readConfirmedTransactionsEvent =
        await _readConfirmedTransactionsEventChannel.Reader.ReadAsync(
          stoppingToken);
      var recentYnabTransactions = await _ynabTransactionRepository.GetRecent();
      foreach (var confirmedTx in readConfirmedTransactionsEvent
                 .ConfirmedTransactions) {
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

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    await base.StopAsync(cancellationToken);
    _logger.LogInformation("Bye");
  }
}
