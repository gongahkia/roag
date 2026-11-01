// Static C# port of src/onebit_shader.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 0ce1e1841b64928f98db98d9a8616eeed2236e6661602b59390bb96eb1566d7a
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_onebit_shader { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results((V)"\nextern vec3 ink;\nvec4 effect(vec4 color, Image texture, vec2 texture_coords, vec2 screen_coords)\n{\n    vec4 pixel = Texel(texture, texture_coords) * color;\n    float gray = dot(pixel.rgb, vec3(0.2126, 0.7152, 0.0722));\n    return vec4(vec3(gray) * ink, pixel.a);\n}\n"));
V v_6 = values_5[0];
R.Set(v_2, (V)"new", new V((RuleFunction)(args_7 => {

return R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"attempted", (V)false))}, new Results(R.Table(new Entry((V)"__index", v_2))))));
return Results.Empty;
})));
R.Set(v_2, "begin", new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
V v_10 = args_8[1];
V v_11 = args_8[2];
if (R.Truth((V)(!R.Truth(R.Get(v_9, (V)"attempted"))))) {
var values_12 = R.Join(new V[] {}, new Results((V)true));
var obj_13 = v_9;
V key_14 = (V)"attempted";
R.Set(obj_13, key_14, values_12[0]);
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(new V((RuleFunction)(args_15 => {

var values_16 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"newShader"), R.Join(new V[] {}, new Results(v_6))));
V v_17 = values_16[0];
R.Invoke(v_17, "send", R.Join(new V[] {(V)"ink"}, new Results(v_4)));
return R.Join(new V[] {}, new Results(v_17));
return Results.Empty;
}))))));
V v_19 = values_18[0];
V v_20 = values_18[1];
if (R.Truth(v_19)) {
var values_21 = R.Join(new V[] {}, new Results(v_20));
var obj_22 = v_9;
V key_23 = (V)"shader";
R.Set(obj_22, key_23, values_21[0]);

var values_24 = R.Join(new V[] {}, new Results(v_4));
var obj_25 = v_9;
V key_26 = (V)"ink";
R.Set(obj_25, key_26, values_24[0]);
} else if (R.Truth(v_11)) {
R.Call(v_11, R.Join(new V[] {}, new Results(v_20)));
}
}
if (R.Truth((V)(!R.Truth(R.Get(v_9, (V)"shader"))))) {
return R.Join(new V[] {V.Nil}, new Results((V)false));
}
var values_27 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"getShader"), Results.Empty));
V v_28 = values_27[0];
R.Call(R.Get(v_10, (V)"setShader"), R.Join(new V[] {}, new Results(R.Get(v_9, (V)"shader"))));
R.Invoke(v_9, "set_ink", R.Join(new V[] {}, new Results(v_10)));
return R.Join(new V[] {v_28}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, "set_ink", new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
V v_32 = args_29[2];
var values_33 = R.Join(new V[] {}, new Results(R.Or(v_32, () => v_4)));
v_32 = values_33[0];
var values_34 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_30, (V)"ink"), () => v_4)));
V v_35 = values_34[0];
var values_36 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_30, (V)"shader"), () => (V)(R.Call(R.Get(v_31, (V)"getShader"), Results.Empty).First == R.Get(v_30, (V)"shader"))), () => (V)false)));
V v_37 = values_36[0];
if (R.Truth(R.And(v_37, () => R.Or(R.Or(R.Or((V)(!R.Truth(R.Get(v_30, (V)"ink"))), () => (V)(R.Get(v_35, (V)1.0d) != R.Get(v_32, (V)1.0d))), () => (V)(R.Get(v_35, (V)2.0d) != R.Get(v_32, (V)2.0d))), () => (V)(R.Get(v_35, (V)3.0d) != R.Get(v_32, (V)3.0d)))))) {
R.Invoke(R.Get(v_30, (V)"shader"), "send", R.Join(new V[] {(V)"ink"}, new Results(v_32)));
}
var values_38 = R.Join(new V[] {}, new Results(v_32));
var obj_39 = v_30;
V key_40 = (V)"ink";
R.Set(obj_39, key_40, values_38[0]);
return R.Join(new V[] {v_35}, new Results(v_37));
return Results.Empty;
})));
R.Set(v_2, "finish", new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
V v_43 = args_41[1];
V v_44 = args_41[2];
V v_45 = args_41[3];
if (R.Truth(v_45)) {
R.Invoke(v_42, "set_ink", R.Join(new V[] {}, new Results(v_43)));

R.Call(R.Get(v_43, (V)"setShader"), R.Join(new V[] {}, new Results(v_44)));
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
