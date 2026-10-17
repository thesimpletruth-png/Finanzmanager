namespace FinanceManager.Core.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "📁";
    public string ColorHex { get; set; } = "#6B7280";
    public bool IsIncome { get; set; } = false;
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public List<Category> SubCategories { get; set; } = new();

    public List<Transaction> Transactions { get; set; } = new();
    public List<CategoryRule> AutoRules { get; set; } = new();
}

public class CategoryRule
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Suchmuster z. B. "REWE", "LIDL", "ARAL", "NETFLIX"
    public string MatchKeyword { get; set; } = string.Empty;
    public bool IsRegex { get; set; } = false;
}

