part of 'region.dart';

enum ShipmentState {
  loading,
  transit,
  unloading,
  delivered,
  returned,
  cancelled,
  lost,
}

class RegionalTradeRoute {
  final String id, a, b, payer;
  final int paid;
  bool enabled = true;
  int deliveries = 0, value = 0, cursor = 0;
  double nextAt = 0;
  RegionalTradeRoute(this.id, this.a, this.b, this.payer, this.paid);
  Map<String, Object> toJson() => {
    'id': id,
    'a': a,
    'b': b,
    'payer': payer,
    'paid': paid,
    'enabled': enabled,
    'deliveries': deliveries,
    'value': value,
    'cursor': cursor,
    'nextAt': nextAt,
  };
}

class RegionalShipment {
  final String id, route, origin, destination;
  final TradeGood good;
  final int quantity, price;
  final double created;
  final String originMarket, destinationMarket;
  ShipmentState state = ShipmentState.loading;
  bool collected = false, returning = false;
  String node, reason = 'Collecting at the origin Market';
  List<String> path = [];
  int leg = 0;
  double progress = 0, blockedSince = -1, finished = -1, departed = -1;
  RegionalShipment(
    this.id,
    this.route,
    this.origin,
    this.destination,
    this.good,
    this.quantity,
    this.price,
    this.created,
    this.originMarket,
    this.destinationMarket,
  ) : node = origin;
  bool get terminal => [
    ShipmentState.delivered,
    ShipmentState.returned,
    ShipmentState.cancelled,
    ShipmentState.lost,
  ].contains(state);
  String get target => returning ? origin : destination;
  Map<String, Object> toJson() => {
    'id': id,
    'route': route,
    'origin': origin,
    'destination': destination,
    'good': good.name,
    'quantity': quantity,
    'price': price,
    'created': created,
    'originMarket': originMarket,
    'destinationMarket': destinationMarket,
    'state': state.name,
    'collected': collected,
    'returning': returning,
    'node': node,
    'reason': reason,
    'path': path,
    'leg': leg,
    'progress': progress,
    'blockedSince': blockedSince,
    'finished': finished,
    'departed': departed,
  };
}

extension RegionalCommerce on SettlementRegion {
  String? market(RegionalTown town) {
    final c = town.colony;
    return c
        .places(Place.market)
        .where(
          (p) =>
              c.staffed(p.key) &&
              c.placeAccess(p.key) != null &&
              c.route(c.world.arrival, c.placeAccess(p.key)!) != null,
        )
        .map((p) => p.key)
        .firstOrNull;
  }

  WeatherKind weatherAt(String site) =>
      towns[site]?.colony.weather ??
      Climate(
        seedNumber(geography.site(site).terrainSeed),
        calendar: capital.colony.climate.calendar,
        environment: geography.site(site).environment,
      ).at(clock);
  Set<String> get closedConnections {
    final closed = <String>{};
    for (final e in geography.connections) {
      if (e.terrain == 'pass' &&
          weatherAt(e.a) == WeatherKind.snow &&
          weatherAt(e.b) == WeatherKind.snow) {
        closed.add(e.id);
      }
      for (final id in [e.a, e.b]) {
        final c = towns[id]?.colony, incident = c?.incident;
        if (c == null ||
            incident == null ||
            incident.state == 'retreating' ||
            incident.state == 'warning') {
          continue;
        }
        if (incident.kind == IncidentKind.raiders &&
                incident.state == 'grazing' ||
            incident.kind == IncidentKind.flood && market(towns[id]!) == null) {
          closed.add(e.id);
        }
      }
    }
    return closed;
  }

  double edgeWeather(RegionalConnection edge) =>
      math.max(weatherAt(edge.a).travel, weatherAt(edge.b).travel);
  double? journey(String a, String b) {
    final route = geography.path(
      a,
      b,
      known: discovered,
      closed: closedConnections,
    );
    if (route == null) return null;
    return route.fold<double>(0, (n, id) {
      final edge = geography.connections.firstWhere((e) => e.id == id);
      return n + edge.seconds * edgeWeather(edge);
    });
  }

  String? connectionIssue(String a, String b) {
    if (a == b || !towns.containsKey(a) || !towns.containsKey(b)) {
      return 'Choose two different founded settlements.';
    }
    if (geography.path(a, b, known: discovered) == null) {
      return 'Discover a connected regional path first.';
    }
    final pair = [a, b]..sort();
    if (tradeRoutes.containsKey('trade-${pair[0]}-${pair[1]}')) {
      return 'This trading relationship already exists.';
    }
    if (!freeBuild && active.colony.session.money < 3000) {
      return 'Establishing a relationship costs 3 coins.';
    }
    return null;
  }

  bool connectTrade(String a, String b) {
    if (connectionIssue(a, b) != null) return false;
    final pair = [a, b]..sort(),
        id = 'trade-${pair[0]}-${pair[1]}',
        paid = freeBuild ? 0 : 3000;
    active.colony.session.money -= paid;
    tradeRoutes[id] = RegionalTradeRoute(id, pair[0], pair[1], activeId, paid)
      ..nextAt = clock;
    remember(
      'trade-route',
      a,
      'Automatic trade established with ${towns[b]!.name}.',
    );
    return true;
  }

  void enableTrade(String id, bool enabled) {
    final route = tradeRoutes[id];
    if (route == null || route.enabled == enabled) return;
    route.enabled = enabled;
    if (!enabled) {
      for (final s
          in shipments.values
              .where(
                (s) =>
                    s.route == id &&
                    s.state == ShipmentState.loading &&
                    !s.collected,
              )
              .toList()) {
        _refund(s, ShipmentState.cancelled, restore: true);
      }
    }
  }

  String tradeStatus(RegionalTradeRoute route) {
    final moving = shipments.values
        .where((s) => s.route == route.id && !s.terminal)
        .firstOrNull;
    if (moving != null) return '${moving.good.label} · ${moving.reason}';
    if (!route.enabled) return 'Paused · existing journeys finish normally';
    if (journey(route.a, route.b) == null) {
      return 'Waiting for an open regional connection';
    }
    if (market(towns[route.a]!) == null || market(towns[route.b]!) == null) {
      return 'Needs staffed, arrival-connected Markets in both towns';
    }
    return 'Ready · automatically exchanges surplus for needed supply';
  }

  int demand(RegionalTown town, TradeGood good) {
    final target = good == TradeGood.materials
        ? (town.landmark == null ? 8 : 2)
        : 6;
    final incoming = shipments.values
        .where(
          (s) =>
              !s.terminal &&
              !s.returning &&
              s.destination == town.siteId &&
              s.good == good,
        )
        .fold<int>(0, (n, s) => n + s.quantity);
    return math.max(
      0,
      target - town.stock[good]! - town.supplied[good]! - incoming,
    );
  }

  void _dispatch(RegionalTradeRoute route) {
    if (!route.enabled ||
        shipments.values.any((s) => s.route == route.id && !s.terminal)) {
      return;
    }
    if (journey(route.a, route.b) == null) return;
    final directions = [(route.a, route.b), (route.b, route.a)];
    for (var offset = 0; offset < 6; offset++) {
      final slot = (route.cursor + offset) % 6;
      final (from, to) = directions[slot ~/ 3];
      final good = TradeGood.values[slot % 3];
      final source = towns[from]!, destination = towns[to]!;
      final originMarket = market(source),
          destinationMarket = market(destination);
      if (originMarket == null ||
          destinationMarket == null ||
          source.colony.visits.length >= 4 ||
          destination.colony.people.length < 2) {
        continue;
      }
      final quantity = math.min(
        3,
        math.min(
          source.stock[good]! - 2,
          math.min(
            demand(destination, good),
            destination.colony.session.money ~/ 1000,
          ),
        ),
      );
      if (quantity <= 0) continue;
      final id = 'shipment-${nextShipment++}';
      final ship = RegionalShipment(
        id,
        route.id,
        from,
        to,
        good,
        quantity,
        quantity * 1000,
        clock,
        originMarket,
        destinationMarket,
      );
      if (source.colony.startFreight(id, originMarket, exporting: true) ==
          null) {
        continue;
      }
      source.stock[good] = source.stock[good]! - quantity;
      destination.colony.session.money -= ship.price;
      shipments[id] = ship;
      route.cursor = (slot + 1) % 6;
      remember(
        'dispatch',
        from,
        '${good.label} reserved for ${destination.name}; collecting at the Market.',
      );
      return;
    }
  }

  void _freightServed(String id, bool exporting) {
    final s = shipments[id];
    if (s == null || s.terminal) return;
    if (exporting && s.state == ShipmentState.loading) {
      s.collected = true;
      s.reason = 'Collected · walking to the departure road';
      return;
    }
    if (!exporting && s.state == ShipmentState.unloading) {
      if (s.returning) {
        _refund(s, ShipmentState.returned, restore: true);
        return;
      }
      final source = towns[s.origin]!,
          destination = towns[s.destination]!,
          route = tradeRoutes[s.route]!;
      destination.supplied[s.good] = destination.supplied[s.good]! + s.quantity;
      source.colony.session.money += s.price;
      source.exportsEarned += s.price;
      destination.importsSpent += s.price;
      destination.deliveries++;
      route.deliveries++;
      route.value += s.price;
      s.state = ShipmentState.delivered;
      s.finished = clock;
      s.reason = 'Delivered at the staffed Market';
      remember(
        'delivery',
        s.destination,
        '${s.quantity} ${s.good.label} arrived from ${source.name}; ${Rules.coins(s.price)} coins paid once.',
      );
    }
  }

  void _freightExit(String id, bool exporting, bool served) {
    final s = shipments[id];
    if (s == null || s.terminal) return;
    if (exporting && s.state == ShipmentState.loading) {
      if (!served || !s.collected) {
        _refund(s, ShipmentState.cancelled, restore: true);
        return;
      }
      s.state = ShipmentState.transit;
      s.departed = clock;
      s.node = s.origin;
      s.reason = 'Travelling between settlements';
      _routeFromNode(s);
    } else if (!exporting && s.state == ShipmentState.unloading && !served) {
      s.returning = true;
      s.state = ShipmentState.transit;
      s.node = s.destination;
      s.path = [];
      s.leg = 0;
      s.progress = 0;
      s.blockedSince = -1;
      s.reason = 'Returning undelivered cargo along the regional roads';
      _routeFromNode(s);
    }
  }

  void _refund(
    RegionalShipment s,
    ShipmentState finalState, {
    required bool restore,
  }) {
    if (s.terminal) return;
    if (restore) {
      final source = towns[s.origin]!;
      source.stock[s.good] = source.stock[s.good]! + s.quantity;
    }
    towns[s.destination]!.colony.session.money += s.price;
    s.state = finalState;
    s.finished = clock;
    s.reason = finalState == ShipmentState.returned
        ? 'Cargo returned to the origin Market'
        : finalState == ShipmentState.cancelled
        ? 'Reservation cancelled; goods and escrow restored'
        : 'Cargo lost after a prolonged Standard-mode interruption; buyer escrow refunded';
    for (final town in towns.values) {
      town.colony.retireFreight(s.id);
    }
    remember('shipment-${finalState.name}', s.destination, s.reason);
  }

  void _routeFromNode(RegionalShipment s) {
    final route = geography.path(
      s.node,
      s.target,
      known: discovered,
      closed: closedConnections,
    );
    if (route != null) {
      s.path = route;
      s.leg = 0;
      s.progress = 0;
    }
  }

  void _blocked(RegionalShipment s, String reason) {
    s.reason = reason;
    if (s.blockedSince < 0) s.blockedSince = clock;
    if (clock - s.blockedSince >= 180 &&
        (towns[s.origin]!.colony.incidentMode == IncidentMode.standard ||
            towns[s.destination]!.colony.incidentMode ==
                IncidentMode.standard)) {
      _refund(s, ShipmentState.lost, restore: !s.collected);
    }
  }

  void _shipmentTick(RegionalShipment s, double dt, Set<String> closed) {
    if (s.terminal) return;
    if (s.state == ShipmentState.loading) {
      if (!s.collected && clock - s.created > 60) {
        _refund(s, ShipmentState.cancelled, restore: true);
      } else if (s.collected) {
        _blocked(s, 'Collected cargo is waiting for a legal departure road');
      }
      return;
    }
    if (s.state == ShipmentState.unloading) {
      final town = towns[s.target]!;
      final v = town.colony.visits.values
          .where((v) => v.freight == s.id)
          .firstOrNull;
      if (v == null) {
        _blocked(s, 'Waiting for a local caravan arrival');
        return;
      }
      if (clock - s.departed > 240 && !s.returning && !v.completed) {
        town.colony.retireFreight(s.id);
      }
      if (v.actor.activity != Activity.socialising && !v.actor.moving) {
        _blocked(s, 'Waiting for legal Market access and a present vendor');
      } else {
        s.blockedSince = -1;
        s.reason = 'Caravan approaching the destination Market';
      }
      return;
    }
    if (s.path.isEmpty && s.node != s.target) _routeFromNode(s);
    if (s.node == s.target && s.leg >= s.path.length) {
      final town = towns[s.target]!, venue = market(town);
      if (venue == null || town.colony.visits.length >= 4) {
        _blocked(
          s,
          'At the destination · waiting for a staffed, connected Market',
        );
        return;
      }
      if (town.colony.startFreight(s.id, venue, exporting: false) != null) {
        s.state = ShipmentState.unloading;
        s.reason = 'Arrived at the entry road · walking to the Market';
        s.blockedSince = -1;
      }
      return;
    }
    if (s.leg >= s.path.length) {
      _blocked(
        s,
        'No open discovered route; cargo waits at the last reached site',
      );
      return;
    }
    var edge = geography.connections.firstWhere((e) => e.id == s.path[s.leg]);
    if (closed.contains(edge.id)) {
      if (s.progress <= 1e-9) {
        _routeFromNode(s);
        if (s.path.isNotEmpty) {
          edge = geography.connections.firstWhere((e) => e.id == s.path[s.leg]);
        }
      }
      if (closed.contains(edge.id)) {
        _blocked(s, 'Pass or connection interrupted · waiting safely');
        return;
      }
    }
    s.blockedSince = -1;
    s.reason = s.returning
        ? 'Returning cargo through the region'
        : 'Travelling through ${edge.terrain}';
    s.progress += dt / edgeWeather(edge);
    if (s.progress >= edge.seconds - 1e-9) {
      s.node = edge.other(s.node);
      s.progress = 0;
      s.leg++;
    }
  }

  void _tradeTick(double dt) {
    final closed = closedConnections;
    for (final s in shipments.values.toList()) {
      _shipmentTick(s, dt, closed);
    }
    if (clock + 1e-9 >= nextTradeAt) {
      nextTradeAt = clock + 10;
      for (final r in tradeRoutes.values) {
        if (clock + 1e-9 < r.nextAt) continue;
        r.nextAt = clock + 20;
        _dispatch(r);
      }
      final terminal = shipments.values
          .where(
            (s) =>
                s.terminal &&
                !towns.values.any(
                  (t) => t.colony.visits.values.any((v) => v.freight == s.id),
                ),
          )
          .toList();
      while (shipments.length > 128 && terminal.isNotEmpty) {
        shipments.remove(terminal.removeAt(0).id);
      }
    }
    if (clock + 1e-9 >= nextSupplyAt) {
      nextSupplyAt = clock + 30;
      for (final town in towns.values) {
        if (town.colony.people.length < 2 || market(town) == null) continue;
        if (town.supplied[TradeGood.provisions]! > 0) {
          town.supplied[TradeGood.provisions] =
              town.supplied[TradeGood.provisions]! - 1;
          town.provisionBenefitUntil = clock + 60;
        }
        if (town.supplied[TradeGood.wares]! > 0) {
          town.supplied[TradeGood.wares] = town.supplied[TradeGood.wares]! - 1;
          town.colony.nextVisitorAt = math.min(
            town.colony.nextVisitorAt,
            clock + 5,
          );
          town.colony.visitorCooldown[VisitorRole.merchant] = math.min(
            town.colony.visitorCooldown[VisitorRole.merchant] ?? clock,
            clock + 15,
          );
          remember(
            'merchant-opportunity',
            town.siteId,
            'Delivered Crafted Wares attracted a merchant opportunity; service still requires real arrival.',
          );
        }
      }
    }
  }

  Map<String, Object> tradeJson() => {
    'version': 1,
    'nextShipment': nextShipment,
    'nextTradeAt': nextTradeAt,
    'nextSupplyAt': nextSupplyAt,
    'routes': tradeRoutes.values.map((r) => r.toJson()).toList(),
    'shipments': shipments.values.map((s) => s.toJson()).toList(),
  };
  void restoreTrade(dynamic raw) {
    Never bad() =>
        throw const FormatException('Invalid regional trade checkpoint.');
    int integer(dynamic n, [int max = 1000000000000]) {
      if (n is! int || n < 0 || n > max) bad();
      return n;
    }

    double number(dynamic n, [double min = 0, double max = 1e12]) {
      if (n is! num || !n.isFinite || n < min || n > max) bad();
      return n.toDouble();
    }

    if (raw is! Map ||
        raw['version'] != 1 ||
        raw['routes'] is! List ||
        raw['routes'].length > 128 ||
        raw['shipments'] is! List ||
        raw['shipments'].length > 256) {
      bad();
    }
    nextShipment = integer(raw['nextShipment']);
    if (nextShipment < 1) bad();
    nextTradeAt = number(raw['nextTradeAt']);
    nextSupplyAt = number(raw['nextSupplyAt']);
    for (final d in raw['routes']) {
      if (d is! Map ||
          d['a'] is! String ||
          d['b'] is! String ||
          !towns.containsKey(d['a']) ||
          !towns.containsKey(d['b']) ||
          (d['a'] as String).compareTo(d['b']) >= 0 ||
          d['id'] != 'trade-${d['a']}-${d['b']}' ||
          tradeRoutes.containsKey(d['id']) ||
          !towns.containsKey(d['payer']) ||
          d['enabled'] is! bool ||
          geography.path(d['a'], d['b'], known: discovered) == null) {
        bad();
      }
      final route =
          RegionalTradeRoute(
              d['id'],
              d['a'],
              d['b'],
              d['payer'],
              integer(d['paid'], 3000),
            )
            ..enabled = d['enabled']
            ..deliveries = integer(d['deliveries'])
            ..value = integer(d['value'])
            ..cursor = integer(d['cursor'], 5)
            ..nextAt = number(d['nextAt']);
      tradeRoutes[route.id] = route;
    }
    final inFlight = <String>{};
    for (final d in raw['shipments']) {
      if (d is! Map ||
          d['id'] is! String ||
          !RegExp(r'^shipment-[1-9][0-9]*$').hasMatch(d['id']) ||
          int.parse((d['id'] as String).split('-').last) >= nextShipment ||
          shipments.containsKey(d['id']) ||
          !tradeRoutes.containsKey(d['route']) ||
          !towns.containsKey(d['origin']) ||
          !towns.containsKey(d['destination']) ||
          d['origin'] == d['destination'] ||
          d['originMarket'] is! String ||
          d['destinationMarket'] is! String ||
          d['reason'] is! String ||
          d['reason'].length > 250 ||
          d['collected'] is! bool ||
          d['returning'] is! bool ||
          d['path'] is! List ||
          d['path'].length > 32 ||
          !discovered.contains(d['node'])) {
        bad();
      }
      final relation = tradeRoutes[d['route']]!;
      if (![relation.a, relation.b].contains(d['origin']) ||
          ![relation.a, relation.b].contains(d['destination'])) {
        bad();
      }
      final good = TradeGood.values.firstWhere(
        (g) => g.name == d['good'],
        orElse: () => throw const FormatException('Invalid cargo.'),
      );
      final quantity = integer(d['quantity'], 3);
      if (quantity < 1 || d['price'] != quantity * 1000) bad();
      final s = RegionalShipment(
        d['id'],
        d['route'],
        d['origin'],
        d['destination'],
        good,
        quantity,
        d['price'],
        number(d['created'], 0, clock),
        d['originMarket'],
        d['destinationMarket'],
      );
      s.state = ShipmentState.values.firstWhere(
        (v) => v.name == d['state'],
        orElse: () => throw const FormatException('Invalid shipment state.'),
      );
      s
        ..collected = d['collected']
        ..returning = d['returning']
        ..node = d['node']
        ..reason = d['reason']
        ..path = List<String>.from(d['path'])
        ..leg = integer(d['leg'], s.path.length)
        ..progress = number(d['progress'], 0, 600)
        ..blockedSince = number(d['blockedSince'], -1, clock)
        ..finished = number(d['finished'], -1, clock)
        ..departed = number(d['departed'], -1, clock);
      if (s.terminal != (s.finished >= 0) ||
          !s.terminal && !inFlight.add(s.route) ||
          s.state == ShipmentState.loading && s.departed >= 0) {
        bad();
      }
      for (final id in s.path) {
        if (!geography.connections.any((e) => e.id == id)) bad();
      }
      if (s.leg < s.path.length &&
          !geography.connections
              .firstWhere((e) => e.id == s.path[s.leg])
              .contains(s.node)) {
        bad();
      }
      shipments[s.id] = s;
    }
    for (final t in towns.values) {
      final ids = <String>{};
      for (final v in t.colony.visits.values.where((v) => v.freight != null)) {
        final s = shipments[v.freight];
        if (s == null || !ids.add(v.freight!)) bad();
        if (s.terminal && !v.leaving) bad();
        if (v.exporting &&
            (t.siteId != s.origin ||
                s.state != ShipmentState.loading && !s.terminal)) {
          bad();
        }
        if (!v.exporting && t.siteId != s.target) bad();
      }
    }
    for (final s in shipments.values.where((s) => !s.terminal)) {
      final appearances = towns.values
          .expand((t) => t.colony.visits.values)
          .where((v) => v.freight == s.id)
          .toList();
      if (s.state == ShipmentState.transit
          ? appearances.isNotEmpty
          : appearances.length != 1) {
        bad();
      }
    }
  }
}
