using Imger.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Imger.Pages.OptimizationControlPanel
{
    /// <summary>
    /// MainOcpPage.xaml 的交互逻辑
    /// </summary>
    public partial class RealESRGANPage : Page
    {
        #region Usage
        /*
            Usage: realesrgan-ncnn-vulkan -i infile -o outfile [options]...

                -h                   show this help
                -i input-path        input image path (jpg/png/webp) or directory
                -o output-path       output image path (jpg/png/webp) or directory
                -s scale             upscale ratio (can be 2, 3, 4. default=4)
                -t tile-size         tile size (>=32/0=auto, default=0) can be 0,0,0 for multi-gpu
                -m model-path        folder path to the pre-trained models. default=models
                -n model-name        model name (default=realesr-animevideov3, can be realesr-animevideov3 | realesrgan-x4plus | realesrgan-x4plus-anime | realesrnet-x4plus)
                -g gpu-id            gpu device to use (default=auto) can be 0,1,2 for multi-gpu
                -j load:proc:save    thread count for load/proc/save (default=1:2:2) can be 1:2,2,2:2 for multi-gpu
                -x                   enable tta mode
                -f format            output image format (jpg/png/webp, default=ext/png)
                -v                   verbose output
         */
        #endregion

        private FileFolderPickerWindow? _picker;
        public RealESRGANPage()
        {
            InitializeComponent();
            _ = LoadGPUListAsync();
        }

        // 需要跳过的GPU
        private static readonly string[] SkipGPUs = ["Oray"];

        private async Task LoadGPUListAsync()
        {
            try
            {
                GPUCombobox.Items.Add(new ComboBoxItem { Content = "Loading GPUs…" });
                GPUCombobox.IsEnabled = false;

                var gpuNames = await Task.Run(() =>
                {
                    var list = new List<string>();

                    using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");

                    foreach (var m in searcher.Get())
                    {
                        var name = m["Name"]?.ToString() ?? "Unknown GPU";

                        // 只要包含数组里任意关键字就跳过
                        if (SkipGPUs.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        list.Add(name);
                    }
                    return list;
                });

                GPUCombobox.Items.Clear();

                if (gpuNames.Count == 0)
                {
                    //GPUCombobox.Items.Add(new ComboBoxItem { Content = "No GPU found" });
                    MessageBox.Show("No GPU found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    gpuNames.ForEach(n => GPUCombobox.Items.Add(new ComboBoxItem { Content = n }));
                    GPUCombobox.Items.Add(new ComboBoxItem { Content = "Default (Multi-GPU)" });
                }

                GPUCombobox.IsEnabled = true;
                GPUCombobox.SelectedIndex = 0;
            }
            catch
            {
                GPUCombobox.Items.Clear();
                //GPUCombobox.Items.Add(new ComboBoxItem { Content = "Error loading GPUs" });
                MessageBox.Show("Error loading GPUs", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                GPUCombobox.IsEnabled = true;
            }
        }

        private void SelectInputPathBtn_Click(object sender, RoutedEventArgs e) => InputPathTextBox.Text = Select();

        private void SelectOutputPathBtn_Click(object sender, RoutedEventArgs e) => OutputPathTextBox.Text = Select();

        private string Select()
        {
            try
            {
                _picker = new FileFolderPickerWindow();
                if (_picker.ShowDialog() == true)
                {
                    string path = _picker.SelectedPath!;
                    //bool isFile = dlg.IsFile;
                    return path;
                }
                return "";
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Select error", MessageBoxButton.OK, MessageBoxImage.Error);
                _picker!.Close();
                return "";
            }
            finally
            {
                _picker?.Close(); // 确保关闭
                _picker = null;
                // 强制垃圾回收，释放窗口资源
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
