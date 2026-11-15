import 'rules.dart';

enum Reason {
  none('', '', false),
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
