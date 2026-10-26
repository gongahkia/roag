// Static C# port of tests/idea_geomancer_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 69f992155a352fd13e611ae8fc3d00491d161acfded67f7ccba9b4ec70cfa99d
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_geomancer_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_geomancer"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_11 = values_10[0];

var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_13 = values_12[0];

var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))));
V v_15 = values_14[0];
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {

var values_18 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_7, (V)"id")))))));
V v_19 = values_18[0];

var values_20 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_19))));
V v_21 = values_20[0];

var values_22 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_23 = R.Get(v_21, (V)"position");
V key_24 = (V)"x";
var obj_25 = R.Get(v_21, (V)"position");
V key_26 = (V)"y";
R.Set(obj_23, key_24, values_22[0]);
R.Set(obj_25, key_26, values_22[1]);
R.Call(R.Get(v_13, (V)"recompute"), R.Join(new V[] {}, new Results(v_19)));

return R.Join(new V[] {v_19}, new Results(v_21));
return Results.Empty;
}));
V v_27 = V.Nil;
v_27 = new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
foreach (var row_30 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_29, (V)"entities"))))))) {
V v_31 = row_30[0];
V v_32 = row_30[1];
if (R.Truth((V)(R.Get(v_32, (V)"controller") == R.Get(v_7, (V)"controller")))) {
return R.Join(new V[] {}, new Results(v_32));
}
}
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Geomancer placement preview is pure and a paid block preserves original terrain and RNG"}, new Results(new V((RuleFunction)(args_33 => {

var values_34 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_35 = values_34[0];
V v_36 = values_34[1];

var values_37 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_35))).First, (V)"rows")))));
V v_38 = values_37[0];

var values_39 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_35, (V)"rng"), (V)"state")));
V v_40 = values_39[0];

var values_41 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_35))));
V v_42 = values_41[0];
{ double end_45 = R.Num((V)20.0d), step_46 = R.Num((V)1.0d);
for (double i_44 = R.Num((V)1.0d); step_46 > 0 ? i_44 <= end_45 : i_44 >= end_45; i_44 += step_46) {
V v_43 = i_44;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_35, (V)"sweeping_slash", (V)0.0d}, new Results((V)1.0d))).First, (V)"valid"))));
}
}

R.Call(v_3, R.Join(new V[] {v_42}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_35)))));
var values_47 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_35}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))))));
V v_48 = values_47[0];
V v_49 = values_47[1];
V v_50 = values_47[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_48}, new Results(v_49)));
var values_51 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_35))))));
V v_52 = values_51[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_35, (V)"clock") == (V)153.0d), () => (V)(R.Get(v_52, (V)"expires") == (V)300.0d)), () => (V)(!R.Truth(R.Get(v_52, (V)"hp")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_38 == R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_35))).First, (V)"rows")))).First), () => (V)(v_40 == R.Get(R.Get(v_35, (V)"rng"), (V)"state"))))));
foreach (var row_53 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_50)))))) {
V v_54 = row_53[0];
V v_55 = row_53[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_55, (V)"kind") != (V)"AttackPerformed"), () => (V)(R.Get(v_55, (V)"kind") != (V)"AbilityUsed")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_35)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"bulwark stops movement projectiles and sight without becoming an enemy or reward source"}, new Results(new V((RuleFunction)(args_56 => {

var values_57 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_58 = values_57[0];
V v_59 = values_57[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_58}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_60 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_58))));
V v_61 = values_60[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_61, () => (V)(!R.Truth(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_58, (V)"move", (V)1.0d}, new Results((V)0.0d))).First, (V)"valid")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_9, (V)"projectile_passable"), R.Join(new V[] {R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_58))).First, (V)8.0d, (V)6.0d}, new Results(v_58))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_13, (V)"can_see"), R.Join(new V[] {v_58, R.Get(R.Get(v_59, (V)"position"), (V)"floor_id"), (V)10.0d}, new Results((V)6.0d))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_58, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"cells"), (V)1.0d), (V)"x") == (V)8.0d))));
var values_62 = R.Join(new V[] {}, new Results(R.Get(v_58, (V)"currency")));
V v_63 = values_62[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_58}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_58, (V)"currency") == v_63), () => R.Call(v_27, R.Join(new V[] {}, new Results(v_58))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"bulwark expires at the exact scheduled time and replacement cancels the old timer"}, new Results(new V((RuleFunction)(args_64 => {

var values_65 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_66 = values_65[0];
V v_67 = values_65[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_66}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));
var values_68 = R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_66))));
V v_69 = values_68[0];

var values_70 = R.Join(new V[] {}, new Results(R.Get(v_69, (V)"expiry_schedule_id")));
V v_71 = values_70[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_66}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))))));

var values_72 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_66))))));
V v_73 = values_72[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_73, (V)"id") != R.Get(v_69, (V)"id")), () => (V)(R.Get(v_73, (V)"expires") == (V)453.0d)), () => (V)(R.Get(v_66, (V)"clock") == (V)306.0d)))));
foreach (var row_74 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_66, (V)"schedule"))))))) {
V v_75 = row_74[0];
V v_76 = row_74[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_76, (V)"id") != v_71))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_66}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_27, R.Join(new V[] {}, new Results(v_66)))));
var values_77 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_66}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_78 = values_77[0];
V v_79 = values_77[1];
V v_80 = values_77[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(v_27, R.Join(new V[] {}, new Results(v_66))).First)))));
var values_81 = R.Join(new V[] {}, new Results((V)false));
V v_82 = values_81[0];

foreach (var row_83 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_80)))))) {
V v_84 = row_83[0];
V v_85 = row_83[1];
if (R.Truth((V)(R.Get(v_85, (V)"kind") == (V)"GeomancerBlockExpired"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_85, (V)"time") == (V)453.0d))));

var values_86 = R.Join(new V[] {}, new Results((V)true));
v_82 = values_86[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_82, () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_66))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"occupied diagonal and one-cell route sealing requests are rejected without advancing time"}, new Results(new V((RuleFunction)(args_87 => {

var values_88 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_89 = values_88[0];
V v_90 = values_88[1];

var values_91 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_89))));
V v_92 = values_91[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_89}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_92}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_89)))));
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_89, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d)));
var values_93 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_89))));
v_92 = values_93[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_89}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_92}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_89)))));
var values_94 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_90, (V)"position"), (V)"floor_id")), new Entry((V)"width", (V)5.0d), new Entry((V)"height", (V)3.0d), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"#####"), new Entry((V)2, (V)"#...#"), new Entry((V)3, (V)"#####"))))));
V v_95 = values_94[0];
var values_96 = R.Join(new V[] {(V)2.0d}, new Results((V)2.0d));
var obj_97 = R.Get(v_90, (V)"position");
V key_98 = (V)"x";
var obj_99 = R.Get(v_90, (V)"position");
V key_100 = (V)"y";
R.Set(obj_97, key_98, values_96[0]);
R.Set(obj_99, key_100, values_96[1]);

var values_101 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_90))));
var obj_102 = v_89;
V key_103 = (V)"entities";
R.Set(obj_102, key_103, values_101[0]);
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_89, v_95, (V)3.0d}, new Results((V)2.0d))));
V v_105 = values_104[0];
V v_106 = values_104[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_105)), () => (V)(v_106 == (V)"cannot seal an accessible route")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stone bolt stops at the first body and a blocked diagonal corner"}, new Results(new V((RuleFunction)(args_107 => {

var values_108 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_109 = values_108[0];
V v_110 = values_108[1];

var values_111 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_109, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_112 = values_111[0];
var values_113 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_109, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_114 = values_113[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_109}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_112, (V)"hp") == (V)6.0d), () => (V)(R.Get(v_114, (V)"hp") == (V)10.0d)), () => (V)(R.Get(v_109, (V)"clock") == (V)141.0d)))));
var values_115 = R.Join(new V[] {(V)8.0d}, new Results((V)4.0d));
var obj_116 = R.Get(v_110, (V)"position");
V key_117 = (V)"x";
var obj_118 = R.Get(v_110, (V)"position");
V key_119 = (V)"y";
R.Set(obj_116, key_117, values_115[0]);
R.Set(obj_118, key_119, values_115[1]);

var values_120 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_109, (V)"basic_attack", (V)1.0d}, new Results((V)1.0d))));
V v_121 = values_120[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_121, (V)"cells")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_121, (V)"cells"), (V)1.0d), (V)"x") == (V)9.0d)), () => (V)(R.Get(R.Get(R.Get(v_121, (V)"cells"), (V)1.0d), (V)"y") == (V)5.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"saved bulwarks require one valid owner matching finite expiry and no orphan timer"}, new Results(new V((RuleFunction)(args_122 => {

var values_123 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_124 = values_123[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_124}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));
var values_125 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_124))));
V v_126 = values_125[0];

R.Call(v_3, R.Join(new V[] {v_126}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_126)))))))))));
foreach (var row_151 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_127 => {
V v_128 = args_127[0];
foreach (var row_129 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_128, (V)"entities"))))))) {
V v_130 = row_129[0];
V v_131 = row_129[1];
if (R.Truth((V)(R.Get(v_131, (V)"controller") == R.Get(v_7, (V)"controller")))) {
var values_132 = R.Join(new V[] {}, new Results((V)9999.0d));
var obj_133 = v_131;
V key_134 = (V)"owner_id";
R.Set(obj_133, key_134, values_132[0]);
}
}
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_135 => {
V v_136 = args_135[0];
foreach (var row_137 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_136, (V)"entities"))))))) {
V v_138 = row_137[0];
V v_139 = row_137[1];
if (R.Truth((V)(R.Get(v_139, (V)"controller") == R.Get(v_7, (V)"controller")))) {
var values_140 = R.Join(new V[] {}, new Results((V)(R.Get(v_139, (V)"expires") + (V)1.0d)));
var obj_141 = v_139;
V key_142 = (V)"expires";
R.Set(obj_141, key_142, values_140[0]);
}
}
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_143 => {
V v_144 = args_143[0];
foreach (var row_145 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_144, (V)"schedule"))))))) {
V v_146 = row_145[0];
V v_147 = row_145[1];
if (R.Truth(R.Get(v_147, (V)"idea_geomancer"))) {
var values_148 = R.Join(new V[] {}, new Results((V)false));
var obj_149 = v_147;
V key_150 = (V)"idea_geomancer";
R.Set(obj_149, key_150, values_148[0]);
}
}
return Results.Empty;
})))))))))) {
V v_152 = row_151[0];
V v_153 = row_151[1];
var values_154 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_126))))));
V v_155 = values_154[0];

R.Call(v_153, R.Join(new V[] {}, new Results(v_155)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_155))).First)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"paused-floor bulwark expiry rebases with its work and replacement removes parked work"}, new Results(new V((RuleFunction)(args_156 => {

var values_157 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {(V)9.0d}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_7, (V)"id")), new Entry((V)"multifloor", (V)true))))));
V v_158 = values_157[0];

var values_159 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_158))));
V v_160 = values_159[0];

var values_161 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_158))));
V v_162 = values_161[0];
var values_163 = Results.Empty;
V v_164 = values_163[0];
{ double end_167 = R.Num((V)(R.Get(v_162, (V)"height") - (V)1.0d)), step_168 = R.Num((V)1.0d);
for (double i_166 = R.Num((V)2.0d); step_168 > 0 ? i_166 <= end_167 : i_166 >= end_167; i_166 += step_168) {
V v_165 = i_166;
{ double end_171 = R.Num((V)(R.Get(v_162, (V)"width") - (V)1.0d)), step_172 = R.Num((V)1.0d);
for (double i_170 = R.Num((V)2.0d); step_172 > 0 ? i_170 <= end_171 : i_170 >= end_171; i_170 += step_172) {
V v_169 = i_170;
if (R.Truth(R.And((V)(!R.Truth(v_164)), () => R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {v_162, v_169}, new Results(v_165))).First))) {
var values_173 = R.Join(new V[] {v_169}, new Results(v_165));
var obj_174 = R.Get(v_160, (V)"position");
V key_175 = (V)"x";
var obj_176 = R.Get(v_160, (V)"position");
V key_177 = (V)"y";
R.Set(obj_174, key_175, values_173[0]);
R.Set(obj_176, key_177, values_173[1]);
foreach (var row_178 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d)))))))))) {
V v_179 = row_178[0];
V v_180 = row_178[1];
if (R.Truth(R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_158, v_162, (V)(v_169 + R.Get(v_180, (V)1.0d))}, new Results((V)(v_165 + R.Get(v_180, (V)2.0d))))).First)) {
var values_181 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)(v_169 + R.Get(v_180, (V)1.0d))), new Entry((V)2, (V)(v_165 + R.Get(v_180, (V)2.0d))))));
v_164 = values_181[0];

break;
}
}
}
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_164)));

var values_183 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_158, v_160, R.Get(v_164, (V)1.0d), R.Get(v_164, (V)2.0d), (V)1.0d}, new Results(new V((RuleFunction)(args_182 => {


return Results.Empty;
}))))));
V v_184 = values_183[0];
var values_185 = R.Join(new V[] {}, new Results(R.Get(v_158, (V)"active_floor_id")));
V v_186 = values_185[0];

R.Call(R.Get(v_15, (V)"park"), R.Join(new V[] {v_158}, new Results(v_186)));

var values_187 = R.Join(new V[] {}, new Results((V)1000.0d));
var obj_188 = v_158;
V key_189 = (V)"clock";
R.Set(obj_188, key_189, values_187[0]);
R.Call(R.Get(v_15, (V)"resume"), R.Join(new V[] {v_158}, new Results(v_186)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_184, (V)"expires") == (V)1300.0d))));
var values_190 = R.Join(new V[] {}, new Results((V)0.0d));
V v_191 = values_190[0];

foreach (var row_192 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_158, (V)"schedule"))))))) {
V v_193 = row_192[0];
V v_194 = row_192[1];
if (R.Truth(R.Get(v_194, (V)"idea_geomancer"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_194, (V)"due") == (V)1300.0d))));

var values_195 = R.Join(new V[] {}, new Results((V)(v_191 + (V)1.0d)));
v_191 = values_195[0];
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_191 == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Geomancer placement preserves connected required access across genuine seeded floors"}, new Results(new V((RuleFunction)(args_196 => {

{ double end_199 = R.Num((V)20.0d), step_200 = R.Num((V)1.0d);
for (double i_198 = R.Num((V)1.0d); step_200 > 0 ? i_198 <= end_199 : i_198 >= end_199; i_198 += step_200) {
V v_197 = i_198;
var values_201 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_197}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_7, (V)"id")), new Entry((V)"multifloor", (V)true))))));
V v_202 = values_201[0];

var values_203 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_202))));
V v_204 = values_203[0];

var values_205 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_202))));
V v_206 = values_205[0];
var values_207 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_202))))));
V v_208 = values_207[0];
foreach (var row_209 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_210 = row_209[0];
V v_211 = row_209[1];
var values_212 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_202, v_206, (V)(R.Get(R.Get(v_204, (V)"position"), (V)"x") + R.Get(v_211, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_204, (V)"position"), (V)"y") + R.Get(v_211, (V)2.0d))))));
V v_213 = values_212[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_213))).First == (V)"boolean"))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_208 == R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_202))))).First))));
foreach (var row_214 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_206, (V)"placements"), (V)"stairs"))))))) {
V v_215 = row_214[0];
V v_216 = row_214[1];
var values_217 = R.Join(new V[] {(V)(R.Get(v_216, (V)"x") - (V)1.0d)}, new Results(R.Get(v_216, (V)"y")));
var obj_218 = R.Get(v_204, (V)"position");
V key_219 = (V)"x";
var obj_220 = R.Get(v_204, (V)"position");
V key_221 = (V)"y";
R.Set(obj_218, key_219, values_217[0]);
R.Set(obj_220, key_221, values_217[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_202, v_206, R.Get(v_216, (V)"x")}, new Results(R.Get(v_216, (V)"y")))).First)))));
}
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all class pages use matching bounded visible rows and mouse hitboxes"}, new Results(new V((RuleFunction)(args_222 => {

var values_223 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.pixel_ui"))));
V v_224 = values_223[0];

var values_225 = R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))).First, (V)"order")));
V v_226 = values_225[0];
foreach (var row_227 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_226)))))) {
V v_228 = row_227[0];
V v_229 = row_227[1];
var values_230 = R.Join(new V[] {}, R.Call(R.Get(v_224, (V)"class_page"), R.Join(new V[] {v_226}, new Results(v_229))));
V v_231 = values_230[0];
V v_232 = values_230[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_228 >= v_231), () => (V)(v_228 <= v_232)), () => (V)((V)(v_232 - v_231) < (V)9.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_224, (V)"class_at"), R.Join(new V[] {(V)300.0d, (V)((V)((V)150.0d + (V)((V)(v_228 - v_231) * (V)36.0d)) + (V)10.0d), v_231}, new Results(v_232))).First == v_228))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_224, (V)"class_at"), R.Join(new V[] {(V)300.0d, (V)((V)((V)150.0d + (V)((V)((V)(v_232 - v_231) + (V)1.0d) * (V)36.0d)) + (V)10.0d), v_231}, new Results(v_232))).First)))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
