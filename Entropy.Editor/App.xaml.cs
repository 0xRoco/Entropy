using System.IO;
using System.Text;
using System.Windows;

namespace Entropy.Editor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            var sb = new StringBuilder();
            var ex = (Exception?)args.Exception;
            while (ex is not null)
            {
                sb.AppendLine($"--- {ex.GetType().FullName} ---");
                sb.AppendLine(ex.Message);
                sb.AppendLine(ex.StackTrace);
                ex = ex.InnerException;
            }
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "editor-crash.log"), sb.ToString());

            MessageBox.Show(args.Exception.Message, "Entropy Editor",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
