using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagementSystem;

public sealed class LoginForm : Form
{
    private readonly AppDatabase _database;
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();
    private readonly Label _message = new();

    public LoginForm(AppDatabase database)
    {
        _database = database;
        Text = "Sign in - Library Management System";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(980, 610);
        MinimumSize = new Size(900, 570);
        BackColor = Theme.Background;
        Font = Theme.BodyFont;

        BuildInterface();
    }

    private void BuildInterface()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));

        Panel brandPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Sidebar,
            Padding = new Padding(55)
        };
        Label mark = new()
        {
            Text = "LMS",
            Font = new Font("Segoe UI Semibold", 27F),
            ForeColor = Color.White,
            BackColor = Theme.Accent,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(86, 86),
            Location = new Point(58, 130)
        };
        Label brand = new()
        {
            Text = "LIBRARY MANAGEMENT\nSYSTEM",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 21F),
            ForeColor = Color.White,
            Location = new Point(55, 235)
        };
        Label subtitle = new()
        {
            Text = "Library Management System\nManage books, members and circulation with confidence.",
            AutoSize = true,
            MaximumSize = new Size(330, 0),
            Font = new Font("Segoe UI", 11F),
            ForeColor = Color.FromArgb(203, 213, 225),
            Location = new Point(58, 290)
        };
        brandPanel.Controls.Add(mark);
        brandPanel.Controls.Add(brand);
        brandPanel.Controls.Add(subtitle);

        Panel loginPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(78, 85, 78, 60)
        };
        TableLayoutPanel form = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 10
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Label title = new()
        {
            Text = "Welcome back",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 25F),
            ForeColor = Theme.Text,
            Margin = new Padding(0, 0, 0, 4)
        };
        Label intro = new()
        {
            Text = "Sign in to continue to the library dashboard.",
            AutoSize = true,
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 0, 0, 28)
        };

        _username.Dock = DockStyle.Top;
        _username.Font = new Font("Segoe UI", 11F);
        _username.Height = 38;
        _username.PlaceholderText = "Enter username";
        _username.Margin = new Padding(0, 7, 0, 18);

        _password.Dock = DockStyle.Top;
        _password.Font = new Font("Segoe UI", 11F);
        _password.Height = 38;
        _password.PlaceholderText = "Enter password";
        _password.UseSystemPasswordChar = true;
        _password.Margin = new Padding(0, 7, 0, 8);

        CheckBox showPassword = new()
        {
            Text = "Show password",
            AutoSize = true,
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 0, 0, 18)
        };
        showPassword.CheckedChanged += (_, _) => _password.UseSystemPasswordChar = !showPassword.Checked;

        Button signIn = Theme.PrimaryButton("SIGN IN");
        signIn.Dock = DockStyle.Top;
        signIn.Height = 44;
        signIn.Margin = new Padding(0, 3, 0, 12);
        signIn.Click += (_, _) => AttemptLogin();
        AcceptButton = signIn;

        _message.AutoSize = true;
        _message.ForeColor = Theme.Danger;
        _message.Margin = new Padding(0, 0, 0, 18);

        Label demo = new()
        {
            Text = "First login: admin / admin123",
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 9F),
            Margin = new Padding(0, 12, 0, 0)
        };

        form.Controls.Add(title);
        form.Controls.Add(intro);
        form.Controls.Add(FieldLabel("Username"));
        form.Controls.Add(_username);
        form.Controls.Add(FieldLabel("Password"));
        form.Controls.Add(_password);
        form.Controls.Add(showPassword);
        form.Controls.Add(_message);
        form.Controls.Add(signIn);
        form.Controls.Add(demo);
        loginPanel.Controls.Add(form);

        layout.Controls.Add(brandPanel, 0, 0);
        layout.Controls.Add(loginPanel, 1, 0);
        Controls.Add(layout);

        Shown += (_, _) => _username.Focus();
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Theme.Text,
        Font = new Font("Segoe UI Semibold", 9.5F)
    };

    private void AttemptLogin()
    {
        _message.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(_username.Text) || string.IsNullOrEmpty(_password.Text))
        {
            _message.Text = "Enter both username and password.";
            return;
        }

        UserAccount? account = _database.Authenticate(_username.Text, _password.Text);
        if (account is null)
        {
            _message.Text = "The username or password is incorrect.";
            _password.SelectAll();
            _password.Focus();
            return;
        }

        Hide();
        using MainForm main = new(_database, account);
        DialogResult result = main.ShowDialog(this);
        if (result == DialogResult.Retry)
        {
            _password.Clear();
            _message.Text = string.Empty;
            Show();
            Activate();
            _password.Focus();
        }
        else
        {
            Close();
        }
    }
}
