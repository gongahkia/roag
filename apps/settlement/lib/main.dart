import 'dart:convert';

import 'package:flame/game.dart';
import 'package:flutter/gestures.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter/services.dart';

import 'browser.dart' as browser;
import 'model.dart';
import 'colony.dart';
import 'rules.dart';
import 'appearance.dart';
import 'settlement_game.dart';
import 'build_thumbnail.dart';
import 'dialogue.dart';
import 'resident_request.dart';
import 'world_generation.dart';
import 'world_creation.dart';

void main() => runApp(const SettlementApp());

class SettlementApp extends StatelessWidget {
  const SettlementApp({super.key});
  @override
  Widget build(BuildContext context) => MaterialApp(
    debugShowCheckedModeBanner: false,
    title: 'ROAG · A place to return',
    theme: ThemeData(
      brightness: Brightness.dark,
      scaffoldBackgroundColor: const Color(0xff202a25),
      colorScheme: const ColorScheme.dark(
        primary: Color(0xffdbc68c),
        surface: Color(0xff202a25),
        secondary: Color(0xff9db77c),
      ),
      fontFamily: 'SourceCode',
      textTheme: const TextTheme(
        bodyMedium: TextStyle(fontSize: 12),
        bodySmall: TextStyle(fontSize: 11, color: Color(0xffa4b39d)),
      ),
      tooltipTheme: const TooltipThemeData(
        waitDuration: Duration(milliseconds: 450),
      ),
    ),
    home: const LandscapePage(),
  );
}

class LandscapePage extends StatefulWidget {
  const LandscapePage({super.key});
  @override
  State<LandscapePage> createState() => _LandscapePageState();
}

class _LandscapePageState extends State<LandscapePage> {
  late Colony colony;
  LandscapeSession get session => colony.session;
  bool migrationSafe = true, needsMigrationSave = false;
  late final SettlementGame game;
  final focus = FocusNode();
  Tool tool = Tool.cottage;
  Placement? preview;
  String saveStatus = 'Autosave in this browser';
  String notice = '';
  int noticeId = 0;
  bool muted = false, space = false, menuOpen = false;
  bool introduction = false;
  final catalogueKey = GlobalKey<PopupMenuButtonState<Tool>>();
  Offset? inspectionStart;
  bool inspectionDragged = false;
  Offset? last;
  int? pointer;
  bool panning = false, reportedInvalid = false;
  Conversation? conversation;
  Cell? contextBuilding;
  Offset? contextAnchor;

  @override
  void initState() {
    super.initState();
    HardwareKeyboard.instance.addHandler(hardwareKey);
    colony = Colony(
      LandscapeSession(WorldGeneration.create('202', EnvironmentPreset.plains)),
    );
    try {
      final saved = browser.loadLocal();
      introduction = saved.isEmpty;
      if (saved.isNotEmpty) {
        colony = Colony.decode(saved);
        if (jsonDecode(saved)['schema'] != 6) {
          try {
            final schema = jsonDecode(saved)['schema'] as int;
            if (schema < 3) browser.backupLegacy(saved);
            if (schema < 4) browser.backupLiving(saved);
            if (schema < 5) browser.backupIncidents(saved);
            browser.backupStories(saved);
            needsMigrationSave = true;
          } catch (_) {
            migrationSafe = false;
            notice = 'Could not preserve the old save backup. Export a copy; autosave is paused.';
          }
        }
        saveStatus = 'Saved in this browser';
      }
    } catch (_) {
      migrationSafe = false;
      saveStatus = 'Saved landscape could not be opened';
      notice = 'Temporary landscape. Your saved copy is protected; export before leaving.';
    }
    game = SettlementGame(
      colony,
      (p) {
        if (mounted) setState(() => preview = p);
      },
      onReady: () {
        browser.hideLoading();
        if (needsMigrationSave && migrationSafe) {
          save();
          needsMigrationSave = false;
        }
      },
      profile: Uri.base.queryParameters['profile'] == '1',
      onState: () {
        if (game.profile) browser.reportMetrics(jsonEncode(game.metrics));
        if (mounted) setState(() {});
      },
      onCheckpoint: () {
        if (mounted) save();
      },
    );
    browser.watchActivity((active) {
      game.suspended = !active;
      if (!active && mounted) {
        space = false;
        cancelGesture();
        if (game.canvasReady && migrationSafe) save();
      }
    });
  }

  void tell(String text) {
    setState(() {
      notice = text;
      noticeId++;
    });
    final id = noticeId;
    Future<void>.delayed(const Duration(seconds: 3), () {
      if (mounted && noticeId == id) setState(() => notice = '');
    });
  }

  bool save() {
    try {
      if (!migrationSafe) throw StateError('Original save is protected.');
      colony.persist(colony.snapshot(), browser.saveLocal);
      saveStatus = 'Saved in this browser';
      return true;
    } catch (_) {
      final firstFailure = saveStatus != 'Not saved · export a copy';
      saveStatus = 'Not saved · export a copy';
      if (firstFailure) {
        tell(
          'Browser storage is unavailable or full. Export a portable copy from the menu.',
        );
      }
      return false;
    }
  }

  void select(Tool next) {
    cancelGesture();
    setState(() {
      tool = next;
      game.tool = next;
      game.inspectMode = false;
      game.clearInspection();
      conversation = null;
      game.dialogueOpen = false;
    });
    game.clearPreview();
    focus.requestFocus();
  }

  bool inside(Offset p) =>
      p.dx >= 0 &&
      p.dy >= 0 &&
      p.dx < game.canvasSize.x &&
      p.dy < game.canvasSize.y;

  void paintAt(Offset position) {
    final stroke = session.stroke;
    if (stroke == null) return;
    stroke.paintTo(game.pick(position));
    if (!reportedInvalid &&
        stroke.firstInvalid != null &&
        stroke.tool != Tool.bridge) {
      reportedInvalid = true;
      game.invalid(stroke.firstInvalid!.cell, stroke.firstInvalid!.message);
    }
    game.clearPreview();
    game.hover(position);
    setState(() {});
  }

  void pointerDown(PointerDownEvent e) {
    if (menuOpen ||
        pointer != null ||
        !game.canvasReady ||
        !inside(e.localPosition)) {
      return;
    }
    final pan =
        space || e.buttons & (kSecondaryMouseButton | kMiddleMouseButton) != 0;
    if (!pan && e.buttons & kPrimaryMouseButton == 0) return;
    focus.requestFocus();
    if (!pan &&
        (game.movingFrom != null ||
            game.inspectMode ||
            HardwareKeyboard.instance.isAltPressed)) {
      pointer = e.pointer;
      inspectionStart = e.localPosition;
      inspectionDragged = false;
      last = e.localPosition;
      return;
    }
    pointer = e.pointer;
    last = e.localPosition;
    panning = pan;
    final cell = game.pick(e.localPosition);
    if (!space &&
        e.buttons == kSecondaryMouseButton &&
        game.movingFrom == null &&
        session.world.contains(cell) &&
        buildingTool(session.world.at(cell)) != null) {
      contextBuilding = cell;
      contextAnchor = e.position;
    }
    reportedInvalid = false;
    if (!pan) {
      session.beginStroke(tool, game.pick(e.localPosition));
      paintAt(e.localPosition);
    }
  }

  void pointerMove(PointerMoveEvent e) {
    if (pointer != e.pointer || last == null) return;
    if (inspectionStart != null) {
      if ((e.localPosition - inspectionStart!).distance > 5) {
        inspectionDragged = true;
      }
      if (game.movingFrom != null) game.hover(e.localPosition);
      return;
    }
    if (contextBuilding != null) {
      if ((e.position - contextAnchor!).distance <= 5) return;
      contextBuilding = null;
    }
    if (!inside(e.localPosition)) {
      finishGesture();
      return;
    }
    if (panning) {
      game.pan(e.localPosition - last!);
    } else {
      paintAt(e.localPosition);
    }
    last = e.localPosition;
  }

  void pointerUp(PointerUpEvent e) {
    if (pointer != e.pointer) return;
    if (inspectionStart != null) {
      if (!inspectionDragged &&
          (e.localPosition - inspectionStart!).distance <= 5) {
        if (game.movingFrom != null) {
          commitMove(e.localPosition);
        } else {
          conversation = null;
          game.dialogueOpen = false;
          game.inspect(e.localPosition);
        }
      }
      inspectionStart = null;
      pointer = null;
      last = null;
      setState(() {});
      return;
    }
    if (contextBuilding != null &&
        (e.position - contextAnchor!).distance <= 5) {
      final cell = contextBuilding!;
      finishGesture();
      contextBuilding = null;
      buildingMenu(cell, e.position);
      return;
    }
    if (!panning && inside(e.localPosition)) paintAt(e.localPosition);
    finishGesture();
  }

  void finishGesture() {
    final stroke = session.stroke;
    pointer = null;
    last = null;
    panning = false;
    final committed = session.finishStroke();
    game.clearPreview();
    if (committed && stroke != null) {
      game.reconcile();
      game.feedback(stroke.changed.last, true, stroke.tool == Tool.remove);
      final saved = save();
      browser.sound(
        stroke.tool == Tool.remove
            ? 'remove'
            : stroke.tool.isRoad
            ? 'path'
            : 'place',
      );
      if (saved) {
        final noun = stroke.tool == Tool.path
            ? 'path cell'
            : stroke.tool == Tool.remove
            ? 'piece'
            : stroke.tool.label.toLowerCase();
        tell(
          '${stroke.changed.length} $noun${stroke.changed.length == 1 ? '' : 's'} ${stroke.tool == Tool.repair
              ? 'repaired'
              : stroke.tool == Tool.remove
              ? 'removed'
              : 'placed'} · ${Rules.coins(stroke.price)} coins',
        );
      }
    } else if (session.historyError != null) {
      tell(session.historyError!);
    } else if (stroke?.firstInvalid != null) {
      browser.sound('invalid');
    }
    if (mounted) setState(() {});
  }

  void cancelGesture({bool cancelMove = true}) {
    inspectionStart = null;
    inspectionDragged = false;
    session.cancelStroke();
    pointer = null;
    last = null;
    panning = false;
    contextBuilding = null;
    contextAnchor = null;
    if (cancelMove) game.movingFrom = null;
    game.clearPreview();
  }

  void cameraAction(void Function() action) {
    cancelGesture(cancelMove: false);
    action();
    focus.requestFocus();
  }

  void history(bool redo) {
    if (session.stroke != null) {
      cancelGesture();
      return;
    }
    cancelGesture();
    if (redo ? session.redo() : session.undo()) {
      game.reconcile(restored: true);
      final saved = save();
      game.clearPreview();
      browser.sound('undo');
      if (saved) tell(redo ? 'Redone' : 'Undone');
      setState(() {});
    } else if (session.historyError != null) {
      tell(session.historyError!);
    }
    focus.requestFocus();
  }

  KeyEventResult key(FocusNode node, KeyEvent e) {
    if (menuOpen) return KeyEventResult.ignored;
    if (e.logicalKey == LogicalKeyboardKey.space) {
      if (conversation != null && !focus.hasPrimaryFocus) {
        return KeyEventResult.ignored;
      }
      space = e is! KeyUpEvent;
      return KeyEventResult.handled;
    }
    if (e is! KeyDownEvent) return KeyEventResult.ignored;
    final k = e.logicalKey;
    final modifier =
        HardwareKeyboard.instance.isControlPressed ||
        HardwareKeyboard.instance.isMetaPressed;
    if (modifier && k == LogicalKeyboardKey.keyZ) {
      history(HardwareKeyboard.instance.isShiftPressed);
      return KeyEventResult.handled;
    }
    if (modifier && k == LogicalKeyboardKey.keyY) {
      history(true);
      return KeyEventResult.handled;
    }
    if (k == LogicalKeyboardKey.digit1 || k == LogicalKeyboardKey.keyC) {
      select(Tool.cottage);
    } else if (k == LogicalKeyboardKey.digit2 || k == LogicalKeyboardKey.keyP) {
      select(Tool.path);
    } else if (k == LogicalKeyboardKey.digit3 || k == LogicalKeyboardKey.keyR) {
      select(Tool.remove);
    } else if (k == LogicalKeyboardKey.digit4 || k == LogicalKeyboardKey.keyG) {
      select(Tool.garden);
    } else if (k == LogicalKeyboardKey.digit5 || k == LogicalKeyboardKey.keyV) {
      select(Tool.green);
    } else if (k == LogicalKeyboardKey.digit6) {
      select(Tool.workshop);
    } else if (k == LogicalKeyboardKey.digit7) {
      select(Tool.market);
    } else if (k == LogicalKeyboardKey.digit8) {
      select(Tool.hall);
    } else if (k == LogicalKeyboardKey.digit9) {
      select(Tool.stoneRoad);
    } else if (k == LogicalKeyboardKey.digit0) {
      select(Tool.gardenPath);
    } else if (k == LogicalKeyboardKey.keyB) {
      catalogueKey.currentState?.showButtonMenu();
    } else if (k == LogicalKeyboardKey.keyI) {
      toggleInspection();
    } else if (k == LogicalKeyboardKey.keyM && game.movableSelection) {
      beginMove(game.inspectedCell!);
    } else if (k == LogicalKeyboardKey.keyT) {
      setSpeed(switch (colony.speed) {
        0 => 1,
        1 => 2,
        2 => 4,
        _ => 0,
      });
    } else if (k == LogicalKeyboardKey.home || k == LogicalKeyboardKey.keyH) {
      cameraAction(() => game.home());
    } else if (k == LogicalKeyboardKey.equal ||
        k == LogicalKeyboardKey.numpadAdd) {
      cameraAction(() => game.zoom(1));
    } else if (k == LogicalKeyboardKey.minus ||
        k == LogicalKeyboardKey.numpadSubtract) {
      cameraAction(() => game.zoom(-1));
    } else if (k == LogicalKeyboardKey.arrowLeft) {
      cameraAction(() => game.pan(const Offset(48, 0)));
    } else if (k == LogicalKeyboardKey.arrowRight) {
      cameraAction(() => game.pan(const Offset(-48, 0)));
    } else if (k == LogicalKeyboardKey.arrowUp) {
      cameraAction(() => game.pan(const Offset(0, 48)));
    } else if (k == LogicalKeyboardKey.arrowDown) {
      cameraAction(() => game.pan(const Offset(0, -48)));
    } else if (k == LogicalKeyboardKey.escape) {
      cancelGesture();
      game.inspectMode = false;
      game.clearInspection();
      conversation = null;
      game.dialogueOpen = false;
      setState(() {});
    } else {
      return KeyEventResult.ignored;
    }
    return KeyEventResult.handled;
  }

  bool hardwareKey(KeyEvent event) =>
      key(focus, event) == KeyEventResult.handled;

  Future<void> menu(String action) async {
    cancelGesture();
    setState(() => menuOpen = true);
    game.clearPreview();
    game.menuVisible = true;
    try {
      if (action == 'incidents') {
        await showPreparedness();
      } else if (action == 'stories') {
        await showHistory();
      } else if (action == 'yearwatch') {
        setSpeed(colony.speed == 16 ? 1 : 16);
        tell(
          'Year watch: ${colony.speed}×. Ordinary speed buttons still work.',
        );
      } else if (action == 'climate') {
        await showClimate();
      } else if (action == 'export') {
        browser.exportFile(colony.snapshot().text);
        tell('Exported a portable landscape copy.');
      } else if (action == 'import') {
        final text = await browser.importFile();
        if (text.isNotEmpty && mounted) {
          final imported = Colony.decode(text);
          final confirmed = await showDialog<bool>(
            context: context,
            builder: (context) => AlertDialog(
              title: const Text('Replace the active settlement?'),
              content: const Text(
                'This imported town replaces your current settlement and undo history. Export your current copy first to keep it.',
              ),
              actions: [
                TextButton(
                  onPressed: () => browser.exportFile(colony.snapshot().text),
                  child: const Text('Export current copy'),
                ),
                TextButton(
                  onPressed: () => Navigator.pop(context, false),
                  child: const Text('Keep current settlement'),
                ),
                FilledButton(
                  onPressed: () => Navigator.pop(context, true),
                  child: const Text('Replace settlement'),
                ),
              ],
            ),
          );
          if (confirmed != true || !mounted) return;
          replaceColony(imported);
          migrationSafe = true;
          introduction = false;
          final saved = save();
          game.home();
          if (saved) tell('Landscape imported.');
        }
      } else if (action == 'new') {
        final draft = await showDialog<WorldDraft>(
          context: context,
          builder: (_) => WorldCreationDialog(
            exportCurrent: () => browser.exportFile(colony.snapshot().text),
          ),
        );
        if (draft != null && mounted) {
          final world = WorldGeneration.create(draft.seed, draft.preset);
          replaceColony(
            Colony(LandscapeSession(world))
              ..incidentMode = draft.incidents
              ..configureScenario(draft.scenario),
          );
          migrationSafe = true;
          introduction = true;
          final saved = save();
          game.home();
          if (saved) tell('${draft.preset.label} · seed ${draft.seed}');
        }
      } else if (action == 'world') {
        final info = session.world.generation;
        final seed = info?.seedText ?? session.world.seed.toString();
        await showDialog<void>(
          context: context,
          builder: (context) => AlertDialog(
            title: Text(info?.preset.label ?? 'Existing settlement'),
            content: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SelectableText('Seed: $seed'),
                  Text(
                    info == null
                        ? 'Legacy landscape · original authored layout preserved.'
                        : 'Generation v${info.version} · 40 × 30 cells · concrete land saved.',
                  ),
                  const SizedBox(height: 12),
                  for (final t in Terrain.values)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: Text('${t.label}: ${t.instruction}'),
                    ),
                  const Text(
                    'Wet paths cost 1.5× travel time (2× after prolonged Wetlands rain); weather uses the larger penalty once. Stone stays 18% quicker and withstands bank floods. Complete bridges stay above floods; protect their approaches. Clear costs 1; Plant 1; Bridge 3 per water cell.',
                  ),
                ],
              ),
            ),
            actions: [
              TextButton(
                onPressed: () {
                  Clipboard.setData(ClipboardData(text: seed));
                  tell('Seed copied.');
                },
                child: const Text('Copy seed'),
              ),
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Back to settlement'),
              ),
            ],
          ),
        );
      } else if (action == 'unlimited') {
        setState(() => session.unlimited = !session.unlimited);
        save();
      } else if (action == 'help') {
        await showDialog<void>(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text('A small place, shaped by you'),
            content: SingleChildScrollView(
              child: Text(
                '1 Cottage (5 coins) · 2 Path · 3 Remove · 4/G Farm (8) · 5/V Park (6)\n'
                'Click or drag, release to commit one stroke. Escape cancels. Undo during a stroke cancels it first.\n\n'
                'Farms have ${Rules.gardenCapacity} work positions: people must reach them and work to earn ${Rules.coins(Rules.workIncomePerSecond)} coins/second each. The settlement grant adds 20 coins per 150-second day. Paths and demolition are free.\n\n'
                'Parks have ${Rules.greenCapacity} visitor spaces for actual social visits and relaxation. Work and leisure improve mood after real activity; newcomers get 75 seconds of grace. Connected, successful neighbourhoods can gather in the evening.\n\n'
                'Landscape: Clear trees/wet ground (1), Plant a tree (1), Bridge a straight bank-to-bank span (3/water cell). Water blocks travel until the whole bridge connects. Clear marsh for buildings; roads can use it slowly. Stone mitigates wet ground and rain.\n\n'
                'New landscape lets you choose an environment and seed; export before replacing your town. World information shows terrain rules and a copyable seed.\n\n'
                'Preparedness (B): Firehouse 18 coins / 2 automatic staff; Watchpost 14 / 1 guard; Refuge 10 / 8 temporary shelter spaces. Inn 16 / 1 worker, 4 social spaces. Repair damaged safe buildings for 1–5 coins. Fresh unused repairs can be undone; used work and new damage remain forward.\n\n'
                'Incidents can be Peaceful or Standard in the menu. Legacy towns start Peaceful. New Standard towns have three days of grace. Prepare roads, staff and shelter; inhabitants respond on their own.\n\n'
                'B: grouped build catalogue. 6 Workshop · 7 Market · 8 Hall · 9 Stone Road · 0 Garden Path.\n\n'
                'I: inspect; Talk opens a short optional conversation. Move (M) relocates an inspected building for free; right-click offers the same shortcut. Escape cancels. Click again to cycle overlapping people. Alt-click: quick inspection. T cycles pause/1×/2×/4×. People follow their routines automatically.\n\n'
                'Right/middle or Space-drag pans. Scroll or +/− zooms. H returns to the lane. Ctrl/⌘ Z undoes; Shift Z redoes. Undo changes construction charges, not earned income or time.\n\n'
                'Automatic checkpoints belong to this browser and origin. Export is portable. Old saves are backed up before migration. No ROAG dungeon storage is used.',
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Back to the landscape'),
              ),
            ],
          ),
        );
      }
    } catch (e) {
      tell(
        action == 'import'
            ? 'Import refused. The current landscape is unchanged. ${e is FormatException ? e.message : 'The file could not be read.'}'
            : 'Could not export this landscape.',
      );
    } finally {
      if (mounted) {
        setState(() => menuOpen = false);
        game.menuVisible = false;
        focus.requestFocus();
      }
    }
  }

  Future<void> showHistory() async {
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('${colony.scenario.label} · settlement history'),
        content: SizedBox(
          width: 440,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(colony.scenario.description),
                for (var i = 0; i < colony.scenario.objectives.length; i++)
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text(
                      '${colony.scenarioCompleted.contains(i) ? '✓' : '○'} ${colony.scenario.objectives[i]}',
                    ),
                  ),
                const SizedBox(height: 12),
                const Text(
                  'Objectives are optional, remain completed and never end the town. Peaceful keeps destructive incident goals optional. History records actual outcomes; no repeatable reward claims.',
                ),
                if (colony.stories.isEmpty && colony.incidentHistory.isEmpty)
                  const Padding(
                    padding: EdgeInsets.only(top: 12),
                    child: Text('No major memories yet. Keep building.'),
                  ),
                for (final s in colony.stories.reversed)
                  Padding(
                    padding: const EdgeInsets.only(top: 10),
                    child: Text(
                      'Day ${(s.time / Rules.day).floor() + 1} · ${s.time.toStringAsFixed(1)}s · (${s.cell.x + 1}, ${s.cell.y + 1})\n${s.outcome}',
                    ),
                  ),
                for (final h in colony.incidentHistory.reversed.where(
                  (h) => !colony.stories.any((s) => s.source == h.id),
                ))
                  Padding(
                    padding: const EdgeInsets.only(top: 10),
                    child: Text(
                      'Earlier · Day ${(h.ends / Rules.day).floor() + 1}\n${h.kind}: ${h.outcome}; ${h.damage} damage, ${h.responders} arrived responders.',
                    ),
                  ),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Back to building'),
          ),
        ],
      ),
    );
  }

  Future<void> showPreparedness() async {
    await showDialog<void>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, update) => AlertDialog(
          title: const Text('Preparedness & incidents'),
          content: SizedBox(
            width: 430,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  DropdownButton<IncidentMode>(
                    value: colony.incidentMode,
                    items: const [
                      DropdownMenuItem(
                        value: IncidentMode.peaceful,
                        child: Text('Peaceful'),
                      ),
                      DropdownMenuItem(
                        value: IncidentMode.standard,
                        child: Text('Standard'),
                      ),
                    ],
                    onChanged: (mode) {
                      if (mode == null) return;
                      setState(() => colony.setIncidentMode(mode));
                      update(() {});
                      save();
                    },
                  ),
                  const Text(
                    'Peaceful prevents new destructive incidents. Standard: at least 3 days of grace after opt-in, then occasional recoverable trouble. An incident already underway continues. Pause with Ⅱ while planning.',
                  ),
                  const SizedBox(height: 12),
                  Text(
                    colony.incident?.description ?? 'No active incident. Build connected Firehouses, Watchposts and Refuges through B. Staff train by actually reaching work; no individual commands.',
                  ),
                  const SizedBox(height: 12),
                  const Text(
                    'Fire: gaps stop spread. Floods: coherent bank overflow; Stone Roads and complete bridges stay open, ordinary roads close temporarily. Wildlife and brigands: guards within 10 weighted road steps must arrive. Caravan: real Inn service or spare Refuge space. Repair after danger passes.',
                  ),
                  for (final h in colony.incidentHistory.reversed.take(6))
                    Padding(
                      padding: const EdgeInsets.only(top: 8),
                      child: Text(
                        '${h.kind} · ${h.outcome} · ${h.damage} damage · ${h.responders} arrived · ${h.sheltered} sheltered',
                      ),
                    ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Back to building'),
            ),
          ],
        ),
      ),
    );
  }

  void replaceColony(Colony next) {
    cancelGesture();
    next.rebaseNavigation(colony.navigation.revision);
    colony = next;
    game.colony = next;
    game.resetPresentation();
    conversation = null;
  }

  Future<void> showClimate() async {
    final calendar = colony.climate.calendar;
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(
          '${colony.season.label} ${calendar.seasonDay(colony.clock)} · Year ${calendar.year(colony.clock)}',
        ),
        content: SizedBox(
          width: 370,
          child: Text(
            'Day ${colony.day} · ${colony.phase}\n${colony.weather.label}: ${colony.weather.effect}\n\n${colony.climate.forecast(colony.clock)}\n\n'
            '${calendar.daysPerSeason} days per season · 150 seconds per day. Farm output: Spring +10%, Summer ordinary, Autumn +15%, Winter −20%; Wind −5%, Snow −10%; Good Harvest ×1.25. Combined output is capped at 65–150%. Current Farm multiplier: ${(colony.climate.farmMultiplier(colony.clock, harvest: colony.special?.kind.name == 'harvest' && colony.special!.active(colony.clock)) * 100).round()}%.\n\n'
            'Wet ground and weather use the larger travel penalty, once. Stone Roads stay quick. Winter, Rain and Snow favour covered Halls; Summer favours Parks. Existing trips finish their current segment.\n\n'
            'New homes attract Humans (60%), Imps (20%, Workshop preference) and Djinn (20%, Hall preference), seeded by home identity. Existing residents keep their identities.\n\n'
            'Working Markets attract merchants; Parks and Halls attract storytellers. They need real roads, space and time. Optional conversations do not interrupt building. Completed visits: ${colony.guestCompletions.values.fold<int>(0, (a, b) => a + b)}. Visitor trade: ${Rules.coins(colony.visitorEarned)} coins.',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Back to settlement'),
          ),
        ],
      ),
    );
  }

  void setSpeed(int speed) {
    setState(() => colony.speed = speed);
    save();
    focus.requestFocus();
  }

  void toggleInspection() {
    cancelGesture();
    setState(() {
      game.inspectMode = !game.inspectMode;
      if (!game.inspectMode) game.clearInspection();
      conversation = null;
      game.dialogueOpen = false;
    });
    focus.requestFocus();
  }

  void beginMove(Cell from) {
    if (!session.world.contains(from) ||
        buildingTool(session.world.at(from)) == null) {
      return;
    }
    cancelGesture();
    setState(() {
      conversation = null;
      game.dialogueOpen = false;
      game.movingFrom = from;
      game.inspectMode = false;
      game.clearInspection();
    });
    focus.requestFocus();
  }

  void commitMove(Offset point) {
    final from = game.movingFrom;
    if (from == null) return;
    final plan = evaluateMove(session.world, from, game.pick(point));
    if (!session.relocate(plan)) {
      game.invalid(plan.cell, plan.message);
      tell(plan.message);
      return;
    }
    game.movingFrom = null;
    game.clearPreview();
    game.reconcile();
    game.feedback(plan.cell, true, false);
    save();
    browser.sound('place');
    tell('Building moved · Free · identity and past earnings retained');
  }

  Future<void> buildingMenu(Cell cell, Offset position) async {
    cancelGesture();
    conversation = null;
    game.dialogueOpen = false;
    setState(() => menuOpen = true);
    game.menuVisible = true;
    final label = buildingTool(session.world.at(cell))!.label;
    final action = await showMenu<String>(
      context: context,
      position: RelativeRect.fromRect(
        Rect.fromLTWH(position.dx, position.dy, 1, 1),
        Offset.zero & MediaQuery.sizeOf(context),
      ),
      items: [
        PopupMenuItem(value: 'inspect', child: Text('Inspect $label')),
        PopupMenuItem(value: 'move', child: Text('Move $label · Free')),
      ],
    );
    if (!mounted) return;
    setState(() => menuOpen = false);
    game.menuVisible = false;
    if (action == 'move') {
      beginMove(cell);
    } else if (action == 'inspect') {
      game.inspectBuilding(cell);
    }
    focus.requestFocus();
  }

  void talk({String? resident, bool civic = false}) {
    final id = resident ?? game.inspectedResident;
    if (id == null ||
        (!colony.people.containsKey(id) && !colony.visits.containsKey(id))) {
      return;
    }
    cancelGesture();
    setState(() {
      conversation = Conversation(
        id,
        node: colony.visits.containsKey(id)
            ? 'visitor'
            : civic
            ? 'civic'
            : 'hello',
        event: civic ? colony.special?.id : null,
      );
      game.dialogueOpen = true;
    });
  }

  void chooseDialogue(String id) {
    if (conversation == null) return;
    final result = Dialogues.choose(colony, conversation!, id);
    setState(() {
      conversation = result.conversation;
      game.dialogueOpen = conversation != null;
      if (conversation == null && id == 'done') {
        game.clearInspection();
        game.inspectMode = false;
      }
    });
    if (result.changed) {
      save();
      browser.sound('place');
    }
    if (result.notice != null) tell(result.notice!);
    focus.requestFocus();
  }

  void locateConcern(String resident) {
    final r = colony.people[resident];
    if (r == null) return;
    final workConcern = colony.requests
        .forResident(resident)
        .any(
          (q) => q.state == RequestState.accepted && q.need == Need.workAccess,
        );
    final target = workConcern
        ? colony.world.places[r.workId] ?? colony.world.cottages[resident]
        : colony.world.cottages[resident];
    if (target != null) cameraAction(() => game.locate(target));
  }

  Widget inspectionPanel() {
    final chat = conversation;
    final person = colony.actor(chat?.resident ?? game.inspectedResident);
    final view = chat == null ? null : Dialogues.view(colony, chat);
    return ConstrainedBox(
      constraints: BoxConstraints(
        maxHeight: (game.canvasSize.y - 28).clamp(80, 900),
      ),
      child: Container(
        color: const Color(0xf5202a25),
        padding: const EdgeInsets.all(12),
        child: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              if (person != null)
                Image.asset(
                  'assets/images/${Appearance.sprite(person.bodyId, person.palette)}.png',
                  width: 36,
                  height: 36,
                  fit: BoxFit.contain,
                  filterQuality: FilterQuality.none,
                ),
              if (view == null) ...[
                Text(
                  game.inspection ?? '',
                  style: const TextStyle(
                    fontSize: 11,
                    color: Color(0xffeadfbd),
                  ),
                ),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 6,
                  children: [
                    if (person != null &&
                        (colony.people.containsKey(person.homeId) ||
                            colony.visits.containsKey(person.homeId)))
                      TextButton(
                        onPressed: () => talk(),
                        child: const Text('Talk'),
                      ),
                    if (person != null &&
                        colony.requests
                            .forResident(person.homeId)
                            .any((r) => r.state == RequestState.accepted))
                      TextButton(
                        onPressed: () => locateConcern(person.homeId),
                        child: const Text('Locate request'),
                      ),
                    if (game.movableSelection)
                      TextButton(
                        onPressed: () => beginMove(game.inspectedCell!),
                        child: const Text('Move building · Free (M)'),
                      ),
                    if (game.inspectedEvent &&
                        colony.civicAvailable(colony.special?.id))
                      TextButton(
                        onPressed: () =>
                            talk(resident: colony.civicHost, civic: true),
                        child: const Text('Discuss Market day'),
                      ),
                    TextButton(
                      onPressed: () {
                        setState(() {
                          game.clearInspection();
                          game.inspectMode = false;
                        });
                        focus.requestFocus();
                      },
                      child: const Text('Close'),
                    ),
                  ],
                ),
                const Text(
                  'Escape closes · I toggles inspect mode',
                  style: TextStyle(fontSize: 10, color: Color(0xffa4b39d)),
                ),
              ] else ...[
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        person?.name ?? 'Conversation',
                        style: const TextStyle(color: Color(0xffead39b)),
                      ),
                    ),
                    IconButton(
                      tooltip: 'Close conversation',
                      onPressed: () {
                        setState(() {
                          conversation = null;
                          game.dialogueOpen = false;
                        });
                        focus.requestFocus();
                      },
                      icon: const Icon(Icons.close, size: 16),
                    ),
                  ],
                ),
                if (person != null)
                  Text(
                    '${person.trait.label} · ${game.activityLabel(person.activity)}',
                    style: const TextStyle(
                      fontSize: 10,
                      color: Color(0xffb9c4ad),
                    ),
                  ),
                const SizedBox(height: 10),
                Text(
                  view.line,
                  style: const TextStyle(
                    fontSize: 12,
                    height: 1.35,
                    color: Color(0xffeadfbd),
                  ),
                ),
                const SizedBox(height: 8),
                for (final (choice, disabled) in view.choices) ...[
                  Align(
                    alignment: Alignment.centerLeft,
                    child: TextButton(
                      onPressed: disabled == null
                          ? () => chooseDialogue(choice.id)
                          : null,
                      child: Text(
                        choice.label,
                        style: const TextStyle(fontSize: 11),
                      ),
                    ),
                  ),
                  if (disabled != null)
                    Text(
                      disabled,
                      style: const TextStyle(
                        fontSize: 10,
                        color: Color(0xffc89e82),
                      ),
                    ),
                ],
                Text(
                  colony.speed == 0
                      ? 'Simulation paused by you.'
                      : 'The town keeps living while we talk.',
                  style: const TextStyle(fontSize: 9, color: Color(0xffa4b39d)),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget button(
    String label,
    String tip,
    VoidCallback? press, {
    Widget? icon,
  }) => Tooltip(
    message: tip,
    child: TextButton(
      onPressed: press,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[icon, const SizedBox(width: 6)],
          Text(label),
        ],
      ),
    ),
  );

  @override
  Widget build(BuildContext context) {
    game.reducedMotion = MediaQuery.disableAnimationsOf(context);
    final scene = session.view;
    final houses = scene.cells
        .where((c) => scene.at(c).cottage != null)
        .toList();
    final lost = houses.where((c) => scene.access(c) == null).length;
    String hint = preview?.message ?? tool.purpose;
    if (session.stroke != null) {
      hint =
          'Release: ${session.stroke!.changed.length} pieces · ${session.stroke!.free ? 'Free' : '${Rules.coins(session.stroke!.price)} / ${Rules.coins(session.stroke!.budget)} coins'} · Escape cancels';
    }
    if (game.movingFrom != null) {
      hint =
          preview?.message ??
          'Move building · Free · choose a connected plot · Escape cancels';
    } else if (game.inspectMode) {
      hint = 'Click a person or place · repeat click cycles nearby people · Escape closes.';
    }
    if (preview != null &&
        scene.contains(preview!.cell) &&
        scene.at(preview!.cell).cottage != null) {
      hint = scene.access(preview!.cell) == null
          ? 'Cottage · No access · reconnect a path'
          : hint;
    }
    return Scaffold(
      body: Focus(
        autofocus: true,
        focusNode: focus,
        onFocusChange: (focused) {
          if (!focused) {
            space = false;
            cancelGesture();
          }
        },
        child: Column(
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
              decoration: const BoxDecoration(
                border: Border(bottom: BorderSide(color: Color(0xff475441))),
              ),
              child: Row(
                children: [
                  const Text(
                    'A place to return',
                    style: TextStyle(fontSize: 18, color: Color(0xffeadfbd)),
                  ),
                  const Spacer(),
                  Tooltip(
                    message:
                        'Productive work: ${Rules.coins(colony.workEarned)} coins. Completed Market trade: ${Rules.coins(colony.tradeEarned)} coins. Grant: ${Rules.coins(colony.grantEarned)} coins.${colony.civicSpent == 0 ? '' : ' Civic support: ${Rules.coins(colony.civicSpent)} coins spent.'} No maintenance.',
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Semantics(
                          label:
                              'Treasury: ${Rules.coins(session.money)} coins',
                          excludeSemantics: true,
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Image.asset(
                                'assets/images/coin.png',
                                width: 16,
                                height: 16,
                                filterQuality: FilterQuality.none,
                                color: const Color(0xffead39b),
                              ),
                              const SizedBox(width: 6),
                              Text(
                                Rules.coins(session.money),
                                style: const TextStyle(
                                  color: Color(0xffead39b),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const Text(
                          'Grant +20/day',
                          style: TextStyle(
                            fontSize: 9,
                            color: Color(0xffa4b39d),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 18),
                  Text(
                    '${colony.people.length} people${lost > 0 ? ' · $lost cut off' : ''}',
                    style: const TextStyle(fontSize: 11),
                  ),
                  const SizedBox(width: 18),
                  Tooltip(
                    message: 'Base 50 · recent work +15 · leisure +15 · gathering +10. Persistent connection −20; missed work/leisure −10 each after grace. I inspects actual causes.',
                    child: Text(
                      '${colony.mood} ${colony.happinessMean}',
                      style: const TextStyle(
                        color: Color(0xffbcd7ac),
                        fontSize: 11,
                      ),
                    ),
                  ),
                  const SizedBox(width: 18),
                  TextButton(
                    onPressed: () => menu('climate'),
                    style: TextButton.styleFrom(
                      padding: const EdgeInsets.symmetric(horizontal: 4),
                    ),
                    child: Text(
                      'Day ${colony.day} · ${colony.phase}\n${colony.season.label} · ${colony.weather.label}${colony.speed == 16 ? ' · 16×' : ''}',
                      style: const TextStyle(fontSize: 10),
                    ),
                  ),
                  const SizedBox(width: 10),
                  for (final speed in [0, 1, 2, 4])
                    Tooltip(
                      message: speed == 0
                          ? 'Pause simulation'
                          : '$speed× simulation',
                      child: TextButton(
                        style: TextButton.styleFrom(
                          minimumSize: const Size(30, 32),
                          padding: const EdgeInsets.symmetric(horizontal: 6),
                          foregroundColor: colony.speed == speed
                              ? const Color(0xfff0dba0)
                              : const Color(0xff809078),
                        ),
                        onPressed: () => setSpeed(speed),
                        child: Text(
                          speed == 0 ? 'Ⅱ' : '$speed×',
                          style: const TextStyle(fontSize: 12),
                        ),
                      ),
                    ),
                  IconButton(
                    tooltip: 'Inspect (I)',
                    onPressed: toggleInspection,
                    icon: const Icon(Icons.info_outline, size: 18),
                  ),
                  IconButton(
                    tooltip: muted ? 'Unmute' : 'Mute',
                    onPressed: () {
                      setState(() => muted = !muted);
                      browser.mute(muted);
                      focus.requestFocus();
                    },
                    icon: Icon(
                      muted
                          ? Icons.volume_off_outlined
                          : Icons.volume_up_outlined,
                      size: 20,
                    ),
                  ),
                  PopupMenuButton<String>(
                    tooltip: 'Landscape menu',
                    onOpened: () {
                      menuOpen = true;
                      game.menuVisible = true;
                      space = false;
                      cancelGesture();
                    },
                    onCanceled: () => setState(() {
                      menuOpen = false;
                      game.menuVisible = false;
                    }),
                    onSelected: menu,
                    itemBuilder: (context) => [
                      PopupMenuItem(
                        enabled: false,
                        child: Text(
                          saveStatus,
                          style: const TextStyle(fontSize: 11),
                        ),
                      ),
                      const PopupMenuItem(
                        value: 'export',
                        child: Text('Export landscape…'),
                      ),
                      const PopupMenuItem(
                        value: 'import',
                        child: Text('Import landscape…'),
                      ),
                      const PopupMenuItem(
                        value: 'new',
                        child: Text('New landscape…'),
                      ),
                      PopupMenuItem(
                        value: 'incidents',
                        child: Text(
                          'Incidents: ${colony.incidentMode == IncidentMode.peaceful ? 'Peaceful' : 'Standard'}',
                        ),
                      ),
                      const PopupMenuItem(
                        value: 'stories',
                        child: Text('Scenario & settlement history'),
                      ),
                      const PopupMenuItem(
                        value: 'world',
                        child: Text('World information & seed'),
                      ),
                      PopupMenuItem(
                        value: 'unlimited',
                        child: Text(
                          'Unlimited building: ${session.unlimited ? 'On' : 'Off'}',
                        ),
                      ),
                      const PopupMenuItem(
                        value: 'climate',
                        child: Text('Calendar & forecast'),
                      ),
                      PopupMenuItem(
                        value: 'yearwatch',
                        child: Text(
                          'Watch a year · ${colony.speed == 16 ? 'Return to 1×' : '16×'}',
                        ),
                      ),
                      const PopupMenuDivider(),
                      const PopupMenuItem(
                        value: 'help',
                        child: Text('Controls & about'),
                      ),
                    ],
                    icon: const Icon(Icons.more_horiz),
                  ),
                ],
              ),
            ),
            Expanded(
              child: ClipRect(
                child: Stack(
                  children: [
                    Positioned.fill(
                      child: MouseRegion(
                        onExit: (event) {
                          // only physical boundary crossings finish a stroke;
                          // the activity handler cancels on focus loss.
                          if (pointer != null && !inside(event.localPosition)) {
                            finishGesture();
                          } else if (pointer == null) {
                            game.clearPreview();
                          }
                        },
                        onHover: (e) {
                          if (!menuOpen) game.hover(e.localPosition);
                        },
                        child: Listener(
                          onPointerDown: pointerDown,
                          onPointerMove: pointerMove,
                          onPointerUp: pointerUp,
                          onPointerCancel: (_) => cancelGesture(),
                          onPointerSignal: (e) {
                            if (e is PointerScrollEvent && !menuOpen) {
                              GestureBinding.instance.pointerSignalResolver
                                  .register(e, (event) {
                                    cameraAction(
                                      () => game.zoom(
                                        e.scrollDelta.dy < 0 ? 1 : -1,
                                        e.localPosition,
                                      ),
                                    );
                                  });
                            }
                          },
                          child: GameWidget(
                            game: game,
                            autofocus: false,
                            loadingBuilder: (_) => const Center(
                              child: Text('Opening the landscape…'),
                            ),
                            errorBuilder: (_, error) => Center(
                              child: Text(
                                'Could not open the landscape. Reload to try again.\n$error',
                              ),
                            ),
                          ),
                        ),
                      ),
                    ),
                    const Positioned(
                      left: 24,
                      top: 18,
                      child: IgnorePointer(
                        child: Text(
                          'Homes, farms, parks. Listen to your neighbours.',
                          style: TextStyle(
                            color: Color(0xffc3c9aa),
                            fontSize: 11,
                          ),
                        ),
                      ),
                    ),
                    if (colony.visits.isNotEmpty && !introduction)
                      Positioned(
                        left: 24,
                        top: colony.special?.active(colony.clock) == true
                            ? 76
                            : 38,
                        right: game.inspection != null ? 332 : 24,
                        child: Wrap(
                          spacing: 6,
                          children: [
                            for (final v in colony.visits.values)
                              TextButton(
                                onPressed: () {
                                  game.inspectedResident = v.id;
                                  game.inspectedCell = null;
                                  cameraAction(() => game.locate(v.actor.cell));
                                  setState(() {});
                                },
                                child: Text(
                                  '${v.role.label} · ${v.state}',
                                  style: const TextStyle(fontSize: 10),
                                ),
                              ),
                          ],
                        ),
                      ),
                    if (colony.incident != null)
                      Positioned(
                        left: 20,
                        top: 112,
                        child: SizedBox(
                          width: 320,
                          child: Card(
                            color: const Color(0xee202a25),
                            child: TextButton(
                              onPressed: () {
                                final c = colony.buildingCell(
                                  colony.incident!.target,
                                );
                                if (c != null) {
                                  game.locate(c);
                                  game.inspectBuilding(c);
                                }
                                menu('incidents');
                              },
                              child: Text(
                                '${colony.incident!.title} · ${colony.incident!.phaseLabel} · locate / details',
                                style: const TextStyle(
                                  color: Color(0xffe3b586),
                                  fontSize: 11,
                                ),
                              ),
                            ),
                          ),
                        ),
                      ),
                    if (colony.incident == null &&
                        colony.incidentHistory.isNotEmpty &&
                        colony.clock - colony.incidentHistory.last.ends < 12)
                      Positioned(
                        left: 20,
                        top: 112,
                        child: SizedBox(
                          width: 320,
                          child: Card(
                            color: const Color(0xee202a25),
                            child: TextButton(
                              onPressed: () => menu('incidents'),
                              child: Text(
                                '${colony.incidentHistory.last.kind}: ${colony.incidentHistory.last.outcome} · ${colony.incidentHistory.last.damage} damage · inspect recovery',
                                style: const TextStyle(
                                  color: Color(0xffd4c795),
                                  fontSize: 11,
                                ),
                              ),
                            ),
                          ),
                        ),
                      ),
                    if (introduction)
                      Positioned(
                        top: 36,
                        left: 16,
                        child: SizedBox(
                          width: 302,
                          child: Card(
                            color: const Color(0xf0202a25),
                            child: Padding(
                              padding: const EdgeInsets.all(12),
                              child: Column(
                                mainAxisSize: MainAxisSize.min,
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'A place to begin',
                                    style: TextStyle(color: Color(0xffead39b)),
                                  ),
                                  const SizedBox(height: 6),
                                  const Text(
                                    'Drag cottages beside paths to welcome people. Add workplaces and parks; watch coins, mood and neighbours. Clear trees to expand, bridge water, and build around rock. B opens every tool.',
                                  ),
                                  TextButton(
                                    onPressed: () =>
                                        setState(() => introduction = false),
                                    child: const Text('Start building'),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ),
                    if (colony.special?.active(colony.clock) == true)
                      Positioned(
                        left: 24,
                        top: 40,
                        right: game.inspection != null ? 312 : 24,
                        child: Align(
                          alignment: Alignment.centerLeft,
                          child: Tooltip(
                            message: colony.special!.effect,
                            child: TextButton(
                              onPressed: () {
                                cancelGesture();
                                game.clearInspection();
                                game.inspectedEvent = true;
                                setState(() {});
                              },
                              child: Text(
                                '${colony.special!.title} · ${(colony.special!.ends - colony.clock).ceil()}s left · ${colony.clock - colony.special!.starts < 5 ? colony.special!.shortEffect : 'inspect'}',
                                style: const TextStyle(
                                  fontSize: 11,
                                  color: Color(0xffe4d29b),
                                  backgroundColor: Color(0xdd202a25),
                                ),
                              ),
                            ),
                          ),
                        ),
                      ),
                    if (game.inspection != null || conversation != null)
                      Positioned(
                        right: 16,
                        top: 14,
                        width: 300,
                        child: inspectionPanel(),
                      ),
                    if (notice.isNotEmpty)
                      Positioned(
                        bottom: 16,
                        left: 24,
                        right: 180,
                        child: IgnorePointer(
                          child: Text(
                            notice,
                            style: const TextStyle(
                              color: Color(0xfff3d9a0),
                              fontSize: 12,
                              backgroundColor: Color(0xee202a25),
                            ),
                          ),
                        ),
                      ),
                  ],
                ),
              ),
            ),
            Container(
              padding: const EdgeInsets.fromLTRB(18, 10, 18, 9),
              decoration: const BoxDecoration(
                color: Color(0xff202a25),
                border: Border(top: BorderSide(color: Color(0xff475441))),
              ),
              child: Column(
                children: [
                  Row(
                    children: [
                      for (final t in Tool.values.take(5))
                        Padding(
                          padding: const EdgeInsets.only(right: 8),
                          child: Tooltip(
                            message: '${t.label} · ${t.purpose}',
                            child: OutlinedButton(
                              style: OutlinedButton.styleFrom(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 10,
                                  vertical: 9,
                                ),
                                foregroundColor: t == tool
                                    ? const Color(0xfff0dba0)
                                    : const Color(0xffafbaa3),
                                backgroundColor: t == tool
                                    ? const Color(0xff3b4937)
                                    : Colors.transparent,
                                side: BorderSide(
                                  color: t == tool
                                      ? const Color(0xffad9f6a)
                                      : const Color(0xff536047),
                                ),
                              ),
                              onPressed: () => select(t),
                              child: Row(
                                children: [
                                  BuildThumbnail(t, game.art, size: 16),
                                  const SizedBox(width: 6),
                                  Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        '${t.index + 1} ${t.label}',
                                        style: const TextStyle(fontSize: 11),
                                      ),
                                      Text(
                                        session.unlimited ||
                                                Rules.cost(t.name) == 0
                                            ? 'Free'
                                            : '${Rules.cost(t.name) ~/ 1000} coins',
                                        style: const TextStyle(fontSize: 9),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      PopupMenuButton<Tool>(
                        key: catalogueKey,
                        constraints: const BoxConstraints(
                          minWidth: 360,
                          maxWidth: 360,
                        ),
                        tooltip: 'Build catalogue (B)',
                        onOpened: () {
                          cancelGesture();
                          setState(() => menuOpen = true);
                          game.menuVisible = true;
                        },
                        onCanceled: () {
                          setState(() => menuOpen = false);
                          game.menuVisible = false;
                          focus.requestFocus();
                        },
                        onSelected: (t) {
                          menuOpen = false;
                          game.menuVisible = false;
                          select(t);
                        },
                        itemBuilder: (context) => [
                          const PopupMenuItem<Tool>(
                            enabled: false,
                            height: 34,
                            child: Text(
                              'Buildings need an adjoining connected path.',
                              style: TextStyle(fontSize: 10),
                            ),
                          ),
                          for (final group in <String, List<Tool>>{
                            'Homes': [Tool.cottage],
                            'Work': [
                              Tool.garden,
                              Tool.workshop,
                              Tool.market,
                              Tool.inn,
                            ],
                            'Preparedness': [
                              Tool.firehouse,
                              Tool.watchpost,
                              Tool.refuge,
                              Tool.repair,
                            ],
                            'Social': [Tool.green, Tool.hall],
                            'Roads': [
                              Tool.path,
                              Tool.stoneRoad,
                              Tool.gardenPath,
                            ],
                            'Landscape': [Tool.clear, Tool.plant, Tool.bridge],
                          }.entries) ...[
                            PopupMenuItem<Tool>(
                              enabled: false,
                              height: 26,
                              child: Text(
                                group.key,
                                style: const TextStyle(
                                  color: Color(0xffd8c78f),
                                  fontSize: 11,
                                ),
                              ),
                            ),
                            for (final t in group.value)
                              PopupMenuItem<Tool>(
                                value: t,
                                height: 68,
                                child: SizedBox(
                                  width: 330,
                                  child: Row(
                                    children: [
                                      BuildThumbnail(t, game.art),
                                      const SizedBox(width: 12),
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment:
                                              CrossAxisAlignment.start,
                                          mainAxisSize: MainAxisSize.min,
                                          children: [
                                            Text(
                                              '${t.index > 9 ? '' : '${t.index == 9 ? 0 : t.index + 1} '}${t.label} · ${t == Tool.repair
                                                  ? '1–5 coins/building'
                                                  : Rules.cost(t.name) == 0
                                                  ? 'Free'
                                                  : '${Rules.coins(Rules.cost(t.name))} coins${t == Tool.bridge ? '/water cell' : ''}'}',
                                              style: TextStyle(
                                                color: tool == t
                                                    ? const Color(0xffead39b)
                                                    : null,
                                              ),
                                            ),
                                            Text(
                                              t.purpose,
                                              style: const TextStyle(
                                                fontSize: 10,
                                                color: Color(0xffb9c4ad),
                                              ),
                                            ),
                                          ],
                                        ),
                                      ),
                                      if (tool == t)
                                        const Icon(Icons.check, size: 14),
                                    ],
                                  ),
                                ),
                              ),
                          ],
                        ],
                        child: Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 8,
                            vertical: 10,
                          ),
                          decoration: BoxDecoration(
                            border: Border.all(color: const Color(0xff7a8760)),
                            borderRadius: BorderRadius.circular(18),
                          ),
                          child: Row(
                            children: [
                              const Icon(Icons.apps, size: 16),
                              const SizedBox(width: 5),
                              Text(
                                tool.index >= 5
                                    ? (tool == Tool.hall ? 'Hall' : tool.label)
                                    : 'Build…',
                                style: const TextStyle(fontSize: 11),
                              ),
                            ],
                          ),
                        ),
                      ),
                      const Spacer(),
                      IconButton(
                        tooltip: 'Zoom out (−)',
                        constraints: const BoxConstraints.tightFor(
                          width: 32,
                          height: 40,
                        ),
                        padding: EdgeInsets.zero,
                        onPressed: () => cameraAction(() => game.zoom(-1)),
                        icon: const Icon(Icons.remove, size: 17),
                      ),
                      IconButton(
                        tooltip: 'Return to lane (H)',
                        constraints: const BoxConstraints.tightFor(
                          width: 32,
                          height: 40,
                        ),
                        padding: EdgeInsets.zero,
                        onPressed: () => cameraAction(game.home),
                        icon: const Icon(Icons.center_focus_weak, size: 17),
                      ),
                      IconButton(
                        tooltip: 'Zoom in (+)',
                        constraints: const BoxConstraints.tightFor(
                          width: 32,
                          height: 40,
                        ),
                        padding: EdgeInsets.zero,
                        onPressed: () => cameraAction(() => game.zoom(1)),
                        icon: const Icon(Icons.add, size: 17),
                      ),
                      button(
                        'Undo',
                        'Undo (Ctrl/⌘ Z)',
                        session.canUndo ? () => history(false) : null,
                        icon: const Icon(Icons.undo, size: 17),
                      ),
                      button(
                        'Redo',
                        'Redo (Ctrl/⌘ Shift Z)',
                        session.canRedo ? () => history(true) : null,
                        icon: const Icon(Icons.redo, size: 17),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          hint,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            fontSize: 11,
                            color: preview?.valid == false
                                ? const Color(0xffdca487)
                                : const Color(0xffb4c3a5),
                          ),
                        ),
                      ),
                      const Text(
                        'Space + drag · pan   Scroll · zoom',
                        style: TextStyle(
                          fontSize: 10,
                          color: Color(0xff94a28b),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  void dispose() {
    browser.unwatchActivity();
    HardwareKeyboard.instance.removeHandler(hardwareKey);
    focus.dispose();
    super.dispose();
  }
}
