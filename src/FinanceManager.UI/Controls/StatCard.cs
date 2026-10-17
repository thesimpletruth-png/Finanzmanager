using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FinanceManager.UI.Controls;

public class StatCard : Panel
{
    private Label _lblTitle = null!;
    private Label _lblValue = null!;
    private Label _lblSub = null!;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Title
    {
        get => _lblTitle.Text;
        set => _lblTitle.Text = value;
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Value
    {
        get => _lblValue.Text;
        set => _lblValue.Text = value;
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Subtitle
    {
        get => _lblSub.Text;
        set => _lblSub.Text = value;
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color ValueColor
    {
        get => _lblValue.ForeColor;
        set => _lblValue.ForeColor = value;
    }

    public StatCard()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        Padding = new Padding(16);
        Margin = new Padding(8);
        Size = new Size(240, 110);

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        _lblTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Color.FromArgb(100, 116, 139), // Slate 500
            Text = "TITEL"
        };

        _lblValue = new Label
        {
            Dock = DockStyle.Top,
            Height = 38,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42), // Slate 900
            Text = "0,00 €"
        };

        _lblSub = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 20,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(148, 163, 184), // Slate 400
            Text = ""
        };

        Controls.Add(_lblSub);
        Controls.Add(_lblValue);
        Controls.Add(_lblTitle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw subtle border
        using var pen = new Pen(Color.FromArgb(226, 232, 240), 1.5f);
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
    }
}
