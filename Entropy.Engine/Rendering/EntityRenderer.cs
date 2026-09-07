using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.Rendering.Options;
using Entropy.Engine.World;
using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public static class EntityRenderer
{
    public static void Draw(
        ECS.World world,
        string mapId,
        VisibilityMap visibility,
        QuadBatcher glyphBatcher,
        QuadBatcher? spriteBatcher,
        Func<ECS.Entity, string?>? spriteKeyOf,
        IReadOnlyDictionary<string, Vector2i>? sprites,
        GlyphAtlas? spriteAtlas,
        int spriteCellSize,
        EntityRenderOptions? option = null)
    {
        var options = option ?? new EntityRenderOptions();
        var useSprites = spriteBatcher != null && sprites is { Count: > 0 } && spriteAtlas != null;

        foreach (var entity in world.Query<Position, Glyph>())
        {
            if (world.Has<Location>(entity) &&
                world.Get<Location>(entity).MapId != mapId)
            {
                continue;
            }

            ref var position = ref world.Get<Position>(entity);
            var visible = !options.CullByVisibility ||
                visibility.IsVisible((int)position.Value.X, (int)position.Value.Y);

            if (useSprites)
            {
                var key = spriteKeyOf?.Invoke(entity);

                if (key != null && sprites!.TryGetValue(key, out var cell))
                {
                    var tint = visible ? Color4.White : Color4.White.Scaled(options.MemoryDim);                    var uvMin = new Vector2(
                        cell.X * spriteCellSize / (float)spriteAtlas!.Width,
                        cell.Y * spriteCellSize / (float)spriteAtlas!.Height);
                    var uvMax = new Vector2(
                        (cell.X + 1) * spriteCellSize / (float)spriteAtlas.Width,
                        (cell.Y + 1) * spriteCellSize / (float)spriteAtlas.Height);

                    spriteBatcher!.AddTexturedQuad(
                        position.Value.X,
                        position.Value.Y,
                        1,
                        1,
                        tint,
                        uvMin,
                        uvMax);
                    continue;
                }
            }

            if (!visible) continue;

            ref var glyph = ref world.Get<Glyph>(entity);
            glyphBatcher.AddTexturedQuad(
                (int)position.Value.X,
                (int)position.Value.Y,
                1,
                1,
                glyph.Foreground,
                glyph.Character);
        }

        glyphBatcher.Flush();
        spriteBatcher?.Flush();
    }
}
