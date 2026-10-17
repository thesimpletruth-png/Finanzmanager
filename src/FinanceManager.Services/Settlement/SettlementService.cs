using FinanceManager.Core.Interfaces;
using FinanceManager.Core.Models;

namespace FinanceManager.Services.Settlement;

public class SettlementService : ISettlementService
{
    private readonly IFinanceRepository _repository;

    public SettlementService(IFinanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<HouseholdSettlementResult> CalculateSettlementAsync(DateTime from, DateTime to, SplitMethod method)
    {
        var persons = await _repository.GetPersonsAsync();
        var transactions = await _repository.GetTransactionsAsync(from, to);

        // Filter: Nur Ausgaben, die als Haushaltsausgabe markiert sind
        var householdExpenses = transactions.Where(t => t.IsHouseholdExpense && t.Amount < 0).ToList();
        var totalAmount = Math.Abs(householdExpenses.Sum(t => t.Amount));

        var result = new HouseholdSettlementResult
        {
            PeriodStart = from,
            PeriodEnd = to,
            TotalHouseholdExpenses = totalAmount
        };

        if (persons.Count == 0 || totalAmount == 0)
        {
            return result;
        }

        // 1. Calculate actual paid per person
        var paidMap = new Dictionary<int, decimal>();
        foreach (var p in persons) paidMap[p.Id] = 0m;

        foreach (var t in householdExpenses)
        {
            if (t.PaidByPersonId.HasValue && paidMap.ContainsKey(t.PaidByPersonId.Value))
            {
                paidMap[t.PaidByPersonId.Value] += Math.Abs(t.Amount);
            }
        }

        // 2. Calculate target share percentage per person
        var targetPercentages = new Dictionary<int, decimal>();

        if (method == SplitMethod.IncomeProportional)
        {
            var totalIncome = persons.Sum(p => p.MonthlyNetIncome);
            if (totalIncome > 0)
            {
                foreach (var p in persons)
                {
                    targetPercentages[p.Id] = (p.MonthlyNetIncome / totalIncome) * 100m;
                }
            }
            else
            {
                // Fallback 50:50 if income is 0
                decimal equalShare = 100m / persons.Count;
                foreach (var p in persons) targetPercentages[p.Id] = equalShare;
            }
        }
        else // FiftyFifty / Gleichmäßig
        {
            decimal equalShare = 100m / persons.Count;
            foreach (var p in persons) targetPercentages[p.Id] = equalShare;
        }

        // 3. Build contribution list
        foreach (var p in persons)
        {
            decimal actualPaid = paidMap[p.Id];
            decimal pct = targetPercentages[p.Id];
            decimal targetShare = Math.Round(totalAmount * (pct / 100m), 2);

            result.Contributions.Add(new PersonContribution
            {
                PersonId = p.Id,
                PersonName = p.Name,
                ActualPaidAmount = actualPaid,
                TargetPercentage = pct,
                TargetShareAmount = targetShare
            });
        }

        // 4. Calculate recommended settlement transfers (who pays whom)
        // Debtors (Balance < 0) pay Creditors (Balance > 0)
        var debtors = result.Contributions.Where(c => c.Balance < 0).Select(c => new { c.PersonName, Amount = Math.Abs(c.Balance) }).ToList();
        var creditors = result.Contributions.Where(c => c.Balance > 0).Select(c => new { c.PersonName, Amount = c.Balance }).ToList();

        int dIdx = 0;
        int cIdx = 0;

        var debtorsRemaining = debtors.ToDictionary(d => d.PersonName, d => d.Amount);
        var creditorsRemaining = creditors.ToDictionary(c => c.PersonName, c => c.Amount);

        while (dIdx < debtors.Count && cIdx < creditors.Count)
        {
            var debtorName = debtors[dIdx].PersonName;
            var creditorName = creditors[cIdx].PersonName;

            var debt = debtorsRemaining[debtorName];
            var credit = creditorsRemaining[creditorName];

            var transferAmount = Math.Min(debt, credit);

            if (transferAmount > 0.01m)
            {
                result.RecommendedTransfers.Add(new SettlementTransfer
                {
                    FromPerson = debtorName,
                    ToPerson = creditorName,
                    Amount = Math.Round(transferAmount, 2),
                    Reason = $"Haushaltsausgleich {from:MM/yyyy} ({method})"
                });

                debtorsRemaining[debtorName] -= transferAmount;
                creditorsRemaining[creditorName] -= transferAmount;
            }

            if (debtorsRemaining[debtorName] <= 0.01m) dIdx++;
            if (creditorsRemaining[creditorName] <= 0.01m) cIdx++;
        }

        return result;
    }
}

