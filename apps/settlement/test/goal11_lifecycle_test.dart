import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/region.dart';
import 'package:roag_settlement/world_generation.dart';

Colony checkpoint() =>
    Colony.decode(File('test/fixtures/goal09-town.json').readAsStringSync());
SettlementRegion freeRegion() {
  final c = checkpoint();
  c.session.unlimited = true;
  return SettlementRegion.fromColony(c);
}

void main() {
  test('legacy migration retains the exact existing authoritative colony and money', () {
    final c = checkpoint(), before = c.encode();
    final r = SettlementRegion.fromColony(c);
    expect(r.capital.colony.encode(), before);
    expect(r.treasury, c.session.money);
    expect(r.capital.stock.values.every((v) => v == 0), isTrue);
    expect(r.towns.length, 1);
    expect(
      r.capital.stage.index,
      greaterThanOrEqualTo(DevelopmentStage.village.index),
    );
    final restored = SettlementRegion.decode(r.encode());
    expect(restored.capital.colony.encode(), before);
    expect(restored.encode(), r.encode());
  });
  test(
    'discovery and paid founding are reachable, explicit and idempotent',
    () {
      final c = checkpoint();
      // explicit financial fixture; production/milestone facts are from the real saved town.
      c.session.money = 200000;
      final r = SettlementRegion.fromColony(c);
      expect(r.discoveryIssue('site-01'), isNull);
      expect(r.discover('site-01'), isTrue);
      expect(r.discover('site-01'), isFalse);
      expect(c.session.money, 198000);
      expect(r.found('site-01', name: 'Alder Reach'), isTrue);
      expect(c.session.money, 138000);
      expect(r.towns['site-01']!.colony.session.money, 50000);
      expect(r.treasury, 188000);
      final after = r.encode();
      expect(r.found('site-01', name: 'Duplicate'), isFalse);
      expect(r.encode(), after);
      expect(
        r.towns['site-01']!.colony.world.generation!.preset,
        EnvironmentPreset.woodlands,
      );
      expect(r.towns['site-01']!.colony.clock, c.clock);
      expect(
        r.towns['site-01']!.colony.climate.calendar.toJson(),
        c.climate.calendar.toJson(),
      );
    },
  );
  test('an undeveloped town sees attainable requirements instead of founding for free', () {
    final c = Colony(
      LandscapeSession(WorldGeneration.create('202', EnvironmentPreset.plains)),
    );
    final r = SettlementRegion.fromColony(c), before = r.encode();
    expect(r.discover('site-01'), isFalse);
    expect(r.found('site-01'), isFalse);
    expect(r.encode(), before);
    expect(r.discoveryIssue('site-01'), contains('four people'));
  });
  test('three towns retain stable IDs and independently owned construction histories', () {
    final r = freeRegion(), original = r.capital.colony;
    expect(r.discover('site-01'), isTrue);
    expect(r.found('site-01'), isTrue);
    expect(r.discover('site-02'), isTrue);
    expect(r.found('site-02'), isTrue);
    final second = r.towns['site-01']!.colony, before = original.encode();
    expect(r.visit('site-01'), isTrue);
    second.session.beginStroke(Tool.cottage, const Cell(2, 14));
    second.session.stroke!.paintTo(const Cell(2, 14));
    expect(second.session.finishStroke(), isTrue);
    second.reconcile(second.session.world);
    final building = second.world.at(const Cell(2, 14)).cottageId;
    expect(second.session.canUndo, isTrue);
    expect(r.rename('site-01', 'Still Alder'), isTrue);
    expect(r.visit('site-00'), isTrue);
    expect(identical(r.active.colony, original), isTrue);
    expect(original.encode(), before);
    expect(r.visit('site-01'), isTrue);
    expect(second.world.at(const Cell(2, 14)).cottageId, building);
    expect(second.session.undo(), isTrue);
    expect(original.encode(), before);
    expect(r.towns.length, 3);
    expect(r.visit('unknown'), isFalse);
  });
  test('all towns share one timeline and switching adds no elapsed time', () {
    final r = freeRegion();
    r.discover('site-01');
    r.found('site-01');
    r.setSpeed(1);
    final before = r.clock;
    r.visit('site-01');
    r.visit('site-00');
    expect(r.clock, before);
    for (var i = 0; i < 400; i++) {
      r.update(.05);
    }
    expect(r.clock, closeTo(before + 20, 1e-8));
    for (final t in r.towns.values) {
      expect(t.colony.clock, r.clock);
    }
    final text = r.encode();
    expect(SettlementRegion.decode(text).encode(), text);
  });
  test('background operation earns exactly the standalone model outcome', () {
    final first = checkpoint()..speed = 1,
        other = Colony.decode(first.encode());
    final r = SettlementRegion.fromColony(other);
    for (var i = 0; i < 500; i++) {
      first.update(.05);
      r.update(.05);
    }
    expect(other.encode(), first.encode());
  });
  test('render rate and selected town do not affect regional simulation', () {
    SettlementRegion prepare() {
      final r = freeRegion();
      r.discover('site-01');
      r.found('site-01');
      r.setSpeed(1);
      return r;
    }

    final a = prepare(), b = prepare();
    b.visit('site-01');
    for (var i = 0; i < 600; i++) {
      a.update(1 / 30);
    }
    for (var i = 0; i < 2400; i++) {
      b.update(1 / 120);
    }
    for (final id in a.towns.keys) {
      expect(a.towns[id]!.colony.encode(), b.towns[id]!.colony.encode());
    }
    expect(a.clock, b.clock);
  });
  test('invalid regional imports cannot mutate the existing region', () {
    final r = freeRegion(), before = r.encode();
    Map<String, dynamic> data() => jsonDecode(before);
    var d = data();
    d['clock'] = r.clock + 1;
    expect(() => SettlementRegion.decode(jsonEncode(d)), throwsFormatException);
    d = data();
    d['towns'].add(d['towns'].first);
    expect(() => SettlementRegion.decode(jsonEncode(d)), throwsFormatException);
    d = data();
    d['towns'][0]['stock']['materials'] = -1;
    expect(() => SettlementRegion.decode(jsonEncode(d)), throwsFormatException);
    d = data();
    d['active'] = 'site-09';
    expect(() => SettlementRegion.decode(jsonEncode(d)), throwsFormatException);
    expect(r.encode(), before);
  });
}
