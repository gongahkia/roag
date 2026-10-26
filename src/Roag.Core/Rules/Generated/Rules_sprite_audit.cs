// Static C# port of src/sprite_audit.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: a73edc38fcafe49e4a284a3de007d27a5fcd3a1356ee1a25f9e740d441aa37a3
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_sprite_audit { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_visuals"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_terrain"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_slots"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_interaction_content"))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_environment"))));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_atlas"))).First, (V)"checksum")));
V v_20 = values_19[0];
var values_21 = R.Join(new V[] {}, new Results(R.Table()));
V v_22 = values_21[0];
V v_23 = V.Nil;
v_23 = new V((RuleFunction)(args_24 => {

return R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table()}, new Results(R.Table(new Entry((V)"audit_array", (V)true))))));
return Results.Empty;
}));
V v_25 = V.Nil;
v_25 = new V((RuleFunction)(args_26 => {
V v_27 = args_26[0];
var values_28 = R.Join(new V[] {}, new Results(R.Table()));
V v_29 = values_28[0];

foreach (var row_30 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_27)))))) {
V v_31 = row_30[0];
var values_32 = R.Join(new V[] {}, new Results(v_31));
var obj_33 = v_29;
V key_34 = (V)((V)R.Length(v_29) + (V)1.0d);
R.Set(obj_33, key_34, values_32[0]);
}

R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_29)));

return R.Join(new V[] {}, new Results(v_29));
return Results.Empty;
}));
V v_35 = V.Nil;
v_35 = new V((RuleFunction)(args_36 => {
V v_37 = args_36[0];
var values_38 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {v_37}, new Results((V)"rb"))))));
V v_39 = values_38[0];

var values_40 = R.Join(new V[] {}, R.Invoke(v_39, "read", R.Join(new V[] {}, new Results((V)"*a"))));
V v_41 = values_40[0];

R.Invoke(v_39, "close", Results.Empty);

return R.Join(new V[] {}, new Results(v_41));
return Results.Empty;
}));
V v_42 = V.Nil;
v_42 = new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Invoke(v_44, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)8.0d))).First == (V)"\u00c2\u0089PNG\r\n\u001a\n")}, new Results((V)"invalid sprite sheet")));
V v_45 = V.Nil;
v_45 = new V((RuleFunction)(args_46 => {
V v_47 = args_46[0];
var values_48 = R.Join(new V[] {}, R.Invoke(v_44, "byte", R.Join(new V[] {v_47}, new Results((V)(v_47 + (V)3.0d)))));
V v_49 = values_48[0];
V v_50 = values_48[1];
V v_51 = values_48[2];
V v_52 = values_48[3];
return R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)((V)(v_49 * (V)256.0d) + v_50) * (V)256.0d) + v_51) * (V)256.0d) + v_52)));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_53 => {

return R.Join(new V[] {}, R.Call(v_45, R.Join(new V[] {}, new Results((V)17.0d))));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_54 => {

return R.Join(new V[] {}, R.Call(v_45, R.Join(new V[] {}, new Results((V)21.0d))));
return Results.Empty;
}))))));
return Results.Empty;
}));
V v_55 = V.Nil;
v_55 = new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
V v_58 = args_56[1];
V v_59 = args_56[2];
V v_60 = args_56[3];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_57), new Entry((V)"y", v_58), new Entry((V)"w", v_59), new Entry((V)"h", v_60))));
return Results.Empty;
}));
R.Set(v_22, (V)"collect", new V((RuleFunction)(args_61 => {

var values_62 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"version", (V)1.0d), new Entry((V)"sheets", R.Table()), new Entry((V)"packs", R.Table()), new Entry((V)"requirements", R.Call(v_23, Results.Empty).First))));
V v_63 = values_62[0];
foreach (var row_64 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"caves", v_6), new Entry((V)"onebit", v_4)))))))) {
V v_65 = row_64[0];
V v_66 = row_64[1];
var values_67 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", R.Get(v_66, (V)"path")), new Entry((V)"width", R.Get(v_66, (V)"width")), new Entry((V)"height", R.Get(v_66, (V)"height")), new Entry((V)"tile", R.Get(v_66, (V)"tile")), new Entry((V)"stride", R.Or(R.Get(v_66, (V)"stride"), () => R.Get(v_66, (V)"tile"))), new Entry((V)"columns", R.Or(R.Get(v_66, (V)"columns"), () => (V)(R.Get(v_66, (V)"width") / R.Get(v_66, (V)"tile")))), new Entry((V)"checksum", R.Get(v_66, (V)"checksum")))));
var obj_68 = R.Get(v_63, (V)"sheets");
V key_69 = v_65;
R.Set(obj_68, key_69, values_67[0]);
}
var values_70 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", (V)"assets/kenney/approved/tiny-dungeon.png"), new Entry((V)"width", (V)203.0d), new Entry((V)"height", (V)186.0d), new Entry((V)"tile", (V)16.0d), new Entry((V)"stride", (V)17.0d), new Entry((V)"columns", (V)12.0d), new Entry((V)"checksum", (V)"d407b7ab694fa5d5"))));
var obj_71 = R.Get(v_63, (V)"sheets");
V key_72 = (V)"tiny";
R.Set(obj_71, key_72, values_70[0]);
var values_73 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", (V)"assets/kenney/approved/roguelike-rpg.png"), new Entry((V)"width", (V)968.0d), new Entry((V)"height", (V)526.0d), new Entry((V)"tile", (V)16.0d), new Entry((V)"stride", (V)17.0d), new Entry((V)"columns", (V)57.0d), new Entry((V)"checksum", (V)"a2cba6dbb9cac15d"))));
var obj_74 = R.Get(v_63, (V)"sheets");
V key_75 = (V)"rpg";
R.Set(obj_74, key_75, values_73[0]);
foreach (var row_76 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {}, new Results(R.Get(v_8, (V)"bindings"))))))))) {
V v_77 = row_76[0];
V v_78 = row_76[1];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"^core:actor/"))).First, () => R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"^core:enemy/"))).First), () => R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"^core:object/"))).First), () => R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"^core:chest/"))).First), () => R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"^core:boon/"))).First), () => R.And(R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"^core:drop/"))).First, () => (V)(!R.Truth(R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"/detail$"))).First)))), () => R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"^core:terrain/[^/]+$"))).First), () => R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"^core:terrain/.+/base$"))).First), () => R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"^core:ui/.+_icon$"))).First), () => R.Invoke(v_78, "match", R.Join(new V[] {}, new Results((V)"^core:ui/.+_effect$"))).First))) {
var values_79 = R.Join(new V[] {}, new Results(v_78));
var obj_80 = R.Get(v_63, (V)"requirements");
V key_81 = (V)((V)R.Length(R.Get(v_63, (V)"requirements")) + (V)1.0d);
R.Set(obj_80, key_81, values_79[0]);
}
}
foreach (var row_82 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_83 = row_82[0];
V v_84 = row_82[1];
var values_85 = R.Join(new V[] {}, new Results(v_2));
V v_86 = values_85[0];
var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_86, (V)"new"), R.Join(new V[] {v_35, v_42}, new Results(v_55))));
V v_88 = values_87[0];
var values_89 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"bindings", R.Call(v_23, Results.Empty).First), new Entry((V)"missing", R.Call(v_23, Results.Empty).First), new Entry((V)"usage", R.Table()), new Entry((V)"shared_actors", R.Call(v_23, Results.Empty).First), new Entry((V)"actor_count", (V)0.0d), new Entry((V)"actor_silhouettes", (V)0.0d), new Entry((V)"static_pose_aliases", (V)0.0d), new Entry((V)"wall_masks", (V)47.0d), new Entry((V)"water_masks", (V)47.0d))));
V v_90 = values_89[0];
var values_91 = R.Join(new V[] {}, new Results(v_90));
var obj_92 = R.Get(v_63, (V)"packs");
V key_93 = v_84;
R.Set(obj_92, key_93, values_91[0]);
V v_94 = V.Nil;
v_94 = new V((RuleFunction)(args_95 => {
V v_96 = args_95[0];
V v_97 = args_95[1];
V v_98 = args_95[2];
var values_99 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_90, (V)"usage"), v_96), () => R.Table())));
V v_100 = values_99[0];

var values_101 = R.Join(new V[] {}, new Results(v_100));
var obj_102 = R.Get(v_90, (V)"usage");
V key_103 = v_96;
R.Set(obj_102, key_103, values_101[0]);
var values_104 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_100, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_97))).First), () => R.Table())));
V v_105 = values_104[0];

var values_106 = R.Join(new V[] {}, new Results(v_105));
var obj_107 = v_100;
V key_108 = R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_97))).First;
R.Set(obj_107, key_108, values_106[0]);
var values_109 = R.Join(new V[] {}, new Results(v_98));
var obj_110 = v_105;
V key_111 = (V)((V)R.Length(v_105) + (V)1.0d);
R.Set(obj_110, key_111, values_109[0]);
return Results.Empty;
}));
var values_112 = R.Join(new V[] {}, new Results(R.Table()));
V v_113 = values_112[0];
foreach (var row_114 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_63, (V)"requirements"))))))) {
V v_115 = row_114[0];
V v_116 = row_114[1];
var values_117 = R.Join(new V[] {}, R.Invoke(v_88, "resolve", R.Join(new V[] {}, new Results(v_116))));
V v_118 = values_117[0];
if (R.Truth((V)(!R.Truth(v_118)))) {
var values_119 = R.Join(new V[] {}, new Results(v_116));
var obj_120 = R.Get(v_90, (V)"missing");
V key_121 = (V)((V)R.Length(R.Get(v_90, (V)"missing")) + (V)1.0d);
R.Set(obj_120, key_121, values_119[0]);
} else {
var values_122 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_116), new Entry((V)"width", R.Get(v_118, (V)"width")), new Entry((V)"height", R.Get(v_118, (V)"height")), new Entry((V)"index", R.Or(R.Or(R.Get(v_118, (V)"symbol_index"), () => R.Get(v_118, (V)"tile_index")), () => (-(V)1.0d))), new Entry((V)"sheet", R.Or(R.Or(R.Or(R.Or(R.Get(v_118, (V)"approved_sheet"), () => R.Get(v_118, (V)"terrain_pack")), () => R.And(R.Get(v_118, (V)"symbol_index"), () => (V)"onebit")), () => R.And(R.Get(v_118, (V)"tile_index"), () => (V)"micro")), () => R.Or(R.And((V)(v_84 == (V)"kenney"), () => (V)"caves-composite-or-floor"), () => (V)"onebit-composite"))), new Entry((V)"motif", R.Or(R.Get(v_118, (V)"motif"), () => (V)"")), new Entry((V)"derived", R.Or(R.Get(v_118, (V)"derived"), () => V.Nil)), new Entry((V)"monochrome", R.Or(R.Get(v_118, (V)"monochrome"), () => V.Nil)), new Entry((V)"pose_alias", R.Or((V)(R.Invoke(v_116, "match", R.Join(new V[] {}, new Results((V)"/attack$"))).First != V.Nil), () => (V)(R.Invoke(v_116, "match", R.Join(new V[] {}, new Results((V)"/hurt$"))).First != V.Nil))))));
var obj_123 = R.Get(v_90, (V)"bindings");
V key_124 = (V)((V)R.Length(R.Get(v_90, (V)"bindings")) + (V)1.0d);
R.Set(obj_123, key_124, values_122[0]);
}
}
foreach (var row_125 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))).First, (V)"art"))))))) {
V v_126 = row_125[0];
var values_127 = R.Join(new V[] {}, new Results(R.Concat((V)"core:terrain/", v_126)));
V v_128 = values_127[0];

var values_129 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_88, "resolve", R.Join(new V[] {}, new Results(v_128))))));
V v_130 = values_129[0];
R.Call(v_94, R.Join(new V[] {R.Or(R.Or(R.Get(v_130, (V)"approved_sheet"), () => R.Get(v_130, (V)"terrain_pack")), () => (V)"onebit"), R.Or(R.Get(v_130, (V)"tile_index"), () => R.Get(v_130, (V)"symbol_index"))}, new Results(v_128)));
}
var values_131 = R.Join(new V[] {}, new Results(R.Get(v_18, (V)"content")));
V v_132 = values_131[0];
foreach (var row_133 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_132, (V)"material_order"))))))) {
V v_134 = row_133[0];
V v_135 = row_133[1];
var values_136 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"material"), R.Join(new V[] {v_135}, new Results(v_84))));
V v_137 = values_136[0];
foreach (var row_138 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_137, (V)"grid"), () => R.Table(new Entry((V)1, R.Get(v_137, (V)"center")))))))))) {
V v_139 = row_138[0];
V v_140 = row_138[1];
R.Call(v_94, R.Join(new V[] {R.Get(v_137, (V)"sheet"), v_140}, new Results(R.Concat((V)"environment ", v_135))));
}
foreach (var row_141 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_137, (V)"inner"), () => R.Table()))))))) {
V v_142 = row_141[0];
V v_143 = row_141[1];
R.Call(v_94, R.Join(new V[] {R.Get(v_137, (V)"sheet"), v_143}, new Results(R.Concat((V)"environment ", R.Concat(v_135, (V)" concave corner")))));
}
if (R.Truth(R.Get(v_137, (V)"frame"))) {
R.Call(v_94, R.Join(new V[] {R.Get(v_137, (V)"sheet"), R.Get(v_137, (V)"frame")}, new Results(R.Concat((V)"environment ", R.Concat(v_135, (V)" frame")))));
}
if (R.Truth(R.Get(v_137, (V)"accent"))) {
R.Call(v_94, R.Join(new V[] {R.Get(v_137, (V)"sheet"), R.Get(v_137, (V)"accent")}, new Results(R.Concat((V)"environment ", R.Concat(v_135, (V)" accent")))));
}
}
foreach (var row_144 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_132, (V)"props"))))))) {
V v_145 = row_144[0];
var values_146 = R.Join(new V[] {}, R.Call(R.Get(v_132, (V)"prop_id"), R.Join(new V[] {}, new Results(v_145))));
V v_147 = values_146[0];

var values_148 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_88, "resolve", R.Join(new V[] {}, new Results(v_147))))));
V v_149 = values_148[0];
R.Call(v_94, R.Join(new V[] {R.Get(v_149, (V)"approved_sheet"), R.Get(v_149, (V)"tile_index")}, new Results(v_147)));
}
foreach (var row_150 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_132, (V)"symbols"))))))) {
V v_151 = row_150[0];
V v_152 = row_150[1];
R.Call(v_94, R.Join(new V[] {(V)"onebit", v_152}, R.Call(R.Get(v_132, (V)"symbol_id"), R.Join(new V[] {}, new Results(v_151)))));
}
foreach (var row_153 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_63, (V)"requirements"))))))) {
V v_154 = row_153[0];
V v_155 = row_153[1];
if (R.Truth(R.Or(R.Invoke(v_155, "match", R.Join(new V[] {}, new Results((V)"^core:actor/[^/]+$"))).First, () => R.Invoke(v_155, "match", R.Join(new V[] {}, new Results((V)"^core:enemy/[^/]+$"))).First))) {
var values_156 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_88, "resolve", R.Join(new V[] {}, new Results(v_155))))));
V v_157 = values_156[0];
var values_158 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Get(v_157, (V)"approved_sheet"), () => R.And(R.Get(v_157, (V)"symbol_index"), () => (V)"onebit")), () => (V)"micro")));
V v_159 = values_158[0];
var values_160 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_157, (V)"symbol_index"), () => R.Get(v_157, (V)"tile_index"))));
V v_161 = values_160[0];
R.Call(v_94, R.Join(new V[] {v_159, v_161}, new Results(v_155)));
var values_162 = R.Join(new V[] {}, new Results(R.Concat(v_159, R.Concat((V)":", v_161))));
V v_163 = values_162[0];
var values_164 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_113, v_163), () => R.Table(new Entry((V)"index", v_161), new Entry((V)"sheet", v_159), new Entry((V)"ids", R.Table())))));
var obj_165 = v_113;
V key_166 = v_163;
R.Set(obj_165, key_166, values_164[0]);
var values_167 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_113, v_163), (V)"ids")));
V v_168 = values_167[0];

var values_169 = R.Join(new V[] {}, new Results(v_155));
var obj_170 = v_168;
V key_171 = (V)((V)R.Length(v_168) + (V)1.0d);
R.Set(obj_170, key_171, values_169[0]);
var values_172 = R.Join(new V[] {}, new Results((V)(R.Get(v_90, (V)"actor_count") + (V)1.0d)));
var obj_173 = v_90;
V key_174 = (V)"actor_count";
R.Set(obj_173, key_174, values_172[0]);
var values_175 = R.Join(new V[] {}, new Results((V)(R.Get(v_90, (V)"static_pose_aliases") + (V)2.0d)));
var obj_176 = v_90;
V key_177 = (V)"static_pose_aliases";
R.Set(obj_176, key_177, values_175[0]);
}
}
foreach (var row_178 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {}, new Results(v_113)))))))) {
V v_179 = row_178[0];
V v_180 = row_178[1];
var values_181 = R.Join(new V[] {}, new Results((V)(R.Get(v_90, (V)"actor_silhouettes") + (V)1.0d)));
var obj_182 = v_90;
V key_183 = (V)"actor_silhouettes";
R.Set(obj_182, key_183, values_181[0]);
if (R.Truth((V)((V)R.Length(R.Get(R.Get(v_113, v_180), (V)"ids")) > (V)1.0d))) {
var values_184 = R.Join(new V[] {}, new Results(R.Get(v_113, v_180)));
var obj_185 = R.Get(v_90, (V)"shared_actors");
V key_186 = (V)((V)R.Length(R.Get(v_90, (V)"shared_actors")) + (V)1.0d);
R.Set(obj_185, key_186, values_184[0]);
}
}
foreach (var row_187 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {}, new Results(R.Get(v_86, (V)"objects"))))))))) {
V v_188 = row_187[0];
V v_189 = row_187[1];
R.Call(v_94, R.Join(new V[] {R.Or(R.And((V)(v_84 == (V)"kenney"), () => (V)"micro"), () => (V)"onebit"), R.Get(R.Get(v_86, (V)"objects"), v_189)}, new Results(v_189)));
}
foreach (var row_190 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {}, new Results(R.Get(v_4, (V)"boons"))))))))) {
V v_191 = row_190[0];
V v_192 = row_190[1];
var values_193 = R.Join(new V[] {}, R.Invoke(v_88, "resolve", R.Join(new V[] {}, new Results(R.Concat(v_192, (V)"/icon")))));
V v_194 = values_193[0];
R.Call(v_94, R.Join(new V[] {R.Or(R.Or(R.Get(v_194, (V)"approved_sheet"), () => R.Get(v_194, (V)"terrain_pack")), () => R.Or(R.And(R.Get(v_194, (V)"symbol_index"), () => (V)"onebit"), () => (V)"micro")), R.Or(R.Get(v_194, (V)"symbol_index"), () => R.Get(v_194, (V)"tile_index"))}, new Results(R.Concat(v_192, (V)"/icon + /detail"))));
}
foreach (var row_195 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {}, new Results(R.Get(v_4, (V)"terrain"))))))))) {
V v_196 = row_195[0];
V v_197 = row_195[1];
R.Call(v_94, R.Join(new V[] {(V)"onebit", R.Get(R.Get(R.Get(v_4, (V)"terrain"), v_197), (V)1.0d)}, new Results(v_197)));
}
foreach (var row_198 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {}, new Results(R.Get(v_10, (V)"cues"))))))))) {
V v_199 = row_198[0];
V v_200 = row_198[1];
R.Call(v_94, R.Join(new V[] {(V)"onebit", R.Get(R.Get(v_10, (V)"cues"), v_200)}, new Results(R.Concat((V)"core:ui/", v_200))));
}
foreach (var row_201 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {}, new Results(R.Or(R.Get(v_12, (V)"symbols"), () => R.Table()))))))))) {
V v_202 = row_201[0];
V v_203 = row_201[1];
R.Call(v_94, R.Join(new V[] {(V)"onebit", R.Get(R.Get(v_12, (V)"symbols"), v_203)}, new Results(v_203)));
}
foreach (var row_204 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"floor"), new Entry((V)2, (V)"wall"), new Entry((V)3, (V)"water"), new Entry((V)4, (V)"frame")))))))) {
V v_205 = row_204[0];
V v_206 = row_204[1];
R.Call(v_94, R.Join(new V[] {(V)"onebit", R.Get(v_2, R.Concat(v_206, (V)"_index"))}, new Results(R.Concat((V)"composite terrain material: ", v_206))));
}
foreach (var row_207 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_90, (V)"usage"))))))) {
V v_208 = row_207[0];
V v_209 = row_207[1];
foreach (var row_210 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_209)))))) {
V v_211 = row_210[0];
V v_212 = row_210[1];
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_212)));
}
}
}
var values_213 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"json"), R.Join(new V[] {}, new Results(v_63))))));
var obj_214 = v_63;
V key_215 = (V)"mapping_checksum";
R.Set(obj_214, key_215, values_213[0]);
return R.Join(new V[] {}, new Results(v_63));
return Results.Empty;
})));
R.Set(v_22, (V)"json", new V((RuleFunction)(args_216 => {
V v_217 = args_216[0];
var values_218 = R.Join(new V[] {}, R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_217))));
V v_219 = values_218[0];
if (R.Truth((V)(v_219 == (V)"string"))) {
return R.Join(new V[] {}, new Results(R.Concat((V)"\"", R.Concat(R.Invoke(v_217, "gsub", R.Join(new V[] {(V)"[%z\u0001-\u001f\\\"]"}, new Results(new V((RuleFunction)(args_220 => {
V v_221 = args_220[0];
return R.Join(new V[] {}, R.Invoke((V)"\\u%04x", "format", R.Join(new V[] {}, R.Invoke(v_221, "byte", Results.Empty))));
return Results.Empty;
}))))).First, (V)"\""))));
}
if (R.Truth(R.Or((V)(v_219 == (V)"number"), () => (V)(v_219 == (V)"boolean")))) {
return R.Join(new V[] {}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_217))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_219 == (V)"table")}, new Results((V)"unsupported audit value")));
var values_222 = R.Join(new V[] {}, new Results(R.Table()));
V v_223 = values_222[0];
if (R.Truth(R.Or((V)((V)R.Length(v_217) > (V)0.0d), () => R.Get(R.Or(R.Call(rt.Global("getmetatable"), R.Join(new V[] {}, new Results(v_217))).First, () => R.Table()), (V)"audit_array")))) {
foreach (var row_224 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_217)))))) {
V v_225 = row_224[0];
V v_226 = row_224[1];
var values_227 = R.Join(new V[] {}, R.Call(R.Get(v_22, (V)"json"), R.Join(new V[] {}, new Results(v_226))));
var obj_228 = v_223;
V key_229 = (V)((V)R.Length(v_223) + (V)1.0d);
R.Set(obj_228, key_229, values_227[0]);
}
return R.Join(new V[] {}, new Results(R.Concat((V)"[", R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_223}, new Results((V)","))).First, (V)"]"))));
}
foreach (var row_230 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {}, new Results(v_217)))))))) {
V v_231 = row_230[0];
V v_232 = row_230[1];
var values_233 = R.Join(new V[] {}, new Results(R.Concat(R.Call(R.Get(v_22, (V)"json"), R.Join(new V[] {}, new Results(v_232))).First, R.Concat((V)":", R.Call(R.Get(v_22, (V)"json"), R.Join(new V[] {}, new Results(R.Get(v_217, v_232)))).First))));
var obj_234 = v_223;
V key_235 = (V)((V)R.Length(v_223) + (V)1.0d);
R.Set(obj_234, key_235, values_233[0]);
}
return R.Join(new V[] {}, new Results(R.Concat((V)"{", R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_223}, new Results((V)","))).First, (V)"}"))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_22));
return Results.Empty;
} }
