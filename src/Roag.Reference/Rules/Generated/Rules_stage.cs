// Static C# port of src/stage.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 0f70619078c34871ad1ca59caeb1693b2ec2c62ce06429b8b20407fbfd790878
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_stage { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, new Results(R.Table()));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_16 = values_15[0];
V v_17 = V.Nil;
v_17 = new V((RuleFunction)(args_18 => {
V v_19 = args_18[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_19))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_19));
}
var values_20 = R.Join(new V[] {}, new Results(R.Table()));
V v_21 = values_20[0];
foreach (var row_22 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_19)))))) {
V v_23 = row_22[0];
V v_24 = row_22[1];
var values_25 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_24))));
var obj_26 = v_21;
V key_27 = v_23;
R.Set(obj_26, key_27, values_25[0]);
}
return R.Join(new V[] {}, new Results(v_21));
return Results.Empty;
}));
V v_28 = V.Nil;
v_28 = new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
V v_32 = args_29[2];
foreach (var row_33 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_30, (V)"entities"))))))) {
V v_34 = row_33[0];
V v_35 = row_33[1];
var values_36 = R.Join(new V[] {}, new Results(R.Get(v_35, (V)"position")));
V v_37 = values_36[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.Get(v_35, (V)"blocks_movement"), () => v_37), () => (V)(R.Get(v_37, (V)"floor_id") == R.Get(v_30, (V)"active_floor_id"))), () => (V)(R.Get(v_37, (V)"x") == v_31)), () => (V)(R.Get(v_37, (V)"y") == v_32)))) {
return R.Join(new V[] {}, new Results(v_35));
}
}
return Results.Empty;
}));
V v_38 = V.Nil;
v_38 = new V((RuleFunction)(args_39 => {
V v_40 = args_39[0];
V v_41 = args_39[1];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_40, (V)"x") - R.Get(v_41, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_40, (V)"y") - R.Get(v_41, (V)"y"))))).First)));
return Results.Empty;
}));
V v_42 = V.Nil;
v_42 = new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
V v_46 = args_43[2];
V v_47 = args_43[3];
var values_48 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_44))));
V v_49 = values_48[0];
var values_50 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_44, (V)"entities"), (V)1.0d)));
V v_51 = values_50[0];
var values_52 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_51, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_51, (V)"position"), (V)"y")))))));
V v_53 = values_52[0];
var values_54 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)((V)(R.Get(R.Get(v_51, (V)"position"), (V)"y") * R.Get(v_49, (V)"width")) + R.Get(R.Get(v_51, (V)"position"), (V)"x")), (V)true))));
V v_55 = values_54[0];
var values_56 = R.Join(new V[] {}, new Results((V)1.0d));
V v_57 = values_56[0];
while (R.Truth((V)(v_57 <= (V)R.Length(v_53)))) {
var values_58 = R.Join(new V[] {}, new Results(R.Get(v_53, v_57)));
V v_59 = values_58[0];

var values_60 = R.Join(new V[] {}, new Results((V)(v_57 + (V)1.0d)));
v_57 = values_60[0];
foreach (var row_61 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_16)))))) {
V v_62 = row_61[0];
V v_63 = row_61[1];
var values_64 = R.Join(new V[] {(V)(R.Get(v_59, (V)"x") + R.Get(v_63, (V)1.0d))}, new Results((V)(R.Get(v_59, (V)"y") + R.Get(v_63, (V)2.0d))));
V v_65 = values_64[0];
V v_66 = values_64[1];
var values_67 = R.Join(new V[] {}, new Results((V)((V)(v_66 * R.Get(v_49, (V)"width")) + v_65)));
V v_68 = values_67[0];
var values_69 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {v_44, v_65}, new Results(v_66))));
V v_70 = values_69[0];
if (R.Truth(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_55, v_68))), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_49, v_65}, new Results(v_66))).First), () => (V)(!R.Truth(R.And((V)(v_65 == v_45), () => (V)(v_66 == v_46))))), () => R.Or(R.Or((V)(!R.Truth(v_70)), () => (V)(R.Get(v_70, (V)"controller") == (V)"ai")), () => R.And(v_47, () => (V)(R.Get(v_70, (V)"controller") == (V)"door")))))) {
var values_71 = R.Join(new V[] {}, new Results((V)true));
var obj_72 = v_55;
V key_73 = v_68;
R.Set(obj_72, key_73, values_71[0]);
var values_74 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_65), new Entry((V)"y", v_66))));
var obj_75 = v_53;
V key_76 = (V)((V)R.Length(v_53) + (V)1.0d);
R.Set(obj_75, key_76, values_74[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_55));
return Results.Empty;
}));
V v_77 = V.Nil;
v_77 = new V((RuleFunction)(args_78 => {
V v_79 = args_78[0];
V v_80 = args_78[1];
V v_81 = args_78[2];
V v_82 = args_78[3];
V v_83 = args_78[4];
var values_84 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_79))));
V v_85 = values_84[0];
var values_86 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_79, (V)"entities"), (V)1.0d)));
V v_87 = values_86[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(!R.Truth(R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_85, v_80}, new Results(v_81))).First)), () => R.Call(v_28, R.Join(new V[] {v_79, v_80}, new Results(v_81))).First), () => (V)(R.Call(v_38, R.Join(new V[] {R.Get(v_87, (V)"position")}, new Results(R.Table(new Entry((V)"x", v_80), new Entry((V)"y", v_81))))).First < v_82)), () => R.Call(R.Get(v_12, (V)"is_visible"), R.Join(new V[] {v_79, R.Get(v_79, (V)"active_floor_id"), v_80}, new Results(v_81))).First), () => (V)(!R.Truth(R.Get(v_83, (V)((V)(v_81 * R.Get(v_85, (V)"width")) + v_80))))))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth(R.And((V)(v_80 == R.Get(v_85, (V)"start_x")), () => (V)(v_81 == R.Get(v_85, (V)"start_y"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth(R.And((V)(v_80 == R.Get(R.Get(v_85, (V)"elite"), (V)"x")), () => (V)(v_81 == R.Get(R.Get(v_85, (V)"elite"), (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_88 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_79, (V)"secrets"))))))) {
V v_89 = row_88[0];
V v_90 = row_88[1];
if (R.Truth(R.Or(R.And((V)(R.Get(v_90, (V)"x") == v_80), () => (V)(R.Get(v_90, (V)"y") == v_81)), () => R.And((V)(R.Get(v_90, (V)"cache_x") == v_80), () => (V)(R.Get(v_90, (V)"cache_y") == v_81))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
V v_91 = V.Nil;
v_91 = new V((RuleFunction)(args_92 => {
V v_93 = args_92[0];
V v_94 = args_92[1];
V v_95 = args_92[2];
var values_96 = R.Join(new V[] {}, new Results(R.Table()));
V v_97 = values_96[0];
var values_98 = R.Join(new V[] {}, new Results(R.Table()));
V v_99 = values_98[0];
var values_100 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)6, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))), new Entry((V)7, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)8, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)9, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)10, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)2.0d)))), new Entry((V)11, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)0.0d))), new Entry((V)12, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)2.0d))), new Entry((V)13, R.Table(new Entry((V)1, (-(V)2.0d)), new Entry((V)2, (V)0.0d))))));
V v_101 = values_100[0];
foreach (var row_102 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_93, (V)"generated_map"), (V)"rooms"))))))) {
V v_103 = row_102[0];
V v_104 = row_102[1];
if (R.Truth(R.And((V)(R.Get(v_104, (V)"role") != (V)"start"), () => (V)(R.Get(v_104, (V)"role") != (V)"elite")))) {
foreach (var row_105 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_104, (V)"markers"), (V)"enemy"), () => R.Table()))))))) {
V v_106 = row_105[0];
V v_107 = row_105[1];
foreach (var row_108 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_101)))))) {
V v_109 = row_108[0];
V v_110 = row_108[1];
var values_111 = R.Join(new V[] {(V)(R.Get(v_107, (V)"x") + R.Get(v_110, (V)1.0d))}, new Results((V)(R.Get(v_107, (V)"y") + R.Get(v_110, (V)2.0d))));
V v_112 = values_111[0];
V v_113 = values_111[1];
var values_114 = R.Join(new V[] {}, new Results(R.Concat(v_112, R.Concat((V)",", v_113))));
V v_115 = values_114[0];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_99, v_115))), () => R.Call(v_77, R.Join(new V[] {v_93, v_112, v_113, v_94}, new Results(v_95))).First))) {
var values_116 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_112), new Entry((V)"y", v_113))));
var obj_117 = v_97;
V key_118 = (V)((V)R.Length(v_97) + (V)1.0d);
R.Set(obj_117, key_118, values_116[0]);
var values_119 = R.Join(new V[] {}, new Results((V)true));
var obj_120 = v_99;
V key_121 = v_115;
R.Set(obj_120, key_121, values_119[0]);
}
}
}
}
}
return R.Join(new V[] {}, new Results(v_97));
return Results.Empty;
}));
V v_122 = V.Nil;
v_122 = new V((RuleFunction)(args_123 => {
V v_124 = args_123[0];
V v_125 = args_123[1];
V v_126 = args_123[2];
V v_127 = args_123[3];
V v_128 = args_123[4];
var values_129 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_124))));
V v_130 = values_129[0];
var values_131 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", v_125), new Entry((V)"y", v_126))))));
V v_132 = values_131[0];
{ double end_135 = R.Num((V)5.0d), step_136 = R.Num((V)1.0d);
for (double i_134 = R.Num((V)1.0d); step_136 > 0 ? i_134 <= end_135 : i_134 >= end_135; i_134 += step_136) {
V v_133 = i_134;
{ double end_139 = R.Num(v_133), step_140 = R.Num((V)1.0d);
for (double i_138 = R.Num((-v_133)); step_140 > 0 ? i_138 <= end_139 : i_138 >= end_139; i_138 += step_140) {
V v_137 = i_138;
{ double end_143 = R.Num(v_133), step_144 = R.Num((V)1.0d);
for (double i_142 = R.Num((-v_133)); step_144 > 0 ? i_142 <= end_143 : i_142 >= end_143; i_142 += step_144) {
V v_141 = i_142;
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_141))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_137))).First) == v_133))) {
var values_145 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)(v_125 + v_141)), new Entry((V)"y", (V)(v_126 + v_137)))));
var obj_146 = v_132;
V key_147 = (V)((V)R.Length(v_132) + (V)1.0d);
R.Set(obj_146, key_147, values_145[0]);
}
}
}
}
}
}
}
foreach (var row_148 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_132)))))) {
V v_149 = row_148[0];
V v_150 = row_148[1];
var values_151 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_150, (V)"x"), R.Concat((V)",", R.Get(v_150, (V)"y")))));
V v_152 = values_151[0];
if (R.Truth(R.And(R.And(R.And(R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_130, R.Get(v_150, (V)"x")}, new Results(R.Get(v_150, (V)"y")))).First, () => (V)(!R.Truth(R.Call(v_28, R.Join(new V[] {v_124, R.Get(v_150, (V)"x")}, new Results(R.Get(v_150, (V)"y")))).First))), () => (V)(!R.Truth(R.Get(v_127, v_152)))), () => R.Or((V)(!R.Truth(v_128)), () => R.Call(v_128, R.Join(new V[] {}, new Results(v_150))).First)))) {
return R.Join(new V[] {}, new Results(v_150));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"elite arena has no free reward/exit cell")));
return Results.Empty;
}));
R.Set(v_14, (V)"init", new V((RuleFunction)(args_153 => {
V v_154 = args_153[0];
V v_155 = args_153[1];
if (R.Truth((V)(!R.Truth(R.Get(v_154, (V)"generated_map"))))) {
return Results.Empty;
}
var values_156 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_154, (V)"stage_options"), () => R.Table())));
V v_157 = values_156[0];
var values_158 = R.Join(new V[] {}, new Results(R.Get(v_154, (V)"generated_map")));
V v_159 = values_158[0];
var values_160 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_6, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_159, (V)"biome_id")))).First}, new Results((V)"unknown elite biome"))));
V v_161 = values_160[0];
var values_162 = R.Join(new V[] {}, R.Call(v_155, R.Join(new V[] {v_154, R.Get(v_161, (V)"elite_id"), R.Get(R.Get(v_159, (V)"elite"), (V)"x"), R.Get(R.Get(v_159, (V)"elite"), (V)"y")}, new Results((V)0.0d))));
V v_163 = values_162[0];
R.Call(R.Get(v_10, (V)"remove_for_actor"), R.Join(new V[] {v_154, R.Get(v_163, (V)"id")}, new Results((V)"actor_ready")));
var values_164 = R.Join(new V[] {}, new Results((V)true));
var obj_165 = v_163;
V key_166 = (V)"dormant";
R.Set(obj_165, key_166, values_164[0]);
var values_167 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_168 = R.Get(v_163, (V)"ai_memory");
V key_169 = (V)"attacks";
R.Set(obj_168, key_169, values_167[0]);
var values_170 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get_stage_rules"), Results.Empty));
V v_171 = values_170[0];
var values_172 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"index", R.Or(R.Get(v_157, (V)"index"), () => (V)1.0d)), new Entry((V)"biome_id", R.Or(R.Get(v_159, (V)"biome_id"), () => R.Get(R.Get(v_6, (V)"order"), (V)1.0d))), new Entry((V)"phase", (V)"exploring"), new Entry((V)"elite_id", R.Get(v_163, (V)"id")), new Entry((V)"elite_floor_id", R.Get(v_159, (V)"floor_id")), new Entry((V)"elite_definition_id", R.Get(v_161, (V)"elite_id")), new Entry((V)"pressure_rng", R.Call(R.Get(v_8, (V)"new"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"derive"), R.Join(new V[] {R.Get(v_154, (V)"root_seed")}, new Results(R.Concat((V)"pressure:", R.Concat(R.Or(R.Get(v_157, (V)"index"), () => (V)1.0d), R.Concat((V)":", R.Get(v_154, (V)"run_seed"))))))))).First), new Entry((V)"pressure_count", (V)0.0d), new Entry((V)"next_pressure", R.Get(v_171, (V)"pressure_first")), new Entry((V)"route_history", R.Or(R.Get(v_157, (V)"route_history"), () => R.Table())), new Entry((V)"diagnostics", R.Table()), new Entry((V)"elite_reward_id", V.Nil), new Entry((V)"exit_id", V.Nil), new Entry((V)"defeat_time", V.Nil), new Entry((V)"deadline", V.Nil), new Entry((V)"pursuer_id", V.Nil))));
var obj_173 = v_154;
V key_174 = (V)"stage";
R.Set(obj_173, key_174, values_172[0]);
R.Call(R.Get(v_10, (V)"enqueue"), R.Join(new V[] {v_154}, new Results(R.Table(new Entry((V)"kind", (V)"stage_pressure"), new Entry((V)"due", R.Get(v_171, (V)"pressure_first"))))));
var values_175 = R.Join(new V[] {}, new Results(V.Nil));
var obj_176 = v_154;
V key_177 = (V)"stage_options";
R.Set(obj_176, key_177, values_175[0]);
return Results.Empty;
})));
R.Set(v_14, (V)"after_player_action", new V((RuleFunction)(args_178 => {
V v_179 = args_178[0];
V v_180 = args_178[1];
var values_181 = R.Join(new V[] {}, new Results(R.Get(v_179, (V)"stage")));
V v_182 = values_181[0];
if (R.Truth(R.Or((V)(!R.Truth(v_182)), () => (V)(R.Get(v_182, (V)"phase") != (V)"exploring")))) {
return Results.Empty;
}
var values_183 = Results.Empty;
V v_184 = values_183[0];
foreach (var row_185 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_179, (V)"entities"))))))) {
V v_186 = row_185[0];
V v_187 = row_185[1];
if (R.Truth((V)(R.Get(v_187, (V)"id") == R.Get(v_182, (V)"elite_id")))) {
var values_188 = R.Join(new V[] {}, new Results(v_187));
v_184 = values_188[0];

break;
}
}
if (R.Truth(R.Or((V)(!R.Truth(v_184)), () => (V)(!R.Truth(R.Get(v_184, (V)"dormant")))))) {
return Results.Empty;
}
var values_189 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_179, (V)"entities"), (V)1.0d)));
V v_190 = values_189[0];
if (R.Truth(R.And((V)(R.Get(R.Get(v_190, (V)"position"), (V)"floor_id") == R.Get(R.Get(v_184, (V)"position"), (V)"floor_id")), () => (V)(R.Call(v_38, R.Join(new V[] {R.Get(v_190, (V)"position")}, new Results(R.Get(v_184, (V)"position")))).First <= R.Get(R.Call(R.Get(v_2, (V)"get_stage_rules"), Results.Empty).First, (V)"elite_activation_radius"))))) {
var values_191 = R.Join(new V[] {}, new Results(V.Nil));
var obj_192 = v_184;
V key_193 = (V)"dormant";
R.Set(obj_192, key_193, values_191[0]);
var values_194 = R.Join(new V[] {}, new Results((V)"elite_active"));
var obj_195 = v_182;
V key_196 = (V)"phase";
R.Set(obj_195, key_196, values_194[0]);
R.Call(R.Get(v_10, (V)"enqueue"), R.Join(new V[] {v_179}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", R.Get(v_179, (V)"clock")), new Entry((V)"actor_id", R.Get(v_184, (V)"id")), new Entry((V)"is_player", (V)false)))));
R.Call(v_180, R.Join(new V[] {(V)"EliteEngaged", R.Table(new Entry((V)"elite_id", R.Get(v_184, (V)"id")), new Entry((V)"floor_id", R.Get(v_179, (V)"active_floor_id")), new Entry((V)"x", R.Get(R.Get(v_184, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_184, (V)"position"), (V)"y")))}, new Results(R.Get(v_184, (V)"id"))));
}
return Results.Empty;
})));
R.Set(v_14, (V)"on_event", new V((RuleFunction)(args_197 => {
V v_198 = args_197[0];
V v_199 = args_197[1];
V v_200 = args_197[2];
V v_201 = args_197[3];
var values_202 = R.Join(new V[] {}, new Results(R.Get(v_198, (V)"stage")));
V v_203 = values_202[0];
if (R.Truth((V)(!R.Truth(v_203)))) {
return Results.Empty;
}
if (R.Truth(R.And(R.And((V)(R.Get(v_199, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_199, (V)"target_id") == R.Get(v_203, (V)"elite_id"))), () => (V)(R.Get(v_203, (V)"phase") == (V)"elite_active")))) {
var values_204 = Results.Empty;
V v_205 = values_204[0];
foreach (var row_206 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_198, (V)"entities"))))))) {
V v_207 = row_206[0];
V v_208 = row_206[1];
if (R.Truth((V)(R.Get(v_208, (V)"id") == R.Get(v_203, (V)"elite_id")))) {
var values_209 = R.Join(new V[] {}, new Results(v_208));
v_205 = values_209[0];

break;
}
}
if (R.Truth(R.And(R.And(R.And(v_205, () => (V)(!R.Truth(R.Get(v_205, (V)"phase_two")))), () => (V)(R.Get(v_205, (V)"hp") > (V)0.0d)), () => (V)((V)(R.Get(v_205, (V)"hp") * (V)100.0d) <= (V)(R.Get(v_205, (V)"max_hp") * R.Get(R.Call(R.Get(v_2, (V)"get_stage_rules"), Results.Empty).First, (V)"elite_phase_percent")))))) {
var values_210 = R.Join(new V[] {}, new Results((V)true));
var obj_211 = v_205;
V key_212 = (V)"phase_two";
R.Set(obj_211, key_212, values_210[0]);
var values_213 = R.Join(new V[] {}, new Results((V)(R.Get(v_205, (V)"speed") + (V)25.0d)));
var obj_214 = v_205;
V key_215 = (V)"speed";
R.Set(obj_214, key_215, values_213[0]);
R.Call(v_200, R.Join(new V[] {(V)"ElitePhaseChanged", R.Table(new Entry((V)"elite_id", R.Get(v_205, (V)"id")), new Entry((V)"phase", (V)2.0d), new Entry((V)"floor_id", R.Get(v_198, (V)"active_floor_id")), new Entry((V)"x", R.Get(R.Get(v_205, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_205, (V)"position"), (V)"y")))}, new Results(R.Get(v_205, (V)"id"))));
}
} else if (R.Truth(R.And(R.And((V)(R.Get(v_199, (V)"kind") == (V)"EntityKilled"), () => (V)(R.Get(v_199, (V)"target_id") == R.Get(v_203, (V)"elite_id"))), () => R.Or((V)(R.Get(v_203, (V)"phase") == (V)"exploring"), () => (V)(R.Get(v_203, (V)"phase") == (V)"elite_active"))))) {
var values_216 = R.Join(new V[] {}, new Results((V)"extraction"));
var obj_217 = v_203;
V key_218 = (V)"phase";
R.Set(obj_217, key_218, values_216[0]);
var values_219 = R.Join(new V[] {}, new Results(R.Get(v_199, (V)"time")));
var obj_220 = v_203;
V key_221 = (V)"defeat_time";
R.Set(obj_220, key_221, values_219[0]);
var values_222 = R.Join(new V[] {}, new Results((V)(R.Get(v_199, (V)"time") + R.Get(R.Call(R.Get(v_2, (V)"get_stage_rules"), Results.Empty).First, (V)"grace"))));
var obj_223 = v_203;
V key_224 = (V)"deadline";
R.Set(obj_223, key_224, values_222[0]);
var values_225 = R.Join(new V[] {}, new Results(R.Table()));
V v_226 = values_225[0];
var values_227 = R.Join(new V[] {}, R.Call(v_122, R.Join(new V[] {v_198, R.Get(v_199, (V)"x"), R.Get(v_199, (V)"y")}, new Results(v_226))));
V v_228 = values_227[0];
var values_229 = R.Join(new V[] {}, new Results((V)true));
var obj_230 = v_226;
V key_231 = R.Concat(R.Get(v_228, (V)"x"), R.Concat((V)",", R.Get(v_228, (V)"y")));
R.Set(obj_230, key_231, values_229[0]);
var values_232 = R.Join(new V[] {}, R.Call(v_201, R.Join(new V[] {(V)"core:chest/free_elite", R.Get(v_228, (V)"x"), R.Get(v_228, (V)"y")}, new Results(R.Concat((V)"elite:stage:", R.Get(v_203, (V)"index"))))));
V v_233 = values_232[0];
var values_234 = R.Join(new V[] {}, new Results(R.Get(v_233, (V)"id")));
var obj_235 = v_203;
V key_236 = (V)"elite_reward_id";
R.Set(obj_235, key_236, values_234[0]);
var values_237 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_198))));
V v_238 = values_237[0];
var values_249 = R.Join(new V[] {}, R.Call(v_122, R.Join(new V[] {v_198, R.Get(v_199, (V)"x"), R.Get(v_199, (V)"y"), v_226}, new Results(new V((RuleFunction)(args_239 => {
V v_240 = args_239[0];
var values_241 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_198, R.Get(v_240, (V)"x"), R.Get(v_240, (V)"y")}, new Results((V)true))));
V v_242 = values_241[0];
foreach (var row_243 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_16)))))) {
V v_244 = row_243[0];
V v_245 = row_243[1];
var values_246 = R.Join(new V[] {(V)(R.Get(v_228, (V)"x") + R.Get(v_245, (V)1.0d))}, new Results((V)(R.Get(v_228, (V)"y") + R.Get(v_245, (V)2.0d))));
V v_247 = values_246[0];
V v_248 = values_246[1];
if (R.Truth(R.Get(v_242, (V)((V)(v_248 * R.Get(v_238, (V)"width")) + v_247)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}))))));
V v_250 = values_249[0];
var values_251 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_198, (V)"next_entity_id")), new Entry((V)"definition_id", (V)"core:exit/arena"), new Entry((V)"controller", (V)"exit"), new Entry((V)"faction", (V)"neutral"), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_198, (V)"active_floor_id")), new Entry((V)"x", R.Get(v_250, (V)"x")), new Entry((V)"y", R.Get(v_250, (V)"y")))), new Entry((V)"blocks_movement", (V)true), new Entry((V)"routes", R.Call(v_17, R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"routes_for"), R.Join(new V[] {R.Get(v_203, (V)"index")}, new Results(R.Get(v_203, (V)"route_history")))))).First))));
V v_252 = values_251[0];
var values_253 = R.Join(new V[] {}, new Results((V)(R.Get(v_252, (V)"id") + (V)1.0d)));
var obj_254 = v_198;
V key_255 = (V)"next_entity_id";
R.Set(obj_254, key_255, values_253[0]);
var values_256 = R.Join(new V[] {}, new Results(v_252));
var obj_257 = R.Get(v_198, (V)"entities");
V key_258 = (V)((V)R.Length(R.Get(v_198, (V)"entities")) + (V)1.0d);
R.Set(obj_257, key_258, values_256[0]);
var values_259 = R.Join(new V[] {}, new Results(R.Get(v_252, (V)"id")));
var obj_260 = v_203;
V key_261 = (V)"exit_id";
R.Set(obj_260, key_261, values_259[0]);
R.Call(R.Get(v_10, (V)"enqueue"), R.Join(new V[] {v_198}, new Results(R.Table(new Entry((V)"kind", (V)"stage_deadline"), new Entry((V)"due", R.Get(v_203, (V)"deadline"))))));
R.Call(v_200, R.Join(new V[] {(V)"EliteDefeated", R.Table(new Entry((V)"elite_id", R.Get(v_203, (V)"elite_id")), new Entry((V)"free_chest_id", R.Get(v_233, (V)"id")), new Entry((V)"exit_id", R.Get(v_252, (V)"id")), new Entry((V)"deadline", R.Get(v_203, (V)"deadline")), new Entry((V)"floor_id", R.Get(v_198, (V)"active_floor_id")), new Entry((V)"x", R.Get(v_199, (V)"x")), new Entry((V)"y", R.Get(v_199, (V)"y")))}, new Results(R.Get(v_199, (V)"source_id"))));
}
return Results.Empty;
})));
V v_262 = V.Nil;
v_262 = new V((RuleFunction)(args_263 => {
V v_264 = args_263[0];
V v_265 = args_263[1];
V v_266 = args_263[2];
var values_267 = R.Join(new V[] {}, new Results(R.Get(v_264, (V)"stage")));
V v_268 = values_267[0];
if (R.Truth(R.And((V)(R.Get(v_268, (V)"phase") != (V)"exploring"), () => (V)(R.Get(v_268, (V)"phase") != (V)"elite_active")))) {
return Results.Empty;
}
var values_269 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get_stage_rules"), Results.Empty));
V v_270 = values_269[0];
var values_271 = R.Join(new V[] {}, new Results((V)0.0d));
V v_272 = values_271[0];
foreach (var row_273 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_264, (V)"entities"))))))) {
V v_274 = row_273[0];
V v_275 = row_273[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_275, (V)"controller") == (V)"ai"), () => (V)(R.Get(R.Get(v_275, (V)"position"), (V)"floor_id") == R.Get(v_264, (V)"active_floor_id"))), () => (V)(R.Get(v_275, (V)"id") != R.Get(v_268, (V)"elite_id"))), () => (V)(R.Get(v_275, (V)"id") != R.Get(v_268, (V)"pursuer_id"))))) {
var values_276 = R.Join(new V[] {}, new Results((V)(v_272 + (V)1.0d)));
v_272 = values_276[0];
}
}
var values_277 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results(v_264))));
V v_278 = values_277[0];
var values_279 = R.Join(new V[] {}, R.Call(v_91, R.Join(new V[] {v_264, R.Get(v_270, (V)"pressure_safe_radius")}, new Results(v_278))));
V v_280 = values_279[0];
if (R.Truth(R.And((V)(v_272 < R.Get(v_270, (V)"pressure_max_active")), () => (V)((V)R.Length(v_280) > (V)0.0d)))) {
var values_281 = R.Join(new V[] {}, new Results(R.Get(v_280, R.Call(R.Get(v_8, (V)"next_int"), R.Join(new V[] {R.Get(v_268, (V)"pressure_rng")}, new Results((V)R.Length(v_280)))).First)));
V v_282 = values_281[0];
var values_283 = R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_268, (V)"biome_id")))))).First, (V)"enemy_ids")));
V v_284 = values_283[0];
var values_285 = R.Join(new V[] {}, new Results(R.Get(v_284, R.Call(R.Get(v_8, (V)"next_int"), R.Join(new V[] {R.Get(v_268, (V)"pressure_rng")}, new Results((V)R.Length(v_284)))).First)));
V v_286 = values_285[0];
var values_287 = R.Join(new V[] {}, R.Call(v_266, R.Join(new V[] {v_264, v_286, R.Get(v_282, (V)"x"), R.Get(v_282, (V)"y")}, new Results(R.Get(v_264, (V)"clock")))));
V v_288 = values_287[0];
var values_289 = R.Join(new V[] {}, new Results((V)(R.Get(v_268, (V)"pressure_count") + (V)1.0d)));
var obj_290 = v_268;
V key_291 = (V)"pressure_count";
R.Set(obj_290, key_291, values_289[0]);
R.Call(v_265, R.Join(new V[] {(V)"PressureSpawned", R.Table(new Entry((V)"enemy_id", R.Get(v_288, (V)"id")), new Entry((V)"definition_id", v_286), new Entry((V)"tier", R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)3.0d}, new Results((V)((V)1.0d + R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_268, (V)"pressure_count") / (V)3.0d)))).First)))).First), new Entry((V)"floor_id", R.Get(v_264, (V)"active_floor_id")), new Entry((V)"x", R.Get(v_282, (V)"x")), new Entry((V)"y", R.Get(v_282, (V)"y")))}, new Results(R.Get(v_288, (V)"id"))));
} else {
var values_292 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"time", R.Get(v_264, (V)"clock")), new Entry((V)"kind", R.Or(R.And((V)(v_272 >= R.Get(v_270, (V)"pressure_max_active")), () => (V)"population_cap"), () => (V)"no_legal_anchor")))));
var obj_293 = R.Get(v_268, (V)"diagnostics");
V key_294 = (V)((V)R.Length(R.Get(v_268, (V)"diagnostics")) + (V)1.0d);
R.Set(obj_293, key_294, values_292[0]);
}
var values_295 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)3.0d}, new Results((V)((V)1.0d + R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_268, (V)"pressure_count") / (V)3.0d)))).First)))));
V v_296 = values_295[0];
var values_297 = R.Join(new V[] {}, new Results((V)(R.Get(v_264, (V)"clock") + R.Get(R.Get(v_270, (V)"pressure_periods"), v_296))));
var obj_298 = v_268;
V key_299 = (V)"next_pressure";
R.Set(obj_298, key_299, values_297[0]);
R.Call(R.Get(v_10, (V)"enqueue"), R.Join(new V[] {v_264}, new Results(R.Table(new Entry((V)"kind", (V)"stage_pressure"), new Entry((V)"due", R.Get(v_268, (V)"next_pressure"))))));
return Results.Empty;
}));
V v_300 = V.Nil;
v_300 = new V((RuleFunction)(args_301 => {
V v_302 = args_301[0];
V v_303 = args_301[1];
V v_304 = args_301[2];
var values_305 = R.Join(new V[] {}, new Results(R.Get(v_302, (V)"stage")));
V v_306 = values_305[0];
if (R.Truth(R.Or((V)(R.Get(v_306, (V)"phase") != (V)"extraction"), () => R.Get(v_306, (V)"pursuer_id")))) {
return Results.Empty;
}
var values_307 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results(v_302))));
V v_308 = values_307[0];
var values_309 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get_stage_rules"), Results.Empty));
V v_310 = values_309[0];
var values_311 = R.Join(new V[] {}, R.Call(v_91, R.Join(new V[] {v_302, R.Get(v_310, (V)"pursuer_spawn_distance")}, new Results(v_308))));
V v_312 = values_311[0];
if (R.Truth((V)((V)R.Length(v_312) == (V)0.0d))) {
var values_313 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_302))));
V v_314 = values_313[0];
{ double end_317 = R.Num((V)(R.Get(v_314, (V)"height") - (V)1.0d)), step_318 = R.Num((V)1.0d);
for (double i_316 = R.Num((V)2.0d); step_318 > 0 ? i_316 <= end_317 : i_316 >= end_317; i_316 += step_318) {
V v_315 = i_316;
{ double end_321 = R.Num((V)(R.Get(v_314, (V)"width") - (V)1.0d)), step_322 = R.Num((V)1.0d);
for (double i_320 = R.Num((V)2.0d); step_322 > 0 ? i_320 <= end_321 : i_320 >= end_321; i_320 += step_322) {
V v_319 = i_320;
if (R.Truth(R.Call(v_77, R.Join(new V[] {v_302, v_319, v_315, R.Get(v_310, (V)"pursuer_spawn_distance")}, new Results(v_308))).First)) {
var values_323 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_319), new Entry((V)"y", v_315))));
var obj_324 = v_312;
V key_325 = (V)((V)R.Length(v_312) + (V)1.0d);
R.Set(obj_324, key_325, values_323[0]);
}
}
}
}
}
}
var values_326 = R.Join(new V[] {}, new Results((V)"pursuit"));
var obj_327 = v_306;
V key_328 = (V)"phase";
R.Set(obj_327, key_328, values_326[0]);
if (R.Truth((V)((V)R.Length(v_312) == (V)0.0d))) {
var values_329 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"time", R.Get(v_302, (V)"clock")), new Entry((V)"kind", (V)"no_pursuer_spawn"))));
var obj_330 = R.Get(v_306, (V)"diagnostics");
V key_331 = (V)((V)R.Length(R.Get(v_306, (V)"diagnostics")) + (V)1.0d);
R.Set(obj_330, key_331, values_329[0]);
R.Call(v_303, R.Join(new V[] {(V)"PursuerSpawnFailed", R.Table(new Entry((V)"deadline", R.Get(v_306, (V)"deadline")))}, new Results(V.Nil)));
return Results.Empty;
}
var values_332 = R.Join(new V[] {}, new Results(R.Get(v_312, (V)R.Length(v_312))));
V v_333 = values_332[0];
var values_334 = R.Join(new V[] {}, R.Call(v_304, R.Join(new V[] {v_302, (V)"core:enemy/extraction_pursuer", R.Get(v_333, (V)"x"), R.Get(v_333, (V)"y")}, new Results(R.Get(v_302, (V)"clock")))));
V v_335 = values_334[0];
var values_336 = R.Join(new V[] {}, new Results(R.Get(v_335, (V)"id")));
var obj_337 = v_306;
V key_338 = (V)"pursuer_id";
R.Set(obj_337, key_338, values_336[0]);
R.Call(v_303, R.Join(new V[] {(V)"PursuerArrived", R.Table(new Entry((V)"pursuer_id", R.Get(v_335, (V)"id")), new Entry((V)"deadline", R.Get(v_306, (V)"deadline")), new Entry((V)"floor_id", R.Get(v_302, (V)"active_floor_id")), new Entry((V)"x", R.Get(v_333, (V)"x")), new Entry((V)"y", R.Get(v_333, (V)"y")))}, new Results(R.Get(v_335, (V)"id"))));
return Results.Empty;
}));
R.Set(v_14, (V)"resolve_due", new V((RuleFunction)(args_339 => {
V v_340 = args_339[0];
V v_341 = args_339[1];
V v_342 = args_339[2];
V v_343 = args_339[3];
if (R.Truth((V)(!R.Truth(R.Get(v_340, (V)"stage"))))) {
return Results.Empty;
}
if (R.Truth((V)(R.Get(v_341, (V)"kind") == (V)"stage_pressure"))) {
R.Call(v_262, R.Join(new V[] {v_340, v_342}, new Results(v_343)));
} else if (R.Truth((V)(R.Get(v_341, (V)"kind") == (V)"stage_deadline"))) {
R.Call(v_300, R.Join(new V[] {v_340, v_342}, new Results(v_343)));
}
return Results.Empty;
})));
R.Set(v_14, (V)"route_options", new V((RuleFunction)(args_344 => {
V v_345 = args_344[0];
V v_346 = args_344[1];
var values_347 = R.Join(new V[] {}, new Results(R.Get(v_345, (V)"stage")));
V v_348 = values_347[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_348)), () => (V)(R.Get(v_348, (V)"exit_id") != v_346)), () => R.And((V)(R.Get(v_348, (V)"phase") != (V)"extraction"), () => (V)(R.Get(v_348, (V)"phase") != (V)"pursuit"))))) {
return R.Join(new V[] {V.Nil}, new Results((V)"exit unavailable"));
}
var values_349 = Results.Empty;
V v_350 = values_349[0];
foreach (var row_351 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_345, (V)"entities"))))))) {
V v_352 = row_351[0];
V v_353 = row_351[1];
if (R.Truth((V)(R.Get(v_353, (V)"id") == v_346))) {
var values_354 = R.Join(new V[] {}, new Results(v_353));
v_350 = values_354[0];

break;
}
}
if (R.Truth(R.Or((V)(!R.Truth(v_350)), () => (V)(R.Get(v_350, (V)"controller") != (V)"exit")))) {
return R.Join(new V[] {V.Nil}, new Results((V)"exit unavailable"));
}
var values_355 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_345, (V)"entities"), (V)1.0d)));
V v_356 = values_355[0];
if (R.Truth(R.Or((V)(R.Get(R.Get(v_356, (V)"position"), (V)"floor_id") != R.Get(R.Get(v_350, (V)"position"), (V)"floor_id")), () => (V)(R.Call(v_38, R.Join(new V[] {R.Get(v_356, (V)"position")}, new Results(R.Get(v_350, (V)"position")))).First != (V)1.0d)))) {
return R.Join(new V[] {V.Nil}, new Results((V)"exit not adjacent"));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"exit_id", v_346), new Entry((V)"selected", (V)1.0d), new Entry((V)"routes", R.Get(v_350, (V)"routes")), new Entry((V)"stage_index", R.Get(v_348, (V)"index")))));
return Results.Empty;
})));
R.Set(v_14, (V)"validate", new V((RuleFunction)(args_357 => {
V v_358 = args_357[0];
var values_359 = R.Join(new V[] {}, new Results(R.Get(v_358, (V)"stage")));
V v_360 = values_359[0];
if (R.Truth((V)(!R.Truth(v_360)))) {
return R.Join(new V[] {}, new Results((V)true));
}
var values_361 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"exploring", (V)true), new Entry((V)"elite_active", (V)true), new Entry((V)"extraction", (V)true), new Entry((V)"pursuit", (V)true))));
V v_362 = values_361[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.Get(v_362, R.Get(v_360, (V)"phase")), () => (V)(R.Get(v_360, (V)"index") >= (V)1.0d)), () => (V)(R.Get(v_360, (V)"index") <= (V)3.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_360, (V)"route_history")))).First == (V)"table")), () => (V)((V)R.Length(R.Get(v_360, (V)"route_history")) == (V)(R.Get(v_360, (V)"index") - (V)1.0d)))}, new Results((V)"invalid active stage state")));
var values_363 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_6, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_360, (V)"biome_id")))).First}, new Results((V)"invalid stage biome"))));
V v_364 = values_363[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_360, (V)"elite_definition_id") == V.Nil), () => (V)(R.Get(v_360, (V)"elite_definition_id") == R.Get(v_364, (V)"elite_id")))}, new Results((V)"stage elite content mismatch")));
var values_365 = R.Join(new V[] {}, new Results(R.Get(v_364, (V)"floor_id")));
V v_366 = values_365[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(R.Get(v_358, (V)"generated_map"), (V)"biome_id") == R.Get(v_364, (V)"id"))}, new Results((V)"stage biome mismatch")));
if (R.Truth((V)(R.Get(v_360, (V)"index") == (V)1.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_360, (V)"biome_id") == R.Get(R.Get(v_6, (V)"order"), (V)1.0d))}, new Results((V)"first stage biome mismatch")));
} else {
var values_367 = R.Join(new V[] {}, new Results(R.Table()));
V v_368 = values_367[0];
{ double end_371 = R.Num((V)(R.Get(v_360, (V)"index") - (V)1.0d)), step_372 = R.Num((V)1.0d);
for (double i_370 = R.Num((V)1.0d); step_372 > 0 ? i_370 <= end_371 : i_370 >= end_371; i_370 += step_372) {
V v_369 = i_370;
var values_373 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_360, (V)"route_history"), v_369)));
V v_374 = values_373[0];
var values_375 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_374))).First == (V)"string"), () => R.Invoke(v_374, "gsub", R.Join(new V[] {(V)"^core:route/"}, new Results((V)"core:biome/"))).First), () => V.Nil)));
V v_376 = values_375[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(v_376, () => R.Call(R.Get(v_6, (V)"get"), R.Join(new V[] {}, new Results(v_376))).First), () => (V)(v_376 != R.Get(R.Get(v_6, (V)"order"), (V)1.0d))), () => (V)(!R.Truth(R.Get(v_368, v_376))))}, new Results((V)"invalid or repeated biome route")));
var values_377 = R.Join(new V[] {}, new Results((V)true));
var obj_378 = v_368;
V key_379 = v_376;
R.Set(obj_378, key_379, values_377[0]);
if (R.Truth((V)(v_369 == (V)(R.Get(v_360, (V)"index") - (V)1.0d)))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_376 == R.Get(v_360, (V)"biome_id"))}, new Results((V)"route destination mismatch")));
}
}
}
}
var values_380 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_360, (V)"index") == (V)1.0d), () => v_366), () => R.Concat(v_366, R.Concat((V)"/stage/", R.Get(v_360, (V)"index"))))));
V v_381 = values_380[0];
if (R.Truth(R.Get(v_358, (V)"floor_order"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)((V)R.Length(R.Get(v_358, (V)"floor_order")) == (V)2.0d), () => (V)(R.Get(R.Get(v_358, (V)"floor_order"), (V)1.0d) == R.Concat(v_366, R.Concat((V)"/stage/", R.Concat(R.Get(v_360, (V)"index"), (V)"/depth/1"))))), () => (V)(R.Get(R.Get(v_358, (V)"floor_order"), (V)2.0d) == R.Concat(v_366, R.Concat((V)"/stage/", R.Concat(R.Get(v_360, (V)"index"), (V)"/depth/2"))))), () => (V)(R.Get(v_358, (V)"active_floor_id") == R.Get(R.Get(v_358, (V)"floor_order"), R.Get(v_358, (V)"active_floor_index")))), () => (V)(R.Get(v_360, (V)"elite_floor_id") == R.Get(R.Get(v_358, (V)"floor_order"), (V)2.0d)))}, new Results((V)"connected stage floor identity mismatch")));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_358, (V)"active_floor_id") == v_381)}, new Results((V)"stage floor identity mismatch")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_360, (V)"pressure_count")))).First == (V)"number"), () => (V)(R.Get(v_360, (V)"pressure_count") >= (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_360, (V)"pressure_rng")))).First == (V)"table"))}, new Results((V)"invalid pressure director")));
R.Call(R.Get(v_8, (V)"new"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_360, (V)"pressure_rng"), (V)"state"))));
var values_382 = R.Join(new V[] {}, new Results(R.Table()));
V v_383 = values_382[0];
foreach (var row_384 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_358, (V)"entities"))))))) {
V v_385 = row_384[0];
V v_386 = row_384[1];
var values_387 = R.Join(new V[] {}, new Results(v_386));
var obj_388 = v_383;
V key_389 = R.Get(v_386, (V)"id");
R.Set(obj_388, key_389, values_387[0]);
}
if (R.Truth(R.Or((V)(R.Get(v_360, (V)"phase") == (V)"exploring"), () => (V)(R.Get(v_360, (V)"phase") == (V)"elite_active")))) {
var values_390 = R.Join(new V[] {}, new Results(R.Get(v_383, R.Get(v_360, (V)"elite_id"))));
V v_391 = values_390[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(v_391, () => (V)(R.Get(v_391, (V)"definition_id") == R.Or(R.Get(v_360, (V)"elite_definition_id"), () => (V)"core:enemy/thorn_warden"))), () => R.Or((V)(R.Get(v_391, (V)"ai_id") == (V)"warden"), () => (V)(R.Get(v_391, (V)"ai_id") == (V)"biome_elite")))}, new Results((V)"active stage missing biome elite")));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_383, R.Get(v_360, (V)"elite_id")))), () => R.Get(v_383, R.Get(v_360, (V)"elite_reward_id"))), () => (V)(R.Get(R.Get(v_383, R.Get(v_360, (V)"elite_reward_id")), (V)"controller") == (V)"chest")), () => R.Get(v_383, R.Get(v_360, (V)"exit_id"))), () => (V)(R.Get(R.Get(v_383, R.Get(v_360, (V)"exit_id")), (V)"controller") == (V)"exit")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_360, (V)"defeat_time")))).First == (V)"number")), () => (V)(R.Get(v_360, (V)"deadline") == (V)(R.Get(v_360, (V)"defeat_time") + R.Get(R.Call(R.Get(v_2, (V)"get_stage_rules"), Results.Empty).First, (V)"grace"))))}, new Results((V)"invalid extraction state")));
if (R.Truth(R.Get(v_360, (V)"pursuer_id"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_383, R.Get(v_360, (V)"pursuer_id")), () => R.Get(R.Get(v_383, R.Get(v_360, (V)"pursuer_id")), (V)"invulnerable"))}, new Results((V)"missing active pursuer")));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_14));
return Results.Empty;
} }
