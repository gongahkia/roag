// connected developer fixtures: actual homes/residents; no synthetic counters.
import 'dart:io';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';

Colony scalingTown(int population) {
  const w = 40, h = 30;
  final c = Colony(
    LandscapeSession(
      Landscape(202, w, h, [
        for (var y = 0; y < h; y++)
          for (var x = 0; x < w; x++)
            Lot(grass: (x + y) % 2, path: x == 0 || y.isOdd),
      ]),
    ),
  );
  c.session.unlimited = true;
  void add(Tool t, Cell p) {
    c.session.beginStroke(t, p);
    c.session.stroke!.paintTo(p);
    if (!c.session.finishStroke()) throw StateError('invalid fixture');
    c.reconcile(c.session.world);
  }

  final candidates = [
    for (var y = 0; y < h; y += 2)
      for (var x = 1; x < w; x++) Cell(x, y),
  ];
  for (var i = 0; i < 10; i++) {
    add(Tool.garden, candidates.removeAt(0));
  }
  for (var i = 0; i < 5; i++) {
    add(Tool.green, candidates.removeAt(0));
  }
  final chosen = [
    for (var i = 0; i < population; i++)
      candidates[(i * candidates.length / population).floor()],
  ];
  for (final p in chosen) {
    add(Tool.cottage, p);
  }
  c.speed = 0;
  if (c.people.length != population ||
      !c.world.cottages.values.every((p) => c.world.access(p) != null)) {
    throw StateError('invalid population');
  }
  return c;
}

void main(List<String> args) {
  final d = Directory(
    args.isEmpty ? '/tmp/roag-tranche05-fixtures' : args.first,
  )..createSync(recursive: true);
  for (final n in [85, 250, 500]) {
    final c = scalingTown(n)..nextSpecialAt = 1e9;
    File('${d.path}/scale-$n.json').writeAsStringSync(c.encode());
    stdout.writeln(
      '$n real residents; ${c.world.activePaths.length} connected road cells',
    );
  }
}
