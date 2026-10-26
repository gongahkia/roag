// Static C# port of src/profile.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 91f08a3238ab3cb9a92413021534ba280c3c5647bb7f103aa6ed1c33f8c11faa
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_profile { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievement_content"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table()));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results((V)"roag-profile/1"));
var obj_8 = v_6;
V key_9 = (V)"schema";
R.Set(obj_8, key_9, values_7[0]);
var values_10 = R.Join(new V[] {}, new Results((V)"roag-profile-record/1"));
var obj_11 = v_6;
V key_12 = (V)"format";
R.Set(obj_11, key_12, values_10[0]);
var values_13 = R.Join(new V[] {}, new Results(R.Get(v_4, (V)"version")));
var obj_14 = v_6;
V key_15 = (V)"content";
R.Set(obj_14, key_15, values_13[0]);
var values_16 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"profile-a.roag"), new Entry((V)2, (V)"profile-b.roag"))));
var obj_17 = v_6;
V key_18 = (V)"slots";
R.Set(obj_17, key_18, values_16[0]);
var values_19 = R.Join(new V[] {}, new Results((V)131072.0d));
var obj_20 = v_6;
V key_21 = (V)"max_bytes";
R.Set(obj_20, key_21, values_19[0]);
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
var values_25 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
V v_26 = values_25[0];
V v_27 = values_25[1];
{ double end_30 = R.Num((V)R.Length(v_24)), step_31 = R.Num((V)1.0d);
for (double i_29 = R.Num((V)1.0d); step_31 > 0 ? i_29 <= end_30 : i_29 >= end_30; i_29 += step_31) {
V v_28 = i_29;
var values_32 = R.Join(new V[] {}, new Results((V)((V)(v_26 + R.Invoke(v_24, "byte", R.Join(new V[] {}, new Results(v_28))).First) % (V)65521.0d)));
v_26 = values_32[0];

var values_33 = R.Join(new V[] {}, new Results((V)((V)(v_27 + v_26) % (V)65521.0d)));
v_27 = values_33[0];
}
}
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"format"), R.Join(new V[] {(V)"%08x"}, new Results((V)((V)(v_27 * (V)65536.0d) + v_26)))));
return Results.Empty;
}));
R.Set(v_6, (V)"fresh", new V((RuleFunction)(args_34 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", R.Get(v_6, (V)"schema")), new Entry((V)"content", R.Get(v_6, (V)"content")), new Entry((V)"next_run_serial", (V)1.0d), new Entry((V)"earned", R.Table()))));
return Results.Empty;
})));
R.Set(v_6, (V)"validate", new V((RuleFunction)(args_35 => {
V v_36 = args_35[0];
R.Call(R.Get(v_4, (V)"validate"), Results.Empty);
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_36))).First == (V)"table"), () => (V)(R.Get(v_36, (V)"schema") == R.Get(v_6, (V)"schema"))), () => (V)(R.Get(v_36, (V)"content") == R.Get(v_6, (V)"content")))}, new Results((V)"incompatible profile content")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_36, (V)"next_run_serial")))).First == (V)"number"), () => (V)((V)(R.Get(v_36, (V)"next_run_serial") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_36, (V)"next_run_serial") >= (V)1.0d)), () => (V)(R.Get(v_36, (V)"next_run_serial") < (V)9007199254740991.0d))}, new Results((V)"invalid profile run serial")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_36, (V)"earned")))).First == (V)"table")}, new Results((V)"invalid profile achievements")));
foreach (var row_37 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_36, (V)"earned"))))))) {
V v_38 = row_37[0];
V v_39 = row_37[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_38))).First, () => (V)(v_39 == (V)true))}, new Results((V)"invalid profile achievement ID")));
}
foreach (var row_40 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_36)))))) {
V v_41 = row_40[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or(R.Or((V)(v_41 == (V)"schema"), () => (V)(v_41 == (V)"content")), () => (V)(v_41 == (V)"next_run_serial")), () => (V)(v_41 == (V)"earned"))}, new Results((V)"invalid profile field")));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
V v_42 = V.Nil;
v_42 = new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
R.Call(R.Get(v_6, (V)"validate"), R.Join(new V[] {}, new Results(v_45)));
var values_46 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"encode"), R.Join(new V[] {}, new Results(v_45))));
V v_47 = values_46[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_47) <= R.Get(v_6, (V)"max_bytes"))}, new Results((V)"profile size limit")));
return R.Join(new V[] {}, new Results(R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Table(new Entry((V)1, R.Get(v_6, (V)"format")), new Entry((V)2, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_44))).First), new Entry((V)3, R.Get(v_6, (V)"schema")), new Entry((V)4, R.Get(v_6, (V)"content")), new Entry((V)5, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results((V)R.Length(v_47)))).First), new Entry(6, R.Call(v_22, R.Join(new V[] {}, new Results(v_47)))))}, new Results((V)"\n"))).First, R.Concat((V)"\n", v_47))));
return Results.Empty;
}));
V v_48 = V.Nil;
v_48 = new V((RuleFunction)(args_49 => {
V v_50 = args_49[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_50))).First == (V)"string"), () => (V)((V)R.Length(v_50) <= (V)(R.Get(v_6, (V)"max_bytes") + (V)256.0d)))}, new Results((V)"invalid profile record size")));
var values_51 = R.Join(new V[] {R.Table()}, new Results((V)1.0d));
V v_52 = values_51[0];
V v_53 = values_51[1];
{ double end_56 = R.Num((V)6.0d), step_57 = R.Num((V)1.0d);
for (double i_55 = R.Num((V)1.0d); step_57 > 0 ? i_55 <= end_56 : i_55 >= end_56; i_55 += step_57) {
V v_54 = i_55;
var values_58 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Invoke(v_50, "find", R.Join(new V[] {(V)"\n", v_53}, new Results((V)true))).First}, new Results((V)"truncated profile header"))));
V v_59 = values_58[0];
var values_60 = R.Join(new V[] {}, R.Invoke(v_50, "sub", R.Join(new V[] {v_53}, new Results((V)(v_59 - (V)1.0d)))));
var obj_61 = v_52;
V key_62 = v_54;
R.Set(obj_61, key_62, values_60[0]);

var values_63 = R.Join(new V[] {}, new Results((V)(v_59 + (V)1.0d)));
v_53 = values_63[0];
}
}
var values_64 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_52, (V)2.0d)))));
V v_65 = values_64[0];
var values_66 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_52, (V)5.0d)))));
V v_67 = values_66[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(v_65, () => (V)((V)(v_65 % (V)1.0d) == (V)0.0d)), () => (V)(v_65 >= (V)1.0d)), () => (V)(v_65 < (V)9007199254740991.0d))}, new Results((V)"invalid profile generation")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(v_67, () => (V)((V)(v_67 % (V)1.0d) == (V)0.0d)), () => (V)(v_67 >= (V)0.0d)), () => (V)(v_67 <= R.Get(v_6, (V)"max_bytes"))), () => (V)((V)((V)((V)R.Length(v_50) - v_53) + (V)1.0d) == v_67))}, new Results((V)"profile payload size mismatch")));
var values_68 = R.Join(new V[] {}, R.Invoke(v_50, "sub", R.Join(new V[] {}, new Results(v_53))));
V v_69 = values_68[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(v_22, R.Join(new V[] {}, new Results(v_69))).First == R.Get(v_52, (V)6.0d))}, new Results((V)"profile checksum mismatch")));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"format", R.Get(v_52, (V)1.0d)), new Entry((V)"sequence", v_65), new Entry((V)"schema", R.Get(v_52, (V)3.0d)), new Entry((V)"content", R.Get(v_52, (V)4.0d)), new Entry((V)"profile", R.Call(R.Get(v_2, (V)"decode"), R.Join(new V[] {}, new Results(v_69))).First))));
return Results.Empty;
}));
R.Set(v_6, (V)"new", new V((RuleFunction)(args_70 => {
V v_71 = args_70[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_71))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_71, (V)"read")))).First == (V)"function")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_71, (V)"write")))).First == (V)"function"))}, new Results((V)"invalid profile storage")));
return R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"storage", v_71))}, new Results(R.Table(new Entry((V)"__index", v_6))))));
return Results.Empty;
})));
R.Set(v_6, (V)"inspect", new V((RuleFunction)(args_72 => {
V v_73 = args_72[0];
var values_74 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_75 = values_74[0];
V v_76 = values_74[1];
foreach (var row_77 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"slots"))))))) {
V v_78 = row_77[0];
V v_79 = row_77[1];
var values_80 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_73, (V)"storage"), (V)"read"), R.Get(v_73, (V)"storage")}, new Results(v_79))));
V v_81 = values_80[0];
V v_82 = values_80[1];
if (R.Truth(R.And(v_81, () => (V)(v_82 != V.Nil)))) {
var values_83 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_48}, new Results(v_82))));
V v_84 = values_83[0];
V v_85 = values_83[1];
if (R.Truth(v_84)) {
var values_86 = R.Join(new V[] {}, new Results(v_78));
var obj_87 = v_85;
V key_88 = (V)"slot";
R.Set(obj_87, key_88, values_86[0]);

var values_89 = R.Join(new V[] {}, new Results(v_85));
var obj_90 = v_75;
V key_91 = (V)((V)R.Length(v_75) + (V)1.0d);
R.Set(obj_90, key_91, values_89[0]);
} else {
var values_92 = R.Join(new V[] {}, new Results((V)(v_76 + (V)1.0d)));
v_76 = values_92[0];
}
} else if (R.Truth((V)(!R.Truth(v_81)))) {
var values_93 = R.Join(new V[] {}, new Results((V)(v_76 + (V)1.0d)));
v_76 = values_93[0];
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_75}, new Results(new V((RuleFunction)(args_94 => {
V v_95 = args_94[0];
V v_96 = args_94[1];
if (R.Truth((V)(R.Get(v_95, (V)"sequence") != R.Get(v_96, (V)"sequence")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_95, (V)"sequence") > R.Get(v_96, (V)"sequence"))));
}
return R.Join(new V[] {}, new Results((V)(R.Get(v_95, (V)"slot") > R.Get(v_96, (V)"slot"))));
return Results.Empty;
})))));
var values_97 = R.Join(new V[] {}, new Results(R.Get(v_75, (V)1.0d)));
V v_98 = values_97[0];
if (R.Truth((V)(!R.Truth(v_98)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", R.Or(R.And((V)(v_76 > (V)0.0d), () => (V)"corrupt"), () => (V)"absent")), new Entry((V)"invalid", v_76))));
}
if (R.Truth(R.Or(R.Or((V)(R.Get(v_98, (V)"format") != R.Get(v_6, (V)"format")), () => (V)(R.Get(v_98, (V)"schema") != R.Get(v_6, (V)"schema"))), () => (V)(R.Get(v_98, (V)"content") != R.Get(v_6, (V)"content"))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"incompatible"), new Entry((V)"reason", (V)"Incompatible Profile"), new Entry((V)"record", v_98), new Entry((V)"invalid", v_76))));
}
var values_99 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_6, (V)"validate")}, new Results(R.Get(v_98, (V)"profile")))));
V v_100 = values_99[0];
V v_101 = values_99[1];
if (R.Truth((V)(!R.Truth(v_100)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", (V)"corrupt"), new Entry((V)"reason", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_101))).First), new Entry((V)"record", v_98), new Entry((V)"invalid", v_76))));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", R.Or(R.And((V)(v_76 > (V)0.0d), () => (V)"recovered"), () => (V)"active")), new Entry((V)"profile", R.Get(v_98, (V)"profile")), new Entry((V)"record", v_98), new Entry((V)"invalid", v_76))));
return Results.Empty;
})));
R.Set(v_6, (V)"save", new V((RuleFunction)(args_102 => {
V v_103 = args_102[0];
V v_104 = args_102[1];
var values_105 = R.Join(new V[] {}, R.Invoke(v_103, "inspect", Results.Empty));
V v_106 = values_105[0];
if (R.Truth(R.Or((V)(R.Get(v_106, (V)"status") == (V)"incompatible"), () => (V)(R.Get(v_106, (V)"status") == (V)"corrupt")))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"profile ", R.Concat(R.Get(v_106, (V)"status"), (V)"; progress was not overwritten"))));
}
var values_107 = R.Join(new V[] {}, new Results(R.Get(v_106, (V)"record")));
V v_108 = values_107[0];
var values_109 = R.Join(new V[] {}, new Results(R.Or(R.And(v_108, () => (V)((V)3.0d - R.Get(v_108, (V)"slot"))), () => (V)1.0d)));
V v_110 = values_109[0];
var values_111 = R.Join(new V[] {}, new Results(R.Or(R.And(v_108, () => (V)(R.Get(v_108, (V)"sequence") + (V)1.0d)), () => (V)1.0d)));
V v_112 = values_111[0];
var values_113 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_42, v_112}, new Results(v_104))));
V v_114 = values_113[0];
V v_115 = values_113[1];
if (R.Truth((V)(!R.Truth(v_114)))) {
return R.Join(new V[] {(V)false}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_115))));
}
var values_116 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"slots"), v_110)));
V v_117 = values_116[0];
var values_118 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_103, (V)"storage"), (V)"write"), R.Get(v_103, (V)"storage"), v_117}, new Results(v_115))));
V v_119 = values_118[0];
V v_120 = values_118[1];
if (R.Truth(R.Or((V)(!R.Truth(v_119)), () => (V)(!R.Truth(v_120))))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"profile write failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_120))).First)));
}
var values_121 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_103, (V)"storage"), (V)"read"), R.Get(v_103, (V)"storage")}, new Results(v_117))));
V v_122 = values_121[0];
V v_123 = values_121[1];
if (R.Truth(R.Or((V)(!R.Truth(v_122)), () => (V)(v_123 != v_115)))) {
return R.Join(new V[] {(V)false}, new Results((V)"profile read-back failed"));
}
var values_124 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_48}, new Results(v_123))));
V v_125 = values_124[0];
V v_126 = values_124[1];
if (R.Truth(R.Or((V)(!R.Truth(v_125)), () => (V)(R.Get(v_126, (V)"sequence") != v_112)))) {
return R.Join(new V[] {(V)false}, new Results((V)"profile verification failed"));
}
return R.Join(new V[] {(V)true}, new Results(v_112));
return Results.Empty;
})));
R.Set(v_6, (V)"allocate_run", new V((RuleFunction)(args_127 => {
V v_128 = args_127[0];
var values_129 = R.Join(new V[] {}, R.Invoke(v_128, "inspect", Results.Empty));
V v_130 = values_129[0];
if (R.Truth(R.Or((V)(R.Get(v_130, (V)"status") == (V)"corrupt"), () => (V)(R.Get(v_130, (V)"status") == (V)"incompatible")))) {
return R.Join(new V[] {V.Nil}, new Results(R.Or(R.Get(v_130, (V)"reason"), () => R.Get(v_130, (V)"status"))));
}
var values_131 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_130, (V)"profile"), () => R.Call(R.Get(v_6, (V)"fresh"), Results.Empty).First)));
V v_132 = values_131[0];
var values_133 = R.Join(new V[] {}, new Results(R.Get(v_132, (V)"next_run_serial")));
V v_134 = values_133[0];
var values_135 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"encode"), R.Join(new V[] {}, new Results(v_132))))));
V v_136 = values_135[0];
var values_137 = R.Join(new V[] {}, new Results((V)(v_134 + (V)1.0d)));
var obj_138 = v_136;
V key_139 = (V)"next_run_serial";
R.Set(obj_138, key_139, values_137[0]);
var values_140 = R.Join(new V[] {}, R.Invoke(v_128, "save", R.Join(new V[] {}, new Results(v_136))));
V v_141 = values_140[0];
V v_142 = values_140[1];
if (R.Truth((V)(!R.Truth(v_141)))) {
return R.Join(new V[] {V.Nil}, new Results(v_142));
}
var values_143 = R.Join(new V[] {}, R.Invoke(v_128, "save", R.Join(new V[] {}, new Results(v_136))));
v_141 = values_143[0];
v_142 = values_143[1];
if (R.Truth((V)(!R.Truth(v_141)))) {
return R.Join(new V[] {V.Nil}, new Results(R.Concat((V)"run ID backup failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_142))).First)));
}
return R.Join(new V[] {R.Invoke((V)"local-run/%d", "format", R.Join(new V[] {}, new Results(v_134))).First}, new Results(v_136));
return Results.Empty;
})));
R.Set(v_6, (V)"commit_awards", new V((RuleFunction)(args_144 => {
V v_145 = args_144[0];
V v_146 = args_144[1];
var values_147 = R.Join(new V[] {}, R.Invoke(v_145, "inspect", Results.Empty));
V v_148 = values_147[0];
if (R.Truth(R.Or((V)(R.Get(v_148, (V)"status") == (V)"corrupt"), () => (V)(R.Get(v_148, (V)"status") == (V)"incompatible")))) {
return R.Join(new V[] {(V)false}, new Results(R.Or(R.Get(v_148, (V)"reason"), () => R.Get(v_148, (V)"status"))));
}
var values_149 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_148, (V)"profile"), () => R.Call(R.Get(v_6, (V)"fresh"), Results.Empty).First)));
V v_150 = values_149[0];
var values_151 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"encode"), R.Join(new V[] {}, new Results(v_150))))));
V v_152 = values_151[0];
var values_153 = R.Join(new V[] {}, new Results((V)false));
V v_154 = values_153[0];
foreach (var row_155 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_146)))))) {
V v_156 = row_155[0];
V v_157 = row_155[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_156))).First)), () => (V)(v_157 != (V)true)))) {
return R.Join(new V[] {(V)false}, new Results((V)"invalid pending award"));
}
var values_158 = R.Join(new V[] {}, new Results((V)true));
v_154 = values_158[0];
var values_159 = R.Join(new V[] {}, new Results((V)true));
var obj_160 = R.Get(v_152, (V)"earned");
V key_161 = v_156;
R.Set(obj_160, key_161, values_159[0]);
}
if (R.Truth(v_154)) {
var values_162 = R.Join(new V[] {}, R.Invoke(v_145, "save", R.Join(new V[] {}, new Results(v_152))));
V v_163 = values_162[0];
V v_164 = values_162[1];
if (R.Truth((V)(!R.Truth(v_163)))) {
return R.Join(new V[] {(V)false}, new Results(v_164));
}
var values_165 = R.Join(new V[] {}, R.Invoke(v_145, "save", R.Join(new V[] {}, new Results(v_152))));
v_163 = values_165[0];
v_164 = values_165[1];
if (R.Truth((V)(!R.Truth(v_163)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"award backup failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_164))).First)));
}
}
return R.Join(new V[] {(V)true}, new Results(v_152));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
