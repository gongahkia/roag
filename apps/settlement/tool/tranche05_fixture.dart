import 'dart:io';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/special_event.dart';

import 'colony_fixture.dart' show paint;

Colony developedTown() {
  final c = Colony(LandscapeSession(Landscape.create(202)));
  paint(c, Tool.path, const Cell(7, 15), const Cell(18, 15));
  paint(c, Tool.cottage, const Cell(2, 14), const Cell(5, 14));
  paint(c, Tool.cottage, const Cell(2, 16), const Cell(5, 16));
  for (final (tool, x) in [
    (Tool.garden, 6),
    (Tool.workshop, 8),
    (Tool.market, 10),
    (Tool.hall, 16),
    (Tool.green, 12),
  ]) {
    paint(c, tool, Cell(x, 14));
  }
  return c;
}

void main(List<String> args) {
  final dir = Directory(args.isEmpty ? '/tmp/roag-tranche05-play' : args.first)
    ..createSync(recursive: true);
  final c = developedTown();
  c.speed = 0;
  File('${dir.path}/opening.json').writeAsStringSync(c.encode());
  c.speed = 1;
  c.nextSpecialAt = 1e9;
  for (var i = 0; i < 280; i++) {
    c.update(.05);
  }
  for (final kind in SpecialKind.values) {
    final copy = Colony.decode(c.encode());
    if (!copy.beginSpecial(kind, fixture: true))
      throw StateError('event not eligible: $kind');
    File('${dir.path}/${kind.name}.json').writeAsStringSync(copy.encode());
  }
  c.nextSpecialAt = c.clock + 12;
  File('${dir.path}/active.json').writeAsStringSync(c.encode());
  stdout.writeln(
    'Eight actual residents, five functional destinations; eligible real work/service fixtures for all three explicitly forced special events.',
  );
}
