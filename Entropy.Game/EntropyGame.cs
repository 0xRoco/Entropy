using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.Rendering;
using Entropy.Engine.UI;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Content;
using Entropy.Content.Validation;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using Entropy.Game.WorldGen;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game;

public class EntropyGame : IGameClient
{
    public bool ExitRequested { get; private set; }
    
    private const int ViewRadius = 6;
    private Vector2i _clientSize;

    private MainMenuScreen _mainMenu = null!;
    private GameMode _mode = GameMode.MainMenu;
    
    private Camera _camera = null!;
    private TileCamera _tileCamera = null!;
    private Shader _shader = null!;
    private QuadBatcher _batcher = null!;
    private QuadBatcher _tileBatcher = null!;
    private GlyphAtlas _atlas = null!;
    private TilesetDefinition _tileset = null!;
    private GlyphAtlas _terrainAtlas = null!;
    private string _contentRoot = null!;
    private QuadBatcher _terrainBatcher = null!;

    private IGameInput _input = null!;
    private TileMap _map = null!;
    private World _world = null!;
    private MapGraph _maps = null!;
    private IReadOnlyDictionary<string, BuildingInstance> _buildings = null!;
    private DefinitionRegistry _definitions = null!;
    private Entity _player;
    private VisibilityMap _visibility = null!;
    private Dictionary<string, VisibilityMap> _visibilities = null!;
    private Rng _rng = new(Random.Shared.Next(int.MinValue, int.MaxValue));
    private MessageLog _log = null!;
    private GameContext _context = null!;
    private TurnProcessor _turnProcessor = null!;
    private WorldClock _clock = null!;

    private DrawContext _drawContext = null!;
    private GameHud _hud = null!;

    private bool _disposed;

    
    public void Load(Vector2i clientSize, IGameInput input)
    {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _clientSize = clientSize;
        _input = input;

        _contentRoot = ContentPaths.TryFindContentRoot(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException(
                "Could not locate the Content folder (no source tree and no deployed Content directory found).");
        var gameRoot = Path.GetDirectoryName(_contentRoot)!;

        _shader = Shader.FromFiles(
            Path.Combine(_contentRoot, "Shaders", "quad.vert"),
            Path.Combine(_contentRoot, "Shaders", "textured.frag"));
        _camera = new Camera { ViewportSize = clientSize };
        _tileCamera = new TileCamera { ViewportSize = clientSize };
        _atlas = new GlyphAtlas(Path.Combine(_contentRoot, "tilesets", "ascii.png"));
        _batcher = new QuadBatcher(_shader, _camera, _atlas);
        _tileBatcher = new QuadBatcher(_shader, _tileCamera, _atlas);
        _log = new MessageLog();
        _drawContext = new DrawContext { Batcher = _tileBatcher, Atlas = _atlas };
        _clock = new WorldClock(2001, 3, 12, 7, 30);

        var jsonFolder = Path.Combine(_contentRoot, "Json");
        _definitions = new DefinitionRegistry();
        _definitions.LoadItems(jsonFolder);
        _definitions.LoadCreatures(jsonFolder);
        _definitions.LoadTerrains(jsonFolder);
        _definitions.LoadTilesets(jsonFolder);
        _definitions.LoadBuildingTemplates(jsonFolder);
        _definitions.LoadWorldObjects(jsonFolder);

        _tileset = _definitions.Tileset("entropy_art");

        _terrainAtlas = _tileset.Mode == "art"
            ? new GlyphAtlas(Path.Combine(gameRoot, _tileset.Atlas.Replace('/', Path.DirectorySeparatorChar)))
            : _atlas;

        _terrainBatcher = new QuadBatcher(_shader, _camera, _terrainAtlas);
        
        var block = CityBlockGenerator.Generate(_definitions);

        _mainMenu = new MainMenuScreen(ToTileSize(clientSize));
        _mainMenu.NewGameRequested += StartNewGame;
        _mainMenu.ExitRequested += () => ExitRequested = true;
    }

    public void Update(FrameEventArgs args)
    { 
        if (_mode == GameMode.MainMenu)
        {
            var menuKey = _input.GetKeyPressed();

            if (menuKey != null)
                _mainMenu.HandleKey(menuKey.Value);

            return;
        }
        
        if (!_hud.HasOpenModal)
        {
            _camera.Position += Controls.GetCameraPan(_input, (float)args.Time);
            _camera.Zoom = Controls.GetZoomDelta(_input, _camera.Zoom);
        }

        var inspectedTile = Controls.GetInspectedTile(_input, _camera);
        if (inspectedTile != null)
        {
            var tx = (int)inspectedTile.Value.X;
            var ty = (int)inspectedTile.Value.Y;
            if (tx >= 0 && tx < _context.Map.Width &&
                ty >= 0 && ty < _context.Map.Height)
            {
                InteractionSystem.ExamineAt(_context, new Vector2i(tx, ty));
            }
        }

        var rightClicked = Controls.GetClickedTile(_input, _camera, MouseButton.Right);
        if (rightClicked != null && !_hud.HasOpenModal)
        {
            var tx = (int)rightClicked.Value.X;
            var ty = (int)rightClicked.Value.Y;
            if (tx >= 0 && tx < _context.Map.Width &&
                ty >= 0 && ty < _context.Map.Height &&
                _context.Visibility.IsVisible(tx, ty))
            {
                _hud.OpenWorldMenu(new Vector2i(tx, ty));
            }
        }

        var key = _input.GetKeyPressed();
        if (key == Keys.F5)
        {
            ReloadContent();
            return;
        }
        if (key != null && _hud.HandleKey(key.Value)) return;

        if (!_world.IsAlive(_player) || _world.Get<Health>(_player).Current <= 0) return;
        Controls.GetMoveDirection(_input);

        if (!ProcessPlayerAction()) return;

        AdvanceTurn();
    }

    public void Render(FrameEventArgs args)
    {
        if (_mode == GameMode.MainMenu)
        {
            GL.Viewport(0, 0, _clientSize.X, _clientSize.Y);

            _mainMenu.Draw(_drawContext);
            _tileBatcher.Flush();

            return;
        }
        
        ApplyMapViewport();

        TileRenderer.Draw(
            _context.Map,
            _context.Visibility,
            _camera,
            _terrainBatcher,
            tilesetMode: _tileset.Mode,
            artAtlas: _terrainAtlas,
            artCellSize: (int)Camera.TilePixelSize,
            spriteMap: _tileset.Sprites,
            terrainSpriteKeys: _definitions.TerrainSpriteKeys);

        EntityRenderer.Draw(
            _world,
            _context.MapId,
            _context.Visibility,
            _batcher,
            _terrainBatcher,
            SpriteKeyOf,
            _tileset.Sprites,
            _terrainAtlas,
            _tileset.CellSize);

        GL.Viewport(0, 0, _clientSize.X, _clientSize.Y);

        _hud.Draw(_drawContext);
        _tileBatcher.Flush();
    }

    public void Resize(int width, int height)
    {
        _clientSize = new Vector2i(width, height);
        _mainMenu.Resize(ToTileSize(_clientSize));

        if (_mode != GameMode.Gameplay) return;
        
        _tileCamera.ViewportSize = _clientSize;
        _hud.Resize(ToTileSize(_clientSize));
        ConfigureMapCamera();
    }
    
    private void StartNewGame()
    {
        _clock = new WorldClock(2001, 3, 12, 7, 30);
        _log = new MessageLog();

        var result = WorldSetup.StartNewGame(_rng, _log, _definitions, ViewRadius);

        _map = result.Map;
        _maps = result.Maps;
        _buildings = result.Buildings;
        _world = result.World;
        _player = result.Player;
        _visibilities = result.Visibilities;
        _visibility = _visibilities[result.MapId];
        _turnProcessor = result.Turns;

        _context = new GameContext
        {
            Map = result.Map,
            MapId = result.MapId,
            Maps = result.Maps,
            Log = _log,
            World = _world,
            Definitions = _definitions,
            Player = _player,
            Rng = _rng,
            Clock = _clock,
            Turns = _turnProcessor,
            Visibilities = result.Visibilities,
            Visibility = _visibility,
            ViewRadius = ViewRadius
        };

        _hud = new GameHud(_context, _clock, _rng.Seed, ToTileSize(_clientSize));
        _hud.NewCharacterRequested += RestartGame;
        _hud.MainMenuRequested += ReturnToMainMenu;
        _hud.TurnRequested += AdvanceTurn;

        ConfigureMapCamera();
        _camera.Position = _world.Get<Position>(_player).Value;

        _mode = GameMode.Gameplay;
    }
    
    private void RestartGame()
    {
        _rng = new Rng(Random.Shared.Next(int.MinValue, int.MaxValue));
        StartNewGame();
    }

    private void ReturnToMainMenu()
    {
        _mode = GameMode.MainMenu;
    }
    
    private void ConfigureMapCamera()
    {
        var rect = _hud.Layout.Map;
        var pixelsPerTile = (int)Camera.TilePixelSize;

        _camera.ViewportOrigin = new Vector2i(
            rect.X * pixelsPerTile,
            rect.Y * pixelsPerTile);

        _camera.ViewportSize = new Vector2i(
            rect.Width * pixelsPerTile,
            rect.Height * pixelsPerTile);
    }

    private void ApplyMapViewport()
    {
        var origin = _camera.ViewportOrigin;
        var size = _camera.ViewportSize;

        GL.Viewport(
            origin.X,
            _clientSize.Y - origin.Y - size.Y,
            size.X,
            size.Y);
    }
    
    private void AdvanceTurn()
    {
        _context.Clock.Advance(1);
        NeedsSystem.Update(_context);
        _turnProcessor.RunAITurns(_player, _context);
        _camera.Position = _world.Get<Position>(_player).Value;
    }

    private bool ProcessPlayerAction()
    {
        var move = Controls.GetMoveDirection(_input);
        if (move != null)
            return _turnProcessor.ProcessPlayerTurn(
                _player,
                (Vector2i)move,
                _context,
                _context.Visibility,
                ViewRadius);

        var key = _input.GetKeyPressed();
        
        return key switch
        {
            Keys.G => TryPickupAtPlayer(),
            Keys.E => OpenInteractMenu(),
            Keys.Period => true,
            _ => false
        };
    }

    private bool OpenInteractMenu()
    {
        if (_hud.HasOpenModal)
            return false;

        var pos = _world.Get<Position>(_player).Value;
        var facing = _world.Has<Facing>(_player)
            ? _world.Get<Facing>(_player).Direction
            : new Vector2i(1, 0);

        var target = new Vector2i((int)pos.X + facing.X, (int)pos.Y + facing.Y);
        if (target.X < 0 || target.X >= _context.Map.Width ||
            target.Y < 0 || target.Y >= _context.Map.Height)
        {
            target = new Vector2i((int)pos.X, (int)pos.Y);
        }

        _hud.OpenWorldMenu(target);
        return false;
    }

    private bool TryPickupAtPlayer()
    {
        var pos = _world.Get<Position>(_player).Value;
        var items = ItemSystem.ItemsAt(_world, _context.MapId, pos);
        if (items.Count == 0) return false;

        foreach (var item in items)
        {
            var name = _world.Get<ItemIdentity>(item).Name;
            if (ItemSystem.TryPickup(_world, _player, item))
                _log.Add($"You pick up the {name}.");
        }
        return true;
    }
    
    private void ReloadContent()
    {
        try
        {
            var jsonFolder = Path.Combine(_contentRoot, "Json");
            var fresh = new DefinitionRegistry();
            fresh.LoadItems(jsonFolder);
            fresh.LoadCreatures(jsonFolder);
            fresh.LoadTerrains(jsonFolder);
            fresh.LoadTilesets(jsonFolder);
            fresh.LoadBuildingTemplates(jsonFolder);
            fresh.LoadWorldObjects(jsonFolder);

            var errors = DefinitionValidator.Validate(
                fresh.Items, fresh.Creatures, fresh.Terrains,
                fresh.Tilesets, fresh.BuildingTemplates, fresh.WorldObjects);
            if (errors.Count > 0)
            {
                _log.Add($"Content reload blocked, {errors.Count} validation error(s):", Color4.Red);
                foreach (var error in errors.Take(3))
                    _log.Add("  " + error, Color4.Red);
                return;
            }

            _definitions = fresh;
            _tileset = _definitions.Tileset("entropy_art");

            _terrainAtlas.Dispose();
            _terrainAtlas = _tileset.Mode == "art"
                ? new GlyphAtlas(Path.Combine(
                    Path.GetDirectoryName(_contentRoot)!,
                    _tileset.Atlas.Replace('/', Path.DirectorySeparatorChar)))
                : _atlas;
            _terrainBatcher.Dispose();
            _terrainBatcher = new QuadBatcher(_shader, _camera, _terrainAtlas);

            foreach (var map in _maps.Maps.Values)
                RestampTerrainTiles(map);
                
            var restamped = 0;
            foreach (var building in _buildings.Values)
            {
                var template = _definitions.BuildingTemplate(building.TemplateId);
                var map = _maps[building.MapId];
                if (map.Width != template.Width || map.Height != template.Height)
                {
                    _log.Add(
                        $"  '{building.Id}' changed size. restart to apply.",
                        Color4.Yellow);
                    continue;
                }

                for (var y = 0; y < template.Height; y++)
                for (var x = 0; x < template.Width; x++)
                {
                    var marker = template.Grid[y][x];
                    map.SetTile(x, y, _definitions.TileOf(template.Legend[marker]));
                }
                restamped++;
            }

            _log.Add(
                $"Content reloaded: {_definitions.Terrains.Count} terrains, " +
                $"{_definitions.Items.Count} items, {_definitions.Creatures.Count} creatures, " +
                $"{restamped} building interior(s) restamped.",
                Color4.LightGray);
        }
        catch (Exception ex)
        {
            _log.Add($"Content reload failed: {ex.Message}", Color4.Red);
        }
    }

    private void RestampTerrainTiles(TileMap map)
    {
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
        {
            var tile = map[x, y];
            if (tile.TerrainDefIndex == 0) continue;
            map.SetTile(x, y, _definitions.TileForIndex(tile.TerrainDefIndex));
        }
    }

    private string? SpriteKeyOf(Entity entity)
    {
        if (_world.Has<WorldObjectIdentity>(entity))
            return "furniture:" + _world.Get<WorldObjectIdentity>(entity).DefinitionId;
        if (_world.Has<CreatureIdentity>(entity))
            return "creature:" + _world.Get<CreatureIdentity>(entity).DefinitionId;
        if (_world.Has<Item>(entity) && _world.Has<ItemIdentity>(entity))
            return "item:" + _world.Get<ItemIdentity>(entity).DefinitionId;
        return null;
    }

    private static Vector2i ToTileSize(Vector2i pixels) =>
        new(
            pixels.X / (int)Camera.TilePixelSize,
            pixels.Y / (int)Camera.TilePixelSize);
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _batcher.Dispose();
        _tileBatcher.Dispose();
        _terrainBatcher.Dispose();
        _shader.Dispose();
        _atlas.Dispose();
    }
}