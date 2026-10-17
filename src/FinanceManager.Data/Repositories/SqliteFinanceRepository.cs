using Microsoft.EntityFrameworkCore;
using FinanceManager.Core.Interfaces;
using FinanceManager.Core.Models;

namespace FinanceManager.Data.Repositories;

public class SqliteFinanceRepository : IFinanceRepository
{
    private readonly FinanceDbContext _context;

    public SqliteFinanceRepository(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        await _context.Database.EnsureCreatedAsync();
        await SeedInitialDataAsync();
    }

    private async Task SeedInitialDataAsync()
    {
        // 1. Initial Persons if empty
        if (!await _context.Persons.AnyAsync())
        {
            var person1 = new Person { Name = "Person 1", ColorHex = "#2563EB", MonthlyNetIncome = 3000m };
            var person2 = new Person { Name = "Person 2", ColorHex = "#DB2777", MonthlyNetIncome = 2500m };
            _context.Persons.AddRange(person1, person2);
            await _context.SaveChangesAsync();

            // 2. Initial Accounts
            var jointAccount = new Account
            {
                Name = "Gemeinschaftskonto",
                BankName = "Haushaltsbank",
                Type = AccountType.Checking,
                IsJointAccount = true,
                InitialBalance = 0m
            };

            var personalAccount1 = new Account
            {
                Name = "Girokonto Person 1",
                BankName = "Hausbank",
                Type = AccountType.Checking,
                IsJointAccount = false,
                OwnerPersonId = person1.Id,
                InitialBalance = 0m
            };

            var personalAccount2 = new Account
            {
                Name = "Girokonto Person 2",
                BankName = "Hausbank",
                Type = AccountType.Checking,
                IsJointAccount = false,
                OwnerPersonId = person2.Id,
                InitialBalance = 0m
            };

            _context.Accounts.AddRange(jointAccount, personalAccount1, personalAccount2);
        }

        // 3. Initial Categories if empty
        if (!await _context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new Category { Name = "Gehalt / Einkommen", Icon = "💰", ColorHex = "#10B981", IsIncome = true },
                new Category { Name = "Lebensmittel & Haushalt", Icon = "🛒", ColorHex = "#F59E0B", IsIncome = false },
                new Category { Name = "Miete & Wohnen", Icon = "🏠", ColorHex = "#3B82F6", IsIncome = false },
                new Category { Name = "Strom, Gas & Nebenkosten", Icon = "⚡", ColorHex = "#EF4444", IsIncome = false },
                new Category { Name = "Mobilität & Auto", Icon = "🚗", ColorHex = "#8B5CF6", IsIncome = false },
                new Category { Name = "Freizeit, Kultur & Ausgehen", Icon = "🍕", ColorHex = "#EC4899", IsIncome = false },
                new Category { Name = "Abonnements & Streaming", Icon = "📺", ColorHex = "#6366F1", IsIncome = false },
                new Category { Name = "Versicherungen & Finanzen", Icon = "🛡️", ColorHex = "#14B8A6", IsIncome = false },
                new Category { Name = "Sonstiges", Icon = "📦", ColorHex = "#6B7280", IsIncome = false }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();

            // 4. Default Rules for Auto-Categorization
            var lebensmittel = categories.First(c => c.Name.StartsWith("Lebensmittel"));
            var streaming = categories.First(c => c.Name.StartsWith("Abonnements"));
            var mobilitaet = categories.First(c => c.Name.StartsWith("Mobilität"));
            var wohnen = categories.First(c => c.Name.StartsWith("Miete"));

            _context.CategoryRules.AddRange(
                new CategoryRule { CategoryId = lebensmittel.Id, MatchKeyword = "REWE" },
                new CategoryRule { CategoryId = lebensmittel.Id, MatchKeyword = "ALDI" },
                new CategoryRule { CategoryId = lebensmittel.Id, MatchKeyword = "LIDL" },
                new CategoryRule { CategoryId = lebensmittel.Id, MatchKeyword = "EDEKA" },
                new CategoryRule { CategoryId = lebensmittel.Id, MatchKeyword = "DM DROGERIE" },
                new CategoryRule { CategoryId = streaming.Id, MatchKeyword = "NETFLIX" },
                new CategoryRule { CategoryId = streaming.Id, MatchKeyword = "SPOTIFY" },
                new CategoryRule { CategoryId = streaming.Id, MatchKeyword = "AMAZON PRIME" },
                new CategoryRule { CategoryId = mobilitaet.Id, MatchKeyword = "ARAL" },
                new CategoryRule { CategoryId = mobilitaet.Id, MatchKeyword = "SHELL" },
                new CategoryRule { CategoryId = mobilitaet.Id, MatchKeyword = "DEUTSCHE BAHN" },
                new CategoryRule { CategoryId = wohnen.Id, MatchKeyword = "MIETE" }
            );
            await _context.SaveChangesAsync();
        }
    }

    // Persons
    public async Task<List<Person>> GetPersonsAsync() =>
        await _context.Persons.OrderBy(p => p.Name).ToListAsync();

    public async Task<Person?> GetPersonByIdAsync(int id) =>
        await _context.Persons.Include(p => p.Accounts).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Person> AddPersonAsync(Person person)
    {
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }

    public async Task UpdatePersonAsync(Person person)
    {
        _context.Persons.Update(person);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePersonAsync(int id)
    {
        var person = await _context.Persons.FindAsync(id);
        if (person != null)
        {
            _context.Persons.Remove(person);
            await _context.SaveChangesAsync();
        }
    }

    // Accounts
    public async Task<List<Account>> GetAccountsAsync() =>
        await _context.Accounts.Include(a => a.OwnerPerson).OrderBy(a => a.Name).ToListAsync();

    public async Task<Account?> GetAccountByIdAsync(int id) =>
        await _context.Accounts.Include(a => a.OwnerPerson).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Account> AddAccountAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAccountAsync(Account account)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(int id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account != null)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
        }
    }

    // Categories
    public async Task<List<Category>> GetCategoriesAsync() =>
        await _context.Categories.Include(c => c.SubCategories).OrderBy(c => c.Name).ToListAsync();

    public async Task<Category?> GetCategoryByIdAsync(int id) =>
        await _context.Categories.FindAsync(id);

    public async Task<Category> AddCategoryAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<List<CategoryRule>> GetCategoryRulesAsync() =>
        await _context.CategoryRules.Include(r => r.Category).ToListAsync();

    public async Task AddCategoryRuleAsync(CategoryRule rule)
    {
        _context.CategoryRules.Add(rule);
        await _context.SaveChangesAsync();
    }

    // Transactions
    public async Task<List<Transaction>> GetTransactionsAsync(DateTime? from = null, DateTime? to = null, int? accountId = null, int? personId = null)
    {
        var query = _context.Transactions
                            .Include(t => t.Account)
                            .Include(t => t.Category)
                            .Include(t => t.PaidByPerson)
                            .Include(t => t.Splits).ThenInclude(s => s.Person)
                            .AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.BookingDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(t => t.BookingDate <= to.Value.Date);

        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId.Value);

        if (personId.HasValue)
            query = query.Where(t => t.PaidByPersonId == personId.Value || t.Splits.Any(s => s.PersonId == personId.Value));

        return await query.OrderByDescending(t => t.BookingDate).ThenByDescending(t => t.Id).ToListAsync();
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int id) =>
        await _context.Transactions
                      .Include(t => t.Account)
                      .Include(t => t.Category)
                      .Include(t => t.PaidByPerson)
                      .Include(t => t.Splits)
                      .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Transaction> AddTransactionAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task AddTransactionsBatchAsync(IEnumerable<Transaction> transactions)
    {
        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTransactionAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTransactionAsync(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> TransactionFingerprintExistsAsync(string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint)) return false;
        return await _context.Transactions.AnyAsync(t => t.ImportFingerprint == fingerprint);
    }

    public async Task<HashSet<string>> GetExistingFingerprintsAsync(IEnumerable<string> fingerprints)
    {
        var list = fingerprints.Where(f => !string.IsNullOrEmpty(f)).ToList();
        if (list.Count == 0) return new HashSet<string>();

        var existing = await _context.Transactions
                                     .Where(t => t.ImportFingerprint != null && list.Contains(t.ImportFingerprint))
                                     .Select(t => t.ImportFingerprint!)
                                     .ToListAsync();

        return new HashSet<string>(existing);
    }

    // Import Batches
    public async Task<List<ImportBatch>> GetImportBatchesAsync() =>
        await _context.ImportBatches.Include(b => b.Account).OrderByDescending(b => b.ImportedAt).ToListAsync();

    public async Task<ImportBatch> AddImportBatchAsync(ImportBatch batch)
    {
        _context.ImportBatches.Add(batch);
        await _context.SaveChangesAsync();
        return batch;
    }
}

