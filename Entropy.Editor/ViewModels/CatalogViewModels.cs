using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Entropy.Content;
using Entropy.Editor.Services;
using OpenTK.Mathematics;

namespace Entropy.Editor.ViewModels;

public abstract partial class DefListViewModel<T>(ContentWorkspace workspace) : ObservableObject
    where T : class
{
    public ContentWorkspace Workspace { get; } = workspace;

    public ObservableCollection<T> Defs { get; } = new();
    public ICollectionView VisibleDefs => _visibleDefs ??= CreateView();
    [ObservableProperty] private T? _selected;
    [ObservableProperty] private ImageSource? _previewImage;
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private string _sortProperty = "Id";
    [ObservableProperty] private ListSortDirection _sortDirection = ListSortDirection.Ascending;

    private ICollectionView? _visibleDefs;

    public abstract string TypeName { get; }

    public void Reload(IEnumerable<T> defs)
    {
        Defs.Clear();
        foreach (var def in defs) Defs.Add(def);
        Selected = Defs.Count > 0 ? Defs[0] : null;
        VisibleDefs.Refresh();
        UpdatePreview();
    }

    partial void OnSelectedChanged(T? value)
    {
        UpdatePreview();
        SelectedChanged(value);
    }

    protected virtual void SelectedChanged(T? value) { }

    partial void OnFilterTextChanged(string value) => VisibleDefs.Refresh();

    [RelayCommand]
    private void Sort(string property)
    {
        if (string.Equals(SortProperty, property, StringComparison.Ordinal))
            SortDirection = SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        else
        {
            SortProperty = property;
            SortDirection = ListSortDirection.Ascending;
        }

        ApplySort();
    }

    protected abstract string? SpriteKeyOf(T def);

    [RelayCommand]
    private void Add()
    {
        var def = CreateNew();
        if (def is null) return;
        AddToWorkspace(def);
        Defs.Add(def);
        Selected = def;
        UpdatePreview();
    }

    [RelayCommand]
    private void Duplicate()
    {
        if (Selected is null) return;
        var copy = Clone(Selected);
        if (copy is null) return;
        AddToWorkspace(copy);
        Defs.Add(copy);
        Selected = copy;
    }

    [RelayCommand]
    private void Delete()
    {
        if (Selected is null) return;
        var index = Defs.IndexOf(Selected);
        RemoveFromWorkspace(Selected);
        Defs.RemoveAt(index);
        Selected = Defs.Count > 0 ? Defs[Math.Min(index, Defs.Count - 1)] : null;
        UpdatePreview();
    }

    protected abstract void AddToWorkspace(T def);
    protected abstract void RemoveFromWorkspace(T def);

    protected abstract T? CreateNew();

    protected abstract T? Clone(T source);

    private void UpdatePreview()
    {
        PreviewImage = Selected is null
            ? null
            : Workspace.GetPreviewImage(SpriteKeyOf(Selected) ?? string.Empty);
    }

    private ICollectionView CreateView()
    {
        var view = new ListCollectionView(Defs);
        view.Filter = item => string.IsNullOrWhiteSpace(FilterText) || MatchesFilter(item, FilterText);
        view.SortDescriptions.Add(new SortDescription(SortProperty, SortDirection));
        return view;
    }

    partial void OnSortPropertyChanged(string value) => ApplySort();

    partial void OnSortDirectionChanged(ListSortDirection value) => ApplySort();

    private void ApplySort()
    {
        if (_visibleDefs is null)
            return;

        _visibleDefs.SortDescriptions.Clear();
        _visibleDefs.SortDescriptions.Add(new SortDescription(SortProperty, SortDirection));
        _visibleDefs.Refresh();
    }

    private static bool MatchesFilter(object? item, string filter)
    {
        if (item is null)
            return false;

        var type = item.GetType();
        return new[] { "Id", "Name", "Description" }
            .Select(property => type.GetProperty(property)?.GetValue(item)?.ToString())
            .Any(value => value?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true);
    }

    protected static string UniqueId(IEnumerable<string> existingIds, string baseId)
    {
        var id = baseId;
        var suffix = 2;
        var taken = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);
        while (taken.Contains(id))
            id = $"{baseId}_{suffix++}";
        return id;
    }
}

public partial class ItemsViewModel(ContentWorkspace workspace) : DefListViewModel<ItemDefinition>(workspace)
{
    public override string TypeName => "Item";

    protected override string SpriteKeyOf(ItemDefinition def) => "item:" + def.Id;

    protected override void AddToWorkspace(ItemDefinition def) => Workspace.Items.Add(def);
    protected override void RemoveFromWorkspace(ItemDefinition def) => Workspace.Items.Remove(def);

    protected override ItemDefinition CreateNew() => new()
    {
        Id = UniqueId(Defs.Select(d => d.Id), "new_item"),
        Name = "New Item",
        Symbol = '?',
        Color = ColorNames.Parse("white"),
        Description = "",
        Category = "",
        Weight = 0,
        PriceCents = 0,
        Stackable = true,
        MaxStack = 10
    };

    protected override ItemDefinition Clone(ItemDefinition source) => new()
    {
        Comment = source.Comment,
        Id = UniqueId(Defs.Select(d => d.Id), source.Id),
        Name = source.Name,
        Symbol = source.Symbol,
        Color = source.Color,
        Description = source.Description,
        Category = source.Category,
        Materials = new List<Material>(source.Materials),
        Flags = new HashSet<string>(source.Flags, StringComparer.OrdinalIgnoreCase),
        Weight = source.Weight,
        PriceCents = source.PriceCents,
        Stackable = source.Stackable,
        MaxStack = source.MaxStack,
        Effects = source.Effects.Select(e => e).ToList()
    };
}


public partial class CreaturesViewModel(ContentWorkspace workspace) : DefListViewModel<CreatureDefinition>(workspace)
{
    public override string TypeName => "Creature";

    protected override string? SpriteKeyOf(CreatureDefinition def) => "creature:" + def.Id;

    protected override void AddToWorkspace(CreatureDefinition def) => Workspace.Creatures.Add(def);
    protected override void RemoveFromWorkspace(CreatureDefinition def) => Workspace.Creatures.Remove(def);

    protected override CreatureDefinition CreateNew() => new()
    {
        Id = UniqueId(Defs.Select(d => d.Id), "new_creature"),
        Name = "New Creature",
        Symbol = '?',
        Color = ColorNames.Parse("gray"),
        Health = 5,
        Speed = 100,
        SightRadius = 5,
        SmellRadius = 0,
        Behavior = "wander",
        Hostile = false
    };

    protected override CreatureDefinition Clone(CreatureDefinition source) => new()
    {
        Comment = source.Comment,
        Id = UniqueId(Defs.Select(d => d.Id), source.Id),
        Name = source.Name,
        Symbol = source.Symbol,
        Color = source.Color,
        Health = source.Health,
        Speed = source.Speed,
        SightRadius = source.SightRadius,
        SmellRadius = source.SmellRadius,
        Behavior = source.Behavior,
        Hostile = source.Hostile
    };
}


public partial class TerrainViewModel(ContentWorkspace workspace) : DefListViewModel<TerrainDefinition>(workspace)
{
    public override string TypeName => "Terrain";

    protected override string? SpriteKeyOf(TerrainDefinition def) => "terrain:" + def.Id;

    protected override void AddToWorkspace(TerrainDefinition def) => Workspace.Terrains.Add(def);
    protected override void RemoveFromWorkspace(TerrainDefinition def) => Workspace.Terrains.Remove(def);

    protected override TerrainDefinition CreateNew() => new()
    {
        Id = UniqueId(Defs.Select(d => d.Id), "new_terrain"),
        Name = "New Terrain",
        Symbol = '.',
        Color = ColorNames.Parse("gray"),
        Background = ColorNames.Parse("black"),
        Walkable = true,
        Opaque = false,
        MoveCost = 100
    };

    protected override TerrainDefinition Clone(TerrainDefinition source) => new()
    {
        Comment = source.Comment,
        Id = UniqueId(Defs.Select(d => d.Id), source.Id),
        Name = source.Name,
        Symbol = source.Symbol,
        Color = source.Color,
        Background = source.Background,
        Description = source.Description,
        Category = source.Category,
        Flags = new HashSet<string>(source.Flags, StringComparer.OrdinalIgnoreCase),
        Walkable = source.Walkable,
        Opaque = source.Opaque,
        MoveCost = source.MoveCost
    };
}

public partial class TilesetsViewModel(ContentWorkspace workspace) : DefListViewModel<TilesetDefinition>(workspace)
{
    public ObservableCollection<string> SpriteKeys { get; } = new();

    [ObservableProperty] private ImageSource? _atlasImage;
    [ObservableProperty] private string? _selectedSpriteKey;
    [ObservableProperty] private string _newSpriteKey = string.Empty;
    [ObservableProperty] private int _newSpriteX;
    [ObservableProperty] private int _newSpriteY;
    [ObservableProperty] private double _atlasWidth = 1;
    [ObservableProperty] private double _atlasHeight = 1;
    [ObservableProperty] private double _highlightX;
    [ObservableProperty] private double _highlightY;
    [ObservableProperty] private double _highlightOpacity;
    [ObservableProperty] private string _spriteKeyEdit = string.Empty;
    [ObservableProperty] private int _spriteXEdit;
    [ObservableProperty] private int _spriteYEdit;
    [ObservableProperty] private string _asciiPreviewText = string.Empty;
    [ObservableProperty] private double _asciiPreviewOpacity;

    public override string TypeName => "Tileset";

    protected override void SelectedChanged(TilesetDefinition? value)
    {
        AtlasImage = value is null ? null : Workspace.GetAtlasImage(value);
        if (AtlasImage is System.Windows.Media.Imaging.BitmapSource bitmap)
        {
            AtlasWidth = bitmap.PixelWidth;
            AtlasHeight = bitmap.PixelHeight;
        }
        else
        {
            AtlasWidth = 1;
            AtlasHeight = 1;
        }
        SpriteKeys.Clear();
        if (value is not null)
            foreach (var key in value.Sprites.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
                SpriteKeys.Add(key);
        SelectedSpriteKey = SpriteKeys.FirstOrDefault();
        AsciiPreviewText = string.Equals(value?.Mode, "ascii", StringComparison.OrdinalIgnoreCase)
            ? BuildAsciiPreview()
            : string.Empty;
        AsciiPreviewOpacity = string.IsNullOrEmpty(AsciiPreviewText) ? 0 : 1;
    }

    partial void OnSelectedSpriteKeyChanged(string? value)
    {
        if (Selected is not null && value is not null && Selected.Sprites.TryGetValue(value, out var cell))
        {
            SpriteKeyEdit = value;
            SpriteXEdit = cell.X;
            SpriteYEdit = cell.Y;
            HighlightX = cell.X * Selected.CellSize;
            HighlightY = cell.Y * Selected.CellSize;
            HighlightOpacity = 1;
        }
        else
        {
            SpriteKeyEdit = string.Empty;
            SpriteXEdit = 0;
            SpriteYEdit = 0;
            HighlightOpacity = 0;
        }
    }

    [RelayCommand]
    private void UpdateSprite()
    {
        if (Selected is null || string.IsNullOrWhiteSpace(SelectedSpriteKey) || string.IsNullOrWhiteSpace(SpriteKeyEdit)) return;
        var oldKey = SelectedSpriteKey;
        var newKey = SpriteKeyEdit.Trim();
        Selected.Sprites.Remove(oldKey);
        Selected.Sprites[newKey] = new Vector2i(SpriteXEdit, SpriteYEdit);
        var index = SpriteKeys.IndexOf(oldKey);
        SpriteKeys.Remove(oldKey);
        if (index < 0 || index > SpriteKeys.Count) index = SpriteKeys.Count;
        SpriteKeys.Insert(index, newKey);
        SelectedSpriteKey = newKey;
    }

    private static string BuildAsciiPreview()
    {
        const string glyphs = "@#$%&*+=-:.ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return string.Join(Environment.NewLine, Enumerable.Range(0, 8)
            .Select(row => new string(Enumerable.Range(0, 16)
                .Select(column => glyphs[(row * 16 + column) % glyphs.Length]).ToArray())));
    }

    [RelayCommand]
    private void AddSprite()
    {
        if (Selected is null || string.IsNullOrWhiteSpace(NewSpriteKey)) return;
        Selected.Sprites[NewSpriteKey.Trim()] = new Vector2i(NewSpriteX, NewSpriteY);
        if (!SpriteKeys.Contains(NewSpriteKey.Trim(), StringComparer.OrdinalIgnoreCase))
            SpriteKeys.Add(NewSpriteKey.Trim());
        SelectedSpriteKey = NewSpriteKey.Trim();
        NewSpriteKey = string.Empty;
    }

    [RelayCommand]
    private void RemoveSprite()
    {
        if (Selected is null || string.IsNullOrWhiteSpace(SelectedSpriteKey)) return;
        Selected.Sprites.Remove(SelectedSpriteKey);
        SpriteKeys.Remove(SelectedSpriteKey);
        SelectedSpriteKey = SpriteKeys.FirstOrDefault();
    }

    protected override string? SpriteKeyOf(TilesetDefinition def) => null; // preview not applicable

    protected override void AddToWorkspace(TilesetDefinition def) => Workspace.Tilesets.Add(def);
    protected override void RemoveFromWorkspace(TilesetDefinition def) => Workspace.Tilesets.Remove(def);

    protected override TilesetDefinition CreateNew() => new()
    {
        Id = UniqueId(Defs.Select(d => d.Id), "new_tileset"),
        Mode = "ascii",
        Atlas = "",
        CellSize = 16
    };

    protected override TilesetDefinition Clone(TilesetDefinition source) => new()
    {
        Comment = source.Comment,
        Id = UniqueId(Defs.Select(d => d.Id), source.Id),
        Mode = source.Mode,
        Atlas = source.Atlas,
        CellSize = source.CellSize,
        Sprites = new Dictionary<string, Vector2i>(source.Sprites)
    };
}

public partial class BuildingsViewModel(ContentWorkspace workspace) : DefListViewModel<BuildingTemplate>(workspace)
{
    public override string TypeName => "Building";

    [ObservableProperty] private TerrainDefinition? _selectedTerrain;

    [ObservableProperty] private string? _placingAnchor;

    [ObservableProperty] private int _zoom = 1;

    public int CellPixels => 28 * Zoom;
    public double GlyphPixels => 8 + 8 * Zoom;

    partial void OnZoomChanged(int value)
    {
        OnPropertyChanged(nameof(CellPixels));
        OnPropertyChanged(nameof(GlyphPixels));
    }

    private const string LegendCharset =
        "#.d,;:o*+~=xX-abcdefgijklmnopqrstuvwxyzABCDEFHIJKLMNOPQRSTUVWXYZ0123456789";

    protected override string? SpriteKeyOf(BuildingTemplate def) => null; // preview not applicable

    protected override void AddToWorkspace(BuildingTemplate def) => Workspace.Buildings.Add(def);
    protected override void RemoveFromWorkspace(BuildingTemplate def) => Workspace.Buildings.Remove(def);

    protected override BuildingTemplate CreateNew() => new()
    {
        Id = UniqueId(Defs.Select(d => d.Id), "new_building"),
        Name = "New Building",
        Grid =
        [
            "############",
            "#..........#",
            "#..........#",
            "#..........#",
            "#..........#",
            "#..........#",
            "#..........#",
            "######d#####"
        ],
        Legend = new Dictionary<char, string>
        {
            ['#'] = "wall_brick",
            ['.'] = "floor_linoleum",
            ['d'] = "door"
        },
        Anchors = new Dictionary<string, Vector2i>
        {
            ["entry"] = new(6, 7)
        }
    };

    protected override BuildingTemplate Clone(BuildingTemplate source) => new()
    {
        Comment = source.Comment,
        Id = UniqueId(Defs.Select(d => d.Id), source.Id),
        Name = source.Name,
        Grid = new List<string>(source.Grid),
        Legend = new Dictionary<char, string>(source.Legend),
        Anchors = new Dictionary<string, Vector2i>(source.Anchors)
    };

    public void PaintCell(int x, int y, TerrainDefinition? terrain)
    {
        if (Selected is null || terrain is null) return;
        if (y < 0 || y >= Selected.Grid.Count) return;
        var row = Selected.Grid[y];
        if (x < 0 || x >= row.Length) return;

        var marker = LegendMarkerFor(Selected, terrain.Id);
        if (row[x] == marker) return;

        var old = row[x];
        Selected.Grid[y] = row[..x] + marker + row[(x + 1)..];
        PruneUnusedLegend(Selected);
    }

    public void Resize(int width, int height)
    {
        if (Selected is null || Selected.Grid.Count == 0) return;
        width = Math.Clamp(width, 3, 64);
        height = Math.Clamp(height, 3, 64);

        var pad = Selected.Grid
            .SelectMany(r => r)
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault(Selected.Legend.Keys.FirstOrDefault('#'));

        var rows = new List<string>();
        for (var y = 0; y < height; y++)
        {
            var source = y < Selected.Grid.Count ? Selected.Grid[y] : string.Empty;
            rows.Add(source.Length >= width
                ? source[..width]
                : source + new string(pad, width - source.Length));
        }
        Selected.Grid = rows;

        var clamped = Selected.Anchors.ToDictionary(
            a => a.Key,
            a => new Vector2i(
                Math.Clamp(a.Value.X, 0, width - 1),
                Math.Clamp(a.Value.Y, 0, height - 1)));
        Selected.Anchors.Clear();
        foreach (var (name, pos) in clamped)
            Selected.Anchors[name] = pos;

        PruneUnusedLegend(Selected);
    }

    public void PlaceAnchor(string name, int x, int y)
    {
        if (Selected is null || Selected.Grid.Count == 0) return;
        x = Math.Clamp(x, 0, Selected.Grid[0].Length - 1);
        y = Math.Clamp(y, 0, Selected.Grid.Count - 1);
        Selected.Anchors[name] = new Vector2i(x, y);
    }

    public bool AddAnchor(string name)
    {
        if (Selected is null || string.IsNullOrWhiteSpace(name) || Selected.Anchors.ContainsKey(name))
            return false;
        Selected.Anchors[name] = new Vector2i(0, 0);
        return true;
    }

    public void DeleteAnchor(string name) => Selected?.Anchors.Remove(name);

    private char LegendMarkerFor(BuildingTemplate def, string terrainId)
    {
        foreach (var (marker, id) in def.Legend)
            if (string.Equals(id, terrainId, StringComparison.OrdinalIgnoreCase))
                return marker;

        foreach (var candidate in LegendCharset)
        {
            if (def.Legend.ContainsKey(candidate)) continue;
            def.Legend[candidate] = terrainId;
            return candidate;
        }

        throw new InvalidOperationException($"Building template '{def.Id}' exhausted the legend charset.");
    }

    private static void PruneUnusedLegend(BuildingTemplate def)
    {
        var used = def.Grid.SelectMany(r => r).ToHashSet();
        foreach (var marker in def.Legend.Keys.Where(m => !used.Contains(m)).ToList())
            def.Legend.Remove(marker);
    }
}

public partial class WorldObjectsViewModel(ContentWorkspace workspace) : DefListViewModel<WorldObjectDefinition>(workspace)
{
    public override string TypeName => "World Object";

    protected override string? SpriteKeyOf(WorldObjectDefinition def) => "furniture:" + def.Id;

    protected override void AddToWorkspace(WorldObjectDefinition def) => Workspace.WorldObjects.Add(def);
    protected override void RemoveFromWorkspace(WorldObjectDefinition def) => Workspace.WorldObjects.Remove(def);

    protected override WorldObjectDefinition CreateNew() => new()
    {
        Id = UniqueId(Defs.Select(d => d.Id), "new_object"),
        Name = "New Object",
        Symbol = '&',
        Color = ColorNames.Parse("gray"),
        Description = "",
        Flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        ContainerSlots = 0,
        StarterItems = []
    };

    protected override WorldObjectDefinition Clone(WorldObjectDefinition source) => new()
    {
        Comment = source.Comment,
        Id = UniqueId(Defs.Select(d => d.Id), source.Id),
        Name = source.Name,
        Symbol = source.Symbol,
        Color = source.Color,
        Description = source.Description,
        Flags = new HashSet<string>(source.Flags, StringComparer.OrdinalIgnoreCase),
        ContainerSlots = source.ContainerSlots,
        StarterItems = new List<string>(source.StarterItems)
    };
}
