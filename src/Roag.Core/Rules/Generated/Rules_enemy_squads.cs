// Static C# port of src/enemy_squads.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 35bbef282047e79221c19303f12a861df6636fb32eee6707ce7550149a447be1
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_enemy_squads { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"ward_radius", (V)4.0d), new Entry((V)"guard_leash", (V)6.0d), new Entry((V)"rally_radius", (V)5.0d), new Entry((V)"snare_cap", (V)2.0d), new Entry((V)"snare_delay", (V)40.0d))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"warded_bodyguards"), new Entry((V)2, (V)"healer_rushwings"), new Entry((V)3, (V)"snares_and_siege"), new Entry((V)4, (V)"shield_and_archer"), new Entry((V)5, (V)"captain_and_followers"))));
var obj_8 = v_6;
V key_9 = (V)"order";
R.Set(obj_8, key_9, values_7[0]);
var values_10 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"ward_weaver"), new Entry((V)2, (V)"Ward Weaver"), new Entry((V)3, (V)6.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)80.0d), new Entry((V)7, (V)140.0d), new Entry((V)8, (V)140.0d), new Entry((V)9, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)"bodyguard_brute"), new Entry((V)2, (V)"Bodyguard Brute"), new Entry((V)3, (V)10.0d), new Entry((V)4, (V)3.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)70.0d), new Entry((V)7, (V)100.0d), new Entry((V)8, (V)130.0d), new Entry((V)9, (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)"mend_acolyte"), new Entry((V)2, (V)"Mend Acolyte"), new Entry((V)3, (V)6.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)85.0d), new Entry((V)7, (V)120.0d), new Entry((V)8, (V)150.0d), new Entry((V)9, (V)4.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)"rushwing"), new Entry((V)2, (V)"Rushwing"), new Entry((V)3, (V)4.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)130.0d), new Entry((V)7, (V)60.0d), new Entry((V)8, (V)100.0d), new Entry((V)9, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)"snare_scribe"), new Entry((V)2, (V)"Snare Scribe"), new Entry((V)3, (V)6.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)80.0d), new Entry((V)7, (V)125.0d), new Entry((V)8, (V)140.0d), new Entry((V)9, (V)4.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)"siege_wisp"), new Entry((V)2, (V)"Siege Wisp"), new Entry((V)3, (V)6.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)65.0d), new Entry((V)7, (V)155.0d), new Entry((V)8, (V)150.0d), new Entry((V)9, (V)5.0d))), new Entry((V)7, R.Table(new Entry((V)1, (V)"shield_bearer"), new Entry((V)2, (V)"Shield Bearer"), new Entry((V)3, (V)9.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)70.0d), new Entry((V)7, (V)100.0d), new Entry((V)8, (V)135.0d), new Entry((V)9, (V)1.0d))), new Entry((V)8, R.Table(new Entry((V)1, (V)"pin_archer"), new Entry((V)2, (V)"Pin Archer"), new Entry((V)3, (V)5.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)90.0d), new Entry((V)7, (V)110.0d), new Entry((V)8, (V)135.0d), new Entry((V)9, (V)6.0d))), new Entry((V)9, R.Table(new Entry((V)1, (V)"banner_captain"), new Entry((V)2, (V)"Banner Captain"), new Entry((V)3, (V)8.0d), new Entry((V)4, (V)2.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)75.0d), new Entry((V)7, (V)110.0d), new Entry((V)8, (V)140.0d), new Entry((V)9, (V)1.0d))), new Entry((V)10, R.Table(new Entry((V)1, (V)"banner_follower"), new Entry((V)2, (V)"Banner Follower"), new Entry((V)3, (V)3.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)95.0d), new Entry((V)7, (V)90.0d), new Entry((V)8, (V)125.0d), new Entry((V)9, (V)1.0d))))));
var obj_11 = v_6;
V key_12 = (V)"enemies";
R.Set(obj_11, key_12, values_10[0]);
var values_13 = R.Join(new V[] {}, new Results(R.Table()));
var obj_14 = v_6;
V key_15 = (V)"roles";
R.Set(obj_14, key_15, values_13[0]);
foreach (var row_16 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"enemies"))))))) {
V v_17 = row_16[0];
V v_18 = row_16[1];
var values_19 = R.Join(new V[] {}, new Results(R.Get(v_18, (V)1.0d)));
var obj_20 = R.Get(v_6, (V)"roles");
V key_21 = R.Concat((V)"core:enemy/", R.Get(v_18, (V)1.0d));
R.Set(obj_20, key_21, values_19[0]);
}
var values_22 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"warded_bodyguards", R.Table(new Entry((V)1, (V)"ward_weaver"), new Entry((V)2, (V)"bodyguard_brute"), new Entry((V)3, (V)2.0d))), new Entry((V)"healer_rushwings", R.Table(new Entry((V)1, (V)"mend_acolyte"), new Entry((V)2, (V)"rushwing"), new Entry((V)3, (V)2.0d))), new Entry((V)"snares_and_siege", R.Table(new Entry((V)1, (V)"snare_scribe"), new Entry((V)2, (V)"siege_wisp"), new Entry((V)3, (V)1.0d))), new Entry((V)"shield_and_archer", R.Table(new Entry((V)1, (V)"shield_bearer"), new Entry((V)2, (V)"pin_archer"), new Entry((V)3, (V)1.0d))), new Entry((V)"captain_and_followers", R.Table(new Entry((V)1, (V)"banner_captain"), new Entry((V)2, (V)"banner_follower"), new Entry((V)3, (V)2.0d))))));
var obj_23 = v_6;
V key_24 = (V)"groups";
R.Set(obj_23, key_24, values_22[0]);
var values_25 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"overgrown_ruins", R.Table(new Entry((V)1, (V)"captain_and_followers"))), new Entry((V)"fungal_hollows", R.Table(new Entry((V)1, (V)"healer_rushwings"))), new Entry((V)"sunken_aqueduct", R.Table(new Entry((V)1, (V)"shield_and_archer"))), new Entry((V)"ashen_bastion", R.Table(new Entry((V)1, (V)"warded_bodyguards"), new Entry((V)2, (V)"shield_and_archer"), new Entry((V)3, (V)"captain_and_followers"))), new Entry((V)"glacial_observatory", R.Table(new Entry((V)1, (V)"snares_and_siege"))), new Entry((V)"fractured_archive", R.Table(new Entry((V)1, (V)"warded_bodyguards"), new Entry((V)2, (V)"snares_and_siege"))), new Entry((V)"sundered_forge", R.Table(new Entry((V)1, (V)"captain_and_followers"), new Entry((V)2, (V)"healer_rushwings"))))));
var obj_26 = v_6;
V key_27 = (V)"biomes";
R.Set(obj_26, key_27, values_25[0]);
var values_28 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"ward_weaver", (V)84.0d), new Entry((V)"bodyguard_brute", (V)110.0d), new Entry((V)"mend_acolyte", (V)86.0d), new Entry((V)"rushwing", (V)120.0d), new Entry((V)"snare_scribe", (V)124.0d), new Entry((V)"siege_wisp", (V)108.0d), new Entry((V)"shield_bearer", (V)96.0d), new Entry((V)"pin_archer", (V)87.0d), new Entry((V)"banner_captain", (V)98.0d), new Entry((V)"banner_follower", (V)97.0d))));
var obj_29 = v_6;
V key_30 = (V)"tiny";
R.Set(obj_29, key_30, values_28[0]);
var values_31 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"ward_weaver", (V)319.0d), new Entry((V)"bodyguard_brute", (V)270.0d), new Entry((V)"mend_acolyte", (V)123.0d), new Entry((V)"rushwing", (V)415.0d), new Entry((V)"snare_scribe", (V)221.0d), new Entry((V)"siege_wisp", (V)321.0d), new Entry((V)"shield_bearer", (V)126.0d), new Entry((V)"pin_archer", (V)78.0d), new Entry((V)"banner_captain", (V)172.0d), new Entry((V)"banner_follower", (V)174.0d))));
var obj_32 = v_6;
V key_33 = (V)"onebit";
R.Set(obj_32, key_33, values_31[0]);
var values_34 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"squad_ward_effect", (V)636.0d), new Entry((V)"squad_heal_effect", (V)535.0d), new Entry((V)"squad_snare_effect", (V)715.0d), new Entry((V)"squad_shield_effect", (V)239.0d), new Entry((V)"squad_rally_effect", (V)527.0d))));
var obj_35 = v_6;
V key_36 = (V)"cues";
R.Set(obj_35, key_36, values_34[0]);
var values_37 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_38 = values_37[0];
V v_39 = V.Nil;
v_39 = new V((RuleFunction)(args_40 => {
V v_41 = args_40[0];
V v_42 = args_40[1];
return R.Join(new V[] {}, new Results(R.Concat(v_41, R.Concat((V)",", v_42))));
return Results.Empty;
}));
R.Set(v_6, (V)"flank_reachable", new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
V v_46 = args_43[2];
var values_47 = R.Join(new V[] {}, new Results(R.Table()));
V v_48 = values_47[0];
foreach (var row_49 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(v_46, () => R.Table()))))))) {
V v_50 = row_49[0];
var values_51 = R.Join(new V[] {}, new Results((V)true));
var obj_52 = v_48;
V key_53 = v_50;
R.Set(obj_52, key_53, values_51[0]);
}
{ double end_56 = R.Num((V)R.Length(v_45)), step_57 = R.Num((V)1.0d);
for (double i_55 = R.Num((V)2.0d); step_57 > 0 ? i_55 <= end_56 : i_55 >= end_56; i_55 += step_57) {
V v_54 = i_55;
var values_58 = R.Join(new V[] {}, new Results((V)true));
var obj_59 = v_48;
V key_60 = R.Call(v_39, R.Join(new V[] {R.Get(R.Get(v_45, v_54), (V)"x")}, new Results(R.Get(R.Get(v_45, v_54), (V)"y")))).First;
R.Set(obj_59, key_60, values_58[0]);
}
}
var values_61 = R.Join(new V[] {}, new Results(R.Get(v_45, (V)1.0d)));
V v_62 = values_61[0];
var values_63 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_44, (V)"start_x")), new Entry((V)"y", R.Get(v_44, (V)"start_y")))))));
V v_64 = values_63[0];
var values_65 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Call(v_39, R.Join(new V[] {R.Get(v_44, (V)"start_x")}, new Results(R.Get(v_44, (V)"start_y")))).First, (V)true))));
V v_66 = values_65[0];
var values_67 = R.Join(new V[] {}, new Results((V)1.0d));
V v_68 = values_67[0];
while (R.Truth(R.Get(v_64, v_68))) {
var values_69 = R.Join(new V[] {}, new Results(R.Get(v_64, v_68)));
V v_70 = values_69[0];

var values_71 = R.Join(new V[] {}, new Results((V)(v_68 + (V)1.0d)));
v_68 = values_71[0];
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_70, (V)"x") - R.Get(v_62, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_70, (V)"y") - R.Get(v_62, (V)"y"))))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results((V)true));
}
foreach (var row_72 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_38)))))) {
V v_73 = row_72[0];
V v_74 = row_72[1];
var values_75 = R.Join(new V[] {(V)(R.Get(v_70, (V)"x") + R.Get(v_74, (V)1.0d))}, new Results((V)(R.Get(v_70, (V)"y") + R.Get(v_74, (V)2.0d))));
V v_76 = values_75[0];
V v_77 = values_75[1];
var values_78 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {v_76}, new Results(v_77))));
V v_79 = values_78[0];
var values_80 = R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_44, (V)"rows"), v_77), () => R.Get(R.Get(v_4, (V)"definitions"), R.Invoke(R.Get(R.Get(v_44, (V)"rows"), v_77), "sub", R.Join(new V[] {v_76}, new Results(v_76))).First))));
V v_81 = values_80[0];
if (R.Truth(R.And(R.And(R.And(v_81, () => R.Get(v_81, (V)"walkable")), () => (V)(!R.Truth(R.Get(v_66, v_79)))), () => (V)(!R.Truth(R.Get(v_48, v_79)))))) {
var values_82 = R.Join(new V[] {}, new Results((V)true));
var obj_83 = v_66;
V key_84 = v_79;
R.Set(obj_83, key_84, values_82[0]);

var values_85 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_76), new Entry((V)"y", v_77))));
var obj_86 = v_64;
V key_87 = (V)((V)R.Length(v_64) + (V)1.0d);
R.Set(obj_86, key_87, values_85[0]);
}
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_6, (V)"distance", new V((RuleFunction)(args_88 => {
V v_89 = args_88[0];
V v_90 = args_88[1];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_89, (V)"x") - R.Get(v_90, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_89, (V)"y") - R.Get(v_90, (V)"y"))))).First)));
return Results.Empty;
})));
R.Set(v_6, (V)"entity", new V((RuleFunction)(args_91 => {
V v_92 = args_91[0];
V v_93 = args_91[1];
foreach (var row_94 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_92, (V)"entities"))))))) {
V v_95 = row_94[0];
V v_96 = row_94[1];
if (R.Truth((V)(R.Get(v_96, (V)"id") == v_93))) {
return R.Join(new V[] {}, new Results(v_96));
}
}
return Results.Empty;
})));
R.Set(v_6, (V)"leader", new V((RuleFunction)(args_97 => {
V v_98 = args_97[0];
V v_99 = args_97[1];
var values_100 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"entity"), R.Join(new V[] {v_98}, new Results(R.Get(v_99, (V)"squad_leader_id")))));
V v_101 = values_100[0];
var values_102 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"groups"), R.Get(v_99, (V)"squad_kind"))));
V v_103 = values_102[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(v_101, () => R.Get(v_101, (V)"hp")), () => (V)(R.Get(v_101, (V)"hp") > (V)0.0d)), () => (V)(R.Get(R.Get(v_101, (V)"position"), (V)"floor_id") == R.Get(R.Get(v_99, (V)"position"), (V)"floor_id"))), () => v_103), () => (V)(R.Get(v_101, (V)"definition_id") == R.Concat((V)"core:enemy/", R.Get(v_103, (V)1.0d)))), () => (V)(R.Get(v_101, (V)"squad_id") == R.Get(v_99, (V)"squad_id"))))) {
return R.Join(new V[] {}, new Results(v_101));
}
return Results.Empty;
})));
R.Set(v_6, (V)"clear", new V((RuleFunction)(args_104 => {
V v_105 = args_104[0];
V v_106 = args_104[1];
V v_107 = args_104[2];
if (R.Truth((V)(R.Get(v_106, (V)"floor_id") != R.Get(v_107, (V)"floor_id")))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_108 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_106, (V)"floor_id") == R.Get(v_105, (V)"active_floor_id")), () => R.Get(v_105, (V)"generated_map")), () => R.And(R.Get(v_105, (V)"inactive_floor_maps"), () => R.Get(R.Get(v_105, (V)"inactive_floor_maps"), R.Get(v_106, (V)"floor_id"))))));
V v_109 = values_108[0];
var values_110 = R.Join(new V[] {}, new Results(R.Or(v_109, () => R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"get_map"), R.Join(new V[] {}, new Results(R.Get(v_105, (V)"map_id")))).First)));
v_109 = values_110[0];
if (R.Truth((V)(!R.Truth(v_109)))) {
return R.Join(new V[] {}, new Results((V)false));
}
V v_111 = V.Nil;
v_111 = new V((RuleFunction)(args_112 => {
V v_113 = args_112[0];
V v_114 = args_112[1];
var values_115 = R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_109, (V)"rows"), v_114), () => R.Invoke(R.Get(R.Get(v_109, (V)"rows"), v_114), "sub", R.Join(new V[] {v_113}, new Results(v_113))).First)));
V v_116 = values_115[0];
var values_117 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_4, (V)"definitions"), v_116)));
V v_118 = values_117[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_118)), () => (V)(!R.Truth(R.Get(v_118, (V)"projectile_passable")))), () => R.Get(v_118, (V)"blocks_vision")))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_119 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_105, (V)"entities"))))))) {
V v_120 = row_119[0];
V v_121 = row_119[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.Get(v_121, (V)"position"), () => (V)(R.Get(R.Get(v_121, (V)"position"), (V)"floor_id") == R.Get(v_106, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_121, (V)"position"), (V)"x") == v_113)), () => (V)(R.Get(R.Get(v_121, (V)"position"), (V)"y") == v_114)), () => R.Or(R.Get(v_121, (V)"blocks_projectiles"), () => R.Get(v_121, (V)"blocks_vision"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
var values_122 = R.Join(new V[] {R.Get(v_106, (V)"x")}, new Results(R.Get(v_106, (V)"y")));
V v_123 = values_122[0];
V v_124 = values_122[1];
var values_125 = R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_107, (V)"x") - v_123)))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_107, (V)"y") - v_124)))));
V v_126 = values_125[0];
V v_127 = values_125[1];
var values_128 = R.Join(new V[] {R.Or(R.And((V)(R.Get(v_107, (V)"x") > v_123), () => (V)1.0d), () => (-(V)1.0d))}, new Results(R.Or(R.And((V)(R.Get(v_107, (V)"y") > v_124), () => (V)1.0d), () => (-(V)1.0d))));
V v_129 = values_128[0];
V v_130 = values_128[1];
var values_131 = R.Join(new V[] {}, new Results((V)(v_126 - v_127)));
V v_132 = values_131[0];
while (R.Truth(R.Or((V)(v_123 != R.Get(v_107, (V)"x")), () => (V)(v_124 != R.Get(v_107, (V)"y"))))) {
var values_133 = R.Join(new V[] {v_123}, new Results(v_124));
V v_134 = values_133[0];
V v_135 = values_133[1];

var values_136 = R.Join(new V[] {}, new Results((V)(v_132 * (V)2.0d)));
V v_137 = values_136[0];
if (R.Truth((V)(v_137 > (-v_127)))) {
var values_138 = R.Join(new V[] {}, new Results((V)(v_123 + v_129)));
v_123 = values_138[0];

var values_139 = R.Join(new V[] {}, new Results((V)(v_132 - v_127)));
v_132 = values_139[0];
}
if (R.Truth((V)(v_137 < v_126))) {
var values_140 = R.Join(new V[] {}, new Results((V)(v_124 + v_130)));
v_124 = values_140[0];

var values_141 = R.Join(new V[] {}, new Results((V)(v_132 + v_126)));
v_132 = values_141[0];
}
if (R.Truth(R.And(R.And((V)(v_123 != v_134), () => (V)(v_124 != v_135)), () => R.Or((V)(!R.Truth(R.Call(v_111, R.Join(new V[] {v_123}, new Results(v_135))).First)), () => (V)(!R.Truth(R.Call(v_111, R.Join(new V[] {v_134}, new Results(v_124))).First)))))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth((V)(!R.Truth(R.Call(v_111, R.Join(new V[] {v_123}, new Results(v_124))).First)))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_6, (V)"ward_source", new V((RuleFunction)(args_142 => {
V v_143 = args_142[0];
V v_144 = args_142[1];
if (R.Truth((V)(R.Get(R.Get(v_6, (V)"roles"), R.Get(v_144, (V)"definition_id")) != (V)"bodyguard_brute"))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_145 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"leader"), R.Join(new V[] {v_143}, new Results(v_144))));
V v_146 = values_145[0];
if (R.Truth(R.And(R.And(R.And(R.And(v_146, () => R.Get(R.Get(v_146, (V)"ai_memory"), (V)"ward_ready")), () => (V)(R.Or(R.Get(v_146, (V)"stunned_until"), () => (V)0.0d) <= R.Get(v_143, (V)"clock"))), () => (V)(R.Call(R.Get(v_6, (V)"distance"), R.Join(new V[] {R.Get(v_146, (V)"position")}, new Results(R.Get(v_144, (V)"position")))).First <= R.Get(v_6, (V)"ward_radius"))), () => R.Call(R.Get(v_6, (V)"clear"), R.Join(new V[] {v_143, R.Get(v_146, (V)"position")}, new Results(R.Get(v_144, (V)"position")))).First))) {
return R.Join(new V[] {}, new Results(v_146));
}
return Results.Empty;
})));
R.Set(v_6, (V)"rally_source", new V((RuleFunction)(args_147 => {
V v_148 = args_147[0];
V v_149 = args_147[1];
if (R.Truth((V)(R.Get(R.Get(v_6, (V)"roles"), R.Get(v_149, (V)"definition_id")) != (V)"banner_follower"))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_150 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"leader"), R.Join(new V[] {v_148}, new Results(v_149))));
V v_151 = values_150[0];
if (R.Truth(R.And(R.And(R.And(v_151, () => (V)(R.Or(R.Get(v_151, (V)"stunned_until"), () => (V)0.0d) <= R.Get(v_148, (V)"clock"))), () => (V)(R.Call(R.Get(v_6, (V)"distance"), R.Join(new V[] {R.Get(v_151, (V)"position")}, new Results(R.Get(v_149, (V)"position")))).First <= R.Get(v_6, (V)"rally_radius"))), () => R.Call(R.Get(v_6, (V)"clear"), R.Join(new V[] {v_148, R.Get(v_151, (V)"position")}, new Results(R.Get(v_149, (V)"position")))).First))) {
return R.Join(new V[] {}, new Results(v_151));
}
return Results.Empty;
})));
R.Set(v_6, (V)"block", new V((RuleFunction)(args_152 => {
V v_153 = args_152[0];
V v_154 = args_152[1];
V v_155 = args_152[2];
V v_156 = args_152[3];
var values_157 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"ward_source"), R.Join(new V[] {v_153}, new Results(v_154))));
V v_158 = values_157[0];
if (R.Truth(v_158)) {
return R.Join(new V[] {(V)"forcefield"}, new Results(R.Get(v_158, (V)"id")));
}
if (R.Truth(R.And(R.And(R.And(R.And(R.And(v_156, () => (V)(R.Get(R.Get(v_6, (V)"roles"), R.Get(v_154, (V)"definition_id")) == (V)"shield_bearer")), () => R.Get(v_155, (V)"position")), () => (V)(R.Get(R.Get(v_155, (V)"position"), (V)"floor_id") == R.Get(R.Get(v_154, (V)"position"), (V)"floor_id"))), () => (V)(R.Or(R.Get(v_154, (V)"stunned_until"), () => (V)0.0d) <= R.Get(v_153, (V)"clock"))), () => (V)(!R.Truth(R.Get(R.Get(v_154, (V)"ai_memory"), (V)"shield_lowered")))))) {
var values_159 = R.Join(new V[] {R.Get(v_154, (V)"ai_memory")}, new Results(R.Get(v_154, (V)"position")));
V v_160 = values_159[0];
V v_161 = values_159[1];
var values_162 = R.Join(new V[] {(V)(R.Get(R.Get(v_155, (V)"position"), (V)"x") - R.Get(v_161, (V)"x"))}, new Results((V)(R.Get(R.Get(v_155, (V)"position"), (V)"y") - R.Get(v_161, (V)"y"))));
V v_163 = values_162[0];
V v_164 = values_162[1];
var values_165 = R.Join(new V[] {R.Or(R.Get(v_160, (V)"shield_dx"), () => (V)0.0d)}, new Results(R.Or(R.Get(v_160, (V)"shield_dy"), () => (V)1.0d)));
V v_166 = values_165[0];
V v_167 = values_165[1];
var values_168 = R.Join(new V[] {}, new Results((V)((V)(v_163 * v_166) + (V)(v_164 * v_167))));
V v_169 = values_168[0];
if (R.Truth(R.And((V)(v_169 > (V)0.0d), () => (V)(v_169 >= R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)((V)(v_163 * v_167) - (V)(v_164 * v_166))))).First)))) {
return R.Join(new V[] {(V)"directional shield"}, new Results(R.Get(v_154, (V)"id")));
}
}
return Results.Empty;
})));
R.Set(v_6, (V)"plan", new V((RuleFunction)(args_170 => {
V v_171 = args_170[0];
V v_172 = args_170[1];
var values_173 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"biomes"), R.Invoke(R.Or(R.Get(v_172, (V)"biome_id"), () => (V)""), "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))).First)));
V v_174 = values_173[0];
if (R.Truth((V)(!R.Truth(v_174)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_175 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"derive"), R.Join(new V[] {R.Get(v_171, (V)"run_seed")}, new Results(R.Concat((V)"enemy-squad:", R.Get(v_172, (V)"floor_id"))))))));
V v_176 = values_175[0];
var values_177 = R.Join(new V[] {}, new Results(R.Get(v_174, R.Call(R.Get(v_2, (V)"next_int"), R.Join(new V[] {v_176}, new Results((V)R.Length(v_174)))).First)));
V v_178 = values_177[0];
var values_179 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"groups"), v_178)));
V v_180 = values_179[0];
var values_181 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_178 == (V)"captain_and_followers"), () => (V)((V)1.0d + R.Call(R.Get(v_2, (V)"next_int"), R.Join(new V[] {v_176}, new Results((V)4.0d))).First)), () => R.Get(v_180, (V)3.0d))));
V v_182 = values_181[0];
var values_183 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_184 = values_183[0];
V v_185 = values_183[1];
foreach (var row_186 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_171, (V)"entities"))))))) {
V v_187 = row_186[0];
V v_188 = row_186[1];
if (R.Truth(R.And(R.Get(v_188, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_188, (V)"position"), (V)"floor_id") == R.Get(v_172, (V)"floor_id"))))) {
var values_189 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {R.Get(R.Get(v_188, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_188, (V)"position"), (V)"y")))));
V v_190 = values_189[0];
var values_191 = R.Join(new V[] {}, new Results((V)true));
var obj_192 = v_184;
V key_193 = v_190;
R.Set(obj_192, key_193, values_191[0]);
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_188, (V)"controller") != (V)"ai"), () => (V)(R.Get(v_188, (V)"controller") != (V)"player")), () => (V)(R.Get(v_188, (V)"controller") != (V)"door")), () => (V)(R.Get(v_188, (V)"controller") != (V)"chest")))) {
var values_194 = R.Join(new V[] {}, new Results((V)true));
var obj_195 = v_185;
V key_196 = v_190;
R.Set(obj_195, key_196, values_194[0]);
}
}
}
foreach (var row_197 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_172, (V)"placements"), (V)"stairs"), () => R.Table()))))))) {
V v_198 = row_197[0];
V v_199 = row_197[1];
var values_200 = R.Join(new V[] {}, new Results((V)true));
var obj_201 = v_184;
V key_202 = R.Call(v_39, R.Join(new V[] {R.Get(v_199, (V)"x")}, new Results(R.Get(v_199, (V)"y")))).First;
R.Set(obj_201, key_202, values_200[0]);
}
foreach (var row_203 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_172, (V)"placements"), (V)"chests"), () => R.Table()))))))) {
V v_204 = row_203[0];
V v_205 = row_203[1];
var values_206 = R.Join(new V[] {}, new Results((V)true));
var obj_207 = v_184;
V key_208 = R.Call(v_39, R.Join(new V[] {R.Get(v_205, (V)"x")}, new Results(R.Get(v_205, (V)"y")))).First;
R.Set(obj_207, key_208, values_206[0]);
}
if (R.Truth(R.Get(v_172, (V)"elite"))) {
var values_209 = R.Join(new V[] {}, new Results((V)true));
var obj_210 = v_184;
V key_211 = R.Call(v_39, R.Join(new V[] {R.Get(R.Get(v_172, (V)"elite"), (V)"x")}, new Results(R.Get(R.Get(v_172, (V)"elite"), (V)"y")))).First;
R.Set(obj_210, key_211, values_209[0]);
}
var values_212 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))))));
V v_213 = values_212[0];
foreach (var row_214 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_172, (V)"rooms"))))))) {
V v_215 = row_214[0];
V v_216 = row_214[1];
if (R.Truth(R.And((V)(R.Get(v_216, (V)"role") != (V)"start"), () => (V)(R.Get(v_216, (V)"role") != (V)"elite")))) {
var values_217 = R.Join(new V[] {}, new Results(R.Get(R.Or(R.Get(R.Get(v_216, (V)"markers"), (V)"enemy"), () => R.Table()), (V)1.0d)));
V v_218 = values_217[0];
if (R.Truth(v_218)) {
{ double end_221 = R.Num((V)2.0d), step_222 = R.Num((V)1.0d);
for (double i_220 = R.Num((-(V)2.0d)); step_222 > 0 ? i_220 <= end_221 : i_220 >= end_221; i_220 += step_222) {
V v_219 = i_220;
{ double end_225 = R.Num((V)2.0d), step_226 = R.Num((V)1.0d);
for (double i_224 = R.Num((-(V)2.0d)); step_226 > 0 ? i_224 <= end_225 : i_224 >= end_225; i_224 += step_226) {
V v_223 = i_224;
var values_227 = R.Join(new V[] {(V)(R.Get(v_218, (V)"x") + v_223)}, new Results((V)(R.Get(v_218, (V)"y") + v_219)));
V v_228 = values_227[0];
V v_229 = values_227[1];
var values_230 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_6, (V)"distance"), R.Join(new V[] {R.Table(new Entry((V)"x", v_228), new Entry((V)"y", v_229))}, new Results(R.Table(new Entry((V)"x", R.Get(v_172, (V)"start_x")), new Entry((V)"y", R.Get(v_172, (V)"start_y")))))).First >= (V)10.0d)));
V v_231 = values_230[0];
{ double end_234 = R.Num((V)(v_229 + (V)1.0d)), step_235 = R.Num((V)1.0d);
for (double i_233 = R.Num((V)(v_229 - (V)1.0d)); step_235 > 0 ? i_233 <= end_234 : i_233 >= end_234; i_233 += step_235) {
V v_232 = i_233;
{ double end_238 = R.Num((V)(v_228 + (V)1.0d)), step_239 = R.Num((V)1.0d);
for (double i_237 = R.Num((V)(v_228 - (V)1.0d)); step_239 > 0 ? i_237 <= end_238 : i_237 >= end_238; i_237 += step_239) {
V v_236 = i_237;
var values_240 = R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_172, (V)"rows"), v_232), () => R.Get(R.Get(v_4, (V)"definitions"), R.Invoke(R.Get(R.Get(v_172, (V)"rows"), v_232), "sub", R.Join(new V[] {v_236}, new Results(v_236))).First))));
V v_241 = values_240[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_241)), () => (V)(!R.Truth(R.Get(v_241, (V)"walkable")))), () => R.Get(v_184, R.Call(v_39, R.Join(new V[] {v_236}, new Results(v_232))).First)))) {
var values_242 = R.Join(new V[] {}, new Results((V)false));
v_231 = values_242[0];
}
}
}
}
}
if (R.Truth(v_231)) {
var values_243 = R.Join(new V[] {}, new Results(R.Table()));
V v_244 = values_243[0];
{ double end_247 = R.Num((V)(v_182 + (V)1.0d)), step_248 = R.Num((V)1.0d);
for (double i_246 = R.Num((V)1.0d); step_248 > 0 ? i_246 <= end_247 : i_246 >= end_247; i_246 += step_248) {
V v_245 = i_246;
var values_249 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)(v_228 + R.Get(R.Get(v_213, v_245), (V)1.0d))), new Entry((V)"y", (V)(v_229 + R.Get(R.Get(v_213, v_245), (V)2.0d))))));
var obj_250 = v_244;
V key_251 = v_245;
R.Set(obj_250, key_251, values_249[0]);
}
}
if (R.Truth(R.Or((V)(v_178 != (V)"warded_bodyguards"), () => R.Call(R.Get(v_6, (V)"flank_reachable"), R.Join(new V[] {v_172, v_244}, new Results(v_185))).First))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_178), new Entry((V)"points", v_244), new Entry((V)"room_id", R.Get(v_216, (V)"id")))));
}
}
}
}
}
}
}
}
}
return Results.Empty;
})));
R.Set(v_6, (V)"populate", new V((RuleFunction)(args_252 => {
V v_253 = args_252[0];
V v_254 = args_252[1];
V v_255 = args_252[2];
var values_256 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"plan"), R.Join(new V[] {v_253}, new Results(v_254))));
V v_257 = values_256[0];
if (R.Truth((V)(!R.Truth(v_257)))) {
return Results.Empty;
}
var values_258 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"groups"), R.Get(v_257, (V)"kind"))));
V v_259 = values_258[0];
var values_260 = Results.Empty;
V v_261 = values_260[0];
foreach (var row_262 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_257, (V)"points"))))))) {
V v_263 = row_262[0];
V v_264 = row_262[1];
var values_265 = R.Join(new V[] {}, R.Call(v_255, R.Join(new V[] {v_253, R.Concat((V)"core:enemy/", R.Or(R.And((V)(v_263 == (V)1.0d), () => R.Get(v_259, (V)1.0d)), () => R.Get(v_259, (V)2.0d))), R.Get(v_264, (V)"x")}, new Results(R.Get(v_264, (V)"y")))));
V v_266 = values_265[0];
var values_267 = R.Join(new V[] {}, new Results(R.Or(v_261, () => v_266)));
v_261 = values_267[0];
var values_268 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_254, (V)"floor_id"), R.Concat((V)":squad:", R.Get(v_257, (V)"room_id")))));
var obj_269 = v_266;
V key_270 = (V)"squad_id";
R.Set(obj_269, key_270, values_268[0]);
var values_271 = R.Join(new V[] {}, new Results(R.Get(v_257, (V)"kind")));
var obj_272 = v_266;
V key_273 = (V)"squad_kind";
R.Set(obj_272, key_273, values_271[0]);

var values_274 = R.Join(new V[] {}, new Results(R.Get(v_261, (V)"id")));
var obj_275 = v_266;
V key_276 = (V)"squad_leader_id";
R.Set(obj_275, key_276, values_274[0]);
var values_277 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_254, (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(R.Get(v_257, (V)"points"), (V)1.0d), (V)"x")), new Entry((V)"y", R.Get(R.Get(R.Get(v_257, (V)"points"), (V)1.0d), (V)"y")))));
var obj_278 = v_266;
V key_279 = (V)"squad_home";
R.Set(obj_278, key_279, values_277[0]);
}
return Results.Empty;
})));
R.Set(v_6, (V)"validate", new V((RuleFunction)(args_280 => {
V v_281 = args_280[0];
V v_282 = args_280[1];
foreach (var row_283 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_281, (V)"entities"))))))) {
V v_284 = row_283[0];
V v_285 = row_283[1];
if (R.Truth(R.Get(v_285, (V)"squad_id"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.Get(R.Get(v_6, (V)"roles"), R.Get(v_285, (V)"definition_id")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_285, (V)"squad_id")))).First == (V)"string")), () => R.Get(R.Get(v_6, (V)"groups"), R.Get(v_285, (V)"squad_kind"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_285, (V)"squad_leader_id")))).First == (V)"number")), () => (V)((V)(R.Get(v_285, (V)"squad_leader_id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_285, (V)"squad_leader_id") > (V)0.0d)), () => (V)(R.Get(v_285, (V)"squad_leader_id") < R.Get(v_281, (V)"next_entity_id")))}, new Results((V)"invalid saved squad")));
var values_286 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"groups"), R.Get(v_285, (V)"squad_kind"))));
V v_287 = values_286[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_285, (V)"definition_id") == R.Concat((V)"core:enemy/", R.Get(v_287, (V)1.0d))), () => (V)(R.Get(v_285, (V)"definition_id") == R.Concat((V)"core:enemy/", R.Get(v_287, (V)2.0d))))}, new Results((V)"squad role mismatch")));
var values_288 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"entity"), R.Join(new V[] {v_281}, new Results(R.Get(v_285, (V)"squad_leader_id")))));
V v_289 = values_288[0];
if (R.Truth(v_289)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_289, (V)"definition_id") == R.Concat((V)"core:enemy/", R.Get(v_287, (V)1.0d))), () => (V)(R.Get(v_289, (V)"squad_id") == R.Get(v_285, (V)"squad_id"))), () => (V)(R.Get(R.Get(v_289, (V)"position"), (V)"floor_id") == R.Get(R.Get(v_285, (V)"position"), (V)"floor_id")))}, new Results((V)"squad leader mismatch")));
}
var values_290 = R.Join(new V[] {}, new Results(R.Get(v_285, (V)"squad_home")));
V v_291 = values_290[0];
var values_292 = R.Join(new V[] {}, new Results(R.And(v_291, () => R.Get(v_282, R.Get(v_291, (V)"floor_id")))));
V v_293 = values_292[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_293, () => (V)(R.Get(v_291, (V)"floor_id") == R.Get(R.Get(v_285, (V)"position"), (V)"floor_id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_291, (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(v_291, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_291, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_291, (V)"y") % (V)1.0d) == (V)0.0d)), () => R.Get(R.Get(v_293, (V)"rows"), R.Get(v_291, (V)"y"))), () => R.Get(R.Get(v_4, (V)"definitions"), R.Invoke(R.Get(R.Get(v_293, (V)"rows"), R.Get(v_291, (V)"y")), "sub", R.Join(new V[] {R.Get(v_291, (V)"x")}, new Results(R.Get(v_291, (V)"x")))).First)), () => R.Get(R.Get(R.Get(v_4, (V)"definitions"), R.Invoke(R.Get(R.Get(v_293, (V)"rows"), R.Get(v_291, (V)"y")), "sub", R.Join(new V[] {R.Get(v_291, (V)"x")}, new Results(R.Get(v_291, (V)"x")))).First), (V)"walkable"))}, new Results((V)"invalid squad home")));
}
if (R.Truth(R.Get(v_285, (V)"snare_delay"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_285, (V)"snare_delay") == R.Get(v_6, (V)"snare_delay"))}, new Results((V)"invalid snare delay")));
}
if (R.Truth(R.And(R.Get(v_285, (V)"ai_memory"), () => (V)(R.Get(R.Get(v_285, (V)"ai_memory"), (V)"ward_ready") != V.Nil)))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(R.Get(v_6, (V)"roles"), R.Get(v_285, (V)"definition_id")) == (V)"ward_weaver"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_285, (V)"ai_memory"), (V)"ward_ready")))).First == (V)"boolean"))}, new Results((V)"invalid ward state")));
}
if (R.Truth(R.And(R.Get(v_285, (V)"ai_memory"), () => R.Get(R.Get(v_285, (V)"ai_memory"), (V)"shield_dx")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(R.Get(v_6, (V)"roles"), R.Get(v_285, (V)"definition_id")) == (V)"shield_bearer"), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_285, (V)"ai_memory"), (V)"shield_dx")))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_285, (V)"ai_memory"), (V)"shield_dy")))).First) == (V)1.0d))}, new Results((V)"invalid shield facing")));
}
if (R.Truth(R.And(R.Get(v_285, (V)"ai_memory"), () => (V)(R.Get(R.Get(v_285, (V)"ai_memory"), (V)"shield_lowered") != V.Nil)))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(R.Get(v_6, (V)"roles"), R.Get(v_285, (V)"definition_id")) == (V)"shield_bearer"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_285, (V)"ai_memory"), (V)"shield_lowered")))).First == (V)"boolean"))}, new Results((V)"invalid shield phase")));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_281, (V)"enemy_snares") == V.Nil), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_281, (V)"enemy_snares")))).First == (V)"table"))}, new Results((V)"invalid enemy snares")));
var values_294 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_295 = values_294[0];
V v_296 = values_294[1];
foreach (var row_297 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_281, (V)"enemy_snares"), () => R.Table()))))))) {
V v_298 = row_297[0];
V v_299 = row_297[1];
var values_300 = R.Join(new V[] {}, new Results(R.Get(v_282, R.Get(v_299, (V)"floor_id"))));
V v_301 = values_300[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_301, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_299, (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(v_299, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_299, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_299, (V)"y") % (V)1.0d) == (V)0.0d)), () => R.Get(R.Get(v_301, (V)"rows"), R.Get(v_299, (V)"y"))), () => R.Get(R.Get(v_4, (V)"definitions"), R.Invoke(R.Get(R.Get(v_301, (V)"rows"), R.Get(v_299, (V)"y")), "sub", R.Join(new V[] {R.Get(v_299, (V)"x")}, new Results(R.Get(v_299, (V)"x")))).First)), () => R.Get(R.Get(R.Get(v_4, (V)"definitions"), R.Invoke(R.Get(R.Get(v_301, (V)"rows"), R.Get(v_299, (V)"y")), "sub", R.Join(new V[] {R.Get(v_299, (V)"x")}, new Results(R.Get(v_299, (V)"x")))).First), (V)"walkable")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_299, (V)"source_id")))).First == (V)"number"))}, new Results((V)"invalid saved enemy snare")));
var values_302 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"entity"), R.Join(new V[] {v_281}, new Results(R.Get(v_299, (V)"source_id")))));
V v_303 = values_302[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(v_303, () => (V)(R.Get(R.Get(v_6, (V)"roles"), R.Get(v_303, (V)"definition_id")) == (V)"snare_scribe")), () => (V)(R.Get(R.Get(v_303, (V)"position"), (V)"floor_id") == R.Get(v_299, (V)"floor_id")))}, new Results((V)"orphan saved snare")));
var values_304 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_299, (V)"floor_id"), R.Concat((V)":", R.Call(v_39, R.Join(new V[] {R.Get(v_299, (V)"x")}, new Results(R.Get(v_299, (V)"y")))).First))));
V v_305 = values_304[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_296, v_305)))}, new Results((V)"duplicate saved snare")));

var values_306 = R.Join(new V[] {}, new Results((V)true));
var obj_307 = v_296;
V key_308 = v_305;
R.Set(obj_307, key_308, values_306[0]);
var values_309 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_295, R.Get(v_299, (V)"source_id")), () => (V)0.0d) + (V)1.0d)));
var obj_310 = v_295;
V key_311 = R.Get(v_299, (V)"source_id");
R.Set(obj_310, key_311, values_309[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_295, R.Get(v_299, (V)"source_id")) <= R.Get(v_6, (V)"snare_cap"))}, new Results((V)"too many saved snares")));
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
