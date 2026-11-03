using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MailSender.ViewModels;

namespace MailSender.Views
{
    public partial class ComposeView : UserControl
    {
        public ComposeView()
        {
            InitializeComponent();
        }

        private void NewRecipientInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.OemComma)
            {
                e.Handled = true;
                if (DataContext is MainViewModel vm)
                {
                    vm.AddRecipientCommand.Execute(null);
                }
            }
        }

        private void SenderAccount_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void BodyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl + Enter to Send
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                if (DataContext is MainViewModel vm)
                {
                    vm.SendMailCommand.Execute(null);
                }
            }
        }

        private void FormatBold_Click(object sender, RoutedEventArgs e)
        {
            InsertFormatting("**", "**", "in đậm");
        }

        private void FormatItalic_Click(object sender, RoutedEventArgs e)
        {
            InsertFormatting("*", "*", "in nghiêng");
        }

        private void FormatUnderline_Click(object sender, RoutedEventArgs e)
        {
            InsertFormatting("<u>", "</u>", "gạch chân");
        }

        private void FormatLink_Click(object sender, RoutedEventArgs e)
        {
            InsertFormatting("[", "](https://example.com)", "Liên kết");
        }

        private void InsertFormatting(string prefix, string suffix, string defaultPlaceholder)
        {
            string selected = BodyTextBox.SelectedText;
            if (string.IsNullOrEmpty(selected))
            {
                selected = defaultPlaceholder;
            }

            int start = BodyTextBox.SelectionStart;
            string replacement = $"{prefix}{selected}{suffix}";
            BodyTextBox.SelectedText = replacement;
            BodyTextBox.SelectionStart = start + prefix.Length;
            BodyTextBox.SelectionLength = selected.Length;
            BodyTextBox.Focus();
        }

        private void ToggleFormatMode_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.IsHtmlMode = !vm.IsHtmlMode;
            }
        }

        private void Dropzone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.AddAttachmentCommand.Execute(null);
            }
        }

        private void Dropzone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (DataContext is MainViewModel vm)
                {
                    foreach (var f in files)
                    {
                        var info = new System.IO.FileInfo(f);
                        double sizeMB = info.Length / (1024.0 * 1024.0);
                        if (vm.TotalAttachmentMB + sizeMB > 25.0)
                        {
                            vm.ActiveModal = "FileLimit";
                            return;
                        }
                        vm.Attachments.Add(new Models.AttachmentItem
                        {
                            FileName = info.Name,
                            FileSize = $"{sizeMB:0.1} MB",
                            FileSizeBytes = info.Length
                        });
                    }
                    vm.TotalAttachmentMB += 0.1; // trigger update
                }
            }
        }
    }
}
