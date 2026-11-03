using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MailSender.Services
{
    public class SmtpConfig
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Account { get; set; } = "";
        public string Password { get; set; } = "";
        public string DisplayName { get; set; } = "MailSender App";
        public int TimeoutMs { get; set; } = 15000;
    }

    public class SmtpNetworkService
    {
        /// <summary>
        /// Kiểm tra tầng Socket TCP và xác thực tài khoản SMTP thật
        /// </summary>
        public async Task<(bool Success, string Message)> TestConnectionAsync(SmtpConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Host))
                return (false, "Địa chỉ SMTP Host không được để trống.");

            if (config.Port <= 0 || config.Port > 65535)
                return (false, "Cổng SMTP Port không hợp lệ (1 - 65535).");

            // 1. Kiểm tra Socket TCP Ping trước
            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(config.Host, config.Port);
                var timeoutTask = Task.Delay(config.TimeoutMs);

                var completed = await Task.WhenAny(connectTask, timeoutTask);
                if (completed == timeoutTask)
                {
                    return (false, $"Timeout: Không thể mở kết nối TCP tới {config.Host}:{config.Port} sau {config.TimeoutMs / 1000}s.");
                }
                await connectTask;
            }
            catch (SocketException ex)
            {
                return (false, $"Lỗi kết nối Socket TCP: {ex.Message} (Mã lỗi: {ex.SocketErrorCode})");
            }
            catch (Exception ex)
            {
                return (false, $"Không thể kết nối máy chủ: {ex.Message}");
            }

            // 2. Kiểm tra xác thực (nếu đã nhập account & password)
            if (string.IsNullOrWhiteSpace(config.Account) || string.IsNullOrWhiteSpace(config.Password))
            {
                return (true, $"Kết nối mạng TCP tới {config.Host}:{config.Port} thành công!\n(Chưa kiểm tra xác thực vì chưa nhập mật khẩu)");
            }

            try
            {
                using var client = CreateClient(config);
                using var msg = new MailMessage
                {
                    From = new MailAddress(config.Account, config.DisplayName, Encoding.UTF8),
                    Subject = "[TEST] Kiểm tra kết nối SMTP từ MailSender WPF",
                    Body = $"Kết nối SMTP tới {config.Host}:{config.Port} thành công lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}!",
                    BodyEncoding = Encoding.UTF8
                };
                msg.To.Add(config.Account);

                await client.SendMailAsync(msg);
                return (true, $"Kết nối và xác thực SMTP thành công!\nĐã gửi thư kiểm tra về chính hòm thư {config.Account}.");
            }
            catch (SmtpException ex)
            {
                string tip = "";
                if (config.Host.Contains("gmail", StringComparison.OrdinalIgnoreCase))
                {
                    tip = "\n\n💡 Lưu ý Gmail: Bạn cần bật 'Xác minh 2 bước' trong tài khoản Google và tạo 'Mật khẩu ứng dụng' (App Password 16 ký tự).";
                }
                return (false, $"Lỗi xác thực SMTP ({ex.StatusCode}): {ex.Message}{tip}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi gửi thư kiểm tra: {ex.Message}");
            }
        }

        /// <summary>
        /// Gửi 1 email thật qua giao thức SMTP (hỗ trợ To, CC, BCC, HTML, đính kèm file)
        /// </summary>
        public async Task SendMailAsync(
            SmtpConfig config,
            IEnumerable<string> toList,
            string? cc,
            string? bcc,
            string subject,
            string body,
            bool isHtml,
            IEnumerable<string>? attachmentPaths)
        {
            if (string.IsNullOrWhiteSpace(config.Account))
                throw new InvalidOperationException("Email người gửi chưa được cấu hình.");

            using var msg = new MailMessage
            {
                From = new MailAddress(config.Account, config.DisplayName, Encoding.UTF8),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            foreach (var to in toList)
            {
                string trimmed = to.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    msg.To.Add(trimmed);
            }

            if (!string.IsNullOrWhiteSpace(cc))
            {
                foreach (var c in cc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    msg.CC.Add(c.Trim());
            }

            if (!string.IsNullOrWhiteSpace(bcc))
            {
                foreach (var b in bcc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    msg.Bcc.Add(b.Trim());
            }

            if (attachmentPaths != null)
            {
                foreach (var path in attachmentPaths)
                {
                    if (File.Exists(path))
                        msg.Attachments.Add(new Attachment(path));
                }
            }

            using var client = CreateClient(config);
            await client.SendMailAsync(msg);
        }

        private static SmtpClient CreateClient(SmtpConfig config)
        {
            var client = new SmtpClient(config.Host, config.Port)
            {
                EnableSsl = config.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = config.TimeoutMs
            };

            if (!string.IsNullOrWhiteSpace(config.Account) && !string.IsNullOrWhiteSpace(config.Password))
            {
                client.Credentials = new NetworkCredential(config.Account, config.Password);
            }

            return client;
        }
    }
}

