import 'appearance.dart';

enum PeopleKind {
  human('human-v1', 'Human', 'The established individual routines and traits.'),
  imp(
    'imp-v1',
    'Imp',
    'Prefers an available Workshop when choosing new work. Personal traits still matter.',
  ),
  djinn(
    'djinn-v1',
    'Djinn',
    'Prefers a reachable Community Hall for leisure. Work and personal traits stay independent.',
  );

  final String id, label, effect;
  const PeopleKind(this.id, this.label, this.effect);
  String body(String identity) => switch (this) {
    human => Appearance.assignBody(identity),
    imp => 'imp-body-v1',
    djinn => 'djinn-body-v1',
  };
  static PeopleKind decode(dynamic id) => values.firstWhere(
    (p) => p.id == id,
    orElse: () => throw const FormatException('Unknown people type.'),
  );
  bool compatible(String body) => switch (this) {
    human => ![
      'imp-body-v1',
      'djinn-body-v1',
      'merchant-body-v1',
      'storyteller-body-v1',
    ].contains(body),
    imp => body == 'imp-body-v1',
    djinn => body == 'djinn-body-v1',
  };
  static PeopleKind arrival(int seed, String identity) {
    var n = identity.codeUnits.fold(
      (seed + 49999) % 2147483647,
      (a, b) => (a * 131 + b) % 2147483647,
    );
    n ^= n >> 16;
    n = n * 65599 % 2147483647;
    n ^= n >> 13;
    n = n * 32749 % 2147483647;
    n ^= n >> 16;
    final roll = n % 100;
    return roll < 60
        ? human
        : roll < 80
        ? imp
        : djinn;
  }

  String newName(String identity) {
    final h = Appearance.identityHash(identity);
    final first = this == imp
        ? ['Brin', 'Vexa', 'Tavi', 'Nyx']
        : ['Safa', 'Nima', 'Rami', 'Luma'];
    final last = this == imp
        ? ['Ember', 'Spark', 'Ash', 'Glow']
        : ['Mist', 'Breeze', 'Cloud', 'Dawn'];
    return '${first[h % 4]} ${last[h ~/ 4 % 4]}';
  }
}
