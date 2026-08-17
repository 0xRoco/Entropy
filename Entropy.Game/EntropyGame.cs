using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.Rendering;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game;

public class EntropyGame : IGameClient
{
    private Camera _camera = null!;
    private UiCamera _uiCamera = null!;
    private TileCamera _tileCamera = null!;
    private Shader _shader = null!;
    private QuadBatcher _batcher = null!;
    private QuadBatcher _uiBatcher = null!;
    private QuadBatcher _tileBatcher = null!;
    private GlyphAtlas _atlas = null!;
    private TileMap _map = null!;
    private IGameInput _input = null!;
    private World _world = null!;
    private Entity _player;
    private VisibilityMap _visibility = null!;
    private MessageLog _log = null!;
    private Rng _rng = null!;

    private GameContext _context = null!;
    private TurnProcessor _turnProcessor = null!;

    private Ui _ui = null!;
    private DrawContext _drawContext = null!;
    private Panel _testPanel = null!;
    
    private const int ViewRadius = 6;
    
    public void Load(Vector2i clientSize, IGameInput input)
    {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _input = input;
        _shader = Shader.FromFiles("Content/Shaders/quad.vert", "Content/Shaders/textured.frag");
        _camera = new Camera { ViewportSize = clientSize };
        _uiCamera = new UiCamera { ViewportSize = clientSize };
        _tileCamera = new TileCamera { ViewportSize = clientSize };
        _atlas = new GlyphAtlas("Content/ascii.png");
        _batcher = new QuadBatcher(_shader, _camera, _atlas);
        _uiBatcher = new QuadBatcher(_shader, _uiCamera, _atlas);
        _tileBatcher = new QuadBatcher(_shader, _tileCamera, _atlas);

        _ui = new Ui();
        _drawContext = new DrawContext {Batcher = _tileBatcher, Atlas = _atlas};

        _testPanel = new Panel
        {
            X = 5,
            Y = 3,
            Width = 30,
            Height = 10,
        };
        
        _testPanel.Add(new Label
        {
            X = 1, Y = 1,
            Width = 28,
            Text = "Inventory",
            Color = Color4.Yellow
        });
        
        _ui.AddRoot(_testPanel);
        
        _world = new World();
        _turnProcessor = new TurnProcessor();
        _rng = new Rng(1337);
        _log = new MessageLog();
        
        (_map, var roomCenters) = MapGenerator.Generate(60, 40, _rng, 25);
        Console.WriteLine($"Map generated with dimensions: {_map.Width}x{_map.Height}, Rooms: {roomCenters.Count}");
        Console.WriteLine($"Seed: {_rng.Seed}");
        
        var spawn = roomCenters[0];
        _player = EntitySpawner.CreatePlayer(_world, spawn.X, spawn.Y);
        _turnProcessor.AddActor(_player);
        
        var human = EntitySpawner.CreateHuman(_world, spawn.X + 1, spawn.Y);
        _turnProcessor.AddActor(human);

        _context = new GameContext
        {
            Map = _map,
            Log = _log,
            World = _world,
            Player = _player,
            Rng = _rng
        };


        foreach (var roomCenter in roomCenters.Skip(1))
        {
            var zombie = EntitySpawner.CreateZombie(_world, roomCenter.X + 1, roomCenter.Y); 
            _turnProcessor.AddActor(zombie);
        }
        
        _camera.Position = _world.Get<Position>(_player).Value;
        _visibility = new VisibilityMap(_map.Width, _map.Height);
        var startPos = _world.Get<Position>(_player).Value;
        Fov.Compute(new Vector2i((int)startPos.X, (int)startPos.Y), ViewRadius, _map, _visibility);
        
        _log.Add("Welcome to Entropy!", Color4.Red);
    }

    public void Update(FrameEventArgs args)
    { 
        _camera.Position += Controls.GetCameraPan(_input, (float)args.Time); 
        _camera.Zoom = Controls.GetZoomDelta(_input, _camera.Zoom);

        var inspectedTile = Controls.GetInspectedTile(_input, _camera);
        if (inspectedTile != null)
        {
            var tx = (int)inspectedTile.Value.X;
            var ty = (int)inspectedTile.Value.Y;
            if (tx >= 0 && tx < _map.Width && ty >= 0 && ty < _map.Height)
                _log.Add($"You squint your eyes and see a tile at ({tx}, {ty}) with glyph '{_map[tx, ty].Glyph}'", Color4.LightGray);
        }
        
        
        if (!_world.IsAlive(_player) || _world.Get<Health>(_player).Current <= 0) return;
        var move = Controls.GetMoveDirection(_input);
        
        if (move == null ||
            !_turnProcessor.ProcessPlayerTurn(_player, (Vector2i)move, _context, _visibility, ViewRadius)) return;
        
        _camera.Position = _world.Get<Position>(_player).Value;
        _turnProcessor.RunAITurns(_player, _context);
    }

    public void Render(FrameEventArgs args)
    {
        TileRenderer.Draw(_map, _visibility, _camera, _batcher);
        EntityRenderer.Draw(_world, _visibility, _batcher);
        LogPanel.Draw(_log, _uiBatcher, _uiCamera);
        
        _ui.Draw(_drawContext);
        _tileBatcher.Flush();
    }

    public void Resize(int width, int height)
    {
        _camera.ViewportSize = new Vector2i(width, height);
        _uiCamera.ViewportSize = new Vector2i(width, height);
        _tileCamera.ViewportSize = new Vector2i(width, height);
    }

    public void Dispose()
    {
        _batcher.Dispose();
        _uiBatcher.Dispose();
        _shader.Dispose();
        _atlas.Dispose();
    }
    
}