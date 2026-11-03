@echo off
chcp 65001 > nul
title Khởi Chạy MailSender - D:\WORKSPACE\thu
echo ========================================================
echo   KHỞI ĐỘNG ỨNG DỤNG MAILSENDER (WPF .NET 8)
echo ========================================================
echo Đang mở ứng dụng...

start "" "%~dp0Untitled\MailSender\bin\Debug\net8.0-windows\MailSender.exe"

exit

