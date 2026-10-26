// Static C# port of tests/legacy_kenney_visuals.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: dc6caf181c2231fa2d300f518ecddefab508266eef4efa2e5ba4f823ccdc8c7e
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_legacy_kenney_visuals { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_atlas"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_terrain"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_duelist"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_geomancer"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_stormcaller"))));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_wayfinder"))));
V v_20 = values_19[0];
var values_21 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_interaction_content"))));
V v_22 = values_21[0];
var values_23 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))));
V v_24 = values_23[0];
var values_25 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_environment"))));
V v_26 = values_25[0];
var values_27 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", (V)"assets/kenney/micro-roguelike/tiles.png"), new Entry((V)"checksum", (V)"5407eab40dd3457c"), new Entry((V)"width", (V)128.0d), new Entry((V)"height", (V)80.0d), new Entry((V)"tile", (V)8.0d))));
V v_28 = values_27[0];
var values_29 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:actor/adventurer", (V)5.0d), new Entry((V)"core:actor/ranger", (V)11.0d), new Entry((V)"core:actor/arcanist", (V)6.0d), new Entry((V)"core:actor/berserker", (V)4.0d), new Entry((V)"core:actor/sentinel", (V)10.0d), new Entry((V)"core:actor/alchemist", (V)7.0d), new Entry((V)"core:actor/shade", (V)8.0d), new Entry((V)"core:enemy/mossbound_guard", (V)12.0d), new Entry((V)"core:enemy/thornspitter", (V)20.0d), new Entry((V)"core:enemy/ruin_skitter", (V)25.0d), new Entry((V)"core:enemy/spore_herald", (V)14.0d), new Entry((V)"core:enemy/canal_lancer", (V)22.0d), new Entry((V)"core:enemy/cinder_mender", (V)7.0d), new Entry((V)"core:enemy/cinder_bellkeeper", (V)15.0d), new Entry((V)"core:enemy/glass_charger", (V)29.0d), new Entry((V)"core:enemy/crystal_bombardier", (V)27.0d), new Entry((V)"core:enemy/vault_lurker", (V)31.0d), new Entry((V)"core:enemy/mire_binder", (V)21.0d), new Entry((V)"core:enemy/pack_chorister", (V)28.0d), new Entry((V)"core:enemy/gloom_strider", (V)26.0d), new Entry((V)"core:enemy/forge_conjurer", (V)13.0d), new Entry((V)"core:enemy/mossbreaker", (V)23.0d), new Entry((V)"core:enemy/bastion_bulwark", (V)30.0d), new Entry((V)"core:enemy/thorn_warden", (V)9.0d), new Entry((V)"core:enemy/mycelial_matriarch", (V)14.0d), new Entry((V)"core:enemy/canal_lockmaster", (V)22.0d), new Entry((V)"core:enemy/ash_marshal", (V)15.0d), new Entry((V)"core:enemy/prism_curator", (V)10.0d), new Entry((V)"core:enemy/index_keeper", (V)6.0d), new Entry((V)"core:enemy/ember_crucible", (V)13.0d), new Entry((V)"core:enemy/extraction_pursuer", (V)24.0d), new Entry((V)"core:enemy/hollow_regent", (V)9.0d))));
V v_30 = values_29[0];
var values_31 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(v_20, (V)"beacon_id"), (V)95.0d), new Entry(R.Get(v_16, (V)"sentry_object"), (V)119.0d), new Entry(R.Get(v_14, (V)"block_id"), (V)116.0d), new Entry((V)"core:drop/currency", (V)90.0d), new Entry((V)"core:drop/food", (V)137.0d), new Entry((V)"core:drop/health", (V)135.0d), new Entry((V)"core:chest/standard", (V)75.0d), new Entry((V)"core:chest/free_elite", (V)76.0d), new Entry((V)"core:object/chest_open", (V)77.0d), new Entry((V)"core:object/door_closed", (V)36.0d), new Entry((V)"core:object/door_open", (V)38.0d), new Entry((V)"core:object/stairs_down", (V)37.0d), new Entry((V)"core:object/stairs_up", (V)118.0d), new Entry((V)"core:object/exit", (V)138.0d), new Entry((V)"core:ui/hp_icon", (V)100.0d), new Entry((V)"core:ui/xp_icon", (V)129.0d), new Entry((V)"core:ui/currency_icon", (V)90.0d), new Entry((V)"core:ui/hit_effect", (V)130.0d), new Entry((V)"core:ui/boon_effect", (V)132.0d), new Entry((V)"core:ui/burning_effect", (V)136.0d), new Entry((V)"core:ui/block_effect", (V)102.0d), new Entry((V)"core:ui/attack_center_effect", (V)131.0d), new Entry((V)"core:ui/attack_impact_effect", (V)133.0d), new Entry((V)"core:ui/weapon_sword_icon", (V)70.0d), new Entry((V)"core:ui/weapon_bow_icon", (V)71.0d), new Entry((V)"core:ui/weapon_spell_icon", (V)72.0d), new Entry((V)"core:ui/weapon_dagger_icon", (V)73.0d), new Entry((V)"core:ui/weapon_hook_icon", (V)74.0d), new Entry((V)"core:object/review_ward", (V)106.0d), new Entry((V)"core:object/review_overgrowth_1", (V)84.0d), new Entry((V)"core:object/review_overgrowth_2", (V)85.0d))));
V v_32 = values_31[0];
var values_33 = R.Join(new V[] {}, new Results(v_30));
var obj_34 = v_28;
V key_35 = (V)"actors";
R.Set(obj_34, key_35, values_33[0]);

var values_36 = R.Join(new V[] {}, new Results(v_32));
var obj_37 = v_28;
V key_38 = (V)"objects";
R.Set(obj_37, key_38, values_36[0]);
foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_22, (V)"art"))))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
if (R.Truth((V)(R.Get(v_41, (V)1.0d) == (V)"micro"))) {
var values_42 = R.Join(new V[] {}, new Results(R.Get(v_41, (V)2.0d)));
var obj_43 = v_32;
V key_44 = R.Call(R.Get(v_22, (V)"binding"), R.Join(new V[] {}, new Results(v_40))).First;
R.Set(obj_43, key_44, values_42[0]);
}
}
var values_45 = R.Join(new V[] {}, new Results(R.Table()));
var obj_46 = v_28;
V key_47 = (V)"squad_actors";
R.Set(obj_46, key_47, values_45[0]);
var values_48 = R.Join(new V[] {}, new Results(R.Get(v_10, (V)"tiny")));
var obj_49 = v_28;
V key_50 = (V)"idea_actors";
R.Set(obj_49, key_50, values_48[0]);
foreach (var row_51 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"tiny"))))))) {
V v_52 = row_51[0];
V v_53 = row_51[1];
var values_54 = R.Join(new V[] {}, new Results(v_53));
var obj_55 = R.Get(v_28, (V)"squad_actors");
V key_56 = R.Concat((V)"core:enemy/", v_52);
R.Set(obj_55, key_56, values_54[0]);
}
var values_57 = R.Join(new V[] {}, new Results(R.Get(v_4, (V)"canonical")));
var obj_58 = v_28;
V key_59 = (V)"canonical";
R.Set(obj_58, key_59, values_57[0]);
var values_60 = R.Join(new V[] {}, new Results(R.Get(v_4, (V)"masks")));
var obj_61 = v_28;
V key_62 = (V)"wall_masks";
R.Set(obj_61, key_62, values_60[0]);
R.Set(v_28, (V)"region", new V((RuleFunction)(args_63 => {
V v_64 = args_63[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_64))).First == (V)"number"), () => (V)((V)(v_64 % (V)1.0d) == (V)0.0d)), () => (V)(v_64 >= (V)0.0d)), () => (V)(v_64 < (V)160.0d))}, new Results((V)"invalid Kenney tile index")));
return R.Join(new V[] {(V)((V)(v_64 % (V)16.0d) * (V)8.0d), (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_64 / (V)16.0d)))).First * (V)8.0d), (V)8.0d}, new Results((V)8.0d));
return Results.Empty;
})));
R.Set(v_28, (V)"new", new V((RuleFunction)(args_65 => {
V v_66 = args_65[0];
V v_67 = args_65[1];
V v_68 = args_65[2];
var values_69 = R.Join(new V[] {}, R.Call(v_66, R.Join(new V[] {}, new Results(R.Get(v_28, (V)"path")))));
V v_70 = values_69[0];
if (R.Truth((V)(!R.Truth(v_70)))) {
return R.Join(new V[] {V.Nil}, new Results((V)"absent"));
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, new Results(v_70))).First == R.Get(v_28, (V)"checksum"))}, new Results((V)"corrupt Kenney Micro Roguelike sheet")));
var values_71 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_67, R.Join(new V[] {v_70, R.Get(v_28, (V)"path")}, new Results((V)true))).First}, new Results((V)"unreadable Kenney sprite sheet"))));
V v_72 = values_71[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Invoke(v_72, "getWidth", Results.Empty).First == R.Get(v_28, (V)"width")), () => (V)(R.Invoke(v_72, "getHeight", Results.Empty).First == R.Get(v_28, (V)"height")))}, new Results((V)"Kenney sheet dimensions differ")));
var values_73 = R.Join(new V[] {}, new Results(R.Or(v_68, () => R.Get(R.Get(rt.Global("love"), (V)"graphics"), (V)"newQuad"))));
v_68 = values_73[0];
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"new"), R.Join(new V[] {v_66, v_67}, new Results(v_68))));
V v_75 = values_74[0];
var values_76 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {v_66, v_67}, new Results(v_68))));
V v_77 = values_76[0];
var values_78 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_79 = values_78[0];
V v_80 = values_78[1];
V v_81 = V.Nil;
v_81 = new V((RuleFunction)(args_82 => {
V v_83 = args_82[0];
V v_84 = args_82[1];
var values_85 = R.Join(new V[] {}, new Results(R.Concat(v_83, R.Concat((V)":", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_84))).First))));
V v_86 = values_85[0];
if (R.Truth((V)(!R.Truth(R.Get(v_79, v_86))))) {
var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_28, (V)"region"), R.Join(new V[] {}, new Results(v_83))));
V v_88 = values_87[0];
V v_89 = values_87[1];
V v_90 = values_87[2];
V v_91 = values_87[3];
var values_92 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_72), new Entry((V)"quad", R.Call(v_68, R.Join(new V[] {v_88, v_89, v_90, v_91, R.Get(v_28, (V)"width")}, new Results(R.Get(v_28, (V)"height")))).First), new Entry((V)"width", v_90), new Entry((V)"height", v_91), new Entry((V)"offset_x", v_88), new Entry((V)"offset_y", v_89), new Entry((V)"source", (V)"kenney"), new Entry((V)"tile_index", v_83))));
var obj_93 = v_79;
V key_94 = v_86;
R.Set(obj_93, key_94, values_92[0]);
}
return R.Join(new V[] {}, new Results(R.Get(v_79, v_86)));
return Results.Empty;
}));
foreach (var row_95 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_30)))))) {
V v_96 = row_95[0];
V v_97 = row_95[1];
var values_98 = R.Join(new V[] {}, R.Call(v_81, R.Join(new V[] {v_97}, new Results((V)true))));
var obj_99 = v_80;
V key_100 = v_96;
R.Set(obj_99, key_100, values_98[0]);
var values_101 = R.Join(new V[] {}, new Results(R.Get(v_80, v_96)));
var obj_102 = v_80;
V key_103 = R.Concat(v_96, (V)"/attack");
R.Set(obj_102, key_103, values_101[0]);

var values_104 = R.Join(new V[] {}, new Results(R.Get(v_80, v_96)));
var obj_105 = v_80;
V key_106 = R.Concat(v_96, (V)"/hurt");
R.Set(obj_105, key_106, values_104[0]);
}
var values_107 = R.Join(new V[] {}, new Results(R.Table()));
V v_108 = values_107[0];
V v_109 = V.Nil;
v_109 = new V((RuleFunction)(args_110 => {
V v_111 = args_110[0];
V v_112 = args_110[1];
V v_113 = args_110[2];
V v_114 = args_110[3];
V v_115 = args_110[4];
V v_116 = args_110[5];
V v_117 = args_110[6];
V v_118 = args_110[7];
var values_119 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_66, R.Join(new V[] {}, new Results(v_112))).First}, new Results(R.Concat((V)"missing approved ", R.Concat(v_111, (V)" sheet"))))));
V v_120 = values_119[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, new Results(v_120))).First == v_113)}, new Results(R.Concat((V)"corrupt approved ", R.Concat(v_111, (V)" sheet")))));
var values_121 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_67, R.Join(new V[] {v_120, v_112}, new Results((V)true))).First}, new Results(R.Concat((V)"unreadable approved ", R.Concat(v_111, (V)" sheet"))))));
V v_122 = values_121[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Invoke(v_122, "getWidth", Results.Empty).First == v_114), () => (V)(R.Invoke(v_122, "getHeight", Results.Empty).First == v_115))}, new Results(R.Concat((V)"approved ", R.Concat(v_111, (V)" dimensions differ")))));
var values_123 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_122))));
var obj_124 = v_108;
V key_125 = v_111;
R.Set(obj_124, key_125, values_123[0]);
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_118)))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
var values_129 = R.Join(new V[] {(V)((V)(v_128 % v_117) * v_116)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_128 / v_117)))).First * v_116)));
V v_130 = values_129[0];
V v_131 = values_129[1];
var values_132 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_122), new Entry((V)"quad", R.Call(v_68, R.Join(new V[] {v_130, v_131, (V)16.0d, (V)16.0d, v_114}, new Results(v_115))).First), new Entry((V)"width", (V)16.0d), new Entry((V)"height", (V)16.0d), new Entry((V)"offset_x", v_130), new Entry((V)"offset_y", v_131), new Entry((V)"source", (V)"kenney"), new Entry((V)"approved_sheet", v_111), new Entry((V)"tile_index", v_128))));
V v_133 = values_132[0];
var values_134 = R.Join(new V[] {}, new Results(v_133));
var obj_135 = v_80;
V key_136 = v_127;
R.Set(obj_135, key_136, values_134[0]);
if (R.Truth(R.Or(R.Invoke(v_127, "match", R.Join(new V[] {}, new Results((V)"^core:actor/"))).First, () => R.Invoke(v_127, "match", R.Join(new V[] {}, new Results((V)"^core:enemy/"))).First))) {
var values_137 = R.Join(new V[] {}, new Results(v_133));
var obj_138 = v_80;
V key_139 = R.Concat(v_127, (V)"/attack");
R.Set(obj_138, key_139, values_137[0]);

var values_140 = R.Join(new V[] {}, new Results(v_133));
var obj_141 = v_80;
V key_142 = R.Concat(v_127, (V)"/hurt");
R.Set(obj_141, key_142, values_140[0]);
}
}
return Results.Empty;
}));
var values_143 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:actor/hookrunner", (V)111.0d), new Entry((V)"core:actor/trapper", (V)112.0d), new Entry((V)"core:object/review_gate_tiny", (V)45.0d), new Entry((V)"core:object/review_plate_tiny_up", (V)54.0d), new Entry((V)"core:object/review_plate_tiny_down", (V)55.0d))));
V v_144 = values_143[0];
foreach (var row_145 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_28, (V)"squad_actors"))))))) {
V v_146 = row_145[0];
V v_147 = row_145[1];
var values_148 = R.Join(new V[] {}, new Results(v_147));
var obj_149 = v_144;
V key_150 = v_146;
R.Set(obj_149, key_150, values_148[0]);
}
foreach (var row_151 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_28, (V)"idea_actors"))))))) {
V v_152 = row_151[0];
V v_153 = row_151[1];
var values_154 = R.Join(new V[] {}, new Results(v_153));
var obj_155 = v_144;
V key_156 = v_152;
R.Set(obj_155, key_156, values_154[0]);
}
var values_157 = R.Join(new V[] {}, new Results(R.Get(v_12, (V)"tiny")));
var obj_158 = v_144;
V key_159 = R.Get(v_12, (V)"id");
R.Set(obj_158, key_159, values_157[0]);
var values_160 = R.Join(new V[] {}, new Results(R.Get(v_14, (V)"tiny")));
var obj_161 = v_144;
V key_162 = R.Get(v_14, (V)"id");
R.Set(obj_161, key_162, values_160[0]);
var values_163 = R.Join(new V[] {}, new Results((V)85.0d));
var obj_164 = v_144;
V key_165 = R.Get(v_16, (V)"spirit_id");
R.Set(obj_164, key_165, values_163[0]);
var values_166 = R.Join(new V[] {}, new Results((V)109.0d));
var obj_167 = v_144;
V key_168 = R.Get(v_16, (V)"tinker_id");
R.Set(obj_167, key_168, values_166[0]);
foreach (var row_169 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_22, (V)"art"))))))) {
V v_170 = row_169[0];
V v_171 = row_169[1];
if (R.Truth((V)(R.Get(v_171, (V)1.0d) == (V)"tiny"))) {
var values_172 = R.Join(new V[] {}, new Results(R.Get(v_171, (V)2.0d)));
var obj_173 = v_144;
V key_174 = R.Call(R.Get(v_22, (V)"binding"), R.Join(new V[] {}, new Results(v_170))).First;
R.Set(obj_173, key_174, values_172[0]);
}
}
foreach (var row_175 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"art"))))))) {
V v_176 = row_175[0];
V v_177 = row_175[1];
if (R.Truth((V)(R.Get(v_177, (V)1.0d) == (V)"tiny"))) {
var values_178 = R.Join(new V[] {}, new Results(R.Get(v_177, (V)2.0d)));
var obj_179 = v_144;
V key_180 = R.Call(R.Get(v_24, (V)"binding"), R.Join(new V[] {}, new Results(v_176))).First;
R.Set(obj_179, key_180, values_178[0]);
}
}
R.Call(v_109, R.Join(new V[] {(V)"tiny", (V)"assets/kenney/approved/tiny-dungeon.png", (V)"d407b7ab694fa5d5", (V)203.0d, (V)186.0d, (V)17.0d, (V)12.0d}, new Results(v_144)));
var values_181 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:object/review_key", (V)671.0d), new Entry((V)"core:object/review_vault_top", (V)158.0d), new Entry((V)"core:object/review_vault_bottom", (V)215.0d), new Entry((V)"core:object/review_gate_top", (V)161.0d), new Entry((V)"core:object/review_gate_bottom", (V)218.0d), new Entry((V)"core:object/review_plate_up", (V)959.0d), new Entry((V)"core:object/review_plate_down", (V)961.0d))));
V v_182 = values_181[0];
foreach (var row_183 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_22, (V)"art"))))))) {
V v_184 = row_183[0];
V v_185 = row_183[1];
if (R.Truth((V)(R.Get(v_185, (V)1.0d) == (V)"rpg"))) {
var values_186 = R.Join(new V[] {}, new Results(R.Get(v_185, (V)2.0d)));
var obj_187 = v_182;
V key_188 = R.Call(R.Get(v_22, (V)"binding"), R.Join(new V[] {}, new Results(v_184))).First;
R.Set(obj_187, key_188, values_186[0]);
}
}
foreach (var row_189 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"art"))))))) {
V v_190 = row_189[0];
V v_191 = row_189[1];
if (R.Truth((V)(R.Get(v_191, (V)1.0d) == (V)"rpg"))) {
var values_192 = R.Join(new V[] {}, new Results(R.Get(v_191, (V)2.0d)));
var obj_193 = v_182;
V key_194 = R.Call(R.Get(v_24, (V)"binding"), R.Join(new V[] {}, new Results(v_190))).First;
R.Set(obj_193, key_194, values_192[0]);
}
}
R.Call(v_109, R.Join(new V[] {(V)"rpg", (V)"assets/kenney/approved/roguelike-rpg.png", (V)"a2cba6dbb9cac15d", (V)968.0d, (V)526.0d, (V)17.0d, (V)57.0d}, new Results(v_182)));
foreach (var row_195 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_32)))))) {
V v_196 = row_195[0];
V v_197 = row_195[1];
var values_198 = R.Join(new V[] {}, R.Call(v_81, R.Join(new V[] {v_197}, new Results((V)true))));
var obj_199 = v_80;
V key_200 = v_196;
R.Set(obj_199, key_200, values_198[0]);
}
V v_201 = V.Nil;
v_201 = new V((RuleFunction)(args_202 => {
V v_203 = args_202[0];
var values_204 = R.Join(new V[] {(V)((V)(v_203 % (V)29.0d) * (V)17.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_203 / (V)29.0d)))).First * (V)17.0d)));
V v_205 = values_204[0];
V v_206 = values_204[1];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", R.Get(v_77, (V)"native")), new Entry((V)"quad", R.Call(v_68, R.Join(new V[] {v_205, v_206, (V)16.0d, (V)16.0d, (V)492.0d}, new Results((V)305.0d))).First), new Entry((V)"width", (V)16.0d), new Entry((V)"height", (V)16.0d), new Entry((V)"offset_x", v_205), new Entry((V)"offset_y", v_206), new Entry((V)"source", (V)"kenney"), new Entry((V)"terrain_pack", (V)"caves"), new Entry((V)"tile_index", v_203))));
return Results.Empty;
}));
foreach (var row_207 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:object/review_secret_clue", (V)95.0d), new Entry((V)"core:object/review_ore", (V)329.0d), new Entry((V)"core:object/review_ore_depleted", (V)358.0d), new Entry((V)"core:object/review_brazier_unlit", (V)506.0d), new Entry((V)"core:object/review_brazier_lit", (V)507.0d), new Entry((V)"core:object/review_mushroom_1", (V)87.0d), new Entry((V)"core:object/review_mushroom_2", (V)88.0d), new Entry((V)"core:object/review_rail_top_left", (V)445.0d), new Entry((V)"core:object/review_rail_top_right", (V)446.0d), new Entry((V)"core:object/review_rail_middle_left", (V)474.0d), new Entry((V)"core:object/review_rail_middle_right", (V)475.0d), new Entry((V)"core:object/review_rail_bottom_left", (V)503.0d), new Entry((V)"core:object/review_rail_bottom_right", (V)504.0d), new Entry((V)"core:object/review_pillar_top", (V)145.0d), new Entry((V)"core:object/review_pillar_middle", (V)177.0d), new Entry((V)"core:object/review_pillar_bottom", (V)174.0d)))))))) {
V v_208 = row_207[0];
V v_209 = row_207[1];
var values_210 = R.Join(new V[] {}, R.Call(v_201, R.Join(new V[] {}, new Results(v_209))));
var obj_211 = v_80;
V key_212 = v_208;
R.Set(obj_211, key_212, values_210[0]);
}
foreach (var row_213 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_75, (V)"handles"))))))) {
V v_214 = row_213[0];
V v_215 = row_213[1];
var values_216 = R.Join(new V[] {}, new Results(v_215));
var obj_217 = v_80;
V key_218 = v_214;
R.Set(obj_217, key_218, values_216[0]);
}
var values_219 = R.Join(new V[] {}, new Results(R.Table()));
V v_220 = values_219[0];
foreach (var row_221 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_10, (V)"shared_onebit"), () => R.Table()))))))) {
V v_222 = row_221[0];
V v_223 = row_221[1];
var values_224 = R.Join(new V[] {}, new Results(v_223));
var obj_225 = v_220;
V key_226 = v_222;
R.Set(obj_225, key_226, values_224[0]);
}
var values_227 = R.Join(new V[] {}, new Results((V)175.0d));
var obj_228 = v_220;
V key_229 = R.Get(v_18, (V)"id");
R.Set(obj_228, key_229, values_227[0]);

var values_230 = R.Join(new V[] {}, new Results((V)473.0d));
var obj_231 = v_220;
V key_232 = R.Get(v_20, (V)"id");
R.Set(obj_231, key_232, values_230[0]);
foreach (var row_233 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_220)))))) {
V v_234 = row_233[0];
V v_235 = row_233[1];
var values_236 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"region"), R.Join(new V[] {}, new Results(v_235))));
V v_237 = values_236[0];
V v_238 = values_236[1];
V v_239 = values_236[2];
V v_240 = values_236[3];
var values_241 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", R.Get(v_75, (V)"texture")), new Entry((V)"quad", R.Call(v_68, R.Join(new V[] {v_237, v_238, v_239, v_240, R.Get(v_6, (V)"width")}, new Results(R.Get(v_6, (V)"height")))).First), new Entry((V)"width", v_239), new Entry((V)"height", v_240), new Entry((V)"offset_x", v_237), new Entry((V)"offset_y", v_238), new Entry((V)"source", (V)"kenney"), new Entry((V)"symbol_index", v_235))));
V v_242 = values_241[0];
var values_243 = R.Join(new V[] {}, new Results(v_242));
var obj_244 = v_80;
V key_245 = v_234;
R.Set(obj_244, key_245, values_243[0]);

var values_246 = R.Join(new V[] {}, new Results(v_242));
var obj_247 = v_80;
V key_248 = R.Concat(v_234, (V)"/attack");
R.Set(obj_247, key_248, values_246[0]);

var values_249 = R.Join(new V[] {}, new Results(v_242));
var obj_250 = v_80;
V key_251 = R.Concat(v_234, (V)"/hurt");
R.Set(obj_250, key_251, values_249[0]);
}
foreach (var row_252 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_22, (V)"art"))))))) {
V v_253 = row_252[0];
V v_254 = row_252[1];
if (R.Truth((V)(R.Get(v_254, (V)1.0d) == (V)"onebit"))) {
var values_255 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"region"), R.Join(new V[] {}, new Results(R.Get(v_254, (V)2.0d)))));
V v_256 = values_255[0];
V v_257 = values_255[1];
V v_258 = values_255[2];
V v_259 = values_255[3];
var values_260 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", R.Get(v_75, (V)"texture")), new Entry((V)"quad", R.Call(v_68, R.Join(new V[] {v_256, v_257, v_258, v_259, R.Get(v_6, (V)"width")}, new Results(R.Get(v_6, (V)"height")))).First), new Entry((V)"width", v_258), new Entry((V)"height", v_259), new Entry((V)"offset_x", v_256), new Entry((V)"offset_y", v_257), new Entry((V)"source", (V)"kenney"), new Entry((V)"symbol_index", R.Get(v_254, (V)2.0d)))));
var obj_261 = v_80;
V key_262 = R.Call(R.Get(v_22, (V)"binding"), R.Join(new V[] {}, new Results(v_253))).First;
R.Set(obj_261, key_262, values_260[0]);
}
}
var values_263 = R.Join(new V[] {}, R.Call(v_81, R.Join(new V[] {(V)89.0d}, new Results((V)true))));
var obj_264 = v_80;
V key_265 = (V)"core:boon/ring_of_reversal/icon";
R.Set(obj_264, key_265, values_263[0]);
foreach (var row_266 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"art"))))))) {
V v_267 = row_266[0];
V v_268 = row_266[1];
if (R.Truth((V)(R.Get(v_268, (V)1.0d) == (V)"caves"))) {
var values_269 = R.Join(new V[] {}, R.Call(v_201, R.Join(new V[] {}, new Results(R.Get(v_268, (V)2.0d)))));
var obj_270 = v_80;
V key_271 = R.Call(R.Get(v_24, (V)"binding"), R.Join(new V[] {}, new Results(v_267))).First;
R.Set(obj_270, key_271, values_269[0]);
} else if (R.Truth((V)(R.Get(v_268, (V)1.0d) == (V)"onebit"))) {
var values_272 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"region"), R.Join(new V[] {}, new Results(R.Get(v_268, (V)2.0d)))));
V v_273 = values_272[0];
V v_274 = values_272[1];
V v_275 = values_272[2];
V v_276 = values_272[3];
var values_277 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", R.Get(v_75, (V)"texture")), new Entry((V)"quad", R.Call(v_68, R.Join(new V[] {v_273, v_274, v_275, v_276, R.Get(v_6, (V)"width")}, new Results(R.Get(v_6, (V)"height")))).First), new Entry((V)"width", v_275), new Entry((V)"height", v_276), new Entry((V)"offset_x", v_273), new Entry((V)"offset_y", v_274), new Entry((V)"source", (V)"kenney"), new Entry((V)"symbol_index", R.Get(v_268, (V)2.0d)))));
var obj_278 = v_80;
V key_279 = R.Call(R.Get(v_24, (V)"binding"), R.Join(new V[] {}, new Results(v_267))).First;
R.Set(obj_278, key_279, values_277[0]);
}
}
var values_280 = R.Join(new V[] {}, new Results(R.Get(v_80, (V)"core:boon/ring_of_reversal/icon")));
var obj_281 = v_80;
V key_282 = (V)"core:boon/ring_of_reversal/detail";
R.Set(obj_281, key_282, values_280[0]);
foreach (var row_283 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_77, (V)"bindings"))))))) {
V v_284 = row_283[0];
V v_285 = row_283[1];
var values_286 = R.Join(new V[] {}, new Results(v_285));
var obj_287 = v_80;
V key_288 = v_284;
R.Set(obj_287, key_288, values_286[0]);
}
foreach (var row_289 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_10, (V)"caves_objects"), () => R.Table()))))))) {
V v_290 = row_289[0];
V v_291 = row_289[1];
var values_292 = R.Join(new V[] {}, R.Call(v_201, R.Join(new V[] {}, new Results(v_291))));
var obj_293 = v_80;
V key_294 = v_290;
R.Set(obj_293, key_294, values_292[0]);
}
var values_295 = R.Join(new V[] {}, R.Call(R.Get(v_26, (V)"new"), R.Join(new V[] {(V)"kenney", v_66, v_67, v_68}, new Results(R.Table(new Entry((V)"rpg", R.Get(R.Get(v_108, (V)"rpg"), (V)"texture")), new Entry((V)"caves", R.Get(v_77, (V)"native")), new Entry((V)"onebit", R.Get(v_75, (V)"texture")))))));
V v_296 = values_295[0];
foreach (var row_297 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_296, (V)"handles"))))))) {
V v_298 = row_297[0];
V v_299 = row_297[1];
var values_300 = R.Join(new V[] {}, new Results(v_299));
var obj_301 = v_80;
V key_302 = v_298;
R.Set(obj_301, key_302, values_300[0]);
}
return R.Join(new V[] {R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"handles", v_80), new Entry((V)"region", v_81), new Entry((V)"terrain_mode", (V)"tiled"), new Entry((V)"style", (V)"kenney"), new Entry((V)"signature", R.Concat(R.Get(v_28, (V)"checksum"), R.Concat((V)":", R.Concat(R.Get(v_75, (V)"signature"), R.Concat((V)":", R.Concat(R.Get(v_77, (V)"signature"), R.Concat((V)":d407b7ab694fa5d5:a2cba6dbb9cac15d:", R.Get(v_296, (V)"signature")))))))), new Entry((V)"environment", v_296), new Entry((V)"status", (V)"loaded"), new Entry((V)"wall_regions", R.Get(R.Get(v_77, (V)"walls"), (V)"stone")), new Entry((V)"terrain_renderer", v_77), new Entry((V)"pages", R.Table(new Entry((V)"sprites", R.Table(new Entry((V)"texture", v_72))), new Entry((V)"symbols", R.Table(new Entry((V)"texture", R.Get(v_75, (V)"texture")))), new Entry((V)"terrain", R.Table(new Entry((V)"texture", R.Get(v_77, (V)"native")))), new Entry((V)"surfaces", R.Table(new Entry((V)"texture", R.Get(v_77, (V)"surface")))), new Entry((V)"tiny", R.Get(v_108, (V)"tiny")), new Entry((V)"rpg", R.Get(v_108, (V)"rpg")))))}, new Results(R.Table(new Entry((V)"__index", v_28))))).First}, new Results((V)"loaded"));
return Results.Empty;
})));
R.Set(v_28, "resolve", new V((RuleFunction)(args_303 => {
V v_304 = args_303[0];
V v_305 = args_303[1];
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_304, (V)"handles"), v_305)));
return Results.Empty;
})));
R.Set(v_28, "terrain", new V((RuleFunction)(args_306 => {
V v_307 = args_306[0];
V v_308 = args_306[1];
V v_309 = args_306[2];
V v_310 = args_306[3];
V v_311 = args_306[4];
V v_312 = args_306[5];
return R.Join(new V[] {}, new Results(R.Or(R.Invoke(R.Get(v_307, (V)"environment"), "resolve", R.Join(new V[] {v_308, v_309, v_310, v_311}, new Results(v_312))).First, () => R.Invoke(R.Get(v_307, (V)"terrain_renderer"), "resolve", R.Join(new V[] {v_308, v_309, v_310, v_311}, new Results(v_312))).First)));
return Results.Empty;
})));
R.Set(v_28, "set_world", new V((RuleFunction)(args_313 => {
V v_314 = args_313[0];
V v_315 = args_313[1];
R.Invoke(R.Get(v_314, (V)"environment"), "set_world", R.Join(new V[] {}, new Results(v_315)));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_28));
return Results.Empty;
} }
