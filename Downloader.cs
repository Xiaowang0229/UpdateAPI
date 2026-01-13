using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HuaZi.Library.Downloader;

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