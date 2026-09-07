using Entropy.Engine.ECS.Components;
using Entropy.Engine.Rendering.Options;
using Entropy.Engine.World;

namespace Entropy.Engine.Rendering;

public static class EntityRenderer
{
    public static void Draw(
        ECS.World world,
        string mapId,
        VisibilityMap visibility,
        QuadBatcher batcher,
        EntityRenderOptions? option = null)
    {
        var options = option ?? new EntityRenderOptions();

        foreach (var entity in world.Query<Position, Glyph>())
        {
            if (world.Has<Location>(entity) &&
                world.Get<Location>(entity).MapId != mapId)
            {
                continue;
            }

            ref var position = ref world.Get<Position>(entity);

            if (options.CullByVisibility &&
                !visibility.IsVisible((int)position.Value.X, (int)position.Value.Y))
            {
                continue;
            }

            ref var glyph = ref world.Get<Glyph>(entity);

            batcher.AddTexturedQuad(
                (int)position.Value.X,
                (int)position.Value.Y,
                1,
                1,
                glyph.Foreground,
                glyph.Character);
        }

        batcher.Flush();
    }
}