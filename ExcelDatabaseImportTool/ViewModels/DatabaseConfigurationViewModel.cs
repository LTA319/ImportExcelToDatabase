using System.ComponentModel;

namespace ExcelDatabaseImportTool.ViewModels
{
    public class DatabaseConfigurationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Will be implemented in task 8.2
    }
}