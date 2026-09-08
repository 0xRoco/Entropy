using System.Windows;
using Entropy.Editor.ViewModels;

namespace Entropy.Editor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        DataContext = new MainViewModel();
        InitializeComponent();
    }
}
