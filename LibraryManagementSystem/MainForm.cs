using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagementSystem;

public sealed class MainForm : Form
{
    private readonly AppDatabase _database;
    private readonly UserAccount _account;
    private readonly Panel _content = new();
    private readonly Label _pageHeading = new();
    private readonly Dictionary<string, Button> _navigationButtons = new();
    private readonly Dictionary<string, UserControl> _pages = new();

    public MainForm(AppDatabase database, UserAccount account)
    {
        _database = database;
        _account = account;
        Text = "Library Management System";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1120, 700);
        BackColor = Theme.Background;
        Font = Theme.BodyFont;

        BuildInterface();
        ShowPage("Dashboard");
    }

    private void BuildInterface()
    {
        Panel sidebar = new()
        {
            Dock = DockStyle.Left,
            Width = 238,
            BackColor = Theme.Sidebar,
            Padding = new Padding(15, 22, 15, 18)
        };

        Label logo = new()
        {
            Dock = DockStyle.Top,
            Height = 78,
            Text = "LMS  LIBRARY SYSTEM",
            Font = new Font("Segoe UI Semibold", 16F),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        sidebar.Controls.Add(logo);

        FlowLayoutPanel navigation = new()
        {
            Dock = DockStyle.Top,
            Height = 350,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 18, 0, 0)
        };

        AddNavigationButton(navigation, "Dashboard");
        AddNavigationButton(navigation, "Books");
        AddNavigationButton(navigation, "Members");
        AddNavigationButton(navigation, "Loans & Returns");
        AddNavigationButton(navigation, "Reports & Backup");
        sidebar.Controls.Add(navigation);

        Button logout = new()
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            Text = "Log out",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0, MouseOverBackColor = Theme.SidebarHover },
            BackColor = Theme.Sidebar,
            ForeColor = Color.FromArgb(248, 250, 252),
            Cursor = Cursors.Hand
        };
        logout.Click += (_, _) =>
        {
            if (Ui.Confirm("Log out from the current account?"))
            {
                DialogResult = DialogResult.Retry;
                Close();
            }
        };
        sidebar.Controls.Add(logout);

        Label user = new()
        {
            Dock = DockStyle.Bottom,
            Height = 65,
            Text = $"{_account.FullName}\n{_account.Role}",
            ForeColor = Color.FromArgb(203, 213, 225),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0)
        };
        sidebar.Controls.Add(user);

        Panel topbar = new()
        {
            Dock = DockStyle.Top,
            Height = 74,
            BackColor = Theme.Surface,
            Padding = new Padding(28, 0, 28, 0)
        };
        _pageHeading.Dock = DockStyle.Left;
        _pageHeading.AutoSize = false;
        _pageHeading.Width = 480;
        _pageHeading.TextAlign = ContentAlignment.MiddleLeft;
        _pageHeading.Font = new Font("Segoe UI Semibold", 15F);
        _pageHeading.ForeColor = Theme.Text;
        topbar.Controls.Add(_pageHeading);

        Label date = new()
        {
            Dock = DockStyle.Right,
            Width = 260,
            Text = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Theme.Muted
        };
        topbar.Controls.Add(date);

        _content.Dock = DockStyle.Fill;
        _content.BackColor = Theme.Background;
        _content.Padding = new Padding(24);

        Controls.Add(_content);
        Controls.Add(topbar);
        Controls.Add(sidebar);
    }

    private void AddNavigationButton(FlowLayoutPanel parent, string title)
    {
        Button button = new()
        {
            Text = title,
            Width = 208,
            Height = 48,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 0, 0),
            Margin = new Padding(0, 3, 0, 3),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0, MouseOverBackColor = Theme.SidebarHover },
            BackColor = Theme.Sidebar,
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Segoe UI Semibold", 10F),
            Cursor = Cursors.Hand
        };
        button.Click += (_, _) => ShowPage(title);
        _navigationButtons[title] = button;
        parent.Controls.Add(button);
    }

    private void ShowPage(string title)
    {
        if (!_pages.TryGetValue(title, out UserControl? page))
        {
            page = title switch
            {
                "Dashboard" => new DashboardPage(_database),
                "Books" => new BooksPage(_database),
                "Members" => new MembersPage(_database),
                "Loans & Returns" => new LoansPage(_database),
                "Reports & Backup" => new ReportsPage(_database, _account),
                _ => throw new InvalidOperationException("Unknown page.")
            };
            page.Dock = DockStyle.Fill;
            _pages[title] = page;
        }

        _content.SuspendLayout();
        _content.Controls.Clear();
        _content.Controls.Add(page);
        _content.ResumeLayout();
        _pageHeading.Text = title;

        foreach ((string key, Button button) in _navigationButtons)
        {
            bool selected = key == title;
            button.BackColor = selected ? Theme.Accent : Theme.Sidebar;
            button.ForeColor = Color.White;
        }

        if (page is IRefreshable refreshable)
            refreshable.RefreshData();
    }
}
