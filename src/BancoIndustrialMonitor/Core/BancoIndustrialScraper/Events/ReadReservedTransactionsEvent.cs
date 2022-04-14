using System.Collections.Generic;
using BancoIndustrialMonitor.Application.BIScraper.Models;

namespace BancoIndustrialMonitor.Application.BIScraper.Events;

public record ReadReservedTransactionsEvent(
  IList<ReservedTransaction> ReservedTransactions);
