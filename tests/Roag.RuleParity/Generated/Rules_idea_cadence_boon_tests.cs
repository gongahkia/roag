// Static C# port of tests/idea_cadence_boon_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: d15a8e63ea6385a08f5eef9e9191a8968e07646894e4160e400840b2ab139485
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_cadence_boon_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"threefold_remedy"), new Entry((V)2, (V)"twin_seals"), new Entry((V)3, (V)"fourfold_ledger"), new Entry((V)4, (V)"stone_backstop"), new Entry((V)5, (V)"borrowed_guard"), new Entry((V)6, (V)"long_wick"), new Entry((V)7, (V)"passing_ember"), new Entry((V)8, (V)"purging_seal"))));
V v_21 = values_20[0];
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
V v_25 = args_23[1];
V v_26 = args_23[2];
V v_27 = args_23[3];
R.Call(R.Get(v_7, (V)"set_stacks"), R.Join(new V[] {R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_24))).First, R.Concat((V)"core:boon/", v_25), R.Or(v_26, () => (V)"rare")}, new Results(R.Or(v_27, () => (V)1.0d))));
return Results.Empty;
}));
V v_28 = V.Nil;
v_28 = new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
var values_32 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, (V)"core:map/scrolling_room"}, new Results(R.Table(new Entry((V)"class_id", v_31))))));
V v_33 = values_32[0];
var values_34 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_33))));
V v_35 = values_34[0];

var values_36 = R.Join(new V[] {(V)15.0d}, new Results((V)16.0d));
var obj_37 = R.Get(v_35, (V)"position");
V key_38 = (V)"x";
var obj_39 = R.Get(v_35, (V)"position");
V key_40 = (V)"y";
R.Set(obj_37, key_38, values_36[0]);
R.Set(obj_39, key_40, values_36[1]);
if (R.Truth(v_30)) {
R.Call(v_22, R.Join(new V[] {v_33}, new Results(v_30)));
}
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_33)));

return R.Join(new V[] {v_33}, new Results(v_35));
return Results.Empty;
}));
V v_41 = V.Nil;
v_41 = new V((RuleFunction)(args_42 => {
V v_43 = args_42[0];
V v_44 = args_42[1];
V v_45 = args_42[2];
V v_46 = args_42[3];
V v_47 = args_42[4];
var values_48 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_43))).First, (V)"position")));
V v_49 = values_48[0];
var values_50 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_43, R.Or(v_47, () => (V)"core:enemy/mossbound_guard"), (V)(R.Get(v_49, (V)"x") + v_44), (V)(R.Get(v_49, (V)"y") + v_45)}, new Results((V)100000.0d))));
V v_51 = values_50[0];
var values_52 = R.Join(new V[] {}, new Results(R.Or(v_46, () => (V)100.0d)));
var obj_53 = v_51;
V key_54 = (V)"hp";
R.Set(obj_53, key_54, values_52[0]);

var values_55 = R.Join(new V[] {}, new Results(R.Get(v_51, (V)"hp")));
var obj_56 = v_51;
V key_57 = (V)"max_hp";
R.Set(obj_56, key_57, values_55[0]);

return R.Join(new V[] {}, new Results(v_51));
return Results.Empty;
}));
V v_58 = V.Nil;
v_58 = new V((RuleFunction)(args_59 => {
V v_60 = args_59[0];
V v_61 = args_59[1];
V v_62 = args_59[2];
V v_63 = args_59[3];
var values_64 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_60}, new Results(R.Table(new Entry((V)"kind", R.Or(v_61, () => (V)"basic_attack")), new Entry((V)"dx", R.Or(v_62, () => (V)1.0d)), new Entry((V)"dy", R.Or(v_63, () => (V)0.0d)))))));
V v_65 = values_64[0];
V v_66 = values_64[1];
V v_67 = values_64[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_65}, new Results(v_66)));

return R.Join(new V[] {}, new Results(v_67));
return Results.Empty;
}));
V v_68 = V.Nil;
v_68 = new V((RuleFunction)(args_69 => {
V v_70 = args_69[0];
V v_71 = args_69[1];
V v_72 = args_69[2];
var values_73 = R.Join(new V[] {}, new Results((V)0.0d));
V v_74 = values_73[0];

foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_70)))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
if (R.Truth(R.And((V)(R.Get(v_77, (V)"kind") == v_71), () => R.Or((V)(!R.Truth(v_72)), () => (V)(R.Get(v_77, (V)"boon_id") == R.Concat((V)"core:boon/", v_72)))))) {
var values_78 = R.Join(new V[] {}, new Results((V)(v_74 + (V)1.0d)));
v_74 = values_78[0];
}
}

return R.Join(new V[] {}, new Results(v_74));
return Results.Empty;
}));
V v_79 = V.Nil;
v_79 = new V((RuleFunction)(args_80 => {
V v_81 = args_80[0];
V v_82 = args_80[1];
V v_83 = args_80[2];
V v_84 = args_80[3];
var values_85 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_81, (V)"generated_map"), () => R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"map_for"), R.Join(new V[] {}, new Results(v_81))))))).First)));
var obj_86 = v_81;
V key_87 = (V)"generated_map";
R.Set(obj_86, key_87, values_85[0]);
var values_88 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_81, (V)"generated_map"), (V)"rows"), v_83)));
V v_89 = values_88[0];
var values_90 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_89, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_82 - (V)1.0d)))).First, R.Concat(v_84, R.Invoke(v_89, "sub", R.Join(new V[] {}, new Results((V)(v_82 + (V)1.0d)))).First))));
var obj_91 = R.Get(R.Get(v_81, (V)"generated_map"), (V)"rows");
V key_92 = v_83;
R.Set(obj_91, key_92, values_90[0]);
return Results.Empty;
}));
V v_93 = V.Nil;
v_93 = new V((RuleFunction)(args_94 => {
V v_95 = args_94[0];
V v_96 = args_94[1];
return R.Join(new V[] {}, new Results(R.And(R.Get(v_95, (V)"boon_memory"), () => R.Get(R.Get(v_95, (V)"boon_memory"), R.Concat((V)"core:boon/", v_96)))));
return Results.Empty;
}));
V v_97 = V.Nil;
v_97 = new V((RuleFunction)(args_98 => {
V v_99 = args_98[0];
V v_100 = args_98[1];
R.Call(v_22, R.Join(new V[] {v_99, (V)"emberbrand"}, new Results((V)"common")));

R.Call(v_58, R.Join(new V[] {}, new Results(v_99)));
R.Call(v_22, R.Join(new V[] {v_99, (V)"emberbrand", (V)"common"}, new Results((V)0.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_100, (V)"statuses"), (V)"burning"))));
return Results.Empty;
}));
V v_101 = V.Nil;
v_101 = new V((RuleFunction)(args_102 => {
V v_103 = args_102[0];
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_105 = values_104[0];

var values_106 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_105))));
V v_107 = values_106[0];
var values_108 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_109 = R.Get(v_107, (V)"position");
V key_110 = (V)"x";
var obj_111 = R.Get(v_107, (V)"position");
V key_112 = (V)"y";
R.Set(obj_109, key_110, values_108[0]);
R.Set(obj_111, key_112, values_108[1]);

R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_105)));
var values_113 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_105, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)0.0d))));
V v_114 = values_113[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_105, R.Get(v_114, (V)"id"), (V)"core:boon/emberbrand", (V)"common"}, new Results((V)1.0d)));
R.Call(v_58, R.Join(new V[] {v_105}, new Results((V)"wait")));

R.Call(v_58, R.Join(new V[] {v_105}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_107, (V)"statuses"), (V)"burning"))));
R.Call(R.Get(v_19, (V)"remove_for_actor"), R.Join(new V[] {v_105}, new Results(R.Get(v_114, (V)"id"))));
foreach (var row_115 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_105, (V)"entities"))))))) {
V v_116 = row_115[0];
V v_117 = row_115[1];
if (R.Truth((V)(R.Get(v_117, (V)"id") == R.Get(v_114, (V)"id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_105, (V)"entities")}, new Results(v_116)));

break;
}
}
R.Call(v_22, R.Join(new V[] {v_105}, new Results(v_103)));

return R.Join(new V[] {v_105}, new Results(v_107));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"eight cadence/status boons validate distinct native icons and bounded extensions"}, new Results(new V((RuleFunction)(args_118 => {

var values_119 = R.Join(new V[] {}, new Results(R.Table()));
V v_120 = values_119[0];
foreach (var row_121 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_21)))))) {
V v_122 = row_121[0];
V v_123 = row_121[1];
var values_124 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results(R.Concat((V)"core:boon/", v_123)))))));
V v_125 = values_124[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_120, R.Get(R.Get(v_125, (V)"art"), (V)"index")))))));

var values_126 = R.Join(new V[] {}, new Results((V)true));
var obj_127 = v_120;
V key_128 = R.Get(R.Get(v_125, (V)"art"), (V)"index");
R.Set(obj_127, key_128, values_126[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))).First, (V)"boons"), R.Get(v_125, (V)"id")), (V)1.0d) == R.Get(R.Get(v_125, (V)"art"), (V)"index")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"validate"), Results.Empty)));
var values_129 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/long_wick"))))))));
V v_130 = values_129[0];

var values_131 = R.Join(new V[] {}, new Results((V)1000.0d));
var obj_132 = R.Get(R.Get(v_130, (V)"effects"), (V)1.0d);
V key_133 = (V)"cap";
R.Set(obj_132, key_133, values_131[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_9, (V)"validate"), R.Table(new Entry(R.Get(v_130, (V)"id"), v_130))}, new Results(R.Table(new Entry((V)1, R.Get(v_130, (V)"id")))))).First)))));
var values_134 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/fourfold_ledger"))))))));
v_130 = values_134[0];
var values_135 = R.Join(new V[] {}, new Results((V)"arbitrary_field"));
var obj_136 = R.Get(R.Get(R.Get(v_130, (V)"predicate"), (V)"all"), (V)3.0d);
V key_137 = (V)"field";
R.Set(obj_136, key_137, values_135[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_9, (V)"validate"), R.Table(new Entry(R.Get(v_130, (V)"id"), v_130))}, new Results(R.Table(new Entry((V)1, R.Get(v_130, (V)"id")))))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Threefold Remedy requires three different primary victims in the same committed attack"}, new Results(new V((RuleFunction)(args_138 => {

foreach (var row_139 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)3.0d)))))))) {
V v_140 = row_139[0];
V v_141 = row_139[1];
var values_142 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"threefold_remedy"}, new Results((V)"core:actor/berserker"))));
V v_143 = values_142[0];
V v_144 = values_142[1];

var values_145 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_146 = v_144;
V key_147 = (V)"hp";
R.Set(obj_146, key_147, values_145[0]);
foreach (var row_148 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_141 == (V)3.0d), () => R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))))), () => R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))))))))))) {
V v_149 = row_148[0];
V v_150 = row_148[1];
R.Call(v_41, R.Join(new V[] {v_143, R.Get(v_150, (V)1.0d)}, new Results(R.Get(v_150, (V)2.0d))));
}
var values_151 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {v_143}, new Results((V)"sweeping_slash"))));
V v_152 = values_151[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_144, (V)"hp") == (V)((V)10.0d + R.Or(R.And((V)(v_141 == (V)3.0d), () => (V)3.0d), () => (V)0.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {v_152, (V)"BoonActivated"}, new Results((V)"threefold_remedy"))).First == R.Or(R.And((V)(v_141 == (V)3.0d), () => (V)1.0d), () => (V)0.0d)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Threefold groups reset per causal chain and secondary Echo damage never fills them"}, new Results(new V((RuleFunction)(args_153 => {

var values_154 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"threefold_remedy"}, new Results((V)"core:actor/berserker"))));
V v_155 = values_154[0];
V v_156 = values_154[1];

var values_157 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_158 = v_156;
V key_159 = (V)"hp";
R.Set(obj_158, key_159, values_157[0]);
R.Call(v_41, R.Join(new V[] {v_155, (V)1.0d}, new Results((V)0.0d)));

R.Call(v_41, R.Join(new V[] {v_155, (V)1.0d}, new Results((V)1.0d)));

R.Call(v_22, R.Join(new V[] {v_155}, new Results((V)"temporal_echo")));
{ double end_162 = R.Num((V)3.0d), step_163 = R.Num((V)1.0d);
for (double i_161 = R.Num((V)1.0d); step_163 > 0 ? i_161 <= end_162 : i_161 >= end_162; i_161 += step_163) {
V v_160 = i_161;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {v_155}, new Results((V)"sweeping_slash"))).First, (V)"BoonActivated"}, new Results((V)"threefold_remedy"))).First == (V)0.0d))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_156, (V)"hp") == (V)10.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"six distinct Backswing victims form two remedies after the primary HP batch"}, new Results(new V((RuleFunction)(args_164 => {

var values_165 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"threefold_remedy"}, new Results((V)"core:actor/berserker"))));
V v_166 = values_165[0];
V v_167 = values_165[1];

var values_168 = R.Join(new V[] {}, new Results((V)(R.Get(v_167, (V)"max_hp") - (V)1.0d)));
var obj_169 = v_167;
V key_170 = (V)"hp";
R.Set(obj_169, key_170, values_168[0]);
R.Call(v_22, R.Join(new V[] {v_166}, new Results((V)"backswing")));
foreach (var row_171 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)6, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d)))))))))) {
V v_172 = row_171[0];
V v_173 = row_171[1];
R.Call(v_41, R.Join(new V[] {v_166, R.Get(v_173, (V)1.0d)}, new Results(R.Get(v_173, (V)2.0d))));
}
var values_174 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {v_166}, new Results((V)"sweeping_slash"))));
V v_175 = values_174[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {v_175, (V)"BoonActivated"}, new Results((V)"threefold_remedy"))).First == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_167, (V)"hp") == R.Get(v_167, (V)"max_hp")))));
var values_176 = Results.Empty;
V v_177 = values_176[0];
V v_178 = values_176[1];
foreach (var row_179 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_175)))))) {
V v_180 = row_179[0];
V v_181 = row_179[1];
if (R.Truth(R.And((V)(R.Get(v_181, (V)"kind") == (V)"DamageBatchResolved"), () => (V)(!R.Truth(v_177))))) {
var values_182 = R.Join(new V[] {}, new Results(v_180));
v_177 = values_182[0];
}
if (R.Truth(R.And((V)(R.Get(v_181, (V)"kind") == (V)"Healed"), () => (V)(!R.Truth(v_178))))) {
var values_183 = R.Join(new V[] {}, new Results(v_180));
v_178 = values_183[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_177, () => v_178), () => (V)(v_177 < v_178)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Twin Seals needs two rewarded kills in one chain and grants exactly one finite ward"}, new Results(new V((RuleFunction)(args_184 => {

var values_185 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"twin_seals"}, new Results((V)"core:actor/berserker"))));
V v_186 = values_185[0];
V v_187 = values_185[1];
R.Call(v_41, R.Join(new V[] {v_186, (V)1.0d, (V)0.0d}, new Results((V)1.0d)));

R.Call(v_41, R.Join(new V[] {v_186, (V)1.0d, (V)1.0d}, new Results((V)1.0d)));
var values_188 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {v_186}, new Results((V)"sweeping_slash"))));
V v_189 = values_188[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {v_189, (V)"BoonActivated"}, new Results((V)"twin_seals"))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(v_93, R.Join(new V[] {v_187}, new Results((V)"twin_seals"))).First, (V)"ward") == (V)4.0d), () => (V)(R.Get(R.Call(v_93, R.Join(new V[] {v_187}, new Results((V)"twin_seals"))).First, (V)"expires_at") == (V)300.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_7, (V)"consume_ward"), R.Join(new V[] {v_186, v_187}, new Results((V)3.0d))).First == (V)3.0d), () => (V)(R.Call(R.Get(v_7, (V)"consume_ward"), R.Join(new V[] {v_186, v_187}, new Results((V)3.0d))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"separate kills and revived reward-exempt victims cannot assemble Twin Seals"}, new Results(new V((RuleFunction)(args_190 => {

var values_191 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"twin_seals"))));
V v_192 = values_191[0];
V v_193 = values_191[1];

R.Call(v_41, R.Join(new V[] {v_192, (V)1.0d, (V)0.0d}, new Results((V)1.0d)));

R.Call(v_58, R.Join(new V[] {}, new Results(v_192)));
R.Call(v_41, R.Join(new V[] {v_192, (V)1.0d, (V)0.0d}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {}, new Results(v_192))).First, (V)"BoonActivated"}, new Results((V)"twin_seals"))).First == (V)0.0d))));
var values_194 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"twin_seals"}, new Results((V)"core:actor/berserker"))));
v_192 = values_194[0];
v_193 = values_194[1];
R.Call(v_41, R.Join(new V[] {v_192, (V)1.0d, (V)0.0d}, new Results((V)1.0d)));

var values_195 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_192, (V)1.0d, (V)1.0d}, new Results((V)1.0d))));
V v_196 = values_195[0];

var values_197 = R.Join(new V[] {}, new Results((V)true));
var obj_198 = v_196;
V key_199 = (V)"reward_exempt";
R.Set(obj_198, key_199, values_197[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {v_192}, new Results((V)"sweeping_slash"))).First, (V)"BoonActivated"}, new Results((V)"twin_seals"))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Fourfold Ledger counts four different real pickup procs without counting itself"}, new Results(new V((RuleFunction)(args_200 => {

var values_201 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"fourfold_ledger"))));
V v_202 = values_201[0];
V v_203 = values_201[1];

var values_204 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_205 = v_203;
V key_206 = (V)"hp";
R.Set(obj_205, key_206, values_204[0]);

var values_207 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_208 = v_203;
V key_209 = (V)"max_hp";
R.Set(obj_208, key_209, values_207[0]);
foreach (var row_210 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"beggars_cap"), new Entry((V)2, (V)"road_flask"), new Entry((V)3, (V)"ration_reserve"), new Entry((V)4, (V)"field_dressing")))))))) {
V v_211 = row_210[0];
V v_212 = row_210[1];
R.Call(v_22, R.Join(new V[] {v_202}, new Results(v_212)));
}
R.Call(R.Get(v_15, (V)"spawn_currency"), R.Join(new V[] {v_202, (V)1.0d, R.Get(v_202, (V)"active_floor_id"), (V)16.0d, (V)16.0d, new V((RuleFunction)(args_213 => {


return Results.Empty;
}))}, new Results(R.Get(v_203, (V)"id"))));
R.Call(R.Get(v_15, (V)"spawn_supply"), R.Join(new V[] {v_202, (V)"food", R.Get(v_202, (V)"active_floor_id"), (V)16.0d, (V)16.0d, new V((RuleFunction)(args_214 => {


return Results.Empty;
}))}, new Results(R.Get(v_203, (V)"id"))));
R.Call(R.Get(v_15, (V)"spawn_supply"), R.Join(new V[] {v_202, (V)"health", R.Get(v_202, (V)"active_floor_id"), (V)16.0d, (V)16.0d, new V((RuleFunction)(args_215 => {


return Results.Empty;
}))}, new Results(R.Get(v_203, (V)"id"))));
var values_216 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {v_202}, new Results((V)"move"))));
V v_217 = values_216[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {v_217, (V)"BoonActivated"}, new Results((V)"fourfold_ledger"))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_203, (V)"hp") == (V)22.0d), () => (V)(R.Call(v_68, R.Join(new V[] {v_217}, new Results((V)"BoonActivated"))).First == (V)5.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_202))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_202)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"repeated activations of one boon are one ledger entry and cannot recurse"}, new Results(new V((RuleFunction)(args_218 => {

var values_219 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"fourfold_ledger"}, new Results((V)"core:actor/berserker"))));
V v_220 = values_219[0];
V v_221 = values_219[1];

var values_222 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_223 = v_221;
V key_224 = (V)"hp";
R.Set(obj_223, key_224, values_222[0]);
R.Call(v_22, R.Join(new V[] {v_220}, new Results((V)"rooks_wake")));

R.Call(v_22, R.Join(new V[] {v_220}, new Results((V)"emberbrand")));

R.Call(v_22, R.Join(new V[] {v_220}, new Results((V)"linebreaker_pawn")));
R.Call(v_41, R.Join(new V[] {v_220, (V)1.0d}, new Results((-(V)1.0d))));

R.Call(v_41, R.Join(new V[] {v_220, (V)1.0d}, new Results((V)0.0d)));

R.Call(v_41, R.Join(new V[] {v_220, (V)1.0d}, new Results((V)1.0d)));
var values_225 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {v_220}, new Results((V)"sweeping_slash"))));
V v_226 = values_225[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_68, R.Join(new V[] {v_226}, new Results((V)"BoonActivated"))).First > (V)4.0d), () => (V)(R.Call(v_68, R.Join(new V[] {v_226, (V)"BoonActivated"}, new Results((V)"fourfold_ledger"))).First == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_221, (V)"hp") == (V)10.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Stone Backstop requires actual owner cardinal alignment and a blocking tile behind the victim"}, new Results(new V((RuleFunction)(args_227 => {

foreach (var row_228 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"open"), new Entry((V)2, (V)"wall"), new Entry((V)3, (V)"door"), new Entry((V)4, (V)"diagonal")))))))) {
V v_229 = row_228[0];
V v_230 = row_228[1];
var values_231 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"stone_backstop"))));
V v_232 = values_231[0];
V v_233 = values_231[1];

var values_234 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_232, (V)1.0d}, new Results(R.Or(R.And((V)(v_230 == (V)"diagonal"), () => (V)1.0d), () => (V)0.0d)))));
V v_235 = values_234[0];
if (R.Truth((V)(v_230 == (V)"wall"))) {
R.Call(v_79, R.Join(new V[] {v_232, (V)17.0d, (V)16.0d}, new Results((V)"#")));
} else if (R.Truth((V)(v_230 == (V)"door"))) {
R.Call(R.Get(v_5, (V)"spawn_door"), R.Join(new V[] {v_232, (V)17.0d}, new Results((V)16.0d)));
} else if (R.Truth((V)(v_230 == (V)"diagonal"))) {
R.Call(v_79, R.Join(new V[] {v_232, (V)17.0d, (V)18.0d}, new Results((V)"#")));
}
var values_236 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {v_232, (V)"basic_attack", (V)1.0d}, new Results(R.Or(R.And((V)(v_230 == (V)"diagonal"), () => (V)1.0d), () => (V)0.0d)))));
V v_237 = values_236[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {v_237, (V)"BoonActivated"}, new Results((V)"stone_backstop"))).First == R.Or(R.And(R.Or((V)(v_230 == (V)"wall"), () => (V)(v_230 == (V)"door")), () => (V)1.0d), () => (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_235, (V)"hp") == R.Or(R.And(R.Or((V)(v_230 == (V)"wall"), () => (V)(v_230 == (V)"door")), () => (V)94.0d), () => (V)97.0d)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a piercing Needle captures Backstop before the same attack destroys its barricade"}, new Results(new V((RuleFunction)(args_238 => {

var values_239 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"stone_backstop"}, new Results((V)"core:actor/ranger"))));
V v_240 = values_239[0];
V v_241 = values_239[1];

R.Call(v_22, R.Join(new V[] {v_240}, new Results((V)"needle_flight")));
var values_242 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_240, (V)1.0d}, new Results((V)0.0d))));
V v_243 = values_242[0];

R.Call(v_79, R.Join(new V[] {v_240, (V)17.0d, (V)16.0d}, new Results((V)"B")));
var values_244 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {}, new Results(v_240))));
V v_245 = values_244[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_68, R.Join(new V[] {v_245}, new Results((V)"TerrainBroken"))).First == (V)1.0d), () => (V)(R.Call(v_68, R.Join(new V[] {v_245, (V)"BoonActivated"}, new Results((V)"stone_backstop"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_243, (V)"hp") == (V)92.0d), () => (V)(R.Invoke(R.Get(R.Get(R.Get(v_240, (V)"generated_map"), (V)"rows"), (V)16.0d), "sub", R.Join(new V[] {(V)17.0d}, new Results((V)17.0d))).First == (V)".")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"unowned Backstop leaves original damage outcome metadata absent"}, new Results(new V((RuleFunction)(args_246 => {

var values_247 = R.Join(new V[] {}, R.Call(v_28, Results.Empty));
V v_248 = values_247[0];

R.Call(v_41, R.Join(new V[] {v_248, (V)1.0d}, new Results((V)0.0d)));

R.Call(v_79, R.Join(new V[] {v_248, (V)17.0d, (V)16.0d}, new Results((V)"#")));
foreach (var row_249 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {}, new Results(v_248)))))))) {
V v_250 = row_249[0];
V v_251 = row_249[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_251, (V)"wall_backed") == V.Nil))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Borrowed Guard triggers once on a fully blocked primary attack and has a real cooldown"}, new Results(new V((RuleFunction)(args_252 => {

var values_253 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"borrowed_guard"))));
V v_254 = values_253[0];
V v_255 = values_253[1];

var values_256 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_254, (V)1.0d}, new Results((V)0.0d))));
V v_257 = values_256[0];

var values_258 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_259 = v_257;
V key_260 = (V)"defense";
R.Set(obj_259, key_260, values_258[0]);
var values_261 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {}, new Results(v_254))));
V v_262 = values_261[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {v_262, (V)"BoonActivated"}, new Results((V)"borrowed_guard"))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(v_93, R.Join(new V[] {v_255}, new Results((V)"borrowed_guard"))).First, (V)"ward") == (V)3.0d), () => (V)(R.Get(R.Call(v_93, R.Join(new V[] {v_255}, new Results((V)"borrowed_guard"))).First, (V)"expires_at") == (V)300.0d)))));
R.Call(R.Get(v_7, (V)"consume_ward"), R.Join(new V[] {v_254, v_255}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {}, new Results(v_254))).First, (V)"BoonActivated"}, new Results((V)"borrowed_guard"))).First == (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {}, new Results(v_254))).First, (V)"BoonActivated"}, new Results((V)"borrowed_guard"))).First == (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {}, new Results(v_254))).First, (V)"BoonActivated"}, new Results((V)"borrowed_guard"))).First == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Borrowed Guard excludes bosses, exempt foes and ordinary positive hits"}, new Results(new V((RuleFunction)(args_263 => {

foreach (var row_264 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"boss"), new Entry((V)2, (V)"exempt"), new Entry((V)3, (V)"positive")))))))) {
V v_265 = row_264[0];
V v_266 = row_264[1];
var values_267 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"borrowed_guard"))));
V v_268 = values_267[0];
V v_269 = values_267[1];
var values_270 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_268, (V)1.0d, (V)0.0d, V.Nil}, new Results(R.Or(R.And((V)(v_266 == (V)"boss"), () => (V)"core:enemy/thorn_warden"), () => V.Nil)))));
V v_271 = values_270[0];
var values_272 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_266 == (V)"positive"), () => (V)0.0d), () => (V)100.0d)));
var obj_273 = v_271;
V key_274 = (V)"defense";
R.Set(obj_273, key_274, values_272[0]);
if (R.Truth((V)(v_266 == (V)"exempt"))) {
var values_275 = R.Join(new V[] {}, new Results((V)true));
var obj_276 = v_271;
V key_277 = (V)"reward_exempt";
R.Set(obj_276, key_277, values_275[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {}, new Results(v_268))).First, (V)"BoonActivated"}, new Results((V)"borrowed_guard"))).First == (V)0.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a blocked multi-target sweep creates one Borrowed Guard rather than one per victim"}, new Results(new V((RuleFunction)(args_278 => {

var values_279 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"borrowed_guard"}, new Results((V)"core:actor/berserker"))));
V v_280 = values_279[0];
V v_281 = values_279[1];
foreach (var row_282 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d)))))))))) {
V v_283 = row_282[0];
V v_284 = row_282[1];
var values_285 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_286 = R.Call(v_41, R.Join(new V[] {v_280, R.Get(v_284, (V)1.0d)}, new Results(R.Get(v_284, (V)2.0d)))).First;
V key_287 = (V)"defense";
R.Set(obj_286, key_287, values_285[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {v_280}, new Results((V)"sweeping_slash"))).First, (V)"BoonActivated"}, new Results((V)"borrowed_guard"))).First == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Long Wick extends real Burning without changing its power or first queued tick"}, new Results(new V((RuleFunction)(args_288 => {

var values_289 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"long_wick"))));
V v_290 = values_289[0];

var values_291 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_290, (V)1.0d}, new Results((V)0.0d))));
V v_292 = values_291[0];

R.Call(v_97, R.Join(new V[] {v_290}, new Results(v_292)));
var values_293 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_292, (V)"statuses"), (V)"burning")));
V v_294 = values_293[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_294, (V)"remaining") == (V)5.0d), () => (V)(R.Get(v_294, (V)"power") == (V)2.0d)), () => (V)(R.Get(v_294, (V)"interval") == (V)100.0d)), () => (V)(R.Get(v_294, (V)"next_due") == (V)200.0d)))));
var values_295 = Results.Empty;
V v_296 = values_295[0];

foreach (var row_297 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_290, (V)"schedule"))))))) {
V v_298 = row_297[0];
V v_299 = row_297[1];
if (R.Truth((V)(R.Get(v_299, (V)"id") == R.Get(v_294, (V)"schedule_id")))) {
var values_300 = R.Join(new V[] {}, new Results(v_299));
v_296 = values_300[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_296, () => (V)(R.Get(v_296, (V)"due") == R.Get(v_294, (V)"next_due"))))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_290))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_290)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"uncapped Wick stacks cap remaining ticks at eight and cannot amplify tick power"}, new Results(new V((RuleFunction)(args_301 => {

var values_302 = R.Join(new V[] {}, R.Call(v_28, Results.Empty));
V v_303 = values_302[0];

R.Call(v_22, R.Join(new V[] {v_303, (V)"long_wick", (V)"legendary"}, new Results((V)100.0d)));
var values_304 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_303, (V)1.0d}, new Results((V)0.0d))));
V v_305 = values_304[0];

R.Call(v_97, R.Join(new V[] {v_303}, new Results(v_305)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_305, (V)"statuses"), (V)"burning"), (V)"remaining") == (V)7.0d), () => (V)(R.Get(R.Get(R.Get(v_305, (V)"statuses"), (V)"burning"), (V)"power") == (V)2.0d)))));
var values_306 = R.Join(new V[] {}, new Results(R.Get(v_305, (V)"hp")));
V v_307 = values_306[0];

{ double end_310 = R.Num((V)7.0d), step_311 = R.Num((V)1.0d);
for (double i_309 = R.Num((V)1.0d); step_311 > 0 ? i_309 <= end_310 : i_309 >= end_310; i_309 += step_311) {
V v_308 = i_309;
R.Call(v_58, R.Join(new V[] {v_303}, new Results((V)"wait")));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(R.Get(v_305, (V)"statuses"), (V)"burning"))), () => (V)(R.Get(v_305, (V)"hp") == (V)(v_307 - (V)7.0d))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Passing Ember ignites the nearest clear foe after a real Burning victim dies"}, new Results(new V((RuleFunction)(args_312 => {

var values_313 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"passing_ember"))));
V v_314 = values_313[0];

var values_315 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_314, (V)1.0d}, new Results((V)0.0d))));
V v_316 = values_315[0];

R.Call(v_97, R.Join(new V[] {v_314}, new Results(v_316)));
var values_317 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_318 = v_316;
V key_319 = (V)"hp";
R.Set(obj_318, key_319, values_317[0]);

var values_320 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_314, (V)1.0d}, new Results((V)1.0d))));
V v_321 = values_320[0];

var values_322 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_314, (V)3.0d}, new Results((V)0.0d))));
V v_323 = values_322[0];
var values_324 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {}, new Results(v_314))));
V v_325 = values_324[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {v_325, (V)"BoonActivated"}, new Results((V)"passing_ember"))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_321, (V)"statuses"), (V)"burning"), () => (V)(!R.Truth(R.And(R.Get(v_323, (V)"statuses"), () => R.Get(R.Get(v_323, (V)"statuses"), (V)"burning"))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_321, (V)"statuses"), (V)"burning"), (V)"power") == (V)3.0d), () => (V)(R.Get(R.Get(R.Get(v_321, (V)"statuses"), (V)"burning"), (V)"remaining") == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Passing Ember breaks equal-distance ties by ID and cannot cross a closed door"}, new Results(new V((RuleFunction)(args_326 => {

var values_327 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"passing_ember"))));
V v_328 = values_327[0];

var values_329 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_328, (V)1.0d}, new Results((V)0.0d))));
V v_330 = values_329[0];

R.Call(v_97, R.Join(new V[] {v_328}, new Results(v_330)));

var values_331 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_332 = v_330;
V key_333 = (V)"hp";
R.Set(obj_332, key_333, values_331[0]);
var values_334 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_328, (V)2.0d}, new Results((V)0.0d))));
V v_335 = values_334[0];

var values_336 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_328, (V)1.0d}, new Results((V)1.0d))));
V v_337 = values_336[0];
R.Call(v_58, R.Join(new V[] {}, new Results(v_328)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_335, (V)"statuses"), (V)"burning"), () => (V)(!R.Truth(R.And(R.Get(v_337, (V)"statuses"), () => R.Get(R.Get(v_337, (V)"statuses"), (V)"burning"))))))));
var values_338 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"passing_ember"))));
v_328 = values_338[0];

var values_339 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_328, (V)1.0d}, new Results((V)0.0d))));
v_330 = values_339[0];

R.Call(v_97, R.Join(new V[] {v_328}, new Results(v_330)));

var values_340 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_341 = v_330;
V key_342 = (V)"hp";
R.Set(obj_341, key_342, values_340[0]);
var values_343 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_328, (V)3.0d}, new Results((V)0.0d))));
V v_344 = values_343[0];

R.Call(R.Get(v_5, (V)"spawn_door"), R.Join(new V[] {v_328, (V)17.0d}, new Results((V)16.0d)));
R.Call(v_58, R.Join(new V[] {}, new Results(v_328)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.And(R.Get(v_344, (V)"statuses"), () => R.Get(R.Get(v_344, (V)"statuses"), (V)"burning")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"unburned or exempt deaths never pass an ember"}, new Results(new V((RuleFunction)(args_345 => {

foreach (var row_346 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)false), new Entry((V)2, (V)true)))))))) {
V v_347 = row_346[0];
V v_348 = row_346[1];
var values_349 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"passing_ember"))));
V v_350 = values_349[0];

var values_351 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_350, (V)1.0d}, new Results((V)0.0d))));
V v_352 = values_351[0];
if (R.Truth(v_348)) {
R.Call(v_97, R.Join(new V[] {v_350}, new Results(v_352)));

var values_353 = R.Join(new V[] {}, new Results((V)true));
var obj_354 = v_352;
V key_355 = (V)"reward_exempt";
R.Set(obj_354, key_355, values_353[0]);
}
var values_356 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_357 = v_352;
V key_358 = (V)"hp";
R.Set(obj_357, key_358, values_356[0]);

var values_359 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_350, (V)1.0d}, new Results((V)1.0d))));
V v_360 = values_359[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_68, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {}, new Results(v_350))).First, (V)"BoonActivated"}, new Results((V)"passing_ember"))).First == (V)0.0d), () => (V)(!R.Truth(R.And(R.Get(v_360, (V)"statuses"), () => R.Get(R.Get(v_360, (V)"statuses"), (V)"burning"))))))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an inherited passed-fire kill cannot make an endless automatic ember chain"}, new Results(new V((RuleFunction)(args_361 => {

var values_362 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"passing_ember"))));
V v_363 = values_362[0];

var values_364 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_363, (V)1.0d}, new Results((V)0.0d))));
V v_365 = values_364[0];

R.Call(v_97, R.Join(new V[] {v_363}, new Results(v_365)));

var values_366 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_367 = v_365;
V key_368 = (V)"hp";
R.Set(obj_367, key_368, values_366[0]);
var values_369 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_363, (V)1.0d, (V)1.0d}, new Results((V)2.0d))));
V v_370 = values_369[0];

var values_371 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_363, (V)2.0d}, new Results((V)1.0d))));
V v_372 = values_371[0];
var values_373 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {}, new Results(v_363))));
V v_374 = values_373[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_370, (V)"hp") == (V)0.0d), () => (V)(!R.Truth(R.And(R.Get(v_372, (V)"statuses"), () => R.Get(R.Get(v_372, (V)"statuses"), (V)"burning"))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {v_374, (V)"BoonActivated"}, new Results((V)"passing_ember"))).First == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Purging Seal releases one real cleanse burst without self damage or an empty-try proc"}, new Results(new V((RuleFunction)(args_375 => {

var values_376 = R.Join(new V[] {}, R.Call(v_101, R.Join(new V[] {}, new Results((V)"purging_seal"))));
V v_377 = values_376[0];
V v_378 = values_376[1];

R.Call(v_22, R.Join(new V[] {v_377}, new Results((V)"field_dressing")));
var values_379 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_377, (V)"core:enemy/mossbound_guard", (V)6.0d, (V)5.0d}, new Results((V)100000.0d))));
V v_380 = values_379[0];

var values_381 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_382 = v_380;
V key_383 = (V)"hp";
R.Set(obj_382, key_383, values_381[0]);

var values_384 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_385 = v_380;
V key_386 = (V)"max_hp";
R.Set(obj_385, key_386, values_384[0]);
R.Call(R.Get(v_15, (V)"spawn_supply"), R.Join(new V[] {v_377, (V)"health", R.Get(v_377, (V)"active_floor_id"), (V)6.0d, (V)6.0d, new V((RuleFunction)(args_387 => {


return Results.Empty;
}))}, new Results(R.Get(v_378, (V)"id"))));
var values_388 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {v_377, (V)"move", (-(V)1.0d)}, new Results((V)0.0d))));
V v_389 = values_388[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(R.Get(v_378, (V)"statuses"), (V)"burning"))), () => (V)(R.Call(v_68, R.Join(new V[] {v_389, (V)"BoonActivated"}, new Results((V)"purging_seal"))).First == (V)1.0d)), () => (V)(R.Get(v_380, (V)"hp") == (V)98.0d)))));
R.Call(R.Get(v_15, (V)"spawn_supply"), R.Join(new V[] {v_377, (V)"health", R.Get(v_377, (V)"active_floor_id"), (V)7.0d, (V)6.0d, new V((RuleFunction)(args_390 => {


return Results.Empty;
}))}, new Results(R.Get(v_378, (V)"id"))));

var values_391 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_392 = v_378;
V key_393 = (V)"hp";
R.Set(obj_392, key_393, values_391[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_68, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {v_377, (V)"move", (V)1.0d}, new Results((V)0.0d))).First, (V)"BoonActivated"}, new Results((V)"purging_seal"))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"consuming enemy Burning with Cinder Stopper cannot trigger the owner Purging Seal"}, new Results(new V((RuleFunction)(args_394 => {

var values_395 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"purging_seal"))));
V v_396 = values_395[0];

var values_397 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {v_396, (V)1.0d}, new Results((V)0.0d))));
V v_398 = values_397[0];

R.Call(v_97, R.Join(new V[] {v_396}, new Results(v_398)));

R.Call(v_22, R.Join(new V[] {v_396}, new Results((V)"cinder_stopper")));
var values_399 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {}, new Results(v_396))));
V v_400 = values_399[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_68, R.Join(new V[] {v_400}, new Results((V)"StatusCleansed"))).First == (V)1.0d), () => (V)(R.Call(v_68, R.Join(new V[] {v_400, (V)"BoonActivated"}, new Results((V)"purging_seal"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"cadence inventory and extended queued status retain exact saved action continuation"}, new Results(new V((RuleFunction)(args_401 => {

var values_402 = R.Join(new V[] {}, R.Call(v_28, Results.Empty));
V v_403 = values_402[0];
V v_404 = values_402[1];

foreach (var row_405 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_21)))))) {
V v_406 = row_405[0];
V v_407 = row_405[1];
R.Call(v_22, R.Join(new V[] {v_403}, new Results(v_407)));
}
R.Call(v_22, R.Join(new V[] {v_403}, new Results((V)"emberbrand")));

R.Call(v_22, R.Join(new V[] {v_403}, new Results((V)"temporal_echo")));

R.Call(v_41, R.Join(new V[] {v_403, (V)1.0d}, new Results((V)0.0d)));

R.Call(v_41, R.Join(new V[] {v_403, (V)1.0d}, new Results((V)1.0d)));
var values_408 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_409 = v_404;
V key_410 = (V)"hp";
R.Set(obj_409, key_410, values_408[0]);

R.Call(v_58, R.Join(new V[] {}, new Results(v_403)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_403, (V)"delayed_effects"))))));
var values_411 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_403))))))))));
V v_412 = values_411[0];
foreach (var row_413 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"wait"), new Entry((V)2, (V)"basic_attack"), new Entry((V)3, (V)"wait"), new Entry((V)4, (V)"move"), new Entry((V)5, (V)"wait")))))))) {
V v_414 = row_413[0];
V v_415 = row_413[1];
R.Call(v_3, R.Join(new V[] {R.Call(v_58, R.Join(new V[] {v_403, v_415, (V)0.0d}, new Results((-(V)1.0d)))).First}, R.Call(v_58, R.Join(new V[] {v_412, v_415, (V)0.0d}, new Results((-(V)1.0d))))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_403))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_412)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"zero-gain healing stays authoritative but adds no empty playback beat"}, new Results(new V((RuleFunction)(args_416 => {

var values_417 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_418 = values_417[0];

var values_419 = R.Join(new V[] {}, R.Call(R.Get(v_418, (V)"new"), Results.Empty));
V v_420 = values_419[0];
var values_421 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"Healed"), new Entry((V)"actual_gain", (V)0.0d), new Entry((V)"requested", (V)3.0d), new Entry((V)"target_id", (V)1.0d))))));
V v_422 = values_421[0];
var values_423 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, new Results(v_422))));
V v_424 = values_423[0];

R.Call(R.Get(v_418, (V)"add"), R.Join(new V[] {v_420, v_422}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_420, (V)"items")) == (V)0.0d), () => (V)(v_424 == R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, new Results(v_422))).First)))));
R.Call(R.Get(v_418, (V)"add"), R.Join(new V[] {v_420, R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"Healed"), new Entry((V)"actual_gain", (V)1.0d), new Entry((V)"requested", (V)3.0d), new Entry((V)"target_id", (V)1.0d))))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_420, (V)"items")) == (V)1.0d))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
