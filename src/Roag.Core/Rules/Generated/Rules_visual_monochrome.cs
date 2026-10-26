// Static C# port of src/visual_monochrome.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 86937e6504365328699ec5d751100ba357c77e4671aa607b339ac7307093c058
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visual_monochrome { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"threshold", (V)0.42d), new Entry((V)"dark", (V)0.07d), new Entry((V)"light", (V)0.9d))));
V v_2 = values_1[0];
R.Set(v_2, (V)"pixel", new V((RuleFunction)(args_3 => {
V v_4 = args_3[0];
V v_5 = args_3[1];
V v_6 = args_3[2];
V v_7 = args_3[3];
var values_8 = R.Join(new V[] {}, new Results(R.Or(R.And((V)((V)((V)((V)(v_4 * (V)0.2126d) + (V)(v_5 * (V)0.7152d)) + (V)(v_6 * (V)0.0722d)) >= R.Get(v_2, (V)"threshold")), () => R.Get(v_2, (V)"light")), () => R.Get(v_2, (V)"dark"))));
V v_9 = values_8[0];
return R.Join(new V[] {v_9, v_9, v_9}, new Results(v_7));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
