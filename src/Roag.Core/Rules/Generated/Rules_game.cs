// Static C# port of src/game.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 8b8ad0f6689d0dcb71a53a1a2a4ad357efa0524e88f3fe6a1cedde5aaa52e91e
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_game { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_patterns"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievements"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_duelist"))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_geomancer"))));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))));
V v_20 = values_19[0];
var values_21 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_stormcaller"))));
V v_22 = values_21[0];
var values_23 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_wayfinder"))));
V v_24 = values_23[0];
var values_25 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_26 = values_25[0];
var values_27 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_28 = values_27[0];
var values_29 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.finale"))));
V v_30 = values_29[0];
var values_31 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))));
V v_32 = values_31[0];
var values_33 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_34 = values_33[0];
var values_35 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_36 = values_35[0];
var values_37 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_38 = values_37[0];
var values_39 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.stage"))));
V v_40 = values_39[0];
var values_41 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.targeting"))));
V v_42 = values_41[0];
var values_43 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_44 = values_43[0];
var values_45 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))));
V v_46 = values_45[0];
var values_47 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.regular_ai"))));
V v_48 = values_47[0];
var values_49 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.review_features"))));
V v_50 = values_49[0];
var values_51 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_interactions"))));
V v_52 = values_51[0];
var values_53 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_54 = values_53[0];
var values_55 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_56 = values_55[0];
var values_57 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_sentinel"))));
V v_58 = values_57[0];
var values_59 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_mirrors"))));
V v_60 = values_59[0];
var values_61 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_silt"))));
V v_62 = values_61[0];
var values_63 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_embers"))));
V v_64 = values_63[0];
var values_65 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_webs"))));
V v_66 = values_65[0];
var values_67 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_oaths"))));
V v_68 = values_67[0];
var values_69 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_70 = values_69[0];
var values_71 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.world"))));
V v_72 = values_71[0];
var values_73 = R.Join(new V[] {}, new Results(R.Table()));
V v_74 = values_73[0];
var values_75 = R.Join(new V[] {}, new Results((V)"roag-run/11"));
V v_76 = values_75[0];
var values_77 = R.Join(new V[] {}, new Results(v_76));
var obj_78 = v_74;
V key_79 = (V)"snapshot_schema";
R.Set(obj_78, key_79, values_77[0]);
var values_80 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"wait", (V)100.0d), new Entry((V)"basic_attack", (V)100.0d), new Entry((V)"sweeping_slash", (V)150.0d), new Entry((V)"dash", (V)130.0d), new Entry((V)"claim_chest", (V)100.0d), new Entry((V)"open_vault", (V)100.0d), new Entry((V)"choose_route", (V)0.0d))));
V v_81 = values_80[0];
var values_82 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table()}, new Results(R.Table(new Entry((V)"__mode", (V)"k"))))));
V v_83 = values_82[0];
V v_84 = V.Nil;
v_84 = new V((RuleFunction)(args_85 => {
V v_86 = args_85[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_86))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_86));
}
var values_87 = R.Join(new V[] {}, new Results(R.Table()));
V v_88 = values_87[0];
foreach (var row_89 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_86)))))) {
V v_90 = row_89[0];
V v_91 = row_89[1];
var values_92 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_91))));
var obj_93 = v_88;
V key_94 = v_90;
R.Set(obj_93, key_94, values_92[0]);
}
return R.Join(new V[] {}, new Results(v_88));
return Results.Empty;
}));
V v_95 = V.Nil;
v_95 = new V((RuleFunction)(args_96 => {
V v_97 = args_96[0];
V v_98 = args_96[1];
V v_99 = args_96[2];
V v_100 = args_96[3];
V v_101 = args_96[4];
V v_102 = args_96[5];
var values_103 = R.Join(new V[] {}, new Results(R.Get(v_83, v_98)));
V v_104 = values_103[0];
var values_105 = R.Join(new V[] {}, new Results(R.And(v_104, () => R.Get(v_104, (V)"cause"))));
V v_106 = values_105[0];
var values_107 = Results.Empty;
V v_108 = values_107[0];
foreach (var row_109 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_97, (V)"entities"))))))) {
V v_110 = row_109[0];
V v_111 = row_109[1];
if (R.Truth((V)(R.Get(v_111, (V)"id") == v_101))) {
var values_112 = R.Join(new V[] {}, new Results(v_111));
v_108 = values_112[0];

break;
}
}
var values_113 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_97, (V)"next_event_id")), new Entry((V)"kind", v_99), new Entry((V)"time", R.Get(v_97, (V)"clock")), new Entry((V)"source_id", v_101), new Entry((V)"action_id", v_102), new Entry((V)"root_action_id", R.Or(R.And(v_106, () => R.Get(v_106, (V)"root_action_id")), () => v_102)), new Entry((V)"chain_id", R.Or(R.And(v_106, () => R.Get(v_106, (V)"chain_id")), () => v_102)), new Entry((V)"parent_id", R.Or(R.And(v_106, () => R.Get(v_106, (V)"parent_id")), () => V.Nil)), new Entry((V)"ancestry", R.Or(R.And(v_106, () => R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_106, (V)"ancestry")))).First), () => R.Table())), new Entry((V)"proc_coefficient", R.Or(R.And(v_106, () => R.Get(v_106, (V)"proc_coefficient")), () => (V)1.0d)), new Entry((V)"source_faction", R.Or(R.And(v_108, () => R.Get(v_108, (V)"faction")), () => R.And(v_106, () => R.Get(v_106, (V)"source_faction")))), new Entry((V)"source_position", R.Or(R.And(v_108, () => R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_108, (V)"position")))).First), () => R.And(v_106, () => R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_106, (V)"source_position")))).First))), new Entry((V)"source_tags", R.Or(R.And(v_108, () => R.Table(new Entry((V)1, R.Get(v_108, (V)"definition_id")), new Entry((V)2, R.Get(v_108, (V)"faction")), new Entry((V)3, R.Get(v_108, (V)"ai_id")))), () => V.Nil)))));
V v_114 = values_113[0];
if (R.Truth(R.And(v_106, () => R.Get(v_106, (V)"class_generated")))) {
var values_115 = R.Join(new V[] {}, new Results((V)true));
var obj_116 = v_114;
V key_117 = (V)"class_generated";
R.Set(obj_116, key_117, values_115[0]);
}
var values_118 = R.Join(new V[] {}, new Results((V)(R.Get(v_97, (V)"next_event_id") + (V)1.0d)));
var obj_119 = v_97;
V key_120 = (V)"next_event_id";
R.Set(obj_119, key_120, values_118[0]);
foreach (var row_121 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(v_100, () => R.Table()))))))) {
V v_122 = row_121[0];
V v_123 = row_121[1];
var values_124 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_123))));
var obj_125 = v_114;
V key_126 = v_122;
R.Set(obj_125, key_126, values_124[0]);
}
var values_127 = R.Join(new V[] {}, new Results(v_114));
var obj_128 = v_98;
V key_129 = (V)((V)R.Length(v_98) + (V)1.0d);
R.Set(obj_128, key_129, values_127[0]);
if (R.Truth(v_104)) {
var values_130 = R.Join(new V[] {}, new Results(v_114));
var obj_131 = R.Get(v_104, (V)"events");
V key_132 = (V)((V)R.Length(R.Get(v_104, (V)"events")) + (V)1.0d);
R.Set(obj_131, key_132, values_130[0]);
}
return R.Join(new V[] {}, new Results(v_114));
return Results.Empty;
}));
R.Set(v_74, (V)"effective_cost", new V((RuleFunction)(args_133 => {
V v_134 = args_133[0];
V v_135 = args_133[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_134))).First == (V)"number"), () => (V)((V)(v_134 % (V)1.0d) == (V)0.0d)), () => (V)(v_134 > (V)0.0d)), () => (V)(v_134 <= (V)1000000.0d))}, new Results((V)"invalid base cost")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_135))).First == (V)"number"), () => (V)((V)(v_135 % (V)1.0d) == (V)0.0d)), () => (V)(v_135 > (V)0.0d)), () => (V)(v_135 <= (V)4503599627370496.0d))}, new Results((V)"invalid action speed")));
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)(v_134 * (V)200.0d) + v_135) / (V)(v_135 * (V)2.0d))))))));
return Results.Empty;
})));
R.Set(v_74, (V)"entity", new V((RuleFunction)(args_136 => {
V v_137 = args_136[0];
V v_138 = args_136[1];
foreach (var row_139 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"entities"))))))) {
V v_140 = row_139[0];
V v_141 = row_139[1];
if (R.Truth((V)(R.Get(v_141, (V)"id") == v_138))) {
return R.Join(new V[] {}, new Results(v_141));
}
}
return R.Join(new V[] {}, new Results(V.Nil));
return Results.Empty;
})));
R.Set(v_74, (V)"player", new V((RuleFunction)(args_142 => {
V v_143 = args_142[0];
return R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_143}, new Results(R.Get(v_143, (V)"player_id")))).First}, new Results((V)"player entity missing"))));
return Results.Empty;
})));
V v_144 = V.Nil;
v_144 = new V((RuleFunction)(args_145 => {
V v_146 = args_145[0];
V v_147 = args_145[1];
V v_148 = args_145[2];
V v_149 = args_145[3];
V v_150 = args_145[4];
V v_151 = args_145[5];
foreach (var row_152 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_146, (V)"entities"))))))) {
V v_153 = row_152[0];
V v_154 = row_152[1];
var values_155 = R.Join(new V[] {}, new Results(R.Get(v_154, (V)"position")));
V v_156 = values_155[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_154, (V)"id") != v_150), () => v_156), () => (V)(R.Get(v_156, (V)"floor_id") == v_147)), () => (V)(R.Get(v_156, (V)"x") == v_148)), () => (V)(R.Get(v_156, (V)"y") == v_149)), () => R.Or((V)(!R.Truth(v_151)), () => R.And((V)(R.Get(v_154, (V)"hp") != V.Nil), () => (V)(R.Get(v_154, (V)"hp") > (V)0.0d)))), () => R.Or(v_151, () => R.Get(v_154, (V)"blocks_movement"))))) {
return R.Join(new V[] {}, new Results(v_154));
}
}
return R.Join(new V[] {}, new Results(V.Nil));
return Results.Empty;
}));
R.Set(v_74, (V)"spawn_enemy", new V((RuleFunction)(args_157 => {
V v_158 = args_157[0];
V v_159 = args_157[1];
V v_160 = args_157[2];
V v_161 = args_157[3];
V v_162 = args_157[4];
var values_163 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"get_enemy"), R.Join(new V[] {}, new Results(v_159))).First}, new Results((V)"unknown enemy definition"))));
V v_164 = values_163[0];
var values_165 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_158))).First}, new Results((V)"unknown map"))));
V v_166 = values_165[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_166, v_160}, new Results(v_161))).First}, new Results((V)"enemy spawn is blocked")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_158, R.Get(v_166, (V)"floor_id"), v_160, v_161, V.Nil}, new Results((V)false))).First))}, new Results((V)"enemy spawn is occupied")));
var values_167 = R.Join(new V[] {}, new Results(R.Get(v_158, (V)"next_entity_id")));
V v_168 = values_167[0];
var values_169 = R.Join(new V[] {}, new Results((V)(v_168 + (V)1.0d)));
var obj_170 = v_158;
V key_171 = (V)"next_entity_id";
R.Set(obj_170, key_171, values_169[0]);
var values_172 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_168), new Entry((V)"definition_id", v_159), new Entry((V)"controller", (V)"ai"), new Entry((V)"ai_id", R.Get(v_164, (V)"ai")), new Entry((V)"ai_memory", R.Table(new Entry((V)"last_dx", (V)0.0d), new Entry((V)"last_dy", (V)0.0d))), new Entry((V)"faction", (V)"enemy"), new Entry((V)"boons", R.Table()), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_166, (V)"floor_id")), new Entry((V)"x", v_160), new Entry((V)"y", v_161))), new Entry((V)"blocks_movement", (V)true), new Entry((V)"hp", R.Get(v_164, (V)"hp")), new Entry((V)"max_hp", R.Get(v_164, (V)"hp")), new Entry((V)"damage", R.Get(v_164, (V)"damage")), new Entry((V)"defense", R.Get(v_164, (V)"defense")), new Entry((V)"speed", R.Get(v_164, (V)"speed")), new Entry((V)"stunned_until", (V)0.0d), new Entry((V)"invulnerable", R.Or(R.Get(v_164, (V)"invulnerable"), () => V.Nil)))));
V v_173 = values_172[0];
var values_174 = R.Join(new V[] {}, new Results(v_173));
var obj_175 = R.Get(v_158, (V)"entities");
V key_176 = (V)((V)R.Length(R.Get(v_158, (V)"entities")) + (V)1.0d);
R.Set(obj_175, key_176, values_174[0]);
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_158}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", R.Or(v_162, () => R.Get(v_158, (V)"clock"))), new Entry((V)"actor_id", v_168), new Entry((V)"is_player", (V)false)))));
return R.Join(new V[] {}, new Results(v_173));
return Results.Empty;
})));
R.Set(v_74, (V)"spawn_chest", new V((RuleFunction)(args_177 => {
V v_178 = args_177[0];
V v_179 = args_177[1];
V v_180 = args_177[2];
V v_181 = args_177[3];
V v_182 = args_177[4];
var values_183 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"get_chest"), R.Join(new V[] {}, new Results(v_179))).First}, new Results((V)"unknown chest definition"))));
V v_184 = values_183[0];
var values_185 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_178))).First}, new Results((V)"unknown map"))));
V v_186 = values_185[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_186, v_180}, new Results(v_181))).First}, new Results((V)"chest spawn is blocked")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_178, R.Get(v_186, (V)"floor_id"), v_180, v_181, V.Nil}, new Results((V)false))).First))}, new Results((V)"chest spawn is occupied")));
var values_187 = Results.Empty;
V v_188 = values_187[0];
if (R.Truth(R.Get(v_178, (V)"generated_map"))) {
var values_189 = R.Join(new V[] {}, new Results(R.Or(v_182, () => R.Concat(v_179, R.Concat((V)":", R.Concat(v_180, R.Concat((V)",", v_181)))))));
v_182 = values_189[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_182))).First == (V)"string"), () => (V)((V)R.Length(v_182) > (V)0.0d))}, new Results((V)"generated chest needs stable marker ID")));
foreach (var row_190 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_178, (V)"entities"))))))) {
V v_191 = row_190[0];
V v_192 = row_190[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_192, (V)"stable_marker_id") != v_182)}, new Results((V)"duplicate generated chest marker ID")));
}
var values_193 = R.Join(new V[] {}, R.Call(R.Get(v_34, (V)"new"), R.Join(new V[] {}, R.Call(R.Get(v_34, (V)"derive"), R.Join(new V[] {R.Get(v_178, (V)"run_seed")}, new Results(R.Concat(R.Get(v_178, (V)"active_floor_id"), R.Concat((V)":", v_182))))))));
v_188 = values_193[0];
}
var values_194 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_178, (V)"next_entity_id")), new Entry((V)"definition_id", v_179), new Entry((V)"controller", (V)"chest"), new Entry((V)"faction", (V)"neutral"), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_186, (V)"floor_id")), new Entry((V)"x", v_180), new Entry((V)"y", v_181))), new Entry((V)"blocks_movement", (V)true), new Entry((V)"claimed", (V)false), new Entry((V)"price", R.Get(v_184, (V)"price")), new Entry((V)"offers", R.Call(R.Get(v_36, (V)"make_offers"), R.Join(new V[] {v_178, v_184}, new Results(v_188))).First), new Entry((V)"stable_marker_id", v_182))));
V v_195 = values_194[0];
var values_196 = R.Join(new V[] {}, new Results((V)(R.Get(v_195, (V)"id") + (V)1.0d)));
var obj_197 = v_178;
V key_198 = (V)"next_entity_id";
R.Set(obj_197, key_198, values_196[0]);
var values_199 = R.Join(new V[] {}, new Results(v_195));
var obj_200 = R.Get(v_178, (V)"entities");
V key_201 = (V)((V)R.Length(R.Get(v_178, (V)"entities")) + (V)1.0d);
R.Set(obj_200, key_201, values_199[0]);
return R.Join(new V[] {}, new Results(v_195));
return Results.Empty;
})));
R.Set(v_74, (V)"spawn_door", new V((RuleFunction)(args_202 => {
V v_203 = args_202[0];
V v_204 = args_202[1];
V v_205 = args_202[2];
V v_206 = args_202[3];
var values_207 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_203))).First}, new Results((V)"unknown map"))));
V v_208 = values_207[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_208, v_204}, new Results(v_205))).First}, new Results((V)"door socket is blocked")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_203, R.Get(v_208, (V)"floor_id"), v_204, v_205, V.Nil}, new Results((V)false))).First))}, new Results((V)"door socket is occupied")));
var values_209 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_203, (V)"next_entity_id")), new Entry((V)"definition_id", (V)"core:door/ruin"), new Entry((V)"controller", (V)"door"), new Entry((V)"faction", (V)"neutral"), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_208, (V)"floor_id")), new Entry((V)"x", v_204), new Entry((V)"y", v_205))), new Entry((V)"stable_marker_id", v_206), new Entry((V)"open", (V)false), new Entry((V)"blocks_movement", (V)true), new Entry((V)"blocks_vision", (V)true), new Entry((V)"blocks_projectiles", (V)true))));
V v_210 = values_209[0];
var values_211 = R.Join(new V[] {}, new Results((V)(R.Get(v_210, (V)"id") + (V)1.0d)));
var obj_212 = v_203;
V key_213 = (V)"next_entity_id";
R.Set(obj_212, key_213, values_211[0]);
var values_214 = R.Join(new V[] {}, new Results(v_210));
var obj_215 = R.Get(v_203, (V)"entities");
V key_216 = (V)((V)R.Length(R.Get(v_203, (V)"entities")) + (V)1.0d);
R.Set(obj_215, key_216, values_214[0]);
return R.Join(new V[] {}, new Results(v_210));
return Results.Empty;
})));
R.Set(v_74, (V)"new", new V((RuleFunction)(args_217 => {
V v_218 = args_217[0];
V v_219 = args_217[1];
V v_220 = args_217[2];
R.Call(R.Get(v_12, (V)"validate_all"), Results.Empty);
R.Call(R.Get(v_6, (V)"validate"), Results.Empty);
R.Call(R.Get(v_72, (V)"validate"), Results.Empty);
var values_221 = R.Join(new V[] {}, new Results(R.Or(v_218, () => (V)1.0d)));
v_218 = values_221[0];
R.Call(R.Get(v_34, (V)"new"), R.Join(new V[] {}, new Results(v_218)));
var values_222 = R.Join(new V[] {}, new Results(R.Or(v_219, () => (V)"core:map/test_room")));
v_219 = values_222[0];
var values_223 = Results.Empty;
V v_224 = values_223[0];
var values_225 = Results.Empty;
V v_226 = values_225[0];
var values_227 = R.Join(new V[] {}, R.Call(R.Get(v_26, (V)"for_map"), R.Join(new V[] {}, new Results(v_219))));
V v_228 = values_227[0];
if (R.Truth(v_228)) {
var values_229 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Or(R.And(v_220, () => R.Get(v_220, (V)"generation")), () => R.Table())))));
V v_230 = values_229[0];
var values_231 = R.Join(new V[] {}, new Results(R.Get(v_228, (V)"id")));
var obj_232 = v_230;
V key_233 = (V)"biome_id";
R.Set(obj_232, key_233, values_231[0]);
var values_234 = R.Join(new V[] {}, new Results(R.Or(R.And(R.And(v_220, () => R.Get(v_220, (V)"stage_options")), () => R.Get(R.Get(v_220, (V)"stage_options"), (V)"index")), () => (V)1.0d)));
V v_235 = values_234[0];
if (R.Truth(R.And(R.And(v_220, () => R.Get(v_220, (V)"stage")), () => R.Get(v_220, (V)"multifloor")))) {
var values_236 = R.Join(new V[] {}, new Results(v_235));
var obj_237 = v_230;
V key_238 = (V)"stage_index";
R.Set(obj_237, key_238, values_236[0]);
var values_239 = R.Join(new V[] {}, R.Call(R.Get(v_28, (V)"generate_pair"), R.Join(new V[] {v_218}, new Results(v_230))));
v_226 = values_239[0];
var values_240 = R.Join(new V[] {}, new Results(R.Get(v_226, (V)1.0d)));
v_224 = values_240[0];
} else {
var values_241 = R.Join(new V[] {}, R.Call(R.Get(v_28, (V)"generate"), R.Join(new V[] {v_218}, new Results(v_230))));
v_224 = values_241[0];
if (R.Truth(R.And(R.And(v_220, () => R.Get(v_220, (V)"stage")), () => (V)(v_235 > (V)1.0d)))) {
var values_242 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_228, (V)"floor_id"), R.Concat((V)"/stage/", v_235))));
var obj_243 = v_224;
V key_244 = (V)"floor_id";
R.Set(obj_243, key_244, values_242[0]);
var values_245 = R.Join(new V[] {}, new Results(R.Get(v_224, (V)"floor_id")));
var obj_246 = R.Get(v_224, (V)"elite");
V key_247 = (V)"floor_id";
R.Set(obj_246, key_247, values_245[0]);
}
}
}
var values_248 = R.Join(new V[] {}, new Results(R.Or(v_224, () => R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"get_map"), R.Join(new V[] {}, new Results(v_219))).First}, new Results((V)"unknown map content ID"))).First)));
V v_249 = values_248[0];
var values_250 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"get_class"), R.Join(new V[] {}, new Results(R.Or(R.And(v_220, () => R.Get(v_220, (V)"class_id")), () => R.Get(R.Get(v_14, (V)"order"), (V)1.0d))))).First}, new Results((V)"unknown starting class"))));
V v_251 = values_250[0];
var values_252 = R.Join(new V[] {}, new Results((V)(R.Or(R.And(v_220, () => R.Get(v_220, (V)"entity_id_base")), () => (V)0.0d) + (V)1.0d)));
V v_253 = values_252[0];
var values_254 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Or(R.And(v_220, () => R.Get(v_220, (V)"available_boons")), () => R.Call(R.Get(v_6, (V)"starter_order"), Results.Empty).First)))));
V v_255 = values_254[0];
R.Call(R.Get(v_10, (V)"validate_pool"), R.Join(new V[] {}, new Results(v_255)));
var values_256 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", v_76), new Entry((V)"content_version", R.Get(v_12, (V)"version")), new Entry((V)"boon_content_version", R.Get(v_6, (V)"version")), new Entry((V)"map_id", R.Get(v_249, (V)"id")), new Entry((V)"run_seed", v_218), new Entry((V)"root_seed", R.Or(R.And(v_220, () => R.Get(v_220, (V)"root_seed")), () => v_218)), new Entry((V)"available_boons", v_255), new Entry((V)"meta", R.Call(v_84, R.Join(new V[] {}, new Results(R.Or(R.And(v_220, () => R.Get(v_220, (V)"meta")), () => R.Call(R.Get(v_10, (V)"new_run"), R.Join(new V[] {}, new Results((V)false))).First)))).First), new Entry((V)"generated_map", v_224), new Entry((V)"stage_options", R.And(v_220, () => R.Get(v_220, (V)"stage_options"))), new Entry((V)"inactive_floor_maps", R.Table()), new Entry((V)"floor_pause", R.Table()), new Entry((V)"floor_order", R.Or(R.And(v_226, () => R.Table(new Entry((V)1, R.Get(R.Get(v_226, (V)1.0d), (V)"floor_id")), new Entry((V)2, R.Get(R.Get(v_226, (V)2.0d), (V)"floor_id")))), () => V.Nil)), new Entry((V)"active_floor_index", R.Or(R.And(v_226, () => (V)1.0d), () => V.Nil)), new Entry((V)"retired_stages", R.Or(R.And(v_220, () => R.Get(v_220, (V)"retired_stages")), () => R.Table())), new Entry((V)"room_content_version", R.Or(R.And(v_224, () => R.Get(v_28, (V)"room_content_version")), () => V.Nil)), new Entry((V)"active_floor_id", R.Get(v_249, (V)"floor_id")), new Entry((V)"clock", (V)0.0d), new Entry((V)"player_id", v_253), new Entry((V)"next_entity_id", (V)(v_253 + (V)1.0d)), new Entry((V)"next_schedule_id", (V)1.0d), new Entry((V)"next_event_id", (V)1.0d), new Entry((V)"next_action_id", (V)1.0d), new Entry((V)"next_activation_id", (V)1.0d), new Entry((V)"chain_history", R.Table()), new Entry((V)"delayed_effects", R.Table()), new Entry((V)"progression", R.Table(new Entry((V)"level", (V)1.0d), new Entry((V)"xp_total", (V)0.0d))), new Entry((V)"currency", (V)0.0d), new Entry((V)"next_drop_id", (V)1.0d), new Entry((V)"drops", R.Table()), new Entry((V)"rewarded_kills", R.Table()), new Entry((V)"explored", R.Table()), new Entry((V)"visible", R.Table()), new Entry((V)"secrets", R.Table()), new Entry((V)"world_content_version", R.Get(v_72, (V)"version")), new Entry((V)"class_special", R.Table()), new Entry((V)"review_features", R.Table()), new Entry((V)"review_keys", R.Table()), new Entry((V)"review_clues", R.Table()), new Entry((V)"idea_rooms", R.Table(new Entry((V)"version", (V)1.0d), new Entry((V)"nodes", R.Table()), new Entry((V)"groups", R.Table()))), new Entry((V)"rng", R.Call(R.Get(v_34, (V)"new"), R.Join(new V[] {}, new Results(v_218))).First), new Entry((V)"entities", R.Table(new Entry((V)1, R.Table(new Entry((V)"id", v_253), new Entry((V)"definition_id", R.Get(v_251, (V)"id")), new Entry((V)"controller", (V)"player"), new Entry((V)"faction", (V)"player"), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_249, (V)"floor_id")), new Entry((V)"x", R.Get(v_249, (V)"start_x")), new Entry((V)"y", R.Get(v_249, (V)"start_y")))), new Entry((V)"blocks_movement", (V)true), new Entry((V)"hp", R.Get(v_251, (V)"hp")), new Entry((V)"max_hp", R.Get(v_251, (V)"hp")), new Entry((V)"damage", R.Get(v_251, (V)"damage")), new Entry((V)"slash_damage", R.Get(v_251, (V)"slash_damage")), new Entry((V)"defense", R.Get(v_251, (V)"defense")), new Entry((V)"speed", R.Get(v_251, (V)"speed")), new Entry((V)"boons", R.Table()), new Entry((V)"crit_chance_ppm", R.Get(v_251, (V)"crit_chance_ppm")), new Entry((V)"crit_damage_percent", R.Get(v_251, (V)"crit_damage_percent")))))), new Entry((V)"schedule", R.Table()), new Entry((V)"last_action", V.Nil), new Entry((V)"game_over", (V)false))));
V v_257 = values_256[0];
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_257}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)0.0d), new Entry((V)"actor_id", v_253), new Entry((V)"is_player", (V)true)))));
if (R.Truth(v_224)) {
V v_258 = V.Nil;
v_258 = new V((RuleFunction)(args_259 => {
V v_260 = args_259[0];
var values_261 = R.Join(new V[] {}, new Results(R.Get(v_257, (V)"generated_map")));
V v_262 = values_261[0];
foreach (var row_263 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_262, (V)"placements"), (V)"enemies"))))))) {
V v_264 = row_263[0];
V v_265 = row_263[1];
R.Call(R.Get(v_74, (V)"spawn_enemy"), R.Join(new V[] {v_257, R.Get(v_265, (V)"definition_id"), R.Get(v_265, (V)"x")}, new Results(R.Get(v_265, (V)"y"))));
}
foreach (var row_266 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_262, (V)"placements"), (V)"chests"))))))) {
V v_267 = row_266[0];
V v_268 = row_266[1];
R.Call(R.Get(v_74, (V)"spawn_chest"), R.Join(new V[] {v_257, R.Get(v_268, (V)"definition_id"), R.Get(v_268, (V)"x"), R.Get(v_268, (V)"y")}, new Results(R.Or(R.And(v_226, () => R.Concat(R.Get(v_262, (V)"floor_id"), R.Concat((V)":", R.Get(v_268, (V)"id")))), () => R.Get(v_268, (V)"id")))));
}
foreach (var row_269 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_262, (V)"placements"), (V)"doors"), () => R.Table()))))))) {
V v_270 = row_269[0];
V v_271 = row_269[1];
R.Call(R.Get(v_74, (V)"spawn_door"), R.Join(new V[] {v_257, R.Get(v_271, (V)"x"), R.Get(v_271, (V)"y")}, new Results(R.Or(R.And(v_226, () => R.Concat(R.Get(v_262, (V)"floor_id"), R.Concat((V)":", R.Get(v_271, (V)"id")))), () => R.Get(v_271, (V)"id")))));
}
R.Call(R.Get(v_72, (V)"populate"), R.Join(new V[] {v_257}, new Results(v_260)));
R.Call(R.Get(v_50, (V)"populate"), R.Join(new V[] {v_257}, new Results(v_262)));
if (R.Truth(R.And(v_220, () => R.Get(v_220, (V)"stage")))) {
R.Call(R.Get(v_54, (V)"populate"), R.Join(new V[] {v_257, v_262}, new Results(R.Get(v_74, (V)"spawn_enemy"))));
R.Call(R.Get(v_56, (V)"populate"), R.Join(new V[] {v_257, v_262, new V((RuleFunction)(args_272 => {
V v_273 = args_272[0];
V v_274 = args_272[1];
var values_275 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"get_enemy"), R.Join(new V[] {}, new Results(v_274))))));
V v_276 = values_275[0];
var values_277 = R.Join(new V[] {v_274}, new Results(R.Get(v_276, (V)"ai")));
var obj_278 = v_273;
V key_279 = (V)"definition_id";
var obj_280 = v_273;
V key_281 = (V)"ai_id";
R.Set(obj_278, key_279, values_277[0]);
R.Set(obj_280, key_281, values_277[1]);
var values_282 = R.Join(new V[] {R.Get(v_276, (V)"hp"), R.Get(v_276, (V)"hp"), R.Get(v_276, (V)"damage"), R.Get(v_276, (V)"defense")}, new Results(R.Get(v_276, (V)"speed")));
var obj_283 = v_273;
V key_284 = (V)"hp";
var obj_285 = v_273;
V key_286 = (V)"max_hp";
var obj_287 = v_273;
V key_288 = (V)"damage";
var obj_289 = v_273;
V key_290 = (V)"defense";
var obj_291 = v_273;
V key_292 = (V)"speed";
R.Set(obj_283, key_284, values_282[0]);
R.Set(obj_285, key_286, values_282[1]);
R.Set(obj_287, key_288, values_282[2]);
R.Set(obj_289, key_290, values_282[3]);
R.Set(obj_291, key_292, values_282[4]);
var values_293 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"last_dx", (V)0.0d), new Entry((V)"last_dy", (V)0.0d))));
var obj_294 = v_273;
V key_295 = (V)"ai_memory";
R.Set(obj_294, key_295, values_293[0]);
return Results.Empty;
}))}, new Results(R.Get(v_74, (V)"spawn_enemy"))));
}
R.Call(R.Get(v_52, (V)"populate"), R.Join(new V[] {v_257}, new Results(v_262)));
R.Call(R.Get(v_46, (V)"populate"), R.Join(new V[] {v_257}, new Results(v_262)));
return Results.Empty;
}));
R.Call(v_258, R.Join(new V[] {}, new Results((V)false)));
if (R.Truth(v_226)) {
var values_296 = R.Join(new V[] {}, new Results(R.Get(v_226, (V)2.0d)));
var obj_297 = v_257;
V key_298 = (V)"generated_map";
R.Set(obj_297, key_298, values_296[0]);
var values_299 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_226, (V)2.0d), (V)"floor_id")));
var obj_300 = v_257;
V key_301 = (V)"active_floor_id";
R.Set(obj_300, key_301, values_299[0]);
R.Call(v_258, R.Join(new V[] {}, new Results((V)true)));
R.Call(R.Get(v_40, (V)"init"), R.Join(new V[] {v_257}, new Results(R.Get(v_74, (V)"spawn_enemy"))));
R.Call(R.Get(v_32, (V)"park"), R.Join(new V[] {v_257}, new Results(R.Get(v_257, (V)"active_floor_id"))));
var values_302 = R.Join(new V[] {}, new Results(R.Get(v_257, (V)"generated_map")));
var obj_303 = R.Get(v_257, (V)"inactive_floor_maps");
V key_304 = R.Get(v_257, (V)"active_floor_id");
R.Set(obj_303, key_304, values_302[0]);
var values_305 = R.Join(new V[] {}, new Results(R.Get(v_226, (V)1.0d)));
var obj_306 = v_257;
V key_307 = (V)"generated_map";
R.Set(obj_306, key_307, values_305[0]);
var values_308 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_226, (V)1.0d), (V)"floor_id")));
var obj_309 = v_257;
V key_310 = (V)"active_floor_id";
R.Set(obj_309, key_310, values_308[0]);
} else if (R.Truth(R.And(v_220, () => R.Get(v_220, (V)"stage")))) {
R.Call(R.Get(v_40, (V)"init"), R.Join(new V[] {v_257}, new Results(R.Get(v_74, (V)"spawn_enemy"))));
}
}
if (R.Truth(R.And(v_220, () => R.Get(v_220, (V)"demo_enemies")))) {
foreach (var row_311 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"demo_spawns"), R.Join(new V[] {}, new Results(R.Get(v_249, (V)"id"))))))))) {
V v_312 = row_311[0];
V v_313 = row_311[1];
R.Call(R.Get(v_74, (V)"spawn_enemy"), R.Join(new V[] {v_257, R.Get(v_313, (V)"definition_id"), R.Get(v_313, (V)"x")}, new Results(R.Get(v_313, (V)"y"))));
}
}
if (R.Truth(R.And(v_220, () => R.Get(v_220, (V)"demo_chests")))) {
foreach (var row_314 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"demo_chests"), R.Join(new V[] {}, new Results(R.Get(v_249, (V)"id"))))))))) {
V v_315 = row_314[0];
V v_316 = row_314[1];
R.Call(R.Get(v_74, (V)"spawn_chest"), R.Join(new V[] {v_257, R.Get(v_316, (V)"definition_id"), R.Get(v_316, (V)"x")}, new Results(R.Get(v_316, (V)"y"))));
}
}
R.Call(R.Get(v_70, (V)"recompute"), R.Join(new V[] {}, new Results(v_257)));
return R.Join(new V[] {}, new Results(v_257));
return Results.Empty;
})));
R.Set(v_74, (V)"new_generated", new V((RuleFunction)(args_317 => {
V v_318 = args_317[0];
V v_319 = args_317[1];
return R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"new"), R.Join(new V[] {v_318, R.Get(v_28, (V)"map_id")}, new Results(v_319))));
return Results.Empty;
})));
R.Set(v_74, (V)"new_stage", new V((RuleFunction)(args_320 => {
V v_321 = args_320[0];
V v_322 = args_320[1];
var values_323 = R.Join(new V[] {}, new Results(R.Or(v_322, () => R.Table())));
v_322 = values_323[0];
var values_324 = R.Join(new V[] {}, new Results((V)true));
var obj_325 = v_322;
V key_326 = (V)"stage";
R.Set(obj_325, key_326, values_324[0]);
var values_327 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_322, (V)"stage_options"), () => R.Get(R.Get(v_322, (V)"stage_options"), (V)"biome_id")), () => R.Get(R.Get(v_26, (V)"order"), (V)1.0d))));
V v_328 = values_327[0];
return R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"new"), R.Join(new V[] {v_321, R.Get(R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_26, (V)"get"), R.Join(new V[] {}, new Results(v_328))))).First, (V)"map_id")}, new Results(v_322))));
return Results.Empty;
})));
R.Set(v_74, (V)"new_finale", new V((RuleFunction)(args_329 => {
V v_330 = args_329[0];
V v_331 = args_329[1];
var values_332 = R.Join(new V[] {}, new Results(R.Or(v_331, () => R.Table())));
v_331 = values_332[0];
var values_333 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"new"), R.Join(new V[] {v_330, R.Get(R.Get(v_30, (V)"map"), (V)"id")}, new Results(v_331))));
V v_334 = values_333[0];
var values_335 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_30, (V)"map")))));
var obj_336 = v_334;
V key_337 = (V)"final_map";
R.Set(obj_336, key_337, values_335[0]);
R.Call(R.Get(v_30, (V)"init"), R.Join(new V[] {v_334, R.Get(v_74, (V)"spawn_enemy")}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Or(R.Get(v_331, (V)"route_history"), () => R.Table()))))));
R.Call(R.Get(v_70, (V)"recompute"), R.Join(new V[] {}, new Results(v_334)));
return R.Join(new V[] {}, new Results(v_334));
return Results.Empty;
})));
V v_338 = V.Nil;
v_338 = new V((RuleFunction)(args_339 => {
V v_340 = args_339[0];
V v_341 = args_339[1];
V v_342 = args_339[2];
V v_343 = args_339[3];
V v_344 = args_339[4];
if (R.Truth((V)(v_341 == (V)"cancel"))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"cancelled"), new Entry((V)"cells", R.Table()))));
}
if (R.Truth((V)(v_341 == (V)"use_utility"))) {
var values_345 = R.Join(new V[] {}, R.Call(R.Get(v_52, (V)"utility_preview"), R.Join(new V[] {v_340}, new Results(R.Table(new Entry((V)"dx", v_342), new Entry((V)"dy", v_343), new Entry((V)"steps", v_344))))));
V v_346 = values_345[0];
var values_347 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_340))));
V v_348 = values_347[0];

var values_349 = R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.Get(v_346, (V)"utility_move"), () => (V)(!R.Truth(R.Get(v_346, (V)"utility_return")))), () => (V)(R.Call(R.Get(v_44, (V)"move_extra"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_340))).First, (V)"dash"}, new Results(R.Get(v_346, (V)"cells")))).First + R.Call(R.Get(v_62, (V)"move_extra"), R.Join(new V[] {v_340, (V)"dash"}, new Results(R.Get(v_346, (V)"cells")))).First)), () => (V)0.0d)));
V v_350 = values_349[0];
var values_351 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {(V)((V)100.0d + v_350)}, new Results((V)(R.Get(v_348, (V)"speed") + R.Call(R.Get(v_4, (V)"speed_bonus"), R.Join(new V[] {v_340}, new Results(v_348))).First)))));
var obj_352 = v_346;
V key_353 = (V)"cost";
R.Set(obj_352, key_353, values_351[0]);
return R.Join(new V[] {}, new Results(v_346));
}
if (R.Truth((V)(!R.Truth(R.Get(v_81, v_341))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"unknown action"), new Entry((V)"cells", R.Table()))));
}
var values_354 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_340))));
V v_355 = values_354[0];
var values_356 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"for_player"), R.Join(new V[] {}, new Results(v_355))));
V v_357 = values_356[0];
var values_358 = R.Join(new V[] {}, new Results((V)(R.Get(v_355, (V)"speed") + R.Call(R.Get(v_4, (V)"speed_bonus"), R.Join(new V[] {v_340}, new Results(v_355))).First)));
V v_359 = values_358[0];
var values_360 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_357, (V)"costs"), v_341), () => R.Get(v_81, v_341))));
V v_361 = values_360[0];
var values_362 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {v_361}, new Results(v_359))));
V v_363 = values_362[0];
if (R.Truth((V)(v_341 == (V)"wait"))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)true), new Entry((V)"cells", R.Table()), new Entry((V)"cost", v_363))));
}
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_42, (V)"direction_index"), R.Join(new V[] {v_342}, new Results(v_343))).First)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"invalid direction"), new Entry((V)"cells", R.Table()))));
}
if (R.Truth(R.And(R.And((V)(v_341 == (V)"dash"), () => (V)(R.Get(v_357, (V)"id") == R.Get(v_24, (V)"id"))), () => R.Get(R.Get(v_340, (V)"class_special"), (V)"beacon")))) {
var values_364 = R.Join(new V[] {}, new Results(R.Get(v_355, (V)"position")));
V v_365 = values_364[0];
var values_369 = R.Join(new V[] {}, R.Call(R.Get(v_24, (V)"return_preview"), R.Join(new V[] {v_340, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_340))).First, v_355, v_12, R.Get(v_70, (V)"can_see"), R.Get(v_70, (V)"was_explored")}, new Results(new V((RuleFunction)(args_366 => {
V v_367 = args_366[0];
V v_368 = args_366[1];
return R.Join(new V[] {}, R.Call(v_144, R.Join(new V[] {v_340, R.Get(v_365, (V)"floor_id"), v_367, v_368, R.Get(v_355, (V)"id")}, new Results((V)false))));
return Results.Empty;
}))))));
V v_370 = values_369[0];
V v_371 = values_369[1];
V v_372 = values_369[2];
V v_373 = values_369[3];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", v_370), new Entry((V)"reason", v_371), new Entry((V)"cost", v_363), new Entry((V)"cells", R.Or(R.And(v_372, () => R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_372, (V)"x")), new Entry((V)"y", R.Get(v_372, (V)"y")), new Entry((V)"status", (V)"open"))))), () => R.Table())), new Entry((V)"distance", (V)1.0d), new Entry((V)"return_distance", v_373), new Entry((V)"beacon_return", (V)true))));
}
if (R.Truth(R.And((V)(v_341 == (V)"dash"), () => (V)(R.Get(v_357, (V)"id") == (V)"core:actor/shade")))) {
var values_374 = R.Join(new V[] {}, new Results(R.And(R.Get(v_340, (V)"class_special"), () => R.Get(R.Get(v_340, (V)"class_special"), (V)"decoy"))));
V v_375 = values_374[0];
if (R.Truth(R.Or((V)(!R.Truth(v_375)), () => (V)(R.Get(v_375, (V)"floor_id") != R.Get(R.Get(v_355, (V)"position"), (V)"floor_id"))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"place a decoy first"), new Entry((V)"cells", R.Table()))));
}
if (R.Truth(R.Call(v_144, R.Join(new V[] {v_340, R.Get(v_375, (V)"floor_id"), R.Get(v_375, (V)"x"), R.Get(v_375, (V)"y"), R.Get(v_355, (V)"id")}, new Results((V)false))).First)) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"decoy space occupied"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_375, (V)"x")), new Entry((V)"y", R.Get(v_375, (V)"y")), new Entry((V)"status", (V)"occupied"))))))));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)true), new Entry((V)"cost", v_363), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_375, (V)"x")), new Entry((V)"y", R.Get(v_375, (V)"y")), new Entry((V)"status", (V)"open"))))), new Entry((V)"distance", (V)1.0d), new Entry((V)"swap", (V)true))));
}
if (R.Truth(R.Or((V)(v_341 == (V)"move"), () => (V)(v_341 == (V)"dash")))) {
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_42, (V)"is_cardinal"), R.Join(new V[] {v_342}, new Results(v_343))).First)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"movement is cardinal"), new Entry((V)"cells", R.Table()))));
}
var values_376 = R.Join(new V[] {}, new Results(R.Get(v_355, (V)"position")));
V v_377 = values_376[0];
var values_378 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_340))));
V v_379 = values_378[0];
var values_380 = R.Join(new V[] {R.Table(), (V)0.0d}, new Results((V)false));
V v_381 = values_380[0];
V v_382 = values_380[1];
V v_383 = values_380[2];
var values_384 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_341 == (V)"dash"), () => R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)3.0d}, new Results(R.Get(v_357, (V)"dash_distance")))).First), () => (V)1.0d)));
V v_385 = values_384[0];
var values_386 = Results.Empty;
V v_387 = values_386[0];
V v_388 = values_386[1];
V v_389 = values_386[2];
V v_390 = values_386[3];
V v_391 = values_386[4];
{ double end_394 = R.Num(v_385), step_395 = R.Num((V)1.0d);
for (double i_393 = R.Num((V)1.0d); step_395 > 0 ? i_393 <= end_394 : i_393 >= end_394; i_393 += step_395) {
V v_392 = i_393;
if (R.Truth(R.And(R.And((V)(v_341 == (V)"dash"), () => (V)(v_392 > R.Get(v_357, (V)"dash_distance"))), () => (V)(!R.Truth(R.Call(R.Get(v_44, (V)"can_extend_dash"), R.Join(new V[] {v_379}, new Results(v_381))).First))))) {
break;
}
var values_396 = R.Join(new V[] {(V)(R.Get(v_377, (V)"x") + (V)(v_342 * v_392))}, new Results((V)(R.Get(v_377, (V)"y") + (V)(v_343 * v_392))));
V v_397 = values_396[0];
V v_398 = values_396[1];
var values_399 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_397), new Entry((V)"y", v_398))));
V v_400 = values_399[0];
if (R.Truth(v_383)) {
var values_401 = R.Join(new V[] {}, new Results((V)"unreachable"));
var obj_402 = v_400;
V key_403 = (V)"status";
R.Set(obj_402, key_403, values_401[0]);
} else if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_379, v_397}, new Results(v_398))).First)))) {
var values_404 = R.Join(new V[] {}, new Results((V)"wall"));
var obj_405 = v_400;
V key_406 = (V)"status";
R.Set(obj_405, key_406, values_404[0]);
var values_407 = R.Join(new V[] {(V)true}, new Results((V)"wall"));
v_383 = values_407[0];
v_387 = values_407[1];
} else {
var values_408 = R.Join(new V[] {}, R.Call(v_144, R.Join(new V[] {v_340, R.Get(v_377, (V)"floor_id"), v_397, v_398, R.Get(v_355, (V)"id")}, new Results((V)false))));
V v_409 = values_408[0];
if (R.Truth(v_409)) {
if (R.Truth(R.And(R.And((V)(v_341 == (V)"move"), () => (V)(R.Get(v_409, (V)"controller") == (V)"chest")), () => (V)(!R.Truth(R.Get(v_409, (V)"claimed")))))) {
var values_410 = R.Join(new V[] {(V)"chest", (V)"chest"}, new Results(R.Get(v_409, (V)"id")));
var obj_411 = v_400;
V key_412 = (V)"status";
R.Set(obj_411, key_412, values_410[0]);
v_387 = values_410[1];
v_389 = values_410[2];
} else if (R.Truth(R.And((V)(v_341 == (V)"move"), () => (V)(R.Get(v_409, (V)"controller") == (V)"exit")))) {
var values_413 = R.Join(new V[] {(V)"exit", (V)"exit"}, new Results(V.Nil));
var obj_414 = v_400;
V key_415 = (V)"status";
R.Set(obj_414, key_415, values_413[0]);
v_387 = values_413[1];
v_389 = values_413[2];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"exit"), new Entry((V)"cells", R.Table(new Entry((V)1, v_400))), new Entry((V)"exit_id", R.Get(v_409, (V)"id")))));
} else if (R.Truth(R.And(R.And((V)(v_341 == (V)"move"), () => (V)(R.Get(v_409, (V)"controller") == (V)"door")), () => (V)(!R.Truth(R.Get(v_409, (V)"open")))))) {
var values_416 = R.Join(new V[] {(V)"door"}, new Results(R.Get(v_409, (V)"id")));
var obj_417 = v_400;
V key_418 = (V)"status";
R.Set(obj_417, key_418, values_416[0]);
v_390 = values_416[1];
} else if (R.Truth(R.And(R.And((V)(v_341 == (V)"move"), () => (V)(R.Get(v_409, (V)"controller") == (V)"review_vault")), () => (V)(!R.Truth(R.Get(v_409, (V)"opened")))))) {
var values_419 = R.Join(new V[] {(V)"vault"}, new Results(R.Get(v_409, (V)"id")));
var obj_420 = v_400;
V key_421 = (V)"status";
R.Set(obj_420, key_421, values_419[0]);
v_391 = values_419[1];
} else if (R.Truth(R.And(R.And(R.And(R.And((V)(v_341 == (V)"move"), () => (V)(R.Get(v_409, (V)"controller") == (V)"ai")), () => (V)(R.Get(v_409, (V)"faction") != R.Get(v_355, (V)"faction"))), () => R.Get(v_409, (V)"hp")), () => (V)(R.Get(v_409, (V)"hp") > (V)0.0d)))) {
var values_422 = R.Join(new V[] {(V)"enemy"}, new Results((V)true));
var obj_423 = v_400;
V key_424 = (V)"status";
R.Set(obj_423, key_424, values_422[0]);
v_388 = values_422[1];
} else {
var values_425 = R.Join(new V[] {(V)"occupied"}, new Results((V)"occupied"));
var obj_426 = v_400;
V key_427 = (V)"status";
R.Set(obj_426, key_427, values_425[0]);
v_387 = values_425[1];
}
var values_428 = R.Join(new V[] {}, new Results((V)true));
v_383 = values_428[0];
} else {
var values_429 = R.Join(new V[] {}, new Results((V)"open"));
var obj_430 = v_400;
V key_431 = (V)"status";
R.Set(obj_430, key_431, values_429[0]);
var values_432 = R.Join(new V[] {}, new Results((V)(v_382 + (V)1.0d)));
v_382 = values_432[0];
}
}
var values_433 = R.Join(new V[] {}, new Results(v_400));
var obj_434 = v_381;
V key_435 = (V)((V)R.Length(v_381) + (V)1.0d);
R.Set(obj_434, key_435, values_433[0]);
}
}
var values_436 = Results.Empty;
V v_437 = values_436[0];
V v_438 = values_436[1];
if (R.Truth(R.And((V)(v_341 == (V)"move"), () => (V)(v_382 == (V)1.0d)))) {
var values_439 = R.Join(new V[] {}, R.Call(R.Get(v_32, (V)"portal"), R.Join(new V[] {v_340, R.Get(R.Get(v_381, (V)1.0d), (V)"x")}, new Results(R.Get(R.Get(v_381, (V)1.0d), (V)"y")))));
v_437 = values_439[0];
if (R.Truth(v_437)) {
var values_440 = R.Join(new V[] {}, R.Call(R.Get(v_32, (V)"landing"), R.Join(new V[] {v_340}, new Results(v_437))));
v_438 = values_440[0];
v_387 = values_440[1];
if (R.Truth((V)(!R.Truth(v_438)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", v_387), new Entry((V)"cells", v_381))));
}
}
}
if (R.Truth((V)(v_382 > (V)0.0d))) {
var values_441 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_44, (V)"move_extra"), R.Join(new V[] {v_379, v_341}, new Results(v_381))).First + R.Call(R.Get(v_62, (V)"move_extra"), R.Join(new V[] {v_340, v_341}, new Results(v_381))).First)));
V v_442 = values_441[0];
if (R.Truth((V)(v_442 > (V)0.0d))) {
var values_443 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {(V)(v_361 + v_442)}, new Results(v_359))));
v_363 = values_443[0];
}
}
if (R.Truth(v_388)) {
var values_444 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {R.Get(R.Get(v_357, (V)"costs"), (V)"basic_attack")}, new Results(v_359))));
v_363 = values_444[0];
}
if (R.Truth(R.And(v_391, () => (V)(!R.Truth(R.And(R.Get(v_340, (V)"review_keys"), () => R.Get(R.Get(v_340, (V)"review_keys"), R.Get(v_377, (V)"floor_id")))))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"find the vault key"), new Entry((V)"cells", v_381), new Entry((V)"vault_id", v_391))));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", R.Or(R.Or(R.Or((V)(v_382 > (V)0.0d), () => (V)(v_388 == (V)true)), () => (V)(v_390 != V.Nil)), () => (V)(v_391 != V.Nil))), new Entry((V)"reason", R.Or(R.And(R.And(R.And(R.And((V)(v_382 == (V)0.0d), () => (V)(!R.Truth(v_388))), () => (V)(!R.Truth(v_390))), () => (V)(!R.Truth(v_391))), () => v_387), () => V.Nil)), new Entry((V)"cost", v_363), new Entry((V)"cells", v_381), new Entry((V)"distance", v_382), new Entry((V)"bump", v_388), new Entry((V)"chest_id", v_389), new Entry((V)"door_id", v_390), new Entry((V)"vault_id", v_391), new Entry((V)"stair", v_437), new Entry((V)"landing", v_438))));
}
var values_445 = R.Join(new V[] {}, new Results(R.Get(v_355, (V)"position")));
V v_446 = values_445[0];
var values_447 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_ability"), R.Join(new V[] {v_340, v_341, v_342, v_343, (V)true}, new Results((V)true))));
V v_448 = values_447[0];
V v_449 = values_447[1];
V v_450 = values_447[2];
V v_451 = values_447[3];
V v_452 = values_447[4];
V v_453 = values_447[5];
V v_454 = values_447[6];
if (R.Truth(R.And((V)(v_341 == (V)"sweeping_slash"), () => R.Or((V)(R.Get(v_357, (V)"id") == (V)"core:actor/shade"), () => (V)(R.Get(v_357, (V)"id") == (V)"core:actor/trapper"))))) {
var values_455 = R.Join(new V[] {}, new Results(R.Get(v_448, (V)1.0d)));
V v_456 = values_455[0];
var values_457 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_340))));
V v_458 = values_457[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_456)), () => (V)(!R.Truth(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_458, R.Get(v_456, (V)"x")}, new Results(R.Get(v_456, (V)"y")))).First))), () => R.Call(v_144, R.Join(new V[] {v_340, R.Get(v_446, (V)"floor_id"), R.Get(v_456, (V)"x"), R.Get(v_456, (V)"y"), R.Get(v_355, (V)"id")}, new Results((V)false))).First))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"needs an empty adjacent tile"), new Entry((V)"cells", v_448))));
}
}
if (R.Truth(R.And((V)(v_341 == (V)"sweeping_slash"), () => (V)(R.Get(v_357, (V)"id") == R.Get(v_24, (V)"id"))))) {
var values_462 = R.Join(new V[] {}, R.Call(R.Get(v_24, (V)"plant_preview"), R.Join(new V[] {v_340, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_340))).First, v_355, R.Get(v_448, (V)1.0d), R.Get(v_70, (V)"can_see"), R.Get(v_70, (V)"was_explored")}, new Results(new V((RuleFunction)(args_459 => {
V v_460 = args_459[0];
V v_461 = args_459[1];
return R.Join(new V[] {}, R.Call(v_144, R.Join(new V[] {v_340, R.Get(v_446, (V)"floor_id"), v_460, v_461, R.Get(v_355, (V)"id")}, new Results((V)false))));
return Results.Empty;
}))))));
V v_463 = values_462[0];
V v_464 = values_462[1];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", v_463), new Entry((V)"reason", v_464), new Entry((V)"cost", v_363), new Entry((V)"cells", v_448))));
}
if (R.Truth(R.And((V)(v_341 == (V)"sweeping_slash"), () => (V)(R.Get(v_357, (V)"id") == R.Get(v_22, (V)"id"))))) {
var values_465 = R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Or(R.Get(v_355, (V)"storm_charge"), () => (V)0.0d) > (V)0.0d), () => (V)(!R.Truth(R.Get(R.Get(v_340, (V)"class_special"), (V)"storm")))), () => (V)((V)R.Length(v_448) > (V)0.0d))));
V v_466 = values_465[0];
foreach (var row_467 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_448)))))) {
V v_468 = row_467[0];
V v_469 = row_467[1];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_70, (V)"can_see"), R.Join(new V[] {v_340, R.Get(v_446, (V)"floor_id"), R.Get(v_469, (V)"x")}, new Results(R.Get(v_469, (V)"y")))).First)))) {
var values_470 = R.Join(new V[] {}, new Results((V)false));
v_466 = values_470[0];
}
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", v_466), new Entry((V)"cost", v_363), new Entry((V)"cells", v_448), new Entry((V)"reason", R.Or(R.And((V)(!R.Truth(v_466)), () => (V)"needs charge, a visible clear cross and no pending discharge"), () => V.Nil)))));
}
if (R.Truth(R.And((V)(v_341 == (V)"sweeping_slash"), () => R.Or((V)(R.Get(v_357, (V)"id") == R.Get(v_20, (V)"spirit_id")), () => (V)(R.Get(v_357, (V)"id") == R.Get(v_20, (V)"tinker_id")))))) {
var values_471 = R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"preview"), R.Join(new V[] {v_340, v_355, v_448, v_12}, new Results(R.Get(v_70, (V)"can_see")))));
V v_472 = values_471[0];
V v_473 = values_471[1];
V v_474 = values_471[2];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", v_472), new Entry((V)"reason", v_473), new Entry((V)"cost", v_363), new Entry((V)"cells", v_448), new Entry((V)"companion_mode", v_474))));
}
if (R.Truth(R.And((V)(v_341 == (V)"sweeping_slash"), () => (V)(R.Get(v_357, (V)"id") == R.Get(v_18, (V)"id"))))) {
var values_475 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"preview"), R.Join(new V[] {v_340, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_340))).First, R.Get(R.Get(v_448, (V)1.0d), (V)"x")}, new Results(R.Get(R.Get(v_448, (V)1.0d), (V)"y")))));
V v_476 = values_475[0];
V v_477 = values_475[1];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", v_476), new Entry((V)"reason", v_477), new Entry((V)"cost", v_363), new Entry((V)"cells", v_448))));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)true), new Entry((V)"cost", v_363), new Entry((V)"cells", v_448), new Entry((V)"reflection", v_453), new Entry((V)"weapon_hint", v_454))));
return Results.Empty;
}));
R.Set(v_74, (V)"preview", new V((RuleFunction)(args_478 => {
V v_479 = args_478[0];
V v_480 = args_478[1];
V v_481 = args_478[2];
V v_482 = args_478[3];
V v_483 = args_478[4];
V v_484 = args_478[5];
if (R.Truth(R.And((V)(v_484 != V.Nil), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_484))).First != (V)"table")))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"invalid preview options"), new Entry((V)"cells", R.Table()))));
}
if (R.Truth(R.And(R.And(v_484, () => (V)(R.Get(v_484, (V)"vow") != V.Nil)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_484, (V)"vow")))).First != (V)"boolean")))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"invalid vow option"), new Entry((V)"cells", R.Table()))));
}
var values_485 = R.Join(new V[] {}, R.Call(v_338, R.Join(new V[] {v_479, v_480, v_481, v_482}, new Results(v_483))));
V v_486 = values_485[0];
var values_487 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_479))));
V v_488 = values_487[0];
if (R.Truth(R.And(R.Get(v_486, (V)"valid"), () => R.Call(R.Get(v_14, (V)"immediate_attack"), R.Join(new V[] {v_488, R.Or(R.And(R.Get(v_486, (V)"bump"), () => (V)"basic_attack"), () => v_480)}, new Results(v_486))).First))) {
var values_489 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"dice_power"), R.Join(new V[] {}, new Results(v_488))));
var obj_490 = v_486;
V key_491 = (V)"fate_power";
R.Set(obj_490, key_491, values_489[0]);
}
if (R.Truth(R.And(R.Get(v_486, (V)"valid"), () => R.Call(R.Get(v_4, (V)"has_pattern"), R.Join(new V[] {}, new Results(v_488))).First))) {
var values_492 = Results.Empty;
V v_493 = values_492[0];
V v_494 = values_492[1];
if (R.Truth(R.Or(R.Or((V)(v_480 == (V)"basic_attack"), () => (V)(v_480 == (V)"sweeping_slash")), () => R.Get(v_486, (V)"bump")))) {
var values_495 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_ability"), R.Join(new V[] {v_479, R.Or(R.And(R.Get(v_486, (V)"bump"), () => (V)"basic_attack"), () => v_480), v_481, v_482, (V)(!R.Truth(R.Get(v_486, (V)"bump")))}, new Results((V)true))));
V v_496 = values_495[0];
V v_497 = values_495[1];
V v_498 = values_495[2];
V v_499 = values_495[3];
var values_500 = R.Join(new V[] {}, new Results(v_499));
v_493 = values_500[0];
if (R.Truth(R.And((V)(v_480 == (V)"sweeping_slash"), () => (V)(R.Get(v_486, (V)"companion_mode") == (V)"strike")))) {
var values_501 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_20, (V)"owned"), R.Join(new V[] {v_479}, new Results((V)"spirit"))).First, (V)"position")));
v_494 = values_501[0];
}
}
R.Call(R.Get(v_4, (V)"preview_patterns"), R.Join(new V[] {v_479, v_488, v_480, v_486, v_493}, new Results(v_494)));
}
if (R.Truth(R.Or((V)(!R.Truth(R.And(v_484, () => R.Get(v_484, (V)"vow")))), () => (V)(!R.Truth(R.Get(v_486, (V)"valid")))))) {
return R.Join(new V[] {}, new Results(v_486));
}
var values_502 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"attack_vow"), R.Join(new V[] {}, new Results(v_488))));
V v_503 = values_502[0];
if (R.Truth((V)(!R.Truth(v_503)))) {
var values_504 = R.Join(new V[] {}, new Results((V)false));
var obj_505 = v_486;
V key_506 = (V)"valid";
R.Set(obj_505, key_506, values_504[0]);

var values_507 = R.Join(new V[] {}, new Results((V)"requires Pilgrim's Vow"));
var obj_508 = v_486;
V key_509 = (V)"reason";
R.Set(obj_508, key_509, values_507[0]);
} else if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_14, (V)"immediate_attack"), R.Join(new V[] {v_488, v_480}, new Results(v_486))).First)))) {
var values_510 = R.Join(new V[] {}, new Results((V)false));
var obj_511 = v_486;
V key_512 = (V)"valid";
R.Set(obj_511, key_512, values_510[0]);

var values_513 = R.Join(new V[] {}, new Results((V)"vow needs an immediate attack; movement and utility are free of HP cost"));
var obj_514 = v_486;
V key_515 = (V)"reason";
R.Set(obj_514, key_515, values_513[0]);
} else if (R.Truth((V)(R.Get(v_488, (V)"hp") <= R.Get(v_503, (V)"hp_cost")))) {
var values_516 = R.Join(new V[] {}, new Results((V)false));
var obj_517 = v_486;
V key_518 = (V)"valid";
R.Set(obj_517, key_518, values_516[0]);

var values_519 = R.Join(new V[] {}, R.Invoke((V)"vow costs %d HP and must leave 1 HP", "format", R.Join(new V[] {}, new Results(R.Get(v_503, (V)"hp_cost")))));
var obj_520 = v_486;
V key_521 = (V)"reason";
R.Set(obj_520, key_521, values_519[0]);
} else {
var values_522 = R.Join(new V[] {}, new Results(v_503));
var obj_523 = v_486;
V key_524 = (V)"vow";
R.Set(obj_523, key_524, values_522[0]);
var values_525 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_ability"), R.Join(new V[] {v_479, v_480, v_481, v_482, (V)true}, new Results((V)true))));
V v_526 = values_525[0];
V v_527 = values_525[1];
var values_528 = R.Join(new V[] {}, new Results((V)(v_527 + R.Get(v_503, (V)"bonus"))));
var obj_529 = v_486;
V key_530 = (V)"damage";
R.Set(obj_529, key_530, values_528[0]);
}
return R.Join(new V[] {}, new Results(v_486));
return Results.Empty;
})));
R.Set(v_74, (V)"set_boon_stacks", new V((RuleFunction)(args_531 => {
V v_532 = args_531[0];
V v_533 = args_531[1];
V v_534 = args_531[2];
V v_535 = args_531[3];
V v_536 = args_531[4];
var values_537 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_532}, new Results(v_533))).First}, new Results((V)"unknown boon owner"))));
V v_538 = values_537[0];
if (R.Truth(R.Get(v_532, (V)"meta"))) {
var values_539 = R.Join(new V[] {}, new Results((V)false));
var obj_540 = R.Get(v_532, (V)"meta");
V key_541 = (V)"eligible";
R.Set(obj_540, key_541, values_539[0]);
}
R.Call(R.Get(v_4, (V)"set_stacks"), R.Join(new V[] {v_538, v_534, v_535}, new Results(v_536)));
return Results.Empty;
})));
R.Set(v_74, (V)"chest_options", new V((RuleFunction)(args_542 => {
V v_543 = args_542[0];
V v_544 = args_542[1];
var values_545 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_543}, new Results(v_544))));
V v_546 = values_545[0];
if (R.Truth(R.Or((V)(!R.Truth(v_546)), () => (V)(R.Get(v_546, (V)"controller") != (V)"chest")))) {
return R.Join(new V[] {V.Nil}, new Results((V)"unknown chest"));
}
return R.Join(new V[] {}, R.Call(R.Get(v_36, (V)"chest_options"), R.Join(new V[] {v_543}, new Results(v_546))));
return Results.Empty;
})));
R.Set(v_74, (V)"interaction_options", new V((RuleFunction)(args_547 => {
V v_548 = args_547[0];
V v_549 = args_547[1];
V v_550 = args_547[2];
if (R.Truth(R.Call(R.Get(v_46, (V)"find"), R.Join(new V[] {v_548}, new Results(v_549))).First)) {
return R.Join(new V[] {}, R.Call(R.Get(v_46, (V)"options"), R.Join(new V[] {v_548}, new Results(v_549))));
}
var values_551 = R.Join(new V[] {}, R.Call(R.Get(v_52, (V)"options"), R.Join(new V[] {v_548, v_549}, new Results(v_550))));
V v_552 = values_551[0];
V v_553 = values_551[1];
if (R.Truth((V)(!R.Truth(v_552)))) {
return R.Join(new V[] {V.Nil}, new Results(v_553));
}
foreach (var row_554 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_552, (V)"entries"))))))) {
V v_555 = row_554[0];
V v_556 = row_554[1];
if (R.Truth(R.And(R.Get(v_556, (V)"remove"), () => R.Get(v_556, (V)"enabled")))) {
var values_557 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_548))));
V v_558 = values_557[0];

var values_559 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_558))));
V v_560 = values_559[0];

var values_561 = R.Join(new V[] {}, new Results(R.Get(v_556, (V)"remove")));
V v_562 = values_561[0];
R.Call(R.Get(v_4, (V)"set_stacks"), R.Join(new V[] {v_560, R.Get(v_562, (V)"id"), R.Get(v_562, (V)"rarity")}, new Results((V)(R.Get(R.Get(R.Get(v_560, (V)"boons"), R.Get(v_562, (V)"id")), R.Get(v_562, (V)"rarity")) - (V)1.0d))));
if (R.Truth(R.And((V)(!R.Truth(R.Get(R.Get(v_560, (V)"boons"), R.Get(v_562, (V)"id")))), () => R.Get(v_560, (V)"boon_memory")))) {
var values_563 = R.Join(new V[] {}, new Results(V.Nil));
var obj_564 = R.Get(v_560, (V)"boon_memory");
V key_565 = R.Get(v_562, (V)"id");
R.Set(obj_564, key_565, values_563[0]);
}
if (R.Truth(R.Get(v_556, (V)"offer"))) {
var values_566 = R.Join(new V[] {}, new Results(R.Get(v_556, (V)"offer")));
V v_567 = values_566[0];

R.Call(R.Get(v_4, (V)"set_stacks"), R.Join(new V[] {v_560, R.Get(v_567, (V)"boon_id"), R.Get(v_567, (V)"rarity")}, new Results((V)(R.Or(R.And(R.Get(R.Get(v_560, (V)"boons"), R.Get(v_567, (V)"boon_id")), () => R.Get(R.Get(R.Get(v_560, (V)"boons"), R.Get(v_567, (V)"boon_id")), R.Get(v_567, (V)"rarity"))), () => (V)0.0d) + (V)1.0d))));
}
var values_568 = R.Join(new V[] {}, new Results(R.Table()));
V v_569 = values_568[0];
foreach (var row_570 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"basic_attack"), new Entry((V)2, (V)"sweeping_slash")))))))) {
V v_571 = row_570[0];
V v_572 = row_570[1];
var values_573 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_ability"), R.Join(new V[] {v_548, v_572, (V)1.0d, (V)0.0d, (V)true}, new Results((V)true))));
V v_574 = values_573[0];
V v_575 = values_573[1];
var values_576 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_ability"), R.Join(new V[] {v_558, v_572, (V)1.0d, (V)0.0d, (V)true}, new Results((V)true))));
V v_577 = values_576[0];
V v_578 = values_576[1];
var values_579 = R.Join(new V[] {}, new Results(R.Concat(R.Or(R.And((V)(v_572 == (V)"basic_attack"), () => (V)"F"), () => (V)"R"), R.Invoke((V)" east: %d\u00e2\u0086\u0092%d cells, %d\u00e2\u0086\u0092%d damage", "format", R.Join(new V[] {(V)R.Length(v_574), (V)R.Length(v_577), v_575}, new Results(v_578))).First)));
var obj_580 = v_569;
V key_581 = (V)((V)R.Length(v_569) + (V)1.0d);
R.Set(obj_580, key_581, values_579[0]);
}
var values_582 = R.Join(new V[] {}, new Results(R.Concat(R.Or(R.Get(v_556, (V)"detail"), () => (V)""), R.Concat((V)" ", R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_569}, new Results((V)"; "))).First, (V)". Automatic stats/HP and committed effects are preserved.")))));
var obj_583 = v_556;
V key_584 = (V)"detail";
R.Set(obj_583, key_584, values_582[0]);
}
}
return R.Join(new V[] {}, new Results(v_552));
return Results.Empty;
})));
R.Set(v_74, (V)"nearby_interactions", new V((RuleFunction)(args_585 => {
V v_586 = args_585[0];
var values_587 = R.Join(new V[] {}, R.Call(R.Get(v_52, (V)"nearby"), R.Join(new V[] {}, new Results(v_586))));
V v_588 = values_587[0];
foreach (var row_589 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_46, (V)"nearby"), R.Join(new V[] {}, new Results(v_586)))))))) {
V v_590 = row_589[0];
V v_591 = row_589[1];
var values_592 = R.Join(new V[] {}, new Results(v_591));
var obj_593 = v_588;
V key_594 = (V)((V)R.Length(v_588) + (V)1.0d);
R.Set(obj_593, key_594, values_592[0]);
}
return R.Join(new V[] {}, new Results(v_588));
return Results.Empty;
})));
R.Set(v_74, (V)"utility_options", new V((RuleFunction)(args_595 => {
V v_596 = args_595[0];
return R.Join(new V[] {}, R.Call(R.Get(v_52, (V)"utility_options"), R.Join(new V[] {}, new Results(v_596))));
return Results.Empty;
})));
R.Set(v_74, (V)"route_options", new V((RuleFunction)(args_597 => {
V v_598 = args_597[0];
V v_599 = args_597[1];
var values_600 = R.Join(new V[] {}, R.Call(R.Get(v_40, (V)"route_options"), R.Join(new V[] {v_598}, new Results(v_599))));
V v_601 = values_600[0];
V v_602 = values_600[1];
return R.Join(new V[] {R.Or(R.And(v_601, () => R.Call(v_84, R.Join(new V[] {}, new Results(v_601))).First), () => V.Nil)}, new Results(v_602));
return Results.Empty;
})));
R.Set(v_74, (V)"next_level_xp", new V((RuleFunction)(args_603 => {
V v_604 = args_603[0];
return R.Join(new V[] {}, R.Call(R.Get(v_36, (V)"next_threshold"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_604, (V)"progression"), (V)"level")))));
return Results.Empty;
})));
R.Set(v_74, (V)"effective_ability", new V((RuleFunction)(args_605 => {
V v_606 = args_605[0];
V v_607 = args_605[1];
V v_608 = args_605[2];
V v_609 = args_605[3];
V v_610 = args_605[4];
V v_611 = args_605[5];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(v_607 == (V)"basic_attack"), () => (V)(v_607 == (V)"sweeping_slash"))}, new Results((V)"unknown ability")));
var values_612 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_606))));
V v_613 = values_612[0];
var values_614 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"attack"), R.Join(new V[] {v_606, v_613, v_607, v_608, v_609, v_12}, new Results(v_610))));
V v_615 = values_614[0];
V v_616 = values_614[1];
V v_617 = values_614[2];
V v_618 = values_614[3];
R.Call(rt.Global("assert"), R.Join(new V[] {v_615}, new Results((V)"invalid ability direction")));
var values_619 = Results.Empty;
V v_620 = values_619[0];
var values_621 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"route_before_form"), R.Join(new V[] {v_613, v_616}, new Results(v_610))));
V v_622 = values_621[0];
if (R.Truth(v_622)) {
var values_623 = R.Join(new V[] {}, R.Call(R.Get(v_60, (V)"route"), R.Join(new V[] {v_606, v_613, v_615, v_622}, new Results(v_611))));
v_615 = values_623[0];
v_620 = values_623[1];
}
var values_624 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"ability"), R.Join(new V[] {v_606, v_613, v_616, v_615, (V)(R.Or(R.And((V)(v_607 == (V)"basic_attack"), () => R.Get(v_613, (V)"damage")), () => R.Get(v_613, (V)"slash_damage")) + R.Call(R.Get(v_16, (V)"bonus_for"), R.Join(new V[] {v_613}, new Results(v_607))).First), v_608, v_609, v_610, v_617}, new Results(v_611))));
V v_625 = values_624[0];
V v_626 = values_624[1];
V v_627 = values_624[2];
V v_628 = values_624[3];
var values_629 = R.Join(new V[] {}, new Results(v_625));
V v_630 = values_629[0];
if (R.Truth(R.And(R.And((V)(!R.Truth(v_622)), () => (V)(v_616 != (V)"core:ability/hookrunner_hook")), () => (V)(v_616 != (V)"core:ability/duelist_thrust")))) {
var values_631 = R.Join(new V[] {}, R.Call(R.Get(v_60, (V)"route"), R.Join(new V[] {v_606, v_613, v_625}, new Results(v_627))));
v_630 = values_631[0];
v_620 = values_631[1];
}
return R.Join(new V[] {v_630, (V)(v_626 + R.Call(R.Get(v_52, (V)"focus_bonus"), R.Join(new V[] {}, new Results(v_613))).First), v_616, v_627, v_618, v_620}, new Results(v_628));
return Results.Empty;
})));
V v_632 = V.Nil;
v_632 = new V((RuleFunction)(args_633 => {
V v_634 = args_633[0];
V v_635 = args_633[1];
V v_636 = args_633[2];
V v_637 = args_633[3];
V v_638 = args_633[4];
V v_639 = args_633[5];
R.Call(R.Get(v_58, (V)"interrupt"), R.Join(new V[] {}, new Results(v_635)));
var values_640 = R.Join(new V[] {}, new Results(R.Get(v_635, (V)"pending_attack")));
V v_641 = values_640[0];
if (R.Truth((V)(!R.Truth(v_641)))) {
return R.Join(new V[] {}, new Results((V)false));
}
R.Call(R.Get(v_38, (V)"remove_for_actor"), R.Join(new V[] {v_634, R.Get(v_635, (V)"id")}, new Results((V)"impact")));
var values_642 = R.Join(new V[] {}, new Results(V.Nil));
var obj_643 = v_635;
V key_644 = (V)"pending_attack";
R.Set(obj_643, key_644, values_642[0]);
R.Call(R.Get(v_56, (V)"prune_corpses"), R.Join(new V[] {}, new Results(v_634)));
R.Call(v_95, R.Join(new V[] {v_634, v_636, (V)"WindupCancelled", R.Table(new Entry((V)"target_id", R.Get(v_635, (V)"id")), new Entry((V)"windup_id", R.Get(v_641, (V)"schedule_id")), new Entry((V)"reason", v_637), new Entry((V)"target_cells", R.Get(v_641, (V)"target_cells"))), v_638}, new Results(v_639)));
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
R.Set(v_74, (V)"shove_landing", new V((RuleFunction)(args_645 => {
V v_646 = args_645[0];
V v_647 = args_645[1];
V v_648 = args_645[2];
V v_649 = args_645[3];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_648))).First != (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_649))).First != (V)"number")), () => (V)((V)(v_648 % (V)1.0d) != (V)0.0d)), () => (V)((V)(v_649 % (V)1.0d) != (V)0.0d)), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_648))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_649))).First) != (V)1.0d)))) {
return R.Join(new V[] {V.Nil}, new Results((V)"invalid direction"));
}
var values_650 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_646}, new Results(v_647))));
V v_651 = values_650[0];
if (R.Truth(R.Or(R.Or(R.Or((V)(!R.Truth(v_651)), () => (V)(!R.Truth(R.Get(v_651, (V)"hp")))), () => (V)(R.Get(v_651, (V)"hp") <= (V)0.0d)), () => R.And((V)(R.Get(v_651, (V)"controller") != (V)"ai"), () => (V)(R.Get(v_651, (V)"controller") != (V)"player"))))) {
return R.Join(new V[] {V.Nil}, new Results((V)"invalid target"));
}
if (R.Truth(R.Get(v_651, (V)"invulnerable"))) {
return R.Join(new V[] {V.Nil}, new Results((V)"immune"));
}
var values_652 = R.Join(new V[] {}, new Results(R.Get(v_651, (V)"position")));
V v_653 = values_652[0];
if (R.Truth((V)(R.Get(v_653, (V)"floor_id") != R.Get(v_646, (V)"active_floor_id")))) {
return R.Join(new V[] {V.Nil}, new Results((V)"inactive floor"));
}
var values_654 = R.Join(new V[] {(V)(R.Get(v_653, (V)"x") + v_648)}, new Results((V)(R.Get(v_653, (V)"y") + v_649)));
V v_655 = values_654[0];
V v_656 = values_654[1];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_646))).First, v_655}, new Results(v_656))).First)))) {
return R.Join(new V[] {V.Nil}, new Results((V)"wall"));
}
if (R.Truth(R.Call(R.Get(v_32, (V)"portal"), R.Join(new V[] {v_646, v_655}, new Results(v_656))).First)) {
return R.Join(new V[] {V.Nil}, new Results((V)"stairs"));
}
foreach (var row_657 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_646, (V)"entities"))))))) {
V v_658 = row_657[0];
V v_659 = row_657[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_659, (V)"controller") == (V)"exit"), () => (V)(R.Get(R.Get(v_659, (V)"position"), (V)"floor_id") == R.Get(v_653, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_659, (V)"position"), (V)"x") == v_655)), () => (V)(R.Get(R.Get(v_659, (V)"position"), (V)"y") == v_656)))) {
return R.Join(new V[] {V.Nil}, new Results((V)"exit"));
}
}
if (R.Truth(R.Call(v_144, R.Join(new V[] {v_646, R.Get(v_653, (V)"floor_id"), v_655, v_656, R.Get(v_651, (V)"id")}, new Results((V)false))).First)) {
return R.Join(new V[] {V.Nil}, new Results((V)"occupied"));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_653, (V)"floor_id")), new Entry((V)"x", v_655), new Entry((V)"y", v_656))));
return Results.Empty;
})));
V v_660 = V.Nil;
v_660 = new V((RuleFunction)(args_661 => {
V v_662 = args_661[0];
V v_663 = args_661[1];
V v_664 = args_661[2];
V v_665 = args_661[3];
V v_666 = args_661[4];
V v_667 = args_661[5];
V v_668 = args_661[6];
var values_669 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"shove_landing"), R.Join(new V[] {v_662, R.Get(v_664, (V)"id"), v_665}, new Results(v_666))));
V v_670 = values_669[0];
V v_671 = values_669[1];
if (R.Truth((V)(!R.Truth(v_670)))) {
R.Call(v_95, R.Join(new V[] {v_662, v_663, (V)"ShoveBlocked", R.Table(new Entry((V)"target_id", R.Get(v_664, (V)"id")), new Entry((V)"reason", v_671), new Entry((V)"floor_id", R.Get(R.Get(v_664, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_664, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_664, (V)"position"), (V)"y"))), v_667}, new Results(v_668)));
return R.Join(new V[] {}, new Results((V)false));
}
var values_672 = R.Join(new V[] {}, R.Call(v_632, R.Join(new V[] {v_662, v_664, v_663, (V)"displacement", v_667}, new Results(v_668))));
V v_673 = values_672[0];
var values_674 = R.Join(new V[] {R.Get(R.Get(v_664, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_664, (V)"position"), (V)"y")));
V v_675 = values_674[0];
V v_676 = values_674[1];
var values_677 = R.Join(new V[] {R.Get(v_670, (V)"x")}, new Results(R.Get(v_670, (V)"y")));
var obj_678 = R.Get(v_664, (V)"position");
V key_679 = (V)"x";
var obj_680 = R.Get(v_664, (V)"position");
V key_681 = (V)"y";
R.Set(obj_678, key_679, values_677[0]);
R.Set(obj_680, key_681, values_677[1]);
R.Call(v_95, R.Join(new V[] {v_662, v_663, (V)"Displaced", R.Table(new Entry((V)"target_id", R.Get(v_664, (V)"id")), new Entry((V)"reason", (V)"shove"), new Entry((V)"floor_id", R.Get(v_670, (V)"floor_id")), new Entry((V)"from_x", v_675), new Entry((V)"from_y", v_676), new Entry((V)"x", R.Get(v_670, (V)"x")), new Entry((V)"y", R.Get(v_670, (V)"y"))), v_667}, new Results(v_668)));
R.Call(v_95, R.Join(new V[] {v_662, v_663, (V)"TileEntered", R.Table(new Entry((V)"floor_id", R.Get(v_670, (V)"floor_id")), new Entry((V)"from_x", v_675), new Entry((V)"from_y", v_676), new Entry((V)"x", R.Get(v_670, (V)"x")), new Entry((V)"y", R.Get(v_670, (V)"y")), new Entry((V)"forced", (V)true)), R.Get(v_664, (V)"id")}, new Results(v_668)));
if (R.Truth((V)(R.Get(v_664, (V)"id") == R.Get(v_662, (V)"player_id")))) {
R.Call(R.Get(v_36, (V)"pickup_at"), R.Join(new V[] {v_662, v_664, R.Get(v_670, (V)"x"), R.Get(v_670, (V)"y")}, new Results(new V((RuleFunction)(args_682 => {
V v_683 = args_682[0];
V v_684 = args_682[1];
V v_685 = args_682[2];
R.Call(v_95, R.Join(new V[] {v_662, v_663, v_683, v_684, v_685}, new Results(v_668)));
return Results.Empty;
})))));
} else if (R.Truth(v_673)) {
var values_686 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_664, (V)"definition_id")))))));
V v_687 = values_686[0];
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_662}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"actor_id", R.Get(v_664, (V)"id")), new Entry((V)"is_player", (V)false), new Entry((V)"due", (V)(R.Get(v_662, (V)"clock") + R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {R.Get(v_687, (V)"recovery")}, new Results(R.Get(v_664, (V)"speed")))).First))))));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
V v_688 = V.Nil;
v_688 = new V((RuleFunction)(args_689 => {
V v_690 = args_689[0];
V v_691 = args_689[1];
V v_692 = args_689[2];
V v_693 = args_689[3];
V v_694 = args_689[4];
if (R.Truth(R.Call(rt.Global("_roag_downed"), R.Join(new V[] {v_690}, new Results(v_691))).First)) {
return Results.Empty;
}
R.Call(R.Get(v_56, (V)"release_drop"), R.Join(new V[] {v_690, v_691}, new Results(new V((RuleFunction)(args_695 => {
V v_696 = args_695[0];
V v_697 = args_695[1];
R.Call(v_95, R.Join(new V[] {v_690, v_692, v_696, v_697, R.Get(v_691, (V)"id")}, new Results(v_694)));
return Results.Empty;
})))));
R.Call(R.Get(v_56, (V)"remember_corpse"), R.Join(new V[] {v_690}, new Results(v_691)));
if (R.Truth((V)(R.Get(v_691, (V)"id") == R.Get(v_690, (V)"player_id")))) {
var values_698 = R.Join(new V[] {}, new Results((V)true));
var obj_699 = v_690;
V key_700 = (V)"game_over";
R.Set(obj_699, key_700, values_698[0]);
R.Call(R.Get(v_38, (V)"remove_for_actor"), R.Join(new V[] {v_690}, new Results(R.Get(v_691, (V)"id"))));
return Results.Empty;
}
R.Call(v_632, R.Join(new V[] {v_690, v_691, v_692, (V)"death", v_693}, new Results(v_694)));
R.Call(R.Get(v_38, (V)"remove_for_actor"), R.Join(new V[] {v_690}, new Results(R.Get(v_691, (V)"id"))));
foreach (var row_701 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_690, (V)"entities"))))))) {
V v_702 = row_701[0];
V v_703 = row_701[1];
if (R.Truth((V)(R.Get(v_703, (V)"id") == R.Get(v_691, (V)"id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_690, (V)"entities")}, new Results(v_702)));

break;
}
}
return Results.Empty;
}));
V v_704 = V.Nil;
v_704 = new V((RuleFunction)(args_705 => {
V v_706 = args_705[0];
V v_707 = args_705[1];
V v_708 = args_705[2];
V v_709 = args_705[3];
V v_710 = args_705[4];
V v_711 = args_705[5];
V v_712 = args_705[6];
V v_713 = args_705[7];
V v_714 = args_705[8];
V v_715 = args_705[9];
V v_716 = args_705[10];
V v_717 = args_705[11];
var values_718 = R.Join(new V[] {}, new Results(R.Table()));
V v_719 = values_718[0];
var values_720 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_706))));
V v_721 = values_720[0];
var values_722 = R.Join(new V[] {}, new Results(R.Or(R.And(R.And(v_715, () => R.Call(R.Get(v_4, (V)"has_pattern"), R.Join(new V[] {}, new Results(v_708))).First), () => R.Get(v_708, (V)"position")), () => V.Nil)));
V v_723 = values_722[0];
foreach (var row_724 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_710)))))) {
V v_725 = row_724[0];
V v_726 = row_724[1];
var values_727 = R.Join(new V[] {}, new Results(R.Or((V)(!R.Truth(v_712)), () => R.Call(R.Get(v_12, (V)"projectile_passable"), R.Join(new V[] {v_721, R.Get(v_726, (V)"x"), R.Get(v_726, (V)"y")}, new Results(v_706))).First)));
V v_728 = values_727[0];
if (R.Truth(R.And((V)(!R.Truth(v_728)), () => (V)(v_712 != (V)"area")))) {
break;
}
var values_729 = R.Join(new V[] {}, new Results(R.Or(R.And(v_728, () => R.Call(v_144, R.Join(new V[] {v_706, R.Get(v_726, (V)"floor_id"), R.Get(v_726, (V)"x"), R.Get(v_726, (V)"y"), R.Or(R.Get(v_708, (V)"collision_id"), () => R.Get(v_708, (V)"id"))}, new Results((V)true))).First), () => V.Nil)));
V v_730 = values_729[0];
if (R.Truth(v_730)) {
var values_731 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_732 = values_731[0];
V v_733 = values_731[1];
foreach (var row_734 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_706, (V)"entities"))))))) {
V v_735 = row_734[0];
V v_736 = row_734[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_736, (V)"id") != R.Get(v_730, (V)"id")), () => R.Get(v_736, (V)"hp")), () => (V)(R.Get(v_736, (V)"hp") > (V)0.0d)), () => (V)(R.Get(R.Get(v_736, (V)"position"), (V)"floor_id") == R.Get(v_726, (V)"floor_id"))), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_736, (V)"position"), (V)"x") - R.Get(v_726, (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_736, (V)"position"), (V)"y") - R.Get(v_726, (V)"y"))))))).First == (V)1.0d)))) {
if (R.Truth((V)(R.Get(v_736, (V)"faction") != R.Get(v_708, (V)"faction")))) {
var values_737 = R.Join(new V[] {}, new Results((V)(v_732 + (V)1.0d)));
v_732 = values_737[0];
}
if (R.Truth(R.And(R.And((V)(R.Get(v_736, (V)"faction") == R.Get(v_730, (V)"faction")), () => R.Get(v_736, (V)"statuses")), () => R.Get(R.Get(v_736, (V)"statuses"), (V)"burning")))) {
var values_738 = R.Join(new V[] {}, new Results((V)(v_733 + (V)1.0d)));
v_733 = values_738[0];
}
}
}
var values_739 = R.Join(new V[] {}, new Results((V)0.0d));
V v_740 = values_739[0];
if (R.Truth(R.And(R.And(R.And(R.And(v_715, () => (V)(R.Get(v_708, (V)"id") == R.Get(v_706, (V)"player_id"))), () => (V)(R.Get(v_708, (V)"reversal_target_id") == R.Get(v_730, (V)"id"))), () => R.Get(v_708, (V)"boons")), () => R.Get(R.Get(v_708, (V)"boons"), (V)"core:boon/ring_of_reversal")))) {
var values_741 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"aggregate"), R.Join(new V[] {(V)"core:boon/ring_of_reversal", R.Get(R.Get(v_708, (V)"boons"), (V)"core:boon/ring_of_reversal")}, new Results((V)"power"))));
v_740 = values_741[0];
var values_742 = R.Join(new V[] {}, new Results(V.Nil));
var obj_743 = v_708;
V key_744 = (V)"reversal_target_id";
R.Set(obj_743, key_744, values_742[0]);
}
var values_745 = R.Join(new V[] {}, new Results(R.And(R.And(v_717, () => (V)(R.Get(v_717, (V)"face") == (V)7.0d)), () => R.Call(R.Get(v_4, (V)"skull_target"), R.Join(new V[] {}, new Results(v_730))).First)));
V v_746 = values_745[0];
var values_747 = R.Join(new V[] {}, new Results((V)(R.Or(R.And(v_746, () => R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_711}, new Results((V)(R.Get(v_730, (V)"hp") + R.Or(R.Get(v_730, (V)"defense"), () => (V)0.0d))))).First), () => v_711) + v_740)));
V v_748 = values_747[0];
var values_749 = R.Join(new V[] {}, R.Call(R.Get(v_54, (V)"block"), R.Join(new V[] {v_706, v_730, v_708}, new Results(v_715))));
V v_750 = values_749[0];
V v_751 = values_749[1];
var values_752 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Or(R.Get(v_730, (V)"invulnerable"), () => v_750), () => (V)0.0d), () => R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(v_748 - R.Or(R.Get(v_730, (V)"defense"), () => (V)0.0d))))).First)));
V v_753 = values_752[0];
var values_754 = R.Join(new V[] {}, R.Call(R.Get(v_16, (V)"parry"), R.Join(new V[] {v_706, v_730, v_708, v_715, v_712}, new Results(v_753))));
V v_755 = values_754[0];
if (R.Truth(v_755)) {
var values_756 = R.Join(new V[] {}, new Results((V)0.0d));
v_753 = values_756[0];

var values_757 = R.Join(new V[] {}, new Results((V)"parry"));
v_750 = values_757[0];
}
if (R.Truth(R.Call(R.Get(v_22, (V)"absorb"), R.Join(new V[] {v_730}, new Results(v_753))).First)) {
var values_758 = R.Join(new V[] {}, new Results((V)0.0d));
v_753 = values_758[0];

var values_759 = R.Join(new V[] {}, new Results((V)"grounded ward"));
v_750 = values_759[0];
}
var values_760 = R.Join(new V[] {}, new Results((V)false));
V v_761 = values_760[0];
if (R.Truth(R.And(R.And((V)(R.Get(v_730, (V)"id") == R.Get(v_706, (V)"player_id")), () => (V)(v_753 > (V)0.0d)), () => (V)(R.Or(R.Get(v_730, (V)"pickup_ward_actions"), () => (V)0.0d) > (V)0.0d)))) {
var values_762 = R.Join(new V[] {}, new Results((V)true));
v_761 = values_762[0];

var values_763 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_764 = v_730;
V key_765 = (V)"pickup_ward_actions";
R.Set(obj_764, key_765, values_763[0]);
var values_766 = R.Join(new V[] {}, new Results((V)0.0d));
v_753 = values_766[0];
}
var values_767 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"consume_ward"), R.Join(new V[] {v_706, v_730}, new Results(v_753))));
V v_768 = values_767[0];
var values_769 = R.Join(new V[] {}, new Results((V)(v_753 - v_768)));
v_753 = values_769[0];
var values_770 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_730, (V)"hp")}, new Results(v_753))));
V v_771 = values_770[0];
var values_772 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"target_id", R.Get(v_730, (V)"id")), new Entry((V)"target_definition_id", R.Get(v_730, (V)"definition_id")), new Entry((V)"target_faction", R.Get(v_730, (V)"faction")), new Entry((V)"target_tags", R.Table(new Entry((V)1, R.Get(v_730, (V)"definition_id")), new Entry((V)2, R.Get(v_730, (V)"faction")))), new Entry((V)"floor_id", R.Get(v_726, (V)"floor_id")), new Entry((V)"x", R.Get(v_726, (V)"x")), new Entry((V)"y", R.Get(v_726, (V)"y")), new Entry((V)"damage_type", R.Or(v_713, () => (V)"physical")), new Entry((V)"ability_id", v_716), new Entry((V)"critical", R.Or(v_714, () => (V)false)), new Entry((V)"attempted_damage", v_748), new Entry((V)"after_defense", v_753), new Entry((V)"reversal_bonus", R.Or(R.And((V)(v_740 > (V)0.0d), () => v_740), () => V.Nil)), new Entry((V)"ward_absorbed", v_768), new Entry((V)"pickup_ward", R.Or(v_761, () => V.Nil)), new Entry((V)"parried", R.Or(v_755, () => V.Nil)), new Entry((V)"block_reason", v_750), new Entry((V)"protecting_id", v_751), new Entry((V)"hp_before", R.Get(v_730, (V)"hp")), new Entry((V)"hp_after", (V)(R.Get(v_730, (V)"hp") - v_771)), new Entry((V)"actual_loss", v_771), new Entry((V)"target_max_hp", R.Get(v_730, (V)"max_hp")), new Entry((V)"direct_attack", R.Or(v_715, () => (V)false)), new Entry((V)"pattern_origin", R.Call(v_84, R.Join(new V[] {}, new Results(v_723))).First), new Entry((V)"wall_backed", R.Or(R.And(R.And(R.And(v_715, () => R.Get(v_708, (V)"boons")), () => R.Get(R.Get(v_708, (V)"boons"), (V)"core:boon/stone_backstop")), () => R.Call(R.Get(v_4, (V)"backstop"), R.Join(new V[] {v_706, R.Or(R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_706}, new Results(R.Get(v_708, (V)"id")))).First, () => v_708)}, new Results(v_726))).First), () => V.Nil)), new Entry((V)"reward_exempt", R.Or(R.Get(v_730, (V)"reward_exempt"), () => V.Nil)), new Entry((V)"fate_face", R.Or(R.And(v_717, () => R.Get(v_717, (V)"face")), () => V.Nil)), new Entry((V)"fate_skull", R.Or(v_746, () => V.Nil)), new Entry((V)"fate_skull_resisted", R.Or(R.And(R.And(v_717, () => (V)(R.Get(v_717, (V)"face") == (V)7.0d)), () => (V)(!R.Truth(v_746))), () => V.Nil)), new Entry((V)"source_hp_at_attack", R.Get(v_708, (V)"hp")), new Entry((V)"source_max_hp_at_attack", R.Get(v_708, (V)"max_hp")), new Entry((V)"adjacent_hostile_count", v_732), new Entry((V)"target_burning", R.Or(R.And(R.Get(v_730, (V)"statuses"), () => (V)(R.Get(R.Get(v_730, (V)"statuses"), (V)"burning") != V.Nil)), () => (V)false)), new Entry((V)"adjacent_burning_count", v_733))));
var obj_773 = v_719;
V key_774 = (V)((V)R.Length(v_719) + (V)1.0d);
R.Set(obj_773, key_774, values_772[0]);
if (R.Truth((V)(v_712 == (V)true))) {
break;
}
}
}
R.Call(R.Get(v_68, (V)"redistribute"), R.Join(new V[] {v_706}, new Results(v_719)));
foreach (var row_775 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_719)))))) {
V v_776 = row_775[0];
V v_777 = row_775[1];
var values_778 = R.Join(new V[] {}, new Results(R.Get(v_777, (V)"hp_after")));
var obj_779 = R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_706}, new Results(R.Get(v_777, (V)"target_id")))).First;
V key_780 = (V)"hp";
R.Set(obj_779, key_780, values_778[0]);
}
if (R.Truth((V)((V)R.Length(v_719) > (V)0.0d))) {
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"DamageBatchResolved", R.Table(new Entry((V)"outcomes", v_719)), R.Get(v_708, (V)"id")}, new Results(v_709)));
}
foreach (var row_781 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_719)))))) {
V v_782 = row_781[0];
V v_783 = row_781[1];
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"DamageAttempted", v_783, R.Get(v_708, (V)"id")}, new Results(v_709)));
if (R.Truth(R.And((V)(R.Get(v_783, (V)"fate_face") == (V)7.0d), () => R.Or(R.Get(v_783, (V)"fate_skull_resisted"), () => (V)(R.Get(v_783, (V)"actual_loss") < R.Get(v_783, (V)"hp_before")))))) {
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"FateSkullResisted", R.Table(new Entry((V)"target_id", R.Get(v_783, (V)"target_id")), new Entry((V)"floor_id", R.Get(v_783, (V)"floor_id")), new Entry((V)"x", R.Get(v_783, (V)"x")), new Entry((V)"y", R.Get(v_783, (V)"y")), new Entry((V)"reason", R.Or(R.Or(R.And(R.Get(v_783, (V)"fate_skull_resisted"), () => (V)"regular enemies only"), () => R.Get(v_783, (V)"block_reason")), () => (V)"ward absorbed the hit"))), R.Get(v_708, (V)"id")}, new Results(v_709)));
}
if (R.Truth(R.Get(v_783, (V)"reversal_bonus"))) {
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"RingReversalSpent", R.Table(new Entry((V)"target_id", R.Get(v_783, (V)"target_id")), new Entry((V)"bonus", R.Get(v_783, (V)"reversal_bonus")), new Entry((V)"floor_id", R.Get(v_783, (V)"floor_id")), new Entry((V)"x", R.Get(v_783, (V)"x")), new Entry((V)"y", R.Get(v_783, (V)"y"))), R.Get(v_708, (V)"id")}, new Results(v_709)));
}
if (R.Truth(R.Get(v_783, (V)"parried"))) {
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"DuelistParried", R.Table(new Entry((V)"target_id", R.Get(v_708, (V)"id")), new Entry((V)"floor_id", R.Get(v_783, (V)"floor_id")), new Entry((V)"x", R.Get(v_783, (V)"x")), new Entry((V)"y", R.Get(v_783, (V)"y")), new Entry((V)"bonus", R.Get(v_16, (V)"bonus"))), R.Get(v_783, (V)"target_id")}, new Results(v_709)));
}
if (R.Truth(R.Get(v_783, (V)"pickup_ward"))) {
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"WardFlaskBlocked", R.Table(new Entry((V)"floor_id", R.Get(v_783, (V)"floor_id")), new Entry((V)"x", R.Get(v_783, (V)"x")), new Entry((V)"y", R.Get(v_783, (V)"y")), new Entry((V)"target_id", R.Get(v_783, (V)"target_id"))), R.Get(v_708, (V)"id")}, new Results(v_709)));
}
if (R.Truth((V)(R.Get(v_783, (V)"actual_loss") > (V)0.0d))) {
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"DamageTaken", v_783, R.Get(v_708, (V)"id")}, new Results(v_709)));
var values_784 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_706}, new Results(R.Get(v_783, (V)"target_id")))));
V v_785 = values_784[0];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_783, (V)"hp_after") > (V)0.0d), () => v_785), () => (V)(R.Get(R.Get(v_48, (V)"roles"), R.Get(v_785, (V)"definition_id")) == (V)"retaliator")), () => (V)(R.Get(v_708, (V)"faction") != R.Get(v_785, (V)"faction"))))) {
var values_786 = R.Join(new V[] {}, new Results((V)true));
var obj_787 = R.Get(v_785, (V)"ai_memory");
V key_788 = (V)"retaliate";
R.Set(obj_787, key_788, values_786[0]);
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"RetaliationReadied", R.Table(new Entry((V)"target_id", R.Get(v_785, (V)"id")), new Entry((V)"floor_id", R.Get(v_783, (V)"floor_id")), new Entry((V)"x", R.Get(v_783, (V)"x")), new Entry((V)"y", R.Get(v_783, (V)"y"))), R.Get(v_785, (V)"id")}, new Results(v_709)));
}
} else {
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"DamageBlocked", v_783, R.Get(v_708, (V)"id")}, new Results(v_709)));
}
if (R.Truth((V)(R.Get(v_783, (V)"hp_after") == (V)0.0d))) {
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"Died", v_783, R.Get(v_708, (V)"id")}, new Results(v_709)));
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"EntityKilled", v_783, R.Get(v_708, (V)"id")}, new Results(v_709)));
R.Call(v_688, R.Join(new V[] {v_706, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_706}, new Results(R.Get(v_783, (V)"target_id")))).First, v_707, R.Get(v_708, (V)"id")}, new Results(v_709)));
}
}
foreach (var row_789 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_719)))))) {
V v_790 = row_789[0];
V v_791 = row_789[1];
var values_792 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_706}, new Results(R.Get(v_791, (V)"target_id")))));
V v_793 = values_792[0];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_791, (V)"oath_shared_only"))), () => (V)(!R.Truth(R.And(v_793, () => R.Get(v_793, (V)"invulnerable"))))))) {
R.Call(v_95, R.Join(new V[] {v_706, v_707, (V)"HitConfirmed", v_791, R.Get(v_708, (V)"id")}, new Results(v_709)));
}
}
return Results.Empty;
}));
V v_794 = V.Nil;
v_794 = new V((RuleFunction)(args_795 => {
V v_796 = args_795[0];
V v_797 = args_795[1];
V v_798 = args_795[2];
V v_799 = args_795[3];
V v_800 = args_795[4];
V v_801 = args_795[5];
V v_802 = args_795[6];
V v_803 = args_795[7];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_800)), () => (V)(!R.Truth(R.Get(v_800, (V)"hp")))), () => (V)(R.Get(v_800, (V)"hp") <= (V)0.0d)))) {
return Results.Empty;
}
var values_804 = R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_796}, new Results(v_798))).First, () => R.Table(new Entry((V)"id", R.Or(v_798, () => (V)0.0d)), new Entry((V)"faction", R.Or(v_803, () => (V)"environment")), new Entry((V)"hp", (V)0.0d), new Entry((V)"max_hp", (V)0.0d)))));
V v_805 = values_804[0];
R.Call(v_704, R.Join(new V[] {v_796, v_797, v_805, v_799, R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_800, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_800, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_800, (V)"position"), (V)"y"))))), v_801, (V)false, v_802, (V)false}, new Results((V)false)));
return Results.Empty;
}));
V v_806 = V.Nil;
v_806 = new V((RuleFunction)(args_807 => {
V v_808 = args_807[0];
V v_809 = args_807[1];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"chain_id", R.Get(v_808, (V)"chain_id")), new Entry((V)"root_action_id", R.Get(v_808, (V)"root_action_id")), new Entry((V)"parent_id", R.Get(v_808, (V)"id")), new Entry((V)"ancestry", R.Get(v_808, (V)"ancestry")), new Entry((V)"proc_coefficient", R.Or(R.Get(v_809, (V)"proc_coefficient"), () => (V)1.0d)), new Entry((V)"source_faction", R.Get(v_808, (V)"source_faction")), new Entry((V)"source_position", R.Get(v_808, (V)"source_position")))));
return Results.Empty;
}));
V v_810 = V.Nil;
v_810 = new V((RuleFunction)(args_811 => {
V v_812 = args_811[0];
V v_813 = args_811[1];
V v_814 = args_811[2];
var values_815 = R.Join(new V[] {}, new Results(R.Get(v_813, (V)"event")));
V v_816 = values_815[0];
if (R.Truth((V)(R.Get(v_814, (V)"kind") == (V)"echo_impact"))) {
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_816, (V)"target_cells"), () => R.Table())));
}
if (R.Truth((V)(R.Get(v_814, (V)"kind") == (V)"pattern"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"cells"), R.Join(new V[] {v_812, R.Get(v_814, (V)"pattern")}, new Results(v_816))));
}
if (R.Truth((V)(R.Get(v_814, (V)"kind") == (V)"damage_source"))) {
var values_817 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_812}, new Results(R.Get(v_816, (V)"source_id")))));
V v_818 = values_817[0];
if (R.Truth(R.Or((V)(!R.Truth(v_818)), () => (V)(R.Get(v_818, (V)"hp") <= (V)0.0d)))) {
return R.Join(new V[] {}, new Results(R.Table()));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_818, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_818, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_818, (V)"position"), (V)"y")))))));
}
if (R.Truth((V)(R.Get(v_814, (V)"kind") == (V)"damage_target"))) {
var values_819 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_812}, new Results(R.Get(v_816, (V)"target_id")))));
V v_820 = values_819[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_820)), () => (V)(!R.Truth(R.Get(v_820, (V)"hp")))), () => (V)(R.Get(v_820, (V)"hp") <= (V)0.0d)))) {
return R.Join(new V[] {}, new Results(R.Table()));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_820, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_820, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_820, (V)"position"), (V)"y")))))));
}
if (R.Truth(R.Or((V)(R.Get(v_814, (V)"kind") == (V)"adjacent_hostiles"), () => (V)(R.Get(v_814, (V)"kind") == (V)"path_adjacent_hostiles")))) {
var values_821 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_812}, new Results(R.Get(v_813, (V)"owner_id")))));
V v_822 = values_821[0];
if (R.Truth((V)(!R.Truth(v_822)))) {
return R.Join(new V[] {}, new Results(R.Table()));
}
var values_823 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_814, (V)"kind") == (V)"path_adjacent_hostiles"), () => R.Get(v_816, (V)"path")), () => R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(v_816, (V)"floor_id")), new Entry((V)"x", R.Get(v_816, (V)"x")), new Entry((V)"y", R.Get(v_816, (V)"y"))))))));
V v_824 = values_823[0];
var values_825 = R.Join(new V[] {}, new Results(R.Table()));
V v_826 = values_825[0];
foreach (var row_827 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_812, (V)"entities"))))))) {
V v_828 = row_827[0];
V v_829 = row_827[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_829, (V)"id") != R.Get(v_822, (V)"id")), () => R.Get(v_829, (V)"hp")), () => (V)(R.Get(v_829, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_829, (V)"faction") != R.Get(v_822, (V)"faction"))))) {
foreach (var row_830 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_824, () => R.Table()))))))) {
V v_831 = row_830[0];
V v_832 = row_830[1];
if (R.Truth(R.And((V)(R.Get(R.Get(v_829, (V)"position"), (V)"floor_id") == R.Get(v_832, (V)"floor_id")), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_829, (V)"position"), (V)"x") - R.Get(v_832, (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_829, (V)"position"), (V)"y") - R.Get(v_832, (V)"y"))))))).First == (V)1.0d)))) {
var values_833 = R.Join(new V[] {}, new Results(v_829));
var obj_834 = v_826;
V key_835 = (V)((V)R.Length(v_826) + (V)1.0d);
R.Set(obj_834, key_835, values_833[0]);

break;
}
}
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_826}, new Results(new V((RuleFunction)(args_836 => {
V v_837 = args_836[0];
V v_838 = args_836[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_837, (V)"id") < R.Get(v_838, (V)"id"))));
return Results.Empty;
})))));
var values_839 = R.Join(new V[] {}, new Results(R.Table()));
V v_840 = values_839[0];
foreach (var row_841 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_826)))))) {
V v_842 = row_841[0];
V v_843 = row_841[1];
var values_844 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_843, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_843, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_843, (V)"position"), (V)"y")))));
var obj_845 = v_840;
V key_846 = (V)((V)R.Length(v_840) + (V)1.0d);
R.Set(obj_845, key_846, values_844[0]);
}
return R.Join(new V[] {}, new Results(v_840));
}
if (R.Truth((V)(R.Get(v_814, (V)"kind") == (V)"status_neighbors"))) {
var values_847 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_848 = values_847[0];
V v_849 = values_847[1];
foreach (var row_850 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_812, (V)"entities"))))))) {
V v_851 = row_850[0];
V v_852 = row_850[1];
var values_853 = R.Join(new V[] {}, new Results(R.Get(v_852, (V)"position")));
V v_854 = values_853[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_852, (V)"id") != R.Get(v_816, (V)"target_id")), () => R.Get(v_852, (V)"hp")), () => (V)(R.Get(v_852, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_852, (V)"faction") != R.Get(v_813, (V)"source_faction"))), () => R.Get(v_852, (V)"statuses")), () => R.Get(R.Get(v_852, (V)"statuses"), R.Get(v_814, (V)"status"))), () => (V)(R.Get(v_854, (V)"floor_id") == R.Get(v_816, (V)"floor_id"))), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_854, (V)"x") - R.Get(v_816, (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_854, (V)"y") - R.Get(v_816, (V)"y"))))))).First == (V)1.0d)))) {
var values_855 = R.Join(new V[] {}, new Results(v_852));
var obj_856 = v_849;
V key_857 = (V)((V)R.Length(v_849) + (V)1.0d);
R.Set(obj_856, key_857, values_855[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_849}, new Results(new V((RuleFunction)(args_858 => {
V v_859 = args_858[0];
V v_860 = args_858[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_859, (V)"id") < R.Get(v_860, (V)"id"))));
return Results.Empty;
})))));
foreach (var row_861 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_849)))))) {
V v_862 = row_861[0];
V v_863 = row_861[1];
var values_864 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_863, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_863, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_863, (V)"position"), (V)"y")))));
var obj_865 = v_848;
V key_866 = (V)((V)R.Length(v_848) + (V)1.0d);
R.Set(obj_865, key_866, values_864[0]);
}
return R.Join(new V[] {}, new Results(v_848));
}
var values_867 = R.Join(new V[] {R.Or(R.Get(v_816, (V)"x"), () => R.Get(v_816, (V)"from_x"))}, new Results(R.Or(R.Get(v_816, (V)"y"), () => R.Get(v_816, (V)"from_y"))));
V v_868 = values_867[0];
V v_869 = values_867[1];
var values_870 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_816, (V)"floor_id"), () => R.And(R.Get(v_816, (V)"source_position"), () => R.Get(R.Get(v_816, (V)"source_position"), (V)"floor_id")))));
V v_871 = values_870[0];
if (R.Truth(R.Or((V)(!R.Truth(v_868)), () => (V)(!R.Truth(v_869))))) {
var values_872 = R.Join(new V[] {R.Get(R.Get(v_816, (V)"source_position"), (V)"x")}, new Results(R.Get(R.Get(v_816, (V)"source_position"), (V)"y")));
v_868 = values_872[0];
v_869 = values_872[1];
}
if (R.Truth((V)(R.Get(v_814, (V)"kind") == (V)"damage_nearest"))) {
var values_873 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_812}, new Results(R.Get(v_813, (V)"owner_id")))));
V v_874 = values_873[0];
if (R.Truth((V)(!R.Truth(v_874)))) {
return R.Join(new V[] {}, new Results(R.Table()));
}
var values_875 = Results.Empty;
V v_876 = values_875[0];
V v_877 = values_875[1];
foreach (var row_878 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_812, (V)"entities"))))))) {
V v_879 = row_878[0];
V v_880 = row_878[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_880, (V)"id") != R.Get(v_874, (V)"id")), () => (V)(R.Get(v_880, (V)"id") != R.Get(v_816, (V)"target_id"))), () => R.Get(v_880, (V)"hp")), () => (V)(R.Get(v_880, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_880, (V)"faction") != R.Get(v_874, (V)"faction"))), () => (V)(R.Get(R.Get(v_880, (V)"position"), (V)"floor_id") == v_871)))) {
var values_881 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_880, (V)"position"), (V)"x") - v_868)))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_880, (V)"position"), (V)"y") - v_869)))).First)));
V v_882 = values_881[0];
if (R.Truth(R.And(R.And((V)(v_882 <= R.Get(v_814, (V)"radius")), () => R.Or((V)(!R.Truth(R.Get(v_814, (V)"clear_path"))), () => R.Call(R.Get(v_8, (V)"clear_segment"), R.Join(new V[] {v_812, v_871, v_868, v_869, R.Get(R.Get(v_880, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_880, (V)"position"), (V)"y")))).First)), () => R.Or(R.Or((V)(!R.Truth(v_876)), () => (V)(v_882 < v_877)), () => R.And((V)(v_882 == v_877), () => (V)(R.Get(v_880, (V)"id") < R.Get(v_876, (V)"id"))))))) {
var values_883 = R.Join(new V[] {v_880}, new Results(v_882));
v_876 = values_883[0];
v_877 = values_883[1];
}
}
}
if (R.Truth((V)(!R.Truth(v_876)))) {
return R.Join(new V[] {}, new Results(R.Table()));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", v_871), new Entry((V)"x", R.Get(R.Get(v_876, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_876, (V)"position"), (V)"y")))))));
}
if (R.Truth((V)(R.Get(v_814, (V)"kind") == (V)"area"))) {
var values_884 = R.Join(new V[] {}, new Results(R.Table()));
V v_885 = values_884[0];
{ double end_888 = R.Num(R.Get(v_814, (V)"radius")), step_889 = R.Num((V)1.0d);
for (double i_887 = R.Num((-R.Get(v_814, (V)"radius"))); step_889 > 0 ? i_887 <= end_888 : i_887 >= end_888; i_887 += step_889) {
V v_886 = i_887;
{ double end_892 = R.Num(R.Get(v_814, (V)"radius")), step_893 = R.Num((V)1.0d);
for (double i_891 = R.Num((-R.Get(v_814, (V)"radius"))); step_893 > 0 ? i_891 <= end_892 : i_891 >= end_892; i_891 += step_893) {
V v_890 = i_891;
if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_890))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_886))))).First <= R.Get(v_814, (V)"radius")))) {
var values_894 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", v_871), new Entry((V)"x", (V)(v_868 + v_890)), new Entry((V)"y", (V)(v_869 + v_886)))));
var obj_895 = v_885;
V key_896 = (V)((V)R.Length(v_885) + (V)1.0d);
R.Set(obj_895, key_896, values_894[0]);
}
}
}
}
}
return R.Join(new V[] {}, new Results(v_885));
}
return R.Join(new V[] {}, new Results(R.Table()));
return Results.Empty;
}));
V v_897 = V.Nil;
v_897 = new V((RuleFunction)(args_898 => {
V v_899 = args_898[0];
V v_900 = args_898[1];
V v_901 = args_898[2];
V v_902 = args_898[3];
var values_903 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_83, v_900)}, new Results((V)"missing boon resolution context"))));
V v_904 = values_903[0];
var values_905 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_899}, new Results(R.Get(v_901, (V)"owner_id")))));
V v_906 = values_905[0];
var values_907 = R.Join(new V[] {}, new Results(R.Or(v_906, () => R.Table(new Entry((V)"id", R.Get(v_901, (V)"owner_id")), new Entry((V)"position", R.Get(v_901, (V)"source_position"))))));
V v_908 = values_907[0];
var values_909 = R.Join(new V[] {}, R.Call(v_806, R.Join(new V[] {v_901}, new Results(v_902))));
var obj_910 = v_904;
V key_911 = (V)"cause";
R.Set(obj_910, key_911, values_909[0]);
if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"echo"))) {
var values_912 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_901))));
V v_913 = values_912[0];
if (R.Truth(R.Get(v_902, (V)"pattern"))) {
var values_914 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"cells"), R.Join(new V[] {v_899, R.Get(v_902, (V)"pattern")}, new Results(R.Get(v_901, (V)"event")))));
var obj_915 = R.Get(v_913, (V)"event");
V key_916 = (V)"target_cells";
R.Set(obj_915, key_916, values_914[0]);
}
var values_917 = R.Join(new V[] {}, new Results((V)(R.Get(v_899, (V)"clock") + R.Get(v_902, (V)"delay"))));
V v_918 = values_917[0];
var values_919 = R.Join(new V[] {}, R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_899}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", v_918), new Entry((V)"actor_id", R.Get(v_908, (V)"id")), new Entry((V)"attack_id", R.Get(v_901, (V)"root_action_id")))))));
V v_920 = values_919[0];
var values_921 = R.Join(new V[] {}, new Results(R.Get(v_920, (V)"id")));
var obj_922 = v_920;
V key_923 = (V)"effect_id";
R.Set(obj_922, key_923, values_921[0]);
var values_924 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"activation", v_913), new Entry((V)"effect", R.Table(new Entry((V)"kind", (V)"echo_impact"), new Entry((V)"damage_type", R.Get(v_902, (V)"damage_type")), new Entry((V)"proc_coefficient", R.Get(v_902, (V)"proc_coefficient")))))));
var obj_925 = R.Get(v_899, (V)"delayed_effects");
V key_926 = R.Get(v_920, (V)"id");
R.Set(obj_925, key_926, values_924[0]);
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"DelayedEffectScheduled", R.Table(new Entry((V)"boon_id", R.Get(v_901, (V)"boon_id")), new Entry((V)"activation_id", R.Get(v_901, (V)"id")), new Entry((V)"due", v_918), new Entry((V)"effect_id", R.Get(v_920, (V)"id")), new Entry((V)"target_cells", R.Get(R.Get(v_913, (V)"event"), (V)"target_cells"))), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"displace_target"))) {
var values_927 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_899}, new Results(R.Get(R.Get(v_901, (V)"event"), (V)"target_id")))));
V v_928 = values_927[0];
if (R.Truth(R.And(R.And(v_906, () => v_928), () => R.Call(R.Get(v_4, (V)"skull_target"), R.Join(new V[] {}, new Results(v_928))).First))) {
var values_929 = R.Join(new V[] {R.Get(v_906, (V)"position")}, new Results(R.Get(v_928, (V)"position")));
V v_930 = values_929[0];
V v_931 = values_929[1];
var values_932 = R.Join(new V[] {(V)(R.Get(v_931, (V)"x") - R.Get(v_930, (V)"x"))}, new Results((V)(R.Get(v_931, (V)"y") - R.Get(v_930, (V)"y"))));
V v_933 = values_932[0];
V v_934 = values_932[1];
if (R.Truth(R.And((V)(R.Get(v_930, (V)"floor_id") == R.Get(v_931, (V)"floor_id")), () => (V)((V)(v_933 == (V)0.0d) != (V)(v_934 == (V)0.0d))))) {
var values_935 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_933 == (V)0.0d), () => (V)0.0d), () => R.And((V)(v_933 > (V)0.0d), () => (V)1.0d)), () => (-(V)1.0d))));
v_933 = values_935[0];
var values_936 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_934 == (V)0.0d), () => (V)0.0d), () => R.And((V)(v_934 > (V)0.0d), () => (V)1.0d)), () => (-(V)1.0d))));
v_934 = values_936[0];
if (R.Truth((V)(R.Get(v_902, (V)"mode") == (V)"pull"))) {
var values_937 = R.Join(new V[] {(-v_933)}, new Results((-v_934)));
v_933 = values_937[0];
v_934 = values_937[1];
}
var values_938 = R.Join(new V[] {}, R.Call(v_660, R.Join(new V[] {v_899, v_900, v_928, v_933, v_934, R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id")))));
V v_939 = values_938[0];
if (R.Truth(R.And((V)(!R.Truth(v_939)), () => R.Get(v_902, (V)"collision_damage")))) {
R.Call(v_704, R.Join(new V[] {v_899, v_900, v_908, R.Get(v_901, (V)"root_action_id"), R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(v_931, (V)"floor_id")), new Entry((V)"x", R.Get(v_931, (V)"x")), new Entry((V)"y", R.Get(v_931, (V)"y"))))), R.Get(v_901, (V)"power"), (V)false}, new Results(R.Get(v_902, (V)"damage_type"))));
}
}
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"stun_target"))) {
var values_940 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_899}, new Results(R.Get(R.Get(v_901, (V)"event"), (V)"target_id")))));
V v_941 = values_940[0];
if (R.Truth(R.And(R.And(v_906, () => v_941), () => R.Call(R.Get(v_4, (V)"skull_target"), R.Join(new V[] {}, new Results(v_941))).First))) {
var values_942 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Or(R.Get(v_941, (V)"stunned_until"), () => (V)0.0d)}, new Results((V)(R.Get(v_899, (V)"clock") + R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_902, (V)"cap")}, new Results(R.Get(v_901, (V)"power")))).First)))));
V v_943 = values_942[0];
var values_944 = R.Join(new V[] {}, new Results(v_943));
V v_945 = values_944[0];
foreach (var row_946 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"schedule"))))))) {
V v_947 = row_946[0];
V v_948 = row_946[1];
if (R.Truth(R.And((V)(R.Get(v_948, (V)"actor_id") == R.Get(v_941, (V)"id")), () => (V)(R.Get(v_948, (V)"kind") == (V)"actor_ready")))) {
var values_949 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_945}, new Results(R.Get(v_948, (V)"due")))));
v_945 = values_949[0];
}
}
R.Call(v_632, R.Join(new V[] {v_899, v_941, v_900, (V)"stun", R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
var values_950 = R.Join(new V[] {}, new Results(v_943));
var obj_951 = v_941;
V key_952 = (V)"stunned_until";
R.Set(obj_951, key_952, values_950[0]);
R.Call(R.Get(v_38, (V)"remove_for_actor"), R.Join(new V[] {v_899, R.Get(v_941, (V)"id")}, new Results((V)"actor_ready")));
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_899}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", v_945), new Entry((V)"actor_id", R.Get(v_941, (V)"id")), new Entry((V)"is_player", (V)false)))));
var values_953 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_906, (V)"boon_memory"), () => R.Table())));
var obj_954 = v_906;
V key_955 = (V)"boon_memory";
R.Set(obj_954, key_955, values_953[0]);
var values_956 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"expires_at", (V)(R.Get(v_899, (V)"clock") + R.Get(v_902, (V)"cooldown"))))));
var obj_957 = R.Get(v_906, (V)"boon_memory");
V key_958 = R.Get(v_901, (V)"boon_id");
R.Set(obj_957, key_958, values_956[0]);
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"Stunned", R.Table(new Entry((V)"target_id", R.Get(v_941, (V)"id")), new Entry((V)"until_time", v_943), new Entry((V)"boon_id", R.Get(v_901, (V)"boon_id"))), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"ward_to_heal"))) {
if (R.Truth(R.And(R.And(R.And(v_906, () => R.Get(v_906, (V)"hp")), () => (V)(R.Get(v_906, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_906, (V)"hp") < R.Get(v_906, (V)"max_hp"))))) {
var values_959 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_901, (V)"power")}, new Results((V)(R.Get(v_906, (V)"max_hp") - R.Get(v_906, (V)"hp"))))));
V v_960 = values_959[0];
var values_961 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"consume_ward"), R.Join(new V[] {v_899, v_906}, new Results(v_960))));
V v_962 = values_961[0];
if (R.Truth((V)(v_962 > (V)0.0d))) {
var values_963 = R.Join(new V[] {}, new Results(R.Get(v_906, (V)"hp")));
V v_964 = values_963[0];

var values_965 = R.Join(new V[] {}, new Results((V)(v_964 + v_962)));
var obj_966 = v_906;
V key_967 = (V)"hp";
R.Set(obj_966, key_967, values_965[0]);
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"Healed", R.Table(new Entry((V)"target_id", R.Get(v_906, (V)"id")), new Entry((V)"requested", R.Get(v_901, (V)"power")), new Entry((V)"actual_gain", v_962), new Entry((V)"hp_before", v_964), new Entry((V)"hp_after", R.Get(v_906, (V)"hp")), new Entry((V)"spent_wards", (V)true)), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
}
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"consume_burning"))) {
var values_968 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_899}, new Results(R.Get(R.Get(v_901, (V)"event"), (V)"target_id")))));
V v_969 = values_968[0];
var values_970 = R.Join(new V[] {}, new Results(R.And(R.And(v_969, () => R.Get(v_969, (V)"statuses")), () => R.Get(R.Get(v_969, (V)"statuses"), (V)"burning"))));
V v_971 = values_970[0];
if (R.Truth(R.And(R.And(R.And(v_969, () => R.Get(v_969, (V)"hp")), () => (V)(R.Get(v_969, (V)"hp") > (V)0.0d)), () => v_971))) {
{ double end_974 = R.Num((V)1.0d), step_975 = R.Num((-(V)1.0d));
for (double i_973 = R.Num((V)R.Length(R.Get(v_899, (V)"schedule"))); step_975 > 0 ? i_973 <= end_974 : i_973 >= end_974; i_973 += step_975) {
V v_972 = i_973;
if (R.Truth((V)(R.Get(R.Get(R.Get(v_899, (V)"schedule"), v_972), (V)"id") == R.Get(v_971, (V)"schedule_id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_899, (V)"schedule")}, new Results(v_972)));
}
}
}
var values_976 = R.Join(new V[] {}, new Results(V.Nil));
var obj_977 = R.Get(v_969, (V)"statuses");
V key_978 = (V)"burning";
R.Set(obj_977, key_978, values_976[0]);
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"StatusCleansed", R.Table(new Entry((V)"status", (V)"burning"), new Entry((V)"target_id", R.Get(v_969, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_969, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_969, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_969, (V)"position"), (V)"y")), new Entry((V)"consumed", (V)true)), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
R.Call(v_704, R.Join(new V[] {v_899, v_900, v_908, R.Get(v_901, (V)"root_action_id"), R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_969, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_969, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_969, (V)"position"), (V)"y"))))), R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_901, (V)"power")}, new Results((V)(R.Get(v_971, (V)"power") * R.Get(v_971, (V)"remaining"))))).First, (V)false}, new Results(R.Get(v_902, (V)"damage_type"))));
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"gather_pickups"))) {
if (R.Truth(R.And(R.And(R.And(v_906, () => (V)(R.Get(v_906, (V)"id") == R.Get(v_899, (V)"player_id"))), () => R.Get(v_906, (V)"hp")), () => (V)(R.Get(v_906, (V)"hp") > (V)0.0d)))) {
var values_979 = R.Join(new V[] {}, new Results((V)0.0d));
V v_980 = values_979[0];

var values_981 = R.Join(new V[] {}, new Results(R.Get(v_906, (V)"position")));
V v_982 = values_981[0];

var values_983 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_899))));
V v_984 = values_983[0];
foreach (var row_985 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_986 = row_985[0];
V v_987 = row_985[1];
var values_988 = R.Join(new V[] {(V)(R.Get(v_982, (V)"x") + R.Get(v_987, (V)1.0d))}, new Results((V)(R.Get(v_982, (V)"y") + R.Get(v_987, (V)2.0d))));
V v_989 = values_988[0];
V v_990 = values_988[1];
if (R.Truth(R.And(R.And(R.And((V)(v_980 < R.Get(v_901, (V)"power")), () => R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_984, v_989}, new Results(v_990))).First), () => R.Call(R.Get(v_12, (V)"projectile_passable"), R.Join(new V[] {v_984, v_989, v_990}, new Results(v_899))).First), () => R.Call(R.Get(v_70, (V)"can_see"), R.Join(new V[] {v_899, R.Get(v_982, (V)"floor_id"), v_989}, new Results(v_990))).First))) {
var values_991 = R.Join(new V[] {}, new Results((V)false));
V v_992 = values_991[0];
R.Call(R.Get(v_36, (V)"pickup_at"), R.Join(new V[] {v_899, v_906, v_989, v_990}, new Results(new V((RuleFunction)(args_993 => {
V v_994 = args_993[0];
V v_995 = args_993[1];
V v_996 = args_993[2];
var values_997 = R.Join(new V[] {}, new Results((V)true));
v_992 = values_997[0];
R.Call(v_95, R.Join(new V[] {v_899, v_900, v_994, v_995, v_996}, new Results(R.Get(v_901, (V)"root_action_id"))));
return Results.Empty;
})))));
if (R.Truth(v_992)) {
var values_998 = R.Join(new V[] {}, new Results((V)(v_980 + (V)1.0d)));
v_980 = values_998[0];
}
}
}
}
} else if (R.Truth(R.Or(R.Or((V)(R.Get(v_902, (V)"kind") == (V)"prime"), () => (V)(R.Get(v_902, (V)"kind") == (V)"speed_boost")), () => (V)(R.Get(v_902, (V)"kind") == (V)"ward")))) {
if (R.Truth(R.And(R.And(v_906, () => R.Get(v_906, (V)"hp")), () => (V)(R.Get(v_906, (V)"hp") > (V)0.0d)))) {
var values_999 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_906, (V)"boon_memory"), () => R.Table())));
var obj_1000 = v_906;
V key_1001 = (V)"boon_memory";
R.Set(obj_1000, key_1001, values_999[0]);
var values_1002 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_906, (V)"boon_memory"), R.Get(v_901, (V)"boon_id")), () => R.Table())));
V v_1003 = values_1002[0];
var values_1004 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_1003, (V)"expires_at"), () => (-(V)1.0d)) >= R.Get(v_899, (V)"clock"))));
V v_1005 = values_1004[0];
var values_1006 = R.Join(new V[] {}, new Results((V)(R.Get(v_899, (V)"clock") + R.Get(v_902, (V)"duration"))));
var obj_1007 = v_1003;
V key_1008 = (V)"expires_at";
R.Set(obj_1007, key_1008, values_1006[0]);
if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"prime"))) {
var values_1009 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_1010 = v_1003;
V key_1011 = (V)"charges";
R.Set(obj_1010, key_1011, values_1009[0]);
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"speed_boost"))) {
var values_1012 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Or(R.And(v_1005, () => R.Or(R.Get(v_1003, (V)"speed"), () => (V)0.0d)), () => (V)0.0d)}, new Results(R.Get(v_901, (V)"power")))));
var obj_1013 = v_1003;
V key_1014 = (V)"speed";
R.Set(obj_1013, key_1014, values_1012[0]);
} else {
var values_1015 = R.Join(new V[] {}, new Results(R.Get(v_901, (V)"power")));
V v_1016 = values_1015[0];
if (R.Truth((V)(R.Get(v_902, (V)"scale") == (V)"supply_excess"))) {
var values_1017 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_1016}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(R.Or(R.Get(R.Get(v_901, (V)"event"), (V)"amount"), () => (V)0.0d) - R.Or(R.Get(R.Get(v_901, (V)"event"), (V)"actual_gain"), () => (V)0.0d))))))));
v_1016 = values_1017[0];
}
var values_1018 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Or(R.And(v_1005, () => R.Or(R.Get(v_1003, (V)"ward"), () => (V)0.0d)), () => (V)0.0d)}, new Results(v_1016))));
var obj_1019 = v_1003;
V key_1020 = (V)"ward";
R.Set(obj_1019, key_1020, values_1018[0]);
}
var values_1021 = R.Join(new V[] {}, new Results(v_1003));
var obj_1022 = R.Get(v_906, (V)"boon_memory");
V key_1023 = R.Get(v_901, (V)"boon_id");
R.Set(obj_1022, key_1023, values_1021[0]);
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"BoonStateApplied", R.Table(new Entry((V)"boon_id", R.Get(v_901, (V)"boon_id")), new Entry((V)"effect_kind", R.Get(v_902, (V)"kind")), new Entry((V)"expires_at", R.Get(v_1003, (V)"expires_at")), new Entry((V)"power", R.Get(v_901, (V)"power"))), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"mark_damage"))) {
if (R.Truth(R.And(R.And(v_906, () => R.Get(v_906, (V)"hp")), () => (V)(R.Get(v_906, (V)"hp") > (V)0.0d)))) {
var values_1024 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_899}, new Results(R.Get(R.Get(v_901, (V)"event"), (V)"target_id")))));
V v_1025 = values_1024[0];
if (R.Truth(R.And(R.And(v_1025, () => R.Get(v_1025, (V)"hp")), () => (V)(R.Get(v_1025, (V)"hp") > (V)0.0d)))) {
var values_1026 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_906, (V)"boon_memory"), () => R.Table())));
var obj_1027 = v_906;
V key_1028 = (V)"boon_memory";
R.Set(obj_1027, key_1028, values_1026[0]);
var values_1029 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_906, (V)"boon_memory"), R.Get(v_901, (V)"boon_id")), () => R.Table())));
V v_1030 = values_1029[0];
if (R.Truth(R.Or((V)(R.Get(v_1030, (V)"mark_target_id") != R.Get(v_1025, (V)"id")), () => (V)(R.Or(R.Get(v_1030, (V)"expires_at"), () => (-(V)1.0d)) < R.Get(v_899, (V)"clock"))))) {
var values_1031 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_1032 = v_1030;
V key_1033 = (V)"mark_count";
R.Set(obj_1032, key_1033, values_1031[0]);
}
var values_1034 = R.Join(new V[] {}, new Results(R.Get(v_1025, (V)"id")));
var obj_1035 = v_1030;
V key_1036 = (V)"mark_target_id";
R.Set(obj_1035, key_1036, values_1034[0]);
var values_1037 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_902, (V)"cap")}, new Results((V)(R.Or(R.Get(v_1030, (V)"mark_count"), () => (V)0.0d) + (V)1.0d)))));
var obj_1038 = v_1030;
V key_1039 = (V)"mark_count";
R.Set(obj_1038, key_1039, values_1037[0]);
var values_1040 = R.Join(new V[] {}, new Results((V)(R.Get(v_899, (V)"clock") + R.Get(v_902, (V)"duration"))));
var obj_1041 = v_1030;
V key_1042 = (V)"expires_at";
R.Set(obj_1041, key_1042, values_1040[0]);
var values_1043 = R.Join(new V[] {}, new Results(v_1030));
var obj_1044 = R.Get(v_906, (V)"boon_memory");
V key_1045 = R.Get(v_901, (V)"boon_id");
R.Set(obj_1044, key_1045, values_1043[0]);
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"BoonStateApplied", R.Table(new Entry((V)"boon_id", R.Get(v_901, (V)"boon_id")), new Entry((V)"effect_kind", R.Get(v_902, (V)"kind")), new Entry((V)"target_id", R.Get(v_1025, (V)"id")), new Entry((V)"stacks", R.Get(v_1030, (V)"mark_count")), new Entry((V)"expires_at", R.Get(v_1030, (V)"expires_at"))), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
if (R.Truth((V)(R.Get(v_1030, (V)"mark_count") >= R.Get(v_902, (V)"threshold")))) {
var values_1046 = R.Join(new V[] {}, new Results(R.Get(v_1025, (V)"position")));
V v_1047 = values_1046[0];
R.Call(v_704, R.Join(new V[] {v_899, v_900, v_908, R.Get(v_901, (V)"root_action_id"), R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(v_1047, (V)"floor_id")), new Entry((V)"x", R.Get(v_1047, (V)"x")), new Entry((V)"y", R.Get(v_1047, (V)"y"))))), (V)(R.Get(v_901, (V)"power") * (V)((V)(R.Get(v_1030, (V)"mark_count") - R.Get(v_902, (V)"threshold")) + (V)1.0d)), (V)false, R.Get(v_902, (V)"damage_type"), (V)false}, new Results((V)false)));
}
}
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"extend_burning"))) {
var values_1048 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_899}, new Results(R.Get(R.Get(v_901, (V)"event"), (V)"target_id")))));
V v_1049 = values_1048[0];
var values_1050 = R.Join(new V[] {}, new Results(R.And(R.And(v_1049, () => R.Get(v_1049, (V)"statuses")), () => R.Get(R.Get(v_1049, (V)"statuses"), (V)"burning"))));
V v_1051 = values_1050[0];
if (R.Truth(R.And(R.And(R.And(v_1049, () => R.Get(v_1049, (V)"hp")), () => (V)(R.Get(v_1049, (V)"hp") > (V)0.0d)), () => v_1051))) {
var values_1052 = R.Join(new V[] {}, new Results(R.Get(v_1051, (V)"remaining")));
V v_1053 = values_1052[0];
var values_1054 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_902, (V)"cap")}, new Results((V)(v_1053 + R.Get(v_901, (V)"power"))))));
var obj_1055 = v_1051;
V key_1056 = (V)"remaining";
R.Set(obj_1055, key_1056, values_1054[0]);
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"BoonStateApplied", R.Table(new Entry((V)"boon_id", R.Get(v_901, (V)"boon_id")), new Entry((V)"effect_kind", R.Get(v_902, (V)"kind")), new Entry((V)"target_id", R.Get(v_1049, (V)"id")), new Entry((V)"remaining", R.Get(v_1051, (V)"remaining")), new Entry((V)"added_ticks", (V)(R.Get(v_1051, (V)"remaining") - v_1053)), new Entry((V)"floor_id", R.Get(R.Get(v_1049, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_1049, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_1049, (V)"position"), (V)"y"))), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"apply_status"))) {
var values_1057 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_899}, new Results(R.Get(R.Get(v_901, (V)"event"), (V)"target_id")))));
V v_1058 = values_1057[0];
if (R.Truth((V)(R.Get(v_902, (V)"target") == (V)"nearest"))) {
var values_1059 = R.Join(new V[] {}, R.Call(v_810, R.Join(new V[] {v_899, v_901}, new Results(R.Table(new Entry((V)"kind", (V)"damage_nearest"), new Entry((V)"radius", R.Get(v_902, (V)"radius")), new Entry((V)"clear_path", R.Get(v_902, (V)"clear_path")))))));
V v_1060 = values_1059[0];
var values_1061 = R.Join(new V[] {}, new Results(R.Get(v_1060, (V)1.0d)));
V v_1062 = values_1061[0];
var values_1063 = R.Join(new V[] {}, new Results(R.Or(R.And(v_1062, () => R.Call(v_144, R.Join(new V[] {v_899, R.Get(v_1062, (V)"floor_id"), R.Get(v_1062, (V)"x"), R.Get(v_1062, (V)"y"), R.Get(v_908, (V)"id")}, new Results((V)true))).First), () => V.Nil)));
v_1058 = values_1063[0];
}
if (R.Truth(R.And(R.And(v_1058, () => R.Get(v_1058, (V)"hp")), () => (V)(R.Get(v_1058, (V)"hp") > (V)0.0d)))) {
var values_1064 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_1058, (V)"statuses"), () => R.Table())));
var obj_1065 = v_1058;
V key_1066 = (V)"statuses";
R.Set(obj_1065, key_1066, values_1064[0]);
var values_1067 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1058, (V)"statuses"), R.Get(v_902, (V)"status"))));
V v_1068 = values_1067[0];
if (R.Truth(v_1068)) {
var values_1069 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_1068, (V)"power")}, new Results(R.Get(v_901, (V)"power")))));
var obj_1070 = v_1068;
V key_1071 = (V)"power";
R.Set(obj_1070, key_1071, values_1069[0]);
var values_1072 = R.Join(new V[] {}, new Results(R.Get(v_902, (V)"ticks")));
var obj_1073 = v_1068;
V key_1074 = (V)"remaining";
R.Set(obj_1073, key_1074, values_1072[0]);
var values_1075 = R.Join(new V[] {}, new Results(R.Get(v_901, (V)"owner_id")));
var obj_1076 = v_1068;
V key_1077 = (V)"source_id";
R.Set(obj_1076, key_1077, values_1075[0]);
var values_1078 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_901))));
var obj_1079 = v_1068;
V key_1080 = (V)"activation";
R.Set(obj_1079, key_1080, values_1078[0]);
} else {
var values_1081 = R.Join(new V[] {}, new Results((V)(R.Get(v_899, (V)"clock") + R.Get(v_902, (V)"interval"))));
V v_1082 = values_1081[0];
var values_1083 = R.Join(new V[] {}, R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_899}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", v_1082), new Entry((V)"actor_id", R.Get(v_1058, (V)"id")), new Entry((V)"attack_id", R.Get(v_901, (V)"root_action_id")))))));
V v_1084 = values_1083[0];
var values_1085 = R.Join(new V[] {}, new Results(R.Get(v_1084, (V)"id")));
var obj_1086 = v_1084;
V key_1087 = (V)"effect_id";
R.Set(obj_1086, key_1087, values_1085[0]);
var values_1088 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"power", R.Get(v_901, (V)"power")), new Entry((V)"remaining", R.Get(v_902, (V)"ticks")), new Entry((V)"interval", R.Get(v_902, (V)"interval")), new Entry((V)"next_due", v_1082), new Entry((V)"schedule_id", R.Get(v_1084, (V)"id")), new Entry((V)"source_id", R.Get(v_901, (V)"owner_id")), new Entry((V)"activation", R.Call(v_84, R.Join(new V[] {}, new Results(v_901))).First))));
var obj_1089 = R.Get(v_1058, (V)"statuses");
V key_1090 = R.Get(v_902, (V)"status");
R.Set(obj_1089, key_1090, values_1088[0]);
}
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"StatusApplied", R.Table(new Entry((V)"status", R.Get(v_902, (V)"status")), new Entry((V)"target_id", R.Get(v_1058, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_1058, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_1058, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_1058, (V)"position"), (V)"y")), new Entry((V)"power", R.Get(R.Get(R.Get(v_1058, (V)"statuses"), R.Get(v_902, (V)"status")), (V)"power"))), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"cleanse_owner"))) {
var values_1091 = R.Join(new V[] {}, new Results(R.And(R.And(v_906, () => R.Get(v_906, (V)"statuses")), () => R.Get(R.Get(v_906, (V)"statuses"), R.Get(v_902, (V)"status")))));
V v_1092 = values_1091[0];
if (R.Truth(R.And(R.And(R.And(v_906, () => R.Get(v_906, (V)"hp")), () => (V)(R.Get(v_906, (V)"hp") > (V)0.0d)), () => v_1092))) {
{ double end_1095 = R.Num((V)1.0d), step_1096 = R.Num((-(V)1.0d));
for (double i_1094 = R.Num((V)R.Length(R.Get(v_899, (V)"schedule"))); step_1096 > 0 ? i_1094 <= end_1095 : i_1094 >= end_1095; i_1094 += step_1096) {
V v_1093 = i_1094;
if (R.Truth((V)(R.Get(R.Get(R.Get(v_899, (V)"schedule"), v_1093), (V)"id") == R.Get(v_1092, (V)"schedule_id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_899, (V)"schedule")}, new Results(v_1093)));
}
}
}
var values_1097 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1098 = R.Get(v_906, (V)"statuses");
V key_1099 = R.Get(v_902, (V)"status");
R.Set(obj_1098, key_1099, values_1097[0]);
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"StatusCleansed", R.Table(new Entry((V)"status", R.Get(v_902, (V)"status")), new Entry((V)"target_id", R.Get(v_906, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_906, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_906, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_906, (V)"position"), (V)"y"))), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"refund_purchase"))) {
if (R.Truth(R.And(R.And(R.And(v_906, () => (V)(R.Get(v_906, (V)"id") == R.Get(v_899, (V)"player_id"))), () => R.Get(v_906, (V)"hp")), () => (V)(R.Get(v_906, (V)"hp") > (V)0.0d)))) {
var values_1100 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_901, (V)"power")}, new Results(R.Or(R.Get(R.Get(v_901, (V)"event"), (V)"price"), () => (V)0.0d)))));
V v_1101 = values_1100[0];
if (R.Truth((V)(v_1101 > (V)0.0d))) {
var values_1102 = R.Join(new V[] {}, new Results(R.Get(v_899, (V)"currency")));
V v_1103 = values_1102[0];

var values_1104 = R.Join(new V[] {}, new Results((V)(v_1103 + v_1101)));
var obj_1105 = v_899;
V key_1106 = (V)"currency";
R.Set(obj_1105, key_1106, values_1104[0]);
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"CurrencyRefunded", R.Table(new Entry((V)"boon_id", R.Get(v_901, (V)"boon_id")), new Entry((V)"chest_id", R.Get(R.Get(v_901, (V)"event"), (V)"chest_id")), new Entry((V)"amount", v_1101), new Entry((V)"old_currency", v_1103), new Entry((V)"new_currency", R.Get(v_899, (V)"currency"))), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
}
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"enrich_drop"))) {
foreach (var row_1107 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"drops"))))))) {
V v_1108 = row_1107[0];
V v_1109 = row_1107[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_1109, (V)"kind") == (V)"currency"), () => (V)(!R.Truth(R.Get(v_1109, (V)"collected")))), () => (V)(R.Get(v_1109, (V)"killed_entity_id") == R.Get(R.Get(v_901, (V)"event"), (V)"target_id"))))) {
var values_1110 = R.Join(new V[] {}, new Results((V)(R.Get(v_1109, (V)"amount") + R.Get(v_901, (V)"power"))));
var obj_1111 = v_1109;
V key_1112 = (V)"amount";
R.Set(obj_1111, key_1112, values_1110[0]);
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"CurrencyDropEnriched", R.Table(new Entry((V)"boon_id", R.Get(v_901, (V)"boon_id")), new Entry((V)"drop_id", R.Get(v_1109, (V)"id")), new Entry((V)"amount", R.Get(v_901, (V)"power")), new Entry((V)"new_amount", R.Get(v_1109, (V)"amount")), new Entry((V)"floor_id", R.Get(R.Get(v_1109, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_1109, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_1109, (V)"position"), (V)"y"))), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
break;
}
}
} else if (R.Truth((V)(R.Get(v_902, (V)"kind") == (V)"heal_owner"))) {
if (R.Truth(R.And(R.And(v_906, () => R.Get(v_906, (V)"hp")), () => (V)(R.Get(v_906, (V)"hp") > (V)0.0d)))) {
if (R.Truth(R.And(R.Get(v_902, (V)"consume_prime"), () => R.Get(v_906, (V)"boon_memory")))) {
var values_1113 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_906, (V)"boon_memory"), R.Get(v_901, (V)"boon_id"))));
V v_1114 = values_1113[0];
if (R.Truth(v_1114)) {
var values_1115 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_1116 = v_1114;
V key_1117 = (V)"charges";
R.Set(obj_1116, key_1117, values_1115[0]);
}
}
var values_1118 = R.Join(new V[] {}, new Results(R.Get(v_906, (V)"hp")));
V v_1119 = values_1118[0];
var values_1120 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_906, (V)"max_hp")}, new Results((V)(R.Get(v_906, (V)"hp") + R.Get(v_901, (V)"power"))))));
var obj_1121 = v_906;
V key_1122 = (V)"hp";
R.Set(obj_1121, key_1122, values_1120[0]);
if (R.Truth(R.And(R.Get(v_902, (V)"cooldown"), () => (V)(R.Get(v_906, (V)"hp") > v_1119)))) {
var values_1123 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_906, (V)"boon_memory"), () => R.Table())));
var obj_1124 = v_906;
V key_1125 = (V)"boon_memory";
R.Set(obj_1124, key_1125, values_1123[0]);
var values_1126 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"expires_at", (V)(R.Get(v_899, (V)"clock") + R.Get(v_902, (V)"cooldown"))))));
var obj_1127 = R.Get(v_906, (V)"boon_memory");
V key_1128 = R.Get(v_901, (V)"boon_id");
R.Set(obj_1127, key_1128, values_1126[0]);
}
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"Healed", R.Table(new Entry((V)"target_id", R.Get(v_906, (V)"id")), new Entry((V)"requested", R.Get(v_901, (V)"power")), new Entry((V)"actual_gain", (V)(R.Get(v_906, (V)"hp") - v_1119)), new Entry((V)"hp_before", v_1119), new Entry((V)"hp_after", R.Get(v_906, (V)"hp"))), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
}
} else {
var values_1129 = R.Join(new V[] {}, R.Call(v_810, R.Join(new V[] {v_899, v_901}, new Results(v_902))));
V v_1130 = values_1129[0];
if (R.Truth(R.And(R.And(R.Get(v_902, (V)"consume_prime"), () => v_906), () => R.Get(v_906, (V)"boon_memory")))) {
var values_1131 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_906, (V)"boon_memory"), R.Get(v_901, (V)"boon_id"))));
V v_1132 = values_1131[0];
if (R.Truth(v_1132)) {
var values_1133 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_1134 = v_1132;
V key_1135 = (V)"charges";
R.Set(obj_1134, key_1135, values_1133[0]);
}
}
R.Call(v_95, R.Join(new V[] {v_899, v_900, (V)"EffectApplied", R.Table(new Entry((V)"boon_id", R.Get(v_901, (V)"boon_id")), new Entry((V)"activation_id", R.Get(v_901, (V)"id")), new Entry((V)"damage_type", R.Get(v_902, (V)"damage_type")), new Entry((V)"target_cells", v_1130)), R.Get(v_908, (V)"id")}, new Results(R.Get(v_901, (V)"root_action_id"))));
if (R.Truth((V)((V)R.Length(v_1130) > (V)0.0d))) {
var values_1136 = R.Join(new V[] {}, new Results(R.Get(v_901, (V)"power")));
V v_1137 = values_1136[0];
if (R.Truth((V)(R.Get(v_902, (V)"scale") == (V)"adjacent_hostiles"))) {
var values_1138 = R.Join(new V[] {}, new Results((V)(v_1137 * R.Or(R.Get(R.Get(v_901, (V)"event"), (V)"adjacent_hostile_count"), () => (V)0.0d))));
v_1137 = values_1138[0];
}
R.Call(v_704, R.Join(new V[] {v_899, v_900, v_908, R.Get(v_901, (V)"root_action_id"), v_1130, v_1137, (V)false}, new Results(R.Get(v_902, (V)"damage_type"))));
}
}
var values_1139 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1140 = v_904;
V key_1141 = (V)"cause";
R.Set(obj_1140, key_1141, values_1139[0]);
return Results.Empty;
}));
V v_1142 = V.Nil;
v_1142 = new V((RuleFunction)(args_1143 => {
V v_1144 = args_1143[0];
V v_1145 = args_1143[1];
R.Call(R.Get(v_64, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_62, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_60, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_66, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_68, (V)"prune"), R.Join(new V[] {}, new Results(v_1144)));
var values_1146 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_83, v_1145)}, new Results((V)"missing boon resolution context"))));
V v_1147 = values_1146[0];
var values_1148 = R.Join(new V[] {}, new Results((V)0.0d));
V v_1149 = values_1148[0];
while (R.Truth(R.Or((V)(R.Get(v_1147, (V)"event_head") <= (V)R.Length(R.Get(v_1147, (V)"events"))), () => (V)(R.Get(v_1147, (V)"effect_head") <= (V)R.Length(R.Get(v_1147, (V)"effects")))))) {
var values_1150 = R.Join(new V[] {}, new Results((V)(v_1149 + (V)1.0d)));
v_1149 = values_1150[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_1149 <= (V)1000000.0d)}, new Results((V)"boon effect resolution exceeded diagnostic limit")));
if (R.Truth((V)(R.Get(v_1147, (V)"event_head") <= (V)R.Length(R.Get(v_1147, (V)"events"))))) {
var values_1151 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1147, (V)"events"), R.Get(v_1147, (V)"event_head"))));
V v_1152 = values_1151[0];
var values_1153 = R.Join(new V[] {}, new Results((V)(R.Get(v_1147, (V)"event_head") + (V)1.0d)));
var obj_1154 = v_1147;
V key_1155 = (V)"event_head";
R.Set(obj_1154, key_1155, values_1153[0]);
if (R.Truth(R.And((V)(R.Get(v_1152, (V)"kind") == (V)"Moved"), () => (V)(R.Get(v_1152, (V)"source_id") == R.Get(v_1144, (V)"player_id"))))) {
var values_1156 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1144))));
V v_1157 = values_1156[0];
if (R.Truth(R.And(R.Get(v_1157, (V)"boons"), () => R.Get(R.Get(v_1157, (V)"boons"), (V)"core:boon/ring_of_reversal")))) {
foreach (var row_1158 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1144, (V)"entities"))))))) {
V v_1159 = row_1158[0];
V v_1160 = row_1158[1];
var values_1161 = R.Join(new V[] {}, new Results(R.Get(v_1160, (V)"pending_attack")));
V v_1162 = values_1161[0];
if (R.Truth(R.And(v_1162, () => (V)(R.Get(v_1162, (V)"floor_id") == R.Get(v_1152, (V)"floor_id"))))) {
var values_1163 = R.Join(new V[] {(V)false}, new Results((V)false));
V v_1164 = values_1163[0];
V v_1165 = values_1163[1];
foreach (var row_1166 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1162, (V)"target_cells"))))))) {
V v_1167 = row_1166[0];
V v_1168 = row_1166[1];
if (R.Truth(R.And((V)(R.Get(v_1168, (V)"x") == R.Get(v_1152, (V)"from_x")), () => (V)(R.Get(v_1168, (V)"y") == R.Get(v_1152, (V)"from_y"))))) {
var values_1169 = R.Join(new V[] {}, new Results((V)true));
v_1164 = values_1169[0];
}
if (R.Truth(R.And((V)(R.Get(v_1168, (V)"x") == R.Get(v_1152, (V)"x")), () => (V)(R.Get(v_1168, (V)"y") == R.Get(v_1152, (V)"y"))))) {
var values_1170 = R.Join(new V[] {}, new Results((V)true));
v_1165 = values_1170[0];
}
}
if (R.Truth(R.And(v_1164, () => (V)(!R.Truth(v_1165))))) {
var values_1171 = R.Join(new V[] {}, new Results(R.Get(v_1160, (V)"id")));
var obj_1172 = v_1157;
V key_1173 = (V)"reversal_target_id";
R.Set(obj_1172, key_1173, values_1171[0]);
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, (V)"RingReversalPrimed", R.Table(new Entry((V)"target_id", R.Get(v_1160, (V)"id")), new Entry((V)"floor_id", R.Get(v_1152, (V)"floor_id")), new Entry((V)"x", R.Get(v_1152, (V)"x")), new Entry((V)"y", R.Get(v_1152, (V)"y"))), R.Get(v_1157, (V)"id")}, new Results(R.Get(v_1152, (V)"action_id"))));
break;
}
}
}
}
}
R.Call(R.Get(v_22, (V)"on_event"), R.Join(new V[] {v_1144, v_1152}, new Results(new V((RuleFunction)(args_1174 => {
V v_1175 = args_1174[0];
V v_1176 = args_1174[1];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1175, v_1176, R.Get(v_1144, (V)"player_id")}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
})))));
if (R.Truth((V)(!R.Truth(R.Get(v_1152, (V)"class_generated"))))) {
R.Call(R.Get(v_4, (V)"on_event"), R.Join(new V[] {v_1144, v_1152, new V((RuleFunction)(args_1177 => {
V v_1178 = args_1177[0];
V v_1179 = args_1177[1];
var values_1180 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"activation", v_1178), new Entry((V)"effect", v_1179))));
var obj_1181 = R.Get(v_1147, (V)"effects");
V key_1182 = (V)((V)R.Length(R.Get(v_1147, (V)"effects")) + (V)1.0d);
R.Set(obj_1181, key_1182, values_1180[0]);
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1183 => {
V v_1184 = args_1183[0];
var values_1185 = R.Join(new V[] {}, R.Call(v_806, R.Join(new V[] {v_1184}, new Results(R.Table()))));
var obj_1186 = v_1147;
V key_1187 = (V)"cause";
R.Set(obj_1186, key_1187, values_1185[0]);
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, (V)"BoonActivated", R.Table(new Entry((V)"boon_id", R.Get(v_1184, (V)"boon_id")), new Entry((V)"activation_id", R.Get(v_1184, (V)"id")), new Entry((V)"trigger_event_id", R.Get(v_1152, (V)"id")), new Entry((V)"power", R.Get(v_1184, (V)"power")), new Entry((V)"x", R.Or(R.Get(v_1152, (V)"x"), () => R.Get(v_1152, (V)"from_x"))), new Entry((V)"y", R.Or(R.Get(v_1152, (V)"y"), () => R.Get(v_1152, (V)"from_y"))), new Entry((V)"floor_id", R.Get(v_1152, (V)"floor_id")), new Entry((V)"pair_target_id", R.Get(v_1184, (V)"pair_target_id"))), R.Get(v_1184, (V)"owner_id")}, new Results(R.Get(v_1184, (V)"root_action_id"))));
var values_1188 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1189 = v_1147;
V key_1190 = (V)"cause";
R.Set(obj_1189, key_1190, values_1188[0]);
return Results.Empty;
})))));
}
var values_1191 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"chain_id", R.Get(v_1152, (V)"chain_id")), new Entry((V)"root_action_id", R.Get(v_1152, (V)"root_action_id")), new Entry((V)"parent_id", R.Get(v_1152, (V)"id")), new Entry((V)"ancestry", R.Get(v_1152, (V)"ancestry")), new Entry((V)"proc_coefficient", R.Get(v_1152, (V)"proc_coefficient")), new Entry((V)"source_faction", R.Get(v_1152, (V)"source_faction")), new Entry((V)"source_position", R.Get(v_1152, (V)"source_position")), new Entry((V)"class_generated", R.Get(v_1152, (V)"class_generated")))));
var obj_1192 = v_1147;
V key_1193 = (V)"cause";
R.Set(obj_1192, key_1193, values_1191[0]);
if (R.Truth(R.Or(R.Or((V)(R.Get(v_1152, (V)"kind") == (V)"Died"), () => (V)(R.Get(v_1152, (V)"kind") == (V)"WindupCancelled")), () => (V)(R.Get(v_1152, (V)"kind") == (V)"FragileCollapsed")))) {
R.Call(R.Get(v_56, (V)"prune_corpses"), R.Join(new V[] {}, new Results(v_1144)));
R.Call(R.Get(v_64, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_62, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_60, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_66, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_68, (V)"prune"), R.Join(new V[] {}, new Results(v_1144)));
}
if (R.Truth((V)(R.Get(v_1152, (V)"kind") == (V)"Died"))) {
R.Call(R.Get(v_56, (V)"split"), R.Join(new V[] {v_1144, v_1152, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144))).First, new V((RuleFunction)(args_1194 => {
V v_1195 = args_1194[0];
V v_1196 = args_1194[1];
return R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144))).First, v_1195}, new Results(v_1196))).First, () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1144, R.Get(v_1144, (V)"active_floor_id"), v_1195, v_1196, V.Nil}, new Results((V)false))).First)))));
return Results.Empty;
})), R.Get(v_74, (V)"spawn_enemy")}, new Results(new V((RuleFunction)(args_1197 => {
V v_1198 = args_1197[0];
V v_1199 = args_1197[1];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1198, v_1199, R.Get(v_1152, (V)"target_id")}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
})))));
{ double end_1202 = R.Num((V)1.0d), step_1203 = R.Num((-(V)1.0d));
for (double i_1201 = R.Num((V)R.Length(R.Or(R.Get(v_1144, (V)"enemy_snares"), () => R.Table()))); step_1203 > 0 ? i_1201 <= end_1202 : i_1201 >= end_1202; i_1201 += step_1203) {
V v_1200 = i_1201;
if (R.Truth((V)(R.Get(R.Get(R.Get(v_1144, (V)"enemy_snares"), v_1200), (V)"source_id") == R.Get(v_1152, (V)"target_id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_1144, (V)"enemy_snares")}, new Results(v_1200)));
}
}
}
} else if (R.Truth((V)(R.Get(v_1152, (V)"kind") == (V)"TileEntered"))) {
var values_1204 = R.Join(new V[] {}, new Results(R.And(R.Get(v_1144, (V)"class_special"), () => R.Get(R.Get(v_1144, (V)"class_special"), (V)"snare"))));
V v_1205 = values_1204[0];
var values_1206 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1144}, new Results(R.Get(v_1152, (V)"source_id")))));
V v_1207 = values_1206[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.Get(v_1152, (V)"forced"), () => v_1205), () => v_1207), () => (V)(R.Get(v_1207, (V)"controller") == (V)"ai")), () => (V)(R.Get(v_1207, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_1205, (V)"floor_id") == R.Get(v_1152, (V)"floor_id"))), () => (V)(R.Get(v_1205, (V)"x") == R.Get(v_1152, (V)"x"))), () => (V)(R.Get(v_1205, (V)"y") == R.Get(v_1152, (V)"y"))))) {
var values_1208 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1209 = R.Get(v_1144, (V)"class_special");
V key_1210 = (V)"snare";
R.Set(obj_1209, key_1210, values_1208[0]);

var values_1211 = R.Join(new V[] {}, new Results((V)true));
var obj_1212 = v_1207;
V key_1213 = (V)"skip_next_turn";
R.Set(obj_1212, key_1213, values_1211[0]);
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, (V)"SnareTriggered", R.Table(new Entry((V)"floor_id", R.Get(v_1152, (V)"floor_id")), new Entry((V)"x", R.Get(v_1152, (V)"x")), new Entry((V)"y", R.Get(v_1152, (V)"y")), new Entry((V)"target_id", R.Get(v_1207, (V)"id"))), R.Get(v_1144, (V)"player_id")}, new Results(R.Get(v_1152, (V)"action_id"))));
R.Call(v_794, R.Join(new V[] {v_1144, v_1145, R.Get(v_1144, (V)"player_id"), R.Get(v_1152, (V)"action_id"), v_1207, (V)3.0d, (V)"physical"}, new Results((V)"player")));
}
{ double end_1216 = R.Num((V)1.0d), step_1217 = R.Num((-(V)1.0d));
for (double i_1215 = R.Num((V)R.Length(R.Or(R.Get(v_1144, (V)"enemy_snares"), () => R.Table()))); step_1217 > 0 ? i_1215 <= end_1216 : i_1215 >= end_1216; i_1215 += step_1217) {
V v_1214 = i_1215;
var values_1218 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1144, (V)"enemy_snares"), v_1214)));
V v_1219 = values_1218[0];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_1219, (V)"floor_id") == R.Get(v_1152, (V)"floor_id")), () => (V)(R.Get(v_1219, (V)"x") == R.Get(v_1152, (V)"x"))), () => (V)(R.Get(v_1219, (V)"y") == R.Get(v_1152, (V)"y"))), () => (V)(R.Get(v_1219, (V)"source_id") != R.Get(v_1152, (V)"source_id"))))) {
var values_1220 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1144}, new Results(R.Get(v_1152, (V)"source_id")))));
V v_1221 = values_1220[0];
if (R.Truth(R.And(R.And(v_1221, () => R.Get(v_1221, (V)"hp")), () => (V)(R.Get(v_1221, (V)"hp") > (V)0.0d)))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_1144, (V)"enemy_snares")}, new Results(v_1214)));
var values_1222 = R.Join(new V[] {}, new Results((V)false));
V v_1223 = values_1222[0];
if (R.Truth((V)(R.Get(v_1221, (V)"id") == R.Get(v_1144, (V)"player_id")))) {
foreach (var row_1224 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1144, (V)"schedule"))))))) {
V v_1225 = row_1224[0];
V v_1226 = row_1224[1];
if (R.Truth(R.And((V)(R.Get(v_1226, (V)"kind") == (V)"actor_ready"), () => (V)(R.Get(v_1226, (V)"actor_id") == R.Get(v_1221, (V)"id"))))) {
var values_1227 = R.Join(new V[] {}, new Results((V)(R.Get(v_1226, (V)"due") + R.Get(v_54, (V)"snare_delay"))));
var obj_1228 = v_1226;
V key_1229 = (V)"due";
R.Set(obj_1228, key_1229, values_1227[0]);

var values_1230 = R.Join(new V[] {}, new Results((V)true));
v_1223 = values_1230[0];
break;
}
}
if (R.Truth(v_1223)) {
R.Call(R.Get(v_38, (V)"sort"), R.Join(new V[] {}, new Results(v_1144)));
}
}
if (R.Truth((V)(!R.Truth(v_1223)))) {
var values_1231 = R.Join(new V[] {}, new Results(R.Get(v_54, (V)"snare_delay")));
var obj_1232 = v_1221;
V key_1233 = (V)"snare_delay";
R.Set(obj_1232, key_1233, values_1231[0]);
}
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, (V)"EnemySnareTriggered", R.Table(new Entry((V)"floor_id", R.Get(v_1219, (V)"floor_id")), new Entry((V)"x", R.Get(v_1219, (V)"x")), new Entry((V)"y", R.Get(v_1219, (V)"y")), new Entry((V)"target_id", R.Get(v_1221, (V)"id"))), R.Get(v_1219, (V)"source_id")}, new Results(R.Get(v_1152, (V)"action_id"))));
R.Call(v_794, R.Join(new V[] {v_1144, v_1145, R.Get(v_1219, (V)"source_id"), R.Get(v_1152, (V)"action_id"), v_1221, (V)1.0d, (V)"physical"}, new Results((V)"enemy")));
}
}
}
}
}
R.Call(R.Get(v_66, (V)"on_event"), R.Join(new V[] {v_1144, v_1152, new V((RuleFunction)(args_1234 => {
V v_1235 = args_1234[0];
V v_1236 = args_1234[1];
V v_1237 = args_1234[2];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1235, v_1236, v_1237}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1238 => {
V v_1239 = args_1238[0];
V v_1240 = args_1238[1];
var values_1241 = R.Join(new V[] {}, new Results((V)false));
V v_1242 = values_1241[0];
foreach (var row_1243 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1144, (V)"schedule"))))))) {
V v_1244 = row_1243[0];
V v_1245 = row_1243[1];
if (R.Truth(R.And((V)(R.Get(v_1245, (V)"kind") == (V)"actor_ready"), () => (V)(R.Get(v_1245, (V)"actor_id") == R.Get(v_1239, (V)"id"))))) {
var values_1246 = R.Join(new V[] {}, new Results((V)(R.Get(v_1245, (V)"due") + v_1240)));
var obj_1247 = v_1245;
V key_1248 = (V)"due";
R.Set(obj_1247, key_1248, values_1246[0]);

var values_1249 = R.Join(new V[] {}, new Results((V)true));
v_1242 = values_1249[0];

break;
}
}
if (R.Truth(v_1242)) {
R.Call(R.Get(v_38, (V)"sort"), R.Join(new V[] {}, new Results(v_1144)));
} else {
var values_1250 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_1239, (V)"snare_delay"), () => (V)0.0d) + v_1240)));
var obj_1251 = v_1239;
V key_1252 = (V)"snare_delay";
R.Set(obj_1251, key_1252, values_1250[0]);
}
return Results.Empty;
})))));
R.Call(R.Get(v_36, (V)"on_event"), R.Join(new V[] {v_1144, v_1152}, new Results(new V((RuleFunction)(args_1253 => {
V v_1254 = args_1253[0];
V v_1255 = args_1253[1];
V v_1256 = args_1253[2];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1254, v_1255, v_1256}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
})))));
R.Call(R.Get(v_50, (V)"on_event"), R.Join(new V[] {v_1144, v_1152, new V((RuleFunction)(args_1257 => {
V v_1258 = args_1257[0];
V v_1259 = args_1257[1];
V v_1260 = args_1257[2];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1258, v_1259, v_1260}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1261 => {
V v_1262 = args_1261[0];
V v_1263 = args_1261[1];
V v_1264 = args_1261[2];
V v_1265 = args_1261[3];
V v_1266 = args_1261[4];
R.Call(R.Get(v_36, (V)"spawn_currency"), R.Join(new V[] {v_1144, v_1262, v_1263, v_1264, v_1265, new V((RuleFunction)(args_1267 => {
V v_1268 = args_1267[0];
V v_1269 = args_1267[1];
V v_1270 = args_1267[2];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1268, v_1269, v_1270}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
}))}, new Results(v_1266)));
return Results.Empty;
})))));
R.Call(R.Get(v_52, (V)"on_event"), R.Join(new V[] {v_1144, v_1152}, new Results(new V((RuleFunction)(args_1271 => {
V v_1272 = args_1271[0];
V v_1273 = args_1271[1];
V v_1274 = args_1271[2];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1272, v_1273, v_1274}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
})))));
R.Call(R.Get(v_72, (V)"on_event"), R.Join(new V[] {v_1144, v_1152, new V((RuleFunction)(args_1275 => {
V v_1276 = args_1275[0];
V v_1277 = args_1275[1];
V v_1278 = args_1275[2];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1276, v_1277, v_1278}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1279 => {
V v_1280 = args_1279[0];
V v_1281 = args_1279[1];
V v_1282 = args_1279[2];
V v_1283 = args_1279[3];
return R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"spawn_chest"), R.Join(new V[] {v_1144, v_1280, v_1281, v_1282}, new Results(v_1283))));
return Results.Empty;
})))));
var values_1284 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_1147, (V)"terrain_contact"), () => R.Table())));
var obj_1285 = v_1147;
V key_1286 = (V)"terrain_contact";
R.Set(obj_1285, key_1286, values_1284[0]);
R.Call(R.Get(v_46, (V)"on_event"), R.Join(new V[] {v_1144, v_1152, R.Get(v_1147, (V)"terrain_contact"), new V((RuleFunction)(args_1287 => {
V v_1288 = args_1287[0];
V v_1289 = args_1287[1];
V v_1290 = args_1287[2];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1288, v_1289, v_1290}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1291 => {
V v_1292 = args_1291[0];
V v_1293 = args_1291[1];
V v_1294 = args_1291[2];
return R.Join(new V[] {}, R.Call(v_660, R.Join(new V[] {v_1144, v_1145, v_1292, v_1293, v_1294, (V)0.0d}, new Results(R.Get(v_1152, (V)"action_id")))));
return Results.Empty;
})))));
R.Call(R.Get(v_44, (V)"on_event"), R.Join(new V[] {v_1144, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144))).First, v_1152, R.Get(v_1147, (V)"terrain_contact"), new V((RuleFunction)(args_1295 => {
V v_1296 = args_1295[0];
return R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1144}, new Results(v_1296))));
return Results.Empty;
})), new V((RuleFunction)(args_1297 => {
V v_1298 = args_1297[0];
V v_1299 = args_1297[1];
V v_1300 = args_1297[2];
V v_1301 = args_1297[3];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1298, v_1299, v_1300}, new Results(v_1301)));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1302 => {
V v_1303 = args_1302[0];
V v_1304 = args_1302[1];
V v_1305 = args_1302[2];
V v_1306 = args_1302[3];
V v_1307 = args_1302[4];
V v_1308 = args_1302[5];
R.Call(v_794, R.Join(new V[] {v_1144, v_1145, v_1304, v_1305, v_1303, v_1306, v_1307}, new Results(v_1308)));
return Results.Empty;
})))));
R.Call(R.Get(v_40, (V)"on_event"), R.Join(new V[] {v_1144, v_1152, new V((RuleFunction)(args_1309 => {
V v_1310 = args_1309[0];
V v_1311 = args_1309[1];
V v_1312 = args_1309[2];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1310, v_1311, v_1312}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1313 => {
V v_1314 = args_1313[0];
V v_1315 = args_1313[1];
V v_1316 = args_1313[2];
V v_1317 = args_1313[3];
return R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"spawn_chest"), R.Join(new V[] {v_1144, v_1314, v_1315, v_1316}, new Results(v_1317))));
return Results.Empty;
})))));
R.Call(R.Get(v_30, (V)"on_event"), R.Join(new V[] {v_1144, v_1152}, new Results(new V((RuleFunction)(args_1318 => {
V v_1319 = args_1318[0];
V v_1320 = args_1318[1];
V v_1321 = args_1318[2];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1319, v_1320, v_1321}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
})))));
R.Call(R.Get(v_10, (V)"observe"), R.Join(new V[] {v_1144, v_1152}, new Results(new V((RuleFunction)(args_1322 => {
V v_1323 = args_1322[0];
V v_1324 = args_1322[1];
V v_1325 = args_1322[2];
R.Call(v_95, R.Join(new V[] {v_1144, v_1145, v_1323, v_1324, v_1325}, new Results(R.Get(v_1152, (V)"action_id"))));
return Results.Empty;
})))));
var values_1326 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1327 = v_1147;
V key_1328 = (V)"cause";
R.Set(obj_1327, key_1328, values_1326[0]);
} else {
var values_1329 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1147, (V)"effects"), R.Get(v_1147, (V)"effect_head"))));
V v_1330 = values_1329[0];
var values_1331 = R.Join(new V[] {}, new Results((V)(R.Get(v_1147, (V)"effect_head") + (V)1.0d)));
var obj_1332 = v_1147;
V key_1333 = (V)"effect_head";
R.Set(obj_1332, key_1333, values_1331[0]);
R.Call(v_897, R.Join(new V[] {v_1144, v_1145, R.Get(v_1330, (V)"activation")}, new Results(R.Get(v_1330, (V)"effect"))));
}
}
R.Call(R.Get(v_64, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_62, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_60, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_66, (V)"prune"), R.Join(new V[] {v_1144}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1144)))));
R.Call(R.Get(v_68, (V)"prune"), R.Join(new V[] {}, new Results(v_1144)));
return Results.Empty;
}));
V v_1334 = V.Nil;
v_1334 = new V((RuleFunction)(args_1335 => {
V v_1336 = args_1335[0];
V v_1337 = args_1335[1];
V v_1338 = args_1335[2];
V v_1339 = args_1335[3];
V v_1340 = args_1335[4];
V v_1341 = args_1335[5];
V v_1342 = args_1335[6];
V v_1343 = args_1335[7];
V v_1344 = args_1335[8];
var values_1345 = R.Join(new V[] {}, new Results(R.Get(v_83, v_1337)));
V v_1346 = values_1345[0];
var values_1347 = R.Join(new V[] {}, new Results(R.And(v_1346, () => R.Get(v_1346, (V)"cause"))));
V v_1348 = values_1347[0];
if (R.Truth(v_1346)) {
var values_1349 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"chain_id", v_1339), new Entry((V)"root_action_id", v_1339), new Entry((V)"ancestry", R.Table()), new Entry((V)"proc_coefficient", R.Call(R.Get(v_12, (V)"ability_proc_coefficient"), R.Join(new V[] {}, new Results(v_1340))).First), new Entry((V)"class_generated", R.And(v_1344, () => R.Get(v_1344, (V)"class_generated"))))));
var obj_1350 = v_1346;
V key_1351 = (V)"cause";
R.Set(obj_1350, key_1351, values_1349[0]);
}
var values_1352 = R.Join(new V[] {}, new Results(R.Get(v_1338, (V)"position")));
V v_1353 = values_1352[0];
var values_1354 = Results.Empty;
V v_1355 = values_1354[0];
if (R.Truth(R.And((V)(R.Get(v_1338, (V)"id") == R.Get(v_1336, (V)"player_id")), () => (V)(!R.Truth(R.And(v_1344, () => R.Get(v_1344, (V)"class_generated"))))))) {
if (R.Truth(R.And(v_1346, () => (V)(!R.Truth(R.Get(v_1346, (V)"dice_checked")))))) {
var values_1356 = R.Join(new V[] {}, new Results((V)true));
var obj_1357 = v_1346;
V key_1358 = (V)"dice_checked";
R.Set(obj_1357, key_1358, values_1356[0]);
var values_1359 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"roll_dice"), R.Join(new V[] {v_1336, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1336))).First}, new Results(v_1339))));
var obj_1360 = v_1346;
V key_1361 = (V)"fate_roll";
R.Set(obj_1360, key_1361, values_1359[0]);
if (R.Truth(R.Get(v_1346, (V)"fate_roll"))) {
var values_1362 = R.Join(new V[] {}, new Results(R.Get(v_1346, (V)"fate_roll")));
V v_1363 = values_1362[0];
R.Call(v_95, R.Join(new V[] {v_1336, v_1337, (V)"FateRolled", R.Table(new Entry((V)"boon_id", (V)"core:boon/fateful_dice"), new Entry((V)"face", R.Get(v_1363, (V)"face")), new Entry((V)"face_name", R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"faces"), R.Get(v_1363, (V)"face"))), new Entry((V)"power", R.Get(v_1363, (V)"power")), new Entry((V)"binding_id", R.Concat((V)"core:ui/fate_", R.Concat(R.Get(v_1363, (V)"face"), (V)"_icon"))), new Entry((V)"floor_id", R.Get(v_1353, (V)"floor_id")), new Entry((V)"x", R.Get(v_1353, (V)"x")), new Entry((V)"y", R.Get(v_1353, (V)"y"))), R.Get(v_1338, (V)"id")}, new Results(v_1339)));
}
}
var values_1364 = R.Join(new V[] {}, new Results(R.And(v_1346, () => R.Get(v_1346, (V)"fate_roll"))));
v_1355 = values_1364[0];
if (R.Truth(R.And(v_1355, () => (V)(R.Get(v_1355, (V)"face") == (V)2.0d)))) {
var values_1365 = R.Join(new V[] {}, new Results((V)(v_1342 + R.Get(v_1355, (V)"power"))));
v_1342 = values_1365[0];
}
if (R.Truth(R.And(R.And(v_1346, () => R.Get(v_1346, (V)"vow")), () => (V)(!R.Truth(R.Get(v_1346, (V)"vow_spent")))))) {
var values_1366 = R.Join(new V[] {}, new Results((V)(v_1342 + R.Get(R.Get(v_1346, (V)"vow"), (V)"bonus"))));
v_1342 = values_1366[0];
var values_1367 = R.Join(new V[] {}, new Results((V)true));
var obj_1368 = v_1346;
V key_1369 = (V)"vow_spent";
R.Set(obj_1368, key_1369, values_1367[0]);
}
var values_1370 = R.Join(new V[] {}, R.Call(R.Get(v_52, (V)"focus"), R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1336))))));
V v_1371 = values_1370[0];
if (R.Truth((V)(!R.Truth(R.And(v_1344, () => R.Get(v_1344, (V)"focus_included")))))) {
var values_1372 = R.Join(new V[] {}, new Results((V)(v_1342 + v_1371)));
v_1342 = values_1372[0];
}
if (R.Truth((V)(v_1371 > (V)0.0d))) {
R.Call(v_95, R.Join(new V[] {v_1336, v_1337, (V)"SupplyFocusSpent", R.Table(new Entry((V)"bonus", v_1371), new Entry((V)"floor_id", R.Get(v_1353, (V)"floor_id")), new Entry((V)"x", R.Get(v_1353, (V)"x")), new Entry((V)"y", R.Get(v_1353, (V)"y"))), R.Get(v_1338, (V)"id")}, new Results(v_1339)));
}
}
var values_1373 = Results.Empty;
V v_1374 = values_1373[0];
if (R.Truth(R.And(v_1355, () => (V)(R.Get(v_1355, (V)"face") == (V)8.0d)))) {
var values_1375 = R.Join(new V[] {}, new Results(v_1341));
v_1374 = values_1375[0];

var values_1376 = R.Join(new V[] {}, new Results(R.Table()));
v_1341 = values_1376[0];

var values_1377 = R.Join(new V[] {}, new Results((V)0.0d));
v_1342 = values_1377[0];
}
var values_1378 = R.Join(new V[] {}, new Results((V)false));
V v_1379 = values_1378[0];
if (R.Truth(R.And(R.And(R.And((V)(!R.Truth(v_1374)), () => (V)(R.Get(v_1338, (V)"controller") == (V)"player")), () => R.Call(R.Get(v_14, (V)"is_attack"), R.Join(new V[] {}, new Results(v_1340))).First), () => (V)(R.Or(R.Get(v_1338, (V)"crit_chance_ppm"), () => (V)0.0d) > (V)0.0d)))) {
var values_1380 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_34, (V)"next_int"), R.Join(new V[] {R.Get(v_1336, (V)"rng")}, new Results((V)1000000.0d))).First <= R.Get(v_1338, (V)"crit_chance_ppm"))));
v_1379 = values_1380[0];
if (R.Truth(v_1379)) {
var values_1381 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)(v_1342 * R.Get(v_1338, (V)"crit_damage_percent")) / (V)100.0d) + (V)0.5d)))));
v_1342 = values_1381[0];
}
}
var values_1382 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_1353, (V)"floor_id")), new Entry((V)"from_x", R.Get(v_1353, (V)"x")), new Entry((V)"from_y", R.Get(v_1353, (V)"y")), new Entry((V)"ability_id", v_1340), new Entry((V)"target_cells", v_1341), new Entry((V)"raw_damage", v_1342), new Entry((V)"fate_face", R.Or(R.And(v_1355, () => R.Get(v_1355, (V)"face")), () => V.Nil)), new Entry((V)"fate_miss", R.Or(R.And(v_1374, () => (V)true), () => V.Nil)), new Entry((V)"aim_cells", v_1374))));
V v_1383 = values_1382[0];
if (R.Truth(v_1379)) {
var values_1384 = R.Join(new V[] {}, new Results((V)true));
var obj_1385 = v_1383;
V key_1386 = (V)"critical";
R.Set(obj_1385, key_1386, values_1384[0]);
}
if (R.Truth(R.And((V)((V)R.Length(v_1341) == (V)1.0d), () => (V)(v_1340 == (V)"core:ability/basic_attack")))) {
var values_1387 = R.Join(new V[] {R.Get(R.Get(v_1341, (V)1.0d), (V)"x")}, new Results(R.Get(R.Get(v_1341, (V)1.0d), (V)"y")));
var obj_1388 = v_1383;
V key_1389 = (V)"target_x";
var obj_1390 = v_1383;
V key_1391 = (V)"target_y";
R.Set(obj_1388, key_1389, values_1387[0]);
R.Set(obj_1390, key_1391, values_1387[1]);
}
if (R.Truth(v_1344)) {
foreach (var row_1392 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1344)))))) {
V v_1393 = row_1392[0];
V v_1394 = row_1392[1];
if (R.Truth((V)(v_1393 != (V)"focus_included"))) {
var values_1395 = R.Join(new V[] {}, new Results(v_1394));
var obj_1396 = v_1383;
V key_1397 = v_1393;
R.Set(obj_1396, key_1397, values_1395[0]);
}
}
}
if (R.Truth((V)((V)R.Length(R.Or(R.Get(v_1336, (V)"idea_webs"), () => R.Table())) > (V)0.0d))) {
var values_1398 = R.Join(new V[] {}, new Results(R.Table()));
var obj_1399 = v_1383;
V key_1400 = (V)"web_cut_cells";
R.Set(obj_1399, key_1400, values_1398[0]);
foreach (var row_1401 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_1341)))))) {
V v_1402 = row_1401[0];
V v_1403 = row_1401[1];
var values_1404 = R.Join(new V[] {}, new Results(R.Or((V)(!R.Truth(v_1343)), () => R.Call(R.Get(v_12, (V)"projectile_passable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1336))).First, R.Get(v_1403, (V)"x"), R.Get(v_1403, (V)"y")}, new Results(v_1336))).First)));
V v_1405 = values_1404[0];
if (R.Truth(R.And((V)(!R.Truth(v_1405)), () => (V)(v_1343 != (V)"area")))) {
break;
}
if (R.Truth(v_1405)) {
var values_1406 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_1403))));
var obj_1407 = R.Get(v_1383, (V)"web_cut_cells");
V key_1408 = (V)((V)R.Length(R.Get(v_1383, (V)"web_cut_cells")) + (V)1.0d);
R.Set(obj_1407, key_1408, values_1406[0]);
}
if (R.Truth(R.And((V)(v_1343 == (V)true), () => R.Call(v_144, R.Join(new V[] {v_1336, R.Get(v_1403, (V)"floor_id"), R.Get(v_1403, (V)"x"), R.Get(v_1403, (V)"y"), R.Get(v_1338, (V)"id")}, new Results((V)true))).First))) {
break;
}
}
}
R.Call(v_95, R.Join(new V[] {v_1336, v_1337, (V)"AttackPerformed", v_1383, R.Get(v_1338, (V)"id")}, new Results(v_1339)));
R.Call(v_704, R.Join(new V[] {v_1336, v_1337, v_1338, v_1339, v_1341, v_1342, v_1343, R.And(v_1344, () => R.Get(v_1344, (V)"damage_type")), v_1379, (V)true, v_1340}, new Results(v_1355)));
if (R.Truth(R.Call(R.Get(v_4, (V)"active"), R.Join(new V[] {}, new Results(v_1336))).First)) {
R.Call(v_95, R.Join(new V[] {v_1336, v_1337, (V)"AbilityUsed", v_1383, R.Get(v_1338, (V)"id")}, new Results(v_1339)));
}
if (R.Truth(v_1346)) {
var values_1409 = R.Join(new V[] {}, new Results(v_1348));
var obj_1410 = v_1346;
V key_1411 = (V)"cause";
R.Set(obj_1410, key_1411, values_1409[0]);
}
return R.Join(new V[] {}, new Results(v_1383));
return Results.Empty;
}));
V v_1412 = V.Nil;
v_1412 = new V((RuleFunction)(args_1413 => {
V v_1414 = args_1413[0];
V v_1415 = args_1413[1];
V v_1416 = args_1413[2];
var values_1417 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1414}, new Results(R.Get(v_1415, (V)"actor_id")))));
V v_1418 = values_1417[0];
if (R.Truth(R.Or((V)(!R.Truth(v_1418)), () => (V)(R.Get(v_1418, (V)"hp") <= (V)0.0d)))) {
return Results.Empty;
}
if (R.Truth(R.Get(v_1418, (V)"snare_delay"))) {
var values_1419 = R.Join(new V[] {}, new Results(R.Get(v_1418, (V)"snare_delay")));
V v_1420 = values_1419[0];

var values_1421 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1422 = v_1418;
V key_1423 = (V)"snare_delay";
R.Set(obj_1422, key_1423, values_1421[0]);
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_1414}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)(R.Get(v_1414, (V)"clock") + v_1420)), new Entry((V)"actor_id", R.Get(v_1418, (V)"id")), new Entry((V)"is_player", (V)false)))));
return Results.Empty;
}
if (R.Truth(R.Get(v_1418, (V)"skip_next_turn"))) {
var values_1424 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1425 = v_1418;
V key_1426 = (V)"skip_next_turn";
R.Set(obj_1425, key_1426, values_1424[0]);
R.Call(v_95, R.Join(new V[] {v_1414, v_1416, (V)"SnareHeld", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_1418, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_1418, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_1418, (V)"position"), (V)"y")), new Entry((V)"target_id", R.Get(v_1418, (V)"id"))), R.Get(v_1414, (V)"player_id")}, new Results(V.Nil)));
var values_1427 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_1418, (V)"definition_id")))));
V v_1428 = values_1427[0];
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_1414}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)(R.Get(v_1414, (V)"clock") + R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {R.Get(v_1428, (V)"move_cost")}, new Results(R.Get(v_1418, (V)"speed")))).First)), new Entry((V)"actor_id", R.Get(v_1418, (V)"id")), new Entry((V)"is_player", (V)false)))));
return Results.Empty;
}
if (R.Truth(R.Get(v_1418, (V)"pending_attack"))) {
return Results.Empty;
}
if (R.Truth((V)(R.Or(R.Get(v_1418, (V)"stunned_until"), () => (V)0.0d) > R.Get(v_1414, (V)"clock")))) {
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_1414}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", R.Get(v_1418, (V)"stunned_until")), new Entry((V)"actor_id", R.Get(v_1418, (V)"id")), new Entry((V)"is_player", (V)false)))));
return Results.Empty;
}
var values_1429 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_1418, (V)"definition_id")))));
V v_1430 = values_1429[0];
var values_1431 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"choose"), R.Join(new V[] {v_1414}, new Results(v_1418))));
V v_1432 = values_1431[0];
var values_1433 = R.Join(new V[] {}, new Results(R.Get(v_1414, (V)"next_action_id")));
V v_1434 = values_1433[0];
var values_1435 = R.Join(new V[] {}, new Results((V)(v_1434 + (V)1.0d)));
var obj_1436 = v_1414;
V key_1437 = (V)"next_action_id";
R.Set(obj_1436, key_1437, values_1435[0]);
var values_1438 = R.Join(new V[] {}, new Results(R.Get(v_1418, (V)"position")));
V v_1439 = values_1438[0];
if (R.Truth(R.Get(v_1432, (V)"shield_dx"))) {
var values_1440 = R.Join(new V[] {R.Get(v_1432, (V)"shield_dx")}, new Results(R.Get(v_1432, (V)"shield_dy")));
var obj_1441 = R.Get(v_1418, (V)"ai_memory");
V key_1442 = (V)"shield_dx";
var obj_1443 = R.Get(v_1418, (V)"ai_memory");
V key_1444 = (V)"shield_dy";
R.Set(obj_1441, key_1442, values_1440[0]);
R.Set(obj_1443, key_1444, values_1440[1]);
var values_1445 = R.Join(new V[] {}, new Results((V)(R.Get(v_1432, (V)"kind") == (V)"windup")));
var obj_1446 = R.Get(v_1418, (V)"ai_memory");
V key_1447 = (V)"shield_lowered";
R.Set(obj_1446, key_1447, values_1445[0]);
}
if (R.Truth(R.Get(v_1432, (V)"alert"))) {
var values_1448 = R.Join(new V[] {}, new Results((V)true));
var obj_1449 = R.Get(v_1418, (V)"ai_memory");
V key_1450 = (V)"alerted";
R.Set(obj_1449, key_1450, values_1448[0]);
}
if (R.Truth((V)(R.Get(v_1432, (V)"kind") == (V)"windup"))) {
var values_1451 = R.Join(new V[] {R.Get(v_1432, (V)"cells")}, new Results(V.Nil));
V v_1452 = values_1451[0];
V v_1453 = values_1451[1];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_1432, (V)"advance"))), () => (V)(!R.Truth(R.Get(v_1432, (V)"support")))))) {
var values_1454 = R.Join(new V[] {}, R.Call(R.Get(v_60, (V)"route"), R.Join(new V[] {v_1414, v_1418, R.Get(v_1432, (V)"cells")}, new Results(R.Get(v_1432, (V)"ranged")))));
v_1452 = values_1454[0];
v_1453 = values_1454[1];
}
var values_1455 = R.Join(new V[] {}, new Results(v_1452));
var obj_1456 = v_1432;
V key_1457 = (V)"cells";
R.Set(obj_1456, key_1457, values_1455[0]);
if (R.Truth(v_1453)) {
R.Call(R.Get(v_60, (V)"consume"), R.Join(new V[] {v_1414}, new Results(v_1453)));
R.Call(v_95, R.Join(new V[] {v_1414, v_1416, (V)"MirrorReflected", R.Table(new Entry((V)"reflection", R.Call(v_84, R.Join(new V[] {}, new Results(v_1453))).First), new Entry((V)"target_cells", R.Call(v_84, R.Join(new V[] {}, new Results(v_1452))).First), new Entry((V)"floor_id", R.Get(v_1439, (V)"floor_id")), new Entry((V)"x", R.Get(v_1453, (V)"x")), new Entry((V)"y", R.Get(v_1453, (V)"y"))), R.Get(v_1418, (V)"id")}, new Results(v_1434)));
}
R.Call(R.Get(v_56, (V)"commit"), R.Join(new V[] {v_1414, v_1418}, new Results(v_1432)));
var values_1458 = R.Join(new V[] {}, new Results(R.And(R.Get(v_1414, (V)"class_special"), () => R.Get(R.Get(v_1414, (V)"class_special"), (V)"decoy"))));
V v_1459 = values_1458[0];
if (R.Truth(R.And(v_1459, () => (V)(R.Get(v_1459, (V)"floor_id") == R.Get(v_1439, (V)"floor_id"))))) {
foreach (var row_1460 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1432, (V)"cells"))))))) {
V v_1461 = row_1460[0];
V v_1462 = row_1460[1];
if (R.Truth(R.And((V)(R.Get(v_1462, (V)"x") == R.Get(v_1459, (V)"x")), () => (V)(R.Get(v_1462, (V)"y") == R.Get(v_1459, (V)"y"))))) {
var values_1463 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1464 = R.Get(v_1414, (V)"class_special");
V key_1465 = (V)"decoy";
R.Set(obj_1464, key_1465, values_1463[0]);
R.Call(v_95, R.Join(new V[] {v_1414, v_1416, (V)"DecoyTriggered", R.Table(new Entry((V)"floor_id", R.Get(v_1459, (V)"floor_id")), new Entry((V)"x", R.Get(v_1459, (V)"x")), new Entry((V)"y", R.Get(v_1459, (V)"y"))), R.Get(v_1418, (V)"id")}, new Results(v_1434)));
break;
}
}
}
if (R.Truth(R.Or(R.Or((V)(R.Get(v_1418, (V)"ai_id") == (V)"warden"), () => (V)(R.Get(v_1418, (V)"ai_id") == (V)"hollow_regent")), () => (V)(R.Get(v_1418, (V)"ai_id") == (V)"biome_elite")))) {
var values_1466 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(R.Get(v_1418, (V)"ai_memory"), (V)"attacks"), () => (V)0.0d) + (V)1.0d)));
var obj_1467 = R.Get(v_1418, (V)"ai_memory");
V key_1468 = (V)"attacks";
R.Set(obj_1467, key_1468, values_1466[0]);
}
var values_1469 = R.Join(new V[] {}, new Results((V)(R.Get(v_1414, (V)"clock") + R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {R.Get(v_1430, (V)"windup")}, new Results(R.Get(v_1418, (V)"speed")))).First)));
V v_1470 = values_1469[0];
var values_1471 = R.Join(new V[] {}, R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_1414}, new Results(R.Table(new Entry((V)"kind", (V)"impact"), new Entry((V)"due", v_1470), new Entry((V)"actor_id", R.Get(v_1418, (V)"id")), new Entry((V)"attack_id", v_1434))))));
V v_1472 = values_1471[0];
var values_1473 = R.Join(new V[] {}, new Results(R.Table()));
V v_1474 = values_1473[0];
foreach (var row_1475 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1432, (V)"cells"))))))) {
V v_1476 = row_1475[0];
V v_1477 = row_1475[1];
var values_1478 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_1439, (V)"floor_id")), new Entry((V)"x", R.Get(v_1477, (V)"x")), new Entry((V)"y", R.Get(v_1477, (V)"y")))));
var obj_1479 = v_1474;
V key_1480 = v_1476;
R.Set(obj_1479, key_1480, values_1478[0]);
}
var values_1481 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schedule_id", R.Get(v_1472, (V)"id")), new Entry((V)"sequence", R.Get(v_1472, (V)"seq")), new Entry((V)"action_id", v_1434), new Entry((V)"source_id", R.Get(v_1418, (V)"id")), new Entry((V)"floor_id", R.Get(v_1439, (V)"floor_id")), new Entry((V)"source_x", R.Get(v_1439, (V)"x")), new Entry((V)"source_y", R.Get(v_1439, (V)"y")), new Entry((V)"ability_id", R.Get(v_1432, (V)"ability_id")), new Entry((V)"target_cells", v_1474), new Entry((V)"ranged", R.Get(v_1432, (V)"ranged")), new Entry((V)"damage", (V)((V)(R.Get(v_1418, (V)"damage") + R.Or(R.Get(v_1432, (V)"damage_bonus"), () => (V)0.0d)) + R.Or(R.And(R.Call(R.Get(v_54, (V)"rally_source"), R.Join(new V[] {v_1414}, new Results(v_1418))).First, () => (V)1.0d), () => (V)0.0d))), new Entry((V)"due", v_1470), new Entry((V)"support", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1432, (V)"support")))).First), new Entry((V)"advance", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1432, (V)"advance")))).First), new Entry((V)"shove", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1432, (V)"shove")))).First), new Entry((V)"ember_landing", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1432, (V)"ember_landing")))).First), new Entry((V)"reflection", R.Call(v_84, R.Join(new V[] {}, new Results(v_1453))).First), new Entry((V)"sentinel_facing", R.Get(v_1432, (V)"sentinel_facing")), new Entry((V)"committed_time", R.Get(v_1414, (V)"clock")))));
var obj_1482 = v_1418;
V key_1483 = (V)"pending_attack";
R.Set(obj_1482, key_1483, values_1481[0]);
if (R.Truth(R.And(R.Get(v_1432, (V)"support"), () => (V)(R.Get(R.Get(v_1432, (V)"support"), (V)"kind") == (V)"revive")))) {
R.Call(R.Get(v_56, (V)"prune_corpses"), R.Join(new V[] {}, new Results(v_1414)));
}
if (R.Truth(R.Get(v_1432, (V)"clear_retaliate"))) {
var values_1484 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1485 = R.Get(v_1418, (V)"ai_memory");
V key_1486 = (V)"retaliate";
R.Set(obj_1485, key_1486, values_1484[0]);
}
R.Call(v_95, R.Join(new V[] {v_1414, v_1416, (V)"WindupStarted", R.Table(new Entry((V)"windup_id", R.Get(v_1472, (V)"id")), new Entry((V)"due", v_1470), new Entry((V)"ability_id", R.Get(v_1432, (V)"ability_id")), new Entry((V)"floor_id", R.Get(v_1439, (V)"floor_id")), new Entry((V)"from_x", R.Get(v_1439, (V)"x")), new Entry((V)"from_y", R.Get(v_1439, (V)"y")), new Entry((V)"target_cells", v_1474), new Entry((V)"shove", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1432, (V)"shove")))).First), new Entry((V)"support_kind", R.And(R.Get(v_1432, (V)"support"), () => R.Get(R.Get(v_1432, (V)"support"), (V)"kind"))), new Entry((V)"sentinel_facing", R.Get(v_1432, (V)"sentinel_facing")), new Entry((V)"sentinel_strike", R.And(R.Get(v_1432, (V)"sentinel_facing"), () => R.Get(R.Get(v_1418, (V)"ai_memory"), (V)"sentinel_strikes"))), new Entry((V)"reflection", R.Call(v_84, R.Join(new V[] {}, new Results(v_1453))).First), new Entry((V)"ember_landing", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1432, (V)"ember_landing")))).First)), R.Get(v_1418, (V)"id")}, new Results(v_1434)));
return Results.Empty;
}
var values_1487 = R.Join(new V[] {}, new Results((V)false));
V v_1488 = values_1487[0];
if (R.Truth((V)(R.Get(v_1432, (V)"kind") == (V)"move"))) {
var values_1489 = R.Join(new V[] {(V)(R.Get(v_1439, (V)"x") + R.Get(v_1432, (V)"dx"))}, new Results((V)(R.Get(v_1439, (V)"y") + R.Get(v_1432, (V)"dy"))));
V v_1490 = values_1489[0];
V v_1491 = values_1489[1];
var values_1492 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1414))));
V v_1493 = values_1492[0];
if (R.Truth(R.And(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_1493, v_1490}, new Results(v_1491))).First, () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1414, R.Get(v_1439, (V)"floor_id"), v_1490, v_1491, R.Get(v_1418, (V)"id")}, new Results((V)false))).First))))) {
var values_1494 = R.Join(new V[] {R.Get(v_1439, (V)"x")}, new Results(R.Get(v_1439, (V)"y")));
V v_1495 = values_1494[0];
V v_1496 = values_1494[1];
var values_1497 = R.Join(new V[] {v_1490}, new Results(v_1491));
var obj_1498 = v_1439;
V key_1499 = (V)"x";
var obj_1500 = v_1439;
V key_1501 = (V)"y";
R.Set(obj_1498, key_1499, values_1497[0]);
R.Set(obj_1500, key_1501, values_1497[1]);
var values_1502 = R.Join(new V[] {}, new Results((V)true));
v_1488 = values_1502[0];
var values_1503 = R.Join(new V[] {R.Get(v_1432, (V)"dx")}, new Results(R.Get(v_1432, (V)"dy")));
var obj_1504 = R.Get(v_1418, (V)"ai_memory");
V key_1505 = (V)"last_dx";
var obj_1506 = R.Get(v_1418, (V)"ai_memory");
V key_1507 = (V)"last_dy";
R.Set(obj_1504, key_1505, values_1503[0]);
R.Set(obj_1506, key_1507, values_1503[1]);
R.Call(v_95, R.Join(new V[] {v_1414, v_1416, (V)"Moved", R.Table(new Entry((V)"floor_id", R.Get(v_1439, (V)"floor_id")), new Entry((V)"from_x", v_1495), new Entry((V)"from_y", v_1496), new Entry((V)"x", v_1490), new Entry((V)"y", v_1491)), R.Get(v_1418, (V)"id")}, new Results(v_1434)));
R.Call(v_95, R.Join(new V[] {v_1414, v_1416, (V)"TileEntered", R.Table(new Entry((V)"floor_id", R.Get(v_1439, (V)"floor_id")), new Entry((V)"from_x", v_1495), new Entry((V)"from_y", v_1496), new Entry((V)"x", v_1490), new Entry((V)"y", v_1491)), R.Get(v_1418, (V)"id")}, new Results(v_1434)));
var values_1508 = R.Join(new V[] {}, new Results(R.And(R.Get(v_1414, (V)"class_special"), () => R.Get(R.Get(v_1414, (V)"class_special"), (V)"snare"))));
V v_1509 = values_1508[0];
if (R.Truth(R.And(R.And(R.And(v_1509, () => (V)(R.Get(v_1509, (V)"floor_id") == R.Get(v_1439, (V)"floor_id"))), () => (V)(R.Get(v_1509, (V)"x") == v_1490)), () => (V)(R.Get(v_1509, (V)"y") == v_1491)))) {
var values_1510 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1511 = R.Get(v_1414, (V)"class_special");
V key_1512 = (V)"snare";
R.Set(obj_1511, key_1512, values_1510[0]);
var values_1513 = R.Join(new V[] {}, new Results((V)true));
var obj_1514 = v_1418;
V key_1515 = (V)"skip_next_turn";
R.Set(obj_1514, key_1515, values_1513[0]);
R.Call(v_95, R.Join(new V[] {v_1414, v_1416, (V)"SnareTriggered", R.Table(new Entry((V)"floor_id", R.Get(v_1439, (V)"floor_id")), new Entry((V)"x", v_1490), new Entry((V)"y", v_1491), new Entry((V)"target_id", R.Get(v_1418, (V)"id"))), R.Get(v_1414, (V)"player_id")}, new Results(v_1434)));
R.Call(v_794, R.Join(new V[] {v_1414, v_1416, R.Get(v_1414, (V)"player_id"), v_1434, v_1418, (V)3.0d, (V)"physical"}, new Results((V)"player")));
if (R.Truth((V)(R.Get(v_1418, (V)"hp") <= (V)0.0d))) {
return Results.Empty;
}
}
} else {
R.Call(v_95, R.Join(new V[] {v_1414, v_1416, (V)"Waited", R.Table(new Entry((V)"floor_id", R.Get(v_1439, (V)"floor_id")), new Entry((V)"x", R.Get(v_1439, (V)"x")), new Entry((V)"y", R.Get(v_1439, (V)"y"))), R.Get(v_1418, (V)"id")}, new Results(v_1434)));
}
} else {
R.Call(v_95, R.Join(new V[] {v_1414, v_1416, (V)"Waited", R.Table(new Entry((V)"floor_id", R.Get(v_1439, (V)"floor_id")), new Entry((V)"x", R.Get(v_1439, (V)"x")), new Entry((V)"y", R.Get(v_1439, (V)"y"))), R.Get(v_1418, (V)"id")}, new Results(v_1434)));
}
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_1414}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)(R.Get(v_1414, (V)"clock") + R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {(V)(R.Get(v_1430, (V)"move_cost") + R.Or(R.And(v_1488, () => (V)(R.Call(R.Get(v_44, (V)"move_extra"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1414))).First, (V)"move"}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_1439, (V)"x")), new Entry((V)"y", R.Get(v_1439, (V)"y")), new Entry((V)"status", (V)"open"))))))).First + R.Call(R.Get(v_62, (V)"move_extra"), R.Join(new V[] {v_1414, (V)"move"}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_1439, (V)"x")), new Entry((V)"y", R.Get(v_1439, (V)"y")), new Entry((V)"status", (V)"open"))))))).First)), () => (V)0.0d))}, new Results(R.Get(v_1418, (V)"speed")))).First)), new Entry((V)"actor_id", R.Get(v_1418, (V)"id")), new Entry((V)"is_player", (V)false)))));
return Results.Empty;
}));
V v_1516 = V.Nil;
v_1516 = new V((RuleFunction)(args_1517 => {
V v_1518 = args_1517[0];
V v_1519 = args_1517[1];
V v_1520 = args_1517[2];
var values_1521 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1518}, new Results(R.Get(v_1519, (V)"actor_id")))));
V v_1522 = values_1521[0];
if (R.Truth(R.Or((V)(!R.Truth(v_1522)), () => (V)(R.Get(v_1522, (V)"hp") <= (V)0.0d)))) {
return Results.Empty;
}
var values_1523 = R.Join(new V[] {}, new Results(R.Get(v_1522, (V)"pending_attack")));
V v_1524 = values_1523[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_1524)), () => (V)(R.Get(v_1524, (V)"schedule_id") != R.Get(v_1519, (V)"id"))), () => (V)(R.Get(v_1524, (V)"action_id") != R.Get(v_1519, (V)"attack_id"))))) {
return Results.Empty;
}
var values_1525 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1526 = v_1522;
V key_1527 = (V)"pending_attack";
R.Set(obj_1526, key_1527, values_1525[0]);
if (R.Truth(R.Get(v_1524, (V)"support"))) {
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"SupportResolved", R.Table(new Entry((V)"ability_id", R.Get(v_1524, (V)"ability_id")), new Entry((V)"windup_id", R.Get(v_1524, (V)"schedule_id")), new Entry((V)"target_cells", R.Get(v_1524, (V)"target_cells")), new Entry((V)"floor_id", R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
if (R.Truth((V)(R.Get(R.Get(v_1524, (V)"support"), (V)"kind") == (V)"heal"))) {
foreach (var row_1528 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1524, (V)"target_cells"))))))) {
V v_1529 = row_1528[0];
V v_1530 = row_1528[1];
var values_1531 = R.Join(new V[] {}, R.Call(v_144, R.Join(new V[] {v_1518, R.Get(v_1530, (V)"floor_id"), R.Get(v_1530, (V)"x"), R.Get(v_1530, (V)"y"), R.Get(v_1522, (V)"id")}, new Results((V)true))));
V v_1532 = values_1531[0];
if (R.Truth(R.And(R.And(v_1532, () => (V)(R.Get(v_1532, (V)"faction") == R.Get(v_1522, (V)"faction"))), () => R.Or((V)(!R.Truth(R.Get(R.Get(v_1524, (V)"support"), (V)"target_id"))), () => (V)(R.Get(v_1532, (V)"id") == R.Get(R.Get(v_1524, (V)"support"), (V)"target_id")))))) {
var values_1533 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(R.Get(v_1524, (V)"support"), (V)"power")}, new Results((V)(R.Get(v_1532, (V)"max_hp") - R.Get(v_1532, (V)"hp"))))));
V v_1534 = values_1533[0];
if (R.Truth((V)(v_1534 > (V)0.0d))) {
var values_1535 = R.Join(new V[] {}, new Results((V)(R.Get(v_1532, (V)"hp") + v_1534)));
var obj_1536 = v_1532;
V key_1537 = (V)"hp";
R.Set(obj_1536, key_1537, values_1535[0]);
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"Healed", R.Table(new Entry((V)"target_id", R.Get(v_1532, (V)"id")), new Entry((V)"amount", v_1534), new Entry((V)"floor_id", R.Get(v_1530, (V)"floor_id")), new Entry((V)"x", R.Get(v_1530, (V)"x")), new Entry((V)"y", R.Get(v_1530, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
}
}
}
} else if (R.Truth((V)(R.Get(R.Get(v_1524, (V)"support"), (V)"kind") == (V)"mirror"))) {
var values_1546 = R.Join(new V[] {}, R.Call(R.Get(v_60, (V)"create"), R.Join(new V[] {v_1518, v_1522, v_1524}, new Results(R.Table(new Entry((V)"map", R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1518))).First), new Entry((V)"free", new V((RuleFunction)(args_1538 => {
V v_1539 = args_1538[0];
V v_1540 = args_1538[1];
V v_1541 = args_1538[2];
V v_1542 = args_1538[3];
return R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1539))).First, v_1541}, new Results(v_1542))).First, () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1539, R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id"), v_1541, v_1542, v_1540}, new Results((V)false))).First)))));
return Results.Empty;
}))), new Entry((V)"visible", new V((RuleFunction)(args_1543 => {
V v_1544 = args_1543[0];
V v_1545 = args_1543[1];
return R.Join(new V[] {}, R.Call(R.Get(v_70, (V)"can_see"), R.Join(new V[] {v_1518, R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id"), v_1544}, new Results(v_1545))));
return Results.Empty;
}))))))));
V v_1547 = values_1546[0];
V v_1548 = values_1546[1];
var values_1549 = R.Join(new V[] {}, new Results(R.Get(v_1524, (V)"support")));
V v_1550 = values_1549[0];
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, R.Or(R.And(v_1547, () => (V)"MirrorCreated"), () => (V)"MirrorBlocked"), R.Table(new Entry((V)"mirror_id", R.And(v_1548, () => R.Get(v_1548, (V)"id"))), new Entry((V)"floor_id", R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(v_1550, (V)"x")), new Entry((V)"y", R.Get(v_1550, (V)"y")), new Entry((V)"in_dx", R.Get(v_1550, (V)"in_dx")), new Entry((V)"in_dy", R.Get(v_1550, (V)"in_dy"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
} else if (R.Truth((V)(R.Get(R.Get(v_1524, (V)"support"), (V)"kind") == (V)"web"))) {
var values_1559 = R.Join(new V[] {}, R.Call(R.Get(v_66, (V)"create"), R.Join(new V[] {v_1518, v_1522, v_1524}, new Results(R.Table(new Entry((V)"map", R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1518))).First), new Entry((V)"free", new V((RuleFunction)(args_1551 => {
V v_1552 = args_1551[0];
V v_1553 = args_1551[1];
V v_1554 = args_1551[2];
V v_1555 = args_1551[3];
return R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1552))).First, v_1554}, new Results(v_1555))).First, () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1552, R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id"), v_1554, v_1555, v_1553}, new Results((V)false))).First)))));
return Results.Empty;
}))), new Entry((V)"visible", new V((RuleFunction)(args_1556 => {
V v_1557 = args_1556[0];
V v_1558 = args_1556[1];
return R.Join(new V[] {}, R.Call(R.Get(v_70, (V)"can_see"), R.Join(new V[] {v_1518, R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id"), v_1557}, new Results(v_1558))));
return Results.Empty;
}))))))));
V v_1560 = values_1559[0];
V v_1561 = values_1559[1];
var values_1562 = R.Join(new V[] {}, new Results(R.Get(v_1524, (V)"support")));
V v_1563 = values_1562[0];
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, R.Or(R.And(v_1560, () => (V)"WebCreated"), () => (V)"WebBlocked"), R.Table(new Entry((V)"web_id", R.And(v_1561, () => R.Get(v_1561, (V)"id"))), new Entry((V)"floor_id", R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id")), new Entry((V)"x", (V)((V)(R.Get(v_1563, (V)"ax") + R.Get(v_1563, (V)"bx")) / (V)2.0d)), new Entry((V)"y", (V)((V)(R.Get(v_1563, (V)"ay") + R.Get(v_1563, (V)"by")) / (V)2.0d)), new Entry((V)"ax", R.Get(v_1563, (V)"ax")), new Entry((V)"ay", R.Get(v_1563, (V)"ay")), new Entry((V)"bx", R.Get(v_1563, (V)"bx")), new Entry((V)"by", R.Get(v_1563, (V)"by"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
} else if (R.Truth((V)(R.Get(R.Get(v_1524, (V)"support"), (V)"kind") == (V)"alarm"))) {
foreach (var row_1564 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1524, (V)"target_cells"))))))) {
V v_1565 = row_1564[0];
V v_1566 = row_1564[1];
var values_1575 = R.Join(new V[] {}, R.Call(R.Get(v_56, (V)"alarm_landing"), R.Join(new V[] {v_1518, v_1522, v_1566, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1518))).First, new V((RuleFunction)(args_1567 => {
V v_1568 = args_1567[0];
V v_1569 = args_1567[1];
V v_1570 = args_1567[2];
V v_1571 = args_1567[3];
return R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1568))).First, v_1570}, new Results(v_1571))).First, () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1568, R.Get(v_1566, (V)"floor_id"), v_1570, v_1571, v_1569}, new Results((V)false))).First)))));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1572 => {
V v_1573 = args_1572[0];
V v_1574 = args_1572[1];
return R.Join(new V[] {}, R.Call(R.Get(v_70, (V)"can_see"), R.Join(new V[] {v_1518, R.Get(v_1566, (V)"floor_id"), v_1573}, new Results(v_1574))));
return Results.Empty;
}))))));
V v_1576 = values_1575[0];
if (R.Truth(v_1576)) {
var values_1577 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"spawn_enemy"), R.Join(new V[] {v_1518, (V)"core:enemy/ruin_skitter", R.Get(v_1566, (V)"x"), R.Get(v_1566, (V)"y")}, new Results((V)(R.Get(v_1518, (V)"clock") + (V)1.0d)))));
V v_1578 = values_1577[0];
var values_1579 = R.Join(new V[] {}, new Results(R.Get(v_1522, (V)"id")));
var obj_1580 = v_1578;
V key_1581 = (V)"called_by";
R.Set(obj_1580, key_1581, values_1579[0]);

var values_1582 = R.Join(new V[] {}, new Results((V)true));
var obj_1583 = v_1578;
V key_1584 = (V)"reward_exempt";
R.Set(obj_1583, key_1584, values_1582[0]);
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"AlarmReinforcement", R.Table(new Entry((V)"target_id", R.Get(v_1578, (V)"id")), new Entry((V)"floor_id", R.Get(v_1566, (V)"floor_id")), new Entry((V)"x", R.Get(v_1566, (V)"x")), new Entry((V)"y", R.Get(v_1566, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
} else {
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"AlarmBlocked", R.Table(new Entry((V)"floor_id", R.Get(v_1566, (V)"floor_id")), new Entry((V)"x", R.Get(v_1566, (V)"x")), new Entry((V)"y", R.Get(v_1566, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
}
}
} else if (R.Truth((V)(R.Get(R.Get(v_1524, (V)"support"), (V)"kind") == (V)"gulp"))) {
var values_1585 = R.Join(new V[] {}, R.Call(R.Get(v_56, (V)"drop"), R.Join(new V[] {v_1518}, new Results(R.Get(R.Get(v_1524, (V)"support"), (V)"drop_id")))));
V v_1586 = values_1585[0];
var values_1587 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1524, (V)"target_cells"), (V)1.0d)));
V v_1588 = values_1587[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_1586, () => (V)(!R.Truth(R.Get(v_1586, (V)"collected")))), () => (V)(!R.Truth(R.Get(v_1586, (V)"carried_by")))), () => (V)(!R.Truth(R.Get(v_1522, (V)"carried_drop_id")))), () => (V)(R.Get(R.Get(v_1586, (V)"position"), (V)"floor_id") == R.Get(v_1588, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_1586, (V)"position"), (V)"x") == R.Get(v_1588, (V)"x"))), () => (V)(R.Get(R.Get(v_1586, (V)"position"), (V)"y") == R.Get(v_1588, (V)"y"))), () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1518, R.Get(v_1588, (V)"floor_id"), R.Get(v_1588, (V)"x"), R.Get(v_1588, (V)"y"), R.Get(v_1522, (V)"id")}, new Results((V)false))).First))))) {
var values_1589 = R.Join(new V[] {}, new Results(R.Get(v_1522, (V)"id")));
var obj_1590 = v_1586;
V key_1591 = (V)"carried_by";
R.Set(obj_1590, key_1591, values_1589[0]);

var values_1592 = R.Join(new V[] {}, new Results(R.Get(v_1586, (V)"id")));
var obj_1593 = v_1522;
V key_1594 = (V)"carried_drop_id";
R.Set(obj_1593, key_1594, values_1592[0]);
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"DropSwallowed", R.Table(new Entry((V)"drop_id", R.Get(v_1586, (V)"id")), new Entry((V)"drop_kind", R.Get(v_1586, (V)"kind")), new Entry((V)"amount", R.Get(v_1586, (V)"amount")), new Entry((V)"floor_id", R.Get(v_1588, (V)"floor_id")), new Entry((V)"x", R.Get(v_1588, (V)"x")), new Entry((V)"y", R.Get(v_1588, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
} else {
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"GulpBlocked", R.Table(new Entry((V)"floor_id", R.Get(v_1588, (V)"floor_id")), new Entry((V)"x", R.Get(v_1588, (V)"x")), new Entry((V)"y", R.Get(v_1588, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
}
} else if (R.Truth((V)(R.Get(R.Get(v_1524, (V)"support"), (V)"kind") == (V)"revive"))) {
var values_1595 = R.Join(new V[] {}, R.Call(R.Get(v_56, (V)"corpse"), R.Join(new V[] {v_1518}, new Results(R.Get(R.Get(v_1524, (V)"support"), (V)"corpse_id")))));
V v_1596 = values_1595[0];
var values_1597 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1524, (V)"target_cells"), (V)1.0d)));
V v_1598 = values_1597[0];
var values_1599 = R.Join(new V[] {}, new Results((V)false));
V v_1600 = values_1599[0];
foreach (var row_1601 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1518, (V)"entities"))))))) {
V v_1602 = row_1601[0];
V v_1603 = row_1601[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_1603, (V)"controller") == (V)"exit"), () => (V)(R.Get(R.Get(v_1603, (V)"position"), (V)"floor_id") == R.Get(v_1598, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_1603, (V)"position"), (V)"x") == R.Get(v_1598, (V)"x"))), () => (V)(R.Get(R.Get(v_1603, (V)"position"), (V)"y") == R.Get(v_1598, (V)"y"))))) {
var values_1604 = R.Join(new V[] {}, new Results((V)true));
v_1600 = values_1604[0];

break;
}
}
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_1596, () => (V)(R.Get(v_1596, (V)"claimed_by") == R.Get(v_1522, (V)"id"))), () => (V)(R.Get(v_1596, (V)"floor_id") == R.Get(v_1518, (V)"active_floor_id"))), () => (V)(R.Get(v_1596, (V)"x") == R.Get(v_1598, (V)"x"))), () => (V)(R.Get(v_1596, (V)"y") == R.Get(v_1598, (V)"y"))), () => R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1518))).First, R.Get(v_1596, (V)"x")}, new Results(R.Get(v_1596, (V)"y")))).First), () => R.Call(R.Get(v_70, (V)"can_see"), R.Join(new V[] {v_1518, R.Get(v_1596, (V)"floor_id"), R.Get(v_1596, (V)"x")}, new Results(R.Get(v_1596, (V)"y")))).First), () => (V)(!R.Truth(R.Call(R.Get(v_32, (V)"portal"), R.Join(new V[] {v_1518, R.Get(v_1596, (V)"x")}, new Results(R.Get(v_1596, (V)"y")))).First))), () => (V)(!R.Truth(v_1600))), () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1518, R.Get(v_1596, (V)"floor_id"), R.Get(v_1596, (V)"x"), R.Get(v_1596, (V)"y"), V.Nil}, new Results((V)false))).First))))) {
var values_1605 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"spawn_enemy"), R.Join(new V[] {v_1518, R.Get(v_1596, (V)"definition_id"), R.Get(v_1596, (V)"x"), R.Get(v_1596, (V)"y")}, new Results((V)(R.Get(v_1518, (V)"clock") + (V)1.0d)))));
V v_1606 = values_1605[0];
var values_1607 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"ceil"), R.Join(new V[] {}, new Results((V)(R.Get(v_1596, (V)"max_hp") / (V)2.0d)))))));
var obj_1608 = v_1606;
V key_1609 = (V)"max_hp";
R.Set(obj_1608, key_1609, values_1607[0]);

var values_1610 = R.Join(new V[] {}, new Results(R.Get(v_1606, (V)"max_hp")));
var obj_1611 = v_1606;
V key_1612 = (V)"hp";
R.Set(obj_1611, key_1612, values_1610[0]);
var values_1613 = R.Join(new V[] {}, new Results(R.Get(v_1596, (V)"id")));
var obj_1614 = v_1606;
V key_1615 = (V)"revival_of";
R.Set(obj_1614, key_1615, values_1613[0]);

var values_1616 = R.Join(new V[] {}, new Results(R.Get(v_1522, (V)"id")));
var obj_1617 = v_1606;
V key_1618 = (V)"revived_by";
R.Set(obj_1617, key_1618, values_1616[0]);

var values_1619 = R.Join(new V[] {}, new Results((V)true));
var obj_1620 = v_1606;
V key_1621 = (V)"reward_exempt";
R.Set(obj_1620, key_1621, values_1619[0]);
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"EnemyRevived", R.Table(new Entry((V)"target_id", R.Get(v_1606, (V)"id")), new Entry((V)"original_id", R.Get(v_1596, (V)"id")), new Entry((V)"definition_id", R.Get(v_1596, (V)"definition_id")), new Entry((V)"floor_id", R.Get(v_1596, (V)"floor_id")), new Entry((V)"x", R.Get(v_1596, (V)"x")), new Entry((V)"y", R.Get(v_1596, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
} else {
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"RevivalBlocked", R.Table(new Entry((V)"floor_id", R.Get(v_1598, (V)"floor_id")), new Entry((V)"x", R.Get(v_1598, (V)"x")), new Entry((V)"y", R.Get(v_1598, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
}
{ double end_1624 = R.Num((V)1.0d), step_1625 = R.Num((-(V)1.0d));
for (double i_1623 = R.Num((V)R.Length(R.Or(R.Get(v_1518, (V)"idea_corpses"), () => R.Table()))); step_1625 > 0 ? i_1623 <= end_1624 : i_1623 >= end_1624; i_1623 += step_1625) {
V v_1622 = i_1623;
if (R.Truth((V)(R.Get(R.Get(R.Get(v_1518, (V)"idea_corpses"), v_1622), (V)"id") == R.Get(R.Get(v_1524, (V)"support"), (V)"corpse_id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_1518, (V)"idea_corpses")}, new Results(v_1622)));
}
}
}
R.Call(R.Get(v_56, (V)"prune_corpses"), R.Join(new V[] {}, new Results(v_1518)));
} else if (R.Truth((V)(R.Get(R.Get(v_1524, (V)"support"), (V)"kind") == (V)"phase"))) {
var values_1626 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1524, (V)"target_cells"), (V)1.0d)));
V v_1627 = values_1626[0];
var values_1636 = R.Join(new V[] {}, R.Call(R.Get(v_56, (V)"phase_landing"), R.Join(new V[] {v_1518, v_1522, v_1627, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1518))).First, new V((RuleFunction)(args_1628 => {
V v_1629 = args_1628[0];
V v_1630 = args_1628[1];
V v_1631 = args_1628[2];
V v_1632 = args_1628[3];
return R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1629))).First, v_1631}, new Results(v_1632))).First, () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1629, R.Get(v_1629, (V)"active_floor_id"), v_1631, v_1632, v_1630}, new Results((V)false))).First)))));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1633 => {
V v_1634 = args_1633[0];
V v_1635 = args_1633[1];
return R.Join(new V[] {}, R.Call(R.Get(v_70, (V)"can_see"), R.Join(new V[] {v_1518, R.Get(v_1518, (V)"active_floor_id"), v_1634}, new Results(v_1635))));
return Results.Empty;
}))))));
V v_1637 = values_1636[0];
if (R.Truth(v_1637)) {
var values_1638 = R.Join(new V[] {R.Get(R.Get(v_1522, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_1522, (V)"position"), (V)"y")));
V v_1639 = values_1638[0];
V v_1640 = values_1638[1];
var values_1641 = R.Join(new V[] {R.Get(v_1627, (V)"x")}, new Results(R.Get(v_1627, (V)"y")));
var obj_1642 = R.Get(v_1522, (V)"position");
V key_1643 = (V)"x";
var obj_1644 = R.Get(v_1522, (V)"position");
V key_1645 = (V)"y";
R.Set(obj_1642, key_1643, values_1641[0]);
R.Set(obj_1644, key_1645, values_1641[1]);
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"Displaced", R.Table(new Entry((V)"target_id", R.Get(v_1522, (V)"id")), new Entry((V)"reason", (V)"phase"), new Entry((V)"floor_id", R.Get(v_1627, (V)"floor_id")), new Entry((V)"from_x", v_1639), new Entry((V)"from_y", v_1640), new Entry((V)"x", R.Get(v_1627, (V)"x")), new Entry((V)"y", R.Get(v_1627, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"TileEntered", R.Table(new Entry((V)"floor_id", R.Get(v_1627, (V)"floor_id")), new Entry((V)"from_x", v_1639), new Entry((V)"from_y", v_1640), new Entry((V)"x", R.Get(v_1627, (V)"x")), new Entry((V)"y", R.Get(v_1627, (V)"y")), new Entry((V)"forced", (V)true)), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
} else {
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"PhaseBlocked", R.Table(new Entry((V)"floor_id", R.Get(v_1627, (V)"floor_id")), new Entry((V)"x", R.Get(v_1627, (V)"x")), new Entry((V)"y", R.Get(v_1627, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
}
} else if (R.Truth((V)(R.Get(R.Get(v_1524, (V)"support"), (V)"kind") == (V)"ward"))) {
var values_1646 = R.Join(new V[] {}, new Results((V)true));
var obj_1647 = R.Get(v_1522, (V)"ai_memory");
V key_1648 = (V)"ward_ready";
R.Set(obj_1647, key_1648, values_1646[0]);
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"SquadWardRaised", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_1522, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_1522, (V)"position"), (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
} else if (R.Truth((V)(R.Get(R.Get(v_1524, (V)"support"), (V)"kind") == (V)"snare"))) {
var values_1649 = R.Join(new V[] {}, new Results((V)0.0d));
V v_1650 = values_1649[0];
foreach (var row_1651 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1518, (V)"enemy_snares"), () => R.Table()))))))) {
V v_1652 = row_1651[0];
V v_1653 = row_1651[1];
if (R.Truth((V)(R.Get(v_1653, (V)"source_id") == R.Get(v_1522, (V)"id")))) {
var values_1654 = R.Join(new V[] {}, new Results((V)(v_1650 + (V)1.0d)));
v_1650 = values_1654[0];
}
}
foreach (var row_1655 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1524, (V)"target_cells"))))))) {
V v_1656 = row_1655[0];
V v_1657 = row_1655[1];
var values_1658 = R.Join(new V[] {}, new Results((V)false));
V v_1659 = values_1658[0];
foreach (var row_1660 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1518, (V)"enemy_snares"), () => R.Table()))))))) {
V v_1661 = row_1660[0];
V v_1662 = row_1660[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_1662, (V)"floor_id") == R.Get(v_1657, (V)"floor_id")), () => (V)(R.Get(v_1662, (V)"x") == R.Get(v_1657, (V)"x"))), () => (V)(R.Get(v_1662, (V)"y") == R.Get(v_1657, (V)"y"))))) {
var values_1663 = R.Join(new V[] {}, new Results((V)true));
v_1659 = values_1663[0];
}
}
if (R.Truth(R.And(R.And(R.And((V)(v_1650 < R.Get(v_54, (V)"snare_cap")), () => (V)(!R.Truth(v_1659))), () => R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1518))).First, R.Get(v_1657, (V)"x")}, new Results(R.Get(v_1657, (V)"y")))).First), () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1518, R.Get(v_1657, (V)"floor_id"), R.Get(v_1657, (V)"x"), R.Get(v_1657, (V)"y"), V.Nil}, new Results((V)false))).First))))) {
var values_1664 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_1518, (V)"enemy_snares"), () => R.Table())));
var obj_1665 = v_1518;
V key_1666 = (V)"enemy_snares";
R.Set(obj_1665, key_1666, values_1664[0]);
var values_1667 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_1657, (V)"floor_id")), new Entry((V)"x", R.Get(v_1657, (V)"x")), new Entry((V)"y", R.Get(v_1657, (V)"y")), new Entry((V)"source_id", R.Get(v_1522, (V)"id")))));
var obj_1668 = R.Get(v_1518, (V)"enemy_snares");
V key_1669 = (V)((V)R.Length(R.Get(v_1518, (V)"enemy_snares")) + (V)1.0d);
R.Set(obj_1668, key_1669, values_1667[0]);
var values_1670 = R.Join(new V[] {}, new Results((V)(v_1650 + (V)1.0d)));
v_1650 = values_1670[0];
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"EnemySnarePlaced", R.Table(new Entry((V)"floor_id", R.Get(v_1657, (V)"floor_id")), new Entry((V)"x", R.Get(v_1657, (V)"x")), new Entry((V)"y", R.Get(v_1657, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
}
}
} else if (R.Truth((V)(R.Get(R.Get(v_1524, (V)"support"), (V)"kind") == (V)"summon"))) {
var values_1671 = R.Join(new V[] {}, new Results((V)0.0d));
V v_1672 = values_1671[0];
foreach (var row_1673 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1518, (V)"entities"))))))) {
V v_1674 = row_1673[0];
V v_1675 = row_1673[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_1675, (V)"summoned_by_id") == R.Get(v_1522, (V)"id")), () => R.Get(v_1675, (V)"hp")), () => (V)(R.Get(v_1675, (V)"hp") > (V)0.0d)))) {
var values_1676 = R.Join(new V[] {}, new Results((V)(v_1672 + (V)1.0d)));
v_1672 = values_1676[0];
}
}
foreach (var row_1677 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1524, (V)"target_cells"))))))) {
V v_1678 = row_1677[0];
V v_1679 = row_1677[1];
if (R.Truth(R.Or((V)(v_1672 >= R.Get(R.Get(v_1524, (V)"support"), (V)"max_active")), () => (V)(R.Or(R.Get(R.Get(v_1522, (V)"ai_memory"), (V)"summons"), () => (V)0.0d) >= (V)3.0d)))) {
break;
}
if (R.Truth(R.And(R.And((V)(R.Get(v_1679, (V)"floor_id") == R.Get(v_1518, (V)"active_floor_id")), () => R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1518))).First, R.Get(v_1679, (V)"x")}, new Results(R.Get(v_1679, (V)"y")))).First), () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1518, R.Get(v_1679, (V)"floor_id"), R.Get(v_1679, (V)"x"), R.Get(v_1679, (V)"y"), V.Nil}, new Results((V)false))).First))))) {
var values_1680 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"spawn_enemy"), R.Join(new V[] {v_1518, R.Get(R.Get(v_1524, (V)"support"), (V)"enemy_id"), R.Get(v_1679, (V)"x"), R.Get(v_1679, (V)"y")}, new Results((V)(R.Get(v_1518, (V)"clock") + (V)1.0d)))));
V v_1681 = values_1680[0];
var values_1682 = R.Join(new V[] {}, new Results(R.Get(v_1522, (V)"id")));
var obj_1683 = v_1681;
V key_1684 = (V)"summoned_by_id";
R.Set(obj_1683, key_1684, values_1682[0]);
var values_1685 = R.Join(new V[] {}, new Results((V)(v_1672 + (V)1.0d)));
v_1672 = values_1685[0];
var values_1686 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(R.Get(v_1522, (V)"ai_memory"), (V)"summons"), () => (V)0.0d) + (V)1.0d)));
var obj_1687 = R.Get(v_1522, (V)"ai_memory");
V key_1688 = (V)"summons";
R.Set(obj_1687, key_1688, values_1686[0]);
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"EnemySummoned", R.Table(new Entry((V)"enemy_id", R.Get(v_1681, (V)"id")), new Entry((V)"definition_id", R.Get(v_1681, (V)"definition_id")), new Entry((V)"floor_id", R.Get(v_1679, (V)"floor_id")), new Entry((V)"x", R.Get(v_1679, (V)"x")), new Entry((V)"y", R.Get(v_1679, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
}
}
}
} else {
R.Call(v_1334, R.Join(new V[] {v_1518, v_1520, v_1522, R.Get(v_1524, (V)"action_id"), R.Get(v_1524, (V)"ability_id"), R.Get(v_1524, (V)"target_cells"), R.Get(v_1524, (V)"damage"), R.Get(v_1524, (V)"ranged")}, new Results(R.Table(new Entry((V)"windup_id", R.Get(v_1524, (V)"schedule_id")), new Entry((V)"committed_time", R.Get(v_1524, (V)"committed_time")), new Entry((V)"reflection", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1524, (V)"reflection")))).First)))));
if (R.Truth(R.And(R.Get(v_1524, (V)"shove"), () => (V)(!R.Truth(R.Get(v_1518, (V)"game_over")))))) {
var values_1689 = R.Join(new V[] {}, new Results(R.Get(v_1524, (V)"shove")));
V v_1690 = values_1689[0];
var values_1691 = R.Join(new V[] {}, R.Call(v_144, R.Join(new V[] {v_1518, R.Get(v_1690, (V)"floor_id"), R.Get(v_1690, (V)"x"), R.Get(v_1690, (V)"y"), R.Get(v_1522, (V)"id")}, new Results((V)true))));
V v_1692 = values_1691[0];
if (R.Truth(v_1692)) {
R.Call(v_660, R.Join(new V[] {v_1518, v_1520, v_1692, R.Get(v_1690, (V)"dx"), R.Get(v_1690, (V)"dy"), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
}
}
}
if (R.Truth(R.And(R.Get(v_1524, (V)"ember_landing"), () => (V)(!R.Truth(R.Get(v_1518, (V)"game_over")))))) {
var values_1693 = R.Join(new V[] {}, new Results(R.Get(v_1524, (V)"ember_landing")));
V v_1694 = values_1693[0];

var values_1695 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1518))));
V v_1696 = values_1695[0];
if (R.Truth(R.Call(R.Get(v_64, (V)"fits"), R.Join(new V[] {v_1518, v_1696}, new Results(v_1694))).First)) {
if (R.Truth((V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1518, R.Get(v_1694, (V)"floor_id"), R.Get(v_1694, (V)"x"), R.Get(v_1694, (V)"y"), R.Get(v_1522, (V)"id")}, new Results((V)false))).First)))) {
var values_1697 = R.Join(new V[] {R.Get(R.Get(v_1522, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_1522, (V)"position"), (V)"y")));
V v_1698 = values_1697[0];
V v_1699 = values_1697[1];
var values_1700 = R.Join(new V[] {R.Get(v_1694, (V)"x")}, new Results(R.Get(v_1694, (V)"y")));
var obj_1701 = R.Get(v_1522, (V)"position");
V key_1702 = (V)"x";
var obj_1703 = R.Get(v_1522, (V)"position");
V key_1704 = (V)"y";
R.Set(obj_1701, key_1702, values_1700[0]);
R.Set(obj_1703, key_1704, values_1700[1]);
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"Displaced", R.Table(new Entry((V)"target_id", R.Get(v_1522, (V)"id")), new Entry((V)"reason", (V)"flight"), new Entry((V)"floor_id", R.Get(v_1694, (V)"floor_id")), new Entry((V)"from_x", v_1698), new Entry((V)"from_y", v_1699), new Entry((V)"x", R.Get(v_1694, (V)"x")), new Entry((V)"y", R.Get(v_1694, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"TileEntered", R.Table(new Entry((V)"floor_id", R.Get(v_1694, (V)"floor_id")), new Entry((V)"from_x", v_1698), new Entry((V)"from_y", v_1699), new Entry((V)"x", R.Get(v_1694, (V)"x")), new Entry((V)"y", R.Get(v_1694, (V)"y")), new Entry((V)"forced", (V)true)), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
} else {
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"MothLandingBlocked", R.Table(new Entry((V)"floor_id", R.Get(v_1694, (V)"floor_id")), new Entry((V)"x", R.Get(v_1694, (V)"x")), new Entry((V)"y", R.Get(v_1694, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
}
R.Call(R.Get(v_64, (V)"create"), R.Join(new V[] {v_1518, v_1522, v_1524, v_1696}, new Results(new V((RuleFunction)(args_1705 => {
V v_1706 = args_1705[0];
V v_1707 = args_1705[1];
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, v_1706, v_1707, R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
return Results.Empty;
})))));
}
}
if (R.Truth(R.And(R.Get(v_1524, (V)"advance"), () => (V)(!R.Truth(R.Get(v_1518, (V)"game_over")))))) {
var values_1708 = R.Join(new V[] {R.Get(R.Get(v_1522, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_1522, (V)"position"), (V)"y")));
V v_1709 = values_1708[0];
V v_1710 = values_1708[1];
var values_1711 = R.Join(new V[] {}, new Results(R.Table()));
V v_1712 = values_1711[0];
{ double end_1715 = R.Num(R.Get(R.Get(v_1524, (V)"advance"), (V)"max_steps")), step_1716 = R.Num((V)1.0d);
for (double i_1714 = R.Num((V)1.0d); step_1716 > 0 ? i_1714 <= end_1715 : i_1714 >= end_1715; i_1714 += step_1716) {
V v_1713 = i_1714;
var values_1717 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_1522, (V)"position"), (V)"x") + R.Get(R.Get(v_1524, (V)"advance"), (V)"dx"))));
V v_1718 = values_1717[0];
var values_1719 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_1522, (V)"position"), (V)"y") + R.Get(R.Get(v_1524, (V)"advance"), (V)"dy"))));
V v_1720 = values_1719[0];
if (R.Truth(R.Or((V)(!R.Truth(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1518))).First, v_1718}, new Results(v_1720))).First)), () => R.Call(v_144, R.Join(new V[] {v_1518, R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id"), v_1718, v_1720, R.Get(v_1522, (V)"id")}, new Results((V)false))).First))) {
break;
}
var values_1721 = R.Join(new V[] {v_1718}, new Results(v_1720));
var obj_1722 = R.Get(v_1522, (V)"position");
V key_1723 = (V)"x";
var obj_1724 = R.Get(v_1522, (V)"position");
V key_1725 = (V)"y";
R.Set(obj_1722, key_1723, values_1721[0]);
R.Set(obj_1724, key_1725, values_1721[1]);
var values_1726 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id")), new Entry((V)"x", v_1718), new Entry((V)"y", v_1720))));
var obj_1727 = v_1712;
V key_1728 = (V)((V)R.Length(v_1712) + (V)1.0d);
R.Set(obj_1727, key_1728, values_1726[0]);
}
}
if (R.Truth((V)((V)R.Length(v_1712) > (V)0.0d))) {
R.Call(R.Get(v_62, (V)"leave"), R.Join(new V[] {v_1518, v_1522, v_1524, v_1709, v_1710, v_1712, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1518))).First}, new Results(new V((RuleFunction)(args_1729 => {
V v_1730 = args_1729[0];
V v_1731 = args_1729[1];
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, v_1730, v_1731, R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
return Results.Empty;
})))));
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"Moved", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_1522, (V)"position"), (V)"floor_id")), new Entry((V)"from_x", v_1709), new Entry((V)"from_y", v_1710), new Entry((V)"x", R.Get(R.Get(v_1522, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_1522, (V)"position"), (V)"y")), new Entry((V)"path", v_1712)), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
var values_1732 = R.Join(new V[] {v_1709}, new Results(v_1710));
V v_1733 = values_1732[0];
V v_1734 = values_1732[1];
foreach (var row_1735 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_1712)))))) {
V v_1736 = row_1735[0];
V v_1737 = row_1735[1];
R.Call(v_95, R.Join(new V[] {v_1518, v_1520, (V)"TileEntered", R.Table(new Entry((V)"floor_id", R.Get(v_1737, (V)"floor_id")), new Entry((V)"from_x", v_1733), new Entry((V)"from_y", v_1734), new Entry((V)"x", R.Get(v_1737, (V)"x")), new Entry((V)"y", R.Get(v_1737, (V)"y"))), R.Get(v_1522, (V)"id")}, new Results(R.Get(v_1524, (V)"action_id"))));
var values_1738 = R.Join(new V[] {R.Get(v_1737, (V)"x")}, new Results(R.Get(v_1737, (V)"y")));
v_1733 = values_1738[0];
v_1734 = values_1738[1];
}
}
}
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_1518}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"actor_id", R.Get(v_1522, (V)"id")), new Entry((V)"due", (V)(R.Get(v_1518, (V)"clock") + R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {R.Get(R.Call(R.Get(v_12, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_1522, (V)"definition_id")))).First, (V)"recovery")}, new Results(R.Get(v_1522, (V)"speed")))).First)), new Entry((V)"is_player", (V)false)))));
return Results.Empty;
}));
V v_1739 = V.Nil;
v_1739 = new V((RuleFunction)(args_1740 => {
V v_1741 = args_1740[0];
V v_1742 = args_1740[1];
var values_1743 = R.Join(new V[] {}, new Results((V)0.0d));
V v_1744 = values_1743[0];
while (R.Truth((V)(!R.Truth(R.Get(v_1741, (V)"game_over"))))) {
var values_1745 = R.Join(new V[] {}, new Results((V)(v_1744 + (V)1.0d)));
v_1744 = values_1745[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_1744 <= (V)100000.0d)}, new Results((V)"scheduler failed to reach player decision")));
var values_1746 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_38, (V)"peek"), R.Join(new V[] {}, new Results(v_1741))).First}, new Results((V)"scheduler has no ready actor"))));
V v_1747 = values_1746[0];
var values_1748 = R.Join(new V[] {}, new Results(R.Get(v_1747, (V)"due")));
var obj_1749 = v_1741;
V key_1750 = (V)"clock";
R.Set(obj_1749, key_1750, values_1748[0]);
if (R.Truth(R.And((V)(R.Get(v_1747, (V)"kind") == (V)"actor_ready"), () => R.Or((V)(R.Get(v_1747, (V)"actor_id") == R.Get(v_1741, (V)"player_id")), () => R.Call(rt.Global("_roag_party_ready"), R.Join(new V[] {v_1741}, new Results(v_1747))).First)))) {
return Results.Empty;
}
R.Call(R.Get(v_38, (V)"pop"), R.Join(new V[] {}, new Results(v_1741)));
if (R.Truth((V)(R.Get(v_1747, (V)"kind") == (V)"actor_ready"))) {
R.Call(v_1412, R.Join(new V[] {v_1741, v_1747}, new Results(v_1742)));
} else if (R.Truth((V)(R.Get(v_1747, (V)"kind") == (V)"impact"))) {
R.Call(v_1516, R.Join(new V[] {v_1741, v_1747}, new Results(v_1742)));
} else if (R.Truth(R.Or((V)(R.Get(v_1747, (V)"kind") == (V)"stage_pressure"), () => (V)(R.Get(v_1747, (V)"kind") == (V)"stage_deadline")))) {
R.Call(R.Get(v_40, (V)"resolve_due"), R.Join(new V[] {v_1741, v_1747, new V((RuleFunction)(args_1751 => {
V v_1752 = args_1751[0];
V v_1753 = args_1751[1];
V v_1754 = args_1751[2];
R.Call(v_95, R.Join(new V[] {v_1741, v_1742, v_1752, v_1753, v_1754}, new Results(V.Nil)));
return Results.Empty;
}))}, new Results(R.Get(v_74, (V)"spawn_enemy"))));
} else if (R.Truth(R.Get(v_1747, (V)"idea_storm"))) {
R.Call(R.Get(v_22, (V)"resolve"), R.Join(new V[] {v_1741, v_1747}, new Results(new V((RuleFunction)(args_1755 => {
V v_1756 = args_1755[0];
var values_1757 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1741}, new Results(R.Get(v_1756, (V)"owner_id")))));
V v_1758 = values_1757[0];
if (R.Truth(R.And(v_1758, () => (V)(R.Get(v_1758, (V)"hp") > (V)0.0d)))) {
var values_1759 = R.Join(new V[] {}, new Results(R.Get(v_83, v_1742)));
V v_1760 = values_1759[0];

var values_1761 = R.Join(new V[] {}, new Results(R.Get(v_1760, (V)"cause")));
V v_1762 = values_1761[0];
var values_1763 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"proc_coefficient", (V)0.0d), new Entry((V)"class_generated", (V)true), new Entry((V)"root_action_id", R.Get(v_1756, (V)"action_id")), new Entry((V)"chain_id", R.Get(v_1756, (V)"action_id")), new Entry((V)"ancestry", R.Table()))));
var obj_1764 = v_1760;
V key_1765 = (V)"cause";
R.Set(obj_1764, key_1765, values_1763[0]);
R.Call(v_95, R.Join(new V[] {v_1741, v_1742, (V)"StormDischarged", R.Table(new Entry((V)"target_cells", R.Get(v_1756, (V)"cells")), new Entry((V)"damage", R.Get(v_1756, (V)"damage")), new Entry((V)"floor_id", R.Get(v_1756, (V)"floor_id"))), R.Get(v_1758, (V)"id")}, new Results(R.Get(v_1756, (V)"action_id"))));
R.Call(v_704, R.Join(new V[] {v_1741, v_1742, v_1758, R.Get(v_1756, (V)"action_id"), R.Get(v_1756, (V)"cells"), R.Get(v_1756, (V)"damage"), (V)"area", (V)"lightning", (V)false, (V)false}, new Results((V)"core:ability/stormcaller_discharge")));
var values_1766 = R.Join(new V[] {}, new Results(v_1762));
var obj_1767 = v_1760;
V key_1768 = (V)"cause";
R.Set(obj_1767, key_1768, values_1766[0]);
}
return Results.Empty;
})))));
} else if (R.Truth(R.Get(v_1747, (V)"idea_fixture"))) {
R.Call(R.Get(v_52, (V)"resolve"), R.Join(new V[] {v_1741, v_1747}, new Results(new V((RuleFunction)(args_1769 => {
V v_1770 = args_1769[0];
V v_1771 = args_1769[1];
var values_1772 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1741))))));
V v_1773 = values_1772[0];

var values_1774 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_1770, (V)"floor_id")), new Entry((V)"x", R.Get(v_1770, (V)"center_x")), new Entry((V)"y", R.Get(v_1770, (V)"center_y")))));
var obj_1775 = v_1773;
V key_1776 = (V)"position";
R.Set(obj_1775, key_1776, values_1774[0]);

var values_1777 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_1778 = v_1773;
V key_1779 = (V)"collision_id";
R.Set(obj_1778, key_1779, values_1777[0]);
var values_1780 = R.Join(new V[] {}, new Results(R.Get(v_83, v_1742)));
V v_1781 = values_1780[0];

var values_1782 = R.Join(new V[] {}, new Results(R.Get(v_1781, (V)"cause")));
V v_1783 = values_1782[0];
var values_1784 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"proc_coefficient", (V)0.0d), new Entry((V)"class_generated", (V)true), new Entry((V)"root_action_id", R.Get(v_1771, (V)"action_id")), new Entry((V)"chain_id", R.Get(v_1771, (V)"action_id")), new Entry((V)"ancestry", R.Table()))));
var obj_1785 = v_1781;
V key_1786 = (V)"cause";
R.Set(obj_1785, key_1786, values_1784[0]);
R.Call(v_95, R.Join(new V[] {v_1741, v_1742, (V)"FixtureFell", R.Table(new Entry((V)"floor_id", R.Get(v_1770, (V)"floor_id")), new Entry((V)"x", R.Get(v_1770, (V)"center_x")), new Entry((V)"y", R.Get(v_1770, (V)"center_y")), new Entry((V)"target_cells", R.Get(v_1771, (V)"cells"))), R.Get(v_1773, (V)"id")}, new Results(R.Get(v_1771, (V)"action_id"))));
R.Call(v_704, R.Join(new V[] {v_1741, v_1742, v_1773, R.Get(v_1771, (V)"action_id"), R.Get(v_1771, (V)"cells"), (V)4.0d, (V)"area", (V)"physical", (V)false, (V)false}, new Results((V)"core:ability/falling_fixture")));
var values_1787 = R.Join(new V[] {}, new Results(v_1783));
var obj_1788 = v_1781;
V key_1789 = (V)"cause";
R.Set(obj_1788, key_1789, values_1787[0]);
return Results.Empty;
})))));
} else if (R.Truth(R.Get(v_1747, (V)"idea_sentry"))) {
R.Call(R.Get(v_20, (V)"resolve"), R.Join(new V[] {v_1741, v_1747, v_12, R.Get(v_70, (V)"can_see"), new V((RuleFunction)(args_1790 => {
V v_1791 = args_1790[0];
V v_1792 = args_1790[1];
R.Call(v_95, R.Join(new V[] {v_1741, v_1742, v_1791, v_1792, R.Get(v_1741, (V)"player_id")}, new Results(R.Get(v_1747, (V)"attack_id"))));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1793 => {
V v_1794 = args_1793[0];
V v_1795 = args_1793[1];
V v_1796 = args_1793[2];
var values_1797 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1741))))));
V v_1798 = values_1797[0];

var values_1799 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1794, (V)"position")))));
var obj_1800 = v_1798;
V key_1801 = (V)"position";
R.Set(obj_1800, key_1801, values_1799[0]);
var values_1802 = R.Join(new V[] {}, new Results(R.Get(v_1794, (V)"id")));
var obj_1803 = v_1798;
V key_1804 = (V)"collision_id";
R.Set(obj_1803, key_1804, values_1802[0]);

var values_1805 = R.Join(new V[] {}, new Results((V)"construct"));
var obj_1806 = v_1798;
V key_1807 = (V)"controller";
R.Set(obj_1806, key_1807, values_1805[0]);
R.Call(v_1334, R.Join(new V[] {v_1741, v_1742, v_1798, v_1796, (V)"core:ability/tinkerer_sentry", v_1795, (V)3.0d, (V)true}, new Results(R.Table(new Entry((V)"class_generated", (V)true), new Entry((V)"reflection", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1794, (V)"sentry_reflection")))).First)))));
return Results.Empty;
})))));
} else if (R.Truth(R.Get(v_1747, (V)"idea_geomancer"))) {
R.Call(R.Get(v_18, (V)"expire"), R.Join(new V[] {v_1741, v_1747}, new Results(new V((RuleFunction)(args_1808 => {
V v_1809 = args_1808[0];
V v_1810 = args_1808[1];
V v_1811 = args_1808[2];
V v_1812 = args_1808[3];
R.Call(v_95, R.Join(new V[] {v_1741, v_1742, v_1809, v_1810, v_1811}, new Results(v_1812)));
return Results.Empty;
})))));
} else if (R.Truth(R.Get(v_1747, (V)"idea_terrain"))) {
R.Call(R.Get(v_46, (V)"resolve"), R.Join(new V[] {v_1741, v_1747, new V((RuleFunction)(args_1813 => {
V v_1814 = args_1813[0];
V v_1815 = args_1813[1];
V v_1816 = args_1813[2];
V v_1817 = args_1813[3];
R.Call(v_95, R.Join(new V[] {v_1741, v_1742, v_1814, v_1815, v_1816}, new Results(v_1817)));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1818 => {
V v_1819 = args_1818[0];
V v_1820 = args_1818[1];
V v_1821 = args_1818[2];
R.Call(v_794, R.Join(new V[] {v_1741, v_1742, (V)0.0d, v_1820, v_1819, v_1821, (V)"fire"}, new Results((V)"environment")));
return Results.Empty;
})))));
} else if (R.Truth(R.Get(v_1747, (V)"idea_ember"))) {
R.Call(R.Get(v_64, (V)"resolve"), R.Join(new V[] {v_1741, v_1747, new V((RuleFunction)(args_1822 => {
V v_1823 = args_1822[0];
V v_1824 = args_1822[1];
return R.Join(new V[] {}, R.Call(v_144, R.Join(new V[] {v_1741, R.Get(v_1741, (V)"active_floor_id"), v_1823, v_1824, V.Nil}, new Results((V)true))));
return Results.Empty;
})), new V((RuleFunction)(args_1825 => {
V v_1826 = args_1825[0];
V v_1827 = args_1825[1];
V v_1828 = args_1825[2];
V v_1829 = args_1825[3];
R.Call(v_95, R.Join(new V[] {v_1741, v_1742, v_1826, v_1827, v_1828}, new Results(v_1829)));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_1830 => {
V v_1831 = args_1830[0];
V v_1832 = args_1830[1];
V v_1833 = args_1830[2];
V v_1834 = args_1830[3];
R.Call(v_794, R.Join(new V[] {v_1741, v_1742, v_1832, v_1833, v_1831, v_1834, (V)"fire"}, new Results((V)"enemy")));
return Results.Empty;
})))));
} else {
var values_1835 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1741, (V)"delayed_effects"), R.Get(v_1747, (V)"id"))));
V v_1836 = values_1835[0];
if (R.Truth(v_1836)) {
var values_1837 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1838 = R.Get(v_1741, (V)"delayed_effects");
V key_1839 = R.Get(v_1747, (V)"id");
R.Set(obj_1838, key_1839, values_1837[0]);
var values_1840 = R.Join(new V[] {}, new Results(R.Get(v_83, v_1742)));
V v_1841 = values_1840[0];
var values_1842 = R.Join(new V[] {}, R.Call(v_806, R.Join(new V[] {R.Get(v_1836, (V)"activation")}, new Results(R.Get(v_1836, (V)"effect")))));
var obj_1843 = v_1841;
V key_1844 = (V)"cause";
R.Set(obj_1843, key_1844, values_1842[0]);
R.Call(v_95, R.Join(new V[] {v_1741, v_1742, (V)"DelayedEffectResolved", R.Table(new Entry((V)"effect_id", R.Get(v_1747, (V)"id")), new Entry((V)"boon_id", R.Get(R.Get(v_1836, (V)"activation"), (V)"boon_id")), new Entry((V)"activation_id", R.Get(R.Get(v_1836, (V)"activation"), (V)"id")), new Entry((V)"target_cells", R.Get(R.Get(R.Get(v_1836, (V)"activation"), (V)"event"), (V)"target_cells"))), R.Get(R.Get(v_1836, (V)"activation"), (V)"owner_id")}, new Results(R.Get(R.Get(v_1836, (V)"activation"), (V)"root_action_id"))));
var values_1845 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1846 = v_1841;
V key_1847 = (V)"cause";
R.Set(obj_1846, key_1847, values_1845[0]);
R.Call(v_897, R.Join(new V[] {v_1741, v_1742, R.Get(v_1836, (V)"activation")}, new Results(R.Get(v_1836, (V)"effect"))));
} else {
var values_1848 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1741}, new Results(R.Get(v_1747, (V)"actor_id")))));
V v_1849 = values_1848[0];
var values_1850 = R.Join(new V[] {}, new Results(R.And(R.And(v_1849, () => R.Get(v_1849, (V)"statuses")), () => R.Get(R.Get(v_1849, (V)"statuses"), (V)"burning"))));
V v_1851 = values_1850[0];
if (R.Truth(R.And(v_1851, () => (V)(R.Get(v_1851, (V)"schedule_id") == R.Get(v_1747, (V)"id"))))) {
var values_1852 = R.Join(new V[] {}, new Results(R.Get(v_1851, (V)"activation")));
V v_1853 = values_1852[0];
var values_1854 = R.Join(new V[] {}, new Results(R.Get(v_83, v_1742)));
V v_1855 = values_1854[0];
var values_1856 = R.Join(new V[] {}, R.Call(v_806, R.Join(new V[] {v_1853}, new Results(R.Table(new Entry((V)"damage_type", (V)"fire"))))));
var obj_1857 = v_1855;
V key_1858 = (V)"cause";
R.Set(obj_1857, key_1858, values_1856[0]);
R.Call(v_95, R.Join(new V[] {v_1741, v_1742, (V)"StatusTick", R.Table(new Entry((V)"status", (V)"burning"), new Entry((V)"target_id", R.Get(v_1849, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_1849, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_1849, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_1849, (V)"position"), (V)"y"))), R.Get(v_1851, (V)"source_id")}, new Results(R.Get(v_1853, (V)"root_action_id"))));
var values_1859 = R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1741}, new Results(R.Get(v_1851, (V)"source_id")))).First, () => R.Table(new Entry((V)"id", R.Get(v_1851, (V)"source_id")), new Entry((V)"position", R.Get(v_1853, (V)"source_position")), new Entry((V)"faction", R.Get(v_1853, (V)"source_faction"))))));
V v_1860 = values_1859[0];
R.Call(v_704, R.Join(new V[] {v_1741, v_1742, v_1860, R.Get(v_1853, (V)"root_action_id"), R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_1849, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_1849, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_1849, (V)"position"), (V)"y"))))), R.Get(v_1851, (V)"power"), (V)false}, new Results((V)"fire")));
var values_1861 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1862 = v_1855;
V key_1863 = (V)"cause";
R.Set(obj_1862, key_1863, values_1861[0]);
var values_1864 = R.Join(new V[] {}, new Results((V)(R.Get(v_1851, (V)"remaining") - (V)1.0d)));
var obj_1865 = v_1851;
V key_1866 = (V)"remaining";
R.Set(obj_1865, key_1866, values_1864[0]);
if (R.Truth(R.And((V)(R.Get(v_1849, (V)"hp") > (V)0.0d), () => (V)(R.Get(v_1851, (V)"remaining") > (V)0.0d)))) {
var values_1867 = R.Join(new V[] {}, new Results((V)(R.Get(v_1741, (V)"clock") + R.Get(v_1851, (V)"interval"))));
V v_1868 = values_1867[0];
var values_1869 = R.Join(new V[] {}, R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_1741}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", v_1868), new Entry((V)"actor_id", R.Get(v_1849, (V)"id")), new Entry((V)"attack_id", R.Get(v_1853, (V)"root_action_id")))))));
V v_1870 = values_1869[0];
var values_1871 = R.Join(new V[] {}, new Results(R.Get(v_1870, (V)"id")));
var obj_1872 = v_1870;
V key_1873 = (V)"effect_id";
R.Set(obj_1872, key_1873, values_1871[0]);
var values_1874 = R.Join(new V[] {}, new Results(R.Get(v_1870, (V)"id")));
var obj_1875 = v_1851;
V key_1876 = (V)"schedule_id";
R.Set(obj_1875, key_1876, values_1874[0]);
var values_1877 = R.Join(new V[] {}, new Results(v_1868));
var obj_1878 = v_1851;
V key_1879 = (V)"next_due";
R.Set(obj_1878, key_1879, values_1877[0]);
} else if (R.Truth(R.Get(v_1849, (V)"statuses"))) {
var values_1880 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1881 = R.Get(v_1849, (V)"statuses");
V key_1882 = (V)"burning";
R.Set(obj_1881, key_1882, values_1880[0]);
}
} else {
R.Call(v_95, R.Join(new V[] {v_1741, v_1742, (V)"ScheduledEffectResolved", R.Table(new Entry((V)"effect_id", R.Get(v_1747, (V)"effect_id"))), R.Get(v_1747, (V)"actor_id")}, new Results(R.Get(v_1747, (V)"attack_id"))));
}
}
}
R.Call(v_1142, R.Join(new V[] {v_1741}, new Results(v_1742)));
}
return Results.Empty;
}));
R.Set(v_74, (V)"submit", new V((RuleFunction)(args_1883 => {
V v_1884 = args_1883[0];
V v_1885 = args_1883[1];
if (R.Truth(R.Get(v_1884, (V)"game_over"))) {
return R.Join(new V[] {(V)false, (V)"game over"}, new Results(R.Table()));
}
var values_1886 = R.Join(new V[] {}, R.Call(R.Get(v_38, (V)"peek"), R.Join(new V[] {}, new Results(v_1884))));
V v_1887 = values_1886[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(v_1887, () => (V)(R.Get(v_1887, (V)"kind") == (V)"actor_ready")), () => (V)(R.Get(v_1887, (V)"actor_id") == R.Get(v_1884, (V)"player_id"))), () => (V)(R.Get(v_1887, (V)"due") == R.Get(v_1884, (V)"clock")))}, new Results((V)"player is not ready")));
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_1885))).First != (V)"table"))) {
return R.Join(new V[] {(V)false, (V)"invalid intent"}, new Results(R.Table()));
}
if (R.Truth(R.And((V)(R.Get(v_1885, (V)"vow") != V.Nil), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_1885, (V)"vow")))).First != (V)"boolean")))) {
return R.Join(new V[] {(V)false, (V)"invalid vow option"}, new Results(R.Table()));
}
if (R.Truth(R.And(R.And(R.Get(v_1885, (V)"vow"), () => (V)(R.Get(v_1885, (V)"kind") != (V)"basic_attack")), () => (V)(R.Get(v_1885, (V)"kind") != (V)"sweeping_slash")))) {
return R.Join(new V[] {(V)false, (V)"vow needs an immediate attack"}, new Results(R.Table()));
}
var values_1888 = Results.Empty;
V v_1889 = values_1888[0];
V v_1890 = values_1888[1];
if (R.Truth((V)(R.Get(v_1885, (V)"kind") == (V)"choose_route"))) {
var values_1891 = R.Join(new V[] {}, R.Call(R.Get(v_40, (V)"route_options"), R.Join(new V[] {v_1884}, new Results(R.Get(v_1885, (V)"exit_id")))));
V v_1892 = values_1891[0];
V v_1893 = values_1891[1];
if (R.Truth((V)(!R.Truth(v_1892)))) {
return R.Join(new V[] {(V)false, v_1893}, new Results(R.Table()));
}
var values_1894 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1892, (V)"routes"), R.Get(v_1885, (V)"route_index"))));
V v_1895 = values_1894[0];
if (R.Truth((V)(!R.Truth(v_1895)))) {
return R.Join(new V[] {(V)false, (V)"invalid route"}, new Results(R.Table()));
}
var values_1896 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1884, (V)"stage"), (V)"index")));
V v_1897 = values_1896[0];
var values_1898 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1884))))));
V v_1899 = values_1898[0];
var values_1900 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(R.Get(v_1884, (V)"stage"), (V)"route_history")))));
V v_1901 = values_1900[0];
var values_1902 = R.Join(new V[] {}, new Results(R.Get(v_1895, (V)"id")));
var obj_1903 = v_1901;
V key_1904 = (V)((V)R.Length(v_1901) + (V)1.0d);
R.Set(obj_1903, key_1904, values_1902[0]);
var values_1905 = R.Join(new V[] {}, new Results(R.Table()));
V v_1906 = values_1905[0];
if (R.Truth(R.And((V)(v_1897 >= (V)3.0d), () => (V)(R.Get(v_1895, (V)"id") != (V)"core:route/gateway")))) {
return R.Join(new V[] {(V)false, (V)"invalid final route"}, new Results(R.Table()));
}
var values_1907 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Or(R.Get(v_1884, (V)"retired_stages"), () => R.Table())))));
V v_1908 = values_1907[0];
var values_1909 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_1884))));
V v_1910 = values_1909[0];
var values_1911 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1912 = v_1910;
V key_1913 = (V)"retired_stages";
R.Set(obj_1912, key_1913, values_1911[0]);
var values_1914 = R.Join(new V[] {}, new Results((V)"transition"));
var obj_1915 = R.Get(v_1910, (V)"stage");
V key_1916 = (V)"phase";
R.Set(obj_1915, key_1916, values_1914[0]);
var values_1917 = R.Join(new V[] {}, new Results(v_1910));
var obj_1918 = v_1908;
V key_1919 = (V)((V)R.Length(v_1908) + (V)1.0d);
R.Set(obj_1918, key_1919, values_1917[0]);
var values_1920 = R.Join(new V[] {}, new Results((V)(v_1897 + (V)1.0d)));
V v_1921 = values_1920[0];
var values_1922 = R.Join(new V[] {}, R.Call(R.Get(v_34, (V)"derive"), R.Join(new V[] {R.Get(v_1884, (V)"root_seed")}, new Results(R.Concat(R.Or(R.And((V)(v_1897 >= (V)3.0d), () => (V)"finale:"), () => R.Concat((V)"stage:", R.Concat(v_1921, (V)":"))), R.Get(v_1895, (V)"id"))))));
V v_1923 = values_1922[0];
var values_1924 = Results.Empty;
V v_1925 = values_1924[0];
if (R.Truth((V)(v_1897 >= (V)3.0d))) {
var values_1926 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"new_finale"), R.Join(new V[] {v_1923}, new Results(R.Table(new Entry((V)"root_seed", R.Get(v_1884, (V)"root_seed")), new Entry((V)"class_id", R.Get(v_1899, (V)"definition_id")), new Entry((V)"entity_id_base", (V)(R.Get(v_1884, (V)"next_entity_id") - (V)1.0d)), new Entry((V)"retired_stages", v_1908), new Entry((V)"route_history", v_1901), new Entry((V)"available_boons", R.Get(v_1884, (V)"available_boons")), new Entry((V)"meta", R.Get(v_1884, (V)"meta")))))));
v_1925 = values_1926[0];
} else {
var values_1927 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"new_stage"), R.Join(new V[] {v_1923}, new Results(R.Table(new Entry((V)"root_seed", R.Get(v_1884, (V)"root_seed")), new Entry((V)"class_id", R.Get(v_1899, (V)"definition_id")), new Entry((V)"entity_id_base", (V)(R.Get(v_1884, (V)"next_entity_id") - (V)1.0d)), new Entry((V)"retired_stages", v_1908), new Entry((V)"available_boons", R.Get(v_1884, (V)"available_boons")), new Entry((V)"meta", R.Get(v_1884, (V)"meta")), new Entry((V)"multifloor", (V)(R.Get(v_1884, (V)"floor_order") != V.Nil)), new Entry((V)"stage_options", R.Table(new Entry((V)"index", v_1921), new Entry((V)"route_history", v_1901), new Entry((V)"biome_id", R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_1895, (V)"biome_id")}, new Results((V)"route lacks biome"))).First))))))));
v_1925 = values_1927[0];
}
var values_1928 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1925))));
V v_1929 = values_1928[0];
var values_1930 = R.Join(new V[] {}, new Results(R.Get(v_1929, (V)"position")));
V v_1931 = values_1930[0];
var values_1932 = R.Join(new V[] {}, new Results(R.Get(v_1929, (V)"id")));
V v_1933 = values_1932[0];
foreach (var row_1934 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1929)))))) {
V v_1935 = row_1934[0];
var values_1936 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1937 = v_1929;
V key_1938 = v_1935;
R.Set(obj_1937, key_1938, values_1936[0]);
}
foreach (var row_1939 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1899)))))) {
V v_1940 = row_1939[0];
V v_1941 = row_1939[1];
var values_1942 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_1941))));
var obj_1943 = v_1929;
V key_1944 = v_1940;
R.Set(obj_1943, key_1944, values_1942[0]);
}
var values_1945 = R.Join(new V[] {}, new Results(v_1933));
var obj_1946 = v_1929;
V key_1947 = (V)"id";
R.Set(obj_1946, key_1947, values_1945[0]);
var values_1948 = R.Join(new V[] {}, new Results(v_1931));
var obj_1949 = v_1929;
V key_1950 = (V)"position";
R.Set(obj_1949, key_1950, values_1948[0]);
if (R.Truth(R.And(R.Get(v_1929, (V)"utility"), () => R.Get(R.Get(v_1929, (V)"utility"), (V)"planted")))) {
var values_1951 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1952 = v_1929;
V key_1953 = (V)"utility";
R.Set(obj_1952, key_1953, values_1951[0]);
}
R.Call(R.Get(v_4, (V)"rebase_stage_memory"), R.Join(new V[] {v_1929, R.Get(v_1884, (V)"clock")}, new Results(R.Get(v_1925, (V)"clock"))));
var values_1954 = R.Join(new V[] {}, new Results(R.And(R.Get(v_1929, (V)"statuses"), () => R.Get(R.Get(v_1929, (V)"statuses"), (V)"burning"))));
V v_1955 = values_1954[0];
if (R.Truth(v_1955)) {
var values_1956 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_1925, (V)"clock") + R.Get(v_1955, (V)"next_due")) - R.Get(v_1884, (V)"clock"))));
V v_1957 = values_1956[0];
var values_1958 = R.Join(new V[] {}, R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_1925}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", v_1957), new Entry((V)"actor_id", v_1933), new Entry((V)"attack_id", R.Get(R.Get(v_1955, (V)"activation"), (V)"root_action_id")), new Entry((V)"floor_id", R.Get(v_1931, (V)"floor_id")))))));
V v_1959 = values_1958[0];
var values_1960 = R.Join(new V[] {}, new Results(R.Get(v_1959, (V)"id")));
var obj_1961 = v_1959;
V key_1962 = (V)"effect_id";
R.Set(obj_1961, key_1962, values_1960[0]);
var values_1963 = R.Join(new V[] {}, new Results(v_1957));
var obj_1964 = v_1955;
V key_1965 = (V)"next_due";
R.Set(obj_1964, key_1965, values_1963[0]);

var values_1966 = R.Join(new V[] {}, new Results(R.Get(v_1959, (V)"id")));
var obj_1967 = v_1955;
V key_1968 = (V)"schedule_id";
R.Set(obj_1967, key_1968, values_1966[0]);
}
var values_1969 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1884, (V)"progression")))));
var obj_1970 = v_1925;
V key_1971 = (V)"progression";
R.Set(obj_1970, key_1971, values_1969[0]);
var values_1972 = R.Join(new V[] {}, new Results(R.Get(v_1884, (V)"currency")));
var obj_1973 = v_1925;
V key_1974 = (V)"currency";
R.Set(obj_1973, key_1974, values_1972[0]);
var values_1975 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_1884, (V)"rng")))));
var obj_1976 = v_1925;
V key_1977 = (V)"rng";
R.Set(obj_1976, key_1977, values_1975[0]);
var values_1978 = R.Join(new V[] {}, new Results(R.Get(v_1884, (V)"next_event_id")));
var obj_1979 = v_1925;
V key_1980 = (V)"next_event_id";
R.Set(obj_1979, key_1980, values_1978[0]);
var values_1981 = R.Join(new V[] {}, new Results((V)(R.Get(v_1884, (V)"next_action_id") + (V)1.0d)));
var obj_1982 = v_1925;
V key_1983 = (V)"next_action_id";
R.Set(obj_1982, key_1983, values_1981[0]);
var values_1984 = R.Join(new V[] {}, new Results(R.Get(v_1884, (V)"next_activation_id")));
var obj_1985 = v_1925;
V key_1986 = (V)"next_activation_id";
R.Set(obj_1985, key_1986, values_1984[0]);
var values_1987 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"time", R.Get(v_1884, (V)"clock")), new Entry((V)"cost", (V)0.0d), new Entry((V)"route_id", R.Get(v_1895, (V)"id")))));
var obj_1988 = v_1925;
V key_1989 = (V)"last_action";
R.Set(obj_1988, key_1989, values_1987[0]);
foreach (var row_1990 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1884)))))) {
V v_1991 = row_1990[0];
var values_1992 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1993 = v_1884;
V key_1994 = v_1991;
R.Set(obj_1993, key_1994, values_1992[0]);
}
foreach (var row_1995 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1925)))))) {
V v_1996 = row_1995[0];
V v_1997 = row_1995[1];
var values_1998 = R.Join(new V[] {}, new Results(v_1997));
var obj_1999 = v_1884;
V key_2000 = v_1996;
R.Set(obj_1999, key_2000, values_1998[0]);
}
R.Call(R.Get(v_70, (V)"recompute"), R.Join(new V[] {}, new Results(v_1884)));
R.Call(v_95, R.Join(new V[] {v_1884, v_1906, R.Or(R.And((V)(v_1897 >= (V)3.0d), () => (V)"FinalSanctumEntered"), () => (V)"StageEntered"), R.Table(new Entry((V)"stage_index", v_1921), new Entry((V)"route_id", R.Get(v_1895, (V)"id")), new Entry((V)"stage_seed", v_1923), new Entry((V)"floor_id", R.Get(v_1884, (V)"active_floor_id")), new Entry((V)"x", R.Get(v_1931, (V)"x")), new Entry((V)"y", R.Get(v_1931, (V)"y"))), R.Get(v_1884, (V)"player_id")}, new Results((V)(R.Get(v_1925, (V)"next_action_id") - (V)1.0d))));
return R.Join(new V[] {(V)true, V.Nil}, new Results(v_1906));
} else if (R.Truth((V)(R.Get(v_1885, (V)"kind") == (V)"interact"))) {
var values_2001 = R.Join(new V[] {}, R.Call(R.Get(v_52, (V)"preview"), R.Join(new V[] {v_1884}, new Results(v_1885))));
v_1889 = values_2001[0];
if (R.Truth((V)(!R.Truth(R.Get(v_1889, (V)"valid"))))) {
return R.Join(new V[] {(V)false, R.Get(v_1889, (V)"reason"), R.Table()}, new Results(v_1889));
}
var values_2002 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1884))));
V v_2003 = values_2002[0];

var values_2004 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {(V)100.0d}, new Results((V)(R.Get(v_2003, (V)"speed") + R.Call(R.Get(v_4, (V)"speed_bonus"), R.Join(new V[] {v_1884}, new Results(v_2003))).First)))));
var obj_2005 = v_1889;
V key_2006 = (V)"cost";
R.Set(obj_2005, key_2006, values_2004[0]);
} else if (R.Truth((V)(R.Get(v_1885, (V)"kind") == (V)"claim_chest"))) {
var values_2007 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1884}, new Results(R.Get(v_1885, (V)"chest_id")))));
v_1890 = values_2007[0];
if (R.Truth(R.Or((V)(!R.Truth(v_1890)), () => (V)(R.Get(v_1890, (V)"controller") != (V)"chest")))) {
return R.Join(new V[] {(V)false, (V)"unknown chest"}, new Results(R.Table()));
}
var values_2008 = R.Join(new V[] {}, R.Call(R.Get(v_36, (V)"can_claim"), R.Join(new V[] {v_1884, v_1890}, new Results(R.Get(v_1885, (V)"offer_index")))));
V v_2009 = values_2008[0];
V v_2010 = values_2008[1];
if (R.Truth((V)(!R.Truth(v_2009)))) {
return R.Join(new V[] {(V)false, v_2010}, new Results(R.Table()));
}
var values_2011 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)true), new Entry((V)"cost", R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {R.Get(v_81, (V)"claim_chest")}, new Results((V)(R.Get(R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1884))).First, (V)"speed") + R.Call(R.Get(v_4, (V)"speed_bonus"), R.Join(new V[] {v_1884}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1884))))).First)))).First))));
v_1889 = values_2011[0];
} else {
var values_2012 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"preview"), R.Join(new V[] {v_1884, R.Get(v_1885, (V)"kind"), R.Get(v_1885, (V)"dx"), R.Get(v_1885, (V)"dy"), R.Get(v_1885, (V)"steps")}, new Results(v_1885))));
v_1889 = values_2012[0];
if (R.Truth((V)(!R.Truth(R.Get(v_1889, (V)"valid"))))) {
return R.Join(new V[] {(V)false, R.Get(v_1889, (V)"reason"), R.Table()}, new Results(v_1889));
}
}
R.Call(R.Get(v_38, (V)"pop"), R.Join(new V[] {}, new Results(v_1884)));
var values_2013 = R.Join(new V[] {}, new Results(R.Table()));
V v_2014 = values_2013[0];
var values_2015 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"events", R.Table()), new Entry((V)"event_head", (V)1.0d), new Entry((V)"effects", R.Table()), new Entry((V)"effect_head", (V)1.0d))));
var obj_2016 = v_83;
V key_2017 = v_2014;
R.Set(obj_2016, key_2017, values_2015[0]);
var values_2018 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_1884))));
V v_2019 = values_2018[0];
var values_2020 = R.Join(new V[] {}, new Results(R.Get(v_2019, (V)"position")));
V v_2021 = values_2020[0];
var values_2022 = R.Join(new V[] {}, new Results(R.Get(v_1884, (V)"next_action_id")));
V v_2023 = values_2022[0];
var values_2024 = R.Join(new V[] {}, new Results((V)(v_2023 + (V)1.0d)));
var obj_2025 = v_1884;
V key_2026 = (V)"next_action_id";
R.Set(obj_2025, key_2026, values_2024[0]);
var values_2027 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.And(R.Get(v_1889, (V)"bump"), () => (V)"basic_attack"), () => R.And(R.Get(v_1889, (V)"door_id"), () => (V)"open_door")), () => R.And(R.Get(v_1889, (V)"vault_id"), () => (V)"open_vault")), () => R.Get(v_1885, (V)"kind"))));
V v_2028 = values_2027[0];
var values_2029 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_2028), new Entry((V)"time", R.Get(v_1884, (V)"clock")), new Entry((V)"cost", R.Get(v_1889, (V)"cost")))));
V v_2030 = values_2029[0];
if (R.Truth(R.Get(v_1889, (V)"vow"))) {
var values_2031 = R.Join(new V[] {}, new Results(R.Get(v_2019, (V)"hp")));
V v_2032 = values_2031[0];
var values_2033 = R.Join(new V[] {}, new Results((V)(v_2032 - R.Get(R.Get(v_1889, (V)"vow"), (V)"hp_cost"))));
var obj_2034 = v_2019;
V key_2035 = (V)"hp";
R.Set(obj_2034, key_2035, values_2033[0]);
var values_2036 = R.Join(new V[] {}, new Results(R.Get(v_1889, (V)"vow")));
var obj_2037 = R.Get(v_83, v_2014);
V key_2038 = (V)"vow";
R.Set(obj_2037, key_2038, values_2036[0]);
var values_2039 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1889, (V)"vow"), (V)"hp_cost")));
var obj_2040 = v_2030;
V key_2041 = (V)"vow_hp_cost";
R.Set(obj_2040, key_2041, values_2039[0]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"VowPaid", R.Table(new Entry((V)"boon_id", R.Get(R.Get(v_1889, (V)"vow"), (V)"boon_id")), new Entry((V)"hp_before", v_2032), new Entry((V)"hp_after", R.Get(v_2019, (V)"hp")), new Entry((V)"hp_cost", R.Get(R.Get(v_1889, (V)"vow"), (V)"hp_cost")), new Entry((V)"bonus", R.Get(R.Get(v_1889, (V)"vow"), (V)"bonus")), new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(v_2021, (V)"x")), new Entry((V)"y", R.Get(v_2021, (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
}
if (R.Truth(R.Get(v_1889, (V)"bump"))) {
var values_2042 = R.Join(new V[] {}, new Results((V)true));
var obj_2043 = v_2030;
V key_2044 = (V)"bump";
R.Set(obj_2043, key_2044, values_2042[0]);
}
V v_2045 = V.Nil;
v_2045 = new V((RuleFunction)(args_2046 => {
V v_2047 = args_2046[0];
V v_2048 = args_2046[1];
V v_2049 = args_2046[2];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2047, v_2048, v_2049}, new Results(v_2023)));
return Results.Empty;
}));
R.Call(R.Get(v_52, (V)"before_action"), R.Join(new V[] {v_1884, v_2028, v_1889}, new Results(v_2045)));
if (R.Truth((V)(v_2028 == (V)"use_utility"))) {
R.Call(R.Get(v_52, (V)"use_utility"), R.Join(new V[] {v_1884, v_1889}, new Results(v_2045)));
}
if (R.Truth(R.And((V)(v_2028 == (V)"dash"), () => R.Get(v_1889, (V)"beacon_return")))) {
var values_2050 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_1884, (V)"class_special"), (V)"beacon")))));
V v_2051 = values_2050[0];

var values_2052 = R.Join(new V[] {R.Get(v_2021, (V)"x")}, new Results(R.Get(v_2021, (V)"y")));
V v_2053 = values_2052[0];
V v_2054 = values_2052[1];
var values_2055 = R.Join(new V[] {R.Get(v_2051, (V)"x")}, new Results(R.Get(v_2051, (V)"y")));
var obj_2056 = v_2021;
V key_2057 = (V)"x";
var obj_2058 = v_2021;
V key_2059 = (V)"y";
R.Set(obj_2056, key_2057, values_2055[0]);
R.Set(obj_2058, key_2059, values_2055[1]);

var values_2060 = R.Join(new V[] {}, new Results(V.Nil));
var obj_2061 = R.Get(v_1884, (V)"class_special");
V key_2062 = (V)"beacon";
R.Set(obj_2061, key_2062, values_2060[0]);
var values_2063 = R.Join(new V[] {}, new Results(R.Get(v_1889, (V)"return_distance")));
var obj_2064 = v_2030;
V key_2065 = (V)"distance";
R.Set(obj_2064, key_2065, values_2063[0]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"Displaced", R.Table(new Entry((V)"target_id", R.Get(v_2019, (V)"id")), new Entry((V)"reason", (V)"beacon"), new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"from_x", v_2053), new Entry((V)"from_y", v_2054), new Entry((V)"x", R.Get(v_2051, (V)"x")), new Entry((V)"y", R.Get(v_2051, (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"TileEntered", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"from_x", v_2053), new Entry((V)"from_y", v_2054), new Entry((V)"x", R.Get(v_2051, (V)"x")), new Entry((V)"y", R.Get(v_2051, (V)"y")), new Entry((V)"forced", (V)true)), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
R.Call(R.Get(v_36, (V)"pickup_at"), R.Join(new V[] {v_1884, v_2019, R.Get(v_2051, (V)"x"), R.Get(v_2051, (V)"y")}, new Results(new V((RuleFunction)(args_2066 => {
V v_2067 = args_2066[0];
V v_2068 = args_2066[1];
V v_2069 = args_2066[2];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2067, v_2068, v_2069}, new Results(v_2023)));
return Results.Empty;
})))));
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"BeaconReturned", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(v_2051, (V)"x")), new Entry((V)"y", R.Get(v_2051, (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
} else if (R.Truth(R.Or(R.Or((V)(v_2028 == (V)"move"), () => (V)(v_2028 == (V)"dash")), () => R.And((V)(v_2028 == (V)"use_utility"), () => R.Get(v_1889, (V)"utility_move"))))) {
var values_2070 = R.Join(new V[] {R.Get(v_2021, (V)"x")}, new Results(R.Get(v_2021, (V)"y")));
V v_2071 = values_2070[0];
V v_2072 = values_2070[1];
var values_2073 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Or(R.Get(v_1889, (V)"swap"), () => R.Get(v_1889, (V)"utility_return")), () => (V)(R.Get(R.Get(R.Get(v_1889, (V)"cells"), (V)1.0d), (V)"x") - v_2071)), () => R.Get(v_1885, (V)"dx"))));
V v_2074 = values_2073[0];
var values_2075 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Or(R.Get(v_1889, (V)"swap"), () => R.Get(v_1889, (V)"utility_return")), () => (V)(R.Get(R.Get(R.Get(v_1889, (V)"cells"), (V)1.0d), (V)"y") - v_2072)), () => R.Get(v_1885, (V)"dy"))));
V v_2076 = values_2075[0];
var values_2077 = R.Join(new V[] {}, new Results((V)(v_2071 + (V)(v_2074 * R.Get(v_1889, (V)"distance")))));
var obj_2078 = v_2021;
V key_2079 = (V)"x";
R.Set(obj_2078, key_2079, values_2077[0]);
var values_2080 = R.Join(new V[] {}, new Results((V)(v_2072 + (V)(v_2076 * R.Get(v_1889, (V)"distance")))));
var obj_2081 = v_2021;
V key_2082 = (V)"y";
R.Set(obj_2081, key_2082, values_2080[0]);
var values_2083 = R.Join(new V[] {v_2074}, new Results(v_2076));
var obj_2084 = v_2030;
V key_2085 = (V)"dx";
var obj_2086 = v_2030;
V key_2087 = (V)"dy";
R.Set(obj_2084, key_2085, values_2083[0]);
R.Set(obj_2086, key_2087, values_2083[1]);
if (R.Truth((V)(v_2028 == (V)"dash"))) {
var values_2088 = R.Join(new V[] {}, new Results(R.Get(v_1889, (V)"distance")));
var obj_2089 = v_2030;
V key_2090 = (V)"distance";
R.Set(obj_2089, key_2090, values_2088[0]);
}
var values_2091 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"from_x", v_2071), new Entry((V)"from_y", v_2072), new Entry((V)"x", R.Get(v_2021, (V)"x")), new Entry((V)"y", R.Get(v_2021, (V)"y")))));
V v_2092 = values_2091[0];
if (R.Truth(R.Or((V)(v_2028 == (V)"dash"), () => R.Get(v_1889, (V)"utility_move")))) {
var values_2093 = R.Join(new V[] {}, new Results((V)"core:ability/dash"));
var obj_2094 = v_2092;
V key_2095 = (V)"ability_id";
R.Set(obj_2094, key_2095, values_2093[0]);
var values_2096 = R.Join(new V[] {}, new Results(R.Table()));
var obj_2097 = v_2092;
V key_2098 = (V)"path";
R.Set(obj_2097, key_2098, values_2096[0]);
{ double end_2101 = R.Num(R.Get(v_1889, (V)"distance")), step_2102 = R.Num((V)1.0d);
for (double i_2100 = R.Num((V)1.0d); step_2102 > 0 ? i_2100 <= end_2101 : i_2100 >= end_2101; i_2100 += step_2102) {
V v_2099 = i_2100;
var values_2103 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(R.Get(v_1889, (V)"cells"), v_2099), (V)"x")), new Entry((V)"y", R.Get(R.Get(R.Get(v_1889, (V)"cells"), v_2099), (V)"y")))));
var obj_2104 = R.Get(v_2092, (V)"path");
V key_2105 = v_2099;
R.Set(obj_2104, key_2105, values_2103[0]);
}
}
}
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"Moved", v_2092, R.Get(v_2019, (V)"id")}, new Results(v_2023)));
{ double end_2108 = R.Num(R.Get(v_1889, (V)"distance")), step_2109 = R.Num((V)1.0d);
for (double i_2107 = R.Num((V)1.0d); step_2109 > 0 ? i_2107 <= end_2108 : i_2107 >= end_2108; i_2107 += step_2109) {
V v_2106 = i_2107;
var values_2110 = R.Join(new V[] {R.Get(R.Get(R.Get(v_1889, (V)"cells"), v_2106), (V)"x")}, new Results(R.Get(R.Get(R.Get(v_1889, (V)"cells"), v_2106), (V)"y")));
V v_2111 = values_2110[0];
V v_2112 = values_2110[1];
var values_2113 = R.Join(new V[] {}, new Results((V)(v_2071 + (V)(v_2074 * (V)(v_2106 - (V)1.0d)))));
V v_2114 = values_2113[0];
var values_2115 = R.Join(new V[] {}, new Results((V)(v_2072 + (V)(v_2076 * (V)(v_2106 - (V)1.0d)))));
V v_2116 = values_2115[0];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"TileEntered", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"from_x", v_2114), new Entry((V)"from_y", v_2116), new Entry((V)"x", v_2111), new Entry((V)"y", v_2112), new Entry((V)"source_position", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", v_2111), new Entry((V)"y", v_2112)))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
R.Call(R.Get(v_36, (V)"pickup_at"), R.Join(new V[] {v_1884, v_2019, v_2111, v_2112}, new Results(new V((RuleFunction)(args_2117 => {
V v_2118 = args_2117[0];
V v_2119 = args_2117[1];
V v_2120 = args_2117[2];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2118, v_2119, v_2120}, new Results(v_2023)));
return Results.Empty;
})))));
}
}
if (R.Truth(R.Get(v_1889, (V)"swap"))) {
var values_2121 = R.Join(new V[] {}, new Results(V.Nil));
var obj_2122 = R.Get(v_1884, (V)"class_special");
V key_2123 = (V)"decoy";
R.Set(obj_2122, key_2123, values_2121[0]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"DecoySwapped", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(v_2021, (V)"x")), new Entry((V)"y", R.Get(v_2021, (V)"y")), new Entry((V)"from_x", v_2071), new Entry((V)"from_y", v_2072)), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
}
if (R.Truth(R.And((V)(v_2028 == (V)"move"), () => R.Get(v_1889, (V)"stair")))) {
var values_2124 = R.Join(new V[] {}, new Results(R.Get(v_2021, (V)"floor_id")));
V v_2125 = values_2124[0];
var values_2126 = R.Join(new V[] {}, R.Call(R.Get(v_32, (V)"change"), R.Join(new V[] {v_1884, R.Get(v_1889, (V)"stair")}, new Results(R.Get(v_1889, (V)"landing")))));
V v_2127 = values_2126[0];
V v_2128 = values_2126[1];
var values_2129 = R.Join(new V[] {}, new Results(R.Get(v_2019, (V)"position")));
V v_2130 = values_2129[0];
var values_2131 = R.Join(new V[] {}, new Results(R.Get(v_2130, (V)"floor_id")));
var obj_2132 = v_2030;
V key_2133 = (V)"to_floor_id";
R.Set(obj_2132, key_2133, values_2131[0]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"FloorChanged", R.Table(new Entry((V)"from_floor_id", v_2125), new Entry((V)"to_floor_id", R.Get(v_2130, (V)"floor_id")), new Entry((V)"floor_id", R.Get(v_2130, (V)"floor_id")), new Entry((V)"from_x", R.Get(R.Get(v_1889, (V)"stair"), (V)"x")), new Entry((V)"from_y", R.Get(R.Get(v_1889, (V)"stair"), (V)"y")), new Entry((V)"x", R.Get(v_2130, (V)"x")), new Entry((V)"y", R.Get(v_2130, (V)"y")), new Entry((V)"stair_id", R.Get(R.Get(v_1889, (V)"stair"), (V)"id"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
if (R.Truth(v_2127)) {
if (R.Truth(v_2128)) {
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"WindupCancelled", R.Table(new Entry((V)"target_id", R.Get(R.Get(v_1884, (V)"stage"), (V)"pursuer_id")), new Entry((V)"windup_id", R.Get(v_2128, (V)"schedule_id")), new Entry((V)"reason", (V)"floor_change"), new Entry((V)"floor_id", v_2125), new Entry((V)"x", R.Get(R.Get(v_1889, (V)"stair"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_1889, (V)"stair"), (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
}
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"PursuerRelocated", R.Table(new Entry((V)"pursuer_id", R.Get(R.Get(v_1884, (V)"stage"), (V)"pursuer_id")), new Entry((V)"floor_id", R.Get(v_2130, (V)"floor_id")), new Entry((V)"x", R.Get(v_2127, (V)"x")), new Entry((V)"y", R.Get(v_2127, (V)"y"))), R.Get(R.Get(v_1884, (V)"stage"), (V)"pursuer_id")}, new Results(v_2023)));
}
}
} else if (R.Truth(R.Or((V)(v_2028 == (V)"basic_attack"), () => (V)(v_2028 == (V)"sweeping_slash")))) {
var values_2134 = R.Join(new V[] {R.Get(v_1885, (V)"dx")}, new Results(R.Get(v_1885, (V)"dy")));
var obj_2135 = v_2030;
V key_2136 = (V)"dx";
var obj_2137 = v_2030;
V key_2138 = (V)"dy";
R.Set(obj_2135, key_2136, values_2134[0]);
R.Set(obj_2137, key_2138, values_2134[1]);
var values_2139 = R.Join(new V[] {}, new Results(R.Table()));
V v_2140 = values_2139[0];
var values_2141 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"effective_ability"), R.Join(new V[] {v_1884, v_2028, R.Get(v_1885, (V)"dx"), R.Get(v_1885, (V)"dy")}, new Results((V)(!R.Truth(R.Get(v_1889, (V)"bump")))))));
V v_2142 = values_2141[0];
V v_2143 = values_2141[1];
V v_2144 = values_2141[2];
V v_2145 = values_2141[3];
V v_2146 = values_2141[4];
V v_2147 = values_2141[5];
foreach (var row_2148 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_2142)))))) {
V v_2149 = row_2148[0];
V v_2150 = row_2148[1];
var values_2151 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(v_2150, (V)"x")), new Entry((V)"y", R.Get(v_2150, (V)"y")))));
var obj_2152 = v_2140;
V key_2153 = v_2149;
R.Set(obj_2152, key_2153, values_2151[0]);
}
if (R.Truth((V)(v_2144 == R.Get(R.Get(v_24, (V)"definition"), (V)"special_id")))) {
var values_2157 = R.Join(new V[] {}, R.Call(R.Get(v_24, (V)"plant_preview"), R.Join(new V[] {v_1884, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1884))).First, v_2019, R.Get(v_2140, (V)1.0d), R.Get(v_70, (V)"can_see"), R.Get(v_70, (V)"was_explored")}, new Results(new V((RuleFunction)(args_2154 => {
V v_2155 = args_2154[0];
V v_2156 = args_2154[1];
return R.Join(new V[] {}, R.Call(v_144, R.Join(new V[] {v_1884, R.Get(v_2021, (V)"floor_id"), v_2155, v_2156, R.Get(v_2019, (V)"id")}, new Results((V)false))));
return Results.Empty;
}))))));
V v_2158 = values_2157[0];
V v_2159 = values_2157[1];
V v_2160 = values_2157[2];
R.Call(R.Get(v_24, (V)"plant"), R.Join(new V[] {v_1884, v_2019, R.Get(v_2140, (V)1.0d)}, new Results(v_2160)));
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"BeaconPlanted", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_2140, (V)1.0d), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_2140, (V)1.0d), (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
} else if (R.Truth((V)(v_2144 == R.Get(R.Get(v_22, (V)"definition"), (V)"special_id")))) {
R.Call(R.Get(v_22, (V)"lock"), R.Join(new V[] {v_1884, v_2019, v_2140, v_2023}, new Results(new V((RuleFunction)(args_2161 => {
V v_2162 = args_2161[0];
V v_2163 = args_2161[1];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2162, v_2163, R.Get(v_2019, (V)"id")}, new Results(v_2023)));
return Results.Empty;
})))));
} else if (R.Truth((V)(v_2144 == R.Get(R.Get(v_20, (V)"tinker"), (V)"special_id")))) {
R.Call(R.Get(v_20, (V)"deploy"), R.Join(new V[] {v_1884, v_2019, R.Get(v_2140, (V)1.0d), v_2023}, new Results(new V((RuleFunction)(args_2164 => {
V v_2165 = args_2164[0];
V v_2166 = args_2164[1];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2165, v_2166, R.Get(v_2019, (V)"id")}, new Results(v_2023)));
return Results.Empty;
})))));
} else if (R.Truth((V)(v_2144 == R.Get(R.Get(v_20, (V)"spirit"), (V)"special_id")))) {
var values_2167 = R.Join(new V[] {}, R.Call(R.Get(v_20, (V)"preview"), R.Join(new V[] {v_1884, v_2019, v_2140, v_12}, new Results(R.Get(v_70, (V)"can_see")))));
V v_2168 = values_2167[0];
V v_2169 = values_2167[1];
V v_2170 = values_2167[2];
V v_2171 = values_2167[3];
if (R.Truth((V)(v_2170 == (V)"strike"))) {
var values_2172 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_2019))));
V v_2173 = values_2172[0];

var values_2174 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_2171, (V)"position")))));
var obj_2175 = v_2173;
V key_2176 = (V)"position";
R.Set(obj_2175, key_2176, values_2174[0]);
R.Call(v_1334, R.Join(new V[] {v_1884, v_2014, v_2173, v_2023, v_2144, v_2140, R.Get(v_2019, (V)"slash_damage"), (V)false}, new Results(R.Table(new Entry((V)"damage_type", (V)"spirit")))));
} else {
R.Call(R.Get(v_20, (V)"place"), R.Join(new V[] {v_1884, v_2019, R.Get(v_2140, (V)1.0d)}, new Results(new V((RuleFunction)(args_2177 => {
V v_2178 = args_2177[0];
V v_2179 = args_2177[1];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2178, v_2179, R.Get(v_2019, (V)"id")}, new Results(v_2023)));
return Results.Empty;
})))));
}
} else if (R.Truth((V)(v_2144 == R.Get(R.Get(v_18, (V)"definition"), (V)"special_id")))) {
R.Call(R.Get(v_18, (V)"place"), R.Join(new V[] {v_1884, v_2019, R.Get(R.Get(v_2140, (V)1.0d), (V)"x"), R.Get(R.Get(v_2140, (V)1.0d), (V)"y"), v_2023}, new Results(new V((RuleFunction)(args_2180 => {
V v_2181 = args_2180[0];
V v_2182 = args_2180[1];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2181, v_2182, R.Get(v_2019, (V)"id")}, new Results(v_2023)));
return Results.Empty;
})))));
} else if (R.Truth((V)(v_2144 == (V)"core:ability/duelist_parry"))) {
R.Call(R.Get(v_16, (V)"raise"), R.Join(new V[] {v_1884, v_2019, R.Get(v_1885, (V)"dx"), R.Get(v_1885, (V)"dy")}, new Results(R.Get(v_1889, (V)"cost"))));
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"DuelistGuardRaised", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(v_2021, (V)"x")), new Entry((V)"y", R.Get(v_2021, (V)"y")), new Entry((V)"dx", R.Get(v_1885, (V)"dx")), new Entry((V)"dy", R.Get(v_1885, (V)"dy")), new Entry((V)"expires", (V)(R.Get(v_1884, (V)"clock") + R.Get(v_1889, (V)"cost")))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
} else if (R.Truth((V)(v_2144 == (V)"core:ability/shade_decoy"))) {
var values_2183 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_2140, (V)1.0d), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_2140, (V)1.0d), (V)"y")))));
var obj_2184 = R.Get(v_1884, (V)"class_special");
V key_2185 = (V)"decoy";
R.Set(obj_2184, key_2185, values_2183[0]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"DecoyPlaced", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_2140, (V)1.0d), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_2140, (V)1.0d), (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
} else if (R.Truth((V)(v_2144 == (V)"core:ability/trapper_snare"))) {
var values_2186 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_2140, (V)1.0d), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_2140, (V)1.0d), (V)"y")))));
var obj_2187 = R.Get(v_1884, (V)"class_special");
V key_2188 = (V)"snare";
R.Set(obj_2187, key_2188, values_2186[0]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"SnarePlaced", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_2140, (V)1.0d), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_2140, (V)1.0d), (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
} else {
if (R.Truth(R.And((V)(v_2144 == (V)"core:ability/duelist_thrust"), () => R.Get(v_2019, (V)"duelist_riposte")))) {
var values_2189 = R.Join(new V[] {}, new Results(V.Nil));
var obj_2190 = v_2019;
V key_2191 = (V)"duelist_riposte";
R.Set(obj_2190, key_2191, values_2189[0]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"DuelistRiposteSpent", R.Table(new Entry((V)"bonus", R.Get(v_16, (V)"bonus")), new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(v_2021, (V)"x")), new Entry((V)"y", R.Get(v_2021, (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
}
if (R.Truth(v_2147)) {
R.Call(R.Get(v_60, (V)"consume"), R.Join(new V[] {v_1884}, new Results(v_2147)));
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"MirrorReflected", R.Table(new Entry((V)"reflection", R.Call(v_84, R.Join(new V[] {}, new Results(v_2147))).First), new Entry((V)"target_cells", R.Call(v_84, R.Join(new V[] {}, new Results(v_2140))).First), new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(v_2147, (V)"x")), new Entry((V)"y", R.Get(v_2147, (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
}
var values_2192 = Results.Empty;
V v_2193 = values_2192[0];
if (R.Truth((V)(v_2144 == (V)"core:ability/hookrunner_hook"))) {
foreach (var row_2194 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_2140)))))) {
V v_2195 = row_2194[0];
V v_2196 = row_2194[1];
var values_2197 = R.Join(new V[] {}, R.Call(v_144, R.Join(new V[] {v_1884, R.Get(v_2196, (V)"floor_id"), R.Get(v_2196, (V)"x"), R.Get(v_2196, (V)"y"), R.Get(v_2019, (V)"id")}, new Results((V)true))));
v_2193 = values_2197[0];
if (R.Truth(v_2193)) {
break;
}
}
}
var values_2198 = R.Join(new V[] {}, R.Call(v_1334, R.Join(new V[] {v_1884, v_2014, v_2019, v_2023, v_2144, v_2140, v_2143, v_2145}, new Results(R.Table(new Entry((V)"bump", R.Or(R.Get(v_1889, (V)"bump"), () => V.Nil)), new Entry((V)"damage_type", v_2146), new Entry((V)"reflection", R.Call(v_84, R.Join(new V[] {}, new Results(v_2147))).First), new Entry((V)"focus_included", (V)true))))));
V v_2199 = values_2198[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(v_2193, () => (V)(!R.Truth(R.Get(v_2199, (V)"fate_miss")))), () => (V)(R.Get(v_2193, (V)"hp") > (V)0.0d)), () => (V)(!R.Truth(R.Get(v_2193, (V)"invulnerable")))), () => (V)(R.Get(v_2193, (V)"ai_id") != (V)"warden")), () => (V)(R.Get(v_2193, (V)"ai_id") != (V)"biome_elite")), () => (V)(R.Get(v_2193, (V)"ai_id") != (V)"hollow_regent")))) {
{ double end_2202 = R.Num((V)2.0d), step_2203 = R.Num((V)1.0d);
for (double i_2201 = R.Num((V)1.0d); step_2203 > 0 ? i_2201 <= end_2202 : i_2201 >= end_2202; i_2201 += step_2203) {
V v_2200 = i_2201;
var values_2204 = R.Join(new V[] {(V)(R.Get(R.Get(v_2193, (V)"position"), (V)"x") - R.Get(v_1885, (V)"dx"))}, new Results((V)(R.Get(R.Get(v_2193, (V)"position"), (V)"y") - R.Get(v_1885, (V)"dy"))));
V v_2205 = values_2204[0];
V v_2206 = values_2204[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1884))).First, v_2205}, new Results(v_2206))).First)), () => R.Call(v_144, R.Join(new V[] {v_1884, R.Get(R.Get(v_2193, (V)"position"), (V)"floor_id"), v_2205, v_2206, R.Get(v_2193, (V)"id")}, new Results((V)false))).First))) {
break;
}
var values_2207 = R.Join(new V[] {R.Get(R.Get(v_2193, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_2193, (V)"position"), (V)"y")));
V v_2208 = values_2207[0];
V v_2209 = values_2207[1];
var values_2210 = R.Join(new V[] {v_2205}, new Results(v_2206));
var obj_2211 = R.Get(v_2193, (V)"position");
V key_2212 = (V)"x";
var obj_2213 = R.Get(v_2193, (V)"position");
V key_2214 = (V)"y";
R.Set(obj_2211, key_2212, values_2210[0]);
R.Set(obj_2213, key_2214, values_2210[1]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"Displaced", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_2193, (V)"position"), (V)"floor_id")), new Entry((V)"from_x", v_2208), new Entry((V)"from_y", v_2209), new Entry((V)"x", v_2205), new Entry((V)"y", v_2206), new Entry((V)"target_id", R.Get(v_2193, (V)"id"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
}
}
}
}
} else if (R.Truth((V)(v_2028 == (V)"open_door"))) {
var values_2215 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1884}, new Results(R.Get(v_1889, (V)"door_id")))).First}, new Results((V)"missing door"))));
V v_2216 = values_2215[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_2216, (V)"controller") == (V)"door"), () => (V)(!R.Truth(R.Get(v_2216, (V)"open"))))}, new Results((V)"door already opened")));
var values_2217 = R.Join(new V[] {}, new Results((V)true));
var obj_2218 = v_2216;
V key_2219 = (V)"open";
R.Set(obj_2218, key_2219, values_2217[0]);
var values_2220 = R.Join(new V[] {}, new Results((V)false));
var obj_2221 = v_2216;
V key_2222 = (V)"blocks_movement";
R.Set(obj_2221, key_2222, values_2220[0]);
var values_2223 = R.Join(new V[] {}, new Results((V)false));
var obj_2224 = v_2216;
V key_2225 = (V)"blocks_vision";
R.Set(obj_2224, key_2225, values_2223[0]);
var values_2226 = R.Join(new V[] {}, new Results((V)false));
var obj_2227 = v_2216;
V key_2228 = (V)"blocks_projectiles";
R.Set(obj_2227, key_2228, values_2226[0]);
var values_2229 = R.Join(new V[] {}, new Results(R.Get(v_2216, (V)"id")));
var obj_2230 = v_2030;
V key_2231 = (V)"door_id";
R.Set(obj_2230, key_2231, values_2229[0]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"DoorOpened", R.Table(new Entry((V)"door_id", R.Get(v_2216, (V)"id")), new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_2216, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_2216, (V)"position"), (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
} else if (R.Truth((V)(v_2028 == (V)"open_vault"))) {
var values_2232 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_1884}, new Results(R.Get(v_1889, (V)"vault_id")))).First}, new Results((V)"missing vault"))));
V v_2233 = values_2232[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_1884, (V)"review_keys"), R.Get(v_2021, (V)"floor_id"))}, new Results((V)"vault key vanished")));
var values_2234 = R.Join(new V[] {}, new Results(V.Nil));
var obj_2235 = R.Get(v_1884, (V)"review_keys");
V key_2236 = R.Get(v_2021, (V)"floor_id");
R.Set(obj_2235, key_2236, values_2234[0]);
var values_2237 = R.Join(new V[] {}, new Results((V)true));
var obj_2238 = v_2233;
V key_2239 = (V)"opened";
R.Set(obj_2238, key_2239, values_2237[0]);

var values_2240 = R.Join(new V[] {}, new Results((V)false));
var obj_2241 = v_2233;
V key_2242 = (V)"blocks_movement";
R.Set(obj_2241, key_2242, values_2240[0]);
var values_2243 = R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(v_34, (V)"derive"), R.Join(new V[] {R.Get(v_1884, (V)"run_seed")}, new Results(R.Concat(R.Get(v_2021, (V)"floor_id"), R.Concat((V)":vault:", R.Concat(R.Get(R.Get(v_2233, (V)"position"), (V)"x"), R.Concat((V)",", R.Get(R.Get(v_2233, (V)"position"), (V)"y")))))))).First % (V)4.0d) + (V)1.0d)));
V v_2244 = values_2243[0];
var values_2245 = R.Join(new V[] {}, new Results(R.Get(R.Table(new Entry((V)1, (V)"cache"), new Entry((V)2, (V)"clue"), new Entry((V)3, (V)"boon"), new Entry((V)4, (V)"ambush")), v_2244)));
V v_2246 = values_2245[0];
var values_2247 = R.Join(new V[] {}, new Results(R.Get(v_2233, (V)"id")));
var obj_2248 = v_2030;
V key_2249 = (V)"vault_id";
R.Set(obj_2248, key_2249, values_2247[0]);

var values_2250 = R.Join(new V[] {}, new Results(v_2246));
var obj_2251 = v_2030;
V key_2252 = (V)"outcome";
R.Set(obj_2251, key_2252, values_2250[0]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"VaultOpened", R.Table(new Entry((V)"vault_id", R.Get(v_2233, (V)"id")), new Entry((V)"outcome", v_2246), new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_2233, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_2233, (V)"position"), (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
var values_2253 = R.Join(new V[] {R.Get(R.Get(v_2233, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_2233, (V)"position"), (V)"y")));
V v_2254 = values_2253[0];
V v_2255 = values_2253[1];
var values_2260 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_2256 => {
V v_2257 = args_2256[0];
V v_2258 = args_2256[1];
V v_2259 = args_2256[2];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2257, v_2258, v_2259}, new Results(v_2023)));
return Results.Empty;
}))));
V v_2261 = values_2260[0];
if (R.Truth((V)(v_2246 == (V)"cache"))) {
R.Call(R.Get(v_36, (V)"spawn_currency"), R.Join(new V[] {v_1884, (V)5.0d, R.Get(v_2021, (V)"floor_id"), v_2254, v_2255, v_2261}, new Results(R.Get(v_2019, (V)"id"))));
R.Call(R.Get(v_36, (V)"spawn_supply"), R.Join(new V[] {v_1884, (V)"health", R.Get(v_2021, (V)"floor_id"), v_2254, v_2255, v_2261}, new Results(R.Get(v_2019, (V)"id"))));
} else if (R.Truth((V)(v_2246 == (V)"clue"))) {
R.Call(R.Get(v_36, (V)"spawn_currency"), R.Join(new V[] {v_1884, (V)4.0d, R.Get(v_2021, (V)"floor_id"), v_2254, v_2255, v_2261}, new Results(R.Get(v_2019, (V)"id"))));
foreach (var row_2262 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1884, (V)"secrets"), () => R.Table()))))))) {
V v_2263 = row_2262[0];
V v_2264 = row_2262[1];
if (R.Truth(R.And((V)(R.Get(v_2264, (V)"floor_id") == R.Get(v_2021, (V)"floor_id")), () => (V)(!R.Truth(R.Get(v_2264, (V)"revealed")))))) {
var values_2265 = R.Join(new V[] {(V)(R.Get(v_2264, (V)"x") - R.Get(v_2021, (V)"x"))}, new Results((V)(R.Get(v_2264, (V)"y") - R.Get(v_2021, (V)"y"))));
V v_2266 = values_2265[0];
V v_2267 = values_2265[1];
var values_2268 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_2266))).First >= R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_2267))).First), () => R.Or(R.And((V)(v_2266 < (V)0.0d), () => (V)"west"), () => (V)"east")), () => R.Or(R.And((V)(v_2267 < (V)0.0d), () => (V)"north"), () => (V)"south"))));
V v_2269 = values_2268[0];
var values_2270 = R.Join(new V[] {}, new Results(v_2269));
var obj_2271 = R.Get(v_1884, (V)"review_clues");
V key_2272 = R.Get(v_2021, (V)"floor_id");
R.Set(obj_2271, key_2272, values_2270[0]);
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"VaultClueFound", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"direction", v_2269)), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
break;
}
}
} else if (R.Truth((V)(v_2246 == (V)"boon"))) {
R.Call(R.Get(v_74, (V)"spawn_chest"), R.Join(new V[] {v_1884, (V)"core:chest/free_elite", v_2254, v_2255}, new Results(R.Concat((V)"review:vault:", R.Get(v_2021, (V)"floor_id")))));
} else {
var values_2273 = R.Join(new V[] {}, R.Call(R.Get(v_26, (V)"get"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1884))).First, (V)"biome_id")))));
V v_2274 = values_2273[0];
var values_2275 = R.Join(new V[] {}, new Results(R.Or(R.And(v_2274, () => R.Get(R.Get(v_2274, (V)"enemy_ids"), (V)1.0d)), () => (V)"core:enemy/mossbound_guard")));
V v_2276 = values_2275[0];
foreach (var row_2277 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_2278 = row_2277[0];
V v_2279 = row_2277[1];
var values_2280 = R.Join(new V[] {(V)(v_2254 + R.Get(v_2279, (V)1.0d))}, new Results((V)(v_2255 + R.Get(v_2279, (V)2.0d))));
V v_2281 = values_2280[0];
V v_2282 = values_2280[1];
if (R.Truth(R.And(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_1884))).First, v_2281}, new Results(v_2282))).First, () => (V)(!R.Truth(R.Call(v_144, R.Join(new V[] {v_1884, R.Get(v_2021, (V)"floor_id"), v_2281, v_2282, V.Nil}, new Results((V)false))).First))))) {
R.Call(R.Get(v_74, (V)"spawn_enemy"), R.Join(new V[] {v_1884, v_2276, v_2281, v_2282}, new Results((V)(R.Get(v_1884, (V)"clock") + (V)100.0d))));
break;
}
}
}
} else if (R.Truth((V)(v_2028 == (V)"interact"))) {
R.Call(R.Get(v_52, (V)"apply"), R.Join(new V[] {v_1884, v_1889, v_2023}, new Results(v_2045)));
var values_2283 = R.Join(new V[] {}, new Results(R.Get(v_1889, (V)"node_id")));
var obj_2284 = v_2030;
V key_2285 = (V)"node_id";
R.Set(obj_2284, key_2285, values_2283[0]);

var values_2286 = R.Join(new V[] {}, new Results(R.Get(v_1885, (V)"choice_id")));
var obj_2287 = v_2030;
V key_2288 = (V)"choice_id";
R.Set(obj_2287, key_2288, values_2286[0]);
} else if (R.Truth((V)(v_2028 == (V)"use_utility"))) {
var values_2289 = R.Join(new V[] {}, new Results(R.Get(v_1889, (V)"utility_kind")));
var obj_2290 = v_2030;
V key_2291 = (V)"utility_kind";
R.Set(obj_2290, key_2291, values_2289[0]);
} else if (R.Truth((V)(v_2028 == (V)"claim_chest"))) {
var values_2296 = R.Join(new V[] {}, R.Call(R.Get(v_36, (V)"claim"), R.Join(new V[] {v_1884, v_1890, R.Get(v_1885, (V)"offer_index")}, new Results(new V((RuleFunction)(args_2292 => {
V v_2293 = args_2292[0];
V v_2294 = args_2292[1];
V v_2295 = args_2292[2];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2293, v_2294, v_2295}, new Results(v_2023)));
return Results.Empty;
}))))));
V v_2297 = values_2296[0];
var values_2298 = R.Join(new V[] {R.Get(v_1890, (V)"id"), R.Get(v_2297, (V)"boon_id")}, new Results(R.Get(v_2297, (V)"rarity")));
var obj_2299 = v_2030;
V key_2300 = (V)"chest_id";
var obj_2301 = v_2030;
V key_2302 = (V)"boon_id";
var obj_2303 = v_2030;
V key_2304 = (V)"rarity";
R.Set(obj_2299, key_2300, values_2298[0]);
R.Set(obj_2301, key_2302, values_2298[1]);
R.Set(obj_2303, key_2304, values_2298[2]);
} else {
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, (V)"Waited", R.Table(new Entry((V)"floor_id", R.Get(v_2021, (V)"floor_id")), new Entry((V)"x", R.Get(v_2021, (V)"x")), new Entry((V)"y", R.Get(v_2021, (V)"y"))), R.Get(v_2019, (V)"id")}, new Results(v_2023)));
}
if (R.Truth((V)(v_2028 == (V)"dash"))) {
R.Call(R.Get(v_22, (V)"ground"), R.Join(new V[] {v_2019}, new Results(new V((RuleFunction)(args_2305 => {
V v_2306 = args_2305[0];
V v_2307 = args_2305[1];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2306, v_2307, R.Get(v_2019, (V)"id")}, new Results(v_2023)));
return Results.Empty;
})))));
}
var values_2308 = R.Join(new V[] {}, new Results(v_2030));
var obj_2309 = v_1884;
V key_2310 = (V)"last_action";
R.Set(obj_2309, key_2310, values_2308[0]);
R.Call(v_1142, R.Join(new V[] {v_1884}, new Results(v_2014)));
R.Call(R.Get(v_40, (V)"after_player_action"), R.Join(new V[] {v_1884}, new Results(new V((RuleFunction)(args_2311 => {
V v_2312 = args_2311[0];
V v_2313 = args_2311[1];
V v_2314 = args_2311[2];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2312, v_2313, v_2314}, new Results(v_2023)));
return Results.Empty;
})))));
R.Call(v_1142, R.Join(new V[] {v_1884}, new Results(v_2014)));
var values_2315 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_2019, (V)"snare_delay"), () => (V)0.0d)));
V v_2316 = values_2315[0];

var values_2317 = R.Join(new V[] {}, new Results(V.Nil));
var obj_2318 = v_2019;
V key_2319 = (V)"snare_delay";
R.Set(obj_2318, key_2319, values_2317[0]);
var values_2320 = R.Join(new V[] {}, new Results((V)(R.Get(v_2030, (V)"cost") + v_2316)));
var obj_2321 = v_2030;
V key_2322 = (V)"cost";
R.Set(obj_2321, key_2322, values_2320[0]);
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_1884}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)((V)(R.Get(v_1884, (V)"clock") + R.Get(v_1889, (V)"cost")) + v_2316)), new Entry((V)"actor_id", R.Get(v_2019, (V)"id")), new Entry((V)"is_player", (V)true)))));
R.Call(v_1739, R.Join(new V[] {v_1884}, new Results(v_2014)));
R.Call(R.Get(v_50, (V)"after_time"), R.Join(new V[] {v_1884}, new Results(new V((RuleFunction)(args_2323 => {
V v_2324 = args_2323[0];
V v_2325 = args_2323[1];
V v_2326 = args_2323[2];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2324, v_2325, v_2326}, new Results(v_2023)));
return Results.Empty;
})))));
R.Call(R.Get(v_30, (V)"finish"), R.Join(new V[] {v_1884}, new Results(new V((RuleFunction)(args_2327 => {
V v_2328 = args_2327[0];
V v_2329 = args_2327[1];
V v_2330 = args_2327[2];
R.Call(v_95, R.Join(new V[] {v_1884, v_2014, v_2328, v_2329, v_2330}, new Results(v_2023)));
return Results.Empty;
})))));
R.Call(v_1142, R.Join(new V[] {v_1884}, new Results(v_2014)));
if (R.Truth((V)(R.Or(R.Get(v_2019, (V)"pickup_ward_actions"), () => (V)0.0d) > (V)0.0d))) {
var values_2331 = R.Join(new V[] {}, new Results((V)(R.Get(v_2019, (V)"pickup_ward_actions") - (V)1.0d)));
var obj_2332 = v_2019;
V key_2333 = (V)"pickup_ward_actions";
R.Set(obj_2332, key_2333, values_2331[0]);
}
R.Call(R.Get(v_52, (V)"after_action"), R.Join(new V[] {v_1884}, new Results(v_2045)));
R.Call(R.Get(v_46, (V)"after_action"), R.Join(new V[] {v_1884, v_2023}, new Results(v_2045)));
R.Call(v_1142, R.Join(new V[] {v_1884}, new Results(v_2014)));
R.Call(R.Get(v_70, (V)"recompute"), R.Join(new V[] {}, new Results(v_1884)));
R.Call(R.Get(v_4, (V)"prune_histories"), R.Join(new V[] {}, new Results(v_1884)));
R.Call(R.Get(v_10, (V)"prune"), R.Join(new V[] {}, new Results(v_1884)));
if (R.Truth(R.Get(v_1884, (V)"class_special"))) {
var values_2334 = R.Join(new V[] {}, new Results(V.Nil));
var obj_2335 = R.Get(v_1884, (V)"class_special");
V key_2336 = (V)"duelist_guard";
R.Set(obj_2335, key_2336, values_2334[0]);
}
var values_2337 = R.Join(new V[] {}, new Results(V.Nil));
var obj_2338 = v_83;
V key_2339 = v_2014;
R.Set(obj_2338, key_2339, values_2337[0]);
R.Call(rt.Global("_roag_finish_action"), R.Join(new V[] {}, new Results(v_1884)));
return R.Join(new V[] {(V)true, V.Nil}, new Results(v_2014));
return Results.Empty;
})));
R.Set(v_74, (V)"stun", new V((RuleFunction)(args_2340 => {
V v_2341 = args_2340[0];
V v_2342 = args_2340[1];
V v_2343 = args_2340[2];
V v_2344 = args_2340[3];
V v_2345 = args_2340[4];
if (R.Truth(R.Get(v_2341, (V)"meta"))) {
var values_2346 = R.Join(new V[] {}, new Results((V)false));
var obj_2347 = R.Get(v_2341, (V)"meta");
V key_2348 = (V)"eligible";
R.Set(obj_2347, key_2348, values_2346[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_2343))).First == (V)"number"), () => (V)((V)(v_2343 % (V)1.0d) == (V)0.0d)), () => (V)(v_2343 > (V)0.0d))}, new Results((V)"stun duration must be positive integer time")));
var values_2349 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_2341}, new Results(v_2342))));
V v_2350 = values_2349[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_2350)), () => (V)(R.Get(v_2350, (V)"controller") != (V)"ai")), () => (V)(R.Get(v_2350, (V)"hp") <= (V)0.0d)))) {
return R.Join(new V[] {(V)false, (V)"invalid stun target"}, new Results(R.Table()));
}
if (R.Truth(R.Get(v_2350, (V)"invulnerable"))) {
return R.Join(new V[] {(V)false, (V)"immune"}, new Results(R.Table()));
}
var values_2351 = R.Join(new V[] {}, new Results(R.Table()));
V v_2352 = values_2351[0];
R.Call(v_632, R.Join(new V[] {v_2341, v_2350, v_2352, (V)"stun", v_2344}, new Results(v_2345)));
var values_2353 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Or(R.Get(v_2350, (V)"stunned_until"), () => (V)0.0d)}, new Results((V)(R.Get(v_2341, (V)"clock") + v_2343)))));
var obj_2354 = v_2350;
V key_2355 = (V)"stunned_until";
R.Set(obj_2354, key_2355, values_2353[0]);
R.Call(R.Get(v_38, (V)"remove_for_actor"), R.Join(new V[] {v_2341, R.Get(v_2350, (V)"id")}, new Results((V)"actor_ready")));
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_2341}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", R.Get(v_2350, (V)"stunned_until")), new Entry((V)"actor_id", R.Get(v_2350, (V)"id")), new Entry((V)"is_player", (V)false)))));
R.Call(v_95, R.Join(new V[] {v_2341, v_2352, (V)"Stunned", R.Table(new Entry((V)"target_id", R.Get(v_2350, (V)"id")), new Entry((V)"until_time", R.Get(v_2350, (V)"stunned_until"))), v_2344}, new Results(v_2345)));
return R.Join(new V[] {(V)true, V.Nil}, new Results(v_2352));
return Results.Empty;
})));
R.Set(v_74, (V)"displace", new V((RuleFunction)(args_2356 => {
V v_2357 = args_2356[0];
V v_2358 = args_2356[1];
V v_2359 = args_2356[2];
V v_2360 = args_2356[3];
V v_2361 = args_2356[4];
V v_2362 = args_2356[5];
if (R.Truth(R.Get(v_2357, (V)"meta"))) {
var values_2363 = R.Join(new V[] {}, new Results((V)false));
var obj_2364 = R.Get(v_2357, (V)"meta");
V key_2365 = (V)"eligible";
R.Set(obj_2364, key_2365, values_2363[0]);
}
var values_2366 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_2357}, new Results(v_2358))));
V v_2367 = values_2366[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_2367)), () => (V)(R.Get(v_2367, (V)"controller") != (V)"ai")), () => (V)(R.Get(v_2367, (V)"hp") <= (V)0.0d)))) {
return R.Join(new V[] {(V)false, (V)"invalid displacement target"}, new Results(R.Table()));
}
if (R.Truth(R.Get(v_2367, (V)"invulnerable"))) {
return R.Join(new V[] {(V)false, (V)"immune"}, new Results(R.Table()));
}
if (R.Truth(R.And((V)(R.Get(R.Get(v_2367, (V)"position"), (V)"x") == v_2359), () => (V)(R.Get(R.Get(v_2367, (V)"position"), (V)"y") == v_2360)))) {
return R.Join(new V[] {(V)false, (V)"same cell"}, new Results(R.Table()));
}
var values_2368 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"map_for"), R.Join(new V[] {}, new Results(v_2357))));
V v_2369 = values_2368[0];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_2369, v_2359}, new Results(v_2360))).First)))) {
return R.Join(new V[] {(V)false, (V)"wall"}, new Results(R.Table()));
}
if (R.Truth(R.Call(v_144, R.Join(new V[] {v_2357, R.Get(R.Get(v_2367, (V)"position"), (V)"floor_id"), v_2359, v_2360, R.Get(v_2367, (V)"id")}, new Results((V)false))).First)) {
return R.Join(new V[] {(V)false, (V)"occupied"}, new Results(R.Table()));
}
var values_2370 = R.Join(new V[] {}, new Results(R.Table()));
V v_2371 = values_2370[0];
var values_2372 = R.Join(new V[] {}, R.Call(v_632, R.Join(new V[] {v_2357, v_2367, v_2371, (V)"displacement", v_2361}, new Results(v_2362))));
V v_2373 = values_2372[0];
var values_2374 = R.Join(new V[] {R.Get(R.Get(v_2367, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_2367, (V)"position"), (V)"y")));
V v_2375 = values_2374[0];
V v_2376 = values_2374[1];
var values_2377 = R.Join(new V[] {v_2359}, new Results(v_2360));
var obj_2378 = R.Get(v_2367, (V)"position");
V key_2379 = (V)"x";
var obj_2380 = R.Get(v_2367, (V)"position");
V key_2381 = (V)"y";
R.Set(obj_2378, key_2379, values_2377[0]);
R.Set(obj_2380, key_2381, values_2377[1]);
R.Call(v_95, R.Join(new V[] {v_2357, v_2371, (V)"Displaced", R.Table(new Entry((V)"target_id", R.Get(v_2367, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_2367, (V)"position"), (V)"floor_id")), new Entry((V)"from_x", v_2375), new Entry((V)"from_y", v_2376), new Entry((V)"x", v_2359), new Entry((V)"y", v_2360)), v_2361}, new Results(v_2362)));
if (R.Truth(v_2373)) {
var values_2382 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_2367, (V)"definition_id")))));
V v_2383 = values_2382[0];
R.Call(R.Get(v_38, (V)"enqueue"), R.Join(new V[] {v_2357}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"actor_id", R.Get(v_2367, (V)"id")), new Entry((V)"is_player", (V)false), new Entry((V)"due", (V)(R.Get(v_2357, (V)"clock") + R.Call(R.Get(v_74, (V)"effective_cost"), R.Join(new V[] {R.Get(v_2383, (V)"recovery")}, new Results(R.Get(v_2367, (V)"speed")))).First))))));
}
return R.Join(new V[] {(V)true, V.Nil}, new Results(v_2371));
return Results.Empty;
})));
R.Set(v_74, (V)"pending_strikes", new V((RuleFunction)(args_2384 => {
V v_2385 = args_2384[0];
var values_2386 = R.Join(new V[] {}, new Results(R.Table()));
V v_2387 = values_2386[0];
foreach (var row_2388 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_2385, (V)"idea_terrain"), () => R.Get(R.Get(v_2385, (V)"idea_terrain"), (V)"nodes")), () => R.Table()))))))) {
V v_2389 = row_2388[0];
V v_2390 = row_2388[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_2390, (V)"kind") == (V)"heat_vent"), () => (V)(R.Get(v_2390, (V)"phase") == (V)"warning")), () => (V)(R.Get(v_2390, (V)"floor_id") == R.Get(v_2385, (V)"active_floor_id"))))) {
var values_2391 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"sequence", R.Get(v_2390, (V)"schedule_id")), new Entry((V)"schedule_id", R.Get(v_2390, (V)"schedule_id")), new Entry((V)"source_id", (V)0.0d), new Entry((V)"floor_id", R.Get(v_2390, (V)"floor_id")), new Entry((V)"ability_id", (V)"core:ability/heat_vent"), new Entry((V)"due", R.Get(v_2390, (V)"due")), new Entry((V)"damage", (V)2.0d), new Entry((V)"terrain_heat", (V)true), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(v_2390, (V)"floor_id")), new Entry((V)"x", R.Get(v_2390, (V)"x")), new Entry((V)"y", R.Get(v_2390, (V)"y")))))))));
var obj_2392 = v_2387;
V key_2393 = (V)((V)R.Length(v_2387) + (V)1.0d);
R.Set(obj_2392, key_2393, values_2391[0]);
}
}
foreach (var row_2394 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_2385, (V)"idea_rooms"), () => R.Get(R.Get(v_2385, (V)"idea_rooms"), (V)"nodes")), () => R.Table()))))))) {
V v_2395 = row_2394[0];
V v_2396 = row_2394[1];
if (R.Truth(R.And(R.Get(v_2396, (V)"pending"), () => (V)(R.Get(v_2396, (V)"floor_id") == R.Get(v_2385, (V)"active_floor_id"))))) {
var values_2397 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"sequence", R.Get(R.Get(v_2396, (V)"pending"), (V)"schedule_id")), new Entry((V)"schedule_id", R.Get(R.Get(v_2396, (V)"pending"), (V)"schedule_id")), new Entry((V)"source_id", R.Get(R.Get(v_2396, (V)"pending"), (V)"owner_id")), new Entry((V)"floor_id", R.Get(v_2396, (V)"floor_id")), new Entry((V)"ability_id", (V)"core:ability/falling_fixture"), new Entry((V)"target_cells", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(R.Get(v_2396, (V)"pending"), (V)"cells")))).First), new Entry((V)"damage", (V)4.0d), new Entry((V)"due", R.Get(R.Get(v_2396, (V)"pending"), (V)"due")), new Entry((V)"fixture", (V)true))));
var obj_2398 = v_2387;
V key_2399 = (V)((V)R.Length(v_2387) + (V)1.0d);
R.Set(obj_2398, key_2399, values_2397[0]);
}
}
foreach (var row_2400 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2385, (V)"entities"))))))) {
V v_2401 = row_2400[0];
V v_2402 = row_2400[1];
if (R.Truth(R.And(R.Get(v_2402, (V)"pending_attack"), () => (V)(R.Get(R.Get(v_2402, (V)"position"), (V)"floor_id") == R.Get(v_2385, (V)"active_floor_id"))))) {
var values_2403 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_2402, (V)"pending_attack")))));
var obj_2404 = v_2387;
V key_2405 = (V)((V)R.Length(v_2387) + (V)1.0d);
R.Set(obj_2404, key_2405, values_2403[0]);
}
}
foreach (var row_2406 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2385, (V)"entities"))))))) {
V v_2407 = row_2406[0];
V v_2408 = row_2406[1];
if (R.Truth(R.And((V)(R.Get(v_2408, (V)"sentry_phase") == (V)"impact"), () => (V)(R.Get(R.Get(v_2408, (V)"position"), (V)"floor_id") == R.Get(v_2385, (V)"active_floor_id"))))) {
var values_2409 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"sequence", R.Get(v_2408, (V)"sentry_schedule_id")), new Entry((V)"schedule_id", R.Get(v_2408, (V)"sentry_schedule_id")), new Entry((V)"source_id", R.Get(v_2408, (V)"owner_id")), new Entry((V)"floor_id", R.Get(R.Get(v_2408, (V)"position"), (V)"floor_id")), new Entry((V)"source_x", R.Get(R.Get(v_2408, (V)"position"), (V)"x")), new Entry((V)"source_y", R.Get(R.Get(v_2408, (V)"position"), (V)"y")), new Entry((V)"ability_id", (V)"core:ability/tinkerer_sentry"), new Entry((V)"target_cells", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_2408, (V)"sentry_cells")))).First), new Entry((V)"damage", (V)3.0d), new Entry((V)"due", R.Get(v_2408, (V)"sentry_due")), new Entry((V)"companion", (V)true), new Entry((V)"reflection", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_2408, (V)"sentry_reflection")))).First))));
var obj_2410 = v_2387;
V key_2411 = (V)((V)R.Length(v_2387) + (V)1.0d);
R.Set(obj_2410, key_2411, values_2409[0]);
}
}
var values_2412 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2385, (V)"class_special"), (V)"storm")));
V v_2413 = values_2412[0];
if (R.Truth(R.And(v_2413, () => (V)(R.Get(v_2413, (V)"floor_id") == R.Get(v_2385, (V)"active_floor_id"))))) {
var values_2414 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"sequence", R.Get(v_2413, (V)"schedule_id")), new Entry((V)"schedule_id", R.Get(v_2413, (V)"schedule_id")), new Entry((V)"source_id", R.Get(v_2413, (V)"owner_id")), new Entry((V)"floor_id", R.Get(v_2413, (V)"floor_id")), new Entry((V)"ability_id", R.Get(R.Get(v_22, (V)"definition"), (V)"special_id")), new Entry((V)"target_cells", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(v_2413, (V)"cells")))).First), new Entry((V)"damage", R.Get(v_2413, (V)"damage")), new Entry((V)"due", R.Get(v_2413, (V)"due")), new Entry((V)"companion", (V)true))));
var obj_2415 = v_2387;
V key_2416 = (V)((V)R.Length(v_2387) + (V)1.0d);
R.Set(obj_2415, key_2416, values_2414[0]);
}
foreach (var row_2417 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_2385, (V)"idea_embers"), () => R.Table()))))))) {
V v_2418 = row_2417[0];
V v_2419 = row_2417[1];
if (R.Truth((V)(R.Get(v_2419, (V)"floor_id") == R.Get(v_2385, (V)"active_floor_id")))) {
var values_2420 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schedule_id", R.Get(v_2419, (V)"schedule_id")), new Entry((V)"sequence", R.Get(v_2419, (V)"schedule_id")), new Entry((V)"action_id", R.Get(v_2419, (V)"action_id")), new Entry((V)"source_id", R.Get(v_2419, (V)"source_id")), new Entry((V)"floor_id", R.Get(v_2419, (V)"floor_id")), new Entry((V)"source_x", R.Get(v_2419, (V)"x")), new Entry((V)"source_y", R.Get(v_2419, (V)"y")), new Entry((V)"ability_id", (V)"core:ability/moth_ember"), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(v_2419, (V)"floor_id")), new Entry((V)"x", R.Get(v_2419, (V)"x")), new Entry((V)"y", R.Get(v_2419, (V)"y")))))), new Entry((V)"damage", (V)1.0d), new Entry((V)"due", R.Get(v_2419, (V)"next_due")), new Entry((V)"ember_pulse", (V)true), new Entry((V)"committed_time", (V)(R.Get(v_2419, (V)"next_due") - R.Get(v_64, (V)"interval"))))));
var obj_2421 = v_2387;
V key_2422 = (V)((V)R.Length(v_2387) + (V)1.0d);
R.Set(obj_2421, key_2422, values_2420[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_2387}, new Results(new V((RuleFunction)(args_2423 => {
V v_2424 = args_2423[0];
V v_2425 = args_2423[1];
if (R.Truth((V)(R.Get(v_2424, (V)"due") != R.Get(v_2425, (V)"due")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_2424, (V)"due") < R.Get(v_2425, (V)"due"))));
}
return R.Join(new V[] {}, new Results((V)(R.Get(v_2424, (V)"sequence") < R.Get(v_2425, (V)"sequence"))));
return Results.Empty;
})))));
return R.Join(new V[] {}, new Results(v_2387));
return Results.Empty;
})));
R.Set(v_74, (V)"pending_boon_effects", new V((RuleFunction)(args_2426 => {
V v_2427 = args_2426[0];
var values_2428 = R.Join(new V[] {}, new Results(R.Table()));
V v_2429 = values_2428[0];
foreach (var row_2430 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2427, (V)"schedule"))))))) {
V v_2431 = row_2430[0];
V v_2432 = row_2430[1];
var values_2433 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2427, (V)"delayed_effects"), R.Get(v_2432, (V)"id"))));
V v_2434 = values_2433[0];
if (R.Truth(v_2434)) {
var values_2435 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_2432, (V)"id")), new Entry((V)"due", R.Get(v_2432, (V)"due")), new Entry((V)"boon_id", R.Get(R.Get(v_2434, (V)"activation"), (V)"boon_id")), new Entry((V)"target_cells", R.Call(v_84, R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_2434, (V)"activation"), (V)"event"), (V)"target_cells")))).First))));
var obj_2436 = v_2429;
V key_2437 = (V)((V)R.Length(v_2429) + (V)1.0d);
R.Set(obj_2436, key_2437, values_2435[0]);
}
}
return R.Join(new V[] {}, new Results(v_2429));
return Results.Empty;
})));
R.Set(v_74, (V)"is_visible", new V((RuleFunction)(args_2438 => {
V v_2439 = args_2438[0];
V v_2440 = args_2438[1];
V v_2441 = args_2438[2];
V v_2442 = args_2438[3];
return R.Join(new V[] {}, R.Call(R.Get(v_70, (V)"is_visible"), R.Join(new V[] {v_2439, v_2440, v_2441}, new Results(v_2442))));
return Results.Empty;
})));
R.Set(v_74, (V)"was_explored", new V((RuleFunction)(args_2443 => {
V v_2444 = args_2443[0];
V v_2445 = args_2443[1];
V v_2446 = args_2443[2];
V v_2447 = args_2443[3];
return R.Join(new V[] {}, R.Call(R.Get(v_70, (V)"was_explored"), R.Join(new V[] {v_2444, v_2445, v_2446}, new Results(v_2447))));
return Results.Empty;
})));
R.Set(v_74, (V)"snapshot", new V((RuleFunction)(args_2448 => {
V v_2449 = args_2448[0];
return R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_2449))));
return Results.Empty;
})));
R.Set(v_74, (V)"restore", new V((RuleFunction)(args_2450 => {
V v_2451 = args_2450[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_2451))).First == (V)"table"), () => (V)(R.Get(v_2451, (V)"schema") == v_76))}, new Results((V)"incompatible run schema")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_2451, (V)"content_version") == R.Get(v_12, (V)"version"))}, new Results((V)"incompatible content version")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_2451, (V)"boon_content_version") == R.Get(v_6, (V)"version"))}, new Results((V)"incompatible boon content version")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_2451, (V)"world_content_version") == R.Get(v_72, (V)"version"))}, new Results((V)"incompatible exploration content version")));
R.Call(R.Get(v_12, (V)"validate_all"), Results.Empty);
R.Call(R.Get(v_6, (V)"validate"), Results.Empty);
R.Call(R.Get(v_72, (V)"validate"), Results.Empty);
var values_2452 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Get(v_2451, (V)"generated_map"), () => R.Get(v_2451, (V)"final_map")), () => R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"get_map"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"map_id")))).First}, new Results((V)"unknown map content ID"))).First)));
V v_2453 = values_2452[0];
var values_2454 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(v_2453, (V)"floor_id"), v_2453))));
V v_2455 = values_2454[0];
if (R.Truth(R.Get(v_2451, (V)"generated_map"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(R.Get(v_2451, (V)"generated_map"), (V)"studio_pack_fingerprint") == R.Call(R.Get(v_28, (V)"studio_pack_fingerprint"), Results.Empty).First)}, new Results((V)"incompatible published world pack")));
var values_2456 = R.Join(new V[] {}, R.Call(R.Get(v_26, (V)"get"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_2453, (V)"biome_id"), () => R.Get(R.Get(v_26, (V)"order"), (V)1.0d))))));
V v_2457 = values_2456[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(v_2457, () => (V)(R.Get(v_2451, (V)"map_id") == R.Get(v_2457, (V)"map_id"))), () => (V)(R.Get(v_2451, (V)"run_seed") == R.Get(v_2453, (V)"seed")))}, new Results((V)"generated floor identity mismatch")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_2451, (V)"room_content_version") == R.Get(v_28, (V)"room_content_version")), () => (V)(R.Get(v_2451, (V)"room_content_version") == (V)"roag-rooms/2"))}, new Results((V)"incompatible room content version")));
if (R.Truth(R.Get(v_2451, (V)"floor_order"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"floor_order")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_2451, (V)"floor_order")) == (V)2.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"inactive_floor_maps")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"floor_pause")))).First == (V)"table")), () => R.Or((V)(R.Get(v_2451, (V)"active_floor_index") == (V)1.0d), () => (V)(R.Get(v_2451, (V)"active_floor_index") == (V)2.0d))), () => (V)(R.Get(R.Get(v_2451, (V)"floor_order"), R.Get(v_2451, (V)"active_floor_index")) == R.Get(v_2453, (V)"floor_id")))}, new Results((V)"invalid connected-floor index")));
var values_2458 = R.Join(new V[] {}, new Results(R.Table()));
V v_2459 = values_2458[0];
var values_2460 = R.Join(new V[] {}, new Results((V)0.0d));
V v_2461 = values_2460[0];
foreach (var row_2462 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"inactive_floor_maps"))))))) {
V v_2463 = row_2462[0];
var values_2464 = R.Join(new V[] {}, new Results((V)(v_2461 + (V)1.0d)));
v_2461 = values_2464[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_2461 == (V)1.0d)}, new Results((V)"connected stage has extra inactive maps")));
foreach (var row_2465 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"floor_order"))))))) {
V v_2466 = row_2465[0];
V v_2467 = row_2465[1];
var values_2468 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_2466 == R.Get(v_2451, (V)"active_floor_index")), () => v_2453), () => R.Get(R.Get(v_2451, (V)"inactive_floor_maps"), v_2467))));
V v_2469 = values_2468[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(v_2469, () => (V)(R.Get(v_2469, (V)"floor_id") == v_2467)), () => (V)(R.Get(v_2469, (V)"id") == R.Get(v_2453, (V)"id"))), () => (V)(R.Get(v_2469, (V)"biome_id") == R.Get(v_2453, (V)"biome_id"))), () => (V)(R.Get(v_2469, (V)"seed") == R.Get(v_2451, (V)"run_seed"))), () => (V)(R.Get(v_2469, (V)"studio_pack_fingerprint") == R.Call(R.Get(v_28, (V)"studio_pack_fingerprint"), Results.Empty).First))}, new Results((V)"invalid connected-floor identity")));
var values_2470 = R.Join(new V[] {}, new Results(v_2469));
var obj_2471 = v_2459;
V key_2472 = v_2466;
R.Set(obj_2471, key_2472, values_2470[0]);
var values_2473 = R.Join(new V[] {}, new Results(v_2469));
var obj_2474 = v_2455;
V key_2475 = v_2467;
R.Set(obj_2474, key_2475, values_2473[0]);
}
var values_2476 = R.Join(new V[] {}, R.Call(R.Get(v_28, (V)"validate_pair"), R.Join(new V[] {v_2459}, new Results(R.Get(v_2451, (V)"secrets")))));
V v_2477 = values_2476[0];
V v_2478 = values_2476[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_2477}, new Results(R.Concat((V)"invalid saved connected floors: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_2478))).First))));
} else {
var values_2479 = R.Join(new V[] {}, R.Call(R.Get(v_28, (V)"validate"), R.Join(new V[] {v_2453}, new Results(R.Get(v_2451, (V)"secrets")))));
V v_2480 = values_2479[0];
V v_2481 = values_2479[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_2480}, new Results(R.Concat((V)"invalid saved generated floor: ", R.Or(R.And(R.Get(v_2481, (V)1.0d), () => R.Get(R.Get(v_2481, (V)1.0d), (V)"kind")), () => (V)"unknown")))));
}
}
if (R.Truth(R.Get(v_2451, (V)"final_map"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_2451, (V)"map_id") == R.Get(R.Get(v_30, (V)"map"), (V)"id")), () => (V)(R.Get(v_2453, (V)"id") == R.Get(R.Get(v_30, (V)"map"), (V)"id"))), () => (V)(R.Get(v_2453, (V)"floor_id") == R.Get(R.Get(v_30, (V)"map"), (V)"floor_id")))}, new Results((V)"invalid saved final chamber identity")));
R.Call(R.Get(v_12, (V)"validate_map"), R.Join(new V[] {}, new Results(v_2453)));
R.Call(R.Get(v_30, (V)"validate_map"), R.Join(new V[] {}, new Results(v_2453)));
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_2451, (V)"active_floor_id") == R.Get(v_2453, (V)"floor_id"))}, new Results((V)"incompatible floor ID")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"clock")))).First == (V)"number"), () => (V)((V)(R.Get(v_2451, (V)"clock") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2451, (V)"clock") >= (V)0.0d))}, new Results((V)"invalid world clock")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"entities")))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"schedule")))).First == (V)"table"))}, new Results((V)"invalid run state")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"rng")))).First == (V)"table")}, new Results((V)"missing RNG state")));
R.Call(R.Get(v_10, (V)"validate_pool"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"available_boons"))));
R.Call(R.Get(v_10, (V)"validate"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"meta"))));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"chain_history")))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"delayed_effects")))).First == (V)"table"))}, new Results((V)"missing boon state")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"progression")))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2451, (V)"progression"), (V)"level")))).First == (V)"number")), () => (V)((V)(R.Get(R.Get(v_2451, (V)"progression"), (V)"level") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(R.Get(v_2451, (V)"progression"), (V)"level") >= (V)1.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2451, (V)"progression"), (V)"xp_total")))).First == (V)"number")), () => (V)((V)(R.Get(R.Get(v_2451, (V)"progression"), (V)"xp_total") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(R.Get(v_2451, (V)"progression"), (V)"xp_total") >= (V)0.0d)), () => (V)(R.Get(R.Get(v_2451, (V)"progression"), (V)"xp_total") < R.Call(R.Get(v_36, (V)"next_threshold"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2451, (V)"progression"), (V)"level")))).First))}, new Results((V)"invalid progression state")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"currency")))).First == (V)"number"), () => (V)((V)(R.Get(v_2451, (V)"currency") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2451, (V)"currency") >= (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"drops")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"rewarded_kills")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"next_drop_id")))).First == (V)"number"))}, new Results((V)"invalid reward state")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"explored")))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"secrets")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2451, (V)"visible")))).First == (V)"table"))}, new Results((V)"missing exploration state")));
var values_2482 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {}, new Results(v_2451))));
V v_2483 = values_2482[0];
var values_2484 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_2483, (V)"class_special"), () => R.Table())));
var obj_2485 = v_2483;
V key_2486 = (V)"class_special";
R.Set(obj_2485, key_2486, values_2484[0]);
var values_2487 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_2483, (V)"review_features"), () => R.Table())));
var obj_2488 = v_2483;
V key_2489 = (V)"review_features";
R.Set(obj_2488, key_2489, values_2487[0]);
var values_2490 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_2483, (V)"review_keys"), () => R.Table())));
var obj_2491 = v_2483;
V key_2492 = (V)"review_keys";
R.Set(obj_2491, key_2492, values_2490[0]);
var values_2493 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_2483, (V)"review_clues"), () => R.Table())));
var obj_2494 = v_2483;
V key_2495 = (V)"review_clues";
R.Set(obj_2494, key_2495, values_2493[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"root_seed")))).First == (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"retired_stages")))).First == (V)"table"))}, new Results((V)"missing run stage identity")));
var values_2496 = R.Join(new V[] {}, new Results(R.Table()));
V v_2497 = values_2496[0];
foreach (var row_2498 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"schedule"))))))) {
V v_2499 = row_2498[0];
V v_2500 = row_2498[1];
var values_2501 = R.Join(new V[] {}, new Results(v_2500));
var obj_2502 = v_2497;
V key_2503 = (V)((V)R.Length(v_2497) + (V)1.0d);
R.Set(obj_2502, key_2503, values_2501[0]);
}
if (R.Truth(R.Get(v_2483, (V)"floor_order"))) {
var values_2504 = R.Join(new V[] {}, new Results((V)0.0d));
V v_2505 = values_2504[0];
foreach (var row_2506 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"floor_pause"))))))) {
V v_2507 = row_2506[0];
V v_2508 = row_2506[1];
var values_2509 = R.Join(new V[] {}, new Results((V)(v_2505 + (V)1.0d)));
v_2505 = values_2509[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(v_2507 != R.Get(v_2483, (V)"active_floor_id")), () => R.Get(v_2455, v_2507)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_2508))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2508, (V)"suspended_at")))).First == (V)"number")), () => (V)(R.Get(v_2508, (V)"suspended_at") <= R.Get(v_2483, (V)"clock"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2508, (V)"schedule")))).First == (V)"table"))}, new Results((V)"invalid paused floor")));
foreach (var row_2510 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2508, (V)"schedule"))))))) {
V v_2511 = row_2510[0];
V v_2512 = row_2510[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_2512, (V)"floor_id") == v_2507), () => (V)(R.Get(v_2512, (V)"due") >= R.Get(v_2508, (V)"suspended_at")))}, new Results((V)"invalid paused floor work")));
var values_2513 = R.Join(new V[] {}, new Results(v_2512));
var obj_2514 = v_2497;
V key_2515 = (V)((V)R.Length(v_2497) + (V)1.0d);
R.Set(obj_2514, key_2515, values_2513[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_2505 == (V)1.0d)}, new Results((V)"connected stage must have one paused floor")));
var values_2516 = R.Join(new V[] {}, new Results(R.Table()));
V v_2517 = values_2516[0];
foreach (var row_2518 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_2497)))))) {
V v_2519 = row_2518[0];
V v_2520 = row_2518[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2520, (V)"id")))).First == (V)"number"), () => (V)((V)(R.Get(v_2520, (V)"id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2520, (V)"id") >= (V)1.0d)), () => (V)(R.Get(v_2520, (V)"id") < R.Get(v_2483, (V)"next_schedule_id"))), () => (V)(!R.Truth(R.Get(v_2517, R.Get(v_2520, (V)"id")))))}, new Results((V)"duplicate connected-floor schedule ID")));
var values_2521 = R.Join(new V[] {}, new Results((V)true));
var obj_2522 = v_2517;
V key_2523 = R.Get(v_2520, (V)"id");
R.Set(obj_2522, key_2523, values_2521[0]);
if (R.Truth(R.Or((V)(R.Get(v_2520, (V)"kind") == (V)"stage_pressure"), () => (V)(R.Get(v_2520, (V)"kind") == (V)"stage_deadline")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_2520, (V)"floor_id") == V.Nil)}, new Results((V)"stage timer attached to a floor")));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_2455, R.Get(v_2520, (V)"floor_id"))}, new Results((V)"scheduled work has no floor")));
}
}
foreach (var row_2524 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"schedule"))))))) {
V v_2525 = row_2524[0];
V v_2526 = row_2524[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_2526, (V)"floor_id") == V.Nil), () => (V)(R.Get(v_2526, (V)"floor_id") == R.Get(v_2483, (V)"active_floor_id")))}, new Results((V)"inactive floor work is running")));
}
}
R.Call(R.Get(v_40, (V)"validate"), R.Join(new V[] {}, new Results(v_2483)));
R.Call(R.Get(v_30, (V)"validate"), R.Join(new V[] {}, new Results(v_2483)));
var values_2527 = R.Join(new V[] {}, new Results(R.Table()));
V v_2528 = values_2527[0];
foreach (var row_2529 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"retired_stages"))))))) {
V v_2530 = row_2529[0];
V v_2531 = row_2529[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_2531, (V)"schema") == v_76), () => R.Get(v_2531, (V)"stage")), () => (V)(R.Get(R.Get(v_2531, (V)"stage"), (V)"phase") == (V)"transition")), () => R.Get(v_2531, (V)"generated_map")), () => (V)(R.Get(v_2531, (V)"active_floor_id") == R.Get(R.Get(v_2531, (V)"generated_map"), (V)"floor_id"))), () => (V)(R.Get(v_2531, (V)"active_floor_id") != R.Get(v_2483, (V)"active_floor_id"))), () => (V)(!R.Truth(R.Get(v_2528, R.Get(v_2531, (V)"active_floor_id"))))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2531, (V)"schedule")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2531, (V)"clock")))).First == (V)"number"))}, new Results((V)"invalid frozen stage")));
var values_2532 = R.Join(new V[] {}, new Results((V)true));
var obj_2533 = v_2528;
V key_2534 = R.Get(v_2531, (V)"active_floor_id");
R.Set(obj_2533, key_2534, values_2532[0]);
}
R.Call(R.Get(v_70, (V)"recompute"), R.Join(new V[] {}, new Results(v_2483)));
R.Call(R.Get(v_34, (V)"new"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2483, (V)"rng"), (V)"state"))));
var values_2535 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"player"), R.Join(new V[] {}, new Results(v_2483))));
V v_2536 = values_2535[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(R.Get(v_2536, (V)"position"), (V)"floor_id") == R.Get(v_2453, (V)"floor_id"))}, new Results((V)"player on wrong floor")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_2453, R.Get(R.Get(v_2536, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_2536, (V)"position"), (V)"y")))).First}, new Results((V)"player outside walkable map")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2536, (V)"crit_chance_ppm")))).First == (V)"number"), () => (V)((V)(R.Get(v_2536, (V)"crit_chance_ppm") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2536, (V)"crit_chance_ppm") >= (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2536, (V)"crit_damage_percent")))).First == (V)"number")), () => (V)((V)(R.Get(v_2536, (V)"crit_damage_percent") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2536, (V)"crit_damage_percent") >= (V)100.0d))}, new Results((V)"invalid player crit stats")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2536, (V)"hp")))).First == (V)"number"), () => (V)(R.Get(v_2536, (V)"hp") >= (V)0.0d)), () => (V)(R.Get(v_2536, (V)"hp") <= R.Get(v_2536, (V)"max_hp")))}, new Results((V)"invalid player health")));
R.Call(R.Get(v_54, (V)"validate"), R.Join(new V[] {v_2483}, new Results(v_2455)));
R.Call(R.Get(v_16, (V)"validate"), R.Join(new V[] {}, new Results(v_2483)));
R.Call(R.Get(v_18, (V)"validate"), R.Join(new V[] {v_2483}, new Results(v_2455)));
R.Call(R.Get(v_20, (V)"validate"), R.Join(new V[] {v_2483}, new Results(v_2455)));
R.Call(R.Get(v_22, (V)"validate"), R.Join(new V[] {v_2483}, new Results(v_2455)));
R.Call(R.Get(v_24, (V)"validate"), R.Join(new V[] {v_2483, v_2455}, new Results(v_2536)));
R.Call(R.Get(v_52, (V)"validate"), R.Join(new V[] {v_2483}, new Results(v_2455)));
R.Call(R.Get(v_46, (V)"validate"), R.Join(new V[] {v_2483}, new Results(v_2455)));
R.Call(R.Get(v_56, (V)"validate"), R.Join(new V[] {v_2483, v_2455}, new Results(v_12)));
var values_2537 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_2538 = values_2537[0];
V v_2539 = values_2537[1];
foreach (var row_2540 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"entities"))))))) {
V v_2541 = row_2540[0];
V v_2542 = row_2540[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"id")))).First == (V)"number"), () => (V)((V)(R.Get(v_2542, (V)"id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2542, (V)"id") >= (V)1.0d)), () => (V)(R.Get(v_2542, (V)"id") < R.Get(v_2483, (V)"next_entity_id"))), () => (V)(!R.Truth(R.Get(v_2539, R.Get(v_2542, (V)"id")))))}, new Results((V)"invalid or duplicate entity ID")));
var values_2543 = R.Join(new V[] {}, new Results((V)true));
var obj_2544 = v_2539;
V key_2545 = R.Get(v_2542, (V)"id");
R.Set(obj_2544, key_2545, values_2543[0]);
var values_2546 = R.Join(new V[] {}, new Results(R.And(R.Get(v_2542, (V)"position"), () => R.Get(v_2455, R.Get(R.Get(v_2542, (V)"position"), (V)"floor_id")))));
V v_2547 = values_2546[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"position")))).First == (V)"table"), () => v_2547), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2542, (V)"position"), (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(R.Get(v_2542, (V)"position"), (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2542, (V)"position"), (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(R.Get(v_2542, (V)"position"), (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(R.Get(v_2542, (V)"position"), (V)"x") >= (V)1.0d)), () => (V)(R.Get(R.Get(v_2542, (V)"position"), (V)"x") <= R.Get(v_2547, (V)"width"))), () => (V)(R.Get(R.Get(v_2542, (V)"position"), (V)"y") >= (V)1.0d)), () => (V)(R.Get(R.Get(v_2542, (V)"position"), (V)"y") <= R.Get(v_2547, (V)"height")))}, new Results((V)"entity on incompatible floor")));
if (R.Truth((V)(R.Get(v_2542, (V)"controller") == (V)"ai"))) {
var values_2548 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"definition_id")))));
V v_2549 = values_2548[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_2549, () => (V)(R.Get(v_2542, (V)"ai_id") == R.Get(v_2549, (V)"ai")))}, new Results((V)"unknown enemy or AI content reference")));
} else if (R.Truth((V)(R.Get(v_2542, (V)"controller") == (V)"player"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_2542, (V)"id") == R.Get(v_2483, (V)"player_id")), () => R.Call(R.Get(v_12, (V)"get_class"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"definition_id")))).First)}, new Results((V)"invalid player content reference")));
} else if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_2542, (V)"controller") != (V)"chest"), () => (V)(R.Get(v_2542, (V)"controller") != (V)"door")), () => (V)(R.Get(v_2542, (V)"controller") != (V)"exit")), () => (V)(R.Get(v_2542, (V)"controller") != (V)"review_vault")), () => (V)(R.Get(v_2542, (V)"controller") != (V)"review_gate")), () => (V)(R.Get(v_2542, (V)"controller") != (V)"review_pillar")), () => (V)(R.Get(v_2542, (V)"controller") != R.Get(v_18, (V)"controller"))), () => (V)(R.Get(v_2542, (V)"controller") != R.Get(v_20, (V)"controller"))))) {
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"unknown entity controller")));
}
if (R.Truth((V)(R.Get(v_2542, (V)"controller") == (V)"review_vault"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"opened")))).First == (V)"boolean"), () => (V)(R.Get(v_2542, (V)"blocks_movement") == (V)(!R.Truth(R.Get(v_2542, (V)"opened")))))}, new Results((V)"invalid vault state")));
} else if (R.Truth((V)(R.Get(v_2542, (V)"controller") == (V)"review_gate"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"open")))).First == (V)"boolean"), () => (V)(R.Get(v_2542, (V)"blocks_movement") == (V)(!R.Truth(R.Get(v_2542, (V)"open"))))), () => (V)(R.Get(v_2542, (V)"blocks_vision") == (V)(!R.Truth(R.Get(v_2542, (V)"open"))))), () => (V)(R.Get(v_2542, (V)"blocks_projectiles") == (V)(!R.Truth(R.Get(v_2542, (V)"open")))))}, new Results((V)"invalid timed gate state")));
} else if (R.Truth((V)(R.Get(v_2542, (V)"controller") == (V)"review_pillar"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Get(v_2542, (V)"blocks_movement"), () => R.Get(v_2542, (V)"blocks_vision")), () => R.Get(v_2542, (V)"blocks_projectiles"))}, new Results((V)"invalid pillar state")));
}
if (R.Truth(R.Or((V)(R.Get(v_2542, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_2542, (V)"controller") == (V)"player")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"hp")))).First == (V)"number"), () => (V)((V)(R.Get(v_2542, (V)"hp") % (V)1.0d) == (V)0.0d)), () => R.Or((V)(R.Get(v_2542, (V)"hp") > (V)0.0d), () => R.And(R.And(R.Get(v_2483, (V)"game_over"), () => (V)(R.Get(v_2542, (V)"controller") == (V)"player")), () => (V)(R.Get(v_2542, (V)"hp") == (V)0.0d)))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"max_hp")))).First == (V)"number")), () => (V)((V)(R.Get(v_2542, (V)"max_hp") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2542, (V)"hp") <= R.Get(v_2542, (V)"max_hp"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"speed")))).First == (V)"number")), () => (V)((V)(R.Get(v_2542, (V)"speed") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2542, (V)"speed") > (V)0.0d))}, new Results((V)"invalid combatant state")));
}
R.Call(R.Get(v_4, (V)"validate_inventory"), R.Join(new V[] {v_2542}, new Results(v_2451)));
if (R.Truth((V)(R.Get(v_2542, (V)"controller") == (V)"chest"))) {
R.Call(R.Get(v_36, (V)"validate_chest"), R.Join(new V[] {}, new Results(v_2542)));
var values_2550 = R.Join(new V[] {}, new Results(R.Table()));
V v_2551 = values_2550[0];
foreach (var row_2552 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"available_boons"))))))) {
V v_2553 = row_2552[0];
V v_2554 = row_2552[1];
var values_2555 = R.Join(new V[] {}, new Results((V)true));
var obj_2556 = v_2551;
V key_2557 = v_2554;
R.Set(obj_2556, key_2557, values_2555[0]);
}
foreach (var row_2558 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"offers"))))))) {
V v_2559 = row_2558[0];
V v_2560 = row_2558[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_2551, R.Get(v_2560, (V)"boon_id"))}, new Results((V)"chest offer outside frozen run pool")));
}
}
if (R.Truth((V)(R.Get(v_2542, (V)"controller") == (V)"door"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Get(v_2542, (V)"definition_id") == (V)"core:door/ruin"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2542, (V)"open")))).First == (V)"boolean")), () => (V)(R.Get(v_2542, (V)"blocks_movement") == (V)(!R.Truth(R.Get(v_2542, (V)"open"))))), () => (V)(R.Get(v_2542, (V)"blocks_vision") == (V)(!R.Truth(R.Get(v_2542, (V)"open"))))), () => (V)(R.Get(v_2542, (V)"blocks_projectiles") == (V)(!R.Truth(R.Get(v_2542, (V)"open")))))}, new Results((V)"invalid door state")));
}
if (R.Truth((V)(R.Get(v_2542, (V)"controller") == (V)"exit"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.Get(v_2483, (V)"stage"), () => (V)(R.Get(R.Get(v_2483, (V)"stage"), (V)"exit_id") == R.Get(v_2542, (V)"id"))), () => R.Get(v_2542, (V)"blocks_movement")), () => R.Or(R.And((V)(R.Get(R.Get(v_2483, (V)"stage"), (V)"index") == (V)3.0d), () => (V)((V)R.Length(R.Get(v_2542, (V)"routes")) == (V)1.0d)), () => R.And((V)(R.Get(R.Get(v_2483, (V)"stage"), (V)"index") < (V)3.0d), () => R.Or((V)((V)R.Length(R.Get(v_2542, (V)"routes")) == (V)2.0d), () => (V)((V)R.Length(R.Get(v_2542, (V)"routes")) == (V)3.0d)))))}, new Results((V)"invalid stage exit")));
}
if (R.Truth(R.Get(v_2542, (V)"blocks_movement"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_2547, R.Get(R.Get(v_2542, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_2542, (V)"position"), (V)"y")))).First}, new Results((V)"entity outside walkable map")));
var values_2561 = R.Join(new V[] {}, new Results(R.Concat(R.Get(R.Get(v_2542, (V)"position"), (V)"floor_id"), R.Concat((V)":", R.Concat(R.Get(R.Get(v_2542, (V)"position"), (V)"x"), R.Concat((V)",", R.Get(R.Get(v_2542, (V)"position"), (V)"y")))))));
V v_2562 = values_2561[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_2538, v_2562)))}, new Results((V)"overlapping blocking entities")));
var values_2563 = R.Join(new V[] {}, new Results((V)true));
var obj_2564 = v_2538;
V key_2565 = v_2562;
R.Set(obj_2564, key_2565, values_2563[0]);
}
if (R.Truth(R.Get(v_2542, (V)"pending_attack"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_12, (V)"has_ability"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2542, (V)"pending_attack"), (V)"ability_id")))).First}, new Results((V)"unknown pending ability reference")));
var values_2566 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2542, (V)"pending_attack"), (V)"support")));
V v_2567 = values_2566[0];
if (R.Truth(v_2567)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.And(R.And(R.And(R.And((V)(R.Get(v_2567, (V)"kind") == (V)"heal"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2567, (V)"power")))).First == (V)"number")), () => (V)((V)(R.Get(v_2567, (V)"power") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2567, (V)"power") >= (V)1.0d)), () => (V)(R.Get(v_2567, (V)"power") <= (V)20.0d)), () => R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_2567, (V)"kind") == (V)"summon"), () => R.Call(R.Get(v_12, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_2567, (V)"enemy_id")))).First), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2567, (V)"max_active")))).First == (V)"number")), () => (V)((V)(R.Get(v_2567, (V)"max_active") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2567, (V)"max_active") >= (V)1.0d)), () => (V)(R.Get(v_2567, (V)"max_active") <= (V)3.0d))), () => R.And((V)(R.Get(v_2567, (V)"kind") == (V)"mirror"), () => (V)(R.Get(v_2542, (V)"definition_id") == (V)"core:enemy/mirror_hexer"))), () => R.And((V)(R.Get(v_2567, (V)"kind") == (V)"web"), () => (V)(R.Get(v_2542, (V)"definition_id") == (V)"core:enemy/web_weaver"))), () => R.And((V)(R.Get(v_2567, (V)"kind") == (V)"alarm"), () => (V)(R.Get(v_2542, (V)"definition_id") == (V)"core:enemy/alarm_keeper"))), () => R.And((V)(R.Get(v_2567, (V)"kind") == (V)"gulp"), () => (V)(R.Get(v_2542, (V)"definition_id") == (V)"core:enemy/gulp_toad"))), () => R.And((V)(R.Get(v_2567, (V)"kind") == (V)"revive"), () => (V)(R.Get(v_2542, (V)"definition_id") == (V)"core:enemy/grave_cantor"))), () => R.And((V)(R.Get(v_2567, (V)"kind") == (V)"phase"), () => (V)(R.Get(v_2542, (V)"definition_id") == (V)"core:enemy/hollow_wraith"))), () => R.And((V)(R.Get(v_2567, (V)"kind") == (V)"ward"), () => (V)(R.Get(v_2542, (V)"definition_id") == (V)"core:enemy/ward_weaver"))), () => R.And((V)(R.Get(v_2567, (V)"kind") == (V)"snare"), () => (V)(R.Get(v_2542, (V)"definition_id") == (V)"core:enemy/snare_scribe")))}, new Results((V)"invalid pending support windup")));
}
var values_2568 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2542, (V)"pending_attack"), (V)"advance")));
V v_2569 = values_2568[0];
if (R.Truth(v_2569)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2569, (V)"max_steps")))).First == (V)"number"), () => (V)((V)(R.Get(v_2569, (V)"max_steps") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2569, (V)"max_steps") >= (V)1.0d)), () => (V)(R.Get(v_2569, (V)"max_steps") <= (V)3.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2569, (V)"dx")))).First == (V)"number")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2569, (V)"dy")))).First == (V)"number")), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_2569, (V)"dx")))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_2569, (V)"dy")))).First) == (V)1.0d))}, new Results((V)"invalid pending charge path")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2542, (V)"pending_attack"), (V)"target_cells")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(R.Get(v_2542, (V)"pending_attack"), (V)"target_cells")) >= (V)1.0d))}, new Results((V)"missing pending target cells")));
foreach (var row_2570 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2542, (V)"pending_attack"), (V)"target_cells"))))))) {
V v_2571 = row_2570[0];
V v_2572 = row_2570[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_2572, (V)"floor_id") == R.Get(R.Get(v_2542, (V)"position"), (V)"floor_id")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2572, (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(v_2572, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2572, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_2572, (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2572, (V)"x") >= (V)1.0d)), () => (V)(R.Get(v_2572, (V)"x") <= R.Get(v_2547, (V)"width"))), () => (V)(R.Get(v_2572, (V)"y") >= (V)1.0d)), () => (V)(R.Get(v_2572, (V)"y") <= R.Get(v_2547, (V)"height")))}, new Results((V)"invalid pending target cell")));
}
R.Call(R.Get(v_56, (V)"validate_pending"), R.Join(new V[] {v_2542}, new Results(R.Get(v_2542, (V)"pending_attack"))));
var values_2573 = R.Join(new V[] {}, new Results((V)false));
V v_2574 = values_2573[0];
foreach (var row_2575 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_2497)))))) {
V v_2576 = row_2575[0];
V v_2577 = row_2575[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_2577, (V)"id") == R.Get(R.Get(v_2542, (V)"pending_attack"), (V)"schedule_id")), () => (V)(R.Get(v_2577, (V)"kind") == (V)"impact")), () => (V)(R.Get(v_2577, (V)"actor_id") == R.Get(v_2542, (V)"id"))), () => (V)(R.Get(v_2577, (V)"attack_id") == R.Get(R.Get(v_2542, (V)"pending_attack"), (V)"action_id"))), () => (V)(R.Get(v_2577, (V)"seq") == R.Get(R.Get(v_2542, (V)"pending_attack"), (V)"sequence"))), () => (V)(R.Get(v_2577, (V)"due") == R.Get(R.Get(v_2542, (V)"pending_attack"), (V)"due"))))) {
var values_2578 = R.Join(new V[] {}, new Results((V)true));
v_2574 = values_2578[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_2574}, new Results((V)"pending windup has no matching impact")));
}
}
R.Call(R.Get(v_50, (V)"validate"), R.Join(new V[] {v_2483}, new Results(v_2455)));
if (R.Truth(R.Get(v_2483, (V)"generated_map"))) {
var values_2579 = R.Join(new V[] {}, new Results((V)0.0d));
V v_2580 = values_2579[0];
var values_2581 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_2453))));
V v_2582 = values_2581[0];
if (R.Truth(R.Get(v_2483, (V)"floor_order"))) {
var values_2583 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_2455, R.Get(R.Get(v_2483, (V)"floor_order"), (V)1.0d))), new Entry((V)2, R.Get(v_2455, R.Get(R.Get(v_2483, (V)"floor_order"), (V)2.0d))))));
v_2582 = values_2583[0];
}
foreach (var row_2584 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_2582)))))) {
V v_2585 = row_2584[0];
V v_2586 = row_2584[1];
var values_2587 = R.Join(new V[] {}, new Results((V)(v_2580 + (V)R.Length(R.Or(R.Get(R.Get(v_2586, (V)"placements"), (V)"secrets"), () => R.Table())))));
v_2580 = values_2587[0];
foreach (var row_2588 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_2586, (V)"placements"), (V)"secrets"), () => R.Table()))))))) {
V v_2589 = row_2588[0];
V v_2590 = row_2588[1];
var values_2591 = Results.Empty;
V v_2592 = values_2591[0];
foreach (var row_2593 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"secrets"))))))) {
V v_2594 = row_2593[0];
V v_2595 = row_2593[1];
if (R.Truth(R.And((V)(R.Get(v_2595, (V)"id") == R.Get(v_2590, (V)"id")), () => (V)(R.Get(v_2595, (V)"floor_id") == R.Get(v_2586, (V)"floor_id"))))) {
var values_2596 = R.Join(new V[] {}, new Results(v_2595));
v_2592 = values_2596[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(v_2592, () => (V)(R.Get(v_2592, (V)"definition_id") == R.Get(v_2590, (V)"definition_id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2592, (V)"revealed")))).First == (V)"boolean"))}, new Results((V)"generated secret identity mismatch")));
if (R.Truth(R.And(R.Get(v_2592, (V)"revealed"), () => R.Get(v_2592, (V)"wall_x")))) {
var values_2597 = R.Join(new V[] {}, R.Call(R.Get(v_74, (V)"entity"), R.Join(new V[] {v_2483}, new Results(R.Get(v_2592, (V)"reward_id")))));
V v_2598 = values_2597[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(v_2598, () => (V)(R.Get(v_2598, (V)"controller") == (V)"chest")), () => (V)(R.Get(v_2598, (V)"stable_marker_id") == R.Concat((V)"secret:", R.Get(v_2592, (V)"id"))))}, new Results((V)"revealed wall cache has no stable reward")));
}
}
foreach (var row_2599 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_2586, (V)"placements"), (V)"doors"), () => R.Table()))))))) {
V v_2600 = row_2599[0];
V v_2601 = row_2599[1];
var values_2602 = R.Join(new V[] {}, new Results((V)false));
V v_2603 = values_2602[0];
var values_2604 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_2483, (V)"floor_order"), () => R.Concat(R.Get(v_2586, (V)"floor_id"), R.Concat((V)":", R.Get(v_2601, (V)"id")))), () => R.Get(v_2601, (V)"id"))));
V v_2605 = values_2604[0];
foreach (var row_2606 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"entities"))))))) {
V v_2607 = row_2606[0];
V v_2608 = row_2606[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_2608, (V)"controller") == (V)"door"), () => (V)(R.Get(v_2608, (V)"stable_marker_id") == v_2605)), () => (V)(R.Get(R.Get(v_2608, (V)"position"), (V)"floor_id") == R.Get(v_2586, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_2608, (V)"position"), (V)"x") == R.Get(v_2601, (V)"x"))), () => (V)(R.Get(R.Get(v_2608, (V)"position"), (V)"y") == R.Get(v_2601, (V)"y"))))) {
var values_2609 = R.Join(new V[] {}, new Results((V)true));
v_2603 = values_2609[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_2603}, new Results((V)"generated door state missing")));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_2483, (V)"secrets")) == v_2580)}, new Results((V)"generated secret state missing")));
}
var values_2610 = R.Join(new V[] {}, new Results(R.Table()));
V v_2611 = values_2610[0];
foreach (var row_2612 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"drops"))))))) {
V v_2613 = row_2612[0];
V v_2614 = row_2612[1];
var values_2615 = R.Join(new V[] {}, new Results(R.And(R.Get(v_2614, (V)"position"), () => R.Get(v_2455, R.Get(R.Get(v_2614, (V)"position"), (V)"floor_id")))));
V v_2616 = values_2615[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2614, (V)"id")))).First == (V)"number"), () => (V)((V)(R.Get(v_2614, (V)"id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2614, (V)"id") >= (V)1.0d)), () => (V)(R.Get(v_2614, (V)"id") < R.Get(v_2483, (V)"next_drop_id"))), () => (V)(!R.Truth(R.Get(v_2611, R.Get(v_2614, (V)"id"))))), () => R.Or(R.Or((V)(R.Get(v_2614, (V)"kind") == (V)"currency"), () => (V)(R.Get(v_2614, (V)"kind") == (V)"food")), () => (V)(R.Get(v_2614, (V)"kind") == (V)"health"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2614, (V)"amount")))).First == (V)"number")), () => (V)((V)(R.Get(v_2614, (V)"amount") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2614, (V)"amount") > (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2614, (V)"collected")))).First == (V)"boolean")), () => R.Or(R.Or((V)(R.Get(v_2614, (V)"kind") == (V)"currency"), () => R.And((V)(R.Get(v_2614, (V)"kind") == (V)"food"), () => (V)(R.Get(v_2614, (V)"amount") == (V)4.0d))), () => R.And((V)(R.Get(v_2614, (V)"kind") == (V)"health"), () => (V)(R.Get(v_2614, (V)"amount") == (V)10.0d)))), () => v_2616), () => R.Or(R.Call(R.Get(v_12, (V)"walkable"), R.Join(new V[] {v_2616, R.Get(R.Get(v_2614, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_2614, (V)"position"), (V)"y")))).First, () => R.And(R.Or(R.Get(v_2614, (V)"collected"), () => R.Get(v_2614, (V)"carried_by")), () => R.Call(R.Get(v_46, (V)"closed"), R.Join(new V[] {v_2483, R.Get(R.Get(v_2614, (V)"position"), (V)"floor_id"), R.Get(R.Get(v_2614, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_2614, (V)"position"), (V)"y")))).First)))}, new Results((V)"invalid reward drop")));
var values_2617 = R.Join(new V[] {}, new Results((V)true));
var obj_2618 = v_2611;
V key_2619 = R.Get(v_2614, (V)"id");
R.Set(obj_2618, key_2619, values_2617[0]);
}
foreach (var row_2620 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"delayed_effects"))))))) {
V v_2621 = row_2620[0];
V v_2622 = row_2620[1];
var values_2623 = R.Join(new V[] {}, new Results((V)false));
V v_2624 = values_2623[0];
foreach (var row_2625 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_2497)))))) {
V v_2626 = row_2625[0];
V v_2627 = row_2625[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_2627, (V)"id") == v_2621), () => (V)(R.Get(v_2627, (V)"kind") == (V)"effect")), () => (V)(R.Get(v_2627, (V)"effect_id") == v_2621)))) {
var values_2628 = R.Join(new V[] {}, new Results((V)true));
v_2624 = values_2628[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(v_2624, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2622, (V)"activation")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2622, (V)"activation"), (V)"ancestry")))).First == (V)"table"))}, new Results((V)"invalid delayed boon effect")));
}
foreach (var row_2629 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2483, (V)"entities"))))))) {
V v_2630 = row_2629[0];
V v_2631 = row_2629[1];
foreach (var row_2632 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_2631, (V)"statuses"), () => R.Table()))))))) {
V v_2633 = row_2632[0];
V v_2634 = row_2632[1];
var values_2635 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2631, (V)"position"), (V)"floor_id")));
V v_2636 = values_2635[0];
var values_2637 = R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.Get(v_2483, (V)"floor_pause"), () => R.Get(R.Get(v_2483, (V)"floor_pause"), v_2636)), () => R.Get(R.Get(R.Get(v_2483, (V)"floor_pause"), v_2636), (V)"suspended_at")), () => R.Get(v_2483, (V)"clock"))));
V v_2638 = values_2637[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_2633 == (V)"burning"), () => R.Get(v_2631, (V)"hp")), () => (V)(R.Get(v_2631, (V)"hp") > (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2634, (V)"power")))).First == (V)"number")), () => (V)(R.Get(v_2634, (V)"power") >= (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2634, (V)"remaining")))).First == (V)"number")), () => (V)((V)(R.Get(v_2634, (V)"remaining") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2634, (V)"remaining") > (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2634, (V)"interval")))).First == (V)"number")), () => (V)((V)(R.Get(v_2634, (V)"interval") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_2634, (V)"interval") > (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2634, (V)"next_due")))).First == (V)"number")), () => (V)(R.Get(v_2634, (V)"next_due") >= v_2638)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_2634, (V)"activation")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2634, (V)"activation"), (V)"ancestry")))).First == (V)"table"))}, new Results((V)"invalid active status")));
var values_2639 = R.Join(new V[] {}, new Results((V)false));
V v_2640 = values_2639[0];
foreach (var row_2641 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_2497)))))) {
V v_2642 = row_2641[0];
V v_2643 = row_2641[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_2643, (V)"id") == R.Get(v_2634, (V)"schedule_id")), () => (V)(R.Get(v_2643, (V)"kind") == (V)"effect")), () => (V)(R.Get(v_2643, (V)"actor_id") == R.Get(v_2631, (V)"id"))), () => (V)(R.Get(v_2643, (V)"due") == R.Get(v_2634, (V)"next_due"))))) {
var values_2644 = R.Join(new V[] {}, new Results((V)true));
v_2640 = values_2644[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_2640}, new Results((V)"missing scheduled status tick")));
}
}
R.Call(R.Get(v_38, (V)"sort"), R.Join(new V[] {}, new Results(v_2483)));
if (R.Truth((V)(!R.Truth(R.Get(v_2483, (V)"game_over"))))) {
var values_2645 = R.Join(new V[] {}, R.Call(R.Get(v_38, (V)"peek"), R.Join(new V[] {}, new Results(v_2483))));
V v_2646 = values_2645[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(v_2646, () => (V)(R.Get(v_2646, (V)"kind") == (V)"actor_ready")), () => (V)(R.Get(v_2646, (V)"actor_id") == R.Get(v_2483, (V)"player_id"))), () => (V)(R.Get(v_2646, (V)"due") == R.Get(v_2483, (V)"clock")))}, new Results((V)"snapshot is not at a player decision boundary")));
}
return R.Join(new V[] {}, new Results(v_2483));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_74));
return Results.Empty;
} }
