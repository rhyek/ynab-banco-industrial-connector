// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-6.0
// https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider
// https://github.com/tonerdo/dotnet-env
// https://dusted.codes/dotenv-in-dotnet

using YnabBancoIndustrialConnector.Application;
using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper;
using YnabBancoIndustrialConnector.Infrastructure.YnabController;

namespace YnabBancoIndustrialConnector.Programs.HttpApi;

public static class ConfigureOptionsExtensionMethods
{
  public static void ConfigureOptions(
    this IServiceCollection serviceCollection, IConfiguration config)
  {
    serviceCollection.Configure<YnabControllerOptions>(
      config.GetSection("YNAB"));
    serviceCollection.Configure<BancoIndustrialScraperOptions>(
      config.GetSection("BANCO_INDUSTRIAL_SCRAPER"));
    serviceCollection.Configure<ApplicationOptions>(
      config.GetSection("APPLICATION"));
  }
}
