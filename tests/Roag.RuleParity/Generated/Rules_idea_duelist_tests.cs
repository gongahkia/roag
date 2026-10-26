// Static C# port of tests/idea_duelist_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: bebb6197ee839c6b62eea5da91d75a672869569f3fb42e48c198d4ea239d4f7c
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_duelist_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_duelist"))));
V v_9 = values_8[0];
V v_10 = V.Nil;
v_10 = new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
V v_13 = args_11[1];
V v_14 = args_11[2];
V v_15 = args_11[3];
var values_16 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_9, (V)"id")))))));
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
var values_25 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_17, R.Or(v_12, () => (V)"core:enemy/mossbound_guard"), R.Or(v_13, () => (V)8.0d), R.Or(v_14, () => (V)6.0d)}, new Results(R.Or(v_15, () => (V)0.0d)))));
V v_26 = values_25[0];
return R.Join(new V[] {v_17, v_19}, new Results(v_26));
return Results.Empty;
}));
V v_27 = V.Nil;
v_27 = new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
V v_30 = args_28[1];
V v_31 = args_28[2];
var values_32 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_29}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", R.Or(v_30, () => (V)1.0d)), new Entry((V)"dy", R.Or(v_31, () => (V)0.0d)))))));
V v_33 = values_32[0];
V v_34 = values_32[1];
V v_35 = values_32[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_33}, new Results(v_34)));

return R.Join(new V[] {}, new Results(v_35));
return Results.Empty;
}));
V v_36 = V.Nil;
v_36 = new V((RuleFunction)(args_37 => {

var values_38 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_39 = values_38[0];
V v_40 = values_38[1];
V v_41 = values_38[2];

R.Call(v_27, R.Join(new V[] {}, new Results(v_39)));

R.Call(v_27, R.Join(new V[] {}, new Results(v_39)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_40, (V)"hp") == (V)20.0d), () => R.Get(v_40, (V)"duelist_riposte")), () => (V)(R.Get(v_39, (V)"clock") == (V)174.0d)))));
return R.Join(new V[] {v_39, v_40}, new Results(v_41));
return Results.Empty;
}));
V v_42 = V.Nil;
v_42 = new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
var values_46 = R.Join(new V[] {}, new Results((V)0.0d));
V v_47 = values_46[0];

foreach (var row_48 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_44)))))) {
V v_49 = row_48[0];
V v_50 = row_48[1];
if (R.Truth((V)(R.Get(v_50, (V)"kind") == v_45))) {
var values_51 = R.Join(new V[] {}, new Results((V)(v_47 + (V)1.0d)));
v_47 = values_51[0];
}
}

return R.Join(new V[] {}, new Results(v_47));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Duelist explicit thrust is a pure two-cell lane while hybrid bump retains one target"}, new Results(new V((RuleFunction)(args_52 => {

var values_53 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {V.Nil, V.Nil, V.Nil}, new Results((V)100000.0d))));
V v_54 = values_53[0];
V v_55 = values_53[1];
V v_56 = values_53[2];
var values_57 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_54, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_58 = values_57[0];
var values_59 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_54))));
V v_60 = values_59[0];
{ double end_63 = R.Num((V)30.0d), step_64 = R.Num((V)1.0d);
for (double i_62 = R.Num((V)1.0d); step_64 > 0 ? i_62 <= end_63 : i_62 >= end_63; i_62 += step_64) {
V v_61 = i_62;
var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_54, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_66 = values_65[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_66, (V)"valid"), () => (V)(R.Get(v_66, (V)"cost") == (V)87.0d)), () => (V)((V)R.Length(R.Get(v_66, (V)"cells")) == (V)2.0d)))));
}
}
R.Call(v_3, R.Join(new V[] {v_60}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_54)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_54}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_56, (V)"hp") == (V)7.0d), () => (V)(R.Get(v_58, (V)"hp") == (V)10.0d)), () => (V)(R.Get(R.Get(v_55, (V)"position"), (V)"x") == (V)7.0d)), () => (V)(R.Get(v_54, (V)"clock") == (V)87.0d)))));
var values_67 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {V.Nil, V.Nil, V.Nil}, new Results((V)100000.0d))));
v_54 = values_67[0];
v_55 = values_67[1];
v_56 = values_67[2];

var values_68 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_54, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)6.0d}, new Results((V)100000.0d))));
v_58 = values_68[0];
var values_69 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_54}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_70 = values_69[0];
V v_71 = values_69[1];
V v_72 = values_69[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_56, (V)"hp") == (V)7.0d), () => (V)(R.Get(v_58, (V)"hp") == (V)7.0d)), () => (V)(R.Call(v_42, R.Join(new V[] {v_72}, new Results((V)"DamageBatchResolved"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"parry pays its real recovery and catches only a melee impact inside that committed window"}, new Results(new V((RuleFunction)(args_73 => {

var values_74 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_75 = values_74[0];
V v_76 = values_74[1];
V v_77 = values_74[2];

var values_78 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_75, (V)"rng"), (V)"state")));
V v_79 = values_78[0];
R.Call(v_27, R.Join(new V[] {}, new Results(v_75)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_75, (V)"clock") == (V)87.0d), () => (V)(!R.Truth(R.Get(v_76, (V)"duelist_riposte")))), () => (V)(!R.Truth(R.Get(R.Get(v_75, (V)"class_special"), (V)"duelist_guard")))))));
var values_80 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_75))));
V v_81 = values_80[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Get(v_75, (V)"clock") == (V)174.0d), () => (V)(R.Get(v_76, (V)"hp") == (V)20.0d)), () => R.Get(v_76, (V)"duelist_riposte")), () => (V)(R.Get(v_77, (V)"hp") == (V)10.0d)), () => (V)(R.Get(R.Get(v_75, (V)"rng"), (V)"state") == v_79)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_42, R.Join(new V[] {v_81}, new Results((V)"DuelistParried"))).First == (V)1.0d), () => (V)(R.Call(v_42, R.Join(new V[] {v_81}, new Results((V)"DamageBlocked"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(R.Get(v_75, (V)"class_special"), (V)"duelist_guard"))), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_75))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a successful parry funds one paid plus-two thrust and never an automatic counterattack"}, new Results(new V((RuleFunction)(args_82 => {

var values_83 = R.Join(new V[] {}, R.Call(v_36, Results.Empty));
V v_84 = values_83[0];
V v_85 = values_83[1];
V v_86 = values_83[2];

var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_84))));
V v_88 = values_87[0];
{ double end_91 = R.Num((V)30.0d), step_92 = R.Num((V)1.0d);
for (double i_90 = R.Num((V)1.0d); step_92 > 0 ? i_90 <= end_91 : i_90 >= end_91; i_90 += step_92) {
V v_89 = i_90;
var values_93 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"effective_ability"), R.Join(new V[] {v_84, (V)"basic_attack", (V)1.0d, (V)0.0d}, new Results((V)true))));
V v_94 = values_93[0];
V v_95 = values_93[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_95 == (V)6.0d))));
}
}
R.Call(v_3, R.Join(new V[] {v_88}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_84)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_86, (V)"hp") == (V)10.0d))));
var values_96 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_97 = values_96[0];
V v_98 = values_96[1];
V v_99 = values_96[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_84, (V)"clock") == (V)261.0d), () => (V)(R.Get(v_86, (V)"hp") == (V)5.0d)), () => (V)(!R.Truth(R.Get(v_85, (V)"duelist_riposte")))), () => (V)(R.Call(v_42, R.Join(new V[] {v_99}, new Results((V)"DuelistRiposteSpent"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_86, (V)"hp") == (V)2.0d), () => (V)(R.Get(v_84, (V)"clock") == (V)348.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"rear strikes ranged shots and later Burning ticks bypass a frontal melee parry"}, new Results(new V((RuleFunction)(args_100 => {

var values_101 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {V.Nil, (V)6.0d}, new Results((V)6.0d))));
V v_102 = values_101[0];
V v_103 = values_101[1];
V v_104 = values_101[2];

R.Call(v_27, R.Join(new V[] {}, new Results(v_102)));

R.Call(v_27, R.Join(new V[] {}, new Results(v_102)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_103, (V)"hp") == (V)17.0d), () => (V)(!R.Truth(R.Get(v_103, (V)"duelist_riposte")))))));
var values_105 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {(V)"core:enemy/thornspitter", (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
v_102 = values_105[0];
v_103 = values_105[1];
v_104 = values_105[2];

R.Call(v_27, R.Join(new V[] {}, new Results(v_102)));

R.Call(v_27, R.Join(new V[] {}, new Results(v_102)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_103, (V)"hp") == (V)18.0d), () => (V)(!R.Truth(R.Get(v_103, (V)"duelist_riposte")))))));
var values_106 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {V.Nil, (V)6.0d}, new Results((V)6.0d))));
v_102 = values_106[0];
v_103 = values_106[1];
v_104 = values_106[2];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_102, R.Get(v_104, (V)"id"), (V)"core:boon/emberbrand", (V)"common"}, new Results((V)1.0d)));
R.Call(v_27, R.Join(new V[] {}, new Results(v_102)));

R.Call(v_27, R.Join(new V[] {}, new Results(v_102)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_103, (V)"statuses"), (V)"burning"))));
R.Call(v_27, R.Join(new V[] {}, new Results(v_102)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_103, (V)"hp") == (V)15.0d), () => (V)(!R.Truth(R.Get(v_103, (V)"duelist_riposte")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"one stance blocks at most one hit even when two adjacent attackers impact at the same timestamp"}, new Results(new V((RuleFunction)(args_107 => {

var values_108 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {(V)"core:enemy/ruin_skitter", (V)8.0d, (V)5.0d}, new Results((V)100.0d))));
V v_109 = values_108[0];
V v_110 = values_108[1];
V v_111 = values_108[2];
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_109, (V)"core:enemy/ruin_skitter", (V)8.0d, (V)7.0d}, new Results((V)100.0d)));
R.Call(v_27, R.Join(new V[] {}, new Results(v_109)));

var values_112 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_109))));
V v_113 = values_112[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_110, (V)"hp") == (V)19.0d), () => R.Get(v_110, (V)"duelist_riposte")), () => (V)(R.Call(v_42, R.Join(new V[] {v_113}, new Results((V)"DuelistParried"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_42, R.Join(new V[] {v_113}, new Results((V)"DamageTaken"))).First == (V)1.0d), () => (V)(R.Call(v_42, R.Join(new V[] {v_113}, new Results((V)"DamageBlocked"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"raising a guard deals no damage and cannot manufacture an attack or Temporal Echo"}, new Results(new V((RuleFunction)(args_114 => {

var values_115 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {V.Nil, V.Nil, V.Nil}, new Results((V)100000.0d))));
V v_116 = values_115[0];
V v_117 = values_115[1];
V v_118 = values_115[2];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_116, R.Get(v_117, (V)"id"), (V)"core:boon/temporal_echo", (V)"common"}, new Results((V)1.0d)));
var values_119 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_116, (V)"rng"), (V)"state")));
V v_120 = values_119[0];

var values_121 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_116))));
V v_122 = values_121[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_118, (V)"hp") == (V)10.0d), () => (V)(R.Get(v_117, (V)"hp") == (V)20.0d)), () => (V)(!R.Truth(R.Get(v_117, (V)"duelist_riposte")))), () => (V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_116, (V)"delayed_effects")))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_42, R.Join(new V[] {v_122}, new Results((V)"AttackPerformed"))).First == (V)0.0d), () => (V)(R.Call(v_42, R.Join(new V[] {v_122}, new Results((V)"AbilityUsed"))).First == (V)0.0d)), () => (V)(R.Get(R.Get(v_116, (V)"rng"), (V)"state") == v_120)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a charged miss spends the riposte once and rejected input cannot spend it"}, new Results(new V((RuleFunction)(args_123 => {

var values_124 = R.Join(new V[] {}, R.Call(v_36, Results.Empty));
V v_125 = values_124[0];
V v_126 = values_124[1];
V v_127 = values_124[2];

var values_128 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_125))));
V v_129 = values_128[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_125}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)0.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_129}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_125)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_125}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(v_126, (V)"duelist_riposte"))), () => (V)(R.Get(v_127, (V)"hp") == (V)10.0d)), () => (V)(R.Get(v_125, (V)"clock") == (V)261.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a physical two-cell thrust ignores a reflector instead of becoming a projectile"}, new Results(new V((RuleFunction)(args_130 => {

var values_131 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {(V)"core:enemy/mirror_hexer", (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
V v_132 = values_131[0];
V v_133 = values_131[1];
V v_134 = values_131[2];
while (R.Truth((V)(R.Get(v_132, (V)"clock") < (V)250.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_132}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_132, (V)"idea_mirrors")) == (V)1.0d))));

var values_135 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_136 = R.Get(v_133, (V)"position");
V key_137 = (V)"x";
R.Set(obj_136, key_137, values_135[0]);
var values_138 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_132, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_139 = values_138[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(v_139, (V)"reflection"))), () => (V)(R.Get(R.Get(R.Get(v_139, (V)"cells"), (V)2.0d), (V)"x") == (V)10.0d)), () => (V)(R.Get(R.Get(R.Get(v_139, (V)"cells"), (V)2.0d), (V)"y") == (V)6.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_132}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_134, (V)"hp") == (V)3.0d), () => (V)((V)R.Length(R.Get(v_132, (V)"idea_mirrors")) == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"one-cell sidestep and thrust use actual terrain restrictions without diagonal corner penetration"}, new Results(new V((RuleFunction)(args_140 => {

var values_141 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {V.Nil, V.Nil, V.Nil}, new Results((V)100000.0d))));
V v_142 = values_141[0];
V v_143 = values_141[1];
V v_144 = values_141[2];
var values_145 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_142, (V)"dash", (V)0.0d}, new Results((V)1.0d))));
V v_146 = values_145[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_146, (V)"valid"), () => (V)(R.Get(v_146, (V)"distance") == (V)1.0d)), () => (V)(R.Get(v_146, (V)"cost") == (V)74.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_143, (V)"position"), (V)"y") == (V)7.0d), () => (V)(R.Get(v_142, (V)"clock") == (V)74.0d)))));
var values_147 = R.Join(new V[] {(V)8.0d}, new Results((V)4.0d));
var obj_148 = R.Get(v_143, (V)"position");
V key_149 = (V)"x";
var obj_150 = R.Get(v_143, (V)"position");
V key_151 = (V)"y";
R.Set(obj_148, key_149, values_147[0]);
R.Set(obj_150, key_151, values_147[1]);
var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_142, (V)"basic_attack", (V)1.0d}, new Results((V)1.0d))));
v_146 = values_152[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_146, (V)"cells")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_146, (V)"cells"), (V)1.0d), (V)"x") == (V)9.0d)), () => (V)(R.Get(R.Get(R.Get(v_146, (V)"cells"), (V)1.0d), (V)"y") == (V)5.0d)))));
var values_153 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_142, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_154 = values_153[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_154, (V)"hp") == (V)10.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"held riposte roundtrips exactly and forged charges or outliving parry windows reject"}, new Results(new V((RuleFunction)(args_155 => {

var values_156 = R.Join(new V[] {}, R.Call(v_36, Results.Empty));
V v_157 = values_156[0];
V v_158 = values_156[1];
V v_159 = values_156[2];

var values_160 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_157))));
V v_161 = values_160[0];
var values_162 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_161))))))));
V v_163 = values_162[0];
{ double end_166 = R.Num((V)3.0d), step_167 = R.Num((V)1.0d);
for (double i_165 = R.Num((V)1.0d); step_167 > 0 ? i_165 <= end_166 : i_165 >= end_166; i_165 += step_167) {
V v_164 = i_165;
var values_168 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))));
V v_169 = values_168[0];
var values_170 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_157}, new Results(v_169))));
V v_171 = values_170[0];
V v_172 = values_170[1];
V v_173 = values_170[2];

var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_163}, new Results(v_169))));
V v_175 = values_174[0];
V v_176 = values_174[1];
V v_177 = values_174[2];
R.Call(v_3, R.Join(new V[] {v_173}, new Results(v_177)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_157))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_163)))));
}
}
foreach (var row_199 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_178 => {
V v_179 = args_178[0];
foreach (var row_180 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_179, (V)"entities"))))))) {
V v_181 = row_180[0];
V v_182 = row_180[1];
if (R.Truth((V)(R.Get(v_182, (V)"id") == R.Get(v_179, (V)"player_id")))) {
var values_183 = R.Join(new V[] {}, new Results((V)false));
var obj_184 = v_182;
V key_185 = (V)"duelist_riposte";
R.Set(obj_184, key_185, values_183[0]);
}
}
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_186 => {
V v_187 = args_186[0];
var values_188 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"expires", (V)(R.Get(v_187, (V)"clock") + (V)100.0d)))));
var obj_189 = R.Get(v_187, (V)"class_special");
V key_190 = (V)"duelist_guard";
R.Set(obj_189, key_190, values_188[0]);
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_191 => {
V v_192 = args_191[0];
foreach (var row_193 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_192, (V)"entities"))))))) {
V v_194 = row_193[0];
V v_195 = row_193[1];
if (R.Truth((V)(R.Get(v_195, (V)"controller") == (V)"ai"))) {
var values_196 = R.Join(new V[] {}, new Results((V)true));
var obj_197 = v_195;
V key_198 = (V)"duelist_riposte";
R.Set(obj_197, key_198, values_196[0]);
}
}
return Results.Empty;
})))))))))) {
V v_200 = row_199[0];
V v_201 = row_199[1];
var values_202 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_161))))));
V v_203 = values_202[0];

R.Call(v_201, R.Join(new V[] {}, new Results(v_203)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_203))).First)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Duelist enters genuine paired generation and can defeat an ordinary first foe by legal actions"}, new Results(new V((RuleFunction)(args_204 => {

var values_205 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_206 = values_205[0];

var values_207 = R.Join(new V[] {}, R.Call(R.Get(v_206, (V)"new"), R.Join(new V[] {(V)9.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"class_id", R.Get(v_9, (V)"id")))))));
V v_208 = values_207[0];
var values_209 = R.Join(new V[] {}, new Results(R.Get(v_208, (V)"state")));
V v_210 = values_209[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_210))).First, (V)"definition_id") == R.Get(v_9, (V)"id")))));
var values_213 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_206, (V)"nearest"), R.Join(new V[] {v_210}, new Results(new V((RuleFunction)(args_211 => {
V v_212 = args_211[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_212, (V)"controller") == (V)"ai"), () => (V)(!R.Truth(R.Get(v_212, (V)"invulnerable"))))));
return Results.Empty;
}))))))));
V v_214 = values_213[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_206, (V)"fight"), R.Join(new V[] {v_208, R.Get(v_214, (V)"id")}, new Results((V)180.0d)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_210}, new Results(R.Get(v_214, (V)"id")))).First)), () => (V)((V)R.Length(R.Get(v_208, (V)"trace")) > (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_210)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"the fixed renderer resolves the reviewed Duelist and report its deliberate Twin body reuse"}, new Results(new V((RuleFunction)(args_215 => {

var values_216 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_217 = values_216[0];
foreach (var row_218 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_219 = row_218[0];
V v_220 = row_218[1];
var values_221 = R.Join(new V[] {}, new Results((V)false));
V v_222 = values_221[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_217, (V)"packs"), v_220), (V)"missing")) == (V)0.0d))));
foreach (var row_223 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_217, (V)"packs"), v_220), (V)"bindings"))))))) {
V v_224 = row_223[0];
V v_225 = row_223[1];
if (R.Truth((V)(R.Get(v_225, (V)"id") == R.Get(v_9, (V)"id")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_225, (V)"sheet") == (V)"onebit"), () => (V)(R.Get(v_225, (V)"index") == (V)124.0d)))));

var values_226 = R.Join(new V[] {}, new Results((V)true));
v_222 = values_226[0];
}
}
var values_227 = R.Join(new V[] {}, new Results((V)false));
V v_228 = values_227[0];
foreach (var row_229 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_217, (V)"packs"), v_220), (V)"shared_actors"))))))) {
V v_230 = row_229[0];
V v_231 = row_229[1];
foreach (var row_232 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_231, (V)"ids"))))))) {
V v_233 = row_232[0];
V v_234 = row_232[1];
if (R.Truth((V)(v_234 == R.Get(v_9, (V)"id")))) {
var values_235 = R.Join(new V[] {}, new Results((V)true));
v_228 = values_235[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_222, () => v_228))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
