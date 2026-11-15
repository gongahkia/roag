import 'tranche05_fixture.dart' show developedTown;

import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/resident_identity.dart';

void main() {
  final c = developedTown();
  for (var i = 0; i < 1800; i++) {
    c.update(.05);
    if (i % 100 == 0) {
      final market = c.places(Place.market).first.key;
      print(
        '${c.clock.toStringAsFixed(1)}: ${c.special?.kind.name} vendors=${c.workers(market)} customers=${c.occupancy(market)} trade=${c.tradeEarned} hall=${c.occupancy(c.places(Place.hall).first.key)}',
      );
    }
  }
  for (final r in c.people.values) {
    print(
      '${r.homeId} trait=${r.trait} work=${r.workId} state=${r.activity} dest=${r.destination} lastWork=${r.lastWork} lastSocial=${r.lastSocial} trade=${r.tradeDay} pref=${ResidentIdentity.hash(r.homeId, 379) % 2}',
    );
  }
}
