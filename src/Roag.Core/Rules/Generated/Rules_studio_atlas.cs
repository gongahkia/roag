// Static C# port of src/studio_atlas.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 41995c9d6b2e50493189f217a4c561aac4c7460be5ae300f3e1bea6608da49a4
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_atlas { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_project"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"bit"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", (V)"roag-studio-atlas/1"), new Entry((V)"max_pages", (V)32.0d), new Entry((V)"slots", R.Table(new Entry((V)1, (V)"assets/studio/atlas-a.pack"), new Entry((V)2, (V)"assets/studio/atlas-b.pack"))))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table()));
V v_10 = values_9[0];
{ double end_13 = R.Num((V)255.0d), step_14 = R.Num((V)1.0d);
for (double i_12 = R.Num((V)0.0d); step_14 > 0 ? i_12 <= end_13 : i_12 >= end_13; i_12 += step_14) {
V v_11 = i_12;
var values_15 = R.Join(new V[] {}, new Results(v_11));
V v_16 = values_15[0];
{ double end_19 = R.Num((V)8.0d), step_20 = R.Num((V)1.0d);
for (double i_18 = R.Num((V)1.0d); step_20 > 0 ? i_18 <= end_19 : i_18 >= end_19; i_18 += step_20) {
V v_17 = i_18;
var values_21 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Call(R.Get(v_6, (V)"band"), R.Join(new V[] {v_16}, new Results((V)1.0d))).First == (V)1.0d), () => R.Call(R.Get(v_6, (V)"bxor"), R.Join(new V[] {R.Call(R.Get(v_6, (V)"rshift"), R.Join(new V[] {v_16}, new Results((V)1.0d))).First}, new Results((V)3988292384.0d))).First), () => R.Call(R.Get(v_6, (V)"rshift"), R.Join(new V[] {v_16}, new Results((V)1.0d))).First)));
v_16 = values_21[0];
}
}
var values_22 = R.Join(new V[] {}, new Results(v_16));
var obj_23 = v_10;
V key_24 = v_11;
R.Set(obj_23, key_24, values_22[0]);
}
}
R.Set(v_8, (V)"checksum", new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
var values_27 = R.Join(new V[] {(V)1.0d, (V)0.0d}, new Results((V)4294967295.0d));
V v_28 = values_27[0];
V v_29 = values_27[1];
V v_30 = values_27[2];
{ double end_33 = R.Num((V)R.Length(v_26)), step_34 = R.Num((V)1.0d);
for (double i_32 = R.Num((V)1.0d); step_34 > 0 ? i_32 <= end_33 : i_32 >= end_33; i_32 += step_34) {
V v_31 = i_32;
var values_35 = R.Join(new V[] {}, R.Invoke(v_26, "byte", R.Join(new V[] {}, new Results(v_31))));
V v_36 = values_35[0];
var values_37 = R.Join(new V[] {}, new Results((V)((V)(v_28 + v_36) % (V)65521.0d)));
v_28 = values_37[0];

var values_38 = R.Join(new V[] {}, new Results((V)((V)(v_29 + v_28) % (V)65521.0d)));
v_29 = values_38[0];
var values_39 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"bxor"), R.Join(new V[] {R.Get(v_10, R.Call(R.Get(v_6, (V)"band"), R.Join(new V[] {R.Call(R.Get(v_6, (V)"bxor"), R.Join(new V[] {v_30}, new Results(v_36))).First}, new Results((V)255.0d))).First)}, R.Call(R.Get(v_6, (V)"rshift"), R.Join(new V[] {v_30}, new Results((V)8.0d))))));
v_30 = values_39[0];
}
}
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"format"), R.Join(new V[] {(V)"%08x%08x", (V)((V)(v_29 * (V)65536.0d) + v_28)}, new Results((V)(R.Call(R.Get(v_6, (V)"bxor"), R.Join(new V[] {v_30}, new Results((V)4294967295.0d))).First % (V)4294967296.0d)))));
return Results.Empty;
})));
V v_40 = V.Nil;
v_40 = new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_42))).First == (V)"string"), () => (V)((V)R.Length(v_42) == (V)16.0d)), () => (V)(R.Invoke(v_42, "match", R.Join(new V[] {}, new Results((V)"^%x+$"))).First != V.Nil))));
return Results.Empty;
}));
V v_43 = V.Nil;
v_43 = new V((RuleFunction)(args_44 => {
V v_45 = args_44[0];
V v_46 = args_44[1];
V v_47 = args_44[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_45))).First == (V)"number"), () => (V)((V)(v_45 % (V)1.0d) == (V)0.0d)), () => (V)(v_45 >= v_46)), () => (V)(v_45 <= v_47))));
return Results.Empty;
}));
V v_48 = V.Nil;
v_48 = new V((RuleFunction)(args_49 => {
V v_50 = args_49[0];
var values_51 = R.Join(new V[] {}, new Results((V)8.0d));
V v_52 = values_51[0];
while (R.Truth((V)(v_52 < v_50))) {
var values_53 = R.Join(new V[] {}, new Results((V)(v_52 * (V)2.0d)));
v_52 = values_53[0];
}
return R.Join(new V[] {}, new Results(v_52));
return Results.Empty;
}));
V v_54 = V.Nil;
v_54 = new V((RuleFunction)(args_55 => {
V v_56 = args_55[0];
return R.Join(new V[] {}, new Results((V)(R.Call(v_48, R.Join(new V[] {}, new Results(v_56))).First == v_56)));
return Results.Empty;
}));
R.Set(v_8, (V)"inputs", new V((RuleFunction)(args_57 => {
V v_58 = args_57[0];
V v_59 = args_57[1];
R.Call(R.Get(v_2, (V)"validate"), R.Join(new V[] {}, new Results(v_58)));
var values_60 = R.Join(new V[] {R.Table(), R.Table()}, new Results(R.Table()));
V v_61 = values_60[0];
V v_62 = values_60[1];
V v_63 = values_60[2];
var values_64 = R.Join(new V[] {}, new Results(R.Table()));
V v_65 = values_64[0];
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_58, (V)"assets"))))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
var values_69 = R.Join(new V[] {}, new Results(v_68));
var obj_70 = v_65;
V key_71 = (V)((V)R.Length(v_65) + (V)1.0d);
R.Set(obj_70, key_71, values_69[0]);
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_65}, new Results(new V((RuleFunction)(args_72 => {
V v_73 = args_72[0];
V v_74 = args_72[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_73, (V)"id") < R.Get(v_74, (V)"id"))));
return Results.Empty;
})))));
foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_65)))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
if (R.Truth((V)(R.Get(v_77, (V)"source") != (V)""))) {
var values_78 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_59, R.Join(new V[] {}, new Results(R.Concat((V)"assets/studio/", R.Get(v_77, (V)"source"))))).First}, new Results(R.Concat((V)"missing published image: ", R.Get(v_77, (V)"id"))))));
V v_79 = values_78[0];
var values_80 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"png_dimensions"), R.Join(new V[] {}, new Results(v_79))));
V v_81 = values_80[0];
V v_82 = values_80[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(v_81 == R.Get(v_77, (V)"width")), () => (V)(v_82 == R.Get(v_77, (V)"height")))}, new Results(R.Concat((V)"published image dimensions changed: ", R.Get(v_77, (V)"id")))));
var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"checksum"), R.Join(new V[] {}, new Results(v_79))));
V v_84 = values_83[0];
var values_85 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Table(new Entry((V)1, R.Get(v_77, (V)"id")), new Entry((V)2, R.Get(v_77, (V)"source")), new Entry((V)3, (V)R.Length(v_79)), new Entry((V)4, v_84))}, new Results((V)":"))));
var obj_86 = v_63;
V key_87 = (V)((V)R.Length(v_63) + (V)1.0d);
R.Set(obj_86, key_87, values_85[0]);
if (R.Truth(R.Or((V)(R.Get(v_77, (V)"kind") == (V)"glyph"), () => (V)(R.Get(v_77, (V)"kind") == (V)"bitmap_font")))) {
var values_88 = R.Join(new V[] {}, new Results((V)"bitmap font uses standalone ImageData for glyph fallback"));
var obj_89 = v_62;
V key_90 = R.Get(v_77, (V)"id");
R.Set(obj_89, key_90, values_88[0]);
} else if (R.Truth((V)(R.Get(v_77, (V)"kind") == (V)"shader"))) {
var values_91 = R.Join(new V[] {}, new Results((V)"shader references are not static atlas sprites"));
var obj_92 = v_62;
V key_93 = R.Get(v_77, (V)"id");
R.Set(obj_92, key_93, values_91[0]);
} else if (R.Truth(R.And(R.Or((V)(R.Get(v_77, (V)"kind") == (V)"background"), () => (V)(R.Get(v_77, (V)"kind") == (V)"ui_background")), () => R.Or((V)(v_81 > (V)512.0d), () => (V)(v_82 > (V)512.0d))))) {
var values_94 = R.Join(new V[] {}, new Results((V)"large background kept standalone"));
var obj_95 = v_62;
V key_96 = R.Get(v_77, (V)"id");
R.Set(obj_95, key_96, values_94[0]);
} else {
var values_97 = R.Join(new V[] {}, new Results(R.Get(v_77, (V)"atlas_group")));
V v_98 = values_97[0];
if (R.Truth((V)(v_98 == (V)""))) {
if (R.Truth(R.Or((V)(R.Get(v_77, (V)"kind") == (V)"actor"), () => (V)(R.Get(v_77, (V)"kind") == (V)"enemy")))) {
var values_99 = R.Join(new V[] {}, new Results((V)"actors"));
v_98 = values_99[0];
} else if (R.Truth((V)(R.Get(v_77, (V)"kind") == (V)"tile"))) {
var values_100 = R.Join(new V[] {}, new Results((V)"world"));
v_98 = values_100[0];
} else if (R.Truth(R.Or((V)(R.Get(v_77, (V)"kind") == (V)"effect"), () => (V)(R.Get(v_77, (V)"kind") == (V)"effect_sprite")))) {
var values_101 = R.Join(new V[] {}, new Results((V)"effects"));
v_98 = values_101[0];
} else if (R.Truth(R.Or((V)(R.Get(v_77, (V)"kind") == (V)"background"), () => (V)(R.Get(v_77, (V)"kind") == (V)"ui_background")))) {
var values_102 = R.Join(new V[] {}, new Results((V)"backgrounds"));
v_98 = values_102[0];
} else {
var values_103 = R.Join(new V[] {}, new Results((V)"ui"));
v_98 = values_103[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Invoke(v_98, "match", R.Join(new V[] {}, new Results((V)"^[a-z0-9_-]+$"))).First}, new Results(R.Concat((V)"invalid atlas group: ", R.Get(v_77, (V)"id")))));
var values_104 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_77, (V)"id")), new Entry((V)"w", v_81), new Entry((V)"h", v_82), new Entry((V)"filter", R.Get(v_77, (V)"filter")), new Entry((V)"group", v_98), new Entry((V)"checksum", v_84), new Entry((V)"source", R.Get(v_77, (V)"source")), new Entry((V)"bytes", v_79))));
var obj_105 = v_61;
V key_106 = (V)((V)R.Length(v_61) + (V)1.0d);
R.Set(obj_105, key_106, values_104[0]);
}
}
}
return R.Join(new V[] {v_61, v_62}, R.Call(R.Get(v_8, (V)"checksum"), R.Join(new V[] {}, new Results(R.Concat(R.Call(R.Get(v_2, (V)"encode"), R.Join(new V[] {}, new Results(v_58))).First, R.Concat((V)"\n", R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_63}, new Results((V)"\n"))).First))))));
return Results.Empty;
})));
R.Set(v_8, (V)"plan", new V((RuleFunction)(args_107 => {
V v_108 = args_107[0];
V v_109 = args_107[1];
V v_110 = args_107[2];
V v_111 = args_107[3];
var values_112 = R.Join(new V[] {}, new Results(R.Or(v_110, () => (V)2048.0d)));
v_110 = values_112[0];

var values_113 = R.Join(new V[] {}, new Results(R.Or(v_111, () => (V)2.0d)));
v_111 = values_113[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Call(v_43, R.Join(new V[] {v_110, (V)32.0d}, new Results((V)2048.0d))).First, () => R.Call(v_54, R.Join(new V[] {}, new Results(v_110))).First), () => R.Call(v_43, R.Join(new V[] {v_111, (V)1.0d}, new Results((V)8.0d))).First)}, new Results((V)"invalid atlas size or padding")));
var values_114 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"inputs"), R.Join(new V[] {v_108}, new Results(v_109))));
V v_115 = values_114[0];
V v_116 = values_114[1];
V v_117 = values_114[2];
var values_118 = R.Join(new V[] {}, new Results(R.Table()));
V v_119 = values_118[0];
foreach (var row_120 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_115)))))) {
V v_121 = row_120[0];
V v_122 = row_120[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)((V)(R.Get(v_122, (V)"w") + (V)(v_111 * (V)2.0d)) <= v_110), () => (V)((V)(R.Get(v_122, (V)"h") + (V)(v_111 * (V)2.0d)) <= v_110))}, new Results(R.Concat((V)"asset exceeds atlas page: ", R.Get(v_122, (V)"id")))));
var values_123 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_122, (V)"filter"), R.Concat((V)":", R.Get(v_122, (V)"group")))));
V v_124 = values_123[0];
var values_125 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_119, v_124), () => R.Table())));
var obj_126 = v_119;
V key_127 = v_124;
R.Set(obj_126, key_127, values_125[0]);
var values_128 = R.Join(new V[] {}, new Results(v_122));
var obj_129 = R.Get(v_119, v_124);
V key_130 = (V)((V)R.Length(R.Get(v_119, v_124)) + (V)1.0d);
R.Set(obj_129, key_130, values_128[0]);
}
var values_131 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_132 = values_131[0];
V v_133 = values_131[1];
var values_134 = R.Join(new V[] {}, new Results(R.Table()));
V v_135 = values_134[0];

foreach (var row_136 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_119)))))) {
V v_137 = row_136[0];
var values_138 = R.Join(new V[] {}, new Results(v_137));
var obj_139 = v_135;
V key_140 = (V)((V)R.Length(v_135) + (V)1.0d);
R.Set(obj_139, key_140, values_138[0]);
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_135)));
foreach (var row_141 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_135)))))) {
V v_142 = row_141[0];
V v_143 = row_141[1];
var values_144 = R.Join(new V[] {}, R.Invoke(v_143, "match", R.Join(new V[] {}, new Results((V)"^([^:]+):(.+)$"))));
V v_145 = values_144[0];
V v_146 = values_144[1];
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {R.Get(v_119, v_143)}, new Results(new V((RuleFunction)(args_147 => {
V v_148 = args_147[0];
V v_149 = args_147[1];
if (R.Truth((V)(R.Get(v_148, (V)"h") != R.Get(v_149, (V)"h")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_148, (V)"h") > R.Get(v_149, (V)"h"))));
}
if (R.Truth((V)(R.Get(v_148, (V)"w") != R.Get(v_149, (V)"w")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_148, (V)"w") > R.Get(v_149, (V)"w"))));
}
return R.Join(new V[] {}, new Results((V)(R.Get(v_148, (V)"id") < R.Get(v_149, (V)"id"))));
return Results.Empty;
})))));
var values_150 = Results.Empty;
V v_151 = values_150[0];
V v_152 = values_150[1];
V v_153 = values_150[2];
V v_154 = values_150[3];
foreach (var row_155 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_119, v_143))))))) {
V v_156 = row_155[0];
V v_157 = row_155[1];
var values_158 = R.Join(new V[] {(V)(R.Get(v_157, (V)"w") + (V)(v_111 * (V)2.0d))}, new Results((V)(R.Get(v_157, (V)"h") + (V)(v_111 * (V)2.0d))));
V v_159 = values_158[0];
V v_160 = values_158[1];
if (R.Truth(R.Or((V)(!R.Truth(v_151)), () => (V)((V)(v_153 + v_160) > v_110)))) {
var values_161 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"filter", v_145), new Entry((V)"group", v_146), new Entry((V)"used_w", (V)0.0d), new Entry((V)"used_h", (V)0.0d), new Entry((V)"entries", R.Table()))));
v_151 = values_161[0];
var values_162 = R.Join(new V[] {}, new Results(v_151));
var obj_163 = v_132;
V key_164 = (V)((V)R.Length(v_132) + (V)1.0d);
R.Set(obj_163, key_164, values_162[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_132) <= R.Get(v_8, (V)"max_pages"))}, new Results((V)"atlas page count limit")));
var values_165 = R.Join(new V[] {(V)0.0d, (V)0.0d}, new Results((V)0.0d));
v_152 = values_165[0];
v_153 = values_165[1];
v_154 = values_165[2];
}
if (R.Truth((V)((V)(v_152 + v_159) > v_110))) {
var values_166 = R.Join(new V[] {}, new Results((V)0.0d));
v_152 = values_166[0];

var values_167 = R.Join(new V[] {}, new Results((V)(v_153 + v_154)));
v_153 = values_167[0];

var values_168 = R.Join(new V[] {}, new Results((V)0.0d));
v_154 = values_168[0];
if (R.Truth((V)((V)(v_153 + v_160) > v_110))) {
var values_169 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"filter", v_145), new Entry((V)"group", v_146), new Entry((V)"used_w", (V)0.0d), new Entry((V)"used_h", (V)0.0d), new Entry((V)"entries", R.Table()))));
v_151 = values_169[0];
var values_170 = R.Join(new V[] {}, new Results(v_151));
var obj_171 = v_132;
V key_172 = (V)((V)R.Length(v_132) + (V)1.0d);
R.Set(obj_171, key_172, values_170[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_132) <= R.Get(v_8, (V)"max_pages"))}, new Results((V)"atlas page count limit")));
var values_173 = R.Join(new V[] {}, new Results((V)0.0d));
v_153 = values_173[0];
}
}
var values_174 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_157, (V)"id")), new Entry((V)"x", (V)(v_152 + v_111)), new Entry((V)"y", (V)(v_153 + v_111)), new Entry((V)"w", R.Get(v_157, (V)"w")), new Entry((V)"h", R.Get(v_157, (V)"h")), new Entry((V)"padding", v_111), new Entry((V)"checksum", R.Get(v_157, (V)"checksum")), new Entry((V)"source", R.Get(v_157, (V)"source")), new Entry((V)"filter", v_145), new Entry((V)"group", v_146), new Entry((V)"page", (V)R.Length(v_132)))));
V v_175 = values_174[0];
var values_176 = R.Join(new V[] {}, new Results(v_175));
var obj_177 = v_133;
V key_178 = R.Get(v_157, (V)"id");
R.Set(obj_177, key_178, values_176[0]);
var values_179 = R.Join(new V[] {}, new Results(v_175));
var obj_180 = R.Get(v_151, (V)"entries");
V key_181 = (V)((V)R.Length(R.Get(v_151, (V)"entries")) + (V)1.0d);
R.Set(obj_180, key_181, values_179[0]);
var values_182 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_151, (V)"used_w")}, new Results((V)(v_152 + v_159)))));
var obj_183 = v_151;
V key_184 = (V)"used_w";
R.Set(obj_183, key_184, values_182[0]);
var values_185 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_151, (V)"used_h")}, new Results((V)(v_153 + v_160)))));
var obj_186 = v_151;
V key_187 = (V)"used_h";
R.Set(obj_186, key_187, values_185[0]);
var values_188 = R.Join(new V[] {}, new Results((V)(v_152 + v_159)));
v_152 = values_188[0];

var values_189 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_154}, new Results(v_160))));
v_154 = values_189[0];
}
}
foreach (var row_190 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_132)))))) {
V v_191 = row_190[0];
V v_192 = row_190[1];
var values_193 = R.Join(new V[] {}, R.Call(v_48, R.Join(new V[] {}, new Results(R.Get(v_192, (V)"used_w")))));
var obj_194 = v_192;
V key_195 = (V)"width";
R.Set(obj_194, key_195, values_193[0]);

var values_196 = R.Join(new V[] {}, R.Call(v_48, R.Join(new V[] {}, new Results(R.Get(v_192, (V)"used_h")))));
var obj_197 = v_192;
V key_198 = (V)"height";
R.Set(obj_197, key_198, values_196[0]);
}
return R.Join(new V[] {R.Table(new Entry((V)"schema", R.Get(v_8, (V)"schema")), new Entry((V)"project_id", R.Get(v_108, (V)"id")), new Entry((V)"fingerprint", v_117), new Entry((V)"max_size", v_110), new Entry((V)"padding", v_111), new Entry((V)"pages", v_132), new Entry((V)"assets", v_133), new Entry((V)"excluded", v_116))}, new Results(v_115));
return Results.Empty;
})));
R.Set(v_8, (V)"validate", new V((RuleFunction)(args_199 => {
V v_200 = args_199[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_200))).First == (V)"table"), () => (V)(R.Get(v_200, (V)"schema") == R.Get(v_8, (V)"schema")))}, new Results((V)"incompatible atlas schema")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(R.Get(v_200, (V)"project_id")))).First, () => R.Call(v_40, R.Join(new V[] {}, new Results(R.Get(v_200, (V)"fingerprint")))).First)}, new Results((V)"invalid atlas identity")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Call(v_43, R.Join(new V[] {R.Get(v_200, (V)"max_size"), (V)32.0d}, new Results((V)2048.0d))).First, () => R.Call(v_54, R.Join(new V[] {}, new Results(R.Get(v_200, (V)"max_size")))).First), () => R.Call(v_43, R.Join(new V[] {R.Get(v_200, (V)"padding"), (V)1.0d}, new Results((V)8.0d))).First)}, new Results((V)"invalid atlas configuration")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_200, (V)"pages")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_200, (V)"pages")) <= R.Get(v_8, (V)"max_pages"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_200, (V)"assets")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_200, (V)"excluded")))).First == (V)"table"))}, new Results((V)"invalid atlas tables")));
var values_201 = R.Join(new V[] {}, new Results(R.Table()));
V v_202 = values_201[0];
foreach (var row_203 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_200, (V)"pages"))))))) {
V v_204 = row_203[0];
V v_205 = row_203[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_205, (V)"filter") == (V)"nearest"), () => (V)(R.Get(v_205, (V)"filter") == (V)"linear"))}, new Results((V)"invalid atlas filter")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_205, (V)"group")))).First == (V)"string"), () => R.Invoke(R.Get(v_205, (V)"group"), "match", R.Join(new V[] {}, new Results((V)"^[a-z0-9_-]+$"))).First)}, new Results((V)"invalid atlas group")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Call(v_43, R.Join(new V[] {R.Get(v_205, (V)"width"), (V)8.0d}, new Results(R.Get(v_200, (V)"max_size")))).First, () => R.Call(v_43, R.Join(new V[] {R.Get(v_205, (V)"height"), (V)8.0d}, new Results(R.Get(v_200, (V)"max_size")))).First), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_205, (V)"entries")))).First == (V)"table"))}, new Results((V)"invalid atlas page")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(v_40, R.Join(new V[] {}, new Results(R.Get(v_205, (V)"checksum")))).First, () => (V)(R.Get(v_205, (V)"filename") == R.Concat((V)"assets/studio/atlas/page-", R.Concat(R.Get(v_205, (V)"checksum"), (V)".png"))))}, new Results((V)"invalid atlas page filename")));
foreach (var row_206 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_205, (V)"entries"))))))) {
V v_207 = row_206[0];
V v_208 = row_206[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(R.Get(v_208, (V)"id")))).First, () => (V)(!R.Truth(R.Get(v_202, R.Get(v_208, (V)"id")))))}, new Results((V)"duplicate or invalid atlas asset ID")));
var values_209 = R.Join(new V[] {}, new Results((V)true));
var obj_210 = v_202;
V key_211 = R.Get(v_208, (V)"id");
R.Set(obj_210, key_211, values_209[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(R.Get(v_200, (V)"assets"), R.Get(v_208, (V)"id")), () => (V)(R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_200, (V)"assets"), R.Get(v_208, (V)"id"))))).First == R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(v_208))).First))}, new Results((V)"atlas mapping differs from page")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_208, (V)"page") == v_204), () => (V)(R.Get(v_208, (V)"filter") == R.Get(v_205, (V)"filter"))), () => (V)(R.Get(v_208, (V)"group") == R.Get(v_205, (V)"group"))), () => R.Call(v_43, R.Join(new V[] {R.Get(v_208, (V)"x"), R.Get(v_200, (V)"padding")}, new Results(R.Get(v_205, (V)"width")))).First), () => R.Call(v_43, R.Join(new V[] {R.Get(v_208, (V)"y"), R.Get(v_200, (V)"padding")}, new Results(R.Get(v_205, (V)"height")))).First), () => R.Call(v_43, R.Join(new V[] {R.Get(v_208, (V)"w"), (V)1.0d}, new Results((V)1024.0d))).First), () => R.Call(v_43, R.Join(new V[] {R.Get(v_208, (V)"h"), (V)1.0d}, new Results((V)1024.0d))).First), () => (V)(R.Get(v_208, (V)"padding") == R.Get(v_200, (V)"padding"))), () => (V)((V)((V)(R.Get(v_208, (V)"x") + R.Get(v_208, (V)"w")) + R.Get(v_200, (V)"padding")) <= R.Get(v_205, (V)"width"))), () => (V)((V)((V)(R.Get(v_208, (V)"y") + R.Get(v_208, (V)"h")) + R.Get(v_200, (V)"padding")) <= R.Get(v_205, (V)"height"))), () => R.Call(R.Get(v_2, (V)"valid_path"), R.Join(new V[] {}, new Results(R.Get(v_208, (V)"source")))).First), () => R.Call(v_40, R.Join(new V[] {}, new Results(R.Get(v_208, (V)"checksum")))).First)}, new Results((V)"atlas rectangle out of bounds")));
}
{ double end_214 = R.Num((V)R.Length(R.Get(v_205, (V)"entries"))), step_215 = R.Num((V)1.0d);
for (double i_213 = R.Num((V)1.0d); step_215 > 0 ? i_213 <= end_214 : i_213 >= end_214; i_213 += step_215) {
V v_212 = i_213;
var values_216 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_205, (V)"entries"), v_212)));
V v_217 = values_216[0];
{ double end_220 = R.Num((V)R.Length(R.Get(v_205, (V)"entries"))), step_221 = R.Num((V)1.0d);
for (double i_219 = R.Num((V)(v_212 + (V)1.0d)); step_221 > 0 ? i_219 <= end_220 : i_219 >= end_220; i_219 += step_221) {
V v_218 = i_219;
var values_222 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_205, (V)"entries"), v_218)));
V v_223 = values_222[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or(R.Or((V)((V)((V)(R.Get(v_217, (V)"x") + R.Get(v_217, (V)"w")) + R.Get(v_200, (V)"padding")) <= (V)(R.Get(v_223, (V)"x") - R.Get(v_200, (V)"padding"))), () => (V)((V)((V)(R.Get(v_223, (V)"x") + R.Get(v_223, (V)"w")) + R.Get(v_200, (V)"padding")) <= (V)(R.Get(v_217, (V)"x") - R.Get(v_200, (V)"padding")))), () => (V)((V)((V)(R.Get(v_217, (V)"y") + R.Get(v_217, (V)"h")) + R.Get(v_200, (V)"padding")) <= (V)(R.Get(v_223, (V)"y") - R.Get(v_200, (V)"padding")))), () => (V)((V)((V)(R.Get(v_223, (V)"y") + R.Get(v_223, (V)"h")) + R.Get(v_200, (V)"padding")) <= (V)(R.Get(v_217, (V)"y") - R.Get(v_200, (V)"padding"))))}, new Results((V)"overlapping atlas rectangles")));
}
}
}
}
}
foreach (var row_224 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_200, (V)"assets"))))))) {
V v_225 = row_224[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_202, v_225)}, new Results((V)"orphan atlas mapping")));
}
foreach (var row_226 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_200, (V)"excluded"))))))) {
V v_227 = row_226[0];
V v_228 = row_226[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(v_227))).First, () => (V)(!R.Truth(R.Get(v_202, v_227)))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_228))).First == (V)"string")), () => (V)((V)R.Length(v_228) > (V)0.0d)), () => (V)((V)R.Length(v_228) <= (V)120.0d))}, new Results((V)"invalid atlas exclusion")));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_8, (V)"matches_inputs", new V((RuleFunction)(args_229 => {
V v_230 = args_229[0];
V v_231 = args_229[1];
V v_232 = args_229[2];
R.Call(R.Get(v_8, (V)"validate"), R.Join(new V[] {}, new Results(v_230)));
var values_233 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"plan"), R.Join(new V[] {v_231, v_232, R.Get(v_230, (V)"max_size")}, new Results(R.Get(v_230, (V)"padding")))));
V v_234 = values_233[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_230, (V)"project_id") != R.Get(v_234, (V)"project_id")), () => (V)(R.Get(v_230, (V)"fingerprint") != R.Get(v_234, (V)"fingerprint"))), () => (V)(R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_230, (V)"assets")))).First != R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_234, (V)"assets")))).First)), () => (V)(R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_230, (V)"excluded")))).First != R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_234, (V)"excluded")))).First)), () => (V)((V)R.Length(R.Get(v_230, (V)"pages")) != (V)R.Length(R.Get(v_234, (V)"pages")))))) {
return R.Join(new V[] {(V)false}, new Results((V)"atlas does not cover the active published visuals"));
}
foreach (var row_235 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_230, (V)"pages"))))))) {
V v_236 = row_235[0];
V v_237 = row_235[1];
var values_238 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_234, (V)"pages"), v_236)));
V v_239 = values_238[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_237, (V)"width") != R.Get(v_239, (V)"width")), () => (V)(R.Get(v_237, (V)"height") != R.Get(v_239, (V)"height"))), () => (V)(R.Get(v_237, (V)"filter") != R.Get(v_239, (V)"filter"))), () => (V)(R.Get(v_237, (V)"group") != R.Get(v_239, (V)"group"))), () => (V)(R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_237, (V)"entries")))).First != R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"entries")))).First)))) {
return R.Join(new V[] {(V)false}, new Results((V)"atlas layout differs from deterministic source plan"));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_8, (V)"encode", new V((RuleFunction)(args_240 => {
V v_241 = args_240[0];
R.Call(R.Get(v_8, (V)"validate"), R.Join(new V[] {}, new Results(v_241)));
var values_242 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(v_241))));
V v_243 = values_242[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_243) <= (V)((V)1024.0d * (V)1024.0d))}, new Results((V)"atlas metadata too large")));
return R.Join(new V[] {}, new Results(v_243));
return Results.Empty;
})));
R.Set(v_8, (V)"decode", new V((RuleFunction)(args_244 => {
V v_245 = args_244[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_245))).First == (V)"string"), () => (V)((V)R.Length(v_245) <= (V)((V)1024.0d * (V)1024.0d)))}, new Results((V)"invalid atlas metadata size")));
var values_246 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"decode"), R.Join(new V[] {}, new Results(v_245))));
V v_247 = values_246[0];
R.Call(R.Get(v_8, (V)"validate"), R.Join(new V[] {}, new Results(v_247)));
return R.Join(new V[] {}, new Results(v_247));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_8));
return Results.Empty;
} }
