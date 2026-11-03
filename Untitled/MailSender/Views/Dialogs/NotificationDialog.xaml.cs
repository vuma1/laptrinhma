using System.Windows;
using System.Windows.Controls;
using MailSender.ViewModels;

namespace MailSender.Views.Dialogs
{
    public partial class NotificationDialog : UserControl
    {
        public NotificationDialog()
        {
            InitializeComponent();
        }

        private void GoToHistory_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window?.DataContext is MainViewModel vm)
            {
                vm.ActiveModal = "None";
                vm.CurrentView = "History";
            }
        }

        private void GoToSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window?.DataContext is MainViewModel vm)
            {
                vm.ActiveModal = "None";
                vm.CurrentView = "Settings";
            }
        }

        private void ChooseOtherFile_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window?.DataContext is MainViewModel vm)
            {
                vm.ActiveModal = "None";
                vm.AddAttachmentCommand.Execute(null);
            }
        }
    }
}
