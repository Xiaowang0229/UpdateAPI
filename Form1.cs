using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using System.IO.Compression;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace UpdateAPILite
{
    public partial class Updater : Form
    {
        private string Websitepath;
        private string Outputpath;
        private string StartApplicationName;
        public Updater()
        {
            InitializeComponent();
            var args = Environment.GetCommandLineArgs();
            if (args.Length != 3)
            {
                MessageBox.Show($"应依照如下方法给参: UpdateAPI.exe \"<Websitepath>\" \"<Outputpath>\",你的参数个数是{args.Length}个", "Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                Environment.Exit(0);
            }
            else
            {
                Websitepath = args[1];
                Outputpath = Path.GetDirectoryName(args[2]);
                StartApplicationName = args[2];
            }
        }

        private async void Updater_Load(object sender, EventArgs e)
        {
            try
            {
                var filname = Others.RandomHashGenerate();
                UpdateStatus.Text = "尝试启动下载";
                bool DownloadStatus = false;
                if (File.Exists(Path.GetTempPath() + "\\" + filname + ".zip"))
                {
                    File.Delete(Path.GetTempPath() + "\\" + filname + ".zip");
                }
                try
                {

                    var downloader = new Downloader
                    {
                        Url = Websitepath,
                        SavePath = Path.GetTempPath() + "\\" + filname + ".zip",
                        Completed = (async (s, e) =>
                        {
                            if (s)
                            {

                                UpdateStatus.Text = "下载已完成";
                                UpdateStatus.Text = "正在尝试解压资源文件";

                                DownloadStatus = true;

                            }
                            else
                            {
                                throw new Exception($"下载失败！错误为: {e}");

                                //Environment.Exit(0);
                            }
                        }),
                        Progress = ((p, s) =>
                        {
                            //Console.WriteLine($"目前进度:{(int)p}%");
                            StatusProgressuint.Value = Convert.ToInt32(p * 0.45);
                            StatusProgressText.Text = Convert.ToInt32((((int)p) * 0.45)).ToString() + "%";

                        })
                    };
                    downloader.StartDownload();
                    UpdateStatus.Text = "下载启动成功，请稍候";
                    while (DownloadStatus == false)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }


                    try
                    {
                        var progress = new Progress<(int p, string? f)>(t =>
                        {
                            if (InvokeRequired)
                            {
                                Invoke(() =>
                                {
                                    StatusProgressuint.Value = Convert.ToInt32((int)(t.p * 0.45 + 45));
                                    StatusProgressText.Text = $"{Convert.ToInt32((int)(t.p * 0.45 + 45))}%";
                                });
                            }
                            else
                            {
                                StatusProgressuint.Value = Convert.ToInt32((int)(t.p * 0.45 + 45));
                                StatusProgressText.Text = $"{Convert.ToInt32((int)(t.p * 0.45 + 45))}%";
                            }
                        });

                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(2));
                            //ZipFile.ExtractToDirectory(, Outputpath, true);
                            await IOHelper.ExtractZipAsync(
                                zipFilePath: Path.GetTempPath() + "\\" + filname + ".zip",
                                destinationDirectory: Outputpath,
                                progress: progress,
                                overwrite: true                    // 是否覆盖已有文件
                            );


                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"解压失败！错误为: {ex}");
                        }

                        await Task.Delay(TimeSpan.FromSeconds(2));
                        UpdateStatus.Text = "解压已完成，正在进行清理";
                        var progress2 = new Progress<(int p, string? f)>(t =>
                        {
                            if (InvokeRequired)
                            {
                                Invoke(() =>
                                {
                                    StatusProgressuint.Value = Convert.ToInt32((int)(t.p * 0.1 + 90));
                                    StatusProgressText.Text = $"{Convert.ToInt32((int)(t.p * 0.1 + 90))}%";
                                });
                            }
                            else
                            {
                                StatusProgressuint.Value = Convert.ToInt32((int)(t.p * 0.1 + 90));
                                StatusProgressText.Text = $"{Convert.ToInt32((int)(t.p * 0.1 + 90))}%";
                            }
                        });

                        try
                        {
                            await IOHelper.DeleteFileAsync(
                filePath: Path.GetTempPath() + "\\" + filname + ".zip",
                progress: progress
            );
                        }
                        catch (IOException ex)
                        {
                            throw new Exception($"清理失败！错误为: {ex}");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"解压失败！错误为: {e}");
                    }
                    StatusProgressText.Text = "100%";
                    StatusProgressuint.Value = 100;
                    await Task.Delay(100);

                    var stdinfo = new ProcessStartInfo
                    {
                        FileName = StartApplicationName,
                        WorkingDirectory = Outputpath,
                        UseShellExecute = true
                    };
                    Process.Start(stdinfo);
                    await Task.Delay(100);
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    throw new Exception($"操作失败！错误为: {ex}");
                }
            }
            catch(Exception ex)
            {
                throw new Exception($"操作失败！错误为: {ex}");
            }
        }
    }
}
