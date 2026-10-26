// Static C# port of tests/run_seed_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 1d88b08e18551aed703441a3bf2283fec01492dc96d23f20d0d40d14b81121d6
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_run_seed_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.profile"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.run_seed"))));
V v_8 = values_7[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
R.Call(v_10, R.Join(new V[] {(V)"successive new runs get distinct valid maps across profile reloads"}, new Results(new V((RuleFunction)(args_11 => {

var values_12 = R.Join(new V[] {}, new Results(R.Table()));
V v_13 = values_12[0];
R.Set(v_13, "read", new V((RuleFunction)(args_14 => {
V v_15 = args_14[0];
V v_16 = args_14[1];
return R.Join(new V[] {}, new Results(R.Get(v_15, v_16)));
return Results.Empty;
})));
R.Set(v_13, "write", new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
V v_20 = args_17[2];
var values_21 = R.Join(new V[] {}, new Results(v_20));
var obj_22 = v_18;
V key_23 = v_19;
R.Set(obj_22, key_23, values_21[0]);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
var values_24 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results(v_13))));
V v_25 = values_24[0];
var values_26 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_25, "allocate_run", Results.Empty))));
V v_27 = values_26[0];
V v_28 = values_26[1];
var values_29 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"for_serial"), R.Join(new V[] {}, new Results((V)(R.Get(v_28, (V)"next_run_serial") - (V)1.0d)))));
V v_30 = values_29[0];
var values_31 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results(v_13))));
V v_32 = values_31[0];
var values_33 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_32, "allocate_run", Results.Empty))));
V v_34 = values_33[0];
V v_35 = values_33[1];
var values_36 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"for_serial"), R.Join(new V[] {(V)(R.Get(v_35, (V)"next_run_serial") - (V)1.0d)}, new Results(v_30))));
V v_37 = values_36[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_30 != v_37))));
R.Call(R.Get(v_6, (V)"new"), R.Join(new V[] {}, new Results(v_30)));

R.Call(R.Get(v_6, (V)"new"), R.Join(new V[] {}, new Results(v_37)));
var values_38 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {}, new Results(v_30))).First, (V)"generated_map")));
V v_39 = values_38[0];
var values_40 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {}, new Results(v_37))).First, (V)"generated_map")));
V v_41 = values_40[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(R.Get(v_39, (V)"rows")))).First != R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(R.Get(v_41, (V)"rows")))).First))));
return Results.Empty;
})))));
R.Call(v_10, R.Join(new V[] {(V)"new run seed skips the current seed even after an older manual choice"}, new Results(new V((RuleFunction)(args_42 => {

var values_43 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"for_serial"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_44 = values_43[0];
var values_45 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"for_serial"), R.Join(new V[] {(V)3.0d}, new Results(v_44))));
V v_46 = values_45[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_46 != v_44))));
R.Call(R.Get(v_6, (V)"new"), R.Join(new V[] {}, new Results(v_46)));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
