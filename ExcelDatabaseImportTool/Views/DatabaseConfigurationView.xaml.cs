using System.Windows;
using System.Windows.Controls;
using ExcelDatabaseImportTool.ViewModels;

namespace ExcelDatabaseImportTool.Views
{
    /// <summary>
    /// Interaction logic for DatabaseConfigurationView.xaml
    /// </summary>
    public partial class DatabaseConfigurationView : UserControl
    {
        public DatabaseConfigurationView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle password changes to update the ViewModel
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is DatabaseConfigurationViewModel viewModel && sender is PasswordBox passwordBox)
            {
                // Update the encrypted password in the current configuration
                viewModel.CurrentConfiguration.EncryptedPassword = passwordBox.Password;
            }
        }
    }
}
