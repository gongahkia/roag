// Static C# port of tests/spectator_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: c3525c67f6ba9c02789ff367f5ff6042c1cccd57c1b95b691cdf8ec670aab1d6
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_spectator_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.spectator"))));
V v_8 = values_7[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
R.Call(v_10, R.Join(new V[] {(V)"spectator creates every biome as an ineligible disposable run"}, new Results(new V((RuleFunction)(args_12 => {

foreach (var row_13 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_14 = row_13[0];
V v_15 = row_13[1];
var values_16 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), R.Join(new V[] {(V)17.0d, v_15}, new Results(R.Get(R.Get(v_4, (V)"order"), (V)1.0d)))));
V v_17 = values_16[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_17, (V)"meta"), (V)"eligible") == (V)false))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_6, (V)"player"), R.Join(new V[] {}, new Results(v_17))).First, (V)"invulnerable") == (V)true))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_17, (V)"generated_map"), (V)"biome_id") == v_15))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_17, (V)"floor_order")) == (V)2.0d), () => (V)(R.Get(v_17, (V)"clock") == (V)0.0d)))));
}
return Results.Empty;
})))));
R.Call(v_10, R.Join(new V[] {(V)"spectator camera pan is bounded and never changes the run"}, new Results(new V((RuleFunction)(args_18 => {

var values_19 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), R.Join(new V[] {(V)17.0d, R.Get(R.Get(v_2, (V)"order"), (V)1.0d)}, new Results(R.Get(R.Get(v_4, (V)"order"), (V)1.0d)))));
V v_20 = values_19[0];
var values_21 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_20))));
V v_22 = values_21[0];
var values_23 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"camera_step"), R.Join(new V[] {v_20, R.Table(new Entry((V)"x", (V)1.0d), new Entry((V)"y", (V)1.0d)), (-(V)1.0d), (-(V)1.0d)}, new Results((V)true))));
V v_24 = values_23[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_24, (V)"x") == (V)1.0d), () => (V)(R.Get(v_24, (V)"y") == (V)1.0d)))));
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"camera_step"), R.Join(new V[] {v_20, v_24, (V)1.0d, (V)0.0d}, new Results((V)true))));
v_24 = values_25[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_24, (V)"x") == (V)9.0d), () => (V)(R.Get(v_24, (V)"y") == (V)1.0d)))));
R.Call(v_11, R.Join(new V[] {v_22}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_20)))));
return Results.Empty;
})))));
R.Call(v_10, R.Join(new V[] {(V)"spectator god play blocks real enemy attacks without awarding progress"}, new Results(new V((RuleFunction)(args_26 => {

var values_27 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), R.Join(new V[] {(V)17.0d, R.Get(R.Get(v_2, (V)"order"), (V)1.0d)}, new Results(R.Get(R.Get(v_4, (V)"order"), (V)1.0d)))));
V v_28 = values_27[0];
var values_29 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"player"), R.Join(new V[] {}, new Results(v_28))));
V v_30 = values_29[0];
R.Call(R.Get(v_6, (V)"spawn_enemy"), R.Join(new V[] {v_28, (V)"core:enemy/mossbound_guard", (V)(R.Get(R.Get(v_30, (V)"position"), (V)"x") + (V)1.0d), R.Get(R.Get(v_30, (V)"position"), (V)"y")}, new Results((V)0.0d)));
var values_31 = R.Join(new V[] {}, new Results((V)false));
V v_32 = values_31[0];
{ double end_35 = R.Num((V)30.0d), step_36 = R.Num((V)1.0d);
for (double i_34 = R.Num((V)1.0d); step_36 > 0 ? i_34 <= end_35 : i_34 >= end_35; i_34 += step_36) {
V v_33 = i_34;
var values_37 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"submit"), R.Join(new V[] {v_28}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_38 = values_37[0];
V v_39 = values_37[1];
V v_40 = values_37[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_38}, new Results(v_39)));
foreach (var row_41 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_40)))))) {
V v_42 = row_41[0];
V v_43 = row_41[1];
if (R.Truth(R.And((V)(R.Get(v_43, (V)"kind") == (V)"DamageBlocked"), () => (V)(R.Get(v_43, (V)"target_id") == R.Get(v_30, (V)"id"))))) {
var values_44 = R.Join(new V[] {}, new Results((V)true));
v_32 = values_44[0];
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_32, () => (V)(R.Get(v_30, (V)"hp") == R.Get(v_30, (V)"max_hp"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_28, (V)"meta"), (V)"eligible") == (V)false), () => (V)(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_28, (V)"meta"), (V)"pending")))).First == V.Nil)))));
return Results.Empty;
})))));
R.Call(v_10, R.Join(new V[] {(V)"spectator floor inspection keeps world time and invulnerability"}, new Results(new V((RuleFunction)(args_45 => {

var values_46 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), R.Join(new V[] {(V)17.0d, R.Get(R.Get(v_2, (V)"order"), (V)1.0d)}, new Results(R.Get(R.Get(v_4, (V)"order"), (V)2.0d)))));
V v_47 = values_46[0];
var values_48 = R.Join(new V[] {}, new Results(R.Get(v_47, (V)"active_floor_id")));
V v_49 = values_48[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"switch_floor"), R.Join(new V[] {}, new Results(v_47)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_47, (V)"active_floor_id") != v_49), () => (V)(R.Get(v_47, (V)"active_floor_index") == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_47, (V)"clock") == (V)0.0d), () => R.Get(R.Call(R.Get(v_6, (V)"player"), R.Join(new V[] {}, new Results(v_47))).First, (V)"invulnerable")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"switch_floor"), R.Join(new V[] {}, new Results(v_47)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_47, (V)"active_floor_id") == v_49), () => (V)(R.Get(v_47, (V)"active_floor_index") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_47, (V)"clock") == (V)0.0d), () => (V)(R.Get(R.Get(v_47, (V)"meta"), (V)"eligible") == (V)false)))));
R.Call(R.Get(v_6, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_47)))));
return Results.Empty;
})))));
R.Call(v_10, R.Join(new V[] {(V)"spectator selections wrap without changing class or biome registries"}, new Results(new V((RuleFunction)(args_50 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"next_index"), R.Join(new V[] {R.Get(v_2, (V)"order")}, new Results((V)R.Length(R.Get(v_2, (V)"order"))))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"next_index"), R.Join(new V[] {R.Get(v_4, (V)"order")}, new Results((V)R.Length(R.Get(v_4, (V)"order"))))).First == (V)1.0d))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
