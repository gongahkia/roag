import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/region.dart';

import '../tool/goal08_fixture.dart' show add;
import 'goal11_trade_test.dart' show commerceFixture, runUntil;

SettlementRegion threeTowns() {
  final r = commerceFixture();
  r.discover('site-02');
  r.found('site-02');
  final c = r.towns['site-02']!.colony;
  for (var x = 2; x <= 5; x++) {
    add(c, Tool.cottage, Cell(x, 14));
  }
  add(c, Tool.garden, const Cell(2, 16));
  add(c, Tool.market, const Cell(4, 16));
  add(c, Tool.hall, const Cell(6, 16));
  c.nextIncidentAt = 1e9;
  c.nextSpecialAt = 1e9;
  r.connectTrade('site-00', 'site-02');
  return r;
}

void main() {
  test('genuine three-town trade completes the Network once and Undo leaves its state forward', () {
    final r = threeTowns();
    runUntil(
      r,
      () => r.projectIssue('network', 'site-00') == null,
      limit: 1600,
    );
    final before =
        r.capital.stock[TradeGood.materials]! +
        r.capital.supplied[TradeGood.materials]!;
    final clock = r.clock;
    add(r.capital.colony, Tool.path, const Cell(9, 16));
    expect(r.completeProject('network', 'site-00'), isTrue);
    expect(r.goodsAvailable(r.capital, TradeGood.materials), before - 4);
    final done = r.encode();
    expect(r.completeProject('network', 'site-00'), isFalse);
    expect(r.encode(), done);
    expect(r.capital.colony.session.undo(), isTrue);
    r.capital.colony.reconcile(r.capital.colony.session.world, restored: true);
    expect(r.clock, clock);
    expect(r.projects, contains('network'));
    expect(r.goodsAvailable(r.capital, TradeGood.materials), before - 4);
    expect(r.capital.colony.regionalMood!(), greaterThanOrEqualTo(2));
    expect(SettlementRegion.decode(r.encode()).encode(), r.encode());
  });
  test('Monument spends actual goods and coins, persists, and disappears with its Hall without a project refund', () {
    final r = threeTowns(), c = r.capital.colony;
    add(c, Tool.hall, const Cell(9, 14));
    r.setFreeBuild(false);
    runUntil(
      r,
      () => r.projectIssue('landmark', 'site-00') == null,
      limit: 1600,
    );
    final balance = c.session.money,
        materials = r.goodsAvailable(r.capital, TradeGood.materials);
    expect(r.completeProject('landmark', 'site-00'), isTrue);
    expect(c.session.money, balance - 40000);
    expect(r.capital.projectsSpent, 40000);
    expect(r.goodsAvailable(r.capital, TradeGood.materials), materials - 6);
    expect(r.landmarkStanding(r.capital), isTrue);
    expect(SettlementRegion.decode(r.encode()).encode(), r.encode());
    expect(c.session.undo(), isTrue);
    c.reconcile(c.session.world, restored: true);
    expect(r.landmarkStanding(r.capital), isFalse);
    expect(r.projects, contains('landmark'));
    expect(r.capital.projectsSpent, 40000);
    expect(c.session.money, balance - 40000);
    expect(r.completeProject('landmark', 'site-00'), isFalse);
  });
  test('Resilience recognises real trained, connected infrastructure instead of bare building counts', () {
    final r = commerceFixture(connect: false), c = r.towns['site-01']!.colony;
    add(c, Tool.watchpost, const Cell(4, 16));
    add(c, Tool.refuge, const Cell(2, 16));
    expect(r.projectIssue('resilient', 'site-00'), isNotNull);
    runUntil(
      r,
      () => r.projectIssue('resilient', 'site-00') == null,
      limit: 100,
    );
    expect(r.completeProject('resilient', 'site-00'), isTrue);
    expect(r.projects, contains('resilient'));
    final before = r.encode();
    expect(r.completeProject('resilient', 'site-00'), isFalse);
    expect(r.encode(), before);
  });
  test('future-region discovery requires a real completed route, and Free Build remains available', () {
    final r = commerceFixture();
    r.setFreeBuild(false);
    expect(r.discoveryIssue('site-03'), contains('delivery'));
    runUntil(r, () => r.tradeRoutes.values.single.deliveries > 0);
    expect(r.discoveryIssue('site-03'), isNull);
    expect(r.discover('site-03'), isTrue);
    final before = r.encode();
    expect(r.discover('site-03'), isFalse);
    expect(r.encode(), before);
  });
  test('stale regional snapshots cannot overwrite newer clock, stock or delivery state', () {
    final r = commerceFixture(), snapshot = r.snapshot();
    r.update(.05);
    String? written;
    expect(r.persist(snapshot, (s) => written = s), isFalse);
    expect(written, isNull);
    final current = r.snapshot();
    expect(r.persist(current, (s) => written = s), isTrue);
    expect(written, current.text);
  });
}
