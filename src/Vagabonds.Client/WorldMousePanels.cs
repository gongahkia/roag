using Microsoft.Xna.Framework;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;

namespace Vagabonds.Client;

internal sealed partial class WorldGame
{
    private void DrawPanel(int w, int h)
    {
        var box = PanelBox(w, h); Fill(box, new(24, 31, 37));
        int x = box.X + 20, y = box.Y + 64, width = box.Width - 40;
        string title = _panel switch { "inventory" => "PARTY & EQUIPMENT", "game" => "GAME MENU", "person" => "CONVERSATION", "actions" => "EXPEDITION ACTIONS", _ => _panel.ToUpperInvariant() };
        Text(title, x, box.Y + 18, Gold, 2);
        switch (_panel)
        {
            case "guide":
                string[] paragraphs = [
                    "Click observed ground to travel; the teal preview becomes a gold route. Click people, stairs, caches or storage to approach and interact. Right-click stops travel or closes a panel.",
                    "Play/Pause controls world time. Panels pause it while you choose. Use World map for the large-scale overview; the +/- and View buttons control the closer exploration camera. The mouse wheel also zooms.",
                    "Party manages companions and equipment. Conversations offer recruitment, stories, trade and treatment. Everyone needs food and rest; wounds and exhaustion affect combat and work.",
                    "Build chooses a structure, then click adjacent ground. Dig and Repair also target adjacent terrain. Work assigns workers or cancels reservations. Craft prepares supplies beside a camp. Actions offers rest, gathering, waiting and side-arc tasks.",
                    "In battle, click an ability, then its enemy or companion target. Self/group abilities execute when clicked. Choices cost no time; completed rounds advance one minute. Enemy intentions are visible, and Tactics describes bosses.",
                    "All dungeons and the selected final threat are available from the start. Defeating that boss completes the run. A party wipe causes permanent death; the ending offers a new traveller or a fresh world. Save/Load and the window close button are available without keyboard input."
                ];
                foreach (string paragraph in paragraphs) { y += Wrap(paragraph, x, y, width); y += 10; }
                break;
            case "game":
                Wrap("Mouse controls exploration, conversations, work, equipment and combat. Panels pause world time. Save preserves your current expedition; Load restores the selected slot.", x + 390, y, width - 390, Muted);
                break;
            case "person":
                var actor = _session.State.Actors.FirstOrDefault(a => a.Id == _person);
                if (actor == null) break;
                _sprites.Fit(_batch, actor.Sprite, new(x, y, 64, 64), Ink);
                Text(actor.Name + " / " + _session.Class(actor).Name, x + 80, y, Gold);
                Wrap(actor.Trait + " " + actor.Background + "; hopes to " + actor.Motivation + ".", x + 80, y + 24, width - 80);
                Text($"Hunger {actor.Body.Hunger:0}  Fatigue {actor.Body.Fatigue:0}  Wounds {actor.Body.Wounds}  Trust {_session.Party.First().Trust.GetValueOrDefault(actor.Id)}", x, y + 80, Muted);
                Wrap(actor.Memories.LastOrDefault()?.Text ?? "Their usual job is " + actor.Job + ". Recruitment leaves that post vacant.", x, y + 110, width, Muted);
                break;
            case "trade":
                var town = _session.Plan.Towns.FirstOrDefault(t => _session.State.Position.Distance(t.Center) <= 28);
                if (town == null) { Wrap("Approach a town to trade.", x, y, width); break; }
                Text(town.Name + " / Gold " + _session.Count("gold"), x, y, Gold);
                int row = 0;
                foreach (string item in new[] { "food", "wood", "stone", "medicine" })
                {
                    var stocks = _session.State.Towns[town.Id]; int ry = y + 52 + row++ * 56;
                    Text(item + "  stock " + stocks.Stocks[item] + "  carried " + _session.Count(item), x, ry);
                    Text($"Buy {stocks.Price(item)} gold / sell {Math.Max(1, stocks.Price(item) / 2)} gold", x, ry + 19, Muted);
                }
                break;
            case "inventory":
                var member = SelectedMember();
                Text($"Food {_session.Count("food")}  Medicine {_session.Count("medicine")}  Gold {_session.Count("gold")}  Wood {_session.Count("wood")}  Stone {_session.Count("stone")}", x, y + 44, Gold);
                Text($"{_session.Class(member).Name} L{member.Level} / {member.Life} / HP {member.Health}/{member.MaxHealth} / Power {member.Power} / Armour {member.Armor} / Speed {member.Speed}", x, y + 68);
                Text($"Hunger {member.Body.Hunger:0}/100  Fatigue {member.Body.Fatigue:0}/100  Wounds {member.Body.Wounds}  Stress {member.Stress}", x, y + 92, Muted);
                if (member.Memories.LastOrDefault() is { } memory && h > 760) Wrap(memory.Text, x, box.Bottom - 130, width, Muted);
                break;
            case "build":
                Wrap("Choose a structure, then click adjacent observed ground. Supplies are reserved immediately; the structure appears when work is finished. Use Work to assign a resident or cancel and refund it.", x, y - 4, width, Muted);
                break;
            case "work":
                Text("PENDING ORDERS", x, y, Gold); Text("AVAILABLE PEOPLE", x + width / 2 + 8, y, Gold);
                if (_session.State.Work.Count == 0) Wrap("No pending work. Choose Build, Dig, Craft or Gather food.", x, y + 40, width / 2 - 24, Muted);
                if (SelectedWork() is { } work) Wrap(work.Status + ". Click a worker and Assign, or cancel to return reserved supplies.", x, y + 336, width, Muted);
                break;
            case "craft":
                Text("Requires a serviceable camp. Click a recipe to reserve supplies and begin work.", x, y, Muted);
                var recipes = CraftRecipes();
                for (int i = 0; i < Math.Min(4, recipes.Length - _craftPage * 4); i++)
                    Text(string.Join(", ", recipes[_craftPage * 4 + i].Cost.Select(c => c.Value + " " + _session.Content.Get<ItemDefinition>(c.Key).Name)), x + 8, y + 86 + i * 74, Muted);
                break;
            case "actions":
                Wrap("Waiting and resting advance shared time. Nearby danger interrupts them. Repair targets adjacent owned structures; storage keeps actual stacks.", x, y + 236, width, Muted);
                break;
            case "storage":
                Text("Choose which supplies to move between the shared pack and this chest.", x, y, Muted);
                var contents = StoredItems(); var stacks = StorageItems();
                for (int i = 0; i < Math.Min(6, stacks.Length - _storagePage * 6); i++)
                {
                    string stack = stacks[_storagePage * 6 + i]; var item = _session.Content.All<ItemDefinition>().FirstOrDefault(d => d.Stack == stack);
                    Text($"{item?.Name ?? stack} / carried {_session.Count(stack)} / stored {contents.GetValueOrDefault(stack)}", x, y + 42 + i * 44);
                }
                break;
            case "journal":
                y += Wrap(_session.FinalObjective, x, y, width, Gold); y += 8;
                var encounterBoss = _session.State.Battle?.ActorIds.Select(id => _session.State.Actors.Single(a => a.Id == id)).FirstOrDefault(a => _session.Content.Entries[a.Archetype] is BossDefinition);
                var boss = _session.Content.Get<BossDefinition>((encounterBoss ?? _session.FinalBoss).Archetype);
                y += Wrap(boss.Tactics + " " + boss.Weakness, x, y, width, Muted); y += 8;
                y += Wrap("Side arc: " + _session.State.Arc.Status + ". " + _session.State.Arc.Objective, x, y, width); y += 8;
                foreach (string line in JournalLines(width).Skip(_journalOffset))
                {
                    if (y + 19 > box.Bottom - 100) break;
                    Text(line, x, y); y += 19;
                }
                break;
            case "map":
                Text("Known places / 4096 x 4096 surface. Exploration keeps a closer camera.", x, y, Muted);
                var region = new Rectangle(x, y + 28, width, box.Bottom - y - 64); Fill(region, new(12, 22, 27));
                foreach (var delta in _session.State.Chunks.Where(c => c.Key.Floor == 0 && c.Discovered.Count > 0))
                    Fill(new(region.X + delta.Key.X * region.Width / 64, region.Y + delta.Key.Y * region.Height / 64, Math.Max(2, region.Width / 64), Math.Max(2, region.Height / 64)), new(70, 86, 74));
                foreach (var known in _session.Plan.Towns.Where(t => _session.Chunks.Seen(t.Center)))
                    Text(known.Name, Math.Min(region.Right - 160, region.X + known.Center.X * region.Width / WorldPlan.Size), region.Y + known.Center.Y * region.Height / WorldPlan.Size, Gold);
                var position = _session.State.Position;
                Fill(new(region.X + position.X * region.Width / WorldPlan.Size, region.Y + position.Y * region.Height / WorldPlan.Size, 5, 5), Ink);
                break;
            case "inspector": DrawInspector(x, y, width); break;
        }
    }
}
