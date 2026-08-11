using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagementSystem;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(244, 247, 250);
    public static readonly Color Surface = Color.White;
    public static readonly Color Sidebar = Color.FromArgb(17, 24, 39);
    public static readonly Color SidebarHover = Color.FromArgb(31, 41, 55);
    public static readonly Color Accent = Color.FromArgb(5, 150, 105);
    public static readonly Color AccentDark = Color.FromArgb(4, 120, 87);
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color Muted = Color.FromArgb(107, 114, 128);
    public static readonly Color Border = Color.FromArgb(226, 232, 240);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Font HeadingFont = new("Segoe UI Semibold", 20F);
    public static readonly Font SectionFont = new("Segoe UI Semibold", 12F);
    public static readonly Font BodyFont = new("Segoe UI", 10F);

    public static Button PrimaryButton(string text)
    {
        Button button = BaseButton(text);
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        button.FlatAppearance.MouseOverBackColor = AccentDark;
        return button;
    }

    public static Button SecondaryButton(string text)
    {
        Button button = BaseButton(text);
        button.BackColor = Surface;
        button.ForeColor = Text;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = Background;
        return button;
    }

    public static Button DangerButton(string text)
    {
        Button button = BaseButton(text);
        button.BackColor = Danger;
        button.ForeColor = Color.White;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
        return button;
    }

    private static Button BaseButton(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Width = 116,
        Height = 38,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Font = new Font("Segoe UI Semibold", 9.5F),
        Margin = new Padding(5),
        FlatAppearance = { BorderSize = 0 }
    };

    public static Label PageTitle(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = HeadingFont,
        ForeColor = Text,
        Margin = new Padding(0, 0, 0, 14)
    };

    public static TextBox SearchBox(string placeholder) => new()
    {
        Width = 290,
        Height = 36,
        Font = BodyFont,
        PlaceholderText = placeholder,
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(0, 5, 10, 5)
    };

    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Border;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowTemplate.Height = 38;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 42;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(209, 250, 229);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(6, 78, 59);
    }

    public static Panel Card() => new()
    {
        BackColor = Surface,
        Padding = new Padding(18),
        Margin = new Padding(7),
        BorderStyle = BorderStyle.FixedSingle
    };
}

internal static class Ui
{
    public static void Error(Exception ex) => MessageBox.Show(
        ex.Message,
        "Unable to complete action",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);

    public static void Info(string message) => MessageBox.Show(
        message,
        "Library Management System",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);

    public static bool Confirm(string message) => MessageBox.Show(
        message,
        "Please confirm",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question) == DialogResult.Yes;
}

internal interface IRefreshable
{
    void RefreshData();
}
