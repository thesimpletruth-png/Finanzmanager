namespace FinanceManager.Core.Models;

public enum TransactionType
{
    Expense = 0,
    Income = 1,
    Transfer = 2
}

public enum AccountType
{
    Checking = 0,    // Girokonto
    Savings = 1,     // Tagesgeld / Sparkonto
    CreditCard = 2,  // Kreditkarte
    Cash = 3         // Bargeld
}

public enum SplitMethod
{
    FiftyFifty = 0,           // 50:50 gleichmäßig
    IncomeProportional = 1,   // Proportional zum Nettoeinkommen der beteiligten Personen
    CustomFixed = 2           // Fester Betrag / Prozentsatz
}

