import 'dart:ui';

import 'reasons.dart';
import 'residents.dart';
import 'rules.dart';

class BubbleCandidate {
  final Resident resident;
  final List<Rect> positions;
  BubbleCandidate(this.resident, this.positions);
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
  String _lastId = '';
  void update(double dt, List<BubbleCandidate> candidates) {
    time += dt.clamp(0.0, .1);
    final byId = {for (final c in candidates) c.resident.homeId: c};
    visible.removeWhere((b) {
      final c = byId[b.residentId];
      if (c == null ||
          !c.resident.active ||
          b.until <= time ||
          b.reason.problem && c.resident.problem != b.reason) {
        return true;
      }
      final options = c.positions.where(
        (r) => !visible.any(
          (other) => other != b && other.bounds.inflate(5).overlaps(r),
        ),
      );
      if (options.isEmpty) return true;
      b.bounds = options.first;
      return false;
    });
    final sorted = [...candidates]
      ..sort((a, b) {
        final priority = (b.resident.problem.problem ? 1 : 0).compareTo(
          a.resident.problem.problem ? 1 : 0,
        );
        if (priority != 0) return priority;
        final aTurn = a.resident.homeId.compareTo(_lastId) > 0,
            bTurn = b.resident.homeId.compareTo(_lastId) > 0;
        if (aTurn != bTurn) return aTurn ? -1 : 1;
        return a.resident.homeId.compareTo(b.resident.homeId);
      });
    while (visible.length < Rules.bubbleLimit) {
      final available = sorted.where((c) {
        final r = c.resident, reason = r.feedback;
        if (!r.active ||
            reason == Reason.none ||
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
          (rect) => !visible.any((b) => b.bounds.inflate(5).overlaps(rect)),
        );
      }).toList();
      if (available.isEmpty) break;
      final positive = available.where(
        (c) => c.resident.problem == Reason.none && !c.resident.message.problem,
      );
      // leave room for a truthful positive even in a large unsettled village.
      final c =
          visible.length == Rules.bubbleLimit - 1 &&
              visible.every((b) => b.reason.problem) &&
              positive.isNotEmpty
          ? positive.first
          : available.first;
      final r = c.resident, reason = r.feedback;
      final rect = c.positions.firstWhere(
        (rect) => !visible.any((b) => b.bounds.inflate(5).overlaps(rect)),
      );
      visible.add(
        SpeechBubble(r.homeId, reason, rect, time + Rules.bubbleSeconds),
      );
      _lastSpoken[r.homeId] = time;
      _lastId = r.homeId;
      if (reason.problem) {
        _lastComplaint['${r.homeId}:${reason.name}'] = time;
      } else {
        _lastSignal[r.homeId] = r.signalSerial;
      }
    }
  }
}
