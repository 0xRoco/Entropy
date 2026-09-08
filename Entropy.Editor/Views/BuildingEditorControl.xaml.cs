using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Entropy.Content;
using Entropy.Editor.ViewModels;

namespace Entropy.Editor.Views;

public partial class BuildingEditorControl : UserControl
{
    private BuildingsViewModel? Vm => DataContext as BuildingsViewModel;
    private BuildingsViewModel? _hookedVm;
    private readonly Dictionary<string, ImageSource?> _tileCache = new(StringComparer.OrdinalIgnoreCase);

    public BuildingEditorControl()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => HookViewModel();
        HookViewModel();
    }
    
    private void HookViewModel()
    {
        if (_hookedVm is not null)
        {
            _hookedVm.PropertyChanged -= OnVmPropertyChanged;
            _hookedVm = null;
        }

        if (DataContext is BuildingsViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            _hookedVm = vm;
        }

        RebuildGrid();
        SyncResizeBoxes();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(BuildingsViewModel.Selected):
                RebuildGrid();
                SyncResizeBoxes();
                break;
            case nameof(BuildingsViewModel.PlacingAnchor):
                UpdatePlaceHint();
                break;
        }
    }

    private void RebuildGrid()
    {
        var vm = Vm;
        var rows = new List<BuildingRowViewModel>();
        _tileCache.Clear();

        if (vm?.Selected is { } def && def.Grid.Count > 0)
        {
            var terrains = TerrainById(vm);
            var brushCache = new Dictionary<string, (Brush Fill, Brush Glyph)>(StringComparer.OrdinalIgnoreCase);
            var width = def.Grid.Max(r => r.Length);

            for (var y = 0; y < def.Grid.Count; y++)
            {
                var cells = new List<BuildingCellViewModel>();
                for (var x = 0; x < width; x++)
                {
                    var marker = x < def.Grid[y].Length ? def.Grid[y][x] : default;
                    var terrainId = def.Legend.TryGetValue(marker, out var id) ? id : null;
                    var terrain = terrainId is not null ? terrains.GetValueOrDefault(terrainId) : null;

                    Brush fill, glyph;
                    if (terrainId is not null && brushCache.TryGetValue(terrainId, out var cached))
                    {
                        (fill, glyph) = cached;
                    }
                    else
                    {
                        fill = BrushFor(terrain?.Background);
                        glyph = BrushFor(terrain?.Color);
                        if (terrainId is not null)
                            brushCache[terrainId] = (fill, glyph);
                    }

                    var badge = def.Anchors
                        .Where(a => a.Value.X == x && a.Value.Y == y)
                        .Select(a => char.ToUpperInvariant(a.Key[0]).ToString())
                        .FirstOrDefault();

                    cells.Add(new BuildingCellViewModel(
                        x, y, fill, terrain?.Symbol.ToString() ?? string.Empty, glyph,
                        terrainId is not null ? TileFor(vm, terrainId) : null,
                        badge));
                }
                rows.Add(new BuildingRowViewModel(y, cells));
            }
        }

        PaintGrid.ItemsSource = rows;
        AnchorList.Items.Refresh();
        UpdatePlaceHint();
    }

    private ImageSource? TileFor(BuildingsViewModel vm, string terrainId)
    {
        if (_tileCache.TryGetValue(terrainId, out var cached))
            return cached;

        var tile = vm.Workspace.GetPreviewImage("terrain:" + terrainId);
        if (tile is Freezable { CanFreeze: true } freezable)
            freezable.Freeze();

        _tileCache[terrainId] = tile;
        return tile;
    }

    private void UpdateCellVisuals(BuildingCellViewModel cell)
    {
        var vm = Vm;
        if (vm?.Selected is not { } def) return;
        if (cell.Y >= def.Grid.Count || cell.X >= def.Grid[cell.Y].Length) return;

        var marker = def.Grid[cell.Y][cell.X];
        var terrainId = def.Legend.TryGetValue(marker, out var id) ? id : null;
        var terrain = terrainId is not null ? TerrainById(vm).GetValueOrDefault(terrainId) : null;

        cell.Fill = BrushFor(terrain?.Background);
        cell.Glyph = terrain?.Symbol.ToString() ?? string.Empty;
        cell.GlyphBrush = BrushFor(terrain?.Color);
        cell.Tile = terrainId is not null ? TileFor(vm, terrainId) : null;
    }

    private void UpdatePlaceHint()
    {
        var placing = Vm?.PlacingAnchor;
        foreach (var toggle in AnchorPresets.Children.OfType<ToggleButton>())
            toggle.IsChecked = placing is not null && toggle.Tag as string == placing;

        if (placing is null)
        {
            PlaceHint.Visibility = Visibility.Collapsed;
            return;
        }

        PlaceHint.Text = $"Click a cell to place '{placing}'";
        PlaceHint.Visibility = Visibility.Visible;
    }

    private void SyncResizeBoxes()
    {
        var def = Vm?.Selected;
        if (def is null || def.Grid.Count == 0)
        {
            WidthBox.Clear();
            HeightBox.Clear();
            return;
        }

        WidthBox.Text = def.Grid.Max(r => r.Length).ToString();
        HeightBox.Text = def.Grid.Count.ToString();
    }

    private void PaintGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var vm = Vm;
        if (vm?.Selected is null) return;

        var cell = CellUnderMouse(e);
        if (cell is null) return;

        if (vm.PlacingAnchor is { } anchorName)
        {
            vm.PlaceAnchor(anchorName, cell.X, cell.Y);
            vm.PlacingAnchor = null;
            RebuildGrid();
        }
        else
        {
            vm.PaintCell(cell.X, cell.Y, vm.SelectedTerrain);
            UpdateCellVisuals(cell);
        }

        e.Handled = true;
    }

    private void PaintGrid_MouseMove(object sender, MouseEventArgs e)
    {
        var vm = Vm;
        if (vm?.Selected is null || vm.PlacingAnchor is not null) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var cell = CellUnderMouse(e);
        if (cell is null) return;

        vm.PaintCell(cell.X, cell.Y, vm.SelectedTerrain);
        UpdateCellVisuals(cell);
    }

    private void PaintGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var vm = Vm;
        if (vm?.Selected is null) return;

        var cell = CellUnderMouse(e);
        if (cell is null) return;

        var marker = vm.Selected.Grid[cell.Y][cell.X];
        if (vm.Selected.Legend.TryGetValue(marker, out var terrainId))
        {
            vm.SelectedTerrain = vm.Workspace.Terrains.FirstOrDefault(t =>
                string.Equals(t.Id, terrainId, StringComparison.OrdinalIgnoreCase));
        }

        e.Handled = true;
    }

    private void OnApplyResize(object sender, RoutedEventArgs e)
    {
        var vm = Vm;
        if (vm is null) return;
        if (!int.TryParse(WidthBox.Text, out var width) ||
            !int.TryParse(HeightBox.Text, out var height))
            return;

        vm.Resize(width, height);
        RebuildGrid();
        SyncResizeBoxes();
    }

    private void OnZoomIn(object sender, RoutedEventArgs e)
    {
        if (Vm is { } vm)
            vm.Zoom = Math.Clamp(vm.Zoom + 1, 1, 4);
    }

    private void OnZoomOut(object sender, RoutedEventArgs e)
    {
        if (Vm is { } vm)
            vm.Zoom = Math.Clamp(vm.Zoom - 1, 1, 4);
    }

    private void OnPresetAnchor(object sender, RoutedEventArgs e)
    {
        var vm = Vm;
        if (vm?.Selected is null) return;

        var name = (string)((ButtonBase)sender).Tag;
        if (vm.PlacingAnchor == name)
        {
            vm.PlacingAnchor = null;
            return;
        }

        if (!vm.Selected.Anchors.ContainsKey(name))
            vm.AddAnchor(name);
        vm.PlacingAnchor = name;
    }

    private void OnAddAnchor(object sender, RoutedEventArgs e)
    {
        var vm = Vm;
        if (vm?.Selected is null) return;

        var name = AnchorNameBox.Text.Trim();
        if (vm.AddAnchor(name))
        {
            vm.PlacingAnchor = name;
            AnchorNameBox.Clear();
            RebuildGrid();
        }
        else
        {
            PlaceHint.Text = "Enter a unique anchor name first.";
            PlaceHint.Visibility = Visibility.Visible;
        }
    }

    private void OnDeleteAnchor(object sender, RoutedEventArgs e)
    {
        var vm = Vm;
        if (vm?.Selected is null) return;
        if (AnchorList.SelectedValue is not string name) return;

        vm.DeleteAnchor(name);
        if (vm.PlacingAnchor == name)
            vm.PlacingAnchor = null;
        RebuildGrid();
    }

    private BuildingCellViewModel? CellUnderMouse(MouseEventArgs e)
    {
        var hit = PaintGrid.InputHitTest(e.GetPosition(PaintGrid)) as FrameworkElement;
        while (hit is not null && hit.DataContext is not BuildingCellViewModel)
            hit = VisualTreeHelper.GetParent(hit) as FrameworkElement;
        return hit?.DataContext as BuildingCellViewModel;
    }

    private Dictionary<string, TerrainDefinition> TerrainById(BuildingsViewModel vm) =>
        vm.Workspace.Terrains
            .GroupBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    private static Brush BrushFor(OpenTK.Mathematics.Color4? color) =>
        color is not { } c
            ? Brushes.DimGray
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(
                (byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255)));
}

public sealed record BuildingRowViewModel(int Y, IReadOnlyList<BuildingCellViewModel> Cells);

public sealed class BuildingCellViewModel : ObservableObject
{
    public BuildingCellViewModel(
        int x, int y, Brush fill, string glyph, Brush glyphBrush,
        ImageSource? tile, string? badge)
    {
        X = x;
        Y = y;
        _fill = fill;
        _glyph = glyph;
        _glyphBrush = glyphBrush;
        _tile = tile;
        _badge = badge;
    }

    public int X { get; init; }
    public int Y { get; init; }

    private Brush _fill = Brushes.DimGray;
    public Brush Fill { get => _fill; set => SetProperty(ref _fill, value); }

    private string _glyph = string.Empty;
    public string Glyph { get => _glyph; set => SetProperty(ref _glyph, value); }

    private Brush _glyphBrush = Brushes.Transparent;
    public Brush GlyphBrush { get => _glyphBrush; set => SetProperty(ref _glyphBrush, value); }

    private ImageSource? _tile;
    public ImageSource? Tile { get => _tile; set => SetProperty(ref _tile, value); }

    private string? _badge;
    public string? Badge { get => _badge; set => SetProperty(ref _badge, value); }
}
