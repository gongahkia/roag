import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/appearance.dart';
import 'package:roag_settlement/climate.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/dialogue.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/navigation.dart';
import 'package:roag_settlement/peoples.dart';
import 'package:roag_settlement/residents.dart';
import 'package:roag_settlement/resident_identity.dart';
import 'package:roag_settlement/rules.dart';
import 'package:roag_settlement/settlement_game.dart';

import 'colony_test.dart' show advance, paint, ordinaryClimate;
import 'tranche05_test.dart' as old;

Colony town() {
  final c = ordinaryClimate(Colony(LandscapeSession(Landscape.create(202))))
    ..nextVisitorAt = 18;
  paint(c, Tool.path, const Cell(7, 15), const Cell(18, 15));
  paint(c, Tool.cottage, const Cell(2, 14), const Cell(7, 14));
  paint(c, Tool.cottage, const Cell(2, 16), const Cell(7, 16));
  paint(c, Tool.garden, const Cell(8, 14));
  paint(c, Tool.workshop, const Cell(10, 14));
  paint(c, Tool.market, const Cell(12, 14));
  paint(c, Tool.hall, const Cell(14, 14));
  paint(c, Tool.green, const Cell(16, 14));
  return c;
}

void until(Colony c, bool Function() condition, [int ticks = 3000]) =>
    old.until(c, condition, ticks);

void main() {
  test('versioned calendar has four six-day seasons, years and anchored legacy dates', () {
    const c = Calendar();
    expect(
      [for (var d = 0; d < 24; d++) c.season(d * Rules.day)],
      [for (final s in Season.values) ...List.filled(6, s)],
    );
    expect(c.season(24 * Rules.day), Season.spring);
    expect(c.year(24 * Rules.day), 2);
    final old = Calendar(anchorDay: 150);
    expect(old.season(150 * Rules.day), Season.spring);
    expect(old.seasonDay(150 * Rules.day), 1);
    expect(
      Calendar.decode(old.toJson(), 150 * Rules.day).toJson(),
      old.toJson(),
    );
    expect(
      () =>
          Calendar.decode({...old.toJson(), 'anchorDay': 151}, 150 * Rules.day),
      throwsFormatException,
    );
  });
  test('seasonal weather and next-day forecast are reproducible, independently inspectable and versioned', () {
    final a = Climate(202), b = Climate(202), outcomes = <WeatherKind>{};
    for (var day = 0; day < 240; day++) {
      final time = day * Rules.day + 1;
      expect(a.at(time), b.at(time));
      expect(a.tomorrow(time), a.at((day + 1) * Rules.day));
      outcomes.add(a.at(time));
      if (a.calendar.season(time) != Season.winter) {
        expect(a.at(time), isNot(WeatherKind.snow));
      }
      final checkpoint = a.toJson(time);
      for (var i = 0; i < 10; i++) {
        a.forecast(time);
        a.toJson(time);
      }
      expect(a.toJson(time), checkpoint);
      expect(
        Climate.decode(
          checkpoint,
          202,
          time,
          EnvironmentPreset.plains,
        ).toJson(time),
        checkpoint,
      );
    }
    expect(outcomes, WeatherKind.values.toSet());
    expect([
      for (var d = 0; d < 24; d++) a.scheduled(d),
    ], isNot([for (var d = 0; d < 24; d++) Climate(203).scheduled(d)]));
  });
  test('four Farm seasons and adverse weather apply once with harvest and a moderate cap', () {
    for (final season in Season.values) {
      for (final weather in WeatherKind.values) {
        final c = Climate(
          2,
          calendar: Calendar(initialSeason: season),
          fixedWeather: weather,
        );
        expect(
          c.farmMultiplier(0),
          closeTo((season.farm * weather.farm).clamp(.65, 1.5), 1e-9),
        );
        expect(
          c.farmMultiplier(0, harvest: true),
          closeTo((season.farm * weather.farm * 1.25).clamp(.65, 1.5), 1e-9),
        );
      }
    }
    expect(
      Climate(
        2,
        calendar: const Calendar(initialSeason: Season.summer),
      ).socialRepeat(0),
      14.4,
    );
  });
  test('season and weather change real productive payments, never assignment or travel pay', () {
    for (final season in Season.values) {
      final c = town();
      c.climate = Climate(
        c.world.seed,
        calendar: Calendar(initialSeason: season),
        fixedWeather: WeatherKind.snow,
      );
      c.nextVisitorAt = 1e9;
      until(
        c,
        () => c.people.values.any(
          (r) =>
              r.activity == Activity.working &&
              c.kind(r.workId) == Place.garden,
        ),
      );
      final r = c.people.values.firstWhere(
        (r) =>
            r.activity == Activity.working && c.kind(r.workId) == Place.garden,
      );
      final before = r.earned, seconds = r.workedSeconds;
      advance(c, 1);
      expect(
        r.earned - before,
        closeTo(
          ((r.workedSeconds - seconds) *
                  Place.garden.income *
                  c.climate.farmMultiplier(c.clock))
              .round(),
          1,
        ),
      );
    }
  });
  test('navigation and actual traversal share max of weather and wet ground; Stone stays useful', () {
    for (final weather in WeatherKind.values) {
      const wet = Lot(path: true, terrain: Terrain.marsh);
      expect(terrainRoadCost(wet, false, weatherCost: weather.travel), 1.5);
      expect(
        terrainRoadCost(
          const Lot(path: true),
          false,
          weatherCost: weather.travel,
        ),
        weather.travel,
      );
      expect(
        terrainRoadCost(
          const Lot(path: true, terrain: Terrain.marsh, road: RoadKind.stone),
          false,
          weatherCost: weather.travel,
        ),
        1 / Rules.stoneSpeed,
      );
      final c = town();
      c.climate = Climate(c.world.seed, fixedWeather: weather);
      final r = c.people.values.first..cell = const Cell(3, 15);
      final ordinary = ordinaryClimate(town());
      expect(
        c.walkingSpeed(r, const Cell(4, 15)) * weather.travel,
        closeTo(ordinary.walkingSpeed(r, const Cell(4, 15)), 1e-9),
      );
    }
  });
  test('day-boundary weather invalidates future routes while keeping underway edge and no teleport', () {
    final c = town();
    c.climate = Climate(202);
    var day = 0;
    while (c.climate.scheduled(day) == c.climate.scheduled(day + 1)) {
      day++;
    }
    c.clock = (day + 1) * Rules.day - .10;
    c.navigation
      ..weatherCost = c.weather.travel
      ..rain = c.raining;
    final r = c.people.values.first
      ..cell = const Cell(2, 15)
      ..x = 2.5
      ..y = 15.5
      ..arrived = true;
    c.go(r, const Cell(18, 15), Activity.returning);
    c.update(.05);
    final frozen = r.segmentSpeed, x = r.x;
    advance(c, .15);
    expect(r.segmentSpeed, frozen);
    expect(r.x, greaterThan(x));
    expect(r.x, lessThan(3.5));
    expect(r.route.length, 1);
    expect(r.replanAfterEdge, isTrue);
  });
  test('new homes have distinct seeded peoples, independent individual traits, stable identity and readable inspection', () {
    final a = town(), b = town();
    expect(
      a.people.values.map((r) => r.peopleKind).toSet(),
      containsAll(PeopleKind.values),
    );
    for (final r in a.people.values) {
      final peer = b.people[r.homeId]!;
      expect(
        (r.peopleKind, r.name, r.bodyId, r.trait, r.palette),
        (peer.peopleKind, peer.name, peer.bodyId, peer.trait, peer.palette),
      );
      expect(r.peopleKind.compatible(r.bodyId), isTrue);
      final game = SettlementGame(a, (_) {})..inspectedResident = r.homeId;
      expect(game.inspection, contains(r.peopleKind.label));
    }
    final saved = a.encode();
    final loaded = Colony.decode(saved);
    expect(loaded.encode(), saved);
    final before = {
      for (final r in a.people.values)
        r.homeId: (r.peopleKind, r.name, r.bodyId, r.trait, r.palette),
    };
    paint(a, Tool.remove, const Cell(2, 14));
    a.session.undo();
    a.reconcile(a.session.world);
    for (final r in a.people.values) {
      expect((
        r.peopleKind,
        r.name,
        r.bodyId,
        r.trait,
        r.palette,
      ), before[r.homeId]);
    }
    expect(Appearance.assignBody('home-1'), 'plain-v1');
  });
  test('Imp work and Djinn leisure are real preferences and Green Thumb remains individual', () {
    final c = town()..nextVisitorAt = 1e9;
    final imp = c.people.values.firstWhere(
      (r) => r.peopleKind == PeopleKind.imp,
    );
    // Isolate an available preference decision; other people retain their jobs later.
    for (final r in c.people.values) {
      r.timer = 100;
    }
    imp
      ..trait = Trait.brisk
      ..arrived = true
      ..activity = Activity.home
      ..timer = 0;
    c.clock = 3;
    c.decide(imp);
    until(c, () => imp.workId != null);
    expect(c.kind(imp.workId), Place.workshop);
    final djinn = c.people.values.firstWhere(
      (r) => r.peopleKind == PeopleKind.djinn,
    );
    until(
      c,
      () =>
          c.kind(djinn.destination) == Place.hall &&
          (djinn.activity == Activity.socialTravel ||
              djinn.activity == Activity.socialising),
    );
    expect(c.kind(djinn.destination), Place.hall);
  });
  test('both visitors enter, walk, perform a real activity, benefit actual listeners and depart', () {
    final c = town();
    var sawMerchantTravel = false, sawStoryTravel = false, sawDeparture = false;
    for (var i = 0; i < 2200; i++) {
      c.update(.05);
      for (final v in c.visits.values) {
        if (v.actor.route.isNotEmpty) {
          if (v.role == VisitorRole.merchant) {
            sawMerchantTravel = true;
          } else {
            sawStoryTravel = true;
          }
        }
        if (v.leaving && v.actor.route.isNotEmpty) sawDeparture = true;
        expect(c.visits.length, lessThanOrEqualTo(2));
        expect(v.actor.route.every(c.world.roadCell), isTrue);
      }
    }
    expect(sawMerchantTravel, isTrue);
    expect(sawStoryTravel, isTrue);
    expect(sawDeparture, isTrue);
    expect(c.guestCompletions.keys, containsAll(['merchant', 'storyteller']));
    expect(c.visitorEarned, 2000);
    expect(
      c.visitHistory
          .firstWhere((h) => h.role == VisitorRole.storyteller)
          .attendees,
      greaterThan(0),
    );
    expect(c.visitHistory.length, 2);
    expect(c.visits, isEmpty);
    expect(c.people.length, 12);
  });
  test('active visitor save/load and optional dialogue do not duplicate rewards, costs or activity', () {
    final a = town();
    until(
      a,
      () => a.visits.values.any(
        (v) =>
            v.role == VisitorRole.storyteller &&
            v.actor.activity == Activity.socialising,
      ),
    );
    final v = a.visits.values.firstWhere(
      (v) => v.role == VisitorRole.storyteller,
    );
    final chat = Conversation(v.id, node: 'visitor');
    final before = a.encode();
    for (var i = 0; i < 30; i++) {
      Dialogues.view(a, chat);
      a.climate.forecast(a.clock);
    }
    expect(a.encode(), before);
    expect(Dialogues.choose(a, chat, 'encore').changed, isTrue);
    expect(a.visitorSpent, 2000);
    expect(Dialogues.choose(a, chat, 'encore').changed, isFalse);
    final snapshot = a.encode(), b = Colony.decode(snapshot);
    expect(b.encode(), snapshot);
    advance(a, 60);
    advance(b, 60);
    expect(b.encode(), a.encode());
    expect(a.visitorSpent, 2000);
    expect(a.visitorEarned, 2000);
  });
  test('removing a venue during a visit stops its activity without free payment and leaves legally', () {
    final c = town();
    until(
      c,
      () => c.visits.values.any(
        (v) =>
            v.role == VisitorRole.merchant &&
            v.actor.activity == Activity.socialising,
      ),
    );
    final v = c.visits.values.firstWhere((v) => v.role == VisitorRole.merchant),
        x = v.actor.x,
        y = v.actor.y;
    paint(c, Tool.remove, c.world.places[v.venue]!);
    expect((v.actor.x, v.actor.y), (x, y));
    expect(v.leaving, isTrue);
    expect(v.completed, isFalse);
    expect(Colony.decode(c.encode()).encode(), c.encode());
    advance(c, 20);
    expect(c.visitorEarned, 0);
    expect(c.visits.containsKey(v.id), isFalse);
  });
  test('cutting and reconnecting a visitor route waits safely with no fictional crossing', () {
    final c = town();
    until(
      c,
      () => c.visits.values.any(
        (v) => v.role == VisitorRole.merchant && v.actor.x > 7.5,
      ),
    );
    final v = c.visits.values.firstWhere((v) => v.role == VisitorRole.merchant);
    paint(c, Tool.remove, const Cell(7, 15));
    final before = c.visitorEarned;
    advance(c, 10);
    expect(c.visitorEarned, before);
    expect(v.actor.route.any((p) => p == const Cell(7, 15)), isFalse);
    expect(Colony.decode(c.encode()).encode(), c.encode());
    paint(c, Tool.path, const Cell(7, 15));
    until(c, () => !c.visits.containsKey(v.id), 4000);
  });
  test('completed visitors recur with durable memory; bounded history and construction Undo stay forward', () {
    final c = town();
    advance(c, 240);
    until(
      c,
      () => c.visits.values.any(
        (v) => (c.guestCompletions[v.role.name] ?? 0) > 0,
      ),
      5000,
    );
    final v = c.visits.values.firstWhere(
      (v) => (c.guestCompletions[v.role.name] ?? 0) > 0,
    );
    expect(c.visitorStory(v), contains('earlier visit'));
    final history = jsonEncode(c.visitHistory.map((h) => h.toJson()).toList()),
        clock = c.clock;
    final earned = c.visitorEarned,
        spent = c.visitorSpent,
        completed = Map.of(c.guestCompletions);
    paint(c, Tool.path, const Cell(18, 15), const Cell(19, 15));
    c.session.undo();
    c.reconcile(c.session.world);
    c.session.redo();
    c.reconcile(c.session.world);
    expect(c.clock, clock);
    expect(jsonEncode(c.visitHistory.map((h) => h.toJson()).toList()), history);
    expect((c.visitorEarned, c.visitorSpent), (earned, spent));
    expect(c.guestCompletions, completed);
    advance(c, 24 * Rules.day);
    expect(c.visitHistory.length, lessThanOrEqualTo(24));
    expect(c.guestCompletions.length, 2);
  });
  test('legacy human checkpoint preserves every old field, event and money; migration never replays time', () {
    final old = jsonDecode(
      File('../../docs/world-builder/evidence/tranche-05/large-85.json')
          .readAsStringSync(),
    );
    final c = Colony.decode(jsonEncode(old)), saved = jsonDecode(c.encode());
    expect(saved['lots'], old['lots']);
    for (final key in (old['colony'] as Map).keys.where(
      (k) => k != 'residents',
    )) {
      expect(saved['colony'][key], old['colony'][key]);
    }
    for (var i = 0; i < old['colony']['residents'].length; i++) {
      final original = old['colony']['residents'][i] as Map,
          now = saved['colony']['residents'][i] as Map;
      expect({for (final key in original.keys) key: now[key]}, original);
      expect(now['people'], 'human-v1');
    }
    expect(c.visits, isEmpty);
    expect(c.visitorEarned, 0);
    expect(c.climate.calendar.seasonDay(c.clock), 1);
    expect(Colony.decode(c.encode()).encode(), c.encode());
  });
  test('climate and visitor malformed imports fail before an existing colony is replaced', () {
    final c = town();
    until(c, () => c.visits.isNotEmpty);
    final saved = c.encode();
    for (final change in <void Function(dynamic)>[
      (d) => d['colony']['climate']['version'] = 99,
      (d) => d['colony']['climate']['next'] = 'imaginary',
      (d) => d['colony']['climate']['calendar']['anchorDay'] = 999999,
      (d) => d['colony']['residents'][0]['people'] = 'unknown',
      (d) => d['colony']['residents'][0]['bodyId'] = 'merchant-body-v1',
      (d) => d['colony']['visitors']['active'][0]['progress'] = 100,
      (d) => d['colony']['visitors']['active'][0]['completed'] = true,
      (d) => d['colony']['visitors']['active'][0]['actor']['route'] = [
        [39, 29],
      ],
      (d) => d['colony']['visitors']['active'].add(
        d['colony']['visitors']['active'][0],
      ),
    ]) {
      final bad = jsonDecode(saved);
      change(bad);
      expect(() => Colony.decode(jsonEncode(bad)), throwsFormatException);
      expect(c.encode(), saved);
    }
  });
  test('render cadence, inspection, cosmetic motion and forecasts do not consume simulation streams', () {
    final a = town(), b = Colony.decode(a.encode());
    final game = SettlementGame(a, (_) {})..reducedMotion = true;
    for (var i = 0; i < 6000; i++) {
      a.update(.025);
      b.update(.05);
      a.update(.025);
      if (i % 50 == 0) {
        a.climate.forecast(a.clock);
        game.inspectedResident = a.people.keys.first;
        game.inspection;
      }
    }
    expect(a.encode(), b.encode());
    expect(a.eventRng, b.eventRng);
    expect(a.specialRng, b.specialRng);
    expect(a.visitorRng, b.visitorRng);
  });
  test('actual actor atlas retains all old frames and distinct selected source masks', () {
    final m = jsonDecode(File('assets/manifest.json').readAsStringSync());
    expect(Appearance.worldBodies.length, 8);
    expect(Appearance.bodies.length, 4);
    expect(m['living_world']['version'], 1);
    for (final body in Appearance.worldBodies) {
      for (var palette = 0; palette < 4; palette++) {
        expect(
          File('assets/images/${Appearance.sprite(body, palette)}.png')
              .existsSync(),
          isTrue,
        );
      }
    }
    expect(File('assets/images/world-actors-atlas.png').existsSync(), isTrue);
  });
}
