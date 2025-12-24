using System.Configuration;
using System.Data;
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
    }

}
