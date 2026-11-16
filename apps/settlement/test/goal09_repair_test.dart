import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/condition.dart';

LandscapeSession town() {
  final s = LandscapeSession(Landscape.create());
  s.beginStroke(Tool.cottage, const Cell(2, 14));
  expect(s.finishStroke(), true);
  s.conditions[s.world.at(const Cell(2, 14)).buildingId!] = BuildingCondition(
    health: 30,
    damageRevision: 1,
    scorched: true,
  );
  return s;
}

void main() {
  test('multi-building repair captures prior use, exact cost, cancellation and one history step', () {
    final s = town(), first = s.world.at(const Cell(2, 14)).buildingId!;
    s.beginStroke(Tool.cottage, const Cell(3, 14));
    expect(s.finishStroke(), true);
    final second = s.world.at(const Cell(3, 14)).buildingId!;
    s.condition(first)
      ..productive = 12
      ..uses = 4;
    s.condition(second)
      ..health = 50
      ..damageRevision = 1;
    final money = s.money;
    s.beginStroke(Tool.repair, const Cell(2, 14));
    s.stroke!.paintTo(const Cell(3, 14));
    expect(s.stroke!.price, 6000);
    s.cancelStroke();
    expect(s.money, money);
    expect(s.condition(first).health, 30);
    s.beginStroke(Tool.repair, const Cell(2, 14));
    s.stroke!.paintTo(const Cell(3, 14));
    expect(s.finishStroke(), true);
    expect(s.money, money - 6000);
    expect(s.condition(second).health, 100);
    expect(s.undo(), true);
    expect(s.money, money);
    expect(s.condition(first).health, 30);
    expect(s.condition(second).health, 50);
    expect(s.redo(), true);
    expect(s.money, money - 6000);
  });

  test('repair is one paid stroke, restores the same identity, and fresh undo/redo is exact', () {
    final s = town(),
        id = s.world.at(const Cell(2, 14)).buildingId!,
        money = s.money,
        world = s.world;
    s.beginStroke(Tool.repair, const Cell(2, 14));
    s.stroke!.paintTo(const Cell(2, 14));
    expect(s.stroke!.price, 3500);
    expect(s.condition(id).health, 30);
    expect(s.finishStroke(), true);
    expect(s.money, money - 3500);
    expect(identical(s.world, world), true);
    expect(s.condition(id).health, 100);
    expect(s.undo(), true);
    expect(s.money, money);
    expect(s.condition(id).health, 30);
    expect(s.redo(), true);
    expect(s.money, money - 3500);
    expect(s.condition(id).health, 100);
  });
  test('healthy, unsafe and unaffordable repairs do nothing', () {
    final s = town(),
        id = s.world.at(const Cell(2, 14)).buildingId!,
        money = s.money;
    s.unsafeBuilding = (key) => key == id;
    s.beginStroke(Tool.repair, const Cell(2, 14));
    expect(s.finishStroke(), false);
    expect(s.money, money);
    s.unsafeBuilding = null;
    s.money = 1000;
    s.beginStroke(Tool.repair, const Cell(2, 14));
    expect(s.finishStroke(), false);
    expect(s.condition(id).health, 30);
    s.money = money;
    s.condition(id).health = 100;
    s.beginStroke(Tool.repair, const Cell(2, 14));
    expect(s.finishStroke(), false);
  });
  test(
    'damage during a staged repair rejects the whole transaction without fees',
    () {
      final s = town(),
          id = s.world.at(const Cell(2, 14)).buildingId!,
          money = s.money;
      s.beginStroke(Tool.repair, const Cell(2, 14));
      s.condition(id)
        ..health = 20
        ..damageRevision = 2;
      expect(s.finishStroke(), false);
      expect(s.money, money);
      expect(s.condition(id).health, 20);
    },
  );
  test('later damage or productive use consumes repair cost; history never refunds a used repair', () {
    for (final damage in [false, true]) {
      final s = town(), id = s.world.at(const Cell(2, 14)).buildingId!;
      s.beginStroke(Tool.repair, const Cell(2, 14));
      expect(s.finishStroke(), true);
      final paid = s.money;
      if (damage) {
        s.condition(id)
          ..health = 70
          ..damageRevision = 2;
      } else {
        s.condition(id).productive += 1;
      }
      expect(s.undo(), false);
      expect(s.money, paid);
      expect(s.condition(id).health, damage ? 70 : 100);
      expect(s.canUndo, true, reason: 'older construction remains available');
    }
  });
  test(
    'layout undo restores the damaged identity without rewinding damage',
    () {
      final s = town(), id = s.world.at(const Cell(2, 14)).buildingId!;
      s.beginStroke(Tool.remove, const Cell(2, 14));
      expect(s.finishStroke(), true);
      expect(s.undo(), true);
      expect(s.world.at(const Cell(2, 14)).buildingId, id);
      expect(s.condition(id).health, 30);
    },
  );
}
