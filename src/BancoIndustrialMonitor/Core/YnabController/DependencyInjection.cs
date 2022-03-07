using System.Reflection;
using BancoIndustrialMonitor.Application.YnabController.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BancoIndustrialMonitor.Application.YnabController;

public static class DependencyInjection
{
  public static IServiceCollection AddYnabController(this IServiceCollection services)
  {
    services.AddSingleton<YnabTransactionRepository>();
    services.AddHostedService<YnabControllerBackgroundService>();
    services.AddMediatR(Assembly.GetExecutingAssembly());
    return services;
  }
}
