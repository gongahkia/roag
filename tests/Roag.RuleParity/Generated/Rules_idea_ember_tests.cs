// Static C# port of tests/idea_ember_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 29732fdc0d8446706c57338bfaa03bb252f8d4efbdc5c17631d8da4212c70851
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_ember_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_embers"))));
V v_11 = values_10[0];

var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_13 = values_12[0];
V v_14 = V.Nil;
v_14 = new V((RuleFunction)(args_15 => {

var values_16 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
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
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_17, R.Get(v_11, (V)"id"), (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
V v_26 = values_25[0];
return R.Join(new V[] {v_17, v_19}, new Results(v_26));
return Results.Empty;
}));
V v_27 = V.Nil;
v_27 = new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
V v_30 = args_28[1];
var values_31 = R.Join(new V[] {}, new Results(R.Table()));
V v_32 = values_31[0];
{ double end_35 = R.Num(v_30), step_36 = R.Num((V)1.0d);
for (double i_34 = R.Num((V)1.0d); step_36 > 0 ? i_34 <= end_35 : i_34 >= end_35; i_34 += step_36) {
V v_33 = i_34;
var values_37 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_29}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_38 = values_37[0];
V v_39 = values_37[1];
V v_40 = values_37[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_38)));
foreach (var row_41 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_40)))))) {
V v_42 = row_41[0];
V v_43 = row_41[1];
var values_44 = R.Join(new V[] {}, new Results(v_43));
var obj_45 = v_32;
V key_46 = (V)((V)R.Length(v_32) + (V)1.0d);
R.Set(obj_45, key_46, values_44[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_32));
return Results.Empty;
}));
V v_47 = V.Nil;
v_47 = new V((RuleFunction)(args_48 => {

var values_49 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_50 = values_49[0];
V v_51 = values_49[1];
V v_52 = values_49[2];

R.Call(v_27, R.Join(new V[] {v_50}, new Results((V)3.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_51, (V)"hp") == (V)22.0d), () => (V)((V)R.Length(R.Get(v_50, (V)"idea_embers")) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_50, (V)"idea_embers"), (V)1.0d), (V)"next_due") == (V)330.0d)))));
return R.Join(new V[] {v_50, v_51}, new Results(v_52));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Moth preview is pure and a dive retains its locked landing after the player moves"}, new Results(new V((RuleFunction)(args_53 => {

var values_54 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_55 = values_54[0];
V v_56 = values_54[1];
V v_57 = values_54[2];

var values_58 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_55))));
V v_59 = values_58[0];
{ double end_62 = R.Num((V)30.0d), step_63 = R.Num((V)1.0d);
for (double i_61 = R.Num((V)1.0d); step_63 > 0 ? i_61 <= end_62 : i_61 >= end_62; i_61 += step_63) {
V v_60 = i_61;
var values_64 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_55}, new Results(v_57))));
V v_65 = values_64[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_65, (V)"ember_landing"), (V)"x") == (V)7.0d), () => (V)(R.Get(R.Get(v_65, (V)"ember_landing"), (V)"y") == (V)6.0d)), () => (V)(R.Get(v_65, (V)"ranged") == (V)"area")))));
}
}
R.Call(v_3, R.Join(new V[] {v_59}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_55)))));

R.Call(v_27, R.Join(new V[] {v_55}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_57, (V)"pending_attack"), (V)"due") == (V)230.0d), () => (V)(R.Get(R.Get(v_57, (V)"ai_memory"), (V)"ember_dives") == (V)1.0d)), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_55))))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_55}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));

R.Call(v_27, R.Join(new V[] {v_55}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_56, (V)"hp") == (V)24.0d), () => (V)(R.Get(R.Get(v_57, (V)"position"), (V)"x") == (V)7.0d)), () => (V)(R.Get(R.Get(v_57, (V)"position"), (V)"y") == (V)6.0d)), () => (V)(R.Get(R.Get(R.Get(v_55, (V)"idea_embers"), (V)1.0d), (V)"x") == (V)7.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an occupied landing keeps bodies separate and the finite ember pulses at exact world times"}, new Results(new V((RuleFunction)(args_66 => {

var values_67 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
V v_68 = values_67[0];
V v_69 = values_67[1];
V v_70 = values_67[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_70, (V)"position"), (V)"x") == (V)10.0d), () => (V)(R.Get(R.Get(v_69, (V)"position"), (V)"x") == (V)7.0d)))));
var values_71 = R.Join(new V[] {}, new Results(R.Table()));
V v_72 = values_71[0];
foreach (var row_73 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {v_68}, new Results((V)2.0d)))))))) {
V v_74 = row_73[0];
V v_75 = row_73[1];
if (R.Truth((V)(R.Get(v_75, (V)"kind") == (V)"EmberPulsed"))) {
var values_76 = R.Join(new V[] {}, new Results(R.Get(v_75, (V)"time")));
var obj_77 = v_72;
V key_78 = (V)((V)R.Length(v_72) + (V)1.0d);
R.Set(obj_77, key_78, values_76[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)((V)R.Length(v_72) == (V)2.0d), () => (V)(R.Get(v_72, (V)1.0d) == (V)330.0d)), () => (V)(R.Get(v_72, (V)2.0d) == (V)430.0d)), () => (V)(R.Get(v_69, (V)"hp") == (V)20.0d)), () => (V)((V)R.Length(R.Get(v_68, (V)"idea_embers")) == (V)0.0d)))));
foreach (var row_79 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_68, (V)"schedule"))))))) {
V v_80 = row_79[0];
V v_81 = row_79[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_81, (V)"idea_ember"))))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"fire warnings expose the actual next pulse while read-only targeting cannot advance or consume it"}, new Results(new V((RuleFunction)(args_82 => {

var values_83 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
V v_84 = values_83[0];
V v_85 = values_83[1];
V v_86 = values_83[2];

var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_84))));
V v_88 = values_87[0];
{ double end_91 = R.Num((V)30.0d), step_92 = R.Num((V)1.0d);
for (double i_90 = R.Num((V)1.0d); step_92 > 0 ? i_90 <= end_91 : i_90 >= end_91; i_90 += step_92) {
V v_89 = i_90;
var values_93 = R.Join(new V[] {}, new Results((V)false));
V v_94 = values_93[0];
foreach (var row_95 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"pending_strikes"), R.Join(new V[] {}, new Results(v_84)))))))) {
V v_96 = row_95[0];
V v_97 = row_95[1];
if (R.Truth(R.Get(v_97, (V)"ember_pulse"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_97, (V)"due") == (V)330.0d), () => (V)(R.Get(v_97, (V)"damage") == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_97, (V)"target_cells"), (V)1.0d), (V)"x") == (V)7.0d)))));

var values_98 = R.Join(new V[] {}, new Results((V)true));
v_94 = values_98[0];
var values_99 = R.Join(new V[] {}, new Results((V)99.0d));
var obj_100 = R.Get(R.Get(v_97, (V)"target_cells"), (V)1.0d);
V key_101 = (V)"x";
R.Set(obj_100, key_101, values_99[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_94)));

R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_84, (V)"dash", (V)0.0d}, new Results((V)1.0d)));
}
}
R.Call(v_3, R.Join(new V[] {v_88}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_84)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"leaving a burning cell avoids later pulses without changing the underlying floor"}, new Results(new V((RuleFunction)(args_102 => {

var values_103 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
V v_104 = values_103[0];
V v_105 = values_103[1];
V v_106 = values_103[2];

var values_107 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_13, (V)"map_for"), R.Join(new V[] {}, new Results(v_104))).First, (V)"rows")))));
V v_108 = values_107[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_104}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));

R.Call(v_27, R.Join(new V[] {v_104}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_105, (V)"hp") == (V)22.0d), () => (V)((V)R.Length(R.Get(v_104, (V)"idea_embers")) == (V)0.0d)), () => (V)(v_108 == R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_13, (V)"map_for"), R.Join(new V[] {}, new Results(v_104))).First, (V)"rows")))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an ember survives its caster death for only its remaining scheduled pulse"}, new Results(new V((RuleFunction)(args_109 => {

var values_110 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
V v_111 = values_110[0];
V v_112 = values_110[1];
V v_113 = values_110[2];

var values_114 = R.Join(new V[] {(V)9.0d}, new Results((V)6.0d));
var obj_115 = R.Get(v_112, (V)"position");
V key_116 = (V)"x";
var obj_117 = R.Get(v_112, (V)"position");
V key_118 = (V)"y";
R.Set(obj_115, key_116, values_114[0]);
R.Set(obj_117, key_118, values_114[1]);

var values_119 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_120 = v_113;
V key_121 = (V)"hp";
R.Set(obj_120, key_121, values_119[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_111}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_111}, new Results(R.Get(v_113, (V)"id")))).First)), () => (V)((V)R.Length(R.Get(v_111, (V)"idea_embers")) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_111, (V)"idea_embers"), (V)1.0d), (V)"remaining") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_111)))))));

var values_122 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_123 = R.Get(v_112, (V)"position");
V key_124 = (V)"x";
var obj_125 = R.Get(v_112, (V)"position");
V key_126 = (V)"y";
R.Set(obj_123, key_124, values_122[0]);
R.Set(obj_125, key_126, values_122[1]);
R.Call(v_27, R.Join(new V[] {v_111}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_112, (V)"hp") == (V)21.0d), () => (V)((V)R.Length(R.Get(v_111, (V)"idea_embers")) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stun and displacement cancel the committed dive without refunding its finite attempt"}, new Results(new V((RuleFunction)(args_127 => {

foreach (var row_128 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"stun"), new Entry((V)2, (V)"displace")))))))) {
V v_129 = row_128[0];
V v_130 = row_128[1];
var values_131 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_132 = values_131[0];
V v_133 = values_131[1];
V v_134 = values_131[2];

R.Call(v_27, R.Join(new V[] {v_132}, new Results((V)1.0d)));

var values_135 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_134, (V)"pending_attack"), (V)"action_id")));
V v_136 = values_135[0];
if (R.Truth((V)(v_130 == (V)"stun"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_132, R.Get(v_134, (V)"id")}, new Results((V)500.0d)))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"displace"), R.Join(new V[] {v_132, R.Get(v_134, (V)"id"), (V)9.0d}, new Results((V)6.0d)))));
}
R.Call(v_27, R.Join(new V[] {v_132}, new Results((V)2.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_133, (V)"hp") == (V)24.0d), () => (V)((V)R.Length(R.Or(R.Get(v_132, (V)"idea_embers"), () => R.Table())) == (V)0.0d)), () => (V)(R.Get(R.Get(v_134, (V)"ai_memory"), (V)"ember_dives") >= (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(!R.Truth(R.Get(v_134, (V)"pending_attack"))), () => (V)(R.Get(R.Get(v_134, (V)"pending_attack"), (V)"action_id") != v_136)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a different creature on the locked tile takes normal friendly fire and prevents body overlap"}, new Results(new V((RuleFunction)(args_137 => {

var values_138 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_139 = values_138[0];
V v_140 = values_138[1];
V v_141 = values_138[2];

R.Call(v_27, R.Join(new V[] {v_139}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_139}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));
var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_139, (V)"core:enemy/mossbound_guard", (V)7.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_143 = values_142[0];
R.Call(v_27, R.Join(new V[] {v_139}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_143, (V)"hp") == (V)9.0d), () => (V)(R.Get(v_140, (V)"hp") == (V)24.0d)), () => (V)(R.Get(R.Get(v_141, (V)"position"), (V)"x") == (V)10.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_139, (V)"idea_embers"), (V)1.0d), (V)"x") == (V)7.0d), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_139))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"only the source Moth is immune to its own ember and another Moth can burn"}, new Results(new V((RuleFunction)(args_144 => {

var values_145 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_146 = values_145[0];
V v_147 = values_145[1];
V v_148 = values_145[2];

R.Call(v_27, R.Join(new V[] {v_146}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_146}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));

R.Call(v_27, R.Join(new V[] {v_146}, new Results((V)3.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_148, (V)"hp") == (V)6.0d), () => (V)((V)R.Length(R.Get(v_146, (V)"idea_embers")) == (V)0.0d)), () => (V)(R.Get(v_147, (V)"hp") == (V)24.0d)))));
var values_149 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
v_146 = values_149[0];
v_147 = values_149[1];
v_148 = values_149[2];

R.Call(v_27, R.Join(new V[] {v_146}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_146}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));

R.Call(v_27, R.Join(new V[] {v_146}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"displace"), R.Join(new V[] {v_146, R.Get(v_148, (V)"id"), (V)8.0d}, new Results((V)6.0d)))));
var values_150 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_146, R.Get(v_11, (V)"id"), (V)7.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_151 = values_150[0];

R.Call(v_27, R.Join(new V[] {v_146}, new Results((V)2.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_151, (V)"hp") == (V)4.0d), () => (V)(R.Get(v_148, (V)"hp") == (V)6.0d)), () => (V)((V)R.Length(R.Get(v_146, (V)"idea_embers")) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pending dive and active ember restore exactly alongside real Burning and Temporal Echo work"}, new Results(new V((RuleFunction)(args_152 => {

var values_153 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
V v_154 = values_153[0];
V v_155 = values_153[1];
V v_156 = values_153[2];

var values_157 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_154, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_158 = values_157[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_154, R.Get(v_155, (V)"id"), (V)"core:boon/emberbrand", (V)"common"}, new Results((V)1.0d)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_154, R.Get(v_155, (V)"id"), (V)"core:boon/temporal_echo", (V)"common"}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_154}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(R.Get(v_158, (V)"statuses"), (V)"burning"), () => (V)((V)R.Length(R.Get(v_154, (V)"idea_embers")) == (V)1.0d)), () => R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_154, (V)"delayed_effects")))).First))));
var values_159 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_154))))))))));
V v_160 = values_159[0];
{ double end_163 = R.Num((V)3.0d), step_164 = R.Num((V)1.0d);
for (double i_162 = R.Num((V)1.0d); step_164 > 0 ? i_162 <= end_163 : i_162 >= end_163; i_162 += step_164) {
V v_161 = i_162;
R.Call(v_3, R.Join(new V[] {R.Call(v_27, R.Join(new V[] {v_154}, new Results((V)1.0d))).First}, R.Call(v_27, R.Join(new V[] {v_160}, new Results((V)1.0d)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_154))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_160)))));
}
}
var values_165 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
v_154 = values_165[0];
v_155 = values_165[1];
v_156 = values_165[2];

R.Call(v_27, R.Join(new V[] {v_154}, new Results((V)1.0d)));

var values_166 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_154))))))))));
v_160 = values_166[0];
R.Call(v_3, R.Join(new V[] {R.Call(v_27, R.Join(new V[] {v_154}, new Results((V)4.0d))).First}, R.Call(v_27, R.Join(new V[] {v_160}, new Results((V)4.0d)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_154))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_160)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"frozen-floor fire clocks and queued pulses rebase together without touching another floor"}, new Results(new V((RuleFunction)(args_167 => {

var values_168 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"clock", (V)900.0d), new Entry((V)"entities", R.Table()), new Entry((V)"schedule", R.Table()), new Entry((V)"floor_pause", R.Table(new Entry((V)"a", R.Table(new Entry((V)"suspended_at", (V)300.0d), new Entry((V)"schedule", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"id", (V)1.0d), new Entry((V)"seq", (V)1.0d), new Entry((V)"due", (V)330.0d), new Entry((V)"actor_id", (V)0.0d), new Entry((V)"floor_id", (V)"a"), new Entry((V)"idea_ember", (V)true))))))))), new Entry((V)"idea_embers", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", (V)"a"), new Entry((V)"next_due", (V)330.0d), new Entry((V)"expires", (V)430.0d))), new Entry((V)2, R.Table(new Entry((V)"floor_id", (V)"b"), new Entry((V)"next_due", (V)1000.0d), new Entry((V)"expires", (V)1100.0d))))))));
V v_169 = values_168[0];
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))).First, (V)"resume"), R.Join(new V[] {v_169}, new Results((V)"a")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(R.Get(v_169, (V)"idea_embers"), (V)1.0d), (V)"next_due") == (V)930.0d), () => (V)(R.Get(R.Get(R.Get(v_169, (V)"idea_embers"), (V)1.0d), (V)"expires") == (V)1030.0d)), () => (V)(R.Get(R.Get(R.Get(v_169, (V)"schedule"), (V)1.0d), (V)"due") == (V)930.0d)), () => (V)(R.Get(R.Get(R.Get(v_169, (V)"idea_embers"), (V)2.0d), (V)"next_due") == (V)1000.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"invalid dive ownership pulse counts lost timers and orphaned fire work reject safely"}, new Results(new V((RuleFunction)(args_170 => {

var values_171 = R.Join(new V[] {}, R.Call(v_47, Results.Empty));
V v_172 = values_171[0];
V v_173 = values_171[1];
V v_174 = values_171[2];

var values_175 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_172))));
V v_176 = values_175[0];
foreach (var row_202 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_177 => {
V v_178 = args_177[0];
var values_179 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_180 = R.Get(R.Get(v_178, (V)"idea_embers"), (V)1.0d);
V key_181 = (V)"remaining";
R.Set(obj_180, key_181, values_179[0]);
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_182 => {
V v_183 = args_182[0];
var values_184 = R.Join(new V[] {}, new Results(R.Get(v_183, (V)"player_id")));
var obj_185 = R.Get(R.Get(v_183, (V)"idea_embers"), (V)1.0d);
V key_186 = (V)"source_id";
R.Set(obj_185, key_186, values_184[0]);
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_187 => {
V v_188 = args_187[0];
var values_189 = R.Join(new V[] {}, new Results((V)299.0d));
var obj_190 = R.Get(R.Get(v_188, (V)"idea_embers"), (V)1.0d);
V key_191 = (V)"next_due";
R.Set(obj_190, key_191, values_189[0]);
return Results.Empty;
}))), new Entry((V)4, new V((RuleFunction)(args_192 => {
V v_193 = args_192[0];
var values_194 = R.Join(new V[] {}, new Results((V)999999.0d));
var obj_195 = R.Get(R.Get(v_193, (V)"idea_embers"), (V)1.0d);
V key_196 = (V)"schedule_id";
R.Set(obj_195, key_196, values_194[0]);
return Results.Empty;
}))), new Entry((V)5, new V((RuleFunction)(args_197 => {
V v_198 = args_197[0];
var values_199 = R.Join(new V[] {}, new Results(R.Table()));
var obj_200 = v_198;
V key_201 = (V)"idea_embers";
R.Set(obj_200, key_201, values_199[0]);
return Results.Empty;
})))))))))) {
V v_203 = row_202[0];
V v_204 = row_202[1];
var values_205 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_176))))));
V v_206 = values_205[0];

R.Call(v_204, R.Join(new V[] {}, new Results(v_206)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_206))).First)))));
}
var values_207 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
v_172 = values_207[0];
v_173 = values_207[1];
v_174 = values_207[2];

R.Call(v_27, R.Join(new V[] {v_172}, new Results((V)1.0d)));

var values_208 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_172))));
V v_209 = values_208[0];
foreach (var row_210 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_209, (V)"entities"))))))) {
V v_211 = row_210[0];
V v_212 = row_210[1];
if (R.Truth((V)(R.Get(v_212, (V)"id") == R.Get(v_174, (V)"id")))) {
var values_213 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_214 = R.Get(R.Get(v_212, (V)"pending_attack"), (V)"ember_landing");
V key_215 = (V)"x";
R.Set(obj_214, key_215, values_213[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_209))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a naturally selected Moth uses approved unused winged body 411 in the fixed 1-Bit look"}, new Results(new V((RuleFunction)(args_216 => {

var values_217 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_seeds"))).First, (V)"find"), R.Join(new V[] {}, new Results(R.Get(v_11, (V)"id")))));
V v_218 = values_217[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_218)))))));
var values_219 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_220 = values_219[0];
foreach (var row_221 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_222 = row_221[0];
V v_223 = row_221[1];
var values_224 = R.Join(new V[] {}, new Results((V)false));
V v_225 = values_224[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_220, (V)"packs"), v_223), (V)"missing")) == (V)0.0d))));
foreach (var row_226 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_220, (V)"packs"), v_223), (V)"bindings"))))))) {
V v_227 = row_226[0];
V v_228 = row_226[1];
if (R.Truth((V)(R.Get(v_228, (V)"id") == R.Get(v_11, (V)"id")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_228, (V)"sheet") == (V)"onebit"), () => (V)(R.Get(v_228, (V)"index") == (V)411.0d)))));

var values_229 = R.Join(new V[] {}, new Results((V)true));
v_225 = values_229[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_225)));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
