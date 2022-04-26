namespace YnabBancoIndustrialConnector.Interfaces;

public interface ICurrencyConverterService
{
  Task<decimal> ToUsd(string fromCurrencyCode, decimal fromAmount);
}
