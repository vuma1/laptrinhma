using System.Windows;
using System.Windows.Controls;
using MailSender.ViewModels;

namespace MailSender.Views.Dialogs
{
    public partial class ErrorDetailDialog : UserControl
    {
        public ErrorDetailDialog()
        {
            InitializeComponent();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window?.DataContext is MainViewModel vm)
            {
                vm.ActiveModal = "None";
                vm.CurrentView = "Settings";
            }
        }
    }
}

