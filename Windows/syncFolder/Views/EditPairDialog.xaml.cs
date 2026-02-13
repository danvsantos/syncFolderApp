using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using syncFolder.Models;

namespace syncFolder.Views;

public partial class EditPairDialog : Window
{
    private readonly SyncPair? _editingPair;
    private readonly List<string> _filterPatterns = new();
    private readonly string[] _presetFilters = { "Thumbs.db", "desktop.ini", "*.tmp", "*.log", "node_modules", ".git" };

    public SyncPair? ResultPair { get; private set; }

    private bool IsAddMode => _editingPair == null;

    public EditPairDialog(SyncPair? pairToEdit)
    {
        InitializeComponent();
        _editingPair = pairToEdit;

        LocalizeUI();
        SetupIntervalComboBox();
        SetupSyncModeComboBox();
        SetupPresets();
        LoadPairData();
    }

    private void LocalizeUI()
    {
        Title = IsAddMode ? Strings.Resources.AddSyncPair : Strings.Resources.EditSyncPair;
        TitleText.Text = Title;

        NameLabel.Text = Strings.Resources.NameLabel;
        SourceLabel.Text = Strings.Resources.SourceLabel;
        DestLabel.Text = Strings.Resources.DestinationLabel;
        BrowseSourceButton.Content = Strings.Resources.Browse;
        BrowseDestButton.Content = Strings.Resources.Browse;
        IntervalLabel.Text = Strings.Resources.IntervalLabel;
        SyncModeLabel.Text = Strings.Resources.SyncModeLabel;
        ConflictHint.Text = Strings.Resources.ConflictDescription;
        DeleteOrphansCheckbox.Content = Strings.Resources.DeleteOrphans;
        EnabledCheckbox.Content = Strings.Resources.Enabled;
        FiltersLabel.Text = Strings.Resources.ExcludeFilters;
        AddFilterButton.Content = Strings.Resources.Add;
        PresetsLabel.Text = Strings.Resources.Presets;
        CancelButton.Content = Strings.Resources.Cancel;
        SaveButton.Content = IsAddMode ? Strings.Resources.Add : Strings.Resources.Save;
        ValidationText.Text = Strings.Resources.PathsMustDiffer;

        NameTextBox.ToolTip = Strings.Resources.MyDocumentsBackup;
    }

    private void SetupIntervalComboBox()
    {
        var intervals = new (int value, string label)[]
        {
            (1, Strings.Resources.Get("1 minute")),
            (2, Strings.Resources.Get("2 minutes")),
            (5, Strings.Resources.Get("5 minutes")),
            (10, Strings.Resources.Get("10 minutes")),
            (15, Strings.Resources.Get("15 minutes")),
            (30, Strings.Resources.Get("30 minutes")),
            (60, Strings.Resources.Get("60 minutes")),
            (120, Strings.Resources.Get("120 minutes"))
        };

        foreach (var (value, label) in intervals)
        {
            IntervalComboBox.Items.Add(new ComboBoxItem { Content = label, Tag = value });
        }
    }

    private void SetupSyncModeComboBox()
    {
        SyncModeComboBox.Items.Add(new ComboBoxItem
        {
            Content = Strings.Resources.OneWay,
            Tag = SyncMode.OneWay
        });
        SyncModeComboBox.Items.Add(new ComboBoxItem
        {
            Content = Strings.Resources.Bidirectional,
            Tag = SyncMode.Bidirectional
        });
    }

    private void SetupPresets()
    {
        foreach (var preset in _presetFilters)
        {
            var btn = new Button
            {
                Content = preset,
                FontSize = 11,
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 4, 0),
                Tag = preset
            };
            btn.Click += PresetButton_Click;
            PresetsPanel.Children.Add(btn);
        }
    }

    private void LoadPairData()
    {
        if (_editingPair != null)
        {
            NameTextBox.Text = _editingPair.Name;
            SourceTextBox.Text = _editingPair.SourcePath;
            DestTextBox.Text = _editingPair.DestinationPath;
            DeleteOrphansCheckbox.IsChecked = _editingPair.DeleteOrphans;
            EnabledCheckbox.IsChecked = _editingPair.IsEnabled;

            // Set interval
            foreach (ComboBoxItem item in IntervalComboBox.Items)
            {
                if ((int)item.Tag == _editingPair.IntervalMinutes)
                {
                    IntervalComboBox.SelectedItem = item;
                    break;
                }
            }

            // Set sync mode
            foreach (ComboBoxItem item in SyncModeComboBox.Items)
            {
                if ((SyncMode)item.Tag == _editingPair.SyncMode)
                {
                    SyncModeComboBox.SelectedItem = item;
                    break;
                }
            }

            // Load filters
            _filterPatterns.AddRange(_editingPair.FilterPatterns);
        }
        else
        {
            // Defaults for new pair
            IntervalComboBox.SelectedIndex = 2; // 5 minutes
            SyncModeComboBox.SelectedIndex = 0;  // One-way
            EnabledCheckbox.IsChecked = true;
        }

        RefreshFilters();
        UpdateSyncModeHint();
        UpdatePresetButtons();
    }

    private void RefreshFilters()
    {
        FiltersListView.Items.Clear();
        foreach (var pattern in _filterPatterns)
            FiltersListView.Items.Add(pattern);

        FiltersListView.Visibility = _filterPatterns.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdatePresetButtons()
    {
        foreach (Button btn in PresetsPanel.Children)
        {
            btn.IsEnabled = !_filterPatterns.Contains(btn.Tag as string ?? "");
        }
    }

    private void UpdateSyncModeHint()
    {
        if (SyncModeComboBox.SelectedItem is ComboBoxItem item && (SyncMode)item.Tag == SyncMode.Bidirectional)
            ConflictHint.Visibility = Visibility.Visible;
        else
            ConflictHint.Visibility = Visibility.Collapsed;
    }

    private bool IsValid()
    {
        var name = NameTextBox.Text?.Trim() ?? "";
        var source = SourceTextBox.Text?.Trim() ?? "";
        var dest = DestTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(dest))
            return false;

        if (string.Equals(source, dest, StringComparison.OrdinalIgnoreCase))
        {
            ValidationWarning.Visibility = Visibility.Visible;
            return false;
        }

        ValidationWarning.Visibility = Visibility.Collapsed;

        var interval = GetSelectedInterval();
        return interval >= 1 && interval <= 120;
    }

    private int GetSelectedInterval()
    {
        if (IntervalComboBox.SelectedItem is ComboBoxItem item && item.Tag is int val)
            return val;
        return 5;
    }

    private SyncMode GetSelectedSyncMode()
    {
        if (SyncModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is SyncMode mode)
            return mode;
        return SyncMode.OneWay;
    }

    // Event Handlers

    private void SyncMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateSyncModeHint();
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseFolder();
        if (path != null) SourceTextBox.Text = path;
    }

    private void BrowseDest_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseFolder();
        if (path != null) DestTextBox.Text = path;
    }

    private static string? BrowseFolder()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            ShowNewFolderButton = true,
            Description = "Select Folder"
        };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    private void AddFilter_Click(object sender, RoutedEventArgs e)
    {
        AddFilter();
    }

    private void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AddFilter();
    }

    private void AddFilter()
    {
        var pattern = FilterTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(pattern) || _filterPatterns.Contains(pattern)) return;

        _filterPatterns.Add(pattern);
        FilterTextBox.Text = "";
        RefreshFilters();
        UpdatePresetButtons();
    }

    private void RemoveFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string pattern)
        {
            _filterPatterns.Remove(pattern);
            RefreshFilters();
            UpdatePresetButtons();
        }
    }

    private void PresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string preset)
        {
            if (!_filterPatterns.Contains(preset))
            {
                _filterPatterns.Add(preset);
                RefreshFilters();
                UpdatePresetButtons();
            }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!IsValid())
        {
            MessageBox.Show("Please fill in all required fields correctly.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pair = new SyncPair
        {
            Id = _editingPair?.Id ?? Guid.NewGuid(),
            Name = NameTextBox.Text.Trim(),
            SourcePath = SourceTextBox.Text.Trim(),
            DestinationPath = DestTextBox.Text.Trim(),
            IntervalMinutes = GetSelectedInterval(),
            DeleteOrphans = DeleteOrphansCheckbox.IsChecked == true,
            IsEnabled = EnabledCheckbox.IsChecked == true,
            FilterPatterns = new List<string>(_filterPatterns),
            SyncMode = GetSelectedSyncMode(),
            LastSyncDate = _editingPair?.LastSyncDate
        };

        ResultPair = pair;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
