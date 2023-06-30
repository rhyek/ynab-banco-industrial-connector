namespace YnabBancoIndustrialConnector.Application;

public class ApplicationOptions
{
  public string BancoIndustrialMobileNotificationDebitCardAccountName {
    get;
    set;
  } = default!;

  public string BancoIndustrialMobileNotificationCreditCardAccountName {
    get;
    set;
  } = default!;

  public string? ScrapeBankTransactionsSqsUrl { get; set; } = null;

  public string? DuplicateConfirmedReferencesSqsUrl { get; set; } = null;
}
