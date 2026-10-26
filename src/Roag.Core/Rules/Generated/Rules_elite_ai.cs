// Static C# port of src/elite_ai.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 17722434009968eb1adcc1ec3fb2db8380255590c98bd0d481cee425b0d12085
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_elite_ai { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:enemy/mycelial_matriarch", R.Table(new Entry((V)1, (V)"target_diagonals"), new Entry((V)2, (V)"source_ring"))), new Entry((V)"core:enemy/canal_lockmaster", R.Table(new Entry((V)1, (V)"source_lane"), new Entry((V)2, (V)"target_sidebars"))), new Entry((V)"core:enemy/ash_marshal", R.Table(new Entry((V)1, (V)"source_fan"), new Entry((V)2, (V)"target_cross"))), new Entry((V)"core:enemy/prism_curator", R.Table(new Entry((V)1, (V)"target_star"), new Entry((V)2, (V)"target_ring"))), new Entry((V)"core:enemy/index_keeper", R.Table(new Entry((V)1, (V)"target_row"), new Entry((V)2, (V)"target_columns"))), new Entry((V)"core:enemy/ember_crucible", R.Table(new Entry((V)1, (V)"source_spokes"), new Entry((V)2, (V)"target_box"))))));
var obj_4 = v_2;
V key_5 = (V)"profiles";
R.Set(obj_4, key_5, values_3[0]);
V v_6 = V.Nil;
v_6 = new V((RuleFunction)(args_7 => {
V v_8 = args_7[0];
return R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_8 < (V)0.0d), () => (-(V)1.0d)), () => (V)1.0d)));
return Results.Empty;
}));
V v_9 = V.Nil;
v_9 = new V((RuleFunction)(args_10 => {
V v_11 = args_10[0];
V v_12 = args_10[1];
V v_13 = args_10[2];
V v_14 = args_10[3];
V v_15 = args_10[4];
var values_16 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_17 = values_16[0];
V v_18 = values_16[1];
V v_19 = V.Nil;
v_19 = new V((RuleFunction)(args_20 => {
V v_21 = args_20[0];
V v_22 = args_20[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(v_21 >= (V)1.0d), () => (V)(v_22 >= (V)1.0d)), () => (V)(v_21 <= R.Get(v_14, (V)"width"))), () => (V)(v_22 <= R.Get(v_14, (V)"height"))), () => R.Call(v_15, R.Join(new V[] {v_14, v_21}, new Results(v_22))).First))) {
var values_23 = R.Join(new V[] {}, new Results(R.Concat(v_21, R.Concat((V)",", v_22))));
V v_24 = values_23[0];
if (R.Truth((V)(!R.Truth(R.Get(v_18, v_24))))) {
var values_25 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_21), new Entry((V)"y", v_22))));
var obj_26 = v_17;
V key_27 = (V)((V)R.Length(v_17) + (V)1.0d);
R.Set(obj_26, key_27, values_25[0]);

var values_28 = R.Join(new V[] {}, new Results((V)true));
var obj_29 = v_18;
V key_30 = v_24;
R.Set(obj_29, key_30, values_28[0]);
}
}
return Results.Empty;
}));
var values_31 = R.Join(new V[] {R.Get(v_13, (V)"x")}, new Results(R.Get(v_13, (V)"y")));
V v_32 = values_31[0];
V v_33 = values_31[1];
var values_34 = R.Join(new V[] {R.Get(v_12, (V)"x")}, new Results(R.Get(v_12, (V)"y")));
V v_35 = values_34[0];
V v_36 = values_34[1];
var values_37 = R.Join(new V[] {(V)(v_32 - v_35)}, new Results((V)(v_33 - v_36)));
V v_38 = values_37[0];
V v_39 = values_37[1];
var values_40 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_41 = values_40[0];
V v_42 = values_40[1];
if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_38))).First >= R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_39))).First))) {
var values_43 = R.Join(new V[] {}, R.Call(v_6, R.Join(new V[] {}, new Results(v_38))));
v_41 = values_43[0];
} else {
var values_44 = R.Join(new V[] {}, R.Call(v_6, R.Join(new V[] {}, new Results(v_39))));
v_42 = values_44[0];
}
var values_45 = R.Join(new V[] {(-v_42)}, new Results(v_41));
V v_46 = values_45[0];
V v_47 = values_45[1];
if (R.Truth((V)(v_11 == (V)"target_diagonals"))) {
R.Call(v_19, R.Join(new V[] {v_32}, new Results(v_33)));
foreach (var row_48 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d)))))))))) {
V v_49 = row_48[0];
V v_50 = row_48[1];
R.Call(v_19, R.Join(new V[] {(V)(v_32 + R.Get(v_50, (V)1.0d))}, new Results((V)(v_33 + R.Get(v_50, (V)2.0d)))));
}
} else if (R.Truth((V)(v_11 == (V)"source_ring"))) {
{ double end_53 = R.Num((V)2.0d), step_54 = R.Num((V)1.0d);
for (double i_52 = R.Num((-(V)2.0d)); step_54 > 0 ? i_52 <= end_53 : i_52 >= end_53; i_52 += step_54) {
V v_51 = i_52;
{ double end_57 = R.Num((V)2.0d), step_58 = R.Num((V)1.0d);
for (double i_56 = R.Num((-(V)2.0d)); step_58 > 0 ? i_56 <= end_57 : i_56 >= end_57; i_56 += step_58) {
V v_55 = i_56;
if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_55))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_51))))).First == (V)2.0d))) {
R.Call(v_19, R.Join(new V[] {(V)(v_35 + v_55)}, new Results((V)(v_36 + v_51))));
}
}
}
}
}
} else if (R.Truth((V)(v_11 == (V)"source_lane"))) {
{ double end_61 = R.Num((V)4.0d), step_62 = R.Num((V)1.0d);
for (double i_60 = R.Num((V)1.0d); step_62 > 0 ? i_60 <= end_61 : i_60 >= end_61; i_60 += step_62) {
V v_59 = i_60;
var values_63 = R.Join(new V[] {(V)(v_35 + (V)(v_41 * v_59))}, new Results((V)(v_36 + (V)(v_42 * v_59))));
V v_64 = values_63[0];
V v_65 = values_63[1];
if (R.Truth((V)(!R.Truth(R.Call(v_15, R.Join(new V[] {v_14, v_64}, new Results(v_65))).First)))) {
break;
}
R.Call(v_19, R.Join(new V[] {v_64}, new Results(v_65)));
}
}
} else if (R.Truth((V)(v_11 == (V)"target_sidebars"))) {
{ double end_68 = R.Num((V)1.0d), step_69 = R.Num((V)1.0d);
for (double i_67 = R.Num((-(V)1.0d)); step_69 > 0 ? i_67 <= end_68 : i_67 >= end_68; i_67 += step_69) {
V v_66 = i_67;
R.Call(v_19, R.Join(new V[] {(V)((V)(v_32 + v_46) + (V)(v_41 * v_66))}, new Results((V)((V)(v_33 + v_47) + (V)(v_42 * v_66)))));
R.Call(v_19, R.Join(new V[] {(V)((V)(v_32 - v_46) + (V)(v_41 * v_66))}, new Results((V)((V)(v_33 - v_47) + (V)(v_42 * v_66)))));
}
}
} else if (R.Truth((V)(v_11 == (V)"source_fan"))) {
{ double end_72 = R.Num((V)2.0d), step_73 = R.Num((V)1.0d);
for (double i_71 = R.Num((V)1.0d); step_73 > 0 ? i_71 <= end_72 : i_71 >= end_72; i_71 += step_73) {
V v_70 = i_71;
{ double end_76 = R.Num(v_70), step_77 = R.Num((V)1.0d);
for (double i_75 = R.Num((-v_70)); step_77 > 0 ? i_75 <= end_76 : i_75 >= end_76; i_75 += step_77) {
V v_74 = i_75;
R.Call(v_19, R.Join(new V[] {(V)((V)(v_35 + (V)(v_41 * v_70)) + (V)(v_46 * v_74))}, new Results((V)((V)(v_36 + (V)(v_42 * v_70)) + (V)(v_47 * v_74)))));
}
}
}
}
} else if (R.Truth((V)(v_11 == (V)"target_cross"))) {
R.Call(v_19, R.Join(new V[] {v_32}, new Results(v_33)));
foreach (var row_78 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_79 = row_78[0];
V v_80 = row_78[1];
R.Call(v_19, R.Join(new V[] {(V)(v_32 + R.Get(v_80, (V)1.0d))}, new Results((V)(v_33 + R.Get(v_80, (V)2.0d)))));
}
} else if (R.Truth((V)(v_11 == (V)"target_star"))) {
R.Call(v_19, R.Join(new V[] {v_32}, new Results(v_33)));
{ double end_83 = R.Num((V)2.0d), step_84 = R.Num((V)1.0d);
for (double i_82 = R.Num((V)1.0d); step_84 > 0 ? i_82 <= end_83 : i_82 >= end_83; i_82 += step_84) {
V v_81 = i_82;
foreach (var row_85 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d)))))))))) {
V v_86 = row_85[0];
V v_87 = row_85[1];
R.Call(v_19, R.Join(new V[] {(V)(v_32 + (V)(R.Get(v_87, (V)1.0d) * v_81))}, new Results((V)(v_33 + (V)(R.Get(v_87, (V)2.0d) * v_81)))));
}
}
}
} else if (R.Truth((V)(v_11 == (V)"target_ring"))) {
{ double end_90 = R.Num((V)2.0d), step_91 = R.Num((V)1.0d);
for (double i_89 = R.Num((-(V)2.0d)); step_91 > 0 ? i_89 <= end_90 : i_89 >= end_90; i_89 += step_91) {
V v_88 = i_89;
{ double end_94 = R.Num((V)2.0d), step_95 = R.Num((V)1.0d);
for (double i_93 = R.Num((-(V)2.0d)); step_95 > 0 ? i_93 <= end_94 : i_93 >= end_94; i_93 += step_95) {
V v_92 = i_93;
if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_92))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_88))))).First == (V)2.0d))) {
R.Call(v_19, R.Join(new V[] {(V)(v_32 + v_92)}, new Results((V)(v_33 + v_88))));
}
}
}
}
}
} else if (R.Truth((V)(v_11 == (V)"target_row"))) {
{ double end_98 = R.Num((V)2.0d), step_99 = R.Num((V)1.0d);
for (double i_97 = R.Num((-(V)2.0d)); step_99 > 0 ? i_97 <= end_98 : i_97 >= end_98; i_97 += step_99) {
V v_96 = i_97;
R.Call(v_19, R.Join(new V[] {(V)(v_32 + v_96)}, new Results(v_33)));
}
}
} else if (R.Truth((V)(v_11 == (V)"target_columns"))) {
{ double end_102 = R.Num((V)1.0d), step_103 = R.Num((V)1.0d);
for (double i_101 = R.Num((-(V)1.0d)); step_103 > 0 ? i_101 <= end_102 : i_101 >= end_102; i_101 += step_103) {
V v_100 = i_101;
R.Call(v_19, R.Join(new V[] {(V)(v_32 - (V)1.0d)}, new Results((V)(v_33 + v_100))));

R.Call(v_19, R.Join(new V[] {(V)(v_32 + (V)1.0d)}, new Results((V)(v_33 + v_100))));
}
}
} else if (R.Truth((V)(v_11 == (V)"source_spokes"))) {
{ double end_106 = R.Num((V)3.0d), step_107 = R.Num((V)1.0d);
for (double i_105 = R.Num((V)1.0d); step_107 > 0 ? i_105 <= end_106 : i_105 >= end_106; i_105 += step_107) {
V v_104 = i_105;
R.Call(v_19, R.Join(new V[] {(V)(v_35 + (V)(v_41 * v_104))}, new Results((V)(v_36 + (V)(v_42 * v_104)))));
R.Call(v_19, R.Join(new V[] {(V)(v_35 + (V)(v_46 * v_104))}, new Results((V)(v_36 + (V)(v_47 * v_104)))));
R.Call(v_19, R.Join(new V[] {(V)(v_35 - (V)(v_46 * v_104))}, new Results((V)(v_36 - (V)(v_47 * v_104)))));
}
}
} else if (R.Truth((V)(v_11 == (V)"target_box"))) {
{ double end_110 = R.Num((V)1.0d), step_111 = R.Num((V)1.0d);
for (double i_109 = R.Num((-(V)1.0d)); step_111 > 0 ? i_109 <= end_110 : i_109 >= end_110; i_109 += step_111) {
V v_108 = i_109;
{ double end_114 = R.Num((V)1.0d), step_115 = R.Num((V)1.0d);
for (double i_113 = R.Num((-(V)1.0d)); step_115 > 0 ? i_113 <= end_114 : i_113 >= end_114; i_113 += step_115) {
V v_112 = i_113;
R.Call(v_19, R.Join(new V[] {(V)(v_32 + v_112)}, new Results((V)(v_33 + v_108))));
}
}
}
}
}
return R.Join(new V[] {}, new Results(v_17));
return Results.Empty;
}));
R.Set(v_2, (V)"choose", new V((RuleFunction)(args_116 => {
V v_117 = args_116[0];
V v_118 = args_116[1];
V v_119 = args_116[2];
V v_120 = args_116[3];
var values_121 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_2, (V)"profiles"), R.Get(v_118, (V)"definition_id"))}, new Results((V)"unknown biome elite"))));
V v_122 = values_121[0];
var values_123 = R.Join(new V[] {}, new Results(R.Get(v_120, (V)"definition")));
V v_124 = values_123[0];
var values_125 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_119, (V)"position"), (V)"x") - R.Get(R.Get(v_118, (V)"position"), (V)"x"))));
V v_126 = values_125[0];
var values_127 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_119, (V)"position"), (V)"y") - R.Get(R.Get(v_118, (V)"position"), (V)"y"))));
V v_128 = values_127[0];
var values_129 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_126))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_128))).First)));
V v_130 = values_129[0];
if (R.Truth(R.And((V)(v_130 <= R.Get(v_124, (V)"range")), () => R.Call(R.Get(v_120, (V)"clear_line"), R.Join(new V[] {v_117, R.Get(v_118, (V)"position")}, new Results(R.Get(v_119, (V)"position")))).First))) {
var values_131 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_118, (V)"ai_memory"), (V)"attacks"), () => (V)0.0d)));
V v_132 = values_131[0];
var values_133 = R.Join(new V[] {}, new Results((V)((V)((V)(v_132 + R.Or(R.And(R.Get(v_118, (V)"phase_two"), () => (V)1.0d), () => (V)0.0d)) % (V)2.0d) + (V)1.0d)));
V v_134 = values_133[0];
var values_135 = R.Join(new V[] {}, new Results(R.Get(v_122, v_134)));
V v_136 = values_135[0];
var values_137 = R.Join(new V[] {}, R.Call(v_9, R.Join(new V[] {v_136, R.Get(v_118, (V)"position"), R.Get(v_119, (V)"position"), R.Get(v_120, (V)"map")}, new Results(R.Get(v_120, (V)"projectile_passable")))));
V v_138 = values_137[0];
if (R.Truth((V)((V)R.Length(v_138) > (V)0.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", R.Concat((V)"core:ability/elite_", v_136)), new Entry((V)"cells", v_138), new Entry((V)"ranged", (V)"area"))));
}
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_120, (V)"next_step"), R.Join(new V[] {v_117, v_118}, R.Call(R.Get(v_120, (V)"adjacent_goals"), R.Join(new V[] {v_117, v_118, v_119}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_139 => {

var values_140 = R.Join(new V[] {}, new Results(R.Table()));
V v_141 = values_140[0];
foreach (var row_142 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"profiles"))))))) {
V v_143 = row_142[0];
V v_144 = row_142[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_143))).First == (V)"string"), () => (V)((V)R.Length(v_144) == (V)2.0d)), () => (V)(R.Get(v_144, (V)1.0d) != R.Get(v_144, (V)2.0d))))));
foreach (var row_145 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_144)))))) {
V v_146 = row_145[0];
V v_147 = row_145[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_141, R.Concat(v_143, v_147))))}, new Results((V)"duplicate elite footprint")));
var values_148 = R.Join(new V[] {}, new Results((V)true));
var obj_149 = v_141;
V key_150 = R.Concat(v_143, v_147);
R.Set(obj_149, key_150, values_148[0]);
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
