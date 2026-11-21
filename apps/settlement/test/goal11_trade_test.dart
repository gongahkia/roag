import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/region.dart';

import '../tool/goal08_fixture.dart' show add, paint;

SettlementRegion commerceFixture({bool connect = true}) {
  final c = Colony.decode(
    File('test/fixtures/goal09-town.json').readAsStringSync(),
  )..incidentMode = IncidentMode.peaceful;
  c.session.unlimited = true;
  final r = SettlementRegion.fromColony(c);
  r.discover('site-01');
  r.found('site-01');
  final second = r.towns['site-01']!.colony;
  second.incidentMode = IncidentMode.peaceful;
  var w = second.world;
  // explicit resource fixture, before construction; work and transport remain real.
  w = w.replace(
    const Cell(2, 10),
    const Lot(terrain: Terrain.forest, scenery: Scenery.oak),
  );
  second.session.world = w;
  second.reconcile(w);
  for (var y = 14; y >= 12; y--) {
    if (!second.world.at(Cell(1, y)).path) add(second, Tool.path, Cell(1, y));
  }
  for (var x = 2; x <= 5; x++) {
    add(second, Tool.cottage, Cell(x, 14));
  }
  add(second, Tool.market, const Cell(6, 16));
  add(second, Tool.lumberCamp, const Cell(2, 12));
  second.nextVisitorAt = 1e9;
  second.nextSpecialAt = 1e9;
  if (connect) r.connectTrade('site-00', 'site-01');
  r.setSpeed(1);
  return r;
}

void runUntil(SettlementRegion r, bool Function() done, {double limit = 700}) {
  for (var i = 0; i < (limit / .05).round() && !done(); i++) {
    r.update(.05);
  }
  expect(
    done(),
    isTrue,
    reason:
        'condition did not occur before ${r.clock}: ${r.shipments.values.map((s) => s.toJson()).toList()}',
  );
}

int cash(SettlementRegion r) =>
    r.treasury +
    r.shipments.values
        .where((s) => !s.terminal)
        .fold<int>(0, (n, s) => n + s.price);
int minted(SettlementRegion r) => r.towns.values.fold<int>(0, (n, t) {
  final c = t.colony;
  return n +
      c.workEarned +
      c.grantEarned +
      c.tradeEarned +
      c.visitorEarned -
      c.civicSpent -
      c.visitorSpent -
      c.incidentSpent;
});
void main() {
  test('goods require actual work; Materials require maintained woodland and access', () {
    final r = commerceFixture(connect: false),
        c = r.towns['site-01']!.colony,
        town = r.towns['site-01']!;
    expect(r.capital.stock.values.every((v) => v == 0), isTrue);
    runUntil(r, () => town.produced[TradeGood.materials]! >= 2);
    expect(r.capital.produced[TradeGood.provisions], greaterThan(0));
    expect(r.capital.produced[TradeGood.wares], greaterThan(0));
    final camp = c.places(Place.lumberCamp).single;
    expect(c.session.condition(camp.key).productive, greaterThan(0));
    expect(c.workers(camp.key), lessThanOrEqualTo(1));
    final trees = c.world.cells
        .where(
          (p) =>
              (p.x - camp.value.x).abs() + (p.y - camp.value.y).abs() <= 3 &&
              c.world.at(p).terrain == Terrain.forest,
        )
        .toList();
    for (final p in trees) {
      expect(paint(c, Tool.clear, p), isTrue);
    }
    expect(c.workSourceAvailable(camp.key), isFalse);
    final before = town.produced[TradeGood.materials];
    for (var i = 0; i < 1000; i++) {
      r.update(.05);
    }
    expect(town.produced[TradeGood.materials], before);
    add(c, Tool.plant, const Cell(2, 10));
    runUntil(r, () => town.produced[TradeGood.materials]! > before!);
  });
  test('automatic trade reserves real goods, pays once at actual arrival and is zero-sum', () {
    final r = commerceFixture(), initialCash = cash(r), initialMint = minted(r);
    runUntil(r, () => r.tradeRoutes.values.single.deliveries >= 3);
    final delivered = r.shipments.values
        .where((s) => s.state == ShipmentState.delivered)
        .toList();
    expect(delivered.length, greaterThanOrEqualTo(3));
    expect(
      delivered.every(
        (s) =>
            s.collected && s.finished > s.departed && s.departed >= s.created,
      ),
      isTrue,
    );
    expect(delivered.map((s) => s.origin).toSet().length, 2);
    expect(delivered.any((s) => s.good == TradeGood.materials), isTrue);
    expect(cash(r) - initialCash, minted(r) - initialMint);
    expect(
      r.capital.exportsEarned + r.towns['site-01']!.exportsEarned,
      r.tradeRoutes.values.single.value,
    );
    final text = r.encode();
    expect(SettlementRegion.decode(text).encode(), text);
  });
  test('transit save/reload continues the same cargo and outcome without extra payments', () {
    final r = commerceFixture();
    runUntil(
      r,
      () => r.shipments.values.any((s) => s.state == ShipmentState.transit),
    );
    final id = r.shipments.values
        .firstWhere((s) => s.state == ShipmentState.transit)
        .id;
    final before = r.encode(), restored = SettlementRegion.decode(before);
    expect(restored.encode(), before);
    for (var i = 0; i < 4000; i++) {
      r.update(.05);
      restored.update(.05);
    }
    expect(restored.encode(), r.encode());
    expect(r.shipments[id]!.state, ShipmentState.delivered);
  });
  test('cancelling an uncollected reservation restores escrow and goods exactly once', () {
    final r = commerceFixture();
    runUntil(
      r,
      () => r.shipments.values.any(
        (s) => s.state == ShipmentState.loading && !s.collected,
      ),
    );
    final s = r.shipments.values.firstWhere((s) => !s.terminal),
        source = r.towns[s.origin]!,
        buyer = r.towns[s.destination]!;
    final goods = source.stock[s.good]!, money = buyer.colony.session.money;
    r.enableTrade(s.route, false);
    expect(s.state, ShipmentState.cancelled);
    expect(source.stock[s.good], goods + s.quantity);
    expect(buyer.colony.session.money, money + s.price);
    final after = r.encode();
    r.enableTrade(s.route, false);
    expect(r.encode(), after);
    expect(SettlementRegion.decode(after).encode(), after);
  });
  test('Peaceful access interruption causes a real return journey, not a fictitious delivery', () {
    final r = commerceFixture();
    runUntil(
      r,
      () => r.shipments.values.any((s) => s.state == ShipmentState.unloading),
    );
    final s = r.shipments.values.firstWhere(
          (s) => s.state == ShipmentState.unloading,
        ),
        dest = r.towns[s.destination]!.colony;
    final paid = r.towns[s.origin]!.exportsEarned;
    add(dest, Tool.remove, const Cell(1, 15));
    runUntil(r, () => s.state == ShipmentState.returned, limit: 800);
    expect(s.returning, isTrue);
    expect(r.towns[s.origin]!.exportsEarned, paid);
    expect(r.tradeRoutes[s.route]!.deliveries, 0);
    expect(s.finished, greaterThan(s.departed));
    expect(SettlementRegion.decode(r.encode()).encode(), r.encode());
  });
  test('Standard prolonged obstruction loses cargo and refunds escrow without paying the exporter', () {
    final r = commerceFixture();
    runUntil(
      r,
      () => r.shipments.values.any((s) => s.state == ShipmentState.unloading),
    );
    final s = r.shipments.values.firstWhere(
          (s) => s.state == ShipmentState.unloading,
        ),
        dest = r.towns[s.destination]!.colony;
    final paid = r.towns[s.origin]!.exportsEarned;
    dest.incidentMode = IncidentMode.standard;
    dest.nextIncidentAt = 1e9;
    runUntil(
      r,
      () => dest.visits.values.any(
        (v) => v.freight == s.id && v.actor.cell.x >= 2,
      ),
    );
    add(dest, Tool.remove, const Cell(1, 15));
    runUntil(r, () => s.state == ShipmentState.lost, limit: 200);
    expect(r.towns[s.origin]!.exportsEarned, paid);
    expect(r.tradeRoutes[s.route]!.deliveries, 0);
    expect(s.reason, contains('refunded'));
    final restored = SettlementRegion.decode(r.encode());
    expect(restored.encode(), r.encode());
    for (var i = 0; i < 200; i++) {
      r.update(.05);
      restored.update(.05);
    }
    expect(restored.encode(), r.encode());
  });
}
