// Developer fixtures use ordinary construction, actor travel and simulation.
import 'dart:io';
import 'dart:convert';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/climate.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/world_generation.dart';

import 'scaling_fixture.dart' show scalingTown;

bool paint(Colony c, Tool t, Cell a, [Cell? b]) {
  c.session.beginStroke(t, a);
  c.session.stroke!.paintTo(b ?? a);
  final ok = c.session.finishStroke();
  if (ok) c.reconcile(c.session.world);
  return ok;
}

void add(Colony c, Tool t, Cell p) {
  if (t.isRoad && c.world.at(p).path && c.world.at(p).road == t.roadKind) {
    return;
  }
  if ([Terrain.forest, Terrain.marsh].contains(c.world.at(p).terrain)) {
    paint(c, Tool.clear, p);
  }
  if (!paint(c, t, p)) {
    throw StateError('Invalid fixture $t at $p (${c.world.at(p).terrain})');
  }
}

Colony livingTown() {
  final c = Colony(
    LandscapeSession(WorldGeneration.create('202', EnvironmentPreset.plains)),
  );
  for (var x = 7; x <= 14; x++) {
    add(c, Tool.path, Cell(x, 15));
  }
  for (final y in [14, 16]) {
    for (var x = 2; x <= 7; x++) {
      add(c, Tool.cottage, Cell(x, y));
    }
  }
  add(c, Tool.garden, const Cell(8, 14));
  add(c, Tool.workshop, const Cell(10, 14));
  add(c, Tool.market, const Cell(12, 14));
  add(c, Tool.hall, const Cell(14, 14));
  add(c, Tool.green, const Cell(8, 16));
  c.speed = 0;
  return c;
}

void main(List<String> args) {
  final out = Directory(
    args.isEmpty ? '/tmp/roag-goal08-fixtures' : args.single,
  )..createSync(recursive: true);
  final town = livingTown();
  File('${out.path}/living-town.json').writeAsStringSync(town.encode());
  stdout.writeln(
    'New seeded town: ${town.people.values.map((r) => r.peopleKind.label).toList()}; ${town.session.money} milli-coins',
  );
  for (final season in Season.values) {
    final c = Colony.decode(town.encode());
    c.clock = season.index * 6 * 150;
    if (season == Season.winter) {
      while (c.weather != WeatherKind.snow) {
        c.clock += 150;
      }
    }
    c.navigation
      ..rain = c.raining
      ..weatherCost = c.weather.travel;
    File('${out.path}/${season.name}.json').writeAsStringSync(c.encode());
  }
  for (final n in [85, 250, 500]) {
    final c = scalingTown(n)..nextSpecialAt = 1e9;
    final available = c.world.cells
        .where(
          (p) =>
              !c.world.occupied(p) &&
              c.world.at(p).terrain.buildable &&
              c.world.access(p) != null,
        )
        .toList();
    add(c, Tool.market, available.removeAt(0));
    add(c, Tool.hall, available.removeAt(0));
    add(c, Tool.workshop, available.removeAt(0));
    c.climate = Climate(
      c.world.seed,
      calendar: const Calendar(initialSeason: Season.autumn),
      fixedWeather: WeatherKind.rain,
    );
    c.navigation
      ..rain = true
      ..weatherCost = c.weather.travel;
    c.speed = 1;
    for (var i = 0; i < 10000 && c.visits.length < 2; i++) {
      c.update(.05);
    }
    if (c.visits.length != 2) {
      throw StateError('Two active visitors not reached for $n');
    }
    c.speed = 0;
    Colony.decode(c.encode());
    File('${out.path}/scale-$n.json').writeAsStringSync(c.encode());
    stdout.writeln(
      '$n people; ${c.visits.length} active visitors; ${c.clock}s; rain/autumn',
    );
  }
  File('${out.path}/manifest.json').writeAsStringSync(
    jsonEncode({
      'seed': '202',
      'world': 'Gentle Plains',
      'smallPopulation': 12,
      'fixtureOnly': 'Season snapshots / developed rain-autumn fixtures; small town uses normal climate and paid construction.',
    }),
  );
}
