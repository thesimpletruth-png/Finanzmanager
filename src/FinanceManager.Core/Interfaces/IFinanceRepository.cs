using FinanceManager.Core.Models;

namespace FinanceManager.Core.Interfaces;

public interface IFinanceRepository
{
    // Persons
    Task<List<Person>> GetPersonsAsync();
    Task<Person?> GetPersonByIdAsync(int id);
    Task<Person> AddPersonAsync(Person person);
    Task UpdatePersonAsync(Person person);
    Task DeletePersonAsync(int id);

    // Accounts
    Task<List<Account>> GetAccountsAsync();
    Task<Account?> GetAccountByIdAsync(int id);
    Task<Account> AddAccountAsync(Account account);
    Task UpdateAccountAsync(Account account);
    Task DeleteAccountAsync(int id);

    // Categories
    Task<List<Category>> GetCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<Category> AddCategoryAsync(Category category);
    Task<List<CategoryRule>> GetCategoryRulesAsync();
    Task AddCategoryRuleAsync(CategoryRule rule);

    // Transactions
    Task<List<Transaction>> GetTransactionsAsync(DateTime? from = null, DateTime? to = null, int? accountId = null, int? personId = null);
    Task<Transaction?> GetTransactionByIdAsync(int id);
    Task<Transaction> AddTransactionAsync(Transaction transaction);
    Task AddTransactionsBatchAsync(IEnumerable<Transaction> transactions);
    Task UpdateTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(int id);
    Task<bool> TransactionFingerprintExistsAsync(string fingerprint);
    Task<HashSet<string>> GetExistingFingerprintsAsync(IEnumerable<string> fingerprints);

    // Import Batches
    Task<List<ImportBatch>> GetImportBatchesAsync();
    Task<ImportBatch> AddImportBatchAsync(ImportBatch batch);

    // Database Initialization & Seed
    Task EnsureDatabaseCreatedAsync();
}

