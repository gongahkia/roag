import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/climate.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/navigation.dart';
import 'package:roag_settlement/residents.dart';
import 'package:roag_settlement/world_generation.dart';
import 'package:roag_settlement/settlement_game.dart';

import '../tool/goal10_fixture.dart' show floodTown;
import '../tool/goal09_fixture.dart' show baseTown;
import 'colony_test.dart' show advance, paint;
import 'goal09_incident_test.dart' show until;

void main() {
  test('Highland objectives require real work beyond the pass and reject a route cut during danger even after reconnection', () {
    for (final cut in [false, true]) {
      final c =
          Colony(
              LandscapeSession(
                WorldGeneration.create('202', EnvironmentPreset.highlands),
              ),
            )
            ..nextSpecialAt = 1e9
            ..nextVisitorAt = 1e9
            ..nextIncidentAt = 1e9;
      c.configureScenario(ScenarioKind.crossing);
      paint(c, Tool.path, const Cell(7, 15), const Cell(24, 15));
      paint(c, Tool.cottage, const Cell(2, 14), const Cell(7, 14));
      paint(c, Tool.garden, const Cell(22, 14));
      paint(c, Tool.green, const Cell(8, 14));
      advance(c, 10);
      c.startIncident(
        IncidentKind.fire,
        target: c.world.at(const Cell(7, 14)).buildingId,
        fixture: true,
      );
      until(c, () => c.incident?.passageWork == true);
      expect(c.scenarioCompleted.contains(1), true);
      if (cut) {
        paint(c, Tool.remove, const Cell(16, 15));
        expect(c.session.undo(), true);
        c.reconcile(c.session.world);
        expect(c.incident!.passageHeld, false);
      }
      until(c, () => c.incident == null);
      advance(c, 1.1);
      expect(c.scenarioCompleted.contains(2), !cut);
    }
  });
  test('past exposure does not invent participation in a later assistance encounter', () {
    final c = baseTown();
    for (final r in c.people.values) {
      r.exposureUntil = c.clock + 70;
    }
    c.startIncident(IncidentKind.caravan, fixture: true);
    until(c, () => c.incident == null);
    expect(c.stories.last.participants, isEmpty);
  });
  test('a caravan reserves one future Inn place and cannot gain service while all real spaces are occupied', () {
    final c = baseTown();
    final inn = c.places(Place.inn).first.key;
    c.startIncident(IncidentKind.caravan, target: inn, fixture: true);
    expect(c.residentCapacity(inn), 3);
    until(c, () => c.incident?.state == 'grazing');
    final guests = c.people.values
        .where((r) => r.workId != inn)
        .take(4)
        .toList();
    final access = c.world.access(c.world.places[inn]!)!;
    // settled occupancy fixture; subsequent service uses the normal fixed clock.
    for (final r in guests) {
      r.route.clear();
      r.cell = access;
      r.x = access.x + .5;
      r.y = access.y + .5;
      r.activity = Activity.socialising;
      r.destination = inn;
      r.socialProgress = 0;
    }
    final progress = c.incident!.animalProgress;
    advance(c, .5);
    expect(c.incident!.animalProgress, progress);
    for (final r in guests) {
      c.goHome(r);
    }
    advance(c, 1);
    expect(c.incident!.animalProgress, greaterThan(progress));
  });
  test('natural flood eligibility uses prolonged rain, warning, one active owner and real recovery cooldown', () {
    final c = floodTown(start: false);
    advance(c, 429);
    expect(c.incident, isNull);
    c.nextIncidentAt = 450;
    c.incidentRng = 5; // test-only ticket injection; no playable forcing.
    advance(c, 31.1);
    expect(c.incident?.kind, IncidentKind.flood);
    expect(c.incident!.origin, 'scheduled');
    expect(c.incident!.state, 'warning');
    expect(c.startIncident(IncidentKind.raiders, fixture: true), false);
    advance(c, 55);
    expect(c.incident, isNull);
    expect(c.incidentCooldown, greaterThan(c.clock + 500));
    final before = c.incidentHistory.length;
    advance(c, 100);
    expect(c.incidentHistory.length, before);
  });
  test('an isolated trained Watchpost cannot protect an accessible Market; reconnecting restores actual protection', () {
    final c = baseTown(watch: false);
    paint(c, Tool.path, const Cell(13, 18));
    paint(c, Tool.watchpost, const Cell(14, 18));
    advance(c, 20);
    expect(
      c.people.values.any(
        (r) => c.kind(r.workId) == Place.watchpost && c.ready(r),
      ),
      true,
    );
    paint(c, Tool.remove, const Cell(13, 17));
    c.startIncident(IncidentKind.raiders, fixture: true);
    until(c, () => c.incident == null);
    expect(c.incidentHistory.last.responders, 0);
    expect(c.incidentHistory.last.damage, 12);
    paint(c, Tool.path, const Cell(13, 17));
    advance(c, 20);
    c.startIncident(IncidentKind.raiders, fixture: true);
    until(c, () => c.incident == null);
    expect(c.incidentHistory.last.responders, 1);
    expect(c.incidentHistory.last.damage, lessThan(12));
  });
  test('a present but unstaffed Inn never provides assistance or a reward', () {
    final c = Colony(LandscapeSession(Landscape.create()))
      ..nextIncidentAt = 1e9;
    paint(c, Tool.inn, const Cell(5, 14));
    c.startIncident(IncidentKind.caravan, fixture: true);
    advance(c, 120);
    expect(c.incident, isNull);
    expect(c.visitorEarned, 0);
    expect(c.incidentHistory.last.outcome, 'departed without assistance');
  });
  test('serious recovery objectives need real staff and a safe recovered town; achievements cannot refund or duplicate money', () {
    final c = baseTown()..scenario = ScenarioKind.outpost;
    c.startIncident(
      IncidentKind.fire,
      target: c.world.at(const Cell(7, 14)).buildingId,
      fixture: true,
    );
    until(c, () => c.incident == null);
    advance(c, 1);
    expect(c.scenarioCompleted.contains(0), true);
    expect(c.scenarioCompleted.contains(1), true);
    expect(c.scenarioCompleted.contains(2), false);
    final damaged = c.session.conditions.entries
        .where((e) => e.value.health < 100)
        .toList();
    for (final e in damaged) {
      paint(c, Tool.repair, c.buildingCell(e.key)!);
    }
    advance(c, 1.1);
    expect(c.scenarioCompleted, {0, 1, 2});
    final b = Colony.decode(c.encode()), money = b.session.money;
    advance(b, 1);
    expect(b.stories.where((s) => s.kind == 'scenario').length, 3);
    expect(b.session.money - money, lessThan(2000));
    expect(b.stories.any((s) => s.kind == 'recovery'), true);
  });
  test('900 challenging seeds have reproducible terrain, enough clear land and affordable eastward access', () {
    for (final preset in [
      EnvironmentPreset.wetlands,
      EnvironmentPreset.highlands,
      EnvironmentPreset.frontier,
    ]) {
      final unique = <String>{};
      for (var seed = 0; seed < 300; seed++) {
        final w = WorldGeneration.create('$seed', preset),
            c = Colony(LandscapeSession(w));
        expect(w.encode(), WorldGeneration.create('$seed', preset).encode());
        unique.add(w.lots.map((l) => l.terrain.index).join(','));
        expect(w.generation!.version, 3);
        expect(w.roadCell(w.arrival), true);
        expect(
          w.lots.where((l) => l.terrain.buildable).length,
          greaterThan(250),
        );
        final budget = c.session.money;
        paint(c, Tool.path, const Cell(7, 15), const Cell(16, 15));
        if (preset.hasRiver) {
          final water = w.cells
              .where((p) => p.y == 15 && w.at(p).terrain == Terrain.water)
              .toList();
          if (water.first.x - 1 > 16) {
            paint(
              c,
              Tool.path,
              const Cell(16, 15),
              Cell(water.first.x - 1, 15),
            );
          }
          paint(
            c,
            Tool.bridge,
            Cell(water.first.x - 1, 15),
            Cell(water.last.x + 1, 15),
          );
          paint(c, Tool.path, Cell(water.last.x + 1, 15), const Cell(32, 15));
        } else {
          paint(c, Tool.path, const Cell(16, 15), const Cell(32, 15));
        }
        expect(
          c.world.activePaths.contains(const Cell(32, 15)),
          true,
          reason: '$preset $seed',
        );
        expect(budget - c.session.money, lessThanOrEqualTo(9000));
        expect(c.session.preview(Tool.garden, const Cell(3, 14)).valid, true);
        expect(c.session.preview(Tool.green, const Cell(5, 14)).valid, true);
      }
      expect(unique.length, 300);
    }
  });
  test('v2 old presets preserve frozen terrain hashes and loading never regenerates new worlds', () {
    for (final p in [
      EnvironmentPreset.plains,
      EnvironmentPreset.woodlands,
      EnvironmentPreset.river,
    ]) {
      final w = WorldGeneration.create('202', p);
      const frozen = {
        'plains': 1263944736,
        'woodlands': 594241829,
        'river': 1649315164,
      };
      expect(
        w.encode().codeUnits.fold<int>(
          17,
          (a, b) => (a * 131 + b) % 2147483647,
        ),
        frozen[p.name],
      );
      expect(w.generation!.version, 2);
      expect(
        w.lots.where((l) => l.terrain == Terrain.water).length,
        p == EnvironmentPreset.river ? 76 : 0,
      );
    }
    final c = Colony(
      LandscapeSession(
        WorldGeneration.create('snow pass', EnvironmentPreset.highlands),
      ),
    );
    paint(c, Tool.cottage, const Cell(2, 14));
    final d = jsonDecode(c.encode());
    d['generation']['version'] = 999;
    final loaded = Colony.decode(jsonEncode(d));
    expect(
      loaded.world.lots.map((l) => l.toJson()).toList(),
      c.world.lots.map((l) => l.toJson()).toList(),
    );
  });
  test('prolonged wetland rain changes marsh costs once, Stone mitigates and navigation agrees', () {
    final c = Colony(
      LandscapeSession(
        WorldGeneration.create('202', EnvironmentPreset.wetlands),
      ),
    );
    var w = c.world.replace(
      const Cell(8, 15),
      Lot(grass: 0, path: true, terrain: Terrain.marsh),
    );
    c.session.world = w;
    c.reconcile(w);
    c.climate = Climate(w.seed, fixedWeather: WeatherKind.rain);
    advance(c, 20.1);
    expect(c.marshTravel, 2);
    expect(c.navigation.marshCost, 2);
    expect(terrainRoadCost(w.at(const Cell(8, 15)), true, marshCost: 2), 2);
    expect(
      c.navigation.distance(const Cell(7, 15), const Cell(8, 15)),
      closeTo(1.625, 1e-9),
    );
    paint(c, Tool.stoneRoad, const Cell(8, 15));
    expect(c.navigation.marshCost, 2);
    expect(
      terrainRoadCost(c.world.at(const Cell(8, 15)), true, marshCost: 2),
      closeTo(1 / 1.18, 1e-9),
    );
  });
  test('flood warning is inert, contiguous bounded water closes real roads and recedes without regenerating terrain', () {
    final c = floodTown(), original = c.world.toJson(), e = c.incident!;
    expect(e.flood.length, inInclusiveRange(1, 40));
    expect(c.world.closures, isEmpty);
    expect(e.flood.contains(const Cell(7, 15)), true);
    advance(c, 6.5);
    expect(c.world.roadCell(const Cell(7, 15)), false);
    expect(c.navigation.route(const Cell(1, 15), const Cell(14, 15)), isNull);
    expect(c.usable(e.target), false);
    advance(c, 46);
    expect(c.incident, isNull);
    expect(c.world.closures, isEmpty);
    expect(c.world.toJson(), original);
    expect(c.incidentHistory.last.damage, greaterThan(0));
    expect(c.status(e.target)!.health, 82);
    expect(c.usable(e.target), true);
  });
  test('Stone roads retain actual flood access and prepared residents reach real shelter', () {
    final c = floodTown(stone: true);
    advance(c, 7);
    expect(c.world.closures, isEmpty);
    expect(
      c.navigation.route(const Cell(1, 15), const Cell(14, 15)),
      isNotNull,
    );
    advance(c, 15);
    expect(c.incident!.sheltered, isNotEmpty);
    for (final id in c.incident!.sheltered) {
      final r = c.people[id]!;
      expect(r.sheltered, true);
    }
  });
  test('active flood reload, reconstruction and exact forward Undo retain closures and damage', () {
    final c = floodTown();
    advance(c, 14);
    final b = Colony.decode(c.encode());
    expect(b.encode(), c.encode());
    final target = c.incident!.target, health = c.status(target)!.health;
    paint(c, Tool.stoneRoad, const Cell(7, 15));
    expect(c.world.roadCell(const Cell(7, 15)), true);
    expect(c.session.undo(), true);
    c.reconcile(c.session.world);
    expect(c.world.roadCell(const Cell(7, 15)), false);
    expect(c.status(target)!.health, health);
    final d = Colony.decode(c.encode());
    expect(d.encode(), c.encode());
    advance(c, 45);
    advance(d, 45);
    expect(d.encode(), c.encode());
  });
  test('flood target removed while active still recedes safely', () {
    final c = floodTown();
    advance(c, 8);
    paint(c, Tool.remove, c.buildingCell(c.incident!.target)!);
    advance(c, 50);
    expect(c.incident, isNull);
    expect(c.world.closures, isEmpty);
    expect(Colony.decode(c.encode()).encode(), c.encode());
  });
  test('brigands must reach the Market; loss is bounded and actual trained guards reduce it', () {
    final outcomes = <int>[];
    for (final protect in [false, true]) {
      final c = baseTown(watch: protect);
      c.startIncident(IncidentKind.raiders, fixture: true);
      expect(c.incidentSpent, 0);
      advance(c, 6);
      expect(c.incidentSpent, 0);
      until(c, () => c.incident == null);
      outcomes.add(c.incidentHistory.last.damage);
      expect(c.incidentSpent, lessThanOrEqualTo(4000));
      expect(outcomes.last, lessThanOrEqualTo(12));
      if (protect) {
        expect(c.incidentHistory.last.responders, 1);
        expect(c.incidentHistory.last.outcome, 'driven away');
      } else {
        expect(c.incidentSpent, 4000);
        expect(outcomes.last, greaterThan(0));
      }
      final earned = c.incidentSpent;
      advance(c, 20);
      expect(c.incidentSpent, earned);
    }
    expect(outcomes.last, lessThan(outcomes.first));
  });
  test('cut approaches stop theft and unreachable Watchposts provide no fictional response', () {
    final c = baseTown();
    c.startIncident(IncidentKind.raiders, fixture: true);
    paint(c, Tool.remove, const Cell(7, 15));
    advance(c, 30);
    expect(c.incidentSpent, 0);
    expect(c.incident!.damage, 0);
    expect(c.incident!.animal!.x, lessThan(7.5));
    paint(c, Tool.path, const Cell(7, 15));
    until(c, () => c.incident == null);
    expect(c.incidentHistory.last.responders, greaterThan(0));
  });
  test('every new actor and flood checkpoint resumes identically through warnings, edits, activity and retreat', () {
    for (final kind in [
      IncidentKind.flood,
      IncidentKind.raiders,
      IncidentKind.caravan,
    ]) {
      final c = kind == IncidentKind.flood ? floodTown() : baseTown();
      if (c.incident == null) c.startIncident(kind, fixture: true);
      for (var i = 0; i < 240; i++) {
        advance(c, .25);
        final b = Colony.decode(c.encode());
        expect(b.encode(), c.encode(), reason: '$kind phase $i');
        if (i % 30 == 0) {
          advance(c, .5);
          advance(b, .5);
          expect(c.encode(), b.encode());
        }
      }
      expect(c.incident, isNull, reason: '$kind must depart or recede');
    }
  });
  test('caravan receives no reward without actual service, waits through a cut and retains helped memory after departure', () {
    final c = baseTown();
    final inn = c.places(Place.inn).first.key;
    c.startIncident(IncidentKind.caravan, target: inn, fixture: true);
    paint(c, Tool.remove, const Cell(7, 15));
    advance(c, 25);
    expect(c.visitorEarned, 0);
    paint(c, Tool.path, const Cell(7, 15));
    until(c, () => c.incident == null, 5000);
    expect(c.visitorEarned, 2000);
    expect(c.incidentHistory.last.outcome, 'assisted and departed');
    final b = Colony.decode(c.encode());
    advance(b, 20);
    expect(b.visitorEarned, 2000);
    expect(b.stories.last.outcome, contains('assisted'));
  });
  test('an unstaffed Inn cannot award a caravan reward and removal causes legal retreat', () {
    final c = baseTown(inn: false);
    paint(c, Tool.inn, const Cell(14, 17));
    c.startIncident(
      IncidentKind.caravan,
      target: c.places(Place.inn).first.key,
      fixture: true,
    );
    paint(c, Tool.remove, const Cell(14, 17));
    until(c, () => c.incident == null);
    expect(c.visitorEarned, 0);
    expect(c.incidentHistory.last.outcome, 'departed without assistance');
  });
  test('Peaceful keeps destructive incidents off but allows assistance; cooldown and damaged-town recovery defer threats', () {
    final c = baseTown()..incidentMode = IncidentMode.peaceful;
    for (final k in [
      IncidentKind.fire,
      IncidentKind.wildlife,
      IncidentKind.flood,
      IncidentKind.raiders,
    ]) {
      expect(c.startIncident(k, fixture: true), false);
    }
    expect(c.startIncident(IncidentKind.caravan, fixture: true), true);
    until(c, () => c.incident == null);
    expect(c.incidentCooldown, greaterThan(c.clock));
    c.setIncidentMode(IncidentMode.standard);
    expect(c.nextIncidentAt, greaterThanOrEqualTo(c.clock + 450));
  });
  test('scenario selection preserves entered seeds; optional objectives need real activity and persist through Undo and reload', () {
    final c = Colony(
      LandscapeSession(
        WorldGeneration.create('my plateau', EnvironmentPreset.highlands),
      ),
    );
    c.configureScenario(ScenarioKind.crossing);
    expect(c.session.money, 220000);
    expect(c.world.generation!.seedText, 'my plateau');
    c.session.preview(Tool.path, const Cell(8, 15));
    advance(c, 1);
    expect(c.scenarioCompleted, isEmpty);
    paint(c, Tool.path, const Cell(7, 15), const Cell(22, 15));
    advance(c, 1);
    expect(c.scenarioCompleted, {0});
    expect(c.session.undo(), true);
    c.reconcile(c.session.world);
    advance(c, 1);
    expect(c.scenarioCompleted, {0});
    final b = Colony.decode(c.encode());
    expect(b.scenarioCompleted, {0});
    expect(b.stories.where((s) => s.kind == 'scenario').length, 1);
    expect(() => c.configureScenario(ScenarioKind.free), throwsStateError);
  });
  test('the trade scenario cannot complete a merchant objective from caravan pay or previews', () {
    final c = Colony(LandscapeSession(Landscape.create()))
      ..scenario = ScenarioKind.trade
      ..nextVisitorAt = 1e9
      ..nextSpecialAt = 1e9;
    paint(c, Tool.refuge, const Cell(5, 14));
    c.startIncident(IncidentKind.caravan, fixture: true);
    until(c, () => c.incident == null);
    advance(c, 1);
    expect(c.scenarioCompleted.contains(1), false);
    expect(c.scenarioCompleted.contains(2), false);
  });
  test('schema-5 old towns migrate to Free Build without resetting incident choice, identities or concrete terrain', () {
    final text = File('test/fixtures/goal09-town.json').readAsStringSync();
    final old = jsonDecode(text),
        c = Colony.decode(text),
        now = jsonDecode(c.encode());
    expect(old['schema'], 5);
    expect(now['schema'], 6);
    for (final key in ['lots', 'seed', 'generation', 'nextCottageId']) {
      expect(now[key], old[key]);
    }
    for (final key in [
      'clock',
      'money',
      'residents',
      'preparedness',
      'climate',
      'visitors',
    ]) {
      if (key == 'preparedness') continue;
      expect(now['colony'][key], old['colony'][key]);
    }
    expect(c.incidentMode, IncidentMode.standard);
    expect(c.scenario, ScenarioKind.free);
    expect(c.stories, isEmpty);
  });
  test('malformed flooding, future scenarios and contradictory active encounter imports reject atomically', () {
    final c = floodTown();
    advance(c, 8);
    final text = c.encode();
    for (final mutate in <void Function(dynamic)>[
      (d) => d['colony']['emergence']['scenario'] = 'unavailable',
      (d) => d['colony']['emergence']['completed'] = [0],
      (d) => d['colony']['preparedness']['active']['floodSource'] = [0, 0],
      (d) => d['colony']['preparedness']['active']['flood'].add([39, 29]),
      (d) => d['colony']['preparedness']['active']['losses'] = {'home-1': 99},
    ]) {
      final d = jsonDecode(text);
      mutate(d);
      expect(() => Colony.decode(jsonEncode(d)), throwsFormatException);
      expect(c.encode(), text);
    }
  });
  test('inspection and render cadence cannot reroll incidents or alter continuation; bounded persistent histories', () {
    final a = baseTown()..scenario = ScenarioKind.outpost;
    a.startIncident(IncidentKind.raiders, fixture: true);
    final b = Colony.decode(a.encode());
    final game = SettlementGame(a, (_) {});
    for (var i = 0; i < 600; i++) {
      game.inspectedResident = a.people.keys.first;
      game.inspection;
      a.update(.025);
      a.update(.025);
      b.update(.05);
    }
    expect(a.encode(), b.encode());
    for (var i = 0; i < 100; i++) {
      a.remember('crossing', 'Fact $i', a.world.arrival);
    }
    expect(a.stories.length, 80);
    expect(Colony.decode(a.encode()).stories.length, 80);
  });
}
