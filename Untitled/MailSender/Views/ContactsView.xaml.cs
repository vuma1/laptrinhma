using System.Windows;
using System.Windows.Controls;
using MailSender.ViewModels;

namespace MailSender.Views
{
    public partial class ContactsView : UserControl
    {
        public ContactsView()
        {
            InitializeComponent();
        }

        private void ToggleEmptyState_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.HasContacts = !vm.HasContacts;
            }
        }

        private void SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectAllContactsCommand.Execute(true);
            }
        }

        private void SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectAllContactsCommand.Execute(false);
            }
        }

        private void ChangeGroup_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                foreach (var c in vm.Contacts)
                {
                    if (c.IsSelected)
                    {
                        c.Group = "Nhóm chung";
                    }
                }
                vm.FilteredContacts.Refresh();
                MessageBox.Show("Đã đổi nhóm thành công cho các liên hệ được chọn!", "Đổi nhóm", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RowMoreOptions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Models.ContactItem item)
            {
                var contextMenu = new ContextMenu();
                var editItem = new MenuItem { Header = "Sửa liên hệ" };
                editItem.Click += (_, _) =>
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.OpenEditContactDialogCommand.Execute(item);
                    }
                };

                var deleteItem = new MenuItem { Header = "Xóa liên hệ" };
                deleteItem.Click += (_, _) =>
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.Contacts.Remove(item);
                        vm.FilteredContacts.Refresh();
                    }
                };

                contextMenu.Items.Add(editItem);
                contextMenu.Items.Add(deleteItem);
                contextMenu.IsOpen = true;
            }
        }
    }
}
