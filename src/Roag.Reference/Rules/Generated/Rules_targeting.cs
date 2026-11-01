// Static C# port of src/targeting.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: aeacae896aab10050dc8b6ade29575a8b446a7187f1c6b23b49e10898d3c4225
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_targeting { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)6, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))))));
V v_4 = values_3[0];
R.Set(v_2, (V)"direction_index", new V((RuleFunction)(args_5 => {
V v_6 = args_5[0];
V v_7 = args_5[1];
foreach (var row_8 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_4)))))) {
V v_9 = row_8[0];
V v_10 = row_8[1];
if (R.Truth(R.And((V)(v_6 == R.Get(v_10, (V)1.0d)), () => (V)(v_7 == R.Get(v_10, (V)2.0d))))) {
return R.Join(new V[] {}, new Results(v_9));
}
}
return R.Join(new V[] {}, new Results(V.Nil));
return Results.Empty;
})));
R.Set(v_2, (V)"is_cardinal", new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
V v_13 = args_11[1];
var values_14 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"direction_index"), R.Join(new V[] {v_12}, new Results(v_13))));
V v_15 = values_14[0];
return R.Join(new V[] {}, new Results(R.And((V)(v_15 != V.Nil), () => (V)((V)(v_15 % (V)2.0d) == (V)1.0d))));
return Results.Empty;
})));
R.Set(v_2, (V)"cells", new V((RuleFunction)(args_16 => {
V v_17 = args_16[0];
V v_18 = args_16[1];
V v_19 = args_16[2];
V v_20 = args_16[3];
V v_21 = args_16[4];
var values_22 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"direction_index"), R.Join(new V[] {v_20}, new Results(v_21))));
V v_23 = values_22[0];
if (R.Truth((V)(!R.Truth(v_23)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
if (R.Truth((V)(v_17 == (V)"basic_attack"))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)(v_18 + v_20)), new Entry((V)"y", (V)(v_19 + v_21)))))));
}
if (R.Truth((V)(v_17 == (V)"sweeping_slash"))) {
var values_24 = R.Join(new V[] {}, new Results(R.Table()));
V v_25 = values_24[0];
foreach (var row_26 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)1.0d)))))))) {
V v_27 = row_26[0];
V v_28 = row_26[1];
var values_29 = R.Join(new V[] {}, new Results(R.Get(v_4, (V)((V)((V)((V)(v_23 + v_28) - (V)1.0d) % (V)8.0d) + (V)1.0d))));
V v_30 = values_29[0];
var values_31 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)(v_18 + R.Get(v_30, (V)1.0d))), new Entry((V)"y", (V)(v_19 + R.Get(v_30, (V)2.0d))))));
var obj_32 = v_25;
V key_33 = (V)((V)R.Length(v_25) + (V)1.0d);
R.Set(obj_32, key_33, values_31[0]);
}
return R.Join(new V[] {}, new Results(v_25));
}
return R.Join(new V[] {}, new Results(V.Nil));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
