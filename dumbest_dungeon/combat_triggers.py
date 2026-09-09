"""Registered automatic combat rules; identities are independent of their owners."""

from .triggers import EventType, Limiter, LimitKind, TriggerSpec, validate_trigger_graph

RIPOSTE = TriggerSpec("status:riposte", EventType.DAMAGE, (EventType.DAMAGE,),
                      limiter=Limiter(LimitKind.FAMILY, family="riposte"), proc_family="riposte")
REGISTERED = {spec.id: spec for spec in (RIPOSTE,)}
validate_trigger_graph(tuple(REGISTERED.values()))
