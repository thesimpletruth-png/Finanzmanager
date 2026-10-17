namespace FinanceManager.Core.Models;

public class HouseholdSettlementResult
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalHouseholdExpenses { get; set; }

    public List<PersonContribution> Contributions { get; set; } = new();
    public List<SettlementTransfer> RecommendedTransfers { get; set; } = new();
}

public class PersonContribution
{
    public int PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public decimal ActualPaidAmount { get; set; } // Tatsächlich verauslagt
    public decimal TargetShareAmount { get; set; } // Soll-Anteil nach Split-Regel (50:50 oder Einkommen)
    public decimal Balance => ActualPaidAmount - TargetShareAmount; // Positiv = bekommt Geld, Negativ = schuldet Geld
    public decimal TargetPercentage { get; set; }
}

public class SettlementTransfer
{
    public string FromPerson { get; set; } = string.Empty;
    public string ToPerson { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

