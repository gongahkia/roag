// Static C# port of src/idea_interactions.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: f4fe0d960da6f32e9e31162f85a050c6ad9b6635503079de01d1040c6c2c01eb
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_interactions { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_interaction_content"))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"definitions", v_16))));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_20 = values_19[0];
V v_21 = V.Nil;
v_21 = new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_23))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_23));
}
var values_24 = R.Join(new V[] {}, new Results(R.Table()));
V v_25 = values_24[0];

foreach (var row_26 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_23)))))) {
V v_27 = row_26[0];
V v_28 = row_26[1];
var values_29 = R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {}, new Results(v_28))));
var obj_30 = v_25;
V key_31 = v_27;
R.Set(obj_30, key_31, values_29[0]);
}

return R.Join(new V[] {}, new Results(v_25));
return Results.Empty;
}));
V v_32 = V.Nil;
v_32 = new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
V v_35 = args_33[1];
foreach (var row_36 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_34, (V)"entities"))))))) {
V v_37 = row_36[0];
V v_38 = row_36[1];
if (R.Truth((V)(R.Get(v_38, (V)"id") == v_35))) {
return R.Join(new V[] {}, new Results(v_38));
}
}
return Results.Empty;
}));
V v_39 = V.Nil;
v_39 = new V((RuleFunction)(args_40 => {
V v_41 = args_40[0];
return R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_41}, new Results(R.Get(v_41, (V)"player_id")))))));
return Results.Empty;
}));
V v_42 = V.Nil;
v_42 = new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
var values_45 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_44, (V)"idea_rooms"), () => R.Table(new Entry((V)"version", R.Get(v_16, (V)"version")), new Entry((V)"nodes", R.Table()), new Entry((V)"groups", R.Table())))));
var obj_46 = v_44;
V key_47 = (V)"idea_rooms";
R.Set(obj_46, key_47, values_45[0]);
return R.Join(new V[] {}, new Results(R.Get(v_44, (V)"idea_rooms")));
return Results.Empty;
}));
V v_48 = V.Nil;
v_48 = new V((RuleFunction)(args_49 => {
V v_50 = args_49[0];
V v_51 = args_49[1];
return R.Join(new V[] {}, new Results(R.Concat(v_50, R.Concat((V)",", v_51))));
return Results.Empty;
}));
V v_52 = V.Nil;
v_52 = new V((RuleFunction)(args_53 => {
V v_54 = args_53[0];
V v_55 = args_53[1];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_54, (V)"x") - R.Get(v_55, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_54, (V)"y") - R.Get(v_55, (V)"y"))))).First)));
return Results.Empty;
}));
R.Set(v_18, (V)"room_at", new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
V v_58 = args_56[1];
V v_59 = args_56[2];
foreach (var row_60 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_57, (V)"rooms"), () => R.Table()))))))) {
V v_61 = row_60[0];
V v_62 = row_60[1];
if (R.Truth(R.And(R.And(R.And((V)(v_58 >= R.Get(v_62, (V)"x")), () => (V)(v_58 < (V)(R.Get(v_62, (V)"x") + R.Get(v_62, (V)"width")))), () => (V)(v_59 >= R.Get(v_62, (V)"y"))), () => (V)(v_59 < (V)(R.Get(v_62, (V)"y") + R.Get(v_62, (V)"height")))))) {
return R.Join(new V[] {}, new Results(v_62));
}
}
return Results.Empty;
})));
V v_63 = V.Nil;
v_63 = new V((RuleFunction)(args_64 => {
V v_65 = args_64[0];
V v_66 = args_64[1];
V v_67 = args_64[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(v_65, () => (V)(v_66 >= R.Get(v_65, (V)"x"))), () => (V)(v_66 < (V)(R.Get(v_65, (V)"x") + R.Get(v_65, (V)"width")))), () => (V)(v_67 >= R.Get(v_65, (V)"y"))), () => (V)(v_67 < (V)(R.Get(v_65, (V)"y") + R.Get(v_65, (V)"height"))))));
return Results.Empty;
}));
V v_68 = V.Nil;
v_68 = new V((RuleFunction)(args_69 => {
V v_70 = args_69[0];
V v_71 = args_69[1];
V v_72 = args_69[2];
V v_73 = args_69[3];
foreach (var row_74 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_70, (V)"entities"))))))) {
V v_75 = row_74[0];
V v_76 = row_74[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.Get(v_76, (V)"blocks_movement"), () => (V)(R.Get(v_76, (V)"id") != R.Get(v_70, (V)"player_id"))), () => (V)(R.Get(R.Get(v_76, (V)"position"), (V)"floor_id") == v_71)), () => (V)(R.Get(R.Get(v_76, (V)"position"), (V)"x") == v_72)), () => (V)(R.Get(R.Get(v_76, (V)"position"), (V)"y") == v_73)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
R.Set(v_18, (V)"node", new V((RuleFunction)(args_77 => {
V v_78 = args_77[0];
V v_79 = args_77[1];
foreach (var row_80 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_78, (V)"idea_rooms"), () => R.Get(R.Get(v_78, (V)"idea_rooms"), (V)"nodes")), () => R.Table()))))))) {
V v_81 = row_80[0];
V v_82 = row_80[1];
if (R.Truth((V)(R.Get(v_82, (V)"id") == v_79))) {
return R.Join(new V[] {}, new Results(v_82));
}
}
return Results.Empty;
})));
V v_83 = V.Nil;
v_83 = new V((RuleFunction)(args_84 => {
V v_85 = args_84[0];
var values_86 = R.Join(new V[] {}, new Results(R.Table()));
V v_87 = values_86[0];

var values_88 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_39, R.Join(new V[] {}, new Results(v_85))).First, (V)"definition_id")));
V v_89 = values_88[0];
foreach (var row_90 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_85, (V)"available_boons"))))))) {
V v_91 = row_90[0];
V v_92 = row_90[1];
var values_93 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_92))));
V v_94 = values_93[0];

var values_95 = R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_94, (V)"eligible_classes")))));
V v_96 = values_95[0];
foreach (var row_97 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_94, (V)"eligible_classes"), () => R.Table()))))))) {
V v_98 = row_97[0];
V v_99 = row_97[1];
if (R.Truth((V)(v_99 == v_89))) {
var values_100 = R.Join(new V[] {}, new Results((V)true));
v_96 = values_100[0];
}
}
if (R.Truth(v_96)) {
var values_101 = R.Join(new V[] {}, new Results(v_92));
var obj_102 = v_87;
V key_103 = (V)((V)R.Length(v_87) + (V)1.0d);
R.Set(obj_102, key_103, values_101[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_87)));

return R.Join(new V[] {}, new Results(v_87));
return Results.Empty;
}));
R.Set(v_18, (V)"place", new V((RuleFunction)(args_104 => {
V v_105 = args_104[0];
V v_106 = args_104[1];
V v_107 = args_104[2];
V v_108 = args_104[3];
V v_109 = args_104[4];
V v_110 = args_104[5];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(R.Get(v_16, (V)"names"), v_106), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_107, v_108}, new Results(v_109))).First)}, new Results((V)"invalid interaction placement")));
var values_111 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results(v_105))));
V v_112 = values_111[0];
var values_113 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat(R.Get(v_107, (V)"floor_id"), R.Concat((V)":interaction:", R.Concat(v_106, R.Concat((V)":", R.Call(v_48, R.Join(new V[] {v_108}, new Results(v_109))).First))))), new Entry((V)"kind", v_106), new Entry((V)"floor_id", R.Get(v_107, (V)"floor_id")), new Entry((V)"x", v_108), new Entry((V)"y", v_109), new Entry((V)"spent", (V)false))));
V v_114 = values_113[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(R.Get(v_18, (V)"node"), R.Join(new V[] {v_105}, new Results(R.Get(v_114, (V)"id")))).First))}, new Results((V)"duplicate interaction")));
foreach (var row_115 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(v_110, () => R.Table()))))))) {
V v_116 = row_115[0];
V v_117 = row_115[1];
var values_118 = R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {}, new Results(v_117))));
var obj_119 = v_114;
V key_120 = v_116;
R.Set(obj_119, key_120, values_118[0]);
}
var values_121 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"derive"), R.Join(new V[] {R.Or(R.Get(v_105, (V)"root_seed"), () => R.Get(v_105, (V)"run_seed"))}, new Results(R.Get(v_114, (V)"id")))));
V v_122 = values_121[0];
if (R.Truth((V)(v_106 == (V)"offering_font"))) {
var values_123 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"make_offers"), R.Join(new V[] {v_105, R.Call(R.Get(v_2, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/standard"))).First}, R.Call(R.Get(v_10, (V)"new"), R.Join(new V[] {}, new Results(v_122))))));
var obj_124 = v_114;
V key_125 = (V)"offers";
R.Set(obj_124, key_125, values_123[0]);
} else if (R.Truth((V)(v_106 == (V)"travelling_trader"))) {
var values_126 = R.Join(new V[] {}, new Results(R.Or(R.And((V)((V)(v_122 % (V)2.0d) == (V)0.0d), () => (V)"cleanse_flask"), () => (V)"escape_scroll")));
var obj_127 = v_114;
V key_128 = (V)"stock";
R.Set(obj_127, key_128, values_126[0]);
} else if (R.Truth((V)(v_106 == (V)"attunement_forge"))) {
var values_129 = R.Join(new V[] {}, new Results(R.Table()));
var obj_130 = v_114;
V key_131 = (V)"trades";
R.Set(obj_130, key_131, values_129[0]);

var values_132 = R.Join(new V[] {}, R.Call(v_83, R.Join(new V[] {}, new Results(v_105))));
V v_133 = values_132[0];
foreach (var row_134 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_133)))))) {
V v_135 = row_134[0];
V v_136 = row_134[1];
var values_137 = R.Join(new V[] {}, new Results(R.Table()));
var obj_138 = R.Get(v_114, (V)"trades");
V key_139 = v_136;
R.Set(obj_138, key_139, values_137[0]);
foreach (var row_140 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"rarities"))))))) {
V v_141 = row_140[0];
V v_142 = row_140[1];
var values_143 = R.Join(new V[] {}, new Results(R.Table()));
V v_144 = values_143[0];

foreach (var row_145 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_133)))))) {
V v_146 = row_145[0];
V v_147 = row_145[1];
if (R.Truth((V)(v_147 != v_136))) {
var values_148 = R.Join(new V[] {}, new Results(v_147));
var obj_149 = v_144;
V key_150 = (V)((V)R.Length(v_144) + (V)1.0d);
R.Set(obj_149, key_150, values_148[0]);
}
}
var values_151 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"new"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"derive"), R.Join(new V[] {v_122}, new Results(R.Concat(v_136, R.Concat((V)":", v_142))))))));
V v_152 = values_151[0];

var values_153 = R.Join(new V[] {}, new Results(R.Table()));
V v_154 = values_153[0];
{ double end_157 = R.Num(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)3.0d}, new Results((V)R.Length(v_144)))).First), step_158 = R.Num((V)1.0d);
for (double i_156 = R.Num((V)1.0d); step_158 > 0 ? i_156 <= end_157 : i_156 >= end_157; i_156 += step_158) {
V v_155 = i_156;
var values_159 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {v_144}, R.Call(R.Get(v_10, (V)"next_int"), R.Join(new V[] {v_152}, new Results((V)R.Length(v_144)))))));
var obj_160 = v_154;
V key_161 = (V)((V)R.Length(v_154) + (V)1.0d);
R.Set(obj_160, key_161, values_159[0]);
}
}
var values_162 = R.Join(new V[] {}, new Results(v_154));
var obj_163 = R.Get(R.Get(v_114, (V)"trades"), v_136);
V key_164 = v_142;
R.Set(obj_163, key_164, values_162[0]);
}
}
}
var values_165 = R.Join(new V[] {}, new Results(v_114));
var obj_166 = R.Get(v_112, (V)"nodes");
V key_167 = (V)((V)R.Length(R.Get(v_112, (V)"nodes")) + (V)1.0d);
R.Set(obj_166, key_167, values_165[0]);

return R.Join(new V[] {}, new Results(v_114));
return Results.Empty;
})));
var values_168 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"offering_font"), new Entry((V)2, (V)"ration_cache"), new Entry((V)3, (V)"cleanse_flask"), new Entry((V)4, (V)"escape_scroll"), new Entry((V)5, (V)"return_token"), new Entry((V)6, (V)"charm_well"), new Entry((V)7, (V)"attunement_forge"), new Entry((V)8, (V)"travelling_trader"), new Entry((V)9, (V)"memorial"), new Entry((V)10, (V)"camp"), new Entry((V)11, (V)"archive_contract"))));
V v_169 = values_168[0];
V v_170 = V.Nil;
v_170 = new V((RuleFunction)(args_171 => {
V v_172 = args_171[0];
V v_173 = args_171[1];
V v_174 = args_171[2];
var values_175 = R.Join(new V[] {}, new Results(R.Table()));
V v_176 = values_175[0];
foreach (var row_177 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_172, (V)"entities"))))))) {
V v_178 = row_177[0];
V v_179 = row_177[1];
var values_180 = R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_179, (V)"controller") == (V)"ai"), () => R.Call(R.Get(v_2, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_179, (V)"definition_id")))).First)));
V v_181 = values_180[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_181, () => (V)(R.Get(v_179, (V)"hp") > (V)0.0d)), () => (V)(R.Get(R.Get(v_179, (V)"position"), (V)"floor_id") == R.Get(v_173, (V)"floor_id"))), () => R.Call(v_63, R.Join(new V[] {v_174, R.Get(R.Get(v_179, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_179, (V)"position"), (V)"y")))).First), () => (V)(R.Get(v_181, (V)"ai") != (V)"warden")), () => (V)(R.Get(v_181, (V)"ai") != (V)"biome_elite")), () => (V)(R.Get(v_181, (V)"ai") != (V)"pursuer")), () => (V)(R.Get(v_181, (V)"ai") != (V)"hollow_regent")), () => (V)(R.Get(v_179, (V)"definition_id") != (V)"core:enemy/rotating_sentinel")), () => (V)(!R.Truth(R.Get(v_179, (V)"reward_exempt")))))) {
var values_182 = R.Join(new V[] {}, new Results(R.Get(v_179, (V)"id")));
var obj_183 = v_176;
V key_184 = (V)((V)R.Length(v_176) + (V)1.0d);
R.Set(obj_183, key_184, values_182[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_176)));

return R.Join(new V[] {}, new Results(v_176));
return Results.Empty;
}));
R.Set(v_18, (V)"populate", new V((RuleFunction)(args_185 => {
V v_186 = args_185[0];
V v_187 = args_185[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_187, (V)"rooms"))), () => (V)((V)R.Length(R.Get(v_187, (V)"rooms")) == (V)0.0d)))) {
return Results.Empty;
}
var values_188 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_189 = values_188[0];
V v_190 = values_188[1];
V v_191 = V.Nil;
v_191 = new V((RuleFunction)(args_192 => {
V v_193 = args_192[0];
V v_194 = args_192[1];
var values_195 = R.Join(new V[] {}, new Results((V)true));
var obj_196 = v_189;
V key_197 = R.Call(v_48, R.Join(new V[] {v_193}, new Results(v_194))).First;
R.Set(obj_196, key_197, values_195[0]);
return Results.Empty;
}));
foreach (var row_198 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Or(R.Get(R.Get(v_187, (V)"placements"), (V)"stairs"), () => R.Table())), new Entry((V)2, R.Or(R.Get(R.Get(v_187, (V)"placements"), (V)"chests"), () => R.Table())), new Entry((V)3, R.Or(R.Get(R.Get(v_187, (V)"placements"), (V)"doors"), () => R.Table())), new Entry((V)4, R.Or(R.Get(R.Get(v_187, (V)"placements"), (V)"secrets"), () => R.Table())), new Entry((V)5, R.Or(R.Get(v_186, (V)"review_features"), () => R.Table()))))))))) {
V v_199 = row_198[0];
V v_200 = row_198[1];
foreach (var row_201 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_200)))))) {
V v_202 = row_201[0];
V v_203 = row_201[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_203, (V)"floor_id"))), () => (V)(R.Get(v_203, (V)"floor_id") == R.Get(v_187, (V)"floor_id"))))) {
R.Call(v_191, R.Join(new V[] {R.Get(v_203, (V)"x")}, new Results(R.Get(v_203, (V)"y"))));
}
}
}
foreach (var row_204 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_186, (V)"entities"))))))) {
V v_205 = row_204[0];
V v_206 = row_204[1];
if (R.Truth((V)(R.Get(R.Get(v_206, (V)"position"), (V)"floor_id") == R.Get(v_187, (V)"floor_id")))) {
R.Call(v_191, R.Join(new V[] {R.Get(R.Get(v_206, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_206, (V)"position"), (V)"y"))));
}
}
var values_207 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"derive"), R.Join(new V[] {R.Get(v_186, (V)"run_seed")}, new Results(R.Concat(R.Get(v_187, (V)"floor_id"), (V)":interactions")))));
V v_208 = values_207[0];
foreach (var row_209 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_187, (V)"rooms"))))))) {
V v_210 = row_209[0];
V v_211 = row_209[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_211, (V)"role") != (V)"start"), () => (V)(R.Get(v_211, (V)"role") != (V)"entrance")), () => (V)(R.Get(v_211, (V)"role") != (V)"elite")), () => (V)(R.Get(v_211, (V)"role") != (V)"exit")))) {
{ double end_214 = R.Num((V)((V)(R.Get(v_211, (V)"y") + R.Get(v_211, (V)"height")) - (V)2.0d)), step_215 = R.Num((V)1.0d);
for (double i_213 = R.Num((V)(R.Get(v_211, (V)"y") + (V)1.0d)); step_215 > 0 ? i_213 <= end_214 : i_213 >= end_214; i_213 += step_215) {
V v_212 = i_213;
{ double end_218 = R.Num((V)((V)(R.Get(v_211, (V)"x") + R.Get(v_211, (V)"width")) - (V)2.0d)), step_219 = R.Num((V)1.0d);
for (double i_217 = R.Num((V)(R.Get(v_211, (V)"x") + (V)1.0d)); step_219 > 0 ? i_217 <= end_218 : i_217 >= end_218; i_217 += step_219) {
V v_216 = i_217;
if (R.Truth(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_189, R.Call(v_48, R.Join(new V[] {v_216}, new Results(v_212))).First))), () => (V)(R.Invoke(R.Get(R.Get(v_187, (V)"rows"), v_212), "sub", R.Join(new V[] {v_216}, new Results(v_216))).First == (V)".")), () => (V)(R.Call(v_52, R.Join(new V[] {R.Table(new Entry((V)"x", v_216), new Entry((V)"y", v_212))}, new Results(R.Table(new Entry((V)"x", R.Get(v_187, (V)"start_x")), new Entry((V)"y", R.Get(v_187, (V)"start_y")))))).First >= (V)4.0d)), () => R.Or((V)(!R.Truth(R.Get(v_187, (V)"elite"))), () => (V)(R.Call(v_52, R.Join(new V[] {R.Table(new Entry((V)"x", v_216), new Entry((V)"y", v_212))}, new Results(R.Get(v_187, (V)"elite")))).First >= (V)4.0d))))) {
var values_220 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_216), new Entry((V)"y", v_212), new Entry((V)"room", v_211), new Entry((V)"score", R.Call(R.Get(v_10, (V)"derive"), R.Join(new V[] {v_208}, R.Call(v_48, R.Join(new V[] {v_216}, new Results(v_212))))).First))));
var obj_221 = v_190;
V key_222 = (V)((V)R.Length(v_190) + (V)1.0d);
R.Set(obj_221, key_222, values_220[0]);
}
}
}
}
}
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_190}, new Results(new V((RuleFunction)(args_223 => {
V v_224 = args_223[0];
V v_225 = args_223[1];
if (R.Truth((V)(R.Get(v_224, (V)"score") != R.Get(v_225, (V)"score")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_224, (V)"score") < R.Get(v_225, (V)"score"))));
}
if (R.Truth((V)(R.Get(v_224, (V)"y") != R.Get(v_225, (V)"y")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_224, (V)"y") < R.Get(v_225, (V)"y"))));
}

return R.Join(new V[] {}, new Results((V)(R.Get(v_224, (V)"x") < R.Get(v_225, (V)"x"))));
return Results.Empty;
})))));
var values_226 = R.Join(new V[] {}, new Results(R.Table()));
V v_227 = values_226[0];
V v_228 = V.Nil;
v_228 = new V((RuleFunction)(args_229 => {
V v_230 = args_229[0];
foreach (var row_231 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_190)))))) {
V v_232 = row_231[0];
V v_233 = row_231[1];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_189, R.Call(v_48, R.Join(new V[] {R.Get(v_233, (V)"x")}, new Results(R.Get(v_233, (V)"y")))).First))), () => (V)(!R.Truth(R.Get(v_227, R.Get(R.Get(v_233, (V)"room"), (V)"id"))))), () => R.Or((V)(!R.Truth(v_230)), () => R.Call(v_230, R.Join(new V[] {}, new Results(v_233))).First)))) {
R.Call(v_191, R.Join(new V[] {R.Get(v_233, (V)"x")}, new Results(R.Get(v_233, (V)"y"))));

var values_234 = R.Join(new V[] {}, new Results((V)true));
var obj_235 = v_227;
V key_236 = R.Get(R.Get(v_233, (V)"room"), (V)"id");
R.Set(obj_235, key_236, values_234[0]);

return R.Join(new V[] {}, new Results(v_233));
}
}
return Results.Empty;
}));
var values_237 = R.Join(new V[] {}, new Results((V)((V)(v_208 % (V)R.Length(v_169)) + (V)1.0d)));
V v_238 = values_237[0];
{ double end_241 = R.Num(R.Get(v_16, (V)"service_cap")), step_242 = R.Num((V)1.0d);
for (double i_240 = R.Num((V)1.0d); step_242 > 0 ? i_240 <= end_241 : i_240 >= end_241; i_240 += step_242) {
V v_239 = i_240;
var values_243 = R.Join(new V[] {}, new Results(R.Get(v_169, (V)((V)((V)((V)(v_238 + v_239) - (V)2.0d) % (V)R.Length(v_169)) + (V)1.0d))));
V v_244 = values_243[0];
var values_247 = R.Join(new V[] {}, R.Call(v_228, R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_244 == (V)"archive_contract"), () => new V((RuleFunction)(args_245 => {
V v_246 = args_245[0];
return R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_170, R.Join(new V[] {v_186, v_187}, new Results(R.Get(v_246, (V)"room")))).First) > (V)0.0d)));
return Results.Empty;
}))), () => V.Nil)))));
V v_248 = values_247[0];
if (R.Truth(v_248)) {
var values_249 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_244 == (V)"archive_contract"), () => R.Table(new Entry((V)"room_id", R.Get(R.Get(v_248, (V)"room"), (V)"id")), new Entry((V)"room", R.Call(v_21, R.Join(new V[] {}, new Results(R.Get(v_248, (V)"room")))).First), new Entry((V)"targets", R.Call(v_170, R.Join(new V[] {v_186, v_187}, new Results(R.Get(v_248, (V)"room")))).First), new Entry((V)"status", (V)"offered"), new Entry((V)"defeated", R.Table()))), () => V.Nil)));
V v_250 = values_249[0];
R.Call(R.Get(v_18, (V)"place"), R.Join(new V[] {v_186, v_244, v_187, R.Get(v_248, (V)"x"), R.Get(v_248, (V)"y")}, new Results(v_250)));
}
}
}
if (R.Truth((V)((V)(v_208 % (V)2.0d) == (V)0.0d))) {
var values_256 = R.Join(new V[] {}, R.Call(v_228, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_251 => {
V v_252 = args_251[0];
foreach (var row_253 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_20)))))) {
V v_254 = row_253[0];
V v_255 = row_253[1];
if (R.Truth(R.Or(R.Or(R.Get(v_189, R.Call(v_48, R.Join(new V[] {(V)(R.Get(v_252, (V)"x") + R.Get(v_255, (V)1.0d))}, new Results((V)(R.Get(v_252, (V)"y") + R.Get(v_255, (V)2.0d))))).First), () => (V)(!R.Truth(R.Call(v_63, R.Join(new V[] {R.Get(v_252, (V)"room"), (V)(R.Get(v_252, (V)"x") + R.Get(v_255, (V)1.0d))}, new Results((V)(R.Get(v_252, (V)"y") + R.Get(v_255, (V)2.0d))))).First))), () => (V)(!R.Truth(R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_187, (V)(R.Get(v_252, (V)"x") + R.Get(v_255, (V)1.0d))}, new Results((V)(R.Get(v_252, (V)"y") + R.Get(v_255, (V)2.0d))))).First))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))))));
V v_257 = values_256[0];
if (R.Truth(v_257)) {
var values_258 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_187, (V)"floor_id"), R.Concat((V)":bells:", R.Call(v_48, R.Join(new V[] {R.Get(v_257, (V)"x")}, new Results(R.Get(v_257, (V)"y")))).First))));
V v_259 = values_258[0];
var values_260 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)3.0d))));
V v_261 = values_260[0];

var values_262 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"new"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"derive"), R.Join(new V[] {v_208}, new Results((V)"bells"))))));
V v_263 = values_262[0];
{ double end_266 = R.Num((V)2.0d), step_267 = R.Num((-(V)1.0d));
for (double i_265 = R.Num((V)3.0d); step_267 > 0 ? i_265 <= end_266 : i_265 >= end_266; i_265 += step_267) {
V v_264 = i_265;
var values_268 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"next_int"), R.Join(new V[] {v_263}, new Results(v_264))));
V v_269 = values_268[0];

var values_270 = R.Join(new V[] {R.Get(v_261, v_269)}, new Results(R.Get(v_261, v_264)));
var obj_271 = v_261;
V key_272 = v_264;
var obj_273 = v_261;
V key_274 = v_269;
R.Set(obj_271, key_272, values_270[0]);
R.Set(obj_273, key_274, values_270[1]);
}
}
var values_275 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"bells"), new Entry((V)"floor_id", R.Get(v_187, (V)"floor_id")), new Entry((V)"sequence", v_261), new Entry((V)"progress", (V)0.0d), new Entry((V)"completed", (V)false))));
var obj_276 = R.Get(R.Call(v_42, R.Join(new V[] {}, new Results(v_186))).First, (V)"groups");
V key_277 = v_259;
R.Set(obj_276, key_277, values_275[0]);
R.Call(R.Get(v_18, (V)"place"), R.Join(new V[] {v_186, (V)"bell_clue", v_187, R.Get(v_257, (V)"x"), R.Get(v_257, (V)"y")}, new Results(R.Table(new Entry((V)"group", v_259)))));
{ double end_280 = R.Num((V)3.0d), step_281 = R.Num((V)1.0d);
for (double i_279 = R.Num((V)1.0d); step_281 > 0 ? i_279 <= end_280 : i_279 >= end_280; i_279 += step_281) {
V v_278 = i_279;
var values_282 = R.Join(new V[] {}, new Results(R.Get(v_20, v_278)));
V v_283 = values_282[0];

R.Call(v_191, R.Join(new V[] {(V)(R.Get(v_257, (V)"x") + R.Get(v_283, (V)1.0d))}, new Results((V)(R.Get(v_257, (V)"y") + R.Get(v_283, (V)2.0d)))));

R.Call(R.Get(v_18, (V)"place"), R.Join(new V[] {v_186, (V)"bell", v_187, (V)(R.Get(v_257, (V)"x") + R.Get(v_283, (V)1.0d)), (V)(R.Get(v_257, (V)"y") + R.Get(v_283, (V)2.0d))}, new Results(R.Table(new Entry((V)"group", v_259), new Entry((V)"bell", v_278)))));
}
}
}
} else {
var values_292 = R.Join(new V[] {}, R.Call(v_228, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_284 => {
V v_285 = args_284[0];
foreach (var row_286 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_287 = row_286[0];
V v_288 = row_286[1];
var values_289 = R.Join(new V[] {(V)((V)(R.Get(v_285, (V)"x") + (V)1.0d) + R.Get(v_288, (V)1.0d))}, new Results((V)(R.Get(v_285, (V)"y") + R.Get(v_288, (V)2.0d))));
V v_290 = values_289[0];
V v_291 = values_289[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Call(v_63, R.Join(new V[] {R.Get(v_285, (V)"room"), v_290}, new Results(v_291))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_187, v_290}, new Results(v_291))).First))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))))));
V v_293 = values_292[0];
if (R.Truth(v_293)) {
var values_294 = R.Join(new V[] {}, new Results(R.Table()));
V v_295 = values_294[0];

var values_296 = R.Join(new V[] {(V)(R.Get(v_293, (V)"x") + (V)1.0d)}, new Results(R.Get(v_293, (V)"y")));
V v_297 = values_296[0];
V v_298 = values_296[1];
foreach (var row_299 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_300 = row_299[0];
V v_301 = row_299[1];
var values_302 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_187, (V)"floor_id")), new Entry((V)"x", (V)(v_297 + R.Get(v_301, (V)1.0d))), new Entry((V)"y", (V)(v_298 + R.Get(v_301, (V)2.0d))))));
var obj_303 = v_295;
V key_304 = (V)((V)R.Length(v_295) + (V)1.0d);
R.Set(obj_303, key_304, values_302[0]);
}
R.Call(R.Get(v_18, (V)"place"), R.Join(new V[] {v_186, (V)"falling_fixture", v_187, R.Get(v_293, (V)"x"), R.Get(v_293, (V)"y")}, new Results(R.Table(new Entry((V)"cells", v_295), new Entry((V)"center_x", v_297), new Entry((V)"center_y", v_298)))));
}
}
var values_305 = R.Join(new V[] {}, R.Call(v_228, Results.Empty));
V v_306 = values_305[0];
if (R.Truth(v_306)) {
var values_307 = Results.Empty;
V v_308 = values_307[0];
foreach (var row_309 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_187, (V)"rooms"))))))) {
V v_310 = row_309[0];
V v_311 = row_309[1];
if (R.Truth(R.And((V)(R.Get(v_311, (V)"id") != R.Get(R.Get(v_306, (V)"room"), (V)"id")), () => R.Or((V)(!R.Truth(v_308)), () => (V)(R.Call(v_52, R.Join(new V[] {v_306}, new Results(R.Table(new Entry((V)"x", R.Get(v_311, (V)"x")), new Entry((V)"y", R.Get(v_311, (V)"y")))))).First < R.Call(v_52, R.Join(new V[] {v_306}, new Results(R.Table(new Entry((V)"x", R.Get(v_308, (V)"x")), new Entry((V)"y", R.Get(v_308, (V)"y")))))).First))))) {
var values_312 = R.Join(new V[] {}, new Results(v_311));
v_308 = values_312[0];
}
}
if (R.Truth(v_308)) {
var values_313 = R.Join(new V[] {R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_306, (V)"x")), new Entry((V)"y", R.Get(v_306, (V)"y")), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)0.0d))))}, new Results(R.Table(new Entry(R.Call(v_48, R.Join(new V[] {R.Get(v_306, (V)"x")}, new Results(R.Get(v_306, (V)"y")))).First, (V)true))));
V v_314 = values_313[0];
V v_315 = values_313[1];

var values_316 = R.Join(new V[] {}, new Results((V)1.0d));
V v_317 = values_316[0];

var values_318 = Results.Empty;
V v_319 = values_318[0];
while (R.Truth((V)(v_317 <= (V)R.Length(v_314)))) {
var values_320 = R.Join(new V[] {}, new Results(R.Get(v_314, v_317)));
V v_321 = values_320[0];

var values_322 = R.Join(new V[] {}, new Results((V)(v_317 + (V)1.0d)));
v_317 = values_322[0];
if (R.Truth(R.Call(v_63, R.Join(new V[] {v_308, R.Get(v_321, (V)"x")}, new Results(R.Get(v_321, (V)"y")))).First)) {
var values_323 = R.Join(new V[] {}, new Results(v_321));
v_319 = values_323[0];

break;
}
foreach (var row_324 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_20)))))) {
V v_325 = row_324[0];
V v_326 = row_324[1];
var values_327 = R.Join(new V[] {(V)(R.Get(v_321, (V)"x") + R.Get(v_326, (V)1.0d))}, new Results((V)(R.Get(v_321, (V)"y") + R.Get(v_326, (V)2.0d))));
V v_328 = values_327[0];
V v_329 = values_327[1];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_315, R.Call(v_48, R.Join(new V[] {v_328}, new Results(v_329))).First))), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_187, v_328}, new Results(v_329))).First))) {
var values_330 = R.Join(new V[] {}, new Results(R.Or((V)(R.Get(v_321, (V)"dx") != (V)0.0d), () => (V)(R.Get(v_321, (V)"dy") != (V)0.0d))));
V v_331 = values_330[0];
var values_332 = R.Join(new V[] {}, new Results((V)true));
var obj_333 = v_315;
V key_334 = R.Call(v_48, R.Join(new V[] {v_328}, new Results(v_329))).First;
R.Set(obj_333, key_334, values_332[0]);

var values_335 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_328), new Entry((V)"y", v_329), new Entry((V)"dx", R.Or(R.And(v_331, () => R.Get(v_321, (V)"dx")), () => R.Get(v_326, (V)1.0d))), new Entry((V)"dy", R.Or(R.And(v_331, () => R.Get(v_321, (V)"dy")), () => R.Get(v_326, (V)2.0d))))));
var obj_336 = v_314;
V key_337 = (V)((V)R.Length(v_314) + (V)1.0d);
R.Set(obj_336, key_337, values_335[0]);
}
}
}
if (R.Truth(v_319)) {
R.Call(R.Get(v_18, (V)"place"), R.Join(new V[] {v_186, (V)"route_sign", v_187, R.Get(v_306, (V)"x"), R.Get(v_306, (V)"y")}, new Results(R.Table(new Entry((V)"dx", R.Get(v_319, (V)"dx")), new Entry((V)"dy", R.Get(v_319, (V)"dy")), new Entry((V)"hint", R.Or(R.Or(R.And((V)(R.Get(v_308, (V)"role") == (V)"treasure"), () => (V)"Treasure"), () => R.And((V)(R.Get(v_308, (V)"role") == (V)"supply"), () => (V)"Supplies")), () => (V)"Rooms / danger"))))));
}
}
}
return Results.Empty;
})));
V v_338 = V.Nil;
v_338 = new V((RuleFunction)(args_339 => {
V v_340 = args_339[0];
V v_341 = args_339[1];
if (R.Truth((V)(!R.Truth(v_341)))) {
return R.Join(new V[] {(V)false}, new Results((V)"unknown interaction"));
}
var values_342 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_39, R.Join(new V[] {}, new Results(v_340))).First, (V)"position")));
V v_343 = values_342[0];
if (R.Truth(R.Or(R.Or((V)(R.Get(v_343, (V)"floor_id") != R.Get(v_341, (V)"floor_id")), () => (V)(R.Call(v_52, R.Join(new V[] {v_343}, new Results(v_341))).First > (V)1.0d)), () => (V)(!R.Truth(R.Call(R.Get(v_14, (V)"can_see"), R.Join(new V[] {v_340, R.Get(v_341, (V)"floor_id"), R.Get(v_341, (V)"x")}, new Results(R.Get(v_341, (V)"y")))).First))))) {
return R.Join(new V[] {(V)false}, new Results((V)"stand beside a visible interaction"));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
V v_344 = V.Nil;
v_344 = new V((RuleFunction)(args_345 => {
V v_346 = args_345[0];
V v_347 = args_345[1];
V v_348 = args_345[2];
var values_349 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"boons", R.Call(v_21, R.Join(new V[] {}, new Results(R.Get(v_346, (V)"boons")))).First))));
V v_350 = values_349[0];
if (R.Truth(v_348)) {
R.Call(R.Get(v_6, (V)"set_stacks"), R.Join(new V[] {v_350, R.Get(v_348, (V)"id"), R.Get(v_348, (V)"rarity")}, new Results((V)(R.Get(R.Get(R.Get(v_350, (V)"boons"), R.Get(v_348, (V)"id")), R.Get(v_348, (V)"rarity")) - (V)1.0d))));
}
if (R.Truth(v_347)) {
var values_351 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(R.Get(v_350, (V)"boons"), R.Get(v_347, (V)"boon_id")), () => R.Get(R.Get(R.Get(v_350, (V)"boons"), R.Get(v_347, (V)"boon_id")), R.Get(v_347, (V)"rarity"))), () => (V)0.0d)));
V v_352 = values_351[0];
return R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_6, (V)"set_stacks"), v_350, R.Get(v_347, (V)"boon_id"), R.Get(v_347, (V)"rarity")}, new Results((V)(v_352 + (V)1.0d)))));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
V v_353 = V.Nil;
v_353 = new V((RuleFunction)(args_354 => {
V v_355 = args_354[0];
V v_356 = args_354[1];
V v_357 = args_354[2];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"take"), new Entry((V)"label", R.Concat(R.Get(R.Get(v_16, (V)"names"), v_356), R.Or(R.And((V)(v_357 > (V)0.0d), () => R.Concat((V)" \u00c2\u00b7 ", R.Concat(v_357, (V)" gold"))), () => (V)""))), new Entry((V)"utility", v_356), new Entry((V)"price", v_357), new Entry((V)"replace_required", (V)(R.Get(v_355, (V)"utility") != V.Nil)), new Entry((V)"detail", R.Concat((V)"One charge. ", R.Or(R.And(R.Get(v_355, (V)"utility"), () => R.Concat((V)"Replaces ", R.Concat(R.Get(R.Get(v_16, (V)"names"), R.Get(R.Get(v_355, (V)"utility"), (V)"kind")), (V)"; confirm that exchange explicitly."))), () => (V)"Uses the one carried utility slot."))))));
return Results.Empty;
}));
R.Set(v_18, (V)"options", new V((RuleFunction)(args_358 => {
V v_359 = args_358[0];
V v_360 = args_358[1];
V v_361 = args_358[2];
if (R.Truth(R.And((V)(v_361 != V.Nil), () => R.Or(R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_361))).First != (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_361, (V)"id")))).First != (V)"string")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_361, (V)"rarity")))).First != (V)"string"))))) {
return R.Join(new V[] {V.Nil}, new Results((V)"invalid stack selection"));
}
var values_362 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"node"), R.Join(new V[] {v_359}, new Results(v_360))));
V v_363 = values_362[0];

var values_364 = R.Join(new V[] {}, R.Call(v_338, R.Join(new V[] {v_359}, new Results(v_363))));
V v_365 = values_364[0];
V v_366 = values_364[1];
if (R.Truth((V)(!R.Truth(v_365)))) {
return R.Join(new V[] {V.Nil}, new Results(v_366));
}
var values_367 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {}, new Results(v_359))));
V v_368 = values_367[0];

var values_369 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"node_id", v_360), new Entry((V)"title", R.Get(R.Get(v_16, (V)"names"), R.Get(v_363, (V)"kind"))), new Entry((V)"entries", R.Table()), new Entry((V)"detail", (V)"Confirm spends one normal action. Cancel is free."), new Entry((V)"selection", R.Call(v_21, R.Join(new V[] {}, new Results(v_361))).First))));
V v_370 = values_369[0];
var values_371 = R.Join(new V[] {}, new Results(R.Get(v_370, (V)"entries")));
V v_372 = values_371[0];
V v_373 = V.Nil;
v_373 = new V((RuleFunction)(args_374 => {
V v_375 = args_374[0];
var values_376 = R.Join(new V[] {}, new Results((V)(R.Get(v_375, (V)"enabled") != (V)false)));
var obj_377 = v_375;
V key_378 = (V)"enabled";
R.Set(obj_377, key_378, values_376[0]);

var values_379 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_375, (V)"price"), () => (V)0.0d)));
var obj_380 = v_375;
V key_381 = (V)"price";
R.Set(obj_380, key_381, values_379[0]);
if (R.Truth((V)(R.Get(v_375, (V)"price") > R.Get(v_359, (V)"currency")))) {
var values_382 = R.Join(new V[] {}, new Results((V)false));
var obj_383 = v_375;
V key_384 = (V)"enabled";
R.Set(obj_383, key_384, values_382[0]);

var values_385 = R.Join(new V[] {}, new Results(R.Concat((V)"Need ", R.Concat((V)(R.Get(v_375, (V)"price") - R.Get(v_359, (V)"currency")), (V)" more gold"))));
var obj_386 = v_375;
V key_387 = (V)"reason";
R.Set(obj_386, key_387, values_385[0]);
}
var values_388 = R.Join(new V[] {}, new Results(v_375));
var obj_389 = v_372;
V key_390 = (V)((V)R.Length(v_372) + (V)1.0d);
R.Set(obj_389, key_390, values_388[0]);
return Results.Empty;
}));
if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"route_sign"))) {
var values_391 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_363, (V)"hint"), R.Concat((V)" ", R.Concat(R.Or(R.Or(R.Or(R.And((V)(R.Get(v_363, (V)"dx") < (V)0.0d), () => (V)"west"), () => R.And((V)(R.Get(v_363, (V)"dx") > (V)0.0d), () => (V)"east")), () => R.And((V)(R.Get(v_363, (V)"dy") < (V)0.0d), () => (V)"north")), () => (V)"south"), (V)". A coarse hint only; reading costs no time.")))));
var obj_392 = v_370;
V key_393 = (V)"detail";
R.Set(obj_392, key_393, values_391[0]);
} else if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"bell_clue"))) {
var values_394 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_359, (V)"idea_rooms"), (V)"groups"), R.Get(v_363, (V)"group"))));
V v_395 = values_394[0];
var values_396 = R.Join(new V[] {}, new Results(R.Concat((V)"Ring bells ", R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Get(v_395, (V)"sequence")}, new Results((V)" \u00e2\u0086\u0092 "))).First, (V)". Labels work without colour or audio. Reading costs no time."))));
var obj_397 = v_370;
V key_398 = (V)"detail";
R.Set(obj_397, key_398, values_396[0]);
} else if (R.Truth(R.Get(v_363, (V)"spent"))) {
var values_399 = R.Join(new V[] {}, new Results((V)"Already used. No refill on returning or Continue."));
var obj_400 = v_370;
V key_401 = (V)"detail";
R.Set(obj_400, key_401, values_399[0]);
} else if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"offering_font"))) {
var values_402 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"ceil"), R.Join(new V[] {}, new Results((V)(R.Get(v_368, (V)"max_hp") * (V)0.2d)))))));
V v_403 = values_402[0];
var values_404 = R.Join(new V[] {}, R.Invoke((V)"Pay %d HP (remain %d/%d). Choose one saved offer; no rerolls.", "format", R.Join(new V[] {v_403, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(R.Get(v_368, (V)"hp") - v_403)))).First}, new Results(R.Get(v_368, (V)"max_hp")))));
var obj_405 = v_370;
V key_406 = (V)"detail";
R.Set(obj_405, key_406, values_404[0]);
foreach (var row_407 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_363, (V)"offers"))))))) {
V v_408 = row_407[0];
V v_409 = row_407[1];
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_408))).First), new Entry((V)"offer", R.Call(v_21, R.Join(new V[] {}, new Results(v_409))).First), new Entry((V)"hp_cost", v_403), new Entry((V)"label", R.Concat(R.Get(R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_409, (V)"boon_id")))).First, (V)"name"), R.Concat((V)" \u00c2\u00b7 ", R.Get(v_409, (V)"rarity")))), new Entry((V)"enabled", R.And((V)(R.Get(v_368, (V)"hp") > v_403), () => R.Call(v_344, R.Join(new V[] {v_368}, new Results(v_409))).First)), new Entry((V)"reason", R.Or(R.And((V)(R.Get(v_368, (V)"hp") <= v_403), () => (V)"Payment must leave at least 1 HP"), () => (V)"Incompatible boon"))))));
}
} else if (R.Truth(R.Or((V)(R.Get(v_363, (V)"kind") == (V)"ration_cache"), () => (V)(R.Get(v_363, (V)"kind") == (V)"camp")))) {
var values_410 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_363, (V)"kind") == (V)"camp"), () => R.Call(R.Get(rt.Global("math"), (V)"ceil"), R.Join(new V[] {}, new Results((V)(R.Get(v_368, (V)"max_hp") * (V)0.25d)))).First), () => (V)4.0d)));
V v_411 = values_410[0];
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"food"), new Entry((V)"label", R.Concat((V)"Recover ", R.Concat(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_411}, new Results((V)(R.Get(v_368, (V)"max_hp") - R.Get(v_368, (V)"hp"))))).First, (V)" HP"))), new Entry((V)"heal", v_411), new Entry((V)"enabled", (V)(R.Get(v_368, (V)"hp") < R.Get(v_368, (V)"max_hp"))), new Entry((V)"reason", (V)"Already at full HP")))));
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"ward"), new Entry((V)"label", R.Or(R.And((V)(R.Get(v_363, (V)"kind") == (V)"camp"), () => (V)"Cleanse Burning + one-hit ward"), () => (V)"One-hit ward")), new Entry((V)"cleanse", (V)(R.Get(v_363, (V)"kind") == (V)"camp")), new Entry((V)"ward", (V)true), new Entry((V)"detail", (V)"Blocks one positive hit within the next three committed player actions; replaces a supply ward.")))));
} else if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"charm_well"))) {
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"focus"), new Entry((V)"label", (V)"Focus \u00c2\u00b7 +2 to next direct attack"), new Entry((V)"focus", (V)true), new Entry((V)"detail", (V)"One primary attack batch, expires after three committed actions; replaces a supply condition.")))));
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"ward"), new Entry((V)"label", (V)"Brace \u00c2\u00b7 one-hit ward"), new Entry((V)"ward", (V)true), new Entry((V)"detail", (V)"One positive hit, expires after three committed actions; replaces a supply condition.")))));
} else if (R.Truth(R.Or(R.Or((V)(R.Get(v_363, (V)"kind") == (V)"cleanse_flask"), () => (V)(R.Get(v_363, (V)"kind") == (V)"escape_scroll")), () => (V)(R.Get(v_363, (V)"kind") == (V)"return_token")))) {
R.Call(v_373, R.Join(new V[] {}, R.Call(v_353, R.Join(new V[] {v_368, R.Get(v_363, (V)"kind")}, new Results((V)0.0d)))));
} else if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"travelling_trader"))) {
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"food"), new Entry((V)"label", (V)"Food \u00c2\u00b7 +4 HP \u00c2\u00b7 2 gold"), new Entry((V)"price", (V)2.0d), new Entry((V)"heal", (V)4.0d), new Entry((V)"enabled", (V)(R.Get(v_368, (V)"hp") < R.Get(v_368, (V)"max_hp"))), new Entry((V)"reason", (V)"Already at full HP")))));
var values_412 = R.Join(new V[] {}, R.Call(v_353, R.Join(new V[] {v_368, R.Get(v_363, (V)"stock")}, new Results(R.Or(R.And((V)(R.Get(v_363, (V)"stock") == (V)"cleanse_flask"), () => (V)2.0d), () => (V)3.0d)))));
V v_413 = values_412[0];

var values_414 = R.Join(new V[] {}, new Results((V)"utility"));
var obj_415 = v_413;
V key_416 = (V)"id";
R.Set(obj_415, key_416, values_414[0]);

R.Call(v_373, R.Join(new V[] {}, new Results(v_413)));
} else if (R.Truth(R.Or((V)(R.Get(v_363, (V)"kind") == (V)"attunement_forge"), () => (V)(R.Get(v_363, (V)"kind") == (V)"memorial")))) {
var values_417 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_363, (V)"kind") == (V)"memorial"), () => (V)"Retire exactly one rarity stack; pending committed effects remain. Healing is capped at missing HP."), () => (V)"Trade exactly one rarity stack + 3 gold for a fixed same-rarity alternative. Inspect a stack first.")));
var obj_418 = v_370;
V key_419 = (V)"detail";
R.Set(obj_418, key_419, values_417[0]);
var values_420 = R.Join(new V[] {}, new Results(R.Table()));
V v_421 = values_420[0];

foreach (var row_422 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_368, (V)"boons"))))))) {
V v_423 = row_422[0];
var values_424 = R.Join(new V[] {}, new Results(v_423));
var obj_425 = v_421;
V key_426 = (V)((V)R.Length(v_421) + (V)1.0d);
R.Set(obj_425, key_426, values_424[0]);
}

R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_421)));
foreach (var row_427 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_421)))))) {
V v_428 = row_427[0];
V v_429 = row_427[1];
foreach (var row_430 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"rarities"))))))) {
V v_431 = row_430[0];
V v_432 = row_430[1];
var values_433 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(R.Get(v_368, (V)"boons"), v_429), v_432), () => (V)0.0d)));
V v_434 = values_433[0];
if (R.Truth(R.And((V)(v_434 > (V)0.0d), () => R.Or((V)(!R.Truth(v_361)), () => R.And((V)(R.Get(v_361, (V)"id") == v_429), () => (V)(R.Get(v_361, (V)"rarity") == v_432)))))) {
var values_435 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_429), new Entry((V)"rarity", v_432))));
V v_436 = values_435[0];
if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"memorial"))) {
var values_437 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"ceil"), R.Join(new V[] {}, new Results((V)(R.Get(v_368, (V)"max_hp") * (V)0.35d)))));
V v_438 = values_437[0];
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat(v_429, R.Concat((V)"|", v_432))), new Entry((V)"remove", v_436), new Entry((V)"heal", v_438), new Entry((V)"label", R.Concat(R.Get(R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_429))).First, (V)"name"), R.Concat((V)" \u00c2\u00b7 ", R.Concat(v_432, R.Concat((V)" x", v_434))))), new Entry((V)"detail", R.Invoke((V)"Retire one; %d stacks remain in this rarity. Recover %d HP.", "format", R.Join(new V[] {(V)(v_434 - (V)1.0d)}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_438}, new Results((V)(R.Get(v_368, (V)"max_hp") - R.Get(v_368, (V)"hp"))))))).First), new Entry((V)"enabled", (V)(R.Get(v_368, (V)"hp") < R.Get(v_368, (V)"max_hp"))), new Entry((V)"reason", (V)"Already at full HP")))));
} else if (R.Truth((V)(!R.Truth(v_361)))) {
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat(v_429, R.Concat((V)"|", v_432))), new Entry((V)"select", v_436), new Entry((V)"label", R.Concat(R.Get(R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_429))).First, (V)"name"), R.Concat((V)" \u00c2\u00b7 ", R.Concat(v_432, R.Concat((V)" x", v_434)))))))));
} else {
foreach (var row_439 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(R.Get(v_363, (V)"trades"), v_429), () => R.Get(R.Get(R.Get(v_363, (V)"trades"), v_429), v_432)), () => R.Table()))))))) {
V v_440 = row_439[0];
V v_441 = row_439[1];
var values_442 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"boon_id", v_441), new Entry((V)"rarity", v_432))));
V v_443 = values_442[0];
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_440))).First), new Entry((V)"remove", v_436), new Entry((V)"offer", v_443), new Entry((V)"price", (V)3.0d), new Entry((V)"label", R.Concat(R.Get(R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_441))).First, (V)"name"), R.Concat((V)" \u00c2\u00b7 ", R.Concat(v_432, (V)" \u00c2\u00b7 3 gold")))), new Entry((V)"detail", R.Concat((V)"Retire one ", R.Concat(R.Get(R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_429))).First, (V)"name"), R.Concat((V)" (", R.Concat(v_432, (V)"); other stacks remain. New ability geometry is recalculated."))))), new Entry((V)"enabled", R.Call(v_344, R.Join(new V[] {v_368, v_443}, new Results(v_436))).First), new Entry((V)"reason", (V)"Incompatible replacement")))));
}
}
}
}
}
if (R.Truth((V)((V)R.Length(v_372) == (V)0.0d))) {
var values_444 = R.Join(new V[] {}, new Results((V)"No eligible owned stack / replacement. Nothing is spent."));
var obj_445 = v_370;
V key_446 = (V)"detail";
R.Set(obj_445, key_446, values_444[0]);
}
} else if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"bell"))) {
var values_447 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_359, (V)"idea_rooms"), (V)"groups"), R.Get(v_363, (V)"group"))));
V v_448 = values_447[0];
var values_449 = R.Join(new V[] {}, new Results(R.Concat((V)"Bell ", R.Get(v_363, (V)"bell"))));
var obj_450 = v_370;
V key_451 = (V)"title";
R.Set(obj_450, key_451, values_449[0]);

var values_452 = R.Join(new V[] {}, new Results(R.Concat((V)"Clue ", R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Get(v_448, (V)"sequence")}, new Results((V)" \u00e2\u0086\u0092 "))).First, R.Concat((V)" \u00c2\u00b7 progress ", R.Concat(R.Get(v_448, (V)"progress"), (V)"/3. Wrong bells reset progress; one 5-gold reward."))))));
var obj_453 = v_370;
V key_454 = (V)"detail";
R.Set(obj_453, key_454, values_452[0]);
if (R.Truth((V)(!R.Truth(R.Get(v_448, (V)"completed"))))) {
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"ring"), new Entry((V)"label", R.Concat((V)"Ring bell ", R.Get(v_363, (V)"bell")))))));
} else {
var values_455 = R.Join(new V[] {}, new Results((V)"Sequence completed; reward already collected."));
var obj_456 = v_370;
V key_457 = (V)"detail";
R.Set(obj_456, key_457, values_455[0]);
}
} else if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"falling_fixture"))) {
var values_458 = R.Join(new V[] {}, new Results((V)"One use. Locked five-tile cross, base 4 physical damage to everyone, impact in +100 world time. No guaranteed reaction turn."));
var obj_459 = v_370;
V key_460 = (V)"detail";
R.Set(obj_459, key_460, values_458[0]);
var values_461 = R.Join(new V[] {}, new Results((V)true));
V v_462 = values_461[0];

foreach (var row_463 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_363, (V)"cells"))))))) {
V v_464 = row_463[0];
V v_465 = row_463[1];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_14, (V)"can_see"), R.Join(new V[] {v_359, R.Get(v_363, (V)"floor_id"), R.Get(v_465, (V)"x")}, new Results(R.Get(v_465, (V)"y")))).First)))) {
var values_466 = R.Join(new V[] {}, new Results((V)false));
v_462 = values_466[0];
}
}
var values_467 = R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {}, new Results(R.Get(v_363, (V)"cells")))));
var obj_468 = v_370;
V key_469 = (V)"cells";
R.Set(obj_468, key_469, values_467[0]);

R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"trigger"), new Entry((V)"label", (V)"Trigger falling fixture"), new Entry((V)"enabled", v_462), new Entry((V)"reason", (V)"See the complete impact cross before triggering"), new Entry((V)"detail", (V)"Confirm only if its marked cross is safe for you.")))));
} else if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"archive_contract"))) {
var values_470 = R.Join(new V[] {}, new Results((V)"Clear marked ordinary enemies without leaving this room or using a movement ability. Walking is allowed; abilities are never disabled. Reward: 8 gold once."));
var obj_471 = v_370;
V key_472 = (V)"detail";
R.Set(obj_471, key_472, values_470[0]);
if (R.Truth((V)(R.Get(v_363, (V)"status") == (V)"offered"))) {
var values_473 = R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_363, (V)"targets")) > (V)0.0d)));
V v_474 = values_473[0];
foreach (var row_475 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_363, (V)"targets"))))))) {
V v_476 = row_475[0];
V v_477 = row_475[1];
var values_478 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_359}, new Results(v_477))));
V v_479 = values_478[0];
if (R.Truth(R.Or((V)(!R.Truth(v_479)), () => (V)(!R.Truth(R.Call(v_63, R.Join(new V[] {R.Get(v_363, (V)"room"), R.Get(R.Get(v_479, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_479, (V)"position"), (V)"y")))).First))))) {
var values_480 = R.Join(new V[] {}, new Results((V)false));
v_474 = values_480[0];
}
}
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"accept"), new Entry((V)"label", (V)"Accept room contract"), new Entry((V)"enabled", v_474), new Entry((V)"reason", (V)"Original targets have already left or died")))));
} else if (R.Truth((V)(R.Get(v_363, (V)"status") == (V)"active"))) {
R.Call(v_373, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"abandon"), new Entry((V)"label", (V)"Abandon contract"), new Entry((V)"detail", (V)"Normal controls and kill rewards remain.")))));
} else {
var values_481 = R.Join(new V[] {}, new Results(R.Concat((V)"Contract ", R.Concat(R.Get(v_363, (V)"status"), R.Concat((V)": ", R.Or(R.Get(v_363, (V)"failure"), () => (V)"reward collected"))))));
var obj_482 = v_370;
V key_483 = (V)"detail";
R.Set(obj_482, key_483, values_481[0]);
}
}
return R.Join(new V[] {}, new Results(v_370));
return Results.Empty;
})));
R.Set(v_18, (V)"preview", new V((RuleFunction)(args_484 => {
V v_485 = args_484[0];
V v_486 = args_484[1];
var values_487 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"options"), R.Join(new V[] {v_485, R.Get(v_486, (V)"node_id")}, new Results(R.Get(v_486, (V)"selection")))));
V v_488 = values_487[0];
V v_489 = values_487[1];
if (R.Truth((V)(!R.Truth(v_488)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", v_489))));
}
foreach (var row_490 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_488, (V)"entries"))))))) {
V v_491 = row_490[0];
V v_492 = row_490[1];
if (R.Truth(R.And((V)(R.Get(v_492, (V)"id") == R.Get(v_486, (V)"choice_id")), () => (V)(!R.Truth(R.Get(v_492, (V)"select")))))) {
if (R.Truth((V)(!R.Truth(R.Get(v_492, (V)"enabled"))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", R.Or(R.Get(v_492, (V)"reason"), () => (V)"unavailable")))));
}
if (R.Truth(R.And(R.Get(v_492, (V)"replace_required"), () => (V)(R.Get(v_486, (V)"replace") != (V)true)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"confirm replacing the carried utility"))));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)true), new Entry((V)"entry", v_492), new Entry((V)"node_id", R.Get(v_486, (V)"node_id")), new Entry((V)"cells", R.Or(R.Get(v_488, (V)"cells"), () => R.Table())))));
}
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"choose a valid interaction option"))));
return Results.Empty;
})));
V v_493 = V.Nil;
v_493 = new V((RuleFunction)(args_494 => {
V v_495 = args_494[0];
V v_496 = args_494[1];
V v_497 = args_494[2];
var values_498 = R.Join(new V[] {}, new Results(R.And(R.Get(v_496, (V)"statuses"), () => R.Get(R.Get(v_496, (V)"statuses"), (V)"burning"))));
V v_499 = values_498[0];
if (R.Truth(v_499)) {
{ double end_502 = R.Num((V)1.0d), step_503 = R.Num((-(V)1.0d));
for (double i_501 = R.Num((V)R.Length(R.Get(v_495, (V)"schedule"))); step_503 > 0 ? i_501 <= end_502 : i_501 >= end_502; i_501 += step_503) {
V v_500 = i_501;
if (R.Truth((V)(R.Get(R.Get(R.Get(v_495, (V)"schedule"), v_500), (V)"id") == R.Get(v_499, (V)"schedule_id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_495, (V)"schedule")}, new Results(v_500)));
}
}
}
var values_504 = R.Join(new V[] {}, new Results(V.Nil));
var obj_505 = R.Get(v_496, (V)"statuses");
V key_506 = (V)"burning";
R.Set(obj_505, key_506, values_504[0]);
R.Call(v_497, R.Join(new V[] {(V)"StatusCleansed", R.Table(new Entry((V)"status", (V)"burning"), new Entry((V)"target_id", R.Get(v_496, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_496, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_496, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_496, (V)"position"), (V)"y")))}, new Results(R.Get(v_496, (V)"id"))));
}
return Results.Empty;
}));
V v_507 = V.Nil;
v_507 = new V((RuleFunction)(args_508 => {
V v_509 = args_508[0];
V v_510 = args_508[1];
V v_511 = args_508[2];
V v_512 = args_508[3];
var values_513 = R.Join(new V[] {}, new Results(R.Get(v_509, (V)"hp")));
V v_514 = values_513[0];

var values_515 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_509, (V)"max_hp")}, new Results((V)(R.Get(v_509, (V)"hp") + v_510)))));
var obj_516 = v_509;
V key_517 = (V)"hp";
R.Set(obj_516, key_517, values_515[0]);
var values_518 = R.Join(new V[] {}, new Results(R.Or((V)(v_512 == (V)"ration_cache"), () => (V)(v_512 == (V)"travelling_trader"))));
V v_519 = values_518[0];
R.Call(v_511, R.Join(new V[] {R.Or(R.And(v_519, () => (V)"SupplyCollected"), () => (V)"Healed"), R.Table(new Entry((V)"supply_kind", R.Or(R.And(v_519, () => (V)"food"), () => V.Nil)), new Entry((V)"target_id", R.Get(v_509, (V)"id")), new Entry((V)"requested", v_510), new Entry((V)"amount", v_510), new Entry((V)"actual_gain", (V)(R.Get(v_509, (V)"hp") - v_514)), new Entry((V)"hp_before", v_514), new Entry((V)"hp_after", R.Get(v_509, (V)"hp")), new Entry((V)"floor_id", R.Get(R.Get(v_509, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_509, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_509, (V)"position"), (V)"y")))}, new Results(R.Get(v_509, (V)"id"))));
return Results.Empty;
}));
R.Set(v_18, (V)"apply", new V((RuleFunction)(args_520 => {
V v_521 = args_520[0];
V v_522 = args_520[1];
V v_523 = args_520[2];
V v_524 = args_520[3];
var values_525 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {}, new Results(v_521))));
V v_526 = values_525[0];

var values_527 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"node"), R.Join(new V[] {v_521}, new Results(R.Get(v_522, (V)"node_id")))))));
V v_528 = values_527[0];

var values_529 = R.Join(new V[] {}, new Results(R.Get(v_522, (V)"entry")));
V v_530 = values_529[0];
if (R.Truth((V)(R.Get(v_530, (V)"price") > (V)0.0d))) {
var values_531 = R.Join(new V[] {}, new Results(R.Get(v_521, (V)"currency")));
V v_532 = values_531[0];

var values_533 = R.Join(new V[] {}, new Results((V)(v_532 - R.Get(v_530, (V)"price"))));
var obj_534 = v_521;
V key_535 = (V)"currency";
R.Set(obj_534, key_535, values_533[0]);
R.Call(v_524, R.Join(new V[] {(V)"CurrencySpent", R.Table(new Entry((V)"amount", R.Get(v_530, (V)"price")), new Entry((V)"old_currency", v_532), new Entry((V)"new_currency", R.Get(v_521, (V)"currency")))}, new Results(R.Get(v_526, (V)"id"))));
}
if (R.Truth(R.Get(v_530, (V)"hp_cost"))) {
var values_536 = R.Join(new V[] {}, new Results((V)(R.Get(v_526, (V)"hp") - R.Get(v_530, (V)"hp_cost"))));
var obj_537 = v_526;
V key_538 = (V)"hp";
R.Set(obj_537, key_538, values_536[0]);

R.Call(v_524, R.Join(new V[] {(V)"FontOfferingPaid", R.Table(new Entry((V)"amount", R.Get(v_530, (V)"hp_cost")), new Entry((V)"hp_after", R.Get(v_526, (V)"hp")), new Entry((V)"floor_id", R.Get(v_528, (V)"floor_id")), new Entry((V)"x", R.Get(v_528, (V)"x")), new Entry((V)"y", R.Get(v_528, (V)"y")))}, new Results(R.Get(v_526, (V)"id"))));
}
if (R.Truth(R.Get(v_530, (V)"remove"))) {
var values_539 = R.Join(new V[] {}, new Results(R.Get(v_530, (V)"remove")));
V v_540 = values_539[0];

var values_541 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_526, (V)"boons"), R.Get(v_540, (V)"id")), R.Get(v_540, (V)"rarity"))));
V v_542 = values_541[0];
R.Call(R.Get(v_6, (V)"set_stacks"), R.Join(new V[] {v_526, R.Get(v_540, (V)"id"), R.Get(v_540, (V)"rarity")}, new Results((V)(v_542 - (V)1.0d))));
if (R.Truth(R.And((V)(!R.Truth(R.Get(R.Get(v_526, (V)"boons"), R.Get(v_540, (V)"id")))), () => R.Get(v_526, (V)"boon_memory")))) {
var values_543 = R.Join(new V[] {}, new Results(V.Nil));
var obj_544 = R.Get(v_526, (V)"boon_memory");
V key_545 = R.Get(v_540, (V)"id");
R.Set(obj_544, key_545, values_543[0]);
}
R.Call(v_524, R.Join(new V[] {(V)"BoonRetired", R.Table(new Entry((V)"boon_id", R.Get(v_540, (V)"id")), new Entry((V)"rarity", R.Get(v_540, (V)"rarity")), new Entry((V)"old_count", v_542), new Entry((V)"new_count", (V)(v_542 - (V)1.0d)))}, new Results(R.Get(v_526, (V)"id"))));
}
if (R.Truth(R.Get(v_530, (V)"offer"))) {
var values_546 = R.Join(new V[] {}, new Results(R.Get(v_530, (V)"offer")));
V v_547 = values_546[0];

var values_548 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(R.Get(v_526, (V)"boons"), R.Get(v_547, (V)"boon_id")), () => R.Get(R.Get(R.Get(v_526, (V)"boons"), R.Get(v_547, (V)"boon_id")), R.Get(v_547, (V)"rarity"))), () => (V)0.0d)));
V v_549 = values_548[0];
R.Call(R.Get(v_6, (V)"set_stacks"), R.Join(new V[] {v_526, R.Get(v_547, (V)"boon_id"), R.Get(v_547, (V)"rarity")}, new Results((V)(v_549 + (V)1.0d))));
R.Call(v_524, R.Join(new V[] {(V)"BoonGranted", R.Table(new Entry((V)"boon_id", R.Get(v_547, (V)"boon_id")), new Entry((V)"rarity", R.Get(v_547, (V)"rarity")), new Entry((V)"old_count", v_549), new Entry((V)"new_count", (V)(v_549 + (V)1.0d)))}, new Results(R.Get(v_526, (V)"id"))));
}
if (R.Truth(R.Get(v_530, (V)"heal"))) {
R.Call(v_507, R.Join(new V[] {v_526, R.Get(v_530, (V)"heal"), v_524}, new Results(R.Get(v_528, (V)"kind"))));
}
if (R.Truth(R.Get(v_530, (V)"cleanse"))) {
R.Call(v_493, R.Join(new V[] {v_521, v_526}, new Results(v_524)));
}
if (R.Truth(R.Or(R.Get(v_530, (V)"ward"), () => R.Get(v_530, (V)"focus")))) {
var values_550 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_530, (V)"ward"), () => (V)4.0d), () => (V)0.0d)));
var obj_551 = v_526;
V key_552 = (V)"pickup_ward_actions";
R.Set(obj_551, key_552, values_550[0]);

var values_553 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_530, (V)"focus"), () => (V)4.0d), () => (V)0.0d)));
var obj_554 = v_526;
V key_555 = (V)"supply_focus_actions";
R.Set(obj_554, key_555, values_553[0]);
R.Call(v_524, R.Join(new V[] {(V)"SupplyPrepared", R.Table(new Entry((V)"condition", R.Or(R.And(R.Get(v_530, (V)"ward"), () => (V)"ward"), () => (V)"focus")), new Entry((V)"actions", (V)3.0d), new Entry((V)"floor_id", R.Get(v_528, (V)"floor_id")), new Entry((V)"x", R.Get(v_528, (V)"x")), new Entry((V)"y", R.Get(v_528, (V)"y")))}, new Results(R.Get(v_526, (V)"id"))));
}
if (R.Truth(R.Get(v_530, (V)"utility"))) {
var values_556 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", R.Get(v_530, (V)"utility")))));
var obj_557 = v_526;
V key_558 = (V)"utility";
R.Set(obj_557, key_558, values_556[0]);

R.Call(v_524, R.Join(new V[] {(V)"UtilityCollected", R.Table(new Entry((V)"utility_kind", R.Get(v_530, (V)"utility")), new Entry((V)"floor_id", R.Get(v_528, (V)"floor_id")), new Entry((V)"x", R.Get(v_528, (V)"x")), new Entry((V)"y", R.Get(v_528, (V)"y")))}, new Results(R.Get(v_526, (V)"id"))));
}
if (R.Truth((V)(R.Get(v_528, (V)"kind") == (V)"bell"))) {
var values_559 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_521, (V)"idea_rooms"), (V)"groups"), R.Get(v_528, (V)"group"))));
V v_560 = values_559[0];
var values_561 = R.Join(new V[] {}, new Results((V)(R.Get(v_528, (V)"bell") == R.Get(R.Get(v_560, (V)"sequence"), (V)(R.Get(v_560, (V)"progress") + (V)1.0d)))));
V v_562 = values_561[0];
var values_563 = R.Join(new V[] {}, new Results(R.Or(R.And(v_562, () => (V)(R.Get(v_560, (V)"progress") + (V)1.0d)), () => (V)0.0d)));
var obj_564 = v_560;
V key_565 = (V)"progress";
R.Set(obj_564, key_565, values_563[0]);
R.Call(v_524, R.Join(new V[] {(V)"BellRung", R.Table(new Entry((V)"bell", R.Get(v_528, (V)"bell")), new Entry((V)"correct", v_562), new Entry((V)"progress", R.Get(v_560, (V)"progress")), new Entry((V)"floor_id", R.Get(v_528, (V)"floor_id")), new Entry((V)"x", R.Get(v_528, (V)"x")), new Entry((V)"y", R.Get(v_528, (V)"y")))}, new Results(R.Get(v_526, (V)"id"))));
if (R.Truth((V)(R.Get(v_560, (V)"progress") == (V)3.0d))) {
var values_566 = R.Join(new V[] {}, new Results((V)true));
var obj_567 = v_560;
V key_568 = (V)"completed";
R.Set(obj_567, key_568, values_566[0]);

var values_569 = R.Join(new V[] {}, new Results(R.Get(v_521, (V)"currency")));
V v_570 = values_569[0];

var values_571 = R.Join(new V[] {}, new Results((V)(v_570 + (V)5.0d)));
var obj_572 = v_521;
V key_573 = (V)"currency";
R.Set(obj_572, key_573, values_571[0]);
R.Call(v_524, R.Join(new V[] {(V)"CurrencyCollected", R.Table(new Entry((V)"amount", (V)5.0d), new Entry((V)"old_currency", v_570), new Entry((V)"new_currency", R.Get(v_521, (V)"currency")), new Entry((V)"floor_id", R.Get(v_528, (V)"floor_id")), new Entry((V)"x", R.Get(v_528, (V)"x")), new Entry((V)"y", R.Get(v_528, (V)"y")))}, new Results(R.Get(v_526, (V)"id"))));
}
} else if (R.Truth((V)(R.Get(v_528, (V)"kind") == (V)"falling_fixture"))) {
var values_574 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"enqueue"), R.Join(new V[] {v_521}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"actor_id", (V)0.0d), new Entry((V)"due", (V)(R.Get(v_521, (V)"clock") + R.Get(v_16, (V)"fixture_delay"))), new Entry((V)"attack_id", v_523), new Entry((V)"floor_id", R.Get(v_528, (V)"floor_id")))))));
V v_575 = values_574[0];
var values_576 = R.Join(new V[] {}, new Results(R.Get(v_575, (V)"id")));
var obj_577 = v_575;
V key_578 = (V)"effect_id";
R.Set(obj_577, key_578, values_576[0]);

var values_579 = R.Join(new V[] {}, new Results(R.Get(v_528, (V)"id")));
var obj_580 = v_575;
V key_581 = (V)"idea_fixture";
R.Set(obj_580, key_581, values_579[0]);
var values_582 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schedule_id", R.Get(v_575, (V)"id")), new Entry((V)"due", R.Get(v_575, (V)"due")), new Entry((V)"action_id", v_523), new Entry((V)"cells", R.Call(v_21, R.Join(new V[] {}, new Results(R.Get(v_528, (V)"cells")))).First), new Entry((V)"owner_id", R.Get(v_526, (V)"id")))));
var obj_583 = v_528;
V key_584 = (V)"pending";
R.Set(obj_583, key_584, values_582[0]);
R.Call(v_524, R.Join(new V[] {(V)"FixtureLocked", R.Table(new Entry((V)"target_cells", R.Get(v_528, (V)"cells")), new Entry((V)"due", R.Get(v_575, (V)"due")), new Entry((V)"damage", R.Get(v_16, (V)"fixture_damage")), new Entry((V)"floor_id", R.Get(v_528, (V)"floor_id")), new Entry((V)"x", R.Get(v_528, (V)"center_x")), new Entry((V)"y", R.Get(v_528, (V)"center_y")))}, new Results(R.Get(v_526, (V)"id"))));
var values_585 = R.Join(new V[] {}, new Results((V)true));
var obj_586 = v_528;
V key_587 = (V)"spent";
R.Set(obj_586, key_587, values_585[0]);
} else if (R.Truth((V)(R.Get(v_528, (V)"kind") == (V)"archive_contract"))) {
var values_588 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_530, (V)"id") == (V)"accept"), () => (V)"active"), () => (V)"failed")));
var obj_589 = v_528;
V key_590 = (V)"status";
R.Set(obj_589, key_590, values_588[0]);

var values_591 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_530, (V)"id") == (V)"abandon"), () => (V)"abandoned"), () => V.Nil)));
var obj_592 = v_528;
V key_593 = (V)"failure";
R.Set(obj_592, key_593, values_591[0]);
R.Call(v_524, R.Join(new V[] {(V)"ContractChanged", R.Table(new Entry((V)"status", R.Get(v_528, (V)"status")), new Entry((V)"floor_id", R.Get(v_528, (V)"floor_id")), new Entry((V)"x", R.Get(v_528, (V)"x")), new Entry((V)"y", R.Get(v_528, (V)"y")))}, new Results(R.Get(v_526, (V)"id"))));
} else {
var values_594 = R.Join(new V[] {}, new Results((V)true));
var obj_595 = v_528;
V key_596 = (V)"spent";
R.Set(obj_595, key_596, values_594[0]);
}
R.Call(v_524, R.Join(new V[] {(V)"InteractionUsed", R.Table(new Entry((V)"interaction", R.Get(v_528, (V)"kind")), new Entry((V)"node_id", R.Get(v_528, (V)"id")), new Entry((V)"choice_id", R.Get(v_530, (V)"id")), new Entry((V)"floor_id", R.Get(v_528, (V)"floor_id")), new Entry((V)"x", R.Get(v_528, (V)"x")), new Entry((V)"y", R.Get(v_528, (V)"y")))}, new Results(R.Get(v_526, (V)"id"))));
return Results.Empty;
})));
R.Set(v_18, (V)"nearby", new V((RuleFunction)(args_597 => {
V v_598 = args_597[0];
var values_599 = R.Join(new V[] {}, new Results(R.Table()));
V v_600 = values_599[0];
foreach (var row_601 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_598, (V)"idea_rooms"), () => R.Get(R.Get(v_598, (V)"idea_rooms"), (V)"nodes")), () => R.Table()))))))) {
V v_602 = row_601[0];
V v_603 = row_601[1];
if (R.Truth(R.Call(v_338, R.Join(new V[] {v_598}, new Results(v_603))).First)) {
var values_604 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"node_id", R.Get(v_603, (V)"id")), new Entry((V)"title", R.Get(R.Get(v_16, (V)"names"), R.Get(v_603, (V)"kind"))), new Entry((V)"kind", R.Get(v_603, (V)"kind")), new Entry((V)"x", R.Get(v_603, (V)"x")), new Entry((V)"y", R.Get(v_603, (V)"y")), new Entry((V)"spent", R.Get(v_603, (V)"spent")))));
var obj_605 = v_600;
V key_606 = (V)((V)R.Length(v_600) + (V)1.0d);
R.Set(obj_605, key_606, values_604[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_600}, new Results(new V((RuleFunction)(args_607 => {
V v_608 = args_607[0];
V v_609 = args_607[1];
if (R.Truth((V)(R.Get(v_608, (V)"spent") != R.Get(v_609, (V)"spent")))) {
return R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_608, (V)"spent")))));
}
var values_610 = R.Join(new V[] {R.Call(v_52, R.Join(new V[] {R.Get(R.Call(v_39, R.Join(new V[] {}, new Results(v_598))).First, (V)"position")}, new Results(v_608))).First}, R.Call(v_52, R.Join(new V[] {R.Get(R.Call(v_39, R.Join(new V[] {}, new Results(v_598))).First, (V)"position")}, new Results(v_609))));
V v_611 = values_610[0];
V v_612 = values_610[1];
if (R.Truth((V)(v_611 != v_612))) {
return R.Join(new V[] {}, new Results((V)(v_611 < v_612)));
}

return R.Join(new V[] {}, new Results((V)(R.Get(v_608, (V)"node_id") < R.Get(v_609, (V)"node_id"))));
return Results.Empty;
})))));
return R.Join(new V[] {}, new Results(v_600));
return Results.Empty;
})));
R.Set(v_18, (V)"utility_options", new V((RuleFunction)(args_613 => {
V v_614 = args_613[0];
var values_615 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {}, new Results(v_614))));
V v_616 = values_615[0];

var values_617 = R.Join(new V[] {}, new Results(R.Get(v_616, (V)"utility")));
V v_618 = values_617[0];
if (R.Truth((V)(!R.Truth(v_618)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"title", (V)"UTILITY SLOT"), new Entry((V)"detail", (V)"Empty. Find a supply node or trader."), new Entry((V)"entries", R.Table()))));
}
var values_619 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.And((V)(R.Get(v_618, (V)"kind") == (V)"cleanse_flask"), () => (V)"Remove Burning. One paid action; not a heal."), () => R.And((V)(R.Get(v_618, (V)"kind") == (V)"escape_scroll"), () => (V)"Choose a visible, clear cardinal path of 1\u00e2\u0080\u00933 tiles. One paid action.")), () => R.And(R.Get(v_618, (V)"planted"), () => (V)"One paid return to the visible empty marker. Leaving the room forfeits it.")), () => (V)"Plant at your room cell, then return once. Two paid actions; leaving that room forfeits the marker.")));
V v_620 = values_619[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"title", R.Get(R.Get(v_16, (V)"names"), R.Get(v_618, (V)"kind"))), new Entry((V)"detail", v_620), new Entry((V)"entries", R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)"use"), new Entry((V)"label", R.Or(R.And((V)(R.Get(v_618, (V)"kind") == (V)"return_token"), () => R.Or(R.And(R.Get(v_618, (V)"planted"), () => (V)"Return to token"), () => (V)"Plant return token")), () => R.Concat((V)"Use ", R.Get(R.Get(v_16, (V)"names"), R.Get(v_618, (V)"kind"))))), new Entry((V)"enabled", (V)true))))))));
return Results.Empty;
})));
R.Set(v_18, (V)"utility_preview", new V((RuleFunction)(args_621 => {
V v_622 = args_621[0];
V v_623 = args_621[1];
var values_624 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {}, new Results(v_622))));
V v_625 = values_624[0];

var values_626 = R.Join(new V[] {}, new Results(R.Get(v_625, (V)"utility")));
V v_627 = values_626[0];

var values_628 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_622))));
V v_629 = values_628[0];
if (R.Truth((V)(!R.Truth(v_627)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"utility slot is empty"), new Entry((V)"cells", R.Table()))));
}
var values_630 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)true), new Entry((V)"cells", R.Table()), new Entry((V)"utility_kind", R.Get(v_627, (V)"kind")))));
V v_631 = values_630[0];
if (R.Truth((V)(R.Get(v_627, (V)"kind") == (V)"cleanse_flask"))) {
if (R.Truth((V)(!R.Truth(R.And(R.Get(v_625, (V)"statuses"), () => R.Get(R.Get(v_625, (V)"statuses"), (V)"burning")))))) {
var values_632 = R.Join(new V[] {}, new Results((V)false));
var obj_633 = v_631;
V key_634 = (V)"valid";
R.Set(obj_633, key_634, values_632[0]);

var values_635 = R.Join(new V[] {}, new Results((V)"not Burning; charge retained"));
var obj_636 = v_631;
V key_637 = (V)"reason";
R.Set(obj_636, key_637, values_635[0]);
}
} else if (R.Truth((V)(R.Get(v_627, (V)"kind") == (V)"escape_scroll"))) {
var values_638 = R.Join(new V[] {R.Get(v_623, (V)"dx"), R.Get(v_623, (V)"dy")}, new Results(R.Or(R.Get(v_623, (V)"steps"), () => (V)3.0d)));
V v_639 = values_638[0];
V v_640 = values_638[1];
V v_641 = values_638[2];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_639))).First != (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_640))).First != (V)"number")), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_639))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_640))).First) != (V)1.0d)), () => (V)((V)(v_639 % (V)1.0d) != (V)0.0d)), () => (V)((V)(v_640 % (V)1.0d) != (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_641))).First != (V)"number")), () => (V)((V)(v_641 % (V)1.0d) != (V)0.0d)), () => (V)(v_641 < (V)1.0d)), () => (V)(v_641 > (V)3.0d)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"choose cardinal direction and 1\u00e2\u0080\u00933 tiles"), new Entry((V)"cells", R.Table()))));
}
{ double end_644 = R.Num(v_641), step_645 = R.Num((V)1.0d);
for (double i_643 = R.Num((V)1.0d); step_645 > 0 ? i_643 <= end_644 : i_643 >= end_644; i_643 += step_645) {
V v_642 = i_643;
var values_646 = R.Join(new V[] {(V)(R.Get(R.Get(v_625, (V)"position"), (V)"x") + (V)(v_639 * v_642))}, new Results((V)(R.Get(R.Get(v_625, (V)"position"), (V)"y") + (V)(v_640 * v_642))));
V v_647 = values_646[0];
V v_648 = values_646[1];
var values_649 = R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_629, v_647}, new Results(v_648))).First, () => R.Call(R.Get(v_14, (V)"can_see"), R.Join(new V[] {v_622, R.Get(v_629, (V)"floor_id"), v_647}, new Results(v_648))).First), () => (V)(!R.Truth(R.Call(v_68, R.Join(new V[] {v_622, R.Get(v_629, (V)"floor_id"), v_647}, new Results(v_648))).First))), () => (V)(!R.Truth(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))).First, (V)"portal"), R.Join(new V[] {v_622, v_647}, new Results(v_648))).First)))));
V v_650 = values_649[0];
var values_651 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_647), new Entry((V)"y", v_648), new Entry((V)"status", R.Or(R.And(v_650, () => (V)"open"), () => (V)"blocked")))));
var obj_652 = R.Get(v_631, (V)"cells");
V key_653 = (V)((V)R.Length(R.Get(v_631, (V)"cells")) + (V)1.0d);
R.Set(obj_652, key_653, values_651[0]);
if (R.Truth((V)(!R.Truth(v_650)))) {
var values_654 = R.Join(new V[] {}, new Results((V)false));
var obj_655 = v_631;
V key_656 = (V)"valid";
R.Set(obj_655, key_656, values_654[0]);

var values_657 = R.Join(new V[] {}, new Results((V)"path needs visible clear cells, without stairs"));
var obj_658 = v_631;
V key_659 = (V)"reason";
R.Set(obj_658, key_659, values_657[0]);
}
}
}
var values_660 = R.Join(new V[] {}, new Results(v_641));
var obj_661 = v_631;
V key_662 = (V)"distance";
R.Set(obj_661, key_662, values_660[0]);

var values_663 = R.Join(new V[] {}, new Results((V)true));
var obj_664 = v_631;
V key_665 = (V)"utility_move";
R.Set(obj_664, key_665, values_663[0]);
} else if (R.Truth((V)(!R.Truth(R.Get(v_627, (V)"planted"))))) {
var values_666 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"room_at"), R.Join(new V[] {v_629, R.Get(R.Get(v_625, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_625, (V)"position"), (V)"y")))));
V v_667 = values_666[0];
if (R.Truth((V)(!R.Truth(v_667)))) {
var values_668 = R.Join(new V[] {}, new Results((V)false));
var obj_669 = v_631;
V key_670 = (V)"valid";
R.Set(obj_669, key_670, values_668[0]);

var values_671 = R.Join(new V[] {}, new Results((V)"plant inside a room"));
var obj_672 = v_631;
V key_673 = (V)"reason";
R.Set(obj_672, key_673, values_671[0]);
} else {
var values_674 = R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {}, new Results(v_667))));
var obj_675 = v_631;
V key_676 = (V)"plant_room";
R.Set(obj_675, key_676, values_674[0]);
}
} else {
var values_677 = R.Join(new V[] {}, new Results(R.Get(v_627, (V)"planted")));
V v_678 = values_677[0];
if (R.Truth(R.Or(R.Or(R.Or((V)(R.Get(v_678, (V)"floor_id") != R.Get(v_629, (V)"floor_id")), () => (V)(!R.Truth(R.Call(v_63, R.Join(new V[] {R.Get(v_678, (V)"room"), R.Get(R.Get(v_625, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_625, (V)"position"), (V)"y")))).First))), () => (V)(!R.Truth(R.Call(R.Get(v_14, (V)"can_see"), R.Join(new V[] {v_622, R.Get(v_629, (V)"floor_id"), R.Get(v_678, (V)"x")}, new Results(R.Get(v_678, (V)"y")))).First))), () => R.Call(v_68, R.Join(new V[] {v_622, R.Get(v_629, (V)"floor_id"), R.Get(v_678, (V)"x")}, new Results(R.Get(v_678, (V)"y")))).First))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"marker must be visible, empty and in this room"), new Entry((V)"cells", R.Table()))));
}
var values_679 = R.Join(new V[] {R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_625, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_625, (V)"position"), (V)"y")), new Entry((V)"d", (V)0.0d))))}, new Results(R.Table(new Entry(R.Call(v_48, R.Join(new V[] {R.Get(R.Get(v_625, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_625, (V)"position"), (V)"y")))).First, (V)true))));
V v_680 = values_679[0];
V v_681 = values_679[1];

var values_682 = R.Join(new V[] {}, new Results((V)1.0d));
V v_683 = values_682[0];

var values_684 = Results.Empty;
V v_685 = values_684[0];
while (R.Truth((V)(v_683 <= (V)R.Length(v_680)))) {
var values_686 = R.Join(new V[] {}, new Results(R.Get(v_680, v_683)));
V v_687 = values_686[0];

var values_688 = R.Join(new V[] {}, new Results((V)(v_683 + (V)1.0d)));
v_683 = values_688[0];
if (R.Truth(R.And((V)(R.Get(v_687, (V)"x") == R.Get(v_678, (V)"x")), () => (V)(R.Get(v_687, (V)"y") == R.Get(v_678, (V)"y"))))) {
var values_689 = R.Join(new V[] {}, new Results(v_687));
v_685 = values_689[0];

break;
}
if (R.Truth((V)(R.Get(v_687, (V)"d") < R.Get(v_16, (V)"return_range")))) {
foreach (var row_690 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_20)))))) {
V v_691 = row_690[0];
V v_692 = row_690[1];
var values_693 = R.Join(new V[] {(V)(R.Get(v_687, (V)"x") + R.Get(v_692, (V)1.0d))}, new Results((V)(R.Get(v_687, (V)"y") + R.Get(v_692, (V)2.0d))));
V v_694 = values_693[0];
V v_695 = values_693[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_681, R.Call(v_48, R.Join(new V[] {v_694}, new Results(v_695))).First))), () => R.Call(v_63, R.Join(new V[] {R.Get(v_678, (V)"room"), v_694}, new Results(v_695))).First), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_629, v_694}, new Results(v_695))).First), () => R.Call(R.Get(v_14, (V)"was_explored"), R.Join(new V[] {v_622, R.Get(v_629, (V)"floor_id"), v_694}, new Results(v_695))).First), () => (V)(!R.Truth(R.Call(v_68, R.Join(new V[] {v_622, R.Get(v_629, (V)"floor_id"), v_694}, new Results(v_695))).First))), () => (V)(!R.Truth(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))).First, (V)"portal"), R.Join(new V[] {v_622, v_694}, new Results(v_695))).First))))) {
var values_696 = R.Join(new V[] {}, new Results((V)true));
var obj_697 = v_681;
V key_698 = R.Call(v_48, R.Join(new V[] {v_694}, new Results(v_695))).First;
R.Set(obj_697, key_698, values_696[0]);

var values_699 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_694), new Entry((V)"y", v_695), new Entry((V)"d", (V)(R.Get(v_687, (V)"d") + (V)1.0d)))));
var obj_700 = v_680;
V key_701 = (V)((V)R.Length(v_680) + (V)1.0d);
R.Set(obj_700, key_701, values_699[0]);
}
}
}
}
if (R.Truth(R.Or((V)(!R.Truth(v_685)), () => (V)(R.Get(v_685, (V)"d") == (V)0.0d)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"valid", (V)false), new Entry((V)"reason", (V)"marker needs a known clear path of 1\u00e2\u0080\u00934 steps"), new Entry((V)"cells", R.Table()))));
}
var values_702 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_678, (V)"x")), new Entry((V)"y", R.Get(v_678, (V)"y")), new Entry((V)"status", (V)"open"))))));
var obj_703 = v_631;
V key_704 = (V)"cells";
R.Set(obj_703, key_704, values_702[0]);

var values_705 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_706 = v_631;
V key_707 = (V)"distance";
R.Set(obj_706, key_707, values_705[0]);

var values_708 = R.Join(new V[] {}, new Results(R.Get(v_685, (V)"d")));
var obj_709 = v_631;
V key_710 = (V)"return_distance";
R.Set(obj_709, key_710, values_708[0]);

var values_711 = R.Join(new V[] {}, new Results((V)true));
var obj_712 = v_631;
V key_713 = (V)"utility_move";
R.Set(obj_712, key_713, values_711[0]);

var values_714 = R.Join(new V[] {}, new Results((V)true));
var obj_715 = v_631;
V key_716 = (V)"utility_return";
R.Set(obj_715, key_716, values_714[0]);
}
return R.Join(new V[] {}, new Results(v_631));
return Results.Empty;
})));
R.Set(v_18, (V)"use_utility", new V((RuleFunction)(args_717 => {
V v_718 = args_717[0];
V v_719 = args_717[1];
V v_720 = args_717[2];
var values_721 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {}, new Results(v_718))));
V v_722 = values_721[0];

var values_723 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_722, (V)"utility")))));
V v_724 = values_723[0];
if (R.Truth(R.And((V)(R.Get(v_724, (V)"kind") == (V)"return_token"), () => (V)(!R.Truth(R.Get(v_724, (V)"planted")))))) {
var values_725 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_722, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_722, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_722, (V)"position"), (V)"y")), new Entry((V)"room", R.Call(v_21, R.Join(new V[] {}, new Results(R.Get(v_719, (V)"plant_room")))).First))));
var obj_726 = v_724;
V key_727 = (V)"planted";
R.Set(obj_726, key_727, values_725[0]);
R.Call(v_720, R.Join(new V[] {(V)"ReturnTokenPlanted", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_722, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_722, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_722, (V)"position"), (V)"y")))}, new Results(R.Get(v_722, (V)"id"))));
} else {
if (R.Truth((V)(R.Get(v_724, (V)"kind") == (V)"cleanse_flask"))) {
R.Call(v_493, R.Join(new V[] {v_718, v_722}, new Results(v_720)));
}
var values_728 = R.Join(new V[] {}, new Results(V.Nil));
var obj_729 = v_722;
V key_730 = (V)"utility";
R.Set(obj_729, key_730, values_728[0]);

R.Call(v_720, R.Join(new V[] {(V)"UtilityUsed", R.Table(new Entry((V)"utility_kind", R.Get(v_724, (V)"kind")), new Entry((V)"floor_id", R.Get(R.Get(v_722, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_722, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_722, (V)"position"), (V)"y")))}, new Results(R.Get(v_722, (V)"id"))));
}
return Results.Empty;
})));
V v_731 = V.Nil;
v_731 = new V((RuleFunction)(args_732 => {
V v_733 = args_732[0];
V v_734 = args_732[1];
V v_735 = args_732[2];
V v_736 = args_732[3];
var values_737 = R.Join(new V[] {}, new Results((V)"failed"));
var obj_738 = v_733;
V key_739 = (V)"status";
R.Set(obj_738, key_739, values_737[0]);

var values_740 = R.Join(new V[] {}, new Results(v_734));
var obj_741 = v_733;
V key_742 = (V)"failure";
R.Set(obj_741, key_742, values_740[0]);
R.Call(v_735, R.Join(new V[] {(V)"ContractChanged", R.Table(new Entry((V)"status", (V)"failed"), new Entry((V)"reason", v_734), new Entry((V)"floor_id", R.Get(v_733, (V)"floor_id")), new Entry((V)"x", R.Get(v_733, (V)"x")), new Entry((V)"y", R.Get(v_733, (V)"y")))}, new Results(v_736)));
return Results.Empty;
}));
R.Set(v_18, (V)"before_action", new V((RuleFunction)(args_743 => {
V v_744 = args_743[0];
V v_745 = args_743[1];
V v_746 = args_743[2];
V v_747 = args_743[3];
if (R.Truth(R.Or(R.And((V)(v_745 == (V)"dash"), () => (V)(R.Or(R.Get(v_746, (V)"distance"), () => (V)0.0d) > (V)0.0d)), () => R.And((V)(v_745 == (V)"use_utility"), () => R.Get(v_746, (V)"utility_move"))))) {
foreach (var row_748 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_744, (V)"idea_rooms"), () => R.Get(R.Get(v_744, (V)"idea_rooms"), (V)"nodes")), () => R.Table()))))))) {
V v_749 = row_748[0];
V v_750 = row_748[1];
if (R.Truth(R.And((V)(R.Get(v_750, (V)"kind") == (V)"archive_contract"), () => (V)(R.Get(v_750, (V)"status") == (V)"active")))) {
R.Call(v_731, R.Join(new V[] {v_750, (V)"movement ability used", v_747}, new Results(R.Get(v_744, (V)"player_id"))));
}
}
}
return Results.Empty;
})));
R.Set(v_18, (V)"on_event", new V((RuleFunction)(args_751 => {
V v_752 = args_751[0];
V v_753 = args_751[1];
V v_754 = args_751[2];
var values_755 = R.Join(new V[] {}, new Results(R.Get(v_752, (V)"idea_rooms")));
V v_756 = values_755[0];

if (R.Truth((V)(!R.Truth(v_756)))) {
return Results.Empty;
}
var values_757 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {}, new Results(v_752))));
V v_758 = values_757[0];
var values_759 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Or((V)(R.Get(v_753, (V)"kind") == (V)"TileEntered"), () => (V)(R.Get(v_753, (V)"kind") == (V)"FloorChanged")), () => (V)(R.Get(v_753, (V)"source_id") == R.Get(v_758, (V)"id"))), () => R.And((V)(R.Get(v_753, (V)"kind") == (V)"Displaced"), () => (V)(R.Get(v_753, (V)"target_id") == R.Get(v_758, (V)"id"))))));
V v_760 = values_759[0];
if (R.Truth(v_760)) {
var values_761 = R.Join(new V[] {}, new Results(R.And(R.Get(v_758, (V)"utility"), () => R.Get(R.Get(v_758, (V)"utility"), (V)"planted"))));
V v_762 = values_761[0];
if (R.Truth(R.And(v_762, () => R.Or((V)(R.Get(v_753, (V)"floor_id") != R.Get(v_762, (V)"floor_id")), () => (V)(!R.Truth(R.Call(v_63, R.Join(new V[] {R.Get(v_762, (V)"room"), R.Get(v_753, (V)"x")}, new Results(R.Get(v_753, (V)"y")))).First)))))) {
var values_763 = R.Join(new V[] {}, new Results(V.Nil));
var obj_764 = v_758;
V key_765 = (V)"utility";
R.Set(obj_764, key_765, values_763[0]);

R.Call(v_754, R.Join(new V[] {(V)"ReturnTokenExpired", R.Table(new Entry((V)"floor_id", R.Get(v_753, (V)"floor_id")), new Entry((V)"x", R.Get(v_753, (V)"x")), new Entry((V)"y", R.Get(v_753, (V)"y")))}, new Results(R.Get(v_758, (V)"id"))));
}
}
foreach (var row_766 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_756, (V)"nodes"))))))) {
V v_767 = row_766[0];
V v_768 = row_766[1];
if (R.Truth(R.And((V)(R.Get(v_768, (V)"kind") == (V)"archive_contract"), () => (V)(R.Get(v_768, (V)"status") == (V)"active")))) {
if (R.Truth(R.And(v_760, () => R.Or((V)(R.Get(v_753, (V)"floor_id") != R.Get(v_768, (V)"floor_id")), () => (V)(!R.Truth(R.Call(v_63, R.Join(new V[] {R.Get(v_768, (V)"room"), R.Get(v_753, (V)"x")}, new Results(R.Get(v_753, (V)"y")))).First)))))) {
R.Call(v_731, R.Join(new V[] {v_768, (V)"left the room", v_754}, new Results(R.Get(v_758, (V)"id"))));
} else if (R.Truth((V)(R.Get(v_753, (V)"kind") == (V)"EntityKilled"))) {
var values_769 = R.Join(new V[] {}, new Results(R.Get(v_753, (V)"target_id")));
V v_770 = values_769[0];
foreach (var row_771 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_768, (V)"targets"))))))) {
V v_772 = row_771[0];
V v_773 = row_771[1];
if (R.Truth(R.Or((V)(v_770 == v_773), () => (V)(R.Get(v_753, (V)"revival_of") == v_773)))) {
if (R.Truth((V)(R.Get(v_753, (V)"source_faction") != (V)"player"))) {
R.Call(v_731, R.Join(new V[] {v_768, (V)"target defeated by another faction", v_754}, new Results(R.Get(v_758, (V)"id"))));
} else {
var values_774 = R.Join(new V[] {}, new Results((V)true));
var obj_775 = R.Get(v_768, (V)"defeated");
V key_776 = R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_773))).First;
R.Set(obj_775, key_776, values_774[0]);
}
}
}
}
}
}
return Results.Empty;
})));
R.Set(v_18, (V)"after_action", new V((RuleFunction)(args_777 => {
V v_778 = args_777[0];
V v_779 = args_777[1];
var values_780 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {}, new Results(v_778))));
V v_781 = values_780[0];
if (R.Truth((V)(R.Or(R.Get(v_781, (V)"supply_focus_actions"), () => (V)0.0d) > (V)0.0d))) {
var values_782 = R.Join(new V[] {}, new Results((V)(R.Get(v_781, (V)"supply_focus_actions") - (V)1.0d)));
var obj_783 = v_781;
V key_784 = (V)"supply_focus_actions";
R.Set(obj_783, key_784, values_782[0]);
}
if (R.Truth((V)(R.Get(v_781, (V)"hp") <= (V)0.0d))) {
return Results.Empty;
}
foreach (var row_785 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_778, (V)"idea_rooms"), () => R.Get(R.Get(v_778, (V)"idea_rooms"), (V)"nodes")), () => R.Table()))))))) {
V v_786 = row_785[0];
V v_787 = row_785[1];
if (R.Truth(R.And((V)(R.Get(v_787, (V)"kind") == (V)"archive_contract"), () => (V)(R.Get(v_787, (V)"status") == (V)"active")))) {
var values_788 = R.Join(new V[] {}, new Results((V)true));
V v_789 = values_788[0];
foreach (var row_790 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_787, (V)"targets"))))))) {
V v_791 = row_790[0];
V v_792 = row_790[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Get(R.Get(v_787, (V)"defeated"), R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_792))).First))), () => R.Call(v_32, R.Join(new V[] {v_778}, new Results(v_792))).First))) {
var values_793 = R.Join(new V[] {}, new Results((V)false));
v_789 = values_793[0];
}
foreach (var row_794 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_778, (V)"idea_corpses"), () => R.Table()))))))) {
V v_795 = row_794[0];
V v_796 = row_794[1];
if (R.Truth((V)(R.Get(v_796, (V)"id") == v_792))) {
var values_797 = R.Join(new V[] {}, new Results((V)false));
v_789 = values_797[0];
}
}
foreach (var row_798 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_778, (V)"entities"))))))) {
V v_799 = row_798[0];
V v_800 = row_798[1];
if (R.Truth((V)(R.Get(v_800, (V)"revival_of") == v_792))) {
var values_801 = R.Join(new V[] {}, new Results((V)false));
v_789 = values_801[0];
}
}
}
if (R.Truth(v_789)) {
var values_802 = R.Join(new V[] {}, new Results((V)"completed"));
var obj_803 = v_787;
V key_804 = (V)"status";
R.Set(obj_803, key_804, values_802[0]);

var values_805 = R.Join(new V[] {}, new Results((V)true));
var obj_806 = v_787;
V key_807 = (V)"spent";
R.Set(obj_806, key_807, values_805[0]);

var values_808 = R.Join(new V[] {}, new Results(R.Get(v_778, (V)"currency")));
V v_809 = values_808[0];

var values_810 = R.Join(new V[] {}, new Results((V)(v_809 + (V)8.0d)));
var obj_811 = v_778;
V key_812 = (V)"currency";
R.Set(obj_811, key_812, values_810[0]);
R.Call(v_779, R.Join(new V[] {(V)"ContractChanged", R.Table(new Entry((V)"status", (V)"completed"), new Entry((V)"reward", (V)8.0d), new Entry((V)"floor_id", R.Get(v_787, (V)"floor_id")), new Entry((V)"x", R.Get(v_787, (V)"x")), new Entry((V)"y", R.Get(v_787, (V)"y")))}, new Results(R.Get(v_781, (V)"id"))));
R.Call(v_779, R.Join(new V[] {(V)"CurrencyCollected", R.Table(new Entry((V)"amount", (V)8.0d), new Entry((V)"old_currency", v_809), new Entry((V)"new_currency", R.Get(v_778, (V)"currency")), new Entry((V)"floor_id", R.Get(v_787, (V)"floor_id")), new Entry((V)"x", R.Get(v_787, (V)"x")), new Entry((V)"y", R.Get(v_787, (V)"y")))}, new Results(R.Get(v_781, (V)"id"))));
}
}
}
return Results.Empty;
})));
R.Set(v_18, (V)"resolve", new V((RuleFunction)(args_813 => {
V v_814 = args_813[0];
V v_815 = args_813[1];
V v_816 = args_813[2];
var values_817 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_18, (V)"node"), R.Join(new V[] {v_814}, new Results(R.Get(v_815, (V)"idea_fixture")))).First}, new Results((V)"orphan fixture"))));
V v_818 = values_817[0];
var values_819 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_818, (V)"pending")}, new Results((V)"missing fixture commitment"))));
V v_820 = values_819[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_820, (V)"schedule_id") == R.Get(v_815, (V)"id")), () => (V)(R.Get(v_820, (V)"due") == R.Get(v_814, (V)"clock"))), () => (V)(R.Get(v_818, (V)"floor_id") == R.Get(v_814, (V)"active_floor_id")))}, new Results((V)"mistimed fixture")));
var values_821 = R.Join(new V[] {}, new Results(V.Nil));
var obj_822 = v_818;
V key_823 = (V)"pending";
R.Set(obj_822, key_823, values_821[0]);

R.Call(v_816, R.Join(new V[] {v_818}, new Results(v_820)));
return Results.Empty;
})));
R.Set(v_18, (V)"focus", new V((RuleFunction)(args_824 => {
V v_825 = args_824[0];
if (R.Truth((V)(R.Or(R.Get(v_825, (V)"supply_focus_actions"), () => (V)0.0d) > (V)0.0d))) {
var values_826 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_827 = v_825;
V key_828 = (V)"supply_focus_actions";
R.Set(obj_827, key_828, values_826[0]);

return R.Join(new V[] {}, new Results((V)2.0d));
}
return R.Join(new V[] {}, new Results((V)0.0d));
return Results.Empty;
})));
R.Set(v_18, (V)"focus_bonus", new V((RuleFunction)(args_829 => {
V v_830 = args_829[0];
return R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Or(R.Get(v_830, (V)"supply_focus_actions"), () => (V)0.0d) > (V)0.0d), () => (V)2.0d), () => (V)0.0d)));
return Results.Empty;
})));
R.Set(v_18, (V)"status", new V((RuleFunction)(args_831 => {
V v_832 = args_831[0];
var values_833 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {}, new Results(v_832))));
V v_834 = values_833[0];

var values_835 = R.Join(new V[] {}, new Results(R.Table()));
V v_836 = values_835[0];
if (R.Truth(R.Get(v_834, (V)"utility"))) {
var values_837 = R.Join(new V[] {}, new Results(R.Concat(R.Get(R.Get(v_16, (V)"names"), R.Get(R.Get(v_834, (V)"utility"), (V)"kind")), R.Or(R.And(R.Get(R.Get(v_834, (V)"utility"), (V)"planted"), () => (V)" (planted)"), () => (V)""))));
var obj_838 = v_836;
V key_839 = (V)((V)R.Length(v_836) + (V)1.0d);
R.Set(obj_838, key_839, values_837[0]);
}
if (R.Truth((V)(R.Or(R.Get(v_834, (V)"supply_focus_actions"), () => (V)0.0d) > (V)0.0d))) {
var values_840 = R.Join(new V[] {}, new Results(R.Concat((V)"Focus ", R.Concat(R.Get(v_834, (V)"supply_focus_actions"), (V)" actions"))));
var obj_841 = v_836;
V key_842 = (V)((V)R.Length(v_836) + (V)1.0d);
R.Set(obj_841, key_842, values_840[0]);
}
if (R.Truth((V)(R.Or(R.Get(v_834, (V)"pickup_ward_actions"), () => (V)0.0d) > (V)0.0d))) {
var values_843 = R.Join(new V[] {}, new Results(R.Concat((V)"Ward ", R.Concat(R.Get(v_834, (V)"pickup_ward_actions"), (V)" actions"))));
var obj_844 = v_836;
V key_845 = (V)((V)R.Length(v_836) + (V)1.0d);
R.Set(obj_844, key_845, values_843[0]);
}
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_836}, new Results((V)" \u00c2\u00b7 "))));
return Results.Empty;
})));
R.Set(v_18, (V)"marked", new V((RuleFunction)(args_846 => {
V v_847 = args_846[0];
V v_848 = args_846[1];
foreach (var row_849 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_847, (V)"idea_rooms"), () => R.Get(R.Get(v_847, (V)"idea_rooms"), (V)"nodes")), () => R.Table()))))))) {
V v_850 = row_849[0];
V v_851 = row_849[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_851, (V)"kind") == (V)"archive_contract"), () => (V)(R.Get(v_851, (V)"status") == (V)"active")), () => (V)(R.Get(v_851, (V)"floor_id") == R.Get(R.Get(v_848, (V)"position"), (V)"floor_id"))))) {
foreach (var row_852 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_851, (V)"targets"))))))) {
V v_853 = row_852[0];
V v_854 = row_852[1];
if (R.Truth(R.Or((V)(R.Get(v_848, (V)"id") == v_854), () => (V)(R.Get(v_848, (V)"revival_of") == v_854)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
V v_855 = V.Nil;
v_855 = new V((RuleFunction)(args_856 => {
V v_857 = args_856[0];
V v_858 = args_856[1];
V v_859 = args_856[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_857))).First == (V)"number"), () => (V)((V)(v_857 % (V)1.0d) == (V)0.0d)), () => (V)(v_857 >= v_858)), () => (V)(v_857 <= v_859))));
return Results.Empty;
}));
R.Set(v_18, (V)"validate", new V((RuleFunction)(args_860 => {
V v_861 = args_860[0];
V v_862 = args_860[1];
var values_863 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {}, new Results(v_861))));
V v_864 = values_863[0];

var values_865 = R.Join(new V[] {}, new Results(R.Get(v_864, (V)"utility")));
V v_866 = values_865[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_864, (V)"supply_focus_actions") == V.Nil), () => R.Call(v_855, R.Join(new V[] {R.Get(v_864, (V)"supply_focus_actions"), (V)0.0d}, new Results((V)3.0d))).First)}, new Results((V)"invalid supply focus")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_864, (V)"pickup_ward_actions") == V.Nil), () => R.Call(v_855, R.Join(new V[] {R.Get(v_864, (V)"pickup_ward_actions"), (V)0.0d}, new Results((V)3.0d))).First)}, new Results((V)"invalid supply ward")));
if (R.Truth(v_866)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_866))).First == (V)"table"), () => R.Or(R.Or((V)(R.Get(v_866, (V)"kind") == (V)"cleanse_flask"), () => (V)(R.Get(v_866, (V)"kind") == (V)"escape_scroll")), () => (V)(R.Get(v_866, (V)"kind") == (V)"return_token")))}, new Results((V)"invalid utility")));
if (R.Truth(R.Get(v_866, (V)"planted"))) {
var values_867 = R.Join(new V[] {}, new Results(R.Get(v_866, (V)"planted")));
V v_868 = values_867[0];

var values_869 = R.Join(new V[] {}, new Results(R.Get(v_862, R.Get(v_868, (V)"floor_id"))));
V v_870 = values_869[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_866, (V)"kind") == (V)"return_token"), () => v_870), () => (V)(R.Get(v_868, (V)"floor_id") == R.Get(R.Get(v_864, (V)"position"), (V)"floor_id"))), () => R.Call(v_855, R.Join(new V[] {R.Get(v_868, (V)"x"), (V)1.0d}, new Results(R.Get(v_870, (V)"width")))).First), () => R.Call(v_855, R.Join(new V[] {R.Get(v_868, (V)"y"), (V)1.0d}, new Results(R.Get(v_870, (V)"height")))).First), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_870, R.Get(v_868, (V)"x")}, new Results(R.Get(v_868, (V)"y")))).First), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_868, (V)"room")))).First == (V)"table")), () => R.Call(v_63, R.Join(new V[] {R.Get(v_868, (V)"room"), R.Get(v_868, (V)"x")}, new Results(R.Get(v_868, (V)"y")))).First), () => R.Call(v_63, R.Join(new V[] {R.Get(v_868, (V)"room"), R.Get(R.Get(v_864, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_864, (V)"position"), (V)"y")))).First)}, new Results((V)"invalid token marker")));
var values_871 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"room_at"), R.Join(new V[] {v_870, R.Get(v_868, (V)"x")}, new Results(R.Get(v_868, (V)"y")))));
V v_872 = values_871[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(v_872, () => (V)(R.Get(v_872, (V)"id") == R.Get(R.Get(v_868, (V)"room"), (V)"id"))), () => (V)(R.Get(v_872, (V)"x") == R.Get(R.Get(v_868, (V)"room"), (V)"x"))), () => (V)(R.Get(v_872, (V)"y") == R.Get(R.Get(v_868, (V)"room"), (V)"y"))), () => (V)(R.Get(v_872, (V)"width") == R.Get(R.Get(v_868, (V)"room"), (V)"width"))), () => (V)(R.Get(v_872, (V)"height") == R.Get(R.Get(v_868, (V)"room"), (V)"height")))}, new Results((V)"invalid token room")));
}
}
var values_873 = R.Join(new V[] {}, new Results(R.Get(v_861, (V)"idea_rooms")));
V v_874 = values_873[0];
if (R.Truth((V)(!R.Truth(v_874)))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(v_866))}, new Results((V)"utility without room state")));

return R.Join(new V[] {}, new Results((V)true));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_874))).First == (V)"table"), () => (V)(R.Get(v_874, (V)"version") == R.Get(v_16, (V)"version"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_874, (V)"nodes")))).First == (V)"table")), () => (V)((V)R.Length(R.Get(v_874, (V)"nodes")) <= (V)32.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_874, (V)"groups")))).First == (V)"table"))}, new Results((V)"invalid interaction store")));
var values_875 = R.Join(new V[] {}, new Results(R.Table()));
V v_876 = values_875[0];

foreach (var row_877 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_83, R.Join(new V[] {}, new Results(v_861)))))))) {
V v_878 = row_877[0];
V v_879 = row_877[1];
var values_880 = R.Join(new V[] {}, new Results((V)true));
var obj_881 = v_876;
V key_882 = v_879;
R.Set(obj_881, key_882, values_880[0]);
}
var values_883 = R.Join(new V[] {R.Table(), R.Table(), R.Table()}, new Results(R.Table()));
V v_884 = values_883[0];
V v_885 = values_883[1];
V v_886 = values_883[2];
V v_887 = values_883[3];
V v_888 = V.Nil;
v_888 = new V((RuleFunction)(args_889 => {
V v_890 = args_889[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_890))).First == (V)"table"), () => R.Get(v_876, R.Get(v_890, (V)"boon_id")))}, new Results((V)"offer outside frozen class pool")));
var values_891 = R.Join(new V[] {}, new Results((V)false));
V v_892 = values_891[0];

foreach (var row_893 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"rarities"))))))) {
V v_894 = row_893[0];
V v_895 = row_893[1];
if (R.Truth((V)(v_895 == R.Get(v_890, (V)"rarity")))) {
var values_896 = R.Join(new V[] {}, new Results((V)true));
v_892 = values_896[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_892}, new Results((V)"invalid interaction rarity")));
return Results.Empty;
}));
foreach (var row_897 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_874, (V)"nodes"))))))) {
V v_898 = row_897[0];
V v_899 = row_897[1];
var values_900 = R.Join(new V[] {}, new Results(R.Get(v_862, R.Get(v_899, (V)"floor_id"))));
V v_901 = values_900[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_901, () => R.Get(R.Get(v_16, (V)"names"), R.Get(v_899, (V)"kind"))), () => R.Call(v_855, R.Join(new V[] {R.Get(v_899, (V)"x"), (V)1.0d}, new Results(R.Get(v_901, (V)"width")))).First), () => R.Call(v_855, R.Join(new V[] {R.Get(v_899, (V)"y"), (V)1.0d}, new Results(R.Get(v_901, (V)"height")))).First), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_901, R.Get(v_899, (V)"x")}, new Results(R.Get(v_899, (V)"y")))).First), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"spent")))).First == (V)"boolean")), () => (V)(R.Get(v_899, (V)"id") == R.Concat(R.Get(v_899, (V)"floor_id"), R.Concat((V)":interaction:", R.Concat(R.Get(v_899, (V)"kind"), R.Concat((V)":", R.Call(v_48, R.Join(new V[] {R.Get(v_899, (V)"x")}, new Results(R.Get(v_899, (V)"y")))).First)))))), () => (V)(!R.Truth(R.Get(v_884, R.Get(v_899, (V)"id")))))}, new Results((V)"invalid interaction identity")));
var values_902 = R.Join(new V[] {}, new Results((V)true));
var obj_903 = v_884;
V key_904 = R.Get(v_899, (V)"id");
R.Set(obj_903, key_904, values_902[0]);
var values_905 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_899, (V)"floor_id"), R.Concat((V)":", R.Call(v_48, R.Join(new V[] {R.Get(v_899, (V)"x")}, new Results(R.Get(v_899, (V)"y")))).First))));
V v_906 = values_905[0];

R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_885, v_906)))}, new Results((V)"overlapping interactions")));

var values_907 = R.Join(new V[] {}, new Results((V)true));
var obj_908 = v_885;
V key_909 = v_906;
R.Set(obj_908, key_909, values_907[0]);
if (R.Truth((V)(R.Get(v_899, (V)"kind") == (V)"offering_font"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"offers")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_899, (V)"offers")) == (V)3.0d))}, new Results((V)"invalid font offers")));

var values_910 = R.Join(new V[] {}, new Results(R.Table()));
V v_911 = values_910[0];
foreach (var row_912 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"offers"))))))) {
V v_913 = row_912[0];
V v_914 = row_912[1];
R.Call(v_888, R.Join(new V[] {}, new Results(v_914)));

R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_911, R.Get(v_914, (V)"boon_id"))))}, new Results((V)"duplicate font offer")));

var values_915 = R.Join(new V[] {}, new Results((V)true));
var obj_916 = v_911;
V key_917 = R.Get(v_914, (V)"boon_id");
R.Set(obj_916, key_917, values_915[0]);
}
} else if (R.Truth((V)(R.Get(v_899, (V)"kind") == (V)"attunement_forge"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"trades")))).First == (V)"table")}, new Results((V)"missing forge alternatives")));
foreach (var row_918 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_876)))))) {
V v_919 = row_918[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_899, (V)"trades"), v_919)))).First == (V)"table")}, new Results((V)"missing forge pool")));
foreach (var row_920 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"rarities"))))))) {
V v_921 = row_920[0];
V v_922 = row_920[1];
var values_923 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_899, (V)"trades"), v_919), v_922)));
V v_924 = values_923[0];

R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_924))).First == (V)"table"), () => (V)((V)R.Length(v_924) == (V)3.0d))}, new Results((V)"invalid forge alternatives")));
var values_925 = R.Join(new V[] {}, new Results(R.Table()));
V v_926 = values_925[0];
foreach (var row_927 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_924)))))) {
V v_928 = row_927[0];
V v_929 = row_927[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Get(v_876, v_929), () => (V)(v_929 != v_919)), () => (V)(!R.Truth(R.Get(v_926, v_929))))}, new Results((V)"invalid forge offer")));

var values_930 = R.Join(new V[] {}, new Results((V)true));
var obj_931 = v_926;
V key_932 = v_929;
R.Set(obj_931, key_932, values_930[0]);
}
}
}
foreach (var row_933 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"trades"))))))) {
V v_934 = row_933[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_876, v_934)}, new Results((V)"forge offer outside frozen class pool")));
}
} else if (R.Truth((V)(R.Get(v_899, (V)"kind") == (V)"travelling_trader"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_899, (V)"stock") == (V)"cleanse_flask"), () => (V)(R.Get(v_899, (V)"stock") == (V)"escape_scroll"))}, new Results((V)"invalid trader stock")));
} else if (R.Truth(R.Or((V)(R.Get(v_899, (V)"kind") == (V)"bell"), () => (V)(R.Get(v_899, (V)"kind") == (V)"bell_clue")))) {
var values_935 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_874, (V)"groups"), R.Get(v_899, (V)"group"))));
V v_936 = values_935[0];

R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_936, () => (V)(R.Get(v_936, (V)"floor_id") == R.Get(v_899, (V)"floor_id")))}, new Results((V)"missing bell group")));
var values_937 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_887, R.Get(v_899, (V)"group")), () => R.Table(new Entry((V)"bells", R.Table()), new Entry((V)"clues", (V)0.0d)))));
var obj_938 = v_887;
V key_939 = R.Get(v_899, (V)"group");
R.Set(obj_938, key_939, values_937[0]);
if (R.Truth((V)(R.Get(v_899, (V)"kind") == (V)"bell"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(v_855, R.Join(new V[] {R.Get(v_899, (V)"bell"), (V)1.0d}, new Results((V)3.0d))).First, () => (V)(!R.Truth(R.Get(R.Get(R.Get(v_887, R.Get(v_899, (V)"group")), (V)"bells"), R.Get(v_899, (V)"bell")))))}, new Results((V)"duplicate bell")));
var values_940 = R.Join(new V[] {}, new Results((V)true));
var obj_941 = R.Get(R.Get(v_887, R.Get(v_899, (V)"group")), (V)"bells");
V key_942 = R.Get(v_899, (V)"bell");
R.Set(obj_941, key_942, values_940[0]);
} else {
var values_943 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_887, R.Get(v_899, (V)"group")), (V)"clues") + (V)1.0d)));
var obj_944 = R.Get(v_887, R.Get(v_899, (V)"group"));
V key_945 = (V)"clues";
R.Set(obj_944, key_945, values_943[0]);
}
} else if (R.Truth((V)(R.Get(v_899, (V)"kind") == (V)"falling_fixture"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"cells")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_899, (V)"cells")) == (V)5.0d)), () => R.Call(v_855, R.Join(new V[] {R.Get(v_899, (V)"center_x"), (V)1.0d}, new Results(R.Get(v_901, (V)"width")))).First), () => R.Call(v_855, R.Join(new V[] {R.Get(v_899, (V)"center_y"), (V)1.0d}, new Results(R.Get(v_901, (V)"height")))).First)}, new Results((V)"invalid fixture cross")));
var values_946 = R.Join(new V[] {}, new Results(R.Table()));
V v_947 = values_946[0];
foreach (var row_948 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"cells"))))))) {
V v_949 = row_948[0];
V v_950 = row_948[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_950, (V)"floor_id") == R.Get(v_899, (V)"floor_id")), () => R.Call(v_855, R.Join(new V[] {R.Get(v_950, (V)"x"), (V)1.0d}, new Results(R.Get(v_901, (V)"width")))).First), () => R.Call(v_855, R.Join(new V[] {R.Get(v_950, (V)"y"), (V)1.0d}, new Results(R.Get(v_901, (V)"height")))).First), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_901, R.Get(v_950, (V)"x")}, new Results(R.Get(v_950, (V)"y")))).First), () => (V)(R.Call(v_52, R.Join(new V[] {v_950}, new Results(R.Table(new Entry((V)"x", R.Get(v_899, (V)"center_x")), new Entry((V)"y", R.Get(v_899, (V)"center_y")))))).First <= (V)1.0d)), () => (V)(!R.Truth(R.Get(v_947, R.Call(v_48, R.Join(new V[] {R.Get(v_950, (V)"x")}, new Results(R.Get(v_950, (V)"y")))).First))))}, new Results((V)"invalid fixture cell")));

var values_951 = R.Join(new V[] {}, new Results((V)true));
var obj_952 = v_947;
V key_953 = R.Call(v_48, R.Join(new V[] {R.Get(v_950, (V)"x")}, new Results(R.Get(v_950, (V)"y")))).First;
R.Set(obj_952, key_953, values_951[0]);
}
if (R.Truth(R.Get(v_899, (V)"pending"))) {
var values_954 = R.Join(new V[] {}, new Results(R.Get(v_899, (V)"pending")));
V v_955 = values_954[0];

var values_956 = R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.Get(v_861, (V)"floor_pause"), () => R.Get(R.Get(v_861, (V)"floor_pause"), R.Get(v_899, (V)"floor_id"))), () => R.Get(R.Get(R.Get(v_861, (V)"floor_pause"), R.Get(v_899, (V)"floor_id")), (V)"suspended_at")), () => R.Get(v_861, (V)"clock"))));
V v_957 = values_956[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.Get(v_899, (V)"spent"), () => (V)(R.Get(v_955, (V)"owner_id") == R.Get(v_864, (V)"id"))), () => R.Call(v_855, R.Join(new V[] {R.Get(v_955, (V)"schedule_id"), (V)1.0d}, new Results((V)(R.Get(v_861, (V)"next_schedule_id") - (V)1.0d)))).First), () => R.Call(v_855, R.Join(new V[] {R.Get(v_955, (V)"action_id"), (V)1.0d}, new Results((V)(R.Get(v_861, (V)"next_action_id") - (V)1.0d)))).First), () => R.Call(v_855, R.Join(new V[] {R.Get(v_955, (V)"due"), v_957}, new Results((V)(v_957 + R.Get(v_16, (V)"fixture_delay"))))).First), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_955, (V)"cells")))).First == (V)"table")), () => (V)((V)R.Length(R.Get(v_955, (V)"cells")) == (V)5.0d))}, new Results((V)"invalid fixture commitment")));
foreach (var row_958 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_955, (V)"cells"))))))) {
V v_959 = row_958[0];
V v_960 = row_958[1];
var values_961 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_899, (V)"cells"), v_959)));
V v_962 = values_961[0];

R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_960, (V)"x") == R.Get(v_962, (V)"x")), () => (V)(R.Get(v_960, (V)"y") == R.Get(v_962, (V)"y"))), () => (V)(R.Get(v_960, (V)"floor_id") == R.Get(v_962, (V)"floor_id")))}, new Results((V)"changed locked fixture cells")));
}
var values_963 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"node", v_899), new Entry((V)"record", v_955))));
var obj_964 = v_886;
V key_965 = R.Get(v_955, (V)"schedule_id");
R.Set(obj_964, key_965, values_963[0]);
}
} else if (R.Truth((V)(R.Get(v_899, (V)"kind") == (V)"route_sign"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.Call(v_855, R.Join(new V[] {R.Get(v_899, (V)"dx"), (-(V)1.0d)}, new Results((V)1.0d))).First, () => R.Call(v_855, R.Join(new V[] {R.Get(v_899, (V)"dy"), (-(V)1.0d)}, new Results((V)1.0d))).First), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"dx")))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"dy")))).First) == (V)1.0d)), () => R.Or(R.Or((V)(R.Get(v_899, (V)"hint") == (V)"Treasure"), () => (V)(R.Get(v_899, (V)"hint") == (V)"Supplies")), () => (V)(R.Get(v_899, (V)"hint") == (V)"Rooms / danger")))}, new Results((V)"invalid route hint")));
} else if (R.Truth((V)(R.Get(v_899, (V)"kind") == (V)"archive_contract"))) {
var values_966 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"room_at"), R.Join(new V[] {v_901, R.Get(v_899, (V)"x")}, new Results(R.Get(v_899, (V)"y")))));
V v_967 = values_966[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_967, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"room")))).First == (V)"table")), () => (V)(R.Get(v_967, (V)"id") == R.Get(v_899, (V)"room_id"))), () => (V)(R.Get(v_967, (V)"id") == R.Get(R.Get(v_899, (V)"room"), (V)"id"))), () => (V)(R.Get(v_967, (V)"x") == R.Get(R.Get(v_899, (V)"room"), (V)"x"))), () => (V)(R.Get(v_967, (V)"y") == R.Get(R.Get(v_899, (V)"room"), (V)"y"))), () => (V)(R.Get(v_967, (V)"width") == R.Get(R.Get(v_899, (V)"room"), (V)"width"))), () => (V)(R.Get(v_967, (V)"height") == R.Get(R.Get(v_899, (V)"room"), (V)"height"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"targets")))).First == (V)"table")), () => (V)((V)R.Length(R.Get(v_899, (V)"targets")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_899, (V)"targets")) <= (V)64.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"defeated")))).First == (V)"table")), () => R.Or(R.Or(R.Or((V)(R.Get(v_899, (V)"status") == (V)"offered"), () => (V)(R.Get(v_899, (V)"status") == (V)"active")), () => (V)(R.Get(v_899, (V)"status") == (V)"failed")), () => (V)(R.Get(v_899, (V)"status") == (V)"completed")))}, new Results((V)"invalid contract")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_899, (V)"spent") == (V)(R.Get(v_899, (V)"status") == (V)"completed"))}, new Results((V)"invalid contract reward flag")));
var values_968 = R.Join(new V[] {}, new Results(R.Table()));
V v_969 = values_968[0];

foreach (var row_970 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"targets"))))))) {
V v_971 = row_970[0];
V v_972 = row_970[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(v_855, R.Join(new V[] {v_972, (V)1.0d}, new Results((V)(R.Get(v_861, (V)"next_entity_id") - (V)1.0d)))).First, () => (V)(!R.Truth(R.Get(v_969, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_972))).First))))}, new Results((V)"invalid contract target")));

var values_973 = R.Join(new V[] {}, new Results((V)true));
var obj_974 = v_969;
V key_975 = R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_972))).First;
R.Set(obj_974, key_975, values_973[0]);
}
foreach (var row_976 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_899, (V)"defeated"))))))) {
V v_977 = row_976[0];
V v_978 = row_976[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_969, v_977), () => (V)(v_978 == (V)true))}, new Results((V)"invalid defeated contract target")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_899, (V)"failure") == V.Nil), () => (V)(R.Get(v_899, (V)"failure") == (V)"left the room")), () => (V)(R.Get(v_899, (V)"failure") == (V)"movement ability used")), () => (V)(R.Get(v_899, (V)"failure") == (V)"target defeated by another faction")), () => (V)(R.Get(v_899, (V)"failure") == (V)"abandoned"))}, new Results((V)"invalid contract failure")));
}
}
foreach (var row_979 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_874, (V)"groups"))))))) {
V v_980 = row_979[0];
V v_981 = row_979[1];
var values_982 = R.Join(new V[] {}, new Results(R.Get(v_887, v_980)));
V v_983 = values_982[0];

R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_981, (V)"kind") == (V)"bells"), () => v_983), () => (V)(R.Get(v_983, (V)"clues") == (V)1.0d)), () => R.Get(R.Get(v_983, (V)"bells"), (V)1.0d)), () => R.Get(R.Get(v_983, (V)"bells"), (V)2.0d)), () => R.Get(R.Get(v_983, (V)"bells"), (V)3.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_981, (V)"sequence")))).First == (V)"table")), () => (V)((V)R.Length(R.Get(v_981, (V)"sequence")) == (V)3.0d)), () => R.Call(v_855, R.Join(new V[] {R.Get(v_981, (V)"progress"), (V)0.0d}, new Results((V)3.0d))).First), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_981, (V)"completed")))).First == (V)"boolean")), () => (V)(R.Get(v_981, (V)"completed") == (V)(R.Get(v_981, (V)"progress") == (V)3.0d)))}, new Results((V)"invalid bell progress")));
var values_984 = R.Join(new V[] {}, new Results(R.Table()));
V v_985 = values_984[0];

foreach (var row_986 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_981, (V)"sequence"))))))) {
V v_987 = row_986[0];
V v_988 = row_986[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(v_855, R.Join(new V[] {v_988, (V)1.0d}, new Results((V)3.0d))).First, () => (V)(!R.Truth(R.Get(v_985, v_988))))}, new Results((V)"invalid bell sequence")));

var values_989 = R.Join(new V[] {}, new Results((V)true));
var obj_990 = v_985;
V key_991 = v_988;
R.Set(obj_990, key_991, values_989[0]);
}
}
var values_992 = R.Join(new V[] {}, new Results(R.Table()));
V v_993 = values_992[0];

foreach (var row_994 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_861, (V)"schedule"))))))) {
V v_995 = row_994[0];
V v_996 = row_994[1];
var values_997 = R.Join(new V[] {}, new Results(v_996));
var obj_998 = v_993;
V key_999 = (V)((V)R.Length(v_993) + (V)1.0d);
R.Set(obj_998, key_999, values_997[0]);
}
foreach (var row_1000 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_861, (V)"floor_pause"), () => R.Table()))))))) {
V v_1001 = row_1000[0];
V v_1002 = row_1000[1];
foreach (var row_1003 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1002, (V)"schedule"))))))) {
V v_1004 = row_1003[0];
V v_1005 = row_1003[1];
var values_1006 = R.Join(new V[] {}, new Results(v_1005));
var obj_1007 = v_993;
V key_1008 = (V)((V)R.Length(v_993) + (V)1.0d);
R.Set(obj_1007, key_1008, values_1006[0]);
}
}
foreach (var row_1009 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_993)))))) {
V v_1010 = row_1009[0];
V v_1011 = row_1009[1];
if (R.Truth(R.Get(v_1011, (V)"idea_fixture"))) {
var values_1012 = R.Join(new V[] {}, new Results(R.Get(v_886, R.Get(v_1011, (V)"id"))));
V v_1013 = values_1012[0];

R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_1013, () => (V)(R.Get(v_1011, (V)"idea_fixture") == R.Get(R.Get(v_1013, (V)"node"), (V)"id"))), () => (V)(R.Get(v_1011, (V)"kind") == (V)"effect")), () => (V)(R.Get(v_1011, (V)"actor_id") == (V)0.0d)), () => (V)(R.Get(v_1011, (V)"effect_id") == R.Get(v_1011, (V)"id"))), () => (V)(R.Get(v_1011, (V)"attack_id") == R.Get(R.Get(v_1013, (V)"record"), (V)"action_id"))), () => (V)(R.Get(v_1011, (V)"due") == R.Get(R.Get(v_1013, (V)"record"), (V)"due"))), () => (V)(R.Get(v_1011, (V)"floor_id") == R.Get(R.Get(v_1013, (V)"node"), (V)"floor_id")))}, new Results((V)"orphan fixture schedule")));
var values_1014 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1015 = v_886;
V key_1016 = R.Get(v_1011, (V)"id");
R.Set(obj_1015, key_1016, values_1014[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(v_886))).First))}, new Results((V)"missing fixture schedule")));
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_18));
return Results.Empty;
} }
