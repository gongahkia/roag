// Static C# port of src/studio_theme_store.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 25bddef2f813e34a02e67a23dfc330d6e5ae835bc9d24c4ef96a45d19f9e0ea5
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_theme_store { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_theme"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_project"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"paths", R.Table(new Entry((V)1, (V)"assets/studio/theme-a.pack"), new Entry((V)2, (V)"assets/studio/theme-b.pack"))))));
V v_6 = values_5[0];
V v_7 = V.Nil;
v_7 = new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
var values_10 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
V v_11 = values_10[0];
V v_12 = values_10[1];
{ double end_15 = R.Num((V)R.Length(v_9)), step_16 = R.Num((V)1.0d);
for (double i_14 = R.Num((V)1.0d); step_16 > 0 ? i_14 <= end_15 : i_14 >= end_15; i_14 += step_16) {
V v_13 = i_14;
var values_17 = R.Join(new V[] {}, new Results((V)((V)(v_11 + R.Invoke(v_9, "byte", R.Join(new V[] {}, new Results(v_13))).First) % (V)65521.0d)));
v_11 = values_17[0];

var values_18 = R.Join(new V[] {}, new Results((V)((V)(v_12 + v_11) % (V)65521.0d)));
v_12 = values_18[0];
}
}
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"format"), R.Join(new V[] {(V)"%08x"}, new Results((V)((V)(v_12 * (V)65536.0d) + v_11)))));
return Results.Empty;
}));
V v_19 = V.Nil;
v_19 = new V((RuleFunction)(args_20 => {
V v_21 = args_20[0];
V v_22 = args_20[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"valid_project_id"), R.Join(new V[] {}, new Results(v_21))).First}, new Results((V)"invalid project ID")));
return R.Join(new V[] {}, new Results(R.Concat((V)"studio/projects/", R.Concat(v_21, R.Concat((V)"/theme-", R.Concat(v_22, (V)".txt"))))));
return Results.Empty;
}));
V v_23 = V.Nil;
v_23 = new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
V v_26 = args_24[1];
V v_27 = args_24[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_25))).First == (V)"number"), () => (V)((V)(v_25 % (V)1.0d) == (V)0.0d)), () => (V)(v_25 >= (V)1.0d)), () => (V)(v_25 < (V)9007199254740991.0d)))));
var values_28 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"encode"), R.Join(new V[] {}, new Results(v_26))));
V v_29 = values_28[0];
return R.Join(new V[] {}, new Results(R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Table(new Entry((V)1, R.Concat((V)"ROAG-STUDIO-", R.Concat(v_27, (V)"/1"))), new Entry((V)2, v_25), new Entry((V)3, (V)R.Length(v_29)), new Entry(4, R.Call(v_7, R.Join(new V[] {}, new Results(v_29)))))}, new Results((V)"\n"))).First, R.Concat((V)"\n", v_29))));
return Results.Empty;
}));
V v_30 = V.Nil;
v_30 = new V((RuleFunction)(args_31 => {
V v_32 = args_31[0];
V v_33 = args_31[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_32))).First == (V)"string"), () => (V)((V)R.Length(v_32) <= (V)66000.0d))}, new Results((V)"invalid theme record size")));
var values_34 = R.Join(new V[] {}, R.Invoke(v_32, "match", R.Join(new V[] {}, new Results((V)"^([^\n]+)\n([^\n]+)\n([^\n]+)\n([^\n]+)\n(.*)$"))));
V v_35 = values_34[0];
V v_36 = values_34[1];
V v_37 = values_34[2];
V v_38 = values_34[3];
V v_39 = values_34[4];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_35 == R.Concat((V)"ROAG-STUDIO-", R.Concat(v_33, (V)"/1")))}, new Results((V)"incompatible theme record")));
var values_40 = R.Join(new V[] {R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_36))).First}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_37))));
v_36 = values_40[0];
v_37 = values_40[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_36, () => (V)((V)(v_36 % (V)1.0d) == (V)0.0d)), () => (V)(v_36 >= (V)1.0d)), () => (V)(v_36 < (V)9007199254740991.0d)), () => v_37), () => (V)((V)(v_37 % (V)1.0d) == (V)0.0d)), () => (V)(v_37 >= (V)0.0d)), () => (V)(v_37 <= (V)65536.0d)), () => (V)((V)R.Length(v_39) == v_37)), () => (V)(R.Call(v_7, R.Join(new V[] {}, new Results(v_39))).First == v_38))}, new Results((V)"corrupt theme record")));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"seq", v_36), new Entry((V)"payload", v_39))));
return Results.Empty;
}));
V v_41 = V.Nil;
v_41 = new V((RuleFunction)(args_42 => {
V v_43 = args_42[0];
V v_44 = args_42[1];
V v_45 = args_42[2];
V v_46 = args_42[3];
V v_47 = args_42[4];
var values_48 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_49 = values_48[0];
V v_50 = values_48[1];
foreach (var row_51 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_44)))))) {
V v_52 = row_51[0];
V v_53 = row_51[1];
var values_54 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_43}, new Results(v_53))));
V v_55 = values_54[0];
V v_56 = values_54[1];
if (R.Truth(R.And(v_55, () => v_56))) {
var values_57 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_30, v_56}, new Results(v_45))));
V v_58 = values_57[0];
V v_59 = values_57[1];
if (R.Truth(v_58)) {
var values_60 = R.Join(new V[] {}, new Results(v_52));
var obj_61 = v_59;
V key_62 = (V)"slot";
R.Set(obj_61, key_62, values_60[0]);

var values_63 = R.Join(new V[] {}, new Results(v_59));
var obj_64 = v_49;
V key_65 = (V)((V)R.Length(v_49) + (V)1.0d);
R.Set(obj_64, key_65, values_63[0]);
} else {
var values_66 = R.Join(new V[] {}, new Results((V)(v_50 + (V)1.0d)));
v_50 = values_66[0];
}
} else if (R.Truth((V)(!R.Truth(v_55)))) {
var values_67 = R.Join(new V[] {}, new Results((V)(v_50 + (V)1.0d)));
v_50 = values_67[0];
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_49}, new Results(new V((RuleFunction)(args_68 => {
V v_69 = args_68[0];
V v_70 = args_68[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_69, (V)"seq") > R.Get(v_70, (V)"seq"))));
return Results.Empty;
})))));
foreach (var row_71 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_49)))))) {
V v_72 = row_71[0];
V v_73 = row_71[1];
var values_74 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"decode")}, new Results(R.Get(v_73, (V)"payload")))));
V v_75 = values_74[0];
V v_76 = values_74[1];
if (R.Truth(R.And((V)(!R.Truth(v_75)), () => (V)(v_72 == (V)1.0d)))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"incompatible or invalid theme: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_76))).First)}, new Results(v_73));
}
if (R.Truth(v_75)) {
var values_77 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"validate"), v_76}, new Results(v_46))));
V v_78 = values_77[0];
V v_79 = values_77[1];
if (R.Truth(R.And(v_47, () => (V)(R.Get(v_76, (V)"project_id") != v_47)))) {
var values_80 = R.Join(new V[] {}, new Results((V)false));
v_78 = values_80[0];

var values_81 = R.Join(new V[] {}, new Results((V)"wrong project ID"));
v_79 = values_81[0];
}
if (R.Truth(R.And((V)(!R.Truth(v_78)), () => (V)(v_72 == (V)1.0d)))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"invalid theme: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_79))).First)}, new Results(v_73));
}
if (R.Truth(v_78)) {
return R.Join(new V[] {v_76, R.Or(R.And(R.Or((V)(v_72 > (V)1.0d), () => (V)(v_50 > (V)0.0d)), () => (V)"recovered previous theme generation"), () => (V)"loaded")}, new Results(v_73));
}
}
}
return R.Join(new V[] {V.Nil}, new Results(R.Or(R.And((V)(v_50 > (V)0.0d), () => (V)"corrupt theme"), () => (V)"absent")));
return Results.Empty;
}));
R.Set(v_6, (V)"new", new V((RuleFunction)(args_82 => {
V v_83 = args_82[0];
return R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"io", v_83))}, new Results(R.Table(new Entry((V)"__index", v_6))))));
return Results.Empty;
})));
R.Set(v_6, "open", new V((RuleFunction)(args_84 => {
V v_85 = args_84[0];
V v_86 = args_84[1];
V v_87 = args_84[2];
var values_88 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Call(v_19, R.Join(new V[] {v_86}, new Results((V)"a"))).First), new Entry(2, R.Call(v_19, R.Join(new V[] {v_86}, new Results((V)"b")))))));
V v_89 = values_88[0];
var values_92 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {new V((RuleFunction)(args_90 => {
V v_91 = args_90[0];
return R.Join(new V[] {}, R.Invoke(R.Get(v_85, (V)"io"), "read", R.Join(new V[] {}, new Results(v_91))));
return Results.Empty;
})), v_89, (V)"THEME-SOURCE", v_87}, new Results(v_86))));
V v_93 = values_92[0];
V v_94 = values_92[1];
V v_95 = values_92[2];
if (R.Truth((V)(v_94 == (V)"absent"))) {
var values_96 = R.Join(new V[] {}, new Results((V)"missing theme document"));
v_94 = values_96[0];
}
return R.Join(new V[] {v_93, v_94}, new Results(v_95));
return Results.Empty;
})));
R.Set(v_6, "save", new V((RuleFunction)(args_97 => {
V v_98 = args_97[0];
V v_99 = args_97[1];
V v_100 = args_97[2];
var values_101 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"validate"), v_99}, new Results(v_100))));
V v_102 = values_101[0];
V v_103 = values_101[1];
if (R.Truth((V)(!R.Truth(v_102)))) {
return R.Join(new V[] {(V)false}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_103))));
}
var values_104 = R.Join(new V[] {}, R.Invoke(v_98, "open", R.Join(new V[] {R.Get(v_99, (V)"project_id")}, new Results(v_100))));
V v_105 = values_104[0];
V v_106 = values_104[1];
V v_107 = values_104[2];
if (R.Truth(R.And(R.And((V)(!R.Truth(v_105)), () => (V)(v_106 != (V)"missing theme document")), () => (V)(v_106 != (V)"corrupt theme")))) {
return R.Join(new V[] {(V)false}, new Results(v_106));
}
var values_108 = R.Join(new V[] {}, new Results(R.Or(R.And(v_107, () => R.Or(R.And((V)(R.Get(v_107, (V)"slot") == (V)1.0d), () => (V)"b"), () => (V)"a")), () => (V)"a")));
V v_109 = values_108[0];
var values_110 = R.Join(new V[] {}, new Results(R.Or(R.And(v_107, () => (V)(R.Get(v_107, (V)"seq") + (V)1.0d)), () => (V)1.0d)));
V v_111 = values_110[0];
var values_112 = R.Join(new V[] {}, R.Call(v_23, R.Join(new V[] {v_111, v_99}, new Results((V)"THEME-SOURCE"))));
V v_113 = values_112[0];
var values_114 = R.Join(new V[] {}, R.Call(v_19, R.Join(new V[] {R.Get(v_99, (V)"project_id")}, new Results(v_109))));
V v_115 = values_114[0];
var values_116 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_98, (V)"io"), (V)"mkdir"), R.Get(v_98, (V)"io")}, new Results(R.Concat((V)"studio/projects/", R.Get(v_99, (V)"project_id"))))));
V v_117 = values_116[0];
V v_118 = values_116[1];
if (R.Truth((V)(!R.Truth(v_117)))) {
return R.Join(new V[] {(V)false}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_118))));
}
var values_119 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_98, (V)"io"), (V)"write"), R.Get(v_98, (V)"io"), v_115}, new Results(v_113))));
V v_120 = values_119[0];
V v_121 = values_119[1];
if (R.Truth(R.Or((V)(!R.Truth(v_120)), () => (V)(!R.Truth(v_121))))) {
return R.Join(new V[] {(V)false}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Or(v_121, () => (V)"theme write failed")))));
}
var values_122 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_98, (V)"io"), (V)"read"), R.Get(v_98, (V)"io")}, new Results(v_115))));
V v_123 = values_122[0];
V v_124 = values_122[1];
if (R.Truth(R.Or((V)(!R.Truth(v_123)), () => (V)(v_124 != v_113)))) {
return R.Join(new V[] {(V)false}, new Results((V)"theme read-back failed"));
}
var values_125 = R.Join(new V[] {}, R.Invoke(v_98, "open", R.Join(new V[] {R.Get(v_99, (V)"project_id")}, new Results(v_100))));
V v_126 = values_125[0];
V v_127 = values_125[1];
V v_128 = values_125[2];
if (R.Truth(R.Or((V)(!R.Truth(v_126)), () => (V)(R.Get(v_128, (V)"seq") != v_111)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"theme verification failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_127))).First)));
}
return R.Join(new V[] {(V)true}, new Results(v_111));
return Results.Empty;
})));
R.Set(v_6, (V)"inspect_published", new V((RuleFunction)(args_129 => {
V v_130 = args_129[0];
return R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_130, R.Get(v_6, (V)"paths")}, new Results((V)"PUBLISHED-THEME"))));
return Results.Empty;
})));
R.Set(v_6, (V)"plan_published", new V((RuleFunction)(args_131 => {
V v_132 = args_131[0];
V v_133 = args_131[1];
R.Call(R.Get(v_2, (V)"validate"), R.Join(new V[] {}, new Results(v_132)));
var values_134 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"inspect_published"), R.Join(new V[] {}, new Results(v_133))));
V v_135 = values_134[0];
V v_136 = values_134[1];
V v_137 = values_134[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or(v_135, () => (V)(v_136 == (V)"absent")), () => (V)(v_136 == (V)"corrupt theme"))}, new Results(R.Concat((V)"refusing to overwrite incompatible published theme: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_136))).First))));
var values_138 = R.Join(new V[] {}, new Results(R.Or(R.And(v_137, () => R.Or(R.And((V)(R.Get(v_137, (V)"slot") == (V)1.0d), () => (V)2.0d), () => (V)1.0d)), () => (V)1.0d)));
V v_139 = values_138[0];
var values_140 = R.Join(new V[] {}, new Results(R.Or(R.And(v_137, () => (V)(R.Get(v_137, (V)"seq") + (V)1.0d)), () => (V)1.0d)));
V v_141 = values_140[0];
var values_142 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"paths"), v_139)));
V v_143 = values_142[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", v_143), new Entry((V)"bytes", R.Call(v_23, R.Join(new V[] {v_141, v_132}, new Results((V)"PUBLISHED-THEME"))).First), new Entry((V)"seq", v_141), new Entry((V)"overwrite", (V)(R.Call(v_133, R.Join(new V[] {}, new Results(v_143))).First != V.Nil)))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
