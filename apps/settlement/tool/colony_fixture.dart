// Reproducible checkpoints made only through normal construction and simulation.
import 'dart:io';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';

void paint(Colony colony, Tool tool, Cell from, [Cell? to]) {
  colony.session.beginStroke(tool, from);
  colony.session.stroke!.paintTo(to ?? from);
  if (!colony.session.finishStroke()) {
    throw StateError('Fixture stroke refused');
  }
  colony.reconcile(colony.session.world);
}

void main(List<String> args) {
  final dir = Directory(args.isEmpty ? '/tmp/roag-colony-fixtures' : args.first)
    ..createSync(recursive: true);
  final c = Colony(LandscapeSession(Landscape.create(202)));
  paint(c, Tool.path, const Cell(7, 15), const Cell(12, 15));
  paint(c, Tool.cottage, const Cell(2, 14), const Cell(5, 14));
  paint(c, Tool.garden, const Cell(8, 14));
  paint(c, Tool.green, const Cell(10, 14));
  File('${dir.path}/colony-start.json').writeAsStringSync(c.encode());
  for (var i = 0; i < 1700; i++) {
    c.update(.05);
  }
  File('${dir.path}/colony-evening-ready.json').writeAsStringSync(c.encode());
  for (var i = 0; i < 400; i++) {
    c.update(.05);
  }
  if (c.event == null || c.event!.awarded.length < 3) {
    throw StateError('Normal conditions did not produce a gathering');
  }
  File('${dir.path}/colony-gathering.json').writeAsStringSync(c.encode());
  final cut = Colony.decode(
    File('${dir.path}/colony-start.json').readAsStringSync(),
  );
  for (var i = 0; i < 440; i++) {
    cut.update(.05);
  }
  paint(cut, Tool.remove, const Cell(9, 15));
  for (var i = 0; i < 2160; i++) {
    cut.update(.05);
  }
  cut.speed = 0;
  File('${dir.path}/colony-night-cut.json').writeAsStringSync(cut.encode());
  stdout.writeln(
    'Seed 202: four homes, one garden, one green; normal work/social routines; ${c.event!.awarded.length} actual gathering attendees.',
  );
}
