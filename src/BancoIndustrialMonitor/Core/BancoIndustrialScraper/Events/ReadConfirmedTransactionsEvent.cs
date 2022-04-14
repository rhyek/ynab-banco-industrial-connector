using System.Collections.Generic;
using BancoIndustrialMonitor.Application.BIScraper.Models;

namespace BancoIndustrialMonitor.Application.BIScraper.Events;

public record ReadConfirmedTransactionsEvent(
  IList<ConfirmedTransaction> ConfirmedTransactions);
