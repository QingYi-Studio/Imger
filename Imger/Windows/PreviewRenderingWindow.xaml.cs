using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Imger.Forms
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
        private const double MaxScale = 5000.0; // 最大缩放

        public PreviewRenderingWindow(string imageSource)
        {
            InitializeComponent();

            // 绑定鼠标事件（放在 Host 上，便于空白处也能响应）
            Host.MouseWheel += OnMouseWheel;
            Host.MouseLeftButtonDown += OnMouseLeftButtonDown;
            Host.MouseLeftButtonUp += OnMouseLeftButtonUp;
            Host.MouseMove += OnMouseMove;
            Host.MouseLeave += (_, __) => EndPan();
            Host.MouseRightButtonDown += (_, __) => { if (Mouse.LeftButton != MouseButtonState.Pressed) ResetView(); };

            if (File.Exists(imageSource))
            {
                Img.Source = LoadBitmapHighQuality(imageSource);
                // 窗口加载完成后自适应居中
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

        // 将图片按窗口大小等比缩放并居中（不放大超过 1x，保证清晰）
        private void FitToView()
        {
            if (Img.Source == null || Host.ActualWidth <= 0 || Host.ActualHeight <= 0) return;

            double imgW = Img.Source.Width;
            double imgH = Img.Source.Height;
            double viewW = Host.ActualWidth;
            double viewH = Host.ActualHeight;

            // 计算适配比例（不超过 1，避免无谓放大导致模糊；如需允许放大，可去掉 Math.Min 中的 1）
            double scale = Math.Min(1.0, Math.Min(viewW / imgW, viewH / imgH));
            scale = Clamp(scale, MinScale, MaxScale);

            // 使图像居中
            double tx = (viewW - imgW * scale) / 2.0;
            double ty = (viewH - imgH * scale) / 2.0;

            var m = Matrix.Identity;
            m.ScaleAt(scale, scale, 0, 0);
            m.Translate(tx, ty);
            ViewTransform.Matrix = m;
        }

        // 滚轮缩放（以光标为中心缩放）
        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Img.Source == null)
                return;

            // 缩放步进：Ctrl加速，Shift减速（可选）
            double step = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? 1.25 :
                          (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 1.07 : 1.15);
            double zoom = e.Delta > 0 ? step : 1.0 / step;

            // 当前矩阵
            Matrix m = ViewTransform.Matrix;

            // 当前缩放（假设无倾斜）
            double currentScale = m.M11;
            double targetScale = Clamp(currentScale * zoom, MinScale, MaxScale);

            // 若达到边界，按比例调整zoom，避免卡边
            zoom = targetScale / currentScale;
            if (Math.Abs(zoom - 1.0) < 1e-6)
                return;

            // 获取光标相对Img的坐标（以该点为锚点ScaleAt）
            Point cursor = e.GetPosition(Host);
            m.ScaleAt(zoom, zoom, cursor.X, cursor.Y);
            ViewTransform.Matrix = m;

            // 启用缓存，保持清晰
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

            // 在屏幕坐标系平移（直接对矩阵追加 Translate）
            Matrix m = _startMatrix;
            m.Translate(nowV.X,nowV.Y);
            ViewTransform.Matrix = m;
        }

        // 左键拖拽结束
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => EndPan();

        private void EndPan()
        {
            if (!_isPanning) return;
            _isPanning = false;
            Host.ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
        }

        // 重置视图（右键单击）
        private void ResetView() => FitToView();

        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
    }
}

