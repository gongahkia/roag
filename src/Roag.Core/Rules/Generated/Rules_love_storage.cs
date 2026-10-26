// Static C# port of src/love_storage.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 5efbc7dcf9cdec07f3446d1f1302e34b85fdd6025d22f77da748ae3e69072b77
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_love_storage { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
R.Set(v_2, "read", new V((RuleFunction)(args_3 => {
V v_4 = args_3[0];
V v_5 = args_3[1];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(R.Get(rt.Global("love"), (V)"filesystem"), (V)"getInfo"), R.Join(new V[] {}, new Results(v_5))).First)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_6 = R.Join(new V[] {}, R.Call(R.Get(R.Get(rt.Global("love"), (V)"filesystem"), (V)"read"), R.Join(new V[] {}, new Results(v_5))));
V v_7 = values_6[0];
V v_8 = values_6[1];
if (R.Truth((V)(!R.Truth(v_7)))) {
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results(R.Or(v_8, () => (V)"save read failed"))));
}
return R.Join(new V[] {}, new Results(v_7));
return Results.Empty;
})));
R.Set(v_2, "write", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
V v_12 = args_9[2];
var values_13 = R.Join(new V[] {}, R.Call(R.Get(R.Get(rt.Global("love"), (V)"filesystem"), (V)"write"), R.Join(new V[] {v_11}, new Results(v_12))));
V v_14 = values_13[0];
V v_15 = values_13[1];
if (R.Truth((V)(!R.Truth(v_14)))) {
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results(R.Or(v_15, () => (V)"save write failed"))));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
