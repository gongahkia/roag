import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/climate.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/dialogue.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/residents.dart';
import 'package:roag_settlement/settlement_game.dart';

import 'colony_test.dart' show advance, paint;
import 'goal09_incident_test.dart' show prepared, until;
import 'goal08_test.dart' show town;
import 'goal07_test.dart' show crossingTown, riverSpan;

void subset(dynamic actual, dynamic old) {
  if (old is Map) {
    for (final k in old.keys) {
      subset(actual[k], old[k]);
    }
  } else if (old is List) {
    expect(actual.length, old.length);
    for (var i = 0; i < old.length; i++) {
      subset(actual[i], old[i]);
    }
  } else {
    expect(actual, old);
  }
}

void main() {
  test('active checkpoints survive every phase and repeated terrain edits without stale actor requests', () {
    for (final kind in IncidentKind.values) {
      final c = prepared();
      c.startIncident(kind, fixture: true);
      for (var i = 0; i < 120; i++) {
        advance(c, .25);
        if (i % 12 == 0) {
          final cut = const Cell(6, 15);
          if (c.world.at(cut).path) {
            paint(c, Tool.remove, cut);
          } else {
            paint(c, Tool.path, cut);
          }
        }
        final loaded = Colony.decode(c.encode());
        expect(loaded.encode(), c.encode(), reason: '$kind tick $i');
        expect(c.planning.map((p) => p.id).toSet().length, c.planning.length);
      }
    }
  });

  test(
    'unemployed Firehouse has no suppression and town-wide response is bounded',
    () {
      final empty = Colony(LandscapeSession(Landscape.create(202)))
        ..incidentMode = IncidentMode.standard
        ..nextIncidentAt = 1e9
        ..nextVisitorAt = 1e9
        ..nextSpecialAt = 1e9;
      paint(empty, Tool.firehouse, const Cell(2, 14));
      paint(empty, Tool.garden, const Cell(4, 14));
      empty.startIncident(IncidentKind.fire, fixture: true);
      advance(empty, 50);
      expect(empty.incidentHistory.single.responders, 0);
      expect(empty.incidentHistory.single.damage, greaterThan(0));
      final c = prepared();
      paint(c, Tool.firehouse, const Cell(16, 16));
      paint(c, Tool.firehouse, const Cell(18, 16));
      advance(c, 10);
      expect(
        c.people.values
            .where((r) => c.kind(r.workId) == Place.firehouse)
            .length,
        6,
      );
      c.startIncident(IncidentKind.fire, fixture: true);
      advance(c, 1);
      expect(c.incident!.dispatched.length, lessThanOrEqualTo(4));
      expect(c.incident!.dispatched, hasLength(4));
    },
  );
  test('Watchpost coverage follows weighted road distance rather than visual proximity', () {
    final c = prepared(), id = c.places(Place.watchpost).first.key;
    paint(c, Tool.path, const Cell(18, 14));
    expect(
      c.session.relocate(
        evaluateMove(c.world, c.world.places[id]!, const Cell(18, 13)),
      ),
      true,
    );
    c.reconcile(c.session.world);
    expect(
      c.travelDistance(
        c.placeAccess(id)!,
        c.placeAccess(c.places(Place.garden).first.key)!,
      ),
      greaterThan(10),
    );
    c.startIncident(IncidentKind.wildlife, fixture: true);
    advance(c, 40);
    expect(c.incidentHistory.single.responders, 0);
    expect(c.incidentHistory.single.damage, greaterThan(0));
  });
  test('operational damage reduces actual Farm pay and Market service capacity exactly once', () {
    final a = prepared(), farm = a.places(Place.garden).first.key;
    until(
      a,
      () => a.people.values.any(
        (r) => r.workId == farm && r.activity == Activity.working,
      ),
    );
    final b = Colony.decode(a.encode()),
        market = a.places(Place.market).first.key;
    b.damageBuilding(farm, 10);
    b.damageBuilding(market, 10);
    expect(b.status(farm)!.output, .6);
    expect(b.capacity(market), 1);
    final ra = a.people.values.firstWhere(
          (r) => r.workId == farm && r.activity == Activity.working,
        ),
        rb = b.people[ra.homeId]!;
    final pa = ra.earned, pb = rb.earned;
    advance(a, 1);
    advance(b, 1);
    expect((rb.earned - pb) / (ra.earned - pa), closeTo(.6, .02));
    b.damageBuilding(market, 60);
    expect(b.capacity(market), 0);
    expect(b.staffed(market), false);
    expect(Colony.decode(b.encode()).encode(), b.encode());
  });
  test(
    'healthy versus damaged Firehouse changes actual suppression capability',
    () {
      final a = prepared(),
          b = Colony.decode(a.encode()),
          station = b.places(Place.firehouse).first.key,
          target = a.world.at(const Cell(2, 14)).buildingId!;
      b.damageBuilding(station, 10);
      for (final c in [a, b]) {
        c.startIncident(IncidentKind.fire, target: target, fixture: true);
      }
      advance(a, 12);
      advance(b, 12);
      final healthy = a.incidentHistory.isNotEmpty
          ? a.incidentHistory.single.ends
          : a.clock + 100;
      final damaged = b.incidentHistory.isNotEmpty
          ? b.incidentHistory.single.ends
          : b.clock + 100;
      expect(damaged, greaterThan(healthy));
    },
  );

  test('rain limits fire spread and damage; all dry fire footprints remain bounded', () {
    final dry = prepared(firehouse: false), wet = prepared(firehouse: false);
    wet.climate = Climate(
      202,
      calendar: const Calendar(initialSeason: Season.summer),
      fixedWeather: WeatherKind.rain,
    );
    for (final c in [dry, wet]) {
      c.startIncident(
        IncidentKind.fire,
        target: c.world.at(const Cell(2, 14)).buildingId,
        fixture: true,
      );
      advance(c, 60);
      expect(c.incident, isNull);
      expect(c.incidentHistory.single.damage, lessThanOrEqualTo(210));
      expect(c.incidentHistory.single.buildings.length, lessThanOrEqualTo(3));
    }
    expect(wet.incidentHistory.single.buildings.length, 1);
    expect(
      wet.incidentHistory.single.damage,
      lessThan(dry.incidentHistory.single.damage),
    );
  });
  test('empty and disconnected stations supply no remote fire protection', () {
    final c = prepared();
    final station = c.places(Place.firehouse).first;
    paint(c, Tool.remove, const Cell(10, 15));
    expect(c.world.access(station.value), isNull);
    expect(
      c.people.values.where(c.ready).where((r) => r.workId == station.key),
      isEmpty,
    );
    c.startIncident(
      IncidentKind.fire,
      target: c.world.at(const Cell(2, 14)).buildingId,
      fixture: true,
    );
    advance(c, 60);
    expect(c.incidentHistory.single.responders, 0);
    expect(c.incidentHistory.single.damage, greaterThan(0));
  });
  test(
    'real refuge arrivals are bounded and confer shelter only after travel',
    () {
      final c = prepared(firehouse: false);
      c.startIncident(
        IncidentKind.fire,
        target: c.world.at(const Cell(5, 14)).buildingId,
        fixture: true,
      );
      var travelled = false, sheltered = false;
      for (var i = 0; i < 1000; i++) {
        c.update(.05);
        for (final r in c.people.values) {
          if (r.activity == Activity.shelterTravel) {
            travelled = true;
            expect(r.sheltered, false);
          }
          if (r.activity == Activity.sheltering) {
            sheltered = true;
            expect(c.incidentAccess(r.safetyTarget, r.cell), r.cell);
            expect(r.sheltered, true);
          }
        }
        for (final p in c.places(Place.refuge)) {
          expect(
            c.shelterReservations(p.key),
            lessThanOrEqualTo(c.shelterCapacity(p.key)),
          );
        }
      }
      expect(travelled, true);
      expect(sheltered, true);
      expect(c.people.length, 12);
      expect(c.incidentHistory.single.sheltered, greaterThan(0));
      expect(Colony.decode(c.encode()).encode(), c.encode());
    },
  );
  test('road edits interrupt wildlife without imaginary arrival or crop damage; reconnection resumes', () {
    final c = prepared(watch: false);
    c.startIncident(IncidentKind.wildlife, fixture: true);
    paint(c, Tool.remove, const Cell(5, 15));
    advance(c, 25);
    expect(c.incident!.state, 'approaching');
    expect(c.incident!.damage, 0);
    expect(c.incident!.animal!.x, lessThan(5.5));
    expect(Colony.decode(c.encode()).encode(), c.encode());
    paint(c, Tool.path, const Cell(5, 15));
    until(c, () => c.incident == null);
    expect(c.incidentHistory.single.damage, greaterThan(0));
  });
  test('responders respect a broken real river crossing and retain position across reload', () {
    final c = crossingTown();
    paint(c, Tool.cottage, const Cell(6, 14), const Cell(7, 14));
    paint(c, Tool.firehouse, const Cell(9, 14));
    advance(c, 25);
    c.incidentMode = IncidentMode.standard;
    c.nextIncidentAt = 1e9;
    c.nextVisitorAt = 1e9;
    final farm = c.places(Place.garden).first.key, span = riverSpan(c.world);
    expect(c.people.values.where(c.ready), isNotEmpty);
    c.startIncident(IncidentKind.fire, target: farm, fixture: true);
    advance(c, 1);
    final cut = Cell(span.first.x + 1, 15);
    paint(c, Tool.remove, cut);
    final b = Colony.decode(c.encode());
    advance(c, 12);
    advance(b, 12);
    expect(b.encode(), c.encode());
    expect(c.incident?.responders ?? {}, isEmpty);
    expect(
      c.people.values
          .where((r) => r.safetyRole == 'fire')
          .every((r) => r.x <= span.first.x + 1.5),
      true,
    );
    paint(c, Tool.bridge, span.first, span.last);
    until(c, () => c.incident == null);
    expect(c.people.length, c.world.cottages.length);
  });
  test('condition follows damaged relocation, layout history and persistence without restored health', () {
    final c = prepared(),
        id = c.places(Place.market).first.key,
        from = c.world.places[id]!;
    c.damageBuilding(id, 70);
    expect(c.capacity(id), 0);
    expect(c.usable(id), false);
    final move = evaluateMove(c.world, from, const Cell(16, 14));
    expect(c.session.relocate(move), true);
    c.reconcile(c.session.world);
    expect(c.world.places[id], const Cell(16, 14));
    expect(c.status(id)!.health, 30);
    c.session.undo();
    c.reconcile(c.session.world);
    expect(c.status(id)!.health, 30);
    expect(c.world.places[id], from);
    expect(Colony.decode(c.encode()).encode(), c.encode());
  });
  test(
    'naturally scheduled incidents honor grace, independent RNG and cooldown',
    () {
      final c = prepared();
      c.nextIncidentAt = 450;
      c.clock = 449.9;
      advance(c, .05);
      expect(c.incident, isNull);
      final b = Colony.decode(c.encode()), rng = c.incidentRng;
      final game = SettlementGame(c, (_) {})
        ..inspectedResident = c.people.keys.first;
      for (var i = 0; i < 50; i++) {
        game.inspection;
        Dialogues.view(
          c,
          Conversation(c.people.keys.first, node: 'preparedness'),
        );
      }
      expect(c.incidentRng, rng);
      expect(c.encode(), b.encode());
      advance(c, 45);
      advance(b, 45);
      expect(c.encode(), b.encode());
      expect(c.incidentHistory.single.origin, 'scheduled');
      expect(c.nextIncidentAt, greaterThan(c.clock + 500));
    },
  );
  test('presentation/cadence and offscreen inspection do not change incident outcomes', () {
    final a = prepared(), b = Colony.decode(a.encode());
    for (final c in [a, b]) {
      c.startIncident(IncidentKind.wildlife, fixture: true);
    }
    final game = SettlementGame(a, (_) {})..reducedMotion = true;
    for (var i = 0; i < 600; i++) {
      a.update(.05);
      game.inspectedResident = a.people.keys.first;
      game.inspection;
      b.update(.02);
      b.update(.03);
    }
    expect(b.encode(), a.encode());
  });
  test('malformed incident imports are refused and leave existing settlement untouched', () {
    final c = prepared();
    c.startIncident(IncidentKind.fire, fixture: true);
    advance(c, 8);
    final before = c.encode();
    for (final corrupt in <void Function(Map)>[
      (d) => d['colony']['preparedness']['rng'] = 0,
      (d) => d['colony']['preparedness']['mode'] = 'hostile',
      (d) =>
          d['colony']['preparedness']['conditions'].values.first['health'] = -1,
      (d) => d['colony']['preparedness']['active']['fires'][0]['heat'] = 1e9,
      (d) => d['colony']['preparedness']['active']['responders'] = ['invented'],
    ]) {
      final data = jsonDecode(before) as Map;
      corrupt(data);
      expect(() => Colony.decode(jsonEncode(data)), throwsFormatException);
      expect(c.encode(), before);
    }
  });
  test('legacy Goal08 checkpoint preserves identities, economy, land and visitor history exactly', () {
    final path = File('test/fixtures/goal08-town.json');
    if (!path.existsSync()) {
      fail(
        'Generate the baseline Goal08 fixture before this local compatibility check.',
      );
    }
    final data = jsonDecode(path.readAsStringSync()),
        c = Colony.decode(jsonEncode(data)),
        out = jsonDecode(c.encode());
    expect(c.incidentMode, IncidentMode.peaceful);
    for (final key in ['lots', 'seed', 'generation']) {
      expect(out[key], data[key]);
    }
    for (final key in [
      'money',
      'earned',
      'spent',
      'eventRng',
      'specialRng',
      'climate',
    ]) {
      expect(out['colony'][key], data['colony'][key]);
    }
    subset(out['colony']['visitors'], data['colony']['visitors']);
    final old = data['colony']['residents'] as List,
        now = out['colony']['residents'] as List;
    for (var i = 0; i < old.length; i++) {
      for (final key in old[i].keys) {
        expect(now[i][key], old[i][key]);
      }
    }
  });
  test('staffed Inn hosts real post-visit rest without repeating trade rewards and resumes exactly', () {
    final c = town();
    paint(c, Tool.inn, const Cell(10, 16));
    until(
      c,
      () => c.visits.values.any(
        (v) => v.inn != null && v.actor.activity == Activity.socialising,
      ),
    );
    final inn = c.places(Place.inn).first.key;
    expect(c.jobs(inn), 1);
    expect(c.capacity(inn), 4);
    final v = c.visits.values.firstWhere(
      (v) => v.inn != null && v.actor.activity == Activity.socialising,
    );
    expect(v.completed, true);
    expect(c.placeAccess(inn), v.actor.cell);
    final b = Colony.decode(c.encode());
    expect(b.encode(), c.encode());
    advance(c, 25);
    advance(b, 25);
    expect(b.encode(), c.encode());
    expect(c.visitorEarned, 2000);
    expect(c.session.condition(inn).uses, greaterThan(0));
    expect(c.visitorCooldown[v.role], lessThan(v.starts + 150));
  });
  test('removed Inn sends resting guests home, without duplicate income or population', () {
    final c = town();
    paint(c, Tool.inn, const Cell(10, 16));
    until(
      c,
      () => c.visits.values.any(
        (v) => v.inn != null && v.actor.activity == Activity.socialising,
      ),
    );
    paint(c, Tool.remove, c.places(Place.inn).first.value);
    advance(c, 40);
    expect(c.visitorEarned, 2000);
    expect(c.people.length, 12);
    expect(Colony.decode(c.encode()).encode(), c.encode());
  });
}
