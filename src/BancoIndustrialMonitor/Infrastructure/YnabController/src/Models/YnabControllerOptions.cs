namespace YnabBancoIndustrialConnector.Infrastructure.YnabController.
  YnabApiResponses;

public class YnabControllerOptions
{
  public string PersonalAccessToken { get; set; } = default!;

  public string BudgetId { get; set; } = default!;

  public string AccountId { get; set; } = default!;

  public string BiMobileNotificationAccountNameForEstablishmentTransactions {
    get;
    set;
  } = default!;
}
