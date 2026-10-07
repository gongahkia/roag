"""Registered automatic combat rules; identities are independent of their owners."""

from .triggers import EventType, Limiter, LimitKind, Phase, TriggerSpec, validate_trigger_graph

RIPOSTE = TriggerSpec("status:riposte", EventType.DAMAGE, (EventType.DAMAGE,),
                      limiter=Limiter(LimitKind.FAMILY, family="riposte"), proc_family="riposte")
CARD_TRIGGERS = tuple(TriggerSpec(identity, EventType.CARD_PLAY, tuple(EventType(op) for op in emits),
                                limiter=Limiter(LimitKind.CARD), proc_family=identity)
                      for identity, emits in (
                          ("boon:resonant_circuit", ("energy",)),
                          ("boon:countercurrent", ("draw",)),
                          ("curse:frayed_focus", ("discard",)),
                          ("boon:hunters_rhythm", ("draw",)),
                          ("rules:block_card_count", ()),
                          ("item:focusing_lens", ("draw",)),
                          ("item:reserve_cell", ("energy",))))
CURSE_TRIGGERS = tuple(TriggerSpec("curse:" + event.value, event,
                                 (EventType.STRESS, EventType.STATUS, EventType.MOVE, EventType.ENERGY),
                                 limiter=Limiter(LimitKind.FAMILY, family="curse_card"), proc_family="curse_card")
                       for event in (EventType.CARD_DRAW, EventType.CARD_HELD))
MERCY = TriggerSpec("boon:mercy_circuit", EventType.HEAL, (EventType.BLOCK,),
                    limiter=Limiter(LimitKind.FAMILY, family="mercy"), proc_family="mercy")
ADRENAL = TriggerSpec("boon:adrenal_coil", EventType.DAMAGE, (EventType.BLOCK,), priority=-10,
                      limiter=Limiter(LimitKind.FAMILY, family="adrenal"), proc_family="adrenal")
THIRD_BELL = TriggerSpec("base:third_bell", EventType.CARD_PLAY, (EventType.BLOCK,),
                         priority=90, limiter=Limiter(LimitKind.TURN), proc_family="mutation:third_bell")
DEATH_SURGE = TriggerSpec("base:death_surge", EventType.DEATH, (EventType.STATUS,), phase=Phase.DEATH,
                          priority=100, limiter=Limiter(LimitKind.ROOT), proc_family="mutation:death_surge")
RIME_SHELL = TriggerSpec("biome:rime_shell", EventType.STATUS, (), phase=Phase.REPLACE,
                         priority=-130, limiter=Limiter(LimitKind.COMBAT), proc_family="mutation:rime_shell")
SPORE_LINK = TriggerSpec("biome:spore_link", EventType.DEATH, (EventType.STATUS,), phase=Phase.DEATH,
                         priority=140, limiter=Limiter(LimitKind.ROOT), proc_family="mutation:spore_link")
REINFORCEMENT_CALL = TriggerSpec("base:reinforcement_call", EventType.DEATH, (), phase=Phase.DEATH,
                                 priority=110, limiter=Limiter(LimitKind.ROOT),
                                 proc_family="mutation:reinforcement_call")
MUTATION_TRIGGERS = (THIRD_BELL, DEATH_SURGE, REINFORCEMENT_CALL, RIME_SHELL, SPORE_LINK)
REGISTERED = {spec.id: spec for spec in (RIPOSTE, MERCY, ADRENAL) + CARD_TRIGGERS + CURSE_TRIGGERS + MUTATION_TRIGGERS}
HOST_EDGES = (TriggerSpec("rules:draw_cards", EventType.DRAW, (EventType.CARD_DRAW,), phase=Phase.PRIMARY),
              TriggerSpec("rules:lethal_damage", EventType.DAMAGE, (EventType.DEATH,), phase=Phase.PRIMARY))
validate_trigger_graph(tuple(REGISTERED.values()) + HOST_EDGES)
