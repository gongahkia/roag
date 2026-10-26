// Static C# port of tests/idea_dice_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: d420c604ee64d1f94882b676efa16f3e43f21222d2edf586de7596fa5f5a58b8
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_dice_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_21 = values_20[0];
var values_22 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_oaths"))));
V v_23 = values_22[0];
var values_24 = R.Join(new V[] {}, new Results((V)"core:boon/fateful_dice"));
V v_25 = values_24[0];
var values_26 = R.Join(new V[] {}, new Results(R.Table()));
V v_27 = values_26[0];
{ double end_30 = R.Num((V)80.0d), step_31 = R.Num((V)1.0d);
for (double i_29 = R.Num((V)1.0d); step_31 > 0 ? i_29 <= end_30 : i_29 >= end_30; i_29 += step_31) {
V v_28 = i_29;
var values_32 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results(v_28))));
V v_33 = values_32[0];

var values_34 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", R.Get(R.Get(v_33, (V)"rng"), (V)"state")))));
V v_35 = values_34[0];
var values_36 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"next_int"), R.Join(new V[] {v_35}, new Results((V)8.0d))));
V v_37 = values_36[0];
var values_38 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_27, v_37), () => v_28)));
var obj_39 = v_27;
V key_40 = v_37;
R.Set(obj_39, key_40, values_38[0]);
}
}
V v_41 = V.Nil;
v_41 = new V((RuleFunction)(args_42 => {
V v_43 = args_42[0];
V v_44 = args_42[1];
var values_45 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_27, v_43)))).First, V.Nil}, new Results(R.Table(new Entry((V)"class_id", v_44))))));
V v_46 = values_45[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_46, R.Get(v_46, (V)"player_id"), v_25, (V)"rare"}, new Results((V)1.0d)));
return R.Join(new V[] {v_46}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_46))));
return Results.Empty;
}));
V v_47 = V.Nil;
v_47 = new V((RuleFunction)(args_48 => {
V v_49 = args_48[0];
V v_50 = args_48[1];
V v_51 = args_48[2];
V v_52 = args_48[3];
var values_53 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_49, R.Or(v_52, () => (V)"core:enemy/mossbound_guard"), R.Or(v_50, () => (V)3.0d), R.Or(v_51, () => (V)2.0d)}, new Results((V)100000.0d))));
V v_54 = values_53[0];
var values_55 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_56 = v_54;
V key_57 = (V)"hp";
R.Set(obj_56, key_57, values_55[0]);

var values_58 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_59 = v_54;
V key_60 = (V)"max_hp";
R.Set(obj_59, key_60, values_58[0]);

return R.Join(new V[] {}, new Results(v_54));
return Results.Empty;
}));
V v_61 = V.Nil;
v_61 = new V((RuleFunction)(args_62 => {
V v_63 = args_62[0];
V v_64 = args_62[1];
var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_63}, new Results(R.Or(v_64, () => R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
V v_66 = values_65[0];
V v_67 = values_65[1];
V v_68 = values_65[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_66}, new Results(v_67)));

return R.Join(new V[] {}, new Results(v_68));
return Results.Empty;
}));
V v_69 = V.Nil;
v_69 = new V((RuleFunction)(args_70 => {
V v_71 = args_70[0];
V v_72 = args_70[1];
var values_73 = R.Join(new V[] {}, new Results((V)0.0d));
V v_74 = values_73[0];

foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_71)))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
if (R.Truth((V)(R.Get(v_77, (V)"kind") == v_72))) {
var values_78 = R.Join(new V[] {}, new Results((V)(v_74 + (V)1.0d)));
v_74 = values_78[0];
}
}

return R.Join(new V[] {}, new Results(v_74));
return Results.Empty;
}));
V v_79 = V.Nil;
v_79 = new V((RuleFunction)(args_80 => {
V v_81 = args_80[0];
V v_82 = args_80[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_69, R.Join(new V[] {v_81}, new Results((V)"FateRolled"))).First == (V)1.0d))));
foreach (var row_83 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_81)))))) {
V v_84 = row_83[0];
V v_85 = row_83[1];
if (R.Truth((V)(R.Get(v_85, (V)"kind") == (V)"FateRolled"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_85, (V)"face") == v_82), () => (V)(R.Get(v_85, (V)"binding_id") == R.Concat((V)"core:ui/fate_", R.Concat(v_82, (V)"_icon")))))));

return R.Join(new V[] {}, new Results(v_85));
}
}
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"dice declares six numbered faces, skull and miss with exact approved native bindings"}, new Results(new V((RuleFunction)(args_86 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_11, (V)"cues")) == (V)8.0d), () => (V)((V)R.Length(R.Get(v_11, (V)"faces")) == (V)8.0d)), () => R.Call(R.Get(v_7, (V)"validate"), Results.Empty).First))));
var values_87 = R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_slots"))).First, (V)"bindings")));
V v_88 = values_87[0];
foreach (var row_89 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_11, (V)"cues"))))))) {
V v_90 = row_89[0];
V v_91 = row_89[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_27, v_90), () => R.Get(v_88, R.Get(v_91, (V)"id"))), () => (V)(R.Get(v_91, (V)"id") == R.Concat((V)"core:ui/fate_", R.Concat(v_90, (V)"_icon")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_91, (V)"index") == R.Or(R.Or(R.And((V)(v_90 <= (V)6.0d), () => (V)((V)727.0d + v_90)), () => R.And((V)(v_90 == (V)7.0d), () => (V)727.0d)), () => (V)726.0d)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"get"), R.Join(new V[] {}, new Results(v_25))).First, (V)"art"), (V)"index") == (V)728.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty).First, (V)"requirements")) == (V)((V)((V)491.0d + (V)((V)2.0d * (V)R.Length(R.Get(v_11, (V)"order")))) + (V)R.Length(R.Get(v_11, (V)"cues")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"dice preview and canceled or invalid input never roll or consume RNG"}, new Results(new V((RuleFunction)(args_92 => {

var values_93 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)8.0d))));
V v_94 = values_93[0];

R.Call(v_47, R.Join(new V[] {}, new Results(v_94)));

var values_95 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_94))));
V v_96 = values_95[0];
{ double end_99 = R.Num((V)30.0d), step_100 = R.Num((V)1.0d);
for (double i_98 = R.Num((V)1.0d); step_100 > 0 ? i_98 <= end_99 : i_98 >= end_99; i_98 += step_100) {
V v_97 = i_98;
var values_101 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_94, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_102 = values_101[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_102, (V)"valid"), () => (V)(R.Get(v_102, (V)"fate_power") == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_17, (V)"preview_hint"), R.Join(new V[] {R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_94))).First, (V)"basic_attack"}, new Results(v_102))).First, "find", R.Join(new V[] {(V)"commit", (V)1.0d}, new Results((V)true)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_94}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_94}, new Results(R.Table(new Entry((V)"kind", (V)"cancel"))))).First)))));
}
}
R.Call(v_3, R.Join(new V[] {v_96}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_94)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"every numbered or special outcome consumes one bounded RNG draw on a committed empty attack"}, new Results(new V((RuleFunction)(args_103 => {

{ double end_106 = R.Num((V)8.0d), step_107 = R.Num((V)1.0d);
for (double i_105 = R.Num((V)1.0d); step_107 > 0 ? i_105 <= end_106 : i_105 >= end_106; i_105 += step_107) {
V v_104 = i_105;
var values_108 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results(v_104))));
V v_109 = values_108[0];
V v_110 = values_108[1];

var values_111 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", R.Get(R.Get(v_109, (V)"rng"), (V)"state")))));
V v_112 = values_111[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_13, (V)"next_int"), R.Join(new V[] {v_112}, new Results((V)8.0d))).First == v_104))));
var values_113 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_109))));
V v_114 = values_113[0];

R.Call(v_79, R.Join(new V[] {v_114}, new Results(v_104)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_109, (V)"rng"), (V)"state") == R.Get(v_112, (V)"state")), () => (V)(R.Get(v_109, (V)"clock") == (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_110, (V)"boon_memory"), v_25), (V)"face") == v_104), () => (V)(R.Get(R.Get(R.Get(v_110, (V)"boon_memory"), v_25), (V)"roll_action_id") == (V)1.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_109))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_109)))))))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"face one heals once after real primary damage and cannot farm empty attacks or supply overflow"}, new Results(new V((RuleFunction)(args_115 => {

var values_116 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)1.0d))));
V v_117 = values_116[0];
V v_118 = values_116[1];

var values_119 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_120 = v_118;
V key_121 = (V)"hp";
R.Set(obj_120, key_121, values_119[0]);

R.Call(v_47, R.Join(new V[] {}, new Results(v_117)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_117, R.Get(v_118, (V)"id"), (V)"core:boon/overflow_chalice", (V)"rare"}, new Results((V)1.0d)));
var values_122 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_117))));
V v_123 = values_122[0];

R.Call(v_79, R.Join(new V[] {v_123}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_118, (V)"hp") == (V)13.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_123}, new Results((V)"Healed"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(v_118, (V)"boon_memory"), (V)"core:boon/overflow_chalice"))))));
var values_124 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)1.0d))));
V v_125 = values_124[0];
V v_126 = values_124[1];

var values_127 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_128 = v_126;
V key_129 = (V)"hp";
R.Set(obj_128, key_129, values_127[0]);
var values_130 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_125))));
v_123 = values_130[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_126, (V)"hp") == (V)10.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_123}, new Results((V)"Healed"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"face two boosts the primary batch before defense without changing action cost"}, new Results(new V((RuleFunction)(args_131 => {

var values_132 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)2.0d))));
V v_133 = values_132[0];

var values_134 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_133))));
V v_135 = values_134[0];
var values_136 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_133, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"cost")));
V v_137 = values_136[0];
var values_138 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_133))));
V v_139 = values_138[0];

R.Call(v_79, R.Join(new V[] {v_139}, new Results((V)2.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_135, (V)"hp") == (V)94.0d), () => (V)(R.Get(v_133, (V)"clock") == v_137)))));
foreach (var row_140 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_139)))))) {
V v_141 = row_140[0];
V v_142 = row_140[1];
if (R.Truth((V)(R.Get(v_142, (V)"kind") == (V)"AttackPerformed"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_142, (V)"raw_damage") == (V)7.0d))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"face three ignites actual hit targets for three real scheduled ticks"}, new Results(new V((RuleFunction)(args_143 => {

var values_144 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)3.0d))));
V v_145 = values_144[0];

var values_146 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_145))));
V v_147 = values_146[0];
var values_148 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_145))));
V v_149 = values_148[0];

R.Call(v_79, R.Join(new V[] {v_149}, new Results((V)3.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_147, (V)"statuses"), (V)"burning"), () => (V)(R.Get(R.Get(R.Get(v_147, (V)"statuses"), (V)"burning"), (V)"power") == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_147, (V)"statuses"), (V)"burning"), (V)"remaining") == (V)2.0d), () => (V)(R.Get(R.Get(R.Get(v_147, (V)"statuses"), (V)"burning"), (V)"next_due") == (V)200.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_69, R.Join(new V[] {v_149}, new Results((V)"StatusApplied"))).First == (V)1.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_149}, new Results((V)"StatusTick"))).First == (V)1.0d)), () => (V)(R.Get(v_147, (V)"hp") == (V)95.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_145))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_145)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"face four stores a finite one-hit ward without erasing saved face or renewing on other rolls"}, new Results(new V((RuleFunction)(args_150 => {

var values_151 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)4.0d))));
V v_152 = values_151[0];
V v_153 = values_151[1];

R.Call(v_47, R.Join(new V[] {}, new Results(v_152)));
var values_154 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_152))));
V v_155 = values_154[0];

R.Call(v_79, R.Join(new V[] {v_155}, new Results((V)4.0d)));
var values_156 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_153, (V)"boon_memory"), v_25)));
V v_157 = values_156[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_157, (V)"ward") == (V)3.0d), () => (V)(R.Get(v_157, (V)"expires_at") == (V)200.0d)), () => (V)(R.Get(v_157, (V)"face") == (V)4.0d)))));
var values_158 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", R.Get(R.Get(v_152, (V)"rng"), (V)"state")))));
V v_159 = values_158[0];

var values_160 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"next_int"), R.Join(new V[] {v_159}, new Results((V)8.0d))));
V v_161 = values_160[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_161 != (V)4.0d))));
R.Call(v_61, R.Join(new V[] {}, new Results(v_152)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_157, (V)"expires_at") == (V)200.0d), () => (V)(R.Get(v_157, (V)"face") == v_161)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_9, (V)"consume_ward"), R.Join(new V[] {v_152, v_153}, new Results((V)9.0d))).First == (V)3.0d), () => (V)(R.Get(v_157, (V)"ward") == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"face five arcs from the actual first victim to one other nearby foe"}, new Results(new V((RuleFunction)(args_162 => {

var values_163 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)5.0d))));
V v_164 = values_163[0];

var values_165 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_164))));
V v_166 = values_165[0];

var values_167 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_164, (V)4.0d}, new Results((V)2.0d))));
V v_168 = values_167[0];
var values_169 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_164))));
V v_170 = values_169[0];

R.Call(v_79, R.Join(new V[] {v_170}, new Results((V)5.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_166, (V)"hp") == (V)97.0d), () => (V)(R.Get(v_168, (V)"hp") == (V)98.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_69, R.Join(new V[] {v_170}, new Results((V)"EffectApplied"))).First == (V)1.0d))));
foreach (var row_171 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_170)))))) {
V v_172 = row_171[0];
V v_173 = row_171[1];
if (R.Truth(R.And((V)(R.Get(v_173, (V)"kind") == (V)"DamageTaken"), () => (V)(!R.Truth(R.Get(v_173, (V)"direct_attack")))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_173, (V)"target_id") == R.Get(v_168, (V)"id")), () => (V)(R.Get(v_173, (V)"damage_type") == (V)"lightning")))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"face six produces one victim-centered burst after the complete primary sweep batch"}, new Results(new V((RuleFunction)(args_174 => {

var values_175 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)6.0d))));
V v_176 = values_175[0];
V v_177 = values_175[1];

var values_178 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_179 = R.Get(v_177, (V)"position");
V key_180 = (V)"x";
var obj_181 = R.Get(v_177, (V)"position");
V key_182 = (V)"y";
R.Set(obj_179, key_180, values_178[0]);
R.Set(obj_181, key_182, values_178[1]);

R.Call(R.Get(v_19, (V)"recompute"), R.Join(new V[] {}, new Results(v_176)));
var values_183 = R.Join(new V[] {}, new Results(R.Table()));
V v_184 = values_183[0];
foreach (var row_185 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_176, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))).First, (V)"cells"))))))) {
V v_186 = row_185[0];
V v_187 = row_185[1];
var values_188 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_176, R.Get(v_187, (V)"x")}, new Results(R.Get(v_187, (V)"y")))));
var obj_189 = v_184;
V key_190 = (V)((V)R.Length(v_184) + (V)1.0d);
R.Set(obj_189, key_190, values_188[0]);
}
var values_191 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {v_176}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_192 = values_191[0];

R.Call(v_79, R.Join(new V[] {v_192}, new Results((V)6.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_69, R.Join(new V[] {v_192}, new Results((V)"EffectApplied"))).First == (V)1.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_192}, new Results((V)"BoonActivated"))).First == (V)1.0d)))));
var values_193 = Results.Empty;
V v_194 = values_193[0];
V v_195 = values_193[1];
foreach (var row_196 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_192)))))) {
V v_197 = row_196[0];
V v_198 = row_196[1];
if (R.Truth(R.And((V)(R.Get(v_198, (V)"kind") == (V)"DamageBatchResolved"), () => (V)(!R.Truth(v_194))))) {
var values_199 = R.Join(new V[] {}, new Results(v_197));
v_194 = values_199[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_198, (V)"outcomes")) == (V)3.0d))));
}
if (R.Truth(R.And((V)(R.Get(v_198, (V)"kind") == (V)"BoonActivated"), () => (V)(R.Get(v_198, (V)"boon_id") == v_25)))) {
var values_200 = R.Join(new V[] {}, new Results(v_197));
v_195 = values_200[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_194 < v_195), () => (V)(R.Get(v_177, (V)"hp") == R.Get(v_177, (V)"max_hp"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_184, (V)1.0d), (V)"hp") < (V)100.0d), () => (V)(R.Get(R.Get(v_184, (V)2.0d), (V)"hp") < (V)100.0d)), () => (V)(R.Get(R.Get(v_184, (V)3.0d), (V)"hp") < (V)100.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a landed skull kills an ordinary full-health foe through normal defense and rewards once"}, new Results(new V((RuleFunction)(args_201 => {

var values_202 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)7.0d))));
V v_203 = values_202[0];

var values_204 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_203))));
V v_205 = values_204[0];

var values_206 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_203))));
V v_207 = values_206[0];

R.Call(v_79, R.Join(new V[] {v_207}, new Results((V)7.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_203}, new Results(R.Get(v_205, (V)"id")))).First)), () => (V)(R.Call(v_69, R.Join(new V[] {v_207}, new Results((V)"EntityKilled"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_69, R.Join(new V[] {v_207}, new Results((V)"FateSkullResisted"))).First == (V)0.0d))));
var values_208 = R.Join(new V[] {}, new Results((V)0.0d));
V v_209 = values_208[0];
foreach (var row_210 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_203, (V)"drops"))))))) {
V v_211 = row_210[0];
V v_212 = row_210[1];
if (R.Truth(R.And((V)(R.Get(v_212, (V)"kind") == (V)"currency"), () => (V)(R.Get(v_212, (V)"killed_entity_id") == R.Get(v_205, (V)"id"))))) {
var values_213 = R.Join(new V[] {}, new Results((V)(v_209 + (V)1.0d)));
v_209 = values_213[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_209 == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"skull excludes every biome miniboss, finale, pursuer and special champion"}, new Results(new V((RuleFunction)(args_214 => {

var values_215 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:enemy/hollow_regent"), new Entry((V)2, (V)"core:enemy/extraction_pursuer"), new Entry((V)3, (V)"core:enemy/rotating_sentinel"))));
V v_216 = values_215[0];
foreach (var row_217 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))).First, (V)"order"))))))) {
V v_218 = row_217[0];
V v_219 = row_217[1];
var values_220 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))).First, (V)"get"), R.Join(new V[] {}, new Results(v_219))).First, (V)"elite_id")));
var obj_221 = v_216;
V key_222 = (V)((V)R.Length(v_216) + (V)1.0d);
R.Set(obj_221, key_222, values_220[0]);
}
foreach (var row_223 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_216)))))) {
V v_224 = row_223[0];
V v_225 = row_223[1];
var values_226 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)7.0d))));
V v_227 = values_226[0];

var values_228 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_227, (V)3.0d, (V)2.0d}, new Results(v_225))));
V v_229 = values_228[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(R.Get(v_9, (V)"skull_target"), R.Join(new V[] {}, new Results(v_229))).First))}, new Results(v_225)));
var values_230 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_227))));
V v_231 = values_230[0];

R.Call(v_79, R.Join(new V[] {v_231}, new Results((V)7.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_227}, new Results(R.Get(v_229, (V)"id")))).First, () => (V)(R.Get(v_229, (V)"hp") >= (V)96.0d))}, new Results(v_225)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(v_69, R.Join(new V[] {v_231}, new Results((V)"FateSkullResisted"))).First == (V)1.0d)}, new Results(v_225)));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"skull cannot bypass reward-exempt resurrection or a living wizard forcefield"}, new Results(new V((RuleFunction)(args_232 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_9, (V)"skull_target"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"controller", (V)"ai"), new Entry((V)"faction", (V)"player"), new Entry((V)"definition_id", (V)"core:enemy/mossbound_guard"))))).First)))));
var values_233 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)7.0d))));
V v_234 = values_233[0];

var values_235 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_234))));
V v_236 = values_235[0];

var values_237 = R.Join(new V[] {}, new Results((V)true));
var obj_238 = v_236;
V key_239 = (V)"reward_exempt";
R.Set(obj_238, key_239, values_237[0]);
var values_240 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_234))));
V v_241 = values_240[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_236, (V)"hp") == (V)97.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_241}, new Results((V)"FateSkullResisted"))).First == (V)1.0d)))));
var values_242 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)7.0d))));
v_234 = values_242[0];

var values_243 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_234, (V)3.0d, (V)2.0d}, new Results((V)"core:enemy/bodyguard_brute"))));
v_236 = values_243[0];
var values_244 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_234, (V)"core:enemy/ward_weaver", (V)4.0d, (V)2.0d}, new Results((V)100000.0d))));
V v_245 = values_244[0];
var values_246 = R.Join(new V[] {}, new Results((V)true));
var obj_247 = R.Get(v_245, (V)"ai_memory");
V key_248 = (V)"ward_ready";
R.Set(obj_247, key_248, values_246[0]);
foreach (var row_249 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_236), new Entry((V)2, v_245)))))))) {
V v_250 = row_249[0];
V v_251 = row_249[1];
var values_252 = R.Join(new V[] {}, new Results((V)"dice-ward"));
var obj_253 = v_251;
V key_254 = (V)"squad_id";
R.Set(obj_253, key_254, values_252[0]);

var values_255 = R.Join(new V[] {}, new Results((V)"warded_bodyguards"));
var obj_256 = v_251;
V key_257 = (V)"squad_kind";
R.Set(obj_256, key_257, values_255[0]);
var values_258 = R.Join(new V[] {}, new Results(R.Get(v_245, (V)"id")));
var obj_259 = v_251;
V key_260 = (V)"squad_leader_id";
R.Set(obj_259, key_260, values_258[0]);
var values_261 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_234, (V)"active_floor_id")), new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)2.0d))));
var obj_262 = v_251;
V key_263 = (V)"squad_home";
R.Set(obj_262, key_263, values_261[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_234)))))));
var values_264 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_234))));
v_241 = values_264[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_234}, new Results(R.Get(v_236, (V)"id")))).First, () => (V)(R.Get(v_236, (V)"hp") == (V)100.0d)), () => (V)(R.Call(v_69, R.Join(new V[] {v_241}, new Results((V)"FateSkullResisted"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_69, R.Join(new V[] {v_241}, new Results((V)"EntityKilled"))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"skull executes the selected ordinary Oath twin without converting execution to shared chip damage"}, new Results(new V((RuleFunction)(args_265 => {

var values_266 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)7.0d))));
V v_267 = values_266[0];
var values_268 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_267, (V)3.0d, (V)2.0d}, new Results(R.Get(v_23, (V)"a")))));
V v_269 = values_268[0];

var values_270 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_267, (V)4.0d, (V)2.0d}, new Results(R.Get(v_23, (V)"b")))));
V v_271 = values_270[0];
var values_272 = R.Join(new V[] {}, new Results(R.Get(v_271, (V)"id")));
var obj_273 = v_269;
V key_274 = (V)"oath_partner_id";
R.Set(obj_273, key_274, values_272[0]);

var values_275 = R.Join(new V[] {}, new Results(R.Get(v_269, (V)"id")));
var obj_276 = v_271;
V key_277 = (V)"oath_partner_id";
R.Set(obj_276, key_277, values_275[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_23, (V)"partner"), R.Join(new V[] {v_267}, new Results(v_269))).First == v_271))));
var values_278 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_267))));
V v_279 = values_278[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_267}, new Results(R.Get(v_269, (V)"id")))).First)), () => (V)(R.Get(v_271, (V)"hp") == (V)100.0d)), () => R.Get(v_271, (V)"oath_broken")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_69, R.Join(new V[] {v_279}, new Results((V)"EntityKilled"))).First == (V)1.0d))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_267))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_267)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a skull sweep evaluates the living forcefield for every target before committing wizard death"}, new Results(new V((RuleFunction)(args_280 => {

var values_281 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)7.0d))));
V v_282 = values_281[0];
V v_283 = values_281[1];

var values_284 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_285 = R.Get(v_283, (V)"position");
V key_286 = (V)"x";
var obj_287 = R.Get(v_283, (V)"position");
V key_288 = (V)"y";
R.Set(obj_285, key_286, values_284[0]);
R.Set(obj_287, key_288, values_284[1]);

R.Call(R.Get(v_19, (V)"recompute"), R.Join(new V[] {}, new Results(v_282)));
var values_289 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_282, (V)"core:enemy/ward_weaver", (V)8.0d, (V)5.0d}, new Results((V)100000.0d))));
V v_290 = values_289[0];
var values_291 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_282, (V)8.0d, (V)6.0d}, new Results((V)"core:enemy/bodyguard_brute"))));
V v_292 = values_291[0];

var values_293 = R.Join(new V[] {}, new Results((V)true));
var obj_294 = R.Get(v_290, (V)"ai_memory");
V key_295 = (V)"ward_ready";
R.Set(obj_294, key_295, values_293[0]);
foreach (var row_296 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_290), new Entry((V)2, v_292)))))))) {
V v_297 = row_296[0];
V v_298 = row_296[1];
var values_299 = R.Join(new V[] {}, new Results((V)"skull-batch"));
var obj_300 = v_298;
V key_301 = (V)"squad_id";
R.Set(obj_300, key_301, values_299[0]);

var values_302 = R.Join(new V[] {}, new Results((V)"warded_bodyguards"));
var obj_303 = v_298;
V key_304 = (V)"squad_kind";
R.Set(obj_303, key_304, values_302[0]);
var values_305 = R.Join(new V[] {}, new Results(R.Get(v_290, (V)"id")));
var obj_306 = v_298;
V key_307 = (V)"squad_leader_id";
R.Set(obj_306, key_307, values_305[0]);
var values_308 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_282, (V)"active_floor_id")), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)5.0d))));
var obj_309 = v_298;
V key_310 = (V)"squad_home";
R.Set(obj_309, key_310, values_308[0]);
}
var values_311 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {v_282}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_312 = values_311[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_282}, new Results(R.Get(v_290, (V)"id")))).First)), () => (V)(R.Get(v_292, (V)"hp") == (V)100.0d)), () => (V)(R.Call(v_69, R.Join(new V[] {v_312}, new Results((V)"EntityKilled"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_69, R.Join(new V[] {v_312}, new Results((V)"FateSkullResisted"))).First == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a cross is a genuine miss with no hidden hit, Burning, Storm charge or newly queued Echo"}, new Results(new V((RuleFunction)(args_313 => {

var values_314 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {(V)8.0d}, new Results((V)"core:actor/stormcaller"))));
V v_315 = values_314[0];
V v_316 = values_314[1];

var values_317 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_315))));
V v_318 = values_317[0];
foreach (var row_319 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"temporal_echo"), new Entry((V)2, (V)"emberbrand"), new Entry((V)3, (V)"storm_conductor")))))))) {
V v_320 = row_319[0];
V v_321 = row_319[1];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_315, R.Get(v_316, (V)"id"), R.Concat((V)"core:boon/", v_321), (V)"rare"}, new Results((V)1.0d)));
}
var values_322 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_315))));
V v_323 = values_322[0];

R.Call(v_79, R.Join(new V[] {v_323}, new Results((V)8.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_318, (V)"hp") == (V)100.0d), () => (V)(!R.Truth(R.Get(v_318, (V)"statuses")))), () => (V)(R.Or(R.Get(v_316, (V)"storm_charge"), () => (V)0.0d) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_69, R.Join(new V[] {v_323}, new Results((V)"DamageBatchResolved"))).First == (V)0.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_323}, new Results((V)"HitConfirmed"))).First == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_69, R.Join(new V[] {v_323}, new Results((V)"BoonActivated"))).First == (V)0.0d), () => (V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_315, (V)"delayed_effects")))).First))))));
foreach (var row_324 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_323)))))) {
V v_325 = row_324[0];
V v_326 = row_324[1];
if (R.Truth((V)(R.Get(v_326, (V)"kind") == (V)"AttackPerformed"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(v_326, (V)"fate_miss"), () => (V)((V)R.Length(R.Get(v_326, (V)"target_cells")) == (V)0.0d)), () => (V)((V)R.Length(R.Get(v_326, (V)"aim_cells")) > (V)0.0d)), () => (V)(R.Get(v_326, (V)"raw_damage") == (V)0.0d)))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a missed attack cannot break a barricade and trigger hidden terrain-boon damage"}, new Results(new V((RuleFunction)(args_327 => {

var values_328 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)8.0d))));
V v_329 = values_328[0];
V v_330 = values_328[1];
var values_331 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"map_for"), R.Join(new V[] {}, new Results(v_329))))))));
var obj_332 = v_329;
V key_333 = (V)"generated_map";
R.Set(obj_332, key_333, values_331[0]);
var values_334 = R.Join(new V[] {}, new Results(R.Get(v_329, (V)"generated_map")));
V v_335 = values_334[0];

var values_336 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_335, (V)"rows"), (V)2.0d), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)2.0d))).First, R.Concat((V)"B", R.Invoke(R.Get(R.Get(v_335, (V)"rows"), (V)2.0d), "sub", R.Join(new V[] {}, new Results((V)4.0d))).First))));
var obj_337 = R.Get(v_335, (V)"rows");
V key_338 = (V)2.0d;
R.Set(obj_337, key_338, values_336[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_329, R.Get(v_330, (V)"id"), (V)"core:boon/fracture_dividend", (V)"rare"}, new Results((V)1.0d)));
var values_339 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_329))));
V v_340 = values_339[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Invoke(R.Get(R.Get(v_335, (V)"rows"), (V)2.0d), "sub", R.Join(new V[] {(V)3.0d}, new Results((V)3.0d))).First == (V)"B"), () => (V)(R.Call(v_69, R.Join(new V[] {v_340}, new Results((V)"TerrainBroken"))).First == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_69, R.Join(new V[] {v_340}, new Results((V)"EffectApplied"))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a rolled cross also misses the Hookrunner pull instead of displacing an unhit foe"}, new Results(new V((RuleFunction)(args_341 => {

var values_342 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {(V)8.0d}, new Results((V)"core:actor/hookrunner"))));
V v_343 = values_342[0];

var values_344 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {v_343, (V)5.0d}, new Results((V)2.0d))));
V v_345 = values_344[0];
var values_346 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {v_343}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_347 = values_346[0];

R.Call(v_79, R.Join(new V[] {v_347}, new Results((V)8.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_345, (V)"hp") == (V)100.0d), () => (V)(R.Get(R.Get(v_345, (V)"position"), (V)"x") == (V)5.0d)), () => (V)(R.Get(R.Get(v_345, (V)"position"), (V)"y") == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_69, R.Join(new V[] {v_347}, new Results((V)"Displaced"))).First == (V)0.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_347}, new Results((V)"DamageTaken"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a cross does not cancel an earlier committed Echo and does not create another one"}, new Results(new V((RuleFunction)(args_348 => {

var values_349 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results(R.Get(v_27, (V)8.0d)))));
V v_350 = values_349[0];

var values_351 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_350))));
V v_352 = values_351[0];

var values_353 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_350))));
V v_354 = values_353[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_350, R.Get(v_352, (V)"id"), (V)"core:boon/temporal_echo", (V)"rare"}, new Results((V)1.0d)));
R.Call(v_61, R.Join(new V[] {}, new Results(v_350)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_350, (V)"delayed_effects"))))));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_350, R.Get(v_352, (V)"id"), v_25, (V)"rare"}, new Results((V)1.0d)));
var values_355 = R.Join(new V[] {}, new Results(R.Get(v_354, (V)"hp")));
V v_356 = values_355[0];

var values_357 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_350))));
V v_358 = values_357[0];

R.Call(v_79, R.Join(new V[] {v_358}, new Results((V)8.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_69, R.Join(new V[] {v_358}, new Results((V)"DelayedEffectScheduled"))).First == (V)0.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_358}, new Results((V)"DelayedEffectResolved"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_354, (V)"hp") < v_356), () => (V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_350, (V)"delayed_effects")))).First))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Pilgrim still pays on a rolled miss and heavy dice combine before one critical"}, new Results(new V((RuleFunction)(args_359 => {

foreach (var row_360 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)8.0d), new Entry((V)2, (V)2.0d)))))))) {
V v_361 = row_360[0];
V v_362 = row_360[1];
var values_363 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results(v_362))));
V v_364 = values_363[0];
V v_365 = values_363[1];

var values_366 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_367 = v_365;
V key_368 = (V)"hp";
R.Set(obj_367, key_368, values_366[0]);

var values_369 = R.Join(new V[] {}, R.Call(v_47, R.Join(new V[] {}, new Results(v_364))));
V v_370 = values_369[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_364, R.Get(v_365, (V)"id"), (V)"core:boon/pilgrims_vow", (V)"rare"}, new Results((V)1.0d)));
if (R.Truth((V)(v_362 == (V)2.0d))) {
var values_371 = R.Join(new V[] {}, new Results((V)1000000.0d));
var obj_372 = v_365;
V key_373 = (V)"crit_chance_ppm";
R.Set(obj_372, key_373, values_371[0]);
}
var values_374 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {v_364}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true))))));
V v_375 = values_374[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_365, (V)"hp") == (V)8.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_375}, new Results((V)"VowPaid"))).First == (V)1.0d)))));

R.Call(v_79, R.Join(new V[] {v_375}, new Results(v_362)));
if (R.Truth((V)(v_362 == (V)8.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_370, (V)"hp") == (V)100.0d))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_370, (V)"hp") == (V)((V)100.0d - (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)(R.Get(v_365, (V)"damage") + (V)6.0d) + (V)3.0d) * R.Get(v_365, (V)"crit_damage_percent")) / (V)100.0d) + (V)0.5d)))).First - R.Get(v_370, (V)"defense")))))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"uncapped mixed rarity dice copies increase power without extra rolls or extra primary events"}, new Results(new V((RuleFunction)(args_376 => {

var values_377 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)4.0d))));
V v_378 = values_377[0];
V v_379 = values_377[1];

R.Call(v_47, R.Join(new V[] {}, new Results(v_378)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_378, R.Get(v_379, (V)"id"), v_25, (V)"common"}, new Results((V)10.0d)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_378, R.Get(v_379, (V)"id"), v_25, (V)"legendary"}, new Results((V)20.0d)));
var values_380 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", R.Get(R.Get(v_378, (V)"rng"), (V)"state")))));
V v_381 = values_380[0];

R.Call(R.Get(v_13, (V)"next_int"), R.Join(new V[] {v_381}, new Results((V)8.0d)));
var values_382 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_378))));
V v_383 = values_382[0];

R.Call(v_79, R.Join(new V[] {v_383}, new Results((V)4.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_378, (V)"rng"), (V)"state") == R.Get(v_381, (V)"state")), () => (V)(R.Get(R.Get(R.Get(v_379, (V)"boon_memory"), v_25), (V)"ward") == (V)93.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_69, R.Join(new V[] {v_383}, new Results((V)"AttackPerformed"))).First == (V)1.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_383}, new Results((V)"BoonActivated"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all fifteen basic attacks roll once while utility placement and generated sentry shots never roll"}, new Results(new V((RuleFunction)(args_384 => {

foreach (var row_385 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_17, (V)"order"))))))) {
V v_386 = row_385[0];
V v_387 = row_385[1];
var values_388 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {(V)8.0d}, new Results(v_387))));
V v_389 = values_388[0];
V v_390 = values_388[1];

R.Call(v_47, R.Join(new V[] {}, new Results(v_389)));
var values_391 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {}, new Results(v_389))));
V v_392 = values_391[0];

R.Call(v_79, R.Join(new V[] {v_392}, new Results((V)8.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_69, R.Join(new V[] {v_392}, new Results((V)"HitConfirmed"))).First == (V)0.0d), () => (V)(R.Get(R.Get(R.Get(v_390, (V)"boon_memory"), v_25), (V)"face") == (V)8.0d)))));
}
var values_393 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {(V)4.0d}, new Results((V)"core:actor/tinkerer"))));
V v_394 = values_393[0];
V v_395 = values_393[1];

R.Call(v_47, R.Join(new V[] {v_394, (V)5.0d}, new Results((V)2.0d)));
var values_396 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_394, (V)"rng"), (V)"state")));
V v_397 = values_396[0];
var values_398 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {v_394}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_399 = values_398[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_69, R.Join(new V[] {v_399}, new Results((V)"FateRolled"))).First == (V)0.0d))));
{ double end_402 = R.Num((V)6.0d), step_403 = R.Num((V)1.0d);
for (double i_401 = R.Num((V)1.0d); step_403 > 0 ? i_401 <= end_402 : i_401 >= end_402; i_401 += step_403) {
V v_400 = i_401;
var values_404 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {v_394}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
v_399 = values_404[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_69, R.Join(new V[] {v_399}, new Results((V)"FateRolled"))).First == (V)0.0d))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_395, (V)"boon_memory"))), () => (V)(R.Get(R.Get(v_394, (V)"rng"), (V)"state") == v_397)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"saved last roll and active ward continue exactly with pending Echo and later real dice outcomes"}, new Results(new V((RuleFunction)(args_405 => {

var values_406 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)4.0d))));
V v_407 = values_406[0];
V v_408 = values_406[1];

R.Call(v_47, R.Join(new V[] {}, new Results(v_407)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_407, R.Get(v_408, (V)"id"), (V)"core:boon/temporal_echo", (V)"rare"}, new Results((V)1.0d)));
R.Call(v_61, R.Join(new V[] {}, new Results(v_407)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_408, (V)"boon_memory"), v_25), (V)"ward") == (V)3.0d), () => R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_407, (V)"delayed_effects")))).First))));
var values_409 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_407))))))))));
V v_410 = values_409[0];
foreach (var row_411 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)"kind", (V)"wait")))))))))) {
V v_412 = row_411[0];
V v_413 = row_411[1];
R.Call(v_3, R.Join(new V[] {R.Call(v_61, R.Join(new V[] {v_407}, new Results(v_413))).First}, R.Call(v_61, R.Join(new V[] {v_410}, new Results(v_413)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_407))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_410)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a commanded Spirit roll is saved on the real owner rather than its temporary attack proxy"}, new Results(new V((RuleFunction)(args_414 => {

var values_415 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {(V)4.0d}, new Results((V)"core:actor/spiritbinder"))));
V v_416 = values_415[0];
V v_417 = values_415[1];
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))).First, (V)"place"), R.Join(new V[] {v_416, v_417, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))}, new Results(new V((RuleFunction)(args_418 => {


return Results.Empty;
})))));
R.Call(v_47, R.Join(new V[] {v_416, (V)4.0d}, new Results((V)2.0d)));
var values_419 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {v_416}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_420 = values_419[0];

R.Call(v_79, R.Join(new V[] {v_420}, new Results((V)4.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_417, (V)"boon_memory"), v_25), (V)"face") == (V)4.0d), () => (V)(R.Get(R.Get(R.Get(v_417, (V)"boon_memory"), v_25), (V)"ward") == (V)3.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_416))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_416)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"saved dice faces, roll IDs and malformed face predicates reject before activation"}, new Results(new V((RuleFunction)(args_421 => {

var values_422 = R.Join(new V[] {}, R.Call(v_41, R.Join(new V[] {}, new Results((V)4.0d))));
V v_423 = values_422[0];

R.Call(v_47, R.Join(new V[] {}, new Results(v_423)));

R.Call(v_61, R.Join(new V[] {}, new Results(v_423)));
var values_424 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_423))));
V v_425 = values_424[0];
foreach (var row_426 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)9.0d), new Entry((V)3, (V)1.5d)))))))) {
V v_427 = row_426[0];
V v_428 = row_426[1];
var values_429 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, new Results(v_425))))));
V v_430 = values_429[0];

var values_431 = R.Join(new V[] {}, new Results(v_428));
var obj_432 = R.Get(R.Get(R.Get(R.Get(v_430, (V)"entities"), (V)1.0d), (V)"boon_memory"), v_25);
V key_433 = (V)"face";
R.Set(obj_432, key_433, values_431[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_430))).First)))));
}
var values_434 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, new Results(v_425))))));
V v_435 = values_434[0];

var values_436 = R.Join(new V[] {}, new Results(R.Get(v_435, (V)"next_action_id")));
var obj_437 = R.Get(R.Get(R.Get(R.Get(v_435, (V)"entities"), (V)1.0d), (V)"boon_memory"), v_25);
V key_438 = (V)"roll_action_id";
R.Set(obj_437, key_438, values_436[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_435))).First)))));
var values_439 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"get"), R.Join(new V[] {}, new Results(v_25))))))));
V v_440 = values_439[0];

var values_441 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"op", (V)"roll_face"), new Entry((V)"value", (V)9.0d))));
var obj_442 = v_440;
V key_443 = (V)"predicate";
R.Set(obj_442, key_443, values_441[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"validate"), R.Table(new Entry(v_25, v_440))}, new Results(R.Table(new Entry((V)1, v_25))))).First)))));
R.Call(v_3, R.Join(new V[] {v_425}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_423)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
