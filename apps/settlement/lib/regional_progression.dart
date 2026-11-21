part of 'region.dart';

extension RegionalProgression on SettlementRegion {
  int reliablePartners(String site) => tradeRoutes.values
      .where(
        (r) => r.enabled && r.deliveries > 0 && (r.a == site || r.b == site),
      )
      .length;
  bool landmarkStanding(RegionalTown town) =>
      town.landmark != null &&
      town.colony.kind(town.landmark) == Place.hall &&
      town.colony.placeAccess(town.landmark) != null;
  void _progress() {
    for (final town in towns.values) {
      final before = town.stage;
      town.recognise();
      if (town.colony.people.length >= 12 &&
          reliablePartners(town.siteId) >= 2 &&
          landmarkStanding(town) &&
          town.protected) {
        town.stage = DevelopmentStage.centre;
      }
      if (town.stage != before) {
        remember(
          'development',
          town.siteId,
          '${town.name} became a ${town.stage.label}.',
        );
      }
    }
  }

  int goodsAvailable(RegionalTown town, TradeGood good) =>
      town.stock[good]! + town.supplied[good]!;
  void _useGoods(RegionalTown town, TradeGood good, int count) {
    final imported = math.min(count, town.supplied[good]!);
    town.supplied[good] = town.supplied[good]! - imported;
    town.stock[good] = town.stock[good]! - (count - imported);
  }

  List<bool> projectConditions(String project, String townId) {
    final town = towns[townId];
    if (town == null) return [false];
    switch (project) {
      case 'network':
        return [
          towns.length >= 3,
          tradeRoutes.values
                  .where((r) => r.enabled && r.deliveries > 0)
                  .length >=
              2,
          tradeRoutes.values.fold<int>(0, (n, r) => n + r.deliveries) >= 4,
          goodsAvailable(town, TradeGood.materials) >= 4 &&
              goodsAvailable(town, TradeGood.wares) >= 2,
        ];
      case 'landmark':
        return [
          freeBuild || town.stage.index >= DevelopmentStage.town.index,
          town.colony
              .places(Place.hall)
              .any(
                (p) =>
                    town.colony.placeAccess(p.key) != null &&
                    town.colony.session.condition(p.key).health == 100,
              ),
          goodsAvailable(town, TradeGood.materials) >= 6 &&
              goodsAvailable(town, TradeGood.wares) >= 4,
          freeBuild || town.colony.session.money >= 40000,
        ];
      case 'resilient':
        return [
          towns.values
                  .where(
                    (t) =>
                        t.protected &&
                        t.colony
                            .places(Place.refuge)
                            .any((p) => t.colony.placeAccess(p.key) != null),
                  )
                  .length >=
              2,
          towns.values.every(
            (t) => t.colony.world.places.keys
                .followedBy(t.colony.world.cottages.keys)
                .every((id) => t.colony.session.condition(id).health == 100),
          ),
          towns.values.every((t) => t.colony.incident == null),
        ];
      default:
        return [false];
    }
  }

  String projectRules(String project) => switch (project) {
    'network' => 'Three settlements, two reliable relationships, four actual deliveries; spend 4 Materials and 2 Crafted Wares. Regional cooperation gives +2 mood.',
    'landmark' => 'A developed Town and a healthy connected Hall; spend 40 coins, 6 Materials and 4 Crafted Wares. A visible Wayfarer Monument draws more real visitor opportunities while the Hall stands.',
    'resilient' => 'Two settlements with trained protection and accessible Refuges, no active incident and all extant damage repaired. Shared experience extends Standard cargo waiting to 240 seconds.',
    _ => 'Unknown project.',
  };
  String? projectIssue(String project, String townId) {
    if (projects.contains(project)) {
      return 'Already completed; recognition stays forward.';
    }
    final conditions = projectConditions(project, townId);
    return conditions.every((v) => v)
        ? null
        : '${conditions.where((v) => v).length}/${conditions.length} conditions met';
  }

  bool completeProject(String project, String townId) {
    if (projectIssue(project, townId) != null) return false;
    final town = towns[townId]!;
    if (project == 'network') {
      _useGoods(town, TradeGood.materials, 4);
      _useGoods(town, TradeGood.wares, 2);
    } else if (project == 'landmark') {
      final hall = town.colony
          .places(Place.hall)
          .firstWhere(
            (p) =>
                town.colony.placeAccess(p.key) != null &&
                town.colony.session.condition(p.key).health == 100,
          );
      _useGoods(town, TradeGood.materials, 6);
      _useGoods(town, TradeGood.wares, 4);
      final cost = freeBuild ? 0 : 40000;
      town.colony.session.money -= cost;
      town.projectsSpent += cost;
      town.landmark = hall.key;
    }
    projects.add(project);
    remember(
      'project',
      townId,
      '${switch (project) {
        'network' => 'Trade Network',
        'landmark' => 'Wayfarer Monument',
        _ => 'Resilient Region',
      }} completed.',
    );
    return true;
  }
}
