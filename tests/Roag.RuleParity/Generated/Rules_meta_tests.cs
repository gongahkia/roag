// Static C# port of tests/meta_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 01f4bf6b432e84415c3c36e1d0b1a80b18169c02cbb1af6654995610f3112e6d
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_meta_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievements"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievement_content"))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.profile"))));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_20 = values_19[0];
var values_21 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_22 = values_21[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
V v_25 = args_23[1];
V v_26 = V.Nil;
v_26 = new V((RuleFunction)(args_27 => {

var values_28 = R.Join(new V[] {}, new Results(R.Table()));
V v_29 = values_28[0];
R.Set(v_29, "read", new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
V v_32 = args_30[1];
return R.Join(new V[] {}, new Results(R.Get(v_31, v_32)));
return Results.Empty;
})));
R.Set(v_29, "write", new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
V v_35 = args_33[1];
V v_36 = args_33[2];
if (R.Truth(R.Get(v_34, (V)"fail"))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_37 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_34, (V)"truncate"), () => R.Invoke(v_36, "sub", R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_36) / (V)2.0d)))))).First), () => v_36)));
var obj_38 = v_34;
V key_39 = v_35;
R.Set(obj_38, key_39, values_37[0]);
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_29));
return Results.Empty;
}));
V v_40 = V.Nil;
v_40 = new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
V v_43 = args_41[1];
V v_44 = args_41[2];
var values_45 = R.Join(new V[] {}, new Results((V)0.0d));
V v_46 = values_45[0];
foreach (var row_47 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_42)))))) {
V v_48 = row_47[0];
V v_49 = row_47[1];
if (R.Truth(R.And((V)(R.Get(v_49, (V)"kind") == v_43), () => R.Or((V)(!R.Truth(v_44)), () => (V)(R.Get(v_49, (V)"boon_id") == v_44))))) {
var values_50 = R.Join(new V[] {}, new Results((V)(v_46 + (V)1.0d)));
v_46 = values_50[0];
}
}
return R.Join(new V[] {}, new Results(v_46));
return Results.Empty;
}));
V v_51 = V.Nil;
v_51 = new V((RuleFunction)(args_52 => {
V v_53 = args_52[0];
V v_54 = args_52[1];
R.Call(R.Get(v_2, (V)"set_boon_stacks"), R.Join(new V[] {v_53, R.Get(v_53, (V)"player_id"), R.Concat((V)"core:boon/", v_54), (V)"rare"}, new Results((V)1.0d)));
return Results.Empty;
}));
V v_55 = V.Nil;
v_55 = new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
V v_58 = args_56[1];
V v_59 = args_56[2];
V v_60 = args_56[3];
var values_61 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_57, R.Or(v_58, () => (V)"core:enemy/mossbound_guard"), v_59}, new Results(v_60))));
V v_62 = values_61[0];
R.Call(R.Get(v_4, (V)"remove_for_actor"), R.Join(new V[] {v_57}, new Results(R.Get(v_62, (V)"id"))));
return R.Join(new V[] {}, new Results(v_62));
return Results.Empty;
}));
R.Call(v_24, R.Join(new V[] {(V)"twenty-two achievements preserve four functioning boon unlocks"}, new Results(new V((RuleFunction)(args_63 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_16, (V)"validate"), Results.Empty).First, () => (V)((V)R.Length(R.Get(v_16, (V)"order")) == (V)22.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_10, (V)"order")) == (V)((V)53.0d + (V)R.Length(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"order")))), () => (V)((V)R.Length(R.Call(R.Get(v_14, (V)"pool"), R.Join(new V[] {}, new Results(R.Table()))).First) == (V)((V)49.0d + (V)R.Length(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"order"))))))));
var values_64 = R.Join(new V[] {}, new Results(R.Table()));
V v_65 = values_64[0];
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_16, (V)"order"))))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
var values_69 = R.Join(new V[] {}, new Results((V)true));
var obj_70 = v_65;
V key_71 = v_68;
R.Set(obj_70, key_71, values_69[0]);
}
var values_72 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"pool"), R.Join(new V[] {}, new Results(v_65))));
V v_73 = values_72[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_73) == (V)R.Length(R.Get(v_10, (V)"order"))), () => R.Call(R.Get(v_14, (V)"validate_pool"), R.Join(new V[] {}, new Results(v_73))).First))));
{ double end_76 = R.Num((V)4.0d), step_77 = R.Num((V)1.0d);
for (double i_75 = R.Num((V)1.0d); step_77 > 0 ? i_75 <= end_76 : i_75 >= end_76; i_75 += step_77) {
V v_74 = i_75;
var values_78 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"order"), v_74)));
V v_79 = values_78[0];
var values_80 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_16, (V)"get"), R.Join(new V[] {}, new Results(v_79))).First, (V)"reward")));
V v_81 = values_80[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"get"), R.Join(new V[] {}, new Results(v_81)))));
}
}
{ double end_84 = R.Num((V)R.Length(R.Get(v_16, (V)"order"))), step_85 = R.Num((V)1.0d);
for (double i_83 = R.Num((V)5.0d); step_85 > 0 ? i_83 <= end_84 : i_83 >= end_84; i_83 += step_85) {
V v_82 = i_83;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_16, (V)"get"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"order"), v_82)))).First, (V)"reward") == V.Nil))));
}
}
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"new milestones award once and never alter the boon pool"}, new Results(new V((RuleFunction)(args_86 => {

var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/badges"))).First))))));
V v_88 = values_87[0];
var values_89 = R.Join(new V[] {}, new Results(R.Table()));
V v_90 = values_89[0];
V v_91 = V.Nil;
v_91 = new V((RuleFunction)(args_92 => {
V v_93 = args_92[0];
V v_94 = args_92[1];
var values_95 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_93), new Entry((V)"fields", v_94))));
var obj_96 = v_90;
V key_97 = (V)((V)R.Length(v_90) + (V)1.0d);
R.Set(obj_96, key_97, values_95[0]);
return Results.Empty;
}));
var values_98 = R.Join(new V[] {}, new Results((V)R.Length(R.Call(R.Get(v_14, (V)"pool"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_88, (V)"meta"), (V)"earned")))).First)));
V v_99 = values_98[0];
var values_100 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"EntityKilled"), new Entry((V)"source_id", R.Get(v_88, (V)"player_id")), new Entry((V)"target_id", (V)999.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"CurrencyCollected"), new Entry((V)"source_id", R.Get(v_88, (V)"player_id")), new Entry((V)"new_currency", (V)49.0d))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"CurrencyCollected"), new Entry((V)"source_id", R.Get(v_88, (V)"player_id")), new Entry((V)"new_currency", (V)50.0d))), new Entry((V)4, R.Table(new Entry((V)"kind", (V)"LevelUp"), new Entry((V)"target_id", R.Get(v_88, (V)"player_id")), new Entry((V)"new_level", (V)5.0d))), new Entry((V)5, R.Table(new Entry((V)"kind", (V)"TerrainBroken"), new Entry((V)"source_id", R.Get(v_88, (V)"player_id")))), new Entry((V)6, R.Table(new Entry((V)"kind", (V)"StageEntered"), new Entry((V)"stage_index", (V)2.0d))), new Entry((V)7, R.Table(new Entry((V)"kind", (V)"FinalSanctumEntered"))))));
V v_101 = values_100[0];
foreach (var row_102 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_101)))))) {
V v_103 = row_102[0];
V v_104 = row_102[1];
R.Call(R.Get(v_14, (V)"observe"), R.Join(new V[] {v_88, v_104}, new Results(v_91)));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_90) == (V)7.0d))));
foreach (var row_105 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_90)))))) {
V v_106 = row_105[0];
V v_107 = row_105[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_107, (V)"kind") == (V)"AchievementEarned"), () => (V)(!R.Truth(R.Get(R.Get(v_107, (V)"fields"), (V)"reward_boon_id")))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_14, (V)"pool"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_88, (V)"meta"), (V)"earned")))).First) == v_99))));
foreach (var row_108 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_101)))))) {
V v_109 = row_108[0];
V v_110 = row_108[1];
R.Call(R.Get(v_14, (V)"observe"), R.Join(new V[] {v_88, v_110}, new Results(v_91)));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_90) == (V)7.0d))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"eligible run freezes chest pool and fixture grants remove eligibility"}, new Results(new V((RuleFunction)(args_111 => {

var values_112 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/1"))));
V v_113 = values_112[0];
var values_114 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"meta", v_113))))));
V v_115 = values_114[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_115, (V)"available_boons")) == (V)20.0d), () => R.Get(R.Get(v_115, (V)"meta"), (V)"eligible")))));
var values_116 = R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_115))))));
V v_117 = values_116[0];
var values_118 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"make_offers"), R.Join(new V[] {v_115}, R.Call(R.Get(v_8, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/standard"))))));
V v_119 = values_118[0];
foreach (var row_120 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_119)))))) {
V v_121 = row_120[0];
V v_122 = row_120[1];
{ double end_125 = R.Num((V)24.0d), step_126 = R.Num((V)1.0d);
for (double i_124 = R.Num((V)21.0d); step_126 > 0 ? i_124 <= end_125 : i_124 >= end_125; i_124 += step_126) {
V v_123 = i_124;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_122, (V)"boon_id") != R.Get(R.Get(v_10, (V)"order"), v_123)))));
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_117 != R.Call(R.Get(v_20, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_115))))).First))));
R.Call(R.Get(v_2, (V)"set_boon_stacks"), R.Join(new V[] {v_115, R.Get(v_115, (V)"player_id"), (V)"core:boon/emberbrand", (V)"common"}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(R.Get(v_115, (V)"meta"), (V)"eligible"))), () => (V)(!R.Truth(R.Get(R.Get(R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_115))))).First, (V)"meta"), (V)"eligible")))))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"profile slots recover corruption and reject newer incompatibility"}, new Results(new V((RuleFunction)(args_127 => {

var values_128 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_129 = values_128[0];

var values_130 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_129))));
V v_131 = values_130[0];
var values_132 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"fresh"), Results.Empty));
V v_133 = values_132[0];
var values_134 = R.Join(new V[] {}, new Results((V)true));
var obj_135 = R.Get(v_133, (V)"earned");
V key_136 = R.Get(R.Get(v_16, (V)"order"), (V)1.0d);
R.Set(obj_135, key_136, values_134[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_131, "save", R.Join(new V[] {}, new Results(v_133)))));
var values_137 = R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"encode"), R.Join(new V[] {}, new Results(v_133))))));
V v_138 = values_137[0];
var values_139 = R.Join(new V[] {}, new Results((V)true));
var obj_140 = R.Get(v_138, (V)"earned");
V key_141 = R.Get(R.Get(v_16, (V)"order"), (V)2.0d);
R.Set(obj_140, key_141, values_139[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_131, "save", R.Join(new V[] {}, new Results(v_138)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Invoke(v_131, "inspect", Results.Empty).First, (V)"profile"), (V)"earned"), R.Get(R.Get(v_16, (V)"order"), (V)2.0d)))));
var values_142 = R.Join(new V[] {}, R.Invoke(R.Get(v_129, R.Get(R.Get(v_18, (V)"slots"), (V)2.0d)), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)20.0d))));
var obj_143 = v_129;
V key_144 = R.Get(R.Get(v_18, (V)"slots"), (V)2.0d);
R.Set(obj_143, key_144, values_142[0]);
var values_145 = R.Join(new V[] {}, R.Invoke(v_131, "inspect", Results.Empty));
V v_146 = values_145[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_146, (V)"status") == (V)"recovered"), () => R.Get(R.Get(R.Get(v_146, (V)"profile"), (V)"earned"), R.Get(R.Get(v_16, (V)"order"), (V)1.0d))), () => (V)(!R.Truth(R.Get(R.Get(R.Get(v_146, (V)"profile"), (V)"earned"), R.Get(R.Get(v_16, (V)"order"), (V)2.0d))))))));
var values_147 = R.Join(new V[] {}, new Results(V.Nil));
var obj_148 = v_129;
V key_149 = R.Get(R.Get(v_18, (V)"slots"), (V)2.0d);
R.Set(obj_148, key_149, values_147[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_131, "save", R.Join(new V[] {}, new Results(v_138)))));
var values_150 = R.Join(new V[] {}, R.Invoke(R.Get(v_129, R.Get(R.Get(v_18, (V)"slots"), (V)2.0d)), "gsub", R.Join(new V[] {(V)"roag%-profile%-record/1", (V)"roag-profile-record/9"}, new Results((V)1.0d))));
var obj_151 = v_129;
V key_152 = R.Get(R.Get(v_18, (V)"slots"), (V)2.0d);
R.Set(obj_151, key_152, values_150[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_131, "inspect", Results.Empty).First, (V)"status") == (V)"incompatible"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_131, "save", R.Join(new V[] {}, new Results(v_138))).First)))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"failed profile writes preserve previous generation and do not touch run slots"}, new Results(new V((RuleFunction)(args_153 => {

var values_154 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_155 = values_154[0];

var values_156 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_155))));
V v_157 = values_156[0];
var values_158 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"fresh"), Results.Empty));
V v_159 = values_158[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_157, "save", R.Join(new V[] {}, new Results(v_159)))));
var values_160 = R.Join(new V[] {}, new Results(R.Get(v_155, R.Get(R.Get(v_18, (V)"slots"), (V)1.0d))));
V v_161 = values_160[0];
var values_162 = R.Join(new V[] {}, new Results((V)true));
var obj_163 = v_155;
V key_164 = (V)"fail";
R.Set(obj_163, key_164, values_162[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_157, "save", R.Join(new V[] {}, new Results(v_159))).First)))));

var values_165 = R.Join(new V[] {}, new Results((V)false));
var obj_166 = v_155;
V key_167 = (V)"fail";
R.Set(obj_166, key_167, values_165[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_155, R.Get(R.Get(v_18, (V)"slots"), (V)1.0d)) == v_161), () => (V)(R.Get(R.Invoke(v_157, "inspect", Results.Empty).First, (V)"status") == (V)"active")))));
var values_168 = R.Join(new V[] {}, new Results((V)true));
var obj_169 = v_155;
V key_170 = (V)"truncate";
R.Set(obj_169, key_170, values_168[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_157, "save", R.Join(new V[] {}, new Results(v_159))).First)))));

var values_171 = R.Join(new V[] {}, new Results((V)false));
var obj_172 = v_155;
V key_173 = (V)"truncate";
R.Set(obj_172, key_173, values_171[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_157, "inspect", Results.Empty).First, (V)"status") == (V)"recovered"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_155, R.Get(R.Get(v_20, (V)"slots"), (V)1.0d)) == V.Nil), () => (V)(R.Get(v_155, R.Get(R.Get(v_20, (V)"slots"), (V)2.0d)) == V.Nil)))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"both corrupt profile records and oversized input fail closed"}, new Results(new V((RuleFunction)(args_174 => {

var values_175 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_176 = values_175[0];

var values_177 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_176))));
V v_178 = values_177[0];
var values_179 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"fresh"), Results.Empty));
V v_180 = values_179[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(v_178, "save", R.Join(new V[] {}, new Results(v_180))).First, () => R.Invoke(v_178, "save", R.Join(new V[] {}, new Results(v_180))).First))));
var values_181 = R.Join(new V[] {}, new Results((V)"broken"));
var obj_182 = v_176;
V key_183 = R.Get(R.Get(v_18, (V)"slots"), (V)1.0d);
R.Set(obj_182, key_183, values_181[0]);
var values_184 = R.Join(new V[] {}, new Results((V)"also broken"));
var obj_185 = v_176;
V key_186 = R.Get(R.Get(v_18, (V)"slots"), (V)2.0d);
R.Set(obj_185, key_186, values_184[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Invoke(v_178, "inspect", Results.Empty).First, (V)"status") == (V)"corrupt"), () => (V)(!R.Truth(R.Invoke(v_178, "save", R.Join(new V[] {}, new Results(v_180))).First))))));
var values_187 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"x"}, new Results((V)(R.Get(v_18, (V)"max_bytes") + (V)257.0d)))));
var obj_188 = v_176;
V key_189 = R.Get(R.Get(v_18, (V)"slots"), (V)1.0d);
R.Set(obj_188, key_189, values_187[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_178, "inspect", Results.Empty).First, (V)"status") == (V)"corrupt"))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"profile remains available when active run content becomes incompatible"}, new Results(new V((RuleFunction)(args_190 => {

var values_191 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_192 = values_191[0];

var values_193 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_192))));
V v_194 = values_193[0];
var values_195 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"order"), (V)1.0d)));
V v_196 = values_195[0];
var values_197 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"fresh"), Results.Empty));
V v_198 = values_197[0];

var values_199 = R.Join(new V[] {}, new Results((V)true));
var obj_200 = R.Get(v_198, (V)"earned");
V key_201 = v_196;
R.Set(obj_200, key_201, values_199[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_194, "save", R.Join(new V[] {}, new Results(v_198)))));
var values_202 = R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"new"), R.Join(new V[] {}, new Results(v_192))));
V v_203 = values_202[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_203, "replace", R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {}, new Results((V)77.0d)))))));
var values_204 = R.Join(new V[] {}, R.Invoke(R.Get(v_192, R.Get(R.Get(v_20, (V)"slots"), (V)2.0d)), "gsub", R.Join(new V[] {(V)"roag%-save/1", (V)"roag-save/9"}, new Results((V)1.0d))));
var obj_205 = v_192;
V key_206 = R.Get(R.Get(v_20, (V)"slots"), (V)2.0d);
R.Set(obj_205, key_206, values_204[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_203, "inspect", Results.Empty).First, (V)"status") == (V)"incompatible"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Invoke(v_194, "inspect", Results.Empty).First, (V)"profile"), (V)"earned"), v_196))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"all four unlocked boons are offered through frozen future-run chest rolls"}, new Results(new V((RuleFunction)(args_207 => {

var values_208 = R.Join(new V[] {}, new Results(R.Table()));
V v_209 = values_208[0];

foreach (var row_210 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_16, (V)"order"))))))) {
V v_211 = row_210[0];
V v_212 = row_210[1];
var values_213 = R.Join(new V[] {}, new Results((V)true));
var obj_214 = v_209;
V key_215 = v_212;
R.Set(obj_214, key_215, values_213[0]);
}
var values_216 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"pool"), R.Join(new V[] {}, new Results(v_209))));
V v_217 = values_216[0];
var values_218 = R.Join(new V[] {}, new Results(R.Table()));
V v_219 = values_218[0];
{ double end_222 = R.Num((V)400.0d), step_223 = R.Num((V)1.0d);
for (double i_221 = R.Num((V)1.0d); step_223 > 0 ? i_221 <= end_222 : i_221 >= end_222; i_221 += step_223) {
V v_220 = i_221;
var values_224 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_220, V.Nil}, new Results(R.Table(new Entry((V)"available_boons", v_217))))));
V v_225 = values_224[0];
foreach (var row_226 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"make_offers"), R.Join(new V[] {v_225}, R.Call(R.Get(v_8, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/standard")))))))))) {
V v_227 = row_226[0];
V v_228 = row_226[1];
var values_229 = R.Join(new V[] {}, new Results((V)true));
var obj_230 = v_219;
V key_231 = R.Get(v_228, (V)"boon_id");
R.Set(obj_230, key_231, values_229[0]);
}
}
}
{ double end_234 = R.Num((V)24.0d), step_235 = R.Num((V)1.0d);
for (double i_233 = R.Num((V)21.0d); step_235 > 0 ? i_233 <= end_234 : i_233 >= end_234; i_233 += step_235) {
V v_232 = i_233;
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_219, R.Get(R.Get(v_10, (V)"order"), v_232))}, new Results(R.Get(R.Get(v_10, (V)"order"), v_232))));
}
}
var values_236 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"available_boons", v_217))))));
V v_237 = values_236[0];
var values_238 = R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_237))))));
V v_239 = values_238[0];
var values_240 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_237))))));
V v_241 = values_240[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_20, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_241))))).First == v_239))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"unlocked mixed-rarity stacks remain additive and serializable without cap"}, new Results(new V((RuleFunction)(args_242 => {

var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {}, new Results((V)81.0d))));
V v_244 = values_243[0];
R.Call(R.Get(v_2, (V)"set_boon_stacks"), R.Join(new V[] {v_244, R.Get(v_244, (V)"player_id"), (V)"core:boon/prospectors_heart", (V)"common"}, new Results((V)100.0d)));
R.Call(R.Get(v_2, (V)"set_boon_stacks"), R.Join(new V[] {v_244, R.Get(v_244, (V)"player_id"), (V)"core:boon/prospectors_heart", (V)"rare"}, new Results((V)3.0d)));
var values_245 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_244))).First, (V)"boons"), (V)"core:boon/prospectors_heart")));
V v_246 = values_245[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_12, (V)"aggregate"), R.Join(new V[] {(V)"core:boon/prospectors_heart", v_246}, new Results((V)"power"))).First == (V)109.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_20, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_244))))).First == R.Call(R.Get(v_20, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_244))))))))).First))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"Guardian Vow block bursts, Prospector pickup heals, Crownfire first strike burns"}, new Results(new V((RuleFunction)(args_247 => {

var values_248 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {}, new Results((V)36.0d))));
V v_249 = values_248[0];
var values_250 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_249))));
V v_251 = values_250[0];

var values_252 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_253 = R.Get(v_251, (V)"position");
V key_254 = (V)"x";
R.Set(obj_253, key_254, values_252[0]);

var values_255 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_256 = R.Get(v_251, (V)"position");
V key_257 = (V)"y";
R.Set(obj_256, key_257, values_255[0]);

var values_258 = R.Join(new V[] {}, new Results((V)99.0d));
var obj_259 = v_251;
V key_260 = (V)"defense";
R.Set(obj_259, key_260, values_258[0]);
R.Call(v_51, R.Join(new V[] {v_249}, new Results((V)"guardians_vow")));
R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_249, (V)"core:enemy/thornspitter", (V)8.0d}, new Results((V)2.0d)));
var values_261 = R.Join(new V[] {}, R.Call(v_55, R.Join(new V[] {v_249, (V)"core:enemy/mossbound_guard", (V)7.0d}, new Results((V)5.0d))));
V v_262 = values_261[0];
var values_263 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_249}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_264 = values_263[0];
V v_265 = values_263[1];
V v_266 = values_263[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_40, R.Join(new V[] {v_266}, new Results((V)"DamageBlocked"))).First >= (V)1.0d), () => (V)(R.Call(v_40, R.Join(new V[] {v_266, (V)"BoonActivated"}, new Results((V)"core:boon/guardians_vow"))).First == (V)1.0d)), () => (V)(R.Get(v_262, (V)"hp") < R.Get(v_262, (V)"max_hp"))))));
var values_267 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {}, new Results((V)51.0d))));
V v_268 = values_267[0];
var values_269 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_268))));
V v_270 = values_269[0];

var values_271 = R.Join(new V[] {}, new Results((V)(R.Get(v_270, (V)"hp") - (V)4.0d)));
var obj_272 = v_270;
V key_273 = (V)"hp";
R.Set(obj_272, key_273, values_271[0]);
R.Call(v_51, R.Join(new V[] {v_268}, new Results((V)"prospectors_heart")));
R.Call(R.Get(v_6, (V)"spawn_currency"), R.Join(new V[] {v_268, (V)2.0d, R.Get(R.Get(v_270, (V)"position"), (V)"floor_id"), (V)(R.Get(R.Get(v_270, (V)"position"), (V)"x") + (V)1.0d), R.Get(R.Get(v_270, (V)"position"), (V)"y")}, new Results(new V((RuleFunction)(args_274 => {


return Results.Empty;
})))));
var values_275 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_268}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_276 = values_275[0];
V v_277 = values_275[1];
V v_278 = values_275[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_40, R.Join(new V[] {v_278}, new Results((V)"CurrencyCollected"))).First == (V)1.0d), () => (V)(R.Call(v_40, R.Join(new V[] {v_278, (V)"BoonActivated"}, new Results((V)"core:boon/prospectors_heart"))).First == (V)1.0d)), () => (V)(R.Get(v_270, (V)"hp") > (V)(R.Get(v_270, (V)"max_hp") - (V)4.0d))))));
var values_279 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {}, new Results((V)52.0d))));
V v_280 = values_279[0];
R.Call(v_51, R.Join(new V[] {v_280}, new Results((V)"crownfire")));
var values_281 = R.Join(new V[] {}, R.Call(v_55, R.Join(new V[] {v_280, V.Nil, (V)3.0d}, new Results((V)2.0d))));
V v_282 = values_281[0];
var values_283 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_280}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_284 = values_283[0];
V v_285 = values_283[1];
V v_286 = values_283[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_40, R.Join(new V[] {v_286, (V)"BoonActivated"}, new Results((V)"core:boon/crownfire"))).First == (V)1.0d), () => R.Get(v_282, (V)"statuses")), () => R.Get(R.Get(v_282, (V)"statuses"), (V)"burning")))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"Circuit Chorus follows another boon activation without self-loop"}, new Results(new V((RuleFunction)(args_287 => {

var values_288 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {}, new Results((V)53.0d))));
V v_289 = values_288[0];
R.Call(v_51, R.Join(new V[] {v_289}, new Results((V)"circuit_chorus")));
R.Call(R.Get(v_2, (V)"set_boon_stacks"), R.Join(new V[] {v_289, R.Get(v_289, (V)"player_id"), (V)"core:boon/storm_conductor", (V)"legendary"}, new Results((V)2.0d)));
R.Call(v_55, R.Join(new V[] {v_289, V.Nil, (V)3.0d}, new Results((V)2.0d)));

R.Call(v_55, R.Join(new V[] {v_289, V.Nil, (V)4.0d}, new Results((V)2.0d)));
var values_290 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_289}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_291 = values_290[0];
V v_292 = values_290[1];
V v_293 = values_290[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_40, R.Join(new V[] {v_293, (V)"BoonActivated"}, new Results((V)"core:boon/storm_conductor"))).First >= (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_40, R.Join(new V[] {v_293, (V)"BoonActivated"}, new Results((V)"core:boon/circuit_chorus"))).First >= (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_293) < (V)100.0d))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"normal legal elite defeat awards once and future run can roll reward"}, new Results(new V((RuleFunction)(args_294 => {

var values_295 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/100"))).First))));
V v_296 = values_295[0];
var values_297 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"stage_one"), R.Join(new V[] {(V)3.0d, (V)1.0d}, new Results(v_296))));
V v_298 = values_297[0];
var values_299 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"order"), (V)1.0d)));
V v_300 = values_299[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(R.Get(R.Get(v_298, (V)"state"), (V)"meta"), (V)"earned"), v_300), () => R.Get(R.Get(R.Get(R.Get(v_298, (V)"state"), (V)"meta"), (V)"pending"), v_300)))));
var values_301 = R.Join(new V[] {}, new Results((V)0.0d));
V v_302 = values_301[0];
foreach (var row_303 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_298, (V)"events"))))))) {
V v_304 = row_303[0];
V v_305 = row_303[1];
if (R.Truth(R.And((V)(R.Get(v_305, (V)"kind") == (V)"AchievementEarned"), () => (V)(R.Get(v_305, (V)"achievement_id") == v_300)))) {
var values_306 = R.Join(new V[] {}, new Results((V)(v_302 + (V)1.0d)));
v_302 = values_306[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_302 == (V)1.0d))));
var values_307 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_308 = values_307[0];

var values_309 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_308))));
V v_310 = values_309[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_310, "commit_awards", R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_298, (V)"state"), (V)"meta"), (V)"pending"))))));
var values_311 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"pool"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Invoke(v_310, "inspect", Results.Empty).First, (V)"profile"), (V)"earned")))));
V v_312 = values_311[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_312) == (V)((V)50.0d + (V)R.Length(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"order")))), () => (V)(R.Get(v_312, (V)R.Length(v_312)) == (V)"core:boon/guardians_vow")))));
var values_313 = R.Join(new V[] {}, new Results((V)false));
V v_314 = values_313[0];
{ double end_317 = R.Num((V)200.0d), step_318 = R.Num((V)1.0d);
for (double i_316 = R.Num((V)1.0d); step_318 > 0 ? i_316 <= end_317 : i_316 >= end_317; i_316 += step_318) {
V v_315 = i_316;
var values_319 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {v_315}, new Results(R.Table(new Entry((V)"available_boons", v_312))))));
V v_320 = values_319[0];
foreach (var row_321 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_320, (V)"entities"))))))) {
V v_322 = row_321[0];
V v_323 = row_321[1];
if (R.Truth((V)(R.Get(v_323, (V)"controller") == (V)"chest"))) {
foreach (var row_324 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_323, (V)"offers"))))))) {
V v_325 = row_324[0];
V v_326 = row_324[1];
if (R.Truth((V)(R.Get(v_326, (V)"boon_id") == R.Get(v_312, (V)50.0d)))) {
var values_327 = R.Join(new V[] {}, new Results((V)true));
v_314 = values_327[0];
}
}
}
}
if (R.Truth(v_314)) {
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_314}, new Results((V)"unlocked boon never appeared in generated chest")));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"ordinary movement and empty-tile strike reveal secret achievement"}, new Results(new V((RuleFunction)(args_328 => {

var values_329 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"new"), R.Join(new V[] {(V)1.0d}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/secret"))).First))))));
V v_330 = values_329[0];
var values_331 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_330, (V)"state"), (V)"secrets"), (V)1.0d)));
V v_332 = values_331[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(R.Get(R.Get(v_330, (V)"state"), (V)"meta"), (V)"earned"), R.Get(R.Get(v_16, (V)"order"), (V)2.0d)))))));
var values_335 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"navigate"), R.Join(new V[] {v_330, new V((RuleFunction)(args_333 => {
V v_334 = args_333[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_334, (V)"x") == R.Get(v_332, (V)"x")), () => (V)(R.Get(v_334, (V)"y") == R.Get(v_332, (V)"y")))));
return Results.Empty;
}))}, new Results((V)350.0d))));
V v_336 = values_335[0];
V v_337 = values_335[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_336}, new Results(v_337)));
var values_338 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"action"), R.Join(new V[] {v_330}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(R.Get(v_332, (V)"wall_x") - R.Get(v_332, (V)"x"))), new Entry((V)"dy", (V)(R.Get(v_332, (V)"wall_y") - R.Get(v_332, (V)"y"))))))));
V v_339 = values_338[0];
V v_340 = values_338[1];
V v_341 = values_338[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_40, R.Join(new V[] {v_341}, new Results((V)"SecretDiscovered"))).First == (V)1.0d), () => R.Get(R.Get(R.Get(R.Get(v_330, (V)"state"), (V)"meta"), (V)"earned"), R.Get(R.Get(v_16, (V)"order"), (V)2.0d))))));
var values_342 = R.Join(new V[] {}, new Results((V)0.0d));
V v_343 = values_342[0];
foreach (var row_344 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_341)))))) {
V v_345 = row_344[0];
V v_346 = row_344[1];
if (R.Truth((V)(R.Get(v_346, (V)"kind") == (V)"AchievementEarned"))) {
var values_347 = R.Join(new V[] {}, new Results((V)(v_343 + (V)1.0d)));
v_343 = values_347[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_343 == (V)1.0d))));
var values_348 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"action"), R.Join(new V[] {v_330}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(R.Get(v_332, (V)"wall_x") - R.Get(v_332, (V)"x"))), new Entry((V)"dy", (V)(R.Get(v_332, (V)"wall_y") - R.Get(v_332, (V)"y"))))))));
V v_349 = values_348[0];
V v_350 = values_348[1];
V v_351 = values_348[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_40, R.Join(new V[] {v_351}, new Results((V)"SecretDiscovered"))).First == (V)0.0d), () => (V)(R.Call(v_40, R.Join(new V[] {v_351}, new Results((V)"AchievementEarned"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"earning an achievement changes neither gameplay RNG nor world time"}, new Results(new V((RuleFunction)(args_352 => {

var values_353 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"new"), R.Join(new V[] {(V)1.0d}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/rng"))).First))))));
V v_354 = values_353[0];
var values_355 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"new"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_356 = values_355[0];
var values_357 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_354, (V)"state"), (V)"secrets"), (V)1.0d)));
V v_358 = values_357[0];
var values_361 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"navigate"), R.Join(new V[] {v_354, new V((RuleFunction)(args_359 => {
V v_360 = args_359[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_360, (V)"x") == R.Get(v_358, (V)"x")), () => (V)(R.Get(v_360, (V)"y") == R.Get(v_358, (V)"y")))));
return Results.Empty;
}))}, new Results((V)350.0d))));
V v_362 = values_361[0];
V v_363 = values_361[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_362}, new Results(v_363)));
R.Call(R.Get(v_22, (V)"action"), R.Join(new V[] {v_354}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(R.Get(v_358, (V)"wall_x") - R.Get(v_358, (V)"x"))), new Entry((V)"dy", (V)(R.Get(v_358, (V)"wall_y") - R.Get(v_358, (V)"y")))))));
foreach (var row_364 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_354, (V)"trace"))))))) {
V v_365 = row_364[0];
V v_366 = row_364[1];
var values_367 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"action"), R.Join(new V[] {v_356}, new Results(R.Get(v_366, (V)"intent")))));
V v_368 = values_367[0];
V v_369 = values_367[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_368 == R.Get(v_366, (V)"ok")), () => (V)(v_369 == R.Get(v_366, (V)"reason"))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_354, (V)"state"), (V)"meta"), (V)"earned"), R.Get(R.Get(v_16, (V)"order"), (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_354, (V)"state"), (V)"clock") == R.Get(R.Get(v_356, (V)"state"), (V)"clock")), () => (V)(R.Get(R.Get(R.Get(v_354, (V)"state"), (V)"rng"), (V)"state") == R.Get(R.Get(R.Get(v_356, (V)"state"), (V)"rng"), (V)"state"))), () => (V)(R.Get(R.Get(v_354, (V)"state"), (V)"currency") == R.Get(R.Get(v_356, (V)"state"), (V)"currency"))))));
var values_370 = R.Join(new V[] {R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(R.Get(v_354, (V)"state")))).First, (V)"position")}, new Results(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(R.Get(v_356, (V)"state")))).First, (V)"position")));
V v_371 = values_370[0];
V v_372 = values_370[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_371, (V)"floor_id") == R.Get(v_372, (V)"floor_id")), () => (V)(R.Get(v_371, (V)"x") == R.Get(v_372, (V)"x"))), () => (V)(R.Get(v_371, (V)"y") == R.Get(v_372, (V)"y"))))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"chain rule requires three distinct same-chain boon activations"}, new Results(new V((RuleFunction)(args_373 => {

var values_374 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {(V)75.0d, V.Nil}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/chain"))).First))))));
V v_375 = values_374[0];
var values_376 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"order"), (V)3.0d)));
V v_377 = values_376[0];
var values_378 = R.Join(new V[] {}, new Results(R.Table()));
V v_379 = values_378[0];
var values_386 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_380 => {
V v_381 = args_380[0];
V v_382 = args_380[1];
if (R.Truth((V)(v_381 == (V)"AchievementEarned"))) {
var values_383 = R.Join(new V[] {}, new Results(v_382));
var obj_384 = v_379;
V key_385 = (V)((V)R.Length(v_379) + (V)1.0d);
R.Set(obj_384, key_385, values_383[0]);
}
return Results.Empty;
}))));
V v_387 = values_386[0];
V v_388 = V.Nil;
v_388 = new V((RuleFunction)(args_389 => {
V v_390 = args_389[0];
V v_391 = args_389[1];
R.Call(R.Get(v_14, (V)"observe"), R.Join(new V[] {v_375, R.Table(new Entry((V)"kind", (V)"BoonActivated"), new Entry((V)"boon_id", v_390), new Entry((V)"chain_id", v_391), new Entry((V)"source_id", R.Get(v_375, (V)"player_id")))}, new Results(v_387)));
return Results.Empty;
}));
{ double end_394 = R.Num((V)3.0d), step_395 = R.Num((V)1.0d);
for (double i_393 = R.Num((V)1.0d); step_395 > 0 ? i_393 <= end_394 : i_393 >= end_394; i_393 += step_395) {
V v_392 = i_393;
R.Call(R.Get(v_14, (V)"observe"), R.Join(new V[] {v_375, R.Table(new Entry((V)"kind", (V)"BoonActivated"), new Entry((V)"boon_id", R.Get(R.Get(v_10, (V)"order"), v_392)), new Entry((V)"chain_id", (V)6.0d), new Entry((V)"source_id", (V)999.0d))}, new Results(v_387)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(R.Get(v_375, (V)"meta"), (V)"earned"), v_377))))));
R.Call(v_388, R.Join(new V[] {R.Get(R.Get(v_10, (V)"order"), (V)1.0d)}, new Results((V)7.0d)));
R.Call(v_388, R.Join(new V[] {R.Get(R.Get(v_10, (V)"order"), (V)1.0d)}, new Results((V)7.0d)));
R.Call(v_388, R.Join(new V[] {R.Get(R.Get(v_10, (V)"order"), (V)2.0d)}, new Results((V)7.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(R.Get(v_375, (V)"meta"), (V)"earned"), v_377))))));
R.Call(v_388, R.Join(new V[] {R.Get(R.Get(v_10, (V)"order"), (V)3.0d)}, new Results((V)8.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(R.Get(v_375, (V)"meta"), (V)"earned"), v_377))))));
R.Call(v_388, R.Join(new V[] {R.Get(R.Get(v_10, (V)"order"), (V)3.0d)}, new Results((V)7.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(R.Get(v_375, (V)"meta"), (V)"earned"), v_377), () => (V)((V)R.Length(v_379) == (V)1.0d)))));
R.Call(v_388, R.Join(new V[] {R.Get(R.Get(v_10, (V)"order"), (V)4.0d)}, new Results((V)8.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_379) == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"normal legal seed three Victory awards Regent Fall"}, new Results(new V((RuleFunction)(args_396 => {

var values_397 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"complete_run"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/victory"))).First))))));
V v_398 = values_397[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_398, (V)"state"), (V)"victory"), () => (V)((V)R.Length(R.Get(v_398, (V)"trace")) > (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_398, (V)"state"), (V)"meta"), (V)"earned"), R.Get(R.Get(v_16, (V)"order"), (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_398, (V)"state"), (V)"meta"), (V)"earned"), R.Get(R.Get(v_16, (V)"order"), (V)4.0d)))));
var values_399 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_400 = values_399[0];
V v_401 = values_399[1];
foreach (var row_402 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_398, (V)"events"))))))) {
V v_403 = row_402[0];
V v_404 = row_402[1];
if (R.Truth((V)(R.Get(v_404, (V)"kind") == (V)"Victory"))) {
var values_405 = R.Join(new V[] {}, new Results((V)(v_400 + (V)1.0d)));
v_400 = values_405[0];
}
if (R.Truth(R.And((V)(R.Get(v_404, (V)"kind") == (V)"AchievementEarned"), () => (V)(R.Get(v_404, (V)"achievement_id") == R.Get(R.Get(v_16, (V)"order"), (V)4.0d))))) {
var values_406 = R.Join(new V[] {}, new Results((V)(v_401 + (V)1.0d)));
v_401 = values_406[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_400 == (V)1.0d), () => (V)(v_401 == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"eligible chest claims earn three-boon Chain Reaction through one attack"}, new Results(new V((RuleFunction)(args_407 => {

var values_408 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {(V)37.0d, (V)"core:map/test_room"}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/chain-play"))).First), new Entry((V)"available_boons", R.Call(R.Get(v_10, (V)"starter_order"), Results.Empty).First))))));
V v_409 = values_408[0];
var values_410 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_409), new Entry((V)"events", R.Table()), new Entry((V)"trace", R.Table()))));
V v_411 = values_410[0];
var values_412 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_chest"), R.Join(new V[] {v_409, (V)"core:chest/free_elite", (V)3.0d}, new Results((V)2.0d))));
V v_413 = values_412[0];
var values_414 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_chest"), R.Join(new V[] {v_409, (V)"core:chest/free_elite", (V)2.0d}, new Results((V)3.0d))));
V v_415 = values_414[0];
var values_416 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_chest"), R.Join(new V[] {v_409, (V)"core:chest/free_elite", (V)4.0d}, new Results((V)2.0d))));
V v_417 = values_416[0];
V v_418 = V.Nil;
v_418 = new V((RuleFunction)(args_419 => {
V v_420 = args_419[0];
V v_421 = args_419[1];
V v_422 = args_419[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_420, (V)"offers"), v_421), (V)"boon_id") == v_422))));
var values_423 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"action"), R.Join(new V[] {v_411}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_420, (V)"id")), new Entry((V)"offer_index", v_421))))));
V v_424 = values_423[0];
V v_425 = values_423[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_424}, new Results(v_425)));
return Results.Empty;
}));
R.Call(v_418, R.Join(new V[] {v_415, (V)2.0d}, new Results((V)"core:boon/storm_conductor")));
R.Call(v_418, R.Join(new V[] {v_413, (V)3.0d}, new Results((V)"core:boon/emberbrand")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"action"), R.Join(new V[] {v_411}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(v_418, R.Join(new V[] {v_417, (V)1.0d}, new Results((V)"core:boon/opening_cut")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"action"), R.Join(new V[] {v_411}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_426 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_409, (V)"core:enemy/mossbound_guard", (V)5.0d, (V)2.0d}, new Results((V)100000.0d))));
V v_427 = values_426[0];
var values_428 = R.Join(new V[] {(V)100.0d}, new Results((V)100.0d));
var obj_429 = v_427;
V key_430 = (V)"hp";
var obj_431 = v_427;
V key_432 = (V)"max_hp";
R.Set(obj_429, key_430, values_428[0]);
R.Set(obj_431, key_432, values_428[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"action"), R.Join(new V[] {v_411}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_433 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"order"), (V)3.0d)));
V v_434 = values_433[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(R.Get(R.Get(v_409, (V)"meta"), (V)"earned"), v_434), () => R.Get(R.Get(v_409, (V)"meta"), (V)"eligible")), () => (V)(!R.Truth(R.Get(v_409, (V)"game_over")))))));
var values_435 = R.Join(new V[] {}, new Results((V)0.0d));
V v_436 = values_435[0];
foreach (var row_437 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_411, (V)"events"))))))) {
V v_438 = row_437[0];
V v_439 = row_437[1];
if (R.Truth(R.And((V)(R.Get(v_439, (V)"kind") == (V)"AchievementEarned"), () => (V)(R.Get(v_439, (V)"achievement_id") == v_434)))) {
var values_440 = R.Join(new V[] {}, new Results((V)(v_436 + (V)1.0d)));
v_436 = values_440[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_436 == (V)1.0d), () => (V)((V)R.Length(R.Get(v_411, (V)"trace")) == (V)6.0d)))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"profile run IDs advance even when a seed repeats"}, new Results(new V((RuleFunction)(args_441 => {

var values_442 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_443 = values_442[0];

var values_444 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_443))));
V v_445 = values_444[0];
var values_446 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_445, "allocate_run", Results.Empty))));
V v_447 = values_446[0];
var values_448 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_445, "allocate_run", Results.Empty))));
V v_449 = values_448[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_447 != v_449))));
var values_450 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results(v_447))).First))))));
V v_451 = values_450[0];
var values_452 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results(v_449))).First))))));
V v_453 = values_452[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_451, (V)"run_seed") == R.Get(v_453, (V)"run_seed")), () => (V)(R.Get(R.Get(v_451, (V)"meta"), (V)"run_id") != R.Get(R.Get(v_453, (V)"meta"), (V)"run_id"))))));
var values_454 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Invoke(v_445, "inspect", Results.Empty).First, (V)"record"), (V)"slot")));
V v_455 = values_454[0];
var values_456 = R.Join(new V[] {}, new Results((V)"truncated"));
var obj_457 = v_443;
V key_458 = R.Get(R.Get(v_18, (V)"slots"), v_455);
R.Set(obj_457, key_458, values_456[0]);
var values_459 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_445, "allocate_run", Results.Empty))));
V v_460 = values_459[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_460 != v_449), () => (V)(v_460 != v_447)))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"verified award survives corruption of either profile generation"}, new Results(new V((RuleFunction)(args_461 => {

var values_462 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_463 = values_462[0];

var values_464 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_463))));
V v_465 = values_464[0];
var values_466 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"order"), (V)1.0d)));
V v_467 = values_466[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_465, "commit_awards", R.Join(new V[] {}, new Results(R.Table(new Entry(v_467, (V)true)))))));
var values_468 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Invoke(v_465, "inspect", Results.Empty).First, (V)"record"), (V)"slot")));
V v_469 = values_468[0];
var values_470 = R.Join(new V[] {}, new Results((V)"truncated"));
var obj_471 = v_463;
V key_472 = R.Get(R.Get(v_18, (V)"slots"), v_469);
R.Set(obj_471, key_472, values_470[0]);
var values_473 = R.Join(new V[] {}, R.Invoke(v_465, "inspect", Results.Empty));
V v_474 = values_473[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_474, (V)"status") == (V)"recovered"), () => R.Get(R.Get(R.Get(v_474, (V)"profile"), (V)"earned"), v_467)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_465, "commit_awards", R.Join(new V[] {}, new Results(R.Table(new Entry(v_467, (V)true)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Invoke(v_465, "inspect", Results.Empty).First, (V)"profile"), (V)"earned"), v_467))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"pending terminal award outbox survives restart and idempotent commit"}, new Results(new V((RuleFunction)(args_475 => {

var values_476 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_477 = values_476[0];

var values_478 = R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"new"), R.Join(new V[] {}, new Results(v_477))));
V v_479 = values_478[0];

var values_480 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_477))));
V v_481 = values_480[0];
var values_482 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {(V)66.0d, V.Nil}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/200"))).First))))));
V v_483 = values_482[0];
var values_484 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"order"), (V)1.0d)));
V v_485 = values_484[0];
var values_486 = R.Join(new V[] {}, new Results((V)true));
var obj_487 = R.Get(R.Get(v_483, (V)"meta"), (V)"earned");
V key_488 = v_485;
R.Set(obj_487, key_488, values_486[0]);

var values_489 = R.Join(new V[] {}, new Results((V)true));
var obj_490 = R.Get(R.Get(v_483, (V)"meta"), (V)"pending");
V key_491 = v_485;
R.Set(obj_490, key_491, values_489[0]);
var values_492 = R.Join(new V[] {}, new Results((V)true));
var obj_493 = v_483;
V key_494 = (V)"game_over";
R.Set(obj_493, key_494, values_492[0]);

var values_495 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_496 = R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_483))).First;
V key_497 = (V)"hp";
R.Set(obj_496, key_497, values_495[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_479, "pending_terminal", R.Join(new V[] {}, new Results(v_483)))));
var values_498 = R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_20, (V)"new"), R.Join(new V[] {}, new Results(v_477))).First, "inspect", Results.Empty));
V v_499 = values_498[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_499, (V)"status") == (V)"pending_terminal"), () => (V)(!R.Truth(R.Invoke(R.Call(R.Get(v_20, (V)"new"), R.Join(new V[] {}, new Results(v_477))).First, "load", Results.Empty).First))))));
var values_500 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_499, (V)"record"), (V)"slot")));
V v_501 = values_500[0];
var values_502 = R.Join(new V[] {}, R.Invoke(R.Get(v_477, R.Get(R.Get(v_20, (V)"slots"), v_501)), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)50.0d))));
var obj_503 = v_477;
V key_504 = R.Get(R.Get(v_20, (V)"slots"), v_501);
R.Set(obj_503, key_504, values_502[0]);
var values_505 = R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_20, (V)"new"), R.Join(new V[] {}, new Results(v_477))).First, "inspect", Results.Empty));
V v_506 = values_505[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_506, (V)"status") == (V)"pending_terminal"), () => (V)(R.Get(v_506, (V)"invalid") == (V)1.0d)), () => R.Get(R.Get(R.Get(R.Get(v_506, (V)"state"), (V)"meta"), (V)"pending"), v_485)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_481, "commit_awards", R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_499, (V)"state"), (V)"meta"), (V)"pending"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_481, "commit_awards", R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_499, (V)"state"), (V)"meta"), (V)"pending"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_479, "tombstone", Results.Empty)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_479, "inspect", Results.Empty).First, (V)"status") == (V)"terminal"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Invoke(v_481, "inspect", Results.Empty).First, (V)"profile"), (V)"earned"), v_485))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"failed profile commit leaves terminal outbox recoverable"}, new Results(new V((RuleFunction)(args_507 => {

var values_508 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_509 = values_508[0];

var values_510 = R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"new"), R.Join(new V[] {}, new Results(v_509))));
V v_511 = values_510[0];

var values_512 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_509))));
V v_513 = values_512[0];
var values_514 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {(V)78.0d, V.Nil}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/fault"))).First))))));
V v_515 = values_514[0];
var values_516 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"order"), (V)1.0d)));
V v_517 = values_516[0];
var values_518 = R.Join(new V[] {}, new Results((V)true));
var obj_519 = R.Get(R.Get(v_515, (V)"meta"), (V)"earned");
V key_520 = v_517;
R.Set(obj_519, key_520, values_518[0]);

var values_521 = R.Join(new V[] {}, new Results((V)true));
var obj_522 = R.Get(R.Get(v_515, (V)"meta"), (V)"pending");
V key_523 = v_517;
R.Set(obj_522, key_523, values_521[0]);
var values_524 = R.Join(new V[] {}, new Results((V)true));
var obj_525 = v_515;
V key_526 = (V)"game_over";
R.Set(obj_525, key_526, values_524[0]);

var values_527 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_528 = R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_515))).First;
V key_529 = (V)"hp";
R.Set(obj_528, key_529, values_527[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_511, "pending_terminal", R.Join(new V[] {}, new Results(v_515)))));
var values_530 = R.Join(new V[] {}, new Results((V)true));
var obj_531 = v_509;
V key_532 = (V)"fail";
R.Set(obj_531, key_532, values_530[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_513, "commit_awards", R.Join(new V[] {}, new Results(R.Get(R.Get(v_515, (V)"meta"), (V)"pending")))).First)))));
var values_533 = R.Join(new V[] {}, new Results((V)false));
var obj_534 = v_509;
V key_535 = (V)"fail";
R.Set(obj_534, key_535, values_533[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_511, "inspect", Results.Empty).First, (V)"status") == (V)"pending_terminal"))));
var values_536 = R.Join(new V[] {}, new Results(R.Get(R.Invoke(R.Call(R.Get(v_20, (V)"new"), R.Join(new V[] {}, new Results(v_509))).First, "inspect", Results.Empty).First, (V)"state")));
V v_537 = values_536[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_537, (V)"meta"), (V)"pending"), v_517))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_513, "commit_awards", R.Join(new V[] {}, new Results(R.Get(R.Get(v_537, (V)"meta"), (V)"pending"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_511, "tombstone", Results.Empty)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Invoke(v_513, "inspect", Results.Empty).First, (V)"profile"), (V)"earned"), v_517))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_511, "inspect", Results.Empty).First, (V)"status") == (V)"terminal"))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"verified profile award survives a failed terminal tombstone retry"}, new Results(new V((RuleFunction)(args_538 => {

var values_539 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_540 = values_539[0];

var values_541 = R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"new"), R.Join(new V[] {}, new Results(v_540))));
V v_542 = values_541[0];

var values_543 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_540))));
V v_544 = values_543[0];
var values_545 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {(V)80.0d, V.Nil}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/tombstone"))).First))))));
V v_546 = values_545[0];
var values_547 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_16, (V)"order"), (V)1.0d)));
V v_548 = values_547[0];
var values_549 = R.Join(new V[] {}, new Results((V)true));
var obj_550 = R.Get(R.Get(v_546, (V)"meta"), (V)"earned");
V key_551 = v_548;
R.Set(obj_550, key_551, values_549[0]);

var values_552 = R.Join(new V[] {}, new Results((V)true));
var obj_553 = R.Get(R.Get(v_546, (V)"meta"), (V)"pending");
V key_554 = v_548;
R.Set(obj_553, key_554, values_552[0]);
var values_555 = R.Join(new V[] {}, new Results((V)true));
var obj_556 = v_546;
V key_557 = (V)"game_over";
R.Set(obj_556, key_557, values_555[0]);

var values_558 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_559 = R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_546))).First;
V key_560 = (V)"hp";
R.Set(obj_559, key_560, values_558[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_542, "pending_terminal", R.Join(new V[] {}, new Results(v_546)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_544, "commit_awards", R.Join(new V[] {}, new Results(R.Get(R.Get(v_546, (V)"meta"), (V)"pending"))))));
var values_561 = R.Join(new V[] {}, new Results((V)true));
var obj_562 = v_540;
V key_563 = (V)"fail";
R.Set(obj_562, key_563, values_561[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_542, "tombstone", Results.Empty).First)))));
var values_564 = R.Join(new V[] {}, new Results((V)false));
var obj_565 = v_540;
V key_566 = (V)"fail";
R.Set(obj_565, key_566, values_564[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_542, "inspect", Results.Empty).First, (V)"status") == (V)"pending_terminal"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_544, "commit_awards", R.Join(new V[] {}, new Results(R.Get(R.Get(v_546, (V)"meta"), (V)"pending"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_542, "tombstone", Results.Empty)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Invoke(v_542, "inspect", Results.Empty).First, (V)"status") == (V)"terminal"), () => R.Get(R.Get(R.Get(R.Invoke(v_544, "inspect", Results.Empty).First, (V)"profile"), (V)"earned"), v_548)))));
return Results.Empty;
})))));
R.Call(v_24, R.Join(new V[] {(V)"meta queries and disk commits draw no simulation RNG or world time"}, new Results(new V((RuleFunction)(args_567 => {

var values_568 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)79.0d}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_14, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"local-run/pure"))).First))))));
V v_569 = values_568[0];
var values_570 = R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_569))))));
V v_571 = values_570[0];
var values_572 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_573 = values_572[0];

var values_574 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"new"), R.Join(new V[] {}, new Results(v_573))));
V v_575 = values_574[0];
var values_576 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_575, "allocate_run", Results.Empty))));
V v_577 = values_576[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_577)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_575, "commit_awards", R.Join(new V[] {}, new Results(R.Table())))));
R.Call(R.Get(v_14, (V)"validate_pool"), R.Join(new V[] {}, new Results(R.Get(v_569, (V)"available_boons"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_571 == R.Call(R.Get(v_20, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_569))))).First))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
