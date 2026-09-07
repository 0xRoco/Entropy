using Entropy.Engine.World;
using Entropy.Game.Definitions;
using OpenTK.Mathematics;

namespace Entropy.Game.WorldGen;

public static class CityBlockGenerator
{
    public const string StreetMapId = "block_street";

    public static CityBlock Generate(DefinitionRegistry defs)
    {
        var maps = new MapGraph();
        var street = new TileMap(60, 40);

        Fill(street, defs.TileOf("grass"));

        FillRect(street, 0, 18, street.Width, 4, defs.TileOf("road_asphalt"));
        FillRect(street, 0, 16, street.Width, 2, defs.TileOf("sidewalk"));
        FillRect(street, 0, 22, street.Width, 2, defs.TileOf("sidewalk"));

        maps.AddMap(StreetMapId, street);

        var buildings = new Dictionary<string, BuildingInstance>();
        
        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "corner_store",
            templateId: "corner_store",
            mapId: "store_interior",
            exteriorX: 4,
            exteriorY: 6);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "apartment_dana",
            templateId: "apartment_small",
            mapId: "apartment_dana_interior",
            exteriorX: 22,
            exteriorY: 8);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "apartment_marcus",
            templateId: "apartment_small",
            mapId: "apartment_marcus_interior",
            exteriorX: 40,
            exteriorY: 8);

        return new CityBlock(maps, StreetMapId, buildings);
    }

    private static void AddBuilding(
        MapGraph maps,
        Dictionary<string, BuildingInstance> buildings,
        DefinitionRegistry defs,
        TileMap street,
        string instanceId,
        string templateId,
        string mapId,
        int exteriorX,
        int exteriorY)
    {
        var template = defs.BuildingTemplate(templateId);
        var interior = template.BuildMap(defs);

        maps.AddMap(mapId, interior);
        
        FillRect(
            street,
            exteriorX,
            exteriorY,
            interior.Width,
            interior.Height,
            defs.TileOf("wall_brick"));

        var exteriorDoor = new Vector2i(
            exteriorX + interior.Width / 2,
            exteriorY + interior.Height - 1);

        street.SetTile(exteriorDoor.X, exteriorDoor.Y, defs.TileOf("door"));

        var interiorEntry = template.Anchor("entry");

        maps.Connect(
            StreetMapId,
            exteriorDoor,
            mapId,
            interiorEntry);

        buildings.Add(
            instanceId,
            new BuildingInstance(
                instanceId,
                templateId,
                mapId,
                exteriorDoor,
                template.Anchors));
    }

    private static void Fill(TileMap map, Tile tile)
    {
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            map.SetTile(x, y, tile);
    }

    private static void FillRect(
        TileMap map,
        int x,
        int y,
        int width,
        int height,
        Tile tile)
    {
        for (var row = y; row < y + height; row++)
        for (var column = x; column < x + width; column++)
            map.SetTile(column, row, tile);
    }
}