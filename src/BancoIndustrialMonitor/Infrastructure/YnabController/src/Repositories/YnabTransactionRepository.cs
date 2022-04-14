using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Models;

namespace YnabBancoIndustrialConnector.Infrastructure.YnabController.
  Repositories;

public class YnabTransactionRepository
{
  private readonly YnabControllerOptions _options;

  private readonly ILogger<YnabTransactionRepository> _logger;

  private readonly FlurlClient _httpClient;

  public YnabTransactionRepository(
    IOptions<YnabControllerOptions> options,
    ILogger<YnabTransactionRepository> logger)
  {
    _options = options.Value;
    _logger = logger;
    _httpClient = new FlurlClient("https://api.youneedabudget.com/v1")
      .WithOAuthBearerToken(_options.PersonalAccessToken)
      .AfterCall(call => {
        var requestsUsed =
          call.Response.Headers.FirstOrDefault("X-Rate-Limit");
        if (requestsUsed != null) {
          _logger.LogInformation(
            "YNAB rate limit used requests for this hour: {Value}",
            requestsUsed);
        }
      });
  }

  public async Task<IList<YnabTransaction>> GetRecent()
  {
    var lastMonth = DateTime.Now.AddMonths(-2);
    var sinceDate = DateOnly
      .FromDateTime(new(lastMonth.Year, lastMonth.Month, 1))
      .ToString("o");
    var json = await _httpClient
      .Request(
        $"budgets/{_options.BudgetId}/accounts/{_options.AccountId}/transactions")
      .SetQueryParams(new {
        since_date = sinceDate
      })
      .GetAsync()
      .ReceiveJson<TransactionsResponse>();
    var transactions = json.Data.Transactions
      .Select(t => t with {Amount = t.Amount / 1_000})
      .ToList();
    return transactions;
  }

  public async Task<YnabTransaction?> FindByReference(string reference,
    IList<YnabTransaction>? source = null)
  {
    return (source ?? await GetRecent())
      .FirstOrDefault(t => t.Metadata.Reference == reference);
  }

  private readonly IList<object> _pendingCreations = new List<object>();

  private async Task<int> CommitPendingCreations()
  {
    if (_pendingCreations.Count > 0) {
      var transactions = new List<object>(_pendingCreations);
      _pendingCreations.Clear();
      _logger.LogInformation("Commiting creations: {Data}",
        JsonConvert.SerializeObject(transactions,
          Formatting.Indented));
      await _httpClient
        .Request($"budgets/{_options.BudgetId}/transactions")
        .PostJsonAsync(new {
          transactions
        });
      return transactions.Count;
    }

    return 0;
  }

  private readonly Dictionary<string, Dictionary<string, dynamic>>
    _pendingUpdates = new();

  private void AddPendingUpdate(string ynabTxId, string key, dynamic? value)
  {
    if (!_pendingUpdates.ContainsKey(ynabTxId)) {
      _pendingUpdates.Add(ynabTxId, new() {{"id", ynabTxId}});
    }

    _pendingUpdates[ynabTxId].Add(key: key, value: value);
  }

  private void AddPendingUpdate(string ynabTxId, object dict)
  {
    foreach (var property in dict.GetType().GetProperties()) {
      AddPendingUpdate(ynabTxId, property.Name, property.GetValue(dict));
    }
  }

  private async Task<int> CommitPendingUpdates()
  {
    if (_pendingUpdates.Count > 0) {
      var transactions = _pendingUpdates.Select(kv => kv.Value).ToList();
      _pendingUpdates.Clear();
      _logger.LogInformation("Commiting updates: {Data}",
        JsonConvert.SerializeObject(transactions,
          Formatting.Indented));
      await _httpClient
        .Request($"budgets/{_options.BudgetId}/transactions")
        .PatchJsonAsync(new {
          transactions
        });
      return transactions.Count;
    }

    return 0;
  }

  public async Task CommitChanges()
  {
    await Task.WhenAll(CommitPendingCreations(), CommitPendingUpdates());
  }

  public static (string? payeeId, string? categoryId)
    GetPayeeAndCategoryForDescription(
      string? description,
      IEnumerable<YnabTransaction> recentTransactions)
  {
    string? payeeId = null;
    string? categoryId = null;
    if (description != null) {
      var othersWithSameDescription = recentTransactions
        .Where(t => t.Metadata.Description == description)
        .ToList();
      if (othersWithSameDescription.Count > 0) {
        var payeeIdGroups =
          othersWithSameDescription
            .Where(t => t.PayeeId != null)
            .GroupBy(t => t.PayeeId)
            .ToList();
        // only use if every tx has the same payee id
        if (payeeIdGroups.Count == 1) {
          payeeId = payeeIdGroups.First().Key;
        }

        var categoryIdGroups =
          othersWithSameDescription
            .Where(t => t.CategoryId != null)
            .GroupBy(t => t.CategoryId)
            .ToList();
        // only use if every tx has the same category id
        if (categoryIdGroups.Count == 1) {
          categoryId = categoryIdGroups.First().Key;
        }
      }
    }
    return (payeeId, categoryId);
  }

  public async Task<bool> CreateTransaction(string reference, decimal amount,
    DateOnly date, string cleared,
    string? description = null,
    IList<YnabTransaction>? recentTransactions = null)
  {
    recentTransactions ??= await GetRecent();
    var existing = await FindByReference(reference, source: recentTransactions);
    if (existing != null) {
      return false;
    }
    var ynabAmount = decimal.ToInt32(amount * 1_000);
    var metadata = new YnabTransactionMetadata(reference, true,
      description);
    var (payeeId, categoryId) =
      GetPayeeAndCategoryForDescription(description, recentTransactions);

    // see if there is a scheduled ynab transaction we should be replacing
    if (payeeId != null && categoryId != null) {
      var txGeneratedFromSchedule = recentTransactions.FirstOrDefault(ynabTx =>
        ynabTx.PayeeId == payeeId &&
        ynabTx.CategoryId == categoryId && ynabTx.Memo == "" &&
        !ynabTx.Approved);
      if (txGeneratedFromSchedule != null) {
        AddPendingUpdate(txGeneratedFromSchedule.Id, new {
          date = date.ToString("o"),
          amount = ynabAmount,
          memo = metadata.SerializeMemo(),
        });
        return true;
      }
    }

    _pendingCreations.Add(new {
      account_id = _options.AccountId,
      date = date.ToString("o"),
      amount = ynabAmount,
      cleared,
      memo = metadata.SerializeMemo(),
      payee_id = payeeId,
      category_id = categoryId,
    });
    return true;
  }

  public void UpdateTransactionAmount(string ynabTxId, decimal amount)
  {
    var ynabAmount = decimal.ToInt32(amount * 1_000);
    AddPendingUpdate(ynabTxId, "amount", ynabAmount);
  }

  public void UpdateTransactionSetToCleared(string ynabTxId)
  {
    AddPendingUpdate(ynabTxId, "cleared",
      YnabTransactionCleared.Cleared);
  }

  public void UpdateTransactionMetadata(string ynabTxId,
    YnabTransactionMetadata metadata)
  {
    AddPendingUpdate(ynabTxId, "memo", metadata.SerializeMemo());
  }

  public void UpdateTransactionPayee(string ynabTxId, string? payeeId)
  {
    AddPendingUpdate(ynabTxId, "payee_id", payeeId);
  }

  public void UpdateTransactionCategory(string ynabTxId, string? categoryId)
  {
    AddPendingUpdate(ynabTxId, "category_id", categoryId);
  }

  public void UpdateTransactionDate(string ynabTxId, DateOnly date)
  {
    AddPendingUpdate(ynabTxId, "date", date.ToString("o"));
  }
}
