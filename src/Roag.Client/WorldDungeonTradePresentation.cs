using Microsoft.Xna.Framework;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private int _nativeTradeAmount = 1;
    private static readonly string[] TradeStacks = ["food", "wood", "stone", "medicine"];

    private void BuildDungeonTradeControls(Rectangle box)
    {
        int width = (box.Width - 72) / 2;
        int[] amounts = [1, 5, 10, 50];
        for (int i = 0; i < amounts.Length; i++)
            AddDungeonPartyControl("dungeon:trade-amount:" + amounts[i], "Quantity " + amounts[i],
                new(box.X + 24 + i * 150, box.Y + 154, 138, 32), selected: _nativeTradeAmount == amounts[i]);
        var member = _session.DungeonMember;
        var town = _session.Plan.Towns.FirstOrDefault(t => member.Position.Distance(t.Center) <= 28);
        for (int i = 0; i < TradeStacks.Length; i++) for (int column = 0; column < 2; column++)
        {
            string stack = TradeStacks[i]; var kind = column == 0 ? WorldCommandKind.Buy : WorldCommandKind.Sell;
            var command = new WorldCommand(kind, Item: stack, Amount: _nativeTradeAmount);
            var quote = _session.QuoteTrade(member.Id, command);
            var preview = _session.DungeonCombat.Preview(new(TacticalAction.Trade, Work: command));
            string sprite = _session.Content.All<ItemDefinition>().First(item => item.Stack == stack).Sprite!;
            string price = quote.Valid ? " / " + Math.Abs(quote.GoldChange) + " gold" : "";
            string stock = column == 0 && town != null ? " / stock " + _session.State.Towns[town.Id].Stocks[stack] : "";
            AddDungeonPartyControl($"dungeon:trade-do:{kind}:{stack}", kind + " " + stack + price,
                new(box.X + 24 + column * (width + 24), box.Y + 210 + i * 64, width, 56),
                DungeonSelectionReady && preview.Valid, reason: DungeonSelectionReady ? preview.Reason : "Select the ready member first.",
                sprite: sprite, detail: $"{_nativeTradeAmount} units / carried {_session.Count(stack)}" + stock);
        }
    }

    private void DrawDungeonTradePanel(Rectangle box, int x, int y)
    {
        var member = _session.DungeonMember;
        Text(member.Name + " / " + DungeonReadiness(member), x, y, Gold); y += 28;
        var town = _session.Plan.Towns.FirstOrDefault(t => member.Position.Distance(t.Center) <= 28);
        Wrap(town == null ? "Trade on the surface near a town."
            : town.Name + $" / shared gold {_session.Count("gold")}",
            x, y, box.Width - 48, Muted);
        if (_session.State.NativeCampaign != null)
            Text("Market / 06:00 to 20:00 / " + (_session.IsTownMarketOpen ? "open" : "closed"), x, y + 32, Muted);
        Wrap("Browsing and quantities are free. Each exchange spends this member's own action; supplies remain shared. Immediate danger prevents trade.",
            x, box.Bottom - 48, box.Width - 48, Muted);
    }

    private void ActivateDungeonTradeControl(string[] parts)
    {
        if (parts[1] == "trade") { _panel = "dungeon-trade"; return; }
        if (parts[1] == "trade-amount") { _nativeTradeAmount = int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture); return; }
        var result = _session.SubmitDungeon(new(TacticalAction.Trade, Work: new(Enum.Parse<WorldCommandKind>(parts[2]), Item: parts[3], Amount: _nativeTradeAmount)));
        _notice = result.Message;
        if (result.Success) { StopTravel(); _roagAim = "move"; CollectDungeonFeedback(); ClosePanel(); }
    }
}
