using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagementSystem;

public sealed class MemberDialog : Form
{
    private readonly TextBox _number = new();
    private readonly TextBox _name = new();
    private readonly TextBox _email = new();
    private readonly TextBox _phone = new();
    private readonly TextBox _address = new();
    private readonly DateTimePicker _joined = new();
    private readonly CheckBox _active = new();

    public Member Member { get; private set; }

    public MemberDialog(Member member, bool editing)
    {
        Member = member;
        Text = editing ? "Edit member" : "Add a new member";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(610, 590);
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
            Padding = new Padding(32, 25, 32, 22),
            ColumnCount = 1,
            RowCount = 9
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        for (int i = 1; i <= 6; i++)
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 5 ? 88 : 61));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label heading = Theme.PageTitle(Text);
        form.Controls.Add(heading, 0, 0);
        ConfigureTextBox(_number);
        ConfigureTextBox(_name);
        ConfigureTextBox(_email);
        ConfigureTextBox(_phone);
        ConfigureTextBox(_address);
        _address.Multiline = true;
        _address.ScrollBars = ScrollBars.Vertical;
        _joined.Dock = DockStyle.Fill;
        _joined.Format = DateTimePickerFormat.Long;
        _active.Text = "Active member (allowed to borrow books)";
        _active.AutoSize = true;
        _active.ForeColor = Theme.Text;

        form.Controls.Add(Field("Member number *", _number), 0, 1);
        form.Controls.Add(Field("Full name *", _name), 0, 2);
        form.Controls.Add(Field("Email", _email), 0, 3);
        form.Controls.Add(Field("Phone", _phone), 0, 4);
        form.Controls.Add(Field("Address", _address), 0, 5);
        form.Controls.Add(Field("Joined date", _joined), 0, 6);
        form.Controls.Add(_active, 0, 7);

        FlowLayoutPanel bottom = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };
        Button save = Theme.PrimaryButton("Save member");
        Button cancel = Theme.SecondaryButton("Cancel");
        cancel.DialogResult = DialogResult.Cancel;
        save.Click += (_, _) => SaveAndClose();
        bottom.Controls.Add(save);
        bottom.Controls.Add(cancel);
        form.Controls.Add(bottom, 0, 8);
        Controls.Add(form);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void FillFields()
    {
        _number.Text = Member.MemberNumber;
        _name.Text = Member.FullName;
        _email.Text = Member.Email;
        _phone.Text = Member.Phone;
        _address.Text = Member.Address;
        _joined.Value = Member.JoinedDate.Date;
        _active.Checked = Member.IsActive;
    }

    private void SaveAndClose()
    {
        if (string.IsNullOrWhiteSpace(_number.Text) || string.IsNullOrWhiteSpace(_name.Text))
        {
            Ui.Info("Member number and full name are required.");
            return;
        }
        if (!string.IsNullOrWhiteSpace(_email.Text) && !_email.Text.Contains('@'))
        {
            Ui.Info("Enter a valid email address.");
            return;
        }

        Member.MemberNumber = _number.Text.Trim();
        Member.FullName = _name.Text.Trim();
        Member.Email = _email.Text.Trim();
        Member.Phone = _phone.Text.Trim();
        Member.Address = _address.Text.Trim();
        Member.JoinedDate = _joined.Value.Date;
        Member.IsActive = _active.Checked;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static void ConfigureTextBox(TextBox box)
    {
        box.Dock = DockStyle.Fill;
        box.Font = Theme.BodyFont;
    }

    private static Panel Field(string label, Control control)
    {
        Panel panel = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 0, 5) };
        control.Dock = DockStyle.Fill;
        Label caption = new()
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 23,
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI Semibold", 9F)
        };
        panel.Controls.Add(control);
        panel.Controls.Add(caption);
        return panel;
    }
}
