using Microsoft.Xna.Framework;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;

namespace Vagabonds.Client;

internal sealed partial class WorldGame
{
    private void DrawCombatRewards(Rectangle box)
    {
        if (_session.State.LastEncounterReward is not { } reward) return;
        int x = box.X + 20, y = box.Y + 64, width = box.Width - 40, gap = 12;
        int rewardWidth = (width - gap * 2) / 3;
        bool wildlife = reward.Gold == 0 && reward.Food == 0;
        DrawRewardCard(new(x, y, rewardWidth, 76), "coin", "+" + reward.Gold, "GOLD", "In the shared pack", Gold);
        DrawRewardCard(new(x + rewardWidth + gap, y, rewardWidth, 76), "ration", "+" + reward.Food, "FOOD", "In the shared pack", new(192, 212, 149));
        DrawRewardCard(new(x + 2 * (rewardWidth + gap), y, rewardWidth, 76), "spark", "+" + reward.Experience, "EXPERIENCE", "Each conscious survivor", new(169, 208, 235));

        var party = _session.Party.ToArray();
        int cardY = y + 114, cardHeight = Math.Clamp(box.Height - 342, 168, 204);
        Text("THE SURVIVORS", x, y + 90, Gold);
        int cardWidth = party.Length == 0 ? width : (width - gap * (party.Length - 1)) / party.Length;
        for (int i = 0; i < party.Length; i++)
        {
            var member = party[i]; var rect = new Rectangle(x + i * (cardWidth + gap), cardY, cardWidth, cardHeight);
            bool alive = member.Life == LifeState.Alive;
            bool levelled = reward.LevelUps.Contains(member.Name + " / level " + member.Level, StringComparer.Ordinal);
            Color accent = !alive ? new(228, 149, 133) : levelled ? Gold : new(145, 204, 165);
            Fill(rect, new(30, 41, 46)); Fill(new(rect.X, rect.Y, rect.Width, 3), accent);
            int columns = Math.Max(1, (rect.Width - 24) / 8);
            Text(Short(member.Name, columns), rect.X + 12, rect.Y + 12, alive ? Ink : accent);
            Text(Short(_session.Class(member).Name + " / L" + member.Level + (levelled ? " / LEVEL UP" : ""), columns), rect.X + 12, rect.Y + 33, levelled ? Gold : Muted);

            int portrait = Math.Max(48, Math.Min(Math.Min(96, rect.Height - 98), Math.Max(64, rect.Width / 3)) / 12 * 12);
            _sprites.Fit(_batch, member.Sprite, new(rect.X + 10, rect.Y + 57, portrait, portrait), alive ? Ink : accent * .7f);
            int statsX = rect.X + portrait + 24, statsWidth = Math.Max(16, rect.Right - statsX - 12);
            Text("HP " + member.Health + "/" + member.MaxHealth, statsX, rect.Y + 61, alive ? Ink : accent);
            RewardMeter(new(statsX, rect.Y + 82, statsWidth, 5), member.Health, member.MaxHealth, accent);
            Text("MP " + member.Mana + "/" + member.MaxMana, statsX, rect.Y + 97, new(166, 202, 229));
            RewardMeter(new(statsX, rect.Y + 118, statsWidth, 4), member.Mana, member.MaxMana, new(118, 169, 205));

            int xpY = rect.Bottom - 32;
            Text(alive ? "XP " + member.Experience + "/" + member.Level * 30 : "DOWNED / needs medicine", rect.X + 12, xpY, alive ? Muted : accent);
            RewardMeter(new(rect.X + 12, rect.Bottom - 10, rect.Width - 24, 4), alive ? member.Experience : 0, member.Level * 30, Gold);
        }

        int contextY = cardY + cardHeight + 12;
        int contextHeight = box.Bottom - 110 - contextY;
        if (wildlife)
        {
            var ingredients = _session.State.Loot.Where(c => c.SourceActor > 0 && !c.Taken && _session.Visible.Contains(c.Position))
                .SelectMany(c => c.Contents).GroupBy(p => p.Key).Take(4).ToArray();
            if (ingredients.Length > 0)
            {
                int ingredientWidth = width / ingredients.Length;
                for (int i = 0; i < ingredients.Length; i++)
                {
                    var item = _session.Content.All<ItemDefinition>().FirstOrDefault(d => d.Stack == ingredients[i].Key);
                    int ix = x + i * ingredientWidth;
                    _sprites.Fit(_batch, item?.Sprite ?? "chest", new(ix, contextY, 32, 32), Gold);
                    Text("x" + ingredients[i].Sum(p => p.Value) + "  " + Short(item?.Name ?? ingredients[i].Key, Math.Max(1, (ingredientWidth - 92) / 8)), ix + 42, contextY + 1);
                    Text("On nearby remains", ix + 42, contextY + 18, Muted);
                }
            }
            else Text("Close this panel and inspect the remains for ingredients.", x, contextY + 6, Muted);
        }
        else if (contextHeight >= 28)
        {
            _sprites.Fit(_batch, "chest", new(x, contextY, 32, 32), Gold);
            Text("Supplies awarded. Nearby caches may hold equipment for your party.", x + 44, contextY + 8, Muted);
        }

        int downed = party.Count(a => a.Life == LifeState.Downed);
        string guidance = downed > 0
            ? _session.Count("medicine") > 0
                ? "Open Party, select a downed companion, then Revive. Each revival uses 1 medicine."
                : "A companion is downed. Find medicine, then open Party to revive them."
            : wildlife ? "Close to collect ingredients from the remains, or review your party before travelling."
                : "Review your party's health and equipment, or close to continue the expedition.";
        Wrap(guidance, x, box.Bottom - 44, width, downed > 0 ? new(228, 149, 133) : Muted);
    }

    private void DrawRewardCard(Rectangle rect, string sprite, string amount, string label, string detail, Color accent)
    {
        Fill(rect, new(34, 43, 47)); Fill(new(rect.X, rect.Y, 3, rect.Height), accent);
        _sprites.Fit(_batch, sprite, new(rect.X + 14, rect.Y + 16, 42, 42), accent);
        Text(amount, rect.X + 70, rect.Y + 10, accent, 2);
        int amountWidth = amount.Length * 16;
        Text(label, rect.X + 80 + amountWidth, rect.Y + 17, accent);
        Text(Short(detail, Math.Max(1, (rect.Width - 82) / 8)), rect.X + 70, rect.Y + 48, Muted);
    }

    private void RewardMeter(Rectangle rect, int value, int maximum, Color color)
    {
        Fill(rect, new(12, 23, 29));
        Fill(new(rect.X, rect.Y, (int)(rect.Width * Math.Clamp((double)value / Math.Max(1, maximum), 0, 1)), rect.Height), color);
    }
}
