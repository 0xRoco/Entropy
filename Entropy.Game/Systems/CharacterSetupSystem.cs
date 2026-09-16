using Entropy.Content;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components.Identity;
using Entropy.Game.UI;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class CharacterSetupSystem
{
    public static void Apply(GameContext context, CharacterCatalog catalog, CharacterBuild character)
    {
        var scenario = catalog.Scenarios.Single(option => option.Id == character.ScenarioId);
        if (context.Maps.Maps.ContainsKey(scenario.StartMapId))
        {
            context.MapId = scenario.StartMapId;
            context.Map = context.Maps[scenario.StartMapId];
            context.Visibility = context.Visibilities[scenario.StartMapId];
            context.World.Set(context.Player, new Location { MapId = scenario.StartMapId });
            context.World.Get<Position>(context.Player).Value = new Vector2(scenario.StartX, scenario.StartY);
            Fov.Compute(new Vector2i(scenario.StartX, scenario.StartY), context.ViewRadius, context.Map,
                context.Visibility);
        }

        var profession = catalog.Professions.Single(option => option.Id == character.ProfessionId);
        var background = catalog.Backgrounds.Single(option => option.Id == character.BackgroundId);
        context.World.Set(context.Player, new CharacterIdentity
        {
            Name = character.Name,
            ProfessionId = profession.Id,
            BackgroundId = background.Id,
            Stats = new(character.Stats, StringComparer.OrdinalIgnoreCase),
            TraitIds = [.. character.TraitIds],
            SkillIds = [.. profession.Skills.Concat(background.Skills).Concat(character.SkillIds).Distinct()]
        });
        context.World.Set(context.Player, new Named { Name = character.Name });

        var position = context.World.Get<Position>(context.Player).Value;
        foreach (var itemId in profession.StartingItems.Concat(background.StartingItems))
        {
            var item = EntitySpawner.CreateItem(
                context.World,
                context.MapId,
                context.Definitions.Item(itemId),
                (int)position.X,
                (int)position.Y);
            ItemSystem.Transfer(context.World, item, context.Player);
        }

        context.Events.Publish(new SimEvent(
            "character.created",
            $"You are {character.Name}, a {profession.Name} from {background.Name}.",
            context.MapId,
            new Vector2i((int)position.X, (int)position.Y),
            context.Clock.MinuteOfDay,
            context.World.StableId(context.Player)));
        EventProjector.Project(context.Events, context.Log);
    }
}
