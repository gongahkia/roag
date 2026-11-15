import 'dart:convert';
import 'dart:io';
import 'dart:ui';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/appearance.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/reasons.dart';
import 'package:roag_settlement/resident_presentation.dart';
import 'package:roag_settlement/residents.dart';
import 'package:roag_settlement/rules.dart';
import 'package:roag_settlement/settlement_game.dart';
import 'package:roag_settlement/speech.dart';

import 'colony_test.dart' show village, paint, advance;

Map<String, (String, int)> looks(Colony c) => {
  for (final r in c.records) r.homeId: (r.bodyId, r.palette),
};

void main() {
  test('Farm/Park labels preserve old identifiers, costs and capacities', () {
    final old = File(
      '../../docs/world-builder/evidence/tranche-03/colony-start.json',
    ).readAsStringSync();
    final c = Colony.decode(old), original = jsonDecode(old);
    expect(c.world.toJson()['lots'], original['lots']);
    expect(c.places(Place.garden).length, 1);
    expect(c.places(Place.green).length, 1);
    expect(Tool.garden.label, 'Farm');
    expect(Tool.green.label, 'Park');
    expect(
      Tool.garden.purpose,
      contains('${Rules.gardenCapacity} work positions'),
    );
    expect(
      Tool.green.purpose,
      contains('${Rules.greenCapacity} visitor spaces'),
    );
    expect(
      {
        for (final key in ['cottage', 'garden', 'green']) key: Rules.costs[key],
      },
      {'cottage': 5000, 'garden': 8000, 'green': 6000},
    );
    expect([Rules.gardenCapacity, Rules.greenCapacity], [2, 6]);
    for (final row in original['colony']['residents']) {
      expect(c.people[row['id']]!.palette, row['palette']);
    }
    expect(looks(Colony.decode(c.encode())), looks(c));
  });
  test('stable cast survives unrelated additions, removal, Undo/Redo and portable checkpoints', () {
    final c = village();
    final before = looks(c), money = c.session.money;
    expect(before.values.map((v) => v.$1).toSet().length, 4);
    paint(c, Tool.cottage, const Cell(6, 14));
    for (final e in before.entries) {
      expect(looks(c)[e.key], e.value);
    }
    expect(c.session.undo(), isTrue);
    c.reconcile(c.session.world);
    expect(c.session.money, money);
    expect(c.session.redo(), isTrue);
    c.reconcile(c.session.world);
    paint(c, Tool.remove, const Cell(2, 14));
    final removed = before.keys.first;
    expect(c.inactive[removed]!.bodyId, before[removed]!.$1);
    expect(c.session.undo(), isTrue);
    c.reconcile(c.session.world);
    advance(c, 20);
    for (final e in before.entries) {
      expect(looks(c)[e.key], e.value);
    }
    expect(looks(Colony.decode(c.encode())), looks(c));
  });
  test(
    'unknown appearance IDs retain identity and resolve to a safe sprite',
    () {
      final c = village(), data = jsonDecode(c.encode());
      data['colony']['residents'][0]['bodyId'] = 'future-body-v8';
      final restored = Colony.decode(jsonEncode(data)),
          r = restored.people.values.first;
      expect(r.bodyId, 'future-body-v8');
      expect(
        Appearance.sprite(r.bodyId, r.palette),
        Appearance.sprite('plain-v1', r.palette),
      );
      expect(
        Colony.decode(restored.encode()).people[r.homeId]!.bodyId,
        r.bodyId,
      );
      data['colony']['residents'][0]['bodyId'] = [];
      expect(() => Colony.decode(jsonEncode(data)), throwsFormatException);
    },
  );
  test('standing slots stay bounded and existing people do not shift when another arrives', () {
    final p = ResidentPresentation();
    final people = [
      for (var i = 0; i < 6; i++)
        Resident('home-$i', const Cell(4, 15), Activity.socialising),
    ];
    p.update(.05, people.take(3));
    final first = [for (final r in people.take(3)) p.pose(r).feet];
    p.update(.05, people);
    expect([for (final r in people.take(3)) p.pose(r).feet], first);
    expect(people.map((r) => p.pose(r).feet).toSet().length, 6);
    for (final r in people) {
      final delta = p.pose(r).feet - const Offset(54, 186);
      expect(delta.dx.abs(), lessThanOrEqualTo(3));
      expect(delta.dy.abs(), lessThanOrEqualTo(1.5));
    }
    people.removeAt(1);
    final retained = p.pose(people.last).feet;
    p.update(.05, people);
    expect(p.pose(people.last).feet, retained);
  });
  test('travel lanes stay on the actual cardinal edge; rest and reduced motion remain grounded', () {
    final p = ResidentPresentation(),
        r = Resident('home-1', const Cell(4, 15), Activity.workTravel);
    r.route.add(const Cell(5, 15));
    r.x = 4.99;
    p.update(.05, [r]);
    expect((p.pose(r).feet.dy - r.y * 12).abs(), lessThanOrEqualTo(1.1));
    expect(p.pose(r).feet.dx, r.x * 12);
    expect(p.pose(r, reducedMotion: true).lift, 0);
    expect(p.pose(r, reducedMotion: true).gesture, 0);
    r.route.clear();
    r.activity = Activity.resting;
    p.update(.05, [r]);
    expect(p.pose(r).lift, 0);
    expect(p.pose(r).lean, 0);
    expect(p.pose(r).gesture, 0);
  });
  test('cosmetic updates, speech and inspection cannot advance the colony', () {
    final c = village();
    advance(c, 10);
    final game = SettlementGame(c, (_) {}),
        before = c.encode(),
        director = SpeechDirector();
    for (var i = 0; i < 900; i++) {
      game.presentation.update(.05, c.people.values);
      game.reducedMotion = i.isEven;
      for (final r in c.people.values) {
        game.actorPosition(r);
        game.pose(r);
        game.inspectedResident = r.homeId;
        game.inspection;
        director.update(.05, [
          BubbleCandidate(r, [const Rect.fromLTWH(10, 10, 160, 30)]),
        ]);
      }
    }
    expect(c.encode(), before);
    final copy = Colony.decode(before);
    for (var i = 0; i < 4400; i++) {
      c.update(.05);
      copy.update(.05);
      game.presentation.update(.05, c.people.values);
      for (final r in c.people.values) {
        game.pose(r);
        game.pose(r);
      }
    }
    expect(
      c.encode(),
      copy.encode(),
      reason: '220 seconds includes work, leisure, evening and rest',
    );
  });
  test(
    'inspection and speech distinguish missing farms from disconnected farms',
    () {
      final c = village(garden: false, green: false);
      advance(c, 8);
      final game = SettlementGame(c, (_) {}), r = c.people.values.first;
      expect(r.workIssue, Reason.noWork);
      expect(game.speechReason(r), Reason.noWork);
      paint(c, Tool.garden, const Cell(8, 14));
      paint(c, Tool.remove, const Cell(7, 15));
      advance(c, 4);
      // the person has not been assigned yet; the existing AI still records noWork.
      expect(r.workIssue, Reason.noWork);
      expect(game.speechReason(r), Reason.farmsCut);
      game.inspectedResident = r.homeId;
      expect(game.inspection, contains(Reason.farmsCut.detail));
      expect(game.inspection, isNot(contains('Build a Farm')));
    },
  );
}
