using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KRPackToolGUI;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<string> _selectedFiles = new();

    private List<Rho5Result> _results = new();

    /// <summary>All flat lines ("<c>DataPack2_00000.rho5\aaa\bbb.xml</c>"), unfiltered.</summary>
    private List<string> _flatAll = new();

    /// <summary>Currently displayed (filtered) subset of <see cref="_flatAll"/>.</summary>
    private List<string> _flatView = new();

    public MainWindow()
    {
        InitializeComponent();
        FilesList.ItemsSource = _selectedFiles;
    }

    // ---- file list management -------------------------------------------------

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "選擇 rho5 檔案",
            Filter = "rho5 檔案 (*.rho5)|*.rho5|所有檔案 (*.*)|*.*",
            Multiselect = true
        };
        if (dlg.ShowDialog(this) == true)
            AddPaths(dlg.FileNames);
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        var added = 0;
        foreach (var path in paths)
        {
            if (_selectedFiles.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase)))
                continue;
            _selectedFiles.Add(path);
            added++;
        }
        if (added > 0)
            UpdateStatus($"已加入 {added} 個檔案，共 {_selectedFiles.Count} 個。按「解析」開始。");
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in FilesList.SelectedItems.Cast<string>().ToList())
            _selectedFiles.Remove(item);
    }

    private void ClearFiles_Click(object sender, RoutedEventArgs e)
    {
        _selectedFiles.Clear();
        _results = new();
        _flatAll = new();
        _flatView = new();
        StructureTree.Items.Clear();
        FlatList.ItemsSource = null;
        FlatCount.Text = string.Empty;
        UpdateStatus("已清除。");
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        AddPaths(paths.Where(p => p.EndsWith(".rho5", StringComparison.OrdinalIgnoreCase)));
    }

    // ---- parsing -----------------------------------------------------------

    private async void Parse_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFiles.Count == 0)
        {
            MessageBox.Show(this, "請先新增至少一個 rho5 檔案。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var files = _selectedFiles.ToList();
        var regionChoice = (RegionBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "AUTO";

        SetBusy(true);
        UpdateStatus($"解析 {files.Count} 個 rho5 檔案中…");
        try
        {
            var results = await Task.Run(
                () => files.Select(f => Rho5Scanner.Scan(f, regionChoice)).ToList());

            _results = results;
            BuildTree(results);
            BuildFlat(results);

            var totalFiles = results.Sum(r => r.InternalPaths.Count);
            var ok = results.Count(r => r.Error is null);
            var failed = results.Count - ok;
            var msg = $"完成：{ok}/{results.Count} 個 rho5，共 {totalFiles} 個內部檔案";
            if (failed > 0)
                msg += $"　（{failed} 個失敗，見樹狀結構的紅色節點）";
            UpdateStatus(msg);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "解析發生非預期錯誤",
                MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("解析失敗。");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ---- tree view ---------------------------------------------------------

    private void BuildTree(IReadOnlyList<Rho5Result> results)
    {
        StructureTree.Items.Clear();
        var expandRoots = results.Count == 1;

        foreach (var r in results)
        {
            var root = new TreeViewItem { IsExpanded = expandRoots };

            if (r.Error is not null)
            {
                root.Header = $"⚠ {r.FileName} — {r.Error}";
                root.Foreground = Brushes.Firebrick;
                StructureTree.Items.Add(root);
                continue;
            }

            root.Header = $"{r.FileName}  ({r.InternalPaths.Count} 個檔案 · 地區 {r.Region})";

            var tree = new DirNode();
            foreach (var path in r.InternalPaths)
                tree.Add(path.Split('/', StringSplitOptions.RemoveEmptyEntries));
            tree.Emit(root);

            StructureTree.Items.Add(root);
        }
    }

    /// <summary>Builds a folder/file tree from split path segments.</summary>
    private sealed class DirNode
    {
        private readonly SortedDictionary<string, DirNode> _dirs = new(StringComparer.OrdinalIgnoreCase);
        private readonly SortedSet<string> _files = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string[] parts, int index = 0)
        {
            if (parts.Length == 0)
                return;
            if (index == parts.Length - 1)
            {
                _files.Add(parts[index]);
                return;
            }
            if (!_dirs.TryGetValue(parts[index], out var child))
                _dirs[parts[index]] = child = new DirNode();
            child.Add(parts, index + 1);
        }

        public void Emit(ItemsControl parent)
        {
            foreach (var (name, node) in _dirs)
            {
                var item = new TreeViewItem { Header = "📁 " + name };
                node.Emit(item);
                parent.Items.Add(item);
            }
            foreach (var file in _files)
                parent.Items.Add(new TreeViewItem { Header = "📄 " + file });
        }
    }

    // ---- flat list -------------------------------------------------------

    private void BuildFlat(IEnumerable<Rho5Result> results)
    {
        _flatAll = new List<string>();
        foreach (var r in results)
        {
            if (r.Error is not null)
                continue;
            foreach (var path in r.InternalPaths)
                _flatAll.Add(r.FileName + "\\" + path.Replace('/', '\\'));
        }
        ApplyFilter();
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = FilterBox.Text?.Trim();
        _flatView = string.IsNullOrEmpty(query)
            ? _flatAll
            : _flatAll.Where(s => s.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        FlatList.ItemsSource = _flatView;
        FlatCount.Text = _flatAll.Count == 0
            ? string.Empty
            : $"顯示 {_flatView.Count} / {_flatAll.Count} 列";
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var lines = FlatList.SelectedItems.Count > 0
            ? FlatList.SelectedItems.Cast<string>()
            : _flatView;
        var text = string.Join(Environment.NewLine, lines);
        if (text.Length == 0)
            return;

        try
        {
            Clipboard.SetText(text);
            UpdateStatus("已複製到剪貼簿。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "複製失敗：" + ex.Message, "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_flatView.Count == 0)
        {
            MessageBox.Show(this, "沒有可匯出的內容，請先解析。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "匯出檔案清單",
            Filter = "文字檔 (*.txt)|*.txt|所有檔案 (*.*)|*.*",
            FileName = "rho5-structure.txt"
        };
        if (dlg.ShowDialog(this) != true)
            return;

        try
        {
            File.WriteAllText(dlg.FileName,
                string.Join(Environment.NewLine, _flatView), new UTF8Encoding(true));
            UpdateStatus($"已匯出 {_flatView.Count} 列至 {dlg.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "匯出失敗：" + ex.Message, "錯誤",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- misc -----------------------------------------------------------

    private void SetBusy(bool busy)
    {
        AddBtn.IsEnabled = !busy;
        RemoveBtn.IsEnabled = !busy;
        ClearBtn.IsEnabled = !busy;
        ParseBtn.IsEnabled = !busy;
        RegionBox.IsEnabled = !busy;
        Busy.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        Busy.IsIndeterminate = busy;
        Cursor = busy ? Cursors.Wait : null;
    }

    private void UpdateStatus(string text) => StatusText.Text = text;
}
