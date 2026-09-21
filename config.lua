return {
    version = '0.4.0', saveVersion = '0.4.0', stateVersion = '0.2.0', title = 'Cosmonauts', -- Working title, not a novelty claim.
    width = 256, height = 160, block = 4, seed = 12345, preset = 'frontier',
    mode = 'challenge', ticksPerSecond = 20, dayTicks = 3600,
    historyEvery = 200, historyLimit = 10, maxCommands = 20000,
    autosaveEvery = 200, maxStepsPerFrame = 8,
    windowWidth = 1340, windowHeight = 840,
    hungerRate = 0.018, fatigueRate = 0.010, startFood = 12,
    cropTicks = 1200, cropYield = 3, irrigationCapacity = 12,
    layout = 'hybrid', climate = 'balanced', openness = 0.48, biomeScale = 1.0,
    features = 'living', density = 1.0, crew = 3,
    -- New campaigns advance a worker every simulation tick and reconsider an
    -- idle route promptly. These values are copied into world.rules, so a
    -- saved campaign keeps its recorded pacing.
    workerMoveEvery = 1, workerPlanEvery = 8, jumpVersion = 1,
}
