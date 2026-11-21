import 'dart:convert';

enum Terrain {
  ground,
  forest,
  marsh,
  water,
  rock;

  String get label => switch (this) {
    ground => 'Open ground',
    forest => 'Woodland',
    marsh => 'Wet ground',
    water => 'Water',
    rock => 'Rock',
  };
  bool get buildable => this == ground;
  bool get roadAllowed => this == ground || this == marsh;
  String get instruction => switch (this) {
    ground => 'Build beside a connected path.',
    forest => 'Clear the trees before building or laying a road.',
    marsh => 'Paths can cross wet ground; Clear prepares it for buildings.',
    water =>
      'Water needs a complete bank-to-bank Bridge; buildings stay on land.',
    rock => 'Impassable rock · build around it.',
  };
}

enum BridgeAxis { horizontal, vertical }

enum EnvironmentPreset {
  plains(
    'Gentle Plains',
    'Open meadows, scattered trees and an easy start.',
    16,
    9,
    4,
  ),
  woodlands(
    'Woodlands',
    'Forest clusters and small clearings. Clear land as you expand.',
    50,
    12,
    5,
  ),
  river(
    'River Country',
    'A winding river divides the land. Bridge short crossings to expand.',
    20,
    18,
    4,
    hasRiver: true,
  ),
  wetlands(
    'Wetlands',
    'Difficult · connected channels, marshy banks and small dry islands. Stone roads withstand flooding; bridges cross water.',
    30,
    57,
    3,
    hasRiver: true,
  ),
  highlands(
    'Highlands',
    'Difficult · rocky ridges separate wooded plateaus. Narrow passes reward alternative roads. No simulated elevation.',
    30,
    12,
    47,
  ),
  frontier(
    'Harsh Frontier',
    'Challenging · exposed river approaches, rock and woodland. Prepare reachable protection; construction prices stay ordinary.',
    42,
    25,
    25,
    hasRiver: true,
  ),
  pineUplands(
    'Pine Uplands',
    'Discoverable · dense conifer stands around rocky passes. Forestry thrives; winter snow slows exposed roads.',
    60,
    18,
    36,
  ),
  saltCoast(
    'Salt Coast',
    'Discoverable · a broad tidal shore and inland inlet divide wet ground. Stone approaches and short bridges keep trade moving.',
    22,
    45,
    14,
  );

  final String label, description;
  final int forestPercent, wetThreshold, rockThreshold;
  final bool hasRiver;
  const EnvironmentPreset(
    this.label,
    this.description,
    this.forestPercent,
    this.wetThreshold,
    this.rockThreshold, {
    this.hasRiver = false,
  });
}

// base-131 over trimmed UTF-8, modulo the same bounded prime used by simulation.
// decimal seeds use their integer value modulo that prime, including negatives.
String canonicalSeed(String input) {
  final text = input.trim();
  if (text.isEmpty ||
      text.length > 64 ||
      text.runes.any((r) => r < 32 || r == 127)) {
    throw const FormatException('Use 1–64 printable characters for the seed.');
  }
  return RegExp(r'^[+-]?[0-9]+$').hasMatch(text)
      ? BigInt.parse(text).toString()
      : text;
}

int seedNumber(String input) {
  final text = canonicalSeed(input);
  if (RegExp(r'^-?[0-9]+$').hasMatch(text)) {
    return (BigInt.parse(text) % BigInt.from(2147483647)).toInt();
  }
  var value = 0;
  for (final byte in utf8.encode(text)) {
    value = (value * 131 + byte) % 2147483647;
  }
  return value;
}

class GenerationInfo {
  static const currentVersion = 4;
  final String seedText;
  final EnvironmentPreset preset;
  final int version, environmentVariant;
  const GenerationInfo(
    this.seedText,
    this.preset,
    this.environmentVariant, {
    this.version = currentVersion,
  });
  int get seed => seedNumber(seedText);
  Map<String, Object> toJson() => {
    'version': version,
    'preset': preset.name,
    'seedText': seedText,
    'environmentVariant': environmentVariant,
    'configuration': {'width': 40, 'height': 30},
  };
  factory GenerationInfo.decode(dynamic d) {
    if (d is! Map ||
        d['version'] is! int ||
        d['version'] < 1 ||
        d['version'] > 1000000 ||
        d['seedText'] is! String ||
        canonicalSeed(d['seedText']) != d['seedText'] ||
        d['environmentVariant'] is! int ||
        d['environmentVariant'] < 0 ||
        d['environmentVariant'] > 3 ||
        d['configuration'] is! Map ||
        d['configuration']['width'] != 40 ||
        d['configuration']['height'] != 30) {
      throw const FormatException('Invalid generation metadata.');
    }
    final preset = EnvironmentPreset.values.firstWhere(
      (p) => p.name == d['preset'],
      orElse: () => throw const FormatException('Unknown environment preset.'),
    );
    // concrete lots are authoritative even when a newer generator version is stored.
    return GenerationInfo(
      d['seedText'],
      preset,
      d['environmentVariant'],
      version: d['version'],
    );
  }
}
