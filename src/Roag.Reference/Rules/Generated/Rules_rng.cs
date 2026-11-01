// Static C# port of src/rng.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: cbe25c57dbd781388284e07f28d30d2f8d1cefc1af711254209a09b049640bf9
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_rng { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results((V)2147483647.0d));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results((V)48271.0d));
V v_6 = values_5[0];
R.Set(v_2, (V)"new", new V((RuleFunction)(args_7 => {
V v_8 = args_7[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_8))).First == (V)"number"), () => (V)((V)(v_8 % (V)1.0d) == (V)0.0d)), () => (V)(v_8 >= (V)1.0d)), () => (V)(v_8 < v_4))}, new Results((V)"seed must be an integer from 1 to 2147483646")));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_8))));
return Results.Empty;
})));
R.Set(v_2, (V)"next_raw", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_10, (V)"state")))).First == (V)"number"), () => (V)((V)(R.Get(v_10, (V)"state") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_10, (V)"state") >= (V)1.0d)), () => (V)(R.Get(v_10, (V)"state") < v_4))}, new Results((V)"invalid RNG state")));
var values_11 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_10, (V)"state") * v_6) % v_4)));
var obj_12 = v_10;
V key_13 = (V)"state";
R.Set(obj_12, key_13, values_11[0]);
return R.Join(new V[] {}, new Results(R.Get(v_10, (V)"state")));
return Results.Empty;
})));
R.Set(v_2, (V)"next_int", new V((RuleFunction)(args_14 => {
V v_15 = args_14[0];
V v_16 = args_14[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_16))).First == (V)"number"), () => (V)((V)(v_16 % (V)1.0d) == (V)0.0d)), () => (V)(v_16 >= (V)1.0d)), () => (V)(v_16 < v_4))}, new Results((V)"invalid RNG range")));
var values_17 = R.Join(new V[] {}, new Results((V)(v_4 - (V)1.0d)));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, new Results((V)(v_18 - (V)(v_18 % v_16))));
V v_20 = values_19[0];
var values_21 = Results.Empty;
V v_22 = values_21[0];
while (true) {
var values_23 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"next_raw"), R.Join(new V[] {}, new Results(v_15))).First - (V)1.0d)));
v_22 = values_23[0];
if (R.Truth((V)(v_22 < v_20))) break;
}
return R.Join(new V[] {}, new Results((V)((V)(v_22 % v_16) + (V)1.0d)));
return Results.Empty;
})));
R.Set(v_2, (V)"derive", new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
V v_26 = args_24[1];
R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {}, new Results(v_25)));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_26))).First == (V)"string"), () => (V)((V)R.Length(v_26) > (V)0.0d))}, new Results((V)"RNG stream needs a label")));
var values_27 = R.Join(new V[] {}, new Results(v_25));
V v_28 = values_27[0];
{ double end_31 = R.Num((V)R.Length(v_26)), step_32 = R.Num((V)1.0d);
for (double i_30 = R.Num((V)1.0d); step_32 > 0 ? i_30 <= end_31 : i_30 >= end_31; i_30 += step_32) {
V v_29 = i_30;
var values_33 = R.Join(new V[] {}, new Results((V)((V)((V)(v_28 * (V)131.0d) + R.Invoke(v_26, "byte", R.Join(new V[] {}, new Results(v_29))).First) % (V)(v_4 - (V)1.0d))));
v_28 = values_33[0];
}
}
return R.Join(new V[] {}, new Results((V)(v_28 + (V)1.0d)));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
