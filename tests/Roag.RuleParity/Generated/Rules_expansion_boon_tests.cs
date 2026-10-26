// Static C# port of tests/expansion_boon_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 38ce8fb340764cad6d2bc2f13e3fd6b2fbcbad548c5fff097dd76b29ae18590c
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_expansion_boon_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievements"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_19 = values_18[0];
V v_20 = V.Nil;
v_20 = new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
V v_23 = args_21[1];
V v_24 = args_21[2];
var values_25 = R.Join(new V[] {}, new Results((V)0.0d));
V v_26 = values_25[0];
foreach (var row_27 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_22)))))) {
V v_28 = row_27[0];
V v_29 = row_27[1];
if (R.Truth(R.And((V)(R.Get(v_29, (V)"kind") == v_23), () => R.Or((V)(!R.Truth(v_24)), () => (V)(R.Get(v_29, (V)"boon_id") == R.Concat((V)"core:boon/", v_24)))))) {
var values_30 = R.Join(new V[] {}, new Results((V)(v_26 + (V)1.0d)));
v_26 = values_30[0];
}
}
return R.Join(new V[] {}, new Results(v_26));
return Results.Empty;
}));
V v_31 = V.Nil;
v_31 = new V((RuleFunction)(args_32 => {
V v_33 = args_32[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_33))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_33));
}
var values_34 = R.Join(new V[] {}, new Results(R.Table()));
V v_35 = values_34[0];
foreach (var row_36 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_33)))))) {
V v_37 = row_36[0];
V v_38 = row_36[1];
var values_39 = R.Join(new V[] {}, R.Call(v_31, R.Join(new V[] {}, new Results(v_38))));
var obj_40 = v_35;
V key_41 = v_37;
R.Set(obj_40, key_41, values_39[0]);
}
return R.Join(new V[] {}, new Results(v_35));
return Results.Empty;
}));
V v_42 = V.Nil;
v_42 = new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
var values_46 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {(V)7.0d, (V)"core:map/test_room"}, new Results(R.Table(new Entry((V)"class_id", v_45))))));
V v_47 = values_46[0];
if (R.Truth(v_44)) {
R.Call(R.Get(v_13, (V)"set_boon_stacks"), R.Join(new V[] {v_47, R.Get(v_47, (V)"player_id"), R.Concat((V)"core:boon/", v_44), (V)"rare"}, new Results((V)1.0d)));
}
return R.Join(new V[] {}, new Results(v_47));
return Results.Empty;
}));
V v_48 = V.Nil;
v_48 = new V((RuleFunction)(args_49 => {
V v_50 = args_49[0];
V v_51 = args_49[1];
V v_52 = args_49[2];
V v_53 = args_49[3];
if (R.Truth((V)(!R.Truth(R.Get(v_50, (V)"generated_map"))))) {
var values_54 = R.Join(new V[] {}, R.Call(v_31, R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/test_room"))))));
var obj_55 = v_50;
V key_56 = (V)"generated_map";
R.Set(obj_55, key_56, values_54[0]);
}
var values_57 = R.Join(new V[] {}, new Results(R.Get(v_50, (V)"generated_map")));
V v_58 = values_57[0];
var values_59 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_58, (V)"rows"), v_53), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_52 - (V)1.0d)))).First, R.Concat(v_51, R.Invoke(R.Get(R.Get(v_58, (V)"rows"), v_53), "sub", R.Join(new V[] {}, new Results((V)(v_52 + (V)1.0d)))).First))));
var obj_60 = R.Get(v_58, (V)"rows");
V key_61 = v_53;
R.Set(obj_60, key_61, values_59[0]);
return Results.Empty;
}));
V v_62 = V.Nil;
v_62 = new V((RuleFunction)(args_63 => {
V v_64 = args_63[0];
V v_65 = args_63[1];
V v_66 = args_63[2];
V v_67 = args_63[3];
var values_68 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_64, (V)"core:enemy/mossbound_guard", v_65}, new Results(v_66))));
V v_69 = values_68[0];
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_64}, new Results(R.Get(v_69, (V)"id"))));
if (R.Truth(v_67)) {
var values_70 = R.Join(new V[] {}, new Results(v_67));
var obj_71 = v_69;
V key_72 = (V)"hp";
R.Set(obj_71, key_72, values_70[0]);

var values_73 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_69, (V)"max_hp")}, new Results(v_67))));
var obj_74 = v_69;
V key_75 = (V)"max_hp";
R.Set(obj_74, key_75, values_73[0]);
}
return R.Join(new V[] {}, new Results(v_69));
return Results.Empty;
}));
V v_76 = V.Nil;
v_76 = new V((RuleFunction)(args_77 => {
V v_78 = args_77[0];
V v_79 = args_77[1];
V v_80 = args_77[2];
V v_81 = args_77[3];
var values_82 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"submit"), R.Join(new V[] {v_78}, new Results(R.Table(new Entry((V)"kind", v_79), new Entry((V)"dx", v_80), new Entry((V)"dy", v_81))))));
V v_83 = values_82[0];
V v_84 = values_82[1];
V v_85 = values_82[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_83}, new Results(v_84)));
return R.Join(new V[] {}, new Results(v_85));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"additive boon identities retain 20 originals, four unlocks and class-aware normal offers"}, new Results(new V((RuleFunction)(args_86 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_5, (V)"validate"), Results.Empty).First, () => (V)((V)R.Length(R.Get(v_5, (V)"order")) == (V)((V)53.0d + (V)R.Length(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"order"))))))));
var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"starter_order"), Results.Empty));
V v_88 = values_87[0];
var values_89 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"pool"), R.Join(new V[] {}, new Results(R.Table()))));
V v_90 = values_89[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_88) == (V)20.0d), () => (V)((V)R.Length(v_90) == (V)((V)49.0d + (V)R.Length(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"order"))))))));
{ double end_93 = R.Num((V)20.0d), step_94 = R.Num((V)1.0d);
for (double i_92 = R.Num((V)1.0d); step_94 > 0 ? i_92 <= end_93 : i_92 >= end_93; i_92 += step_94) {
V v_91 = i_92;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_90, v_91) == R.Get(v_88, v_91)))));
}
}
var values_95 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_expansion_content"))));
V v_96 = values_95[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_96, (V)"order")) == (V)26.0d))));
var values_97 = R.Join(new V[] {}, new Results(R.Table()));
V v_98 = values_97[0];
foreach (var row_99 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_19, (V)"order"))))))) {
V v_100 = row_99[0];
V v_101 = row_99[1];
var values_102 = R.Join(new V[] {}, new Results(R.Table()));
V v_103 = values_102[0];
{ double end_106 = R.Num((V)950.0d), step_107 = R.Num((V)1.0d);
for (double i_105 = R.Num((V)1.0d); step_107 > 0 ? i_105 <= end_106 : i_105 >= end_106; i_105 += step_107) {
V v_104 = i_105;
var values_108 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {v_104, (V)"core:map/test_room"}, new Results(R.Table(new Entry((V)"class_id", v_101), new Entry((V)"available_boons", v_90))))));
V v_109 = values_108[0];
var values_110 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"make_offers"), R.Join(new V[] {v_109}, R.Call(R.Get(v_11, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/standard"))))));
V v_111 = values_110[0];
foreach (var row_112 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_111)))))) {
V v_113 = row_112[0];
V v_114 = row_112[1];
var values_115 = R.Join(new V[] {}, new Results((V)true));
var obj_116 = v_103;
V key_117 = R.Get(v_114, (V)"boon_id");
R.Set(obj_116, key_117, values_115[0]);
var values_118 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_114, (V)"boon_id")))).First, (V)"eligible_classes")));
V v_119 = values_118[0];
if (R.Truth(v_119)) {
var values_120 = R.Join(new V[] {}, new Results((V)false));
V v_121 = values_120[0];
foreach (var row_122 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_119)))))) {
V v_123 = row_122[0];
V v_124 = row_122[1];
if (R.Truth((V)(v_124 == v_101))) {
var values_125 = R.Join(new V[] {}, new Results((V)true));
v_121 = values_125[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_121}, new Results((V)"offered unusable class boon")));
}
}
}
}
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_96, (V)"order"))))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
var values_129 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"get"), R.Join(new V[] {}, new Results(v_128))).First, (V)"eligible_classes")));
V v_130 = values_129[0];
if (R.Truth(R.Or((V)(!R.Truth(v_130)), () => (V)(R.Get(v_130, (V)1.0d) == v_101)))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_103, v_128)}, new Results(R.Concat((V)"new boon never offered: ", v_128))));
var values_131 = R.Join(new V[] {}, new Results((V)true));
var obj_132 = v_98;
V key_133 = v_128;
R.Set(obj_132, key_133, values_131[0]);
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_103, v_128)))}, new Results(R.Concat((V)"class boon leaked: ", v_128))));
}
}
}
foreach (var row_134 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_96, (V)"order"))))))) {
V v_135 = row_134[0];
V v_136 = row_134[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_98, v_136))));
}
var values_137 = R.Join(new V[] {}, new Results(R.Table()));
V v_138 = values_137[0];
foreach (var row_139 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_19, (V)"order"))))))) {
V v_140 = row_139[0];
V v_141 = row_139[1];
{ double end_144 = R.Num((V)100.0d), step_145 = R.Num((V)1.0d);
for (double i_143 = R.Num((V)1.0d); step_145 > 0 ? i_143 <= end_144 : i_143 >= end_144; i_143 += step_145) {
V v_142 = i_143;
var values_146 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new_stage"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"class_id", v_141), new Entry((V)"available_boons", v_90))))));
V v_147 = values_146[0];
foreach (var row_148 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_147, (V)"entities"))))))) {
V v_149 = row_148[0];
V v_150 = row_148[1];
if (R.Truth((V)(R.Get(v_150, (V)"controller") == (V)"chest"))) {
foreach (var row_151 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_150, (V)"offers"))))))) {
V v_152 = row_151[0];
V v_153 = row_151[1];
var values_154 = R.Join(new V[] {}, new Results((V)true));
var obj_155 = v_138;
V key_156 = R.Get(v_153, (V)"boon_id");
R.Set(obj_155, key_156, values_154[0]);
}
}
}
}
}
}
foreach (var row_157 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_96, (V)"order"))))))) {
V v_158 = row_157[0];
V v_159 = row_157[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_138, v_159)}, new Results(R.Concat((V)"new boon absent from generated chests: ", v_159))));
}
{ double end_162 = R.Num((V)24.0d), step_163 = R.Num((V)1.0d);
for (double i_161 = R.Num((V)21.0d); step_163 > 0 ? i_161 <= end_162 : i_161 >= end_162; i_161 += step_163) {
V v_160 = i_161;
foreach (var row_164 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_90)))))) {
V v_165 = row_164[0];
V v_166 = row_164[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_166 != R.Get(R.Get(v_5, (V)"order"), v_160)))));
}
}
}
var values_167 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"stonewake"))));
V v_168 = values_167[0];
R.Call(R.Get(v_13, (V)"set_boon_stacks"), R.Join(new V[] {v_168, R.Get(v_168, (V)"player_id"), (V)"core:boon/stonewake", (V)"common"}, new Results((V)9.0d)));
R.Call(R.Get(v_13, (V)"set_boon_stacks"), R.Join(new V[] {v_168, R.Get(v_168, (V)"player_id"), (V)"core:boon/stonewake", (V)"legendary"}, new Results((V)7.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_9, (V)"aggregate"), R.Join(new V[] {(V)"core:boon/stonewake", R.Get(R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_168))).First, (V)"boons"), (V)"core:boon/stonewake")}, new Results((V)"power"))).First == (V)40.0d))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_168))).First}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_168)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"terrain boons activate through actual bog, bramble, ice, break, mist and crystal"}, new Results(new V((RuleFunction)(args_169 => {

var values_170 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"stonewake"))));
V v_171 = values_170[0];
R.Call(v_48, R.Join(new V[] {v_171, (V)"g", (V)2.0d}, new Results((V)2.0d)));
var values_172 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_171, (V)4.0d, (V)2.0d}, new Results((V)20.0d))));
V v_173 = values_172[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_20, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_171, (V)"move", (V)1.0d}, new Results((V)0.0d))).First, (V)"BoonActivated"}, new Results((V)"stonewake"))).First >= (V)1.0d))));
var values_174 = R.Join(new V[] {}, new Results(R.Get(v_173, (V)"hp")));
V v_175 = values_174[0];
var values_176 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_171, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_177 = values_176[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_177, (V)"BoonActivated"}, new Results((V)"stonewake"))).First >= (V)1.0d), () => (V)(R.Get(v_173, (V)"hp") <= (V)(v_175 - (V)5.0d))))));
var values_178 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"bramble_compact"))));
v_171 = values_178[0];

R.Call(v_48, R.Join(new V[] {v_171, (V)"b", (V)3.0d}, new Results((V)2.0d)));

var values_179 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_171, (V)4.0d, (V)2.0d}, new Results((V)20.0d))));
v_173 = values_179[0];
var values_180 = R.Join(new V[] {}, new Results(R.Get(v_173, (V)"hp")));
v_175 = values_180[0];

var values_181 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_171, (V)"move", (V)1.0d}, new Results((V)0.0d))));
v_177 = values_181[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_20, R.Join(new V[] {v_177}, new Results((V)"TerrainTriggered"))).First >= (V)1.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_177, (V)"BoonActivated"}, new Results((V)"bramble_compact"))).First >= (V)1.0d)), () => (V)(R.Get(v_173, (V)"hp") < v_175)))));
var values_182 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"froststep_catalyst"))));
v_171 = values_182[0];

R.Call(v_48, R.Join(new V[] {v_171, (V)"i", (V)3.0d}, new Results((V)2.0d)));
var values_183 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_171, (V)6.0d, (V)2.0d}, new Results((V)20.0d))));
v_173 = values_183[0];

var values_184 = R.Join(new V[] {}, new Results(R.Get(v_173, (V)"hp")));
v_175 = values_184[0];
var values_185 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_171, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
v_177 = values_185[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_177, (V)"BoonActivated"}, new Results((V)"froststep_catalyst"))).First >= (V)1.0d), () => (V)(R.Get(v_173, (V)"hp") < v_175)))));
var values_186 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"fracture_dividend"))));
v_171 = values_186[0];

R.Call(v_48, R.Join(new V[] {v_171, (V)"B", (V)3.0d}, new Results((V)2.0d)));
var values_187 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_171, (V)4.0d, (V)2.0d}, new Results((V)20.0d))));
v_173 = values_187[0];

var values_188 = R.Join(new V[] {}, new Results(R.Get(v_173, (V)"hp")));
v_175 = values_188[0];
var values_189 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_171, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
v_177 = values_189[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_20, R.Join(new V[] {v_177}, new Results((V)"TerrainBroken"))).First >= (V)1.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_177, (V)"BoonActivated"}, new Results((V)"fracture_dividend"))).First >= (V)1.0d)), () => (V)(R.Get(v_173, (V)"hp") < v_175)))));
var values_190 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"mist_veil"))));
v_171 = values_190[0];

R.Call(v_48, R.Join(new V[] {v_171, (V)"m", (V)2.0d}, new Results((V)2.0d)));
var values_191 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_171, (V)3.0d, (V)2.0d}, new Results((V)20.0d))));
v_173 = values_191[0];

var values_192 = R.Join(new V[] {}, new Results(R.Get(v_173, (V)"hp")));
v_175 = values_192[0];
var values_193 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_171, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
v_177 = values_193[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_177, (V)"BoonActivated"}, new Results((V)"mist_veil"))).First >= (V)1.0d), () => (V)(R.Get(v_173, (V)"hp") <= (V)(v_175 - (V)5.0d))))));
var values_194 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"crystal_harmony"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)3.0d)))));
v_171 = values_194[0];

R.Call(v_48, R.Join(new V[] {v_171, (V)"c", (V)3.0d}, new Results((V)3.0d)));
R.Call(v_62, R.Join(new V[] {v_171, (V)3.0d, (V)2.0d}, new Results((V)20.0d)));

var values_195 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_171, (V)4.0d, (V)2.0d}, new Results((V)20.0d))));
v_173 = values_195[0];

var values_196 = R.Join(new V[] {}, new Results(R.Get(v_173, (V)"hp")));
v_175 = values_196[0];
var values_197 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_171, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
v_177 = values_197[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_177, (V)"BoonActivated"}, new Results((V)"crystal_harmony"))).First >= (V)1.0d), () => (V)(R.Get(v_173, (V)"hp") < v_175)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"conditional offense uses marks, wounded health, Burning and three-target batches"}, new Results(new V((RuleFunction)(args_198 => {

var values_199 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"sunderers_mark"))));
V v_200 = values_199[0];
var values_201 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_200, (V)3.0d, (V)2.0d}, new Results((V)30.0d))));
V v_202 = values_201[0];
var values_203 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_200, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_204 = values_203[0];
var values_205 = R.Join(new V[] {}, new Results(R.Get(v_202, (V)"hp")));
V v_206 = values_205[0];
var values_207 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_200, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_208 = values_207[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_20, R.Join(new V[] {v_204, (V)"BoonActivated"}, new Results((V)"sunderers_mark"))).First == (V)1.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_208, (V)"BoonActivated"}, new Results((V)"sunderers_mark"))).First == (V)1.0d)), () => (V)((V)(v_206 - R.Get(v_202, (V)"hp")) > (V)4.0d)))));
var values_209 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_200))).First, (V)"boon_memory"), (V)"core:boon/sunderers_mark")));
V v_210 = values_209[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_210, (V)"mark_target_id") == R.Get(v_202, (V)"id")), () => (V)(R.Get(v_210, (V)"mark_count") == (V)2.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_200))).First}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_200)))))))));
var values_211 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"executioners_thread"))));
v_200 = values_211[0];

var values_212 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_200, (V)3.0d, (V)2.0d}, new Results((V)20.0d))));
v_202 = values_212[0];

var values_213 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_214 = v_202;
V key_215 = (V)"hp";
R.Set(obj_214, key_215, values_213[0]);
var values_216 = R.Join(new V[] {}, new Results(R.Get(v_202, (V)"hp")));
V v_217 = values_216[0];

var values_218 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_200, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_219 = values_218[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_219, (V)"BoonActivated"}, new Results((V)"executioners_thread"))).First >= (V)1.0d), () => (V)((V)(v_217 - R.Get(v_202, (V)"hp")) > (V)4.0d)))));
var values_220 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"shared_agony"))));
v_200 = values_220[0];
var values_221 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_200, (V)3.0d, (V)2.0d}, new Results((V)20.0d))));
V v_222 = values_221[0];
var values_223 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_200, (V)3.0d, (V)3.0d}, new Results((V)20.0d))));
V v_224 = values_223[0];
var values_225 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"burning", R.Table(new Entry((V)"power", (V)1.0d), new Entry((V)"remaining", (V)3.0d), new Entry((V)"interval", (V)100.0d), new Entry((V)"next_due", (V)100.0d))))));
var obj_226 = v_222;
V key_227 = (V)"statuses";
R.Set(obj_226, key_227, values_225[0]);
var values_228 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"burning", R.Table(new Entry((V)"power", (V)1.0d), new Entry((V)"remaining", (V)3.0d), new Entry((V)"interval", (V)100.0d), new Entry((V)"next_due", (V)100.0d))))));
var obj_229 = v_224;
V key_230 = (V)"statuses";
R.Set(obj_229, key_230, values_228[0]);
var values_231 = R.Join(new V[] {}, new Results(R.Get(v_224, (V)"hp")));
v_217 = values_231[0];

var values_232 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_200, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
v_219 = values_232[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_219, (V)"BoonActivated"}, new Results((V)"shared_agony"))).First >= (V)1.0d), () => (V)(R.Get(v_224, (V)"hp") < v_217)))));
var values_233 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"cluster_rupture"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)3.0d)))));
v_200 = values_233[0];
R.Call(v_62, R.Join(new V[] {v_200, (V)3.0d, (V)2.0d}, new Results((V)20.0d)));

R.Call(v_62, R.Join(new V[] {v_200, (V)4.0d, (V)2.0d}, new Results((V)20.0d)));

var values_234 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_200, (V)5.0d, (V)2.0d}, new Results((V)20.0d))));
v_202 = values_234[0];
var values_235 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_200, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
v_219 = values_235[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_20, R.Join(new V[] {v_219, (V)"BoonActivated"}, new Results((V)"cluster_rupture"))).First >= (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"block, harm, credited kill and alternating motion provide bounded stateful boons"}, new Results(new V((RuleFunction)(args_236 => {

var values_237 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"counterweight"))));
V v_238 = values_237[0];
var values_239 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_238, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)0.0d))));
V v_240 = values_239[0];
var values_241 = R.Join(new V[] {}, new Results((V)20.0d));
var obj_242 = R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_238))).First;
V key_243 = (V)"defense";
R.Set(obj_242, key_243, values_241[0]);
R.Call(v_76, R.Join(new V[] {v_238}, new Results((V)"wait")));
var values_244 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_238}, new Results((V)"wait"))));
V v_245 = values_244[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_245}, new Results((V)"DamageBlocked"))).First >= (V)1.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_245, (V)"BoonActivated"}, new Results((V)"counterweight"))).First >= (V)1.0d)))));
var values_246 = R.Join(new V[] {}, new Results(R.Get(v_240, (V)"hp")));
V v_247 = values_246[0];
var values_248 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_238, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
v_245 = values_248[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_245, (V)"BoonActivated"}, new Results((V)"counterweight"))).First >= (V)1.0d), () => (V)((V)(v_247 - R.Get(v_240, (V)"hp")) > R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_238))).First, (V)"slash_damage"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_238))).First, (V)"boon_memory"), (V)"core:boon/counterweight"), (V)"charges") == (V)0.0d))));
var values_249 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"pain_dividend"))));
v_238 = values_249[0];
var values_250 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_238, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)0.0d))));
v_240 = values_250[0];
R.Call(v_76, R.Join(new V[] {v_238}, new Results((V)"wait")));

var values_251 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_238}, new Results((V)"wait"))));
v_245 = values_251[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_245}, new Results((V)"DamageTaken"))).First >= (V)1.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_245, (V)"BoonActivated"}, new Results((V)"pain_dividend"))).First >= (V)1.0d)))));
var values_252 = R.Join(new V[] {}, new Results(R.Get(v_240, (V)"hp")));
v_247 = values_252[0];
var values_253 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_238, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
v_245 = values_253[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_245, (V)"BoonActivated"}, new Results((V)"pain_dividend"))).First >= (V)1.0d), () => (V)((V)(v_247 - R.Get(v_240, (V)"hp")) > R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_238))).First, (V)"damage"))))));
var values_254 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"bloodrush"))));
v_238 = values_254[0];
var values_255 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_238, (V)3.0d, (V)2.0d}, new Results((V)1.0d))));
v_240 = values_255[0];
var values_256 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_13, (V)"preview"), R.Join(new V[] {v_238, (V)"move", (V)0.0d}, new Results((V)1.0d))).First, (V)"cost")));
V v_257 = values_256[0];
var values_258 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_238, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
v_245 = values_258[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_245}, new Results((V)"EntityKilled"))).First >= (V)1.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_245, (V)"BoonActivated"}, new Results((V)"bloodrush"))).First >= (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_13, (V)"preview"), R.Join(new V[] {v_238, (V)"move", (V)0.0d}, new Results((V)1.0d))).First, (V)"cost") < v_257))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_238))).First}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_238)))))))));
var values_259 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"rhythm_of_steel"))));
v_238 = values_259[0];
var values_260 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_238, (V)4.0d, (V)2.0d}, new Results((V)20.0d))));
v_240 = values_260[0];
var values_261 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_238, (V)"move", (V)1.0d}, new Results((V)0.0d))));
v_245 = values_261[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_20, R.Join(new V[] {v_245, (V)"BoonActivated"}, new Results((V)"rhythm_of_steel"))).First >= (V)1.0d))));
var values_262 = R.Join(new V[] {}, new Results(R.Get(v_240, (V)"hp")));
v_247 = values_262[0];

var values_263 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_238, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
v_245 = values_263[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_245, (V)"BoonActivated"}, new Results((V)"rhythm_of_steel"))).First >= (V)1.0d), () => (V)((V)(v_247 - R.Get(v_240, (V)"hp")) > R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_238))).First, (V)"damage"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"enemy-originated windups, approach and friendly fire drive reactions"}, new Results(new V((RuleFunction)(args_264 => {

var values_265 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"windup_hunter"))));
V v_266 = values_265[0];
var values_267 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_266, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)0.0d))));
V v_268 = values_267[0];
var values_269 = R.Join(new V[] {}, new Results(R.Get(v_268, (V)"hp")));
V v_270 = values_269[0];
var values_271 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_266}, new Results((V)"wait"))));
V v_272 = values_271[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(v_20, R.Join(new V[] {v_272}, new Results((V)"WindupStarted"))).First >= (V)1.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_272, (V)"BoonActivated"}, new Results((V)"windup_hunter"))).First >= (V)1.0d)), () => (V)(R.Get(v_268, (V)"hp") < v_270)), () => R.Get(v_268, (V)"pending_attack")))));
var values_273 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"crossfire_omen"))));
v_266 = values_273[0];
R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_266, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)0.0d)));
R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_266, (V)"core:enemy/mossbound_guard", (V)2.0d, (V)3.0d}, new Results((V)0.0d)));
var values_274 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_266}, new Results((V)"wait"))));
v_272 = values_274[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_272}, new Results((V)"WindupStarted"))).First >= (V)2.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_272, (V)"BoonActivated"}, new Results((V)"crossfire_omen"))).First >= (V)1.0d)))));
var values_275 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"pursuit_spark"))));
v_266 = values_275[0];
var values_276 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_266, (V)"core:enemy/mossbound_guard", (V)5.0d, (V)2.0d}, new Results((V)0.0d))));
v_268 = values_276[0];
var values_277 = R.Join(new V[] {}, new Results(R.Get(v_268, (V)"hp")));
v_270 = values_277[0];

var values_278 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_266}, new Results((V)"wait"))));
v_272 = values_278[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_20, R.Join(new V[] {v_272}, new Results((V)"Moved"))).First >= (V)1.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_272, (V)"BoonActivated"}, new Results((V)"pursuit_spark"))).First >= (V)1.0d)), () => (V)(R.Get(v_268, (V)"hp") < v_270)))));
foreach (var row_279 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"far_witness"), new Entry((V)2, (V)"betrayal_ward")))))))) {
V v_280 = row_279[0];
V v_281 = row_279[1];
var values_282 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results(v_281))));
v_266 = values_282[0];
R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_266, (V)"core:enemy/thornspitter", (V)9.0d, (V)2.0d}, new Results((V)0.0d)));
var values_283 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_266, (V)"core:enemy/mossbound_guard", (V)7.0d, (V)2.0d}, new Results((V)0.0d))));
v_268 = values_283[0];
var values_284 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_266}, new Results((V)"wait"))));
v_272 = values_284[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(v_20, R.Join(new V[] {v_272, (V)"BoonActivated"}, new Results(v_281))).First >= (V)1.0d)}, new Results(R.Concat(v_281, (V)" did not observe real hostile friendly fire"))));
if (R.Truth((V)(v_281 == (V)"far_witness"))) {
var values_285 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results(V.Nil))));
V v_286 = values_285[0];
R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_286, (V)"core:enemy/thornspitter", (V)9.0d, (V)2.0d}, new Results((V)0.0d)));
var values_287 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_286, (V)"core:enemy/mossbound_guard", (V)7.0d, (V)2.0d}, new Results((V)0.0d))));
V v_288 = values_287[0];
R.Call(v_76, R.Join(new V[] {v_286}, new Results((V)"wait")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_268, (V)"hp") < R.Get(v_288, (V)"hp"))}, new Results((V)"distant witness dealt no bonus damage")));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_266))).First, (V)"boon_memory"), (V)"core:boon/betrayal_ward"), (V)"ward") == (V)3.0d))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"formation attack and status-sharing bonuses produce ordered secondary damage"}, new Results(new V((RuleFunction)(args_289 => {

var values_290 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"ordered_ranks"))));
V v_291 = values_290[0];
var values_292 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_291, (V)3.0d, (V)2.0d}, new Results((V)20.0d))));
V v_293 = values_292[0];
R.Call(v_62, R.Join(new V[] {v_291, (V)4.0d, (V)2.0d}, new Results((V)20.0d)));

R.Call(v_62, R.Join(new V[] {v_291, (V)5.0d, (V)2.0d}, new Results((V)20.0d)));
var values_294 = R.Join(new V[] {}, new Results(R.Get(v_293, (V)"hp")));
V v_295 = values_294[0];
var values_296 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_291, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_297 = values_296[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_297, (V)"BoonActivated"}, new Results((V)"ordered_ranks"))).First >= (V)1.0d), () => (V)((V)(v_295 - R.Get(v_293, (V)"hp")) > R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_291))).First, (V)"damage"))))));
var values_298 = Results.Empty;
V v_299 = values_298[0];
foreach (var row_300 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_297)))))) {
V v_301 = row_300[0];
V v_302 = row_300[1];
if (R.Truth((V)(R.Get(v_302, (V)"kind") == (V)"DamageBatchResolved"))) {
var values_303 = R.Join(new V[] {}, new Results(v_302));
v_299 = values_303[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_299, () => (V)(R.Get(R.Get(R.Get(v_299, (V)"outcomes"), (V)1.0d), (V)"target_id") == R.Get(v_293, (V)"id"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ten mixed old/new boon interactions retain one causal action and real effects"}, new Results(new V((RuleFunction)(args_304 => {

V v_305 = V.Nil;
v_305 = new V((RuleFunction)(args_306 => {
V v_307 = args_306[0];
V v_308 = args_306[1];
V v_309 = args_306[2];
V v_310 = args_306[3];
R.Call(R.Get(v_13, (V)"set_boon_stacks"), R.Join(new V[] {v_307, R.Get(v_307, (V)"player_id"), R.Concat((V)"core:boon/", v_308), R.Or(v_309, () => (V)"rare")}, new Results(R.Or(v_310, () => (V)1.0d))));
return Results.Empty;
}));
V v_311 = V.Nil;
v_311 = new V((RuleFunction)(args_312 => {
V v_313 = args_312[0];
V v_314 = args_312[1];
V v_315 = args_312[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(v_20, R.Join(new V[] {v_313, (V)"BoonActivated"}, new Results(v_314))).First >= (V)1.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_313, (V)"BoonActivated"}, new Results(v_315))).First >= (V)1.0d))}, new Results(R.Concat(v_314, R.Concat((V)" + ", R.Concat(v_315, (V)" failed to activate"))))));
return Results.Empty;
}));
var values_316 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"mist_veil"))));
V v_317 = values_316[0];
R.Call(v_305, R.Join(new V[] {v_317}, new Results((V)"opening_cut")));

R.Call(v_48, R.Join(new V[] {v_317, (V)"m", (V)2.0d}, new Results((V)2.0d)));

R.Call(v_62, R.Join(new V[] {v_317, (V)3.0d, (V)2.0d}, new Results((V)20.0d)));
R.Call(v_311, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_317, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"mist_veil"}, new Results((V)"opening_cut")));
var values_318 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"crystal_harmony"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)3.0d)))));
v_317 = values_318[0];
R.Call(v_305, R.Join(new V[] {v_317, (V)"storm_conductor", (V)"legendary"}, new Results((V)2.0d)));
R.Call(v_48, R.Join(new V[] {v_317, (V)"c", (V)3.0d}, new Results((V)3.0d)));

R.Call(v_62, R.Join(new V[] {v_317, (V)3.0d, (V)2.0d}, new Results((V)20.0d)));

R.Call(v_62, R.Join(new V[] {v_317, (V)4.0d, (V)2.0d}, new Results((V)20.0d)));
R.Call(v_311, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_317, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"crystal_harmony"}, new Results((V)"storm_conductor")));
var values_319 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"bloodrush"))));
v_317 = values_319[0];

R.Call(v_305, R.Join(new V[] {v_317}, new Results((V)"grave_dividend")));
var values_320 = R.Join(new V[] {}, new Results((V)20.0d));
var obj_321 = R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_317))).First;
V key_322 = (V)"hp";
R.Set(obj_321, key_322, values_320[0]);

R.Call(v_62, R.Join(new V[] {v_317, (V)3.0d, (V)2.0d}, new Results((V)1.0d)));
R.Call(v_311, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_317, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"bloodrush"}, new Results((V)"grave_dividend")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_317))).First, (V)"hp") > (V)20.0d))));
var values_323 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"windup_hunter"))));
v_317 = values_323[0];

R.Call(v_305, R.Join(new V[] {v_317, (V)"harriers_response", (V)"legendary"}, new Results((V)2.0d)));
R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_317, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)0.0d)));
R.Call(v_311, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_317}, new Results((V)"wait"))).First, (V)"windup_hunter"}, new Results((V)"harriers_response")));
var values_324 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"betrayal_ward"))));
v_317 = values_324[0];

R.Call(v_305, R.Join(new V[] {v_317}, new Results((V)"turncoat_spark")));
var values_325 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_317, (V)"core:enemy/thornspitter", (V)9.0d, (V)2.0d}, new Results((V)0.0d))));
V v_326 = values_325[0];
R.Call(R.Get(v_13, (V)"spawn_enemy"), R.Join(new V[] {v_317, (V)"core:enemy/mossbound_guard", (V)7.0d, (V)2.0d}, new Results((V)0.0d)));
R.Call(v_311, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_317}, new Results((V)"wait"))).First, (V)"betrayal_ward"}, new Results((V)"turncoat_spark")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_326, (V)"hp") < R.Get(v_326, (V)"max_hp")))));
var values_327 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"rhythm_of_steel"))));
v_317 = values_327[0];

R.Call(v_305, R.Join(new V[] {v_317}, new Results((V)"pulse_step")));
R.Call(v_62, R.Join(new V[] {v_317, (V)4.0d, (V)2.0d}, new Results((V)20.0d)));

R.Call(v_62, R.Join(new V[] {v_317, (V)3.0d, (V)3.0d}, new Results((V)20.0d)));
R.Call(v_311, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_317, (V)"move", (V)1.0d}, new Results((V)0.0d))).First, (V)"rhythm_of_steel"}, new Results((V)"pulse_step")));
var values_328 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"cluster_rupture"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)3.0d)))));
v_317 = values_328[0];

R.Call(v_305, R.Join(new V[] {v_317}, new Results((V)"resonant_wounds")));
R.Call(v_62, R.Join(new V[] {v_317, (V)3.0d, (V)2.0d}, new Results((V)20.0d)));

R.Call(v_62, R.Join(new V[] {v_317, (V)4.0d, (V)2.0d}, new Results((V)20.0d)));

R.Call(v_62, R.Join(new V[] {v_317, (V)5.0d, (V)2.0d}, new Results((V)20.0d)));
R.Call(v_311, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_317, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))).First, (V)"cluster_rupture"}, new Results((V)"resonant_wounds")));
var values_329 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"shared_agony"))));
v_317 = values_329[0];

R.Call(v_305, R.Join(new V[] {v_317}, new Results((V)"flashpoint")));
var values_330 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_317, (V)3.0d, (V)2.0d}, new Results((V)20.0d))));
V v_331 = values_330[0];

var values_332 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_317, (V)3.0d, (V)3.0d}, new Results((V)20.0d))));
V v_333 = values_332[0];
var values_334 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"burning", R.Table(new Entry((V)"power", (V)1.0d), new Entry((V)"remaining", (V)3.0d), new Entry((V)"interval", (V)100.0d), new Entry((V)"next_due", (V)100.0d))))));
var obj_335 = v_331;
V key_336 = (V)"statuses";
R.Set(obj_335, key_336, values_334[0]);
var values_337 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"burning", R.Table(new Entry((V)"power", (V)1.0d), new Entry((V)"remaining", (V)3.0d), new Entry((V)"interval", (V)100.0d), new Entry((V)"next_due", (V)100.0d))))));
var obj_338 = v_333;
V key_339 = (V)"statuses";
R.Set(obj_338, key_339, values_337[0]);
R.Call(v_311, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_317, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"shared_agony"}, new Results((V)"flashpoint")));
var values_340 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"froststep_catalyst"))));
v_317 = values_340[0];

R.Call(v_305, R.Join(new V[] {v_317}, new Results((V)"phantom_lunge")));
R.Call(v_48, R.Join(new V[] {v_317, (V)"i", (V)3.0d}, new Results((V)2.0d)));

R.Call(v_62, R.Join(new V[] {v_317, (V)6.0d, (V)2.0d}, new Results((V)20.0d)));
R.Call(v_311, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_317, (V)"dash", (V)1.0d}, new Results((V)0.0d))).First, (V)"froststep_catalyst"}, new Results((V)"phantom_lunge")));
var values_341 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)"sunderers_mark"))));
v_317 = values_341[0];

R.Call(v_305, R.Join(new V[] {v_317}, new Results((V)"executioners_thread")));
var values_342 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_317, (V)3.0d, (V)2.0d}, new Results((V)20.0d))));
v_331 = values_342[0];

var values_343 = R.Join(new V[] {}, new Results((V)60.0d));
var obj_344 = v_331;
V key_345 = (V)"max_hp";
R.Set(obj_344, key_345, values_343[0]);
R.Call(v_76, R.Join(new V[] {v_317, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)));
R.Call(v_311, R.Join(new V[] {R.Call(v_76, R.Join(new V[] {v_317, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"sunderers_mark"}, new Results((V)"executioners_thread")));
var values_346 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"splitflight"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)2.0d)))));
v_317 = values_346[0];

R.Call(v_305, R.Join(new V[] {v_317}, new Results((V)"opening_cut")));
R.Call(v_62, R.Join(new V[] {v_317, (V)3.0d, (V)2.0d}, new Results((V)20.0d)));

R.Call(v_62, R.Join(new V[] {v_317, (V)3.0d, (V)3.0d}, new Results((V)20.0d)));
var values_347 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_317, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_348 = values_347[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(v_20, R.Join(new V[] {v_348, (V)"BoonActivated"}, new Results((V)"opening_cut"))).First >= (V)2.0d)}, new Results((V)"Ranger branch lost multi-target pre-hit HP facts")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all six class transformations change normal ability geometry or follow-up"}, new Results(new V((RuleFunction)(args_349 => {

var values_350 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"splitflight"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)2.0d)))));
V v_351 = values_350[0];
var values_352 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_351, (V)3.0d, (V)2.0d}, new Results((V)20.0d))));
V v_353 = values_352[0];
var values_354 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_351, (V)3.0d, (V)3.0d}, new Results((V)20.0d))));
V v_355 = values_354[0];
var values_356 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_351, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_357 = values_356[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_353, (V)"hp") < (V)20.0d), () => (V)(R.Get(v_355, (V)"hp") < (V)20.0d)), () => (V)(R.Call(v_20, R.Join(new V[] {v_357}, new Results((V)"DamageBatchResolved"))).First >= (V)1.0d)))));
var values_358 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"recurve"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)2.0d)))));
v_351 = values_358[0];

var values_359 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_351, (V)8.0d, (V)2.0d}, new Results((V)20.0d))));
V v_360 = values_359[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Call(R.Get(v_13, (V)"preview"), R.Join(new V[] {v_351, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))).First, (V)"cells")) == (V)6.0d))));
R.Call(v_76, R.Join(new V[] {v_351, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_360, (V)"hp") < (V)20.0d))));
var values_361 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"expanding_sigil"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)3.0d)))));
v_351 = values_361[0];

var values_362 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_351, (V)6.0d, (V)2.0d}, new Results((V)20.0d))));
v_360 = values_362[0];
R.Call(v_76, R.Join(new V[] {v_351, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_360, (V)"hp") < (V)20.0d))));
var values_363 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"alchemical_flip"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)3.0d)))));
v_351 = values_363[0];
var values_364 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_351, (V)3.0d, (V)2.0d}, new Results((V)20.0d))));
v_353 = values_364[0];

var values_365 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_351, (V)3.0d, (V)3.0d}, new Results((V)20.0d))));
v_355 = values_365[0];
var values_366 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"burning", R.Table(new Entry((V)"power", (V)1.0d), new Entry((V)"remaining", (V)3.0d), new Entry((V)"interval", (V)100.0d), new Entry((V)"next_due", (V)100.0d))))));
var obj_367 = v_353;
V key_368 = (V)"statuses";
R.Set(obj_367, key_368, values_366[0]);
var values_369 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"burning", R.Table(new Entry((V)"power", (V)1.0d), new Entry((V)"remaining", (V)3.0d), new Entry((V)"interval", (V)100.0d), new Entry((V)"next_due", (V)100.0d))))));
var obj_370 = v_355;
V key_371 = (V)"statuses";
R.Set(obj_370, key_371, values_369[0]);
R.Call(v_76, R.Join(new V[] {v_351, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_353, (V)"hp") < (V)20.0d), () => (V)(R.Get(v_355, (V)"hp") < (V)20.0d)))));
var values_372 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"earthshaker"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)4.0d)))));
v_351 = values_372[0];
var values_373 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_351, (V)3.0d, (V)2.0d}, new Results((V)20.0d))));
v_353 = values_373[0];

var values_374 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_351, (V)3.0d, (V)3.0d}, new Results((V)20.0d))));
v_355 = values_374[0];
R.Call(v_76, R.Join(new V[] {v_351, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_353, (V)"hp") < (V)20.0d), () => (V)(R.Get(v_355, (V)"hp") < (V)20.0d)))));
var values_375 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"battle_march"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)4.0d)))));
v_351 = values_375[0];

var values_376 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_351, (V)5.0d, (V)2.0d}, new Results((V)30.0d))));
v_360 = values_376[0];
var values_377 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_351, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
v_357 = values_377[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_20, R.Join(new V[] {v_357, (V)"BoonActivated"}, new Results((V)"battle_march"))).First >= (V)1.0d))));
var values_378 = R.Join(new V[] {}, new Results(R.Get(v_360, (V)"hp")));
V v_379 = values_378[0];
var values_380 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_351, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
v_357 = values_380[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_357, (V)"BoonActivated"}, new Results((V)"battle_march"))).First >= (V)1.0d), () => (V)((V)(v_379 - R.Get(v_360, (V)"hp")) > R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_351))).First, (V)"damage"))))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_351))).First}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_351)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new class transformations preserve blocked terrain and rejected Dash rules"}, new Results(new V((RuleFunction)(args_381 => {

var values_382 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"splitflight"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)2.0d)))));
V v_383 = values_382[0];
R.Call(v_48, R.Join(new V[] {v_383, (V)"#", (V)3.0d}, new Results((V)3.0d)));
R.Call(v_62, R.Join(new V[] {v_383, (V)3.0d, (V)2.0d}, new Results((V)20.0d)));
var values_384 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_383, (V)2.0d, (V)3.0d}, new Results((V)20.0d))));
V v_385 = values_384[0];
R.Call(v_76, R.Join(new V[] {v_383, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_385, (V)"hp") == (V)20.0d)}, new Results((V)"Splitflight crossed a blocked diagonal corner")));
var values_386 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"recurve"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)2.0d)))));
v_383 = values_386[0];

R.Call(v_48, R.Join(new V[] {v_383, (V)"B", (V)8.0d}, new Results((V)2.0d)));
var values_387 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"preview"), R.Join(new V[] {v_383, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_388 = values_387[0];
foreach (var row_389 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_388, (V)"cells"))))))) {
V v_390 = row_389[0];
V v_391 = row_389[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_391, (V)"x") != (V)8.0d)}, new Results((V)"Recurve crossed barricade")));
}
var values_392 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"expanding_sigil"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)3.0d)))));
v_383 = values_392[0];

R.Call(v_48, R.Join(new V[] {v_383, (V)"#", (V)4.0d}, new Results((V)2.0d)));
var values_393 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"preview"), R.Join(new V[] {v_383, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
v_388 = values_393[0];
foreach (var row_394 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_388, (V)"cells"))))))) {
V v_395 = row_394[0];
V v_396 = row_394[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_396, (V)"x") != (V)6.0d)}, new Results((V)"Sigil expanded through an unreachable center")));
}
var values_397 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"alchemical_flip"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)3.0d)))));
v_383 = values_397[0];

R.Call(v_48, R.Join(new V[] {v_383, (V)"#", (V)3.0d}, new Results((V)3.0d)));
var values_398 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_383, (V)3.0d, (V)2.0d}, new Results((V)20.0d))));
V v_399 = values_398[0];

var values_400 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {v_383, (V)2.0d, (V)3.0d}, new Results((V)20.0d))));
v_385 = values_400[0];
var values_401 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"burning", R.Table(new Entry((V)"power", (V)1.0d), new Entry((V)"remaining", (V)3.0d), new Entry((V)"interval", (V)100.0d), new Entry((V)"next_due", (V)100.0d))))));
var obj_402 = v_399;
V key_403 = (V)"statuses";
R.Set(obj_402, key_403, values_401[0]);
var values_404 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"burning", R.Table(new Entry((V)"power", (V)1.0d), new Entry((V)"remaining", (V)3.0d), new Entry((V)"interval", (V)100.0d), new Entry((V)"next_due", (V)100.0d))))));
var obj_405 = v_385;
V key_406 = (V)"statuses";
R.Set(obj_405, key_406, values_404[0]);
R.Call(v_76, R.Join(new V[] {v_383, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_385, (V)"hp") == (V)20.0d)}, new Results((V)"Alchemical Flip crossed a blocked corner")));
var values_407 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"earthshaker"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)4.0d)))));
v_383 = values_407[0];

R.Call(v_48, R.Join(new V[] {v_383, (V)"B", (V)3.0d}, new Results((V)3.0d)));
var values_408 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"preview"), R.Join(new V[] {v_383, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
v_388 = values_408[0];
foreach (var row_409 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_388, (V)"cells"))))))) {
V v_410 = row_409[0];
V v_411 = row_409[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.And((V)(R.Get(v_411, (V)"x") == (V)3.0d), () => (V)(R.Get(v_411, (V)"y") == (V)3.0d))))}, new Results((V)"shockwave crossed barricade")));
}
var values_412 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {(V)"battle_march"}, new Results(R.Get(R.Get(v_19, (V)"order"), (V)4.0d)))));
v_383 = values_412[0];

R.Call(v_48, R.Join(new V[] {v_383, (V)"B", (V)3.0d}, new Results((V)2.0d)));
var values_413 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_383))));
V v_414 = values_413[0];
var values_415 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"submit"), R.Join(new V[] {v_383}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_416 = values_415[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_416)), () => (V)(!R.Truth(R.Get(R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_383))).First, (V)"boon_memory")))))));
R.Call(v_3, R.Join(new V[] {v_414}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_383)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ordinary paired-floor New Run earns and activates a purchased new boon"}, new Results(new V((RuleFunction)(args_417 => {

var values_418 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.expansion_boon_acceptance"))).First, (V)"run"), Results.Empty));
V v_419 = values_418[0];
V v_420 = values_418[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_420, (V)"seed") == (V)50.0d), () => (V)(R.Get(v_420, (V)"actions") > (V)0.0d)), () => (V)(R.Get(v_420, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_420, (V)"clock") > (V)0.0d)), () => (V)(R.Get(v_420, (V)"rarity") == (V)"common")), () => (V)(R.Get(v_420, (V)"movement_activations") >= (V)1.0d)), () => (V)(R.Get(v_420, (V)"hit_activations") >= (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_419, (V)"state"), (V)"floor_order"), () => (V)((V)R.Length(R.Get(R.Get(v_419, (V)"state"), (V)"floor_order")) == (V)2.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_419, (V)"state")))).First}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_419, (V)"state"))))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"movement boon on a stair commits on the destination without waking old-floor effects"}, new Results(new V((RuleFunction)(args_421 => {

var values_422 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_423 = values_422[0];
var values_424 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_425 = values_424[0];
R.Call(R.Get(v_13, (V)"set_boon_stacks"), R.Join(new V[] {v_425, R.Get(v_425, (V)"player_id"), (V)"core:boon/rhythm_of_steel", (V)"common"}, new Results((V)1.0d)));
var values_426 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_425, (V)"generated_map"), (V)"placements"), (V)"stairs"), (V)1.0d)));
V v_427 = values_426[0];
var values_428 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"player"), R.Join(new V[] {}, new Results(v_425))));
V v_429 = values_428[0];
var values_430 = Results.Empty;
V v_431 = values_430[0];
V v_432 = values_430[1];
foreach (var row_433 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_434 = row_433[0];
V v_435 = row_433[1];
var values_436 = R.Join(new V[] {(V)(R.Get(v_427, (V)"x") - R.Get(v_435, (V)1.0d))}, new Results((V)(R.Get(v_427, (V)"y") - R.Get(v_435, (V)2.0d))));
V v_437 = values_436[0];
V v_438 = values_436[1];
var values_439 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"walkable"), R.Join(new V[] {R.Get(v_425, (V)"generated_map"), v_437}, new Results(v_438))));
V v_440 = values_439[0];
foreach (var row_441 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_425, (V)"entities"))))))) {
V v_442 = row_441[0];
V v_443 = row_441[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_443, (V)"id") != R.Get(v_429, (V)"id")), () => R.Get(v_443, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_443, (V)"position"), (V)"floor_id") == R.Get(v_425, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_443, (V)"position"), (V)"x") == v_437)), () => (V)(R.Get(R.Get(v_443, (V)"position"), (V)"y") == v_438)))) {
var values_444 = R.Join(new V[] {}, new Results((V)false));
v_440 = values_444[0];
}
}
if (R.Truth(v_440)) {
var values_445 = R.Join(new V[] {v_437}, new Results(v_438));
var obj_446 = R.Get(v_429, (V)"position");
V key_447 = (V)"x";
var obj_448 = R.Get(v_429, (V)"position");
V key_449 = (V)"y";
R.Set(obj_446, key_447, values_445[0]);
R.Set(obj_448, key_449, values_445[1]);
var values_450 = R.Join(new V[] {R.Get(v_435, (V)1.0d)}, new Results(R.Get(v_435, (V)2.0d)));
v_431 = values_450[0];
v_432 = values_450[1];
break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_431}, new Results((V)"no legal stair approach")));
R.Call(R.Get(v_423, (V)"recompute"), R.Join(new V[] {}, new Results(v_425)));
var values_451 = R.Join(new V[] {}, new Results(R.Get(v_425, (V)"active_floor_id")));
V v_452 = values_451[0];
var values_453 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_425, (V)"move", v_431}, new Results(v_432))));
V v_454 = values_453[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_20, R.Join(new V[] {v_454}, new Results((V)"FloorChanged"))).First == (V)1.0d), () => (V)(R.Call(v_20, R.Join(new V[] {v_454, (V)"BoonActivated"}, new Results((V)"rhythm_of_steel"))).First >= (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_425, (V)"active_floor_id") != v_452), () => (V)(R.Get(R.Get(R.Get(v_429, (V)"boon_memory"), (V)"core:boon/rhythm_of_steel"), (V)"charges") == (V)1.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_425))).First}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"snapshot"), R.Join(new V[] {}, new Results(v_425)))))))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
