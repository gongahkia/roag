// Static C# port of tests/pixel_ui_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: e04f43a5cc4000201ec0beceabb6aec29202628320a12a1f8733b637f77587bd
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_pixel_ui_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.pixel_ui"))));
V v_2 = values_1[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_3 => {
V v_4 = args_3[0];
R.Call(v_4, R.Join(new V[] {(V)"1-Bit UI palette is neutral and other packs restore color"}, new Results(new V((RuleFunction)(args_5 => {

R.Call(R.Get(v_2, (V)"set_style"), R.Join(new V[] {}, new Results((V)"onebit")));
foreach (var row_6 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"colors"))))))) {
V v_7 = row_6[0];
V v_8 = row_6[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_8, (V)1.0d) == R.Get(v_8, (V)2.0d)), () => (V)(R.Get(v_8, (V)2.0d) == R.Get(v_8, (V)3.0d))))));
}
R.Call(R.Get(v_2, (V)"set_style"), R.Join(new V[] {}, new Results((V)"kenney")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_2, (V)"colors"), (V)"selected"), (V)1.0d) != R.Get(R.Get(R.Get(v_2, (V)"colors"), (V)"selected"), (V)2.0d)))));
R.Call(R.Get(v_2, (V)"set_style"), R.Join(new V[] {}, new Results((V)"classic")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_2, (V)"colors"), (V)"selected"), (V)1.0d) != R.Get(R.Get(R.Get(v_2, (V)"colors"), (V)"selected"), (V)2.0d)))));
return Results.Empty;
})))));
R.Call(v_4, R.Join(new V[] {(V)"focused menu list hitboxes select exact rows and reject gaps and edges"}, new Results(new V((RuleFunction)(args_9 => {

{ double end_12 = R.Num((V)5.0d), step_13 = R.Num((V)1.0d);
for (double i_11 = R.Num((V)1.0d); step_13 > 0 ? i_11 <= end_12 : i_11 >= end_12; i_11 += step_13) {
V v_10 = i_11;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"home_at"), R.Join(new V[] {(V)500.0d}, new Results((V)((V)((V)232.0d + (V)((V)(v_10 - (V)1.0d) * (V)42.0d)) + (V)12.0d)))).First == v_10))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_2, (V)"home_at"), R.Join(new V[] {(V)500.0d}, new Results((V)270.0d))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_2, (V)"home_at"), R.Join(new V[] {(V)744.0d}, new Results((V)240.0d))).First))))));
{ double end_16 = R.Num((V)9.0d), step_17 = R.Num((V)1.0d);
for (double i_15 = R.Num((V)1.0d); step_17 > 0 ? i_15 <= end_16 : i_15 >= end_16; i_15 += step_17) {
V v_14 = i_15;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"class_at"), R.Join(new V[] {(V)300.0d}, new Results((V)((V)((V)150.0d + (V)((V)(v_14 - (V)1.0d) * (V)36.0d)) + (V)10.0d)))).First == v_14))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_2, (V)"class_at"), R.Join(new V[] {(V)500.0d}, new Results((V)200.0d))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_2, (V)"class_at"), R.Join(new V[] {(V)300.0d}, new Results((V)185.0d))).First))))));
return Results.Empty;
})))));
R.Call(v_4, R.Join(new V[] {(V)"sound, effects and accessibility rows fit the shared settings layout"}, new Results(new V((RuleFunction)(args_18 => {

var values_19 = R.Join(new V[] {}, new Results((V)4.0d));
V v_20 = values_19[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_packs"))).First, (V)"order") == V.Nil))));
{ double end_23 = R.Num(v_20), step_24 = R.Num((V)1.0d);
for (double i_22 = R.Num((V)1.0d); step_24 > 0 ? i_22 <= end_23 : i_22 >= end_23; i_22 += step_24) {
V v_21 = i_22;
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"settings_row"), R.Join(new V[] {(V)190.0d, (V)55.0d, (V)644.0d}, new Results(v_21))));
V v_26 = values_25[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)(R.Get(v_26, (V)"y") + R.Get(v_26, (V)"h")) < (V)((V)55.0d + (V)422.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"settings_at"), R.Join(new V[] {(V)190.0d, (V)55.0d, (V)644.0d, v_20, (V)(R.Get(v_26, (V)"x") + (V)4.0d)}, new Results((V)(R.Get(v_26, (V)"y") + (V)4.0d)))).First == v_21))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_2, (V)"settings_at"), R.Join(new V[] {(V)190.0d, (V)55.0d, (V)644.0d, v_20, R.Get(v_26, (V)"x")}, new Results((V)((V)(R.Get(v_26, (V)"y") + R.Get(v_26, (V)"h")) + (V)1.0d)))).First)))));
}
}
return Results.Empty;
})))));
R.Call(v_4, R.Join(new V[] {(V)"pause chest and route list targets match their visible choices"}, new Results(new V((RuleFunction)(args_27 => {

{ double end_30 = R.Num((V)3.0d), step_31 = R.Num((V)1.0d);
for (double i_29 = R.Num((V)1.0d); step_31 > 0 ? i_29 <= end_30 : i_29 >= end_30; i_29 += step_31) {
V v_28 = i_29;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"pause_at"), R.Join(new V[] {(V)500.0d}, new Results((V)((V)((V)252.0d + (V)((V)(v_28 - (V)1.0d) * (V)44.0d)) + (V)8.0d)))).First == v_28))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"chest_at"), R.Join(new V[] {(V)300.0d}, new Results((V)((V)((V)178.0d + (V)((V)(v_28 - (V)1.0d) * (V)76.0d)) + (V)8.0d)))).First == v_28))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"route_at"), R.Join(new V[] {(V)3.0d, (V)500.0d}, new Results((V)((V)((V)216.0d + (V)((V)(v_28 - (V)1.0d) * (V)64.0d)) + (V)8.0d)))).First == v_28))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_2, (V)"route_at"), R.Join(new V[] {(V)1.0d, (V)500.0d}, new Results((V)300.0d))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_2, (V)"chest_at"), R.Join(new V[] {(V)600.0d}, new Results((V)190.0d))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_2, (V)"pause_at"), R.Join(new V[] {(V)200.0d}, new Results((V)270.0d))).First))))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
