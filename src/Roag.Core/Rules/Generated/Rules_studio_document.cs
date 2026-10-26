// Static C# port of src/studio_document.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: b4c9af5a6d798f1c1a3a00e2b69e042bc93fc7e41272e51cce7a7543c4dfcbc2
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_document { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"max_dimension", (V)512.0d), new Entry((V)"max_layers", (V)16.0d), new Entry((V)"max_bytes", (V)((V)((V)16.0d * (V)1024.0d) * (V)1024.0d)))));
V v_2 = values_1[0];
V v_3 = V.Nil;
v_3 = new V((RuleFunction)(args_4 => {
V v_5 = args_4[0];
return R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_project"))).First, (V)"valid_id"), R.Join(new V[] {}, new Results(v_5))));
return Results.Empty;
}));
V v_6 = V.Nil;
v_6 = new V((RuleFunction)(args_7 => {
V v_8 = args_7[0];
V v_9 = args_7[1];
if (R.Truth((V)(!R.Truth(v_8)))) {
R.Call(rt.Global("error"), R.Join(new V[] {v_9}, new Results((V)2.0d)));
}
return Results.Empty;
}));
V v_10 = V.Nil;
v_10 = new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"char"), R.Join(new V[] {(V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_12 / (V)16777216.0d)))).First % (V)256.0d), (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_12 / (V)65536.0d)))).First % (V)256.0d), (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_12 / (V)256.0d)))).First % (V)256.0d)}, new Results((V)(v_12 % (V)256.0d)))));
return Results.Empty;
}));
V v_13 = V.Nil;
v_13 = new V((RuleFunction)(args_14 => {
V v_15 = args_14[0];
V v_16 = args_14[1];
var values_17 = R.Join(new V[] {}, R.Invoke(v_15, "byte", R.Join(new V[] {v_16}, new Results((V)(v_16 + (V)3.0d)))));
V v_18 = values_17[0];
V v_19 = values_17[1];
V v_20 = values_17[2];
V v_21 = values_17[3];

R.Call(v_6, R.Join(new V[] {v_21}, new Results((V)"truncated document")));

return R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)((V)(v_18 * (V)256.0d) + v_19) * (V)256.0d) + v_20) * (V)256.0d) + v_21)));
return Results.Empty;
}));
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
R.Call(v_6, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_24))).First == (V)"table"), () => (V)((V)R.Length(v_24) == (V)4.0d))}, new Results((V)"invalid RGBA")));

{ double end_27 = R.Num((V)4.0d), step_28 = R.Num((V)1.0d);
for (double i_26 = R.Num((V)1.0d); step_28 > 0 ? i_26 <= end_27 : i_26 >= end_27; i_26 += step_28) {
V v_25 = i_26;
R.Call(v_6, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_24, v_25)))).First == (V)"number"), () => (V)((V)(R.Get(v_24, v_25) % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_24, v_25) >= (V)0.0d)), () => (V)(R.Get(v_24, v_25) <= (V)255.0d))}, new Results((V)"invalid channel")));
}
}
return Results.Empty;
}));
V v_29 = V.Nil;
v_29 = new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
V v_32 = args_30[1];
var values_33 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"\u0000"}, new Results((V)(v_31 * (V)4.0d)))));
V v_34 = values_33[0];

var values_35 = R.Join(new V[] {}, new Results(R.Table()));
V v_36 = values_35[0];

{ double end_39 = R.Num(v_32), step_40 = R.Num((V)1.0d);
for (double i_38 = R.Num((V)1.0d); step_40 > 0 ? i_38 <= end_39 : i_38 >= end_39; i_38 += step_40) {
V v_37 = i_38;
var values_41 = R.Join(new V[] {}, new Results(v_34));
var obj_42 = v_36;
V key_43 = v_37;
R.Set(obj_42, key_43, values_41[0]);
}
}

return R.Join(new V[] {}, new Results(v_36));
return Results.Empty;
}));
V v_44 = V.Nil;
v_44 = new V((RuleFunction)(args_45 => {
V v_46 = args_45[0];
V v_47 = args_45[1];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_46))).First == (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_47))).First == (V)"number")), () => (V)((V)(v_46 % (V)1.0d) == (V)0.0d)), () => (V)((V)(v_47 % (V)1.0d) == (V)0.0d)), () => (V)(v_46 >= (V)1.0d)), () => (V)(v_47 >= (V)1.0d)), () => (V)(v_46 <= R.Get(v_2, (V)"max_dimension"))), () => (V)(v_47 <= R.Get(v_2, (V)"max_dimension")))));
return Results.Empty;
}));
R.Set(v_2, (V)"new", new V((RuleFunction)(args_48 => {
V v_49 = args_48[0];
V v_50 = args_48[1];
V v_51 = args_48[2];
R.Call(v_6, R.Join(new V[] {R.Call(v_3, R.Join(new V[] {}, new Results(v_49))).First}, new Results((V)"invalid document asset ID")));

R.Call(v_6, R.Join(new V[] {R.Call(v_44, R.Join(new V[] {v_50}, new Results(v_51))).First}, new Results((V)"invalid canvas dimensions")));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_49), new Entry((V)"width", v_50), new Entry((V)"height", v_51), new Entry((V)"next_layer", (V)2.0d), new Entry((V)"selected", (V)"layer_1"), new Entry((V)"layers", R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)"layer_1"), new Entry((V)"name", (V)"Layer 1"), new Entry((V)"visible", (V)true), new Entry((V)"locked", (V)false), new Entry((V)"rows", R.Call(v_29, R.Join(new V[] {v_50}, new Results(v_51))).First))))))));
return Results.Empty;
})));
V v_52 = V.Nil;
v_52 = new V((RuleFunction)(args_53 => {
V v_54 = args_53[0];
V v_55 = args_53[1];
foreach (var row_56 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_54, (V)"layers"))))))) {
V v_57 = row_56[0];
V v_58 = row_56[1];
if (R.Truth((V)(R.Get(v_58, (V)"id") == v_55))) {
return R.Join(new V[] {v_58}, new Results(v_57));
}
}
return Results.Empty;
}));
R.Set(v_2, (V)"active", new V((RuleFunction)(args_59 => {
V v_60 = args_59[0];
return R.Join(new V[] {}, R.Call(v_52, R.Join(new V[] {v_60}, new Results(R.Get(v_60, (V)"selected")))));
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_61 => {
V v_62 = args_61[0];
R.Call(v_6, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_62))).First == (V)"table"), () => R.Call(v_3, R.Join(new V[] {}, new Results(R.Get(v_62, (V)"id")))).First)}, new Results((V)"invalid document ID")));
R.Call(v_6, R.Join(new V[] {R.Call(v_44, R.Join(new V[] {R.Get(v_62, (V)"width")}, new Results(R.Get(v_62, (V)"height")))).First}, new Results((V)"invalid canvas dimensions")));
R.Call(v_6, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_62, (V)"layers")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_62, (V)"layers")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_62, (V)"layers")) <= R.Get(v_2, (V)"max_layers")))}, new Results((V)"invalid layer count")));
R.Call(v_6, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_62, (V)"next_layer")))).First == (V)"number"), () => (V)((V)(R.Get(v_62, (V)"next_layer") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_62, (V)"next_layer") >= (V)2.0d)), () => (V)(R.Get(v_62, (V)"next_layer") <= (V)100000.0d))}, new Results((V)"invalid layer sequence")));
var values_63 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_64 = values_63[0];
V v_65 = values_63[1];
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_62, (V)"layers"))))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
R.Call(v_6, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_68, (V)"id")))).First == (V)"string"), () => R.Invoke(R.Get(v_68, (V)"id"), "match", R.Join(new V[] {}, new Results((V)"^layer_%d+$"))).First), () => (V)(!R.Truth(R.Get(v_64, R.Get(v_68, (V)"id")))))}, new Results((V)"invalid layer ID")));

var values_69 = R.Join(new V[] {}, new Results((V)true));
var obj_70 = v_64;
V key_71 = R.Get(v_68, (V)"id");
R.Set(obj_70, key_71, values_69[0]);
var values_72 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, R.Invoke(R.Get(v_68, (V)"id"), "match", R.Join(new V[] {}, new Results((V)"^layer_(%d+)$"))))));
V v_73 = values_72[0];
R.Call(v_6, R.Join(new V[] {R.And(R.And(v_73, () => (V)(v_73 >= (V)1.0d)), () => (V)(v_73 < (V)100000.0d))}, new Results((V)"invalid layer sequence")));
var values_74 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_65}, new Results(v_73))));
v_65 = values_74[0];
R.Call(v_6, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_68, (V)"name")))).First == (V)"string"), () => (V)((V)R.Length(R.Get(v_68, (V)"name")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_68, (V)"name")) <= (V)64.0d)), () => (V)(!R.Truth(R.Invoke(R.Get(v_68, (V)"name"), "find", R.Join(new V[] {}, new Results((V)"%z"))).First)))}, new Results((V)"invalid layer name")));
R.Call(v_6, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_68, (V)"visible")))).First == (V)"boolean"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_68, (V)"locked")))).First == (V)"boolean"))}, new Results((V)"invalid layer flags")));
R.Call(v_6, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_68, (V)"rows")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_68, (V)"rows")) == R.Get(v_62, (V)"height")))}, new Results((V)"invalid raster rows")));
foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_68, (V)"rows"))))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
R.Call(v_6, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_77))).First == (V)"string"), () => (V)((V)R.Length(v_77) == (V)(R.Get(v_62, (V)"width") * (V)4.0d)))}, new Results((V)"invalid raster length")));
}
}
R.Call(v_6, R.Join(new V[] {(V)(R.Get(v_62, (V)"next_layer") > v_65)}, new Results((V)"layer sequence would reuse an ID")));
R.Call(v_6, R.Join(new V[] {R.Get(v_64, R.Get(v_62, (V)"selected"))}, new Results((V)"invalid selected layer")));
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, (V)"from_rgba", new V((RuleFunction)(args_78 => {
V v_79 = args_78[0];
V v_80 = args_78[1];
V v_81 = args_78[2];
V v_82 = args_78[3];
R.Call(v_6, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_82))).First == (V)"string"), () => R.Call(v_44, R.Join(new V[] {v_80}, new Results(v_81))).First), () => (V)((V)R.Length(v_82) == (V)((V)(v_80 * v_81) * (V)4.0d)))}, new Results((V)"invalid imported RGBA")));
var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_79, v_80}, new Results(v_81))));
V v_84 = values_83[0];

var values_85 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_84, (V)"layers"), (V)1.0d), (V)"rows")));
V v_86 = values_85[0];
{ double end_89 = R.Num(v_81), step_90 = R.Num((V)1.0d);
for (double i_88 = R.Num((V)1.0d); step_90 > 0 ? i_88 <= end_89 : i_88 >= end_89; i_88 += step_90) {
V v_87 = i_88;
var values_91 = R.Join(new V[] {}, R.Invoke(v_82, "sub", R.Join(new V[] {(V)((V)((V)((V)(v_87 - (V)1.0d) * v_80) * (V)4.0d) + (V)1.0d)}, new Results((V)((V)(v_87 * v_80) * (V)4.0d)))));
var obj_92 = v_86;
V key_93 = v_87;
R.Set(obj_92, key_93, values_91[0]);
}
}
return R.Join(new V[] {}, new Results(v_84));
return Results.Empty;
})));
V v_94 = V.Nil;
v_94 = new V((RuleFunction)(args_95 => {
V v_96 = args_95[0];
V v_97 = args_95[1];
var values_98 = R.Join(new V[] {}, new Results((V)((V)(v_97 * (V)4.0d) + (V)1.0d)));
V v_99 = values_98[0];

return R.Join(new V[] {}, new Results(R.Table(new Entry(1, R.Invoke(v_96, "byte", R.Join(new V[] {v_99}, new Results((V)(v_99 + (V)3.0d))))))));
return Results.Empty;
}));
R.Set(v_2, (V)"get", new V((RuleFunction)(args_100 => {
V v_101 = args_100[0];
V v_102 = args_100[1];
V v_103 = args_100[2];
V v_104 = args_100[3];
if (R.Truth(R.Or(R.Or(R.Or((V)(v_102 < (V)0.0d), () => (V)(v_103 < (V)0.0d)), () => (V)(v_102 >= R.Get(v_101, (V)"width"))), () => (V)(v_103 >= R.Get(v_101, (V)"height"))))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_105 = R.Join(new V[] {}, R.Call(v_52, R.Join(new V[] {v_101}, new Results(R.Or(v_104, () => R.Get(v_101, (V)"selected"))))));
V v_106 = values_105[0];

return R.Join(new V[] {}, new Results(R.Or(R.And(v_106, () => R.Call(v_94, R.Join(new V[] {R.Get(R.Get(v_106, (V)"rows"), (V)(v_103 + (V)1.0d))}, new Results(v_102))).First), () => V.Nil)));
return Results.Empty;
})));
V v_107 = V.Nil;
v_107 = new V((RuleFunction)(args_108 => {
V v_109 = args_108[0];
V v_110 = args_108[1];
var values_111 = R.Join(new V[] {R.Get(v_110, (V)4.0d)}, new Results(R.Get(v_109, (V)4.0d)));
V v_112 = values_111[0];
V v_113 = values_111[1];
if (R.Truth((V)(v_112 == (V)0.0d))) {
return R.Join(new V[] {}, new Results(v_109));
}
if (R.Truth((V)(v_112 == (V)255.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_110, (V)1.0d)), new Entry((V)2, R.Get(v_110, (V)2.0d)), new Entry((V)3, R.Get(v_110, (V)3.0d)), new Entry((V)4, (V)255.0d))));
}
var values_114 = R.Join(new V[] {}, new Results((V)(v_112 + R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)(v_113 * (V)((V)255.0d - v_112)) + (V)127.0d) / (V)255.0d)))).First)));
V v_115 = values_114[0];
if (R.Truth((V)(v_115 == (V)0.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)0.0d), new Entry((V)4, (V)0.0d))));
}
var values_116 = R.Join(new V[] {}, new Results(R.Table()));
V v_117 = values_116[0];
{ double end_120 = R.Num((V)3.0d), step_121 = R.Num((V)1.0d);
for (double i_119 = R.Num((V)1.0d); step_121 > 0 ? i_119 <= end_120 : i_119 >= end_120; i_119 += step_121) {
V v_118 = i_119;
var values_122 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)(R.Get(v_110, v_118) * v_112) * (V)255.0d) + (V)((V)(R.Get(v_109, v_118) * v_113) * (V)((V)255.0d - v_112))) / (V)(v_115 * (V)255.0d)) + (V)0.5d)))));
var obj_123 = v_117;
V key_124 = v_118;
R.Set(obj_123, key_124, values_122[0]);
}
}
var values_125 = R.Join(new V[] {}, new Results(v_115));
var obj_126 = v_117;
V key_127 = (V)4.0d;
R.Set(obj_126, key_127, values_125[0]);

return R.Join(new V[] {}, new Results(v_117));
return Results.Empty;
}));
R.Set(v_2, (V)"composite_pixel", new V((RuleFunction)(args_128 => {
V v_129 = args_128[0];
V v_130 = args_128[1];
V v_131 = args_128[2];
var values_132 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)0.0d), new Entry((V)4, (V)0.0d))));
V v_133 = values_132[0];
foreach (var row_134 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_129, (V)"layers"))))))) {
V v_135 = row_134[0];
V v_136 = row_134[1];
if (R.Truth(R.Get(v_136, (V)"visible"))) {
var values_137 = R.Join(new V[] {}, R.Call(v_107, R.Join(new V[] {v_133}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {v_129, v_130, v_131}, new Results(R.Get(v_136, (V)"id")))))));
v_133 = values_137[0];
}
}
return R.Join(new V[] {}, new Results(v_133));
return Results.Empty;
})));
R.Set(v_2, (V)"flatten", new V((RuleFunction)(args_138 => {
V v_139 = args_138[0];
R.Call(R.Get(v_2, (V)"validate"), R.Join(new V[] {}, new Results(v_139)));
var values_140 = R.Join(new V[] {}, new Results(R.Table()));
V v_141 = values_140[0];
foreach (var row_142 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_139, (V)"layers"))))))) {
V v_143 = row_142[0];
V v_144 = row_142[1];
if (R.Truth(R.Get(v_144, (V)"visible"))) {
var values_145 = R.Join(new V[] {}, new Results(v_144));
var obj_146 = v_141;
V key_147 = (V)((V)R.Length(v_141) + (V)1.0d);
R.Set(obj_146, key_147, values_145[0]);
}
}
if (R.Truth((V)((V)R.Length(v_141) == (V)0.0d))) {
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"\u0000"}, new Results((V)((V)(R.Get(v_139, (V)"width") * R.Get(v_139, (V)"height")) * (V)4.0d)))));
}
if (R.Truth((V)((V)R.Length(v_141) == (V)1.0d))) {
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_141, (V)1.0d), (V)"rows")))));
}
var values_148 = R.Join(new V[] {}, new Results(R.Table()));
V v_149 = values_148[0];
{ double end_152 = R.Num(R.Get(v_139, (V)"height")), step_153 = R.Num((V)1.0d);
for (double i_151 = R.Num((V)1.0d); step_153 > 0 ? i_151 <= end_152 : i_151 >= end_152; i_151 += step_153) {
V v_150 = i_151;
var values_154 = R.Join(new V[] {}, new Results(R.Table()));
V v_155 = values_154[0];

foreach (var row_156 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_141)))))) {
V v_157 = row_156[0];
V v_158 = row_156[1];
var values_159 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_158, (V)"rows"), v_150)));
var obj_160 = v_155;
V key_161 = v_157;
R.Set(obj_160, key_161, values_159[0]);
}
var values_162 = R.Join(new V[] {}, new Results(R.Table()));
V v_163 = values_162[0];
{ double end_166 = R.Num((V)(R.Get(v_139, (V)"width") - (V)1.0d)), step_167 = R.Num((V)1.0d);
for (double i_165 = R.Num((V)0.0d); step_167 > 0 ? i_165 <= end_166 : i_165 >= end_166; i_165 += step_167) {
V v_164 = i_165;
var values_168 = R.Join(new V[] {}, new Results((V)((V)(v_164 * (V)4.0d) + (V)1.0d)));
V v_169 = values_168[0];

var values_170 = R.Join(new V[] {(V)0.0d, (V)0.0d, (V)0.0d}, new Results((V)0.0d));
V v_171 = values_170[0];
V v_172 = values_170[1];
V v_173 = values_170[2];
V v_174 = values_170[3];
{ double end_177 = R.Num((V)R.Length(v_155)), step_178 = R.Num((V)1.0d);
for (double i_176 = R.Num((V)1.0d); step_178 > 0 ? i_176 <= end_177 : i_176 >= end_177; i_176 += step_178) {
V v_175 = i_176;
var values_179 = R.Join(new V[] {}, R.Invoke(R.Get(v_155, v_175), "byte", R.Join(new V[] {v_169}, new Results((V)(v_169 + (V)3.0d)))));
V v_180 = values_179[0];
V v_181 = values_179[1];
V v_182 = values_179[2];
V v_183 = values_179[3];
if (R.Truth((V)(v_183 == (V)255.0d))) {
var values_184 = R.Join(new V[] {v_180, v_181, v_182}, new Results((V)255.0d));
v_171 = values_184[0];
v_172 = values_184[1];
v_173 = values_184[2];
v_174 = values_184[3];
} else if (R.Truth((V)(v_183 > (V)0.0d))) {
var values_185 = R.Join(new V[] {}, new Results((V)(v_183 + R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)(v_174 * (V)((V)255.0d - v_183)) + (V)127.0d) / (V)255.0d)))).First)));
V v_186 = values_185[0];
var values_187 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)(v_180 * v_183) * (V)255.0d) + (V)((V)(v_171 * v_174) * (V)((V)255.0d - v_183))) / (V)(v_186 * (V)255.0d)) + (V)0.5d)))));
v_171 = values_187[0];
var values_188 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)(v_181 * v_183) * (V)255.0d) + (V)((V)(v_172 * v_174) * (V)((V)255.0d - v_183))) / (V)(v_186 * (V)255.0d)) + (V)0.5d)))));
v_172 = values_188[0];
var values_189 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)(v_182 * v_183) * (V)255.0d) + (V)((V)(v_173 * v_174) * (V)((V)255.0d - v_183))) / (V)(v_186 * (V)255.0d)) + (V)0.5d)))));
v_173 = values_189[0];
var values_190 = R.Join(new V[] {}, new Results(v_186));
v_174 = values_190[0];
}
}
}
var values_191 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"char"), R.Join(new V[] {v_171, v_172, v_173}, new Results(v_174))));
var obj_192 = v_163;
V key_193 = (V)((V)R.Length(v_163) + (V)1.0d);
R.Set(obj_192, key_193, values_191[0]);
}
}
var values_194 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(v_163))));
var obj_195 = v_149;
V key_196 = v_150;
R.Set(obj_195, key_196, values_194[0]);
}
}
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(v_149))));
return Results.Empty;
})));
R.Set(v_2, (V)"set", new V((RuleFunction)(args_197 => {
V v_198 = args_197[0];
V v_199 = args_197[1];
V v_200 = args_197[2];
V v_201 = args_197[3];
V v_202 = args_197[4];
var values_203 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"active"), R.Join(new V[] {}, new Results(v_198))));
V v_204 = values_203[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_204)), () => R.Get(v_204, (V)"locked")), () => (V)(!R.Truth(R.Get(v_204, (V)"visible")))))) {
return R.Join(new V[] {(V)false}, new Results((V)"layer is locked or hidden"));
}
if (R.Truth(R.Or(R.Or(R.Or((V)(v_199 < (V)0.0d), () => (V)(v_200 < (V)0.0d)), () => (V)(v_199 >= R.Get(v_198, (V)"width"))), () => (V)(v_200 >= R.Get(v_198, (V)"height"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
R.Call(v_22, R.Join(new V[] {}, new Results(v_201)));
var values_205 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_204, (V)"rows"), (V)(v_200 + (V)1.0d))));
V v_206 = values_205[0];

var values_207 = R.Join(new V[] {}, R.Call(v_94, R.Join(new V[] {v_206}, new Results(v_199))));
V v_208 = values_207[0];
var values_209 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_202 == (V)"erase"), () => R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)0.0d), new Entry((V)4, (V)0.0d))), () => R.And((V)(v_202 == (V)"replace"), () => v_201)), () => R.Call(v_107, R.Join(new V[] {v_208}, new Results(v_201))).First)));
V v_210 = values_209[0];
var values_211 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"char"), R.Join(new V[] {}, R.Call(rt.Global("unpack"), R.Join(new V[] {}, new Results(v_210))))));
V v_212 = values_211[0];

var values_213 = R.Join(new V[] {}, new Results((V)((V)(v_199 * (V)4.0d) + (V)1.0d)));
V v_214 = values_213[0];
if (R.Truth((V)(R.Invoke(v_206, "sub", R.Join(new V[] {v_214}, new Results((V)(v_214 + (V)3.0d)))).First == v_212))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_215 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_206, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_214 - (V)1.0d)))).First, R.Concat(v_212, R.Invoke(v_206, "sub", R.Join(new V[] {}, new Results((V)(v_214 + (V)4.0d)))).First))));
var obj_216 = R.Get(v_204, (V)"rows");
V key_217 = (V)(v_200 + (V)1.0d);
R.Set(obj_216, key_217, values_215[0]);
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, (V)"line", new V((RuleFunction)(args_218 => {
V v_219 = args_218[0];
V v_220 = args_218[1];
V v_221 = args_218[2];
V v_222 = args_218[3];
V v_223 = args_218[4];
V v_224 = args_218[5];
V v_225 = args_218[6];
V v_226 = args_218[7];
R.Call(v_6, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_225))).First == (V)"number"), () => (V)((V)(v_225 % (V)1.0d) == (V)0.0d)), () => (V)(v_225 >= (V)1.0d)), () => (V)(v_225 <= (V)64.0d))}, new Results((V)"invalid brush size")));
var values_227 = R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_222 - v_220)))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_223 - v_221)))));
V v_228 = values_227[0];
V v_229 = values_227[1];

var values_230 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_220 < v_222), () => (V)1.0d), () => (-(V)1.0d))));
V v_231 = values_230[0];

var values_232 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_221 < v_223), () => (V)1.0d), () => (-(V)1.0d))));
V v_233 = values_232[0];
var values_234 = R.Join(new V[] {}, new Results((V)(v_228 - v_229)));
V v_235 = values_234[0];

var values_236 = R.Join(new V[] {}, new Results((V)false));
V v_237 = values_236[0];
while (R.Truth((V)true)) {
{ double end_240 = R.Num((V)(v_221 + R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_225 / (V)2.0d)))).First)), step_241 = R.Num((V)1.0d);
for (double i_239 = R.Num((V)(v_221 - R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_225 - (V)1.0d) / (V)2.0d)))).First)); step_241 > 0 ? i_239 <= end_240 : i_239 >= end_240; i_239 += step_241) {
V v_238 = i_239;
{ double end_244 = R.Num((V)(v_220 + R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_225 / (V)2.0d)))).First)), step_245 = R.Num((V)1.0d);
for (double i_243 = R.Num((V)(v_220 - R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_225 - (V)1.0d) / (V)2.0d)))).First)); step_245 > 0 ? i_243 <= end_244 : i_243 >= end_244; i_243 += step_245) {
V v_242 = i_243;
var values_246 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"set"), R.Join(new V[] {v_219, v_242, v_238, v_224}, new Results(v_226))));
V v_247 = values_246[0];

var values_248 = R.Join(new V[] {}, new Results(R.Or(v_247, () => v_237)));
v_237 = values_248[0];
}
}
}
}
if (R.Truth(R.And((V)(v_220 == v_222), () => (V)(v_221 == v_223)))) {
break;
}
var values_249 = R.Join(new V[] {}, new Results((V)((V)2.0d * v_235)));
V v_250 = values_249[0];
if (R.Truth((V)(v_250 > (-v_229)))) {
var values_251 = R.Join(new V[] {}, new Results((V)(v_235 - v_229)));
v_235 = values_251[0];

var values_252 = R.Join(new V[] {}, new Results((V)(v_220 + v_231)));
v_220 = values_252[0];
}
if (R.Truth((V)(v_250 < v_228))) {
var values_253 = R.Join(new V[] {}, new Results((V)(v_235 + v_228)));
v_235 = values_253[0];

var values_254 = R.Join(new V[] {}, new Results((V)(v_221 + v_233)));
v_221 = values_254[0];
}
}
return R.Join(new V[] {}, new Results(v_237));
return Results.Empty;
})));
R.Set(v_2, (V)"fill", new V((RuleFunction)(args_255 => {
V v_256 = args_255[0];
V v_257 = args_255[1];
V v_258 = args_255[2];
V v_259 = args_255[3];
R.Call(v_22, R.Join(new V[] {}, new Results(v_259)));
var values_260 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"active"), R.Join(new V[] {}, new Results(v_256))));
V v_261 = values_260[0];

if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_261)), () => R.Get(v_261, (V)"locked")), () => (V)(!R.Truth(R.Get(v_261, (V)"visible")))))) {
return R.Join(new V[] {(V)false}, new Results((V)"layer is locked or hidden"));
}
var values_262 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {v_256, v_257}, new Results(v_258))));
V v_263 = values_262[0];

if (R.Truth((V)(!R.Truth(v_263)))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_264 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"char"), R.Join(new V[] {}, R.Call(rt.Global("unpack"), R.Join(new V[] {}, new Results(v_263))))));
V v_265 = values_264[0];

var values_266 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"char"), R.Join(new V[] {}, R.Call(rt.Global("unpack"), R.Join(new V[] {}, new Results(v_259))))));
V v_267 = values_266[0];
if (R.Truth((V)(v_265 == v_267))) {
return R.Join(new V[] {}, new Results((V)false));
}
V v_268 = V.Nil;
v_268 = new V((RuleFunction)(args_269 => {
V v_270 = args_269[0];
V v_271 = args_269[1];
return R.Join(new V[] {}, new Results((V)(R.Invoke(v_270, "sub", R.Join(new V[] {(V)((V)(v_271 * (V)4.0d) + (V)1.0d)}, new Results((V)((V)(v_271 * (V)4.0d) + (V)4.0d)))).First == v_265)));
return Results.Empty;
}));
var values_272 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, v_257), new Entry((V)2, v_258))))));
V v_273 = values_272[0];
while (R.Truth((V)((V)R.Length(v_273) > (V)0.0d))) {
var values_274 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {}, new Results(v_273))));
V v_275 = values_274[0];
var values_276 = R.Join(new V[] {R.Get(v_275, (V)1.0d)}, new Results(R.Get(v_275, (V)2.0d)));
V v_277 = values_276[0];
V v_278 = values_276[1];
if (R.Truth(R.And(R.And(R.And((V)(v_277 >= (V)0.0d), () => (V)(v_278 >= (V)0.0d)), () => (V)(v_277 < R.Get(v_256, (V)"width"))), () => (V)(v_278 < R.Get(v_256, (V)"height"))))) {
var values_279 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_261, (V)"rows"), (V)(v_278 + (V)1.0d))));
V v_280 = values_279[0];
if (R.Truth(R.Call(v_268, R.Join(new V[] {v_280}, new Results(v_277))).First)) {
var values_281 = R.Join(new V[] {v_277}, new Results(v_277));
V v_282 = values_281[0];
V v_283 = values_281[1];
while (R.Truth(R.And((V)(v_282 > (V)0.0d), () => R.Call(v_268, R.Join(new V[] {v_280}, new Results((V)(v_282 - (V)1.0d)))).First))) {
var values_284 = R.Join(new V[] {}, new Results((V)(v_282 - (V)1.0d)));
v_282 = values_284[0];
}
while (R.Truth(R.And((V)((V)(v_283 + (V)1.0d) < R.Get(v_256, (V)"width")), () => R.Call(v_268, R.Join(new V[] {v_280}, new Results((V)(v_283 + (V)1.0d)))).First))) {
var values_285 = R.Join(new V[] {}, new Results((V)(v_283 + (V)1.0d)));
v_283 = values_285[0];
}
var values_286 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_280, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_282 * (V)4.0d)))).First, R.Concat(R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {v_267}, new Results((V)((V)(v_283 - v_282) + (V)1.0d)))).First, R.Invoke(v_280, "sub", R.Join(new V[] {}, new Results((V)((V)((V)(v_283 + (V)1.0d) * (V)4.0d) + (V)1.0d)))).First))));
var obj_287 = R.Get(v_261, (V)"rows");
V key_288 = (V)(v_278 + (V)1.0d);
R.Set(obj_287, key_288, values_286[0]);
foreach (var row_289 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)(v_278 - (V)1.0d)), new Entry((V)2, (V)(v_278 + (V)1.0d))))))))) {
V v_290 = row_289[0];
V v_291 = row_289[1];
if (R.Truth(R.And((V)(v_291 >= (V)0.0d), () => (V)(v_291 < R.Get(v_256, (V)"height"))))) {
var values_292 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_261, (V)"rows"), (V)(v_291 + (V)1.0d))));
V v_293 = values_292[0];
var values_294 = R.Join(new V[] {}, new Results(v_282));
V v_295 = values_294[0];
while (R.Truth((V)(v_295 <= v_283))) {
if (R.Truth(R.Call(v_268, R.Join(new V[] {v_293}, new Results(v_295))).First)) {
var values_296 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_295), new Entry((V)2, v_291))));
var obj_297 = v_273;
V key_298 = (V)((V)R.Length(v_273) + (V)1.0d);
R.Set(obj_297, key_298, values_296[0]);
while (true) {
var values_299 = R.Join(new V[] {}, new Results((V)(v_295 + (V)1.0d)));
v_295 = values_299[0];
if (R.Truth(R.Or((V)(v_295 > v_283), () => (V)(!R.Truth(R.Call(v_268, R.Join(new V[] {v_293}, new Results(v_295))).First))))) break;
}
} else {
var values_300 = R.Join(new V[] {}, new Results((V)(v_295 + (V)1.0d)));
v_295 = values_300[0];
}
}
}
}
}
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, (V)"add_layer", new V((RuleFunction)(args_301 => {
V v_302 = args_301[0];
V v_303 = args_301[1];
R.Call(v_6, R.Join(new V[] {(V)((V)R.Length(R.Get(v_302, (V)"layers")) < R.Get(v_2, (V)"max_layers"))}, new Results((V)"layer limit reached")));
var values_304 = R.Join(new V[] {}, new Results(R.Concat((V)"layer_", R.Get(v_302, (V)"next_layer"))));
V v_305 = values_304[0];

var values_306 = R.Join(new V[] {}, new Results((V)(R.Get(v_302, (V)"next_layer") + (V)1.0d)));
var obj_307 = v_302;
V key_308 = (V)"next_layer";
R.Set(obj_307, key_308, values_306[0]);
var values_309 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_305), new Entry((V)"name", R.Or(v_303, () => R.Concat((V)"Layer ", (V)((V)R.Length(R.Get(v_302, (V)"layers")) + (V)1.0d)))), new Entry((V)"visible", (V)true), new Entry((V)"locked", (V)false), new Entry((V)"rows", R.Call(v_29, R.Join(new V[] {R.Get(v_302, (V)"width")}, new Results(R.Get(v_302, (V)"height")))).First))));
var obj_310 = R.Get(v_302, (V)"layers");
V key_311 = (V)((V)R.Length(R.Get(v_302, (V)"layers")) + (V)1.0d);
R.Set(obj_310, key_311, values_309[0]);
var values_312 = R.Join(new V[] {}, new Results(v_305));
var obj_313 = v_302;
V key_314 = (V)"selected";
R.Set(obj_313, key_314, values_312[0]);

return R.Join(new V[] {}, new Results(v_305));
return Results.Empty;
})));
R.Set(v_2, (V)"delete_layer", new V((RuleFunction)(args_315 => {
V v_316 = args_315[0];
R.Call(v_6, R.Join(new V[] {(V)((V)R.Length(R.Get(v_316, (V)"layers")) > (V)1.0d)}, new Results((V)"cannot delete last layer")));
var values_317 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"active"), R.Join(new V[] {}, new Results(v_316))));
V v_318 = values_317[0];
V v_319 = values_317[1];

R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_316, (V)"layers")}, new Results(v_319)));
var values_320 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_316, (V)"layers"), R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_319}, new Results((V)R.Length(R.Get(v_316, (V)"layers"))))).First), (V)"id")));
var obj_321 = v_316;
V key_322 = (V)"selected";
R.Set(obj_321, key_322, values_320[0]);
return Results.Empty;
})));
R.Set(v_2, (V)"move_layer", new V((RuleFunction)(args_323 => {
V v_324 = args_323[0];
V v_325 = args_323[1];
var values_326 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"active"), R.Join(new V[] {}, new Results(v_324))));
V v_327 = values_326[0];
V v_328 = values_326[1];

var values_329 = R.Join(new V[] {}, new Results((V)(v_328 + v_325)));
V v_330 = values_329[0];
if (R.Truth(R.Or((V)(v_330 < (V)1.0d), () => (V)(v_330 > (V)R.Length(R.Get(v_324, (V)"layers")))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_331 = R.Join(new V[] {R.Get(R.Get(v_324, (V)"layers"), v_330)}, new Results(R.Get(R.Get(v_324, (V)"layers"), v_328)));
var obj_332 = R.Get(v_324, (V)"layers");
V key_333 = v_328;
var obj_334 = R.Get(v_324, (V)"layers");
V key_335 = v_330;
R.Set(obj_332, key_333, values_331[0]);
R.Set(obj_334, key_335, values_331[1]);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, (V)"encode", new V((RuleFunction)(args_336 => {
V v_337 = args_336[0];
R.Call(R.Get(v_2, (V)"validate"), R.Join(new V[] {}, new Results(v_337)));
var values_338 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"ROAG-LAYERS/1\n"), new Entry((V)2, R.Call(v_10, R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_337, (V)"id"))))).First), new Entry((V)3, R.Get(v_337, (V)"id")), new Entry((V)4, R.Call(v_10, R.Join(new V[] {}, new Results(R.Get(v_337, (V)"width")))).First), new Entry((V)5, R.Call(v_10, R.Join(new V[] {}, new Results(R.Get(v_337, (V)"height")))).First), new Entry((V)6, R.Call(v_10, R.Join(new V[] {}, new Results(R.Get(v_337, (V)"next_layer")))).First), new Entry((V)7, R.Call(v_10, R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_337, (V)"layers"))))).First), new Entry((V)8, R.Call(v_10, R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_337, (V)"selected"))))).First), new Entry((V)9, R.Get(v_337, (V)"selected")))));
V v_339 = values_338[0];
foreach (var row_340 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_337, (V)"layers"))))))) {
V v_341 = row_340[0];
V v_342 = row_340[1];
var values_343 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_342, (V)"id"))))));
var obj_344 = v_339;
V key_345 = (V)((V)R.Length(v_339) + (V)1.0d);
R.Set(obj_344, key_345, values_343[0]);

var values_346 = R.Join(new V[] {}, new Results(R.Get(v_342, (V)"id")));
var obj_347 = v_339;
V key_348 = (V)((V)R.Length(v_339) + (V)1.0d);
R.Set(obj_347, key_348, values_346[0]);

var values_349 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_342, (V)"name"))))));
var obj_350 = v_339;
V key_351 = (V)((V)R.Length(v_339) + (V)1.0d);
R.Set(obj_350, key_351, values_349[0]);

var values_352 = R.Join(new V[] {}, new Results(R.Get(v_342, (V)"name")));
var obj_353 = v_339;
V key_354 = (V)((V)R.Length(v_339) + (V)1.0d);
R.Set(obj_353, key_354, values_352[0]);
var values_355 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"char"), R.Join(new V[] {R.Or(R.And(R.Get(v_342, (V)"visible"), () => (V)1.0d), () => (V)0.0d)}, new Results(R.Or(R.And(R.Get(v_342, (V)"locked"), () => (V)1.0d), () => (V)0.0d)))));
var obj_356 = v_339;
V key_357 = (V)((V)R.Length(v_339) + (V)1.0d);
R.Set(obj_356, key_357, values_355[0]);
var values_358 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(R.Get(v_342, (V)"rows")))));
var obj_359 = v_339;
V key_360 = (V)((V)R.Length(v_339) + (V)1.0d);
R.Set(obj_359, key_360, values_358[0]);
}
var values_361 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(v_339))));
V v_362 = values_361[0];

R.Call(v_6, R.Join(new V[] {(V)((V)R.Length(v_362) <= R.Get(v_2, (V)"max_bytes"))}, new Results((V)"document too large")));
var values_363 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
V v_364 = values_363[0];
V v_365 = values_363[1];

{ double end_368 = R.Num((V)R.Length(v_362)), step_369 = R.Num((V)1.0d);
for (double i_367 = R.Num((V)1.0d); step_369 > 0 ? i_367 <= end_368 : i_367 >= end_368; i_367 += step_369) {
V v_366 = i_367;
var values_370 = R.Join(new V[] {}, new Results((V)((V)(v_364 + R.Invoke(v_362, "byte", R.Join(new V[] {}, new Results(v_366))).First) % (V)65521.0d)));
v_364 = values_370[0];

var values_371 = R.Join(new V[] {}, new Results((V)((V)(v_365 + v_364) % (V)65521.0d)));
v_365 = values_371[0];
}
}
return R.Join(new V[] {}, new Results(R.Concat(v_362, R.Call(v_10, R.Join(new V[] {}, new Results((V)((V)(v_365 * (V)65536.0d) + v_364)))).First)));
return Results.Empty;
})));
R.Set(v_2, (V)"decode", new V((RuleFunction)(args_372 => {
V v_373 = args_372[0];
R.Call(v_6, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_373))).First == (V)"string"), () => (V)((V)R.Length(v_373) <= (V)(R.Get(v_2, (V)"max_bytes") + (V)4.0d))), () => (V)(R.Invoke(v_373, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)14.0d))).First == (V)"ROAG-LAYERS/1\n"))}, new Results((V)"invalid document header")));
var values_374 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
V v_375 = values_374[0];
V v_376 = values_374[1];

{ double end_379 = R.Num((V)((V)R.Length(v_373) - (V)4.0d)), step_380 = R.Num((V)1.0d);
for (double i_378 = R.Num((V)1.0d); step_380 > 0 ? i_378 <= end_379 : i_378 >= end_379; i_378 += step_380) {
V v_377 = i_378;
var values_381 = R.Join(new V[] {}, new Results((V)((V)(v_375 + R.Invoke(v_373, "byte", R.Join(new V[] {}, new Results(v_377))).First) % (V)65521.0d)));
v_375 = values_381[0];

var values_382 = R.Join(new V[] {}, new Results((V)((V)(v_376 + v_375) % (V)65521.0d)));
v_376 = values_382[0];
}
}
R.Call(v_6, R.Join(new V[] {(V)(R.Call(v_13, R.Join(new V[] {v_373}, new Results((V)((V)R.Length(v_373) - (V)3.0d)))).First == (V)((V)(v_376 * (V)65536.0d) + v_375))}, new Results((V)"document checksum mismatch")));
var values_383 = R.Join(new V[] {}, new Results((V)15.0d));
V v_384 = values_383[0];

V v_385 = V.Nil;
v_385 = new V((RuleFunction)(args_386 => {

var values_387 = R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {v_373}, new Results(v_384))));
V v_388 = values_387[0];

var values_389 = R.Join(new V[] {}, new Results((V)(v_384 + (V)4.0d)));
v_384 = values_389[0];

R.Call(v_6, R.Join(new V[] {R.And((V)(v_388 <= (V)1024.0d), () => (V)((V)((V)(v_384 + v_388) - (V)1.0d) <= (V)((V)R.Length(v_373) - (V)4.0d)))}, new Results((V)"invalid document string")));
var values_390 = R.Join(new V[] {}, R.Invoke(v_373, "sub", R.Join(new V[] {v_384}, new Results((V)((V)(v_384 + v_388) - (V)1.0d)))));
V v_391 = values_390[0];

var values_392 = R.Join(new V[] {}, new Results((V)(v_384 + v_388)));
v_384 = values_392[0];

return R.Join(new V[] {}, new Results(v_391));
return Results.Empty;
}));
var values_393 = R.Join(new V[] {}, R.Call(v_385, Results.Empty));
V v_394 = values_393[0];

var values_395 = R.Join(new V[] {R.Call(v_13, R.Join(new V[] {v_373}, new Results(v_384))).First}, R.Call(v_13, R.Join(new V[] {v_373}, new Results((V)(v_384 + (V)4.0d)))));
V v_396 = values_395[0];
V v_397 = values_395[1];

var values_398 = R.Join(new V[] {}, new Results((V)(v_384 + (V)8.0d)));
v_384 = values_398[0];
R.Call(v_6, R.Join(new V[] {R.Call(v_44, R.Join(new V[] {v_396}, new Results(v_397))).First}, new Results((V)"invalid canvas dimensions")));
var values_399 = R.Join(new V[] {R.Call(v_13, R.Join(new V[] {v_373}, new Results(v_384))).First}, R.Call(v_13, R.Join(new V[] {v_373}, new Results((V)(v_384 + (V)4.0d)))));
V v_400 = values_399[0];
V v_401 = values_399[1];

var values_402 = R.Join(new V[] {}, new Results((V)(v_384 + (V)8.0d)));
v_384 = values_402[0];
R.Call(v_6, R.Join(new V[] {R.And((V)(v_401 >= (V)1.0d), () => (V)(v_401 <= R.Get(v_2, (V)"max_layers")))}, new Results((V)"invalid layer count")));
var values_403 = R.Join(new V[] {}, R.Call(v_385, Results.Empty));
V v_404 = values_403[0];

var values_405 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_394), new Entry((V)"width", v_396), new Entry((V)"height", v_397), new Entry((V)"next_layer", v_400), new Entry((V)"selected", v_404), new Entry((V)"layers", R.Table()))));
V v_406 = values_405[0];
{ double end_409 = R.Num(v_401), step_410 = R.Num((V)1.0d);
for (double i_408 = R.Num((V)1.0d); step_410 > 0 ? i_408 <= end_409 : i_408 >= end_409; i_408 += step_410) {
V v_407 = i_408;
var values_411 = R.Join(new V[] {R.Call(v_385, Results.Empty).First}, R.Call(v_385, Results.Empty));
V v_412 = values_411[0];
V v_413 = values_411[1];

var values_414 = R.Join(new V[] {}, R.Invoke(v_373, "byte", R.Join(new V[] {v_384}, new Results((V)(v_384 + (V)1.0d)))));
V v_415 = values_414[0];
V v_416 = values_414[1];

var values_417 = R.Join(new V[] {}, new Results((V)(v_384 + (V)2.0d)));
v_384 = values_417[0];
R.Call(v_6, R.Join(new V[] {R.And(R.Or((V)(v_415 == (V)0.0d), () => (V)(v_415 == (V)1.0d)), () => R.Or((V)(v_416 == (V)0.0d), () => (V)(v_416 == (V)1.0d)))}, new Results((V)"invalid layer flags")));
var values_418 = R.Join(new V[] {}, new Results(R.Table()));
V v_419 = values_418[0];

{ double end_422 = R.Num(v_397), step_423 = R.Num((V)1.0d);
for (double i_421 = R.Num((V)1.0d); step_423 > 0 ? i_421 <= end_422 : i_421 >= end_422; i_421 += step_423) {
V v_420 = i_421;
R.Call(v_6, R.Join(new V[] {(V)((V)((V)(v_384 + (V)(v_396 * (V)4.0d)) - (V)1.0d) <= (V)((V)R.Length(v_373) - (V)4.0d))}, new Results((V)"truncated raster")));

var values_424 = R.Join(new V[] {}, R.Invoke(v_373, "sub", R.Join(new V[] {v_384}, new Results((V)((V)(v_384 + (V)(v_396 * (V)4.0d)) - (V)1.0d)))));
var obj_425 = v_419;
V key_426 = v_420;
R.Set(obj_425, key_426, values_424[0]);

var values_427 = R.Join(new V[] {}, new Results((V)(v_384 + (V)(v_396 * (V)4.0d))));
v_384 = values_427[0];
}
}
var values_428 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_412), new Entry((V)"name", v_413), new Entry((V)"visible", (V)(v_415 == (V)1.0d)), new Entry((V)"locked", (V)(v_416 == (V)1.0d)), new Entry((V)"rows", v_419))));
var obj_429 = R.Get(v_406, (V)"layers");
V key_430 = (V)((V)R.Length(R.Get(v_406, (V)"layers")) + (V)1.0d);
R.Set(obj_429, key_430, values_428[0]);
}
}
R.Call(v_6, R.Join(new V[] {(V)(v_384 == (V)((V)R.Length(v_373) - (V)3.0d))}, new Results((V)"trailing raster data")));

R.Call(R.Get(v_2, (V)"validate"), R.Join(new V[] {}, new Results(v_406)));

return R.Join(new V[] {}, new Results(v_406));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
