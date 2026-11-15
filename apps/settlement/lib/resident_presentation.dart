import 'dart:math' as math;
import 'dart:ui';

import 'appearance.dart';
import 'model.dart';
import 'residents.dart';

class ResidentPose {
  final Offset feet;
  final double lift, lean, facing, gesture;
  const ResidentPose(
    this.feet,
    this.lift,
    this.lean,
    this.facing,
    this.gesture,
  );
}

// owns only cosmetic time and bounded slots. never writes to residents or world.
class ResidentPresentation {
  double time = 0;
  final Map<String, (Cell, int)> _standing = {};
  static const slots = [
    Offset(-3, -1.5),
    Offset(0, 1.5),
    Offset(3, -1.5),
    Offset(-3, 1.5),
    Offset(0, -1.5),
    Offset(3, 1.5),
  ];

  void update(double dt, Iterable<Resident> residents) {
    time += dt.clamp(0.0, .1);
    final people = residents.where((r) => r.visible).toList();
    final byId = {for (final r in people) r.homeId: r};
    _standing.removeWhere((id, slot) {
      final r = byId[id];
      return r == null || r.moving || r.cell != slot.$1;
    });
    people.sort((a, b) => a.homeId.compareTo(b.homeId));
    for (final r in people.where((r) => !r.moving)) {
      if (_standing.containsKey(r.homeId)) continue;
      final used = _standing.values
          .where((s) => s.$1 == r.cell)
          .map((s) => s.$2)
          .toSet();
      final start = Appearance.identityHash(r.homeId) % 6;
      final slot = List.generate(
        6,
        (i) => (start + i) % 6,
      ).firstWhere((i) => !used.contains(i), orElse: () => start);
      _standing[r.homeId] = (r.cell, slot);
    }
  }

  ResidentPose pose(
    Resident r, {
    bool reducedMotion = false,
    bool paused = false,
  }) {
    final hash = Appearance.identityHash(r.homeId);
    final phase = hash % 101 / 101 * math.pi * 2;
    var offset = Offset.zero;
    if (r.route.isNotEmpty) {
      final next = r.route.first, lane = (hash % 3 - 1) * 1.1;
      offset = next.x != r.cell.x ? Offset(0, lane) : Offset(lane, 0);
    } else if (r.activity != Activity.settling) {
      offset = slots[_standing[r.homeId]?.$2 ?? hash % 6];
    }
    // all lanes/standing feet stay inside their road cell, including corners.
    final feet = Offset(r.x * 12, r.y * 12) + offset;
    final motion = !reducedMotion && !paused;
    final t = time + phase;
    var lift = 0.0, lean = 0.0, gesture = 0.0, facing = r.facing;
    final social =
        r.activity == Activity.socialising || r.activity == Activity.gathering;
    if (social) facing = offset.dx > 0 ? -1 : 1;
    if (motion && r.moving) {
      lift = math.sin(time * 15 + phase).abs() * .55;
      lean = math.sin(time * 15 + phase) * .025;
    } else if (motion && r.activity == Activity.working) {
      gesture = math.max(0, math.sin(t * 5.2));
      lean = gesture * .075;
    } else if (motion && social) {
      gesture = math.max(0, math.sin(t * 2.4) - .5) * 2;
      lean = gesture * .025;
    } else if (motion && r.activity != Activity.resting) {
      // an occasional glance, with still intervals rather than constant bobbing.
      if (t % 11 > 9.8) facing = -facing;
    }
    return ResidentPose(feet, lift, lean, facing, gesture);
  }
}
