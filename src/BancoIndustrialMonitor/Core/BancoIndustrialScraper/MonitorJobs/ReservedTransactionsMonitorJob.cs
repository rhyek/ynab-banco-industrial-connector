using System.Threading.Channels;
using BancoIndustrialMonitor.Application.BIScraper.Events;
using BancoIndustrialMonitor.Application.BIScraper.Interfaces;
using BancoIndustrialMonitor.Application.BIScraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace BancoIndustrialMonitor.Application.BIScraper.MonitorJobs;

public class ReservedTransactionsMonitorJob : IMonitorJob
{
  private readonly ILogger<ReservedTransactionsMonitorJob>
    _logger;

  private readonly Channel<ReadReservedTransactionsEvent>
    _readReservedTransactionsEventChannel;

  public ReservedTransactionsMonitorJob(
    ILogger<ReservedTransactionsMonitorJob> logger,
    Channel<ReadReservedTransactionsEvent>
      readReservedTransactionsEventChannel
  )
  {
    _logger = logger;
    _readReservedTransactionsEventChannel =
      readReservedTransactionsEventChannel;
  }

  public async Task Run(IPage page, IElementHandle accountCell,
    CancellationToken cancellationToken)
  {
    _logger.LogInformation("Reading reserved transactions...");

    await Task.Delay(TimeSpan.FromSeconds(2),
      cancellationToken);
    await accountCell.ClickAsync();
    await page.WaitForURLAsync(
      "**/InformacionCuentas/Monetario/InformacionCuentasMonetaria/SaldoCuentaMonetaria**");
    var electronCell = await page.WaitForSelectorAsync("text=\"Electron\"");
    if (await electronCell!.EvaluateHandleAsync(@"
      (element) =>
        element.closest('tr').querySelector(':scope > td:nth-child(3) a')
    ") is IElementHandle detailLink) {
      await Task.Delay(millisecondsDelay: 2_000,
        cancellationToken);
      await detailLink.ClickAsync();
      await page.WaitForURLAsync(
        "**/InformacionCuentas/Monetario/InformacionCuentasMonetaria/TarjetaElectron**");
      var txRowHandles =
        await page.QuerySelectorAllAsync(".tbl-report > tbody > tr");
      var reservedTransactions = (await Task.WhenAll(txRowHandles
          .Select(async (row) => {
            var dateCell =
              (await row.QuerySelectorAsync(":scope > :nth-child(1)"))!;
            var dateText =
              (await dateCell.TextContentAsync())!.Trim();
            var date =
              DateOnly.FromDateTime(DateTime.ParseExact(dateText,
                "dd/MM/yyyy HH:mm:ss",
                provider: null));

            var referenceNumberCell =
              (await row.QuerySelectorAsync(":scope > :nth-child(3)"))!;
            var referenceNumberText =
              (await referenceNumberCell.TextContentAsync())!.Trim();

            var amountCell =
              (await row.QuerySelectorAsync(":scope > :nth-child(4)"))!;
            var amountText = (await amountCell.TextContentAsync())!.Trim();
            var amount =
              decimal.Parse(amountText.Trim().Replace(",", ""));

            var statusCell =
              (await row.QuerySelectorAsync(":scope > :nth-child(5)"))!;
            var statusText = (await statusCell.TextContentAsync())!.Trim();

            return new {
              date,
              reference = referenceNumberText,
              amount,
              status = statusText,
            };
          })))
        .Where(t => t.status.ToUpper() == "VIGENTE")
        .Select(t => new ReservedTransaction(t.reference,
          t.date, t.amount))
        .OrderBy(t => t.Date)
        .ToList();
      await _readReservedTransactionsEventChannel.Writer.WriteAsync(
        new(reservedTransactions),
        cancellationToken);
      _logger.LogInformation("Notified {Count} reserved transactions",
        reservedTransactions.Count);
    }
  }
}
