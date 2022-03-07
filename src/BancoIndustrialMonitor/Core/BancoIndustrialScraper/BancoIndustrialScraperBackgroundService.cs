using System.Threading.Channels;
using BancoIndustrialMonitor.Application.BIScraper.Events;
using BancoIndustrialMonitor.Application.BIScraper.Interfaces;
using BancoIndustrialMonitor.Application.BIScraper.MonitorJobs;
using BancoIndustrialScraper.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace BancoIndustrialMonitor.Application.BIScraper;

// https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice#implementing-ihostedservice-with-a-custom-hosted-service-class-deriving-from-the-backgroundservice-base-class
public class BancoIndustrialScraperBackgroundService : BackgroundService
{
  private readonly BancoIndustrialScraperOptions _options;

  private readonly ILogger<BancoIndustrialScraperBackgroundService> _logger;

  private readonly Channel<RequestReadReservedTransactionsEvent>
    _requestReadReservedTransactionsEventChannel;

  private readonly Channel<RequestReadConfirmedTransactionsEvent>
    _requestReadConfirmedTransactionsEventChannel;

  private readonly SemaphoreSlim _monitorSemaphore = new(initialCount: 1);

  private readonly ReservedTransactionsMonitorJob
    _reservedTransactionsMonitorJob;

  private readonly ConfirmedTransactionsMonitorJob
    _confirmedTransactionsMonitorJob;

  public BancoIndustrialScraperBackgroundService
  (
    IOptions<BancoIndustrialScraperOptions> options,
    ILogger<BancoIndustrialScraperBackgroundService> logger,
    Channel<RequestReadReservedTransactionsEvent>
      requestReadReservedTransactionsEventChannel,
    Channel<RequestReadConfirmedTransactionsEvent>
      requestReadConfirmedTransactionsEventChannel,
    ReservedTransactionsMonitorJob reservedTransactionsMonitorJob,
    ConfirmedTransactionsMonitorJob confirmedTransactionsMonitorJob
  )
  {
    _options = options.Value;
    _logger = logger;
    _requestReadReservedTransactionsEventChannel =
      requestReadReservedTransactionsEventChannel;
    _requestReadConfirmedTransactionsEventChannel =
      requestReadConfirmedTransactionsEventChannel;
    _reservedTransactionsMonitorJob = reservedTransactionsMonitorJob;
    _confirmedTransactionsMonitorJob = confirmedTransactionsMonitorJob;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var reservedTask = Task.Run(async () => {
        var reader = _requestReadReservedTransactionsEventChannel.Reader;
        while (!stoppingToken.IsCancellationRequested) {
          await reader.ReadAsync(
            stoppingToken); // allow to queue up to 1 while processing previous
          await Monitor(
            new(Reserved: true), stoppingToken);
        }
      },
      CancellationToken.None);
    var confirmedTask = Task.Run(async () => {
        var reader = _requestReadConfirmedTransactionsEventChannel.Reader;
        while (!stoppingToken.IsCancellationRequested) {
          await reader.ReadAsync(
            stoppingToken); // allow to queue up to 1 while processing previous
          await Monitor(
            new(Confirmed: true), stoppingToken);
        }
      },
      CancellationToken.None);
    await Task.WhenAll(reservedTask, confirmedTask);
  }

  private async Task WaitFor(int milliseconds,
    CancellationToken cancellationToken)
  {
    _logger.LogInformation(
      "Waiting for {Seconds} seconds...", milliseconds / 1_000);
    await Task.Delay(milliseconds,
      cancellationToken);
  }

  private async Task Monitor(RequestReadTransactionsEvent requestReadEvent,
    CancellationToken stoppingToken)
  {
    await _monitorSemaphore.WaitAsync(stoppingToken);

    using var playwright = await Playwright.CreateAsync();
    await using var browser =
      await playwright.Chromium.LaunchAsync(new() {
        Headless = true
      });
    _logger.LogInformation(
      "Received request to read transactions: Reserved: {Reserved}, Confirmed: {Confirmed}",
      requestReadEvent.Reserved, requestReadEvent.Confirmed);
    var pendingJobs = new List<IMonitorJob>();
    if (requestReadEvent.Reserved) {
      pendingJobs.Add(_reservedTransactionsMonitorJob);
    }
    if (requestReadEvent.Confirmed) {
      pendingJobs.Add(_confirmedTransactionsMonitorJob);
    }

    var attempts = 1;
    while (!stoppingToken.IsCancellationRequested && pendingJobs.Count > 0 &&
           attempts <= 3) {
      _logger.LogInformation(
        "Attempt: {Attempts}. Pending jobs: {PendingCount}",
        attempts, pendingJobs.Count);
      _logger.LogInformation("Logging in to bank...");
      try {
        var page = await browser.NewPageAsync();
        await page.GotoAsync(
          "https://www.bienlinea.bi.com.gt/InicioSesion/Inicio/Autenticar");
        await page.FillAsync("#campoInstalacion", _options.Auth.UserId);
        await page.FillAsync("#campoUsuario", _options.Auth.Username);
        await page.FillAsync("#campoContrasenia", _options.Auth.Password);
        await page.ClickAsync("#autenticar");
        await page.WaitForURLAsync(
          "**/InicioSesion/Token/BienvenidoDashBoard");

        var pendingJobsCopy = pendingJobs.ToList();
        foreach (var job in pendingJobsCopy) {
          var accountCell = await GetAccountCell(_options.AccountId, page,
            stoppingToken);
          await job.Run(page, accountCell, stoppingToken);
          pendingJobs.Remove(job);
          if (pendingJobsCopy.Last() != job) {
            await Task.Delay(millisecondsDelay: 2_000,
              stoppingToken);
            await page.GotoAsync(
              "https://www.bienlinea.bi.com.gt/InicioSesion/Token/BienvenidoDashBoard");
          }
        }
      }
      catch (TaskCanceledException) {
        _logger.LogInformation("Task canceled");
        break;
      }
      catch (Exception e) {
        _logger.LogError(e, message: "Some error");
        attempts++;
        await WaitFor(60_000, stoppingToken);
      }
    }
    if (pendingJobs.Count == 0) {
      _logger.LogInformation("Jobs finished");
    }
    else {
      _logger.LogInformation(
        "Could not complete {Count} job(s) after {Attempts} attempts",
        pendingJobs.Count, attempts);
    }

    _monitorSemaphore.Release();
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    await base.StopAsync(cancellationToken);
    _logger.LogInformation("Monitor stopped");
  }

  private static async Task<IElementHandle> GetAccountCell(string accountId,
    IPage page,
    CancellationToken cancellationToken)
  {
    await Task.Delay(millisecondsDelay: 2_000,
      cancellationToken);
    await page.ClickAsync(
      "#mainmenu > div.row > div > ul > li:nth-child(1) > a");
    await Task.Delay(millisecondsDelay: 1_000,
      cancellationToken);
    await page.ClickAsync(
      "#mainmenu-op0 > div > div:nth-child(1) > ul > li.drp.mnu-ctamnt > a");
    await page.WaitForURLAsync(
      "**/InformacionCuentas/Monetario/InformacionCuentasMonetaria/ICMonetariaAmbas");
    var accountCell =
      await page.WaitForSelectorAsync($"text=\"{accountId}\"");
    if (accountCell == null) {
      throw new("Account cell was null.");
    }

    return accountCell;
  }

  // private async Task ReadCurrentBalance(IPage page,
  //   CancellationToken cancellationToken)
  // {
  //   _logger.LogInformation("Obteniendo saldo disponible...");
  //   var accountCell = await GetAccountCell(page, cancellationToken);
  //   var amountStr = await accountCell.EvaluateAsync<string>(@"
  //     (element) =>
  //       element
  //         .closest('tr')
  //         .querySelector(':scope > td:nth-child(3)')
  //         .textContent
  //   ");
  //   var amount = decimal.Parse(amountStr.Replace(",", ""));
  //   _logger.LogInformation("Saldo disponible: {Amount}", amount);
  // }
}
