using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private ClimateConditions? _weatherTextConditions;
    private string _weatherText = "", _weatherCaption = "";
    private (ClimateConditions Climate, bool Covered)? _weatherCaptionConditions;
    private string WeatherText()
    {
        var weather = _session.WeatherAt(_session.State.Position);
        if (_weatherTextConditions != weather)
        { _weatherTextConditions = weather; _weatherText = _session.WeatherDescription(_session.State.Position); }
        return _weatherText;
    }
    private string WeatherCaption()
    {
        var weather = _session.WeatherAt(_session.State.Position); bool covered = _session.ShelteredAt(_session.State.Position);
        var key = (weather, covered);
        if (_weatherCaptionConditions != key)
        {
            _weatherCaptionConditions = key;
            _weatherCaption = weather.Season + " " + (_session.State.Tick / WorldSession.TicksPerDay % WorldSession.SeasonDays + 1) + "/" + WorldSession.SeasonDays
                + " / " + (weather.Underground ? "Underground" : weather.Weather.ToString()) + " / " + (covered ? "Covered" : "Exposed");
        }
        return _weatherCaption;
    }
}
