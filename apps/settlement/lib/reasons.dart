enum Reason {
  none('', '', false),
  homeCut(
    'My lane is cut off.',
    'Restore a path from this home to the arrival lane.',
    true,
  ),
  noWork(
    'I’d like somewhere to work.',
    'Build or connect a Productive Garden. Each has two work positions.',
    true,
  ),
  workFull(
    'All the garden jobs are taken.',
    'Both work positions are assigned. Build or connect another Productive Garden.',
    true,
  ),
  workCut(
    'I can’t reach my garden.',
    'Restore a path between this home and its assigned garden.',
    true,
  ),
  offWork(
    'Off to the garden.',
    'Following a valid route to an assigned garden. Travel takes time.',
    false,
  ),
  workDone(
    'A good day’s work!',
    'Completed real productive time in a garden. The earnings are in the treasury.',
    false,
  ),
  noLeisure(
    'Somewhere to relax would be nice.',
    'Build or connect a Village Green for a social visit.',
    true,
  ),
  socialFull(
    'No space here yet.',
    'Actually waiting beside a full green. There are six social places; another reachable green can help.',
    true,
  ),
  socialCut(
    'The green is cut off.',
    'Restore a path to the Village Green.',
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

  final String bubble, detail;
  final bool problem;
  const Reason(this.bubble, this.detail, this.problem);
}
