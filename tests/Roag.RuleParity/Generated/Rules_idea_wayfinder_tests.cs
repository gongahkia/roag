// Static C# port of tests/idea_wayfinder_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 05cd25111d80d46dda0d2f9cb1b1093d703d2931d9428539beeb1c6f50aa1f3a
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_wayfinder_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_wayfinder"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_9 = values_8[0];

var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_11 = values_10[0];
V v_12 = V.Nil;
v_12 = new V((RuleFunction)(args_13 => {

var values_14 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_7, (V)"id")))))));
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

R.Call(R.Get(v_9, (V)"recompute"), R.Join(new V[] {}, new Results(v_15)));

return R.Join(new V[] {v_15}, new Results(v_17));
return Results.Empty;
}));
V v_23 = V.Nil;
v_23 = new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_25}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))))));

return R.Join(new V[] {}, new Results(R.Get(R.Get(v_25, (V)"class_special"), (V)"beacon")));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Wayfinder pure beacon preview and paid planting preserve RNG without creating an attack"}, new Results(new V((RuleFunction)(args_26 => {

var values_27 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_28 = values_27[0];
V v_29 = values_27[1];

var values_30 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_28))));
V v_31 = values_30[0];

var values_32 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_28, (V)"rng"), (V)"state")));
V v_33 = values_32[0];
{ double end_36 = R.Num((V)30.0d), step_37 = R.Num((V)1.0d);
for (double i_35 = R.Num((V)1.0d); step_37 > 0 ? i_35 <= end_36 : i_35 >= end_36; i_35 += step_37) {
V v_34 = i_35;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_28, (V)"sweeping_slash", (-(V)1.0d)}, new Results((V)0.0d))).First, (V)"valid"))));
}
}

R.Call(v_3, R.Join(new V[] {v_31}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_28)))));
var values_38 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_28}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))))));
V v_39 = values_38[0];
V v_40 = values_38[1];
V v_41 = values_38[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_28, (V)"clock") == (V)91.0d), () => (V)(R.Get(R.Get(R.Get(v_28, (V)"class_special"), (V)"beacon"), (V)"x") == (V)6.0d)), () => (V)(R.Get(R.Get(v_28, (V)"rng"), (V)"state") == v_33)))));
foreach (var row_42 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_41)))))) {
V v_43 = row_42[0];
V v_44 = row_42[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_44, (V)"kind") != (V)"AttackPerformed"), () => (V)(R.Get(v_44, (V)"kind") != (V)"AbilityUsed")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_28)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"return consumes one beacon and pays normal cost while entering only the real landing"}, new Results(new V((RuleFunction)(args_45 => {

var values_46 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_47 = values_46[0];
V v_48 = values_46[1];

R.Call(v_23, R.Join(new V[] {}, new Results(v_47)));
{ double end_51 = R.Num((V)3.0d), step_52 = R.Num((V)1.0d);
for (double i_50 = R.Num((V)1.0d); step_52 > 0 ? i_50 <= end_51 : i_50 >= end_51; i_50 += step_52) {
V v_49 = i_50;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_47}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
}
}
var values_53 = R.Join(new V[] {}, new Results(R.Get(v_47, (V)"clock")));
V v_54 = values_53[0];

var values_55 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_47))));
V v_56 = values_55[0];

var values_57 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_47, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_58 = values_57[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_58, (V)"valid"), () => R.Get(v_58, (V)"beacon_return")), () => (V)(R.Get(v_58, (V)"return_distance") == (V)4.0d)))));

R.Call(v_3, R.Join(new V[] {v_56}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_47)))));
var values_59 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_47}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_60 = values_59[0];
V v_61 = values_59[1];
V v_62 = values_59[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_48, (V)"position"), (V)"x") == (V)6.0d), () => (V)(R.Get(R.Get(v_48, (V)"position"), (V)"y") == (V)6.0d)), () => (V)(R.Get(v_47, (V)"clock") == (V)(v_54 + (V)100.0d))), () => (V)(!R.Truth(R.Get(R.Get(v_47, (V)"class_special"), (V)"beacon")))))));
var values_63 = R.Join(new V[] {}, new Results((V)0.0d));
V v_64 = values_63[0];

foreach (var row_65 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_62)))))) {
V v_66 = row_65[0];
V v_67 = row_65[1];
if (R.Truth(R.And((V)(R.Get(v_67, (V)"kind") == (V)"TileEntered"), () => (V)(R.Get(v_67, (V)"source_id") == R.Get(v_48, (V)"id"))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_67, (V)"x") == (V)6.0d), () => (V)(R.Get(v_67, (V)"y") == (V)6.0d)), () => R.Get(v_67, (V)"forced")))));

var values_68 = R.Join(new V[] {}, new Results((V)(v_64 + (V)1.0d)));
v_64 = values_68[0];
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_64 == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_47, (V)"dash", (V)1.0d}, new Results((V)0.0d))).First, (V)"beacon_return"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"beacons reject occupied stairs diagonal and unexplored destinations without advancing time"}, new Results(new V((RuleFunction)(args_69 => {

var values_70 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_71 = values_70[0];
V v_72 = values_70[1];

var values_73 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_71))));
V v_74 = values_73[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_71}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (-(V)1.0d)))))).First)))));

R.Call(v_3, R.Join(new V[] {v_74}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_71)))));
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_71, (V)"core:enemy/mossbound_guard", (V)6.0d, (V)6.0d}, new Results((V)100000.0d)));

var values_75 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_71))));
v_74 = values_75[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_71}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_74}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_71)))));
var values_76 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
v_71 = values_76[0];
v_72 = values_76[1];

var values_77 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"map_for"), R.Join(new V[] {}, new Results(v_71))));
V v_78 = values_77[0];
var values_79 = R.Join(new V[] {}, new Results(V.Nil));
var obj_80 = R.Get(R.Get(R.Get(v_71, (V)"explored"), R.Get(R.Get(v_72, (V)"position"), (V)"floor_id")), (V)"cells");
V key_81 = (V)((V)((V)((V)6.0d - (V)1.0d) * R.Get(v_78, (V)"width")) + (V)6.0d);
R.Set(obj_80, key_81, values_79[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_71, (V)"sweeping_slash", (-(V)1.0d)}, new Results((V)0.0d))).First, (V)"valid"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"blocked and over-distance returns keep their beacon and spend no time"}, new Results(new V((RuleFunction)(args_82 => {

var values_83 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_84 = values_83[0];
V v_85 = values_83[1];

R.Call(v_23, R.Join(new V[] {}, new Results(v_84)));

R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_84, (V)"core:enemy/mossbound_guard", (V)6.0d, (V)6.0d}, new Results((V)100000.0d)));
var values_86 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_84))));
V v_87 = values_86[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_87}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_84)))));
var values_88 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
v_84 = values_88[0];
v_85 = values_88[1];

R.Call(v_23, R.Join(new V[] {}, new Results(v_84)));

var values_89 = R.Join(new V[] {}, new Results((V)13.0d));
var obj_90 = R.Get(v_85, (V)"position");
V key_91 = (V)"x";
R.Set(obj_90, key_91, values_89[0]);

R.Call(R.Get(v_9, (V)"recompute"), R.Join(new V[] {}, new Results(v_84)));

var values_92 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_84))));
v_87 = values_92[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_87}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_84)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"return is bound to one actual generated room and cannot cross floors"}, new Results(new V((RuleFunction)(args_93 => {

{ double end_96 = R.Num((V)12.0d), step_97 = R.Num((V)1.0d);
for (double i_95 = R.Num((V)1.0d); step_97 > 0 ? i_95 <= end_96 : i_95 >= end_96; i_95 += step_97) {
V v_94 = i_95;
var values_98 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_94}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_7, (V)"id")), new Entry((V)"multifloor", (V)true))))));
V v_99 = values_98[0];

var values_100 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_99))));
V v_101 = values_100[0];

var values_102 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"map_for"), R.Join(new V[] {}, new Results(v_99))));
V v_103 = values_102[0];
var values_104 = R.Join(new V[] {}, new Results((V)false));
V v_105 = values_104[0];
foreach (var row_106 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_107 = row_106[0];
V v_108 = row_106[1];
if (R.Truth(R.And((V)(!R.Truth(v_105)), () => R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_99, (V)"sweeping_slash", R.Get(v_108, (V)1.0d)}, new Results(R.Get(v_108, (V)2.0d)))).First, (V)"valid")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_99}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", R.Get(v_108, (V)1.0d)), new Entry((V)"dy", R.Get(v_108, (V)2.0d))))))));

var values_109 = R.Join(new V[] {}, new Results((V)true));
v_105 = values_109[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_105)));

var values_110 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_99, (V)"class_special"), (V)"beacon")));
V v_111 = values_110[0];
var values_112 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_103, (V)"rooms"), (V)R.Length(R.Get(v_103, (V)"rooms")))));
V v_113 = values_112[0];

var values_114 = R.Join(new V[] {R.Get(R.Get(v_113, (V)"center"), (V)"x")}, new Results(R.Get(R.Get(v_113, (V)"center"), (V)"y")));
var obj_115 = R.Get(v_101, (V)"position");
V key_116 = (V)"x";
var obj_117 = R.Get(v_101, (V)"position");
V key_118 = (V)"y";
R.Set(obj_115, key_116, values_114[0]);
R.Set(obj_117, key_118, values_114[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_99, (V)"dash", (V)1.0d}, new Results((V)0.0d))).First, (V)"valid"))))));
var values_119 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_99, (V)"floor_order"), (V)2.0d)));
var obj_120 = v_111;
V key_121 = (V)"floor_id";
R.Set(obj_120, key_121, values_119[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_99, (V)"dash", (V)1.0d}, new Results((V)0.0d))).First, (V)"valid"))))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a real return collects an existing supply at the landing and leaves no fake traversal events"}, new Results(new V((RuleFunction)(args_122 => {

var values_123 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_124 = values_123[0];
V v_125 = values_123[1];

R.Call(v_23, R.Join(new V[] {}, new Results(v_124)));

var values_126 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_127 = v_125;
V key_128 = (V)"hp";
R.Set(obj_127, key_128, values_126[0]);
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))).First, (V)"spawn_supply"), R.Join(new V[] {v_124, (V)"food", R.Get(R.Get(v_125, (V)"position"), (V)"floor_id"), (V)6.0d, (V)6.0d, new V((RuleFunction)(args_129 => {


return Results.Empty;
})), R.Get(v_125, (V)"id")}, new Results(V.Nil)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_124}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_124}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_125, (V)"hp") == (V)14.0d), () => R.Get(R.Get(R.Get(v_124, (V)"drops"), (V)1.0d), (V)"collected")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"planted beacons and next return resume exactly while forged owners or rooms reject"}, new Results(new V((RuleFunction)(args_130 => {

var values_131 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_132 = values_131[0];
V v_133 = values_131[1];

R.Call(v_23, R.Join(new V[] {}, new Results(v_132)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_132}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_134 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_132))));
V v_135 = values_134[0];

var values_136 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_135))))))));
V v_137 = values_136[0];
var values_138 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_132}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_139 = values_138[0];
V v_140 = values_138[1];
V v_141 = values_138[2];

var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_137}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_143 = values_142[0];
V v_144 = values_142[1];
V v_145 = values_142[2];
R.Call(v_3, R.Join(new V[] {v_141}, new Results(v_145)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_132))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_137)))));
foreach (var row_146 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"owner_id"), new Entry((V)2, (V)"room_id")))))))) {
V v_147 = row_146[0];
V v_148 = row_146[1];
var values_149 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_135))))));
V v_150 = values_149[0];

var values_151 = R.Join(new V[] {}, new Results((V)99999.0d));
var obj_152 = R.Get(R.Get(v_150, (V)"class_special"), (V)"beacon");
V key_153 = v_148;
R.Set(obj_152, key_153, values_151[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_150))).First)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Wayfinder knife uses its actual two-cell line and normal first-body armor"}, new Results(new V((RuleFunction)(args_154 => {

var values_155 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_156 = values_155[0];
V v_157 = values_155[1];

var values_158 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_156, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_159 = values_158[0];
var values_160 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_156, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_161 = values_160[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_156}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_159, (V)"hp") == (V)8.0d), () => (V)(R.Get(v_161, (V)"hp") == (V)10.0d)), () => (V)(R.Get(v_156, (V)"clock") == (V)91.0d)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
