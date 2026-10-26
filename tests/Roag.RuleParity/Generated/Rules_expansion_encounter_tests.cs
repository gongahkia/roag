// Static C# port of tests/expansion_encounter_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: f8b5e147fa006f9431b38fdb4959ccab1f5d8c3e6ee6160e66ea8011cca1f815
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_expansion_encounter_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.regular_ai"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_17 = values_16[0];
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
var values_22 = R.Join(new V[] {}, new Results(R.Table()));
V v_23 = values_22[0];
foreach (var row_24 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_20, () => R.Table()))))))) {
V v_25 = row_24[0];
V v_26 = row_24[1];
if (R.Truth((V)(R.Get(v_26, (V)"kind") == v_21))) {
var values_27 = R.Join(new V[] {}, new Results(v_26));
var obj_28 = v_23;
V key_29 = (V)((V)R.Length(v_23) + (V)1.0d);
R.Set(obj_28, key_29, values_27[0]);
}
}
return R.Join(new V[] {}, new Results(v_23));
return Results.Empty;
}));
V v_30 = V.Nil;
v_30 = new V((RuleFunction)(args_31 => {
V v_32 = args_31[0];
V v_33 = args_31[1];
V v_34 = args_31[2];
V v_35 = args_31[3];
V v_36 = args_31[4];
var values_37 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {(V)3.0d}, new Results((V)"core:map/test_room"))));
V v_38 = values_37[0];
var values_39 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_38))));
V v_40 = values_39[0];
var values_41 = R.Join(new V[] {R.Or(v_35, () => (V)9.0d)}, new Results(R.Or(v_36, () => (V)6.0d)));
var obj_42 = R.Get(v_40, (V)"position");
V key_43 = (V)"x";
var obj_44 = R.Get(v_40, (V)"position");
V key_45 = (V)"y";
R.Set(obj_42, key_43, values_41[0]);
R.Set(obj_44, key_45, values_41[1]);
var values_46 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_38, v_32, R.Or(v_33, () => (V)9.0d), R.Or(v_34, () => (V)8.0d)}, new Results((V)0.0d))));
V v_47 = values_46[0];
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_38)));
return R.Join(new V[] {v_38}, new Results(v_47));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"seven biome elites have unique IDs and two locked footprints"}, new Results(new V((RuleFunction)(args_48 => {

var values_49 = R.Join(new V[] {}, new Results(R.Table()));
V v_50 = values_49[0];
foreach (var row_51 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"order"))))))) {
V v_52 = row_51[0];
V v_53 = row_51[1];
var values_54 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"get"), R.Join(new V[] {}, new Results(v_53))));
V v_55 = values_54[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_50, R.Get(v_55, (V)"elite_id")))))));
var values_56 = R.Join(new V[] {}, new Results((V)true));
var obj_57 = v_50;
V key_58 = R.Get(v_55, (V)"elite_id");
R.Set(obj_57, key_58, values_56[0]);
var values_59 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_52 > (V)1.0d), () => R.Table(new Entry((V)"index", (V)2.0d), new Entry((V)"biome_id", v_53), new Entry((V)"route_history", R.Table(new Entry(1, new Results(R.Invoke(v_53, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First)))))), () => V.Nil)));
V v_60 = values_59[0];
var values_61 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"stage_options", v_60))))));
V v_62 = values_61[0];
var values_63 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_62}, new Results(R.Get(R.Get(v_62, (V)"stage"), (V)"elite_id")))))));
V v_64 = values_63[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_64, (V)"definition_id") == R.Get(v_55, (V)"elite_id")))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_62))))))).First}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_62)))));
if (R.Truth((V)(v_52 > (V)1.0d))) {
var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_62))));
V v_66 = values_65[0];
var values_67 = R.Join(new V[] {}, new Results((V)false));
V v_68 = values_67[0];
foreach (var row_69 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)2.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)2.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)2.0d)))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)6, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)7, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)8, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_70 = row_69[0];
V v_71 = row_69[1];
var values_72 = R.Join(new V[] {(V)(R.Get(R.Get(v_64, (V)"position"), (V)"x") + R.Get(v_71, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_64, (V)"position"), (V)"y") + R.Get(v_71, (V)2.0d))));
V v_73 = values_72[0];
V v_74 = values_72[1];
if (R.Truth(R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {R.Get(v_62, (V)"generated_map"), v_73}, new Results(v_74))).First)) {
var values_75 = R.Join(new V[] {v_73}, new Results(v_74));
var obj_76 = R.Get(v_66, (V)"position");
V key_77 = (V)"x";
var obj_78 = R.Get(v_66, (V)"position");
V key_79 = (V)"y";
R.Set(obj_76, key_77, values_75[0]);
R.Set(obj_78, key_79, values_75[1]);
var values_80 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_81 = R.Get(v_64, (V)"ai_memory");
V key_82 = (V)"attacks";
R.Set(obj_81, key_82, values_80[0]);
var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_62}, new Results(v_64))));
V v_84 = values_83[0];
var values_85 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_86 = R.Get(v_64, (V)"ai_memory");
V key_87 = (V)"attacks";
R.Set(obj_86, key_87, values_85[0]);
var values_88 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_62}, new Results(v_64))));
V v_89 = values_88[0];
if (R.Truth(R.And((V)(R.Get(v_84, (V)"kind") == (V)"windup"), () => (V)(R.Get(v_89, (V)"kind") == (V)"windup")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_84, (V)"ability_id") != R.Get(v_89, (V)"ability_id")), () => (V)((V)R.Length(R.Get(v_84, (V)"cells")) > (V)0.0d)), () => (V)((V)R.Length(R.Get(v_89, (V)"cells")) > (V)0.0d)))));
var values_90 = R.Join(new V[] {}, new Results((V)true));
var obj_91 = v_64;
V key_92 = (V)"phase_two";
R.Set(obj_91, key_92, values_90[0]);
var values_93 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_62}, new Results(v_64))));
V v_94 = values_93[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_94, (V)"ability_id") == R.Get(v_84, (V)"ability_id")))));
var values_95 = R.Join(new V[] {}, new Results((V)true));
v_68 = values_95[0];

break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_68}, new Results(R.Concat((V)"elite lacked two observable patterns: ", v_53))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_7, (V)"order")) == (V)7.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ten regular roles are distinct and occur in biome pools"}, new Results(new V((RuleFunction)(args_96 => {

var values_97 = R.Join(new V[] {(V)0.0d}, new Results(R.Table()));
V v_98 = values_97[0];
V v_99 = values_97[1];
foreach (var row_100 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_13, (V)"roles"))))))) {
V v_101 = row_100[0];
V v_102 = row_100[1];
var values_103 = R.Join(new V[] {}, new Results((V)(v_98 + (V)1.0d)));
v_98 = values_103[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_9, (V)"get_enemy"), R.Join(new V[] {}, new Results(v_101))).First, (V)"ai") == (V)"expansion_regular"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_99, v_102))))));

var values_104 = R.Join(new V[] {}, new Results((V)true));
var obj_105 = v_99;
V key_106 = v_102;
R.Set(obj_105, key_106, values_104[0]);
var values_107 = R.Join(new V[] {}, new Results((V)false));
V v_108 = values_107[0];
foreach (var row_109 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"order"))))))) {
V v_110 = row_109[0];
V v_111 = row_109[1];
foreach (var row_112 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_7, (V)"get"), R.Join(new V[] {}, new Results(v_111))).First, (V)"enemy_ids"))))))) {
V v_113 = row_112[0];
V v_114 = row_112[1];
if (R.Truth((V)(v_114 == v_101))) {
var values_115 = R.Join(new V[] {}, new Results((V)true));
v_108 = values_115[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_108}, new Results(R.Concat((V)"unreachable enemy role: ", v_101))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_98 == (V)10.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"regular attack decisions expose positioning, coordination and retaliation"}, new Results(new V((RuleFunction)(args_116 => {

var values_117 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {(V)"core:enemy/glass_charger", (V)7.0d, (V)8.0d, (V)7.0d}, new Results((V)5.0d))));
V v_118 = values_117[0];
V v_119 = values_117[1];
var values_120 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_118}, new Results(v_119))));
V v_121 = values_120[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_121, (V)"ability_id") == (V)"core:ability/glass_charge"), () => (V)(R.Get(R.Get(v_121, (V)"advance"), (V)"max_steps") == (V)3.0d)), () => (V)((V)R.Length(R.Get(v_121, (V)"cells")) == (V)3.0d)))));
var values_122 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"core:enemy/mire_binder"))));
v_118 = values_122[0];
v_119 = values_122[1];
var values_123 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_118}, new Results(v_119))));
v_121 = values_123[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_121, (V)"ability_id") == (V)"core:ability/mire_ring"))));
foreach (var row_124 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_121, (V)"cells"))))))) {
V v_125 = row_124[0];
V v_126 = row_124[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_126, (V)"x") != (V)9.0d), () => (V)(R.Get(v_126, (V)"y") != (V)6.0d))}, new Results((V)"denial ring must leave locked center safe")));
}
var values_127 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {(V)"core:enemy/pack_chorister", (V)9.0d, (V)8.0d, (V)9.0d}, new Results((V)7.0d))));
v_118 = values_127[0];
v_119 = values_127[1];
R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_118, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)8.0d}, new Results((V)0.0d)));
var values_128 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_118}, new Results(v_119))));
v_121 = values_128[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_121, (V)"ability_id") == (V)"core:ability/pack_chorus"), () => (V)(R.Get(v_121, (V)"damage_bonus") == (V)1.0d)))));
var values_129 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {(V)"core:enemy/bastion_bulwark", (V)9.0d, (V)8.0d, (V)9.0d}, new Results((V)7.0d))));
v_118 = values_129[0];
v_119 = values_129[1];
var values_130 = R.Join(new V[] {}, new Results((V)true));
var obj_131 = R.Get(v_119, (V)"ai_memory");
V key_132 = (V)"retaliate";
R.Set(obj_131, key_132, values_130[0]);
var values_133 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_118}, new Results(v_119))));
v_121 = values_133[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_121, (V)"ability_id") == (V)"core:ability/bulwark_retort"), () => R.Get(v_121, (V)"clear_retaliate")))));
var values_134 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {(V)"core:enemy/gloom_strider", (V)9.0d, (V)8.0d, (V)9.0d}, new Results((V)7.0d))));
v_118 = values_134[0];
v_119 = values_134[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_118}, new Results(v_119))).First, (V)"kind") == (V)"move"))));
var values_135 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {(V)"core:enemy/vault_lurker", (V)9.0d, (V)8.0d, (V)9.0d}, new Results((V)6.0d))));
v_118 = values_135[0];
v_119 = values_135[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_118}, new Results(v_119))).First, (V)"ability_id") == (V)"core:ability/vault_ambush"))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(R.Get(v_119, (V)"ai_memory"), (V)"alerted")))}, new Results((V)"AI decision must not mutate runtime")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"summoner windup, pending save and canonical spawned enemy agree"}, new Results(new V((RuleFunction)(args_136 => {

var values_137 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"core:enemy/forge_conjurer"))));
V v_138 = values_137[0];
V v_139 = values_137[1];
var values_140 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_138}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_141 = values_140[0];
V v_142 = values_140[1];
V v_143 = values_140[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_141, () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_143}, new Results((V)"WindupStarted"))).First) >= (V)1.0d)))));
var values_144 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_138))));
V v_145 = values_144[0];
var values_146 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"restore"), R.Join(new V[] {}, new Results(v_145))));
V v_147 = values_146[0];
var values_148 = R.Join(new V[] {}, new Results((V)false));
V v_149 = values_148[0];
{ double end_152 = R.Num((V)3.0d), step_153 = R.Num((V)1.0d);
for (double i_151 = R.Num((V)1.0d); step_153 > 0 ? i_151 <= end_152 : i_151 >= end_152; i_151 += step_153) {
V v_150 = i_151;
var values_154 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_138}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_155 = values_154[0];
V v_156 = values_154[1];
V v_157 = values_154[2];
var values_158 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_147}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_159 = values_158[0];
V v_160 = values_158[1];
V v_161 = values_158[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_155, () => v_159))));

R.Call(v_3, R.Join(new V[] {v_157}, new Results(v_161)));
if (R.Truth((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_157}, new Results((V)"EnemySummoned"))).First) > (V)0.0d))) {
var values_162 = R.Join(new V[] {}, new Results((V)true));
v_149 = values_162[0];

break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_149, () => (V)(R.Get(R.Get(v_139, (V)"ai_memory"), (V)"summons") == (V)1.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_138))).First}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_147)))));
var values_163 = Results.Empty;
V v_164 = values_163[0];
foreach (var row_165 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_138, (V)"entities"))))))) {
V v_166 = row_165[0];
V v_167 = row_165[1];
if (R.Truth((V)(R.Get(v_167, (V)"summoned_by_id") == R.Get(v_139, (V)"id")))) {
var values_168 = R.Join(new V[] {}, new Results(v_167));
v_164 = values_168[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_164, () => (V)(R.Get(v_164, (V)"definition_id") == (V)"core:enemy/ruin_skitter")), () => (V)(R.Get(v_164, (V)"faction") == R.Get(v_139, (V)"faction"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"support heals wounded allies at locked cells and charger traverses only open path"}, new Results(new V((RuleFunction)(args_169 => {

var values_170 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {}, new Results((V)"core:enemy/cinder_mender"))));
V v_171 = values_170[0];
V v_172 = values_170[1];
var values_173 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_171, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)8.0d}, new Results((V)0.0d))));
V v_174 = values_173[0];
var values_175 = R.Join(new V[] {}, new Results((V)(R.Get(v_174, (V)"max_hp") - (V)4.0d)));
var obj_176 = v_174;
V key_177 = (V)"hp";
R.Set(obj_176, key_177, values_175[0]);
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_171}, new Results(R.Get(v_174, (V)"id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_171}, new Results(v_172))).First, (V)"support"), (V)"kind") == (V)"heal"))));
var values_178 = R.Join(new V[] {}, new Results((V)false));
V v_179 = values_178[0];
{ double end_182 = R.Num((V)4.0d), step_183 = R.Num((V)1.0d);
for (double i_181 = R.Num((V)1.0d); step_183 > 0 ? i_181 <= end_182 : i_181 >= end_182; i_181 += step_183) {
V v_180 = i_181;
var values_184 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_171}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_185 = values_184[0];
V v_186 = values_184[1];
V v_187 = values_184[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_185)));
if (R.Truth((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_187}, new Results((V)"Healed"))).First) > (V)0.0d))) {
var values_188 = R.Join(new V[] {}, new Results((V)true));
v_179 = values_188[0];

break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_179, () => (V)(R.Get(v_174, (V)"hp") == (V)(R.Get(v_174, (V)"max_hp") - (V)1.0d))))));
var values_189 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {(V)"core:enemy/glass_charger", (V)7.0d, (V)8.0d, (V)7.0d}, new Results((V)5.0d))));
v_171 = values_189[0];
var values_190 = R.Join(new V[] {}, new Results((V)false));
V v_191 = values_190[0];
{ double end_194 = R.Num((V)3.0d), step_195 = R.Num((V)1.0d);
for (double i_193 = R.Num((V)1.0d); step_195 > 0 ? i_193 <= end_194 : i_193 >= end_194; i_193 += step_195) {
V v_192 = i_193;
var values_196 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_171}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_197 = values_196[0];
V v_198 = values_196[1];
V v_199 = values_196[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_197)));
foreach (var row_200 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_199)))))) {
V v_201 = row_200[0];
V v_202 = row_200[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_202, (V)"kind") == (V)"Moved"), () => R.Get(v_202, (V)"path")), () => (V)((V)R.Length(R.Get(v_202, (V)"path")) > (V)0.0d)))) {
var values_203 = R.Join(new V[] {}, new Results((V)true));
v_191 = values_203[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_202, (V)"x") == (V)7.0d), () => (V)(R.Get(v_202, (V)"y") == (V)6.0d)))));
}
}
if (R.Truth(v_191)) {
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_191}, new Results((V)"charger impact did not move along its locked line")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"artillery friendly fire uses one ordered canonical damage batch"}, new Results(new V((RuleFunction)(args_204 => {

var values_205 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {(V)"core:enemy/crystal_bombardier", (V)9.0d, (V)8.0d, (V)9.0d}, new Results((V)6.0d))));
V v_206 = values_205[0];
V v_207 = values_205[1];
var values_208 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_206, (V)"core:enemy/mossbound_guard", (V)11.0d, (V)6.0d}, new Results((V)0.0d))));
V v_209 = values_208[0];
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_206}, new Results(R.Get(v_209, (V)"id"))));
var values_210 = Results.Empty;
V v_211 = values_210[0];
V v_212 = values_210[1];
V v_213 = values_210[2];
{ double end_216 = R.Num((V)4.0d), step_217 = R.Num((V)1.0d);
for (double i_215 = R.Num((V)1.0d); step_217 > 0 ? i_215 <= end_216 : i_215 >= end_216; i_215 += step_217) {
V v_214 = i_215;
var values_218 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_206}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_219 = values_218[0];
V v_220 = values_218[1];
V v_221 = values_218[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_219)));
foreach (var row_222 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_221)))))) {
V v_223 = row_222[0];
V v_224 = row_222[1];
if (R.Truth(R.And((V)(R.Get(v_224, (V)"kind") == (V)"DamageBatchResolved"), () => (V)(R.Get(v_224, (V)"source_id") == R.Get(v_207, (V)"id"))))) {
var values_225 = R.Join(new V[] {}, new Results(v_224));
v_211 = values_225[0];
{ double end_228 = R.Num((V)R.Length(v_221)), step_229 = R.Num((V)1.0d);
for (double i_227 = R.Num((V)(v_223 + (V)1.0d)); step_229 > 0 ? i_227 <= end_228 : i_227 >= end_228; i_227 += step_229) {
V v_226 = i_227;
var values_230 = R.Join(new V[] {}, new Results(R.Get(v_221, v_226)));
V v_231 = values_230[0];
if (R.Truth((V)(R.Get(v_231, (V)"kind") == (V)"DamageTaken"))) {
if (R.Truth((V)(R.Get(v_231, (V)"target_id") == R.Get(v_206, (V)"player_id")))) {
var values_232 = R.Join(new V[] {}, new Results((V)true));
v_212 = values_232[0];
}
if (R.Truth((V)(R.Get(v_231, (V)"target_id") == R.Get(v_209, (V)"id")))) {
var values_233 = R.Join(new V[] {}, new Results((V)true));
v_213 = values_233[0];
}
}
}
}
}
}
if (R.Truth(v_211)) {
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(v_211, () => v_212), () => v_213), () => (V)((V)R.Length(R.Get(v_211, (V)"outcomes")) == (V)2.0d))}, new Results((V)"artillery friendly fire did not batch")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"retaliator arms only after actual hostile HP loss"}, new Results(new V((RuleFunction)(args_234 => {

var values_235 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {(V)9.0d}, new Results((V)"core:map/test_room"))));
V v_236 = values_235[0];
var values_237 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_236, (V)"core:enemy/bastion_bulwark", (V)3.0d, (V)2.0d}, new Results((V)0.0d))));
V v_238 = values_237[0];
var values_239 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_236}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_240 = values_239[0];
V v_241 = values_239[1];
V v_242 = values_239[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_240, () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_242}, new Results((V)"RetaliationReadied"))).First) == (V)1.0d)), () => R.Get(v_238, (V)"pending_attack")), () => (V)(R.Get(R.Get(v_238, (V)"pending_attack"), (V)"ability_id") == (V)"core:ability/bulwark_retort")))));
var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {(V)9.0d}, new Results((V)"core:map/test_room"))));
V v_244 = values_243[0];
var values_245 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_244, (V)"core:enemy/bastion_bulwark", (V)3.0d, (V)2.0d}, new Results((V)0.0d))));
V v_246 = values_245[0];
var values_247 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_248 = R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_244))).First;
V key_249 = (V)"damage";
R.Set(obj_248, key_249, values_247[0]);
var values_250 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_244}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_251 = values_250[0];
V v_252 = values_250[1];
V v_253 = values_250[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_253}, new Results((V)"DamageBlocked"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_253}, new Results((V)"RetaliationReadied"))).First) == (V)0.0d)), () => (V)(!R.Truth(R.Get(R.Get(v_246, (V)"ai_memory"), (V)"retaliate")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Mossbreaker destroys a real barricade before pursuing"}, new Results(new V((RuleFunction)(args_254 => {

var values_255 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {(V)9.0d}, new Results((V)"core:map/test_room"))));
V v_256 = values_255[0];
V v_257 = V.Nil;
v_257 = new V((RuleFunction)(args_258 => {
V v_259 = args_258[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_259))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_259));
}
var values_260 = R.Join(new V[] {}, new Results(R.Table()));
V v_261 = values_260[0];
foreach (var row_262 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_259)))))) {
V v_263 = row_262[0];
V v_264 = row_262[1];
var values_265 = R.Join(new V[] {}, R.Call(v_257, R.Join(new V[] {}, new Results(v_264))));
var obj_266 = v_261;
V key_267 = v_263;
R.Set(obj_266, key_267, values_265[0]);
}
return R.Join(new V[] {}, new Results(v_261));
return Results.Empty;
}));
var values_268 = R.Join(new V[] {}, R.Call(v_257, R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/test_room"))))));
var obj_269 = v_256;
V key_270 = (V)"generated_map";
R.Set(obj_269, key_270, values_268[0]);
var values_271 = R.Join(new V[] {}, new Results(R.Get(v_256, (V)"generated_map")));
V v_272 = values_271[0];
var values_273 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_272, (V)"rows"), (V)8.0d), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)7.0d))).First, R.Concat((V)"B", R.Invoke(R.Get(R.Get(v_272, (V)"rows"), (V)8.0d), "sub", R.Join(new V[] {}, new Results((V)9.0d))).First))));
var obj_274 = R.Get(v_272, (V)"rows");
V key_275 = (V)8.0d;
R.Set(obj_274, key_275, values_273[0]);
var values_276 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_256))));
V v_277 = values_276[0];
var values_278 = R.Join(new V[] {(V)7.0d}, new Results((V)8.0d));
var obj_279 = R.Get(v_277, (V)"position");
V key_280 = (V)"x";
var obj_281 = R.Get(v_277, (V)"position");
V key_282 = (V)"y";
R.Set(obj_279, key_280, values_278[0]);
R.Set(obj_281, key_282, values_278[1]);
var values_283 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_256, (V)"core:enemy/mossbreaker", (V)9.0d, (V)8.0d}, new Results((V)0.0d))));
V v_284 = values_283[0];
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_256)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_256}, new Results(v_284))).First, (V)"ability_id") == (V)"core:ability/mossbreaker_crack"))));
var values_285 = R.Join(new V[] {}, new Results((V)false));
V v_286 = values_285[0];
{ double end_289 = R.Num((V)4.0d), step_290 = R.Num((V)1.0d);
for (double i_288 = R.Num((V)1.0d); step_290 > 0 ? i_288 <= end_289 : i_288 >= end_289; i_288 += step_290) {
V v_287 = i_288;
var values_291 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_256}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_292 = values_291[0];
V v_293 = values_291[1];
V v_294 = values_291[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_292)));
if (R.Truth((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_294}, new Results((V)"TerrainBroken"))).First) > (V)0.0d))) {
var values_295 = R.Join(new V[] {}, new Results((V)true));
v_286 = values_295[0];

break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_286, () => (V)(R.Invoke(R.Get(R.Get(v_272, (V)"rows"), (V)8.0d), "sub", R.Join(new V[] {(V)8.0d}, new Results((V)8.0d))).First == (V)".")), () => R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {v_272, (V)8.0d}, new Results((V)8.0d))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new elite pending windup and phase survive authoritative continuation"}, new Results(new V((RuleFunction)(args_296 => {

var values_297 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_7, (V)"order"), (V)2.0d)));
V v_298 = values_297[0];
var values_299 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)11.0d}, new Results(R.Table(new Entry((V)"stage_options", R.Table(new Entry((V)"index", (V)2.0d), new Entry((V)"biome_id", v_298), new Entry((V)"route_history", R.Table(new Entry((V)1, (V)"core:route/fungal_hollows"))))))))));
V v_300 = values_299[0];
var values_301 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_300}, new Results(R.Get(R.Get(v_300, (V)"stage"), (V)"elite_id")))));
V v_302 = values_301[0];
foreach (var row_303 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_300, (V)"entities"))))))) {
V v_304 = row_303[0];
V v_305 = row_303[1];
if (R.Truth((V)(R.Get(v_305, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_300}, new Results(R.Get(v_305, (V)"id"))));
}
}
var values_306 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_300))));
V v_307 = values_306[0];
var values_308 = Results.Empty;
V v_309 = values_308[0];
V v_310 = values_308[1];
foreach (var row_311 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_312 = row_311[0];
V v_313 = row_311[1];
var values_314 = R.Join(new V[] {(V)(R.Get(R.Get(v_302, (V)"position"), (V)"x") + R.Get(v_313, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_302, (V)"position"), (V)"y") + R.Get(v_313, (V)2.0d))));
V v_315 = values_314[0];
V v_316 = values_314[1];
var values_317 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {R.Get(v_300, (V)"generated_map"), v_315}, new Results(v_316))));
V v_318 = values_317[0];
foreach (var row_319 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_300, (V)"entities"))))))) {
V v_320 = row_319[0];
V v_321 = row_319[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_321, (V)"id") != R.Get(v_307, (V)"id")), () => R.Get(v_321, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_321, (V)"position"), (V)"x") == v_315)), () => (V)(R.Get(R.Get(v_321, (V)"position"), (V)"y") == v_316)))) {
var values_322 = R.Join(new V[] {}, new Results((V)false));
v_318 = values_322[0];
}
}
if (R.Truth(v_318)) {
var values_323 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_315), new Entry((V)"y", v_316))));
v_309 = values_323[0];

var values_324 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-R.Get(v_313, (V)1.0d))), new Entry((V)2, (-R.Get(v_313, (V)2.0d))))));
v_310 = values_324[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_309}, new Results((V)"no elite approach")));
var values_325 = R.Join(new V[] {R.Get(v_309, (V)"x")}, new Results(R.Get(v_309, (V)"y")));
var obj_326 = R.Get(v_307, (V)"position");
V key_327 = (V)"x";
var obj_328 = R.Get(v_307, (V)"position");
V key_329 = (V)"y";
R.Set(obj_326, key_327, values_325[0]);
R.Set(obj_328, key_329, values_325[1]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_300)));
var values_330 = R.Join(new V[] {}, new Results(V.Nil));
var obj_331 = v_302;
V key_332 = (V)"dormant";
R.Set(obj_331, key_332, values_330[0]);
var values_333 = R.Join(new V[] {}, new Results((V)"elite_active"));
var obj_334 = R.Get(v_300, (V)"stage");
V key_335 = (V)"phase";
R.Set(obj_334, key_335, values_333[0]);
var values_336 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_302, (V)"max_hp") / (V)2.0d)))).First + (V)1.0d)));
var obj_337 = v_302;
V key_338 = (V)"hp";
R.Set(obj_337, key_338, values_336[0]);
var values_339 = R.Join(new V[] {}, new Results((V)(R.Get(v_302, (V)"defense") + (V)1.0d)));
var obj_340 = v_307;
V key_341 = (V)"damage";
R.Set(obj_340, key_341, values_339[0]);
var values_342 = R.Join(new V[] {}, new Results(R.Get(v_302, (V)"speed")));
V v_343 = values_342[0];
R.Call(R.Get(v_15, (V)"enqueue"), R.Join(new V[] {v_300}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", R.Get(v_300, (V)"clock")), new Entry((V)"actor_id", R.Get(v_302, (V)"id")), new Entry((V)"is_player", (V)false)))));
var values_344 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_300}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_310, (V)1.0d)), new Entry((V)"dy", R.Get(v_310, (V)2.0d)))))));
V v_345 = values_344[0];
V v_346 = values_344[1];
V v_347 = values_344[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_345, () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_347}, new Results((V)"ElitePhaseChanged"))).First) == (V)1.0d)), () => R.Get(v_302, (V)"phase_two")), () => (V)(R.Get(v_302, (V)"speed") == (V)(v_343 + (V)25.0d))))));
if (R.Truth((V)(!R.Truth(R.Get(v_302, (V)"pending_attack"))))) {
{ double end_350 = R.Num((V)2.0d), step_351 = R.Num((V)1.0d);
for (double i_349 = R.Num((V)1.0d); step_351 > 0 ? i_349 <= end_350 : i_349 >= end_350; i_349 += step_351) {
V v_348 = i_349;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_300}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
if (R.Truth(R.Get(v_302, (V)"pending_attack"))) {
break;
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_302, (V)"pending_attack"), () => R.Invoke(R.Get(R.Get(v_302, (V)"pending_attack"), (V)"ability_id"), "find", R.Join(new V[] {(V)"core:ability/elite_", (V)1.0d}, new Results((V)true))).First))));
var values_352 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_300))))));
V v_353 = values_352[0];
{ double end_356 = R.Num((V)2.0d), step_357 = R.Num((V)1.0d);
for (double i_355 = R.Num((V)1.0d); step_357 > 0 ? i_355 <= end_356 : i_355 >= end_356; i_355 += step_357) {
V v_354 = i_355;
var values_358 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_300}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_359 = values_358[0];
V v_360 = values_358[1];
V v_361 = values_358[2];
var values_362 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_353}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_363 = values_362[0];
V v_364 = values_362[1];
V v_365 = values_362[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_359 == v_363))));

R.Call(v_3, R.Join(new V[] {v_361}, new Results(v_365)));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_300))).First}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_353)))));
}
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
