// Static C# port of tests/idea_environment_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 1119261b8aeabb141a4abb0c2d1b9d696b164e161f3733e7870884cfed70503e
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_environment_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_environment"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, new Results(R.Get(v_5, (V)"content")));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_presentation"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_11 = values_10[0];

var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_13 = values_12[0];

var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_terrain"))));
V v_15 = values_14[0];
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
var values_19 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {v_18}, new Results((V)"rb"))));
V v_20 = values_19[0];

if (R.Truth(v_20)) {
var values_21 = R.Join(new V[] {}, R.Invoke(v_20, "read", R.Join(new V[] {}, new Results((V)"*a"))));
V v_22 = values_21[0];

R.Invoke(v_20, "close", Results.Empty);

return R.Join(new V[] {}, new Results(v_22));
}
return Results.Empty;
}));
V v_23 = V.Nil;
v_23 = new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
V v_26 = V.Nil;
v_26 = new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
var values_29 = R.Join(new V[] {}, R.Invoke(v_25, "byte", R.Join(new V[] {v_28}, new Results((V)(v_28 + (V)3.0d)))));
V v_30 = values_29[0];
V v_31 = values_29[1];
V v_32 = values_29[2];
V v_33 = values_29[3];

return R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)((V)(v_30 * (V)256.0d) + v_31) * (V)256.0d) + v_32) * (V)256.0d) + v_33)));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_34 => {

return R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {}, new Results((V)17.0d))));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_35 => {

return R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {}, new Results((V)21.0d))));
return Results.Empty;
}))))));
return Results.Empty;
}));
V v_36 = V.Nil;
v_36 = new V((RuleFunction)(args_37 => {
V v_38 = args_37[0];
V v_39 = args_37[1];
V v_40 = args_37[2];
V v_41 = args_37[3];
V v_42 = args_37[4];
V v_43 = args_37[5];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_38), new Entry((V)"y", v_39), new Entry((V)"w", v_40), new Entry((V)"h", v_41), new Entry((V)"pw", v_42), new Entry((V)"ph", v_43))));
return Results.Empty;
}));
V v_44 = V.Nil;
v_44 = new V((RuleFunction)(args_45 => {
V v_46 = args_45[0];
V v_47 = args_45[1];
V v_48 = args_45[2];
return R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {v_46, R.Or(v_47, () => v_16), R.Or(v_48, () => v_23)}, new Results(v_36))));
return Results.Empty;
}));
V v_49 = V.Nil;
v_49 = new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
return R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, new Results(v_51))))));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"approved room materials and foreground art have distinct stable IDs and bounded native regions"}, new Results(new V((RuleFunction)(args_52 => {

var values_53 = R.Join(new V[] {}, new Results(R.Table()));
V v_54 = values_53[0];
foreach (var row_55 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"material_order"))))))) {
V v_56 = row_55[0];
V v_57 = row_55[1];
var values_58 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"material_id"), R.Join(new V[] {}, new Results(v_57))));
V v_59 = values_58[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_54, v_59))))));

var values_60 = R.Join(new V[] {}, new Results((V)true));
var obj_61 = v_54;
V key_62 = v_59;
R.Set(obj_61, key_62, values_60[0]);
foreach (var row_63 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"kenney"), new Entry((V)2, (V)"onebit")))))))) {
V v_64 = row_63[0];
V v_65 = row_63[1];
var values_66 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"material"), R.Join(new V[] {v_57}, new Results(v_65))));
V v_67 = values_66[0];

var values_68 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_7, (V)"sources"), R.Get(v_67, (V)"sheet"))));
V v_69 = values_68[0];
foreach (var row_70 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_67, (V)"grid"), () => R.Table(new Entry((V)1, R.Get(v_67, (V)"center")))))))))) {
V v_71 = row_70[0];
V v_72 = row_70[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_72 >= (V)0.0d), () => (V)((V)((V)((V)(v_72 % R.Get(v_69, (V)"columns")) * R.Get(v_69, (V)"stride")) + (V)16.0d) <= R.Get(v_69, (V)"width"))), () => (V)((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_72 / R.Get(v_69, (V)"columns"))))).First * R.Get(v_69, (V)"stride")) + (V)16.0d) <= R.Get(v_69, (V)"height"))))));
}
}
}
foreach (var row_73 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"props"))))))) {
V v_74 = row_73[0];
V v_75 = row_73[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_54, R.Call(R.Get(v_7, (V)"prop_id"), R.Join(new V[] {}, new Results(v_74))).First))))));

var values_76 = R.Join(new V[] {}, new Results((V)true));
var obj_77 = v_54;
V key_78 = R.Call(R.Get(v_7, (V)"prop_id"), R.Join(new V[] {}, new Results(v_74))).First;
R.Set(obj_77, key_78, values_76[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_7, (V)"sources"), R.Get(v_75, (V)1.0d)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(R.Get(v_7, (V)"materials"), (V)"pale"), (V)"mixed"), (V)"center") == (V)8.0d), () => (V)(R.Get(R.Get(R.Get(v_7, (V)"props"), (V)"grave_a"), (V)2.0d) == (V)564.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"crypt and pale sanctuary are one existing-room family with memorial context and separate prop roles"}, new Results(new V((RuleFunction)(args_79 => {

var values_80 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)1.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_81 = values_80[0];

var values_82 = R.Join(new V[] {}, new Results(R.Get(v_81, (V)"generated_map")));
V v_83 = values_82[0];

var values_84 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_83, (V)"rooms"), (V)2.0d)));
V v_85 = values_84[0];
var values_86 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"idea_rooms", R.Table(new Entry((V)"nodes", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"memorial"), new Entry((V)"floor_id", R.Get(v_83, (V)"floor_id")), new Entry((V)"x", (V)(R.Get(v_85, (V)"x") + (V)1.0d)), new Entry((V)"y", (V)(R.Get(v_85, (V)"y") + (V)1.0d)))))))))));
V v_87 = values_86[0];
var values_88 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"plan"), R.Join(new V[] {v_87}, new Results(v_83))));
V v_89 = values_88[0];

var values_90 = Results.Empty;
V v_91 = values_90[0];
foreach (var row_92 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_89, (V)"rooms"))))))) {
V v_93 = row_92[0];
V v_94 = row_92[1];
if (R.Truth((V)(R.Get(R.Get(v_94, (V)"room"), (V)"id") == R.Get(v_85, (V)"id")))) {
var values_95 = R.Join(new V[] {}, new Results(v_94));
v_91 = values_95[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_91, (V)"family") == (V)"crypt_sanctuary"), () => (V)(R.Get(v_91, (V)"floor") == (V)"pale")), () => (V)(R.Get(v_91, (V)"wall") == (V)"ruin_carved")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_83, (V)"rooms")) == (V)R.Length(R.Get(v_89, (V)"rooms"))), () => (V)((V)R.Length(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))).First, (V)"order")) == (V)7.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all approved room families and contextual dressing appear across genuine seven-biome floors"}, new Results(new V((RuleFunction)(args_96 => {

var values_97 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_98 = values_97[0];

var values_99 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_100 = values_99[0];
V v_101 = values_99[1];
foreach (var row_102 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_98, (V)"order"))))))) {
V v_103 = row_102[0];
V v_104 = row_102[1];
{ double end_107 = R.Num((V)5.0d), step_108 = R.Num((V)1.0d);
for (double i_106 = R.Num((V)1.0d); step_108 > 0 ? i_106 <= end_107 : i_106 >= end_107; i_106 += step_108) {
V v_105 = i_106;
var values_109 = R.Join(new V[] {}, new Results((V)(v_104 == R.Get(R.Get(v_98, (V)"order"), (V)1.0d))));
V v_110 = values_109[0];

var values_111 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {v_105}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"stage_options", R.Table(new Entry((V)"biome_id", v_104), new Entry((V)"index", R.Or(R.And(v_110, () => (V)1.0d), () => (V)2.0d)), new Entry((V)"route_history", R.Or(R.And(v_110, () => R.Table()), () => R.Table(new Entry(1, new Results(R.Invoke(v_104, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First))))))))))));
V v_112 = values_111[0];
var values_113 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_112, (V)"generated_map")))));
V v_114 = values_113[0];

foreach (var row_115 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_112, (V)"inactive_floor_maps"))))))) {
V v_116 = row_115[0];
V v_117 = row_115[1];
var values_118 = R.Join(new V[] {}, new Results(v_117));
var obj_119 = v_114;
V key_120 = (V)((V)R.Length(v_114) + (V)1.0d);
R.Set(obj_119, key_120, values_118[0]);
}
foreach (var row_121 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_114)))))) {
V v_122 = row_121[0];
V v_123 = row_121[1];
var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"plan"), R.Join(new V[] {v_112}, new Results(v_123))));
V v_125 = values_124[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_125, (V)"props")) <= (V)((V)R.Length(R.Get(v_123, (V)"rooms")) * (V)2.0d)))));
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_125, (V)"rooms"))))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
var values_129 = R.Join(new V[] {}, new Results((V)true));
var obj_130 = v_100;
V key_131 = R.Get(v_128, (V)"family");
R.Set(obj_130, key_131, values_129[0]);
}
foreach (var row_132 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_125, (V)"props"))))))) {
V v_133 = row_132[0];
V v_134 = row_132[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(R.Get(v_123, (V)"rows"), R.Get(v_134, (V)"y")), "sub", R.Join(new V[] {R.Get(v_134, (V)"x")}, new Results(R.Get(v_134, (V)"x")))).First == (V)"#"))));

var values_135 = R.Join(new V[] {}, new Results((V)true));
var obj_136 = v_101;
V key_137 = R.Get(v_134, (V)"id");
R.Set(obj_136, key_137, values_135[0]);
}
}
}
}
}
foreach (var row_138 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"families"))))))) {
V v_139 = row_138[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_100, v_139)}, new Results(R.Concat((V)"unseen family ", v_139))));
}
foreach (var row_140 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"camp_table"), new Entry((V)2, (V)"camp_seat"), new Entry((V)3, (V)"workshop_supplies"), new Entry((V)4, (V)"shelf_full"), new Entry((V)5, (V)"shelf_empty"), new Entry((V)6, (V)"chapel_window"), new Entry((V)7, (V)"prison_bars"), new Entry((V)8, (V)"rock_a"), new Entry((V)9, (V)"bone_a")))))))) {
V v_141 = row_140[0];
V v_142 = row_140[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_101, R.Call(R.Get(v_7, (V)"prop_id"), R.Join(new V[] {}, new Results(v_142))).First)}, new Results(R.Concat((V)"unseen dressing ", v_142))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"room planning is deterministic and cannot change geometry, entity state, RNG or profile eligibility"}, new Results(new V((RuleFunction)(args_143 => {

var values_144 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)9.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_145 = values_144[0];

var values_146 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_145))));
V v_147 = values_146[0];
var values_148 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"plan"), R.Join(new V[] {v_145}, new Results(R.Get(v_145, (V)"generated_map")))));
V v_149 = values_148[0];

R.Call(v_3, R.Join(new V[] {v_149}, R.Call(R.Get(v_5, (V)"plan"), R.Join(new V[] {v_145}, new Results(R.Get(v_145, (V)"generated_map"))))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_145))).First}, new Results(v_147)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"source-preserving room decoration avoids secret walls and authored visual overrides"}, new Results(new V((RuleFunction)(args_150 => {

var values_151 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_152 = values_151[0];

var values_153 = R.Join(new V[] {}, new Results(R.Get(v_152, (V)"generated_map")));
V v_154 = values_153[0];

var values_155 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"plan"), R.Join(new V[] {v_152}, new Results(v_154))));
V v_156 = values_155[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_156, (V)"props")) > (V)0.0d))));
var values_157 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_156, (V)"props"), (V)1.0d)));
V v_158 = values_157[0];

var values_159 = R.Join(new V[] {}, R.Call(v_49, R.Join(new V[] {}, new Results(v_154))));
V v_160 = values_159[0];

var values_161 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_160, (V)"studio_visuals"), () => R.Table())));
var obj_162 = v_160;
V key_163 = (V)"studio_visuals";
R.Set(obj_162, key_163, values_161[0]);

var values_164 = R.Join(new V[] {}, new Results((V)"studio:reserved"));
var obj_165 = R.Get(v_160, (V)"studio_visuals");
V key_166 = R.Concat(R.Get(v_158, (V)"x"), R.Concat((V)",", R.Get(v_158, (V)"y")));
R.Set(obj_165, key_166, values_164[0]);
foreach (var row_167 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"plan"), R.Join(new V[] {v_152}, new Results(v_160))).First, (V)"props"))))))) {
V v_168 = row_167[0];
V v_169 = row_167[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(v_169, (V)"x") != R.Get(v_158, (V)"x")), () => (V)(R.Get(v_169, (V)"y") != R.Get(v_158, (V)"y"))))));
}
foreach (var row_170 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_156, (V)"props"))))))) {
V v_171 = row_170[0];
V v_172 = row_170[1];
foreach (var row_173 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_154, (V)"placements"), (V)"secrets"))))))) {
V v_174 = row_173[0];
V v_175 = row_173[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(v_172, (V)"x") != R.Get(v_175, (V)"wall_x")), () => (V)(R.Get(v_172, (V)"y") != R.Get(v_175, (V)"wall_y"))))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all 256 neighbor cases resolve canonical connected environment walls and hide unknown connections"}, new Results(new V((RuleFunction)(args_176 => {

var values_177 = R.Join(new V[] {}, R.Call(v_44, R.Join(new V[] {}, new Results((V)"kenney"))));
V v_178 = values_177[0];

var values_179 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)4.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)8.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)16.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)32.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)64.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)128.0d))))));
V v_180 = values_179[0];
{ double end_183 = R.Num((V)255.0d), step_184 = R.Num((V)1.0d);
for (double i_182 = R.Num((V)0.0d); step_184 > 0 ? i_182 <= end_183 : i_182 >= end_183; i_182 += step_184) {
V v_181 = i_182;
var values_185 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"."), new Entry((V)2, (V)"."), new Entry((V)3, (V)"."))), new Entry((V)2, R.Table(new Entry((V)1, (V)"."), new Entry((V)2, (V)"#"), new Entry((V)3, (V)"."))), new Entry((V)3, R.Table(new Entry((V)1, (V)"."), new Entry((V)2, (V)"."), new Entry((V)3, (V)"."))))));
V v_186 = values_185[0];
foreach (var row_187 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_180)))))) {
V v_188 = row_187[0];
V v_189 = row_187[1];
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_181 / R.Get(v_189, (V)3.0d))))).First % (V)2.0d) == (V)1.0d))) {
var values_190 = R.Join(new V[] {}, new Results((V)"#"));
var obj_191 = R.Get(v_186, (V)(R.Get(v_189, (V)2.0d) + (V)2.0d));
V key_192 = (V)(R.Get(v_189, (V)1.0d) + (V)2.0d);
R.Set(obj_191, key_192, values_190[0]);
}
}
var values_193 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rows", R.Table()), new Entry((V)"seed", (V)1.0d), new Entry((V)"floor_id", (V)"fixture"), new Entry((V)"rooms", R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)1.0d), new Entry((V)"x", (V)1.0d), new Entry((V)"y", (V)1.0d), new Entry((V)"width", (V)3.0d), new Entry((V)"height", (V)3.0d), new Entry((V)"role", (V)"landmark"))))), new Entry((V)"placements", R.Table()))));
V v_194 = values_193[0];
foreach (var row_195 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_186)))))) {
V v_196 = row_195[0];
V v_197 = row_195[1];
var values_198 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(v_197))));
var obj_199 = R.Get(v_194, (V)"rows");
V key_200 = v_196;
R.Set(obj_199, key_200, values_198[0]);
}
R.Invoke(v_178, "set_world", R.Join(new V[] {}, new Results(R.Table(new Entry((V)"generated_map", v_194), new Entry((V)"active_floor_id", R.Get(v_194, (V)"floor_id")), new Entry((V)"next_event_id", (V)1.0d)))));
var values_201 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_178, "resolve", R.Join(new V[] {v_194, (V)2.0d, (V)2.0d}, new Results((V)"wall"))))));
V v_202 = values_201[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_202, (V)"mask") == R.Call(R.Get(v_15, (V)"canonical"), R.Join(new V[] {}, new Results(v_181))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_178, "resolve", R.Join(new V[] {v_194, (V)2.0d, (V)2.0d, (V)"wall"}, new Results(new V((RuleFunction)(args_203 => {

return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}))))).First, (V)"mask") == (V)255.0d))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"derived material layouts are deterministic, bounded and nonoverlapping across cached pages"}, new Results(new V((RuleFunction)(args_204 => {

foreach (var row_205 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"kenney"), new Entry((V)2, (V)"onebit")))))))) {
V v_206 = row_205[0];
V v_207 = row_205[1];
foreach (var row_208 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"sources"))))))) {
V v_209 = row_208[0];
V v_210 = row_208[1];
var values_211 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"surfaces"), R.Join(new V[] {v_209}, new Results(v_207))));
V v_212 = values_211[0];

R.Call(v_3, R.Join(new V[] {v_212}, R.Call(R.Get(v_5, (V)"surfaces"), R.Join(new V[] {v_209}, new Results(v_207)))));

var values_213 = R.Join(new V[] {}, new Results(R.Table()));
V v_214 = values_213[0];
foreach (var row_215 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_212)))))) {
V v_216 = row_215[0];
V v_217 = row_215[1];
var values_218 = R.Join(new V[] {(V)((V)(R.Get(v_217, (V)"slot") % R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_210, (V)"width") / (V)16.0d)))).First) * (V)16.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_217, (V)"slot") / R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_210, (V)"width") / (V)16.0d)))).First)))).First * (V)16.0d)));
V v_219 = values_218[0];
V v_220 = values_218[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(v_214, R.Get(v_217, (V)"slot")))), () => (V)((V)(v_219 + (V)16.0d) <= R.Get(v_210, (V)"width"))), () => (V)((V)(v_220 + (V)16.0d) <= R.Get(v_210, (V)"height"))))));

var values_221 = R.Join(new V[] {}, new Results((V)true));
var obj_222 = v_214;
V key_223 = R.Get(v_217, (V)"slot");
R.Set(obj_222, key_223, values_221[0]);
}
}
}
return Results.Empty;
})))));
V v_224 = V.Nil;
v_224 = new V((RuleFunction)(args_225 => {
V v_226 = args_225[0];
V v_227 = args_225[1];
var values_228 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_7, (V)"sources"), v_226)));
V v_229 = values_228[0];

var values_230 = R.Join(new V[] {}, new Results(R.Table()));
V v_231 = values_230[0];

var values_232 = R.Join(new V[] {}, new Results(R.Table()));
V v_233 = values_232[0];
R.Set(v_233, "getPixel", new V((RuleFunction)(args_234 => {
V v_235 = args_234[0];
V v_236 = args_234[1];
V v_237 = args_234[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_236 >= (V)0.0d), () => (V)(v_236 < R.Get(v_229, (V)"width"))), () => (V)(v_237 >= (V)0.0d)), () => (V)(v_237 < R.Get(v_229, (V)"height"))))));
var values_238 = R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_237 / R.Get(v_229, (V)"stride"))))).First * R.Get(v_229, (V)"columns")) + R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_236 / R.Get(v_229, (V)"stride"))))).First)));
V v_239 = values_238[0];
return R.Join(new V[] {(V)(v_239 / (V)2048.0d), (V)((V)(v_236 % R.Get(v_229, (V)"stride")) / (V)16.0d), (V)((V)(v_237 % R.Get(v_229, (V)"stride")) / (V)16.0d)}, new Results((V)1.0d));
return Results.Empty;
})));
R.Set(v_233, "setPixel", new V((RuleFunction)(args_240 => {
V v_241 = args_240[0];
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"source pixel overwritten")));
return Results.Empty;
})));
R.Set(v_233, "clone", new V((RuleFunction)(args_242 => {
V v_243 = args_242[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"setPixel", new V((RuleFunction)(args_244 => {
V v_245 = args_244[0];
V v_246 = args_244[1];
V v_247 = args_244[2];
V v_248 = args_244[3];
V v_249 = args_244[4];
V v_250 = args_244[5];
V v_251 = args_244[6];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(v_246 >= (V)0.0d), () => (V)(v_246 < R.Get(v_229, (V)"width"))), () => (V)(v_247 >= (V)0.0d)), () => (V)(v_247 < R.Get(v_229, (V)"height"))), () => (V)(v_251 == (V)1.0d)))));
var values_252 = R.Join(new V[] {}, new Results((V)((V)(v_247 * R.Get(v_229, (V)"width")) + v_246)));
V v_253 = values_252[0];

R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_231, v_253)))}, new Results((V)"overlapping derived writes")));

var values_254 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_248), new Entry((V)2, v_249), new Entry((V)3, v_250), new Entry((V)4, v_251))));
var obj_255 = v_231;
V key_256 = v_253;
R.Set(obj_255, key_256, values_254[0]);
return Results.Empty;
}))))));
return Results.Empty;
})));
R.Call(R.Get(v_5, (V)"surface_sheet"), R.Join(new V[] {v_233, v_226}, new Results(v_227)));
V v_257 = V.Nil;
v_257 = new V((RuleFunction)(args_258 => {
V v_259 = args_258[0];
V v_260 = args_258[1];
V v_261 = args_258[2];
V v_262 = args_258[3];
foreach (var row_263 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"surfaces"), R.Join(new V[] {v_226}, new Results(v_227)))))))) {
V v_264 = row_263[0];
V v_265 = row_263[1];
if (R.Truth(R.And((V)(R.Get(v_265, (V)"name") == v_259), () => (V)(R.Get(v_265, (V)"mask") == v_260)))) {
var values_266 = R.Join(new V[] {(V)((V)(R.Get(v_265, (V)"slot") % R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_229, (V)"width") / (V)16.0d)))).First) * (V)16.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_265, (V)"slot") / R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_229, (V)"width") / (V)16.0d)))).First)))).First * (V)16.0d)));
V v_267 = values_266[0];
V v_268 = values_266[1];
return R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_231, (V)((V)((V)((V)(v_268 + v_262) * R.Get(v_229, (V)"width")) + v_267) + v_261))))));
}
}
return Results.Empty;
}));
return R.Join(new V[] {v_231}, new Results(v_257));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"fully connected wall pixels sample the correct native center at untrimmed local coordinates"}, new Results(new V((RuleFunction)(args_269 => {

var values_270 = R.Join(new V[] {}, R.Call(v_224, R.Join(new V[] {(V)"rpg"}, new Results((V)"kenney"))));
V v_271 = values_270[0];
V v_272 = values_270[1];
foreach (var row_273 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)7.0d), new Entry((V)2, (V)5.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)15.0d), new Entry((V)2, (V)15.0d)))))))))) {
V v_274 = row_273[0];
V v_275 = row_273[1];
var values_276 = R.Join(new V[] {}, R.Call(v_272, R.Join(new V[] {(V)"ruin", (V)255.0d, R.Get(v_275, (V)1.0d)}, new Results(R.Get(v_275, (V)2.0d)))));
V v_277 = values_276[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_277, (V)1.0d) == (V)((V)762.0d / (V)2048.0d)), () => (V)(R.Get(v_277, (V)2.0d) == (V)(R.Get(v_275, (V)1.0d) / (V)16.0d))), () => (V)(R.Get(v_277, (V)3.0d) == (V)(R.Get(v_275, (V)2.0d) / (V)16.0d))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(v_271)))));
var values_278 = R.Join(new V[] {}, R.Call(v_272, R.Join(new V[] {(V)"carpet", (V)255.0d, (V)7.0d}, new Results((V)5.0d))));
V v_279 = values_278[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_279, (V)1.0d) == (V)((V)((V)980.0d / (V)2048.0d) * (V)0.72d)), () => (V)(R.Get(v_279, (V)2.0d) == (V)((V)((V)7.0d / (V)16.0d) * (V)0.72d))), () => (V)(R.Get(v_279, (V)3.0d) == (V)((V)((V)5.0d / (V)16.0d) * (V)0.72d))))));
R.Call(v_3, R.Join(new V[] {R.Get(R.Get(R.Get(R.Get(v_7, (V)"materials"), (V)"carpet"), (V)"mixed"), (V)"grid")}, new Results(R.Table(new Entry((V)1, (V)922.0d), new Entry((V)2, (V)923.0d), new Entry((V)3, (V)924.0d), new Entry((V)4, (V)979.0d), new Entry((V)5, (V)980.0d), new Entry((V)6, (V)981.0d), new Entry((V)7, (V)1036.0d), new Entry((V)8, (V)1037.0d), new Entry((V)9, (V)1038.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"water exterior and concave corners use the inspected RPG bank family rather than stray floor sprites"}, new Results(new V((RuleFunction)(args_280 => {

var values_281 = R.Join(new V[] {}, R.Call(v_224, R.Join(new V[] {(V)"rpg"}, new Results((V)"kenney"))));
V v_282 = values_281[0];
V v_283 = values_281[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(v_283, R.Join(new V[] {(V)"drowned", (V)0.0d, (V)0.0d}, new Results((V)0.0d))).First, (V)1.0d) == (V)((V)2.0d / (V)2048.0d)), () => (V)(R.Get(R.Call(v_283, R.Join(new V[] {(V)"drowned", (V)0.0d, (V)15.0d}, new Results((V)15.0d))).First, (V)1.0d) == (V)((V)118.0d / (V)2048.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(v_283, R.Join(new V[] {(V)"drowned", (V)127.0d, (V)0.0d}, new Results((V)0.0d))).First, (V)1.0d) == (V)((V)115.0d / (V)2048.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(v_283, R.Join(new V[] {(V)"drowned", (V)239.0d, (V)15.0d}, new Results((V)0.0d))).First, (V)1.0d) == (V)((V)114.0d / (V)2048.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(v_283, R.Join(new V[] {(V)"drowned", (V)223.0d, (V)15.0d}, new Results((V)15.0d))).First, (V)1.0d) == (V)((V)57.0d / (V)2048.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(v_283, R.Join(new V[] {(V)"drowned", (V)191.0d, (V)0.0d}, new Results((V)15.0d))).First, (V)1.0d) == (V)((V)58.0d / (V)2048.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"monochrome environment surfaces contain neutral opaque pixels and keep native originals immutable"}, new Results(new V((RuleFunction)(args_284 => {

foreach (var row_285 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)"caves"), new Entry((V)3, (V)"onebit")))))))) {
V v_286 = row_285[0];
V v_287 = row_285[1];
var values_288 = R.Join(new V[] {}, R.Call(v_224, R.Join(new V[] {v_287}, new Results((V)"onebit"))));
V v_289 = values_288[0];

foreach (var row_290 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_289)))))) {
V v_291 = row_290[0];
V v_292 = row_290[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Get(v_292, (V)1.0d) == R.Get(v_292, (V)2.0d)), () => (V)(R.Get(v_292, (V)2.0d) == R.Get(v_292, (V)3.0d))), () => (V)(R.Get(v_292, (V)4.0d) == (V)1.0d)), () => (V)(R.Get(v_292, (V)1.0d) >= (V)0.0d)), () => (V)(R.Get(v_292, (V)1.0d) <= (V)1.0d)))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"environment handles cache every texture and Quad instead of decoding them per tile or frame"}, new Results(new V((RuleFunction)(args_293 => {

var values_294 = R.Join(new V[] {}, new Results((V)0.0d));
V v_295 = values_294[0];

var values_299 = R.Join(new V[] {}, R.Call(v_44, R.Join(new V[] {(V)"kenney", V.Nil}, new Results(new V((RuleFunction)(args_296 => {
V v_297 = args_296[0];
var values_298 = R.Join(new V[] {}, new Results((V)(v_295 + (V)1.0d)));
v_295 = values_298[0];

return R.Join(new V[] {}, R.Call(v_23, R.Join(new V[] {}, new Results(v_297))));
return Results.Empty;
}))))));
V v_300 = values_299[0];
var values_301 = R.Join(new V[] {}, new Results(v_295));
V v_302 = values_301[0];

var values_303 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_304 = values_303[0];
{ double end_307 = R.Num((V)20.0d), step_308 = R.Num((V)1.0d);
for (double i_306 = R.Num((V)1.0d); step_308 > 0 ? i_306 <= end_307 : i_306 >= end_307; i_306 += step_308) {
V v_305 = i_306;
R.Invoke(v_300, "set_world", R.Join(new V[] {}, new Results(v_304)));

foreach (var row_309 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_300, (V)"plan"), (V)"rooms"))))))) {
V v_310 = row_309[0];
V v_311 = row_309[1];
R.Invoke(v_300, "resolve", R.Join(new V[] {R.Get(v_304, (V)"generated_map"), R.Get(R.Get(v_311, (V)"room"), (V)"x"), R.Get(R.Get(v_311, (V)"room"), (V)"y")}, new Results((V)"wall")));
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_295 == v_302), () => (V)(v_302 <= (V)6.0d)))));
var values_312 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"prop_id"), R.Join(new V[] {}, new Results((V)"grave_a"))));
V v_313 = values_312[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_300, (V)"handles"), v_313) == R.Get(R.Get(v_300, (V)"handles"), v_313)), () => (V)(R.Get(R.Get(R.Get(v_300, (V)"handles"), v_313), (V)"width") == (V)16.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"missing optional colored counterparts preserve native actors/material fallback and a distinct refresh signature"}, new Results(new V((RuleFunction)(args_314 => {

var values_315 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_visuals"))));
V v_316 = values_315[0];

var values_317 = R.Join(new V[] {}, R.Call(R.Get(v_316, (V)"new"), R.Join(new V[] {v_16, v_23}, new Results(v_36))));
V v_318 = values_317[0];
var values_321 = R.Join(new V[] {}, R.Call(R.Get(v_316, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_319 => {
V v_320 = args_319[0];
if (R.Truth(R.And((V)(v_320 != R.Get(R.Get(R.Get(v_7, (V)"sources"), (V)"rpg"), (V)"path")), () => (V)(v_320 != R.Get(R.Get(R.Get(v_7, (V)"sources"), (V)"caves"), (V)"path"))))) {
return R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {}, new Results(v_320))));
}
return Results.Empty;
})), v_23}, new Results(v_36))));
V v_322 = values_321[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_318, (V)"signature") != R.Get(v_322, (V)"signature")), () => R.Invoke(v_322, "resolve", R.Join(new V[] {}, new Results((V)"core:actor/adventurer"))).First))));
var values_323 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rows", R.Table(new Entry((V)1, (V)"~~~"), new Entry((V)2, (V)"~~~"), new Entry((V)3, (V)"~~~"))), new Entry((V)"rooms", R.Table()), new Entry((V)"floor_id", (V)"fixture"))));
V v_324 = values_323[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(v_322, "terrain", R.Join(new V[] {v_324, (V)2.0d, (V)2.0d}, new Results((V)"water"))).First, () => R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_322, (V)"environment"), (V)"failures")))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"room materials and dressing survive exact save reopening without entering the authoritative snapshot"}, new Results(new V((RuleFunction)(args_325 => {

var values_326 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)11.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_327 = values_326[0];

var values_328 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_327))));
V v_329 = values_328[0];

var values_330 = R.Join(new V[] {}, R.Call(v_44, R.Join(new V[] {}, new Results((V)"onebit"))));
V v_331 = values_330[0];

R.Invoke(v_331, "set_world", R.Join(new V[] {}, new Results(v_327)));
var values_332 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"plan"), R.Join(new V[] {v_327}, new Results(R.Get(v_327, (V)"generated_map")))));
V v_333 = values_332[0];

var values_334 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, new Results(v_329))))))));
V v_335 = values_334[0];
R.Call(v_3, R.Join(new V[] {v_333}, R.Call(R.Get(v_5, (V)"plan"), R.Join(new V[] {v_335}, new Results(R.Get(v_335, (V)"generated_map"))))));

R.Call(v_3, R.Join(new V[] {v_329}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_327)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_327, (V)"environment") == V.Nil))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"floor switches and visual refresh replace the presentation plan without regenerating either floor"}, new Results(new V((RuleFunction)(args_336 => {

var values_337 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)5.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_338 = values_337[0];

var values_339 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_338))));
V v_340 = values_339[0];

var values_341 = R.Join(new V[] {}, R.Call(v_44, R.Join(new V[] {}, new Results((V)"kenney"))));
V v_342 = values_341[0];

R.Invoke(v_342, "set_world", R.Join(new V[] {}, new Results(v_338)));
var values_343 = R.Join(new V[] {}, new Results(R.Get(v_342, (V)"plan")));
V v_344 = values_343[0];

var values_345 = Results.Empty;
V v_346 = values_345[0];
foreach (var row_347 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_338, (V)"inactive_floor_maps"))))))) {
V v_348 = row_347[0];
V v_349 = row_347[1];
var values_350 = R.Join(new V[] {}, new Results(v_349));
v_346 = values_350[0];
}
var values_351 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"generated_map", v_346), new Entry((V)"active_floor_id", R.Get(v_346, (V)"floor_id")), new Entry((V)"next_event_id", R.Get(v_338, (V)"next_event_id")), new Entry((V)"idea_rooms", R.Get(v_338, (V)"idea_rooms")))));
V v_352 = values_351[0];
R.Invoke(v_342, "set_world", R.Join(new V[] {}, new Results(v_352)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_342, (V)"plan") != v_344), () => (V)(R.Get(R.Get(R.Get(R.Get(v_342, (V)"plan"), (V)"rooms"), (V)1.0d), (V)"room") == R.Get(R.Get(v_346, (V)"rooms"), (V)1.0d))))));
var values_353 = R.Join(new V[] {}, R.Call(v_44, R.Join(new V[] {}, new Results((V)"kenney"))));
V v_354 = values_353[0];

R.Invoke(v_354, "set_world", R.Join(new V[] {}, new Results(v_338)));

R.Call(v_3, R.Join(new V[] {R.Get(v_354, (V)"plan")}, R.Call(R.Get(v_5, (V)"plan"), R.Join(new V[] {v_338}, new Results(R.Get(v_338, (V)"generated_map"))))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_338))).First}, new Results(v_340)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"every playable class has compatible F/R/X art and weapon exclusions stay out of the new family"}, new Results(new V((RuleFunction)(args_355 => {

var values_356 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_visuals"))).First, (V)"new"), R.Join(new V[] {v_16, v_23}, new Results(v_36))));
V v_357 = values_356[0];
foreach (var row_358 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))).First, (V)"order"))))))) {
V v_359 = row_358[0];
V v_360 = row_358[1];
foreach (var row_361 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"basic_attack"), new Entry((V)2, (V)"sweeping_slash"), new Entry((V)3, (V)"dash")))))))) {
V v_362 = row_361[0];
V v_363 = row_361[1];
var values_364 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"weapon"), R.Join(new V[] {v_360}, new Results(v_363))));
V v_365 = values_364[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_357, "resolve", R.Join(new V[] {}, new Results(v_365)))));
}
}
foreach (var row_366 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"symbols"))))))) {
V v_367 = row_366[0];
V v_368 = row_366[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_368))).First == (V)"number"), () => (V)(v_368 >= (V)0.0d)), () => (V)(v_368 < (V)1078.0d)))));
}
foreach (var row_369 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"props"))))))) {
V v_370 = row_369[0];
V v_371 = row_369[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_371, (V)1.0d) != (V)"micro"))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"status icons reflect real Burning, supply wards, focus and charge while retaining text labels"}, new Results(new V((RuleFunction)(args_372 => {

var values_373 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"definition_id", (V)"core:actor/stormcaller"), new Entry((V)"storm_charge", (V)2.0d), new Entry((V)"pickup_ward_actions", (V)2.0d), new Entry((V)"supply_focus_actions", (V)1.0d), new Entry((V)"statuses", R.Table(new Entry((V)"burning", R.Table(new Entry((V)"remaining", (V)2.0d))))))));
V v_374 = values_373[0];
var values_375 = R.Join(new V[] {}, R.Call(v_49, R.Join(new V[] {}, new Results(v_374))));
V v_376 = values_375[0];

var values_377 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"statuses"), R.Join(new V[] {}, new Results(v_374))));
V v_378 = values_377[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_378) == (V)4.0d), () => (V)(R.Get(R.Get(v_378, (V)4.0d), (V)"label") == (V)"Charge 2")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_378, (V)4.0d), (V)"id") == (V)"core:ui/status_charge_2_icon"))));

R.Call(v_3, R.Join(new V[] {v_376}, new Results(v_374)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(R.Get(v_9, (V)"statuses"), R.Join(new V[] {}, new Results(R.Table()))).First) == (V)0.0d), () => (V)(R.Call(R.Get(v_9, (V)"signal"), R.Join(new V[] {}, new Results((V)"warning"))).First == (V)"core:ui/signal_armed_effect")), () => (V)(R.Call(R.Get(v_9, (V)"signal"), R.Join(new V[] {}, new Results((V)"closed"))).First == (V)"core:ui/signal_spent_effect")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"static combat cues distinguish damage types, blocked damage, boon activations and real recovery"}, new Results(new V((RuleFunction)(args_379 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_9, (V)"effect"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"actual_loss", (V)0.0d))))).First == V.Nil))));
foreach (var row_380 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"physical", (V)"slash"), new Entry((V)"fire", (V)"fire"), new Entry((V)"lightning", (V)"spark"), new Entry((V)"echo", (V)"echo"), new Entry((V)"spirit", (V)"echo")))))))) {
V v_381 = row_380[0];
V v_382 = row_380[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_9, (V)"effect"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"damage_type", v_381), new Entry((V)"actual_loss", (V)2.0d))))).First == R.Concat((V)"core:ui/cue_", R.Concat(v_382, (V)"_effect"))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_9, (V)"effect"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"DamageBlocked"))))).First == (V)"core:ui/cue_block_effect"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_9, (V)"effect"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"Healed"), new Entry((V)"actual_gain", (V)2.0d))))).First == (V)"core:ui/status_heal_icon"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_9, (V)"effect"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"Moved"))))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_9, (V)"effect"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"Healed"), new Entry((V)"actual_gain", (V)0.0d))))).First))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a genuine authoritative hit resolves semantic artwork and playback cannot change its state or RNG"}, new Results(new V((RuleFunction)(args_383 => {

var values_384 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_385 = values_384[0];

var values_386 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_385))));
V v_387 = values_386[0];

var values_388 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_389 = R.Get(v_387, (V)"position");
V key_390 = (V)"x";
var obj_391 = R.Get(v_387, (V)"position");
V key_392 = (V)"y";
R.Set(obj_389, key_390, values_388[0]);
R.Set(obj_391, key_392, values_388[1]);
var values_393 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_385, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_394 = values_393[0];
var values_395 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_385}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_396 = values_395[0];
V v_397 = values_395[1];
V v_398 = values_395[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_396}, new Results(v_397)));
var values_399 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_385))));
V v_400 = values_399[0];

var values_401 = R.Join(new V[] {}, new Results((V)false));
V v_402 = values_401[0];

var values_403 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_visuals"))).First, (V)"new"), R.Join(new V[] {v_16, v_23}, new Results(v_36))));
V v_404 = values_403[0];
foreach (var row_405 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_398)))))) {
V v_406 = row_405[0];
V v_407 = row_405[1];
if (R.Truth((V)(R.Get(v_407, (V)"kind") == (V)"DamageTaken"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_407, (V)"target_id") == R.Get(v_394, (V)"id")), () => R.Invoke(v_404, "resolve", R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"effect"), R.Join(new V[] {}, new Results(v_407))))).First))));

var values_408 = R.Join(new V[] {}, new Results((V)true));
v_402 = values_408[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_402)));

var values_409 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_410 = values_409[0];

var values_411 = R.Join(new V[] {}, R.Call(R.Get(v_410, (V)"new"), Results.Empty));
V v_412 = values_411[0];

R.Call(R.Get(v_410, (V)"add"), R.Join(new V[] {v_412, v_398}, new Results(R.Get(v_387, (V)"id"))));
{ double end_415 = R.Num((V)60.0d), step_416 = R.Num((V)1.0d);
for (double i_414 = R.Num((V)1.0d); step_416 > 0 ? i_414 <= end_415 : i_414 >= end_415; i_414 += step_416) {
V v_413 = i_414;
R.Call(R.Get(v_410, (V)"update"), R.Join(new V[] {v_412}, new Results((V)0.05d)));
}
}

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_385))).First}, new Results(v_400)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"saved catalogue covers all environment and boon IDs without changing class bodies"}, new Results(new V((RuleFunction)(args_417 => {

var values_418 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))));
V v_419 = values_418[0];
var values_420 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_421 = values_420[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_421, (V)"requirements")) == (V)((V)((V)491.0d + (V)((V)2.0d * (V)R.Length(R.Get(v_419, (V)"order")))) + (V)R.Length(R.Get(v_419, (V)"cues")))))));
foreach (var row_422 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_421, (V)"packs"))))))) {
V v_423 = row_422[0];
V v_424 = row_422[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_424, (V)"missing")) == (V)0.0d), () => (V)(R.Get(v_424, (V)"actor_count") == (V)65.0d)), () => (V)(R.Get(v_424, (V)"static_pose_aliases") == (V)130.0d)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))).First, (V)"order")) == (V)((V)53.0d + (V)R.Length(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"order")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"actual world and HUD draw paths show fog-safe dressing and rewards with the minimap hidden"}, new Results(new V((RuleFunction)(args_425 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(rt.Global("_G"), (V)"love"))))));

var values_426 = R.Join(new V[] {}, new Results(R.Table()));
V v_427 = values_426[0];

var values_428 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"love", v_427))}, new Results(R.Table(new Entry((V)"__index", rt.Global("_G")))))));
V v_429 = values_428[0];
var values_430 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("loadfile"), R.Join(new V[] {}, new Results((V)"main.lua"))))));
V v_431 = values_430[0];

R.Call(rt.Global("setfenv"), R.Join(new V[] {v_431}, new Results(v_429)));

R.Call(v_431, Results.Empty);
V v_432 = V.Nil;
v_432 = new V((RuleFunction)(args_433 => {
V v_434 = args_433[0];
V v_435 = args_433[1];
var values_436 = R.Join(new V[] {}, new Results(R.Table()));
V v_437 = values_436[0];

V v_438 = V.Nil;
v_438 = new V((RuleFunction)(args_439 => {
V v_440 = args_439[0];
if (R.Truth(R.Get(v_437, v_440))) {
return Results.Empty;
}

var values_441 = R.Join(new V[] {}, new Results((V)true));
var obj_442 = v_437;
V key_443 = v_440;
R.Set(obj_442, key_443, values_441[0]);

var values_444 = R.Join(new V[] {}, new Results(R.Table()));
V v_445 = values_444[0];
{ double end_448 = R.Num((V)200.0d), step_449 = R.Num((V)1.0d);
for (double i_447 = R.Num((V)1.0d); step_449 > 0 ? i_447 <= end_448 : i_447 >= end_448; i_447 += step_449) {
V v_446 = i_447;
var values_450 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("debug"), (V)"getupvalue"), R.Join(new V[] {v_440}, new Results(v_446))));
V v_451 = values_450[0];
V v_452 = values_450[1];

if (R.Truth((V)(!R.Truth(v_451)))) {
break;
}
R.Call(v_435, R.Join(new V[] {v_440, v_446, v_451}, new Results(v_452)));

if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_452))).First == (V)"function"))) {
var values_453 = R.Join(new V[] {}, new Results(v_452));
var obj_454 = v_445;
V key_455 = (V)((V)R.Length(v_445) + (V)1.0d);
R.Set(obj_454, key_455, values_453[0]);
}
}
}
foreach (var row_456 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_445)))))) {
V v_457 = row_456[0];
V v_458 = row_456[1];
R.Call(v_438, R.Join(new V[] {}, new Results(v_458)));
}
return Results.Empty;
}));
foreach (var row_459 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_434)))))) {
V v_460 = row_459[0];
V v_461 = row_459[1];
R.Call(v_438, R.Join(new V[] {}, new Results(v_461)));
}
return Results.Empty;
}));
var values_462 = Results.Empty;
V v_463 = values_462[0];
V v_464 = values_462[1];
R.Call(v_432, R.Join(new V[] {R.Table(new Entry((V)1, R.Get(v_427, (V)"draw")))}, new Results(new V((RuleFunction)(args_465 => {
V v_466 = args_465[0];
V v_467 = args_465[1];
V v_468 = args_465[2];
V v_469 = args_465[3];
if (R.Truth((V)(v_468 == (V)"draw_world"))) {
var values_470 = R.Join(new V[] {}, new Results(v_469));
v_463 = values_470[0];
} else if (R.Truth((V)(v_468 == (V)"draw_panel"))) {
var values_471 = R.Join(new V[] {}, new Results(v_469));
v_464 = values_471[0];
}
return Results.Empty;
})))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_463, () => v_464))));
var values_472 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)1.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_473 = values_472[0];

var values_474 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_visuals"))).First, (V)"new"), R.Join(new V[] {v_16, v_23}, new Results(v_36))));
V v_475 = values_474[0];
var values_479 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"generated", v_475), new Entry((V)"has_terrain", (V)true), new Entry((V)"resolve", new V((RuleFunction)(args_476 => {
V v_477 = args_476[0];
V v_478 = args_476[1];
return R.Join(new V[] {}, R.Invoke(v_475, "resolve", R.Join(new V[] {}, new Results(v_478))));
return Results.Empty;
}))))));
V v_480 = values_479[0];
var values_481 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_473))));
V v_482 = values_481[0];

var values_483 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.camera"))).First, (V)"new"), R.Join(new V[] {R.Get(v_473, (V)"generated_map"), R.Table(new Entry((V)"x", (V)16.0d), new Entry((V)"y", (V)16.0d), new Entry((V)"width", (V)992.0d), new Entry((V)"height", (V)520.0d)), (V)40.0d, R.Get(R.Get(v_482, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_482, (V)"position"), (V)"y")))));
V v_484 = values_483[0];
var values_489 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_485 => {
V v_486 = args_485[0];
V v_487 = args_485[1];
return R.Join(new V[] {}, new Results((V)((V)R.Length(v_487) * (V)7.0d)));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_488 => {

return R.Join(new V[] {}, new Results((V)14.0d));
return Results.Empty;
}))))));
V v_490 = values_489[0];
var values_491 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_473), new Entry((V)"visual_assets", v_480), new Entry((V)"camera", v_484), new Entry((V)"spectator_enabled", (V)false), new Entry((V)"minimap_visible", (V)false), new Entry((V)"ui_body_font", v_490), new Entry((V)"ui_display_font", v_490), new Entry((V)"ui_menu_font", v_490))));
V v_492 = values_491[0];
R.Call(v_432, R.Join(new V[] {R.Table(new Entry((V)1, R.Get(v_427, (V)"draw")), new Entry((V)2, R.Get(v_427, (V)"keypressed")))}, new Results(new V((RuleFunction)(args_493 => {
V v_494 = args_493[0];
V v_495 = args_493[1];
V v_496 = args_493[2];
if (R.Truth((V)(R.Get(v_492, v_496) != V.Nil))) {
R.Call(R.Get(rt.Global("debug"), (V)"setupvalue"), R.Join(new V[] {v_494, v_495}, new Results(R.Get(v_492, v_496))));
}
return Results.Empty;
})))));
var values_497 = R.Join(new V[] {}, new Results(R.Table()));
V v_498 = values_497[0];

var values_510 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"draw", new V((RuleFunction)(args_499 => {
V v_500 = args_499[0];
V v_501 = args_499[1];
var values_502 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_498, v_501), () => (V)0.0d) + (V)1.0d)));
var obj_503 = v_498;
V key_504 = v_501;
R.Set(obj_503, key_504, values_502[0]);
return Results.Empty;
}))), new Entry((V)"getFont", new V((RuleFunction)(args_505 => {

return R.Join(new V[] {}, new Results(v_490));
return Results.Empty;
}))), new Entry((V)"getDimensions", new V((RuleFunction)(args_506 => {

return R.Join(new V[] {(V)1024.0d}, new Results((V)640.0d));
return Results.Empty;
}))), new Entry((V)"getWidth", new V((RuleFunction)(args_507 => {

return R.Join(new V[] {}, new Results((V)1024.0d));
return Results.Empty;
}))))}, new Results(R.Table(new Entry((V)"__index", new V((RuleFunction)(args_508 => {

return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_509 => {


return Results.Empty;
}))));
return Results.Empty;
}))))))));
var obj_511 = v_427;
V key_512 = (V)"graphics";
R.Set(obj_511, key_512, values_510[0]);
var values_513 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_473))));
V v_514 = values_513[0];

R.Call(v_463, Results.Empty);

R.Call(v_464, Results.Empty);

R.Invoke(v_475, "set_world", R.Join(new V[] {}, new Results(v_473)));
var values_515 = R.Join(new V[] {}, new Results((V)0.0d));
V v_516 = values_515[0];
foreach (var row_517 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_475, (V)"environment"), (V)"plan"), (V)"props"))))))) {
V v_518 = row_517[0];
V v_519 = row_517[1];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_11, (V)"is_visible"), R.Join(new V[] {v_473, R.Get(v_473, (V)"active_floor_id"), R.Get(v_519, (V)"x")}, new Results(R.Get(v_519, (V)"y")))).First)))) {
var values_520 = R.Join(new V[] {}, new Results((V)(v_516 + (V)1.0d)));
v_516 = values_520[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_498, R.Get(R.Invoke(v_475, "resolve", R.Join(new V[] {}, new Results(R.Get(v_519, (V)"id")))).First, (V)"quad")))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_516 > (V)0.0d), () => R.Get(v_498, R.Get(R.Invoke(v_475, "resolve", R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"weapon"), R.Join(new V[] {R.Get(v_482, (V)"definition_id")}, new Results((V)"basic_attack"))))).First, (V)"quad"))))));
R.Call(v_432, R.Join(new V[] {R.Table(new Entry((V)1, R.Get(v_427, (V)"draw")), new Entry((V)2, R.Get(v_427, (V)"keypressed")))}, new Results(new V((RuleFunction)(args_521 => {
V v_522 = args_521[0];
V v_523 = args_521[1];
V v_524 = args_521[2];
if (R.Truth((V)(v_524 == (V)"spectator_enabled"))) {
R.Call(R.Get(rt.Global("debug"), (V)"setupvalue"), R.Join(new V[] {v_522, v_523}, new Results((V)true)));
}
return Results.Empty;
})))));
if (R.Truth(rt.Global("jit"))) {
R.Call(R.Get(rt.Global("jit"), (V)"flush"), Results.Empty);
}
var values_525 = R.Join(new V[] {}, new Results(R.Table()));
v_498 = values_525[0];

R.Call(v_463, Results.Empty);

R.Call(v_464, Results.Empty);
foreach (var row_526 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_475, (V)"environment"), (V)"plan"), (V)"props"))))))) {
V v_527 = row_526[0];
V v_528 = row_526[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_498, R.Get(R.Invoke(v_475, "resolve", R.Join(new V[] {}, new Results(R.Get(v_528, (V)"id")))).First, (V)"quad"))}, new Results((V)"world dressing absent with hidden minimap")));
}
foreach (var row_529 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_473, (V)"idea_terrain"), (V)"nodes"))))))) {
V v_530 = row_529[0];
V v_531 = row_529[1];
if (R.Truth((V)(R.Get(v_531, (V)"floor_id") == R.Get(v_473, (V)"active_floor_id")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_498, R.Get(R.Invoke(v_475, "resolve", R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))).First, (V)"visual"), R.Join(new V[] {}, new Results(v_531))))).First, (V)"quad"))}, new Results(R.Concat((V)"terrain reward pass absent: ", R.Concat(R.Get(v_531, (V)"kind"), R.Concat((V)" / ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))).First, (V)"visual"), R.Join(new V[] {}, new Results(v_531))))).First))))));
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_473))).First}, new Results(v_514)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(rt.Global("_G"), (V)"love"))))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
