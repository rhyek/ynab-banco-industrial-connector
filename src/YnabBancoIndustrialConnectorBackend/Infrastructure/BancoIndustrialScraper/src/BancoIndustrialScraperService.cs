using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.Interfaces;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.Models;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.MonitorJobs;

namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper;

// https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice#implementing-ihostedservice-with-a-custom-hosted-service-class-deriving-from-the-backgroundservice-base-class
public class BancoIndustrialScraperService
{
  private readonly BancoIndustrialScraperOptions _options;

  private readonly ILogger<BancoIndustrialScraperService> _logger;

  private readonly SemaphoreSlim _monitorSemaphore = new(initialCount: 1);

  private readonly ReservedTransactionsScraperJob
    _reservedTransactionsScraperJob;

  private readonly ConfirmedTransactionsScraperJob
    _confirmedTransactionsScraperJob;

  public BancoIndustrialScraperService
  (
    IOptions<BancoIndustrialScraperOptions> options,
    ILogger<BancoIndustrialScraperService> logger,
    ReservedTransactionsScraperJob reservedTransactionsScraperJob,
    ConfirmedTransactionsScraperJob confirmedTransactionsScraperJob
  )
  {
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

    using var playwright = await Playwright.CreateAsync();
    await using var browser =
      await playwright.Chromium.LaunchAsync(new() {
        Headless = true
      });

    var attempts = 1;
    while (!stoppingToken.IsCancellationRequested &&
           attempts <= 3) {
      _logger.LogInformation(
        "Attempt: {Attempts}", attempts);
      _logger.LogInformation("Logging in to bank...");
      if (_options.Auth == null) {
        throw new Exception("Options are null");
      }
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

        var accountCell = await GetAccountCell(_options.AccountId, page,
          stoppingToken);
        result = await scraperJob.Run(page, accountCell, stoppingToken);
        break;
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