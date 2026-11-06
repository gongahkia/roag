using Microsoft.Xna.Framework;
using Roag.Core.Profiles;
using Roag.Core.Tactics;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private int _profilePage;
    private void BuildPermanentProfileControls(Rectangle box)
    {
        void Add(string id, string label, Rectangle bounds, bool enabled) => ControlCache.Add("dungeon:" + id, label, bounds,
            enabled, false, "", "", null, "", -1, null, false);
        _profilePage = Math.Clamp(_profilePage, 0, (AchievementCatalog.All.Length - 1) / 3);
        Add("profile-prev", "Previous", new(box.X + 24, box.Bottom - 54, 130, 34), _profilePage > 0);
        Add("profile-next", "Next", new(box.Right - 154, box.Bottom - 54, 130, 34), (_profilePage + 1) * 3 < AchievementCatalog.All.Length);
    }
    private void DrawPermanentProfile(Rectangle box, int x, int y)
    {
        Text($"{_session.PermanentAchievements.Count}/22 recorded / {AchievementCatalog.Rewards(_session.PermanentAchievements).Length}/4 boon unlocks / page {_profilePage + 1}/8", x, y, Gold);
        y += 30;
        Wrap(_session.HasPermanentProfile ? "Achievements survive fresh worlds. Unlocks enter future campaigns' boon pools."
            : "This preview or earlier save does not record permanent progress. Start a new run to earn achievements.", x, y, box.Width - 48, Muted);
        y += 54;
        foreach (var achievement in AchievementCatalog.All.Skip(_profilePage * 3).Take(3))
        {
            bool pending = _session.State.Tactics?.Profile?.Pending.Contains(achievement.Id) == true;
            bool earned = _session.PermanentAchievements.Contains(achievement.Id);
            if (achievement.Reward is { } reward) DrawDungeonBoonIcon(reward, BoonRarity.Common, new(x, y + 4, 40, 40));
            else _sprites.Fit(_batch, "banner", new(x + 4, y + 8, 32, 32), earned ? Gold : Muted);
            Text(achievement.Name + (pending ? " / awaiting save" : earned ? " / earned" : " / locked"), x + 54, y, earned ? Gold : Ink);
            Wrap(achievement.Hint, x + 54, y + 24, box.Width - 108, Muted);
            if (achievement.Reward is { } boon) Text("Unlock: " + BoonCatalog.Default.Get(boon).Name, x + 54, y + 58, Gold);
            y += 100;
        }
    }
}
