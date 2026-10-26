// Static C# port of src/idea_enemies.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: e5f91b0e14225a63a806b59d6e16f15d713fdcbd6c534fc09c8cf1ac85eb236d
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_enemies { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_webs"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_oaths"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_sentinel"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_mirrors"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_silt"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_embers"))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"version", (V)"roag-idea-enemies/1"))));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"heavy_rammer"), new Entry((V)2, (V)"Heavy Rammer"), new Entry((V)3, (V)12.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)1.0d), new Entry((V)6, (V)70.0d), new Entry((V)7, (V)110.0d), new Entry((V)8, (V)140.0d), new Entry((V)9, (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)"hollow_wraith"), new Entry((V)2, (V)"Hollow Wraith"), new Entry((V)3, (V)7.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)90.0d), new Entry((V)7, (V)100.0d), new Entry((V)8, (V)120.0d), new Entry((V)9, (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)"grave_cantor"), new Entry((V)2, (V)"Grave Cantor"), new Entry((V)3, (V)7.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)80.0d), new Entry((V)7, (V)150.0d), new Entry((V)8, (V)160.0d), new Entry((V)9, (V)4.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)"gulp_toad"), new Entry((V)2, (V)"Gulp Toad"), new Entry((V)3, (V)14.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)1.0d), new Entry((V)6, (V)80.0d), new Entry((V)7, (V)100.0d), new Entry((V)8, (V)140.0d), new Entry((V)9, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)"haunted_cache"), new Entry((V)2, (V)"Haunted Cache"), new Entry((V)3, (V)8.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)75.0d), new Entry((V)7, (V)140.0d), new Entry((V)8, (V)140.0d), new Entry((V)9, (V)1.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)"bone_splitter"), new Entry((V)2, (V)"Bone Splitter"), new Entry((V)3, (V)8.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)80.0d), new Entry((V)7, (V)100.0d), new Entry((V)8, (V)130.0d), new Entry((V)9, (V)1.0d))), new Entry((V)7, R.Table(new Entry((V)1, (V)"bone_fragment"), new Entry((V)2, (V)"Bone Fragment"), new Entry((V)3, (V)2.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)95.0d), new Entry((V)7, (V)100.0d), new Entry((V)8, (V)110.0d), new Entry((V)9, (V)1.0d))), new Entry((V)8, R.Table(new Entry((V)1, (V)"alarm_keeper"), new Entry((V)2, (V)"Alarm Keeper"), new Entry((V)3, (V)6.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)75.0d), new Entry((V)7, (V)220.0d), new Entry((V)8, (V)150.0d), new Entry((V)9, (V)1.0d))), new Entry((V)9, R.Table(new Entry((V)1, (V)"web_weaver"), new Entry((V)2, (V)"Web Weaver"), new Entry((V)3, (V)7.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)90.0d), new Entry((V)7, (V)140.0d), new Entry((V)8, (V)140.0d), new Entry((V)9, (V)4.0d))), new Entry((V)10, R.Table(new Entry((V)1, (V)"oath_twin_a"), new Entry((V)2, (V)"Oath Twin \u00e2\u0080\u0094 Pike"), new Entry((V)3, (V)8.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)90.0d), new Entry((V)7, (V)100.0d), new Entry((V)8, (V)130.0d), new Entry((V)9, (V)1.0d))), new Entry((V)11, R.Table(new Entry((V)1, (V)"oath_twin_b"), new Entry((V)2, (V)"Oath Twin \u00e2\u0080\u0094 Blade"), new Entry((V)3, (V)8.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)100.0d), new Entry((V)7, (V)110.0d), new Entry((V)8, (V)130.0d), new Entry((V)9, (V)1.0d))), new Entry((V)12, R.Table(new Entry((V)1, (V)"rotating_sentinel"), new Entry((V)2, (V)"Rotating Sentinel"), new Entry((V)3, (V)14.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)1.0d), new Entry((V)6, (V)80.0d), new Entry((V)7, (V)160.0d), new Entry((V)8, (V)80.0d), new Entry((V)9, (V)1.0d))), new Entry((V)13, R.Table(new Entry((V)1, (V)"mirror_hexer"), new Entry((V)2, (V)"Mirror Hexer"), new Entry((V)3, (V)7.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)85.0d), new Entry((V)7, (V)150.0d), new Entry((V)8, (V)140.0d), new Entry((V)9, (V)4.0d))), new Entry((V)14, R.Table(new Entry((V)1, (V)"silt_worm"), new Entry((V)2, (V)"Silt Worm"), new Entry((V)3, (V)10.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)85.0d), new Entry((V)7, (V)140.0d), new Entry((V)8, (V)140.0d), new Entry((V)9, (V)3.0d))), new Entry((V)15, R.Table(new Entry((V)1, (V)"ember_moth"), new Entry((V)2, (V)"Ember Moth"), new Entry((V)3, (V)6.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)100.0d), new Entry((V)7, (V)180.0d), new Entry((V)8, (V)150.0d), new Entry((V)9, (V)3.0d))))));
var obj_20 = v_18;
V key_21 = (V)"enemies";
R.Set(obj_20, key_21, values_19[0]);
var values_22 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(v_10, (V)"id"), (V)true))));
var obj_23 = v_18;
V key_24 = (V)"champions";
R.Set(obj_23, key_24, values_22[0]);
var values_25 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"bone_fragment", (V)true), new Entry((V)"oath_twin_b", (V)true))));
var obj_26 = v_18;
V key_27 = (V)"secondary";
R.Set(obj_26, key_27, values_25[0]);
var values_28 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:enemy/heavy_rammer", (V)"rammer"), new Entry((V)"core:enemy/hollow_wraith", (V)"wraith"), new Entry((V)"core:enemy/grave_cantor", (V)"reviver"), new Entry((V)"core:enemy/gulp_toad", (V)"toad"), new Entry((V)"core:enemy/haunted_cache", (V)"cache"), new Entry((V)"core:enemy/bone_splitter", (V)"bone"), new Entry((V)"core:enemy/bone_fragment", (V)"fragment"), new Entry((V)"core:enemy/alarm_keeper", (V)"alarm"), new Entry((V)"core:enemy/web_weaver", (V)"web"), new Entry((V)"core:enemy/oath_twin_a", (V)"oath"), new Entry((V)"core:enemy/oath_twin_b", (V)"oath"), new Entry(R.Get(v_10, (V)"id"), (V)"sentinel"), new Entry(R.Get(v_12, (V)"id"), (V)"mirror"), new Entry(R.Get(v_14, (V)"id"), (V)"silt"), new Entry(R.Get(v_16, (V)"id"), (V)"moth"))));
var obj_29 = v_18;
V key_30 = (V)"roles";
R.Set(obj_29, key_30, values_28[0]);
var values_31 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:enemy/heavy_rammer", (V)89.0d), new Entry((V)"core:enemy/grave_cantor", (V)88.0d), new Entry((V)"core:enemy/gulp_toad", (V)123.0d), new Entry((V)"core:enemy/haunted_cache", (V)92.0d), new Entry((V)"core:object/idea_cache_dormant", (V)90.0d), new Entry((V)"core:enemy/web_weaver", (V)122.0d), new Entry((V)"core:enemy/oath_twin_a", (V)99.0d), new Entry((V)"core:enemy/oath_twin_b", (V)100.0d))));
var obj_32 = v_18;
V key_33 = (V)"tiny";
R.Set(obj_32, key_33, values_31[0]);
var values_34 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:enemy/hollow_wraith", (V)416.0d), new Entry((V)"core:enemy/bone_splitter", (V)323.0d), new Entry((V)"core:enemy/bone_fragment", (V)323.0d), new Entry((V)"core:enemy/alarm_keeper", (V)173.0d), new Entry(R.Get(v_10, (V)"id"), (V)224.0d), new Entry(R.Get(v_12, (V)"id"), (V)320.0d), new Entry(R.Get(v_14, (V)"id"), (V)371.0d), new Entry(R.Get(v_16, (V)"id"), (V)411.0d))));
var obj_35 = v_18;
V key_36 = (V)"shared_onebit";
R.Set(obj_35, key_36, values_34[0]);
var values_37 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:enemy/heavy_rammer", (V)129.0d), new Entry((V)"core:enemy/hollow_wraith", (V)416.0d), new Entry((V)"core:enemy/grave_cantor", (V)318.0d), new Entry((V)"core:enemy/gulp_toad", (V)422.0d), new Entry((V)"core:enemy/haunted_cache", (V)417.0d), new Entry((V)"core:enemy/bone_splitter", (V)323.0d), new Entry((V)"core:enemy/bone_fragment", (V)323.0d), new Entry((V)"core:enemy/alarm_keeper", (V)173.0d), new Entry((V)"core:enemy/web_weaver", (V)366.0d), new Entry((V)"core:enemy/oath_twin_a", (V)124.0d), new Entry((V)"core:enemy/oath_twin_b", (V)127.0d), new Entry(R.Get(v_10, (V)"id"), (V)224.0d), new Entry(R.Get(v_12, (V)"id"), (V)320.0d), new Entry(R.Get(v_14, (V)"id"), (V)371.0d), new Entry(R.Get(v_16, (V)"id"), (V)411.0d))));
var obj_38 = v_18;
V key_39 = (V)"onebit";
R.Set(obj_38, key_39, values_37[0]);
var values_40 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:object/idea_cache_dormant", (V)303.0d), new Entry((V)"core:object/idea_silt_trail", (V)268.0d))));
var obj_41 = v_18;
V key_42 = (V)"objects";
R.Set(obj_41, key_42, values_40[0]);
var values_43 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:object/idea_silt_trail", (V)64.0d))));
var obj_44 = v_18;
V key_45 = (V)"caves_objects";
R.Set(obj_44, key_45, values_43[0]);
var values_46 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:ui/idea_corpse_effect", (V)539.0d), new Entry((V)"core:ui/idea_alarm_effect", (V)915.0d), new Entry((V)"core:ui/idea_oath_effect", (V)574.0d), new Entry((V)"core:ui/idea_mirror_effect", (V)493.0d), new Entry((V)"core:ui/idea_reflect_effect", (V)494.0d))));
var obj_47 = v_18;
V key_48 = (V)"symbols";
R.Set(obj_47, key_48, values_46[0]);
var values_49 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rammer_shove", (V)"rammer shove"), new Entry((V)"wraith_phase", (V)"wraith phase"), new Entry((V)"wraith_touch", (V)"wraith touch"), new Entry((V)"cantor_revive", (V)"cantor revival"), new Entry((V)"cantor_touch", (V)"cantor touch"), new Entry((V)"toad_gulp", (V)"swallow loose drop"), new Entry((V)"toad_bite", (V)"toad bite"), new Entry((V)"cache_bite", (V)"haunted cache bite"), new Entry((V)"bone_strike", (V)"bone strike"), new Entry((V)"fragment_stab", (V)"fragment stab"), new Entry((V)"keeper_alarm", (V)"reinforcement alarm"), new Entry((V)"keeper_touch", (V)"keeper touch"), new Entry((V)"weaver_web", (V)"anchored web"), new Entry((V)"weaver_bite", (V)"weaver bite"), new Entry((V)"oath_pike", (V)"oath pike"), new Entry((V)"oath_blade", (V)"oath blade"), new Entry((V)"oath_share", (V)"shared oath loss"), new Entry((V)"sentinel_quarter", (V)"sentinel quarter turn"), new Entry((V)"sentinel_touch", (V)"sentinel strike"), new Entry((V)"hexer_mirror", (V)"directional reflector"), new Entry((V)"hexer_bolt", (V)"hexer bolt"), new Entry((V)"worm_charge", (V)"silt charge"), new Entry((V)"worm_touch", (V)"worm bite"), new Entry((V)"moth_dive", (V)"ember landing"), new Entry((V)"moth_touch", (V)"moth touch"), new Entry((V)"moth_ember", (V)"ember pulse"))));
var obj_50 = v_18;
V key_51 = (V)"abilities";
R.Set(obj_50, key_51, values_49[0]);
var values_52 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_53 = values_52[0];
var values_54 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"guard", (V)true), new Entry((V)"spitter", (V)true), new Entry((V)"skitter", (V)true), new Entry((V)"spore_herald", (V)true), new Entry((V)"canal_lancer", (V)true), new Entry((V)"cinder_bellkeeper", (V)true), new Entry((V)"expansion_regular", (V)true))));
V v_55 = values_54[0];
R.Set(v_18, (V)"revivable", new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
return R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.Get(v_55, R.Get(v_57, (V)"ai_id")), () => (V)(R.Get(v_57, (V)"definition_id") == (V)"core:enemy/heavy_rammer")), () => (V)(R.Get(v_57, (V)"definition_id") == (V)"core:enemy/hollow_wraith")), () => (V)(R.Get(v_57, (V)"definition_id") == (V)"core:enemy/gulp_toad"))));
return Results.Empty;
})));
R.Set(v_18, (V)"drop", new V((RuleFunction)(args_58 => {
V v_59 = args_58[0];
V v_60 = args_58[1];
foreach (var row_61 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_59, (V)"drops"))))))) {
V v_62 = row_61[0];
V v_63 = row_61[1];
if (R.Truth((V)(R.Get(v_63, (V)"id") == v_60))) {
return R.Join(new V[] {}, new Results(v_63));
}
}
return Results.Empty;
})));
R.Set(v_18, (V)"release_drop", new V((RuleFunction)(args_64 => {
V v_65 = args_64[0];
V v_66 = args_64[1];
V v_67 = args_64[2];
if (R.Truth((V)(!R.Truth(R.Get(v_66, (V)"carried_drop_id"))))) {
return Results.Empty;
}
var values_68 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_18, (V)"drop"), R.Join(new V[] {v_65}, new Results(R.Get(v_66, (V)"carried_drop_id")))).First}, new Results((V)"missing carried drop"))));
V v_69 = values_68[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_69, (V)"carried_by") == R.Get(v_66, (V)"id")), () => (V)(!R.Truth(R.Get(v_69, (V)"collected"))))}, new Results((V)"invalid carried drop release")));
var values_70 = R.Join(new V[] {}, new Results(V.Nil));
var obj_71 = v_69;
V key_72 = (V)"carried_by";
R.Set(obj_71, key_72, values_70[0]);

var values_73 = R.Join(new V[] {}, new Results(V.Nil));
var obj_74 = v_66;
V key_75 = (V)"carried_drop_id";
R.Set(obj_74, key_75, values_73[0]);
var values_76 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_66, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_66, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_66, (V)"position"), (V)"y")))));
var obj_77 = v_69;
V key_78 = (V)"position";
R.Set(obj_77, key_78, values_76[0]);
R.Call(v_67, R.Join(new V[] {(V)"DropRegurgitated"}, new Results(R.Table(new Entry((V)"drop_id", R.Get(v_69, (V)"id")), new Entry((V)"drop_kind", R.Get(v_69, (V)"kind")), new Entry((V)"amount", R.Get(v_69, (V)"amount")), new Entry((V)"floor_id", R.Get(R.Get(v_69, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_69, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_69, (V)"position"), (V)"y"))))));
return Results.Empty;
})));
R.Set(v_18, (V)"split", new V((RuleFunction)(args_79 => {
V v_80 = args_79[0];
V v_81 = args_79[1];
V v_82 = args_79[2];
V v_83 = args_79[3];
V v_84 = args_79[4];
V v_85 = args_79[5];
if (R.Truth(R.Or(R.Or(R.Or((V)(R.Get(v_81, (V)"target_definition_id") != (V)"core:enemy/bone_splitter"), () => R.Get(v_81, (V)"reward_exempt")), () => R.Get(v_80, (V)"game_over")), () => (V)(R.Get(v_81, (V)"floor_id") != R.Get(v_80, (V)"active_floor_id"))))) {
return Results.Empty;
}
var values_86 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_87 = values_86[0];
var values_88 = R.Join(new V[] {}, new Results((V)0.0d));
V v_89 = values_88[0];
foreach (var row_90 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_87)))))) {
V v_91 = row_90[0];
V v_92 = row_90[1];
var values_93 = R.Join(new V[] {(V)(R.Get(v_81, (V)"x") + R.Get(v_92, (V)1.0d))}, new Results((V)(R.Get(v_81, (V)"y") + R.Get(v_92, (V)2.0d))));
V v_94 = values_93[0];
V v_95 = values_93[1];
var values_96 = R.Join(new V[] {}, new Results((V)false));
V v_97 = values_96[0];
foreach (var row_98 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_82, (V)"placements"), () => R.Get(R.Get(v_82, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_99 = row_98[0];
V v_100 = row_98[1];
if (R.Truth(R.And((V)(R.Get(v_100, (V)"x") == v_94), () => (V)(R.Get(v_100, (V)"y") == v_95)))) {
var values_101 = R.Join(new V[] {}, new Results((V)true));
v_97 = values_101[0];
}
}
if (R.Truth(R.And(R.And(R.Get(v_82, (V)"elite"), () => (V)(R.Get(R.Get(v_82, (V)"elite"), (V)"x") == v_94)), () => (V)(R.Get(R.Get(v_82, (V)"elite"), (V)"y") == v_95)))) {
var values_102 = R.Join(new V[] {}, new Results((V)true));
v_97 = values_102[0];
}
foreach (var row_103 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_80, (V)"entities"))))))) {
V v_104 = row_103[0];
V v_105 = row_103[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_105, (V)"controller") == (V)"exit"), () => (V)(R.Get(R.Get(v_105, (V)"position"), (V)"floor_id") == R.Get(v_81, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_105, (V)"position"), (V)"x") == v_94)), () => (V)(R.Get(R.Get(v_105, (V)"position"), (V)"y") == v_95)))) {
var values_106 = R.Join(new V[] {}, new Results((V)true));
v_97 = values_106[0];
}
}
if (R.Truth(R.And((V)(!R.Truth(v_97)), () => R.Call(v_83, R.Join(new V[] {v_94}, new Results(v_95))).First))) {
var values_107 = R.Join(new V[] {}, R.Call(v_84, R.Join(new V[] {v_80, (V)"core:enemy/bone_fragment", v_94, v_95}, new Results((V)(R.Get(v_80, (V)"clock") + (V)1.0d)))));
V v_108 = values_107[0];
var values_109 = R.Join(new V[] {}, new Results((V)true));
var obj_110 = v_108;
V key_111 = (V)"reward_exempt";
R.Set(obj_110, key_111, values_109[0]);

var values_112 = R.Join(new V[] {}, new Results(R.Get(v_81, (V)"target_id")));
var obj_113 = v_108;
V key_114 = (V)"fragment_of";
R.Set(obj_113, key_114, values_112[0]);
var values_115 = R.Join(new V[] {}, new Results((V)(v_89 + (V)1.0d)));
v_89 = values_115[0];
R.Call(v_85, R.Join(new V[] {(V)"BoneFragmented"}, new Results(R.Table(new Entry((V)"target_id", R.Get(v_108, (V)"id")), new Entry((V)"original_id", R.Get(v_81, (V)"target_id")), new Entry((V)"floor_id", R.Get(v_81, (V)"floor_id")), new Entry((V)"x", v_94), new Entry((V)"y", v_95)))));
if (R.Truth((V)(v_89 == (V)2.0d))) {
break;
}
}
}
return Results.Empty;
})));
R.Set(v_18, (V)"prune_corpses", new V((RuleFunction)(args_116 => {
V v_117 = args_116[0];
{ double end_120 = R.Num((V)1.0d), step_121 = R.Num((-(V)1.0d));
for (double i_119 = R.Num((V)R.Length(R.Or(R.Get(v_117, (V)"idea_corpses"), () => R.Table()))); step_121 > 0 ? i_119 <= end_120 : i_119 >= end_120; i_119 += step_121) {
V v_118 = i_119;
var values_122 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_117, (V)"idea_corpses"), v_118)));
V v_123 = values_122[0];
var values_124 = R.Join(new V[] {}, new Results((V)false));
V v_125 = values_124[0];
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_117, (V)"entities"))))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
var values_129 = R.Join(new V[] {}, new Results(R.Get(v_128, (V)"pending_attack")));
V v_130 = values_129[0];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_128, (V)"definition_id") == (V)"core:enemy/grave_cantor"), () => (V)(R.Get(v_128, (V)"hp") > (V)0.0d)), () => (V)(R.Get(R.Get(v_128, (V)"position"), (V)"floor_id") == R.Get(v_123, (V)"floor_id"))), () => R.Or((V)(!R.Truth(R.Get(R.Get(v_128, (V)"ai_memory"), (V)"revival_spent"))), () => R.And(R.And(R.And(v_130, () => R.Get(v_130, (V)"support")), () => (V)(R.Get(R.Get(v_130, (V)"support"), (V)"kind") == (V)"revive")), () => (V)(R.Get(R.Get(v_130, (V)"support"), (V)"corpse_id") == R.Get(v_123, (V)"id"))))))) {
var values_131 = R.Join(new V[] {}, new Results((V)true));
v_125 = values_131[0];

break;
}
}
if (R.Truth((V)(!R.Truth(v_125)))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_117, (V)"idea_corpses")}, new Results(v_118)));
}
}
}
return Results.Empty;
})));
R.Set(v_18, (V)"remember_corpse", new V((RuleFunction)(args_132 => {
V v_133 = args_132[0];
V v_134 = args_132[1];
if (R.Truth(R.Or(R.Or((V)(R.Get(v_134, (V)"controller") != (V)"ai"), () => R.Get(v_134, (V)"reward_exempt")), () => (V)(!R.Truth(R.Call(R.Get(v_18, (V)"revivable"), R.Join(new V[] {}, new Results(v_134))).First))))) {
return Results.Empty;
}
var values_135 = R.Join(new V[] {}, new Results((V)false));
V v_136 = values_135[0];
foreach (var row_137 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_133, (V)"entities"))))))) {
V v_138 = row_137[0];
V v_139 = row_137[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_139, (V)"definition_id") == (V)"core:enemy/grave_cantor"), () => (V)(R.Get(v_139, (V)"hp") > (V)0.0d)), () => (V)(!R.Truth(R.Get(R.Get(v_139, (V)"ai_memory"), (V)"revival_spent")))), () => (V)(R.Get(R.Get(v_139, (V)"position"), (V)"floor_id") == R.Get(R.Get(v_134, (V)"position"), (V)"floor_id"))))) {
var values_140 = R.Join(new V[] {}, new Results((V)true));
v_136 = values_140[0];

break;
}
}
if (R.Truth((V)(!R.Truth(v_136)))) {
return Results.Empty;
}
var values_141 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_133, (V)"idea_corpses"), () => R.Table())));
var obj_142 = v_133;
V key_143 = (V)"idea_corpses";
R.Set(obj_142, key_143, values_141[0]);
if (R.Truth((V)((V)R.Length(R.Get(v_133, (V)"idea_corpses")) >= (V)32.0d))) {
return Results.Empty;
}
var values_144 = R.Join(new V[] {}, new Results((V)0.0d));
V v_145 = values_144[0];
foreach (var row_146 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_133, (V)"idea_corpses"))))))) {
V v_147 = row_146[0];
V v_148 = row_146[1];
if (R.Truth((V)(R.Get(v_148, (V)"floor_id") == R.Get(R.Get(v_134, (V)"position"), (V)"floor_id")))) {
var values_149 = R.Join(new V[] {}, new Results((V)(v_145 + (V)1.0d)));
v_145 = values_149[0];
}
}
if (R.Truth((V)(v_145 >= (V)16.0d))) {
return Results.Empty;
}
var values_150 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_134, (V)"id")), new Entry((V)"definition_id", R.Get(v_134, (V)"definition_id")), new Entry((V)"floor_id", R.Get(R.Get(v_134, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_134, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_134, (V)"position"), (V)"y")), new Entry((V)"max_hp", R.Get(v_134, (V)"max_hp")))));
var obj_151 = R.Get(v_133, (V)"idea_corpses");
V key_152 = (V)((V)R.Length(R.Get(v_133, (V)"idea_corpses")) + (V)1.0d);
R.Set(obj_151, key_152, values_150[0]);
return Results.Empty;
})));
R.Set(v_18, (V)"corpse", new V((RuleFunction)(args_153 => {
V v_154 = args_153[0];
V v_155 = args_153[1];
foreach (var row_156 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_154, (V)"idea_corpses"), () => R.Table()))))))) {
V v_157 = row_156[0];
V v_158 = row_156[1];
if (R.Truth((V)(R.Get(v_158, (V)"id") == v_155))) {
return R.Join(new V[] {}, new Results(v_158));
}
}
return Results.Empty;
})));
R.Set(v_18, (V)"commit", new V((RuleFunction)(args_159 => {
V v_160 = args_159[0];
V v_161 = args_159[1];
V v_162 = args_159[2];
R.Call(R.Get(v_10, (V)"commit"), R.Join(new V[] {v_161}, new Results(v_162)));
if (R.Truth((V)(R.Get(v_162, (V)"ability_id") == (V)"core:ability/moth_dive"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Or(R.Get(R.Get(v_161, (V)"ai_memory"), (V)"ember_dives"), () => (V)0.0d) < (V)2.0d)}, new Results((V)"spent ember dive budget")));
var values_163 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(R.Get(v_161, (V)"ai_memory"), (V)"ember_dives"), () => (V)0.0d) + (V)1.0d)));
var obj_164 = R.Get(v_161, (V)"ai_memory");
V key_165 = (V)"ember_dives";
R.Set(obj_164, key_165, values_163[0]);
}
if (R.Truth((V)(R.Get(v_162, (V)"ability_id") == (V)"core:ability/worm_charge"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Or(R.Get(R.Get(v_161, (V)"ai_memory"), (V)"silt_charges"), () => (V)0.0d) < (V)2.0d)}, new Results((V)"spent silt charge budget")));
var values_166 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(R.Get(v_161, (V)"ai_memory"), (V)"silt_charges"), () => (V)0.0d) + (V)1.0d)));
var obj_167 = R.Get(v_161, (V)"ai_memory");
V key_168 = (V)"silt_charges";
R.Set(obj_167, key_168, values_166[0]);
}
if (R.Truth(R.And(R.Get(v_162, (V)"support"), () => (V)(R.Get(R.Get(v_162, (V)"support"), (V)"kind") == (V)"mirror")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Or(R.Get(R.Get(v_161, (V)"ai_memory"), (V)"mirror_casts"), () => (V)0.0d) < (V)2.0d)}, new Results((V)"spent mirror budget")));
var values_169 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(R.Get(v_161, (V)"ai_memory"), (V)"mirror_casts"), () => (V)0.0d) + (V)1.0d)));
var obj_170 = R.Get(v_161, (V)"ai_memory");
V key_171 = (V)"mirror_casts";
R.Set(obj_170, key_171, values_169[0]);
}
if (R.Truth(R.And(R.Get(v_162, (V)"support"), () => (V)(R.Get(R.Get(v_162, (V)"support"), (V)"kind") == (V)"web")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Or(R.Get(R.Get(v_161, (V)"ai_memory"), (V)"web_casts"), () => (V)0.0d) < (V)2.0d)}, new Results((V)"spent web budget")));
var values_172 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(R.Get(v_161, (V)"ai_memory"), (V)"web_casts"), () => (V)0.0d) + (V)1.0d)));
var obj_173 = R.Get(v_161, (V)"ai_memory");
V key_174 = (V)"web_casts";
R.Set(obj_173, key_174, values_172[0]);
}
if (R.Truth(R.And(R.Get(v_162, (V)"support"), () => (V)(R.Get(R.Get(v_162, (V)"support"), (V)"kind") == (V)"alarm")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(R.Get(v_161, (V)"ai_memory"), (V)"alarm_spent")))}, new Results((V)"spent alarm")));
var values_175 = R.Join(new V[] {}, new Results((V)true));
var obj_176 = R.Get(v_161, (V)"ai_memory");
V key_177 = (V)"alarm_spent";
R.Set(obj_176, key_177, values_175[0]);
}
if (R.Truth(R.And(R.Get(v_162, (V)"support"), () => (V)(R.Get(R.Get(v_162, (V)"support"), (V)"kind") == (V)"revive")))) {
var values_178 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_18, (V)"corpse"), R.Join(new V[] {v_160}, new Results(R.Get(R.Get(v_162, (V)"support"), (V)"corpse_id")))).First}, new Results((V)"missing committed corpse"))));
V v_179 = values_178[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(!R.Truth(R.Get(v_179, (V)"claimed_by"))), () => (V)(!R.Truth(R.Get(R.Get(v_161, (V)"ai_memory"), (V)"revival_spent"))))}, new Results((V)"spent revival")));
var values_180 = R.Join(new V[] {}, new Results(R.Get(v_161, (V)"id")));
var obj_181 = v_179;
V key_182 = (V)"claimed_by";
R.Set(obj_181, key_182, values_180[0]);

var values_183 = R.Join(new V[] {}, new Results((V)true));
var obj_184 = R.Get(v_161, (V)"ai_memory");
V key_185 = (V)"revival_spent";
R.Set(obj_184, key_185, values_183[0]);
}
return Results.Empty;
})));
R.Set(v_18, (V)"alarm_landing", new V((RuleFunction)(args_186 => {
V v_187 = args_186[0];
V v_188 = args_186[1];
V v_189 = args_186[2];
V v_190 = args_186[3];
V v_191 = args_186[4];
V v_192 = args_186[5];
var values_193 = R.Join(new V[] {}, new Results(R.Get(v_188, (V)"position")));
V v_194 = values_193[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_189, (V)"floor_id") != R.Get(v_194, (V)"floor_id")), () => (V)(R.Get(v_194, (V)"floor_id") != R.Get(v_187, (V)"active_floor_id"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_189, (V)"x") - R.Get(v_194, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_189, (V)"y") - R.Get(v_194, (V)"y"))))).First) != (V)1.0d)), () => (V)(!R.Truth(R.Get(R.Get(v_190, (V)"rows"), R.Get(v_189, (V)"y"))))), () => (V)(R.Invoke(R.Get(R.Get(v_190, (V)"rows"), R.Get(v_189, (V)"y")), "sub", R.Join(new V[] {R.Get(v_189, (V)"x")}, new Results(R.Get(v_189, (V)"x")))).First != (V)".")), () => (V)(!R.Truth(R.Call(v_191, R.Join(new V[] {v_187, R.Get(v_188, (V)"id"), R.Get(v_189, (V)"x")}, new Results(R.Get(v_189, (V)"y")))).First))), () => (V)(!R.Truth(R.Call(v_192, R.Join(new V[] {R.Get(v_189, (V)"x")}, new Results(R.Get(v_189, (V)"y")))).First))))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_195 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_190, (V)"placements"), () => R.Get(R.Get(v_190, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_196 = row_195[0];
V v_197 = row_195[1];
if (R.Truth(R.And((V)(R.Get(v_197, (V)"x") == R.Get(v_189, (V)"x")), () => (V)(R.Get(v_197, (V)"y") == R.Get(v_189, (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
foreach (var row_198 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_187, (V)"entities"))))))) {
V v_199 = row_198[0];
V v_200 = row_198[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_200, (V)"controller") == (V)"exit"), () => (V)(R.Get(R.Get(v_200, (V)"position"), (V)"floor_id") == R.Get(v_194, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_200, (V)"position"), (V)"x") == R.Get(v_189, (V)"x"))), () => (V)(R.Get(R.Get(v_200, (V)"position"), (V)"y") == R.Get(v_189, (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_18, (V)"phase_geometry", new V((RuleFunction)(args_201 => {
V v_202 = args_201[0];
V v_203 = args_201[1];
var values_204 = R.Join(new V[] {(V)(R.Get(v_203, (V)"x") - R.Get(v_202, (V)"x"))}, new Results((V)(R.Get(v_203, (V)"y") - R.Get(v_202, (V)"y"))));
V v_205 = values_204[0];
V v_206 = values_204[1];
return R.Join(new V[] {}, new Results(R.And(R.Or((V)(v_205 == (V)0.0d), () => (V)(v_206 == (V)0.0d)), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_205))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_206))).First) == (V)2.0d))));
return Results.Empty;
})));
R.Set(v_18, (V)"phase_landing", new V((RuleFunction)(args_207 => {
V v_208 = args_207[0];
V v_209 = args_207[1];
V v_210 = args_207[2];
V v_211 = args_207[3];
V v_212 = args_207[4];
V v_213 = args_207[5];
if (R.Truth(R.Or(R.Or((V)(R.Get(v_210, (V)"floor_id") != R.Get(R.Get(v_209, (V)"position"), (V)"floor_id")), () => (V)(R.Get(v_210, (V)"floor_id") != R.Get(v_208, (V)"active_floor_id"))), () => (V)(!R.Truth(R.Call(R.Get(v_18, (V)"phase_geometry"), R.Join(new V[] {R.Get(v_209, (V)"position")}, new Results(v_210))).First))))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth(R.Or((V)(!R.Truth(R.Call(v_212, R.Join(new V[] {v_208, R.Get(v_209, (V)"id"), R.Get(v_210, (V)"x")}, new Results(R.Get(v_210, (V)"y")))).First)), () => (V)(!R.Truth(R.Call(v_213, R.Join(new V[] {R.Get(v_210, (V)"x")}, new Results(R.Get(v_210, (V)"y")))).First))))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_214 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_211, (V)"placements"), () => R.Get(R.Get(v_211, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_215 = row_214[0];
V v_216 = row_214[1];
if (R.Truth(R.And((V)(R.Get(v_216, (V)"x") == R.Get(v_210, (V)"x")), () => (V)(R.Get(v_216, (V)"y") == R.Get(v_210, (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
foreach (var row_217 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_208, (V)"entities"))))))) {
V v_218 = row_217[0];
V v_219 = row_217[1];
var values_220 = R.Join(new V[] {}, new Results(R.Get(v_219, (V)"position")));
V v_221 = values_220[0];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_219, (V)"controller") == (V)"exit"), () => (V)(R.Get(v_221, (V)"floor_id") == R.Get(v_210, (V)"floor_id"))), () => (V)(R.Get(v_221, (V)"x") == R.Get(v_210, (V)"x"))), () => (V)(R.Get(v_221, (V)"y") == R.Get(v_210, (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_18, (V)"choose", new V((RuleFunction)(args_222 => {
V v_223 = args_222[0];
V v_224 = args_222[1];
V v_225 = args_222[2];
V v_226 = args_222[3];
var values_227 = R.Join(new V[] {R.Get(v_224, (V)"position")}, new Results(R.Get(v_225, (V)"position")));
V v_228 = values_227[0];
V v_229 = values_227[1];
var values_230 = R.Join(new V[] {(V)(R.Get(v_229, (V)"x") - R.Get(v_228, (V)"x"))}, new Results((V)(R.Get(v_229, (V)"y") - R.Get(v_228, (V)"y"))));
V v_231 = values_230[0];
V v_232 = values_230[1];
var values_233 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_18, (V)"roles"), R.Get(v_224, (V)"definition_id"))));
V v_234 = values_233[0];
if (R.Truth((V)(v_234 == (V)"moth"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_16, (V)"choose"), R.Join(new V[] {v_223, v_224, v_225}, new Results(v_226))));
}
if (R.Truth((V)(v_234 == (V)"silt"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"choose"), R.Join(new V[] {v_223, v_224, v_225}, new Results(v_226))));
}
if (R.Truth((V)(v_234 == (V)"mirror"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"choose"), R.Join(new V[] {v_223, v_224, v_225}, new Results(v_226))));
}
if (R.Truth((V)(v_234 == (V)"sentinel"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"choose"), R.Join(new V[] {v_223, v_224, v_225}, new Results(v_226))));
}
if (R.Truth((V)(v_234 == (V)"oath"))) {
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_231))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_232))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", R.Or(R.And((V)(R.Get(v_224, (V)"definition_id") == R.Get(v_8, (V)"a")), () => (V)"core:ability/oath_pike"), () => (V)"core:ability/oath_blade")), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_229, (V)"x")), new Entry((V)"y", R.Get(v_229, (V)"y")))))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_226, (V)"next_step"), R.Join(new V[] {v_223, v_224}, R.Call(R.Get(v_226, (V)"adjacent_goals"), R.Join(new V[] {v_223, v_224, v_225}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(v_234 == (V)"web"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"choose"), R.Join(new V[] {v_223, v_224, v_225}, new Results(v_226))));
}
if (R.Truth((V)(v_234 == (V)"alarm"))) {
if (R.Truth(R.And((V)(!R.Truth(R.Get(R.Get(v_224, (V)"ai_memory"), (V)"alarm_spent"))), () => R.Call(R.Get(v_226, (V)"visible"), R.Join(new V[] {R.Get(v_228, (V)"x")}, new Results(R.Get(v_228, (V)"y")))).First))) {
var values_235 = R.Join(new V[] {}, new Results(R.Table()));
V v_236 = values_235[0];
foreach (var row_237 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_53)))))) {
V v_238 = row_237[0];
V v_239 = row_237[1];
var values_240 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_228, (V)"floor_id")), new Entry((V)"x", (V)(R.Get(v_228, (V)"x") + R.Get(v_239, (V)1.0d))), new Entry((V)"y", (V)(R.Get(v_228, (V)"y") + R.Get(v_239, (V)2.0d))))));
V v_241 = values_240[0];
if (R.Truth(R.Call(R.Get(v_18, (V)"alarm_landing"), R.Join(new V[] {v_223, v_224, v_241, R.Get(v_226, (V)"map"), R.Get(v_226, (V)"free")}, new Results(R.Get(v_226, (V)"visible")))).First)) {
var values_242 = R.Join(new V[] {}, new Results(v_241));
var obj_243 = v_236;
V key_244 = (V)((V)R.Length(v_236) + (V)1.0d);
R.Set(obj_243, key_244, values_242[0]);

if (R.Truth((V)((V)R.Length(v_236) == (V)2.0d))) {
break;
}
}
}
if (R.Truth((V)((V)R.Length(v_236) > (V)0.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/keeper_alarm"), new Entry((V)"cells", v_236), new Entry((V)"support", R.Table(new Entry((V)"kind", (V)"alarm"))))));
}
}
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_231))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_232))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/keeper_touch"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_229, (V)"x")), new Entry((V)"y", R.Get(v_229, (V)"y")))))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_226, (V)"next_step"), R.Join(new V[] {v_223, v_224}, R.Call(R.Get(v_226, (V)"adjacent_goals"), R.Join(new V[] {v_223, v_224, v_225}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth(R.Or((V)(v_234 == (V)"bone"), () => (V)(v_234 == (V)"fragment")))) {
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_231))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_232))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", R.Concat((V)"core:ability/", R.Or(R.And((V)(v_234 == (V)"bone"), () => (V)"bone_strike"), () => (V)"fragment_stab"))), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_229, (V)"x")), new Entry((V)"y", R.Get(v_229, (V)"y")))))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_226, (V)"next_step"), R.Join(new V[] {v_223, v_224}, R.Call(R.Get(v_226, (V)"adjacent_goals"), R.Join(new V[] {v_223, v_224, v_225}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(R.Get(v_18, (V)"roles"), R.Get(v_224, (V)"definition_id")) == (V)"cache"))) {
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_231))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_232))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/cache_bite"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_229, (V)"x")), new Entry((V)"y", R.Get(v_229, (V)"y")))))), new Entry((V)"alert", (V)true))));
}
if (R.Truth(R.And((V)(!R.Truth(R.Get(R.Get(v_224, (V)"ai_memory"), (V)"alerted"))), () => (V)(R.Get(v_224, (V)"hp") == R.Get(v_224, (V)"max_hp"))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))));
}
var values_245 = R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_226, (V)"next_step"), R.Join(new V[] {v_223, v_224}, R.Call(R.Get(v_226, (V)"adjacent_goals"), R.Join(new V[] {v_223, v_224, v_225}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
V v_246 = values_245[0];
var values_247 = R.Join(new V[] {}, new Results((V)true));
var obj_248 = v_246;
V key_249 = (V)"alert";
R.Set(obj_248, key_249, values_247[0]);

return R.Join(new V[] {}, new Results(v_246));
}
if (R.Truth((V)(R.Get(R.Get(v_18, (V)"roles"), R.Get(v_224, (V)"definition_id")) == (V)"toad"))) {
var values_250 = Results.Empty;
V v_251 = values_250[0];
V v_252 = values_250[1];
if (R.Truth((V)(!R.Truth(R.Get(v_224, (V)"carried_drop_id"))))) {
foreach (var row_253 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_223, (V)"drops"))))))) {
V v_254 = row_253[0];
V v_255 = row_253[1];
var values_256 = R.Join(new V[] {}, new Results(R.Get(v_255, (V)"position")));
V v_257 = values_256[0];
var values_258 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_257, (V)"x") - R.Get(v_228, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_257, (V)"y") - R.Get(v_228, (V)"y"))))).First)));
V v_259 = values_258[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_255, (V)"collected"))), () => (V)(!R.Truth(R.Get(v_255, (V)"carried_by")))), () => (V)(R.Get(v_257, (V)"floor_id") == R.Get(v_228, (V)"floor_id"))), () => (V)(v_259 <= (V)4.0d)), () => R.Call(R.Get(v_226, (V)"free"), R.Join(new V[] {v_223, R.Get(v_224, (V)"id"), R.Get(v_257, (V)"x")}, new Results(R.Get(v_257, (V)"y")))).First), () => R.Call(R.Get(v_226, (V)"visible"), R.Join(new V[] {R.Get(v_257, (V)"x")}, new Results(R.Get(v_257, (V)"y")))).First), () => R.Call(R.Get(v_226, (V)"clear_line"), R.Join(new V[] {v_223, v_228}, new Results(v_257))).First), () => R.Or(R.Or((V)(!R.Truth(v_252)), () => (V)(v_259 < v_252)), () => R.And((V)(v_259 == v_252), () => (V)(R.Get(v_255, (V)"id") < R.Get(v_251, (V)"id"))))))) {
var values_260 = R.Join(new V[] {v_255}, new Results(v_259));
v_251 = values_260[0];
v_252 = values_260[1];
}
}
}
if (R.Truth(v_251)) {
if (R.Truth((V)(v_252 <= (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/toad_gulp"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_251, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_251, (V)"position"), (V)"y")))))), new Entry((V)"support", R.Table(new Entry((V)"kind", (V)"gulp"), new Entry((V)"drop_id", R.Get(v_251, (V)"id")))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_226, (V)"next_step"), R.Join(new V[] {v_223, v_224}, new Results(R.Table(new Entry((V)((V)((V)(R.Get(R.Get(v_251, (V)"position"), (V)"y") - (V)1.0d) * R.Get(R.Get(v_226, (V)"map"), (V)"width")) + R.Get(R.Get(v_251, (V)"position"), (V)"x")), (V)true))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_231))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_232))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/toad_bite"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_229, (V)"x")), new Entry((V)"y", R.Get(v_229, (V)"y")))))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_226, (V)"next_step"), R.Join(new V[] {v_223, v_224}, R.Call(R.Get(v_226, (V)"adjacent_goals"), R.Join(new V[] {v_223, v_224, v_225}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(R.Get(v_18, (V)"roles"), R.Get(v_224, (V)"definition_id")) == (V)"reviver"))) {
var values_261 = Results.Empty;
V v_262 = values_261[0];
V v_263 = values_261[1];
if (R.Truth((V)(!R.Truth(R.Get(R.Get(v_224, (V)"ai_memory"), (V)"revival_spent"))))) {
foreach (var row_264 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_223, (V)"idea_corpses"), () => R.Table()))))))) {
V v_265 = row_264[0];
V v_266 = row_264[1];
var values_267 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_266, (V)"x") - R.Get(v_228, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_266, (V)"y") - R.Get(v_228, (V)"y"))))).First)));
V v_268 = values_267[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_266, (V)"floor_id") == R.Get(v_228, (V)"floor_id")), () => (V)(!R.Truth(R.Get(v_266, (V)"claimed_by")))), () => (V)(v_268 <= (V)4.0d)), () => R.Call(R.Get(v_226, (V)"free"), R.Join(new V[] {v_223, R.Get(v_224, (V)"id"), R.Get(v_266, (V)"x")}, new Results(R.Get(v_266, (V)"y")))).First), () => R.Call(R.Get(v_226, (V)"visible"), R.Join(new V[] {R.Get(v_266, (V)"x")}, new Results(R.Get(v_266, (V)"y")))).First), () => R.Call(R.Get(v_226, (V)"clear_line"), R.Join(new V[] {v_223, v_228}, new Results(v_266))).First), () => R.Or((V)(!R.Truth(v_263)), () => (V)(v_268 < v_263))))) {
var values_269 = R.Join(new V[] {v_266}, new Results(v_268));
v_262 = values_269[0];
v_263 = values_269[1];
}
}
}
if (R.Truth(v_262)) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/cantor_revive"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_262, (V)"x")), new Entry((V)"y", R.Get(v_262, (V)"y")))))), new Entry((V)"support", R.Table(new Entry((V)"kind", (V)"revive"), new Entry((V)"corpse_id", R.Get(v_262, (V)"id")))))));
}
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_231))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_232))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/cantor_touch"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_229, (V)"x")), new Entry((V)"y", R.Get(v_229, (V)"y")))))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_226, (V)"next_step"), R.Join(new V[] {v_223, v_224}, R.Call(R.Get(v_226, (V)"adjacent_goals"), R.Join(new V[] {v_223, v_224, v_225}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(R.Get(v_18, (V)"roles"), R.Get(v_224, (V)"definition_id")) == (V)"wraith"))) {
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_231))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_232))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/wraith_touch"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_229, (V)"x")), new Entry((V)"y", R.Get(v_229, (V)"y")))))))));
}
var values_270 = Results.Empty;
V v_271 = values_270[0];
V v_272 = values_270[1];
foreach (var row_273 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_53)))))) {
V v_274 = row_273[0];
V v_275 = row_273[1];
var values_276 = R.Join(new V[] {(V)(R.Get(v_228, (V)"x") + (V)(R.Get(v_275, (V)1.0d) * (V)2.0d))}, new Results((V)(R.Get(v_228, (V)"y") + (V)(R.Get(v_275, (V)2.0d) * (V)2.0d))));
V v_277 = values_276[0];
V v_278 = values_276[1];
var values_279 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_226, (V)"map"), (V)"rows"), (V)(R.Get(v_228, (V)"y") + R.Get(v_275, (V)2.0d)))));
V v_280 = values_279[0];
var values_281 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_228, (V)"floor_id")), new Entry((V)"x", v_277), new Entry((V)"y", v_278))));
V v_282 = values_281[0];
var values_283 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_277 - R.Get(v_229, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_278 - R.Get(v_229, (V)"y"))))).First)));
V v_284 = values_283[0];
if (R.Truth(R.And(R.And(R.And(R.And(v_280, () => (V)(R.Invoke(v_280, "sub", R.Join(new V[] {(V)(R.Get(v_228, (V)"x") + R.Get(v_275, (V)1.0d))}, new Results((V)(R.Get(v_228, (V)"x") + R.Get(v_275, (V)1.0d))))).First == (V)"#")), () => (V)(v_284 < (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_231))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_232))).First))), () => R.Call(R.Get(v_18, (V)"phase_landing"), R.Join(new V[] {v_223, v_224, v_282, R.Get(v_226, (V)"map"), R.Get(v_226, (V)"free")}, new Results(R.Get(v_226, (V)"visible")))).First), () => R.Or((V)(!R.Truth(v_272)), () => (V)(v_284 < v_272))))) {
var values_285 = R.Join(new V[] {v_282}, new Results(v_284));
v_271 = values_285[0];
v_272 = values_285[1];
}
}
if (R.Truth(v_271)) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/wraith_phase"), new Entry((V)"cells", R.Table(new Entry((V)1, v_271))), new Entry((V)"support", R.Table(new Entry((V)"kind", (V)"phase"))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_226, (V)"next_step"), R.Join(new V[] {v_223, v_224}, R.Call(R.Get(v_226, (V)"adjacent_goals"), R.Join(new V[] {v_223, v_224, v_225}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_231))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_232))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/rammer_shove"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_229, (V)"x")), new Entry((V)"y", R.Get(v_229, (V)"y")))))), new Entry((V)"shove", R.Table(new Entry((V)"floor_id", R.Get(v_228, (V)"floor_id")), new Entry((V)"x", R.Get(v_229, (V)"x")), new Entry((V)"y", R.Get(v_229, (V)"y")), new Entry((V)"dx", v_231), new Entry((V)"dy", v_232))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_226, (V)"next_step"), R.Join(new V[] {v_223, v_224}, R.Call(R.Get(v_226, (V)"adjacent_goals"), R.Join(new V[] {v_223, v_224, v_225}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
return Results.Empty;
})));
R.Set(v_18, (V)"revival_company", new V((RuleFunction)(args_286 => {
V v_287 = args_286[0];
V v_288 = args_286[1];
V v_289 = args_286[2];
var values_290 = R.Join(new V[] {}, new Results(R.Get(v_289, (V)"position")));
V v_291 = values_290[0];
foreach (var row_292 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_287, (V)"entities"))))))) {
V v_293 = row_292[0];
V v_294 = row_292[1];
var values_295 = R.Join(new V[] {}, new Results(R.Get(v_294, (V)"position")));
V v_296 = values_295[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_294 != v_289), () => (V)(R.Get(v_294, (V)"controller") == (V)"ai")), () => (V)(R.Get(v_294, (V)"hp") > (V)0.0d)), () => R.Call(R.Get(v_18, (V)"revivable"), R.Join(new V[] {}, new Results(v_294))).First), () => (V)(!R.Truth(R.Get(v_294, (V)"reward_exempt")))), () => (V)(R.Get(v_296, (V)"floor_id") == R.Get(v_291, (V)"floor_id"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_291, (V)"x") - R.Get(v_296, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_291, (V)"y") - R.Get(v_296, (V)"y"))))).First) <= (V)4.0d)), () => R.Call(R.Get(v_4, (V)"clear"), R.Join(new V[] {v_287, v_291}, new Results(v_296))).First))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
var values_297 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"companion_cell"), R.Join(new V[] {v_287, v_288}, new Results(v_289))));
V v_298 = values_297[0];
return R.Join(new V[] {(V)(v_298 != V.Nil)}, new Results(v_298));
return Results.Empty;
})));
R.Set(v_18, (V)"companion_cell", new V((RuleFunction)(args_299 => {
V v_300 = args_299[0];
V v_301 = args_299[1];
V v_302 = args_299[2];
var values_303 = R.Join(new V[] {}, new Results(R.Get(v_302, (V)"position")));
V v_304 = values_303[0];
var values_305 = R.Join(new V[] {}, new Results(R.Table()));
V v_306 = values_305[0];
V v_307 = V.Nil;
v_307 = new V((RuleFunction)(args_308 => {
V v_309 = args_308[0];
if (R.Truth(R.And(R.And(v_309, () => R.Get(v_309, (V)"x")), () => R.Get(v_309, (V)"y")))) {
var values_310 = R.Join(new V[] {}, new Results((V)true));
var obj_311 = v_306;
V key_312 = R.Concat(R.Get(v_309, (V)"x"), R.Concat((V)",", R.Get(v_309, (V)"y")));
R.Set(obj_311, key_312, values_310[0]);
}
return Results.Empty;
}));
foreach (var row_313 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_300, (V)"entities"))))))) {
V v_314 = row_313[0];
V v_315 = row_313[1];
if (R.Truth((V)(R.Get(R.Get(v_315, (V)"position"), (V)"floor_id") == R.Get(v_301, (V)"floor_id")))) {
R.Call(v_307, R.Join(new V[] {}, new Results(R.Get(v_315, (V)"position"))));
}
}
foreach (var row_316 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_301, (V)"placements"), () => R.Table()))))))) {
V v_317 = row_316[0];
V v_318 = row_316[1];
foreach (var row_319 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_318)))))) {
V v_320 = row_319[0];
V v_321 = row_319[1];
R.Call(v_307, R.Join(new V[] {}, new Results(v_321)));
}
}
foreach (var row_322 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_301, (V)"corridor_quests"), () => R.Table()))))))) {
V v_323 = row_322[0];
V v_324 = row_322[1];
foreach (var row_325 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_324, (V)"cells"))))))) {
V v_326 = row_325[0];
V v_327 = row_325[1];
R.Call(v_307, R.Join(new V[] {}, new Results(v_327)));
}
}
foreach (var row_328 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_53)))))) {
V v_329 = row_328[0];
V v_330 = row_328[1];
var values_331 = R.Join(new V[] {(V)(R.Get(v_304, (V)"x") + R.Get(v_330, (V)1.0d))}, new Results((V)(R.Get(v_304, (V)"y") + R.Get(v_330, (V)2.0d))));
V v_332 = values_331[0];
V v_333 = values_331[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.Get(R.Get(v_301, (V)"rows"), v_333), () => (V)(R.Invoke(R.Get(R.Get(v_301, (V)"rows"), v_333), "sub", R.Join(new V[] {v_332}, new Results(v_332))).First == (V)".")), () => (V)(!R.Truth(R.Get(v_306, R.Concat(v_332, R.Concat((V)",", v_333)))))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_332 - R.Get(v_301, (V)"start_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_333 - R.Get(v_301, (V)"start_y"))))).First) >= (V)8.0d)), () => R.Or((V)(!R.Truth(R.Get(v_301, (V)"elite"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_332 - R.Get(R.Get(v_301, (V)"elite"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_333 - R.Get(R.Get(v_301, (V)"elite"), (V)"y"))))).First) > (V)2.0d))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_332), new Entry((V)"y", v_333))));
}
}
return R.Join(new V[] {}, new Results(V.Nil));
return Results.Empty;
})));
R.Set(v_18, (V)"selection", new V((RuleFunction)(args_334 => {
V v_335 = args_334[0];
V v_336 = args_334[1];
var values_337 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"derive"), R.Join(new V[] {v_335}, new Results(R.Concat((V)"idea-enemies:", v_336)))));
V v_338 = values_337[0];
if (R.Truth((V)((V)(v_338 % (V)4.0d) != (V)0.0d))) {
return R.Join(new V[] {V.Nil}, new Results(v_338));
}
var values_339 = R.Join(new V[] {}, new Results(R.Table()));
V v_340 = values_339[0];
foreach (var row_341 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_18, (V)"enemies"))))))) {
V v_342 = row_341[0];
V v_343 = row_341[1];
if (R.Truth((V)(!R.Truth(R.Get(R.Get(v_18, (V)"secondary"), R.Get(v_343, (V)1.0d)))))) {
var values_344 = R.Join(new V[] {}, new Results(v_343));
var obj_345 = v_340;
V key_346 = (V)((V)R.Length(v_340) + (V)1.0d);
R.Set(obj_345, key_346, values_344[0]);
}
}
return R.Join(new V[] {R.Get(v_340, (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_338 / (V)16.0d)))).First % (V)R.Length(v_340)) + (V)1.0d))}, new Results(v_338));
return Results.Empty;
})));
R.Set(v_18, (V)"populate", new V((RuleFunction)(args_347 => {
V v_348 = args_347[0];
V v_349 = args_347[1];
V v_350 = args_347[2];
V v_351 = args_347[3];
var values_352 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"selection"), R.Join(new V[] {R.Or(R.Get(v_348, (V)"root_seed"), () => R.Get(v_348, (V)"run_seed"))}, new Results(R.Get(v_349, (V)"floor_id")))));
V v_353 = values_352[0];
V v_354 = values_352[1];
if (R.Truth((V)(!R.Truth(v_353)))) {
return Results.Empty;
}
var values_355 = R.Join(new V[] {}, new Results(R.Table()));
V v_356 = values_355[0];
foreach (var row_357 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_348, (V)"entities"))))))) {
V v_358 = row_357[0];
V v_359 = row_357[1];
var values_360 = R.Join(new V[] {}, new Results(R.Get(v_359, (V)"position")));
V v_361 = values_360[0];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_359, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_361, (V)"floor_id") == R.Get(v_349, (V)"floor_id"))), () => (V)(!R.Truth(R.Get(v_359, (V)"squad_id")))), () => R.Or(R.Or((V)(R.Get(v_359, (V)"ai_id") == (V)"guard"), () => (V)(R.Get(v_359, (V)"ai_id") == (V)"skitter")), () => (V)(R.Get(v_359, (V)"ai_id") == (V)"expansion_regular"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_361, (V)"x") - R.Get(v_349, (V)"start_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_361, (V)"y") - R.Get(v_349, (V)"start_y"))))).First) >= (V)8.0d)))) {
var values_362 = R.Join(new V[] {}, new Results(v_359));
var obj_363 = v_356;
V key_364 = (V)((V)R.Length(v_356) + (V)1.0d);
R.Set(obj_363, key_364, values_362[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_356}, new Results(new V((RuleFunction)(args_365 => {
V v_366 = args_365[0];
V v_367 = args_365[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_366, (V)"id") < R.Get(v_367, (V)"id"))));
return Results.Empty;
})))));
if (R.Truth((V)((V)R.Length(v_356) == (V)0.0d))) {
return Results.Empty;
}
var values_368 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_354 / (V)4.0d)))).First % (V)R.Length(v_356))));
V v_369 = values_368[0];
var values_370 = Results.Empty;
V v_371 = values_370[0];
V v_372 = values_370[1];
{ double end_375 = R.Num((V)R.Length(v_356)), step_376 = R.Num((V)1.0d);
for (double i_374 = R.Num((V)1.0d); step_376 > 0 ? i_374 <= end_375 : i_374 >= end_375; i_374 += step_376) {
V v_373 = i_374;
var values_377 = R.Join(new V[] {}, new Results(R.Get(v_356, (V)((V)((V)((V)(v_369 + v_373) - (V)1.0d) % (V)R.Length(v_356)) + (V)1.0d))));
V v_378 = values_377[0];
var values_379 = R.Join(new V[] {(V)true}, new Results(V.Nil));
V v_380 = values_379[0];
V v_381 = values_379[1];
if (R.Truth((V)(R.Get(v_353, (V)1.0d) == (V)"rotating_sentinel"))) {
var values_382 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_10, (V)"optional_room"), R.Join(new V[] {v_349}, new Results(R.Get(v_378, (V)"position")))).First != V.Nil)));
v_380 = values_382[0];
}
if (R.Truth((V)(R.Get(v_353, (V)1.0d) == (V)"grave_cantor"))) {
var values_383 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"revival_company"), R.Join(new V[] {v_348, v_349}, new Results(v_378))));
v_380 = values_383[0];
v_381 = values_383[1];
}
if (R.Truth((V)(R.Get(v_353, (V)1.0d) == (V)"oath_twin_a"))) {
var values_384 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"companion_cell"), R.Join(new V[] {v_348, v_349}, new Results(v_378))));
v_381 = values_384[0];

var values_385 = R.Join(new V[] {}, new Results((V)(v_381 != V.Nil)));
v_380 = values_385[0];
if (R.Truth(v_380)) {
var values_386 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_349, (V)"floor_id")), new Entry((V)"x", R.Get(v_381, (V)"x")), new Entry((V)"y", R.Get(v_381, (V)"y")))));
V v_387 = values_386[0];
var values_388 = R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_4, (V)"clear"), R.Join(new V[] {v_348, R.Get(v_378, (V)"position")}, new Results(v_387))).First, () => R.Call(R.Get(v_4, (V)"clear"), R.Join(new V[] {v_348, v_387}, new Results(R.Get(v_378, (V)"position")))).First)));
v_380 = values_388[0];
}
}
if (R.Truth(R.And(v_380, () => R.Or((V)(!R.Truth(v_381)), () => v_351)))) {
var values_389 = R.Join(new V[] {v_378}, new Results(v_381));
v_371 = values_389[0];
v_372 = values_389[1];

break;
}
}
}
if (R.Truth((V)(!R.Truth(v_371)))) {
return Results.Empty;
}
var values_390 = R.Join(new V[] {}, new Results(R.Get(v_371, (V)"definition_id")));
V v_391 = values_390[0];
R.Call(v_350, R.Join(new V[] {v_371}, new Results(R.Concat((V)"core:enemy/", R.Get(v_353, (V)1.0d)))));
foreach (var row_392 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_349, (V)"placements"), (V)"enemies"))))))) {
V v_393 = row_392[0];
V v_394 = row_392[1];
if (R.Truth(R.And((V)(R.Get(v_394, (V)"x") == R.Get(R.Get(v_371, (V)"position"), (V)"x")), () => (V)(R.Get(v_394, (V)"y") == R.Get(R.Get(v_371, (V)"position"), (V)"y"))))) {
var values_395 = R.Join(new V[] {}, new Results(R.Get(v_371, (V)"definition_id")));
var obj_396 = v_394;
V key_397 = (V)"definition_id";
R.Set(obj_396, key_397, values_395[0]);
break;
}
}
if (R.Truth(v_372)) {
var values_398 = R.Join(new V[] {}, new Results((V)(R.Get(v_353, (V)1.0d) == (V)"oath_twin_a")));
V v_399 = values_398[0];
var values_400 = R.Join(new V[] {}, R.Call(v_351, R.Join(new V[] {v_348, R.Or(R.And(v_399, () => R.Get(v_8, (V)"b")), () => v_391), R.Get(v_372, (V)"x")}, new Results(R.Get(v_372, (V)"y")))));
V v_401 = values_400[0];
if (R.Truth(v_399)) {
var values_402 = R.Join(new V[] {}, new Results(R.Get(v_401, (V)"id")));
var obj_403 = v_371;
V key_404 = (V)"oath_partner_id";
R.Set(obj_403, key_404, values_402[0]);

var values_405 = R.Join(new V[] {}, new Results(R.Get(v_371, (V)"id")));
var obj_406 = v_401;
V key_407 = (V)"oath_partner_id";
R.Set(obj_406, key_407, values_405[0]);
}
}
return Results.Empty;
})));
R.Set(v_18, (V)"validate_pending", new V((RuleFunction)(args_408 => {
V v_409 = args_408[0];
V v_410 = args_408[1];
R.Call(R.Get(v_6, (V)"validate_pending"), R.Join(new V[] {v_409}, new Results(v_410)));
var values_411 = R.Join(new V[] {}, new Results(R.And(R.Get(v_410, (V)"support"), () => (V)(R.Get(R.Get(v_410, (V)"support"), (V)"kind") == (V)"alarm"))));
V v_412 = values_411[0];
if (R.Truth(R.Or(v_412, () => (V)(R.Get(v_410, (V)"ability_id") == (V)"core:ability/keeper_alarm")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(v_412, () => (V)(R.Get(v_409, (V)"definition_id") == (V)"core:enemy/alarm_keeper")), () => (V)(R.Get(v_410, (V)"ability_id") == (V)"core:ability/keeper_alarm")), () => (V)(R.Get(R.Get(v_409, (V)"ai_memory"), (V)"alarm_spent") == (V)true)), () => (V)((V)R.Length(R.Get(v_410, (V)"target_cells")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_410, (V)"target_cells")) <= (V)2.0d))}, new Results((V)"invalid committed alarm")));
var values_413 = R.Join(new V[] {}, new Results(R.Table()));
V v_414 = values_413[0];
foreach (var row_415 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_410, (V)"target_cells"))))))) {
V v_416 = row_415[0];
V v_417 = row_415[1];
var values_418 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_417, (V)"x"), R.Concat((V)",", R.Get(v_417, (V)"y")))));
V v_419 = values_418[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_417, (V)"floor_id") == R.Get(v_410, (V)"floor_id")), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_417, (V)"x") - R.Get(v_410, (V)"source_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_417, (V)"y") - R.Get(v_410, (V)"source_y"))))).First) == (V)1.0d)), () => (V)(!R.Truth(R.Get(v_414, v_419))))}, new Results((V)"invalid alarm cell")));
var values_420 = R.Join(new V[] {}, new Results((V)true));
var obj_421 = v_414;
V key_422 = v_419;
R.Set(obj_421, key_422, values_420[0]);
}
}
var values_423 = R.Join(new V[] {}, new Results(R.And(R.Get(v_410, (V)"support"), () => (V)(R.Get(R.Get(v_410, (V)"support"), (V)"kind") == (V)"gulp"))));
V v_424 = values_423[0];
if (R.Truth(R.Or(v_424, () => (V)(R.Get(v_410, (V)"ability_id") == (V)"core:ability/toad_gulp")))) {
var values_425 = R.Join(new V[] {}, new Results(R.And(v_424, () => R.Get(R.Get(v_410, (V)"support"), (V)"drop_id"))));
V v_426 = values_425[0];
var values_427 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_410, (V)"target_cells"), (V)1.0d)));
V v_428 = values_427[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_424, () => (V)(R.Get(v_409, (V)"definition_id") == (V)"core:enemy/gulp_toad")), () => (V)(R.Get(v_410, (V)"ability_id") == (V)"core:ability/toad_gulp")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_426))).First == (V)"number")), () => (V)((V)(v_426 % (V)1.0d) == (V)0.0d)), () => (V)(v_426 >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_410, (V)"target_cells")) == (V)1.0d)), () => (V)(R.Get(v_428, (V)"floor_id") == R.Get(v_410, (V)"floor_id"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_428, (V)"x") - R.Get(v_410, (V)"source_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_428, (V)"y") - R.Get(v_410, (V)"source_y"))))).First) <= (V)1.0d))}, new Results((V)"invalid committed gulp")));
}
var values_429 = R.Join(new V[] {}, new Results(R.And(R.Get(v_410, (V)"support"), () => (V)(R.Get(R.Get(v_410, (V)"support"), (V)"kind") == (V)"revive"))));
V v_430 = values_429[0];
if (R.Truth(R.Or(v_430, () => (V)(R.Get(v_410, (V)"ability_id") == (V)"core:ability/cantor_revive")))) {
var values_431 = R.Join(new V[] {}, new Results(R.And(v_430, () => R.Get(R.Get(v_410, (V)"support"), (V)"corpse_id"))));
V v_432 = values_431[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_430, () => (V)(R.Get(v_409, (V)"definition_id") == (V)"core:enemy/grave_cantor")), () => (V)(R.Get(v_410, (V)"ability_id") == (V)"core:ability/cantor_revive")), () => (V)(R.Get(R.Get(v_409, (V)"ai_memory"), (V)"revival_spent") == (V)true)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_432))).First == (V)"number")), () => (V)((V)(v_432 % (V)1.0d) == (V)0.0d)), () => (V)(v_432 >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_410, (V)"target_cells")) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_410, (V)"target_cells"), (V)1.0d), (V)"floor_id") == R.Get(v_410, (V)"floor_id")))}, new Results((V)"invalid committed revival")));
}
var values_433 = R.Join(new V[] {}, new Results(R.And(R.Get(v_410, (V)"support"), () => (V)(R.Get(R.Get(v_410, (V)"support"), (V)"kind") == (V)"phase"))));
V v_434 = values_433[0];
if (R.Truth(R.Or(v_434, () => (V)(R.Get(v_410, (V)"ability_id") == (V)"core:ability/wraith_phase")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(v_434, () => (V)(R.Get(v_409, (V)"definition_id") == (V)"core:enemy/hollow_wraith")), () => (V)(R.Get(v_410, (V)"ability_id") == (V)"core:ability/wraith_phase")), () => (V)((V)R.Length(R.Get(v_410, (V)"target_cells")) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_410, (V)"target_cells"), (V)1.0d), (V)"floor_id") == R.Get(v_410, (V)"floor_id"))), () => R.Call(R.Get(v_18, (V)"phase_geometry"), R.Join(new V[] {R.Table(new Entry((V)"x", R.Get(v_410, (V)"source_x")), new Entry((V)"y", R.Get(v_410, (V)"source_y")))}, new Results(R.Get(R.Get(v_410, (V)"target_cells"), (V)1.0d)))).First)}, new Results((V)"invalid committed phase")));
}
var values_435 = R.Join(new V[] {}, new Results(R.Get(v_410, (V)"shove")));
V v_436 = values_435[0];
if (R.Truth(R.And((V)(!R.Truth(v_436)), () => (V)(R.Get(v_410, (V)"ability_id") != (V)"core:ability/rammer_shove")))) {
return Results.Empty;
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_409, (V)"definition_id") == (V)"core:enemy/heavy_rammer"), () => (V)(R.Get(v_410, (V)"ability_id") == (V)"core:ability/rammer_shove")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_436))).First == (V)"table")), () => (V)(R.Get(v_436, (V)"floor_id") == R.Get(v_410, (V)"floor_id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_436, (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(v_436, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_436, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_436, (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_436, (V)"dx")))).First == (V)"number")), () => (V)((V)(R.Get(v_436, (V)"dx") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_436, (V)"dy")))).First == (V)"number")), () => (V)((V)(R.Get(v_436, (V)"dy") % (V)1.0d) == (V)0.0d)), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_436, (V)"dx")))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_436, (V)"dy")))).First) == (V)1.0d)), () => (V)(R.Get(v_436, (V)"x") == (V)(R.Get(v_410, (V)"source_x") + R.Get(v_436, (V)"dx")))), () => (V)(R.Get(v_436, (V)"y") == (V)(R.Get(v_410, (V)"source_y") + R.Get(v_436, (V)"dy")))), () => (V)((V)R.Length(R.Get(v_410, (V)"target_cells")) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_410, (V)"target_cells"), (V)1.0d), (V)"x") == R.Get(v_436, (V)"x"))), () => (V)(R.Get(R.Get(R.Get(v_410, (V)"target_cells"), (V)1.0d), (V)"y") == R.Get(v_436, (V)"y")))}, new Results((V)"invalid committed shove")));
return Results.Empty;
})));
R.Set(v_18, (V)"validate", new V((RuleFunction)(args_437 => {
V v_438 = args_437[0];
V v_439 = args_437[1];
V v_440 = args_437[2];
R.Call(R.Get(v_16, (V)"validate"), R.Join(new V[] {v_438}, new Results(v_439)));
R.Call(R.Get(v_14, (V)"validate"), R.Join(new V[] {v_438}, new Results(v_439)));
R.Call(R.Get(v_12, (V)"validate"), R.Join(new V[] {v_438}, new Results(v_439)));
R.Call(R.Get(v_10, (V)"validate"), R.Join(new V[] {v_438}, new Results(v_439)));
R.Call(R.Get(v_8, (V)"validate"), R.Join(new V[] {}, new Results(v_438)));
R.Call(R.Get(v_6, (V)"validate"), R.Join(new V[] {v_438}, new Results(v_439)));
var values_441 = R.Join(new V[] {}, new Results(R.Get(v_438, (V)"idea_corpses")));
V v_442 = values_441[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(v_442 == V.Nil), () => R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_442))).First == (V)"table"), () => (V)((V)R.Length(v_442) <= (V)32.0d)))}, new Results((V)"invalid corpse store")));
var values_443 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_444 = values_443[0];
V v_445 = values_443[1];
V v_446 = V.Nil;
v_446 = new V((RuleFunction)(args_447 => {
V v_448 = args_447[0];
foreach (var row_449 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_438, (V)"entities"))))))) {
V v_450 = row_449[0];
V v_451 = row_449[1];
if (R.Truth((V)(R.Get(v_451, (V)"id") == v_448))) {
return R.Join(new V[] {}, new Results(v_451));
}
}
return Results.Empty;
}));
var values_452 = R.Join(new V[] {}, new Results(R.Table()));
V v_453 = values_452[0];
foreach (var row_454 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_438, (V)"drops"))))))) {
V v_455 = row_454[0];
V v_456 = row_454[1];
if (R.Truth(R.Get(v_456, (V)"carried_by"))) {
var values_457 = R.Join(new V[] {}, R.Call(v_446, R.Join(new V[] {}, new Results(R.Get(v_456, (V)"carried_by")))));
V v_458 = values_457[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_456, (V)"carried_by")))).First == (V)"number"), () => v_458), () => (V)(R.Get(v_458, (V)"controller") == (V)"ai")), () => (V)(R.Get(v_458, (V)"definition_id") == (V)"core:enemy/gulp_toad")), () => (V)(R.Get(v_458, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_458, (V)"carried_drop_id") == R.Get(v_456, (V)"id"))), () => (V)(!R.Truth(R.Get(v_456, (V)"collected")))), () => (V)(R.Get(R.Get(v_458, (V)"position"), (V)"floor_id") == R.Get(R.Get(v_456, (V)"position"), (V)"floor_id"))), () => (V)(!R.Truth(R.Get(v_453, R.Get(v_458, (V)"id")))))}, new Results((V)"invalid swallowed drop")));
var values_459 = R.Join(new V[] {}, new Results((V)true));
var obj_460 = v_453;
V key_461 = R.Get(v_458, (V)"id");
R.Set(obj_460, key_461, values_459[0]);
}
}
foreach (var row_462 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_438, (V)"entities"))))))) {
V v_463 = row_462[0];
V v_464 = row_462[1];
if (R.Truth(R.Get(v_464, (V)"carried_drop_id"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_464, (V)"definition_id") == (V)"core:enemy/gulp_toad"), () => R.Get(v_453, R.Get(v_464, (V)"id")))}, new Results((V)"missing swallowed drop")));
}
}
var values_465 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_466 = values_465[0];
V v_467 = values_465[1];
foreach (var row_468 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_438, (V)"entities"))))))) {
V v_469 = row_468[0];
V v_470 = row_468[1];
if (R.Truth((V)(R.Get(v_470, (V)"definition_id") == (V)"core:enemy/alarm_keeper"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(R.Get(v_470, (V)"ai_memory"), (V)"alarm_spent") == V.Nil), () => (V)(R.Get(R.Get(v_470, (V)"ai_memory"), (V)"alarm_spent") == (V)true))}, new Results((V)"invalid alarm budget")));
}
if (R.Truth((V)(R.Get(v_470, (V)"definition_id") == (V)"core:enemy/haunted_cache"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(R.Get(v_470, (V)"ai_memory"), (V)"alerted") == V.Nil), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_470, (V)"ai_memory"), (V)"alerted")))).First == (V)"boolean"))}, new Results((V)"invalid cache wake state")));
}
if (R.Truth((V)(R.Get(v_470, (V)"definition_id") == (V)"core:enemy/grave_cantor"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(R.Get(v_470, (V)"ai_memory"), (V)"revival_spent") == V.Nil), () => (V)(R.Get(R.Get(v_470, (V)"ai_memory"), (V)"revival_spent") == (V)true))}, new Results((V)"invalid revival budget")));
}
if (R.Truth(R.Or(R.Get(v_470, (V)"fragment_of"), () => (V)(R.Get(v_470, (V)"definition_id") == (V)"core:enemy/bone_fragment")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_470, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_470, (V)"definition_id") == (V)"core:enemy/bone_fragment")), () => (V)(R.Get(v_470, (V)"reward_exempt") == (V)true)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_470, (V)"fragment_of")))).First == (V)"number")), () => (V)((V)(R.Get(v_470, (V)"fragment_of") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_470, (V)"fragment_of") >= (V)1.0d)), () => (V)(R.Get(v_470, (V)"fragment_of") < R.Get(v_470, (V)"id"))), () => (V)(!R.Truth(R.Get(v_470, (V)"revival_of")))), () => (V)(!R.Truth(R.Get(v_470, (V)"revived_by"))))}, new Results((V)"invalid bone fragment")));
var values_471 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_466, R.Get(v_470, (V)"fragment_of")), () => (V)0.0d) + (V)1.0d)));
var obj_472 = v_466;
V key_473 = R.Get(v_470, (V)"fragment_of");
R.Set(obj_472, key_473, values_471[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_466, R.Get(v_470, (V)"fragment_of")) <= (V)2.0d)}, new Results((V)"fragment cap exceeded")));
} else if (R.Truth(R.Get(v_470, (V)"called_by"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_470, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_470, (V)"definition_id") == (V)"core:enemy/ruin_skitter")), () => (V)(R.Get(v_470, (V)"reward_exempt") == (V)true)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_470, (V)"called_by")))).First == (V)"number")), () => (V)((V)(R.Get(v_470, (V)"called_by") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_470, (V)"called_by") >= (V)1.0d)), () => (V)(R.Get(v_470, (V)"called_by") < R.Get(v_470, (V)"id"))), () => (V)(!R.Truth(R.Get(v_470, (V)"revival_of")))), () => (V)(!R.Truth(R.Get(v_470, (V)"revived_by"))))}, new Results((V)"invalid alarm reinforcement")));
var values_474 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_467, R.Get(v_470, (V)"called_by")), () => (V)0.0d) + (V)1.0d)));
var obj_475 = v_467;
V key_476 = R.Get(v_470, (V)"called_by");
R.Set(obj_475, key_476, values_474[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_467, R.Get(v_470, (V)"called_by")) <= (V)2.0d)}, new Results((V)"alarm reinforcement cap exceeded")));
} else if (R.Truth(R.Or(R.Or(R.Get(v_470, (V)"revival_of"), () => R.Get(v_470, (V)"revived_by")), () => R.Get(v_470, (V)"reward_exempt")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_470, (V)"controller") == (V)"ai"), () => R.Call(R.Get(v_18, (V)"revivable"), R.Join(new V[] {}, new Results(v_470))).First), () => (V)(R.Get(v_470, (V)"reward_exempt") == (V)true)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_470, (V)"revival_of")))).First == (V)"number")), () => (V)((V)(R.Get(v_470, (V)"revival_of") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_470, (V)"revival_of") >= (V)1.0d)), () => (V)(R.Get(v_470, (V)"revival_of") < R.Get(v_470, (V)"id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_470, (V)"revived_by")))).First == (V)"number")), () => (V)((V)(R.Get(v_470, (V)"revived_by") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_470, (V)"revived_by") >= (V)1.0d)), () => (V)(R.Get(v_470, (V)"revived_by") < R.Get(v_470, (V)"id")))}, new Results((V)"invalid revived actor")));
}
}
foreach (var row_477 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_442, () => R.Table()))))))) {
V v_478 = row_477[0];
V v_479 = row_477[1];
var values_480 = R.Join(new V[] {}, new Results(R.Get(v_439, R.Get(v_479, (V)"floor_id"))));
V v_481 = values_480[0];
var values_482 = R.Join(new V[] {}, R.Call(R.Get(v_440, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_479, (V)"definition_id")))));
V v_483 = values_482[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_479, (V)"id")))).First == (V)"number"), () => (V)((V)(R.Get(v_479, (V)"id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_479, (V)"id") >= (V)1.0d)), () => (V)(R.Get(v_479, (V)"id") < R.Get(v_438, (V)"next_entity_id"))), () => (V)(!R.Truth(R.Get(v_444, R.Get(v_479, (V)"id"))))), () => (V)(!R.Truth(R.Call(v_446, R.Join(new V[] {}, new Results(R.Get(v_479, (V)"id")))).First))), () => v_481), () => v_483), () => R.Call(R.Get(v_18, (V)"revivable"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"ai_id", R.Get(v_483, (V)"ai")), new Entry((V)"definition_id", R.Get(v_479, (V)"definition_id")))))).First), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_479, (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(v_479, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_479, (V)"x") >= (V)1.0d)), () => (V)(R.Get(v_479, (V)"x") <= R.Get(v_481, (V)"width"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_479, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_479, (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_479, (V)"y") >= (V)1.0d)), () => (V)(R.Get(v_479, (V)"y") <= R.Get(v_481, (V)"height"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_479, (V)"max_hp")))).First == (V)"number")), () => (V)((V)(R.Get(v_479, (V)"max_hp") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_479, (V)"max_hp") >= (V)1.0d)), () => (V)(R.Get(v_479, (V)"max_hp") <= (V)1000000.0d))}, new Results((V)"invalid corpse record")));
var values_484 = R.Join(new V[] {}, new Results((V)true));
var obj_485 = v_444;
V key_486 = R.Get(v_479, (V)"id");
R.Set(obj_485, key_486, values_484[0]);

var values_487 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_445, R.Get(v_479, (V)"floor_id")), () => (V)0.0d) + (V)1.0d)));
var obj_488 = v_445;
V key_489 = R.Get(v_479, (V)"floor_id");
R.Set(obj_488, key_489, values_487[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_445, R.Get(v_479, (V)"floor_id")) <= (V)16.0d)}, new Results((V)"corpse floor cap exceeded")));
if (R.Truth(R.Get(v_479, (V)"claimed_by"))) {
var values_490 = R.Join(new V[] {}, R.Call(v_446, R.Join(new V[] {}, new Results(R.Get(v_479, (V)"claimed_by")))));
V v_491 = values_490[0];
var values_492 = R.Join(new V[] {}, new Results(R.And(v_491, () => R.Get(v_491, (V)"pending_attack"))));
V v_493 = values_492[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_491, () => (V)(R.Get(v_491, (V)"definition_id") == (V)"core:enemy/grave_cantor")), () => (V)(R.Get(R.Get(v_491, (V)"position"), (V)"floor_id") == R.Get(v_479, (V)"floor_id"))), () => v_493), () => R.Get(v_493, (V)"support")), () => (V)(R.Get(R.Get(v_493, (V)"support"), (V)"kind") == (V)"revive")), () => (V)(R.Get(R.Get(v_493, (V)"support"), (V)"corpse_id") == R.Get(v_479, (V)"id"))), () => (V)(R.Get(R.Get(R.Get(v_493, (V)"target_cells"), (V)1.0d), (V)"x") == R.Get(v_479, (V)"x"))), () => (V)(R.Get(R.Get(R.Get(v_493, (V)"target_cells"), (V)1.0d), (V)"y") == R.Get(v_479, (V)"y")))}, new Results((V)"invalid corpse claim")));
}
}
foreach (var row_494 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_438, (V)"entities"))))))) {
V v_495 = row_494[0];
V v_496 = row_494[1];
var values_497 = R.Join(new V[] {}, new Results(R.Get(v_496, (V)"pending_attack")));
V v_498 = values_497[0];
if (R.Truth(R.And(R.And(v_498, () => R.Get(v_498, (V)"support")), () => (V)(R.Get(R.Get(v_498, (V)"support"), (V)"kind") == (V)"revive")))) {
var values_499 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"corpse"), R.Join(new V[] {v_438}, new Results(R.Get(R.Get(v_498, (V)"support"), (V)"corpse_id")))));
V v_500 = values_499[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_500, () => (V)(R.Get(v_500, (V)"claimed_by") == R.Get(v_496, (V)"id")))}, new Results((V)"missing pending corpse claim")));
} else if (R.Truth(R.And(R.And(v_498, () => R.Get(v_498, (V)"support")), () => (V)(R.Get(R.Get(v_498, (V)"support"), (V)"kind") == (V)"gulp")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_18, (V)"drop"), R.Join(new V[] {v_438}, new Results(R.Get(R.Get(v_498, (V)"support"), (V)"drop_id")))).First}, new Results((V)"missing pending gulp drop")));
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_18));
return Results.Empty;
} }
