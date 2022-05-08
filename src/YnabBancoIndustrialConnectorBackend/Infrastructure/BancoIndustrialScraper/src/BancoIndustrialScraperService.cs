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

  private static IEnumerable<string> _GetBrowserFlags()
  {
    var flags = new List<string> {
      "--allow-running-insecure-content", // https://source.chromium.org/search?q=lang:cpp+symbol:kAllowRunningInsecureContent&ss=chromium
      "--autoplay-policy=user-gesture-required", // https://source.chromium.org/search?q=lang:cpp+symbol:kAutoplayPolicy&ss=chromium
      "--disable-component-update", // https://source.chromium.org/search?q=lang:cpp+symbol:kDisableComponentUpdate&ss=chromium
      "--disable-domain-reliability", // https://source.chromium.org/search?q=lang:cpp+symbol:kDisableDomainReliability&ss=chromium
      "--disable-features=AudioServiceOutOfProcess,IsolateOrigins,site-per-process", // https://source.chromium.org/search?q=file:content_features.cc&ss=chromium
      "--disable-print-preview", // https://source.chromium.org/search?q=lang:cpp+symbol:kDisablePrintPreview&ss=chromium
      "--disable-setuid-sandbox", // https://source.chromium.org/search?q=lang:cpp+symbol:kDisableSetuidSandbox&ss=chromium
      "--disable-site-isolation-trials", // https://source.chromium.org/search?q=lang:cpp+symbol:kDisableSiteIsolation&ss=chromium
      "--disable-speech-api", // https://source.chromium.org/search?q=lang:cpp+symbol:kDisableSpeechAPI&ss=chromium
      "--disable-web-security", // https://source.chromium.org/search?q=lang:cpp+symbol:kDisableWebSecurity&ss=chromium
      "--disk-cache-size=33554432", // https://source.chromium.org/search?q=lang:cpp+symbol:kDiskCacheSize&ss=chromium
      "--enable-features=SharedArrayBuffer", // https://source.chromium.org/search?q=file:content_features.cc&ss=chromium
      "--hide-scrollbars", // https://source.chromium.org/search?q=lang:cpp+symbol:kHideScrollbars&ss=chromium
      "--ignore-gpu-blocklist", // https://source.chromium.org/search?q=lang:cpp+symbol:kIgnoreGpuBlocklist&ss=chromium
      "--in-process-gpu", // https://source.chromium.org/search?q=lang:cpp+symbol:kInProcessGPU&ss=chromium
      "--mute-audio", // https://source.chromium.org/search?q=lang:cpp+symbol:kMuteAudio&ss=chromium
      "--no-default-browser-check", // https://source.chromium.org/search?q=lang:cpp+symbol:kNoDefaultBrowserCheck&ss=chromium
      "--no-pings", // https://source.chromium.org/search?q=lang:cpp+symbol:kNoPings&ss=chromium
      "--no-sandbox", // https://source.chromium.org/search?q=lang:cpp+symbol:kNoSandbox&ss=chromium
      "--no-zygote", // https://source.chromium.org/search?q=lang:cpp+symbol:kNoZygote&ss=chromium
      "--use-gl=swiftshader", // https://source.chromium.org/search?q=lang:cpp+symbol:kUseGl&ss=chromium
      "--window-size=1920,1080", // https://source.chromium.org/search?q=lang:cpp+symbol:kWindowSize&ss=chromium
    };
    if (!string.IsNullOrEmpty(
          Environment.GetEnvironmentVariable("IN_LAMBDA"))) {
      flags.Add(
        "--single-process"); // https://source.chromium.org/search?q=lang:cpp+symbol:kSingleProcess&ss=chromium"
    }
    return flags.ToArray();
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
    await using var browser =
      await playwright.Chromium.LaunchAsync(new() {
        Headless = true,
        // ExecutablePath = "/lambda-chromium/chromium",
        Args = _GetBrowserFlags(),
      });
    await using var context = await browser.NewContextAsync();
    await context.Tracing.StartAsync(new() {
      Screenshots = true,
      Snapshots = true,
    });
    var attempts = 0;
    const int maxAttempts = 1;
    while (!stoppingToken.IsCancellationRequested &&
           attempts < maxAttempts) {
      attempts++;
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
        if (attempts < maxAttempts) {
          await WaitFor(60_000, stoppingToken);
        }
      }
    }
    await context.Tracing.StopAsync(new ()
    {
      Path = "trace.zip"
    });
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
