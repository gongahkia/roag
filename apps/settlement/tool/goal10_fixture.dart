// Paid, deterministic comparison towns. Only explicitly labelled incident starts
// bypass the director. The water pocket is authored before the encounter.
import 'dart:io';
import 'dart:convert';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/climate.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/world_generation.dart';

import 'goal09_fixture.dart' show baseTown, advance;
import 'goal08_fixture.dart' show add;
import 'scaling_fixture.dart' show scalingTown;

Colony floodTown({bool stone = false, bool start = true}) {
  final c = baseTown();
  var w = c.world;
  for (var x = 5; x <= 9; x++) {
    w = w.replace(Cell(x, 12), Lot(grass: 0, terrain: Terrain.water));
  }
  c.session.world = w;
  c.reconcile(w);
  c.climate = Climate(w.seed, fixedWeather: WeatherKind.rain);
  if (stone) {
    for (var x = 5; x <= 9; x++) {
      add(c, Tool.stoneRoad, Cell(x, 15));
    }
  }
  c.nextStoryTick = 0;
  if (start) {
    c.startIncident(
      IncidentKind.flood,
      target: c.world.at(const Cell(7, 14)).buildingId,
      fixture: true,
    );
  }
  return c;
}

void main(List<String> args) {
  final out = Directory(
    args.isEmpty ? '/var/tmp/roag-goal10/fixtures' : args.first,
  )..createSync(recursive: true);
  final generationTimes = <String, Object>{};
  for (final p in [
    EnvironmentPreset.wetlands,
    EnvironmentPreset.highlands,
    EnvironmentPreset.frontier,
  ]) {
    final samples = <int>[];
    for (var seed = 0; seed < 100; seed++) {
      final watch = Stopwatch()..start();
      WorldGeneration.create('$seed', p);
      samples.add(watch.elapsedMicroseconds);
    }
    samples.sort();
    generationTimes[p.name] = {
      'samples': 100,
      'meanUs': samples.reduce((a, b) => a + b) / samples.length,
      'p95Us': samples[95],
      'maxUs': samples.last,
    };
  }
  File('${out.path}/generation-times.json')
      .writeAsStringSync(jsonEncode(generationTimes));
  void save(String name, Colony c) {
    c.speed = 0;
    final text = c.encode();
    if (Colony.decode(text).encode() != text) throw StateError('Inexact $name');
    File('${out.path}/$name.json').writeAsStringSync(text);
    stdout.writeln(
      '$name: ${c.people.length} residents, ${c.clock}s, ${c.incident?.kind}',
    );
  }

  for (final p in [
    EnvironmentPreset.wetlands,
    EnvironmentPreset.highlands,
    EnvironmentPreset.frontier,
  ]) {
    save(
      'empty-${p.name}',
      Colony(LandscapeSession(WorldGeneration.create('202', p))),
    );
  }
  save('flood-path', floodTown());
  save('flood-stone', floodTown(stone: true));
  for (final protect in [true, false]) {
    final c = baseTown(watch: protect)
      ..climate = Climate(202, fixedWeather: WeatherKind.rain);
    c.startIncident(IncidentKind.raiders, fixture: true);
    save('raid-${protect ? 'prepared' : 'unprepared'}', c);
  }
  final caravan = baseTown()
    ..climate = Climate(202, fixedWeather: WeatherKind.rain);
  caravan.startIncident(
    IncidentKind.caravan,
    target: caravan.places(Place.inn).first.key,
    fixture: true,
  );
  save('caravan', caravan);
  final natural = baseTown(naturalClimate: true);
  natural.nextIncidentAt = 450;
  advance(natural, 429);
  save('natural', natural);
  final trade = baseTown()
    ..scenario = ScenarioKind.trade
    ..nextVisitorAt = 20;
  save('trade', trade);
  for (final n in [85, 250, 500]) {
    final c = scalingTown(n)
      ..nextSpecialAt = 1e9
      ..nextVisitorAt = 1e9;
    final spots = c.world.cells
        .where((p) => !c.world.occupied(p) && c.world.access(p) != null)
        .toList();
    for (final t in [Tool.market, Tool.inn, Tool.watchpost, Tool.refuge]) {
      add(c, t, spots.removeAt(0));
    }
    advance(c, 20);
    c.incidentMode = IncidentMode.standard;
    c.nextIncidentAt = 1e9;
    c.climate = Climate(202, fixedWeather: WeatherKind.rain);
    final market = c.places(Place.market).first.value;
    var w = c.world;
    for (final p in [
      Cell(market.x, market.y + 2),
      Cell(market.x + 1, market.y + 2),
    ]) {
      if (w.contains(p) && !w.occupied(p)) {
        w = w.replace(p, Lot(grass: 0, terrain: Terrain.water));
      }
    }
    c.session.world = w;
    c.reconcile(w);
    for (final kind in [
      IncidentKind.raiders,
      IncidentKind.caravan,
      IncidentKind.flood,
    ]) {
      final d = Colony.decode(c.encode());
      if (!d.startIncident(kind, fixture: true)) {
        throw StateError('No $kind target in $n');
      }
      save('${kind.name}-scale-$n', d);
    }
  }
}
