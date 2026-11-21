import 'dart:convert';

import 'colony.dart';
import 'region.dart';
import 'rules.dart';
import 'special_event.dart';

// Debug work owns a decoded copy. The original object, histories and clocks stay
// untouched; persistence remains the host page's responsibility.
class SettlementDebugSession {
  final SettlementRegion original;
  late SettlementRegion region;
  bool closed = false;
  SettlementDebugSession(this.original) {
    region = SettlementRegion.decode(original.encode())..setSpeed(0);
  }

  Map<String, Object?> get diagnostics => {
    'mode': 'disposable-sudo-copy',
    'clock': region.clock,
    'active': region.activeId,
    'speed': region.speed,
    'towns': [
      for (final t in region.towns.values)
        {
          'site': t.siteId,
          'name': t.name,
          'population': t.colony.people.length,
          'clock': t.colony.clock,
          'money': t.colony.session.money,
          'stock': {for (final g in TradeGood.values) g.name: t.stock[g]},
          'planningBacklog': t.colony.planning.length,
          'routeSearches': t.colony.routeSearches,
          'cacheHits': t.colony.navigation.hits,
          'expansions': t.colony.navigation.expansions,
          'incident': t.colony.incident?.toJson(),
        },
    ],
  };

  String snapshot() {
    _open();
    return jsonEncode({
      'ok': true,
      'diagnostics': diagnostics,
      'state': jsonDecode(region.encode()),
    });
  }

  String command(Map<String, dynamic> command) {
    _open();
    final c = region.active.colony;
    int integer(String key, int min, int max) {
      final n = command[key];
      if (n is! int || n < min || n > max) {
        throw FormatException('$key must be an integer from $min to $max.');
      }
      return n;
    }

    String result;
    switch (command['action']) {
      case 'speed':
        region.setSpeed(integer('value', 0, 20));
        result = 'Simulation ${region.speed}×';
      case 'step':
        final ticks = integer('ticks', 1, 200);
        region.setSpeed(1);
        for (var i = 0; i < ticks; i++) {
          region.update(.05);
        }
        region.setSpeed(0);
        result = 'Advanced ${(ticks * .05).toStringAsFixed(2)}s; paused';
      case 'freeBuild':
        if (command['value'] is! bool)
          throw const FormatException('value must be boolean.');
        region.setFreeBuild(command['value']);
        result = 'Free building ${region.freeBuild ? 'on' : 'off'}';
      case 'coins':
        final amount = integer('amount', 1, 1000);
        c.session.money += amount * 1000;
        region.remember('debug', region.activeId, 'Sudo grant: $amount coins.');
        result = 'Added $amount test coins';
      case 'goods':
        final amount = integer('amount', 1, 24);
        final good = TradeGood.values
            .where((g) => g.name == command['good'])
            .firstOrNull;
        if (good == null) throw const FormatException('Unknown good.');
        final town = region.active;
        final added = (24 - town.stock[good]!).clamp(0, amount);
        town.stock[good] = town.stock[good]! + added;
        town.produced[good] = town.produced[good]! + added;
        region.remember(
          'debug',
          region.activeId,
          'Sudo fixture grant: $added ${good.label}; not earned production.',
        );
        result = 'Added $added test ${good.label}';
      case 'incident':
        final kind = IncidentKind.values
            .where((k) => k.name == command['kind'])
            .firstOrNull;
        if (kind == null) throw const FormatException('Unknown incident.');
        if (c.incident != null)
          throw StateError('An incident is already active.');
        final previous = c.incidentMode;
        c.incidentMode = IncidentMode.standard;
        final started = c.startIncident(kind, fixture: true);
        c.incidentMode = previous;
        if (!started)
          throw StateError(
            'No eligible target. Build the required housing, Farm, Market/Inn or water approach first.',
          );
        result = 'Test ${kind.name} started through the incident director';
      case 'rain':
        if (!c.beginSpecial(SpecialKind.rain, fixture: true)) {
          throw StateError(
            'Finish the current special event before starting a test shower.',
          );
        }
        result = 'Test shower started';
      default:
        throw const FormatException('Unknown Sudo action.');
    }
    return result;
  }

  void close() => closed = true;
  void _open() {
    if (closed) throw StateError('This Sudo session has ended.');
  }
}
