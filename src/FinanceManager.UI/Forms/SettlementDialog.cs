using FinanceManager.Core.Interfaces;
using FinanceManager.Core.Models;

namespace FinanceManager.UI.Forms;

public partial class SettlementDialog : Form
{
    private readonly ISettlementService _settlementService;
    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private ComboBox _cbMethod = null!;
    private Button _btnCalculate = null!;
    private DataGridView _gridContributions = null!;
    private DataGridView _gridTransfers = null!;
    private Label _lblTotal = null!;

    public SettlementDialog(ISettlementService settlementService)
    {
        _settlementService = settlementService;
        InitializeComponent();
        Calculate();
    }

    private void InitializeComponent()
    {
        Text = "Haushalts-Abrechnung & Kostenteilung";
        Size = new Size(880, 600);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(248, 250, 252);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); // Filter
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Total summary
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));  // Contributions
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));  // Transfers

        // 1. Filter Bar
        var filterPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        filterPanel.Controls.Add(new Label { Text = "Von:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        _dtpFrom = new DateTimePicker { Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), Format = DateTimePickerFormat.Short, Width = 110 };
        filterPanel.Controls.Add(_dtpFrom);

        filterPanel.Controls.Add(new Label { Text = "Bis:", AutoSize = true, Padding = new Padding(12, 8, 0, 0) });
        _dtpTo = new DateTimePicker { Value = DateTime.Today, Format = DateTimePickerFormat.Short, Width = 110 };
        filterPanel.Controls.Add(_dtpTo);

        filterPanel.Controls.Add(new Label { Text = "Split-Methode:", AutoSize = true, Padding = new Padding(16, 8, 0, 0) });
        _cbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
        _cbMethod.Items.AddRange(new object[] { "50:50 (Gleichmäßige Teilung)", "Einkommens-proportional (Fair Share)" });
        _cbMethod.SelectedIndex = 1; // Default fair share
        filterPanel.Controls.Add(_cbMethod);

        _btnCalculate = new Button
        {
            Text = "Berechnen",
            Width = 110,
            Height = 30,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnCalculate.FlatAppearance.BorderSize = 0;
        _btnCalculate.Click += (s, e) => Calculate();
        filterPanel.Controls.Add(_btnCalculate);

        mainLayout.Controls.Add(filterPanel, 0, 0);

        // 2. Summary
        _lblTotal = new Label
        {
            Text = "Gemeinsame Haushaltsausgaben: 0,00 €",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        mainLayout.Controls.Add(_lblTotal, 0, 1);

        // 3. Grid Contributions
        var box1 = new GroupBox { Text = "Beteiligung & Saldo der Personen", Dock = DockStyle.Fill };
        _gridContributions = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        _gridContributions.Columns.Add("Person", "Person");
        _gridContributions.Columns.Add("Paid", "Verauslagt (€)");
        _gridContributions.Columns.Add("TargetPct", "Soll-Anteil (%)");
        _gridContributions.Columns.Add("TargetShare", "Soll-Betrag (€)");
        _gridContributions.Columns.Add("Balance", "Saldo / Differenz (€)");

        box1.Controls.Add(_gridContributions);
        mainLayout.Controls.Add(box1, 0, 2);

        // 4. Grid Transfers
        var box2 = new GroupBox { Text = "Empfohlener Ausgleichstransfer (Wer zahlt wem wie viel?)", Dock = DockStyle.Fill };
        _gridTransfers = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        _gridTransfers.Columns.Add("From", "Zahlt von");
        _gridTransfers.Columns.Add("To", "An Empfänger");
        _gridTransfers.Columns.Add("Amount", "Ausgleichsbetrag (€)");
        _gridTransfers.Columns.Add("Reason", "Zweck");

        box2.Controls.Add(_gridTransfers);
        mainLayout.Controls.Add(box2, 0, 3);

        Controls.Add(mainLayout);
    }

    private async void Calculate()
    {
        try
        {
            var method = _cbMethod.SelectedIndex == 1 ? SplitMethod.IncomeProportional : SplitMethod.FiftyFifty;
            var result = await _settlementService.CalculateSettlementAsync(_dtpFrom.Value, _dtpTo.Value, method);

            _lblTotal.Text = $"Gemeinsame Haushaltsausgaben im Zeitraum: {result.TotalHouseholdExpenses:N2} €";

            _gridContributions.Rows.Clear();
            foreach (var c in result.Contributions)
            {
                int idx = _gridContributions.Rows.Add(
                    c.PersonName,
                    c.ActualPaidAmount.ToString("N2") + " €",
                    c.TargetPercentage.ToString("F1") + " %",
                    c.TargetShareAmount.ToString("N2") + " €",
                    (c.Balance >= 0 ? "+" : "") + c.Balance.ToString("N2") + " €"
                );

                var row = _gridContributions.Rows[idx];
                if (c.Balance > 0)
                {
                    row.Cells[4].Style.ForeColor = Color.FromArgb(16, 185, 129); // Green (bekommt Geld)
                }
                else if (c.Balance < 0)
                {
                    row.Cells[4].Style.ForeColor = Color.FromArgb(220, 38, 38); // Red (schuldet Geld)
                }
            }

            _gridTransfers.Rows.Clear();
            if (result.RecommendedTransfers.Count == 0)
            {
                _gridTransfers.Rows.Add("Ausgeglichen", "Niemand", "0,00 €", "Keine Ausgleichszahlung erforderlich");
            }
            else
            {
                foreach (var t in result.RecommendedTransfers)
                {
                    _gridTransfers.Rows.Add(t.FromPerson, t.ToPerson, t.Amount.ToString("N2") + " €", t.Reason);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Fehler bei der Berechnung:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

