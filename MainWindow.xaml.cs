using HuaZi.Library.Downloader;
using Markdig;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Documents;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace UpdateAPI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private string[] args = Environment.GetCommandLineArgs();
        private string Websitepath;
        private string Outputpath;
        private string StartApplicationName;
        public MainWindow()
        {
            if(args.Count() != 4)
            {
                MessageBox.Show("启动更新程序失败，请依照方法正确给参！","错误",System.Windows.MessageBoxButton.OK,MessageBoxImage.Error);
                Environment.Exit(0);
            }
            Websitepath = args[0];
            Outputpath = args[1];
            StartApplicationName = args[3];
            InitializeComponent();
            


        }

        private async void FluentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            try
            {
                log.Text += "尝试启动下载……\r\n";
                bool DownloadStatus = false;
                if (File.Exists(Path.GetTempPath() + "\\Temp.zip"))
                {
                    File.Delete(Path.GetTempPath() + "\\Temp.zip");
                }
                var downloader = new Downloader
                {
                    Url = Websitepath,
                    SavePath = Path.GetTempPath() + "\\Temp.zip",
                    Completed = ((s, e) =>
                    {
                        if (s)
                        {

                            Thread.Sleep(TimeSpan.FromMilliseconds(500));

                            DownloadStatus = true;

                        }
                        else
                        {
                            MessageBox.Show($"下载失败！错误为:{e}", "错误", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }),
                    Progress = ((p, s) =>
                    {
                        //Console.WriteLine($"目前进度:{(int)p}%");
                        progressbar.Value = ((int)p) * 0.9;
                        log.Text += $"当前下载进度：{(((int)p)).ToString()}%\r\n";
                    })
                };
                downloader.StartDownload();
                while (DownloadStatus == false)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }

                log.Text += "下载已完成。\r\n";
                log.Text += "正在尝试解压资源文件……\r\n";
                await ZipFile.ExtractToDirectoryAsync(Path.GetTempPath() + "\\Temp.zip", Outputpath, true);
                log.Text += "解压已完成，即将退出程序\r\n";
                progressbar.Value = 100;
                await Task.Delay(50);
                Process.Start(new ProcessStartInfo
                {
                    FileName = StartApplicationName,
                    UseShellExecute = true
                });
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载错误：{ex.Message}","错误",System.Windows.MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }
    }
}