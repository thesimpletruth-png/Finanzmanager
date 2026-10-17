namespace FinanceManager.Core.Models;

public class ImportBatch
{
    public int Id { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public string FileName { get; set; } = string.Empty;
    public string BankProfileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public List<Transaction> Transactions { get; set; } = new();
}

public class ImportProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // z. B. "Sparkasse Standard", "ING DiBa", "DKB", "Generic"
    public string Delimiter { get; set; } = ";";
    public string DecimalSeparator { get; set; } = ",";
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    public bool HasHeader { get; set; } = true;

    // Spaltenindizes oder Spaltennamen (0-basiert)
    public int DateColumnIndex { get; set; } = 0;
    public int? ValueDateColumnIndex { get; set; }
    public int AmountColumnIndex { get; set; } = 1;
    public int PayeeColumnIndex { get; set; } = 2;
    public int PurposeColumnIndex { get; set; } = 3;

    // Erkennungsmuster im Header (z. B. "Auftragskonto;Buchungstag;Valutadatum")
    public string? HeaderFingerprint { get; set; }
}

