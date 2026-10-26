// Static C# port of tests/idea_bone_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 01a3ae5e5076cc6b71114e77509a07b0217c108b0c6ce57ff6f843b341798a0e
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_bone_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_9 = values_8[0];
V v_10 = V.Nil;
v_10 = new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
var values_13 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", v_12))))));
V v_14 = values_13[0];

var values_15 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_14))));
V v_16 = values_15[0];

var values_17 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_18 = R.Get(v_16, (V)"position");
V key_19 = (V)"x";
var obj_20 = R.Get(v_16, (V)"position");
V key_21 = (V)"y";
R.Set(obj_18, key_19, values_17[0]);
R.Set(obj_20, key_21, values_17[1]);
var values_22 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_14, (V)"core:enemy/bone_splitter", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_23 = values_22[0];

var values_24 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_25 = v_23;
V key_26 = (V)"hp";
R.Set(obj_25, key_26, values_24[0]);
return R.Join(new V[] {v_14, v_16}, new Results(v_23));
return Results.Empty;
}));
V v_27 = V.Nil;
v_27 = new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
var values_30 = R.Join(new V[] {}, new Results(R.Table()));
V v_31 = values_30[0];

foreach (var row_32 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_29, (V)"entities"))))))) {
V v_33 = row_32[0];
V v_34 = row_32[1];
if (R.Truth(R.Get(v_34, (V)"fragment_of"))) {
var values_35 = R.Join(new V[] {}, new Results(v_34));
var obj_36 = v_31;
V key_37 = (V)((V)R.Length(v_31) + (V)1.0d);
R.Set(obj_36, key_37, values_35[0]);
}
}

return R.Join(new V[] {}, new Results(v_31));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Bone Splitter fragments appear only after the complete primary HP batch and do not inherit its damage"}, new Results(new V((RuleFunction)(args_38 => {

var values_39 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_40 = values_39[0];
V v_41 = values_39[1];
V v_42 = values_39[2];
var values_43 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_40, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)5.0d}, new Results((V)100000.0d))));
V v_44 = values_43[0];

var values_45 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_46 = v_44;
V key_47 = (V)"hp";
R.Set(obj_46, key_47, values_45[0]);
var values_48 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_40}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_49 = values_48[0];
V v_50 = values_48[1];
V v_51 = values_48[2];
var values_52 = R.Join(new V[] {(V)0.0d, (V)0.0d}, new Results(R.Table()));
V v_53 = values_52[0];
V v_54 = values_52[1];
V v_55 = values_52[2];
foreach (var row_56 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_51)))))) {
V v_57 = row_56[0];
V v_58 = row_56[1];
if (R.Truth(R.And((V)(R.Get(v_58, (V)"kind") == (V)"DamageBatchResolved"), () => (V)(v_53 == (V)0.0d)))) {
var values_59 = R.Join(new V[] {}, new Results(v_57));
v_53 = values_59[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_58, (V)"outcomes")) == (V)2.0d))));
foreach (var row_60 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_58, (V)"outcomes"))))))) {
V v_61 = row_60[0];
V v_62 = row_60[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_62, (V)"hp_after") == (V)0.0d))));

var values_63 = R.Join(new V[] {}, new Results((V)true));
var obj_64 = v_55;
V key_65 = R.Get(v_62, (V)"target_id");
R.Set(obj_64, key_65, values_63[0]);
}
} else if (R.Truth((V)(R.Get(v_58, (V)"kind") == (V)"BoneFragmented"))) {
var values_66 = R.Join(new V[] {}, new Results((V)(v_54 + (V)1.0d)));
v_54 = values_66[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_57 > v_53), () => (V)(R.Get(v_58, (V)"original_id") == R.Get(v_42, (V)"id"))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(v_55, R.Get(v_42, (V)"id")), () => R.Get(v_55, R.Get(v_44, (V)"id"))), () => (V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_40}, new Results(R.Get(v_42, (V)"id")))).First))), () => (V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_40}, new Results(R.Get(v_44, (V)"id")))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_54 == (V)2.0d), () => (V)((V)R.Length(R.Call(v_27, R.Join(new V[] {}, new Results(v_40))).First) == (V)2.0d)), () => (V)(R.Get(R.Get(v_40, (V)"progression"), (V)"xp_total") == (V)9.0d)))));
foreach (var row_67 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_40)))))))) {
V v_68 = row_67[0];
V v_69 = row_67[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_69, (V)"max_hp") == (V)2.0d), () => (V)(R.Get(v_69, (V)"hp") == (V)2.0d)), () => R.Get(v_69, (V)"reward_exempt")))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"two fragments die once with no new split, XP, currency or supply reward"}, new Results(new V((RuleFunction)(args_70 => {

var values_71 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {}, new Results((V)"core:actor/sentinel"))));
V v_72 = values_71[0];
V v_73 = values_71[1];
V v_74 = values_71[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_72}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_27, R.Join(new V[] {}, new Results(v_72))).First) == (V)2.0d), () => (V)(R.Get(R.Get(v_72, (V)"progression"), (V)"xp_total") == (V)4.0d)))));
var values_75 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_72, (V)"drops"))));
V v_76 = values_75[0];
var values_77 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_72}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_78 = values_77[0];
V v_79 = values_77[1];
V v_80 = values_77[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_27, R.Join(new V[] {}, new Results(v_72))).First) == (V)0.0d), () => (V)(R.Get(R.Get(v_72, (V)"progression"), (V)"xp_total") == (V)4.0d)), () => (V)((V)R.Length(R.Get(v_72, (V)"drops")) == v_76)))));
var values_81 = R.Join(new V[] {}, new Results((V)0.0d));
V v_82 = values_81[0];
foreach (var row_83 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_80)))))) {
V v_84 = row_83[0];
V v_85 = row_83[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_85, (V)"kind") != (V)"BoneFragmented"))));
if (R.Truth((V)(R.Get(v_85, (V)"kind") == (V)"EntityKilled"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_85, (V)"reward_exempt"))));

var values_86 = R.Join(new V[] {}, new Results((V)(v_82 + (V)1.0d)));
v_82 = values_86[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_82 == (V)2.0d), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_72))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a cramped split uses only free legal cells and never occupies stairs or carves walls"}, new Results(new V((RuleFunction)(args_87 => {

var values_88 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_89 = values_88[0];
V v_90 = values_88[1];
V v_91 = values_88[2];
var values_92 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"map_for"), R.Join(new V[] {}, new Results(v_89))))))));
var obj_93 = v_89;
V key_94 = (V)"generated_map";
R.Set(obj_93, key_94, values_92[0]);
var values_95 = R.Join(new V[] {}, new Results(R.Get(v_89, (V)"generated_map")));
V v_96 = values_95[0];
var values_97 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_96, (V)"rows")))));
V v_98 = values_97[0];
var values_99 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"stairs", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)5.0d))))))));
var obj_100 = v_96;
V key_101 = (V)"placements";
R.Set(obj_100, key_101, values_99[0]);
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_89, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)6.0d}, new Results((V)100000.0d)));
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_89, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)7.0d}, new Results((V)100000.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_89}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_102 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_89))));
V v_103 = values_102[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_103) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_103, (V)1.0d), (V)"position"), (V)"x") == (V)8.0d)), () => (V)(R.Get(R.Get(R.Get(v_103, (V)1.0d), (V)"position"), (V)"y") == (V)6.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_96, (V)"rows")))).First == v_98))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"fragment continuation preserves pending attacks, exact rewards, events and RNG across codec saves"}, new Results(new V((RuleFunction)(args_104 => {

var values_105 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_106 = values_105[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_106}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_107 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_106))))))))));
V v_108 = values_107[0];
foreach (var row_109 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"wait")))))))))) {
V v_110 = row_109[0];
V v_111 = row_109[1];
var values_112 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_106}, new Results(v_111))));
V v_113 = values_112[0];
V v_114 = values_112[1];
V v_115 = values_112[2];

var values_116 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_108}, new Results(v_111))));
V v_117 = values_116[0];
V v_118 = values_116[1];
V v_119 = values_116[2];
R.Call(v_3, R.Join(new V[] {v_115}, new Results(v_119)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_106))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_108)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"forged fragment ownership, duplicate bodies and renewed rewards fail save validation"}, new Results(new V((RuleFunction)(args_120 => {

var values_121 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_122 = values_121[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_122}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_123 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(v_27, R.Join(new V[] {}, new Results(v_122))).First, (V)1.0d), (V)"id")));
V v_124 = values_123[0];
foreach (var row_140 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_125 => {
V v_126 = args_125[0];
var values_127 = R.Join(new V[] {}, new Results((V)false));
var obj_128 = v_126;
V key_129 = (V)"reward_exempt";
R.Set(obj_128, key_129, values_127[0]);
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_130 => {
V v_131 = args_130[0];
var values_132 = R.Join(new V[] {}, new Results(R.Get(v_131, (V)"id")));
var obj_133 = v_131;
V key_134 = (V)"fragment_of";
R.Set(obj_133, key_134, values_132[0]);
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_135 => {
V v_136 = args_135[0];
var values_137 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_138 = v_136;
V key_139 = (V)"revival_of";
R.Set(obj_138, key_139, values_137[0]);
return Results.Empty;
})))))))))) {
V v_141 = row_140[0];
V v_142 = row_140[1];
var values_143 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_122))));
V v_144 = values_143[0];
foreach (var row_145 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_144, (V)"entities"))))))) {
V v_146 = row_145[0];
V v_147 = row_145[1];
if (R.Truth((V)(R.Get(v_147, (V)"id") == v_124))) {
R.Call(v_142, R.Join(new V[] {}, new Results(v_147)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_144))).First)))));
}
var values_148 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_122))));
V v_149 = values_148[0];

var values_150 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Call(v_27, R.Join(new V[] {}, new Results(v_122))).First, (V)1.0d)))))));
V v_151 = values_150[0];
var values_152 = R.Join(new V[] {}, new Results(R.Get(v_149, (V)"next_entity_id")));
var obj_153 = v_151;
V key_154 = (V)"id";
R.Set(obj_153, key_154, values_152[0]);

var values_155 = R.Join(new V[] {}, new Results((V)(R.Get(v_151, (V)"id") + (V)1.0d)));
var obj_156 = v_149;
V key_157 = (V)"next_entity_id";
R.Set(obj_156, key_157, values_155[0]);
var values_158 = R.Join(new V[] {}, new Results(v_151));
var obj_159 = R.Get(v_149, (V)"entities");
V key_160 = (V)((V)R.Length(R.Get(v_149, (V)"entities")) + (V)1.0d);
R.Set(obj_159, key_160, values_158[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_149))).First))}, new Results((V)"one parent cannot own three fragments")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_122)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"natural floors contain splitters but never pre-spawn fragments and both reviewed bodies resolve"}, new Results(new V((RuleFunction)(args_161 => {

var values_162 = R.Join(new V[] {}, new Results((V)false));
V v_163 = values_162[0];
{ double end_166 = R.Num((V)50.0d), step_167 = R.Num((V)1.0d);
for (double i_165 = R.Num((V)1.0d); step_167 > 0 ? i_165 <= end_166 : i_165 >= end_166; i_165 += step_167) {
V v_164 = i_165;
var values_168 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_164}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_169 = values_168[0];
foreach (var row_170 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_169, (V)"entities"))))))) {
V v_171 = row_170[0];
V v_172 = row_170[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_172, (V)"definition_id") != (V)"core:enemy/bone_fragment"))));
if (R.Truth((V)(R.Get(v_172, (V)"definition_id") == (V)"core:enemy/bone_splitter"))) {
var values_173 = R.Join(new V[] {}, new Results((V)true));
v_163 = values_173[0];
}
}
}
}
if (R.Truth((V)(!R.Truth(v_163)))) {
var values_174 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_seeds"))).First, (V)"find"), R.Join(new V[] {}, new Results((V)"core:enemy/bone_splitter"))));
V v_175 = values_174[0];
foreach (var row_176 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_175, (V)"entities"))))))) {
V v_177 = row_176[0];
V v_178 = row_176[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_178, (V)"definition_id") != (V)"core:enemy/bone_fragment"))));
}
var values_179 = R.Join(new V[] {}, new Results((V)true));
v_163 = values_179[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_163)));
var values_180 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_181 = values_180[0];
foreach (var row_182 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_183 = row_182[0];
V v_184 = row_182[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_181, (V)"packs"), v_184), (V)"missing")) == (V)0.0d))));
var values_185 = R.Join(new V[] {}, new Results(R.Table()));
V v_186 = values_185[0];
foreach (var row_187 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_181, (V)"packs"), v_184), (V)"bindings"))))))) {
V v_188 = row_187[0];
V v_189 = row_187[1];
if (R.Truth((V)(R.Get(v_189, (V)"id") == (V)"core:enemy/bone_splitter"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_189, (V)"index") == (V)323.0d))));

var values_190 = R.Join(new V[] {}, new Results((V)true));
var obj_191 = v_186;
V key_192 = (V)323.0d;
R.Set(obj_191, key_192, values_190[0]);
}
if (R.Truth((V)(R.Get(v_189, (V)"id") == (V)"core:enemy/bone_fragment"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_189, (V)"index") == (V)323.0d))));

var values_193 = R.Join(new V[] {}, new Results((V)true));
var obj_194 = v_186;
V key_195 = (V)"fragment";
R.Set(obj_194, key_195, values_193[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_186, (V)323.0d), () => R.Get(v_186, (V)"fragment")))));
var values_196 = R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_slots"))).First, (V)"enemy_size")));
V v_197 = values_196[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_197, R.Join(new V[] {(V)"core:enemy/bone_fragment"}, new Results((V)48.0d))).First == (V)32.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_197, R.Join(new V[] {(V)"core:enemy/bone_fragment"}, new Results((V)32.0d))).First == (V)16.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_197, R.Join(new V[] {(V)"core:enemy/bone_fragment"}, new Results((V)16.0d))).First == (V)16.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_197, R.Join(new V[] {(V)"core:enemy/bone_splitter"}, new Results((V)48.0d))).First == (V)48.0d))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_9, (V)"revivable"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"ai_id", (V)"idea_regular"), new Entry((V)"definition_id", (V)"core:enemy/bone_splitter"))))).First)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
