// Static C# port of src/corridor_branches.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: d73c895ffd823bd1f917074e4a1b04a85f7a5357157b6f31db20cda1fdf52e61
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_corridor_branches { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table()));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"spur"), new Entry((V)2, (V)"braid"), new Entry((V)3, (V)"switchback"), new Entry((V)4, (V)"hub"), new Entry((V)5, (V)"false_end"))));
var obj_6 = v_4;
V key_7 = (V)"kinds";
R.Set(obj_6, key_7, values_5[0]);
V v_8 = V.Nil;
v_8 = new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
return R.Join(new V[] {}, new Results(R.Concat(v_10, R.Concat((V)",", v_11))));
return Results.Empty;
}));
V v_12 = V.Nil;
v_12 = new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
V v_15 = args_13[1];
var values_16 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_17 = values_16[0];
V v_18 = values_16[1];
V v_19 = V.Nil;
v_19 = new V((RuleFunction)(args_20 => {
V v_21 = args_20[0];
V v_22 = args_20[1];
V v_23 = args_20[2];
V v_24 = args_20[3];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(v_21 == v_23), () => (V)(v_22 == v_24)))));
var values_25 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_21 == v_23), () => (V)0.0d), () => R.Or(R.And((V)(v_23 > v_21), () => (V)1.0d), () => (-(V)1.0d)))));
V v_26 = values_25[0];
var values_27 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_22 == v_24), () => (V)0.0d), () => R.Or(R.And((V)(v_24 > v_22), () => (V)1.0d), () => (-(V)1.0d)))));
V v_28 = values_27[0];
var values_29 = R.Join(new V[] {v_21}, new Results(v_22));
V v_30 = values_29[0];
V v_31 = values_29[1];
while (R.Truth((V)true)) {
if (R.Truth((V)(v_31 != (V)0.0d))) {
var values_32 = R.Join(new V[] {}, R.Call(v_15, R.Join(new V[] {v_30}, new Results(v_31))));
var obj_33 = v_17;
V key_34 = R.Call(v_8, R.Join(new V[] {v_30}, new Results(v_31))).First;
R.Set(obj_33, key_34, values_32[0]);
}
if (R.Truth(R.And((V)(v_30 == v_23), () => (V)(v_31 == v_24)))) {
break;
}
var values_35 = R.Join(new V[] {(V)(v_30 + v_26)}, new Results((V)(v_31 + v_28)));
v_30 = values_35[0];
v_31 = values_35[1];
}
return Results.Empty;
}));
V v_36 = V.Nil;
v_36 = new V((RuleFunction)(args_37 => {
V v_38 = args_37[0];
V v_39 = args_37[1];
V v_40 = args_37[2];
var values_41 = R.Join(new V[] {}, R.Call(v_15, R.Join(new V[] {v_39}, new Results(v_40))));
V v_42 = values_41[0];
var values_43 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_38), new Entry((V)"x", R.Get(v_42, (V)"x")), new Entry((V)"y", R.Get(v_42, (V)"y")))));
var obj_44 = v_18;
V key_45 = (V)((V)R.Length(v_18) + (V)1.0d);
R.Set(obj_44, key_45, values_43[0]);
return Results.Empty;
}));
if (R.Truth((V)(v_14 == (V)"spur"))) {
R.Call(v_19, R.Join(new V[] {(V)3.0d, (V)0.0d, (V)3.0d}, new Results((V)3.0d)));

R.Call(v_19, R.Join(new V[] {(V)3.0d, (V)3.0d, (V)5.0d}, new Results((V)3.0d)));
R.Call(v_36, R.Join(new V[] {(V)"clue", (V)3.0d}, new Results((V)2.0d)));

R.Call(v_36, R.Join(new V[] {(V)"reward", (V)5.0d}, new Results((V)3.0d)));
} else if (R.Truth((V)(v_14 == (V)"braid"))) {
R.Call(v_19, R.Join(new V[] {(V)1.0d, (V)0.0d, (V)1.0d}, new Results((V)2.0d)));

R.Call(v_19, R.Join(new V[] {(V)1.0d, (V)2.0d, (V)6.0d}, new Results((V)2.0d)));

R.Call(v_19, R.Join(new V[] {(V)6.0d, (V)2.0d, (V)6.0d}, new Results((V)0.0d)));
R.Call(v_19, R.Join(new V[] {(V)4.0d, (V)2.0d, (V)4.0d}, new Results((V)4.0d)));
R.Call(v_36, R.Join(new V[] {(V)"key", (V)2.0d}, new Results((V)2.0d)));

R.Call(v_36, R.Join(new V[] {(V)"gate", (V)4.0d}, new Results((V)3.0d)));

R.Call(v_36, R.Join(new V[] {(V)"reward", (V)4.0d}, new Results((V)4.0d)));
} else if (R.Truth((V)(v_14 == (V)"switchback"))) {
R.Call(v_19, R.Join(new V[] {(V)2.0d, (V)0.0d, (V)2.0d}, new Results((V)3.0d)));

R.Call(v_19, R.Join(new V[] {(V)5.0d, (V)0.0d, (V)5.0d}, new Results((V)2.0d)));

R.Call(v_19, R.Join(new V[] {(V)5.0d, (V)2.0d, (V)6.0d}, new Results((V)2.0d)));
R.Call(v_19, R.Join(new V[] {(V)6.0d, (V)2.0d, (V)6.0d}, new Results((V)3.0d)));
R.Call(v_36, R.Join(new V[] {(V)"plate", (V)2.0d}, new Results((V)3.0d)));

R.Call(v_36, R.Join(new V[] {(V)"gate", (V)6.0d}, new Results((V)2.0d)));

R.Call(v_36, R.Join(new V[] {(V)"reward", (V)6.0d}, new Results((V)3.0d)));
} else if (R.Truth((V)(v_14 == (V)"hub"))) {
R.Call(v_19, R.Join(new V[] {(V)3.0d, (V)0.0d, (V)3.0d}, new Results((V)3.0d)));

R.Call(v_19, R.Join(new V[] {(V)5.0d, (V)0.0d, (V)5.0d}, new Results((-(V)3.0d))));

R.Call(v_19, R.Join(new V[] {(V)3.0d, (V)1.0d, (V)5.0d}, new Results((V)1.0d)));
R.Call(v_36, R.Join(new V[] {(V)"brazier", (V)3.0d}, new Results((V)3.0d)));

R.Call(v_36, R.Join(new V[] {(V)"brazier", (V)5.0d}, new Results((-(V)3.0d))));

R.Call(v_36, R.Join(new V[] {(V)"reward", (V)4.0d}, new Results((V)1.0d)));
} else if (R.Truth((V)(v_14 == (V)"false_end"))) {
R.Call(v_19, R.Join(new V[] {(V)2.0d, (V)0.0d, (V)2.0d}, new Results((V)3.0d)));

R.Call(v_19, R.Join(new V[] {(V)2.0d, (V)3.0d, (V)6.0d}, new Results((V)3.0d)));

R.Call(v_19, R.Join(new V[] {(V)6.0d, (V)3.0d, (V)6.0d}, new Results((V)0.0d)));
R.Call(v_19, R.Join(new V[] {(V)5.0d, (V)2.0d, (V)5.0d}, new Results((V)3.0d)));
R.Call(v_36, R.Join(new V[] {(V)"clue", (V)3.0d}, new Results((V)3.0d)));

R.Call(v_36, R.Join(new V[] {(V)"gate", (V)4.0d}, new Results((V)3.0d)));
R.Call(v_36, R.Join(new V[] {(V)"enemy", (V)5.0d}, new Results((V)3.0d)));

R.Call(v_36, R.Join(new V[] {(V)"reward", (V)5.0d}, new Results((V)2.0d)));
} else {
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results(R.Concat((V)"unknown corridor branch: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_14))).First))));
}
var values_46 = R.Join(new V[] {}, new Results(R.Table()));
V v_47 = values_46[0];
foreach (var row_48 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_17)))))) {
V v_49 = row_48[0];
V v_50 = row_48[1];
var values_51 = R.Join(new V[] {}, new Results(v_50));
var obj_52 = v_47;
V key_53 = (V)((V)R.Length(v_47) + (V)1.0d);
R.Set(obj_52, key_53, values_51[0]);
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_47}, new Results(new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
return R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_55, (V)"y") == R.Get(v_56, (V)"y")), () => (V)(R.Get(v_55, (V)"x") < R.Get(v_56, (V)"x"))), () => (V)(R.Get(v_55, (V)"y") < R.Get(v_56, (V)"y")))));
return Results.Empty;
})))));
return R.Join(new V[] {v_47}, new Results(v_18));
return Results.Empty;
}));
R.Set(v_4, (V)"attach", new V((RuleFunction)(args_57 => {
V v_58 = args_57[0];
V v_59 = args_57[1];
V v_60 = args_57[2];
V v_61 = args_57[3];
V v_62 = args_57[4];
V v_63 = args_57[5];
var values_64 = R.Join(new V[] {}, new Results(R.Table()));
var obj_65 = v_58;
V key_66 = (V)"corridor_quests";
R.Set(obj_65, key_66, values_64[0]);
if (R.Truth((V)(R.Call(R.Get(v_2, (V)"next_int"), R.Join(new V[] {v_62}, new Results((V)100.0d))).First > (V)70.0d))) {
return Results.Empty;
}
var values_67 = R.Join(new V[] {}, new Results(R.Table()));
V v_68 = values_67[0];
foreach (var row_69 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_58, (V)"corridors"))))))) {
V v_70 = row_69[0];
V v_71 = row_69[1];
var values_72 = R.Join(new V[] {}, new Results(R.Get(v_71, (V)"cells")));
V v_73 = values_72[0];
{ double end_76 = R.Num((V)((V)R.Length(v_73) - (V)9.0d)), step_77 = R.Num((V)1.0d);
for (double i_75 = R.Num((V)3.0d); step_77 > 0 ? i_75 <= end_76 : i_75 >= end_76; i_75 += step_77) {
V v_74 = i_75;
var values_78 = R.Join(new V[] {R.Get(v_73, v_74)}, new Results(R.Get(v_73, (V)(v_74 + (V)1.0d))));
V v_79 = values_78[0];
V v_80 = values_78[1];
var values_81 = R.Join(new V[] {(V)(R.Get(v_80, (V)"x") - R.Get(v_79, (V)"x"))}, new Results((V)(R.Get(v_80, (V)"y") - R.Get(v_79, (V)"y"))));
V v_82 = values_81[0];
V v_83 = values_81[1];
var values_84 = R.Join(new V[] {}, new Results((V)true));
V v_85 = values_84[0];
{ double end_88 = R.Num((V)(v_74 + (V)7.0d)), step_89 = R.Num((V)1.0d);
for (double i_87 = R.Num((V)(v_74 + (V)2.0d)); step_89 > 0 ? i_87 <= end_88 : i_87 >= end_88; i_87 += step_89) {
V v_86 = i_87;
if (R.Truth(R.Or((V)((V)(R.Get(R.Get(v_73, v_86), (V)"x") - R.Get(R.Get(v_73, (V)(v_86 - (V)1.0d)), (V)"x")) != v_82), () => (V)((V)(R.Get(R.Get(v_73, v_86), (V)"y") - R.Get(R.Get(v_73, (V)(v_86 - (V)1.0d)), (V)"y")) != v_83)))) {
var values_90 = R.Join(new V[] {}, new Results((V)false));
v_85 = values_90[0];

break;
}
}
}
if (R.Truth(v_85)) {
var values_91 = R.Join(new V[] {}, new Results((V)true));
V v_92 = values_91[0];
{ double end_95 = R.Num((V)(v_74 + (V)7.0d)), step_96 = R.Num((V)1.0d);
for (double i_94 = R.Num(v_74); step_96 > 0 ? i_94 <= end_95 : i_94 >= end_95; i_94 += step_96) {
V v_93 = i_94;
var values_97 = R.Join(new V[] {}, new Results(R.Get(v_73, v_93)));
V v_98 = values_97[0];
if (R.Truth(R.Get(v_61, R.Call(v_8, R.Join(new V[] {R.Get(v_98, (V)"x")}, new Results(R.Get(v_98, (V)"y")))).First))) {
var values_99 = R.Join(new V[] {}, new Results((V)false));
v_92 = values_99[0];

break;
}
}
}
if (R.Truth(v_92)) {
foreach (var row_100 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d)))))))) {
V v_101 = row_100[0];
V v_102 = row_100[1];
var values_103 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"corridor", v_71), new Entry((V)"first", v_74), new Entry((V)"dx", v_82), new Entry((V)"dy", v_83), new Entry((V)"side", v_102))));
var obj_104 = v_68;
V key_105 = (V)((V)R.Length(v_68) + (V)1.0d);
R.Set(obj_104, key_105, values_103[0]);
}
}
}
}
}
}
if (R.Truth((V)((V)R.Length(v_68) == (V)0.0d))) {
return Results.Empty;
}
var values_106 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"next_int"), R.Join(new V[] {v_62}, new Results((V)R.Length(R.Get(v_4, (V)"kinds"))))));
V v_107 = values_106[0];
var values_108 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"next_int"), R.Join(new V[] {v_62}, new Results((V)R.Length(v_68)))));
V v_109 = values_108[0];
var values_110 = R.Join(new V[] {}, new Results(R.Table()));
V v_111 = values_110[0];
foreach (var row_112 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_58, (V)"corridors"))))))) {
V v_113 = row_112[0];
V v_114 = row_112[1];
foreach (var row_115 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_114, (V)"cells"))))))) {
V v_116 = row_115[0];
V v_117 = row_115[1];
var values_118 = R.Join(new V[] {}, new Results((V)true));
var obj_119 = v_111;
V key_120 = R.Call(v_8, R.Join(new V[] {R.Get(v_117, (V)"x")}, new Results(R.Get(v_117, (V)"y")))).First;
R.Set(obj_119, key_120, values_118[0]);
}
}
{ double end_123 = R.Num((V)((V)R.Length(v_68) - (V)1.0d)), step_124 = R.Num((V)1.0d);
for (double i_122 = R.Num((V)0.0d); step_124 > 0 ? i_122 <= end_123 : i_122 >= end_123; i_122 += step_124) {
V v_121 = i_122;
var values_125 = R.Join(new V[] {}, new Results(R.Get(v_68, (V)((V)((V)((V)(v_109 + v_121) - (V)1.0d) % (V)R.Length(v_68)) + (V)1.0d))));
V v_126 = values_125[0];
var values_127 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_4, (V)"kinds"), (V)((V)((V)((V)(v_107 + v_121) - (V)1.0d) % (V)R.Length(R.Get(v_4, (V)"kinds"))) + (V)1.0d))));
V v_128 = values_127[0];
var values_129 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_126, (V)"corridor"), (V)"cells"), R.Get(v_126, (V)"first"))));
V v_130 = values_129[0];
V v_131 = V.Nil;
v_131 = new V((RuleFunction)(args_132 => {
V v_133 = args_132[0];
V v_134 = args_132[1];
var values_135 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_130, (V)"x") + (V)(v_133 * R.Get(v_126, (V)"dx"))) - (V)((V)(v_134 * R.Get(v_126, (V)"dy")) * R.Get(v_126, (V)"side")))));
V v_136 = values_135[0];
var values_137 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_130, (V)"y") + (V)(v_133 * R.Get(v_126, (V)"dy"))) + (V)((V)(v_134 * R.Get(v_126, (V)"dx")) * R.Get(v_126, (V)"side")))));
V v_138 = values_137[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_136), new Entry((V)"y", v_138))));
return Results.Empty;
}));
var values_139 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {v_128}, new Results(v_131))));
V v_140 = values_139[0];
V v_141 = values_139[1];
var values_142 = R.Join(new V[] {}, new Results((V)true));
V v_143 = values_142[0];
foreach (var row_144 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_140)))))) {
V v_145 = row_144[0];
V v_146 = row_144[1];
var values_147 = R.Join(new V[] {R.Get(v_146, (V)"x")}, new Results(R.Get(v_146, (V)"y")));
V v_148 = values_147[0];
V v_149 = values_147[1];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or((V)(v_148 <= (V)1.0d), () => (V)(v_149 <= (V)1.0d)), () => (V)(v_148 >= R.Get(v_58, (V)"width"))), () => (V)(v_149 >= R.Get(v_58, (V)"height"))), () => R.Get(v_61, R.Call(v_8, R.Join(new V[] {v_148}, new Results(v_149))).First)), () => R.Get(v_111, R.Call(v_8, R.Join(new V[] {v_148}, new Results(v_149))).First)))) {
var values_150 = R.Join(new V[] {}, new Results((V)false));
v_143 = values_150[0];

break;
}
}
if (R.Truth(v_143)) {
foreach (var row_151 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_141)))))) {
V v_152 = row_151[0];
V v_153 = row_151[1];
if (R.Truth(R.And((V)(R.Get(v_153, (V)"kind") == (V)"enemy"), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_153, (V)"x") - R.Get(v_58, (V)"start_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_153, (V)"y") - R.Get(v_58, (V)"start_y"))))).First) < (V)5.0d)))) {
var values_154 = R.Join(new V[] {}, new Results((V)false));
v_143 = values_154[0];

break;
}
}
}
if (R.Truth(v_143)) {
var values_155 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat(R.Get(v_58, (V)"floor_id"), R.Concat((V)":branch:", R.Get(R.Get(v_126, (V)"corridor"), (V)"edge_id")))), new Entry((V)"kind", v_128), new Entry((V)"edge_id", R.Get(R.Get(v_126, (V)"corridor"), (V)"edge_id")), new Entry((V)"cells", v_140), new Entry((V)"nodes", v_141))));
V v_156 = values_155[0];
foreach (var row_157 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_140)))))) {
V v_158 = row_157[0];
V v_159 = row_157[1];
var values_160 = R.Join(new V[] {}, new Results((V)"."));
var obj_161 = R.Get(v_59, R.Get(v_159, (V)"y"));
V key_162 = R.Get(v_159, (V)"x");
R.Set(obj_161, key_162, values_160[0]);
var values_163 = R.Join(new V[] {}, new Results(R.Get(v_63, (V)"corridor_visual")));
var obj_164 = R.Get(v_60, R.Get(v_159, (V)"y"));
V key_165 = R.Get(v_159, (V)"x");
R.Set(obj_164, key_165, values_163[0]);
var values_166 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_159, (V)"x")), new Entry((V)"y", R.Get(v_159, (V)"y")), new Entry((V)"kind", (V)"quest_branch"))));
var obj_167 = R.Get(v_58, (V)"editable_cells");
V key_168 = (V)((V)R.Length(R.Get(v_58, (V)"editable_cells")) + (V)1.0d);
R.Set(obj_167, key_168, values_166[0]);
}
var values_169 = R.Join(new V[] {}, new Results(v_128));
var obj_170 = R.Get(v_126, (V)"corridor");
V key_171 = (V)"branch_kind";
R.Set(obj_170, key_171, values_169[0]);
var values_172 = R.Join(new V[] {}, new Results(v_140));
var obj_173 = R.Get(v_126, (V)"corridor");
V key_174 = (V)"branch_cells";
R.Set(obj_173, key_174, values_172[0]);
var values_175 = R.Join(new V[] {}, new Results(v_156));
var obj_176 = R.Get(v_58, (V)"corridor_quests");
V key_177 = (V)1.0d;
R.Set(obj_176, key_177, values_175[0]);
return Results.Empty;
}
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
