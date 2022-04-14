using System.Collections.Generic;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.Models;

namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper.Events;

public record ReadConfirmedTransactionsEvent(
  IList<ConfirmedTransaction> ConfirmedTransactions);
