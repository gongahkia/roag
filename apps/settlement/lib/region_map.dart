import 'dart:async';
import 'dart:math' as math;

import 'package:flame/components.dart' show Sprite, Vector2;
import 'package:material_ui/material_ui.dart';
import 'package:flutter/services.dart';

import 'region.dart';
import 'rules.dart';
import 'model.dart';

class RegionMapDialog extends StatefulWidget {
  final SettlementRegion region;
  final Map<String, Sprite> art;
  final VoidCallback changed;
  final void Function(String) visit;
  const RegionMapDialog({
    super.key,
    required this.region,
    required this.art,
    required this.changed,
    required this.visit,
  });
  @override
  State<RegionMapDialog> createState() => _RegionMapDialogState();
}

class _RegionMapDialogState extends State<RegionMapDialog> {
  SettlementRegion get region => widget.region;
  late String selected = region.activeId;
  Timer? refresh;
  String notice = '';
  @override
  void initState() {
    super.initState();
    refresh = Timer.periodic(const Duration(milliseconds: 250), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    refresh?.cancel();
    super.dispose();
  }

  void changed(bool accepted, String success) {
    if (!accepted) {
      setState(
        () => notice = 'Conditions changed; review the current requirements.',
      );
      return;
    }
    widget.changed();
    setState(() => notice = success);
  }

  Future<void> found(RegionalSite site) async {
    var name = '${site.environment.label.split(' ').first} Hearth';
    final confirmed = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Found a new settlement?'),
        content: SizedBox(
          width: 360,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '${site.environment.label}\n${site.environment.description}',
              ),
              const SizedBox(height: 12),
              Text(
                region.freeBuild
                    ? 'Free Build expedition · new town receives 50 coins.'
                    : '60 coins from ${region.capital.name}: 50 become the new town treasury; 10 prepare the expedition.',
              ),
              const SizedBox(height: 12),
              TextFormField(
                initialValue: name,
                maxLength: 32,
                decoration: const InputDecoration(labelText: 'Settlement name'),
                onChanged: (v) => name = v,
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, name),
            child: const Text('Confirm founding'),
          ),
        ],
      ),
    );
    if (confirmed == null || !mounted) return;
    try {
      changed(
        region.found(site.id, name: confirmed),
        'Settlement founded. Visit it to start building.',
      );
    } on FormatException catch (e) {
      setState(() => notice = e.message);
    }
  }

  Future<void> rename(RegionalTown town) async {
    var name = town.name;
    final confirmed = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Name this settlement'),
        content: TextFormField(
          initialValue: name,
          maxLength: 32,
          decoration: const InputDecoration(labelText: 'Settlement name'),
          onChanged: (v) => name = v,
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, name),
            child: const Text('Rename'),
          ),
        ],
      ),
    );
    if (confirmed == null || !mounted) return;
    try {
      changed(
        region.rename(town.siteId, confirmed),
        'Name updated; settlement and route identities are unchanged.',
      );
    } on FormatException catch (e) {
      setState(() => notice = e.message);
    }
  }

  Future<void> project(String id, RegionalTown town) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Complete regional project?'),
        content: Text(region.projectRules(id)),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Complete project'),
          ),
        ],
      ),
    );
    if (confirmed == true && mounted) {
      changed(
        region.completeProject(id, town.siteId),
        'Regional project completed.',
      );
    }
  }

  Widget heading(String text) => Padding(
    padding: const EdgeInsets.only(top: 18, bottom: 8),
    child: Text(
      text,
      style: const TextStyle(color: Color(0xffead39b), fontSize: 13),
    ),
  );
  Widget small(String text) => Text(
    text,
    style: const TextStyle(fontSize: 11, height: 1.5, color: Color(0xffb9c4ad)),
  );
  Widget inspector() {
    final site = region.geography.site(selected),
        known = region.discovered.contains(selected),
        town = region.towns[selected];
    final discovery = region.discoveryIssue(selected),
        founding = region.foundingIssue(selected);
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            town?.name ?? (known ? site.environment.label : 'Uncharted trail'),
            style: const TextStyle(fontSize: 17, color: Color(0xffeadfbd)),
          ),
          const SizedBox(height: 8),
          if (!known) ...[
            small(
              'A neighbouring trail leads here. Explore to learn its environment; the land itself remains unseen until settlement.',
            ),
            const SizedBox(height: 12),
            FilledButton(
              onPressed: discovery == null
                  ? () => changed(
                      region.discover(selected),
                      'Location discovered.',
                    )
                  : null,
              child: Text(
                region.freeBuild
                    ? 'Discover location · Free'
                    : 'Discover location · 2 coins',
              ),
            ),
            if (discovery != null) small(discovery),
          ] else ...[
            small(site.environment.description),
            const SizedBox(height: 8),
            if (town == null) ...[
              small(
                'A protected arrival lane and viable opening are included. Houses, work, social spaces and landscape tools remain available.',
              ),
              const SizedBox(height: 12),
              FilledButton(
                onPressed: founding == null ? () => found(site) : null,
                child: Text(
                  region.freeBuild
                      ? 'Found settlement · Free'
                      : 'Found settlement · 60 coins',
                ),
              ),
              if (founding != null) small(founding),
            ] else ...[
              small(
                '${town.stage.label} · ${town.colony.people.length} people · ${Rules.coins(town.colony.session.money)} coins',
              ),
              const SizedBox(height: 10),
              FilledButton(
                onPressed: () => {
                  Navigator.pop(context),
                  widget.visit(town.siteId),
                },
                child: Text(
                  town.siteId == region.activeId
                      ? 'Return to this settlement'
                      : 'Visit settlement',
                ),
              ),
              TextButton(
                onPressed: () => rename(town),
                child: const Text('Rename settlement'),
              ),
              heading('Development'),
              for (final (met, label)
                  in (town.stage == DevelopmentStage.hamlet
                      ? [
                          for (var i = 0; i < 3; i++)
                            (
                              town.villageRequirements[i],
                              [
                                'Four residents',
                                '3 coins earned from real work',
                                'Staffed, connected Market',
                              ][i],
                            ),
                        ]
                      : [
                          for (var i = 0; i < 4; i++)
                            (
                              town.townRequirements[i],
                              [
                                'Eight residents',
                                '120 seconds of actual work',
                                'Connected Park or Hall',
                                'A delivery or 20 coins of work income',
                              ][i],
                            ),
                        ]))
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 3),
                  child: small('${met ? '✓' : '○'} $label'),
                ),
              if (town.stage.index >= DevelopmentStage.town.index)
                small(
                  'Regional Centre: 12 people, two reliable partners, a standing Monument and trained protection. Recognition never removes existing abilities.',
                ),
              heading('Supply'),
              for (final good in TradeGood.values)
                Padding(
                  padding: const EdgeInsets.only(bottom: 7),
                  child: small(
                    '${good.label}: ${region.goodsAvailable(town, good) >= 6
                        ? 'Producing surplus'
                        : region.goodsAvailable(town, good) > 0
                        ? 'Balanced'
                        : 'In need of supply'}',
                  ),
                ),
              ExpansionTile(
                title: const Text(
                  'Inspect production & accounts',
                  style: TextStyle(fontSize: 11),
                ),
                tilePadding: EdgeInsets.zero,
                children: [
                  for (final good in TradeGood.values)
                    small(
                      '${good.label}: ${town.stock[good]} local · ${town.supplied[good]} imported · ${town.produced[good]} genuinely produced',
                    ),
                  small(
                    'Work income: ${Rules.coins(town.colony.workEarned)} coins. Imports paid: ${Rules.coins(town.importsSpent)}; exports received: ${Rules.coins(town.exportsEarned)}. These trade payments transfer existing coins.',
                  ),
                  small(
                    'Farms supply Provisions. Workshops make Wares. Lumber Camps need standing woodland and real staff. Imported goods are consumed locally, not sold in a loop.',
                  ),
                ],
              ),
              heading('Automatic trade'),
              if (town.siteId != region.activeId) ...[
                TextButton(
                  onPressed:
                      region.connectionIssue(region.activeId, town.siteId) ==
                          null
                      ? () => changed(
                          region.connectTrade(region.activeId, town.siteId),
                          'Automatic trade relationship established.',
                        )
                      : null,
                  child: Text(
                    'Connect ${region.active.name} · ${region.freeBuild ? 'Free' : '3 coins'}',
                  ),
                ),
                if (region.connectionIssue(region.activeId, town.siteId) !=
                    null)
                  small(region.connectionIssue(region.activeId, town.siteId)!),
              ] else
                small(
                  'Select another founded town on the map to connect it with this settlement.',
                ),
              for (final route in region.tradeRoutes.values.where(
                (r) => r.a == selected || r.b == selected,
              ))
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(10),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${region.towns[route.a]!.name} ↔ ${region.towns[route.b]!.name}',
                          style: const TextStyle(
                            fontSize: 12,
                            color: Color(0xffead39b),
                          ),
                        ),
                        const SizedBox(height: 6),
                        small(region.tradeStatus(route)),
                        small(
                          'Regional road journey: ${region.journey(route.a, route.b)?.ceil().toString() ?? 'interrupted'} seconds, plus actual Market approaches. ${route.deliveries} deliveries.',
                        ),
                        for (final shipment in region.shipments.values.where(
                          (s) => s.route == route.id && !s.terminal,
                        ))
                          small(
                            '${shipment.id} · ${shipment.quantity} ${shipment.good.label} · ${shipment.state.name}${shipment.returning ? ' (returning)' : ''}',
                          ),
                        TextButton(
                          onPressed: () {
                            region.enableTrade(route.id, !route.enabled);
                            widget.changed();
                            setState(() {});
                          },
                          child: Text(
                            route.enabled
                                ? 'Pause new shipments'
                                : 'Resume automatic trade',
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              heading('Regional projects'),
              for (final (id, label) in [
                ('network', 'Trade Network'),
                ('landmark', 'Wayfarer Monument'),
                ('resilient', 'Resilient Region'),
              ]) ...[
                Text(
                  label,
                  style: const TextStyle(
                    fontSize: 12,
                    color: Color(0xffead39b),
                  ),
                ),
                small(region.projectRules(id)),
                if (region.projects.contains(id))
                  small('✓ Completed · saved regional recognition')
                else ...[
                  small(
                    region.projectIssue(id, town.siteId) ?? 'Ready to complete',
                  ),
                  TextButton(
                    onPressed: region.projectIssue(id, town.siteId) == null
                        ? () => project(id, town)
                        : null,
                    child: Text('Complete $label'),
                  ),
                ],
                const SizedBox(height: 10),
              ],
            ],
          ],
          heading('Regional history'),
          for (final fact in region.history.reversed.take(8))
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: small(
                'Day ${((fact['at'] as num) / Rules.day).floor() + 1} · ${fact['text']}',
              ),
            ),
          if (region.history.isEmpty)
            small(
              'Discovery, founding, deliveries and regional events will leave a shared history.',
            ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final size = MediaQuery.sizeOf(context);
    return Dialog(
      insetPadding: const EdgeInsets.all(12),
      child: SizedBox(
        width: math.min(1120, size.width - 24),
        height: math.min(800, size.height - 24),
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 10, 8, 8),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Region',
                          style: TextStyle(
                            fontSize: 18,
                            color: Color(0xffeadfbd),
                          ),
                        ),
                        Text(
                          '${region.towns.length} settlements · ${Rules.coins(region.treasury)} available coins · Day ${region.capital.colony.day}',
                          style: const TextStyle(fontSize: 10),
                        ),
                      ],
                    ),
                  ),
                  for (final speed in [0, 1, 4, 16])
                    TextButton(
                      onPressed: () {
                        region.setSpeed(speed);
                        widget.changed();
                        setState(() {});
                      },
                      child: Text(
                        speed == 0 ? 'Ⅱ' : '$speed×',
                        style: TextStyle(
                          fontSize: 11,
                          color: region.speed == speed
                              ? const Color(0xffead39b)
                              : const Color(0xff8b9b83),
                        ),
                      ),
                    ),
                  IconButton(
                    tooltip: 'Back to settlement',
                    onPressed: () => Navigator.pop(context),
                    icon: const Icon(Icons.close),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),
            Expanded(
              child: LayoutBuilder(
                builder: (context, constraints) {
                  final map = RegionalMap(
                    region: region,
                    art: widget.art,
                    selected: selected,
                    onSelect: (id) => setState(() => selected = id),
                  );
                  return constraints.maxWidth >= 720
                      ? Row(
                          children: [
                            Expanded(child: map),
                            const VerticalDivider(width: 1),
                            SizedBox(width: 320, child: inspector()),
                          ],
                        )
                      : Column(
                          children: [
                            SizedBox(
                              height: math.min(
                                240,
                                constraints.maxHeight * .42,
                              ),
                              child: map,
                            ),
                            const Divider(height: 1),
                            Expanded(child: inspector()),
                          ],
                        );
                },
              ),
            ),
            if (notice.isNotEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 8,
                ),
                child: small(notice),
              ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      'Regional seed: ${region.geography.seedText}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontSize: 10),
                    ),
                  ),
                  TextButton(
                    onPressed: () => Clipboard.setData(
                      ClipboardData(text: region.geography.seedText),
                    ),
                    child: const Text('Copy seed'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class RegionalMap extends StatelessWidget {
  final SettlementRegion region;
  final Map<String, Sprite> art;
  final String selected;
  final void Function(String) onSelect;
  const RegionalMap({
    super.key,
    required this.region,
    required this.art,
    required this.selected,
    required this.onSelect,
  });
  Set<String> get visible => {
    ...region.discovered,
    for (final s in region.discovered)
      ...region.geography.neighbours(s).map((e) => e.other(s)),
  };
  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, c) {
      final size = Size(c.maxWidth, c.maxHeight);
      return Stack(
        children: [
          Positioned.fill(
            child: CustomPaint(
              painter: _RegionalPainter(region, art, selected, visible),
            ),
          ),
          for (final site in region.geography.sites.where(
            (s) => visible.contains(s.id),
          ))
            Positioned(
              left: 32 + site.x / 1000 * (size.width - 64) - 22,
              top: 32 + site.y / 1000 * (size.height - 80) - 22,
              width: 44,
              height: 44,
              child: Semantics(
                button: true,
                selected: site.id == selected,
                label:
                    region.towns[site.id]?.name ??
                    (region.discovered.contains(site.id)
                        ? site.environment.label
                        : 'Uncharted ${site.id}'),
                child: Tooltip(
                  message:
                      region.towns[site.id]?.name ??
                      (region.discovered.contains(site.id)
                          ? site.environment.label
                          : 'Uncharted trail'),
                  child: InkWell(
                    onTap: () => onSelect(site.id),
                    child: const SizedBox.expand(),
                  ),
                ),
              ),
            ),
          const Positioned(
            left: 12,
            top: 12,
            child: Text(
              'Select a site · Houses are founded towns',
              style: TextStyle(fontSize: 10, color: Color(0xffa4b39d)),
            ),
          ),
        ],
      );
    },
  );
}

class _RegionalPainter extends CustomPainter {
  final SettlementRegion region;
  final Map<String, Sprite> art;
  final String selected;
  final Set<String> visible;
  _RegionalPainter(this.region, this.art, this.selected, this.visible);
  Offset at(RegionalSite s, Size size) => Offset(
    32 + s.x / 1000 * (size.width - 64),
    32 + s.y / 1000 * (size.height - 80),
  );
  void label(
    Canvas canvas,
    String text,
    Offset point,
    Color color, {
    double size = 9,
  }) {
    final p = TextPainter(
      text: TextSpan(
        text: text,
        style: TextStyle(
          fontFamily: 'SourceCode',
          fontSize: size,
          color: color,
        ),
      ),
      textDirection: TextDirection.ltr,
      maxLines: 1,
      ellipsis: '…',
    )..layout(maxWidth: 120);
    p.paint(canvas, point - Offset(p.width / 2, 0));
  }

  @override
  void paint(Canvas canvas, Size size) {
    canvas.drawRect(
      Offset.zero & size,
      Paint()..color = const Color(0xff233329),
    );
    final closed = region.closedConnections;
    for (final edge in region.geography.connections.where(
      (e) => visible.contains(e.a) && visible.contains(e.b),
    )) {
      final known =
          region.discovered.contains(edge.a) &&
          region.discovered.contains(edge.b);
      final p = Paint()
        ..color = closed.contains(edge.id)
            ? const Color(0xffbb826c)
            : known
            ? const Color(0xff738262)
            : const Color(0xff3f5141)
        ..strokeWidth = known ? 3 : 1
        ..isAntiAlias = false;
      canvas.drawLine(
        at(region.geography.site(edge.a), size),
        at(region.geography.site(edge.b), size),
        p,
      );
    }
    for (final route in region.tradeRoutes.values.where((r) => r.enabled)) {
      final path = region.geography.path(
        route.a,
        route.b,
        known: region.discovered,
      );
      if (path == null) continue;
      var current = route.a;
      for (final id in path) {
        final edge = region.geography.connections.firstWhere((e) => e.id == id),
            next = edge.other(current);
        canvas.drawLine(
          at(region.geography.site(current), size),
          at(region.geography.site(next), size),
          Paint()
            ..color = const Color(0xffc5af76)
            ..strokeWidth = 1
            ..isAntiAlias = false,
        );
        current = next;
      }
    }
    for (final site in region.geography.sites.where(
      (s) => visible.contains(s.id),
    )) {
      final point = at(site, size),
          known = region.discovered.contains(site.id),
          town = region.towns[site.id];
      canvas.drawRect(
        Rect.fromCenter(center: point, width: 30, height: 30),
        Paint()
          ..color = site.id == selected
              ? const Color(0xff536448)
              : const Color(0xff2a3e30),
      );
      if (site.id == selected) {
        canvas.drawRect(
          Rect.fromCenter(center: point, width: 32, height: 32),
          Paint()
            ..color = const Color(0xffe0c78b)
            ..style = PaintingStyle.stroke
            ..strokeWidth = 1
            ..isAntiAlias = false,
        );
      }
      final frame = town != null
          ? 'cottage0'
          : !known
          ? null
          : switch (site.environment) {
              EnvironmentPreset.pineUplands => 'spruce',
              EnvironmentPreset.woodlands => 'oak',
              EnvironmentPreset.highlands ||
              EnvironmentPreset.frontier => 'boulder',
              EnvironmentPreset.saltCoast ||
              EnvironmentPreset.river ||
              EnvironmentPreset.wetlands => 'water',
              _ => 'vegetables',
            };
      if (frame == null) {
        label(
          canvas,
          '?',
          point - const Offset(0, 9),
          const Color(0xff84937c),
          size: 16,
        );
      } else {
        art[frame]?.render(
          canvas,
          position: Vector2(point.dx - 12, point.dy - 12),
          size: Vector2.all(24),
          overridePaint: Paint()
            ..filterQuality = FilterQuality.none
            ..colorFilter = ColorFilter.mode(
              town != null ? const Color(0xffd4ba85) : const Color(0xff94b287),
              BlendMode.srcIn,
            ),
        );
      }
      label(
        canvas,
        town?.name ?? (known ? site.environment.label : 'Uncharted'),
        point + const Offset(0, 20),
        known ? const Color(0xffd2d6bc) : const Color(0xff7e8e78),
      );
    }
    for (final s in region.shipments.values.where((s) => !s.terminal)) {
      var point = at(region.geography.site(s.node), size);
      if (s.state == ShipmentState.transit && s.leg < s.path.length) {
        final edge = region.geography.connections.firstWhere(
          (e) => e.id == s.path[s.leg],
        );
        point = Offset.lerp(
          point,
          at(region.geography.site(edge.other(s.node)), size),
          (s.progress / edge.seconds).clamp(0, 1),
        )!;
      }
      art['merchant']?.render(
        canvas,
        position: Vector2(point.dx - 6, point.dy - 15),
        size: Vector2.all(12),
        overridePaint: Paint()
          ..filterQuality = FilterQuality.none
          ..colorFilter = const ColorFilter.mode(
            Color(0xffefda9e),
            BlendMode.srcIn,
          ),
      );
    }
  }

  @override
  bool shouldRepaint(covariant _RegionalPainter old) => true;
}
