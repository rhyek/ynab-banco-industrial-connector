using Microsoft.Extensions.DependencyInjection;
using YnabBancoIndustrialConnector.Domain.MonitorJobs;

namespace YnabBancoIndustrialConnector.Domain;

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
