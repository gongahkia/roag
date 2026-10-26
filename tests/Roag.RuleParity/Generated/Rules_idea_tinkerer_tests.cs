// Static C# port of tests/idea_tinkerer_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 8c1845200cbae60f1ad6d2012382ea24ee74fa0c60fd51513271fc16425c1fcf
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_tinkerer_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_9 = values_8[0];

var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_11 = values_10[0];
V v_12 = V.Nil;
v_12 = new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
V v_15 = args_13[1];
var values_16 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_7, (V)"tinker_id")))))));
V v_17 = values_16[0];

var values_18 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_17))));
V v_19 = values_18[0];

var values_20 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_21 = R.Get(v_19, (V)"position");
V key_22 = (V)"x";
var obj_23 = R.Get(v_19, (V)"position");
V key_24 = (V)"y";
R.Set(obj_21, key_22, values_20[0]);
R.Set(obj_23, key_24, values_20[1]);
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_17, (V)"core:enemy/mossbound_guard", R.Or(v_14, () => (V)10.0d), R.Or(v_15, () => (V)6.0d)}, new Results((V)100000.0d))));
V v_26 = values_25[0];

R.Call(R.Get(v_9, (V)"recompute"), R.Join(new V[] {}, new Results(v_17)));

return R.Join(new V[] {v_17, v_19}, new Results(v_26));
return Results.Empty;
}));
V v_27 = V.Nil;
v_27 = new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_29}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

return R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"owned"), R.Join(new V[] {v_29}, new Results((V)"sentry"))))));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Tinkerer deploys one three-attempt sentry and shows its locked real scheduled line"}, new Results(new V((RuleFunction)(args_30 => {

var values_31 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_32 = values_31[0];
V v_33 = values_31[1];
V v_34 = values_31[2];

var values_35 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_32))));
V v_36 = values_35[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_32, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))).First, (V)"valid"))));

R.Call(v_3, R.Join(new V[] {v_36}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_32)))));
var values_37 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_32))));
V v_38 = values_37[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_32, (V)"clock") == (V)168.0d), () => (V)(R.Get(v_38, (V)"sentry_shots") == (V)2.0d)), () => (V)(R.Get(v_38, (V)"sentry_phase") == (V)"impact")))));
var values_39 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"pending_strikes"), R.Join(new V[] {}, new Results(v_32))));
V v_40 = values_39[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_40) == (V)1.0d), () => (V)(R.Get(R.Get(v_40, (V)1.0d), (V)"due") == (V)180.0d)), () => (V)(R.Get(R.Get(R.Get(R.Get(v_40, (V)1.0d), (V)"target_cells"), (V)2.0d), (V)"x") == (V)10.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_34, (V)"hp") == (V)10.0d), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_32))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"three finite sentry bolts use normal armor and cannot farm more attacks by waiting"}, new Results(new V((RuleFunction)(args_41 => {

var values_42 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_43 = values_42[0];
V v_44 = values_42[1];
V v_45 = values_42[2];

var values_46 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_43))));
V v_47 = values_46[0];

var values_48 = R.Join(new V[] {}, new Results((V)0.0d));
V v_49 = values_48[0];
{ double end_52 = R.Num((V)12.0d), step_53 = R.Num((V)1.0d);
for (double i_51 = R.Num((V)1.0d); step_53 > 0 ? i_51 <= end_52 : i_51 >= end_52; i_51 += step_53) {
V v_50 = i_51;
var values_54 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_43}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_55 = values_54[0];
V v_56 = values_54[1];
V v_57 = values_54[2];
foreach (var row_58 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_57)))))) {
V v_59 = row_58[0];
V v_60 = row_58[1];
if (R.Truth((V)(R.Get(v_60, (V)"kind") == (V)"AttackPerformed"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_60, (V)"ability_id") == (V)"core:ability/tinkerer_sentry"), () => (V)(R.Get(v_60, (V)"proc_coefficient") == (V)0.0d)))));

var values_61 = R.Join(new V[] {}, new Results((V)(v_49 + (V)1.0d)));
v_49 = values_61[0];
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_49 == (V)3.0d), () => (V)(R.Get(v_45, (V)"hp") == (V)4.0d)), () => (V)(R.Get(v_47, (V)"sentry_shots") == (V)0.0d)), () => (V)(!R.Truth(R.Get(v_47, (V)"sentry_schedule_id")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_43)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"committed sentry bolts retain friendly fire and stop on the player in their line"}, new Results(new V((RuleFunction)(args_62 => {

var values_63 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)5.0d}, new Results((V)6.0d))));
V v_64 = values_63[0];
V v_65 = values_63[1];
V v_66 = values_63[2];

R.Call(v_27, R.Join(new V[] {}, new Results(v_64)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_64}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_65, (V)"hp") == (V)20.0d), () => (V)(R.Get(v_66, (V)"hp") == (V)10.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a moved sentry target is not tracked and replacement cancels the old committed shot"}, new Results(new V((RuleFunction)(args_67 => {

var values_68 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_69 = values_68[0];
V v_70 = values_68[1];
V v_71 = values_68[2];

var values_72 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_69))));
V v_73 = values_72[0];

var values_74 = R.Join(new V[] {}, new Results((V)7.0d));
var obj_75 = R.Get(v_71, (V)"position");
V key_76 = (V)"y";
R.Set(obj_75, key_76, values_74[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_69}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_71, (V)"hp") == (V)10.0d))));
var values_77 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
v_69 = values_77[0];
v_70 = values_77[1];
v_71 = values_77[2];

var values_78 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_69))));
v_73 = values_78[0];

var values_79 = R.Join(new V[] {}, new Results(R.Get(v_73, (V)"sentry_schedule_id")));
V v_80 = values_79[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_69}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));

var values_81 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"owned"), R.Join(new V[] {v_69}, new Results((V)"sentry"))));
V v_82 = values_81[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_82, (V)"id") != R.Get(v_73, (V)"id")), () => (V)(R.Get(v_71, (V)"hp") == (V)10.0d)))));
foreach (var row_83 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_69, (V)"schedule"))))))) {
V v_84 = row_83[0];
V v_85 = row_83[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_85, (V)"id") != v_80))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"sentry placement rejects occupied diagonals walls and stairs without spending time"}, new Results(new V((RuleFunction)(args_86 => {

var values_87 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)8.0d}, new Results((V)6.0d))));
V v_88 = values_87[0];
V v_89 = values_87[1];
V v_90 = values_87[2];

var values_91 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_88))));
V v_92 = values_91[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_88}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_92}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_88)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_88}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_92}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_88)))));
var values_93 = R.Join(new V[] {(V)8.0d}, new Results((V)5.0d));
var obj_94 = R.Get(v_89, (V)"position");
V key_95 = (V)"x";
var obj_96 = R.Get(v_89, (V)"position");
V key_97 = (V)"y";
R.Set(obj_94, key_95, values_93[0]);
R.Set(obj_96, key_97, values_93[1]);

var values_98 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_88))));
v_92 = values_98[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_88}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_92}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_88)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"sentry kills credit the owner once without autonomous boon or Echo proc chains"}, new Results(new V((RuleFunction)(args_99 => {

var values_100 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_101 = values_100[0];
V v_102 = values_100[1];
V v_103 = values_100[2];

var values_104 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_105 = v_103;
V key_106 = (V)"hp";
R.Set(obj_105, key_106, values_104[0]);

R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_101, R.Get(v_102, (V)"id"), (V)"core:boon/temporal_echo", (V)"common"}, new Results((V)1.0d)));
R.Call(v_27, R.Join(new V[] {}, new Results(v_101)));

var values_107 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_101}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_108 = values_107[0];
V v_109 = values_107[1];
V v_110 = values_107[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_101}, new Results(R.Get(v_103, (V)"id")))).First)), () => (V)(R.Get(R.Get(v_101, (V)"progression"), (V)"xp_total") > (V)0.0d)), () => (V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_101, (V)"delayed_effects")))).First))))));
var values_111 = R.Join(new V[] {}, new Results((V)0.0d));
V v_112 = values_111[0];

foreach (var row_113 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_110)))))) {
V v_114 = row_113[0];
V v_115 = row_113[1];
if (R.Truth((V)(R.Get(v_115, (V)"kind") == (V)"EntityKilled"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_115, (V)"source_id") == R.Get(v_102, (V)"id")))));

var values_116 = R.Join(new V[] {}, new Results((V)(v_112 + (V)1.0d)));
v_112 = values_116[0];
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_112 == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pending finite sentry shots resume exactly and reject forged charge or missing timer state"}, new Results(new V((RuleFunction)(args_117 => {

var values_118 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_119 = values_118[0];

R.Call(v_27, R.Join(new V[] {}, new Results(v_119)));

var values_120 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_119))));
V v_121 = values_120[0];

var values_122 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_121))))))));
V v_123 = values_122[0];
{ double end_126 = R.Num((V)5.0d), step_127 = R.Num((V)1.0d);
for (double i_125 = R.Num((V)1.0d); step_127 > 0 ? i_125 <= end_126 : i_125 >= end_126; i_125 += step_127) {
V v_124 = i_125;
var values_128 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_119}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_129 = values_128[0];
V v_130 = values_128[1];
V v_131 = values_128[2];

var values_132 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_123}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_133 = values_132[0];
V v_134 = values_132[1];
V v_135 = values_132[2];

R.Call(v_3, R.Join(new V[] {v_131}, new Results(v_135)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_119))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_123)))));
}
}
foreach (var row_150 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_136 => {
V v_137 = args_136[0];
foreach (var row_138 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_137, (V)"entities"))))))) {
V v_139 = row_138[0];
V v_140 = row_138[1];
if (R.Truth((V)(R.Get(v_140, (V)"companion_kind") == (V)"sentry"))) {
var values_141 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_142 = v_140;
V key_143 = (V)"sentry_shots";
R.Set(obj_142, key_143, values_141[0]);
}
}
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_144 => {
V v_145 = args_144[0];
{ double end_148 = R.Num((V)1.0d), step_149 = R.Num((-(V)1.0d));
for (double i_147 = R.Num((V)R.Length(R.Get(v_145, (V)"schedule"))); step_149 > 0 ? i_147 <= end_148 : i_147 >= end_148; i_147 += step_149) {
V v_146 = i_147;
if (R.Truth(R.Get(R.Get(R.Get(v_145, (V)"schedule"), v_146), (V)"idea_sentry"))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_145, (V)"schedule")}, new Results(v_146)));
}
}
}
return Results.Empty;
})))))))))) {
V v_151 = row_150[0];
V v_152 = row_150[1];
var values_153 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_121))))));
V v_154 = values_153[0];

R.Call(v_152, R.Join(new V[] {}, new Results(v_154)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_154))).First)))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
