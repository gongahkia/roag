import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/model.dart';

void main() {
  late LandscapeSession session;
  setUp(() => session = LandscapeSession(Landscape.create()));
  void edit(Tool tool, int x, int y) {
    final plan = evaluate(session.world, tool, Cell(x, y));
    expect(plan.valid, isTrue, reason: plan.message);
    expect(session.commit(plan), isTrue);
  }

  test(
    'seeded ground, variants and guaranteed clear lane are reproducible',
    () {
      expect(session.world.encode(), Landscape.create(202).encode());
      expect(session.world.encode(), isNot(Landscape.create(203).encode()));
      expect(session.world.activePaths.length, 8);
      for (var x = 0; x < 20; x++) {
        for (var y = 11; y <= 19; y++) {
          expect(session.world.at(Cell(x, y)).scenery, Scenery.none);
        }
      }
    },
  );
  test('preview is pure; commit consumes exactly its evaluated world', () {
    final before = session.world.encode();
    final p = evaluate(session.world, Tool.cottage, const Cell(5, 14));
    expect(p.valid, isTrue);
    expect(p.access, const Cell(5, 15));
    expect(p.affected, containsAll([const Cell(5, 14), const Cell(5, 15)]));
    for (var i = 0; i < 40; i++) {
      evaluate(session.world, Tool.cottage, Cell(i % 20, 14));
    }
    expect(session.world.encode(), before);
    expect(session.commit(p), isTrue);
    expect(session.world.encode(), p.after!.encode());
    expect(
      session.commit(p),
      isFalse,
      reason: 'stale plan cannot replace a newer landscape',
    );
  });
  test('cardinal junctions include chosen access without conducting through cottage', () {
    edit(Tool.path, 5, 16);
    expect(session.world.roadMask(const Cell(5, 15)), 14);
    edit(Tool.cottage, 5, 14);
    expect(session.world.roadMask(const Cell(5, 15)), 15);
    expect(
      evaluate(session.world, Tool.path, const Cell(5, 13)).valid,
      isFalse,
    );
    expect(
      evaluate(session.world, Tool.cottage, const Cell(8, 14)).valid,
      isFalse,
      reason: 'diagonal is insufficient',
    );
    edit(Tool.path, 8, 15);
    expect(
      evaluate(session.world, Tool.cottage, const Cell(8, 14)).valid,
      isTrue,
    );
  });
  test('access preference is deterministic when several paths adjoin', () {
    edit(Tool.path, 4, 14);
    edit(Tool.path, 4, 13);
    edit(Tool.path, 5, 13);
    final p = evaluate(session.world, Tool.cottage, const Cell(5, 14));
    expect(p.access, const Cell(5, 15));
  });
  test('demolition leaves nonfunctional traces and reconnects quietly', () {
    edit(Tool.cottage, 7, 14);
    final removal = evaluate(session.world, Tool.remove, const Cell(6, 15));
    expect(removal.message, contains('1 cottage loses access'));
    expect(
      removal.affected,
      containsAll([const Cell(7, 15), const Cell(7, 14)]),
    );
    edit(Tool.remove, 6, 15);
    expect(session.world.at(const Cell(6, 15)).trace, Trace.scar);
    expect(session.world.access(const Cell(7, 14)), isNull);
    expect(session.world.at(const Cell(7, 15)).path, isTrue);
    expect(
      evaluate(session.world, Tool.path, const Cell(8, 15)).valid,
      isFalse,
    );
    edit(Tool.path, 6, 15);
    expect(session.world.access(const Cell(7, 14)), const Cell(7, 15));
    expect(session.world.at(const Cell(6, 15)).trace, Trace.none);
    edit(Tool.remove, 7, 14);
    expect(session.world.at(const Cell(7, 14)).trace, Trace.pad);
    expect(
      evaluate(session.world, Tool.path, const Cell(7, 13)).valid,
      isFalse,
    );
    edit(Tool.cottage, 7, 14);
  });
  test(
    'invalid actions, protected arrival and empty removal are exact no-ops',
    () {
      final before = session.world.encode();
      for (final (tool, c) in [
        (Tool.remove, session.world.arrival),
        (Tool.remove, const Cell(2, 14)),
        (Tool.path, const Cell(38, 28)),
        (Tool.cottage, const Cell(-1, 5)),
        (Tool.cottage, const Cell(3, 15)),
        (Tool.path, const Cell(80, 30)),
      ]) {
        final p = evaluate(session.world, tool, c);
        expect(p.valid, isFalse);
        expect(session.commit(p), isFalse);
        expect(session.world.encode(), before);
        expect(session.canUndo, isFalse);
      }
    },
  );
  test(
    'blocking trees, replaceable plants and exact undo/redo including variants',
    () {
      final a = const Cell(5, 14), b = const Cell(4, 14);
      session.replace(
        session.world
            .replace(a, const Lot(grass: 1, scenery: Scenery.vegetables))
            .replace(b, const Lot(scenery: Scenery.oak)),
      );
      final before = session.world.encode();
      expect(evaluate(session.world, Tool.cottage, b).valid, isFalse);
      expect(
        evaluate(session.world, Tool.cottage, a).message,
        contains('replaces plants'),
      );
      edit(Tool.cottage, 5, 14);
      final placed = session.world.encode();
      edit(Tool.remove, 5, 14);
      final removed = session.world.encode();
      expect(session.undo(), isTrue);
      expect(session.world.encode(), placed);
      expect(session.undo(), isTrue);
      expect(
        jsonDecode(session.world.encode())..remove('nextCottageId'),
        jsonDecode(before)..remove('nextCottageId'),
      );
      expect(session.world.nextCottageId, 2);
      expect(session.redo(), isTrue);
      expect(session.world.encode(), placed);
      expect(session.redo(), isTrue);
      expect(session.world.encode(), removed);
      session.undo();
      edit(Tool.cottage, 6, 14);
      expect(session.canRedo, isFalse);
    },
  );
  test('history is bounded and does not keep invalid actions', () {
    session.unlimited = true;
    for (var i = 0; i < 100; i++) {
      edit(Tool.cottage, 3, 14);
      edit(Tool.remove, 3, 14);
    }
    var count = 0;
    while (session.undo()) {
      count++;
    }
    expect(count, LandscapeSession.historyLimit);
  });
  test(
    'JSON reproduces terrain, history traces, connectivity and variants',
    () {
      edit(Tool.cottage, 7, 14);
      edit(Tool.cottage, 5, 16);
      edit(Tool.remove, 5, 16);
      edit(Tool.remove, 6, 15);
      final original = session.world;
      final copy = Landscape.decode(original.encode());
      expect(copy.encode(), original.encode());
      expect(copy.activePaths, original.activePaths);
      expect(copy.access(const Cell(7, 14)), isNull);
      expect(copy.at(const Cell(5, 16)).trace, Trace.pad);
    },
  );
  test('malformed/unsupported imports preserve the live scene and history', () {
    edit(Tool.cottage, 5, 14);
    final before = session.world.encode();
    final valid = jsonDecode(before) as Map<String, dynamic>;
    final invalid = <String>[
      '{',
      '[]',
      'null',
      jsonEncode({...valid, 'schema': 99}),
      jsonEncode({...valid, 'width': 400}),
      jsonEncode({...valid, 'height': 12.5}),
      jsonEncode({...valid, 'seed': -1}),
      jsonEncode({...valid, 'lots': []}),
    ];
    for (final id in ['grass9', 'bogus', 'scar', false, 'cottage7']) {
      final copy = jsonDecode(before) as Map<String, dynamic>;
      final row = (copy['lots'] as List)[0] as List;
      row[0] = id;
      invalid.add(jsonEncode(copy));
    }
    final noArrival = jsonDecode(before) as Map<String, dynamic>;
    (noArrival['lots'] as List)[15 * 40][3] = false;
    invalid.add(jsonEncode(noArrival));
    for (final text in invalid) {
      expect(() => session.importText(text), throwsFormatException);
      expect(session.world.encode(), before);
      expect(session.canUndo, isTrue);
    }
    session.importText(before);
    expect(session.world.encode(), before);
    expect(session.canUndo, isFalse);
  });
}
