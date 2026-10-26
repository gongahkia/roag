// Static C# port of tests/visual_packs_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 8ce944127d830aa7603e8fd0d7a2f4db45a2c753f8fa9ef6c53f1a9af134a89f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visual_packs_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_packs"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_4 = values_3[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_5 => {
V v_6 = args_5[0];
V v_7 = args_5[1];
R.Call(v_6, R.Join(new V[] {(V)"game appearance is fixed and exposes no preference store or selectable list"}, new Results(new V((RuleFunction)(args_8 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_2, (V)"default") == (V)"onebit"), () => (V)(R.Get(v_2, (V)"label") == (V)"Kenney 1-Bit")))));
foreach (var row_9 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"order"), new Entry((V)2, (V)"labels"), new Entry((V)3, (V)"valid"), new Entry((V)4, (V)"index"), new Entry((V)5, (V)"save"), new Entry((V)6, (V)"inspect"), new Entry((V)7, (V)"files"), new Entry((V)8, (V)"format")))))))) {
V v_10 = row_9[0];
V v_11 = row_9[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_2, v_11) == V.Nil)}, new Results(v_11)));
}
return Results.Empty;
})))));
R.Call(v_6, R.Join(new V[] {(V)"fixed visual builder rejects alternate renderer injection"}, new Results(new V((RuleFunction)(args_12 => {

foreach (var row_13 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"classic"), new Entry((V)2, (V)"kenney"), new Entry((V)3, (V)"automatic"), new Entry((V)4, (V)"generated"), new Entry((V)5, (V)"published"), new Entry((V)6, (V)"unsafe")))))))) {
V v_14 = row_13[0];
V v_15 = row_13[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"build"), new V((RuleFunction)(args_16 => {


return Results.Empty;
})), new V((RuleFunction)(args_17 => {


return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_18 => {

return R.Join(new V[] {R.Table(new Entry((V)"style", v_15))}, new Results((V)"loaded"));
return Results.Empty;
}))))).First)))));
}
return Results.Empty;
})))));
R.Call(v_6, R.Join(new V[] {(V)"fixed visuals bypass published overrides and resolve only the native onebit source"}, new Results(new V((RuleFunction)(args_19 => {

var values_20 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new_stage"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_21 = values_20[0];

var values_22 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_21))));
V v_23 = values_22[0];

var values_24 = R.Join(new V[] {}, new Results((V)0.0d));
V v_25 = values_24[0];
var values_29 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"style", (V)"onebit"), new Entry((V)"resolve", new V((RuleFunction)(args_26 => {
V v_27 = args_26[0];
V v_28 = args_26[1];
if (R.Truth((V)(v_28 == (V)"core:actor/adventurer"))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"source", (V)"onebit"))));
}
return Results.Empty;
}))))));
V v_30 = values_29[0];
V v_31 = V.Nil;
v_31 = new V((RuleFunction)(args_32 => {

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"retired published artwork must not be read")));
return Results.Empty;
}));
var values_37 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"build"), R.Join(new V[] {v_31, new V((RuleFunction)(args_33 => {

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"unexpected loose image load")));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_34 => {
V v_35 = args_34[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_35 == v_31))));

var values_36 = R.Join(new V[] {}, new Results((V)(v_25 + (V)1.0d)));
v_25 = values_36[0];

return R.Join(new V[] {v_30}, new Results((V)"loaded"));
return Results.Empty;
}))))));
V v_38 = values_37[0];
V v_39 = values_37[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_39 == (V)"loaded"), () => (V)(R.Get(v_38, (V)"status") == (V)"generated")), () => (V)(v_25 == (V)1.0d)), () => R.Get(v_38, (V)"has_terrain")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_38, "resolve", R.Join(new V[] {}, new Results((V)"core:actor/adventurer"))).First, (V)"source") == (V)"onebit"))));

R.Call(v_7, R.Join(new V[] {v_23}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_21)))));
return Results.Empty;
})))));
R.Call(v_6, R.Join(new V[] {(V)"missing fixed artwork permits only automatic emergency drawing without writing preferences"}, new Results(new V((RuleFunction)(args_40 => {

var values_41 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new_stage"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_42 = values_41[0];

var values_43 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_42))));
V v_44 = values_43[0];
var values_45 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"visual-pack-a.roag", (V)"old classic choice"), new Entry((V)"visual-pack-b.roag", (V)"old mixed choice"))));
V v_46 = values_45[0];
V v_47 = V.Nil;
v_47 = new V((RuleFunction)(args_48 => {
V v_49 = args_48[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_49, "match", R.Join(new V[] {}, new Results((V)"visual%-pack"))).First)))));

return R.Join(new V[] {}, new Results(R.Get(v_46, v_49)));
return Results.Empty;
}));
var values_52 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"build"), R.Join(new V[] {v_47, new V((RuleFunction)(args_50 => {

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"unexpected image load")));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_51 => {

return R.Join(new V[] {V.Nil}, new Results((V)"absent"));
return Results.Empty;
}))))));
V v_53 = values_52[0];
V v_54 = values_52[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_54 == (V)"absent"), () => (V)(R.Get(v_53, (V)"status") == (V)"absent")), () => (V)(!R.Truth(R.Invoke(v_53, "resolve", R.Join(new V[] {}, new Results((V)"core:actor/adventurer"))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_46, (V)"visual-pack-a.roag") == (V)"old classic choice"), () => (V)(R.Get(v_46, (V)"visual-pack-b.roag") == (V)"old mixed choice")))));
R.Call(v_7, R.Join(new V[] {v_44}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_42)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
