using System.Data;
using FinanceManager.Core.Interfaces;
using FinanceManager.Core.Models;

namespace FinanceManager.UI.Forms;

public partial class ImportDialog : Form
{
    private readonly IImportService _importService;
    private readonly IFinanceRepository _repository;

    private ComboBox _cbAccount = null!;
    private ComboBox _cbPerson = null!;
    private ComboBox _cbProfile = null!;
    private TextBox _txtFilePath = null!;
    private Button _btnBrowse = null!;
    private Button _btnAnalyze = null!;
    private DataGridView _gridPreview = null!;
    private Label _lblSummary = null!;
    private Button _btnExecute = null!;
    private Button _btnCancel = null!;

    private ImportPreviewResult? _currentPreview;
    private List<Account> _accounts = new();
    private List<Person> _persons = new();
    private List<Category> _categories = new();

    public ImportDialog(IImportService importService, IFinanceRepository repository)
    {
        _importService = importService;
        _repository = repository;

        InitializeComponent();
        LoadInitialData();
    }

    private void InitializeComponent()
    {
        Text = "Kontoauszug / CSV-Import";
        Size = new Size(1000, 680);
        MinimumSize = new Size(800, 500);
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
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110)); // Top Settings
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));  // Status summary
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // DataGridView
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Buttons

        // 1. Top Settings Panel
        var topPanel = new GroupBox
        {
            Text = "Import-Einstellungen",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(30, 41, 59)
        };

        var settingsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            Padding = new Padding(8)
        };
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        // Row 0: File path
        var lblFile = new Label { Text = "CSV-Datei:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        _txtFilePath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White };
        _btnBrowse = new Button { Text = "Durchsuchen...", Dock = DockStyle.Fill, BackColor = Color.White };
        _btnBrowse.Click += BtnBrowse_Click;

        var lblProfile = new Label { Text = "Bank-Profil:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        _cbProfile = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cbProfile.Items.AddRange(new object[] { "Automatisch erkennen", "Sparkasse Standard", "ING DiBa", "Generisches Bank CSV" });
        _cbProfile.SelectedIndex = 0;

        _btnAnalyze = new Button
        {
            Text = "Einlesen ➔",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnAnalyze.FlatAppearance.BorderSize = 0;
        _btnAnalyze.Click += BtnAnalyze_Click;

        settingsGrid.Controls.Add(lblFile, 0, 0);
        settingsGrid.Controls.Add(_txtFilePath, 1, 0);
        settingsGrid.SetColumnSpan(_txtFilePath, 2);
        settingsGrid.Controls.Add(_btnBrowse, 3, 0);
        settingsGrid.Controls.Add(lblProfile, 4, 0);
        settingsGrid.Controls.Add(_cbProfile, 5, 0);

        // Row 1: Account & Person
        var lblAccount = new Label { Text = "Ziel-Konto:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        _cbAccount = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

        var lblPerson = new Label { Text = "Gezahlt von:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        _cbPerson = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

        settingsGrid.Controls.Add(lblAccount, 0, 1);
        settingsGrid.Controls.Add(_cbAccount, 1, 1);
        settingsGrid.Controls.Add(lblPerson, 2, 1);
        settingsGrid.Controls.Add(_cbPerson, 3, 1);
        settingsGrid.Controls.Add(_btnAnalyze, 5, 1);

        topPanel.Controls.Add(settingsGrid);
        mainLayout.Controls.Add(topPanel, 0, 0);

        // 2. Summary Bar
        _lblSummary = new Label
        {
            Text = "Bitte wähle eine CSV-Datei eines Bankkontos aus und klicke auf 'Einlesen'.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Color.FromArgb(71, 85, 105)
        };
        mainLayout.Controls.Add(_lblSummary, 0, 1);

        // 3. DataGridView Preview
        _gridPreview = new DataGridView
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
        _gridPreview.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
        _gridPreview.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5f);
        _gridPreview.ColumnHeadersHeight = 36;
        _gridPreview.RowTemplate.Height = 30;

        mainLayout.Controls.Add(_gridPreview, 0, 2);

        // 4. Action Buttons
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };

        _btnCancel = new Button
        {
            Text = "Abbrechen",
            Size = new Size(110, 36),
            DialogResult = DialogResult.Cancel,
            BackColor = Color.White
        };

        _btnExecute = new Button
        {
            Text = "Ausgewählte importieren",
            Size = new Size(200, 36),
            BackColor = Color.FromArgb(16, 185, 129), // Emerald 500
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        _btnExecute.FlatAppearance.BorderSize = 0;
        _btnExecute.Click += BtnExecute_Click;

        bottomPanel.Controls.Add(_btnCancel);
        bottomPanel.Controls.Add(_btnExecute);
        mainLayout.Controls.Add(bottomPanel, 0, 3);

        Controls.Add(mainLayout);
    }

    private async void LoadInitialData()
    {
        _accounts = await _repository.GetAccountsAsync();
        _persons = await _repository.GetPersonsAsync();
        _categories = await _repository.GetCategoriesAsync();

        _cbAccount.DisplayMember = "Name";
        _cbAccount.ValueMember = "Id";
        _cbAccount.DataSource = _accounts;

        var personList = new List<object> { new { Id = (int?)null, Name = "-- Konto-Inhaber verwenden --" } };
        personList.AddRange(_persons.Select(p => new { Id = (int?)p.Id, Name = p.Name }));
        _cbPerson.DisplayMember = "Name";
        _cbPerson.ValueMember = "Id";
        _cbPerson.DataSource = personList;
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Kontoauszug / CSV auswählen",
            Filter = "CSV-Dateien (*.csv;*.txt)|*.csv;*.txt|Alle Dateien (*.*)|*.*"
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _txtFilePath.Text = ofd.FileName;
        }
    }

    private async void BtnAnalyze_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtFilePath.Text) || !File.Exists(_txtFilePath.Text))
        {
            MessageBox.Show(this, "Bitte wähle zuerst eine gültige CSV-Datei aus.", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_cbAccount.SelectedItem is not Account selectedAccount)
        {
            MessageBox.Show(this, "Bitte wähle ein Ziel-Konto aus.", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            string? profile = _cbProfile.SelectedIndex > 0 ? _cbProfile.SelectedItem?.ToString() : null;

            _currentPreview = await _importService.PrepareImportAsync(_txtFilePath.Text, selectedAccount.Id, profile);

            RenderPreviewGrid();
            _btnExecute.Enabled = _currentPreview.Rows.Any(r => r.IncludeInImport);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Fehler beim Analysieren der Datei:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void RenderPreviewGrid()
    {
        if (_currentPreview == null) return;

        _lblSummary.Text = $"Erkanntes Profil: [{_currentPreview.DetectedProfileName}] | Gesamt: {_currentPreview.TotalRowsFound} Buchungen gefunden | Neu: {_currentPreview.NewRowsCount} | Dubletten übersprungen: {_currentPreview.DuplicateRowsCount}";

        _gridPreview.Columns.Clear();

        // Checkbox column for selection
        var chkCol = new DataGridViewCheckBoxColumn
        {
            Name = "IncludeInImport",
            HeaderText = "Importieren",
            Width = 90
        };
        _gridPreview.Columns.Add(chkCol);

        _gridPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Datum", Width = 95 });
        _gridPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Payee", HeaderText = "Begünstigter / Auftraggeber", Width = 200 });
        _gridPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Purpose", HeaderText = "Verwendungszweck", Width = 260 });
        _gridPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Betrag (€)", Width = 100 });
        _gridPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Kategorie (Erkannt)", Width = 160 });

        var chkHousehold = new DataGridViewCheckBoxColumn
        {
            Name = "IsHousehold",
            HeaderText = "Haushalt?",
            Width = 85
        };
        _gridPreview.Columns.Add(chkHousehold);

        _gridPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 110 });

        foreach (var row in _currentPreview.Rows)
        {
            int idx = _gridPreview.Rows.Add();
            var dgvRow = _gridPreview.Rows[idx];

            dgvRow.Cells["IncludeInImport"].Value = row.IncludeInImport;
            dgvRow.Cells["Date"].Value = row.BookingDate.ToString("dd.MM.yyyy");
            dgvRow.Cells["Payee"].Value = row.PayeeOrPayer;
            dgvRow.Cells["Purpose"].Value = row.Purpose;
            dgvRow.Cells["Amount"].Value = row.Amount.ToString("N2") + " €";
            dgvRow.Cells["Category"].Value = row.SuggestedCategoryName ?? "Sonstiges";
            dgvRow.Cells["IsHousehold"].Value = row.IsHouseholdExpense;

            if (row.IsDuplicate)
            {
                dgvRow.Cells["Status"].Value = "Dublette";
                dgvRow.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242); // Soft red
                dgvRow.DefaultCellStyle.ForeColor = Color.FromArgb(153, 27, 27);
            }
            else
            {
                dgvRow.Cells["Status"].Value = "Neu";
                dgvRow.DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244); // Soft green
            }

            dgvRow.Tag = row;
        }
    }

    private async void BtnExecute_Click(object? sender, EventArgs e)
    {
        if (_currentPreview == null || _cbAccount.SelectedItem is not Account selectedAccount) return;

        // Sync grid checkbox selections back to preview rows
        for (int i = 0; i < _gridPreview.Rows.Count; i++)
        {
            var dgvRow = _gridPreview.Rows[i];
            if (dgvRow.Tag is ParsedTransactionPreview previewRow)
            {
                previewRow.IncludeInImport = Convert.ToBoolean(dgvRow.Cells["IncludeInImport"].Value);
                previewRow.IsHouseholdExpense = Convert.ToBoolean(dgvRow.Cells["IsHousehold"].Value);
            }
        }

        var toImport = _currentPreview.Rows.Where(r => r.IncludeInImport).ToList();
        if (toImport.Count == 0)
        {
            MessageBox.Show(this, "Es wurden keine Buchungen zum Importieren ausgewählt.", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;

            int? selectedPersonId = null;
            if (_cbPerson.SelectedValue is int pId && pId > 0)
            {
                selectedPersonId = pId;
            }

            var request = new ImportExecutionRequest
            {
                AccountId = selectedAccount.Id,
                FileName = _txtFilePath.Text,
                ProfileName = _currentPreview.DetectedProfileName,
                PaidByPersonId = selectedPersonId,
                SelectedRows = _currentPreview.Rows
            };

            var batch = await _importService.ExecuteImportAsync(request);

            MessageBox.Show(this, $"Import erfolgreich abgeschlossen!\n\nImportierte Buchungen: {batch.ImportedCount}\nÜbersprungene Dubletten: {batch.DuplicateCount}", "Import erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Fehler beim Speichern der Buchungen:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }
}

