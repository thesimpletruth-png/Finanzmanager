using FinanceManager.Core.Interfaces;
using FinanceManager.Core.Models;

namespace FinanceManager.Services.Categorization;

public class CategoryService : ICategoryService
{
    private readonly IFinanceRepository _repository;

    public CategoryService(IFinanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Category?> DetectCategoryAsync(string payee, string purpose)
    {
        var rules = await _repository.GetCategoryRulesAsync();
        var searchText = $"{payee} {purpose}".ToUpperInvariant();

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.MatchKeyword)) continue;

            if (searchText.Contains(rule.MatchKeyword.ToUpperInvariant()))
            {
                return rule.Category;
            }
        }

        return null;
    }

    public async Task LearnRuleAsync(string keyword, int categoryId)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;

        var existingRules = await _repository.GetCategoryRulesAsync();
        if (existingRules.Any(r => r.CategoryId == categoryId && r.MatchKeyword.Equals(keyword.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return; // Bereits bekannt
        }

        await _repository.AddCategoryRuleAsync(new CategoryRule
        {
            CategoryId = categoryId,
            MatchKeyword = keyword.Trim().ToUpperInvariant()
        });
    }
}

