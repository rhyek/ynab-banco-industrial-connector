using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace YnabBancoIndustrialConnector.Infrastructure.BIScraper.Interfaces;

public interface IMonitorJob
{
  Task Run(IPage page, IElementHandle accountCell,
    CancellationToken cancellationToken);
}
