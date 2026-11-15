import 'dart:math' as math;
import 'dart:ui';

import 'package:flame/components.dart';
import 'package:flame/game.dart';
import 'package:flutter/painting.dart';

import 'model.dart';
import 'residents.dart';
import 'colony.dart';
import 'rules.dart';
import 'reasons.dart';
import 'speech.dart';
import 'appearance.dart';
import 'resident_presentation.dart';
import 'special_event.dart';
import 'build_art.dart';

class SettlementGame extends FlameGame {
  Colony colony;
  LandscapeSession get session => colony.session;
  final void Function(Placement?) onPreview;
  final void Function()? onReady;
  Colony get residents => colony;
  final void Function()? onState, onCheckpoint;
  SpeechDirector speech = SpeechDirector();
  bool inspectMode = false, reducedMotion = false, menuVisible = false;
  bool dialogueOpen = false;
  Cell? movingFrom;
  ResidentPresentation presentation = ResidentPresentation();
  String? inspectedResident;
  Cell? inspectedCell;
  double _stateAge = 0, _saveAge = 0, _lastCheckpointClock = 0;
  bool suspended = false;
  final bool profile;
  final List<int> modelMicros = [], presentationMicros = [];
  int modelCalls = 0;
  Map<String, Object> get metrics => {
    'modelMicros': modelMicros,
    'presentationMicros': presentationMicros,
    'modelCalls': modelCalls,
    'routeSearches': colony.routeSearches,
    'population': colony.people.length,
    'clock': colony.clock,
    'cacheHits': colony.navigation.hits,
    'expansions': colony.navigation.expansions,
    'fieldsBuilt': colony.navigation.fieldsBuilt,
    'planningBacklog': colony.planning.length,
    'planned': colony.planned,
    'discardedPlans': colony.discardedPlans,
    'navigationRevision': colony.navigation.revision,
  };
  SettlementGame(
    this.colony,
    this.onPreview, {
    this.onReady,
    this.onState,
    this.onCheckpoint,
    this.profile = false,
  });
  Tool tool = Tool.cottage;
  Placement? preview;
  Offset? _hoverPoint;
  int? _previewBudget;
  final Map<String, Sprite> art = {};
  Image? residentAtlas;
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
      'workshop',
      'market',
      'hall',
      'cottage0',
      'cottage1',
      'resident',
      for (var i = 0; i < 13; i++) 'road$i',
    ];
    for (final name in names) {
      art[name] = Sprite(await images.load('$name.png'));
    }
    residentAtlas = await images.load('resident-atlas.png');
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
    if (inspectMode) return;
    _hoverPoint = point;
    final c = pick(point);
    if (session.stroke?.changed.contains(c) == true) {
      clearPreview();
      return;
    }
    final budget = session.stroke == null
        ? session.money
        : session.stroke!.budget - session.stroke!.price;
    if (preview?.cell == c &&
        identical(preview?.before, session.view) &&
        _previewBudget == budget) {
      return;
    }
    _previewBudget = budget;
    preview = movingFrom == null ? evaluate(session.view, tool, c)
        : evaluateMove(session.world, movingFrom!, c);
    final free = session.stroke?.free ?? session.unlimited;
    final cost = free || movingFrom != null ? 0 : Rules.cost(tool.name);
    if (preview!.valid && cost > budget) {
      preview = Placement(
        session.view,
        null,
        c,
        session.stroke != null
            ? 'Stroke budget used · release before using new income.'
            : 'Not enough coins · productive work, served Market visits and the daily grant earn more.',
      );
    }
    onPreview(preview);
  }

  void clearPreview() {
    preview = null;
    _hoverPoint = null;
    _previewBudget = null;
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
    if (!suspended && canvasReady) {
      final watch = profile ? (Stopwatch()..start()) : null;
      residents.update(dt);
      if (watch != null) {
        modelCalls++;
        if (modelMicros.length >= 2048) modelMicros.removeAt(0);
        modelMicros.add(watch.elapsedMicroseconds);
        watch.reset();
      }
      _stateAge += dt.clamp(0.0, .1);
      _saveAge += dt.clamp(0.0, .1);
      presentation.update(dt, residents.people.values);
      updateSpeech(dt);
      if (watch != null) {
        if (presentationMicros.length >= 2048) presentationMicros.removeAt(0);
        presentationMicros.add(watch.elapsedMicroseconds);
        watch.stop();
      }
      if (_stateAge >= .25) {
        _stateAge = 0;
        if (_hoverPoint != null && session.stroke == null && !inspectMode) {
          hover(_hoverPoint!);
        }
        onState?.call();
      }
      if (_saveAge >= 3 && colony.clock != _lastCheckpointClock) {
        _saveAge = 0;
        _lastCheckpointClock = colony.clock;
        onCheckpoint?.call();
      }
    }
    if (_notice != null) {
      _notice!.age += dt.clamp(0.0, .05);
      if (_notice!.age > 1.8) _notice = null;
    }
    for (final p in _pulses) {
      p.age += dt;
    }
    _pulses.removeWhere((p) => p.age > .3);
  }

  void resetPresentation() {
    speech = SpeechDirector();
    presentation = ResidentPresentation();
    inspectMode = false;
    dialogueOpen = false;
    movingFrom = null;
    _pulses.clear();
    _notice = null;
    clearInspection();
    clearPreview();
    _lastCheckpointClock = colony.clock;
    _saveAge = 0;
  }

  ResidentPose pose(Resident r) => presentation.pose(
    r,
    reducedMotion: reducedMotion,
    paused: colony.speed == 0 || suspended,
  );
  Offset actorPosition(Resident r) => pose(r).feet;

  Reason speechReason(Resident r) =>
      r.feedback == Reason.noWork &&
          colony.workplaces.isNotEmpty &&
          colony.workplaces.every((p) => colony.world.access(p.value) == null)
      ? Reason.farmsCut
      : r.feedback;

  Offset? _inspectionPoint;
  int nearbyPeople = 0;
  bool inspectedEvent = false;
  bool get movableSelection => inspectedResident == null && inspectedCell != null &&
      session.world.contains(inspectedCell!) && buildingTool(session.world.at(inspectedCell!)) != null;
  void inspectBuilding(Cell c) {
    clearInspection(); inspectedCell = c; inspectMode = true; onState?.call();
  }
  void locate(Cell c) {
    camera.viewfinder.position = Vector2((c.x + .5) * 12, (c.y + .5) * 12);
    clearPreview();
  }
  void inspect(Offset point) {
    final hits = <(String, double)>[];
    for (final r in residents.people.values.where((r) => r.visible)) {
      final p = actorPosition(r),
          screen = camera.localToGlobal(Vector2(p.dx, p.dy - 4));
      final distance = (Offset(screen.x, screen.y) - point).distance;
      if (distance < 24) hits.add((r.homeId, distance));
    }
    hits.sort(
      (a, b) => a.$2 != b.$2 ? a.$2.compareTo(b.$2) : a.$1.compareTo(b.$1),
    );
    final ids = hits.map((h) => h.$1).toList();
    final cycle =
        _inspectionPoint != null &&
        (_inspectionPoint! - point).distance < 8 &&
        ids.contains(inspectedResident);
    inspectedResident = ids.isEmpty
        ? null
        : ids[cycle ? (ids.indexOf(inspectedResident!) + 1) % ids.length : 0];
    nearbyPeople = ids.length;
    _inspectionPoint = point;
    inspectedEvent = false;
    inspectedCell = pick(point);
    onState?.call();
  }

  void clearInspection() {
    inspectedResident = null;
    inspectedCell = null;
    _inspectionPoint = null;
    nearbyPeople = 0;
    inspectedEvent = false;
  }

  String? get inspection {
    if (inspectedEvent && colony.special != null) {
      final e = colony.special!;
      return '${e.title}\n${e.effect}\n${e.civicChoice == null ? '' : 'Town choice: ${e.civicChoice}. ${e.civicAwarded.length} hospitality benefits.\n'}${e.active(colony.clock) ? 'Ends in ${(e.ends - colony.clock).ceil()} seconds.' : 'This event has ended.'}';
    }
    final r = colony.people[inspectedResident];
    if (r != null) {
      final reason = speechReason(r);
      final detail = reason == Reason.none
          ? (r.route.isNotEmpty
                ? 'Following a valid path. Travel takes time.'
                : 'Following the daily routine automatically.')
          : reason.detail;
      final mood = colony
          .contributions(r)
          .entries
          .where((e) => e.value != 0)
          .map((e) => '${e.key} ${e.value > 0 ? '+' : ''}${e.value}')
          .join(' · ');
      final home = colony.world.cottages[r.homeId],
          work = colony.world.places[r.workId];
      final requests = colony.requests.forResident(r.homeId).map((q) => q.status).join('\n');
      return '${r.name} · ${Appearance.description(r.bodyId, r.palette)}\n${r.trait.label}: ${r.trait.effect}\n${activityLabel(r.activity)}\nHome: Cottage ${home == null ? 'removed' : 'at ${home.x + 1}, ${home.y + 1}'}. Work: ${work == null ? 'seeking work' : '${colony.kind(r.workId)!.label} at ${work.x + 1}, ${work.y + 1}'}.\n$detail\nHappiness ${colony.happiness(r)} · base 50${mood.isEmpty ? '' : '\n$mood'}${requests.isEmpty ? '' : '\n$requests'}${nearbyPeople > 1 ? '\nClick again to cycle $nearbyPeople nearby people.' : ''}';
    }
    final c = inspectedCell;
    if (c == null || !session.world.contains(c)) return null;
    final lot = session.world.at(c), access = session.world.access(c);
    final place = lot.place;
    if (place != null) {
      final id = lot.placeId!;
      final work = place.workCapacity > 0
          ? '${place.workCapacity} work positions\n${colony.workers(id)} working · ${colony.jobs(id)} assigned. Income: ${Rules.coins(place.income)} coin/second per actual worker.${place == Place.garden && colony.special?.kind == SpecialKind.harvest && colony.special!.active(colony.clock) ? ' Good harvest: ${Rules.exactCoins((place.income * 1.25).round())}/second.' : ''}\n'
          : '';
      final visits = place.visitorCapacity > 0
          ? '${place.visitorCapacity} visitor spaces · ${colony.occupancy(id)} visiting. ${Rules.socialDuration.toInt()} seconds of actual leisure improves mood.\n'
          : '';
      final trade = place == Place.market
          ? '${colony.staffed(id) ? 'Vendor present' : 'Waiting for a working vendor'}. Completed service earns ${Rules.exactCoins(Rules.tradeCredit)} coin once per resident/day across all Markets${colony.marketDay ? ' + ${Rules.exactCoins(Rules.marketDayBonus)} Market-day bonus' : ''}. Vendor work pays separately.\n'
          : '';
      return '${place.label} · $work$visits$trade${place == Place.hall ? 'Covered; preferred in rain.\n' : ''}${place.workCapacity > 0 ? '${colony.people.values.where((r) => r.workIssue != Reason.none).length} residents seeking reachable work.\n' : ''}${access == null ? 'Restore a path to the arrival lane.' : 'Residents follow their routines automatically.'}';
    }
    if (lot.cottage != null) {
      return 'Cottage · one resident\n${access == null ? 'Disconnected. Restore a path to the arrival lane.' : 'Connected. Inspect its resident for work and leisure needs.'}';
    }
    return lot.path
        ? '${lot.road == RoadKind.dirt
              ? 'Dirt path'
              : lot.road == RoadKind.stone
              ? 'Stone Road'
              : 'Garden Path'} · ${session.world.activePaths.contains(c) ? 'connected' : 'disconnected'}\n${lot.road == RoadKind.stone
              ? '18% quicker travel; unaffected by rain.'
              : lot.road == RoadKind.gardenPath
              ? 'Normal speed; +3 mood after actual traversal, once per day. Rain slows travel.'
              : 'Free baseline path; rain slows travel.'}'
        : 'Landscape · choose a building beside a connected path.';
  }

  String activityLabel(Activity activity) => switch (activity) {
    Activity.pending => 'Moving in soon',
    Activity.arriving => 'Arriving',
    Activity.workTravel => 'Walking to work',
    Activity.working => 'Working',
    Activity.socialTravel => 'Walking to a social visit',
    Activity.socialising => 'Socialising',
    Activity.socialWaiting => 'Waiting for a visitor space or service',
    Activity.gatheringTravel => 'Joining the gathering',
    Activity.gathering => 'At the gathering',
    Activity.resting => 'Resting',
    Activity.returning => 'Returning home',
    Activity.waiting => 'Waiting for a path',
    Activity.settling => 'Stepping off a removed path',
    _ => 'At home',
  };
  final Map<Reason, TextPainter> _bubbleText = {};
  TextPainter bubbleText(Reason reason) => _bubbleText.putIfAbsent(
    reason,
    () => TextPainter(
      text: TextSpan(
        text: reason.bubble,
        style: const TextStyle(
          fontFamily: 'SourceCode',
          fontSize: 11,
          height: 1.2,
          color: Color(0xff26392c),
        ),
      ),
      textDirection: TextDirection.ltr,
    )..layout(maxWidth: 178),
  );

  void updateSpeech(double dt) {
    final candidates = <BubbleCandidate>[];
    if (!menuVisible) {
      for (final r in residents.people.values.where((r) => r.visible)) {
        final reason = speechReason(r);
        if (reason == Reason.none) continue;
        final position = actorPosition(r),
            p = camera.localToGlobal(Vector2(position.dx, position.dy - 8));
        if (p.x < 0 || p.y < 0 || p.x > size.x || p.y > size.y) continue;
        final text = bubbleText(reason),
            w = text.width.ceilToDouble() + 14,
            h = text.height.ceilToDouble() + 12;
        final x = (p.x - w / 2)
            .clamp(8.0, math.max(8.0, size.x - w - 8))
            .toDouble();
        final options =
            [
                  Rect.fromLTWH(x, p.y - h - 10, w, h),
                  Rect.fromLTWH(p.x + 14, p.y - h / 2, w, h),
                  Rect.fromLTWH(p.x - w - 14, p.y - h / 2, w, h),
                  Rect.fromLTWH(x, p.y + 40, w, h),
                ]
                .where(
                  (b) =>
                      b.left >= 8 &&
                      b.top >= 8 &&
                      b.right <= size.x - 8 &&
                      b.bottom <= size.y - 8,
                )
                .toList();
        candidates.add(BubbleCandidate(r, options, displayReason: reason));
      }
    }
    speech.update(
      dt,
      candidates,
      avoid: [
        const Rect.fromLTWH(16, 12, 700, 80),
        // matches the existing top-right read-only inspector, with safe padding.
        if (inspection != null || dialogueOpen) Rect.fromLTWH(size.x - 332, 0, 332, size.y),
      ],
    );
  }

  @override
  void render(Canvas canvas) {
    super.render(canvas);
    if (!canvasReady) return;
    if (colony.phase == 'Night') {
      canvas.drawRect(
        Offset.zero & Size(size.x, size.y),
        Paint()..color = const Color(0x15131c30),
      );
    }
    if (colony.raining && !reducedMotion && size.x > 0 && size.y > 0) {
      final paint = Paint()
        ..color = const Color(0x456b9caf)
        ..strokeWidth = 1;
      for (var i = 0; i < 45; i++) {
        final x = (i * 131.0 + presentation.time * 25) % size.x;
        final y = (i * 79.0 + presentation.time * 180) % size.y;
        canvas.drawLine(Offset(x, y), Offset(x - 3, y + 7), paint);
      }
    }
    final e = colony.event;
    if (e != null &&
        e.state != 'ended' &&
        colony.world.places.containsKey(e.green)) {
      final c = colony.world.places[e.green]!,
          p = camera.localToGlobal(Vector2((c.x + .5) * 12, c.y * 12));
      final label = TextPainter(
        text: TextSpan(
          text: e.state == 'active'
              ? 'Evening gathering'
              : 'Gathering · neighbours arriving',
          style: const TextStyle(
            fontFamily: 'SourceCode',
            fontSize: 11,
            color: Color(0xfff1d78e),
            backgroundColor: Color(0xdd202a25),
          ),
        ),
        textDirection: TextDirection.ltr,
      )..layout();
      label.paint(canvas, Offset(p.x - label.width / 2, p.y - 18));
    }
    for (final bubble in speech.visible) {
      final r = residents.people[bubble.residentId];
      if (r == null) continue;
      final actor = actorPosition(r),
          point = camera.localToGlobal(Vector2(actor.dx, actor.dy - 8));
      canvas.drawLine(
        Offset(
          point.x.clamp(bubble.bounds.left + 5, bubble.bounds.right - 5),
          point.y.clamp(bubble.bounds.top + 5, bubble.bounds.bottom - 5),
        ),
        Offset(point.x, point.y),
        Paint()
          ..color = const Color(0xffe2ddbb)
          ..strokeWidth = 1.5,
      );
      canvas.drawRRect(
        RRect.fromRectAndRadius(bubble.bounds, const Radius.circular(5)),
        Paint()..color = const Color(0xf2ece3c4),
      );
      final text = bubbleText(bubble.reason);
      text.paint(canvas, bubble.bounds.topLeft + const Offset(7, 6));
    }
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
  static const green = Color(0xff658052), gold = Color(0xffd9c58d);

  late final drawings = BuildArt(game.art);
  void frame(
    Canvas canvas,
    String name,
    Cell c,
    Color tint, {
    double opacity = 1,
  }) => drawings.frame(canvas, name, c, tint, opacity: opacity);
  void road(Canvas canvas, Landscape scene, Cell c, {double opacity = 1}) =>
      drawings.road(canvas, scene, c, opacity: opacity);
  void cottage(
    Canvas canvas,
    Landscape scene,
    Cell c,
    int variant, {
    double opacity = 1,
    bool bodyOnly = false,
    bool groundOnly = false,
  }) => drawings.cottage(
    canvas,
    scene,
    c,
    variant,
    opacity: opacity,
    bodyOnly: bodyOnly,
    groundOnly: groundOnly,
  );
  void place(Canvas canvas, Landscape scene, Cell c, {double opacity = 1}) {
    final tended = game.colony.tended[scene.at(c).placeId];
    drawings.place(
      canvas,
      scene,
      c,
      opacity: opacity,
      harvestAge: tended == null ? null : game.colony.clock - tended,
      gatheringActive:
          game.colony.event?.green == scene.at(c).placeId &&
          game.colony.event?.state == 'active',
    );
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
                scene.at(c).place != null ||
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
    final visible = game.camera.visibleWorldRect.inflate(14);
    final people =
        game.residents.visible
            .where(
              (entry) =>
                  visible.contains(Offset(entry.$1.x * 12, entry.$1.y * 12)),
            )
            .toList()
          ..sort(
            (a, b) => game
                .actorPosition(a.$1)
                .dy
                .compareTo(game.actorPosition(b.$1).dy),
          );
    var actor = 0;
    final band = <(Resident, double)>[];
    for (final c in _objects) {
      if (!visible.overlaps(Rect.fromLTWH(c.x * 12.0, c.y * 12.0, 12, 12))) {
        continue;
      }
      while (actor < people.length &&
          game.actorPosition(people[actor].$1).dy <= (c.y + 1) * 12) {
        band.add(people[actor++]);
      }
      peopleBand(canvas, band);
      band.clear();
      final l = scene.at(c);
      if (l.cottage != null) {
        cottage(canvas, scene, c, l.cottage!, bodyOnly: true);
      } else if (l.place != null) {
        place(canvas, scene, c);
      } else {
        frame(canvas, 'oak', c, green);
      }
    }
    band.addAll(people.skip(actor));
    peopleBand(canvas, band);
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
        if (next.at(c).path) road(canvas, next, c, opacity: .8);
        if (next.at(c).place != null) {
          place(canvas, next, c, opacity: .75);
        }
        if (next.at(c).cottage != null) {
          cottage(canvas, next, c, next.at(c).cottage!, opacity: .75);
        }
      }
      if (plan.movingFrom != null) {
        final from = plan.movingFrom!;
        canvas.drawRect(Rect.fromLTWH(from.x * 12.0, from.y * 12.0, 12, 12),
          Paint()..color = gold.withValues(alpha: .2));
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

  final _atlasRects = List<Rect>.generate(
    32,
    (i) => Rect.fromLTWH(i % 8 * 52.0, i ~/ 8 * 52.0, 52, 52),
    growable: false,
  );
  final _peoplePaint = Paint()
    ..filterQuality = FilterQuality.none
    ..isAntiAlias = false;
  void peopleBand(Canvas canvas, List<(Resident, double)> people) {
    final atlas = game.residentAtlas;
    if (people.isEmpty || atlas == null) return;
    const scale = 9 / 48;
    final transforms = <RSTransform>[], rects = <Rect>[], colors = <Color>[];
    for (final (r, alpha) in people) {
      final pose = game.pose(r), feet = pose.feet;
      final color = alpha == 1
          ? const Color(0xffffffff)
          : Color.fromRGBO(255, 255, 255, alpha);
      transforms.add(
        RSTransform(scale, 0, feet.dx - 26 * scale / 2, feet.dy - .4),
      );
      rects.add(const Rect.fromLTWH(0, 208, 26, 7));
      colors.add(color);
      final body = Appearance.bodies.indexOf(Appearance.resolveBody(r.bodyId));
      final index =
          (body * 4 + r.palette.clamp(0, 3)) * 2 + (pose.facing < 0 ? 1 : 0);
      transforms.add(
        RSTransform.fromComponents(
          rotation: -pose.lean * pose.facing,
          scale: scale,
          anchorX: 26,
          anchorY: 50,
          translateX: feet.dx,
          translateY: feet.dy - pose.lift,
        ),
      );
      rects.add(_atlasRects[index]);
      colors.add(color);
    }
    canvas.drawAtlas(
      atlas,
      transforms,
      rects,
      colors,
      BlendMode.modulate,
      null,
      _peoplePaint,
    );
    for (final (r, alpha) in people) {
      personAccents(canvas, r, alpha);
    }
  }

  void personAccents(Canvas canvas, Resident r, double alpha) {
    final pose = game.pose(r), position = pose.feet;
    if (r.activity != Activity.working &&
        !(pose.gesture > .25 &&
            [Activity.socialising, Activity.gathering].contains(r.activity)) &&
        r.flash <= 0 &&
        game.inspectedResident != r.homeId) {
      return;
    }
    canvas.save();
    canvas.translate(position.dx, position.dy);
    if (game.inspectedResident == r.homeId) {
      canvas.drawOval(
        const Rect.fromLTWH(-3, -1, 6, 2),
        Paint()
          ..color = gold
          ..style = PaintingStyle.stroke
          ..strokeWidth = .4,
      );
    }
    canvas.translate(0, -pose.lift);
    canvas.scale(pose.facing, 1);
    canvas.skew(pose.lean, 0);
    if (r.activity == Activity.working) {
      if (game.colony.kind(r.workId) == Place.garden &&
          r.workProgress > 0 &&
          game.colony.special?.kind == SpecialKind.harvest &&
          game.colony.special!.active(game.colony.clock)) {
        canvas.drawRect(
          const Rect.fromLTWH(-3, -6, 1, 1),
          Paint()..color = gold,
        );
      }
      if (game.colony.kind(r.workId) == Place.workshop) {
        canvas.drawRect(
          Rect.fromLTWH(3, -3 + pose.gesture, 2, .8),
          Paint()..color = const Color(0xffb5c8bf),
        );
      }
      canvas.drawLine(
        Offset(2, -4),
        Offset(4, -1.3 + pose.gesture * 1.2),
        Paint()
          ..color = const Color(0xffd6b97b).withValues(alpha: alpha)
          ..strokeWidth = .7,
      );
    } else if (pose.gesture > .25 &&
        [Activity.socialising, Activity.gathering].contains(r.activity)) {
      canvas.drawRect(
        Rect.fromLTWH(3.4, -5 - pose.gesture, .7, 1.4),
        Paint()..color = const Color(0xffe7bd91).withValues(alpha: alpha),
      );
    }
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
