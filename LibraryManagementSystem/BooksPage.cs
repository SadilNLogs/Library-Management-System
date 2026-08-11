using System;
using System.Linq;
using System.Windows.Forms;

namespace LibraryManagementSystem;

public sealed class BooksPage : UserControl, IRefreshable
{
    private readonly AppDatabase _database;
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = Theme.SearchBox("Search title, author, ISBN or category...");

    public BooksPage(AppDatabase database)
    {
        _database = database;
        BackColor = Theme.Background;
        BuildInterface();
    }

    private void BuildInterface()
    {
        Label title = Theme.PageTitle("Book catalogue");
        title.Dock = DockStyle.Top;
        title.Height = 48;

        FlowLayoutPanel toolbar = new()
        {
            Dock = DockStyle.Top,
            Height = 58,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        Button add = Theme.PrimaryButton("+ Add book");
        Button edit = Theme.SecondaryButton("Edit");
        Button delete = Theme.DangerButton("Delete");
        Button refresh = Theme.SecondaryButton("Refresh");
        toolbar.Controls.Add(_search);
        toolbar.Controls.Add(add);
        toolbar.Controls.Add(edit);
        toolbar.Controls.Add(delete);
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
        add.Click += (_, _) => AddBook();
        edit.Click += (_, _) => EditBook();
        delete.Click += (_, _) => DeleteBook();
        refresh.Click += (_, _) => RefreshData();
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditBook(); };
    }

    public void RefreshData() => LoadGrid();

    private void LoadGrid()
    {
        string query = _search.Text.Trim();
        var rows = _database.Data.Books
            .Where(book => string.IsNullOrWhiteSpace(query)
                || book.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || book.Author.Contains(query, StringComparison.OrdinalIgnoreCase)
                || book.Isbn.Contains(query, StringComparison.OrdinalIgnoreCase)
                || book.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                || book.AccessionNumber.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(book => book.Title)
            .Select(book => new
            {
                book.Id,
                Accession = book.AccessionNumber,
                book.Title,
                book.Author,
                book.Category,
                ISBN = book.Isbn,
                Copies = book.TotalCopies,
                Available = book.AvailableCopies,
                book.Shelf,
                Status = book.Availability
            })
            .ToList();
        _grid.DataSource = rows;
        if (_grid.Columns.Contains("Id"))
            _grid.Columns["Id"].Visible = false;
    }

    private Guid? SelectedId()
    {
        if (_grid.CurrentRow?.Cells["Id"].Value is Guid id)
            return id;
        Ui.Info("Select a book first.");
        return null;
    }

    private void AddBook()
    {
        Book book = new() { AccessionNumber = _database.NextAccessionNumber(), PublishYear = DateTime.Today.Year, TotalCopies = 1 };
        using BookDialog dialog = new(book, false);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            _database.AddBook(dialog.Book);
            LoadGrid();
            Ui.Info("Book added successfully.");
        }
        catch (Exception ex) { Ui.Error(ex); }
    }

    private void EditBook()
    {
        Guid? id = SelectedId();
        if (!id.HasValue)
            return;
        Book source = _database.Data.Books.First(book => book.Id == id.Value);
        Book copy = new()
        {
            Id = source.Id,
            AccessionNumber = source.AccessionNumber,
            Title = source.Title,
            Author = source.Author,
            Isbn = source.Isbn,
            Category = source.Category,
            Publisher = source.Publisher,
            PublishYear = source.PublishYear,
            Shelf = source.Shelf,
            TotalCopies = source.TotalCopies,
            AvailableCopies = source.AvailableCopies,
            CreatedAt = source.CreatedAt
        };
        using BookDialog dialog = new(copy, true);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            _database.UpdateBook(dialog.Book);
            LoadGrid();
            Ui.Info("Book updated successfully.");
        }
        catch (Exception ex) { Ui.Error(ex); }
    }

    private void DeleteBook()
    {
        Guid? id = SelectedId();
        if (!id.HasValue)
            return;
        Book book = _database.Data.Books.First(item => item.Id == id.Value);
        if (!Ui.Confirm($"Delete '{book.Title}' from the catalogue?"))
            return;
        try
        {
            _database.DeleteBook(book.Id);
            LoadGrid();
        }
        catch (Exception ex) { Ui.Error(ex); }
    }
}
