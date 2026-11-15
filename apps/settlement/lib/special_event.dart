enum SpecialKind { rain, harvest, marketDay }

class SpecialEvent {
  String get shortEffect => switch (kind) {
    SpecialKind.rain =>
      'Slower paths · Stone Roads stay quick · Halls preferred',
    SpecialKind.harvest => '+25% from actual Farm work',
    SpecialKind.marketDay =>
      'Staffed visits favoured · qualifying trade +0.25 coin',
  };
  final String id;
  final SpecialKind kind;
  final double starts, ends;
  SpecialEvent(this.id, this.kind, this.starts, this.ends);
  bool active(double clock) => clock >= starts && clock < ends;
  String get title => switch (kind) {
    SpecialKind.rain => 'Rain shower',
    SpecialKind.harvest => 'Good harvest',
    SpecialKind.marketDay => 'Market day',
  };
  String get effect => switch (kind) {
    SpecialKind.rain => 'Dirt and Garden Paths are 20% slower. Stone Roads stay quick; covered Halls are preferred.',
    SpecialKind.harvest => 'Actual Farm work earns 25% more. Connected Farms need workers, not just ownership.',
    SpecialKind.marketDay => 'Staffed Markets attract visits. The first qualifying trade per person/day earns an extra 0.25 coin.',
  };
  Map<String, Object> toJson() => {
    'id': id,
    'kind': kind.name,
    'starts': starts,
    'ends': ends,
  };
}
