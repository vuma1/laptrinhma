using System.Windows;
using System.Windows.Controls;
using MailSender.ViewModels;

namespace MailSender.Views
{
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();
        }

        private void ToggleEmptyHistory_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.HasHistory = !vm.HasHistory;
            }
        }

        private void ViewDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Models.HistoryItem item)
            {
                MessageBox.Show($"Thời gian: {item.TimePrimary} ({item.TimeSecondary})\nNgười nhận: {item.Recipient}\nChủ đề: {item.Subject}\nTrạng thái: {item.StatusText}", "Chi tiết thư đã gửi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
