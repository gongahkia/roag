import 'dart:math' as math;

import 'rules.dart';
import 'terrain.dart';

enum Season {
  spring('Spring', 1.10),
  summer('Summer', 1.00),
  autumn('Autumn', 1.15),
  winter('Winter', .80);

  final String label;
  final double farm;
  const Season(this.label, this.farm);
}

enum WeatherKind {
  clear('Clear', 1, 1),
  rain('Rain', 1.25, 1),
  wind('Windy', 1.10, .95),
  snow('Snow', 1.40, .90);

  final String label;
  final double travel, farm;
  const WeatherKind(this.label, this.travel, this.farm);
  bool get covered => this == rain || this == snow;
  String get effect => switch (this) {
    clear => 'Ordinary travel and outdoor work.',
    rain => 'Dirt/Garden travel takes 1.25× time; covered Halls are preferred.',
    wind => 'Dirt/Garden travel takes 1.10× time; Farm output is 5% lower.',
    snow => 'Dirt/Garden travel takes 1.40× time; Farm output is 10% lower; Halls are preferred.',
  };
}

class Calendar {
  static const version = 1;
  final int anchorDay, daysPerSeason;
  final Season initialSeason;
  const Calendar({
    this.anchorDay = 0,
    this.daysPerSeason = 6,
    this.initialSeason = Season.spring,
  });
  int relativeDay(double clock) =>
      math.max(0, (clock / Rules.day).floor() - anchorDay);
  Season season(double clock) => Season
      .values[(relativeDay(clock) ~/ daysPerSeason + initialSeason.index) % 4];
  int seasonDay(double clock) => relativeDay(clock) % daysPerSeason + 1;
  int year(double clock) => relativeDay(clock) ~/ (daysPerSeason * 4) + 1;
  Map<String, Object> toJson() => {
    'version': version,
    'anchorDay': anchorDay,
    'daysPerSeason': daysPerSeason,
    'initialSeason': initialSeason.name,
  };
  factory Calendar.decode(dynamic d, double clock) {
    if (d is! Map ||
        d['version'] != version ||
        d['anchorDay'] is! int ||
        d['anchorDay'] < 0 ||
        d['anchorDay'] > (clock / Rules.day).floor() ||
        d['daysPerSeason'] is! int ||
        d['daysPerSeason'] < 2 ||
        d['daysPerSeason'] > 12) {
      throw const FormatException('Invalid calendar.');
    }
    return Calendar(
      anchorDay: d['anchorDay'],
      daysPerSeason: d['daysPerSeason'],
      initialSeason: Season.values.firstWhere(
        (s) => s.name == (d['initialSeason'] ?? 'spring'),
        orElse: () => throw const FormatException('Invalid initial season.'),
      ),
    );
  }
}

// Weather is a versioned, stateless day stream. Forecasts read the same scheduled
// result as execution, without consuming the event/terrain/visitor RNG streams.
class Climate {
  static const version = 1;
  final int seed;
  final Calendar calendar;
  final EnvironmentPreset environment;
  final WeatherKind? fixedWeather;
  WeatherKind? temporary;
  double temporaryStarts = 0, temporaryEnds = 0;
  Climate(
    this.seed, {
    this.calendar = const Calendar(),
    this.environment = EnvironmentPreset.plains,
    this.fixedWeather,
  });
  static const weights = {
    Season.spring: [45, 35, 20, 0],
    Season.summer: [65, 15, 20, 0],
    Season.autumn: [40, 35, 25, 0],
    Season.winter: [35, 10, 20, 35],
  };
  List<int> weightsFor(Season season) => [
    for (var i = 0; i < 4; i++)
      weights[season]![i] +
          (i == 1 && environment == EnvironmentPreset.river ? 5 : 0) +
          (environment == EnvironmentPreset.pineUplands &&
                  (i == 3 && season == Season.winter || i == 2)
              ? 15
              : 0) +
          (environment == EnvironmentPreset.saltCoast && (i == 1 || i == 2)
              ? 15
              : 0),
  ];
  WeatherKind scheduled(int absoluteDay) {
    if (fixedWeather != null) return fixedWeather!;
    final season = calendar.season(absoluteDay * Rules.day);
    final options = weightsFor(season),
        total = options.fold<int>(0, (a, b) => a + b);
    var n = (seed + absoluteDay * 104729 + 15485863) % 2147483647;
    n = n * 48271 % 2147483647;
    n = n * 48271 % 2147483647;
    var index = n % total;
    for (var i = 0; i < options.length; i++) {
      if (index < options[i]) return WeatherKind.values[i];
      index -= options[i];
    }
    return WeatherKind.clear;
  }

  WeatherKind at(double clock) =>
      temporary != null && clock >= temporaryStarts && clock < temporaryEnds
      ? temporary!
      : scheduled((clock / Rules.day).floor());
  WeatherKind tomorrow(double clock) =>
      scheduled((clock / Rules.day).floor() + 1);
  bool covered(double clock) =>
      at(clock).covered || calendar.season(clock) == Season.winter;
  double farmMultiplier(double clock, {bool harvest = false}) =>
      (calendar.season(clock).farm * at(clock).farm * (harvest ? 1.25 : 1))
          .clamp(.65, 1.50);
  double socialRepeat(double clock) =>
      Rules.leisureRepeatAfter *
      (calendar.season(clock) == Season.summer ? .80 : 1);
  void legacyShower(double start, double end) {
    temporary = WeatherKind.rain;
    temporaryStarts = start;
    temporaryEnds = end;
  }

  String forecast(double clock) {
    final day = (clock / Rules.day).floor() + 1, next = tomorrow(clock);
    return temporary != null && temporaryEnds > day * Rules.day
        ? 'Tomorrow: ${temporary!.label} for ${(temporaryEnds - day * Rules.day).ceil()}s, then ${next.label}.'
        : 'Tomorrow: ${next.label}, scheduled for the full day.';
  }

  Map<String, Object> toJson(double clock) => {
    'version': version,
    'seed': seed,
    'calendar': calendar.toJson(),
    if (fixedWeather != null) 'fixtureWeather': fixedWeather!.name,
    'day': (clock / Rules.day).floor(),
    'scheduled': scheduled((clock / Rules.day).floor()).name,
    'next': tomorrow(clock).name,
    if (temporary != null && temporaryEnds > clock)
      'window': {
        'kind': temporary!.name,
        'starts': temporaryStarts,
        'ends': temporaryEnds,
      },
  };
  factory Climate.decode(
    dynamic d,
    int seed,
    double clock,
    EnvironmentPreset environment,
  ) {
    if (d is! Map ||
        d['version'] != version ||
        d['seed'] != seed ||
        d['day'] is! int ||
        d['day'] != (clock / Rules.day).floor()) {
      throw const FormatException('Invalid weather schedule.');
    }
    final c = Climate(
      seed,
      calendar: Calendar.decode(d['calendar'], clock),
      environment: environment,
      fixedWeather: d['fixtureWeather'] == null
          ? null
          : WeatherKind.values.firstWhere(
              (w) => w.name == d['fixtureWeather'],
              orElse: () =>
                  throw const FormatException('Invalid fixture weather.'),
            ),
    );
    if (d['scheduled'] != c.scheduled(d['day']).name ||
        d['next'] != c.tomorrow(clock).name) {
      throw const FormatException(
        'Weather forecast disagrees with its versioned schedule.',
      );
    }
    final w = d['window'];
    if (w != null) {
      if (w is! Map ||
          w['kind'] != 'rain' ||
          w['starts'] is! num ||
          w['ends'] is! num ||
          !w['starts'].isFinite ||
          !w['ends'].isFinite ||
          w['starts'] < 0 ||
          w['starts'] > clock ||
          w['ends'] <= clock ||
          w['ends'] > clock + Rules.specialDuration ||
          w['ends'] - w['starts'] > Rules.specialDuration + 1e-6) {
        throw const FormatException('Invalid carried-over rain shower.');
      }
      c.legacyShower(w['starts'].toDouble(), w['ends'].toDouble());
    }
    return c;
  }
}
