// Static C# port of src/visual_packs.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 74e81aa5bdc1ac7ced34c29afd77d4b9e42023c396fcc1f2d6616423693f2fc8
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visual_packs { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_assets"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"default", (V)"onebit"), new Entry((V)"label", (V)"Kenney 1-Bit"))));
V v_4 = values_3[0];
R.Set(v_4, (V)"build", new V((RuleFunction)(args_5 => {
V v_6 = args_5[0];
V v_7 = args_5[1];
V v_8 = args_5[2];
var values_9 = R.Join(new V[] {}, R.Call(v_8, R.Join(new V[] {}, new Results(v_6))));
V v_10 = values_9[0];
V v_11 = values_9[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(!R.Truth(v_10)), () => (V)(R.Get(v_10, (V)"style") == (V)"onebit"))}, new Results((V)"unsupported game artwork")));
return R.Join(new V[] {R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_12 => {

return R.Join(new V[] {}, new Results(V.Nil));
return Results.Empty;
})), v_7}, new Results(v_10))).First}, new Results(v_11));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
