// Static C# port of tests/idea_revival_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 5a677500e0b0e43e38b47bc7b6513b8ebf899543305377e355d602a1725fb35a
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_revival_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_11 = values_10[0];
V v_12 = V.Nil;
v_12 = new V((RuleFunction)(args_13 => {

var values_14 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_15 = values_14[0];

var values_16 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_15))));
V v_17 = values_16[0];

var values_18 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_19 = R.Get(v_17, (V)"position");
V key_20 = (V)"x";
var obj_21 = R.Get(v_17, (V)"position");
V key_22 = (V)"y";
R.Set(obj_19, key_20, values_18[0]);
R.Set(obj_21, key_22, values_18[1]);
var values_23 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_15, (V)"core:enemy/grave_cantor", (V)10.0d, (V)6.0d}, new Results((V)200.0d))));
V v_24 = values_23[0];
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_15, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_26 = values_25[0];

var values_27 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_28 = v_26;
V key_29 = (V)"hp";
R.Set(obj_28, key_29, values_27[0]);

var values_30 = R.Join(new V[] {}, new Results((V)6.0d));
var obj_31 = v_26;
V key_32 = (V)"max_hp";
R.Set(obj_31, key_32, values_30[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_15}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
return R.Join(new V[] {v_15, v_17, v_24}, new Results(R.Get(v_26, (V)"id")));
return Results.Empty;
}));
V v_33 = V.Nil;
v_33 = new V((RuleFunction)(args_34 => {
V v_35 = args_34[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_35}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_35}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_35, (V)"clock") == (V)300.0d))));
return Results.Empty;
}));
V v_36 = V.Nil;
v_36 = new V((RuleFunction)(args_37 => {
V v_38 = args_37[0];
foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_38, (V)"entities"))))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
if (R.Truth(R.Get(v_41, (V)"revival_of"))) {
return R.Join(new V[] {}, new Results(v_41));
}
}
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Cantor records an eligible corpse and commits one pure locked revival"}, new Results(new V((RuleFunction)(args_42 => {

var values_43 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_44 = values_43[0];
V v_45 = values_43[1];
V v_46 = values_43[2];
V v_47 = values_43[3];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_44, (V)"idea_corpses")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_44, (V)"idea_corpses"), (V)1.0d), (V)"id") == v_47)), () => (V)(R.Get(R.Get(v_44, (V)"progression"), (V)"xp_total") == (V)5.0d)))));
var values_48 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_44))));
V v_49 = values_48[0];

var values_50 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"choose"), R.Join(new V[] {v_44}, new Results(v_46))));
V v_51 = values_50[0];
R.Call(v_3, R.Join(new V[] {v_49}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_44)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_51, (V)"support"), (V)"corpse_id") == v_47))));
R.Call(v_33, R.Join(new V[] {}, new Results(v_44)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_46, (V)"ai_memory"), (V)"revival_spent"), () => (V)(R.Get(R.Get(v_46, (V)"pending_attack"), (V)"due") == (V)388.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_44, (V)"idea_corpses"), (V)1.0d), (V)"claimed_by") == R.Get(v_46, (V)"id")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_44}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

var values_52 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results(v_44))))));
V v_53 = values_52[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_53, (V)"hp") == (V)3.0d), () => (V)(R.Get(v_53, (V)"max_hp") == (V)3.0d)), () => (V)(R.Get(v_53, (V)"revival_of") == v_47)), () => R.Get(v_53, (V)"reward_exempt")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_44, (V)"idea_corpses")) == (V)0.0d), () => (V)(!R.Truth(R.Get(v_46, (V)"pending_attack")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"revived units never duplicate XP, currency or supplies and cannot revive again"}, new Results(new V((RuleFunction)(args_54 => {

var values_55 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_56 = values_55[0];
V v_57 = values_55[1];
V v_58 = values_55[2];

R.Call(v_33, R.Join(new V[] {}, new Results(v_56)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_56}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_59 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results(v_56))))));
V v_60 = values_59[0];

var values_61 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_56, (V)"progression"), (V)"xp_total")));
V v_62 = values_61[0];

var values_63 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_56, (V)"drops"))));
V v_64 = values_63[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_56}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_56}, new Results(R.Get(v_60, (V)"id")))).First)), () => (V)(R.Get(R.Get(v_56, (V)"progression"), (V)"xp_total") == v_62)), () => (V)((V)R.Length(R.Get(v_56, (V)"drops")) == v_64)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_56, (V)"idea_corpses")) == (V)0.0d), () => R.Get(R.Get(v_56, (V)"rewarded_kills"), R.Get(v_60, (V)"id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_7, (V)"choose"), R.Join(new V[] {v_56}, new Results(v_58))).First, (V)"support"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"occupying a corpse cancels the only attempt without spawning into the player"}, new Results(new V((RuleFunction)(args_65 => {

var values_66 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_67 = values_66[0];
V v_68 = values_66[1];
V v_69 = values_66[2];

R.Call(v_33, R.Join(new V[] {}, new Results(v_67)));
var values_70 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_67}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_71 = values_70[0];
V v_72 = values_70[1];
V v_73 = values_70[2];
var values_74 = R.Join(new V[] {}, new Results((V)false));
V v_75 = values_74[0];

foreach (var row_76 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_73)))))) {
V v_77 = row_76[0];
V v_78 = row_76[1];
if (R.Truth((V)(R.Get(v_78, (V)"kind") == (V)"RevivalBlocked"))) {
var values_79 = R.Join(new V[] {}, new Results((V)true));
v_75 = values_79[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(v_75, () => (V)(!R.Truth(R.Call(v_36, R.Join(new V[] {}, new Results(v_67))).First))), () => (V)(R.Get(R.Get(v_68, (V)"position"), (V)"x") == (V)8.0d)), () => (V)((V)R.Length(R.Get(v_67, (V)"idea_corpses")) == (V)0.0d)), () => R.Get(R.Get(v_69, (V)"ai_memory"), (V)"revival_spent")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stun cancels a committed revival permanently and clears its corpse marker"}, new Results(new V((RuleFunction)(args_80 => {

var values_81 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_82 = values_81[0];
V v_83 = values_81[1];
V v_84 = values_81[2];

R.Call(v_33, R.Join(new V[] {}, new Results(v_82)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_82, R.Get(v_84, (V)"id")}, new Results((V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(v_84, (V)"pending_attack"))), () => R.Get(R.Get(v_84, (V)"ai_memory"), (V)"revival_spent")), () => (V)((V)R.Length(R.Get(v_82, (V)"idea_corpses")) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_82}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(v_36, R.Join(new V[] {}, new Results(v_82))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a primary batch killing caster and ally cannot create a resurrection afterlife"}, new Results(new V((RuleFunction)(args_85 => {

var values_86 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_87 = values_86[0];

var values_88 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_87))));
V v_89 = values_88[0];

var values_90 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_91 = R.Get(v_89, (V)"position");
V key_92 = (V)"x";
var obj_93 = R.Get(v_89, (V)"position");
V key_94 = (V)"y";
R.Set(obj_91, key_92, values_90[0]);
R.Set(obj_93, key_94, values_90[1]);
var values_95 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_87, (V)"core:enemy/grave_cantor", (V)8.0d, (V)5.0d}, new Results((V)200.0d))));
V v_96 = values_95[0];

var values_97 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_98 = v_96;
V key_99 = (V)"hp";
R.Set(obj_98, key_99, values_97[0]);
var values_100 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_87, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_101 = values_100[0];

var values_102 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_103 = v_101;
V key_104 = (V)"hp";
R.Set(obj_103, key_104, values_102[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_87}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_87}, new Results(R.Get(v_96, (V)"id")))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_87}, new Results(R.Get(v_101, (V)"id")))).First))), () => (V)(!R.Truth(R.Call(v_36, R.Join(new V[] {}, new Results(v_87))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(!R.Truth(R.Get(v_87, (V)"idea_corpses"))), () => (V)((V)R.Length(R.Get(v_87, (V)"idea_corpses")) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"corpse claims and revival budget survive save with exact subsequent rewards and RNG"}, new Results(new V((RuleFunction)(args_105 => {

var values_106 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_107 = values_106[0];

R.Call(v_33, R.Join(new V[] {}, new Results(v_107)));
var values_108 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_107))));
V v_109 = values_108[0];

var values_110 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, new Results(v_109))))))));
V v_111 = values_110[0];
var values_112 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_107}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_113 = values_112[0];
V v_114 = values_112[1];
V v_115 = values_112[2];

var values_116 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_111}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_117 = values_116[0];
V v_118 = values_116[1];
V v_119 = values_116[2];
R.Call(v_3, R.Join(new V[] {v_115}, new Results(v_119)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_107))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_111)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_107}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_111}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_107))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_111)))));
var values_120 = R.Join(new V[] {}, new Results((V)999999.0d));
var obj_121 = R.Get(R.Get(v_109, (V)"idea_corpses"), (V)1.0d);
V key_122 = (V)"claimed_by";
R.Set(obj_121, key_122, values_120[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_109))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"corpse storage is bounded per floor and ignores elites, casters and revived bodies"}, new Results(new V((RuleFunction)(args_123 => {

var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_125 = values_124[0];
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_125, (V)"core:enemy/grave_cantor", (V)10.0d, (V)6.0d}, new Results((V)100000.0d)));
var values_126 = R.Join(new V[] {}, new Results(R.Get(v_125, (V)"active_floor_id")));
V v_127 = values_126[0];
{ double end_130 = R.Num((V)199.0d), step_131 = R.Num((V)1.0d);
for (double i_129 = R.Num((V)100.0d); step_131 > 0 ? i_129 <= end_130 : i_129 >= end_130; i_129 += step_131) {
V v_128 = i_129;
R.Call(R.Get(v_11, (V)"remember_corpse"), R.Join(new V[] {v_125}, new Results(R.Table(new Entry((V)"id", v_128), new Entry((V)"controller", (V)"ai"), new Entry((V)"ai_id", (V)"guard"), new Entry((V)"definition_id", (V)"core:enemy/mossbound_guard"), new Entry((V)"max_hp", (V)10.0d), new Entry((V)"position", R.Table(new Entry((V)"floor_id", v_127), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d)))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_125, (V)"idea_corpses")) == (V)16.0d))));
var values_132 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_125))));
V v_133 = values_132[0];
foreach (var row_134 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"biome_elite"), new Entry((V)2, (V)"core:enemy/thorn_warden"))), new Entry((V)2, R.Table(new Entry((V)1, (V)"idea_regular"), new Entry((V)2, (V)"core:enemy/grave_cantor")))))))))) {
V v_135 = row_134[0];
V v_136 = row_134[1];
R.Call(R.Get(v_11, (V)"remember_corpse"), R.Join(new V[] {v_125}, new Results(R.Table(new Entry((V)"id", (V)200.0d), new Entry((V)"controller", (V)"ai"), new Entry((V)"ai_id", R.Get(v_136, (V)1.0d)), new Entry((V)"definition_id", R.Get(v_136, (V)2.0d)), new Entry((V)"max_hp", (V)10.0d), new Entry((V)"position", R.Table(new Entry((V)"floor_id", v_127), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d)))))));
}
R.Call(R.Get(v_11, (V)"remember_corpse"), R.Join(new V[] {v_125}, new Results(R.Table(new Entry((V)"id", (V)201.0d), new Entry((V)"controller", (V)"ai"), new Entry((V)"ai_id", (V)"guard"), new Entry((V)"reward_exempt", (V)true), new Entry((V)"definition_id", (V)"core:enemy/mossbound_guard"), new Entry((V)"max_hp", (V)10.0d), new Entry((V)"position", R.Table(new Entry((V)"floor_id", v_127), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d)))))));
R.Call(v_3, R.Join(new V[] {v_133}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_125)))));
{ double end_139 = R.Num((V)3.0d), step_140 = R.Num((V)1.0d);
for (double i_138 = R.Num((V)2.0d); step_140 > 0 ? i_138 <= end_139 : i_138 >= end_139; i_138 += step_140) {
V v_137 = i_138;
var values_141 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_125, (V)"core:enemy/grave_cantor", (V)11.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_142 = values_141[0];
var values_143 = R.Join(new V[] {}, new Results(R.Concat((V)"test:frozen/", v_137)));
var obj_144 = R.Get(v_142, (V)"position");
V key_145 = (V)"floor_id";
R.Set(obj_144, key_145, values_143[0]);
{ double end_148 = R.Num((V)20.0d), step_149 = R.Num((V)1.0d);
for (double i_147 = R.Num((V)1.0d); step_149 > 0 ? i_147 <= end_148 : i_147 >= end_148; i_147 += step_149) {
V v_146 = i_147;
R.Call(R.Get(v_11, (V)"remember_corpse"), R.Join(new V[] {v_125}, new Results(R.Table(new Entry((V)"id", (V)((V)((V)300.0d + (V)(v_137 * (V)100.0d)) + v_146)), new Entry((V)"controller", (V)"ai"), new Entry((V)"ai_id", (V)"guard"), new Entry((V)"definition_id", (V)"core:enemy/mossbound_guard"), new Entry((V)"max_hp", (V)10.0d), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_142, (V)"position"), (V)"floor_id")), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d)))))));
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_125, (V)"idea_corpses")) == (V)32.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"natural generation includes Cantors without repopulating loaded floors"}, new Results(new V((RuleFunction)(args_150 => {

var values_151 = R.Join(new V[] {}, new Results((V)0.0d));
V v_152 = values_151[0];
var values_153 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_154 = values_153[0];
foreach (var row_155 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_154, (V)"order"))))))) {
V v_156 = row_155[0];
V v_157 = row_155[1];
{ double end_160 = R.Num((V)30.0d), step_161 = R.Num((V)1.0d);
for (double i_159 = R.Num((V)1.0d); step_161 > 0 ? i_159 <= end_160 : i_159 >= end_160; i_159 += step_161) {
V v_158 = i_159;
var values_162 = R.Join(new V[] {}, new Results((V)(v_157 == R.Get(R.Get(v_154, (V)"order"), (V)1.0d))));
V v_163 = values_162[0];
var values_164 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_158}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"stage_options", R.Table(new Entry((V)"biome_id", v_157), new Entry((V)"index", R.Or(R.And(v_163, () => (V)1.0d), () => (V)2.0d)), new Entry((V)"route_history", R.Or(R.And(v_163, () => R.Table()), () => R.Table(new Entry(1, new Results(R.Invoke(v_157, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First))))))))))));
V v_165 = values_164[0];
foreach (var row_166 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_165, (V)"entities"))))))) {
V v_167 = row_166[0];
V v_168 = row_166[1];
if (R.Truth((V)(R.Get(v_168, (V)"definition_id") == (V)"core:enemy/grave_cantor"))) {
var values_169 = R.Join(new V[] {}, new Results((V)(v_152 + (V)1.0d)));
v_152 = values_169[0];
var values_170 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(R.Get(v_168, (V)"position"), (V)"floor_id") == R.Get(v_165, (V)"active_floor_id")), () => R.Get(v_165, (V)"generated_map")), () => R.Get(R.Get(v_165, (V)"inactive_floor_maps"), R.Get(R.Get(v_168, (V)"position"), (V)"floor_id")))));
V v_171 = values_170[0];
var values_172 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"revival_company"), R.Join(new V[] {v_165, v_171}, new Results(v_168))));
V v_173 = values_172[0];
V v_174 = values_172[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_173, () => (V)(!R.Truth(v_174)))}, new Results((V)"natural Cantor lacks an eligible ally in clear revival range")));
foreach (var row_175 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_165, (V)"entities"))))))) {
V v_176 = row_175[0];
V v_177 = row_175[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_177, (V)"controller") == (V)"ai"), () => R.Call(R.Get(v_11, (V)"revivable"), R.Join(new V[] {}, new Results(v_177))).First), () => (V)(R.Get(R.Get(v_177, (V)"position"), (V)"floor_id") == R.Get(R.Get(v_168, (V)"position"), (V)"floor_id"))))) {
var values_178 = R.Join(new V[] {}, new Results(R.Get(v_177, (V)"position")));
V v_179 = values_178[0];
foreach (var row_180 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_171, (V)"placements"), (V)"stairs"), () => R.Table()))))))) {
V v_181 = row_180[0];
V v_182 = row_180[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_179, (V)"x") != R.Get(v_182, (V)"x")), () => (V)(R.Get(v_179, (V)"y") != R.Get(v_182, (V)"y")))}, new Results((V)"companion occupies stairs")));
}
}
}
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_165))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_165)))))))))))));
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_152 > (V)0.0d)}, new Results((V)"natural revival encounter never generated")));
var values_183 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_184 = values_183[0];
foreach (var row_185 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_186 = row_185[0];
V v_187 = row_185[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_184, (V)"packs"), v_187), (V)"missing")) == (V)0.0d))));
var values_188 = R.Join(new V[] {(V)false}, new Results((V)false));
V v_189 = values_188[0];
V v_190 = values_188[1];
foreach (var row_191 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_184, (V)"packs"), v_187), (V)"bindings"))))))) {
V v_192 = row_191[0];
V v_193 = row_191[1];
if (R.Truth((V)(R.Get(v_193, (V)"id") == (V)"core:enemy/grave_cantor"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_193, (V)"index") == (V)318.0d))));

var values_194 = R.Join(new V[] {}, new Results((V)true));
v_189 = values_194[0];
} else if (R.Truth((V)(R.Get(v_193, (V)"id") == (V)"core:ui/idea_corpse_effect"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_193, (V)"index") == (V)539.0d))));

var values_195 = R.Join(new V[] {}, new Results((V)true));
v_190 = values_195[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_189, () => v_190))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Cantor companion planning is pure, reuses allies and cannot occupy reserved geometry"}, new Results(new V((RuleFunction)(args_196 => {

var values_197 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_198 = values_197[0];
var values_199 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_198, (V)"core:enemy/grave_cantor", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_200 = values_199[0];
var values_201 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"map_for"), R.Join(new V[] {}, new Results(v_198))));
V v_202 = values_201[0];
var values_203 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_198))));
V v_204 = values_203[0];
var values_205 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"revival_company"), R.Join(new V[] {v_198, v_202}, new Results(v_200))));
V v_206 = values_205[0];
V v_207 = values_205[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_206, () => v_207))));

R.Call(v_3, R.Join(new V[] {v_204}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_198)))));
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_198, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)6.0d}, new Results((V)100000.0d)));
var values_208 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"revival_company"), R.Join(new V[] {v_198, v_202}, new Results(v_200))));
v_206 = values_208[0];
v_207 = values_208[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_206, () => (V)(!R.Truth(v_207)))}, new Results((V)"existing ally should avoid an extra spawn")));
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {}, new Results(R.Get(v_198, (V)"entities"))));
var values_209 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, new Results(v_202))))));
v_202 = values_209[0];

var values_210 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"stairs", R.Table()))));
var obj_211 = v_202;
V key_212 = (V)"placements";
R.Set(obj_211, key_212, values_210[0]);
{ double end_215 = R.Num(R.Get(v_202, (V)"height")), step_216 = R.Num((V)1.0d);
for (double i_214 = R.Num((V)1.0d); step_216 > 0 ? i_214 <= end_215 : i_214 >= end_215; i_214 += step_216) {
V v_213 = i_214;
var values_217 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"#"}, new Results(R.Get(v_202, (V)"width")))));
var obj_218 = R.Get(v_202, (V)"rows");
V key_219 = v_213;
R.Set(obj_218, key_219, values_217[0]);
}
}
var values_220 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)10.0d), new Entry((V)2, (V)5.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)11.0d), new Entry((V)2, (V)6.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)10.0d), new Entry((V)2, (V)7.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)9.0d), new Entry((V)2, (V)6.0d))))));
V v_221 = values_220[0];
foreach (var row_222 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_221)))))) {
V v_223 = row_222[0];
V v_224 = row_222[1];
var values_225 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_202, (V)"rows"), R.Get(v_224, (V)2.0d))));
V v_226 = values_225[0];

var values_227 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_226, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_224, (V)1.0d) - (V)1.0d)))).First, R.Concat((V)".", R.Invoke(v_226, "sub", R.Join(new V[] {}, new Results((V)(R.Get(v_224, (V)1.0d) + (V)1.0d)))).First))));
var obj_228 = R.Get(v_202, (V)"rows");
V key_229 = R.Get(v_224, (V)2.0d);
R.Set(obj_228, key_229, values_227[0]);
var values_230 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_224, (V)1.0d)), new Entry((V)"y", R.Get(v_224, (V)2.0d)))));
var obj_231 = R.Get(R.Get(v_202, (V)"placements"), (V)"stairs");
V key_232 = (V)((V)R.Length(R.Get(R.Get(v_202, (V)"placements"), (V)"stairs")) + (V)1.0d);
R.Set(obj_231, key_232, values_230[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(R.Get(v_11, (V)"revival_company"), R.Join(new V[] {v_198, v_202}, new Results(v_200))).First))}, new Results((V)"stairs must remain free")));
var values_233 = R.Join(new V[] {}, new Results(R.Table()));
var obj_234 = R.Get(v_202, (V)"placements");
V key_235 = (V)"stairs";
R.Set(obj_234, key_235, values_233[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_11, (V)"revival_company"), R.Join(new V[] {v_198, v_202}, new Results(v_200))).First}, new Results((V)"ordinary adjacent floor should fit")));
{ double end_238 = R.Num(R.Get(v_202, (V)"height")), step_239 = R.Num((V)1.0d);
for (double i_237 = R.Num((V)1.0d); step_239 > 0 ? i_237 <= end_238 : i_237 >= end_238; i_237 += step_239) {
V v_236 = i_237;
var values_240 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"#"}, new Results(R.Get(v_202, (V)"width")))));
var obj_241 = R.Get(v_202, (V)"rows");
V key_242 = v_236;
R.Set(obj_241, key_242, values_240[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(R.Get(v_11, (V)"revival_company"), R.Join(new V[] {v_198, v_202}, new Results(v_200))).First))}, new Results((V)"never carve a companion into walls")));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
