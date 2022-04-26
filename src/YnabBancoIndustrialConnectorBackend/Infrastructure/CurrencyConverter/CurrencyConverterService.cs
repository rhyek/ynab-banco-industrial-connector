using Flurl.Http;
using Newtonsoft.Json.Linq;
using YnabBancoIndustrialConnector.Interfaces;

namespace YnabBancoIndustrialConnector.Infrastructure.CurrencyConverter;

public class CurrencyConverterService : ICurrencyConverterService
{
  private readonly FlurlClient _client =
    new(
      "https://cdn.jsdelivr.net/gh/fawazahmed0/currency-api@1/latest/currencies");

  private async Task<Dictionary<string, decimal>?>
    GetExchangeRatesForCurrencyCode(
      string currencyCode)
  {
    var currencyCodeLower = currencyCode.ToLower();
    var response = await _client.Request($"{currencyCodeLower}.min.json")
      .GetJsonAsync<Dictionary<string, dynamic>>();
    var entry = response[currencyCodeLower] as JObject;
    return entry?.ToObject<Dictionary<string, decimal>>();
  }

  public async Task<decimal> ToUsd(string fromCurrencyCode, decimal fromAmount)
  {
    var exchangeRates = await GetExchangeRatesForCurrencyCode("usd");
    if (exchangeRates == null) {
      return 0m;
    }
    var exchangeRate = exchangeRates[fromCurrencyCode.ToLower()];
    return fromAmount / exchangeRate;
  }
}
