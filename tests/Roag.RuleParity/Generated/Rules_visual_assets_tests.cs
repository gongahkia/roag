// Static C# port of tests/visual_assets_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: cb0d169068281ebc99cf2533384615557c80210fbf05f1b3db533fa9f1683d56
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visual_assets_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_assets"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_slots"))));
V v_4 = values_3[0];
V v_5 = V.Nil;
v_5 = new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
var values_8 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {v_7}, new Results((V)"rb"))));
V v_9 = values_8[0];

if (R.Truth((V)(!R.Truth(v_9)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_10 = R.Join(new V[] {}, R.Invoke(v_9, "read", R.Join(new V[] {}, new Results((V)"*a"))));
V v_11 = values_10[0];

R.Invoke(v_9, "close", Results.Empty);

return R.Join(new V[] {}, new Results(v_11));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_12 => {
V v_13 = args_12[0];
R.Call(v_13, R.Join(new V[] {(V)"connected wall borders use known neighbors without exposing hidden geometry"}, new Results(new V((RuleFunction)(args_14 => {

var values_15 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rows", R.Table(new Entry((V)1, (V)"..."), new Entry((V)2, (V)".#B"), new Entry((V)3, (V)"..."))))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"wall_edges"), R.Join(new V[] {v_16, (V)2.0d}, new Results((V)2.0d))));
V v_18 = values_17[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(v_18, (V)"n"), () => R.Get(v_18, (V)"s")), () => R.Get(v_18, (V)"w")), () => (V)(!R.Truth(R.Get(v_18, (V)"e")))))));
var values_22 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"wall_edges"), R.Join(new V[] {v_16, (V)2.0d, (V)2.0d}, new Results(new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
return R.Join(new V[] {}, new Results((V)(v_21 < (V)2.0d)));
return Results.Empty;
}))))));
v_18 = values_22[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(v_18, (V)"n"), () => (V)(!R.Truth(R.Get(v_18, (V)"s")))), () => (V)(!R.Truth(R.Get(v_18, (V)"w")))), () => (V)(!R.Truth(R.Get(v_18, (V)"e")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_16, (V)"rows"), (V)2.0d) == (V)".#B"))));
return Results.Empty;
})))));
R.Call(v_13, R.Join(new V[] {(V)"default checkout has no hand-painted published art override"}, new Results(new V((RuleFunction)(args_23 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_5, R.Join(new V[] {}, new Results((V)"assets/studio/manifest.txt"))).First == V.Nil))));
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_5}, new Results(new V((RuleFunction)(args_24 => {

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"unexpected image load")));
return Results.Empty;
}))))));
V v_26 = values_25[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_26, (V)"status") == (V)"absent"), () => (V)(!R.Truth(R.Invoke(v_26, "resolve", R.Join(new V[] {}, new Results((V)"core:actor/adventurer"))).First))))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
