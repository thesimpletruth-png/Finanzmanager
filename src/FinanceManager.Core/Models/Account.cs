namespace FinanceManager.Core.Models;

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public AccountType Type { get; set; } = AccountType.Checking;
    public decimal InitialBalance { get; set; } = 0m;
    public bool IsJointAccount { get; set; } = false; // Gemeinschaftskonto

    // Gehört zu einer bestimmten Person (wenn nicht rein gemeinschaftlich)
    public int? OwnerPersonId { get; set; }
    public Person? OwnerPerson { get; set; }

    public List<Transaction> Transactions { get; set; } = new();
}

