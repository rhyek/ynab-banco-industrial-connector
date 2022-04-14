using System;

namespace BancoIndustrialMonitor.Application.BIScraper.Models;

public record ReservedTransaction (
  string Reference,
  DateOnly Date,
  decimal Amount
);
