// Static C# port of src/onebit_visuals.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: d1eebdb2a3a0f24a06c044de78e7c8abed24e4f79789c7195d3407e9223aeeef
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_onebit_visuals { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_terrain"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
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
var values_27 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", R.Get(v_2, (V)"path")), new Entry((V)"checksum", R.Get(v_2, (V)"checksum")), new Entry((V)"width", (V)784.0d), new Entry((V)"height", (V)352.0d), new Entry((V)"tile", (V)16.0d), new Entry((V)"ink", R.Table(new Entry((V)1, (V)0.9d), new Entry((V)2, (V)0.9d), new Entry((V)3, (V)0.9d))), new Entry((V)"background", R.Table(new Entry((V)1, (V)0.045d), new Entry((V)2, (V)0.045d), new Entry((V)3, (V)0.045d))), new Entry((V)"floor_index", (V)5.0d), new Entry((V)"wall_index", (V)16.0d), new Entry((V)"water_index", (V)268.0d), new Entry((V)"frame_index", (V)169.0d))));
V v_28 = values_27[0];
var values_29 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:actor/adventurer", (V)468.0d), new Entry((V)"core:actor/ranger", (V)472.0d), new Entry((V)"core:actor/arcanist", (V)467.0d), new Entry((V)"core:actor/berserker", (V)226.0d), new Entry((V)"core:actor/sentinel", (V)324.0d), new Entry((V)"core:actor/alchemist", (V)469.0d), new Entry((V)"core:actor/shade", (V)79.0d), new Entry((V)"core:actor/hookrunner", (V)80.0d), new Entry((V)"core:actor/trapper", (V)77.0d), new Entry((V)"core:enemy/mossbound_guard", (V)275.0d), new Entry((V)"core:enemy/thornspitter", (V)171.0d), new Entry((V)"core:enemy/ruin_skitter", (V)459.0d), new Entry((V)"core:enemy/spore_herald", (V)418.0d), new Entry((V)"core:enemy/canal_lancer", (V)26.0d), new Entry((V)"core:enemy/cinder_mender", (V)75.0d), new Entry((V)"core:enemy/cinder_bellkeeper", (V)32.0d), new Entry((V)"core:enemy/glass_charger", (V)410.0d), new Entry((V)"core:enemy/crystal_bombardier", (V)271.0d), new Entry((V)"core:enemy/vault_lurker", (V)31.0d), new Entry((V)"core:enemy/mire_binder", (V)420.0d), new Entry((V)"core:enemy/pack_chorister", (V)461.0d), new Entry((V)"core:enemy/gloom_strider", (V)367.0d), new Entry((V)"core:enemy/forge_conjurer", (V)471.0d), new Entry((V)"core:enemy/mossbreaker", (V)325.0d), new Entry((V)"core:enemy/bastion_bulwark", (V)274.0d), new Entry((V)"core:enemy/thorn_warden", (V)272.0d), new Entry((V)"core:enemy/mycelial_matriarch", (V)419.0d), new Entry((V)"core:enemy/canal_lockmaster", (V)128.0d), new Entry((V)"core:enemy/ash_marshal", (V)43.0d), new Entry((V)"core:enemy/prism_curator", (V)227.0d), new Entry((V)"core:enemy/index_keeper", (V)76.0d), new Entry((V)"core:enemy/ember_crucible", (V)273.0d), new Entry((V)"core:enemy/extraction_pursuer", (V)421.0d), new Entry((V)"core:enemy/hollow_regent", (V)269.0d))));
var obj_30 = v_28;
V key_31 = (V)"actors";
R.Set(obj_30, key_31, values_29[0]);
foreach (var row_32 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"onebit"))))))) {
V v_33 = row_32[0];
V v_34 = row_32[1];
var values_35 = R.Join(new V[] {}, new Results(v_34));
var obj_36 = R.Get(v_28, (V)"actors");
V key_37 = R.Concat((V)"core:enemy/", v_33);
R.Set(obj_36, key_37, values_35[0]);
}
foreach (var row_38 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_10, (V)"onebit"))))))) {
V v_39 = row_38[0];
V v_40 = row_38[1];
var values_41 = R.Join(new V[] {}, new Results(v_40));
var obj_42 = R.Get(v_28, (V)"actors");
V key_43 = v_39;
R.Set(obj_42, key_43, values_41[0]);
}
var values_44 = R.Join(new V[] {}, new Results(R.Get(v_12, (V)"onebit")));
var obj_45 = R.Get(v_28, (V)"actors");
V key_46 = R.Get(v_12, (V)"id");
R.Set(obj_45, key_46, values_44[0]);
var values_47 = R.Join(new V[] {}, new Results(R.Get(v_14, (V)"onebit")));
var obj_48 = R.Get(v_28, (V)"actors");
V key_49 = R.Get(v_14, (V)"id");
R.Set(obj_48, key_49, values_47[0]);
var values_50 = R.Join(new V[] {}, new Results((V)122.0d));
var obj_51 = R.Get(v_28, (V)"actors");
V key_52 = R.Get(v_16, (V)"spirit_id");
R.Set(obj_51, key_52, values_50[0]);
var values_53 = R.Join(new V[] {}, new Results((V)222.0d));
var obj_54 = R.Get(v_28, (V)"actors");
V key_55 = R.Get(v_16, (V)"tinker_id");
R.Set(obj_54, key_55, values_53[0]);
var values_56 = R.Join(new V[] {}, new Results((V)175.0d));
var obj_57 = R.Get(v_28, (V)"actors");
V key_58 = R.Get(v_18, (V)"id");
R.Set(obj_57, key_58, values_56[0]);
var values_59 = R.Join(new V[] {}, new Results((V)473.0d));
var obj_60 = R.Get(v_28, (V)"actors");
V key_61 = R.Get(v_20, (V)"id");
R.Set(obj_60, key_61, values_59[0]);
var values_62 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(v_20, (V)"beacon_id"), (V)408.0d), new Entry((V)"core:ui/storm_charge_effect", (V)615.0d), new Entry(R.Get(v_16, (V)"sentry_object"), (V)262.0d), new Entry(R.Get(v_16, (V)"spirit_object"), (V)417.0d), new Entry(R.Get(v_14, (V)"block_id"), (V)262.0d), new Entry((V)"core:drop/currency", (V)818.0d), new Entry((V)"core:drop/food", (V)238.0d), new Entry((V)"core:drop/health", (V)670.0d), new Entry((V)"core:chest/standard", (V)302.0d), new Entry((V)"core:chest/free_elite", (V)301.0d), new Entry((V)"core:object/chest_open", (V)344.0d), new Entry((V)"core:object/door_closed", (V)548.0d), new Entry((V)"core:object/door_open", (V)443.0d), new Entry((V)"core:object/stairs_down", (V)295.0d), new Entry((V)"core:object/stairs_up", (V)296.0d), new Entry((V)"core:object/exit", (V)451.0d), new Entry((V)"core:ui/hp_icon", (V)530.0d), new Entry((V)"core:ui/xp_icon", (V)706.0d), new Entry((V)"core:ui/currency_icon", (V)818.0d), new Entry((V)"core:ui/hit_effect", (V)618.0d), new Entry((V)"core:ui/boon_effect", (V)616.0d), new Entry((V)"core:ui/burning_effect", (V)567.0d), new Entry((V)"core:ui/block_effect", (V)233.0d), new Entry((V)"core:ui/attack_center_effect", (V)625.0d), new Entry((V)"core:ui/attack_impact_effect", (V)626.0d), new Entry((V)"core:ui/weapon_sword_icon", (V)327.0d), new Entry((V)"core:ui/weapon_bow_icon", (V)328.0d), new Entry((V)"core:ui/weapon_spell_icon", (V)329.0d), new Entry((V)"core:ui/weapon_dagger_icon", (V)331.0d), new Entry((V)"core:ui/weapon_hook_icon", (V)332.0d), new Entry((V)"core:object/review_secret_clue", (V)3.0d), new Entry((V)"core:object/review_key", (V)571.0d), new Entry((V)"core:object/review_vault_bottom", (V)552.0d), new Entry((V)"core:object/review_vault_top", (V)552.0d), new Entry((V)"core:object/review_gate_bottom", (V)607.0d), new Entry((V)"core:object/review_gate_top", (V)607.0d), new Entry((V)"core:object/review_plate_up", (V)111.0d), new Entry((V)"core:object/review_plate_down", (V)112.0d), new Entry((V)"core:object/review_gate_tiny", (V)607.0d), new Entry((V)"core:object/review_plate_tiny_up", (V)111.0d), new Entry((V)"core:object/review_plate_tiny_down", (V)112.0d), new Entry((V)"core:object/review_ore", (V)6.0d), new Entry((V)"core:object/review_ore_depleted", (V)524.0d), new Entry((V)"core:object/review_brazier_unlit", (V)738.0d), new Entry((V)"core:object/review_brazier_lit", (V)739.0d), new Entry((V)"core:object/review_ward", (V)578.0d), new Entry((V)"core:object/review_mushroom_1", (V)103.0d), new Entry((V)"core:object/review_mushroom_2", (V)54.0d), new Entry((V)"core:object/review_overgrowth_1", (V)49.0d), new Entry((V)"core:object/review_overgrowth_2", (V)50.0d), new Entry((V)"core:object/review_rail_top_left", (V)245.0d), new Entry((V)"core:object/review_rail_top_right", (V)245.0d), new Entry((V)"core:object/review_rail_middle_left", (V)294.0d), new Entry((V)"core:object/review_rail_middle_right", (V)294.0d), new Entry((V)"core:object/review_rail_bottom_left", (V)294.0d), new Entry((V)"core:object/review_rail_bottom_right", (V)294.0d), new Entry((V)"core:object/review_pillar_top", (V)542.0d), new Entry((V)"core:object/review_pillar_middle", (V)591.0d), new Entry((V)"core:object/review_pillar_bottom", (V)640.0d))));
var obj_63 = v_28;
V key_64 = (V)"objects";
R.Set(obj_63, key_64, values_62[0]);
var values_65 = R.Join(new V[] {}, new Results(R.Get(v_4, (V)"masks")));
var obj_66 = v_28;
V key_67 = (V)"masks";
R.Set(obj_66, key_67, values_65[0]);
foreach (var row_68 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_22, (V)"art"))))))) {
V v_69 = row_68[0];
V v_70 = row_68[1];
var values_71 = R.Join(new V[] {}, new Results(R.Get(v_70, (V)3.0d)));
var obj_72 = R.Get(v_28, (V)"objects");
V key_73 = R.Call(R.Get(v_22, (V)"binding"), R.Join(new V[] {}, new Results(v_69))).First;
R.Set(obj_72, key_73, values_71[0]);
}
foreach (var row_74 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"art"))))))) {
V v_75 = row_74[0];
V v_76 = row_74[1];
var values_77 = R.Join(new V[] {}, new Results(R.Get(v_76, (V)3.0d)));
var obj_78 = R.Get(v_28, (V)"objects");
V key_79 = R.Call(R.Get(v_24, (V)"binding"), R.Join(new V[] {}, new Results(v_75))).First;
R.Set(obj_78, key_79, values_77[0]);
}
foreach (var row_80 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_10, (V)"objects"), () => R.Table()))))))) {
V v_81 = row_80[0];
V v_82 = row_80[1];
var values_83 = R.Join(new V[] {}, new Results(v_82));
var obj_84 = R.Get(v_28, (V)"objects");
V key_85 = v_81;
R.Set(obj_84, key_85, values_83[0]);
}
var values_86 = R.Join(new V[] {}, new Results(R.Get(v_4, (V)"canonical")));
var obj_87 = v_28;
V key_88 = (V)"canonical";
R.Set(obj_87, key_88, values_86[0]);
R.Set(v_28, (V)"surface_sheet", new V((RuleFunction)(args_89 => {
V v_90 = args_89[0];
var values_91 = R.Join(new V[] {}, R.Invoke(v_90, "clone", Results.Empty));
V v_92 = values_91[0];
V v_93 = V.Nil;
v_93 = new V((RuleFunction)(args_94 => {
V v_95 = args_94[0];
V v_96 = args_94[1];
V v_97 = args_94[2];
var values_98 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"region"), R.Join(new V[] {}, new Results(v_95))));
V v_99 = values_98[0];
V v_100 = values_98[1];
return R.Join(new V[] {}, R.Invoke(v_90, "getPixel", R.Join(new V[] {(V)(v_99 + v_96)}, new Results((V)(v_100 + v_97)))));
return Results.Empty;
}));
V v_101 = V.Nil;
v_101 = new V((RuleFunction)(args_102 => {
V v_103 = args_102[0];
V v_104 = args_102[1];
V v_105 = args_102[2];
V v_106 = args_102[3];
var values_107 = R.Join(new V[] {}, R.Call(v_93, R.Join(new V[] {v_103, v_104}, new Results(v_105))));
V v_108 = values_107[0];
V v_109 = values_107[1];
V v_110 = values_107[2];
V v_111 = values_107[3];
return R.Join(new V[] {(V)((V)(R.Get(R.Get(v_28, (V)"background"), (V)1.0d) * (V)((V)1.0d - v_111)) + (V)((V)(v_108 * v_106) * v_111)), (V)((V)(R.Get(R.Get(v_28, (V)"background"), (V)2.0d) * (V)((V)1.0d - v_111)) + (V)((V)(v_109 * v_106) * v_111)), (V)((V)(R.Get(R.Get(v_28, (V)"background"), (V)3.0d) * (V)((V)1.0d - v_111)) + (V)((V)(v_110 * v_106) * v_111))}, new Results((V)1.0d));
return Results.Empty;
}));
foreach (var row_112 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"wall"), new Entry((V)2, (V)"water"), new Entry((V)3, (V)"floor")))))))) {
V v_113 = row_112[0];
V v_114 = row_112[1];
var values_115 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_114 == (V)"floor"), () => R.Table(new Entry((V)1, (V)255.0d))), () => R.Get(v_28, (V)"masks"))));
V v_116 = values_115[0];
foreach (var row_117 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_116)))))) {
V v_118 = row_117[0];
V v_119 = row_117[1];
var values_120 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_114 == (V)"floor"), () => (V)94.0d), () => (V)((V)((V)((V)(v_113 - (V)1.0d) * (V)47.0d) + v_118) - (V)1.0d))));
V v_121 = values_120[0];
var values_122 = R.Join(new V[] {(V)((V)(v_121 % (V)49.0d) * (V)16.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_121 / (V)49.0d)))).First * (V)16.0d)));
V v_123 = values_122[0];
V v_124 = values_122[1];
{ double end_127 = R.Num((V)15.0d), step_128 = R.Num((V)1.0d);
for (double i_126 = R.Num((V)0.0d); step_128 > 0 ? i_126 <= end_127 : i_126 >= end_127; i_126 += step_128) {
V v_125 = i_126;
{ double end_131 = R.Num((V)15.0d), step_132 = R.Num((V)1.0d);
for (double i_130 = R.Num((V)0.0d); step_132 > 0 ? i_130 <= end_131 : i_130 >= end_131; i_130 += step_132) {
V v_129 = i_130;
var values_133 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_114 == (V)"wall"), () => R.Get(v_28, (V)"wall_index")), () => R.And((V)(v_114 == (V)"water"), () => R.Get(v_28, (V)"water_index"))), () => R.Get(v_28, (V)"floor_index"))));
V v_134 = values_133[0];
var values_135 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_114 == (V)"wall"), () => (V)0.35d), () => R.And((V)(v_114 == (V)"water"), () => (V)0.55d)), () => (V)0.19d)));
V v_136 = values_135[0];
var values_137 = R.Join(new V[] {}, R.Call(v_101, R.Join(new V[] {v_134, v_129, v_125}, new Results(v_136))));
V v_138 = values_137[0];
V v_139 = values_137[1];
V v_140 = values_137[2];
V v_141 = values_137[3];
V v_142 = V.Nil;
v_142 = new V((RuleFunction)(args_143 => {
V v_144 = args_143[0];
return R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_119 / v_144)))).First % (V)2.0d) == (V)1.0d)));
return Results.Empty;
}));
var values_145 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.And((V)(!R.Truth(R.Call(v_142, R.Join(new V[] {}, new Results((V)1.0d))).First)), () => (V)(v_125 < (V)2.0d)), () => R.And((V)(!R.Truth(R.Call(v_142, R.Join(new V[] {}, new Results((V)4.0d))).First)), () => (V)(v_125 >= (V)14.0d))), () => R.And((V)(!R.Truth(R.Call(v_142, R.Join(new V[] {}, new Results((V)8.0d))).First)), () => (V)(v_129 < (V)2.0d))), () => R.And((V)(!R.Truth(R.Call(v_142, R.Join(new V[] {}, new Results((V)2.0d))).First)), () => (V)(v_129 >= (V)14.0d)))));
V v_146 = values_145[0];
if (R.Truth(R.And((V)(v_114 != (V)"floor"), () => v_146))) {
var values_147 = R.Join(new V[] {}, R.Call(v_93, R.Join(new V[] {R.Get(v_28, (V)"frame_index"), v_129}, new Results(v_125))));
V v_148 = values_147[0];
V v_149 = values_147[1];
V v_150 = values_147[2];
V v_151 = values_147[3];
if (R.Truth((V)(v_151 > (V)0.0d))) {
var values_152 = R.Join(new V[] {(V)(v_148 * (V)0.86d), (V)(v_149 * (V)0.86d)}, new Results((V)(v_150 * (V)0.86d)));
v_138 = values_152[0];
v_139 = values_152[1];
v_140 = values_152[2];
}
}
foreach (var row_153 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)8.0d), new Entry((V)3, (V)128.0d), new Entry((V)4, (V)0.0d), new Entry((V)5, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)16.0d), new Entry((V)4, (V)12.0d), new Entry((V)5, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)32.0d), new Entry((V)4, (V)12.0d), new Entry((V)5, (V)12.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)8.0d), new Entry((V)3, (V)64.0d), new Entry((V)4, (V)0.0d), new Entry((V)5, (V)12.0d)))))))))) {
V v_154 = row_153[0];
V v_155 = row_153[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_114 != (V)"floor"), () => R.Call(v_142, R.Join(new V[] {}, new Results(R.Get(v_155, (V)1.0d)))).First), () => R.Call(v_142, R.Join(new V[] {}, new Results(R.Get(v_155, (V)2.0d)))).First), () => (V)(!R.Truth(R.Call(v_142, R.Join(new V[] {}, new Results(R.Get(v_155, (V)3.0d)))).First))), () => (V)(v_129 >= R.Get(v_155, (V)4.0d))), () => (V)(v_129 < (V)(R.Get(v_155, (V)4.0d) + (V)4.0d))), () => (V)(v_125 >= R.Get(v_155, (V)5.0d))), () => (V)(v_125 < (V)(R.Get(v_155, (V)5.0d) + (V)4.0d))))) {
var values_156 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_155, (V)4.0d) == (V)0.0d), () => (V)((V)3.0d - v_129)), () => (V)((V)27.0d - v_129))));
V v_157 = values_156[0];
var values_158 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_155, (V)5.0d) == (V)0.0d), () => (V)((V)3.0d - v_125)), () => (V)((V)27.0d - v_125))));
V v_159 = values_158[0];
var values_160 = R.Join(new V[] {}, R.Call(v_93, R.Join(new V[] {R.Get(v_28, (V)"frame_index"), v_157}, new Results(v_159))));
V v_161 = values_160[0];
V v_162 = values_160[1];
V v_163 = values_160[2];
V v_164 = values_160[3];
if (R.Truth((V)(v_164 > (V)0.0d))) {
var values_165 = R.Join(new V[] {(V)(v_161 * (V)0.86d), (V)(v_162 * (V)0.86d)}, new Results((V)(v_163 * (V)0.86d)));
v_138 = values_165[0];
v_139 = values_165[1];
v_140 = values_165[2];
}
}
}
R.Invoke(v_92, "setPixel", R.Join(new V[] {(V)(v_123 + v_129), (V)(v_124 + v_125), v_138, v_139, v_140}, new Results(v_141)));
}
}
}
}
}
}
return R.Join(new V[] {}, new Results(v_92));
return Results.Empty;
})));
R.Set(v_28, (V)"new", new V((RuleFunction)(args_166 => {
V v_167 = args_166[0];
V v_168 = args_166[1];
V v_169 = args_166[2];
var values_170 = R.Join(new V[] {}, R.Call(v_167, R.Join(new V[] {}, new Results(R.Get(v_28, (V)"path")))));
V v_171 = values_170[0];
if (R.Truth((V)(!R.Truth(v_171)))) {
return R.Join(new V[] {V.Nil}, new Results((V)"absent"));
}
var values_172 = R.Join(new V[] {}, new Results(R.Or(v_169, () => R.Get(R.Get(rt.Global("love"), (V)"graphics"), (V)"newQuad"))));
v_169 = values_172[0];
var values_175 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_173 => {
V v_174 = args_173[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_174 == R.Get(v_28, (V)"path")))));

return R.Join(new V[] {}, new Results(v_171));
return Results.Empty;
})), v_168, v_169}, new Results(R.Table(new Entry((V)"monochrome", (V)true))))));
V v_176 = values_175[0];
var values_177 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_168, R.Join(new V[] {v_171, R.Get(v_28, (V)"path")}, new Results((V)"onebit-surfaces"))).First}, new Results((V)"unreadable 1-Bit surfaces"))));
V v_178 = values_177[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Invoke(v_178, "getWidth", Results.Empty).First == (V)784.0d), () => (V)(R.Invoke(v_178, "getHeight", Results.Empty).First == (V)352.0d))}, new Results((V)"1-Bit surface dimensions differ")));
var values_179 = R.Join(new V[] {}, new Results(R.Get(v_176, (V)"handles")));
V v_180 = values_179[0];
var values_181 = R.Join(new V[] {}, new Results(R.Table()));
V v_182 = values_181[0];
V v_183 = V.Nil;
v_183 = new V((RuleFunction)(args_184 => {
V v_185 = args_184[0];
if (R.Truth((V)(!R.Truth(R.Get(v_182, v_185))))) {
var values_186 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"region"), R.Join(new V[] {}, new Results(v_185))));
V v_187 = values_186[0];
V v_188 = values_186[1];
V v_189 = values_186[2];
V v_190 = values_186[3];
var values_191 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", R.Get(v_176, (V)"texture")), new Entry((V)"quad", R.Call(v_169, R.Join(new V[] {v_187, v_188, v_189, v_190, (V)784.0d}, new Results((V)352.0d))).First), new Entry((V)"width", v_189), new Entry((V)"height", v_190), new Entry((V)"offset_x", v_187), new Entry((V)"offset_y", v_188), new Entry((V)"source", (V)"onebit"), new Entry((V)"symbol_index", v_185), new Entry((V)"tint", R.Get(v_28, (V)"ink")))));
var obj_192 = v_182;
V key_193 = v_185;
R.Set(obj_192, key_193, values_191[0]);
}
return R.Join(new V[] {}, new Results(R.Get(v_182, v_185)));
return Results.Empty;
}));
foreach (var row_194 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_28, (V)"actors"))))))) {
V v_195 = row_194[0];
V v_196 = row_194[1];
var values_197 = R.Join(new V[] {}, R.Call(v_183, R.Join(new V[] {}, new Results(v_196))));
var obj_198 = v_180;
V key_199 = v_195;
R.Set(obj_198, key_199, values_197[0]);

var values_200 = R.Join(new V[] {}, new Results(R.Get(v_180, v_195)));
var obj_201 = v_180;
V key_202 = R.Concat(v_195, (V)"/attack");
R.Set(obj_201, key_202, values_200[0]);

var values_203 = R.Join(new V[] {}, new Results(R.Get(v_180, v_195)));
var obj_204 = v_180;
V key_205 = R.Concat(v_195, (V)"/hurt");
R.Set(obj_204, key_205, values_203[0]);
}
foreach (var row_206 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_28, (V)"objects"))))))) {
V v_207 = row_206[0];
V v_208 = row_206[1];
var values_209 = R.Join(new V[] {}, R.Call(v_183, R.Join(new V[] {}, new Results(v_208))));
var obj_210 = v_180;
V key_211 = v_207;
R.Set(obj_210, key_211, values_209[0]);
}
var values_212 = R.Join(new V[] {}, new Results(R.Table()));
V v_213 = values_212[0];

var values_221 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(new V((RuleFunction)(args_214 => {

var values_215 = R.Join(new V[] {}, new Results((V)"assets/kenney/approved/tiny-dungeon.png"));
V v_216 = values_215[0];

var values_217 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_167, R.Join(new V[] {}, new Results(v_216))))));
V v_218 = values_217[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_atlas"))).First, (V)"checksum"), R.Join(new V[] {}, new Results(v_218))).First == (V)"d407b7ab694fa5d5")}, new Results((V)"corrupt vent source")));
var values_219 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_168, R.Join(new V[] {v_218, v_216}, new Results((V)"approved-monochrome"))))));
V v_220 = values_219[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Invoke(v_220, "getWidth", Results.Empty).First == (V)203.0d), () => (V)(R.Invoke(v_220, "getHeight", Results.Empty).First == (V)186.0d))}, new Results((V)"wrong vent source size")));
return R.Join(new V[] {}, new Results(v_220));
return Results.Empty;
}))))));
V v_222 = values_221[0];
V v_223 = values_221[1];
if (R.Truth(v_222)) {
var values_224 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_223))));
var obj_225 = v_213;
V key_226 = (V)"vents";
R.Set(obj_225, key_226, values_224[0]);
foreach (var row_227 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"heat_vent_cooldown"), new Entry((V)2, (V)"heat_vent_warning"), new Entry((V)3, (V)"heat_vent_burst")))))))) {
V v_228 = row_227[0];
V v_229 = row_227[1];
var values_230 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_24, (V)"art"), v_229), (V)2.0d)));
V v_231 = values_230[0];

var values_232 = R.Join(new V[] {(V)((V)(v_231 % (V)12.0d) * (V)17.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_231 / (V)12.0d)))).First * (V)17.0d)));
V v_233 = values_232[0];
V v_234 = values_232[1];
var values_235 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_223), new Entry((V)"quad", R.Call(v_169, R.Join(new V[] {v_233, v_234, (V)16.0d, (V)16.0d, (V)203.0d}, new Results((V)186.0d))).First), new Entry((V)"width", (V)16.0d), new Entry((V)"height", (V)16.0d), new Entry((V)"offset_x", v_233), new Entry((V)"offset_y", v_234), new Entry((V)"source", (V)"onebit"), new Entry((V)"approved_sheet", (V)"tiny"), new Entry((V)"tile_index", v_231), new Entry((V)"monochrome", (V)true))));
var obj_236 = v_180;
V key_237 = R.Call(R.Get(v_24, (V)"binding"), R.Join(new V[] {}, new Results(v_229))).First;
R.Set(obj_236, key_237, values_235[0]);
}
}
var values_238 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_239 = values_238[0];
V v_240 = values_238[1];
V v_241 = V.Nil;
v_241 = new V((RuleFunction)(args_242 => {
V v_243 = args_242[0];
V v_244 = args_242[1];
V v_245 = args_242[2];
var values_246 = R.Join(new V[] {(V)((V)(v_243 % (V)49.0d) * (V)16.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_243 / (V)49.0d)))).First * (V)16.0d)));
V v_247 = values_246[0];
V v_248 = values_246[1];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_178), new Entry((V)"quad", R.Call(v_169, R.Join(new V[] {v_247, v_248, (V)16.0d, (V)16.0d, (V)784.0d}, new Results((V)352.0d))).First), new Entry((V)"width", (V)16.0d), new Entry((V)"height", (V)16.0d), new Entry((V)"offset_x", v_247), new Entry((V)"offset_y", v_248), new Entry((V)"source", (V)"onebit"), new Entry((V)"terrain_pack", (V)"onebit"), new Entry((V)"wall_mask", R.Or(R.And((V)(v_245 == (V)"wall"), () => v_244), () => V.Nil)), new Entry((V)"water_mask", R.Or(R.And((V)(v_245 == (V)"water"), () => v_244), () => V.Nil)))));
return Results.Empty;
}));
foreach (var row_249 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_28, (V)"masks"))))))) {
V v_250 = row_249[0];
V v_251 = row_249[1];
var values_252 = R.Join(new V[] {}, R.Call(v_241, R.Join(new V[] {(V)(v_250 - (V)1.0d), v_251}, new Results((V)"wall"))));
var obj_253 = v_239;
V key_254 = v_251;
R.Set(obj_253, key_254, values_252[0]);
var values_255 = R.Join(new V[] {}, R.Call(v_241, R.Join(new V[] {(V)((V)((V)47.0d + v_250) - (V)1.0d), v_251}, new Results((V)"water"))));
var obj_256 = v_240;
V key_257 = v_251;
R.Set(obj_256, key_257, values_255[0]);
}
var values_258 = R.Join(new V[] {}, R.Call(v_241, R.Join(new V[] {(V)94.0d, (V)255.0d}, new Results((V)"floor"))));
V v_259 = values_258[0];
foreach (var row_260 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"order"))))))) {
V v_261 = row_260[0];
V v_262 = row_260[1];
var values_263 = R.Join(new V[] {}, new Results(R.Concat((V)"core:terrain/", R.Concat(R.Invoke(v_262, "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))).First, (V)"/"))));
V v_264 = values_263[0];
var values_265 = R.Join(new V[] {}, new Results(v_259));
var obj_266 = v_180;
V key_267 = R.Concat(v_264, (V)"floor/base");
R.Set(obj_266, key_267, values_265[0]);

var values_268 = R.Join(new V[] {}, new Results(R.Get(v_239, (V)255.0d)));
var obj_269 = v_180;
V key_270 = R.Concat(v_264, (V)"wall/base");
R.Set(obj_269, key_270, values_268[0]);
var values_271 = R.Join(new V[] {}, new Results(R.Get(v_240, (V)255.0d)));
var obj_272 = v_180;
V key_273 = R.Concat(v_264, (V)"water/base");
R.Set(obj_272, key_273, values_271[0]);
}
var values_274 = R.Join(new V[] {}, R.Call(R.Get(v_26, (V)"new"), R.Join(new V[] {(V)"onebit", v_167, v_168, v_169}, new Results(R.Table(new Entry((V)"onebit", R.Get(v_176, (V)"texture")))))));
V v_275 = values_274[0];
foreach (var row_276 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_275, (V)"handles"))))))) {
V v_277 = row_276[0];
V v_278 = row_276[1];
var values_279 = R.Join(new V[] {}, new Results(v_278));
var obj_280 = v_180;
V key_281 = v_277;
R.Set(obj_280, key_281, values_279[0]);
}
return R.Join(new V[] {R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"handles", v_180), new Entry((V)"walls", v_239), new Entry((V)"waters", v_240), new Entry((V)"floor", v_259), new Entry((V)"environment", v_275), new Entry((V)"terrain_renderer", (V)true), new Entry((V)"terrain_mode", (V)"tiled"), new Entry((V)"style", (V)"onebit"), new Entry((V)"label", (V)"Kenney 1-Bit"), new Entry((V)"signature", R.Concat(R.Get(v_28, (V)"checksum"), R.Concat((V)":onebit-1", R.Concat(R.Or(R.And(v_222, () => (V)":tiny-vents-1"), () => (V)":native-vents"), R.Concat((V)":", R.Get(v_275, (V)"signature")))))), new Entry((V)"pages", R.Table(new Entry((V)"symbols", R.Table(new Entry((V)"texture", R.Get(v_176, (V)"texture")))), new Entry((V)"surfaces", R.Table(new Entry((V)"texture", v_178))), new Entry((V)"vents", R.Get(v_213, (V)"vents")))), new Entry((V)"vent_status", R.Or(R.And(v_222, () => (V)"monochrome Tiny"), () => (V)"native fallback")))}, new Results(R.Table(new Entry((V)"__index", v_28))))).First}, new Results((V)"loaded"));
return Results.Empty;
})));
R.Set(v_28, "resolve", new V((RuleFunction)(args_282 => {
V v_283 = args_282[0];
V v_284 = args_282[1];
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_283, (V)"handles"), v_284)));
return Results.Empty;
})));
R.Set(v_28, "terrain", new V((RuleFunction)(args_285 => {
V v_286 = args_285[0];
V v_287 = args_285[1];
V v_288 = args_285[2];
V v_289 = args_285[3];
V v_290 = args_285[4];
V v_291 = args_285[5];
var values_292 = R.Join(new V[] {}, R.Invoke(R.Get(v_286, (V)"environment"), "resolve", R.Join(new V[] {v_287, v_288, v_289, v_290}, new Results(v_291))));
V v_293 = values_292[0];
if (R.Truth(v_293)) {
return R.Join(new V[] {}, new Results(v_293));
}
if (R.Truth((V)(v_290 == (V)"floor"))) {
return R.Join(new V[] {}, new Results(R.Get(v_286, (V)"floor")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(v_290 == (V)"wall"), () => (V)(v_290 == (V)"water"))}, new Results((V)"invalid 1-Bit terrain kind")));
var values_294 = R.Join(new V[] {}, new Results((V)0.0d));
V v_295 = values_294[0];
foreach (var row_296 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)4.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)8.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)16.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)32.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)64.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)128.0d)))))))))) {
V v_297 = row_296[0];
V v_298 = row_296[1];
var values_299 = R.Join(new V[] {(V)(v_288 + R.Get(v_298, (V)1.0d))}, new Results((V)(v_289 + R.Get(v_298, (V)2.0d))));
V v_300 = values_299[0];
V v_301 = values_299[1];

var values_302 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_287, (V)"rows"), v_301)));
V v_303 = values_302[0];
if (R.Truth(R.And(R.And(R.And((V)(v_300 >= (V)1.0d), () => v_303), () => (V)(v_300 <= (V)R.Length(v_303))), () => R.Or(R.And(v_291, () => (V)(!R.Truth(R.Call(v_291, R.Join(new V[] {v_300}, new Results(v_301))).First))), () => (V)(R.Invoke(v_303, "sub", R.Join(new V[] {v_300}, new Results(v_300))).First == R.Or(R.And((V)(v_290 == (V)"wall"), () => (V)"#"), () => (V)"~")))))) {
var values_304 = R.Join(new V[] {}, new Results((V)(v_295 + R.Get(v_298, (V)3.0d))));
v_295 = values_304[0];
}
}
return R.Join(new V[] {}, new Results(R.Get(R.Or(R.And((V)(v_290 == (V)"wall"), () => R.Get(v_286, (V)"walls")), () => R.Get(v_286, (V)"waters")), R.Call(R.Get(v_28, (V)"canonical"), R.Join(new V[] {}, new Results(v_295))).First)));
return Results.Empty;
})));
R.Set(v_28, "set_world", new V((RuleFunction)(args_305 => {
V v_306 = args_305[0];
V v_307 = args_305[1];
R.Invoke(R.Get(v_306, (V)"environment"), "set_world", R.Join(new V[] {}, new Results(v_307)));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_28));
return Results.Empty;
} }
