using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Entropy.Content;
using Entropy.Content.Loading;
using Entropy.Simulation;

namespace Entropy.Editor.ViewModels;

public sealed partial class MapWorkspaceViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ObservableCollection<MapDocumentViewModel> Documents { get; } = new();
    public ObservableCollection<TerrainDefinition> TerrainChoices { get; } = new();
    public ObservableCollection<WorldObjectDefinition> ObjectChoices { get; } = new();
    public ObservableCollection<string> TargetMapChoices { get; } = new() { "outside" };

    [ObservableProperty] private MapDocumentViewModel? _selected;
    [ObservableProperty] private string _statusText = "No maps loaded.";
    [ObservableProperty] private string? _selectedTerrainId;
    [ObservableProperty] private string? _selectedTerrainSymbol;
    [ObservableProperty] private string? _selectedObjectDefinitionId;
    [ObservableProperty] private string _anchorKind = string.Empty;
    [ObservableProperty] private string _targetMap = string.Empty;
    [ObservableProperty] private string _targetAnchor = string.Empty;

    public void Load(string contentJsonFolder)
    {
        Documents.Clear();
        TerrainChoices.Clear();
        ObjectChoices.Clear();
        TargetMapChoices.Clear();
        TargetMapChoices.Add("outside");
        foreach (var terrain in DefinitionLoader.LoadTerrains(contentJsonFolder))
            TerrainChoices.Add(terrain);
        foreach (var worldObject in DefinitionLoader.LoadWorldObjects(contentJsonFolder))
            ObjectChoices.Add(worldObject);
        SelectedTerrainId = TerrainChoices.FirstOrDefault()?.Id;
        var contentRoot = Directory.GetParent(contentJsonFolder)?.FullName;
        var mapsFolder = contentRoot is null ? null : Path.Combine(contentRoot, "Maps");
        if (mapsFolder is null || !Directory.Exists(mapsFolder))
        {
            StatusText = "Maps folder not found.";
            Selected = null;
            return;
        }

        foreach (var path in Directory.EnumerateFiles(mapsFolder, "*.json").OrderBy(path => path))
        {
            try
            {
                var definition = JsonSerializer.Deserialize<MapDefinition>(File.ReadAllText(path), JsonOptions)
                                 ?? throw new InvalidOperationException("The map file is empty.");
                Documents.Add(new MapDocumentViewModel(path, definition));
                TargetMapChoices.Add(definition.Id);
            }
            catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException)
            {
                Documents.Add(MapDocumentViewModel.Invalid(path, ex.Message));
            }
        }

        Selected = Documents.FirstOrDefault();
        StatusText = $"Loaded {Documents.Count} map document(s) from {mapsFolder}";
    }

    partial void OnSelectedTerrainIdChanged(string? value) =>
        SelectedTerrainSymbol = TerrainChoices.FirstOrDefault(terrain => terrain.Id.Equals(value, StringComparison.OrdinalIgnoreCase))?.Symbol.ToString();

    [RelayCommand]
    private void SaveMap()
    {
        if (Selected is null || !Selected.IsDirty || Selected.Definition is null)
            return;

        if (Selected.HasExternalChanges)
        {
            StatusText = $"Cannot save {Path.GetFileName(Selected.FilePath)}: the file changed on disk. Reload before saving.";
            return;
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Selected.FilePath, JsonSerializer.Serialize(Selected.Definition, options));
        Selected.MarkClean();
        StatusText = $"Saved {Path.GetFileName(Selected.FilePath)}";
    }

    public void SaveAll() => SaveMap();

    public IReadOnlyList<string> Validate()
    {
        var objectIds = ObjectChoices
            .Select(item => item.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mapsById = Documents
            .Where(document => document.Definition is not null)
            .GroupBy(document => document.Definition!.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();

        foreach (var document in Documents)
        {
            if (document.Definition is not { } definition)
            {
                errors.Add($"{document.DisplayName}: {document.ValidationErrors.FirstOrDefault() ?? "map could not be loaded"}");
                continue;
            }

            errors.AddRange(document.ValidationErrors.Select(error => $"{document.DisplayName}: {error}"));
            foreach (var placement in definition.Objects.Where(item => !objectIds.Contains(item.Definition)))
                errors.Add($"{document.DisplayName}: object '{placement.Id}' references missing definition '{placement.Definition}'");

            foreach (var transition in definition.Transitions)
            {
                // "outside" is a runtime alias resolved to outside_<seed> by WorldSetup.
                if (transition.TargetMap.Equals("outside", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!mapsById.TryGetValue(transition.TargetMap, out var target))
                {
                    errors.Add($"{document.DisplayName}: transition '{transition.Id}' targets missing map '{transition.TargetMap}'");
                    continue;
                }

                if (!target.Definition!.Anchors.Any(anchor =>
                        anchor.Id.Equals(transition.TargetAnchor, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"{document.DisplayName}: transition '{transition.Id}' targets missing anchor '{transition.TargetAnchor}' on '{transition.TargetMap}'");
            }
        }

        return errors;
    }

    [RelayCommand]
    private void UndoMap() => Selected?.Undo();

    [RelayCommand]
    private void RedoMap() => Selected?.Redo();
}

public sealed partial class MapDocumentViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions SnapshotOptions = new();
    public string FilePath { get; }
    public MapDefinition? Definition { get; }
    public bool HasExternalChanges => File.Exists(FilePath) &&
                                      File.GetLastWriteTimeUtc(FilePath) != _lastWriteTimeUtc;
    public string DisplayName => Definition?.Name is { Length: > 0 } name ? name : Definition?.Id ?? Path.GetFileName(FilePath);
    public string TabTitle => IsDirty ? $"* {DisplayName}" : DisplayName;
    public string Summary => Definition is null ? "Invalid map" : $"{Definition.Width} x {Definition.Height}  |  {Definition.Kind}";
    public ObservableCollection<string> ValidationErrors { get; } = new();
    public bool IsValid => ValidationErrors.Count == 0;
    public ObservableCollection<MapCellViewModel> Cells { get; } = new();
    [ObservableProperty] private bool _isDirty;
    private DateTime _lastWriteTimeUtc;
    public event Action? Changed;
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private string? _resizeSnapshot;

    public MapDocumentViewModel(string filePath, MapDefinition definition)
    {
        FilePath = filePath;
        Definition = definition;
        _lastWriteTimeUtc = File.Exists(filePath) ? File.GetLastWriteTimeUtc(filePath) : default;
        foreach (var error in MapDefinitionValidator.Validate(definition))
            ValidationErrors.Add(error);
        RefreshCells(definition);
    }

    private MapDocumentViewModel(string filePath, string error)
    {
        FilePath = filePath;
        _lastWriteTimeUtc = File.Exists(filePath) ? File.GetLastWriteTimeUtc(filePath) : default;
        ValidationErrors.Add(error);
    }

    public static MapDocumentViewModel Invalid(string filePath, string error) => new(filePath, error);

    public void Paint(int x, int y, string terrainId, char? symbolOverride = null)
    {
        if (Definition?.Terrain is not { } terrain ||
            y < 0 || y >= Definition.Height || x < 0 || x >= Definition.Width ||
            !EnsureTerrainSymbol(terrain, terrainId, symbolOverride))
            return;

        var symbol = terrain.Legend.First(pair => pair.Value.Equals(terrainId, StringComparison.OrdinalIgnoreCase)).Key[0];
        var row = terrain.Rows[y].ToCharArray();
        if (row[x] == symbol)
            return;

        PushSnapshot();
        _redo.Clear();
        row[x] = symbol;
        terrain.Rows[y] = new string(row);
        MarkChanged();
    }

    public void Fill(int x, int y, string terrainId, char? symbolOverride = null)
    {
        if (Definition?.Terrain is not { } terrain ||
            y < 0 || y >= Definition.Height || x < 0 || x >= Definition.Width ||
            !EnsureTerrainSymbol(terrain, terrainId, symbolOverride) || y >= terrain.Rows.Count || x >= terrain.Rows[y].Length)
            return;

        var replacement = terrain.Legend.First(pair => pair.Value.Equals(terrainId, StringComparison.OrdinalIgnoreCase)).Key[0];
        var original = terrain.Rows[y][x];
        if (original == replacement)
            return;

        PushSnapshotBeforeMutation();
        var queue = new Queue<(int X, int Y)>();
        var visited = new HashSet<(int X, int Y)>();
        queue.Enqueue((x, y));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current) || current.Y < 0 || current.Y >= terrain.Rows.Count ||
                current.X < 0 || current.X >= terrain.Rows[current.Y].Length ||
                terrain.Rows[current.Y][current.X] != original)
                continue;

            var row = terrain.Rows[current.Y].ToCharArray();
            row[current.X] = replacement;
            terrain.Rows[current.Y] = new string(row);
            queue.Enqueue((current.X - 1, current.Y));
            queue.Enqueue((current.X + 1, current.Y));
            queue.Enqueue((current.X, current.Y - 1));
            queue.Enqueue((current.X, current.Y + 1));
        }

        MarkChanged();
    }

    public void PaintRectangle(int startX, int startY, int endX, int endY, string terrainId, char? symbolOverride = null)
    {
        if (Definition?.Terrain is not { } terrain || !EnsureTerrainSymbol(terrain, terrainId, symbolOverride))
            return;

        var symbol = terrain.Legend.First(pair => pair.Value.Equals(terrainId, StringComparison.OrdinalIgnoreCase)).Key[0];
        var left = Math.Clamp(Math.Min(startX, endX), 0, Definition.Width - 1);
        var right = Math.Clamp(Math.Max(startX, endX), 0, Definition.Width - 1);
        var top = Math.Clamp(Math.Min(startY, endY), 0, Definition.Height - 1);
        var bottom = Math.Clamp(Math.Max(startY, endY), 0, Definition.Height - 1);

        PushSnapshotBeforeMutation();
        for (var y = top; y <= bottom && y < terrain.Rows.Count; y++)
        {
            var row = terrain.Rows[y].ToCharArray();
            for (var x = left; x <= right && x < row.Length; x++)
                row[x] = symbol;
            terrain.Rows[y] = new string(row);
        }

        MarkChanged();
    }

    private static bool EnsureTerrainSymbol(MapTerrainLayer terrain, string terrainId, char? symbolOverride)
    {
        if (terrain.Legend.ContainsValue(terrainId))
            return true;
        if (symbolOverride is not { } symbol)
            return false;

        var key = symbol.ToString();
        if (terrain.Legend.TryGetValue(key, out var existing) && !existing.Equals(terrainId, StringComparison.OrdinalIgnoreCase))
            return false;
        terrain.Legend[key] = terrainId;
        return true;
    }

    public bool Resize(int width, int height, bool keepLeft, bool keepTop, char? selectedTerrainSymbol = null)
        => ResizeCore(width, height, keepLeft, keepTop, selectedTerrainSymbol, true);

    public void BeginResizePreview() => _resizeSnapshot = Snapshot();

    public void PreviewResize(int width, int height, bool keepLeft, bool keepTop, char? selectedTerrainSymbol = null)
    {
        if (_resizeSnapshot is null) BeginResizePreview();
        Restore(_resizeSnapshot!, false);
        ResizeCore(width, height, keepLeft, keepTop, selectedTerrainSymbol, false);
    }

    public void CommitResizePreview()
    {
        if (_resizeSnapshot is not null)
        {
            _undo.Push(_resizeSnapshot);
            _redo.Clear();
            _resizeSnapshot = null;
        }
    }

    private bool ResizeCore(int width, int height, bool keepLeft, bool keepTop, char? selectedTerrainSymbol, bool recordHistory)
    {
        if (Definition?.Terrain is not { } terrain || width <= 0 || height <= 0)
            return false;

        if (width == Definition.Width && height == Definition.Height)
            return false;

        var fill = terrain.Legend.Keys.FirstOrDefault(key => !string.IsNullOrEmpty(key))?[0] ?? selectedTerrainSymbol ?? '?';
        var offsetX = keepLeft ? 0 : width - Definition.Width;
        var offsetY = keepTop ? 0 : height - Definition.Height;
        var oldRows = terrain.Rows.ToArray();
        if (recordHistory)
            PushSnapshotBeforeMutation();
        var rows = new List<string>(height);
        for (var y = 0; y < height; y++)
        {
            var row = new string(fill, width).ToCharArray();
            var sourceY = y - offsetY;
            if (sourceY >= 0 && sourceY < oldRows.Length)
            {
                var source = oldRows[sourceY];
                for (var x = 0; x < width; x++)
                {
                    var sourceX = x - offsetX;
                    if (sourceX >= 0 && sourceX < source.Length)
                        row[x] = source[sourceX];
                }
            }
            rows.Add(new string(row));
        }

        terrain.Rows = rows;
        Definition.Width = width;
        Definition.Height = height;
        foreach (var item in Definition.Objects) { item.X += offsetX; item.Y += offsetY; }
        foreach (var item in Definition.Anchors) { item.X += offsetX; item.Y += offsetY; }
        foreach (var item in Definition.Transitions) { item.X += offsetX; item.Y += offsetY; }
        Definition.Objects.RemoveAll(item => item.X < 0 || item.X >= width || item.Y < 0 || item.Y >= height);
        Definition.Anchors.RemoveAll(item => item.X < 0 || item.X >= width || item.Y < 0 || item.Y >= height);
        Definition.Transitions.RemoveAll(item => item.X < 0 || item.X >= width || item.Y < 0 || item.Y >= height);
        MarkChanged();
        return true;
    }

    public void Undo()
    {
        if (Definition is null || _undo.Count == 0)
            return;

        _redo.Push(Snapshot());
        Restore(_undo.Pop());
    }

    public void Redo()
    {
        if (Definition?.Terrain is not { } terrain || _redo.Count == 0)
            return;

        _undo.Push(Snapshot());
        Restore(_redo.Pop());
    }

    public void PlaceObject(int x, int y, string definitionId)
    {
        if (Definition is null || string.IsNullOrWhiteSpace(definitionId)) return;
        PushSnapshot();
        Definition.Objects.Add(new MapObjectPlacement
        {
            Id = UniqueId(Definition.Objects.Select(item => item.Id), "object"),
            Definition = definitionId,
            X = x,
            Y = y
        });
        MarkChanged();
    }

    public void PlaceAnchor(int x, int y, string kind)
    {
        if (Definition is null || string.IsNullOrWhiteSpace(kind)) return;
        PushSnapshot();
        Definition.Anchors.Add(new MapAnchor
        {
            Id = UniqueId(Definition.Anchors.Select(item => item.Id), kind),
            Kind = kind,
            X = x,
            Y = y
        });
        MarkChanged();
    }

    public void PlaceTransition(int x, int y, string targetMap, string targetAnchor)
    {
        if (Definition is null || string.IsNullOrWhiteSpace(targetMap) || string.IsNullOrWhiteSpace(targetAnchor)) return;
        PushSnapshot();
        Definition.Transitions.Add(new MapTransitionDefinition
        {
            Id = UniqueId(Definition.Transitions.Select(item => item.Id), "transition"),
            X = x,
            Y = y,
            TargetMap = targetMap,
            TargetAnchor = targetAnchor
        });
        MarkChanged();
    }

    public void RemoveEntitiesAt(int x, int y)
    {
        if (Definition is null)
            return;

        var hasEntities = Definition.Objects.Any(item => item.X == x && item.Y == y) ||
                          Definition.Anchors.Any(item => item.X == x && item.Y == y) ||
                          Definition.Transitions.Any(item => item.X == x && item.Y == y);
        if (!hasEntities)
            return;

        PushSnapshotBeforeMutation();
        Definition.Objects.RemoveAll(item => item.X == x && item.Y == y);
        Definition.Anchors.RemoveAll(item => item.X == x && item.Y == y);
        Definition.Transitions.RemoveAll(item => item.X == x && item.Y == y);
        MarkChanged();
    }

    public void MoveEntitiesAt(int x, int y, int dx, int dy)
    {
        if (Definition is null)
            return;

        var nextX = x + dx;
        var nextY = y + dy;
        if (nextX < 0 || nextX >= Definition.Width || nextY < 0 || nextY >= Definition.Height)
            return;

        var objects = Definition.Objects.Where(item => item.X == x && item.Y == y).ToList();
        var anchors = Definition.Anchors.Where(item => item.X == x && item.Y == y).ToList();
        var transitions = Definition.Transitions.Where(item => item.X == x && item.Y == y).ToList();
        if (objects.Count == 0 && anchors.Count == 0 && transitions.Count == 0)
            return;

        PushSnapshotBeforeMutation();
        foreach (var item in objects)
            SetObjectPosition(item, nextX, nextY);
        foreach (var item in anchors)
            SetAnchorPosition(item, nextX, nextY);
        foreach (var item in transitions)
            SetTransitionPosition(item, nextX, nextY);
        MarkChanged();
    }

    public void UpdateEntitiesAt(int x, int y, string objectDefinition, string anchorKind,
        string targetMap, string targetAnchor)
    {
        if (Definition is null)
            return;

        var objects = Definition.Objects.Where(item => item.X == x && item.Y == y).ToList();
        var anchors = Definition.Anchors.Where(item => item.X == x && item.Y == y).ToList();
        var transitions = Definition.Transitions.Where(item => item.X == x && item.Y == y).ToList();
        if (objects.Count == 0 && anchors.Count == 0 && transitions.Count == 0)
            return;

        PushSnapshotBeforeMutation();
        if (!string.IsNullOrWhiteSpace(objectDefinition))
            foreach (var item in objects)
                item.Definition = objectDefinition;
        if (!string.IsNullOrWhiteSpace(anchorKind))
            foreach (var item in anchors)
                item.Kind = anchorKind;
        if (!string.IsNullOrWhiteSpace(targetMap) && !string.IsNullOrWhiteSpace(targetAnchor))
            foreach (var item in transitions)
            {
                item.TargetMap = targetMap;
                item.TargetAnchor = targetAnchor;
            }

        MarkChanged();
    }

    public void RemoveEntity(string type, string id)
    {
        if (Definition is null)
            return;

        var exists = type switch
        {
            "Object" => Definition.Objects.Any(item => item.Id == id),
            "Anchor" => Definition.Anchors.Any(item => item.Id == id),
            "Transition" => Definition.Transitions.Any(item => item.Id == id),
            _ => false
        };
        if (!exists)
            return;

        PushSnapshotBeforeMutation();
        switch (type)
        {
            case "Object": Definition.Objects.RemoveAll(item => item.Id == id); break;
            case "Anchor": Definition.Anchors.RemoveAll(item => item.Id == id); break;
            case "Transition": Definition.Transitions.RemoveAll(item => item.Id == id); break;
        }
        MarkChanged();
    }

    public void MoveEntity(string type, string id, int dx, int dy)
    {
        if (Definition is null)
            return;

        var position = type switch
        {
            "Object" => Definition.Objects.FirstOrDefault(item => item.Id == id) is { } item
                ? (item.X, item.Y) : ((int, int)?)null,
            "Anchor" => Definition.Anchors.FirstOrDefault(item => item.Id == id) is { } item
                ? (item.X, item.Y) : ((int, int)?)null,
            "Transition" => Definition.Transitions.FirstOrDefault(item => item.Id == id) is { } item
                ? (item.X, item.Y) : ((int, int)?)null,
            _ => null
        };
        if (position is not { } current)
            return;

        var nextX = current.Item1 + dx;
        var nextY = current.Item2 + dy;
        if (nextX < 0 || nextX >= Definition.Width || nextY < 0 || nextY >= Definition.Height)
            return;

        PushSnapshotBeforeMutation();
        switch (type)
        {
            case "Object" when Definition.Objects.FirstOrDefault(item => item.Id == id) is { } item:
                SetObjectPosition(item, nextX, nextY);
                break;
            case "Anchor" when Definition.Anchors.FirstOrDefault(item => item.Id == id) is { } item:
                SetAnchorPosition(item, nextX, nextY);
                break;
            case "Transition" when Definition.Transitions.FirstOrDefault(item => item.Id == id) is { } item:
                SetTransitionPosition(item, nextX, nextY);
                break;
            default:
                return;
        }

        MarkChanged();
    }

    public void UpdateEntity(string type, string id, string objectDefinition, string anchorKind,
        string targetMap, string targetAnchor)
    {
        if (Definition is null)
            return;

        var exists = type switch
        {
            "Object" => Definition.Objects.Any(item => item.Id == id),
            "Anchor" => Definition.Anchors.Any(item => item.Id == id),
            "Transition" => Definition.Transitions.Any(item => item.Id == id),
            _ => false
        };
        if (!exists)
            return;

        PushSnapshotBeforeMutation();
        switch (type)
        {
            case "Object" when Definition.Objects.FirstOrDefault(item => item.Id == id) is { } item:
                if (!string.IsNullOrWhiteSpace(objectDefinition)) item.Definition = objectDefinition;
                break;
            case "Anchor" when Definition.Anchors.FirstOrDefault(item => item.Id == id) is { } item:
                if (!string.IsNullOrWhiteSpace(anchorKind)) item.Kind = anchorKind;
                break;
            case "Transition" when Definition.Transitions.FirstOrDefault(item => item.Id == id) is { } item:
                if (!string.IsNullOrWhiteSpace(targetMap)) item.TargetMap = targetMap;
                if (!string.IsNullOrWhiteSpace(targetAnchor)) item.TargetAnchor = targetAnchor;
                break;
            default:
                return;
        }

        MarkChanged();
    }

    public void MarkClean()
    {
        IsDirty = false;
        OnPropertyChanged(nameof(TabTitle));
        _lastWriteTimeUtc = File.Exists(FilePath) ? File.GetLastWriteTimeUtc(FilePath) : default;
    }

    private void PushSnapshot() => _undo.Push(Snapshot());

    private void PushSnapshotBeforeMutation()
    {
        _undo.Push(Snapshot());
        _redo.Clear();
    }

    private string Snapshot() => JsonSerializer.Serialize(Definition);

    private void Restore(string snapshot, bool markChanged = true)
    {
        if (Definition is null) return;
        var restored = JsonSerializer.Deserialize<MapDefinition>(snapshot, SnapshotOptions);
        if (restored is null) return;
        Definition.Width = restored.Width;
        Definition.Height = restored.Height;
        Definition.Terrain.Rows = [.. restored.Terrain.Rows];
        Definition.Objects.Clear();
        Definition.Objects.AddRange(restored.Objects);
        Definition.Anchors.Clear();
        Definition.Anchors.AddRange(restored.Anchors);
        Definition.Transitions.Clear();
        Definition.Transitions.AddRange(restored.Transitions);
        if (markChanged)
            MarkChanged();
        else
            RefreshCells(Definition);
    }

    private void MarkChanged()
    {
        IsDirty = true;
        OnPropertyChanged(nameof(TabTitle));
        OnPropertyChanged(nameof(Summary));
        ValidationErrors.Clear();
        if (Definition is not null)
            foreach (var error in MapDefinitionValidator.Validate(Definition))
                ValidationErrors.Add(error);
        OnPropertyChanged(nameof(IsValid));
        RefreshCells(Definition!);
        Changed?.Invoke();
    }

    private static string UniqueId(IEnumerable<string> existingIds, string baseId)
    {
        var id = baseId;
        var suffix = 2;
        var taken = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);
        while (taken.Contains(id))
            id = $"{baseId}_{suffix++}";
        return id;
    }

    private static void SetObjectPosition(MapObjectPlacement source, int x, int y)
    {
        source.X = x;
        source.Y = y;
    }

    private static void SetAnchorPosition(MapAnchor source, int x, int y)
    {
        source.X = x;
        source.Y = y;
    }

    private static void SetTransitionPosition(MapTransitionDefinition source, int x, int y)
    {
        source.X = x;
        source.Y = y;
    }

    private void RefreshCells(MapDefinition definition)
    {
        Cells.Clear();
        if (definition.Terrain is null)
            return;

        for (var y = 0; y < definition.Height; y++)
        for (var x = 0; x < definition.Width; x++)
        {
            var glyph = y < definition.Terrain.Rows.Count && x < definition.Terrain.Rows[y].Length
                ? definition.Terrain.Rows[y][x]
                : '?';
            var terrainId = definition.Terrain.Legend.TryGetValue(glyph.ToString(), out var id) ? id : "invalid";
            Cells.Add(new MapCellViewModel(glyph, terrainId));
        }
    }
}

public sealed record MapCellViewModel(char Glyph, string TerrainId);
