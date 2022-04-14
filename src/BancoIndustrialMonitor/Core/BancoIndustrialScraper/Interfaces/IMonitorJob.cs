using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace BancoIndustrialMonitor.Application.BIScraper.Interfaces;

public interface IMonitorJob
{
  Task Run(IPage page, IElementHandle accountCell,
    CancellationToken cancellationToken);
}
