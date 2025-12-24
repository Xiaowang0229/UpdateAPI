using HuaZi.Library.Downloader;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Documents;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;

namespace UpdateAPI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private string[] args;
        private string Websitepath;
        private string Outputpath;
        private string StartApplicationName;
        public MainWindow()
        {
            
            InitializeComponent();
            args = Environment.GetCommandLineArgs();
            if (args.Length < 3)
            {
                MessageBox.Show("应依照如下方法给参: UpdateAPI.exe <Websitepath> <Outputpath> <StartApplicationName>", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            Websitepath = args[1];
            Outputpath = Path.GetDirectoryName(args[2]);
            StartApplicationName = args[2];
            
        }

        private async void FluentWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
            log.Text += "尝试启动下载……\r\n";
            bool DownloadStatus = false;
            if (File.Exists(Path.GetTempPath() + "\\Temp.zip"))
            {
                File.Delete(Path.GetTempPath() + "\\Temp.zip");
            }
            try
            {
                
                var downloader = new Downloader
                {
                    Url = Websitepath,
                    SavePath = Path.GetTempPath() + "\\Temp.zip",
                    Completed = (async (s, e) =>
                    {
                        if (s)
                        {

                            log.Text += "下载已完成。\r\n";
                            log.Text += "正在尝试解压资源文件……\r\n";

                            DownloadStatus = true;

                        }
                        else
                        {
                            MessageBox.Show($"下载失败！错误为:{e}", "错误", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                            Environment.Exit(0);
                        }
                    }),
                    Progress = ((p, s) =>
                    {
                        //Console.WriteLine($"目前进度:{(int)p}%");
                        progressbar.Value = ((int)p) * 0.9;
                        progresstext.Text = "当前进度："+(((int)p) * 0.9).ToString() + "%";
                        
                    })
                };
                downloader.StartDownload();
                log.Text += "下载启动成功，请稍候……。\r\n";
                while (DownloadStatus == false)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }

                
                try
                {
                    await ZipFile.ExtractToDirectoryAsync(Path.GetTempPath() + "\\Temp.zip", Outputpath, true);
                    log.Text += "解压已完成，即将退出程序……\r\n";
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"解压错误：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }
                progresstext.Text = "当前进度：100%";
                progressbar.Value = 100;
                await Task.Delay(50);
                //MessageBox.Show(Outputpath + "\\" + StartApplicationName);
                
                App.SafeStart(StartApplicationName);
                await Task.Delay(500);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动时发生错误：{ex.Message}","错误",System.Windows.MessageBoxButton.OK,MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }
    }
}