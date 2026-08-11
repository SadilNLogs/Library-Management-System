using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LibraryManagementSystem;

public sealed class LoansPage : UserControl, IRefreshable
{
    private readonly AppDatabase _database;
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = Theme.SearchBox("Search loan, book or member...");
    private readonly ComboBox _filter = new();

    public LoansPage(AppDatabase database)
    {
        _database = database;
        BackColor = Theme.Background;
        BuildInterface();
    }

    private void BuildInterface()
    {
        Label title = Theme.PageTitle("Loans and returns");
        title.Dock = DockStyle.Top;
        title.Height = 48;

        FlowLayoutPanel toolbar = new()
        {
            Dock = DockStyle.Top,
            Height = 58,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        _filter.Width = 140;
        _filter.Height = 36;
        _filter.DropDownStyle = ComboBoxStyle.DropDownList;
        _filter.Margin = new Padding(0, 5, 10, 5);
        _filter.Items.AddRange(new object[] { "All loans", "Active", "Overdue", "Returned" });
        _filter.SelectedIndex = 0;

        Button issue = Theme.PrimaryButton("+ Issue book");
        Button returnBook = Theme.SecondaryButton("Return book");
        Button refresh = Theme.SecondaryButton("Refresh");
        toolbar.Controls.Add(_search);
        toolbar.Controls.Add(_filter);
        toolbar.Controls.Add(issue);
        toolbar.Controls.Add(returnBook);
        toolbar.Controls.Add(refresh);

        Panel card = Theme.Card();
        card.Dock = DockStyle.Fill;
        _grid.Dock = DockStyle.Fill;
        Theme.StyleGrid(_grid);
        card.Controls.Add(_grid);

        Controls.Add(card);
        Controls.Add(toolbar);
        Controls.Add(title);

        _search.TextChanged += (_, _) => LoadGrid();
        _filter.SelectedIndexChanged += (_, _) => LoadGrid();
        issue.Click += (_, _) => IssueBook();
        returnBook.Click += (_, _) => ReturnBook();
        refresh.Click += (_, _) => LoadGrid();
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) ReturnBook(); };
    }

    public void RefreshData() => LoadGrid();

    private void LoadGrid()
    {
        string query = _search.Text.Trim();
        string filter = _filter.SelectedItem?.ToString() ?? "All loans";
        var rows = _database.Data.Loans
            .Select(loan => new
            {
                Loan = loan,
                Book = _database.Data.Books.FirstOrDefault(book => book.Id == loan.BookId),
                Member = _database.Data.Members.FirstOrDefault(member => member.Id == loan.MemberId)
            })
            .Where(item =>
            {
                string status = GetStatus(item.Loan);
                bool filterMatch = filter switch
                {
                    "Active" => status == "Issued" || status == "Overdue",
                    "Overdue" => status == "Overdue",
                    "Returned" => status == "Returned",
                    _ => true
                };
                bool searchMatch = string.IsNullOrWhiteSpace(query)
                    || item.Loan.LoanNumber.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (item.Book?.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.Member?.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.Member?.MemberNumber.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
                return filterMatch && searchMatch;
            })
            .OrderByDescending(item => item.Loan.IssueDate)
            .Select(item => new
            {
                item.Loan.Id,
                Loan = item.Loan.LoanNumber,
                Book = item.Book?.Title ?? "Deleted book",
                Member = item.Member?.FullName ?? "Deleted member",
                Issued = item.Loan.IssueDate.ToString("dd MMM yyyy"),
                Due = item.Loan.DueDate.ToString("dd MMM yyyy"),
                Returned = item.Loan.ReturnDate?.ToString("dd MMM yyyy") ?? "-",
                Status = GetStatus(item.Loan),
                Fine = item.Loan.FinePaid > 0 ? $"Rs. {item.Loan.FinePaid:N2}" : "-"
            })
            .ToList();

        _grid.DataSource = rows;
        if (_grid.Columns.Contains("Id"))
            _grid.Columns["Id"].Visible = false;

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (string.Equals(row.Cells["Status"].Value?.ToString(), "Overdue", StringComparison.Ordinal))
                row.DefaultCellStyle.ForeColor = Theme.Danger;
        }
    }

    private static string GetStatus(Loan loan) => loan.IsReturned
        ? "Returned"
        : loan.DueDate.Date < DateTime.Today ? "Overdue" : "Issued";

    private Guid? SelectedId()
    {
        if (_grid.CurrentRow?.Cells["Id"].Value is Guid id)
            return id;
        Ui.Info("Select a loan first.");
        return null;
    }

    private void IssueBook()
    {
        if (!_database.Data.Books.Any(book => book.AvailableCopies > 0))
        {
            Ui.Info("There are no available book copies to issue.");
            return;
        }
        if (!_database.Data.Members.Any(member => member.IsActive))
        {
            Ui.Info("Add an active member before issuing a book.");
            return;
        }

        using IssueLoanDialog dialog = new(_database);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            _database.IssueBook(dialog.BookId, dialog.MemberId, dialog.IssueDate, dialog.DueDate);
            LoadGrid();
            Ui.Info("Book issued successfully.");
        }
        catch (Exception ex) { Ui.Error(ex); }
    }

    private void ReturnBook()
    {
        Guid? id = SelectedId();
        if (!id.HasValue)
            return;
        Loan loan = _database.Data.Loans.First(item => item.Id == id.Value);
        if (loan.IsReturned)
        {
            Ui.Info("This book has already been returned.");
            return;
        }

        using ReturnLoanDialog dialog = new(_database, loan);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            _database.ReturnBook(loan.Id, dialog.ReturnDate, dialog.FinePaid);
            LoadGrid();
            Ui.Info("Book returned successfully.");
        }
        catch (Exception ex) { Ui.Error(ex); }
    }
}

internal sealed record SelectionItem(Guid Id, string Text)
{
    public override string ToString() => Text;
}

public sealed class IssueLoanDialog : Form
{
    private readonly ComboBox _book = new();
    private readonly ComboBox _member = new();
    private readonly DateTimePicker _issueDate = new();
    private readonly DateTimePicker _dueDate = new();

    public Guid BookId => ((SelectionItem)_book.SelectedItem!).Id;
    public Guid MemberId => ((SelectionItem)_member.SelectedItem!).Id;
    public DateTime IssueDate => _issueDate.Value.Date;
    public DateTime DueDate => _dueDate.Value.Date;

    public IssueLoanDialog(AppDatabase database)
    {
        Text = "Issue a book";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(590, 440);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        _book.Items.AddRange(database.Data.Books
            .Where(book => book.AvailableCopies > 0)
            .OrderBy(book => book.Title)
            .Select(book => new SelectionItem(book.Id, $"{book.Title} — {book.Author} ({book.AvailableCopies} available)"))
            .Cast<object>()
            .ToArray());
        _member.Items.AddRange(database.Data.Members
            .Where(member => member.IsActive)
            .OrderBy(member => member.FullName)
            .Select(member => new SelectionItem(member.Id, $"{member.MemberNumber} — {member.FullName}"))
            .Cast<object>()
            .ToArray());
        _book.SelectedIndex = 0;
        _member.SelectedIndex = 0;
        _issueDate.Value = DateTime.Today;
        _dueDate.Value = DateTime.Today.AddDays(14);

        BuildInterface();
    }

    private void BuildInterface()
    {
        TableLayoutPanel form = DialogLayout("Issue a book", 6);
        _book.DropDownStyle = ComboBoxStyle.DropDownList;
        _member.DropDownStyle = ComboBoxStyle.DropDownList;
        _issueDate.Format = DateTimePickerFormat.Long;
        _dueDate.Format = DateTimePickerFormat.Long;
        form.Controls.Add(DialogField("Book *", _book), 0, 1);
        form.Controls.Add(DialogField("Member *", _member), 0, 2);
        form.Controls.Add(DialogField("Issue date", _issueDate), 0, 3);
        form.Controls.Add(DialogField("Due date", _dueDate), 0, 4);

        FlowLayoutPanel buttons = DialogButtons(out Button save, out Button cancel);
        save.Text = "Issue book";
        save.Click += (_, _) =>
        {
            if (_dueDate.Value.Date <= _issueDate.Value.Date)
            {
                Ui.Info("Due date must be after the issue date.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        form.Controls.Add(buttons, 0, 5);
        Controls.Add(form);
        AcceptButton = save;
        CancelButton = cancel;
    }

    internal static TableLayoutPanel DialogLayout(string title, int rows)
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 24, 30, 20),
            ColumnCount = 1,
            RowCount = rows
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        for (int i = 1; i < rows - 1; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Label heading = Theme.PageTitle(title);
        layout.Controls.Add(heading, 0, 0);
        return layout;
    }

    internal static Panel DialogField(string label, Control control)
    {
        Panel panel = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 0, 6) };
        control.Dock = DockStyle.Fill;
        Label caption = new()
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Theme.Text
        };
        panel.Controls.Add(control);
        panel.Controls.Add(caption);
        return panel;
    }

    internal static FlowLayoutPanel DialogButtons(out Button save, out Button cancel)
    {
        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0)
        };
        save = Theme.PrimaryButton("Save");
        cancel = Theme.SecondaryButton("Cancel");
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        return buttons;
    }
}

public sealed class ReturnLoanDialog : Form
{
    private readonly AppDatabase _database;
    private readonly Loan _loan;
    private readonly DateTimePicker _returnDate = new();
    private readonly NumericUpDown _fine = new();

    public DateTime ReturnDate => _returnDate.Value.Date;
    public decimal FinePaid => _fine.Value;

    public ReturnLoanDialog(AppDatabase database, Loan loan)
    {
        _database = database;
        _loan = loan;
        Text = "Return a book";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(590, 390);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;
        BuildInterface();
        UpdateFine();
    }

    private void BuildInterface()
    {
        TableLayoutPanel form = IssueLoanDialog.DialogLayout("Return book", 5);
        Book? book = _database.Data.Books.FirstOrDefault(item => item.Id == _loan.BookId);
        Member? member = _database.Data.Members.FirstOrDefault(item => item.Id == _loan.MemberId);
        Label summary = new()
        {
            Dock = DockStyle.Fill,
            Text = $"{book?.Title ?? "Unknown book"}\nBorrowed by {member?.FullName ?? "Unknown member"} • Due {_loan.DueDate:dd MMM yyyy}",
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI", 9.5F),
            Padding = new Padding(0, 6, 0, 0)
        };
        _returnDate.Format = DateTimePickerFormat.Long;
        _returnDate.MinDate = _loan.IssueDate.Date;
        _returnDate.Value = DateTime.Today < _loan.IssueDate.Date ? _loan.IssueDate.Date : DateTime.Today;
        _returnDate.ValueChanged += (_, _) => UpdateFine();
        _fine.DecimalPlaces = 2;
        _fine.Maximum = 1000000;
        _fine.ThousandsSeparator = true;

        form.Controls.Add(summary, 0, 1);
        form.Controls.Add(IssueLoanDialog.DialogField("Return date", _returnDate), 0, 2);
        form.Controls.Add(IssueLoanDialog.DialogField("Fine paid (Rs.) — Rs. 10 per overdue day", _fine), 0, 3);
        FlowLayoutPanel buttons = IssueLoanDialog.DialogButtons(out Button save, out Button cancel);
        save.Text = "Confirm return";
        save.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        form.Controls.Add(buttons, 0, 4);
        Controls.Add(form);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void UpdateFine()
    {
        decimal calculated = _database.CalculateFine(_loan, _returnDate.Value.Date);
        _fine.Value = Math.Min(_fine.Maximum, calculated);
    }
}
