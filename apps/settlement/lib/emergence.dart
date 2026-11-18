part of 'colony.dart';

enum ScenarioKind {
  free(
    'Free Build',
    'Ordinary 200 coins; no objectives. Every tool remains available.',
  ),
  trade(
    'Riverside Trade',
    'River Country · 210 coins. Connect a Market, receive a real merchant and complete trade.',
  ),
  crossing(
    'Highland Crossing',
    'Highlands · 220 coins. Reach the far plateau, establish work and keep a route through trouble.',
  ),
  outpost(
    'Frontier Outpost',
    'Harsh Frontier · 210 coins. Welcome six residents, staff protection and recover from trouble.',
  );

  final String label, description;
  const ScenarioKind(this.label, this.description);
  EnvironmentPreset? get environment => switch (this) {
    free => null,
    trade => EnvironmentPreset.river,
    crossing => EnvironmentPreset.highlands,
    outpost => EnvironmentPreset.frontier,
  };
  List<String> get objectives => switch (this) {
    free => const [],
    trade => const [
      'Connect a working Market',
      'Receive a merchant at the Market',
      'Complete a real trade',
    ],
    crossing => const [
      'Connect a road beyond column 20',
      'Six residents and actual far-side work',
      'Keep far-side access through an incident',
    ],
    outpost => const [
      'Welcome six residents',
      'Train reachable protective staff',
      'Recover from a serious incident',
    ],
  };
}

class StoryFact {
  final String id, kind, outcome;
  final double time;
  final Cell cell;
  final List<String> participants;
  final String? source;
  const StoryFact(
    this.id,
    this.kind,
    this.outcome,
    this.time,
    this.cell,
    this.participants, [
    this.source,
  ]);
  Map<String, Object> toJson() => {
    'id': id,
    'kind': kind,
    'outcome': outcome,
    'time': time,
    'cell': [cell.x, cell.y],
    'participants': participants,
    'source': ?source,
  };
}

extension Emergence on Colony {
  void configureScenario(ScenarioKind choice) {
    if (clock != 0 || people.isNotEmpty) {
      throw StateError('Choose a scenario only in a new world.');
    }
    scenario = choice;
    session.money = switch (choice) {
      ScenarioKind.free => 200000,
      ScenarioKind.crossing => 220000,
      _ => 210000,
    };
  }

  void remember(
    String kind,
    String outcome,
    Cell cell, [
    Iterable<String> participants = const [],
    String? source,
  ]) {
    stories.add(
      StoryFact(
        'story-${nextStoryId++}',
        kind,
        outcome,
        clock,
        cell,
        participants.take(500).toList(),
        source,
      ),
    );
    if (stories.length > 80) stories.removeAt(0);
  }

  String? rememberedBy(String id) => stories.reversed
      .where((s) => s.participants.contains(id))
      .firstOrNull
      ?.outcome;

  void _worldBoundary(double dt) {
    rainExposure = raining
        ? math.min(180, rainExposure + dt)
        : math.max(0, rainExposure - dt * .5);
    if (clock + 1e-9 < nextStoryTick) return;
    nextStoryTick = clock + 1;
    final reached = switch (scenario) {
      ScenarioKind.free => <bool>[],
      ScenarioKind.trade => [
        places(Place.market)
            .any((p) => workers(p.key) > 0 && placeAccess(p.key) != null),
        visits.values.any(
              (v) => v.role == VisitorRole.merchant && v.progress > 0,
            ) ||
            visitHistory.any(
              (v) => v.role == VisitorRole.merchant && v.reward > 0,
            ),
        visitHistory.any(
              (v) => v.role == VisitorRole.merchant && v.reward > 0,
            ) ||
            visits.values.any(
              (v) => v.role == VisitorRole.merchant && v.completed,
            ) ||
            tradeEarned > 0,
      ],
      ScenarioKind.crossing => [
        world.activePaths.any((c) => c.x >= 20),
        people.length >= 6 && farSideWorking,
        stories.any((h) => h.kind == 'passage') &&
            world.activePaths.any((c) => c.x >= 20) &&
            workEarned > 0,
      ],
      ScenarioKind.outpost => [
        people.length >= 6,
        people.values.any(ready),
        incidentHistory.any((h) => h.kind != IncidentKind.caravan.name) &&
            session.conditions.entries
                .where((e) => buildingCell(e.key) != null)
                .every((e) => e.value.health == 100),
      ],
    };
    for (var i = 0; i < reached.length; i++) {
      if (reached[i] && scenarioCompleted.add(i)) {
        remember(
          'scenario',
          '${scenario.label}: ${scenario.objectives[i]}',
          world.arrival,
        );
      }
    }
    for (final entry in session.conditions.entries) {
      final c = entry.value,
          source = 'recovery-${entry.key}-${c.damageRevision}';
      if (c.health == 100 &&
          c.repairedAt >= 0 &&
          clock - c.repairedAt <= 1.05 &&
          buildingCell(entry.key) != null &&
          !stories.any((s) => s.source == source)) {
        remember(
          'recovery',
          '${kind(entry.key)?.label ?? 'Cottage'} repaired and available again.',
          buildingCell(entry.key)!,
          people.containsKey(entry.key) ? [entry.key] : const [],
          source,
        );
      }
    }
  }

  bool flooded(String? id) =>
      incident?.kind == IncidentKind.flood &&
      incident!.state != 'warning' &&
      incident!.flood.contains(buildingCell(id));

  List<String> _floodTargets() =>
      [...world.cottages.keys, ...world.places.keys].where((id) {
        final c = buildingCell(id)!;
        return usable(id) && world.floodRisk.contains(c);
      }).toList();

  List<String> _raidTargets() =>
      places(Place.market)
          .where((p) => usable(p.key) && world.access(p.value) != null)
          .map((p) => p.key)
          .toList();

  List<String> _caravanTargets() => world.places.entries
      .where(
        (p) =>
            [Place.inn, Place.refuge].contains(kind(p.key)) &&
            usable(p.key) &&
            world.access(p.value) != null,
      )
      .map((p) => p.key)
      .toList();

  List<String> _incidentTargets(IncidentKind kind) => switch (kind) {
    IncidentKind.fire => _fireTargets(),
    IncidentKind.wildlife => _wildTargets(),
    IncidentKind.flood => _floodTargets(),
    IncidentKind.raiders => _raidTargets(),
    IncidentKind.caravan => _caravanTargets(),
  };

  bool get farSideWorking => people.values.any(
    (r) =>
        r.activity == Activity.working &&
        r.workProgress > 0 &&
        usable(r.workId) &&
        (buildingCell(r.workId)?.x ?? -1) >= 20 &&
        placeAccess(r.workId) == r.cell,
  );

  void _prepareEncounter(Incident e) {
    if (e.kind == IncidentKind.flood) {
      final target = buildingCell(e.target)!;
      final waters =
          world.cells
              .where((c) => world.at(c).terrain == Terrain.water)
              .toList()
            ..sort(
              (a, b) => ((a.x - target.x).abs() + (a.y - target.y).abs())
                  .compareTo((b.x - target.x).abs() + (b.y - target.y).abs()),
            );
      final start = waters.first,
          queue = Queue<(Cell, int)>()..add((start, 0)),
          seen = <Cell>{};
      while (queue.isNotEmpty && e.flood.length < 40) {
        final (c, d) = queue.removeFirst();
        if (!world.contains(c) ||
            !seen.add(c) ||
            world.at(c).terrain == Terrain.rock ||
            c == world.arrival) {
          continue;
        }
        e.flood.add(c);
        if (d < 3) queue.addAll(c.neighbours.map((n) => (n, d + 1)));
      }
      e.floodSource = start;
    } else {
      e.animal =
          Resident(
              'animal-${int.parse(e.id.split('-').last)}',
              world.arrival,
              Activity.emergencyWaiting,
            )
            ..bodyId = e.kind == IncidentKind.raiders
                ? 'hood-v1'
                : 'merchant-body-v1'
            ..name = e.kind == IncidentKind.raiders
                ? 'Road brigand'
                : 'Caravan traveller'
            ..trait = Trait.sociable
            ..arrived = true;
    }
  }

  void _applyFlood(Incident e, {bool ending = false}) {
    final closed = ending
        ? <Cell>{}
        : e.flood.where((c) {
            final lot = world.at(c);
            return lot.path && lot.bridge == null && lot.road != RoadKind.stone;
          }).toSet();
    if (world.closures.length == closed.length &&
        world.closures.containsAll(closed)) {
      return;
    }
    world.closeRoads(closed);
    e.passageHeld &= world.activePaths.any((c) => c.x >= 20);
    navigation.invalidate(
      world,
      raining: raining,
      weatherCost: weather.travel,
      marshCost: marshTravel,
    );
    // Stop entering new flood cells. Existing edge occupants retreat along the
    // existing local settle rule or physically wait; nobody jumps a crossing.
    for (final r in actors.toList()) {
      if (r.route.isNotEmpty && !world.roadStep(r.cell, r.route.first)) {
        settleActor(r);
      } else if (r.route.length > 1) {
        r.route.removeRange(1, r.route.length);
        r.replanAfterEdge = true;
      }
      if (!r.moving) plan(r);
    }
    for (final c in e.flood) {
      final id = buildingAt(c);
      if (id != null) _invalidateBuildingUse(id);
    }
    _tickEvent();
    _countsDirty = true;
    _problems();
  }

  void _encounterTick(Incident e, double dt) {
    if (e.kind == IncidentKind.flood) {
      if (clock < e.activeAt) return;
      if (e.state == 'warning') e.state = 'flooding';
      _applyFlood(e);
      e.animalProgress = math.min(45, e.animalProgress + dt);
      if ((e.animalProgress / 5).floor() >
          (math.max(0, e.animalProgress - dt) / 5).floor()) {
        for (final c in e.flood) {
          final id = buildingAt(c);
          if (id == null || !e.losses.containsKey(id) && e.losses.length >= 8) {
            continue;
          }
          final loss = math.min(3, 18 - (e.losses[id] ?? 0));
          if (loss <= 0) continue;
          damageBuilding(id, loss);
          e.losses[id] = (e.losses[id] ?? 0) + loss;
          e.damage += loss;
        }
      }
      if (e.animalProgress >= 45) {
        _applyFlood(e, ending: true);
        _finishIncident('water receded');
      }
      return;
    }
    if (e.animal == null || e.state != 'grazing') {
      if ((e.kind == IncidentKind.caravan || e.kind == IncidentKind.raiders) &&
          clock - e.starts > 100 &&
          e.state != 'retreating') {
        _retreatAnimal(e);
      }
      return;
    }
    if (e.animal!.moving ||
        incidentAccess(e.target, e.animal!.cell) != e.animal!.cell) {
      e.state = 'approaching';
      plan(e.animal!);
      return;
    }
    dt = math.min(dt, math.max(0, clock - e.grazingAt));
    if (e.kind == IncidentKind.raiders) {
      e.animalProgress = math.min(10, e.animalProgress + dt);
      if (e.animalProgress >= 2 && !e.paid) {
        e.paid = true;
        e.moneyLost = math.min(session.money, 4000);
        session.money -= e.moneyLost;
        incidentSpent += e.moneyLost;
      }
      if (e.animalProgress < 10 &&
          (e.animalProgress / 2).floor() >
              (math.max(0, e.animalProgress - dt) / 2).floor()) {
        damageBuilding(e.target, 3);
        e.damage += 3;
      }
      if (e.animalProgress >= 10) _retreatAnimal(e);
    } else if (e.kind == IncidentKind.caravan) {
      final served = kind(e.target) == Place.inn
          ? workers(e.target) > 0 &&
                usable(e.target) &&
                occupancy(e.target) < socialCapacity(e.target)
          : shelterCapacity(e.target) > shelterReservations(e.target);
      if (served) {
        e.animalProgress = math.min(8, e.animalProgress + dt);
        session.condition(e.target).productive += dt;
      }
      if (e.animalProgress >= 8) {
        if (!e.paid) {
          e.paid = true;
          _earn(2000, visitor: true);
        }
        _retreatAnimal(e);
      } else if (clock - e.starts > 100) {
        _retreatAnimal(e);
      }
    }
  }

  Map<String, Object?> emergenceJson() => {
    'version': 1,
    'scenario': scenario.name,
    'completed': scenarioCompleted.toList()..sort(),
    'rainExposure': rainExposure,
    'nextTick': nextStoryTick,
    'nextId': nextStoryId,
    'incidentSpent': incidentSpent,
    'history': stories.map((s) => s.toJson()).toList(),
  };

  void restoreEmergence(dynamic value) {
    final r = _SafetyReader(this), d = r.map(value);
    if (d['version'] != 1) r.bad();
    scenario = ScenarioKind.values.firstWhere(
      (s) => s.name == d['scenario'],
      orElse: r.bad,
    );
    for (final i in r.list(d['completed'], 3)) {
      if (i is! int ||
          i < 0 ||
          i >= scenario.objectives.length ||
          !scenarioCompleted.add(i)) {
        r.bad();
      }
    }
    rainExposure = r.number(d, 'rainExposure', 0, 180);
    nextStoryTick = r.number(d, 'nextTick', 0, clock + 1.01);
    nextStoryId = r.integer(d, 'nextId', 1);
    incidentSpent = r.integer(d, 'incidentSpent', 0, 1000000000000);
    final ids = <String>{};
    for (final item in r.list(d['history'], 80)) {
      final s = r.map(item), id = r.id(s['id']);
      final n = int.tryParse(id.replaceFirst('story-', ''));
      if (n == null ||
          n < 1 ||
          n >= nextStoryId ||
          !ids.add(id) ||
          s['outcome'] is! String ||
          s['outcome'].length > 500) {
        r.bad();
      }
      stories.add(
        StoryFact(
          id,
          r.id(s['kind']),
          s['outcome'],
          r.number(s, 'time', 0, clock),
          r.cell(s['cell']),
          r.identities(s['participants']).toList(),
          s['source'] == null ? null : r.id(s['source']),
        ),
      );
    }
  }
}
