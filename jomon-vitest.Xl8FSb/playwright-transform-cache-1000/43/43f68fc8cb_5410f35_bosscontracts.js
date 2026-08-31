// b4580043428c9fa2369425f185f25074d7b02c29
const contracts = {
  mine: {
    id: 'mine:obsidian-warden',
    arena: 'rail quarry',
    phases: {
      pressure: {
        terrain: 'rail',
        telegraph: 'foreman-cavein',
        counterplay: 'leave the marked shelf'
      },
      cataclysm: {
        terrain: 'crumble',
        telegraph: 'guardian-slam',
        counterplay: 'keep an escape lane'
      }
    },
    adds: ['guard', 'pursuer'],
    reward: 'relic'
  },
  wilds: {
    id: 'wilds:heartwood',
    arena: 'root clearing',
    phases: {
      pressure: {
        terrain: 'bramble',
        telegraph: 'heartwood-charge',
        counterplay: 'cut a clear lane'
      },
      cataclysm: {
        terrain: 'water',
        telegraph: 'guardian-slam',
        counterplay: 'keep dry footing'
      }
    },
    adds: ['ambusher', 'controller'],
    reward: 'relic'
  },
  caverns: {
    id: 'caverns:tidemaw',
    arena: 'storm-tide chamber',
    phases: {
      pressure: {
        terrain: 'current',
        telegraph: 'geode-fissure',
        counterplay: 'brace at an anchor'
      },
      cataclysm: {
        terrain: 'deepWater',
        telegraph: 'guardian-slam',
        counterplay: 'preserve a dry shelf'
      }
    },
    adds: ['artillery', 'controller'],
    reward: 'relic'
  },
  ruins: {
    id: 'ruins:stone-keeper',
    arena: 'ritual precinct',
    phases: {
      pressure: {
        terrain: 'dart',
        telegraph: 'regent-decree',
        counterplay: 'move before the decree lands'
      },
      cataclysm: {
        terrain: 'darkness',
        telegraph: 'regent-judgment',
        counterplay: 'keep a lit escape route'
      }
    },
    adds: ['support', 'controller'],
    reward: 'relic'
  },
  furnace: {
    id: 'furnace:kiln-heart',
    arena: 'kiln terrace',
    phases: {
      pressure: {
        terrain: 'smoke',
        telegraph: 'enemy-fire',
        counterplay: 'take the lift lane'
      },
      cataclysm: {
        terrain: 'fireVent',
        telegraph: 'guardian-slam',
        counterplay: 'leave the vent line'
      }
    },
    adds: ['guard', 'artillery'],
    reward: 'relic'
  },
  floodedRuins: {
    id: 'flooded:drowned-regent',
    arena: 'tide basin',
    phases: {
      pressure: {
        terrain: 'current',
        telegraph: 'enemy-pull',
        counterplay: 'brace at an anchor'
      },
      cataclysm: {
        terrain: 'deepWater',
        telegraph: 'guardian-slam',
        counterplay: 'keep a dry crossing'
      }
    },
    adds: ['guard', 'controller'],
    reward: 'relic'
  },
  cliffs: {
    id: 'cliffs:sky-warden',
    arena: 'wind shelf',
    phases: {
      pressure: {
        terrain: 'ledge',
        telegraph: 'enemy-shot',
        counterplay: 'hold the rope route'
      },
      cataclysm: {
        terrain: 'smoke',
        telegraph: 'guardian-slam',
        counterplay: 'wait out the gust'
      }
    },
    adds: ['skirmisher', 'pursuer'],
    reward: 'relic'
  },
  burial: {
    id: 'burial:barrow-king',
    arena: 'ancestor field',
    phases: {
      pressure: {
        terrain: 'spiritPath',
        telegraph: 'enemy-ritual',
        counterplay: 'leave the procession path'
      },
      cataclysm: {
        terrain: 'graveSoil',
        telegraph: 'guardian-slam',
        counterplay: 'keep an open grave lane'
      }
    },
    adds: ['support', 'pursuer'],
    reward: 'relic'
  },
  saltFlats: {
    id: 'salt:sovreign',
    arena: 'mirror basin',
    phases: {
      pressure: {
        terrain: 'saltMirror',
        telegraph: 'enemy-pull',
        counterplay: 'break the mirror line'
      },
      cataclysm: {
        terrain: 'brine',
        telegraph: 'guardian-slam',
        counterplay: 'leave the brine edge'
      }
    },
    adds: ['skirmisher', 'artillery'],
    reward: 'relic'
  },
  frostReliquary: {
    id: 'frost:reliquary-warden',
    arena: 'ice reliquary',
    phases: {
      pressure: {
        terrain: 'ice',
        telegraph: 'enemy-ward',
        counterplay: 'break the ward line'
      },
      cataclysm: {
        terrain: 'frostRime',
        telegraph: 'guardian-slam',
        counterplay: 'keep a thawed route'
      }
    },
    adds: ['guard', 'support'],
    reward: 'relic'
  }
};
const guardianBiome = {
  foreman: 'mine',
  heartwood: 'wilds',
  geode: 'caverns',
  regent: 'ruins',
  kilnheart: 'furnace',
  drownedRegent: 'floodedRuins',
  skyWarden: 'cliffs',
  barrowKing: 'burial',
  saltSovereign: 'saltFlats',
  reliquaryWarden: 'frostReliquary'
};
export const bossContractFor = biome => contracts[biome];
export const bossContractForGuardian = kind => guardianBiome[kind] ? contracts[guardianBiome[kind]] : undefined;
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJjb250cmFjdHMiLCJtaW5lIiwiaWQiLCJhcmVuYSIsInBoYXNlcyIsInByZXNzdXJlIiwidGVycmFpbiIsInRlbGVncmFwaCIsImNvdW50ZXJwbGF5IiwiY2F0YWNseXNtIiwiYWRkcyIsInJld2FyZCIsIndpbGRzIiwiY2F2ZXJucyIsInJ1aW5zIiwiZnVybmFjZSIsImZsb29kZWRSdWlucyIsImNsaWZmcyIsImJ1cmlhbCIsInNhbHRGbGF0cyIsImZyb3N0UmVsaXF1YXJ5IiwiZ3VhcmRpYW5CaW9tZSIsImZvcmVtYW4iLCJoZWFydHdvb2QiLCJnZW9kZSIsInJlZ2VudCIsImtpbG5oZWFydCIsImRyb3duZWRSZWdlbnQiLCJza3lXYXJkZW4iLCJiYXJyb3dLaW5nIiwic2FsdFNvdmVyZWlnbiIsInJlbGlxdWFyeVdhcmRlbiIsImJvc3NDb250cmFjdEZvciIsImJpb21lIiwiYm9zc0NvbnRyYWN0Rm9yR3VhcmRpYW4iLCJraW5kIiwidW5kZWZpbmVkIl0sInNvdXJjZXMiOlsiYm9zcy1jb250cmFjdHMudHMiXSwic291cmNlc0NvbnRlbnQiOlsiaW1wb3J0IHR5cGUgeyBCaW9tZSwgR3VhcmRpYW5QaGFzZSwgTW9uc3RlclJvbGUsIFRpbGVLaW5kIH0gZnJvbSAnLi4vdHlwZXMnXG5cbmV4cG9ydCBpbnRlcmZhY2UgQm9zc1BoYXNlQ29udHJhY3QgeyB0ZXJyYWluOiBUaWxlS2luZDsgdGVsZWdyYXBoOiBzdHJpbmc7IGNvdW50ZXJwbGF5OiBzdHJpbmcgfVxuZXhwb3J0IGludGVyZmFjZSBCb3NzQ29udHJhY3QgeyBpZDogc3RyaW5nOyBhcmVuYTogc3RyaW5nOyBwaGFzZXM6IFJlY29yZDxFeGNsdWRlPEd1YXJkaWFuUGhhc2UsICdvcGVuaW5nJz4sIEJvc3NQaGFzZUNvbnRyYWN0PjsgYWRkczogTW9uc3RlclJvbGVbXTsgcmV3YXJkOiAncmVsaWMnIH1cblxuY29uc3QgY29udHJhY3RzOiBSZWNvcmQ8QmlvbWUsIEJvc3NDb250cmFjdD4gPSB7XG4gIG1pbmU6IHsgaWQ6ICdtaW5lOm9ic2lkaWFuLXdhcmRlbicsIGFyZW5hOiAncmFpbCBxdWFycnknLCBwaGFzZXM6IHsgcHJlc3N1cmU6IHsgdGVycmFpbjogJ3JhaWwnLCB0ZWxlZ3JhcGg6ICdmb3JlbWFuLWNhdmVpbicsIGNvdW50ZXJwbGF5OiAnbGVhdmUgdGhlIG1hcmtlZCBzaGVsZicgfSwgY2F0YWNseXNtOiB7IHRlcnJhaW46ICdjcnVtYmxlJywgdGVsZWdyYXBoOiAnZ3VhcmRpYW4tc2xhbScsIGNvdW50ZXJwbGF5OiAna2VlcCBhbiBlc2NhcGUgbGFuZScgfSB9LCBhZGRzOiBbJ2d1YXJkJywgJ3B1cnN1ZXInXSwgcmV3YXJkOiAncmVsaWMnIH0sXG4gIHdpbGRzOiB7IGlkOiAnd2lsZHM6aGVhcnR3b29kJywgYXJlbmE6ICdyb290IGNsZWFyaW5nJywgcGhhc2VzOiB7IHByZXNzdXJlOiB7IHRlcnJhaW46ICdicmFtYmxlJywgdGVsZWdyYXBoOiAnaGVhcnR3b29kLWNoYXJnZScsIGNvdW50ZXJwbGF5OiAnY3V0IGEgY2xlYXIgbGFuZScgfSwgY2F0YWNseXNtOiB7IHRlcnJhaW46ICd3YXRlcicsIHRlbGVncmFwaDogJ2d1YXJkaWFuLXNsYW0nLCBjb3VudGVycGxheTogJ2tlZXAgZHJ5IGZvb3RpbmcnIH0gfSwgYWRkczogWydhbWJ1c2hlcicsICdjb250cm9sbGVyJ10sIHJld2FyZDogJ3JlbGljJyB9LFxuICBjYXZlcm5zOiB7IGlkOiAnY2F2ZXJuczp0aWRlbWF3JywgYXJlbmE6ICdzdG9ybS10aWRlIGNoYW1iZXInLCBwaGFzZXM6IHsgcHJlc3N1cmU6IHsgdGVycmFpbjogJ2N1cnJlbnQnLCB0ZWxlZ3JhcGg6ICdnZW9kZS1maXNzdXJlJywgY291bnRlcnBsYXk6ICdicmFjZSBhdCBhbiBhbmNob3InIH0sIGNhdGFjbHlzbTogeyB0ZXJyYWluOiAnZGVlcFdhdGVyJywgdGVsZWdyYXBoOiAnZ3VhcmRpYW4tc2xhbScsIGNvdW50ZXJwbGF5OiAncHJlc2VydmUgYSBkcnkgc2hlbGYnIH0gfSwgYWRkczogWydhcnRpbGxlcnknLCAnY29udHJvbGxlciddLCByZXdhcmQ6ICdyZWxpYycgfSxcbiAgcnVpbnM6IHsgaWQ6ICdydWluczpzdG9uZS1rZWVwZXInLCBhcmVuYTogJ3JpdHVhbCBwcmVjaW5jdCcsIHBoYXNlczogeyBwcmVzc3VyZTogeyB0ZXJyYWluOiAnZGFydCcsIHRlbGVncmFwaDogJ3JlZ2VudC1kZWNyZWUnLCBjb3VudGVycGxheTogJ21vdmUgYmVmb3JlIHRoZSBkZWNyZWUgbGFuZHMnIH0sIGNhdGFjbHlzbTogeyB0ZXJyYWluOiAnZGFya25lc3MnLCB0ZWxlZ3JhcGg6ICdyZWdlbnQtanVkZ21lbnQnLCBjb3VudGVycGxheTogJ2tlZXAgYSBsaXQgZXNjYXBlIHJvdXRlJyB9IH0sIGFkZHM6IFsnc3VwcG9ydCcsICdjb250cm9sbGVyJ10sIHJld2FyZDogJ3JlbGljJyB9LFxuICBmdXJuYWNlOiB7IGlkOiAnZnVybmFjZTpraWxuLWhlYXJ0JywgYXJlbmE6ICdraWxuIHRlcnJhY2UnLCBwaGFzZXM6IHsgcHJlc3N1cmU6IHsgdGVycmFpbjogJ3Ntb2tlJywgdGVsZWdyYXBoOiAnZW5lbXktZmlyZScsIGNvdW50ZXJwbGF5OiAndGFrZSB0aGUgbGlmdCBsYW5lJyB9LCBjYXRhY2x5c206IHsgdGVycmFpbjogJ2ZpcmVWZW50JywgdGVsZWdyYXBoOiAnZ3VhcmRpYW4tc2xhbScsIGNvdW50ZXJwbGF5OiAnbGVhdmUgdGhlIHZlbnQgbGluZScgfSB9LCBhZGRzOiBbJ2d1YXJkJywgJ2FydGlsbGVyeSddLCByZXdhcmQ6ICdyZWxpYycgfSxcbiAgZmxvb2RlZFJ1aW5zOiB7IGlkOiAnZmxvb2RlZDpkcm93bmVkLXJlZ2VudCcsIGFyZW5hOiAndGlkZSBiYXNpbicsIHBoYXNlczogeyBwcmVzc3VyZTogeyB0ZXJyYWluOiAnY3VycmVudCcsIHRlbGVncmFwaDogJ2VuZW15LXB1bGwnLCBjb3VudGVycGxheTogJ2JyYWNlIGF0IGFuIGFuY2hvcicgfSwgY2F0YWNseXNtOiB7IHRlcnJhaW46ICdkZWVwV2F0ZXInLCB0ZWxlZ3JhcGg6ICdndWFyZGlhbi1zbGFtJywgY291bnRlcnBsYXk6ICdrZWVwIGEgZHJ5IGNyb3NzaW5nJyB9IH0sIGFkZHM6IFsnZ3VhcmQnLCAnY29udHJvbGxlciddLCByZXdhcmQ6ICdyZWxpYycgfSxcbiAgY2xpZmZzOiB7IGlkOiAnY2xpZmZzOnNreS13YXJkZW4nLCBhcmVuYTogJ3dpbmQgc2hlbGYnLCBwaGFzZXM6IHsgcHJlc3N1cmU6IHsgdGVycmFpbjogJ2xlZGdlJywgdGVsZWdyYXBoOiAnZW5lbXktc2hvdCcsIGNvdW50ZXJwbGF5OiAnaG9sZCB0aGUgcm9wZSByb3V0ZScgfSwgY2F0YWNseXNtOiB7IHRlcnJhaW46ICdzbW9rZScsIHRlbGVncmFwaDogJ2d1YXJkaWFuLXNsYW0nLCBjb3VudGVycGxheTogJ3dhaXQgb3V0IHRoZSBndXN0JyB9IH0sIGFkZHM6IFsnc2tpcm1pc2hlcicsICdwdXJzdWVyJ10sIHJld2FyZDogJ3JlbGljJyB9LFxuICBidXJpYWw6IHsgaWQ6ICdidXJpYWw6YmFycm93LWtpbmcnLCBhcmVuYTogJ2FuY2VzdG9yIGZpZWxkJywgcGhhc2VzOiB7IHByZXNzdXJlOiB7IHRlcnJhaW46ICdzcGlyaXRQYXRoJywgdGVsZWdyYXBoOiAnZW5lbXktcml0dWFsJywgY291bnRlcnBsYXk6ICdsZWF2ZSB0aGUgcHJvY2Vzc2lvbiBwYXRoJyB9LCBjYXRhY2x5c206IHsgdGVycmFpbjogJ2dyYXZlU29pbCcsIHRlbGVncmFwaDogJ2d1YXJkaWFuLXNsYW0nLCBjb3VudGVycGxheTogJ2tlZXAgYW4gb3BlbiBncmF2ZSBsYW5lJyB9IH0sIGFkZHM6IFsnc3VwcG9ydCcsICdwdXJzdWVyJ10sIHJld2FyZDogJ3JlbGljJyB9LFxuICBzYWx0RmxhdHM6IHsgaWQ6ICdzYWx0OnNvdnJlaWduJywgYXJlbmE6ICdtaXJyb3IgYmFzaW4nLCBwaGFzZXM6IHsgcHJlc3N1cmU6IHsgdGVycmFpbjogJ3NhbHRNaXJyb3InLCB0ZWxlZ3JhcGg6ICdlbmVteS1wdWxsJywgY291bnRlcnBsYXk6ICdicmVhayB0aGUgbWlycm9yIGxpbmUnIH0sIGNhdGFjbHlzbTogeyB0ZXJyYWluOiAnYnJpbmUnLCB0ZWxlZ3JhcGg6ICdndWFyZGlhbi1zbGFtJywgY291bnRlcnBsYXk6ICdsZWF2ZSB0aGUgYnJpbmUgZWRnZScgfSB9LCBhZGRzOiBbJ3NraXJtaXNoZXInLCAnYXJ0aWxsZXJ5J10sIHJld2FyZDogJ3JlbGljJyB9LFxuICBmcm9zdFJlbGlxdWFyeTogeyBpZDogJ2Zyb3N0OnJlbGlxdWFyeS13YXJkZW4nLCBhcmVuYTogJ2ljZSByZWxpcXVhcnknLCBwaGFzZXM6IHsgcHJlc3N1cmU6IHsgdGVycmFpbjogJ2ljZScsIHRlbGVncmFwaDogJ2VuZW15LXdhcmQnLCBjb3VudGVycGxheTogJ2JyZWFrIHRoZSB3YXJkIGxpbmUnIH0sIGNhdGFjbHlzbTogeyB0ZXJyYWluOiAnZnJvc3RSaW1lJywgdGVsZWdyYXBoOiAnZ3VhcmRpYW4tc2xhbScsIGNvdW50ZXJwbGF5OiAna2VlcCBhIHRoYXdlZCByb3V0ZScgfSB9LCBhZGRzOiBbJ2d1YXJkJywgJ3N1cHBvcnQnXSwgcmV3YXJkOiAncmVsaWMnIH1cbn1cblxuY29uc3QgZ3VhcmRpYW5CaW9tZTogUmVjb3JkPHN0cmluZywgQmlvbWU+ID0geyBmb3JlbWFuOiAnbWluZScsIGhlYXJ0d29vZDogJ3dpbGRzJywgZ2VvZGU6ICdjYXZlcm5zJywgcmVnZW50OiAncnVpbnMnLCBraWxuaGVhcnQ6ICdmdXJuYWNlJywgZHJvd25lZFJlZ2VudDogJ2Zsb29kZWRSdWlucycsIHNreVdhcmRlbjogJ2NsaWZmcycsIGJhcnJvd0tpbmc6ICdidXJpYWwnLCBzYWx0U292ZXJlaWduOiAnc2FsdEZsYXRzJywgcmVsaXF1YXJ5V2FyZGVuOiAnZnJvc3RSZWxpcXVhcnknIH1cblxuZXhwb3J0IGNvbnN0IGJvc3NDb250cmFjdEZvciA9IChiaW9tZTogQmlvbWUpOiBCb3NzQ29udHJhY3QgPT4gY29udHJhY3RzW2Jpb21lXVxuZXhwb3J0IGNvbnN0IGJvc3NDb250cmFjdEZvckd1YXJkaWFuID0gKGtpbmQ6IHN0cmluZyk6IEJvc3NDb250cmFjdCB8IHVuZGVmaW5lZCA9PiBndWFyZGlhbkJpb21lW2tpbmRdID8gY29udHJhY3RzW2d1YXJkaWFuQmlvbWVba2luZF1dIDogdW5kZWZpbmVkXG4iXSwibWFwcGluZ3MiOiJBQUtBLE1BQU1BLFNBQXNDLEdBQUc7RUFDN0NDLElBQUksRUFBRTtJQUFFQyxFQUFFLEVBQUUsc0JBQXNCO0lBQUVDLEtBQUssRUFBRSxhQUFhO0lBQUVDLE1BQU0sRUFBRTtNQUFFQyxRQUFRLEVBQUU7UUFBRUMsT0FBTyxFQUFFLE1BQU07UUFBRUMsU0FBUyxFQUFFLGdCQUFnQjtRQUFFQyxXQUFXLEVBQUU7TUFBeUIsQ0FBQztNQUFFQyxTQUFTLEVBQUU7UUFBRUgsT0FBTyxFQUFFLFNBQVM7UUFBRUMsU0FBUyxFQUFFLGVBQWU7UUFBRUMsV0FBVyxFQUFFO01BQXNCO0lBQUUsQ0FBQztJQUFFRSxJQUFJLEVBQUUsQ0FBQyxPQUFPLEVBQUUsU0FBUyxDQUFDO0lBQUVDLE1BQU0sRUFBRTtFQUFRLENBQUM7RUFDelRDLEtBQUssRUFBRTtJQUFFVixFQUFFLEVBQUUsaUJBQWlCO0lBQUVDLEtBQUssRUFBRSxlQUFlO0lBQUVDLE1BQU0sRUFBRTtNQUFFQyxRQUFRLEVBQUU7UUFBRUMsT0FBTyxFQUFFLFNBQVM7UUFBRUMsU0FBUyxFQUFFLGtCQUFrQjtRQUFFQyxXQUFXLEVBQUU7TUFBbUIsQ0FBQztNQUFFQyxTQUFTLEVBQUU7UUFBRUgsT0FBTyxFQUFFLE9BQU87UUFBRUMsU0FBUyxFQUFFLGVBQWU7UUFBRUMsV0FBVyxFQUFFO01BQW1CO0lBQUUsQ0FBQztJQUFFRSxJQUFJLEVBQUUsQ0FBQyxVQUFVLEVBQUUsWUFBWSxDQUFDO0lBQUVDLE1BQU0sRUFBRTtFQUFRLENBQUM7RUFDdlRFLE9BQU8sRUFBRTtJQUFFWCxFQUFFLEVBQUUsaUJBQWlCO0lBQUVDLEtBQUssRUFBRSxvQkFBb0I7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QUFJLEVBQUUsQ0FBQyxTQUFTLEVBQUUsU0FBUyxDQUFDO0lBQUVDLE1BQU0sRUFBRTtFQUFRLENBQUM7RUFDM1VRLFNBQVMsRUFBRTtJQUFFakIsRUFBRSxFQUFFLGVBQWU7SUFBRUMsS0FBSyxFQUFFLGNBQWM7SUFBRUMsTUFBTSxFQUFFO01BQUVDLFFBQVEsRUFBRTtRQUFFQyxPQUFPLEVBQUUsWUFBWTtRQUFFQyxTQUFTLEVBQUUsWUFBWTtRQUFFQyxXQUFXLEVBQUU7TUFBd0IsQ0FBQztNQUFFQyxTQUFTLEVBQUU7UUFBRUgsT0FBTyxFQUFFLE9BQU87UUFBRUMsU0FBUyxFQUFFLGVBQWU7UUFBRUMsV0FBVyxFQUFFO01BQXVCO0lBQUUsQ0FBQztJQUFFRSxJQUFJLEVBQUUsQ0FBQyxZQUFZLEVBQUUsV0FBVyxDQUFDO0lBQUVDLE1BQU0sRUFBRTtFQUFRLENBQUM7RUFDL1RTLGNBQWMsRUFBRTtJQUFFbEIsRUFBRSxFQUFFLHdCQUF3QjtJQUFFQyxLQUFLLEVBQUUsZUFBZTtJQUFFQyxNQUFNLEVBQUU7TUFBRUMsUUFBUSxFQUFFO1FBQUVDLE9BQU8sRUFBRSxLQUFLO1FBQUVDLFNBQVMsRUFBRSxZQUFZO1FBQUVDLFdBQVcsRUFBRTtNQUFzQixDQUFDO01BQUVDLFNBQVMsRUFBRTtRQUFFSCxPQUFPLEVBQUUsV0FBVztRQUFFQyxTQUFTLEVBQUUsZUFBZTtRQUFFQyxXQUFXLEVBQUU7TUFBc0I7SUFBRSxDQUFDO0lBQUVFLElBQUksRUFBRSxDQUFDLE9BQU8sRUFBRSxTQUFTLENBQUM7SUFBRUMsTUFBTSxFQUFFO0VBQVE7QUFDbFUsQ0FBQztBQUVELE1BQU1VLGFBQW9DLEdBQUc7RUFBRUMsT0FBTyxFQUFFLE1BQU07RUFBRUMsU0FBUyxFQUFFLE9BQU87RUFBRUMsS0FBSyxFQUFFLFNBQVM7RUFBRUMsTUFBTSxFQUFFLE9BQU87RUFBRUMsU0FBUyxFQUFFLFNBQVM7RUFBRUMsYUFBYSxFQUFFLGNBQWM7RUFBRUMsU0FBUyxFQUFFLFFBQVE7RUFBRUMsVUFBVSxFQUFFLFFBQVE7RUFBRUMsYUFBYSxFQUFFLFdBQVc7RUFBRUMsZUFBZSxFQUFFO0FBQWlCLENBQUM7QUFFdFIsT0FBTyxNQUFNQyxlQUFlLEdBQUlDLEtBQVksSUFBbUJqQyxTQUFTLENBQUNpQyxLQUFLLENBQUM7QUFDL0UsT0FBTyxNQUFNQyx1QkFBdUIsR0FBSUMsSUFBWSxJQUErQmQsYUFBYSxDQUFDYyxJQUFJLENBQUMsR0FBR25DLFNBQVMsQ0FBQ3FCLGFBQWEsQ0FBQ2MsSUFBSSxDQUFDLENBQUMsR0FBR0MsU0FBUyIsImlnbm9yZUxpc3QiOltdfQ==