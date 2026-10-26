// Static C# port of tests/idea_spiritbinder_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: daed1df5e9beb4cc8732725e3dd3f5b88871ce8699bc783276e1ce9feb64c621
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_spiritbinder_tests { internal static Results Load(NativeRuntime rt) {
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

var values_14 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_7, (V)"spirit_id")))))));
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
R.Call(v_2, R.Join(new V[] {(V)"Spiritbinder paid placement is pure in preview and creates only one nonblocking friendly object"}, new Results(new V((RuleFunction)(args_23 => {

var values_24 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_25 = values_24[0];
V v_26 = values_24[1];

var values_27 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_25))));
V v_28 = values_27[0];

var values_29 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_25, (V)"rng"), (V)"state")));
V v_30 = values_29[0];
{ double end_33 = R.Num((V)20.0d), step_34 = R.Num((V)1.0d);
for (double i_32 = R.Num((V)1.0d); step_34 > 0 ? i_32 <= end_33 : i_32 >= end_33; i_32 += step_34) {
V v_31 = i_32;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_25, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))).First, (V)"valid"))));
}
}

R.Call(v_3, R.Join(new V[] {v_28}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_25)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_25}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

var values_35 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"owned"), R.Join(new V[] {v_25}, new Results((V)"spirit"))))));
V v_36 = values_35[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Get(R.Get(v_36, (V)"position"), (V)"x") == (V)9.0d), () => (V)(R.Get(R.Get(v_36, (V)"position"), (V)"y") == (V)6.0d)), () => (V)(R.Get(v_36, (V)"owner_id") == R.Get(v_26, (V)"id"))), () => (V)(!R.Truth(R.Get(v_36, (V)"hp")))), () => (V)(!R.Truth(R.Get(v_36, (V)"blocks_movement")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_25, (V)"clock") == (V)140.0d), () => (V)(R.Get(R.Get(v_25, (V)"rng"), (V)"state") == v_30)), () => (V)(!R.Truth(R.Get(v_36, (V)"ai_id")))), () => (V)(!R.Truth(R.Get(v_36, (V)"pending_attack")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_25)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a familiar attacks only on a paid visible adjacent command and never on waits"}, new Results(new V((RuleFunction)(args_37 => {

var values_38 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_39 = values_38[0];
V v_40 = values_38[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_39}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_41 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_39, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_42 = values_41[0];
{ double end_45 = R.Num((V)3.0d), step_46 = R.Num((V)1.0d);
for (double i_44 = R.Num((V)1.0d); step_46 > 0 ? i_44 <= end_45 : i_44 >= end_45; i_44 += step_46) {
V v_43 = i_44;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_39}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_42, (V)"hp") == (V)10.0d))));
var values_47 = R.Join(new V[] {}, new Results(R.Get(v_39, (V)"clock")));
V v_48 = values_47[0];

var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_39}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_50 = values_49[0];
V v_51 = values_49[1];
V v_52 = values_49[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_42, (V)"hp") == (V)8.0d), () => (V)(R.Get(v_39, (V)"clock") == (V)(v_48 + (V)140.0d))))));
var values_53 = R.Join(new V[] {}, new Results((V)0.0d));
V v_54 = values_53[0];

foreach (var row_55 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_52)))))) {
V v_56 = row_55[0];
V v_57 = row_55[1];
if (R.Truth((V)(R.Get(v_57, (V)"kind") == (V)"AttackPerformed"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_57, (V)"source_id") == R.Get(v_40, (V)"id")), () => (V)(R.Get(v_57, (V)"from_x") == (V)9.0d)))));

var values_58 = R.Join(new V[] {}, new Results((V)(v_54 + (V)1.0d)));
v_54 = values_58[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_54 == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"repositioning preserves the single familiar ID and cannot scout or cross a wall"}, new Results(new V((RuleFunction)(args_59 => {

var values_60 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_61 = values_60[0];
V v_62 = values_60[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_61}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

var values_63 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"owned"), R.Join(new V[] {v_61}, new Results((V)"spirit"))));
V v_64 = values_63[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_61}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(R.Get(v_7, (V)"owned"), R.Join(new V[] {v_61}, new Results((V)"spirit"))).First, (V)"id") == R.Get(v_64, (V)"id")), () => (V)(R.Get(R.Get(v_64, (V)"position"), (V)"x") == (V)5.0d)))));
var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_61, (V)"explored")))));
V v_66 = values_65[0];

{ double end_69 = R.Num((V)20.0d), step_70 = R.Num((V)1.0d);
for (double i_68 = R.Num((V)1.0d); step_70 > 0 ? i_68 <= end_69 : i_68 >= end_69; i_68 += step_70) {
V v_67 = i_68;
R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_61, (V)"sweeping_slash", (V)1.0d}, new Results((V)1.0d)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_66 == R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_61, (V)"explored")))).First))));
var values_71 = R.Join(new V[] {(V)8.0d}, new Results((V)4.0d));
var obj_72 = R.Get(v_62, (V)"position");
V key_73 = (V)"x";
var obj_74 = R.Get(v_62, (V)"position");
V key_75 = (V)"y";
R.Set(obj_72, key_73, values_71[0]);
R.Set(obj_74, key_75, values_71[1]);

var values_76 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_61, (V)"sweeping_slash", (V)1.0d}, new Results((V)1.0d))));
V v_77 = values_76[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_77, (V)"valid"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an enemy outside familiar reach rejects the command at zero cost"}, new Results(new V((RuleFunction)(args_78 => {

var values_79 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_80 = values_79[0];
V v_81 = values_79[1];

R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_80, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d)));
var values_82 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_80))));
V v_83 = values_82[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_80}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_83}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_80)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_80}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))))));

var values_84 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_80))));
v_83 = values_84[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_80}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First)))));

R.Call(v_3, R.Join(new V[] {v_83}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_80)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a spirit command uses one ordinary proc pipeline and placement manufactures no attacks or Echo"}, new Results(new V((RuleFunction)(args_85 => {

var values_86 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_87 = values_86[0];
V v_88 = values_86[1];

R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_87, R.Get(v_88, (V)"id"), (V)"core:boon/temporal_echo", (V)"common"}, new Results((V)1.0d)));
var values_89 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_87}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_90 = values_89[0];
V v_91 = values_89[1];
V v_92 = values_89[2];
foreach (var row_93 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_92)))))) {
V v_94 = row_93[0];
V v_95 = row_93[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_95, (V)"kind") != (V)"AttackPerformed"), () => (V)(R.Get(v_95, (V)"kind") != (V)"AbilityUsed")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_87, (V)"delayed_effects")))).First)))));
var values_96 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_87, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_97 = values_96[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_87}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_97, (V)"hp") < (V)10.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"owned familiar and next commanded attack roundtrip exactly and reject forged ownership"}, new Results(new V((RuleFunction)(args_98 => {

var values_99 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_100 = values_99[0];
V v_101 = values_99[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_100}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_100, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d)));
var values_102 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_100))));
V v_103 = values_102[0];

var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_103))))))));
V v_105 = values_104[0];
var values_106 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_100}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_107 = values_106[0];
V v_108 = values_106[1];
V v_109 = values_106[2];

var values_110 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_105}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_111 = values_110[0];
V v_112 = values_110[1];
V v_113 = values_110[2];
R.Call(v_3, R.Join(new V[] {v_109}, new Results(v_113)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_100))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_105)))));
foreach (var row_114 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_103, (V)"entities"))))))) {
V v_115 = row_114[0];
V v_116 = row_114[1];
if (R.Truth((V)(R.Get(v_116, (V)"controller") == R.Get(v_7, (V)"controller")))) {
var values_117 = R.Join(new V[] {}, new Results((V)9999.0d));
var obj_118 = v_116;
V key_119 = (V)"owner_id";
R.Set(obj_118, key_119, values_117[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_103))).First)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
