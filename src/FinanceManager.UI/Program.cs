using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FinanceManager.Core.Interfaces;
using FinanceManager.Data;
using FinanceManager.Data.Repositories;
using FinanceManager.Services.Categorization;
using FinanceManager.Services.Import;
using FinanceManager.Services.Settlement;
using FinanceManager.UI.Forms;

namespace FinanceManager.UI;

static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider();

        var mainForm = ServiceProvider.GetRequiredService<MainForm>();
        Application.Run(mainForm);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Database
        services.AddDbContext<FinanceDbContext>();

        // Repositories
        services.AddScoped<IFinanceRepository, SqliteFinanceRepository>();

        // Services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<ISettlementService, SettlementService>();

        // UI Forms
        services.AddTransient<MainForm>();
        services.AddTransient<ImportDialog>();
        services.AddTransient<PersonsDialog>();
        services.AddTransient<SettlementDialog>();
    }
}