// Static C# port of tests/idea_seeds.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: b1a5483ddc75f7bd2e9e0b786a64f26a25a9728000c0a2b725a04ace4ecb4550
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_seeds { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table()));
V v_6 = values_5[0];
R.Set(v_6, (V)"find", new V((RuleFunction)(args_7 => {
V v_8 = args_7[0];
V v_9 = args_7[1];
var values_10 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)1.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_11 = values_10[0];
{ double end_14 = R.Num(R.Or(v_9, () => (V)512.0d)), step_15 = R.Num((V)1.0d);
for (double i_13 = R.Num((V)1.0d); step_15 > 0 ? i_13 <= end_14 : i_13 >= end_14; i_13 += step_15) {
V v_12 = i_13;
var values_16 = R.Join(new V[] {}, new Results((V)false));
V v_17 = values_16[0];
foreach (var row_18 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_11, (V)"floor_order"))))))) {
V v_19 = row_18[0];
V v_20 = row_18[1];
var values_21 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"selection"), R.Join(new V[] {v_12}, new Results(v_20))));
V v_22 = values_21[0];
if (R.Truth(R.And(v_22, () => (V)(R.Concat((V)"core:enemy/", R.Get(v_22, (V)1.0d)) == v_8)))) {
var values_23 = R.Join(new V[] {}, new Results((V)true));
v_17 = values_23[0];

break;
}
}
if (R.Truth(v_17)) {
var values_24 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {v_12}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_25 = values_24[0];
foreach (var row_26 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_25, (V)"entities"))))))) {
V v_27 = row_26[0];
V v_28 = row_26[1];
if (R.Truth((V)(R.Get(v_28, (V)"definition_id") == v_8))) {
return R.Join(new V[] {v_25}, new Results(v_12));
}
}
}
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results(R.Concat((V)"no real generated ", R.Concat(v_8, (V)" in bounded seed search")))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
