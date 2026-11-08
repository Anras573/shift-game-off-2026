using System.Numerics;
using GravityShift.Components;
using GravityShift.Gravity;
using GravityShift.Levels;
using GravityShift.Systems;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Physics;
using Yaeger.Physics.Components;
using Yaeger.Physics.Systems;
using Yaeger.Platform;
using Yaeger.Systems;

namespace GravityShift;

/// <summary>
/// One attempt at one level: its own <see cref="World"/> plus the systems that run it.
/// Restarting a level just builds a fresh session, which keeps reset logic trivial.
/// </summary>
public sealed class LevelSession
{
    private static readonly Color WallColor = new(70, 78, 104);
    private static readonly Color BackgroundColor = new(24, 26, 38);
    private static readonly Color GoalColor = new(255, 214, 92);
    private static readonly Color HazardColor = new(220, 60, 70);
    private static readonly Color ParcelColor = new(196, 140, 84);
    private static readonly Color BeltColor = new(54, 112, 120);
    private static readonly Color BeltStripeColor = new(140, 220, 225);
    private static readonly Color LockColor = new(150, 90, 230, 90); // translucent: a zone, not a wall

    private readonly PhysicsWorld2D _physics;
    private readonly CharacterControllerSystem _controller;
    private readonly GravityShiftSystem _gravityShift;
    private readonly PlayerMovementSystem _movement;
    private readonly CameraRotationSystem _camera;
    private readonly LevelRulesSystem _rules;
    private readonly BeltMarkerSystem _beltMarkers;
    private readonly UnifiedRenderSystem? _render;
    private readonly IViewport? _viewport;
    private float _accumulator;

    public LevelSession(
        LevelData level,
        IInputState input,
        IRenderSurface? renderSurface = null,
        IViewport? viewport = null
    )
    {
        Level = level;
        World = new World();
        Gravity = new GravityState();

        // Zero world gravity: PhysicsWorld2D is only here to turn the tilemap into merged
        // static colliders. The player's gravity lives on the CharacterControllerSystem.
        _physics = new PhysicsWorld2D(World, Vector2.Zero, broadphaseCellSize: 0.5f);
        _controller = new CharacterControllerSystem(
            World,
            Gravity.Direction.ToVector() * Tuning.GravityStrength
        );
        _gravityShift = new GravityShiftSystem(World, Gravity, _controller, input);
        _movement = new PlayerMovementSystem(World, Gravity, input, level);
        _ = new GravityLockSystem(World, Gravity, _controller);
        _beltMarkers = new BeltMarkerSystem(World);
        _viewport = viewport;
        _camera = new CameraRotationSystem(World, Gravity, viewport);
        _rules = new LevelRulesSystem(World, level, _controller);

        if (renderSurface is not null)
            _render = new UnifiedRenderSystem(renderSurface, null, World, viewport);

        Build();
    }

    public LevelData Level { get; }
    public World World { get; }
    public GravityState Gravity { get; }
    public LevelOutcome Outcome => _rules.Outcome;
    public GravityShiftSystem GravityShift => _gravityShift;

    /// <summary>Advances the level by one rendered frame of <paramref name="deltaTime"/> seconds.</summary>
    public void Update(float deltaTime)
    {
        _gravityShift.HandleInput();

        _accumulator += MathF.Max(deltaTime, 0f);
        var steps = 0;
        while (_accumulator >= Tuning.FixedTimeStep && steps < Tuning.MaxStepsPerFrame)
        {
            Step(Tuning.FixedTimeStep);
            _accumulator -= Tuning.FixedTimeStep;
            steps++;
        }

        // Drop any backlog we couldn't simulate rather than spiralling.
        if (steps == Tuning.MaxStepsPerFrame)
            _accumulator = 0f;

        _camera.Update(deltaTime);
        _beltMarkers.Update(deltaTime);
    }

    /// <summary>Runs exactly one fixed simulation step. Public for deterministic tests.</summary>
    public void Step(float step)
    {
        if (_rules.Outcome != LevelOutcome.Playing)
            return;

        _gravityShift.Update(step);
        _movement.Update(step);
        _physics.Update(step);
        _controller.Update(step);
        _rules.Update(step);
    }

    public void Render() => _render?.Render();

    private void Build()
    {
        var level = Level;

        var background = World.CreateEntity("background");
        World.AddComponent(background, new Transform2D(Vector2.Zero, scale: level.WorldSize));
        World.AddComponent(
            background,
            new Sprite(IRenderSurface.SolidTexturePath, BackgroundColor)
        );
        World.AddComponent(background, new RenderLayer(0));

        // One flat-colour tile (the engine's built-in 1x1 white texture), marked solid so
        // TilemapColliderSystem generates merged colliders from it.
        var tileset = new Tileset(IRenderSurface.SolidTexturePath, 1, 1, [0]);
        var tilemap = new Tilemap(
            tileset,
            level.Width,
            level.Height,
            new Vector2(Tuning.TileSize),
            WallColor
        );
        for (var row = 0; row < level.Height; row++)
        for (var column = 0; column < level.Width; column++)
            if (level.IsSolid(column, row))
                tilemap.SetTile(column, row, 0);

        var walls = World.CreateEntity("walls");
        World.AddComponent(walls, new Transform2D(level.Origin));
        World.AddComponent(walls, tilemap);
        World.AddComponent(walls, new RenderLayer(1));

        // Chutes are generous (touching counts), shredders forgiving (must overlap a bit).
        if (level.Belts.Count > 0)
            BuildBelts(tileset);

        foreach (var cell in level.Locks)
            CreateTrigger<GravityLock>(
                level.CellCenter(cell),
                LockColor,
                Tuning.TileSize,
                layer: 1
            );

        foreach (var cell in level.Goals)
            CreateTrigger<Goal>(level.CellCenter(cell), GoalColor, Tuning.TileSize * 1.2f);
        foreach (var cell in level.Hazards)
            CreateTrigger<Hazard>(level.CellCenter(cell), HazardColor, Tuning.TileSize * 0.7f);

        var player = World.CreateEntity("player");
        World.AddComponent(player, new Player());
        World.AddComponent(
            player,
            new Transform2D(level.CellCenter(level.Spawn), scale: new Vector2(Tuning.PlayerSize))
        );
        World.AddComponent(player, Velocity2D.Zero);
        World.AddComponent(player, new CharacterController2D(Tuning.PlayerSize, Tuning.PlayerSize));
        World.AddComponent(player, new Sprite(IRenderSurface.SolidTexturePath, ParcelColor));
        World.AddComponent(player, new RenderLayer(3));

        var camera = World.CreateEntity("camera");
        World.AddComponent(
            camera,
            new Camera2D(
                Vector2.Zero,
                CameraRotationSystem.FitZoom(_viewport),
                Gravity.Direction.CameraRotation()
            )
        );
    }

    private void BuildBelts(Tileset tileset)
    {
        var level = Level;
        var belts = new Tilemap(
            tileset,
            level.Width,
            level.Height,
            new Vector2(Tuning.TileSize),
            BeltColor
        );
        foreach (var (cell, direction) in level.Belts)
        {
            belts.SetTile(cell.Column, cell.Row, 0);

            // A thin stripe across the belt, slid along it by BeltMarkerSystem.
            var stripe = World.CreateEntity();
            var across =
                MathF.Abs(direction.X) > 0f ? new Vector2(0.18f, 0.8f) : new Vector2(0.8f, 0.18f);
            World.AddComponent(stripe, new BeltMarker(level.CellCenter(cell), direction));
            World.AddComponent(
                stripe,
                new Transform2D(level.CellCenter(cell), scale: across * Tuning.TileSize)
            );
            World.AddComponent(
                stripe,
                new Sprite(IRenderSurface.SolidTexturePath, BeltStripeColor)
            );
            World.AddComponent(stripe, new RenderLayer(2));
        }

        var entity = World.CreateEntity("belts");
        World.AddComponent(entity, new Transform2D(level.Origin));
        World.AddComponent(entity, belts);
        World.AddComponent(entity, new RenderLayer(1));
    }

    private void CreateTrigger<TMarker>(
        Vector2 position,
        Color color,
        float triggerSize,
        int layer = 2
    )
        where TMarker : struct
    {
        var entity = World.CreateEntity();
        World.AddComponent(entity, default(TMarker));
        World.AddComponent(entity, new BoxCollider2D(new Vector2(triggerSize), isTrigger: true));
        World.AddComponent(entity, new Transform2D(position, scale: new Vector2(Tuning.TileSize)));
        World.AddComponent(entity, new Sprite(IRenderSurface.SolidTexturePath, color));
        World.AddComponent(entity, new RenderLayer(layer));
    }
}
