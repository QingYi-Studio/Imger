using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Imger.Windows
{
    public partial class FileFolderPickerWindow : Window
    {
        public string? SelectedPath { get; private set; }
        public bool IsFile { get; private set; }

        public FileFolderPickerWindow()
        {
            InitializeComponent();
            LoadRootFolders();
        }

        // 初始化根目录
        private void LoadRootFolders()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                var node = CreateFolderNode(drive.RootDirectory);
                FolderTree.Items.Add(node);
            }
        }

        // 创建树节点(文件夹)
        private TreeViewItem CreateFolderNode(DirectoryInfo dirInfo)
        {
            var item = new TreeViewItem
            {
                Header = dirInfo.Name,
                Tag = dirInfo.FullName
            };

            // 占位子项，用于延迟加载
            item.Items.Add(null);
            item.Expanded += Folder_Expanded;
            return item;
        }

        // 展开树节点时加载子目录
        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();
                try
                {
                    var dirPath = item.Tag.ToString();
                    var dirs = new DirectoryInfo(dirPath!).GetDirectories();
                    foreach (var dir in dirs)
                    {
                        if ((dir.Attributes & FileAttributes.Hidden) == 0)
                            item.Items.Add(CreateFolderNode(dir));
                    }
                }
                catch { }
            }
        }

        // TreeView选择更改
        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FolderTree.SelectedItem is TreeViewItem item)
            {
                var path = item.Tag.ToString();
                PopulateFileList(path!);
            }
        }

        // 刷新右侧文件列表
        private void PopulateFileList(string folderPath)
        {
            var items = new ObservableCollection<FileItem>();

            // 文件夹
            try
            {
                foreach (var dir in new DirectoryInfo(folderPath).GetDirectories())
                {
                    if ((dir.Attributes & FileAttributes.Hidden) == 0)
                        items.Add(new FileItem
                        {
                            Name = dir.Name,
                            FullPath = dir.FullName,
                            Type = "文件夹",
                            DateModified = dir.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                        });
                }

                // 文件
                foreach (var file in new DirectoryInfo(folderPath).GetFiles())
                {
                    items.Add(new FileItem
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        Type = file.Extension,
                        DateModified = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }
            catch { /* 忽略受保护路径等异常 */ }
            FileList.ItemsSource = items;
        }

        // 双击选中
        private void FileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (FileList.SelectedItem is FileItem item)
            {
                SelectedPath = item.FullPath;
                IsFile = (item.Type != "文件夹");
                DialogResult = true;
            }
        }

        // 点击确定按钮
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // 可选择列表或左侧树中的文件夹
            if (FileList.SelectedItem is FileItem fileItem)
            {
                SelectedPath = fileItem.FullPath;
                IsFile = fileItem.Type != "文件夹";
                DialogResult = true;
            }
            else if (FolderTree.SelectedItem is TreeViewItem treeItem)
            {
                SelectedPath = treeItem.Tag.ToString();
                IsFile = false;
                DialogResult = true;
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void JumpButton_Click(object sender, RoutedEventArgs e)
        {
            var path = PathInputBox.Text.Trim();
            if (Directory.Exists(path))
            {
                PopulateFileList(path);

                if (ExpandTreeToPath(path))
                {
                    // 自动选中展开的TreeViewItem
                    SelectTreeItemByPath(path);
                }
            }
            else
            {
                MessageBox.Show("The directory does not exist or cannot be accessed!", "Failed to navigate.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 展开目录树到指定路径
        private bool ExpandTreeToPath(string path)
        {
            foreach (TreeViewItem rootItem in FolderTree.Items)
            {
                if (TryExpandTreeItem(rootItem, path))
                    return true;
            }
            return false;
        }

        private bool TryExpandTreeItem(TreeViewItem item, string targetPath)
        {
            string? itemPath = item.Tag as string;

            if (string.Equals(itemPath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                item.IsSelected = true;
                item.IsExpanded = true;
                item.BringIntoView();
                return true;
            }

            if (targetPath.StartsWith(itemPath!, StringComparison.OrdinalIgnoreCase))
            {
                item.IsExpanded = true;
                Folder_Expanded(item, null!);

                foreach (var child in item.Items)
                {
                    if (child is TreeViewItem treeViewChild)
                    {
                        if (TryExpandTreeItem(treeViewChild, targetPath))
                            return true;
                    }
                }
            }
            return false;
        }

        private void SelectTreeItemByPath(string path)
        {
            foreach (TreeViewItem rootItem in FolderTree.Items)
            {
                TreeViewItem found = FindTreeItemByPath(rootItem, path);
                if (found != null)
                {
                    found.IsSelected = true;
                    found.BringIntoView();
                    break;
                }
            }
        }

        private TreeViewItem FindTreeItemByPath(TreeViewItem item, string path)
        {
            if (item?.Tag as string == path)
                return item;

            if (item?.Items == null)
                return null!;

            foreach (var child in item.Items)
            {
                if (child is TreeViewItem treeViewChild)
                {
                    var result = FindTreeItemByPath(treeViewChild, path);
                    if (result != null)
                        return result;
                }
            }
            return null!;
        }
    }

    // 文件/文件夹数据模型
    public class FileItem
    {
        public string? Name { get; set; }
        public string? FullPath { get; set; }
        public string? Type { get; set; }
        public string? DateModified { get; set; }
    }
}
