import 'dart:math' as math;
import 'dart:ui';

import 'package:flame/components.dart';
import 'package:flame/game.dart';
import 'package:flutter/painting.dart';

import 'model.dart';
import 'residents.dart';

class SettlementGame extends FlameGame {
  final LandscapeSession session;
  final void Function(Placement?) onPreview;
  final void Function()? onReady;
  late final Residents residents = Residents(session.world);
  bool suspended = false;
  SettlementGame(this.session, this.onPreview, {this.onReady});
  Tool tool = Tool.cottage;
  Placement? preview;
  final Map<String, Sprite> art = {};
  final List<_Pulse> _pulses = [];
  bool canvasReady = false;
  static const tile = 12.0;

  @override
  Color backgroundColor() => const Color(0xff202a25);

  @override
  Future<void> onLoad() async {
    final names = [
      'grass0',
      'grass1',
      'dirt',
      'oak',
      'vegetables',
      'cottage0',
      'cottage1',
      'resident',
      for (var i = 0; i < 13; i++) 'road$i',
    ];
    for (final name in names) {
      art[name] = Sprite(await images.load('$name.png'));
    }
    await world.add(_LandscapeLayer(this));
    camera.viewfinder.zoom = 4;
    home();
    canvasReady = true;
    onReady?.call();
  }

  void home() {
    camera.viewfinder.position = Vector2(
      math.max(9 * tile, (canvasSize.x / 2 - 24) / 4),
      (session.world.height ~/ 2 + .5) * tile,
    );
    camera.viewfinder.zoom = 4;
    clearPreview();
  }

  Cell pick(Offset point) {
    final p = camera.globalToLocal(Vector2(point.dx, point.dy));
    return Cell((p.x / tile).floor(), (p.y / tile).floor());
  }

  void hover(Offset point) {
    if (!canvasReady) return;
    final c = pick(point);
    if (session.stroke?.changed.contains(c) == true) {
      clearPreview();
      return;
    }
    if (preview?.cell == c && identical(preview?.before, session.view)) return;
    preview = evaluate(session.view, tool, c);
    onPreview(preview);
  }

  void clearPreview() {
    preview = null;
    onPreview(null);
  }

  void pan(Offset delta) {
    camera.viewfinder.position -=
        Vector2(delta.dx, delta.dy) / camera.viewfinder.zoom;
    final p = camera.viewfinder.position;
    p.x = p.x.clamp(-30, session.world.width * tile + 30);
    p.y = p.y.clamp(-30, session.world.height * tile + 30);
    clearPreview();
  }

  void zoom(int direction, [Offset? anchor]) {
    final p = anchor ?? Offset(size.x / 2, size.y / 2);
    final before = camera.globalToLocal(Vector2(p.dx, p.dy));
    camera.viewfinder.zoom = (camera.viewfinder.zoom + direction).clamp(2, 6);
    final after = camera.globalToLocal(Vector2(p.dx, p.dy));
    camera.viewfinder.position += before - after;
    clearPreview();
  }

  void feedback(Cell c, bool valid, bool removed) {
    _pulses.add(_Pulse(c, valid, removed));
  }

  void reconcile({bool restored = false, bool reset = false}) =>
      residents.reconcile(session.world, restored: restored, reset: reset);
  _WorldNotice? _notice;
  void invalid(Cell c, String text) => _notice = _WorldNotice(c, text);

  @override
  void update(double dt) {
    super.update(dt);
    if (!suspended) residents.update(dt);
    if (_notice != null) {
      _notice!.age += dt.clamp(0.0, .05);
      if (_notice!.age > 1.8) _notice = null;
    }
    for (final p in _pulses) {
      p.age += dt;
    }
    _pulses.removeWhere((p) => p.age > .3);
  }
}

class _WorldNotice {
  final Cell cell;
  final String text;
  double age = 0;
  _WorldNotice(this.cell, this.text);
}

class _Pulse {
  final Cell cell;
  final bool valid, removed;
  double age = 0;
  _Pulse(this.cell, this.valid, this.removed);
}

class _LandscapeLayer extends Component {
  final SettlementGame game;
  _LandscapeLayer(this.game);
  Landscape? _cachedScene;
  Image? _sceneImage;
  List<Cell> _objects = [];
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
  static const green = Color(0xff658052), gold = Color(0xffd9c58d);

  void frame(
    Canvas canvas,
    String name,
    Cell c,
    Color tint, {
    double opacity = 1,
  }) {
    game.art[name]?.render(
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
    final color = Color.lerp(
      const Color(0xff7b7660),
      const Color(0xffbba47d),
      scene.activePaths.contains(c) ? 1 : 0,
    )!.withValues(alpha: opacity);
    var index = roadArt[mask];
    if (index == 0 || index == 2) {
      index = index! + variation(scene.seed, c, 5) % 2;
    }
    if (index != null) {
      frame(canvas, 'road$index', c, color);
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

  void drawLandscape(Canvas canvas, Landscape scene) {
    final bounds = Rect.fromLTWH(0, 0, scene.width * 12.0, scene.height * 12.0);
    canvas.drawRect(
      bounds.inflate(2),
      Paint()..color = const Color(0xff56634a),
    );
    canvas.drawRect(bounds, Paint()..color = const Color(0xff2e4032));
    for (final c in scene.cells) {
      final l = scene.at(c), x = c.x * 12.0, y = c.y * 12.0;
      frame(canvas, 'grass${l.grass}', c, const Color(0xff344a36));
      if (l.trace == Trace.pad) {
        canvas.drawRect(
          Rect.fromLTWH(x + 1, y + 3, 10, 9),
          Paint()..color = const Color(0xff625b40),
        );
        frame(canvas, 'dirt', c, const Color(0xff82734e));
      } else if (l.trace == Trace.scar) {
        canvas.drawRect(
          Rect.fromLTWH(x + 2, y + 5, 8, 2),
          Paint()..color = const Color(0xff6e7050),
        );
        canvas.drawRect(
          Rect.fromLTWH(x + 4, y + 3, 2, 6),
          Paint()..color = const Color(0xff575f42),
        );
      }
      if (l.scenery == Scenery.vegetables) {
        frame(canvas, 'vegetables', c, const Color(0xff9f9f5a));
      }
      if (l.path) road(canvas, scene, c);
    }
    for (final c in scene.cells) {
      final l = scene.at(c);
      if (l.cottage != null) {
        cottage(canvas, scene, c, l.cottage!, groundOnly: true);
      }
    }
    final arrival = scene.arrival;
    canvas.drawRect(
      Rect.fromLTWH(-4, arrival.y * 12 + 4, 2, 4),
      Paint()..color = gold,
    );
  }

  @override
  void render(Canvas canvas) {
    final stroke = game.session.stroke;
    // removals stay provisional: residents still use the committed roads.
    final scene = stroke?.tool == Tool.remove
        ? game.session.world
        : game.session.view;
    if (!identical(scene, _cachedScene)) {
      // rasterize unchanged land once at native resolution; camera motion and
      // feedback can then redraw without traversing every lot each frame.
      final recorder = PictureRecorder();
      final layer = Canvas(recorder)..translate(6, 6);
      drawLandscape(layer, scene);
      final picture = recorder.endRecording();
      _sceneImage?.dispose();
      _sceneImage = picture.toImageSync(
        scene.width * 12 + 12,
        scene.height * 12 + 12,
      );
      picture.dispose();
      _cachedScene = scene;
      _objects = scene.cells
          .where(
            (c) =>
                scene.at(c).cottage != null ||
                scene.at(c).scenery == Scenery.oak,
          )
          .toList();
    }
    canvas.drawImage(
      _sceneImage!,
      const Offset(-6, -6),
      Paint()
        ..filterQuality = FilterQuality.none
        ..isAntiAlias = false,
    );
    final people = game.residents.visible.toList()
      ..sort((a, b) => a.$1.y.compareTo(b.$1.y));
    var actor = 0;
    for (final c in _objects) {
      while (actor < people.length &&
          people[actor].$1.y * 12 + 2 <= (c.y + 1) * 12) {
        person(canvas, people[actor].$1, people[actor].$2);
        actor++;
      }
      final l = scene.at(c);
      if (l.cottage != null) {
        cottage(canvas, scene, c, l.cottage!, bodyOnly: true);
      } else {
        frame(canvas, 'oak', c, green);
      }
    }
    while (actor < people.length) {
      person(canvas, people[actor].$1, people[actor].$2);
      actor++;
    }
    if (stroke != null) {
      for (final c in stroke.changed) {
        final removed = stroke.tool == Tool.remove;
        final rect = Rect.fromLTWH(c.x * 12.0 + .5, c.y * 12.0 + .5, 11, 11);
        canvas.drawRect(
          rect,
          Paint()
            ..color = (removed ? const Color(0xffd39073) : gold).withValues(
              alpha: .13,
            ),
        );
        canvas.drawRect(
          rect,
          Paint()
            ..color = removed ? const Color(0xffd39073) : gold
            ..style = PaintingStyle.stroke
            ..strokeWidth = .4,
        );
        if (removed) {
          canvas.drawLine(
            rect.topLeft,
            rect.bottomRight,
            Paint()
              ..color = const Color(0xffd39073)
              ..strokeWidth = .5,
          );
        }
      }
    }
    final plan = game.preview;
    if (plan != null && scene.contains(plan.cell)) {
      final c = plan.cell, x = c.x * 12.0, y = c.y * 12.0;
      final color = plan.valid ? gold : const Color(0xffe1987b);
      if (plan.valid && stroke == null) {
        final next = plan.after!;
        for (final n in plan.affected) {
          if (n == c) continue;
          final lot = next.at(n);
          canvas.drawRect(
            Rect.fromLTWH(n.x * 12.0, n.y * 12.0, 12, 12),
            Paint()..color = const Color(0xff2e4032),
          );
          frame(canvas, 'grass${lot.grass}', n, const Color(0xff3c5239));
          if (lot.path) road(canvas, next, n);
          if (lot.cottage != null) cottage(canvas, next, n, lot.cottage!);
          canvas.drawRect(
            Rect.fromLTWH(n.x * 12.0 + 1, n.y * 12.0 + 1, 10, 10),
            Paint()
              ..color = gold.withValues(alpha: .55)
              ..style = PaintingStyle.stroke
              ..strokeWidth = .35,
          );
        }
        if (game.tool == Tool.path) road(canvas, next, c, opacity: .8);
        if (game.tool == Tool.cottage) {
          cottage(canvas, next, c, next.at(c).cottage!, opacity: .75);
        }
      }
      canvas.drawRect(
        Rect.fromLTWH(x + .5, y + .5, 11, 11),
        Paint()..color = color.withValues(alpha: .13),
      );
      canvas.drawRect(
        Rect.fromLTWH(x + .5, y + .5, 11, 11),
        Paint()
          ..color = color
          ..style = PaintingStyle.stroke
          ..strokeWidth = .5
          ..isAntiAlias = false,
      );
    }
    for (final p in game._pulses) {
      final a = (1 - p.age / .3).clamp(0.0, 1.0);
      final color = p.valid ? gold : const Color(0xffee9873);
      canvas.drawRect(
        Rect.fromLTWH(
          p.cell.x * 12.0 + 1,
          p.cell.y * 12.0 + 1,
          10,
          10,
        ).inflate(p.age * (p.removed ? 7 : 3)),
        Paint()
          ..color = color.withValues(alpha: a)
          ..style = PaintingStyle.stroke
          ..strokeWidth = .6,
      );
      if (p.valid) {
        for (var i = 0; i < 4; i++) {
          final angle = i * math.pi / 2 + .6;
          final r = p.age * 13;
          canvas.drawRect(
            Rect.fromLTWH(
              p.cell.x * 12 + 6 + math.cos(angle) * r,
              p.cell.y * 12 + 6 + math.sin(angle) * r,
              .8,
              .8,
            ),
            Paint()..color = color.withValues(alpha: a),
          );
        }
      }
    }
    final notice = game._notice;
    if (notice != null) {
      final text = TextPainter(
        text: TextSpan(
          text: notice.text,
          style: const TextStyle(
            fontFamily: 'SourceCode',
            fontSize: 3,
            color: Color(0xfff3d9a0),
          ),
        ),
        textDirection: TextDirection.ltr,
      )..layout(maxWidth: 110);
      final origin = Offset(notice.cell.x * 12.0, notice.cell.y * 12.0 - 7);
      canvas.drawRect(
        origin & Size(text.width + 2, text.height + 1),
        Paint()..color = const Color(0xee202a25),
      );
      text.paint(canvas, origin + const Offset(1, 0));
    }
  }

  void person(Canvas canvas, Resident r, double alpha) {
    final x = r.x * 12, y = r.y * 12;
    final offset = (r.homeId.codeUnits.fold(0, (a, b) => a + b) % 3 - 1) * .8;
    final bob = r.moving ? math.sin(r.age * 18).abs() * .3 : 0.0;
    final sprite = game.art['resident'];
    if (sprite == null) return;
    canvas.save();
    canvas.translate(x + offset, y - bob);
    canvas.scale(r.facing, 1);
    canvas.drawOval(
      const Rect.fromLTWH(-2.5, 1, 5, 1.5),
      Paint()..color = const Color(0xff17291f).withValues(alpha: alpha * .8),
    );
    for (final d in [
      const Offset(-.35, 0),
      const Offset(.35, 0),
      const Offset(0, -.35),
      const Offset(0, .35),
    ]) {
      sprite.render(
        canvas,
        position: Vector2(-3.5 + d.dx, -5 + d.dy),
        size: Vector2.all(7),
        overridePaint: Paint()
          ..filterQuality = FilterQuality.none
          ..colorFilter = ColorFilter.mode(
            const Color(0xff1b2b26).withValues(alpha: alpha),
            BlendMode.srcIn,
          ),
      );
    }
    sprite.render(
      canvas,
      position: Vector2(-3.5, -5),
      size: Vector2.all(7),
      overridePaint: Paint()
        ..filterQuality = FilterQuality.none
        ..colorFilter = ColorFilter.mode(
          (r.homeId.codeUnits.last.isEven
                  ? const Color(0xfff4e6ba)
                  : const Color(0xffa9d5d0))
              .withValues(alpha: alpha),
          BlendMode.srcIn,
        ),
    );
    canvas.restore();
    final home = game.session.world.cottages[r.homeId];
    if (r.flash > 0 && home != null) {
      canvas.drawRect(
        Rect.fromLTWH(
          home.x * 12.0 + .5,
          home.y * 12.0 + .5,
          11,
          11,
        ).inflate((1 - r.flash) * 2),
        Paint()
          ..color = gold.withValues(alpha: r.flash)
          ..style = PaintingStyle.stroke
          ..strokeWidth = .5,
      );
    }
  }

  @override
  void onRemove() {
    _sceneImage?.dispose();
    super.onRemove();
  }
}
