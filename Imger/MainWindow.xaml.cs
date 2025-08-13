using Imger.Pages.OptimizationControlPanel;
using System;
using System.Reflection;
using System.Windows;

namespace Imger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainOcpPage mainOptimizationControlPanelPage;

        public MainWindow()
        {
            InitializeComponent();

            UiInit();
        }

        private void UiInit()
        {
            //MainW.Title = MainW.Title + $" - Version {versionAttribute.InformationalVersion}";
            mainOptimizationControlPanelPage = new MainOcpPage();
            mainOptimizationControlPanelPage.InitializeComponent();
        }

        private void PreviewRendering_Drop(object sender, DragEventArgs e)
        {
            // Get the file data and render it in PreviewRendering control
        }
    }
}