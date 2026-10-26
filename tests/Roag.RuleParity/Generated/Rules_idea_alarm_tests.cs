// Static C# port of tests/idea_alarm_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 2be7dfda74c0e0934ef9cede8789b8be83a851b3fe9158ddec12d63d70836831
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_alarm_tests { internal static Results Load(NativeRuntime rt) {
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
var values_23 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_15, (V)"core:enemy/alarm_keeper", (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
V v_24 = values_23[0];
return R.Join(new V[] {v_15, v_17}, new Results(v_24));
return Results.Empty;
}));
V v_25 = V.Nil;
v_25 = new V((RuleFunction)(args_26 => {
V v_27 = args_26[0];
var values_28 = R.Join(new V[] {}, new Results(R.Table()));
V v_29 = values_28[0];

foreach (var row_30 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_27, (V)"entities"))))))) {
V v_31 = row_30[0];
V v_32 = row_30[1];
if (R.Truth(R.Get(v_32, (V)"called_by"))) {
var values_33 = R.Join(new V[] {}, new Results(v_32));
var obj_34 = v_29;
V key_35 = (V)((V)R.Length(v_29) + (V)1.0d);
R.Set(obj_34, key_35, values_33[0]);
}
}

return R.Join(new V[] {}, new Results(v_29));
return Results.Empty;
}));
V v_36 = V.Nil;
v_36 = new V((RuleFunction)(args_37 => {
V v_38 = args_37[0];
var values_39 = R.Join(new V[] {}, new Results(R.Table()));
V v_40 = values_39[0];
while (R.Truth((V)(R.Get(v_38, (V)"clock") < (V)400.0d))) {
var values_41 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_38}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_42 = values_41[0];
V v_43 = values_41[1];
V v_44 = values_41[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_42)));
foreach (var row_45 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_44)))))) {
V v_46 = row_45[0];
V v_47 = row_45[1];
var values_48 = R.Join(new V[] {}, new Results(v_47));
var obj_49 = v_40;
V key_50 = (V)((V)R.Length(v_40) + (V)1.0d);
R.Set(obj_49, key_50, values_48[0]);
}
}
return R.Join(new V[] {}, new Results(v_40));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Alarm Keeper commits a long pure visible call, spends its only budget and locks two cells"}, new Results(new V((RuleFunction)(args_51 => {

var values_52 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_53 = values_52[0];
V v_54 = values_52[1];
V v_55 = values_52[2];

var values_56 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_53))));
V v_57 = values_56[0];
var values_58 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"choose"), R.Join(new V[] {v_53}, new Results(v_55))));
V v_59 = values_58[0];

R.Call(v_3, R.Join(new V[] {v_57}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_53)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_59, (V)"support"), (V)"kind") == (V)"alarm"), () => (V)((V)R.Length(R.Get(v_59, (V)"cells")) == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_53}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(R.Get(v_55, (V)"ai_memory"), (V)"alarm_spent"), () => (V)(R.Get(R.Get(v_55, (V)"pending_attack"), (V)"due") == (V)343.0d)), () => (V)((V)R.Length(R.Call(v_25, R.Join(new V[] {}, new Results(v_53))).First) == (V)0.0d)))));
var values_60 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_55, (V)"pending_attack"), (V)"target_cells")))))));
V v_61 = values_60[0];

var values_62 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results(v_53))));
V v_63 = values_62[0];
var values_64 = R.Join(new V[] {}, new Results((V)0.0d));
V v_65 = values_64[0];
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_63)))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
if (R.Truth((V)(R.Get(v_68, (V)"kind") == (V)"AlarmReinforcement"))) {
var values_69 = R.Join(new V[] {}, new Results((V)(v_65 + (V)1.0d)));
v_65 = values_69[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_68, (V)"x") == R.Get(R.Get(v_61, v_65), (V)"x")), () => (V)(R.Get(v_68, (V)"y") == R.Get(R.Get(v_61, v_65), (V)"y"))), () => (V)(R.Get(v_68, (V)"time") == (V)343.0d)))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_65 == (V)2.0d), () => (V)((V)R.Length(R.Call(v_25, R.Join(new V[] {}, new Results(v_53))).First) == (V)2.0d)), () => (V)(!R.Truth(R.Get(v_55, (V)"pending_attack")))))));
foreach (var row_70 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {}, new Results(v_53)))))))) {
V v_71 = row_70[0];
V v_72 = row_70[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_72, (V)"called_by") == R.Get(v_55, (V)"id")), () => R.Get(v_72, (V)"reward_exempt")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(R.Call(R.Get(v_7, (V)"choose"), R.Join(new V[] {v_53}, new Results(v_55))).First, (V)"support")))}, new Results((V)"a resolved alarm must never call again")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stun or death interrupts the finite call without a delayed second attempt"}, new Results(new V((RuleFunction)(args_73 => {

var values_74 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_75 = values_74[0];
V v_76 = values_74[1];
V v_77 = values_74[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_75}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_75, R.Get(v_77, (V)"id")}, new Results((V)500.0d)))));
R.Call(v_36, R.Join(new V[] {}, new Results(v_75)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(R.Get(v_77, (V)"ai_memory"), (V)"alarm_spent"), () => (V)((V)R.Length(R.Call(v_25, R.Join(new V[] {}, new Results(v_75))).First) == (V)0.0d)), () => (V)(!R.Truth(R.Get(v_77, (V)"pending_attack")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_7, (V)"choose"), R.Join(new V[] {v_75}, new Results(v_77))).First, (V)"support"))))));
var values_78 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
v_75 = values_78[0];
v_76 = values_78[1];
v_77 = values_78[2];

var values_79 = R.Join(new V[] {}, new Results((V)9.0d));
var obj_80 = R.Get(v_76, (V)"position");
V key_81 = (V)"x";
R.Set(obj_80, key_81, values_79[0]);

var values_82 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_83 = v_77;
V key_84 = (V)"hp";
R.Set(obj_83, key_84, values_82[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_75}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_77, (V)"pending_attack"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_75}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(v_36, R.Join(new V[] {}, new Results(v_75)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_75}, new Results(R.Get(v_77, (V)"id")))).First)), () => (V)((V)R.Length(R.Call(v_25, R.Join(new V[] {}, new Results(v_75))).First) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an occupied marked cell blocks that reinforcement without retargeting the other cell"}, new Results(new V((RuleFunction)(args_85 => {

var values_86 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_87 = values_86[0];
V v_88 = values_86[1];
V v_89 = values_86[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_87}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_90 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_89, (V)"pending_attack"), (V)"target_cells"), (V)1.0d)));
V v_91 = values_90[0];
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_87, (V)"core:enemy/mossbound_guard", R.Get(v_91, (V)"x"), R.Get(v_91, (V)"y")}, new Results((V)100000.0d)));
var values_92 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results(v_87))));
V v_93 = values_92[0];

var values_94 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_95 = values_94[0];
V v_96 = values_94[1];
foreach (var row_97 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_93)))))) {
V v_98 = row_97[0];
V v_99 = row_97[1];
if (R.Truth((V)(R.Get(v_99, (V)"kind") == (V)"AlarmReinforcement"))) {
var values_100 = R.Join(new V[] {}, new Results((V)(v_95 + (V)1.0d)));
v_95 = values_100[0];
} else if (R.Truth((V)(R.Get(v_99, (V)"kind") == (V)"AlarmBlocked"))) {
var values_101 = R.Join(new V[] {}, new Results((V)(v_96 + (V)1.0d)));
v_96 = values_101[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_99, (V)"x") == R.Get(v_91, (V)"x")), () => (V)(R.Get(v_99, (V)"y") == R.Get(v_91, (V)"y"))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_95 == (V)1.0d), () => (V)(v_96 == (V)1.0d)), () => (V)((V)R.Length(R.Call(v_25, R.Join(new V[] {}, new Results(v_87))).First) == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"hidden destinations, stairs, exits and map boundaries are never eligible alarm cells"}, new Results(new V((RuleFunction)(args_102 => {

var values_103 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_104 = values_103[0];
V v_105 = values_103[1];
V v_106 = values_103[2];

var values_107 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"map_for"), R.Join(new V[] {}, new Results(v_104))));
V v_108 = values_107[0];
var values_115 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"choose"), R.Join(new V[] {v_104, v_106, v_105}, new Results(R.Table(new Entry((V)"map", v_108), new Entry((V)"free", new V((RuleFunction)(args_109 => {

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))), new Entry((V)"visible", new V((RuleFunction)(args_110 => {
V v_111 = args_110[0];
V v_112 = args_110[1];
return R.Join(new V[] {}, new Results(R.Or((V)(v_111 != R.Get(R.Get(v_106, (V)"position"), (V)"x")), () => (V)(v_112 != R.Get(R.Get(v_106, (V)"position"), (V)"y")))));
return Results.Empty;
}))), new Entry((V)"next_step", new V((RuleFunction)(args_113 => {


return Results.Empty;
}))), new Entry((V)"adjacent_goals", new V((RuleFunction)(args_114 => {

return R.Join(new V[] {}, new Results(R.Table()));
return Results.Empty;
}))))))));
V v_116 = values_115[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_116, (V)"support")))}, new Results((V)"an unseen caller cannot begin a supposedly visible alarm")));
var values_118 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_117 => {

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))));
V v_119 = values_118[0];
var values_120 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_104, (V)"active_floor_id")), new Entry((V)"x", (V)11.0d), new Entry((V)"y", (V)6.0d))));
V v_121 = values_120[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_11, (V)"alarm_landing"), R.Join(new V[] {v_104, v_106, v_121, v_108, v_119}, new Results(new V((RuleFunction)(args_122 => {

return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}))))).First)))));
var values_123 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, new Results(v_108))))));
v_108 = values_123[0];

var values_124 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"stairs", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)11.0d), new Entry((V)"y", (V)6.0d))))))));
var obj_125 = v_108;
V key_126 = (V)"placements";
R.Set(obj_125, key_126, values_124[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_11, (V)"alarm_landing"), R.Join(new V[] {v_104, v_106, v_121, v_108, v_119}, new Results(new V((RuleFunction)(args_127 => {

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))))).First)))));
var values_128 = R.Join(new V[] {}, new Results(R.Table()));
var obj_129 = R.Get(v_108, (V)"placements");
V key_130 = (V)"stairs";
R.Set(obj_129, key_130, values_128[0]);
var values_131 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"controller", (V)"exit"), new Entry((V)"position", v_121))));
V v_132 = values_131[0];

var values_133 = R.Join(new V[] {}, new Results(v_132));
var obj_134 = R.Get(v_104, (V)"entities");
V key_135 = (V)((V)R.Length(R.Get(v_104, (V)"entities")) + (V)1.0d);
R.Set(obj_134, key_135, values_133[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_11, (V)"alarm_landing"), R.Join(new V[] {v_104, v_106, v_121, v_108, v_119}, new Results(new V((RuleFunction)(args_136 => {

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))))).First)))));
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {}, new Results(R.Get(v_104, (V)"entities"))));

var values_137 = R.Join(new V[] {(V)1.0d}, new Results((V)1.0d));
var obj_138 = R.Get(v_106, (V)"position");
V key_139 = (V)"x";
var obj_140 = R.Get(v_106, (V)"position");
V key_141 = (V)"y";
R.Set(obj_138, key_139, values_137[0]);
R.Set(obj_140, key_141, values_137[1]);

var values_142 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
var obj_143 = v_121;
V key_144 = (V)"x";
var obj_145 = v_121;
V key_146 = (V)"y";
R.Set(obj_143, key_144, values_142[0]);
R.Set(obj_145, key_146, values_142[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_11, (V)"alarm_landing"), R.Join(new V[] {v_104, v_106, v_121, v_108, v_119}, new Results(new V((RuleFunction)(args_147 => {

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"reinforcements have no repeated XP, currency, supply drops or revival records"}, new Results(new V((RuleFunction)(args_148 => {

var values_149 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_150 = values_149[0];
V v_151 = values_149[1];
V v_152 = values_149[2];

R.Call(v_36, R.Join(new V[] {}, new Results(v_150)));
var values_153 = R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {}, new Results(v_150))));
V v_154 = values_153[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_154) == (V)2.0d))));
var values_155 = R.Join(new V[] {R.Get(R.Get(v_150, (V)"progression"), (V)"xp_total")}, new Results((V)R.Length(R.Get(v_150, (V)"drops"))));
V v_156 = values_155[0];
V v_157 = values_155[1];
foreach (var row_158 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_154)))))) {
V v_159 = row_158[0];
V v_160 = row_158[1];
var values_161 = R.Join(new V[] {(V)(R.Get(R.Get(v_160, (V)"position"), (V)"x") - (V)1.0d)}, new Results(R.Get(R.Get(v_160, (V)"position"), (V)"y")));
var obj_162 = R.Get(v_151, (V)"position");
V key_163 = (V)"x";
var obj_164 = R.Get(v_151, (V)"position");
V key_165 = (V)"y";
R.Set(obj_162, key_163, values_161[0]);
R.Set(obj_164, key_165, values_161[1]);
var values_166 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_167 = v_160;
V key_168 = (V)"hp";
R.Set(obj_167, key_168, values_166[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_150}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_25, R.Join(new V[] {}, new Results(v_150))).First) == (V)0.0d), () => (V)(R.Get(R.Get(v_150, (V)"progression"), (V)"xp_total") == v_156)), () => (V)((V)R.Length(R.Get(v_150, (V)"drops")) == v_157)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(!R.Truth(R.Get(v_150, (V)"idea_corpses"))), () => (V)((V)R.Length(R.Get(v_150, (V)"idea_corpses")) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pending alarm and called actors resume exact ordered events and reject forged budgets or claims"}, new Results(new V((RuleFunction)(args_169 => {

var values_170 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_171 = values_170[0];
V v_172 = values_170[1];
V v_173 = values_170[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_171}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_171))));
V v_175 = values_174[0];
var values_176 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, new Results(v_175))))))));
V v_177 = values_176[0];
R.Call(v_3, R.Join(new V[] {R.Call(v_36, R.Join(new V[] {}, new Results(v_171))).First}, R.Call(v_36, R.Join(new V[] {}, new Results(v_177)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_171))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_177)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_171)))))))))));
foreach (var row_178 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_175, (V)"entities"))))))) {
V v_179 = row_178[0];
V v_180 = row_178[1];
if (R.Truth((V)(R.Get(v_180, (V)"id") == R.Get(v_173, (V)"id")))) {
var values_181 = R.Join(new V[] {}, new Results((V)false));
var obj_182 = R.Get(v_180, (V)"ai_memory");
V key_183 = (V)"alarm_spent";
R.Set(obj_182, key_183, values_181[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_175))).First)))));
var values_184 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_171))));
v_175 = values_184[0];
foreach (var row_185 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_175, (V)"entities"))))))) {
V v_186 = row_185[0];
V v_187 = row_185[1];
if (R.Truth(R.Get(v_187, (V)"called_by"))) {
var values_188 = R.Join(new V[] {}, new Results((V)false));
var obj_189 = v_187;
V key_190 = (V)"reward_exempt";
R.Set(obj_189, key_190, values_188[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_175))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real generated floors contain Alarm Keepers and the approved keeper and bell resolve in the fixed 1-Bit renderer"}, new Results(new V((RuleFunction)(args_191 => {

var values_192 = R.Join(new V[] {}, new Results((V)false));
V v_193 = values_192[0];
{ double end_196 = R.Num((V)50.0d), step_197 = R.Num((V)1.0d);
for (double i_195 = R.Num((V)1.0d); step_197 > 0 ? i_195 <= end_196 : i_195 >= end_196; i_195 += step_197) {
V v_194 = i_195;
var values_198 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_194}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_199 = values_198[0];
foreach (var row_200 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_199, (V)"entities"))))))) {
V v_201 = row_200[0];
V v_202 = row_200[1];
if (R.Truth((V)(R.Get(v_202, (V)"definition_id") == (V)"core:enemy/alarm_keeper"))) {
var values_203 = R.Join(new V[] {}, new Results((V)true));
v_193 = values_203[0];
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_193)));
var values_204 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_205 = values_204[0];
foreach (var row_206 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_207 = row_206[0];
V v_208 = row_206[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_205, (V)"packs"), v_208), (V)"missing")) == (V)0.0d))));
var values_209 = R.Join(new V[] {(V)false}, new Results((V)false));
V v_210 = values_209[0];
V v_211 = values_209[1];
foreach (var row_212 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_205, (V)"packs"), v_208), (V)"bindings"))))))) {
V v_213 = row_212[0];
V v_214 = row_212[1];
if (R.Truth((V)(R.Get(v_214, (V)"id") == (V)"core:enemy/alarm_keeper"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_214, (V)"index") == (V)173.0d))));

var values_215 = R.Join(new V[] {}, new Results((V)true));
v_210 = values_215[0];
}
if (R.Truth((V)(R.Get(v_214, (V)"id") == (V)"core:ui/idea_alarm_effect"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_214, (V)"index") == (V)915.0d))));

var values_216 = R.Join(new V[] {}, new Results((V)true));
v_211 = values_216[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_210, () => v_211))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
