using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Definitions;
using Entropy.Game.Systems;
using Entropy.Game.UI;

namespace Entropy.Game;

class Program
{
    private static void Main(string[] args)
    { 
        using var game = new EntropyGame();
        var settings = new WindowSettings { Title = "Entropy Game", Width = 1920, Height = 1080 };
        using var window = new Window(game, settings);
        window.Run();
    }
}
