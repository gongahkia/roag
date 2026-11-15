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
    colony = Colony(LandscapeSession(Landscape.create()));
    try {
      final saved = browser.loadLocal();
      if (saved.isNotEmpty) {
        colony = Colony.decode(saved);
        if (jsonDecode(saved)['schema'] != 3) {
          try {
            browser.backupLegacy(saved);
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
    if (!reportedInvalid && stroke.firstInvalid != null) {
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
          '${stroke.changed.length} $noun${stroke.changed.length == 1 ? '' : 's'} ${stroke.tool == Tool.remove ? 'removed' : 'placed'} · ${Rules.coins(stroke.price)} coins',
        );
      }
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
      if (action == 'export') {
        browser.exportFile(colony.snapshot().text);
        tell('Exported a portable landscape copy.');
      } else if (action == 'import') {
        final text = await browser.importFile();
        if (text.isNotEmpty && mounted) {
          final imported = Colony.decode(text);
          replaceColony(imported);
          final saved = save();
          game.home();
          if (saved) tell('Landscape imported.');
        }
      } else if (action == 'new') {
        final confirmed = await showDialog<bool>(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text('Start a new landscape?'),
            content: const Text(
              'This replaces your current landscape and undo history. Export a copy first if you want to return to it.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Keep building'),
              ),
              FilledButton(
                onPressed: () => Navigator.pop(context, true),
                child: const Text('New landscape'),
              ),
            ],
          ),
        );
        if (confirmed == true && mounted) {
          replaceColony(Colony(LandscapeSession(Landscape.create())));
          migrationSafe = true;
          final saved = save();
          game.home();
          if (saved) tell('A fresh landscape.');
        }
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

  void replaceColony(Colony next) {
    cancelGesture();
    next.rebaseNavigation(colony.navigation.revision);
    colony = next;
    game.colony = next;
    game.resetPresentation();
    conversation = null;
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
    if (id == null || !colony.people.containsKey(id)) return;
    cancelGesture();
    setState(() {
      conversation = Conversation(
        id,
        node: civic ? 'civic' : 'hello',
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
    final person = colony.people[chat?.resident ?? game.inspectedResident];
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
                    if (person != null)
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
                    if (game.inspectedEvent && colony.civicHost != null)
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
                const Text(
                  'The town keeps living while we talk.',
                  style: TextStyle(fontSize: 9, color: Color(0xffa4b39d)),
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
                        'Productive work: ${Rules.coins(colony.workEarned)} coins. Completed Market trade: ${Rules.coins(colony.tradeEarned)} coins. Grant: ${Rules.coins(colony.grantEarned)} coins. No maintenance.',
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
                  Text(
                    'Day ${colony.day} · ${colony.phase}',
                    style: const TextStyle(fontSize: 11),
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
                        value: 'unlimited',
                        child: Text(
                          'Unlimited building: ${session.unlimited ? 'On' : 'Off'}',
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
                            'Work': [Tool.garden, Tool.workshop, Tool.market],
                            'Social': [Tool.green, Tool.hall],
                            'Roads': [
                              Tool.path,
                              Tool.stoneRoad,
                              Tool.gardenPath,
                            ],
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
                                              '${t.index == 9 ? 0 : t.index + 1} ${t.label} · ${Rules.cost(t.name) == 0 ? 'Free' : '${Rules.coins(Rules.cost(t.name))} coins'}',
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
