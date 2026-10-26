// Static C# port of tests/corridor_branch_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 415c1041b35499abd0b5484de3b6336efb26a9996d385502d6213fa519725ea7
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_corridor_branch_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.review_features"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rooms"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_10 = values_9[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
V v_13 = args_11[1];
R.Call(v_12, R.Join(new V[] {(V)"approved room pool and joined breakable cloister are live"}, new Results(new V((RuleFunction)(args_14 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_6, (V)"order")) == (V)21.0d))));
var values_15 = R.Join(new V[] {}, new Results((V)0.0d));
V v_16 = values_15[0];
foreach (var row_17 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"order"))))))) {
V v_18 = row_17[0];
V v_19 = row_17[1];
var values_20 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(v_19))));
V v_21 = values_20[0];
foreach (var row_22 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"order"))))))) {
V v_23 = row_22[0];
V v_24 = row_22[1];
var values_25 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"definitions"), R.Concat(R.Get(v_21, (V)"room_prefix"), R.Invoke(v_24, "match", R.Join(new V[] {}, new Results((V)"[^/]+$"))).First))));
V v_26 = values_25[0];
R.Call(rt.Global("assert"), R.Join(new V[] {v_26}, new Results((V)"missing approved biome room")));
if (R.Truth(R.Get(v_26, (V)"structures"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_26, (V)"structures")) == (V)3.0d))));
var values_27 = R.Join(new V[] {}, new Results((V)(v_16 + (V)1.0d)));
v_16 = values_27[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_16 == (V)105.0d))));
var values_28 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"5,8", (V)true), new Entry((V)"17,8", (V)true), new Entry((V)"11,12", (V)true), new Entry((V)"7,4", (V)true), new Entry((V)"15,4", (V)true))));
V v_29 = values_28[0];
foreach (var row_30 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"order"))))))) {
V v_31 = row_30[0];
V v_32 = row_30[1];
var values_33 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(v_32))));
V v_34 = values_33[0];
var values_35 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"definitions"), R.Concat(R.Get(v_34, (V)"room_prefix"), (V)"broken_cloister"))));
V v_36 = values_35[0];
{ double end_39 = R.Num((V)12.0d), step_40 = R.Num((V)1.0d);
for (double i_38 = R.Num((V)4.0d); step_40 > 0 ? i_38 <= end_39 : i_38 >= end_39; i_38 += step_40) {
V v_37 = i_38;
{ double end_43 = R.Num((V)17.0d), step_44 = R.Num((V)1.0d);
for (double i_42 = R.Num((V)5.0d); step_44 > 0 ? i_42 <= end_43 : i_42 >= end_43; i_42 += step_44) {
V v_41 = i_42;
if (R.Truth(R.Or(R.Or(R.Or((V)(v_37 == (V)4.0d), () => (V)(v_37 == (V)12.0d)), () => (V)(v_41 == (V)5.0d)), () => (V)(v_41 == (V)17.0d)))) {
var values_45 = R.Join(new V[] {}, R.Invoke(R.Get(R.Get(v_36, (V)"rows"), v_37), "sub", R.Join(new V[] {v_41}, new Results(v_41))));
V v_46 = values_45[0];
var values_47 = R.Join(new V[] {}, new Results(R.Concat(v_41, R.Concat((V)",", v_37))));
V v_48 = values_47[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_46 == R.Or(R.Or(R.And((V)(v_48 == (V)"11,4"), () => (V)"."), () => R.And(R.Get(v_29, v_48), () => (V)"B")), () => (V)"#"))}, new Results(R.Concat((V)"cloister ring breached in ", R.Concat(v_32, R.Concat((V)" at ", v_48))))));
}
}
}
}
}
}
return Results.Empty;
})))));
R.Call(v_12, R.Join(new V[] {(V)"all five sidequests progress, reward once, and survive restore"}, new Results(new V((RuleFunction)(args_49 => {

var values_50 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_51 = values_50[0];
V v_52 = values_50[1];
{ double end_55 = R.Num((V)80.0d), step_56 = R.Num((V)1.0d);
for (double i_54 = R.Num((V)1.0d); step_56 > 0 ? i_54 <= end_55 : i_54 >= end_55; i_54 += step_56) {
V v_53 = i_54;
if (R.Truth((V)(v_52 == (V)5.0d))) {
break;
}
var values_57 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_53}, new Results(R.Get(R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_8, (V)"order"), (V)1.0d)))).First, (V)"map_id")))));
V v_58 = values_57[0];
var values_59 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_58, (V)"generated_map"), (V)"corridor_quests"), (V)1.0d)));
V v_60 = values_59[0];
if (R.Truth(R.And(v_60, () => (V)(!R.Truth(R.Get(v_51, R.Get(v_60, (V)"kind"))))))) {
var values_61 = R.Join(new V[] {}, new Results(v_58));
var obj_62 = v_51;
V key_63 = R.Get(v_60, (V)"kind");
R.Set(obj_62, key_63, values_61[0]);

var values_64 = R.Join(new V[] {}, new Results((V)(v_52 + (V)1.0d)));
v_52 = values_64[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_52 == (V)5.0d)}, new Results((V)"a reviewed branch type never appeared")));
foreach (var row_65 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"spur"), new Entry((V)2, (V)"braid"), new Entry((V)3, (V)"switchback"), new Entry((V)4, (V)"hub"), new Entry((V)5, (V)"false_end")))))))) {
V v_66 = row_65[0];
V v_67 = row_65[1];
var values_68 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_51, v_67)))));
V v_69 = values_68[0];
var values_70 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_69, (V)"generated_map"), (V)"corridor_quests"), (V)1.0d)));
V v_71 = values_70[0];
var values_72 = R.Join(new V[] {}, new Results(R.Table()));
V v_73 = values_72[0];
V v_74 = V.Nil;
v_74 = new V((RuleFunction)(args_75 => {
V v_76 = args_75[0];
V v_77 = args_75[1];
var values_78 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_76), new Entry((V)"fields", v_77))));
var obj_79 = v_73;
V key_80 = (V)((V)R.Length(v_73) + (V)1.0d);
R.Set(obj_79, key_80, values_78[0]);
return Results.Empty;
}));
var values_81 = Results.Empty;
V v_82 = values_81[0];
V v_83 = values_81[1];
foreach (var row_84 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_69, (V)"review_features"))))))) {
V v_85 = row_84[0];
V v_86 = row_84[1];
if (R.Truth(R.And((V)(R.Get(v_86, (V)"quest_id") == R.Get(v_71, (V)"id")), () => (V)(R.Get(v_86, (V)"kind") == (V)"quest_reward")))) {
var values_87 = R.Join(new V[] {}, new Results(v_86));
v_82 = values_87[0];
}
}
foreach (var row_88 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_69, (V)"entities"))))))) {
V v_89 = row_88[0];
V v_90 = row_88[1];
if (R.Truth((V)(R.Get(v_90, (V)"quest_id") == R.Get(v_71, (V)"id")))) {
var values_91 = R.Join(new V[] {}, new Results(v_90));
v_83 = values_91[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_82, () => (V)(!R.Truth(R.Get(v_82, (V)"unlocked")))), () => (V)(!R.Truth(R.Get(v_82, (V)"collected")))))));
if (R.Truth(v_83)) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_83, (V)"open"))), () => R.Get(v_83, (V)"blocks_movement")))));
}
foreach (var row_92 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_69, (V)"review_features"))))))) {
V v_93 = row_92[0];
V v_94 = row_92[1];
if (R.Truth(R.And((V)(R.Get(v_94, (V)"quest_id") == R.Get(v_71, (V)"id")), () => (V)(R.Get(v_94, (V)"kind") != (V)"quest_reward")))) {
R.Call(R.Get(v_4, (V)"on_event"), R.Join(new V[] {v_69, R.Table(new Entry((V)"kind", (V)"TileEntered"), new Entry((V)"floor_id", R.Get(v_94, (V)"floor_id")), new Entry((V)"source_id", R.Get(v_69, (V)"player_id")), new Entry((V)"x", R.Get(v_94, (V)"x")), new Entry((V)"y", R.Get(v_94, (V)"y"))), v_74}, new Results(new V((RuleFunction)(args_95 => {


return Results.Empty;
})))));
}
}
if (R.Truth(R.And((V)(v_67 == (V)"false_end"), () => R.Get(v_82, (V)"target_id")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_82, (V)"unlocked"))))));
R.Call(R.Get(v_4, (V)"on_event"), R.Join(new V[] {v_69, R.Table(new Entry((V)"kind", (V)"EntityKilled"), new Entry((V)"floor_id", R.Get(v_82, (V)"floor_id")), new Entry((V)"target_id", R.Get(v_82, (V)"target_id"))), v_74}, new Results(new V((RuleFunction)(args_96 => {


return Results.Empty;
})))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_82, (V)"unlocked")}, new Results(R.Concat(v_67, (V)" reward stayed locked"))));
if (R.Truth(v_83)) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_83, (V)"open"), () => (V)(!R.Truth(R.Get(v_83, (V)"blocks_movement")))))));
}
var values_97 = R.Join(new V[] {}, new Results(R.Get(v_69, (V)"currency")));
V v_98 = values_97[0];
var values_99 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"TileEntered"), new Entry((V)"floor_id", R.Get(v_82, (V)"floor_id")), new Entry((V)"source_id", R.Get(v_69, (V)"player_id")), new Entry((V)"x", R.Get(v_82, (V)"x")), new Entry((V)"y", R.Get(v_82, (V)"y")))));
V v_100 = values_99[0];
R.Call(R.Get(v_4, (V)"on_event"), R.Join(new V[] {v_69, v_100, v_74}, new Results(new V((RuleFunction)(args_101 => {


return Results.Empty;
})))));
R.Call(R.Get(v_4, (V)"on_event"), R.Join(new V[] {v_69, v_100, v_74}, new Results(new V((RuleFunction)(args_102 => {


return Results.Empty;
})))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_82, (V)"collected"), () => (V)(R.Get(v_69, (V)"currency") == (V)(v_98 + (V)5.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_69)))))));
}
return Results.Empty;
})))));
R.Call(v_12, R.Join(new V[] {(V)"quest branch cells and nodes stay walkable across all biomes"}, new Results(new V((RuleFunction)(args_103 => {

var values_104 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_105 = values_104[0];
var values_106 = R.Join(new V[] {}, new Results(R.Table()));
V v_107 = values_106[0];
foreach (var row_108 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"order"))))))) {
V v_109 = row_108[0];
V v_110 = row_108[1];
{ double end_113 = R.Num((V)30.0d), step_114 = R.Num((V)1.0d);
for (double i_112 = R.Num((V)1.0d); step_114 > 0 ? i_112 <= end_113 : i_112 >= end_113; i_112 += step_114) {
V v_111 = i_112;
var values_115 = R.Join(new V[] {}, R.Call(R.Get(v_105, (V)"generate_pair"), R.Join(new V[] {v_111}, new Results(R.Table(new Entry((V)"biome_id", v_110))))));
V v_116 = values_115[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_105, (V)"validate_pair"), R.Join(new V[] {}, new Results(v_116)))));
foreach (var row_117 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_116)))))) {
V v_118 = row_117[0];
V v_119 = row_117[1];
foreach (var row_120 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_119, (V)"corridor_quests"), () => R.Table()))))))) {
V v_121 = row_120[0];
V v_122 = row_120[1];
var values_123 = R.Join(new V[] {}, new Results((V)true));
var obj_124 = v_107;
V key_125 = v_110;
R.Set(obj_124, key_125, values_123[0]);
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_122, (V)"nodes"))))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"walkable"), R.Join(new V[] {v_119, R.Get(v_128, (V)"x")}, new Results(R.Get(v_128, (V)"y"))))));
}
}
}
}
}
}
foreach (var row_129 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"order"))))))) {
V v_130 = row_129[0];
V v_131 = row_129[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_107, v_131)}, new Results((V)"biome lost all sidequest branches")));
}
return Results.Empty;
})))));
R.Call(v_12, R.Join(new V[] {(V)"an ordinary move discovers a sidequest clue"}, new Results(new V((RuleFunction)(args_132 => {

var values_133 = Results.Empty;
V v_134 = values_133[0];
V v_135 = values_133[1];
{ double end_138 = R.Num((V)40.0d), step_139 = R.Num((V)1.0d);
for (double i_137 = R.Num((V)1.0d); step_139 > 0 ? i_137 <= end_138 : i_137 >= end_138; i_137 += step_139) {
V v_136 = i_137;
var values_140 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_136}, new Results(R.Get(R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_8, (V)"order"), (V)1.0d)))).First, (V)"map_id")))));
V v_141 = values_140[0];
var values_142 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_141, (V)"generated_map"), (V)"corridor_quests"), (V)1.0d)));
V v_143 = values_142[0];
if (R.Truth(R.And(v_143, () => (V)(R.Get(v_143, (V)"kind") == (V)"spur")))) {
var values_144 = R.Join(new V[] {}, new Results(v_141));
v_134 = values_144[0];
foreach (var row_145 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_134, (V)"review_features"))))))) {
V v_146 = row_145[0];
V v_147 = row_145[1];
if (R.Truth(R.And((V)(R.Get(v_147, (V)"quest_id") == R.Get(v_143, (V)"id")), () => (V)(R.Get(v_147, (V)"kind") == (V)"quest_clue")))) {
var values_148 = R.Join(new V[] {}, new Results(v_147));
v_135 = values_148[0];
}
}
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_134, () => v_135))));
var values_149 = R.Join(new V[] {}, new Results((V)false));
V v_150 = values_149[0];
foreach (var row_151 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_152 = row_151[0];
V v_153 = row_151[1];
var values_154 = R.Join(new V[] {(V)(R.Get(v_135, (V)"x") - R.Get(v_153, (V)1.0d))}, new Results((V)(R.Get(v_135, (V)"y") - R.Get(v_153, (V)2.0d))));
V v_155 = values_154[0];
V v_156 = values_154[1];
var values_157 = R.Join(new V[] {}, new Results((V)false));
V v_158 = values_157[0];
foreach (var row_159 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_134, (V)"entities"))))))) {
V v_160 = row_159[0];
V v_161 = row_159[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_161, (V)"id") != R.Get(v_134, (V)"player_id")), () => R.Get(v_161, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_161, (V)"position"), (V)"floor_id") == R.Get(v_134, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_161, (V)"position"), (V)"x") == v_155)), () => (V)(R.Get(R.Get(v_161, (V)"position"), (V)"y") == v_156)))) {
var values_162 = R.Join(new V[] {}, new Results((V)true));
v_158 = values_162[0];

break;
}
}
if (R.Truth(R.And(R.Call(R.Get(v_10, (V)"walkable"), R.Join(new V[] {R.Get(v_134, (V)"generated_map"), v_155}, new Results(v_156))).First, () => (V)(!R.Truth(v_158))))) {
var values_163 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_134))));
V v_164 = values_163[0];
var values_165 = R.Join(new V[] {v_155}, new Results(v_156));
var obj_166 = R.Get(v_164, (V)"position");
V key_167 = (V)"x";
var obj_168 = R.Get(v_164, (V)"position");
V key_169 = (V)"y";
R.Set(obj_166, key_167, values_165[0]);
R.Set(obj_168, key_169, values_165[1]);
var values_170 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_134}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_153, (V)1.0d)), new Entry((V)"dy", R.Get(v_153, (V)2.0d)))))));
V v_171 = values_170[0];
V v_172 = values_170[1];
V v_173 = values_170[2];
if (R.Truth(v_171)) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_135, (V)"discovered"))));
var values_174 = R.Join(new V[] {}, new Results((V)false));
V v_175 = values_174[0];
foreach (var row_176 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_173)))))) {
V v_177 = row_176[0];
V v_178 = row_176[1];
if (R.Truth((V)(R.Get(v_178, (V)"kind") == (V)"SidequestClueFound"))) {
var values_179 = R.Join(new V[] {}, new Results((V)true));
v_175 = values_179[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_175}, new Results((V)"movement did not emit quest discovery")));
var values_180 = R.Join(new V[] {}, new Results((V)true));
v_150 = values_180[0];

break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_150}, new Results((V)"could not move onto the sidequest clue")));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
