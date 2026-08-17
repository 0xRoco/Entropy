namespace Entropy.Engine.Core;

public class WindowSettings
{
    public string Title { get; set; } = "Entropy Engine";
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public bool VSync { get; set; } = true;
    public double UpdateFrequency { get; set; } = 60;
}