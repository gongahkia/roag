// Static C# port of tests/idea_interaction_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: f8308a93c1d7fad84d1ba7a2c1d3eb34fe2927a03c6deba40bd6c357d8c6488f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_interaction_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_interactions"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_11 = values_10[0];

var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_13 = values_12[0];

var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_interaction_content"))));
V v_17 = values_16[0];

var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_19 = values_18[0];
V v_20 = V.Nil;
v_20 = new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
V v_23 = args_21[1];
var values_24 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_25 = values_24[0];

var values_26 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_25))));
V v_27 = values_26[0];

var values_28 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_29 = R.Get(v_27, (V)"position");
V key_30 = (V)"x";
var obj_31 = R.Get(v_27, (V)"position");
V key_32 = (V)"y";
R.Set(obj_29, key_30, values_28[0]);
R.Set(obj_31, key_32, values_28[1]);

R.Call(R.Get(v_13, (V)"recompute"), R.Join(new V[] {}, new Results(v_25)));
var values_33 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_25, v_22, R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_25))).First, (V)7.0d, (V)5.0d}, new Results(v_23))));
V v_34 = values_33[0];
return R.Join(new V[] {v_25, v_27}, new Results(v_34));
return Results.Empty;
}));
V v_35 = V.Nil;
v_35 = new V((RuleFunction)(args_36 => {
V v_37 = args_36[0];
V v_38 = args_36[1];
V v_39 = args_36[2];
V v_40 = args_36[3];
V v_41 = args_36[4];
var values_42 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_37}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_38, (V)"id")), new Entry((V)"choice_id", v_39), new Entry((V)"selection", v_40), new Entry((V)"replace", v_41))))));
V v_43 = values_42[0];
V v_44 = values_42[1];
V v_45 = values_42[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_43}, new Results(v_44)));

return R.Join(new V[] {}, new Results(v_45));
return Results.Empty;
}));
V v_46 = V.Nil;
v_46 = new V((RuleFunction)(args_47 => {
V v_48 = args_47[0];
V v_49 = args_47[1];
var values_50 = R.Join(new V[] {}, new Results((V)0.0d));
V v_51 = values_50[0];

foreach (var row_52 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_48)))))) {
V v_53 = row_52[0];
V v_54 = row_52[1];
if (R.Truth((V)(R.Get(v_54, (V)"kind") == v_49))) {
var values_55 = R.Join(new V[] {}, new Results((V)(v_51 + (V)1.0d)));
v_51 = values_55[0];
}
}

return R.Join(new V[] {}, new Results(v_51));
return Results.Empty;
}));
V v_56 = V.Nil;
v_56 = new V((RuleFunction)(args_57 => {
V v_58 = args_57[0];
var values_59 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_generated"), R.Join(new V[] {(V)37.0d}, new Results(R.Table(new Entry((V)"class_id", v_58))))));
V v_60 = values_59[0];

var values_61 = R.Join(new V[] {}, new Results((V)false));
var obj_62 = R.Get(v_60, (V)"meta");
V key_63 = (V)"eligible";
R.Set(obj_62, key_63, values_61[0]);
{ double end_66 = R.Num((V)1.0d), step_67 = R.Num((-(V)1.0d));
for (double i_65 = R.Num((V)R.Length(R.Get(v_60, (V)"entities"))); step_67 > 0 ? i_65 <= end_66 : i_65 >= end_66; i_65 += step_67) {
V v_64 = i_65;
var values_68 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_60, (V)"entities"), v_64)));
V v_69 = values_68[0];
if (R.Truth((V)(R.Get(v_69, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_60}, new Results(R.Get(v_69, (V)"id"))));

R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_60, (V)"entities")}, new Results(v_64)));
}
}
}
var values_70 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"version", (V)1.0d), new Entry((V)"nodes", R.Table()), new Entry((V)"groups", R.Table()))));
var obj_71 = v_60;
V key_72 = (V)"idea_rooms";
R.Set(obj_71, key_72, values_70[0]);
var values_73 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_60))));
V v_74 = values_73[0];

var values_75 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_60))));
V v_76 = values_75[0];
foreach (var row_77 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_76, (V)"rooms"))))))) {
V v_78 = row_77[0];
V v_79 = row_77[1];
{ double end_82 = R.Num((V)((V)(R.Get(v_79, (V)"y") + R.Get(v_79, (V)"height")) - (V)3.0d)), step_83 = R.Num((V)1.0d);
for (double i_81 = R.Num((V)(R.Get(v_79, (V)"y") + (V)1.0d)); step_83 > 0 ? i_81 <= end_82 : i_81 >= end_82; i_81 += step_83) {
V v_80 = i_81;
{ double end_86 = R.Num((V)((V)(R.Get(v_79, (V)"x") + R.Get(v_79, (V)"width")) - (V)4.0d)), step_87 = R.Num((V)1.0d);
for (double i_85 = R.Num((V)(R.Get(v_79, (V)"x") + (V)1.0d)); step_87 > 0 ? i_85 <= end_86 : i_85 >= end_86; i_85 += step_87) {
V v_84 = i_85;
var values_88 = R.Join(new V[] {}, new Results((V)true));
V v_89 = values_88[0];
{ double end_92 = R.Num((V)(v_80 + (V)2.0d)), step_93 = R.Num((V)1.0d);
for (double i_91 = R.Num(v_80); step_93 > 0 ? i_91 <= end_92 : i_91 >= end_92; i_91 += step_93) {
V v_90 = i_91;
{ double end_96 = R.Num((V)(v_84 + (V)3.0d)), step_97 = R.Num((V)1.0d);
for (double i_95 = R.Num(v_84); step_97 > 0 ? i_95 <= end_96 : i_95 >= end_96; i_95 += step_97) {
V v_94 = i_95;
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {v_76, v_94}, new Results(v_90))).First)))) {
var values_98 = R.Join(new V[] {}, new Results((V)false));
v_89 = values_98[0];
}
foreach (var row_99 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_60, (V)"entities"))))))) {
V v_100 = row_99[0];
V v_101 = row_99[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_101, (V)"id") != R.Get(v_74, (V)"id")), () => R.Get(v_101, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_101, (V)"position"), (V)"x") == v_94)), () => (V)(R.Get(R.Get(v_101, (V)"position"), (V)"y") == v_90)))) {
var values_102 = R.Join(new V[] {}, new Results((V)false));
v_89 = values_102[0];
}
}
}
}
}
}
if (R.Truth(v_89)) {
var values_103 = R.Join(new V[] {v_84}, new Results(v_80));
var obj_104 = R.Get(v_74, (V)"position");
V key_105 = (V)"x";
var obj_106 = R.Get(v_74, (V)"position");
V key_107 = (V)"y";
R.Set(obj_104, key_105, values_103[0]);
R.Set(obj_106, key_107, values_103[1]);

R.Call(R.Get(v_13, (V)"recompute"), R.Join(new V[] {}, new Results(v_60)));

return R.Join(new V[] {v_60, v_74, v_76}, new Results(v_79));
}
}
}
}
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no clear prepared room")));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"interaction inspection, cancellation and invalid choices leave exact state and RNG intact"}, new Results(new V((RuleFunction)(args_108 => {

var values_109 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"offering_font"))));
V v_110 = values_109[0];
V v_111 = values_109[1];
V v_112 = values_109[2];

var values_113 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_110))));
V v_114 = values_113[0];
{ double end_117 = R.Num((V)4.0d), step_118 = R.Num((V)1.0d);
for (double i_116 = R.Num((V)1.0d); step_118 > 0 ? i_116 <= end_117 : i_116 >= end_117; i_116 += step_118) {
V v_115 = i_116;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_110}, new Results(R.Get(v_112, (V)"id")))).First, (V)"entries")) == (V)3.0d))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_110}, new Results(R.Table(new Entry((V)"kind", (V)"cancel"))))).First)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_110}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_112, (V)"id")), new Entry((V)"choice_id", (V)"99"))))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_110}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", (V)"../bad"), new Entry((V)"choice_id", (V)"1"))))).First)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_110))).First}, new Results(v_114)));
var values_119 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_120 = R.Get(v_111, (V)"position");
V key_121 = (V)"x";
R.Set(obj_120, key_121, values_119[0]);

var values_122 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_123 = R.Get(v_111, (V)"position");
V key_124 = (V)"y";
R.Set(obj_123, key_124, values_122[0]);

R.Call(R.Get(v_13, (V)"recompute"), R.Join(new V[] {}, new Results(v_110)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_110}, new Results(R.Get(v_112, (V)"id")))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"offering font pays nonlethal exact HP once and grants a cached class-eligible boon"}, new Results(new V((RuleFunction)(args_125 => {

var values_126 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"offering_font"))));
V v_127 = values_126[0];
V v_128 = values_126[1];
V v_129 = values_126[2];

var values_130 = R.Join(new V[] {}, new Results((V)18.0d));
var obj_131 = v_128;
V key_132 = (V)"hp";
R.Set(obj_131, key_132, values_130[0]);

var values_133 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_129, (V)"offers")))));
V v_134 = values_133[0];

var values_135 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_127, (V)"rng"), (V)"state")));
V v_136 = values_135[0];
var values_137 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_127}, new Results(R.Get(v_129, (V)"id")))).First, (V)"entries"), (V)1.0d)));
V v_138 = values_137[0];

var values_139 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_138, (V)"offer"), (V)"boon_id")));
V v_140 = values_139[0];

var values_141 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_138, (V)"offer"), (V)"rarity")));
V v_142 = values_141[0];
var values_143 = R.Join(new V[] {}, R.Call(v_35, R.Join(new V[] {v_127, v_129}, new Results((V)"1"))));
V v_144 = values_143[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_128, (V)"hp") == (V)13.0d), () => (V)(R.Get(R.Get(R.Get(v_128, (V)"boons"), v_140), v_142) == (V)1.0d)), () => R.Get(v_129, (V)"spent")), () => (V)(R.Get(v_127, (V)"clock") == (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(v_129, (V)"offers")))).First == v_134), () => (V)(R.Get(R.Get(v_127, (V)"rng"), (V)"state") == v_136)), () => (V)(R.Call(v_46, R.Join(new V[] {v_144}, new Results((V)"BoonGranted"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_127}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_129, (V)"id")), new Entry((V)"choice_id", (V)"2"))))).First)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_127)))))));
var values_145 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"offering_font"))));
V v_146 = values_145[0];
V v_147 = values_145[1];
V v_148 = values_145[2];

var values_149 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_150 = v_147;
V key_151 = (V)"hp";
R.Set(obj_150, key_151, values_149[0]);

var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_146))));
V v_153 = values_152[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_146}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_148, (V)"id")), new Entry((V)"choice_id", (V)"1"))))).First)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_146))).First}, new Results(v_153)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ration and camp recovery is capped, finite, and refuses a no-op full-health choice"}, new Results(new V((RuleFunction)(args_154 => {

foreach (var row_155 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"ration_cache"), new Entry((V)2, (V)"camp")))))))) {
V v_156 = row_155[0];
V v_157 = row_155[1];
var values_158 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results(v_157))));
V v_159 = values_158[0];
V v_160 = values_158[1];
V v_161 = values_158[2];

var values_162 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_159))));
V v_163 = values_162[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_159}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_161, (V)"id")), new Entry((V)"choice_id", (V)"food"))))).First)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_159))).First}, new Results(v_163)));
var values_164 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_165 = v_160;
V key_166 = (V)"hp";
R.Set(obj_165, key_166, values_164[0]);

R.Call(v_35, R.Join(new V[] {v_159, v_161}, new Results((V)"food")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_160, (V)"hp") == R.Or(R.And((V)(v_157 == (V)"camp"), () => (V)16.0d), () => (V)14.0d)), () => R.Get(v_161, (V)"spent")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_159}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_161, (V)"id")), new Entry((V)"choice_id", (V)"ward"))))).First)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_159)))))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a supplied ward blocks exactly one real hit in a simultaneous enemy burst"}, new Results(new V((RuleFunction)(args_167 => {

var values_168 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"ration_cache"))));
V v_169 = values_168[0];
V v_170 = values_168[1];
V v_171 = values_168[2];
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_169, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)0.0d)));

R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_169, (V)"core:enemy/mossbound_guard", (V)6.0d, (V)6.0d}, new Results((V)0.0d)));
R.Call(v_35, R.Join(new V[] {v_169, v_171}, new Results((V)"ward")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_170, (V)"pickup_ward_actions") == (V)3.0d))));
var values_172 = R.Join(new V[] {}, new Results(R.Get(v_170, (V)"hp")));
V v_173 = values_172[0];

var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_169}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_175 = values_174[0];
V v_176 = values_174[1];
V v_177 = values_174[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_46, R.Join(new V[] {v_177}, new Results((V)"WardFlaskBlocked"))).First == (V)1.0d), () => (V)(R.Get(v_170, (V)"hp") < v_173)), () => (V)(R.Get(v_170, (V)"pickup_ward_actions") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_169)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"well focus matches attack preview, applies to one primary batch and expires after three actions"}, new Results(new V((RuleFunction)(args_178 => {

var values_179 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"charm_well"))));
V v_180 = values_179[0];
V v_181 = values_179[1];
V v_182 = values_179[2];

R.Call(v_35, R.Join(new V[] {v_180, v_182}, new Results((V)"focus")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_181, (V)"supply_focus_actions") == (V)3.0d))));
var values_183 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_180, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_184 = values_183[0];
var values_185 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"effective_ability"), R.Join(new V[] {v_180, (V)"basic_attack", (V)1.0d, (V)0.0d}, new Results((V)true))));
V v_186 = values_185[0];
V v_187 = values_185[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_187 == (V)(R.Get(v_181, (V)"damage") + (V)2.0d)))));
var values_188 = R.Join(new V[] {}, new Results(R.Get(v_184, (V)"hp")));
V v_189 = values_188[0];

var values_190 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_180}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_191 = values_190[0];
V v_192 = values_190[1];
V v_193 = values_190[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_184, (V)"hp") == (V)(v_189 - R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(v_187 - R.Get(v_184, (V)"defense"))))).First)), () => (V)(R.Get(v_181, (V)"supply_focus_actions") == (V)0.0d)), () => (V)(R.Call(v_46, R.Join(new V[] {v_193}, new Results((V)"SupplyFocusSpent"))).First == (V)1.0d)))));
var values_194 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"charm_well"))));
V v_195 = values_194[0];
V v_196 = values_194[1];
V v_197 = values_194[2];

R.Call(v_35, R.Join(new V[] {v_195, v_197}, new Results((V)"focus")));
{ double end_200 = R.Num((V)3.0d), step_201 = R.Num((V)1.0d);
for (double i_199 = R.Num((V)1.0d); step_201 > 0 ? i_199 <= end_200 : i_199 >= end_200; i_199 += step_201) {
V v_198 = i_199;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_195}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_196, (V)"supply_focus_actions") == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"focus applies to every member of one simultaneous primary batch but not its delayed echo"}, new Results(new V((RuleFunction)(args_202 => {

var values_203 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"charm_well"))));
V v_204 = values_203[0];
V v_205 = values_203[1];
V v_206 = values_203[2];

R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_204, R.Get(v_205, (V)"id"), (V)"core:boon/temporal_echo", (V)"common"}, new Results((V)1.0d)));
R.Call(v_35, R.Join(new V[] {v_204, v_206}, new Results((V)"focus")));
foreach (var row_207 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)5.0d), new Entry((V)2, (V)6.0d), new Entry((V)3, (V)7.0d)))))))) {
V v_208 = row_207[0];
V v_209 = row_207[1];
var values_210 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_204, (V)"core:enemy/mossbound_guard", (V)8.0d, v_209}, new Results((V)100000.0d))));
V v_211 = values_210[0];

var values_212 = R.Join(new V[] {}, new Results((V)40.0d));
var obj_213 = v_211;
V key_214 = (V)"hp";
R.Set(obj_213, key_214, values_212[0]);

var values_215 = R.Join(new V[] {}, new Results((V)40.0d));
var obj_216 = v_211;
V key_217 = (V)"max_hp";
R.Set(obj_216, key_217, values_215[0]);
}
var values_218 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"effective_ability"), R.Join(new V[] {v_204, (V)"sweeping_slash", (V)1.0d, (V)0.0d}, new Results((V)true))));
V v_219 = values_218[0];
V v_220 = values_218[1];
var values_221 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_204}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_222 = values_221[0];
V v_223 = values_221[1];
V v_224 = values_221[2];

var values_225 = R.Join(new V[] {}, new Results((V)0.0d));
V v_226 = values_225[0];
foreach (var row_227 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_224)))))) {
V v_228 = row_227[0];
V v_229 = row_227[1];
if (R.Truth(R.And((V)(R.Get(v_229, (V)"kind") == (V)"DamageAttempted"), () => R.Get(v_229, (V)"direct_attack")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_229, (V)"attempted_damage") == v_220))));

var values_230 = R.Join(new V[] {}, new Results((V)(v_226 + (V)1.0d)));
v_226 = values_230[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_226 == (V)3.0d), () => (V)(R.Call(v_46, R.Join(new V[] {v_224}, new Results((V)"SupplyFocusSpent"))).First == (V)1.0d)), () => (V)(R.Get(v_205, (V)"supply_focus_actions") == (V)0.0d)))));
var values_231 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_204}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_232 = values_231[0];
V v_233 = values_231[1];
V v_234 = values_231[2];

var values_235 = R.Join(new V[] {}, new Results((V)0.0d));
V v_236 = values_235[0];
foreach (var row_237 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_234)))))) {
V v_238 = row_237[0];
V v_239 = row_237[1];
if (R.Truth(R.And((V)(R.Get(v_239, (V)"kind") == (V)"DamageAttempted"), () => (V)(R.Get(v_239, (V)"damage_type") == (V)"echo")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_239, (V)"attempted_damage") == (V)1.0d))));

var values_240 = R.Join(new V[] {}, new Results((V)(v_236 + (V)1.0d)));
v_236 = values_240[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_236 == (V)3.0d), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_204))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"utility collection has one slot and requires explicit replacement without losing a charge on refusal"}, new Results(new V((RuleFunction)(args_241 => {

var values_242 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"cleanse_flask"))));
V v_243 = values_242[0];
V v_244 = values_242[1];
V v_245 = values_242[2];

R.Call(v_35, R.Join(new V[] {v_243, v_245}, new Results((V)"take")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_244, (V)"utility"), (V)"kind") == (V)"cleanse_flask"))));
var values_246 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_243, (V)"escape_scroll", R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_243))).First, (V)8.0d}, new Results((V)6.0d))));
V v_247 = values_246[0];

var values_248 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_243))));
V v_249 = values_248[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_243}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_247, (V)"id")), new Entry((V)"choice_id", (V)"take"))))).First)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_243))).First}, new Results(v_249)));
R.Call(v_35, R.Join(new V[] {v_243, v_247, (V)"take", V.Nil}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_244, (V)"utility"), (V)"kind") == (V)"escape_scroll"), () => R.Get(v_245, (V)"spent")), () => R.Get(v_247, (V)"spent")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_243)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"cleanse removes actual Burning and only its pending tick; unused healthy doses are retained"}, new Results(new V((RuleFunction)(args_250 => {

var values_251 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"cleanse_flask"))));
V v_252 = values_251[0];
V v_253 = values_251[1];
V v_254 = values_251[2];

R.Call(v_35, R.Join(new V[] {v_252, v_254}, new Results((V)"take")));
var values_255 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_252))));
V v_256 = values_255[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_252}, new Results(R.Table(new Entry((V)"kind", (V)"use_utility"))))).First)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_252))).First}, new Results(v_256)));
var values_257 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_252, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results(R.Get(v_252, (V)"clock")))));
V v_258 = values_257[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_252, R.Get(v_258, (V)"id"), (V)"core:boon/emberbrand", (V)"common"}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_252}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_252}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_253, (V)"statuses"), (V)"burning"))));
var values_259 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_253, (V)"statuses"), (V)"burning"), (V)"schedule_id")));
V v_260 = values_259[0];

var values_261 = R.Join(new V[] {}, new Results(R.Get(v_253, (V)"hp")));
V v_262 = values_261[0];
var values_263 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_252}, new Results(R.Table(new Entry((V)"kind", (V)"use_utility"))))));
V v_264 = values_263[0];
V v_265 = values_263[1];
V v_266 = values_263[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_264}, new Results(v_265)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_253, (V)"utility"))), () => (V)(!R.Truth(R.Get(R.Get(v_253, (V)"statuses"), (V)"burning")))), () => (V)(R.Get(v_253, (V)"hp") == v_262)), () => (V)(R.Call(v_46, R.Join(new V[] {v_266}, new Results((V)"StatusCleansed"))).First == (V)1.0d)))));
foreach (var row_267 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_252, (V)"schedule"))))))) {
V v_268 = row_267[0];
V v_269 = row_267[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_269, (V)"id") != v_260))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_252)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"camp preparation cleanses Burning while retaining exactly three future ward actions"}, new Results(new V((RuleFunction)(args_270 => {

var values_271 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"camp"))));
V v_272 = values_271[0];
V v_273 = values_271[1];
V v_274 = values_271[2];

var values_275 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_272, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)0.0d))));
V v_276 = values_275[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_272, R.Get(v_276, (V)"id"), (V)"core:boon/emberbrand", (V)"common"}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_272}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_272}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_273, (V)"statuses"), (V)"burning"))));
R.Call(v_35, R.Join(new V[] {v_272, v_274}, new Results((V)"ward")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(R.Get(v_273, (V)"statuses"), (V)"burning"))), () => (V)(R.Get(v_273, (V)"pickup_ward_actions") == (V)3.0d)), () => R.Get(v_274, (V)"spent")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"escape scroll previews a chosen exact path, consumes one charge and emits each entered tile"}, new Results(new V((RuleFunction)(args_277 => {

var values_278 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"escape_scroll"))));
V v_279 = values_278[0];
V v_280 = values_278[1];
V v_281 = values_278[2];

R.Call(v_35, R.Join(new V[] {v_279, v_281}, new Results((V)"take")));

var values_282 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_279))));
V v_283 = values_282[0];
var values_284 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_279, (V)"use_utility", (V)1.0d, (V)0.0d}, new Results((V)3.0d))));
V v_285 = values_284[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_285, (V)"valid"), () => (V)((V)R.Length(R.Get(v_285, (V)"cells")) == (V)3.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_279))).First}, new Results(v_283)));
var values_286 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_279}, new Results(R.Table(new Entry((V)"kind", (V)"use_utility"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"steps", (V)3.0d))))));
V v_287 = values_286[0];
V v_288 = values_286[1];
V v_289 = values_286[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_287}, new Results(v_288)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_280, (V)"position"), (V)"x") == (V)10.0d), () => (V)(R.Get(R.Get(v_280, (V)"position"), (V)"y") == (V)6.0d)), () => (V)(!R.Truth(R.Get(v_280, (V)"utility")))), () => (V)(R.Call(v_46, R.Join(new V[] {v_289}, new Results((V)"TileEntered"))).First == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_279)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"escape refuses occupied, unseen, wall and invalid-range paths with exact no-op state"}, new Results(new V((RuleFunction)(args_290 => {

foreach (var row_291 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"occupied"), new Entry((V)2, (V)"unseen"), new Entry((V)3, (V)"wall"), new Entry((V)4, (V)"steps")))))))) {
V v_292 = row_291[0];
V v_293 = row_291[1];
var values_294 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"escape_scroll"))));
V v_295 = values_294[0];
V v_296 = values_294[1];
V v_297 = values_294[2];

R.Call(v_35, R.Join(new V[] {v_295, v_297}, new Results((V)"take")));
var values_298 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"use_utility"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"steps", (V)3.0d))));
V v_299 = values_298[0];
if (R.Truth((V)(v_293 == (V)"occupied"))) {
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_295, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)6.0d}, new Results((V)100000.0d)));
} else if (R.Truth((V)(v_293 == (V)"unseen"))) {
R.Call(R.Get(v_13, (V)"recompute"), R.Join(new V[] {v_295}, new Results((V)1.0d)));
} else if (R.Truth((V)(v_293 == (V)"wall"))) {
var values_300 = R.Join(new V[] {}, new Results((V)6.0d));
var obj_301 = R.Get(v_296, (V)"position");
V key_302 = (V)"x";
R.Set(obj_301, key_302, values_300[0]);

var values_303 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_304 = v_299;
V key_305 = (V)"dx";
R.Set(obj_304, key_305, values_303[0]);

var values_306 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_307 = v_299;
V key_308 = (V)"dy";
R.Set(obj_307, key_308, values_306[0]);
} else {
var values_309 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_310 = v_299;
V key_311 = (V)"steps";
R.Set(obj_310, key_311, values_309[0]);
}
var values_312 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_295))));
V v_313 = values_312[0];

R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_295}, new Results(v_299))).First))}, new Results(v_293)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_295))).First}, new Results(v_313)));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"trader stock and prices persist; failed purchases do not spend gold or reroll"}, new Results(new V((RuleFunction)(args_314 => {

var values_315 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"travelling_trader"))));
V v_316 = values_315[0];
V v_317 = values_315[1];
V v_318 = values_315[2];

var values_319 = R.Join(new V[] {}, new Results(R.Get(v_318, (V)"stock")));
V v_320 = values_319[0];

var values_321 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_316))));
V v_322 = values_321[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_316}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_318, (V)"id")), new Entry((V)"choice_id", (V)"utility"))))).First)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_316))).First}, new Results(v_322)));
var values_323 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_324 = v_316;
V key_325 = (V)"currency";
R.Set(obj_324, key_325, values_323[0]);

var values_326 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_320 == (V)"cleanse_flask"), () => (V)2.0d), () => (V)3.0d)));
V v_327 = values_326[0];
var values_328 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_316))))));
V v_329 = values_328[0];

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_316}, new Results(R.Get(v_318, (V)"id")))).First}, R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_329}, new Results(R.Get(v_318, (V)"id"))))));
R.Call(v_35, R.Join(new V[] {v_316, v_318}, new Results((V)"utility")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_316, (V)"currency") == (V)((V)5.0d - v_327)), () => (V)(R.Get(R.Get(v_317, (V)"utility"), (V)"kind") == v_320)), () => R.Get(v_318, (V)"spent")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_316}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_318, (V)"id")), new Entry((V)"choice_id", (V)"food"))))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"forge trades exactly one selected rarity, same-rarity replacement and fixed saved alternatives"}, new Results(new V((RuleFunction)(args_330 => {

var values_331 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"attunement_forge"))));
V v_332 = values_331[0];
V v_333 = values_331[1];
V v_334 = values_331[2];

var values_335 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_336 = v_332;
V key_337 = (V)"currency";
R.Set(obj_336, key_337, values_335[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_332, R.Get(v_333, (V)"id"), (V)"core:boon/crescent_reach", (V)"common"}, new Results((V)2.0d)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_332, R.Get(v_333, (V)"id"), (V)"core:boon/crescent_reach", (V)"rare"}, new Results((V)1.0d)));
var values_338 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:boon/crescent_reach"), new Entry((V)"rarity", (V)"common"))));
V v_339 = values_338[0];
var values_340 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_332))));
V v_341 = values_340[0];

var values_342 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_332, R.Get(v_334, (V)"id")}, new Results(v_339))));
V v_343 = values_342[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_343, (V)"entries")) == (V)3.0d), () => R.Invoke(R.Get(R.Get(R.Get(v_343, (V)"entries"), (V)1.0d), (V)"detail"), "find", R.Join(new V[] {}, new Results((V)"cells"))).First))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_332))).First}, new Results(v_341)));
var values_344 = Results.Empty;
V v_345 = values_344[0];

foreach (var row_346 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_343, (V)"entries"))))))) {
V v_347 = row_346[0];
V v_348 = row_346[1];
if (R.Truth(R.Get(v_348, (V)"enabled"))) {
var values_349 = R.Join(new V[] {}, new Results(v_348));
v_345 = values_349[0];

break;
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_345)));
var values_350 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_332))))));
V v_351 = values_350[0];

R.Call(v_3, R.Join(new V[] {v_343}, R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_351, R.Get(v_334, (V)"id")}, new Results(v_339)))));
R.Call(v_35, R.Join(new V[] {v_332, v_334, R.Get(v_345, (V)"id")}, new Results(v_339)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_332, (V)"currency") == (V)7.0d), () => (V)(R.Get(R.Get(R.Get(v_333, (V)"boons"), R.Get(v_339, (V)"id")), (V)"common") == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_333, (V)"boons"), R.Get(v_339, (V)"id")), (V)"rare") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(R.Get(v_333, (V)"boons"), R.Get(R.Get(v_345, (V)"offer"), (V)"boon_id")), (V)"common") == (V)1.0d), () => R.Get(v_334, (V)"spent")), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_332))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"memorial previews transformation removal, retires one stack, heals and preserves other stacks/stats"}, new Results(new V((RuleFunction)(args_352 => {

var values_353 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"memorial"))));
V v_354 = values_353[0];
V v_355 = values_353[1];
V v_356 = values_353[2];

var values_357 = R.Join(new V[] {}, new Results((V)6.0d));
var obj_358 = v_355;
V key_359 = (V)"hp";
R.Set(obj_358, key_359, values_357[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_354, R.Get(v_355, (V)"id"), (V)"core:boon/crescent_reach", (V)"common"}, new Results((V)1.0d)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_354, R.Get(v_355, (V)"id"), (V)"core:boon/emberbrand", (V)"rare"}, new Results((V)2.0d)));
var values_360 = R.Join(new V[] {R.Get(v_355, (V)"max_hp"), R.Get(v_355, (V)"damage")}, new Results(R.Get(v_355, (V)"speed")));
V v_361 = values_360[0];
V v_362 = values_360[1];
V v_363 = values_360[2];
var values_364 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_354}, new Results(R.Get(v_356, (V)"id")))));
V v_365 = values_364[0];

var values_366 = Results.Empty;
V v_367 = values_366[0];
foreach (var row_368 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_365, (V)"entries"))))))) {
V v_369 = row_368[0];
V v_370 = row_368[1];
if (R.Truth((V)(R.Get(R.Get(v_370, (V)"remove"), (V)"id") == (V)"core:boon/crescent_reach"))) {
var values_371 = R.Join(new V[] {}, new Results(v_370));
v_367 = values_371[0];
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_367, () => R.Invoke(R.Get(v_367, (V)"detail"), "find", R.Join(new V[] {}, new Results((V)"cells"))).First))));
R.Call(v_35, R.Join(new V[] {v_354, v_356}, new Results(R.Get(v_367, (V)"id"))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_355, (V)"hp") == (V)15.0d), () => (V)(!R.Truth(R.Get(R.Get(v_355, (V)"boons"), (V)"core:boon/crescent_reach")))), () => (V)(R.Get(R.Get(R.Get(v_355, (V)"boons"), (V)"core:boon/emberbrand"), (V)"rare") == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_355, (V)"max_hp") == v_361), () => (V)(R.Get(v_355, (V)"damage") == v_362)), () => (V)(R.Get(v_355, (V)"speed") == v_363)), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_354))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"retiring Temporal Echo preserves its already committed locked effect"}, new Results(new V((RuleFunction)(args_372 => {

var values_373 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"attunement_forge"))));
V v_374 = values_373[0];
V v_375 = values_373[1];
V v_376 = values_373[2];

var values_377 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_378 = v_374;
V key_379 = (V)"currency";
R.Set(obj_378, key_379, values_377[0]);

var values_380 = R.Join(new V[] {}, new Results((V)300.0d));
var obj_381 = v_375;
V key_382 = (V)"speed";
R.Set(obj_381, key_382, values_380[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_374, R.Get(v_375, (V)"id"), (V)"core:boon/temporal_echo", (V)"common"}, new Results((V)1.0d)));
var values_383 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_374, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_384 = values_383[0];

var values_385 = R.Join(new V[] {}, new Results((V)40.0d));
var obj_386 = v_384;
V key_387 = (V)"hp";
R.Set(obj_386, key_387, values_385[0]);

var values_388 = R.Join(new V[] {}, new Results((V)40.0d));
var obj_389 = v_384;
V key_390 = (V)"max_hp";
R.Set(obj_389, key_390, values_388[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_374}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_374, (V)"delayed_effects"))))));
var values_391 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:boon/temporal_echo"), new Entry((V)"rarity", (V)"common"))));
V v_392 = values_391[0];

var values_393 = Results.Empty;
V v_394 = values_393[0];
foreach (var row_395 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_374, R.Get(v_376, (V)"id")}, new Results(v_392))).First, (V)"entries"))))))) {
V v_396 = row_395[0];
V v_397 = row_395[1];
if (R.Truth(R.Get(v_397, (V)"enabled"))) {
var values_398 = R.Join(new V[] {}, new Results(v_397));
v_394 = values_398[0];

break;
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_394)));
R.Call(v_35, R.Join(new V[] {v_374, v_376, R.Get(v_394, (V)"id")}, new Results(v_392)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(R.Get(v_375, (V)"boons"), R.Get(v_392, (V)"id")))), () => R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_374, (V)"delayed_effects")))).First))));
var values_399 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_374))))));
V v_400 = values_399[0];

var values_401 = R.Join(new V[] {}, new Results((V)0.0d));
V v_402 = values_401[0];
{ double end_405 = R.Num((V)5.0d), step_406 = R.Num((V)1.0d);
for (double i_404 = R.Num((V)1.0d); step_406 > 0 ? i_404 <= end_405 : i_404 >= end_405; i_404 += step_406) {
V v_403 = i_404;
var values_407 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_374}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_408 = values_407[0];
V v_409 = values_407[1];
V v_410 = values_407[2];

var values_411 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_400}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_412 = values_411[0];
V v_413 = values_411[1];
V v_414 = values_411[2];

R.Call(v_3, R.Join(new V[] {v_410}, new Results(v_414)));

var values_415 = R.Join(new V[] {}, new Results((V)(v_402 + R.Call(v_46, R.Join(new V[] {v_410}, new Results((V)"DelayedEffectResolved"))).First)));
v_402 = values_415[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_402 == (V)1.0d))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_374))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_400)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"return token uses two paid actions in one real room and never overwrites the Wayfinder beacon"}, new Results(new V((RuleFunction)(args_416 => {

var values_417 = R.Join(new V[] {}, R.Call(v_56, R.Join(new V[] {}, new Results((V)"core:actor/wayfinder"))));
V v_418 = values_417[0];
V v_419 = values_417[1];
V v_420 = values_417[2];

var values_421 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_418, (V)"return_token", v_420, R.Get(R.Get(v_419, (V)"position"), (V)"x")}, new Results((V)(R.Get(R.Get(v_419, (V)"position"), (V)"y") + (V)1.0d)))));
V v_422 = values_421[0];
R.Call(v_35, R.Join(new V[] {v_418, v_422}, new Results((V)"take")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_418}, new Results(R.Table(new Entry((V)"kind", (V)"use_utility")))))));

var values_423 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_419, (V)"utility"), (V)"planted")));
V v_424 = values_423[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_424)));
var values_425 = R.Join(new V[] {R.Get(R.Get(v_419, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_419, (V)"position"), (V)"y")));
V v_426 = values_425[0];
V v_427 = values_425[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_418}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_418}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_418}, new Results(R.Table(new Entry((V)"kind", (V)"use_utility")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_419, (V)"position"), (V)"x") == v_426), () => (V)(R.Get(R.Get(v_419, (V)"position"), (V)"y") == v_427)), () => (V)(!R.Truth(R.Get(v_419, (V)"utility")))), () => (V)(!R.Truth(R.Get(R.Get(v_418, (V)"class_special"), (V)"beacon")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_418)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"token refuses blocked landing and expires on leaving its actual room by ordinary movement"}, new Results(new V((RuleFunction)(args_428 => {

var values_429 = R.Join(new V[] {}, R.Call(v_56, Results.Empty));
V v_430 = values_429[0];
V v_431 = values_429[1];
V v_432 = values_429[2];
V v_433 = values_429[3];

var values_434 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_430, (V)"return_token", v_432, R.Get(R.Get(v_431, (V)"position"), (V)"x")}, new Results((V)(R.Get(R.Get(v_431, (V)"position"), (V)"y") + (V)1.0d)))));
V v_435 = values_434[0];
R.Call(v_35, R.Join(new V[] {v_430, v_435}, new Results((V)"take")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_430}, new Results(R.Table(new Entry((V)"kind", (V)"use_utility")))))));

var values_436 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_431, (V)"utility"), (V)"planted")));
V v_437 = values_436[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_430}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

var values_438 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_430, (V)"core:enemy/mossbound_guard", R.Get(v_437, (V)"x"), R.Get(v_437, (V)"y")}, new Results((V)100000.0d))));
V v_439 = values_438[0];
var values_440 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_430))));
V v_441 = values_440[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_430}, new Results(R.Table(new Entry((V)"kind", (V)"use_utility"))))).First)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_430))).First}, new Results(v_441)));
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_430}, new Results(R.Get(v_439, (V)"id"))));

foreach (var row_442 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_430, (V)"entities"))))))) {
V v_443 = row_442[0];
V v_444 = row_442[1];
if (R.Truth((V)(R.Get(v_444, (V)"id") == R.Get(v_439, (V)"id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_430, (V)"entities")}, new Results(v_443)));

break;
}
}
var values_445 = R.Join(new V[] {}, new Results((V)false));
V v_446 = values_445[0];
{ double end_449 = R.Num((V)((V)(R.Get(v_433, (V)"y") + R.Get(v_433, (V)"height")) - (V)1.0d)), step_450 = R.Num((V)1.0d);
for (double i_448 = R.Num(R.Get(v_433, (V)"y")); step_450 > 0 ? i_448 <= end_449 : i_448 >= end_449; i_448 += step_450) {
V v_447 = i_448;
{ double end_453 = R.Num((V)((V)(R.Get(v_433, (V)"x") + R.Get(v_433, (V)"width")) - (V)1.0d)), step_454 = R.Num((V)1.0d);
for (double i_452 = R.Num(R.Get(v_433, (V)"x")); step_454 > 0 ? i_452 <= end_453 : i_452 >= end_453; i_452 += step_454) {
V v_451 = i_452;
foreach (var row_455 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_456 = row_455[0];
V v_457 = row_455[1];
var values_458 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"room_at"), R.Join(new V[] {v_432, (V)(v_451 + R.Get(v_457, (V)1.0d))}, new Results((V)(v_447 + R.Get(v_457, (V)2.0d))))));
V v_459 = values_458[0];
if (R.Truth(R.And(R.And(R.And((V)(!R.Truth(v_446)), () => R.Or((V)(!R.Truth(v_459)), () => (V)(R.Get(v_459, (V)"id") != R.Get(v_433, (V)"id")))), () => R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {v_432, (V)(v_451 + R.Get(v_457, (V)1.0d))}, new Results((V)(v_447 + R.Get(v_457, (V)2.0d))))).First), () => R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {v_432, v_451}, new Results(v_447))).First))) {
var values_460 = R.Join(new V[] {v_451}, new Results(v_447));
var obj_461 = R.Get(v_431, (V)"position");
V key_462 = (V)"x";
var obj_463 = R.Get(v_431, (V)"position");
V key_464 = (V)"y";
R.Set(obj_461, key_462, values_460[0]);
R.Set(obj_463, key_464, values_460[1]);

R.Call(R.Get(v_13, (V)"recompute"), R.Join(new V[] {}, new Results(v_430)));
var values_465 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_430, (V)"move", R.Get(v_457, (V)1.0d)}, new Results(R.Get(v_457, (V)2.0d)))));
V v_466 = values_465[0];

if (R.Truth(R.And(R.Get(v_466, (V)"valid"), () => (V)(R.Get(v_466, (V)"distance") == (V)1.0d)))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_430}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_457, (V)1.0d)), new Entry((V)"dy", R.Get(v_457, (V)2.0d))))))));

var values_467 = R.Join(new V[] {}, new Results((V)true));
v_446 = values_467[0];
}
}
}
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_446, () => (V)(!R.Truth(R.Get(v_431, (V)"utility")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_430)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"bell puzzle keeps fixed clue/progress through restore, resets wrong input and pays only once"}, new Results(new V((RuleFunction)(args_468 => {

var values_469 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {(V)"bell_clue"}, new Results(R.Table(new Entry((V)"group", (V)"test-bells"))))));
V v_470 = values_469[0];
V v_471 = values_469[1];
V v_472 = values_469[2];

var values_473 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"bells"), new Entry((V)"floor_id", R.Get(v_470, (V)"active_floor_id")), new Entry((V)"sequence", R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)3.0d))), new Entry((V)"progress", (V)0.0d), new Entry((V)"completed", (V)false))));
var obj_474 = R.Get(R.Get(v_470, (V)"idea_rooms"), (V)"groups");
V key_475 = (V)"test-bells";
R.Set(obj_474, key_475, values_473[0]);
var values_476 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_470))));
V v_477 = values_476[0];

var values_478 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_470, (V)"bell", v_477, (V)8.0d, (V)6.0d}, new Results(R.Table(new Entry((V)"group", (V)"test-bells"), new Entry((V)"bell", (V)1.0d))))).First), new Entry((V)2, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_470, (V)"bell", v_477, (V)6.0d, (V)6.0d}, new Results(R.Table(new Entry((V)"group", (V)"test-bells"), new Entry((V)"bell", (V)2.0d))))).First), new Entry(3, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_470, (V)"bell", v_477, (V)7.0d, (V)7.0d}, new Results(R.Table(new Entry((V)"group", (V)"test-bells"), new Entry((V)"bell", (V)3.0d)))))))));
V v_479 = values_478[0];
var values_480 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_470))));
V v_481 = values_480[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Get(R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_470}, new Results(R.Get(v_472, (V)"id")))).First, (V)"detail"), "find", R.Join(new V[] {}, new Results((V)"2")))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_470))).First}, new Results(v_481)));
R.Call(v_35, R.Join(new V[] {v_470, R.Get(v_479, (V)1.0d)}, new Results((V)"ring")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Get(v_470, (V)"idea_rooms"), (V)"groups"), (V)"test-bells"), (V)"progress") == (V)0.0d))));
R.Call(v_35, R.Join(new V[] {v_470, R.Get(v_479, (V)2.0d)}, new Results((V)"ring")));

var values_482 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_470))))));
V v_483 = values_482[0];

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_483))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_470)))));
R.Call(v_35, R.Join(new V[] {v_470, R.Get(v_479, (V)1.0d)}, new Results((V)"ring")));

R.Call(v_35, R.Join(new V[] {v_470, R.Get(v_479, (V)3.0d)}, new Results((V)"ring")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_470, (V)"currency") == (V)5.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_470}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(R.Get(v_479, (V)3.0d), (V)"id")), new Entry((V)"choice_id", (V)"ring"))))).First)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_470)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"falling fixture locks exact cells, hits allies/player normally and resolves before same-time readiness"}, new Results(new V((RuleFunction)(args_484 => {

var values_485 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {(V)"falling_fixture"}, new Results(R.Table(new Entry((V)"center_x", (V)8.0d), new Entry((V)"center_y", (V)6.0d), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)2, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)5.0d))), new Entry((V)3, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)4, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)7.0d))), new Entry((V)5, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)6.0d))))))))));
V v_486 = values_485[0];
V v_487 = values_485[1];
V v_488 = values_485[2];
var values_489 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_486, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_490 = values_489[0];

var values_491 = R.Join(new V[] {}, new Results(R.Get(v_490, (V)"hp")));
V v_492 = values_491[0];

var values_493 = R.Join(new V[] {}, new Results(R.Get(v_487, (V)"hp")));
V v_494 = values_493[0];
var values_495 = R.Join(new V[] {}, R.Call(v_35, R.Join(new V[] {v_486, v_488}, new Results((V)"trigger"))));
V v_496 = values_495[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_490, (V)"hp") == (V)(v_492 - R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)((V)4.0d - R.Get(v_490, (V)"defense"))))).First)), () => (V)(R.Get(v_487, (V)"hp") == (V)(v_494 - (V)4.0d))), () => R.Get(v_488, (V)"spent")), () => (V)(!R.Truth(R.Get(v_488, (V)"pending")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_46, R.Join(new V[] {v_496}, new Results((V)"FixtureLocked"))).First == (V)1.0d), () => (V)(R.Call(v_46, R.Join(new V[] {v_496}, new Results((V)"FixtureFell"))).First == (V)1.0d)))));
foreach (var row_497 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_496)))))) {
V v_498 = row_497[0];
V v_499 = row_497[1];
if (R.Truth((V)(R.Get(v_499, (V)"kind") == (V)"FixtureFell"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_499, (V)"time") == (V)100.0d))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_486)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"fast fixture commitment parks/rebases and exact pending cells survive Continue"}, new Results(new V((RuleFunction)(args_500 => {

var values_501 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {(V)"falling_fixture"}, new Results(R.Table(new Entry((V)"center_x", (V)8.0d), new Entry((V)"center_y", (V)6.0d), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)2, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)5.0d))), new Entry((V)3, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)4, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)7.0d))), new Entry((V)5, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)6.0d))))))))));
V v_502 = values_501[0];
V v_503 = values_501[1];
V v_504 = values_501[2];
var values_505 = R.Join(new V[] {}, new Results((V)300.0d));
var obj_506 = v_503;
V key_507 = (V)"speed";
R.Set(obj_506, key_507, values_505[0]);

R.Call(v_35, R.Join(new V[] {v_502, v_504}, new Results((V)"trigger")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_504, (V)"pending"), () => (V)(R.Get(R.Get(v_504, (V)"pending"), (V)"due") == (V)100.0d)), () => (V)(R.Get(v_502, (V)"clock") == (V)33.0d)))));
var values_508 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))));
V v_509 = values_508[0];

R.Call(R.Get(v_509, (V)"park"), R.Join(new V[] {v_502}, new Results(R.Get(v_502, (V)"active_floor_id"))));

var values_510 = R.Join(new V[] {}, new Results((V)(R.Get(v_502, (V)"clock") + (V)1000.0d)));
var obj_511 = v_502;
V key_512 = (V)"clock";
R.Set(obj_511, key_512, values_510[0]);

R.Call(R.Get(v_509, (V)"resume"), R.Join(new V[] {v_502}, new Results(R.Get(v_502, (V)"active_floor_id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_504, (V)"pending"), (V)"due") == (V)1100.0d))));

var values_513 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_502))))))))));
V v_514 = values_513[0];
{ double end_517 = R.Num((V)3.0d), step_518 = R.Num((V)1.0d);
for (double i_516 = R.Num((V)1.0d); step_518 > 0 ? i_516 <= end_517 : i_516 >= end_517; i_516 += step_518) {
V v_515 = i_516;
var values_519 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_502}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_520 = values_519[0];
V v_521 = values_519[1];
V v_522 = values_519[2];

var values_523 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_514}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_524 = values_523[0];
V v_525 = values_523[1];
V v_526 = values_523[2];

R.Call(v_3, R.Join(new V[] {v_522}, new Results(v_526)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_504, (V)"pending"))))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_502))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_514)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"sign inspection gives only a fixed coarse hint with exact no-op state"}, new Results(new V((RuleFunction)(args_527 => {

var values_528 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {(V)"route_sign"}, new Results(R.Table(new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"hint", (V)"Treasure"))))));
V v_529 = values_528[0];
V v_530 = values_528[1];
V v_531 = values_528[2];

var values_532 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_529))));
V v_533 = values_532[0];
var values_534 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_529}, new Results(R.Get(v_531, (V)"id")))));
V v_535 = values_534[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(R.Get(v_535, (V)"detail"), "find", R.Join(new V[] {}, new Results((V)"Treasure east"))).First, () => (V)((V)R.Length(R.Get(v_535, (V)"entries")) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_529}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_531, (V)"id")), new Entry((V)"choice_id", (V)"read"))))).First)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_529))).First}, new Results(v_533)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"contract rewards one voluntary legal kill sequence and never disables movement controls"}, new Results(new V((RuleFunction)(args_536 => {

var values_537 = R.Join(new V[] {}, R.Call(v_56, Results.Empty));
V v_538 = values_537[0];
V v_539 = values_537[1];
V v_540 = values_537[2];
V v_541 = values_537[3];

var values_542 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_538, (V)"core:enemy/mossbound_guard", (V)(R.Get(R.Get(v_539, (V)"position"), (V)"x") + (V)1.0d), R.Get(R.Get(v_539, (V)"position"), (V)"y")}, new Results((V)100000.0d))));
V v_543 = values_542[0];

var values_544 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_545 = v_543;
V key_546 = (V)"hp";
R.Set(obj_545, key_546, values_544[0]);
var values_547 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_538, (V)"archive_contract", v_540, R.Get(R.Get(v_539, (V)"position"), (V)"x"), (V)(R.Get(R.Get(v_539, (V)"position"), (V)"y") + (V)1.0d)}, new Results(R.Table(new Entry((V)"room_id", R.Get(v_541, (V)"id")), new Entry((V)"room", v_541), new Entry((V)"targets", R.Table(new Entry((V)1, R.Get(v_543, (V)"id")))), new Entry((V)"status", (V)"offered"), new Entry((V)"defeated", R.Table()))))));
V v_548 = values_547[0];
R.Call(v_35, R.Join(new V[] {v_538, v_548}, new Results((V)"accept")));

var values_549 = R.Join(new V[] {}, new Results(R.Get(v_538, (V)"currency")));
V v_550 = values_549[0];

var values_551 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_538}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_552 = values_551[0];
V v_553 = values_551[1];
V v_554 = values_551[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_548, (V)"status") == (V)"completed"), () => (V)(R.Get(v_538, (V)"currency") == (V)(v_550 + (V)8.0d))), () => (V)(R.Call(v_46, R.Join(new V[] {v_554}, new Results((V)"ContractChanged"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_538}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_548, (V)"id")), new Entry((V)"choice_id", (V)"accept"))))).First)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_538)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"interaction population changes only its bounded store, not RNG, entities, scheduler or geometry"}, new Results(new V((RuleFunction)(args_555 => {

var values_556 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {(V)9.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_557 = values_556[0];

var values_558 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_557))));
V v_559 = values_558[0];
var values_560 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"clock", R.Get(v_557, (V)"clock")), new Entry((V)"rng", R.Get(v_557, (V)"rng")), new Entry((V)"entities", R.Get(v_557, (V)"entities")), new Entry((V)"schedule", R.Get(v_557, (V)"schedule")), new Entry((V)"map", v_559))));
V v_561 = values_560[0];
var values_562 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_561))));
V v_563 = values_562[0];

var values_564 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"version", (V)1.0d), new Entry((V)"nodes", R.Table()), new Entry((V)"groups", R.Table()))));
var obj_565 = v_557;
V key_566 = (V)"idea_rooms";
R.Set(obj_565, key_566, values_564[0]);
R.Call(R.Get(v_7, (V)"populate"), R.Join(new V[] {v_557}, new Results(v_559)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"clock", R.Get(v_557, (V)"clock")), new Entry((V)"rng", R.Get(v_557, (V)"rng")), new Entry((V)"entities", R.Get(v_557, (V)"entities")), new Entry((V)"schedule", R.Get(v_557, (V)"schedule")), new Entry((V)"map", v_559))))).First == v_563))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_557, (V)"idea_rooms"), (V)"nodes")) >= (V)3.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"legitimate font and memorial use preserve achievement eligibility and do not mislabel rest as a food pickup"}, new Results(new V((RuleFunction)(args_567 => {

var values_568 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"offering_font"))));
V v_569 = values_568[0];
V v_570 = values_568[1];
V v_571 = values_568[2];

var values_572 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievements"))).First, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"finite-service-test"))));
var obj_573 = v_569;
V key_574 = (V)"meta";
R.Set(obj_573, key_574, values_572[0]);
var values_575 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_571, (V)"offers"), (V)1.0d)));
V v_576 = values_575[0];

R.Call(v_35, R.Join(new V[] {v_569, v_571}, new Results((V)"1")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_569, (V)"meta"), (V)"eligible"), () => R.Get(R.Get(v_570, (V)"boons"), R.Get(v_576, (V)"boon_id"))))));
var values_577 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_569, (V)"memorial", R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_569))).First, (V)8.0d}, new Results((V)6.0d))));
V v_578 = values_577[0];
var values_579 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_569}, new Results(R.Get(v_578, (V)"id")))).First, (V)"entries"), (V)1.0d)));
V v_580 = values_579[0];
var values_581 = R.Join(new V[] {}, R.Call(v_35, R.Join(new V[] {v_569, v_578}, new Results(R.Get(v_580, (V)"id")))));
V v_582 = values_581[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(R.Get(v_569, (V)"meta"), (V)"eligible"), () => (V)(R.Call(v_46, R.Join(new V[] {v_582}, new Results((V)"Healed"))).First == (V)1.0d)), () => (V)(R.Call(v_46, R.Join(new V[] {v_582}, new Results((V)"SupplyCollected"))).First == (V)0.0d)), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_569))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a contract fails on actual Dash while the ordinary movement still commits"}, new Results(new V((RuleFunction)(args_583 => {

var values_584 = R.Join(new V[] {}, R.Call(v_56, Results.Empty));
V v_585 = values_584[0];
V v_586 = values_584[1];
V v_587 = values_584[2];
V v_588 = values_584[3];

var values_589 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_585, (V)"core:enemy/mossbound_guard", (V)(R.Get(R.Get(v_586, (V)"position"), (V)"x") + (V)3.0d), (V)(R.Get(R.Get(v_586, (V)"position"), (V)"y") + (V)2.0d)}, new Results((V)100000.0d))));
V v_590 = values_589[0];
var values_591 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_585, (V)"archive_contract", v_587, R.Get(R.Get(v_586, (V)"position"), (V)"x"), (V)(R.Get(R.Get(v_586, (V)"position"), (V)"y") + (V)1.0d)}, new Results(R.Table(new Entry((V)"room_id", R.Get(v_588, (V)"id")), new Entry((V)"room", v_588), new Entry((V)"targets", R.Table(new Entry((V)1, R.Get(v_590, (V)"id")))), new Entry((V)"status", (V)"offered"), new Entry((V)"defeated", R.Table()))))));
V v_592 = values_591[0];
R.Call(v_35, R.Join(new V[] {v_585, v_592}, new Results((V)"accept")));

var values_593 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_586, (V)"position"), (V)"x")));
V v_594 = values_593[0];
var values_595 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_585}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_596 = values_595[0];
V v_597 = values_595[1];
V v_598 = values_595[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_596}, new Results(v_597)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_586, (V)"position"), (V)"x") > v_594), () => (V)(R.Get(v_592, (V)"status") == (V)"failed")), () => (V)(R.Get(v_592, (V)"failure") == (V)"movement ability used")), () => (V)(R.Call(v_46, R.Join(new V[] {v_598}, new Results((V)"ContractChanged"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_585)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"room contract permits a nonmovement class deployment and has an explicit paid abandon"}, new Results(new V((RuleFunction)(args_599 => {

var values_600 = R.Join(new V[] {}, R.Call(v_56, R.Join(new V[] {}, new Results((V)"core:actor/spiritbinder"))));
V v_601 = values_600[0];
V v_602 = values_600[1];
V v_603 = values_600[2];
V v_604 = values_600[3];

var values_605 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_601, (V)"core:enemy/mossbound_guard", (V)(R.Get(R.Get(v_602, (V)"position"), (V)"x") + (V)3.0d), (V)(R.Get(R.Get(v_602, (V)"position"), (V)"y") + (V)2.0d)}, new Results((V)100000.0d))));
V v_606 = values_605[0];
var values_607 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_601, (V)"archive_contract", v_603, R.Get(R.Get(v_602, (V)"position"), (V)"x"), (V)(R.Get(R.Get(v_602, (V)"position"), (V)"y") + (V)1.0d)}, new Results(R.Table(new Entry((V)"room_id", R.Get(v_604, (V)"id")), new Entry((V)"room", v_604), new Entry((V)"targets", R.Table(new Entry((V)1, R.Get(v_606, (V)"id")))), new Entry((V)"status", (V)"offered"), new Entry((V)"defeated", R.Table()))))));
V v_608 = values_607[0];
R.Call(v_35, R.Join(new V[] {v_601, v_608}, new Results((V)"accept")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_601}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_608, (V)"status") == (V)"active"))));
R.Call(v_35, R.Join(new V[] {v_601, v_608}, new Results((V)"abandon")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_608, (V)"status") == (V)"failed"), () => (V)(R.Get(v_608, (V)"failure") == (V)"abandoned")), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_601))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stale forge selections, invalid stack metadata and insufficient funds are exact no-ops"}, new Results(new V((RuleFunction)(args_609 => {

var values_610 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"attunement_forge"))));
V v_611 = values_610[0];
V v_612 = values_610[1];
V v_613 = values_610[2];

R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_611, R.Get(v_612, (V)"id"), (V)"core:boon/emberbrand", (V)"common"}, new Results((V)1.0d)));
foreach (var row_614 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)false), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)"bad"), new Entry((V)4, R.Table(new Entry((V)"id", (V)"core:boon/missing"), new Entry((V)"rarity", (V)"common"))), new Entry((V)5, R.Table(new Entry((V)"id", (V)"core:boon/emberbrand"), new Entry((V)"rarity", (V)"legendary")))))))))) {
V v_615 = row_614[0];
V v_616 = row_614[1];
var values_617 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_611))));
V v_618 = values_617[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_611}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_613, (V)"id")), new Entry((V)"choice_id", (V)"1"), new Entry((V)"selection", v_616))))).First)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_611))).First}, new Results(v_618)));
}
var values_619 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_611))));
V v_620 = values_619[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_611}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_613, (V)"id")), new Entry((V)"choice_id", (V)"1"), new Entry((V)"selection", R.Table(new Entry((V)"id", (V)"core:boon/emberbrand"), new Entry((V)"rarity", (V)"common"))))))).First)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_611))).First}, new Results(v_620)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"malformed utility, font, forge, bell, fixture and contract snapshots reject before activation"}, new Results(new V((RuleFunction)(args_621 => {

var values_678 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_622 => {

var values_623 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"offering_font"))));
V v_624 = values_623[0];
V v_625 = values_623[1];
V v_626 = values_623[2];

var values_627 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_626, (V)"offers"), (V)1.0d)));
var obj_628 = R.Get(v_626, (V)"offers");
V key_629 = (V)2.0d;
R.Set(obj_628, key_629, values_627[0]);

return R.Join(new V[] {}, new Results(v_624));
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_630 => {

var values_631 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"attunement_forge"))));
V v_632 = values_631[0];
V v_633 = values_631[1];
V v_634 = values_631[2];

var values_635 = R.Join(new V[] {}, new Results((V)"../bad"));
var obj_636 = R.Get(R.Get(R.Get(v_634, (V)"trades"), (V)"core:boon/emberbrand"), (V)"common");
V key_637 = (V)1.0d;
R.Set(obj_636, key_637, values_635[0]);

return R.Join(new V[] {}, new Results(v_632));
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_638 => {

var values_639 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"cleanse_flask"))));
V v_640 = values_639[0];
V v_641 = values_639[1];
V v_642 = values_639[2];

R.Call(v_35, R.Join(new V[] {v_640, v_642}, new Results((V)"take")));

var values_643 = R.Join(new V[] {}, new Results((V)"unknown"));
var obj_644 = R.Get(v_641, (V)"utility");
V key_645 = (V)"kind";
R.Set(obj_644, key_645, values_643[0]);

return R.Join(new V[] {}, new Results(v_640));
return Results.Empty;
}))), new Entry((V)4, new V((RuleFunction)(args_646 => {

var values_647 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"ration_cache"))));
V v_648 = values_647[0];
V v_649 = values_647[1];
V v_650 = values_647[2];

var values_651 = R.Join(new V[] {}, new Results((-(V)1.0d)));
var obj_652 = v_649;
V key_653 = (V)"supply_focus_actions";
R.Set(obj_652, key_653, values_651[0]);

return R.Join(new V[] {}, new Results(v_648));
return Results.Empty;
}))), new Entry((V)5, new V((RuleFunction)(args_654 => {

var values_655 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"ration_cache"))));
V v_656 = values_655[0];
V v_657 = values_655[1];
V v_658 = values_655[2];

var values_659 = R.Join(new V[] {}, new Results((V)(R.Get(v_658, (V)"x") + (V)1.0d)));
var obj_660 = v_658;
V key_661 = (V)"x";
R.Set(obj_660, key_661, values_659[0]);

return R.Join(new V[] {}, new Results(v_656));
return Results.Empty;
}))), new Entry((V)6, new V((RuleFunction)(args_662 => {

var values_663 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"ration_cache"))));
V v_664 = values_663[0];
V v_665 = values_663[1];
V v_666 = values_663[2];

var values_667 = R.Join(new V[] {}, new Results((V)99.0d));
var obj_668 = R.Get(v_664, (V)"idea_rooms");
V key_669 = (V)"version";
R.Set(obj_668, key_669, values_667[0]);

return R.Join(new V[] {}, new Results(v_664));
return Results.Empty;
}))), new Entry((V)7, new V((RuleFunction)(args_670 => {

var values_671 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"ration_cache"))));
V v_672 = values_671[0];
V v_673 = values_671[1];
V v_674 = values_671[2];

var values_675 = R.Join(new V[] {}, new Results(v_674));
var obj_676 = R.Get(R.Get(v_672, (V)"idea_rooms"), (V)"nodes");
V key_677 = (V)2.0d;
R.Set(obj_676, key_677, values_675[0]);

return R.Join(new V[] {}, new Results(v_672));
return Results.Empty;
}))))));
V v_679 = values_678[0];
foreach (var row_680 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_679)))))) {
V v_681 = row_680[0];
V v_682 = row_680[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(v_682, Results.Empty))))).First)))));
}
var values_683 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {(V)"falling_fixture"}, new Results(R.Table(new Entry((V)"center_x", (V)8.0d), new Entry((V)"center_y", (V)6.0d), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)2, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)5.0d))), new Entry((V)3, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)6.0d))), new Entry((V)4, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)7.0d))), new Entry((V)5, R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)6.0d))))))))));
V v_684 = values_683[0];
V v_685 = values_683[1];
V v_686 = values_683[2];
var values_687 = R.Join(new V[] {}, new Results((V)300.0d));
var obj_688 = v_685;
V key_689 = (V)"speed";
R.Set(obj_688, key_689, values_687[0]);

R.Call(v_35, R.Join(new V[] {v_684, v_686}, new Results((V)"trigger")));

var values_690 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_684))));
V v_691 = values_690[0];
var values_692 = R.Join(new V[] {}, new Results((V)9.0d));
var obj_693 = R.Get(R.Get(R.Get(R.Get(R.Get(R.Get(v_691, (V)"idea_rooms"), (V)"nodes"), (V)1.0d), (V)"pending"), (V)"cells"), (V)1.0d);
V key_694 = (V)"x";
R.Set(obj_693, key_694, values_692[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_691))).First)))));
var values_695 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_684))));
v_691 = values_695[0];

foreach (var row_696 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_691, (V)"schedule"))))))) {
V v_697 = row_696[0];
V v_698 = row_696[1];
if (R.Truth(R.Get(v_698, (V)"idea_fixture"))) {
var values_699 = R.Join(new V[] {}, new Results((V)(R.Get(v_698, (V)"due") + (V)1.0d)));
var obj_700 = v_698;
V key_701 = (V)"due";
R.Set(obj_700, key_701, values_699[0]);
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_691))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"existing saves without the optional interaction store reopen without populating new rewards"}, new Results(new V((RuleFunction)(args_702 => {

var values_703 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {(V)9.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_704 = values_703[0];

var values_705 = R.Join(new V[] {}, new Results(V.Nil));
var obj_706 = v_704;
V key_707 = (V)"idea_rooms";
R.Set(obj_706, key_707, values_705[0]);
var values_708 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_704))));
V v_709 = values_708[0];

var values_710 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_709))))))));
V v_711 = values_710[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_711, (V)"idea_rooms") == V.Nil))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_711))).First}, new Results(v_709)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a carried charge survives all stage handoffs and the final chamber without a schema change"}, new Results(new V((RuleFunction)(args_712 => {

var values_713 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)2.0d))));
V v_714 = values_713[0];

var values_715 = R.Join(new V[] {}, new Results((V)false));
var obj_716 = R.Get(v_714, (V)"meta");
V key_717 = (V)"eligible";
R.Set(obj_716, key_717, values_715[0]);

var values_718 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"cleanse_flask"))));
var obj_719 = R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_714))).First;
V key_720 = (V)"utility";
R.Set(obj_719, key_720, values_718[0]);
{ double end_723 = R.Num((V)3.0d), step_724 = R.Num((V)1.0d);
for (double i_722 = R.Num((V)1.0d); step_724 > 0 ? i_722 <= end_723 : i_722 >= end_723; i_722 += step_724) {
V v_721 = i_722;
var values_725 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_714))));
V v_726 = values_725[0];

var values_727 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_714}, new Results(R.Get(R.Get(v_714, (V)"stage"), (V)"elite_id")))));
V v_728 = values_727[0];
foreach (var row_729 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_714, (V)"entities"))))))) {
V v_730 = row_729[0];
V v_731 = row_729[1];
if (R.Truth((V)(R.Get(v_731, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_714}, new Results(R.Get(v_731, (V)"id"))));
}
}
var values_732 = Results.Empty;
V v_733 = values_732[0];
foreach (var row_734 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_735 = row_734[0];
V v_736 = row_734[1];
var values_737 = R.Join(new V[] {(V)(R.Get(R.Get(v_728, (V)"position"), (V)"x") + R.Get(v_736, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_728, (V)"position"), (V)"y") + R.Get(v_736, (V)2.0d))));
V v_738 = values_737[0];
V v_739 = values_737[1];
if (R.Truth(R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {R.Get(v_714, (V)"generated_map"), v_738}, new Results(v_739))).First)) {
var values_740 = R.Join(new V[] {v_738}, new Results(v_739));
var obj_741 = R.Get(v_726, (V)"position");
V key_742 = (V)"x";
var obj_743 = R.Get(v_726, (V)"position");
V key_744 = (V)"y";
R.Set(obj_741, key_742, values_740[0]);
R.Set(obj_743, key_744, values_740[1]);

var values_745 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-R.Get(v_736, (V)1.0d))), new Entry((V)2, (-R.Get(v_736, (V)2.0d))))));
v_733 = values_745[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_733)));

var values_746 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_728, (V)"hp") + R.Get(v_728, (V)"defense")) + (V)2.0d)));
var obj_747 = v_726;
V key_748 = (V)"damage";
R.Set(obj_747, key_748, values_746[0]);

R.Call(R.Get(v_13, (V)"recompute"), R.Join(new V[] {}, new Results(v_714)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_714}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_733, (V)1.0d)), new Entry((V)"dy", R.Get(v_733, (V)2.0d))))))));

var values_749 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_714}, new Results(R.Get(R.Get(v_714, (V)"stage"), (V)"exit_id")))));
V v_750 = values_749[0];
foreach (var row_751 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_752 = row_751[0];
V v_753 = row_751[1];
var values_754 = R.Join(new V[] {(V)(R.Get(R.Get(v_750, (V)"position"), (V)"x") + R.Get(v_753, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_750, (V)"position"), (V)"y") + R.Get(v_753, (V)2.0d))));
V v_755 = values_754[0];
V v_756 = values_754[1];
if (R.Truth(R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {R.Get(v_714, (V)"generated_map"), v_755}, new Results(v_756))).First)) {
var values_757 = R.Join(new V[] {v_755}, new Results(v_756));
var obj_758 = R.Get(v_726, (V)"position");
V key_759 = (V)"x";
var obj_760 = R.Get(v_726, (V)"position");
V key_761 = (V)"y";
R.Set(obj_758, key_759, values_757[0]);
R.Set(obj_760, key_761, values_757[1]);

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_714}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_750, (V)"id")), new Entry((V)"route_index", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_714))).First, (V)"utility"), (V)"kind") == (V)"cleanse_flask"), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_714))))).First))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_714, (V)"finale"), () => (V)(R.Get(v_714, (V)"schema") == (V)"roag-run/11")), () => (V)((V)R.Length(R.Get(R.Get(v_714, (V)"idea_rooms"), (V)"nodes")) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all fourteen features occur naturally with bounded service counts and deterministic exact reopening"}, new Results(new V((RuleFunction)(args_762 => {

var values_763 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_764 = values_763[0];

var values_765 = R.Join(new V[] {}, new Results(R.Table()));
V v_766 = values_765[0];

var values_767 = R.Join(new V[] {}, new Results((V)0.0d));
V v_768 = values_767[0];
foreach (var row_769 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_764, (V)"order"))))))) {
V v_770 = row_769[0];
V v_771 = row_769[1];
{ double end_774 = R.Num((V)5.0d), step_775 = R.Num((V)1.0d);
for (double i_773 = R.Num((V)1.0d); step_775 > 0 ? i_773 <= end_774 : i_773 >= end_774; i_773 += step_775) {
V v_772 = i_773;
var values_776 = R.Join(new V[] {}, new Results((V)(v_771 == R.Get(R.Get(v_764, (V)"order"), (V)1.0d))));
V v_777 = values_776[0];
var values_778 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_772}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"stage_options", R.Table(new Entry((V)"biome_id", v_771), new Entry((V)"index", R.Or(R.And(v_777, () => (V)1.0d), () => (V)2.0d)), new Entry((V)"route_history", R.Or(R.And(v_777, () => R.Table()), () => R.Table(new Entry(1, new Results(R.Invoke(v_771, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First))))))))))));
V v_779 = values_778[0];
var values_780 = R.Join(new V[] {}, new Results(R.Table()));
V v_781 = values_780[0];
foreach (var row_782 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_779, (V)"idea_rooms"), (V)"nodes"))))))) {
V v_783 = row_782[0];
V v_784 = row_782[1];
var values_785 = R.Join(new V[] {}, new Results((V)true));
var obj_786 = v_766;
V key_787 = R.Get(v_784, (V)"kind");
R.Set(obj_786, key_787, values_785[0]);

var values_788 = R.Join(new V[] {}, new Results((V)(v_768 + (V)1.0d)));
v_768 = values_788[0];

var values_789 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_781, R.Get(v_784, (V)"floor_id")), () => R.Table(new Entry((V)"services", (V)0.0d), new Entry((V)"assemblies", (V)0.0d), new Entry((V)"signs", (V)0.0d)))));
var obj_790 = v_781;
V key_791 = R.Get(v_784, (V)"floor_id");
R.Set(obj_790, key_791, values_789[0]);
var values_792 = R.Join(new V[] {}, new Results(R.Get(v_781, R.Get(v_784, (V)"floor_id"))));
V v_793 = values_792[0];
if (R.Truth(R.Or((V)(R.Get(v_784, (V)"kind") == (V)"falling_fixture"), () => (V)(R.Get(v_784, (V)"kind") == (V)"bell_clue")))) {
var values_794 = R.Join(new V[] {}, new Results((V)(R.Get(v_793, (V)"assemblies") + (V)1.0d)));
var obj_795 = v_793;
V key_796 = (V)"assemblies";
R.Set(obj_795, key_796, values_794[0]);
} else if (R.Truth((V)(R.Get(v_784, (V)"kind") == (V)"route_sign"))) {
var values_797 = R.Join(new V[] {}, new Results((V)(R.Get(v_793, (V)"signs") + (V)1.0d)));
var obj_798 = v_793;
V key_799 = (V)"signs";
R.Set(obj_798, key_799, values_797[0]);
} else if (R.Truth((V)(R.Get(v_784, (V)"kind") != (V)"bell"))) {
var values_800 = R.Join(new V[] {}, new Results((V)(R.Get(v_793, (V)"services") + (V)1.0d)));
var obj_801 = v_793;
V key_802 = (V)"services";
R.Set(obj_801, key_802, values_800[0]);
}
}
foreach (var row_803 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_781)))))) {
V v_804 = row_803[0];
V v_805 = row_803[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_805, (V)"services") <= (V)2.0d), () => (V)(R.Get(v_805, (V)"assemblies") <= (V)1.0d)), () => (V)(R.Get(v_805, (V)"signs") <= (V)1.0d)))));
}
var values_806 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_779))));
V v_807 = values_806[0];

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, new Results(v_807))))).First}, new Results(v_807)));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_772}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"stage_options", R.Table(new Entry((V)"biome_id", v_771), new Entry((V)"index", R.Or(R.And(v_777, () => (V)1.0d), () => (V)2.0d)), new Entry((V)"route_history", R.Or(R.And(v_777, () => R.Table()), () => R.Table(new Entry(1, new Results(R.Invoke(v_771, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First))))))))))))).First}, new Results(v_807)));
}
}
}
foreach (var row_808 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_17, (V)"order"))))))) {
V v_809 = row_808[0];
V v_810 = row_808[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_766, v_810)}, new Results(R.Concat((V)"not placed: ", v_810))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_768 > (V)200.0d))));
return Results.Empty;
})))));
V v_811 = V.Nil;
v_811 = new V((RuleFunction)(args_812 => {
V v_813 = args_812[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(rt.Global("_G"), (V)"love"))))));

var values_814 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"love", R.Table()))}, new Results(R.Table(new Entry((V)"__index", rt.Global("_G")))))));
V v_815 = values_814[0];
var values_816 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("loadfile"), R.Join(new V[] {}, new Results((V)"main.lua"))))));
V v_817 = values_816[0];

R.Call(rt.Global("setfenv"), R.Join(new V[] {v_817}, new Results(v_815)));

R.Call(v_817, Results.Empty);
V v_818 = V.Nil;
v_818 = new V((RuleFunction)(args_819 => {
V v_820 = args_819[0];
V v_821 = args_819[1];
{ double end_824 = R.Num((V)60.0d), step_825 = R.Num((V)1.0d);
for (double i_823 = R.Num((V)1.0d); step_825 > 0 ? i_823 <= end_824 : i_823 >= end_824; i_823 += step_825) {
V v_822 = i_823;
var values_826 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("debug"), (V)"getupvalue"), R.Join(new V[] {R.Get(R.Get(v_815, (V)"love"), (V)"keypressed")}, new Results(v_822))));
V v_827 = values_826[0];
if (R.Truth((V)(v_827 == v_820))) {
R.Call(R.Get(rt.Global("debug"), (V)"setupvalue"), R.Join(new V[] {R.Get(R.Get(v_815, (V)"love"), (V)"keypressed"), v_822}, new Results(v_821)));

return Results.Empty;
}
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results(R.Concat((V)"callback upvalue missing: ", v_820))));
return Results.Empty;
}));
R.Call(v_818, R.Join(new V[] {(V)"state"}, new Results(v_813)));

R.Call(v_818, R.Join(new V[] {(V)"title"}, new Results((V)false)));

R.Call(v_818, R.Join(new V[] {(V)"spectator_enabled"}, new Results((V)false)));
return R.Join(new V[] {R.Get(v_815, (V)"love")}, new Results(v_818));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"real keyboard and mouse service callbacks inspect freely and reject confirmation during playback"}, new Results(new V((RuleFunction)(args_828 => {

var values_829 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"ration_cache"))));
V v_830 = values_829[0];
V v_831 = values_829[1];
V v_832 = values_829[2];

var values_833 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_834 = v_831;
V key_835 = (V)"hp";
R.Set(obj_834, key_835, values_833[0]);
var values_836 = R.Join(new V[] {}, R.Call(v_811, R.Join(new V[] {}, new Results(v_830))));
V v_837 = values_836[0];
V v_838 = values_836[1];

var values_839 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_840 = values_839[0];

var values_841 = R.Join(new V[] {}, R.Call(R.Get(v_840, (V)"new"), Results.Empty));
V v_842 = values_841[0];

R.Call(v_838, R.Join(new V[] {(V)"playback"}, new Results(v_842)));
var values_843 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_830))));
V v_844 = values_843[0];

R.Call(R.Get(v_837, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"e")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_837, (V)"roagUIState"), Results.Empty).First, (V)"page") == (V)"interaction"))));
R.Call(R.Get(v_837, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_837, (V)"roagMenuState"), Results.Empty).First, (V)"confirm"))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_830))).First}, new Results(v_844)));
R.Call(R.Get(v_840, (V)"add"), R.Join(new V[] {v_842, R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"AttackPerformed"), new Entry((V)"source_id", R.Get(v_831, (V)"id")), new Entry((V)"time", R.Get(v_830, (V)"clock")), new Entry((V)"floor_id", R.Get(v_830, (V)"active_floor_id")), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d))))))))}, new Results(R.Get(v_831, (V)"id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_840, (V)"busy"), R.Join(new V[] {}, new Results(v_842)))));

R.Call(R.Get(v_837, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));

R.Call(R.Get(v_837, (V)"mousepressed"), R.Join(new V[] {(V)700.0d, (V)520.0d}, new Results((V)1.0d)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_830))).First}, new Results(v_844)));
R.Call(R.Get(v_840, (V)"update"), R.Join(new V[] {v_842}, new Results((V)10.0d)));

R.Call(R.Get(v_837, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_831, (V)"hp") == (V)14.0d), () => (V)(R.Get(v_830, (V)"clock") == (V)100.0d)), () => (V)(!R.Truth(R.Call(R.Get(v_837, (V)"roagMenuState"), Results.Empty).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(rt.Global("_G"), (V)"love"))), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_830))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real inventory item button and scroll targeting callbacks cancel safely and commit the chosen distance"}, new Results(new V((RuleFunction)(args_845 => {

var values_846 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"escape_scroll"))));
V v_847 = values_846[0];
V v_848 = values_846[1];
V v_849 = values_846[2];

R.Call(v_35, R.Join(new V[] {v_847, v_849}, new Results((V)"take")));

var values_850 = R.Join(new V[] {}, R.Call(v_811, R.Join(new V[] {}, new Results(v_847))));
V v_851 = values_850[0];
var values_852 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_847))));
V v_853 = values_852[0];

R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"i")));

R.Call(R.Get(v_851, (V)"mousepressed"), R.Join(new V[] {(V)300.0d, (V)500.0d}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_851, (V)"roagUIState"), Results.Empty).First, (V)"page") == (V)"interaction"))));

R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));

R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));
R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"1")));

R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"d")));

R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"escape")));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_847))).First}, new Results(v_853)));
R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"u")));

R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));

R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));

R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"2")));

R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"d")));

R.Call(R.Get(v_851, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_848, (V)"position"), (V)"x") == (V)9.0d), () => (V)(R.Get(R.Get(v_848, (V)"position"), (V)"y") == (V)6.0d)), () => (V)(!R.Truth(R.Get(v_848, (V)"utility")))), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_847))))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(rt.Global("_G"), (V)"love"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real service renderer issues bounded menu draw calls with injected graphics and no state or RNG changes"}, new Results(new V((RuleFunction)(args_854 => {

var values_855 = R.Join(new V[] {}, R.Call(v_20, R.Join(new V[] {}, new Results((V)"attunement_forge"))));
V v_856 = values_855[0];
V v_857 = values_855[1];
V v_858 = values_855[2];

var values_859 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_860 = v_856;
V key_861 = (V)"currency";
R.Set(obj_860, key_861, values_859[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_856, R.Get(v_857, (V)"id"), (V)"core:boon/crescent_reach", (V)"common"}, new Results((V)1.0d)));
var values_862 = R.Join(new V[] {}, R.Call(v_811, R.Join(new V[] {}, new Results(v_856))));
V v_863 = values_862[0];
V v_864 = values_862[1];

R.Call(R.Get(v_863, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"e")));

R.Call(R.Get(v_863, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));
var values_865 = R.Join(new V[] {}, new Results(R.Table()));
V v_866 = values_865[0];
V v_867 = V.Nil;
v_867 = new V((RuleFunction)(args_868 => {
V v_869 = args_868[0];
V v_870 = args_868[1];
V v_871 = args_868[2];
if (R.Truth(R.Or(R.Get(v_866, v_869), () => (V)(v_871 > (V)4.0d)))) {
return Results.Empty;
}

var values_872 = R.Join(new V[] {}, new Results((V)true));
var obj_873 = v_866;
V key_874 = v_869;
R.Set(obj_873, key_874, values_872[0]);
var values_875 = R.Join(new V[] {}, new Results(R.Table()));
V v_876 = values_875[0];
{ double end_879 = R.Num((V)60.0d), step_880 = R.Num((V)1.0d);
for (double i_878 = R.Num((V)1.0d); step_880 > 0 ? i_878 <= end_879 : i_878 >= end_879; i_878 += step_880) {
V v_877 = i_878;
var values_881 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("debug"), (V)"getupvalue"), R.Join(new V[] {v_869}, new Results(v_877))));
V v_882 = values_881[0];
V v_883 = values_881[1];

if (R.Truth((V)(!R.Truth(v_882)))) {
break;
}
if (R.Truth((V)(v_882 == v_870))) {
return R.Join(new V[] {}, new Results(v_883));
}
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_883))).First == (V)"function"))) {
var values_884 = R.Join(new V[] {}, new Results(v_883));
var obj_885 = v_876;
V key_886 = (V)((V)R.Length(v_876) + (V)1.0d);
R.Set(obj_885, key_886, values_884[0]);
}
}
}
foreach (var row_887 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_876)))))) {
V v_888 = row_887[0];
V v_889 = row_887[1];
var values_890 = R.Join(new V[] {}, R.Call(v_867, R.Join(new V[] {v_889, v_870}, new Results((V)(v_871 + (V)1.0d)))));
V v_891 = values_890[0];

if (R.Truth(v_891)) {
return R.Join(new V[] {}, new Results(v_891));
}
}
return Results.Empty;
}));
var values_892 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_867, R.Join(new V[] {R.Get(v_863, (V)"draw"), (V)"draw_interaction_menu"}, new Results((V)0.0d))))));
V v_893 = values_892[0];

var values_894 = R.Join(new V[] {}, new Results(R.Table()));
V v_895 = values_894[0];
R.Set(v_895, "getWidth", new V((RuleFunction)(args_896 => {
V v_897 = args_896[0];
V v_898 = args_896[1];
return R.Join(new V[] {}, new Results((V)((V)R.Length(v_898) * (V)7.0d)));
return Results.Empty;
})));

R.Set(v_895, "getHeight", new V((RuleFunction)(args_899 => {
V v_900 = args_899[0];
return R.Join(new V[] {}, new Results((V)14.0d));
return Results.Empty;
})));
var values_901 = R.Join(new V[] {}, new Results(R.Table()));
V v_902 = values_901[0];

var values_903 = R.Join(new V[] {}, new Results(R.Table()));
V v_904 = values_903[0];

var values_905 = R.Join(new V[] {}, new Results(v_904));
var obj_906 = v_863;
V key_907 = (V)"graphics";
R.Set(obj_906, key_907, values_905[0]);
R.Set(v_904, (V)"getFont", new V((RuleFunction)(args_908 => {

return R.Join(new V[] {}, new Results(v_895));
return Results.Empty;
})));

R.Set(v_904, (V)"setFont", new V((RuleFunction)(args_909 => {


return Results.Empty;
})));
R.Set(v_904, (V)"getDimensions", new V((RuleFunction)(args_910 => {

return R.Join(new V[] {(V)1024.0d}, new Results((V)640.0d));
return Results.Empty;
})));
R.Set(v_904, (V)"getWidth", new V((RuleFunction)(args_911 => {

return R.Join(new V[] {}, new Results((V)1024.0d));
return Results.Empty;
})));
R.Set(v_904, (V)"setColor", new V((RuleFunction)(args_912 => {


return Results.Empty;
})));

R.Set(v_904, (V)"setLineWidth", new V((RuleFunction)(args_913 => {


return Results.Empty;
})));
R.Set(v_904, (V)"rectangle", new V((RuleFunction)(args_914 => {
V v_915 = args_914[0];
V v_916 = args_914[1];
V v_917 = args_914[2];
V v_918 = args_914[3];
V v_919 = args_914[4];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_916 >= (V)0.0d), () => (V)(v_917 >= (V)0.0d)), () => (V)((V)(v_916 + v_918) <= (V)1024.0d)), () => (V)((V)(v_917 + v_919) <= (V)640.0d)))));

var values_920 = R.Join(new V[] {}, new Results(v_915));
var obj_921 = v_902;
V key_922 = (V)((V)R.Length(v_902) + (V)1.0d);
R.Set(obj_921, key_922, values_920[0]);
return Results.Empty;
})));
R.Set(v_904, (V)"printf", new V((RuleFunction)(args_923 => {
V v_924 = args_923[0];
V v_925 = args_923[1];
V v_926 = args_923[2];
V v_927 = args_923[3];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_924))).First == (V)"string"), () => (V)(v_925 >= (V)0.0d)), () => (V)((V)(v_925 + v_927) <= (V)1024.0d)), () => (V)(v_926 >= (V)0.0d)), () => (V)(v_926 < (V)640.0d)))));

var values_928 = R.Join(new V[] {}, new Results(v_924));
var obj_929 = v_902;
V key_930 = (V)((V)R.Length(v_902) + (V)1.0d);
R.Set(obj_929, key_930, values_928[0]);
return Results.Empty;
})));
R.Set(v_904, (V)"print", new V((RuleFunction)(args_931 => {
V v_932 = args_931[0];
V v_933 = args_931[1];
V v_934 = args_931[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_932))).First == (V)"string"), () => (V)(v_933 >= (V)0.0d)), () => (V)(v_933 < (V)1024.0d)), () => (V)(v_934 >= (V)0.0d)), () => (V)(v_934 < (V)640.0d)))));

var values_935 = R.Join(new V[] {}, new Results(v_932));
var obj_936 = v_902;
V key_937 = (V)((V)R.Length(v_902) + (V)1.0d);
R.Set(obj_936, key_937, values_935[0]);
return Results.Empty;
})));
{ double end_940 = R.Num((V)60.0d), step_941 = R.Num((V)1.0d);
for (double i_939 = R.Num((V)1.0d); step_941 > 0 ? i_939 <= end_940 : i_939 >= end_940; i_939 += step_941) {
V v_938 = i_939;
var values_942 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("debug"), (V)"getupvalue"), R.Join(new V[] {v_893}, new Results(v_938))));
V v_943 = values_942[0];

if (R.Truth((V)(!R.Truth(v_943)))) {
break;
}
if (R.Truth(R.Or(R.Or((V)(v_943 == (V)"ui_display_font"), () => (V)(v_943 == (V)"ui_body_font")), () => (V)(v_943 == (V)"ui_menu_font")))) {
R.Call(R.Get(rt.Global("debug"), (V)"setupvalue"), R.Join(new V[] {v_893, v_938}, new Results(v_895)));
}
}
}
var values_944 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_856))));
V v_945 = values_944[0];

var values_946 = R.Join(new V[] {}, new Results(v_863));
var obj_947 = rt.Global("_G");
V key_948 = (V)"love";
R.Set(obj_947, key_948, values_946[0]);

var values_949 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(v_893))));
V v_950 = values_949[0];
V v_951 = values_949[1];

var values_952 = R.Join(new V[] {}, new Results(V.Nil));
var obj_953 = rt.Global("_G");
V key_954 = (V)"love";
R.Set(obj_953, key_954, values_952[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {v_950}, new Results(v_951)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_902) > (V)15.0d))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_856))).First}, new Results(v_945)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"service menu controller pages, cancels confirmations and never mutates a game snapshot"}, new Results(new V((RuleFunction)(args_955 => {

var values_956 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.interaction_menu"))));
V v_957 = values_956[0];

var values_958 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_959 = values_958[0];

var values_960 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_959))));
V v_961 = values_960[0];

var values_962 = R.Join(new V[] {}, new Results(R.Table()));
V v_963 = values_962[0];
{ double end_966 = R.Num((V)15.0d), step_967 = R.Num((V)1.0d);
for (double i_965 = R.Num((V)1.0d); step_967 > 0 ? i_965 <= end_966 : i_965 >= end_966; i_965 += step_967) {
V v_964 = i_965;
var values_968 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_964))).First), new Entry((V)"label", R.Concat((V)"Choice ", v_964)), new Entry((V)"enabled", (V)(v_964 != (V)2.0d)))));
var obj_969 = v_963;
V key_970 = v_964;
R.Set(obj_969, key_970, values_968[0]);
}
}
var values_971 = R.Join(new V[] {}, R.Call(R.Get(v_957, (V)"new"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"entries", v_963))))));
V v_972 = values_971[0];

R.Call(R.Get(v_957, (V)"key"), R.Join(new V[] {v_972}, new Results((V)"right")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_972, (V)"selected") == (V)7.0d), () => (V)((V)R.Length(R.Call(R.Get(v_957, (V)"rows"), R.Join(new V[] {}, new Results(v_972))).First) == (V)6.0d)))));
R.Call(R.Get(v_957, (V)"key"), R.Join(new V[] {v_972}, new Results((V)"return")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_972, (V)"confirm"))));

R.Call(R.Get(v_957, (V)"key"), R.Join(new V[] {v_972}, new Results((V)"escape")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_972, (V)"confirm"))))));
R.Call(R.Get(v_957, (V)"key"), R.Join(new V[] {v_972}, new Results((V)"down")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_972, (V)"selected") == (V)8.0d))));

R.Call(R.Get(v_957, (V)"key"), R.Join(new V[] {v_972}, new Results((V)"return")));

var values_973 = R.Join(new V[] {}, R.Call(R.Get(v_957, (V)"key"), R.Join(new V[] {v_972}, new Results((V)"return"))));
V v_974 = values_973[0];
V v_975 = values_973[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_974 == (V)"choose"), () => (V)(R.Get(v_975, (V)"id") == (V)"8")))));
var values_976 = R.Join(new V[] {}, R.Call(R.Get(v_957, (V)"new"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"entries", v_963))))));
v_972 = values_976[0];

R.Call(R.Get(v_957, (V)"key"), R.Join(new V[] {v_972}, new Results((V)"2")));

var values_977 = R.Join(new V[] {}, R.Call(R.Get(v_957, (V)"key"), R.Join(new V[] {v_972}, new Results((V)"return"))));
v_974 = values_977[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_974 == (V)"status"), () => (V)(!R.Truth(R.Get(v_972, (V)"confirm")))))));
R.Call(R.Get(v_957, (V)"mouse"), R.Join(new V[] {v_972, (V)130.0d, (V)166.0d}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_972, (V)"selected") == (V)1.0d))));

R.Call(R.Get(v_957, (V)"mouse"), R.Join(new V[] {v_972, (V)700.0d, (V)520.0d}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_972, (V)"confirm"))));
var values_978 = R.Join(new V[] {}, R.Call(R.Get(v_957, (V)"mouse"), R.Join(new V[] {v_972, (V)700.0d, (V)520.0d}, new Results((V)1.0d))));
v_974 = values_978[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_974 == (V)"choose"))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_959))).First}, new Results(v_961)));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
