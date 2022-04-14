using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BancoIndustrialScraper.Models;

public enum TransactionType
{
  Debit,
  Credit,
}

public enum TransactionOrigin
{
  Establishment,
  Agency,
}

public record MobileNotificationTransaction()
{
  public string Reference { get; init; } = default!;
  public string Currency { get; init; } = default!;
  public decimal Amount { get; init; } = default!;
  public TransactionType Type { get; init; } = default!;
  public string Description { get; init; } = default!;
  public string Account { get; init; } = default!;
  public DateTime DateTime { get; init; } = default!;
  public TransactionOrigin Origin { get; init; } = default!;

  // https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings
  public static DateTime ParseDateTime(string str,
    DateTime? currentDateTime = null)
  {
    var finalCurrentDateTime = currentDateTime ?? DateTime.Now;
    var culture = CultureInfo.CreateSpecificCulture("es-ES");
    var datetime = DateTime.ParseExact(str, "dd-MMM HH:mm", culture);
    var datetimeLastYear = datetime.AddYears(-1);

    if (Math.Abs(finalCurrentDateTime.Ticks - datetime.Ticks) <
        Math.Abs(finalCurrentDateTime.Ticks - datetimeLastYear.Ticks)) {
      return datetime;
    }
    return datetimeLastYear;
  }

  public static MobileNotificationTransaction? FromMessage(string message,
    DateTime? currentDateTime = null)
  {
    const string datetimeRegex = @"(?<datetime>\d{2}-[a-zA-Z]{3} \d{2}:\d{2})";
    var regex = new Regex(
      @$"BiMovil: Se ha (?<operation>.+) (?<currency>(US|Q))\.(?<amount>.+) en (?<originPhrase>el Establecimiento|la Agencia): (?<description>.+) Cuenta: (?<account>.+) {datetimeRegex} (Aut\.|Autorizacion: )(?<reference>.+)\.");
    var match = regex.Match(message);
    if (match.Success) {
      var type = match.Groups["operation"].Value.Contains("credito")
        ? TransactionType.Credit
        : TransactionType.Debit;
      var origin =
        match.Groups["originPhrase"].Value.Contains("Establecimiento")
          ? TransactionOrigin.Establishment
          : TransactionOrigin.Agency;
      return new() {
        Reference = match.Groups["reference"].Value,
        Currency = match.Groups["currency"].Value,
        Amount = decimal.Parse(match.Groups["amount"].Value),
        Type = type,
        Description = match.Groups["description"].Value,
        Account = match.Groups["account"].Value,
        DateTime = ParseDateTime(match.Groups["datetime"].Value,
          currentDateTime),
        Origin = origin
      };
    }

    return null;
  }
};
