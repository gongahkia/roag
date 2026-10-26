// Static C# port of tests/idea_control_boon_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 993aa301f8a9cfdc902286860708df92f6a83d1c15f2e06d1e0df8a7c3919ec5
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_control_boon_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_17 = values_16[0];
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
V v_22 = args_19[2];
V v_23 = args_19[3];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_20, R.Get(v_20, (V)"player_id"), R.Concat((V)"core:boon/", v_21), R.Or(v_22, () => (V)"rare")}, new Results(R.Or(v_23, () => (V)1.0d))));
return Results.Empty;
}));
V v_24 = V.Nil;
v_24 = new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
V v_27 = args_25[1];
var values_28 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", v_27))))));
V v_29 = values_28[0];

var values_30 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_29))));
V v_31 = values_30[0];
if (R.Truth(v_26)) {
R.Call(v_18, R.Join(new V[] {v_29}, new Results(v_26)));
}
return R.Join(new V[] {v_29}, new Results(v_31));
return Results.Empty;
}));
V v_32 = V.Nil;
v_32 = new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
V v_35 = args_33[1];
V v_36 = args_33[2];
V v_37 = args_33[3];
V v_38 = args_33[4];
var values_39 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_34, R.Or(v_37, () => (V)"core:enemy/mossbound_guard"), R.Or(v_35, () => (V)3.0d), R.Or(v_36, () => (V)2.0d)}, new Results(R.Or(v_38, () => (V)100000.0d)))));
V v_40 = values_39[0];
var values_41 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_42 = v_40;
V key_43 = (V)"hp";
R.Set(obj_42, key_43, values_41[0]);

var values_44 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_45 = v_40;
V key_46 = (V)"max_hp";
R.Set(obj_45, key_46, values_44[0]);

return R.Join(new V[] {}, new Results(v_40));
return Results.Empty;
}));
V v_47 = V.Nil;
v_47 = new V((RuleFunction)(args_48 => {
V v_49 = args_48[0];
V v_50 = args_48[1];
V v_51 = args_48[2];
V v_52 = args_48[3];
var values_53 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_49}, new Results(R.Table(new Entry((V)"kind", R.Or(v_50, () => (V)"basic_attack")), new Entry((V)"dx", R.Or(v_51, () => (V)1.0d)), new Entry((V)"dy", R.Or(v_52, () => (V)0.0d)))))));
V v_54 = values_53[0];
V v_55 = values_53[1];
V v_56 = values_53[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_54}, new Results(v_55)));

return R.Join(new V[] {}, new Results(v_56));
return Results.Empty;
}));
V v_57 = V.Nil;
v_57 = new V((RuleFunction)(args_58 => {
V v_59 = args_58[0];
V v_60 = args_58[1];
V v_61 = args_58[2];
var values_62 = R.Join(new V[] {}, new Results((V)0.0d));
V v_63 = values_62[0];
foreach (var row_64 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_59)))))) {
V v_65 = row_64[0];
V v_66 = row_64[1];
if (R.Truth(R.And((V)(R.Get(v_66, (V)"kind") == v_60), () => R.Or((V)(!R.Truth(v_61)), () => (V)(R.Get(v_66, (V)"boon_id") == R.Concat((V)"core:boon/", v_61)))))) {
var values_67 = R.Join(new V[] {}, new Results((V)(v_63 + (V)1.0d)));
v_63 = values_67[0];
}
}
return R.Join(new V[] {}, new Results(v_63));
return Results.Empty;
}));
V v_68 = V.Nil;
v_68 = new V((RuleFunction)(args_69 => {
V v_70 = args_69[0];
V v_71 = args_69[1];
V v_72 = args_69[2];
V v_73 = args_69[3];
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_70))));
V v_75 = values_74[0];

var values_76 = R.Join(new V[] {}, new Results(R.Or(v_73, () => (V)"beggars_cap")));
v_73 = values_76[0];

R.Call(v_18, R.Join(new V[] {v_70}, new Results(v_73)));
var values_77 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_75, (V)"boon_memory"), () => R.Table())));
var obj_78 = v_75;
V key_79 = (V)"boon_memory";
R.Set(obj_78, key_79, values_77[0]);
var values_80 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"ward", v_71), new Entry((V)"expires_at", R.Or(v_72, () => (V)500.0d)))));
var obj_81 = R.Get(v_75, (V)"boon_memory");
V key_82 = R.Concat((V)"core:boon/", v_73);
R.Set(obj_81, key_82, values_80[0]);
return Results.Empty;
}));
V v_83 = V.Nil;
v_83 = new V((RuleFunction)(args_84 => {
V v_85 = args_84[0];
V v_86 = args_84[1];
return R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_85))).First, (V)"boon_memory"), R.Concat((V)"core:boon/", v_86))));
return Results.Empty;
}));
V v_87 = V.Nil;
v_87 = new V((RuleFunction)(args_88 => {
V v_89 = args_88[0];
V v_90 = args_88[1];
V v_91 = args_88[2];
V v_92 = args_88[3];
return R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_currency"), R.Join(new V[] {v_89, R.Or(v_92, () => (V)1.0d), R.Get(v_89, (V)"active_floor_id"), v_90, v_91, new V((RuleFunction)(args_93 => {


return Results.Empty;
}))}, new Results(R.Get(v_89, (V)"player_id")))));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"six control/resource boons declare real finite effects and native icons"}, new Results(new V((RuleFunction)(args_94 => {

var values_95 = R.Join(new V[] {}, new Results(R.Table()));
V v_96 = values_95[0];
foreach (var row_97 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"grasping_hand"), new Entry((V)2, (V)"anchor_maul"), new Entry((V)3, (V)"hourglass_clasp"), new Entry((V)4, (V)"ward_ledger"), new Entry((V)5, (V)"cinder_stopper"), new Entry((V)6, (V)"scavengers_hook")))))))) {
V v_98 = row_97[0];
V v_99 = row_97[1];
var values_100 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results(R.Concat((V)"core:boon/", v_99)))))));
V v_101 = values_100[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_96, R.Get(R.Get(v_101, (V)"art"), (V)"index")))))));

var values_102 = R.Join(new V[] {}, new Results((V)true));
var obj_103 = v_96;
V key_104 = R.Get(R.Get(v_101, (V)"art"), (V)"index");
R.Set(obj_103, key_104, values_102[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))).First, (V)"boons"), R.Get(v_101, (V)"id")), (V)1.0d) == R.Get(R.Get(v_101, (V)"art"), (V)"index")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"validate"), Results.Empty)));
foreach (var row_105 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"displace_target"), new Entry((V)2, (V)"stun_target")))))))) {
V v_106 = row_105[0];
V v_107 = row_105[1];
var values_108 = R.Join(new V[] {}, new Results(R.Concat((V)"core:boon/", R.Or(R.And((V)(v_107 == (V)"displace_target"), () => (V)"anchor_maul"), () => (V)"hourglass_clasp"))));
V v_109 = values_108[0];
var values_110 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results(v_109))))))));
V v_111 = values_110[0];
if (R.Truth((V)(v_107 == (V)"displace_target"))) {
var values_112 = R.Join(new V[] {}, new Results((V)"teleport"));
var obj_113 = R.Get(R.Get(v_111, (V)"effects"), (V)1.0d);
V key_114 = (V)"mode";
R.Set(obj_113, key_114, values_112[0]);
} else {
var values_115 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_116 = R.Get(R.Get(v_111, (V)"effects"), (V)1.0d);
V key_117 = (V)"cooldown";
R.Set(obj_116, key_117, values_115[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_9, (V)"validate"), R.Table(new Entry(v_109, v_111))}, new Results(R.Table(new Entry((V)1, v_109))))).First)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Grasping Hand special hits add ordinary defended damage then pull a surviving regular foe one cell"}, new Results(new V((RuleFunction)(args_118 => {

var values_119 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {(V)"grasping_hand"}, new Results((V)"core:actor/ranger"))));
V v_120 = values_119[0];

var values_121 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_120, (V)5.0d}, new Results((V)2.0d))));
V v_122 = values_121[0];
var values_123 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_120}, new Results((V)"sweeping_slash"))));
V v_124 = values_123[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_122, (V)"position"), (V)"x") == (V)4.0d), () => (V)(R.Call(v_57, R.Join(new V[] {v_124}, new Results((V)"Displaced"))).First == (V)1.0d)))));
var values_125 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_126 = values_125[0];
V v_127 = values_125[1];
foreach (var row_128 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_124)))))) {
V v_129 = row_128[0];
V v_130 = row_128[1];
if (R.Truth(R.And((V)(R.Get(v_130, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_130, (V)"target_id") == R.Get(v_122, (V)"id"))))) {
if (R.Truth(R.Get(v_130, (V)"direct_attack"))) {
var values_131 = R.Join(new V[] {}, new Results(R.Get(v_130, (V)"actual_loss")));
v_126 = values_131[0];
} else {
var values_132 = R.Join(new V[] {}, new Results(R.Get(v_130, (V)"actual_loss")));
v_127 = values_132[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_127 == (V)2.0d), () => (V)(R.Get(v_122, (V)"hp") == (V)((V)((V)100.0d - v_126) - v_127))))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_120))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_120)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Grasping Hand cannot pull through an occupied cell and basic hits do not trigger it"}, new Results(new V((RuleFunction)(args_133 => {

var values_134 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {(V)"grasping_hand"}, new Results((V)"core:actor/ranger"))));
V v_135 = values_134[0];

var values_136 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_135, (V)5.0d}, new Results((V)2.0d))));
V v_137 = values_136[0];
R.Call(R.Get(v_5, (V)"spawn_chest"), R.Join(new V[] {v_135, (V)"core:chest/standard", (V)4.0d}, new Results((V)2.0d)));
var values_138 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_135}, new Results((V)"sweeping_slash"))));
V v_139 = values_138[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_137, (V)"position"), (V)"x") == (V)5.0d), () => (V)(R.Call(v_57, R.Join(new V[] {v_139}, new Results((V)"ShoveBlocked"))).First == (V)1.0d)))));
var values_140 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"grasping_hand"))));
v_135 = values_140[0];

var values_141 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {}, new Results(v_135))));
v_137 = values_141[0];
var values_142 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_135))));
v_139 = values_142[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_137, (V)"hp") == (V)97.0d), () => (V)(R.Call(v_57, R.Join(new V[] {v_139, (V)"BoonActivated"}, new Results((V)"grasping_hand"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Grasping Hand uses the real owner row/column even when a Spirit also owns strike-origin pattern boons"}, new Results(new V((RuleFunction)(args_143 => {

foreach (var row_144 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)false), new Entry((V)2, (V)true)))))))) {
V v_145 = row_144[0];
V v_146 = row_144[1];
var values_147 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {(V)"grasping_hand"}, new Results((V)"core:actor/spiritbinder"))));
V v_148 = values_147[0];
V v_149 = values_147[1];
var values_150 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_151 = R.Get(v_149, (V)"position");
V key_152 = (V)"x";
var obj_153 = R.Get(v_149, (V)"position");
V key_154 = (V)"y";
R.Set(obj_151, key_152, values_150[0]);
R.Set(obj_153, key_154, values_150[1]);
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))).First, (V)"place"), R.Join(new V[] {v_148, v_149, R.Table(new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)8.0d))}, new Results(new V((RuleFunction)(args_155 => {


return Results.Empty;
})))));
var values_156 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_148, (V)9.0d}, new Results((V)8.0d))));
V v_157 = values_156[0];

R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_148)));
if (R.Truth(v_146)) {
R.Call(v_18, R.Join(new V[] {v_148}, new Results((V)"linebreaker_pawn")));
}
var values_158 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_148, (V)"sweeping_slash", (V)1.0d}, new Results((V)1.0d))));
V v_159 = values_158[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_159, (V)"valid"), () => (V)(R.Get(v_159, (V)"companion_mode") == (V)"strike")))));
var values_160 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_148, (V)"sweeping_slash", (V)1.0d}, new Results((V)1.0d))));
V v_161 = values_160[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_161, (V)"BoonActivated"}, new Results((V)"grasping_hand"))).First == (V)0.0d), () => (V)(R.Get(v_157, (V)"hp") == (V)98.0d)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Anchor Maul pushes a cardinal basic victim without adding collision damage in open space"}, new Results(new V((RuleFunction)(args_162 => {

var values_163 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"anchor_maul"))));
V v_164 = values_163[0];

var values_165 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {}, new Results(v_164))));
V v_166 = values_165[0];
var values_167 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_164))));
V v_168 = values_167[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_166, (V)"position"), (V)"x") == (V)4.0d), () => (V)(R.Get(v_166, (V)"hp") == (V)97.0d)), () => (V)(R.Call(v_57, R.Join(new V[] {v_168}, new Results((V)"Displaced"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_168}, new Results((V)"DamageTaken"))).First == (V)1.0d), () => (V)(R.Call(v_57, R.Join(new V[] {v_168}, new Results((V)"ShoveBlocked"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a wall-blocked Anchor Maul collision deals defended damage without crossing masonry"}, new Results(new V((RuleFunction)(args_169 => {

var values_170 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"anchor_maul"))));
V v_171 = values_170[0];
V v_172 = values_170[1];

var values_173 = R.Join(new V[] {(V)2.0d}, new Results((V)3.0d));
var obj_174 = R.Get(v_172, (V)"position");
V key_175 = (V)"x";
var obj_176 = R.Get(v_172, (V)"position");
V key_177 = (V)"y";
R.Set(obj_174, key_175, values_173[0]);
R.Set(obj_176, key_177, values_173[1]);

R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_171)));
var values_178 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_171, (V)3.0d}, new Results((V)3.0d))));
V v_179 = values_178[0];
var values_180 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_171))));
V v_181 = values_180[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_179, (V)"position"), (V)"x") == (V)3.0d), () => (V)(R.Get(v_179, (V)"hp") == (V)95.0d)), () => (V)(R.Call(v_57, R.Join(new V[] {v_181}, new Results((V)"ShoveBlocked"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_181}, new Results((V)"DamageTaken"))).First == (V)2.0d), () => (V)(R.Call(v_57, R.Join(new V[] {v_181}, new Results((V)"Displaced"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"successful boon displacement cancels the actual locked windup and preserves its committed cells"}, new Results(new V((RuleFunction)(args_182 => {

var values_183 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"anchor_maul"))));
V v_184 = values_183[0];

var values_185 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_184, (V)3.0d, (V)2.0d, V.Nil}, new Results((V)0.0d))));
V v_186 = values_185[0];

var values_187 = R.Join(new V[] {}, new Results((V)20.0d));
var obj_188 = v_186;
V key_189 = (V)"speed";
R.Set(obj_188, key_189, values_187[0]);
R.Call(v_47, R.Join(new V[] {v_184}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_186, (V)"pending_attack"))));
var values_190 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_186, (V)"pending_attack")))))));
V v_191 = values_190[0];
var values_192 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_184))));
V v_193 = values_192[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_193}, new Results((V)"WindupCancelled"))).First == (V)1.0d), () => (V)(!R.Truth(R.Get(v_186, (V)"pending_attack")))))));
foreach (var row_194 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_193)))))) {
V v_195 = row_194[0];
V v_196 = row_194[1];
if (R.Truth((V)(R.Get(v_196, (V)"kind") == (V)"WindupCancelled"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_196, (V)"windup_id") == R.Get(v_191, (V)"schedule_id")))));

R.Call(v_3, R.Join(new V[] {R.Get(v_196, (V)"target_cells")}, new Results(R.Get(v_191, (V)"target_cells"))));
}
}
foreach (var row_197 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_184, (V)"schedule"))))))) {
V v_198 = row_197[0];
V v_199 = row_197[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_199, (V)"id") != R.Get(v_191, (V)"schedule_id")))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all miniboss/finale/special and reward-exempt targets resist the three regular-only control boons"}, new Results(new V((RuleFunction)(args_200 => {

var values_201 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:enemy/hollow_regent"), new Entry((V)2, (V)"core:enemy/rotating_sentinel"), new Entry((V)3, (V)"core:enemy/extraction_pursuer"))));
V v_202 = values_201[0];
foreach (var row_203 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))).First, (V)"order"))))))) {
V v_204 = row_203[0];
V v_205 = row_203[1];
var values_206 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))).First, (V)"get"), R.Join(new V[] {}, new Results(v_205))).First, (V)"elite_id")));
var obj_207 = v_202;
V key_208 = (V)((V)R.Length(v_202) + (V)1.0d);
R.Set(obj_207, key_208, values_206[0]);
}
foreach (var row_209 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"grasping_hand"), new Entry((V)2, (V)"anchor_maul"), new Entry((V)3, (V)"hourglass_clasp")))))))) {
V v_210 = row_209[0];
V v_211 = row_209[1];
foreach (var row_212 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_202)))))) {
V v_213 = row_212[0];
V v_214 = row_212[1];
var values_215 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results(v_211))));
V v_216 = values_215[0];

var values_217 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_216, (V)3.0d, (V)2.0d}, new Results(v_214))));
V v_218 = values_217[0];
var values_219 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_216}, new Results(R.Or(R.And((V)(v_211 == (V)"grasping_hand"), () => (V)"sweeping_slash"), () => (V)"basic_attack")))));
V v_220 = values_219[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(v_57, R.Join(new V[] {v_220, (V)"BoonActivated"}, new Results(v_211))).First == (V)0.0d), () => (V)(R.Get(R.Get(v_218, (V)"position"), (V)"x") == (V)3.0d))}, new Results(R.Concat(v_211, v_214))));
}
var values_221 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results(v_211))));
V v_222 = values_221[0];

var values_223 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {}, new Results(v_222))));
V v_224 = values_223[0];

var values_225 = R.Join(new V[] {}, new Results((V)true));
var obj_226 = v_224;
V key_227 = (V)"reward_exempt";
R.Set(obj_226, key_227, values_225[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_57, R.Join(new V[] {R.Call(v_47, R.Join(new V[] {v_222}, new Results((V)"sweeping_slash"))).First, (V)"BoonActivated"}, new Results(v_211))).First == (V)0.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Hourglass stuns only the first surviving primary victim per attack and never accelerates later recovery"}, new Results(new V((RuleFunction)(args_228 => {

var values_229 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"hourglass_clasp"))));
V v_230 = values_229[0];
V v_231 = values_229[1];

var values_232 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_233 = R.Get(v_231, (V)"position");
V key_234 = (V)"x";
var obj_235 = R.Get(v_231, (V)"position");
V key_236 = (V)"y";
R.Set(obj_233, key_234, values_232[0]);
R.Set(obj_235, key_236, values_232[1]);

R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_230)));
var values_237 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Call(v_32, R.Join(new V[] {v_230, (V)8.0d}, new Results((V)5.0d))).First), new Entry((V)2, R.Call(v_32, R.Join(new V[] {v_230, (V)8.0d}, new Results((V)6.0d))).First), new Entry(3, R.Call(v_32, R.Join(new V[] {v_230, (V)8.0d}, new Results((V)7.0d)))))));
V v_238 = values_237[0];
var values_239 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_230}, new Results((V)"sweeping_slash"))));
V v_240 = values_239[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_240}, new Results((V)"Stunned"))).First == (V)1.0d), () => (V)(R.Get(R.Call(v_83, R.Join(new V[] {v_230}, new Results((V)"hourglass_clasp"))).First, (V)"expires_at") == (V)400.0d)))));
var values_241 = R.Join(new V[] {}, new Results((V)0.0d));
V v_242 = values_241[0];
foreach (var row_243 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_238)))))) {
V v_244 = row_243[0];
V v_245 = row_243[1];
if (R.Truth((V)(R.Get(v_245, (V)"stunned_until") > (V)0.0d))) {
var values_246 = R.Join(new V[] {}, new Results((V)(v_242 + (V)1.0d)));
v_242 = values_246[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_245, (V)"stunned_until") == (V)30.0d))));
foreach (var row_247 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_230, (V)"schedule"))))))) {
V v_248 = row_247[0];
V v_249 = row_247[1];
if (R.Truth((V)(R.Get(v_249, (V)"actor_id") == R.Get(v_245, (V)"id")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_249, (V)"due") == (V)100000.0d))));
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_242 == (V)1.0d), () => (V)(R.Get(v_230, (V)"clock") == (V)150.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Hourglass cooldown has an exact 400-time boundary and uncapped stacks cannot exceed its 100-time stun cap"}, new Results(new V((RuleFunction)(args_250 => {

var values_251 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"hourglass_clasp"))));
V v_252 = values_251[0];

R.Call(v_32, R.Join(new V[] {}, new Results(v_252)));
R.Call(v_18, R.Join(new V[] {v_252, (V)"hourglass_clasp", (V)"legendary"}, new Results((V)20.0d)));
var values_253 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_252))));
V v_254 = values_253[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_57, R.Join(new V[] {v_254}, new Results((V)"Stunned"))).First == (V)1.0d))));
foreach (var row_255 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_254)))))) {
V v_256 = row_255[0];
V v_257 = row_255[1];
if (R.Truth((V)(R.Get(v_257, (V)"kind") == (V)"Stunned"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_257, (V)"until_time") == (V)100.0d))));
}
}
{ double end_260 = R.Num((V)3.0d), step_261 = R.Num((V)1.0d);
for (double i_259 = R.Num((V)1.0d); step_261 > 0 ? i_259 <= end_260 : i_259 >= end_260; i_259 += step_261) {
V v_258 = i_259;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_57, R.Join(new V[] {R.Call(v_47, R.Join(new V[] {}, new Results(v_252))).First}, new Results((V)"Stunned"))).First == (V)0.0d))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_252, (V)"clock") == (V)400.0d), () => (V)(R.Call(v_57, R.Join(new V[] {R.Call(v_47, R.Join(new V[] {}, new Results(v_252))).First}, new Results((V)"Stunned"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Hourglass cancels a real windup without requiring an extra player reaction turn"}, new Results(new V((RuleFunction)(args_262 => {

var values_263 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"hourglass_clasp"))));
V v_264 = values_263[0];

var values_265 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_264, (V)3.0d, (V)2.0d, V.Nil}, new Results((V)0.0d))));
V v_266 = values_265[0];

var values_267 = R.Join(new V[] {}, new Results((V)20.0d));
var obj_268 = v_266;
V key_269 = (V)"speed";
R.Set(obj_268, key_269, values_267[0]);
R.Call(v_47, R.Join(new V[] {v_264}, new Results((V)"wait")));

var values_270 = R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_266, (V)"pending_attack")))).First, (V)"schedule_id")));
V v_271 = values_270[0];
var values_272 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_264))));
V v_273 = values_272[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_273}, new Results((V)"Stunned"))).First == (V)1.0d), () => (V)(R.Call(v_57, R.Join(new V[] {v_273}, new Results((V)"WindupCancelled"))).First == (V)1.0d)))));
foreach (var row_274 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_273)))))) {
V v_275 = row_274[0];
V v_276 = row_274[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.And((V)(R.Get(v_276, (V)"kind") == (V)"DamageBatchResolved"), () => (V)(R.Get(v_276, (V)"windup_id") == v_271)))))));
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_264))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_264)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Ward Ledger spends active wards in catalogue order, caps healing and leaves no reusable capacity"}, new Results(new V((RuleFunction)(args_277 => {

var values_278 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"ward_ledger"))));
V v_279 = values_278[0];
V v_280 = values_278[1];

var values_281 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_282 = v_280;
V key_283 = (V)"hp";
R.Set(obj_282, key_283, values_281[0]);
R.Call(v_68, R.Join(new V[] {v_279, (V)2.0d, (V)500.0d}, new Results((V)"beggars_cap")));

R.Call(v_68, R.Join(new V[] {v_279, (V)6.0d, (V)500.0d}, new Results((V)"overflow_chalice")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"ward_total"), R.Join(new V[] {v_279}, new Results(v_280))).First == (V)8.0d))));
var values_284 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_279}, new Results((V)"wait"))));
V v_285 = values_284[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_280, (V)"hp") == (V)13.0d), () => (V)(R.Call(R.Get(v_7, (V)"ward_total"), R.Join(new V[] {v_279}, new Results(v_280))).First == (V)0.0d)), () => (V)(R.Call(v_57, R.Join(new V[] {v_285}, new Results((V)"Healed"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(v_83, R.Join(new V[] {v_279}, new Results((V)"beggars_cap"))).First, (V)"ward") == (V)0.0d), () => (V)(R.Get(R.Call(v_83, R.Join(new V[] {v_279}, new Results((V)"overflow_chalice"))).First, (V)"ward") == (V)0.0d)))));
R.Call(v_47, R.Join(new V[] {v_279}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_280, (V)"hp") == (V)13.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Ward Ledger cannot spend expired wards or full-health wards, and cannot overheal"}, new Results(new V((RuleFunction)(args_286 => {

var values_287 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"ward_ledger"))));
V v_288 = values_287[0];
V v_289 = values_287[1];

R.Call(v_68, R.Join(new V[] {v_288}, new Results((V)6.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {R.Call(v_47, R.Join(new V[] {v_288}, new Results((V)"wait"))).First, (V)"BoonActivated"}, new Results((V)"ward_ledger"))).First == (V)0.0d), () => (V)(R.Get(R.Call(v_83, R.Join(new V[] {v_288}, new Results((V)"beggars_cap"))).First, (V)"ward") == (V)6.0d)))));
var values_290 = R.Join(new V[] {}, new Results((V)23.0d));
var obj_291 = v_289;
V key_292 = (V)"hp";
R.Set(obj_291, key_292, values_290[0]);
var values_293 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_288}, new Results((V)"wait"))));
V v_294 = values_293[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_289, (V)"hp") == (V)24.0d), () => (V)(R.Call(R.Get(v_7, (V)"ward_total"), R.Join(new V[] {v_288}, new Results(v_289))).First == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_57, R.Join(new V[] {v_294}, new Results((V)"Healed"))).First == (V)1.0d))));
var values_295 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"ward_ledger"))));
v_288 = values_295[0];
v_289 = values_295[1];

var values_296 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_297 = v_289;
V key_298 = (V)"hp";
R.Set(obj_297, key_298, values_296[0]);

R.Call(v_68, R.Join(new V[] {v_288, (V)6.0d}, new Results((V)0.0d)));
R.Call(v_47, R.Join(new V[] {v_288, (V)"move", (V)0.0d}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_288, (V)"clock") == (V)100.0d))));
var values_299 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_288}, new Results((V)"wait"))));
v_294 = values_299[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_289, (V)"hp") == (V)10.0d), () => (V)(R.Call(v_57, R.Join(new V[] {v_294, (V)"BoonActivated"}, new Results((V)"ward_ledger"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Cinder Stopper consumes real remaining Burning and removes its exact scheduled tick"}, new Results(new V((RuleFunction)(args_300 => {

var values_301 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"cinder_stopper"))));
V v_302 = values_301[0];

var values_303 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {}, new Results(v_302))));
V v_304 = values_303[0];
R.Call(v_18, R.Join(new V[] {v_302, (V)"emberbrand"}, new Results((V)"common")));

R.Call(v_47, R.Join(new V[] {}, new Results(v_302)));
var values_305 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_304, (V)"statuses"), (V)"burning")))));
V v_306 = values_305[0];

var values_307 = R.Join(new V[] {}, new Results(R.Get(v_306, (V)"schedule_id")));
V v_308 = values_307[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_306, (V)"power") == (V)2.0d), () => (V)(R.Get(v_306, (V)"remaining") == (V)2.0d)))));
R.Call(v_18, R.Join(new V[] {v_302, (V)"emberbrand", (V)"common"}, new Results((V)0.0d)));
var values_309 = R.Join(new V[] {}, new Results(R.Get(v_304, (V)"hp")));
V v_310 = values_309[0];

var values_311 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_302))));
V v_312 = values_311[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(R.Get(v_304, (V)"statuses"), (V)"burning"))), () => (V)(R.Get(v_304, (V)"hp") == (V)((V)(v_310 - (V)3.0d) - (V)3.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_312}, new Results((V)"StatusCleansed"))).First == (V)1.0d), () => (V)(R.Call(v_57, R.Join(new V[] {v_312}, new Results((V)"StatusTick"))).First == (V)0.0d)))));
foreach (var row_313 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_302, (V)"schedule"))))))) {
V v_314 = row_313[0];
V v_315 = row_313[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_315, (V)"id") != v_308))));
}
var values_316 = R.Join(new V[] {}, new Results(R.Get(v_304, (V)"hp")));
V v_317 = values_316[0];

R.Call(v_47, R.Join(new V[] {v_302}, new Results((V)"wait")));

R.Call(v_47, R.Join(new V[] {v_302}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_304, (V)"hp") == v_317))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_302))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_302)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Cinder Stopper ignores newly ignited and blocked hits, and later Emberbrand may ignite normally again"}, new Results(new V((RuleFunction)(args_318 => {

var values_319 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"cinder_stopper"))));
V v_320 = values_319[0];

var values_321 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {}, new Results(v_320))));
V v_322 = values_321[0];

R.Call(v_18, R.Join(new V[] {v_320, (V)"emberbrand"}, new Results((V)"common")));
var values_323 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_320))));
V v_324 = values_323[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_324, (V)"BoonActivated"}, new Results((V)"cinder_stopper"))).First == (V)0.0d), () => R.Get(R.Get(v_322, (V)"statuses"), (V)"burning")))));
var values_325 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_320))));
v_324 = values_325[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_324}, new Results((V)"StatusCleansed"))).First == (V)1.0d), () => R.Get(R.Get(v_322, (V)"statuses"), (V)"burning")))));
var values_326 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_327 = v_322;
V key_328 = (V)"defense";
R.Set(obj_327, key_328, values_326[0]);

var values_329 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_322, (V)"statuses"), (V)"burning"), (V)"schedule_id")));
V v_330 = values_329[0];
var values_331 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_320))));
v_324 = values_331[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_324, (V)"BoonActivated"}, new Results((V)"cinder_stopper"))).First == (V)0.0d), () => R.Get(R.Get(v_322, (V)"statuses"), (V)"burning")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(R.Get(R.Get(v_322, (V)"statuses"), (V)"burning"), (V)"schedule_id") != v_330), () => (V)(R.Get(R.Get(R.Get(v_322, (V)"statuses"), (V)"burning"), (V)"next_due") > R.Get(v_320, (V)"clock"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Scavenger Hook gathers a rarity-limited number of tiles in stable north/east/south/west order"}, new Results(new V((RuleFunction)(args_332 => {

var values_333 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"scavengers_hook"))));
V v_334 = values_333[0];
V v_335 = values_333[1];

var values_336 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_337 = R.Get(v_335, (V)"position");
V key_338 = (V)"x";
var obj_339 = R.Get(v_335, (V)"position");
V key_340 = (V)"y";
R.Set(obj_337, key_338, values_336[0]);
R.Set(obj_339, key_340, values_336[1]);

R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_334)));
R.Call(v_18, R.Join(new V[] {v_334, (V)"scavengers_hook", (V)"rare"}, new Results((V)0.0d)));

R.Call(v_18, R.Join(new V[] {v_334, (V)"scavengers_hook"}, new Results((V)"common")));
var values_341 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Call(v_87, R.Join(new V[] {v_334, (V)7.0d, (V)5.0d}, new Results((V)1.0d))).First), new Entry((V)2, R.Call(v_87, R.Join(new V[] {v_334, (V)8.0d, (V)6.0d}, new Results((V)2.0d))).First), new Entry((V)3, R.Call(v_87, R.Join(new V[] {v_334, (V)7.0d, (V)7.0d}, new Results((V)3.0d))).First), new Entry(4, R.Call(v_87, R.Join(new V[] {v_334, (V)6.0d, (V)6.0d}, new Results((V)4.0d)))))));
V v_342 = values_341[0];
var values_343 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_334, (V)"rng"), (V)"state")));
V v_344 = values_343[0];

var values_345 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_334}, new Results((V)"wait"))));
V v_346 = values_345[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_334, (V)"currency") == (V)1.0d), () => R.Get(R.Get(v_342, (V)1.0d), (V)"collected")), () => (V)(!R.Truth(R.Get(R.Get(v_342, (V)2.0d), (V)"collected")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_57, R.Join(new V[] {v_346}, new Results((V)"CurrencyCollected"))).First == (V)1.0d), () => (V)(R.Get(v_334, (V)"clock") == (V)100.0d)), () => (V)(R.Get(R.Get(v_334, (V)"rng"), (V)"state") == v_344)))));
R.Call(v_18, R.Join(new V[] {v_334, (V)"scavengers_hook", (V)"common"}, new Results((V)0.0d)));

R.Call(v_18, R.Join(new V[] {v_334, (V)"scavengers_hook"}, new Results((V)"rare")));
R.Call(v_47, R.Join(new V[] {v_334}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_334, (V)"currency") == (V)10.0d))));
foreach (var row_347 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_342)))))) {
V v_348 = row_347[0];
V v_349 = row_347[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_349, (V)"collected"))));
}
R.Call(v_47, R.Join(new V[] {v_334}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_334, (V)"currency") == (V)10.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Scavenger Hook cannot gather through walls or closed doors, and opening a door exposes the pickup safely"}, new Results(new V((RuleFunction)(args_350 => {

var values_351 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"scavengers_hook"))));
V v_352 = values_351[0];
var values_353 = R.Join(new V[] {}, R.Call(v_87, R.Join(new V[] {v_352, (V)2.0d}, new Results((V)1.0d))));
V v_354 = values_353[0];

var values_355 = R.Join(new V[] {}, R.Call(v_87, R.Join(new V[] {v_352, (V)3.0d}, new Results((V)2.0d))));
V v_356 = values_355[0];

var values_357 = R.Join(new V[] {}, R.Call(v_87, R.Join(new V[] {v_352, (V)2.0d}, new Results((V)3.0d))));
V v_358 = values_357[0];
var values_359 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_door"), R.Join(new V[] {v_352, (V)3.0d}, new Results((V)2.0d))));
V v_360 = values_359[0];
R.Call(v_47, R.Join(new V[] {v_352}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_358, (V)"collected"), () => (V)(!R.Truth(R.Get(v_356, (V)"collected")))), () => (V)(!R.Truth(R.Get(v_354, (V)"collected")))))));
var values_361 = R.Join(new V[] {}, new Results((V)true));
var obj_362 = v_360;
V key_363 = (V)"open";
R.Set(obj_362, key_363, values_361[0]);

var values_364 = R.Join(new V[] {}, new Results((V)false));
var obj_365 = v_360;
V key_366 = (V)"blocks_movement";
R.Set(obj_365, key_366, values_364[0]);

var values_367 = R.Join(new V[] {}, new Results((V)false));
var obj_368 = v_360;
V key_369 = (V)"blocks_vision";
R.Set(obj_368, key_369, values_367[0]);

var values_370 = R.Join(new V[] {}, new Results((V)false));
var obj_371 = v_360;
V key_372 = (V)"blocks_projectiles";
R.Set(obj_371, key_372, values_370[0]);
R.Call(v_47, R.Join(new V[] {v_352}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_356, (V)"collected"), () => (V)(!R.Truth(R.Get(v_354, (V)"collected")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"full-health food and carried drops do not spend the Hook tile budget"}, new Results(new V((RuleFunction)(args_373 => {

var values_374 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"scavengers_hook"))));
V v_375 = values_374[0];
V v_376 = values_374[1];

var values_377 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_378 = R.Get(v_376, (V)"position");
V key_379 = (V)"x";
var obj_380 = R.Get(v_376, (V)"position");
V key_381 = (V)"y";
R.Set(obj_378, key_379, values_377[0]);
R.Set(obj_380, key_381, values_377[1]);

R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_375)));
R.Call(v_18, R.Join(new V[] {v_375, (V)"scavengers_hook", (V)"rare"}, new Results((V)0.0d)));

R.Call(v_18, R.Join(new V[] {v_375, (V)"scavengers_hook"}, new Results((V)"common")));
var values_383 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_supply"), R.Join(new V[] {v_375, (V)"food", R.Get(v_375, (V)"active_floor_id"), (V)7.0d, (V)5.0d, new V((RuleFunction)(args_382 => {


return Results.Empty;
}))}, new Results(R.Get(v_376, (V)"id")))));
V v_384 = values_383[0];
var values_385 = R.Join(new V[] {}, R.Call(v_87, R.Join(new V[] {v_375, (V)7.0d}, new Results((V)5.0d))));
V v_386 = values_385[0];

var values_387 = R.Join(new V[] {}, new Results((V)999.0d));
var obj_388 = v_386;
V key_389 = (V)"carried_by";
R.Set(obj_388, key_389, values_387[0]);
var values_390 = R.Join(new V[] {}, R.Call(v_87, R.Join(new V[] {v_375, (V)8.0d}, new Results((V)6.0d))));
V v_391 = values_390[0];
R.Call(v_47, R.Join(new V[] {v_375}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(v_384, (V)"collected"))), () => (V)(!R.Truth(R.Get(v_386, (V)"collected")))), () => R.Get(v_391, (V)"collected")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"gathered supplies and gold retain normal healing, cleanse/speed and finite pickup-boon triggers"}, new Results(new V((RuleFunction)(args_392 => {

var values_393 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"scavengers_hook"))));
V v_394 = values_393[0];
V v_395 = values_393[1];

var values_396 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_397 = R.Get(v_395, (V)"position");
V key_398 = (V)"x";
var obj_399 = R.Get(v_395, (V)"position");
V key_400 = (V)"y";
R.Set(obj_397, key_398, values_396[0]);
R.Set(obj_399, key_400, values_396[1]);

var values_401 = R.Join(new V[] {}, new Results((V)6.0d));
var obj_402 = v_395;
V key_403 = (V)"hp";
R.Set(obj_402, key_403, values_401[0]);

R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_394)));
R.Call(v_18, R.Join(new V[] {v_394}, new Results((V)"road_flask")));

R.Call(v_18, R.Join(new V[] {v_394}, new Results((V)"field_dressing")));

R.Call(v_18, R.Join(new V[] {v_394, (V)"prospectors_heart"}, new Results((V)"common")));
R.Call(R.Get(v_11, (V)"spawn_supply"), R.Join(new V[] {v_394, (V)"food", R.Get(v_394, (V)"active_floor_id"), (V)7.0d, (V)5.0d, new V((RuleFunction)(args_404 => {


return Results.Empty;
}))}, new Results(R.Get(v_395, (V)"id"))));
R.Call(v_87, R.Join(new V[] {v_394, (V)8.0d, (V)6.0d}, new Results((V)2.0d)));
R.Call(R.Get(v_11, (V)"spawn_supply"), R.Join(new V[] {v_394, (V)"health", R.Get(v_394, (V)"active_floor_id"), (V)7.0d, (V)7.0d, new V((RuleFunction)(args_405 => {


return Results.Empty;
}))}, new Results(R.Get(v_395, (V)"id"))));
var values_406 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_394}, new Results((V)"wait"))));
V v_407 = values_406[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_57, R.Join(new V[] {v_407}, new Results((V)"SupplyCollected"))).First == (V)2.0d), () => (V)(R.Call(v_57, R.Join(new V[] {v_407}, new Results((V)"CurrencyCollected"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_395, (V)"hp") == (V)24.0d), () => (V)(R.Get(v_394, (V)"currency") == (V)2.0d)), () => (V)(R.Get(R.Call(v_83, R.Join(new V[] {v_394}, new Results((V)"road_flask"))).First, (V)"speed") == (V)20.0d)), () => (V)(R.Get(v_394, (V)"clock") == (V)100.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_394))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_394)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"saved cooldowns, wards and pending Burning continue identically through real later control and Wait actions"}, new Results(new V((RuleFunction)(args_408 => {

var values_409 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)"hourglass_clasp"))));
V v_410 = values_409[0];
V v_411 = values_409[1];

R.Call(v_32, R.Join(new V[] {}, new Results(v_410)));
foreach (var row_412 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"anchor_maul"), new Entry((V)2, (V)"ward_ledger"), new Entry((V)3, (V)"scavengers_hook"), new Entry((V)4, (V)"cinder_stopper"), new Entry((V)5, (V)"emberbrand")))))))) {
V v_413 = row_412[0];
V v_414 = row_412[1];
R.Call(v_18, R.Join(new V[] {v_410}, new Results(v_414)));
}
R.Call(v_68, R.Join(new V[] {v_410}, new Results((V)4.0d)));

var values_415 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_416 = v_411;
V key_417 = (V)"hp";
R.Set(obj_416, key_417, values_415[0]);

R.Call(v_47, R.Join(new V[] {}, new Results(v_410)));
var values_418 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_410))))))))));
V v_419 = values_418[0];
foreach (var row_420 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"wait"), new Entry((V)2, (V)"move"), new Entry((V)3, (V)"basic_attack"), new Entry((V)4, (V)"wait"), new Entry((V)5, (V)"basic_attack")))))))) {
V v_421 = row_420[0];
V v_422 = row_420[1];
R.Call(v_3, R.Join(new V[] {R.Call(v_47, R.Join(new V[] {v_410, v_422, (V)0.0d}, new Results(R.Or(R.And((V)(v_422 == (V)"move"), () => (V)1.0d), () => (-(V)1.0d))))).First}, R.Call(v_47, R.Join(new V[] {v_419, v_422, (V)0.0d}, new Results(R.Or(R.And((V)(v_422 == (V)"move"), () => (V)1.0d), () => (-(V)1.0d)))))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_410))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_419)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"actual stage handoff preserves only the remaining Hourglass cooldown and keeps expired cooldowns ready"}, new Results(new V((RuleFunction)(args_423 => {

var values_424 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_boon_handoff_fixture"))));
V v_425 = values_424[0];
foreach (var row_426 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)250.0d), new Entry((V)2, (-(V)10.0d))))))))) {
V v_427 = row_426[0];
V v_428 = row_426[1];
var values_429 = R.Join(new V[] {}, R.Call(R.Get(v_425, (V)"ready"), Results.Empty));
V v_430 = values_429[0];
V v_431 = values_429[1];
V v_432 = values_429[2];

R.Call(v_18, R.Join(new V[] {v_430}, new Results((V)"hourglass_clasp")));
var values_433 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:boon/hourglass_clasp", R.Table(new Entry((V)"expires_at", (V)(R.Get(v_430, (V)"clock") + v_428)))))));
var obj_434 = v_431;
V key_435 = (V)"boon_memory";
R.Set(obj_434, key_435, values_433[0]);
R.Call(R.Get(v_425, (V)"route"), R.Join(new V[] {v_430}, new Results(v_432)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_430, (V)"clock") == (V)0.0d), () => (V)(R.Get(R.Call(v_83, R.Join(new V[] {v_430}, new Results((V)"hourglass_clasp"))).First, (V)"expires_at") == R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results(v_428))).First)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_430))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_430)))))))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new control previews are read-only and actual effects do not invalidate ordinary run eligibility"}, new Results(new V((RuleFunction)(args_436 => {

var values_437 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {(V)37.0d}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievements"))).First, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"control-boon-meta-fixture"))).First))))));
V v_438 = values_437[0];
var values_439 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_438))));
V v_440 = values_439[0];

var values_441 = Results.Empty;
V v_442 = values_441[0];
V v_443 = values_441[1];
V v_444 = values_441[2];
var values_445 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_446 = values_445[0];

var values_447 = R.Join(new V[] {}, R.Call(R.Get(v_446, (V)"map_for"), R.Join(new V[] {}, new Results(v_438))));
V v_448 = values_447[0];
foreach (var row_449 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_450 = row_449[0];
V v_451 = row_449[1];
var values_452 = R.Join(new V[] {(V)(R.Get(R.Get(v_440, (V)"position"), (V)"x") + R.Get(v_451, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_440, (V)"position"), (V)"y") + R.Get(v_451, (V)2.0d))));
V v_453 = values_452[0];
V v_454 = values_452[1];
var values_455 = R.Join(new V[] {}, R.Call(R.Get(v_446, (V)"walkable"), R.Join(new V[] {v_448, v_453}, new Results(v_454))));
V v_456 = values_455[0];
foreach (var row_457 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_438, (V)"entities"))))))) {
V v_458 = row_457[0];
V v_459 = row_457[1];
if (R.Truth(R.And(R.And(R.And(R.Get(v_459, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_459, (V)"position"), (V)"floor_id") == R.Get(v_438, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_459, (V)"position"), (V)"x") == v_453)), () => (V)(R.Get(R.Get(v_459, (V)"position"), (V)"y") == v_454)))) {
var values_460 = R.Join(new V[] {}, new Results((V)false));
v_456 = values_460[0];
}
}
if (R.Truth(v_456)) {
var values_461 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_438, v_453}, new Results(v_454))));
v_442 = values_461[0];

var values_462 = R.Join(new V[] {R.Get(v_451, (V)1.0d)}, new Results(R.Get(v_451, (V)2.0d)));
v_443 = values_462[0];
v_444 = values_462[1];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_442)));
R.Call(R.Get(v_7, (V)"set_stacks"), R.Join(new V[] {v_440, (V)"core:boon/anchor_maul", (V)"rare"}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_438, (V)"meta"), (V)"eligible"))));
var values_463 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_438))));
V v_464 = values_463[0];
{ double end_467 = R.Num((V)30.0d), step_468 = R.Num((V)1.0d);
for (double i_466 = R.Num((V)1.0d); step_468 > 0 ? i_466 <= end_467 : i_466 >= end_467; i_466 += step_468) {
V v_465 = i_466;
R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_438, (V)"basic_attack", v_443}, new Results(v_444)));

R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_438, (V)"sweeping_slash", v_443}, new Results(v_444)));
}
}
R.Call(v_3, R.Join(new V[] {v_464}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_438)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_57, R.Join(new V[] {R.Call(v_47, R.Join(new V[] {v_438, (V)"basic_attack", v_443}, new Results(v_444))).First, (V)"BoonActivated"}, new Results((V)"anchor_maul"))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_438, (V)"meta"), (V)"eligible"))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
