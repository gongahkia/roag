// Static C# port of src/scheduler.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: f75f10f1376ab4f9ccdc3eb1eab7cf4f6f2a1d74278c194c76b8d712bd5142cf
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_scheduler { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
V v_3 = V.Nil;
v_3 = new V((RuleFunction)(args_4 => {
V v_5 = args_4[0];
if (R.Truth(R.Or(R.Or(R.Or((V)(R.Get(v_5, (V)"kind") == (V)"effect"), () => (V)(R.Get(v_5, (V)"kind") == (V)"impact")), () => (V)(R.Get(v_5, (V)"kind") == (V)"stage_pressure")), () => (V)(R.Get(v_5, (V)"kind") == (V)"stage_deadline")))) {
return R.Join(new V[] {}, new Results((V)0.0d));
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_5, (V)"kind") == (V)"actor_ready")}, new Results((V)"unknown scheduled kind")));
return R.Join(new V[] {}, new Results((V)1.0d));
return Results.Empty;
}));
V v_6 = V.Nil;
v_6 = new V((RuleFunction)(args_7 => {
V v_8 = args_7[0];
V v_9 = args_7[1];
if (R.Truth((V)(R.Get(v_8, (V)"due") != R.Get(v_9, (V)"due")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_8, (V)"due") < R.Get(v_9, (V)"due"))));
}
var values_10 = R.Join(new V[] {R.Call(v_3, R.Join(new V[] {}, new Results(v_8))).First}, R.Call(v_3, R.Join(new V[] {}, new Results(v_9))));
V v_11 = values_10[0];
V v_12 = values_10[1];
if (R.Truth((V)(v_11 != v_12))) {
return R.Join(new V[] {}, new Results((V)(v_11 < v_12)));
}
if (R.Truth(R.And((V)(v_11 == (V)1.0d), () => (V)(R.Get(v_8, (V)"is_player") != R.Get(v_9, (V)"is_player"))))) {
return R.Join(new V[] {}, new Results(R.Get(v_8, (V)"is_player")));
}
if (R.Truth((V)(R.Get(v_8, (V)"seq") != R.Get(v_9, (V)"seq")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_8, (V)"seq") < R.Get(v_9, (V)"seq"))));
}
return R.Join(new V[] {}, new Results((V)(R.Get(v_8, (V)"id") < R.Get(v_9, (V)"id"))));
return Results.Empty;
}));
R.Set(v_2, (V)"enqueue", new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
V v_15 = args_13[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_15, (V)"due")))).First == (V)"number"), () => (V)((V)(R.Get(v_15, (V)"due") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_15, (V)"due") >= R.Get(v_14, (V)"clock")))}, new Results((V)"scheduled time must be an integer at or after world time")));
R.Call(v_3, R.Join(new V[] {}, new Results(v_15)));
var values_16 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_15, (V)"floor_id"), () => R.Get(v_14, (V)"active_floor_id"))));
V v_17 = values_16[0];
if (R.Truth(R.Or((V)(R.Get(v_15, (V)"kind") == (V)"stage_pressure"), () => (V)(R.Get(v_15, (V)"kind") == (V)"stage_deadline")))) {
var values_18 = R.Join(new V[] {}, new Results(V.Nil));
v_17 = values_18[0];
}
var values_19 = R.Join(new V[] {}, new Results(R.Get(v_14, (V)"next_schedule_id")));
V v_20 = values_19[0];
var values_21 = R.Join(new V[] {}, new Results((V)(v_20 + (V)1.0d)));
var obj_22 = v_14;
V key_23 = (V)"next_schedule_id";
R.Set(obj_22, key_23, values_21[0]);
var values_24 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_20), new Entry((V)"seq", v_20), new Entry((V)"due", R.Get(v_15, (V)"due")), new Entry((V)"kind", R.Get(v_15, (V)"kind")), new Entry((V)"actor_id", R.Get(v_15, (V)"actor_id")), new Entry((V)"is_player", R.Get(v_15, (V)"is_player")), new Entry((V)"effect_id", R.Get(v_15, (V)"effect_id")), new Entry((V)"attack_id", R.Get(v_15, (V)"attack_id")), new Entry((V)"floor_id", v_17))));
V v_25 = values_24[0];
var values_26 = R.Join(new V[] {}, new Results(v_25));
var obj_27 = R.Get(v_14, (V)"schedule");
V key_28 = (V)((V)R.Length(R.Get(v_14, (V)"schedule")) + (V)1.0d);
R.Set(obj_27, key_28, values_26[0]);
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {R.Get(v_14, (V)"schedule")}, new Results(v_6)));
return R.Join(new V[] {}, new Results(v_25));
return Results.Empty;
})));
R.Set(v_2, (V)"remove_for_actor", new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
V v_32 = args_29[2];
{ double end_35 = R.Num((V)1.0d), step_36 = R.Num((-(V)1.0d));
for (double i_34 = R.Num((V)R.Length(R.Get(v_30, (V)"schedule"))); step_36 > 0 ? i_34 <= end_35 : i_34 >= end_35; i_34 += step_36) {
V v_33 = i_34;
var values_37 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_30, (V)"schedule"), v_33)));
V v_38 = values_37[0];
if (R.Truth(R.And((V)(R.Get(v_38, (V)"actor_id") == v_31), () => R.Or((V)(v_32 == V.Nil), () => (V)(R.Get(v_38, (V)"kind") == v_32))))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_30, (V)"schedule")}, new Results(v_33)));
}
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"peek", new V((RuleFunction)(args_39 => {
V v_40 = args_39[0];
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_40, (V)"schedule"), (V)1.0d)));
return Results.Empty;
})));
R.Set(v_2, (V)"pop", new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_42, (V)"schedule")}, new Results((V)1.0d))));
return Results.Empty;
})));
R.Set(v_2, (V)"sort", new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {R.Get(v_44, (V)"schedule")}, new Results(v_6)));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
