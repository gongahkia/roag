// safe deterministic copies; never opens a player browser profile.
import 'dart:convert';
import 'dart:io';

import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/resident_request.dart';
import 'package:roag_settlement/special_event.dart';

import 'colony_fixture.dart' show paint;
import 'tranche05_fixture.dart' show developedTown;

void main(List<String> args) {
  final out = Directory(
    args.isEmpty ? '/tmp/roag-tranche06-fixtures' : args.single,
  )..createSync(recursive: true);
  final need = Colony(LandscapeSession(Landscape.create(202)))
    ..nextSpecialAt = 1e9;
  paint(need, Tool.cottage, const Cell(2, 14), const Cell(5, 14));
  for (var i = 0; i < 240; i++) {
    need.update(.05);
  }
  need.speed = 0;
  final actor = need.people.values.firstWhere(
    (r) => need.hasNeed(r, Need.work),
  );
  File('${out.path}/need-work.json').writeAsStringSync(need.encode());
  final market = developedTown()..nextSpecialAt = 1e9;
  for (var i = 0; i < 280; i++) {
    market.update(.05);
  }
  if (!market.beginSpecial(SpecialKind.marketDay, fixture: true)) {
    throw StateError('No actual Market-day service eligibility.');
  }
  market.speed = 0;
  final noCustomers = Colony.decode(market.encode()), host = market.civicHost!;
  for (final id in noCustomers.people.keys.where((id) => id != host).toList()) {
    paint(noCustomers, Tool.remove, noCustomers.world.cottages[id]!);
  }
  File('${out.path}/market-no-customers.json')
      .writeAsStringSync(noCustomers.encode());
  final poor = Colony.decode(market.encode());
  while (poor.session.money >= 3000) {
    final cell = poor.world.cells.firstWhere(
      (cell) => evaluate(poor.world, Tool.stoneRoad, cell).valid,
    );
    paint(poor, Tool.stoneRoad, cell);
  }
  File('${out.path}/market-low-funds.json').writeAsStringSync(poor.encode());
  File('${out.path}/market-day.json').writeAsStringSync(market.encode());
  final populated = Colony.decode(
    File('../../docs/world-builder/evidence/tranche-05/large-85.json')
        .readAsStringSync(),
  )..speed = 0;
  final home = populated.world.cottages.entries.first;
  final workplace = populated.workplaces.firstWhere(
    (p) => populated.workers(p.key) > 0,
  );
  final dest = populated.world.cells.firstWhere(
    (p) =>
        p.x >= 25 &&
        p.x <= 27 &&
        p.y == 20 &&
        evaluateMove(populated.world, home.value, p).valid,
  );
  final workDest = populated.world.cells.firstWhere(
    (p) =>
        p.x >= 25 &&
        p.x <= 27 &&
        p.y == 20 &&
        p != dest &&
        evaluateMove(populated.world, workplace.value, p).valid,
  );
  File('${out.path}/populated.json').writeAsStringSync(populated.encode());
  final metadata = {
    'requestResident': actor.homeId,
    'requestName': actor.name,
    'requestCell': [actor.cell.x, actor.cell.y],
    'cottage': home.key,
    'cottageSource': [home.value.x, home.value.y],
    'cottageDestination': [dest.x, dest.y],
    'workplace': workplace.key,
    'workSource': [workplace.value.x, workplace.value.y],
    'workDestination': [workDest.x, workDest.y],
    'civicHost': market.civicHost,
    'civicEvent': market.special!.id,
  };
  File('${out.path}/metadata.json').writeAsStringSync(jsonEncode(metadata));
  stdout.writeln(jsonEncode(metadata));
}
