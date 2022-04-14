using System.Collections.Generic;
using YnabBancoIndustrialConnector.Infrastructure.BIScraper.Models;

namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper.Events;

public record ReadReservedTransactionsEvent(
  IList<ReservedTransaction> ReservedTransactions);
