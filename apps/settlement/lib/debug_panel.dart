import 'package:material_ui/material_ui.dart';

import 'colony.dart';
import 'debug_session.dart';
import 'region.dart';
import 'rules.dart';

class SettlementDebugPanel extends StatelessWidget {
  final SettlementDebugSession session;
  final void Function(Map<String, dynamic>) command;
  final bool overlay;
  final VoidCallback toggleOverlay;
  const SettlementDebugPanel({
    super.key,
    required this.session,
    required this.command,
    required this.overlay,
    required this.toggleOverlay,
  });

  @override
  Widget build(BuildContext context) {
    final r = session.region, c = r.active.colony;
    final activity = <String, int>{};
    for (final p in c.people.values) {
      activity.update(p.activity.name, (n) => n + 1, ifAbsent: () => 1);
    }
    Widget action(String title, Map<String, dynamic> data) => OutlinedButton(
      onPressed: () => command(data),
      child: Text(title, style: const TextStyle(fontSize: 11)),
    );
    return ExpansionTile(
      initiallyExpanded: true,
      tilePadding: EdgeInsets.zero,
      title: const Text(
        'Sudo diagnostics',
        style: TextStyle(color: Color(0xffead39b), fontSize: 13),
      ),
      children: [
        Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Disposable copy. Normal autosave is paused. Exit or reload to return to your saved town.',
              style: TextStyle(fontSize: 11),
            ),
            const SizedBox(height: 8),
            Text(
              'Clock ${r.clock.toStringAsFixed(2)}s · ${r.speed}× · ${r.towns.length} towns',
            ),
            Wrap(
              spacing: 4,
              children: [
                for (final n in Rules.simulationSpeeds)
                  action(n == 0 ? 'Pause' : '$n×', {
                    'action': 'speed',
                    'value': n,
                  }),
              ],
            ),
            Wrap(
              spacing: 4,
              children: [
                action('Step 0.05s', {'action': 'step', 'ticks': 1}),
                action('Step 1s', {'action': 'step', 'ticks': 20}),
                action('Step 10s', {'action': 'step', 'ticks': 200}),
              ],
            ),
            TextButton(
              onPressed: toggleOverlay,
              child: Text('Roads & routes: ${overlay ? 'On' : 'Off'}'),
            ),
            if (overlay)
              const Text(
                'Green: connected roads. Red: disconnected/closed. Blue: routes of up to 32 visible actors.',
                style: TextStyle(fontSize: 10),
              ),
            Text(
              'Queue ${c.planning.length} · plans ${c.planned}\nSearches ${c.routeSearches} · cache ${c.navigation.hits}\nExpansions ${c.navigation.expansions} · revision ${c.navigation.revision}',
              style: const TextStyle(fontSize: 11),
            ),
            const SizedBox(height: 8),
            Text(
              activity.entries.map((e) => '${e.key}: ${e.value}').join(' · '),
              style: const TextStyle(fontSize: 10),
            ),
            ExpansionTile(
              tilePadding: EdgeInsets.zero,
              title: const Text(
                'Residents · first 12',
                style: TextStyle(fontSize: 11),
              ),
              children: [
                for (final p in c.people.values.take(12))
                  Padding(
                    padding: const EdgeInsets.only(bottom: 6),
                    child: Text(
                      '${p.name} · ${p.homeId}\n${p.activity.name} (${p.x.toStringAsFixed(1)}, ${p.y.toStringAsFixed(1)})\nWork: ${p.workId ?? 'unassigned'} · ${p.workIssue.name}',
                      style: const TextStyle(fontSize: 10),
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 8),
            action('Free building: ${r.freeBuild ? 'On' : 'Off'}', {
              'action': 'freeBuild',
              'value': !r.freeBuild,
            }),
            action('Add 100 test coins', {'action': 'coins', 'amount': 100}),
            for (final g in TradeGood.values)
              action('Add 6 test ${g.label}', {
                'action': 'goods',
                'good': g.name,
                'amount': 6,
              }),
            Text(
              'Stock: ${TradeGood.values.map((g) => '${g.label} ${r.active.stock[g]} +${r.active.supplied[g]} imported').join(' · ')}',
              style: const TextStyle(fontSize: 10),
            ),
            const SizedBox(height: 8),
            const Text(
              'Test events use the real director and legal travel. They bypass its grace timer, not target requirements.',
              style: TextStyle(fontSize: 10),
            ),
            Wrap(
              spacing: 4,
              children: [
                for (final kind in IncidentKind.values)
                  action(
                    'Test ${switch (kind) {
                      IncidentKind.fire => 'Fire',
                      IncidentKind.wildlife => 'Wildlife',
                      IncidentKind.flood => 'Flood',
                      IncidentKind.raiders => 'Brigands',
                      IncidentKind.caravan => 'Caravan',
                    }}',
                    {'action': 'incident', 'kind': kind.name},
                  ),
                action('Test Rain', {'action': 'rain'}),
              ],
            ),
            Text(
              c.incident == null
                  ? 'No active incident'
                  : '${c.incident!.title} · ${c.incident!.state}',
              style: const TextStyle(fontSize: 11),
            ),
            const SizedBox(height: 12),
          ],
        ),
      ],
    );
  }
}
