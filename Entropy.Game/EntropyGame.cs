using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.Rendering;
using Entropy.Engine.UI;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Definitions;
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
    private QuadBatcher _terrainBatcher = null!;

    private IGameInput _input = null!;
    private TileMap _map = null!;
    private World _world = null!;
    private MapGraph _maps = null!;
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
        _shader = Shader.FromFiles("Content/Shaders/quad.vert", "Content/Shaders/textured.frag");
        _camera = new Camera { ViewportSize = clientSize };
        _tileCamera = new TileCamera { ViewportSize = clientSize };
        _atlas = new GlyphAtlas("Content/ascii.png");
        _batcher = new QuadBatcher(_shader, _camera, _atlas);
        _tileBatcher = new QuadBatcher(_shader, _tileCamera, _atlas);
        _log = new MessageLog();
        _drawContext = new DrawContext { Batcher = _tileBatcher, Atlas = _atlas };
        _clock = new WorldClock(2001, 3, 12, 7, 30);

        _definitions = new DefinitionRegistry();
        _definitions.LoadItems("Content/Json");
        _definitions.LoadCreatures("Content/Json");
        _definitions.LoadTerrains("Content/Json");
        _definitions.LoadTilesets("Content/Json");
        _definitions.LoadBuildingTemplates("Content/Json");
        
        _tileset = _definitions.Tileset("entropy_art");

        _terrainAtlas = _tileset.Mode == "art"
            ? new GlyphAtlas(_tileset.Atlas)
            : _atlas;

        _terrainBatcher = new QuadBatcher(_shader, _camera, _terrainAtlas);
        
        var block = CityBlockGenerator.Generate(_definitions);
        Console.WriteLine($"Block maps: {block.Maps.Maps.Count}");
        Console.WriteLine($"Buildings: {block.Buildings.Count}");

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
        
        _camera.Position += Controls.GetCameraPan(_input, (float)args.Time); 
        _camera.Zoom = Controls.GetZoomDelta(_input, _camera.Zoom);

        var inspectedTile = Controls.GetInspectedTile(_input, _camera);
        if (inspectedTile != null)
        {
            var tx = (int)inspectedTile.Value.X;
            var ty = (int)inspectedTile.Value.Y;
            if (tx >= 0 && tx < _context.Map.Width &&
                ty >= 0 && ty < _context.Map.Height)
            {
                _log.Add(
                    $"You squint your eyes and see a tile at ({tx}, {ty}) with glyph '{_context.Map[tx, ty].Glyph}'",
                    Color4.LightGray);
            }
        }

        var key = _input.GetKeyPressed(); 
        if (key != null && _hud.HandleKey(key.Value)) return;
        
        if (!_world.IsAlive(_player) || _world.Get<Health>(_player).Current <= 0) return;
        Controls.GetMoveDirection(_input);
        
        if (!ProcessPlayerAction()) return;
        
        _context.Clock.Advance(1);
        NeedsSystem.Update(_context);
        _turnProcessor.RunAITurns(_player, _context);
        _camera.Position = _world.Get<Position>(_player).Value;
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
            artCellSize: (int)Camera.TilePixelSize);

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
            Keys.Period => true,
            _ => false
        };
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
    
    private string? SpriteKeyOf(Entity entity)
    {
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