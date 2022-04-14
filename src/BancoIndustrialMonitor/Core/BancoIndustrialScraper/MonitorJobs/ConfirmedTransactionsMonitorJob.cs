using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BancoIndustrialMonitor.Application.BIScraper.Events;
using BancoIndustrialMonitor.Application.BIScraper.Interfaces;
using BancoIndustrialMonitor.Application.BIScraper.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace BancoIndustrialMonitor.Application.BIScraper.MonitorJobs;

public class ConfirmedTransactionsMonitorJob : IMonitorJob
{
  private readonly ILogger<ConfirmedTransactionsMonitorJob>
    _logger;

  private readonly Channel<ReadConfirmedTransactionsEvent>
    _readConfirmedTransactionsEventChannel;

  private readonly MemoryCache _cache = new(new MemoryCacheOptions());

  public ConfirmedTransactionsMonitorJob(
    ILogger<ConfirmedTransactionsMonitorJob> logger,
    Channel<ReadConfirmedTransactionsEvent>
      readConfirmedTransactionsEventChannel
  )
  {
    _logger = logger;
    _readConfirmedTransactionsEventChannel =
      readConfirmedTransactionsEventChannel;
  }

  private async Task<IList<ConfirmedTransaction>> GetConfirmedTransactions(
    IPage page, IElementHandle accountCell,
    CancellationToken cancellationToken)
  {
    _logger.LogInformation("Reading confirmed transactions...");

    var confirmedTransactions = new List<ConfirmedTransaction>();
    if (await accountCell.EvaluateHandleAsync(@"
      (element) =>
        element.closest('tr').querySelector(':scope > td:nth-child(5) a')
    ") is IElementHandle optionsButton) {
      await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
      await optionsButton.ClickAsync();
      var dropdown = (await page.WaitForSelectorAsync(".f-open-dropdown"))!;
      var historyLink =
        (await dropdown.QuerySelectorAsync("text=\"HISTÓRICO\""))!;
      await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
      await historyLink.ClickAsync();
      var iteration = 1;
      while (iteration < 3) {
        await page.WaitForURLAsync(
          "**/InformacionCuentas/Monetario/InformacionCuentasMonetaria/Historico**");
        var byMonthButton =
          (await page.QuerySelectorAsync("text=\"Por mes\""))!;
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        await byMonthButton.ClickAsync();
        var consultButton =
          (await page.WaitForSelectorAsync("button#btMes"))!;
        // if second run, click on last month first, then consult
        if (iteration > 1) {
          var lastMonthButton =
            (await page.QuerySelectorAllAsync("#form_Mont a.tomarMes"))[4];
          await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
          await lastMonthButton.ClickAsync();
        }

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        await consultButton.ClickAsync();
        await page.WaitForURLAsync(
          "**/InformacionCuentas/Monetario/InformacionCuentasMonetaria/ConsultaPorMes");


        var titleWithYearH2 =
          (await page.WaitForSelectorAsync(
            "text=\"Estado de cuenta del mes:\""))!;
        var dateText =
          (await (await titleWithYearH2.QuerySelectorAsync("span"))!
            .TextContentAsync())!;
        var year = int.Parse(dateText[^4..]); // last four using range indexer
        var trDateRegex = new Regex(@"^(?<day>\d+) - (?<month>\d+)$");
        var monthConfirmedTransactions = (await
          Task.WhenAll(
            (await page.QuerySelectorAllAsync(".tbl-report tbody tr"))
            .Select(async (tr) => {
              int day;
              int month;
              var dateText =
                (await (await tr.QuerySelectorAsync(":scope > td:nth-child(1)"))
                  !
                  .TextContentAsync())!;
              var dateMatch = trDateRegex.Match(dateText);
              if (dateMatch.Success) {
                day = int.Parse(dateMatch.Groups["day"].Value);
                month = int.Parse(dateMatch.Groups["month"].Value);
              }
              else {
                throw new($"Date didn't match: {dateText}");
              }

              var date = DateOnly.FromDateTime(new(year, month, day));

              var description =
                (await (await tr.QuerySelectorAsync(":scope > td:nth-child(3)"))
                  !
                  .TextContentAsync())!.Trim();

              var reference =
                (await (await tr.QuerySelectorAsync(":scope > td:nth-child(4)"))
                  !
                  .TextContentAsync())!.Trim();

              var debitText =
                (await (await tr.QuerySelectorAsync(":scope > td:nth-child(5)"))
                  !
                  .TextContentAsync())!.Trim();
              var creditText =
                (await (await tr.QuerySelectorAsync(":scope > td:nth-child(6)"))
                  !
                  .TextContentAsync())!.Trim();
              var signMultiplier = 1;
              string amountText;
              if (debitText.Length > 0) {
                signMultiplier = -1;
                amountText = debitText;
              }
              else {
                amountText = creditText;
              }

              var amountRegex = new Regex(@"([\d.,]+)$");
              var amount = signMultiplier *
                           decimal.Parse(
                             amountRegex
                               .Match(amountText.Trim()).Value
                           );

              return new ConfirmedTransaction(
                date,
                description,
                reference,
                amount
              );
            }))).ToList();
        confirmedTransactions.AddRange(monthConfirmedTransactions);

        // only go back to month list if it's the first run
        if (iteration == 1) {
          var goBackButton =
            (await page.WaitForSelectorAsync("button:has-text(\"Regresar\")"))!;
          await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
          await goBackButton.ClickAsync();
        }

        iteration++;
      }
    }

    confirmedTransactions =
      confirmedTransactions.OrderBy((t) => t.Date).ToList();

    return confirmedTransactions;
  }

  public async Task Run(IPage page, IElementHandle accountCell,
    CancellationToken cancellationToken)
  {
    var confirmedTransactions = await _cache.GetOrCreateAsync(
      "confirmedTransactions",
      cacheEntry => {
        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return GetConfirmedTransactions(page, accountCell, cancellationToken);
      });

    await _readConfirmedTransactionsEventChannel.Writer.WriteAsync(
      new(confirmedTransactions), cancellationToken);
    _logger.LogInformation("Notified {Count} confirmed transactions",
      confirmedTransactions.Count);
  }
}
