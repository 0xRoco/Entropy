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
    private const int UiCellPixelWidth = 8;
    private const int UiCellPixelHeight = 16;
    private Vector2i _clientSize;

    private MainMenuScreen _mainMenu = null!;
    private CharacterCreationScreen _characterCreation = null!;
    private PauseMenu _pauseMenu = null!;
    private GameMode _mode = GameMode.Loading;

    private Camera _camera = null!;
    private TileCamera _tileCamera = null!;
    private Shader _shader = null!;
    private QuadBatcher _batcher = null!;
    private QuadBatcher _uiBatcher = null!;
    private FontAtlas _fontAtlas = null!;
    private GlyphAtlas _atlas = null!;
    private TilesetDefinition _tileset = null!;
    private GlyphAtlas _terrainAtlas = null!;
    private string _contentRoot = null!;
    private QuadBatcher _terrainBatcher = null!;
    private LoadingScreen _loadingScreen = null!;
    private List<(string Name, Action Load)> _loadSteps = [];
    private int _loadStep;

    private IGameInput _input = null!;
    private TileMap _map = null!;
    private World _world = null!;
    private MapGraph _maps = null!;
    private IReadOnlyDictionary<string, BuildingInstance> _buildings = null!;
    private DefinitionRegistry _definitions = null!;
    private CharacterCatalog _characterCatalog = null!;
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
        _tileCamera = new TileCamera { ViewportSize = clientSize, CellPixelWidth = 8f, CellPixelHeight = 16f };
        _atlas = new GlyphAtlas(Path.Combine(_contentRoot, "tilesets", "ascii.png"));
        _fontAtlas = new FontAtlas(Path.Combine(_contentRoot, "Fonts", "IBM_VGA_8x16.ttf"));
        _batcher = new QuadBatcher(_shader, _camera, _atlas);
        _uiBatcher = new QuadBatcher(_shader, _tileCamera, _fontAtlas);
        _log = new MessageLog();
        _drawContext = new DrawContext { Batcher = _uiBatcher, Atlas = _fontAtlas };
        _clock = new WorldClock(2001, 3, 12, 7, 30);

        var jsonFolder = Path.Combine(_contentRoot, "Json");
        _definitions = new DefinitionRegistry();

        _loadSteps =
        [
            ("Items", () => _definitions.LoadItems(jsonFolder)),
            ("Creatures", () => _definitions.LoadCreatures(jsonFolder)),
            ("Terrain", () => _definitions.LoadTerrains(jsonFolder)),
            ("Tilesets", () => _definitions.LoadTilesets(jsonFolder)),
            ("Building templates", () => _definitions.LoadBuildingTemplates(jsonFolder)),
            ("World objects", () => _definitions.LoadWorldObjects(jsonFolder)),
            ("Tileset atlas", FinishContentLoad)
        ];

        _loadingScreen = new LoadingScreen([.. _loadSteps.Select(step => step.Name)]);
    }

    private void ProcessLoadStep()
    {
        if (_loadStep >= _loadSteps.Count)
            return;

        var (name, load) = _loadSteps[_loadStep];
        load();
        _loadingScreen.Complete(name);
        _loadStep++;
    }

    private void FinishContentLoad()
    {
        _tileset = _definitions.Tileset("entropy_art");

        _terrainAtlas = _tileset.Mode == "art"
            ? new GlyphAtlas(Path.Combine(
                Path.GetDirectoryName(_contentRoot)!,
                _tileset.Atlas.Replace('/', Path.DirectorySeparatorChar)))
            : _atlas;

        _terrainBatcher = new QuadBatcher(_shader, _camera, _terrainAtlas);

        _mainMenu = new MainMenuScreen(ToUiSize(_clientSize));
        _characterCatalog = CharacterCatalog.Load(Path.Combine(_contentRoot, "Characters", "character_options.json"));
        _characterCreation = new CharacterCreationScreen(_characterCatalog, ToUiSize(_clientSize));
        _characterCreation.Confirmed += StartNewGame;
        _characterCreation.Cancelled += ReturnToMainMenu;

        _mainMenu.CharacterCreationRequested += OpenCharacterCreation;
        _mainMenu.LoadGameRequested += LoadGame;
        _mainMenu.ExitRequested += () => ExitRequested = true;
        _pauseMenu = new PauseMenu(ToUiSize(_clientSize));
        _pauseMenu.ResumeRequested += ResumeGame;
        _pauseMenu.SaveRequested += SaveCurrentGame;
        _pauseMenu.MainMenuRequested += ReturnToMainMenu;
        _pauseMenu.QuitRequested += () => ExitRequested = true;
        _mode = GameMode.MainMenu;
    }

    public void Update(FrameEventArgs args)
    {
        if (_mode == GameMode.Loading)
        {
            ProcessLoadStep();
            return;
        }

        if (_mode == GameMode.MainMenu)
        {
            var menuKey = _input.GetKeyPressed();

            if (menuKey != null)
                _mainMenu.HandleKey(menuKey.Value);

            return;
        }

        if (_mode == GameMode.CharacterCreation)
        {
            var characterKey = _input.GetKeyPressed();
            if (characterKey != null)
                _characterCreation.HandleKey(characterKey.Value);
            return;
        }
        
        if (_world.Has<Sleeping>(_player))
        {
            if (!_world.IsAlive(_player) || _world.Get<Health>(_player).Current <= 0)
            {
                _world.Remove<Sleeping>(_player);
                return;
            }

            AdvanceTurn();
            SleepSystem.Recover(_context, _player);

            var reason = SleepSystem.WakeReason(_context, _player);
            if (reason is not null)
                SleepSystem.Wake(_context, _player, reason);
            else if (_input.GetKeyPressed() != null)
                SleepSystem.Wake(_context, _player, "You wake up.");

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

        if (_mode == GameMode.Paused)
        {
            var pauseKey = _input.GetKeyPressed();
            if (pauseKey != null)
                _pauseMenu.HandleKey(pauseKey.Value);
            return;
        }
        if (key == Keys.F6)
        {
            SaveCurrentGame();
            return;
        }
        if (key == Keys.Escape && !_hud.HasOpenModal)
        {
            _mode = GameMode.Paused;
            return;
        }
        if (key != null && _hud.HandleKey(key.Value)) return;

        if (!_world.IsAlive(_player) || _world.Get<Health>(_player).Current <= 0) return;
        Controls.GetMoveDirection(_input);

        var action = ProcessPlayerAction();
        if (!action.Succeeded || !action.ConsumesTurn) return;

        AdvanceTurn(action.TimeCostMinutes);
    }

    public void Render(FrameEventArgs args)
    {
        if (_mode == GameMode.Loading)
        {
            GL.Viewport(0, 0, _clientSize.X, _clientSize.Y);

            _loadingScreen.Draw(_drawContext, ToUiSize(_clientSize));
            _uiBatcher.Flush();

            return;
        }

        if (_mode == GameMode.MainMenu)
        {
            GL.Viewport(0, 0, _clientSize.X, _clientSize.Y);

            _mainMenu.Draw(_drawContext);
            _uiBatcher.Flush();

            return;
        }

        if (_mode == GameMode.CharacterCreation)
        {
            GL.Viewport(0, 0, _clientSize.X, _clientSize.Y);
            _characterCreation.Draw(_drawContext);
            _uiBatcher.Flush();
            return;
        }

        if (_mode == GameMode.Paused)
        {
            GL.Viewport(0, 0, _clientSize.X, _clientSize.Y);
            _pauseMenu.Draw(_drawContext);
            _uiBatcher.Flush();
            return;
        }

        ApplyMapViewport();
        GL.Enable(EnableCap.ScissorTest);
        GL.Scissor(
            _hud.Layout.Map.X * UiCellPixelWidth,
            _clientSize.Y - (_hud.Layout.Map.Y + _hud.Layout.Map.Height) * UiCellPixelHeight,
            _hud.Layout.Map.Width * UiCellPixelWidth,
            _hud.Layout.Map.Height * UiCellPixelHeight);

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

        GL.Disable(EnableCap.ScissorTest);
        GL.Viewport(0, 0, _clientSize.X, _clientSize.Y);

        _hud.Draw(_drawContext);
        _uiBatcher.Flush();
    }

    public void Resize(int width, int height)
    {
        _clientSize = new Vector2i(width, height);

        _camera.ViewportSize = _clientSize;
        _tileCamera.ViewportSize = _clientSize;

        _mainMenu?.Resize(ToUiSize(_clientSize));
        _characterCreation?.Resize(ToUiSize(_clientSize));
        _pauseMenu?.Resize(ToUiSize(_clientSize));

        if (_mode != GameMode.Gameplay) return;

        _hud.Resize(ToUiSize(_clientSize));
        ConfigureMapCamera();
    }
    
    private void OpenCharacterCreation()
    {
        _mode = GameMode.CharacterCreation;
    }

    private void StartNewGame(CharacterBuild? character = null)
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
            ViewRadius = ViewRadius,
            LockedMaps = new(StringComparer.OrdinalIgnoreCase)
            {
                "neighborhood_pharmacy_interior"
            }
        };

        _hud = new GameHud(_context, _clock, _rng.Seed, ToUiSize(_clientSize));
        _hud.NewCharacterRequested += RestartGame;
        _hud.MainMenuRequested += ReturnToMainMenu;
        _hud.TurnRequested += () => AdvanceTurn();

        ConfigureMapCamera();
        _camera.Position = _world.Get<Position>(_player).Value;

        _mode = GameMode.Gameplay;

        ApplyCharacter(_context, character);
    }

    private void ApplyCharacter(GameContext context, CharacterBuild? character)
    {
        if (character is null)
            return;

        var scenario = _characterCatalog.Scenarios.Single(option => option.Id == character.ScenarioId);
        if (_maps.Maps.ContainsKey(scenario.StartMapId))
        {
            context.MapId = scenario.StartMapId;
            context.Map = _maps[scenario.StartMapId];
            context.Visibility = _visibilities[scenario.StartMapId];
            _world.Set(_player, new Location { MapId = scenario.StartMapId });
            _world.Get<Position>(_player).Value = new Vector2(scenario.StartX, scenario.StartY);
            Fov.Compute(new Vector2i(scenario.StartX, scenario.StartY), ViewRadius, context.Map, context.Visibility);
        }

        var profession = _characterCatalog.Professions.Single(option => option.Id == character.ProfessionId);
        var background = _characterCatalog.Backgrounds.Single(option => option.Id == character.BackgroundId);
        _world.Set(_player, new CharacterIdentity
        {
            Name = character.Name,
            ProfessionId = profession.Id,
            BackgroundId = background.Id,
            Stats = new(character.Stats, StringComparer.OrdinalIgnoreCase),
            TraitIds = [.. character.TraitIds],
            SkillIds = [.. profession.Skills.Concat(background.Skills).Concat(character.SkillIds).Distinct()]
        });
        _world.Set(_player, new Named { Name = character.Name });
        foreach (var itemId in profession.StartingItems.Concat(background.StartingItems))
        {
            var item = EntitySpawner.CreateItem(
                _world,
                context.MapId,
                _definitions.Item(itemId),
                (int)_world.Get<Position>(_player).Value.X,
                (int)_world.Get<Position>(_player).Value.Y);
            ItemSystem.Transfer(_world, item, _player);
        }

        context.Log.Add($"You are {character.Name}, a {profession.Name} from {background.Name}.", Color4.Cyan);
        _camera.Position = _world.Get<Position>(_player).Value;
    }

    private void LoadGame()
    {
        var save = GameSave.Read();
        if (save is null)
        {
            return;
        }

        _rng = new Rng(save.Seed);
        StartNewGame();

        if (!_maps.Maps.ContainsKey(save.MapId))
        {
            _log.Add($"Save refers to unknown map '{save.MapId}'. Starting at the street.", Color4.Yellow);
            return;
        }

        _clock.Advance(save.ElapsedMinutes);

        ref var position = ref _world.Get<Position>(_player);
        position.Value = new Vector2(save.PlayerX, save.PlayerY);
        _world.Set(_player, new Location { MapId = save.MapId });
        _world.Set(_player, new Facing { Direction = new Vector2i(save.FacingX, save.FacingY) });
        _world.Set(_player, new Health { Current = save.Health.Current, Max = save.Health.Max });
        _world.Set(_player, new Hunger
        {
            Current = save.Hunger.Current,
            Max = save.Hunger.Max,
            Starving = save.Hunger.Starving
        });
        _world.Set(_player, new Thirst
        {
            Current = save.Thirst.Current,
            Max = save.Thirst.Max,
            Parched = save.Thirst.Parched
        });
        _world.Set(_player, new Fatigue { Current = save.Fatigue.Current, Max = save.Fatigue.Max });
        if (save.Character is { } character)
        {
            _world.Set(_player, new CharacterIdentity
            {
                Name = character.Name,
                ProfessionId = character.ProfessionId,
                BackgroundId = character.BackgroundId,
                Stats = new(character.Stats, StringComparer.OrdinalIgnoreCase),
                TraitIds = [.. character.TraitIds],
                SkillIds = [.. character.SkillIds]
            });
            _world.Set(_player, new Named { Name = character.Name });
        }

        foreach (var item in save.Inventory)
        {
            var entity = EntitySpawner.CreateItem(
                _world,
                save.MapId,
                _definitions.Item(item.DefinitionId),
                save.PlayerX,
                save.PlayerY,
                item.Count);
            ItemSystem.Transfer(_world, entity, _player);

            if (save.EquippedItemId == item.DefinitionId && _world.Has<Damage>(entity))
                _world.Set(_player, new Equipped { Item = entity });
        }

        _context.MapId = save.MapId;
        _context.Map = _maps[save.MapId];
        _context.Visibility = _visibilities[save.MapId];
        Fov.Compute(
            new Vector2i(save.PlayerX, save.PlayerY),
            ViewRadius,
            _context.Map,
            _context.Visibility);
        _camera.Position = position.Value;
        _log.Add("Game loaded.", Color4.LightGray);
    }

    private void SaveCurrentGame()
    {
        if (_mode != GameMode.Gameplay || !_world.IsAlive(_player))
            return;

        var save = GameSave.Capture(
            _world,
            _player,
            _rng.Seed,
            _clock.TotalMinutes,
            _context.MapId,
            _world.Has<CharacterIdentity>(_player)
                ? ToCharacterData(_world.Get<CharacterIdentity>(_player))
                : null);
        GameSave.Write(save);
        _log.Add("Game saved.", Color4.LightGray);
    }

    private static CharacterData ToCharacterData(CharacterIdentity identity) =>
        new(
            identity.Name,
            identity.ProfessionId,
            identity.BackgroundId,
            new(identity.Stats, StringComparer.OrdinalIgnoreCase),
            [.. identity.TraitIds],
            [.. identity.SkillIds]);
    
    private void RestartGame()
    {
        _rng = new Rng(Random.Shared.Next(int.MinValue, int.MaxValue));
        StartNewGame();
    }

    private void ReturnToMainMenu()
    {
        _mode = GameMode.MainMenu;
    }

    private void ResumeGame()
    {
        _mode = GameMode.Gameplay;
    }
    
    private void ConfigureMapCamera()
    {
        var rect = _hud.Layout.Map;

        _camera.ViewportOrigin = new Vector2i(
            rect.X * UiCellPixelWidth,
            rect.Y * UiCellPixelHeight);

        _camera.ViewportSize = new Vector2i(
            rect.Width * UiCellPixelWidth,
            rect.Height * UiCellPixelHeight);
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
    
    private void AdvanceTurn(int timeCostMinutes = 1)
    {
        _context.Clock.Advance(timeCostMinutes);
        NeedsSystem.Update(_context);
        _turnProcessor.RunAITurns(_player, _context);
        _camera.Position = _world.Get<Position>(_player).Value;
    }

    private ActionResult ProcessPlayerAction()
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
            Keys.G => TryPickupAtPlayer() ? ActionResult.Turn : ActionResult.Failed,
            Keys.E => OpenInteractMenu() ? ActionResult.Turn : ActionResult.Failed,
            Keys.Period => ActionResult.Turn,
            _ => ActionResult.Failed
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

    private static Vector2i ToUiSize(Vector2i pixels) =>
        new(
            pixels.X / 8,
            pixels.Y / 16);
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _batcher.Dispose();
        _uiBatcher.Dispose();
        _fontAtlas.Dispose();
        _terrainBatcher.Dispose();
        _shader.Dispose();
        _atlas.Dispose();
    }
}
