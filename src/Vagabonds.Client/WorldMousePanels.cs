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
        string title = _panel switch { "inventory" => "PARTY & EQUIPMENT", "game" => "GAME MENU", "person" => "CONVERSATION", "actions" => "EXPEDITION ACTIONS", "loot" => "CHOOSE YOUR EQUIPMENT", "spoils" => "ENCOUNTER WON", "combat-log" => "COMBAT LOG", _ => _panel.ToUpperInvariant() };
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
            case "discovery":
                if (_session.State.Discoveries.FirstOrDefault(d => d.Id == _discoveryId) is { } discovery)
                {
                    _sprites.Fit(_batch, _session.Content.Get<DiscoveryDefinition>(discovery.Definition).Sprite!, new(x, y, 54, 54), Gold);
                    Text(Short(discovery.Name, (width - 76) / 8), x + 72, y, Gold);
                    Text(_session.Plan.Towns[discovery.TownId].Name + " / " + _session.State.Factions[_session.Plan.Towns[discovery.TownId].FactionId].Name, x + 72, y + 24, Muted);
                    Wrap(_session.DiscoveryText(discovery), x, y + 78, width);
                    string guards = string.Join(", ", discovery.Enemies.Select(id => _session.State.Actors.Single(a => a.Id == id)).Where(a => a.Life == LifeState.Alive).Select(a => a.Name));
                    Wrap(guards.Length > 0 ? "Guarded by " + guards + ". Their strength is fixed; prepare your party before approaching."
                        : "The linked site is recorded on World map. Its terrain remains unexplored until you reach it.", x, box.Bottom - 214, width, guards.Length > 0 ? new(225, 146, 125) : Muted);
                }
                break;
            case "loot":
                if (_session.State.Loot.FirstOrDefault(c => c.Position == _lootPosition && !c.Taken) is { } chest)
                {
                    Wrap("Supplies: " + string.Join(", ", chest.Contents.Select(p => p.Value + " " + p.Key)) + ". Choose one item; the other offerings remain behind.", x, y, width, Gold);
                    Text("Compare against a companion's currently equipped slot:", x, y + 96, Muted);
                    for (int i = 0; i < chest.Choices.Length; i++)
                    {
                        var item = _session.Content.Get<ItemDefinition>(chest.Choices[i]); var change = _session.CompareGear(SelectedMember(), item.Id); int ry = y + 126 + i * 72;
                        Text($"{item.Slot}: HP+{item.Health} P+{item.Power} A+{item.Armor} S+{item.Speed}", x + 320, ry);
                        Text(Short("Replaces " + change.Previous + ": " + GearDelta(change), (width - 320) / 8), x + 320, ry + 23, Muted);
                    }
                }
                break;
            case "spoils":
                if (_session.State.LastEncounterReward is { } reward)
                {
                    Text($"Gold +{reward.Gold}   Food +{reward.Food}", x, y, Gold, 1.5f);
                    Wrap($"Each conscious survivor gained {reward.Experience} experience. Equipment in nearby caches can support different party roles.", x, y + 46, width);
                    int ly = y + 104; foreach (var level in reward.LevelUps) { Text(level, x, ly, Gold); ly += 26; }
                    if (_session.Party.Any(a => a.Life == LifeState.Downed)) Wrap("A companion is downed. Party lets you spend medicine to revive them.", x, box.Bottom - 164, width, new(224, 151, 132));
                }
                break;
            case "combat-log":
                Text("Damage and healing show actual HP changes; statuses show duration or removal.", x, y, Muted);
                int ey = y + 34;
                foreach (var entry in _session.State.CombatEvents.AsEnumerable().Reverse().Skip(_combatLogOffset).Take(6))
                {
                    Text($"Round {entry.Round} / {entry.Label}" + (entry.Amount > 0 ? " " + entry.Amount : ""), x, ey, EventColor(entry.Kind));
                    Wrap(entry.Detail, x, ey + 20, width, Muted); ey += 52;
                }
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
                Text("Known places / 4096 x 4096 surface. Choose a destination to mark it.", x, y, Muted);
                var region = new Rectangle(x, y + 36, width - 320, box.Bottom - y - 82); Fill(region, new(12, 22, 27));
                foreach (var delta in _session.State.Chunks.Where(c => c.Key.Floor == 0 && c.Discovered.Count > 0))
                    Fill(new(region.X + delta.Key.X * region.Width / 64, region.Y + delta.Key.Y * region.Height / 64, Math.Max(2, region.Width / 64), Math.Max(2, region.Height / 64)), new(70, 86, 74));
                foreach (var known in _session.Plan.Towns.Where(t => _session.Chunks.Seen(t.Center)))
                    Text(known.Name, Math.Min(region.Right - 160, region.X + known.Center.X * region.Width / WorldPlan.Size), region.Y + known.Center.Y * region.Height / WorldPlan.Size, Gold);
                foreach (var site in _session.Plan.Sites.Where(s => _session.State.KnownSites.Contains(s.Id)))
                {
                    int sx = region.X + site.Center.X * region.Width / WorldPlan.Size, sy = region.Y + site.Center.Y * region.Height / WorldPlan.Size;
                    Fill(new(sx - 2, sy - 2, 5, 5), new(220, 145, 120));
                    Text(Short(site.Name, 23), Math.Clamp(sx + 7, region.Left, region.Right - 192), Math.Clamp(sy, region.Top, region.Bottom - 18), new(220, 145, 120));
                }
                foreach (var mark in _session.State.Discoveries.Where(d => d.Known))
                    Fill(new(region.X + mark.Position.X * region.Width / WorldPlan.Size, region.Y + mark.Position.Y * region.Height / WorldPlan.Size, 3, 3), _session.DiscoveryComplete(mark) ? Muted : new(130, 198, 204));
                var position = _session.State.Position;
                Fill(new(region.X + position.X * region.Width / WorldPlan.Size, region.Y + position.Y * region.Height / WorldPlan.Size, 5, 5), Ink);
                if (_waypoint is { } waypoint) Fill(new(region.X + waypoint.X * region.Width / WorldPlan.Size - 2, region.Y + waypoint.Y * region.Height / WorldPlan.Size - 2, 7, 7), Gold);
                break;
            case "inspector": DrawInspector(x, y, width); break;
        }
    }
    private static string GearDelta(GearComparison c) => $"HP {c.Health:+0;-0;0} P {c.Power:+0;-0;0} A {c.Armor:+0;-0;0} S {c.Speed:+0;-0;0}";
}
