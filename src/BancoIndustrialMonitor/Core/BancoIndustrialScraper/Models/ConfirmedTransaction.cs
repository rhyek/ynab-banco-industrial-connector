namespace BancoIndustrialMonitor.Application.BIScraper.Models;

public record ConfirmedTransaction (
  DateOnly Date,
  string Description,
  string Reference,
  decimal Amount
);
