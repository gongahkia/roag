import 'dart:ui';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/residents.dart';
import 'package:roag_settlement/reasons.dart';
import 'package:roag_settlement/speech.dart';
import 'package:roag_settlement/rules.dart';

Resident person(int i) =>
    Resident('home-$i', const Cell(1, 15), Activity.home)
      ..problem = Reason.noWork;
BubbleCandidate at(Resident r, int i) =>
    BubbleCandidate(r, [Rect.fromLTWH(i * 230.0, 20, 210, 44)]);
void advance(SpeechDirector d, double seconds, List<BubbleCandidate> c) {
  for (var i = 0; i < (seconds / .05).round(); i++) {
    d.update(.05, c);
  }
}

void main() {
  test('cause suppression bounds repetition across 85 residents and rotates fairly', () {
    final d = SpeechDirector(),
        people = [for (var i = 0; i < 85; i++) person(i)];
    final candidates = [for (final r in people) at(r, 0)];
    d.update(.05, candidates);
    expect(
      d.visible.length,
      1,
      reason: 'one shared cause, however many people',
    );
    final spoken = <String>{d.visible.single.residentId};
    advance(d, 2.8, candidates);
    expect(d.visible.length, 1);
    advance(d, .4, candidates);
    expect(d.visible, isEmpty);
    advance(d, 20, candidates);
    expect(d.visible, isEmpty, reason: 'no complaint conveyor belt');
    for (var i = 1; i < 85; i++) {
      advance(d, 1.0, candidates);
      while (d.visible.isEmpty) {
        advance(d, .1, candidates);
      }
      expect(
        spoken.add(d.visible.single.residentId),
        isTrue,
        reason: 'each eligible person gets a turn before anyone repeats',
      );
      advance(d, Rules.bubbleSeconds + .1, candidates);
    }
    expect(spoken.length, 85);
  });
  test(
    'two different truthful causes fit and blocked UI areas are avoided',
    () {
      final d = SpeechDirector(),
          a = person(1),
          b = person(2)..problem = Reason.socialCut;
      final rect = Rect.fromLTWH(20, 20, 150, 30);
      d.update(
        .05,
        [
          BubbleCandidate(a, [rect, rect.translate(0, 70)]),
          at(b, 1),
        ],
        avoid: [rect],
      );
      expect(d.visible.length, Rules.bubbleLimit);
      expect(d.visible.every((b) => !b.bounds.overlaps(rect)), isTrue);
      expect(d.visible.map((b) => b.reason).toSet().length, 2);
      a.problem = Reason.none;
      d.update(.05, [at(a, 0), at(b, 1)]);
      expect(d.visible.every((b) => b.residentId != a.homeId), isTrue);
    },
  );
  test(
    'resolved complaints, off-screen people and inactive owners clear promptly',
    () {
      final d = SpeechDirector(), r = person(1);
      d.update(.05, [at(r, 0)]);
      expect(d.visible.length, 1);
      r.problem = Reason.none;
      d.update(.05, [at(r, 0)]);
      expect(d.visible, isEmpty);
      r.problem = Reason.homeCut;
      advance(d, 21, [at(r, 0)]);
      expect(d.visible, isNotEmpty);
      d.update(.05, []);
      expect(d.visible, isEmpty);
      advance(d, 61, [at(r, 0)]);
      r.active = false;
      d.update(.05, [at(r, 0)]);
      expect(d.visible, isEmpty);
    },
  );
  test('overlapping candidates are deferred and positives can share the visible limit', () {
    final d = SpeechDirector(),
        people = [for (var i = 0; i < 5; i++) person(i)];
    final same = [for (final r in people) at(r, 0)];
    d.update(.05, same);
    expect(d.visible.length, 1);
    final positive = person(8)
      ..problem = Reason.none
      ..say(Reason.workDone);
    final mixed = SpeechDirector();
    mixed.update(.05, [
      for (var i = 0; i < people.length; i++) at(people[i], i),
      at(positive, 6),
    ]);
    expect(mixed.visible.any((b) => b.reason == Reason.workDone), isTrue);
    for (final a in mixed.visible) {
      for (final b in mixed.visible) {
        if (a != b) expect(a.bounds.overlaps(b.bounds), isFalse);
      }
    }
  });
  test('speech display does not mutate resident activity, reasons, money or history', () {
    final r = person(1)..say(Reason.offWork), d = SpeechDirector();
    final session = LandscapeSession(Landscape.create());
    final before = session.snapshot().text;
    advance(d, 100, [at(r, 0)]);
    expect(r.activity, Activity.home);
    expect(r.problem, Reason.noWork);
    expect(r.message, Reason.offWork);
    expect(session.snapshot().text, before);
    expect(session.money, Rules.startingMoney);
    expect(session.canUndo, isFalse);
  });
  test(
    'a delayed journey utterance cannot start after its activity has changed',
    () {
      final r = person(1)
        ..problem = Reason.none
        ..say(Reason.offWork);
      final d = SpeechDirector();
      d.update(.05, [at(r, 0)]);
      expect(d.visible, isEmpty);
      r.activity = Activity.workTravel;
      d.update(.05, [at(r, 0)]);
      expect(d.visible.single.reason, Reason.offWork);
      r.activity = Activity.working;
      advance(d, 2.8, [at(r, 0)]);
      expect(
        d.visible.length,
        1,
        reason: 'an utterance already made retains readable presentation time',
      );
      advance(d, .3, [at(r, 0)]);
      expect(d.visible, isEmpty);
      advance(d, 30, [at(r, 0)]);
      expect(d.visible, isEmpty);
    },
  );
}
