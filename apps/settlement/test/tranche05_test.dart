import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/navigation.dart';
import 'package:roag_settlement/residents.dart';
import 'package:roag_settlement/resident_identity.dart';
import 'package:roag_settlement/rules.dart';
import 'package:roag_settlement/special_event.dart';

import '../tool/scaling_fixture.dart' show scalingTown;
import 'colony_test.dart' show paint, advance;

Colony town() {
  final c = Colony(LandscapeSession(Landscape.create(202)))
    ..nextSpecialAt = 1e9;
  paint(c, Tool.path, const Cell(7, 15), const Cell(18, 15));
  paint(c, Tool.cottage, const Cell(2, 14), const Cell(5, 14));
  paint(c, Tool.cottage, const Cell(2, 16), const Cell(5, 16));
  paint(c, Tool.garden, const Cell(6, 14));
  paint(c, Tool.workshop, const Cell(8, 14));
  paint(c, Tool.market, const Cell(10, 14));
  paint(c, Tool.hall, const Cell(12, 14));
  paint(c, Tool.green, const Cell(14, 14));
  return c;
}

String id(Colony c, Place kind) => c.places(kind).first.key;
void until(Colony c, bool Function() condition, [int ticks = 3000]) {
  for (var i = 0; i < ticks && !condition(); i++) {
    c.update(.05);
  }
  expect(condition(), isTrue);
}

void main() {
  test(
    'three workplaces pay only productive worker time, each at its actual rate',
    () {
      final c = town();
      expect(c.workEarned, 0);
      until(
        c,
        () => [
          Place.garden,
          Place.workshop,
          Place.market,
        ].every((k) => c.workers(id(c, k)) > 0),
      );
      final before = {
        for (final r in c.people.values)
          r.homeId: (r.earned, r.workedSeconds, r.activity, r.workId),
      };
      c.update(.05);
      for (final r in c.people.values) {
        final b = before[r.homeId]!;
        if (b.$3 == Activity.working && r.workedSeconds > b.$2) {
          expect(
            r.earned - b.$1,
            closeTo((r.workedSeconds - b.$2) * c.kind(b.$4)!.income, 1),
          );
        }
      }
      final work = c.workEarned;
      paint(c, Tool.remove, c.world.places[id(c, Place.workshop)]!);
      advance(c, .5);
      expect(
        c.people.values
            .where((r) => r.activity == Activity.working)
            .every((r) => c.placeAccess(r.workId) == r.cell),
        isTrue,
      );
      expect(c.workEarned, greaterThanOrEqualTo(work));
    },
  );
  test('Markets need a present vendor, real customers and six service seconds; credits survive reload and demolition', () {
    final c = town();
    until(c, () => c.tradeEarned > 0);
    final customers = c.people.values
        .where((r) => r.tradeDay == c.day)
        .toList();
    expect(customers, isNotEmpty);
    expect(c.tradeEarned, customers.length * Rules.tradeCredit);
    expect(
      customers.every((r) => r.lastShopping >= 0 && r.lastSocial >= 0),
      isTrue,
    );
    for (var i = 0; i < 1200; i++) {
      c.update(.05);
      expect(c.occupancy(id(c, Place.market)), lessThanOrEqualTo(3));
    }
    expect(
      c.tradeEarned,
      c.people.values.where((r) => r.tradeDay == c.day).length *
          Rules.tradeCredit,
    );
    final copy = Colony.decode(c.encode());
    expect(copy.tradeEarned, c.tradeEarned);
    final earned = copy.tradeEarned,
        market = copy.world.places[id(copy, Place.market)]!;
    paint(copy, Tool.remove, market);
    advance(copy, 3);
    expect(copy.tradeEarned, earned);
    copy.session.undo();
    copy.reconcile(
      copy.world == copy.session.world ? copy.world : copy.session.world,
    );
    advance(copy, 15);
    expect(
      copy.people.values.where((r) => r.tradeDay == copy.day).length *
          Rules.tradeCredit,
      copy.tradeEarned,
    );
  });
  test(
    'an empty Market creates no trade and no unstaffed service progress',
    () {
      final c = town();
      until(c, () => c.workers(id(c, Place.market)) > 0);
      final vendor = c.people.values.firstWhere(
        (r) => r.workId == id(c, Place.market),
      );
      paint(c, Tool.remove, c.world.cottages[vendor.homeId]!);
      // one fixed tick validates the new staffing before any customer progress.
      final before = c.tradeEarned;
      c.update(.05);
      expect(
        c.people.values
            .where(
              (r) =>
                  r.destination == id(c, Place.market) &&
                  r.activity == Activity.socialising,
            )
            .isEmpty,
        isTrue,
      );
      expect(c.tradeEarned, before);
    },
  );
  test('road replacements charge exact changed cells, dedupe, cancel, Undo and atomic Redo', () {
    final c = town(), money = c.session.money;
    paint(c, Tool.stoneRoad, const Cell(2, 15), const Cell(5, 15));
    expect(c.session.money, money - 8000);
    expect(c.world.at(const Cell(3, 15)).road, RoadKind.stone);
    c.session.beginStroke(Tool.stoneRoad, const Cell(2, 15));
    c.session.stroke!.paintTo(const Cell(5, 15));
    expect(c.session.stroke!.changed, isEmpty);
    expect(c.session.finishStroke(), isFalse);
    c.session.beginStroke(Tool.gardenPath, const Cell(3, 15));
    expect(c.session.stroke!.price, 1000);
    c.session.cancelStroke();
    expect(c.session.money, money - 8000);
    paint(c, Tool.path, const Cell(3, 15));
    expect(c.session.money, money - 8000);
    c.session.undo();
    c.reconcile(c.session.world);
    expect(c.world.at(const Cell(3, 15)).road, RoadKind.stone);
    c.session.undo();
    c.reconcile(c.session.world);
    expect(c.session.money, money);
    c.session.money = 7999;
    expect(c.session.redo(), isFalse);
    expect(c.world.at(const Cell(2, 15)).road, RoadKind.dirt);
  });
  test('Stone, Garden, rain and Brisk factors share actual segment costs; an underway segment freezes', () {
    final c = town(), r = c.people.values.first;
    r.trait = Trait.sociable;
    r.cell = const Cell(3, 15);
    r.x = 3.5;
    r.y = 15.5;
    final normal = c.walkingSpeed(r, const Cell(4, 15));
    paint(c, Tool.stoneRoad, const Cell(3, 15), const Cell(4, 15));
    expect(
      c.walkingSpeed(r, const Cell(4, 15)),
      closeTo(normal * Rules.stoneSpeed, 1e-9),
    );
    expect(c.beginSpecial(SpecialKind.rain, fixture: true), isTrue);
    expect(
      c.walkingSpeed(r, const Cell(4, 15)),
      closeTo(normal * Rules.stoneSpeed, 1e-9),
    );
    paint(c, Tool.path, const Cell(3, 15), const Cell(4, 15));
    expect(
      c.walkingSpeed(r, const Cell(4, 15)),
      closeTo(normal / Rules.rainCost, 1e-9),
    );
    r.trait = Trait.brisk;
    expect(
      c.walkingSpeed(r, const Cell(4, 15)),
      closeTo(normal * Rules.briskSpeed / Rules.rainCost, 1e-9),
    );
    r.arrived = true;
    r.route.clear();
    c.go(r, const Cell(5, 15), Activity.returning);
    c.update(.05);
    final speed = r.segmentSpeed;
    paint(c, Tool.stoneRoad, const Cell(3, 15), const Cell(5, 15));
    expect(r.segmentSpeed, speed);
    c.update(.05);
    expect(r.segmentSpeed, speed);
  });
  test('weighted route fields prefer a longer quick route and reject obsolete revisions', () {
    const w = 16, h = 12;
    final world = Landscape(202, w, h, [
      for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
          Lot(
            path: y == 5 || y == 4 || x == 0,
            road: y == 4 ? RoadKind.stone : RoadKind.dirt,
          ),
    ]);
    final nav = Navigation(world, rain: true),
        start = const Cell(0, 5),
        end = const Cell(15, 5);
    final path = nav.route(start, end)!;
    expect(path.any((p) => p.y == 4), isTrue);
    final expanded = nav.expansions;
    nav.route(start, end);
    expect(nav.expansions, expanded);
    expect(nav.hits, greaterThan(0));
    final revision = nav.revision;
    nav.invalidate(world, raining: false);
    expect(nav.route(start, end, expectedRevision: revision), isNull);
    expect(nav.stale, 1);
  });
  test('Garden Path benefit needs a completed traversal, once daily; edits and Undo do not award or rewind it', () {
    final c = town(), r = c.people.values.first;
    r.cell = const Cell(3, 15);
    r.x = 3.5;
    r.y = 15.5;
    r.arrived = true;
    r.route.clear();
    paint(c, Tool.gardenPath, const Cell(3, 15), const Cell(5, 15));
    expect(r.gardenDay, -1);
    c.go(r, const Cell(5, 15), Activity.returning);
    until(c, () => r.gardenDay == c.day);
    expect(c.contributions(r)['Garden path'], Rules.gardenPathBenefit);
    final day = r.gardenDay;
    c.session.undo();
    c.reconcile(c.session.world);
    expect(r.gardenDay, day);
    final copy = Colony.decode(c.encode());
    expect(copy.people[r.homeId]!.gardenDay, day);
  });
  test('rain prefers an available Hall for new visits without interrupting a valid visit', () {
    final c = town();
    until(
      c,
      () => c.people.values.any((r) => r.activity == Activity.socialising),
    );
    final visiting = c.people.values.firstWhere(
          (r) => r.activity == Activity.socialising,
        ),
        destination = visiting.destination;
    expect(c.beginSpecial(SpecialKind.rain, fixture: true), isTrue);
    expect(visiting.destination, destination);
    until(
      c,
      () => c.people.values.any(
        (r) =>
            r.destination == id(c, Place.hall) &&
            [Activity.socialTravel, Activity.socialising].contains(r.activity),
      ),
    );
  });
  test('harvest bonus only pays for actual Farm work, not workshop work or ownership', () {
    final c = town();
    until(
      c,
      () =>
          c.workers(id(c, Place.garden)) > 0 &&
          c.workers(id(c, Place.workshop)) > 0,
    );
    c.beginSpecial(SpecialKind.harvest, fixture: true);
    final farmer = c.people.values.firstWhere(
          (r) => r.workId == id(c, Place.garden),
        ),
        smith = c.people.values.firstWhere(
          (r) => r.workId == id(c, Place.workshop),
        );
    final before = {for (final r in c.people.values) r.homeId: r.workedSeconds},
        earned = c.workEarned;
    final smithEarned = smith.earned;
    advance(c, .5);
    final expected = c.people.values.fold<double>(
      0,
      (sum, r) =>
          sum +
          (r.workedSeconds - before[r.homeId]!) *
              (c.kind(r.workId) == Place.garden
                  ? 250
                  : c.kind(r.workId)?.income ?? 0),
    );
    expect(c.workEarned - earned, closeTo(expected, 1));
    expect(smith.earned - smithEarned, 200);
    expect(farmer.workedSeconds, greaterThan(before[farmer.homeId]!));
  });
  test('Market day bonuses attach to the single daily credited trade', () {
    final c = town();
    until(c, () => c.eligibleSpecial(SpecialKind.marketDay));
    c.beginSpecial(SpecialKind.marketDay, fixture: true);
    until(c, () => c.tradeEarned > 0);
    final earned = c.tradeEarned,
        credited = c.people.values.where((r) => r.tradeDay == c.day).length;
    expect(earned, credited * (Rules.tradeCredit + Rules.marketDayBonus));
    final snapshot = c.encode(), copy = Colony.decode(snapshot);
    expect(copy.special!.id, c.special!.id);
    expect(copy.specialRng, c.specialRng);
    expect(copy.tradeEarned, earned);
    for (var i = 0; i < 200; i++) {
      c.update(.05);
      copy.update(.05);
    }
    expect(copy.encode(), c.encode());
  });
  test(
    'natural event decisions are seeded, one at a time and survive reload/Undo',
    () {
      final c = town()..nextSpecialAt = 12;
      advance(c, 13);
      expect(c.special, isNotNull);
      expect(c.special!.active(c.clock), isTrue);
      final id = c.special!.id, clock = c.clock, rng = c.specialRng;
      paint(c, Tool.stoneRoad, const Cell(17, 15));
      c.session.undo();
      c.reconcile(c.session.world);
      expect(c.special!.id, id);
      expect(c.clock, clock);
      expect(c.specialRng, rng);
      final copy = Colony.decode(c.encode());
      advance(c, 40);
      advance(copy, 40);
      expect(copy.encode(), c.encode());
    },
  );
  test('names, traits, appearances and daily credits remain stable through legacy load and reconciliation', () {
    final old = File(
      '../../docs/world-builder/evidence/tranche-04/residents-85-active.json',
    ).readAsStringSync();
    final c = Colony.decode(old),
        looks = {
          for (final r in c.people.values)
            r.homeId: (r.name, r.trait, r.bodyId, r.palette),
        };
    expect(c.special, isNull);
    expect(c.nextSpecialAt, closeTo(c.clock + 12, 1e-8));
    final copy = Colony.decode(c.encode());
    expect({
      for (final r in copy.people.values)
        r.homeId: (r.name, r.trait, r.bodyId, r.palette),
    }, looks);
    expect(copy.session.money, c.session.money);
    expect(copy.world.toJson()['lots'], jsonDecode(old)['lots']);
  });
  test('Green thumb guides new assignment and retains an existing valid job; Sociable benefits a real visit', () {
    final prefer = Colony(LandscapeSession(Landscape.create(202)))
      ..nextSpecialAt = 1e9;
    paint(prefer, Tool.path, const Cell(7, 15), const Cell(12, 15));
    paint(prefer, Tool.cottage, const Cell(2, 14));
    paint(prefer, Tool.workshop, const Cell(3, 14));
    paint(prefer, Tool.garden, const Cell(9, 14));
    final thumb = prefer.people.values.single..trait = Trait.greenThumb;
    until(prefer, () => thumb.workId != null);
    expect(prefer.kind(thumb.workId), Place.garden);
    final c = town(), r = c.people.values.last;
    r.trait = Trait.greenThumb;
    until(c, () => r.workId != null || c.jobs(id(c, Place.garden)) == 2);
    final social = c.people.values.firstWhere((r) => r.trait == Trait.sociable);
    until(c, () => social.lastSocial >= 0);
    expect(
      c.contributions(social)['Recent leisure'],
      Rules.socialBenefit + Rules.sociableBenefit,
    );
    final assigned = c.people.values.firstWhere((r) => r.workId != null),
        job = assigned.workId;
    assigned.trait = Trait.greenThumb;
    paint(c, Tool.garden, const Cell(16, 14));
    advance(c, .5);
    expect(assigned.workId, job);
  });
  test('bounded fair planning rejects stale work and drains after a substantial 500-person edit', () {
    final c = scalingTown(500)..speed = 1;
    advance(c, 4);
    c.session.beginStroke(Tool.remove, const Cell(2, 15));
    c.session.stroke!.paintTo(const Cell(18, 15));
    expect(c.session.finishStroke(), isTrue);
    c.reconcile(c.session.world);
    final revision = c.navigation.revision;
    final discarded = c.discardedPlans;
    for (var i = 0; i < 350; i++) {
      final before = c.planned;
      c.update(.05);
      expect(c.planned - before, lessThanOrEqualTo(Rules.planningBudget));
      for (final r in c.people.values.where((r) => r.route.isNotEmpty)) {
        expect(c.world.at(r.route.first).path, isTrue);
      }
    }
    expect(c.navigation.revision, greaterThanOrEqualTo(revision));
    expect(c.planning.length, lessThan(20));
    expect(c.planned, greaterThan(500));
    expect(
      c.people.values.every((r) => r.activity != Activity.pending),
      isTrue,
    );
    expect(c.discardedPlans, greaterThanOrEqualTo(discarded));
  });
  test('fixed-step progression agrees across render rates and off-screen presentation is irrelevant', () {
    final a = town(), b = Colony.decode(a.encode());
    for (var i = 0; i < 1800; i++) {
      a.update(1 / 60);
    }
    for (var i = 0; i < 600; i++) {
      b.update(.05);
    }
    expect(a.encode(), b.encode());
  });
  test('road/weather/trait costs change actual arrival ticks, and daily path credit renews only after a later traversal', () {
    int travel(Tool road, bool rain, Trait trait) {
      final c = town(), r = c.people.values.first;
      if (road != Tool.path)
        paint(c, road, const Cell(3, 15), const Cell(9, 15));
      if (rain) c.beginSpecial(SpecialKind.rain, fixture: true);
      r.trait = trait;
      r.arrived = true;
      r.route.clear();
      r.cell = const Cell(3, 15);
      r.x = 3.5;
      r.y = 15.5;
      r.segmentSpeed = 0;
      c.go(r, const Cell(9, 15), Activity.returning);
      var ticks = 0;
      while (r.cell != const Cell(9, 15) && ticks < 300) {
        c.update(.05);
        ticks++;
      }
      expect(r.cell, const Cell(9, 15));
      return ticks;
    }

    final dirt = travel(Tool.path, false, Trait.sociable),
        stone = travel(Tool.stoneRoad, false, Trait.sociable);
    expect(stone, lessThan(dirt));
    expect(travel(Tool.stoneRoad, true, Trait.sociable), stone);
    expect(travel(Tool.path, true, Trait.sociable), greaterThan(dirt));
    expect(travel(Tool.gardenPath, false, Trait.sociable), dirt);
    expect(travel(Tool.path, false, Trait.brisk), lessThan(dirt));
    final c = town(), r = c.people.values.first;
    paint(c, Tool.gardenPath, const Cell(3, 15), const Cell(5, 15));
    r.arrived = true;
    r.cell = const Cell(3, 15);
    r.x = 3.5;
    r.y = 15.5;
    r.route.clear();
    r.segmentSpeed = 0;
    c.go(r, const Cell(4, 15), Activity.returning);
    until(c, () => r.gardenDay == 1);
    c.clock = Rules.day - .05;
    r.route.clear();
    r.segmentSpeed = 0;
    r.activity = Activity.resting;
    r.timer = 2;
    c.update(.05);
    expect(c.day, 2);
    expect(c.contributions(r)['Garden path'], 0);
    c.go(r, const Cell(5, 15), Activity.returning);
    until(c, () => r.gardenDay == 2);
    expect(c.contributions(r)['Garden path'], 3);
  });
  test('rain changes a new visit choice from nearby Park to covered Hall', () {
    final a = Colony(LandscapeSession(Landscape.create()))..nextSpecialAt = 1e9;
    paint(a, Tool.path, const Cell(7, 15), const Cell(12, 15));
    paint(a, Tool.cottage, const Cell(2, 14));
    paint(a, Tool.green, const Cell(3, 14));
    paint(a, Tool.hall, const Cell(10, 14));
    final b = Colony.decode(a.encode());
    b.beginSpecial(SpecialKind.rain, fixture: true);
    until(a, () => a.people.values.single.destination != null);
    until(b, () => b.people.values.single.destination != null);
    expect(a.kind(a.people.values.single.destination), Place.green);
    expect(b.kind(b.people.values.single.destination), Place.hall);
  });
  test('preview never invalidates navigation; import rebases fresh queue work while preserving continuation', () {
    final a = town();
    for (final r in a.people.values) a.decide(r);
    final revision = a.navigation.revision;
    a.session.beginStroke(Tool.stoneRoad, const Cell(2, 15));
    a.session.stroke!.paintTo(const Cell(8, 15));
    expect(a.navigation.revision, revision);
    a.session.cancelStroke();
    final b = Colony.decode(a.encode());
    b.rebaseNavigation(revision + 20);
    expect(b.navigation.revision, greaterThan(revision + 20));
    Map normal(Colony c) {
      final d = jsonDecode(c.encode());
      d['colony'].remove('navigationRevision');
      for (final p in d['colony']['planning']) p['revision'] = 0;
      return d;
    }

    for (var i = 0; i < 300; i++) {
      a.update(.05);
      b.update(.05);
    }
    expect(normal(b), normal(a));
  });
  test('special eligibility, cooldown and expiry are explicit and do not pay ownership income', () {
    final empty = Colony(LandscapeSession(Landscape.create()));
    expect(empty.beginSpecial(SpecialKind.rain, fixture: true), isFalse);
    final c = Colony(LandscapeSession(Landscape.create()));
    paint(c, Tool.cottage, const Cell(2, 14));
    expect(c.beginSpecial(SpecialKind.harvest, fixture: true), isFalse);
    expect(c.beginSpecial(SpecialKind.marketDay, fixture: true), isFalse);
    c.beginSpecial(SpecialKind.rain, fixture: true);
    final first = c.special!.id;
    expect(c.beginSpecial(SpecialKind.harvest, fixture: true), isFalse);
    advance(c, 25.1);
    expect(c.special!.active(c.clock), isFalse);
    expect(c.raining, isFalse);
    expect(c.navigation.rain, isFalse);
    expect(c.workEarned, 0);
    advance(c, 10);
    expect(c.special!.id, first);
    expect(
      c.beginSpecial(SpecialKind.rain),
      isFalse,
      reason: 'normal event entry enforces cooldown',
    );
  });
  test(
    'malformed additive save fields are rejected at their owning boundary',
    () {
      final c = town();
      until(c, () => c.workers(id(c, Place.garden)) > 0);
      c.beginSpecial(SpecialKind.rain, fixture: true);
      final text = c.encode();
      void rejects(void Function(Map) change) {
        final d = jsonDecode(text) as Map;
        change(d);
        expect(() => Colony.decode(jsonEncode(d)), throwsFormatException);
      }

      rejects((d) => d['colony']['residents'][0]['trait'] = 'unknown');
      rejects((d) => d['colony']['residents'][0]['tradeDay'] = 99999);
      rejects((d) => d['colony']['special']['ends'] += 1000);
      rejects((d) => d['colony']['nextSpecialId'] = 1);
      rejects(
        (d) => d['colony']['planning'] = [
          {'id': 'missing', 'revision': 0, 'plan': true},
        ],
      );
      rejects((d) => d['lots'][15 * d['width'] + 3].add('unknown-road'));
    },
  );
}
