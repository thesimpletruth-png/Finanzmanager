namespace FinanceManager.Core.Models;

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#3B82F6"; // Default Blue
    public decimal MonthlyNetIncome { get; set; } = 0m; // Für proportionale Kostenteilung
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public List<Account> Accounts { get; set; } = new();
    public List<Transaction> PaidTransactions { get; set; } = new();
    public List<TransactionSplit> Splits { get; set; } = new();
}

