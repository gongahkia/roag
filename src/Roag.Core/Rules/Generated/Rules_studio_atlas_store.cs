// Static C# port of src/studio_atlas_store.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 4cddd4c5ba988e5eb123680d1d88bcdca873d5f0af85af1d85f0126f656a2129
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_atlas_store { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_atlas"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"format", (V)"ROAG-STUDIO-ATLAS-RECORD/1"), new Entry((V)"disable_path", (V)"assets/studio/atlas-off.flag"))));
V v_4 = values_3[0];
V v_5 = V.Nil;
v_5 = new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_7))).First == (V)"string"), () => (V)((V)R.Length(v_7) <= (V)((V)((V)1024.0d * (V)1024.0d) + (V)200.0d)))}, new Results((V)"invalid atlas record size")));
var values_8 = R.Join(new V[] {}, R.Invoke(v_7, "match", R.Join(new V[] {}, new Results((V)"^([^\n]+)\n([^\n]+)\n([^\n]+)\n([^\n]+)\n(.*)$"))));
V v_9 = values_8[0];
V v_10 = values_8[1];
V v_11 = values_8[2];
V v_12 = values_8[3];
V v_13 = values_8[4];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_9 == R.Get(v_4, (V)"format"))}, new Results((V)"incompatible atlas record")));
var values_14 = R.Join(new V[] {R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_10))).First}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_11))));
v_10 = values_14[0];
v_11 = values_14[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_10, () => (V)((V)(v_10 % (V)1.0d) == (V)0.0d)), () => (V)(v_10 >= (V)1.0d)), () => v_11), () => (V)((V)(v_11 % (V)1.0d) == (V)0.0d)), () => (V)(v_11 >= (V)0.0d)), () => (V)(v_11 <= (V)((V)1024.0d * (V)1024.0d))), () => (V)((V)R.Length(v_13) == v_11)), () => (V)(v_12 == R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, new Results(v_13))).First))}, new Results((V)"corrupt atlas record")));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"seq", v_10), new Entry((V)"payload", v_13))));
return Results.Empty;
}));
R.Set(v_4, (V)"record", new V((RuleFunction)(args_15 => {
V v_16 = args_15[0];
V v_17 = args_15[1];
var values_18 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"encode"), R.Join(new V[] {}, new Results(v_17))));
V v_19 = values_18[0];
return R.Join(new V[] {}, new Results(R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Table(new Entry((V)1, R.Get(v_4, (V)"format")), new Entry((V)2, v_16), new Entry((V)3, (V)R.Length(v_19)), new Entry(4, R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, new Results(v_19)))))}, new Results((V)"\n"))).First, R.Concat((V)"\n", v_19))));
return Results.Empty;
})));
V v_20 = V.Nil;
v_20 = new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
var values_23 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_24 = values_23[0];
V v_25 = values_23[1];
foreach (var row_26 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"slots"))))))) {
V v_27 = row_26[0];
V v_28 = row_26[1];
var values_29 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_22}, new Results(v_28))));
V v_30 = values_29[0];
V v_31 = values_29[1];
if (R.Truth(R.And(v_30, () => v_31))) {
var values_32 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_5}, new Results(v_31))));
V v_33 = values_32[0];
V v_34 = values_32[1];
if (R.Truth(v_33)) {
var values_35 = R.Join(new V[] {}, new Results(v_27));
var obj_36 = v_34;
V key_37 = (V)"slot";
R.Set(obj_36, key_37, values_35[0]);

var values_38 = R.Join(new V[] {}, new Results(v_34));
var obj_39 = v_24;
V key_40 = (V)((V)R.Length(v_24) + (V)1.0d);
R.Set(obj_39, key_40, values_38[0]);
} else {
var values_41 = R.Join(new V[] {}, new Results((V)(v_25 + (V)1.0d)));
v_25 = values_41[0];
}
} else if (R.Truth((V)(!R.Truth(v_30)))) {
var values_42 = R.Join(new V[] {}, new Results((V)(v_25 + (V)1.0d)));
v_25 = values_42[0];
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_24}, new Results(new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_44, (V)"seq") > R.Get(v_45, (V)"seq"))));
return Results.Empty;
})))));
return R.Join(new V[] {v_24}, new Results(v_25));
return Results.Empty;
}));
R.Set(v_4, (V)"inspect", new V((RuleFunction)(args_46 => {
V v_47 = args_46[0];
V v_48 = args_46[1];
V v_49 = args_46[2];
var values_50 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(R.Get(v_4, (V)"disable_path")))));
V v_51 = values_50[0];
if (R.Truth(v_51)) {
return R.Join(new V[] {V.Nil}, new Results((V)"disabled"));
}
var values_52 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results(v_47))));
V v_53 = values_52[0];
V v_54 = values_52[1];
if (R.Truth((V)((V)R.Length(v_53) == (V)0.0d))) {
return R.Join(new V[] {V.Nil}, new Results(R.Or(R.And((V)(v_54 > (V)0.0d), () => (V)"corrupt atlas records"), () => (V)"absent")));
}
var values_55 = Results.Empty;
V v_56 = values_55[0];
foreach (var row_57 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_53)))))) {
V v_58 = row_57[0];
V v_59 = row_57[1];
var values_60 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"decode")}, new Results(R.Get(v_59, (V)"payload")))));
V v_61 = values_60[0];
V v_62 = values_60[1];
var values_63 = R.Join(new V[] {(V)false}, new Results(V.Nil));
V v_64 = values_63[0];
V v_65 = values_63[1];
if (R.Truth(v_61)) {
var values_66 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"matches_inputs"), v_62, v_48}, new Results(v_47))));
V v_67 = values_66[0];
V v_68 = values_66[1];
V v_69 = values_66[2];
var values_70 = R.Join(new V[] {}, new Results(R.And(v_67, () => v_68)));
v_64 = values_70[0];
var values_71 = R.Join(new V[] {}, new Results(R.Or(R.And(v_67, () => v_69), () => R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_68))).First)));
v_65 = values_71[0];
}
if (R.Truth(R.And(v_61, () => v_64))) {
var values_72 = R.Join(new V[] {}, new Results((V)true));
V v_73 = values_72[0];
foreach (var row_74 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_62, (V)"pages"))))))) {
V v_75 = row_74[0];
V v_76 = row_74[1];
var values_77 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(R.Get(v_76, (V)"filename")))));
V v_78 = values_77[0];
if (R.Truth(R.Or(R.Or(R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_78))).First != (V)"string"), () => (V)((V)R.Length(v_78) > (V)((V)((V)32.0d * (V)1024.0d) * (V)1024.0d))), () => (V)(R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, new Results(v_78))).First != R.Get(v_76, (V)"checksum"))), () => R.And(v_49, () => (V)(!R.Truth(R.Call(v_49, R.Join(new V[] {v_78}, new Results(v_76))).First)))))) {
var values_79 = R.Join(new V[] {}, new Results((V)false));
v_73 = values_79[0];

var values_80 = R.Join(new V[] {}, new Results((V)"missing or corrupt atlas page"));
v_56 = values_80[0];

break;
}
}
if (R.Truth(v_73)) {
return R.Join(new V[] {v_62, R.Or(R.And(R.And((V)(v_59 == R.Get(v_53, (V)1.0d)), () => (V)(v_54 == (V)0.0d)), () => (V)"loaded"), () => (V)"recovered previous atlas generation")}, new Results(v_59));
}
} else {
var values_81 = R.Join(new V[] {}, new Results(R.Or(R.And(v_61, () => R.Concat((V)"stale atlas for current loose images: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_65))).First)), () => (V)"incompatible atlas metadata")));
v_56 = values_81[0];
}
}
return R.Join(new V[] {V.Nil}, new Results(R.Or(v_56, () => (V)"no complete atlas generation")));
return Results.Empty;
})));
R.Set(v_4, (V)"plan", new V((RuleFunction)(args_82 => {
V v_83 = args_82[0];
V v_84 = args_82[1];
R.Call(R.Get(v_2, (V)"validate"), R.Join(new V[] {}, new Results(v_83)));
var values_85 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results(v_84))));
V v_86 = values_85[0];
var values_87 = R.Join(new V[] {}, new Results(R.Get(v_86, (V)1.0d)));
V v_88 = values_87[0];
var values_89 = R.Join(new V[] {}, new Results(R.Or(R.And(v_88, () => R.Or(R.And((V)(R.Get(v_88, (V)"slot") == (V)1.0d), () => (V)2.0d), () => (V)1.0d)), () => (V)1.0d)));
V v_90 = values_89[0];
var values_91 = R.Join(new V[] {}, new Results(R.Or(R.And(v_88, () => (V)(R.Get(v_88, (V)"seq") + (V)1.0d)), () => (V)1.0d)));
V v_92 = values_91[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", R.Get(R.Get(v_2, (V)"slots"), v_90)), new Entry((V)"bytes", R.Call(R.Get(v_4, (V)"record"), R.Join(new V[] {v_92}, new Results(v_83))).First), new Entry((V)"seq", v_92), new Entry((V)"occupied", (V)(R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"slots"), v_90)))).First != V.Nil)))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
