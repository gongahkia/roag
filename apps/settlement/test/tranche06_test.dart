import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:roag_settlement/build_art.dart';
import 'package:roag_settlement/colony.dart';
import 'package:roag_settlement/dialogue.dart';
import 'package:roag_settlement/model.dart';
import 'package:roag_settlement/resident_request.dart';
import 'package:roag_settlement/residents.dart';
import 'package:roag_settlement/rules.dart';
import 'package:roag_settlement/reasons.dart';
import 'package:roag_settlement/special_event.dart';

import 'colony_test.dart' show paint, advance;
import 'tranche05_test.dart' show town, until, id;

Colony needingTown() {
  final c = Colony(LandscapeSession(Landscape.create(202)))
    ..nextSpecialAt = 1e9;
  paint(c, Tool.cottage, const Cell(2, 14), const Cell(5, 14));
  advance(c, 12);
  return c;
}

void move(Colony c, Cell from, Cell to) {
  expect(c.session.relocate(evaluateMove(c.world, from, to)), isTrue);
  c.reconcile(c.session.world);
}

Colony marketDay() {
  final c = town();
  advance(c, 14);
  expect(c.beginSpecial(SpecialKind.marketDay, fixture: true), isTrue);
  return c;
}

void main() {
  test('all catalogue sprites use the same runtime building/road samples and verified frames', () {
    final manifest = jsonDecode(
      File('assets/manifest.json').readAsStringSync(),
    );
    for (final t in Tool.values.where((t) => t != Tool.remove)) {
      final scene = BuildArt.samples[t]!,
          cell = t.isRoad ? const Cell(1, 2) : const Cell(1, 1);
      final lot = scene.at(cell);
      expect(manifest['frames'][BuildArt.frameFor(t)], isNotNull);
      expect(lot.path, t.isRoad);
      expect(lot.road, t.roadKind);
      expect(lot.place, t.place);
      expect(lot.cottage != null, t == Tool.cottage);
      if (!t.isRoad) expect(scene.access(cell), isNotNull);
    }
    expect(
      manifest['frames']['coin']['source'],
      'assets/godot-node-icons/white/icon_coin.png',
    );
    expect(
      File('assets/images/coin.png').readAsBytesSync(),
      File('../../assets/godot-node-icons/white/icon_coin.png')
          .readAsBytesSync(),
    );
  });
  test('relocation vacates one site, preserves identity/variant/counter, replaces scenery and creates one free history entry', () {
    final c = needingTown(), from = c.world.cottages.keys.first;
    final scenery = c.world.replace(
      const Cell(6, 14),
      const Lot(grass: 1, scenery: Scenery.vegetables, trace: Trace.scar),
    );
    c.session.replace(scenery);
    c.reconcile(c.session.world);
    final original = c.world,
        source = original.cottages[from]!,
        money = c.session.money;
    final destination = const Cell(6, 14), counter = original.nextCottageId;
    final plan = evaluateMove(original, source, destination);
    expect(c.world.encode(), original.encode());
    expect(c.session.commit(plan), isFalse);
    move(c, source, destination);
    expect(c.world.cottages[from], destination);
    expect(c.world.at(source).trace, Trace.pad);
    expect(c.world.at(destination).cottage, original.at(source).cottage);
    expect(c.world.nextCottageId, counter);
    expect(c.session.money, money);
    expect(c.people.length, 4);
    expect(c.inactive, isEmpty);
    expect(c.session.undo(), isTrue);
    c.reconcile(c.session.world);
    expect(c.world.encode(), original.encode());
    expect(c.session.money, money);
    expect(c.session.redo(), isTrue);
    c.reconcile(c.session.world);
    expect(c.world.cottages[from], destination);
    expect(c.session.money, money);
  });
  test('every building kind relocates with the same identity, and blocked scenery rejects the move', () {
    final c = town();
    final sources = [c.world.cottages.values.first, ...c.world.places.values];
    for (final source in sources) {
      final before = c.world, lot = before.at(source);
      move(c, source, const Cell(17, 14));
      final moved = c.world.at(const Cell(17, 14));
      expect(
        (moved.cottageId, moved.placeId, moved.place),
        (lot.cottageId, lot.placeId, lot.place),
      );
      expect(c.session.undo(), isTrue);
      c.reconcile(c.session.world);
      expect(c.world.encode(), before.encode());
    }
    c.session.replace(
      c.world.replace(const Cell(17, 14), const Lot(scenery: Scenery.oak)),
    );
    c.reconcile(c.session.world);
    final before = c.encode();
    expect(
      evaluateMove(c.world, sources.first, const Cell(17, 14)).valid,
      isFalse,
    );
    expect(c.encode(), before);
  });
  test('moving an active gathering venue keeps event identity and already awarded benefits through Undo', () {
    final c = town();
    until(c, () => c.event?.state == 'active', 6000);
    final event = c.event!, source = c.world.places[event.green]!;
    final awards = event.awarded.toSet(),
        benefits = {
          for (final r in c.people.values) r.homeId: r.eventBenefitUntil,
        };
    move(c, source, const Cell(17, 14));
    expect(c.event!.id, event.id);
    expect(c.event!.state, 'ended');
    expect(c.event!.awarded, awards);
    for (final r in c.people.values) {
      expect(r.eventBenefitUntil, benefits[r.homeId]);
    }
    expect(() => Colony.decode(c.encode()), returnsNormally);
    c.session.undo();
    c.reconcile(c.session.world);
    expect(c.event!.state, 'ended');
    expect(c.event!.awarded, awards);
  });
  test('invalid, stale and cancelled relocation changes no authored or simulation state', () {
    final c = needingTown(),
        before = c.encode(),
        source = c.world.cottages.values.first;
    for (final target in [
      source,
      const Cell(3, 14),
      const Cell(0, 15),
      const Cell(20, 20),
      const Cell(-1, 0),
    ]) {
      expect(
        c.session.relocate(evaluateMove(c.world, source, target)),
        isFalse,
      );
      expect(c.encode(), before);
    }
    final preview = evaluateMove(c.world, source, const Cell(6, 14));
    expect(preview.valid, isTrue);
    expect(c.encode(), before);
    c.session.cancelStroke();
    expect(c.encode(), before);
    paint(c, Tool.path, const Cell(8, 15));
    final changed = c.encode();
    expect(c.session.relocate(preview), isFalse);
    expect(c.encode(), changed);
  });
  test('moving an active cottage retains the actual person and routes without teleporting; immediate checkpoint is valid', () {
    final c = town();
    advance(c, 11);
    final person = c.people.values.first,
        old = c.world.cottages[person.homeId]!;
    final identity = (
      person.name,
      person.bodyId,
      person.palette,
      person.trait,
      person.earned,
      person.workedSeconds,
    );
    final position = (person.x, person.y), revision = c.navigation.revision;
    move(c, old, const Cell(17, 14));
    expect(identical(c.people[person.homeId], person), isTrue);
    expect((person.x, person.y), position);
    expect((
      person.name,
      person.bodyId,
      person.palette,
      person.trait,
      person.earned,
      person.workedSeconds,
    ), identity);
    expect(c.navigation.revision, revision + 1);
    expect(
      Colony.decode(c.encode()).world.cottages[person.homeId],
      const Cell(17, 14),
    );
    advance(c, 20);
    expect(c.people[person.homeId], same(person));
    expect(
      c.world.pathRoute(
        person.cell,
        c.world.access(c.world.cottages[person.homeId]!)!,
      ),
      isNotNull,
    );
  });
  test('moving an occupied workplace invalidates routes and productive presence before save without extra pay', () {
    final c = town();
    until(c, () => c.workers(id(c, Place.garden)) == 2);
    final workers = c.people.values
        .where((r) => r.workId == id(c, Place.garden))
        .toList();
    final earnings = c.workEarned,
        clock = c.clock,
        positions = {for (final r in workers) r.homeId: (r.x, r.y)};
    final from = c.world.places[id(c, Place.garden)]!,
        key = id(c, Place.garden);
    move(c, from, const Cell(17, 14));
    expect(c.workEarned, earnings);
    expect(c.clock, clock);
    expect(c.workers(key), 0);
    expect(workers.every((r) => positions[r.homeId] == (r.x, r.y)), isTrue);
    expect(workers.every((r) => r.workId == key), isTrue);
    final copy = Colony.decode(c.encode());
    expect(copy.world.places[key], const Cell(17, 14));
    c.update(.05);
    expect(c.workEarned, greaterThanOrEqualTo(earnings));
    expect(
      workers.every(
        (r) => r.activity != Activity.working || r.cell == c.placeAccess(key),
      ),
      isTrue,
    );
    until(c, () => c.workers(key) > 0);
    expect(c.workEarned, greaterThan(earnings));
  });
  test('moving an occupied social destination neither teleports visitors nor reissues their benefits', () {
    final c = town();
    until(c, () => c.venues.any((p) => c.occupancy(p.key) > 0));
    final venue = c.venues.firstWhere((p) => c.occupancy(p.key) > 0);
    final before = c.people.values
        .where((r) => r.destination == venue.key)
        .toList();
    final records = {
      for (final r in before) r.homeId: (r.x, r.y, r.lastSocial, r.tradeDay),
    };
    move(c, venue.value, const Cell(17, 14));
    for (final r in before) {
      expect((r.x, r.y, r.lastSocial, r.tradeDay), records[r.homeId]);
    }
    expect(c.occupancy(venue.key), 0);
    expect(() => Colony.decode(c.encode()), returnsNormally);
  });
  test('relocation history preserves forward earnings, event identity and exact terrain at both sites', () {
    final c = marketDay(), event = c.special!.id;
    final from = c.world.places[id(c, Place.market)]!, original = c.world;
    final destination = const Cell(17, 14);
    move(c, from, destination);
    advance(c, 1);
    final money = c.session.money, clock = c.clock, work = c.workEarned;
    expect(c.session.undo(), isTrue);
    c.reconcile(c.session.world);
    expect(c.world.encode(), original.encode());
    expect(c.session.money, money);
    expect(c.clock, clock);
    expect(c.special!.id, event);
    expect(c.workEarned, work);
    expect(c.session.redo(), isTrue);
    c.reconcile(c.session.world);
    expect(c.session.money, money);
    expect(c.special!.id, event);
    expect(
      Colony.decode(c.encode()).world.places[id(c, Place.market)],
      destination,
    );
  });
  test('viewing all relevant dialogue branches changes no gameplay, RNG, request or money state', () {
    final c = needingTown(), person = c.people.values.first;
    final saved = c.encode(),
        identity = (
          person.homeId,
          person.name,
          person.bodyId,
          person.palette,
          person.trait,
        );
    var chat = Conversation(person.homeId);
    expect(
      Dialogues.view(c, chat).choices.any((x) => x.$1.id == 'need'),
      isTrue,
    );
    chat = Dialogues.choose(c, chat, 'back').conversation!;
    chat = Dialogues.choose(c, chat, 'recent').conversation!;
    chat = Dialogues.choose(c, chat, 'need').conversation!;
    expect(Dialogues.view(c, chat).line, contains('looking for work'));
    chat = Dialogues.choose(c, chat, 'decline').conversation!;
    expect(Dialogues.view(c, chat).line, contains('own time'));
    expect(c.encode(), saved);
    expect(c.requests.records, isEmpty);
    expect((
      person.homeId,
      person.name,
      person.bodyId,
      person.palette,
      person.trait,
    ), identity);
  });
  test('request acceptance is explicit, bounded, contextual and reversible only by withdrawal', () {
    final c = needingTown(), ids = c.people.keys.toList();
    for (final key in ids.take(3)) {
      expect(c.acceptRequest(key, Need.work), isTrue);
    }
    expect(c.acceptRequest(ids.last, Need.work), isFalse);
    expect(c.acceptRequest(ids.first, Need.work), isFalse);
    final request = c.requests.forResident(ids.first).first;
    expect(c.cancelRequest(request.id), isTrue);
    expect(c.requests.mood(ids.first, c.clock), 0);
    expect(c.cancelRequest(request.id), isFalse);
    expect(c.acceptRequest(ids.first, Need.work), isFalse);
    expect(c.acceptRequest(ids.last, Need.work), isTrue);
    expect(c.requests.open.length, 3);
    expect(Colony.decode(c.encode()).requests.open.length, 3);
  });
  test('a work request resolves only from its actual resident productive tick, once across history and reload', () {
    final c = needingTown(), r = c.people.values.first;
    expect(c.acceptRequest(r.homeId, Need.work), isTrue);
    final request = c.requests.forResident(r.homeId).first;
    paint(c, Tool.green, const Cell(6, 14));
    advance(c, 2);
    expect(request.state, RequestState.accepted);
    expect(c.requests.mood(r.homeId, c.clock), 0);
    paint(c, Tool.garden, const Cell(7, 14));
    paint(c, Tool.garden, const Cell(7, 16));
    until(c, () => request.state == RequestState.resolved);
    expect(r.workedSeconds, greaterThan(0));
    expect(c.requests.mood(r.homeId, c.clock), Rules.requestBenefit);
    final end = request.rewardUntil;
    c.session.undo();
    c.reconcile(c.session.world);
    expect(request.state, RequestState.resolved);
    if (r.workId == null) expect(r.workIssue, Reason.noWork);
    c.session.redo();
    c.reconcile(c.session.world);
    advance(c, 15);
    final copy = Colony.decode(c.encode());
    expect(copy.requests.concern(r.homeId, Need.work)!.rewardUntil, end);
    expect(copy.acceptRequest(r.homeId, Need.work), isFalse);
    advance(copy, 80);
    expect(copy.requests.mood(r.homeId, copy.clock), 0);
  });
  test('an unreachable-work request is not satisfied by an unrelated Farm, only actual regained work', () {
    final c = town();
    until(c, () => c.workers(id(c, Place.garden)) == 2);
    final r = c.people.values.firstWhere(
      (r) => r.activity == Activity.working && r.workId == id(c, Place.garden),
    );
    final worked = r.workedSeconds;
    paint(c, Tool.remove, const Cell(5, 15));
    expect(c.hasNeed(r, Need.workAccess), isTrue);
    expect(c.acceptRequest(r.homeId, Need.workAccess), isTrue);
    final request = c.requests.concern(r.homeId, Need.workAccess)!;
    paint(c, Tool.garden, const Cell(1, 14));
    advance(c, 2);
    expect(request.state, RequestState.accepted);
    expect(r.workedSeconds, worked);
    paint(c, Tool.path, const Cell(5, 15));
    until(c, () => request.state == RequestState.resolved);
    expect(r.workedSeconds, greaterThan(worked));
  });
  test('social requests require a completed real visit and do not complete on construction or arrival', () {
    final c = needingTown(), r = c.people.values.first;
    until(c, () => c.hasNeed(r, Need.social));
    expect(c.acceptRequest(r.homeId, Need.social), isTrue);
    final request = c.requests.concern(r.homeId, Need.social)!;
    paint(c, Tool.green, const Cell(6, 14));
    expect(request.state, RequestState.accepted);
    until(c, () => r.activity == Activity.socialising);
    expect(request.state, RequestState.accepted);
    until(c, () => request.state == RequestState.resolved);
    expect(r.lastSocial, greaterThanOrEqualTo(request.acceptedAt));
    expect(
      Dialogues.view(c, Conversation(r.homeId)).line,
      contains('proper visit'),
    );
  });
  test('removing a home withdraws its open promise without reward; Undo restores the same person but not a promise rewind', () {
    final c = needingTown(), r = c.people.values.first;
    c.acceptRequest(r.homeId, Need.work);
    final request = c.requests.forResident(r.homeId).first;
    paint(c, Tool.remove, c.world.cottages[r.homeId]!);
    expect(request.state, RequestState.cancelled);
    expect(() => Colony.decode(c.encode()), returnsNormally);
    c.session.undo();
    c.reconcile(c.session.world);
    expect(c.people[r.homeId], same(r));
    expect(request.state, RequestState.cancelled);
  });
  test('dialogue experiences reflect real completed work, visits and gathering participation', () {
    final c = town(), r = c.people.values.first;
    expect(
      Dialogues.view(c, Conversation(r.homeId, node: 'recent')).line,
      contains('not have a finished'),
    );
    until(c, () => r.lastWork >= 0 || r.lastSocial >= 0);
    final line = Dialogues.view(c, Conversation(r.homeId, node: 'recent')).line;
    expect(line, anyOf(contains('completed'), contains('finished')));
    expect(r.lastWork >= 0 || r.lastSocial >= 0, isTrue);
    final gathering = Colony.decode(
      File('../../docs/world-builder/evidence/tranche-03/colony-gathering.json')
          .readAsStringSync(),
    );
    final attendee = gathering.people.values.firstWhere(
      (p) => p.awardedEvent != null && p.trait.name != 'sociable',
    );
    expect(
      Dialogues.view(
        gathering,
        Conversation(attendee.homeId, node: 'recent'),
      ).line,
      contains('gathering'),
    );
  });
  test('Market-day branches charge once and benefit only bounded actual qualifying service', () {
    final ordinary = marketDay(),
        sponsored = Colony.decode(ordinary.encode()),
        key = ordinary.special!.id;
    expect(ordinary.chooseCivic(key, sponsor: false), isTrue);
    expect(sponsored.chooseCivic(key, sponsor: true), isTrue);
    expect(sponsored.chooseCivic(key, sponsor: true), isFalse);
    expect(sponsored.civicSpent, Rules.civicCost);
    expect(sponsored.special!.civicAwarded, isEmpty);
    expect(
      sponsored.people.values.every((r) => r.civicBenefitUntil < 0),
      isTrue,
    );
    advance(ordinary, 24);
    advance(sponsored, 24);
    expect(sponsored.tradeEarned, ordinary.tradeEarned);
    expect(sponsored.session.money + Rules.civicCost, ordinary.session.money);
    expect(
      sponsored.special!.civicAwarded.length,
      inInclusiveRange(1, Rules.civicGuests),
    );
    for (final guest in sponsored.special!.civicAwarded) {
      final r = sponsored.people[guest]!;
      expect(r.tradeDay, sponsored.day);
      expect(r.lastShopping, greaterThanOrEqualTo(sponsored.special!.starts));
      expect(
        r.civicBenefitUntil,
        closeTo(r.lastShopping + Rules.civicDuration, .001),
      );
    }
    final copy = Colony.decode(sponsored.encode());
    expect(copy.special!.civicAwarded, sponsored.special!.civicAwarded);
    expect(copy.chooseCivic(key, sponsor: true), isFalse);
    expect(copy.civicSpent, Rules.civicCost);
  });
  test('sponsored hospitality reaches its six-person cap through real customer service', () {
    final c = town();
    paint(c, Tool.cottage, const Cell(6, 16), const Cell(16, 16));
    advance(c, 14);
    expect(c.beginSpecial(SpecialKind.marketDay, fixture: true), isTrue);
    expect(c.chooseCivic(c.special!.id, sponsor: true), isTrue);
    advance(c, 24.9);
    expect(c.special!.civicAwarded.length, Rules.civicGuests);
    expect(
      c.people.values.where((r) => r.civicBenefitUntil >= 0).length,
      Rules.civicGuests,
    );
    expect(
      c.special!.civicAwarded.every((id) => c.people[id]!.tradeDay == c.day),
      isTrue,
    );
    expect(() => Colony.decode(c.encode()), returnsNormally);
  });
  test('paused multi-home demolition removes dead route owners and immediately exports a valid checkpoint', () {
    final c = marketDay(), host = c.civicHost!, clock = c.clock;
    for (final resident in c.people.keys.where((id) => id != host).toList()) {
      paint(c, Tool.remove, c.world.cottages[resident]!);
      expect(c.planning.every((p) => c.people.containsKey(p.id)), isTrue);
      expect(c.planning.length, lessThanOrEqualTo(c.people.length));
      expect(() => Colony.decode(c.encode()), returnsNormally);
    }
    expect(c.clock, clock);
    expect(Colony.decode(c.encode()).people.keys, [host]);
    c.session.undo();
    c.reconcile(c.session.world);
    expect(() => Colony.decode(c.encode()), returnsNormally);
  });
  test('a Market day with only its vendor no longer offers or charges a civic choice', () {
    final c = marketDay(), host = c.civicHost!;
    for (final resident in c.people.keys.where((id) => id != host).toList()) {
      paint(c, Tool.remove, c.world.cottages[resident]!);
    }
    expect(c.civicHost, host);
    expect(c.eligibleSpecial(SpecialKind.marketDay), isFalse);
    final before = c.encode();
    expect(
      Dialogues.view(
        c,
        Conversation(host),
      ).choices.any((x) => x.$1.id == 'civic'),
      isFalse,
    );
    expect(
      Dialogues.view(
        c,
        Conversation(host, node: 'civic', event: c.special!.id),
      ).choices.any((x) => x.$1.id == 'sponsor'),
      isFalse,
    );
    expect(c.chooseCivic(c.special!.id, sponsor: true), isFalse);
    expect(c.encode(), before);
  });
  test('civic sponsorship rejects stale events, insufficient funds and missing participants without spending', () {
    final c = marketDay(), event = c.special!.id, host = c.civicHost!;
    c.session.money = Rules.civicCost - 1;
    final before = c.encode();
    final chat = Conversation(host, node: 'civic', event: event);
    expect(
      Dialogues.view(
        c,
        chat,
      ).choices.firstWhere((x) => x.$1.id == 'sponsor').$2,
      contains('Not enough'),
    );
    expect(c.chooseCivic(event, sponsor: true), isFalse);
    expect(c.encode(), before);
    expect(c.chooseCivic('special-obsolete', sponsor: false), isFalse);
    paint(c, Tool.remove, c.world.places[id(c, Place.market)]!);
    expect(c.chooseCivic(event, sponsor: false), isFalse);
    expect(c.civicSpent, 0);
    expect(c.special!.civicChoice, isNull);
  });
  test('sponsored civic consequences survive unrelated construction Undo and export/import without another payment', () {
    final c = marketDay();
    paint(c, Tool.gardenPath, const Cell(3, 15));
    c.chooseCivic(c.special!.id, sponsor: true);
    final before = c.session.money;
    c.session.undo();
    c.reconcile(c.session.world);
    expect(c.session.money, before + Rules.cost('gardenPath'));
    expect(c.civicSpent, Rules.civicCost);
    expect(c.special!.civicChoice, 'sponsored');
    c.session.redo();
    c.reconcile(c.session.world);
    expect(c.session.money, before);
    final copy = Colony.decode(c.encode());
    expect(copy.chooseCivic(c.special!.id, sponsor: true), isFalse);
    expect(copy.session.money, before);
    expect(copy.special!.civicChoice, 'sponsored');
  });
  test('past civic decisions and remaining benefits survive the next event and cannot be charged again', () {
    final c = marketDay(), original = c.special!.id;
    c.session.unlimited = true;
    final money = c.session.money;
    expect(c.chooseCivic(original, sponsor: true), isTrue);
    expect(c.session.money, money - Rules.civicCost);
    until(c, () => c.special!.civicAwarded.isNotEmpty);
    final guest = c.special!.civicAwarded.first,
        benefit = c.people[c.special!.civicAwarded.first]!.civicBenefitUntil;
    advance(c, 45);
    expect(c.special!.id, isNot(original));
    expect(c.civicDecisions[original], 'sponsored');
    expect(c.people[guest]!.civicBenefitUntil, benefit);
    final copy = Colony.decode(c.encode());
    expect(copy.civicDecisions[original], 'sponsored');
    final before = copy.session.money;
    expect(copy.chooseCivic(original, sponsor: true), isFalse);
    expect(copy.session.money, before);
    expect(copy.civicSpent, Rules.civicCost);
  });
  test('old checkpoints have no fabricated conversations, requests, civic decisions or benefits', () {
    final text = File(
      '../../docs/world-builder/evidence/tranche-05/large-85.json',
    ).readAsStringSync();
    final c = Colony.decode(text), old = jsonDecode(text);
    expect(c.requests.records, isEmpty);
    expect(c.civicSpent, 0);
    expect(c.special?.civicChoice, isNull);
    expect(c.people.values.every((r) => r.civicBenefitUntil == -1), isTrue);
    final current = jsonDecode(c.encode());
    expect(current['lots'], old['lots']);
    expect(current['colony']['money'], old['colony']['money']);
    expect(current['colony']['residents'], old['colony']['residents']);
  });
  test('malformed request rewards and civic decisions are rejected before replacing a save', () {
    final c = needingTown(), r = c.people.values.first;
    c.acceptRequest(r.homeId, Need.work);
    for (final mutate in <void Function(dynamic)>[
      (d) =>
          d['colony']['requests']['records'][0]['rewardUntil'] = c.clock + 75,
      (d) => d['colony']['requests']['records'][0]['resident'] = 'missing',
      (d) => d['colony']['requests']['records'].add(
        d['colony']['requests']['records'][0],
      ),
      (d) => d['colony']['requests']['nextId'] = 1,
    ]) {
      final d = jsonDecode(c.encode());
      mutate(d);
      expect(() => Colony.decode(jsonEncode(d)), throwsFormatException);
    }
    final day = marketDay(), d = jsonDecode(day.encode());
    d['colony']['special']['civicChoice'] = 'sponsored';
    expect(() => Colony.decode(jsonEncode(d)), throwsFormatException);
  });
}
