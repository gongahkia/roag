// Static C# port of src/studio_world_runtime.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 1a90f6ec6356bca8c90dfdd163204aabdf18f85c8e53212af8fc8f183a5e7587
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_world_runtime { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_world"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"paths", R.Table(new Entry((V)1, (V)"assets/studio/world-a.pack"), new Entry((V)2, (V)"assets/studio/world-b.pack"))))));
V v_4 = values_3[0];
V v_5 = V.Nil;
v_5 = new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
var values_8 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
V v_9 = values_8[0];
V v_10 = values_8[1];
{ double end_13 = R.Num((V)R.Length(v_7)), step_14 = R.Num((V)1.0d);
for (double i_12 = R.Num((V)1.0d); step_14 > 0 ? i_12 <= end_13 : i_12 >= end_13; i_12 += step_14) {
V v_11 = i_12;
var values_15 = R.Join(new V[] {}, new Results((V)((V)(v_9 + R.Invoke(v_7, "byte", R.Join(new V[] {}, new Results(v_11))).First) % (V)65521.0d)));
v_9 = values_15[0];

var values_16 = R.Join(new V[] {}, new Results((V)((V)(v_10 + v_9) % (V)65521.0d)));
v_10 = values_16[0];
}
}
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"format"), R.Join(new V[] {(V)"%08x"}, new Results((V)((V)(v_10 * (V)65536.0d) + v_9)))));
return Results.Empty;
}));
R.Set(v_4, (V)"record", new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_18))).First == (V)"number"), () => (V)((V)(v_18 % (V)1.0d) == (V)0.0d)), () => (V)(v_18 >= (V)1.0d))}, new Results((V)"invalid pack sequence")));
var values_20 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"encode"), R.Join(new V[] {}, new Results(v_19))));
V v_21 = values_20[0];
return R.Join(new V[] {}, new Results(R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Table(new Entry((V)1, (V)"ROAG-STUDIO-PUBLISHED-WORLD/1"), new Entry((V)2, v_18), new Entry((V)3, (V)R.Length(v_21)), new Entry(4, R.Call(v_5, R.Join(new V[] {}, new Results(v_21)))))}, new Results((V)"\n"))).First, R.Concat((V)"\n", v_21))));
return Results.Empty;
})));
R.Set(v_4, (V)"header", new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_23))).First == (V)"string"), () => (V)((V)R.Length(v_23) <= (V)((V)((V)1024.0d * (V)1024.0d) + (V)100.0d)))}, new Results((V)"invalid published pack size")));
var values_24 = R.Join(new V[] {}, R.Invoke(v_23, "match", R.Join(new V[] {}, new Results((V)"^([^\n]+)\n([^\n]+)\n([^\n]+)\n([^\n]+)\n(.*)$"))));
V v_25 = values_24[0];
V v_26 = values_24[1];
V v_27 = values_24[2];
V v_28 = values_24[3];
V v_29 = values_24[4];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_25 == (V)"ROAG-STUDIO-PUBLISHED-WORLD/1")}, new Results((V)"incompatible published pack format")));
var values_30 = R.Join(new V[] {R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_26))).First}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_27))));
v_26 = values_30[0];
v_27 = values_30[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_26, () => (V)((V)(v_26 % (V)1.0d) == (V)0.0d)), () => (V)(v_26 >= (V)1.0d)), () => v_27), () => (V)((V)(v_27 % (V)1.0d) == (V)0.0d)), () => (V)(v_27 >= (V)0.0d)), () => (V)(v_27 <= (V)((V)1024.0d * (V)1024.0d))), () => (V)((V)R.Length(v_29) == v_27)), () => (V)(v_28 == R.Call(v_5, R.Join(new V[] {}, new Results(v_29))).First))}, new Results((V)"corrupt published pack")));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"seq", v_26), new Entry((V)"payload", v_29))));
return Results.Empty;
})));
R.Set(v_4, (V)"inspect", new V((RuleFunction)(args_31 => {
V v_32 = args_31[0];
var values_33 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_34 = values_33[0];
V v_35 = values_33[1];
foreach (var row_36 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"paths"))))))) {
V v_37 = row_36[0];
V v_38 = row_36[1];
var values_39 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_32}, new Results(v_38))));
V v_40 = values_39[0];
V v_41 = values_39[1];
if (R.Truth(R.And(v_40, () => v_41))) {
var values_42 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_4, (V)"header")}, new Results(v_41))));
V v_43 = values_42[0];
V v_44 = values_42[1];
if (R.Truth(v_43)) {
var values_45 = R.Join(new V[] {}, new Results(v_37));
var obj_46 = v_44;
V key_47 = (V)"slot";
R.Set(obj_46, key_47, values_45[0]);

var values_48 = R.Join(new V[] {}, new Results(v_44));
var obj_49 = v_34;
V key_50 = (V)((V)R.Length(v_34) + (V)1.0d);
R.Set(obj_49, key_50, values_48[0]);
} else {
var values_51 = R.Join(new V[] {}, new Results((V)(v_35 + (V)1.0d)));
v_35 = values_51[0];
}
} else if (R.Truth((V)(!R.Truth(v_40)))) {
var values_52 = R.Join(new V[] {}, new Results((V)(v_35 + (V)1.0d)));
v_35 = values_52[0];
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_34}, new Results(new V((RuleFunction)(args_53 => {
V v_54 = args_53[0];
V v_55 = args_53[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_54, (V)"seq") > R.Get(v_55, (V)"seq"))));
return Results.Empty;
})))));
if (R.Truth((V)((V)R.Length(v_34) == (V)0.0d))) {
return R.Join(new V[] {V.Nil}, new Results(R.Or(R.And((V)(v_35 > (V)0.0d), () => (V)"corrupt published world"), () => (V)"absent")));
}
var values_56 = R.Join(new V[] {}, new Results(R.Get(v_34, (V)1.0d)));
V v_57 = values_56[0];
var values_58 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"decode")}, new Results(R.Get(v_57, (V)"payload")))));
V v_59 = values_58[0];
V v_60 = values_58[1];
if (R.Truth((V)(!R.Truth(v_59)))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"incompatible or invalid published world: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_60))).First)}, new Results(v_57));
}
return R.Join(new V[] {v_60, R.Or(R.And((V)(v_35 > (V)0.0d), () => (V)"recovered previous published world"), () => (V)"loaded")}, new Results(v_57));
return Results.Empty;
})));
R.Set(v_4, (V)"plan", new V((RuleFunction)(args_61 => {
V v_62 = args_61[0];
V v_63 = args_61[1];
R.Call(R.Get(v_2, (V)"validate"), R.Join(new V[] {}, new Results(v_62)));
var values_64 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"inspect"), R.Join(new V[] {}, new Results(v_63))));
V v_65 = values_64[0];
V v_66 = values_64[1];
V v_67 = values_64[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or(v_65, () => (V)(v_66 == (V)"absent")), () => (V)(v_66 == (V)"corrupt published world"))}, new Results(R.Concat((V)"refusing to overwrite incompatible published world: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_66))).First))));
var values_68 = R.Join(new V[] {}, new Results(R.Or(R.And(v_67, () => R.Or(R.And((V)(R.Get(v_67, (V)"slot") == (V)1.0d), () => (V)2.0d), () => (V)1.0d)), () => (V)1.0d)));
V v_69 = values_68[0];
var values_70 = R.Join(new V[] {}, new Results(R.Or(R.And(v_67, () => (V)(R.Get(v_67, (V)"seq") + (V)1.0d)), () => (V)1.0d)));
V v_71 = values_70[0];
var values_72 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_4, (V)"paths"), v_69)));
V v_73 = values_72[0];
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"record"), R.Join(new V[] {v_71}, new Results(v_62))));
V v_75 = values_74[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", v_73), new Entry((V)"bytes", v_75), new Entry((V)"seq", v_71), new Entry((V)"status", v_66), new Entry((V)"overwrite", (V)(R.Call(v_63, R.Join(new V[] {}, new Results(v_73))).First != V.Nil)))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
