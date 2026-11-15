import 'dart:ui';

import 'package:flame/components.dart';

import 'model.dart';

// shared runtime composition: world, relocation ghosts and catalogue thumbnails.
class BuildArt {
  final Map<String, Sprite> art;
  BuildArt(this.art);
  static const roadArt = {
    5: 0,
    10: 2,
    12: 4,
    9: 5,
    6: 6,
    3: 7,
    13: 8,
    14: 9,
    7: 10,
    11: 11,
    15: 12,
  };
  void frame(
    Canvas canvas,
    String name,
    Cell c,
    Color tint, {
    double opacity = 1,
  }) {
    art[name]?.render(
      canvas,
      position: Vector2(c.x * 12.0, c.y * 12.0),
      size: Vector2.all(12),
      overridePaint: Paint()
        ..filterQuality = FilterQuality.none
        ..isAntiAlias = false
        ..colorFilter = ColorFilter.mode(
          tint.withValues(alpha: opacity),
          BlendMode.srcIn,
        ),
    );
  }

  void road(Canvas canvas, Landscape scene, Cell c, {double opacity = 1}) {
    final mask = scene.roadMask(c);
    final road = scene.at(c).road;
    final color = Color.lerp(
      const Color(0xff7b7660),
      road == RoadKind.stone
          ? const Color(0xffa3b2ad)
          : road == RoadKind.gardenPath
          ? const Color(0xff8f9f65)
          : const Color(0xffbba47d),
      scene.activePaths.contains(c) ? 1 : 0,
    )!.withValues(alpha: opacity);
    var index = roadArt[mask];
    if (index == 0 || index == 2) {
      index = index! + variation(scene.seed, c, 5) % 2;
    }
    if (index != null) {
      frame(canvas, 'road$index', c, color);
      if (road != RoadKind.dirt) {
        for (final d in [const Offset(4, 4), const Offset(7, 7)]) {
          canvas.drawRect(
            Rect.fromLTWH(
              c.x * 12 + d.dx,
              c.y * 12 + d.dy,
              road == RoadKind.stone ? 2 : 1,
              1,
            ),
            Paint()
              ..color =
                  (road == RoadKind.stone
                          ? const Color(0xff657c78)
                          : const Color(0xffe5d3a0))
                      .withValues(alpha: opacity),
          );
        }
      }
    } else {
      // The source set has no ends. Clip existing dirt to a real center + ports.
      final x = c.x * 12.0, y = c.y * 12.0;
      final path = Path()..addRect(Rect.fromLTWH(x + 3, y + 3, 6, 6));
      if (mask & 1 != 0) path.addRect(Rect.fromLTWH(x + 3, y, 6, 6));
      if (mask & 2 != 0) path.addRect(Rect.fromLTWH(x + 6, y + 3, 6, 6));
      if (mask & 4 != 0) path.addRect(Rect.fromLTWH(x + 3, y + 6, 6, 6));
      if (mask & 8 != 0) path.addRect(Rect.fromLTWH(x, y + 3, 6, 6));
      canvas.save();
      canvas.clipPath(path, doAntiAlias: false);
      canvas.drawRect(Rect.fromLTWH(x, y, 12, 12), Paint()..color = color);
      frame(canvas, 'dirt', c, const Color(0xff777058), opacity: opacity);
      canvas.restore();
    }
  }

  void cottage(
    Canvas canvas,
    Landscape scene,
    Cell c,
    int variant, {
    double opacity = 1,
    bool bodyOnly = false,
    bool groundOnly = false,
  }) {
    final x = c.x * 12.0, y = c.y * 12.0;
    final access = scene.access(c);
    if (!bodyOnly) {
      canvas.drawRect(
        Rect.fromLTWH(x + 1, y + 3, 10, 9),
        Paint()..color = const Color(0xff746345).withValues(alpha: opacity),
      );
      frame(canvas, 'dirt', c, const Color(0xff8b7954), opacity: opacity);
      if (access != null) {
        final end = Offset((access.x + .5) * 12, (access.y + .5) * 12);
        canvas.drawLine(
          Offset(x + 6, y + 8),
          end,
          Paint()
            ..color = const Color(0xffa7946e).withValues(alpha: opacity)
            ..strokeWidth = 2
            ..isAntiAlias = false,
        );
      }
      canvas.drawRect(
        Rect.fromLTWH(x + 3, y + 10, 8, 2),
        Paint()..color = const Color(0xff1d2924).withValues(alpha: opacity),
      );
    }
    if (groundOnly) return;
    frame(
      canvas,
      'cottage$variant',
      c,
      const Color(0xffe29a60),
      opacity: opacity,
    );
    if (access == null && opacity == 1) {
      canvas.drawRect(
        Rect.fromLTWH(x + 8, y + 1, 4, 3),
        Paint()..color = const Color(0xffe2c477),
      );
      canvas.drawRect(
        Rect.fromLTWH(x + 9, y + 2, 2, 1),
        Paint()..color = const Color(0xff403d30),
      );
    }
  }

  void place(
    Canvas canvas,
    Landscape scene,
    Cell c, {
    double opacity = 1,
    double? harvestAge,
    bool gatheringActive = false,
  }) {
    final lot = scene.at(c), x = c.x * 12.0, y = c.y * 12.0;
    canvas.save();
    canvas.drawRect(
      Rect.fromLTWH(x + .5, y + .5, 11, 11),
      Paint()
        ..color =
            (lot.place == Place.garden
                    ? const Color(0xff6c6144)
                    : const Color(0xff456347))
                .withValues(alpha: opacity),
    );
    if ([Place.workshop, Place.market, Place.hall].contains(lot.place)) {
      frame(
        canvas,
        lot.place!.name,
        c,
        lot.place == Place.workshop
            ? const Color(0xffb5beb6)
            : lot.place == Place.market
            ? const Color(0xffdbab78)
            : const Color(0xffc4b0ca),
        opacity: opacity,
      );
      if (lot.place == Place.market) {
        for (var i = 0; i < 4; i++) {
          canvas.drawRect(
            Rect.fromLTWH(x + 1 + i * 2, y + 3, 2, 2),
            Paint()
              ..color =
                  (i.isEven ? const Color(0xffbf7e65) : const Color(0xffe6cf94))
                      .withValues(alpha: opacity),
          );
        }
      }
    } else if (lot.place == Place.garden) {
      for (final row in [3.0, 7.0]) {
        canvas.drawRect(
          Rect.fromLTWH(x + 1, y + row, 10, 2),
          Paint()..color = const Color(0xff493e2e).withValues(alpha: opacity),
        );
      }
      frame(canvas, 'vegetables', c, const Color(0xffb8c578), opacity: opacity);
      final age = harvestAge;
      if (age != null && age < 1) {
        canvas.drawCircle(
          Offset(x + 6, y + 5),
          2 + age * 4,
          Paint()
            ..style = PaintingStyle.stroke
            ..strokeWidth = .5
            ..color = const Color(0xffe4ce88).withValues(alpha: opacity),
        );
      }
    } else {
      final sprite = art['oak'];
      sprite?.render(
        canvas,
        position: Vector2(x + 1, y + 1),
        size: Vector2.all(7),
        overridePaint: Paint()
          ..filterQuality = FilterQuality.none
          ..colorFilter = ColorFilter.mode(
            const Color(0xffa4b878).withValues(alpha: opacity),
            BlendMode.srcIn,
          ),
      );
      for (final by in [3.0, 8.0]) {
        canvas.drawRect(
          Rect.fromLTWH(x + 8, y + by, 3, 1),
          Paint()..color = const Color(0xffceb88a).withValues(alpha: opacity),
        );
      }
      if (gatheringActive) {
        canvas.drawRect(
          Rect.fromLTWH(x, y, 12, 12).inflate(.5),
          Paint()
            ..style = PaintingStyle.stroke
            ..strokeWidth = .5
            ..color = const Color(0xffeed088),
        );
      }
    }
    if (scene.access(c) == null && opacity == 1) {
      canvas.drawRect(
        Rect.fromLTWH(x + 9, y, 3, 2),
        Paint()..color = const Color(0xffe2c477),
      );
    }
    canvas.restore();
  }

  static String frameFor(Tool tool) => switch (tool) {
    Tool.cottage => 'cottage0',
    Tool.garden => 'vegetables',
    Tool.green => 'oak',
    Tool.workshop => 'workshop',
    Tool.market => 'market',
    Tool.hall => 'hall',
    Tool.remove => 'clear',
    _ => 'road2',
  };
  static final Map<Tool, Landscape> samples = {
    for (final tool in Tool.values)
      tool: Landscape(202, 3, 4, [
        for (var y = 0; y < 4; y++)
          for (var x = 0; x < 3; x++)
            y == 2
                ? Lot(
                    path: true,
                    road: tool.isRoad ? tool.roadKind : RoadKind.dirt,
                  )
                : x == 1 && y == 1 && !tool.isRoad && tool != Tool.remove
                ? Lot(
                    trace: Trace.pad,
                    cottage: tool == Tool.cottage ? 0 : null,
                    cottageId: tool == Tool.cottage ? 'home-1' : null,
                    place: tool.place,
                    placeId: tool.place == null
                        ? null
                        : '${tool.place!.name}-1',
                  )
                : const Lot(),
      ], nextCottageId: 2),
  };
  void thumbnail(Canvas canvas, Tool tool) {
    final c = tool.isRoad ? const Cell(1, 2) : const Cell(1, 1),
        scene = samples[tool]!;
    canvas.save();
    canvas.translate(-c.x * 12.0, -c.y * 12.0);
    if (tool.isRoad) {
      road(canvas, scene, c);
    } else if (tool == Tool.cottage) {
      cottage(canvas, scene, c, 0);
    } else if (tool == Tool.remove) {
      frame(canvas, 'clear', c, const Color(0xff9fad97));
    } else {
      place(canvas, scene, c);
    }
    canvas.restore();
  }
}
