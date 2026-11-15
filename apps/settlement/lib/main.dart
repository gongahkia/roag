import 'package:flame/game.dart';
import 'package:flutter/gestures.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter/services.dart';

import 'browser.dart' as browser;
import 'model.dart';
import 'settlement_game.dart';

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
  late final LandscapeSession session;
  late final SettlementGame game;
  final focus = FocusNode();
  Tool tool = Tool.cottage;
  Placement? preview;
  String saveStatus = 'Autosave in this browser';
  String notice = '';
  int noticeId = 0;
  bool muted = false, space = false, menuOpen = false;
  Offset? last;
  int? pointer;
  bool panning = false, reportedInvalid = false;

  @override
  void initState() {
    super.initState();
    HardwareKeyboard.instance.addHandler(hardwareKey);
    Landscape landscape = Landscape.create();
    try {
      final saved = browser.loadLocal();
      if (saved.isNotEmpty) {
        landscape = Landscape.decode(saved);
        saveStatus = 'Saved in this browser';
      }
    } catch (_) {
      saveStatus = 'Saved landscape could not be opened';
      notice = 'Starting a temporary landscape. Export it before leaving; your saved copy is untouched.';
    }
    session = LandscapeSession(landscape);
    WidgetsBinding.instance.addPostFrameCallback((_) => browser.hideLoading());
    game = SettlementGame(session, (p) {
      if (mounted) setState(() => preview = p);
    });
    browser.watchActivity((active) {
      game.suspended = !active;
      if (!active && mounted) cancelGesture();
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
      session.persist(session.snapshot(), browser.saveLocal);
      saveStatus = 'Saved in this browser';
      return true;
    } catch (_) {
      saveStatus = 'Not saved · export a copy';
      tell(
        'Browser storage is unavailable or full. Export a portable copy from the menu.',
      );
      return false;
    }
  }

  void select(Tool next) {
    cancelGesture();
    setState(() {
      tool = next;
      game.tool = next;
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
    pointer = e.pointer;
    last = e.localPosition;
    panning = pan;
    reportedInvalid = false;
    if (!pan) {
      session.beginStroke(tool, game.pick(e.localPosition));
      paintAt(e.localPosition);
    }
  }

  void pointerMove(PointerMoveEvent e) {
    if (pointer != e.pointer || last == null) return;
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
            : stroke.tool == Tool.path
            ? 'path'
            : 'place',
      );
      if (saved) {
        tell(
          '${stroke.changed.length} ${stroke.tool == Tool.remove
              ? 'pieces removed'
              : stroke.tool == Tool.path
              ? 'path cells placed'
              : 'cottages placed'}',
        );
      }
    } else if (stroke?.firstInvalid != null) {
      browser.sound('invalid');
    }
    if (mounted) setState(() {});
  }

  void cancelGesture() {
    session.cancelStroke();
    pointer = null;
    last = null;
    panning = false;
    game.clearPreview();
  }

  void cameraAction(void Function() action) {
    cancelGesture();
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
    try {
      if (action == 'export') {
        browser.exportFile(session.snapshot().text);
        tell('Exported a portable landscape copy.');
      } else if (action == 'import') {
        final text = await browser.importFile();
        if (text.isNotEmpty && mounted) {
          session.importText(text);
          game.reconcile(restored: true, reset: true);
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
          session.replace(Landscape.create());
          game.reconcile(restored: true, reset: true);
          final saved = save();
          game.home();
          if (saved) tell('A fresh landscape.');
        }
      } else if (action == 'help') {
        await showDialog<void>(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text('A small place, shaped by you'),
            content: const SingleChildScrollView(
              child: Text(
                '1 Cottage · 2 Path · 3 Remove\nClick or drag to build. Release to commit one stroke. Escape cancels; Undo during a stroke cancels it first.\n\n'
                'Paths must join the arrival lane. Cottages use one adjoining connected path, preferring south, east, west, then north. Trees block building; small plants can be replaced.\n\n'
                'Each cottage has a resident. New arrivals walk from the lane; residents visit reachable homes and wait when disconnected.\n\n'
                'Removed cottages leave pads. Removed roads leave scars. Neither provides access. An amber mark means No access; reconnect the lane to clear it.\n\n'
                'Drag with the right/middle button or Space + drag to pan. Scroll or +/− to zoom. H returns to the lane. Ctrl/⌘ Z undoes; Shift Z redoes.\n\n'
                'Automatic saves belong to this browser and site. Export is your portable copy. No ROAG dungeon saves are used.',
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
        focus.requestFocus();
      }
    }
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
    final scene = session.view;
    final houses = scene.cells
        .where((c) => scene.at(c).cottage != null)
        .toList();
    final lost = houses.where((c) => scene.access(c) == null).length;
    String hint =
        preview?.message ??
        switch (tool) {
          Tool.cottage => 'A cottage beside a connected path.',
          Tool.path => 'Extend the lane, one cell at a time.',
          Tool.remove => 'Leave a little history behind.',
        };
    if (session.stroke != null) {
      hint =
          'Release to commit ${session.stroke!.changed.length} pieces · Escape cancels';
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
                  const Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'ROAG / A LANDSCAPE TO KEEP',
                        style: TextStyle(
                          fontSize: 10,
                          color: Color(0xff9bb18a),
                          letterSpacing: 1.4,
                        ),
                      ),
                      SizedBox(height: 3),
                      Text(
                        'A place to return',
                        style: TextStyle(
                          fontSize: 21,
                          color: Color(0xffeadfbd),
                        ),
                      ),
                    ],
                  ),
                  const Spacer(),
                  Text(
                    '${houses.length} ${houses.length == 1 ? 'cottage' : 'cottages'}',
                    style: const TextStyle(color: Color(0xffbec8ae)),
                  ),
                  if (lost > 0)
                    Padding(
                      padding: const EdgeInsets.only(left: 12),
                      child: Text(
                        '$lost No access',
                        style: const TextStyle(color: Color(0xffdfc281)),
                      ),
                    ),
                  const SizedBox(width: 16),
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
                      space = false;
                      cancelGesture();
                    },
                    onCanceled: () => setState(() => menuOpen = false),
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
                          // a hidden tab also emits an exit at its last in-canvas
                          // position; only a physical boundary crossing commits.
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
                          'Drag a lane. Give it life.',
                          style: TextStyle(
                            color: Color(0xffc3c9aa),
                            fontSize: 11,
                          ),
                        ),
                      ),
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
                      for (final t in Tool.values)
                        Padding(
                          padding: const EdgeInsets.only(right: 8),
                          child: OutlinedButton(
                            style: OutlinedButton.styleFrom(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 16,
                                vertical: 15,
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
                                Image.asset(
                                  'assets/images/${['hammer', 'grid', 'clear'][t.index]}.png',
                                  width: 18,
                                  height: 18,
                                  filterQuality: FilterQuality.none,
                                  color: t == tool
                                      ? const Color(0xffe9d59d)
                                      : const Color(0xff9fad97),
                                ),
                                const SizedBox(width: 9),
                                Text(
                                  '${t.index + 1}  ${['Cottage', 'Path', 'Remove'][t.index]}',
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
