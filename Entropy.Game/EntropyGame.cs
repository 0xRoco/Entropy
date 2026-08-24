using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.Rendering;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Definitions;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game;

public class EntropyGame : IGameClient
{
    private const int ViewRadius = 6;
    private int _turnCount;
    private Vector2i _clientSize;

    private Camera _camera = null!;
    private TileCamera _tileCamera = null!;
    private Shader _shader = null!;
    private QuadBatcher _batcher = null!;
    private QuadBatcher _tileBatcher = null!;
    private GlyphAtlas _atlas = null!;

    private IGameInput _input = null!;
    private TileMap _map = null!;
    private World _world = null!;
    private DefinitionRegistry _definitions = null!;
    private Entity _player;
    private VisibilityMap _visibility = null!;
    private Rng _rng = new(1337);
    private MessageLog _log = null!;
    private GameContext _context = null!;
    private TurnProcessor _turnProcessor = null!;

    private DrawContext _drawContext = null!;
    private GameHud _hud = null!;

    
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
        _drawContext = new DrawContext {Batcher = _tileBatcher, Atlas = _atlas};
        
        _definitions = new DefinitionRegistry();
        _definitions.LoadItems("Content/Json");
        
        var result = WorldSetup.StartNewGame(_rng, _log, _definitions, ViewRadius);
        _map = result.Map;
        _world = result.World;
        _player = result.Player;
        _visibility = result.Visibility;
        _turnProcessor = result.Turns;
        
        _context = new GameContext
        {
            Map = _map, Log = _log, World = _world,Definitions = _definitions, Player = _player, Rng = _rng
        };
        
        _hud = new GameHud(_context, () => _turnCount, _rng.Seed, ToTileSize(clientSize));
        
        ConfigureMapCamera();
        
        _camera.Position = _world.Get<Position>(_player).Value;
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

        var key = _input.GetKeyPressed(); 
        if (key != null && _hud.HandleKey(key.Value)) return;
        
        if (!_world.IsAlive(_player) || _world.Get<Health>(_player).Current <= 0) return;
        Controls.GetMoveDirection(_input);
        
        if (!ProcessPlayerAction()) return;
        
        _camera.Position = _world.Get<Position>(_player).Value;
        _turnProcessor.RunAITurns(_player, _context);
        
        _turnCount++;
    }

    public void Render(FrameEventArgs args)
    {
        ApplyMapViewport();

        TileRenderer.Draw(_map, _visibility, _camera, _batcher);
        EntityRenderer.Draw(_world, _visibility, _batcher);

        GL.Viewport(0, 0, _clientSize.X, _clientSize.Y);

        _hud.Draw(_drawContext);
        _tileBatcher.Flush();
    }

    public void Resize(int width, int height)
    {
        _clientSize = new Vector2i(width, height);

        _tileCamera.ViewportSize = _clientSize;
        _hud.Resize(ToTileSize(_clientSize));

        ConfigureMapCamera();
    }

    public void Dispose()
    {
        _batcher.Dispose();
        _tileBatcher.Dispose();
        _shader.Dispose();
        _atlas.Dispose();
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
            return _turnProcessor.ProcessPlayerTurn(_player, (Vector2i)move, _context, _visibility, ViewRadius);

        var key = _input.GetKeyPressed();
        if (key == Keys.G)
            return TryPickupAtPlayer();

        return false;
    }

    private bool TryPickupAtPlayer()
    {
        var pos = _world.Get<Position>(_player).Value;
        var items = ItemSystem.ItemsAt(_world, pos);
        if (items.Count == 0) return false;

        foreach (var item in items)
        {
            var name = _world.Get<ItemIdentity>(item).Name;
            if (ItemSystem.TryPickup(_world, _player, item))
                _log.Add($"You pick up the {name}.");
        }
        return true;
    }
    
    private static Vector2i ToTileSize(Vector2i pixels) =>
        new(
            pixels.X / (int)Camera.TilePixelSize,
            pixels.Y / (int)Camera.TilePixelSize); 
}