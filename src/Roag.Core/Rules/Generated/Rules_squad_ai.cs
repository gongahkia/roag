// Static C# port of src/squad_ai.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: bf2bf736ad6f506b53eb8bdf86cd07c7ac630c409c9e4451522de4c73256aa00
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_squad_ai { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table()));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_6 = values_5[0];
V v_7 = V.Nil;
v_7 = new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
V v_10 = args_8[1];
V v_11 = args_8[2];
var values_12 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", R.Concat((V)"core:ability/", v_9)), new Entry((V)"cells", v_10))));
V v_13 = values_12[0];
foreach (var row_14 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(v_11, () => R.Table()))))))) {
V v_15 = row_14[0];
V v_16 = row_14[1];
var values_17 = R.Join(new V[] {}, new Results(v_16));
var obj_18 = v_13;
V key_19 = v_15;
R.Set(obj_18, key_19, values_17[0]);
}
return R.Join(new V[] {}, new Results(v_13));
return Results.Empty;
}));
V v_20 = V.Nil;
v_20 = new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_22, (V)"x")), new Entry((V)"y", R.Get(v_22, (V)"y")))));
return Results.Empty;
}));
R.Set(v_4, (V)"choose", new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
V v_25 = args_23[1];
V v_26 = args_23[2];
V v_27 = args_23[3];
var values_28 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"roles"), R.Get(v_25, (V)"definition_id"))))));
V v_29 = values_28[0];
var values_30 = R.Join(new V[] {R.Get(v_26, (V)"position")}, new Results(R.Get(v_25, (V)"position")));
V v_31 = values_30[0];
V v_32 = values_30[1];
var values_33 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"distance"), R.Join(new V[] {v_32}, new Results(v_31))));
V v_34 = values_33[0];
var values_35 = R.Join(new V[] {}, new Results(R.And((V)(v_34 <= R.Get(R.Get(v_27, (V)"definition"), (V)"range")), () => R.Call(R.Get(v_27, (V)"clear_line"), R.Join(new V[] {v_24, v_32}, new Results(v_31))).First)));
V v_36 = values_35[0];
V v_37 = V.Nil;
v_37 = new V((RuleFunction)(args_38 => {
V v_39 = args_38[0];
V v_40 = args_38[1];
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_27, (V)"next_step"), R.Join(new V[] {v_24, v_25}, R.Call(R.Get(v_27, (V)"adjacent_goals"), R.Join(new V[] {v_24, v_25, R.Or(v_39, () => v_26)}, new Results(v_40))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
return Results.Empty;
}));
V v_41 = V.Nil;
v_41 = new V((RuleFunction)(args_42 => {

foreach (var row_43 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_6)))))) {
V v_44 = row_43[0];
V v_45 = row_43[1];
var values_46 = R.Join(new V[] {(V)(R.Get(v_32, (V)"x") + R.Get(v_45, (V)1.0d))}, new Results((V)(R.Get(v_32, (V)"y") + R.Get(v_45, (V)2.0d))));
V v_47 = values_46[0];
V v_48 = values_46[1];
if (R.Truth(R.And(R.Call(R.Get(v_27, (V)"free"), R.Join(new V[] {v_24, R.Get(v_25, (V)"id"), v_47}, new Results(v_48))).First, () => (V)(R.Call(R.Get(v_2, (V)"distance"), R.Join(new V[] {R.Table(new Entry((V)"x", v_47), new Entry((V)"y", v_48))}, new Results(v_31))).First > v_34)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_45, (V)1.0d)), new Entry((V)"dy", R.Get(v_45, (V)2.0d)))));
}
}
return Results.Empty;
}));
V v_49 = V.Nil;
v_49 = new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
if (R.Truth((V)(v_34 == (V)1.0d))) {
return R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {v_51}, new Results(R.Table(new Entry(1, R.Call(v_20, R.Join(new V[] {}, new Results(v_31)))))))));
}
return R.Join(new V[] {}, R.Call(v_37, Results.Empty));
return Results.Empty;
}));
if (R.Truth((V)(v_29 == (V)"ward_weaver"))) {
if (R.Truth(R.And((V)(!R.Truth(R.Get(R.Get(v_25, (V)"ai_memory"), (V)"ward_ready"))), () => (V)(v_34 <= (V)8.0d)))) {
return R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {(V)"ward_channel", R.Table(new Entry(1, R.Call(v_20, R.Join(new V[] {}, new Results(v_32)))))}, new Results(R.Table(new Entry((V)"support", R.Table(new Entry((V)"kind", (V)"ward"))))))));
}
if (R.Truth((V)(v_34 <= (V)2.0d))) {
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_41, Results.Empty).First, () => R.Call(v_49, R.Join(new V[] {}, new Results((V)"ward_tap"))).First)));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))));
} else if (R.Truth((V)(v_29 == (V)"bodyguard_brute"))) {
var values_52 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"leader"), R.Join(new V[] {v_24}, new Results(v_25))));
V v_53 = values_52[0];
if (R.Truth(R.And(v_53, () => (V)(R.Call(R.Get(v_2, (V)"distance"), R.Join(new V[] {v_32}, new Results(R.Get(v_53, (V)"position")))).First > R.Get(v_2, (V)"guard_leash"))))) {
return R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {}, new Results(v_53))));
}
if (R.Truth((V)(v_34 == (V)1.0d))) {
var values_54 = R.Join(new V[] {(V)(R.Get(v_31, (V)"x") - R.Get(v_32, (V)"x"))}, new Results((V)(R.Get(v_31, (V)"y") - R.Get(v_32, (V)"y"))));
V v_55 = values_54[0];
V v_56 = values_54[1];
var values_57 = R.Join(new V[] {}, new Results(R.Table(new Entry(1, R.Call(v_20, R.Join(new V[] {}, new Results(v_31)))))));
V v_58 = values_57[0];
foreach (var row_59 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d)))))))) {
V v_60 = row_59[0];
V v_61 = row_59[1];
var values_62 = R.Join(new V[] {(V)(R.Get(v_31, (V)"x") - (V)(v_56 * v_61))}, new Results((V)(R.Get(v_31, (V)"y") + (V)(v_55 * v_61))));
V v_63 = values_62[0];
V v_64 = values_62[1];
if (R.Truth(R.Call(R.Get(v_27, (V)"projectile_passable"), R.Join(new V[] {R.Get(v_27, (V)"map"), v_63}, new Results(v_64))).First)) {
var values_65 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_63), new Entry((V)"y", v_64))));
var obj_66 = v_58;
V key_67 = (V)((V)R.Length(v_58) + (V)1.0d);
R.Set(obj_66, key_67, values_65[0]);
}
}
return R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {(V)"brute_sweep"}, new Results(v_58))));
}
var values_68 = R.Join(new V[] {}, R.Call(v_37, Results.Empty));
V v_69 = values_68[0];
if (R.Truth(R.And(R.And(v_53, () => (V)(R.Get(v_69, (V)"kind") == (V)"move")), () => (V)(R.Call(R.Get(v_2, (V)"distance"), R.Join(new V[] {R.Table(new Entry((V)"x", (V)(R.Get(v_32, (V)"x") + R.Get(v_69, (V)"dx"))), new Entry((V)"y", (V)(R.Get(v_32, (V)"y") + R.Get(v_69, (V)"dy"))))}, new Results(R.Get(v_53, (V)"position")))).First > R.Get(v_2, (V)"guard_leash"))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))));
}
return R.Join(new V[] {}, new Results(v_69));
} else if (R.Truth((V)(v_29 == (V)"mend_acolyte"))) {
var values_70 = Results.Empty;
V v_71 = values_70[0];
foreach (var row_72 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"entities"))))))) {
V v_73 = row_72[0];
V v_74 = row_72[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_74, (V)"id") != R.Get(v_25, (V)"id")), () => R.Get(v_74, (V)"squad_id")), () => (V)(R.Get(v_74, (V)"squad_id") == R.Get(v_25, (V)"squad_id"))), () => (V)(R.Get(v_74, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_74, (V)"hp") < R.Get(v_74, (V)"max_hp"))), () => (V)(R.Call(R.Get(v_2, (V)"distance"), R.Join(new V[] {v_32}, new Results(R.Get(v_74, (V)"position")))).First <= (V)4.0d)), () => R.Call(R.Get(v_2, (V)"clear"), R.Join(new V[] {v_24, v_32}, new Results(R.Get(v_74, (V)"position")))).First), () => R.Or((V)(!R.Truth(v_71)), () => (V)((V)(R.Get(v_74, (V)"hp") * R.Get(v_71, (V)"max_hp")) < (V)(R.Get(v_71, (V)"hp") * R.Get(v_74, (V)"max_hp"))))))) {
var values_75 = R.Join(new V[] {}, new Results(v_74));
v_71 = values_75[0];
}
}
if (R.Truth(v_71)) {
return R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {(V)"acolyte_mend", R.Table(new Entry(1, R.Call(v_20, R.Join(new V[] {}, new Results(R.Get(v_71, (V)"position"))))))}, new Results(R.Table(new Entry((V)"support", R.Table(new Entry((V)"kind", (V)"heal"), new Entry((V)"power", (V)2.0d), new Entry((V)"target_id", R.Get(v_71, (V)"id")))))))));
}
if (R.Truth((V)(v_34 <= (V)2.0d))) {
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_41, Results.Empty).First, () => R.Call(v_49, R.Join(new V[] {}, new Results((V)"acolyte_tap"))).First)));
}
if (R.Truth((V)(v_34 > (V)5.0d))) {
return R.Join(new V[] {}, R.Call(v_37, Results.Empty));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))));
} else if (R.Truth(R.Or((V)(v_29 == (V)"rushwing"), () => (V)(v_29 == (V)"banner_follower")))) {
if (R.Truth((V)(v_34 == (V)1.0d))) {
return R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {R.Or(R.And((V)(v_29 == (V)"rushwing"), () => (V)"rushwing_bite"), () => (V)"follower_strike")}, new Results(R.Table(new Entry(1, R.Call(v_20, R.Join(new V[] {}, new Results(v_31)))))))));
}
var values_76 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"leader"), R.Join(new V[] {v_24}, new Results(v_25))));
V v_77 = values_76[0];
if (R.Truth(R.And(R.And(R.And(R.And((V)(v_29 == (V)"rushwing"), () => v_77), () => (V)(R.Get(v_25, (V)"hp") < R.Get(v_25, (V)"max_hp"))), () => (V)(v_34 > (V)2.0d)), () => (V)(R.Call(R.Get(v_2, (V)"distance"), R.Join(new V[] {v_32}, new Results(R.Get(v_77, (V)"position")))).First > (V)2.0d)))) {
return R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {}, new Results(v_77))));
}
var values_78 = R.Join(new V[] {}, new Results(R.Get(v_6, (V)((V)(R.Get(v_25, (V)"id") % (V)4.0d) + (V)1.0d))));
V v_79 = values_78[0];
var values_80 = R.Join(new V[] {(V)(R.Get(v_31, (V)"x") + R.Get(v_79, (V)1.0d))}, new Results((V)(R.Get(v_31, (V)"y") + R.Get(v_79, (V)2.0d))));
V v_81 = values_80[0];
V v_82 = values_80[1];
if (R.Truth(R.Call(R.Get(v_27, (V)"free"), R.Join(new V[] {v_24, R.Get(v_25, (V)"id"), v_81}, new Results(v_82))).First)) {
var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_27, (V)"next_step"), R.Join(new V[] {v_24, v_25}, new Results(R.Table(new Entry((V)((V)((V)(v_82 - (V)1.0d) * R.Get(R.Get(v_27, (V)"map"), (V)"width")) + v_81), (V)true))))));
V v_84 = values_83[0];
if (R.Truth(v_84)) {
return R.Join(new V[] {}, new Results(v_84));
}
}
return R.Join(new V[] {}, R.Call(v_37, Results.Empty));
} else if (R.Truth((V)(v_29 == (V)"snare_scribe"))) {
var values_85 = R.Join(new V[] {(V)0.0d}, new Results(R.Table()));
V v_86 = values_85[0];
V v_87 = values_85[1];
foreach (var row_88 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_24, (V)"enemy_snares"), () => R.Table()))))))) {
V v_89 = row_88[0];
V v_90 = row_88[1];
if (R.Truth((V)(R.Get(v_90, (V)"source_id") == R.Get(v_25, (V)"id")))) {
var values_91 = R.Join(new V[] {}, new Results((V)(v_86 + (V)1.0d)));
v_86 = values_91[0];
}
if (R.Truth((V)(R.Get(v_90, (V)"floor_id") == R.Get(v_32, (V)"floor_id")))) {
var values_92 = R.Join(new V[] {}, new Results((V)true));
var obj_93 = v_87;
V key_94 = R.Concat(R.Get(v_90, (V)"x"), R.Concat((V)",", R.Get(v_90, (V)"y")));
R.Set(obj_93, key_94, values_92[0]);
}
}
var values_95 = R.Join(new V[] {}, new Results(R.Table()));
V v_96 = values_95[0];
foreach (var row_97 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_6)))))) {
V v_98 = row_97[0];
V v_99 = row_97[1];
var values_100 = R.Join(new V[] {(V)(R.Get(v_31, (V)"x") + R.Get(v_99, (V)1.0d))}, new Results((V)(R.Get(v_31, (V)"y") + R.Get(v_99, (V)2.0d))));
V v_101 = values_100[0];
V v_102 = values_100[1];
if (R.Truth(R.And(R.Call(R.Get(v_27, (V)"free"), R.Join(new V[] {v_24, R.Get(v_26, (V)"id"), v_101}, new Results(v_102))).First, () => (V)(!R.Truth(R.Get(v_87, R.Concat(v_101, R.Concat((V)",", v_102)))))))) {
var values_103 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_101), new Entry((V)"y", v_102))));
var obj_104 = v_96;
V key_105 = (V)((V)R.Length(v_96) + (V)1.0d);
R.Set(obj_104, key_105, values_103[0]);
}
}
if (R.Truth(R.And(R.And(v_36, () => (V)(v_86 < R.Get(v_2, (V)"snare_cap"))), () => (V)((V)R.Length(v_96) >= (V)2.0d)))) {
var values_106 = R.Join(new V[] {}, new Results(R.Get(v_96, (V)((V)(R.Get(v_25, (V)"id") % (V)R.Length(v_96)) + (V)1.0d))));
V v_107 = values_106[0];
return R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {(V)"scribe_snare", R.Table(new Entry((V)1, v_107))}, new Results(R.Table(new Entry((V)"support", R.Table(new Entry((V)"kind", (V)"snare"))))))));
}
if (R.Truth((V)(v_34 <= (V)2.0d))) {
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_41, Results.Empty).First, () => R.Call(v_49, R.Join(new V[] {}, new Results((V)"scribe_tap"))).First)));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))));
} else if (R.Truth((V)(v_29 == (V)"siege_wisp"))) {
if (R.Truth(v_36)) {
var values_108 = R.Join(new V[] {}, new Results(R.Table()));
V v_109 = values_108[0];
foreach (var row_110 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d)))))))))) {
V v_111 = row_110[0];
V v_112 = row_110[1];
var values_113 = R.Join(new V[] {(V)(R.Get(v_31, (V)"x") + R.Get(v_112, (V)1.0d))}, new Results((V)(R.Get(v_31, (V)"y") + R.Get(v_112, (V)2.0d))));
V v_114 = values_113[0];
V v_115 = values_113[1];
if (R.Truth(R.Call(R.Get(v_27, (V)"projectile_passable"), R.Join(new V[] {R.Get(v_27, (V)"map"), v_114}, new Results(v_115))).First)) {
var values_116 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_114), new Entry((V)"y", v_115))));
var obj_117 = v_109;
V key_118 = (V)((V)R.Length(v_109) + (V)1.0d);
R.Set(obj_117, key_118, values_116[0]);
}
}
return R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {(V)"siege_square", v_109}, new Results(R.Table(new Entry((V)"ranged", (V)"area"))))));
}
return R.Join(new V[] {}, R.Call(v_37, Results.Empty));
} else if (R.Truth((V)(v_29 == (V)"shield_bearer"))) {
var values_119 = R.Join(new V[] {(V)(R.Get(v_31, (V)"x") - R.Get(v_32, (V)"x"))}, new Results((V)(R.Get(v_31, (V)"y") - R.Get(v_32, (V)"y"))));
V v_120 = values_119[0];
V v_121 = values_119[1];
var values_122 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_123 = values_122[0];
V v_124 = values_122[1];
if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_120))).First >= R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_121))).First))) {
var values_125 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_120 >= (V)0.0d), () => (V)1.0d), () => (-(V)1.0d))));
v_123 = values_125[0];
} else {
var values_126 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_121 >= (V)0.0d), () => (V)1.0d), () => (-(V)1.0d))));
v_124 = values_126[0];
}
var values_127 = R.Join(new V[] {}, R.Call(v_49, R.Join(new V[] {}, new Results((V)"shield_bash"))));
V v_128 = values_127[0];
var values_129 = R.Join(new V[] {v_123}, new Results(v_124));
var obj_130 = v_128;
V key_131 = (V)"shield_dx";
var obj_132 = v_128;
V key_133 = (V)"shield_dy";
R.Set(obj_130, key_131, values_129[0]);
R.Set(obj_132, key_133, values_129[1]);
return R.Join(new V[] {}, new Results(v_128));
} else if (R.Truth((V)(v_29 == (V)"pin_archer"))) {
var values_134 = R.Join(new V[] {(V)(R.Get(v_31, (V)"x") - R.Get(v_32, (V)"x"))}, new Results((V)(R.Get(v_31, (V)"y") - R.Get(v_32, (V)"y"))));
V v_135 = values_134[0];
V v_136 = values_134[1];
if (R.Truth(R.And(R.And(R.Or((V)(v_135 == (V)0.0d), () => (V)(v_136 == (V)0.0d)), () => (V)(v_34 >= (V)2.0d)), () => v_36))) {
var values_137 = R.Join(new V[] {R.Or(R.And((V)(v_135 == (V)0.0d), () => (V)0.0d), () => (V)(v_135 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_135))).First))}, new Results(R.Or(R.And((V)(v_136 == (V)0.0d), () => (V)0.0d), () => (V)(v_136 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_136))).First))));
V v_138 = values_137[0];
V v_139 = values_137[1];
var values_140 = R.Join(new V[] {}, new Results(R.Table()));
V v_141 = values_140[0];
var values_142 = R.Join(new V[] {}, new Results((V)false));
V v_143 = values_142[0];
{ double end_146 = R.Num(v_34), step_147 = R.Num((V)1.0d);
for (double i_145 = R.Num((V)1.0d); step_147 > 0 ? i_145 <= end_146 : i_145 >= end_146; i_145 += step_147) {
V v_144 = i_145;
var values_148 = R.Join(new V[] {(V)(R.Get(v_32, (V)"x") + (V)(v_138 * v_144))}, new Results((V)(R.Get(v_32, (V)"y") + (V)(v_139 * v_144))));
V v_149 = values_148[0];
V v_150 = values_148[1];
foreach (var row_151 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"entities"))))))) {
V v_152 = row_151[0];
V v_153 = row_151[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_153, (V)"id") != R.Get(v_25, (V)"id")), () => (V)(R.Get(v_153, (V)"faction") == R.Get(v_25, (V)"faction"))), () => R.Get(v_153, (V)"hp")), () => (V)(R.Get(v_153, (V)"hp") > (V)0.0d)), () => (V)(R.Get(R.Get(v_153, (V)"position"), (V)"floor_id") == R.Get(v_32, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_153, (V)"position"), (V)"x") == v_149)), () => (V)(R.Get(R.Get(v_153, (V)"position"), (V)"y") == v_150)))) {
var values_154 = R.Join(new V[] {}, new Results((V)true));
v_143 = values_154[0];
}
}
var values_155 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_149), new Entry((V)"y", v_150))));
var obj_156 = v_141;
V key_157 = (V)((V)R.Length(v_141) + (V)1.0d);
R.Set(obj_156, key_157, values_155[0]);
}
}
if (R.Truth((V)(!R.Truth(v_143)))) {
return R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {(V)"pin_shot", v_141}, new Results(R.Table(new Entry((V)"ranged", (V)true))))));
}
}
if (R.Truth((V)(v_34 <= (V)2.0d))) {
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_41, Results.Empty).First, () => R.Call(v_49, R.Join(new V[] {}, new Results((V)"pin_tap"))).First)));
}
var values_158 = R.Join(new V[] {}, new Results(R.Table()));
V v_159 = values_158[0];
foreach (var row_160 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_6)))))) {
V v_161 = row_160[0];
V v_162 = row_160[1];
{ double end_165 = R.Num(R.Get(R.Get(v_27, (V)"definition"), (V)"range")), step_166 = R.Num((V)1.0d);
for (double i_164 = R.Num((V)2.0d); step_166 > 0 ? i_164 <= end_165 : i_164 >= end_165; i_164 += step_166) {
V v_163 = i_164;
var values_167 = R.Join(new V[] {(V)(R.Get(v_31, (V)"x") + (V)(R.Get(v_162, (V)1.0d) * v_163))}, new Results((V)(R.Get(v_31, (V)"y") + (V)(R.Get(v_162, (V)2.0d) * v_163))));
V v_168 = values_167[0];
V v_169 = values_167[1];
if (R.Truth(R.And(R.Call(R.Get(v_27, (V)"free"), R.Join(new V[] {v_24, R.Get(v_25, (V)"id"), v_168}, new Results(v_169))).First, () => R.Call(R.Get(v_27, (V)"clear_line"), R.Join(new V[] {v_24, R.Table(new Entry((V)"x", v_168), new Entry((V)"y", v_169))}, new Results(v_31))).First))) {
var values_170 = R.Join(new V[] {}, new Results((V)true));
var obj_171 = v_159;
V key_172 = (V)((V)((V)(v_169 - (V)1.0d) * R.Get(R.Get(v_27, (V)"map"), (V)"width")) + v_168);
R.Set(obj_171, key_172, values_170[0]);
}
}
}
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_27, (V)"next_step"), R.Join(new V[] {v_24, v_25}, new Results(v_159))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
} else if (R.Truth((V)(v_29 == (V)"banner_captain"))) {
return R.Join(new V[] {}, R.Call(v_49, R.Join(new V[] {}, new Results((V)"captain_cut"))));
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
