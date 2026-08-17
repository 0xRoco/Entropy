using Entropy.Engine.Core;

namespace Entropy.Game;

class Program
{
    private static void Main(string[] args)
    {
        using var game = new EntropyGame();
        var settings = new WindowSettings {Title = "Entropy Game", Width = 1920, Height = 1080};
        using var window = new Window(game, settings);
        window.Run();
    }
}