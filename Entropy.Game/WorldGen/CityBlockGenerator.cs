using Entropy.Engine.World;
using Entropy.Content;
using Entropy.Game.Systems;
using OpenTK.Mathematics;

namespace Entropy.Game.WorldGen;

public static class CityBlockGenerator
{
    public const string StreetMapId = "block_street";

    public static CityBlock Generate(DefinitionRegistry defs)
    {
        var maps = new MapGraph();
        var street = new TileMap(120, 80);

        Fill(street, defs.TileOf("grass"));

        FillRect(street, 0, 20, street.Width, 4, defs.TileOf("road_asphalt"));
        FillRect(street, 0, 18, street.Width, 2, defs.TileOf("sidewalk"));
        FillRect(street, 0, 24, street.Width, 2, defs.TileOf("sidewalk"));
        FillRect(street, 0, 48, street.Width, 4, defs.TileOf("road_asphalt"));
        FillRect(street, 0, 46, street.Width, 2, defs.TileOf("sidewalk"));
        FillRect(street, 0, 52, street.Width, 2, defs.TileOf("sidewalk"));
        FillRect(street, 58, 0, 4, street.Height, defs.TileOf("road_asphalt"));
        FillRect(street, 56, 0, 2, street.Height, defs.TileOf("sidewalk"));
        FillRect(street, 62, 0, 2, street.Height, defs.TileOf("sidewalk"));

        maps.AddMap(StreetMapId, street);

        var buildings = new Dictionary<string, BuildingInstance>();
        
        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "corner_store",
            templateId: "corner_store",
            mapId: "store_interior",
            exteriorX: 4,
            exteriorY: 8);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "store_shack",
            templateId: "small_shack",
            mapId: "store_shack_interior",
            exteriorX: 10,
            exteriorY: 26,
            rotate180: true);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "apartment_dana",
            templateId: "apartment_small",
            mapId: "apartment_dana_interior",
            exteriorX: 22,
            exteriorY: 10);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "apartment_marcus",
            templateId: "apartment_small",
            mapId: "apartment_marcus_interior",
            exteriorX: 40,
            exteriorY: 10);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "neighborhood_house",
            templateId: "detached_house",
            mapId: "neighborhood_house_interior",
            exteriorX: 8,
            exteriorY: 54,
            rotate180: true);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "neighborhood_pharmacy",
            templateId: "pharmacy",
            mapId: "neighborhood_pharmacy_interior",
            exteriorX: 104,
            exteriorY: 9);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "neighborhood_diner",
            templateId: "diner",
            mapId: "neighborhood_diner_interior",
            exteriorX: 27,
            exteriorY: 54,
            rotate180: true);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "neighborhood_hardware",
            templateId: "hardware_store",
            mapId: "neighborhood_hardware_interior",
            exteriorX: 44,
            exteriorY: 54,
            rotate180: true);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "neighborhood_clinic",
            templateId: "clinic",
            mapId: "neighborhood_clinic_interior",
            exteriorX: 70,
            exteriorY: 9);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "neighborhood_warehouse",
            templateId: "warehouse",
            mapId: "neighborhood_warehouse_interior",
            exteriorX: 68,
            exteriorY: 54,
            rotate180: true);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "neighborhood_office",
            templateId: "office",
            mapId: "neighborhood_office_interior",
            exteriorX: 86,
            exteriorY: 10);

        AddBuilding(
            maps, buildings, defs, street,
            instanceId: "neighborhood_service_station",
            templateId: "service_station",
            mapId: "neighborhood_service_station_interior",
            exteriorX: 88,
            exteriorY: 54,
            rotate180: true);

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
        int exteriorY,
        bool rotate180 = false)
    {
        var template = defs.BuildingTemplate(templateId);
        var grid = rotate180
            ? template.Grid
                .AsEnumerable()
                .Reverse()
                .Select(row => new string(row.Reverse().ToArray()))
                .ToList()
            : template.Grid;
        var anchors = rotate180
            ? template.Anchors.ToDictionary(
                pair => pair.Key,
                pair => new Vector2i(
                    template.Width - 1 - pair.Value.X,
                    template.Height - 1 - pair.Value.Y))
            : template.Anchors;
        var interior = BuildInterior(grid, template.Legend, defs);

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
            rotate180 ? exteriorY : exteriorY + interior.Height - 1);

        street.SetTile(exteriorDoor.X, exteriorDoor.Y, defs.TileOf("door"));

        var interiorEntry = anchors["entry"];

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
                anchors));
    }

    private static TileMap BuildInterior(
        IReadOnlyList<string> grid,
        IReadOnlyDictionary<char, string> legend,
        DefinitionRegistry defs)
    {
        var map = new TileMap(grid[0].Length, grid.Count);

        for (var y = 0; y < grid.Count; y++)
        for (var x = 0; x < grid[y].Length; x++)
        {
            var marker = grid[y][x];
            map.SetTile(x, y, defs.TileOf(legend[marker]));
        }

        return map;
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
