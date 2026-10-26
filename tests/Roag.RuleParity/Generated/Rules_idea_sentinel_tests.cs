// Static C# port of tests/idea_sentinel_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: d9ce63e60ed8fdd80fa7c126af09a0d16c4001b92daa2225c38ca0d701fcf1ef
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_sentinel_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_sentinel"))));
V v_9 = values_8[0];

var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
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
var values_23 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_15, R.Get(v_9, (V)"id"), (V)8.0d, (V)6.0d}, new Results((V)50.0d))));
V v_24 = values_23[0];
return R.Join(new V[] {v_15, v_17}, new Results(v_24));
return Results.Empty;
}));
V v_25 = V.Nil;
v_25 = new V((RuleFunction)(args_26 => {
V v_27 = args_26[0];
V v_28 = args_26[1];
var values_29 = R.Join(new V[] {}, new Results(R.Table()));
V v_30 = values_29[0];
{ double end_33 = R.Num(v_28), step_34 = R.Num((V)1.0d);
for (double i_32 = R.Num((V)1.0d); step_34 > 0 ? i_32 <= end_33 : i_32 >= end_33; i_32 += step_34) {
V v_31 = i_32;
var values_35 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_27}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_36 = values_35[0];
V v_37 = values_35[1];
V v_38 = values_35[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_36)));
foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_38)))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
var values_42 = R.Join(new V[] {}, new Results(v_41));
var obj_43 = v_30;
V key_44 = (V)((V)R.Length(v_30) + (V)1.0d);
R.Set(obj_43, key_44, values_42[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_30));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Sentinel preview is pure and its first quarter remains locked after the player leaves"}, new Results(new V((RuleFunction)(args_45 => {

var values_46 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_47 = values_46[0];
V v_48 = values_46[1];
V v_49 = values_46[2];

var values_50 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_47))));
V v_51 = values_50[0];
{ double end_54 = R.Num((V)30.0d), step_55 = R.Num((V)1.0d);
for (double i_53 = R.Num((V)1.0d); step_55 > 0 ? i_53 <= end_54 : i_53 >= end_54; i_53 += step_55) {
V v_52 = i_53;
var values_56 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"choose"), R.Join(new V[] {v_47}, new Results(v_49))));
V v_57 = values_56[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_57, (V)"sentinel_facing") == (V)4.0d), () => (V)((V)R.Length(R.Get(v_57, (V)"cells")) == (V)3.0d)))));
}
}
R.Call(v_3, R.Join(new V[] {v_51}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_47)))));

R.Call(v_25, R.Join(new V[] {v_47}, new Results((V)1.0d)));
var values_58 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_49, (V)"pending_attack"), (V)"target_cells")))));
V v_59 = values_58[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_49, (V)"pending_attack"), (V)"due") == (V)250.0d), () => (V)(R.Get(R.Get(v_49, (V)"ai_memory"), (V)"sentinel_strikes") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_47}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_49, (V)"pending_attack"), (V)"target_cells")))).First == v_59))));
R.Call(v_25, R.Join(new V[] {v_47}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_48, (V)"hp") == (V)24.0d), () => (V)(R.Get(R.Get(v_49, (V)"position"), (V)"x") == (V)8.0d)), () => (V)(R.Get(v_47, (V)"clock") == (V)300.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"four finite quarters advance clockwise with independent strict windups then ordinary melee"}, new Results(new V((RuleFunction)(args_60 => {

var values_61 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_62 = values_61[0];
V v_63 = values_61[1];
V v_64 = values_61[2];

var values_65 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_62, (V)"rng"), (V)"state")));
V v_66 = values_65[0];
var values_67 = R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {v_62}, new Results((V)14.0d))));
V v_68 = values_67[0];

var values_69 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_70 = values_69[0];
V v_71 = values_69[1];
foreach (var row_72 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_68)))))) {
V v_73 = row_72[0];
V v_74 = row_72[1];
if (R.Truth(R.And((V)(R.Get(v_74, (V)"kind") == (V)"WindupStarted"), () => (V)(R.Get(v_74, (V)"ability_id") == (V)"core:ability/sentinel_quarter")))) {
var values_75 = R.Join(new V[] {}, new Results(v_74));
var obj_76 = v_70;
V key_77 = (V)((V)R.Length(v_70) + (V)1.0d);
R.Set(obj_76, key_77, values_75[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_74, (V)"time") == (V)((V)50.0d + (V)((V)((V)R.Length(v_70) - (V)1.0d) * (V)300.0d))))));
} else if (R.Truth(R.And((V)(R.Get(v_74, (V)"kind") == (V)"AttackPerformed"), () => (V)(R.Get(v_74, (V)"ability_id") == (V)"core:ability/sentinel_quarter")))) {
var values_78 = R.Join(new V[] {}, new Results(v_74));
var obj_79 = v_71;
V key_80 = (V)((V)R.Length(v_71) + (V)1.0d);
R.Set(obj_79, key_80, values_78[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_74, (V)"time") == (V)((V)250.0d + (V)((V)((V)R.Length(v_71) - (V)1.0d) * (V)300.0d))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)((V)R.Length(v_70) == (V)4.0d), () => (V)((V)R.Length(v_71) == (V)4.0d)), () => R.Get(R.Get(v_64, (V)"ai_memory"), (V)"sentinel_finished")), () => (V)(R.Get(R.Get(v_64, (V)"ai_memory"), (V)"sentinel_strikes") == (V)4.0d)), () => (V)(R.Get(R.Get(v_62, (V)"rng"), (V)"state") == v_66)))));
foreach (var row_81 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_70)))))) {
V v_82 = row_81[0];
V v_83 = row_81[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_83, (V)"sentinel_facing") == R.Get(R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, (V)3.0d)), v_82)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_64, (V)"pending_attack"), () => (V)(R.Get(R.Get(v_64, (V)"pending_attack"), (V)"ability_id") == (V)"core:ability/sentinel_touch")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"the locked arc keeps friendly fire and commits all three HP outcomes in the same batch"}, new Results(new V((RuleFunction)(args_84 => {

var values_85 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_86 = values_85[0];
V v_87 = values_85[1];
V v_88 = values_85[2];
var values_89 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_86, (V)"core:enemy/mossbound_guard", (V)7.0d, (V)5.0d}, new Results((V)100000.0d))));
V v_90 = values_89[0];
var values_91 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_86, (V)"core:enemy/mossbound_guard", (V)7.0d, (V)7.0d}, new Results((V)100000.0d))));
V v_92 = values_91[0];
var values_93 = R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {v_86}, new Results((V)3.0d))));
V v_94 = values_93[0];

var values_95 = Results.Empty;
V v_96 = values_95[0];
foreach (var row_97 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_94)))))) {
V v_98 = row_97[0];
V v_99 = row_97[1];
if (R.Truth(R.And((V)(R.Get(v_99, (V)"kind") == (V)"DamageBatchResolved"), () => (V)(R.Get(v_99, (V)"source_id") == R.Get(v_88, (V)"id"))))) {
var values_100 = R.Join(new V[] {}, new Results(v_99));
v_96 = values_100[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(v_96, () => (V)((V)R.Length(R.Get(v_96, (V)"outcomes")) == (V)3.0d)), () => (V)(R.Get(v_90, (V)"hp") == (V)9.0d)), () => (V)(R.Get(v_92, (V)"hp") == (V)9.0d)), () => (V)(R.Get(v_87, (V)"hp") == (V)22.0d)))));
foreach (var row_101 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_96, (V)"outcomes"))))))) {
V v_102 = row_101[0];
V v_103 = row_101[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_103, (V)"actual_loss") == R.Or(R.And((V)(R.Get(v_103, (V)"target_id") == R.Get(v_87, (V)"id")), () => (V)2.0d), () => (V)1.0d)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ordinary damage does not cancel or rotate an already committed quarter"}, new Results(new V((RuleFunction)(args_104 => {

var values_105 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_106 = values_105[0];
V v_107 = values_105[1];
V v_108 = values_105[2];

R.Call(v_25, R.Join(new V[] {v_106}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_106}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_108, (V)"hp") == (V)11.0d), () => (V)(R.Get(R.Get(v_108, (V)"pending_attack"), (V)"due") == (V)250.0d)), () => (V)(R.Get(R.Get(v_108, (V)"pending_attack"), (V)"sentinel_facing") == (V)4.0d)))));
R.Call(v_25, R.Join(new V[] {v_106}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_107, (V)"hp") == (V)22.0d), () => (V)(!R.Truth(R.Get(v_108, (V)"pending_attack")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stun interrupts both a pending quarter and the recovery gap without restarting the sequence"}, new Results(new V((RuleFunction)(args_109 => {

foreach (var row_110 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)3.0d)))))))) {
V v_111 = row_110[0];
V v_112 = row_110[1];
var values_113 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_114 = values_113[0];
V v_115 = values_113[1];
V v_116 = values_113[2];

R.Call(v_25, R.Join(new V[] {v_114}, new Results(v_112)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_114, R.Get(v_116, (V)"id")}, new Results((V)500.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_116, (V)"ai_memory"), (V)"sentinel_finished"), () => (V)(!R.Truth(R.Get(v_116, (V)"pending_attack")))))));
foreach (var row_117 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {v_114}, new Results((V)7.0d)))))))) {
V v_118 = row_117[0];
V v_119 = row_117[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(v_119, (V)"kind") != (V)"WindupStarted"), () => (V)(R.Get(v_119, (V)"ability_id") != (V)"core:ability/sentinel_quarter")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_114)))))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"displacement during recovery ends the sequence and death removes every queued impact"}, new Results(new V((RuleFunction)(args_120 => {

var values_121 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_122 = values_121[0];
V v_123 = values_121[1];
V v_124 = values_121[2];

R.Call(v_25, R.Join(new V[] {v_122}, new Results((V)3.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_124, (V)"pending_attack"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"displace"), R.Join(new V[] {v_122, R.Get(v_124, (V)"id"), (V)10.0d}, new Results((V)6.0d)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_124, (V)"ai_memory"), (V)"sentinel_finished"))));
foreach (var row_125 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {v_122}, new Results((V)5.0d)))))))) {
V v_126 = row_125[0];
V v_127 = row_125[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(v_127, (V)"kind") != (V)"WindupStarted"), () => (V)(R.Get(v_127, (V)"ability_id") != (V)"core:ability/sentinel_quarter")))));
}
var values_128 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
v_122 = values_128[0];
v_123 = values_128[1];
v_124 = values_128[2];

R.Call(v_25, R.Join(new V[] {v_122}, new Results((V)1.0d)));

var values_129 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_130 = v_124;
V key_131 = (V)"hp";
R.Set(obj_130, key_131, values_129[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_122}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_122}, new Results(R.Get(v_124, (V)"id")))).First)))));
R.Call(v_25, R.Join(new V[] {v_122}, new Results((V)4.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_123, (V)"hp") == (V)24.0d))));
foreach (var row_132 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_122, (V)"schedule"))))))) {
V v_133 = row_132[0];
V v_134 = row_132[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_134, (V)"actor_id") != R.Get(v_124, (V)"id")))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"saved facing and locked footprint resume exactly while forged sequence data is rejected"}, new Results(new V((RuleFunction)(args_135 => {

var values_136 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_137 = values_136[0];
V v_138 = values_136[1];
V v_139 = values_136[2];

R.Call(v_25, R.Join(new V[] {v_137}, new Results((V)1.0d)));

var values_140 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_137))));
V v_141 = values_140[0];
var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_141))))))));
V v_143 = values_142[0];
{ double end_146 = R.Num((V)10.0d), step_147 = R.Num((V)1.0d);
for (double i_145 = R.Num((V)1.0d); step_147 > 0 ? i_145 <= end_146 : i_145 >= end_146; i_145 += step_147) {
V v_144 = i_145;
R.Call(v_3, R.Join(new V[] {R.Call(v_25, R.Join(new V[] {v_137}, new Results((V)1.0d))).First}, R.Call(v_25, R.Join(new V[] {v_143}, new Results((V)1.0d)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_137))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_143)))));
}
}
foreach (var row_168 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_148 => {
V v_149 = args_148[0];
var values_150 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_151 = R.Get(v_149, (V)"ai_memory");
V key_152 = (V)"sentinel_facing";
R.Set(obj_151, key_152, values_150[0]);
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_153 => {
V v_154 = args_153[0];
var values_155 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_156 = R.Get(v_154, (V)"ai_memory");
V key_157 = (V)"sentinel_strikes";
R.Set(obj_156, key_157, values_155[0]);
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_158 => {
V v_159 = args_158[0];
var values_160 = R.Join(new V[] {}, new Results((V)6.0d));
var obj_161 = R.Get(R.Get(R.Get(v_159, (V)"pending_attack"), (V)"target_cells"), (V)1.0d);
V key_162 = (V)"x";
R.Set(obj_161, key_162, values_160[0]);
return Results.Empty;
}))), new Entry((V)4, new V((RuleFunction)(args_163 => {
V v_164 = args_163[0];
var values_165 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_166 = R.Get(v_164, (V)"pending_attack");
V key_167 = (V)"sentinel_facing";
R.Set(obj_166, key_167, values_165[0]);
return Results.Empty;
})))))))))) {
V v_169 = row_168[0];
V v_170 = row_168[1];
var values_171 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_141))))));
V v_172 = values_171[0];
foreach (var row_173 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_172, (V)"entities"))))))) {
V v_174 = row_173[0];
V v_175 = row_173[1];
if (R.Truth((V)(R.Get(v_175, (V)"id") == R.Get(v_139, (V)"id")))) {
R.Call(v_170, R.Join(new V[] {}, new Results(v_175)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_172))).First)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"natural champions occupy optional room interiors without replacing any biome elite or changing geometry"}, new Results(new V((RuleFunction)(args_176 => {

var values_177 = R.Join(new V[] {}, new Results((V)0.0d));
V v_178 = values_177[0];
{ double end_181 = R.Num((V)100.0d), step_182 = R.Num((V)1.0d);
for (double i_180 = R.Num((V)1.0d); step_182 > 0 ? i_180 <= end_181 : i_180 >= end_181; i_180 += step_182) {
V v_179 = i_180;
var values_183 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_179}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_184 = values_183[0];
foreach (var row_185 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_184, (V)"entities"))))))) {
V v_186 = row_185[0];
V v_187 = row_185[1];
if (R.Truth((V)(R.Get(v_187, (V)"definition_id") == R.Get(v_9, (V)"id")))) {
var values_188 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))).First, (V)"map"), R.Join(new V[] {v_184}, new Results(R.Get(R.Get(v_187, (V)"position"), (V)"floor_id")))));
V v_189 = values_188[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"optional_room"), R.Join(new V[] {v_189}, new Results(R.Get(v_187, (V)"position"))))));

var values_190 = R.Join(new V[] {}, new Results((V)(v_178 + (V)1.0d)));
v_178 = values_190[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_9, (V)"id")))).First, (V)"special_encounter"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_189, (V)"elite"), () => (V)(R.Get(R.Get(v_189, (V)"elite"), (V)"definition_id") != R.Get(v_9, (V)"id"))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_184)))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_178 > (V)0.0d)}, new Results((V)"optional champion never generated")));
var values_191 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_192 = values_191[0];
foreach (var row_193 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_194 = row_193[0];
V v_195 = row_193[1];
var values_196 = R.Join(new V[] {}, new Results((V)false));
V v_197 = values_196[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_192, (V)"packs"), v_195), (V)"missing")) == (V)0.0d))));
foreach (var row_198 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_192, (V)"packs"), v_195), (V)"bindings"))))))) {
V v_199 = row_198[0];
V v_200 = row_198[1];
if (R.Truth((V)(R.Get(v_200, (V)"id") == R.Get(v_9, (V)"id")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_200, (V)"sheet") == (V)"onebit"), () => (V)(R.Get(v_200, (V)"index") == (V)224.0d)))));

var values_201 = R.Join(new V[] {}, new Results((V)true));
v_197 = values_201[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_197)));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
