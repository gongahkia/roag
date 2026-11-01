// Static C# port of src/ui_fonts.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: bc9d772b1aa7a8674d0b776dab82fcb9254eb9613fb17ccd7255f47f3c9fdd5b
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_ui_fonts { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results((V)"assets/fonts/BigBlue_TerminalPlus.ttf"));
V v_4 = values_3[0];
R.Set(v_2, (V)"load", new V((RuleFunction)(args_5 => {
V v_6 = args_5[0];
V v_7 = args_5[1];
var values_8 = R.Join(new V[] {}, new Results(R.Get(rt.Global("love"), (V)"graphics")));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"getFont"), Results.Empty));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, new Results((V)(v_6 != (V)"sans")));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Table(new Entry((V)"small", R.Table(new Entry((V)1, (V)10.0d), new Entry((V)2, (V)20.0d), new Entry((V)3, (V)14.0d))), new Entry((V)"normal", R.Table(new Entry((V)1, (V)12.0d), new Entry((V)2, (V)24.0d), new Entry((V)3, (V)16.0d))), new Entry((V)"large", R.Table(new Entry((V)1, (V)14.0d), new Entry((V)2, (V)26.0d), new Entry((V)3, (V)18.0d)))), v_7), () => R.Table(new Entry((V)1, (V)12.0d), new Entry((V)2, (V)24.0d), new Entry((V)3, (V)16.0d)))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, new Results(R.Or(R.And(v_13, () => v_4), () => V.Nil)));
V v_17 = values_16[0];
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
if (R.Truth(v_17)) {
return R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"newFont"), R.Join(new V[] {v_17}, new Results(v_20))));
}
return R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"newFont"), R.Join(new V[] {}, new Results(v_20))));
return Results.Empty;
}));
var values_21 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_15, (V)1.0d)))));
V v_22 = values_21[0];
var values_23 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_15, (V)2.0d)))));
V v_24 = values_23[0];
var values_25 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_15, (V)3.0d)))));
V v_26 = values_25[0];
if (R.Truth(v_13)) {
R.Invoke(v_22, "setFilter", R.Join(new V[] {(V)"nearest"}, new Results((V)"nearest")));
R.Invoke(v_24, "setFilter", R.Join(new V[] {(V)"nearest"}, new Results((V)"nearest")));
R.Invoke(v_26, "setFilter", R.Join(new V[] {(V)"nearest"}, new Results((V)"nearest")));
}
R.Invoke(v_22, "setFallbacks", R.Join(new V[] {}, new Results(v_11)));
R.Invoke(v_24, "setFallbacks", R.Join(new V[] {}, new Results(v_11)));
R.Invoke(v_26, "setFallbacks", R.Join(new V[] {}, new Results(v_11)));
return R.Join(new V[] {v_22, v_24}, new Results(v_26));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
