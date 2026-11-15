import 'dart:math' as math;

import 'model.dart';

enum Activity {
  pending,
  arriving,
  home,
  visiting,
  returning,
  waiting,
  settling,
}

class Resident {
  final String homeId;
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
      y = cell.y + .5;
  bool get visible => activity != Activity.pending;
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
  final List<_Departure> _departures = [];
  int routeSearches = 0;
  Residents(this.world) {
    reconcile(world, restored: true);
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
  List<Cell>? _route(Cell from, Cell to) {
    routeSearches++;
    return world.pathRoute(from, to);
  }

  void reconcile(Landscape next, {bool restored = false, bool reset = false}) {
    world = next;
    if (reset) {
      people.clear();
      _departures.clear();
    }
    final homes = world.cottages;
    for (final id in people.keys.toList()) {
      if (!homes.containsKey(id)) {
        final r = people.remove(id)!;
        r.route.clear();
        r.destination = null;
        if (r.visible) _departures.add(_Departure(r));
      }
    }
    var added = 0;
    for (final entry in homes.entries) {
      if (people.containsKey(entry.key)) continue;
      _departures.removeWhere((d) => d.person.homeId == entry.key);
      final access =
          world.access(entry.value) ?? world.localAccess(entry.value);
      final r = Resident(
        entry.key,
        restored && access != null ? access : world.arrival,
        restored ? Activity.home : Activity.pending,
      );
      r.timer = restored ? _idle(r) : math.min(added++ * .18, 2.0);
      r.arrived = restored;
      people[entry.key] = r;
    }
    for (final r in people.values) {
      if (r.activity == Activity.pending) continue;
      if (r.destination != null && !homes.containsKey(r.destination))
        r.destination = null;
      if (r.activity == Activity.settling) {
        if (r.settleTarget != null &&
            world.contains(r.settleTarget!) &&
            world.at(r.settleTarget!).path)
          continue;
      }
      if (!world.contains(r.cell) ||
          !world.at(r.cell).path ||
          (r.route.isEmpty &&
              ((r.x - r.cell.x - .5).abs() > .01 ||
                  (r.y - r.cell.y - .5).abs() > .01)) ||
          (r.route.isNotEmpty && !world.at(r.route.first).path)) {
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
        _plan(r);
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
                  world.at(c).path &&
                  (c.x + .5 - r.x).abs() + (c.y + .5 - r.y).abs() <= 1.01,
            )
            .toList()
          ..sort(
            (a, b) => ((a.x + .5 - r.x).abs() + (a.y + .5 - r.y).abs())
                .compareTo((b.x + .5 - r.x).abs() + (b.y + .5 - r.y).abs()),
          );
    r.route.clear();
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

  bool _go(Resident r, Cell target, Activity activity) {
    final path = _route(r.cell, target);
    if (path == null) return false;
    r.route
      ..clear()
      ..addAll(path.skip(1));
    r.activity = activity;
    if (r.route.isEmpty) _reached(r);
    return true;
  }

  void _plan(Resident r) {
    r.replanAfterEdge = false;
    final home = world.cottages[r.homeId];
    if (home == null) return;
    if (!r.arrived) {
      final target = world.access(home);
      if (target != null && _go(r, target, Activity.arriving)) return;
    } else if (r.activity == Activity.visiting &&
        r.destination != null &&
        world.access(home) != null) {
      final other = world.cottages[r.destination];
      final target = other == null ? null : world.access(other);
      if (target != null && _go(r, target, Activity.visiting)) return;
    }
    final access = world.access(home);
    if (access != null && _go(r, access, Activity.returning)) return;
    final local = world.localAccess(home, from: r.cell);
    if (local != null &&
        _go(r, local, r.arrived ? Activity.returning : Activity.arriving))
      return;
    r.route.clear();
    r.activity = Activity.waiting;
  }

  void _reached(Resident r) {
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

  void _outing(Resident r) {
    final home = world.cottages[r.homeId];
    if (home == null ||
        world.access(home) == null ||
        !world.activePaths.contains(r.cell)) {
      r.timer = _idle(r);
      return;
    }
    final others = world.cottages.entries
        .where(
          (e) =>
              e.key != r.homeId &&
              world.access(e.value) != null &&
              world.access(e.value) != r.cell,
        )
        .toList();
    if (others.isNotEmpty) {
      final dest = others[(_style(r.homeId) + r.trips++) % others.length];
      r.destination = dest.key;
      if (_go(r, world.access(dest.value)!, Activity.visiting)) return;
    }
    final nearby = world.pathNeighbours(r.cell).toList();
    if (nearby.isNotEmpty) {
      r.destination = null;
      r.trips++;
      if (_go(r, nearby[_style(r.homeId) % nearby.length], Activity.visiting))
        return;
    }
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
          _go(r, target, Activity.arriving);
        }
        continue;
      }
      if (r.activity == Activity.settling) {
        final target = r.settleTarget;
        if (target != null && _move(r, target, dt, 5.0)) {
          r.settleTarget = null;
          r.activity = Activity.returning;
          _plan(r);
        }
        continue;
      }
      if (r.route.isNotEmpty) {
        final target = r.route.first;
        if (_move(r, target, dt, 3.5 + _style(r.homeId) % 3 * .15)) {
          r.route.removeAt(0);
          if (r.replanAfterEdge) {
            _plan(r);
          } else if (r.route.isEmpty) {
            _reached(r);
          }
        }
      } else if (r.activity == Activity.home) {
        r.timer -= dt;
        if (r.timer <= 0) _outing(r);
      } else if (r.activity == Activity.returning) {
        r.timer -= dt;
        if (r.timer <= 0) _plan(r);
      }
    }
  }

  bool _move(Resident r, Cell target, double dt, double speed) {
    final dx = target.x + .5 - r.x, dy = target.y + .5 - r.y;
    final distance = math.sqrt(dx * dx + dy * dy);
    if (dx.abs() > .001) r.facing = dx.sign;
    if (distance <= speed * dt) {
      r.x = target.x + .5;
      r.y = target.y + .5;
      r.cell = target;
      return true;
    }
    r.x += dx / distance * speed * dt;
    r.y += dy / distance * speed * dt;
    return false;
  }
}
