using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Tests.Content;

public sealed class ContentTests
{
    public static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Vagabonds.sln"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Cannot find the repository content.");
        }
    }
    public static (string File, string Text)[] Documents() => ContentCatalog.Default.Entries.Values
        .Select(d => (File: ContentCatalog.Default.Source(d.Id), Text: JsonSerializer.Serialize<ContentDefinition>(d, ContentCatalog.Json))).ToArray();
    public static ContentCatalog Edited(string id, Action<JsonObject> edit)
    {
        var documents = Documents(); int index = Array.FindIndex(documents, d => JsonNode.Parse(d.Text)!["id"]!.GetValue<string>() == id);
        var node = JsonNode.Parse(documents[index].Text)!.AsObject(); edit(node); documents[index].Text = node.ToJsonString();
        return ContentCatalog.Load(documents);
    }
    [Fact]
    public void AuthoredAndEmbeddedContentHaveTheSameMeaningAndNoDanglingReferences()
    {
        var authored = ContentCatalog.LoadDirectory(Path.Combine(Root, "content")); Assert.Empty(authored.Issues);
        Assert.Equal(ContentCatalog.Default.Hash, authored.Hash); Assert.True(authored.Entries.Count >= 78);
        Assert.Equal(4, authored.Classes.Length); Assert.All(authored.References.Where(r => r.Source != "$runtime"), r => Assert.Contains(r.Target, authored.Entries.Keys));
    }
    [Theory]
    [InlineData("unknown-field")]
    [InlineData("sprite")]
    [InlineData("effect")]
    [InlineData("mana")]
    [InlineData("null-tags")]
    [InlineData("missing-name")]
    [InlineData("unknown-reference")]
    public void InvalidAbilityEditsAreReportedAtTheirSource(string kind)
    {
        var catalog = Edited("ability.longsword", node =>
        {
            switch (kind)
            {
                case "unknown-field": node["powre"] = 10; break;
                case "sprite": node["sprite"] = "invented-artwork"; break;
                case "effect": node["effect"] = "execute-arbitrary-code"; break;
                case "mana": node["mana"] = -1; break;
                case "null-tags": node["tags"] = null; break;
                case "missing-name": node.Remove("name"); break;
                case "unknown-reference": node["id"] = "ability.renamed"; break;
            }
        });
        Assert.NotEmpty(catalog.Issues); Assert.All(catalog.Issues, issue => Assert.False(string.IsNullOrWhiteSpace(issue.File)));
    }
    [Fact]
    public void DuplicateIdsInvalidCostsAndUnknownInteractionHandlersFailValidation()
    {
        var docs = Documents(); Assert.NotEmpty(ContentCatalog.Load([.. docs, docs[0]]).Issues);
        Assert.NotEmpty(Edited("interaction.healing", n => n["cost"]!["item.gold"] = -1).Issues);
        Assert.NotEmpty(Edited("interaction.healing", n => n["action"] = "wish").Issues);
        Assert.NotEmpty(Edited("boss.ash-knight", n => n["behaviors"] = new JsonArray()).Issues);
        Assert.NotEmpty(ContentCatalog.Load([]).Issues);
    }
    [Fact]
    public void EditedCharacterProfilesAndAbilitiesDriveActualWorldCommands()
    {
        var docs = Documents();
        for (int i = 0; i < docs.Length; i++)
        {
            var node = JsonNode.Parse(docs[i].Text)!.AsObject(); string id = node["id"]!.GetValue<string>();
            if (id == "names.travellers") { node["first"] = new JsonArray("Test"); node["last"] = new JsonArray("Traveller"); }
            if (id == "character.traveller") node["classes"] = new JsonArray("class.warden");
            if (id == "ability.longsword") { node["name"] = "Test strike"; node["power"] = 17; }
            docs[i].Text = node.ToJsonString();
        }
        var content = ContentCatalog.Load(docs); Assert.Empty(content.Issues); var world = WorldSession.Create(42, content);
        Assert.Equal("Test Traveller", world.Party.First().Name);
        world.StartBattle(world.State.Actors.First(a => a.Kind == ActorKind.Beast).Id);
        while (!world.State.Battle!.Active.Friendly) world.AdvanceEnemy();
        Assert.Equal("Test strike", world.ActiveSkills()[0].Name); Assert.Equal(17, world.ActiveSkills()[0].Power);
    }
    [Fact]
    public void PaidInteractionUsesActualInventoryAndInvalidAttemptsChangeNothing()
    {
        var world = WorldSession.Create(42); var healer = world.State.Actors.First(a => a.Archetype == world.Content.Game.Healer);
        healer.Position = world.State.Position.Offset(1, 0); string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.PerformInteraction(healer.Id, "interaction.healing").Success); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        var hero = world.Party.First(); hero.Health -= 10; int gold = world.Count("gold"); long tick = world.State.Tick; uint random = world.State.CombatRandom;
        Assert.True(world.PerformInteraction(healer.Id, "interaction.healing").Success);
        Assert.Equal(hero.MaxHealth, hero.Health); Assert.Equal(gold - 5, world.Count("gold")); Assert.Equal(tick, world.State.Tick); Assert.Equal(random, world.State.CombatRandom);
        world.State.Inventory["gold"] = 0; hero.Health--; before = JsonSerializer.Serialize(world.State);
        Assert.False(world.PerformInteraction(healer.Id, "interaction.healing").Success); Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
    [Fact]
    public void RemovingRecruitmentFromAnArchetypeAlsoBlocksDirectRecruitCommands()
    {
        var content = Edited("character.villager", node => node["interactions"] = new JsonArray("interaction.stories"));
        var world = WorldSession.Create(42, content); var npc = world.State.Actors.First(a => a.Archetype == content.Game.Villager);
        npc.Position = world.State.Position.Offset(1, 0); string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.Submit(new(WorldCommandKind.Recruit, ActorId: npc.Id)).Success);
        Assert.False(world.PerformInteraction(npc.Id, "interaction.recruit").Success); Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
}
