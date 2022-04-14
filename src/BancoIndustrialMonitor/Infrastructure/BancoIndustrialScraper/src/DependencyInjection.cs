using System.Reflection;
using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.Events;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.MonitorJobs;

namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper;

public static class DependencyInjection
{
  public static IServiceCollection AddBancoIndustrialScraper(
    this IServiceCollection services)
  {
    services.AddSingleton(
      Channel.CreateBounded<RequestReadReservedTransactionsEvent>(capacity: 1));
    services.AddSingleton(
      Channel.CreateBounded<RequestReadConfirmedTransactionsEvent>(
        capacity: 1));
    services.AddSingleton(
      Channel.CreateUnbounded<ReadReservedTransactionsEvent>());
    services.AddSingleton(Channel
      .CreateUnbounded<ReadConfirmedTransactionsEvent>());
    services.AddSingleton<ReservedTransactionsMonitorJob>();
    services.AddSingleton<ConfirmedTransactionsMonitorJob>();
    services.AddHostedService<BancoIndustrialScraperBackgroundService>(); 
    services.AddMediatR(Assembly.GetExecutingAssembly());
    return services;
  }
}
