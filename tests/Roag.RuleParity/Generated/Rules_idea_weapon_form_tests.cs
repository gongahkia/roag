// Static C# port of tests/idea_weapon_form_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: a0df95bf6f70d57d175990900ad3070eff2096471a7814b47aa5559abe5513a0
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_weapon_form_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_patterns"))).First, (V)"weapon_forms")));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_21 = values_20[0];
var values_22 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"pommel_return"), new Entry((V)2, (V)"needle_flight"), new Entry((V)3, (V)"hollow_sigil"), new Entry((V)4, (V)"backswing"), new Entry((V)5, (V)"distant_bastion"), new Entry((V)6, (V)"volatile_vial"), new Entry((V)7, (V)"split_thrust"))));
V v_23 = values_22[0];
V v_24 = V.Nil;
v_24 = new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
V v_27 = args_25[1];
V v_28 = args_25[2];
V v_29 = args_25[3];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_26, R.Get(v_26, (V)"player_id"), R.Concat((V)"core:boon/", v_27), R.Or(v_28, () => (V)"rare")}, new Results(R.Or(v_29, () => (V)1.0d))));
return Results.Empty;
}));
V v_30 = V.Nil;
v_30 = new V((RuleFunction)(args_31 => {
V v_32 = args_31[0];
var values_33 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, (V)"core:map/scrolling_room"}, new Results(R.Table(new Entry((V)"class_id", R.Get(R.Get(v_11, v_32), (V)"class_id")))))));
V v_34 = values_33[0];
var values_35 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_34))));
V v_36 = values_35[0];

var values_37 = R.Join(new V[] {(V)15.0d}, new Results((V)16.0d));
var obj_38 = R.Get(v_36, (V)"position");
V key_39 = (V)"x";
var obj_40 = R.Get(v_36, (V)"position");
V key_41 = (V)"y";
R.Set(obj_38, key_39, values_37[0]);
R.Set(obj_40, key_41, values_37[1]);
R.Call(v_24, R.Join(new V[] {v_34}, new Results(v_32)));

R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_34)));

return R.Join(new V[] {v_34}, new Results(v_36));
return Results.Empty;
}));
V v_42 = V.Nil;
v_42 = new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
V v_46 = args_43[2];
V v_47 = args_43[3];
V v_48 = args_43[4];
var values_49 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_44))).First, (V)"position")));
V v_50 = values_49[0];
var values_51 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_44, R.Or(v_47, () => (V)"core:enemy/mossbound_guard"), (V)(R.Get(v_50, (V)"x") + v_45), (V)(R.Get(v_50, (V)"y") + v_46)}, new Results((V)100000.0d))));
V v_52 = values_51[0];
var values_53 = R.Join(new V[] {}, new Results(R.Or(v_48, () => (V)100.0d)));
var obj_54 = v_52;
V key_55 = (V)"hp";
R.Set(obj_54, key_55, values_53[0]);

var values_56 = R.Join(new V[] {}, new Results(R.Or(v_48, () => (V)100.0d)));
var obj_57 = v_52;
V key_58 = (V)"max_hp";
R.Set(obj_57, key_58, values_56[0]);

return R.Join(new V[] {}, new Results(v_52));
return Results.Empty;
}));
V v_59 = V.Nil;
v_59 = new V((RuleFunction)(args_60 => {
V v_61 = args_60[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(R.Table(new Entry((V)"hollow_sigil", (V)true), new Entry((V)"backswing", (V)true), new Entry((V)"distant_bastion", (V)true)), v_61), () => (V)"sweeping_slash"), () => (V)"basic_attack")));
return Results.Empty;
}));
V v_62 = V.Nil;
v_62 = new V((RuleFunction)(args_63 => {
V v_64 = args_63[0];
V v_65 = args_63[1];
V v_66 = args_63[2];
V v_67 = args_63[3];
V v_68 = args_63[4];
var values_69 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", R.Or(v_65, () => (V)"basic_attack")), new Entry((V)"dx", R.Or(v_66, () => (V)1.0d)), new Entry((V)"dy", R.Or(v_67, () => (V)0.0d)))));
V v_70 = values_69[0];
if (R.Truth(v_68)) {
foreach (var row_71 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_68)))))) {
V v_72 = row_71[0];
V v_73 = row_71[1];
var values_74 = R.Join(new V[] {}, new Results(v_73));
var obj_75 = v_70;
V key_76 = v_72;
R.Set(obj_75, key_76, values_74[0]);
}
}
var values_77 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_64}, new Results(v_70))));
V v_78 = values_77[0];
V v_79 = values_77[1];
V v_80 = values_77[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_78}, new Results(v_79)));

return R.Join(new V[] {}, new Results(v_80));
return Results.Empty;
}));
V v_81 = V.Nil;
v_81 = new V((RuleFunction)(args_82 => {
V v_83 = args_82[0];
V v_84 = args_82[1];
V v_85 = args_82[2];
foreach (var row_86 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_83)))))) {
V v_87 = row_86[0];
V v_88 = row_86[1];
if (R.Truth(R.And((V)(R.Get(v_88, (V)"x") == v_84), () => (V)(R.Get(v_88, (V)"y") == v_85)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
V v_89 = V.Nil;
v_89 = new V((RuleFunction)(args_90 => {
V v_91 = args_90[0];
V v_92 = args_90[1];
var values_93 = R.Join(new V[] {}, new Results((V)0.0d));
V v_94 = values_93[0];

foreach (var row_95 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_91)))))) {
V v_96 = row_95[0];
V v_97 = row_95[1];
if (R.Truth((V)(R.Get(v_97, (V)"kind") == v_92))) {
var values_98 = R.Join(new V[] {}, new Results((V)(v_94 + (V)1.0d)));
v_94 = values_98[0];
}
}

return R.Join(new V[] {}, new Results(v_94));
return Results.Empty;
}));
V v_99 = V.Nil;
v_99 = new V((RuleFunction)(args_100 => {
V v_101 = args_100[0];
foreach (var row_102 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_101)))))) {
V v_103 = row_102[0];
V v_104 = row_102[1];
if (R.Truth((V)(R.Get(v_104, (V)"kind") == (V)"DamageBatchResolved"))) {
return R.Join(new V[] {}, new Results(R.Get(v_104, (V)"outcomes")));
}
}
return Results.Empty;
}));
V v_105 = V.Nil;
v_105 = new V((RuleFunction)(args_106 => {
V v_107 = args_106[0];
var values_108 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results(v_107))));
V v_109 = values_108[0];
V v_110 = values_108[1];
var values_111 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_109, (V)3.0d, (V)0.0d}, new Results((V)"core:enemy/mirror_hexer"))));
V v_112 = values_111[0];

var values_113 = R.Join(new V[] {}, new Results((V)7.0d));
var obj_114 = v_112;
V key_115 = (V)"hp";
R.Set(obj_114, key_115, values_113[0]);

var values_116 = R.Join(new V[] {}, new Results((V)7.0d));
var obj_117 = v_112;
V key_118 = (V)"max_hp";
R.Set(obj_117, key_118, values_116[0]);
var values_119 = R.Join(new V[] {}, new Results((V)75.0d));
var obj_120 = v_112;
V key_121 = (V)"speed";
R.Set(obj_120, key_121, values_119[0]);
R.Call(R.Get(v_21, (V)"remove_for_actor"), R.Join(new V[] {v_109}, new Results(R.Get(v_112, (V)"id"))));
R.Call(R.Get(v_21, (V)"enqueue"), R.Join(new V[] {v_109}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)50.0d), new Entry((V)"actor_id", R.Get(v_112, (V)"id")), new Entry((V)"is_player", (V)false)))));
{ double end_124 = R.Num((V)4.0d), step_125 = R.Num((V)1.0d);
for (double i_123 = R.Num((V)1.0d); step_125 > 0 ? i_123 <= end_124 : i_123 >= end_124; i_123 += step_125) {
V v_122 = i_123;
if (R.Truth((V)((V)R.Length(R.Or(R.Get(v_109, (V)"idea_mirrors"), () => R.Table())) > (V)0.0d))) {
break;
}
R.Call(v_62, R.Join(new V[] {v_109}, new Results((V)"wait")));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_109, (V)"idea_mirrors")) == (V)1.0d)}, new Results((V)"real mirror not created")));
return R.Join(new V[] {v_109, v_110, v_112}, new Results(R.Get(R.Get(v_109, (V)"idea_mirrors"), (V)1.0d)));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"seven class weapon forms declare exact eligible abilities and native artwork"}, new Results(new V((RuleFunction)(args_126 => {

var values_127 = R.Join(new V[] {}, new Results(R.Table()));
V v_128 = values_127[0];
foreach (var row_129 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_23)))))) {
V v_130 = row_129[0];
V v_131 = row_129[1];
var values_132 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"get"), R.Join(new V[] {}, new Results(R.Concat((V)"core:boon/", v_131)))));
V v_133 = values_132[0];

var values_134 = R.Join(new V[] {}, new Results(R.Get(v_11, v_131)));
V v_135 = values_134[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_133, (V)"eligible_classes"), (V)1.0d) == R.Get(v_135, (V)"class_id")), () => (V)((V)R.Length(R.Get(v_133, (V)"eligible_classes")) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(R.Get(v_133, (V)"modifiers"), (V)1.0d), (V)"kind") == (V)"weapon_form"), () => (V)(R.Get(R.Get(R.Get(v_133, (V)"modifiers"), (V)1.0d), (V)"form") == v_131)), () => (V)(R.Get(R.Get(R.Get(v_133, (V)"modifiers"), (V)1.0d), (V)"ability_id") == R.Get(v_135, (V)"ability_id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_128, R.Get(R.Get(v_133, (V)"art"), (V)"index")))))));

var values_136 = R.Join(new V[] {}, new Results((V)true));
var obj_137 = v_128;
V key_138 = R.Get(R.Get(v_133, (V)"art"), (V)"index");
R.Set(obj_137, key_138, values_136[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))).First, (V)"boons"), R.Get(v_133, (V)"id")), (V)1.0d) == R.Get(R.Get(v_133, (V)"art"), (V)"index")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"validate"), Results.Empty)));
var values_139 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/needle_flight"))))))));
V v_140 = values_139[0];
var values_141 = R.Join(new V[] {}, new Results((V)"walk_through_walls"));
var obj_142 = R.Get(R.Get(v_140, (V)"modifiers"), (V)1.0d);
V key_143 = (V)"form";
R.Set(obj_142, key_143, values_141[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"validate"), R.Table(new Entry(R.Get(v_140, (V)"id"), v_140))}, new Results(R.Table(new Entry((V)1, R.Get(v_140, (V)"id")))))).First)))));
var values_144 = R.Join(new V[] {}, new Results((V)"needle_flight"));
var obj_145 = R.Get(R.Get(v_140, (V)"modifiers"), (V)1.0d);
V key_146 = (V)"form";
R.Set(obj_145, key_146, values_144[0]);

var values_147 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:actor/adventurer"))));
var obj_148 = v_140;
V key_149 = (V)"eligible_classes";
R.Set(obj_148, key_149, values_147[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"validate"), R.Table(new Entry(R.Get(v_140, (V)"id"), v_140))}, new Results(R.Table(new Entry((V)1, R.Get(v_140, (V)"id")))))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"normal offers include each owned-class form and exclude every other class form"}, new Results(new V((RuleFunction)(args_150 => {

var values_151 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"starter_order"), Results.Empty));
V v_152 = values_151[0];
foreach (var row_153 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_23)))))) {
V v_154 = row_153[0];
V v_155 = row_153[1];
var values_156 = R.Join(new V[] {}, new Results(R.Concat((V)"core:boon/", v_155)));
var obj_157 = v_152;
V key_158 = (V)((V)R.Length(v_152) + (V)1.0d);
R.Set(obj_157, key_158, values_156[0]);
}
foreach (var row_159 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_23)))))) {
V v_160 = row_159[0];
V v_161 = row_159[1];
var values_162 = R.Join(new V[] {}, new Results((V)false));
V v_163 = values_162[0];
{ double end_166 = R.Num((V)160.0d), step_167 = R.Num((V)1.0d);
for (double i_165 = R.Num((V)1.0d); step_167 > 0 ? i_165 <= end_166 : i_165 >= end_166; i_165 += step_167) {
V v_164 = i_165;
var values_168 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {v_164, V.Nil}, new Results(R.Table(new Entry((V)"class_id", R.Get(R.Get(v_11, v_161), (V)"class_id")), new Entry((V)"available_boons", v_152))))));
V v_169 = values_168[0];
foreach (var row_170 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))).First, (V)"make_offers"), R.Join(new V[] {v_169}, R.Call(R.Get(v_13, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/standard")))))))))) {
V v_171 = row_170[0];
V v_172 = row_170[1];
if (R.Truth((V)(R.Get(v_172, (V)"boon_id") == R.Concat((V)"core:boon/", v_161)))) {
var values_173 = R.Join(new V[] {}, new Results((V)true));
v_163 = values_173[0];
}
var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_172, (V)"boon_id")))));
V v_175 = values_174[0];
if (R.Truth(R.Get(v_175, (V)"eligible_classes"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_175, (V)"eligible_classes"), (V)1.0d) == R.Get(R.Get(v_11, v_161), (V)"class_id")))));
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_163}, new Results(v_161)));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pommel Return hits the front and clear rear in one primary HP batch with unchanged recovery"}, new Results(new V((RuleFunction)(args_176 => {

var values_177 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"pommel_return"))));
V v_178 = values_177[0];
V v_179 = values_177[1];

var values_180 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_178, (V)1.0d}, new Results((V)0.0d))));
V v_181 = values_180[0];

var values_182 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_178, (-(V)1.0d)}, new Results((V)0.0d))));
V v_183 = values_182[0];
var values_184 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_178))));
V v_185 = values_184[0];

var values_186 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_178, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_187 = values_186[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_187, (V)"cells")) == (V)2.0d), () => R.Invoke(R.Get(v_187, (V)"weapon_hint"), "find", R.Join(new V[] {(V)"rear", (V)1.0d}, new Results((V)true))).First))));
R.Call(v_3, R.Join(new V[] {v_185}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_178)))));
var values_188 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {}, new Results(v_178))));
V v_189 = values_188[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_181, (V)"hp") == (V)94.0d), () => (V)(R.Get(v_183, (V)"hp") == (V)94.0d)), () => (V)((V)R.Length(R.Call(v_99, R.Join(new V[] {}, new Results(v_189))).First) == (V)2.0d)), () => (V)(R.Get(v_178, (V)"clock") == (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_179, (V)"hp") == R.Get(v_179, (V)"max_hp")), () => (V)(R.Call(v_89, R.Join(new V[] {v_189}, new Results((V)"AttackPerformed"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pommel composes with Longblade while bump retains its original narrow footprint"}, new Results(new V((RuleFunction)(args_190 => {

foreach (var row_191 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)false), new Entry((V)2, (V)true)))))))) {
V v_192 = row_191[0];
V v_193 = row_191[1];
var values_194 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"pommel_return"))));
V v_195 = values_194[0];
V v_196 = values_194[1];

R.Call(v_24, R.Join(new V[] {v_195, (V)"longblade_geometry", (V)"common"}, new Results((V)2.0d)));
var values_197 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_195, (V)1.0d}, new Results((V)0.0d))));
V v_198 = values_197[0];

var values_199 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_195, (V)2.0d}, new Results((V)0.0d))));
V v_200 = values_199[0];

var values_201 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_195, (-(V)1.0d)}, new Results((V)0.0d))));
V v_202 = values_201[0];
var values_203 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_195}, new Results(R.Or(R.And(v_193, () => (V)"move"), () => (V)"basic_attack")))));
V v_204 = values_203[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_198, (V)"hp") == (V)92.0d), () => (V)(R.Get(R.Get(v_196, (V)"position"), (V)"x") == (V)15.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_200, (V)"hp") == R.Or(R.And(v_193, () => (V)100.0d), () => (V)92.0d)), () => (V)(R.Get(v_202, (V)"hp") == R.Or(R.And(v_193, () => (V)100.0d), () => (V)92.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_99, R.Join(new V[] {}, new Results(v_204))).First) == R.Or(R.And(v_193, () => (V)1.0d), () => (V)3.0d)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a closed corner blocks the new rear cell without changing the normal front strike"}, new Results(new V((RuleFunction)(args_205 => {

var values_206 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"pommel_return"))));
V v_207 = values_206[0];
V v_208 = values_206[1];
R.Call(R.Get(v_5, (V)"spawn_door"), R.Join(new V[] {v_207, (V)(R.Get(R.Get(v_208, (V)"position"), (V)"x") - (V)1.0d)}, new Results(R.Get(R.Get(v_208, (V)"position"), (V)"y"))));
var values_209 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_207, (V)1.0d}, new Results((V)1.0d))));
V v_210 = values_209[0];

var values_211 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_207, (-(V)1.0d)}, new Results((-(V)1.0d)))));
V v_212 = values_211[0];
var values_213 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_207, (V)"basic_attack", (V)1.0d}, new Results((V)1.0d))));
V v_214 = values_213[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(v_81, R.Join(new V[] {R.Get(v_214, (V)"cells"), R.Get(R.Get(v_212, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_212, (V)"position"), (V)"y")))).First)))));
R.Call(v_62, R.Join(new V[] {v_207, (V)"basic_attack", (V)1.0d}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_210, (V)"hp") == (V)94.0d), () => (V)(R.Get(v_212, (V)"hp") == (V)100.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Needle Flight pierces the full clear bow line but bump stops at first contact"}, new Results(new V((RuleFunction)(args_215 => {

foreach (var row_216 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)false), new Entry((V)2, (V)true)))))))) {
V v_217 = row_216[0];
V v_218 = row_216[1];
var values_219 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"needle_flight"))));
V v_220 = values_219[0];

var values_221 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_220, (V)1.0d}, new Results((V)0.0d))));
V v_222 = values_221[0];

var values_223 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_220, (V)3.0d}, new Results((V)0.0d))));
V v_224 = values_223[0];
var values_225 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_220}, new Results(R.Or(R.And(v_218, () => (V)"move"), () => (V)"basic_attack")))));
V v_226 = values_225[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_222, (V)"hp") == (V)95.0d), () => (V)(R.Get(v_224, (V)"hp") == R.Or(R.And(v_218, () => (V)100.0d), () => (V)95.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_99, R.Join(new V[] {}, new Results(v_226))).First) == R.Or(R.And(v_218, () => (V)1.0d), () => (V)2.0d)), () => (V)(R.Get(v_220, (V)"clock") == (V)87.0d)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Needle and Splitflight preserve the ray plus valid branches without duplicate primary victims"}, new Results(new V((RuleFunction)(args_227 => {

var values_228 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"needle_flight"))));
V v_229 = values_228[0];

R.Call(v_24, R.Join(new V[] {v_229, (V)"splitflight"}, new Results((V)"common")));
var values_230 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_229, (V)1.0d}, new Results((V)0.0d))));
V v_231 = values_230[0];

var values_232 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_229, (V)2.0d}, new Results((V)0.0d))));
V v_233 = values_232[0];

var values_234 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_229, (V)1.0d}, new Results((V)1.0d))));
V v_235 = values_234[0];
var values_236 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {}, new Results(v_229))));
V v_237 = values_236[0];

var values_238 = R.Join(new V[] {}, R.Call(v_99, R.Join(new V[] {}, new Results(v_237))));
V v_239 = values_238[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(v_239) == (V)3.0d), () => (V)(R.Get(v_231, (V)"hp") < (V)100.0d)), () => (V)(R.Get(v_233, (V)"hp") < (V)100.0d)), () => (V)(R.Get(v_235, (V)"hp") < (V)100.0d)))));
var values_240 = R.Join(new V[] {}, new Results(R.Table()));
V v_241 = values_240[0];

foreach (var row_242 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_239)))))) {
V v_243 = row_242[0];
V v_244 = row_242[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_241, R.Get(v_244, (V)"target_id")))))));

var values_245 = R.Join(new V[] {}, new Results((V)true));
var obj_246 = v_241;
V key_247 = R.Get(v_244, (V)"target_id");
R.Set(obj_246, key_247, values_245[0]);
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Needle stops at a closed door and preserves ordinary contact with a breakable barricade"}, new Results(new V((RuleFunction)(args_248 => {

var values_249 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"needle_flight"))));
V v_250 = values_249[0];
V v_251 = values_249[1];

var values_252 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_250, (V)1.0d}, new Results((V)0.0d))));
V v_253 = values_252[0];

var values_254 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_250, (V)3.0d}, new Results((V)0.0d))));
V v_255 = values_254[0];
R.Call(R.Get(v_5, (V)"spawn_door"), R.Join(new V[] {v_250, (V)(R.Get(R.Get(v_251, (V)"position"), (V)"x") + (V)2.0d)}, new Results(R.Get(R.Get(v_251, (V)"position"), (V)"y"))));
R.Call(v_62, R.Join(new V[] {}, new Results(v_250)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_253, (V)"hp") == (V)95.0d), () => (V)(R.Get(v_255, (V)"hp") == (V)100.0d)))));
var values_256 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"needle_flight"))));
v_250 = values_256[0];
v_251 = values_256[1];

var values_257 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_250, (V)1.0d}, new Results((V)0.0d))));
v_253 = values_257[0];

var values_258 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_250, (V)3.0d}, new Results((V)0.0d))));
v_255 = values_258[0];
var values_259 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"map_for"), R.Join(new V[] {}, new Results(v_250))))))));
var obj_260 = v_250;
V key_261 = (V)"generated_map";
R.Set(obj_260, key_261, values_259[0]);
var values_262 = R.Join(new V[] {}, new Results(R.Get(v_250, (V)"generated_map")));
V v_263 = values_262[0];

var values_264 = R.Join(new V[] {(V)(R.Get(R.Get(v_251, (V)"position"), (V)"x") + (V)2.0d)}, new Results(R.Get(R.Get(v_251, (V)"position"), (V)"y")));
V v_265 = values_264[0];
V v_266 = values_264[1];
var values_267 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_263, (V)"rows"), v_266), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_265 - (V)1.0d)))).First, R.Concat((V)"B", R.Invoke(R.Get(R.Get(v_263, (V)"rows"), v_266), "sub", R.Join(new V[] {}, new Results((V)(v_265 + (V)1.0d)))).First))));
var obj_268 = R.Get(v_263, (V)"rows");
V key_269 = v_266;
R.Set(obj_268, key_269, values_267[0]);
var values_270 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_250, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_271 = values_270[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_81, R.Join(new V[] {R.Get(v_271, (V)"cells"), v_265}, new Results(v_266)))));
var values_272 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {}, new Results(v_250))));
V v_273 = values_272[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_253, (V)"hp") == (V)95.0d), () => (V)(R.Get(v_255, (V)"hp") == (V)100.0d)), () => (V)(R.Call(v_89, R.Join(new V[] {v_273}, new Results((V)"TerrainBroken"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(R.Get(v_263, (V)"rows"), v_266), "sub", R.Join(new V[] {v_265}, new Results(v_265))).First == (V)"."))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a piercing Needle reflects even after striking an intervening creature"}, new Results(new V((RuleFunction)(args_274 => {

var values_275 = R.Join(new V[] {}, R.Call(v_105, R.Join(new V[] {}, new Results((V)"needle_flight"))));
V v_276 = values_275[0];
V v_277 = values_275[1];
V v_278 = values_275[2];
V v_279 = values_275[3];
var values_280 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_276, (V)1.0d}, new Results((V)0.0d))));
V v_281 = values_280[0];

var values_282 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_276, (V)2.0d}, new Results((V)1.0d))));
V v_283 = values_282[0];

var values_284 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_276, (V)2.0d}, new Results((V)2.0d))));
V v_285 = values_284[0];
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_276)));

var values_286 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_276))));
V v_287 = values_286[0];
var values_288 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_276, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_289 = values_288[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_289, (V)"reflection"), () => (V)(R.Get(R.Get(v_289, (V)"reflection"), (V)"id") == R.Get(v_279, (V)"id"))))));
R.Call(v_3, R.Join(new V[] {v_287}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_276)))));
var values_290 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {}, new Results(v_276))));
V v_291 = values_290[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_281, (V)"hp") == (V)95.0d), () => (V)(R.Get(v_283, (V)"hp") == (V)95.0d)), () => (V)(R.Get(v_285, (V)"hp") == (V)95.0d)), () => (V)(R.Get(v_278, (V)"hp") == (V)7.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_89, R.Join(new V[] {v_291}, new Results((V)"MirrorReflected"))).First == (V)1.0d), () => (V)((V)R.Length(R.Get(v_276, (V)"idea_mirrors")) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Hollow Sigil spares its center while a surrounding victim takes primary fire damage"}, new Results(new V((RuleFunction)(args_292 => {

var values_293 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"hollow_sigil"))));
V v_294 = values_293[0];
V v_295 = values_293[1];

var values_296 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_294, (V)2.0d}, new Results((V)0.0d))));
V v_297 = values_296[0];

var values_298 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_294, (V)2.0d}, new Results((V)1.0d))));
V v_299 = values_298[0];
var values_300 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_294, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_301 = values_300[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_301, (V)"cells")) == (V)8.0d), () => (V)(!R.Truth(R.Call(v_81, R.Join(new V[] {R.Get(v_301, (V)"cells"), R.Get(R.Get(v_297, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_297, (V)"position"), (V)"y")))).First))))));
var values_302 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_294}, new Results((V)"sweeping_slash"))));
V v_303 = values_302[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_297, (V)"hp") == (V)100.0d), () => (V)(R.Get(v_299, (V)"hp") == (V)94.0d)), () => (V)((V)R.Length(R.Call(v_99, R.Join(new V[] {}, new Results(v_303))).First) == (V)1.0d)), () => (V)(R.Get(v_294, (V)"clock") == (V)170.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_99, R.Join(new V[] {}, new Results(v_303))).First, (V)1.0d), (V)"damage_type") == (V)"fire"))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Hollow and Expanding Sigil retain the outer ring while sparing the center and caster"}, new Results(new V((RuleFunction)(args_304 => {

var values_305 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"hollow_sigil"))));
V v_306 = values_305[0];
V v_307 = values_305[1];

R.Call(v_24, R.Join(new V[] {v_306, (V)"expanding_sigil"}, new Results((V)"common")));
var values_308 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_306, (V)2.0d}, new Results((V)0.0d))));
V v_309 = values_308[0];

var values_310 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_306, (V)2.0d}, new Results((V)1.0d))));
V v_311 = values_310[0];

var values_312 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_306, (V)4.0d}, new Results((V)0.0d))));
V v_313 = values_312[0];
var values_314 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_306, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_315 = values_314[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_315, (V)"cells")) == (V)23.0d), () => (V)(!R.Truth(R.Call(v_81, R.Join(new V[] {R.Get(v_315, (V)"cells"), R.Get(R.Get(v_309, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_309, (V)"position"), (V)"y")))).First))), () => (V)(!R.Truth(R.Call(v_81, R.Join(new V[] {R.Get(v_315, (V)"cells"), R.Get(R.Get(v_307, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_307, (V)"position"), (V)"y")))).First))))));
var values_316 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_306}, new Results((V)"sweeping_slash"))));
V v_317 = values_316[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_309, (V)"hp") == (V)100.0d), () => (V)(R.Get(v_311, (V)"hp") < (V)100.0d)), () => (V)(R.Get(v_313, (V)"hp") < (V)100.0d)), () => (V)((V)R.Length(R.Call(v_99, R.Join(new V[] {}, new Results(v_317))).First) == (V)2.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an unplaced blocked Hollow Sigil keeps the original short travel cells"}, new Results(new V((RuleFunction)(args_318 => {

var values_319 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"hollow_sigil"))));
V v_320 = values_319[0];
V v_321 = values_319[1];

var values_322 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_door"), R.Join(new V[] {v_320, (V)(R.Get(R.Get(v_321, (V)"position"), (V)"x") + (V)1.0d)}, new Results(R.Get(R.Get(v_321, (V)"position"), (V)"y")))));
V v_323 = values_322[0];
var values_324 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_320, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_325 = values_324[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_325, (V)"valid"), () => (V)((V)R.Length(R.Get(v_325, (V)"cells")) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_325, (V)"cells"), (V)1.0d), (V)"x") == R.Get(R.Get(v_323, (V)"position"), (V)"x"))))));
R.Call(v_62, R.Join(new V[] {v_320}, new Results((V)"sweeping_slash")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_323, (V)"open"))), () => (V)(R.Get(v_320, (V)"clock") == (V)170.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Backswing completes eight open neighboring cells in one deliberate primary batch"}, new Results(new V((RuleFunction)(args_326 => {

var values_327 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"backswing"))));
V v_328 = values_327[0];
V v_329 = values_327[1];

var values_330 = R.Join(new V[] {}, new Results(R.Table()));
V v_331 = values_330[0];
{ double end_334 = R.Num((V)1.0d), step_335 = R.Num((V)1.0d);
for (double i_333 = R.Num((-(V)1.0d)); step_335 > 0 ? i_333 <= end_334 : i_333 >= end_334; i_333 += step_335) {
V v_332 = i_333;
{ double end_338 = R.Num((V)1.0d), step_339 = R.Num((V)1.0d);
for (double i_337 = R.Num((-(V)1.0d)); step_339 > 0 ? i_337 <= end_338 : i_337 >= end_338; i_337 += step_339) {
V v_336 = i_337;
if (R.Truth(R.Or((V)(v_336 != (V)0.0d), () => (V)(v_332 != (V)0.0d)))) {
var values_340 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_328, v_336}, new Results(v_332))));
var obj_341 = v_331;
V key_342 = (V)((V)R.Length(v_331) + (V)1.0d);
R.Set(obj_341, key_342, values_340[0]);
}
}
}
}
}
var values_343 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_328, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_344 = values_343[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_344, (V)"cells")) == (V)8.0d))));
var values_345 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_328}, new Results((V)"sweeping_slash"))));
V v_346 = values_345[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_99, R.Join(new V[] {}, new Results(v_346))).First) == (V)8.0d), () => (V)(R.Get(v_328, (V)"clock") == (V)240.0d)), () => (V)(R.Get(v_329, (V)"hp") == R.Get(v_329, (V)"max_hp"))))));
foreach (var row_347 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_331)))))) {
V v_348 = row_347[0];
V v_349 = row_347[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_349, (V)"hp") == (V)93.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Distant Bastion hits the exact two-step diamond while sparing cardinal-adjacent creatures"}, new Results(new V((RuleFunction)(args_350 => {

var values_351 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"distant_bastion"))));
V v_352 = values_351[0];

var values_353 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_352, (V)1.0d}, new Results((V)0.0d))));
V v_354 = values_353[0];
var values_355 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_352, (V)2.0d}, new Results((V)0.0d))));
V v_356 = values_355[0];

var values_357 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_352, (V)1.0d}, new Results((V)1.0d))));
V v_358 = values_357[0];
var values_359 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_352, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_360 = values_359[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_360, (V)"cells")) == (V)8.0d))));
foreach (var row_361 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_360, (V)"cells"))))))) {
V v_362 = row_361[0];
V v_363 = row_361[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_363, (V)"x") - (V)15.0d)))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_363, (V)"y") - (V)16.0d)))).First) == (V)2.0d))));
}
var values_364 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_352}, new Results((V)"sweeping_slash"))));
V v_365 = values_364[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_354, (V)"hp") == (V)100.0d), () => (V)(R.Get(v_356, (V)"hp") == (V)95.0d)), () => (V)(R.Get(v_358, (V)"hp") == (V)95.0d)), () => (V)((V)R.Length(R.Call(v_99, R.Join(new V[] {}, new Results(v_365))).First) == (V)2.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"closed doors and stone corners clip the Distant Bastion diamond rather than being bypassed"}, new Results(new V((RuleFunction)(args_366 => {

var values_367 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"distant_bastion"))));
V v_368 = values_367[0];
V v_369 = values_367[1];

R.Call(R.Get(v_5, (V)"spawn_door"), R.Join(new V[] {v_368, (V)(R.Get(R.Get(v_369, (V)"position"), (V)"x") + (V)1.0d)}, new Results(R.Get(R.Get(v_369, (V)"position"), (V)"y"))));
var values_370 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_368, (V)2.0d}, new Results((V)0.0d))));
V v_371 = values_370[0];

var values_372 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_368, (V)1.0d}, new Results((V)1.0d))));
V v_373 = values_372[0];
var values_374 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_368, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_375 = values_374[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(v_81, R.Join(new V[] {R.Get(v_375, (V)"cells"), R.Get(R.Get(v_371, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_371, (V)"position"), (V)"y")))).First)), () => (V)(!R.Truth(R.Call(v_81, R.Join(new V[] {R.Get(v_375, (V)"cells"), R.Get(R.Get(v_373, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_373, (V)"position"), (V)"y")))).First))))));
R.Call(v_62, R.Join(new V[] {v_368}, new Results((V)"sweeping_slash")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_371, (V)"hp") == (V)100.0d), () => (V)(R.Get(v_373, (V)"hp") == (V)100.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a Volatile Vial keeps its clear travel cells and resolves the first-contact cross in one primary batch"}, new Results(new V((RuleFunction)(args_376 => {

var values_377 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"volatile_vial"))));
V v_378 = values_377[0];
var values_379 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Call(v_42, R.Join(new V[] {v_378, (V)2.0d}, new Results((V)0.0d))).First), new Entry((V)2, R.Call(v_42, R.Join(new V[] {v_378, (V)2.0d}, new Results((-(V)1.0d)))).First), new Entry((V)3, R.Call(v_42, R.Join(new V[] {v_378, (V)2.0d}, new Results((V)1.0d))).First), new Entry(4, R.Call(v_42, R.Join(new V[] {v_378, (V)3.0d}, new Results((V)0.0d)))))));
V v_380 = values_379[0];
var values_381 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_378, (V)4.0d}, new Results((V)0.0d))));
V v_382 = values_381[0];
var values_383 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_378, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_384 = values_383[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_384, (V)"cells")) == (V)5.0d), () => R.Call(v_81, R.Join(new V[] {R.Get(v_384, (V)"cells"), (V)16.0d}, new Results((V)16.0d))).First), () => R.Call(v_81, R.Join(new V[] {R.Get(v_384, (V)"cells"), (V)18.0d}, new Results((V)16.0d))).First))));
var values_385 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {}, new Results(v_378))));
V v_386 = values_385[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_99, R.Join(new V[] {}, new Results(v_386))).First) == (V)4.0d), () => (V)(R.Get(v_382, (V)"hp") == (V)100.0d)), () => (V)(R.Get(v_378, (V)"clock") == (V)100.0d)))));
foreach (var row_387 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_380)))))) {
V v_388 = row_387[0];
V v_389 = row_387[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_389, (V)"hp") == (V)95.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"empty and concealed-body Vial previews are identical and cannot reveal a hidden splash center"}, new Results(new V((RuleFunction)(args_390 => {

var values_391 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"volatile_vial"))));
V v_392 = values_391[0];

var values_393 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_392, (V)2.0d}, new Results((V)0.0d))));
V v_394 = values_393[0];

R.Call(v_24, R.Join(new V[] {v_392}, new Results((V)"linebreaker_pawn")));
var values_395 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"map_for"), R.Join(new V[] {}, new Results(v_392))));
V v_396 = values_395[0];
var values_397 = R.Join(new V[] {}, new Results(V.Nil));
var obj_398 = R.Get(R.Get(v_392, (V)"visible"), (V)"cells");
V key_399 = (V)((V)((V)(R.Get(R.Get(v_394, (V)"position"), (V)"y") - (V)1.0d) * R.Get(v_396, (V)"width")) + R.Get(R.Get(v_394, (V)"position"), (V)"x"));
R.Set(obj_398, key_399, values_397[0]);
var values_400 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_392))))))));
V v_401 = values_400[0];
R.Call(R.Get(v_21, (V)"remove_for_actor"), R.Join(new V[] {v_401}, new Results(R.Get(v_394, (V)"id"))));
foreach (var row_402 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_401, (V)"entities"))))))) {
V v_403 = row_402[0];
V v_404 = row_402[1];
if (R.Truth((V)(R.Get(v_404, (V)"id") == R.Get(v_394, (V)"id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_401, (V)"entities")}, new Results(v_403)));

break;
}
}
var values_405 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_392))));
V v_406 = values_405[0];

var values_407 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_401))));
V v_408 = values_407[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_392, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_401, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)))));
R.Call(v_3, R.Join(new V[] {v_406}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_392)))));

R.Call(v_3, R.Join(new V[] {v_408}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_401)))));
R.Call(v_62, R.Join(new V[] {}, new Results(v_392)));

R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_394, (V)"hp") == (V)95.0d)}, new Results((V)"concealment must not remove physical projectile contact")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Volatile Vial reflects before finding contact and splashing instead of disabling the mirror"}, new Results(new V((RuleFunction)(args_409 => {

var values_410 = R.Join(new V[] {}, R.Call(v_105, R.Join(new V[] {}, new Results((V)"volatile_vial"))));
V v_411 = values_410[0];
V v_412 = values_410[1];
V v_413 = values_410[2];
var values_414 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_411, (V)2.0d}, new Results((V)1.0d))));
V v_415 = values_414[0];

var values_416 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_411, (V)2.0d}, new Results((V)2.0d))));
V v_417 = values_416[0];

R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_411)));
var values_418 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_411))));
V v_419 = values_418[0];

var values_420 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_411, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_421 = values_420[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_421, (V)"reflection"), () => R.Call(v_81, R.Join(new V[] {R.Get(v_421, (V)"cells"), R.Get(R.Get(v_417, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_417, (V)"position"), (V)"y")))).First))));
R.Call(v_3, R.Join(new V[] {v_419}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_411)))));
var values_422 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {}, new Results(v_411))));
V v_423 = values_422[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_415, (V)"hp") == (V)95.0d), () => (V)(R.Get(v_417, (V)"hp") == (V)95.0d)), () => (V)(R.Get(v_413, (V)"hp") == (V)7.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_89, R.Join(new V[] {v_423}, new Results((V)"MirrorReflected"))).First == (V)1.0d), () => (V)((V)R.Length(R.Get(v_411, (V)"idea_mirrors")) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a concealed body cannot leak through the Vial mirror preview but still blocks actual reflection"}, new Results(new V((RuleFunction)(args_424 => {

var values_425 = R.Join(new V[] {}, R.Call(v_105, R.Join(new V[] {}, new Results((V)"volatile_vial"))));
V v_426 = values_425[0];
V v_427 = values_425[1];
V v_428 = values_425[2];

var values_429 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_426, (V)1.0d}, new Results((V)0.0d))));
V v_430 = values_429[0];
var values_431 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"map_for"), R.Join(new V[] {}, new Results(v_426))));
V v_432 = values_431[0];
var values_433 = R.Join(new V[] {}, new Results(V.Nil));
var obj_434 = R.Get(R.Get(v_426, (V)"visible"), (V)"cells");
V key_435 = (V)((V)((V)(R.Get(R.Get(v_430, (V)"position"), (V)"y") - (V)1.0d) * R.Get(v_432, (V)"width")) + R.Get(R.Get(v_430, (V)"position"), (V)"x"));
R.Set(obj_434, key_435, values_433[0]);
var values_436 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_426))))))));
V v_437 = values_436[0];
R.Call(R.Get(v_21, (V)"remove_for_actor"), R.Join(new V[] {v_437}, new Results(R.Get(v_430, (V)"id"))));
foreach (var row_438 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_437, (V)"entities"))))))) {
V v_439 = row_438[0];
V v_440 = row_438[1];
if (R.Truth((V)(R.Get(v_440, (V)"id") == R.Get(v_430, (V)"id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_437, (V)"entities")}, new Results(v_439)));

break;
}
}
var values_441 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_426))));
V v_442 = values_441[0];
var values_443 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_426, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_444 = values_443[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_444, (V)"reflection"))));
R.Call(v_3, R.Join(new V[] {v_444}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_437, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)))));

R.Call(v_3, R.Join(new V[] {v_442}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_426)))));
var values_445 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {}, new Results(v_426))));
V v_446 = values_445[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_430, (V)"hp") == (V)95.0d), () => (V)(R.Get(v_428, (V)"hp") == (V)7.0d)), () => (V)((V)R.Length(R.Get(v_426, (V)"idea_mirrors")) == (V)1.0d)), () => (V)(R.Call(v_89, R.Join(new V[] {v_446}, new Results((V)"MirrorReflected"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a skull Vial evaluates a living wizard forcefield before committing any primary deaths"}, new Results(new V((RuleFunction)(args_447 => {

var values_448 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"volatile_vial"))));
V v_449 = values_448[0];
V v_450 = values_448[1];

R.Call(v_24, R.Join(new V[] {v_449}, new Results((V)"fateful_dice")));
var values_451 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_449, (V)2.0d, (V)0.0d}, new Results((V)"core:enemy/bodyguard_brute"))));
V v_452 = values_451[0];
var values_453 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_449, (V)2.0d, (-(V)1.0d)}, new Results((V)"core:enemy/ward_weaver"))));
V v_454 = values_453[0];

var values_455 = R.Join(new V[] {}, new Results((V)true));
var obj_456 = R.Get(v_454, (V)"ai_memory");
V key_457 = (V)"ward_ready";
R.Set(obj_456, key_457, values_455[0]);
foreach (var row_458 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_454), new Entry((V)2, v_452)))))))) {
V v_459 = row_458[0];
V v_460 = row_458[1];
var values_461 = R.Join(new V[] {}, new Results((V)"vial-batch"));
var obj_462 = v_460;
V key_463 = (V)"squad_id";
R.Set(obj_462, key_463, values_461[0]);

var values_464 = R.Join(new V[] {}, new Results((V)"warded_bodyguards"));
var obj_465 = v_460;
V key_466 = (V)"squad_kind";
R.Set(obj_465, key_466, values_464[0]);

var values_467 = R.Join(new V[] {}, new Results(R.Get(v_454, (V)"id")));
var obj_468 = v_460;
V key_469 = (V)"squad_leader_id";
R.Set(obj_468, key_469, values_467[0]);
var values_470 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_449, (V)"active_floor_id")), new Entry((V)"x", R.Get(R.Get(v_454, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_454, (V)"position"), (V)"y")))));
var obj_471 = v_460;
V key_472 = (V)"squad_home";
R.Set(obj_471, key_472, values_470[0]);
}
{ double end_475 = R.Num((V)80.0d), step_476 = R.Num((V)1.0d);
for (double i_474 = R.Num((V)1.0d); step_476 > 0 ? i_474 <= end_475 : i_474 >= end_475; i_474 += step_476) {
V v_473 = i_474;
if (R.Truth((V)(R.Call(R.Get(v_19, (V)"next_int"), R.Join(new V[] {R.Table(new Entry((V)"state", v_473))}, new Results((V)8.0d))).First == (V)7.0d))) {
var values_477 = R.Join(new V[] {}, new Results(v_473));
var obj_478 = R.Get(v_449, (V)"rng");
V key_479 = (V)"state";
R.Set(obj_478, key_479, values_477[0]);

break;
}
}
}
var values_480 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {}, new Results(v_449))));
V v_481 = values_480[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_449}, new Results(R.Get(v_454, (V)"id")))).First)), () => (V)(R.Get(v_452, (V)"hp") == (V)100.0d)), () => (V)(R.Call(v_89, R.Join(new V[] {v_481}, new Results((V)"EntityKilled"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_99, R.Join(new V[] {}, new Results(v_481))).First) == (V)2.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Split Thrust keeps the stem, forks the second step and spares its former straight endpoint"}, new Results(new V((RuleFunction)(args_482 => {

var values_483 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"split_thrust"))));
V v_484 = values_483[0];

var values_485 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_484, (V)1.0d}, new Results((V)0.0d))));
V v_486 = values_485[0];

var values_487 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_484, (V)2.0d}, new Results((V)0.0d))));
V v_488 = values_487[0];
var values_489 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_484, (V)2.0d}, new Results((-(V)1.0d)))));
V v_490 = values_489[0];

var values_491 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_484, (V)2.0d}, new Results((V)1.0d))));
V v_492 = values_491[0];
var values_493 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_484, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_494 = values_493[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_494, (V)"cells")) == (V)3.0d), () => (V)(!R.Truth(R.Call(v_81, R.Join(new V[] {R.Get(v_494, (V)"cells"), R.Get(R.Get(v_488, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_488, (V)"position"), (V)"y")))).First))))));
var values_495 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {}, new Results(v_484))));
V v_496 = values_495[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_486, (V)"hp") == (V)94.0d), () => (V)(R.Get(v_490, (V)"hp") == (V)94.0d)), () => (V)(R.Get(v_492, (V)"hp") == (V)94.0d)), () => (V)(R.Get(v_488, (V)"hp") == (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_99, R.Join(new V[] {}, new Results(v_496))).First) == (V)3.0d), () => (V)(R.Get(v_484, (V)"clock") == (V)87.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"forked thrust clips each branch independently and leaves bump unchanged"}, new Results(new V((RuleFunction)(args_497 => {

var values_498 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"split_thrust"))));
V v_499 = values_498[0];
V v_500 = values_498[1];

var values_501 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_499, (V)1.0d}, new Results((V)0.0d))));
V v_502 = values_501[0];
var values_503 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_door"), R.Join(new V[] {v_499, (V)(R.Get(R.Get(v_500, (V)"position"), (V)"x") + (V)1.0d)}, new Results((V)(R.Get(R.Get(v_500, (V)"position"), (V)"y") - (V)1.0d)))));
V v_504 = values_503[0];
var values_505 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_499, (V)2.0d}, new Results((-(V)1.0d)))));
V v_506 = values_505[0];

var values_507 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_499, (V)2.0d}, new Results((V)1.0d))));
V v_508 = values_507[0];
R.Call(v_62, R.Join(new V[] {}, new Results(v_499)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_502, (V)"hp") == (V)94.0d), () => (V)(R.Get(v_506, (V)"hp") == (V)100.0d)), () => (V)(R.Get(v_508, (V)"hp") == (V)94.0d)), () => (V)(!R.Truth(R.Get(v_504, (V)"open")))))));
var values_509 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"split_thrust"))));
v_499 = values_509[0];

var values_510 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_499, (V)1.0d}, new Results((V)0.0d))));
v_502 = values_510[0];

var values_511 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_499, (V)2.0d}, new Results((-(V)1.0d)))));
v_506 = values_511[0];

var values_512 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_499, (V)2.0d}, new Results((V)1.0d))));
v_508 = values_512[0];
R.Call(v_62, R.Join(new V[] {v_499}, new Results((V)"move")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_502, (V)"hp") == (V)94.0d), () => (V)(R.Get(v_506, (V)"hp") == (V)100.0d)), () => (V)(R.Get(v_508, (V)"hp") == (V)100.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all seven forms keep one Vow cost and one primary critical roll for every batch"}, new Results(new V((RuleFunction)(args_513 => {

foreach (var row_514 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_23)))))) {
V v_515 = row_514[0];
V v_516 = row_514[1];
var values_517 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results(v_516))));
V v_518 = values_517[0];
V v_519 = values_517[1];

R.Call(v_24, R.Join(new V[] {v_518}, new Results((V)"pilgrims_vow")));

var values_520 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_521 = v_519;
V key_522 = (V)"hp";
R.Set(obj_521, key_522, values_520[0]);

var values_523 = R.Join(new V[] {}, new Results((V)1000000.0d));
var obj_524 = v_519;
V key_525 = (V)"crit_chance_ppm";
R.Set(obj_524, key_525, values_523[0]);
R.Call(v_42, R.Join(new V[] {v_518, (V)1.0d}, new Results((V)0.0d)));

R.Call(v_42, R.Join(new V[] {v_518, (V)2.0d}, new Results((V)1.0d)));
var values_526 = R.Join(new V[] {}, R.Call(rt.Global("select"), R.Join(new V[] {(V)2.0d}, R.Call(R.Get(v_5, (V)"effective_ability"), R.Join(new V[] {v_518, R.Call(v_59, R.Join(new V[] {}, new Results(v_516))).First, (V)1.0d, (V)0.0d}, new Results((V)true))))));
V v_527 = values_526[0];
var values_528 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_518, R.Call(v_59, R.Join(new V[] {}, new Results(v_516))).First, (V)1.0d, (V)0.0d}, new Results(R.Table(new Entry((V)"vow", (V)true))))));
V v_529 = values_528[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_519, (V)"hp") == (V)8.0d), () => (V)(R.Call(v_89, R.Join(new V[] {v_529}, new Results((V)"VowPaid"))).First == (V)1.0d)), () => (V)(R.Call(v_89, R.Join(new V[] {v_529}, new Results((V)"AttackPerformed"))).First == (V)1.0d))}, new Results(v_516)));
foreach (var row_530 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_529)))))) {
V v_531 = row_530[0];
V v_532 = row_530[1];
if (R.Truth((V)(R.Get(v_532, (V)"kind") == (V)"AttackPerformed"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_532, (V)"raw_damage") == R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)((V)(v_527 + (V)6.0d) * R.Get(v_519, (V)"crit_damage_percent")) / (V)100.0d) + (V)0.5d)))).First)}, new Results(v_516)));
}
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a dice cross stays a paid genuine miss for every form with no new hidden Echo or Burning"}, new Results(new V((RuleFunction)(args_533 => {

foreach (var row_534 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_23)))))) {
V v_535 = row_534[0];
V v_536 = row_534[1];
var values_537 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results(v_536))));
V v_538 = values_537[0];

var values_539 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_538, (V)1.0d}, new Results((V)0.0d))));
V v_540 = values_539[0];

R.Call(v_42, R.Join(new V[] {v_538, (V)2.0d}, new Results((V)1.0d)));
R.Call(v_24, R.Join(new V[] {v_538}, new Results((V)"fateful_dice")));

R.Call(v_24, R.Join(new V[] {v_538}, new Results((V)"temporal_echo")));

R.Call(v_24, R.Join(new V[] {v_538}, new Results((V)"emberbrand")));
{ double end_543 = R.Num((V)80.0d), step_544 = R.Num((V)1.0d);
for (double i_542 = R.Num((V)1.0d); step_544 > 0 ? i_542 <= end_543 : i_542 >= end_543; i_542 += step_544) {
V v_541 = i_542;
if (R.Truth((V)(R.Call(R.Get(v_19, (V)"next_int"), R.Join(new V[] {R.Table(new Entry((V)"state", v_541))}, new Results((V)8.0d))).First == (V)8.0d))) {
var values_545 = R.Join(new V[] {}, new Results(v_541));
var obj_546 = R.Get(v_538, (V)"rng");
V key_547 = (V)"state";
R.Set(obj_546, key_547, values_545[0]);

break;
}
}
}
var values_548 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_538}, R.Call(v_59, R.Join(new V[] {}, new Results(v_536))))));
V v_549 = values_548[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Get(v_540, (V)"hp") == (V)100.0d), () => (V)(R.Call(v_89, R.Join(new V[] {v_549}, new Results((V)"DamageTaken"))).First == (V)0.0d)), () => (V)(R.Call(v_89, R.Join(new V[] {v_549}, new Results((V)"BoonActivated"))).First == (V)0.0d)), () => (V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_538, (V)"delayed_effects")))).First)))}, new Results(v_536)));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"mixed rarity uncapped copies change power once without multiplying weapon cells or consuming preview RNG"}, new Results(new V((RuleFunction)(args_550 => {

foreach (var row_551 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_23)))))) {
V v_552 = row_551[0];
V v_553 = row_551[1];
var values_554 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results(v_553))));
V v_555 = values_554[0];
V v_556 = values_554[1];

R.Call(v_42, R.Join(new V[] {v_555, (V)1.0d}, new Results((V)0.0d)));

R.Call(v_42, R.Join(new V[] {v_555, (V)2.0d}, new Results((V)1.0d)));
var values_557 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"effective_ability"), R.Join(new V[] {v_555, R.Call(v_59, R.Join(new V[] {}, new Results(v_553))).First, (V)1.0d, (V)0.0d}, new Results((V)true))));
V v_558 = values_557[0];
R.Call(v_24, R.Join(new V[] {v_555, v_553, (V)"common"}, new Results((V)10.0d)));

R.Call(v_24, R.Join(new V[] {v_555, v_553, (V)"legendary"}, new Results((V)20.0d)));
var values_559 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_555))));
V v_560 = values_559[0];
{ double end_563 = R.Num((V)20.0d), step_564 = R.Num((V)1.0d);
for (double i_562 = R.Num((V)1.0d); step_564 > 0 ? i_562 <= end_563 : i_562 >= end_563; i_562 += step_564) {
V v_561 = i_562;
R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_555, R.Call(v_59, R.Join(new V[] {}, new Results(v_553))).First, (V)1.0d}, new Results((V)0.0d)));
}
}
R.Call(v_3, R.Join(new V[] {v_560}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_555)))));
var values_565 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"effective_ability"), R.Join(new V[] {v_555, R.Call(v_59, R.Join(new V[] {}, new Results(v_553))).First, (V)1.0d, (V)0.0d}, new Results((V)true))));
V v_566 = values_565[0];
V v_567 = values_565[1];
R.Call(v_3, R.Join(new V[] {v_558}, new Results(v_566)));
var values_568 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Call(v_59, R.Join(new V[] {}, new Results(v_553))).First == (V)"basic_attack"), () => R.Get(v_556, (V)"damage")), () => R.Get(v_556, (V)"slash_damage"))));
V v_569 = values_568[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_567 == (V)(v_569 + R.Call(R.Get(v_9, (V)"aggregate"), R.Join(new V[] {R.Concat((V)"core:boon/", v_553), R.Get(R.Get(v_556, (V)"boons"), R.Concat((V)"core:boon/", v_553))}, new Results((V)"power"))).First)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"each saved form continues exactly with pending Echo/Burning and later transformed attacks"}, new Results(new V((RuleFunction)(args_570 => {

foreach (var row_571 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_23)))))) {
V v_572 = row_571[0];
V v_573 = row_571[1];
var values_574 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results(v_573))));
V v_575 = values_574[0];

R.Call(v_42, R.Join(new V[] {v_575, (V)1.0d, (V)0.0d, V.Nil}, new Results((V)1000.0d)));
R.Call(v_24, R.Join(new V[] {v_575, (V)"temporal_echo"}, new Results((V)"common")));

R.Call(v_24, R.Join(new V[] {v_575, (V)"emberbrand"}, new Results((V)"common")));
R.Call(v_62, R.Join(new V[] {}, new Results(v_575)));

R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_575, (V)"delayed_effects")))).First}, new Results(v_573)));
var values_576 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_575))))))))));
V v_577 = values_576[0];
foreach (var row_578 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"wait"), new Entry((V)2, R.Call(v_59, R.Join(new V[] {}, new Results(v_573))).First), new Entry((V)3, (V)"wait"), new Entry((V)4, (V)"basic_attack")))))))) {
V v_579 = row_578[0];
V v_580 = row_578[1];
R.Call(v_3, R.Join(new V[] {R.Call(v_62, R.Join(new V[] {v_575}, new Results(v_580))).First}, R.Call(v_62, R.Join(new V[] {v_577}, new Results(v_580)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_575))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_577)))));
}
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
