using FinanceManager.Core.Models;

namespace FinanceManager.Core.Interfaces;

public interface IBankStatementParser
{
    string ProfileName { get; }
    bool CanParse(string firstLines);
    Task<List<ParsedTransactionRow>> ParseAsync(Stream stream);
}

public class ParsedTransactionRow
{
    public DateTime BookingDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public decimal Amount { get; set; }
    public string PayeeOrPayer { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string RawRow { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
}

public interface IImportService
{
    Task<ImportPreviewResult> PrepareImportAsync(string filePath, int accountId, string? profileName = null);
    Task<ImportBatch> ExecuteImportAsync(ImportExecutionRequest request);
}

public class ImportPreviewResult
{
    public string DetectedProfileName { get; set; } = string.Empty;
    public int TotalRowsFound { get; set; }
    public List<ParsedTransactionPreview> Rows { get; set; } = new();
    public int NewRowsCount => Rows.Count(r => !r.IsDuplicate);
    public int DuplicateRowsCount => Rows.Count(r => r.IsDuplicate);
}

public class ParsedTransactionPreview
{
    public DateTime BookingDate { get; set; }
    public decimal Amount { get; set; }
    public string PayeeOrPayer { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public int? SuggestedCategoryId { get; set; }
    public string? SuggestedCategoryName { get; set; }
    public bool IsHouseholdExpense { get; set; } = false;
    public bool IncludeInImport { get; set; } = true;
}

public class ImportExecutionRequest
{
    public int AccountId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public int? PaidByPersonId { get; set; }
    public List<ParsedTransactionPreview> SelectedRows { get; set; } = new();
}

public interface ISettlementService
{
    Task<HouseholdSettlementResult> CalculateSettlementAsync(DateTime from, DateTime to, SplitMethod method);
}

public interface ICategoryService
{
    Task<Category?> DetectCategoryAsync(string payee, string purpose);
    Task LearnRuleAsync(string keyword, int categoryId);
}

