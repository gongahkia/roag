import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/region_geometry.dart';
import 'package:roag_settlement/world_generation.dart';

void main() {
  test('regional seeds use the established numeric and Unicode semantics', () {
    final one = RegionalGeography.generate(
      ' +000202 ',
      EnvironmentPreset.plains,
    );
    expect(
      jsonEncode(one.toJson()),
      jsonEncode(
        RegionalGeography.generate('202', EnvironmentPreset.plains).toJson(),
      ),
    );
    expect(one.sites.first.terrainSeed, '202');
    final words = RegionalGeography.generate('森林', EnvironmentPreset.plains);
    expect(
      jsonEncode(words.toJson()),
      jsonEncode(
        RegionalGeography.generate('森林', EnvironmentPreset.plains).toJson(),
      ),
    );
    expect(
      words.sites.map((s) => s.terrainSeed),
      isNot(one.sites.map((s) => s.terrainSeed)),
    );
  });
  test('600 regions have stable IDs, connected early opportunities and later discoveries', () {
    final fingerprints = <String>{};
    for (var seed = 0; seed < 600; seed++) {
      final g = RegionalGeography.generate('$seed', EnvironmentPreset.plains);
      final text = jsonEncode(g.toJson());
      expect(
        jsonEncode(
          RegionalGeography.generate(
            '$seed',
            EnvironmentPreset.plains,
          ).toJson(),
        ),
        text,
      );
      expect(
        jsonEncode(RegionalGeography.decode(jsonDecode(text)).toJson()),
        text,
      );
      expect(g.sites.length, 10);
      expect(g.sites.map((s) => s.id).toSet().length, 10);
      expect(g.sites.map((s) => s.terrainSeed).toSet().length, 10);
      expect(g.neighbours('site-00').length, 2);
      expect(g.path('site-00', 'site-01'), ['link-0-1']);
      expect(g.path('site-00', 'site-02'), ['link-0-2']);
      expect(
        g.sites.any((s) => s.environment == EnvironmentPreset.pineUplands),
        isTrue,
      );
      expect(
        g.sites.any((s) => s.environment == EnvironmentPreset.saltCoast),
        isTrue,
      );
      for (final site in g.sites) {
        final route = g.path('site-00', site.id);
        expect(route, isNotNull);
        var at = 'site-00';
        for (final id in route!) {
          final edge = g.connections.firstWhere((e) => e.id == id);
          expect(edge.contains(at), isTrue);
          at = edge.other(at);
        }
        expect(at, site.id);
      }
      fingerprints.add(
        g.sites.map((s) => '${s.x},${s.y},${s.environment.name}').join(';'),
      );
    }
    expect(fingerprints.length, 600);
  });
  test(
    'travel uses only known connected geography and reroutes around closures',
    () {
      final g = RegionalGeography.generate('202', EnvironmentPreset.plains);
      expect(
        g.path('site-00', 'site-03', known: {'site-00', 'site-01'}),
        isNull,
      );
      expect(g.path('site-00', 'site-01', closed: {'link-0-1'}), [
        'link-0-2',
        'link-1-2',
      ]);
      expect(
        g.path('site-00', 'site-01', closed: {'link-0-1', 'link-0-2'}),
        isNull,
      );
      expect(g.path('site-00', 'site-00'), isEmpty);
    },
  );
  test('malformed or disconnected persisted regional graphs are rejected', () {
    Map<String, dynamic> data() => jsonDecode(
      jsonEncode(
        RegionalGeography.generate('202', EnvironmentPreset.plains).toJson(),
      ),
    );
    var d = data();
    d['sites'].add(d['sites'].first);
    expect(() => RegionalGeography.decode(d), throwsFormatException);
    d = data();
    d['connections'][0]['seconds'] = -1;
    expect(() => RegionalGeography.decode(d), throwsFormatException);
    d = data();
    d['connections'].removeWhere((e) => e['a'] == 'site-00');
    expect(() => RegionalGeography.decode(d), throwsFormatException);
    d = data();
    d['sites'][2]['environment'] = 'fake';
    expect(() => RegionalGeography.decode(d), throwsFormatException);
  });
  test('new environments retain viable openings, concrete persistence and distinctive geography', () {
    for (final p in [
      EnvironmentPreset.pineUplands,
      EnvironmentPreset.saltCoast,
    ]) {
      for (var seed = 0; seed < 100; seed++) {
        final w = WorldGeneration.create('$seed', p);
        expect(w.generation!.version, 4);
        expect(w.encode(), Landscape.decode(w.encode()).encode());
        expect(evaluate(w, Tool.cottage, const Cell(2, 14)).valid, isTrue);
        expect(
          w.lots.where((l) => l.terrain == Terrain.ground).length,
          greaterThan(250),
        );
        if (p == EnvironmentPreset.saltCoast) {
          expect(
            w.lots.where((l) => l.terrain == Terrain.water).length,
            greaterThan(250),
          );
          final crossing = w.cells
              .where((c) => c.y == 15 && w.at(c).terrain == Terrain.water)
              .toList();
          expect(crossing.length, 3);
        } else {
          expect(
            w.lots.where((l) => l.terrain == Terrain.forest).length,
            greaterThan(150),
          );
          expect(
            w.lots.where((l) => l.terrain == Terrain.rock).length,
            greaterThan(40),
          );
        }
      }
    }
  });
}
