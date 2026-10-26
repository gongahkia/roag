// Static C# port of src/content.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: d8e71d549cb9031094b462f8b7e1e6fa3fe41cab7184fd60b59a1969f65a0505
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_content { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.finale"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.elite_ai"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.regular_ai"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, new Results((V)"roag-content/8"));
var obj_20 = v_2;
V key_21 = (V)"version";
R.Set(obj_20, key_21, values_19[0]);
var values_22 = R.Join(new V[] {}, new Results(R.Get(v_8, (V)"definitions")));
V v_23 = values_22[0];
var values_24 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"get"), R.Join(new V[] {}, new Results((V)"core:actor/adventurer"))));
V v_25 = values_24[0];
var values_26 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"first_threshold", (V)5.0d), new Entry((V)"threshold_step", (V)5.0d), new Entry((V)"stat_order", R.Table(new Entry((V)1, (V)"max_health"), new Entry((V)2, (V)"damage"), new Entry((V)3, (V)"defense"), new Entry((V)4, (V)"speed"), new Entry((V)5, (V)"crit_chance"), new Entry((V)6, (V)"crit_damage"))), new Entry((V)"stat_weights", R.Table(new Entry((V)"max_health", (V)1.0d), new Entry((V)"damage", (V)1.0d), new Entry((V)"defense", (V)1.0d), new Entry((V)"speed", (V)1.0d), new Entry((V)"crit_chance", (V)1.0d), new Entry((V)"crit_damage", (V)1.0d))), new Entry((V)"growth", R.Table(new Entry((V)"max_health", (V)4.0d), new Entry((V)"damage", (V)1.0d), new Entry((V)"defense", (V)1.0d), new Entry((V)"speed", (V)10.0d), new Entry((V)"crit_chance", (V)50000.0d), new Entry((V)"crit_damage", (V)25.0d))))));
V v_27 = values_26[0];
var values_28 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:chest/standard", R.Table(new Entry((V)"id", (V)"core:chest/standard"), new Entry((V)"name", (V)"Boon Chest"), new Entry((V)"price", (V)5.0d), new Entry((V)"rarity_weights", R.Table(new Entry((V)"common", (V)70.0d), new Entry((V)"uncommon", (V)25.0d), new Entry((V)"rare", (V)4.0d), new Entry((V)"legendary", (V)1.0d))))), new Entry((V)"core:chest/free_elite", R.Table(new Entry((V)"id", (V)"core:chest/free_elite"), new Entry((V)"name", (V)"Elite Boon Chest"), new Entry((V)"price", (V)0.0d), new Entry((V)"rarity_weights", R.Table(new Entry((V)"common", (V)20.0d), new Entry((V)"uncommon", (V)45.0d), new Entry((V)"rare", (V)28.0d), new Entry((V)"legendary", (V)7.0d))))))));
V v_29 = values_28[0];
var values_30 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"elite_activation_radius", (V)7.0d), new Entry((V)"elite_phase_percent", (V)50.0d), new Entry((V)"grace", (V)1500.0d), new Entry((V)"pressure_first", (V)1200.0d), new Entry((V)"pressure_periods", R.Table(new Entry((V)1, (V)1200.0d), new Entry((V)2, (V)900.0d), new Entry((V)3, (V)650.0d))), new Entry((V)"pressure_max_active", (V)18.0d), new Entry((V)"pressure_safe_radius", (V)8.0d), new Entry((V)"pursuer_spawn_distance", (V)14.0d), new Entry((V)"routes", R.Table(new Entry((V)1, R.Get(R.Call(R.Get(v_4, (V)"routes_for"), R.Join(new V[] {(V)1.0d}, new Results(R.Table()))).First, (V)1.0d)), new Entry((V)2, R.Get(R.Call(R.Get(v_4, (V)"routes_for"), R.Join(new V[] {(V)1.0d}, new Results(R.Table()))).First, (V)2.0d)), new Entry((V)3, R.Get(R.Call(R.Get(v_4, (V)"routes_for"), R.Join(new V[] {(V)2.0d}, new Results(R.Table(new Entry((V)1, (V)"core:route/fungal_hollows"))))).First, (V)2.0d)), new Entry((V)4, R.Get(R.Call(R.Get(v_4, (V)"routes_for"), R.Join(new V[] {(V)3.0d}, new Results(R.Table()))).First, (V)1.0d)))))));
V v_31 = values_30[0];
var values_32 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:ability/basic_attack", (V)1.0d), new Entry((V)"core:ability/sweeping_slash", (V)1.0d), new Entry((V)"core:ability/guard_strike", (V)1.0d), new Entry((V)"core:ability/thorn_lane", (V)1.0d), new Entry((V)"core:ability/skitter_stab", (V)1.0d), new Entry((V)"core:ability/spore_burst", (V)1.0d), new Entry((V)"core:ability/canal_thrust", (V)1.0d), new Entry((V)"core:ability/bell_cross", (V)1.0d), new Entry((V)"core:ability/warden_sweep", (V)1.0d), new Entry((V)"core:ability/warden_roots", (V)1.0d), new Entry((V)"core:ability/regent_line", (V)1.0d), new Entry((V)"core:ability/regent_crown", (V)1.0d), new Entry((V)"core:ability/regent_sigil", (V)1.0d), new Entry((V)"core:ability/pursuer_strike", (V)0.0d))));
V v_33 = values_32[0];
var values_34 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_35 = v_33;
V key_36 = (V)"core:ability/tinkerer_sentry";
R.Set(obj_35, key_36, values_34[0]);
var values_37 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:ability/tinkerer_sentry", (V)"Sentry bolt"), new Entry((V)"core:ability/regent_line", (V)"Regent's Line"), new Entry((V)"core:ability/regent_crown", (V)"Broken Crown"), new Entry((V)"core:ability/regent_sigil", (V)"Hollow Sigil"))));
V v_38 = values_37[0];
foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"ward_channel"), new Entry((V)2, (V)"ward_tap"), new Entry((V)3, (V)"brute_sweep"), new Entry((V)4, (V)"acolyte_mend"), new Entry((V)5, (V)"acolyte_tap"), new Entry((V)6, (V)"rushwing_bite"), new Entry((V)7, (V)"scribe_snare"), new Entry((V)8, (V)"scribe_tap"), new Entry((V)9, (V)"siege_square"), new Entry((V)10, (V)"shield_bash"), new Entry((V)11, (V)"pin_shot"), new Entry((V)12, (V)"pin_tap"), new Entry((V)13, (V)"captain_cut"), new Entry((V)14, (V)"follower_strike")))))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
var values_42 = R.Join(new V[] {}, new Results(R.Concat((V)"core:ability/", v_41)));
V v_43 = values_42[0];
var values_44 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_45 = v_33;
V key_46 = v_43;
R.Set(obj_45, key_46, values_44[0]);
var values_47 = R.Join(new V[] {}, R.Invoke(v_41, "gsub", R.Join(new V[] {(V)"_"}, new Results((V)" "))));
var obj_48 = v_38;
V key_49 = v_43;
R.Set(obj_48, key_49, values_47[0]);
}
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_14, (V)"order"))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
var values_53 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"get"), R.Join(new V[] {}, new Results(v_52))));
V v_54 = values_53[0];
var values_55 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_56 = v_33;
V key_57 = R.Get(v_54, (V)"basic_id");
R.Set(obj_56, key_57, values_55[0]);
var values_58 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_59 = v_33;
V key_60 = R.Get(v_54, (V)"special_id");
R.Set(obj_59, key_60, values_58[0]);
var values_61 = R.Join(new V[] {}, new Results(R.Get(v_54, (V)"basic_name")));
var obj_62 = v_38;
V key_63 = R.Get(v_54, (V)"basic_id");
R.Set(obj_62, key_63, values_61[0]);
var values_64 = R.Join(new V[] {}, new Results(R.Get(v_54, (V)"special_name")));
var obj_65 = v_38;
V key_66 = R.Get(v_54, (V)"special_id");
R.Set(obj_65, key_66, values_64[0]);
}
foreach (var row_67 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_18, (V)"abilities"))))))) {
V v_68 = row_67[0];
V v_69 = row_67[1];
var values_70 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_71 = v_33;
V key_72 = R.Concat((V)"core:ability/", v_68);
R.Set(obj_71, key_72, values_70[0]);
var values_73 = R.Join(new V[] {}, new Results(v_69));
var obj_74 = v_38;
V key_75 = R.Concat((V)"core:ability/", v_68);
R.Set(obj_74, key_75, values_73[0]);
}
foreach (var row_76 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_10, (V)"profiles"))))))) {
V v_77 = row_76[0];
V v_78 = row_76[1];
foreach (var row_79 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_78)))))) {
V v_80 = row_79[0];
V v_81 = row_79[1];
var values_82 = R.Join(new V[] {}, new Results(R.Concat((V)"core:ability/elite_", v_81)));
V v_83 = values_82[0];
var values_84 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_85 = v_33;
V key_86 = v_83;
R.Set(obj_85, key_86, values_84[0]);
var values_87 = R.Join(new V[] {}, R.Invoke(v_81, "gsub", R.Join(new V[] {(V)"_"}, new Results((V)" "))));
var obj_88 = v_38;
V key_89 = v_83;
R.Set(obj_88, key_89, values_87[0]);
}
}
foreach (var row_90 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"mossbreaker_crack"), new Entry((V)2, (V)"mossbreaker_cleave"), new Entry((V)3, (V)"gloom_fork"), new Entry((V)4, (V)"mire_ring"), new Entry((V)5, (V)"pack_chorus"), new Entry((V)6, (V)"vault_ambush"), new Entry((V)7, (V)"crystal_barrage"), new Entry((V)8, (V)"cinder_mend"), new Entry((V)9, (V)"cinder_tap"), new Entry((V)10, (V)"glass_charge"), new Entry((V)11, (V)"glass_jab"), new Entry((V)12, (V)"forge_call"), new Entry((V)13, (V)"forge_tap"), new Entry((V)14, (V)"bulwark_retort"), new Entry((V)15, (V)"bulwark_bash")))))))) {
V v_91 = row_90[0];
V v_92 = row_90[1];
var values_93 = R.Join(new V[] {}, new Results(R.Concat((V)"core:ability/", v_92)));
V v_94 = values_93[0];
var values_95 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_96 = v_33;
V key_97 = v_94;
R.Set(obj_96, key_97, values_95[0]);
var values_98 = R.Join(new V[] {}, R.Invoke(v_92, "gsub", R.Join(new V[] {(V)"_"}, new Results((V)" "))));
var obj_99 = v_38;
V key_100 = v_94;
R.Set(obj_99, key_100, values_98[0]);
}
var values_101 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:enemy/hollow_regent", R.Table(new Entry((V)"id", (V)"core:enemy/hollow_regent"), new Entry((V)"name", (V)"Hollow Regent"), new Entry((V)"ai", (V)"hollow_regent"), new Entry((V)"hp", (V)62.0d), new Entry((V)"damage", (V)4.0d), new Entry((V)"defense", (V)1.0d), new Entry((V)"speed", (V)75.0d), new Entry((V)"windup", (V)110.0d), new Entry((V)"recovery", (V)145.0d), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", (V)6.0d), new Entry((V)"xp_reward", (V)0.0d), new Entry((V)"currency_drop", (V)0.0d))), new Entry((V)"core:enemy/thorn_warden", R.Table(new Entry((V)"id", (V)"core:enemy/thorn_warden"), new Entry((V)"name", (V)"Thorn Warden"), new Entry((V)"ai", (V)"warden"), new Entry((V)"hp", (V)44.0d), new Entry((V)"damage", (V)5.0d), new Entry((V)"defense", (V)1.0d), new Entry((V)"speed", (V)85.0d), new Entry((V)"windup", (V)85.0d), new Entry((V)"recovery", (V)120.0d), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", (V)4.0d), new Entry((V)"xp_reward", (V)12.0d), new Entry((V)"currency_drop", (V)8.0d))), new Entry((V)"core:enemy/extraction_pursuer", R.Table(new Entry((V)"id", (V)"core:enemy/extraction_pursuer"), new Entry((V)"name", (V)"Extraction Pursuer"), new Entry((V)"ai", (V)"pursuer"), new Entry((V)"hp", (V)1.0d), new Entry((V)"damage", (V)9.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)190.0d), new Entry((V)"windup", (V)45.0d), new Entry((V)"recovery", (V)55.0d), new Entry((V)"move_cost", (V)70.0d), new Entry((V)"range", (V)1.0d), new Entry((V)"xp_reward", (V)0.0d), new Entry((V)"currency_drop", (V)0.0d), new Entry((V)"invulnerable", (V)true))), new Entry((V)"core:enemy/mossbound_guard", R.Table(new Entry((V)"id", (V)"core:enemy/mossbound_guard"), new Entry((V)"name", (V)"Mossbound Guard"), new Entry((V)"ai", (V)"guard"), new Entry((V)"hp", (V)10.0d), new Entry((V)"damage", (V)3.0d), new Entry((V)"defense", (V)1.0d), new Entry((V)"speed", (V)70.0d), new Entry((V)"windup", (V)80.0d), new Entry((V)"recovery", (V)120.0d), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", (V)1.0d), new Entry((V)"xp_reward", (V)5.0d), new Entry((V)"currency_drop", (V)4.0d))), new Entry((V)"core:enemy/thornspitter", R.Table(new Entry((V)"id", (V)"core:enemy/thornspitter"), new Entry((V)"name", (V)"Thornspitter"), new Entry((V)"ai", (V)"spitter"), new Entry((V)"hp", (V)6.0d), new Entry((V)"damage", (V)2.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)90.0d), new Entry((V)"windup", (V)90.0d), new Entry((V)"recovery", (V)130.0d), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"min_range", (V)2.0d), new Entry((V)"range", (V)7.0d), new Entry((V)"xp_reward", (V)4.0d), new Entry((V)"currency_drop", (V)3.0d))), new Entry((V)"core:enemy/ruin_skitter", R.Table(new Entry((V)"id", (V)"core:enemy/ruin_skitter"), new Entry((V)"name", (V)"Ruin Skitter"), new Entry((V)"ai", (V)"skitter"), new Entry((V)"hp", (V)4.0d), new Entry((V)"damage", (V)1.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)160.0d), new Entry((V)"windup", (V)35.0d), new Entry((V)"recovery", (V)70.0d), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", (V)1.0d), new Entry((V)"xp_reward", (V)3.0d), new Entry((V)"currency_drop", (V)2.0d))), new Entry((V)"core:enemy/spore_herald", R.Table(new Entry((V)"id", (V)"core:enemy/spore_herald"), new Entry((V)"name", (V)"Spore Herald"), new Entry((V)"ai", (V)"spore_herald"), new Entry((V)"hp", (V)7.0d), new Entry((V)"damage", (V)2.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)85.0d), new Entry((V)"windup", (V)100.0d), new Entry((V)"recovery", (V)135.0d), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", (V)5.0d), new Entry((V)"xp_reward", (V)5.0d), new Entry((V)"currency_drop", (V)3.0d))), new Entry((V)"core:enemy/canal_lancer", R.Table(new Entry((V)"id", (V)"core:enemy/canal_lancer"), new Entry((V)"name", (V)"Canal Lancer"), new Entry((V)"ai", (V)"canal_lancer"), new Entry((V)"hp", (V)9.0d), new Entry((V)"damage", (V)3.0d), new Entry((V)"defense", (V)1.0d), new Entry((V)"speed", (V)90.0d), new Entry((V)"windup", (V)75.0d), new Entry((V)"recovery", (V)120.0d), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", (V)2.0d), new Entry((V)"xp_reward", (V)5.0d), new Entry((V)"currency_drop", (V)4.0d))), new Entry((V)"core:enemy/cinder_bellkeeper", R.Table(new Entry((V)"id", (V)"core:enemy/cinder_bellkeeper"), new Entry((V)"name", (V)"Cinder Bellkeeper"), new Entry((V)"ai", (V)"cinder_bellkeeper"), new Entry((V)"hp", (V)12.0d), new Entry((V)"damage", (V)3.0d), new Entry((V)"defense", (V)1.0d), new Entry((V)"speed", (V)65.0d), new Entry((V)"windup", (V)115.0d), new Entry((V)"recovery", (V)145.0d), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", (V)4.0d), new Entry((V)"xp_reward", (V)6.0d), new Entry((V)"currency_drop", (V)4.0d))))));
V v_102 = values_101[0];
var values_103 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"mycelial_matriarch"), new Entry((V)2, (V)"Mycelial Matriarch"), new Entry((V)3, (V)48.0d), new Entry((V)4, (V)4.0d), new Entry((V)5, (V)75.0d), new Entry((V)6, (V)105.0d), new Entry((V)7, (V)135.0d), new Entry((V)8, (V)5.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)"canal_lockmaster"), new Entry((V)2, (V)"Canal Lockmaster"), new Entry((V)3, (V)50.0d), new Entry((V)4, (V)4.0d), new Entry((V)5, (V)80.0d), new Entry((V)6, (V)95.0d), new Entry((V)7, (V)130.0d), new Entry((V)8, (V)5.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)"ash_marshal"), new Entry((V)2, (V)"Ash Marshal"), new Entry((V)3, (V)52.0d), new Entry((V)4, (V)5.0d), new Entry((V)5, (V)70.0d), new Entry((V)6, (V)110.0d), new Entry((V)7, (V)140.0d), new Entry((V)8, (V)5.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)"prism_curator"), new Entry((V)2, (V)"Prism Curator"), new Entry((V)3, (V)46.0d), new Entry((V)4, (V)4.0d), new Entry((V)5, (V)85.0d), new Entry((V)6, (V)100.0d), new Entry((V)7, (V)130.0d), new Entry((V)8, (V)6.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)"index_keeper"), new Entry((V)2, (V)"Index Keeper"), new Entry((V)3, (V)48.0d), new Entry((V)4, (V)4.0d), new Entry((V)5, (V)80.0d), new Entry((V)6, (V)105.0d), new Entry((V)7, (V)135.0d), new Entry((V)8, (V)6.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)"ember_crucible"), new Entry((V)2, (V)"Ember Crucible"), new Entry((V)3, (V)54.0d), new Entry((V)4, (V)5.0d), new Entry((V)5, (V)65.0d), new Entry((V)6, (V)115.0d), new Entry((V)7, (V)145.0d), new Entry((V)8, (V)5.0d))))));
V v_104 = values_103[0];
foreach (var row_105 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_104)))))) {
V v_106 = row_105[0];
V v_107 = row_105[1];
var values_108 = R.Join(new V[] {}, new Results(R.Concat((V)"core:enemy/", R.Get(v_107, (V)1.0d))));
V v_109 = values_108[0];
var values_110 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_109), new Entry((V)"name", R.Get(v_107, (V)2.0d)), new Entry((V)"ai", (V)"biome_elite"), new Entry((V)"hp", R.Get(v_107, (V)3.0d)), new Entry((V)"damage", R.Get(v_107, (V)4.0d)), new Entry((V)"defense", (V)1.0d), new Entry((V)"speed", R.Get(v_107, (V)5.0d)), new Entry((V)"windup", R.Get(v_107, (V)6.0d)), new Entry((V)"recovery", R.Get(v_107, (V)7.0d)), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", R.Get(v_107, (V)8.0d)), new Entry((V)"xp_reward", (V)12.0d), new Entry((V)"currency_drop", (V)8.0d))));
var obj_111 = v_102;
V key_112 = v_109;
R.Set(obj_111, key_112, values_110[0]);
}
var values_113 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"mossbreaker"), new Entry((V)2, (V)"Mossbreaker"), new Entry((V)3, (V)13.0d), new Entry((V)4, (V)3.0d), new Entry((V)5, (V)1.0d), new Entry((V)6, (V)65.0d), new Entry((V)7, (V)100.0d), new Entry((V)8, (V)130.0d), new Entry((V)9, (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)"gloom_strider"), new Entry((V)2, (V)"Gloom Strider"), new Entry((V)3, (V)6.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)120.0d), new Entry((V)7, (V)85.0d), new Entry((V)8, (V)115.0d), new Entry((V)9, (V)5.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)"mire_binder"), new Entry((V)2, (V)"Mire Binder"), new Entry((V)3, (V)8.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)75.0d), new Entry((V)7, (V)120.0d), new Entry((V)8, (V)145.0d), new Entry((V)9, (V)4.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)"pack_chorister"), new Entry((V)2, (V)"Pack Chorister"), new Entry((V)3, (V)9.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)85.0d), new Entry((V)7, (V)85.0d), new Entry((V)8, (V)115.0d), new Entry((V)9, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)"vault_lurker"), new Entry((V)2, (V)"Vault Lurker"), new Entry((V)3, (V)7.0d), new Entry((V)4, (V)3.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)100.0d), new Entry((V)7, (V)120.0d), new Entry((V)8, (V)135.0d), new Entry((V)9, (V)4.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)"crystal_bombardier"), new Entry((V)2, (V)"Crystal Bombardier"), new Entry((V)3, (V)7.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)70.0d), new Entry((V)7, (V)125.0d), new Entry((V)8, (V)150.0d), new Entry((V)9, (V)5.0d))), new Entry((V)7, R.Table(new Entry((V)1, (V)"cinder_mender"), new Entry((V)2, (V)"Cinder Mender"), new Entry((V)3, (V)8.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)75.0d), new Entry((V)7, (V)105.0d), new Entry((V)8, (V)125.0d), new Entry((V)9, (V)3.0d))), new Entry((V)8, R.Table(new Entry((V)1, (V)"glass_charger"), new Entry((V)2, (V)"Glass Charger"), new Entry((V)3, (V)9.0d), new Entry((V)4, (V)3.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)110.0d), new Entry((V)7, (V)100.0d), new Entry((V)8, (V)120.0d), new Entry((V)9, (V)4.0d))), new Entry((V)9, R.Table(new Entry((V)1, (V)"forge_conjurer"), new Entry((V)2, (V)"Forge Conjurer"), new Entry((V)3, (V)9.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)70.0d), new Entry((V)7, (V)120.0d), new Entry((V)8, (V)145.0d), new Entry((V)9, (V)5.0d))), new Entry((V)10, R.Table(new Entry((V)1, (V)"bastion_bulwark"), new Entry((V)2, (V)"Bastion Bulwark"), new Entry((V)3, (V)13.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)2.0d), new Entry((V)6, (V)55.0d), new Entry((V)7, (V)105.0d), new Entry((V)8, (V)135.0d), new Entry((V)9, (V)2.0d))))));
V v_114 = values_113[0];
foreach (var row_115 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_114)))))) {
V v_116 = row_115[0];
V v_117 = row_115[1];
var values_118 = R.Join(new V[] {}, new Results(R.Concat((V)"core:enemy/", R.Get(v_117, (V)1.0d))));
V v_119 = values_118[0];
var values_120 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_119), new Entry((V)"name", R.Get(v_117, (V)2.0d)), new Entry((V)"ai", (V)"expansion_regular"), new Entry((V)"hp", R.Get(v_117, (V)3.0d)), new Entry((V)"damage", R.Get(v_117, (V)4.0d)), new Entry((V)"defense", R.Get(v_117, (V)5.0d)), new Entry((V)"speed", R.Get(v_117, (V)6.0d)), new Entry((V)"windup", R.Get(v_117, (V)7.0d)), new Entry((V)"recovery", R.Get(v_117, (V)8.0d)), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", R.Get(v_117, (V)9.0d)), new Entry((V)"xp_reward", (V)4.0d), new Entry((V)"currency_drop", (V)3.0d))));
var obj_121 = v_102;
V key_122 = v_119;
R.Set(obj_121, key_122, values_120[0]);
}
foreach (var row_123 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_16, (V)"enemies"))))))) {
V v_124 = row_123[0];
V v_125 = row_123[1];
var values_126 = R.Join(new V[] {}, new Results(R.Concat((V)"core:enemy/", R.Get(v_125, (V)1.0d))));
V v_127 = values_126[0];
var values_128 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_127), new Entry((V)"name", R.Get(v_125, (V)2.0d)), new Entry((V)"ai", (V)"squad"), new Entry((V)"hp", R.Get(v_125, (V)3.0d)), new Entry((V)"damage", R.Get(v_125, (V)4.0d)), new Entry((V)"defense", R.Get(v_125, (V)5.0d)), new Entry((V)"speed", R.Get(v_125, (V)6.0d)), new Entry((V)"windup", R.Get(v_125, (V)7.0d)), new Entry((V)"recovery", R.Get(v_125, (V)8.0d)), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", R.Get(v_125, (V)9.0d)), new Entry((V)"xp_reward", (V)2.0d), new Entry((V)"currency_drop", (V)1.0d))));
var obj_129 = v_102;
V key_130 = v_127;
R.Set(obj_129, key_130, values_128[0]);
}
foreach (var row_131 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_18, (V)"enemies"))))))) {
V v_132 = row_131[0];
V v_133 = row_131[1];
var values_134 = R.Join(new V[] {}, new Results(R.Concat((V)"core:enemy/", R.Get(v_133, (V)1.0d))));
V v_135 = values_134[0];
var values_136 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_135), new Entry((V)"name", R.Get(v_133, (V)2.0d)), new Entry((V)"ai", (V)"idea_regular"), new Entry((V)"hp", R.Get(v_133, (V)3.0d)), new Entry((V)"damage", R.Get(v_133, (V)4.0d)), new Entry((V)"defense", R.Get(v_133, (V)5.0d)), new Entry((V)"speed", R.Get(v_133, (V)6.0d)), new Entry((V)"windup", R.Get(v_133, (V)7.0d)), new Entry((V)"recovery", R.Get(v_133, (V)8.0d)), new Entry((V)"move_cost", (V)100.0d), new Entry((V)"range", R.Get(v_133, (V)9.0d)), new Entry((V)"xp_reward", (V)4.0d), new Entry((V)"currency_drop", (V)3.0d), new Entry((V)"special_encounter", R.Or(R.And(R.Get(v_18, (V)"champions"), () => R.Get(R.Get(v_18, (V)"champions"), v_135)), () => V.Nil)))));
var obj_137 = v_102;
V key_138 = v_135;
R.Set(obj_137, key_138, values_136[0]);
}
var values_139 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"definition_id", (V)"core:enemy/mossbound_guard"), new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)3.0d))), new Entry((V)2, R.Table(new Entry((V)"definition_id", (V)"core:enemy/thornspitter"), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)9.0d))), new Entry((V)3, R.Table(new Entry((V)"definition_id", (V)"core:enemy/ruin_skitter"), new Entry((V)"x", (V)6.0d), new Entry((V)"y", (V)6.0d))))));
V v_140 = values_139[0];
var values_141 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"definition_id", (V)"core:chest/standard"), new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)4.0d))))));
V v_142 = values_141[0];
var values_143 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:map/test_room", R.Table(new Entry((V)"id", (V)"core:map/test_room"), new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"width", (V)15.0d), new Entry((V)"height", (V)11.0d), new Entry((V)"start_x", (V)2.0d), new Entry((V)"start_y", (V)2.0d), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"###############"), new Entry((V)2, (V)"#.............#"), new Entry((V)3, (V)"#..###........#"), new Entry((V)4, (V)"#.............#"), new Entry((V)5, (V)"#.......###...#"), new Entry((V)6, (V)"#.............#"), new Entry((V)7, (V)"#....#........#"), new Entry((V)8, (V)"#....#........#"), new Entry((V)9, (V)"#.............#"), new Entry((V)10, (V)"#.............#"), new Entry((V)11, (V)"###############"))))), new Entry((V)"core:map/scrolling_room", R.Table(new Entry((V)"id", (V)"core:map/scrolling_room"), new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"width", (V)35.0d), new Entry((V)"height", (V)25.0d), new Entry((V)"start_x", (V)3.0d), new Entry((V)"start_y", (V)3.0d), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"###################################"), new Entry((V)2, (V)"#.................................#"), new Entry((V)3, (V)"#.................................#"), new Entry((V)4, (V)"#....####.........................#"), new Entry((V)5, (V)"#.................................#"), new Entry((V)6, (V)"#.....................#####.......#"), new Entry((V)7, (V)"#.................................#"), new Entry((V)8, (V)"#.................................#"), new Entry((V)9, (V)"#.........######..................#"), new Entry((V)10, (V)"#.................................#"), new Entry((V)11, (V)"#.................................#"), new Entry((V)12, (V)"#.................................."), new Entry((V)13, (V)"#..................####...........#"), new Entry((V)14, (V)"#.................................#"), new Entry((V)15, (V)"#.................................#"), new Entry((V)16, (V)"#......#####......................#"), new Entry((V)17, (V)"#.................................#"), new Entry((V)18, (V)"#.................................#"), new Entry((V)19, (V)"#.........................#####...#"), new Entry((V)20, (V)"#.................................#"), new Entry((V)21, (V)"#.................................#"), new Entry((V)22, (V)"#.............####................#"), new Entry((V)23, (V)"#.................................#"), new Entry((V)24, (V)"#.................................#"), new Entry((V)25, (V)"###################################"))))))));
V v_144 = values_143[0];
R.Set(v_2, (V)"get_map", new V((RuleFunction)(args_145 => {
V v_146 = args_145[0];
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_144, v_146), () => R.Or(R.And((V)(v_146 == R.Get(R.Get(v_6, (V)"map"), (V)"id")), () => R.Get(v_6, (V)"map")), () => V.Nil))));
return Results.Empty;
})));
R.Set(v_2, (V)"map_for", new V((RuleFunction)(args_147 => {
V v_148 = args_147[0];
return R.Join(new V[] {}, new Results(R.Or(R.Or(R.Get(v_148, (V)"generated_map"), () => R.Get(v_148, (V)"final_map")), () => R.Call(R.Get(v_2, (V)"get_map"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"map_id")))).First)));
return Results.Empty;
})));
R.Set(v_2, (V)"get_enemy", new V((RuleFunction)(args_149 => {
V v_150 = args_149[0];
return R.Join(new V[] {}, new Results(R.Get(v_102, v_150)));
return Results.Empty;
})));
R.Set(v_2, (V)"get_adventurer", new V((RuleFunction)(args_151 => {

return R.Join(new V[] {}, new Results(v_25));
return Results.Empty;
})));
R.Set(v_2, (V)"get_class", new V((RuleFunction)(args_152 => {
V v_153 = args_152[0];
return R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"get"), R.Join(new V[] {}, new Results(v_153))));
return Results.Empty;
})));
R.Set(v_2, (V)"get_progression", new V((RuleFunction)(args_154 => {

return R.Join(new V[] {}, new Results(v_27));
return Results.Empty;
})));
R.Set(v_2, (V)"get_stage_rules", new V((RuleFunction)(args_155 => {

return R.Join(new V[] {}, new Results(v_31));
return Results.Empty;
})));
R.Set(v_2, (V)"get_chest", new V((RuleFunction)(args_156 => {
V v_157 = args_156[0];
return R.Join(new V[] {}, new Results(R.Get(v_29, v_157)));
return Results.Empty;
})));
R.Set(v_2, (V)"demo_chests", new V((RuleFunction)(args_158 => {
V v_159 = args_158[0];
if (R.Truth((V)(v_159 == (V)"core:map/scrolling_room"))) {
return R.Join(new V[] {}, new Results(v_142));
}
return R.Join(new V[] {}, new Results(R.Table()));
return Results.Empty;
})));
R.Set(v_2, (V)"ability_proc_coefficient", new V((RuleFunction)(args_160 => {
V v_161 = args_160[0];
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_33, v_161), () => (V)1.0d)));
return Results.Empty;
})));
R.Set(v_2, (V)"has_ability", new V((RuleFunction)(args_162 => {
V v_163 = args_162[0];
return R.Join(new V[] {}, new Results((V)(R.Get(v_33, v_163) != V.Nil)));
return Results.Empty;
})));
R.Set(v_2, (V)"ability_name", new V((RuleFunction)(args_164 => {
V v_165 = args_164[0];
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_38, v_165), () => R.Invoke(R.Invoke(v_165, "gsub", R.Join(new V[] {(V)"^core:ability/"}, new Results((V)""))).First, "gsub", R.Join(new V[] {(V)"_"}, new Results((V)" "))).First)));
return Results.Empty;
})));
R.Set(v_2, (V)"demo_spawns", new V((RuleFunction)(args_166 => {
V v_167 = args_166[0];
if (R.Truth((V)(v_167 == (V)"core:map/scrolling_room"))) {
return R.Join(new V[] {}, new Results(v_140));
}
return R.Join(new V[] {}, new Results(R.Table()));
return Results.Empty;
})));
V v_168 = V.Nil;
v_168 = new V((RuleFunction)(args_169 => {
V v_170 = args_169[0];
V v_171 = args_169[1];
V v_172 = args_169[2];
if (R.Truth(R.Or(R.Or(R.Or((V)(v_171 < (V)1.0d), () => (V)(v_171 > R.Get(v_170, (V)"width"))), () => (V)(v_172 < (V)1.0d)), () => (V)(v_172 > R.Get(v_170, (V)"height"))))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
return R.Join(new V[] {}, new Results(R.Get(v_23, R.Invoke(R.Get(R.Get(v_170, (V)"rows"), v_172), "sub", R.Join(new V[] {v_171}, new Results(v_171))).First)));
return Results.Empty;
}));
R.Set(v_2, (V)"validate_map", new V((RuleFunction)(args_173 => {
V v_174 = args_173[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_174))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_174, (V)"id")))).First == (V)"string")), () => (V)(R.Get(v_174, (V)"id") != (V)""))}, new Results((V)"map needs a stable ID")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_174, (V)"floor_id")))).First == (V)"string"), () => (V)(R.Get(v_174, (V)"floor_id") != (V)""))}, new Results((V)"map needs a floor ID")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_174, (V)"width")))).First == (V)"number"), () => (V)(R.Get(v_174, (V)"width") > (V)0.0d)), () => (V)((V)(R.Get(v_174, (V)"width") % (V)1.0d) == (V)0.0d))}, new Results((V)"invalid map width")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_174, (V)"height")))).First == (V)"number"), () => (V)(R.Get(v_174, (V)"height") > (V)0.0d)), () => (V)((V)(R.Get(v_174, (V)"height") % (V)1.0d) == (V)0.0d))}, new Results((V)"invalid map height")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_174, (V)"rows")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_174, (V)"rows")) == R.Get(v_174, (V)"height")))}, new Results((V)"invalid map row count")));
{ double end_177 = R.Num(R.Get(v_174, (V)"height")), step_178 = R.Num((V)1.0d);
for (double i_176 = R.Num((V)1.0d); step_178 > 0 ? i_176 <= end_177 : i_176 >= end_177; i_176 += step_178) {
V v_175 = i_176;
var values_179 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_174, (V)"rows"), v_175)));
V v_180 = values_179[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_180))).First == (V)"string"), () => (V)((V)R.Length(v_180) == R.Get(v_174, (V)"width")))}, new Results((V)"invalid map row width")));
{ double end_183 = R.Num((V)R.Length(v_180)), step_184 = R.Num((V)1.0d);
for (double i_182 = R.Num((V)1.0d); step_184 > 0 ? i_182 <= end_183 : i_182 >= end_183; i_182 += step_184) {
V v_181 = i_182;
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_8, (V)"is_valid"), R.Join(new V[] {}, R.Invoke(v_180, "sub", R.Join(new V[] {v_181}, new Results(v_181))))).First}, new Results((V)"unknown map tile")));
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_174, (V)"start_x")))).First == (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_174, (V)"start_y")))).First == (V)"number"))}, new Results((V)"missing start")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)((V)(R.Get(v_174, (V)"start_x") % (V)1.0d) == (V)0.0d), () => (V)((V)(R.Get(v_174, (V)"start_y") % (V)1.0d) == (V)0.0d))}, new Results((V)"start must be on grid")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_174, R.Get(v_174, (V)"start_x")}, new Results(R.Get(v_174, (V)"start_y")))).First}, new Results((V)"start must be walkable")));
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, (V)"walkable", new V((RuleFunction)(args_185 => {
V v_186 = args_185[0];
V v_187 = args_185[1];
V v_188 = args_185[2];
var values_189 = R.Join(new V[] {}, R.Call(v_168, R.Join(new V[] {v_186, v_187}, new Results(v_188))));
V v_190 = values_189[0];
return R.Join(new V[] {}, new Results(R.And((V)(v_190 != V.Nil), () => R.Get(v_190, (V)"walkable"))));
return Results.Empty;
})));
V v_191 = V.Nil;
v_191 = new V((RuleFunction)(args_192 => {
V v_193 = args_192[0];
V v_194 = args_192[1];
V v_195 = args_192[2];
V v_196 = args_192[3];
V v_197 = args_192[4];
if (R.Truth((V)(!R.Truth(v_193)))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_198 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_193, (V)"entities"))))))) {
V v_199 = row_198[0];
V v_200 = row_198[1];
var values_201 = R.Join(new V[] {}, new Results(R.Get(v_200, (V)"position")));
V v_202 = values_201[0];
if (R.Truth(R.And(R.And(R.And(R.And(v_202, () => (V)(R.Get(v_202, (V)"floor_id") == R.Get(v_194, (V)"floor_id"))), () => (V)(R.Get(v_202, (V)"x") == v_195)), () => (V)(R.Get(v_202, (V)"y") == v_196)), () => R.Get(v_200, v_197)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
R.Set(v_2, (V)"projectile_passable", new V((RuleFunction)(args_203 => {
V v_204 = args_203[0];
V v_205 = args_203[1];
V v_206 = args_203[2];
V v_207 = args_203[3];
var values_208 = R.Join(new V[] {}, R.Call(v_168, R.Join(new V[] {v_204, v_205}, new Results(v_206))));
V v_209 = values_208[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(v_209 != V.Nil), () => R.Get(v_209, (V)"projectile_passable")), () => (V)(!R.Truth(R.Call(v_191, R.Join(new V[] {v_207, v_204, v_205, v_206}, new Results((V)"blocks_projectiles"))).First)))));
return Results.Empty;
})));
R.Set(v_2, (V)"mist_blocks", new V((RuleFunction)(args_210 => {
V v_211 = args_210[0];
V v_212 = args_210[1];
V v_213 = args_210[2];
V v_214 = args_210[3];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_211)), () => (V)(!R.Truth(R.Get(R.Get(v_211, (V)"rows"), v_213)))), () => (V)(R.Invoke(R.Get(R.Get(v_211, (V)"rows"), v_213), "sub", R.Join(new V[] {v_212}, new Results(v_212))).First != (V)"m")))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_215 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(v_214, () => R.Get(v_214, (V)"review_features")), () => R.Table()))))))) {
V v_216 = row_215[0];
V v_217 = row_215[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_217, (V)"kind") == (V)"brazier"), () => (V)(R.Get(v_217, (V)"floor_id") == R.Get(v_211, (V)"floor_id"))), () => (V)(R.Or(R.Get(v_217, (V)"lit_until"), () => (V)0.0d) > R.Or(R.Get(v_214, (V)"clock"), () => (V)0.0d))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_217, (V)"x") - v_212)))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_217, (V)"y") - v_213)))).First) <= (V)2.0d)))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, (V)"blocks_vision", new V((RuleFunction)(args_218 => {
V v_219 = args_218[0];
V v_220 = args_218[1];
V v_221 = args_218[2];
V v_222 = args_218[3];
var values_223 = R.Join(new V[] {}, R.Call(v_168, R.Join(new V[] {v_219, v_220}, new Results(v_221))));
V v_224 = values_223[0];
return R.Join(new V[] {}, new Results(R.Or(R.Or((V)(v_224 == V.Nil), () => R.And(R.Get(v_224, (V)"blocks_vision"), () => R.Or(R.Or((V)(!R.Truth(R.Get(R.Get(v_219, (V)"rows"), v_221))), () => (V)(R.Invoke(R.Get(R.Get(v_219, (V)"rows"), v_221), "sub", R.Join(new V[] {v_220}, new Results(v_220))).First != (V)"m")), () => R.Call(R.Get(v_2, (V)"mist_blocks"), R.Join(new V[] {v_219, v_220, v_221}, new Results(v_222))).First))), () => R.Call(v_191, R.Join(new V[] {v_222, v_219, v_220, v_221}, new Results((V)"blocks_vision"))).First)));
return Results.Empty;
})));
R.Set(v_2, (V)"validate_all", new V((RuleFunction)(args_225 => {

R.Call(R.Get(v_8, (V)"validate"), Results.Empty);
R.Call(R.Get(v_10, (V)"validate"), Results.Empty);
R.Call(R.Get(v_12, (V)"validate"), Results.Empty);
R.Call(R.Get(v_14, (V)"validate"), Results.Empty);
R.Call(R.Get(v_4, (V)"validate"), Results.Empty);
V v_226 = V.Nil;
v_226 = new V((RuleFunction)(args_227 => {
V v_228 = args_227[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_228))).First == (V)"number"), () => (V)((V)(v_228 % (V)1.0d) == (V)0.0d)), () => (V)(v_228 > (V)0.0d))));
return Results.Empty;
}));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(v_226, R.Join(new V[] {}, new Results(R.Get(v_27, (V)"first_threshold")))).First, () => R.Call(v_226, R.Join(new V[] {}, new Results(R.Get(v_27, (V)"threshold_step")))).First)}, new Results((V)"invalid XP curve")));
var values_229 = R.Join(new V[] {}, new Results(R.Table()));
V v_230 = values_229[0];
var values_231 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"max_health", (V)true), new Entry((V)"damage", (V)true), new Entry((V)"defense", (V)true), new Entry((V)"speed", (V)true), new Entry((V)"crit_chance", (V)true), new Entry((V)"crit_damage", (V)true))));
V v_232 = values_231[0];
foreach (var row_233 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_27, (V)"stat_order"))))))) {
V v_234 = row_233[0];
V v_235 = row_233[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.Get(v_232, v_235), () => (V)(!R.Truth(R.Get(v_230, v_235)))), () => R.Call(v_226, R.Join(new V[] {}, new Results(R.Get(R.Get(v_27, (V)"stat_weights"), v_235)))).First), () => R.Call(v_226, R.Join(new V[] {}, new Results(R.Get(R.Get(v_27, (V)"growth"), v_235)))).First)}, new Results((V)"invalid stat growth")));
var values_236 = R.Join(new V[] {}, new Results((V)true));
var obj_237 = v_230;
V key_238 = v_235;
R.Set(obj_237, key_238, values_236[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_27, (V)"stat_order")) == (V)6.0d)}, new Results((V)"expected six progression stats")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.Call(v_226, R.Join(new V[] {}, new Results(R.Get(v_25, (V)"crit_damage_percent")))).First, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_25, (V)"crit_chance_ppm")))).First == (V)"number")), () => (V)((V)(R.Get(v_25, (V)"crit_chance_ppm") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_25, (V)"crit_chance_ppm") >= (V)0.0d))}, new Results((V)"invalid starting crit stats")));
foreach (var row_239 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:chest/standard"), new Entry((V)2, (V)"core:chest/free_elite")))))))) {
V v_240 = row_239[0];
V v_241 = row_239[1];
var values_242 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_29, v_241)}, new Results((V)"missing chest definition"))));
V v_243 = values_242[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Get(v_243, (V)"id") == v_241), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_243, (V)"name")))).First == (V)"string")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_243, (V)"price")))).First == (V)"number")), () => (V)((V)(R.Get(v_243, (V)"price") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_243, (V)"price") >= (V)0.0d))}, new Results((V)"invalid chest definition")));
var values_244 = R.Join(new V[] {}, new Results((V)0.0d));
V v_245 = values_244[0];
foreach (var row_246 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"common"), new Entry((V)2, (V)"uncommon"), new Entry((V)3, (V)"rare"), new Entry((V)4, (V)"legendary")))))))) {
V v_247 = row_246[0];
V v_248 = row_246[1];
var values_249 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_243, (V)"rarity_weights"), v_248)));
V v_250 = values_249[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_250))).First == (V)"number"), () => (V)((V)(v_250 % (V)1.0d) == (V)0.0d)), () => (V)(v_250 >= (V)0.0d))}, new Results((V)"invalid chest rarity weight")));
var values_251 = R.Join(new V[] {}, new Results((V)(v_245 + v_250)));
v_245 = values_251[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_245 > (V)0.0d)}, new Results((V)"empty chest rarity profile")));
}
foreach (var row_252 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_33)))))) {
V v_253 = row_252[0];
V v_254 = row_252[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_254))).First == (V)"number"), () => (V)(v_254 >= (V)0.0d)), () => (V)(v_254 <= (V)10.0d))}, new Results((V)"invalid ability proc coefficient")));
}
var values_255 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:map/test_room"), new Entry((V)2, (V)"core:map/scrolling_room"), new Entry((V)3, R.Get(R.Get(v_6, (V)"map"), (V)"id")))));
V v_256 = values_255[0];
var values_257 = R.Join(new V[] {}, new Results(R.Table()));
V v_258 = values_257[0];
foreach (var row_259 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_256)))))) {
V v_260 = row_259[0];
V v_261 = row_259[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_258, v_261)))}, new Results((V)"duplicate content ID")));
var values_262 = R.Join(new V[] {}, new Results((V)true));
var obj_263 = v_258;
V key_264 = v_261;
R.Set(obj_263, key_264, values_262[0]);
var values_265 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"get_map"), R.Join(new V[] {}, new Results(v_261))).First}, new Results((V)"missing map definition"))));
V v_266 = values_265[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_266, (V)"id") == v_261)}, new Results((V)"map definition ID mismatch")));
R.Call(R.Get(v_2, (V)"validate_map"), R.Join(new V[] {}, new Results(v_266)));
}
var values_267 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:enemy/mossbound_guard"), new Entry((V)2, (V)"core:enemy/thornspitter"), new Entry((V)3, (V)"core:enemy/ruin_skitter"), new Entry((V)4, (V)"core:enemy/spore_herald"), new Entry((V)5, (V)"core:enemy/canal_lancer"), new Entry((V)6, (V)"core:enemy/cinder_bellkeeper"), new Entry((V)7, (V)"core:enemy/thorn_warden"), new Entry((V)8, (V)"core:enemy/extraction_pursuer"), new Entry((V)9, (V)"core:enemy/hollow_regent"))));
V v_268 = values_267[0];
foreach (var row_269 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_104)))))) {
V v_270 = row_269[0];
V v_271 = row_269[1];
var values_272 = R.Join(new V[] {}, new Results(R.Concat((V)"core:enemy/", R.Get(v_271, (V)1.0d))));
var obj_273 = v_268;
V key_274 = (V)((V)R.Length(v_268) + (V)1.0d);
R.Set(obj_273, key_274, values_272[0]);
}
foreach (var row_275 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_114)))))) {
V v_276 = row_275[0];
V v_277 = row_275[1];
var values_278 = R.Join(new V[] {}, new Results(R.Concat((V)"core:enemy/", R.Get(v_277, (V)1.0d))));
var obj_279 = v_268;
V key_280 = (V)((V)R.Length(v_268) + (V)1.0d);
R.Set(obj_279, key_280, values_278[0]);
}
foreach (var row_281 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_16, (V)"enemies"))))))) {
V v_282 = row_281[0];
V v_283 = row_281[1];
var values_284 = R.Join(new V[] {}, new Results(R.Concat((V)"core:enemy/", R.Get(v_283, (V)1.0d))));
var obj_285 = v_268;
V key_286 = (V)((V)R.Length(v_268) + (V)1.0d);
R.Set(obj_285, key_286, values_284[0]);
}
foreach (var row_287 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_18, (V)"enemies"))))))) {
V v_288 = row_287[0];
V v_289 = row_287[1];
var values_290 = R.Join(new V[] {}, new Results(R.Concat((V)"core:enemy/", R.Get(v_289, (V)1.0d))));
var obj_291 = v_268;
V key_292 = (V)((V)R.Length(v_268) + (V)1.0d);
R.Set(obj_291, key_292, values_290[0]);
}
foreach (var row_293 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_268)))))) {
V v_294 = row_293[0];
V v_295 = row_293[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_258, v_295)))}, new Results((V)"duplicate content ID")));
var values_296 = R.Join(new V[] {}, new Results((V)true));
var obj_297 = v_258;
V key_298 = v_295;
R.Set(obj_297, key_298, values_296[0]);
var values_299 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_102, v_295)}, new Results((V)"missing enemy definition"))));
V v_300 = values_299[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_300, (V)"id") == v_295), () => R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_300, (V)"ai") == (V)"guard"), () => (V)(R.Get(v_300, (V)"ai") == (V)"spitter")), () => (V)(R.Get(v_300, (V)"ai") == (V)"skitter")), () => (V)(R.Get(v_300, (V)"ai") == (V)"warden")), () => (V)(R.Get(v_300, (V)"ai") == (V)"pursuer")), () => (V)(R.Get(v_300, (V)"ai") == (V)"spore_herald")), () => (V)(R.Get(v_300, (V)"ai") == (V)"canal_lancer")), () => (V)(R.Get(v_300, (V)"ai") == (V)"cinder_bellkeeper")), () => (V)(R.Get(v_300, (V)"ai") == (V)"hollow_regent")), () => (V)(R.Get(v_300, (V)"ai") == (V)"biome_elite")), () => (V)(R.Get(v_300, (V)"ai") == (V)"expansion_regular")), () => (V)(R.Get(v_300, (V)"ai") == (V)"squad")), () => (V)(R.Get(v_300, (V)"ai") == (V)"idea_regular")))}, new Results((V)"invalid enemy identity")));
foreach (var row_301 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"hp"), new Entry((V)2, (V)"damage"), new Entry((V)3, (V)"defense"), new Entry((V)4, (V)"speed"), new Entry((V)5, (V)"windup"), new Entry((V)6, (V)"recovery"), new Entry((V)7, (V)"move_cost"), new Entry((V)8, (V)"range"), new Entry((V)9, (V)"xp_reward"), new Entry((V)10, (V)"currency_drop")))))))) {
V v_302 = row_301[0];
V v_303 = row_301[1];
var values_304 = R.Join(new V[] {}, new Results(R.Get(v_300, v_303)));
V v_305 = values_304[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_305))).First == (V)"number"), () => (V)((V)(v_305 % (V)1.0d) == (V)0.0d)), () => (V)(v_305 >= (V)0.0d))}, new Results((V)"invalid enemy stat")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_300, (V)"hp") > (V)0.0d), () => (V)(R.Get(v_300, (V)"speed") > (V)0.0d)), () => (V)(R.Get(v_300, (V)"windup") > (V)0.0d)), () => (V)(R.Get(v_300, (V)"recovery") > (V)0.0d)), () => (V)(R.Get(v_300, (V)"move_cost") > (V)0.0d)), () => (V)(R.Get(v_300, (V)"range") > (V)0.0d)), () => R.Or(R.Or(R.Get(v_300, (V)"invulnerable"), () => (V)(R.Get(v_300, (V)"ai") == (V)"hollow_regent")), () => R.And((V)(R.Get(v_300, (V)"xp_reward") > (V)0.0d), () => (V)(R.Get(v_300, (V)"currency_drop") > (V)0.0d))))}, new Results((V)"invalid enemy timing")));
if (R.Truth((V)(R.Get(v_300, (V)"ai") == (V)"spitter"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_300, (V)"min_range")))).First == (V)"number"), () => (V)((V)(R.Get(v_300, (V)"min_range") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_300, (V)"min_range") >= (V)2.0d)), () => (V)(R.Get(v_300, (V)"min_range") <= R.Get(v_300, (V)"range")))}, new Results((V)"invalid ranged minimum")));
}
}
foreach (var row_306 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"order"))))))) {
V v_307 = row_306[0];
V v_308 = row_306[1];
var values_309 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_308))));
V v_310 = values_309[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_102, R.Get(v_310, (V)"elite_id")), () => R.Or((V)(R.Get(R.Get(v_102, R.Get(v_310, (V)"elite_id")), (V)"ai") == (V)"warden"), () => (V)(R.Get(R.Get(v_102, R.Get(v_310, (V)"elite_id")), (V)"ai") == (V)"biome_elite")))}, new Results((V)"unknown biome elite")));
foreach (var row_311 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_310, (V)"enemy_ids"))))))) {
V v_312 = row_311[0];
V v_313 = row_311[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Get(v_102, v_313), () => (V)(R.Get(R.Get(v_102, v_313), (V)"ai") != (V)"biome_elite")), () => (V)(R.Get(R.Get(v_102, v_313), (V)"ai") != (V)"warden"))}, new Results((V)"unknown or invalid biome encounter")));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_31, (V)"elite_activation_radius") > (V)0.0d), () => (V)(R.Get(v_31, (V)"grace") > (V)0.0d)), () => (V)(R.Get(v_31, (V)"pressure_first") > (V)0.0d)), () => (V)(R.Get(v_31, (V)"pressure_max_active") > (V)0.0d)), () => (V)((V)R.Length(R.Get(v_31, (V)"pressure_periods")) > (V)0.0d)), () => (V)((V)R.Length(R.Get(v_31, (V)"routes")) >= (V)2.0d))}, new Results((V)"invalid stage rules")));
foreach (var row_314 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_31, (V)"pressure_periods"))))))) {
V v_315 = row_314[0];
V v_316 = row_314[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(v_226, R.Join(new V[] {}, new Results(v_316))).First, () => R.Or((V)(v_315 == (V)1.0d), () => (V)(v_316 <= R.Get(R.Get(v_31, (V)"pressure_periods"), (V)(v_315 - (V)1.0d)))))}, new Results((V)"pressure frequency must rise with stage pressure")));
}
var values_317 = R.Join(new V[] {}, new Results(R.Table()));
V v_318 = values_317[0];
foreach (var row_319 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_31, (V)"routes"))))))) {
V v_320 = row_319[0];
V v_321 = row_319[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_321, (V)"id")))).First == (V)"string"), () => (V)(!R.Truth(R.Get(v_318, R.Get(v_321, (V)"id"))))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_321, (V)"name")))).First == (V)"string")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_321, (V)"description")))).First == (V)"string"))}, new Results((V)"invalid stage route")));
var values_322 = R.Join(new V[] {}, new Results((V)true));
var obj_323 = v_318;
V key_324 = R.Get(v_321, (V)"id");
R.Set(obj_323, key_324, values_322[0]);
}
var values_325 = R.Join(new V[] {}, new Results(R.Get(v_144, (V)"core:map/scrolling_room")));
V v_326 = values_325[0];
var values_327 = R.Join(new V[] {}, new Results(R.Table()));
V v_328 = values_327[0];
foreach (var row_329 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_140)))))) {
V v_330 = row_329[0];
V v_331 = row_329[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_102, R.Get(v_331, (V)"definition_id"))}, new Results((V)"unknown demo enemy")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_326, R.Get(v_331, (V)"x")}, new Results(R.Get(v_331, (V)"y")))).First}, new Results((V)"demo spawn blocked")));
var values_332 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_331, (V)"x"), R.Concat((V)",", R.Get(v_331, (V)"y")))));
V v_333 = values_332[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_328, v_333)))}, new Results((V)"duplicate demo spawn")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_331, (V)"x") != R.Get(v_326, (V)"start_x")), () => (V)(R.Get(v_331, (V)"y") != R.Get(v_326, (V)"start_y")))}, new Results((V)"demo enemy overlaps player")));
var values_334 = R.Join(new V[] {}, new Results((V)true));
var obj_335 = v_328;
V key_336 = v_333;
R.Set(obj_335, key_336, values_334[0]);
}
foreach (var row_337 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_142)))))) {
V v_338 = row_337[0];
V v_339 = row_337[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_29, R.Get(v_339, (V)"definition_id"))}, new Results((V)"unknown demo chest")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_326, R.Get(v_339, (V)"x")}, new Results(R.Get(v_339, (V)"y")))).First}, new Results((V)"demo chest blocked")));
var values_340 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_339, (V)"x"), R.Concat((V)",", R.Get(v_339, (V)"y")))));
V v_341 = values_340[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_328, v_341)))}, new Results((V)"demo chest overlaps enemy")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_339, (V)"x") != R.Get(v_326, (V)"start_x")), () => (V)(R.Get(v_339, (V)"y") != R.Get(v_326, (V)"start_y")))}, new Results((V)"demo chest overlaps player")));
var values_342 = R.Join(new V[] {}, new Results((V)true));
var obj_343 = v_328;
V key_344 = v_341;
R.Set(obj_343, key_344, values_342[0]);
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
