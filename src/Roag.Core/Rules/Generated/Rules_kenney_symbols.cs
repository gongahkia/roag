// Static C# port of src/kenney_symbols.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 13c2a9a9de9dd642ab4cfa9fe74b82b5f4816a56b5a5e877b9a56be1eafaa495
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_kenney_symbols { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_atlas"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", (V)"assets/kenney/1-bit-pack/symbols.png"), new Entry((V)"width", (V)784.0d), new Entry((V)"height", (V)352.0d), new Entry((V)"tile", (V)16.0d))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results((V)"3fc46971ed866cba"));
var obj_12 = v_10;
V key_13 = (V)"checksum";
R.Set(obj_12, key_13, values_11[0]);
var values_14 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.9d), new Entry((V)2, (V)0.89d), new Entry((V)3, (V)0.82d))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.9d), new Entry((V)2, (V)0.9d), new Entry((V)3, (V)0.9d))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.88d), new Entry((V)3, (V)0.43d))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.58d), new Entry((V)3, (V)0.34d))));
V v_21 = values_20[0];
var values_22 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.65d), new Entry((V)2, (V)0.85d), new Entry((V)3, (V)0.72d))));
V v_23 = values_22[0];
var values_24 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.49d), new Entry((V)2, (V)0.84d), new Entry((V)3, (V)0.88d))));
V v_25 = values_24[0];
var values_26 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.76d), new Entry((V)2, (V)0.66d), new Entry((V)3, (V)0.95d))));
V v_27 = values_26[0];
var values_28 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.84d), new Entry((V)2, (V)0.84d), new Entry((V)3, (V)0.91d))));
V v_29 = values_28[0];
var values_30 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"storm_conductor", R.Table(new Entry((V)1, (V)616.0d), new Entry((V)2, (V)"radiating spark"), new Entry((V)3, v_19))), new Entry((V)"detonation_bloom", R.Table(new Entry((V)1, (V)618.0d), new Entry((V)2, (V)"burst"), new Entry((V)3, v_21))), new Entry((V)"thorn_mirror", R.Table(new Entry((V)1, (V)233.0d), new Entry((V)2, (V)"spiked shield"), new Entry((V)3, v_23))), new Entry((V)"static_footsteps", R.Table(new Entry((V)1, (V)1012.0d), new Entry((V)2, (V)"winged boot"), new Entry((V)3, v_19))), new Entry((V)"resonant_wounds", R.Table(new Entry((V)1, (V)708.0d), new Entry((V)2, (V)"clustered stars"), new Entry((V)3, v_27))), new Entry((V)"temporal_echo", R.Table(new Entry((V)1, (V)629.0d), new Entry((V)2, (V)"hourglass"), new Entry((V)3, v_27))), new Entry((V)"crescent_reach", R.Table(new Entry((V)1, (V)563.0d), new Entry((V)2, (V)"curved slash"), new Entry((V)3, v_29))), new Entry((V)"turncoat_spark", R.Table(new Entry((V)1, (V)617.0d), new Entry((V)2, (V)"small spark"), new Entry((V)3, v_19))), new Entry((V)"emberbrand", R.Table(new Entry((V)1, (V)567.0d), new Entry((V)2, (V)"flame"), new Entry((V)3, v_21))), new Entry((V)"flashpoint", R.Table(new Entry((V)1, (V)566.0d), new Entry((V)2, (V)"fire burst"), new Entry((V)3, v_21))), new Entry((V)"grave_dividend", R.Table(new Entry((V)1, (V)622.0d), new Entry((V)2, (V)"skull"), new Entry((V)3, v_23))), new Entry((V)"opening_cut", R.Table(new Entry((V)1, (V)326.0d), new Entry((V)2, (V)"dagger"), new Entry((V)3, v_29))), new Entry((V)"crowdbreaker", R.Table(new Entry((V)1, (V)384.0d), new Entry((V)2, (V)"great axe"), new Entry((V)3, v_29))), new Entry((V)"harriers_response", R.Table(new Entry((V)1, (V)623.0d), new Entry((V)2, (V)"target corners"), new Entry((V)3, v_19))), new Entry((V)"perfect_guard", R.Table(new Entry((V)1, (V)184.0d), new Entry((V)2, (V)"round shield"), new Entry((V)3, v_23))), new Entry((V)"final_spark", R.Table(new Entry((V)1, (V)619.0d), new Entry((V)2, (V)"fading spark ring"), new Entry((V)3, v_19))), new Entry((V)"pulse_step", R.Table(new Entry((V)1, (V)705.0d), new Entry((V)2, (V)"pulse ring"), new Entry((V)3, v_25))), new Entry((V)"packed_powder", R.Table(new Entry((V)1, (V)486.0d), new Entry((V)2, (V)"bomb"), new Entry((V)3, v_21))), new Entry((V)"longblade_geometry", R.Table(new Entry((V)1, (V)378.0d), new Entry((V)2, (V)"long sword"), new Entry((V)3, v_29))), new Entry((V)"phantom_lunge", R.Table(new Entry((V)1, (V)564.0d), new Entry((V)2, (V)"long slash"), new Entry((V)3, v_25))), new Entry((V)"guardians_vow", R.Table(new Entry((V)1, (V)236.0d), new Entry((V)2, (V)"ward shield"), new Entry((V)3, v_23))), new Entry((V)"prospectors_heart", R.Table(new Entry((V)1, (V)818.0d), new Entry((V)2, (V)"coins"), new Entry((V)3, v_23))), new Entry((V)"circuit_chorus", R.Table(new Entry((V)1, (V)575.0d), new Entry((V)2, (V)"linked nodes"), new Entry((V)3, v_19))), new Entry((V)"crownfire", R.Table(new Entry((V)1, (V)141.0d), new Entry((V)2, (V)"crown"), new Entry((V)3, v_21))), new Entry((V)"stonewake", R.Table(new Entry((V)1, (V)262.0d), new Entry((V)2, (V)"stone block"), new Entry((V)3, v_29))), new Entry((V)"bramble_compact", R.Table(new Entry((V)1, (V)104.0d), new Entry((V)2, (V)"thorn bush"), new Entry((V)3, v_23))), new Entry((V)"froststep_catalyst", R.Table(new Entry((V)1, (V)707.0d), new Entry((V)2, (V)"frost sparks"), new Entry((V)3, v_25))), new Entry((V)"fracture_dividend", R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)"stone fragments"), new Entry((V)3, v_29))), new Entry((V)"mist_veil", R.Table(new Entry((V)1, (V)717.0d), new Entry((V)2, (V)"drifting flecks"), new Entry((V)3, v_25))), new Entry((V)"crystal_harmony", R.Table(new Entry((V)1, (V)522.0d), new Entry((V)2, (V)"faceted gem"), new Entry((V)3, v_27))), new Entry((V)"sunderers_mark", R.Table(new Entry((V)1, (V)383.0d), new Entry((V)2, (V)"pickaxe"), new Entry((V)3, v_29))), new Entry((V)"counterweight", R.Table(new Entry((V)1, (V)230.0d), new Entry((V)2, (V)"mace"), new Entry((V)3, v_23))), new Entry((V)"bloodrush", R.Table(new Entry((V)1, (V)1013.0d), new Entry((V)2, (V)"rushing boot"), new Entry((V)3, v_21))), new Entry((V)"pain_dividend", R.Table(new Entry((V)1, (V)532.0d), new Entry((V)2, (V)"marked heart"), new Entry((V)3, v_21))), new Entry((V)"executioners_thread", R.Table(new Entry((V)1, (V)381.0d), new Entry((V)2, (V)"execution axe"), new Entry((V)3, v_29))), new Entry((V)"shared_agony", R.Table(new Entry((V)1, (V)531.0d), new Entry((V)2, (V)"paired hearts"), new Entry((V)3, v_21))), new Entry((V)"windup_hunter", R.Table(new Entry((V)1, (V)624.0d), new Entry((V)2, (V)"target reticle"), new Entry((V)3, v_29))), new Entry((V)"crossfire_omen", R.Table(new Entry((V)1, (V)569.0d), new Entry((V)2, (V)"crossed blades"), new Entry((V)3, v_21))), new Entry((V)"pursuit_spark", R.Table(new Entry((V)1, (V)1010.0d), new Entry((V)2, (V)"movement arrow"), new Entry((V)3, v_19))), new Entry((V)"ordered_ranks", R.Table(new Entry((V)1, (V)187.0d), new Entry((V)2, (V)"formation shield"), new Entry((V)3, v_29))), new Entry((V)"far_witness", R.Table(new Entry((V)1, (V)771.0d), new Entry((V)2, (V)"eye"), new Entry((V)3, v_27))), new Entry((V)"betrayal_ward", R.Table(new Entry((V)1, (V)185.0d), new Entry((V)2, (V)"quartered shield"), new Entry((V)3, v_23))), new Entry((V)"rhythm_of_steel", R.Table(new Entry((V)1, (V)823.0d), new Entry((V)2, (V)"musical notes"), new Entry((V)3, v_29))), new Entry((V)"cluster_rupture", R.Table(new Entry((V)1, (V)830.0d), new Entry((V)2, (V)"scattered burst"), new Entry((V)3, v_21))), new Entry((V)"splitflight", R.Table(new Entry((V)1, (V)282.0d), new Entry((V)2, (V)"drawn bow"), new Entry((V)3, v_25))), new Entry((V)"recurve", R.Table(new Entry((V)1, (V)333.0d), new Entry((V)2, (V)"recurve bow"), new Entry((V)3, v_25))), new Entry((V)"expanding_sigil", R.Table(new Entry((V)1, (V)709.0d), new Entry((V)2, (V)"circular sigil"), new Entry((V)3, v_27))), new Entry((V)"alchemical_flip", R.Table(new Entry((V)1, (V)669.0d), new Entry((V)2, (V)"potion"), new Entry((V)3, v_27))), new Entry((V)"earthshaker", R.Table(new Entry((V)1, (V)380.0d), new Entry((V)2, (V)"war hammer"), new Entry((V)3, v_29))), new Entry((V)"battle_march", R.Table(new Entry((V)1, (V)89.0d), new Entry((V)2, (V)"marching boots"), new Entry((V)3, v_29))), new Entry((V)"aftershock", R.Table(new Entry((V)1, (V)706.0d), new Entry((V)2, (V)"shock burst"), new Entry((V)3, v_29))), new Entry((V)"reprisal_nova", R.Table(new Entry((V)1, (V)716.0d), new Entry((V)2, (V)"radial burst"), new Entry((V)3, v_23))), new Entry((V)"ring_of_reversal", R.Table(new Entry((V)1, (V)337.0d), new Entry((V)2, (V)"reversal ring"), new Entry((V)3, v_25))))));
V v_31 = values_30[0];
var values_32 = R.Join(new V[] {}, new Results(R.Table()));
var obj_33 = v_10;
V key_34 = (V)"boons";
R.Set(obj_33, key_34, values_32[0]);
foreach (var row_35 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_31)))))) {
V v_36 = row_35[0];
V v_37 = row_35[1];
var values_38 = R.Join(new V[] {}, new Results(v_37));
var obj_39 = R.Get(v_10, (V)"boons");
V key_40 = R.Concat((V)"core:boon/", v_36);
R.Set(obj_39, key_40, values_38[0]);
}
foreach (var row_41 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"order"))))))) {
V v_42 = row_41[0];
V v_43 = row_41[1];
var values_44 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"definitions"), v_43), (V)"art")));
V v_45 = values_44[0];
var values_46 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_45, (V)"index")), new Entry((V)2, R.Get(v_45, (V)"motif")), new Entry((V)3, v_23))));
var obj_47 = R.Get(v_10, (V)"boons");
V key_48 = v_43;
R.Set(obj_47, key_48, values_46[0]);
}
var values_49 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:terrain/bramble_bed", R.Table(new Entry((V)1, (V)104.0d), new Entry((V)2, (V)"thorn bush"), new Entry((V)3, R.Table(new Entry((V)1, (V)0.52d), new Entry((V)2, (V)0.78d), new Entry((V)3, (V)0.35d))), new Entry((V)"ground", R.Table(new Entry((V)1, (V)0.07d), new Entry((V)2, (V)0.13d), new Entry((V)3, (V)0.055d))))), new Entry((V)"core:terrain/sticky_bog", R.Table(new Entry((V)1, (V)268.0d), new Entry((V)2, (V)"uneven ripples"), new Entry((V)3, R.Table(new Entry((V)1, (V)0.6d), new Entry((V)2, (V)0.64d), new Entry((V)3, (V)0.32d))), new Entry((V)"ground", R.Table(new Entry((V)1, (V)0.1d), new Entry((V)2, (V)0.12d), new Entry((V)3, (V)0.055d))))), new Entry((V)"core:terrain/glazed_ice", R.Table(new Entry((V)1, (V)616.0d), new Entry((V)2, (V)"ice star"), new Entry((V)3, R.Table(new Entry((V)1, (V)0.51d), new Entry((V)2, (V)0.84d), new Entry((V)3, (V)1.0d))), new Entry((V)"ground", R.Table(new Entry((V)1, (V)0.08d), new Entry((V)2, (V)0.16d), new Entry((V)3, (V)0.22d))))), new Entry((V)"core:terrain/barricade", R.Table(new Entry((V)1, (V)121.0d), new Entry((V)2, (V)"cross-braced crate"), new Entry((V)3, R.Table(new Entry((V)1, (V)0.84d), new Entry((V)2, (V)0.59d), new Entry((V)3, (V)0.35d))), new Entry((V)"ground", R.Table(new Entry((V)1, (V)0.15d), new Entry((V)2, (V)0.095d), new Entry((V)3, (V)0.05d))))), new Entry((V)"core:terrain/obscuring_mist", R.Table(new Entry((V)1, (V)267.0d), new Entry((V)2, (V)"layered wisps"), new Entry((V)3, R.Table(new Entry((V)1, (V)0.69d), new Entry((V)2, (V)0.79d), new Entry((V)3, (V)0.81d))), new Entry((V)"ground", R.Table(new Entry((V)1, (V)0.115d), new Entry((V)2, (V)0.15d), new Entry((V)3, (V)0.16d))))), new Entry((V)"core:terrain/resonant_crystal", R.Table(new Entry((V)1, (V)523.0d), new Entry((V)2, (V)"crystal"), new Entry((V)3, R.Table(new Entry((V)1, (V)0.78d), new Entry((V)2, (V)0.59d), new Entry((V)3, (V)0.97d))), new Entry((V)"ground", R.Table(new Entry((V)1, (V)0.13d), new Entry((V)2, (V)0.085d), new Entry((V)3, (V)0.19d))))))));
var obj_50 = v_10;
V key_51 = (V)"terrain";
R.Set(obj_50, key_51, values_49[0]);
R.Set(v_10, (V)"region", new V((RuleFunction)(args_52 => {
V v_53 = args_52[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_53))).First == (V)"number"), () => (V)((V)(v_53 % (V)1.0d) == (V)0.0d)), () => (V)(v_53 >= (V)0.0d)), () => (V)(v_53 < (V)1078.0d))}, new Results((V)"invalid Kenney symbol index")));
return R.Join(new V[] {(V)((V)(v_53 % (V)49.0d) * (V)16.0d), (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_53 / (V)49.0d)))).First * (V)16.0d), (V)16.0d}, new Results((V)16.0d));
return Results.Empty;
})));
R.Set(v_10, (V)"new", new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
V v_57 = args_54[2];
V v_58 = args_54[3];
var values_59 = R.Join(new V[] {}, new Results(R.And(v_58, () => R.Get(v_58, (V)"monochrome"))));
V v_60 = values_59[0];
var values_61 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_55, R.Join(new V[] {}, new Results(R.Get(v_10, (V)"path")))).First}, new Results((V)"missing Kenney 1-Bit symbol sheet"))));
V v_62 = values_61[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, new Results(v_62))).First == R.Get(v_10, (V)"checksum"))}, new Results((V)"corrupt Kenney 1-Bit symbol sheet")));
var values_63 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_56, R.Join(new V[] {v_62, R.Get(v_10, (V)"path")}, new Results((V)false))).First}, new Results((V)"unreadable Kenney symbols"))));
V v_64 = values_63[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Invoke(v_64, "getWidth", Results.Empty).First == R.Get(v_10, (V)"width")), () => (V)(R.Invoke(v_64, "getHeight", Results.Empty).First == R.Get(v_10, (V)"height")))}, new Results((V)"Kenney symbol sheet dimensions differ")));
var values_65 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_66 = values_65[0];
V v_67 = values_65[1];
V v_68 = V.Nil;
v_68 = new V((RuleFunction)(args_69 => {
V v_70 = args_69[0];
V v_71 = args_69[1];
var values_72 = R.Join(new V[] {}, new Results(R.Get(v_70, (V)1.0d)));
V v_73 = values_72[0];
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"region"), R.Join(new V[] {}, new Results(v_73))));
V v_75 = values_74[0];
V v_76 = values_74[1];
V v_77 = values_74[2];
V v_78 = values_74[3];
if (R.Truth((V)(!R.Truth(R.Get(v_66, v_73))))) {
var values_79 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_75, v_76, v_77, v_78, R.Get(v_10, (V)"width")}, new Results(R.Get(v_10, (V)"height")))));
var obj_80 = v_66;
V key_81 = v_73;
R.Set(obj_80, key_81, values_79[0]);
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_64), new Entry((V)"quad", R.Get(v_66, v_73)), new Entry((V)"width", v_77), new Entry((V)"height", v_78), new Entry((V)"offset_x", v_75), new Entry((V)"offset_y", v_76), new Entry((V)"source", R.Or(R.And(v_60, () => (V)"onebit"), () => (V)"kenney")), new Entry((V)"symbol_index", v_73), new Entry((V)"motif", R.Get(v_70, (V)2.0d)), new Entry((V)"tint", R.Or(R.Or(R.And(v_60, () => v_17), () => R.Get(v_70, (V)3.0d)), () => v_15)), new Entry((V)"tile_inset", R.Or(R.And(v_71, () => (V)4.0d), () => V.Nil)), new Entry((V)"ground", R.And(R.Get(v_70, (V)"ground"), () => R.Or(R.And(v_60, () => R.Table(new Entry((V)1, (V)0.06d), new Entry((V)2, (V)0.06d), new Entry((V)3, (V)0.06d))), () => R.Get(v_70, (V)"ground")))))));
return Results.Empty;
}));
foreach (var row_82 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_10, (V)"boons"))))))) {
V v_83 = row_82[0];
V v_84 = row_82[1];
var values_85 = R.Join(new V[] {}, R.Call(v_68, R.Join(new V[] {}, new Results(v_84))));
var obj_86 = v_67;
V key_87 = R.Concat(v_83, (V)"/icon");
R.Set(obj_86, key_87, values_85[0]);
var values_88 = R.Join(new V[] {}, new Results(R.Get(v_67, R.Concat(v_83, (V)"/icon"))));
var obj_89 = v_67;
V key_90 = R.Concat(v_83, (V)"/detail");
R.Set(obj_89, key_90, values_88[0]);
}
foreach (var row_91 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_10, (V)"terrain"))))))) {
V v_92 = row_91[0];
V v_93 = row_91[1];
var values_94 = R.Join(new V[] {}, R.Call(v_68, R.Join(new V[] {v_93}, new Results((V)true))));
var obj_95 = v_67;
V key_96 = v_92;
R.Set(obj_95, key_96, values_94[0]);
}
foreach (var row_97 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"cues"))))))) {
V v_98 = row_97[0];
V v_99 = row_97[1];
var values_100 = R.Join(new V[] {}, R.Call(v_68, R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_99), new Entry((V)2, v_98), new Entry((V)3, v_27))))));
var obj_101 = v_67;
V key_102 = R.Concat((V)"core:ui/", v_98);
R.Set(obj_101, key_102, values_100[0]);
}
foreach (var row_103 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_6, (V)"symbols"), () => R.Table()))))))) {
V v_104 = row_103[0];
V v_105 = row_103[1];
var values_106 = R.Join(new V[] {}, R.Call(v_68, R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_105), new Entry((V)2, v_104), new Entry((V)3, v_27))))));
var obj_107 = v_67;
V key_108 = v_104;
R.Set(obj_107, key_108, values_106[0]);
}
foreach (var row_109 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"symbols"))))))) {
V v_110 = row_109[0];
V v_111 = row_109[1];
var values_112 = R.Join(new V[] {}, R.Call(v_68, R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_111), new Entry((V)2, v_110), new Entry((V)3, v_27))))));
var obj_113 = v_67;
V key_114 = v_110;
R.Set(obj_113, key_114, values_112[0]);
}
foreach (var row_115 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"cues"))))))) {
V v_116 = row_115[0];
V v_117 = row_115[1];
var values_118 = R.Join(new V[] {}, R.Call(v_68, R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_117, (V)"index")), new Entry((V)2, R.Get(v_117, (V)"label")), new Entry((V)3, v_27))))));
var obj_119 = v_67;
V key_120 = R.Get(v_117, (V)"id");
R.Set(obj_119, key_120, values_118[0]);
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_64), new Entry((V)"handles", v_67), new Entry((V)"signature", R.Concat(R.Get(v_10, (V)"checksum"), (V)":semantic-1")))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_10));
return Results.Empty;
} }
