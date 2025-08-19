using Imger.Windows;
using Imger.Pages.OptimizationControlPanel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Imger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PreviewRenderingWindow? previewRenderingWindow;
        private MainOcpPage? mainOptimizationControlPanelPage;
        private RealESRGANPage? realESRGANPage;
        private string? renderingImagePath;

        public MainWindow()
        {
            InitializeComponent();

            UiInit();
        }

        private void UiInit()
        {
            //MainW.Title = MainW.Title + $" - Version {versionAttribute.InformationalVersion}";
            mainOptimizationControlPanelPage = new MainOcpPage();
            OptimizationControlPanelFrame.Navigate(mainOptimizationControlPanelPage);
        }

        public void ChangeControlPanel(string mode)
        {
            switch (mode)
            {
                case "real-esrgan":
                    realESRGANPage = new RealESRGANPage();
                    OptimizationControlPanelFrame.Navigate(realESRGANPage);
                    break;
            }
        }

        private static readonly HashSet<string> ValidExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",".jpeg",".jpe",".jfif",".jif",
            ".png",".gif",".tif",".tiff",
            ".bmp",".dib",".webp",".ico"
        };

        private void DropZone_PreviewDragOver(object sender, DragEventArgs e)
        {
            // 只要有文件，就先给 Copy 光标，避免系统把 Drop 拦掉
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void DropZone_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                MessageBox.Show("Dropped content is not a file",
                    "Invalid Content", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0)
            {
                MessageBox.Show("No files were dropped.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string? imagePath = files.FirstOrDefault(file =>
            {
                string ext = Path.GetExtension(file);
                return !string.IsNullOrEmpty(ext) && ValidExtensions.Contains(ext);
            });

            renderingImagePath = imagePath;

            if (string.IsNullOrEmpty(imagePath))
            {
                MessageBox.Show("No valid image file found.\nSupported formats: " +
                    "JPEG, PNG, GIF, TIFF, BMP, WEBP, ICO",
                    "Invalid Format", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // 释放文件占用
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();
                bitmap.Freeze(); // 可选：跨线程/提高性能

                PreviewRendering.Source = bitmap;

                if (previewRenderingWindow != null)
                {
                    previewRenderingWindow.Close();
                    previewRenderingWindow = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image:\n{ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DropZone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (renderingImagePath == null) return;

            // 2. 如果窗口已存在且未关闭，只激活它
            if (previewRenderingWindow != null)
            {
                if (previewRenderingWindow.WindowState == WindowState.Minimized)
                    previewRenderingWindow.WindowState = WindowState.Normal;

                previewRenderingWindow.Activate();   // 把窗口提到最前并聚焦
                return;
            }

            // 3. 否则创建新窗口
            previewRenderingWindow = new PreviewRenderingWindow(renderingImagePath);

            // 4. 在窗口关闭时把引用置空，防止内存泄漏
            previewRenderingWindow.Closed += (_, __) => previewRenderingWindow = null;

            previewRenderingWindow.Show();
        }

        private void MainW_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            previewRenderingWindow?.Close();
        }
    }
}