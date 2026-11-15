import 'dart:convert';
import 'dart:math' as math;

import 'model.dart';
import 'reasons.dart';
import 'residents.dart';
import 'rules.dart';

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

class Colony extends Residents {
  final LandscapeSession session;
  double clock = 0, remainder = 0, grantFraction = 0, incomeFraction = 0;
  int speed = 1, eventRng, nextEventId = 1, checkedEvening = -1;
  int cooldownDay = 0, grantEarned = 0, workEarned = 0;
  bool migrated = false;
  Gathering? event;
  final Map<String, double> tended = {};
  Colony(this.session)
    : eventRng = session.world.seed + 4099,
      super(session.world, initialise: false) {
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
    return world.pathRoute(start, end);
  }

  int occupancy(String id) => people.values
      .where(
        (r) =>
            r.destination == id &&
            [Activity.socialising, Activity.gathering].contains(r.activity),
      )
      .length;
  int jobs(String id) => people.values.where((r) => r.workId == id).length;
  int workers(String id) => people.values
      .where((r) => r.workId == id && r.activity == Activity.working)
      .length;

  @override
  void reconcile(Landscape next, {bool restored = false, bool reset = false}) {
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
    for (final r in people.values) {
      final garden = world.places[r.workId];
      if (garden == null ||
          world.at(garden).place != Place.garden ||
          (used[r.workId] ?? 0) >= Rules.gardenCapacity) {
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
    final candidates = <(String, int)>[];
    for (final p in places(Place.garden)) {
      final access = world.access(p.value);
      if (access == null) continue;
      final path = route(r.cell, access);
      if (path == null) continue;
      reachable = true;
      if (jobs(p.key) < Rules.gardenCapacity) {
        candidates.add((p.key, path.length));
      }
    }
    candidates.sort(
      (a, b) => a.$2 != b.$2 ? a.$2.compareTo(b.$2) : a.$1.compareTo(b.$1),
    );
    if (candidates.isEmpty) {
      r.workIssue = reachable ? Reason.workFull : Reason.noWork;
      return false;
    }
    r.workId = candidates.first.$1;
    r.workProgress = 0;
    r.workIssue = Reason.none;
    return true;
  }

  bool _seekSocial(Resident r) {
    final candidates = <(String, int)>[];
    for (final p in places(Place.green)) {
      final access = world.access(p.value);
      final path = access == null ? null : route(r.cell, access);
      if (path != null) candidates.add((p.key, path.length));
    }
    candidates.sort((a, b) => a.$2.compareTo(b.$2));
    if (candidates.isEmpty) {
      r.leisureIssue = places(Place.green).isEmpty
          ? Reason.noLeisure
          : Reason.socialCut;
      return false;
    }
    // travellers do not occupy a seat; full greens can be reached and waited at.
    final available = candidates.where(
      (c) => occupancy(c.$1) < Rules.greenCapacity,
    );
    final chosen = available.isEmpty ? candidates.first : available.first;
    r.destination = chosen.$1;
    r.leisureIssue = Reason.none;
    r.socialProgress = 0;
    return go(r, placeAccess(r.destination)!, Activity.socialTravel);
  }

  bool goHome(Resident r) {
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

  @override
  void decide(Resident r) {
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

  @override
  void plan(Resident r) {
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
          jobs(r.workId!) <= Rules.gardenCapacity) {
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
    if (r.activity == Activity.workTravel) {
      if (connected(r) &&
          placeAccess(r.workId) == r.cell &&
          workers(r.workId!) < Rules.gardenCapacity) {
        r.activity = Activity.working;
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
      if (occupancy(r.destination!) >= Rules.greenCapacity) {
        r.activity = Activity.socialWaiting;
        r.timer = Rules.socialWait;
        r.leisureIssue = Reason.socialFull;
      } else {
        r.activity = gathering ? Activity.gathering : Activity.socialising;
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
          ? Rules.socialBenefit
          : !cut &&
                !grace &&
                r.leisureIssue != Reason.none &&
                r.activity != Activity.socialTravel
          ? Rules.missingPenalty
          : 0,
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
            places(Place.garden).any(
              (p) =>
                  world.access(p.value) != null &&
                  jobs(p.key) < Rules.gardenCapacity,
            )) {
          r.workIssue = Reason.none;
        }
        if (r.workIssue == Reason.workCut && placeAccess(r.workId) != null) {
          r.workIssue = Reason.none;
        }
        if (r.leisureIssue == Reason.socialCut &&
            world.activePaths.contains(r.cell) &&
            places(Place.green).any((p) => world.access(p.value) != null)) {
          r.leisureIssue = Reason.none;
        }
        if (r.leisureIssue == Reason.noLeisure &&
            places(Place.green).any((p) => world.access(p.value) != null)) {
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

  int _earn(double amount, {bool grant = false}) {
    final fraction = amount + (grant ? grantFraction : incomeFraction);
    final whole = (fraction + 1e-9).floor();
    if (grant) {
      grantFraction = math.max(0, fraction - whole);
      grantEarned += whole;
    } else {
      incomeFraction = math.max(0, fraction - whole);
      workEarned += whole;
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
    for (final r in people.values) {
      if (!r.arrived || r.route.isNotEmpty || r.activity == Activity.settling) {
        continue;
      }
      if (r.activity == Activity.working) {
        if (!connected(r) ||
            placeAccess(r.workId) != r.cell ||
            workers(r.workId!) > Rules.gardenCapacity) {
          r.workIssue = Reason.workCut;
          goHome(r);
          continue;
        }
        if (!workingBefore.contains(r.homeId)) continue;
        final productive = math.min(dt, Rules.workDuration - r.workProgress);
        r.earned += _earn(productive * Rules.workIncomePerSecond);
        r.workedSeconds += productive;
        r.workProgress += productive;
        if (r.workProgress >= Rules.workDuration - 1e-9) {
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
        if (!socialBefore.contains(r.homeId)) continue;
        r.socialProgress += dt;
        if (r.socialProgress >= Rules.socialDuration) {
          r.lastSocial = clock;
          r.say(Reason.socialDone);
          goHome(r);
        }
      } else if (r.activity == Activity.socialWaiting) {
        r.timer -= dt;
        if (placeAccess(r.destination) != r.cell || !connected(r)) {
          r.leisureIssue = Reason.socialCut;
          goHome(r);
        } else if (occupancy(r.destination!) < Rules.greenCapacity) {
          r.activity =
              event != null &&
                  event!.state != 'ended' &&
                  event!.invited.contains(r.homeId) &&
                  r.destination == event!.green
              ? Activity.gathering
              : Activity.socialising;
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
    for (final p in places(Place.green)) {
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

  Map<String, Object?> _personJson(Resident r) => {
    'id': r.homeId,
    'active': r.active,
    'palette': r.palette,
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
      r.x = number(row, 'x', 0, world.width.toDouble());
      r.y = number(row, 'y', 0, world.height.toDouble());
      r.timer = number(row, 'timer');
      r.arrived = flag(row, 'arrived');
      r.destination = identity(row['destination']);
      r.workId = identity(row['workId']);
      r.bornAt = number(row, 'bornAt', 0, colony.clock);
      r.lastWork = number(row, 'lastWork', -10000, colony.clock);
      r.lastSocial = number(row, 'lastSocial', -10000, colony.clock);
      r.workProgress = number(row, 'workProgress', 0, Rules.workDuration);
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
              world.at(world.places[r.workId]!).place != Place.garden)) {
        throw const FormatException('Invalid workplace.');
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
              world.at(world.places[r.destination]!).place != Place.green)) {
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
    for (final p in colony.places(Place.garden)) {
      if (colony.jobs(p.key) > Rules.gardenCapacity) {
        throw const FormatException('Garden capacity exceeded.');
      }
    }
    for (final p in colony.places(Place.green)) {
      if (colony.occupancy(p.key) > Rules.greenCapacity) {
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
              world.at(world.places[green]!).place != Place.green)) {
        throw const FormatException(
          'Invalid gathering identity or reward record.',
        );
      }
      colony.event = e;
    }
    colony._problems();
    return colony;
  }
}
