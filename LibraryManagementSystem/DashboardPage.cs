using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LibraryManagementSystem;

public sealed class DashboardPage : UserControl, IRefreshable
{
    private readonly AppDatabase _database;
    private readonly FlowLayoutPanel _cards = new();
    private readonly DataGridView _grid = new();

    public DashboardPage(AppDatabase database)
    {
        _database = database;
        BackColor = Theme.Background;
        Padding = new Padding(0);
        BuildInterface();
    }

    private void BuildInterface()
    {
        Label title = Theme.PageTitle("Library overview");
        title.Dock = DockStyle.Top;
        title.Height = 48;

        _cards.Dock = DockStyle.Top;
        _cards.Height = 155;
        _cards.FlowDirection = FlowDirection.LeftToRight;
        _cards.WrapContents = false;
        _cards.Padding = new Padding(0, 5, 0, 12);

        Panel recent = Theme.Card();
        recent.Dock = DockStyle.Fill;
        recent.Padding = new Padding(20);

        Label recentTitle = new()
        {
            Dock = DockStyle.Top,
            Height = 42,
            Text = "Recent circulation",
            Font = Theme.SectionFont,
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _grid.Dock = DockStyle.Fill;
        Theme.StyleGrid(_grid);
        recent.Controls.Add(_grid);
        recent.Controls.Add(recentTitle);

        Controls.Add(recent);
        Controls.Add(_cards);
        Controls.Add(title);
    }

    public void RefreshData()
    {
        DashboardStats stats = _database.GetStats();
        _cards.Controls.Clear();
        _cards.Controls.Add(CreateStatCard("BOOK TITLES", stats.BookTitles.ToString(), "Unique titles in the catalogue", Color.FromArgb(37, 99, 235)));
        _cards.Controls.Add(CreateStatCard("TOTAL COPIES", stats.TotalCopies.ToString(), "All physical copies", Color.FromArgb(124, 58, 237)));
        _cards.Controls.Add(CreateStatCard("ACTIVE MEMBERS", stats.Members.ToString(), "Members allowed to borrow", Theme.Accent));
        _cards.Controls.Add(CreateStatCard("ACTIVE LOANS", stats.ActiveLoans.ToString(), $"{stats.OverdueLoans} overdue", stats.OverdueLoans > 0 ? Theme.Danger : Color.FromArgb(217, 119, 6)));

        var rows = _database.Data.Loans
            .OrderByDescending(loan => loan.IssueDate)
            .Take(12)
            .Select(loan =>
            {
                Book? book = _database.Data.Books.FirstOrDefault(item => item.Id == loan.BookId);
                Member? member = _database.Data.Members.FirstOrDefault(item => item.Id == loan.MemberId);
                string status = loan.IsReturned ? "Returned" : loan.DueDate.Date < DateTime.Today ? "Overdue" : "Issued";
                return new
                {
                    Loan = loan.LoanNumber,
                    Book = book?.Title ?? "Deleted book",
                    Member = member?.FullName ?? "Deleted member",
                    Issued = loan.IssueDate.ToString("dd MMM yyyy"),
                    Due = loan.DueDate.ToString("dd MMM yyyy"),
                    Status = status
                };
            })
            .ToList();
        _grid.DataSource = rows;

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (string.Equals(row.Cells["Status"].Value?.ToString(), "Overdue", StringComparison.Ordinal))
                row.DefaultCellStyle.ForeColor = Theme.Danger;
        }
    }

    private static Panel CreateStatCard(string label, string value, string note, Color color)
    {
        Panel card = Theme.Card();
        card.Size = new Size(225, 126);

        Panel strip = new()
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = color
        };
        Label heading = new()
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = label,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI Semibold", 8.5F)
        };
        Label number = new()
        {
            Dock = DockStyle.Top,
            Height = 48,
            Text = value,
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI Semibold", 24F)
        };
        Label description = new()
        {
            Dock = DockStyle.Fill,
            Text = note,
            ForeColor = color,
            Font = new Font("Segoe UI", 8.5F)
        };
        card.Controls.Add(description);
        card.Controls.Add(number);
        card.Controls.Add(heading);
        card.Controls.Add(strip);
        return card;
    }
}
