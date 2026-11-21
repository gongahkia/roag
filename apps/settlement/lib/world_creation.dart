import 'package:material_ui/material_ui.dart';

import 'model.dart';
import 'colony.dart';

class WorldDraft {
  final String seed;
  final EnvironmentPreset preset;
  final IncidentMode incidents;
  final ScenarioKind scenario;
  const WorldDraft(
    this.seed,
    this.preset, [
    this.incidents = IncidentMode.standard,
    this.scenario = ScenarioKind.free,
  ]);
}

// draft controls do not hold or modify the active colony.
class WorldCreationDialog extends StatefulWidget {
  final VoidCallback exportCurrent;
  final List<EnvironmentPreset>? available;
  const WorldCreationDialog({
    required this.exportCurrent,
    this.available,
    super.key,
  });
  @override
  State<WorldCreationDialog> createState() => _WorldCreationDialogState();
}

class _WorldCreationDialogState extends State<WorldCreationDialog> {
  final seed = TextEditingController(text: '202');
  EnvironmentPreset preset = EnvironmentPreset.plains;
  String? error;
  IncidentMode incidents = IncidentMode.standard;
  ScenarioKind scenario = ScenarioKind.free;
  @override
  void dispose() {
    seed.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: const Text('Create a new world'),
    content: SizedBox(
      width: 420,
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            DropdownButtonFormField<ScenarioKind>(
              initialValue: scenario,
              decoration: const InputDecoration(labelText: 'Scenario'),
              items: [
                for (final s in ScenarioKind.values)
                  DropdownMenuItem(value: s, child: Text(s.label)),
              ],
              onChanged: (s) => setState(() {
                scenario = s!;
                preset = scenario.environment ?? preset;
              }),
            ),
            const SizedBox(height: 8),
            Text(scenario.description),
            const SizedBox(height: 8),
            DropdownButtonFormField<EnvironmentPreset>(
              key: ValueKey(preset),
              initialValue: preset,
              decoration: const InputDecoration(labelText: 'Environment'),
              items: [
                for (final p
                    in widget.available ??
                        EnvironmentPreset.values.where(
                          (p) => p.index < EnvironmentPreset.pineUplands.index,
                        ))
                  DropdownMenuItem(value: p, child: Text(p.label)),
              ],
              onChanged: (p) => setState(() => preset = p!),
            ),
            const SizedBox(height: 12),
            Text(preset.description),
            const SizedBox(height: 12),
            DropdownButtonFormField<IncidentMode>(
              initialValue: incidents,
              decoration: const InputDecoration(labelText: 'Incidents'),
              items: const [
                DropdownMenuItem(
                  value: IncidentMode.standard,
                  child: Text('Standard · occasional, recoverable'),
                ),
                DropdownMenuItem(
                  value: IncidentMode.peaceful,
                  child: Text('Peaceful · no destructive incidents'),
                ),
              ],
              onChanged: (value) => setState(() => incidents = value!),
            ),
            const Text(
              'Standard begins after three days. Firehouses, Watchposts and Refuges help; repair after danger passes.',
            ),
            const SizedBox(height: 12),
            TextField(
              controller: seed,
              maxLength: 64,
              decoration: InputDecoration(
                labelText: 'World seed',
                helperText: 'Numbers or text · same seed, same region and founding land',
                errorText: error,
              ),
            ),
            TextButton(
              onPressed: () => setState(() {
                seed.text = DateTime.now().millisecondsSinceEpoch.toString();
                error = null;
              }),
              child: const Text('Generate seed'),
            ),
            const SizedBox(height: 12),
            const Text(
              'Creating replaces the entire active region and all its towns. Export a copy to return. Opening or cancelling this screen keeps your region.',
              style: TextStyle(color: Color(0xffe1b68d)),
            ),
          ],
        ),
      ),
    ),
    actions: [
      TextButton(
        onPressed: widget.exportCurrent,
        child: const Text('Export current copy'),
      ),
      TextButton(
        onPressed: () => Navigator.pop(context),
        child: const Text('Continue existing settlement'),
      ),
      FilledButton(
        onPressed: () {
          try {
            final text = canonicalSeed(seed.text);
            Navigator.pop(
              context,
              WorldDraft(text, preset, incidents, scenario),
            );
          } on FormatException catch (e) {
            setState(() => error = e.message);
          }
        },
        child: const Text('Create world'),
      ),
    ],
  );
}
