// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-6.0
// https://github.com/tonerdo/dotnet-env
// https://dusted.codes/dotenv-in-dotnet
using BancoIndustrialMonitor.Application.YnabController.Models;
using BancoIndustrialScraper.Models;

namespace BancoIndustrialMonitor.Programs.HttpApi;

public static class ConfigureOptionsFromEnvsExtensionMethod
{
  private static string GetEnvironmentVariableOrFail(string key)
  {
    return Environment.GetEnvironmentVariable(key) ??
           throw new InvalidOperationException(
             $"Environment variable \"{key}\" not set");
  }

  public static void ConfigureOptionsFromEnvs(
    this IServiceCollection serviceCollection)
  {
    serviceCollection.Configure<YnabControllerOptions>(
      (ynabControllerOptions) => {
        ynabControllerOptions.BudgetId =
          GetEnvironmentVariableOrFail("YNAB_BUDGET_ID");
        ynabControllerOptions.AccountId =
          GetEnvironmentVariableOrFail("YNAB_ACCOUNT_ID");
        ynabControllerOptions.PersonalAccessToken =
          GetEnvironmentVariableOrFail("YNAB_PERSONAL_ACCESS_TOKEN");
        ynabControllerOptions
            .BiMobileNotificationAccountNameForEstablishmentTransactions
          = GetEnvironmentVariableOrFail(
            "BANCO_INDUSTRIAL_MOBILE_NOTIFICATION_ACCOUNT_NAME_FOR_ESTABLISHMENT_TRANSACTIONS");
      });
    serviceCollection.Configure<BancoIndustrialScraperOptions>(
      (bancoIndustrialScraperOptions) => {
        bancoIndustrialScraperOptions.Auth = new() {
          UserId =
            GetEnvironmentVariableOrFail(
              "BANCO_INDUSTRIAL_SCRAPER_AUTH_USER_ID"),
          Username =
            GetEnvironmentVariableOrFail(
              "BANCO_INDUSTRIAL_SCRAPER_AUTH_USERNAME"),
          Password =
            GetEnvironmentVariableOrFail(
              "BANCO_INDUSTRIAL_SCRAPER_AUTH_PASSWORD")
        };
        bancoIndustrialScraperOptions.AccountId =
          GetEnvironmentVariableOrFail("BANCO_INDUSTRIAL_SCRAPER_ACCOUNT_ID");
      });
  }
}
