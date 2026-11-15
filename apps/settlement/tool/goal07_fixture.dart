// generated, fully valid populated river fixtures; construction uses real rules.
import 'dart:convert';
import 'dart:io';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/world_generation.dart';

Colony terrainTown(int population) {
  final c = Colony(
    LandscapeSession(WorldGeneration.create('202', EnvironmentPreset.river)),
  )..nextSpecialAt = 1e9;
  c.session.unlimited = true;
  bool paint(Tool t, Cell a, [Cell? b]) {
    c.session.beginStroke(t, a);
    c.session.stroke!.paintTo(b ?? a);
    final ok = c.session.finishStroke();
    if (ok) c.reconcile(c.session.world);
    return ok;
  }

  for (final p in c.world.cells.toList()) {
    if (c.world.at(p).terrain == Terrain.forest) paint(Tool.clear, p);
  }
  paint(Tool.path, const Cell(0, 15), const Cell(0, 0));
  paint(Tool.path, const Cell(0, 15), const Cell(0, 29));
  for (var pass = 0; pass < 3; pass++) {
    for (var y = 1; y < 30; y += 2) {
      paint(Tool.path, Cell(0, y), Cell(39, y));
      final water = c.world.cells
          .where((p) => p.y == y && c.world.at(p).terrain == Terrain.water)
          .toList();
      paint(Tool.bridge, Cell(water.first.x - 1, y), Cell(water.last.x + 1, y));
      paint(Tool.path, Cell(water.last.x + 1, y), Cell(39, y));
    }
    paint(Tool.path, const Cell(39, 15), const Cell(39, 0));
    paint(Tool.path, const Cell(39, 15), const Cell(39, 29));
  }
  for (final p in c.world.cells.toList()) {
    if (!c.world.occupied(p) && c.world.at(p).terrain == Terrain.marsh) {
      paint(Tool.clear, p);
    }
  }
  final candidates = c.world.cells
      .where(
        (p) =>
            !c.world.occupied(p) &&
            c.world.at(p).terrain.buildable &&
            c.world.access(p) != null,
      )
      .toList();
  if (candidates.length < population + 15) {
    throw StateError('Insufficient valid frontage: ${candidates.length}');
  }
  for (var i = 0; i < 10; i++) {
    if (!paint(Tool.garden, candidates.removeAt(0))) throw StateError('Farm');
  }
  for (var i = 0; i < 5; i++) {
    if (!paint(Tool.green, candidates.removeAt(0))) throw StateError('Park');
  }
  final chosen = [
    for (var i = 0; i < population; i++)
      candidates[(i * candidates.length / population).floor()],
  ];
  for (final p in chosen) {
    if (!paint(Tool.cottage, p)) throw StateError('Cottage');
  }
  if (c.people.length != population || c.world.completeBridgeCells.isEmpty) {
    throw StateError('Invalid populated river fixture');
  }
  c.speed = 0;
  Colony.decode(c.encode());
  return c;
}

void main(List<String> args) {
  final out = Directory(
    args.isEmpty ? '/tmp/roag-goal07-fixtures' : args.single,
  )..createSync(recursive: true);
  final measurements = <String, Object>{};
  for (final p in EnvironmentPreset.values) {
    final samples = <int>[];
    for (var seed = 0; seed < 100; seed++) {
      final watch = Stopwatch()..start();
      WorldGeneration.create('$seed', p);
      samples.add(watch.elapsedMicroseconds);
    }
    samples.sort();
    measurements[p.name] = {
      'samples': 100,
      'medianUs': samples[50],
      'p95Us': samples[95],
      'maxUs': samples.last,
    };
  }
  for (final n in [85, 250, 500]) {
    final c = terrainTown(n);
    File('${out.path}/scale-$n.json').writeAsStringSync(c.encode());
    stdout.writeln(
      '$n actual residents; ${c.world.completeBridgeCells.length} bridge cells; ${c.world.activePaths.length} connected road cells.',
    );
  }
  File('${out.path}/generation-timing.json')
      .writeAsStringSync(jsonEncode(measurements));
}
