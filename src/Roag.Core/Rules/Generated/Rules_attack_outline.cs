// Static C# port of src/attack_outline.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 49cb0d954357968898e0c466f15701085663c6422e28de169c576eaefb397ba0
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_attack_outline { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
R.Set(v_2, (V)"rectangles", new V((RuleFunction)(args_3 => {
V v_4 = args_3[0];
var values_5 = R.Join(new V[] {(V)3.0d}, new Results((V)2.0d));
V v_6 = values_5[0];
V v_7 = values_5[1];
var values_8 = R.Join(new V[] {}, new Results((V)(v_4 - (V)(v_6 * (V)2.0d))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)4.0d}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_4 / (V)8.0d)))))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, new Results(R.Table()));
V v_13 = values_12[0];
{ double end_16 = R.Num((V)(v_9 - (V)1.0d)), step_17 = R.Num((V)(v_11 + (V)3.0d));
for (double i_15 = R.Num((V)0.0d); step_17 > 0 ? i_15 <= end_16 : i_15 >= end_16; i_15 += step_17) {
V v_14 = i_15;
var values_18 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_11}, new Results((V)(v_9 - v_14)))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)(v_6 + v_14)), new Entry((V)2, v_6), new Entry((V)3, v_19), new Entry((V)4, v_7))));
var obj_21 = v_13;
V key_22 = (V)((V)R.Length(v_13) + (V)1.0d);
R.Set(obj_21, key_22, values_20[0]);
var values_23 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)(v_6 + v_14)), new Entry((V)2, (V)((V)(v_4 - v_6) - v_7)), new Entry((V)3, v_19), new Entry((V)4, v_7))));
var obj_24 = v_13;
V key_25 = (V)((V)R.Length(v_13) + (V)1.0d);
R.Set(obj_24, key_25, values_23[0]);
var values_26 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_6), new Entry((V)2, (V)(v_6 + v_14)), new Entry((V)3, v_7), new Entry((V)4, v_19))));
var obj_27 = v_13;
V key_28 = (V)((V)R.Length(v_13) + (V)1.0d);
R.Set(obj_27, key_28, values_26[0]);
var values_29 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)((V)(v_4 - v_6) - v_7)), new Entry((V)2, (V)(v_6 + v_14)), new Entry((V)3, v_7), new Entry((V)4, v_19))));
var obj_30 = v_13;
V key_31 = (V)((V)R.Length(v_13) + (V)1.0d);
R.Set(obj_30, key_31, values_29[0]);
}
}
return R.Join(new V[] {}, new Results(v_13));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
