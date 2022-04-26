namespace YnabBancoIndustrialConnector.Application;

public class ApplicationOptions
{
  public string BancoIndustrialMobileNotificationDebitCardAccountNameForEstablishmentTransactions {
    get;
    set;
  } = default!;

  public string BancoIndustrialMobileNotificationCreditCardAccountNameForEstablishmentTransactions {
    get;
    set;
  } = default!;

  public string? ScrapeBankTransactionsSqsUrl { get; set; } = null;
}
