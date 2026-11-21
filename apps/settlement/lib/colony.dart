import 'dart:convert';
import 'dart:collection';
import 'dart:math' as math;

import 'model.dart';
import 'reasons.dart';
import 'residents.dart';
import 'rules.dart';
import 'navigation.dart';
import 'resident_identity.dart';
import 'special_event.dart';
import 'resident_request.dart';
import 'climate.dart';
import 'peoples.dart';
import 'condition.dart';
part 'visitors.dart';
part 'incidents.dart';
part 'preparedness_codec.dart';
part 'emergence.dart';

class Gathering {
  final String id, green;
  final List<String> invited;
  final Set<String> awarded = {};
  String state = 'inviting';
  double deadline, ends = -1;
  Gathering(this.id, this.green, this.invited, this.deadline);
  Map<String, Object> toJson() => {
    'id': id,
    'green': green,
    'invited': invited,
    'awarded': awarded.toList(),
    'state': state,
    'deadline': deadline,
    'ends': ends,
  };
}

class PlanningRequest {
  final String id;
  final int revision;
  final bool plan;
  PlanningRequest(this.id, this.revision, this.plan);
  Map<String, Object> toJson() => {
    'id': id,
    'revision': revision,
    'plan': plan,
  };
}

class Colony extends Residents {
  void Function(String building, Place kind, double effectiveSeconds)?
  onProductiveWork;
  int Function()? regionalMood;
  void Function(String shipment, bool exporting)? onFreightServed;
  void Function(String shipment, bool exporting, bool served)? onFreightExit;
  ScenarioKind scenario = ScenarioKind.free;
  final Set<int> scenarioCompleted = {};
  final List<StoryFact> stories = [];
  int nextStoryId = 1, incidentSpent = 0;
  double rainExposure = 0, nextStoryTick = 0;
  double get marshTravel =>
      [
            EnvironmentPreset.wetlands,
            EnvironmentPreset.saltCoast,
          ].contains(world.generation?.preset) &&
          rainExposure >= 20
      ? 2
      : 1.5;
  IncidentMode incidentMode = IncidentMode.peaceful;
  Incident? incident;
  final List<IncidentMemory> incidentHistory = [];
  int incidentRng = 1, nextIncidentId = 1;
  int hazardTicks = 0, fireSitesVisited = 0;
  int hazardUsLast = 0, hazardUsMax = 0;
  double nextIncidentAt = 450, incidentCooldown = 0, nextIncidentTick = 0;
  final Map<String, int> _shelters = {};
  int shelterReservations(String id) {
    _counts();
    return _shelters[id] ?? 0;
  }

  final Map<String, Visit> visits = {};
  final List<VisitMemory> visitHistory = [];
  final Map<String, int> guestCompletions = {};
  late int visitorRng;
  int nextVisitId = 1, visitorEarned = 0, visitorSpent = 0;
  double nextVisitorAt = 18;
  final Map<VisitorRole, double> visitorCooldown = {};
  Resident? actor(String? id) =>
      people[id] ??
      visits[id]?.actor ??
      (incident?.animal?.homeId == id ? incident?.animal : null);
  Iterable<Resident> get actors sync* {
    yield* people.values;
    if (incident?.animal != null) yield incident!.animal!;
    for (final v in visits.values) {
      yield v.actor;
    }
  }

  Iterable<(Resident, double)> get allVisible sync* {
    yield* visible;
    if (incident?.animal != null) yield (incident!.animal!, 1);
    for (final v in visits.values) {
      yield (v.actor, 1);
    }
  }

  RequestBook requests = RequestBook();
  int civicSpent = 0;
  final Map<String, String> civicDecisions = {};
  bool hasNeed(Resident r, Need need) {
    if (!r.active || !r.arrived || !people.containsKey(r.homeId)) return false;
    return switch (need) {
      Need.work =>
        r.workId == null &&
            [Reason.noWork, Reason.workFull].contains(r.workIssue) &&
            !workplaces.any(
              (p) =>
                  jobs(p.key) < kind(p.key)!.workCapacity &&
                  world.access(p.value) != null &&
                  world.pathRoute(r.cell, world.access(p.value)!) != null,
            ),
      Need.workAccess =>
        r.workId != null &&
            usable(r.workId) &&
            (placeAccess(r.workId) == null ||
                world.pathRoute(r.cell, placeAccess(r.workId)!) == null),
      Need.social =>
        [Reason.noLeisure, Reason.socialCut].contains(r.leisureIssue) &&
            !venues.any(
              (p) =>
                  staffed(p.key) &&
                  world.access(p.value) != null &&
                  world.pathRoute(r.cell, world.access(p.value)!) != null,
            ),
    };
  }

  Need? needFor(Resident r) {
    for (final need in [Need.workAccess, Need.work, Need.social]) {
      if (hasNeed(r, need) && requests.concern(r.homeId, need) == null) {
        return need;
      }
    }
    return null;
  }

  bool acceptRequest(String resident, Need need) {
    final r = people[resident];
    return r != null &&
        hasNeed(r, need) &&
        requests.accept(resident, need, clock) != null;
  }

  bool cancelRequest(String id) => requests.close(id, clock, resolved: false);
  bool _requestExperience(Resident r, {required bool social}) {
    if (requests.open.isEmpty) return false;
    var helped = false;
    for (final request in requests.forResident(r.homeId)) {
      if ((request.need == Need.social) == social &&
          requests.close(request.id, clock, resolved: true)) {
        helped = true;
      }
    }
    return helped;
  }

  String? get civicHost => !marketDay
      ? null
      : people.values
            .where(
              (r) =>
                  r.activity == Activity.working &&
                  connected(r) &&
                  kind(r.workId) == Place.market &&
                  placeAccess(r.workId) == r.cell,
            )
            .map((r) => r.homeId)
            .firstOrNull;
  bool civicAvailable(String? eventId) =>
      special?.id == eventId &&
      marketDay &&
      civicHost != null &&
      eligibleSpecial(SpecialKind.marketDay);
  bool chooseCivic(String eventId, {required bool sponsor}) {
    if (!civicAvailable(eventId) ||
        civicDecisions.containsKey(eventId) ||
        special!.civicChoice != null ||
        sponsor && session.money < Rules.civicCost) {
      return false;
    }
    special!.civicChoice = sponsor ? 'sponsored' : 'ordinary';
    civicDecisions[eventId] = special!.civicChoice!;
    if (sponsor) {
      session.money -= Rules.civicCost;
      civicSpent += Rules.civicCost;
    }
    return true;
  }

  final LandscapeSession session;
  late Navigation navigation;
  final Queue<PlanningRequest> planning = Queue();
  final Set<String> _queued = {};
  int planned = 0, discardedPlans = 0;
  int? _planningRevision;
  SpecialEvent? special;
  late int specialRng;
  int nextSpecialId = 1, tradeEarned = 0;
  double nextSpecialAt = 12;
  late Climate climate;
  Season get season => climate.calendar.season(clock);
  WeatherKind get weather => climate.at(clock);
  bool get raining => weather == WeatherKind.rain;
  bool get marketDay =>
      special?.kind == SpecialKind.marketDay && special!.active(clock);
  bool _countsDirty = true;
  final Map<String, int> _jobs = {}, _workers = {}, _visitors = {};
  void _counts() {
    if (!_countsDirty) return;
    _countsDirty = false;
    _jobs.clear();
    _workers.clear();
    _visitors.clear();
    _shelters.clear();
    for (final r in people.values) {
      if (r.safetyRole == 'evacuating' && r.safetyTarget != null) {
        _shelters.update(r.safetyTarget!, (v) => v + 1, ifAbsent: () => 1);
      }
      if (r.workId != null) {
        _jobs.update(r.workId!, (v) => v + 1, ifAbsent: () => 1);
      }
      if (r.activity == Activity.working && r.workId != null) {
        _workers.update(r.workId!, (v) => v + 1, ifAbsent: () => 1);
      }
      if ([Activity.socialising, Activity.gathering].contains(r.activity) &&
          r.destination != null) {
        _visitors.update(r.destination!, (v) => v + 1, ifAbsent: () => 1);
      }
    }
  }

  Place? kind(String? id) =>
      world.places[id] == null ? null : world.at(world.places[id]!).place;
  Iterable<MapEntry<String, Cell>> get workplaces => world.places.entries.where(
    (p) => world.at(p.value).place!.workCapacity > 0,
  );
  Iterable<MapEntry<String, Cell>> get venues => world.places.entries.where(
    (p) => world.at(p.value).place!.visitorCapacity > 0,
  );
  int capacity(String? id) => socialCapacity(id);
  bool staffed(String? id) =>
      usable(id) &&
      (![Place.market, Place.inn].contains(kind(id)) ||
          id != null && workers(id) > 0);
  void _enqueue(Resident r, bool plan) {
    if (!r.active || !_queued.add(r.homeId)) return;
    planning.add(PlanningRequest(r.homeId, navigation.revision, plan));
  }

  @override
  void decide(Resident r) => _enqueue(r, false);
  @override
  void plan(Resident r) => _enqueue(r, true);
  void _planBatch() {
    for (var i = 0; i < Rules.planningBudget && planning.isNotEmpty; i++) {
      final request = planning.removeFirst();
      _queued.remove(request.id);
      final r = actor(request.id);
      if (r == null) continue;
      if (request.revision != navigation.revision) {
        discardedPlans++;
        _enqueue(r, request.plan);
        continue;
      }
      if (r.moving) continue;
      _planningRevision = request.revision;
      if (incident?.animal?.homeId == r.homeId) {
        _planAnimal(r);
      } else if (r.safetyRole != null) {
        _planSafety(r);
      } else if (visits.containsKey(r.homeId)) {
        _planVisitor(visits[r.homeId]!);
      } else if (request.plan) {
        _planNow(r);
      } else {
        _decideNow(r);
      }
      _planningRevision = null;
      planned++;
    }
  }

  void rebaseNavigation(int previousRevision) {
    final old = navigation.revision;
    navigation.invalidate(
      world,
      raining: raining,
      weatherCost: weather.travel,
      marshCost: marshTravel,
    );
    navigation.revision = math.max(navigation.revision, previousRevision + 1);
    final requests = planning.toList();
    planning.clear();
    for (final p in requests) {
      planning.add(
        PlanningRequest(
          p.id,
          p.revision == old ? navigation.revision : p.revision,
          p.plan,
        ),
      );
    }
  }

  @override
  bool go(Resident r, Cell target, Activity activity) {
    _countsDirty = true;
    final result = super.go(r, target, activity);
    _countsDirty = true;
    return result;
  }

  @override
  double walkingSpeed(Resident r, Cell target) =>
      (incident?.animal?.homeId == r.homeId
          ? 2.5
          : super.walkingSpeed(r, target)) *
      (r.trait == Trait.brisk ? Rules.briskSpeed : 1) /
      edgeTravelCost(
        world,
        r.cell,
        target,
        raining,
        weatherCost: weather.travel,
        marshCost: marshTravel,
      );
  @override
  bool gardenSegment(Resident r, Cell target) =>
      world.at(r.cell).road == RoadKind.gardenPath ||
      world.at(target).road == RoadKind.gardenPath;
  @override
  void traversed(Resident r, Cell target, bool garden) {
    if (garden && r.gardenDay != day) r.gardenDay = day;
  }

  double clock = 0, remainder = 0, grantFraction = 0, incomeFraction = 0;
  int speed = 1, eventRng, nextEventId = 1, checkedEvening = -1;
  int cooldownDay = 0, grantEarned = 0, workEarned = 0;
  bool migrated = false;
  Gathering? event;
  final Map<String, double> tended = {};
  Colony(this.session)
    : eventRng = 1 + (session.world.seed + 4098) % 2147483646,
      super(session.world, initialise: false) {
    incidentMode = world.generation == null
        ? IncidentMode.peaceful
        : IncidentMode.standard;
    incidentRng = 1 + (world.seed + 95189) % 2147483646;
    session.unsafeBuilding = (id) =>
        burning(id) ||
        flooded(id) ||
        [
              IncidentKind.wildlife,
              IncidentKind.raiders,
            ].contains(incident?.kind) &&
            incident!.state == 'grazing' &&
            incident!.target == id;
    session.clock = () => clock;
    visitorRng = 1 + (world.seed + 67211) % 2147483646;
    climate = Climate(
      world.seed,
      environment: world.generation?.preset ?? EnvironmentPreset.plains,
    );
    navigation = Navigation(world, rain: raining, weatherCost: weather.travel);
    specialRng = 1 + (world.seed + 8938) % 2147483646;
    super.reconcile(world, restored: true);
    _syncJobs();
  }
  int get day => (clock / Rules.day).floor() + 1;
  double get timeOfDay => clock % Rules.day;
  String get phase => timeOfDay < Rules.morningEnd
      ? 'Morning'
      : timeOfDay < Rules.eveningStart
      ? 'Day'
      : timeOfDay < Rules.nightStart
      ? 'Evening'
      : 'Night';
  bool get workPeriod => timeOfDay >= 3 && timeOfDay < Rules.eveningStart;
  Iterable<MapEntry<String, Cell>> places(Place kind) =>
      world.places.entries.where((p) => world.at(p.value).place == kind);
  bool connected(Resident r) =>
      world.cottages[r.homeId] != null &&
      world.access(world.cottages[r.homeId]!) != null;
  Cell? placeAccess(String? id) =>
      id == null || !usable(id) || !world.places.containsKey(id)
      ? null
      : world.access(world.places[id]!);
  List<Cell>? route(Cell start, Cell end) {
    routeSearches++;
    return navigation.route(start, end, expectedRevision: _planningRevision);
  }

  @override
  List<Cell>? routeBetween(Cell from, Cell to) => route(from, to);
  double travelDistance(Cell from, Cell to) {
    routeSearches++;
    return navigation.distance(from, to, expectedRevision: _planningRevision);
  }

  int occupancy(String id) {
    _counts();
    return (_visitors[id] ?? 0) +
        visits.values
            .where(
              (v) =>
                  v.actor.destination == id &&
                  v.actor.activity == Activity.socialising,
            )
            .length;
  }

  int residentCapacity(String id) =>
      capacity(id) -
      (incident?.kind == IncidentKind.caravan &&
              incident!.target == id &&
              incident!.state != 'retreating'
          ? 1
          : 0) -
      visits.values
          .where(
            (v) =>
                (v.inn != null && !v.rested ? v.inn : v.venue) == id &&
                (!v.leaving || v.inn != null && !v.rested) &&
                v.actor.activity != Activity.socialising,
          )
          .length;

  int jobs(String id) {
    _counts();
    return _jobs[id] ?? 0;
  }

  int workers(String id) {
    _counts();
    return _workers[id] ?? 0;
  }

  @override
  void reconcile(Landscape next, {bool restored = false, bool reset = false}) {
    if (!identical(next, world)) {
      final before = world.completeBridgeCells.length,
          after = next.completeBridgeCells.length;
      if (before != after) {
        remember(
          'crossing',
          after < before
              ? 'A bank-to-bank crossing was broken.'
              : 'A complete bank-to-bank crossing opened.',
          (after < before
                  ? world.completeBridgeCells
                  : next.completeBridgeCells)
              .first,
        );
      }
      if (incident?.kind == IncidentKind.flood &&
          incident!.state == 'flooding') {
        next.closeRoads(
          incident!.flood
              .where(
                (c) =>
                    next.at(c).path &&
                    next.at(c).bridge == null &&
                    next.at(c).road != RoadKind.stone,
              )
              .toSet(),
        );
      } else {
        next.closeRoads({});
      }
    }
    if (!identical(next, world)) {
      navigation.invalidate(
        next,
        raining: raining,
        weatherCost: weather.travel,
        marshCost: marshTravel,
      );
    }
    _countsDirty = true;
    final known = records.map((r) => r.homeId).toSet();
    super.reconcile(next, restored: restored, reset: reset);
    if (incident != null) {
      incident!.passageHeld &= world.activePaths.any((c) => c.x >= 20);
    }
    planning.removeWhere(
      (request) =>
          !people.containsKey(request.id) &&
          !visits.containsKey(request.id) &&
          incident?.animal?.homeId != request.id,
    );
    _queued.removeWhere(
      (id) =>
          !people.containsKey(id) &&
          !visits.containsKey(id) &&
          incident?.animal?.homeId != id,
    );
    for (final r in people.values) {
      if (!known.contains(r.homeId)) {
        r.bornAt = clock;
        if (!restored) {
          r.palette = variation(world.seed, world.cottages[r.homeId]!, 8) % 4;
          r.peopleKind = PeopleKind.arrival(world.seed, r.homeId);
          r.bodyId = r.peopleKind.body(r.homeId);
          if (r.peopleKind != PeopleKind.human) {
            r.name = r.peopleKind.newName(r.homeId);
          }
        }
      }
      if (r.activity == Activity.home ||
          r.activity == Activity.waiting ||
          r.activity == Activity.resting) {
        r.timer = math.min(r.timer, .25);
      }
    }
    _syncJobs();
    _tickEvent();
    // invalidate occupied activities before the immediate post-edit checkpoint.
    for (final r in people.values) {
      if ([
            Activity.working,
            Activity.socialising,
            Activity.socialWaiting,
            Activity.gathering,
          ].contains(r.activity) &&
          (!connected(r) || placeAccess(r.destination) != r.cell)) {
        r.workIssue = r.activity == Activity.working
            ? r.workId == null
                  ? Reason.noWork
                  : Reason.workCut
            : r.workIssue;
        r.leisureIssue = r.activity == Activity.working
            ? r.leisureIssue
            : Reason.socialCut;
        r.activity = Activity.waiting;
        r.destination = null;
        r.socialProgress = 0;
        r.timer = 0;
        plan(r);
      }
    }
    _countsDirty = true;
    for (final request in requests.open.values.toList()) {
      if (!people.containsKey(request.resident)) cancelRequest(request.id);
    }
    _reconcileVisitors();
    _reconcileSafety();
    _problems();
  }

  void _syncJobs() {
    for (final r in records) {
      if (r.workId != null && !world.places.containsKey(r.workId)) {
        r.workId = null;
        r.workIssue = Reason.noWork;
      }
      if (r.destination != null &&
          !world.places.containsKey(r.destination) &&
          !(r.safetyRole == 'fire' &&
              world.cottages.containsKey(r.destination))) {
        r.destination = null;
      }
    }
    final used = <String, int>{};
    _countsDirty = true;
    for (final r in people.values) {
      final garden = world.places[r.workId];
      if (garden == null ||
          world.at(garden).place!.workCapacity == 0 ||
          (used[r.workId] ?? 0) >= world.at(garden).place!.workCapacity) {
        r.workId = null;
      } else {
        used.update(r.workId!, (n) => n + 1, ifAbsent: () => 1);
      }
    }
  }

  bool _assign(Resident r) {
    if (r.workId != null && !workSourceAvailable(r.workId)) {
      r.workId = null;
      _countsDirty = true;
    }
    if (r.workId != null) {
      final access = placeAccess(r.workId);
      if (access == null || route(r.cell, access) == null) {
        r.workIssue = Reason.workCut;
        return false;
      }
      r.workIssue = Reason.none;
      return true;
    }
    var reachable = false;
    final candidates = <(String, double)>[];
    for (final p in workplaces) {
      if (!workSourceAvailable(p.key)) continue;
      final access = placeAccess(p.key);
      if (access == null) continue;
      final distance = travelDistance(r.cell, access);
      if (!distance.isFinite) continue;
      reachable = true;
      if (jobs(p.key) < kind(p.key)!.workCapacity) {
        candidates.add((p.key, distance));
      }
    }
    candidates.sort((a, b) {
      if (r.trait == Trait.greenThumb) {
        final preference = (kind(a.$1) == Place.garden ? 0 : 1).compareTo(
          kind(b.$1) == Place.garden ? 0 : 1,
        );
        if (preference != 0) return preference;
      }
      if (r.peopleKind == PeopleKind.imp) {
        final preferred = (kind(a.$1) == Place.workshop ? 0 : 1).compareTo(
          kind(b.$1) == Place.workshop ? 0 : 1,
        );
        if (preferred != 0) return preferred;
      }
      final protection = (kind(a.$1)?.protective == true ? 0 : 1).compareTo(
        kind(b.$1)?.protective == true ? 0 : 1,
      );
      if (protection != 0) return protection;
      return a.$2 != b.$2 ? a.$2.compareTo(b.$2) : a.$1.compareTo(b.$1);
    });
    if (candidates.isEmpty) {
      r.workIssue = reachable ? Reason.workFull : Reason.noWork;
      return false;
    }
    r.workId = candidates.first.$1;
    _countsDirty = true;
    r.workProgress = 0;
    r.workIssue = Reason.none;
    return true;
  }

  Landscape? _timberWorld;
  final Map<Cell, int> _timber = {};
  int timberCount(String id) {
    if (!identical(_timberWorld, world)) {
      _timberWorld = world;
      _timber.clear();
    }
    final cell = world.places[id];
    return cell == null
        ? 0
        : _timber.putIfAbsent(cell, () => nearbyTimber(world, cell));
  }

  bool workSourceAvailable(String? id) =>
      kind(id) != Place.lumberCamp || id != null && timberCount(id) > 0;

  bool shopAvailable(Resident r) => places(Place.market).any(
    (p) =>
        p.key != r.workId &&
        world.access(p.value) != null &&
        occupancy(p.key) < world.at(p.value).place!.visitorCapacity &&
        workers(p.key) > 0,
  );
  bool _seekSocial(Resident r, {bool shopping = false}) {
    final candidates = <(String, double)>[];
    for (final p in venues) {
      if (shopping && kind(p.key) != Place.market) continue;
      final access = placeAccess(p.key);
      if (access == null) continue;
      if ([Place.market, Place.inn].contains(kind(p.key)) &&
          (!staffed(p.key) || r.workId == p.key)) {
        continue;
      }
      final distance = travelDistance(r.cell, access);
      if (distance.isFinite) candidates.add((p.key, distance));
    }
    if (candidates.isEmpty) {
      final reachable = venues.any(
        (p) =>
            world.access(p.value) != null &&
            travelDistance(r.cell, world.access(p.value)!).isFinite,
      );
      r.leisureIssue = venues.isEmpty
          ? Reason.noLeisure
          : reachable
          ? Reason.marketUnstaffed
          : Reason.socialCut;
      return false;
    }
    int preference(String id) {
      if (climate.covered(clock) && kind(id) == Place.inn) return 0;
      if (visits.values.any(
        (v) => v.role == VisitorRole.storyteller && !v.leaving && v.venue == id,
      )) {
        return -1;
      }
      if ((climate.covered(clock) || r.peopleKind == PeopleKind.djinn) &&
          kind(id) == Place.hall) {
        return 0;
      }
      if (season == Season.summer &&
          kind(id) == Place.green &&
          !climate.covered(clock)) {
        return 0;
      }
      if (kind(id) == Place.market &&
          (marketDay ||
              r.tradeDay != day &&
                  ResidentIdentity.hash(r.homeId, 379) % 2 == 0)) {
        return 1;
      }
      return kind(id) == Place.market && r.tradeDay == day ? 3 : 2;
    }

    candidates.sort((a, b) {
      final p = preference(a.$1).compareTo(preference(b.$1));
      return p != 0
          ? p
          : a.$2 != b.$2
          ? a.$2.compareTo(b.$2)
          : a.$1.compareTo(b.$1);
    });
    final available = candidates.where(
      (p) => occupancy(p.$1) < residentCapacity(p.$1),
    );
    final chosen = available.isEmpty ? candidates.first : available.first;
    r.destination = chosen.$1;
    r.leisureIssue = Reason.none;
    r.socialProgress = 0;
    return go(r, placeAccess(r.destination)!, Activity.socialTravel);
  }

  bool goHome(Resident r) {
    if (!homeUsable(r)) {
      _safetyStart(r, 'evacuating');
      return true;
    }
    _countsDirty = true;
    final home = world.cottages[r.homeId];
    if (home == null) return false;
    final target = world.access(home) ?? world.localAccess(home, from: r.cell);
    r.destination = null;
    if (target != null && go(r, target, Activity.returning)) return true;
    r.route.clear();
    r.activity = Activity.waiting;
    r.timer = Rules.searchInterval;
    return false;
  }

  bool get storytelling => visits.values.any(
    (v) =>
        v.role == VisitorRole.storyteller &&
        !v.leaving &&
        v.actor.activity == Activity.socialising,
  );

  void _decideNow(Resident r) {
    if (!homeUsable(r)) {
      _safetyStart(r, 'evacuating');
      return;
    }
    if (!connected(r)) {
      goHome(r);
      return;
    }
    final gathering = event;
    if (gathering != null &&
        gathering.state != 'ended' &&
        gathering.invited.contains(r.homeId)) {
      final target = placeAccess(gathering.green);
      if (target != null) {
        r.destination = gathering.green;
        if (go(r, target, Activity.gatheringTravel)) return;
      }
    }
    if (phase == 'Night') {
      final home = world.access(world.cottages[r.homeId]!);
      if (r.cell == home) {
        r.activity = Activity.resting;
        r.timer = Rules.homeDwell;
      } else {
        goHome(r);
      }
      return;
    }
    if (storytelling &&
        clock - r.lastSocial > climate.socialRepeat(clock) &&
        (r.workId == null || clock - r.lastWork < Rules.workRetryAfter) &&
        _seekSocial(r)) {
      return;
    }
    if (workPeriod &&
        (clock - r.lastWork >
                (kind(r.workId) == Place.market ? 6 : Rules.workRetryAfter) ||
            clock - r.lastSocial < climate.socialRepeat(clock))) {
      if (_assign(r)) {
        r.destination = r.workId;
        if (go(r, placeAccess(r.workId)!, Activity.workTravel)) {
          if (r.activity == Activity.workTravel) r.say(Reason.offWork);
          return;
        }
      }
    }
    if ((r.tradeDay != day ||
            marketDay && clock - r.lastSocial > climate.socialRepeat(clock)) &&
        shopAvailable(r) &&
        _seekSocial(r, shopping: true)) {
      return;
    }
    if (workPeriod &&
        r.workId == null &&
        places(Place.market).isNotEmpty &&
        clock - r.lastSocial > climate.socialRepeat(clock) &&
        _seekSocial(r)) {
      return;
    }
    if ((phase == 'Evening' || r.lastWork >= r.bornAt) &&
        clock - r.lastSocial > climate.socialRepeat(clock)) {
      if (_seekSocial(r)) return;
    }
    // a fruitless work search also identifies a useful leisure opportunity.
    if (workPeriod &&
        r.workId == null &&
        r.lastSocial < r.bornAt &&
        _seekSocial(r)) {
      return;
    }
    r.activity = Activity.home;
    r.timer = Rules.searchInterval + (r.homeId.codeUnits.last % 4) * .25;
    goHome(r);
  }

  void _planNow(Resident r) {
    if (!homeUsable(r)) {
      if (r.arrived) {
        _safetyStart(r, 'evacuating');
      } else {
        r.activity = Activity.waiting;
        r.timer = 3;
      }
      return;
    }
    r.replanAfterEdge = false;
    if (!r.active) return;
    if (!r.arrived) {
      final home = world.cottages[r.homeId];
      final target = home == null
          ? null
          : world.access(home) ?? world.localAccess(home, from: r.cell);
      if (target != null && go(r, target, Activity.arriving)) return;
      r.route.clear();
      r.activity = Activity.waiting;
      r.timer = Rules.searchInterval;
      return;
    }
    if (r.activity == Activity.working) {
      final access = placeAccess(r.workId);
      if (connected(r) &&
          access == r.cell &&
          jobs(r.workId!) <= (kind(r.workId)?.workCapacity ?? 0)) {
        return;
      }
      r.workIssue = Reason.workCut;
      goHome(r);
      return;
    }
    if ([
      Activity.socialising,
      Activity.socialWaiting,
      Activity.gathering,
    ].contains(r.activity)) {
      if (connected(r) && placeAccess(r.destination) == r.cell) return;
      r.leisureIssue = Reason.socialCut;
      goHome(r);
      return;
    }
    if ([
      Activity.workTravel,
      Activity.socialTravel,
      Activity.gatheringTravel,
    ].contains(r.activity)) {
      final activity = r.activity, target = placeAccess(r.destination);
      if (connected(r) && target != null && go(r, target, activity)) return;
      if (activity == Activity.workTravel) {
        r.workIssue = Reason.workCut;
      } else {
        r.leisureIssue = Reason.socialCut;
      }
      goHome(r);
      return;
    }
    goHome(r);
  }

  @override
  void reached(Resident r) {
    if (incident?.animal?.homeId == r.homeId) {
      _animalReached(r);
      return;
    }
    if (r.safetyRole != null) {
      _safetyReached(r);
      return;
    }
    if (visits.containsKey(r.homeId)) {
      _visitorReached(visits[r.homeId]!);
      return;
    }
    _countsDirty = true;
    if (r.activity == Activity.workTravel) {
      if (connected(r) &&
          placeAccess(r.workId) == r.cell &&
          workers(r.workId!) < (kind(r.workId)?.workCapacity ?? 0)) {
        r.activity = Activity.working;
        _countsDirty = true;
        r.workIssue = Reason.none;
      } else {
        r.workIssue = Reason.workCut;
        goHome(r);
      }
      return;
    }
    if (r.activity == Activity.socialTravel ||
        r.activity == Activity.gatheringTravel) {
      final gathering = r.activity == Activity.gatheringTravel;
      if (occupancy(r.destination!) >= residentCapacity(r.destination!)) {
        r.activity = Activity.socialWaiting;
        r.timer = Rules.socialWait;
        r.leisureIssue = Reason.socialFull;
      } else {
        r.activity = gathering ? Activity.gathering : Activity.socialising;
        _countsDirty = true;
        r.socialProgress = 0;
        r.leisureIssue = Reason.none;
      }
      return;
    }
    super.reached(r);
    if (r.arrived && world.cottages.containsKey(r.homeId)) {
      session.condition(r.homeId).uses++;
    }
    r.timer = Rules.homeDwell + r.homeId.codeUnits.last % 5 * Rules.homeStagger;
  }

  Map<String, int> contributions(Resident r) {
    final cut =
        !connected(r) &&
        r.connectionLostAt >= 0 &&
        clock - r.connectionLostAt >= Rules.connectionGrace;
    final grace = clock - r.bornAt < Rules.grace;
    return {
      'Home connection': cut ? Rules.housingPenalty : 0,
      'Recent work': clock - r.lastWork < Rules.recent
          ? Rules.workBenefit
          : !cut &&
                !grace &&
                r.workIssue != Reason.none &&
                r.activity != Activity.workTravel
          ? Rules.missingPenalty
          : 0,
      'Recent leisure': clock - r.lastSocial < Rules.recent
          ? Rules.socialBenefit +
                (r.trait == Trait.sociable ? Rules.sociableBenefit : 0)
          : !cut &&
                !grace &&
                r.leisureIssue != Reason.none &&
                r.activity != Activity.socialTravel
          ? Rules.missingPenalty
          : 0,
      if (!homeUsable(r)) 'Displaced': -8,
      if (r.exposureUntil > clock) 'Incident exposure': r.sheltered ? -2 : -8,
      'Garden path': r.gardenDay == day ? Rules.gardenPathBenefit : 0,
      'Gathering': r.eventBenefitUntil > clock ? Rules.gatheringBenefit : 0,
      if (r.visitorBenefitUntil > clock) 'Traveller story': 4,
      if ((regionalMood?.call() ?? 0) > 0) 'Regional supply': regionalMood!(),
      if (requests.mood(r.homeId, clock) > 0)
        'Kept promise': Rules.requestBenefit,
      if (r.civicBenefitUntil > clock) 'Market hospitality': Rules.civicBenefit,
    };
  }

  int happiness(Resident r) =>
      (Rules.moodBase + contributions(r).values.fold<int>(0, (a, b) => a + b))
          .clamp(10, 90);
  int get happinessMean => people.isEmpty
      ? Rules.moodBase
      : (people.values.map(happiness).reduce((a, b) => a + b) / people.length)
            .round();
  String get mood => happinessMean >= 70
      ? 'Content'
      : happinessMean >= 45
      ? 'Settled'
      : 'Uneasy';

  void _problems() {
    final workAvailable = workplaces.any(
      (p) =>
          world.access(p.value) != null &&
          jobs(p.key) < kind(p.key)!.workCapacity,
    );
    final socialReachable = venues.any((p) => world.access(p.value) != null);
    for (final r in people.values) {
      if (!connected(r)) {
        if (r.connectionLostAt < 0) r.connectionLostAt = clock;
        r.problem = Reason.homeCut;
      } else {
        r.connectionLostAt = -1;
        if (r.workId != null && placeAccess(r.workId) == null) {
          r.workIssue = Reason.workCut;
        }
        if ([Reason.noWork, Reason.workFull].contains(r.workIssue) &&
            workAvailable) {
          r.workIssue = Reason.none;
        }
        if (r.workIssue == Reason.workCut && placeAccess(r.workId) != null) {
          r.workIssue = Reason.none;
        }
        if (r.leisureIssue == Reason.socialCut &&
            world.activePaths.contains(r.cell) &&
            socialReachable) {
          r.leisureIssue = Reason.none;
        }
        if (r.leisureIssue == Reason.noLeisure && socialReachable) {
          r.leisureIssue = Reason.none;
        }
        if ([Reason.noWork, Reason.workFull].contains(r.workIssue) &&
            r.workId != null) {
          r.workIssue = Reason.none;
        }
        if (r.safetyRole != null) {
          r.problem = r.activity == Activity.responding
              ? r.safetyRole == 'fire'
                    ? Reason.fightingFire
                    : Reason.guarding
              : r.activity == Activity.sheltering
              ? Reason.sheltered
              : incident == null
              ? Reason.homeDamaged
              : r.activity == Activity.emergencyWaiting
              ? r.safetyRole == 'evacuating' && !threatened(r.cell)
                    ? Reason.safetyRoad
                    : Reason.responseCut
              : Reason.incidentWarning;
          continue;
        }
        if (!homeUsable(r)) {
          r.problem = Reason.homeDamaged;
          continue;
        }
        if (r.workId != null && (status(r.workId)?.health ?? 100) < 100) {
          r.problem = Reason.workDamaged;
          continue;
        }
        r.problem = r.workIssue == Reason.workCut
            ? r.workIssue
            : r.leisureIssue == Reason.socialFull
            ? r.leisureIssue
            : workPeriod && r.workIssue != Reason.none
            ? r.workIssue
            : r.leisureIssue;
      }
    }
  }

  int _earn(
    double amount, {
    bool grant = false,
    bool trade = false,
    bool visitor = false,
  }) {
    final fraction = amount + (grant ? grantFraction : incomeFraction);
    final whole = (fraction + 1e-9).floor();
    if (grant) {
      grantFraction = math.max(0, fraction - whole);
      grantEarned += whole;
    } else {
      incomeFraction = math.max(0, fraction - whole);
      if (visitor) {
        visitorEarned += whole;
      } else if (trade) {
        tradeEarned += whole;
      } else {
        workEarned += whole;
      }
    }
    session.money += whole;
    return whole;
  }

  @override
  void update(double elapsed) {
    // one clock, bounded foreground delta; speed never affects bubble display time.
    remainder += elapsed.clamp(0.0, .05) * speed;
    while (remainder >= .05 - 1e-9) {
      remainder = math.max(0, remainder - .05);
      _step(.05);
    }
    if (remainder < 1e-9) remainder = 0;
  }

  // the region owns elapsed time and calls this once per shared fixed tick.
  void advanceRegionalTick() => _step(.05);

  void _step(double dt) {
    clock += dt;
    _worldBoundary(dt);
    _specialBoundary();
    _visitorBoundary();
    _incidentOpportunity();
    _countsDirty = true;
    _earn(dt * Rules.grantPerDay / Rules.day, grant: true);
    final workingBefore = people.values
        .where((r) => r.activity == Activity.working)
        .map((r) => r.homeId)
        .toSet();
    final socialBefore = people.values
        .where((r) => r.activity == Activity.socialising)
        .map((r) => r.homeId)
        .toSet();
    super.update(dt);
    _countsDirty = true;
    _planBatch();
    _updateVisitors(dt);
    _updateSafety(dt);
    for (final r in people.values) {
      if (r.safetyRole != null) {
        _safetyPersonStep(r, dt);
        continue;
      }
      if (!r.arrived || r.route.isNotEmpty || r.activity == Activity.settling) {
        continue;
      }
      if (r.activity == Activity.working) {
        if (!workSourceAvailable(r.workId) ||
            !connected(r) ||
            placeAccess(r.workId) != r.cell ||
            workers(r.workId!) > (kind(r.workId)?.workCapacity ?? 0)) {
          r.workIssue = Reason.workCut;
          goHome(r);
          continue;
        }
        if (!workingBefore.contains(r.homeId)) continue;
        final duration = kind(r.workId)!.workDuration;
        final productive = math.min(dt, duration - r.workProgress);
        final bonus = kind(r.workId) == Place.garden
            ? climate.farmMultiplier(
                clock,
                harvest:
                    special?.kind == SpecialKind.harvest &&
                    special!.active(clock),
              )
            : 1.0;
        r.earned += _earn(
          productive *
              kind(r.workId)!.income *
              bonus *
              (status(r.workId)?.output ?? 1),
        );
        session.condition(r.workId!).productive += productive;
        if (productive > 0) {
          onProductiveWork?.call(
            r.workId!,
            kind(r.workId)!,
            productive * bonus * (status(r.workId)?.output ?? 1),
          );
        }
        if (kind(r.workId)!.protective && productive > 0) {
          r.dutyAt = clock;
          r.dutyBuilding = r.workId;
        }
        r.workedSeconds += productive;
        final helped = productive > 0 && _requestExperience(r, social: false);
        if (helped) r.say(Reason.requestThanks);
        r.workProgress += productive;
        if (r.workProgress >= duration - 1e-9) {
          r.workProgress = 0;
          r.lastWork = clock;
          r.say(helped ? Reason.requestThanks : Reason.workDone);
          tended[r.workId!] = clock;
          if (event != null &&
              event!.state != 'ended' &&
              event!.invited.contains(r.homeId)) {
            decide(r);
            continue;
          }
          if (phase != 'Night' && storytelling && _seekSocial(r)) continue;
          if (phase != 'Night' &&
              r.tradeDay != day &&
              shopAvailable(r) &&
              _seekSocial(r, shopping: true)) {
            continue;
          }
          if (phase != 'Night' && _seekSocial(r)) continue;
          goHome(r);
        }
      } else if (r.activity == Activity.socialising) {
        if (!connected(r) || placeAccess(r.destination) != r.cell) {
          r.leisureIssue = Reason.socialCut;
          goHome(r);
          continue;
        }
        if (!staffed(r.destination)) {
          r.activity = Activity.socialWaiting;
          r.timer = 4;
          r.socialProgress = 0;
          r.leisureIssue = Reason.marketUnstaffed;
          _countsDirty = true;
          continue;
        }
        if (!socialBefore.contains(r.homeId)) continue;
        _attendStory(r, dt);
        r.socialProgress += dt;
        if (r.socialProgress >= Rules.socialDuration) {
          r.lastSocial = clock;
          if (r.destination != null) session.condition(r.destination!).uses++;
          final helped = _requestExperience(r, social: true);
          if (kind(r.destination) == Place.market) {
            r.lastShopping = clock;
            if (r.tradeDay != day) {
              r.tradeDay = day;
              if (marketDay &&
                  special!.civicChoice == 'sponsored' &&
                  special!.civicAwarded.length < Rules.civicGuests &&
                  special!.civicAwarded.add(r.homeId)) {
                r.civicBenefitUntil = clock + Rules.civicDuration;
              }
              _earn(
                (Rules.tradeCredit + (marketDay ? Rules.marketDayBonus : 0))
                    .toDouble(),
                trade: true,
              );
            }
          }
          r.say(helped ? Reason.requestThanks : Reason.socialDone);
          goHome(r);
        }
      } else if (r.activity == Activity.socialWaiting) {
        r.timer -= dt;
        if (placeAccess(r.destination) != r.cell || !connected(r)) {
          r.leisureIssue = Reason.socialCut;
          goHome(r);
        } else if (occupancy(r.destination!) <
                residentCapacity(r.destination!) &&
            staffed(r.destination)) {
          r.activity =
              event != null &&
                  event!.state != 'ended' &&
                  event!.invited.contains(r.homeId) &&
                  r.destination == event!.green
              ? Activity.gathering
              : Activity.socialising;
          _countsDirty = true;
          r.leisureIssue = Reason.none;
          r.socialProgress = 0;
        } else if (r.timer <= 0) {
          goHome(r);
          r.leisureIssue = Reason.none;
        }
      } else if (r.activity == Activity.waiting ||
          r.activity == Activity.resting) {
        r.timer -= dt;
        if (r.timer <= 0) {
          r.timer = Rules.searchInterval;
          if (!r.arrived) {
            plan(r);
          } else {
            decide(r);
          }
        }
      } else if (r.activity == Activity.gathering) {
        if (event == null ||
            event!.state == 'ended' ||
            !connected(r) ||
            placeAccess(event!.green) != r.cell) {
          goHome(r);
        } else if (event!.state == 'active') {
          _attendStory(r, dt);
          r.socialProgress += dt;
          if (r.socialProgress >= Rules.socialDuration) r.lastSocial = clock;
        }
      }
    }
    if (phase == 'Evening' && checkedEvening != day) {
      checkedEvening = day;
      _considerGathering();
    }
    _tickEvent();
    _problems();
  }

  void _considerGathering() {
    if (day < cooldownDay || event != null && event!.state != 'ended') return;
    final active = people.values
        .where((r) => r.arrived && connected(r))
        .toList();
    if (active.length < Rules.gatheringMinimum ||
        active.where((r) => clock - r.lastWork < Rules.recent).length < 2 ||
        active.where((r) => clock - r.lastSocial < Rules.recent).length < 2) {
      return;
    }
    for (final p in venues.where((p) => kind(p.key) != Place.market)) {
      final access = placeAccess(p.key);
      if (access == null) continue;
      final invited = active
          .where((r) => route(r.cell, access) != null)
          .take(Rules.gatheringGuests)
          .toList();
      if (invited.length < Rules.gatheringMinimum) continue;
      eventRng = eventRng * 48271 % 2147483647;
      if (eventRng / 2147483647 >= Rules.gatheringChance) return;
      event = Gathering(
        'gathering-${nextEventId++}',
        p.key,
        invited.map((r) => r.homeId).toList(),
        clock + Rules.gatheringWait,
      );
      cooldownDay = day + Rules.gatheringCooldownDays;
      for (final r in invited) {
        // finish productive bouts and valid current edges before joining.
        if (r.route.isEmpty && r.activity != Activity.working) decide(r);
      }
      return;
    }
  }

  void _tickEvent() {
    final e = event;
    if (e == null || e.state == 'ended') return;
    final access = placeAccess(e.green);
    if (access == null) {
      _endGathering();
      return;
    }
    final arrived = people.values
        .where(
          (r) =>
              e.invited.contains(r.homeId) &&
              r.activity == Activity.gathering &&
              r.cell == access &&
              r.route.isEmpty &&
              connected(r),
        )
        .toList();
    if (e.state == 'inviting' && arrived.length >= Rules.gatheringStart) {
      e.state = 'active';
      e.ends = clock + Rules.gatheringDuration;
    }
    if (e.state == 'active') {
      for (final r in arrived) {
        if (e.awarded.add(r.homeId)) {
          r.awardedEvent = e.id;
          r.eventBenefitUntil = clock + Rules.day / 2;
          r.say(Reason.gathering);
        }
      }
      if (arrived.length < Rules.gatheringStart || clock >= e.ends) {
        _endGathering();
      }
    } else if (clock >= e.deadline) {
      _endGathering();
    }
  }

  void _endGathering() {
    final e = event;
    if (e == null) return;
    e.state = 'ended';
    for (final r in people.values) {
      if (e.invited.contains(r.homeId) &&
          [Activity.gathering, Activity.gatheringTravel].contains(r.activity)) {
        goHome(r);
      }
    }
  }

  bool eligibleSpecial(SpecialKind kind) => switch (kind) {
    SpecialKind.rain => people.isNotEmpty,
    SpecialKind.harvest => places(
      Place.garden,
    ).any((p) => world.access(p.value) != null),
    SpecialKind.marketDay =>
      places(
            Place.market,
          ).any((p) => world.access(p.value) != null && workers(p.key) > 0) &&
          people.values.any(
            (r) =>
                r.arrived &&
                ![
                  Activity.working,
                  Activity.workTravel,
                  Activity.gathering,
                  Activity.gatheringTravel,
                  Activity.pending,
                  Activity.arriving,
                ].contains(r.activity) &&
                connected(r),
          ),
  };
  void _weatherCostsChanged() {
    navigation.invalidate(
      world,
      raining: raining,
      weatherCost: weather.travel,
      marshCost: marshTravel,
    );
    for (final r in actors.where((r) => r.route.isNotEmpty)) {
      if (r.route.length > 1) r.route.removeRange(1, r.route.length);
      r.replanAfterEdge = true;
    }
  }

  bool beginSpecial(SpecialKind kind, {bool fixture = false}) {
    if (!fixture && clock + 1e-9 < nextSpecialAt) return false;
    if (special?.active(clock) == true || !eligibleSpecial(kind)) return false;
    final wasWeather = weather;
    special = SpecialEvent(
      'special-${nextSpecialId++}',
      kind,
      clock,
      clock + Rules.specialDuration,
    );
    nextSpecialAt = special!.ends + Rules.specialCooldown;
    if (kind == SpecialKind.rain) climate.legacyShower(clock, special!.ends);
    if (weather != wasWeather) _weatherCostsChanged();
    return true;
  }

  void _specialBoundary() {
    if (navigation.weatherCost != weather.travel ||
        navigation.marshCost != marshTravel) {
      _weatherCostsChanged();
    }
    if (clock + 1e-9 < nextSpecialAt || special?.active(clock) == true) return;
    if (event != null && event!.state != 'ended') {
      nextSpecialAt = clock + 5;
      return;
    }
    final eligible = [
      if (eligibleSpecial(SpecialKind.harvest))
        ...List.filled(
          season == Season.autumn
              ? 3
              : season == Season.spring
              ? 2
              : 1,
          SpecialKind.harvest,
        ),
      if (eligibleSpecial(SpecialKind.marketDay)) SpecialKind.marketDay,
    ];
    if (eligible.isEmpty) {
      nextSpecialAt = clock + 5;
      return;
    }
    specialRng = specialRng * 48271 % 2147483647;
    beginSpecial(eligible[(specialRng * eligible.length) ~/ 2147483647]);
  }

  Map<String, Object?> _personJson(Resident r) => {
    'id': r.homeId,
    'active': r.active,
    'palette': r.palette,
    'bodyId': r.bodyId,
    'people': r.peopleKind.id,
    'visitorBenefitUntil': r.visitorBenefitUntil,
    'safetyRole': r.safetyRole,
    'safetyTarget': r.safetyTarget,
    'safetyIncident': r.safetyIncident,
    'dutyBuilding': r.dutyBuilding,
    'dutyAt': r.dutyAt,
    'responseSince': r.responseSince,
    'exposureUntil': r.exposureUntil,
    'sheltered': r.sheltered,
    'name': r.name,
    'trait': r.trait.name,
    'gardenDay': r.gardenDay,
    'tradeDay': r.tradeDay,
    'lastShopping': r.lastShopping,
    'segmentSpeed': r.segmentSpeed,
    'segmentGarden': r.segmentGarden,
    'cell': [r.cell.x, r.cell.y],
    'x': r.x,
    'y': r.y,
    'activity': r.activity.name,
    'route': [
      for (final c in r.route) [c.x, c.y],
    ],
    'timer': r.timer,
    'arrived': r.arrived,
    'destination': r.destination,
    'workId': r.workId,
    'bornAt': r.bornAt,
    'lastWork': r.lastWork,
    'lastSocial': r.lastSocial,
    'workProgress': r.workProgress,
    'socialProgress': r.socialProgress,
    'workedSeconds': r.workedSeconds,
    'earned': r.earned,
    'connectionLostAt': r.connectionLostAt,
    'eventBenefitUntil': r.eventBenefitUntil,
    if (r.civicBenefitUntil >= 0) 'civicBenefitUntil': r.civicBenefitUntil,
    'awardedEvent': r.awardedEvent,
    'workIssue': r.workIssue.name,
    'leisureIssue': r.leisureIssue.name,
    'replan': r.replanAfterEdge,
    'settle': r.settleTarget == null
        ? null
        : [r.settleTarget!.x, r.settleTarget!.y],
  };
  String encode() => jsonEncode({
    ...world.toJson(),
    'schema': 6,
    'colony': {
      'emergence': emergenceJson(),
      'preparedness': preparednessJson(),
      'climate': climate.toJson(clock),
      'visitors': visitorsJson(),
      if (requests.records.isNotEmpty) 'requests': requests.toJson(),
      if (civicSpent > 0) 'civicSpent': civicSpent,
      if (civicDecisions.isNotEmpty) 'civicDecisions': civicDecisions,
      'navigationRevision': navigation.revision,
      'planning': planning.map((p) => p.toJson()).toList(),
      'special': special?.toJson(),
      'specialRng': specialRng,
      'nextSpecialId': nextSpecialId,
      'nextSpecialAt': nextSpecialAt,
      'tradeEarned': tradeEarned,
      'money': session.money,
      'unlimited': session.unlimited,
      'clock': clock,
      'remainder': remainder,
      'speed': speed,
      'grantFraction': grantFraction,
      'incomeFraction': incomeFraction,
      'grantEarned': grantEarned,
      'workEarned': workEarned,
      'migrated': migrated,
      'eventRng': eventRng,
      'nextEventId': nextEventId,
      'checkedEvening': checkedEvening,
      'cooldownDay': cooldownDay,
      'event': event?.toJson(),
      'residents': records.map(_personJson).toList(),
    },
  });
  SaveSnapshot snapshot() => SaveSnapshot(session.revision, encode());
  bool persist(SaveSnapshot snapshot, void Function(String) write) {
    if (snapshot.revision != session.revision || snapshot.text != encode()) {
      return false;
    }
    write(snapshot.text);
    return true;
  }

  factory Colony.decode(String text) {
    final data = jsonDecode(text), world = Landscape.decode(text);
    final colony = Colony(LandscapeSession(world));
    if (data['schema'] < 5) colony.incidentMode = IncidentMode.peaceful;
    if (![3, 4, 5, 6].contains(data['schema'])) {
      colony.migrated = true;
      return colony;
    }
    colony.planning.clear();
    colony._queued.clear();
    final d = data['colony'];
    if (d is! Map) throw const FormatException('Colony checkpoint is missing.');
    double number(Map d, String key, [double min = -10000, double max = 1e12]) {
      final v = d[key];
      if (v is! num || !v.isFinite || v < min || v > max) {
        throw FormatException('Invalid $key.');
      }
      return v.toDouble();
    }

    int integer(Map d, String key, [int min = 0, int max = 1000000000]) {
      final v = d[key];
      if (v is! int || v < min || v > max) {
        throw FormatException('Invalid $key.');
      }
      return v;
    }

    bool flag(Map d, String key) {
      final v = d[key];
      if (v is! bool) throw FormatException('Invalid $key.');
      return v;
    }

    String? identity(dynamic v) {
      if (v == null) return null;
      if (v is! String || !RegExp(r'^[A-Za-z0-9_-]{1,64}$').hasMatch(v)) {
        throw const FormatException('Invalid identity.');
      }
      return v;
    }

    Cell cell(dynamic v) {
      if (v is! List || v.length != 2 || v[0] is! int || v[1] is! int) {
        throw const FormatException('Invalid position.');
      }
      final c = Cell(v[0], v[1]);
      if (!world.contains(c)) {
        throw const FormatException('Position outside map.');
      }
      return c;
    }

    Reason reason(dynamic v) => Reason.values.firstWhere(
      (r) => r.name == v,
      orElse: () => throw const FormatException('Invalid reason.'),
    );
    colony.session.money = integer(d, 'money', 0, 1000000000000);
    colony.session.unlimited = flag(d, 'unlimited');
    colony.migrated = flag(d, 'migrated');
    colony.clock = number(d, 'clock', 0);
    colony.remainder = number(d, 'remainder', 0, .05);
    if (data['schema'] >= 5) {
      colony.restorePreparedness(d['preparedness']);
    } else {
      colony.nextIncidentAt = colony.clock + 450;
    }
    if (data['schema'] >= 6) colony.restoreEmergence(d['emergence']);
    colony.climate = data['schema'] >= 4
        ? Climate.decode(
            d['climate'],
            world.seed,
            colony.clock,
            world.generation?.preset ?? EnvironmentPreset.plains,
          )
        : Climate(
            world.seed,
            calendar: Calendar(anchorDay: (colony.clock / Rules.day).floor()),
            environment: world.generation?.preset ?? EnvironmentPreset.plains,
          );
    colony.speed = integer(d, 'speed', 0, 16);
    if (![0, 1, 2, 4, if (data['schema'] >= 4) 16].contains(colony.speed)) {
      throw const FormatException('Invalid speed.');
    }
    colony.grantFraction = number(d, 'grantFraction', 0, 1);
    colony.incomeFraction = number(d, 'incomeFraction', 0, 1);
    colony.grantEarned = integer(d, 'grantEarned', 0, 1000000000000);
    colony.workEarned = integer(d, 'workEarned', 0, 1000000000000);
    colony.eventRng = integer(d, 'eventRng', 1, 2147483646);
    colony.nextEventId = integer(d, 'nextEventId', 1);
    colony.checkedEvening = integer(d, 'checkedEvening', -1);
    colony.cooldownDay = integer(d, 'cooldownDay');
    final rows = d['residents'];
    if (rows is! List || rows.length > 20000) {
      throw const FormatException('Invalid population.');
    }
    colony.people.clear();
    colony.inactive.clear();
    final ids = <String>{};
    for (final row in rows) {
      if (row is! Map) throw const FormatException('Invalid resident.');
      final id = identity(row['id']);
      if (id == null || !ids.add(id)) {
        throw const FormatException('Duplicate resident.');
      }
      final r = Resident(
        id,
        cell(row['cell']),
        Activity.values.firstWhere(
          (a) => a.name == row['activity'],
          orElse: () => throw const FormatException('Invalid activity.'),
        ),
      );
      r.active = flag(row, 'active');
      if (data['schema'] >= 5) {
        r.safetyRole = identity(row['safetyRole']);
        r.safetyTarget = identity(row['safetyTarget']);
        r.safetyIncident = identity(row['safetyIncident']);
        r.dutyBuilding = identity(row['dutyBuilding']);
        r.dutyAt = number(row, 'dutyAt', -10000, colony.clock);
        r.responseSince = number(row, 'responseSince', -1, colony.clock);
        r.exposureUntil = number(row, 'exposureUntil', -1, colony.clock + 75);
        r.sheltered = flag(row, 'sheltered');
      }

      r.palette = integer(row, 'palette', 0, 3);
      r.bodyId = identity(row['bodyId']) ?? r.bodyId;
      if (data['schema'] >= 4) {
        r.peopleKind = PeopleKind.decode(row['people']);
        if (!r.peopleKind.compatible(r.bodyId)) {
          throw const FormatException('People artwork mismatch.');
        }
        r.visitorBenefitUntil = number(
          row,
          'visitorBenefitUntil',
          -1,
          colony.clock + 75,
        );
      }
      if (row.containsKey('name')) {
        if (row['name'] is! String ||
            !RegExp(r'^[A-Za-z ]{1,48}$').hasMatch(row['name'])) {
          throw const FormatException('Invalid resident name.');
        }
        r.name = row['name'];
      }
      if (row.containsKey('trait')) {
        r.trait = Trait.values.firstWhere(
          (t) => t.name == row['trait'],
          orElse: () => throw const FormatException('Invalid trait.'),
        );
      }
      if (row.containsKey('gardenDay')) {
        r.gardenDay = integer(row, 'gardenDay', -1, colony.day);
      }
      if (row.containsKey('tradeDay')) {
        r.tradeDay = integer(row, 'tradeDay', -1, colony.day);
      }
      if (row.containsKey('lastShopping')) {
        r.lastShopping = number(row, 'lastShopping', -10000, colony.clock);
      }
      if (row.containsKey('segmentSpeed')) {
        r.segmentSpeed = number(row, 'segmentSpeed', 0, 20);
      }
      if (row.containsKey('segmentGarden')) {
        r.segmentGarden = flag(row, 'segmentGarden');
      }
      r.x = number(row, 'x', 0, world.width.toDouble());
      r.y = number(row, 'y', 0, world.height.toDouble());
      r.timer = number(row, 'timer');
      r.arrived = flag(row, 'arrived');
      r.destination = identity(row['destination']);
      r.workId = identity(row['workId']);
      r.bornAt = number(row, 'bornAt', 0, colony.clock);
      r.lastWork = number(row, 'lastWork', -10000, colony.clock);
      r.lastSocial = number(row, 'lastSocial', -10000, colony.clock);
      r.workProgress = number(
        row,
        'workProgress',
        0,
        data['schema'] >= 5 ? 60 : 35,
      );
      r.socialProgress = number(row, 'socialProgress', 0, Rules.day);
      r.workedSeconds = number(row, 'workedSeconds', 0);
      r.earned = integer(row, 'earned', 0, 1000000000000);
      r.connectionLostAt = number(row, 'connectionLostAt', -1, colony.clock);
      r.eventBenefitUntil = number(
        row,
        'eventBenefitUntil',
        -1,
        colony.clock + Rules.day,
      );
      r.awardedEvent = identity(row['awardedEvent']);
      if (row.containsKey('civicBenefitUntil')) {
        r.civicBenefitUntil = number(
          row,
          'civicBenefitUntil',
          0,
          colony.clock + Rules.civicDuration,
        );
      }
      r.workIssue = reason(row['workIssue']);
      r.leisureIssue = reason(row['leisureIssue']);
      if (![
            Reason.none,
            Reason.noWork,
            Reason.workFull,
            Reason.workCut,
          ].contains(r.workIssue) ||
          ![
            Reason.none,
            Reason.noLeisure,
            Reason.socialFull,
            Reason.socialCut,
            Reason.marketUnstaffed,
          ].contains(r.leisureIssue)) {
        throw const FormatException('Invalid need reason.');
      }
      r.replanAfterEdge = flag(row, 'replan');
      r.settleTarget = row['settle'] == null ? null : cell(row['settle']);
      final path = row['route'];
      if (path is! List || path.length > world.lots.length) {
        throw const FormatException('Invalid route.');
      }
      var previous = r.cell;
      for (final entry in path) {
        final c = cell(entry);
        if (!world.roadStep(previous, c)) {
          throw const FormatException('Invalid path edge.');
        }
        r.route.add(c);
        previous = c;
      }
      if (r.active != world.cottages.containsKey(id)) {
        throw const FormatException('Resident ownership mismatch.');
      }
      if (r.workId != null &&
          (!world.places.containsKey(r.workId) ||
              world.at(world.places[r.workId]!).place!.workCapacity == 0)) {
        throw const FormatException('Invalid workplace.');
      }
      if (r.workId != null &&
          r.workProgress > colony.kind(r.workId)!.workDuration) {
        throw const FormatException('Invalid productive progress.');
      }
      if (r.destination != null &&
          !world.places.containsKey(r.destination) &&
          !(r.safetyRole == 'fire' &&
              world.cottages.containsKey(r.destination))) {
        throw const FormatException('Invalid destination.');
      }
      if ([Activity.working, Activity.workTravel].contains(r.activity) &&
          (r.workId == null || r.destination != r.workId)) {
        throw const FormatException(
          'Work activity has no matching assignment.',
        );
      }
      if ([
            Activity.socialTravel,
            Activity.socialising,
            Activity.socialWaiting,
            Activity.gatheringTravel,
            Activity.gathering,
          ].contains(r.activity) &&
          (r.destination == null ||
              world.at(world.places[r.destination]!).place!.visitorCapacity ==
                  0)) {
        throw const FormatException('Social activity has no green.');
      }
      if ([
            Activity.working,
            Activity.socialising,
            Activity.socialWaiting,
            Activity.gathering,
          ].contains(r.activity) &&
          (!r.arrived ||
              r.route.isNotEmpty ||
              colony.placeAccess(r.destination) != r.cell ||
              !colony.connected(r))) {
        throw const FormatException(
          'Activity is not at a reachable destination.',
        );
      }
      if (!r.active &&
          (r.route.isNotEmpty ||
              r.destination != null ||
              r.activity != Activity.waiting)) {
        throw const FormatException('Inactive resident has an active journey.');
      }
      if ((r.x - r.cell.x - .5).abs() + (r.y - r.cell.y - .5).abs() > 1.02) {
        throw const FormatException('Invalid resident position.');
      }
      (r.active ? colony.people : colony.inactive)[id] = r;
    }
    if (!world.cottages.keys.every(colony.people.containsKey)) {
      throw const FormatException('Missing resident.');
    }
    colony.requests = RequestBook.decode(
      d['requests'],
      ids,
      colony.people.keys.toSet(),
      colony.clock,
    );
    if (d.containsKey('civicSpent')) {
      colony.civicSpent = integer(d, 'civicSpent', 0, 1000000000000);
      if (colony.civicSpent % Rules.civicCost != 0) {
        throw const FormatException('Invalid civic spending.');
      }
    }
    for (final p in colony.workplaces) {
      if (colony.jobs(p.key) > colony.kind(p.key)!.workCapacity) {
        throw const FormatException('Garden capacity exceeded.');
      }
    }
    for (final p in colony.venues) {
      if (colony.occupancy(p.key) > colony.capacity(p.key)) {
        throw const FormatException('Green capacity exceeded.');
      }
    }
    final eventData = d['event'];
    if (eventData != null) {
      if (eventData is! Map ||
          !['inviting', 'active', 'ended'].contains(eventData['state'])) {
        throw const FormatException('Invalid gathering.');
      }
      List<String> participants(dynamic values) {
        if (values is! List || values.length > Rules.gatheringGuests) {
          throw const FormatException('Invalid guests.');
        }
        final names = values.map((v) => identity(v) ?? '').toList();
        if (names.toSet().length != names.length ||
            names.any((n) => !ids.contains(n))) {
          throw const FormatException('Invalid guest identity.');
        }
        return names;
      }

      final id = identity(eventData['id']),
          green = identity(eventData['green']);
      if (id == null || green == null) {
        throw const FormatException('Invalid event identity.');
      }
      final e = Gathering(
        id,
        green,
        participants(eventData['invited']),
        number(eventData, 'deadline'),
      );
      e.state = eventData['state'];
      e.ends = number(eventData, 'ends');
      e.awarded.addAll(participants(eventData['awarded']));
      if (!e.awarded.every(e.invited.contains) ||
          (e.state != 'ended' && colony.placeAccess(green) == null)) {
        throw const FormatException('Invalid gathering attendance.');
      }
      final eventNumber = int.tryParse(e.id.replaceFirst('gathering-', ''));
      if (!e.id.startsWith('gathering-') ||
          eventNumber == null ||
          eventNumber < 1 ||
          eventNumber >= colony.nextEventId ||
          e.awarded.any(
            (id) =>
                [...colony.records]
                    .firstWhere((r) => r.homeId == id)
                    .awardedEvent !=
                e.id,
          ) ||
          (e.state != 'ended' &&
              world.at(world.places[green]!).place!.visitorCapacity == 0)) {
        throw const FormatException(
          'Invalid gathering identity or reward record.',
        );
      }
      colony.event = e;
    }
    if (d.containsKey('navigationRevision')) {
      colony.navigation.revision = integer(d, 'navigationRevision');
    }
    colony.nextSpecialAt = d.containsKey('nextSpecialAt')
        ? number(d, 'nextSpecialAt', colony.clock)
        : colony.clock + 12;
    if (d.containsKey('specialRng')) {
      colony.specialRng = integer(d, 'specialRng', 1, 2147483646);
    }
    if (d.containsKey('nextSpecialId')) {
      colony.nextSpecialId = integer(d, 'nextSpecialId', 1);
    }
    if (d.containsKey('tradeEarned')) {
      colony.tradeEarned = integer(d, 'tradeEarned', 0, 1000000000000);
    }
    final special = d['special'];
    if (special != null) {
      if (special is! Map) {
        throw const FormatException('Invalid special event.');
      }
      final kind = SpecialKind.values.firstWhere(
        (k) => k.name == special['kind'],
        orElse: () => throw const FormatException('Invalid special kind.'),
      );
      final id = identity(special['id']);
      if (id == null) throw const FormatException('Missing special identity.');
      final start = number(special, 'starts', 0, colony.clock),
          end = number(
            special,
            'ends',
            start,
            colony.clock + Rules.specialDuration,
          );
      if (!RegExp(r'^special-[1-9][0-9]*$').hasMatch(id) ||
          int.parse(id.split('-').last) >= colony.nextSpecialId ||
          (end - start - Rules.specialDuration).abs() > 1e-6) {
        throw const FormatException(
          'Invalid special event identity or duration.',
        );
      }
      colony.special = SpecialEvent(id, kind, start, end);
      final choice = special['civicChoice'],
          awards = special['civicAwarded'] ?? [];
      if (choice != null &&
              (kind != SpecialKind.marketDay ||
                  !['sponsored', 'ordinary'].contains(choice)) ||
          awards is! List ||
          awards.length > Rules.civicGuests ||
          awards.toSet().length != awards.length ||
          awards.any((v) => !ids.contains(v)) ||
          awards.isNotEmpty && choice != 'sponsored' ||
          choice == 'sponsored' && colony.civicSpent < Rules.civicCost) {
        throw const FormatException('Invalid civic event decision.');
      }
      colony.special!.civicChoice = choice;
      colony.special!.civicAwarded.addAll(awards.cast<String>());
      for (final id in awards) {
        final r = colony.records.firstWhere((r) => r.homeId == id);
        if (r.civicBenefitUntil < start + Rules.civicDuration ||
            r.civicBenefitUntil > end + Rules.civicDuration) {
          throw const FormatException('Invalid civic participation benefit.');
        }
      }
    }
    final decisions = d['civicDecisions'];
    if (decisions != null) {
      if (decisions is! Map || decisions.length >= colony.nextSpecialId) {
        throw const FormatException('Invalid civic decision history.');
      }
      for (final entry in decisions.entries) {
        if (entry.key is! String ||
            !RegExp(r'^special-[1-9][0-9]*$').hasMatch(entry.key) ||
            int.parse(entry.key.split('-').last) >= colony.nextSpecialId ||
            !['sponsored', 'ordinary'].contains(entry.value)) {
          throw const FormatException('Invalid past civic decision.');
        }
        colony.civicDecisions[entry.key] = entry.value;
      }
      final current = colony.special;
      if (current != null &&
          colony.civicDecisions[current.id] != current.civicChoice) {
        throw const FormatException(
          'Civic decision history disagrees with this event.',
        );
      }
    } else if (colony.special?.civicChoice != null) {
      colony.civicDecisions[colony.special!.id] = colony.special!.civicChoice!;
    }
    if (colony.civicDecisions.values.where((v) => v == 'sponsored').length *
            Rules.civicCost >
        colony.civicSpent) {
      throw const FormatException('Civic spending is missing.');
    }
    if (data['schema'] == 3 &&
        colony.special?.kind == SpecialKind.rain &&
        colony.special!.active(colony.clock)) {
      colony.climate.legacyShower(colony.special!.starts, colony.special!.ends);
    }
    if (data['schema'] >= 4) colony.restoreVisitors(d['visitors']);
    colony.navigation.rain = colony.raining;
    colony.navigation.weatherCost = colony.weather.travel;
    colony.navigation.marshCost = colony.marshTravel;
    final queued = d['planning'] ?? [];
    if (queued is! List ||
        queued.length >
            colony.people.length +
                colony.visits.length +
                (colony.incident?.animal == null ? 0 : 1)) {
      throw const FormatException('Invalid planning queue.');
    }
    for (final request in queued) {
      if (request is! Map) {
        throw const FormatException('Invalid planning request.');
      }
      final id = identity(request['id']);
      if (colony.actor(id) == null || !colony._queued.add(id!)) {
        throw const FormatException('Invalid queued person.');
      }
      colony.planning.add(
        PlanningRequest(
          id,
          integer(request, 'revision', 0, colony.navigation.revision),
          flag(request, 'plan'),
        ),
      );
    }
    colony._countsDirty = true;
    colony.validatePreparedness();
    colony._problems();
    return colony;
  }
}
