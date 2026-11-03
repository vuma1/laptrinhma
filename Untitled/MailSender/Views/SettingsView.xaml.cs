using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MailSender.ViewModels;

namespace MailSender.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void SelectGmail_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedProvider = "Gmail";
            }
        }

        private void SelectOutlook_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedProvider = "Outlook";
            }
        }

        private void SelectLan_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedProvider = "LAN";
            }
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ShowPassword = !vm.ShowPassword;
            }
        }
    }
}
