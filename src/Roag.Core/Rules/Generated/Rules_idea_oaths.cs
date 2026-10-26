// Static C# port of src/idea_oaths.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 3f1f10c546ed1a02a80202a68e4278318b8f8ea609b1d635ecb30e7464c97326
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_oaths { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"radius", (V)4.0d), new Entry((V)"a", (V)"core:enemy/oath_twin_a"), new Entry((V)"b", (V)"core:enemy/oath_twin_b"))));
V v_4 = values_3[0];
R.Set(v_4, (V)"is_twin", new V((RuleFunction)(args_5 => {
V v_6 = args_5[0];
return R.Join(new V[] {}, new Results(R.And(v_6, () => R.Or((V)(R.Get(v_6, (V)"definition_id") == R.Get(v_4, (V)"a")), () => (V)(R.Get(v_6, (V)"definition_id") == R.Get(v_4, (V)"b"))))));
return Results.Empty;
})));
V v_7 = V.Nil;
v_7 = new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
V v_10 = args_8[1];
foreach (var row_11 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_9, (V)"entities"))))))) {
V v_12 = row_11[0];
V v_13 = row_11[1];
if (R.Truth((V)(R.Get(v_13, (V)"id") == v_10))) {
return R.Join(new V[] {}, new Results(v_13));
}
}
return Results.Empty;
}));
R.Set(v_4, (V)"partner", new V((RuleFunction)(args_14 => {
V v_15 = args_14[0];
V v_16 = args_14[1];
if (R.Truth(R.Or(R.Or(R.Or((V)(!R.Truth(R.Call(R.Get(v_4, (V)"is_twin"), R.Join(new V[] {}, new Results(v_16))).First)), () => R.Get(v_16, (V)"oath_broken")), () => (V)(!R.Truth(R.Get(v_16, (V)"hp")))), () => (V)(R.Get(v_16, (V)"hp") <= (V)0.0d)))) {
return Results.Empty;
}
var values_17 = R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {v_15}, new Results(R.Get(v_16, (V)"oath_partner_id")))));
V v_18 = values_17[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.Call(R.Get(v_4, (V)"is_twin"), R.Join(new V[] {}, new Results(v_18))).First, () => (V)(!R.Truth(R.Get(v_18, (V)"oath_broken")))), () => (V)(R.Get(v_18, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_18, (V)"oath_partner_id") == R.Get(v_16, (V)"id"))), () => (V)(R.Get(R.Get(v_18, (V)"position"), (V)"floor_id") == R.Get(R.Get(v_16, (V)"position"), (V)"floor_id"))), () => (V)(R.Call(R.Get(v_2, (V)"distance"), R.Join(new V[] {R.Get(v_16, (V)"position")}, new Results(R.Get(v_18, (V)"position")))).First <= R.Get(v_4, (V)"radius"))), () => R.Call(R.Get(v_2, (V)"clear"), R.Join(new V[] {v_15, R.Get(v_16, (V)"position")}, new Results(R.Get(v_18, (V)"position")))).First), () => R.Call(R.Get(v_2, (V)"clear"), R.Join(new V[] {v_15, R.Get(v_18, (V)"position")}, new Results(R.Get(v_16, (V)"position")))).First))) {
return R.Join(new V[] {}, new Results(v_18));
}
return Results.Empty;
})));
R.Set(v_4, (V)"prune", new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
foreach (var row_21 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_20, (V)"entities"))))))) {
V v_22 = row_21[0];
V v_23 = row_21[1];
if (R.Truth(R.And(R.And(R.Call(R.Get(v_4, (V)"is_twin"), R.Join(new V[] {}, new Results(v_23))).First, () => (V)(!R.Truth(R.Get(v_23, (V)"oath_broken")))), () => (V)(!R.Truth(R.Call(R.Get(v_4, (V)"partner"), R.Join(new V[] {v_20}, new Results(v_23))).First))))) {
var values_24 = R.Join(new V[] {}, new Results((V)true));
var obj_25 = v_23;
V key_26 = (V)"oath_broken";
R.Set(obj_25, key_26, values_24[0]);
var values_27 = R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {v_20}, new Results(R.Get(v_23, (V)"oath_partner_id")))));
V v_28 = values_27[0];
if (R.Truth(R.Call(R.Get(v_4, (V)"is_twin"), R.Join(new V[] {}, new Results(v_28))).First)) {
var values_29 = R.Join(new V[] {}, new Results((V)true));
var obj_30 = v_28;
V key_31 = (V)"oath_broken";
R.Set(obj_30, key_31, values_29[0]);
}
}
}
return Results.Empty;
})));
V v_32 = V.Nil;
v_32 = new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_34))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_34));
}
var values_35 = R.Join(new V[] {}, new Results(R.Table()));
V v_36 = values_35[0];

foreach (var row_37 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_34)))))) {
V v_38 = row_37[0];
V v_39 = row_37[1];
var values_40 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {}, new Results(v_39))));
var obj_41 = v_36;
V key_42 = v_38;
R.Set(obj_41, key_42, values_40[0]);
}

return R.Join(new V[] {}, new Results(v_36));
return Results.Empty;
}));
R.Set(v_4, (V)"redistribute", new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
var values_46 = R.Join(new V[] {}, new Results((V)false));
V v_47 = values_46[0];
foreach (var row_48 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_45)))))) {
V v_49 = row_48[0];
V v_50 = row_48[1];
if (R.Truth(R.Or((V)(R.Get(v_50, (V)"target_definition_id") == R.Get(v_4, (V)"a")), () => (V)(R.Get(v_50, (V)"target_definition_id") == R.Get(v_4, (V)"b"))))) {
var values_51 = R.Join(new V[] {}, new Results((V)true));
v_47 = values_51[0];

break;
}
}
if (R.Truth((V)(!R.Truth(v_47)))) {
return Results.Empty;
}
var values_52 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_53 = values_52[0];
V v_54 = values_52[1];
var values_55 = R.Join(new V[] {}, new Results((V)R.Length(v_45)));
V v_56 = values_55[0];
{ double end_59 = R.Num(v_56), step_60 = R.Num((V)1.0d);
for (double i_58 = R.Num((V)1.0d); step_60 > 0 ? i_58 <= end_59 : i_58 >= end_59; i_58 += step_60) {
V v_57 = i_58;
var values_61 = R.Join(new V[] {}, new Results(R.Get(v_45, v_57)));
var obj_62 = v_53;
V key_63 = R.Get(R.Get(v_45, v_57), (V)"target_id");
R.Set(obj_62, key_63, values_61[0]);
}
}
{ double end_66 = R.Num(v_56), step_67 = R.Num((V)1.0d);
for (double i_65 = R.Num((V)1.0d); step_67 > 0 ? i_65 <= end_66 : i_65 >= end_66; i_65 += step_67) {
V v_64 = i_65;
var values_68 = R.Join(new V[] {}, new Results(R.Get(v_45, v_64)));
V v_69 = values_68[0];
var values_70 = R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {v_44}, new Results(R.Get(v_69, (V)"target_id")))));
V v_71 = values_70[0];
var values_72 = R.Join(new V[] {}, new Results(R.And(v_71, () => R.Call(R.Get(v_4, (V)"partner"), R.Join(new V[] {v_44}, new Results(v_71))).First)));
V v_73 = values_72[0];
if (R.Truth(R.And(R.And(v_73, () => (V)(!R.Truth(R.Get(v_69, (V)"oath_shared_only")))), () => (V)(!R.Truth(R.Get(v_69, (V)"fate_skull")))))) {
var values_74 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_69, (V)"actual_loss") / (V)2.0d)))));
V v_75 = values_74[0];
if (R.Truth((V)(v_75 > (V)0.0d))) {
var values_76 = R.Join(new V[] {}, new Results((V)(R.Get(v_69, (V)"actual_loss") - v_75)));
var obj_77 = v_69;
V key_78 = (V)"actual_loss";
R.Set(obj_77, key_78, values_76[0]);
var values_79 = R.Join(new V[] {}, new Results((V)(R.Get(v_69, (V)"hp_before") - R.Get(v_69, (V)"actual_loss"))));
var obj_80 = v_69;
V key_81 = (V)"hp_after";
R.Set(obj_80, key_81, values_79[0]);

var values_82 = R.Join(new V[] {}, new Results(v_75));
var obj_83 = v_69;
V key_84 = (V)"oath_given";
R.Set(obj_83, key_84, values_82[0]);
var values_85 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_54, R.Get(v_73, (V)"id")), () => R.Table(new Entry((V)"amount", (V)0.0d), new Entry((V)"template", v_69)))));
V v_86 = values_85[0];
var values_87 = R.Join(new V[] {}, new Results((V)(R.Get(v_86, (V)"amount") + v_75)));
var obj_88 = v_86;
V key_89 = (V)"amount";
R.Set(obj_88, key_89, values_87[0]);

var values_90 = R.Join(new V[] {}, new Results(v_86));
var obj_91 = v_54;
V key_92 = R.Get(v_73, (V)"id");
R.Set(obj_91, key_92, values_90[0]);
}
}
}
}
var values_93 = R.Join(new V[] {}, new Results(R.Table()));
V v_94 = values_93[0];

foreach (var row_95 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_54)))))) {
V v_96 = row_95[0];
var values_97 = R.Join(new V[] {}, new Results(v_96));
var obj_98 = v_94;
V key_99 = (V)((V)R.Length(v_94) + (V)1.0d);
R.Set(obj_98, key_99, values_97[0]);
}

R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_94)));
foreach (var row_100 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_94)))))) {
V v_101 = row_100[0];
V v_102 = row_100[1];
var values_103 = R.Join(new V[] {}, new Results(R.Get(v_54, v_102)));
V v_104 = values_103[0];

var values_105 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {v_44}, new Results(v_102))))));
V v_106 = values_105[0];
var values_107 = R.Join(new V[] {}, new Results(R.Get(v_53, v_102)));
V v_108 = values_107[0];
if (R.Truth((V)(!R.Truth(v_108)))) {
var values_109 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {}, new Results(R.Get(v_104, (V)"template")))));
v_108 = values_109[0];
var values_110 = R.Join(new V[] {}, new Results(R.Get(v_106, (V)"id")));
var obj_111 = v_108;
V key_112 = (V)"target_id";
R.Set(obj_111, key_112, values_110[0]);

var values_113 = R.Join(new V[] {}, new Results(R.Get(v_106, (V)"definition_id")));
var obj_114 = v_108;
V key_115 = (V)"target_definition_id";
R.Set(obj_114, key_115, values_113[0]);
var values_116 = R.Join(new V[] {}, new Results(R.Get(v_106, (V)"faction")));
var obj_117 = v_108;
V key_118 = (V)"target_faction";
R.Set(obj_117, key_118, values_116[0]);

var values_119 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_106, (V)"definition_id")), new Entry((V)2, R.Get(v_106, (V)"faction")))));
var obj_120 = v_108;
V key_121 = (V)"target_tags";
R.Set(obj_120, key_121, values_119[0]);
var values_122 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_106, (V)"position"), (V)"floor_id")));
var obj_123 = v_108;
V key_124 = (V)"floor_id";
R.Set(obj_123, key_124, values_122[0]);

var values_125 = R.Join(new V[] {R.Get(R.Get(v_106, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_106, (V)"position"), (V)"y")));
var obj_126 = v_108;
V key_127 = (V)"x";
var obj_128 = v_108;
V key_129 = (V)"y";
R.Set(obj_126, key_127, values_125[0]);
R.Set(obj_128, key_129, values_125[1]);
var values_130 = R.Join(new V[] {}, new Results(R.Get(v_106, (V)"hp")));
var obj_131 = v_108;
V key_132 = (V)"hp_before";
R.Set(obj_131, key_132, values_130[0]);

var values_133 = R.Join(new V[] {}, new Results(R.Get(v_106, (V)"max_hp")));
var obj_134 = v_108;
V key_135 = (V)"target_max_hp";
R.Set(obj_134, key_135, values_133[0]);

var values_136 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_137 = v_108;
V key_138 = (V)"actual_loss";
R.Set(obj_137, key_138, values_136[0]);
var values_139 = R.Join(new V[] {}, new Results(R.Get(v_104, (V)"amount")));
var obj_140 = v_108;
V key_141 = (V)"attempted_damage";
R.Set(obj_140, key_141, values_139[0]);

var values_142 = R.Join(new V[] {}, new Results(R.Get(v_104, (V)"amount")));
var obj_143 = v_108;
V key_144 = (V)"after_defense";
R.Set(obj_143, key_144, values_142[0]);
var values_145 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_146 = v_108;
V key_147 = (V)"ward_absorbed";
R.Set(obj_146, key_147, values_145[0]);

var values_148 = R.Join(new V[] {}, new Results(V.Nil));
var obj_149 = v_108;
V key_150 = (V)"pickup_ward";
R.Set(obj_149, key_150, values_148[0]);

var values_151 = R.Join(new V[] {}, new Results(V.Nil));
var obj_152 = v_108;
V key_153 = (V)"reversal_bonus";
R.Set(obj_152, key_153, values_151[0]);
var values_154 = R.Join(new V[] {}, new Results(V.Nil));
var obj_155 = v_108;
V key_156 = (V)"block_reason";
R.Set(obj_155, key_156, values_154[0]);

var values_157 = R.Join(new V[] {}, new Results(V.Nil));
var obj_158 = v_108;
V key_159 = (V)"protecting_id";
R.Set(obj_158, key_159, values_157[0]);

var values_160 = R.Join(new V[] {}, new Results(V.Nil));
var obj_161 = v_108;
V key_162 = (V)"oath_given";
R.Set(obj_161, key_162, values_160[0]);
var values_163 = R.Join(new V[] {}, new Results((V)false));
var obj_164 = v_108;
V key_165 = (V)"direct_attack";
R.Set(obj_164, key_165, values_163[0]);

var values_166 = R.Join(new V[] {}, new Results((V)false));
var obj_167 = v_108;
V key_168 = (V)"critical";
R.Set(obj_167, key_168, values_166[0]);

var values_169 = R.Join(new V[] {}, new Results((V)"oath"));
var obj_170 = v_108;
V key_171 = (V)"damage_type";
R.Set(obj_170, key_171, values_169[0]);
var values_172 = R.Join(new V[] {}, new Results((V)"core:ability/oath_share"));
var obj_173 = v_108;
V key_174 = (V)"ability_id";
R.Set(obj_173, key_174, values_172[0]);

var values_175 = R.Join(new V[] {}, new Results((V)true));
var obj_176 = v_108;
V key_177 = (V)"oath_shared_only";
R.Set(obj_176, key_177, values_175[0]);
var values_178 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_179 = v_108;
V key_180 = (V)"proc_coefficient";
R.Set(obj_179, key_180, values_178[0]);

var values_181 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_106, (V)"reward_exempt"), () => V.Nil)));
var obj_182 = v_108;
V key_183 = (V)"reward_exempt";
R.Set(obj_182, key_183, values_181[0]);
var values_184 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_106, (V)"statuses"), () => (V)(R.Get(R.Get(v_106, (V)"statuses"), (V)"burning") != V.Nil)), () => (V)false)));
var obj_185 = v_108;
V key_186 = (V)"target_burning";
R.Set(obj_185, key_186, values_184[0]);
var values_187 = R.Join(new V[] {}, new Results(V.Nil));
var obj_188 = v_108;
V key_189 = (V)"adjacent_hostile_count";
R.Set(obj_188, key_189, values_187[0]);

var values_190 = R.Join(new V[] {}, new Results(V.Nil));
var obj_191 = v_108;
V key_192 = (V)"adjacent_burning_count";
R.Set(obj_191, key_192, values_190[0]);
var values_193 = R.Join(new V[] {}, new Results(v_108));
var obj_194 = v_45;
V key_195 = (V)((V)R.Length(v_45) + (V)1.0d);
R.Set(obj_194, key_195, values_193[0]);
}
var values_196 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_108, (V)"hp_before")}, new Results((V)(R.Get(v_108, (V)"actual_loss") + R.Get(v_104, (V)"amount"))))));
var obj_197 = v_108;
V key_198 = (V)"actual_loss";
R.Set(obj_197, key_198, values_196[0]);
var values_199 = R.Join(new V[] {}, new Results((V)(R.Get(v_108, (V)"hp_before") - R.Get(v_108, (V)"actual_loss"))));
var obj_200 = v_108;
V key_201 = (V)"hp_after";
R.Set(obj_200, key_201, values_199[0]);

var values_202 = R.Join(new V[] {}, new Results(R.Get(v_104, (V)"amount")));
var obj_203 = v_108;
V key_204 = (V)"oath_received";
R.Set(obj_203, key_204, values_202[0]);
}
return Results.Empty;
})));
R.Set(v_4, (V)"validate", new V((RuleFunction)(args_205 => {
V v_206 = args_205[0];
foreach (var row_207 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_206, (V)"entities"))))))) {
V v_208 = row_207[0];
V v_209 = row_207[1];
if (R.Truth(R.Or(R.Or(R.Call(R.Get(v_4, (V)"is_twin"), R.Join(new V[] {}, new Results(v_209))).First, () => R.Get(v_209, (V)"oath_partner_id")), () => R.Get(v_209, (V)"oath_broken")))) {
var values_210 = R.Join(new V[] {}, new Results(R.Get(v_209, (V)"oath_partner_id")));
V v_211 = values_210[0];

var values_212 = R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {v_206}, new Results(v_211))));
V v_213 = values_212[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.Call(R.Get(v_4, (V)"is_twin"), R.Join(new V[] {}, new Results(v_209))).First, () => (V)(R.Get(v_209, (V)"controller") == (V)"ai")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_211))).First == (V)"number")), () => (V)((V)(v_211 % (V)1.0d) == (V)0.0d)), () => (V)(v_211 >= (V)1.0d)), () => (V)(v_211 < R.Get(v_206, (V)"next_entity_id"))), () => (V)(v_211 != R.Get(v_209, (V)"id"))), () => R.Or((V)(R.Get(v_209, (V)"oath_broken") == V.Nil), () => (V)(R.Get(v_209, (V)"oath_broken") == (V)true)))}, new Results((V)"invalid oath owner")));
if (R.Truth(v_213)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Call(R.Get(v_4, (V)"is_twin"), R.Join(new V[] {}, new Results(v_213))).First, () => (V)(R.Get(v_213, (V)"definition_id") != R.Get(v_209, (V)"definition_id"))), () => (V)(R.Get(v_213, (V)"oath_partner_id") == R.Get(v_209, (V)"id")))}, new Results((V)"invalid oath pair")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Get(v_209, (V)"oath_broken"), () => R.Call(R.Get(v_4, (V)"partner"), R.Join(new V[] {v_206}, new Results(v_209))).First)}, new Results((V)"invalid intact oath")));
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
