using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roag.Core.Tactics;
using Roag.Core.World;
using Roag.Graphics;

namespace Roag.Client;

/// <summary>An unsaved spectator world. It never receives the player's session or save path.</summary>
internal sealed class TitleBackdrop : IDisposable
{
    private readonly Random _random;
    private readonly Dictionary<long, GridMotion> _motions = [];
    private readonly DungeonFeedback _feedback = new();
    private readonly MapCamera _camera = new();
    private double _age, _step, _time;
    private long _seen;
    private bool _cameraReady;
    public WorldSession World { get; private set; } = null!;
    public int Scenes { get; private set; }
    public int Actions { get; private set; }
    public int Hits { get; private set; }
    public Vector2 Camera => _camera.Center;
    public IReadOnlyList<DungeonEffect> Effects => _feedback.Effects;
    public void Resize(Rectangle bounds) { _camera.Viewport = bounds; _camera.Reset(32); }

    public TitleBackdrop(int seed)
    {
        _random = new(seed);
        Generate();
    }

    private void Generate()
    {
        ClassKind[] classes = [ClassKind.Adventurer, ClassKind.Ranger, ClassKind.Arcanist, ClassKind.Berserker,
            ClassKind.Alchemist, ClassKind.Hookrunner, ClassKind.Stormcaller];
        var next = WorldSession.CreateDungeonRun(_random.Next(int.MinValue, int.MaxValue), classes[_random.Next(classes.Length)]);
        try
        {
            // Start this disposable demonstration beside a naturally placed encounter.
            // Native enemy controllers and paid actions resolve everything thereafter.
            var enemies = next.State.Actors.Where(a => a.Tactics?.Enemy != null && a.Position.Floor == 1).ToArray();
            var enemy = enemies[_random.Next(enemies.Length)];
            var cells = (from dy in Enumerable.Range(-3, 7) from dx in Enumerable.Range(-3, 7)
                let p = enemy.Position.Offset(dx, dy)
                where Math.Abs(dx) + Math.Abs(dy) is >= 2 and <= 4 && next.State.Dungeon!.Contains(p)
                    && next.CanEnter(p) && next.Chunks.At(p) == Material.Floor
                select p).ToArray();
            if (cells.Length > 0) next.Party.Single().Position = next.State.Position = cells[_random.Next(cells.Length)];
            next.Reveal();
        }
        catch { next.Dispose(); throw; }
        World?.Dispose(); World = next;
        _motions.Clear(); _feedback.Reset(); _age = _step = 0; _seen = World.State.Tactics!.NextEvent - 1;
        _cameraReady = false; Scenes++;
    }

    public void Update(double elapsed, bool running, bool reducedMotion)
    {
        if (!running) return;
        double dt = Math.Clamp(elapsed, 0, .1); _time += dt; _age += dt; _step += dt;
        if (_age > 26 || World.State.AwaitingContinuation && _age > 3)
            Generate();
        _feedback.Advance(dt, true, World.State.Position.Floor);
        if (_step >= .48 && !World.State.AwaitingContinuation)
        {
            _step = 0;
            Act();
            foreach (var e in World.State.Tactics!.Events.Where(e => e.Sequence > _seen))
            {
                if (e.Kind == "DamageTaken" && e.Amount > 0) Hits++;
                var actor = World.State.Actors.FirstOrDefault(a => a.Id == e.Source);
                string? label = e.Kind switch { "DamageTaken" => "-" + e.Amount, "Healed" => "+" + e.Amount,
                    "DamageBlocked" => "BLOCK", "Died" => "FALLEN", _ => null };
                _feedback.Observe(e, label, actor?.Tactics?.Facing ?? Facing.South, actor?.Position,
                    World.State.Party.Contains(e.Target));
            }
            _seen = World.State.Tactics.NextEvent - 1;
        }
        foreach (var actor in World.State.Actors.Where(a => a.Life != LifeState.Dead && a.Position.Floor == World.State.Position.Floor))
        {
            if (!_motions.TryGetValue(actor.Id, out var motion)) _motions[actor.Id] = motion = new();
            if (reducedMotion) motion.Reset(actor.Position);
            motion.Observe(actor.Position, .24); motion.Advance(TimeSpan.FromSeconds(dt));
        }
        var hero = World.State.Position;
        var drift = reducedMotion ? Vector2.Zero : new Vector2(MathF.Sin((float)_time * .16f) * 3, MathF.Cos((float)_time * .12f) * 2);
        var target = new Vector2(hero.X + .5f, hero.Y + .5f) + drift;
        // Put the encounter beside the title card instead of behind its buttons.
        target.X -= _camera.Viewport.Width * .2f / _camera.CellSize;
        if (!_cameraReady) { _camera.Center = target; _cameraReady = true; }
        else if (!reducedMotion) _camera.Center = Vector2.Lerp(_camera.Center, target, (float)(1 - Math.Exp(-dt * 1.2)));
    }

    private void Act()
    {
        var hero = World.Party.Single();
        var enemies = World.State.Actors.Where(a => a.Tactics?.Enemy != null && a.Life == LifeState.Alive
            && a.Position.Floor == hero.Position.Floor).OrderBy(a => a.Position.Distance(hero.Position)).ToArray();
        if (enemies.Length == 0) { _age = 27; return; }
        // Occasional waiting lets native windups, movement, incoming hits and
        // defensive reactions play out rather than making an invincible bot.
        if (_random.Next(5) != 0)
        {
            TacticalAction[] attacks = _random.Next(3) == 0 ? [TacticalAction.Special, TacticalAction.BasicAttack]
                : [TacticalAction.BasicAttack];
            foreach (var action in attacks) foreach (var facing in Enum.GetValues<Facing>())
            {
                var intent = new TacticalIntent(action, facing);
                var preview = World.DungeonCombat.Preview(intent);
                if (preview.Valid && enemies.Take(4).Any(a => preview.Cells.Contains(a.Position)))
                { World.SubmitDungeon(intent); Actions++; return; }
            }
            var target = enemies[0].Position;
            var moves = new[] { Facing.North, Facing.East, Facing.South, Facing.West }.Select(f =>
            {
                var (x, y) = AttackGeometry.Offset(f); return (Facing: f, Position: hero.Position.Offset(x, y));
            }).Where(m => World.State.Dungeon!.Contains(m.Position) && World.Chunks.At(m.Position) is not (Material.StairsDown or Material.StairsUp)
                && World.DungeonCombat.Preview(new(TacticalAction.Move, m.Facing)).Valid)
                .OrderBy(m => m.Position.Distance(target)).ToArray();
            if (moves.Length > 0)
            {
                var move = moves[_random.Next(6) == 0 ? _random.Next(moves.Length) : 0];
                World.SubmitDungeon(new(TacticalAction.Move, move.Facing)); Actions++; return;
            }
        }
        World.SubmitDungeon(new(TacticalAction.Wait)); Actions++;
    }

    public void Draw(SpriteBatch batch, SpriteLibrary sprites, Texture2D pixel, UiFontAtlas fonts,
        Rectangle viewport, PresentationPreferences preferences)
    {
        _camera.Viewport = viewport; _camera.Reset(32);
        var first = _camera.ScreenToWorld(Vector2.Zero); var last = _camera.ScreenToWorld(new(viewport.Right, viewport.Bottom));
        Rectangle Cell(WorldPosition p)
        {
            var point = _camera.WorldToScreen(new(p.X, p.Y));
            return new((int)MathF.Floor(point.X), (int)MathF.Floor(point.Y), 33, 33);
        }
        void Fill(Rectangle r, Color c) => batch.Draw(pixel, r, c);
        var floor = World.State.Dungeon!.Floors.Single(f => f.Contains(World.State.Position));
        for (int y = (int)MathF.Floor(first.Y); y <= last.Y; y++) for (int x = (int)MathF.Floor(first.X); x <= last.X; x++)
        {
            var p = new WorldPosition(x, y, floor.Origin.Floor); var r = Cell(p);
            bool inside = floor.Contains(p); var material = inside ? World.Chunks.At(p) : Material.Wall;
            Fill(r, material == Material.Floor ? new(46, 40, 33) : new(21, 28, 29));
            sprites.Draw(batch, Materials.Sprite(material, Biome.Forest), r,
                material == Material.Floor ? new(81, 77, 62) : inside ? new(150, 153, 126) : new(64, 76, 66));
            if (inside && World.ObjectAt(p) is { } prop)
                sprites.Fit(batch, prop.Sprite, r, new(192, 161, 94));
            else if (inside && World.DungeonTerrainAt(p) is { } terrain)
                sprites.Fit(batch, terrain switch { 'b' => "shrub", 'g' => "reed", 'i' => "water", 'm' => "wisp", _ => "crystal" }, r, new(136, 166, 113));
        }
        if (preferences.CombatEffects)
            foreach (var attack in World.State.Tactics!.Attacks) foreach (var p in attack.Cells)
            {
                var r = Cell(p); Fill(new(r.X, r.Y, r.Width, 2), new Color(236, 116, 79) * .7f);
                Fill(new(r.X, r.Bottom - 2, r.Width, 2), new Color(236, 116, 79) * .7f);
            }
        foreach (var actor in World.State.Actors.Where(a => a.Life != LifeState.Dead && a.Position.Floor == floor.Origin.Floor))
        {
            var r = Cell(actor.Position);
            if (_motions.TryGetValue(actor.Id, out var motion))
            {
                var point = _camera.WorldToScreen(motion.Position); r = new((int)point.X - 16, (int)point.Y - 16, 32, 32);
            }
            if (!viewport.Intersects(r)) continue;
            var hit = _feedback.Effects.LastOrDefault(e => e.Kind == "DamageTaken" && e.Target == actor.Id && _feedback.Time - e.Born < .2);
            var tint = actor.Recruited ? new Color(241, 211, 138) : new Color(229, 139, 112);
            if (hit != null && preferences.Flash) tint = Color.White;
            bool flip = (motion?.FacingLeft ?? false) ^ (actor.Sprite == "roag-class-hookrunner");
            sprites.Draw(batch, actor.Sprite, r, tint, effects: flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            Fill(new(r.X + 3, r.Bottom - 2, 26, 2), new(17, 22, 26));
            Fill(new(r.X + 3, r.Bottom - 2, 26 * actor.Health / actor.MaxHealth, 2), tint);
        }
        foreach (var effect in _feedback.Effects)
        {
            double age = _feedback.Time - effect.Born; var r = Cell(effect.Position);
            float alpha = (float)Math.Clamp((1.1 - age) * 3, 0, 1);
            if (preferences.CombatEffects && effect.Kind == "DamageTaken" && age < .35)
                for (int i = 0; i < 7; i++)
                {
                    float a = i * MathHelper.TwoPi / 7, travel = preferences.ReducedMotion ? 10 : 5 + (float)age * 55;
                    Fill(new((int)(r.Center.X + MathF.Cos(a) * travel), (int)(r.Center.Y + MathF.Sin(a) * travel), 3, 3), new Color(255, 155, 94) * alpha);
                }
            if (!preferences.DamageNumbers || effect.Label.Length == 0) continue;
            int x = r.Center.X - effect.Label.Length * fonts.Advance / 2;
            int y = r.Y - 18 - (preferences.ReducedMotion ? 0 : (int)(age * 16));
            Fill(new(x - 3, y - 2, effect.Label.Length * fonts.Advance + 6, 18), new Color(14, 19, 24) * alpha);
            fonts.Draw(batch, effect.Label, new(x, y), new Color(255, 167, 122) * alpha);
        }
    }

    public void Dispose() => World.Dispose();
}
