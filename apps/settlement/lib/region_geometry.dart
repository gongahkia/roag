import 'dart:collection';

import 'terrain.dart';

class RegionalSite {
  final String id, terrainSeed;
  final int x, y;
  final EnvironmentPreset environment;
  const RegionalSite(
    this.id,
    this.x,
    this.y,
    this.environment,
    this.terrainSeed,
  );
  Map<String, Object> toJson() => {
    'id': id,
    'x': x,
    'y': y,
    'environment': environment.name,
    'terrainSeed': terrainSeed,
  };
  factory RegionalSite.decode(dynamic value) {
    if (value is! Map ||
        value['id'] is! String ||
        !RegExp(r'^site-[0-9]{2}$').hasMatch(value['id']) ||
        value['x'] is! int ||
        value['y'] is! int ||
        value['x'] < 0 ||
        value['x'] > 1000 ||
        value['y'] < 0 ||
        value['y'] > 1000 ||
        value['terrainSeed'] is! String ||
        canonicalSeed(value['terrainSeed']) != value['terrainSeed']) {
      throw const FormatException('Invalid regional site.');
    }
    final environment = EnvironmentPreset.values.firstWhere(
      (p) => p.name == value['environment'],
      orElse: () =>
          throw const FormatException('Unknown regional environment.'),
    );
    return RegionalSite(
      value['id'],
      value['x'],
      value['y'],
      environment,
      value['terrainSeed'],
    );
  }
}

class RegionalConnection {
  final String id, a, b;
  final int seconds;
  final String terrain;
  const RegionalConnection(this.id, this.a, this.b, this.seconds, this.terrain);
  bool contains(String site) => a == site || b == site;
  String other(String site) => a == site ? b : a;
  Map<String, Object> toJson() => {
    'id': id,
    'a': a,
    'b': b,
    'seconds': seconds,
    'terrain': terrain,
  };
}

// Versioned geography owns no simulation RNG and is never called when loading a region.
class RegionalGeography {
  static const currentVersion = 1;
  final int version;
  final String seedText;
  final List<RegionalSite> sites;
  final List<RegionalConnection> connections;
  const RegionalGeography(
    this.seedText,
    this.sites,
    this.connections, {
    this.version = currentVersion,
  });
  RegionalSite site(String id) => sites.firstWhere((s) => s.id == id);
  List<RegionalConnection> neighbours(String id) =>
      connections.where((c) => c.contains(id)).toList();
  List<String>? path(
    String from,
    String to, {
    Set<String>? known,
    Set<String> closed = const {},
  }) {
    if (!sites.any((s) => s.id == from) ||
        !sites.any((s) => s.id == to) ||
        known != null && (!known.contains(from) || !known.contains(to))) {
      return null;
    }
    final costs = <String, int>{from: 0}, previous = <String, String>{};
    final pending = {from};
    while (pending.isNotEmpty) {
      final ordered = pending.toList()
        ..sort((a, b) {
          final cost = costs[a]!.compareTo(costs[b]!);
          return cost == 0 ? a.compareTo(b) : cost;
        });
      final at = ordered.first;
      pending.remove(at);
      if (at == to) {
        final result = <String>[];
        var current = at;
        while (current != from) {
          final id = previous[current]!;
          result.add(id);
          current = connections.firstWhere((c) => c.id == id).other(current);
        }
        return result.reversed.toList();
      }
      for (final edge in neighbours(at)) {
        if (closed.contains(edge.id)) continue;
        final next = edge.other(at);
        if (known != null && !known.contains(next)) continue;
        final cost = costs[at]! + edge.seconds;
        if (cost < (costs[next] ?? 1 << 30)) {
          costs[next] = cost;
          previous[next] = edge.id;
          pending.add(next);
        }
      }
    }
    return null;
  }

  factory RegionalGeography.generate(String input, EnvironmentPreset capital) {
    final text = canonicalSeed(input), seed = seedNumber(text);
    var rng = 1 + (seed + 715225739) % 2147483646;
    int next(int limit) {
      rng = rng * 48271 % 2147483647;
      return rng % limit;
    }

    const positions = [
      (100, 500),
      (300, 300),
      (330, 700),
      (500, 180),
      (560, 460),
      (600, 760),
      (760, 120),
      (800, 380),
      (830, 650),
      (940, 840),
    ];
    final later = [
      EnvironmentPreset.highlands,
      EnvironmentPreset.wetlands,
      EnvironmentPreset.frontier,
      EnvironmentPreset.plains,
      EnvironmentPreset.woodlands,
      EnvironmentPreset.river,
    ];
    for (var i = later.length - 1; i > 0; i--) {
      final j = next(i + 1), old = later[i];
      later[i] = later[j];
      later[j] = old;
    }
    final environments = [
      capital,
      EnvironmentPreset.woodlands,
      EnvironmentPreset.river,
      EnvironmentPreset.pineUplands,
      ...later,
    ];
    // Every region includes both discoveries; their terrain seeds are independent of graph draws.
    environments[7] = EnvironmentPreset.saltCoast;
    final sites = <RegionalSite>[
      for (var i = 0; i < positions.length; i++)
        RegionalSite(
          'site-${i.toString().padLeft(2, '0')}',
          (positions[i].$1 + next(61) - 30).clamp(20, 980),
          (positions[i].$2 + next(61) - 30).clamp(20, 980),
          environments[i],
          i == 0
              ? text
              : '${1 + (seed + (i + 1) * 104729 + 906883) % 2147483646}',
        ),
    ];
    const links = [
      (0, 1),
      (0, 2),
      (1, 2),
      (1, 3),
      (1, 4),
      (2, 4),
      (2, 5),
      (3, 4),
      (3, 6),
      (4, 7),
      (4, 5),
      (5, 8),
      (6, 7),
      (7, 8),
      (8, 9),
      (7, 9),
    ];
    final edges = <RegionalConnection>[];
    for (final (a, b) in links) {
      final first = sites[a], second = sites[b];
      final kinds = [first.environment, second.environment];
      final terrain =
          kinds.any(
            (p) => [
              EnvironmentPreset.highlands,
              EnvironmentPreset.pineUplands,
              EnvironmentPreset.frontier,
            ].contains(p),
          )
          ? 'pass'
          : kinds.any((p) => p.hasRiver || p == EnvironmentPreset.saltCoast)
          ? 'crossing'
          : kinds.contains(EnvironmentPreset.woodlands)
          ? 'forest'
          : 'road';
      final distance = (first.x - second.x).abs() + (first.y - second.y).abs();
      final cost =
          20 +
          distance ~/ 12 +
          (terrain == 'pass'
              ? 16
              : terrain == 'crossing'
              ? 10
              : terrain == 'forest'
              ? 6
              : 0);
      edges.add(
        RegionalConnection('link-$a-$b', first.id, second.id, cost, terrain),
      );
    }
    return RegionalGeography(
      text,
      List.unmodifiable(sites),
      List.unmodifiable(edges),
    );
  }

  Map<String, Object> toJson() => {
    'version': version,
    'seedText': seedText,
    'sites': sites.map((s) => s.toJson()).toList(),
    'connections': connections.map((c) => c.toJson()).toList(),
  };
  factory RegionalGeography.decode(dynamic value) {
    Never bad() => throw const FormatException('Invalid regional geography.');
    if (value is! Map ||
        value['version'] != currentVersion ||
        value['seedText'] is! String ||
        canonicalSeed(value['seedText']) != value['seedText'] ||
        value['sites'] is! List ||
        value['sites'].length < 3 ||
        value['sites'].length > 32 ||
        value['connections'] is! List ||
        value['connections'].length > 128) {
      bad();
    }
    final sites = (value['sites'] as List).map(RegionalSite.decode).toList();
    final ids = sites.map((s) => s.id).toSet();
    if (ids.length != sites.length ||
        !ids.contains('site-00') ||
        sites.map((s) => '${s.x},${s.y}').toSet().length != sites.length) {
      bad();
    }
    final edges = <RegionalConnection>[],
        edgeIds = <String>{},
        pairs = <String>{};
    for (final raw in value['connections']) {
      if (raw is! Map ||
          raw['id'] is! String ||
          !RegExp(r'^link-[0-9]+-[0-9]+$').hasMatch(raw['id']) ||
          !edgeIds.add(raw['id']) ||
          !ids.contains(raw['a']) ||
          !ids.contains(raw['b']) ||
          raw['a'] == raw['b'] ||
          raw['seconds'] is! int ||
          raw['seconds'] < 1 ||
          raw['seconds'] > 600 ||
          !['road', 'forest', 'crossing', 'pass'].contains(raw['terrain'])) {
        bad();
      }
      final pair = [raw['a'] as String, raw['b'] as String]..sort();
      if (!pairs.add(pair.join(':'))) bad();
      edges.add(
        RegionalConnection(
          raw['id'],
          raw['a'],
          raw['b'],
          raw['seconds'],
          raw['terrain'],
        ),
      );
    }
    final reachable = {'site-00'}, pending = Queue<String>()..add('site-00');
    while (pending.isNotEmpty) {
      final at = pending.removeFirst();
      for (final e in edges.where((e) => e.contains(at))) {
        if (reachable.add(e.other(at))) pending.add(e.other(at));
      }
    }
    if (reachable.length != sites.length) bad();
    return RegionalGeography(
      value['seedText'],
      List.unmodifiable(sites),
      List.unmodifiable(edges),
    );
  }
}
