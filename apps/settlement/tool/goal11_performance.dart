import 'dart:convert';
import 'dart:io';

import 'package:roag_settlement/region.dart';

Map<String, Object> stats(List<int> values) {
  values.sort();
  return {
    'meanUs': values.reduce((a, b) => a + b) / values.length,
    'p95Us': values[(values.length * .95).floor()],
    'maxUs': values.last,
  };
}

void main(List<String> args) {
  final directory = args.isEmpty ? '/var/tmp/roag-goal11/fixtures' : args.first;
  final result = <String, Object>{};
  for (final name in [
    'three-biome-trade',
    'region-scale-85',
    'region-scale-500',
  ]) {
    final r = SettlementRegion.decode(
      File('$directory/$name.json').readAsStringSync(),
    );
    r.setSpeed(1);
    for (var i = 0; i < 200; i++) {
      r.update(.05);
    }
    r.profile = true;
    final samples = <int>[];
    final activePopulation = r.active.colony.people.length;
    for (var i = 0; i < 2000; i++) {
      final w = Stopwatch()..start();
      r.update(.05);
      samples.add(w.elapsedMicroseconds);
    }
    final encoding = <int>[], decoding = <int>[];
    var text = '';
    for (var i = 0; i < 8; i++) {
      var w = Stopwatch()..start();
      text = r.encode();
      encoding.add(w.elapsedMicroseconds);
      w = Stopwatch()..start();
      SettlementRegion.decode(text);
      decoding.add(w.elapsedMicroseconds);
    }
    final w = Stopwatch()..start();
    for (var i = 0; i < 1000; i++) {
      r.visit(r.towns.keys.elementAt(i % r.towns.length));
    }
    final switchingUs = w.elapsedMicroseconds / 1000;
    final stageStart = r.clock;
    r.profile = false;
    for (var i = 0; i < 20000; i++) {
      r.update(.05);
    }
    if (!r.towns.values.every((t) => t.colony.clock == r.clock)) {
      throw StateError('calendar drift');
    }
    result[name] = {
      'population': activePopulation,
      'totalPopulation': r.towns.values.fold<int>(
        0,
        (n, t) => n + t.colony.people.length,
      ),
      'tick': stats(samples),
      'activeUsPerTick': r.activeModelUs / 2000,
      'backgroundUsPerTick': r.backgroundModelUs / 2000,
      'tradeUsPerTick': r.tradeUs / 2000,
      'schedulingUsPerTick': r.schedulingUs / 2000,
      'save': stats(encoding),
      'load': stats(decoding),
      'bytes': utf8.encode(text).length,
      'modelSwitchMeanUs': switchingUs,
      'seasonRunSeconds': r.clock - stageStart,
      'clocksCoherent': true,
      'deliveries': r.tradeRoutes.values.fold<int>(
        0,
        (n, t) => n + t.deliveries,
      ),
    };
    stdout.writeln(jsonEncode({name: result[name]}));
  }
  File('$directory/../performance-region-vm.json')
      .writeAsStringSync(jsonEncode(result));
}
