namespace FinanceManager.Core.Models;

public class Transaction
{
    public int Id { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public decimal Amount { get; set; } // Negativ für Ausgaben, Positiv für Einnahmen
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public string PayeeOrPayer { get; set; } = string.Empty; // Empfänger / Auftraggeber
    public string Purpose { get; set; } = string.Empty; // Verwendungszweck
    public string? Note { get; set; }

    // Eindeutiger Hash zur Dubletten-Erkennung bei wiederholtem Import
    public string? ImportFingerprint { get; set; }
    public int? ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }

    // Account
    public int AccountId { get; set; }
    public Account? Account { get; set; }

    // Kategorie
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    // Wer hat es bezahlt (Person)
    public int? PaidByPersonId { get; set; }
    public Person? PaidByPerson { get; set; }

    // Haushaltsausgabe (Ja = Gemeinschaft, Nein = rein persönliche Ausgabe)
    public bool IsHouseholdExpense { get; set; } = false;

    // Splits (Aufteilung der Kosten auf Haushaltsmitglieder)
    public List<TransactionSplit> Splits { get; set; } = new();
}

public class TransactionSplit
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public int PersonId { get; set; }
    public Person? Person { get; set; }

    public decimal Amount { get; set; } // Betrag, den diese Person trägt
    public decimal Percentage { get; set; } // Prozentualer Anteil (z. B. 50.0 oder 60.0)
    public string? Note { get; set; }
}

