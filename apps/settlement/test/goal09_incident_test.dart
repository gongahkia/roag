import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/climate.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/residents.dart';

import 'colony_test.dart' show paint, advance;

Colony prepared({
  bool firehouse = true,
  bool watch = true,
  bool refuge = true,
}) {
  final c = Colony(LandscapeSession(Landscape.create(202)))
    ..nextSpecialAt = 1e9
    ..nextVisitorAt = 1e9;
  c.climate = Climate(
    202,
    calendar: const Calendar(initialSeason: Season.summer),
    fixedWeather: WeatherKind.clear,
  );
  paint(c, Tool.path, const Cell(7, 15), const Cell(18, 15));
  paint(c, Tool.cottage, const Cell(2, 14), const Cell(7, 14));
  paint(c, Tool.cottage, const Cell(2, 16), const Cell(7, 16));
  paint(c, Tool.garden, const Cell(8, 14));
  paint(c, Tool.workshop, const Cell(10, 14));
  paint(c, Tool.market, const Cell(12, 14));
  paint(c, Tool.hall, const Cell(14, 14));
  if (firehouse) paint(c, Tool.firehouse, const Cell(10, 16));
  if (watch) paint(c, Tool.watchpost, const Cell(12, 16));
  if (refuge) paint(c, Tool.refuge, const Cell(14, 16));
  advance(c, 20);
  c.incidentMode = IncidentMode.standard;
  c.nextIncidentAt = 1e9;
  return c;
}

void until(Colony c, bool Function() f, [int ticks = 3000]) {
  for (var i = 0; i < ticks && !f(); i++) {
    c.update(.05);
  }
  expect(f(), true);
}

void main() {
  test(
    'actual guard work consumes a repaired Watchpost charge before Undo',
    () {
      final c = prepared(), station = c.places(Place.watchpost).first;
      c.startIncident(IncidentKind.wildlife, fixture: true);
      advance(c, 1);
      expect(c.people.values.any((r) => r.safetyRole == 'guard'), true);
      c.damageBuilding(station.key, 10);
      c.session.beginStroke(Tool.repair, station.value);
      expect(c.session.finishStroke(), true);
      c.reconcile(c.world);
      final productive = c.status(station.key)!.productive;
      until(c, () => (c.incident?.shoo ?? 0) > .05);
      expect(c.status(station.key)!.productive, greaterThan(productive));
      final paid = c.session.money;
      expect(c.session.undo(), false);
      expect(c.session.money, paid);
      expect(c.status(station.key)!.health, 100);
    },
  );
  test('Undo of an unusable home repair retains real displacement and exact continuation', () {
    final c = prepared(firehouse: false),
        home = c.world.at(const Cell(2, 14)).buildingId!,
        person = c.people[c.world.at(const Cell(2, 14)).buildingId]!;
    c.startIncident(IncidentKind.fire, target: home, fixture: true);
    until(c, () => c.incident == null);
    expect(c.homeUsable(person), false);
    c.session.beginStroke(Tool.repair, const Cell(2, 14));
    expect(c.session.finishStroke(), true);
    c.reconcile(c.world);
    expect(c.session.undo(), true);
    c.reconcile(c.world);
    advance(c, .1);
    expect(person.safetyRole, 'evacuating');
    expect(person.safetyIncident, isNull);
    expect(person.activity, Activity.sheltering);
    expect(c.incidentAccess(person.safetyTarget, person.cell), person.cell);
    expect(person.feedback.name, 'sheltered');
    expect(c.homeUsable(person), false);
    final b = Colony.decode(c.encode());
    expect(b.encode(), c.encode());
    advance(c, 2);
    advance(b, 2);
    expect(b.encode(), c.encode());
    expect(c.people[home], same(person));
    final invalid = jsonDecode(c.encode());
    invalid['colony']['preparedness']['conditions'][home]['health'] = 100;
    expect(() => Colony.decode(jsonEncode(invalid)), throwsFormatException);
    c.session.beginStroke(Tool.repair, const Cell(2, 14));
    expect(c.session.finishStroke(), true);
    c.reconcile(c.world);
    expect(person.safetyRole, isNull);
    expect(Colony.decode(c.encode()).encode(), c.encode());
  });

  test(
    'burning relocation is refused without changing identity, money or layout',
    () {
      final c = prepared(), target = c.world.at(const Cell(2, 14)).buildingId!;
      c.startIncident(IncidentKind.fire, target: target, fixture: true);
      final before = c.world.encode(), money = c.session.money;
      expect(
        c.session.relocate(
          evaluateMove(c.world, const Cell(2, 14), const Cell(16, 14)),
        ),
        false,
      );
      expect(c.world.encode(), before);
      expect(c.session.money, money);
      expect(c.people.containsKey(target), true);
    },
  );

  test('protective buildings attract actual automatic staff and preserve identities', () {
    final c = prepared();
    expect(
      c.people.values.where((r) => c.kind(r.workId) == Place.firehouse).length,
      2,
    );
    expect(
      c.people.values.where((r) => c.kind(r.workId) == Place.watchpost).length,
      1,
    );
    expect(c.people.values.where(c.ready).length, 3);
    expect(c.people.length, 12);
  });
  test('staffed firefighters travel, actually suppress, and reduce comparable fire damage', () {
    final a = prepared(),
        b = prepared(firehouse: false),
        target = a.world.at(const Cell(2, 14)).buildingId!;
    expect(
      a.startIncident(IncidentKind.fire, target: target, fixture: true),
      true,
    );
    expect(
      b.startIncident(IncidentKind.fire, target: target, fixture: true),
      true,
    );
    var travel = false, working = false;
    for (var i = 0; i < 1200; i++) {
      a.update(.05);
      b.update(.05);
      travel |= a.people.values.any(
        (r) => r.activity == Activity.responseTravel,
      );
      working |= a.people.values.any((r) => r.activity == Activity.responding);
    }
    expect(travel, true);
    expect(working, true);
    expect(a.incident, isNull);
    expect(b.incident, isNull);
    expect(a.incidentHistory.single.responders, greaterThan(0));
    expect(
      a.incidentHistory.single.damage,
      lessThan(b.incidentHistory.single.damage),
    );
    expect(b.incidentHistory.single.buildings.length, lessThanOrEqualTo(3));
    expect(a.people.length, 12);
    expect(b.people.length, 12);
  });
  test('fire damage is real, safe repair restores original home and no resident disappears', () {
    final c = prepared(firehouse: false),
        target = c.world.at(const Cell(2, 14)).buildingId!,
        person = c.people[target]!;
    c.startIncident(IncidentKind.fire, target: target, fixture: true);
    c.session.beginStroke(Tool.repair, const Cell(2, 14));
    expect(c.session.finishStroke(), false);
    until(c, () => c.incident == null);
    expect(c.status(target)!.health, lessThan(100));
    expect(c.people[target], same(person));
    c.session.beginStroke(Tool.repair, const Cell(2, 14));
    expect(c.session.finishStroke(), true);
    c.reconcile(c.session.world);
    expect(c.status(target)!.health, 100);
    expect(c.homeUsable(person), true);
  });
  test('watch guards reach a real rabbit and reduce actual crop damage', () {
    final a = prepared(),
        b = prepared(watch: false),
        farm = a.places(Place.garden).first.key;
    a.startIncident(IncidentKind.wildlife, target: farm, fixture: true);
    b.startIncident(IncidentKind.wildlife, target: farm, fixture: true);
    var animalMoved = false, guardMoved = false;
    for (var i = 0; i < 2000; i++) {
      a.update(.05);
      b.update(.05);
      animalMoved |= (a.incident?.animal?.x ?? .5) > .7;
      guardMoved |= a.people.values.any(
        (r) => r.safetyRole == 'guard' && r.moving,
      );
    }
    expect(animalMoved, true);
    expect(guardMoved, true);
    expect(a.incident, isNull);
    expect(b.incident, isNull);
    expect(a.incidentHistory.single.outcome, 'driven away');
    expect(
      a.incidentHistory.single.damage,
      lessThan(b.incidentHistory.single.damage),
    );
    expect(b.incidentHistory.single.damage, greaterThan(0));
  });
  test(
    'active fire and real response resume exactly from schema-5 checkpoints',
    () {
      final a = prepared();
      a.startIncident(
        IncidentKind.fire,
        target: a.world.at(const Cell(2, 14)).buildingId,
        fixture: true,
      );
      advance(a, 7);
      final checkpoint = a.encode(), b = Colony.decode(checkpoint);
      expect(b.encode(), checkpoint);
      advance(a, 30);
      advance(b, 30);
      expect(b.encode(), a.encode());
    },
  );
  test(
    'active wildlife movement and grazing restore without duplicate damage',
    () {
      final a = prepared(watch: false);
      a.startIncident(IncidentKind.wildlife, fixture: true);
      advance(a, 8);
      final b = Colony.decode(a.encode());
      expect(b.encode(), a.encode());
      advance(a, 25);
      advance(b, 25);
      expect(b.encode(), a.encode());
    },
  );
  test('Peaceful forbids new destruction; old colony data migrates healthy and opted out', () {
    final c = prepared();
    c.setIncidentMode(IncidentMode.peaceful);
    expect(c.startIncident(IncidentKind.fire, fixture: true), false);
    final old = jsonDecode(c.encode())..['schema'] = 4;
    (old['colony'] as Map).remove('preparedness');
    final b = Colony.decode(jsonEncode(old));
    expect(b.incidentMode, IncidentMode.peaceful);
    expect(b.session.conditions, isEmpty);
    expect(b.people.length, c.people.length);
    expect(b.session.money, c.session.money);
    b.setIncidentMode(IncidentMode.standard);
    expect(b.nextIncidentAt, greaterThanOrEqualTo(b.clock + 450));
  });
  test('actual work and home returns develop a building without changing its capacity', () {
    final c = prepared(),
        farm = c.places(Place.garden).first.key,
        capacity = c.kind(farm)!.workCapacity;
    advance(c, 150);
    expect(c.status(farm)!.developed, true);
    expect(c.kind(farm)!.workCapacity, capacity);
    expect(Colony.decode(c.encode()).encode(), c.encode());
  });
}
