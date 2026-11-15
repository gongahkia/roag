import 'dart:ui';

import 'reasons.dart';
import 'residents.dart';
import 'rules.dart';

class BubbleCandidate {
  final Resident resident;
  final List<Rect> positions;
  final Reason? displayReason;
  Reason get reason => displayReason ?? resident.feedback;
  BubbleCandidate(this.resident, this.positions, {this.displayReason});
}

class SpeechBubble {
  final String residentId;
  final Reason reason;
  Rect bounds;
  double until;
  SpeechBubble(this.residentId, this.reason, this.bounds, this.until);
}

// presentation-only time, screen-space layout and cooldowns never enter a save.
class SpeechDirector {
  double time = 0;
  final List<SpeechBubble> visible = [];
  final Map<String, double> _lastSpoken = {}, _lastComplaint = {};
  final Map<String, int> _lastSignal = {};
  final Map<Reason, double> _lastCause = {};
  final Map<Reason, String> _lastId = {};
  void update(
    double dt,
    List<BubbleCandidate> candidates, {
    List<Rect> avoid = const [],
  }) {
    time += dt.clamp(0.0, .1);
    final byId = {for (final c in candidates) c.resident.homeId: c};
    visible.removeWhere((b) {
      final c = byId[b.residentId];
      if (c == null ||
          !c.resident.active ||
          b.until <= time ||
          b.reason.problem && c.reason != b.reason) {
        return true;
      }
      final options = c.positions.where(
        (r) =>
            !avoid.any((a) => a.overlaps(r)) &&
            !visible.any(
              (other) => other != b && other.bounds.inflate(5).overlaps(r),
            ),
      );
      if (options.isEmpty) return true;
      b.bounds = options.first;
      return false;
    });
    final sorted = [...candidates]
      ..sort((a, b) {
        final priority = (b.reason.problem ? 1 : 0).compareTo(
          a.reason.problem ? 1 : 0,
        );
        if (priority != 0) return priority;
        final aTurn = a.resident.homeId.compareTo(_lastId[a.reason] ?? '') > 0,
            bTurn = b.resident.homeId.compareTo(_lastId[b.reason] ?? '') > 0;
        if (aTurn != bTurn) return aTurn ? -1 : 1;
        return a.resident.homeId.compareTo(b.resident.homeId);
      });
    while (visible.length < Rules.bubbleLimit) {
      final available = sorted.where((c) {
        final r = c.resident, reason = c.reason;
        if (!r.active ||
            reason == Reason.none ||
            visible.any((b) => b.reason == reason) ||
            time - (_lastCause[reason] ?? -1000) <
                (reason.problem
                    ? Rules.causeCooldown
                    : Rules.positiveCauseCooldown) ||
            visible.any((b) => b.residentId == r.homeId) ||
            time - (_lastSpoken[r.homeId] ?? -1000) < Rules.bubbleCooldown) {
          return false;
        }
        if (reason.problem &&
            time - (_lastComplaint['${r.homeId}:${reason.name}'] ?? -1000) <
                Rules.complaintCooldown) {
          return false;
        }
        if (!reason.problem && _lastSignal[r.homeId] == r.signalSerial) {
          return false;
        }
        return c.positions.any(
          (rect) =>
              !avoid.any((a) => a.overlaps(rect)) &&
              !visible.any((b) => b.bounds.inflate(5).overlaps(rect)),
        );
      }).toList();
      if (available.isEmpty) break;
      final positive = available.where((c) => !c.reason.problem);
      // leave room for a truthful positive even in a large unsettled village.
      final c =
          visible.length == Rules.bubbleLimit - 1 &&
              visible.every((b) => b.reason.problem) &&
              positive.isNotEmpty
          ? positive.first
          : available.first;
      final r = c.resident, reason = c.reason;
      final rect = c.positions.firstWhere(
        (rect) =>
            !avoid.any((a) => a.overlaps(rect)) &&
            !visible.any((b) => b.bounds.inflate(5).overlaps(rect)),
      );
      visible.add(
        SpeechBubble(r.homeId, reason, rect, time + Rules.bubbleSeconds),
      );
      _lastSpoken[r.homeId] = time;
      _lastId[reason] = r.homeId;
      _lastCause[reason] = time;
      if (reason.problem) {
        _lastComplaint['${r.homeId}:${reason.name}'] = time;
      } else {
        _lastSignal[r.homeId] = r.signalSerial;
      }
    }
  }
}
