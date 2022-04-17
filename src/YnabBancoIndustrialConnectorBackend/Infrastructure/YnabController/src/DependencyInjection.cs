using Microsoft.Extensions.DependencyInjection;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Repositories;

namespace YnabBancoIndustrialConnector.Infrastructure.YnabController;

public static class DependencyInjection
{
  public static IServiceCollection AddYnabController(this IServiceCollection services)
  {
    services.AddSingleton<YnabTransactionRepository>();
    services.AddSingleton<YnabControllerService>();
    return services;
  }
}
