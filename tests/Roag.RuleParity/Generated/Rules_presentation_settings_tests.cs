// Static C# port of tests/presentation_settings_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 3d8d324ae7fa62debb06023c5f9c561a45945bb6f7c002f7f4390444afce0301
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_presentation_settings_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.presentation_settings"))));
V v_2 = values_1[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_3 => {
V v_4 = args_3[0];
V v_5 = args_3[1];
R.Call(v_4, R.Join(new V[] {(V)"presentation preferences persist independently and reject invalid values"}, new Results(new V((RuleFunction)(args_6 => {

var values_7 = R.Join(new V[] {}, new Results(R.Table()));
V v_8 = values_7[0];
R.Set(v_8, "read", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
return R.Join(new V[] {}, new Results(R.Get(v_10, v_11)));
return Results.Empty;
})));
R.Set(v_8, "write", new V((RuleFunction)(args_12 => {
V v_13 = args_12[0];
V v_14 = args_12[1];
V v_15 = args_12[2];
var values_16 = R.Join(new V[] {}, new Results(v_15));
var obj_17 = v_13;
V key_18 = v_14;
R.Set(obj_17, key_18, values_16[0]);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
var values_19 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"load"), R.Join(new V[] {}, new Results(v_8))));
V v_20 = values_19[0];
V v_21 = values_19[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_21 == (V)"default"), () => (V)(R.Get(v_20, (V)"music") == (V)80.0d)), () => R.Get(v_20, (V)"shake")))));
var values_22 = R.Join(new V[] {}, new Results((V)30.0d));
var obj_23 = v_20;
V key_24 = (V)"music";
R.Set(obj_23, key_24, values_22[0]);

var values_25 = R.Join(new V[] {}, new Results((V)"sans"));
var obj_26 = v_20;
V key_27 = (V)"font";
R.Set(obj_26, key_27, values_25[0]);

var values_28 = R.Join(new V[] {}, new Results((V)false));
var obj_29 = v_20;
V key_30 = (V)"shake";
R.Set(obj_29, key_30, values_28[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"save"), R.Join(new V[] {v_8}, new Results(v_20)))));
var values_31 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"load"), R.Join(new V[] {}, new Results(v_8))));
V v_32 = values_31[0];
V v_33 = values_31[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_33 == (V)"loaded"))));

R.Call(v_5, R.Join(new V[] {v_20}, new Results(v_32)));
var values_34 = R.Join(new V[] {}, new Results((V)55.5d));
var obj_35 = v_32;
V key_36 = (V)"music";
R.Set(obj_35, key_36, values_34[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_2, (V)"save"), R.Join(new V[] {v_8}, new Results(v_32))).First)))));
var values_37 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"load"), R.Join(new V[] {}, new Results(v_8))));
V v_38 = values_37[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_38, (V)"music") == (V)30.0d))));
var values_39 = R.Join(new V[] {}, new Results((V)"corrupt"));
var obj_40 = v_8;
V key_41 = R.Get(v_2, (V)"file");
R.Set(obj_40, key_41, values_39[0]);
var values_42 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"load"), R.Join(new V[] {}, new Results(v_8))));
V v_43 = values_42[0];
V v_44 = values_42[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_44 == (V)"invalid"), () => (V)(R.Get(v_43, (V)"music") == (V)80.0d)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
