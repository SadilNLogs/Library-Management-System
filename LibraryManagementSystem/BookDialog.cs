using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagementSystem;

public sealed class BookDialog : Form
{
    private readonly TextBox _accession = new();
    private readonly TextBox _title = new();
    private readonly TextBox _author = new();
    private readonly TextBox _isbn = new();
    private readonly ComboBox _category = new();
    private readonly TextBox _publisher = new();
    private readonly NumericUpDown _year = new();
    private readonly TextBox _shelf = new();
    private readonly NumericUpDown _copies = new();

    public Book Book { get; private set; }

    public BookDialog(Book book, bool editing)
    {
        Book = book;
        Text = editing ? "Edit book" : "Add a new book";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(650, 570);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        BuildInterface();
        FillFields();
    }

    private void BuildInterface()
    {
        TableLayoutPanel form = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 18),
            ColumnCount = 2,
            RowCount = 13
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        Label heading = Theme.PageTitle(Text);
        form.Controls.Add(heading, 0, 0);
        form.SetColumnSpan(heading, 2);

        ConfigureTextBox(_accession);
        ConfigureTextBox(_title);
        ConfigureTextBox(_author);
        ConfigureTextBox(_isbn);
        ConfigureTextBox(_publisher);
        ConfigureTextBox(_shelf);
        _category.Dock = DockStyle.Fill;
        _category.DropDownStyle = ComboBoxStyle.DropDown;
        _category.Items.AddRange(new object[] { "Software Engineering", "Database", "Networking", "IoT", "Electrical Engineering", "Science", "Business", "Fiction", "Other" });
        _year.Dock = DockStyle.Fill;
        _year.Minimum = 0;
        _year.Maximum = DateTime.Today.Year + 1;
        _copies.Dock = DockStyle.Fill;
        _copies.Minimum = 1;
        _copies.Maximum = 10000;

        AddField(form, "Accession number *", _accession, 1, 0);
        AddField(form, "ISBN", _isbn, 1, 1);
        AddWideField(form, "Book title *", _title, 3);
        AddWideField(form, "Author *", _author, 5);
        AddField(form, "Category", _category, 7, 0);
        AddField(form, "Publisher", _publisher, 7, 1);
        AddField(form, "Publication year", _year, 9, 0);
        AddField(form, "Shelf / location", _shelf, 9, 1);

        FlowLayoutPanel bottom = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 17, 0, 0)
        };
        Button save = Theme.PrimaryButton("Save book");
        Button cancel = Theme.SecondaryButton("Cancel");
        cancel.DialogResult = DialogResult.Cancel;
        save.Click += (_, _) => SaveAndClose();
        bottom.Controls.Add(save);
        bottom.Controls.Add(cancel);

        Panel copiesPanel = new() { Dock = DockStyle.Fill };
        Label copiesLabel = FieldLabel("Total copies *");
        copiesLabel.Dock = DockStyle.Top;
        copiesPanel.Controls.Add(_copies);
        copiesPanel.Controls.Add(copiesLabel);

        form.Controls.Add(copiesPanel, 0, 11);
        form.Controls.Add(bottom, 1, 11);
        form.SetRowSpan(copiesPanel, 2);
        form.SetRowSpan(bottom, 2);
        Controls.Add(form);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void FillFields()
    {
        _accession.Text = Book.AccessionNumber;
        _title.Text = Book.Title;
        _author.Text = Book.Author;
        _isbn.Text = Book.Isbn;
        _category.Text = Book.Category;
        _publisher.Text = Book.Publisher;
        _year.Value = Math.Clamp(Book.PublishYear, 0, DateTime.Today.Year + 1);
        _shelf.Text = Book.Shelf;
        _copies.Value = Math.Clamp(Book.TotalCopies, 1, 10000);
    }

    private void SaveAndClose()
    {
        if (string.IsNullOrWhiteSpace(_accession.Text) || string.IsNullOrWhiteSpace(_title.Text) || string.IsNullOrWhiteSpace(_author.Text))
        {
            Ui.Info("Complete all required fields marked with *.");
            return;
        }

        Book.AccessionNumber = _accession.Text.Trim();
        Book.Title = _title.Text.Trim();
        Book.Author = _author.Text.Trim();
        Book.Isbn = _isbn.Text.Trim();
        Book.Category = _category.Text.Trim();
        Book.Publisher = _publisher.Text.Trim();
        Book.PublishYear = (int)_year.Value;
        Book.Shelf = _shelf.Text.Trim();
        Book.TotalCopies = (int)_copies.Value;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static void ConfigureTextBox(TextBox box)
    {
        box.Dock = DockStyle.Fill;
        box.Font = Theme.BodyFont;
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Top,
        Height = 24,
        ForeColor = Theme.Text,
        Font = new Font("Segoe UI Semibold", 9F)
    };

    private static void AddField(TableLayoutPanel form, string label, Control control, int row, int column)
    {
        Panel panel = new() { Dock = DockStyle.Fill, Margin = new Padding(column == 0 ? 0 : 8, 4, column == 0 ? 8 : 0, 7) };
        control.Dock = DockStyle.Fill;
        panel.Controls.Add(control);
        panel.Controls.Add(FieldLabel(label));
        form.Controls.Add(panel, column, row);
        form.SetRowSpan(panel, 2);
    }

    private static void AddWideField(TableLayoutPanel form, string label, Control control, int row)
    {
        Panel panel = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 7) };
        control.Dock = DockStyle.Fill;
        panel.Controls.Add(control);
        panel.Controls.Add(FieldLabel(label));
        form.Controls.Add(panel, 0, row);
        form.SetColumnSpan(panel, 2);
        form.SetRowSpan(panel, 2);
    }
}
