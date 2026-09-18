using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Entropy.Editor.ViewModels;
using Entropy.Simulation;

namespace Entropy.Editor.Views;

public partial class MapEditorControl : UserControl
{
    public ObservableCollection<MapEntitySelection> EntityChoices { get; } = new();
    private MapEntitySelection? _selectedEntity;

    public MapEditorControl()
    {
        InitializeComponent();
        MapCanvasView.ZoomChanged += OnZoomChanged;
        MapCanvasView.HoveredCellChanged += OnHoveredCellChanged;
        MapCanvasView.SelectedCellChanged += OnSelectedCellChanged;
    }

    private void ZoomOutClick(object sender, RoutedEventArgs e) => MapCanvasView.SetZoom(MapCanvasView.Zoom / 1.1);

    private void ZoomResetClick(object sender, RoutedEventArgs e) => MapCanvasView.SetZoom(1);

    private void ZoomInClick(object sender, RoutedEventArgs e) => MapCanvasView.SetZoom(MapCanvasView.Zoom * 1.1);

    private void GridToggleClick(object sender, RoutedEventArgs e) =>
        MapCanvasView.SetGridVisible(((CheckBox)sender).IsChecked == true);

    private void ObjectsToggleClick(object sender, RoutedEventArgs e) =>
        MapCanvasView.SetOverlayVisibility(MapOverlay.Objects, ((CheckBox)sender).IsChecked == true);

    private void AnchorsToggleClick(object sender, RoutedEventArgs e) =>
        MapCanvasView.SetOverlayVisibility(MapOverlay.Anchors, ((CheckBox)sender).IsChecked == true);

    private void TransitionsToggleClick(object sender, RoutedEventArgs e) =>
        MapCanvasView.SetOverlayVisibility(MapOverlay.Transitions, ((CheckBox)sender).IsChecked == true);

    private void CollisionToggleClick(object sender, RoutedEventArgs e) =>
        MapCanvasView.SetOverlayVisibility(MapOverlay.Collision, ((CheckBox)sender).IsChecked == true);

    private void OpacityToggleClick(object sender, RoutedEventArgs e) =>
        MapCanvasView.SetOverlayVisibility(MapOverlay.Opacity, ((CheckBox)sender).IsChecked == true);

    private void PaintToolClick(object sender, RoutedEventArgs e)
    {
        PaintToolButton.IsChecked = true;
        SelectToolButton.IsChecked = false;
        MapCanvasView.SetTool(MapTool.Paint);
    }

    private void SelectToolClick(object sender, RoutedEventArgs e)
    {
        PaintToolButton.IsChecked = false;
        SelectToolButton.IsChecked = true;
        MapCanvasView.SetTool(MapTool.Select);
    }

    private void ObjectToolClick(object sender, RoutedEventArgs e) => SetPlacementTool(MapTool.PlaceObject);

    private void AnchorToolClick(object sender, RoutedEventArgs e) => SetPlacementTool(MapTool.PlaceAnchor);

    private void TransitionToolClick(object sender, RoutedEventArgs e) => SetPlacementTool(MapTool.PlaceTransition);

    private void FillToolClick(object sender, RoutedEventArgs e) => SetPlacementTool(MapTool.Fill);

    private void RectangleToolClick(object sender, RoutedEventArgs e) => SetPlacementTool(MapTool.Rectangle);

    private void ResizeToolClick(object sender, RoutedEventArgs e) => SetPlacementTool(MapTool.Resize);

    private void SetPlacementTool(MapTool tool)
    {
        PaintToolButton.IsChecked = false;
        SelectToolButton.IsChecked = false;
        MapCanvasView.SetTool(tool);
    }

    private void OnZoomChanged(double zoom)
    {
        ZoomResetButton.Content = $"{zoom:P0}";
    }

    private void OnHoveredCellChanged(Point? cell)
    {
        CoordinateText.Text = cell is { } value
            ? $"Cell: {value.X}, {value.Y}"
            : "Cell: -";
    }

    private void OnSelectedCellChanged(Point? cell)
    {
        EntityChoices.Clear();
        _selectedEntity = null;
        MapCanvasView.SetSelectedEntity(null);
        if (DataContext is MapWorkspaceViewModel workspace)
        {
            workspace.SelectedObjectDefinitionId = null;
            workspace.AnchorKind = string.Empty;
            workspace.TargetMap = string.Empty;
            workspace.TargetAnchor = string.Empty;
        }
        if (cell is not { } value || MapCanvasView.Document?.Definition is not { } definition)
        {
            SelectedCellText.Text = "No cell selected";
            SelectedTerrainText.Text = string.Empty;
            SelectedEntitiesText.Text = string.Empty;
            return;
        }

        var x = (int)value.X;
        var y = (int)value.Y;
        var cellValue = MapCanvasView.Document.Cells[y * definition.Width + x];
        var objects = definition.Objects.Where(item => item.X == x && item.Y == y)
            .Select(item => $"Object: {item.Id} ({item.Definition})");
        var anchors = definition.Anchors.Where(item => item.X == x && item.Y == y)
            .Select(item => $"Anchor: {item.Id} ({item.Kind})");
        var transitions = definition.Transitions.Where(item => item.X == x && item.Y == y)
            .Select(item => $"Transition: {item.Id} -> {item.TargetMap}/{item.TargetAnchor}");

        foreach (var item in definition.Objects.Where(item => item.X == x && item.Y == y))
            EntityChoices.Add(new MapEntitySelection("Object", item.Id, $"Object  {item.Id}  [{item.Definition}]"));
        foreach (var item in definition.Anchors.Where(item => item.X == x && item.Y == y))
            EntityChoices.Add(new MapEntitySelection("Anchor", item.Id, $"Anchor  {item.Id}  [{item.Kind}]"));
        foreach (var item in definition.Transitions.Where(item => item.X == x && item.Y == y))
            EntityChoices.Add(new MapEntitySelection("Transition", item.Id,
                $"Link  {item.Id}  -> {item.TargetMap}/{item.TargetAnchor}"));

        SelectedCellText.Text = $"Coordinate: {x}, {y}\nGlyph: {cellValue.Glyph}";
        SelectedTerrainText.Text = $"Terrain: {cellValue.TerrainId}";
        SelectedEntitiesText.Text = string.Join("\n", objects.Concat(anchors).Concat(transitions));
        if (EntityChoices.Count > 0)
            EntityList.SelectedIndex = 0;
    }

    private void EntitySelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _selectedEntity = EntityList.SelectedItem as MapEntitySelection;
        MapCanvasView.SetSelectedEntity(_selectedEntity);
        if (_selectedEntity is not null && DataContext is MapWorkspaceViewModel workspace &&
            MapCanvasView.Document?.Definition is { } definition)
        {
            switch (_selectedEntity.Type)
            {
                case "Object":
                    if (definition.Objects.FirstOrDefault(item => item.Id == _selectedEntity.Id) is { } objectPlacement)
                        workspace.SelectedObjectDefinitionId = objectPlacement.Definition;
                    break;
                case "Anchor":
                    if (definition.Anchors.FirstOrDefault(item => item.Id == _selectedEntity.Id) is { } anchor)
                        workspace.AnchorKind = anchor.Kind;
                    break;
                case "Transition":
                    if (definition.Transitions.FirstOrDefault(item => item.Id == _selectedEntity.Id) is { } transition)
                    {
                        workspace.TargetMap = transition.TargetMap;
                        workspace.TargetAnchor = transition.TargetAnchor;
                    }
                    break;
            }
        }
    }

    private void RemoveEntitiesClick(object sender, RoutedEventArgs e)
    {
        if (MapCanvasView.SelectedCell is not { } cell || MapCanvasView.Document is null)
            return;

        if (_selectedEntity is null)
            MapCanvasView.Document.RemoveEntitiesAt((int)cell.X, (int)cell.Y);
        else
            MapCanvasView.Document.RemoveEntity(_selectedEntity.Type, _selectedEntity.Id);
        OnSelectedCellChanged(cell);
        MapCanvasView.InvalidateVisual();
    }

    private void MoveLeftClick(object sender, RoutedEventArgs e) => MoveSelected(-1, 0);
    private void MoveUpClick(object sender, RoutedEventArgs e) => MoveSelected(0, -1);
    private void MoveDownClick(object sender, RoutedEventArgs e) => MoveSelected(0, 1);
    private void MoveRightClick(object sender, RoutedEventArgs e) => MoveSelected(1, 0);

    private void MoveSelected(int dx, int dy)
    {
        if (MapCanvasView.SelectedCell is not { } cell || MapCanvasView.Document is null)
            return;

        var next = new Point(cell.X + dx, cell.Y + dy);
        var definition = MapCanvasView.Document.Definition;
        if (definition is null || next.X < 0 || next.X >= definition.Width || next.Y < 0 || next.Y >= definition.Height)
            return;
        if (!definition.Objects.Any(item => item.X == cell.X && item.Y == cell.Y) &&
            !definition.Anchors.Any(item => item.X == cell.X && item.Y == cell.Y) &&
            !definition.Transitions.Any(item => item.X == cell.X && item.Y == cell.Y))
            return;

        if (_selectedEntity is null)
            MapCanvasView.Document.MoveEntitiesAt((int)cell.X, (int)cell.Y, dx, dy);
        else
            MapCanvasView.Document.MoveEntity(_selectedEntity.Type, _selectedEntity.Id, dx, dy);
        MapCanvasView.SelectCell(next);
    }

    private void ApplyEntityFieldsClick(object sender, RoutedEventArgs e)
    {
        if (MapCanvasView.SelectedCell is not { } cell ||
            MapCanvasView.Document is null || DataContext is not MapWorkspaceViewModel workspace)
            return;

        if (_selectedEntity is null)
            MapCanvasView.Document.UpdateEntitiesAt((int)cell.X, (int)cell.Y,
                workspace.SelectedObjectDefinitionId ?? string.Empty, workspace.AnchorKind,
                workspace.TargetMap, workspace.TargetAnchor);
        else
            MapCanvasView.Document.UpdateEntity(_selectedEntity.Type, _selectedEntity.Id,
                workspace.SelectedObjectDefinitionId ?? string.Empty, workspace.AnchorKind,
                workspace.TargetMap, workspace.TargetAnchor);
        OnSelectedCellChanged(cell);
        MapCanvasView.InvalidateVisual();
    }

    private void CloseMapTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: MapDocumentViewModel document } ||
            DataContext is not MapWorkspaceViewModel workspace)
            return;

        if (document.IsDirty)
        {
            var result = MessageBox.Show(
                $"Save changes to {document.DisplayName} before closing?",
                "Unsaved map changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Cancel)
                return;
            if (result == MessageBoxResult.Yes)
                workspace.SaveMapCommand.Execute(null);
            if (document.IsDirty)
                return;
        }

        var index = workspace.Documents.IndexOf(document);
        workspace.Documents.Remove(document);
        workspace.Selected = workspace.Documents.Count == 0
            ? null
            : workspace.Documents[Math.Min(index, workspace.Documents.Count - 1)];
        e.Handled = true;
    }
}

public sealed record MapEntitySelection(string Type, string Id, string Label);

public enum MapTool
{
    Paint,
    Select,
    PlaceObject,
    PlaceAnchor,
    PlaceTransition,
    Fill,
    Rectangle
    , Resize
}

public enum ResizeEdge
{
    None,
    Left,
    Right,
    Top,
    Bottom
}

[Flags]
public enum MapOverlay
{
    None = 0,
    Objects = 1,
    Anchors = 2,
    Transitions = 4
    , Collision = 8
    , Opacity = 16
}

public sealed class MapCanvas : FrameworkElement
{
    private const double TileSize = 18;
    private const double MinZoom = 0.5;
    private const double MaxZoom = 4;
    private Point? _hoveredCell;
    private Point? _rectangleStart;
    private Point? _rectangleEnd;
    private Point? _panStart;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;
    private ResizeEdge _resizeEdge;
    private int _resizeStartWidth;
    private int _resizeStartHeight;
    private int _resizeWidth;
    private int _resizeHeight;

    public double Zoom { get; private set; } = 1;
    public bool ShowGrid { get; private set; } = true;
    public MapOverlay VisibleOverlays { get; private set; } = MapOverlay.Objects | MapOverlay.Anchors | MapOverlay.Transitions;
    public MapTool Tool { get; private set; } = MapTool.Paint;
    public Point? SelectedCell { get; private set; }
    public MapEntitySelection? SelectedEntity { get; private set; }
    public event Action<double>? ZoomChanged;
    public event Action<Point?>? HoveredCellChanged;
    public event Action<Point?>? SelectedCellChanged;

    public void SetTool(MapTool tool) => Tool = tool;

    public void SetSelectedEntity(MapEntitySelection? entity) => SelectedEntity = entity;

    public void SetGridVisible(bool visible)
    {
        if (ShowGrid == visible)
            return;

        ShowGrid = visible;
        InvalidateVisual();
    }

    public void SetOverlayVisibility(MapOverlay overlay, bool visible)
    {
        VisibleOverlays = visible ? VisibleOverlays | overlay : VisibleOverlays & ~overlay;
        InvalidateVisual();
    }

    public void SetZoom(double zoom)
    {
        var nextZoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        if (Math.Abs(nextZoom - Zoom) < 0.001)
            return;

        Zoom = nextZoom;
        InvalidateMeasure();
        InvalidateVisual();
        ZoomChanged?.Invoke(Zoom);
    }

    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
        nameof(Document),
        typeof(MapDocumentViewModel),
        typeof(MapCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public MapDocumentViewModel? Document
    {
        get => (MapDocumentViewModel?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public static readonly DependencyProperty SelectedTerrainIdProperty = DependencyProperty.Register(
        nameof(SelectedTerrainId), typeof(string), typeof(MapCanvas), new FrameworkPropertyMetadata(null));

    public string? SelectedTerrainId
    {
        get => (string?)GetValue(SelectedTerrainIdProperty);
        set => SetValue(SelectedTerrainIdProperty, value);
    }

    public static readonly DependencyProperty SelectedTerrainSymbolProperty = DependencyProperty.Register(
        nameof(SelectedTerrainSymbol), typeof(string), typeof(MapCanvas), new FrameworkPropertyMetadata(null));

    public string? SelectedTerrainSymbol
    {
        get => (string?)GetValue(SelectedTerrainSymbolProperty);
        set => SetValue(SelectedTerrainSymbolProperty, value);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseRightButtonDown += OnMouseRightButtonDown;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;
        PreviewMouseWheel += OnPreviewMouseWheel;
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
        KeyDown += OnKeyDown;
        Focusable = true;
        Cursor = Cursors.Cross;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var definition = Document?.Definition;
        return definition is null
            ? new Size(0, 0)
            : new Size(definition.Width * TileSize * Zoom, definition.Height * TileSize * Zoom);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Document?.Definition is not { } definition)
            return;

        drawingContext.PushTransform(new ScaleTransform(Zoom, Zoom));
        for (var index = 0; index < Document.Cells.Count; index++)
        {
            var cell = Document.Cells[index];
            var x = index % definition.Width * TileSize;
            var y = index / definition.Width * TileSize;
            var rect = new Rect(x, y, TileSize, TileSize);
            var gridPen = ShowGrid ? new Pen(Brushes.Black, 1) : null;
            drawingContext.DrawRectangle(BrushFor(cell.TerrainId), gridPen, rect);
            if (DataContext is MapWorkspaceViewModel workspace &&
                workspace.TerrainChoices.FirstOrDefault(item => item.Id.Equals(cell.TerrainId, StringComparison.OrdinalIgnoreCase)) is { } terrain)
            {
                if (VisibleOverlays.HasFlag(MapOverlay.Collision) && !terrain.Walkable)
                    drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(80, 220, 50, 50)), null, rect);
                if (VisibleOverlays.HasFlag(MapOverlay.Opacity) && terrain.Opaque)
                    drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(65, 80, 120, 220)), null, rect);
            }
            if (cell.TerrainId == "invalid")
                drawingContext.DrawRectangle(null, new Pen(Brushes.Red, 2), rect);

            var text = new FormattedText(
                cell.Glyph.ToString(),
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas"),
                10,
                Brushes.LightGray,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(text, new Point(
                x + (TileSize - text.Width) / 2,
                y + (TileSize - text.Height) / 2));
        }

        if (VisibleOverlays.HasFlag(MapOverlay.Objects))
            foreach (var placement in definition.Objects)
                DrawMarker(drawingContext, placement.X, placement.Y, Brushes.Orange, "O");
        if (VisibleOverlays.HasFlag(MapOverlay.Anchors))
            foreach (var anchor in definition.Anchors)
                DrawMarker(drawingContext, anchor.X, anchor.Y, Brushes.LimeGreen, "A");
        if (VisibleOverlays.HasFlag(MapOverlay.Transitions))
            foreach (var transition in definition.Transitions)
                DrawMarker(drawingContext, transition.X, transition.Y, Brushes.Gold, "T");

        if (_hoveredCell is { } hovered)
        {
            var hoverBrush = new SolidColorBrush(Color.FromArgb(55, 199, 177, 90));
            hoverBrush.Freeze();
            drawingContext.DrawRectangle(hoverBrush, new Pen(Brushes.White, 1),
                new Rect(hovered.X * TileSize, hovered.Y * TileSize, TileSize, TileSize));
        }

        if (SelectedCell is { } selected)
            drawingContext.DrawRectangle(null, new Pen(Brushes.Cyan, 2),
                new Rect(selected.X * TileSize + 1, selected.Y * TileSize + 1, TileSize - 2, TileSize - 2));

        if (_rectangleStart is { } start && _rectangleEnd is { } end)
        {
            var left = Math.Min(start.X, end.X) * TileSize;
            var top = Math.Min(start.Y, end.Y) * TileSize;
            var width = (Math.Abs(start.X - end.X) + 1) * TileSize;
            var height = (Math.Abs(start.Y - end.Y) + 1) * TileSize;
            drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(35, 199, 177, 90)),
                new Pen(Brushes.Gold, 1.5), new Rect(left, top, width, height));
        }

        if (Tool == MapTool.Resize)
        {
            var width = definition.Width * TileSize;
            var height = definition.Height * TileSize;
            var xBrush = new SolidColorBrush(Color.FromArgb(220, 220, 70, 70));
            var yBrush = new SolidColorBrush(Color.FromArgb(220, 70, 200, 110));
            drawingContext.DrawRectangle(yBrush, null, new Rect(0, 0, width, 3));
            drawingContext.DrawRectangle(yBrush, null, new Rect(0, height - 3, width, 3));
            drawingContext.DrawRectangle(xBrush, null, new Rect(0, 0, 3, height));
            drawingContext.DrawRectangle(xBrush, null, new Rect(width - 3, 0, 3, height));
            DrawAxisLabel(drawingContext, "Y", 6, 2, yBrush);
            DrawAxisLabel(drawingContext, "X", width - 14, height - 16, xBrush);
        }

        drawingContext.Pop();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        if (Document is null)
            return;

        var point = e.GetPosition(this);
        if (Tool == MapTool.Resize && Document.Definition is { } resizeDefinition)
        {
            _resizeEdge = ResizeEdgeAt(point, resizeDefinition);
            if (_resizeEdge != ResizeEdge.None)
            {
                _resizeStartWidth = resizeDefinition.Width;
                _resizeStartHeight = resizeDefinition.Height;
                _resizeWidth = _resizeStartWidth;
                _resizeHeight = _resizeStartHeight;
                Document.BeginResizePreview();
                CaptureMouse();
                e.Handled = true;
                return;
            }

            e.Handled = true;
            return;
        }
        var cell = CellAt(point);
        if (cell is null)
            return;

        if (Tool == MapTool.Select)
        {
            SelectCell(cell.Value);
            e.Handled = true;
            return;
        }

        if (Tool == MapTool.Rectangle)
        {
            _rectangleStart = cell.Value;
            _rectangleEnd = cell.Value;
            CaptureMouse();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (DataContext is MapWorkspaceViewModel workspace)
        {
            switch (Tool)
            {
                case MapTool.PlaceObject:
                    Document.PlaceObject((int)cell.Value.X, (int)cell.Value.Y, workspace.SelectedObjectDefinitionId ?? string.Empty);
                    break;
                case MapTool.PlaceAnchor:
                    Document.PlaceAnchor((int)cell.Value.X, (int)cell.Value.Y, workspace.AnchorKind);
                    break;
                case MapTool.PlaceTransition:
                    Document.PlaceTransition((int)cell.Value.X, (int)cell.Value.Y, workspace.TargetMap, workspace.TargetAnchor);
                    break;
                case MapTool.Fill:
                    Document.Fill((int)cell.Value.X, (int)cell.Value.Y, SelectedTerrainId ?? string.Empty,
                        SelectedTerrainSymbol?.FirstOrDefault());
                    break;
                default:
                    goto Paint;
            }

            SelectCell(cell.Value);
            e.Handled = true;
            return;
        }

    Paint:

        if (string.IsNullOrWhiteSpace(SelectedTerrainId))
            return;

        CaptureMouse();
        PaintAt(point);
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (Tool == MapTool.Rectangle && _rectangleStart is { } start &&
            CellAt(e.GetPosition(this)) is { } end && Document is not null)
        {
            if (DataContext is MapWorkspaceViewModel workspace)
                Document.PaintRectangle((int)start.X, (int)start.Y, (int)end.X, (int)end.Y,
                    workspace.SelectedTerrainId ?? string.Empty, workspace.SelectedTerrainSymbol?.FirstOrDefault());
            _rectangleStart = null;
            _rectangleEnd = null;
            InvalidateVisual();
        }

        if (_resizeEdge != ResizeEdge.None && Document?.Definition is { } resizeDefinition)
        {
            var (width, height) = ResizeDimensions(e.GetPosition(this), resizeDefinition);
            Document.PreviewResize(width, height, _resizeEdge != ResizeEdge.Left, _resizeEdge != ResizeEdge.Top,
                (DataContext as MapWorkspaceViewModel)?.SelectedTerrainSymbol?.FirstOrDefault());
            Document.CommitResizePreview();
            _resizeEdge = ResizeEdge.None;
            ReleaseMouseCapture();
            InvalidateMeasure();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (IsMouseCaptured && _panStart is null)
            ReleaseMouseCapture();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_panStart is { } panStart && e.MiddleButton == MouseButtonState.Pressed)
        {
            var viewer = FindScrollViewer();
            if (viewer is not null)
            {
                var panPoint = e.GetPosition(viewer);
                viewer.ScrollToHorizontalOffset(_panStartHorizontalOffset - (panPoint.X - panStart.X));
                viewer.ScrollToVerticalOffset(_panStartVerticalOffset - (panPoint.Y - panStart.Y));
            }

            return;
        }

        var point = e.GetPosition(this);
        if (_resizeEdge != ResizeEdge.None && Document?.Definition is { } resizeDefinition)
        {
            (_resizeWidth, _resizeHeight) = ResizeDimensions(point, resizeDefinition);
            Document.PreviewResize(_resizeWidth, _resizeHeight, _resizeEdge != ResizeEdge.Left,
                _resizeEdge != ResizeEdge.Top, (DataContext as MapWorkspaceViewModel)?.SelectedTerrainSymbol?.FirstOrDefault());
            InvalidateMeasure();
            InvalidateVisual();
            return;
        }
        var cell = CellAt(point);
        if (cell is null)
            _hoveredCell = null;
        else
            _hoveredCell = cell.Value;
        HoveredCellChanged?.Invoke(_hoveredCell);

        if (Tool == MapTool.Rectangle && _rectangleStart is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            _rectangleEnd = CellAt(point);
            InvalidateVisual();
        }
        else if (Tool == MapTool.Paint && e.LeftButton == MouseButtonState.Pressed && IsMouseCaptured)
            PaintAt(point);
        else
            InvalidateVisual();
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (_panStart is not null)
            return;

        _hoveredCell = null;
        HoveredCellChanged?.Invoke(null);
        InvalidateVisual();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        var viewer = FindScrollViewer();
        if (viewer is null)
            return;

        _panStart = e.GetPosition(viewer);
        _panStartHorizontalOffset = viewer.HorizontalOffset;
        _panStartVerticalOffset = viewer.VerticalOffset;
        CaptureMouse();
        Cursor = Cursors.ScrollAll;
        e.Handled = true;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        if (_panStart is null)
            return;

        _panStart = null;
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        Cursor = Cursors.Cross;
        e.Handled = true;
    }

    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        var cell = CellAt(e.GetPosition(this));
        if (cell is not null)
            SelectCell(cell.Value);
        e.Handled = true;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (Document is null || SelectedCell is not { } selected)
            return;

        if (e.Key is Key.Delete or Key.Back)
        {
            if (SelectedEntity is { } entity)
                Document.RemoveEntity(entity.Type, entity.Id);
            else
                Document.RemoveEntitiesAt((int)selected.X, (int)selected.Y);
            SelectedCellChanged?.Invoke(selected);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var delta = e.Key switch
        {
            Key.Left => new Point(-1, 0),
            Key.Up => new Point(0, -1),
            Key.Right => new Point(1, 0),
            Key.Down => new Point(0, 1),
            _ => new Point(0, 0)
        };
        if ((delta.X == 0 && delta.Y == 0) || Document.Definition is not { } definition)
            return;

        var next = new Point(selected.X + delta.X, selected.Y + delta.Y);
        if (next.X < 0 || next.X >= definition.Width || next.Y < 0 || next.Y >= definition.Height)
            return;
        if (SelectedEntity is { } selectedEntity)
            Document.MoveEntity(selectedEntity.Type, selectedEntity.Id, (int)delta.X, (int)delta.Y);
        else
        {
            if (!HasEntitiesAt((int)selected.X, (int)selected.Y))
                return;
            Document.MoveEntitiesAt((int)selected.X, (int)selected.Y, (int)delta.X, (int)delta.Y);
        }
        SelectCell(next);
        e.Handled = true;
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Document?.Definition is null)
            return;

        var viewer = FindScrollViewer();
        var pointer = viewer is null ? e.GetPosition(this) : e.GetPosition(viewer);
        var oldZoom = Zoom;
        var oldHorizontalOffset = viewer?.HorizontalOffset ?? 0;
        var oldVerticalOffset = viewer?.VerticalOffset ?? 0;
        var scale = e.Delta > 0 ? 1.1 : 1 / 1.1;
        SetZoom(Zoom * scale);
        if (Math.Abs(oldZoom - Zoom) < 0.001)
            return;
        e.Handled = true;

        if (viewer is null)
            return;

        var contentPoint = new Vector(
            (oldHorizontalOffset + pointer.X) / oldZoom,
            (oldVerticalOffset + pointer.Y) / oldZoom);
        Dispatcher.BeginInvoke(() =>
        {
            viewer.ScrollToHorizontalOffset(contentPoint.X * Zoom - pointer.X);
            viewer.ScrollToVerticalOffset(contentPoint.Y * Zoom - pointer.Y);
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void PaintAt(Point point)
    {
        if (Document is null || string.IsNullOrWhiteSpace(SelectedTerrainId))
            return;

        Document.Paint((int)(point.X / Zoom / TileSize), (int)(point.Y / Zoom / TileSize), SelectedTerrainId,
            SelectedTerrainSymbol?.FirstOrDefault());
        InvalidateVisual();
    }

    private (int Width, int Height) ResizeDimensions(Point point, MapDefinition definition)
    {
        var x = point.X / Zoom;
        var y = point.Y / Zoom;
        var width = _resizeStartWidth;
        var height = _resizeStartHeight;
        if (_resizeEdge is ResizeEdge.Left or ResizeEdge.Right)
        {
            var delta = (int)Math.Round((x - _resizeStartWidth * TileSize) / TileSize);
            width = _resizeEdge == ResizeEdge.Right
                ? _resizeStartWidth + delta
                : _resizeStartWidth - (int)Math.Round(x / TileSize);
        }
        if (_resizeEdge is ResizeEdge.Top or ResizeEdge.Bottom)
        {
            var delta = (int)Math.Round((y - _resizeStartHeight * TileSize) / TileSize);
            height = _resizeEdge == ResizeEdge.Bottom
                ? _resizeStartHeight + delta
                : _resizeStartHeight - (int)Math.Round(y / TileSize);
        }
        return (Math.Max(1, width), Math.Max(1, height));
    }

    private static void DrawAxisLabel(DrawingContext drawingContext, string label, double x, double y, Brush brush)
    {
        var text = new FormattedText(label, System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, new Typeface("Consolas"), 11, brush,
            1);
        drawingContext.DrawText(text, new Point(x, y));
    }

    private ResizeEdge ResizeEdgeAt(Point point, MapDefinition definition)
    {
        var x = point.X / Zoom;
        var y = point.Y / Zoom;
        var width = definition.Width * TileSize;
        var height = definition.Height * TileSize;
        const double handle = 14;
        if (y >= 0 && y <= height && x >= -handle && x <= handle) return ResizeEdge.Left;
        if (y >= 0 && y <= height && x >= width - handle && x <= width + handle) return ResizeEdge.Right;
        if (x >= 0 && x <= width && y >= -handle && y <= handle) return ResizeEdge.Top;
        if (x >= 0 && x <= width && y >= height - handle && y <= height + handle) return ResizeEdge.Bottom;
        return ResizeEdge.None;
    }

    private Point? CellAt(Point point)
    {
        if (Document?.Definition is not { } definition)
            return null;

        var cell = new Point((int)(point.X / Zoom / TileSize), (int)(point.Y / Zoom / TileSize));
        return cell.X >= 0 && cell.X < definition.Width && cell.Y >= 0 && cell.Y < definition.Height
            ? cell
            : null;
    }

    public void SelectCell(Point cell)
    {
        SelectedCell = cell;
        SelectedCellChanged?.Invoke(cell);
        InvalidateVisual();
    }

    private bool HasEntitiesAt(int x, int y) =>
        Document?.Definition is { } definition &&
        (definition.Objects.Any(item => item.X == x && item.Y == y) ||
         definition.Anchors.Any(item => item.X == x && item.Y == y) ||
         definition.Transitions.Any(item => item.X == x && item.Y == y));

    private static void DrawMarker(DrawingContext context, int x, int y, Brush brush, string label)
    {
        var center = new Point(x * TileSize + TileSize / 2, y * TileSize + TileSize / 2);
        context.DrawEllipse(brush, new Pen(Brushes.Black, 1), center, 4, 4);
        var text = new FormattedText(label, System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, new Typeface("Consolas"), 7, Brushes.Black, 1);
        context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
    }

    private ScrollViewer? FindScrollViewer()
    {
        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is ScrollViewer viewer)
                return viewer;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static Brush BrushFor(string terrainId) => terrainId switch
    {
        "road_asphalt" => new SolidColorBrush(Color.FromRgb(37, 37, 37)),
        "sidewalk" => new SolidColorBrush(Color.FromRgb(85, 85, 85)),
        "wall_concrete" => new SolidColorBrush(Color.FromRgb(56, 56, 56)),
        "wall_brick" => new SolidColorBrush(Color.FromRgb(48, 42, 42)),
        "door" => new SolidColorBrush(Color.FromRgb(90, 90, 90)),
        "grass" => new SolidColorBrush(Color.FromRgb(24, 32, 24)),
        _ => Brushes.Black
    };
}
