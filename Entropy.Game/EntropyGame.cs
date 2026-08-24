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
        
        _hud = new GameHud(_log, _world, _player, () => _turnCount, _rng.Seed, ToTileSize(clientSize));
        _context = new GameContext
        {
            Map = _map, Log = _log, World = _world,Definitions = _definitions, Player = _player, Rng = _rng
        };

        _hud.OnItemDropped += DropSelectedItem;
        _hud.OnItemActivated += UseSelectedItem;
        _hud.OnItemWielded += WieldSelectedItem;
        
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
        TileRenderer.Draw(_map, _visibility, _camera, _batcher);
        EntityRenderer.Draw(_world, _visibility, _batcher);
        _hud.Draw(_drawContext);
        _tileBatcher.Flush();
    }

    public void Resize(int width, int height)
    {
        _camera.ViewportSize = new Vector2i(width, height);
        _tileCamera.ViewportSize = new Vector2i(width, height);
        _hud.Resize(ToTileSize(new Vector2i(width, height)));
    }

    public void Dispose()
    {
        _batcher.Dispose();
        _tileBatcher.Dispose();
        _shader.Dispose();
        _atlas.Dispose();
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

    private void DropSelectedItem(int index)
    {
        var items = ItemSystem.GetItems(_world, _player);
        if (index < 0 || index >= items.Count) return;
        
        var item = items[index];
        var name = _world.Get<ItemIdentity>(item).Name;
        var pos = _world.Get<Position>(_player).Value;
        
        if (_world.Has<Equipped>(_player) && _world.Get<Equipped>(_player).Item.Equals(item))
            _world.Remove<Equipped>(_player);
        
        ItemSystem.Drop(_world, item, (int)pos.X, (int)pos.Y);
        _log.Add($"You drop the {name}.");
    }

    private void UseSelectedItem(int index)
    {
        var items = ItemSystem.GetItems(_world, _player);
        if (index < 0 || index >= items.Count) return;
        ItemUse.Use(_world, _context, items[index], _player);
    }
    
    private void WieldSelectedItem(int index)
    {
        var items = ItemSystem.GetItems(_world, _player);
        if (index < 0 || index >= items.Count) return;

        var item = items[index];

        if (!_world.Has<Damage>(item))
        {
            _log.Add($"You can't wield the {_world.Get<ItemIdentity>(item).Name}.", Color4.LightGray);
            return;
        }

        _world.Set(_player, new Equipped { Item = item });
        _log.Add($"You wield the {_world.Get<ItemIdentity>(item).Name}.", Color4.Cyan);
    }
    
    private static Vector2i ToTileSize(Vector2i pixels) => new Vector2i(pixels. X / (int)Camera.TilePixelSize, pixels.Y / (int)Camera.TilePixelSize);
    
}