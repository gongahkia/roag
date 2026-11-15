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
      generation: GenerationInfo(text, preset, environment.next(4)),
    );
  }
}
