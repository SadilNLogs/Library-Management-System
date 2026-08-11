using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LibraryManagementSystem;

public sealed class ReportsPage : UserControl, IRefreshable
{
    private readonly AppDatabase _database;
    private readonly UserAccount _account;
    private readonly Label _summary = new();

    public ReportsPage(AppDatabase database, UserAccount account)
    {
        _database = database;
        _account = account;
        BackColor = Theme.Background;
        BuildInterface();
    }

    private void BuildInterface()
    {
        Label title = Theme.PageTitle("Reports and data safety");
        title.Dock = DockStyle.Top;
        title.Height = 48;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(0, 5, 0, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        layout.Controls.Add(CreateActionCard(
            "Export reports",
            "Create CSV reports that open in Microsoft Excel. Export the current books, members, or complete loan history.",
            ("Export books", ExportBooks),
            ("Export members", ExportMembers),
            ("Export loans", ExportLoans)), 0, 0);

        layout.Controls.Add(CreateActionCard(
            "Backup and security",
            "Create a dated database backup, open the local data folder, or change the current account password.",
            ("Create backup", CreateBackup),
            ("Open data folder", OpenDataFolder),
            ("Change password", ChangePassword)), 1, 0);

        Panel statusCard = Theme.Card();
        statusCard.Dock = DockStyle.Fill;
        Label statusTitle = new()
        {
            Text = "System summary",
            Dock = DockStyle.Top,
            Height = 42,
            Font = Theme.SectionFont,
            ForeColor = Theme.Text
        };
        _summary.Dock = DockStyle.Fill;
        _summary.Font = new Font("Segoe UI", 10.5F);
        _summary.ForeColor = Theme.Text;
        _summary.Padding = new Padding(0, 8, 0, 0);
        statusCard.Controls.Add(_summary);
        statusCard.Controls.Add(statusTitle);
        layout.Controls.Add(statusCard, 0, 1);

        Panel helpCard = Theme.Card();
        helpCard.Dock = DockStyle.Fill;
        Label helpTitle = new()
        {
            Text = "Data location",
            Dock = DockStyle.Top,
            Height = 42,
            Font = Theme.SectionFont,
            ForeColor = Theme.Text
        };
        Label help = new()
        {
            Dock = DockStyle.Fill,
            Text = "The database is created automatically for the current Windows user. No SQL Server setup or hard-coded PC path is required.\n\n" +
                   "Tip: create a backup after important changes and before moving to another computer.",
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 10F),
            Padding = new Padding(0, 8, 0, 0)
        };
        helpCard.Controls.Add(help);
        helpCard.Controls.Add(helpTitle);
        layout.Controls.Add(helpCard, 1, 1);

        Controls.Add(layout);
        Controls.Add(title);
    }

    public void RefreshData()
    {
        DashboardStats stats = _database.GetStats();
        _summary.Text =
            $"Book titles: {stats.BookTitles}\n" +
            $"Total book copies: {stats.TotalCopies}\n" +
            $"Active members: {stats.Members}\n" +
            $"Active loans: {stats.ActiveLoans}\n" +
            $"Overdue loans: {stats.OverdueLoans}";
    }

    private static Panel CreateActionCard(string title, string description, params (string Text, Action Action)[] actions)
    {
        Panel card = Theme.Card();
        card.Dock = DockStyle.Fill;

        Label heading = new()
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 40,
            Font = Theme.SectionFont,
            ForeColor = Theme.Text
        };
        Label body = new()
        {
            Text = description,
            Dock = DockStyle.Top,
            Height = 70,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Theme.Muted
        };
        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 10, 0, 0)
        };
        foreach ((string text, Action action) in actions)
        {
            Button button = Theme.PrimaryButton(text);
            button.Width = 140;
            button.Click += (_, _) => action();
            buttons.Controls.Add(button);
        }
        card.Controls.Add(buttons);
        card.Controls.Add(body);
        card.Controls.Add(heading);
        return card;
    }

    private void ExportBooks()
    {
        string[] header = { "Accession Number", "Title", "Author", "ISBN", "Category", "Publisher", "Year", "Shelf", "Total Copies", "Available Copies" };
        string[][] rows = _database.Data.Books.OrderBy(book => book.Title).Select(book => new[]
        {
            book.AccessionNumber, book.Title, book.Author, book.Isbn, book.Category, book.Publisher,
            book.PublishYear.ToString(CultureInfo.InvariantCulture), book.Shelf,
            book.TotalCopies.ToString(CultureInfo.InvariantCulture), book.AvailableCopies.ToString(CultureInfo.InvariantCulture)
        }).ToArray();
        ExportCsv("books-report.csv", header, rows);
    }

    private void ExportMembers()
    {
        string[] header = { "Member Number", "Full Name", "Email", "Phone", "Address", "Joined Date", "Status" };
        string[][] rows = _database.Data.Members.OrderBy(member => member.FullName).Select(member => new[]
        {
            member.MemberNumber, member.FullName, member.Email, member.Phone, member.Address,
            member.JoinedDate.ToString("yyyy-MM-dd"), member.IsActive ? "Active" : "Inactive"
        }).ToArray();
        ExportCsv("members-report.csv", header, rows);
    }

    private void ExportLoans()
    {
        string[] header = { "Loan Number", "Book", "Member", "Issue Date", "Due Date", "Return Date", "Status", "Fine Paid (Rs.)" };
        string[][] rows = _database.Data.Loans.OrderByDescending(loan => loan.IssueDate).Select(loan =>
        {
            Book? book = _database.Data.Books.FirstOrDefault(item => item.Id == loan.BookId);
            Member? member = _database.Data.Members.FirstOrDefault(item => item.Id == loan.MemberId);
            string status = loan.IsReturned ? "Returned" : loan.DueDate.Date < DateTime.Today ? "Overdue" : "Issued";
            return new[]
            {
                loan.LoanNumber, book?.Title ?? "Deleted book", member?.FullName ?? "Deleted member",
                loan.IssueDate.ToString("yyyy-MM-dd"), loan.DueDate.ToString("yyyy-MM-dd"),
                loan.ReturnDate?.ToString("yyyy-MM-dd") ?? string.Empty, status,
                loan.FinePaid.ToString("0.00", CultureInfo.InvariantCulture)
            };
        }).ToArray();
        ExportCsv("loans-report.csv", header, rows);
    }

    private void ExportCsv(string defaultName, string[] header, string[][] rows)
    {
        using SaveFileDialog dialog = new()
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = defaultName,
            AddExtension = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using StreamWriter writer = new(dialog.FileName, false, new UTF8Encoding(true));
            writer.WriteLine(string.Join(",", header.Select(EscapeCsv)));
            foreach (string[] row in rows)
                writer.WriteLine(string.Join(",", row.Select(EscapeCsv)));
            Ui.Info($"Report exported successfully.\n\n{dialog.FileName}");
        }
        catch (Exception ex) { Ui.Error(ex); }
    }

    private static string EscapeCsv(string value)
    {
        string safe = value.Replace("\"", "\"\"");
        return safe.IndexOfAny(new[] { ',', '\"', '\r', '\n' }) >= 0 ? $"\"{safe}\"" : safe;
    }

    private void CreateBackup()
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Select a folder for the library backup",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            string file = _database.CreateBackup(dialog.SelectedPath);
            Ui.Info($"Backup created successfully.\n\n{file}");
        }
        catch (Exception ex) { Ui.Error(ex); }
    }

    private void OpenDataFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _database.DataDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex) { Ui.Error(ex); }
    }

    private void ChangePassword()
    {
        using ChangePasswordDialog dialog = new();
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            _database.ChangePassword(_account.Id, dialog.CurrentPassword, dialog.NewPassword);
            Ui.Info("Password changed successfully.");
        }
        catch (Exception ex) { Ui.Error(ex); }
    }
}

public sealed class ChangePasswordDialog : Form
{
    private readonly TextBox _current = new();
    private readonly TextBox _newPassword = new();
    private readonly TextBox _confirm = new();

    public string CurrentPassword => _current.Text;
    public string NewPassword => _newPassword.Text;

    public ChangePasswordDialog()
    {
        Text = "Change password";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(540, 365);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        TableLayoutPanel form = IssueLoanDialog.DialogLayout("Change password", 5);
        ConfigurePasswordBox(_current, "Current password");
        ConfigurePasswordBox(_newPassword, "New password (minimum 8 characters)");
        ConfigurePasswordBox(_confirm, "Confirm new password");
        form.Controls.Add(IssueLoanDialog.DialogField("Current password", _current), 0, 1);
        form.Controls.Add(IssueLoanDialog.DialogField("New password", _newPassword), 0, 2);
        form.Controls.Add(IssueLoanDialog.DialogField("Confirm new password", _confirm), 0, 3);

        FlowLayoutPanel buttons = IssueLoanDialog.DialogButtons(out Button save, out Button cancel);
        save.Text = "Change password";
        save.Width = 145;
        save.Click += (_, _) =>
        {
            if (_newPassword.Text.Length < 8)
            {
                Ui.Info("The new password must contain at least eight characters.");
                return;
            }
            if (!string.Equals(_newPassword.Text, _confirm.Text, StringComparison.Ordinal))
            {
                Ui.Info("The new passwords do not match.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        form.Controls.Add(buttons, 0, 4);
        Controls.Add(form);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private static void ConfigurePasswordBox(TextBox box, string placeholder)
    {
        box.UseSystemPasswordChar = true;
        box.PlaceholderText = placeholder;
    }
}
