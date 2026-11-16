part of 'colony.dart';

class _SafetyReader {
  final Colony colony;
  _SafetyReader(this.colony);
  Never bad() =>
      throw const FormatException('Invalid preparedness checkpoint.');
  Map map(dynamic d) {
    if (d is! Map) bad();
    return d;
  }

  List list(dynamic d, [int max = 20000]) {
    if (d is! List || d.length > max) bad();
    return d;
  }

  double number(Map d, String key, double min, double max) {
    final n = d[key];
    if (n is! num || !n.isFinite || n < min || n > max) bad();
    return n.toDouble();
  }

  int integer(Map d, String key, [int min = 0, int max = 1000000000]) {
    final n = d[key];
    if (n is! int || n < min || n > max) bad();
    return n;
  }

  bool flag(Map d, String key) {
    final n = d[key];
    if (n is! bool) bad();
    return n;
  }

  String id(dynamic v) {
    if (v is! String || !RegExp(r'^[A-Za-z0-9_-]{1,64}$').hasMatch(v)) bad();
    return v;
  }

  String building(dynamic v) {
    final key = id(v);
    if (colony.buildingCell(key) != null) return key;
    final legacy = RegExp(r'^legacy-([0-9]+)-([0-9]+)$').firstMatch(key);
    if (legacy != null &&
        colony.world.contains(
          Cell(int.parse(legacy[1]!), int.parse(legacy[2]!)),
        )) {
      return key;
    }
    final m = RegExp(r'^([A-Za-z]+)-([1-9][0-9]*)$').firstMatch(key);
    if (m == null ||
        !['home', ...Place.values.map((p) => p.name)].contains(m[1]) ||
        int.parse(m[2]!) >= colony.world.nextCottageId) {
      bad();
    }
    return key;
  }

  Cell cell(dynamic v) {
    final a = list(v, 2);
    if (a.length != 2 || a[0] is! int || a[1] is! int) bad();
    final c = Cell(a[0], a[1]);
    if (!colony.world.contains(c)) bad();
    return c;
  }

  Set<String> identities(dynamic d) {
    final values = list(d);
    final ids = values.map(id).toSet();
    if (ids.length != values.length) bad();
    return ids;
  }

  String incidentId(dynamic v, int next) {
    final key = id(v), m = RegExp(r'^incident-([1-9][0-9]*)$').firstMatch(key);
    if (m == null || int.parse(m[1]!) >= next) bad();
    return key;
  }
}

extension PreparednessCodec on Colony {
  Map<String, Object?> preparednessJson() => {
    'version': 1,
    'mode': incidentMode.name,
    'rng': incidentRng,
    'nextId': nextIncidentId,
    'nextAt': nextIncidentAt,
    'cooldown': incidentCooldown,
    'nextTick': nextIncidentTick,
    'conditions': {
      for (final e in session.conditions.entries) e.key: e.value.toJson(),
    },
    'history': incidentHistory.map((h) => h.toJson()).toList(),
    'active': incident == null
        ? null
        : {
            ...incident!.toJson(),
            'lastTick': incident!.lastTick,
            'grazingAt': incident!.grazingAt,
            'dispatched': incident!.dispatched.toList(),
            'animal': incident!.animal == null
                ? null
                : _personJson(incident!.animal!),
          },
  };
  void restorePreparedness(dynamic data) {
    final r = _SafetyReader(this), d = r.map(data);
    if (d['version'] != 1) r.bad();
    incidentMode = IncidentMode.values.firstWhere(
      (m) => m.name == d['mode'],
      orElse: r.bad,
    );
    incidentRng = r.integer(d, 'rng', 1, 2147483646);
    nextIncidentId = r.integer(d, 'nextId', 1);
    nextIncidentAt = r.number(d, 'nextAt', 0, 1e12);
    incidentCooldown = r.number(d, 'cooldown', 0, 1e12);
    nextIncidentTick = r.number(d, 'nextTick', 0, clock + 1.01);
    final records = r.map(d['conditions']);
    if (records.length > 50000) r.bad();
    for (final entry in records.entries) {
      final key = r.building(entry.key), v = r.map(entry.value);
      final c = BuildingCondition(
        health: r.integer(v, 'health', 0, 100),
        damageRevision: r.integer(v, 'damageRevision'),
        repairs: r.integer(v, 'repairs'),
        uses: r.integer(v, 'uses'),
        productive: r.number(v, 'productive', 0, 1e12),
        repairedAt: r.number(v, 'repairedAt', -1, clock),
        scorched: r.flag(v, 'scorched'),
      );
      if (c.health < 100 && c.damageRevision == 0) r.bad();
      session.conditions[key] = c;
    }
    final historyIds = <String>{};
    for (final item in r.list(d['history'], 24)) {
      final h = r.map(item), id = r.incidentId(h['id'], nextIncidentId);
      if (!historyIds.add(id) ||
          !IncidentKind.values.any((k) => k.name == h['kind']) ||
          ![
            'contained',
            'burned out',
            'driven away',
            'retreated',
            'site removed',
          ].contains(h['outcome']) ||
          !['fixture', 'scheduled'].contains(h['origin'])) {
        r.bad();
      }
      final starts = r.number(h, 'starts', 0, clock),
          ends = r.number(h, 'ends', starts, clock),
          buildings = r.list(h['buildings'], 3).map(r.building).toList();
      if (buildings.isEmpty || buildings.toSet().length != buildings.length) {
        r.bad();
      }
      incidentHistory.add(
        IncidentMemory(
          id,
          h['kind'],
          h['outcome'],
          h['origin'],
          starts,
          ends,
          r.integer(h, 'damage', 0, 210),
          r.integer(h, 'responders', 0, 4),
          r.integer(h, 'sheltered', 0, 20000),
          buildings,
        ),
      );
    }
    if (d['active'] == null) return;
    final a = r.map(d['active']), id = r.incidentId(a['id'], nextIncidentId);
    if (historyIds.contains(id) ||
        !['fixture', 'scheduled'].contains(a['origin'])) {
      r.bad();
    }
    final kind = IncidentKind.values.firstWhere(
      (k) => k.name == a['kind'],
      orElse: r.bad,
    );
    final starts = r.number(a, 'starts', 0, clock),
        activeAt = r.number(a, 'activeAt', starts, starts + 6);
    if ((activeAt - starts - 6).abs() > 1e-6) r.bad();
    final e = Incident(
      id,
      kind,
      r.building(a['target']),
      starts,
      activeAt,
      a['origin'],
    );
    e
      ..state = r.id(a['state'])
      ..ends = r.number(a, 'ends', -1, clock)
      ..lastTick = r.number(a, 'lastTick', 0, clock)
      ..grazingAt = r.number(a, 'grazingAt', -1, clock)
      ..animalProgress = r.number(a, 'animalProgress', 0, 10.01)
      ..shoo = r.number(a, 'shoo', 0, 2.1)
      ..damage = r.integer(a, 'damage', 0, 210)
      ..driven = r.flag(a, 'driven');
    e.responders.addAll(r.identities(a['responders']));
    e.sheltered.addAll(r.identities(a['sheltered']));
    e.dispatched.addAll(r.identities(a['dispatched']));
    if (e.responders.length > 4 ||
        e.dispatched.length > 4 ||
        !e.responders.every(e.dispatched.contains)) {
      r.bad();
    }
    for (final item in r.list(a['fires'], 3)) {
      final f = r.map(item),
          site = FireSite(
            r.building(f['building']),
            r.cell(f['cell']),
            r.number(f, 'heat', 0, 26),
          );
      site
        ..elapsed = r.number(f, 'elapsed', 0, 90)
        ..damageClock = r.number(f, 'damageClock', 0, 2.01)
        ..water = r.number(f, 'water', 0, 26)
        ..damage = r.integer(f, 'damage', 0, 70)
        ..spread = r.flag(f, 'spread');
      if (e.fires.any((old) => old.building == site.building)) r.bad();
      if (site.heat > 0 &&
          (buildingCell(site.building) != site.cell ||
              (status(site.building)?.health ?? 100) != 100 - site.damage)) {
        r.bad();
      }
      e.fires.add(site);
    }
    if (kind == IncidentKind.fire) {
      if (!['warning', 'burning'].contains(e.state) ||
          e.fires.isEmpty ||
          e.fires.fold<int>(0, (n, f) => n + f.damage) != e.damage ||
          a['animal'] != null) {
        r.bad();
      }
    } else {
      if (![
            'warning',
            'approaching',
            'grazing',
            'retreating',
          ].contains(e.state) ||
          e.fires.isNotEmpty ||
          e.damage > 20) {
        r.bad();
      }
      final v = r.map(a['animal']), key = r.id(v['id']);
      if (key != 'animal-${int.parse(id.split('-').last)}' ||
          v['bodyId'] != 'rabbit-body-v1' ||
          v['name'] != 'Wild rabbit' ||
          v['active'] != true ||
          v['arrived'] != true ||
          v['workId'] != null) {
        r.bad();
      }
      final actor =
          Resident(
              key,
              r.cell(v['cell']),
              Activity.values.firstWhere(
                (s) => s.name == v['activity'],
                orElse: r.bad,
              ),
            )
            ..bodyId = 'rabbit-body-v1'
            ..name = 'Wild rabbit'
            ..arrived = true;
      if (![
        Activity.emergencyWaiting,
        Activity.incidentTravel,
        Activity.disturbing,
        Activity.settling,
      ].contains(actor.activity)) {
        r.bad();
      }
      actor
        ..x = r.number(v, 'x', 0, world.width.toDouble())
        ..y = r.number(v, 'y', 0, world.height.toDouble())
        ..timer = r.number(v, 'timer', -1, 3)
        ..palette = r.integer(v, 'palette', 0, 3)
        ..gardenDay = r.integer(v, 'gardenDay', -1, day)
        ..segmentSpeed = r.number(v, 'segmentSpeed', 0, 20)
        ..segmentGarden = r.flag(v, 'segmentGarden')
        ..replanAfterEdge = r.flag(v, 'replan');
      actor.trait = Trait.values.firstWhere(
        (t) => t.name == v['trait'],
        orElse: r.bad,
      );
      actor.destination = v['destination'] == null
          ? null
          : r.building(v['destination']);
      actor.settleTarget = v['settle'] == null ? null : r.cell(v['settle']);
      var previous = actor.cell;
      for (final item in r.list(v['route'], world.lots.length)) {
        final cell = r.cell(item);
        if (!world.roadStep(previous, cell)) r.bad();
        actor.route.add(cell);
        previous = cell;
      }
      if ((actor.x - actor.cell.x - .5).abs() +
                  (actor.y - actor.cell.y - .5).abs() >
              1.02 ||
          actor.activity == Activity.settling && actor.settleTarget == null) {
        r.bad();
      }
      e.animal = actor;
    }
    incident = e;
  }

  void validatePreparedness() {
    final r = _SafetyReader(this),
        known = records.map((p) => p.homeId).toSet(),
        e = incident;
    if (e != null) {
      if (![
        ...e.responders,
        ...e.sheltered,
        ...e.dispatched,
      ].every(known.contains)) {
        r.bad();
      }
      if (e.kind == IncidentKind.wildlife &&
          e.state == 'grazing' &&
          (kind(e.target) != Place.garden ||
              e.animal!.moving ||
              incidentAccess(e.target, e.animal!.cell) != e.animal!.cell)) {
        r.bad();
      }
    }
    for (final person in records) {
      if (person.safetyRole == null) {
        if ([
          Activity.responseTravel,
          Activity.responding,
          Activity.shelterTravel,
          Activity.sheltering,
          Activity.fleeing,
          Activity.emergencyWaiting,
        ].contains(person.activity)) {
          r.bad();
        }
        continue;
      }
      if (!person.active ||
          !['fire', 'guard', 'evacuating'].contains(person.safetyRole)) {
        r.bad();
      }
      if (person.safetyIncident != null) {
        r.incidentId(person.safetyIncident, nextIncidentId);
      }
      // displacement can outlive an incident or return after a fresh repair Undo.
      if (person.safetyIncident == null &&
          (person.safetyRole != 'evacuating' || homeUsable(person))) {
        r.bad();
      }
      if (person.safetyTarget != null) r.building(person.safetyTarget);
      if (person.safetyRole != 'evacuating' &&
          (e?.id != person.safetyIncident ||
              !(e?.dispatched.contains(person.homeId) ?? false))) {
        r.bad();
      }
      if (person.activity == Activity.sheltering &&
          (kind(person.safetyTarget) != Place.refuge ||
              !usable(person.safetyTarget) ||
              incidentAccess(person.safetyTarget, person.cell) !=
                  person.cell)) {
        r.bad();
      }
      if (person.activity == Activity.responding &&
          (e == null ||
              !ready(person) ||
              incidentAccess(person.safetyTarget, person.cell) !=
                  person.cell)) {
        r.bad();
      }
    }
    for (final entry in world.places.entries.where(
      (p) => kind(p.key) == Place.refuge,
    )) {
      if (shelterReservations(entry.key) > shelterCapacity(entry.key)) r.bad();
    }
  }
}
