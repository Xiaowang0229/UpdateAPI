using Ookii.Dialogs.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using TaskDialog = Ookii.Dialogs.WinForms.TaskDialog;
using TaskDialogButton = Ookii.Dialogs.WinForms.TaskDialogButton;

namespace UpdateAPILite
{
    public static class IOHelper
    {
        public static async Task ExtractZipAsync(
    string zipFilePath,
    string destinationDirectory,
    IProgress<(int Percent, string? CurrentFile)>? progress = null,
    bool overwrite = true,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(zipFilePath))
                throw new IOException("ZIP 文件路径不能为空");

            if (string.IsNullOrWhiteSpace(destinationDirectory))
                throw new IOException("目标文件夹路径不能为空");

            if (!File.Exists(zipFilePath))
                throw new IOException($"ZIP 文件不存在: {zipFilePath}");

            try
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            catch (Exception ex)
            {
                throw new IOException($"无法创建目标目录: {destinationDirectory}", ex);
            }

            long totalBytes = 0;
            long processedBytes = 0;
            int lastPercent = -1;

            try
            {
                using var archive = ZipFile.OpenRead(zipFilePath);

                // 计算总字节数（用于较精确的进度）
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (entry.Length > 0 && !entry.FullName.EndsWith("/"))
                    {
                        totalBytes += entry.Length;
                    }
                }

                // 空压缩包直接完成
                if (totalBytes == 0)
                {
                    progress?.Report((100, null));
                    return;
                }

                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // 目录条目
                    if (entry.FullName.EndsWith("/") || (entry.Length == 0 && string.IsNullOrEmpty(Path.GetFileName(entry.FullName))))
                    {
                        string dirPath = Path.Combine(destinationDirectory, entry.FullName);
                        try
                        {
                            Directory.CreateDirectory(dirPath);
                        }
                        catch (Exception ex)
                        {
                            throw new IOException($"创建目录失败: {entry.FullName}", ex);
                        }
                        continue;
                    }

                    string destPath = Path.Combine(destinationDirectory, entry.FullName);
                    string? parent = Path.GetDirectoryName(destPath);

                    if (!string.IsNullOrEmpty(parent))
                    {
                        try
                        {
                            Directory.CreateDirectory(parent);
                        }
                        catch (Exception ex)
                        {
                            throw new IOException($"创建父目录失败: {parent}", ex);
                        }
                    }

                    // 不覆盖时跳过
                    if (File.Exists(destPath) && !overwrite)
                    {
                        processedBytes += entry.Length;
                        continue;
                    }

                    progress?.Report((lastPercent, entry.FullName));

                    try
                    {
                        using var entryStream = entry.Open();
                        using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                        byte[] buffer = new byte[81920];
                        int read;

                        while ((read = await entryStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read, cancellationToken);
                            processedBytes += read;

                            int percent = (int)Math.Min(100, (processedBytes * 100L) / totalBytes);

                            if (percent != lastPercent)
                            {
                                progress?.Report((percent, entry.FullName));
                                lastPercent = percent;
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        throw new IOException($"写入文件失败: {entry.FullName}", ex);
                    }
                }

                // 强制报告 100%
                if (lastPercent < 100)
                {
                    progress?.Report((100, null));
                }
            }
            catch (InvalidDataException ex)
            {
                throw new IOException("ZIP 文件格式无效或已损坏", ex);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new IOException("ZIP 解压过程中发生错误", ex);
            }
        }

        public static async Task DeleteFileAsync(
    string filePath,
    IProgress<(int Percent, string? CurrentFile)>? progress = null,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new IOException("文件路径不能为空");

            if (!File.Exists(filePath))
                throw new IOException($"文件不存在: {filePath}");

            progress?.Report((0, filePath));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 可选：这里可以加一些预检查（如文件是否被占用），但通常 File.Delete 会抛出相应异常

                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Delete(filePath);
                }, cancellationToken).ConfigureAwait(false);

                progress?.Report((100, null));
            }
            catch (OperationCanceledException)
            {
                throw; // 保持原样抛出取消异常
            }
            catch (Exception ex)
            {
                throw new IOException($"删除文件失败: {filePath}", ex);
            }
        }
    }

    public class Downloader : IDisposable
    {
        private readonly HttpClient _httpClient;

        private CancellationTokenSource? _cts;

        private Task? _task;

        public string Url { get; init; } = "";

        public string SavePath { get; init; } = "";

        public Action<double, double>? Progress { get; init; }

        public Action<bool, string?>? Completed { get; init; }

        public int ReportIntervalMs { get; init; } = 200;

        public bool IgnoreSslErrors { get; init; }

        public Downloader()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate)
            };
            if (IgnoreSslErrors)
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            _httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        public void StartDownload()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                throw new InvalidOperationException("Url 未设置");
            }

            if (string.IsNullOrWhiteSpace(SavePath))
            {
                throw new InvalidOperationException("SavePath 未设置");
            }

            Task? task = _task;
            if (task != null && !task.IsCompleted)
            {
                throw new InvalidOperationException("已在下载");
            }

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _task = DownloadAsync(_cts.Token);
        }

        public void StopDownload()
        {
            _cts?.Cancel();
        }

        private async Task DownloadAsync(CancellationToken ct)
        {
            _ = 4;
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
                long total = response.Content.Headers.ContentLength ?? (-1);
                bool canReport = total > 0 && Progress != null;
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                await using FileStream file = new FileStream(SavePath, FileMode.Create, FileAccess.Write, FileShare.None, 32768, useAsync: true);
                using Stream stream = await response.Content.ReadAsStreamAsync(ct);
                byte[] buffer = new byte[32768];
                long readTotal = 0L;
                Stopwatch sw = Stopwatch.StartNew();
                long lastBytes = 0L;
                double lastTime = 0.0;
                while (true)
                {
                    int num;
                    int bytes = (num = await stream.ReadAsync(buffer, ct));
                    if (num <= 0)
                    {
                        break;
                    }

                    await file.WriteAsync(buffer.AsMemory(0, bytes), ct);
                    readTotal += bytes;
                    if (canReport && sw.Elapsed.TotalMilliseconds - lastTime >= (double)ReportIntervalMs)
                    {
                        double arg = (double)readTotal * 100.0 / (double)total;
                        double arg2 = (double)(readTotal - lastBytes) / ((sw.Elapsed.TotalMilliseconds - lastTime) / 1000.0) / 1024.0;
                        Progress?.Invoke(arg, arg2);
                        lastBytes = readTotal;
                        lastTime = sw.Elapsed.TotalMilliseconds;
                    }
                }

                Progress?.Invoke(100.0, 0.0);
                Completed?.Invoke(arg1: true, null);
            }
            catch (OperationCanceledException)
            {
                Completed?.Invoke(arg1: false, "已取消");
            }
            catch (Exception ex2)
            {
                Completed?.Invoke(arg1: false, ex2.Message);
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _httpClient.Dispose();
        }
    }
    
    public static class Others
    {
        public static string RandomHashGenerate(int byteLength = 16)
        {
            byte[] bytes = new byte[byteLength];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes);
        }
    
        public static void RegisterGlobalExceptionHandlers()
        {
            // 捕获 UI 线程未处理的异常
            System.Windows.Forms.Application.ThreadException += (s, e) =>
            {


                //MessageBox.Show($"发生错误：{e.Exception}","错误",MessageBoxButton.OK,MessageBoxImage.Error);
                var mb = new TaskDialog
                {
                    WindowTitle = "错误",
                    MainIcon = Ookii.Dialogs.WinForms.TaskDialogIcon.Error,
                    MainInstruction = "程序发生错误，您可将下方内容截图并上报错误",

                    Content = $"{e.Exception}",
                    ButtonStyle = TaskDialogButtonStyle.CommandLinks,


                };
                var mbb1 = new TaskDialogButton
                {

                    Text = "打开错误报告页面(推荐)",
                    CommandLinkNote = "将会自动复制错误信息到剪贴板,可能需要启动网络代理以进入Github",

                };
                mb.Buttons.Add(mbb1);
                var mbb2 = new TaskDialogButton
                {
                    Text = "退出程序",
                    CommandLinkNote = "退出程序以保证错误不再发生",

                };
                mb.Buttons.Add(mbb2);
                var mbb3 = new TaskDialogButton
                {
                    Text = "继续运行程序(不推荐)",
                    CommandLinkNote = "程序可能随时崩溃或内存泄漏",

                };
                mb.Buttons.Add(mbb3);
                var mbb4 = new TaskDialogButton
                {
                    ButtonType = ButtonType.Close
                };
                mb.Buttons.Add(mbb4);
                var res = mb.ShowDialog();
                if (res == mbb1)
                {
                    System.Windows.Forms.Clipboard.SetText($"{e.Exception}");
                    OpenIssue();
                    Environment.Exit(0);
                }
                else if (res == mbb2)
                {
                    Environment.Exit(0);
                }

            };

            // 捕获非 UI 线程未处理的异常
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {



                //MessageBox.Show($"发生错误：{e.ExceptionObject}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                var mb = new TaskDialog
                {
                    WindowTitle = "错误",
                    MainIcon = Ookii.Dialogs.WinForms.TaskDialogIcon.Error,
                    MainInstruction = "程序发生错误，您可将下方内容截图并上报错误",
                    Content = $"{e.ExceptionObject}",
                    ButtonStyle = TaskDialogButtonStyle.CommandLinks,


                };
                var mbb1 = new TaskDialogButton
                {

                    Text = "打开错误报告页面(推荐)",
                    CommandLinkNote = "将会自动复制错误信息到剪贴板,可能需要启动网络代理以进入Github",

                };
                mb.Buttons.Add(mbb1);
                var mbb2 = new TaskDialogButton
                {
                    Text = "退出程序",
                    CommandLinkNote = "退出程序以保证错误不再发生",

                };
                mb.Buttons.Add(mbb2);
                var mbb3 = new TaskDialogButton
                {
                    Text = "继续运行程序(不推荐)",
                    CommandLinkNote = "程序可能随时崩溃或内存泄漏",

                };
                mb.Buttons.Add(mbb3);
                var mbb4 = new TaskDialogButton
                {
                    ButtonType = ButtonType.Close
                };
                mb.Buttons.Add(mbb4);
                var res = mb.ShowDialog();
                if (res == mbb1)
                {
                    System.Windows.Forms.Clipboard.SetText($"{e.ExceptionObject}");
                    OpenIssue();
                    Environment.Exit(0);
                }
                else if (res == mbb2)
                {
                    Environment.Exit(0);
                }
                //KillTaskBar();
                //Environment.Exit(0);
            };

            // 捕获 Task 线程未处理的异常
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {


                var mb = new Ookii.Dialogs.WinForms.TaskDialog
                {
                    WindowTitle = "错误",
                    MainIcon = Ookii.Dialogs.WinForms.TaskDialogIcon.Error,
                    MainInstruction = "程序发生错误，您可将下方内容截图并上报错误",
                    Content = $"{e.Exception}",
                    ButtonStyle = TaskDialogButtonStyle.CommandLinks,


                };
                var mbb1 = new TaskDialogButton
                {

                    Text = "打开错误报告页面(推荐)",
                    CommandLinkNote = "将会自动复制错误信息到剪贴板,可能需要启动网络代理以进入Github",

                };
                mb.Buttons.Add(mbb1);
                var mbb2 = new TaskDialogButton
                {
                    Text = "退出程序",
                    CommandLinkNote = "退出程序以保证错误不再发生",

                };
                mb.Buttons.Add(mbb2);
                var mbb3 = new TaskDialogButton
                {
                    Text = "继续运行程序(不推荐)",
                    CommandLinkNote = "程序可能随时崩溃或内存泄漏",

                };
                mb.Buttons.Add(mbb3);
                var mbb4 = new TaskDialogButton
                {
                    ButtonType = ButtonType.Close
                };
                mb.Buttons.Add(mbb4);
                var res = mb.ShowDialog();
                if (res == mbb1)
                {
                    System.Windows.Forms.Clipboard.SetText($"{e.Exception}");
                    OpenIssue();
                    Environment.Exit(0);
                }
                else if (res == mbb2)
                {
                    Environment.Exit(0);
                }
            };
        }

        private static void OpenIssue()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Xiaowang0229/RocketLauncherRemake/issues/new",
                UseShellExecute = true
            });
        }
    }
}
