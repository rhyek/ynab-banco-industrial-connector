namespace YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper;

public class BancoIndustrialScraperOptions
{
  public class BancoIndustrialScraperOptionsAuth
  {
    public string UserId { get; set; } = default!;

    public string Username { get; set; } = default!;

    public string Password { get; set; } = default!;
  }

  public BancoIndustrialScraperOptionsAuth? Auth { get; set; }

  public string AccountId { get; set; } = default!;

  public string PlaywrightServerUrl { get; set; } = default!;

  public string PlaywrightTraceFile { get; set; } =
    "/tmp/banco-industrial-scraper-trace.zip";
}
