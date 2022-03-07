using Microsoft.Playwright;

namespace BancoIndustrialMonitor.Application.BIScraper.Interfaces;

public interface IMonitorJob
{
  Task Run(IPage page, IElementHandle accountCell,
    CancellationToken cancellationToken);
}
