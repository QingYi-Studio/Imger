using System.Windows;
using System.Windows.Controls;

namespace Imger.Pages.OptimizationControlPanel
{
    /// <summary>
    /// MainOcpPage.xaml 的交互逻辑
    /// </summary>
    public partial class MainOcpPage : Page
    {
        // 获取 MainWindow 的实例
        private readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        public MainOcpPage()
        {
            InitializeComponent();
        }

        private void RealESRGANBtn_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ChangeControlPanel("real-esrgan");
        }
    }
}
