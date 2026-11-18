import 'rules.dart';

enum Reason {
  none('', '', false),
  incidentWarning(
    'There is trouble nearby.',
    'An active incident threatens this area. Residents seek safe roads or a reachable Refuge automatically.',
    true,
  ),
  safetyRoad(
    'Waiting on a safe road.',
    'No reachable Refuge place is available. This resident remains on a safe road and retains their cottage.',
    false,
  ),
  responseCut(
    'I cannot respond here.',
    'A response needs a working station and legal road or bridge access. Restore the affected connection or building.',
    true,
  ),
  fightingFire(
    'At the fire front.',
    'Reached the real site. Active flames are suppressed only while this responder remains here.',
    false,
  ),
  guarding(
    'Keeping watch here.',
    'A guard reached the threatened Farm or Market. The actual wildlife or brigand must be nearby before it can be driven away.',
    false,
  ),
  sheltered(
    'Safe in the Refuge.',
    'Actually reached a functioning Refuge with a reserved shelter place.',
    false,
  ),
  safeAgain(
    'It is safe to return.',
    'The incident has ended and this person has a usable home again.',
    false,
  ),
  homeDamaged(
    'My home needs repairs.',
    'The cottage is temporarily unusable; its resident identity and housing association are retained.',
    true,
  ),
  workDamaged(
    'My workplace needs repairs.',
    'Damage affects actual service or productive work. Use Repair after the site is safe.',
    true,
  ),
  visitorTrading(
    'A fine Market stop.',
    'The travelling merchant reached a staffed Market and is spending real time trading.',
    false,
  ),
  visitorTelling(
    'A story for the town.',
    'The storyteller reached a communal venue. Actual listeners can earn a modest mood benefit.',
    false,
  ),
  visitorLeaving(
    'Until we meet again.',
    'This guest finished their activity and is leaving by the roads.',
    false,
  ),
  homeCut(
    'My lane is cut off.',
    'Restore a path from this home to the arrival lane.',
    true,
  ),
  noWork(
    'Looking for work.',
    'Build or connect a Farm. Each has two work positions.',
    true,
  ),
  workFull(
    'The workplaces are fully staffed.',
    'Both work positions are assigned. Build or connect another Farm.',
    true,
  ),
  workCut(
    'I can’t reach my workplace.',
    'Restore a path between this home and its assigned workplace.',
    true,
  ),
  farmsCut(
    'The workplaces need a path.',
    'Workplaces exist, but none can be reached. Reconnect their paths to the arrival lane.',
    true,
  ),
  offWork(
    'Off to work.',
    'Following a valid route to an assigned workplace. Travel takes time.',
    false,
  ),
  workDone(
    'A good day’s work!',
    'Completed real productive time at a workplace. The earnings are in the treasury.',
    false,
  ),
  noLeisure(
    'Somewhere to relax would be lovely.',
    'Build or connect a Park or Hall, or staff a reachable Market.',
    true,
  ),
  socialFull(
    'Waiting for a visitor space.',
    'Actually waiting beside a full park. There are six social places; another reachable park can help.',
    true,
  ),
  socialCut(
    'I can’t reach a social venue.',
    'Restore a path to a Park, Hall or staffed Market.',
    true,
  ),
  marketUnstaffed(
    'Waiting for a vendor.',
    'This Market needs a present working vendor before a visit can be served.',
    true,
  ),
  socialDone(
    'That was lovely.',
    'Completed a qualifying social visit. Recent leisure improves happiness.',
    false,
  ),
  gathering(
    'Nice to see everyone.',
    'Arrived and joined the evening gathering. This person received its temporary benefit once.',
    false,
  ),
  requestThanks(
    'That helped. Thank you!',
    'A promised improvement led to actual work or a completed social visit. The small gratitude benefit is awarded once.',
    false,
  );

  final String bubble, _detail;
  final bool problem;
  String get detail => switch (this) {
    noWork =>
      'Build a Farm, Workshop or Market beside a connected path. Farms offer ${Rules.gardenCapacity} work positions; Workshops and Markets each offer one.',
    workFull => 'Reachable workplaces have their jobs assigned. Another connected Farm, Workshop or Market can help.',
    socialFull => 'Actually waiting at a full social venue. Parks, Halls and staffed Markets have bounded visitor spaces.',
    _ => _detail,
  };
  const Reason(this.bubble, this._detail, this.problem);
}
