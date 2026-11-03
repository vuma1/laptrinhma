using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MailSender.Models
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class ContactItem : ObservableObject
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    UpdateInitials();
                }
            }
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _group = string.Empty;
        public string Group
        {
            get => _group;
            set => SetProperty(ref _group, value);
        }

        private string _initials = string.Empty;
        public string Initials
        {
            get => _initials;
            set => SetProperty(ref _initials, value);
        }

        public void UpdateInitials()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Initials = "??";
                return;
            }

            var parts = Name.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                Initials = parts[0].Substring(0, System.Math.Min(2, parts[0].Length)).ToUpper();
            }
            else
            {
                Initials = $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}";
            }
        }
    }

    public enum BulkItemStatus
    {
        Valid,
        Duplicate,
        Invalid
    }

    public enum SendingStatus
    {
        Sent,
        Sending,
        Pending,
        Failed
    }

    public class BulkRecipientItem : ObservableObject
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private BulkItemStatus _status;
        public BulkItemStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _actionText = string.Empty;
        public string ActionText
        {
            get => _actionText;
            set => SetProperty(ref _actionText, value);
        }

        private bool _isActionLink;
        public bool IsActionLink
        {
            get => _isActionLink;
            set => SetProperty(ref _isActionLink, value);
        }

        // For sending step
        private SendingStatus _sendingStatus;
        public SendingStatus SendingState
        {
            get => _sendingStatus;
            set => SetProperty(ref _sendingStatus, value);
        }

        private string _sendingStatusText = string.Empty;
        public string SendingStatusText
        {
            get => _sendingStatusText;
            set => SetProperty(ref _sendingStatusText, value);
        }

        private double _progressPercent;
        public double ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }
    }

    public enum HistoryStatus
    {
        Success,
        Error,
        Scheduled
    }

    public class HistoryItem : ObservableObject
    {
        private string _timePrimary = string.Empty;
        public string TimePrimary
        {
            get => _timePrimary;
            set => SetProperty(ref _timePrimary, value);
        }

        private string _timeSecondary = string.Empty;
        public string TimeSecondary
        {
            get => _timeSecondary;
            set => SetProperty(ref _timeSecondary, value);
        }

        private string _recipient = string.Empty;
        public string Recipient
        {
            get => _recipient;
            set => SetProperty(ref _recipient, value);
        }

        private string _subject = string.Empty;
        public string Subject
        {
            get => _subject;
            set => SetProperty(ref _subject, value);
        }

        private HistoryStatus _status;
        public HistoryStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _actionText = "Chi tiết";
        public string ActionText
        {
            get => _actionText;
            set => SetProperty(ref _actionText, value);
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set => SetProperty(ref _isError, value);
        }

        public string ErrorReason { get; set; } = string.Empty;
        public string TechnicalDetails { get; set; } = string.Empty;
    }

    public class AttachmentItem : ObservableObject
    {
        public string FileName { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public double FileSizeBytes { get; set; } = 0;
        public string FilePath { get; set; } = string.Empty;
    }
}
