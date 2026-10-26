using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private DialoguePlayback? _dialogue;
    private ConversationVoice? _voice;
    private string _afterDialogue = "";
    private float _voiceVolume = .45f;
    private bool _reducedMotion;
    private int PortraitSize(Rectangle box) => Math.Clamp(box.Height - 390, 128, 240);
    private Rectangle SpeechBox(Rectangle box) => new(box.X + 20, box.Y + 108 + PortraitSize(box), box.Width - 40,
        box.Height - 260 - PortraitSize(box));

    private void EnsureConversation()
    {
        if (_dialogue != null || _person == 0) return;
        ShowDialogue(WorldDialogue.Lines(_session, _person));
    }
    private void ShowDialogue(IReadOnlyList<DialogueLine> lines)
    {
        _voice?.Stop(); _dialogue = new();
        // Paginate for the minimum window so resizing never hides text or resets a line.
        var speech = SpeechBox(PanelBox(1024, 720));
        _dialogue.Start(lines, (speech.Width - 32) / 8, (speech.Height - 80) / 16);
    }
    private void AdvanceDialogue()
    {
        _voice?.Stop(); _dialogue?.Advance();
        if (_dialogue?.Finished == true && !string.IsNullOrEmpty(_afterDialogue))
        {
            string next = _afterDialogue; _afterDialogue = "";
            ClosePanel(); if (next != "close") _panel = next;
        }
    }
    private void UpdateConversation(double elapsed)
    {
        if (_panel is not ("person" or "camp-story") || !InputFocused) { _voice?.Stop(); return; }
        EnsureConversation();
        _dialogue?.Update(elapsed, (actor, c) => (_voice ??= new()).Play(actor, c, _voiceVolume));
    }
    private Rectangle SpeakingPortrait(Actor actor, Rectangle box, bool right)
    {
        int size = PortraitSize(box);
        var rect = new Rectangle(right ? box.Right - 64 - size : box.Left + 64, box.Top + 58, size, size);
        if (!_reducedMotion && _dialogue is { Speaking: true } && _dialogue.Line?.Speaker == actor.Id)
        {
            double phase = _presentationTime * 17 + actor.Id % 7;
            rect.Y -= (int)(Math.Abs(Math.Sin(phase)) * 7);
            rect.X += (int)(Math.Sin(phase * .7) * 3); rect.Height += (int)(Math.Sin(phase) * 3);
        }
        return rect;
    }
    private void DrawConversation(Rectangle box)
    {
        EnsureConversation();
        var person = _session.State.Actors.FirstOrDefault(a => a.Id == _person);
        if (person == null) return;
        var player = _session.Party.FirstOrDefault(a => a.Life == LifeState.Alive && (_panel != "camp-story" || a.Id != person.Id)) ?? person;
        int size = PortraitSize(box), y = box.Top + 58;
        if (player.Id != person.Id) DrawSpeaker(player, SpeakingPortrait(player, box, false), false);
        DrawSpeaker(person, SpeakingPortrait(person, box, true), true);
        int middle = box.Center.X - 128;
        Text(_panel == "camp-story" ? "BY THE CAMPFIRE" : "A MOMENT OFF THE ROAD", middle, box.Top + 88, Muted);
        Text("Trust " + person.Trust.GetValueOrDefault(player.Id), middle, box.Top + 120, Gold);
        Text($"Hunger {person.Body.Hunger:0} / Fatigue {person.Body.Fatigue:0}", middle, box.Top + 144, Muted);
        Text($"Wounds {person.Body.Wounds} / {person.Job}", middle, box.Top + 168, Muted);
        Text(Short(_session.RoutineDescription(person), 32), middle, box.Top + 212, Muted);
        if (_session.Content.Entries.GetValueOrDefault(person.CombatTrait) is TalentDefinition trait)
        {
            Text(Short(trait.Name, 32), middle, box.Top + 190, Gold);
        }
        var speech = SpeechBox(box); Fill(speech, new(15, 23, 29));
        Text(_dialogue?.Line?.Name ?? person.Name, speech.X + 16, speech.Y + 12, Gold);
        Text(_dialogue?.Text ?? "", speech.X + 16, speech.Y + 36);
        if (_dialogue?.Finished == true) Text("Choose what to say below.", speech.X + 16, speech.Bottom - 18, Muted);
        void DrawSpeaker(Actor actor, Rectangle rect, bool flip)
        {
            bool current = _dialogue?.Line?.Speaker == actor.Id;
            _sprites.Draw(_batch, actor.Sprite, rect, current ? Ink : Muted * .8f,
                effects: flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            Text(Short(actor.Name, 28), rect.Center.X - Math.Min(actor.Name.Length, 28) * 4, y + size + 8, current ? Gold : Muted);
            Text(_session.Class(actor).Name, rect.Center.X - _session.Class(actor).Name.Length * 4, y + size + 27, Muted);
        }
    }
    private void ChooseDialogue(InteractionDefinition interaction)
    {
        if (_dialogue?.Finished != true) return;
        var result = _session.PerformInteraction(_person, interaction.Id); _notice = result.Message;
        if (!result.Success) return;
        ShowDialogue(WorldDialogue.Lines(_session, _person, interaction.Id, result.Message));
        _afterDialogue = interaction.Action switch { "recruit" => "close", "trade" => "trade", _ => "" };
    }
}
