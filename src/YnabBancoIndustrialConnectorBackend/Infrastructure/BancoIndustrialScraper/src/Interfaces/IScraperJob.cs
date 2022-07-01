using Microsoft.Playwright;

namespace YnabBancoIndustrialConnector.Domain.Interfaces;

public interface IScraperJob<T>
{
  Task<T> Run(IPage page, IElementHandle accountCell,
    CancellationToken cancellationToken);
}
