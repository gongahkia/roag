// Static C# port of tests/idea_mirror_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: bec6efe0d3aa227bb096b0eceec891731f322b5e766065ff7580df65cc12fc04
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_mirror_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_mirrors"))));
V v_9 = values_8[0];

var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_11 = values_10[0];
V v_12 = V.Nil;
v_12 = new V((RuleFunction)(args_13 => {

var values_14 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/ranger"))))));
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
var values_23 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_15, R.Get(v_9, (V)"id"), (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
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
V v_45 = V.Nil;
v_45 = new V((RuleFunction)(args_46 => {

var values_47 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_48 = values_47[0];
V v_49 = values_47[1];
V v_50 = values_47[2];

R.Call(v_25, R.Join(new V[] {v_48}, new Results((V)3.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_48, (V)"clock") == (V)261.0d), () => (V)((V)R.Length(R.Get(v_48, (V)"idea_mirrors")) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_48, (V)"idea_mirrors"), (V)1.0d), (V)"expires") == (V)626.0d)))));
return R.Join(new V[] {v_48, v_49, v_50}, new Results(R.Get(R.Get(v_48, (V)"idea_mirrors"), (V)1.0d)));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"a visible Hexer commits one locked directional marker with a finite spent cast"}, new Results(new V((RuleFunction)(args_51 => {

var values_52 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_53 = values_52[0];
V v_54 = values_52[1];
V v_55 = values_52[2];

var values_56 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_53))));
V v_57 = values_56[0];
{ double end_60 = R.Num((V)30.0d), step_61 = R.Num((V)1.0d);
for (double i_59 = R.Num((V)1.0d); step_61 > 0 ? i_59 <= end_60 : i_59 >= end_60; i_59 += step_61) {
V v_58 = i_59;
var values_62 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"choose"), R.Join(new V[] {v_53}, new Results(v_55))));
V v_63 = values_62[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_63, (V)"support"), (V)"kind") == (V)"mirror"), () => (V)(R.Get(R.Get(v_63, (V)"support"), (V)"x") == (V)9.0d)), () => (V)(R.Get(R.Get(v_63, (V)"support"), (V)"in_dx") == (V)1.0d)))));
}
}
R.Call(v_3, R.Join(new V[] {v_57}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_53)))));

R.Call(v_25, R.Join(new V[] {v_53}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_55, (V)"pending_attack"), (V)"due") == (V)226.0d), () => (V)(R.Get(R.Get(v_55, (V)"ai_memory"), (V)"mirror_casts") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(R.Get(v_55, (V)"pending_attack"), (V)"target_cells"), (V)1.0d), (V)"x") == (V)9.0d), () => (V)(!R.Truth(R.Get(v_53, (V)"idea_mirrors")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_53)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"repeated player preview shows both legs without consuming the mirror RNG or time"}, new Results(new V((RuleFunction)(args_64 => {

var values_65 = R.Join(new V[] {}, R.Call(v_45, Results.Empty));
V v_66 = values_65[0];
V v_67 = values_65[1];
V v_68 = values_65[2];

var values_69 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_66))));
V v_70 = values_69[0];
{ double end_73 = R.Num((V)30.0d), step_74 = R.Num((V)1.0d);
for (double i_72 = R.Num((V)1.0d); step_74 > 0 ? i_72 <= end_73 : i_72 >= end_73; i_72 += step_74) {
V v_71 = i_72;
var values_75 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_66, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_76 = values_75[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_76, (V)"valid"), () => (V)(R.Get(v_76, (V)"cost") == (V)87.0d)), () => (V)(R.Get(R.Get(v_76, (V)"reflection"), (V)"step") == (V)2.0d)))));
foreach (var row_77 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_76, (V)"cells"))))))) {
V v_78 = row_77[0];
V v_79 = row_77[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_79, (V)"x") == R.Get(R.Table(new Entry((V)1, (V)8.0d), new Entry((V)2, (V)9.0d), new Entry((V)3, (V)9.0d), new Entry((V)4, (V)9.0d)), v_78)), () => (V)(R.Get(v_79, (V)"y") == R.Get(R.Table(new Entry((V)1, (V)6.0d), new Entry((V)2, (V)6.0d), new Entry((V)3, (V)7.0d), new Entry((V)4, (V)8.0d)), v_78))))));
}
}
}
R.Call(v_3, R.Join(new V[] {v_70}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_66)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a real reflected bow shot hits the turned cell with original shooter damage and one consumption"}, new Results(new V((RuleFunction)(args_80 => {

var values_81 = R.Join(new V[] {}, R.Call(v_45, Results.Empty));
V v_82 = values_81[0];
V v_83 = values_81[1];
V v_84 = values_81[2];

var values_85 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_82, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)7.0d}, new Results((V)100000.0d))));
V v_86 = values_85[0];
var values_87 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_82, (V)"rng"), (V)"state")));
V v_88 = values_87[0];

var values_89 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_82}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_90 = values_89[0];
V v_91 = values_89[1];
V v_92 = values_89[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_90)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Get(v_86, (V)"hp") == (V)8.0d), () => (V)(R.Get(v_84, (V)"hp") == (V)7.0d)), () => (V)((V)R.Length(R.Get(v_82, (V)"idea_mirrors")) == (V)0.0d)), () => (V)(R.Get(v_82, (V)"clock") == (V)348.0d)), () => (V)(R.Get(R.Get(v_82, (V)"rng"), (V)"state") == v_88)))));
var values_93 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_94 = values_93[0];
V v_95 = values_93[1];
foreach (var row_96 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_92)))))) {
V v_97 = row_96[0];
V v_98 = row_96[1];
if (R.Truth((V)(R.Get(v_98, (V)"kind") == (V)"MirrorReflected"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_98, (V)"source_id") == R.Get(v_83, (V)"id")))));

var values_99 = R.Join(new V[] {}, new Results((V)(v_94 + (V)1.0d)));
v_94 = values_99[0];
}
if (R.Truth((V)(R.Get(v_98, (V)"kind") == (V)"DamageTaken"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_98, (V)"source_id") == R.Get(v_83, (V)"id")), () => (V)(R.Get(v_98, (V)"target_id") == R.Get(v_86, (V)"id"))), () => R.Get(v_98, (V)"direct_attack")))));

var values_100 = R.Join(new V[] {}, new Results((V)(v_95 + (V)1.0d)));
v_95 = values_100[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_94 == (V)1.0d), () => (V)(v_95 == (V)1.0d)), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_82))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an intervening creature killed by the same shot blocks reflection instead of disappearing early"}, new Results(new V((RuleFunction)(args_101 => {

var values_102 = R.Join(new V[] {}, R.Call(v_45, Results.Empty));
V v_103 = values_102[0];
V v_104 = values_102[1];
V v_105 = values_102[2];

var values_106 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_103, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_107 = values_106[0];
var values_108 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_109 = v_107;
V key_110 = (V)"hp";
R.Set(obj_109, key_110, values_108[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_103, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"reflection"))))));
var values_111 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_103}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_112 = values_111[0];
V v_113 = values_111[1];
V v_114 = values_111[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_112)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_103}, new Results(R.Get(v_107, (V)"id")))).First)), () => (V)((V)R.Length(R.Get(v_103, (V)"idea_mirrors")) == (V)1.0d)))));
foreach (var row_115 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_114)))))) {
V v_116 = row_115[0];
V v_117 = row_115[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_117, (V)"kind") != (V)"MirrorReflected"))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"melee area diagonal and wrong-direction attacks bypass the limited reflector"}, new Results(new V((RuleFunction)(args_118 => {

var values_119 = R.Join(new V[] {}, R.Call(v_45, Results.Empty));
V v_120 = values_119[0];
V v_121 = values_119[1];
V v_122 = values_119[2];
V v_123 = values_119[3];

var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_120))));
V v_125 = values_124[0];
var values_126 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)3, R.Table(new Entry((V)"x", (V)10.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)4, R.Table(new Entry((V)"x", (V)11.0d), new Entry((V)"y", (V)6.0d))))));
V v_127 = values_126[0];
foreach (var row_128 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)false), new Entry((V)2, (V)"area")))))))) {
V v_129 = row_128[0];
V v_130 = row_128[1];
var values_131 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"route"), R.Join(new V[] {v_120, v_121, v_127}, new Results(v_130))));
V v_132 = values_131[0];
V v_133 = values_131[1];
R.Call(v_3, R.Join(new V[] {v_132}, new Results(v_127)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_133)))));
}
var values_134 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)7.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)8.0d))), new Entry((V)3, R.Table(new Entry((V)"x", (V)10.0d), new Entry((V)"y", (V)9.0d))))));
V v_135 = values_134[0];
var values_136 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"route"), R.Join(new V[] {v_120, v_121, v_135}, new Results((V)true))));
V v_137 = values_136[0];
V v_138 = values_136[1];
R.Call(v_3, R.Join(new V[] {v_137}, new Results(v_135)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_138)))));
var values_139 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_121, (V)"id")), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_120, (V)"active_floor_id")), new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)8.0d))))));
V v_140 = values_139[0];
var values_141 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)7.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)3, R.Table(new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)5.0d))))));
V v_142 = values_141[0];
var values_143 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"route"), R.Join(new V[] {v_120, v_140, v_142}, new Results((V)true))));
v_137 = values_143[0];
v_138 = values_143[1];

R.Call(v_3, R.Join(new V[] {v_137}, new Results(v_142)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_138)))));
R.Call(v_3, R.Join(new V[] {v_125}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_120)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a turned path preserves its range and cannot bounce again from a second mirror"}, new Results(new V((RuleFunction)(args_144 => {

var values_145 = R.Join(new V[] {}, R.Call(v_45, Results.Empty));
V v_146 = values_145[0];
V v_147 = values_145[1];
V v_148 = values_145[2];
V v_149 = values_145[3];
var values_150 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_146, R.Get(v_9, (V)"id"), (V)10.0d, (V)7.0d}, new Results((V)100000.0d))));
V v_151 = values_150[0];

var values_152 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_153 = R.Get(v_151, (V)"ai_memory");
V key_154 = (V)"mirror_casts";
R.Set(obj_153, key_154, values_152[0]);
var values_155 = R.Join(new V[] {}, new Results(R.Get(v_146, (V)"next_action_id")));
V v_156 = values_155[0];

var values_157 = R.Join(new V[] {}, new Results((V)(v_156 + (V)1.0d)));
var obj_158 = v_146;
V key_159 = (V)"next_action_id";
R.Set(obj_158, key_159, values_157[0]);
var values_160 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_156), new Entry((V)"source_id", R.Get(v_151, (V)"id")), new Entry((V)"floor_id", R.Get(v_149, (V)"floor_id")), new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)7.0d), new Entry((V)"source_x", (V)9.0d), new Entry((V)"source_y", (V)8.0d), new Entry((V)"in_dx", (V)0.0d), new Entry((V)"in_dy", (V)1.0d), new Entry((V)"expires", R.Get(v_149, (V)"expires")))));
var obj_161 = R.Get(v_146, (V)"idea_mirrors");
V key_162 = (V)2.0d;
R.Set(obj_161, key_162, values_160[0]);
var values_163 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_146))));
V v_164 = values_163[0];

var values_165 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_146, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"cells")));
V v_166 = values_165[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)((V)R.Length(v_166) == (V)4.0d), () => (V)(R.Get(R.Get(v_166, (V)3.0d), (V)"x") == (V)9.0d)), () => (V)(R.Get(R.Get(v_166, (V)3.0d), (V)"y") == (V)7.0d)), () => (V)(R.Get(R.Get(v_166, (V)4.0d), (V)"x") == (V)9.0d)), () => (V)(R.Get(R.Get(v_166, (V)4.0d), (V)"y") == (V)8.0d)))));
R.Call(v_3, R.Join(new V[] {v_164}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_146)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_146)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"enemy reflection locks at windup commitment retains friendly fire and survives a fresh restore"}, new Results(new V((RuleFunction)(args_167 => {

var values_168 = R.Join(new V[] {}, R.Call(v_45, Results.Empty));
V v_169 = values_168[0];
V v_170 = values_168[1];
V v_171 = values_168[2];

var values_172 = R.Join(new V[] {(V)11.0d}, new Results((V)6.0d));
var obj_173 = R.Get(v_170, (V)"position");
V key_174 = (V)"x";
var obj_175 = R.Get(v_170, (V)"position");
V key_176 = (V)"y";
R.Set(obj_173, key_174, values_172[0]);
R.Set(obj_175, key_176, values_172[1]);
var values_177 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_169, (V)"core:enemy/thornspitter", (V)7.0d, (V)6.0d}, new Results((V)(R.Get(v_169, (V)"clock") + (V)1.0d)))));
V v_178 = values_177[0];
var values_179 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_169, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)7.0d}, new Results((V)100000.0d))));
V v_180 = values_179[0];
R.Call(v_25, R.Join(new V[] {v_169}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_178, (V)"pending_attack"), () => R.Get(R.Get(v_178, (V)"pending_attack"), (V)"reflection")), () => (V)((V)R.Length(R.Get(v_169, (V)"idea_mirrors")) == (V)0.0d)))));
var values_181 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_178, (V)"pending_attack"), (V)"target_cells")))));
V v_182 = values_181[0];
var values_183 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_169))))))))));
V v_184 = values_183[0];
var values_185 = R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {v_169}, new Results((V)1.0d))));
V v_186 = values_185[0];

R.Call(v_3, R.Join(new V[] {v_186}, R.Call(v_25, R.Join(new V[] {v_184}, new Results((V)1.0d)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_169))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_184)))));
var values_187 = R.Join(new V[] {}, new Results((V)false));
V v_188 = values_187[0];
foreach (var row_189 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_186)))))) {
V v_190 = row_189[0];
V v_191 = row_189[1];
if (R.Truth(R.And((V)(R.Get(v_191, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_191, (V)"source_id") == R.Get(v_178, (V)"id"))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_191, (V)"target_id") == R.Get(v_180, (V)"id")))));

var values_192 = R.Join(new V[] {}, new Results((V)true));
v_188 = values_192[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_188, () => (V)(R.Get(v_180, (V)"hp") < (V)10.0d)), () => (V)(R.Get(v_170, (V)"hp") == (V)20.0d)), () => R.Invoke(v_182, "find", R.Join(new V[] {(V)"S1:yD1:7", (V)1.0d}, new Results((V)true))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"occupying a locked marker blocks placement and stun cannot refund its finite attempt"}, new Results(new V((RuleFunction)(args_193 => {

var values_194 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_195 = values_194[0];
V v_196 = values_194[1];
V v_197 = values_194[2];

R.Call(v_25, R.Join(new V[] {v_195}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_195}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_195}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_196, (V)"position"), (V)"x") == (V)9.0d), () => (V)((V)R.Length(R.Or(R.Get(v_195, (V)"idea_mirrors"), () => R.Table())) == (V)0.0d)), () => (V)(R.Get(R.Get(v_197, (V)"ai_memory"), (V)"mirror_casts") == (V)1.0d)))));
var values_198 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
v_195 = values_198[0];
v_196 = values_198[1];
v_197 = values_198[2];

R.Call(v_25, R.Join(new V[] {v_195}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_195, R.Get(v_197, (V)"id")}, new Results((V)500.0d)))));

R.Call(v_25, R.Join(new V[] {v_195}, new Results((V)3.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(v_197, (V)"pending_attack"))), () => (V)(R.Get(R.Get(v_197, (V)"ai_memory"), (V)"mirror_casts") == (V)1.0d)), () => (V)((V)R.Length(R.Or(R.Get(v_195, (V)"idea_mirrors"), () => R.Table())) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"expiry and caster death clear the finite marker while saved continuation stays byte exact"}, new Results(new V((RuleFunction)(args_199 => {

var values_200 = R.Join(new V[] {}, R.Call(v_45, Results.Empty));
V v_201 = values_200[0];
V v_202 = values_200[1];
V v_203 = values_200[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_201, R.Get(v_203, (V)"id")}, new Results((V)1000.0d)))));
var values_204 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_201))));
V v_205 = values_204[0];

var values_206 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_205))))))));
V v_207 = values_206[0];
R.Call(v_3, R.Join(new V[] {R.Call(v_25, R.Join(new V[] {v_201}, new Results((V)5.0d))).First}, R.Call(v_25, R.Join(new V[] {v_207}, new Results((V)5.0d)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_201))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_207)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_201, (V)"idea_mirrors")) == (V)0.0d), () => (V)(R.Get(v_201, (V)"clock") >= (V)626.0d)))));
var values_208 = R.Join(new V[] {}, R.Call(v_45, Results.Empty));
v_201 = values_208[0];
v_202 = values_208[1];
v_203 = values_208[2];

var values_209 = R.Join(new V[] {(V)9.0d}, new Results((V)6.0d));
var obj_210 = R.Get(v_202, (V)"position");
V key_211 = (V)"x";
var obj_212 = R.Get(v_202, (V)"position");
V key_213 = (V)"y";
R.Set(obj_210, key_211, values_209[0]);
R.Set(obj_212, key_213, values_209[1]);

var values_214 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_215 = v_203;
V key_216 = (V)"hp";
R.Set(obj_215, key_216, values_214[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_201}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_201}, new Results(R.Get(v_203, (V)"id")))).First)), () => (V)((V)R.Length(R.Get(v_201, (V)"idea_mirrors")) == (V)0.0d)), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_201))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"inactive floor lifetime rebases and invalid active markers or locked paths reject"}, new Results(new V((RuleFunction)(args_217 => {

var values_218 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"clock", (V)900.0d), new Entry((V)"entities", R.Table()), new Entry((V)"schedule", R.Table()), new Entry((V)"floor_pause", R.Table(new Entry((V)"a", R.Table(new Entry((V)"suspended_at", (V)300.0d), new Entry((V)"schedule", R.Table()))))), new Entry((V)"idea_mirrors", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", (V)"a"), new Entry((V)"expires", (V)626.0d))), new Entry((V)2, R.Table(new Entry((V)"floor_id", (V)"b"), new Entry((V)"expires", (V)1000.0d))))))));
V v_219 = values_218[0];
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))).First, (V)"resume"), R.Join(new V[] {v_219}, new Results((V)"a")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_219, (V)"idea_mirrors"), (V)1.0d), (V)"expires") == (V)1226.0d), () => (V)(R.Get(R.Get(R.Get(v_219, (V)"idea_mirrors"), (V)2.0d), (V)"expires") == (V)1000.0d)))));
var values_220 = R.Join(new V[] {}, R.Call(v_45, Results.Empty));
V v_221 = values_220[0];
V v_222 = values_220[1];
V v_223 = values_220[2];

var values_224 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_221))));
V v_225 = values_224[0];
foreach (var row_246 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_226 => {
V v_227 = args_226[0];
var values_228 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_229 = v_227;
V key_230 = (V)"in_dx";
R.Set(obj_229, key_230, values_228[0]);
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_231 => {
V v_232 = args_231[0];
var values_233 = R.Join(new V[] {}, new Results((V)200.0d));
var obj_234 = v_232;
V key_235 = (V)"expires";
R.Set(obj_234, key_235, values_233[0]);
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_236 => {
V v_237 = args_236[0];
var values_238 = R.Join(new V[] {}, new Results(R.Get(v_221, (V)"player_id")));
var obj_239 = v_237;
V key_240 = (V)"source_id";
R.Set(obj_239, key_240, values_238[0]);
return Results.Empty;
}))), new Entry((V)4, new V((RuleFunction)(args_241 => {
V v_242 = args_241[0];
var values_243 = R.Join(new V[] {}, new Results((V)6.0d));
var obj_244 = v_242;
V key_245 = (V)"x";
R.Set(obj_244, key_245, values_243[0]);
return Results.Empty;
})))))))))) {
V v_247 = row_246[0];
V v_248 = row_246[1];
var values_249 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_225))))));
V v_250 = values_249[0];

R.Call(v_248, R.Join(new V[] {}, new Results(R.Get(R.Get(v_250, (V)"idea_mirrors"), (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_250))).First)))));
}
var values_251 = R.Join(new V[] {(V)11.0d}, new Results((V)6.0d));
var obj_252 = R.Get(v_222, (V)"position");
V key_253 = (V)"x";
var obj_254 = R.Get(v_222, (V)"position");
V key_255 = (V)"y";
R.Set(obj_252, key_253, values_251[0]);
R.Set(obj_254, key_255, values_251[1]);
var values_256 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_221, (V)"core:enemy/thornspitter", (V)7.0d, (V)6.0d}, new Results((V)(R.Get(v_221, (V)"clock") + (V)1.0d)))));
V v_257 = values_256[0];

R.Call(v_25, R.Join(new V[] {v_221}, new Results((V)1.0d)));
var values_258 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_221))));
V v_259 = values_258[0];
foreach (var row_260 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"entities"))))))) {
V v_261 = row_260[0];
V v_262 = row_260[1];
if (R.Truth((V)(R.Get(v_262, (V)"id") == R.Get(v_257, (V)"id")))) {
var values_263 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_264 = R.Get(R.Get(v_262, (V)"pending_attack"), (V)"reflection");
V key_265 = (V)"out_dx";
R.Set(obj_264, key_265, values_263[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_259))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"movement-linked charges and Hookrunner grapples keep their original straight geometry"}, new Results(new V((RuleFunction)(args_266 => {

var values_267 = R.Join(new V[] {}, R.Call(v_45, Results.Empty));
V v_268 = values_267[0];
V v_269 = values_267[1];
V v_270 = values_267[2];

var values_271 = R.Join(new V[] {(V)11.0d}, new Results((V)6.0d));
var obj_272 = R.Get(v_269, (V)"position");
V key_273 = (V)"x";
var obj_274 = R.Get(v_269, (V)"position");
V key_275 = (V)"y";
R.Set(obj_272, key_273, values_271[0]);
R.Set(obj_274, key_275, values_271[1]);
var values_276 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_268, (V)"core:enemy/glass_charger", (V)7.0d, (V)6.0d}, new Results((V)(R.Get(v_268, (V)"clock") + (V)1.0d)))));
V v_277 = values_276[0];
R.Call(v_25, R.Join(new V[] {v_268}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(v_277, (V)"pending_attack"), () => R.Get(R.Get(v_277, (V)"pending_attack"), (V)"advance")), () => (V)(!R.Truth(R.Get(R.Get(v_277, (V)"pending_attack"), (V)"reflection")))), () => (V)((V)R.Length(R.Get(v_268, (V)"idea_mirrors")) == (V)1.0d)))));
foreach (var row_278 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_277, (V)"pending_attack"), (V)"target_cells"))))))) {
V v_279 = row_278[0];
V v_280 = row_278[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_280, (V)"y") == (V)6.0d))));
}
var values_281 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/hookrunner"))))));
v_268 = values_281[0];

var values_282 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_268))));
v_269 = values_282[0];
var values_283 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_284 = R.Get(v_269, (V)"position");
V key_285 = (V)"x";
var obj_286 = R.Get(v_269, (V)"position");
V key_287 = (V)"y";
R.Set(obj_284, key_285, values_283[0]);
R.Set(obj_286, key_287, values_283[1]);

var values_288 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_268, R.Get(v_9, (V)"id"), (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
v_270 = values_288[0];
while (R.Truth((V)(R.Get(v_268, (V)"clock") < (V)250.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_268}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_268, (V)"idea_mirrors")) == (V)1.0d))));
var values_289 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_268, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_290 = values_289[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_290, (V)"reflection"))))));
foreach (var row_291 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_290, (V)"cells"))))))) {
V v_292 = row_291[0];
V v_293 = row_291[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_293, (V)"y") == (V)6.0d))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_268}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_268, (V)"idea_mirrors")) == (V)1.0d), () => (V)(R.Get(R.Get(v_270, (V)"position"), (V)"x") == (V)8.0d)), () => (V)(R.Get(R.Get(v_270, (V)"position"), (V)"y") == (V)6.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a real generated Hexer has approved caster and reflector art in the fixed 1-Bit renderer"}, new Results(new V((RuleFunction)(args_294 => {

var values_295 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_seeds"))).First, (V)"find"), R.Join(new V[] {}, new Results(R.Get(v_9, (V)"id")))));
V v_296 = values_295[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_296)))))));
var values_297 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_298 = values_297[0];
foreach (var row_299 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_300 = row_299[0];
V v_301 = row_299[1];
var values_302 = R.Join(new V[] {}, new Results(R.Table()));
V v_303 = values_302[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_298, (V)"packs"), v_301), (V)"missing")) == (V)0.0d))));
foreach (var row_304 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_298, (V)"packs"), v_301), (V)"bindings"))))))) {
V v_305 = row_304[0];
V v_306 = row_304[1];
if (R.Truth((V)(R.Get(v_306, (V)"id") == R.Get(v_9, (V)"id")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_306, (V)"sheet") == (V)"onebit"), () => (V)(R.Get(v_306, (V)"index") == (V)320.0d)))));

var values_307 = R.Join(new V[] {}, new Results((V)true));
var obj_308 = v_303;
V key_309 = (V)"body";
R.Set(obj_308, key_309, values_307[0]);
}
if (R.Truth((V)(R.Get(v_306, (V)"id") == (V)"core:ui/idea_mirror_effect"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_306, (V)"index") == (V)493.0d))));

var values_310 = R.Join(new V[] {}, new Results((V)true));
var obj_311 = v_303;
V key_312 = (V)"mark";
R.Set(obj_311, key_312, values_310[0]);
}
if (R.Truth((V)(R.Get(v_306, (V)"id") == (V)"core:ui/idea_reflect_effect"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_306, (V)"index") == (V)494.0d))));

var values_313 = R.Join(new V[] {}, new Results((V)true));
var obj_314 = v_303;
V key_315 = (V)"reflect";
R.Set(obj_314, key_315, values_313[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_303, (V)"body"), () => R.Get(v_303, (V)"mark")), () => R.Get(v_303, (V)"reflect")))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
