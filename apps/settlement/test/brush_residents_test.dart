import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/residents.dart';

void main() {
  late LandscapeSession s;
  setUp(() => s = LandscapeSession(Landscape.create()));
  void stroke(Tool t, Cell from, Cell to) {
    s.beginStroke(t, from);
    s.stroke!.paintTo(to);
    expect(s.finishStroke(), isTrue);
  }

  void edit(Tool t, int x, int y) => stroke(t, Cell(x, y), Cell(x, y));
  void advance(Residents r, double seconds) {
    for (var i = 0; i < (seconds * 60).ceil(); i++) {
      r.update(1 / 60);
    }
  }

  test('multi-cottage stroke stages immediately, saves committed data only, undo/redo exact IDs', () {
    final before = s.world.encode();
    s.beginStroke(Tool.cottage, const Cell(1, 14));
    s.stroke!.paintTo(const Cell(6, 14));
    expect(s.view.cottages.length, 6);
    expect(s.world.encode(), before);
    expect(s.snapshot().text, before);
    expect(s.canUndo, isFalse);
    expect(s.finishStroke(), isTrue);
    final after = s.world.encode();
    expect(s.world.cottages.keys.toSet().length, 6);
    expect(s.undo(), isTrue);
    expect(s.world.encode(), before);
    expect(s.canUndo, isFalse);
    expect(s.redo(), isTrue);
    expect(s.world.encode(), after);
    expect(Landscape.decode(after).encode(), after);
  });
  test(
    'fast diagonal uses deterministic cardinal crossings, with X first at ties',
    () {
      final cells = cardinalStroke(
        const Cell(7, 15),
        const Cell(10, 12),
      ).toList();
      expect(cells, [
        const Cell(7, 15),
        const Cell(8, 15),
        const Cell(8, 14),
        const Cell(9, 14),
        const Cell(9, 13),
        const Cell(10, 13),
        const Cell(10, 12),
      ]);
      expect(cardinalStroke(const Cell(2, 2), const Cell(0, 0)), [
        const Cell(2, 2),
        const Cell(1, 2),
        const Cell(1, 1),
        const Cell(0, 1),
        const Cell(0, 0),
      ]);
      for (var i = 1; i < cells.length; i++) {
        expect(
          (cells[i].x - cells[i - 1].x).abs() +
              (cells[i].y - cells[i - 1].y).abs(),
          1,
        );
      }
      stroke(Tool.path, cells.first, cells.last);
      expect(cells.every((c) => s.world.at(c).path), isTrue);
    },
  );
  test('obstacles block connectivity beyond pointer route; brush never routes around them', () {
    s.replace(
      s.world.replace(const Cell(10, 15), const Lot(scenery: Scenery.oak)),
    );
    stroke(Tool.path, const Cell(7, 15), const Cell(14, 15));
    expect(s.world.at(const Cell(8, 15)).path, isTrue);
    expect(s.world.at(const Cell(9, 15)).path, isTrue);
    expect(s.world.at(const Cell(10, 15)).scenery, Scenery.oak);
    for (var x = 10; x <= 14; x++) {
      expect(s.world.at(Cell(x, 15)).path, isFalse);
    }
    expect(s.world.at(const Cell(10, 14)).path, isFalse);
  });
  test('only successes deduplicate; previously invalid anchor can become valid later', () {
    s.beginStroke(Tool.path, const Cell(9, 14));
    expect(s.stroke!.changed, isEmpty);
    s.stroke!.paintTo(const Cell(7, 14));
    s.stroke!.paintTo(const Cell(9, 14));
    expect(s.stroke!.changed.length, 3);
    expect(s.finishStroke(), isTrue);
    expect(s.world.at(const Cell(9, 14)).path, isTrue);
  });
  test('mixed cottage candidates, repeated visits and removal respect logical footprints', () {
    stroke(Tool.cottage, const Cell(0, 14), const Cell(12, 14));
    expect(s.world.cottages.length, 8);
    final before = s.world.encode();
    s.beginStroke(Tool.remove, const Cell(1, 14));
    s.stroke!.paintTo(const Cell(5, 14));
    s.stroke!.paintTo(const Cell(1, 14));
    expect(s.stroke!.changed.length, 5);
    expect(s.world.cottages.length, 8);
    s.finishStroke();
    expect(s.world.cottages.length, 3);
    expect(s.world.at(const Cell(3, 14)).trace, Trace.pad);
    expect(s.world.at(const Cell(3, 15)).path, isTrue);
    s.undo();
    expect(s.world.encode(), before);
    stroke(Tool.remove, const Cell(0, 15), const Cell(4, 15));
    expect(s.world.at(s.world.arrival).path, isTrue);
    expect(s.world.at(const Cell(3, 15)).trace, Trace.scar);
    expect(s.world.at(const Cell(3, 14)).cottage, isNotNull);
  });
  test('no-op strokes create no history; cancel and undo-during-stroke preserve exact state', () {
    s.beginStroke(Tool.path, const Cell(0, 15));
    s.stroke!.paintTo(const Cell(7, 15));
    expect(s.finishStroke(), isFalse);
    expect(s.canUndo, isFalse);
    s.beginStroke(Tool.remove, const Cell(1, 13));
    expect(s.finishStroke(), isFalse);
    edit(Tool.cottage, 3, 14);
    final before = s.world.encode();
    s.beginStroke(Tool.path, const Cell(8, 15));
    s.stroke!.paintTo(const Cell(15, 15));
    s.cancelStroke();
    expect(s.world.encode(), before);
    s.beginStroke(Tool.cottage, const Cell(4, 14));
    expect(s.undo(), isFalse);
    expect(s.stroke, isNull);
    expect(s.world.encode(), before);
    expect(s.undo(), isTrue);
    expect(s.world.cottages, isEmpty);
  });
  test('committed snapshots reject stale deferred writers after commit, undo or import', () {
    final writes = <String>[];
    final old = s.snapshot();
    s.beginStroke(Tool.cottage, const Cell(2, 14));
    s.stroke!.paintTo(const Cell(4, 14));
    expect(s.snapshot().text, old.text);
    s.finishStroke();
    expect(s.persist(old, writes.add), isFalse);
    final latest = s.snapshot();
    expect(s.persist(latest, writes.add), isTrue);
    expect(
      jsonDecode(writes.single)['lots'].where((l) => l[4] != null).length,
      3,
    );
    s.undo();
    expect(s.persist(latest, writes.add), isFalse);
    s.importText(latest.text);
    expect(s.persist(latest, writes.add), isFalse);
    expect(s.persist(s.snapshot(), writes.add), isTrue);
    expect(writes.last, latest.text);
  });
  test('real schema-one scene migrates without changing terrain, traces or variants', () {
    final text = File(
      '../../docs/world-builder/evidence/tranche-01/fuller-landscape.json',
    ).readAsStringSync();
    final old = jsonDecode(text);
    expect(old['schema'], 1);
    final migrated = Landscape.decode(text);
    final encoded = jsonDecode(migrated.encode());
    expect(encoded['schema'], 2);
    expect(
      encoded['lots'].map((l) => l.take(5).toList()).toList(),
      old['lots'],
    );
    expect(migrated.cottages.length, 22);
    expect(Landscape.decode(migrated.encode()).encode(), migrated.encode());
    final before = migrated.cottages.keys.toSet();
    s.replace(migrated);
    edit(Tool.cottage, 1, 14);
    expect(s.world.cottages.keys.toSet().containsAll(before), isTrue);
  });
  test('invalid duplicate or orphan identities preserve the current scene', () {
    stroke(Tool.cottage, const Cell(1, 14), const Cell(3, 14));
    final before = s.world.encode();
    final data = jsonDecode(before);
    data['lots'][14 * 40 + 2][5] = data['lots'][14 * 40 + 1][5];
    expect(() => s.importText(jsonEncode(data)), throwsFormatException);
    expect(s.world.encode(), before);
    data['lots'][14 * 40 + 2][5] = 'home-999999';
    expect(() => s.importText(jsonEncode(data)), throwsFormatException);
    expect(s.world.encode(), before);
  });

  test('residents appear only after commit, stagger arrivals and reach their own access', () {
    final r = Residents(s.world);
    s.beginStroke(Tool.cottage, const Cell(1, 14));
    s.stroke!.paintTo(const Cell(4, 14));
    advance(r, 1);
    expect(r.people, isEmpty);
    s.finishStroke();
    r.reconcile(s.world);
    expect(r.people.length, 4);
    expect(r.people.values.map((p) => p.timer).toSet().length, 4);
    final seen = <String>{};
    for (var i = 0; i < 300; i++) {
      r.update(1 / 60);
      for (final p in r.people.values) {
        if (p.flash > 0) {
          seen.add(p.homeId);
          expect(p.cell, s.world.access(s.world.cottages[p.homeId]!));
        }
        for (final c in p.route) {
          expect(s.world.at(c).path, isTrue);
          expect(s.world.at(c).cottage, isNull);
        }
      }
    }
    expect(seen.length, 4);
    expect(r.people.values.every((p) => p.arrived), isTrue);
    final first = r.people.values.first;
    edit(Tool.path, 8, 15);
    r.reconcile(s.world);
    expect(identical(first, r.people[first.homeId]), isTrue);
    expect(r.visible.map((e) => e.$1.homeId).toSet().length, r.visible.length);
  });
  test('residents visit actual reachable homes and single residents take local walks', () {
    stroke(Tool.cottage, const Cell(1, 14), const Cell(5, 14));
    final r = Residents(s.world);
    final seen = <String>{};
    final before = s.world.encode();
    for (var i = 0; i < 1200; i++) {
      r.update(1 / 60);
      for (final p in r.people.values) {
        if (p.activity == Activity.visiting && p.destination != null) {
          expect(s.world.cottages.containsKey(p.destination), isTrue);
          expect(p.destination, isNot(p.homeId));
          seen.add(p.homeId);
        }
        expect(p.route.every((c) => s.world.at(c).path), isTrue);
      }
    }
    expect(seen, isNotEmpty);
    expect(s.world.encode(), before);
    s.replace(Landscape.create());
    edit(Tool.cottage, 3, 14);
    r.reconcile(s.world, restored: true, reset: true);
    var walked = false;
    for (var i = 0; i < 600; i++) {
      r.update(1 / 60);
      walked |= r.people.values.single.activity == Activity.visiting;
    }
    expect(walked, isTrue);
  });
  test('removed underfoot paths cause bounded local settling and waiting, then reconnect', () {
    edit(Tool.cottage, 7, 14);
    final r = Residents(Landscape.create());
    r.reconcile(s.world);
    final p = r.people.values.single;
    while (p.cell.x < 3) {
      r.update(1 / 60);
    }
    advance(r, .08);
    final x = p.x, y = p.y;
    stroke(Tool.remove, const Cell(2, 15), const Cell(5, 15));
    r.reconcile(s.world);
    expect(p.activity, Activity.waiting);
    expect(p.route, isEmpty);
    advance(r, 3);
    expect(p.x, x);
    expect(p.y, y);
    stroke(Tool.path, const Cell(1, 15), const Cell(7, 15));
    r.reconcile(s.world);
    expect(r.people.length, 1);
    for (var i = 0; i < 240; i++) {
      final ox = p.x, oy = p.y;
      r.update(1 / 60);
      expect((p.x - ox).abs() + (p.y - oy).abs(), lessThan(.11));
    }
    expect(p.arrived, isTrue);
    expect(p.activity, isNot(Activity.waiting));
  });
  test('cut route reroutes through an existing legal loop; disconnected homes stop outings', () {
    edit(Tool.cottage, 7, 14);
    stroke(Tool.path, const Cell(3, 16), const Cell(5, 16));
    final r = Residents(Landscape.create());
    r.reconcile(s.world);
    final p = r.people.values.single;
    while (p.cell.x < 3) {
      r.update(1 / 60);
    }
    edit(Tool.remove, 4, 15);
    r.reconcile(s.world);
    var usedLoop = false;
    for (var i = 0; i < 300; i++) {
      r.update(1 / 60);
      usedLoop |= p.cell.y == 16;
    }
    expect(usedLoop, isTrue);
    expect(p.arrived, isTrue);
    edit(Tool.remove, 3, 15);
    edit(Tool.remove, 3, 16);
    r.reconcile(s.world);
    advance(r, 3);
    expect(s.world.access(const Cell(7, 14)), isNull);
    var outings = false;
    for (var i = 0; i < 500; i++) {
      r.update(1 / 60);
      outings |= p.activity == Activity.visiting;
    }
    expect(outings, isFalse);
  });
  test('pending arrivals and visits cannot outlive a cottage; history/load reconcile without duplicates', () {
    stroke(Tool.cottage, const Cell(1, 14), const Cell(7, 14));
    final r = Residents(Landscape.create());
    r.reconcile(s.world);
    final removed = s.world.cottageKey(const Cell(7, 14));
    expect(r.people[removed]!.activity, Activity.pending);
    edit(Tool.remove, 7, 14);
    r.reconcile(s.world);
    advance(r, 2);
    expect(r.people.containsKey(removed), isFalse);
    s.undo();
    r.reconcile(s.world, restored: true);
    expect(r.people[removed]!.arrived, isTrue);
    expect(r.visible.map((e) => e.$1.homeId).toSet().length, r.visible.length);
    s.redo();
    r.reconcile(s.world, restored: true);
    advance(r, .4);
    expect(r.people.containsKey(removed), isFalse);
    final saved = s.world.encode();
    s.importText(saved);
    r.reconcile(s.world, restored: true, reset: true);
    expect(r.people.keys.toSet(), s.world.cottages.keys.toSet());
    Resident? visitor;
    for (var i = 0; i < 1000 && visitor == null; i++) {
      r.update(1 / 60);
      for (final p in r.people.values) {
        if (p.activity == Activity.visiting && p.destination != null) {
          visitor = p;
          break;
        }
      }
    }
    expect(visitor, isNotNull);
    final destination = visitor!.destination!;
    final home = s.world.cottages[destination]!;
    edit(Tool.remove, home.x, home.y);
    r.reconcile(s.world);
    expect(visitor.destination, isNull);
    expect(r.people.containsKey(destination), isFalse);
    advance(r, 5);
    expect(r.people.keys.toSet(), s.world.cottages.keys.toSet());
  });
  test('background pause cannot catch up queued residents or rewrite authored state', () {
    stroke(Tool.cottage, const Cell(1, 14), const Cell(7, 14));
    final r = Residents(Landscape.create());
    r.reconcile(s.world);
    final before = s.world.encode();
    r.update(10000);
    expect(r.people.values.where((p) => p.visible).length, 1);
    expect(r.people.values.first.x, lessThan(.8));
    expect(s.world.encode(), before);
    final searches = r.routeSearches;
    for (var i = 0; i < 10; i++) {
      r.update(.001);
    }
    expect(
      r.routeSearches,
      searches,
      reason: 'routing is not recalculated each frame',
    );
  });
}
