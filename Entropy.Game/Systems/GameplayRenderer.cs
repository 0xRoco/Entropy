using Entropy.Content;
using Entropy.Engine.ECS;
using Entropy.Engine.Rendering;
using Entropy.Engine.UI;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.UI;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class GameplayRenderer
{
    public static void Draw(
        GameContext context,
        GameHud hud,
        Camera camera,
        QuadBatcher batcher,
        QuadBatcher terrainBatcher,
        QuadBatcher uiBatcher,
        FontAtlas fontAtlas,
        TilesetDefinition tileset,
        GlyphAtlas terrainAtlas,
        DefinitionRegistry definitions,
        Vector2i clientSize)
    {
        var origin = camera.ViewportOrigin;
        var size = camera.ViewportSize;
        GL.Viewport(origin.X, clientSize.Y - origin.Y - size.Y, size.X, size.Y);
        GL.Enable(EnableCap.ScissorTest);
        GL.Scissor(
            hud.Layout.Map.X * 8,
            clientSize.Y - (hud.Layout.Map.Y + hud.Layout.Map.Height) * 16,
            hud.Layout.Map.Width * 8,
            hud.Layout.Map.Height * 16);

        TileRenderer.Draw(
            context.Map,
            context.Visibility,
            camera,
            terrainBatcher,
            tilesetMode: tileset.Mode,
            artAtlas: terrainAtlas,
            artCellSize: (int)Camera.TilePixelSize,
            spriteMap: tileset.Sprites,
            terrainSpriteKeys: definitions.TerrainSpriteKeys);

        EntityRenderer.Draw(
            context.World,
            context.MapId,
            context.Visibility,
            batcher,
            terrainBatcher,
            entity => SpriteKeyOf(context.World, entity),
            tileset.Sprites,
            terrainAtlas,
            tileset.CellSize);

        GL.Disable(EnableCap.ScissorTest);
        GL.Viewport(0, 0, clientSize.X, clientSize.Y);
        hud.Draw(new DrawContext { Batcher = uiBatcher, Atlas = fontAtlas });
    }

    private static string? SpriteKeyOf(Entropy.Engine.ECS.World world, Entity entity)
    {
        if (world.Has<WorldObjectIdentity>(entity))
            return "furniture:" + world.Get<WorldObjectIdentity>(entity).DefinitionId;
        if (world.Has<CreatureIdentity>(entity))
            return "creature:" + world.Get<CreatureIdentity>(entity).DefinitionId;
        if (world.Has<Item>(entity) && world.Has<ItemIdentity>(entity))
            return "item:" + world.Get<ItemIdentity>(entity).DefinitionId;
        return null;
    }
}