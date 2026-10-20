namespace Ithica.Core.Colony;

/// <summary>Observable station reports. Director diagnostics stay in tooling and save state.</summary>
public static class StationReports
{
    public static string Conditions(Simulation station) =>
        $"AIR {station.Pressure * 100:0}% / POWER {station.Generated:0.#}:{station.Demand:0.#} KW / WATER {station.Resources.Water:0.#} / FOOD {station.Resources.Food:0}";

    public static string Followup(string kind) => kind switch
    {
        "pump" => "Pump service", "harvest" => "Seed culture", "cache" => "Locker recovery", _ => "Work underway"
    };

    public static string Outcome(IncidentRecord record)
    {
        var definition = Simulation.IncidentLibrary.Single(d => d.Id == record.DefinitionId);
        string text = record.Outcome;
        const string unanswered = "No response; ";
        string prefix = text.StartsWith(unanswered, StringComparison.Ordinal) ? unanswered : "";
        string body = text[prefix.Length..];
        // Historical saves keep their bytes; translate old handler IDs only for display.
        foreach (var choice in definition.Choices)
            if (body == choice.Id || body.StartsWith(choice.Id + " /", StringComparison.Ordinal))
                return prefix + choice.Label + body[choice.Id.Length..];
        return text;
    }

    public static string Notice(string text)
    {
        int legacy = text.IndexOf(" / decision #", StringComparison.Ordinal);
        if (legacy >= 0) return text[..legacy] + ". Read station reports [I].";
        foreach (var definition in Simulation.IncidentLibrary)
        {
            string prefix = definition.Title + ": ";
            if (text.StartsWith(prefix, StringComparison.Ordinal))
                return prefix + Outcome(new(0, definition.Id, 0, text[prefix.Length..]));
        }
        return text;
    }
}
