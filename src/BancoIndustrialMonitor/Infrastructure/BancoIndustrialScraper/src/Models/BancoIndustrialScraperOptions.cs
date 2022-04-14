namespace YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper.Models;

public class BancoIndustrialScraperOptions
{
  public class BancoIndustrialScraperOptionsAuth
  {
    public string UserId { get; set; } = default!;

    public string Username { get; set; } = default!;

    public string Password { get; set; } = default!;
  }

  public BancoIndustrialScraperOptionsAuth Auth { get; set; } = default!;

  public string AccountId { get; set; } = default!;
}
