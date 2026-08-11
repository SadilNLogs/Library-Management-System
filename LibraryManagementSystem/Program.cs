using System;
using System.Windows.Forms;

namespace LibraryManagementSystem;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            AppDatabase database = new();
            Application.Run(new LoginForm(database));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The application could not start.\n\n" + ex.Message,
                "Startup error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
