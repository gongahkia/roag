// full-state checkpoints for the no-dialogue-choice regression replay.
import 'dart:convert';
import 'dart:io';
import 'package:roag_settlement/colony.dart';

void main(List<String> args) {
  final c = Colony.decode(File('../../docs/world-builder/evidence/tranche-05/large-85.json').readAsStringSync())..speed = 1;
  final samples = [];
  for (var i = 0; i < 3600; i++) {
    c.update(.05);
    if (i % 100 == 0) {
      samples.add({'state': jsonDecode(c.encode()), 'moods': {for (final r in c.people.values) r.homeId: c.happiness(r)}});
    }
  }
  File(args.single).writeAsStringSync(jsonEncode(samples));
  stdout.writeln('85 actual residents; 180 simulation seconds; 36 full checkpoints.');
}
