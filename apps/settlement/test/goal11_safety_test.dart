import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/region.dart';

import 'goal11_trade_test.dart' show commerceFixture, runUntil;

void main() {
  test('forestry uses reachable local land, including legacy oak, never across water or rock', () {
    Landscape patch(Terrain barrier, {bool oak = false}) =>
        Landscape(202, 7, 3, [
          for (var y = 0; y < 3; y++)
            for (var x = 0; x < 7; x++)
              Lot(
                terrain: x == 2
                    ? barrier
                    : x == 3 && y == 1 && !oak
                    ? Terrain.forest
                    : Terrain.ground,
                scenery: x == 3 && y == 1 && oak ? Scenery.oak : Scenery.none,
              ),
        ]);
    expect(nearbyTimber(patch(Terrain.ground), const Cell(1, 1)), 1);
    expect(nearbyTimber(patch(Terrain.ground, oak: true), const Cell(1, 1)), 1);
    expect(nearbyTimber(patch(Terrain.water), const Cell(1, 1)), 0);
    expect(nearbyTimber(patch(Terrain.rock), const Cell(1, 1)), 0);
  });
  test(
    'premature and duplicate service callbacks cannot create a delivery',
    () {
      final r = commerceFixture();
      runUntil(
        r,
        () => r.shipments.values.any((s) => s.state == ShipmentState.transit),
      );
      final s = r.shipments.values.firstWhere(
        (s) => s.state == ShipmentState.transit,
      );
      final before = r.encode();
      r.towns[s.destination]!.colony.onFreightServed!(s.id, false);
      r.towns[s.origin]!.colony.onFreightServed!(s.id, true);
      expect(r.encode(), before);
      runUntil(r, () => s.state == ShipmentState.delivered);
      final after = r.encode();
      for (var i = 0; i < 3; i++) {
        r.towns[s.destination]!.colony.onFreightServed!(s.id, false);
      }
      expect(r.encode(), after);
    },
  );
  test(
    'regional cargo rejects forged quantities and inconsistent chronology',
    () {
      final r = commerceFixture();
      runUntil(
        r,
        () => r.shipments.values.any((s) => s.state == ShipmentState.transit),
      );
      for (final field in ['quantity', 'created']) {
        final data = jsonDecode(r.encode());
        final s = (data['trade']['shipments'] as List).firstWhere(
          (s) => s['state'] == 'transit',
        );
        s[field] = field == 'quantity' ? 999 : r.clock + 100;
        expect(
          () => SettlementRegion.decode(jsonEncode(data)),
          throwsFormatException,
        );
      }
    },
  );
  test(
    'profiling is observational and cannot change authoritative continuation',
    () {
      final a = commerceFixture(),
          b = SettlementRegion.decode(a.encode())..profile = true;
      for (var i = 0; i < 2000; i++) {
        a.update(.05);
        b.update(.05);
      }
      expect(b.encode(), a.encode());
      expect(b.activeModelUs, greaterThan(0));
      expect(b.backgroundModelUs, greaterThan(0));
    },
  );
}
