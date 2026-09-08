using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Entropy.Content;
using Entropy.Editor.ViewModels;

namespace Entropy.Editor.Views;

public partial class ItemsEditorControl : UserControl
{
    private ItemsViewModel? Vm => DataContext as ItemsViewModel;
    private ItemsViewModel? _hookedVm;

    public ItemsEditorControl()
    {
        InitializeComponent();
        EffectAmountBox.Text = "5";
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

        if (DataContext is ItemsViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            _hookedVm = vm;
        }

        RefreshEffects();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ItemsViewModel.Selected))
            RefreshEffects();
    }

    private void OnAddHeal(object sender, RoutedEventArgs e) => AddEffect("heal");
    private void OnAddNourish(object sender, RoutedEventArgs e) => AddEffect("nourish");
    private void OnAddHydrate(object sender, RoutedEventArgs e) => AddEffect("hydrate");

    private void AddEffect(string kind)
    {
        var vm = Vm;
        if (vm?.Selected is null) return;
        if (!int.TryParse(EffectAmountBox.Text, out var amount) || amount <= 0) return;

        var effect = kind switch
        {
            "heal" => (ItemEffect)new ItemEffect.Heal(amount),
            "nourish" => new ItemEffect.Nourish(amount),
            "hydrate" => new ItemEffect.Hydrate(amount),
            _ => null
        };

        if (effect is not null)
            vm.Selected.Effects.Add(effect);

        RefreshEffects();
    }

    private void OnRemoveEffect(object sender, RoutedEventArgs e)
    {
        var vm = Vm;
        if (vm?.Selected is null) return;
        var index = EffectList.SelectedIndex;
        if (index < 0 || index >= vm.Selected.Effects.Count) return;

        vm.Selected.Effects.RemoveAt(index);
        RefreshEffects();
    }

    private void RefreshEffects()
    {
        var vm = Vm;
        if (vm?.Selected is null)
        {
            EffectList.ItemsSource = null;
            return;
        }

        EffectList.ItemsSource = vm.Selected.Effects.Select(effect => effect switch
        {
            ItemEffect.Heal heal => $"heal +{heal.Amount}",
            ItemEffect.Damage damage => $"damage +{damage.Amount}",
            ItemEffect.Nourish nourish => $"nourish +{nourish.Amount}",
            ItemEffect.Hydrate hydrate => $"hydrate +{hydrate.Amount}",
            _ => effect.ToString()
        }).ToList();
    }
}
