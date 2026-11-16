import 'dart:collection';
import 'dart:convert';

import 'rules.dart';
import 'terrain.dart';
import 'condition.dart';
export 'terrain.dart';

enum Tool {
  cottage,
  path,
  remove,
  garden,
  green,
  workshop,
  market,
  hall,
  stoneRoad,
  gardenPath,
  clear,
  plant,
  bridge,
  firehouse,
  watchpost,
  refuge,
  inn,
  repair;

  String get label => switch (this) {
    cottage => 'Cottage',
    path => 'Path',
    remove => 'Remove',
    garden => 'Farm',
    green => 'Park',
    workshop => 'Workshop',
    market => 'Market',
    hall => 'Community Hall',
    stoneRoad => 'Stone Road',
    gardenPath => 'Garden Path',
    clear => 'Clear',
    plant => 'Plant',
    bridge => 'Bridge',
    firehouse => 'Firehouse',
    watchpost => 'Watchpost',
    refuge => 'Refuge',
    inn => 'Inn',
    repair => 'Repair',
  };
  bool get isLandscape => this == clear || this == plant || this == bridge;
  bool get isRoad => this == path || this == stoneRoad || this == gardenPath;
  RoadKind get roadKind => this == stoneRoad
      ? RoadKind.stone
      : this == gardenPath
      ? RoadKind.gardenPath
      : RoadKind.dirt;
  Place? get place => switch (this) {
    garden => Place.garden,
    green => Place.green,
    workshop => Place.workshop,
    market => Place.market,
    hall => Place.hall,
    firehouse => Place.firehouse,
    watchpost => Place.watchpost,
    refuge => Place.refuge,
    inn => Place.inn,
    _ => null,
  };
  String get purpose => switch (this) {
    cottage => 'A cottage beside a connected path.',
    path => 'Extend the lane, one cell at a time.',
    remove => 'Leave a little history behind.',
    garden =>
      '${Rules.gardenCapacity} work positions · earn coins while working.',
    green =>
      '${Rules.greenCapacity} visitor spaces · social visits improve mood; evening gatherings.',
    workshop => '1 specialised job · 0.4 coin/s while working.',
    market => '1 vendor + 3 customers · staffed visits earn trade income.',
    hall => '8 covered visitor spaces · preferred in rain.',
    stoneRoad => '18% quicker travel · stays quick in rain · 2 coins/cell.',
    gardenPath =>
      'Normal pace · +3 mood after traversal, once per day · 1 coin/cell.',
    clear => 'Clear trees or prepare marsh · 1 coin/cell · no road needed.',
    plant =>
      'Plant a tree on empty ground · 1 coin/cell · no automatic benefit.',
    bridge => 'Drag bank to bank, straight across 1–6 water cells · 3 coins/water cell.',
    firehouse =>
      '2 automatic responder jobs · real road travel to fires · 18 coins.',
    watchpost => '1 guard job · reachable wildlife response within 10 road-cost units · 14 coins.',
    refuge => '8 temporary shelter places · real evacuation, no permanent housing · 10 coins.',
    inn => '1 innkeeper + 4 covered social places · visitors can rest here · 16 coins.',
    repair => 'Repair damaged, safe buildings · 1–5 coins each · drag for one transaction.',
  };
}

enum RoadKind { dirt, stone, gardenPath }

enum Place {
  garden,
  green,
  workshop,
  market,
  hall,
  firehouse,
  watchpost,
  refuge,
  inn;

  String get label => switch (this) {
    garden => 'Farm',
    green => 'Park',
    workshop => 'Workshop',
    market => 'Market',
    hall => 'Community Hall',
    firehouse => 'Firehouse',
    watchpost => 'Watchpost',
    refuge => 'Refuge',
    inn => 'Inn',
  };
  int get workCapacity => switch (this) {
    garden => Rules.gardenCapacity,
    workshop => 1,
    market || watchpost || inn => 1,
    firehouse => 2,
    _ => 0,
  };
  int get visitorCapacity => switch (this) {
    green => Rules.greenCapacity,
    hall => 8,
    market => 3,
    inn => 4,
    _ => 0,
  };
  int get income => switch (this) {
    garden => Rules.workIncomePerSecond,
    workshop => 400,
    market || firehouse || watchpost => 100,
    inn => 150,
    _ => 0,
  };
  bool get protective => this == firehouse || this == watchpost;
  double get workDuration => protective
      ? 60
      : this == market || this == inn
      ? 35
      : Rules.workDuration;
}

enum Scenery { none, oak, vegetables }

enum Trace { none, pad, scar }

class Cell {
  final int x, y;
  const Cell(this.x, this.y);
  // south, east, west, north is also the stable cottage access preference.
  Iterable<Cell> get neighbours sync* {
    yield Cell(x, y + 1);
    yield Cell(x + 1, y);
    yield Cell(x - 1, y);
    yield Cell(x, y - 1);
  }

  @override
  bool operator ==(Object other) =>
      other is Cell && x == other.x && y == other.y;
  @override
  int get hashCode => Object.hash(x, y);
}

class Lot {
  final Terrain terrain;
  final BridgeAxis? bridge;
  final int grass;
  final Scenery scenery;
  final Trace trace;
  final bool path;
  final RoadKind road;
  final int? cottage;
  final String? cottageId;
  final Place? place;
  final String? placeId;
  const Lot({
    this.terrain = Terrain.ground,
    this.bridge,
    this.grass = 0,
    this.scenery = Scenery.none,
    this.trace = Trace.none,
    this.path = false,
    this.road = RoadKind.dirt,
    this.cottage,
    this.cottageId,
    this.place,
    this.placeId,
  });

  String? get buildingId => cottageId ?? placeId;

  List<Object?> toJson([String? identity]) => [
    'grass$grass',
    scenery.name,
    trace.name,
    path,
    cottage == null ? null : 'cottage$cottage',
    cottage == null ? null : identity ?? cottageId,
    place?.name,
    placeId,
    if (terrain != Terrain.ground || bridge != null) ...[
      path ? road.name : null,
      terrain.name,
      bridge?.name,
    ] else if (path && road != RoadKind.dirt)
      road.name,
  ];
}

int variation(int seed, Cell c, int salt) {
  // bounded integer arithmetic gives the same variants in the VM and JavaScript.
  var n = (seed + c.x * 193 + c.y * 389 + salt * 997) % 2147483647;
  n = (n * 48271) % 2147483647;
  return n;
}

class Landscape {
  final int seed, width, height;
  final GenerationInfo? generation;
  final List<Lot> lots;
  final int nextCottageId;
  Landscape(
    this.seed,
    this.width,
    this.height,
    Iterable<Lot> lots, {
    this.nextCottageId = 1,
    this.generation,
  }) : lots = List.unmodifiable(lots);
  Cell get arrival => Cell(0, height ~/ 2);
  bool contains(Cell c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;
  Lot at(Cell c) => lots[c.y * width + c.x];
  Iterable<Cell> get cells sync* {
    for (var y = 0; y < height; y++) {
      for (var x = 0; x < width; x++) {
        yield Cell(x, y);
      }
    }
  }

  factory Landscape.create([int seed = 202]) {
    const w = 40, h = 30;
    return Landscape(seed, w, h, [
      for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
          Lot(
            grass: (variation(seed, Cell(x, y), 1) ~/ 37) % 2,
            path: y == h ~/ 2 && x <= 7,
            scenery: (x <= 19 && y >= 11 && y <= 19)
                ? Scenery.none
                : variation(seed, Cell(x, y), 2) % 19 == 0
                ? Scenery.oak
                : variation(seed, Cell(x, y), 3) % 29 == 0
                ? Scenery.vegetables
                : Scenery.none,
          ),
    ]);
  }

  late final Set<Cell> activePaths = _connected();
  late final Set<Cell> completeBridgeCells = _completeBridges();
  Set<Cell> _completeBridges() {
    final valid = <Cell>{};
    for (final c in cells) {
      final axis = at(c).bridge;
      if (axis == null || valid.contains(c)) continue;
      final dx = axis == BridgeAxis.horizontal ? 1 : 0, dy = 1 - dx;
      Cell offset(Cell p, int n) => Cell(p.x + dx * n, p.y + dy * n);
      var start = c;
      while (contains(offset(start, -1)) &&
          at(offset(start, -1)).bridge == axis) {
        start = offset(start, -1);
      }
      final span = <Cell>[];
      var end = start;
      while (contains(end) && at(end).bridge == axis) {
        span.add(end);
        end = offset(end, 1);
      }
      final bank = offset(start, -1);
      bool landRoad(Cell p) =>
          contains(p) &&
          at(p).path &&
          at(p).bridge == null &&
          at(p).terrain.roadAllowed;
      if (span.length <= 6 && landRoad(bank) && landRoad(end)) {
        valid.addAll(span);
      }
    }
    return Set.unmodifiable(valid);
  }

  bool roadCell(Cell c) {
    if (!contains(c)) return false;
    final lot = at(c);
    return lot.path &&
        (lot.bridge != null
            ? completeBridgeCells.contains(c)
            : lot.terrain.roadAllowed);
  }

  bool roadStep(Cell from, Cell to) =>
      contains(from) &&
      roadCell(to) &&
      (from.x - to.x).abs() + (from.y - to.y).abs() == 1 &&
      _aligned(from, to) &&
      _aligned(to, from);
  bool _aligned(Cell c, Cell n) =>
      at(c).bridge == null ||
      (at(c).bridge == BridgeAxis.horizontal ? c.y == n.y : c.x == n.x);
  Iterable<Cell> pathNeighbours(Cell c) => c.neighbours.where(
    (n) => roadCell(n) && _aligned(n, c) && (!contains(c) || _aligned(c, n)),
  );
  Set<Cell> _connected() => pathComponent(arrival);
  Set<Cell> pathComponent(Cell start) {
    final result = <Cell>{};
    final queue = Queue<Cell>()..add(start);
    while (queue.isNotEmpty) {
      final c = queue.removeFirst();
      if (!contains(c) || !roadCell(c) || !result.add(c)) continue;
      queue.addAll(pathNeighbours(c));
    }
    return Set.unmodifiable(result);
  }

  List<Cell>? pathRoute(Cell start, Cell goal) {
    if (!contains(start) ||
        !contains(goal) ||
        !roadCell(start) ||
        !roadCell(goal)) {
      return null;
    }
    final previous = <Cell, Cell?>{start: null};
    final queue = Queue<Cell>()..add(start);
    while (queue.isNotEmpty) {
      final c = queue.removeFirst();
      if (c == goal) {
        final route = <Cell>[];
        for (Cell? n = goal; n != null; n = previous[n]) {
          route.add(n);
        }
        return route.reversed.toList();
      }
      for (final n in pathNeighbours(c)) {
        if (!previous.containsKey(n)) {
          previous[n] = c;
          queue.add(n);
        }
      }
    }
    return null;
  }

  String cottageKey(Cell c) => at(c).cottageId ?? 'legacy-${c.x}-${c.y}';
  late final Map<String, Cell> cottages = Map.unmodifiable({
    for (final c in cells)
      if (at(c).cottage != null) cottageKey(c): c,
  });
  late final Map<String, Cell> places = Map.unmodifiable({
    for (final c in cells)
      if (at(c).place != null) at(c).placeId!: c,
  });
  bool occupied(Cell c) =>
      at(c).path || at(c).cottage != null || at(c).place != null;
  Landscape withCounter(int counter) => Landscape(
    seed,
    width,
    height,
    lots,
    nextCottageId: counter,
    generation: generation,
  );
  Cell? localAccess(Cell home, {Cell? from}) {
    for (final n in pathNeighbours(home)) {
      if (from == null || pathRoute(from, n) != null) return n;
    }
    return null;
  }

  Cell? access(Cell c) {
    for (final n in pathNeighbours(c)) {
      if (activePaths.contains(n)) return n;
    }
    return null;
  }

  // bits N/E/S/W; cottage access adds a visual spur, never a network edge.
  int roadMask(Cell c) {
    var mask = 0;
    final ns = [
      Cell(c.x, c.y - 1),
      Cell(c.x + 1, c.y),
      Cell(c.x, c.y + 1),
      Cell(c.x - 1, c.y),
    ];
    for (var i = 0; i < ns.length; i++) {
      final n = ns[i];
      if (contains(n) &&
          (pathNeighbours(c).contains(n) ||
              ((at(n).cottage != null || at(n).place != null) &&
                  access(n) == c))) {
        mask |= 1 << i;
      }
    }
    if (c == arrival) mask |= 8;
    return mask;
  }

  Landscape replace(Cell c, Lot lot, {bool allocateCottage = false}) {
    final next = lots.toList();
    next[c.y * width + c.x] = lot;
    return Landscape(
      seed,
      width,
      height,
      next,
      nextCottageId: nextCottageId + (allocateCottage ? 1 : 0),
      generation: generation,
    );
  }

  Map<String, Object> toJson() => {
    'schema': 3,
    'nextCottageId': nextCottageId,
    'seed': seed,
    if (generation != null) 'generation': generation!.toJson(),
    'width': width,
    'height': height,
    'lots': [
      for (final c in cells)
        at(c).toJson(at(c).cottage != null ? cottageKey(c) : null),
    ],
  };
  String encode() => jsonEncode(toJson());

  factory Landscape.decode(String text) {
    if (text.length > 2000000) {
      throw const FormatException('File is too large.');
    }
    final d = jsonDecode(text);
    if (d is! Map || ![1, 2, 3, 4, 5].contains(d['schema'])) {
      throw const FormatException('Unsupported landscape format.');
    }
    int integer(String key, int min, int max) {
      final v = d[key];
      if (v is! int || v < min || v > max) {
        throw FormatException('Invalid $key.');
      }
      return v;
    }

    final seed = integer('seed', 0, 2147483646);
    final w = integer('width', 16, 80), h = integer('height', 12, 60);
    final legacy = d['schema'] == 1;
    final modern = (d['schema'] == 3 || d['schema'] == 4 || d['schema'] == 5);
    final nextId = legacy ? 1 : integer('nextCottageId', 1, 1000000000);
    final data = d['lots'];
    if (data is! List || data.length != w * h) {
      throw const FormatException('Invalid landscape dimensions.');
    }
    final lots = <Lot>[];
    final identities = <String>{};
    for (final row in data) {
      if (row is! List ||
          row.length !=
              (legacy
                  ? 5
                  : modern
                  ? (row.length == 11
                        ? 11
                        : row.length == 9
                        ? 9
                        : 8)
                  : 6) ||
          !['grass0', 'grass1'].contains(row[0]) ||
          !Scenery.values.any((s) => s.name == row[1]) ||
          !Trace.values.any((t) => t.name == row[2]) ||
          row[3] is! bool ||
          ![null, 'cottage0', 'cottage1'].contains(row[4])) {
        throw const FormatException('Invalid terrain or content identifier.');
      }
      final extended = modern && row.length == 11;
      final l = Lot(
        terrain: extended
            ? Terrain.values.firstWhere(
                (t) => t.name == row[9],
                orElse: () => throw const FormatException('Invalid terrain.'),
              )
            : Terrain.ground,
        bridge: extended && row[10] != null
            ? BridgeAxis.values.firstWhere(
                (a) => a.name == row[10],
                orElse: () =>
                    throw const FormatException('Invalid bridge axis.'),
              )
            : null,
        grass: row[0] == 'grass0' ? 0 : 1,
        scenery: Scenery.values.firstWhere((s) => s.name == row[1]),
        trace: Trace.values.firstWhere((t) => t.name == row[2]),
        path: row[3],
        road: modern && (row.length == 9 || (extended && row[8] != null))
            ? RoadKind.values.firstWhere(
                (r) => r.name == row[8],
                orElse: () => throw const FormatException('Invalid road kind.'),
              )
            : RoadKind.dirt,
        cottage: row[4] == null
            ? null
            : row[4] == 'cottage0'
            ? 0
            : 1,
        cottageId: row[4] == null
            ? null
            : legacy
            ? 'legacy-${lots.length % w}-${lots.length ~/ w}'
            : row[5] is String
            ? row[5]
            : null,
        place: modern && row[6] != null
            ? Place.values.firstWhere(
                (p) => p.name == row[6],
                orElse: () => throw const FormatException('Invalid place.'),
              )
            : null,
        placeId: modern && row[7] is String ? row[7] : null,
      );
      if (extended &&
          ((l.bridge != null &&
                  (l.terrain != Terrain.water ||
                      !l.path ||
                      l.road != RoadKind.dirt)) ||
              (l.path && l.bridge == null && !l.terrain.roadAllowed) ||
              ((l.cottage != null || l.place != null) &&
                  !l.terrain.buildable) ||
              (l.terrain == Terrain.forest && l.scenery != Scenery.oak) ||
              ((l.terrain == Terrain.water || l.terrain == Terrain.rock) &&
                  l.scenery != Scenery.none) ||
              (!l.path && row[8] != null))) {
        throw const FormatException('Inconsistent terrain footprint.');
      }
      if (row.length == 9 && !l.path) {
        throw const FormatException('Road kind without a road.');
      }
      if (modern &&
          ((l.place == null) != (row[7] == null) ||
              (l.place != null &&
                  (l.placeId == null ||
                      l.cottage != null ||
                      l.path ||
                      l.scenery != Scenery.none ||
                      l.trace != Trace.pad)))) {
        throw const FormatException('Invalid place footprint or identity.');
      }
      if (l.place != null &&
          (!RegExp('^${l.place!.name}-[0-9]+\$').hasMatch(l.placeId!) ||
              !identities.add(l.placeId!) ||
              int.parse(l.placeId!.split('-').last) < 1 ||
              int.parse(l.placeId!.split('-').last) >= nextId)) {
        throw const FormatException('Invalid or duplicate place identity.');
      }
      if (!legacy && row[4] == null && row[5] != null) {
        throw const FormatException('Orphan cottage identity.');
      }
      if (l.cottage != null) {
        final id = l.cottageId;
        if (id == null ||
            !RegExp(r'^[A-Za-z0-9_-]{1,64}$').hasMatch(id) ||
            !identities.add(id)) {
          throw const FormatException('Invalid or duplicate cottage identity.');
        }
        if (id.startsWith('home-')) {
          final number = int.tryParse(id.substring(5));
          if (number == null || number < 1 || number >= nextId) {
            throw const FormatException('Invalid cottage identity counter.');
          }
        }
      }
      if ((l.path && l.cottage != null) ||
          ((l.path || l.cottage != null) && l.scenery != Scenery.none) ||
          (l.path && l.trace != Trace.none) ||
          (l.cottage != null && l.trace != Trace.pad)) {
        throw const FormatException('Overlapping or inconsistent contents.');
      }
      lots.add(l);
    }
    final generation = d.containsKey('generation')
        ? GenerationInfo.decode(d['generation'])
        : null;
    if (generation != null && (generation.seed != seed || w != 40 || h != 30)) {
      throw const FormatException(
        'Generation metadata does not match this world.',
      );
    }
    final result = Landscape(
      seed,
      w,
      h,
      lots,
      nextCottageId: nextId,
      generation: generation,
    );
    if (!result.roadCell(result.arrival)) {
      throw const FormatException('The arrival path is missing.');
    }
    return result;
  }
}

class Placement {
  final Landscape before;
  final Landscape? after;
  final Cell cell;
  final String message;
  final Cell? access;
  final Cell? movingFrom;
  final int? charge;
  Placement(
    this.before,
    this.after,
    this.cell,
    this.message, [
    this.access,
    this.movingFrom,
    this.charge,
  ]);
  bool get valid => after != null;
  int get cost {
    if (!valid) return 0;
    if (charge != null) return charge!;
    final lot = after!.at(cell);
    if (!lot.path && lot.cottage == null && lot.place == null) return 0;
    return Rules.cost(
      lot.place?.name ??
          (lot.cottage != null
              ? 'cottage'
              : lot.road == RoadKind.stone
              ? 'stoneRoad'
              : lot.road == RoadKind.gardenPath
              ? 'gardenPath'
              : 'path'),
    );
  }

  late final List<Cell> _affected = _findAffected().toList(growable: false);
  Iterable<Cell> get affected => _affected;
  Iterable<Cell> _findAffected() sync* {
    if (after == null) return;
    for (final c in before.cells) {
      if (c == cell ||
          c == movingFrom ||
          ((before.at(c).path || after!.at(c).path) &&
              (before.roadMask(c) != after!.roadMask(c) ||
                  before.activePaths.contains(c) !=
                      after!.activePaths.contains(c))) ||
          ((before.at(c).cottage != null || before.at(c).place != null) &&
              before.access(c) != after!.access(c))) {
        yield c;
      }
    }
  }
}

Tool? buildingTool(Lot lot) => lot.cottage != null
    ? Tool.cottage
    : lot.place == null
    ? null
    : Tool.values.firstWhere((t) => t.place == lot.place);

Placement evaluateMove(Landscape world, Cell from, Cell to) {
  Placement no(String why) => Placement(world, null, to, why, null, from);
  if (!world.contains(from) || buildingTool(world.at(from)) == null) {
    return no('Choose an existing building.');
  }
  if (from == to) return no('Choose another plot · Escape cancels.');
  final original = world.at(from);
  final vacated = evaluate(world, Tool.remove, from).after!;
  final placement = evaluate(vacated, buildingTool(original)!, to);
  if (!placement.valid) return no(placement.message);
  final destination = world.at(to);
  final after = vacated.replace(
    to,
    Lot(
      grass: destination.grass,
      terrain: destination.terrain,
      trace: Trace.pad,
      cottage: original.cottage,
      cottageId: original.cottage == null ? null : world.cottageKey(from),
      place: original.place,
      placeId: original.placeId,
    ),
  );
  return Placement(
    world,
    after,
    to,
    'Move ${buildingTool(original)!.label} · Free · click to place · Escape cancels',
    after.access(to),
    from,
  );
}

Placement evaluate(
  Landscape world,
  Tool tool,
  Cell c, {
  Map<String, BuildingCondition>? conditions,
  bool Function(String)? unsafe,
}) {
  Placement no(String why) => Placement(world, null, c, why);
  if (!world.contains(c)) return no('Stay within the landscape.');
  final old = world.at(c);
  if (tool == Tool.bridge) {
    return no('Drag from a land bank across water to the opposite bank.');
  }
  if (tool == Tool.clear) {
    if (world.occupied(c)) {
      return no('Remove the road or building before preparing this ground.');
    }
    if (old.terrain != Terrain.forest &&
        old.terrain != Terrain.marsh &&
        old.scenery != Scenery.oak) {
      return no('Clear prepares trees or wet ground; water and rock stay.');
    }
    return Placement(
      world,
      world.replace(
        c,
        Lot(
          grass: old.grass,
          trace: old.trace == Trace.none ? Trace.scar : old.trace,
        ),
      ),
      c,
      'Prepare ${old.terrain == Terrain.marsh ? 'wet ground' : 'woodland'} · 1 coin',
      null,
      null,
      1000,
    );
  }
  if (tool == Tool.plant) {
    if (world.occupied(c) ||
        old.scenery != Scenery.none ||
        old.terrain != Terrain.ground) {
      return no(
        'Plant on empty ordinary ground, away from roads and buildings.',
      );
    }
    return Placement(
      world,
      world.replace(
        c,
        Lot(
          grass: old.grass,
          terrain: Terrain.forest,
          scenery: Scenery.oak,
          trace: old.trace,
        ),
      ),
      c,
      'Plant a tree · 1 coin',
      null,
      null,
      1000,
    );
  }
  if (tool == Tool.repair) {
    final id = old.buildingId, record = conditions?[old.buildingId];
    if (id == null || record == null || record.health == 100) {
      return no('Choose a damaged building.');
    }
    if (unsafe?.call(id) ?? false) {
      return no(
        'The incident is still active here. Wait until it is safe to repair.',
      );
    }
    return Placement(
      world,
      world,
      c,
      'Repair ${old.place?.label ?? 'Cottage'} · ${Rules.exactCoins(record.repairCost)} coins · restores operation',
      null,
      null,
      record.repairCost,
    );
  }
  if (tool == Tool.remove) {
    if (c == world.arrival) return no('The arrival lane is protected.');
    if (!old.path && old.cottage == null && old.place == null) {
      return no('Nothing to remove here.');
    }
    final next = world.replace(
      c,
      Lot(
        grass: old.grass,
        terrain: old.terrain,
        trace: old.path ? Trace.scar : Trace.pad,
      ),
    );
    final lost = world.cells
        .where(
          (n) =>
              world.at(n).cottage != null &&
              world.access(n) != null &&
              next.access(n) == null,
        )
        .length;
    return Placement(
      world,
      next,
      c,
      old.path
          ? 'Remove path${lost > 0 ? ' · $lost ${lost == 1 ? 'cottage loses' : 'cottages lose'} access' : ''} · leaves a scar'
          : 'Remove ${old.place?.label ?? 'cottage'} · leaves a buildable pad',
    );
  }
  if (old.bridge != null) {
    return no('Use Remove for bridges; complete a crossing with Bridge.');
  }
  if (tool.isRoad && !old.terrain.roadAllowed ||
      !tool.isRoad && !old.terrain.buildable) {
    return no(old.terrain.instruction);
  }
  if (tool.isRoad && old.path) {
    if (old.road == tool.roadKind) {
      return no('This road already has that surface.');
    }
    return Placement(
      world,
      world.replace(
        c,
        Lot(
          grass: old.grass,
          terrain: old.terrain,
          path: true,
          road: tool.roadKind,
        ),
      ),
      c,
      'Change to ${tool.label} · ${Rules.coins(Rules.cost(tool.name))} coins',
    );
  }
  if (world.occupied(c)) return no('This ground is occupied.');
  if (old.scenery == Scenery.oak) return no('A tree occupies this ground.');
  final access = world.access(c);
  if (access == null) {
    return no(
      tool.isRoad
          ? 'Extend a path connected to the arrival lane.'
          : 'This place needs an adjoining connected path.',
    );
  }
  final replacement = old.scenery == Scenery.vegetables
      ? ' · replaces plants'
      : '';
  return Placement(
    world,
    world.replace(
      c,
      Lot(
        grass: old.grass,
        terrain: old.terrain,
        path: tool.isRoad,
        road: tool.roadKind,
        trace: tool.isRoad ? Trace.none : Trace.pad,
        cottage: tool == Tool.cottage ? variation(world.seed, c, 4) % 2 : null,
        cottageId: tool == Tool.cottage ? 'home-${world.nextCottageId}' : null,
        place: tool.place,
        placeId: tool.place == null
            ? null
            : '${tool.place!.name}-${world.nextCottageId}',
      ),
      allocateCottage: !tool.isRoad,
    ),
    c,
    '${tool.isRoad ? 'Extend ${tool.label}' : 'Place ${tool.label}'}$replacement',
    access,
  );
}

class ConstructionEdit {
  final Landscape before, after;
  final int paid;
  final List<RepairChange> repairs;
  ConstructionEdit(
    this.before,
    this.after,
    this.paid, [
    this.repairs = const [],
  ]);
}

class LandscapeSession {
  Landscape world;
  final Map<String, BuildingCondition> conditions = {};
  bool Function(String)? unsafeBuilding;
  double Function()? clock;
  BrushStroke? stroke;
  BuildingCondition condition(String id) =>
      conditions.putIfAbsent(id, () => BuildingCondition());
  Placement preview(Tool tool, Cell c) =>
      evaluate(world, tool, c, conditions: conditions, unsafe: unsafeBuilding);
  int revision = 0;
  int money;
  bool unlimited;
  String? historyError;
  Landscape get view => stroke?.staged ?? world;
  final List<ConstructionEdit> _undo = [], _redo = [];
  static const historyLimit = 80;
  LandscapeSession(
    this.world, {
    this.money = Rules.startingMoney,
    this.unlimited = false,
  });
  bool get canUndo => _undo.isNotEmpty;
  bool get canRedo => _redo.isNotEmpty;
  bool commit(Placement plan) {
    if (stroke != null ||
        plan.movingFrom != null ||
        !identical(plan.before, world) ||
        !plan.valid) {
      return false;
    }
    if (identical(plan.before, plan.after)) {
      final checked = preview(Tool.repair, plan.cell);
      if (!checked.valid) return false;
      beginStroke(Tool.repair, plan.cell);
      return finishStroke();
    }
    final price = unlimited ? 0 : plan.cost;
    if (price > money) return false;
    return _commit(plan.after!, price);
  }

  bool relocate(Placement plan) {
    if (stroke != null ||
        plan.movingFrom == null ||
        !identical(plan.before, world) ||
        !plan.valid) {
      return false;
    }
    if (unsafeBuilding?.call(world.at(plan.movingFrom!).buildingId!) ?? false) {
      return false;
    }
    final checked = evaluateMove(world, plan.movingFrom!, plan.cell);
    if (!checked.valid) return false;
    return _commit(checked.after!, 0);
  }

  bool _commit(Landscape next, int price) {
    _undo.add(ConstructionEdit(world, next, price));
    if (_undo.length > historyLimit) _undo.removeAt(0);
    _redo.clear();
    world = next;
    money -= price;
    revision++;
    return true;
  }

  void _restore(Landscape next) {
    // durable identities are never reused after undo or a history branch.
    final counter = world.nextCottageId > next.nextCottageId
        ? world.nextCottageId
        : next.nextCottageId;
    world = next.withCounter(counter);
  }

  bool undo() {
    historyError = null;
    if (stroke != null) {
      cancelStroke();
      return false;
    }
    if (!canUndo) return false;
    final edit = _undo.removeLast();
    if (edit.repairs.isNotEmpty) {
      if (edit.repairs.any(
        (r) =>
            condition(r.id).damageRevision != r.damageRevision ||
            condition(r.id).health != 100 ||
            condition(r.id).productive != r.productive ||
            condition(r.id).uses != r.uses ||
            (unsafeBuilding?.call(r.id) ?? false),
      )) {
        historyError = 'This repair has since been used or met another incident. Its completed work and cost stay forward; Undo again for earlier construction.';
        return false;
      }
      for (final r in edit.repairs) {
        condition(r.id).health = r.health;
      }
    } else {
      _restore(edit.before);
    }
    money += edit.paid;
    _redo.add(edit);
    revision++;
    return true;
  }

  bool redo() {
    historyError = null;
    if (stroke != null) {
      cancelStroke();
      return false;
    }
    if (!canRedo) return false;
    final edit = _redo.last;
    if (money < edit.paid) {
      historyError =
          'Redo needs ${Rules.coins(edit.paid)} coins. The grant and farm work can help.';
      return false;
    }
    if (edit.repairs.isNotEmpty &&
        edit.repairs.any(
          (r) =>
              condition(r.id).damageRevision != r.damageRevision ||
              condition(r.id).health != r.health ||
              (unsafeBuilding?.call(r.id) ?? false),
        )) {
      _redo.removeLast();
      historyError = 'This repair changed after Undo. Make a new Repair stroke at its current cost.';
      return false;
    }
    _redo.removeLast();
    if (edit.repairs.isNotEmpty) {
      for (final r in edit.repairs) {
        final record = condition(r.id);
        record.health = 100;
        record.repairs++;
        record.repairedAt = clock?.call() ?? 0;
      }
    } else {
      _restore(edit.after);
    }
    money -= edit.paid;
    _undo.add(edit);
    revision++;
    return true;
  }

  void replace(Landscape next) {
    cancelStroke();
    world = next;
    _undo.clear();
    _redo.clear();
    revision++;
  }

  void beginStroke(Tool tool, Cell start) {
    cancelStroke();
    stroke = BrushStroke(
      world,
      tool,
      budget: money,
      free: unlimited,
      conditions: conditions,
      unsafe: unsafeBuilding,
    )..paintTo(start);
  }

  bool finishStroke() {
    final edit = stroke;
    stroke = null;
    if (edit == null ||
        edit.changed.isEmpty ||
        !identical(edit.before, world) ||
        edit.price > money) {
      return false;
    }
    if (edit.tool == Tool.repair) {
      if (edit.repairChanges.any(
        (r) =>
            condition(r.id).health != r.health ||
            condition(r.id).damageRevision != r.damageRevision ||
            (unsafeBuilding?.call(r.id) ?? false),
      )) {
        historyError = 'The incident changed a staged repair. Nothing was charged; try again when safe.';
        return false;
      }
      for (final r in edit.repairChanges) {
        final record = condition(r.id);
        record.health = 100;
        record.repairs++;
        record.repairedAt = clock?.call() ?? 0;
      }
      _undo.add(
        ConstructionEdit(world, world, edit.price, [
          for (final r in edit.repairChanges)
            RepairChange(
              r.id,
              r.health,
              r.damageRevision,
              condition(r.id).productive,
              condition(r.id).uses,
            ),
        ]),
      );
      if (_undo.length > historyLimit) _undo.removeAt(0);
      _redo.clear();
      money -= edit.price;
      revision++;
      return true;
    }
    return _commit(edit.staged, edit.price);
  }

  void cancelStroke() => stroke = null;
  SaveSnapshot snapshot() => SaveSnapshot(revision, world.encode());
  bool persist(SaveSnapshot snapshot, void Function(String) write) {
    if (snapshot.revision != revision || snapshot.text != world.encode()) {
      return false;
    }
    write(snapshot.text);
    return true;
  }

  void importText(String text) => replace(Landscape.decode(text));
}

class SaveSnapshot {
  final int revision;
  final String text;
  const SaveSnapshot(this.revision, this.text);
}

Iterable<Cell> cardinalStroke(Cell from, Cell to) sync* {
  var x = from.x, y = from.y, ix = 0, iy = 0;
  final ax = (to.x - x).abs(), ay = (to.y - y).abs();
  final sx = to.x.compareTo(x), sy = to.y.compareTo(y);
  yield from;
  // centre-to-centre grid crossings; an exact corner crosses X before Y.
  while (ix < ax || iy < ay) {
    if (ix < ax && (iy >= ay || (2 * ix + 1) * ay <= (2 * iy + 1) * ax)) {
      x += sx;
      ix++;
    } else {
      y += sy;
      iy++;
    }
    yield Cell(x, y);
  }
}

class BrushStroke {
  final Landscape before;
  Landscape staged;
  final Tool tool;
  final Set<Cell> changed = {};
  Cell? last, bridgeStart;
  Placement? firstInvalid;
  final int budget;
  final bool free;
  final Map<String, BuildingCondition>? conditions;
  final bool Function(String)? unsafe;
  final List<RepairChange> repairChanges = [];
  int price = 0;
  BrushStroke(
    this.before,
    this.tool, {
    this.budget = Rules.startingMoney,
    this.free = false,
    this.conditions,
    this.unsafe,
  }) : staged = before;
  void paintTo(Cell target) {
    if (tool == Tool.bridge) {
      bridgeStart ??= target;
      final plan = evaluateBridge(before, bridgeStart!, target);
      changed.clear();
      staged = before;
      price = 0;
      firstInvalid = null;
      if (!plan.valid || (!free && plan.cost > budget)) {
        firstInvalid = plan.valid
            ? Placement(
                before,
                null,
                target,
                'This crossing needs ${Rules.coins(plan.cost)} coins.',
              )
            : plan;
        return;
      }
      staged = plan.after!;
      price = free ? 0 : plan.cost;
      changed.addAll(
        before.cells.where((c) => !identical(before.at(c), staged.at(c))),
      );
      return;
    }
    for (final c in cardinalStroke(last ?? target, target)) {
      if (changed.contains(c)) continue;
      if (tool.isRoad &&
          staged.contains(c) &&
          staged.at(c).path &&
          staged.at(c).road == tool.roadKind) {
        continue;
      }
      final plan = evaluate(
        staged,
        tool,
        c,
        conditions: conditions,
        unsafe: unsafe,
      );
      final cost = free ? 0 : plan.cost;
      if (plan.valid && price + cost > budget) {
        firstInvalid ??= Placement(
          staged,
          null,
          c,
          'Stroke budget used · release before using new income.',
        );
        continue;
      }
      if (plan.valid) {
        price += cost;
        if (tool == Tool.repair) {
          final id = staged.at(c).buildingId!, record = conditions![id]!;
          repairChanges.add(
            RepairChange(id, record.health, record.damageRevision),
          );
        }
        staged = plan.after!;
        changed.add(c);
      } else {
        firstInvalid ??= plan;
      }
    }
    last = target;
  }
}

// bank-to-bank is one transaction; incomplete strokes never enter the road graph.
Placement evaluateBridge(Landscape world, Cell start, Cell end) {
  Placement no(String why) => Placement(world, null, end, why);
  if (!world.contains(start) || !world.contains(end)) {
    return no('Keep both banks inside the landscape.');
  }
  if (start == end || (start.x != end.x && start.y != end.y)) {
    return no('Drag straight from one land bank to the other.');
  }
  final cells = cardinalStroke(start, end).toList();
  if (cells.length < 3 || cells.length > 8) {
    return no('Cross 1–6 water cells, with land at both ends.');
  }
  final axis = start.y == end.y ? BridgeAxis.horizontal : BridgeAxis.vertical;
  for (final bank in [start, end]) {
    final lot = world.at(bank);
    if (!lot.terrain.roadAllowed ||
        lot.cottage != null ||
        lot.place != null ||
        lot.scenery == Scenery.oak) {
      return no('Both banks need clear land or an ordinary path.');
    }
  }
  if (!world.activePaths.contains(start) &&
      !world.activePaths.contains(end) &&
      world.access(start) == null &&
      world.access(end) == null) {
    return no('Connect one bank to the arrival lane first.');
  }
  var waterChanges = 0;
  for (final c in cells.skip(1).take(cells.length - 2)) {
    final lot = world.at(c);
    if (lot.terrain != Terrain.water ||
        (lot.bridge != null && lot.bridge != axis)) {
      return no('The span must cross uninterrupted water in one direction.');
    }
    if (lot.bridge == null) waterChanges++;
  }
  if (waterChanges == 0 && world.roadCell(cells[1])) {
    return no('This crossing is already complete.');
  }
  final lots = world.lots.toList();
  for (final c in cells) {
    final old = world.at(c);
    if (old.path) continue;
    lots[c.y * world.width + c.x] = Lot(
      grass: old.grass,
      terrain: old.terrain,
      path: true,
      bridge: old.terrain == Terrain.water ? axis : null,
    );
  }
  final next = Landscape(
    world.seed,
    world.width,
    world.height,
    lots,
    nextCottageId: world.nextCottageId,
    generation: world.generation,
  );
  if (!next.roadCell(cells[1])) {
    return no('This span needs both complete land approaches.');
  }
  return Placement(
    world,
    next,
    end,
    'Complete crossing · ${Rules.coins(waterChanges * Rules.cost('bridge'))} coins',
    null,
    null,
    waterChanges * Rules.cost('bridge'),
  );
}
