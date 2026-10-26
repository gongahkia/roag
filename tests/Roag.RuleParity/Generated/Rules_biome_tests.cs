// Static C# port of tests/biome_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 1e4548c1990ff8cac1bbefe2cdaceec37108a1394462156a5210070dc397c1ff
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_biome_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rooms"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_21 = values_20[0];
var values_22 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_23 = values_22[0];
V v_24 = V.Nil;
v_24 = new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
V v_27 = args_25[1];
var values_28 = R.Join(new V[] {}, new Results((V)0.0d));
V v_29 = values_28[0];
foreach (var row_30 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_26)))))) {
V v_31 = row_30[0];
V v_32 = row_30[1];
if (R.Truth((V)(R.Get(v_32, (V)"kind") == v_27))) {
var values_33 = R.Join(new V[] {}, new Results((V)(v_29 + (V)1.0d)));
v_29 = values_33[0];
}
}
return R.Join(new V[] {}, new Results(v_29));
return Results.Empty;
}));
V v_34 = V.Nil;
v_34 = new V((RuleFunction)(args_35 => {
V v_36 = args_35[0];
V v_37 = args_35[1];
return R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_36, (V)"x") - R.Get(v_37, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_36, (V)"y") - R.Get(v_37, (V)"y"))))).First) == (V)1.0d)));
return Results.Empty;
}));
V v_38 = V.Nil;
v_38 = new V((RuleFunction)(args_39 => {
V v_40 = args_39[0];
V v_41 = args_39[1];
var values_42 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_40))));
V v_43 = values_42[0];
foreach (var row_44 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_45 = row_44[0];
V v_46 = row_44[1];
var values_47 = R.Join(new V[] {(V)(R.Get(R.Get(v_41, (V)"position"), (V)"x") + R.Get(v_46, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_41, (V)"position"), (V)"y") + R.Get(v_46, (V)2.0d))));
V v_48 = values_47[0];
V v_49 = values_47[1];
var values_50 = R.Join(new V[] {}, new Results((V)false));
V v_51 = values_50[0];
foreach (var row_52 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_40, (V)"entities"))))))) {
V v_53 = row_52[0];
V v_54 = row_52[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_54, (V)"id") != R.Get(v_43, (V)"id")), () => R.Get(v_54, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_54, (V)"position"), (V)"x") == v_48)), () => (V)(R.Get(R.Get(v_54, (V)"position"), (V)"y") == v_49)))) {
var values_55 = R.Join(new V[] {}, new Results((V)true));
v_51 = values_55[0];
}
}
if (R.Truth(R.And(R.Call(R.Get(v_15, (V)"walkable"), R.Join(new V[] {R.Get(v_40, (V)"generated_map"), v_48}, new Results(v_49))).First, () => (V)(!R.Truth(v_51))))) {
var values_56 = R.Join(new V[] {v_48}, new Results(v_49));
var obj_57 = R.Get(v_43, (V)"position");
V key_58 = (V)"x";
var obj_59 = R.Get(v_43, (V)"position");
V key_60 = (V)"y";
R.Set(obj_57, key_58, values_56[0]);
R.Set(obj_59, key_60, values_56[1]);
R.Call(R.Get(v_19, (V)"recompute"), R.Join(new V[] {}, new Results(v_40)));
return R.Join(new V[] {(-R.Get(v_46, (V)1.0d))}, new Results((-R.Get(v_46, (V)2.0d))));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no object approach")));
return Results.Empty;
}));
V v_61 = V.Nil;
v_61 = new V((RuleFunction)(args_62 => {
V v_63 = args_62[0];
V v_64 = args_62[1];
foreach (var row_65 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_63, (V)"entities"))))))) {
V v_66 = row_65[0];
V v_67 = row_65[1];
if (R.Truth((V)(R.Get(v_67, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_63}, new Results(R.Get(v_67, (V)"id"))));
}
}
var values_68 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_63}, new Results(R.Get(R.Get(v_63, (V)"stage"), (V)"elite_id")))))));
V v_69 = values_68[0];
var values_70 = R.Join(new V[] {}, R.Call(v_38, R.Join(new V[] {v_63}, new Results(v_69))));
V v_71 = values_70[0];
V v_72 = values_70[1];
var values_73 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_69, (V)"hp") + R.Get(v_69, (V)"defense")) + (V)10.0d)));
var obj_74 = R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_63))).First;
V key_75 = (V)"damage";
R.Set(obj_74, key_75, values_73[0]);
var values_76 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_63}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_71), new Entry((V)"dy", v_72))))));
V v_77 = values_76[0];
V v_78 = values_76[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_77}, new Results(v_78)));
var values_79 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_63}, new Results(R.Get(R.Get(v_63, (V)"stage"), (V)"exit_id")))))));
V v_80 = values_79[0];
R.Call(v_38, R.Join(new V[] {v_63}, new Results(v_80)));
var values_81 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"route_options"), R.Join(new V[] {v_63}, new Results(R.Get(v_80, (V)"id")))))));
V v_82 = values_81[0];
var values_83 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_82, (V)"routes"), v_64)));
V v_84 = values_83[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_84)));
var values_85 = R.Join(new V[] {}, new Results(R.Get(v_63, (V)"clock")));
V v_86 = values_85[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(R.Get(v_11, (V)"route_options"), R.Join(new V[] {v_63}, new Results(R.Get(v_80, (V)"id")))).First, (V)"routes"), v_64), (V)"id") == R.Get(v_84, (V)"id")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_63, (V)"clock") == v_86))));
var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_63}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_80, (V)"id")), new Entry((V)"route_index", v_64))))));
v_77 = values_87[0];
v_78 = values_87[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_77}, new Results(v_78)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_63, (V)"clock") == (V)0.0d))));
return R.Join(new V[] {}, new Results(v_84));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"seven biome registries select transformable authored rooms"}, new Results(new V((RuleFunction)(args_88 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_5, (V)"validate"), Results.Empty).First, () => (V)((V)R.Length(R.Get(v_5, (V)"order")) == (V)7.0d)))));
var values_89 = R.Join(new V[] {}, new Results(R.Table()));
V v_90 = values_89[0];
foreach (var row_91 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_5, (V)"order"))))))) {
V v_92 = row_91[0];
V v_93 = row_91[1];
var values_94 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"get"), R.Join(new V[] {}, new Results(v_93))))));
V v_95 = values_94[0];
var values_96 = R.Join(new V[] {}, new Results((V)0.0d));
V v_97 = values_96[0];
var values_98 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"start"), new Entry((V)2, (V)"combat"), new Entry((V)3, (V)"treasure"), new Entry((V)4, (V)"landmark"), new Entry((V)5, (V)"elite"))));
V v_99 = values_98[0];
foreach (var row_100 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_99)))))) {
V v_101 = row_100[0];
V v_102 = row_100[1];
var values_103 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"for_role"), R.Join(new V[] {v_102}, new Results(v_93))));
V v_104 = values_103[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_104) > (V)0.0d))));
foreach (var row_105 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_104)))))) {
V v_106 = row_105[0];
V v_107 = row_105[1];
var values_108 = R.Join(new V[] {}, new Results((V)(v_97 + (V)R.Length(R.Get(v_107, (V)"editable")))));
v_97 = values_108[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(!R.Truth(R.Get(v_90, R.Get(v_107, (V)"id")))), () => (V)(R.Get(v_90, R.Get(v_107, (V)"id")) == v_93)))));
var values_109 = R.Join(new V[] {}, new Results(v_93));
var obj_110 = v_90;
V key_111 = R.Get(v_107, (V)"id");
R.Set(obj_110, key_111, values_109[0]);
var values_112 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"transform"), R.Join(new V[] {v_107, (V)1.0d}, new Results((V)true))));
V v_113 = values_112[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_113, (V)"width") == (V)R.Length(R.Get(v_107, (V)"rows"))), () => (V)(R.Get(v_113, (V)"height") == (V)R.Length(R.Get(R.Get(v_107, (V)"rows"), (V)1.0d)))), () => (V)((V)R.Length(R.Get(v_113, (V)"sockets")) == (V)4.0d)), () => (V)((V)R.Length(R.Get(R.Get(v_113, (V)"markers"), (V)"secret_anchor")) > (V)0.0d)))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_97 > (V)0.0d)}, new Results((V)"biome lost its editable variation masks")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_7, (V)"for_role"), R.Join(new V[] {(V)"combat"}, new Results(R.Get(R.Get(v_5, (V)"order"), (V)2.0d)))).First) > (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Aqueduct water is static blocked geometry but transparent sight"}, new Results(new V((RuleFunction)(args_114 => {

var values_115 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"generate"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"biome_id", R.Get(R.Get(v_5, (V)"order"), (V)3.0d)))))));
V v_116 = values_115[0];
var values_117 = R.Join(new V[] {}, new Results((V)false));
V v_118 = values_117[0];
foreach (var row_119 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_116, (V)"rows"))))))) {
V v_120 = row_119[0];
V v_121 = row_119[1];
var values_122 = R.Join(new V[] {}, R.Invoke(v_121, "find", R.Join(new V[] {(V)"~", (V)1.0d}, new Results((V)true))));
V v_123 = values_122[0];
if (R.Truth(v_123)) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_15, (V)"walkable"), R.Join(new V[] {v_116, v_123}, new Results(v_120))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_15, (V)"blocks_vision"), R.Join(new V[] {v_116, v_123}, new Results(v_120))).First))), () => R.Call(R.Get(v_15, (V)"projectile_passable"), R.Join(new V[] {v_116, v_123}, new Results(v_120))).First), () => (V)(R.Invoke(R.Get(R.Get(v_116, (V)"visual_rows"), v_120), "sub", R.Join(new V[] {v_123}, new Results(v_123))).First == (V)"q")))));
var values_124 = R.Join(new V[] {}, new Results((V)true));
v_118 = values_124[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_118}, new Results((V)"Aqueduct lacks visible channel water")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"four 100-seed floor corpora retain distinct playable geometry"}, new Results(new V((RuleFunction)(args_125 => {

foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_5, (V)"order"))))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
var values_129 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"get"), R.Join(new V[] {}, new Results(v_128))));
V v_130 = values_129[0];
var values_131 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("os"), (V)"clock"), Results.Empty));
V v_132 = values_131[0];
var values_133 = R.Join(new V[] {(V)0.0d, (V)0.0d, (V)0.0d, (V)0.0d, (V)0.0d}, new Results((V)0.0d));
V v_134 = values_133[0];
V v_135 = values_133[1];
V v_136 = values_133[2];
V v_137 = values_133[3];
V v_138 = values_133[4];
V v_139 = values_133[5];
{ double end_142 = R.Num((V)100.0d), step_143 = R.Num((V)1.0d);
for (double i_141 = R.Num((V)1.0d); step_143 > 0 ? i_141 <= end_142 : i_141 >= end_142; i_141 += step_143) {
V v_140 = i_141;
var values_144 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"generate"), R.Join(new V[] {v_140}, new Results(R.Table(new Entry((V)"biome_id", v_128))))));
V v_145 = values_144[0];
var values_146 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"generate"), R.Join(new V[] {v_140}, new Results(R.Table(new Entry((V)"biome_id", v_128))))));
V v_147 = values_146[0];
R.Call(v_3, R.Join(new V[] {v_145}, new Results(v_147)));
var values_148 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"validate"), R.Join(new V[] {}, new Results(v_145))));
V v_149 = values_148[0];
V v_150 = values_148[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_149}, new Results(R.And(R.Get(v_150, (V)1.0d), () => R.Get(R.Get(v_150, (V)1.0d), (V)"kind")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_145, (V)"biome_id") == v_128), () => (V)(R.Get(v_145, (V)"id") == R.Get(v_130, (V)"map_id"))), () => (V)((V)R.Length(R.Get(v_145, (V)"rooms")) >= (V)8.0d)), () => (V)((V)R.Length(R.Get(v_145, (V)"rooms")) <= (V)12.0d)), () => (V)((V)R.Length(R.Get(R.Get(v_145, (V)"placements"), (V)"chests")) == (V)5.0d)), () => (V)((V)R.Length(R.Get(R.Get(v_145, (V)"placements"), (V)"secrets")) == (V)2.0d)), () => (V)((V)R.Length(R.Get(R.Get(v_145, (V)"placements"), (V)"doors")) == (V)1.0d)), () => R.Get(v_145, (V)"elite")))));
var values_151 = R.Join(new V[] {}, new Results((V)false));
V v_152 = values_151[0];
foreach (var row_153 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_145, (V)"placements"), (V)"enemies"))))))) {
V v_154 = row_153[0];
V v_155 = row_153[1];
if (R.Truth((V)(R.Get(v_155, (V)"definition_id") == R.Get(R.Get(v_130, (V)"enemy_ids"), (V)1.0d)))) {
var values_156 = R.Join(new V[] {}, new Results((V)true));
v_152 = values_156[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_152}, new Results((V)"biome-specific enemy absent")));
foreach (var row_157 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_145, (V)"rooms"))))))) {
V v_158 = row_157[0];
V v_159 = row_157[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(v_159, (V)"template_id"), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)R.Length(R.Get(v_130, (V)"room_prefix"))))).First == R.Get(v_130, (V)"room_prefix")))));
}
if (R.Truth((V)(R.Get(v_130, (V)"strategy") == (V)"courtyards"))) {
var values_160 = R.Join(new V[] {}, new Results((V)false));
V v_161 = values_160[0];
foreach (var row_162 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_145, (V)"graph"), (V)"critical"))))))) {
V v_163 = row_162[0];
V v_164 = row_162[1];
if (R.Truth((V)(v_164 == (V)6.0d))) {
var values_165 = R.Join(new V[] {}, new Results((V)true));
v_161 = values_165[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_161}, new Results((V)"courtyard hub missing from main route")));
foreach (var row_166 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_145, (V)"rooms"))))))) {
V v_167 = row_166[0];
V v_168 = row_166[1];
if (R.Truth((V)(R.Get(v_168, (V)"id") == (V)6.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_168, (V)"template_id") == R.Get(v_130, (V)"hub_template_id")))));
}
}
}
foreach (var row_169 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_145, (V)"corridors"))))))) {
V v_170 = row_169[0];
V v_171 = row_169[1];
if (R.Truth(R.Get(v_171, (V)"parallel"))) {
var values_172 = R.Join(new V[] {}, new Results((V)(v_137 + (V)1.0d)));
v_137 = values_172[0];
}
if (R.Truth(R.Get(v_171, (V)"widened"))) {
var values_173 = R.Join(new V[] {}, new Results((V)(v_138 + (V)1.0d)));
v_138 = values_173[0];
}
var values_174 = R.Join(new V[] {}, new Results((V)0.0d));
V v_175 = values_174[0];
{ double end_178 = R.Num((V)R.Length(R.Get(v_171, (V)"cells"))), step_179 = R.Num((V)1.0d);
for (double i_177 = R.Num((V)3.0d); step_179 > 0 ? i_177 <= end_178 : i_177 >= end_178; i_177 += step_179) {
V v_176 = i_177;
var values_180 = R.Join(new V[] {R.Get(R.Get(v_171, (V)"cells"), (V)(v_176 - (V)2.0d)), R.Get(R.Get(v_171, (V)"cells"), (V)(v_176 - (V)1.0d))}, new Results(R.Get(R.Get(v_171, (V)"cells"), v_176)));
V v_181 = values_180[0];
V v_182 = values_180[1];
V v_183 = values_180[2];
if (R.Truth(R.Or((V)((V)(R.Get(v_182, (V)"x") - R.Get(v_181, (V)"x")) != (V)(R.Get(v_183, (V)"x") - R.Get(v_182, (V)"x"))), () => (V)((V)(R.Get(v_182, (V)"y") - R.Get(v_181, (V)"y")) != (V)(R.Get(v_183, (V)"y") - R.Get(v_182, (V)"y")))))) {
var values_184 = R.Join(new V[] {}, new Results((V)(v_175 + (V)1.0d)));
v_175 = values_184[0];
}
}
}
var values_185 = R.Join(new V[] {}, new Results((V)(v_139 + v_175)));
v_139 = values_185[0];
}
var values_186 = R.Join(new V[] {}, new Results((V)(v_134 + R.Or(R.And((V)(R.Get(v_145, (V)"attempt") > (V)0.0d), () => (V)1.0d), () => (V)0.0d))));
v_134 = values_186[0];
var values_187 = R.Join(new V[] {}, new Results((V)(v_135 + (V)R.Length(R.Get(v_145, (V)"repairs")))));
v_135 = values_187[0];
var values_188 = R.Join(new V[] {}, new Results((V)(v_136 + R.Or(R.And(R.Get(R.Get(v_145, (V)"diagnostics"), (V)"fallback"), () => (V)1.0d), () => (V)0.0d))));
v_136 = values_188[0];
}
}
if (R.Truth((V)(R.Get(v_130, (V)"strategy") == (V)"canals"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_137 >= (V)100.0d))));
}
if (R.Truth((V)(R.Get(v_130, (V)"strategy") == (V)"courtyards"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_138 >= (V)100.0d))));
}
if (R.Truth((V)(R.Get(v_130, (V)"strategy") == (V)"warrens"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_139 >= (V)200.0d))));
}
R.Call(rt.Global("print"), R.Join(new V[] {}, R.Invoke((V)"BIOME CORPUS %s: 100 valid, %d retries, %d repairs, %d fallback, %d bends, %d parallel, %d widened, %.3fs CPU", "format", R.Join(new V[] {v_128, v_134, v_135, v_136, v_139, v_137, v_138}, new Results((V)(R.Call(R.Get(rt.Global("os"), (V)"clock"), Results.Empty).First - v_132))))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"biome faults repair locally or enter validated protected fallback"}, new Results(new V((RuleFunction)(args_189 => {

foreach (var row_190 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_5, (V)"order"))))))) {
V v_191 = row_190[0];
V v_192 = row_190[1];
var values_193 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"generate"), R.Join(new V[] {(V)11.0d}, new Results(R.Table(new Entry((V)"biome_id", v_192), new Entry((V)"fault", (V)"editable"))))));
V v_194 = values_193[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Call(R.Get(v_9, (V)"validate"), R.Join(new V[] {}, new Results(v_194))).First, () => (V)((V)R.Length(R.Get(v_194, (V)"repairs")) >= (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_194, (V)"repairs"), (V)1.0d), (V)"kind") == (V)"restore_corridor")))));
var values_195 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"generate"), R.Join(new V[] {(V)11.0d}, new Results(R.Table(new Entry((V)"biome_id", v_192), new Entry((V)"fault", (V)"protected"), new Entry((V)"retry_limit", (V)1.0d))))));
V v_196 = values_195[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(R.Get(v_196, (V)"diagnostics"), (V)"fallback"), () => R.Call(R.Get(v_9, (V)"validate"), R.Join(new V[] {}, new Results(v_196))).First), () => (V)((V)R.Length(R.Get(R.Get(v_196, (V)"diagnostics"), (V)"attempts")) == (V)2.0d)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"100 runtime stages per biome place one Warden, free chest and exit"}, new Results(new V((RuleFunction)(args_197 => {

{ double end_200 = R.Num((V)R.Length(R.Get(v_5, (V)"order"))), step_201 = R.Num((V)1.0d);
for (double i_199 = R.Num((V)1.0d); step_201 > 0 ? i_199 <= end_200 : i_199 >= end_200; i_199 += step_201) {
V v_198 = i_199;
var values_202 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), v_198)));
V v_203 = values_202[0];
var values_204 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_198 == (V)1.0d), () => (V)1.0d), () => (V)2.0d)));
V v_205 = values_204[0];
var values_206 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_205 == (V)1.0d), () => R.Table()), () => R.Table(new Entry(1, new Results(R.Invoke(v_203, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First))))));
V v_207 = values_206[0];
{ double end_210 = R.Num((V)100.0d), step_211 = R.Num((V)1.0d);
for (double i_209 = R.Num((V)1.0d); step_211 > 0 ? i_209 <= end_210 : i_209 >= end_210; i_209 += step_211) {
V v_208 = i_209;
var values_212 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {v_208}, new Results(R.Table(new Entry((V)"stage_options", R.Table(new Entry((V)"index", v_205), new Entry((V)"biome_id", v_203), new Entry((V)"route_history", v_207))))))));
V v_213 = values_212[0];
var values_214 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_213}, new Results(R.Get(R.Get(v_213, (V)"stage"), (V)"elite_id")))))));
V v_215 = values_214[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_215, (V)"position"), (V)"x") == R.Get(R.Get(R.Get(v_213, (V)"generated_map"), (V)"elite"), (V)"x")), () => (V)(R.Get(R.Get(v_215, (V)"position"), (V)"y") == R.Get(R.Get(R.Get(v_213, (V)"generated_map"), (V)"elite"), (V)"y"))))));
foreach (var row_216 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_213, (V)"entities"))))))) {
V v_217 = row_216[0];
V v_218 = row_216[1];
if (R.Truth((V)(R.Get(v_218, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_213}, new Results(R.Get(v_218, (V)"id"))));
}
}
var values_219 = R.Join(new V[] {}, R.Call(v_38, R.Join(new V[] {v_213}, new Results(v_215))));
V v_220 = values_219[0];
V v_221 = values_219[1];
var values_222 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_215, (V)"hp") + R.Get(v_215, (V)"defense")) + (V)1.0d)));
var obj_223 = R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_213))).First;
V key_224 = (V)"damage";
R.Set(obj_223, key_224, values_222[0]);
var values_225 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_213}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_220), new Entry((V)"dy", v_221))))));
V v_226 = values_225[0];
V v_227 = values_225[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_226}, new Results(v_227)));
var values_228 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_213}, new Results(R.Get(R.Get(v_213, (V)"stage"), (V)"elite_reward_id")))))));
V v_229 = values_228[0];
var values_230 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_213}, new Results(R.Get(R.Get(v_213, (V)"stage"), (V)"exit_id")))))));
V v_231 = values_230[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Get(v_229, (V)"price") == (V)0.0d), () => (V)((V)R.Length(R.Get(v_229, (V)"offers")) == (V)3.0d)), () => (V)(R.Get(v_231, (V)"controller") == (V)"exit")), () => (V)(R.Get(v_231, (V)"id") != R.Get(v_229, (V)"id"))), () => (V)(R.Get(R.Get(v_213, (V)"stage"), (V)"phase") == (V)"extraction")))));
var values_232 = R.Join(new V[] {}, new Results((V)false));
V v_233 = values_232[0];
foreach (var row_234 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_235 = row_234[0];
V v_236 = row_234[1];
if (R.Truth(R.Call(R.Get(v_15, (V)"walkable"), R.Join(new V[] {R.Get(v_213, (V)"generated_map"), (V)(R.Get(R.Get(v_231, (V)"position"), (V)"x") + R.Get(v_236, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_231, (V)"position"), (V)"y") + R.Get(v_236, (V)2.0d))))).First)) {
var values_237 = R.Join(new V[] {}, new Results((V)true));
v_233 = values_237[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_233}, new Results((V)"elite exit has no cardinal approach")));
}
}
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new biomes retain extraction deadlines and one reachable pursuer"}, new Results(new V((RuleFunction)(args_238 => {

{ double end_241 = R.Num((V)4.0d), step_242 = R.Num((V)1.0d);
for (double i_240 = R.Num((V)2.0d); step_242 > 0 ? i_240 <= end_241 : i_240 >= end_241; i_240 += step_242) {
V v_239 = i_240;
var values_243 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), v_239)));
V v_244 = values_243[0];
var values_245 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)7.0d}, new Results(R.Table(new Entry((V)"stage_options", R.Table(new Entry((V)"index", (V)2.0d), new Entry((V)"biome_id", v_244), new Entry((V)"route_history", R.Table(new Entry(1, new Results(R.Invoke(v_244, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First)))))))))));
V v_246 = values_245[0];
foreach (var row_247 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_246, (V)"entities"))))))) {
V v_248 = row_247[0];
V v_249 = row_247[1];
if (R.Truth((V)(R.Get(v_249, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_246}, new Results(R.Get(v_249, (V)"id"))));
}
}
var values_250 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_246}, new Results(R.Get(R.Get(v_246, (V)"stage"), (V)"elite_id")))))));
V v_251 = values_250[0];
var values_252 = R.Join(new V[] {}, R.Call(v_38, R.Join(new V[] {v_246}, new Results(v_251))));
V v_253 = values_252[0];
V v_254 = values_252[1];
var values_255 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_246))));
V v_256 = values_255[0];
var values_257 = R.Join(new V[] {(V)1000.0d, (V)1000.0d}, new Results((V)100.0d));
var obj_258 = v_256;
V key_259 = (V)"hp";
var obj_260 = v_256;
V key_261 = (V)"max_hp";
var obj_262 = v_256;
V key_263 = (V)"defense";
R.Set(obj_258, key_259, values_257[0]);
R.Set(obj_260, key_261, values_257[1]);
R.Set(obj_262, key_263, values_257[2]);
var values_264 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_251, (V)"hp") + R.Get(v_251, (V)"defense")) + (V)1.0d)));
var obj_265 = v_256;
V key_266 = (V)"damage";
R.Set(obj_265, key_266, values_264[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_246}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_253), new Entry((V)"dy", v_254)))))));
var values_267 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_246, (V)"stage"), (V)"pressure_count")));
V v_268 = values_267[0];
var values_269 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_246, (V)"stage"), (V)"deadline")));
V v_270 = values_269[0];
while (R.Truth((V)(R.Get(v_246, (V)"clock") < v_270))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_246}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
var values_271 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_246}, new Results(R.Get(R.Get(v_246, (V)"stage"), (V)"pursuer_id")))))));
V v_272 = values_271[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_246, (V)"stage"), (V)"phase") == (V)"pursuit"), () => R.Get(v_272, (V)"invulnerable")), () => (V)(R.Get(R.Get(v_246, (V)"stage"), (V)"pressure_count") == v_268)), () => R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_246}, new Results(R.Get(R.Get(v_246, (V)"stage"), (V)"exit_id")))).First))));
var values_273 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_246))))));
V v_274 = values_273[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_274, (V)"stage"), (V)"deadline") == v_270), () => (V)(R.Get(R.Get(v_274, (V)"stage"), (V)"pursuer_id") == R.Get(R.Get(v_246, (V)"stage"), (V)"pursuer_id"))))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new enemies choose three distinct locked shapes and use shared damage"}, new Results(new V((RuleFunction)(args_275 => {

var values_276 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)"core:enemy/spore_herald"), new Entry((V)"x", (V)10.0d), new Entry((V)"y", (V)9.0d), new Entry((V)"ability", (V)"core:ability/spore_burst"), new Entry((V)"n", (V)3.0d))), new Entry((V)2, R.Table(new Entry((V)"id", (V)"core:enemy/canal_lancer"), new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)9.0d), new Entry((V)"ability", (V)"core:ability/canal_thrust"), new Entry((V)"n", (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)"id", (V)"core:enemy/cinder_bellkeeper"), new Entry((V)"x", (V)10.0d), new Entry((V)"y", (V)9.0d), new Entry((V)"ability", (V)"core:ability/bell_cross"), new Entry((V)"n", (V)5.0d))))));
V v_277 = values_276[0];
foreach (var row_278 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_277)))))) {
V v_279 = row_278[0];
V v_280 = row_278[1];
var values_281 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {}, new Results((V)8.0d))));
V v_282 = values_281[0];
var values_283 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_282))));
V v_284 = values_283[0];
var values_285 = R.Join(new V[] {(V)7.0d}, new Results((V)9.0d));
var obj_286 = R.Get(v_284, (V)"position");
V key_287 = (V)"x";
var obj_288 = R.Get(v_284, (V)"position");
V key_289 = (V)"y";
R.Set(obj_286, key_287, values_285[0]);
R.Set(obj_288, key_289, values_285[1]);
var values_290 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_282, R.Get(v_280, (V)"id"), R.Get(v_280, (V)"x")}, new Results(R.Get(v_280, (V)"y")))));
V v_291 = values_290[0];
var values_292 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"choose"), R.Join(new V[] {v_282}, new Results(v_291))));
V v_293 = values_292[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_293, (V)"kind") == (V)"windup"), () => (V)(R.Get(v_293, (V)"ability_id") == R.Get(v_280, (V)"ability"))), () => (V)((V)R.Length(R.Get(v_293, (V)"cells")) == R.Get(v_280, (V)"n"))))));
var values_294 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_282}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_295 = values_294[0];
V v_296 = values_294[1];
V v_297 = values_294[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_295}, new Results(v_296)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_24, R.Join(new V[] {v_297}, new Results((V)"WindupStarted"))).First >= (V)1.0d))));
var values_298 = R.Join(new V[] {}, new Results(R.Get(v_291, (V)"pending_attack")));
V v_299 = values_298[0];
if (R.Truth(v_299)) {
var values_300 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_299, (V)"target_cells"), (V)1.0d)));
V v_301 = values_300[0];
var values_302 = R.Join(new V[] {(V)12.0d}, new Results((V)9.0d));
var obj_303 = R.Get(v_284, (V)"position");
V key_304 = (V)"x";
var obj_305 = R.Get(v_284, (V)"position");
V key_306 = (V)"y";
R.Set(obj_303, key_304, values_302[0]);
R.Set(obj_305, key_306, values_302[1]);
R.Call(R.Get(v_19, (V)"recompute"), R.Join(new V[] {}, new Results(v_282)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_299, (V)"target_cells"), (V)1.0d), (V)"x") == R.Get(v_301, (V)"x")), () => (V)(R.Get(R.Get(R.Get(v_299, (V)"target_cells"), (V)1.0d), (V)"y") == R.Get(v_301, (V)"y"))))));
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_282))).First}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_282)))))))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"biome area attacks retain locked cells, friendly fire and wall blocking"}, new Results(new V((RuleFunction)(args_307 => {

var values_308 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {}, new Results((V)8.0d))));
V v_309 = values_308[0];
var values_310 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_309))));
V v_311 = values_310[0];
var values_312 = R.Join(new V[] {(V)7.0d}, new Results((V)9.0d));
var obj_313 = R.Get(v_311, (V)"position");
V key_314 = (V)"x";
var obj_315 = R.Get(v_311, (V)"position");
V key_316 = (V)"y";
R.Set(obj_313, key_314, values_312[0]);
R.Set(obj_315, key_316, values_312[1]);
var values_317 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_309, (V)"core:enemy/spore_herald", (V)10.0d}, new Results((V)9.0d))));
V v_318 = values_317[0];
var values_319 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_309, (V)"core:enemy/mossbound_guard", (V)7.0d}, new Results((V)8.0d))));
V v_320 = values_319[0];
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_309}, new Results(R.Get(v_320, (V)"id"))));
var values_321 = R.Join(new V[] {}, new Results((V)35.0d));
var obj_322 = v_318;
V key_323 = (V)"speed";
R.Set(obj_322, key_323, values_321[0]);
var values_324 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"choose"), R.Join(new V[] {v_309}, new Results(v_318))));
V v_325 = values_324[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_325, (V)"kind") == (V)"windup"), () => (V)((V)R.Length(R.Get(v_325, (V)"cells")) == (V)3.0d)))));
var values_326 = R.Join(new V[] {}, new Results(R.Table()));
V v_327 = values_326[0];
foreach (var row_328 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_325, (V)"cells"))))))) {
V v_329 = row_328[0];
V v_330 = row_328[1];
var values_331 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_330, (V)"x")), new Entry((V)"y", R.Get(v_330, (V)"y")))));
var obj_332 = v_327;
V key_333 = v_329;
R.Set(obj_332, key_333, values_331[0]);
}
var values_334 = R.Join(new V[] {}, new Results((V)false));
V v_335 = values_334[0];
{ double end_338 = R.Num((V)4.0d), step_339 = R.Num((V)1.0d);
for (double i_337 = R.Num((V)1.0d); step_339 > 0 ? i_337 <= end_338 : i_337 >= end_338; i_337 += step_339) {
V v_336 = i_337;
var values_340 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_309}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_341 = values_340[0];
V v_342 = values_340[1];
V v_343 = values_340[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_341}, new Results(v_342)));
foreach (var row_344 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_343)))))) {
V v_345 = row_344[0];
V v_346 = row_344[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_346, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_346, (V)"target_id") == R.Get(v_320, (V)"id"))), () => (V)(R.Get(v_346, (V)"source_id") == R.Get(v_318, (V)"id"))))) {
var values_347 = R.Join(new V[] {}, new Results((V)true));
v_335 = values_347[0];
}
}
if (R.Truth(v_335)) {
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_335, () => (V)(R.Get(v_320, (V)"hp") < R.Get(v_320, (V)"max_hp")))}, new Results((V)"spore ally was not hit")));
foreach (var row_348 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_327)))))) {
V v_349 = row_348[0];
V v_350 = row_348[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_350, (V)"x") == R.Get(R.Get(R.Get(v_325, (V)"cells"), v_349), (V)"x")), () => (V)(R.Get(v_350, (V)"y") == R.Get(R.Get(R.Get(v_325, (V)"cells"), v_349), (V)"y"))))));
}
foreach (var row_351 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:enemy/canal_lancer"), new Entry((V)2, (V)"core:enemy/cinder_bellkeeper"), new Entry((V)3, (V)"core:enemy/spore_herald")))))))) {
V v_352 = row_351[0];
V v_353 = row_351[1];
var values_354 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {}, new Results((V)8.0d))));
V v_355 = values_354[0];
var values_356 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_355))));
V v_357 = values_356[0];
var values_358 = R.Join(new V[] {(V)5.0d}, new Results((V)7.0d));
var obj_359 = R.Get(v_357, (V)"position");
V key_360 = (V)"x";
var obj_361 = R.Get(v_357, (V)"position");
V key_362 = (V)"y";
R.Set(obj_359, key_360, values_358[0]);
R.Set(obj_361, key_362, values_358[1]);
var values_363 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_355, v_353, (V)7.0d}, new Results((V)7.0d))));
V v_364 = values_363[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_13, (V)"choose"), R.Join(new V[] {v_355}, new Results(v_364))).First, (V)"kind") != (V)"windup"))));
}
var values_365 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {}, new Results((V)8.0d))));
V v_366 = values_365[0];
var values_367 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_366))));
V v_368 = values_367[0];
var values_369 = R.Join(new V[] {(V)3.0d}, new Results((V)3.0d));
var obj_370 = R.Get(v_368, (V)"position");
V key_371 = (V)"x";
var obj_372 = R.Get(v_368, (V)"position");
V key_373 = (V)"y";
R.Set(obj_370, key_371, values_369[0]);
R.Set(obj_372, key_373, values_369[1]);
var values_374 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_366, (V)"core:enemy/spore_herald", (V)4.0d}, new Results((V)4.0d))));
V v_375 = values_374[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(R.Call(R.Get(v_13, (V)"choose"), R.Join(new V[] {v_366}, new Results(v_375))).First, (V)"kind") != (V)"windup")}, new Results((V)"spore ray cut through a blocked diagonal corner")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new biome pressure draws from its own hidden encounter pool"}, new Results(new V((RuleFunction)(args_376 => {

{ double end_379 = R.Num((V)4.0d), step_380 = R.Num((V)1.0d);
for (double i_378 = R.Num((V)2.0d); step_380 > 0 ? i_378 <= end_379 : i_378 >= end_379; i_378 += step_380) {
V v_377 = i_378;
var values_381 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), v_377)));
V v_382 = values_381[0];
var values_383 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)17.0d}, new Results(R.Table(new Entry((V)"stage_options", R.Table(new Entry((V)"index", (V)2.0d), new Entry((V)"biome_id", v_382), new Entry((V)"route_history", R.Table(new Entry(1, new Results(R.Invoke(v_382, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First)))))))))));
V v_384 = values_383[0];
foreach (var row_385 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_384, (V)"entities"))))))) {
V v_386 = row_385[0];
V v_387 = row_385[1];
if (R.Truth((V)(R.Get(v_387, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_384}, new Results(R.Get(v_387, (V)"id"))));
}
}
var values_388 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_384))));
V v_389 = values_388[0];
var values_390 = R.Join(new V[] {(V)1000.0d, (V)1000.0d}, new Results((V)100.0d));
var obj_391 = v_389;
V key_392 = (V)"hp";
var obj_393 = v_389;
V key_394 = (V)"max_hp";
var obj_395 = v_389;
V key_396 = (V)"defense";
R.Set(obj_391, key_392, values_390[0]);
R.Set(obj_393, key_394, values_390[1]);
R.Set(obj_395, key_396, values_390[2]);
var values_397 = Results.Empty;
V v_398 = values_397[0];
{ double end_401 = R.Num((V)15.0d), step_402 = R.Num((V)1.0d);
for (double i_400 = R.Num((V)1.0d); step_402 > 0 ? i_400 <= end_401 : i_400 >= end_401; i_400 += step_402) {
V v_399 = i_400;
var values_403 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_384}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_404 = values_403[0];
V v_405 = values_403[1];
V v_406 = values_403[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_404}, new Results(v_405)));
foreach (var row_407 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_406)))))) {
V v_408 = row_407[0];
V v_409 = row_407[1];
if (R.Truth((V)(R.Get(v_409, (V)"kind") == (V)"PressureSpawned"))) {
var values_410 = R.Join(new V[] {}, new Results(v_409));
v_398 = values_410[0];
}
}
if (R.Truth(v_398)) {
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_398}, new Results((V)"pressure never produced biome encounter")));
var values_411 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"get"), R.Join(new V[] {}, new Results(v_382))).First, (V)"enemy_ids")));
V v_412 = values_411[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or(R.Or((V)(R.Get(v_398, (V)"definition_id") == R.Get(v_412, (V)1.0d)), () => (V)(R.Get(v_398, (V)"definition_id") == R.Get(v_412, (V)2.0d))), () => (V)(R.Get(v_398, (V)"definition_id") == R.Get(v_412, (V)3.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_19, (V)"is_visible"), R.Join(new V[] {v_384, R.Get(v_384, (V)"active_floor_id"), R.Get(v_398, (V)"x")}, new Results(R.Get(v_398, (V)"y")))).First)))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new-biome concrete floor, pending AI and boon state survive inert bytes"}, new Results(new V((RuleFunction)(args_413 => {

{ double end_416 = R.Num((V)3.0d), step_417 = R.Num((V)1.0d);
for (double i_415 = R.Num((V)2.0d); step_417 > 0 ? i_415 <= end_416 : i_415 >= end_416; i_415 += step_417) {
V v_414 = i_415;
var values_418 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), v_414)));
V v_419 = values_418[0];
var values_420 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)23.0d}, new Results(R.Table(new Entry((V)"stage_options", R.Table(new Entry((V)"index", (V)2.0d), new Entry((V)"biome_id", v_419), new Entry((V)"route_history", R.Table(new Entry(1, new Results(R.Invoke(v_419, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First)))))))))));
V v_421 = values_420[0];
R.Call(R.Get(v_11, (V)"set_boon_stacks"), R.Join(new V[] {v_421, R.Get(v_421, (V)"player_id"), (V)"core:boon/emberbrand", (V)"rare"}, new Results((V)2.0d)));
foreach (var row_422 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_421, (V)"entities"))))))) {
V v_423 = row_422[0];
V v_424 = row_422[1];
if (R.Truth((V)(R.Get(v_424, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_421}, new Results(R.Get(v_424, (V)"id"))));
}
}
var values_425 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_421))));
V v_426 = values_425[0];
var values_427 = R.Join(new V[] {(V)100.0d}, new Results((V)100.0d));
var obj_428 = v_426;
V key_429 = (V)"hp";
var obj_430 = v_426;
V key_431 = (V)"max_hp";
R.Set(obj_428, key_429, values_427[0]);
R.Set(obj_430, key_431, values_427[1]);
var values_432 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_421, R.Get(R.Get(R.Call(R.Get(v_5, (V)"get"), R.Join(new V[] {}, new Results(v_419))).First, (V)"enemy_ids"), (V)1.0d), (V)(R.Get(R.Get(v_426, (V)"position"), (V)"x") + (V)2.0d)}, new Results(R.Get(R.Get(v_426, (V)"position"), (V)"y")))));
V v_433 = values_432[0];
var values_434 = R.Join(new V[] {}, new Results((V)35.0d));
var obj_435 = v_433;
V key_436 = (V)"speed";
R.Set(obj_435, key_436, values_434[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_421}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_433, (V)"pending_attack"), () => (V)((V)R.Length(R.Get(R.Get(v_433, (V)"pending_attack"), (V)"target_cells")) >= (V)2.0d)))));
var values_437 = R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_421))))));
V v_438 = values_437[0];
var values_439 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"decode"), R.Join(new V[] {}, new Results(v_438))))));
V v_440 = values_439[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_440, (V)"generated_map"), (V)"biome_id") == v_419), () => (V)(R.Get(R.Get(R.Get(v_440, (V)"generated_map"), (V)"rows"), (V)1.0d) == R.Get(R.Get(R.Get(v_421, (V)"generated_map"), (V)"rows"), (V)1.0d))), () => (V)(R.Get(R.Get(R.Get(R.Get(R.Get(v_440, (V)"entities"), (V)1.0d), (V)"boons"), (V)"core:boon/emberbrand"), (V)"rare") == (V)2.0d)), () => (V)(R.Get(R.Get(R.Call(R.Get(v_11, (V)"entity"), R.Join(new V[] {v_440}, new Results(R.Get(v_433, (V)"id")))).First, (V)"pending_attack"), (V)"schedule_id") == R.Get(R.Get(v_433, (V)"pending_attack"), (V)"schedule_id"))))));
var values_441 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_421}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_442 = values_441[0];
V v_443 = values_441[1];
V v_444 = values_441[2];
var values_445 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_440}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_446 = values_445[0];
V v_447 = values_445[1];
V v_448 = values_445[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_442 == v_446), () => (V)(v_443 == v_447)))));
R.Call(v_3, R.Join(new V[] {v_444}, new Results(v_448)));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_421))).First}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_440)))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"biome generation and chest offers never consume combat RNG"}, new Results(new V((RuleFunction)(args_449 => {

var values_450 = Results.Empty;
V v_451 = values_450[0];
{ double end_454 = R.Num((V)R.Length(R.Get(v_5, (V)"order"))), step_455 = R.Num((V)1.0d);
for (double i_453 = R.Num((V)1.0d); step_455 > 0 ? i_453 <= end_454 : i_453 >= end_454; i_453 += step_455) {
V v_452 = i_453;
var values_456 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), v_452)));
V v_457 = values_456[0];
var values_458 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_452 == (V)1.0d), () => (V)1.0d), () => (V)2.0d)));
V v_459 = values_458[0];
var values_460 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_459 == (V)1.0d), () => R.Table()), () => R.Table(new Entry(1, new Results(R.Invoke(v_457, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First))))));
V v_461 = values_460[0];
var values_462 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {(V)29.0d}, new Results(R.Table(new Entry((V)"stage_options", R.Table(new Entry((V)"index", v_459), new Entry((V)"biome_id", v_457), new Entry((V)"route_history", v_461))))))));
V v_463 = values_462[0];
var values_464 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_463, (V)"rng"), (V)"state")));
V v_465 = values_464[0];
if (R.Truth(v_451)) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_465 == v_451))));
} else {
var values_466 = R.Join(new V[] {}, new Results(v_465));
v_451 = values_466[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_463, (V)"generated_map"), (V)"placements"), (V)"chests")) == (V)5.0d))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new enemy deaths award ordinary XP and collectible currency"}, new Results(new V((RuleFunction)(args_467 => {

foreach (var row_468 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:enemy/spore_herald"), new Entry((V)2, (V)"core:enemy/canal_lancer"), new Entry((V)3, (V)"core:enemy/cinder_bellkeeper")))))))) {
V v_469 = row_468[0];
V v_470 = row_468[1];
var values_471 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {}, new Results((V)24.0d))));
V v_472 = values_471[0];
var values_473 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_472, v_470, (V)3.0d}, new Results((V)2.0d))));
V v_474 = values_473[0];
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_472}, new Results(R.Get(v_474, (V)"id"))));
var values_475 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_476 = v_474;
V key_477 = (V)"hp";
R.Set(obj_476, key_477, values_475[0]);
var values_478 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_472}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_479 = values_478[0];
V v_480 = values_478[1];
V v_481 = values_478[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_479}, new Results(v_480)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(v_24, R.Join(new V[] {v_481}, new Results((V)"EntityKilled"))).First == (V)1.0d), () => (V)(R.Call(v_24, R.Join(new V[] {v_481}, new Results((V)"ExperienceGained"))).First == (V)1.0d)), () => (V)(R.Get(R.Get(v_472, (V)"progression"), (V)"level") >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_472, (V)"drops")) == (V)1.0d)))));
var values_482 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_472}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
v_479 = values_482[0];
v_480 = values_482[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_479}, new Results(v_480)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_472, (V)"currency") == R.Get(R.Call(R.Get(v_15, (V)"get_enemy"), R.Join(new V[] {}, new Results(v_470))).First, (V)"currency_drop")), () => R.Get(R.Get(R.Get(v_472, (V)"drops"), (V)1.0d), (V)"collected")))));
}
var values_483 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {}, new Results((V)31.0d))));
V v_484 = values_483[0];
R.Call(R.Get(v_11, (V)"set_boon_stacks"), R.Join(new V[] {v_484, R.Get(v_484, (V)"player_id"), (V)"core:boon/emberbrand", (V)"legendary"}, new Results((V)3.0d)));
var values_485 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"spawn_enemy"), R.Join(new V[] {v_484, (V)"core:enemy/canal_lancer", (V)3.0d}, new Results((V)2.0d))));
V v_486 = values_485[0];
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_484}, new Results(R.Get(v_486, (V)"id"))));
var values_487 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_484}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_488 = values_487[0];
V v_489 = values_487[1];
V v_490 = values_487[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_488}, new Results(v_489)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(v_24, R.Join(new V[] {v_490}, new Results((V)"StatusApplied"))).First >= (V)1.0d), () => (V)(R.Call(v_24, R.Join(new V[] {v_490}, new Results((V)"StatusTick"))).First >= (V)1.0d)), () => (V)(R.Call(v_24, R.Join(new V[] {v_490}, new Results((V)"EntityKilled"))).First == (V)1.0d)), () => (V)(R.Get(R.Get(v_484, (V)"progression"), (V)"level") == (V)2.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_484))).First}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_484)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"route graph selects unvisited biome content and preserves build"}, new Results(new V((RuleFunction)(args_491 => {

{ double end_494 = R.Num((V)2.0d), step_495 = R.Num((V)1.0d);
for (double i_493 = R.Num((V)1.0d); step_495 > 0 ? i_493 <= end_494 : i_493 >= end_494; i_493 += step_495) {
V v_492 = i_493;
{ double end_498 = R.Num((V)2.0d), step_499 = R.Num((V)1.0d);
for (double i_497 = R.Num((V)1.0d); step_499 > 0 ? i_497 <= end_498 : i_497 >= end_498; i_497 += step_499) {
V v_496 = i_497;
var values_500 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {}, new Results((V)19.0d))));
V v_501 = values_500[0];
var values_502 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {v_501}, new Results(v_492))));
V v_503 = values_502[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_501, (V)"stage"), (V)"index") == (V)2.0d), () => (V)(R.Get(R.Get(v_501, (V)"stage"), (V)"biome_id") == R.Get(v_503, (V)"biome_id"))))));
var values_504 = R.Join(new V[] {}, R.Call(v_61, R.Join(new V[] {v_501}, new Results(v_496))));
V v_505 = values_504[0];
var values_506 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_492 == (V)1.0d), () => R.Get(R.Get(v_5, (V)"order"), (V)3.0d)), () => R.Get(R.Get(v_5, (V)"order"), (V)2.0d))));
V v_507 = values_506[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_505, (V)"biome_id") == R.Or(R.And((V)(v_496 == (V)1.0d), () => v_507), () => R.Get(R.Get(v_5, (V)"order"), (V)4.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_501, (V)"stage"), (V)"index") == (V)3.0d), () => (V)(R.Get(R.Get(v_501, (V)"stage"), (V)"biome_id") == R.Get(v_505, (V)"biome_id"))), () => (V)(R.Get(v_505, (V)"biome_id") != R.Get(v_503, (V)"biome_id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_501, (V)"generated_map"), (V)"biome_id") == R.Get(v_505, (V)"biome_id")), () => (V)((V)R.Length(R.Get(v_501, (V)"retired_stages")) == (V)2.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_501))).First}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_501)))))))));
R.Call(v_61, R.Join(new V[] {v_501}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_501, (V)"finale"), () => (V)(R.Get(R.Get(v_501, (V)"finale"), (V)"phase") == (V)"active")), () => (V)(R.Get(v_501, (V)"map_id") == (V)"core:map/final_sanctum")))));
}
}
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"same route reproduces a concrete biome; another route changes it"}, new Results(new V((RuleFunction)(args_508 => {

var values_509 = R.Join(new V[] {R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {}, new Results((V)31.0d))).First, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {}, new Results((V)31.0d))).First}, R.Call(R.Get(v_11, (V)"new_stage"), R.Join(new V[] {}, new Results((V)31.0d))));
V v_510 = values_509[0];
V v_511 = values_509[1];
V v_512 = values_509[2];
R.Call(v_61, R.Join(new V[] {v_510}, new Results((V)1.0d)));
R.Call(v_61, R.Join(new V[] {v_511}, new Results((V)1.0d)));
R.Call(v_61, R.Join(new V[] {v_512}, new Results((V)2.0d)));
R.Call(v_3, R.Join(new V[] {R.Get(v_510, (V)"generated_map")}, new Results(R.Get(v_511, (V)"generated_map"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Get(R.Get(v_510, (V)"stage"), (V)"biome_id") == R.Get(R.Get(v_511, (V)"stage"), (V)"biome_id")), () => (V)(R.Get(v_510, (V)"run_seed") == R.Get(v_511, (V)"run_seed"))), () => (V)(R.Get(v_510, (V)"run_seed") != R.Get(v_512, (V)"run_seed"))), () => (V)(R.Get(R.Get(v_510, (V)"generated_map"), (V)"id") != R.Get(R.Get(v_512, (V)"generated_map"), (V)"id"))), () => (V)(R.Get(v_510, (V)"active_floor_id") != R.Get(v_512, (V)"active_floor_id"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ordinary actions enter Fungal then Ashen and defeat signature foes"}, new Results(new V((RuleFunction)(args_513 => {

var values_514 = R.Join(new V[] {}, R.Call(R.Get(v_23, (V)"biome_route"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_515 = values_514[0];
var values_516 = R.Join(new V[] {}, new Results(R.Get(v_515, (V)"state")));
V v_517 = values_516[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_517, (V)"stage"), (V)"biome_id") == R.Get(R.Get(v_5, (V)"order"), (V)4.0d)), () => (V)((V)R.Length(R.Get(R.Get(v_517, (V)"stage"), (V)"route_history")) == (V)2.0d)), () => R.Call(R.Get(v_23, (V)"has_event"), R.Join(new V[] {v_515}, new Results((V)"BoonGranted"))).First), () => (V)(!R.Truth(R.Get(v_517, (V)"game_over")))))));
var values_518 = R.Join(new V[] {}, R.Call(R.Get(v_23, (V)"replay"), R.Join(new V[] {}, new Results(v_515))));
V v_519 = values_518[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_517))).First}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_519, (V)"state"))))));
var values_520 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_517))))));
V v_521 = values_520[0];
var values_522 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_517}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_523 = values_522[0];
V v_524 = values_522[1];
V v_525 = values_522[2];
var values_526 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"submit"), R.Join(new V[] {v_521}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_527 = values_526[0];
V v_528 = values_526[1];
V v_529 = values_526[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_523 == v_527), () => (V)(v_524 == v_528)))));
R.Call(v_3, R.Join(new V[] {v_525}, new Results(v_529)));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_517))).First}, R.Call(R.Get(v_11, (V)"snapshot"), R.Join(new V[] {}, new Results(v_521)))));
R.Call(rt.Global("print"), R.Join(new V[] {}, R.Invoke((V)"BIOME LEGAL seed=3 actions=%d stage=3 biome=%s hp=%d", "format", R.Join(new V[] {(V)R.Length(R.Get(v_515, (V)"trace")), R.Get(R.Get(v_517, (V)"stage"), (V)"biome_id")}, new Results(R.Get(R.Call(R.Get(v_11, (V)"player"), R.Join(new V[] {}, new Results(v_517))).First, (V)"hp"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ordinary route two reaches Aqueduct and defeats a Lancer"}, new Results(new V((RuleFunction)(args_530 => {

var values_531 = R.Join(new V[] {}, R.Call(R.Get(v_23, (V)"stage_one"), R.Join(new V[] {(V)3.0d}, new Results((V)2.0d))));
V v_532 = values_531[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_532, (V)"state"), (V)"stage"), (V)"biome_id") == R.Get(R.Get(v_5, (V)"order"), (V)3.0d)))));
var values_535 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_23, (V)"nearest"), R.Join(new V[] {R.Get(v_532, (V)"state")}, new Results(new V((RuleFunction)(args_533 => {
V v_534 = args_533[0];
return R.Join(new V[] {}, new Results((V)(R.Get(v_534, (V)"definition_id") == (V)"core:enemy/canal_lancer")));
return Results.Empty;
}))))))));
V v_536 = values_535[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_23, (V)"fight"), R.Join(new V[] {v_532, R.Get(v_536, (V)"id")}, new Results((V)160.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(v_532, (V)"state"), (V)"game_over"))))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
