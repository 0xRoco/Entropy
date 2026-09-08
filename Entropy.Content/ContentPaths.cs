namespace Entropy.Content;

public static class ContentPaths
{
    public static string? TryFindContentRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Entropy.Game", "Content");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        var local = Path.Combine(startDir, "Content");
        return Directory.Exists(local) ? local : null;
    }

    public static string? TryFindJsonFolder(string startDir) =>
        TryFindContentRoot(startDir) is { } root
            ? Path.Combine(root, "Json")
            : null;
}
