import 'dart:collection';
import 'dart:convert';

enum Tool { cottage, path, remove }

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
  const Lot({
    this.grass = 0,
    this.scenery = Scenery.none,
    this.trace = Trace.none,
    this.path = false,
    this.cottage,
  });

  List<Object?> toJson() => [
    'grass$grass',
    scenery.name,
    trace.name,
    path,
    cottage == null ? null : 'cottage$cottage',
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
  Landscape(this.seed, this.width, this.height, Iterable<Lot> lots)
    : lots = List.unmodifiable(lots);
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
  Set<Cell> _connected() {
    final result = <Cell>{};
    final queue = Queue<Cell>()..add(arrival);
    while (queue.isNotEmpty) {
      final c = queue.removeFirst();
      if (!contains(c) || !at(c).path || !result.add(c)) continue;
      queue.addAll(c.neighbours);
    }
    return Set.unmodifiable(result);
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
          (at(n).path || (at(n).cottage != null && access(n) == c))) {
        mask |= 1 << i;
      }
    }
    if (c == arrival) mask |= 8;
    return mask;
  }

  Landscape replace(Cell c, Lot lot) {
    final next = lots.toList();
    next[c.y * width + c.x] = lot;
    return Landscape(seed, width, height, next);
  }

  Map<String, Object> toJson() => {
    'schema': 1,
    'seed': seed,
    'width': width,
    'height': height,
    'lots': lots.map((l) => l.toJson()).toList(),
  };
  String encode() => jsonEncode(toJson());

  factory Landscape.decode(String text) {
    if (text.length > 2000000) {
      throw const FormatException('File is too large.');
    }
    final d = jsonDecode(text);
    if (d is! Map || d['schema'] != 1) {
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
    final data = d['lots'];
    if (data is! List || data.length != w * h) {
      throw const FormatException('Invalid landscape dimensions.');
    }
    final lots = <Lot>[];
    for (final row in data) {
      if (row is! List ||
          row.length != 5 ||
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
      );
      if ((l.path && l.cottage != null) ||
          ((l.path || l.cottage != null) && l.scenery != Scenery.none) ||
          (l.path && l.trace != Trace.none) ||
          (l.cottage != null && l.trace != Trace.pad)) {
        throw const FormatException('Overlapping or inconsistent contents.');
      }
      lots.add(l);
    }
    final result = Landscape(seed, w, h, lots);
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
    if (!old.path && old.cottage == null) return no('Nothing to remove here.');
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
          : 'Remove cottage · leaves a buildable pad',
    );
  }
  if (old.path || old.cottage != null) return no('This ground is occupied.');
  if (old.scenery == Scenery.oak) return no('A tree occupies this ground.');
  final access = world.access(c);
  if (access == null) {
    return no(
      tool == Tool.path
          ? 'Extend a path connected to the arrival lane.'
          : 'A cottage needs an adjoining connected path.',
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
        trace: tool == Tool.cottage ? Trace.pad : Trace.none,
        cottage: tool == Tool.cottage ? variation(world.seed, c, 4) % 2 : null,
      ),
    ),
    c,
    '${tool == Tool.path ? 'Extend path' : 'Place cottage'}$replacement',
    access,
  );
}

class LandscapeSession {
  Landscape world;
  final List<(Landscape, Landscape)> _undo = [], _redo = [];
  static const historyLimit = 80;
  LandscapeSession(this.world);
  bool get canUndo => _undo.isNotEmpty;
  bool get canRedo => _redo.isNotEmpty;
  bool commit(Placement plan) {
    if (!identical(plan.before, world) || !plan.valid) return false;
    _undo.add((world, plan.after!));
    if (_undo.length > historyLimit) _undo.removeAt(0);
    _redo.clear();
    world = plan.after!;
    return true;
  }

  bool undo() {
    if (!canUndo) return false;
    final edit = _undo.removeLast();
    world = edit.$1;
    _redo.add(edit);
    return true;
  }

  bool redo() {
    if (!canRedo) return false;
    final edit = _redo.removeLast();
    world = edit.$2;
    _undo.add(edit);
    return true;
  }

  void replace(Landscape next) {
    world = next;
    _undo.clear();
    _redo.clear();
  }

  void importText(String text) => replace(Landscape.decode(text));
}
