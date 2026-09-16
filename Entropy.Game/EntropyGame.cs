using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.Rendering;
using Entropy.Engine.UI;
using Entropy.Engine.World;
using Entropy.Content;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Components.Vitals;
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
    private string _contentRoot = null!;
    private ContentRuntime _content = null!;
    private LoadingScreen _loadingScreen = null!;

    private IGameInput _input = null!;
    private CharacterCatalog _characterCatalog = null!;
    private Rng _rng = new(Random.Shared.Next(int.MinValue, int.MaxValue));
    private MessageLog _log = null!;
    private GameContext _context = null!;
    private GameSession _session = null!;
    private ISimulation _simulation = null!;
    private WorldClock _clock = null!;

    private DrawContext _drawContext = null!;
    private GameHud _hud = null!;
    private PlayerActions _actions = null!;

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

        _content = new ContentRuntime(_contentRoot, _shader, _camera, _atlas);
        _loadingScreen = new LoadingScreen(_content.LoadStepNames);
    }

    private void ProcessLoadStep()
    {
        var name = _content.ProcessLoadStep();
        if (name is null) return;

        _loadingScreen.Complete(name);
        if (name != "Tileset atlas") return;

        FinishContentLoad();
    }

    private void FinishContentLoad()
    {
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

        if (ActivitySystem.IsActive(_context.World, _context.Player))
        {
            _actions.ProcessActivity();
            EventProjector.Project(_context.Events, _log);
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
            _simulation.Execute(new ExamineCommand(new Vector2i(tx, ty)));
            EventProjector.Project(_context.Events, _log);
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

        if (!_context.World.IsAlive(_context.Player) || _context.World.Get<Health>(_context.Player).Current <= 0) return;
        Controls.GetMoveDirection(_input);

        var action = _actions.ProcessPlayerAction();
        _actions.ProcessActionResult(action);
        EventProjector.Project(_context.Events, _log);
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

        GameplayRenderer.Draw(
            _context,
            _hud,
            _camera,
            _batcher,
            _content.TerrainBatcher,
            _uiBatcher,
            _fontAtlas,
            _content.Tileset,
            _content.TerrainAtlas,
            _content.Definitions,
            _clientSize);
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

        var result = WorldSetup.StartNewGame(_rng, _log, _content.Definitions, ViewRadius, _clock);

        var session = GameSession.Create(result, _log, _content.Definitions, _rng, ViewRadius);
        _session = session;
        _context = session.Context;
        _simulation = session.Simulation;

        _hud = new GameHud(_context, _simulation, _clock, _rng.Seed, ToUiSize(_clientSize));
        _hud.NewCharacterRequested += RestartGame;
        _hud.MainMenuRequested += ReturnToMainMenu;
        _actions = new PlayerActions(_context, _simulation, _input, _camera, _hud);
        _hud.ActionCompleted += _actions.ProcessActionResult;

        ConfigureMapCamera();
        _camera.Position = _context.World.Get<Position>(_context.Player).Value;

        _mode = GameMode.Gameplay;

        ApplyCharacter(_context, character);
    }

    private void ApplyCharacter(GameContext context, CharacterBuild? character)
    {
        if (character is null)
            return;
        CharacterSetupSystem.Apply(context, _characterCatalog, character);
        _camera.Position = _context.World.Get<Position>(_context.Player).Value;
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

        if (!_context.Maps.Maps.ContainsKey(save.MapId))
        {
            _log.Add($"Save refers to unknown map '{save.MapId}'. Starting at the street.", Color4.Yellow);
            return;
        }

        _session.RestoreSave(save);
        _camera.Position = _context.World.Get<Position>(_context.Player).Value;
        _log.Add("Game loaded.", Color4.LightGray);
    }

    private void SaveCurrentGame()
    {
        if (_mode != GameMode.Gameplay || !_context.World.IsAlive(_context.Player))
            return;

        var save = _session.CaptureSave();
        GameSave.Write(save);
        _log.Add("Game saved.", Color4.LightGray);
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


    private void ReloadContent()
    {
        _content.Reload(_context.Maps, _session.Buildings, (message, color) => _log.Add(message, color));
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
        _content.Dispose();
        _shader.Dispose();
        _atlas.Dispose();
    }
}
