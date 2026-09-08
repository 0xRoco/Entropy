using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Entropy.Content;
using Entropy.Content.Loading;
using Entropy.Content.Validation;
using Entropy.Content.Writing;

namespace Entropy.Editor.Services;


public partial class ContentWorkspace : ObservableObject
{
    private BitmapImage? _artImage;
    private string? _artImagePath;

    [ObservableProperty] private string? _folderPath;
    [ObservableProperty] private string? _statusText = "Open a content folder to begin.";

    public ObservableCollection<ItemDefinition> Items { get; } = new();
    public ObservableCollection<CreatureDefinition> Creatures { get; } = new();
    public ObservableCollection<TerrainDefinition> Terrains { get; } = new();
    public ObservableCollection<TilesetDefinition> Tilesets { get; } = new();
    public ObservableCollection<BuildingTemplate> Buildings { get; } = new();

    public TilesetDefinition? ArtTileset => Tilesets.FirstOrDefault(t => t.Mode == "art");

    public bool IsLoaded => FolderPath is not null;
    
    public static string? TryFindContentFolder(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName, "Entropy.Game", "Content", "Json");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    public ImageSource? GetPreviewImage(string spriteKey)
    {
        var art = ArtTileset;
        if (art is null ||
            !art.Sprites.TryGetValue(spriteKey, out var cell) ||
            FolderPath is null)
        {
            return null;
        }
        
        var contentRoot = Directory.GetParent(FolderPath)!.FullName;
        var gameRoot = Directory.GetParent(contentRoot)!.FullName;
        var atlasPath = Path.Combine(gameRoot, art.Atlas.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(atlasPath)) return null;

        if (_artImagePath != atlasPath || _artImage is null)
        {
            _artImage = new BitmapImage();
            _artImage.BeginInit();
            _artImage.CacheOption = BitmapCacheOption.OnLoad;
            _artImage.UriSource = new Uri(atlasPath);
            _artImage.EndInit();
            _artImage.Freeze();
            _artImagePath = atlasPath;
        }

        return new CroppedBitmap(_artImage, new System.Windows.Int32Rect(
            cell.X * art.CellSize, cell.Y * art.CellSize, art.CellSize, art.CellSize));
    }

    public void Load(string folder)
    {
        FolderPath = folder;

        Replace(Items, DefinitionLoader.LoadItems(folder));
        Replace(Creatures, DefinitionLoader.LoadCreatures(folder));
        Replace(Terrains, DefinitionLoader.LoadTerrains(folder));
        Replace(Tilesets, DefinitionLoader.LoadTilesets(folder));
        Replace(Buildings, DefinitionLoader.LoadBuildingTemplates(folder));

        _artImage = null;
        _artImagePath = null;
        OnPropertyChanged(nameof(ArtTileset));
    }

    public void SaveAll()
    {
        if (FolderPath is null)
            throw new InvalidOperationException("No content folder open.");

        var itemsPath = Path.Combine(FolderPath, "items.json");
        var creaturesPath = Path.Combine(FolderPath, "creatures.json");
        var terrainPath = Path.Combine(FolderPath, "terrain.json");
        var tilesetPath = Path.Combine(FolderPath, "tileset.json");
        var buildingsPath = Path.Combine(FolderPath, "building_templates.json");

        DefinitionWriter.WriteItems(itemsPath, Items);
        DefinitionWriter.WriteCreatures(creaturesPath, Creatures);
        DefinitionWriter.WriteTerrains(terrainPath, Terrains);
        DefinitionWriter.WriteTilesets(tilesetPath, Tilesets);
        DefinitionWriter.WriteBuildingTemplates(buildingsPath, Buildings);
    }

    public List<string> Validate() =>
        DefinitionValidator.Validate(Items, Creatures, Terrains, Tilesets, Buildings);

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }
}
