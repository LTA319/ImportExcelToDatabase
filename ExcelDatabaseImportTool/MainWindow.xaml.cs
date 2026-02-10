using System.Windows;
using ExcelDatabaseImportTool.ViewModels;

namespace ExcelDatabaseImportTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // DataContext will be set by dependency injection in App.xaml.cs
    }

    /// <summary>
    /// Constructor with ViewModel injection for dependency injection
    /// </summary>
    /// <param name="viewModel">The MainWindowViewModel instance</param>
    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}