"""Registered automatic combat rules; identities are independent of their owners."""

from .triggers import EventType, Limiter, LimitKind, TriggerSpec, validate_trigger_graph

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
REGISTERED = {spec.id: spec for spec in (RIPOSTE,) + CARD_TRIGGERS}
validate_trigger_graph(tuple(REGISTERED.values()))
