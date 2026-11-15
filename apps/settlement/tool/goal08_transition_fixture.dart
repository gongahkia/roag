// A genuine 500-person simulation stopped just before its first season boundary.
import 'dart:io';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';

import 'scaling_fixture.dart' show scalingTown;
import 'goal08_fixture.dart' show add;

void main() {
  final c = scalingTown(500)..nextSpecialAt = 1e9;
  final available = c.world.cells
      .where((p) => !c.world.occupied(p) && c.world.access(p) != null)
      .toList();
  add(c, Tool.market, available.removeAt(0));
  add(c, Tool.hall, available.removeAt(0));
  add(c, Tool.workshop, available.removeAt(0));
  c.speed = 1;
  while (c.clock < 897) {
    c.update(.05);
  }
  c.speed = 0;
  final out = Directory('/tmp/roag-goal08-transition')
    ..createSync(recursive: true);
  Colony.decode(c.encode());
  File('${out.path}/scale-500.json').writeAsStringSync(c.encode());
  stdout.writeln(
    '${c.clock}s ${c.season.label} / ${c.weather.label}; next ${c.climate.tomorrow(c.clock).label}; ${c.guestCompletions}',
  );
}
