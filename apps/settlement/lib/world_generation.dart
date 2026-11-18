import 'model.dart';

// all arithmetic stays below JavaScript's exact-integer limit. No render/event RNG.
class _Stream {
  int state;
  _Stream(int seed, int domain) : state = 1 + (seed + domain) % 2147483646;
  int next(int limit) {
    state = state * 48271 % 2147483647;
    return state % limit;
  }
}

abstract final class WorldGeneration {
  static Landscape create(String input, EnvironmentPreset preset) {
    if (preset.index >= EnvironmentPreset.wetlands.index) {
      return _challenging(input, preset);
    }
    return _base(input, preset);
  }

  static Landscape _base(String input, EnvironmentPreset preset) {
    final text = canonicalSeed(input), seed = seedNumber(text);
    final scenery = _Stream(seed, 104729), environment = _Stream(seed, 130363);
    final geometry = _Stream(seed, 32452843);
    const width = 40, height = 30;
    // independent coarse grids avoid the translated/repeating patterns of a
    // linear coordinate hash; integer interpolation keeps VM/web agreement.
    List<int> grid(int domain) {
      final random = _Stream(seed, domain);
      return List.generate(9 * 7, (_) => random.next(100));
    }

    final forests = grid(3001), wetlands = grid(7001), rocks = grid(11003);
    int field(int x, int y, List<int> values) {
      const scale = 5;
      final gx = x ~/ scale, gy = y ~/ scale, fx = x % scale, fy = y % scale;
      int sample(int a, int b) => values[b * 9 + a];
      return (sample(gx, gy) * (scale - fx) * (scale - fy) +
              sample(gx + 1, gy) * fx * (scale - fy) +
              sample(gx, gy + 1) * (scale - fx) * fy +
              sample(gx + 1, gy + 1) * fx * fy) ~/
          (scale * scale);
    }

    final river = <int>[], widths = <int>[];
    var center = 19 + geometry.next(3), channel = 2 + geometry.next(2);
    for (var y = 0; y < height; y++) {
      if (y % 4 == 0) {
        center = (center + geometry.next(3) - 1).clamp(17, 23);
        channel = 2 + geometry.next(2);
      }
      river.add(center);
      widths.add(channel);
    }
    final lots = <Lot>[], forestCandidates = <Cell>[];
    for (var y = 0; y < height; y++) {
      for (var x = 0; x < width; x++) {
        var terrain = Terrain.ground;
        if (field(x, y, wetlands) < preset.wetThreshold) {
          terrain = Terrain.marsh;
        }
        if (field(x, y, rocks) < preset.rockThreshold) terrain = Terrain.rock;
        if (preset.hasRiver) {
          final left = river[y], right = left + widths[y] - 1;
          if (x >= left && x <= right) {
            terrain = Terrain.water;
          } else if (x >= left - 1 && x <= right + 1) {
            terrain = Terrain.marsh;
          }
          // a clear, affordable starter approach and useful far-bank bridgehead.
          if (y >= 14 &&
              y <= 16 &&
              terrain != Terrain.water &&
              x <= right + 3) {
            terrain = Terrain.ground;
          }
        }
        if (x <= 10 && y >= 11 && y <= 19) terrain = Terrain.ground;
        final starter = x <= 10 && y >= 11 && y <= 19;
        final approach =
            preset.hasRiver &&
            y >= 14 &&
            y <= 16 &&
            x <= river[y] + widths[y] + 2;
        if (terrain == Terrain.ground && !starter && !approach) {
          forestCandidates.add(Cell(x, y));
        }
        final path = x <= 7 && y == 15;
        final decoration = scenery.next(37);
        lots.add(
          Lot(
            grass: (variation(seed, Cell(x, y), 1) ~/ 37) % 2,
            terrain: terrain,
            path: path,
            scenery: terrain == Terrain.forest
                ? Scenery.oak
                : terrain == Terrain.ground && !path && decoration == 0
                ? Scenery.vegetables
                : Scenery.none,
          ),
        );
      }
    }
    forestCandidates.sort((a, b) {
      final order = field(
        a.x,
        a.y,
        forests,
      ).compareTo(field(b.x, b.y, forests));
      return order != 0
          ? order
          : variation(seed, a, 261).compareTo(variation(seed, b, 261));
    });
    // preset coverage bounds keep beginner maps open and woodland maps forested.
    for (final c in forestCandidates.take(
      forestCandidates.length * preset.forestPercent ~/ 100,
    )) {
      final index = c.y * width + c.x;
      lots[index] = Lot(
        grass: lots[index].grass,
        terrain: Terrain.forest,
        scenery: Scenery.oak,
      );
    }
    return Landscape(
      seed,
      width,
      height,
      lots,
      generation: GenerationInfo(text, preset, environment.next(4), version: 2),
    );
  }

  static Landscape _challenging(String input, EnvironmentPreset preset) {
    // retain v2 fields and stream domains. Additional channels/ridges use their
    // own stream; bounded corridors are generation rules, never live repairs.
    final base = _base(input, preset);
    final geometry = _Stream(base.seed, 49979687);
    final lots = base.lots.toList();
    final ridge = 15 + geometry.next(3), pass = 5 + geometry.next(5);
    final tributary = 5 + geometry.next(3);
    for (final c in base.cells) {
      final old = base.at(c);
      var terrain = old.terrain == Terrain.forest
          ? Terrain.ground
          : old.terrain;
      if (preset == EnvironmentPreset.wetlands) {
        if ((c.y - tributary).abs() <= 1 && c.x >= 12 ||
            (c.y - 24).abs() <= 1 && c.x >= 12) {
          terrain = Terrain.water;
        } else if (terrain == Terrain.ground &&
            variation(base.seed, Cell(c.x ~/ 3, c.y ~/ 3), 881) % 100 <
                preset.wetThreshold) {
          terrain = Terrain.marsh;
        }
      } else {
        // coherent two-cell ridges with two true traversable passes.
        if ((c.x == ridge || c.x == ridge + 1) &&
            (c.y - pass).abs() > 1 &&
            (c.y - 15).abs() > 1) {
          terrain = Terrain.rock;
        }
        if (preset == EnvironmentPreset.frontier &&
            c.x > 27 &&
            c.y % 9 <= 1 &&
            c.y != 15) {
          terrain = Terrain.rock;
        }
      }
      // Preserve the known affordable opening, plus a second dry corridor. All
      // six presets retain the original 11x9 clearing and eight arrival roads.
      if (c.x <= 10 && c.y >= 11 && c.y <= 19 ||
          (c.y - 15).abs() <= 1 && old.terrain != Terrain.water) {
        terrain = Terrain.ground;
      }
      lots[c.y * base.width + c.x] = Lot(
        grass: old.grass,
        terrain: terrain,
        path: old.path,
        scenery: terrain == Terrain.forest ? Scenery.oak : Scenery.none,
      );
    }
    // Validate usable land before vegetation. Extreme fields receive one bounded
    // dry corridor expansion, nearest the protected lane first. No live repair.
    var open = lots.where((l) => l.terrain == Terrain.ground).length;
    final recovery =
        base.cells
            .where(
              (c) =>
                  lots[c.y * base.width + c.x].terrain != Terrain.water &&
                  !(c.x >= ridge &&
                      c.x <= ridge + 1 &&
                      (c.y - pass).abs() > 1 &&
                      (c.y - 15).abs() > 1),
            )
            .toList()
          ..sort((a, b) {
            final d = (a.y - 15).abs().compareTo((b.y - 15).abs());
            return d != 0
                ? d
                : (a.y * base.width + a.x).compareTo(b.y * base.width + b.x);
          });
    for (final c in recovery) {
      if (open >= 450) break;
      final i = c.y * base.width + c.x;
      if (lots[i].terrain != Terrain.ground) {
        lots[i] = Lot(grass: lots[i].grass, terrain: Terrain.ground);
        open++;
      }
    }
    if (open < 450) throw StateError('Invalid constrained starting landscape.');
    final eligible = base.cells
        .where(
          (c) =>
              lots[c.y * base.width + c.x].terrain == Terrain.ground &&
              !(c.x <= 10 && c.y >= 11 && c.y <= 19) &&
              (c.y - 15).abs() > 1,
        )
        .toList();
    eligible.sort((a, b) {
      final av = variation(base.seed, Cell(a.x ~/ 3, a.y ~/ 3), 983),
          bv = variation(base.seed, Cell(b.x ~/ 3, b.y ~/ 3), 983);
      final order = av.compareTo(bv);
      return order != 0
          ? order
          : (a.y * base.width + a.x).compareTo(b.y * base.width + b.x);
    });
    final forestCount = eligible.length * preset.forestPercent ~/ 100;
    for (final c in eligible.take(forestCount < 72 ? 72 : forestCount)) {
      lots[c.y * base.width + c.x] = Lot(
        grass: base.at(c).grass,
        terrain: Terrain.forest,
        scenery: Scenery.oak,
      );
    }
    return Landscape(
      base.seed,
      base.width,
      base.height,
      lots,
      generation: GenerationInfo(
        canonicalSeed(input),
        preset,
        base.generation!.environmentVariant,
      ),
    );
  }
}
