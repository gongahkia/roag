import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/reasons.dart';
import 'package:roag_settlement/residents.dart';
import 'package:roag_settlement/rules.dart';
import 'package:roag_settlement/resident_identity.dart';

void advance(Colony c, double seconds) {
  for (var i = 0; i < (seconds / .05).round(); i++) {
    c.update(.05);
  }
}

void paint(Colony c, Tool t, Cell start, [Cell? end]) {
  c.session.beginStroke(t, start);
  c.session.stroke!.paintTo(end ?? start);
  expect(c.session.finishStroke(), isTrue);
  c.reconcile(c.session.world);
}

Colony village({bool garden = true, bool green = true}) {
  final c = Colony(LandscapeSession(Landscape.create()));
  paint(c, Tool.path, const Cell(7, 15), const Cell(12, 15));
  paint(c, Tool.cottage, const Cell(2, 14), const Cell(5, 14));
  if (garden) paint(c, Tool.garden, const Cell(8, 14));
  if (green) paint(c, Tool.green, const Cell(10, 14));
  return c;
}

void main() {
  test(
    'work requires real travel, two jobs, and validated productive time',
    () {
      final c = village();
      expect(c.workEarned, 0);
      for (
        var i = 0;
        i < 160 &&
            !c.people.values.any((r) => r.activity == Activity.workTravel);
        i++
      ) {
        c.update(.05);
      }
      expect(c.workEarned, 0, reason: 'assignments and journeys pay nothing');
      expect(
        c.people.values.any((r) => r.activity == Activity.workTravel),
        isTrue,
      );
      advance(c, 9);
      expect(c.jobs(c.places(Place.garden).single.key), 2);
      expect(c.workEarned, greaterThan(0));
      expect(
        c.workEarned / 1000,
        closeTo(
          c.people.values.map((r) => r.workedSeconds).reduce((a, b) => a + b) *
              .2,
          .001,
        ),
      );
      expect(
        c.people.values.where((r) => r.activity == Activity.working).length,
        lessThanOrEqualTo(2),
      );
    },
  );
  test(
    'fresh garden promptly fills during daytime, assignments stay stable',
    () {
      final c = village(garden: false, green: false);
      advance(c, 10);
      expect(c.people.values.any((r) => r.workIssue == Reason.noWork), isTrue);
      paint(c, Tool.garden, const Cell(8, 14));
      advance(c, 6);
      expect(c.workEarned, greaterThan(0));
      final assigned = {
        for (final r in c.people.values)
          if (r.workId != null) r.homeId: r.workId,
      };
      paint(c, Tool.garden, const Cell(6, 14));
      advance(c, 1);
      for (final e in assigned.entries) {
        expect(c.people[e.key]!.workId, e.value);
      }
    },
  );
  test(
    'social benefits require a real six-second visit and seats count arrivals',
    () {
      final c = village(garden: false);
      for (
        var i = 0;
        i < 160 &&
            !c.people.values.any((r) => r.activity == Activity.socialTravel);
        i++
      ) {
        c.update(.05);
      }
      expect(c.people.values.every((r) => r.lastSocial < 0), isTrue);
      final green = c.places(Place.green).single.key;
      expect(c.occupancy(green), 0);
      expect(
        c.people.values.any((r) => r.activity == Activity.socialTravel),
        isTrue,
      );
      advance(c, 10);
      expect(c.people.values.any((r) => r.lastSocial >= 0), isTrue);
      expect(
        c.people.values
            .where((r) => r.lastSocial >= 0)
            .every(
              (r) =>
                  c.contributions(r)['Recent leisure'] ==
                  Rules.socialBenefit +
                      (r.trait == Trait.sociable ? Rules.sociableBenefit : 0),
            ),
        isTrue,
      );
    },
  );
  test('mood starts neutral, grace protects newcomers, valid work travel is not a failure', () {
    final c = village(green: false);
    advance(c, 4);
    for (final r in c.people.values) {
      expect(c.happiness(r), Rules.moodBase);
    }
    expect(
      c.people.values
          .where((r) => r.activity == Activity.workTravel)
          .every((r) => r.problem != Reason.workCut),
      isTrue,
    );
    advance(c, 80);
    expect(c.people.values.every((r) => c.happiness(r) >= 30), isTrue);
    expect(c.grantEarned, greaterThan(0));
  });
  test(
    'cut workplace reports a broken connection, restored access resumes work',
    () {
      final c = village();
      advance(c, 10);
      paint(c, Tool.remove, const Cell(7, 15));
      advance(c, 2);
      expect(
        c.people.values
            .where((r) => r.workId != null)
            .every((r) => r.workIssue == Reason.workCut),
        isTrue,
      );
      final earned = c.workEarned;
      advance(c, 2);
      expect(c.workEarned, earned);
      paint(c, Tool.path, const Cell(7, 15));
      advance(c, 8);
      expect(c.workEarned, greaterThan(earned));
      expect(c.people.values.any((r) => r.problem == Reason.workCut), isFalse);
    },
  );
  test('fixed stroke budget retains background earnings on commit and cancellation', () {
    final c = village();
    advance(c, 10);
    c.session.money = 9000;
    c.session.beginStroke(Tool.cottage, const Cell(6, 14));
    c.session.stroke!.paintTo(const Cell(7, 14));
    expect(c.session.stroke!.changed.length, 1);
    final before = c.session.money;
    advance(c, 2);
    final earned = c.session.money - before;
    expect(earned, greaterThan(0));
    c.session.stroke!.paintTo(const Cell(9, 14));
    expect(
      c.session.stroke!.price,
      5000,
      reason: 'background earnings do not enlarge reserved budget',
    );
    expect(c.session.finishStroke(), isTrue);
    c.reconcile(c.session.world);
    expect(c.session.money, 4000 + earned);
    final snapshot = c.session.world.encode();
    c.session.beginStroke(Tool.garden, const Cell(11, 14));
    final balance = c.session.money;
    advance(c, 1);
    c.session.cancelStroke();
    expect(c.session.world.encode(), snapshot);
    expect(c.session.money, greaterThan(balance));
  });
  test('undo refunds only recorded price; redo is atomic if unaffordable', () {
    final c = village();
    advance(c, 10);
    paint(c, Tool.cottage, const Cell(6, 14), const Cell(7, 14));
    advance(c, 2);
    final income = c.workEarned, time = c.clock, money = c.session.money;
    final ids = c.session.world.cottages.keys.toSet(),
        counter = c.session.world.nextCottageId;
    expect(c.session.undo(), isTrue);
    c.reconcile(c.session.world, restored: true);
    expect(c.session.money, money + 10000);
    expect(c.workEarned, income);
    expect(c.clock, time);
    expect(c.session.world.nextCottageId, counter);
    c.session.money = 9999;
    final world = c.session.world.encode();
    expect(c.session.redo(), isFalse);
    expect(c.session.canRedo, isTrue);
    expect(c.session.world.encode(), world);
    expect(c.session.money, 9999);
    advance(c, .1);
    expect(c.session.redo(), isTrue);
    c.reconcile(c.session.world, restored: true);
    expect(c.session.world.cottages.keys.toSet(), ids);
  });
  test('demolition is free and undo demolition does not charge; unlimited records zero', () {
    final c = village();
    final money = c.session.money;
    paint(c, Tool.remove, const Cell(2, 14));
    expect(c.session.money, money);
    c.session.undo();
    c.reconcile(c.session.world, restored: true);
    expect(c.session.money, money);
    c.session.unlimited = true;
    paint(c, Tool.cottage, const Cell(6, 14));
    expect(c.session.money, money);
    c.session.undo();
    expect(c.session.money, money);
    c.session.unlimited = false;
    c.session.redo();
    expect(c.session.money, money);
  });
  test('resident identity, appearance, earnings and experience survive removal and undo', () {
    final c = village();
    advance(c, 30);
    final r = c.people.values.firstWhere((r) => r.earned > 0),
        home = c.world.cottages.values.first;
    final id = r.homeId, position = c.world.cottages[id]!;
    final earnings = r.earned,
        social = r.lastSocial,
        palette = r.palette,
        progress = r.workProgress;
    paint(c, Tool.remove, position);
    expect(c.people.containsKey(id), isFalse);
    expect(c.inactive[id], same(r));
    advance(c, 2);
    expect(r.earned, earnings);
    c.session.undo();
    c.reconcile(c.session.world, restored: true);
    expect(c.people[id], same(r));
    expect(r.earned, earnings);
    expect(r.lastSocial, social);
    expect(r.palette, palette);
    expect(r.workProgress, progress);
    expect(c.people.length, c.world.cottages.length);
    expect(home, isNotNull);
  });
  test('current checkpoint resumes progress and fractional income without grants or duplicates', () {
    final c = village();
    advance(c, 10.35);
    final text = c.encode(), copy = Colony.decode(text);
    expect(copy.encode(), text);
    for (var i = 0; i < 200; i++) {
      c.update(.05);
      copy.update(.05);
    }
    expect(copy.encode(), c.encode());
  });
  test('pause, accelerated simulation and large background delta use one bounded clock', () {
    final c = village();
    c.speed = 0;
    final before = c.encode();
    c.update(10000);
    expect(c.encode(), before);
    c.speed = 4;
    c.update(10000);
    expect(c.clock, closeTo(.2, .00001));
    final a = village(), b = village();
    b.speed = 4;
    advance(a, 20);
    advance(b, 5);
    b.speed = 1;
    expect(a.encode(), b.encode());
  });
  test('seeded evening gathering follows real work/social activity and rewards actual attendees once', () {
    final c = village();
    advance(c, 89);
    expect(c.event, isNull);
    advance(c, 10);
    expect(c.event, isNotNull);
    expect(c.event!.state, 'active');
    expect(c.event!.awarded.length, greaterThanOrEqualTo(3));
    for (final id in c.event!.awarded) {
      final r = c.people[id]!;
      expect(r.cell, c.placeAccess(c.event!.green));
      expect(r.awardedEvent, c.event!.id);
    }
    final copy = Colony.decode(c.encode());
    expect(copy.event!.awarded, c.event!.awarded);
    advance(c, 20);
    advance(copy, 20);
    expect(copy.encode(), c.encode());
    expect(c.event!.state, 'ended');
    expect(c.cooldownDay, 3);
  });
  test(
    'green removal ends a gathering, no phantom attendee reward after undo',
    () {
      final c = village();
      advance(c, 90.1);
      final e = c.event!;
      final green = c.world.places[e.green]!;
      paint(c, Tool.remove, green);
      expect(c.event!.state, 'ended');
      final awarded = e.awarded.toSet();
      c.session.undo();
      c.reconcile(c.session.world, restored: true);
      advance(c, 20);
      expect(c.event!.awarded, awarded);
      expect(c.event!.id, e.id);
    },
  );
  test('camera-independent updates agree exactly and querying presentation awards nothing', () {
    final c = village(), other = Colony.decode(c.encode());
    for (var i = 0; i < 2400; i++) {
      for (final r in c.people.values) {
        c.contributions(r);
        c.happiness(r);
      }
      c.visible.toList();
      c.occupancy(c.places(Place.green).single.key);
      c.update(.05);
      other.update(.05);
    }
    expect(c.encode(), other.encode());
  });
  test('74-cottage old-format save migrates once, keeps layout, neutral mood and original palettes', () {
    final original = File(
      '../../docs/world-builder/evidence/tranche-03/legacy-74.json',
    ).readAsStringSync();
    final old = jsonDecode(original), c = Colony.decode(original);
    expect(c.people.length, 74);
    expect(c.session.money, Rules.startingMoney);
    expect(c.happinessMean, 50);
    expect(c.migrated, isTrue);
    expect(
      c.world.lots.map((l) => l.toJson().take(6).toList()).toList(),
      old['lots'],
    );
    expect(
      c.session.canUndo,
      isFalse,
      reason: 'old construction has no new refundable charges',
    );
    final copy = Colony.decode(c.encode());
    expect(copy.session.money, c.session.money);
    expect(copy.people.keys.toSet(), c.people.keys.toSet());
    expect(
      copy.people.values.map((r) => r.palette),
      c.people.values.map((r) => r.palette),
    );
  });
  test('invalid checkpoint is refused without replacing a live colony', () {
    final c = village();
    advance(c, 10);
    final before = c.encode(), data = jsonDecode(before);
    data['colony']['residents'].add(data['colony']['residents'][0]);
    expect(() => Colony.decode(jsonEncode(data)), throwsFormatException);
    expect(c.encode(), before);
    data['colony']['residents'].removeLast();
    data['colony']['money'] = -1;
    expect(() => Colony.decode(jsonEncode(data)), throwsFormatException);
  });
}
