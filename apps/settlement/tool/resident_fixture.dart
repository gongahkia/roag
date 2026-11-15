// safe fixtures from normal construction/simulation, never the player's town.
import 'dart:io';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';

import 'colony_fixture.dart' show paint;

void main(List<String> args) {
  final dir = Directory(
    args.isEmpty ? '/tmp/roag-resident-fixtures' : args.first,
  )..createSync(recursive: true);
  final c = Colony.decode(
    File('../../docs/world-builder/evidence/tranche-03/legacy-74.json')
        .readAsStringSync(),
  );
  final candidates = [
    for (var y = 19; y <= 21; y++)
      for (var x = 1; x < 28; x++) Cell(x, y),
  ];
  for (final tool in [
    for (var i = 0; i < 6; i++) Tool.garden,
    for (var i = 0; i < 3; i++) Tool.green,
    for (var i = 0; i < 11; i++) Tool.cottage,
  ]) {
    final cell = candidates.firstWhere(
      (cell) => evaluate(c.world, tool, cell).valid,
    );
    paint(c, tool, cell);
  }
  if (c.people.length != 85) throw StateError('Expected 85 residents');
  File('${dir.path}/residents-85-start.json').writeAsStringSync(c.encode());
  for (var i = 0; i < 440; i++) {
    c.update(.05);
  }
  if (c.workEarned <= 0 || !c.people.values.any((r) => r.lastSocial >= 0)) {
    throw StateError('Fixture did not reach real work and social visits');
  }
  File('${dir.path}/residents-85-active.json').writeAsStringSync(c.encode());
  stdout.writeln(
    '85 residents; 6 Farms, 3 Parks; ${c.workEarned} milli-coins from real work; ${c.people.values.where((r) => r.lastSocial >= 0).length} completed social visits.',
  );
}
