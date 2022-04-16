using Microsoft.Playwright;

namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper.Interfaces;

public interface IScraperJob<T>
{
  Task<T> Run(IPage page, IElementHandle accountCell,
    CancellationToken cancellationToken);
}
