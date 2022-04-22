namespace YnabBancoIndustrialConnector.Infrastructure.YnabController;

public class YnabControllerOptions
{
  public string PersonalAccessToken { get; set; } = default!;

  public string BudgetId { get; set; } = default!;

  public string DebitCardAccountId { get; set; } = default!;
  
  public string CreditCardAccountId { get; set; } = default!;
}
