using Imger.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        // 获取 MainWindow 的实例
        private readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

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
                    GPUCombobox.IsEnabled = false;
                }
                else
                {
                    gpuNames.ForEach(n => GPUCombobox.Items.Add(new ComboBoxItem { Content = n }));
                    GPUCombobox.Items.Add(new ComboBoxItem { Content = "Default (Multi-GPU)" });
                    GPUCombobox.IsEnabled = true;
                    GPUCombobox.SelectedIndex = 0;
                }
            }
            catch
            {
                GPUCombobox.Items.Clear();
                //GPUCombobox.Items.Add(new ComboBoxItem { Content = "Error loading GPUs" });
                MessageBox.Show("Error loading GPUs", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                GPUCombobox.IsEnabled = false;
            }
        }

        private void SelectInputPathBtn_Click(object sender, RoutedEventArgs e)
        {
            InputPathTextBox.Text = Select(out bool isFile);
            if (isFile)
                mainWindow.RenderPicture(InputPathTextBox.Text);
        }

        private void SelectOutputPathBtn_Click(object sender, RoutedEventArgs e) => OutputPathTextBox.Text = Select(out _);

        private void BackButton_Click(object sender, RoutedEventArgs e) => mainWindow.ChangeControlPanel("default");

        private async void LaunchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CheckPath() == false)
                return;

            string precommand = "cd ./program/Real-ESRGAN/ && " +
                $"realesrgan-ncnn-vulkan.exe -i {InputPathTextBox.Text} " +
                                           $"-o {OutputPathTextBox.Text} " +
                                           $"-s {ScaleCombobox.Text} " +
                                           $"-t {GetTileSize()} " +
                                           $"-n {ModelCombobox.Text} " +
                                           $"-g {GetGPUId()} " +
                                           $"-j {CheckThread()} " +
                                           $"-f {GetFormat()} ";

            /*
            string tcommand;
            if (TTAModeCheckBox.IsChecked == true)
            {
                tcommand = precommand + "-x ";
            }
            else
            {
                tcommand = precommand;
            }
             */

            string tcommand = TTAModeCheckBox.IsChecked == true ? precommand + "-x " : precommand;

            string command = tcommand + "-v";

            tcommand = null!;
            precommand = null!;
#if DEBUG
            MessageBox.Show(command);
#endif
            try
            {
                // 清除之前的输出
                OutputTextBox.Clear();

                #region Process
                // 创建进程启动信息
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory =  Environment.CurrentDirectory
                };

                // 创建进程
                using var process = new Process();
                process.StartInfo = processStartInfo;

                // 设置输出数据接收处理程序
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        // 在UI线程上更新TextBox
                        Dispatcher.Invoke(() =>
                        {
                            OutputTextBox.AppendText(e.Data + Environment.NewLine);
                            OutputTextBox.ScrollToEnd();
                        });
                    }
                };

                // 设置错误数据接收处理程序
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        // 在UI线程上更新TextBox
                        Dispatcher.Invoke(() =>
                        {
                            OutputTextBox.AppendText(e.Data + Environment.NewLine);
                            OutputTextBox.ScrollToEnd();
                        });
                    }
                };

                // 启动进程
                process.Start();

                // 开始异步读取输出
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 等待进程退出
                await Task.Run(() => process.WaitForExit());

                // 显示退出代码
                Dispatcher.Invoke(() =>
                {
                    OutputTextBox.AppendText($"\nProcess exited with code: {process.ExitCode}");
                });
                #endregion

                if (File.Exists(OutputPathTextBox.Text))
                    mainWindow.RenderPicture(OutputPathTextBox.Text);
            }
            catch (Exception ex)
            {
                // 显示错误信息
                Dispatcher.Invoke(() =>
                {
                    OutputTextBox.AppendText($"Error executing command: {ex.Message}");
                });
            }
        }

        private bool CheckPath()
        {
            // 创建错误消息列表
            List<string> errors = [];

            // 同时检查输入路径
            if (string.IsNullOrWhiteSpace(InputPathTextBox.Text))
            {
                errors.Add("The input path cannot be empty.");
            }
            else
            {
                // 检查输入路径是否存在（可以是文件或目录）
                bool inputPathExists = File.Exists(InputPathTextBox.Text) ||
                                      Directory.Exists(InputPathTextBox.Text);

                if (!inputPathExists)
                {
                    errors.Add("The input path does not exist or cannot be accessed!");
                }
            }

            // 同时检查输出路径
            if (string.IsNullOrWhiteSpace(OutputPathTextBox.Text))
            {
                errors.Add("The output path cannot be empty.");
            }
            else
            {
                // 对于输出路径，如果指定的是文件，检查其目录是否存在
                // 如果指定的是目录，检查目录是否存在
                bool outputPathValid = false;

                try
                {
                    // 检查是否是文件路径（包含扩展名）
                    if (Path.HasExtension(OutputPathTextBox.Text))
                    {
                        // 如果是文件路径，检查其目录是否存在
                        string outputDirectory = Path.GetDirectoryName(OutputPathTextBox.Text)!;
                        outputPathValid = Directory.Exists(outputDirectory);

                        if (!outputPathValid)
                        {
                            errors.Add("The output directory for the specified file does not exist!");
                        }
                    }
                    else
                    {
                        // 如果是目录路径，检查目录是否存在
                        outputPathValid = Directory.Exists(OutputPathTextBox.Text);

                        if (!outputPathValid)
                        {
                            errors.Add("The output directory does not exist!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Invalid output path format: {ex.Message}");
                }
            }

            // 如果有错误，一次性显示所有错误
            if (errors.Count > 0)
            {
                string errorMessage = string.Join("\n\n", errors);
                MessageBox.Show(errorMessage, "Path Validation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private string GetTileSize()
        {
            Int128 iTileSize = Int128.Parse(TileSizeTextBox.Text);
            if (iTileSize < 32 && iTileSize != 0 || iTileSize < 0)
            {
                MessageBox.Show("The tile size needs to be larger than 32.\nThis time, automatic configuration will be used.", "Tile size too small", MessageBoxButton.OK, MessageBoxImage.Warning);
                return "0";
            }
            return iTileSize.ToString();
        }

        private string GetGPUId()
        {
            /*
            int index = GPUCombobox.SelectedIndex;
            string gpuName = GPUCombobox.Text;
            if (gpuName == "Default (Multi-GPU)" || !GPUCombobox.IsEnabled)
            {
                return "auto";
            }
            return index.ToString();
            */
            return GPUCombobox.Text == "Default (Multi-GPU)" || !GPUCombobox.IsEnabled ? "auto" : GPUCombobox.SelectedIndex.ToString();
        }

        private string CheckThread()
        {
            string input = ThreadTextBox.Text;
            string defaultValue = "1:2:2";

            // 检查空值
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            // 检查是否包含冒号
            if (input.Contains(':'))
            {
                // 分割字符串
                string[] parts = input.Split(':');

                // 检查是否有且只有三个部分
                if (parts.Length != 3)
                    return defaultValue;

                // 检查每个部分
                foreach (string part in parts)
                {
                    // 检查长度是否为1
                    if (part.Length != 1)
                        return defaultValue;

                    // 检查是否为数字1-9
                    if (!char.IsDigit(part[0]) || part[0] == '0')
                        return defaultValue;
                }

                return input; // 格式正确，返回原字符串
            }
            else
            {
                // 检查是否为单个数字1-9
                if (input.Length == 1 && char.IsDigit(input[0]) && input[0] != '0')
                    return input; // 格式正确，返回原字符串
                else
                    return defaultValue; // 格式不正确，返回默认值
            }
        }

        private string GetFormat()
        {
            int index = FormatCombobox.SelectedIndex;
            return index switch
            {
                0 => "png",
                1 => "jpg",
                2 => "webp",
                _ => "png",
            };
        }

        private string Select(out bool isFile)
        {
            try
            {
                _picker = new FileFolderPickerWindow();
                if (_picker.ShowDialog() == true)
                {
                    string path = _picker.SelectedPath!;
                    isFile = _picker.IsFile;
                    return path;
                }
                isFile = false;
                return "";
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Select error", MessageBoxButton.OK, MessageBoxImage.Error);
                _picker!.Close();
                isFile = false;
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
