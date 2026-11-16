// Explicit developer checkpoints: construction, staffing and travel use real rules.
// Only startIncident(fixture: true) bypasses the normal grace/cooldown schedule.
import 'dart:convert';
import 'dart:io';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/climate.dart';
import 'package:roag_settlement/model.dart';

import 'goal08_fixture.dart' show livingTown, add, paint;
import 'goal07_fixture.dart' show terrainTown;
import 'scaling_fixture.dart' show scalingTown;

void advance(Colony c, double seconds) {
  c.speed = 1;
  for (var i = 0; i < (seconds / .05).round(); i++) {
    c.update(.05);
  }
}

Colony baseTown({
  bool firehouse = true,
  bool watch = true,
  bool refuge = true,
  bool inn = true,
  bool naturalClimate = false,
}) {
  final c = livingTown()
    ..nextVisitorAt = 1e9
    ..nextSpecialAt = 1e9;
  c.climate = naturalClimate
      ? Climate(202)
      : Climate(
          202,
          calendar: const Calendar(initialSeason: Season.summer),
          fixedWeather: WeatherKind.clear,
        );
  add(c, Tool.path, const Cell(13, 16));
  add(c, Tool.path, const Cell(13, 17));
  if (firehouse) add(c, Tool.firehouse, const Cell(10, 16));
  if (watch) add(c, Tool.watchpost, const Cell(12, 16));
  if (refuge) add(c, Tool.refuge, const Cell(14, 16));
  if (inn) add(c, Tool.inn, const Cell(14, 17));
  advance(c, 20);
  c.incidentMode = IncidentMode.standard;
  c.nextIncidentAt = 1e9;
  return c;
}

void main(List<String> args) {
  final out = Directory(
    args.isEmpty ? '/var/tmp/roag-goal09-fixtures' : args.single,
  )..createSync(recursive: true);
  void save(String name, Colony c) {
    c.speed = 0;
    final text = c.encode();
    if (Colony.decode(text).encode() != text) {
      throw StateError('Invalid exact checkpoint $name');
    }
    File('${out.path}/$name.json').writeAsStringSync(text);
    stdout.writeln(
      '$name: ${c.people.length} residents; ${c.clock.toStringAsFixed(2)}s; incident ${c.incident?.state}; ${c.session.money} milli-coins',
    );
  }

  save('prepared', baseTown());
  for (final type in IncidentKind.values) {
    for (final protected in [true, false]) {
      final c = baseTown(
        firehouse: type == IncidentKind.fire ? protected : true,
        watch: type == IncidentKind.wildlife ? protected : true,
      );
      c.startIncident(
        type,
        target: type == IncidentKind.fire
            ? c.world.at(const Cell(2, 14)).buildingId
            : c.places(Place.garden).first.key,
        fixture: true,
      );
      save('${type.name}-${protected ? 'protected' : 'unprotected'}', c);
    }
  }
  // Continuous video starts in the warning, with trained firefighters. The player
  // builds the refuge, watchpost and Inn, then watches, repairs and resumes life.
  final preparation = baseTown(watch: false, refuge: false, inn: false);
  preparation.startIncident(
    IncidentKind.fire,
    target: preparation.world.at(const Cell(2, 14)).buildingId,
    fixture: true,
  );
  save('preparation', preparation);
  final natural = baseTown(naturalClimate: true);
  natural.nextIncidentAt = 450;
  advance(
    natural,
    429,
  ); // 449s: no forced incident. The real scheduler runs at 450.
  save('natural', natural);
  final bridge = terrainTown(12)
    ..nextVisitorAt = 1e9
    ..nextSpecialAt = 1e9;
  final left = bridge.world.cells.firstWhere(
    (p) =>
        !bridge.world.occupied(p) &&
        bridge.world.at(p).terrain.buildable &&
        bridge.world.access(p) != null &&
        p.x < 10,
  );
  add(bridge, Tool.firehouse, left);
  advance(bridge, 25);
  bridge.incidentMode = IncidentMode.standard;
  bridge.nextIncidentAt = 1e9;
  final right = bridge.world.cells.firstWhere(
    (p) =>
        !bridge.world.occupied(p) &&
        bridge.world.at(p).terrain.buildable &&
        bridge.world.access(p) != null &&
        p.x > 25,
  );
  add(bridge, Tool.garden, right);
  bridge.startIncident(
    IncidentKind.fire,
    target: bridge.world.at(right).placeId,
    fixture: true,
  );
  save('bridge-response', bridge);
  final measurements = <String, Object>{};
  for (final n in [85, 250, 500]) {
    final c = scalingTown(n)
      ..nextSpecialAt = 1e9
      ..nextVisitorAt = 1e9;
    c.session.unlimited = true;
    final frontage = c.world.cells
        .where(
          (p) =>
              !c.world.occupied(p) &&
              c.world.at(p).terrain.buildable &&
              c.world.access(p) != null,
        )
        .toList();
    for (final t in [
      Tool.firehouse,
      Tool.watchpost,
      Tool.refuge,
      Tool.inn,
      Tool.market,
      Tool.hall,
    ]) {
      add(c, t, frontage.removeAt(0));
    }
    c.climate = Climate(
      c.world.seed,
      calendar: const Calendar(initialSeason: Season.autumn),
      fixedWeather: WeatherKind.rain,
    );
    advance(c, 20);
    c.incidentMode = IncidentMode.standard;
    c.nextIncidentAt = 1e9;
    for (final kind in IncidentKind.values) {
      final d = Colony.decode(c.encode());
      d.startIncident(
        kind,
        target: d.places(Place.garden).first.key,
        fixture: true,
      );
      save('${kind.name}-scale-$n', d);
      final watch = Stopwatch()..start();
      final nav = d.navigation.expansions, plans = d.planned;
      advance(d, 30);
      watch.stop();
      measurements['${kind.name}-$n'] = {
        'wallMicros': watch.elapsedMicroseconds,
        'simSeconds': 30,
        'expansions': d.navigation.expansions - nav,
        'plans': d.planned - plans,
        'maxActors': n + 1,
        'outcome': d.incidentHistory.isEmpty
            ? d.incident?.state
            : d.incidentHistory.last.toJson(),
      };
    }
    if (n == 500) {
      final w = Colony.decode(c.encode());
      w.climate = Climate(w.world.seed);
      var nextDay = (w.clock / 150).floor() + 1;
      while (w.climate.scheduled(nextDay - 1) == w.climate.scheduled(nextDay)) {
        nextDay++;
      }
      advance(w, nextDay * 150 - .5 - w.clock);
      w.startIncident(
        IncidentKind.fire,
        target: w.places(Place.garden).first.key,
        fixture: true,
      );
      save('weather-scale-500', w);
      final spread = Colony.decode(c.encode());
      paint(spread, Tool.remove, spread.places(Place.firehouse).first.value);
      spread.climate = Climate(
        spread.world.seed,
        calendar: const Calendar(initialSeason: Season.summer),
        fixedWeather: WeatherKind.wind,
      );
      spread.startIncident(
        IncidentKind.fire,
        target: spread.places(Place.garden).first.key,
        fixture: true,
      );
      save('spread-scale-500', spread);
    }
  }
  File(
    '${out.path}/model-performance.json',
  ).writeAsStringSync(const JsonEncoder.withIndent('  ').convert(measurements));
}
