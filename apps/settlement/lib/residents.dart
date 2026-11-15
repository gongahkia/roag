import 'dart:math' as math;

import 'model.dart';
import 'reasons.dart';
import 'rules.dart';
import 'appearance.dart';
import 'resident_identity.dart';

enum Activity {
  pending,
  arriving,
  home,
  visiting,
  returning,
  waiting,
  settling,
  workTravel,
  working,
  socialTravel,
  socialising,
  socialWaiting,
  gatheringTravel,
  gathering,
  resting,
}

class Resident {
  final String homeId;
  bool active = true;
  int palette = 0;
  late String bodyId, name;
  late Trait trait;
  int gardenDay = -1, tradeDay = -1;
  double lastShopping = -10000;
  double segmentSpeed = 0;
  bool segmentGarden = false;
  String? workId;
  double bornAt = 0, lastWork = -10000, lastSocial = -10000;
  double workProgress = 0, socialProgress = 0, workedSeconds = 0;
  double connectionLostAt = -1, eventBenefitUntil = -1;
  double civicBenefitUntil = -1;
  int earned = 0, signalSerial = 0;
  String? awardedEvent;
  Reason problem = Reason.none, message = Reason.none;
  Reason workIssue = Reason.none, leisureIssue = Reason.none;
  void say(Reason reason) {
    message = reason;
    signalSerial++;
  }

  Cell cell;
  double x, y;
  Activity activity;
  final List<Cell> route = [];
  double timer = 0, flash = 0, age = 0;
  int trips = 0;
  String? destination;
  bool replanAfterEdge = false;
  bool arrived = false;
  Cell? settleTarget;
  Resident(this.homeId, this.cell, this.activity)
    : x = cell.x + .5,
      y = cell.y + .5 {
    bodyId = Appearance.assignBody(homeId);
    name = ResidentIdentity.name(homeId);
    trait = ResidentIdentity.trait(homeId);
    palette = homeId.codeUnits.last.isEven ? 0 : 1;
  }
  Reason get feedback => problem != Reason.none
      ? problem
      : message == Reason.offWork && activity != Activity.workTravel
      ? Reason.none
      : message;
  bool get visible => active && activity != Activity.pending;
  bool get moving => route.isNotEmpty || activity == Activity.settling;
  double facing = 1;
}

class _Departure {
  final Resident person;
  double remaining = .22;
  _Departure(this.person);
}

class Residents {
  Landscape world;
  final Map<String, Resident> people = {};
  final Map<String, Resident> inactive = {};
  Iterable<Resident> get records => [...people.values, ...inactive.values];
  final List<_Departure> _departures = [];
  int routeSearches = 0;
  Residents(this.world, {bool initialise = true}) {
    if (initialise) reconcile(world, restored: true);
  }
  Iterable<(Resident, double)> get visible sync* {
    for (final r in people.values) {
      if (r.visible) yield (r, 1);
    }
    for (final d in _departures) {
      yield (d.person, d.remaining / .22);
    }
  }

  int _style(String id) =>
      id.codeUnits.fold(17, (n, c) => (n * 31 + c) % 2147483647);
  double _idle(Resident r) => 2.3 + (_style(r.homeId) + r.trips * 7) % 19 * .13;
  List<Cell>? routeBetween(Cell from, Cell to) {
    routeSearches++;
    return world.pathRoute(from, to);
  }

  void reconcile(Landscape next, {bool restored = false, bool reset = false}) {
    world = next;
    if (reset) {
      people.clear();
      inactive.clear();
      _departures.clear();
    }
    final homes = world.cottages;
    for (final id in people.keys.toList()) {
      if (!homes.containsKey(id)) {
        final r = people.remove(id)!;
        r.route.clear();
        r.destination = null;
        if (r.visible) _departures.add(_Departure(r));
        r.active = false;
        r.segmentSpeed = 0;
        r.segmentGarden = false;
        r.activity = Activity.waiting;
        inactive[id] = r;
      }
    }
    var added = 0;
    for (final entry in homes.entries) {
      if (people.containsKey(entry.key)) continue;
      _departures.removeWhere((d) => d.person.homeId == entry.key);
      final retained = inactive.remove(entry.key);
      if (retained != null) {
        retained.active = true;
        people[entry.key] = retained;
        continue;
      }
      final access =
          world.access(entry.value) ?? world.localAccess(entry.value);
      final r = Resident(
        entry.key,
        restored && access != null ? access : world.arrival,
        restored ? Activity.home : Activity.pending,
      );
      r.timer = restored
          ? _idle(r)
          : math.min(added++ * Rules.arrivalStagger, Rules.arrivalCap);
      r.arrived = restored;
      people[entry.key] = r;
    }
    for (final r in people.values) {
      if (r.activity == Activity.pending) continue;
      if (r.destination != null &&
          !homes.containsKey(r.destination) &&
          !world.places.containsKey(r.destination)) {
        r.destination = null;
      }
      if (r.activity == Activity.settling) {
        if (r.settleTarget != null &&
            world.contains(r.settleTarget!) &&
            world.roadCell(r.settleTarget!)) {
          continue;
        }
      }
      if (!world.contains(r.cell) ||
          !world.roadCell(r.cell) ||
          (r.route.isEmpty &&
              ((r.x - r.cell.x - .5).abs() > .01 ||
                  (r.y - r.cell.y - .5).abs() > .01)) ||
          (r.route.isNotEmpty && !world.roadStep(r.cell, r.route.first))) {
        _settle(r);
        continue;
      }
      if (r.route.isNotEmpty) {
        // finish only the still-legal current edge, then plan on the new graph.
        final edge = r.route.first;
        r.route
          ..clear()
          ..add(edge);
        r.replanAfterEdge = true;
      } else if (r.activity != Activity.home ||
          world.access(homes[r.homeId]!) != r.cell) {
        plan(r);
      }
    }
  }

  void _settle(Resident r) {
    final candidates = <Cell>{
      r.cell,
      if (r.route.isNotEmpty) r.route.first,
      ...r.cell.neighbours,
    };
    final legal =
        candidates
            .where(
              (c) =>
                  world.contains(c) &&
                  world.roadCell(c) &&
                  (c.x + .5 - r.x).abs() + (c.y + .5 - r.y).abs() <= 1.01,
            )
            .toList()
          ..sort(
            (a, b) => ((a.x + .5 - r.x).abs() + (a.y + .5 - r.y).abs())
                .compareTo((b.x + .5 - r.x).abs() + (b.y + .5 - r.y).abs()),
          );
    r.route.clear();
    r.segmentSpeed = 0;
    r.segmentGarden = false;
    r.replanAfterEdge = false;
    r.destination = null;
    if (legal.isEmpty) {
      r.activity = Activity.waiting;
      r.settleTarget = null;
      return;
    }
    r.settleTarget = legal.first;
    r.activity = Activity.settling;
  }

  bool go(Resident r, Cell target, Activity activity) {
    final path = routeBetween(r.cell, target);
    if (path == null) return false;
    r.route
      ..clear()
      ..addAll(path.skip(1));
    r.activity = activity;
    if (r.route.isEmpty) reached(r);
    return true;
  }

  void plan(Resident r) {
    r.replanAfterEdge = false;
    final home = world.cottages[r.homeId];
    if (home == null) return;
    if (!r.arrived) {
      final target = world.access(home);
      if (target != null && go(r, target, Activity.arriving)) return;
    } else if (r.activity == Activity.visiting &&
        r.destination != null &&
        world.access(home) != null) {
      final other = world.cottages[r.destination];
      final target = other == null ? null : world.access(other);
      if (target != null && go(r, target, Activity.visiting)) return;
    }
    final access = world.access(home);
    if (access != null && go(r, access, Activity.returning)) return;
    final local = world.localAccess(home, from: r.cell);
    if (local != null &&
        go(r, local, r.arrived ? Activity.returning : Activity.arriving)) {
      return;
    }
    r.route.clear();
    r.activity = Activity.waiting;
  }

  void reached(Resident r) {
    if (r.activity == Activity.arriving) {
      r.flash = 1.0;
      r.arrived = true;
    }
    if (r.activity == Activity.visiting) {
      r.timer = .8;
      r.activity = Activity.returning;
    } else {
      r.activity = Activity.home;
      r.timer = _idle(r);
      r.destination = null;
    }
  }

  void decide(Resident r) {
    r.timer = _idle(r);
  }

  void update(double elapsed) {
    // tab pauses discard excess time; there is no queue of timed callbacks.
    final dt = elapsed.clamp(0.0, .05);
    for (final d in _departures) {
      d.remaining -= dt;
    }
    _departures.removeWhere((d) => d.remaining <= 0);
    for (final r in people.values) {
      r.age += dt;
      r.flash = math.max(0, r.flash - dt);
      if (r.activity == Activity.pending) {
        r.timer -= dt;
        final home = world.cottages[r.homeId];
        final target = home == null ? null : world.access(home);
        if (r.timer <= 0 && target != null) {
          r.cell = world.arrival;
          r.x = r.cell.x + .5;
          r.y = r.cell.y + .5;
          plan(r);
        }
        continue;
      }
      if (r.activity == Activity.settling) {
        final target = r.settleTarget;
        if (target != null && _move(r, target, dt, Rules.settleSpeed) >= 0) {
          r.settleTarget = null;
          r.activity = Activity.returning;
          plan(r);
        }
        continue;
      }
      if (r.route.isNotEmpty) {
        var remaining = dt;
        while (r.route.isNotEmpty && remaining > 1e-9) {
          final target = r.route.first;
          final unused = _move(
            r,
            target,
            remaining,
            r.segmentSpeed > 0 ? r.segmentSpeed : walkingSpeed(r, target),
          );
          if (unused < 0) break;
          r.route.removeAt(0);
          remaining = unused;
          if (r.replanAfterEdge) {
            plan(r);
            break;
          }
          if (r.route.isEmpty) {
            reached(r);
            break;
          }
        }
      } else if (r.activity == Activity.home) {
        r.timer -= dt;
        if (r.timer <= 0) decide(r);
      } else if (r.activity == Activity.returning) {
        r.timer -= dt;
        if (r.timer <= 0) plan(r);
      }
    }
  }

  double walkingSpeed(Resident r, Cell target) =>
      Rules.walkSpeed + _style(r.homeId) % 3 * Rules.walkVariation;
  bool gardenSegment(Resident r, Cell target) => false;
  void traversed(Resident r, Cell target, bool garden) {}

  double _move(Resident r, Cell target, double dt, double speed) {
    if (r.activity != Activity.settling) {
      if (!world.contains(target) || !world.roadStep(r.cell, target)) {
        r.segmentSpeed = 0;
        _settle(r);
        return -1;
      }
      if (r.segmentSpeed == 0) {
        r.segmentSpeed = speed;
        r.segmentGarden = gardenSegment(r, target);
      }
      speed = r.segmentSpeed;
    }
    final dx = target.x + .5 - r.x, dy = target.y + .5 - r.y;
    final distance = math.sqrt(dx * dx + dy * dy);
    if (dx.abs() > .001) r.facing = dx.sign;
    if (distance <= speed * dt) {
      r.x = target.x + .5;
      r.y = target.y + .5;
      r.cell = target;
      if (r.activity != Activity.settling) {
        traversed(r, target, r.segmentGarden);
      }
      r.segmentSpeed = 0;
      r.segmentGarden = false;
      return (dt - distance / speed).clamp(0.0, dt);
    }
    r.x += dx / distance * speed * dt;
    r.y += dy / distance * speed * dt;
    return -1;
  }
}
