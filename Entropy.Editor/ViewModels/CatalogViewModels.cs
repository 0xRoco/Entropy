using System.Collections.ObjectModel;
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
    protected ContentWorkspace Workspace { get; } = workspace;

    public ObservableCollection<T> Defs { get; } = new();
    [ObservableProperty] private T? _selected;
    [ObservableProperty] private ImageSource? _previewImage;

    public abstract string TypeName { get; }

    public void Reload(IEnumerable<T> defs)
    {
        Defs.Clear();
        foreach (var def in defs) Defs.Add(def);
        Selected = Defs.Count > 0 ? Defs[0] : null;
        UpdatePreview();
    }

    partial void OnSelectedChanged(T? value) => UpdatePreview();

    protected abstract string? SpriteKeyOf(T def);

    [RelayCommand]
    private void Add()
    {
        var def = CreateNew();
        if (def is null) return;
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
        Defs.Add(copy);
        Selected = copy;
    }

    [RelayCommand]
    private void Delete()
    {
        if (Selected is null) return;
        var index = Defs.IndexOf(Selected);
        Defs.RemoveAt(index);
        Selected = Defs.Count > 0 ? Defs[Math.Min(index, Defs.Count - 1)] : null;
        UpdatePreview();
    }

    protected abstract T? CreateNew();

    protected abstract T? Clone(T source);

    private void UpdatePreview()
    {
        PreviewImage = Selected is null
            ? null
            : Workspace.GetPreviewImage(SpriteKeyOf(Selected) ?? string.Empty);
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

    protected override ItemDefinition CreateNew() => new()
    {
        Id = UniqueId(Defs.Select(d => d.Id), "new_item"),
        Name = "New Item",
        Symbol = '?',
        Color = ColorNames.Parse("white"),
        Description = "",
        Category = "",
        Weight = 0,
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
        Stackable = source.Stackable,
        MaxStack = source.MaxStack,
        Effects = source.Effects.Select(e => e).ToList()
    };
}


public partial class CreaturesViewModel(ContentWorkspace workspace) : DefListViewModel<CreatureDefinition>(workspace)
{
    public override string TypeName => "Creature";

    protected override string? SpriteKeyOf(CreatureDefinition def) => "creature:" + def.Id;

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
    public override string TypeName => "Tileset";

    protected override string? SpriteKeyOf(TilesetDefinition def) => null; // preview not applicable

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

    protected override string? SpriteKeyOf(BuildingTemplate def) => null; // preview not applicable

    protected override BuildingTemplate CreateNew() => new()
    {
        Id = UniqueId(Defs.Select(d => d.Id), "new_building"),
        Name = "New Building",
        Grid = ["#####", "#...#", "##d##"],
        Legend = new Dictionary<char, string>
        {
            ['#'] = "wall_brick",
            ['.'] = "floor_wood",
            ['d'] = "door"
        },
        Anchors = new Dictionary<string, Vector2i>
        {
            ["entry"] = new(4, 2)
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
}
