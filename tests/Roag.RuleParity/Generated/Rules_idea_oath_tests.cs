// Static C# port of tests/idea_oath_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: ffc6729705a348263d5f6fd6237880d82194f7f54b69202e534670b269181ab8
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_oath_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_oaths"))));
V v_9 = values_8[0];
V v_10 = V.Nil;
v_10 = new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
V v_13 = args_11[1];
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
var values_23 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_15, R.Get(v_9, (V)"a"), (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_24 = values_23[0];
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_15, R.Get(v_9, (V)"b"), R.Or(v_12, () => (V)9.0d), R.Or(v_13, () => (V)6.0d)}, new Results((V)100000.0d))));
V v_26 = values_25[0];
var values_27 = R.Join(new V[] {}, new Results(R.Get(v_26, (V)"id")));
var obj_28 = v_24;
V key_29 = (V)"oath_partner_id";
R.Set(obj_28, key_29, values_27[0]);

var values_30 = R.Join(new V[] {}, new Results(R.Get(v_24, (V)"id")));
var obj_31 = v_26;
V key_32 = (V)"oath_partner_id";
R.Set(obj_31, key_32, values_30[0]);
return R.Join(new V[] {v_15, v_17, v_24}, new Results(v_26));
return Results.Empty;
}));
V v_33 = V.Nil;
v_33 = new V((RuleFunction)(args_34 => {
V v_35 = args_34[0];
V v_36 = args_34[1];
var values_37 = R.Join(new V[] {}, new Results((V)0.0d));
V v_38 = values_37[0];

foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_35)))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
if (R.Truth((V)(R.Get(v_41, (V)"kind") == v_36))) {
var values_42 = R.Join(new V[] {}, new Results((V)(v_38 + (V)1.0d)));
v_38 = values_42[0];
}
}

return R.Join(new V[] {}, new Results(v_38));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"an intact oath splits mitigated loss in one batch with one real hit and no RNG draw"}, new Results(new V((RuleFunction)(args_43 => {

var values_44 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_45 = values_44[0];
V v_46 = values_44[1];
V v_47 = values_44[2];
V v_48 = values_44[3];

var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_45))));
V v_50 = values_49[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_9, (V)"partner"), R.Join(new V[] {v_45}, new Results(v_47))).First == v_48), () => (V)(R.Call(R.Get(v_9, (V)"partner"), R.Join(new V[] {v_45}, new Results(v_48))).First == v_47)))));

R.Call(v_3, R.Join(new V[] {v_50}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_45)))));
var values_51 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_45}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_52 = values_51[0];
V v_53 = values_51[1];
V v_54 = values_51[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_47, (V)"hp") == (V)6.0d), () => (V)(R.Get(v_48, (V)"hp") == (V)6.0d)), () => (V)(R.Get(R.Get(v_45, (V)"rng"), (V)"state") == R.Get(R.Get(v_50, (V)"rng"), (V)"state"))), () => (V)(R.Get(v_45, (V)"clock") == (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_33, R.Join(new V[] {v_54}, new Results((V)"DamageBatchResolved"))).First == (V)1.0d), () => (V)(R.Call(v_33, R.Join(new V[] {v_54}, new Results((V)"HitConfirmed"))).First == (V)1.0d)))));
var values_55 = R.Join(new V[] {}, new Results((V)false));
V v_56 = values_55[0];
foreach (var row_57 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_54)))))) {
V v_58 = row_57[0];
V v_59 = row_57[1];
if (R.Truth(R.And((V)(R.Get(v_59, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_59, (V)"target_id") == R.Get(v_48, (V)"id"))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.Get(v_59, (V)"oath_shared_only"), () => (V)(!R.Truth(R.Get(v_59, (V)"direct_attack")))), () => (V)(R.Get(v_59, (V)"proc_coefficient") == (V)0.0d)), () => (V)(R.Get(v_59, (V)"actual_loss") == (V)2.0d)), () => (V)(R.Get(v_59, (V)"damage_type") == (V)"oath")))));

var values_60 = R.Join(new V[] {}, new Results((V)true));
v_56 = values_60[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_56)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an area hit calculates both original losses before exchanging either share"}, new Results(new V((RuleFunction)(args_61 => {

var values_62 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {(V)8.0d}, new Results((V)5.0d))));
V v_63 = values_62[0];
V v_64 = values_62[1];
V v_65 = values_62[2];
V v_66 = values_62[3];
var values_67 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_63}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_68 = values_67[0];
V v_69 = values_67[1];
V v_70 = values_67[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_65, (V)"hp") == (V)5.0d), () => (V)(R.Get(v_66, (V)"hp") == (V)5.0d)), () => (V)(R.Call(v_33, R.Join(new V[] {v_70}, new Results((V)"HitConfirmed"))).First == (V)2.0d)))));
foreach (var row_71 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_70)))))) {
V v_72 = row_71[0];
V v_73 = row_71[1];
if (R.Truth((V)(R.Get(v_73, (V)"kind") == (V)"DamageBatchResolved"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_73, (V)"outcomes")) == (V)2.0d))));
foreach (var row_74 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_73, (V)"outcomes"))))))) {
V v_75 = row_74[0];
V v_76 = row_74[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Get(v_76, (V)"hp_before") == (V)8.0d), () => (V)(R.Get(v_76, (V)"actual_loss") == (V)3.0d)), () => (V)(R.Get(v_76, (V)"oath_given") == (V)1.0d)), () => (V)(R.Get(v_76, (V)"oath_received") == (V)1.0d)), () => (V)(!R.Truth(R.Get(v_76, (V)"oath_shared_only")))))));
}
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"flat defense applies before sharing and is never charged a second time at the recipient"}, new Results(new V((RuleFunction)(args_77 => {

var values_78 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_79 = values_78[0];
V v_80 = values_78[1];
V v_81 = values_78[2];
V v_82 = values_78[3];

var values_83 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_84 = v_81;
V key_85 = (V)"defense";
R.Set(obj_84, key_85, values_83[0]);

var values_86 = R.Join(new V[] {}, new Results((V)99.0d));
var obj_87 = v_82;
V key_88 = (V)"defense";
R.Set(obj_87, key_88, values_86[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_79}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_81, (V)"hp") == (V)7.0d), () => (V)(R.Get(v_82, (V)"hp") == (V)7.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"shared HP loss cannot manufacture a second hit or a recursive Resonant Wounds proc"}, new Results(new V((RuleFunction)(args_89 => {

var values_90 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_91 = values_90[0];
V v_92 = values_90[1];
V v_93 = values_90[2];
V v_94 = values_90[3];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_91, R.Get(v_92, (V)"id"), (V)"core:boon/resonant_wounds", (V)"common"}, new Results((V)1.0d)));
var values_95 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_91}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_96 = values_95[0];
V v_97 = values_95[1];
V v_98 = values_95[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_93, (V)"hp") == (V)6.0d), () => (V)(R.Get(v_94, (V)"hp") == (V)6.0d)), () => (V)(R.Call(v_33, R.Join(new V[] {v_98}, new Results((V)"BoonActivated"))).First == (V)0.0d)), () => (V)(R.Call(v_33, R.Join(new V[] {v_98}, new Results((V)"HitConfirmed"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real simultaneous twin deaths pay each original reward once and retain finite on-kill boons"}, new Results(new V((RuleFunction)(args_99 => {

var values_100 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {(V)8.0d}, new Results((V)5.0d))));
V v_101 = values_100[0];
V v_102 = values_100[1];
V v_103 = values_100[2];
V v_104 = values_100[3];

var values_105 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_106 = v_103;
V key_107 = (V)"hp";
R.Set(obj_106, key_107, values_105[0]);

var values_108 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_109 = v_104;
V key_110 = (V)"hp";
R.Set(obj_109, key_110, values_108[0]);

var values_111 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_112 = v_102;
V key_113 = (V)"hp";
R.Set(obj_112, key_113, values_111[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_101, R.Get(v_102, (V)"id"), (V)"core:boon/grave_dividend", (V)"common"}, new Results((V)1.0d)));
var values_114 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_101}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_115 = values_114[0];
V v_116 = values_114[1];
V v_117 = values_114[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_101}, new Results(R.Get(v_103, (V)"id")))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_101}, new Results(R.Get(v_104, (V)"id")))).First))), () => (V)(R.Get(R.Get(v_101, (V)"progression"), (V)"xp_total") == (V)8.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_33, R.Join(new V[] {v_117}, new Results((V)"EntityKilled"))).First == (V)2.0d), () => (V)(R.Call(v_33, R.Join(new V[] {v_117}, new Results((V)"BoonActivated"))).First == (V)2.0d)), () => (V)((V)R.Length(R.Get(v_101, (V)"drops")) == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_101)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"distance displacement breaks the oath permanently and returning does not reconnect it"}, new Results(new V((RuleFunction)(args_118 => {

var values_119 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_120 = values_119[0];
V v_121 = values_119[1];
V v_122 = values_119[2];
V v_123 = values_119[3];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"displace"), R.Join(new V[] {v_120, R.Get(v_123, (V)"id"), (V)13.0d}, new Results((V)6.0d)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_120}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_122, (V)"oath_broken"), () => R.Get(v_123, (V)"oath_broken")), () => (V)(!R.Truth(R.Call(R.Get(v_9, (V)"partner"), R.Join(new V[] {v_120}, new Results(v_122))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"displace"), R.Join(new V[] {v_120, R.Get(v_123, (V)"id"), (V)9.0d}, new Results((V)6.0d)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_120}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_122, (V)"hp") == (V)4.0d), () => (V)(R.Get(v_123, (V)"hp") == (V)8.0d)), () => (V)(!R.Truth(R.Call(R.Get(v_9, (V)"partner"), R.Join(new V[] {v_120}, new Results(v_123))).First))), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_120))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a blocked sight line breaks the oath instead of sharing through walls or restoring after reopening"}, new Results(new V((RuleFunction)(args_124 => {

var values_125 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {(V)10.0d}, new Results((V)6.0d))));
V v_126 = values_125[0];
V v_127 = values_125[1];
V v_128 = values_125[2];
V v_129 = values_125[3];
var values_130 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"map_for"), R.Join(new V[] {}, new Results(v_126))))))));
var obj_131 = v_126;
V key_132 = (V)"generated_map";
R.Set(obj_131, key_132, values_130[0]);
var values_133 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_126, (V)"generated_map"), (V)"rows"), (V)6.0d)));
V v_134 = values_133[0];
var values_135 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_134, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)8.0d))).First, R.Concat((V)"#", R.Invoke(v_134, "sub", R.Join(new V[] {}, new Results((V)10.0d))).First))));
var obj_136 = R.Get(R.Get(v_126, (V)"generated_map"), (V)"rows");
V key_137 = (V)6.0d;
R.Set(obj_136, key_137, values_135[0]);
R.Call(R.Get(v_9, (V)"prune"), R.Join(new V[] {}, new Results(v_126)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_128, (V)"oath_broken"), () => R.Get(v_129, (V)"oath_broken")))));
var values_138 = R.Join(new V[] {}, new Results(v_134));
var obj_139 = R.Get(R.Get(v_126, (V)"generated_map"), (V)"rows");
V key_140 = (V)6.0d;
R.Set(obj_139, key_140, values_138[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_9, (V)"partner"), R.Join(new V[] {v_126}, new Results(v_128))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"saved pair ownership and broken state resume exactly and forged partners reject"}, new Results(new V((RuleFunction)(args_141 => {

var values_142 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_143 = values_142[0];
V v_144 = values_142[1];
V v_145 = values_142[2];
V v_146 = values_142[3];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_143}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_147 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_143))))))))));
V v_148 = values_147[0];
foreach (var row_149 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))))))) {
V v_150 = row_149[0];
V v_151 = row_149[1];
var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_143}, new Results(v_151))));
V v_153 = values_152[0];
V v_154 = values_152[1];
V v_155 = values_152[2];

var values_156 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_148}, new Results(v_151))));
V v_157 = values_156[0];
V v_158 = values_156[1];
V v_159 = values_156[2];
R.Call(v_3, R.Join(new V[] {v_155}, new Results(v_159)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_143))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_148)))));
}
var values_160 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_143))));
V v_161 = values_160[0];
foreach (var row_162 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_161, (V)"entities"))))))) {
V v_163 = row_162[0];
V v_164 = row_162[1];
if (R.Truth((V)(R.Get(v_164, (V)"id") == R.Get(v_145, (V)"id")))) {
var values_165 = R.Join(new V[] {}, new Results(R.Get(v_143, (V)"player_id")));
var obj_166 = v_164;
V key_167 = (V)"oath_partner_id";
R.Set(obj_166, key_167, values_165[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_161))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"natural groups contain both reviewed twins and never spawn an ownerless secondary twin"}, new Results(new V((RuleFunction)(args_168 => {

var values_169 = R.Join(new V[] {}, new Results((V)0.0d));
V v_170 = values_169[0];
{ double end_173 = R.Num((V)100.0d), step_174 = R.Num((V)1.0d);
for (double i_172 = R.Num((V)1.0d); step_174 > 0 ? i_172 <= end_173 : i_172 >= end_173; i_172 += step_174) {
V v_171 = i_172;
var values_175 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_171}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_176 = values_175[0];
foreach (var row_177 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_176, (V)"entities"))))))) {
V v_178 = row_177[0];
V v_179 = row_177[1];
if (R.Truth(R.Call(R.Get(v_9, (V)"is_twin"), R.Join(new V[] {}, new Results(v_179))).First)) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_9, (V)"partner"), R.Join(new V[] {v_176}, new Results(v_179))).First, () => R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_176}, new Results(R.Get(v_179, (V)"oath_partner_id")))).First))));
if (R.Truth((V)(R.Get(v_179, (V)"definition_id") == R.Get(v_9, (V)"a")))) {
var values_180 = R.Join(new V[] {}, new Results((V)(v_170 + (V)1.0d)));
v_170 = values_180[0];
}
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_176))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_176)))))))))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_170 > (V)0.0d))));
var values_181 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_182 = values_181[0];
foreach (var row_183 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_184 = row_183[0];
V v_185 = row_183[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_182, (V)"packs"), v_185), (V)"missing")) == (V)0.0d))));
var values_186 = R.Join(new V[] {}, new Results((V)0.0d));
V v_187 = values_186[0];
foreach (var row_188 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_182, (V)"packs"), v_185), (V)"bindings"))))))) {
V v_189 = row_188[0];
V v_190 = row_188[1];
if (R.Truth((V)(R.Get(v_190, (V)"id") == R.Get(v_9, (V)"a")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_190, (V)"index") == (V)124.0d))));

var values_191 = R.Join(new V[] {}, new Results((V)(v_187 + (V)1.0d)));
v_187 = values_191[0];
}
if (R.Truth((V)(R.Get(v_190, (V)"id") == R.Get(v_9, (V)"b")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_190, (V)"index") == (V)127.0d))));

var values_192 = R.Join(new V[] {}, new Results((V)(v_187 + (V)1.0d)));
v_187 = values_192[0];
}
if (R.Truth((V)(R.Get(v_190, (V)"id") == (V)"core:ui/idea_oath_effect"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_190, (V)"index") == (V)574.0d))));

var values_193 = R.Join(new V[] {}, new Results((V)(v_187 + (V)1.0d)));
v_187 = values_193[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_187 == (V)3.0d))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
