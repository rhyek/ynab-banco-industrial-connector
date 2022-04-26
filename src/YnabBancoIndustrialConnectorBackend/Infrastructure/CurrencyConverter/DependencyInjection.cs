using Microsoft.Extensions.DependencyInjection;
using YnabBancoIndustrialConnector.Interfaces;

namespace YnabBancoIndustrialConnector.Infrastructure.CurrencyConverter;

public static class DependencyInjection
{
  public static IServiceCollection AddCurrencyConverter(
    this IServiceCollection serviceCollection)
  {
    serviceCollection
      .AddSingleton<ICurrencyConverterService, CurrencyConverterService>();
    return serviceCollection;
  }
}
