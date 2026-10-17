using FinanceManager.Core.Interfaces;
using FinanceManager.Core.Models;
using FinanceManager.Services.Import.Parsers;

namespace FinanceManager.Services.Import;

public class ImportService : IImportService
{
    private readonly IFinanceRepository _repository;
    private readonly ICategoryService _categoryService;
    private readonly List<IBankStatementParser> _parsers;

    public ImportService(IFinanceRepository repository, ICategoryService categoryService)
    {
        _repository = repository;
        _categoryService = categoryService;
        _parsers = new List<IBankStatementParser>
        {
            new SparkasseCsvParser(),
            new IngCsvParser(),
            new GenericCsvBankParser()
        };
    }

    public async Task<ImportPreviewResult> PrepareImportAsync(string filePath, int accountId, string? profileName = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Die Datei {filePath} wurde nicht gefunden.");

        // Read first few lines for profile detection
        string firstLines;
        using (var stream = File.OpenRead(filePath))
        using (var reader = new StreamReader(stream))
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                var l = await reader.ReadLineAsync();
                if (l != null) sb.AppendLine(l);
            }
            firstLines = sb.ToString();
        }

        IBankStatementParser? selectedParser = null;
        if (!string.IsNullOrEmpty(profileName))
        {
            selectedParser = _parsers.FirstOrDefault(p => p.ProfileName.Equals(profileName, StringComparison.OrdinalIgnoreCase));
        }

        selectedParser ??= _parsers.FirstOrDefault(p => p.CanParse(firstLines)) ?? _parsers.Last();

        List<ParsedTransactionRow> parsedRows;
        using (var stream = File.OpenRead(filePath))
        {
            parsedRows = await selectedParser.ParseAsync(stream);
        }

        // Check for duplicates via repository
        var fingerprints = parsedRows.Select(r => r.Fingerprint).ToList();
        var existingFingerprints = await _repository.GetExistingFingerprintsAsync(fingerprints);

        var previewRows = new List<ParsedTransactionPreview>();

        foreach (var row in parsedRows)
        {
            bool isDuplicate = existingFingerprints.Contains(row.Fingerprint);

            // Auto-detect category
            var category = await _categoryService.DetectCategoryAsync(row.PayeeOrPayer, row.Purpose);

            previewRows.Add(new ParsedTransactionPreview
            {
                BookingDate = row.BookingDate,
                Amount = row.Amount,
                PayeeOrPayer = row.PayeeOrPayer,
                Purpose = row.Purpose,
                Fingerprint = row.Fingerprint,
                IsDuplicate = isDuplicate,
                SuggestedCategoryId = category?.Id,
                SuggestedCategoryName = category != null ? $"{category.Icon} {category.Name}" : null,
                IsHouseholdExpense = true, // Standardmäßig als Haushaltsausgabe vorschlagen
                IncludeInImport = !isDuplicate // Dubletten standardmäßig abwählen
            });
        }

        return new ImportPreviewResult
        {
            DetectedProfileName = selectedParser.ProfileName,
            TotalRowsFound = parsedRows.Count,
            Rows = previewRows
        };
    }

    public async Task<ImportBatch> ExecuteImportAsync(ImportExecutionRequest request)
    {
        var account = await _repository.GetAccountByIdAsync(request.AccountId)
                      ?? throw new InvalidOperationException($"Konto mit ID {request.AccountId} nicht gefunden.");

        var selected = request.SelectedRows.Where(r => r.IncludeInImport).ToList();

        var batch = new ImportBatch
        {
            AccountId = request.AccountId,
            FileName = Path.GetFileName(request.FileName),
            BankProfileName = request.ProfileName,
            TotalRows = request.SelectedRows.Count,
            ImportedCount = selected.Count,
            DuplicateCount = request.SelectedRows.Count - selected.Count,
            ImportedAt = DateTime.Now
        };

        var createdBatch = await _repository.AddImportBatchAsync(batch);

        var transactions = new List<Transaction>();

        foreach (var row in selected)
        {
            var tx = new Transaction
            {
                BookingDate = row.BookingDate,
                Amount = row.Amount,
                Type = row.Amount >= 0 ? TransactionType.Income : TransactionType.Expense,
                PayeeOrPayer = row.PayeeOrPayer,
                Purpose = row.Purpose,
                ImportFingerprint = row.Fingerprint,
                ImportBatchId = createdBatch.Id,
                AccountId = request.AccountId,
                CategoryId = row.SuggestedCategoryId,
                PaidByPersonId = request.PaidByPersonId ?? account.OwnerPersonId,
                IsHouseholdExpense = row.IsHouseholdExpense
            };

            transactions.Add(tx);
        }

        await _repository.AddTransactionsBatchAsync(transactions);

        return createdBatch;
    }
}

