// Static C# port of tests/stage_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 3329d9b1e2430b2a8fa40d0e2429ec14053a064ebf15c333d270f0378ef0781a
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_stage_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_17 = values_16[0];
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
var values_22 = R.Join(new V[] {}, new Results(R.Table()));
V v_23 = values_22[0];
foreach (var row_24 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_20)))))) {
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
foreach (var row_33 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_32, (V)"entities"))))))) {
V v_34 = row_33[0];
V v_35 = row_33[1];
if (R.Truth((V)(R.Get(v_35, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_9, (V)"remove_for_actor"), R.Join(new V[] {v_32}, new Results(R.Get(v_35, (V)"id"))));
}
}
return Results.Empty;
}));
V v_36 = V.Nil;
v_36 = new V((RuleFunction)(args_37 => {
V v_38 = args_37[0];
V v_39 = args_37[1];
var values_40 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_38))));
V v_41 = values_40[0];
var values_42 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"map_for"), R.Join(new V[] {}, new Results(v_38))));
V v_43 = values_42[0];
foreach (var row_44 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_17)))))) {
V v_45 = row_44[0];
V v_46 = row_44[1];
var values_47 = R.Join(new V[] {(V)(R.Get(R.Get(v_39, (V)"position"), (V)"x") + R.Get(v_46, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_39, (V)"position"), (V)"y") + R.Get(v_46, (V)2.0d))));
V v_48 = values_47[0];
V v_49 = values_47[1];
var values_50 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_43, v_48}, new Results(v_49))));
V v_51 = values_50[0];
foreach (var row_52 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_38, (V)"entities"))))))) {
V v_53 = row_52[0];
V v_54 = row_52[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_54, (V)"id") != R.Get(v_41, (V)"id")), () => R.Get(v_54, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_54, (V)"position"), (V)"x") == v_48)), () => (V)(R.Get(R.Get(v_54, (V)"position"), (V)"y") == v_49)))) {
var values_55 = R.Join(new V[] {}, new Results((V)false));
v_51 = values_55[0];
}
}
if (R.Truth(v_51)) {
var values_56 = R.Join(new V[] {v_48}, new Results(v_49));
var obj_57 = R.Get(v_41, (V)"position");
V key_58 = (V)"x";
var obj_59 = R.Get(v_41, (V)"position");
V key_60 = (V)"y";
R.Set(obj_57, key_58, values_56[0]);
R.Set(obj_59, key_60, values_56[1]);
R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_38)));
return R.Join(new V[] {(V)(R.Get(R.Get(v_39, (V)"position"), (V)"x") - v_48)}, new Results((V)(R.Get(R.Get(v_39, (V)"position"), (V)"y") - v_49)));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no approach to stage object")));
return Results.Empty;
}));
V v_61 = V.Nil;
v_61 = new V((RuleFunction)(args_62 => {
V v_63 = args_62[0];
R.Call(v_30, R.Join(new V[] {}, new Results(v_63)));
var values_64 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_63}, new Results(R.Get(R.Get(v_63, (V)"stage"), (V)"elite_id")))))));
V v_65 = values_64[0];
var values_66 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_63))));
V v_67 = values_66[0];
var values_68 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_67, (V)"damage")}, new Results((V)(R.Get(v_65, (V)"hp") + R.Get(v_65, (V)"defense"))))));
var obj_69 = v_67;
V key_70 = (V)"damage";
R.Set(obj_69, key_70, values_68[0]);
var values_71 = R.Join(new V[] {}, new Results(R.Get(v_67, (V)"max_hp")));
var obj_72 = v_67;
V key_73 = (V)"hp";
R.Set(obj_72, key_73, values_71[0]);
var values_74 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_63}, new Results(v_65))));
V v_75 = values_74[0];
V v_76 = values_74[1];
var values_77 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_63}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_75), new Entry((V)"dy", v_76))))));
V v_78 = values_77[0];
V v_79 = values_77[1];
V v_80 = values_77[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_78}, new Results(v_79)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_80}, new Results((V)"EliteDefeated"))).First) == (V)1.0d))));
return R.Join(new V[] {}, new Results(v_80));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"stage elite exists once and never overlaps placements across 100 seeds"}, new Results(new V((RuleFunction)(args_81 => {

{ double end_84 = R.Num((V)100.0d), step_85 = R.Num((V)1.0d);
for (double i_83 = R.Num((V)1.0d); step_85 > 0 ? i_83 <= end_84 : i_83 >= end_84; i_83 += step_85) {
V v_82 = i_83;
var values_86 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results(v_82))));
V v_87 = values_86[0];
var values_88 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_87}, new Results(R.Get(R.Get(v_87, (V)"stage"), (V)"elite_id")))))));
V v_89 = values_88[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_89, (V)"definition_id") == (V)"core:enemy/thorn_warden"), () => R.Get(v_89, (V)"dormant")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_89, (V)"position"), (V)"x") == R.Get(R.Get(R.Get(v_87, (V)"generated_map"), (V)"elite"), (V)"x")), () => (V)(R.Get(R.Get(v_89, (V)"position"), (V)"y") == R.Get(R.Get(R.Get(v_87, (V)"generated_map"), (V)"elite"), (V)"y"))))));
foreach (var row_90 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_87, (V)"schedule"))))))) {
V v_91 = row_90[0];
V v_92 = row_90[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_92, (V)"actor_id") != R.Get(v_89, (V)"id")))));
}
var values_93 = R.Join(new V[] {(V)0.0d}, new Results(R.Table()));
V v_94 = values_93[0];
V v_95 = values_93[1];
foreach (var row_96 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_87, (V)"entities"))))))) {
V v_97 = row_96[0];
V v_98 = row_96[1];
if (R.Truth((V)(R.Get(v_98, (V)"definition_id") == R.Get(v_89, (V)"definition_id")))) {
var values_99 = R.Join(new V[] {}, new Results((V)(v_94 + (V)1.0d)));
v_94 = values_99[0];
}
if (R.Truth(R.Get(v_98, (V)"blocks_movement"))) {
var values_100 = R.Join(new V[] {}, new Results(R.Concat(R.Get(R.Get(v_98, (V)"position"), (V)"x"), R.Concat((V)",", R.Get(R.Get(v_98, (V)"position"), (V)"y")))));
V v_101 = values_100[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_95, v_101))))));

var values_102 = R.Join(new V[] {}, new Results((V)true));
var obj_103 = v_95;
V key_104 = v_101;
R.Set(obj_103, key_104, values_102[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_94 == (V)1.0d), () => (V)(R.Get(R.Get(v_87, (V)"stage"), (V)"phase") == (V)"exploring")))));
R.Call(v_61, R.Join(new V[] {}, new Results(v_87)));
var values_105 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_87}, new Results(R.Get(R.Get(v_87, (V)"stage"), (V)"elite_reward_id")))));
V v_106 = values_105[0];
var values_107 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_87}, new Results(R.Get(R.Get(v_87, (V)"stage"), (V)"exit_id")))));
V v_108 = values_107[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_106, () => v_108), () => (V)(R.Get(v_106, (V)"price") == (V)0.0d)))));
V v_109 = V.Nil;
v_109 = new V((RuleFunction)(args_110 => {
V v_111 = args_110[0];
foreach (var row_112 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_17)))))) {
V v_113 = row_112[0];
V v_114 = row_112[1];
var values_115 = R.Join(new V[] {(V)(R.Get(R.Get(v_111, (V)"position"), (V)"x") + R.Get(v_114, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_111, (V)"position"), (V)"y") + R.Get(v_114, (V)2.0d))));
V v_116 = values_115[0];
V v_117 = values_115[1];
var values_118 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {R.Get(v_87, (V)"generated_map"), v_116}, new Results(v_117))));
V v_119 = values_118[0];
foreach (var row_120 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_87, (V)"entities"))))))) {
V v_121 = row_120[0];
V v_122 = row_120[1];
if (R.Truth(R.And(R.And(R.Get(v_122, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_122, (V)"position"), (V)"x") == v_116)), () => (V)(R.Get(R.Get(v_122, (V)"position"), (V)"y") == v_117)))) {
var values_123 = R.Join(new V[] {}, new Results((V)false));
v_119 = values_123[0];
}
}
if (R.Truth(v_119)) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return Results.Empty;
}));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(v_109, R.Join(new V[] {}, new Results(v_106))).First, () => R.Call(v_109, R.Join(new V[] {}, new Results(v_108))).First))));
var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_87))));
V v_125 = values_124[0];
var values_126 = R.Join(new V[] {(V)1000.0d, (V)1000.0d}, new Results((V)100.0d));
var obj_127 = v_125;
V key_128 = (V)"hp";
var obj_129 = v_125;
V key_130 = (V)"max_hp";
var obj_131 = v_125;
V key_132 = (V)"defense";
R.Set(obj_127, key_128, values_126[0]);
R.Set(obj_129, key_130, values_126[1]);
R.Set(obj_131, key_132, values_126[2]);
while (R.Truth((V)(R.Get(v_87, (V)"clock") < R.Get(R.Get(v_87, (V)"stage"), (V)"deadline")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_87}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_87, (V)"stage"), (V)"phase") == (V)"pursuit"), () => R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_87}, new Results(R.Get(R.Get(v_87, (V)"stage"), (V)"pursuer_id")))).First))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Warden has different locked sweep and root line decisions"}, new Results(new V((RuleFunction)(args_133 => {

var values_134 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_135 = values_134[0];
var values_136 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_135}, new Results(R.Get(R.Get(v_135, (V)"stage"), (V)"elite_id")))));
V v_137 = values_136[0];
var values_138 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_135}, new Results(v_137))));
V v_139 = values_138[0];
V v_140 = values_138[1];
var values_141 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_142 = R.Get(v_137, (V)"ai_memory");
V key_143 = (V)"attacks";
R.Set(obj_142, key_143, values_141[0]);
var values_144 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"choose"), R.Join(new V[] {v_135}, new Results(v_137))));
V v_145 = values_144[0];
var values_146 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_147 = R.Get(v_137, (V)"ai_memory");
V key_148 = (V)"attacks";
R.Set(obj_147, key_148, values_146[0]);
var values_149 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"choose"), R.Join(new V[] {v_135}, new Results(v_137))));
V v_150 = values_149[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_145, (V)"kind") == (V)"windup"), () => (V)(R.Get(v_145, (V)"ability_id") == (V)"core:ability/warden_sweep")), () => (V)((V)R.Length(R.Get(v_145, (V)"cells")) == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_150, (V)"kind") == (V)"windup"), () => (V)(R.Get(v_150, (V)"ability_id") == (V)"core:ability/warden_roots")), () => (V)((V)R.Length(R.Get(v_150, (V)"cells")) >= (V)1.0d)), () => R.Get(v_150, (V)"ranged")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(v_139 != (V)0.0d), () => (V)(v_140 != (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"both Warden patterns commit distinct scheduler telegraphs"}, new Results(new V((RuleFunction)(args_151 => {

var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)12.0d))));
V v_153 = values_152[0];
R.Call(v_30, R.Join(new V[] {}, new Results(v_153)));
var values_154 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_153}, new Results(R.Get(R.Get(v_153, (V)"stage"), (V)"elite_id")))));
V v_155 = values_154[0];
R.Call(v_36, R.Join(new V[] {v_153}, new Results(v_155)));
var values_156 = R.Join(new V[] {}, new Results((V)"elite_active"));
var obj_157 = R.Get(v_153, (V)"stage");
V key_158 = (V)"phase";
R.Set(obj_157, key_158, values_156[0]);
var values_159 = R.Join(new V[] {}, new Results(V.Nil));
var obj_160 = v_155;
V key_161 = (V)"dormant";
R.Set(obj_160, key_161, values_159[0]);
V v_162 = V.Nil;
v_162 = new V((RuleFunction)(args_163 => {
V v_164 = args_163[0];
var values_165 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_153))))));
V v_166 = values_165[0];
var values_167 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_166}, new Results(R.Get(R.Get(v_166, (V)"stage"), (V)"elite_id")))));
V v_168 = values_167[0];
var values_169 = R.Join(new V[] {}, new Results(v_164));
var obj_170 = R.Get(v_168, (V)"ai_memory");
V key_171 = (V)"attacks";
R.Set(obj_170, key_171, values_169[0]);
R.Call(R.Get(v_9, (V)"enqueue"), R.Join(new V[] {v_166}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", R.Get(v_166, (V)"clock")), new Entry((V)"actor_id", R.Get(v_168, (V)"id")), new Entry((V)"is_player", (V)false)))));
var values_172 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_166}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_173 = values_172[0];
V v_174 = values_172[1];
V v_175 = values_172[2];
var values_176 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_175}, new Results((V)"WindupStarted"))));
V v_177 = values_176[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_177) >= (V)1.0d), () => (V)(R.Get(R.Get(v_177, (V)1.0d), (V)"source_id") == R.Get(v_168, (V)"id"))))));
return R.Join(new V[] {}, new Results(R.Get(v_177, (V)1.0d)));
return Results.Empty;
}));
var values_178 = R.Join(new V[] {R.Call(v_162, R.Join(new V[] {}, new Results((V)0.0d))).First}, R.Call(v_162, R.Join(new V[] {}, new Results((V)1.0d))));
V v_179 = values_178[0];
V v_180 = values_178[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_179, (V)"ability_id") == (V)"core:ability/warden_sweep"), () => (V)(R.Get(v_180, (V)"ability_id") == (V)"core:ability/warden_roots")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_179, (V)"target_cells")) == (V)3.0d), () => (V)((V)R.Length(R.Get(v_180, (V)"target_cells")) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_179, (V)"due") > R.Get(v_179, (V)"time")), () => (V)(R.Get(v_180, (V)"due") > R.Get(v_180, (V)"time"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"elite activation and phase change use committed simulation only"}, new Results(new V((RuleFunction)(args_181 => {

var values_182 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)2.0d))));
V v_183 = values_182[0];
R.Call(v_30, R.Join(new V[] {}, new Results(v_183)));
var values_184 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_183}, new Results(R.Get(R.Get(v_183, (V)"stage"), (V)"elite_id")))));
V v_185 = values_184[0];
R.Call(v_36, R.Join(new V[] {v_183}, new Results(v_185)));
var values_186 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_183))));
V v_187 = values_186[0];
R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_183)));

R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_183)));
R.Call(v_3, R.Join(new V[] {v_187}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_183)))));
var values_188 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_183}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_189 = values_188[0];
V v_190 = values_188[1];
V v_191 = values_188[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_189}, new Results(v_190)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_183, (V)"stage"), (V)"phase") == (V)"elite_active"), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_191}, new Results((V)"EliteEngaged"))).First) == (V)1.0d)))));
R.Call(v_30, R.Join(new V[] {}, new Results(v_183)));
var values_192 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_185, (V)"max_hp") / (V)2.0d)))).First + (V)1.0d)));
var obj_193 = v_185;
V key_194 = (V)"hp";
R.Set(obj_193, key_194, values_192[0]);
var values_195 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_183))));
V v_196 = values_195[0];
var values_197 = R.Join(new V[] {}, new Results((V)((V)2.0d + R.Get(v_185, (V)"defense"))));
var obj_198 = v_196;
V key_199 = (V)"damage";
R.Set(obj_198, key_199, values_197[0]);
var values_200 = R.Join(new V[] {(V)(R.Get(R.Get(v_185, (V)"position"), (V)"x") - R.Get(R.Get(v_196, (V)"position"), (V)"x"))}, new Results((V)(R.Get(R.Get(v_185, (V)"position"), (V)"y") - R.Get(R.Get(v_196, (V)"position"), (V)"y"))));
V v_201 = values_200[0];
V v_202 = values_200[1];
var values_203 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_183}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_201), new Entry((V)"dy", v_202))))));
V v_204 = values_203[0];
V v_205 = values_203[1];
V v_206 = values_203[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_185, (V)"phase_two"), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_206}, new Results((V)"ElitePhaseChanged"))).First) == (V)1.0d)))));
var values_207 = R.Join(new V[] {}, new Results(R.Get(v_185, (V)"speed")));
V v_208 = values_207[0];
var values_209 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_183}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_201), new Entry((V)"dy", v_202))))));
V v_210 = values_209[0];
V v_211 = values_209[1];
V v_212 = values_209[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_212}, new Results((V)"ElitePhaseChanged"))).First) == (V)0.0d), () => (V)(R.Get(v_185, (V)"speed") == v_208)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Warden windup survives damage but stun, displacement and death cancel it"}, new Results(new V((RuleFunction)(args_213 => {

var values_214 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)11.0d))));
V v_215 = values_214[0];
R.Call(v_30, R.Join(new V[] {}, new Results(v_215)));
var values_216 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_215}, new Results(R.Get(R.Get(v_215, (V)"stage"), (V)"elite_id")))));
V v_217 = values_216[0];
var values_218 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_215))));
V v_219 = values_218[0];
var values_220 = R.Join(new V[] {}, new Results((V)200.0d));
var obj_221 = v_219;
V key_222 = (V)"speed";
R.Set(obj_221, key_222, values_220[0]);

var values_223 = R.Join(new V[] {}, new Results(R.Get(v_219, (V)"max_hp")));
var obj_224 = v_219;
V key_225 = (V)"hp";
R.Set(obj_224, key_225, values_223[0]);
R.Call(v_36, R.Join(new V[] {v_215}, new Results(v_217)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_215}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_217, (V)"pending_attack"), () => (V)(R.Get(R.Get(v_217, (V)"pending_attack"), (V)"due") > R.Get(v_215, (V)"clock"))))));
var values_226 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_217, (V)"pending_attack"), (V)"target_cells")))));
V v_227 = values_226[0];
var values_228 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_215))));
V v_229 = values_228[0];
var values_230 = R.Join(new V[] {(V)(R.Get(R.Get(v_217, (V)"position"), (V)"x") - R.Get(R.Get(v_219, (V)"position"), (V)"x"))}, new Results((V)(R.Get(R.Get(v_217, (V)"position"), (V)"y") - R.Get(R.Get(v_219, (V)"position"), (V)"y"))));
V v_231 = values_230[0];
V v_232 = values_230[1];
var values_233 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_215}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_231), new Entry((V)"dy", v_232))))));
V v_234 = values_233[0];
V v_235 = values_233[1];
V v_236 = values_233[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_236}, new Results((V)"WindupCancelled"))).First) == (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_236}, new Results((V)"AttackPerformed"))).First) >= (V)2.0d))));
R.Call(v_3, R.Join(new V[] {v_227}, new Results(R.Get(R.Get(R.Call(v_18, R.Join(new V[] {v_236}, new Results((V)"AttackPerformed"))).First, (V)2.0d), (V)"target_cells"))));
var values_237 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, new Results(v_229))));
V v_238 = values_237[0];
var values_239 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_238, R.Get(v_217, (V)"id"), (V)200.0d, R.Get(v_219, (V)"id")}, new Results((V)10.0d))));
V v_240 = values_239[0];
V v_241 = values_239[1];
V v_242 = values_239[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_240}, new Results(v_241)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_242}, new Results((V)"WindupCancelled"))).First) == (V)1.0d))));
var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_238}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_244 = values_243[0];
V v_245 = values_243[1];
V v_246 = values_243[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_246}, new Results((V)"AttackPerformed"))).First) == (V)0.0d))));
var values_247 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, new Results(v_229))));
V v_248 = values_247[0];
var values_249 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_248}, new Results(R.Get(v_217, (V)"id")))));
V v_250 = values_249[0];
var values_251 = R.Join(new V[] {}, new Results((V)false));
V v_252 = values_251[0];
foreach (var row_253 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_17)))))) {
V v_254 = row_253[0];
V v_255 = row_253[1];
var values_256 = R.Join(new V[] {(V)(R.Get(R.Get(v_250, (V)"position"), (V)"x") + R.Get(v_255, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_250, (V)"position"), (V)"y") + R.Get(v_255, (V)2.0d))));
V v_257 = values_256[0];
V v_258 = values_256[1];
var values_259 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {R.Get(v_248, (V)"generated_map"), v_257}, new Results(v_258))));
V v_260 = values_259[0];
foreach (var row_261 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_248, (V)"entities"))))))) {
V v_262 = row_261[0];
V v_263 = row_261[1];
if (R.Truth(R.And(R.And(R.Get(v_263, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_263, (V)"position"), (V)"x") == v_257)), () => (V)(R.Get(R.Get(v_263, (V)"position"), (V)"y") == v_258)))) {
var values_264 = R.Join(new V[] {}, new Results((V)false));
v_260 = values_264[0];
}
}
if (R.Truth(v_260)) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"displace"), R.Join(new V[] {v_248, R.Get(v_250, (V)"id"), v_257, v_258, R.Get(v_219, (V)"id")}, new Results((V)11.0d)))));
var values_265 = R.Join(new V[] {}, new Results((V)true));
v_252 = values_265[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_252, () => (V)(!R.Truth(R.Get(v_250, (V)"pending_attack")))))));
var values_266 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, new Results(v_229))));
V v_267 = values_266[0];
var values_268 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_267}, new Results(R.Get(v_217, (V)"id")))));
V v_269 = values_268[0];
var values_270 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_271 = v_269;
V key_272 = (V)"hp";
R.Set(obj_271, key_272, values_270[0]);
var values_273 = R.Join(new V[] {}, new Results((V)20.0d));
var obj_274 = R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_267))).First;
V key_275 = (V)"damage";
R.Set(obj_274, key_275, values_273[0]);
var values_276 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_267}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_231), new Entry((V)"dy", v_232))))));
V v_277 = values_276[0];
V v_278 = values_276[1];
V v_279 = values_276[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_279}, new Results((V)"WindupCancelled"))).First) == (V)1.0d), () => (V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_267}, new Results(R.Get(v_217, (V)"id")))).First))), () => (V)(R.Get(R.Get(v_267, (V)"stage"), (V)"phase") == (V)"extraction")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"elite death creates exactly one free cached chest, exit and deadline"}, new Results(new V((RuleFunction)(args_280 => {

var values_281 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_282 = values_281[0];
var values_283 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_282))));
V v_284 = values_283[0];
var values_285 = R.Join(new V[] {}, new Results(R.Get(v_282, (V)"stage")));
V v_286 = values_285[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_286, (V)"phase") == (V)"extraction"), () => (V)(R.Get(v_286, (V)"defeat_time") == (V)0.0d)), () => (V)(R.Get(v_286, (V)"deadline") == R.Get(R.Call(R.Get(v_7, (V)"get_stage_rules"), Results.Empty).First, (V)"grace"))))));
var values_287 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_282}, new Results(R.Get(v_286, (V)"elite_reward_id")))));
V v_288 = values_287[0];
var values_289 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_282}, new Results(R.Get(v_286, (V)"exit_id")))));
V v_290 = values_289[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_288, (V)"price") == (V)0.0d), () => (V)((V)R.Length(R.Get(v_288, (V)"offers")) == (V)3.0d)), () => (V)(R.Get(v_290, (V)"controller") == (V)"exit")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(R.Get(v_288, (V)"position"), (V)"x") != R.Get(R.Get(v_290, (V)"position"), (V)"x")), () => (V)(R.Get(R.Get(v_288, (V)"position"), (V)"y") != R.Get(R.Get(v_290, (V)"position"), (V)"y"))))));
var values_291 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_288, (V)"offers")))));
V v_292 = values_291[0];
R.Call(v_36, R.Join(new V[] {v_282}, new Results(v_288)));
var values_293 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"chest_options"), R.Join(new V[] {v_282}, new Results(R.Get(v_288, (V)"id")))));
V v_294 = values_293[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_294, () => (V)(R.Get(v_294, (V)"price") == (V)0.0d)), () => (V)((V)R.Length(R.Get(v_294, (V)"offers")) == (V)3.0d)))));
R.Call(v_3, R.Join(new V[] {v_292}, new Results(R.Get(R.Call(R.Get(v_5, (V)"chest_options"), R.Join(new V[] {v_282}, new Results(R.Get(v_288, (V)"id")))).First, (V)"offers"))));
var values_295 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_282}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_288, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
V v_296 = values_295[0];
V v_297 = values_295[1];
V v_298 = values_295[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_296}, new Results(v_297)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_288, (V)"claimed"), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_298}, new Results((V)"BoonGranted"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_284}, new Results((V)"EntityKilled"))).First) == (V)1.0d))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_282))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_282)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"enemy friendly fire can finish Warden without player kill credit"}, new Results(new V((RuleFunction)(args_299 => {

var values_300 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)4.0d))));
V v_301 = values_300[0];
R.Call(v_30, R.Join(new V[] {}, new Results(v_301)));
var values_302 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_301}, new Results(R.Get(R.Get(v_301, (V)"stage"), (V)"elite_id")))));
V v_303 = values_302[0];
var values_304 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_305 = v_303;
V key_306 = (V)"hp";
R.Set(obj_305, key_306, values_304[0]);
var values_307 = Results.Empty;
V v_308 = values_307[0];
V v_309 = values_307[1];
foreach (var row_310 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_17)))))) {
V v_311 = row_310[0];
V v_312 = row_310[1];
var values_313 = R.Join(new V[] {(V)(R.Get(R.Get(v_303, (V)"position"), (V)"x") + R.Get(v_312, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_303, (V)"position"), (V)"y") + R.Get(v_312, (V)2.0d))));
V v_314 = values_313[0];
V v_315 = values_313[1];
if (R.Truth(R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {R.Get(v_301, (V)"generated_map"), v_314}, new Results(v_315))).First)) {
var values_316 = R.Join(new V[] {}, new Results((V)true));
V v_317 = values_316[0];
foreach (var row_318 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_301, (V)"entities"))))))) {
V v_319 = row_318[0];
V v_320 = row_318[1];
if (R.Truth(R.And(R.And(R.Get(v_320, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_320, (V)"position"), (V)"x") == v_314)), () => (V)(R.Get(R.Get(v_320, (V)"position"), (V)"y") == v_315)))) {
var values_321 = R.Join(new V[] {}, new Results((V)false));
v_317 = values_321[0];
}
}
if (R.Truth(v_317)) {
var values_322 = R.Join(new V[] {v_314}, new Results(v_315));
v_308 = values_322[0];
v_309 = values_322[1];

break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_308)));
var values_323 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_301, (V)"core:enemy/mossbound_guard", v_308}, new Results(v_309))));
V v_324 = values_323[0];
R.Call(R.Get(v_9, (V)"remove_for_actor"), R.Join(new V[] {v_301}, new Results(R.Get(v_324, (V)"id"))));
var values_325 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_326 = v_324;
V key_327 = (V)"damage";
R.Set(obj_326, key_327, values_325[0]);
var values_328 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"enqueue"), R.Join(new V[] {v_301}, new Results(R.Table(new Entry((V)"kind", (V)"impact"), new Entry((V)"due", (V)50.0d), new Entry((V)"actor_id", R.Get(v_324, (V)"id")), new Entry((V)"attack_id", (V)999.0d))))));
V v_329 = values_328[0];
var values_330 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schedule_id", R.Get(v_329, (V)"id")), new Entry((V)"sequence", R.Get(v_329, (V)"seq")), new Entry((V)"action_id", (V)999.0d), new Entry((V)"source_id", R.Get(v_324, (V)"id")), new Entry((V)"floor_id", R.Get(v_301, (V)"active_floor_id")), new Entry((V)"source_x", v_308), new Entry((V)"source_y", v_309), new Entry((V)"ability_id", (V)"core:ability/guard_strike"), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(v_301, (V)"active_floor_id")), new Entry((V)"x", R.Get(R.Get(v_303, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_303, (V)"position"), (V)"y")))))), new Entry((V)"ranged", (V)false), new Entry((V)"damage", (V)5.0d), new Entry((V)"due", (V)50.0d), new Entry((V)"committed_time", (V)0.0d))));
var obj_331 = v_324;
V key_332 = (V)"pending_attack";
R.Set(obj_331, key_332, values_330[0]);
var values_333 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_301, (V)"progression"), (V)"xp_total")));
V v_334 = values_333[0];
var values_335 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_301}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_336 = values_335[0];
V v_337 = values_335[1];
V v_338 = values_335[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_338}, new Results((V)"EliteDefeated"))).First) == (V)1.0d), () => (V)(R.Get(R.Get(v_301, (V)"stage"), (V)"phase") == (V)"extraction")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_301, (V)"progression"), (V)"xp_total") == v_334), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_338}, new Results((V)"ExperienceGained"))).First) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_338}, new Results((V)"CurrencyDropped"))).First) >= (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a credited boon explosion can defeat Warden through the event queue"}, new Results(new V((RuleFunction)(args_339 => {

var values_340 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)13.0d))));
V v_341 = values_340[0];
R.Call(v_30, R.Join(new V[] {}, new Results(v_341)));
var values_342 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_341}, new Results(R.Get(R.Get(v_341, (V)"stage"), (V)"elite_id")))));
V v_343 = values_342[0];
var values_344 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_345 = v_343;
V key_346 = (V)"hp";
R.Set(obj_345, key_346, values_344[0]);
var values_347 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_341))));
V v_348 = values_347[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_341, R.Get(v_348, (V)"id"), (V)"core:boon/detonation_bloom", (V)"legendary"}, new Results((V)1.0d)));
var values_349 = Results.Empty;
V v_350 = values_349[0];
V v_351 = values_349[1];
V v_352 = values_349[2];
foreach (var row_353 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_17)))))) {
V v_354 = row_353[0];
V v_355 = row_353[1];
var values_356 = R.Join(new V[] {(V)(R.Get(R.Get(v_343, (V)"position"), (V)"x") + R.Get(v_355, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_343, (V)"position"), (V)"y") + R.Get(v_355, (V)2.0d))));
V v_357 = values_356[0];
V v_358 = values_356[1];
var values_359 = R.Join(new V[] {(V)(v_357 + R.Get(v_355, (V)1.0d))}, new Results((V)(v_358 + R.Get(v_355, (V)2.0d))));
V v_360 = values_359[0];
V v_361 = values_359[1];
if (R.Truth(R.And(R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {R.Get(v_341, (V)"generated_map"), v_357}, new Results(v_358))).First, () => R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {R.Get(v_341, (V)"generated_map"), v_360}, new Results(v_361))).First))) {
var values_362 = R.Join(new V[] {}, new Results((V)true));
V v_363 = values_362[0];
foreach (var row_364 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_341, (V)"entities"))))))) {
V v_365 = row_364[0];
V v_366 = row_364[1];
if (R.Truth(R.And(R.Get(v_366, (V)"blocks_movement"), () => R.Or(R.And((V)(R.Get(R.Get(v_366, (V)"position"), (V)"x") == v_357), () => (V)(R.Get(R.Get(v_366, (V)"position"), (V)"y") == v_358)), () => R.And((V)(R.Get(R.Get(v_366, (V)"position"), (V)"x") == v_360), () => (V)(R.Get(R.Get(v_366, (V)"position"), (V)"y") == v_361)))))) {
var values_367 = R.Join(new V[] {}, new Results((V)false));
v_363 = values_367[0];
}
}
if (R.Truth(v_363)) {
var values_368 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_341, (V)"core:enemy/mossbound_guard", v_357}, new Results(v_358))));
v_350 = values_368[0];
R.Call(R.Get(v_9, (V)"remove_for_actor"), R.Join(new V[] {v_341}, new Results(R.Get(v_350, (V)"id"))));
var values_369 = R.Join(new V[] {v_360}, new Results(v_361));
var obj_370 = R.Get(v_348, (V)"position");
V key_371 = (V)"x";
var obj_372 = R.Get(v_348, (V)"position");
V key_373 = (V)"y";
R.Set(obj_370, key_371, values_369[0]);
R.Set(obj_372, key_373, values_369[1]);
var values_374 = R.Join(new V[] {(-R.Get(v_355, (V)1.0d))}, new Results((-R.Get(v_355, (V)2.0d))));
v_351 = values_374[0];
v_352 = values_374[1];
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_350)));
var values_375 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_376 = v_350;
V key_377 = (V)"hp";
R.Set(obj_376, key_377, values_375[0]);
var values_378 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_341, (V)"progression"), (V)"xp_total")));
V v_379 = values_378[0];
var values_380 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_341}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_351), new Entry((V)"dy", v_352))))));
V v_381 = values_380[0];
V v_382 = values_380[1];
V v_383 = values_380[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_381}, new Results(v_382)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_383}, new Results((V)"BoonActivated"))).First) >= (V)1.0d), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_383}, new Results((V)"EliteDefeated"))).First) == (V)1.0d)), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_383}, new Results((V)"EntityKilled"))).First) == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_341, (V)"stage"), (V)"phase") == (V)"extraction"), () => (V)(R.Get(R.Get(v_341, (V)"progression"), (V)"xp_total") > v_379)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a saved delayed echo can finish Warden at its committed timestamp"}, new Results(new V((RuleFunction)(args_384 => {

var values_385 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)14.0d))));
V v_386 = values_385[0];
R.Call(v_30, R.Join(new V[] {}, new Results(v_386)));
var values_387 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_386}, new Results(R.Get(R.Get(v_386, (V)"stage"), (V)"elite_id")))));
V v_388 = values_387[0];
var values_389 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_390 = v_388;
V key_391 = (V)"hp";
R.Set(obj_390, key_391, values_389[0]);
var values_392 = R.Join(new V[] {}, new Results(V.Nil));
var obj_393 = v_388;
V key_394 = (V)"dormant";
R.Set(obj_393, key_394, values_392[0]);
var values_395 = R.Join(new V[] {}, new Results((V)"elite_active"));
var obj_396 = R.Get(v_386, (V)"stage");
V key_397 = (V)"phase";
R.Set(obj_396, key_397, values_395[0]);
var values_398 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_386))));
V v_399 = values_398[0];
var values_400 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_401 = v_399;
V key_402 = (V)"damage";
R.Set(obj_401, key_402, values_400[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_386, R.Get(v_399, (V)"id"), (V)"core:boon/temporal_echo", (V)"legendary"}, new Results((V)1.0d)));
var values_403 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_386}, new Results(v_388))));
V v_404 = values_403[0];
V v_405 = values_403[1];
var values_406 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_386}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_404), new Entry((V)"dy", v_405))))));
V v_407 = values_406[0];
V v_408 = values_406[1];
V v_409 = values_406[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_388, (V)"hp") == (V)1.0d), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_409}, new Results((V)"DelayedEffectScheduled"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_386, (V)"stage"), (V)"phase") == (V)"elite_active"), () => (V)(R.Get(v_386, (V)"clock") < (V)200.0d)))));
var values_410 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_386))))));
V v_411 = values_410[0];
var values_412 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_386}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_413 = values_412[0];
V v_414 = values_412[1];
V v_415 = values_412[2];
var values_416 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_411}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_417 = values_416[0];
V v_418 = values_416[1];
V v_419 = values_416[2];
R.Call(v_3, R.Join(new V[] {v_415}, new Results(v_419)));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_386))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_411)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_415}, new Results((V)"DelayedEffectResolved"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_415}, new Results((V)"EliteDefeated"))).First) == (V)1.0d)), () => (V)(R.Get(R.Get(v_386, (V)"stage"), (V)"defeat_time") == (V)200.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pressure is time-driven, bounded and uses independent RNG"}, new Results(new V((RuleFunction)(args_420 => {

var values_421 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)5.0d))));
V v_422 = values_421[0];
R.Call(v_30, R.Join(new V[] {}, new Results(v_422)));
var values_423 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_422, (V)"rng"), (V)"state")));
V v_424 = values_423[0];
var values_425 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_422, (V)"stage"), (V)"pressure_rng"), (V)"state")));
V v_426 = values_425[0];
var values_427 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_422))));
V v_428 = values_427[0];
{ double end_431 = R.Num((V)4.0d), step_432 = R.Num((V)1.0d);
for (double i_430 = R.Num((V)1.0d); step_432 > 0 ? i_430 <= end_431 : i_430 >= end_431; i_430 += step_432) {
V v_429 = i_430;
R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_422)));

R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_422)));
}
}
R.Call(v_3, R.Join(new V[] {v_428}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_422)))));
var values_433 = R.Join(new V[] {}, new Results(R.Table()));
V v_434 = values_433[0];
{ double end_437 = R.Num((V)(R.Get(R.Call(R.Get(v_7, (V)"get_stage_rules"), Results.Empty).First, (V)"pressure_first") / (V)100.0d)), step_438 = R.Num((V)1.0d);
for (double i_436 = R.Num((V)1.0d); step_438 > 0 ? i_436 <= end_437 : i_436 >= end_437; i_436 += step_438) {
V v_435 = i_436;
var values_439 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_422}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_440 = values_439[0];
V v_441 = values_439[1];
V v_442 = values_439[2];
foreach (var row_443 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_442}, new Results((V)"PressureSpawned")))))))) {
V v_444 = row_443[0];
V v_445 = row_443[1];
var values_446 = R.Join(new V[] {}, new Results(v_445));
var obj_447 = v_434;
V key_448 = (V)((V)R.Length(v_434) + (V)1.0d);
R.Set(obj_447, key_448, values_446[0]);
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_422, (V)"clock") == R.Get(R.Call(R.Get(v_7, (V)"get_stage_rules"), Results.Empty).First, (V)"pressure_first")), () => (V)((V)R.Length(v_434) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_422, (V)"rng"), (V)"state") == v_424), () => (V)(R.Get(R.Get(R.Get(v_422, (V)"stage"), (V)"pressure_rng"), (V)"state") != v_426)))));
var values_449 = R.Join(new V[] {}, new Results(R.Get(v_434, (V)1.0d)));
V v_450 = values_449[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"is_visible"), R.Join(new V[] {v_422, R.Get(v_422, (V)"active_floor_id"), R.Get(v_450, (V)"x")}, new Results(R.Get(v_450, (V)"y")))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_450, (V)"x") - R.Get(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_422))).First, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_450, (V)"y") - R.Get(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_422))).First, (V)"position"), (V)"y"))))).First) >= R.Get(R.Call(R.Get(v_7, (V)"get_stage_rules"), Results.Empty).First, (V)"pressure_safe_radius")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pressure reports a legal no-spawn outcome without hanging"}, new Results(new V((RuleFunction)(args_451 => {

var values_452 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)6.0d))));
V v_453 = values_452[0];
R.Call(v_30, R.Join(new V[] {}, new Results(v_453)));
{ double end_456 = R.Num((V)((V)(R.Get(R.Call(R.Get(v_7, (V)"get_stage_rules"), Results.Empty).First, (V)"pressure_first") / (V)100.0d) - (V)1.0d)), step_457 = R.Num((V)1.0d);
for (double i_455 = R.Num((V)1.0d); step_457 > 0 ? i_455 <= end_456 : i_455 >= end_456; i_455 += step_457) {
V v_454 = i_455;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_453}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}
var values_458 = R.Join(new V[] {}, new Results(R.Get(v_453, (V)"generated_map")));
V v_459 = values_458[0];
{ double end_462 = R.Num(R.Get(v_459, (V)"height")), step_463 = R.Num((V)1.0d);
for (double i_461 = R.Num((V)1.0d); step_463 > 0 ? i_461 <= end_462 : i_461 >= end_462; i_461 += step_463) {
V v_460 = i_461;
{ double end_466 = R.Num(R.Get(v_459, (V)"width")), step_467 = R.Num((V)1.0d);
for (double i_465 = R.Num((V)1.0d); step_467 > 0 ? i_465 <= end_466 : i_465 >= end_466; i_465 += step_467) {
V v_464 = i_465;
var values_468 = R.Join(new V[] {}, new Results((V)true));
var obj_469 = R.Get(R.Get(v_453, (V)"visible"), (V)"cells");
V key_470 = (V)((V)((V)(v_460 - (V)1.0d) * R.Get(v_459, (V)"width")) + v_464);
R.Set(obj_469, key_470, values_468[0]);
}
}
}
}
var values_471 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_453}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_472 = values_471[0];
V v_473 = values_471[1];
V v_474 = values_471[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_474}, new Results((V)"PressureSpawned"))).First) == (V)0.0d), () => (V)(R.Get(R.Get(v_453, (V)"stage"), (V)"pressure_count") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Get(v_453, (V)"stage"), (V)"diagnostics"), (V)R.Length(R.Get(R.Get(v_453, (V)"stage"), (V)"diagnostics"))), (V)"kind") == (V)"no_legal_anchor"))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stage pressure and enemy turns replay identically from seed and snapshot"}, new Results(new V((RuleFunction)(args_475 => {

var values_476 = R.Join(new V[] {R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)25.0d))).First}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)25.0d))));
V v_477 = values_476[0];
V v_478 = values_476[1];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_477))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_478)))));
{ double end_481 = R.Num((V)(R.Get(R.Call(R.Get(v_7, (V)"get_stage_rules"), Results.Empty).First, (V)"pressure_first") / (V)100.0d)), step_482 = R.Num((V)1.0d);
for (double i_480 = R.Num((V)1.0d); step_482 > 0 ? i_480 <= end_481 : i_480 >= end_481; i_480 += step_482) {
V v_479 = i_480;
var values_483 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_477}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_484 = values_483[0];
V v_485 = values_483[1];
V v_486 = values_483[2];
var values_487 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_478}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_488 = values_487[0];
V v_489 = values_487[1];
V v_490 = values_487[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_484, () => v_488)}, new Results(R.Or(v_485, () => v_489))));
R.Call(v_3, R.Join(new V[] {v_486}, new Results(v_490)));
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_477))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_478)))));
var values_491 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_477))))));
V v_492 = values_491[0];
{ double end_495 = R.Num((V)3.0d), step_496 = R.Num((V)1.0d);
for (double i_494 = R.Num((V)1.0d); step_496 > 0 ? i_494 <= end_495 : i_494 >= end_495; i_494 += step_496) {
V v_493 = i_494;
var values_497 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_477}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_498 = values_497[0];
V v_499 = values_497[1];
V v_500 = values_497[2];
var values_501 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_492}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_502 = values_501[0];
V v_503 = values_501[1];
V v_504 = values_501[2];
R.Call(v_3, R.Join(new V[] {v_500}, new Results(v_504)));
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_477))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_492)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"deadline impact precedes ready player and spawns one immune pursuer"}, new Results(new V((RuleFunction)(args_505 => {

var values_506 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)7.0d))));
V v_507 = values_506[0];
R.Call(v_61, R.Join(new V[] {}, new Results(v_507)));
R.Call(v_30, R.Join(new V[] {}, new Results(v_507)));
var values_508 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_507, (V)"stage"), (V)"deadline")));
V v_509 = values_508[0];
var values_510 = Results.Empty;
V v_511 = values_510[0];
while (R.Truth((V)(R.Get(v_507, (V)"clock") < (V)(v_509 - (V)100.0d)))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_507}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
var values_512 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_507))));
v_511 = values_512[0];
var values_513 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, new Results(v_511))));
V v_514 = values_513[0];
var values_515 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_507}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_516 = values_515[0];
V v_517 = values_515[1];
V v_518 = values_515[2];
var values_519 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_514}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_520 = values_519[0];
V v_521 = values_519[1];
V v_522 = values_519[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_516, () => v_520)}, new Results(R.Or(v_517, () => v_521))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_507))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_514)))));
R.Call(v_3, R.Join(new V[] {v_518}, new Results(v_522)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_507, (V)"clock") == v_509), () => (V)(R.Get(R.Get(v_507, (V)"stage"), (V)"phase") == (V)"pursuit")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_518}, new Results((V)"PursuerArrived"))).First) == (V)1.0d))));
var values_523 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_507}, new Results(R.Get(R.Get(v_507, (V)"stage"), (V)"pursuer_id")))));
V v_524 = values_523[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_524, () => R.Get(v_524, (V)"invulnerable")))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_507))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_507)))))))));
var values_525 = R.Join(new V[] {}, new Results(R.Get(v_507, (V)"currency")));
V v_526 = values_525[0];
var values_527 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_507}, new Results(v_524))));
V v_528 = values_527[0];
V v_529 = values_527[1];
var values_530 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_507}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_528), new Entry((V)"dy", v_529))))));
V v_531 = values_530[0];
V v_532 = values_530[1];
V v_533 = values_530[2];
var values_534 = R.Join(new V[] {(V)0.0d, (V)0.0d}, new Results((V)0.0d));
V v_535 = values_534[0];
V v_536 = values_534[1];
V v_537 = values_534[2];
foreach (var row_538 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_533)))))) {
V v_539 = row_538[0];
V v_540 = row_538[1];
if (R.Truth((V)(R.Get(v_540, (V)"target_id") == R.Get(v_524, (V)"id")))) {
if (R.Truth((V)(R.Get(v_540, (V)"kind") == (V)"DamageBlocked"))) {
var values_541 = R.Join(new V[] {}, new Results((V)(v_535 + (V)1.0d)));
v_535 = values_541[0];
}
if (R.Truth((V)(R.Get(v_540, (V)"kind") == (V)"DamageTaken"))) {
var values_542 = R.Join(new V[] {}, new Results((V)(v_536 + (V)1.0d)));
v_536 = values_542[0];
}
if (R.Truth((V)(R.Get(v_540, (V)"kind") == (V)"HitConfirmed"))) {
var values_543 = R.Join(new V[] {}, new Results((V)(v_537 + (V)1.0d)));
v_537 = values_543[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_535 >= (V)1.0d), () => (V)(v_536 == (V)0.0d)), () => (V)(v_537 == (V)0.0d)))));
var values_544 = R.Join(new V[] {}, new Results((V)false));
V v_545 = values_544[0];
foreach (var row_546 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_533)))))) {
V v_547 = row_546[0];
V v_548 = row_546[1];
if (R.Truth(R.And((V)(R.Get(v_548, (V)"source_id") == R.Get(v_524, (V)"id")), () => R.Or(R.Or((V)(R.Get(v_548, (V)"kind") == (V)"Moved"), () => (V)(R.Get(v_548, (V)"kind") == (V)"WindupStarted")), () => (V)(R.Get(v_548, (V)"kind") == (V)"AttackPerformed"))))) {
var values_549 = R.Join(new V[] {}, new Results((V)true));
v_545 = values_549[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_545)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_524, (V)"hp") == R.Get(v_524, (V)"max_hp")), () => (V)(R.Get(v_507, (V)"currency") == v_526)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_507, R.Get(v_524, (V)"id")}, new Results((V)100.0d))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_5, (V)"displace"), R.Join(new V[] {v_507, R.Get(v_524, (V)"id"), (V)(R.Get(R.Get(v_524, (V)"position"), (V)"x") + (V)1.0d)}, new Results(R.Get(R.Get(v_524, (V)"position"), (V)"y")))).First))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"route menu is free, stable and transitions one time preserving build"}, new Results(new V((RuleFunction)(args_550 => {

var values_551 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)8.0d))));
V v_552 = values_551[0];
R.Call(v_61, R.Join(new V[] {}, new Results(v_552)));
R.Call(v_30, R.Join(new V[] {}, new Results(v_552)));
var values_553 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_552}, new Results(R.Get(R.Get(v_552, (V)"stage"), (V)"exit_id")))));
V v_554 = values_553[0];
var values_555 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_552}, new Results(v_554))));
V v_556 = values_555[0];
V v_557 = values_555[1];
var values_558 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_552))));
V v_559 = values_558[0];
var values_560 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_552}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_556), new Entry((V)"dy", v_557))))));
V v_561 = values_560[0];
V v_562 = values_560[1];
V v_563 = values_560[2];
V v_564 = values_560[3];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(v_561)), () => (V)(v_562 == (V)"exit")), () => (V)(R.Get(v_564, (V)"exit_id") == R.Get(v_554, (V)"id"))))));
var values_565 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"route_options"), R.Join(new V[] {v_552}, new Results(R.Get(v_554, (V)"id")))));
V v_566 = values_565[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_566, (V)"routes")) == (V)3.0d))));
R.Call(v_3, R.Join(new V[] {v_559}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_552)))));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_552, R.Get(v_552, (V)"player_id"), (V)"core:boon/crescent_reach", (V)"rare"}, new Results((V)2.0d)));
var values_567 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_552))));
V v_568 = values_567[0];
var values_569 = R.Join(new V[] {}, new Results((V)17.0d));
var obj_570 = v_568;
V key_571 = (V)"hp";
R.Set(obj_570, key_571, values_569[0]);

var values_572 = R.Join(new V[] {}, new Results((V)31.0d));
var obj_573 = v_568;
V key_574 = (V)"max_hp";
R.Set(obj_573, key_574, values_572[0]);

var values_575 = R.Join(new V[] {}, new Results((V)13.0d));
var obj_576 = v_552;
V key_577 = (V)"currency";
R.Set(obj_576, key_577, values_575[0]);
var values_578 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_579 = R.Get(v_552, (V)"progression");
V key_580 = (V)"level";
R.Set(obj_579, key_580, values_578[0]);

var values_581 = R.Join(new V[] {}, new Results((V)7.0d));
var obj_582 = R.Get(v_552, (V)"progression");
V key_583 = (V)"xp_total";
R.Set(obj_582, key_583, values_581[0]);
var values_584 = R.Join(new V[] {}, new Results(R.Get(v_552, (V)"next_entity_id")));
V v_585 = values_584[0];
var values_586 = R.Join(new V[] {}, new Results(R.Get(v_552, (V)"root_seed")));
V v_587 = values_586[0];
var values_588 = R.Join(new V[] {}, new Results(R.Get(v_552, (V)"next_schedule_id")));
V v_589 = values_588[0];
var values_590 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"enqueue"), R.Join(new V[] {v_552}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", (V)(R.Get(v_552, (V)"clock") + (V)500.0d)), new Entry((V)"effect_id", v_589))))));
V v_591 = values_590[0];
var values_592 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"activation", R.Table(new Entry((V)"ancestry", R.Table()), new Entry((V)"owner_id", R.Get(v_552, (V)"player_id")), new Entry((V)"root_action_id", (V)300.0d), new Entry((V)"chain_id", (V)300.0d), new Entry((V)"event", R.Table(new Entry((V)"target_cells", R.Table()))))), new Entry((V)"effect", R.Table()))));
var obj_593 = R.Get(v_552, (V)"delayed_effects");
V key_594 = R.Get(v_591, (V)"id");
R.Set(obj_593, key_594, values_592[0]);
var values_595 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_552))))));
V v_596 = values_595[0];
var values_597 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_552}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_554, (V)"id")), new Entry((V)"route_index", (V)2.0d))))));
V v_598 = values_597[0];
V v_599 = values_597[1];
V v_600 = values_597[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_598}, new Results(v_599)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_600}, new Results((V)"StageEntered"))).First) == (V)1.0d), () => (V)(R.Get(R.Get(v_552, (V)"stage"), (V)"index") == (V)2.0d)), () => (V)(R.Get(v_552, (V)"clock") == (V)0.0d)), () => (V)((V)R.Length(R.Get(v_552, (V)"retired_stages")) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_552, (V)"run_seed") == R.Call(R.Get(v_13, (V)"derive"), R.Join(new V[] {v_587}, new Results((V)"stage:2:core:route/sunken_aqueduct"))).First), () => (V)(R.Get(R.Get(v_552, (V)"stage"), (V)"biome_id") == (V)"core:biome/sunken_aqueduct")), () => (V)(R.Get(R.Get(v_552, (V)"generated_map"), (V)"strategy") == (V)"canals")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_596}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_554, (V)"id")), new Entry((V)"route_index", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_596, (V)"run_seed") != R.Get(v_552, (V)"run_seed")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_552, (V)"player_id") >= v_585), () => (V)(R.Get(R.Get(R.Get(v_552, (V)"stage"), (V)"route_history"), (V)1.0d) == (V)"core:route/sunken_aqueduct")))));
var values_601 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_552))));
v_568 = values_601[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_568, (V)"hp") == (V)17.0d), () => (V)(R.Get(v_568, (V)"max_hp") == (V)31.0d)), () => (V)(R.Get(v_552, (V)"currency") == (V)13.0d)), () => (V)(R.Get(R.Get(v_552, (V)"progression"), (V)"level") == (V)2.0d)), () => (V)(R.Get(R.Get(v_552, (V)"progression"), (V)"xp_total") == (V)7.0d)), () => (V)(R.Get(R.Get(R.Get(v_568, (V)"boons"), (V)"core:boon/crescent_reach"), (V)"rare") == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Get(v_552, (V)"retired_stages"), (V)1.0d), (V)"stage"), (V)"phase") == (V)"transition"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(R.Get(R.Get(v_552, (V)"retired_stages"), (V)1.0d), (V)"delayed_effects"), R.Get(v_591, (V)"id")), () => (V)(!R.Truth(R.Get(R.Get(v_552, (V)"delayed_effects"), R.Get(v_591, (V)"id"))))))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_552))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_552)))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_552}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_554, (V)"id")), new Entry((V)"route_index", (V)2.0d))))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"three biome stages hand off to a living final boss, not victory"}, new Results(new V((RuleFunction)(args_602 => {

var values_603 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)9.0d))));
V v_604 = values_603[0];
{ double end_607 = R.Num((V)3.0d), step_608 = R.Num((V)1.0d);
for (double i_606 = R.Num((V)1.0d); step_608 > 0 ? i_606 <= end_607 : i_606 >= end_607; i_606 += step_608) {
V v_605 = i_606;
R.Call(v_61, R.Join(new V[] {}, new Results(v_604)));
R.Call(v_30, R.Join(new V[] {}, new Results(v_604)));
var values_609 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_604}, new Results(R.Get(R.Get(v_604, (V)"stage"), (V)"exit_id")))));
V v_610 = values_609[0];
R.Call(v_36, R.Join(new V[] {v_604}, new Results(v_610)));
var values_611 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_604}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_610, (V)"id")), new Entry((V)"route_index", (V)1.0d))))));
V v_612 = values_611[0];
V v_613 = values_611[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_612}, new Results(v_613)));
if (R.Truth((V)(v_605 < (V)3.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_604, (V)"stage"), (V)"index") == (V)(v_605 + (V)1.0d)), () => (V)(R.Get(R.Get(v_604, (V)"stage"), (V)"phase") == (V)"exploring")))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And(R.Get(v_604, (V)"finale"), () => (V)(R.Get(R.Get(v_604, (V)"finale"), (V)"phase") == (V)"active")), () => (V)(!R.Truth(R.Get(v_604, (V)"game_over")))), () => (V)(!R.Truth(R.Get(v_604, (V)"victory")))), () => R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_604}, new Results(R.Get(R.Get(v_604, (V)"finale"), (V)"boss_id")))).First), () => (V)((V)R.Length(R.Get(v_604, (V)"retired_stages")) == (V)3.0d)))));
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_604, (V)"finale"), (V)"route_history")) == (V)3.0d))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_604))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_604)))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_604}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"continuous controlled elite fight, reward, pursuer, route and next-stage combat"}, new Results(new V((RuleFunction)(args_614 => {

var values_615 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_616 = values_615[0];
R.Call(v_30, R.Join(new V[] {}, new Results(v_616)));
var values_617 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_616}, new Results(R.Get(R.Get(v_616, (V)"stage"), (V)"elite_id")))));
V v_618 = values_617[0];
var values_619 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_616))));
V v_620 = values_619[0];
var values_621 = R.Join(new V[] {(V)500.0d, (V)500.0d}, new Results((V)15.0d));
var obj_622 = v_620;
V key_623 = (V)"max_hp";
var obj_624 = v_620;
V key_625 = (V)"hp";
var obj_626 = v_620;
V key_627 = (V)"damage";
R.Set(obj_622, key_623, values_621[0]);
R.Set(obj_624, key_625, values_621[1]);
R.Set(obj_626, key_627, values_621[2]);
R.Call(v_36, R.Join(new V[] {v_616}, new Results(v_618)));
var values_628 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_616}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_629 = values_628[0];
V v_630 = values_628[1];
V v_631 = values_628[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_616, (V)"stage"), (V)"phase") == (V)"elite_active"), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_631}, new Results((V)"WindupStarted"))).First) >= (V)1.0d)))));
var values_632 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_633 = values_632[0];
V v_634 = values_632[1];
{ double end_637 = R.Num((V)5.0d), step_638 = R.Num((V)1.0d);
for (double i_636 = R.Num((V)1.0d); step_638 > 0 ? i_636 <= end_637 : i_636 >= end_637; i_636 += step_638) {
V v_635 = i_636;
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_616}, new Results(R.Get(v_618, (V)"id")))).First)))) {
break;
}
var values_639 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_616}, new Results(v_618))));
V v_640 = values_639[0];
V v_641 = values_639[1];
var values_642 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_616}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_640), new Entry((V)"dy", v_641))))));
V v_643 = values_642[0];
V v_644 = values_642[1];
V v_645 = values_642[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_643}, new Results(v_644)));
var values_646 = R.Join(new V[] {}, new Results((V)(v_633 + (V)R.Length(R.Call(v_18, R.Join(new V[] {v_645}, new Results((V)"ElitePhaseChanged"))).First))));
v_633 = values_646[0];
var values_647 = R.Join(new V[] {}, new Results((V)(v_634 + (V)R.Length(R.Call(v_18, R.Join(new V[] {v_645}, new Results((V)"EliteDefeated"))).First))));
v_634 = values_647[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_633 == (V)1.0d), () => (V)(v_634 == (V)1.0d)), () => (V)(R.Get(R.Get(v_616, (V)"stage"), (V)"phase") == (V)"extraction")), () => (V)(R.Get(v_620, (V)"hp") < R.Get(v_620, (V)"max_hp"))))));
R.Call(v_30, R.Join(new V[] {}, new Results(v_616)));
var values_648 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_616}, new Results(R.Get(R.Get(v_616, (V)"stage"), (V)"elite_reward_id")))));
V v_649 = values_648[0];
R.Call(v_36, R.Join(new V[] {v_616}, new Results(v_649)));
var values_650 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(R.Get(v_5, (V)"chest_options"), R.Join(new V[] {v_616}, new Results(R.Get(v_649, (V)"id")))).First, (V)"offers"), (V)1.0d)));
V v_651 = values_650[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_616}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_649, (V)"id")), new Entry((V)"offer_index", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_649, (V)"claimed"))));
var values_652 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_616))).First, (V)"boons"), R.Get(v_651, (V)"boon_id")), R.Get(v_651, (V)"rarity"))));
V v_653 = values_652[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_653 == (V)1.0d))));
var values_654 = R.Join(new V[] {}, new Results((V)0.0d));
V v_655 = values_654[0];
while (R.Truth((V)(R.Get(v_616, (V)"clock") < R.Get(R.Get(v_616, (V)"stage"), (V)"deadline")))) {
var values_656 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_616}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_657 = values_656[0];
V v_658 = values_656[1];
V v_659 = values_656[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_657}, new Results(v_658)));
var values_660 = R.Join(new V[] {}, new Results((V)(v_655 + (V)R.Length(R.Call(v_18, R.Join(new V[] {v_659}, new Results((V)"PursuerArrived"))).First))));
v_655 = values_660[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_655 == (V)1.0d), () => (V)(R.Get(R.Get(v_616, (V)"stage"), (V)"phase") == (V)"pursuit")))));
var values_661 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_616}, new Results(R.Get(R.Get(v_616, (V)"stage"), (V)"exit_id")))));
V v_662 = values_661[0];
R.Call(v_36, R.Join(new V[] {v_616}, new Results(v_662)));
var values_663 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_616))));
V v_664 = values_663[0];
var values_665 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, new Results(v_664))));
V v_666 = values_665[0];
var values_667 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_616}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_662, (V)"id")), new Entry((V)"route_index", (V)1.0d))))));
V v_668 = values_667[0];
V v_669 = values_667[1];
V v_670 = values_667[2];
var values_671 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_666}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_662, (V)"id")), new Entry((V)"route_index", (V)1.0d))))));
V v_672 = values_671[0];
V v_673 = values_671[1];
V v_674 = values_671[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_668, () => v_672)}, new Results(R.Or(v_669, () => v_673))));
R.Call(v_3, R.Join(new V[] {v_670}, new Results(v_674)));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_616))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_666)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_616))).First, (V)"boons"), R.Get(v_651, (V)"boon_id")), R.Get(v_651, (V)"rarity")) == v_653))));
R.Call(v_30, R.Join(new V[] {}, new Results(v_616)));
var values_675 = Results.Empty;
V v_676 = values_675[0];
foreach (var row_677 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_616, (V)"entities"))))))) {
V v_678 = row_677[0];
V v_679 = row_677[1];
if (R.Truth(R.And((V)(R.Get(v_679, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_679, (V)"id") != R.Get(R.Get(v_616, (V)"stage"), (V)"elite_id"))))) {
var values_680 = R.Join(new V[] {}, new Results(v_679));
v_676 = values_680[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_676)));
var values_681 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_616}, new Results(v_676))));
V v_682 = values_681[0];
V v_683 = values_681[1];
var values_684 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_616}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_682), new Entry((V)"dy", v_683))))));
V v_685 = values_684[0];
V v_686 = values_684[1];
V v_687 = values_684[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_687}, new Results((V)"AttackPerformed"))).First) >= (V)1.0d), () => (V)((V)R.Length(R.Call(v_18, R.Join(new V[] {v_687}, new Results((V)"DamageTaken"))).First) >= (V)1.0d)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
