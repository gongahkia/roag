// all tunable colony rules use simulation seconds and thousandths of a coin.
abstract final class Rules {
  static const day = 150.0;
  static const morningEnd = 15.0, eveningStart = 90.0, nightStart = 125.0;
  static const startingMoney = 200000;
  static const costs = {'cottage': 5000, 'garden': 8000, 'green': 6000};
  static const grantPerDay = 20000;
  static const workIncomePerSecond = 200;
  static const gardenCapacity = 2, greenCapacity = 6;
  static const workDuration = 12.0, socialDuration = 6.0;
  static const workRetryAfter = 24.0, leisureRepeatAfter = 18.0;
  static const grace = 75.0, connectionGrace = 10.0, recent = day;
  static const moodBase = 50, housingPenalty = -20, missingPenalty = -10;
  static const workBenefit = 15, socialBenefit = 15, gatheringBenefit = 10;
  static const arrivalStagger = .18, arrivalCap = 2.0, searchInterval = 3.0;
  static const homeDwell = 1.0, homeStagger = .18, socialWait = 2.0;
  static const walkSpeed = 3.5, walkVariation = .15, settleSpeed = 5.0;
  static const gatheringMinimum = 4, gatheringStart = 3, gatheringGuests = 6;
  static const gatheringDuration = 12.0, gatheringWait = 22.0;
  static const gatheringCooldownDays = 2, gatheringChance = .8;
  static const bubbleLimit = 2, bubbleSeconds = 3.0;
  static const bubbleCooldown = 20.0, complaintCooldown = 60.0;
  static const causeCooldown = 24.0, positiveCauseCooldown = 4.0;
  static int cost(String tool) => costs[tool] ?? 0;
  static String coins(int milli) => (milli / 1000).toStringAsFixed(1);
}
