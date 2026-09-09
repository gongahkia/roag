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
REGISTERED = {spec.id: spec for spec in (RIPOSTE,) + CARD_TRIGGERS + CURSE_TRIGGERS}
HOST_EDGES = (TriggerSpec("rules:draw_cards", EventType.DRAW, (EventType.CARD_DRAW,), phase=Phase.PRIMARY),)
validate_trigger_graph(tuple(REGISTERED.values()) + HOST_EDGES)
