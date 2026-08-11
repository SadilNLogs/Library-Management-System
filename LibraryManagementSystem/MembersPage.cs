using System;
using System.Linq;
using System.Windows.Forms;

namespace LibraryManagementSystem;

public sealed class MembersPage : UserControl, IRefreshable
{
    private readonly AppDatabase _database;
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = Theme.SearchBox("Search member name, number, email or phone...");

    public MembersPage(AppDatabase database)
    {
        _database = database;
        BackColor = Theme.Background;
        BuildInterface();
    }

    private void BuildInterface()
    {
        Label title = Theme.PageTitle("Member directory");
        title.Dock = DockStyle.Top;
        title.Height = 48;

        FlowLayoutPanel toolbar = new()
        {
            Dock = DockStyle.Top,
            Height = 58,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        Button add = Theme.PrimaryButton("+ Add member");
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
        add.Click += (_, _) => AddMember();
        edit.Click += (_, _) => EditMember();
        delete.Click += (_, _) => DeleteMember();
        refresh.Click += (_, _) => LoadGrid();
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditMember(); };
    }

    public void RefreshData() => LoadGrid();

    private void LoadGrid()
    {
        string query = _search.Text.Trim();
        var rows = _database.Data.Members
            .Where(member => string.IsNullOrWhiteSpace(query)
                || member.MemberNumber.Contains(query, StringComparison.OrdinalIgnoreCase)
                || member.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || member.Email.Contains(query, StringComparison.OrdinalIgnoreCase)
                || member.Phone.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(member => member.FullName)
            .Select(member => new
            {
                member.Id,
                Number = member.MemberNumber,
                Name = member.FullName,
                member.Email,
                member.Phone,
                Joined = member.JoinedDate.ToString("dd MMM yyyy"),
                ActiveLoans = _database.Data.Loans.Count(loan => loan.MemberId == member.Id && !loan.IsReturned),
                Status = member.IsActive ? "Active" : "Inactive"
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
        Ui.Info("Select a member first.");
        return null;
    }

    private void AddMember()
    {
        Member member = new() { MemberNumber = _database.NextMemberNumber() };
        using MemberDialog dialog = new(member, false);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            _database.AddMember(dialog.Member);
            LoadGrid();
            Ui.Info("Member added successfully.");
        }
        catch (Exception ex) { Ui.Error(ex); }
    }

    private void EditMember()
    {
        Guid? id = SelectedId();
        if (!id.HasValue)
            return;
        Member source = _database.Data.Members.First(member => member.Id == id.Value);
        Member copy = new()
        {
            Id = source.Id,
            MemberNumber = source.MemberNumber,
            FullName = source.FullName,
            Email = source.Email,
            Phone = source.Phone,
            Address = source.Address,
            JoinedDate = source.JoinedDate,
            IsActive = source.IsActive
        };
        using MemberDialog dialog = new(copy, true);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            _database.UpdateMember(dialog.Member);
            LoadGrid();
            Ui.Info("Member updated successfully.");
        }
        catch (Exception ex) { Ui.Error(ex); }
    }

    private void DeleteMember()
    {
        Guid? id = SelectedId();
        if (!id.HasValue)
            return;
        Member member = _database.Data.Members.First(item => item.Id == id.Value);
        if (!Ui.Confirm($"Delete member '{member.FullName}'?"))
            return;
        try
        {
            _database.DeleteMember(member.Id);
            LoadGrid();
        }
        catch (Exception ex) { Ui.Error(ex); }
    }
}
