using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace UpdateAPI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Window window = new MainWindow();
            window.Show();
        }

        public static bool SafeStart(
        string exePath = null,
        string arguments = null,
        string workingDirectory = null,
        bool requireAdmin = false,
        params string[] parts)
        {
            try
            {
                // 1. 如果传了 parts 参数，就用 Path.Combine 安全拼接（优先级最高）
                if (parts != null && parts.Length > 0)
                {
                    exePath = Path.Combine(parts);
                }

                // 2. exePath 必须有值
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    MessageBox.Show("启动失败：exe路径为空！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // 3. 清理路径：去空格、去多余引号、统一斜杠
                exePath = exePath.Trim().Trim('"', '\'').Trim();

                // 4. 如果是相对路径，转成绝对路径（方便后续判断）
                if (!Path.IsPathRooted(exePath))
                {
                    // 假设相对于当前程序目录
                    exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exePath);
                }

                // 5. 关键：检查文件是否存在
                if (!File.Exists(exePath))
                {
                    MessageBox.Show(
                        $"启动失败：找不到可执行文件！\n路径：{exePath}\n\n请检查路径是否正确、磁盘是否挂载、文件是否被删除或移动。",
                        "文件不存在",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                // 6. 准备 ProcessStartInfo
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments?.Trim(),
                    UseShellExecute = true,                 // 强烈推荐 true，自动处理空格、引号、环境变量
                    Verb = requireAdmin ? "runas" : null    // 需要管理员时自动提升
                };

                // 7. 工作目录：优先用传入的，没有则用 exe 所在目录
                if (!string.IsNullOrWhiteSpace(workingDirectory))
                {
                    workingDirectory = workingDirectory.Trim().Trim('"', '\'');
                    if (Directory.Exists(workingDirectory))
                    {
                        psi.WorkingDirectory = workingDirectory;
                    }
                    else
                    {
                        MessageBox.Show($"警告：指定的工作目录不存在，将使用默认目录。\n{workingDirectory}", "工作目录无效", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    psi.WorkingDirectory = Path.GetDirectoryName(exePath);
                }

                // 8. 启动进程
                Process.Start(psi);
                return true;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // 常见：用户取消 UAC 提升、文件被占用等
                MessageBox.Show(
                    $"启动进程时发生系统错误：\n{ex.Message}\n\n可能原因：权限不足、文件被占用、用户取消了管理员提升。",
                    "Win32 异常",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败：{ex.Message}\n路径：{exePath}", "未知错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }

}
