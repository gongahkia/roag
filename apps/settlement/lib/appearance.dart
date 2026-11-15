// version-one assignments are explicit: extending the cast cannot reroll people.
abstract final class Appearance {
  static const bodies = ['plain-v1', 'longhair-v1', 'hood-v1', 'cap-v1'];
  static const palettes = ['cream', 'teal', 'clay', 'lilac'];
  static int identityHash(String id) =>
      id.codeUnits.fold(17, (n, c) => (n * 31 + c) % 2147483647);
  static String assignBody(String id) => switch (identityHash(id) & 3) {
    0 => 'plain-v1',
    1 => 'longhair-v1',
    2 => 'hood-v1',
    _ => 'cap-v1',
  };
  static String resolveBody(String id) => bodies.contains(id) ? id : 'plain-v1';
  static String sprite(String body, int palette) =>
      'person-${resolveBody(body)}-${palettes[palette.clamp(0, 3)]}';
  static String description(String body, int palette) =>
      '${switch (resolveBody(body)) {
        'longhair-v1' => 'Long hair',
        'hood-v1' => 'Hood',
        'cap-v1' => 'Cap',
        _ => 'Short hair',
      }} · ${palettes[palette.clamp(0, 3)]}';
}
