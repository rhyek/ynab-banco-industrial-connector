using System.Text.RegularExpressions;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.Models;

namespace YnabBancoIndustrialConnector.Infrastructure.YnabController.
    Repositories;

public enum AccountType
{
    Credit,
    Debit
}

public class YnabTransactionRepository
{
    private static readonly Dictionary<string, List<Regex>>
        AlternateDescriptionsForMatching = new()
        {
            { "AMZN Mktp US", new() { new("^AMZN Mktp US\\*.+ US$", RegexOptions.IgnoreCase) } }
        };

    private readonly FlurlClient _httpClient;
    private readonly ILogger<YnabTransactionRepository> _logger;
    private readonly YnabControllerOptions _options;
    private readonly IList<object> _pendingCreations = new List<object>();

    private readonly Dictionary<string, Dictionary<string, dynamic>>
        _pendingUpdates = new();

    public YnabTransactionRepository(
        IOptions<YnabControllerOptions> options,
        ILogger<YnabTransactionRepository> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = new FlurlClient("https://api.youneedabudget.com/v1")
            .WithOAuthBearerToken(_options.PersonalAccessToken)
            .AfterCall(call =>
            {
                var requestsUsed =
                    call.Response.Headers.FirstOrDefault("X-Rate-Limit");
                if (requestsUsed != null)
                {
                    _logger.LogInformation(
                        "YNAB rate limit used requests for this hour: {Value}",
                        requestsUsed);
                }
            });
    }

    public string GetAccountIdForType(AccountType type)
    {
        return type switch
        {
            AccountType.Debit => _options.DebitCardAccountId,
            _ => _options.CreditCardAccountId
        };
    }

    public async Task<IList<YnabTransaction>> GetRecent(
        AccountType accountType)
    {
        var accountId = GetAccountIdForType(accountType);
        var searchPeriod = DateTime.Now.AddDays(-75);
        var sinceDate = DateOnly
            .FromDateTime(searchPeriod)
            .ToString("o");
        var json = await _httpClient
            .Request(
                $"budgets/{_options.BudgetId}/accounts/{accountId}/transactions")
            .SetQueryParams(new
            {
                since_date = sinceDate
            })
            .GetAsync()
            .ReceiveJson<TransactionsResponse>();
        var transactions = json.Data.Transactions
            .Select(t => t with { Amount = t.Amount / 1_000 })
            .ToList();
        return transactions;
    }

    public async Task<YnabTransaction?> FindByReference(string reference,
        AccountType accountType,
        IList<YnabTransaction>? source = null)
    {
        return (source ?? await GetRecent(accountType))
            .FirstOrDefault(t => t.Metadata.Reference == reference);
    }

    private async Task<int> CommitPendingCreations()
    {
        if (_pendingCreations.Count > 0)
        {
            var transactions = new List<object>(_pendingCreations);
            _pendingCreations.Clear();
            _logger.LogInformation("Commiting creations: {Data}",
                JsonConvert.SerializeObject(transactions,
                    Formatting.Indented));
            await _httpClient
                .Request($"budgets/{_options.BudgetId}/transactions")
                .PostJsonAsync(new
                {
                    transactions
                });
            return transactions.Count;
        }

        return 0;
    }

    private void AddPendingUpdate(string ynabTxId, string key, dynamic? value)
    {
        if (!_pendingUpdates.ContainsKey(ynabTxId))
        {
            _pendingUpdates.Add(ynabTxId,
                new() { { "id", ynabTxId } });
        }

        _pendingUpdates[ynabTxId].Add(key: key, value: value);
    }

    private void AddPendingUpdate(string ynabTxId, object dict)
    {
        foreach (var property in dict.GetType().GetProperties())
            AddPendingUpdate(ynabTxId, property.Name, property.GetValue(dict));
    }

    private async Task<int> CommitPendingUpdates()
    {
        if (_pendingUpdates.Count > 0)
        {
            var transactions = _pendingUpdates.Select(kv => kv.Value).ToList();
            _pendingUpdates.Clear();
            _logger.LogInformation("Commiting updates: {Data}",
                JsonConvert.SerializeObject(transactions,
                    Formatting.Indented));
            await _httpClient
                .Request($"budgets/{_options.BudgetId}/transactions")
                .PatchJsonAsync(new
                {
                    transactions
                });
            return transactions.Count;
        }

        return 0;
    }

    public async Task CommitChanges()
    {
        var count =
            await Task.WhenAll(CommitPendingCreations(),
                CommitPendingUpdates());
        _logger.LogInformation("Commited {Count} change(s)", count.Sum());
    }

    public static (string? payeeId, string? categoryId)
        GetPayeeAndCategoryForDescription(
            string? description,
            IEnumerable<YnabTransaction> recentTransactions)
    {
        string? payeeId = null;

        string? categoryId = null;
        // if no description, or if i transferred to myself,
        // or if we transferred with mobile app and don't know to what account,
        // don't assign payee or category
        if (description == null || description.ToLower()
                .Contains("BELWEB TRANSF. PROPIA A 0180135097".ToLower()) ||
            description.ToLower() == "BANCA MOVIL".ToLower())
        {
            return (payeeId, categoryId);
        }
        var descriptionRegexes = new List<Regex>
        {
            new(description),
            new($"{description} GT") // several establishments notify with a name, but in the estado de cuenta it adds a " GT"
        };
        if (AlternateDescriptionsForMatching.TryGetValue(description, out var alternates))
        {
            descriptionRegexes.AddRange(alternates);
        }
        var othersWithSameDescription = recentTransactions
            .Where(t =>
                descriptionRegexes.Any(descriptionRegex =>
                    t.Metadata.Description is not null && descriptionRegex.IsMatch(t.Metadata.Description)))
            .OrderByDescending(t => t.Date)
            .ToList();
        if (othersWithSameDescription.Count > 0)
        {
            var lastForPayeeId = othersWithSameDescription
                .FirstOrDefault(t => t.PayeeId != null);
            if (lastForPayeeId != null)
            {
                payeeId = lastForPayeeId.PayeeId;
            }

            var lastForCategoryId = othersWithSameDescription
                .FirstOrDefault(t => t.CategoryId != null);
            if (lastForCategoryId != null)
            {
                categoryId = lastForCategoryId.CategoryId;
            }
        }
        return (payeeId, categoryId);
    }

    public async Task<bool> CreateTransaction(string reference,
        AccountType accountType,
        decimal amount,
        DateOnly date, string cleared,
        string? description = null,
        IList<YnabTransaction>? recentTransactions = null)
    {
        var ynabAmount = decimal.ToInt32(amount * 1_000);
        var metadata = new YnabTransactionMetadata(reference, true,
            description);
        recentTransactions ??= await GetRecent(accountType);
        var (payeeId, categoryId) =
            GetPayeeAndCategoryForDescription(description, recentTransactions);

        // see if there is a scheduled ynab transaction we should be replacing
        if (payeeId != null && categoryId != null)
        {
            var txGeneratedFromSchedule = recentTransactions.FirstOrDefault(
                ynabTx =>
                    ynabTx.PayeeId == payeeId &&
                    ynabTx.CategoryId == categoryId && ynabTx.Memo == "" &&
                    !ynabTx.Approved);
            if (txGeneratedFromSchedule != null)
            {
                AddPendingUpdate(txGeneratedFromSchedule.Id, new
                {
                    date = date.ToString("o"),
                    amount = ynabAmount,
                    memo = metadata.SerializeMemo()
                });
                return true;
            }
        }
        var existing = await FindByReference(reference, accountType,
            recentTransactions);
        if (existing != null)
        {
            if (existing.Metadata != metadata || existing.Amount != amount ||
                (payeeId != null && existing.PayeeId != payeeId) ||
                (categoryId != null && existing.CategoryId != categoryId))
            {
                AddPendingUpdate(existing.Id, new
                {
                    amount = ynabAmount,
                    memo = metadata.SerializeMemo(),
                    payee_id = payeeId ?? existing.PayeeId,
                    category_id = categoryId ?? existing.CategoryId
                });
                return true;
            }
            return false;
        }

        _pendingCreations.Add(new
        {
            account_id = GetAccountIdForType(accountType),
            date = date.ToString("o"),
            amount = ynabAmount,
            cleared,
            memo = metadata.SerializeMemo(),
            payee_id = payeeId,
            category_id = categoryId
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
