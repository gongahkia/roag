import 'rules.dart';

enum Trait {
  brisk,
  sociable,
  greenThumb;

  String get label => switch (this) {
    brisk => 'Brisk',
    sociable => 'Sociable',
    greenThumb => 'Green thumb',
  };
  String get effect => switch (this) {
    brisk => 'Walks ${((Rules.briskSpeed - 1) * 100).round()}% faster.',
    sociable => '+${Rules.sociableBenefit} mood from a completed social visit.',
    greenThumb => 'Prefers a Farm when choosing an available job.',
  };
}

abstract final class ResidentIdentity {
  static int hash(String id, int salt) =>
      id.codeUnits.fold(salt, (n, c) => (n * 31 + c) % 2147483647);
  static const first = [
    'Ada',
    'Bram',
    'Cora',
    'Dara',
    'Ellis',
    'Fern',
    'Gus',
    'Hazel',
    'Ida',
    'Jory',
    'Kit',
    'Lina',
    'Milo',
    'Nora',
    'Owen',
    'Pip',
  ];
  static const last = [
    'Ash',
    'Birch',
    'Clover',
    'Dale',
    'Elm',
    'Field',
    'Glen',
    'Hill',
    'Ivy',
    'Lake',
    'Moss',
    'Oak',
    'Reed',
    'Vale',
    'Willow',
    'Wood',
  ];
  static String name(String id) {
    final h = hash(id, 739);
    return '${first[h & 15]} ${last[(h >> 4) & 15]}';
  }

  static Trait trait(String id) => switch (hash(id, 1879) % 3) {
    0 => Trait.brisk,
    1 => Trait.sociable,
    _ => Trait.greenThumb,
  };
}
