import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/navigation.dart';
import 'package:roag_settlement/world_generation.dart';
import 'package:roag_settlement/world_creation.dart';
import 'package:roag_settlement/settlement_game.dart';
import 'package:material_ui/material_ui.dart';

import 'colony_test.dart' show paint, advance;

Landscape generated([
  EnvironmentPreset p = EnvironmentPreset.river,
  String seed = '202',
]) => WorldGeneration.create(seed, p);
List<Cell> riverSpan(Landscape w, [int y = 15]) {
  final water = w.cells
      .where((c) => c.y == y && w.at(c).terrain == Terrain.water)
      .toList();
  return [Cell(water.first.x - 1, y), Cell(water.last.x + 1, y)];
}

Colony crossingTown() {
  final c = Colony(LandscapeSession(generated()))..nextSpecialAt = 1e9;
  final span = riverSpan(c.world);
  paint(c, Tool.path, const Cell(7, 15), span.first);
  paint(c, Tool.bridge, span.first, span.last);
  paint(c, Tool.path, span.last, Cell(span.last.x + 2, 15));
  paint(c, Tool.cottage, const Cell(2, 14), const Cell(5, 14));
  paint(c, Tool.garden, Cell(span.last.x + 1, 14));
  paint(c, Tool.green, const Cell(7, 14));
  return c;
}

void main() {
  test('seed semantics are stable for signed decimals, text and UTF-8', () {
    expect(seedNumber(' +000202 '), 202);
    expect(canonicalSeed(' +000202 '), '202');
    expect(seedNumber('-1'), 2147483646);
    expect(seedNumber('2147483849'), 202);
    expect(
      seedNumber('river town'),
      538208645,
    ); // frozen independently calculated base-131 value
    expect(seedNumber('森林'), isNot(seedNumber('forest')));
    expect(() => seedNumber(' \n'), throwsFormatException);
  });
  test('600 seeds per preset reproduce, vary, retain arrival/early plots and affordable river access', () {
    for (final p in EnvironmentPreset.values) {
      final fingerprints = <String>{};
      for (var seed = 0; seed < 600; seed++) {
        final w = WorldGeneration.create('$seed', p);
        expect(w.encode(), WorldGeneration.create('$seed', p).encode());
        fingerprints.add(w.lots.map((l) => l.terrain.index).join(','));
        expect(w.activePaths.length, 8);
        for (var y = 11; y <= 19; y++) {
          for (var x = 0; x <= 10; x++) {
            expect(w.at(Cell(x, y)).terrain, Terrain.ground);
          }
        }
        expect(evaluate(w, Tool.cottage, const Cell(2, 14)).valid, isTrue);
        expect(
          w.lots.where((l) => l.terrain == Terrain.ground).length,
          greaterThan(250),
        );
        expect(
          w.lots.where((l) => l.terrain == Terrain.forest).length,
          greaterThan(70),
        );
        if (p == EnvironmentPreset.river) {
          // continuous channel: each row touches the next, without isolated water dots.
          for (var y = 0; y < 29; y++) {
            final row = w.cells
                .where((c) => c.y == y && w.at(c).terrain == Terrain.water)
                .toList();
            expect(row.length, inInclusiveRange(2, 3));
            expect(
              row.any((c) => w.at(Cell(c.x, y + 1)).terrain == Terrain.water),
              isTrue,
            );
          }
          final span = riverSpan(w), s = LandscapeSession(w);
          s.beginStroke(Tool.path, const Cell(7, 15));
          s.stroke!.paintTo(span.first);
          expect(s.finishStroke(), isTrue);
          final bridge = evaluateBridge(s.world, span.first, span.last);
          expect(bridge.valid, isTrue);
          expect(bridge.cost, lessThanOrEqualTo(9000));
          expect(bridge.after!.activePaths.contains(span.last), isTrue);
          expect(
            bridge.after!.at(Cell(span.last.x + 1, 14)).terrain,
            Terrain.ground,
          );
        }
      }
      expect(fingerprints.length, 600);
    }
  });
  test('generation and pure inspection cannot consume later event streams', () {
    final c = Colony(LandscapeSession(generated())), before = c.encode();
    for (var i = 0; i < 30; i++) {
      WorldGeneration.create('other $i', EnvironmentPreset.woodlands);
      evaluate(c.world, Tool.clear, const Cell(11, 10));
      c.world.activePaths;
    }
    expect(c.encode(), before);
    final clone = Colony.decode(before);
    advance(c, 10);
    advance(clone, 10);
    expect(c.encode(), clone.encode());
  });
  test('terrain placement matrix, clearing, planting and relocation use authoritative rules', () {
    var w = Landscape.create();
    for (final t in Terrain.values) {
      final c = const Cell(4, 14);
      w = w.replace(
        c,
        Lot(
          terrain: t,
          scenery: t == Terrain.forest ? Scenery.oak : Scenery.none,
        ),
      );
      expect(evaluate(w, Tool.cottage, c).valid, t == Terrain.ground);
      expect(evaluate(w, Tool.path, c).valid, t.roadAllowed);
      expect(
        evaluate(w, Tool.clear, c).valid,
        t == Terrain.forest || t == Terrain.marsh,
      );
      expect(evaluate(w, Tool.plant, c).valid, t == Terrain.ground);
    }
    final s = LandscapeSession(generated(EnvironmentPreset.woodlands));
    final tree = s.world.cells.firstWhere(
          (c) => s.world.at(c).terrain == Terrain.forest,
        ),
        old = s.world.encode();
    expect(s.commit(evaluate(s.world, Tool.clear, tree)), isTrue);
    expect(s.money, 199000);
    expect(s.world.at(tree).terrain, Terrain.ground);
    expect(s.world.at(tree).trace, Trace.scar);
    expect(s.commit(evaluate(s.world, Tool.plant, tree)), isTrue);
    expect(s.money, 198000);
    expect(s.world.at(tree).terrain, Terrain.forest);
    expect(s.world.at(tree).trace, Trace.scar);
    expect(s.undo(), isTrue);
    expect(s.undo(), isTrue);
    expect(s.world.encode(), old);
    expect(s.money, 200000);
    expect(s.redo(), isTrue);
    expect(s.redo(), isTrue);
    expect(s.money, 198000);
  });
  test('landscape strokes dedupe, capture a budget, cancel and preserve forward income on undo', () {
    final w = generated(EnvironmentPreset.woodlands),
        s = LandscapeSession(w, money: 1000);
    final cells = w.cells
        .where((c) => w.at(c).terrain == Terrain.forest)
        .take(2)
        .toList();
    s.beginStroke(Tool.clear, cells.first);
    s.stroke!.paintTo(cells.first);
    s.stroke!.paintTo(cells.last);
    expect(s.stroke!.price, 1000);
    expect(s.stroke!.changed.length, 1);
    s.money += 4000;
    expect(s.finishStroke(), isTrue);
    expect(s.money, 4000);
    expect(s.undo(), isTrue);
    expect(s.money, 5000);
    expect(s.world.encode(), w.encode());
    s.beginStroke(Tool.plant, const Cell(2, 12));
    s.cancelStroke();
    expect(s.world.encode(), w.encode());
    expect(s.money, 5000);
  });
  test('bridge strokes are atomic, paid once, cancel safely and repair the real physical gap', () {
    final w = generated(), span = riverSpan(w), s = LandscapeSession(w);
    s.beginStroke(Tool.path, const Cell(7, 15));
    s.stroke!.paintTo(span.first);
    expect(s.finishStroke(), isTrue);
    final before = s.world.encode();
    s.beginStroke(Tool.bridge, span.first);
    s.stroke!.paintTo(Cell(span.last.x - 1, 15));
    expect(s.stroke!.changed, isEmpty);
    expect(s.finishStroke(), isFalse);
    expect(s.world.encode(), before);
    s.beginStroke(Tool.bridge, span.first);
    s.stroke!.paintTo(span.last);
    expect(s.stroke!.price, 9000);
    expect(s.world.encode(), before);
    s.cancelStroke();
    expect(s.money, 200000);
    s.beginStroke(Tool.bridge, span.first);
    s.stroke!.paintTo(span.last);
    expect(s.finishStroke(), isTrue);
    expect(s.money, 191000);
    expect(s.world.pathRoute(s.world.arrival, span.last), isNotNull);
    final bridge = s.world.encode();
    expect(
      s.commit(evaluate(s.world, Tool.remove, Cell(span.first.x + 2, 15))),
      isTrue,
    );
    expect(s.world.pathRoute(s.world.arrival, span.last), isNull);
    expect(s.world.completeBridgeCells, isEmpty);
    s.beginStroke(Tool.bridge, span.first);
    s.stroke!.paintTo(span.last);
    expect(s.stroke!.price, 3000);
    expect(s.finishStroke(), isTrue);
    expect(s.world.encode(), bridge);
    expect(s.money, 188000);
    expect(s.undo(), isTrue);
    expect(s.money, 191000);
    expect(s.undo(), isTrue);
    expect(s.world.encode(), bridge);
  });
  test(
    'floating, bent, overlong, occupied and unfunded bridges cannot commit',
    () {
      final w = generated(),
          span = riverSpan(w),
          s = LandscapeSession(w, money: 1);
      expect(evaluateBridge(w, span.first, span.last).valid, isFalse);
      final c = crossingTown(), far = riverSpan(c.world);
      expect(
        evaluateBridge(c.world, far.first, Cell(far.last.x, 16)).valid,
        isFalse,
      );
      expect(
        evaluateBridge(c.world, const Cell(1, 15), far.last).valid,
        isFalse,
      );
      s.beginStroke(Tool.path, const Cell(7, 15));
      s.stroke!.paintTo(span.first);
      expect(s.finishStroke(), isTrue);
      s.beginStroke(Tool.bridge, span.first);
      s.stroke!.paintTo(span.last);
      expect(s.stroke!.firstInvalid!.message, contains('9'));
      expect(s.finishStroke(), isFalse);
      expect(s.money, 1);
    },
  );
  test('actual residents cross water only on a complete crossing and stop on a broken crossing', () {
    final c = crossingTown(), span = riverSpan(c.world);
    var crossingObserved = false;
    for (var i = 0; i < 1600; i++) {
      c.update(.05);
      if (c.people.values.any(
        (r) => r.x > span.first.x + 1 && r.x < span.last.x,
      )) {
        crossingObserved = true;
        break;
      }
    }
    expect(crossingObserved, isTrue);
    final gap = Cell(span.first.x + 2, 15);
    paint(c, Tool.remove, gap);
    final earned = c.workEarned;
    expect(c.world.activePaths.contains(span.last), isFalse);
    expect(c.navigation.route(c.world.arrival, span.last), isNull);
    advance(c, 8);
    expect(c.workEarned, earned);
    expect(c.people.values.any((r) => r.route.contains(gap)), isFalse);
    expect(Colony.decode(c.encode()).encode(), c.encode());
    paint(c, Tool.bridge, span.first, span.last);
    advance(c, 20);
    expect(c.workEarned, greaterThan(earned));
  });
  test('weighted routes and actual edge speed compose terrain, weather, road and trait once', () {
    final w = Landscape.create().replace(
      const Cell(4, 15),
      const Lot(path: true, terrain: Terrain.marsh),
    );
    expect(terrainRoadCost(w.at(const Cell(4, 15)), false), 1.5);
    expect(terrainRoadCost(w.at(const Cell(4, 15)), true), 1.5);
    expect(
      edgeTravelCost(w, const Cell(3, 15), const Cell(4, 15), false),
      1.25,
    );
    final stone = w.replace(
      const Cell(4, 15),
      const Lot(path: true, terrain: Terrain.marsh, road: RoadKind.stone),
    );
    expect(
      terrainRoadCost(stone.at(const Cell(4, 15)), true),
      closeTo(1 / 1.18, 1e-9),
    );
    final c = Colony(LandscapeSession(w))..nextSpecialAt = 1e9;
    paint(c, Tool.cottage, const Cell(3, 14));
    final r = c.people.values.first;
    expect(c.walkingSpeed(r, const Cell(4, 15)), greaterThan(0));
    final n = c.navigation, rev = n.revision;
    n.route(const Cell(0, 15), const Cell(7, 15));
    final fields = n.fieldsBuilt;
    paint(c, Tool.stoneRoad, const Cell(4, 15));
    expect(n.revision, rev + 1);
    expect(
      n.route(const Cell(0, 15), const Cell(7, 15), expectedRevision: rev),
      isNull,
    );
    n.route(const Cell(0, 15), const Cell(7, 15));
    expect(n.fieldsBuilt, fields + 1);
  });
  test('weighted navigation chooses the cheaper dry/stone detour and agrees with traversal speed', () {
    final w = Landscape(202, 16, 12, [
      for (var y = 0; y < 12; y++)
        for (var x = 0; x < 16; x++)
          Lot(
            path:
                (x >= 2 && x <= 10 && (y == 5 || y == 7)) ||
                (y == 6 && (x == 2 || x == 10)),
            terrain: y == 5 ? Terrain.marsh : Terrain.ground,
            road: y == 5 ? RoadKind.dirt : RoadKind.stone,
          ),
    ]);
    final n = Navigation(w),
        route = n.route(const Cell(2, 5), const Cell(10, 5))!;
    expect(route.any((c) => c.y == 7), isTrue);
    var cost = 0.0;
    for (var i = 1; i < route.length; i++) {
      cost += edgeTravelCost(w, route[i - 1], route[i], false);
    }
    expect(n.distance(route.first, route.last), closeTo(cost, 1e-9));
    expect(cost, lessThan(12));
    final wet = Colony(
      LandscapeSession(
        Landscape.create().replace(
          const Cell(4, 15),
          const Lot(path: true, terrain: Terrain.marsh),
        ),
      ),
    );
    paint(wet, Tool.cottage, const Cell(3, 14));
    final r = wet.people.values.first;
    r.cell = const Cell(3, 15);
    r.x = 3.5;
    r.y = 15.5;
    final dry = Colony(LandscapeSession(Landscape.create()));
    expect(
      wet.walkingSpeed(r, const Cell(4, 15)),
      closeTo(dry.walkingSpeed(r, const Cell(4, 15)) / 1.25, 1e-9),
    );
  });
  test('vertical bridges have only aligned edges; removing a land approach invalidates the complete span', () {
    var w = Landscape.create();
    for (var y = 16; y <= 18; y++) {
      w = w.replace(Cell(7, y), const Lot(terrain: Terrain.water));
    }
    final p = evaluateBridge(w, const Cell(7, 15), const Cell(7, 19));
    expect(p.valid, isTrue);
    final next = p.after!;
    expect(next.completeBridgeCells.length, 3);
    expect(next.pathRoute(const Cell(7, 15), const Cell(7, 19)), isNotNull);
    final flank = next.replace(const Cell(8, 17), const Lot(path: true));
    expect(
      flank.pathNeighbours(const Cell(7, 17)).contains(const Cell(8, 17)),
      isFalse,
    );
    expect(flank.pathRoute(const Cell(7, 15), const Cell(8, 17)), isNull);
    final broken = evaluate(next, Tool.remove, const Cell(7, 19)).after!;
    expect(broken.completeBridgeCells, isEmpty);
    expect(broken.pathRoute(const Cell(7, 15), const Cell(7, 17)), isNull);
  });
  test(
    'inspection explains actual terrain, wet road costs and a broken crossing',
    () {
      final c = crossingTown(),
          game = SettlementGame(c, (_) {}),
          span = riverSpan(c.world);
      game.inspectedCell = Cell(span.first.x + 1, 15);
      expect(game.inspection, contains('Complete bank-to-bank'));
      paint(c, Tool.remove, Cell(span.first.x + 2, 15));
      expect(game.inspection, contains('Broken crossing'));
      final wet = c.world.cells.firstWhere(
        (p) => c.world.at(p).terrain == Terrain.marsh,
      );
      game.inspectedCell = wet;
      expect(game.inspection, contains('Wet ground'));
      expect(game.inspection, contains('Clear'));
    },
  );
  test('terrain navigation and work remain identical at different render-frame cadences', () {
    final a = crossingTown(), b = Colony.decode(a.encode());
    for (var i = 0; i < 2000; i++) {
      a.update(.02);
    }
    for (var i = 0; i < 800; i++) {
      b.update(.05);
    }
    expect(a.workEarned, greaterThan(0));
    expect(a.encode(), b.encode());
  });
  test('concrete terrain and new metadata round-trip; future version does not regenerate', () {
    final c = crossingTown();
    advance(c, 10);
    final encoded = c.encode();
    expect(Colony.decode(encoded).encode(), encoded);
    final d = jsonDecode(encoded);
    d['generation']['version'] = 900;
    final altered = jsonEncode(d);
    expect(
      Colony.decode(altered).world.lots.map((l) => l.toJson()).toList(),
      c.world.lots.map((l) => l.toJson()).toList(),
    );
    final copy = Colony.decode(encoded);
    advance(c, 10);
    advance(copy, 10);
    expect(copy.encode(), c.encode());
    for (final bad in [
      () => d['lots'][0][0] = 'invented',
      () => d['generation']['seedText'] = 'different',
    ]) {
      bad();
      expect(() => Colony.decode(jsonEncode(d)), throwsFormatException);
      expect(c.world.generation!.seedText, '202');
    }
  });
  test('legacy checkpoint retains all concrete lots, clock, event RNG and durable people exactly', () {
    final v1 = jsonDecode(
      File('../../docs/world-builder/evidence/goal-07/legacy-v1-river.json')
          .readAsStringSync(),
    );
    final preserved = Colony.decode(jsonEncode(v1));
    expect(preserved.world.generation!.version, 1);
    expect(preserved.world.toJson()['lots'], v1['lots']);
    final input = File(
      '../../docs/world-builder/evidence/tranche-06/need-town.json',
    );
    final source = input.existsSync()
        ? input
        : File('../../docs/world-builder/evidence/tranche-05/large-85.json');
    final text = source.readAsStringSync(),
        d = jsonDecode(text),
        c = Colony.decode(text);
    expect(c.world.generation, isNull);
    expect(c.world.toJson()['lots'], d['lots']);
    expect(c.world.lots.every((l) => l.terrain == Terrain.ground), isTrue);
    final check = jsonDecode(c.encode());
    expect(check['colony']['clock'], d['colony']['clock']);
    expect(check['seed'], d['seed']);
    expect(Colony.decode(c.encode()).encode(), c.encode());
  });
  testWidgets(
    'New World draft cancellation, inspection and export do not replace the active world',
    (tester) async {
      final c = crossingTown(), before = c.encode();
      var exports = 0;
      WorldDraft? result;
      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) => TextButton(
              onPressed: () async {
                result = await showDialog<WorldDraft>(
                  context: context,
                  builder: (_) =>
                      WorldCreationDialog(exportCurrent: () => exports++),
                );
              },
              child: const Text('New'),
            ),
          ),
        ),
      );
      await tester.tap(find.text('New'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField), 'my woods');
      await tester.tap(find.text('Export current copy'));
      expect(exports, 1);
      expect(c.encode(), before);
      await tester.tap(find.text('Continue existing settlement'));
      await tester.pumpAndSettle();
      expect(result, isNull);
      expect(c.encode(), before);
    },
  );
}
