// condition belongs to a durable building, not a reversible landscape snapshot.
class BuildingCondition {
  int health, damageRevision, repairs, uses;
  double productive, repairedAt;
  bool scorched;
  BuildingCondition({
    this.health = 100,
    this.damageRevision = 0,
    this.repairs = 0,
    this.uses = 0,
    this.productive = 0,
    this.repairedAt = -1,
    this.scorched = false,
  });
  bool get usable => health >= 40;
  String get label => !usable
      ? 'Temporarily unusable'
      : health < 100
      ? 'Damaged'
      : 'Operational';
  double get output => !usable
      ? 0
      : health < 100
      ? .6
      : 1;
  bool get developed => productive >= 45 || uses >= 4;
  int get repairCost =>
      health >= 100 ? 0 : ((100 - health) * 50).clamp(1000, 5000);
  Map<String, Object> toJson() => {
    'health': health,
    'damageRevision': damageRevision,
    'repairs': repairs,
    'uses': uses,
    'productive': productive,
    'repairedAt': repairedAt,
    'scorched': scorched,
  };
}

class RepairChange {
  final String id;
  final int health, damageRevision, uses;
  final double productive;
  const RepairChange(
    this.id,
    this.health,
    this.damageRevision, [
    this.productive = 0,
    this.uses = 0,
  ]);
}
