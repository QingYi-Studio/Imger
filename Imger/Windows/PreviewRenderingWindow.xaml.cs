using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Imger.Windows
{
    /// <summary>
    /// PreviewRenderingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PreviewRenderingWindow : Window
    {
        private Point _lastMouse;            // 上一次鼠标位置（屏幕坐标）
        private bool _isPanning;             // 是否处于拖拽中
        private Matrix _startMatrix;         // 拖拽开始时的矩阵
        private const double MinScale = 0.05; // 最小缩放
        private const double MaxScale = 10000.0; // 最大缩放

        public PreviewRenderingWindow(string imageSource)
        {
            InitializeComponent();
            Host.MouseWheel += OnMouseWheel;
            Host.MouseLeftButtonDown += OnMouseLeftButtonDown;
            Host.MouseLeftButtonUp += OnMouseLeftButtonUp;
            Host.MouseMove += OnMouseMove;
            Host.MouseLeave += (_, __) => EndPan();
            Host.MouseRightButtonDown += (_, __) => { if (Mouse.LeftButton != MouseButtonState.Pressed) ResetView(); };

            if (File.Exists(imageSource))
            {
                Img.Source = LoadBitmapHighQuality(imageSource);

                // 关键：第一次布局完成后再自适应
                Loaded += (_, __) => FitToView();
                SizeChanged += (_, __) => { if (!_isPanning) FitToView(); };
            }
            else
            {
                MessageBox.Show("The image was not found. Please modify the path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        // 高质量加载：一次性读取文件以避免文件锁，并保留 EXIF 方向
        private static BitmapSource LoadBitmapHighQuality(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BitmapSource src = decoder.Frames[0];
            src.Freeze();
            return src;
        }

        // 将图片自适应窗口并居中
        private void FitToView()
        {
            if (Img.Source == null || Host.ActualWidth <= 0 || Host.ActualHeight <= 0)
                return;

            double imgW = Img.Source.Width;  // 图片的宽度
            double imgH = Img.Source.Height; // 图片的高度
            double viewW = Host.ActualWidth; // 容器的宽度
            double viewH = Host.ActualHeight; // 容器的高度

            // 计算适配比例，确保图片最大不超出容器
            double scaleX = viewW / imgW;
            double scaleY = viewH / imgH;

            // 选择适应容器的最小缩放比例，避免图片过大导致超出容器
            double scale = Math.Min(scaleX, scaleY);

            // 限制缩放比例在设定范围内
            scale = Clamp(scale, MinScale, MaxScale);

            // 计算居中偏移量
            double tx = (viewW - imgW * scale) / 2.0;
            double ty = (viewH - imgH * scale) / 2.0;

            // 创建新的变换矩阵
            Matrix m = Matrix.Identity;
            m.ScaleAt(scale, scale, 0, 0);  // 缩放
            m.Translate(tx, ty);            // 平移至居中

            // 应用变换矩阵
            ViewTransform.Matrix = m;
        }

        // 修正滚轮缩放方向和确保图片不裁切
        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Img.Source == null) return;

            // 向上滚放大，向下滚缩小
            double zoom = e.Delta > 0 ? 1.25 : 1.0 / 1.25;

            // Ctrl 键时步长更大
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                zoom = e.Delta > 0 ? 2.0 : 0.5;

            Matrix m = ViewTransform.Matrix;
            double currentScale = m.M11;
            double targetScale = Clamp(currentScale * zoom, MinScale, MaxScale);

            if (Math.Abs(targetScale - currentScale) < 1e-6) return;

            Point cursor = e.GetPosition(Host);   // 以鼠标位置为锚点缩放
            m.ScaleAt(targetScale / currentScale,
                       targetScale / currentScale,
                       cursor.X, cursor.Y);

            ViewTransform.Matrix = m;

            // 提高清晰度
            Img.CacheMode = new BitmapCache { RenderAtScale = targetScale };
        }

        // 左键拖拽开始
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Img.Source == null) return;

            _isPanning = true;
            _lastMouse = e.GetPosition(Host);
            _startMatrix = ViewTransform.Matrix;
            Host.CaptureMouse();
            Mouse.OverrideCursor = Cursors.Hand;
        }

        // 拖拽中
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;

            Point now = e.GetPosition(Host);
            Vector nowV = now - _lastMouse;

            // 在屏幕坐标系平移（直接对矩阵追加Translate）
            Matrix m = _startMatrix;
            m.Translate(nowV.X, nowV.Y);

            ViewTransform.Matrix = m; // 应用平移
        }

        // 左键拖拽结束
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => EndPan();

        // 结束拖拽
        private void EndPan()
        {
            if (!_isPanning) return;

            _isPanning = false;
            Host.ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
        }

        // 重置视图（右键单击）
        private void ResetView() => FitToView();

        // 限制缩放比例
        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
    }
}

