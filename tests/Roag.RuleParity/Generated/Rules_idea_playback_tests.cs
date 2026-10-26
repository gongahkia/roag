// Static C# port of tests/idea_playback_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 4bf75c61f5c72c97c9738960eff00c02a523d437eb44c8ea6aaf08e6f8816e14
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_playback_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_11 = values_10[0];

var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_13 = values_12[0];
V v_14 = V.Nil;
v_14 = new V((RuleFunction)(args_15 => {
V v_16 = args_15[0];
V v_17 = args_15[1];
V v_18 = args_15[2];
V v_19 = args_15[3];
V v_20 = args_15[4];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_16), new Entry((V)"id", v_17), new Entry((V)"time", R.Or(v_18, () => (V)0.0d)), new Entry((V)"chain_id", R.Or(v_19, () => (V)1.0d)), new Entry((V)"floor_id", R.Or(v_20, () => (V)"floor")), new Entry((V)"source_id", (V)1.0d), new Entry((V)"target_id", (V)2.0d), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d), new Entry((V)"actual_loss", (V)1.0d), new Entry((V)"boon_id", (V)"core:boon/emberbrand"), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))))))));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"mixed causal damage, boon and status cues share one bounded ordered beat"}, new Results(new V((RuleFunction)(args_21 => {

var values_22 = R.Join(new V[] {}, new Results(R.Table()));
V v_23 = values_22[0];

var values_24 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"DamageTaken"), new Entry((V)2, (V)"BoonActivated"), new Entry((V)3, (V)"EffectApplied"), new Entry((V)4, (V)"StatusApplied"), new Entry((V)5, (V)"StatusTick"))));
V v_25 = values_24[0];
{ double end_28 = R.Num((V)100.0d), step_29 = R.Num((V)1.0d);
for (double i_27 = R.Num((V)1.0d); step_29 > 0 ? i_27 <= end_28 : i_27 >= end_28; i_27 += step_29) {
V v_26 = i_27;
var values_30 = R.Join(new V[] {}, R.Call(v_14, R.Join(new V[] {R.Get(v_25, (V)((V)((V)(v_26 - (V)1.0d) % (V)R.Length(v_25)) + (V)1.0d))}, new Results(v_26))));
var obj_31 = v_23;
V key_32 = v_26;
R.Set(obj_31, key_32, values_30[0]);
}
}
var values_33 = R.Join(new V[] {}, new Results(V.Nil));
var obj_34 = R.Get(v_23, (V)3.0d);
V key_35 = (V)"floor_id";
R.Set(obj_34, key_35, values_33[0]);

var values_36 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))));
var obj_37 = R.Get(v_23, (V)3.0d);
V key_38 = (V)"source_position";
R.Set(obj_37, key_38, values_36[0]);
var values_39 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_23))));
V v_40 = values_39[0];

var values_41 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_42 = values_41[0];

R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_42, v_23}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_42, (V)"items")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_42, (V)"items"), (V)1.0d), (V)"duration") == R.Get(v_5, (V)"max_cascade_seconds"))))));
foreach (var row_43 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_42, (V)"items"), (V)1.0d), (V)"events"))))))) {
V v_44 = row_43[0];
V v_45 = row_43[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_45 == R.Get(v_23, v_44)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(R.Get(R.Get(v_42, (V)"items"), (V)1.0d), (V)"events")) == (V)100.0d), () => (V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_23))).First == v_40)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"causal grouping never crosses a simulation timestamp, chain, floor or separate submission"}, new Results(new V((RuleFunction)(args_46 => {

var values_47 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Call(v_14, R.Join(new V[] {(V)"DamageTaken", (V)1.0d, (V)0.0d}, new Results((V)1.0d))).First), new Entry((V)2, R.Call(v_14, R.Join(new V[] {(V)"BoonActivated", (V)2.0d, (V)100.0d}, new Results((V)1.0d))).First), new Entry((V)3, R.Call(v_14, R.Join(new V[] {(V)"StatusTick", (V)3.0d, (V)100.0d}, new Results((V)2.0d))).First), new Entry(4, R.Call(v_14, R.Join(new V[] {(V)"DamageTaken", (V)4.0d, (V)100.0d, (V)2.0d}, new Results((V)"other")))))));
V v_48 = values_47[0];
var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_50 = values_49[0];

R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_50, v_48}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_50, (V)"items")) == (V)4.0d))));
R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_50, R.Table(new Entry(1, R.Call(v_14, R.Join(new V[] {(V)"DamageTaken", (V)5.0d, (V)100.0d, (V)2.0d}, new Results((V)"other")))))}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_50, (V)"items")) == (V)5.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"mixed feedback cannot absorb a locked windup, enemy impact or actor motion"}, new Results(new V((RuleFunction)(args_51 => {

var values_52 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Call(v_14, R.Join(new V[] {(V)"BoonActivated"}, new Results((V)1.0d))).First), new Entry((V)2, R.Call(v_14, R.Join(new V[] {(V)"DamageTaken"}, new Results((V)2.0d))).First), new Entry((V)3, R.Call(v_14, R.Join(new V[] {(V)"WindupStarted"}, new Results((V)3.0d))).First), new Entry((V)4, R.Call(v_14, R.Join(new V[] {(V)"AttackPerformed"}, new Results((V)4.0d))).First), new Entry((V)5, R.Call(v_14, R.Join(new V[] {(V)"DamageTaken"}, new Results((V)5.0d))).First), new Entry(6, R.Call(v_14, R.Join(new V[] {(V)"BoonActivated"}, new Results((V)6.0d)))))));
V v_53 = values_52[0];
var values_54 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_55 = R.Get(v_53, (V)4.0d);
V key_56 = (V)"source_id";
R.Set(obj_55, key_56, values_54[0]);
var values_57 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"Moved"), new Entry((V)"source_id", (V)2.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"from_x", (V)3.0d), new Entry((V)"from_y", (V)2.0d), new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)2.0d))));
var obj_58 = v_53;
V key_59 = (V)7.0d;
R.Set(obj_58, key_59, values_57[0]);
var values_60 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_61 = values_60[0];

R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_61, v_53}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_61, (V)"items")) == (V)5.0d), () => (V)(R.Get(R.Get(R.Get(v_61, (V)"items"), (V)2.0d), (V)"event") == R.Get(v_53, (V)3.0d))), () => (V)(R.Get(R.Get(R.Get(v_61, (V)"items"), (V)3.0d), (V)"event") == R.Get(v_53, (V)4.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(R.Get(R.Get(v_61, (V)"items"), (V)5.0d), (V)"motions"), (V)2.0d), () => (V)(R.Get(R.Get(R.Get(v_61, (V)"items"), (V)5.0d), (V)"duration") == R.Get(v_5, (V)"tile_seconds"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"same-chain simultaneous deaths remain ordered and summed numbers retain every victim"}, new Results(new V((RuleFunction)(args_62 => {

var values_63 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Call(v_14, R.Join(new V[] {(V)"DamageTaken"}, new Results((V)1.0d))).First), new Entry((V)2, R.Call(v_14, R.Join(new V[] {(V)"Died"}, new Results((V)2.0d))).First), new Entry((V)3, R.Call(v_14, R.Join(new V[] {(V)"DamageTaken"}, new Results((V)3.0d))).First), new Entry(4, R.Call(v_14, R.Join(new V[] {(V)"Died"}, new Results((V)4.0d)))))));
V v_64 = values_63[0];
var values_65 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_66 = R.Get(v_64, (V)3.0d);
V key_67 = (V)"target_id";
R.Set(obj_66, key_67, values_65[0]);

var values_68 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_69 = R.Get(v_64, (V)4.0d);
V key_70 = (V)"target_id";
R.Set(obj_69, key_70, values_68[0]);

var values_71 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_72 = R.Get(v_64, (V)3.0d);
V key_73 = (V)"x";
R.Set(obj_72, key_73, values_71[0]);

var values_74 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_75 = R.Get(v_64, (V)4.0d);
V key_76 = (V)"x";
R.Set(obj_75, key_76, values_74[0]);
var values_77 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_78 = R.Get(v_64, (V)1.0d);
V key_79 = (V)"actual_loss";
R.Set(obj_78, key_79, values_77[0]);

var values_80 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_81 = R.Get(v_64, (V)3.0d);
V key_82 = (V)"actual_loss";
R.Set(obj_81, key_82, values_80[0]);

var values_83 = R.Join(new V[] {}, new Results((V)true));
var obj_84 = R.Get(v_64, (V)3.0d);
V key_85 = (V)"critical";
R.Set(obj_84, key_85, values_83[0]);
var values_86 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_87 = values_86[0];

R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_87, v_64}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_87, (V)"items")) == (V)1.0d), () => (V)((V)R.Length(R.Get(R.Get(R.Get(v_87, (V)"items"), (V)1.0d), (V)"events")) == (V)4.0d)))));
var values_88 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.combat_numbers"))));
V v_89 = values_88[0];

var values_90 = R.Join(new V[] {}, R.Call(R.Get(v_89, (V)"new"), Results.Empty));
V v_91 = values_90[0];

R.Call(R.Get(v_89, (V)"update"), R.Join(new V[] {v_91, v_87, (V)0.0d, (V)1.0d}, new Results((V)"floor")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(R.Get(v_91, (V)"items")) == (V)2.0d), () => (V)(R.Get(R.Get(R.Get(v_91, (V)"items"), (V)1.0d), (V)"amount") == (V)3.0d)), () => (V)(R.Get(R.Get(R.Get(v_91, (V)"items"), (V)2.0d), (V)"amount") == (V)5.0d)), () => R.Get(R.Get(R.Get(v_91, (V)"items"), (V)2.0d), (V)"critical")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a real dense new-boon chain keeps all visible events with under two seconds of playback"}, new Results(new V((RuleFunction)(args_92 => {

var values_93 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {(V)37.0d, (V)"core:map/scrolling_room"}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/berserker"))))));
V v_94 = values_93[0];
var values_95 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_94))));
V v_96 = values_95[0];

var values_97 = R.Join(new V[] {(V)15.0d}, new Results((V)16.0d));
var obj_98 = R.Get(v_96, (V)"position");
V key_99 = (V)"x";
var obj_100 = R.Get(v_96, (V)"position");
V key_101 = (V)"y";
R.Set(obj_98, key_99, values_97[0]);
R.Set(obj_100, key_101, values_97[1]);

var values_102 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_103 = v_96;
V key_104 = (V)"hp";
R.Set(obj_103, key_104, values_102[0]);
foreach (var row_105 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"backswing"), new Entry((V)2, (V)"threefold_remedy"), new Entry((V)3, (V)"fourfold_ledger"), new Entry((V)4, (V)"long_wick"), new Entry((V)5, (V)"passing_ember"), new Entry((V)6, (V)"rooks_wake"), new Entry((V)7, (V)"fan_of_blades"), new Entry((V)8, (V)"emberbrand"), new Entry((V)9, (V)"temporal_echo"), new Entry((V)10, (V)"cluster_rupture"), new Entry((V)11, (V)"grave_dividend"), new Entry((V)12, (V)"storm_conductor")))))))) {
V v_106 = row_105[0];
V v_107 = row_105[1];
R.Call(R.Get(v_9, (V)"set_stacks"), R.Join(new V[] {v_96, R.Concat((V)"core:boon/", v_107), (V)"rare"}, new Results((V)2.0d)));
}
{ double end_110 = R.Num((V)2.0d), step_111 = R.Num((V)1.0d);
for (double i_109 = R.Num((-(V)2.0d)); step_111 > 0 ? i_109 <= end_110 : i_109 >= end_110; i_109 += step_111) {
V v_108 = i_109;
{ double end_114 = R.Num((V)2.0d), step_115 = R.Num((V)1.0d);
for (double i_113 = R.Num((-(V)2.0d)); step_115 > 0 ? i_113 <= end_114 : i_113 >= end_114; i_113 += step_115) {
V v_112 = i_113;
if (R.Truth(R.Or((V)(v_112 != (V)0.0d), () => (V)(v_108 != (V)0.0d)))) {
var values_116 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_94, (V)"core:enemy/mossbound_guard", (V)((V)15.0d + v_112), (V)((V)16.0d + v_108)}, new Results((V)100000.0d))));
V v_117 = values_116[0];
var values_118 = R.Join(new V[] {}, new Results((V)1000.0d));
var obj_119 = v_117;
V key_120 = (V)"hp";
R.Set(obj_119, key_120, values_118[0]);

var values_121 = R.Join(new V[] {}, new Results((V)1000.0d));
var obj_122 = v_117;
V key_123 = (V)"max_hp";
R.Set(obj_122, key_123, values_121[0]);
}
}
}
}
}
R.Call(R.Get(v_13, (V)"recompute"), R.Join(new V[] {}, new Results(v_94)));
var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_94}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_125 = values_124[0];
V v_126 = values_124[1];
V v_127 = values_124[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_125}, new Results(v_126)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_127) > (V)500.0d))));

var values_128 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_94))));
V v_129 = values_128[0];
var values_130 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_131 = values_130[0];

R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_131, v_127}, new Results(R.Get(v_94, (V)"player_id"))));
var values_132 = R.Join(new V[] {(V)0.0d, R.Table()}, new Results((V)0.0d));
V v_133 = values_132[0];
V v_134 = values_132[1];
V v_135 = values_132[2];
foreach (var row_136 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_131, (V)"items"))))))) {
V v_137 = row_136[0];
V v_138 = row_136[1];
var values_139 = R.Join(new V[] {}, new Results((V)(v_133 + R.Get(v_138, (V)"duration"))));
v_133 = values_139[0];
foreach (var row_140 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_138, (V)"events"), () => R.Table(new Entry((V)1, R.Get(v_138, (V)"event")))))))))) {
V v_141 = row_140[0];
V v_142 = row_140[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_142, (V)"id") > v_135)}, new Results((V)"displayed events reordered")));

var values_143 = R.Join(new V[] {}, new Results(R.Get(v_142, (V)"id")));
v_135 = values_143[0];

var values_144 = R.Join(new V[] {}, new Results((V)true));
var obj_145 = v_134;
V key_146 = R.Get(v_142, (V)"id");
R.Set(obj_145, key_146, values_144[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(v_133 < (V)2.0d), () => (V)((V)R.Length(R.Get(v_131, (V)"items")) <= (V)5.0d))}, new Results((V)"mixed causal chain created a presentation backlog")));
foreach (var row_147 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_127)))))) {
V v_148 = row_147[0];
V v_149 = row_147[1];
if (R.Truth(R.Get(R.Table(new Entry((V)"DamageTaken", (V)true), new Entry((V)"DamageBlocked", (V)true), new Entry((V)"BoonActivated", (V)true), new Entry((V)"EffectApplied", (V)true), new Entry((V)"StatusApplied", (V)true), new Entry((V)"StatusTick", (V)true), new Entry((V)"DelayedEffectScheduled", (V)true), new Entry((V)"DelayedEffectResolved", (V)true), new Entry((V)"Died", (V)true)), R.Get(v_149, (V)"kind")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_134, R.Get(v_149, (V)"id"))}, new Results(R.Concat(R.Get(v_149, (V)"kind"), (V)" lost"))));
}
}
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_131}, new Results((V)(v_133 + (V)0.01d))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"busy"), R.Join(new V[] {}, new Results(v_131))).First)))));

R.Call(v_3, R.Join(new V[] {v_129}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_94)))));
var values_150 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, new Results(v_129))));
V v_151 = values_150[0];
var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_94}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_153 = values_152[0];
V v_154 = values_152[1];
V v_155 = values_152[2];

var values_156 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_151}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_157 = values_156[0];
V v_158 = values_156[1];
V v_159 = values_156[2];
R.Call(v_3, R.Join(new V[] {v_155}, new Results(v_159)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_94))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_151)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
