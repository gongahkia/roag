// Static C# port of src/title_menu.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 043e3f3928816a38454ff6d515730dfa0f1b7c2ba2e2d98d8604b8cfa738021f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_title_menu { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table()));
V v_8 = values_7[0];
R.Set(v_8, (V)"resumable", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(v_10, () => R.Or((V)(R.Get(v_10, (V)"status") == (V)"active"), () => (V)(R.Get(v_10, (V)"status") == (V)"recovered"))), () => (V)false)));
return Results.Empty;
})));
R.Set(v_8, (V)"default_selection", new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.Call(R.Get(v_8, (V)"resumable"), R.Join(new V[] {}, new Results(v_12))).First, () => (V)1.0d), () => (V)2.0d)));
return Results.Empty;
})));
R.Set(v_8, (V)"save_summary", new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
if (R.Truth(R.Or((V)(!R.Truth(v_14)), () => (V)(R.Get(v_14, (V)"status") == (V)"absent")))) {
return R.Join(new V[] {(V)"NO SAVED RUN"}, new Results((V)"Choose New Game to begin"));
}
if (R.Truth(R.And(R.Call(R.Get(v_8, (V)"resumable"), R.Join(new V[] {}, new Results(v_14))).First, () => R.Get(v_14, (V)"state")))) {
var values_15 = R.Join(new V[] {}, new Results(R.Get(v_14, (V)"state")));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"player"), R.Join(new V[] {}, new Results(v_16))));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_18, (V)"definition_id")))));
V v_20 = values_19[0];
var values_21 = R.Join(new V[] {}, new Results(R.And(R.Get(v_16, (V)"stage"), () => R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"stage"), (V)"biome_id")))).First)));
V v_22 = values_21[0];
var values_23 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_16, (V)"finale"), () => (V)"Final Sanctum"), () => R.Or(R.And(v_22, () => R.Get(v_22, (V)"name")), () => (V)"Unknown biome"))));
V v_24 = values_23[0];
var values_25 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_16, (V)"finale"), () => (V)"Final encounter"), () => R.Invoke((V)"Stage %d \u00c2\u00b7 Floor %d", "format", R.Join(new V[] {R.Or(R.And(R.Get(v_16, (V)"stage"), () => R.Get(R.Get(v_16, (V)"stage"), (V)"index")), () => (V)1.0d)}, new Results(R.Or(R.Get(v_16, (V)"active_floor_index"), () => (V)1.0d)))).First)));
V v_26 = values_25[0];
return R.Join(new V[] {R.Concat(R.Or(R.And((V)(R.Get(v_14, (V)"status") == (V)"recovered"), () => (V)"RECOVERED \u00c2\u00b7 "), () => (V)""), R.Concat(R.Or(R.And(v_20, () => R.Get(v_20, (V)"name")), () => (V)"Unknown class"), R.Concat((V)" \u00c2\u00b7 Lv ", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_18, (V)"progression"), () => R.Get(R.Get(v_18, (V)"progression"), (V)"level")), () => (V)1.0d)))).First, R.Concat((V)" \u00c2\u00b7 Seed ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Or(R.Or(R.Get(v_16, (V)"root_seed"), () => R.Get(v_16, (V)"run_seed")), () => (V)"?")))).First)))))}, R.Invoke((V)"%s \u00c2\u00b7 %s \u00c2\u00b7 HP %d/%d", "format", R.Join(new V[] {v_24, v_26, R.Get(v_18, (V)"hp")}, new Results(R.Get(v_18, (V)"max_hp")))));
}
if (R.Truth((V)(R.Get(v_14, (V)"status") == (V)"pending_terminal"))) {
return R.Join(new V[] {(V)"UNLOCK RECOVERY NEEDED"}, new Results((V)"Press R before replacing this save"));
} else if (R.Truth((V)(R.Get(v_14, (V)"status") == (V)"terminal"))) {
return R.Join(new V[] {(V)"RUN COMPLETE"}, new Results((V)"Available for a new game"));
} else if (R.Truth((V)(R.Get(v_14, (V)"status") == (V)"incompatible"))) {
return R.Join(new V[] {(V)"INCOMPATIBLE SAVE"}, new Results((V)"Preserved; new game requires confirmation"));
} else if (R.Truth((V)(R.Get(v_14, (V)"status") == (V)"corrupt"))) {
return R.Join(new V[] {(V)"DAMAGED SAVE"}, new Results((V)"Preserved; new game requires confirmation"));
}
return R.Join(new V[] {(V)"UNAVAILABLE"}, new Results((V)"Cannot continue this save"));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_8));
return Results.Empty;
} }
