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
  test('speech uses presentation seconds, caps visible bubbles and rotates with cooldowns', () {
    final d = SpeechDirector(),
        people = [for (var i = 0; i < 7; i++) person(i)];
    final candidates = [
      for (var i = 0; i < people.length; i++) at(people[i], i),
    ];
    d.update(.05, candidates);
    expect(d.visible.length, Rules.bubbleLimit);
    final first = d.visible.map((b) => b.residentId).toSet();
    advance(d, 2.8, candidates);
    expect(d.visible.map((b) => b.residentId).toSet(), first);
    advance(d, .4, candidates);
    expect(d.visible.every((b) => !first.contains(b.residentId)), isTrue);
    advance(d, 18, candidates);
    expect(
      d.visible.any((b) => first.contains(b.residentId)),
      isFalse,
      reason: 'same complaint has longer suppression than general cooldown',
    );
  });
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
