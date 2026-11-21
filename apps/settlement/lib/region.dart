import 'dart:convert';
import 'dart:math' as math;

import 'climate.dart';
import 'colony.dart';
import 'model.dart';
import 'region_geometry.dart';
import 'residents.dart';
import 'rules.dart';
import 'world_generation.dart';

export 'region_geometry.dart';
part 'regional_trade.dart';
part 'regional_progression.dart';

enum TradeGood {
  provisions('Provisions'),
  materials('Materials'),
  wares('Crafted Wares');

  final String label;
  const TradeGood(this.label);
}

enum DevelopmentStage {
  hamlet('Hamlet'),
  village('Village'),
  town('Town'),
  centre('Regional Centre');

  final String label;
  const DevelopmentStage(this.label);
}

class RegionalTown {
  final String siteId;
  String name;
  final Colony colony;
  DevelopmentStage stage;
  final Map<TradeGood, int> stock = {for (final g in TradeGood.values) g: 0};
  final Map<TradeGood, int> supplied = {for (final g in TradeGood.values) g: 0};
  final Map<TradeGood, int> produced = {for (final g in TradeGood.values) g: 0};
  final Map<TradeGood, double> productionProgress = {
    for (final g in TradeGood.values) g: 0,
  };
  int foundingSpent = 0, importsSpent = 0, exportsEarned = 0, projectsSpent = 0;
  int deliveries = 0;
  String? landmark;
  double provisionBenefitUntil = 0;
  List<double>? camera;
  RegionalTown(
    this.siteId,
    this.name,
    this.colony, {
    this.stage = DevelopmentStage.hamlet,
  });
  List<bool> get villageRequirements => [
    colony.people.length >= 4,
    colony.workEarned >= 3000,
    colony
        .places(Place.market)
        .any((p) => colony.staffed(p.key) && colony.placeAccess(p.key) != null),
  ];
  List<bool> get townRequirements => [
    colony.people.length >= 8,
    colony.people.values.fold<double>(0, (n, r) => n + r.workedSeconds) >= 120,
    colony.places(Place.green).any((p) => colony.placeAccess(p.key) != null) ||
        colony.places(Place.hall).any((p) => colony.placeAccess(p.key) != null),
    deliveries > 0 || colony.workEarned >= 20000,
  ];
  bool get protected => colony.people.values.any(colony.ready);
  void recognise() {
    if (stage.index < DevelopmentStage.village.index &&
        villageRequirements.every((v) => v)) {
      stage = DevelopmentStage.village;
    }
    if (stage.index < DevelopmentStage.town.index &&
        townRequirements.every((v) => v)) {
      stage = DevelopmentStage.town;
    }
  }

  String get progress {
    final requirements = stage == DevelopmentStage.hamlet
        ? villageRequirements
        : townRequirements;
    return '${stage.label} · ${requirements.where((v) => v).length}/${requirements.length} ${stage == DevelopmentStage.hamlet ? 'Village' : 'Town'} conditions';
  }

  Map<String, Object?> toJson() => {
    'site': siteId,
    'name': name,
    'colony': jsonDecode(colony.encode()),
    'stage': stage.name,
    'stock': {for (final g in TradeGood.values) g.name: stock[g]},
    'supplied': {for (final g in TradeGood.values) g.name: supplied[g]},
    'produced': {for (final g in TradeGood.values) g.name: produced[g]},
    'productionProgress': {
      for (final g in TradeGood.values) g.name: productionProgress[g],
    },
    'foundingSpent': foundingSpent,
    'importsSpent': importsSpent,
    'exportsEarned': exportsEarned,
    'projectsSpent': projectsSpent,
    'deliveries': deliveries,
    'landmark': landmark,
    'provisionBenefitUntil': provisionBenefitUntil,
    'camera': camera,
  };
}

// One capital clock orders every town. Only the selected town owns a Flame renderer.
class SettlementRegion {
  static const schema = 7, foundingCost = 60000, foundingAllocation = 50000;
  final RegionalGeography geography;
  final Map<String, RegionalTown> towns;
  final Set<String> discovered;
  final Set<String> projects = {};
  final Map<String, RegionalTradeRoute> tradeRoutes = {};
  final Map<String, RegionalShipment> shipments = {};
  final Set<String> observedClosures = {};
  int nextShipment = 1;
  double nextTradeAt = 0, nextSupplyAt = 0;
  final List<Map<String, Object?>> history = [];
  String activeId;
  int nextFact = 1;
  double nextRegionalAt = 0;
  SettlementRegion._(
    this.geography,
    this.towns,
    this.discovered,
    this.activeId,
  ) {
    for (final town in towns.values) {
      _wireTown(town);
    }
  }
  RegionalTown get capital => towns['site-00']!;
  RegionalTown get active => towns[activeId]!;
  double get clock => capital.colony.clock;
  int get speed => capital.colony.speed;
  int get treasury =>
      towns.values.fold(0, (n, t) => n + t.colony.session.money);
  bool get freeBuild => capital.colony.session.unlimited;
  factory SettlementRegion.fromColony(
    Colony colony, {
    String name = 'First Hearth',
  }) {
    final seed = colony.world.generation?.seedText ?? '${colony.world.seed}';
    final g = RegionalGeography.generate(
      seed,
      colony.world.generation?.preset ?? EnvironmentPreset.plains,
    );
    final town = RegionalTown('site-00', name, colony)..recognise();
    return SettlementRegion._(g, {'site-00': town}, {'site-00'}, 'site-00');
  }
  void _wireTown(RegionalTown town) {
    town.colony.onFreightServed = _freightServed;
    town.colony.onFreightExit = _freightExit;
    town.colony.onProductiveWork = (id, kind, seconds) {
      final good = switch (kind) {
        Place.garden => TradeGood.provisions,
        Place.workshop => TradeGood.wares,
        Place.lumberCamp => TradeGood.materials,
        _ => null,
      };
      if (good == null || seconds <= 0 || town.stock[good]! >= 24) return;
      final duration = good == TradeGood.provisions ? 8 : 12;
      final abundance = good == TradeGood.materials
          ? 1 + math.min(12, town.colony.timberCount(id)) / 24
          : 1.0;
      var progress = town.productionProgress[good]! + seconds * abundance;
      while (progress >= duration - 1e-9 && town.stock[good]! < 24) {
        progress = math.max(0, progress - duration);
        town.stock[good] = town.stock[good]! + 1;
        town.produced[good] = town.produced[good]! + 1;
      }
      town.productionProgress[good] = town.stock[good]! >= 24 ? 0 : progress;
    };
    town.colony.regionalMood = () =>
        (town.provisionBenefitUntil > town.colony.clock ? 3 : 0) +
        (projects.contains('network') ? 2 : 0);
  }

  void setSpeed(int value) {
    if (![0, 1, 2, 4, 16].contains(value)) {
      throw const FormatException('Invalid regional speed.');
    }
    for (final t in towns.values) {
      t.colony.speed = value;
    }
  }

  void setFreeBuild(bool enabled) {
    for (final town in towns.values) {
      town.colony.session.unlimited = enabled;
    }
  }

  void update(double elapsed) {
    final first = capital.colony;
    first.remainder += elapsed.clamp(0.0, .05) * first.speed;
    while (first.remainder >= .05 - 1e-9) {
      first.remainder = math.max(0, first.remainder - .05);
      for (final town in towns.values) {
        town.colony.advanceRegionalTick();
      }
      _tradeTick(.05);
      if (clock + 1e-9 >= nextRegionalAt) {
        nextRegionalAt = clock + 1;
        _progress();
      }
    }
    if (first.remainder < 1e-9) first.remainder = 0;
    for (final town in towns.values) {
      town.colony.remainder = first.remainder;
    }
  }

  void remember(String kind, String site, String text) {
    history.add({
      'id': 'regional-${nextFact++}',
      'at': clock,
      'kind': kind,
      'site': site,
      'text': text,
    });
    if (history.length > 120) history.removeAt(0);
  }

  String? discoveryIssue(String id) {
    if (!geography.sites.any((s) => s.id == id)) {
      return 'Unknown regional site.';
    }
    if (discovered.contains(id)) return 'Already discovered.';
    if (!geography
        .neighbours(id)
        .any((e) => discovered.contains(e.other(id)))) {
      return 'Discover a connected trail first.';
    }
    if (!freeBuild && capital.stage == DevelopmentStage.hamlet) {
      return 'Reach Village: four people, real work and a staffed Market.';
    }
    if (!freeBuild &&
        !['site-01', 'site-02'].contains(id) &&
        towns.length < 2) {
      return 'Found a second settlement to explore farther.';
    }
    if (!freeBuild &&
        !['site-01', 'site-02'].contains(id) &&
        !tradeRoutes.values.any((route) => route.deliveries > 0)) {
      return 'Complete an automatic delivery to reveal the farther environments.';
    }
    if (!freeBuild && capital.colony.session.money < 2000) {
      return 'Exploration costs 2 coins from the founding town.';
    }
    return null;
  }

  bool discover(String id) {
    if (discoveryIssue(id) != null) return false;
    if (!freeBuild) {
      capital.colony.session.money -= 2000;
    }
    discovered.add(id);
    remember(
      'discovery',
      id,
      'Discovered ${geography.site(id).environment.label}.',
    );
    return true;
  }

  String? foundingIssue(String id) {
    if (towns.containsKey(id)) return 'This settlement is already founded.';
    if (!discovered.contains(id)) return 'Discover this site first.';
    if (geography.path('site-00', id, known: discovered) == null) {
      return 'A discovered travel connection is required.';
    }
    if (!freeBuild && capital.stage == DevelopmentStage.hamlet) {
      return 'Village development opens outward expansion.';
    }
    if (!freeBuild && capital.colony.session.money < foundingCost) {
      return 'Founding needs 60 coins: 50 move to the new town; 10 prepare the expedition.';
    }
    return null;
  }

  bool found(String id, {String? name}) {
    if (foundingIssue(id) != null) return false;
    final site = geography.site(id);
    final title = _name(
      name ??
          '${site.environment == EnvironmentPreset.woodlands
              ? 'Alder'
              : site.environment == EnvironmentPreset.river
              ? 'River'
              : 'Wayfarer'} ${towns.length + 1}',
    );
    final c =
        Colony(
            LandscapeSession(
              WorldGeneration.create(site.terrainSeed, site.environment),
            ),
          )
          ..clock = clock
          ..remainder = capital.colony.remainder
          ..speed = speed
          ..incidentMode = capital.colony.incidentMode;
    c.session.money = foundingAllocation;
    c.session.unlimited = freeBuild;
    // A newly arrived expedition shares the date, not the capital's weather RNG.
    c.climate = Climate(
      c.world.seed,
      calendar: capital.colony.climate.calendar,
      environment: site.environment,
    );
    c.nextIncidentAt = clock + 3 * Rules.day;
    c.nextSpecialAt = clock + 12;
    c.nextVisitorAt = clock + 18;
    if (!freeBuild) {
      capital.colony.session.money -= foundingCost;
      capital.foundingSpent += foundingCost;
    }
    final town = RegionalTown(id, title, c);
    towns[id] = town;
    _wireTown(town);
    remember(
      'founding',
      id,
      'Founded $title; 50 coins allocated to its treasury.',
    );
    return true;
  }

  bool visit(String id) {
    if (!towns.containsKey(id)) return false;
    activeId = id;
    return true;
  }

  static String _name(String input) {
    final name = input.trim();
    if (name.isEmpty ||
        name.length > 32 ||
        name.runes.any((r) => r < 32 || r == 127)) {
      throw const FormatException('Use a name of 1–32 printable characters.');
    }
    return name;
  }

  bool rename(String id, String name) {
    final town = towns[id];
    if (town == null) return false;
    town.name = _name(name);
    return true;
  }

  SaveSnapshot snapshot() => SaveSnapshot(
    towns.values.fold<int>(
      nextFact + discovered.length,
      (n, t) => n + t.colony.session.revision,
    ),
    encode(),
  );
  bool persist(SaveSnapshot snapshot, void Function(String) write) {
    if (snapshot.text != encode()) return false;
    write(snapshot.text);
    return true;
  }

  String encode() => jsonEncode({
    'schema': schema,
    'regionalVersion': 1,
    'geography': geography.toJson(),
    'active': activeId,
    'discovered': discovered.toList(),
    'towns': towns.values.map((t) => t.toJson()).toList(),
    'clock': clock,
    'nextRegionalAt': nextRegionalAt,
    'nextFact': nextFact,
    'history': history,
    'projects': projects.toList(),
    'trade': tradeJson(),
  });
  factory SettlementRegion.decode(String text) {
    final raw = jsonDecode(text);
    if (raw is! Map) throw const FormatException('Invalid regional save.');
    if (raw['schema'] != schema) {
      return SettlementRegion.fromColony(Colony.decode(text));
    }
    Never bad() => throw const FormatException('Invalid regional checkpoint.');
    int integer(dynamic n, [int maximum = 1000000000000]) {
      if (n is! int || n < 0 || n > maximum) bad();
      return n;
    }

    double number(dynamic n, [double min = 0, double max = 1e12]) {
      if (n is! num || !n.isFinite || n < min || n > max) bad();
      return n.toDouble();
    }

    if (raw['regionalVersion'] != 1 ||
        raw['towns'] is! List ||
        raw['towns'].isEmpty ||
        raw['towns'].length > 32 ||
        raw['discovered'] is! List ||
        raw['discovered'].length > 32 ||
        raw['history'] is! List ||
        raw['history'].length > 120 ||
        raw['projects'] is! List) {
      bad();
    }
    final geography = RegionalGeography.decode(raw['geography']);
    final known = geography.sites.map((s) => s.id).toSet();
    final discovered = <String>{};
    for (final id in raw['discovered']) {
      if (id is! String || !known.contains(id) || !discovered.add(id)) bad();
    }
    final towns = <String, RegionalTown>{};
    for (final value in raw['towns']) {
      if (value is! Map ||
          value['site'] is! String ||
          !discovered.contains(value['site']) ||
          towns.containsKey(value['site']) ||
          value['name'] is! String) {
        bad();
      }
      final c = Colony.decode(jsonEncode(value['colony']));
      final stage = DevelopmentStage.values.firstWhere(
        (s) => s.name == value['stage'],
        orElse: () => throw const FormatException('Invalid development stage.'),
      );
      final town = RegionalTown(
        value['site'],
        _name(value['name']),
        c,
        stage: stage,
      );
      for (final (key, target) in [
        ('stock', town.stock),
        ('supplied', town.supplied),
        ('produced', town.produced),
      ]) {
        final map = value[key];
        if (map is! Map || map.length != TradeGood.values.length) bad();
        for (final g in TradeGood.values) {
          target[g] = integer(map[g.name]);
        }
      }
      if (value['productionProgress'] is! Map) bad();
      for (final g in TradeGood.values) {
        town.productionProgress[g] = number(
          value['productionProgress'][g.name],
          0,
          120,
        );
      }
      town.foundingSpent = integer(value['foundingSpent']);
      town.importsSpent = integer(value['importsSpent']);
      town.exportsEarned = integer(value['exportsEarned']);
      town.projectsSpent = integer(value['projectsSpent']);
      town.deliveries = integer(value['deliveries']);
      if (value['landmark'] != null &&
          (value['landmark'] is! String ||
              !RegExp(r'^[A-Za-z0-9_-]{1,64}$').hasMatch(value['landmark']))) {
        bad();
      }
      town.landmark = value['landmark'];
      town.provisionBenefitUntil = number(value['provisionBenefitUntil']);
      if (value['camera'] != null) {
        final camera = value['camera'];
        if (camera is! List || camera.length != 3) bad();
        town.camera = [
          number(camera[0], -1000, 10000),
          number(camera[1], -1000, 10000),
          number(camera[2], 2, 6),
        ];
      }
      towns[town.siteId] = town;
    }
    if (!towns.containsKey('site-00') ||
        raw['active'] is! String ||
        !towns.containsKey(raw['active'])) {
      bad();
    }
    final region = SettlementRegion._(
      geography,
      towns,
      discovered,
      raw['active'],
    );
    if (number(raw['clock']) != region.clock ||
        towns.values.any(
          (t) =>
              t.colony.clock != region.clock ||
              t.colony.speed != region.speed ||
              t.colony.remainder != region.capital.colony.remainder,
        )) {
      bad();
    }
    region.nextRegionalAt = number(raw['nextRegionalAt']);
    region.nextFact = integer(raw['nextFact']);
    final facts = <String>{};
    for (final fact in raw['history']) {
      if (fact is! Map ||
          fact['id'] is! String ||
          !RegExp(r'^regional-[0-9]+$').hasMatch(fact['id']) ||
          !facts.add(fact['id']) ||
          fact['text'] is! String ||
          fact['text'].length > 500 ||
          fact['kind'] is! String ||
          !known.contains(fact['site'])) {
        bad();
      }
      number(fact['at'], 0, region.clock + .001);
      if (int.parse((fact['id'] as String).split('-').last) >=
          region.nextFact) {
        bad();
      }
      region.history.add(Map<String, Object?>.from(fact));
    }
    for (final p in raw['projects']) {
      if (p is! String ||
          !['network', 'landmark', 'resilient'].contains(p) ||
          !region.projects.add(p)) {
        bad();
      }
    }
    region.restoreTrade(raw['trade']);
    return region;
  }
}
