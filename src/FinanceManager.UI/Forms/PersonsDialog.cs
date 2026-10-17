using FinanceManager.Core.Interfaces;
using FinanceManager.Core.Models;

namespace FinanceManager.UI.Forms;

public partial class PersonsDialog : Form
{
    private readonly IFinanceRepository _repository;
    private ListBox _listPersons = null!;
    private TextBox _txtName = null!;
    private NumericUpDown _numIncome = null!;
    private Button _btnSave = null!;
    private Button _btnAdd = null!;
    private Button _btnDelete = null!;
    private Person? _selectedPerson;

    public PersonsDialog(IFinanceRepository repository)
    {
        _repository = repository;
        InitializeComponent();
        LoadPersons();
    }

    private void InitializeComponent()
    {
        Text = "Haushaltsmitglieder & Personen";
        Size = new Size(650, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(248, 250, 252);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(16)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

        // Left: List
        var leftBox = new GroupBox { Text = "Personen im Haushalt", Dock = DockStyle.Fill };
        _listPersons = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
        _listPersons.SelectedIndexChanged += ListPersons_SelectedIndexChanged;

        var leftButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.LeftToRight };
        _btnAdd = new Button { Text = "+ Neu", Width = 80, Height = 32, BackColor = Color.White };
        _btnAdd.Click += (s, e) => ResetForm();
        _btnDelete = new Button { Text = "Löschen", Width = 80, Height = 32, BackColor = Color.White, ForeColor = Color.DarkRed };
        _btnDelete.Click += BtnDelete_Click;
        leftButtons.Controls.Add(_btnAdd);
        leftButtons.Controls.Add(_btnDelete);

        leftBox.Controls.Add(_listPersons);
        leftBox.Controls.Add(leftButtons);
        layout.Controls.Add(leftBox, 0, 0);

        // Right: Details
        var rightBox = new GroupBox { Text = "Person Details", Dock = DockStyle.Fill };
        var formGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(12) };
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        formGrid.Controls.Add(new Label { Text = "Name:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _txtName = new TextBox { Dock = DockStyle.Fill };
        formGrid.Controls.Add(_txtName, 1, 0);

        formGrid.Controls.Add(new Label { Text = "Nettoeinkommen (€):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        _numIncome = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            DecimalPlaces = 2,
            Maximum = 1000000,
            Increment = 50
        };
        formGrid.Controls.Add(_numIncome, 1, 1);

        var lblHelp = new Label
        {
            Text = "Hinweis: Das monatliche Nettoeinkommen dient als Basis für den Einkommens-proportionalen Haushalts-Split.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 8.5f)
        };
        formGrid.Controls.Add(lblHelp, 0, 2);
        formGrid.SetColumnSpan(lblHelp, 2);

        _btnSave = new Button
        {
            Text = "Speichern",
            Dock = DockStyle.Right,
            Width = 120,
            Height = 36,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += BtnSave_Click;
        formGrid.Controls.Add(_btnSave, 1, 3);

        rightBox.Controls.Add(formGrid);
        layout.Controls.Add(rightBox, 1, 0);

        Controls.Add(layout);
    }

    private async void LoadPersons()
    {
        var persons = await _repository.GetPersonsAsync();
        _listPersons.DataSource = null;
        _listPersons.DisplayMember = "Name";
        _listPersons.ValueMember = "Id";
        _listPersons.DataSource = persons;

        if (persons.Count > 0)
        {
            _listPersons.SelectedIndex = 0;
        }
    }

    private void ListPersons_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_listPersons.SelectedItem is Person p)
        {
            _selectedPerson = p;
            _txtName.Text = p.Name;
            _numIncome.Value = p.MonthlyNetIncome;
        }
    }

    private void ResetForm()
    {
        _selectedPerson = null;
        _txtName.Clear();
        _numIncome.Value = 0;
        _txtName.Focus();
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show(this, "Bitte gib einen Namen ein.", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_selectedPerson == null)
        {
            var newPerson = new Person
            {
                Name = _txtName.Text.Trim(),
                MonthlyNetIncome = _numIncome.Value
            };
            await _repository.AddPersonAsync(newPerson);
        }
        else
        {
            _selectedPerson.Name = _txtName.Text.Trim();
            _selectedPerson.MonthlyNetIncome = _numIncome.Value;
            await _repository.UpdatePersonAsync(_selectedPerson);
        }

        LoadPersons();
    }

    private async void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_selectedPerson != null)
        {
            if (MessageBox.Show(this, $"Möchtest du '{_selectedPerson.Name}' wirklich löschen?", "Bestätigung", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await _repository.DeletePersonAsync(_selectedPerson.Id);
                ResetForm();
                LoadPersons();
            }
        }
    }
}

