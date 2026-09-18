using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Entropy.Editor.Services;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Data;

namespace Entropy.Editor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<EditorSection> Sections { get; } =
    [
        new(0, "PROJECT", "Project"),
        new(1, "WORLD", "Maps"),
        new(2, "WORLD", "World Objects"),
        new(3, "WORLD", "Building Templates"),
        new(4, "DEFINITIONS", "Items"),
        new(5, "DEFINITIONS", "Creatures"),
        new(6, "DEFINITIONS", "Terrain"),
        new(7, "DEFINITIONS", "Tilesets"),
        new(8, "DIAGNOSTICS", "Diagnostics")
    ];

    public ICollectionView SectionsView { get; }

    public ContentWorkspace Workspace { get; } = new();
    public ObservableCollection<string> RecentProjects { get; } = new();

    public ItemsViewModel Items { get; }
    public CreaturesViewModel Creatures { get; }
    public TerrainViewModel Terrain { get; }
    public TilesetsViewModel Tilesets { get; }
    public BuildingsViewModel Buildings { get; }
    public WorldObjectsViewModel WorldObjects { get; }
    public MapWorkspaceViewModel Maps { get; } = new();

    [ObservableProperty] private string _statusText = "Open a content folder to begin.";
    [ObservableProperty] private ObservableCollection<string> _validationErrors = new();
    public ObservableCollection<DiagnosticEntry> Diagnostics { get; } = new();
    [ObservableProperty] private bool _hasErrors;
    [ObservableProperty] private bool _hasContent;
    [ObservableProperty] private int _selectedSectionIndex;
    [ObservableProperty] private string _searchText = string.Empty;

    public MainViewModel()
    {
        LoadRecentProjects();
        SectionsView = new ListCollectionView(Sections);
        SectionsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(EditorSection.Group)));

        Items = new ItemsViewModel(Workspace);
        Creatures = new CreaturesViewModel(Workspace);
        Terrain = new TerrainViewModel(Workspace);
        Tilesets = new TilesetsViewModel(Workspace);
        Buildings = new BuildingsViewModel(Workspace);
        WorldObjects = new WorldObjectsViewModel(Workspace);

        var detected = ContentWorkspace.TryFindContentFolder(AppContext.BaseDirectory);
        if (detected is not null)
            LoadFolder(detected);
    }

    partial void OnSearchTextChanged(string value)
    {
        Items.FilterText = value;
        Creatures.FilterText = value;
        Terrain.FilterText = value;
        Tilesets.FilterText = value;
        WorldObjects.FilterText = value;
        Buildings.FilterText = value;
    }

    private void LoadFolder(string folder)
    {
        try
        {
            Workspace.Load(folder);
            Maps.Load(folder);
            RememberRecentProject(folder);

            Items.Reload(Workspace.Items);
            Creatures.Reload(Workspace.Creatures);
            Terrain.Reload(Workspace.Terrains);
            Tilesets.Reload(Workspace.Tilesets);
            Buildings.Reload(Workspace.Buildings);
            WorldObjects.Reload(Workspace.WorldObjects);

            HasContent = true;
            ClearErrors();
            StatusText = $"Loaded {Workspace.Items.Count} items, " +
                         $"{Workspace.Creatures.Count} creatures, " +
                         $"{Workspace.Terrains.Count} terrains from {Workspace.FolderPath}";
        }
        catch (Exception ex)
        {
            StatusText = "Load failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Open the game's Content/Json folder"
        };

        if (dialog.ShowDialog() != true) return;

        LoadFolder(dialog.FolderName);
    }

    [RelayCommand]
    private void OpenRecent(string folder)
    {
        if (Directory.Exists(folder))
            LoadFolder(folder);
        else
        {
            RecentProjects.Remove(folder);
            SaveRecentProjects();
            StatusText = "That recent content folder no longer exists.";
        }
    }

    [RelayCommand]
    private void ReloadContent()
    {
        if (!Workspace.IsLoaded || string.IsNullOrWhiteSpace(Workspace.FolderPath))
        {
            StatusText = "No content folder open.";
            return;
        }

        LoadFolder(Workspace.FolderPath);
        if (HasContent)
            StatusText = $"Reloaded content from {Workspace.FolderPath}";
    }

    [RelayCommand]
    private void SaveAll()
    {
        if (!Workspace.IsLoaded)
        {
            StatusText = "No content folder open.";
            return;
        }

        var errors = ValidateAll();
        ShowErrors(errors);

        if (errors.Count > 0)
        {
            StatusText = $"Cannot save: {errors.Count} validation error(s). Fix them first.";
            return;
        }

        try
        {
            Workspace.SaveAll();
            Maps.SaveAll();
            StatusText = $"Saved {Workspace.Items.Count} items to {Workspace.FolderPath}";
        }
        catch (Exception ex)
        {
            StatusText = "Save failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private void Validate()
    {
        if (!Workspace.IsLoaded) return;

        var errors = ValidateAll();
        ShowErrors(errors);
        StatusText = errors.Count == 0
            ? "No validation errors."
            : $"{errors.Count} validation error(s).";
    }

    private void ShowErrors(List<string> errors)
    {
        ValidationErrors = new ObservableCollection<string>(errors);
        Diagnostics.Clear();
        foreach (var error in errors)
        {
            var separator = error.IndexOf(':');
            var source = separator > 0 ? error[..separator] : "Content";
            var message = separator > 0 ? error[(separator + 1)..].Trim() : error;
            Diagnostics.Add(new DiagnosticEntry("Error", source, message));
        }
        HasErrors = errors.Count > 0;
    }

    private void ClearErrors()
    {
        ValidationErrors.Clear();
        HasErrors = false;
    }

    private List<string> ValidateAll()
    {
        var errors = Workspace.Validate()
            .Select(error => $"Content: {error}")
            .ToList();
        errors.AddRange(Maps.Validate().Select(error => $"Map: {error}"));
        return errors;
    }

    private static string RecentProjectsFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Entropy", "recent-projects.txt");

    private void LoadRecentProjects()
    {
        try
        {
            if (!File.Exists(RecentProjectsFile))
                return;

            foreach (var path in File.ReadAllLines(RecentProjectsFile)
                         .Where(Directory.Exists)
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .Take(8))
                RecentProjects.Add(path);
        }
        catch (IOException)
        {
            // Recent projects are optional and must not prevent the editor from opening.
        }
    }

    private void RememberRecentProject(string folder)
    {
        RecentProjects.Remove(folder);
        RecentProjects.Insert(0, folder);
        while (RecentProjects.Count > 8)
            RecentProjects.RemoveAt(RecentProjects.Count - 1);
        SaveRecentProjects();
    }

    private void SaveRecentProjects()
    {
        try
        {
            var directory = Path.GetDirectoryName(RecentProjectsFile)!;
            Directory.CreateDirectory(directory);
            File.WriteAllLines(RecentProjectsFile, RecentProjects);
        }
        catch (IOException)
        {
            // Recent projects are optional and must not block normal editing.
        }
    }
}

public sealed record EditorSection(int Index, string Group, string Name);

public sealed record DiagnosticEntry(string Severity, string Source, string Message)
{
    public string Summary => $"{Source}: {Message}";
}
