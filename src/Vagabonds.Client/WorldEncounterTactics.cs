using Microsoft.Xna.Framework;
using Vagabonds.Core.Content;

namespace Vagabonds.Client;

internal sealed partial class WorldGame
{
    private void DrawEncounterTactics(Rectangle box)
    {
        if (DisplayBattle is not { } battle) { Wrap("Encounter tactics appear while facing a group.", box.X + 20, box.Y + 64, box.Width - 40); return; }
        int x = box.X + 20, y = box.Y + 64, width = box.Width - 40;
        var enemies = battle.Units.Where(u => !u.Friendly).ToArray();
        string[] tactics = enemies.Select(u => _session.EncounterDescription(battle.ActorIds[u.Id])).Where(s => s.Length > 0).Distinct().ToArray();
        string overview = tactics.Length == 0 ? "Read the fixed intentions and turn order. Guard halves incoming damage after armour; stun removes a turn and cancels preparation." : string.Join(" ", tactics);
        y += Wrap(overview, x, y, width, Gold) + 16;
        foreach (var enemy in enemies)
        {
            int height = Math.Max(56, Math.Min(78, (box.Bottom - 80 - y) / Math.Max(1, enemies.Length - Array.IndexOf(enemies, enemy))));
            Fill(new(x, y, width, height - 6), new(30, 41, 46));
            _sprites.Fit(_batch, enemy.Sprite, new(x + 8, y + 6, 48, 48), enemy.Alive ? Ink : Muted);
            Text(Short(enemy.Name, 40) + " / HP " + enemy.Health + "/" + enemy.MaxHealth, x + 70, y + 7, enemy.Alive ? Ink : Muted);
            var intent = _session.EnemyActionPreview(enemy.Id);
            string action = intent == null ? enemy.Alive ? "Waiting for the resolved action." : "Defeated." : intent.Reason.Length > 0 ? intent.Name + ": " + intent.Reason
                : intent.Name + " / " + string.Join(", ", intent.Targets.Select(t => t.Name + (t.Maximum > 0 ? " " + t.Minimum + "-" + t.Maximum : "")));
            Wrap(action, x + 70, y + 29, width - 82, Muted);
            y += height;
        }
        Wrap("Prepared attacks spend a turn winding up. Stun before release cancels them; guarding reduces their damage. Inspect fighter intentions for target and effect details.", x, box.Bottom - 60, width, Muted);
    }
}
