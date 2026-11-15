import 'rules.dart';

enum Reason {
  none('', '', false),
  homeCut(
    'My lane is cut off.',
    'Restore a path from this home to the arrival lane.',
    true,
  ),
  noWork(
    'Looking for farm work.',
    'Build or connect a Farm. Each has two work positions.',
    true,
  ),
  workFull(
    'The farms are fully staffed.',
    'Both work positions are assigned. Build or connect another Farm.',
    true,
  ),
  workCut(
    'I can’t reach my farm.',
    'Restore a path between this home and its assigned farm.',
    true,
  ),
  farmsCut(
    'The farms need a path.',
    'Farms exist, but none can be reached. Reconnect their paths to the arrival lane.',
    true,
  ),
  offWork(
    'Off to the farm.',
    'Following a valid route to an assigned farm. Travel takes time.',
    false,
  ),
  workDone(
    'A good day’s work!',
    'Completed real productive time in a farm. The earnings are in the treasury.',
    false,
  ),
  noLeisure(
    'A park would be lovely.',
    'Build or connect a Park for a social visit.',
    true,
  ),
  socialFull(
    'Waiting for room at the park.',
    'Actually waiting beside a full park. There are six social places; another reachable park can help.',
    true,
  ),
  socialCut('I can’t reach the park.', 'Restore a path to the Park.', true),
  socialDone(
    'That was lovely.',
    'Completed a qualifying social visit. Recent leisure improves happiness.',
    false,
  ),
  gathering(
    'Nice to see everyone.',
    'Arrived and joined the evening gathering. This person received its temporary benefit once.',
    false,
  );

  final String bubble, _detail;
  final bool problem;
  String get detail => switch (this) {
    noWork =>
      'Build a Farm beside a connected path. Each has ${Rules.gardenCapacity} work positions.',
    workFull =>
      'Reachable Farms have all work positions assigned (${Rules.gardenCapacity} per Farm). Another connected Farm can help.',
    socialFull =>
      'Actually waiting beside a full Park (${Rules.greenCapacity} visitor spaces). Another reachable Park can help.',
    _ => _detail,
  };
  const Reason(this.bubble, this._detail, this.problem);
}
