// Static C# port of src/studio_atlas_runtime.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 8beaa8ac166ce29ab2d0f3e09a796eb333a15c689d3099af9ec7c377c8af9b59
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_atlas_runtime { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_atlas"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_atlas_store"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table()));
V v_6 = values_5[0];
R.Set(v_6, (V)"new", new V((RuleFunction)(args_7 => {
V v_8 = args_7[0];
V v_9 = args_7[1];
V v_10 = args_7[2];
V v_11 = V.Nil;
v_11 = new V((RuleFunction)(args_12 => {
V v_13 = args_12[0];
V v_14 = args_12[1];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(new V((RuleFunction)(args_15 => {

return R.Join(new V[] {}, R.Call(R.Get(R.Get(rt.Global("love"), (V)"image"), (V)"newImageData"), R.Join(new V[] {}, R.Call(R.Get(R.Get(rt.Global("love"), (V)"filesystem"), (V)"newFileData"), R.Join(new V[] {v_13}, new Results(R.Get(v_14, (V)"filename")))))));
return Results.Empty;
}))))));
V v_17 = values_16[0];
V v_18 = values_16[1];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_17, () => v_18), () => (V)(R.Invoke(v_18, "getWidth", Results.Empty).First == R.Get(v_14, (V)"width"))), () => (V)(R.Invoke(v_18, "getHeight", Results.Empty).First == R.Get(v_14, (V)"height")))));
return Results.Empty;
}));
var values_19 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_4, (V)"inspect"), v_8, v_10}, new Results(v_11))));
V v_20 = values_19[0];
V v_21 = values_19[1];
V v_22 = values_19[2];
if (R.Truth(R.Or((V)(!R.Truth(v_20)), () => (V)(!R.Truth(v_21))))) {
return R.Join(new V[] {V.Nil}, new Results(R.Or(R.And(v_20, () => v_22), () => R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_21))).First)));
}
var values_23 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_24 = values_23[0];
V v_25 = values_23[1];
foreach (var row_26 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_21, (V)"pages"))))))) {
V v_27 = row_26[0];
V v_28 = row_26[1];
var values_29 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_28, (V)"filename"), R.Concat((V)":", R.Get(v_28, (V)"filter")))));
V v_30 = values_29[0];
var values_31 = R.Join(new V[] {}, new Results(R.Get(v_25, v_30)));
V v_32 = values_31[0];
if (R.Truth((V)(!R.Truth(v_32)))) {
var values_33 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_9, R.Join(new V[] {R.Call(v_8, R.Join(new V[] {}, new Results(R.Get(v_28, (V)"filename")))).First}, new Results(R.Table(new Entry((V)"source", R.Get(v_28, (V)"filename")), new Entry((V)"width", R.Get(v_28, (V)"width")), new Entry((V)"height", R.Get(v_28, (V)"height")), new Entry((V)"filter", R.Get(v_28, (V)"filter")))))))));
v_32 = values_33[0];
R.Invoke(v_32, "setFilter", R.Join(new V[] {R.Get(v_28, (V)"filter")}, new Results(R.Get(v_28, (V)"filter"))));
var values_34 = R.Join(new V[] {}, new Results(v_32));
var obj_35 = v_25;
V key_36 = v_30;
R.Set(obj_35, key_36, values_34[0]);
}
var values_37 = R.Join(new V[] {}, new Results(v_32));
var obj_38 = v_24;
V key_39 = v_27;
R.Set(obj_38, key_39, values_37[0]);
}
var values_40 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"pack", v_21), new Entry((V)"images", v_24), new Entry((V)"handles", R.Table()), new Entry((V)"status", v_22))}, new Results(R.Table(new Entry((V)"__index", v_6))))));
V v_41 = values_40[0];
return R.Join(new V[] {v_41}, new Results(v_22));
return Results.Empty;
})));
R.Set(v_6, "resolve", new V((RuleFunction)(args_42 => {
V v_43 = args_42[0];
V v_44 = args_42[1];
var values_45 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_43, (V)"pack"), (V)"assets"), v_44)));
V v_46 = values_45[0];
if (R.Truth((V)(!R.Truth(v_46)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_47 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_43, (V)"handles"), v_44)));
V v_48 = values_47[0];
if (R.Truth(v_48)) {
return R.Join(new V[] {}, new Results(v_48));
}
var values_49 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_43, (V)"pack"), (V)"pages"), R.Get(v_46, (V)"page"))));
V v_50 = values_49[0];
var values_51 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_43, (V)"images"), R.Get(v_46, (V)"page"))));
V v_52 = values_51[0];
var values_53 = R.Join(new V[] {}, R.Call(R.Get(R.Get(rt.Global("love"), (V)"graphics"), (V)"newQuad"), R.Join(new V[] {R.Get(v_46, (V)"x"), R.Get(v_46, (V)"y"), R.Get(v_46, (V)"w"), R.Get(v_46, (V)"h"), R.Get(v_50, (V)"width")}, new Results(R.Get(v_50, (V)"height")))));
V v_54 = values_53[0];
var values_55 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_52), new Entry((V)"quad", v_54), new Entry((V)"width", R.Get(v_46, (V)"w")), new Entry((V)"height", R.Get(v_46, (V)"h")), new Entry((V)"offset_x", R.Get(v_46, (V)"x")), new Entry((V)"offset_y", R.Get(v_46, (V)"y")), new Entry((V)"source", (V)"atlas"), new Entry((V)"page", R.Get(v_46, (V)"page")))));
v_48 = values_55[0];
var values_56 = R.Join(new V[] {}, new Results(v_48));
var obj_57 = R.Get(v_43, (V)"handles");
V key_58 = v_44;
R.Set(obj_57, key_58, values_56[0]);
return R.Join(new V[] {}, new Results(v_48));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
