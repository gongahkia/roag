// Static C# port of src/review_features.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 4f53f63d8b4297a97274587de12911a6ebde14e385edbe5f83a5fc611ced7bfa
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_review_features { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table()));
V v_6 = values_5[0];
V v_7 = V.Nil;
v_7 = new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
V v_10 = args_8[1];
return R.Join(new V[] {}, new Results(R.Concat(v_9, R.Concat((V)",", v_10))));
return Results.Empty;
}));
V v_11 = V.Nil;
v_11 = new V((RuleFunction)(args_12 => {
V v_13 = args_12[0];
V v_14 = args_12[1];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_13, (V)"x") - R.Get(v_14, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_13, (V)"y") - R.Get(v_14, (V)"y"))))).First)));
return Results.Empty;
}));
var values_15 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_16 = values_15[0];
V v_17 = V.Nil;
v_17 = new V((RuleFunction)(args_18 => {
V v_19 = args_18[0];
V v_20 = args_18[1];
V v_21 = args_18[2];
V v_22 = args_18[3];
V v_23 = args_18[4];
foreach (var row_24 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_19, (V)"entities"))))))) {
V v_25 = row_24[0];
V v_26 = row_24[1];
var values_27 = R.Join(new V[] {}, new Results(R.Get(v_26, (V)"position")));
V v_28 = values_27[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_26, (V)"id") != v_23), () => R.Get(v_26, (V)"blocks_movement")), () => v_28), () => (V)(R.Get(v_28, (V)"floor_id") == v_20)), () => (V)(R.Get(v_28, (V)"x") == v_21)), () => (V)(R.Get(v_28, (V)"y") == v_22)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
V v_29 = V.Nil;
v_29 = new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
V v_32 = args_30[1];
V v_33 = args_30[2];
foreach (var row_34 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_31, (V)"rooms"), () => R.Table()))))))) {
V v_35 = row_34[0];
V v_36 = row_34[1];
if (R.Truth(R.And(R.And(R.And((V)(v_32 >= R.Get(v_36, (V)"x")), () => (V)(v_32 < (V)(R.Get(v_36, (V)"x") + R.Get(v_36, (V)"width")))), () => (V)(v_33 >= R.Get(v_36, (V)"y"))), () => (V)(v_33 < (V)(R.Get(v_36, (V)"y") + R.Get(v_36, (V)"height")))))) {
return R.Join(new V[] {}, new Results(v_36));
}
}
return Results.Empty;
}));
V v_37 = V.Nil;
v_37 = new V((RuleFunction)(args_38 => {
V v_39 = args_38[0];
V v_40 = args_38[1];
V v_41 = args_38[2];
V v_42 = args_38[3];
V v_43 = args_38[4];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or(R.Get(v_43, R.Call(v_7, R.Join(new V[] {v_41}, new Results(v_42))).First), () => (V)(!R.Truth(R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_40, v_41}, new Results(v_42))).First))), () => (V)(R.Invoke(R.Get(R.Get(v_40, (V)"rows"), v_42), "sub", R.Join(new V[] {v_41}, new Results(v_41))).First != (V)".")), () => R.Call(v_17, R.Join(new V[] {v_39, R.Get(v_40, (V)"floor_id"), v_41}, new Results(v_42))).First), () => (V)(R.Call(v_11, R.Join(new V[] {R.Table(new Entry((V)"x", v_41), new Entry((V)"y", v_42))}, new Results(R.Table(new Entry((V)"x", R.Get(v_40, (V)"start_x")), new Entry((V)"y", R.Get(v_40, (V)"start_y")))))).First < (V)4.0d)), () => (V)(R.Call(v_11, R.Join(new V[] {R.Table(new Entry((V)"x", v_41), new Entry((V)"y", v_42))}, new Results(R.Get(v_40, (V)"elite")))).First < (V)3.0d)))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_44 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_40, (V)"placements"), (V)"chests"), () => R.Table()))))))) {
V v_45 = row_44[0];
V v_46 = row_44[1];
if (R.Truth((V)(R.Call(v_11, R.Join(new V[] {R.Table(new Entry((V)"x", v_41), new Entry((V)"y", v_42))}, new Results(v_46))).First < (V)2.0d))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
foreach (var row_47 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_40, (V)"placements"), (V)"doors"), () => R.Table()))))))) {
V v_48 = row_47[0];
V v_49 = row_47[1];
if (R.Truth((V)(R.Call(v_11, R.Join(new V[] {R.Table(new Entry((V)"x", v_41), new Entry((V)"y", v_42))}, new Results(v_49))).First < (V)2.0d))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_40, (V)"placements"), (V)"secrets"), () => R.Table()))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
if (R.Truth((V)(R.Call(v_11, R.Join(new V[] {R.Table(new Entry((V)"x", v_41), new Entry((V)"y", v_42))}, new Results(v_52))).First < (V)2.0d))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
V v_53 = V.Nil;
v_53 = new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
V v_57 = args_54[2];
var values_58 = R.Join(new V[] {}, new Results(R.Table()));
V v_59 = values_58[0];
foreach (var row_60 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_55, (V)"editable_cells"), () => R.Table()))))))) {
V v_61 = row_60[0];
V v_62 = row_60[1];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_62, (V)"kind") == (V)"room"), () => (V)(R.Get(v_62, (V)"kind") == (V)"corridor")), () => (V)(R.Get(v_62, (V)"kind") == (V)"alcove")), () => (V)(R.Get(v_62, (V)"kind") == (V)"parallel_bank")), () => (V)(R.Get(v_62, (V)"kind") == (V)"courtyard_node")), () => (V)(R.Get(v_62, (V)"kind") == (V)"star_node")), () => (V)(R.Get(v_62, (V)"kind") == (V)"forge_bay")))) {
var values_63 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_62, (V)"x")), new Entry((V)"y", R.Get(v_62, (V)"y")), new Entry((V)"kind", R.Get(v_62, (V)"kind")), new Entry((V)"score", R.Call(R.Get(v_4, (V)"derive"), R.Join(new V[] {v_56}, new Results(R.Concat(R.Get(v_55, (V)"floor_id"), R.Concat((V)":", R.Concat(v_57, R.Concat((V)":", R.Call(v_7, R.Join(new V[] {R.Get(v_62, (V)"x")}, new Results(R.Get(v_62, (V)"y")))).First))))))).First))));
var obj_64 = v_59;
V key_65 = (V)((V)R.Length(v_59) + (V)1.0d);
R.Set(obj_64, key_65, values_63[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_59}, new Results(new V((RuleFunction)(args_66 => {
V v_67 = args_66[0];
V v_68 = args_66[1];
if (R.Truth((V)(R.Get(v_67, (V)"score") != R.Get(v_68, (V)"score")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_67, (V)"score") < R.Get(v_68, (V)"score"))));
}
if (R.Truth((V)(R.Get(v_67, (V)"y") != R.Get(v_68, (V)"y")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_67, (V)"y") < R.Get(v_68, (V)"y"))));
}
return R.Join(new V[] {}, new Results((V)(R.Get(v_67, (V)"x") < R.Get(v_68, (V)"x"))));
return Results.Empty;
})))));
return R.Join(new V[] {}, new Results(v_59));
return Results.Empty;
}));
V v_69 = V.Nil;
v_69 = new V((RuleFunction)(args_70 => {
V v_71 = args_70[0];
V v_72 = args_70[1];
V v_73 = args_70[2];
var values_74 = R.Join(new V[] {}, new Results((V)0.0d));
V v_75 = values_74[0];
foreach (var row_76 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_16)))))) {
V v_77 = row_76[0];
V v_78 = row_76[1];
if (R.Truth(R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_71, (V)(v_72 + R.Get(v_78, (V)1.0d))}, new Results((V)(v_73 + R.Get(v_78, (V)2.0d))))).First)) {
var values_79 = R.Join(new V[] {}, new Results((V)(v_75 + (V)1.0d)));
v_75 = values_79[0];
}
}
return R.Join(new V[] {}, new Results(v_75));
return Results.Empty;
}));
V v_80 = V.Nil;
v_80 = new V((RuleFunction)(args_81 => {
V v_82 = args_81[0];
V v_83 = args_81[1];
V v_84 = args_81[2];
V v_85 = args_81[3];
V v_86 = args_81[4];
var values_87 = R.Join(new V[] {R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_82, (V)"start_x")), new Entry((V)"y", R.Get(v_82, (V)"start_y")))))}, new Results(R.Table(new Entry(R.Call(v_7, R.Join(new V[] {R.Get(v_82, (V)"start_x")}, new Results(R.Get(v_82, (V)"start_y")))).First, (V)true))));
V v_88 = values_87[0];
V v_89 = values_87[1];
var values_90 = R.Join(new V[] {}, new Results((V)1.0d));
V v_91 = values_90[0];
while (R.Truth((V)(v_91 <= (V)R.Length(v_88)))) {
var values_92 = R.Join(new V[] {}, new Results(R.Get(v_88, v_91)));
V v_93 = values_92[0];

var values_94 = R.Join(new V[] {}, new Results((V)(v_91 + (V)1.0d)));
v_91 = values_94[0];
foreach (var row_95 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_16)))))) {
V v_96 = row_95[0];
V v_97 = row_95[1];
var values_98 = R.Join(new V[] {(V)(R.Get(v_93, (V)"x") + R.Get(v_97, (V)1.0d))}, new Results((V)(R.Get(v_93, (V)"y") + R.Get(v_97, (V)2.0d))));
V v_99 = values_98[0];
V v_100 = values_98[1];
var values_101 = R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {v_99}, new Results(v_100))));
V v_102 = values_101[0];
if (R.Truth(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_89, v_102))), () => (V)(!R.Truth(R.And((V)(v_99 == v_83), () => (V)(v_100 == v_84))))), () => (V)(!R.Truth(R.And(v_85, () => R.Get(v_85, v_102))))), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_82, v_99}, new Results(v_100))).First))) {
var values_103 = R.Join(new V[] {}, new Results((V)true));
var obj_104 = v_89;
V key_105 = v_102;
R.Set(obj_104, key_105, values_103[0]);

var values_106 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_99), new Entry((V)"y", v_100))));
var obj_107 = v_88;
V key_108 = (V)((V)R.Length(v_88) + (V)1.0d);
R.Set(obj_107, key_108, values_106[0]);
}
}
}
if (R.Truth((V)(!R.Truth(R.Get(v_89, R.Call(v_7, R.Join(new V[] {R.Get(R.Get(v_82, (V)"elite"), (V)"x")}, new Results(R.Get(R.Get(v_82, (V)"elite"), (V)"y")))).First))))) {
return R.Join(new V[] {(V)false}, new Results(v_89));
}
foreach (var row_109 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_82, (V)"rooms"))))))) {
V v_110 = row_109[0];
V v_111 = row_109[1];
var values_112 = R.Join(new V[] {}, new Results((V)false));
V v_113 = values_112[0];
{ double end_116 = R.Num((V)((V)(R.Get(v_111, (V)"y") + R.Get(v_111, (V)"height")) - (V)1.0d)), step_117 = R.Num((V)1.0d);
for (double i_115 = R.Num(R.Get(v_111, (V)"y")); step_117 > 0 ? i_115 <= end_116 : i_115 >= end_116; i_115 += step_117) {
V v_114 = i_115;
{ double end_120 = R.Num((V)((V)(R.Get(v_111, (V)"x") + R.Get(v_111, (V)"width")) - (V)1.0d)), step_121 = R.Num((V)1.0d);
for (double i_119 = R.Num(R.Get(v_111, (V)"x")); step_121 > 0 ? i_119 <= end_120 : i_119 >= end_120; i_119 += step_121) {
V v_118 = i_119;
if (R.Truth(R.Get(v_89, R.Call(v_7, R.Join(new V[] {v_118}, new Results(v_114))).First))) {
var values_122 = R.Join(new V[] {}, new Results((V)true));
v_113 = values_122[0];

break;
}
}
}
if (R.Truth(v_113)) {
break;
}
}
}
if (R.Truth((V)(!R.Truth(v_113)))) {
return R.Join(new V[] {(V)false}, new Results(v_89));
}
}
if (R.Truth(v_86)) {
var values_123 = R.Join(new V[] {}, R.Call(v_80, R.Join(new V[] {v_82, V.Nil, V.Nil, v_85}, new Results((V)false))));
V v_124 = values_123[0];
V v_125 = values_123[1];
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_125)))))) {
V v_127 = row_126[0];
if (R.Truth(R.And((V)(v_127 != R.Call(v_7, R.Join(new V[] {v_83}, new Results(v_84))).First), () => (V)(!R.Truth(R.Get(v_89, v_127)))))) {
return R.Join(new V[] {(V)false}, new Results(v_89));
}
}
}
return R.Join(new V[] {(V)true}, new Results(v_89));
return Results.Empty;
}));
V v_128 = V.Nil;
v_128 = new V((RuleFunction)(args_129 => {
V v_130 = args_129[0];
V v_131 = args_129[1];
V v_132 = args_129[2];
V v_133 = args_129[3];
V v_134 = args_129[4];
var values_135 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_130, (V)"next_entity_id")), new Entry((V)"definition_id", R.Concat((V)"core:object/review_", v_132)), new Entry((V)"controller", R.Concat((V)"review_", v_132)), new Entry((V)"faction", (V)"neutral"), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_131, (V)"floor_id")), new Entry((V)"x", R.Get(v_133, (V)"x")), new Entry((V)"y", R.Get(v_133, (V)"y")))), new Entry((V)"blocks_movement", (V)true), new Entry((V)"blocks_vision", R.Or((V)(v_132 == (V)"pillar"), () => (V)(v_132 == (V)"gate"))), new Entry((V)"blocks_projectiles", R.Or((V)(v_132 == (V)"pillar"), () => (V)(v_132 == (V)"gate"))))));
V v_136 = values_135[0];
foreach (var row_137 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(v_134, () => R.Table()))))))) {
V v_138 = row_137[0];
V v_139 = row_137[1];
var values_140 = R.Join(new V[] {}, new Results(v_139));
var obj_141 = v_136;
V key_142 = v_138;
R.Set(obj_141, key_142, values_140[0]);
}
var values_143 = R.Join(new V[] {}, new Results((V)(R.Get(v_136, (V)"id") + (V)1.0d)));
var obj_144 = v_130;
V key_145 = (V)"next_entity_id";
R.Set(obj_144, key_145, values_143[0]);

var values_146 = R.Join(new V[] {}, new Results(v_136));
var obj_147 = R.Get(v_130, (V)"entities");
V key_148 = (V)((V)R.Length(R.Get(v_130, (V)"entities")) + (V)1.0d);
R.Set(obj_147, key_148, values_146[0]);
return R.Join(new V[] {}, new Results(v_136));
return Results.Empty;
}));
V v_149 = V.Nil;
v_149 = new V((RuleFunction)(args_150 => {
V v_151 = args_150[0];
V v_152 = args_150[1];
V v_153 = args_150[2];
V v_154 = args_150[3];
V v_155 = args_150[4];
var values_156 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat(R.Get(v_152, (V)"floor_id"), R.Concat((V)":", R.Concat(v_153, R.Concat((V)":", R.Call(v_7, R.Join(new V[] {R.Get(v_154, (V)"x")}, new Results(R.Get(v_154, (V)"y")))).First))))), new Entry((V)"kind", v_153), new Entry((V)"floor_id", R.Get(v_152, (V)"floor_id")), new Entry((V)"x", R.Get(v_154, (V)"x")), new Entry((V)"y", R.Get(v_154, (V)"y")))));
V v_157 = values_156[0];
foreach (var row_158 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(v_155, () => R.Table()))))))) {
V v_159 = row_158[0];
V v_160 = row_158[1];
var values_161 = R.Join(new V[] {}, new Results(v_160));
var obj_162 = v_157;
V key_163 = v_159;
R.Set(obj_162, key_163, values_161[0]);
}
var values_164 = R.Join(new V[] {}, new Results(v_157));
var obj_165 = R.Get(v_151, (V)"review_features");
V key_166 = (V)((V)R.Length(R.Get(v_151, (V)"review_features")) + (V)1.0d);
R.Set(obj_165, key_166, values_164[0]);
return R.Join(new V[] {}, new Results(v_157));
return Results.Empty;
}));
V v_167 = V.Nil;
v_167 = new V((RuleFunction)(args_168 => {
V v_169 = args_168[0];
V v_170 = args_168[1];
V v_171 = args_168[2];
var values_172 = R.Join(new V[] {V.Nil, V.Nil, V.Nil, R.Table()}, new Results(V.Nil));
V v_173 = values_172[0];
V v_174 = values_172[1];
V v_175 = values_172[2];
V v_176 = values_172[3];
V v_177 = values_172[4];
foreach (var row_178 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_169, (V)"review_features"))))))) {
V v_179 = row_178[0];
V v_180 = row_178[1];
if (R.Truth((V)(R.Get(v_180, (V)"quest_id") == v_170))) {
if (R.Truth((V)(R.Get(v_180, (V)"kind") == (V)"quest_clue"))) {
var values_181 = R.Join(new V[] {}, new Results(v_180));
v_173 = values_181[0];
} else if (R.Truth((V)(R.Get(v_180, (V)"kind") == (V)"quest_key"))) {
var values_182 = R.Join(new V[] {}, new Results(v_180));
v_174 = values_182[0];
} else if (R.Truth((V)(R.Get(v_180, (V)"kind") == (V)"quest_plate"))) {
var values_183 = R.Join(new V[] {}, new Results(v_180));
v_175 = values_183[0];
} else if (R.Truth((V)(R.Get(v_180, (V)"kind") == (V)"quest_brazier"))) {
var values_184 = R.Join(new V[] {}, new Results(v_180));
var obj_185 = v_176;
V key_186 = (V)((V)R.Length(v_176) + (V)1.0d);
R.Set(obj_185, key_186, values_184[0]);
} else if (R.Truth((V)(R.Get(v_180, (V)"kind") == (V)"quest_reward"))) {
var values_187 = R.Join(new V[] {}, new Results(v_180));
v_177 = values_187[0];
}
}
}
if (R.Truth((V)(!R.Truth(v_177)))) {
return Results.Empty;
}
var values_188 = R.Join(new V[] {}, new Results(R.Get(v_177, (V)"quest_kind")));
V v_189 = values_188[0];
var values_190 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.Or(R.And(R.And((V)(v_189 == (V)"spur"), () => v_173), () => R.Get(v_173, (V)"discovered")), () => R.And(R.And((V)(v_189 == (V)"braid"), () => v_174), () => R.Get(v_174, (V)"collected"))), () => R.And(R.And((V)(v_189 == (V)"switchback"), () => v_175), () => R.Get(v_175, (V)"activated"))), () => R.And(R.And(R.And((V)(v_189 == (V)"hub"), () => (V)((V)R.Length(v_176) == (V)2.0d)), () => R.Get(R.Get(v_176, (V)1.0d), (V)"lit")), () => R.Get(R.Get(v_176, (V)2.0d), (V)"lit"))), () => R.And(R.And((V)(v_189 == (V)"false_end"), () => v_173), () => R.Get(v_173, (V)"discovered")))));
V v_191 = values_190[0];
if (R.Truth(v_191)) {
foreach (var row_192 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_169, (V)"entities"))))))) {
V v_193 = row_192[0];
V v_194 = row_192[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_194, (V)"controller") == (V)"review_gate"), () => (V)(R.Get(v_194, (V)"quest_id") == v_170)), () => (V)(!R.Truth(R.Get(v_194, (V)"open")))))) {
var values_195 = R.Join(new V[] {}, new Results((V)true));
var obj_196 = v_194;
V key_197 = (V)"open";
R.Set(obj_196, key_197, values_195[0]);

var values_198 = R.Join(new V[] {}, new Results((V)false));
var obj_199 = v_194;
V key_200 = (V)"blocks_movement";
R.Set(obj_199, key_200, values_198[0]);
var values_201 = R.Join(new V[] {}, new Results((V)false));
var obj_202 = v_194;
V key_203 = (V)"blocks_vision";
R.Set(obj_202, key_203, values_201[0]);

var values_204 = R.Join(new V[] {}, new Results((V)false));
var obj_205 = v_194;
V key_206 = (V)"blocks_projectiles";
R.Set(obj_205, key_206, values_204[0]);
R.Call(v_171, R.Join(new V[] {(V)"SidequestGateOpened", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_194, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_194, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_194, (V)"position"), (V)"y")), new Entry((V)"quest_kind", v_189))}, new Results(R.Get(v_169, (V)"player_id"))));
}
}
}
var values_207 = R.Join(new V[] {}, new Results(R.And(v_191, () => R.Or((V)(v_189 != (V)"false_end"), () => R.Get(v_177, (V)"enemy_defeated")))));
V v_208 = values_207[0];
if (R.Truth(R.And(v_208, () => (V)(!R.Truth(R.Get(v_177, (V)"unlocked")))))) {
var values_209 = R.Join(new V[] {}, new Results((V)true));
var obj_210 = v_177;
V key_211 = (V)"unlocked";
R.Set(obj_210, key_211, values_209[0]);
R.Call(v_171, R.Join(new V[] {(V)"SidequestRewardReady", R.Table(new Entry((V)"floor_id", R.Get(v_177, (V)"floor_id")), new Entry((V)"x", R.Get(v_177, (V)"x")), new Entry((V)"y", R.Get(v_177, (V)"y")), new Entry((V)"quest_kind", v_189))}, new Results(R.Get(v_169, (V)"player_id"))));
}
return Results.Empty;
}));
R.Set(v_6, (V)"populate", new V((RuleFunction)(args_212 => {
V v_213 = args_212[0];
V v_214 = args_212[1];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_214)), () => (V)(!R.Truth(R.Get(v_214, (V)"rooms")))), () => (V)((V)R.Length(R.Get(v_214, (V)"rooms")) == (V)0.0d)))) {
return Results.Empty;
}
var values_215 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_216 = values_215[0];
V v_217 = values_215[1];
V v_218 = V.Nil;
v_218 = new V((RuleFunction)(args_219 => {
V v_220 = args_219[0];
V v_221 = args_219[1];
foreach (var row_222 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_53, R.Join(new V[] {v_214, R.Get(v_213, (V)"run_seed")}, new Results(v_220)))))))) {
V v_223 = row_222[0];
V v_224 = row_222[1];
if (R.Truth(R.And(R.Call(v_37, R.Join(new V[] {v_213, v_214, R.Get(v_224, (V)"x"), R.Get(v_224, (V)"y")}, new Results(v_216))).First, () => R.Or((V)(!R.Truth(v_221)), () => R.Call(v_221, R.Join(new V[] {}, new Results(v_224))).First)))) {
var values_225 = R.Join(new V[] {}, new Results((V)true));
var obj_226 = v_216;
V key_227 = R.Call(v_7, R.Join(new V[] {R.Get(v_224, (V)"x")}, new Results(R.Get(v_224, (V)"y")))).First;
R.Set(obj_226, key_227, values_225[0]);
return R.Join(new V[] {}, new Results(v_224));
}
}
return Results.Empty;
}));
var values_232 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {(V)"vault"}, new Results(new V((RuleFunction)(args_228 => {
V v_229 = args_228[0];
var values_230 = R.Join(new V[] {}, R.Call(v_29, R.Join(new V[] {v_214, R.Get(v_229, (V)"x")}, new Results(R.Get(v_229, (V)"y")))));
V v_231 = values_230[0];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_231, () => (V)(R.Get(v_231, (V)"role") == (V)"treasure")), () => (V)(R.Call(v_69, R.Join(new V[] {v_214, R.Get(v_229, (V)"x")}, new Results(R.Get(v_229, (V)"y")))).First >= (V)3.0d)), () => R.Call(v_80, R.Join(new V[] {v_214, R.Get(v_229, (V)"x"), R.Get(v_229, (V)"y"), v_217}, new Results((V)true))).First)));
return Results.Empty;
}))))));
V v_233 = values_232[0];
if (R.Truth((V)(!R.Truth(v_233)))) {
var values_238 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {(V)"vault_landmark"}, new Results(new V((RuleFunction)(args_234 => {
V v_235 = args_234[0];
var values_236 = R.Join(new V[] {}, R.Call(v_29, R.Join(new V[] {v_214, R.Get(v_235, (V)"x")}, new Results(R.Get(v_235, (V)"y")))));
V v_237 = values_236[0];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_237, () => (V)(R.Get(v_237, (V)"role") == (V)"landmark")), () => (V)(R.Call(v_69, R.Join(new V[] {v_214, R.Get(v_235, (V)"x")}, new Results(R.Get(v_235, (V)"y")))).First >= (V)3.0d)), () => R.Call(v_80, R.Join(new V[] {v_214, R.Get(v_235, (V)"x"), R.Get(v_235, (V)"y"), v_217}, new Results((V)true))).First)));
return Results.Empty;
}))))));
v_233 = values_238[0];
}
if (R.Truth(v_233)) {
R.Call(v_128, R.Join(new V[] {v_213, v_214, (V)"vault", v_233}, new Results(R.Table(new Entry((V)"opened", (V)false)))));
var values_241 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {(V)"key"}, new Results(new V((RuleFunction)(args_239 => {
V v_240 = args_239[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_11, R.Join(new V[] {v_240}, new Results(v_233))).First >= (V)8.0d), () => (V)(R.Call(v_69, R.Join(new V[] {v_214, R.Get(v_240, (V)"x")}, new Results(R.Get(v_240, (V)"y")))).First >= (V)2.0d))));
return Results.Empty;
}))))));
V v_242 = values_241[0];
if (R.Truth(v_242)) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"key", v_242}, new Results(R.Table(new Entry((V)"collected", (V)false)))));
var values_243 = R.Join(new V[] {}, new Results((V)true));
var obj_244 = v_217;
V key_245 = R.Call(v_7, R.Join(new V[] {R.Get(v_233, (V)"x")}, new Results(R.Get(v_233, (V)"y")))).First;
R.Set(obj_244, key_245, values_243[0]);
} else {
var values_246 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_213, (V)"entities"), (V)R.Length(R.Get(v_213, (V)"entities")))));
V v_247 = values_246[0];
if (R.Truth(R.And(v_247, () => (V)(R.Get(v_247, (V)"controller") == (V)"review_vault")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {}, new Results(R.Get(v_213, (V)"entities"))));

var values_248 = R.Join(new V[] {}, new Results(R.Get(v_247, (V)"id")));
var obj_249 = v_213;
V key_250 = (V)"next_entity_id";
R.Set(obj_249, key_250, values_248[0]);
}
var values_251 = R.Join(new V[] {}, new Results(V.Nil));
var obj_252 = v_216;
V key_253 = R.Call(v_7, R.Join(new V[] {R.Get(v_233, (V)"x")}, new Results(R.Get(v_233, (V)"y")))).First;
R.Set(obj_252, key_253, values_251[0]);
}
}
var values_256 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {(V)"gate"}, new Results(new V((RuleFunction)(args_254 => {
V v_255 = args_254[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_255, (V)"kind") == (V)"corridor"), () => (V)(R.Call(v_69, R.Join(new V[] {v_214, R.Get(v_255, (V)"x")}, new Results(R.Get(v_255, (V)"y")))).First >= (V)2.0d)), () => R.Call(v_80, R.Join(new V[] {v_214, R.Get(v_255, (V)"x"), R.Get(v_255, (V)"y"), v_217}, new Results((V)true))).First)));
return Results.Empty;
}))))));
V v_257 = values_256[0];
if (R.Truth(v_257)) {
var values_258 = Results.Empty;
V v_259 = values_258[0];
var values_260 = R.Join(new V[] {}, R.Call(v_80, R.Join(new V[] {v_214, R.Get(v_257, (V)"x"), R.Get(v_257, (V)"y")}, new Results(v_217))));
V v_261 = values_260[0];
V v_262 = values_260[1];
foreach (var row_263 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_16)))))) {
V v_264 = row_263[0];
V v_265 = row_263[1];
var values_266 = R.Join(new V[] {(V)(R.Get(v_257, (V)"x") + R.Get(v_265, (V)1.0d))}, new Results((V)(R.Get(v_257, (V)"y") + R.Get(v_265, (V)2.0d))));
V v_267 = values_266[0];
V v_268 = values_266[1];
if (R.Truth(R.And(R.Call(v_37, R.Join(new V[] {v_213, v_214, v_267, v_268}, new Results(v_216))).First, () => R.Get(v_262, R.Call(v_7, R.Join(new V[] {v_267}, new Results(v_268))).First)))) {
var values_269 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_267), new Entry((V)"y", v_268))));
v_259 = values_269[0];

break;
}
}
if (R.Truth(v_259)) {
var values_270 = R.Join(new V[] {}, R.Call(v_128, R.Join(new V[] {v_213, v_214, (V)"gate", v_257}, new Results(R.Table(new Entry((V)"open", (V)false), new Entry((V)"closes_at", V.Nil), new Entry((V)"art_variant", R.Or(R.And((V)(R.Get(v_214, (V)"biome_id") == (V)"core:biome/sunken_aqueduct"), () => (V)"rpg"), () => (V)"tiny")))))));
V v_271 = values_270[0];
var values_272 = R.Join(new V[] {}, new Results((V)true));
var obj_273 = v_217;
V key_274 = R.Call(v_7, R.Join(new V[] {R.Get(v_257, (V)"x")}, new Results(R.Get(v_257, (V)"y")))).First;
R.Set(obj_273, key_274, values_272[0]);
var values_275 = R.Join(new V[] {}, new Results((V)true));
var obj_276 = v_216;
V key_277 = R.Call(v_7, R.Join(new V[] {R.Get(v_259, (V)"x")}, new Results(R.Get(v_259, (V)"y")))).First;
R.Set(obj_276, key_277, values_275[0]);
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"plate", v_259}, new Results(R.Table(new Entry((V)"gate_id", R.Get(v_271, (V)"id"))))));
} else {
var values_278 = R.Join(new V[] {}, new Results(V.Nil));
var obj_279 = v_216;
V key_280 = R.Call(v_7, R.Join(new V[] {R.Get(v_257, (V)"x")}, new Results(R.Get(v_257, (V)"y")))).First;
R.Set(obj_279, key_280, values_278[0]);
}
}
var values_283 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {(V)"pillar"}, new Results(new V((RuleFunction)(args_281 => {
V v_282 = args_281[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_282, (V)"kind") == (V)"room"), () => (V)(R.Call(v_69, R.Join(new V[] {v_214, R.Get(v_282, (V)"x")}, new Results(R.Get(v_282, (V)"y")))).First == (V)4.0d)), () => R.Call(v_80, R.Join(new V[] {v_214, R.Get(v_282, (V)"x"), R.Get(v_282, (V)"y"), v_217}, new Results((V)true))).First)));
return Results.Empty;
}))))));
V v_284 = values_283[0];
if (R.Truth(v_284)) {
R.Call(v_128, R.Join(new V[] {v_213, v_214, (V)"pillar"}, new Results(v_284)));
var values_285 = R.Join(new V[] {}, new Results((V)true));
var obj_286 = v_217;
V key_287 = R.Call(v_7, R.Join(new V[] {R.Get(v_284, (V)"x")}, new Results(R.Get(v_284, (V)"y")))).First;
R.Set(obj_286, key_287, values_285[0]);
}
var values_290 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {(V)"ore"}, new Results(new V((RuleFunction)(args_288 => {
V v_289 = args_288[0];
return R.Join(new V[] {}, new Results((V)(R.Get(v_289, (V)"kind") == (V)"room")));
return Results.Empty;
}))))));
V v_291 = values_290[0];
if (R.Truth(v_291)) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"ore", v_291}, new Results(R.Table(new Entry((V)"broken", (V)false)))));
}
var values_294 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {(V)"brazier"}, new Results(new V((RuleFunction)(args_292 => {
V v_293 = args_292[0];
return R.Join(new V[] {}, new Results((V)(R.Get(v_293, (V)"kind") == (V)"room")));
return Results.Empty;
}))))));
V v_295 = values_294[0];
if (R.Truth(v_295)) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"brazier", v_295}, new Results(R.Table(new Entry((V)"lit_until", (V)0.0d)))));
}
var values_298 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {(V)"ward"}, new Results(new V((RuleFunction)(args_296 => {
V v_297 = args_296[0];
return R.Join(new V[] {}, new Results((V)(R.Get(v_297, (V)"kind") == (V)"room")));
return Results.Empty;
}))))));
V v_299 = values_298[0];
if (R.Truth(v_299)) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"ward", v_299}, new Results(R.Table(new Entry((V)"collected", (V)false)))));
}
var values_300 = R.Join(new V[] {}, new Results(R.Get(v_214, (V)"biome_id")));
V v_301 = values_300[0];
if (R.Truth((V)(v_301 == (V)"core:biome/overgrown_ruins"))) {
{ double end_304 = R.Num((V)2.0d), step_305 = R.Num((V)1.0d);
for (double i_303 = R.Num((V)1.0d); step_305 > 0 ? i_303 <= end_304 : i_303 >= end_304; i_303 += step_305) {
V v_302 = i_303;
var values_308 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {R.Concat((V)"overgrowth", v_302)}, new Results(new V((RuleFunction)(args_306 => {
V v_307 = args_306[0];
return R.Join(new V[] {}, new Results((V)(R.Get(v_307, (V)"kind") == (V)"room")));
return Results.Empty;
}))))));
V v_309 = values_308[0];
if (R.Truth(v_309)) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"overgrowth", v_309}, new Results(R.Table(new Entry((V)"variant", v_302)))));
}
}
}
} else if (R.Truth((V)(v_301 == (V)"core:biome/fungal_hollows"))) {
{ double end_312 = R.Num((V)2.0d), step_313 = R.Num((V)1.0d);
for (double i_311 = R.Num((V)1.0d); step_313 > 0 ? i_311 <= end_312 : i_311 >= end_312; i_311 += step_313) {
V v_310 = i_311;
var values_316 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {R.Concat((V)"mushroom", v_310)}, new Results(new V((RuleFunction)(args_314 => {
V v_315 = args_314[0];
return R.Join(new V[] {}, new Results((V)(R.Get(v_315, (V)"kind") == (V)"room")));
return Results.Empty;
}))))));
V v_317 = values_316[0];
if (R.Truth(v_317)) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"mushroom", v_317}, new Results(R.Table(new Entry((V)"variant", v_310)))));
}
}
}
} else if (R.Truth(R.Or((V)(v_301 == (V)"core:biome/ashen_bastion"), () => (V)(v_301 == (V)"core:biome/sundered_forge")))) {
{ double end_320 = R.Num((V)2.0d), step_321 = R.Num((V)1.0d);
for (double i_319 = R.Num((V)1.0d); step_321 > 0 ? i_319 <= end_320 : i_319 >= end_320; i_319 += step_321) {
V v_318 = i_319;
var values_324 = R.Join(new V[] {}, R.Call(v_218, R.Join(new V[] {R.Concat((V)"mine_rail", v_318)}, new Results(new V((RuleFunction)(args_322 => {
V v_323 = args_322[0];
return R.Join(new V[] {}, new Results((V)(R.Get(v_323, (V)"kind") == (V)"corridor")));
return Results.Empty;
}))))));
V v_325 = values_324[0];
if (R.Truth(v_325)) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"mine_rail", v_325}, new Results(R.Table(new Entry((V)"variant", v_318)))));
}
}
}
}
foreach (var row_326 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_214, (V)"rooms"))))))) {
V v_327 = row_326[0];
V v_328 = row_326[1];
foreach (var row_329 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_328, (V)"structures"), () => R.Table()))))))) {
V v_330 = row_329[0];
V v_331 = row_329[1];
var values_332 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"derive"), R.Join(new V[] {R.Get(v_213, (V)"run_seed")}, new Results(R.Concat(R.Get(v_214, (V)"floor_id"), R.Concat((V)":structure:", R.Concat(R.Get(v_328, (V)"id"), R.Concat((V)":", v_330))))))).First % (V)100.0d)));
V v_333 = values_332[0];
if (R.Truth(R.And((V)(v_333 < (V)42.0d), () => R.Call(v_37, R.Join(new V[] {v_213, v_214, R.Get(v_331, (V)"x"), R.Get(v_331, (V)"y")}, new Results(v_216))).First))) {
if (R.Truth(R.Or((V)(R.Get(v_331, (V)"kind") != (V)"pillar"), () => R.Call(v_80, R.Join(new V[] {v_214, R.Get(v_331, (V)"x"), R.Get(v_331, (V)"y"), v_217}, new Results((V)true))).First))) {
var values_334 = R.Join(new V[] {}, new Results((V)true));
var obj_335 = v_216;
V key_336 = R.Call(v_7, R.Join(new V[] {R.Get(v_331, (V)"x")}, new Results(R.Get(v_331, (V)"y")))).First;
R.Set(obj_335, key_336, values_334[0]);
if (R.Truth((V)(R.Get(v_331, (V)"kind") == (V)"pillar"))) {
R.Call(v_128, R.Join(new V[] {v_213, v_214, (V)"pillar"}, new Results(v_331)));
var values_337 = R.Join(new V[] {}, new Results((V)true));
var obj_338 = v_217;
V key_339 = R.Call(v_7, R.Join(new V[] {R.Get(v_331, (V)"x")}, new Results(R.Get(v_331, (V)"y")))).First;
R.Set(obj_338, key_339, values_337[0]);
} else if (R.Truth((V)(R.Get(v_331, (V)"kind") == (V)"ward"))) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"ward", v_331}, new Results(R.Table(new Entry((V)"collected", (V)false)))));
} else if (R.Truth((V)(R.Get(v_331, (V)"kind") == (V)"brazier"))) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"brazier", v_331}, new Results(R.Table(new Entry((V)"lit_until", (V)0.0d)))));
} else if (R.Truth((V)(R.Get(v_331, (V)"kind") == (V)"mushroom"))) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"mushroom", v_331}, new Results(R.Table(new Entry((V)"variant", (V)((V)(v_330 % (V)2.0d) + (V)1.0d))))));
} else if (R.Truth((V)(R.Get(v_331, (V)"kind") == (V)"crystal"))) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"crystal", v_331}, new Results(R.Table(new Entry((V)"variant", (V)1.0d)))));
}
}
}
}
}
foreach (var row_340 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_214, (V)"corridor_quests"), () => R.Table()))))))) {
V v_341 = row_340[0];
V v_342 = row_340[1];
var values_343 = Results.Empty;
V v_344 = values_343[0];
var values_345 = Results.Empty;
V v_346 = values_345[0];
foreach (var row_347 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_342, (V)"nodes"))))))) {
V v_348 = row_347[0];
V v_349 = row_347[1];
if (R.Truth((V)(R.Get(v_349, (V)"kind") == (V)"gate"))) {
R.Call(v_128, R.Join(new V[] {v_213, v_214, (V)"gate", v_349}, new Results(R.Table(new Entry((V)"open", (V)false), new Entry((V)"closes_at", V.Nil), new Entry((V)"quest_id", R.Get(v_342, (V)"id")), new Entry((V)"art_variant", R.Or(R.And((V)(R.Get(v_342, (V)"kind") == (V)"false_end"), () => (V)"hidden"), () => (V)"tiny"))))));
} else if (R.Truth((V)(R.Get(v_349, (V)"kind") == (V)"clue"))) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"quest_clue", v_349}, new Results(R.Table(new Entry((V)"quest_id", R.Get(v_342, (V)"id")), new Entry((V)"discovered", (V)false)))));
} else if (R.Truth((V)(R.Get(v_349, (V)"kind") == (V)"key"))) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"quest_key", v_349}, new Results(R.Table(new Entry((V)"quest_id", R.Get(v_342, (V)"id")), new Entry((V)"collected", (V)false)))));
} else if (R.Truth((V)(R.Get(v_349, (V)"kind") == (V)"plate"))) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"quest_plate", v_349}, new Results(R.Table(new Entry((V)"quest_id", R.Get(v_342, (V)"id")), new Entry((V)"activated", (V)false)))));
} else if (R.Truth((V)(R.Get(v_349, (V)"kind") == (V)"brazier"))) {
R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"quest_brazier", v_349}, new Results(R.Table(new Entry((V)"quest_id", R.Get(v_342, (V)"id")), new Entry((V)"lit", (V)false)))));
} else if (R.Truth((V)(R.Get(v_349, (V)"kind") == (V)"reward"))) {
var values_350 = R.Join(new V[] {}, R.Call(v_149, R.Join(new V[] {v_213, v_214, (V)"quest_reward", v_349}, new Results(R.Table(new Entry((V)"quest_id", R.Get(v_342, (V)"id")), new Entry((V)"quest_kind", R.Get(v_342, (V)"kind")), new Entry((V)"unlocked", (V)false), new Entry((V)"collected", (V)false), new Entry((V)"enemy_defeated", (V)(R.Get(v_342, (V)"kind") != (V)"false_end")))))));
v_344 = values_350[0];
} else if (R.Truth((V)(R.Get(v_349, (V)"kind") == (V)"enemy"))) {
var values_351 = R.Join(new V[] {}, new Results(v_349));
v_346 = values_351[0];
}
}
if (R.Truth(R.And(v_344, () => v_346))) {
foreach (var row_352 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_213, (V)"entities"))))))) {
V v_353 = row_352[0];
V v_354 = row_352[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_354, (V)"controller") == (V)"ai"), () => (V)(R.Get(R.Get(v_354, (V)"position"), (V)"floor_id") == R.Get(v_214, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_354, (V)"position"), (V)"x") == R.Get(v_346, (V)"x"))), () => (V)(R.Get(R.Get(v_354, (V)"position"), (V)"y") == R.Get(v_346, (V)"y"))))) {
var values_355 = R.Join(new V[] {}, new Results(R.Get(v_354, (V)"id")));
var obj_356 = v_344;
V key_357 = (V)"target_id";
R.Set(obj_356, key_357, values_355[0]);

break;
}
}
if (R.Truth((V)(!R.Truth(R.Get(v_344, (V)"target_id"))))) {
var values_358 = R.Join(new V[] {}, new Results((V)true));
var obj_359 = v_344;
V key_360 = (V)"enemy_defeated";
R.Set(obj_359, key_360, values_358[0]);
}
}
}
return Results.Empty;
})));
R.Set(v_6, (V)"on_event", new V((RuleFunction)(args_361 => {
V v_362 = args_361[0];
V v_363 = args_361[1];
V v_364 = args_361[2];
V v_365 = args_361[3];
var values_366 = R.Join(new V[] {}, new Results(R.Get(v_363, (V)"floor_id")));
V v_367 = values_366[0];
if (R.Truth((V)(!R.Truth(v_367)))) {
return Results.Empty;
}
foreach (var row_368 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_362, (V)"review_features"), () => R.Table()))))))) {
V v_369 = row_368[0];
V v_370 = row_368[1];
if (R.Truth((V)(R.Get(v_370, (V)"floor_id") == v_367))) {
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_370, (V)"kind") == (V)"key"), () => (V)(!R.Truth(R.Get(v_370, (V)"collected")))), () => (V)(R.Get(v_363, (V)"kind") == (V)"TileEntered")), () => (V)(R.Get(v_363, (V)"source_id") == R.Get(v_362, (V)"player_id"))), () => (V)(R.Get(v_370, (V)"x") == R.Get(v_363, (V)"x"))), () => (V)(R.Get(v_370, (V)"y") == R.Get(v_363, (V)"y"))))) {
var values_371 = R.Join(new V[] {}, new Results((V)true));
var obj_372 = v_370;
V key_373 = (V)"collected";
R.Set(obj_372, key_373, values_371[0]);
var values_374 = R.Join(new V[] {}, new Results((V)true));
var obj_375 = R.Get(v_362, (V)"review_keys");
V key_376 = v_367;
R.Set(obj_375, key_376, values_374[0]);
R.Call(v_364, R.Join(new V[] {(V)"VaultKeyCollected", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(v_370, (V)"x")), new Entry((V)"y", R.Get(v_370, (V)"y")))}, new Results(R.Get(v_362, (V)"player_id"))));
} else if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_370, (V)"kind") == (V)"ward"), () => (V)(!R.Truth(R.Get(v_370, (V)"collected")))), () => (V)(R.Get(v_363, (V)"kind") == (V)"TileEntered")), () => (V)(R.Get(v_363, (V)"source_id") == R.Get(v_362, (V)"player_id"))), () => (V)(R.Get(v_370, (V)"x") == R.Get(v_363, (V)"x"))), () => (V)(R.Get(v_370, (V)"y") == R.Get(v_363, (V)"y"))))) {
var values_377 = R.Join(new V[] {}, new Results((V)true));
var obj_378 = v_370;
V key_379 = (V)"collected";
R.Set(obj_378, key_379, values_377[0]);
foreach (var row_380 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_362, (V)"entities"))))))) {
V v_381 = row_380[0];
V v_382 = row_380[1];
if (R.Truth((V)(R.Get(v_382, (V)"id") == R.Get(v_362, (V)"player_id")))) {
var values_383 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_384 = v_382;
V key_385 = (V)"pickup_ward_actions";
R.Set(obj_384, key_385, values_383[0]);

break;
}
}
R.Call(v_364, R.Join(new V[] {(V)"WardFlaskCollected", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(v_370, (V)"x")), new Entry((V)"y", R.Get(v_370, (V)"y")))}, new Results(R.Get(v_362, (V)"player_id"))));
} else if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_370, (V)"kind") == (V)"brazier"), () => (V)(R.Get(v_363, (V)"kind") == (V)"TileEntered")), () => (V)(R.Get(v_363, (V)"source_id") == R.Get(v_362, (V)"player_id"))), () => (V)(R.Get(v_370, (V)"x") == R.Get(v_363, (V)"x"))), () => (V)(R.Get(v_370, (V)"y") == R.Get(v_363, (V)"y"))))) {
var values_386 = R.Join(new V[] {}, new Results((V)(R.Get(v_362, (V)"clock") + (V)300.0d)));
var obj_387 = v_370;
V key_388 = (V)"lit_until";
R.Set(obj_387, key_388, values_386[0]);
R.Call(v_364, R.Join(new V[] {(V)"BrazierLit", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(v_370, (V)"x")), new Entry((V)"y", R.Get(v_370, (V)"y")), new Entry((V)"due", R.Get(v_370, (V)"lit_until")))}, new Results(R.Get(v_362, (V)"player_id"))));
} else if (R.Truth(R.And(R.And((V)(R.Get(v_370, (V)"kind") == (V)"ore"), () => (V)(!R.Truth(R.Get(v_370, (V)"broken")))), () => (V)(R.Get(v_363, (V)"kind") == (V)"AttackPerformed")))) {
foreach (var row_389 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_363, (V)"target_cells"), () => R.Table()))))))) {
V v_390 = row_389[0];
V v_391 = row_389[1];
if (R.Truth(R.And((V)(R.Get(v_391, (V)"x") == R.Get(v_370, (V)"x")), () => (V)(R.Get(v_391, (V)"y") == R.Get(v_370, (V)"y"))))) {
var values_392 = R.Join(new V[] {}, new Results((V)true));
var obj_393 = v_370;
V key_394 = (V)"broken";
R.Set(obj_393, key_394, values_392[0]);
R.Call(v_364, R.Join(new V[] {(V)"OreBroken", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(v_370, (V)"x")), new Entry((V)"y", R.Get(v_370, (V)"y")))}, new Results(R.Get(v_363, (V)"source_id"))));
R.Call(v_365, R.Join(new V[] {(V)3.0d, v_367, R.Get(v_370, (V)"x"), R.Get(v_370, (V)"y")}, new Results(R.Get(v_363, (V)"source_id"))));
break;
}
}
} else if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_370, (V)"kind") == (V)"plate"), () => (V)(R.Get(v_363, (V)"kind") == (V)"TileEntered")), () => (V)(R.Get(v_370, (V)"x") == R.Get(v_363, (V)"x"))), () => (V)(R.Get(v_370, (V)"y") == R.Get(v_363, (V)"y"))))) {
foreach (var row_395 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_362, (V)"entities"))))))) {
V v_396 = row_395[0];
V v_397 = row_395[1];
if (R.Truth(R.And((V)(R.Get(v_397, (V)"id") == R.Get(v_370, (V)"gate_id")), () => (V)(R.Get(v_397, (V)"controller") == (V)"review_gate")))) {
var values_398 = R.Join(new V[] {}, new Results((V)true));
var obj_399 = v_397;
V key_400 = (V)"open";
R.Set(obj_399, key_400, values_398[0]);

var values_401 = R.Join(new V[] {}, new Results((V)false));
var obj_402 = v_397;
V key_403 = (V)"blocks_movement";
R.Set(obj_402, key_403, values_401[0]);
var values_404 = R.Join(new V[] {}, new Results((V)false));
var obj_405 = v_397;
V key_406 = (V)"blocks_vision";
R.Set(obj_405, key_406, values_404[0]);

var values_407 = R.Join(new V[] {}, new Results((V)false));
var obj_408 = v_397;
V key_409 = (V)"blocks_projectiles";
R.Set(obj_408, key_409, values_407[0]);
var values_410 = R.Join(new V[] {}, new Results((V)(R.Get(v_362, (V)"clock") + (V)300.0d)));
var obj_411 = v_397;
V key_412 = (V)"closes_at";
R.Set(obj_411, key_412, values_410[0]);
R.Call(v_364, R.Join(new V[] {(V)"TimedGateOpened", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(R.Get(v_397, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_397, (V)"position"), (V)"y")), new Entry((V)"due", R.Get(v_397, (V)"closes_at")))}, new Results(R.Get(v_363, (V)"source_id"))));
break;
}
}
} else if (R.Truth(R.And(R.And(R.And(R.And(R.Get(v_370, (V)"quest_id"), () => (V)(R.Get(v_363, (V)"kind") == (V)"TileEntered")), () => (V)(R.Get(v_363, (V)"source_id") == R.Get(v_362, (V)"player_id"))), () => (V)(R.Get(v_370, (V)"x") == R.Get(v_363, (V)"x"))), () => (V)(R.Get(v_370, (V)"y") == R.Get(v_363, (V)"y"))))) {
var values_413 = R.Join(new V[] {}, new Results((V)false));
V v_414 = values_413[0];
if (R.Truth(R.And((V)(R.Get(v_370, (V)"kind") == (V)"quest_clue"), () => (V)(!R.Truth(R.Get(v_370, (V)"discovered")))))) {
var values_415 = R.Join(new V[] {}, new Results((V)true));
var obj_416 = v_370;
V key_417 = (V)"discovered";
R.Set(obj_416, key_417, values_415[0]);

var values_418 = R.Join(new V[] {}, new Results((V)true));
v_414 = values_418[0];
R.Call(v_364, R.Join(new V[] {(V)"SidequestClueFound", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(v_370, (V)"x")), new Entry((V)"y", R.Get(v_370, (V)"y")), new Entry((V)"quest_id", R.Get(v_370, (V)"quest_id")))}, new Results(R.Get(v_362, (V)"player_id"))));
} else if (R.Truth(R.And((V)(R.Get(v_370, (V)"kind") == (V)"quest_key"), () => (V)(!R.Truth(R.Get(v_370, (V)"collected")))))) {
var values_419 = R.Join(new V[] {}, new Results((V)true));
var obj_420 = v_370;
V key_421 = (V)"collected";
R.Set(obj_420, key_421, values_419[0]);

var values_422 = R.Join(new V[] {}, new Results((V)true));
v_414 = values_422[0];
R.Call(v_364, R.Join(new V[] {(V)"SidequestKeyFound", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(v_370, (V)"x")), new Entry((V)"y", R.Get(v_370, (V)"y")), new Entry((V)"quest_id", R.Get(v_370, (V)"quest_id")))}, new Results(R.Get(v_362, (V)"player_id"))));
} else if (R.Truth(R.And((V)(R.Get(v_370, (V)"kind") == (V)"quest_plate"), () => (V)(!R.Truth(R.Get(v_370, (V)"activated")))))) {
var values_423 = R.Join(new V[] {}, new Results((V)true));
var obj_424 = v_370;
V key_425 = (V)"activated";
R.Set(obj_424, key_425, values_423[0]);

var values_426 = R.Join(new V[] {}, new Results((V)true));
v_414 = values_426[0];
R.Call(v_364, R.Join(new V[] {(V)"SidequestPlatePressed", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(v_370, (V)"x")), new Entry((V)"y", R.Get(v_370, (V)"y")), new Entry((V)"quest_id", R.Get(v_370, (V)"quest_id")))}, new Results(R.Get(v_362, (V)"player_id"))));
} else if (R.Truth(R.And((V)(R.Get(v_370, (V)"kind") == (V)"quest_brazier"), () => (V)(!R.Truth(R.Get(v_370, (V)"lit")))))) {
var values_427 = R.Join(new V[] {}, new Results((V)true));
var obj_428 = v_370;
V key_429 = (V)"lit";
R.Set(obj_428, key_429, values_427[0]);

var values_430 = R.Join(new V[] {}, new Results((V)true));
v_414 = values_430[0];
R.Call(v_364, R.Join(new V[] {(V)"SidequestBrazierLit", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(v_370, (V)"x")), new Entry((V)"y", R.Get(v_370, (V)"y")), new Entry((V)"quest_id", R.Get(v_370, (V)"quest_id")))}, new Results(R.Get(v_362, (V)"player_id"))));
} else if (R.Truth(R.And(R.And((V)(R.Get(v_370, (V)"kind") == (V)"quest_reward"), () => R.Get(v_370, (V)"unlocked")), () => (V)(!R.Truth(R.Get(v_370, (V)"collected")))))) {
var values_431 = R.Join(new V[] {}, new Results((V)true));
var obj_432 = v_370;
V key_433 = (V)"collected";
R.Set(obj_432, key_433, values_431[0]);
var values_434 = R.Join(new V[] {}, new Results((V)5.0d));
V v_435 = values_434[0];
var values_436 = R.Join(new V[] {}, new Results(R.Get(v_362, (V)"currency")));
V v_437 = values_436[0];
var values_438 = R.Join(new V[] {}, new Results((V)(v_437 + v_435)));
var obj_439 = v_362;
V key_440 = (V)"currency";
R.Set(obj_439, key_440, values_438[0]);
R.Call(v_364, R.Join(new V[] {(V)"SidequestCompleted", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(v_370, (V)"x")), new Entry((V)"y", R.Get(v_370, (V)"y")), new Entry((V)"quest_id", R.Get(v_370, (V)"quest_id")), new Entry((V)"quest_kind", R.Get(v_370, (V)"quest_kind")), new Entry((V)"amount", v_435))}, new Results(R.Get(v_362, (V)"player_id"))));
R.Call(v_364, R.Join(new V[] {(V)"CurrencyCollected", R.Table(new Entry((V)"floor_id", v_367), new Entry((V)"x", R.Get(v_370, (V)"x")), new Entry((V)"y", R.Get(v_370, (V)"y")), new Entry((V)"amount", v_435), new Entry((V)"old_currency", v_437), new Entry((V)"new_currency", R.Get(v_362, (V)"currency")))}, new Results(R.Get(v_362, (V)"player_id"))));
}
if (R.Truth(v_414)) {
R.Call(v_167, R.Join(new V[] {v_362, R.Get(v_370, (V)"quest_id")}, new Results(v_364)));
}
}
}
}
if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"EntityKilled"))) {
foreach (var row_441 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_362, (V)"review_features"), () => R.Table()))))))) {
V v_442 = row_441[0];
V v_443 = row_441[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_443, (V)"kind") == (V)"quest_reward"), () => (V)(R.Get(v_443, (V)"target_id") == R.Get(v_363, (V)"target_id"))), () => (V)(!R.Truth(R.Get(v_443, (V)"enemy_defeated")))))) {
var values_444 = R.Join(new V[] {}, new Results((V)true));
var obj_445 = v_443;
V key_446 = (V)"enemy_defeated";
R.Set(obj_445, key_446, values_444[0]);
R.Call(v_167, R.Join(new V[] {v_362, R.Get(v_443, (V)"quest_id")}, new Results(v_364)));
}
}
}
return Results.Empty;
})));
R.Set(v_6, (V)"after_time", new V((RuleFunction)(args_447 => {
V v_448 = args_447[0];
V v_449 = args_447[1];
foreach (var row_450 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_448, (V)"entities"))))))) {
V v_451 = row_450[0];
V v_452 = row_450[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_452, (V)"controller") == (V)"review_gate"), () => R.Get(v_452, (V)"open")), () => R.Get(v_452, (V)"closes_at")), () => (V)(R.Get(v_448, (V)"clock") >= R.Get(v_452, (V)"closes_at"))), () => (V)(!R.Truth(R.Call(v_17, R.Join(new V[] {v_448, R.Get(R.Get(v_452, (V)"position"), (V)"floor_id"), R.Get(R.Get(v_452, (V)"position"), (V)"x"), R.Get(R.Get(v_452, (V)"position"), (V)"y")}, new Results(R.Get(v_452, (V)"id")))).First))))) {
var values_453 = R.Join(new V[] {}, new Results((V)false));
var obj_454 = v_452;
V key_455 = (V)"open";
R.Set(obj_454, key_455, values_453[0]);

var values_456 = R.Join(new V[] {}, new Results((V)true));
var obj_457 = v_452;
V key_458 = (V)"blocks_movement";
R.Set(obj_457, key_458, values_456[0]);

var values_459 = R.Join(new V[] {}, new Results((V)true));
var obj_460 = v_452;
V key_461 = (V)"blocks_vision";
R.Set(obj_460, key_461, values_459[0]);
var values_462 = R.Join(new V[] {}, new Results((V)true));
var obj_463 = v_452;
V key_464 = (V)"blocks_projectiles";
R.Set(obj_463, key_464, values_462[0]);

var values_465 = R.Join(new V[] {}, new Results(V.Nil));
var obj_466 = v_452;
V key_467 = (V)"closes_at";
R.Set(obj_466, key_467, values_465[0]);
R.Call(v_449, R.Join(new V[] {(V)"TimedGateClosed", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_452, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_452, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_452, (V)"position"), (V)"y")))}, new Results(R.Get(v_452, (V)"id"))));
}
}
return Results.Empty;
})));
R.Set(v_6, (V)"mist_cleared", new V((RuleFunction)(args_468 => {
V v_469 = args_468[0];
V v_470 = args_468[1];
V v_471 = args_468[2];
V v_472 = args_468[3];
foreach (var row_473 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_469, (V)"review_features"), () => R.Table()))))))) {
V v_474 = row_473[0];
V v_475 = row_473[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_475, (V)"kind") == (V)"brazier"), () => (V)(R.Get(v_475, (V)"floor_id") == R.Get(v_470, (V)"floor_id"))), () => (V)(R.Or(R.Get(v_475, (V)"lit_until"), () => (V)0.0d) > R.Get(v_469, (V)"clock"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_475, (V)"x") - v_471)))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_475, (V)"y") - v_472)))).First) <= (V)2.0d)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_6, (V)"validate", new V((RuleFunction)(args_476 => {
V v_477 = args_476[0];
V v_478 = args_476[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"review_features")))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"review_keys")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"review_clues")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"class_special")))).First == (V)"table"))}, new Results((V)"invalid review state")));
var values_479 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"key", (V)true), new Entry((V)"ward", (V)true), new Entry((V)"ore", (V)true), new Entry((V)"brazier", (V)true), new Entry((V)"plate", (V)true), new Entry((V)"mushroom", (V)true), new Entry((V)"overgrowth", (V)true), new Entry((V)"mine_rail", (V)true), new Entry((V)"crystal", (V)true), new Entry((V)"quest_clue", (V)true), new Entry((V)"quest_key", (V)true), new Entry((V)"quest_plate", (V)true), new Entry((V)"quest_brazier", (V)true), new Entry((V)"quest_reward", (V)true))));
V v_480 = values_479[0];
var values_481 = R.Join(new V[] {R.Table(), R.Table()}, new Results(R.Table()));
V v_482 = values_481[0];
V v_483 = values_481[1];
V v_484 = values_481[2];
V v_485 = V.Nil;
v_485 = new V((RuleFunction)(args_486 => {
V v_487 = args_486[0];
V v_488 = args_486[1];
V v_489 = args_486[2];
var values_490 = R.Join(new V[] {}, new Results(R.Get(v_478, v_487)));
V v_491 = values_490[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(v_491, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_488))).First == (V)"number")), () => (V)((V)(v_488 % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_489))).First == (V)"number")), () => (V)((V)(v_489 % (V)1.0d) == (V)0.0d)), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_491, v_488}, new Results(v_489))).First)}, new Results((V)"review object outside floor")));
return Results.Empty;
}));
foreach (var row_492 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"review_features"))))))) {
V v_493 = row_492[0];
V v_494 = row_492[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_494))).First == (V)"table"), () => R.Get(v_480, R.Get(v_494, (V)"kind"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"id")))).First == (V)"string")), () => (V)(!R.Truth(R.Get(v_482, R.Get(v_494, (V)"id")))))}, new Results((V)"invalid review feature")));
var values_495 = R.Join(new V[] {}, new Results((V)true));
var obj_496 = v_482;
V key_497 = R.Get(v_494, (V)"id");
R.Set(obj_496, key_497, values_495[0]);
R.Call(v_485, R.Join(new V[] {R.Get(v_494, (V)"floor_id"), R.Get(v_494, (V)"x")}, new Results(R.Get(v_494, (V)"y"))));
if (R.Truth(R.Get(v_494, (V)"quest_id"))) {
var values_498 = R.Join(new V[] {}, new Results(R.Get(v_478, R.Get(v_494, (V)"floor_id"))));
V v_499 = values_498[0];
var values_500 = R.Join(new V[] {}, new Results((V)false));
V v_501 = values_500[0];
foreach (var row_502 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_499, (V)"corridor_quests"), () => R.Table()))))))) {
V v_503 = row_502[0];
V v_504 = row_502[1];
if (R.Truth((V)(R.Get(v_504, (V)"id") == R.Get(v_494, (V)"quest_id")))) {
foreach (var row_505 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_504, (V)"nodes"))))))) {
V v_506 = row_505[0];
V v_507 = row_505[1];
var values_508 = R.Join(new V[] {}, new Results(R.Concat((V)"quest_", R.Get(v_507, (V)"kind"))));
V v_509 = values_508[0];
if (R.Truth((V)(R.Get(v_507, (V)"kind") == (V)"reward"))) {
var values_510 = R.Join(new V[] {}, new Results((V)"quest_reward"));
v_509 = values_510[0];
}
if (R.Truth(R.And(R.And((V)(R.Get(v_507, (V)"x") == R.Get(v_494, (V)"x")), () => (V)(R.Get(v_507, (V)"y") == R.Get(v_494, (V)"y"))), () => (V)(R.Get(v_494, (V)"kind") == v_509)))) {
var values_511 = R.Join(new V[] {}, new Results((V)true));
v_501 = values_511[0];
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_501}, new Results((V)"quest feature lacks matching corridor node")));
if (R.Truth((V)(R.Get(v_494, (V)"kind") == (V)"quest_clue"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"discovered")))).First == (V)"boolean")}, new Results((V)"invalid quest clue")));
} else if (R.Truth((V)(R.Get(v_494, (V)"kind") == (V)"quest_key"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"collected")))).First == (V)"boolean")}, new Results((V)"invalid quest key")));
} else if (R.Truth((V)(R.Get(v_494, (V)"kind") == (V)"quest_plate"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"activated")))).First == (V)"boolean")}, new Results((V)"invalid quest plate")));
} else if (R.Truth((V)(R.Get(v_494, (V)"kind") == (V)"quest_brazier"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"lit")))).First == (V)"boolean")}, new Results((V)"invalid quest brazier")));
} else if (R.Truth((V)(R.Get(v_494, (V)"kind") == (V)"quest_reward"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"unlocked")))).First == (V)"boolean"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"collected")))).First == (V)"boolean")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"enemy_defeated")))).First == (V)"boolean")), () => (V)(!R.Truth(R.And(R.Get(v_494, (V)"collected"), () => (V)(!R.Truth(R.Get(v_494, (V)"unlocked")))))))}, new Results((V)"invalid quest reward")));
}
} else if (R.Truth((V)(R.Get(v_494, (V)"kind") == (V)"key"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"collected")))).First == (V)"boolean")}, new Results((V)"invalid vault key state")));
var values_512 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_483, R.Get(v_494, (V)"floor_id")), () => (V)0.0d) + (V)1.0d)));
var obj_513 = v_483;
V key_514 = R.Get(v_494, (V)"floor_id");
R.Set(obj_513, key_514, values_512[0]);
} else if (R.Truth((V)(R.Get(v_494, (V)"kind") == (V)"ward"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"collected")))).First == (V)"boolean")}, new Results((V)"invalid ward flask state")));
} else if (R.Truth((V)(R.Get(v_494, (V)"kind") == (V)"ore"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"broken")))).First == (V)"boolean")}, new Results((V)"invalid ore state")));
} else if (R.Truth((V)(R.Get(v_494, (V)"kind") == (V)"brazier"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_494, (V)"lit_until")))).First == (V)"number"), () => (V)(R.Get(v_494, (V)"lit_until") >= (V)0.0d))}, new Results((V)"invalid brazier state")));
} else if (R.Truth((V)(R.Get(v_494, (V)"kind") == (V)"plate"))) {
var values_515 = Results.Empty;
V v_516 = values_515[0];
foreach (var row_517 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"entities"))))))) {
V v_518 = row_517[0];
V v_519 = row_517[1];
if (R.Truth((V)(R.Get(v_519, (V)"id") == R.Get(v_494, (V)"gate_id")))) {
var values_520 = R.Join(new V[] {}, new Results(v_519));
v_516 = values_520[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(v_516, () => (V)(R.Get(v_516, (V)"controller") == (V)"review_gate")), () => (V)(R.Get(R.Get(v_516, (V)"position"), (V)"floor_id") == R.Get(v_494, (V)"floor_id")))}, new Results((V)"plate without gate")));
var values_521 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_484, R.Get(v_494, (V)"gate_id")), () => (V)0.0d) + (V)1.0d)));
var obj_522 = v_484;
V key_523 = R.Get(v_494, (V)"gate_id");
R.Set(obj_522, key_523, values_521[0]);
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_494, (V)"variant") == (V)1.0d), () => (V)(R.Get(v_494, (V)"variant") == (V)2.0d))}, new Results((V)"invalid scenery variant")));
}
}
foreach (var row_524 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_483)))))) {
V v_525 = row_524[0];
V v_526 = row_524[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_526 == (V)1.0d)}, new Results((V)"duplicate vault key")));
var values_527 = R.Join(new V[] {}, new Results((V)false));
V v_528 = values_527[0];
foreach (var row_529 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"entities"))))))) {
V v_530 = row_529[0];
V v_531 = row_529[1];
if (R.Truth(R.And((V)(R.Get(v_531, (V)"controller") == (V)"review_vault"), () => (V)(R.Get(R.Get(v_531, (V)"position"), (V)"floor_id") == v_525)))) {
var values_532 = R.Join(new V[] {}, new Results((V)true));
v_528 = values_532[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_528}, new Results((V)"vault key without vault")));
}
foreach (var row_533 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"entities"))))))) {
V v_534 = row_533[0];
V v_535 = row_533[1];
if (R.Truth((V)(R.Get(v_535, (V)"controller") == (V)"review_gate"))) {
if (R.Truth(R.Get(v_535, (V)"quest_id"))) {
var values_536 = R.Join(new V[] {}, new Results(R.Get(v_478, R.Get(R.Get(v_535, (V)"position"), (V)"floor_id"))));
V v_537 = values_536[0];
var values_538 = R.Join(new V[] {}, new Results((V)false));
V v_539 = values_538[0];
foreach (var row_540 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_537, (V)"corridor_quests"), () => R.Table()))))))) {
V v_541 = row_540[0];
V v_542 = row_540[1];
if (R.Truth((V)(R.Get(v_542, (V)"id") == R.Get(v_535, (V)"quest_id")))) {
foreach (var row_543 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_542, (V)"nodes"))))))) {
V v_544 = row_543[0];
V v_545 = row_543[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_545, (V)"kind") == (V)"gate"), () => (V)(R.Get(v_545, (V)"x") == R.Get(R.Get(v_535, (V)"position"), (V)"x"))), () => (V)(R.Get(v_545, (V)"y") == R.Get(R.Get(v_535, (V)"position"), (V)"y"))))) {
var values_546 = R.Join(new V[] {}, new Results((V)true));
v_539 = values_546[0];
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_539}, new Results((V)"quest gate lacks matching corridor node")));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_484, R.Get(v_535, (V)"id")) == (V)1.0d)}, new Results((V)"gate without one plate")));
}
} else if (R.Truth((V)(R.Get(v_535, (V)"controller") == (V)"review_vault"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_483, R.Get(R.Get(v_535, (V)"position"), (V)"floor_id")) == (V)1.0d)}, new Results((V)"vault without key")));
}
}
foreach (var row_547 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"review_keys"))))))) {
V v_548 = row_547[0];
V v_549 = row_547[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Get(v_478, v_548), () => (V)(v_549 == (V)true)), () => (V)(R.Get(v_483, v_548) == (V)1.0d))}, new Results((V)"invalid held vault key")));
}
foreach (var row_550 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_477, (V)"review_clues"))))))) {
V v_551 = row_550[0];
V v_552 = row_550[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_478, v_551), () => R.Or(R.Or(R.Or((V)(v_552 == (V)"north"), () => (V)(v_552 == (V)"east")), () => (V)(v_552 == (V)"south")), () => (V)(v_552 == (V)"west")))}, new Results((V)"invalid vault clue")));
}
foreach (var row_553 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"decoy"), new Entry((V)2, (V)"snare")))))))) {
V v_554 = row_553[0];
V v_555 = row_553[1];
var values_556 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_477, (V)"class_special"), v_555)));
V v_557 = values_556[0];
if (R.Truth(v_557)) {
R.Call(v_485, R.Join(new V[] {R.Get(v_557, (V)"floor_id"), R.Get(v_557, (V)"x")}, new Results(R.Get(v_557, (V)"y"))));
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
