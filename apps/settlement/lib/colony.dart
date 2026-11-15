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
  bool get raining =>
      special?.kind == SpecialKind.rain && special!.active(clock);
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
    for (final r in people.values) {
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
  int capacity(String? id) => kind(id)?.visitorCapacity ?? 0;
  bool staffed(String? id) =>
      kind(id) != Place.market || id != null && workers(id) > 0;
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
      final r = people[request.id];
      if (r == null) continue;
      if (request.revision != navigation.revision) {
        discardedPlans++;
        _enqueue(r, request.plan);
        continue;
      }
      if (r.moving) continue;
      _planningRevision = request.revision;
      if (request.plan) {
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
    navigation.invalidate(world, raining: raining);
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
      super.walkingSpeed(r, target) *
      (r.trait == Trait.brisk ? Rules.briskSpeed : 1) /
      edgeTravelCost(world, r.cell, target, raining);
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
    : eventRng = session.world.seed + 4099,
      super(session.world, initialise: false) {
    navigation = Navigation(world);
    specialRng = (world.seed + 8939) % 2147483647;
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
  Cell? placeAccess(String? id) => id == null || !world.places.containsKey(id)
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
    return _visitors[id] ?? 0;
  }

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
    if (!identical(next, world)) navigation.invalidate(next, raining: raining);
    _countsDirty = true;
    final known = records.map((r) => r.homeId).toSet();
    super.reconcile(next, restored: restored, reset: reset);
    for (final r in people.values) {
      if (!known.contains(r.homeId)) {
        r.bornAt = clock;
        if (!restored) {
          r.palette = variation(world.seed, world.cottages[r.homeId]!, 8) % 4;
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
    _problems();
  }

  void _syncJobs() {
    for (final r in records) {
      if (r.workId != null && !world.places.containsKey(r.workId)) {
        r.workId = null;
        r.workIssue = Reason.noWork;
      }
      if (r.destination != null && !world.places.containsKey(r.destination)) {
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
      final access = world.access(p.value);
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

  bool _seekSocial(Resident r) {
    final candidates = <(String, double)>[];
    for (final p in venues) {
      final access = world.access(p.value);
      if (access == null) continue;
      if (kind(p.key) == Place.market && !staffed(p.key)) continue;
      final distance = travelDistance(r.cell, access);
      if (distance.isFinite) candidates.add((p.key, distance));
    }
    if (candidates.isEmpty) {
      r.leisureIssue = venues.isEmpty ? Reason.noLeisure : Reason.socialCut;
      return false;
    }
    int preference(String id) {
      if (raining && kind(id) == Place.hall) return 0;
      if (kind(id) == Place.market &&
          r.tradeDay != day &&
          (marketDay || ResidentIdentity.hash(r.homeId, 379) % 2 == 0)) {
        return 1;
      }
      return 2;
    }

    candidates.sort((a, b) {
      final p = preference(a.$1).compareTo(preference(b.$1));
      return p != 0
          ? p
          : a.$2 != b.$2
          ? a.$2.compareTo(b.$2)
          : a.$1.compareTo(b.$1);
    });
    final available = candidates.where((p) => occupancy(p.$1) < capacity(p.$1));
    final chosen = available.isEmpty ? candidates.first : available.first;
    r.destination = chosen.$1;
    r.leisureIssue = Reason.none;
    r.socialProgress = 0;
    return go(r, placeAccess(r.destination)!, Activity.socialTravel);
  }

  bool goHome(Resident r) {
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

  void _decideNow(Resident r) {
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
    if (workPeriod &&
        (clock - r.lastWork > Rules.workRetryAfter ||
            clock - r.lastSocial < Rules.leisureRepeatAfter)) {
      if (_assign(r)) {
        r.destination = r.workId;
        if (go(r, placeAccess(r.workId)!, Activity.workTravel)) {
          if (r.activity == Activity.workTravel) r.say(Reason.offWork);
          return;
        }
      }
    }
    if ((phase == 'Evening' || r.lastWork >= r.bornAt) &&
        clock - r.lastSocial > Rules.leisureRepeatAfter) {
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
      if (occupancy(r.destination!) >= capacity(r.destination)) {
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
      'Garden path': r.gardenDay == day ? Rules.gardenPathBenefit : 0,
      'Gathering': r.eventBenefitUntil > clock ? Rules.gatheringBenefit : 0,
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

  int _earn(double amount, {bool grant = false, bool trade = false}) {
    final fraction = amount + (grant ? grantFraction : incomeFraction);
    final whole = (fraction + 1e-9).floor();
    if (grant) {
      grantFraction = math.max(0, fraction - whole);
      grantEarned += whole;
    } else {
      incomeFraction = math.max(0, fraction - whole);
      if (trade) {
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

  void _step(double dt) {
    clock += dt;
    _specialBoundary();
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
    for (final r in people.values) {
      if (!r.arrived || r.route.isNotEmpty || r.activity == Activity.settling) {
        continue;
      }
      if (r.activity == Activity.working) {
        if (!connected(r) ||
            placeAccess(r.workId) != r.cell ||
            workers(r.workId!) > (kind(r.workId)?.workCapacity ?? 0)) {
          r.workIssue = Reason.workCut;
          goHome(r);
          continue;
        }
        if (!workingBefore.contains(r.homeId)) continue;
        final duration = kind(r.workId)!.workDuration;
        final productive = math.min(dt, duration - r.workProgress);
        final bonus =
            kind(r.workId) == Place.garden &&
                special?.kind == SpecialKind.harvest &&
                special!.active(clock)
            ? 1.25
            : 1.0;
        r.earned += _earn(productive * kind(r.workId)!.income * bonus);
        r.workedSeconds += productive;
        r.workProgress += productive;
        if (r.workProgress >= duration - 1e-9) {
          r.workProgress = 0;
          r.lastWork = clock;
          r.say(Reason.workDone);
          tended[r.workId!] = clock;
          if (event != null &&
              event!.state != 'ended' &&
              event!.invited.contains(r.homeId)) {
            decide(r);
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
        r.socialProgress += dt;
        if (r.socialProgress >= Rules.socialDuration) {
          r.lastSocial = clock;
          if (kind(r.destination) == Place.market) {
            r.lastShopping = clock;
            if (r.tradeDay != day) {
              r.tradeDay = day;
              _earn(
                (Rules.tradeCredit + (marketDay ? Rules.marketDayBonus : 0))
                    .toDouble(),
                trade: true,
              );
            }
          }
          r.say(Reason.socialDone);
          goHome(r);
        }
      } else if (r.activity == Activity.socialWaiting) {
        r.timer -= dt;
        if (placeAccess(r.destination) != r.cell || !connected(r)) {
          r.leisureIssue = Reason.socialCut;
          goHome(r);
        } else if (occupancy(r.destination!) < capacity(r.destination) &&
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
      final access = world.access(p.value);
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
  bool beginSpecial(SpecialKind kind, {bool fixture = false}) {
    if (!fixture && clock + 1e-9 < nextSpecialAt) return false;
    if (special?.active(clock) == true || !eligibleSpecial(kind)) return false;
    final wasRain = raining;
    special = SpecialEvent(
      'special-${nextSpecialId++}',
      kind,
      clock,
      clock + Rules.specialDuration,
    );
    nextSpecialAt = special!.ends + Rules.specialCooldown;
    if (raining != wasRain) navigation.invalidate(world, raining: raining);
    return true;
  }

  void _specialBoundary() {
    if (navigation.rain != raining) {
      navigation.invalidate(world, raining: raining);
    }
    if (clock + 1e-9 < nextSpecialAt || special?.active(clock) == true) return;
    if (event != null && event!.state != 'ended') {
      nextSpecialAt = clock + 5;
      return;
    }
    final eligible = SpecialKind.values.where(eligibleSpecial).toList();
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
    'colony': {
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
    if (data['schema'] != 3) {
      colony.migrated = true;
      return colony;
    }
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
    colony.speed = integer(d, 'speed', 0, 4);
    if (![0, 1, 2, 4].contains(colony.speed)) {
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
      r.palette = integer(row, 'palette', 0, 3);
      r.bodyId = identity(row['bodyId']) ?? r.bodyId;
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
      r.workProgress = number(row, 'workProgress', 0, 35);
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
        if (!world.at(c).path || !previous.neighbours.contains(c)) {
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
      if (r.destination != null && !world.places.containsKey(r.destination)) {
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
    }
    colony.navigation.rain = colony.raining;
    final queued = d['planning'] ?? [];
    if (queued is! List || queued.length > colony.people.length) {
      throw const FormatException('Invalid planning queue.');
    }
    for (final request in queued) {
      if (request is! Map) {
        throw const FormatException('Invalid planning request.');
      }
      final id = identity(request['id']);
      if (!colony.people.containsKey(id) || !colony._queued.add(id!)) {
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
    colony._problems();
    return colony;
  }
}
