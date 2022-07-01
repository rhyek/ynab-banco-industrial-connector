using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using YnabBancoIndustrialConnector.Domain.Interfaces;
using YnabBancoIndustrialConnector.Domain.Models;
using YnabBancoIndustrialConnector.Domain.MonitorJobs;
using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper;

namespace YnabBancoIndustrialConnector.Domain;

// https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice#implementing-ihostedservice-with-a-custom-hosted-service-class-deriving-from-the-backgroundservice-base-class
public class BancoIndustrialScraperService
{
  private readonly IHostEnvironment _hostEnvironment;
  private readonly BancoIndustrialScraperOptions _options;
  private readonly ILogger<BancoIndustrialScraperService> _logger;
  private readonly SemaphoreSlim _monitorSemaphore = new(initialCount: 1);
  private readonly ReservedTransactionsScraperJob
    _reservedTransactionsScraperJob;
  private readonly ConfirmedTransactionsScraperJob
    _confirmedTransactionsScraperJob;

  public BancoIndustrialScraperService
  (
    IHostEnvironment hostEnvironment,
    IOptions<BancoIndustrialScraperOptions> options,
    ILogger<BancoIndustrialScraperService> logger,
    ReservedTransactionsScraperJob reservedTransactionsScraperJob,
    ConfirmedTransactionsScraperJob confirmedTransactionsScraperJob)
  {
    _hostEnvironment = hostEnvironment;
    _options = options.Value;
    _logger = logger;
    _reservedTransactionsScraperJob = reservedTransactionsScraperJob;
    _confirmedTransactionsScraperJob = confirmedTransactionsScraperJob;
  }

  public Task<IList<ReservedBankTransaction>?> ScrapeReservedTransactions(
    CancellationToken stoppingToken)
  {
    return ScrapeWithJob(_reservedTransactionsScraperJob, stoppingToken);
  }

  public Task<IList<ConfirmedBankTransaction>?> ScrapeConfirmedTransactions(
    CancellationToken stoppingToken)
  {
    return ScrapeWithJob(_confirmedTransactionsScraperJob, stoppingToken);
  }

  private async Task<T?> ScrapeWithJob<T>(IScraperJob<T> scraperJob,
    CancellationToken stoppingToken) where T : class
  {
    T? result = null;
    await _monitorSemaphore.WaitAsync(stoppingToken);

    _logger.LogInformation("Scraping with job {JobName}",
      scraperJob.GetType().Name);
    _logger.LogInformation("Username: {Username}", _options.Auth?.Username);

    using var playwright = await Playwright.CreateAsync();
    await using var browser = _hostEnvironment.IsDevelopment()
      ? await playwright.Chromium.LaunchAsync(new() {
        Headless = true,
      })
      : await playwright.Chromium.ConnectAsync(_options.PlaywrightServerUrl);
    await using var context = await browser.NewContextAsync(new() {
      UserAgent =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.64 Safari/537.36"
    });
    var attempts = 0;
    const int maxAttempts = 3;
    while (!stoppingToken.IsCancellationRequested &&
           attempts < maxAttempts) {
      attempts++;
      var shouldTrace =
        attempts == maxAttempts && !_hostEnvironment.IsDevelopment();
      if (shouldTrace) {
        await context.Tracing.StartAsync(new() {
          Screenshots = true,
          Snapshots = true,
        });
      }
      _logger.LogInformation(
        "Attempt: {Attempts}", attempts);
      _logger.LogInformation("Logging in to bank...");
      if (_options.Auth == null) {
        throw new Exception("Options are null");
      }
      try {
        var page = await context.NewPageAsync();
        await page.GotoAsync(
          "https://www.bienlinea.bi.com.gt/InicioSesion/Inicio/Autenticar");
        await page.FillAsync("#campoInstalacion", _options.Auth.UserId);
        await page.FillAsync("#campoUsuario", _options.Auth.Username);
        await page.FillAsync("#campoContrasenia", _options.Auth.Password);
        await page.ClickAsync("#autenticar");
        await page.WaitForURLAsync(
          "**/InicioSesion/Token/BienvenidoDashBoard");

        var accountCell = await GetAccountCell(_options.AccountId, page,
          stoppingToken);
        result = await scraperJob.Run(page, accountCell, stoppingToken);
        break;
      }
      catch (TaskCanceledException) {
        _logger.LogInformation("Task cancelled");
        break;
      }
      catch (Exception e) {
        _logger.LogError(e, message: "Some error");
        if (shouldTrace) {
          await context.Tracing.StopAsync(new() {
            Path = _options.PlaywrightTraceFile,
          });
        }
        if (attempts < maxAttempts) {
          await WaitFor(60_000, stoppingToken);
        }
      }
    }
    if (result != null) {
      _logger.LogInformation("Job finished");
    }
    else {
      _logger.LogInformation(
        "Could not complete job after {Attempts} attempts", attempts);
    }

    _monitorSemaphore.Release();

    return result;
  }

  private async Task WaitFor(int milliseconds,
    CancellationToken cancellationToken)
  {
    _logger.LogInformation(
      "Waiting for {Seconds} seconds...", milliseconds / 1_000);
    await Task.Delay(milliseconds,
      cancellationToken);
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
