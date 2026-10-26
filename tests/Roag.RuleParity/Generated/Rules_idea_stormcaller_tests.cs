// Static C# port of tests/idea_stormcaller_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: ebcc922af80905c60727443e492ec667b3cd2dae613e6d35d9210755797d2cc2
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_stormcaller_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_stormcaller"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_9 = values_8[0];
V v_10 = V.Nil;
v_10 = new V((RuleFunction)(args_11 => {

var values_12 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_7, (V)"id")))))));
V v_13 = values_12[0];

var values_14 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_13))));
V v_15 = values_14[0];

var values_16 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_17 = R.Get(v_15, (V)"position");
V key_18 = (V)"x";
var obj_19 = R.Get(v_15, (V)"position");
V key_20 = (V)"y";
R.Set(obj_17, key_18, values_16[0]);
R.Set(obj_19, key_20, values_16[1]);
var values_21 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_13, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_22 = values_21[0];

return R.Join(new V[] {v_13, v_15}, new Results(v_22));
return Results.Empty;
}));
V v_23 = V.Nil;
v_23 = new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
V v_26 = args_24[1];
{ double end_29 = R.Num(v_26), step_30 = R.Num((V)1.0d);
for (double i_28 = R.Num((V)1.0d); step_30 > 0 ? i_28 <= end_29 : i_28 >= end_29; i_28 += step_30) {
V v_27 = i_28;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_25}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
}
}
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Stormcaller earns at most three charges from actual direct bolt hits and pure previews earn none"}, new Results(new V((RuleFunction)(args_31 => {

var values_32 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_33 = values_32[0];
V v_34 = values_32[1];
V v_35 = values_32[2];

var values_36 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_33))));
V v_37 = values_36[0];
{ double end_40 = R.Num((V)30.0d), step_41 = R.Num((V)1.0d);
for (double i_39 = R.Num((V)1.0d); step_41 > 0 ? i_39 <= end_40 : i_39 >= end_40; i_39 += step_41) {
V v_38 = i_39;
R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_33, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)));

R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_33, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d)));
}
}

R.Call(v_3, R.Join(new V[] {v_37}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_33)))));
R.Call(v_23, R.Join(new V[] {v_33}, new Results((V)4.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_34, (V)"storm_charge") == (V)3.0d), () => (V)(R.Get(v_35, (V)"hp") == (V)6.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_33}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_34, (V)"storm_charge") == (V)3.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"discharge consumes capped charge and resolves one fixed cross on simulation time"}, new Results(new V((RuleFunction)(args_42 => {

var values_43 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_44 = values_43[0];
V v_45 = values_43[1];
V v_46 = values_43[2];

R.Call(v_23, R.Join(new V[] {v_44}, new Results((V)3.0d)));

var values_47 = R.Join(new V[] {}, new Results(R.Get(v_44, (V)"clock")));
V v_48 = values_47[0];
var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_44, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_50 = values_49[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_50, (V)"valid"), () => (V)((V)R.Length(R.Get(v_50, (V)"cells")) == (V)4.0d)))));
var values_51 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_44}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_52 = values_51[0];
V v_53 = values_51[1];
V v_54 = values_51[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_45, (V)"storm_charge") == (V)0.0d), () => (V)(!R.Truth(R.Get(R.Get(v_44, (V)"class_special"), (V)"storm")))), () => (V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_44}, new Results(R.Get(v_46, (V)"id")))).First))), () => (V)(R.Get(v_44, (V)"clock") == (V)(v_48 + (V)150.0d))))));
var values_55 = R.Join(new V[] {}, new Results((V)0.0d));
V v_56 = values_55[0];

foreach (var row_57 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_54)))))) {
V v_58 = row_57[0];
V v_59 = row_57[1];
if (R.Truth((V)(R.Get(v_59, (V)"kind") == (V)"StormDischarged"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_59, (V)"time") == (V)(v_48 + (V)100.0d)), () => (V)(R.Get(v_59, (V)"damage") == (V)8.0d)))));

var values_60 = R.Join(new V[] {}, new Results((V)(v_56 + (V)1.0d)));
v_56 = values_60[0];
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_56 == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"uncharged or obstructed discharges are rejected without spending an action"}, new Results(new V((RuleFunction)(args_61 => {

var values_62 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_63 = values_62[0];
V v_64 = values_62[1];

var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_63))));
V v_66 = values_65[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_63}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_66}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_63)))));
R.Call(v_23, R.Join(new V[] {v_63}, new Results((V)1.0d)));

var values_67 = R.Join(new V[] {(V)8.0d}, new Results((V)4.0d));
var obj_68 = R.Get(v_64, (V)"position");
V key_69 = (V)"x";
var obj_70 = R.Get(v_64, (V)"position");
V key_71 = (V)"y";
R.Set(obj_68, key_69, values_67[0]);
R.Set(obj_70, key_71, values_67[1]);

var values_72 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_63))));
v_66 = values_72[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_63}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_66}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_63)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"grounding Dash spends charge into exactly one positive-damage ward"}, new Results(new V((RuleFunction)(args_73 => {

var values_74 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_75 = values_74[0];
V v_76 = values_74[1];

R.Call(v_23, R.Join(new V[] {v_75}, new Results((V)2.0d)));

var values_77 = R.Join(new V[] {}, new Results(R.Get(v_75, (V)"clock")));
V v_78 = values_77[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_75}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_76, (V)"storm_charge") == (V)0.0d), () => R.Get(v_76, (V)"storm_ward")), () => (V)(R.Get(v_75, (V)"clock") == (V)(v_78 + (V)130.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_7, (V)"absorb"), R.Join(new V[] {v_76}, new Results((V)0.0d))).First)), () => R.Get(v_76, (V)"storm_ward")))));
var values_79 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_75, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)8.0d}, new Results(R.Get(v_75, (V)"clock")))));
V v_80 = values_79[0];
var values_81 = R.Join(new V[] {}, new Results(R.Get(v_76, (V)"hp")));
V v_82 = values_81[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_75}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_75}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_76, (V)"hp") == v_82), () => (V)(!R.Truth(R.Get(v_76, (V)"storm_ward")))))));
{ double end_85 = R.Num((V)4.0d), step_86 = R.Num((V)1.0d);
for (double i_84 = R.Num((V)1.0d); step_86 > 0 ? i_84 <= end_85 : i_84 >= end_85; i_84 += step_86) {
V v_83 = i_84;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_75}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_76, (V)"hp") < v_82))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"secondary effects cannot build charge and delayed discharge cannot trigger extra Echo"}, new Results(new V((RuleFunction)(args_87 => {

var values_88 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_89 = values_88[0];
V v_90 = values_88[1];
V v_91 = values_88[2];

R.Call(v_23, R.Join(new V[] {v_89}, new Results((V)1.0d)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_89, R.Get(v_90, (V)"id"), (V)"core:boon/temporal_echo", (V)"common"}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_89}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_89, (V)"delayed_effects")))).First)), () => (V)(R.Get(v_90, (V)"storm_charge") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_89}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_90, (V)"storm_charge") == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_89}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_89}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_90, (V)"storm_charge") == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a fast Stormcaller retains a locked pending cross and cannot track a moved target"}, new Results(new V((RuleFunction)(args_92 => {

var values_93 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_94 = values_93[0];
V v_95 = values_93[1];
V v_96 = values_93[2];

R.Call(v_23, R.Join(new V[] {v_94}, new Results((V)2.0d)));

var values_97 = R.Join(new V[] {}, new Results((V)300.0d));
var obj_98 = v_95;
V key_99 = (V)"speed";
R.Set(obj_98, key_99, values_97[0]);
var values_100 = R.Join(new V[] {}, new Results(R.Get(v_94, (V)"clock")));
V v_101 = values_100[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_94}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_94, (V)"clock") == (V)(v_101 + (V)50.0d)))));
var values_102 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_94, (V)"class_special"), (V)"storm")))));
V v_103 = values_102[0];

var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"pending_strikes"), R.Join(new V[] {}, new Results(v_94))));
V v_105 = values_104[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_105) == (V)1.0d), () => (V)(R.Get(R.Get(v_105, (V)1.0d), (V)"due") == (V)(v_101 + (V)100.0d))))));
var values_106 = R.Join(new V[] {(V)11.0d}, new Results((V)7.0d));
var obj_107 = R.Get(v_96, (V)"position");
V key_108 = (V)"x";
var obj_109 = R.Get(v_96, (V)"position");
V key_110 = (V)"y";
R.Set(obj_107, key_108, values_106[0]);
R.Set(obj_109, key_110, values_106[1]);

var values_111 = R.Join(new V[] {}, new Results(R.Get(v_96, (V)"hp")));
V v_112 = values_111[0];
{ double end_115 = R.Num((V)3.0d), step_116 = R.Num((V)1.0d);
for (double i_114 = R.Num((V)1.0d); step_116 > 0 ? i_114 <= end_115 : i_114 >= end_115; i_114 += step_116) {
V v_113 = i_114;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_94}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_96, (V)"hp") == v_112), () => (V)(!R.Truth(R.Get(R.Get(v_94, (V)"class_special"), (V)"storm")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pending storm and held ward roundtrip with exact effects and forged resources reject"}, new Results(new V((RuleFunction)(args_117 => {

var values_118 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_119 = values_118[0];
V v_120 = values_118[1];

R.Call(v_23, R.Join(new V[] {v_119}, new Results((V)2.0d)));

var values_121 = R.Join(new V[] {}, new Results((V)300.0d));
var obj_122 = v_120;
V key_123 = (V)"speed";
R.Set(obj_122, key_123, values_121[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_119}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_119))));
V v_125 = values_124[0];

var values_126 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, new Results(v_125))))))));
V v_127 = values_126[0];
{ double end_130 = R.Num((V)4.0d), step_131 = R.Num((V)1.0d);
for (double i_129 = R.Num((V)1.0d); step_131 > 0 ? i_129 <= end_130 : i_129 >= end_130; i_129 += step_131) {
V v_128 = i_129;
var values_132 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_119}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_133 = values_132[0];
V v_134 = values_132[1];
V v_135 = values_132[2];

var values_136 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_127}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_137 = values_136[0];
V v_138 = values_136[1];
V v_139 = values_136[2];

R.Call(v_3, R.Join(new V[] {v_135}, new Results(v_139)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_119))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_127)))));
}
}
foreach (var row_159 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_140 => {
V v_141 = args_140[0];
foreach (var row_142 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_141, (V)"entities"))))))) {
V v_143 = row_142[0];
V v_144 = row_142[1];
if (R.Truth((V)(R.Get(v_144, (V)"id") == R.Get(v_141, (V)"player_id")))) {
var values_145 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_146 = v_144;
V key_147 = (V)"storm_charge";
R.Set(obj_146, key_147, values_145[0]);
}
}
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_148 => {
V v_149 = args_148[0];
var values_150 = R.Join(new V[] {}, new Results((-(V)1.0d)));
var obj_151 = R.Get(R.Get(v_149, (V)"class_special"), (V)"storm");
V key_152 = (V)"damage";
R.Set(obj_151, key_152, values_150[0]);
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_153 => {
V v_154 = args_153[0];
{ double end_157 = R.Num((V)1.0d), step_158 = R.Num((-(V)1.0d));
for (double i_156 = R.Num((V)R.Length(R.Get(v_154, (V)"schedule"))); step_158 > 0 ? i_156 <= end_157 : i_156 >= end_157; i_156 += step_158) {
V v_155 = i_156;
if (R.Truth(R.Get(R.Get(R.Get(v_154, (V)"schedule"), v_155), (V)"idea_storm"))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_154, (V)"schedule")}, new Results(v_155)));
}
}
}
return Results.Empty;
})))))))))) {
V v_160 = row_159[0];
V v_161 = row_159[1];
var values_162 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, new Results(v_125))))));
V v_163 = values_162[0];

R.Call(v_161, R.Join(new V[] {}, new Results(v_163)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_163))).First)))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
