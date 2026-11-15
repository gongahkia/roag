part of 'colony.dart';

enum VisitorRole {
  merchant('Travelling merchant', 'Mira Pack', 'merchant-body-v1'),
  storyteller('Wandering storyteller', 'Orin Reed', 'storyteller-body-v1');

  final String label, personName, body;
  const VisitorRole(this.label, this.personName, this.body);
}

class Visit {
  final String id;
  final VisitorRole role;
  final Resident actor;
  final double starts;
  String venue;
  bool leaving = false, completed = false, encore = false;
  double progress = 0;
  final Map<String, double> attendees = {};
  final Set<String> awarded = {};
  Visit(this.id, this.role, this.venue, this.starts, Cell entry)
    : actor = Resident(id, entry, Activity.waiting) {
    actor
      ..bodyId = role.body
      ..name = role.personName
      ..palette = role == VisitorRole.merchant ? 2 : 3
      ..trait = role == VisitorRole.merchant ? Trait.brisk : Trait.sociable
      ..arrived = true;
  }
  String get state => leaving
      ? actor.route.isNotEmpty
            ? 'Leaving by the road'
            : 'Waiting for a way home'
      : actor.activity == Activity.socialising
      ? role == VisitorRole.merchant
            ? 'Trading with the Market'
            : 'Telling a story'
      : actor.route.isNotEmpty
      ? 'Walking to the venue'
      : 'Waiting for access or space';
}

class VisitMemory {
  final String visit, venue;
  final VisitorRole role;
  final double at;
  final int reward, attendees;
  VisitMemory(
    this.visit,
    this.role,
    this.venue,
    this.at,
    this.reward,
    this.attendees,
  );
  Map<String, Object> toJson() => {
    'visit': visit,
    'role': role.name,
    'venue': venue,
    'at': at,
    'reward': reward,
    'attendees': attendees,
  };
}

extension LivingVisitors on Colony {
  bool _visitorVenue(Visit v) => v.role == VisitorRole.merchant
      ? kind(v.venue) == Place.market
      : [Place.green, Place.hall].contains(kind(v.venue));
  void _visitorBoundary() {
    if (clock + 1e-9 < nextVisitorAt) return;
    nextVisitorAt = clock + 15;
    if (visits.length >= 2 || phase == 'Night') return;
    final options = <(VisitorRole, String)>[];
    for (final role in VisitorRole.values) {
      if ((visitorCooldown[role] ?? 0) > clock ||
          visits.values.any((v) => v.role == role)) {
        continue;
      }
      for (final p in venues) {
        final suitable = role == VisitorRole.merchant
            ? kind(p.key) == Place.market && staffed(p.key)
            : [Place.green, Place.hall].contains(kind(p.key));
        final access = placeAccess(p.key);
        if (!suitable || access == null) {
          continue;
        }
        if (route(world.arrival, access) == null) continue;
        // Covered storytelling and summer Parks receive one extra eligible ticket.
        options.add((role, p.key));
        if (role == VisitorRole.storyteller &&
            (climate.covered(clock) && kind(p.key) == Place.hall ||
                season == Season.summer && kind(p.key) == Place.green)) {
          options.add((role, p.key));
        }
        if (role == VisitorRole.merchant && weather == WeatherKind.clear) {
          options.add((role, p.key));
        }
      }
    }
    if (options.isEmpty) return;
    visitorRng = visitorRng * 48271 % 2147483647;
    final chosen = options[visitorRng % options.length];
    final v = Visit(
      'visit-${nextVisitId++}',
      chosen.$1,
      chosen.$2,
      clock,
      world.arrival,
    );
    visits[v.id] = v;
    visitorCooldown[v.role] = clock + Rules.day;
    plan(v.actor);
  }

  void _planVisitor(Visit v) {
    final r = v.actor;
    r.replanAfterEdge = false;
    if (!_visitorVenue(v) || clock - v.starts > Rules.day) v.leaving = true;
    if (v.leaving) {
      r.destination = null;
      if (r.cell == world.arrival &&
          (r.x - r.cell.x - .5).abs() + (r.y - r.cell.y - .5).abs() < .01) {
        visits.remove(v.id);
        return;
      }
      if (go(r, world.arrival, Activity.returning)) return;
    } else {
      final access = placeAccess(v.venue);
      r.destination = v.venue;
      if (access != null && go(r, access, Activity.socialTravel)) return;
    }
    r.route.clear();
    r.activity = Activity.waiting;
    r.timer = Rules.searchInterval;
  }

  void _visitorReached(Visit v) {
    final r = v.actor;
    if (v.leaving && r.cell == world.arrival) {
      visits.remove(v.id);
      return;
    }
    if (!v.leaving &&
        _visitorVenue(v) &&
        (v.role != VisitorRole.merchant || staffed(v.venue)) &&
        placeAccess(v.venue) == r.cell &&
        occupancy(v.venue) < capacity(v.venue)) {
      r.activity = Activity.socialising;
      r.say(
        v.role == VisitorRole.merchant
            ? Reason.visitorTrading
            : Reason.visitorTelling,
      );
      r.destination = v.venue;
      _countsDirty = true;
    } else {
      r.activity = Activity.waiting;
      r.timer = Rules.searchInterval;
    }
  }

  void _reconcileVisitors() {
    for (final v in visits.values.toList()) {
      final r = v.actor;
      if (!_visitorVenue(v) ||
          placeAccess(v.venue) != r.cell &&
              r.activity == Activity.socialising) {
        // A moved venue can be reached again; a removed venue ends the visit.
        if (kind(v.venue) == null) v.leaving = true;
        r.activity = Activity.waiting;
      }
      if (r.route.isNotEmpty) {
        if (world.roadStep(r.cell, r.route.first)) {
          r.route.removeRange(1, r.route.length);
          r.replanAfterEdge = true;
          continue;
        }
        // Retrace only the unfinished part of the removed edge to its near side.
        r.route.clear();
        r.segmentSpeed = 0;
        if (world.roadCell(r.cell)) {
          r.settleTarget = r.cell;
          r.activity = Activity.settling;
        } else {
          r.activity = Activity.waiting;
        }
      }
      if (!r.moving) plan(r);
    }
  }

  void _updateVisitors(double dt) {
    for (final v in visits.values.toList()) {
      final r = v.actor;
      final wasActive = r.activity == Activity.socialising;
      if (r.moving) {
        advanceTravel(r, dt);
      }
      if (!visits.containsKey(v.id) || r.moving) continue;
      if (r.activity == Activity.socialising) {
        if (!_visitorVenue(v) ||
            placeAccess(v.venue) != r.cell ||
            v.role == VisitorRole.merchant && !staffed(v.venue)) {
          r.activity = Activity.waiting;
          r.timer = 0;
          plan(r);
          continue;
        }
        if (!wasActive) continue;
        v.progress += dt;
        if (v.progress + 1e-9 >=
            (v.role == VisitorRole.merchant
                ? 6
                : v.encore
                ? 24
                : 18)) {
          if (!v.completed) {
            v.completed = true;
            final reward = v.role == VisitorRole.merchant
                ? _earn(2000, visitor: true)
                : 0;
            visitHistory.add(
              VisitMemory(
                v.id,
                v.role,
                v.venue,
                clock,
                reward,
                v.awarded.length,
              ),
            );
            if (visitHistory.length > 24) visitHistory.removeAt(0);
            guestCompletions.update(
              v.role.name,
              (n) => n + 1,
              ifAbsent: () => 1,
            );
          }
          v.leaving = true;
          r.say(Reason.visitorLeaving);
          plan(r);
        }
      } else if (!r.moving) {
        r.timer -= dt;
        if (r.timer <= 0) {
          r.timer = Rules.searchInterval;
          plan(r);
        }
      }
    }
  }

  void _attendStory(Resident r, double dt) {
    for (final v in visits.values) {
      if (v.role != VisitorRole.storyteller ||
          v.leaving ||
          v.actor.activity != Activity.socialising ||
          r.destination != v.venue ||
          r.cell != v.actor.cell ||
          v.awarded.contains(r.homeId)) {
        continue;
      }
      final attended = (v.attendees[r.homeId] ?? 0) + dt;
      v.attendees[r.homeId] = attended;
      if (attended + 1e-9 >= 4) {
        v.awarded.add(r.homeId);
        r.visitorBenefitUntil = clock + 75;
        r.say(Reason.socialDone);
      }
    }
  }

  bool canEncore(String? id) {
    final v = visits[id];
    return v != null &&
        v.role == VisitorRole.storyteller &&
        !v.leaving &&
        !v.encore &&
        v.actor.activity == Activity.socialising &&
        session.money >= 2000;
  }

  bool encore(String id) {
    if (!canEncore(id)) return false;
    visits[id]!.encore = true;
    session.money -= 2000;
    visitorSpent += 2000;
    return true;
  }

  String visitorStory(Visit v) {
    final familiar =
        (guestCompletions[v.role.name] ?? 0) > (v.completed ? 1 : 0);
    final returnLine = familiar
        ? 'I remember my earlier visit. It is good to return. '
        : 'This is my first stop here. ';
    if (v.leaving) {
      return '$returnLine${v.completed ? 'Our visit is complete.' : 'The venue is unavailable.'} I will follow the roads back out when there is a safe route.';
    }
    return v.role == VisitorRole.merchant
        ? '${returnLine}A working Market brought me here. Six seconds trading with its vendor earns the town 2 coins, once. ${v.state}.'
        : '$returnLine${climate.covered(clock)
              ? kind(v.venue) == Place.hall
                    ? 'This Hall offers a covered story circle.'
                    : 'A Hall would offer a covered alternative to this Park.'
              : season == Season.summer
              ? 'Summer is a fine time for a Park story.'
              : 'A communal place makes a good stop.'} Listeners who actually stay four seconds receive a small mood benefit. ${v.state}.';
  }

  Map<String, Object> visitorsJson() => {
    'version': 1,
    'rng': visitorRng,
    'nextId': nextVisitId,
    'nextAt': nextVisitorAt,
    'earned': visitorEarned,
    'spent': visitorSpent,
    'cooldowns': {for (final e in visitorCooldown.entries) e.key.name: e.value},
    'completions': guestCompletions,
    'history': visitHistory.map((h) => h.toJson()).toList(),
    'active': [
      for (final v in visits.values)
        {
          'id': v.id,
          'role': v.role.name,
          'venue': v.venue,
          'starts': v.starts,
          'leaving': v.leaving,
          'completed': v.completed,
          'encore': v.encore,
          'progress': v.progress,
          'attendees': v.attendees,
          'awarded': v.awarded.toList(),
          'actor': _personJson(v.actor),
        },
    ],
  };

  void restoreVisitors(dynamic d) {
    Never bad() => throw const FormatException('Invalid visitor checkpoint.');
    double numAt(Map m, String k, double lo, double hi) {
      final n = m[k];
      if (n is! num || !n.isFinite || n < lo || n > hi) bad();
      return n.toDouble();
    }

    int intAt(Map m, String k, int lo, int hi) {
      final n = m[k];
      if (n is! int || n < lo || n > hi) bad();
      return n;
    }

    bool flag(Map m, String k) {
      final v = m[k];
      if (v is! bool) bad();
      return v;
    }

    String id(dynamic v) {
      if (v is! String || !RegExp(r'^[A-Za-z0-9_-]{1,64}$').hasMatch(v)) bad();
      return v;
    }

    VisitorRole role(dynamic v) =>
        VisitorRole.values.firstWhere((r) => r.name == v, orElse: bad);
    Cell cell(dynamic v) {
      if (v is! List || v.length != 2 || v[0] is! int || v[1] is! int) bad();
      final c = Cell(v[0], v[1]);
      if (!world.contains(c)) bad();
      return c;
    }

    if (d is! Map || d['version'] != 1) bad();
    visitorRng = intAt(d, 'rng', 1, 2147483646);
    nextVisitId = intAt(d, 'nextId', 1, 1000000000);
    nextVisitorAt = numAt(d, 'nextAt', 0, 1e12);
    visitorEarned = intAt(d, 'earned', 0, 1000000000000);
    visitorSpent = intAt(d, 'spent', 0, 1000000000000);
    if (d['cooldowns'] is! Map ||
        d['cooldowns'].length > 2 ||
        d['completions'] is! Map ||
        d['completions'].length > 2) {
      bad();
    }
    for (final e in (d['cooldowns'] as Map).entries) {
      visitorCooldown[role(e.key)] = numAt(d['cooldowns'], e.key, 0, 1e12);
    }
    for (final e in (d['completions'] as Map).entries) {
      role(e.key);
      guestCompletions[e.key] = intAt(d['completions'], e.key, 0, 1000000000);
    }
    final history = d['history'], active = d['active'];
    if (history is! List ||
        history.length > 24 ||
        active is! List ||
        active.length > 2) {
      bad();
    }
    final historyIds = <String>{};
    for (final h in history) {
      if (h is! Map) bad();
      final vid = id(h['visit']);
      if (!historyIds.add(vid)) bad();
      visitHistory.add(
        VisitMemory(
          vid,
          role(h['role']),
          id(h['venue']),
          numAt(h, 'at', 0, clock),
          intAt(h, 'reward', 0, 2000),
          intAt(h, 'attendees', 0, 20000),
        ),
      );
    }
    final roles = <VisitorRole>{};
    for (final row in active) {
      if (row is! Map || row['actor'] is! Map) bad();
      final vid = id(row['id']), type = role(row['role']);
      final serial = int.tryParse(vid.replaceFirst('visit-', ''));
      if (serial == null ||
          vid != 'visit-$serial' ||
          serial < 1 ||
          serial >= nextVisitId ||
          visits.containsKey(vid) ||
          !roles.add(type)) {
        bad();
      }
      final v = Visit(
        vid,
        type,
        id(row['venue']),
        numAt(row, 'starts', 0, clock),
        world.arrival,
      );
      v
        ..leaving = flag(row, 'leaving')
        ..completed = flag(row, 'completed')
        ..encore = flag(row, 'encore')
        ..progress = numAt(row, 'progress', 0, 24.05);
      if (v.completed != historyIds.contains(vid) ||
          v.completed && !v.leaving ||
          !v.leaving && kind(v.venue) == null ||
          type == VisitorRole.merchant && (v.encore || v.progress > 6.05)) {
        bad();
      }
      final a = row['actor'] as Map, r = v.actor;
      if (a['id'] != vid ||
          a['bodyId'] != type.body ||
          a['name'] != type.personName ||
          a['active'] != true ||
          a['arrived'] != true ||
          a['workId'] != null) {
        bad();
      }
      r
        ..cell = cell(a['cell'])
        ..x = numAt(a, 'x', 0, world.width.toDouble())
        ..y = numAt(a, 'y', 0, world.height.toDouble())
        ..timer = numAt(a, 'timer', -1, 10)
        ..palette = intAt(a, 'palette', 0, 3)
        ..trait = Trait.values.firstWhere(
          (t) => t.name == a['trait'],
          orElse: bad,
        )
        ..gardenDay = intAt(a, 'gardenDay', -1, day)
        ..segmentSpeed = numAt(a, 'segmentSpeed', 0, 20)
        ..segmentGarden = flag(a, 'segmentGarden')
        ..replanAfterEdge = flag(a, 'replan');
      r.activity = Activity.values.firstWhere(
        (s) => s.name == a['activity'],
        orElse: bad,
      );
      if (![
        Activity.waiting,
        Activity.socialTravel,
        Activity.socialising,
        Activity.returning,
        Activity.settling,
      ].contains(r.activity)) {
        bad();
      }
      r.destination = a['destination'] == null ? null : id(a['destination']);
      r.settleTarget = a['settle'] == null ? null : cell(a['settle']);
      final path = a['route'];
      if (path is! List || path.length > world.lots.length) bad();
      var prev = r.cell;
      for (final n in path) {
        final c = cell(n);
        if (!world.roadStep(prev, c)) bad();
        r.route.add(c);
        prev = c;
      }
      if ((r.x - r.cell.x - .5).abs() + (r.y - r.cell.y - .5).abs() > 1.02 ||
          r.activity == Activity.settling && r.settleTarget == null ||
          r.activity == Activity.socialising &&
              (r.moving ||
                  placeAccess(v.venue) != r.cell ||
                  !_visitorVenue(v)) ||
          !v.leaving && r.destination != v.venue) {
        bad();
      }
      final attendees = row['attendees'], awards = row['awarded'];
      if (attendees is! Map ||
          attendees.length > 20000 ||
          awards is! List ||
          awards.length > attendees.length ||
          awards.toSet().length != awards.length ||
          type == VisitorRole.merchant && attendees.isNotEmpty) {
        bad();
      }
      for (final e in attendees.entries) {
        if (!records.any((p) => p.homeId == e.key)) bad();
        v.attendees[id(e.key)] = numAt(attendees, e.key, 0, 4.05);
      }
      for (final id in awards) {
        if (!v.attendees.containsKey(id) || v.attendees[id]! < 4 - 1e-6) bad();
        v.awarded.add(id);
      }
      visits[vid] = v;
    }
    if (visits.values.any(
      (v) =>
          v.actor.activity == Activity.socialising &&
          occupancy(v.venue) > capacity(v.venue),
    )) {
      bad();
    }
  }
}
