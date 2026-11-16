part of 'colony.dart';

enum IncidentMode { peaceful, standard }

enum IncidentKind { fire, wildlife }

class FireSite {
  final String building;
  final Cell cell;
  double heat, elapsed = 0, damageClock = 0, water = 0;
  int damage = 0;
  bool spread = false;
  FireSite(this.building, this.cell, this.heat);
  Map<String, Object> toJson() => {
    'building': building,
    'cell': [cell.x, cell.y],
    'heat': heat,
    'elapsed': elapsed,
    'damageClock': damageClock,
    'water': water,
    'damage': damage,
    'spread': spread,
  };
}

class Incident {
  final String id, target, origin;
  final IncidentKind kind;
  final double starts;
  final List<FireSite> fires = [];
  final Set<String> responders = {}, sheltered = {};
  Resident? animal;
  String state = 'warning';
  double lastTick = 0, grazingAt = -1;
  final Set<String> dispatched = {};
  double activeAt, ends = -1, animalProgress = 0, shoo = 0;
  int damage = 0;
  bool driven = false;
  Incident(
    this.id,
    this.kind,
    this.target,
    this.starts,
    this.activeAt,
    this.origin,
  );
  String get title => kind == IncidentKind.fire ? 'Fire' : 'Rabbit disturbance';
  String get description => kind == IncidentKind.fire
      ? state == 'warning'
            ? 'Smoke warning: prepare connected Firehouses and Refuges. Responders travel automatically.'
            : 'Fire is damaging nearby buildings. Staffed responders must reach it; repair only after it is safe.'
      : 'A rabbit is following the roads towards a Farm. A staffed Watchpost can send a guard; crops are affected only after arrival.';
  Map<String, Object?> toJson() => {
    'id': id,
    'kind': kind.name,
    'target': target,
    'starts': starts,
    'activeAt': activeAt,
    'origin': origin,
    'state': state,
    'ends': ends,
    'animalProgress': animalProgress,
    'shoo': shoo,
    'damage': damage,
    'driven': driven,
    'fires': fires.map((f) => f.toJson()).toList(),
    'responders': responders.toList(),
    'sheltered': sheltered.toList(),
  };
}

class IncidentMemory {
  final String id, kind, outcome, origin;
  final double starts, ends;
  final int damage, responders, sheltered;
  final List<String> buildings;
  IncidentMemory(
    this.id,
    this.kind,
    this.outcome,
    this.origin,
    this.starts,
    this.ends,
    this.damage,
    this.responders,
    this.sheltered,
    this.buildings,
  );
  Map<String, Object> toJson() => {
    'id': id,
    'kind': kind,
    'outcome': outcome,
    'origin': origin,
    'starts': starts,
    'ends': ends,
    'damage': damage,
    'responders': responders,
    'sheltered': sheltered,
    'buildings': buildings,
  };
}

extension Preparedness on Colony {
  String? buildingAt(Cell c) =>
      world.contains(c) ? world.at(c).buildingId : null;
  Cell? buildingCell(String? id) => world.cottages[id] ?? world.places[id];
  BuildingCondition? status(String? id) => session.conditions[id];
  bool burning(String id) =>
      incident?.kind == IncidentKind.fire &&
      incident!.fires.any((f) => f.building == id && f.heat > 0);
  bool usable(String? id) =>
      id != null &&
      (status(id)?.usable ?? true) &&
      !burning(id) &&
      !(incident?.kind == IncidentKind.wildlife &&
          incident!.target == id &&
          incident!.state == 'grazing');
  int socialCapacity(String? id) {
    final base = kind(id)?.visitorCapacity ?? 0;
    return !usable(id)
        ? 0
        : (status(id)?.health ?? 100) < 100
        ? math.max(1, base ~/ 2)
        : base;
  }

  int shelterCapacity(String id) => kind(id) != Place.refuge || !usable(id)
      ? 0
      : (status(id)?.health ?? 100) < 100
      ? 4
      : 8;
  Cell? incidentAccess(String? id, Cell from) {
    final cell = buildingCell(id);
    if (cell == null) return null;
    for (final n in world.pathNeighbours(cell)) {
      if (travelDistance(from, n).isFinite) return n;
    }
    return null;
  }

  bool ready(Resident r) =>
      kind(r.workId)?.protective == true &&
      usable(r.workId) &&
      r.dutyBuilding == r.workId &&
      (clock - r.dutyAt <= 60 ||
          r.safetyIncident == incident?.id &&
              (r.safetyRole == 'fire' || r.safetyRole == 'guard')) &&
      world.access(world.places[r.workId]!) != null;
  void setIncidentMode(IncidentMode mode) {
    if (mode == incidentMode) return;
    incidentMode = mode;
    if (mode == IncidentMode.standard) {
      nextIncidentAt = math.max(nextIncidentAt, clock + 450);
    }
  }

  void damageBuilding(String id, int amount, {bool scorch = false}) {
    if (buildingCell(id) == null || amount <= 0) return;
    final record = session.condition(id), before = record.health;
    record.health = math.max(0, record.health - amount);
    if (record.health != before) record.damageRevision++;
    record.scorched |= scorch;
    if (kind(id) == Place.refuge) _rebalanceShelters();
    _invalidateBuildingUse(id);
    _tickEvent();
    _problems();
    _countsDirty = true;
  }

  void _invalidateBuildingUse(String id) {
    _countsDirty = true;
    for (final r in actors.toList()) {
      if (r.destination != id ||
          ![
            Activity.working,
            Activity.socialising,
            Activity.socialWaiting,
            Activity.gathering,
          ].contains(r.activity)) {
        continue;
      }
      if (usable(id) &&
          (r.activity == Activity.working || occupancy(id) <= capacity(id))) {
        continue;
      }
      if (r.activity == Activity.working) r.workIssue = Reason.workCut;
      r.activity = Activity.waiting;
      r.timer = 0;
      r.socialProgress = 0;
      plan(r);
      _countsDirty = true;
    }
  }

  bool threatened(Cell c) {
    final e = incident;
    if (e == null) return false;
    if (e.kind == IncidentKind.wildlife) {
      final farm = buildingCell(e.target), animal = e.animal;
      if (farm == null ||
          animal == null ||
          (animal.x - farm.x - .5).abs() + (animal.y - farm.y - .5).abs() > 3) {
        return false;
      }
    }
    final targets = e.kind == IncidentKind.fire
        ? e.fires.where((f) => f.heat > 0).map((f) => f.cell)
        : [if (buildingCell(e.target) != null) buildingCell(e.target)!];
    return targets.any(
      (p) =>
          (p.x - c.x).abs() + (p.y - c.y).abs() <=
          (e.kind == IncidentKind.fire ? 2 : 1),
    );
  }

  bool homeUsable(Resident r) => usable(r.homeId);
  void _safetyStart(Resident r, String role, {String? target}) {
    _countsDirty = true;
    if (role == 'fire' || role == 'guard') incident?.dispatched.add(r.homeId);
    r.safetyRole = role;
    r.safetyTarget = target;
    r.safetyIncident = incident?.id ?? r.safetyIncident;
    if (r.route.isNotEmpty && world.roadStep(r.cell, r.route.first)) {
      r.route.removeRange(1, r.route.length);
      r.replanAfterEdge = true;
    } else {
      r.route.clear();
      r.segmentSpeed = 0;
      r.activity = Activity.emergencyWaiting;
      plan(r);
    }
  }

  Cell? _safeRoad(Resident r) {
    final queue = Queue<Cell>()..add(r.cell), seen = <Cell>{};
    while (queue.isNotEmpty && seen.length < 64) {
      final c = queue.removeFirst();
      if (!seen.add(c)) continue;
      if (world.roadCell(c) && !threatened(c)) return c;
      queue.addAll(world.pathNeighbours(c).where((n) => !seen.contains(n)));
    }
    return null;
  }

  void _planSafety(Resident r) {
    r.replanAfterEdge = false;
    final e = incident;
    if (r.safetyRole == 'fire' || r.safetyRole == 'guard') {
      if (e == null || e.id != r.safetyIncident || !ready(r)) {
        _releaseSafety(r);
        return;
      }
      String? target;
      if (r.safetyRole == 'fire') {
        final sites = e.fires
            .where((f) => f.heat > 0 && buildingCell(f.building) != null)
            .toList();
        sites.sort(
          (a, b) => ((a.cell.x - r.cell.x).abs() + (a.cell.y - r.cell.y).abs())
              .compareTo(
                (b.cell.x - r.cell.x).abs() + (b.cell.y - r.cell.y).abs(),
              ),
        );
        target = sites.firstOrNull?.building;
      } else {
        target = e.target;
      }
      r.safetyTarget = target;
      r.destination = target;
      final access = incidentAccess(target, r.cell);
      if (access != null && go(r, access, Activity.responseTravel)) return;
      r.activity = Activity.emergencyWaiting;
      r.timer = 3;
      r.say(Reason.responseCut);
      return;
    }
    final risk = threatened(r.cell) || !homeUsable(r);
    if (!risk && e == null) {
      _releaseSafety(r);
      return;
    }
    final refuges = places(Place.refuge)
        .where(
          (p) =>
              usable(p.key) &&
              !threatened(p.value) &&
              shelterReservations(p.key) - (r.safetyTarget == p.key ? 1 : 0) <
                  shelterCapacity(p.key),
        )
        .toList();
    refuges.sort(
      (a, b) => ((a.value.x - r.cell.x).abs() + (a.value.y - r.cell.y).abs())
          .compareTo(
            (b.value.x - r.cell.x).abs() + (b.value.y - r.cell.y).abs(),
          ),
    );
    for (final p in refuges) {
      final access = incidentAccess(p.key, r.cell);
      if (access != null) {
        r.safetyTarget = p.key;
        r.destination = p.key;
        if (go(r, access, Activity.shelterTravel)) return;
      }
    }
    r.safetyTarget = null;
    r.destination = null;
    final safe = _safeRoad(r);
    if (safe != null && go(r, safe, Activity.fleeing)) return;
    r.activity = Activity.emergencyWaiting;
    r.timer = 3;
  }

  void _safetyReached(Resident r) {
    if (r.activity == Activity.responseTravel) {
      if (incidentAccess(r.safetyTarget, r.cell) == r.cell &&
          incident?.id == r.safetyIncident) {
        r.activity = Activity.responding;
        r.responseSince = clock;
        r.say(r.safetyRole == 'fire' ? Reason.fightingFire : Reason.guarding);
      } else {
        r.activity = Activity.emergencyWaiting;
        r.timer = 0;
      }
    } else if (r.activity == Activity.shelterTravel) {
      if (usable(r.safetyTarget) &&
          incidentAccess(r.safetyTarget, r.cell) == r.cell) {
        r.activity = Activity.sheltering;
        r.sheltered = true;
        incident?.sheltered.add(r.homeId);
        session.condition(r.safetyTarget!).uses++;
        r.say(Reason.sheltered);
      } else {
        r.activity = Activity.emergencyWaiting;
        r.timer = 0;
      }
    } else {
      r.activity = Activity.emergencyWaiting;
      r.timer = 3;
    }
    _countsDirty = true;
  }

  void _releaseSafety(Resident r) {
    _countsDirty = true;
    r.safetyRole = null;
    r.safetyTarget = null;
    r.safetyIncident = null;
    r.destination = null;
    r.say(
      incident == null && homeUsable(r) ? Reason.safeAgain : Reason.responseCut,
    );
    if (r.route.isNotEmpty && world.roadStep(r.cell, r.route.first)) {
      r.route.removeRange(1, r.route.length);
      r.replanAfterEdge = true;
      r.activity = Activity.returning;
    } else {
      r.route.clear();
      r.activity = Activity.waiting;
      r.timer = 0;
      plan(r);
    }
  }

  void _incidentOpportunity() {
    if (clock + 1e-9 < nextIncidentAt) return;
    nextIncidentAt = clock + 30;
    if (incidentMode != IncidentMode.standard ||
        incident != null ||
        clock < incidentCooldown ||
        !workPeriod ||
        people.length < 6 ||
        event != null && event!.state != 'ended') {
      return;
    }
    if (session.conditions.entries
            .where((e) => buildingCell(e.key) != null && !e.value.usable)
            .isNotEmpty ||
        session.conditions.entries
                .where(
                  (e) => buildingCell(e.key) != null && e.value.health < 100,
                )
                .length >=
            6) {
      return;
    }
    final choices = <IncidentKind>[];
    if (_fireTargets().isNotEmpty) {
      choices.addAll(
        List.filled(
          weather == WeatherKind.rain || weather == WeatherKind.snow
              ? 1
              : season == Season.summer
              ? 4
              : 2,
          IncidentKind.fire,
        ),
      );
    }
    if (_wildTargets().isNotEmpty) {
      choices.addAll(
        List.filled(
          season == Season.spring || season == Season.summer ? 3 : 1,
          IncidentKind.wildlife,
        ),
      );
    }
    if (choices.isEmpty) return;
    incidentRng = incidentRng * 48271 % 2147483647;
    startIncident(choices[incidentRng % choices.length]);
  }

  List<String> _fireTargets() => [...world.cottages.keys, ...world.places.keys]
      .where(
        (id) =>
            (status(id)?.health ?? 100) == 100 &&
            buildingCell(id) != null &&
            world.access(buildingCell(id)!) != null,
      )
      .toList();
  List<String> _wildTargets() => places(Place.garden)
      .where(
        (p) =>
            (status(p.key)?.health ?? 100) == 100 &&
            world.access(p.value) != null &&
            route(world.arrival, world.access(p.value)!) != null,
      )
      .map((p) => p.key)
      .toList();
  bool startIncident(
    IncidentKind kind, {
    String? target,
    bool fixture = false,
  }) {
    if (incidentMode != IncidentMode.standard ||
        incident != null ||
        !fixture && (clock < 450 || clock < incidentCooldown)) {
      return false;
    }
    final options = kind == IncidentKind.fire ? _fireTargets() : _wildTargets();
    if (options.isEmpty || target != null && !options.contains(target)) {
      return false;
    }
    if (target == null) {
      incidentRng = incidentRng * 48271 % 2147483647;
      target = options[incidentRng % options.length];
    }
    final e = Incident(
      'incident-${nextIncidentId++}',
      kind,
      target,
      clock,
      clock + 6,
      fixture ? 'fixture' : 'scheduled',
    );
    if (kind == IncidentKind.fire) {
      final heat = season == Season.summer
          ? 26.0
          : season == Season.winter
          ? 18.0
          : 22.0;
      e.fires.add(FireSite(target, buildingCell(target)!, heat));
    } else {
      e.animal =
          Resident(
              'animal-${nextIncidentId - 1}',
              world.arrival,
              Activity.emergencyWaiting,
            )
            ..bodyId = 'rabbit-body-v1'
            ..name = 'Wild rabbit'
            ..trait = Trait.sociable
            ..arrived = true;
    }
    incident = e;
    if (kind == IncidentKind.fire) {
      _invalidateBuildingUse(target);
      _tickEvent();
    }
    _rebalanceShelters();
    nextIncidentTick = clock;
    _countsDirty = true;
    for (final r in people.values.where(
      (r) => r.arrived && threatened(r.cell),
    )) {
      r.say(Reason.incidentWarning);
    }
    _problems();
    return true;
  }

  void _dispatch() {
    final e = incident;
    if (e == null) return;
    final role = e.kind == IncidentKind.fire ? 'fire' : 'guard',
        type = e.kind == IncidentKind.fire ? Place.firehouse : Place.watchpost;
    final assigned = people.values
        .where((r) => r.safetyIncident == e.id && r.safetyRole == role)
        .length;
    if (assigned >= (role == 'fire' ? 4 : 1)) return;
    final candidates = people.values
        .where(
          (r) =>
              kind(r.workId) == type &&
              r.safetyRole == null &&
              r.arrived &&
              ready(r),
        )
        .toList();
    candidates.sort((a, b) => a.homeId.compareTo(b.homeId));
    var count = assigned;
    for (final r in candidates) {
      final target = buildingCell(e.target);
      if (target == null) continue;
      final station = world.access(world.places[r.workId]!);
      final targetAccess = world.access(target);
      if (role == 'guard' &&
          (station == null ||
              targetAccess == null ||
              travelDistance(station, targetAccess) > 10)) {
        continue;
      }
      if (!e.dispatched.contains(r.homeId) &&
          e.dispatched.length >= (role == 'fire' ? 4 : 1)) {
        continue;
      }
      if (count++ >= (role == 'fire' ? 4 : 1)) break;
      session.condition(r.workId!).uses++;
      _safetyStart(r, role, target: e.target);
    }
  }

  void _finishIncident(String outcome) {
    final e = incident;
    if (e == null) return;
    incidentHistory.add(
      IncidentMemory(
        e.id,
        e.kind.name,
        outcome,
        e.origin,
        e.starts,
        clock,
        e.damage,
        e.responders.length,
        e.sheltered.length,
        e.kind == IncidentKind.fire
            ? e.fires.map((f) => f.building).toList()
            : [e.target],
      ),
    );
    if (incidentHistory.length > 24) incidentHistory.removeAt(0);
    final animalId = e.animal?.homeId;
    if (animalId != null) {
      planning.removeWhere((p) => p.id == animalId);
      _queued.remove(animalId);
    }
    incidentCooldown = clock + 600;
    nextIncidentAt = incidentCooldown;
    incident = null;
    for (final r
        in people.values.where((r) => r.safetyIncident == e.id).toList()) {
      if (homeUsable(r)) {
        _releaseSafety(r);
      } else {
        r.safetyRole = 'evacuating';
        r.activity = Activity.emergencyWaiting;
        r.timer = 0;
        plan(r);
      }
    }
  }

  void _hazardTick(double dt) {
    final e = incident;
    if (e == null) return;
    hazardTicks++;
    dt = (clock - math.max(e.activeAt, e.lastTick)).clamp(0.0, 1.0);
    e.lastTick = clock;
    if (clock >= e.activeAt && e.state == 'warning') {
      e.state = e.kind == IncidentKind.fire ? 'burning' : 'approaching';
    }
    if (buildingCell(e.target) == null && e.kind == IncidentKind.wildlife) {
      if (e.animal != null) _retreatAnimal(e);
    }
    _dispatch();
    for (final r in people.values.where(
      (r) => r.arrived && r.safetyRole == null,
    )) {
      if (threatened(r.cell) || !homeUsable(r)) {
        r.exposureUntil = clock + 75;
        r.sheltered = false;
        _safetyStart(r, 'evacuating');
      }
    }
    if (e.kind == IncidentKind.fire && clock >= e.activeAt) {
      fireSitesVisited += e.fires.length;
      for (final f in e.fires.toList()) {
        if (f.heat <= 0) continue;
        if (buildingCell(f.building) == null) {
          f.heat = 0;
          continue;
        }
        f.heat = math.max(
          0,
          f.heat -
              dt *
                  (weather == WeatherKind.rain || weather == WeatherKind.snow
                      ? 2
                      : 1),
        );
        f.elapsed += dt;
        f.damageClock += dt;
        while (f.damageClock >= 2 && f.heat > 0) {
          f.damageClock -= 2;
          final damage = math.min(
            70 - f.damage,
            weather == WeatherKind.rain || weather == WeatherKind.snow ? 4 : 6,
          );
          damageBuilding(f.building, damage, scorch: true);
          f.damage += damage;
          e.damage += damage;
        }
        if (!f.spread &&
            f.elapsed >=
                (weather == WeatherKind.wind || season == Season.summer
                    ? 4
                    : 6)) {
          f.spread = true;
          if (weather != WeatherKind.rain &&
              weather != WeatherKind.snow &&
              f.heat > 4 &&
              e.fires.length < 3) {
            final candidates = f.cell.neighbours
                .where(world.contains)
                .map(buildingAt)
                .whereType<String>()
                .where(
                  (id) =>
                      (status(id)?.health ?? 100) == 100 &&
                      !e.fires.any((n) => n.building == id),
                )
                .toList();
            if (candidates.isNotEmpty) {
              incidentRng = incidentRng * 48271 % 2147483647;
              final id = candidates[incidentRng % candidates.length];
              e.fires.add(FireSite(id, buildingCell(id)!, 18));
            }
          }
        }
      }
      if (e.fires.every((f) => f.heat <= 0)) {
        _finishIncident(e.responders.isEmpty ? 'burned out' : 'contained');
      }
    } else if (e.kind == IncidentKind.wildlife && e.state == 'grazing') {
      if (e.animal!.moving ||
          incidentAccess(e.target, e.animal!.cell) != e.animal!.cell) {
        e.state = 'approaching';
        if (!e.animal!.moving) plan(e.animal!);
        return;
      }
      dt = math.min(dt, math.max(0, clock - e.grazingAt));
      e.animalProgress = math.min(10, e.animalProgress + dt);
      if (e.animalProgress >= 10) {
        _retreatAnimal(e);
      } else if ((e.animalProgress / 2).floor() >
          (math.max(0, e.animalProgress - dt) / 2).floor()) {
        damageBuilding(e.target, 4);
        e.damage += 4;
      }
    }
  }

  void _retreatAnimal(Incident e) {
    e.state = 'retreating';
    e.animal!.destination = null;
    plan(e.animal!);
  }

  void _planAnimal(Resident r) {
    final e = incident;
    if (e == null) return;
    r.replanAfterEdge = false;
    final goal = e.state == 'retreating'
        ? world.arrival
        : incidentAccess(e.target, r.cell);
    if (goal != null && go(r, goal, Activity.incidentTravel)) return;
    r.activity = Activity.emergencyWaiting;
    r.timer = 3;
  }

  void _animalReached(Resident r) {
    final e = incident;
    if (e == null) return;
    if (e.state == 'retreating' && r.cell == world.arrival) {
      _finishIncident(e.driven ? 'driven away' : 'retreated');
      return;
    }
    if (incidentAccess(e.target, r.cell) == r.cell) {
      e.state = 'grazing';
      r.activity = Activity.disturbing;
      e.grazingAt = clock;
      _invalidateBuildingUse(e.target);
    } else {
      r.activity = Activity.emergencyWaiting;
      r.timer = 3;
    }
  }

  void _updateSafety(double dt) {
    final e = incident;
    final animal = e?.animal;
    if (animal != null && clock >= e!.activeAt) {
      if (animal.moving) {
        advanceTravel(animal, dt);
      } else if (e.state != 'grazing') {
        animal.timer -= dt;
        if (animal.timer <= 0) {
          animal.timer = 3;
          plan(animal);
        }
      }
    }
    if (animal != null &&
        incident == e &&
        !animal.moving &&
        [Activity.waiting, Activity.returning].contains(animal.activity)) {
      animal.activity = Activity.emergencyWaiting;
    }
    if (e != null && clock >= e.activeAt) {
      for (final id in e.dispatched.toList()) {
        final r = people[id];
        if (r == null ||
            r.activity != Activity.responding ||
            !ready(r) ||
            clock - r.responseSince < dt - 1e-9) {
          continue;
        }
        if (r.safetyRole == 'fire') {
          final f = e.fires
              .where((f) => f.building == r.safetyTarget && f.heat > 0)
              .firstOrNull;
          if (f == null || incidentAccess(f.building, r.cell) != r.cell) {
            continue;
          }
          final water = math.min(
            f.heat,
            4 * dt * (status(r.workId)?.output ?? 1),
          );
          f.heat -= water;
          f.water += water;
          e.responders.add(id);
          r.dutyAt = clock;
          r.earned += _earn(dt * 100 * (status(r.workId)?.output ?? 1));
          r.workedSeconds += dt;
          if (r.workId != null) session.condition(r.workId!).productive += dt;
        } else if (e.animal != null && e.state != 'retreating') {
          final animal = e.animal!;
          if ((r.x - animal.x).abs() + (r.y - animal.y).abs() <= 1.1 &&
              (r.cell == animal.cell || world.roadStep(r.cell, animal.cell))) {
            e.shoo += dt * (status(r.workId)?.output ?? 1);
            e.responders.add(id);
            r.dutyAt = clock;
            r.earned += _earn(dt * 100 * (status(r.workId)?.output ?? 1));
            r.workedSeconds += dt;
            if (e.shoo >= 2) {
              e.driven = true;
              _retreatAnimal(e);
            }
          }
        }
      }
    }
    if (clock + 1e-9 >= nextIncidentTick) {
      nextIncidentTick = clock + 1;
      _hazardTick(1);
    }
  }

  void _safetyPersonStep(Resident r, double dt) {
    if ((r.safetyRole == 'fire' || r.safetyRole == 'guard') &&
        (incident?.id != r.safetyIncident || !ready(r))) {
      _releaseSafety(r);
      return;
    }
    if (r.activity == Activity.sheltering &&
        (!usable(r.safetyTarget) || threatened(r.cell))) {
      r.activity = Activity.emergencyWaiting;
      r.timer = 0;
      r.sheltered = false;
      r.safetyTarget = null;
      r.destination = null;
      _countsDirty = true;
    }
    if (r.activity == Activity.emergencyWaiting) {
      r.timer -= dt;
      if (r.timer <= 0) {
        r.timer = 3;
        plan(r);
      }
    }
    if (incident == null && homeUsable(r)) _releaseSafety(r);
  }

  void _rebalanceShelters() {
    for (final p in places(Place.refuge)) {
      final occupants =
          people.values
              .where(
                (r) => r.safetyRole == 'evacuating' && r.safetyTarget == p.key,
              )
              .toList()
            ..sort((a, b) => a.homeId.compareTo(b.homeId));
      for (final r in occupants.skip(shelterCapacity(p.key))) {
        r.sheltered = false;
        _safetyStart(r, 'evacuating');
      }
    }
  }

  void _reconcileSafety() {
    _rebalanceShelters();
    for (final r in inactive.values) {
      r.safetyRole = null;
      r.safetyTarget = null;
      r.safetyIncident = null;
    }
    final active = incident;
    if (active?.kind == IncidentKind.fire) {
      for (final f in active!.fires) {
        if (buildingCell(f.building) == null) f.heat = 0;
      }
      if (active.fires.every((f) => f.heat <= 0)) {
        _finishIncident('site removed');
      }
    }
    if (active?.kind == IncidentKind.wildlife &&
        buildingCell(active!.target) == null) {
      _retreatAnimal(active);
    }

    final animal = incident?.animal;
    if (animal != null) {
      if (incident!.state == 'grazing' &&
          incidentAccess(incident!.target, animal.cell) != animal.cell) {
        incident!.state = 'approaching';
      }
      if (animal.route.isNotEmpty &&
          world.roadStep(animal.cell, animal.route.first)) {
        animal.route.removeRange(1, animal.route.length);
        animal.replanAfterEdge = true;
      } else {
        if (!world.roadCell(animal.cell) ||
            (animal.x - animal.cell.x - .5).abs() +
                    (animal.y - animal.cell.y - .5).abs() >
                .01) {
          settleActor(animal);
        }
        if (!animal.moving && incident!.state != 'grazing') {
          animal.activity = Activity.emergencyWaiting;
          plan(animal);
        }
      }
    }
    for (final r in people.values.where((r) => r.safetyRole != null)) {
      if (!r.moving) {
        r.activity = Activity.emergencyWaiting;
        r.timer = 0;
        plan(r);
      }
    }
  }
}
