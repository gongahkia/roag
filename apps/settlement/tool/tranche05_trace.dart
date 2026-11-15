import 'dart:io';

import 'colony_fixture.dart' show paint;
import 'tranche05_fixture.dart' show developedTown;

import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/resident_identity.dart';

void main(List<String> args) {
  final c = developedTown();
  if (args.contains('--four')) {
    paint(c, Tool.remove, const Cell(2, 16), const Cell(5, 16));
  }
  for (var i = 0; i < 1800; i++) {
    c.update(.05);
    if (i % 100 == 0) {
      if (args.contains('--four')) {
        for (final r in c.people.values) {
          stdout.writeln(
            '${r.homeId} ${r.activity} job=${r.workId} dest=${r.destination} work=${r.workProgress} social=${r.socialProgress}',
          );
        }
      }
      final market = c.places(Place.market).first.key;
      stdout.writeln(
        '${c.clock.toStringAsFixed(1)}: ${c.special?.kind.name} vendors=${c.workers(market)} customers=${c.occupancy(market)} trade=${c.tradeEarned} hall=${c.occupancy(c.places(Place.hall).first.key)}',
      );
    }
  }
  for (final r in c.people.values) {
    stdout.writeln(
      '${r.homeId} trait=${r.trait} work=${r.workId} state=${r.activity} dest=${r.destination} lastWork=${r.lastWork} lastSocial=${r.lastSocial} trade=${r.tradeDay} pref=${ResidentIdentity.hash(r.homeId, 379) % 2}',
    );
  }
}
