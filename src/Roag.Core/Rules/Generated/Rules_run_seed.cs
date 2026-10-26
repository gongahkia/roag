// Static C# port of src/run_seed.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 17784fed00de3b64cef93c4daf942095194b2e211c0f308ae1b00868e17fdffc
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_run_seed { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table()));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results((V)2147483646.0d));
V v_6 = values_5[0];
R.Set(v_4, (V)"for_serial", new V((RuleFunction)(args_7 => {
V v_8 = args_7[0];
V v_9 = args_7[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_8))).First == (V)"number"), () => (V)((V)(v_8 % (V)1.0d) == (V)0.0d)), () => (V)(v_8 >= (V)1.0d))}, new Results((V)"invalid run serial")));
var values_10 = R.Join(new V[] {}, new Results((V)((V)((V)(v_8 - (V)1.0d) % v_6) + (V)1.0d)));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"next_raw"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {}, new Results(v_11))))));
V v_13 = values_12[0];
if (R.Truth((V)(v_13 == v_9))) {
var values_14 = R.Join(new V[] {}, new Results((V)((V)(v_13 % v_6) + (V)1.0d)));
v_13 = values_14[0];
}
return R.Join(new V[] {}, new Results(v_13));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
