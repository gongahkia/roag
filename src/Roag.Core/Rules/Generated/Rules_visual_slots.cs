// Static C# port of src/visual_slots.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 1c76df030aeba67f690bc03bd7d2c6d64eca1697e9456df6bf5b6cf2641a136b
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visual_slots { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_interaction_content"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"bindings", R.Table()))));
V v_16 = values_15[0];
V v_17 = V.Nil;
v_17 = new V((RuleFunction)(args_18 => {
V v_19 = args_18[0];
var values_20 = R.Join(new V[] {}, new Results((V)true));
var obj_21 = R.Get(v_16, (V)"bindings");
V key_22 = v_19;
R.Set(obj_21, key_22, values_20[0]);
return Results.Empty;
}));
foreach (var row_23 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))).First, (V)"art"))))))) {
V v_24 = row_23[0];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat((V)"core:terrain/", v_24))));
}
var values_25 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_environment_content"))));
V v_26 = values_25[0];
foreach (var row_27 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_26, (V)"material_order"))))))) {
V v_28 = row_27[0];
V v_29 = row_27[1];
R.Call(v_17, R.Join(new V[] {}, R.Call(R.Get(v_26, (V)"material_id"), R.Join(new V[] {}, new Results(v_29)))));
}
foreach (var row_30 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_26, (V)"props"))))))) {
V v_31 = row_30[0];
R.Call(v_17, R.Join(new V[] {}, R.Call(R.Get(v_26, (V)"prop_id"), R.Join(new V[] {}, new Results(v_31)))));
}
foreach (var row_32 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_26, (V)"symbols"))))))) {
V v_33 = row_32[0];
R.Call(v_17, R.Join(new V[] {}, R.Call(R.Get(v_26, (V)"symbol_id"), R.Join(new V[] {}, new Results(v_33)))));
}
foreach (var row_34 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_14, (V)"art"))))))) {
V v_35 = row_34[0];
R.Call(v_17, R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"binding"), R.Join(new V[] {}, new Results(v_35)))));
}
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:object/geomancer_bulwark")));
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:object/bound_spirit")));
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:object/bolt_sentry")));
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:ui/storm_charge_effect")));
foreach (var row_36 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"cues"))))))) {
V v_37 = row_36[0];
V v_38 = row_36[1];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Get(v_38, (V)"id"))));
}
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:object/return_beacon")));
V v_39 = V.Nil;
v_39 = new V((RuleFunction)(args_40 => {
V v_41 = args_40[0];
return R.Join(new V[] {}, R.Invoke(v_41, "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))));
return Results.Empty;
}));
foreach (var row_42 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"door_closed"), new Entry((V)2, (V)"door_open"), new Entry((V)3, (V)"stairs_down"), new Entry((V)4, (V)"stairs_up"), new Entry((V)5, (V)"exit"), new Entry((V)6, (V)"chest_open")))))))) {
V v_43 = row_42[0];
V v_44 = row_42[1];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat((V)"core:object/", v_44))));
}
foreach (var row_45 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"review_secret_clue"), new Entry((V)2, (V)"review_key"), new Entry((V)3, (V)"review_vault_top"), new Entry((V)4, (V)"review_vault_bottom"), new Entry((V)5, (V)"review_gate_top"), new Entry((V)6, (V)"review_gate_bottom"), new Entry((V)7, (V)"review_gate_tiny"), new Entry((V)8, (V)"review_plate_up"), new Entry((V)9, (V)"review_plate_down"), new Entry((V)10, (V)"review_plate_tiny_up"), new Entry((V)11, (V)"review_plate_tiny_down"), new Entry((V)12, (V)"review_ore"), new Entry((V)13, (V)"review_ore_depleted"), new Entry((V)14, (V)"review_brazier_unlit"), new Entry((V)15, (V)"review_brazier_lit"), new Entry((V)16, (V)"review_ward"), new Entry((V)17, (V)"review_mushroom_1"), new Entry((V)18, (V)"review_mushroom_2"), new Entry((V)19, (V)"review_overgrowth_1"), new Entry((V)20, (V)"review_overgrowth_2"), new Entry((V)21, (V)"review_rail_top_left"), new Entry((V)22, (V)"review_rail_top_right"), new Entry((V)23, (V)"review_rail_middle_left"), new Entry((V)24, (V)"review_rail_middle_right"), new Entry((V)25, (V)"review_rail_bottom_left"), new Entry((V)26, (V)"review_rail_bottom_right"), new Entry((V)27, (V)"review_pillar_top"), new Entry((V)28, (V)"review_pillar_middle"), new Entry((V)29, (V)"review_pillar_bottom")))))))) {
V v_46 = row_45[0];
V v_47 = row_45[1];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat((V)"core:object/", v_47))));
}
foreach (var row_48 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"bramble_bed"), new Entry((V)2, (V)"sticky_bog"), new Entry((V)3, (V)"glazed_ice"), new Entry((V)4, (V)"barricade"), new Entry((V)5, (V)"obscuring_mist"), new Entry((V)6, (V)"resonant_crystal")))))))) {
V v_49 = row_48[0];
V v_50 = row_48[1];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat((V)"core:terrain/", v_50))));
}
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:ui/burning_effect")));

R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:ui/block_effect")));
foreach (var row_51 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_10, (V)"cues"))))))) {
V v_52 = row_51[0];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat((V)"core:ui/", v_52))));
}
foreach (var row_53 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_12, (V)"symbols"), () => R.Table()))))))) {
V v_54 = row_53[0];
R.Call(v_17, R.Join(new V[] {}, new Results(v_54)));
}
foreach (var row_55 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_12, (V)"objects"), () => R.Table()))))))) {
V v_56 = row_55[0];
R.Call(v_17, R.Join(new V[] {}, new Results(v_56)));
}
foreach (var row_57 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"attack_center_effect"), new Entry((V)2, (V)"attack_impact_effect"), new Entry((V)3, (V)"weapon_sword_icon"), new Entry((V)4, (V)"weapon_bow_icon"), new Entry((V)5, (V)"weapon_spell_icon"), new Entry((V)6, (V)"weapon_dagger_icon"), new Entry((V)7, (V)"weapon_hook_icon")))))))) {
V v_58 = row_57[0];
V v_59 = row_57[1];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat((V)"core:ui/", v_59))));
}
var values_60 = R.Join(new V[] {}, new Results(R.Table()));
V v_61 = values_60[0];
R.Set(v_16, (V)"glyph_binding", new V((RuleFunction)(args_62 => {
V v_63 = args_62[0];
return R.Join(new V[] {}, new Results(R.Get(v_61, v_63)));
return Results.Empty;
})));
{ double end_66 = R.Num((V)126.0d), step_67 = R.Num((V)1.0d);
for (double i_65 = R.Num((V)32.0d); step_67 > 0 ? i_65 <= end_66 : i_65 >= end_66; i_65 += step_67) {
V v_64 = i_65;
var values_68 = R.Join(new V[] {}, R.Invoke((V)"core:glyph/ascii_%03d", "format", R.Join(new V[] {}, new Results(v_64))));
V v_69 = values_68[0];
var values_70 = R.Join(new V[] {}, new Results(v_69));
var obj_71 = v_61;
V key_72 = R.Call(R.Get(rt.Global("string"), (V)"char"), R.Join(new V[] {}, new Results(v_64))).First;
R.Set(obj_71, key_72, values_70[0]);
R.Call(v_17, R.Join(new V[] {}, new Results(v_69)));
}
}
foreach (var row_73 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"hud_panel"), new Entry((V)2, (V)"hp_icon"), new Entry((V)3, (V)"xp_icon"), new Entry((V)4, (V)"currency_icon"), new Entry((V)5, (V)"title_background"), new Entry((V)6, (V)"pause_background"), new Entry((V)7, (V)"bitmap_font"), new Entry((V)8, (V)"hit_effect"), new Entry((V)9, (V)"boon_effect")))))))) {
V v_74 = row_73[0];
V v_75 = row_73[1];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat((V)"core:ui/", v_75))));
}
var values_76 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"base"), new Entry((V)2, (V)"center"), new Entry((V)3, (V)"edge"), new Entry((V)4, (V)"corner"), new Entry((V)5, (V)"inner_corner"), new Entry((V)6, (V)"edge_n"), new Entry((V)7, (V)"edge_e"), new Entry((V)8, (V)"edge_s"), new Entry((V)9, (V)"edge_w"), new Entry((V)10, (V)"corner_nw"), new Entry((V)11, (V)"corner_ne"), new Entry((V)12, (V)"corner_se"), new Entry((V)13, (V)"corner_sw"), new Entry((V)14, (V)"inner_corner_nw"), new Entry((V)15, (V)"inner_corner_ne"), new Entry((V)16, (V)"inner_corner_se"), new Entry((V)17, (V)"inner_corner_sw"), new Entry((V)18, (V)"horizontal"), new Entry((V)19, (V)"vertical"), new Entry((V)20, (V)"isolated"), new Entry((V)21, (V)"end_n"), new Entry((V)22, (V)"end_e"), new Entry((V)23, (V)"end_s"), new Entry((V)24, (V)"end_w"))));
V v_77 = values_76[0];
foreach (var row_78 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"order"))))))) {
V v_79 = row_78[0];
V v_80 = row_78[1];
R.Call(v_17, R.Join(new V[] {}, new Results(v_80)));

R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat(v_80, (V)"/attack"))));

R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat(v_80, (V)"/hurt"))));
}
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:drop/currency")));
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:drop/currency/detail")));
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:drop/food")));
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:drop/health")));
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:chest/standard")));
R.Call(v_17, R.Join(new V[] {}, new Results((V)"core:chest/free_elite")));
var values_81 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:enemy/thorn_warden", (V)true), new Entry((V)"core:enemy/extraction_pursuer", (V)true), new Entry((V)"core:enemy/hollow_regent", (V)true))));
V v_82 = values_81[0];
foreach (var row_83 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_84 = row_83[0];
V v_85 = row_83[1];
var values_86 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_85))));
V v_87 = values_86[0];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat((V)"core:background/", R.Call(v_39, R.Join(new V[] {}, new Results(v_85))).First))));
var values_88 = R.Join(new V[] {}, new Results((V)true));
var obj_89 = v_82;
V key_90 = R.Get(v_87, (V)"elite_id");
R.Set(obj_89, key_90, values_88[0]);
foreach (var row_91 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_87, (V)"enemy_ids"))))))) {
V v_92 = row_91[0];
V v_93 = row_91[1];
var values_94 = R.Join(new V[] {}, new Results((V)true));
var obj_95 = v_82;
V key_96 = v_93;
R.Set(obj_95, key_96, values_94[0]);
}
foreach (var row_97 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"floor"), new Entry((V)2, (V)"wall"), new Entry((V)3, (V)"water")))))))) {
V v_98 = row_97[0];
V v_99 = row_97[1];
foreach (var row_100 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_77)))))) {
V v_101 = row_100[0];
V v_102 = row_100[1];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat((V)"core:terrain/", R.Concat(R.Call(v_39, R.Join(new V[] {}, new Results(v_85))).First, R.Concat((V)"/", R.Concat(v_99, R.Concat((V)"/", v_102))))))));
}
}
}
foreach (var row_103 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_10, (V)"roles"))))))) {
V v_104 = row_103[0];
var values_105 = R.Join(new V[] {}, new Results((V)true));
var obj_106 = v_82;
V key_107 = v_104;
R.Set(obj_106, key_107, values_105[0]);
}
foreach (var row_108 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_12, (V)"roles"))))))) {
V v_109 = row_108[0];
var values_110 = R.Join(new V[] {}, new Results((V)true));
var obj_111 = v_82;
V key_112 = v_109;
R.Set(obj_111, key_112, values_110[0]);
}
foreach (var row_113 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_82)))))) {
V v_114 = row_113[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_6, (V)"get_enemy"), R.Join(new V[] {}, new Results(v_114))).First}, new Results(R.Concat((V)"unknown visual enemy slot: ", v_114))));
R.Call(v_17, R.Join(new V[] {}, new Results(v_114)));

R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat(v_114, (V)"/attack"))));

R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat(v_114, (V)"/hurt"))));
}
foreach (var row_115 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"order"))))))) {
V v_116 = row_115[0];
V v_117 = row_115[1];
R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat(v_117, (V)"/icon"))));

R.Call(v_17, R.Join(new V[] {}, new Results(R.Concat(v_117, (V)"/detail"))));
}
R.Set(v_16, (V)"pose", new V((RuleFunction)(args_118 => {
V v_119 = args_118[0];
V v_120 = args_118[1];
V v_121 = args_118[2];
var values_122 = R.Join(new V[] {(V)false}, new Results((V)false));
V v_123 = values_122[0];
V v_124 = values_122[1];
foreach (var row_125 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Or(v_121, () => R.And(v_120, () => R.Table(new Entry((V)1, v_120)))), () => R.Table()))))))) {
V v_126 = row_125[0];
V v_127 = row_125[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_127, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_127, (V)"target_id") == v_119)), () => (V)(R.Or(R.Get(v_127, (V)"actual_loss"), () => (V)0.0d) > (V)0.0d)))) {
var values_128 = R.Join(new V[] {}, new Results((V)true));
v_123 = values_128[0];
}
if (R.Truth(R.And(R.Or((V)(R.Get(v_127, (V)"kind") == (V)"AttackPerformed"), () => (V)(R.Get(v_127, (V)"kind") == (V)"WindupStarted")), () => (V)(R.Get(v_127, (V)"source_id") == v_119)))) {
var values_129 = R.Join(new V[] {}, new Results((V)true));
v_124 = values_129[0];
}
}
return R.Join(new V[] {}, new Results(R.Or(R.Or(R.And(v_123, () => (V)"hurt"), () => R.And(v_124, () => (V)"attack")), () => V.Nil)));
return Results.Empty;
})));
R.Set(v_16, (V)"facing", new V((RuleFunction)(args_130 => {
V v_131 = args_130[0];
V v_132 = args_130[1];
if (R.Truth(R.Or((V)(v_132 == (-(V)1.0d)), () => (V)(v_132 == (V)1.0d)))) {
return R.Join(new V[] {}, new Results(v_132));
}
return R.Join(new V[] {}, new Results(R.Or(v_131, () => (V)1.0d)));
return Results.Empty;
})));
R.Set(v_16, (V)"enemy_size", new V((RuleFunction)(args_133 => {
V v_134 = args_133[0];
V v_135 = args_133[1];
if (R.Truth((V)(v_134 == (V)"core:enemy/bone_fragment"))) {
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_135}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)16.0d}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)(v_135 * (V)2.0d) / (V)3.0d) / (V)16.0d)))).First * (V)16.0d)))))));
}
return R.Join(new V[] {}, new Results(v_135));
return Results.Empty;
})));
R.Set(v_16, (V)"horizontal_draw", new V((RuleFunction)(args_136 => {
V v_137 = args_136[0];
V v_138 = args_136[1];
V v_139 = args_136[2];
V v_140 = args_136[3];
var values_141 = R.Join(new V[] {}, new Results((V)(v_138 / v_139)));
V v_142 = values_141[0];
if (R.Truth((V)(v_140 == (-(V)1.0d)))) {
return R.Join(new V[] {(V)(v_137 + v_138)}, new Results((-v_142)));
}
return R.Join(new V[] {v_137}, new Results(v_142));
return Results.Empty;
})));
var values_143 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor", (V)"."), new Entry((V)"wall", (V)"#"), new Entry((V)"water", (V)"~"))));
V v_144 = values_143[0];
R.Set(v_16, (V)"terrain_shape", new V((RuleFunction)(args_145 => {
V v_146 = args_145[0];
V v_147 = args_145[1];
V v_148 = args_145[2];
V v_149 = args_145[3];
var values_150 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_144, v_149)}, new Results((V)"invalid terrain kind"))));
V v_151 = values_150[0];
V v_152 = V.Nil;
v_152 = new V((RuleFunction)(args_153 => {
V v_154 = args_153[0];
V v_155 = args_153[1];
var values_156 = R.Join(new V[] {}, new Results((V)(v_148 + v_155)));
V v_157 = values_156[0];

var values_158 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_146, (V)"rows"), v_157)));
V v_159 = values_158[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(v_159, () => (V)(R.Invoke(v_159, "sub", R.Join(new V[] {(V)(v_147 + v_154)}, new Results((V)(v_147 + v_154)))).First == v_151)), () => (V)false)));
return Results.Empty;
}));
var values_160 = R.Join(new V[] {R.Call(v_152, R.Join(new V[] {(V)0.0d}, new Results((-(V)1.0d)))).First, R.Call(v_152, R.Join(new V[] {(V)1.0d}, new Results((V)0.0d))).First, R.Call(v_152, R.Join(new V[] {(V)0.0d}, new Results((V)1.0d))).First}, R.Call(v_152, R.Join(new V[] {(-(V)1.0d)}, new Results((V)0.0d))));
V v_161 = values_160[0];
V v_162 = values_160[1];
V v_163 = values_160[2];
V v_164 = values_160[3];
var values_165 = R.Join(new V[] {}, new Results((V)((V)((V)(R.Or(R.And(v_161, () => (V)1.0d), () => (V)0.0d) + R.Or(R.And(v_162, () => (V)1.0d), () => (V)0.0d)) + R.Or(R.And(v_163, () => (V)1.0d), () => (V)0.0d)) + R.Or(R.And(v_164, () => (V)1.0d), () => (V)0.0d))));
V v_166 = values_165[0];
if (R.Truth((V)(v_166 == (V)4.0d))) {
foreach (var row_167 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"nw"), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)"ne"), new Entry((V)2, (V)1.0d), new Entry((V)3, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)"se"), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)"sw"), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)1.0d)))))))))) {
V v_168 = row_167[0];
V v_169 = row_167[1];
if (R.Truth((V)(!R.Truth(R.Call(v_152, R.Join(new V[] {R.Get(v_169, (V)2.0d)}, new Results(R.Get(v_169, (V)3.0d)))).First)))) {
return R.Join(new V[] {}, new Results(R.Concat((V)"inner_corner_", R.Get(v_169, (V)1.0d))));
}
}
return R.Join(new V[] {}, new Results((V)"center"));
} else if (R.Truth((V)(v_166 == (V)3.0d))) {
if (R.Truth((V)(!R.Truth(v_161)))) {
return R.Join(new V[] {}, new Results((V)"edge_n"));
}
if (R.Truth((V)(!R.Truth(v_162)))) {
return R.Join(new V[] {}, new Results((V)"edge_e"));
}
if (R.Truth((V)(!R.Truth(v_163)))) {
return R.Join(new V[] {}, new Results((V)"edge_s"));
}
return R.Join(new V[] {}, new Results((V)"edge_w"));
} else if (R.Truth((V)(v_166 == (V)2.0d))) {
if (R.Truth(R.And(v_161, () => v_163))) {
return R.Join(new V[] {}, new Results((V)"vertical"));
}
if (R.Truth(R.And(v_162, () => v_164))) {
return R.Join(new V[] {}, new Results((V)"horizontal"));
}
if (R.Truth(R.And(v_161, () => v_162))) {
return R.Join(new V[] {}, new Results((V)"corner_sw"));
}
if (R.Truth(R.And(v_162, () => v_163))) {
return R.Join(new V[] {}, new Results((V)"corner_nw"));
}
if (R.Truth(R.And(v_163, () => v_164))) {
return R.Join(new V[] {}, new Results((V)"corner_ne"));
}
return R.Join(new V[] {}, new Results((V)"corner_se"));
} else if (R.Truth((V)(v_166 == (V)1.0d))) {
if (R.Truth(v_161)) {
return R.Join(new V[] {}, new Results((V)"end_s"));
}
if (R.Truth(v_162)) {
return R.Join(new V[] {}, new Results((V)"end_w"));
}
if (R.Truth(v_163)) {
return R.Join(new V[] {}, new Results((V)"end_n"));
}
return R.Join(new V[] {}, new Results((V)"end_e"));
}
return R.Join(new V[] {}, new Results((V)"isolated"));
return Results.Empty;
})));
R.Set(v_16, (V)"terrain_bindings", new V((RuleFunction)(args_170 => {
V v_171 = args_170[0];
V v_172 = args_170[1];
V v_173 = args_170[2];
var values_174 = R.Join(new V[] {}, new Results(R.Concat((V)"core:terrain/", R.Concat(R.Call(v_39, R.Join(new V[] {}, new Results(R.Or(v_171, () => R.Get(R.Get(v_2, (V)"order"), (V)1.0d))))).First, R.Concat((V)"/", R.Concat(v_172, (V)"/"))))));
V v_175 = values_174[0];
var values_176 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Concat(v_175, v_173)))));
V v_177 = values_176[0];
var values_178 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.And(R.Invoke(v_173, "match", R.Join(new V[] {}, new Results((V)"^inner_corner_"))).First, () => (V)"inner_corner"), () => R.And(R.Invoke(v_173, "match", R.Join(new V[] {}, new Results((V)"^corner_"))).First, () => (V)"corner")), () => R.And(R.Invoke(v_173, "match", R.Join(new V[] {}, new Results((V)"^edge_"))).First, () => (V)"edge")), () => V.Nil)));
V v_179 = values_178[0];
if (R.Truth(v_179)) {
var values_180 = R.Join(new V[] {}, new Results(R.Concat(v_175, v_179)));
var obj_181 = v_177;
V key_182 = (V)((V)R.Length(v_177) + (V)1.0d);
R.Set(obj_181, key_182, values_180[0]);
}
if (R.Truth((V)(v_173 != (V)"center"))) {
var values_183 = R.Join(new V[] {}, new Results(R.Concat(v_175, (V)"center")));
var obj_184 = v_177;
V key_185 = (V)((V)R.Length(v_177) + (V)1.0d);
R.Set(obj_184, key_185, values_183[0]);
}
var values_186 = R.Join(new V[] {}, new Results(R.Concat(v_175, (V)"base")));
var obj_187 = v_177;
V key_188 = (V)((V)R.Length(v_177) + (V)1.0d);
R.Set(obj_187, key_188, values_186[0]);
return R.Join(new V[] {}, new Results(v_177));
return Results.Empty;
})));
R.Set(v_16, (V)"terrain_alpha", new V((RuleFunction)(args_189 => {
V v_190 = args_189[0];
V v_191 = args_189[1];
V v_192 = args_189[2];
V v_193 = args_189[3];
var values_194 = R.Join(new V[] {}, new Results((V)1.0d));
V v_195 = values_194[0];
if (R.Truth(R.And((V)(v_191 == (V)"generated"), () => (V)(v_193 != (V)"tiled")))) {
if (R.Truth((V)(v_190 == (V)"floor"))) {
var values_196 = R.Join(new V[] {}, new Results((V)0.18d));
v_195 = values_196[0];
} else if (R.Truth((V)(v_190 == (V)"wall"))) {
var values_197 = R.Join(new V[] {}, new Results((V)0.72d));
v_195 = values_197[0];
} else if (R.Truth((V)(v_190 == (V)"water"))) {
var values_198 = R.Join(new V[] {}, new Results((V)0.58d));
v_195 = values_198[0];
}
}
return R.Join(new V[] {}, new Results(R.Or(R.And(v_192, () => (V)(v_195 * (V)0.38d)), () => v_195)));
return Results.Empty;
})));
R.Set(v_16, (V)"terrain_draws_at", new V((RuleFunction)(args_199 => {
V v_200 = args_199[0];
V v_201 = args_199[1];
V v_202 = args_199[2];
V v_203 = args_199[3];
V v_204 = args_199[4];
if (R.Truth(R.Or((V)(v_201 != (V)"generated"), () => (V)(v_204 == (V)"tiled")))) {
return R.Join(new V[] {}, new Results((V)true));
}
var values_205 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_200 == (V)"wall"), () => (V)4.0d), () => R.And((V)(v_200 == (V)"water"), () => (V)3.0d)), () => (V)5.0d)));
V v_206 = values_205[0];
return R.Join(new V[] {}, new Results((V)((V)((V)(v_202 + (V)((V)2.0d * v_203)) % v_206) == (V)0.0d)));
return Results.Empty;
})));
R.Set(v_16, (V)"background_alpha", new V((RuleFunction)(args_207 => {
V v_208 = args_207[0];
V v_209 = args_207[1];
if (R.Truth(R.And((V)(v_208 == (V)"generated"), () => (V)(v_209 == (V)"tiled")))) {
return R.Join(new V[] {}, new Results((V)0.0d));
}
return R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_208 == (V)"generated"), () => (V)0.16d), () => (V)1.0d)));
return Results.Empty;
})));
R.Set(v_16, (V)"wall_edges", new V((RuleFunction)(args_210 => {
V v_211 = args_210[0];
V v_212 = args_210[1];
V v_213 = args_210[2];
V v_214 = args_210[3];
var values_215 = R.Join(new V[] {}, new Results(R.Table()));
V v_216 = values_215[0];
foreach (var row_217 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"n"), new Entry((V)2, (V)0.0d), new Entry((V)3, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)"e"), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)"s"), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)"w"), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)0.0d)))))))))) {
V v_218 = row_217[0];
V v_219 = row_217[1];
var values_220 = R.Join(new V[] {(V)(v_212 + R.Get(v_219, (V)2.0d))}, new Results((V)(v_213 + R.Get(v_219, (V)3.0d))));
V v_221 = values_220[0];
V v_222 = values_220[1];
var values_223 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_211, (V)"rows"), v_222)));
V v_224 = values_223[0];
var values_225 = R.Join(new V[] {}, new Results(R.Or(R.And(v_224, () => R.Invoke(v_224, "sub", R.Join(new V[] {v_221}, new Results(v_221))).First), () => (V)"")));
V v_226 = values_225[0];
var values_227 = R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.And((V)(v_226 != (V)""), () => (V)(v_226 != (V)"#")), () => (V)(v_226 != (V)"B")), () => R.Or((V)(!R.Truth(v_214)), () => R.Call(v_214, R.Join(new V[] {v_221}, new Results(v_222))).First)), () => (V)false)));
var obj_228 = v_216;
V key_229 = R.Get(v_219, (V)1.0d);
R.Set(obj_228, key_229, values_227[0]);
}
return R.Join(new V[] {}, new Results(v_216));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_16));
return Results.Empty;
} }
