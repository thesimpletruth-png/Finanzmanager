using FinanceManager.Core.Interfaces;
using FinanceManager.Core.Models;
using FinanceManager.UI.Controls;

namespace FinanceManager.UI.Forms;

public partial class MainForm : Form
{
    private readonly IFinanceRepository _repository;
    private readonly IImportService _importService;
    private readonly ISettlementService _settlementService;

    // Top Header & Dashboard Metrics
    private StatCard _cardBalance = null!;
    private StatCard _cardIncome = null!;
    private StatCard _cardExpense = null!;
    private StatCard _cardHousehold = null!;

    // Filters
    private ComboBox _cbFilterAccount = null!;
    private ComboBox _cbFilterPerson = null!;
    private DateTimePicker _dtpFilterFrom = null!;
    private DateTimePicker _dtpFilterTo = null!;
    private Button _btnRefresh = null!;

    // Main Grid
    private DataGridView _gridTransactions = null!;

    // Actions
    private Button _btnImport = null!;
    private Button _btnPersons = null!;
    private Button _btnSettlement = null!;

    public MainForm(
        IFinanceRepository repository,
        IImportService importService,
        ISettlementService settlementService)
    {
        _repository = repository;
        _importService = importService;
        _settlementService = settlementService;

        InitializeComponent();
        LoadAsync();
    }

    private void InitializeComponent()
    {
        Text = "FinanceManager - Haushaltsbuch & Multi-Personen Finanzverwaltung";
        Size = new Size(1200, 750);
        MinimumSize = new Size(950, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(241, 245, 249); // Slate 100

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));  // Header Menu / Action Bar
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130)); // Stat Cards
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Filter Bar
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // DataGridView

        // 1. Action Bar
        var topActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        _btnImport = CreateActionButton("📥 Kontoauszug / CSV importieren", Color.FromArgb(37, 99, 235));
        _btnImport.Click += BtnImport_Click;

        _btnSettlement = CreateActionButton("⚖️ Haushalts-Ausgleich berechnen", Color.FromArgb(16, 185, 129));
        _btnSettlement.Click += BtnSettlement_Click;

        _btnPersons = CreateActionButton("👥 Personen verwalten", Color.FromArgb(100, 116, 139));
        _btnPersons.Click += BtnPersons_Click;

        topActions.Controls.Add(_btnImport);
        topActions.Controls.Add(_btnSettlement);
        topActions.Controls.Add(_btnPersons);
        mainLayout.Controls.Add(topActions, 0, 0);

        // 2. Stat Cards Dashboard
        var cardsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _cardBalance = new StatCard { Title = "GESAMTSALDO", Value = "0,00 €", Subtitle = "Aktueller Kontostand" };
        _cardIncome = new StatCard { Title = "EINNAHMEN", Value = "0,00 €", ValueColor = Color.FromArgb(16, 185, 129), Subtitle = "Gewählter Zeitraum" };
        _cardExpense = new StatCard { Title = "AUSGABEN", Value = "0,00 €", ValueColor = Color.FromArgb(220, 38, 38), Subtitle = "Gewählter Zeitraum" };
        _cardHousehold = new StatCard { Title = "GEMEINSAME AUSGABEN", Value = "0,00 €", ValueColor = Color.FromArgb(124, 58, 237), Subtitle = "Haushaltsrelevanter Anteil" };

        cardsFlow.Controls.Add(_cardBalance);
        cardsFlow.Controls.Add(_cardIncome);
        cardsFlow.Controls.Add(_cardExpense);
        cardsFlow.Controls.Add(_cardHousehold);
        mainLayout.Controls.Add(cardsFlow, 0, 1);

        // 3. Filter Bar
        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        filterPanel.Controls.Add(new Label { Text = "Konto:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        _cbFilterAccount = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        _cbFilterAccount.SelectedIndexChanged += (s, e) => ReloadTransactions();
        filterPanel.Controls.Add(_cbFilterAccount);

        filterPanel.Controls.Add(new Label { Text = "Person:", AutoSize = true, Padding = new Padding(12, 8, 0, 0) });
        _cbFilterPerson = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        _cbFilterPerson.SelectedIndexChanged += (s, e) => ReloadTransactions();
        filterPanel.Controls.Add(_cbFilterPerson);

        filterPanel.Controls.Add(new Label { Text = "Von:", AutoSize = true, Padding = new Padding(12, 8, 0, 0) });
        _dtpFilterFrom = new DateTimePicker { Value = DateTime.Today.AddMonths(-1), Format = DateTimePickerFormat.Short, Width = 100 };
        _dtpFilterFrom.ValueChanged += (s, e) => ReloadTransactions();
        filterPanel.Controls.Add(_dtpFilterFrom);

        filterPanel.Controls.Add(new Label { Text = "Bis:", AutoSize = true, Padding = new Padding(12, 8, 0, 0) });
        _dtpFilterTo = new DateTimePicker { Value = DateTime.Today, Format = DateTimePickerFormat.Short, Width = 100 };
        _dtpFilterTo.ValueChanged += (s, e) => ReloadTransactions();
        filterPanel.Controls.Add(_dtpFilterTo);

        _btnRefresh = new Button { Text = "🔄 Aktualisieren", Width = 110, Height = 30, BackColor = Color.White };
        _btnRefresh.Click += (s, e) => ReloadTransactions();
        filterPanel.Controls.Add(_btnRefresh);

        mainLayout.Controls.Add(filterPanel, 0, 2);

        // 4. DataGridView
        _gridTransactions = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _gridTransactions.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
        _gridTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5f);
        _gridTransactions.ColumnHeadersHeight = 36;
        _gridTransactions.RowTemplate.Height = 32;

        _gridTransactions.Columns.Add("Date", "Datum");
        _gridTransactions.Columns.Add("Account", "Konto");
        _gridTransactions.Columns.Add("Payee", "Empfänger / Auftraggeber");
        _gridTransactions.Columns.Add("Purpose", "Verwendungszweck");
        _gridTransactions.Columns.Add("Category", "Kategorie");
        _gridTransactions.Columns.Add("Person", "Gezahlt von");
        _gridTransactions.Columns.Add("Household", "Haushalt?");
        _gridTransactions.Columns.Add("Amount", "Betrag (€)");

        var colDate = _gridTransactions.Columns["Date"];
        if (colDate != null) colDate.Width = 90;
        var colAccount = _gridTransactions.Columns["Account"];
        if (colAccount != null) colAccount.Width = 140;
        var colPayee = _gridTransactions.Columns["Payee"];
        if (colPayee != null) colPayee.Width = 200;
        var colPurpose = _gridTransactions.Columns["Purpose"];
        if (colPurpose != null) colPurpose.Width = 260;
        var colCategory = _gridTransactions.Columns["Category"];
        if (colCategory != null) colCategory.Width = 140;
        var colPerson = _gridTransactions.Columns["Person"];
        if (colPerson != null) colPerson.Width = 120;
        var colHousehold = _gridTransactions.Columns["Household"];
        if (colHousehold != null) colHousehold.Width = 85;
        var colAmount = _gridTransactions.Columns["Amount"];
        if (colAmount != null) colAmount.Width = 110;

        mainLayout.Controls.Add(_gridTransactions, 0, 3);

        Controls.Add(mainLayout);
    }

    private Button CreateActionButton(string text, Color backColor)
    {
        var btn = new Button
        {
            Text = text,
            Height = 38,
            AutoSize = true,
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 10, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private async void LoadAsync()
    {
        try
        {
            await _repository.EnsureDatabaseCreatedAsync();

            var accounts = await _repository.GetAccountsAsync();
            var accountFilterList = new List<object> { new { Id = (int?)null, Name = "Alle Konten" } };
            accountFilterList.AddRange(accounts.Select(a => new { Id = (int?)a.Id, Name = a.Name }));
            _cbFilterAccount.DisplayMember = "Name";
            _cbFilterAccount.ValueMember = "Id";
            _cbFilterAccount.DataSource = accountFilterList;

            var persons = await _repository.GetPersonsAsync();
            var personFilterList = new List<object> { new { Id = (int?)null, Name = "Alle Personen" } };
            personFilterList.AddRange(persons.Select(p => new { Id = (int?)p.Id, Name = p.Name }));
            _cbFilterPerson.DisplayMember = "Name";
            _cbFilterPerson.ValueMember = "Id";
            _cbFilterPerson.DataSource = personFilterList;

            ReloadTransactions();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Fehler beim Initialisieren der Datenbank:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ReloadTransactions()
    {
        try
        {
            int? accountId = _cbFilterAccount.SelectedValue is int aId && aId > 0 ? aId : null;
            int? personId = _cbFilterPerson.SelectedValue is int pId && pId > 0 ? pId : null;

            var from = _dtpFilterFrom.Value.Date;
            var to = _dtpFilterTo.Value.Date.AddDays(1).AddTicks(-1);

            var transactions = await _repository.GetTransactionsAsync(from, to, accountId, personId);

            _gridTransactions.Rows.Clear();

            decimal totalIncome = 0m;
            decimal totalExpense = 0m;
            decimal totalHousehold = 0m;

            foreach (var t in transactions)
            {
                if (t.Amount >= 0) totalIncome += t.Amount;
                else
                {
                    totalExpense += Math.Abs(t.Amount);
                    if (t.IsHouseholdExpense) totalHousehold += Math.Abs(t.Amount);
                }

                int idx = _gridTransactions.Rows.Add(
                    t.BookingDate.ToString("dd.MM.yyyy"),
                    t.Account?.Name ?? "-",
                    t.PayeeOrPayer,
                    t.Purpose,
                    t.Category != null ? $"{t.Category.Icon} {t.Category.Name}" : "Sonstiges",
                    t.PaidByPerson?.Name ?? "Gemeinschaftlich",
                    t.IsHouseholdExpense ? "Ja" : "Nein",
                    t.Amount.ToString("N2") + " €"
                );

                var row = _gridTransactions.Rows[idx];
                if (t.Amount >= 0)
                {
                    row.Cells["Amount"].Style.ForeColor = Color.FromArgb(16, 185, 129); // Green
                }
                else
                {
                    row.Cells["Amount"].Style.ForeColor = Color.FromArgb(220, 38, 38); // Red
                }
            }

            // Update Cards
            _cardIncome.Value = $"+{totalIncome:N2} €";
            _cardExpense.Value = $"-{totalExpense:N2} €";
            _cardBalance.Value = $"{(totalIncome - totalExpense):N2} €";
            _cardHousehold.Value = $"{totalHousehold:N2} €";
        }
        catch (Exception)
        {
            // Silently ignore during form load before databinding finishes
        }
    }

    private void BtnImport_Click(object? sender, EventArgs e)
    {
        using var dlg = new ImportDialog(_importService, _repository);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ReloadTransactions();
        }
    }

    private void BtnPersons_Click(object? sender, EventArgs e)
    {
        using var dlg = new PersonsDialog(_repository);
        dlg.ShowDialog(this);
        LoadAsync(); // Refresh filters and names
    }

    private void BtnSettlement_Click(object? sender, EventArgs e)
    {
        using var dlg = new SettlementDialog(_settlementService);
        dlg.ShowDialog(this);
    }
}
