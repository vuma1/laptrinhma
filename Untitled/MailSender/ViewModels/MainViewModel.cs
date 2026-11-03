using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using MailSender.Models;
using MailSender.Services;
using System.Threading;
using System.Threading.Tasks;

namespace MailSender.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly SmtpNetworkService _smtpService = new();
        private CancellationTokenSource? _bulkCts;

        // ----------------- Navigation -----------------
        private string _currentView = "Compose";
        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public ICommand NavigateCommand { get; }

        // ----------------- Modal Dialog System -----------------
        private string _activeModal = "None"; // None, Schedule, ContactEdit, ErrorDetail, Success, NetworkError, AuthError, FileLimit
        public string ActiveModal
        {
            get => _activeModal;
            set => SetProperty(ref _activeModal, value);
        }

        public ICommand ShowModalCommand { get; }
        public ICommand CloseModalCommand { get; }

        // ----------------- 1. Soạn thư (Compose) -----------------
        public ObservableCollection<string> SenderAccounts { get; } = new();

        public string SenderAddress => SmtpAccount;

        private string _subject = string.Empty;
        public string Subject
        {
            get => _subject;
            set => SetProperty(ref _subject, value);
        }

        private string _bodyContent = string.Empty;
        public string BodyContent
        {
            get => _bodyContent;
            set => SetProperty(ref _bodyContent, value);
        }

        public ObservableCollection<string> RecipientChips { get; } = new();

        private string _newRecipientInput = string.Empty;
        public string NewRecipientInput
        {
            get => _newRecipientInput;
            set => SetProperty(ref _newRecipientInput, value);
        }

        private bool _isCcVisible = false;
        public bool IsCcVisible
        {
            get => _isCcVisible;
            set => SetProperty(ref _isCcVisible, value);
        }

        private string _ccInput = string.Empty;
        public string CcInput
        {
            get => _ccInput;
            set => SetProperty(ref _ccInput, value);
        }

        private bool _isBccVisible = false;
        public bool IsBccVisible
        {
            get => _isBccVisible;
            set => SetProperty(ref _isBccVisible, value);
        }

        private string _bccInput = string.Empty;
        public string BccInput
        {
            get => _bccInput;
            set => SetProperty(ref _bccInput, value);
        }

        private bool _isHtmlMode = false;
        public bool IsHtmlMode
        {
            get => _isHtmlMode;
            set => SetProperty(ref _isHtmlMode, value);
        }

        public ObservableCollection<AttachmentItem> Attachments { get; } = new();

        private double _totalAttachmentMB;
        public double TotalAttachmentMB
        {
            get => _totalAttachmentMB;
            set
            {
                if (SetProperty(ref _totalAttachmentMB, value))
                {
                    OnPropertyChanged(nameof(AttachmentQuotaText));
                    OnPropertyChanged(nameof(AttachmentQuotaPercent));
                }
            }
        }

        public string AttachmentQuotaText => $"{TotalAttachmentMB:0.0} / 25 MB";
        public double AttachmentQuotaPercent => Math.Min(100.0, (TotalAttachmentMB / 25.0) * 100.0);

        public ICommand RemoveRecipientCommand { get; }
        public ICommand AddRecipientCommand { get; }
        public ICommand ToggleCcCommand { get; }
        public ICommand ToggleBccCommand { get; }
        public ICommand ToggleHtmlModeCommand { get; }
        public ICommand RemoveAttachmentCommand { get; }
        public ICommand AddAttachmentCommand { get; }
        public ICommand SendMailCommand { get; }
        public ICommand DeleteDraftCommand { get; }
        public ICommand InsertVariableCommand { get; }

        // ----------------- 2. Hàng loạt (Bulk Send) -----------------
        private int _bulkStep = 1; // 1: Kiểm tra danh sách, 2: Đang gửi
        public int BulkStep
        {
            get => _bulkStep;
            set => SetProperty(ref _bulkStep, value);
        }

        private bool _isBulkPaused = false;
        public bool IsBulkPaused
        {
            get => _isBulkPaused;
            set => SetProperty(ref _isBulkPaused, value);
        }

        private bool _isSendSingle = true;
        public bool IsSendSingle
        {
            get => _isSendSingle;
            set => SetProperty(ref _isSendSingle, value);
        }

        private string _bulkFileName = "Chưa chọn tệp";
        public string BulkFileName
        {
            get => _bulkFileName;
            set => SetProperty(ref _bulkFileName, value);
        }

        private string _bulkFileStats = "Chọn tệp CSV hoặc TXT để bắt đầu";
        public string BulkFileStats
        {
            get => _bulkFileStats;
            set => SetProperty(ref _bulkFileStats, value);
        }

        private string _bulkAlertSummary = "Chưa có danh sách người nhận";
        public string BulkAlertSummary
        {
            get => _bulkAlertSummary;
            set => SetProperty(ref _bulkAlertSummary, value);
        }

        private int _bulkValidCount;
        public int BulkValidCount
        {
            get => _bulkValidCount;
            set => SetProperty(ref _bulkValidCount, value);
        }

        private double _bulkProgressPercent;
        public double BulkProgressPercent
        {
            get => _bulkProgressPercent;
            set
            {
                if (SetProperty(ref _bulkProgressPercent, value))
                {
                    OnPropertyChanged(nameof(BulkProgressText));
                }
            }
        }
        public string BulkProgressText => $"{(int)BulkProgressPercent}%";

        private string _bulkSentText = "Chưa bắt đầu gửi";
        public string BulkSentText
        {
            get => _bulkSentText;
            set => SetProperty(ref _bulkSentText, value);
        }

        private string _bulkStatsText = "Chưa có dữ liệu gửi";
        public string BulkStatsText
        {
            get => _bulkStatsText;
            set => SetProperty(ref _bulkStatsText, value);
        }

        private string _bulkRunningTimeText = string.Empty;
        public string BulkRunningTimeText
        {
            get => _bulkRunningTimeText;
            set => SetProperty(ref _bulkRunningTimeText, value);
        }

        public ObservableCollection<BulkRecipientItem> BulkVerifyList { get; } = new();
        public ObservableCollection<BulkRecipientItem> BulkSendingList { get; } = new();

        public ICommand ImportBulkFileCommand { get; }
        public ICommand FixInvalidRecipientCommand { get; }
        public ICommand StartBulkSendingCommand { get; }
        public ICommand ToggleBulkPauseCommand { get; }
        public ICommand CancelBulkSendingCommand { get; }

        // ----------------- 3. Danh bạ (Contacts) -----------------
        private bool _hasContacts;
        public bool HasContacts
        {
            get => _hasContacts;
            set => SetProperty(ref _hasContacts, value);
        }

        public ObservableCollection<ContactItem> Contacts { get; } = new();
        public ICollectionView FilteredContacts { get; private set; }

        private string _searchContactText = string.Empty;
        public string SearchContactText
        {
            get => _searchContactText;
            set
            {
                if (SetProperty(ref _searchContactText, value))
                {
                    FilteredContacts.Refresh();
                }
            }
        }

        private string _selectedGroupFilter = "Tất cả nhóm";
        public string SelectedGroupFilter
        {
            get => _selectedGroupFilter;
            set
            {
                if (SetProperty(ref _selectedGroupFilter, value))
                {
                    FilteredContacts.Refresh();
                }
            }
        }

        public int SelectedContactsCount => Contacts.Count(c => c.IsSelected);

        // Editing contact model for Dialog
        private string _dialogContactTitle = "Sửa liên hệ";
        public string DialogContactTitle
        {
            get => _dialogContactTitle;
            set => SetProperty(ref _dialogContactTitle, value);
        }

        private ContactItem? _currentEditingContact;

        private string _editingContactName = string.Empty;
        public string EditingContactName
        {
            get => _editingContactName;
            set => SetProperty(ref _editingContactName, value);
        }

        private string _editingContactEmail = string.Empty;
        public string EditingContactEmail
        {
            get => _editingContactEmail;
            set => SetProperty(ref _editingContactEmail, value);
        }

        private string _editingContactGroup = "Nhóm chung";
        public string EditingContactGroup
        {
            get => _editingContactGroup;
            set => SetProperty(ref _editingContactGroup, value);
        }

        public ICommand OpenAddContactDialogCommand { get; }
        public ICommand OpenEditContactDialogCommand { get; }
        public ICommand SaveContactDialogCommand { get; }
        public ICommand DeleteSelectedContactsCommand { get; }
        public ICommand ComposeToSelectedContactsCommand { get; }
        public ICommand ImportContactsCsvCommand { get; }
        public ICommand ExportContactsCsvCommand { get; }
        public ICommand SelectAllContactsCommand { get; }

        // ----------------- 4. Lịch sử gửi (History) -----------------
        private bool _hasHistory;
        public bool HasHistory
        {
            get => _hasHistory;
            set => SetProperty(ref _hasHistory, value);
        }

        public ObservableCollection<HistoryItem> HistoryItems { get; } = new();
        public ICollectionView FilteredHistoryItems { get; private set; }

        private string _searchHistoryText = string.Empty;
        public string SearchHistoryText
        {
            get => _searchHistoryText;
            set
            {
                if (SetProperty(ref _searchHistoryText, value))
                {
                    FilteredHistoryItems.Refresh();
                }
            }
        }

        private string _selectedHistoryStatusFilter = "Tất cả trạng thái";
        public string SelectedHistoryStatusFilter
        {
            get => _selectedHistoryStatusFilter;
            set
            {
                if (SetProperty(ref _selectedHistoryStatusFilter, value))
                {
                    FilteredHistoryItems.Refresh();
                }
            }
        }

        private HistoryItem? _selectedErrorHistory;
        public HistoryItem? SelectedErrorHistory
        {
            get => _selectedErrorHistory;
            set => SetProperty(ref _selectedErrorHistory, value);
        }

        public int HistorySuccessCount => HistoryItems.Count(h => h.Status == HistoryStatus.Success);
        public int HistoryErrorCount => HistoryItems.Count(h => h.Status == HistoryStatus.Error);
        public int HistoryScheduledCount => HistoryItems.Count(h => h.Status == HistoryStatus.Scheduled);

        public ICommand OpenErrorDetailCommand { get; }
        public ICommand RetrySendErrorHistoryCommand { get; }
        public ICommand ExportHistoryCsvCommand { get; }

        // ----------------- 5. Cài đặt SMTP (Settings) -----------------
        private string _selectedProvider = string.Empty;
        public string SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                if (SetProperty(ref _selectedProvider, value))
                {
                    ApplyProviderPreset(value);
                }
            }
        }

        private string _smtpHost = "smtp.gmail.com";
        public string SmtpHost
        {
            get => _smtpHost;
            set => SetProperty(ref _smtpHost, value);
        }

        private string _smtpPort = "587";
        public string SmtpPort
        {
            get => _smtpPort;
            set => SetProperty(ref _smtpPort, value);
        }

        private string _smtpSecurity = "STARTTLS (khuyên dùng)";
        public string SmtpSecurity
        {
            get => _smtpSecurity;
            set => SetProperty(ref _smtpSecurity, value);
        }

        private string _smtpAccount = string.Empty;
        public string SmtpAccount
        {
            get => _smtpAccount;
            set
            {
                if (SetProperty(ref _smtpAccount, value))
                {
                    if (!string.IsNullOrWhiteSpace(value) && !SenderAccounts.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        SenderAccounts.Add(value);
                    }

                    OnPropertyChanged(nameof(SenderAddress));
                }
            }
        }

        private string _smtpPassword = string.Empty;
        public string SmtpPassword
        {
            get => _smtpPassword;
            set => SetProperty(ref _smtpPassword, value);
        }

        private bool _showPassword = false;
        public bool ShowPassword
        {
            get => _showPassword;
            set => SetProperty(ref _showPassword, value);
        }

        private bool _isConnectionTested;
        public bool IsConnectionTested
        {
            get => _isConnectionTested;
            set => SetProperty(ref _isConnectionTested, value);
        }

        private string _lastTestedTimeText = string.Empty;
        public string LastTestedTimeText
        {
            get => _lastTestedTimeText;
            set => SetProperty(ref _lastTestedTimeText, value);
        }

        private string _profileConnectionStatus = "Chưa kiểm tra kết nối";
        public string ProfileConnectionStatus
        {
            get => _profileConnectionStatus;
            set => SetProperty(ref _profileConnectionStatus, value);
        }

        public ICommand TestConnectionCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand CancelSettingsCommand { get; }
        public ICommand OpenGooglePasswordGuideCommand { get; }

        // ----------------- 6. Hẹn giờ Dialog (Schedule) -----------------
        private string _scheduleDate = string.Empty;
        public string ScheduleDate
        {
            get => _scheduleDate;
            set => SetProperty(ref _scheduleDate, value);
        }

        private string _scheduleTime = string.Empty;
        public string ScheduleTime
        {
            get => _scheduleTime;
            set => SetProperty(ref _scheduleTime, value);
        }

        public ICommand ConfirmScheduleCommand { get; }

        // ----------------- Constructor -----------------
        public MainViewModel()
        {
            // Setup CollectionViews
            FilteredContacts = CollectionViewSource.GetDefaultView(Contacts);
            FilteredContacts.Filter = FilterContactItem;

            FilteredHistoryItems = CollectionViewSource.GetDefaultView(HistoryItems);
            FilteredHistoryItems.Filter = FilterHistoryItem;

            // Navigation
            NavigateCommand = new RelayCommand(p =>
            {
                if (p is string viewName)
                {
                    CurrentView = viewName;
                }
            });

            // Modal
            ShowModalCommand = new RelayCommand(p =>
            {
                if (p is string modalName)
                {
                    ActiveModal = modalName;
                }
            });

            CloseModalCommand = new RelayCommand(() =>
            {
                ActiveModal = "None";
            });

            // Compose Commands
            RemoveRecipientCommand = new RelayCommand(p =>
            {
                if (p is string email)
                {
                    RecipientChips.Remove(email);
                }
            });

            AddRecipientCommand = new RelayCommand(_ =>
            {
                if (!string.IsNullOrWhiteSpace(NewRecipientInput))
                {
                    string email = NewRecipientInput.Trim();
                    if (!RecipientChips.Contains(email))
                    {
                        RecipientChips.Add(email);
                    }
                    NewRecipientInput = string.Empty;
                }
            });

            ToggleCcCommand = new RelayCommand(() => IsCcVisible = !IsCcVisible);
            ToggleBccCommand = new RelayCommand(() => IsBccVisible = !IsBccVisible);
            ToggleHtmlModeCommand = new RelayCommand(() => IsHtmlMode = !IsHtmlMode);

            RemoveAttachmentCommand = new RelayCommand(p =>
            {
                if (p is AttachmentItem item)
                {
                    Attachments.Remove(item);
                    RecalculateAttachmentSize();
                }
            });

            AddAttachmentCommand = new RelayCommand(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Chọn tệp đính kèm",
                    Multiselect = true,
                    Filter = "Tất cả các tệp (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    foreach (var filePath in dialog.FileNames)
                    {
                        var fileInfo = new FileInfo(filePath);
                        double sizeMB = fileInfo.Length / (1024.0 * 1024.0);

                        // If any file exceeds 25 MB, trigger file size warning dialog!
                        if (TotalAttachmentMB + sizeMB > 25.0)
                        {
                            ActiveModal = "FileLimit";
                            return;
                        }

                        Attachments.Add(new AttachmentItem
                        {
                            FileName = fileInfo.Name,
                            FileSize = $"{sizeMB:0.1} MB",
                            FileSizeBytes = fileInfo.Length,
                            FilePath = filePath
                        });
                    }
                    RecalculateAttachmentSize();
                }
            });

            SendMailCommand = new RelayCommand(async () =>
            {
                if (RecipientChips.Any(email =>
                {
                    try
                    {
                        _ = new MailAddress(email);
                        return false;
                    }
                    catch (FormatException)
                    {
                        return true;
                    }
                }))
                {
                    MessageBox.Show("Vui lòng sửa địa chỉ email chưa hợp lệ trước khi gửi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (RecipientChips.Count == 0)
                {
                    MessageBox.Show("Vui lòng thêm ít nhất một người nhận.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var config = GetCurrentSmtpConfig();
                if (string.IsNullOrWhiteSpace(config.Account) || string.IsNullOrWhiteSpace(config.Password))
                {
                    var res = MessageBox.Show("Chưa cấu hình Email/Mật khẩu người gửi tại mục Cài đặt (Settings).\nBạn có muốn chuyển sang mục Cài đặt để cấu hình không?", "Chưa có tài khoản SMTP", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        CurrentView = "Settings";
                    }
                    return;
                }

                try
                {
                    _ = new MailAddress(config.Account);
                }
                catch (FormatException)
                {
                    ActiveModal = "AuthError";
                    return;
                }

                string recipientSummary = RecipientChips.Count > 1 
                    ? string.Join(", ", RecipientChips)
                    : RecipientChips[0];

                var historyEntry = new HistoryItem
                {
                    TimePrimary = DateTime.Now.ToString("dd/MM · HH:mm"),
                    TimeSecondary = DateTime.Now.ToString("yyyy · 'UTC'zzz"),
                    Recipient = recipientSummary,
                    Subject = Subject,
                    Status = HistoryStatus.Scheduled,
                    StatusText = "🔄 Đang gửi...",
                    ActionText = "Chi tiết"
                };
                HistoryItems.Insert(0, historyEntry);
                HasHistory = true;
                OnPropertyChanged(nameof(HistoryScheduledCount));

                try
                {
                    var attachmentPaths = Attachments.Where(a => !string.IsNullOrEmpty(a.FilePath)).Select(a => a.FilePath);
                    await _smtpService.SendMailAsync(config, RecipientChips, CcInput, BccInput, Subject, BodyContent, IsHtmlMode, attachmentPaths);

                    historyEntry.Status = HistoryStatus.Success;
                    historyEntry.StatusText = "✓ Thành công";
                    historyEntry.IsError = false;
                    OnPropertyChanged(nameof(HistorySuccessCount));

                    // Hiển thị Modal Success xịn sò của UI
                    ActiveModal = "Success";
                }
                catch (SmtpException ex)
                {
                    historyEntry.Status = HistoryStatus.Error;
                    historyEntry.StatusText = "⚠ Thất bại";
                    historyEntry.IsError = true;
                    historyEntry.ErrorReason = ex.Message;
                    historyEntry.TechnicalDetails = $"{config.Host}:{config.Port} · SSL: {config.EnableSsl}\n{ex}";
                    OnPropertyChanged(nameof(HistoryErrorCount));

                    ActiveModal = "AuthError";
                }
                catch (SocketException ex)
                {
                    historyEntry.Status = HistoryStatus.Error;
                    historyEntry.StatusText = "⚠ Thất bại";
                    historyEntry.IsError = true;
                    historyEntry.ErrorReason = ex.Message;
                    historyEntry.TechnicalDetails = $"{config.Host}:{config.Port} · SSL: {config.EnableSsl}\n{ex}";
                    OnPropertyChanged(nameof(HistoryErrorCount));

                    ActiveModal = "NetworkError";
                }
                catch (Exception ex)
                {
                    historyEntry.Status = HistoryStatus.Error;
                    historyEntry.StatusText = "⚠ Thất bại";
                    historyEntry.IsError = true;
                    historyEntry.ErrorReason = ex.Message;
                    historyEntry.TechnicalDetails = $"{config.Host}:{config.Port} · SSL: {config.EnableSsl}\n{ex}";
                    OnPropertyChanged(nameof(HistoryErrorCount));

                    ActiveModal = "NetworkError";
                }
                finally
                {
                    FilteredHistoryItems.Refresh();
                }
            });

            DeleteDraftCommand = new RelayCommand(() =>
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa bản nháp này không?", "Xóa bản nháp", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Subject = string.Empty;
                    BodyContent = string.Empty;
                    Attachments.Clear();
                    RecalculateAttachmentSize();
                }
            });

            InsertVariableCommand = new RelayCommand(p =>
            {
                string varText = p as string ?? "{Name}";
                BodyContent += $" {varText}";
            });

            // Bulk Sending Commands
            ImportBulkFileCommand = new RelayCommand(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Nhập danh sách người nhận",
                    Filter = "CSV, TXT files (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    var fileInfo = new FileInfo(dialog.FileName);
                    BulkFileName = fileInfo.Name;
                    
                    var lines = File.ReadAllLines(dialog.FileName);
                    BulkFileStats = $"{lines.Length} dòng · {fileInfo.Length / 1024.0:0.1} KB · Đã nhập lúc {DateTime.Now:HH:mm}";

                    BulkVerifyList.Clear();
                    var seenEmails = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    int valid = 0, dup = 0, invalid = 0;

                    foreach (var rawLine in lines)
                    {
                        if (string.IsNullOrWhiteSpace(rawLine)) continue;

                        var parts = rawLine.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        string name = parts.Length > 1 ? parts[0].Trim() : "Người nhận";
                        string email = parts.Length > 1 ? parts[1].Trim() : parts[0].Trim();

                        if (!email.Contains("@") || !email.Contains("."))
                        {
                            invalid++;
                            BulkVerifyList.Add(new BulkRecipientItem
                            {
                                Name = name,
                                Email = email,
                                Status = BulkItemStatus.Invalid,
                                StatusText = "⛔ Sai định dạng",
                                ActionText = "Sửa địa chỉ",
                                IsActionLink = true
                            });
                        }
                        else if (seenEmails.Contains(email))
                        {
                            dup++;
                            BulkVerifyList.Add(new BulkRecipientItem
                            {
                                Name = name,
                                Email = email,
                                Status = BulkItemStatus.Duplicate,
                                StatusText = "🕒 Trùng địa chỉ",
                                ActionText = "Tự động bỏ qua",
                                IsActionLink = false
                            });
                        }
                        else
                        {
                            seenEmails.Add(email);
                            valid++;
                            BulkVerifyList.Add(new BulkRecipientItem
                            {
                                Name = name,
                                Email = email,
                                Status = BulkItemStatus.Valid,
                                StatusText = "✓ Hợp lệ",
                                ActionText = "Sẵn sàng",
                                IsActionLink = false
                            });
                        }
                    }

                    BulkValidCount = valid;
                    BulkAlertSummary = $"{valid} hợp lệ · {dup} trùng · {invalid} sai định dạng. Chỉ {valid} địa chỉ hợp lệ, không trùng được xếp hàng gửi.";
                }
            });

            FixInvalidRecipientCommand = new RelayCommand(p =>
            {
                if (p is BulkRecipientItem item)
                {
                    if (!item.Email.EndsWith(".com"))
                    {
                        item.Email += ".com";
                    }
                    item.Status = BulkItemStatus.Valid;
                    item.StatusText = "✓ Hợp lệ";
                    item.ActionText = "Sẵn sàng";
                    item.IsActionLink = false;

                    BulkValidCount++;
                    BulkAlertSummary = "5 hợp lệ · 1 trùng · 0 sai định dạng. Chỉ 5 địa chỉ hợp lệ, không trùng được xếp hàng gửi.";
                }
            });

            StartBulkSendingCommand = new RelayCommand(async () =>
            {
                var validRecipients = BulkVerifyList.Where(i => i.Status == BulkItemStatus.Valid).ToList();
                if (validRecipients.Count == 0)
                {
                    MessageBox.Show("Không có địa chỉ hợp lệ để gửi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var config = GetCurrentSmtpConfig();
                if (string.IsNullOrWhiteSpace(config.Account) || string.IsNullOrWhiteSpace(config.Password))
                {
                    var res = MessageBox.Show("Chưa cấu hình Email/Mật khẩu người gửi tại mục Cài đặt (Settings).\nBạn có muốn chuyển sang mục Cài đặt để cấu hình không?", "Chưa có tài khoản SMTP", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        CurrentView = "Settings";
                    }
                    return;
                }

                BulkStep = 2;
                IsBulkPaused = false;

                BulkSendingList.Clear();
                foreach (var item in validRecipients)
                {
                    BulkSendingList.Add(new BulkRecipientItem
                    {
                        Name = item.Name,
                        Email = item.Email,
                        SendingState = SendingStatus.Pending,
                        SendingStatusText = "— Chờ gửi",
                        ProgressPercent = 0
                    });
                }

                _bulkCts?.Cancel();
                _bulkCts = new CancellationTokenSource();
                var ct = _bulkCts.Token;

                int total = BulkSendingList.Count;
                int sentSuccess = 0;
                int sentFailed = 0;

                BulkProgressPercent = 0;
                BulkSentText = $"Đã gửi 0 / {total} thư";
                BulkStatsText = $"0 đã gửi · 0 đang gửi · {total} chờ · 0 lỗi";

                var attachmentPaths = Attachments.Where(a => !string.IsNullOrEmpty(a.FilePath)).Select(a => a.FilePath).ToList();

                for (int i = 0; i < total; i++)
                {
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }

                    while (IsBulkPaused)
                    {
                        await Task.Delay(500);
                        if (ct.IsCancellationRequested) break;
                    }
                    if (ct.IsCancellationRequested) break;

                    var cur = BulkSendingList[i];
                    cur.SendingState = SendingStatus.Sending;
                    cur.SendingStatusText = "🔄 Đang gửi SMTP";
                    cur.ProgressPercent = 30;

                    BulkStatsText = $"{sentSuccess} đã gửi · 1 đang gửi · {Math.Max(0, total - i - 1)} chờ · {sentFailed} lỗi";

                    try
                    {
                        // Cá nhân hóa nội dung {Name}, {Email}
                        string personalizedSubject = (Subject ?? "")
                            .Replace("{Name}", cur.Name)
                            .Replace("{Email}", cur.Email);

                        string personalizedBody = (BodyContent ?? "")
                            .Replace("{Name}", cur.Name)
                            .Replace("{Email}", cur.Email);

                        cur.ProgressPercent = 60;

                        // Gửi thật qua mạng bằng SmtpNetworkService
                        await _smtpService.SendMailAsync(
                            config,
                            new[] { cur.Email },
                            null,
                            null,
                            personalizedSubject,
                            personalizedBody,
                            IsHtmlMode,
                            attachmentPaths);

                        cur.SendingState = SendingStatus.Sent;
                        cur.SendingStatusText = "✓ Đã gửi";
                        cur.ProgressPercent = 100;
                        sentSuccess++;
                    }
                    catch (Exception ex)
                    {
                        cur.SendingState = SendingStatus.Failed;
                        cur.SendingStatusText = "⚠ Lỗi: " + ex.Message;
                        cur.ProgressPercent = 100;
                        sentFailed++;
                    }

                    int done = i + 1;
                    BulkProgressPercent = Math.Min(100.0, (double)done / total * 100.0);
                    BulkSentText = $"Đã gửi {done} / {total} thư";
                    BulkStatsText = $"{sentSuccess} đã gửi · 0 đang gửi · {Math.Max(0, total - done)} chờ · {sentFailed} lỗi";

                    // Delay an toàn 2 giây giữa các email tránh bị server đánh spam
                    if (i < total - 1 && !ct.IsCancellationRequested)
                    {
                        await Task.Delay(2000, ct).ContinueWith(_ => { });
                    }
                }

                if (!ct.IsCancellationRequested)
                {
                    MessageBox.Show($"Hoàn tất chiến dịch gửi thư hàng loạt!\n- Thành công: {sentSuccess}\n- Lỗi: {sentFailed}", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });

            ToggleBulkPauseCommand = new RelayCommand(() =>
            {
                IsBulkPaused = !IsBulkPaused;
            });

            CancelBulkSendingCommand = new RelayCommand(() =>
            {
                _bulkCts?.Cancel();
                BulkStep = 1;
                IsBulkPaused = false;
            });

            // Contacts Commands
            OpenAddContactDialogCommand = new RelayCommand(() =>
            {
                DialogContactTitle = "Thêm liên hệ";
                _currentEditingContact = null;
                EditingContactName = string.Empty;
                EditingContactEmail = string.Empty;
                EditingContactGroup = "Nhóm chung";
                ActiveModal = "ContactEdit";
            });

            OpenEditContactDialogCommand = new RelayCommand(p =>
            {
                if (p is ContactItem item)
                {
                    DialogContactTitle = "Sửa liên hệ";
                    _currentEditingContact = item;
                    EditingContactName = item.Name;
                    EditingContactEmail = item.Email;
                    EditingContactGroup = item.Group;
                    ActiveModal = "ContactEdit";
                }
            });

            SaveContactDialogCommand = new RelayCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(EditingContactName) || string.IsNullOrWhiteSpace(EditingContactEmail))
                {
                    MessageBox.Show("Vui lòng điền họ tên và email.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_currentEditingContact != null)
                {
                    // Edit existing
                    _currentEditingContact.Name = EditingContactName;
                    _currentEditingContact.Email = EditingContactEmail;
                    _currentEditingContact.Group = EditingContactGroup;
                }
                else
                {
                    // Add new
                    var newContact = new ContactItem
                    {
                        Name = EditingContactName,
                        Email = EditingContactEmail,
                        Group = EditingContactGroup,
                        IsSelected = false
                    };
                    newContact.UpdateInitials();
                    newContact.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ContactItem.IsSelected))
                            OnPropertyChanged(nameof(SelectedContactsCount));
                    };
                    Contacts.Add(newContact);
                    HasContacts = true;
                }

                FilteredContacts.Refresh();
                OnPropertyChanged(nameof(SelectedContactsCount));
                ActiveModal = "None";
            });

            DeleteSelectedContactsCommand = new RelayCommand(() =>
            {
                var selected = Contacts.Where(c => c.IsSelected).ToList();
                if (selected.Count == 0) return;

                var res = MessageBox.Show($"Bạn có muốn xóa {selected.Count} liên hệ đã chọn không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    foreach (var c in selected)
                    {
                        Contacts.Remove(c);
                    }
                    HasContacts = Contacts.Count > 0;
                    FilteredContacts.Refresh();
                    OnPropertyChanged(nameof(SelectedContactsCount));
                }
            });

            ComposeToSelectedContactsCommand = new RelayCommand(() =>
            {
                var selected = Contacts.Where(c => c.IsSelected).ToList();
                if (selected.Count > 0)
                {
                    RecipientChips.Clear();
                    foreach (var c in selected)
                    {
                        RecipientChips.Add(c.Email);
                    }
                }
                CurrentView = "Compose";
            });

            ExportContactsCsvCommand = new RelayCommand(() =>
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất danh bạ ra file CSV",
                    FileName = "contacts.csv",
                    Filter = "CSV files (*.csv)|*.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Name,Email,Group");
                    foreach (var c in Contacts)
                    {
                        sb.AppendLine($"\"{c.Name}\",\"{c.Email}\",\"{c.Group}\"");
                    }
                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Đã xuất danh bạ thành công!", "Xuất CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });

            ImportContactsCsvCommand = new RelayCommand(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Nhập danh bạ từ CSV",
                    Filter = "CSV files (*.csv)|*.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var lines = File.ReadAllLines(dialog.FileName);
                    foreach (var line in lines.Skip(1))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(',');
                        if (parts.Length >= 2)
                        {
                            var c = new ContactItem
                            {
                                Name = parts[0].Trim('"', ' '),
                                Email = parts[1].Trim('"', ' '),
                                Group = parts.Length > 2 ? parts[2].Trim('"', ' ') : "Nhóm chung"
                            };
                            c.UpdateInitials();
                            c.PropertyChanged += (s, e) =>
                            {
                                if (e.PropertyName == nameof(ContactItem.IsSelected))
                                    OnPropertyChanged(nameof(SelectedContactsCount));
                            };
                            Contacts.Add(c);
                        }
                    }
                    HasContacts = Contacts.Count > 0;
                    FilteredContacts.Refresh();
                    OnPropertyChanged(nameof(SelectedContactsCount));
                    MessageBox.Show("Nhập danh bạ thành công!", "Nhập CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });

            SelectAllContactsCommand = new RelayCommand(p =>
            {
                bool isCheck = p is bool b && b;
                foreach (var c in Contacts)
                {
                    c.IsSelected = isCheck;
                }
                OnPropertyChanged(nameof(SelectedContactsCount));
            });

            // History Commands
            OpenErrorDetailCommand = new RelayCommand(p =>
            {
                if (p is HistoryItem item)
                {
                    SelectedErrorHistory = item;
                    ActiveModal = "ErrorDetail";
                }
            });

            RetrySendErrorHistoryCommand = new RelayCommand(() =>
            {
                if (SelectedErrorHistory != null)
                {
                    SelectedErrorHistory.Status = HistoryStatus.Success;
                    SelectedErrorHistory.StatusText = "✓ Thành công";
                    SelectedErrorHistory.ActionText = "Chi tiết";
                    SelectedErrorHistory.IsError = false;

                    OnPropertyChanged(nameof(HistorySuccessCount));
                    OnPropertyChanged(nameof(HistoryErrorCount));
                    FilteredHistoryItems.Refresh();
                }
                ActiveModal = "None";
                MessageBox.Show("Đã gửi lại thư thành công!", "Gửi thư", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ExportHistoryCsvCommand = new RelayCommand(() =>
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất lịch sử gửi",
                    FileName = "lich_su_gui.csv",
                    Filter = "CSV files (*.csv)|*.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Time,Recipient,Subject,Status");
                    foreach (var h in HistoryItems)
                    {
                        sb.AppendLine($"\"{h.TimePrimary}\",\"{h.Recipient}\",\"{h.Subject}\",\"{h.StatusText}\"");
                    }
                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Đã xuất lịch sử gửi thành công!", "Xuất lịch sử", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });

            // Settings Commands
            TestConnectionCommand = new RelayCommand(async () =>
            {
                var config = GetCurrentSmtpConfig();
                if (string.IsNullOrWhiteSpace(config.Host) || string.IsNullOrWhiteSpace(config.Account))
                {
                    ActiveModal = "AuthError";
                    return;
                }

                ProfileConnectionStatus = "Đang kiểm tra kết nối...";
                var (success, msg) = await _smtpService.TestConnectionAsync(config);

                IsConnectionTested = success;
                LastTestedTimeText = $"Đã kiểm tra lúc {DateTime.Now:HH:mm}";

                if (success)
                {
                    ProfileConnectionStatus = "SMTP đã kết nối";
                    MessageBox.Show(msg, "Kiểm Tra Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ProfileConnectionStatus = "Lỗi kết nối SMTP";
                    MessageBox.Show(msg, "Kiểm Tra Thất Bại", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            SaveSettingsCommand = new RelayCommand(() =>
            {
                ProfileConnectionStatus = "SMTP đã lưu cấu hình";
                MessageBox.Show("Đã lưu cấu hình SMTP an toàn trên máy này.", "Lưu cấu hình", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            CancelSettingsCommand = new RelayCommand(() =>
            {
                ApplyProviderPreset(SelectedProvider);
            });

            OpenGooglePasswordGuideCommand = new RelayCommand(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://support.google.com/accounts/answer/185833",
                        UseShellExecute = true
                    });
                }
                catch { }
            });

            // Schedule Commands
            ConfirmScheduleCommand = new RelayCommand(() =>
            {
                HistoryItems.Insert(0, new HistoryItem
                {
                    TimePrimary = $"{ScheduleDate} · {ScheduleTime}",
                    TimeSecondary = DateTime.Now.ToString("yyyy · 'UTC'zzz"),
                    Recipient = RecipientChips.Count == 0 ? "Chưa có người nhận" : $"{RecipientChips.Count} người nhận",
                    Subject = Subject,
                    Status = HistoryStatus.Scheduled,
                    StatusText = "🕒 Chờ hẹn giờ",
                    ActionText = "Chi tiết"
                });

                HasHistory = true;
                OnPropertyChanged(nameof(HistoryScheduledCount));
                ActiveModal = "None";
                CurrentView = "History";
            });

            // Setup CollectionViews
            FilteredContacts = CollectionViewSource.GetDefaultView(Contacts);
            FilteredContacts.Filter = FilterContactItem;

            FilteredHistoryItems = CollectionViewSource.GetDefaultView(HistoryItems);
            FilteredHistoryItems.Filter = FilterHistoryItem;
        }

        private bool FilterContactItem(object obj)
        {
            if (obj is not ContactItem item) return false;

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchContactText))
            {
                string query = SearchContactText.Trim();
                bool matchName = item.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
                bool matchEmail = item.Email.Contains(query, StringComparison.OrdinalIgnoreCase);
                if (!matchName && !matchEmail) return false;
            }

            // Group filter
            if (!string.IsNullOrWhiteSpace(SelectedGroupFilter) && !SelectedGroupFilter.StartsWith("Tất cả"))
            {
                if (!item.Group.Equals(SelectedGroupFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private bool FilterHistoryItem(object obj)
        {
            if (obj is not HistoryItem item) return false;

            if (!string.IsNullOrWhiteSpace(SearchHistoryText))
            {
                string query = SearchHistoryText.Trim();
                bool matchRec = item.Recipient.Contains(query, StringComparison.OrdinalIgnoreCase);
                bool matchSub = item.Subject.Contains(query, StringComparison.OrdinalIgnoreCase);
                if (!matchRec && !matchSub) return false;
            }

            if (!string.IsNullOrWhiteSpace(SelectedHistoryStatusFilter) && !SelectedHistoryStatusFilter.StartsWith("Tất cả"))
            {
                if (SelectedHistoryStatusFilter.Contains("Thành công") && item.Status != HistoryStatus.Success) return false;
                if (SelectedHistoryStatusFilter.Contains("Lỗi") && item.Status != HistoryStatus.Error) return false;
                if (SelectedHistoryStatusFilter.Contains("hẹn giờ") && item.Status != HistoryStatus.Scheduled) return false;
            }

            return true;
        }

        private void RecalculateAttachmentSize()
        {
            double total = 0;
            foreach (var item in Attachments)
            {
                total += item.FileSizeBytes > 0 ? (item.FileSizeBytes / (1024.0 * 1024.0)) : 1.5;
            }
            TotalAttachmentMB = total;
        }

        private SmtpConfig GetCurrentSmtpConfig()
        {
            int.TryParse(SmtpPort?.Trim(), out int port);
            if (port <= 0) port = 587;

            bool enableSsl = !string.Equals(SmtpSecurity, "Không TLS", StringComparison.OrdinalIgnoreCase);

            return new SmtpConfig
            {
                Host = SmtpHost?.Trim() ?? "smtp.gmail.com",
                Port = port,
                EnableSsl = enableSsl,
                Account = SmtpAccount?.Trim() ?? "",
                Password = SmtpPassword?.Trim() ?? "",
                DisplayName = "MailSender App"
            };
        }

        private void ApplyProviderPreset(string provider)
        {
            switch (provider)
            {
                case "Gmail":
                    SmtpHost = "smtp.gmail.com";
                    SmtpPort = "587";
                    SmtpSecurity = "STARTTLS (khuyên dùng)";
                    break;
                case "Outlook":
                    SmtpHost = "smtp.office365.com";
                    SmtpPort = "587";
                    SmtpSecurity = "STARTTLS";
                    break;
                case "LAN":
                    SmtpHost = "192.168.1.10";
                    SmtpPort = "25";
                    SmtpSecurity = "Không TLS";
                    break;
            }
        }
    }
}
