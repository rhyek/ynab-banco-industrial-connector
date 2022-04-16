using Microsoft.Extensions.DependencyInjection;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.MonitorJobs;

namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper;

public static class DependencyInjection
{
  public static IServiceCollection AddBancoIndustrialScraper(
    this IServiceCollection services)
  {
    services.AddSingleton<ReservedTransactionsScraperJob>();
    services.AddSingleton<ConfirmedTransactionsScraperJob>();
    services.AddSingleton<BancoIndustrialScraperService>();
    return services;
  }
}
