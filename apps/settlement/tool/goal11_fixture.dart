// Disposable checkpoints: real construction/work/transport; explicit grid and
// incident fixtures are labelled. Never imports or reads a private browser town.
import 'dart:convert';
import 'dart:io';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/region.dart';
import 'package:roag_settlement/world_generation.dart';

import 'goal08_fixture.dart' show add, paint;
import 'goal09_fixture.dart' show baseTown;
import 'scaling_fixture.dart' show scalingTown;

void runUntil(SettlementRegion r, bool Function() done, [double limit = 1000]) {
  r.setSpeed(1);
  for (var i = 0; i < limit * 20 && !done(); i++) {
    r.update(.05);
  }
  if (!done()) throw StateError('fixture condition at ${r.clock}');
}

void smallTown(Colony c, {bool forestry = false}) {
  for (var x = forestry ? 3 : 2; x <= (forestry ? 6 : 5); x++) {
    add(c, Tool.cottage, Cell(x, 14));
  }
  add(c, Tool.market, const Cell(6, 16));
  add(c, Tool.green, const Cell(4, 16));
  if (forestry) {
    if (c.world.at(const Cell(2, 12)).terrain == Terrain.forest) {
      paint(c, Tool.clear, const Cell(2, 12));
    }
    add(c, Tool.plant, const Cell(2, 12));
    add(c, Tool.lumberCamp, const Cell(2, 14));
  } else {
    add(c, Tool.garden, const Cell(2, 16));
  }
}

SettlementRegion tradingRegion() {
  final c = baseTown()..incidentMode = IncidentMode.peaceful;
  c.session.unlimited = true;
  final r = SettlementRegion.fromColony(c);
  r.discover('site-01');
  r.found('site-01', name: 'Alder Reach');
  smallTown(r.towns['site-01']!.colony, forestry: true);
  r.discover('site-03');
  r.found('site-03', name: 'Pine Crossing');
  smallTown(r.towns['site-03']!.colony);
  r.connectTrade('site-00', 'site-01');
  r.connectTrade('site-00', 'site-03');
  runUntil(r, () => r.tradeRoutes.values.every((t) => t.deliveries > 0));
  return r;
}

void main(List<String> args) {
  final out = Directory(
    args.isEmpty ? '/var/tmp/roag-goal11/fixtures' : args.first,
  )..createSync(recursive: true);
  void save(String name, SettlementRegion r) {
    r.setSpeed(0);
    final text = r.encode();
    if (SettlementRegion.decode(text).encode() != text) {
      throw StateError('inexact $name');
    }
    File('${out.path}/$name.json').writeAsStringSync(text);
    stdout.writeln('$name: ${r.towns.length} towns; ${text.length} chars');
  }

  final trade = tradingRegion();
  save('three-biome-trade', trade);
  final landmark = SettlementRegion.decode(trade.encode());
  runUntil(
    landmark,
    () => landmark.projectIssue('landmark', 'site-00') == null,
  );
  landmark.completeProject('landmark', 'site-00');
  runUntil(landmark, () => landmark.capital.stage == DevelopmentStage.centre);
  save('landmark-centre', landmark);
  runUntil(
    trade,
    () => trade.shipments.values.any((s) => s.state == ShipmentState.unloading),
  );
  final stop = trade.shipments.values.firstWhere(
    (s) => s.state == ShipmentState.unloading,
  );
  trade.visit(stop.destination);
  save('arrival', trade);
  runUntil(
    trade,
    () => trade.towns[stop.destination]!.colony.visits.values.any(
      (v) => v.freight == stop.id && v.actor.cell.x >= 2,
    ),
  );
  trade.towns[stop.destination]!.colony.incidentMode = IncidentMode.standard;
  trade.towns[stop.destination]!.colony.nextIncidentAt = 1e9;
  save('stranding', trade);

  final raid = tradingRegion();
  final capital = raid.capital.colony;
  capital.incidentMode = IncidentMode.standard;
  capital.startIncident(IncidentKind.raiders, fixture: true);
  runUntil(raid, () => raid.closedConnections.isNotEmpty, 100);
  save('regional-incident', raid);

  final coast = SettlementRegion.fromColony(
    Colony(
      LandscapeSession(
        WorldGeneration.create('202', EnvironmentPreset.saltCoast),
      ),
    ),
  );
  save('coast', coast);

  for (final n in [85, 500]) {
    final c = scalingTown(n)..nextSpecialAt = 1e9;
    final available = c.world.cells
        .where((p) => !c.world.occupied(p) && c.world.access(p) != null)
        .toList();
    for (final tool in [Tool.market, Tool.workshop, Tool.hall]) {
      add(c, tool, available.removeAt(0));
    }
    File('${out.path}/single-scale-$n.json').writeAsStringSync(c.encode());
    final r = SettlementRegion.fromColony(c);
    for (final id in ['site-01', 'site-02', 'site-03']) {
      r.discover(id);
      r.found(id);
      final colony = r.towns[id]!.colony, old = r.towns[id]!.colony.world;
      // Explicit scaling-only authored grid, with each site's valid identity.
      colony.session.world = Landscape(old.seed, 40, 30, [
        for (var y = 0; y < 30; y++)
          for (var x = 0; x < 40; x++)
            Lot(grass: (x + y) % 2, path: x == 0 || y.isOdd),
      ], generation: old.generation);
      colony.reconcile(colony.world);
      final cells = [
        for (var y = 0; y < 30; y += 2)
          for (var x = 1; x < 40; x++) Cell(x, y),
      ];
      for (final tool in [
        Tool.garden,
        Tool.workshop,
        Tool.market,
        Tool.green,
        Tool.hall,
      ]) {
        add(colony, tool, cells.removeAt(0));
      }
      for (var i = 0; i < 85; i++) {
        add(colony, Tool.cottage, cells[(i * cells.length / 85).floor()]);
      }
      colony.nextSpecialAt = 1e9;
    }
    r.connectTrade('site-00', 'site-01');
    r.connectTrade('site-00', 'site-02');
    r.setSpeed(1);
    for (var i = 0; i < 1000; i++) {
      r.update(.05);
    }
    save('region-scale-$n', r);
  }
  File('${out.path}/fixture-notes.json').writeAsStringSync(
    jsonEncode({
      'trade': 'Real generated biomes, legal Free Build construction, actual work and deliveries.',
      'regional-incident': 'Explicit fixture:true start through the existing director, real closure phase.',
      'scaling': 'One original 85/500-person connected grid plus three authored 85-person grids; no fake population or earnings counters.',
    }),
  );
}
