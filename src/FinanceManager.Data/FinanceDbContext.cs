using Microsoft.EntityFrameworkCore;
using FinanceManager.Core.Models;

namespace FinanceManager.Data;

public class FinanceDbContext : DbContext
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryRule> CategoryRules => Set<CategoryRule>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionSplit> TransactionSplits => Set<TransactionSplit>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportProfile> ImportProfiles => Set<ImportProfile>();

    public string DbPath { get; }

    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options)
    {
        DbPath = "FinanceManager.db";
    }

    public FinanceDbContext()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(folder, "FinanceManager");
        Directory.CreateDirectory(appFolder);
        DbPath = Path.Combine(appFolder, "FinanceManager.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Person
        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ColorHex).HasMaxLength(20);
            entity.Property(e => e.MonthlyNetIncome).HasPrecision(18, 2);
        });

        // Account
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Iban).HasMaxLength(50);
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.InitialBalance).HasPrecision(18, 2);

            entity.HasOne(e => e.OwnerPerson)
                  .WithMany(p => p.Accounts)
                  .HasForeignKey(e => e.OwnerPersonId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(20);
            entity.Property(e => e.ColorHex).HasMaxLength(20);

            entity.HasOne(e => e.ParentCategory)
                  .WithMany(c => c.SubCategories)
                  .HasForeignKey(e => e.ParentCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CategoryRule
        modelBuilder.Entity<CategoryRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MatchKeyword).IsRequired().HasMaxLength(150);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.AutoRules)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.PayeeOrPayer).HasMaxLength(200);
            entity.Property(e => e.Purpose).HasMaxLength(500);
            entity.Property(e => e.ImportFingerprint).HasMaxLength(100);

            entity.HasIndex(e => e.ImportFingerprint);
            entity.HasIndex(e => e.BookingDate);

            entity.HasOne(e => e.Account)
                  .WithMany(a => a.Transactions)
                  .HasForeignKey(e => e.AccountId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Transactions)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.PaidByPerson)
                  .WithMany(p => p.PaidTransactions)
                  .HasForeignKey(e => e.PaidByPersonId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ImportBatch)
                  .WithMany(b => b.Transactions)
                  .HasForeignKey(e => e.ImportBatchId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // TransactionSplit
        modelBuilder.Entity<TransactionSplit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Percentage).HasPrecision(5, 2);

            entity.HasOne(e => e.Transaction)
                  .WithMany(t => t.Splits)
                  .HasForeignKey(e => e.TransactionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Person)
                  .WithMany(p => p.Splits)
                  .HasForeignKey(e => e.PersonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ImportBatch
        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.BankProfileName).HasMaxLength(100);

            entity.HasOne(e => e.Account)
                  .WithMany()
                  .HasForeignKey(e => e.AccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

