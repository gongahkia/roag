import 'dart:convert';
import 'dart:io';
import 'dart:ui';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/residents.dart';
import 'package:roag_settlement/reasons.dart';
import 'package:roag_settlement/speech.dart';
import 'package:roag_settlement/rules.dart';

import 'colony_test.dart' as f;

void main() {
  test('arrival itself pays no work or social reward', () {
    final c = f.village();
    var witnessed = false;
    for (var i = 0; i < 400 && !witnessed; i++) {
      final before = {
        for (final r in c.people.values) r.homeId: (r.activity, r.earned),
      };
      c.update(.05);
      for (final r in c.people.values) {
        if (r.activity == Activity.working &&
            before[r.homeId]!.$1 != Activity.working) {
          expect(r.earned, before[r.homeId]!.$2);
          expect(r.workProgress, 0);
          witnessed = true;
        }
      }
    }
    expect(witnessed, isTrue);
  });
  test('green seats describe real visits and a full-green complaint is physically waiting', () {
    final c = f.village(garden: false);
    f.paint(c, Tool.cottage, const Cell(6, 14), const Cell(9, 14));
    final green = c.places(Place.green).single.key;
    var full = false;
    for (var i = 0; i < 700; i++) {
      c.update(.05);
      expect(c.occupancy(green), lessThanOrEqualTo(Rules.greenCapacity));
      for (final r in c.people.values) {
        if (r.leisureIssue == Reason.socialFull) {
          full = true;
          expect(r.activity, Activity.socialWaiting);
          expect(r.route, isEmpty);
          expect(r.cell, c.placeAccess(green));
        }
      }
    }
    expect(full, isTrue);
  });
  test(
    'home disconnection has one bounded cause, then reconnection removes it',
    () {
      final c = f.village(garden: false, green: false);
      f.advance(c, 80);
      f.paint(c, Tool.remove, const Cell(1, 15));
      f.advance(c, 11);
      for (final r in c.people.values) {
        expect(c.contributions(r)['Home connection'], Rules.housingPenalty);
        expect(c.contributions(r)['Recent work'], 0);
        expect(c.contributions(r)['Recent leisure'], 0);
        expect(r.problem, Reason.homeCut);
      }
      f.paint(c, Tool.path, const Cell(1, 15));
      f.advance(c, 3);
      expect(c.people.values.every((r) => r.problem != Reason.homeCut), isTrue);
    },
  );
  test('a real day accrues exactly the grant and no offline catch-up', () {
    final c = Colony(LandscapeSession(Landscape.create()));
    f.advance(c, 150);
    expect(c.grantEarned, Rules.grantPerDay);
    expect(c.session.money, Rules.startingMoney + Rules.grantPerDay);
    final restored = Colony.decode(c.encode());
    expect(restored.clock, c.clock);
    expect(restored.grantEarned, c.grantEarned);
  });
  test('garden and green brushes price each unique footprint once and cancel without charge', () {
    final c = f.village(garden: false, green: false), before = c.session.money;
    c.session.beginStroke(Tool.garden, const Cell(6, 14));
    c.session.stroke!.paintTo(const Cell(9, 14));
    c.session.stroke!.paintTo(const Cell(6, 14));
    expect(c.session.stroke!.changed.length, 4);
    expect(c.session.stroke!.price, 32000);
    c.session.cancelStroke();
    expect(c.session.money, before);
    expect(c.world.places, isEmpty);
    f.paint(c, Tool.green, const Cell(6, 14), const Cell(9, 14));
    expect(c.session.money, before - 24000);
    final physical = c.world.lots;
    c.session.undo();
    c.reconcile(c.session.world, restored: true);
    expect(c.world.places, isEmpty);
    c.session.redo();
    c.reconcile(c.session.world, restored: true);
    expect(c.world.lots, physical);
  });
  test(
    'speech scheduling linked to a real colony changes no authoritative field',
    () {
      final c = f.village(garden: false, green: false);
      f.advance(c, 6);
      final before = c.encode();
      final d = SpeechDirector(),
          candidates = [
            for (final r in c.people.values)
              BubbleCandidate(r, [
                Rect.fromLTWH(
                  c.people.keys.toList().indexOf(r.homeId) * 230.0,
                  20,
                  210,
                  44,
                ),
              ]),
          ];
      for (var i = 0; i < 2000; i++) {
        d.update(.05, candidates);
      }
      expect(c.encode(), before);
    },
  );
  test('night and morning preserve journeys instead of teleporting or stopping recovery', () {
    final c = f.village();
    f.advance(c, 124);
    var returned = false, rested = false;
    for (var i = 0; i < 600; i++) {
      final positions = {for (final r in c.people.values) r.homeId: (r.x, r.y)};
      c.update(.05);
      for (final r in c.people.values) {
        final p = positions[r.homeId]!;
        expect(
          (r.x - p.$1).abs() + (r.y - p.$2).abs(),
          lessThanOrEqualTo(.251),
        );
        returned |= r.activity == Activity.returning;
        rested |= r.activity == Activity.resting;
      }
    }
    expect(returned, isTrue);
    expect(rested, isTrue);
    expect(c.day, 2);
    expect(c.grantEarned, greaterThan(0));
  });
  test(
    'malformed productive locations and gathering identities are rejected',
    () {
      final c = f.village();
      f.advance(c, 10);
      final data = jsonDecode(c.encode());
      final worker = data['colony']['residents'].firstWhere(
        (r) => r['activity'] == 'working',
      );
      worker['cell'] = [0, 15];
      worker['x'] = .5;
      expect(() => Colony.decode(jsonEncode(data)), throwsFormatException);
      f.advance(c, 90);
      final eventData = jsonDecode(c.encode());
      eventData['colony']['nextEventId'] = 1;
      expect(() => Colony.decode(jsonEncode(eventData)), throwsFormatException);
    },
  );
  test('reconnecting a green at night clears the old unreachable reason immediately', () {
    final c = f.village();
    f.advance(c, 22);
    expect(
      c.people.values.any((r) => r.activity == Activity.socialising),
      isTrue,
    );
    f.paint(c, Tool.remove, const Cell(9, 15));
    f.advance(c, 108);
    expect(c.phase, 'Night');
    expect(
      c.people.values.any((r) => r.leisureIssue == Reason.socialCut),
      isTrue,
    );
    f.paint(c, Tool.path, const Cell(9, 15));
    expect(c.people.values.any((r) => r.problem == Reason.socialCut), isFalse);
  });
  test('schema-one checkpoints also initialise the colony without changing the original lots', () {
    final original = File(
      '../../docs/world-builder/evidence/tranche-01/fuller-landscape.json',
    ).readAsStringSync();
    final data = jsonDecode(original), c = Colony.decode(original);
    expect(c.people.length, 22);
    expect(c.session.money, Rules.startingMoney);
    expect(
      c.world.lots.map((l) => l.toJson().take(5).toList()).toList(),
      data['lots'],
    );
    expect(Colony.decode(c.encode()).encode(), c.encode());
  });
  test(
    'cutting a lane cannot improve mood by hiding two missed opportunities',
    () {
      final c = f.village(garden: false, green: false);
      f.advance(c, 80);
      final before = {
        for (final r in c.people.values) r.homeId: c.happiness(r),
      };
      f.paint(c, Tool.remove, const Cell(1, 15));
      f.advance(c, 11);
      for (final r in c.people.values) {
        expect(c.happiness(r), lessThanOrEqualTo(before[r.homeId]!));
      }
    },
  );
}
