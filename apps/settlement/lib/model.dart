import 'dart:collection';
import 'dart:convert';

import 'rules.dart';

enum Tool { cottage, path, remove, garden, green }

enum Place { garden, green }

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
  final int grass;
  final Scenery scenery;
  final Trace trace;
  final bool path;
  final int? cottage;
  final String? cottageId;
  final Place? place;
  final String? placeId;
  const Lot({
    this.grass = 0,
    this.scenery = Scenery.none,
    this.trace = Trace.none,
    this.path = false,
    this.cottage,
    this.cottageId,
    this.place,
    this.placeId,
  });

  List<Object?> toJson([String? identity]) => [
    'grass$grass',
    scenery.name,
    trace.name,
    path,
    cottage == null ? null : 'cottage$cottage',
    cottage == null ? null : identity ?? cottageId,
    place?.name,
    placeId,
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
  final List<Lot> lots;
  final int nextCottageId;
  Landscape(
    this.seed,
    this.width,
    this.height,
    Iterable<Lot> lots, {
    this.nextCottageId = 1,
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
  Iterable<Cell> pathNeighbours(Cell c) =>
      c.neighbours.where((n) => contains(n) && at(n).path);
  Set<Cell> _connected() => pathComponent(arrival);
  Set<Cell> pathComponent(Cell start) {
    final result = <Cell>{};
    final queue = Queue<Cell>()..add(start);
    while (queue.isNotEmpty) {
      final c = queue.removeFirst();
      if (!contains(c) || !at(c).path || !result.add(c)) continue;
      queue.addAll(pathNeighbours(c));
    }
    return Set.unmodifiable(result);
  }

  List<Cell>? pathRoute(Cell start, Cell goal) {
    if (!contains(start) ||
        !contains(goal) ||
        !at(start).path ||
        !at(goal).path) {
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
  Landscape withCounter(int counter) =>
      Landscape(seed, width, height, lots, nextCottageId: counter);
  Cell? localAccess(Cell home, {Cell? from}) {
    for (final n in pathNeighbours(home)) {
      if (from == null || pathRoute(from, n) != null) return n;
    }
    return null;
  }

  Cell? access(Cell c) {
    for (final n in c.neighbours) {
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
          (at(n).path ||
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
    );
  }

  Map<String, Object> toJson() => {
    'schema': 3,
    'nextCottageId': nextCottageId,
    'seed': seed,
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
    if (d is! Map || ![1, 2, 3].contains(d['schema'])) {
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
    final modern = d['schema'] == 3;
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
                  ? 8
                  : 6) ||
          !['grass0', 'grass1'].contains(row[0]) ||
          !Scenery.values.any((s) => s.name == row[1]) ||
          !Trace.values.any((t) => t.name == row[2]) ||
          row[3] is! bool ||
          ![null, 'cottage0', 'cottage1'].contains(row[4])) {
        throw const FormatException('Invalid terrain or content identifier.');
      }
      final l = Lot(
        grass: row[0] == 'grass0' ? 0 : 1,
        scenery: Scenery.values.firstWhere((s) => s.name == row[1]),
        trace: Trace.values.firstWhere((t) => t.name == row[2]),
        path: row[3],
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
    final result = Landscape(seed, w, h, lots, nextCottageId: nextId);
    if (!result.at(result.arrival).path) {
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
  const Placement(
    this.before,
    this.after,
    this.cell,
    this.message, [
    this.access,
  ]);
  bool get valid => after != null;
  Iterable<Cell> get affected sync* {
    if (after == null) return;
    for (final c in before.cells) {
      if (c == cell ||
          ((before.at(c).path || after!.at(c).path) &&
              (before.roadMask(c) != after!.roadMask(c) ||
                  before.activePaths.contains(c) !=
                      after!.activePaths.contains(c))) ||
          (before.at(c).cottage != null &&
              before.access(c) != after!.access(c))) {
        yield c;
      }
    }
  }
}

Placement evaluate(Landscape world, Tool tool, Cell c) {
  Placement no(String why) => Placement(world, null, c, why);
  if (!world.contains(c)) return no('Stay within the landscape.');
  final old = world.at(c);
  if (tool == Tool.remove) {
    if (c == world.arrival) return no('The arrival lane is protected.');
    if (!old.path && old.cottage == null && old.place == null) {
      return no('Nothing to remove here.');
    }
    final next = world.replace(
      c,
      Lot(grass: old.grass, trace: old.path ? Trace.scar : Trace.pad),
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
          : 'Remove ${old.place?.name ?? 'cottage'} · leaves a buildable pad',
    );
  }
  if (world.occupied(c)) return no('This ground is occupied.');
  if (old.scenery == Scenery.oak) return no('A tree occupies this ground.');
  final access = world.access(c);
  if (access == null) {
    return no(
      tool == Tool.path
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
        path: tool == Tool.path,
        trace: tool == Tool.path ? Trace.none : Trace.pad,
        cottage: tool == Tool.cottage ? variation(world.seed, c, 4) % 2 : null,
        cottageId: tool == Tool.cottage ? 'home-${world.nextCottageId}' : null,
        place: tool == Tool.garden
            ? Place.garden
            : tool == Tool.green
            ? Place.green
            : null,
        placeId: tool == Tool.garden || tool == Tool.green
            ? '${tool.name}-${world.nextCottageId}'
            : null,
      ),
      allocateCottage: tool != Tool.path,
    ),
    c,
    '${tool == Tool.path ? 'Extend path' : 'Place ${tool.name}'}$replacement',
    access,
  );
}

class ConstructionEdit {
  final Landscape before, after;
  final int paid;
  ConstructionEdit(this.before, this.after, this.paid);
}

class LandscapeSession {
  Landscape world;
  BrushStroke? stroke;
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
    if (stroke != null || !identical(plan.before, world) || !plan.valid) {
      return false;
    }
    final lot = plan.after!.at(plan.cell);
    final price = unlimited
        ? 0
        : Rules.cost(
            lot.place?.name ?? (lot.cottage != null ? 'cottage' : 'path'),
          );
    if (price > money) return false;
    return _commit(plan.after!, price);
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
    _restore(edit.before);
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
          'Redo needs ${Rules.coins(edit.paid)} coins. The grant and garden work can help.';
      return false;
    }
    _redo.removeLast();
    _restore(edit.after);
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
    stroke = BrushStroke(world, tool, budget: money, free: unlimited)
      ..paintTo(start);
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
  Cell? last;
  Placement? firstInvalid;
  final int budget;
  final bool free;
  int price = 0;
  BrushStroke(
    this.before,
    this.tool, {
    this.budget = Rules.startingMoney,
    this.free = false,
  }) : staged = before;
  void paintTo(Cell target) {
    for (final c in cardinalStroke(last ?? target, target)) {
      if (changed.contains(c)) continue;
      if (tool == Tool.path && staged.contains(c) && staged.at(c).path) {
        continue;
      }
      final plan = evaluate(staged, tool, c);
      final cost = free ? 0 : Rules.cost(tool.name);
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
        staged = plan.after!;
        changed.add(c);
      } else {
        firstInvalid ??= plan;
      }
    }
    last = target;
  }
}
