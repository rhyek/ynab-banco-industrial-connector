using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Repositories;

namespace YnabBancoIndustrialConnector.Infrastructure.YnabController;

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
