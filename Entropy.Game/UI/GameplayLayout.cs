using Entropy.Engine.UI;
using OpenTK.Mathematics;

namespace Entropy.Game.UI;

public readonly record struct GameplayLayout(
    UiRect Map,
    UiRect Sidebar,
    UiRect Messages,
    UiRect Commands
)
{
    public static GameplayLayout Create(
        Vector2i viewportTiles,
        int sidebarWidth = 26,
        int messageHeight = 8,
        int commandHeight = 3)
    {
        var sidebar = Math.Min(sidebarWidth, Math.Max(0, viewportTiles.X - 1));
        var commands = Math.Min(commandHeight, Math.Max(0, viewportTiles.Y - 1));
        var messages = Math.Min(messageHeight, Math.Max(0, viewportTiles.Y - commands - 1));
        
        var mapWidth = Math.Max(1, viewportTiles.X - sidebar);
        var mapHeight = Math.Max(1, viewportTiles.Y - messages - commands);
        
        return new GameplayLayout(
            Map: new UiRect(0, 0, mapWidth, mapHeight),
            Sidebar: new UiRect(mapWidth, 0, sidebar, viewportTiles.Y),
            Messages: new UiRect(0, mapHeight, mapWidth, messages),
            Commands: new UiRect(0, mapHeight + messages, mapWidth, commands)
        );
    }
}