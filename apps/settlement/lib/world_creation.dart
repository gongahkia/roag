import 'package:material_ui/material_ui.dart';

import 'model.dart';
import 'colony.dart';

class WorldDraft {
  final String seed;
  final EnvironmentPreset preset;
  final IncidentMode incidents;
  const WorldDraft(
    this.seed,
    this.preset, [
    this.incidents = IncidentMode.standard,
  ]);
}

// draft controls do not hold or modify the active colony.
class WorldCreationDialog extends StatefulWidget {
  final VoidCallback exportCurrent;
  const WorldCreationDialog({required this.exportCurrent, super.key});
  @override
  State<WorldCreationDialog> createState() => _WorldCreationDialogState();
}

class _WorldCreationDialogState extends State<WorldCreationDialog> {
  final seed = TextEditingController(text: '202');
  EnvironmentPreset preset = EnvironmentPreset.plains;
  String? error;
  IncidentMode incidents = IncidentMode.standard;
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
            DropdownButtonFormField<EnvironmentPreset>(
              initialValue: preset,
              decoration: const InputDecoration(labelText: 'Environment'),
              items: [
                for (final p in EnvironmentPreset.values)
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
                helperText:
                    'Numbers or text · same seed + environment, same land',
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
              'Creating replaces the active settlement and its undo history. Export a copy to return to this town. Opening or cancelling this screen keeps it.',
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
            Navigator.pop(context, WorldDraft(text, preset, incidents));
          } on FormatException catch (e) {
            setState(() => error = e.message);
          }
        },
        child: const Text('Create world'),
      ),
    ],
  );
}
