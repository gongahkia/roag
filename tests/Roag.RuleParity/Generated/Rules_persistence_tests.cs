// Static C# port of tests/persistence_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: b2585ec69a7bce5593f32b08c30db36baf8d3213a7c22580278bf7f2ca8aa553
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_persistence_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_13 = values_12[0];
V v_14 = V.Nil;
v_14 = new V((RuleFunction)(args_15 => {

var values_16 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"fail", (V)false), new Entry((V)"truncate", (V)false))));
V v_17 = values_16[0];
R.Set(v_17, "read", new V((RuleFunction)(args_18 => {
V v_19 = args_18[0];
V v_20 = args_18[1];
return R.Join(new V[] {}, new Results(R.Get(v_19, v_20)));
return Results.Empty;
})));
R.Set(v_17, "write", new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
V v_23 = args_21[1];
V v_24 = args_21[2];
if (R.Truth(R.Get(v_22, (V)"fail"))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_25 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_22, (V)"truncate"), () => R.Invoke(v_24, "sub", R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_24) / (V)2.0d)))))).First), () => v_24)));
var obj_26 = v_22;
V key_27 = v_23;
R.Set(obj_26, key_27, values_25[0]);
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_17));
return Results.Empty;
}));
V v_28 = V.Nil;
v_28 = new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
foreach (var row_31 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_30, (V)"entities"))))))) {
V v_32 = row_31[0];
V v_33 = row_31[1];
if (R.Truth((V)(R.Get(v_33, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_9, (V)"remove_for_actor"), R.Join(new V[] {v_30}, new Results(R.Get(v_33, (V)"id"))));
}
}
return Results.Empty;
}));
V v_34 = V.Nil;
v_34 = new V((RuleFunction)(args_35 => {
V v_36 = args_35[0];
V v_37 = args_35[1];
var values_38 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_36))));
V v_39 = values_38[0];
var values_40 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_41 = values_40[0];
foreach (var row_42 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_41)))))) {
V v_43 = row_42[0];
V v_44 = row_42[1];
var values_45 = R.Join(new V[] {(V)(R.Get(R.Get(v_37, (V)"position"), (V)"x") + R.Get(v_44, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_37, (V)"position"), (V)"y") + R.Get(v_44, (V)2.0d))));
V v_46 = values_45[0];
V v_47 = values_45[1];
var values_48 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"walkable"), R.Join(new V[] {R.Get(v_36, (V)"generated_map"), v_46}, new Results(v_47))));
V v_49 = values_48[0];
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_36, (V)"entities"))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_52, (V)"id") != R.Get(v_39, (V)"id")), () => R.Get(v_52, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_52, (V)"position"), (V)"x") == v_46)), () => (V)(R.Get(R.Get(v_52, (V)"position"), (V)"y") == v_47)))) {
var values_53 = R.Join(new V[] {}, new Results((V)false));
v_49 = values_53[0];
}
}
if (R.Truth(v_49)) {
var values_54 = R.Join(new V[] {v_46}, new Results(v_47));
var obj_55 = R.Get(v_39, (V)"position");
V key_56 = (V)"x";
var obj_57 = R.Get(v_39, (V)"position");
V key_58 = (V)"y";
R.Set(obj_55, key_56, values_54[0]);
R.Set(obj_57, key_58, values_54[1]);
R.Call(R.Get(v_11, (V)"recompute"), R.Join(new V[] {}, new Results(v_36)));
return R.Join(new V[] {(V)(R.Get(R.Get(v_37, (V)"position"), (V)"x") - v_46)}, new Results((V)(R.Get(R.Get(v_37, (V)"position"), (V)"y") - v_47)));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no approach")));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"save codec is inert and preserves nested numeric keyed plain data"}, new Results(new V((RuleFunction)(args_59 => {

var values_60 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"empty", R.Table()), new Entry((V)"nested", R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)false), new Entry((V)3, R.Table(new Entry((V)"x", (V)"a\n\u0000b"), new Entry((V)"n", (-(V)0.125d)))))), new Entry((V)7.0d, (V)"seven"), new Entry((V)"price", (V)1.23e-10d))));
V v_61 = values_60[0];
R.Call(v_3, R.Join(new V[] {v_61}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_61)))))));
var values_62 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"X"), new Entry((V)2, (V)"T1:Z"), new Entry((V)3, (V)"S3:hi"), new Entry((V)4, (V)"D3:nan"), new Entry((V)5, (V)"T1:S1:aYjunk"))));
V v_63 = values_62[0];
foreach (var row_64 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_63)))))) {
V v_65 = row_64[0];
V v_66 = row_64[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"decode")}, new Results(v_66))).First)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"decode")}, R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"a"}, new Results((V)(R.Get(v_7, (V)"max_bytes") + (V)1.0d)))))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"encode")}, new Results(R.Table(new Entry((V)"f", new V((RuleFunction)(args_67 => {


return Results.Empty;
}))))))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"two generations select latest and recover a corrupt newest record"}, new Results(new V((RuleFunction)(args_68 => {

var values_69 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_70 = values_69[0];

var values_71 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_70))));
V v_72 = values_71[0];
var values_73 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)2.0d))));
V v_74 = values_73[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_72, "save", R.Join(new V[] {}, new Results(v_74)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_74}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_72, "save", R.Join(new V[] {}, new Results(v_74)))));
var values_75 = R.Join(new V[] {}, R.Invoke(v_72, "load", Results.Empty));
V v_76 = values_75[0];
V v_77 = values_75[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_77, (V)"status") == (V)"active"), () => (V)(R.Get(R.Get(v_77, (V)"record"), (V)"seq") == (V)2.0d)), () => (V)(R.Get(v_76, (V)"clock") == R.Get(v_74, (V)"clock"))))));
var values_78 = R.Join(new V[] {}, R.Invoke(R.Get(v_70, R.Get(R.Get(v_7, (V)"slots"), (V)2.0d)), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)50.0d))));
var obj_79 = v_70;
V key_80 = R.Get(R.Get(v_7, (V)"slots"), (V)2.0d);
R.Set(obj_79, key_80, values_78[0]);
var values_81 = R.Join(new V[] {}, R.Invoke(v_72, "load", Results.Empty));
v_76 = values_81[0];
v_77 = values_81[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_77, (V)"status") == (V)"recovered"), () => (V)(R.Get(v_76, (V)"clock") == (V)0.0d)))));
var values_82 = R.Join(new V[] {}, new Results((V)"broken"));
var obj_83 = v_70;
V key_84 = R.Get(R.Get(v_7, (V)"slots"), (V)1.0d);
R.Set(obj_83, key_84, values_82[0]);
var values_85 = R.Join(new V[] {}, R.Invoke(v_72, "load", Results.Empty));
v_76 = values_85[0];
v_77 = values_85[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_76 == V.Nil), () => (V)(R.Get(v_77, (V)"status") == (V)"corrupt")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"earlier room content saves remain loadable after room expansion"}, new Results(new V((RuleFunction)(args_86 => {

var values_87 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_88 = values_87[0];

var values_89 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_88))));
V v_90 = values_89[0];
var values_91 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)15.0d))));
V v_92 = values_91[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_90, "save", R.Join(new V[] {}, new Results(v_92)))));
var values_93 = R.Join(new V[] {}, R.Invoke(R.Get(v_88, R.Get(R.Get(v_7, (V)"slots"), (V)1.0d)), "gsub", R.Join(new V[] {R.Invoke(R.Get(v_7, (V)"compatibility"), "gsub", R.Join(new V[] {(V)"%W"}, new Results((V)"%%%0"))).First, R.Get(v_7, (V)"prior_room_compatibility")}, new Results((V)1.0d))));
V v_94 = values_93[0];
V v_95 = values_93[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_95 == (V)1.0d))));
var values_96 = R.Join(new V[] {}, new Results(v_94));
var obj_97 = v_88;
V key_98 = R.Get(R.Get(v_7, (V)"slots"), (V)1.0d);
R.Set(obj_97, key_98, values_96[0]);
var values_99 = R.Join(new V[] {}, R.Invoke(v_90, "load", Results.Empty));
V v_100 = values_99[0];
V v_101 = values_99[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_100, () => (V)(R.Get(v_101, (V)"status") == (V)"active")), () => (V)(R.Get(v_100, (V)"run_seed") == (V)15.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_90, "save", R.Join(new V[] {}, new Results(v_100)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_90, "load", Results.Empty)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"failed or truncated writes retain prior verified generation"}, new Results(new V((RuleFunction)(args_102 => {

var values_103 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_104 = values_103[0];

var values_105 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_104))));
V v_106 = values_105[0];
var values_107 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_108 = values_107[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_106, "save", R.Join(new V[] {}, new Results(v_108)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_108}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_109 = R.Join(new V[] {}, new Results((V)true));
var obj_110 = v_104;
V key_111 = (V)"fail";
R.Set(obj_110, key_111, values_109[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_106, "save", R.Join(new V[] {}, new Results(v_108))).First)))));
var values_112 = R.Join(new V[] {}, new Results((V)false));
var obj_113 = v_104;
V key_114 = (V)"fail";
R.Set(obj_113, key_114, values_112[0]);
var values_115 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_106, "load", Results.Empty))));
V v_116 = values_115[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_116, (V)"clock") == (V)0.0d))));
var values_117 = R.Join(new V[] {}, new Results((V)true));
var obj_118 = v_104;
V key_119 = (V)"truncate";
R.Set(obj_118, key_119, values_117[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_106, "save", R.Join(new V[] {}, new Results(v_108))).First)))));
var values_120 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_106, "load", Results.Empty))));
v_116 = values_120[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_116, (V)"clock") == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"New Run replacement writes inactive slot first and survives backup failure"}, new Results(new V((RuleFunction)(args_121 => {

var values_122 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_123 = values_122[0];

var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_123))));
V v_125 = values_124[0];
var values_126 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)12.0d))));
V v_127 = values_126[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_125, "save", R.Join(new V[] {}, new Results(v_127)))));
var values_128 = R.Join(new V[] {}, new Results(R.Get(v_123, (V)"write")));
V v_129 = values_128[0];
var values_130 = R.Join(new V[] {}, new Results((V)0.0d));
V v_131 = values_130[0];
R.Set(v_123, "write", new V((RuleFunction)(args_132 => {
V v_133 = args_132[0];
V v_134 = args_132[1];
V v_135 = args_132[2];
var values_136 = R.Join(new V[] {}, new Results((V)(v_131 + (V)1.0d)));
v_131 = values_136[0];
if (R.Truth((V)(v_131 == (V)1.0d))) {
return R.Join(new V[] {}, new Results((V)false));
}
return R.Join(new V[] {}, R.Call(v_129, R.Join(new V[] {v_133, v_134}, new Results(v_135))));
return Results.Empty;
})));
var values_137 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)13.0d))));
V v_138 = values_137[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_125, "replace", R.Join(new V[] {}, new Results(v_138))).First)))));
var values_139 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_125, "load", Results.Empty))));
V v_140 = values_139[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_140, (V)"run_seed") == (V)12.0d))));
var values_141 = R.Join(new V[] {}, new Results((V)0.0d));
v_131 = values_141[0];
R.Set(v_123, "write", new V((RuleFunction)(args_142 => {
V v_143 = args_142[0];
V v_144 = args_142[1];
V v_145 = args_142[2];
var values_146 = R.Join(new V[] {}, new Results((V)(v_131 + (V)1.0d)));
v_131 = values_146[0];
if (R.Truth((V)(v_131 == (V)2.0d))) {
return R.Join(new V[] {}, new Results((V)false));
}
return R.Join(new V[] {}, R.Call(v_129, R.Join(new V[] {v_143, v_144}, new Results(v_145))));
return Results.Empty;
})));
var values_147 = R.Join(new V[] {}, R.Invoke(v_125, "replace", R.Join(new V[] {}, new Results(v_138))));
V v_148 = values_147[0];
V v_149 = values_147[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_148, () => v_149), () => R.Invoke(v_149, "find", R.Join(new V[] {(V)"backup write failed", (V)1.0d}, new Results((V)true))).First))));
var values_150 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_125, "load", Results.Empty))));
V v_151 = values_150[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_151, (V)"run_seed") == (V)13.0d), () => R.Get(v_123, R.Get(R.Get(v_7, (V)"slots"), (V)1.0d))), () => R.Get(v_123, R.Get(R.Get(v_7, (V)"slots"), (V)2.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_125, "save", R.Join(new V[] {}, new Results(v_138)))));
var values_152 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_125, "load", Results.Empty))));
v_151 = values_152[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_151, (V)"run_seed") == (V)13.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"newest integrity-valid incompatible record blocks stale rollback"}, new Results(new V((RuleFunction)(args_153 => {

var values_154 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_155 = values_154[0];

var values_156 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_155))));
V v_157 = values_156[0];
var values_158 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)4.0d))));
V v_159 = values_158[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_157, "save", R.Join(new V[] {}, new Results(v_159)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_157, "save", R.Join(new V[] {}, new Results(v_159)))));
var values_160 = R.Join(new V[] {}, R.Invoke(R.Get(v_155, R.Get(R.Get(v_7, (V)"slots"), (V)2.0d)), "gsub", R.Join(new V[] {R.Invoke(R.Get(v_13, (V)"version"), "gsub", R.Join(new V[] {(V)"%W"}, new Results((V)"%%%0"))).First, (V)"roag-content/99"}, new Results((V)1.0d))));
var obj_161 = v_155;
V key_162 = R.Get(R.Get(v_7, (V)"slots"), (V)2.0d);
R.Set(obj_161, key_162, values_160[0]);
var values_163 = R.Join(new V[] {}, R.Invoke(v_157, "load", Results.Empty));
V v_164 = values_163[0];
V v_165 = values_163[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_164)), () => (V)(R.Get(v_165, (V)"status") == (V)"incompatible")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_157, "save", R.Join(new V[] {}, new Results(v_159))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_157, "replace", R.Join(new V[] {}, new Results(v_159)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_157, "load", Results.Empty)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"snapshot schema mismatch and unknown content are rejected before installation"}, new Results(new V((RuleFunction)(args_166 => {

var values_167 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_168 = values_167[0];

var values_169 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_168))));
V v_170 = values_169[0];
var values_171 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)4.0d))));
V v_172 = values_171[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_170, "save", R.Join(new V[] {}, new Results(v_172)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_170, "save", R.Join(new V[] {}, new Results(v_172)))));
var values_173 = R.Join(new V[] {}, R.Invoke(R.Get(v_168, R.Get(R.Get(v_7, (V)"slots"), (V)2.0d)), "gsub", R.Join(new V[] {R.Invoke(R.Get(v_5, (V)"snapshot_schema"), "gsub", R.Join(new V[] {(V)"%W"}, new Results((V)"%%%0"))).First, (V)"roag-run/99"}, new Results((V)1.0d))));
var obj_174 = v_168;
V key_175 = R.Get(R.Get(v_7, (V)"slots"), (V)2.0d);
R.Set(obj_174, key_175, values_173[0]);
var values_176 = R.Join(new V[] {}, R.Invoke(v_170, "load", Results.Empty));
V v_177 = values_176[0];
V v_178 = values_176[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_177)), () => (V)(R.Get(v_178, (V)"status") == (V)"incompatible")))));
var values_179 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_172))));
V v_180 = values_179[0];
var values_181 = Results.Empty;
V v_182 = values_181[0];
foreach (var row_183 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_172, (V)"entities"))))))) {
V v_184 = row_183[0];
V v_185 = row_183[1];
if (R.Truth((V)(R.Get(v_185, (V)"controller") == (V)"ai"))) {
var values_186 = R.Join(new V[] {}, new Results(v_185));
v_182 = values_186[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_182)));
var values_187 = R.Join(new V[] {}, new Results((V)"missing:enemy"));
var obj_188 = v_182;
V key_189 = (V)"definition_id";
R.Set(obj_188, key_189, values_187[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_170, "save", R.Join(new V[] {}, new Results(v_172))).First)))));
var values_190 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_180, (V)"entities"), R.Get(v_182, (V)"id")), (V)"definition_id")));
var obj_191 = v_182;
V key_192 = (V)"definition_id";
R.Set(obj_191, key_192, values_190[0]);
R.Call(v_3, R.Join(new V[] {v_180}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_172)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"terminal generation prevents older active save resurrection"}, new Results(new V((RuleFunction)(args_193 => {

var values_194 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_195 = values_194[0];

var values_196 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_195))));
V v_197 = values_196[0];
var values_198 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)5.0d))));
V v_199 = values_198[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_197, "save", R.Join(new V[] {}, new Results(v_199)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_197, "tombstone", Results.Empty)));
var values_200 = R.Join(new V[] {}, R.Invoke(v_197, "load", Results.Empty));
V v_201 = values_200[0];
V v_202 = values_200[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_201)), () => (V)(R.Get(v_202, (V)"status") == (V)"terminal")))));
var values_203 = R.Join(new V[] {}, new Results((V)"broken"));
var obj_204 = v_195;
V key_205 = R.Or(R.And((V)(R.Get(R.Get(v_202, (V)"record"), (V)"slot") == (V)1.0d), () => R.Get(R.Get(v_7, (V)"slots"), (V)1.0d)), () => R.Get(R.Get(v_7, (V)"slots"), (V)2.0d));
R.Set(obj_204, key_205, values_203[0]);
var values_206 = R.Join(new V[] {}, R.Invoke(v_197, "load", Results.Empty));
v_201 = values_206[0];
v_202 = values_206[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_201)), () => (V)(R.Get(v_202, (V)"status") == (V)"terminal")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_197, "replace", R.Join(new V[] {}, new Results(v_199)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_197, "load", Results.Empty)));
var values_207 = R.Join(new V[] {}, new Results((V)true));
var obj_208 = v_199;
V key_209 = (V)"game_over";
R.Set(obj_208, key_209, values_207[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_197, "save", R.Join(new V[] {}, new Results(v_199)))));
var values_210 = R.Join(new V[] {}, R.Invoke(v_197, "load", Results.Empty));
v_201 = values_210[0];
v_202 = values_210[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_201)), () => (V)(R.Get(v_202, (V)"status") == (V)"terminal")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"save and load consume no world time or gameplay RNG"}, new Results(new V((RuleFunction)(args_211 => {

var values_212 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_213 = values_212[0];

var values_214 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_213))));
V v_215 = values_214[0];
var values_216 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)6.0d))));
V v_217 = values_216[0];
var values_218 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_217))));
V v_219 = values_218[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_215, "save", R.Join(new V[] {}, new Results(v_217)))));
R.Call(v_3, R.Join(new V[] {v_219}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_217)))));
var values_220 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_213))).First, "load", Results.Empty))));
V v_221 = values_220[0];
R.Call(v_3, R.Join(new V[] {v_219}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_221)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"full bytes round trip preserves generated stage and deterministic continuation"}, new Results(new V((RuleFunction)(args_222 => {

var values_223 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_224 = values_223[0];

var values_225 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_224))));
V v_226 = values_225[0];
var values_227 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)8.0d))));
V v_228 = values_227[0];
var values_229 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_228))));
V v_230 = values_229[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_228, R.Get(v_230, (V)"id"), (V)"core:boon/crescent_reach", (V)"rare"}, new Results((V)2.0d)));
var values_231 = R.Join(new V[] {}, new Results((V)11.0d));
var obj_232 = v_228;
V key_233 = (V)"currency";
R.Set(obj_232, key_233, values_231[0]);
var values_234 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_235 = R.Get(v_228, (V)"progression");
V key_236 = (V)"level";
R.Set(obj_235, key_236, values_234[0]);
var values_237 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_238 = R.Get(v_228, (V)"progression");
V key_239 = (V)"xp_total";
R.Set(obj_238, key_239, values_237[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_226, "save", R.Join(new V[] {}, new Results(v_228)))));
var values_240 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_224))).First, "load", Results.Empty))));
V v_241 = values_240[0];
foreach (var row_242 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"wait")))))))))) {
V v_243 = row_242[0];
V v_244 = row_242[1];
var values_245 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_228}, new Results(v_244))));
V v_246 = values_245[0];
V v_247 = values_245[1];
V v_248 = values_245[2];
var values_249 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_241}, new Results(v_244))));
V v_250 = values_249[0];
V v_251 = values_249[1];
V v_252 = values_249[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_246 == v_250), () => (V)(v_247 == v_251)))));
R.Call(v_3, R.Join(new V[] {v_248}, new Results(v_252)));
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_228))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_241)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"save retains earned reward, cached chest, opened door, secret and fog"}, new Results(new V((RuleFunction)(args_253 => {

var values_254 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_255 = values_254[0];

var values_256 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_255))));
V v_257 = values_256[0];
var values_258 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_generated"), R.Join(new V[] {}, new Results((V)303.0d))));
V v_259 = values_258[0];
R.Call(v_28, R.Join(new V[] {}, new Results(v_259)));
var values_260 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_259))));
V v_261 = values_260[0];
var values_262 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_263 = values_262[0];
var values_264 = Results.Empty;
V v_265 = values_264[0];
foreach (var row_266 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_263)))))) {
V v_267 = row_266[0];
V v_268 = row_266[1];
var values_269 = R.Join(new V[] {(V)(R.Get(R.Get(v_261, (V)"position"), (V)"x") + R.Get(v_268, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_261, (V)"position"), (V)"y") + R.Get(v_268, (V)2.0d))));
V v_270 = values_269[0];
V v_271 = values_269[1];
if (R.Truth(R.Call(R.Get(v_13, (V)"walkable"), R.Join(new V[] {R.Get(v_259, (V)"generated_map"), v_270}, new Results(v_271))).First)) {
var values_272 = R.Join(new V[] {}, new Results((V)false));
V v_273 = values_272[0];
foreach (var row_274 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"entities"))))))) {
V v_275 = row_274[0];
V v_276 = row_274[1];
if (R.Truth(R.And(R.And(R.Get(v_276, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_276, (V)"position"), (V)"x") == v_270)), () => (V)(R.Get(R.Get(v_276, (V)"position"), (V)"y") == v_271)))) {
var values_277 = R.Join(new V[] {}, new Results((V)true));
v_273 = values_277[0];
}
}
if (R.Truth((V)(!R.Truth(v_273)))) {
var values_278 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_259, (V)"core:enemy/mossbound_guard", v_270}, new Results(v_271))));
v_265 = values_278[0];
R.Call(R.Get(v_9, (V)"remove_for_actor"), R.Join(new V[] {v_259}, new Results(R.Get(v_265, (V)"id"))));
var values_279 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_280 = v_265;
V key_281 = (V)"hp";
R.Set(obj_280, key_281, values_279[0]);
var values_282 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_268, (V)1.0d)), new Entry((V)"dy", R.Get(v_268, (V)2.0d)))))));
V v_283 = values_282[0];
V v_284 = values_282[1];
V v_285 = values_282[2];
var values_286 = R.Join(new V[] {}, new Results((V)false));
V v_287 = values_286[0];
foreach (var row_288 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_285)))))) {
V v_289 = row_288[0];
V v_290 = row_288[1];
if (R.Truth((V)(R.Get(v_290, (V)"kind") == (V)"EntityKilled"))) {
var values_291 = R.Join(new V[] {}, new Results((V)true));
v_287 = values_291[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_287, () => (V)(R.Get(R.Get(v_259, (V)"progression"), (V)"xp_total") > (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_268, (V)1.0d)), new Entry((V)"dy", R.Get(v_268, (V)2.0d))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_259, (V)"currency") >= (V)4.0d))));
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_265)));
var values_292 = Results.Empty;
V v_293 = values_292[0];
foreach (var row_294 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_263)))))) {
V v_295 = row_294[0];
V v_296 = row_294[1];
var values_297 = R.Join(new V[] {(V)(R.Get(R.Get(v_261, (V)"position"), (V)"x") + R.Get(v_296, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_261, (V)"position"), (V)"y") + R.Get(v_296, (V)2.0d))));
V v_298 = values_297[0];
V v_299 = values_297[1];
if (R.Truth(R.Call(R.Get(v_13, (V)"walkable"), R.Join(new V[] {R.Get(v_259, (V)"generated_map"), v_298}, new Results(v_299))).First)) {
var values_300 = R.Join(new V[] {}, new Results((V)false));
V v_301 = values_300[0];
foreach (var row_302 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"entities"))))))) {
V v_303 = row_302[0];
V v_304 = row_302[1];
if (R.Truth(R.And(R.And(R.Get(v_304, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_304, (V)"position"), (V)"x") == v_298)), () => (V)(R.Get(R.Get(v_304, (V)"position"), (V)"y") == v_299)))) {
var values_305 = R.Join(new V[] {}, new Results((V)true));
v_301 = values_305[0];
}
}
if (R.Truth((V)(!R.Truth(v_301)))) {
var values_306 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_259, (V)"core:enemy/ruin_skitter", v_298}, new Results(v_299))));
v_293 = values_306[0];
R.Call(R.Get(v_9, (V)"remove_for_actor"), R.Join(new V[] {v_259}, new Results(R.Get(v_293, (V)"id"))));
var values_307 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_308 = v_293;
V key_309 = (V)"hp";
R.Set(obj_308, key_309, values_307[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_296, (V)1.0d)), new Entry((V)"dy", R.Get(v_296, (V)2.0d))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_296, (V)1.0d)), new Entry((V)"dy", R.Get(v_296, (V)2.0d))))))));
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_293, () => (V)(R.Get(v_259, (V)"currency") >= (V)5.0d)))));
var values_310 = Results.Empty;
V v_311 = values_310[0];
foreach (var row_312 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"entities"))))))) {
V v_313 = row_312[0];
V v_314 = row_312[1];
if (R.Truth((V)(R.Get(v_314, (V)"controller") == (V)"door"))) {
var values_315 = R.Join(new V[] {}, new Results(v_314));
v_311 = values_315[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_311)));
var values_316 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_259}, new Results(v_311))));
V v_317 = values_316[0];
V v_318 = values_316[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_317), new Entry((V)"dy", v_318)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_311, (V)"open"))));
var values_319 = Results.Empty;
V v_320 = values_319[0];
foreach (var row_321 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"secrets"))))))) {
V v_322 = row_321[0];
V v_323 = row_321[1];
if (R.Truth(R.Get(v_323, (V)"wall_x"))) {
var values_324 = R.Join(new V[] {}, new Results(v_323));
v_320 = values_324[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_320)));
var values_325 = R.Join(new V[] {R.Get(v_320, (V)"x")}, new Results(R.Get(v_320, (V)"y")));
var obj_326 = R.Get(v_261, (V)"position");
V key_327 = (V)"x";
var obj_328 = R.Get(v_261, (V)"position");
V key_329 = (V)"y";
R.Set(obj_326, key_327, values_325[0]);
R.Set(obj_328, key_329, values_325[1]);
R.Call(R.Get(v_11, (V)"recompute"), R.Join(new V[] {}, new Results(v_259)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(R.Get(v_320, (V)"wall_x") - R.Get(v_320, (V)"x"))), new Entry((V)"dy", (V)(R.Get(v_320, (V)"wall_y") - R.Get(v_320, (V)"y")))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_320, (V)"revealed"), () => R.Get(v_320, (V)"reward_id")))));
var values_330 = Results.Empty;
V v_331 = values_330[0];
foreach (var row_332 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"entities"))))))) {
V v_333 = row_332[0];
V v_334 = row_332[1];
if (R.Truth(R.And((V)(R.Get(v_334, (V)"controller") == (V)"chest"), () => (V)(R.Get(v_334, (V)"price") > (V)0.0d)))) {
var values_335 = R.Join(new V[] {}, new Results(v_334));
v_331 = values_335[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_331)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_259, (V)"currency") >= R.Get(v_331, (V)"price")))));
R.Call(v_34, R.Join(new V[] {v_259}, new Results(v_331)));
var values_336 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_331, (V)"offers")))));
V v_337 = values_336[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_331, (V)"id")), new Entry((V)"offer_index", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_331, (V)"claimed"))));
var values_338 = Results.Empty;
V v_339 = values_338[0];
foreach (var row_340 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_263)))))) {
V v_341 = row_340[0];
V v_342 = row_340[1];
var values_343 = R.Join(new V[] {(V)(R.Get(R.Get(v_261, (V)"position"), (V)"x") + R.Get(v_342, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_261, (V)"position"), (V)"y") + R.Get(v_342, (V)2.0d))));
V v_344 = values_343[0];
V v_345 = values_343[1];
if (R.Truth(R.Call(R.Get(v_13, (V)"walkable"), R.Join(new V[] {R.Get(v_259, (V)"generated_map"), v_344}, new Results(v_345))).First)) {
var values_346 = R.Join(new V[] {}, new Results((V)false));
V v_347 = values_346[0];
foreach (var row_348 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"entities"))))))) {
V v_349 = row_348[0];
V v_350 = row_348[1];
if (R.Truth(R.And(R.And(R.Get(v_350, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_350, (V)"position"), (V)"x") == v_344)), () => (V)(R.Get(R.Get(v_350, (V)"position"), (V)"y") == v_345)))) {
var values_351 = R.Join(new V[] {}, new Results((V)true));
v_347 = values_351[0];
}
}
if (R.Truth((V)(!R.Truth(v_347)))) {
var values_352 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_259, (V)"core:enemy/ruin_skitter", v_344, v_345}, new Results((V)(R.Get(v_259, (V)"clock") + (V)1000.0d)))));
V v_353 = values_352[0];
R.Call(R.Get(v_9, (V)"remove_for_actor"), R.Join(new V[] {v_259}, new Results(R.Get(v_353, (V)"id"))));
var values_354 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_355 = v_353;
V key_356 = (V)"hp";
R.Set(obj_355, key_356, values_354[0]);
var values_357 = R.Join(new V[] {}, new Results(v_342));
v_339 = values_357[0];
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_339)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_257, "save", R.Join(new V[] {}, new Results(v_259)))));
var values_358 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_255))).First, "load", Results.Empty))));
V v_359 = values_358[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_259))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_359)))));
R.Call(v_3, R.Join(new V[] {v_337}, new Results(R.Get(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_359}, new Results(R.Get(v_331, (V)"id")))).First, (V)"offers"))));
var values_360 = Results.Empty;
V v_361 = values_360[0];
foreach (var row_362 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_359, (V)"secrets"))))))) {
V v_363 = row_362[0];
V v_364 = row_362[1];
if (R.Truth((V)(R.Get(v_364, (V)"id") == R.Get(v_320, (V)"id")))) {
var values_365 = R.Join(new V[] {}, new Results(v_364));
v_361 = values_365[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_359}, new Results(R.Get(v_311, (V)"id")))).First, (V)"open"), () => v_361), () => R.Get(v_361, (V)"revealed")), () => (V)(R.Get(v_361, (V)"reward_id") == R.Get(v_320, (V)"reward_id"))))));
var values_366 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_339, (V)1.0d)), new Entry((V)"dy", R.Get(v_339, (V)2.0d)))))));
V v_367 = values_366[0];
V v_368 = values_366[1];
V v_369 = values_366[2];
var values_370 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_359}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_339, (V)1.0d)), new Entry((V)"dy", R.Get(v_339, (V)2.0d)))))));
V v_371 = values_370[0];
V v_372 = values_370[1];
V v_373 = values_370[2];
R.Call(v_3, R.Join(new V[] {v_369}, new Results(v_373)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_259))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_359)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"delayed Temporal Echo keeps due time and ancestry through disk bytes"}, new Results(new V((RuleFunction)(args_374 => {

var values_375 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_376 = values_375[0];

var values_377 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_376))));
V v_378 = values_377[0];
var values_379 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)71.0d))));
V v_380 = values_379[0];
var values_381 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_380))));
V v_382 = values_381[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_380, R.Get(v_382, (V)"id"), (V)"core:boon/temporal_echo", (V)"common"}, new Results((V)1.0d)));
var values_383 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_380, (V)"core:enemy/thornspitter", (V)3.0d, (V)2.0d}, new Results((V)1000.0d))));
V v_384 = values_383[0];
R.Call(R.Get(v_9, (V)"remove_for_actor"), R.Join(new V[] {v_380}, new Results(R.Get(v_384, (V)"id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_380}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_380))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_378, "save", R.Join(new V[] {}, new Results(v_380)))));
var values_385 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_376))).First, "load", Results.Empty))));
V v_386 = values_385[0];
R.Call(v_3, R.Join(new V[] {R.Get(v_380, (V)"delayed_effects")}, new Results(R.Get(v_386, (V)"delayed_effects"))));
var values_387 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_380}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_388 = values_387[0];
V v_389 = values_387[1];
V v_390 = values_387[2];
var values_391 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_386}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_392 = values_391[0];
V v_393 = values_391[1];
V v_394 = values_391[2];
R.Call(v_3, R.Join(new V[] {v_390}, new Results(v_394)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_380))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_386)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stage two route and retired floor persist without regeneration"}, new Results(new V((RuleFunction)(args_395 => {

var values_396 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_397 = values_396[0];

var values_398 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_397))));
V v_399 = values_398[0];
var values_400 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_401 = values_400[0];
R.Call(v_28, R.Join(new V[] {}, new Results(v_401)));
var values_402 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_401}, new Results(R.Get(R.Get(v_401, (V)"stage"), (V)"elite_id")))));
V v_403 = values_402[0];
var values_404 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_401))));
V v_405 = values_404[0];
var values_406 = R.Join(new V[] {}, new Results((V)(R.Get(v_403, (V)"hp") + R.Get(v_403, (V)"defense"))));
var obj_407 = v_405;
V key_408 = (V)"damage";
R.Set(obj_407, key_408, values_406[0]);
var values_409 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_401}, new Results(v_403))));
V v_410 = values_409[0];
V v_411 = values_409[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_401}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_410), new Entry((V)"dy", v_411)))))));
var values_412 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_401}, new Results(R.Get(R.Get(v_401, (V)"stage"), (V)"exit_id")))));
V v_413 = values_412[0];
R.Call(v_34, R.Join(new V[] {v_401}, new Results(v_413)));
var values_414 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"route_options"), R.Join(new V[] {v_401}, new Results(R.Get(v_413, (V)"id")))))));
V v_415 = values_414[0];
var values_416 = R.Join(new V[] {}, new Results(R.Get(v_401, (V)"clock")));
V v_417 = values_416[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_399, "save", R.Join(new V[] {}, new Results(v_401)))));
var values_418 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_397))).First, "load", Results.Empty))));
V v_419 = values_418[0];
R.Call(v_3, R.Join(new V[] {v_415}, R.Call(R.Get(v_5, (V)"route_options"), R.Join(new V[] {v_419}, new Results(R.Get(v_413, (V)"id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_419, (V)"clock") == v_417))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_401}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_413, (V)"id")), new Entry((V)"route_index", (V)2.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_401, (V)"stage"), (V)"index") == (V)2.0d), () => (V)((V)R.Length(R.Get(v_401, (V)"retired_stages")) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_399, "save", R.Join(new V[] {}, new Results(v_401)))));
var values_420 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_397))).First, "load", Results.Empty))));
V v_421 = values_420[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_401))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_421)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Get(v_421, (V)"retired_stages"), (V)1.0d), (V)"stage"), (V)"phase") == (V)"transition"))));
var values_422 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_401}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_423 = values_422[0];
V v_424 = values_422[1];
V v_425 = values_422[2];
var values_426 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_421}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_427 = values_426[0];
V v_428 = values_426[1];
V v_429 = values_426[2];
R.Call(v_3, R.Join(new V[] {v_425}, new Results(v_429)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_401))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_421)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"unclaimed chest menu reload retains three cached offers and one debit"}, new Results(new V((RuleFunction)(args_430 => {

var values_431 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_432 = values_431[0];

var values_433 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_432))));
V v_434 = values_433[0];
var values_435 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_generated"), R.Join(new V[] {}, new Results((V)21.0d))));
V v_436 = values_435[0];
R.Call(v_28, R.Join(new V[] {}, new Results(v_436)));
var values_437 = Results.Empty;
V v_438 = values_437[0];
foreach (var row_439 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_436, (V)"entities"))))))) {
V v_440 = row_439[0];
V v_441 = row_439[1];
if (R.Truth(R.And((V)(R.Get(v_441, (V)"controller") == (V)"chest"), () => (V)(R.Get(v_441, (V)"price") > (V)0.0d)))) {
var values_442 = R.Join(new V[] {}, new Results(v_441));
v_438 = values_442[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_438)));
var values_443 = R.Join(new V[] {}, new Results((V)(R.Get(v_438, (V)"price") + (V)3.0d)));
var obj_444 = v_436;
V key_445 = (V)"currency";
R.Set(obj_444, key_445, values_443[0]);
R.Call(v_34, R.Join(new V[] {v_436}, new Results(v_438)));
var values_446 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_436))));
V v_447 = values_446[0];
var values_448 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"chest_options"), R.Join(new V[] {v_436}, new Results(R.Get(v_438, (V)"id")))))));
V v_449 = values_448[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_434, "save", R.Join(new V[] {}, new Results(v_436)))));
var values_450 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_432))).First, "load", Results.Empty))));
V v_451 = values_450[0];
R.Call(v_3, R.Join(new V[] {v_447}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_451)))));
R.Call(v_3, R.Join(new V[] {v_449}, R.Call(R.Get(v_5, (V)"chest_options"), R.Join(new V[] {v_451}, new Results(R.Get(v_438, (V)"id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_451}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_438, (V)"id")), new Entry((V)"offer_index", (V)2.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_451, (V)"currency") == (V)3.0d), () => R.Get(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_451}, new Results(R.Get(v_438, (V)"id")))).First, (V)"claimed")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_451}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_438, (V)"id")), new Entry((V)"offer_index", (V)2.0d))))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_451, (V)"currency") == (V)3.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pending Warden telegraph and extraction deadline resume once"}, new Results(new V((RuleFunction)(args_452 => {

var values_453 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_454 = values_453[0];

var values_455 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_454))));
V v_456 = values_455[0];
var values_457 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)11.0d))));
V v_458 = values_457[0];
R.Call(v_28, R.Join(new V[] {}, new Results(v_458)));
var values_459 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_458}, new Results(R.Get(R.Get(v_458, (V)"stage"), (V)"elite_id")))));
V v_460 = values_459[0];
var values_461 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_458))));
V v_462 = values_461[0];
var values_463 = R.Join(new V[] {(V)1000.0d, (V)1000.0d, (V)100.0d}, new Results((V)200.0d));
var obj_464 = v_462;
V key_465 = (V)"hp";
var obj_466 = v_462;
V key_467 = (V)"max_hp";
var obj_468 = v_462;
V key_469 = (V)"defense";
var obj_470 = v_462;
V key_471 = (V)"speed";
R.Set(obj_464, key_465, values_463[0]);
R.Set(obj_466, key_467, values_463[1]);
R.Set(obj_468, key_469, values_463[2]);
R.Set(obj_470, key_471, values_463[3]);
R.Call(v_34, R.Join(new V[] {v_458}, new Results(v_460)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_458}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_460, (V)"pending_attack"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_456, "save", R.Join(new V[] {}, new Results(v_458)))));
var values_472 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_454))).First, "load", Results.Empty))));
V v_473 = values_472[0];
R.Call(v_3, R.Join(new V[] {R.Get(v_460, (V)"pending_attack")}, new Results(R.Get(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_473}, new Results(R.Get(v_460, (V)"id")))).First, (V)"pending_attack"))));
var values_474 = R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)3.0d}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_458}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))))).First}, R.Call(rt.Global("select"), R.Join(new V[] {(V)3.0d}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_473}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))))));
V v_475 = values_474[0];
V v_476 = values_474[1];
R.Call(v_3, R.Join(new V[] {v_475}, new Results(v_476)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_458))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_473)))));
R.Call(v_28, R.Join(new V[] {}, new Results(v_458)));
var values_477 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_458}, new Results(R.Get(R.Get(v_458, (V)"stage"), (V)"elite_id")))));
v_460 = values_477[0];
var values_478 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_458}, new Results(v_460))));
V v_479 = values_478[0];
V v_480 = values_478[1];
var values_481 = R.Join(new V[] {}, new Results((V)(R.Get(v_460, (V)"hp") + R.Get(v_460, (V)"defense"))));
var obj_482 = v_462;
V key_483 = (V)"damage";
R.Set(obj_482, key_483, values_481[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_458}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_479), new Entry((V)"dy", v_480)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_458, (V)"stage"), (V)"phase") == (V)"extraction"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_456, "save", R.Join(new V[] {}, new Results(v_458)))));
var values_484 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_454))).First, "load", Results.Empty))));
v_473 = values_484[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_473, (V)"stage"), (V)"deadline") == R.Get(R.Get(v_458, (V)"stage"), (V)"deadline")))));
while (R.Truth((V)(R.Get(v_458, (V)"clock") < R.Get(R.Get(v_458, (V)"stage"), (V)"deadline")))) {
var values_485 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_458}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_486 = values_485[0];
V v_487 = values_485[1];
V v_488 = values_485[2];
var values_489 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_473}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_490 = values_489[0];
V v_491 = values_489[1];
V v_492 = values_489[2];
R.Call(v_3, R.Join(new V[] {v_488}, new Results(v_492)));
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_458))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_473)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_458, (V)"stage"), (V)"phase") == (V)"pursuit"), () => R.Get(R.Get(v_458, (V)"stage"), (V)"pursuer_id")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pressure timer and RNG continue through a save before reinforcement"}, new Results(new V((RuleFunction)(args_493 => {

var values_494 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_495 = values_494[0];

var values_496 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_495))));
V v_497 = values_496[0];
var values_498 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)18.0d))));
V v_499 = values_498[0];
var values_500 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_499))));
V v_501 = values_500[0];
var values_502 = R.Join(new V[] {(V)1000.0d, (V)1000.0d}, new Results((V)100.0d));
var obj_503 = v_501;
V key_504 = (V)"hp";
var obj_505 = v_501;
V key_506 = (V)"max_hp";
var obj_507 = v_501;
V key_508 = (V)"defense";
R.Set(obj_503, key_504, values_502[0]);
R.Set(obj_505, key_506, values_502[1]);
R.Set(obj_507, key_508, values_502[2]);
while (R.Truth((V)(R.Get(v_499, (V)"clock") < (V)1100.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_499}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_499, (V)"stage"), (V)"next_pressure") >= R.Get(v_499, (V)"clock")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_497, "save", R.Join(new V[] {}, new Results(v_499)))));
var values_509 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_495))).First, "load", Results.Empty))));
V v_510 = values_509[0];
R.Call(v_3, R.Join(new V[] {R.Get(R.Get(v_499, (V)"stage"), (V)"pressure_rng")}, new Results(R.Get(R.Get(v_510, (V)"stage"), (V)"pressure_rng"))));
{ double end_513 = R.Num((V)4.0d), step_514 = R.Num((V)1.0d);
for (double i_512 = R.Num((V)1.0d); step_514 > 0 ? i_512 <= end_513 : i_512 >= end_513; i_512 += step_514) {
V v_511 = i_512;
var values_515 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_499}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_516 = values_515[0];
V v_517 = values_515[1];
V v_518 = values_515[2];
var values_519 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_510}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_520 = values_519[0];
V v_521 = values_519[1];
V v_522 = values_519[2];
R.Call(v_3, R.Join(new V[] {v_518}, new Results(v_522)));
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_499))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_510)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real filesystem adapter survives fresh service and state object"}, new Results(new V((RuleFunction)(args_523 => {

var values_524 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("os"), (V)"tmpname"), Results.Empty));
V v_525 = values_524[0];
R.Call(R.Get(rt.Global("os"), (V)"remove"), R.Join(new V[] {}, new Results(v_525)));
var values_526 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("os"), (V)"getenv"), R.Join(new V[] {}, new Results((V)"TMPDIR"))));
V v_527 = values_526[0];
var values_528 = R.Join(new V[] {}, new Results(R.Or(R.And(v_527, () => R.Concat(v_527, R.Concat((V)"/", R.Invoke(v_525, "match", R.Join(new V[] {}, new Results((V)"[^/]+$"))).First))), () => v_525)));
V v_529 = values_528[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("os"), (V)"execute"), R.Join(new V[] {}, new Results(R.Concat((V)"mkdir -p ", v_529)))).First == (V)0.0d))));
var values_530 = R.Join(new V[] {}, new Results(R.Table()));
V v_531 = values_530[0];
R.Set(v_531, "read", new V((RuleFunction)(args_532 => {
V v_533 = args_532[0];
V v_534 = args_532[1];
var values_535 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {R.Concat(v_529, R.Concat((V)"/", v_534))}, new Results((V)"rb"))));
V v_536 = values_535[0];
if (R.Truth((V)(!R.Truth(v_536)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_537 = R.Join(new V[] {}, R.Invoke(v_536, "read", R.Join(new V[] {}, new Results((V)"*a"))));
V v_538 = values_537[0];

R.Invoke(v_536, "close", Results.Empty);

return R.Join(new V[] {}, new Results(v_538));
return Results.Empty;
})));
R.Set(v_531, "write", new V((RuleFunction)(args_539 => {
V v_540 = args_539[0];
V v_541 = args_539[1];
V v_542 = args_539[2];
var values_543 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {R.Concat(v_529, R.Concat((V)"/", v_541))}, new Results((V)"wb"))))));
V v_544 = values_543[0];
R.Invoke(v_544, "write", R.Join(new V[] {}, new Results(v_542)));

R.Invoke(v_544, "close", Results.Empty);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
var values_545 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)9.0d))));
V v_546 = values_545[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_531))).First, "save", R.Join(new V[] {}, new Results(v_546)))));
var values_547 = R.Join(new V[] {}, new Results(V.Nil));
v_546 = values_547[0];
var values_548 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_531))).First, "load", Results.Empty))));
V v_549 = values_548[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_549, (V)"run_seed") == (V)9.0d), () => (V)(R.Get(v_549, (V)"clock") == (V)0.0d)))));
R.Call(R.Get(rt.Global("os"), (V)"remove"), R.Join(new V[] {}, new Results(R.Concat(v_529, R.Concat((V)"/", R.Get(R.Get(v_7, (V)"slots"), (V)1.0d))))));

R.Call(R.Get(rt.Global("os"), (V)"remove"), R.Join(new V[] {}, new Results(R.Concat(v_529, R.Concat((V)"/", R.Get(R.Get(v_7, (V)"slots"), (V)2.0d))))));
R.Call(R.Get(rt.Global("os"), (V)"execute"), R.Join(new V[] {}, new Results(R.Concat((V)"rmdir ", v_529))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
