using System.Globalization;
using System.Text.RegularExpressions;

namespace YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper.
  Models;

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
    datetime = datetime.AddYears(finalCurrentDateTime.Year - datetime.Year);
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
      @$"BiMovil: (Se ha )?(?<operation>.+) (?<currency>(.+?))\.(?<amount>.+) en\s?(?<originPhrase>el Establecimiento|la Agencia)?: (?<description>.+) Cuenta: (?<account>.+) {datetimeRegex} (Aut\.|Autorizacion: )(?<reference>.+)\.");
    var match = regex.Match(message);
    if (match.Success) {
      var currency = match.Groups["currency"].Value switch {
        "Q" => "GTQ",
        "US" => "USD",
        _ => match.Groups["currency"].Value
      };
      var type = match.Groups["operation"].Value.ToLower().Contains("credito")
        ? TransactionType.Credit
        : TransactionType.Debit;
      var origin = match.Groups["originPhrase"].Value switch {
        var s when s.ToLower().Contains("establecimiento") || s == "" => TransactionOrigin.Establishment,
        _ => TransactionOrigin.Agency
      };
      return new() {
        Reference = match.Groups["reference"].Value,
        Currency = currency,
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
