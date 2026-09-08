using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Entropy.Editor.Services;
using Microsoft.Win32;

namespace Entropy.Editor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ContentWorkspace Workspace { get; } = new();

    public ItemsViewModel Items { get; }
    public CreaturesViewModel Creatures { get; }
    public TerrainViewModel Terrain { get; }
    public TilesetsViewModel Tilesets { get; }
    public BuildingsViewModel Buildings { get; }

    [ObservableProperty] private string _statusText = "Open a content folder to begin.";
    [ObservableProperty] private ObservableCollection<string> _validationErrors = new();
    [ObservableProperty] private bool _hasErrors;
    [ObservableProperty] private bool _hasContent;

    public MainViewModel()
    {
        Items = new ItemsViewModel(Workspace);
        Creatures = new CreaturesViewModel(Workspace);
        Terrain = new TerrainViewModel(Workspace);
        Tilesets = new TilesetsViewModel(Workspace);
        Buildings = new BuildingsViewModel(Workspace);

        var detected = ContentWorkspace.TryFindContentFolder(AppContext.BaseDirectory);
        if (detected is not null)
            LoadFolder(detected);
    }

    private void LoadFolder(string folder)
    {
        try
        {
            Workspace.Load(folder);

            Items.Reload(Workspace.Items);
            Creatures.Reload(Workspace.Creatures);
            Terrain.Reload(Workspace.Terrains);
            Tilesets.Reload(Workspace.Tilesets);
            Buildings.Reload(Workspace.Buildings);

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
    private void SaveAll()
    {
        if (!Workspace.IsLoaded)
        {
            StatusText = "No content folder open.";
            return;
        }

        var errors = Workspace.Validate();
        ShowErrors(errors);

        if (errors.Count > 0)
        {
            StatusText = $"Cannot save: {errors.Count} validation error(s). Fix them first.";
            return;
        }

        try
        {
            Workspace.SaveAll();
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

        var errors = Workspace.Validate();
        ShowErrors(errors);
        StatusText = errors.Count == 0
            ? "No validation errors."
            : $"{errors.Count} validation error(s).";
    }

    private void ShowErrors(List<string> errors)
    {
        ValidationErrors = new ObservableCollection<string>(errors);
        HasErrors = errors.Count > 0;
    }

    private void ClearErrors()
    {
        ValidationErrors.Clear();
        HasErrors = false;
    }
}
