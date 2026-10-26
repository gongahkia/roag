// Static C# port of src/diagnostics.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 91f9d535e32da7cdb21cd2ce921d29d531bc55bd756bf20f401d2bdd52b2cb74
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_diagnostics { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
V v_3 = V.Nil;
v_3 = new V((RuleFunction)(args_4 => {
V v_5 = args_4[0];
V v_6 = args_4[1];
if (R.Truth((V)((V)R.Length(v_5) == (V)0.0d))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_7 = R.Join(new V[] {}, new Results(R.Table()));
V v_8 = values_7[0];
foreach (var row_9 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_5)))))) {
V v_10 = row_9[0];
V v_11 = row_9[1];
var values_12 = R.Join(new V[] {}, new Results(v_11));
var obj_13 = v_8;
V key_14 = v_10;
R.Set(obj_13, key_14, values_12[0]);
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_8)));
return R.Join(new V[] {}, new Results(R.Get(v_8, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"ceil"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_8) * v_6)))))).First)));
return Results.Empty;
}));
R.Set(v_2, (V)"new", new V((RuleFunction)(args_15 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"frame_ms", R.Table()), new Entry((V)"update_ms", R.Table()), new Entry((V)"draw_ms", R.Table()), new Entry((V)"last_frame", V.Nil), new Entry((V)"max_samples", (V)600.0d))));
return Results.Empty;
})));
R.Set(v_2, (V)"sample", new V((RuleFunction)(args_16 => {
V v_17 = args_16[0];
V v_18 = args_16[1];
V v_19 = args_16[2];
V v_20 = args_16[3];
V v_21 = args_16[4];
V v_22 = args_16[5];
if (R.Truth(R.Get(v_17, (V)"last_frame"))) {
var values_23 = R.Join(new V[] {}, new Results((V)((V)(v_18 - R.Get(v_17, (V)"last_frame")) * (V)1000.0d)));
V v_24 = values_23[0];
var values_25 = R.Join(new V[] {}, new Results(R.Get(v_17, (V)"frame_ms")));
V v_26 = values_25[0];
var values_27 = R.Join(new V[] {}, new Results(v_24));
var obj_28 = v_26;
V key_29 = (V)((V)R.Length(v_26) + (V)1.0d);
R.Set(obj_28, key_29, values_27[0]);
if (R.Truth((V)((V)R.Length(v_26) > R.Get(v_17, (V)"max_samples")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {v_26}, new Results((V)1.0d)));
}
}
var values_30 = R.Join(new V[] {}, new Results(v_18));
var obj_31 = v_17;
V key_32 = (V)"last_frame";
R.Set(obj_31, key_32, values_30[0]);
var values_33 = R.Join(new V[] {}, new Results(R.Or(v_19, () => (V)0.0d)));
var obj_34 = R.Get(v_17, (V)"update_ms");
V key_35 = (V)((V)R.Length(R.Get(v_17, (V)"update_ms")) + (V)1.0d);
R.Set(obj_34, key_35, values_33[0]);
var values_36 = R.Join(new V[] {}, new Results(R.Or(v_20, () => (V)0.0d)));
var obj_37 = R.Get(v_17, (V)"draw_ms");
V key_38 = (V)((V)R.Length(R.Get(v_17, (V)"draw_ms")) + (V)1.0d);
R.Set(obj_37, key_38, values_36[0]);
if (R.Truth((V)((V)R.Length(R.Get(v_17, (V)"update_ms")) > R.Get(v_17, (V)"max_samples")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_17, (V)"update_ms")}, new Results((V)1.0d)));
}
if (R.Truth((V)((V)R.Length(R.Get(v_17, (V)"draw_ms")) > R.Get(v_17, (V)"max_samples")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_17, (V)"draw_ms")}, new Results((V)1.0d)));
}
var values_39 = R.Join(new V[] {}, new Results(R.Or(v_21, () => (V)0.0d)));
var obj_40 = v_17;
V key_41 = (V)"visible_actors";
R.Set(obj_40, key_41, values_39[0]);
var values_42 = R.Join(new V[] {}, new Results(R.Or(R.And(v_22, () => R.Get(v_22, (V)"drawcalls")), () => V.Nil)));
var obj_43 = v_17;
V key_44 = (V)"draw_calls";
R.Set(obj_43, key_44, values_42[0]);
var values_45 = R.Join(new V[] {}, new Results(R.Or(R.And(v_22, () => R.Get(v_22, (V)"texturememory")), () => V.Nil)));
var obj_46 = v_17;
V key_47 = (V)"texture_bytes";
R.Set(obj_46, key_47, values_45[0]);
return Results.Empty;
})));
R.Set(v_2, (V)"summary", new V((RuleFunction)(args_48 => {
V v_49 = args_48[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"samples", (V)R.Length(R.Get(v_49, (V)"frame_ms"))), new Entry((V)"frame_p50_ms", R.Call(v_3, R.Join(new V[] {R.Get(v_49, (V)"frame_ms")}, new Results((V)0.5d))).First), new Entry((V)"frame_p95_ms", R.Call(v_3, R.Join(new V[] {R.Get(v_49, (V)"frame_ms")}, new Results((V)0.95d))).First), new Entry((V)"frame_p99_ms", R.Call(v_3, R.Join(new V[] {R.Get(v_49, (V)"frame_ms")}, new Results((V)0.99d))).First), new Entry((V)"update_p95_ms", R.Call(v_3, R.Join(new V[] {R.Get(v_49, (V)"update_ms")}, new Results((V)0.95d))).First), new Entry((V)"draw_p95_ms", R.Call(v_3, R.Join(new V[] {R.Get(v_49, (V)"draw_ms")}, new Results((V)0.95d))).First), new Entry((V)"visible_actors", R.Get(v_49, (V)"visible_actors")), new Entry((V)"draw_calls", R.Get(v_49, (V)"draw_calls")), new Entry((V)"texture_bytes", R.Get(v_49, (V)"texture_bytes")))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
