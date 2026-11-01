// Static C# port of src/spectator.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 099de49c2d9f7fae0947bd8ba5d078f12541d1f4198f2bb0416c57ad60efc2de
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_spectator { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievements"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, new Results(R.Table()));
V v_16 = values_15[0];
R.Set(v_16, (V)"new", new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
V v_20 = args_17[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_19))).First}, new Results((V)"unknown spectator biome")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_6, (V)"get"), R.Join(new V[] {}, new Results(v_20))).First}, new Results((V)"unknown spectator class")));
var values_21 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"new_stage"), R.Join(new V[] {v_18}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"stage_options", R.Table(new Entry((V)"index", (V)1.0d), new Entry((V)"biome_id", v_19))), new Entry((V)"class_id", v_20), new Entry((V)"meta", R.Call(R.Get(v_2, (V)"new_run"), R.Join(new V[] {}, new Results((V)false))).First))))));
V v_22 = values_21[0];
var values_23 = R.Join(new V[] {}, new Results((V)true));
var obj_24 = R.Call(R.Get(v_12, (V)"player"), R.Join(new V[] {}, new Results(v_22))).First;
V key_25 = (V)"invulnerable";
R.Set(obj_24, key_25, values_23[0]);
return R.Join(new V[] {}, new Results(v_22));
return Results.Empty;
})));
R.Set(v_16, (V)"next_index", new V((RuleFunction)(args_26 => {
V v_27 = args_26[0];
V v_28 = args_26[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_27))).First == (V)"table"), () => (V)((V)R.Length(v_27) > (V)0.0d))}, new Results((V)"empty spectator selection")));
return R.Join(new V[] {}, new Results((V)((V)(v_28 % (V)R.Length(v_27)) + (V)1.0d)));
return Results.Empty;
})));
R.Set(v_16, (V)"camera_step", new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
V v_32 = args_29[2];
V v_33 = args_29[3];
V v_34 = args_29[4];
var values_35 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_8, (V)"map_for"), R.Join(new V[] {}, new Results(v_30))).First}, new Results((V)"missing spectator map"))));
V v_36 = values_35[0];
var values_37 = R.Join(new V[] {}, new Results(R.Or(R.And(v_34, () => (V)8.0d), () => (V)1.0d)));
V v_38 = values_37[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_36, (V)"width")}, new Results((V)(R.Get(v_31, (V)"x") + (V)(v_32 * v_38))))))).First), new Entry((V)"y", R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_36, (V)"height")}, new Results((V)(R.Get(v_31, (V)"y") + (V)(v_33 * v_38))))))).First))));
return Results.Empty;
})));
R.Set(v_16, (V)"switch_floor", new V((RuleFunction)(args_39 => {
V v_40 = args_39[0];
var values_41 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_8, (V)"map_for"), R.Join(new V[] {}, new Results(v_40))).First}, new Results((V)"missing spectator map"))));
V v_42 = values_41[0];
foreach (var row_43 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_42, (V)"placements"), () => R.Get(R.Get(v_42, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_44 = row_43[0];
V v_45 = row_43[1];
var values_46 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"landing"), R.Join(new V[] {v_40}, new Results(v_45))));
V v_47 = values_46[0];
if (R.Truth(v_47)) {
var values_48 = R.Join(new V[] {}, new Results(R.Get(v_40, (V)"clock")));
V v_49 = values_48[0];
R.Call(R.Get(v_10, (V)"change"), R.Join(new V[] {v_40, v_45}, new Results(v_47)));
R.Call(R.Get(v_14, (V)"recompute"), R.Join(new V[] {}, new Results(v_40)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_40, (V)"clock") == v_49)}, new Results((V)"spectator floor switch advanced time")));
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {(V)false}, new Results((V)"no usable stair on this floor"));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_16));
return Results.Empty;
} }
