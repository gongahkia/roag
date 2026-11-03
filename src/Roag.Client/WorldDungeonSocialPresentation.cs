using Microsoft.Xna.Framework;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private long _nativeSocialSpeaker;
    private Rectangle NativeSocialSpeechBox(Rectangle box) => new(box.X + 24, box.Y + 184, box.Width - 48, box.Height - 374);
    private Rectangle NativeSocialCard(Rectangle box, bool person) => new(box.X + 24 + (person ? (box.Width - 24) / 2 : 0),
        box.Y + 60, (box.Width - 72) / 2, 108);

    private void OpenNativeSocial(long person)
    {
        ClosePanel(); _person = person; _nativeSocialSpeaker = _session.DungeonMember.Id;
        _panel = "dungeon-social";
        ShowNativeSocialDialogue(WorldDialogue.Lines(_session, _person, listener: _nativeSocialSpeaker));
    }

    private void ShowNativeSocialDialogue(IReadOnlyList<DialogueLine> lines)
    {
        _voice?.Stop(); _dialogue = new();
        var box = NativeSocialSpeechBox(PanelBox(1024, 720));
        _dialogue.Start(lines, (box.Width - 32) / Math.Max(8, _uiFonts?.Advance ?? 8),
            Math.Max(1, (box.Height - 64) / Math.Max(24, _uiFonts?.LineHeight ?? 24)));
    }

    private void EnsureNativeSocialDialogue()
    {
        if (_nativeSocialSpeaker == 0) _nativeSocialSpeaker = _session.DungeonMember.Id;
        if (_dialogue == null) ShowNativeSocialDialogue(WorldDialogue.Lines(_session, _person, listener: _nativeSocialSpeaker));
    }

    private void BuildNativeSocialControls(Rectangle box)
    {
        EnsureNativeSocialDialogue();
        var speech = NativeSocialSpeechBox(box);
        AddDungeonPartyControl("dungeon:social-next", _dialogue!.Speaking ? "Show line" : _dialogue.Finished ? "Read" : "Continue",
            new(speech.Right - 152, speech.Bottom - 30, 140, 24), !_dialogue.Finished);
        var offers = _session.Interactions(_person).Where(i => i.Action != "recruit").ToArray();
        for (int i = 0; i < offers.Length; i++)
        {
            var offer = offers[i]; var member = _session.DungeonMember;
            var quote = _session.QuoteNativeInteraction(member.Id, _person, offer.Id);
            var preview = _session.DungeonCombat.Preview(new(TacticalAction.SocialInteraction, Target: _person, Item: offer.Id));
            bool browse = offer.Action == "trade";
            bool valid = browse ? quote.Valid : DungeonSelectionReady && preview.Valid;
            string reason = _dialogue.Finished ? !browse && !DungeonSelectionReady ? "Select the ready member first."
                : browse ? quote.Reason : preview.Reason : "Finish the conversation line first.";
            string costs = string.Join(" / ", offer.Cost.Select(p => p.Value + " " + _session.Content.Get<ItemDefinition>(p.Key).Name));
            string effect = offer.Action switch
            {
                "heal" => $"You: +{offer.Amount} HP, -1 wound / ",
                "share-meal" => $"{_session.Simulation.MealReduction} hunger: you + NPC / ",
                "rumour" => "Local landmark / ", _ => ""
            };
            string sprite = offer.Action switch { "heal" => "potion", "share-meal" => "apple", "trade" => "coin", _ => _session.ActorById(_person).Sprite };
            int width = (box.Width - 72) / 2;
            AddDungeonPartyControl("dungeon:social:" + offer.Id, offer.Name,
                new(box.X + 24 + i % 2 * (width + 24), box.Bottom - 174 + i / 2 * 60, width, 52),
                _dialogue.Finished && valid, reason: reason, sprite: sprite,
                detail: effect + costs + (browse ? "Free price list" : (costs.Length > 0 ? " / " : "") + "1 action"));
        }
    }

    private void DrawNativeSocialPanel(Rectangle box)
    {
        EnsureNativeSocialDialogue();
        void Speaker(Actor actor, bool person)
        {
            var card = NativeSocialCard(box, person);
            bool speaking = _dialogue!.Line?.Speaker == actor.Id;
            Fill(card, new Color(15, 23, 29));
            if (speaking) Fill(new(card.X, card.Bottom - 2, card.Width, 2), Gold);
            var portrait = new Rectangle(card.X + 10, card.Y + 10, 80, 80);
            if (speaking && _dialogue.Speaking && !_preferences.ReducedMotion)
                portrait.Y -= (int)(Math.Abs(Math.Sin(_presentationTime * 17 + actor.Id % 7)) * 3);
            _sprites.Draw(_batch, actor.Sprite, portrait, speaking ? Ink : Muted,
                effects: ActorSpriteEffects(actor.Sprite, facingLeft: person));
            Text(Short(actor.Name, 26), card.X + 104, card.Y + 14, speaking ? Gold : Ink);
            Text(actor.Tactics?.Build is { } build ? ClassCatalog.Get(build.Class).Name + (person ? " / " + actor.Job : "") : actor.Job,
                card.X + 104, card.Y + 42, Muted);
            if (person) Text("Trust " + actor.Trust.GetValueOrDefault(_nativeSocialSpeaker), card.X + 104, card.Y + 70, Muted);
        }
        Speaker(_session.ActorById(_nativeSocialSpeaker), false); Speaker(_session.ActorById(_person), true);
        var speech = NativeSocialSpeechBox(box); Fill(speech, new Color(15, 23, 29));
        Text(_dialogue!.Line?.Name ?? "", speech.X + 16, speech.Y + 10, Gold);
        Text(_dialogue.Text, speech.X + 16, speech.Y + 38);
        if (_dialogue.Finished) Text("Choose a reply below. Browsing consumes no time.", speech.X + 16, speech.Bottom - 24, Muted);
    }

    private void ChooseNativeSocial(string interaction)
    {
        if (_dialogue?.Finished != true) return;
        var definition = _session.Interactions(_person).FirstOrDefault(i => i.Id == interaction);
        if (definition == null) return;
        long speaker = _session.DungeonMember.Id;
        var result = _session.PerformInteraction(_person, interaction); _notice = result.Message;
        if (!result.Success) return;
        if (definition.Action == "trade") { ClosePanel(); _panel = "dungeon-trade"; return; }
        _nativeSocialSpeaker = speaker;
        CollectDungeonFeedback();
        ShowNativeSocialDialogue(WorldDialogue.Lines(_session, _person, interaction, result.Message, speaker));
    }
}
