import 'colony.dart';
import 'resident_request.dart';
import 'residents.dart';
import 'rules.dart';

// dialogue selection is transient; only explicit commands write colony state.
class Conversation {
  final String resident, node;
  final Need? need;
  final String? event;
  const Conversation(
    this.resident, {
    this.node = 'hello',
    this.need,
    this.event,
  });
  Conversation at(String next, {Need? concern, String? civic}) => Conversation(
    resident,
    node: next,
    need: concern ?? need,
    event: civic ?? event,
  );
}

class DialogueContext {
  final Colony colony;
  final Conversation conversation;
  DialogueContext(this.colony, this.conversation);
  Resident? get person => colony.actor(conversation.resident);
  Need? get need => conversation.need;
  ResidentRequest? get request => need == null
      ? null
      : colony.requests.concern(conversation.resident, need!);
  ResidentRequest? get openRequest => colony.requests
      .forResident(conversation.resident)
      .where((r) => r.state == RequestState.accepted)
      .firstOrNull;
  ResidentRequest? get helped =>
      (colony.requests
              .forResident(conversation.resident)
              .where((r) => r.state == RequestState.resolved)
              .toList()
            ..sort((a, b) => b.closedAt!.compareTo(a.closedAt!)))
          .firstOrNull;
  bool get civicLive => colony.civicAvailable(conversation.event);
  bool get undecided => civicLive && colony.special!.civicChoice == null;
  bool get stillNeeds =>
      person != null && need != null && colony.hasNeed(person!, need!);
}

typedef DialogueLine = String Function(DialogueContext);
typedef DialogueCondition = bool Function(DialogueContext);

enum DialogueEffect { none, accept, withdraw, sponsor, ordinary, encore }

class DialogueChoice {
  final String id, label, next;
  final DialogueCondition? when;
  final DialogueLine? disabled;
  final DialogueEffect effect;
  const DialogueChoice(
    this.id,
    this.label,
    this.next, {
    this.when,
    this.disabled,
    this.effect = DialogueEffect.none,
  });
}

class DialogueNode {
  final String id;
  final DialogueLine line;
  final List<DialogueChoice> choices;
  const DialogueNode(this.id, this.line, this.choices);
}

class DialogueView {
  final String node, line;
  final List<(DialogueChoice, String?)> choices;
  const DialogueView(this.node, this.line, this.choices);
}

class DialogueResult {
  final Conversation? conversation;
  final bool changed;
  final String? notice;
  const DialogueResult(this.conversation, {this.changed = false, this.notice});
}

abstract final class Dialogues {
  static const close = DialogueChoice('close', 'Maybe later', 'close');
  static const back = DialogueChoice('back', 'How are you doing?', 'mood');
  static final nodes = <String, DialogueNode>{
    'visitor': DialogueNode(
      'visitor',
      (c) => c.colony.visits[c.conversation.resident] == null
          ? 'My visit has ended.'
          : c.colony.visitorStory(c.colony.visits[c.conversation.resident]!),
      [
        DialogueChoice(
          'encore',
          'Sponsor an encore · 2 coins',
          'visitor',
          effect: DialogueEffect.encore,
          when: (c) =>
              c.colony.visits[c.conversation.resident]?.role ==
                  VisitorRole.storyteller &&
              !(c.colony.visits[c.conversation.resident]?.encore ?? true),
          disabled: (c) => c.colony.canEncore(c.conversation.resident) ? '' : 'Wait until the storyteller is telling a story; 2 coins required.',
        ),
        close,
      ],
    ),
    'preparedness': DialogueNode('preparedness', (c) {
      final r = c.person!, town = c.colony;
      if (r.safetyRole == 'fire' || r.safetyRole == 'guard') {
        return r.activity == Activity.responding
            ? 'I reached the incident. I can help here while my station stays operational.'
            : 'I am following the legal roads to help. A broken crossing can stop me.';
      }
      if (r.activity == Activity.sheltering) {
        return 'I reached a Refuge. This is temporary shelter; my own cottage is still mine.';
      }
      if (r.safetyRole != null) {
        return 'I am finding a safe road or a Refuge with room. I cannot cross a broken bridge.';
      }
      if (!town.homeUsable(r)) {
        return 'My cottage needs repairs before I can return. Please use Repair when it is safe; I will keep my home.';
      }
      if (r.workId != null && !town.usable(r.workId)) {
        return 'My workplace is unavailable. Repair it when it is safe; I will return on my own.';
      }
      if ((town.status(r.homeId)?.repairs ?? 0) > 0) {
        return 'My cottage has been repaired. The same home, with a little history.';
      }
      final memory = town.rememberedBy(r.homeId);
      if (memory != null) return 'I remember: $memory';
      if (town.incidentHistory.isNotEmpty) {
        final h = town.incidentHistory.last;
        return 'The last ${h.kind} ended: ${h.outcome}. ${h.responders} responders reached it; ${h.sheltered} neighbours found shelter. Roads and staffed buildings make the difference.';
      }
      return 'A connected Firehouse trains two responders, a Watchpost one guard. A Refuge has eight temporary places. They act on their own when needed.';
    }, [back, close]),
    'heritage': DialogueNode(
      'heritage',
      (c) =>
          '${c.person!.peopleKind.label}: ${c.person!.peopleKind.effect} My own trait is ${c.person!.trait.label.toLowerCase()}.',
      [back, close],
    ),
    'hello': DialogueNode(
      'hello',
      (c) => c.helped?.need.thanks ?? 'Hello. It is good to stop for a moment.',
      [
        back,
        const DialogueChoice(
          'preparedness',
          'How is the town prepared?',
          'preparedness',
        ),
        const DialogueChoice(
          'heritage',
          'Tell me about your people.',
          'heritage',
        ),
        const DialogueChoice('recent', 'What have you been up to?', 'recent'),
        DialogueChoice(
          'need',
          'Is there anything you need?',
          'offer',
          when: (c) =>
              c.person != null &&
              (c.colony.needFor(c.person!) != null || c.openRequest != null),
        ),
        DialogueChoice(
          'civic',
          'About Market day…',
          'civic',
          when: (c) => c.colony.civicAvailable(c.colony.special?.id),
        ),
        close,
      ],
    ),
    'mood': DialogueNode(
      'mood',
      (c) {
        final r = c.person!;
        if (c.helped != null) return c.helped!.need.thanks;
        if (c.colony.needFor(r) != null) {
          return 'I am managing, but there is something the town could help with.';
        }
        if (c.colony.happiness(r) >= 70) {
          return 'I feel content here. My recent days have gone well.';
        }
        return 'I am ${r.activity == Activity.working ? 'working at the ${c.colony.kind(r.workId)!.label}' : 'following my daily routine'}. It is good to have a moment to talk.';
      },
      [
        const DialogueChoice('recent', 'Tell me about your day.', 'recent'),
        DialogueChoice(
          'need',
          'Anything I can help with?',
          'offer',
          when: (c) =>
              c.colony.needFor(c.person!) != null || c.openRequest != null,
        ),
        close,
      ],
    ),
    'recent': DialogueNode('recent', _experience, [
      DialogueChoice(
        'need',
        'Anything I can help with?',
        'offer',
        when: (c) =>
            c.colony.needFor(c.person!) != null || c.openRequest != null,
      ),
      back,
      close,
    ]),
    'offer': DialogueNode(
      'offer',
      (c) => c.request?.state == RequestState.accepted
          ? 'You said you would help me ${c.need!.label.toLowerCase()}. I will carry on with my routine; there is no deadline.'
          : c.request?.state == RequestState.resolved
          ? c.need!.thanks
          : c.stillNeeds
          ? c.need!.appeal
          : 'That concern has changed. Let us see how the day goes.',
      [
        DialogueChoice(
          'accept',
          "I'll see what I can do.",
          'promised',
          effect: DialogueEffect.accept,
          when: (c) => c.stillNeeds && c.request == null,
          disabled: (c) =>
              c.colony.requests.unavailable(c.conversation.resident, c.need!) ??
              '',
        ),
        DialogueChoice(
          'withdraw',
          'Withdraw my promise',
          'withdrawn',
          effect: DialogueEffect.withdraw,
          when: (c) => c.request?.state == RequestState.accepted,
        ),
        const DialogueChoice('decline', 'Not right now.', 'declined'),
        close,
      ],
    ),
    'promised': DialogueNode(
      'promised',
      (c) => c.request?.state == RequestState.resolved
          ? c.need!.thanks
          : 'Thank you. ${c.need == Need.social ? 'A real visit' : 'Actually reaching work'} will make the difference. There is no hurry.',
      [
        const DialogueChoice('done', 'Back to building', 'close'),
        DialogueChoice(
          'withdraw',
          'Withdraw my promise',
          'withdrawn',
          effect: DialogueEffect.withdraw,
          when: (c) => c.request?.state == RequestState.accepted,
        ),
      ],
    ),
    'declined': DialogueNode(
      'declined',
      (_) => 'All right. I will carry on. You can keep shaping the town in your own time.',
      [back, close],
    ),
    'withdrawn': DialogueNode(
      'withdrawn',
      (_) => 'All right. No promise, and no hard feelings.',
      [back, close],
    ),
    'civic': DialogueNode(
      'civic',
      (c) => !c.civicLive
          ? 'That Market day is no longer open for a decision.'
          : c.colony.special!.civicChoice != null
          ? 'We have already chosen ${c.colony.special!.civicChoice == 'sponsored' ? 'town hospitality' : 'an ordinary Market day'}. The day carries on.'
          : 'Shall we offer town hospitality? ${Rules.coins(Rules.civicCost)} coins buys +${Rules.civicBenefit} mood for ${Rules.civicDuration.toInt()} seconds for the next ${Rules.civicGuests} qualifying served visitors. Service must finish during this Market day; no visitor means no benefit. Ordinary trade stays the same.',
      [
        DialogueChoice(
          'sponsor',
          'Sponsor · ${Rules.coins(Rules.civicCost)} coins',
          'civic-done',
          effect: DialogueEffect.sponsor,
          when: (c) => c.undecided,
          disabled: (c) => c.colony.session.money < Rules.civicCost
              ? 'Not enough coins. You can let it happen normally.'
              : '',
        ),
        DialogueChoice(
          'ordinary',
          'Let it happen normally · Free',
          'civic-done',
          effect: DialogueEffect.ordinary,
          when: (c) => c.undecided,
        ),
        close,
      ],
    ),
    'civic-done': DialogueNode(
      'civic-done',
      (c) =>
          c.colony.special?.id == c.conversation.event &&
              c.colony.special!.civicChoice == 'sponsored'
          ? 'Town hospitality is arranged. Only actual qualifying service will earn its benefit.'
          : 'An ordinary Market day it is. No coins spent; the normal visits and trade continue.',
      [const DialogueChoice('done', 'Back to building', 'close')],
    ),
  };
  static String _experience(DialogueContext c) {
    final r = c.person!, now = c.colony.clock;
    if (r.trait.name == 'sociable' && now - r.lastSocial < Rules.recent) {
      return 'I finished a social visit recently. Being sociable, I get a little more out of that time together.';
    }
    if (r.awardedEvent != null) {
      return 'I joined an evening gathering. I remember actually meeting everyone there.';
    }
    if (now - r.lastWork < Rules.recent) {
      return 'I completed productive work recently. The coins went into the town treasury.';
    }
    if (now - r.lastSocial < Rules.recent) {
      return 'I completed a social visit recently. That time to relax helped.';
    }
    return switch (r.activity) {
      Activity.working =>
        'I am working at the ${c.colony.kind(r.workId)!.label}. The town earns coins only while I actually work.',
      Activity.workTravel => 'I am following a path to my assigned workplace. I have not arrived yet.',
      Activity.socialTravel => 'I am on my way to a social venue. I still need to arrive and spend time there.',
      Activity.socialising => 'I am spending time at a social venue. I have not finished this visit yet.',
      _ => 'I have been following my routine. I do not have a finished work or social visit to tell you about yet.',
    };
  }

  static DialogueView view(Colony colony, Conversation conversation) {
    final c = DialogueContext(colony, conversation);
    if (c.person == null) {
      return const DialogueView(
        'gone',
        'This resident is no longer in the town.',
        [(close, null)],
      );
    }
    final node = nodes[conversation.node] ?? nodes['hello']!;
    return DialogueView(node.id, node.line(c), [
      for (final choice in node.choices)
        if (choice.when?.call(c) ?? true)
          (
            choice,
            switch (choice.disabled?.call(c)) {
              null || '' => null,
              final reason => reason,
            },
          ),
    ]);
  }

  static DialogueResult choose(
    Colony colony,
    Conversation conversation,
    String choiceId,
  ) {
    final available = view(
      colony,
      conversation,
    ).choices.where((c) => c.$1.id == choiceId && c.$2 == null).firstOrNull;
    if (available == null) {
      return DialogueResult(
        conversation,
        notice: 'That choice is no longer available.',
      );
    }
    final choice = available.$1;
    var changed = false;
    switch (choice.effect) {
      case DialogueEffect.accept:
        changed =
            conversation.need != null &&
            colony.acceptRequest(conversation.resident, conversation.need!);
      case DialogueEffect.withdraw:
        final r = DialogueContext(colony, conversation).request;
        changed = r != null && colony.cancelRequest(r.id);
      case DialogueEffect.sponsor:
        changed =
            conversation.event != null &&
            colony.chooseCivic(conversation.event!, sponsor: true);
      case DialogueEffect.ordinary:
        changed =
            conversation.event != null &&
            colony.chooseCivic(conversation.event!, sponsor: false);
      case DialogueEffect.encore:
        changed = colony.encore(conversation.resident);
      case DialogueEffect.none:
        break;
    }
    if (choice.effect != DialogueEffect.none && !changed) {
      return DialogueResult(
        conversation,
        notice: 'The situation changed. No consequence was applied.',
      );
    }
    if (choice.next == 'close') return DialogueResult(null, changed: changed);
    final next = conversation.at(
      choice.next,
      concern: choice.next == 'offer'
          ? DialogueContext(colony, conversation).openRequest?.need ??
                colony.needFor(colony.actor(conversation.resident)!)
          : null,
      civic: choice.next == 'civic' ? colony.special?.id : null,
    );
    return DialogueResult(next, changed: changed);
  }
}
