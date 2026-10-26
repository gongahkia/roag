// Static C# port of tests/idea_boon_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: e8a09c25007c6d984f898e8cdea96dd4e7436e64d6938d7db01e283804d237c2
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_boon_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_21 = values_20[0];
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
V v_25 = args_23[1];
V v_26 = args_23[2];
V v_27 = args_23[3];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_24, R.Get(v_24, (V)"player_id"), R.Concat((V)"core:boon/", v_25), R.Or(v_26, () => (V)"rare")}, new Results(R.Or(v_27, () => (V)1.0d))));
return Results.Empty;
}));
V v_28 = V.Nil;
v_28 = new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
var values_32 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_33 = values_32[0];

var values_34 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_33))));
V v_35 = values_34[0];
if (R.Truth(v_30)) {
R.Call(v_22, R.Join(new V[] {v_33}, new Results(v_30)));
}
if (R.Truth(v_31)) {
var values_36 = R.Join(new V[] {}, new Results(v_31));
var obj_37 = v_35;
V key_38 = (V)"hp";
R.Set(obj_37, key_38, values_36[0]);
}
return R.Join(new V[] {v_33}, new Results(v_35));
return Results.Empty;
}));
V v_39 = V.Nil;
v_39 = new V((RuleFunction)(args_40 => {
V v_41 = args_40[0];
V v_42 = args_40[1];
V v_43 = args_40[2];
V v_44 = args_40[3];
var values_45 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_41}, new Results(R.Table(new Entry((V)"kind", v_42), new Entry((V)"dx", v_43), new Entry((V)"dy", v_44))))));
V v_46 = values_45[0];
V v_47 = values_45[1];
V v_48 = values_45[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_46}, new Results(v_47)));

return R.Join(new V[] {}, new Results(v_48));
return Results.Empty;
}));
V v_49 = V.Nil;
v_49 = new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
V v_52 = args_50[1];
V v_53 = args_50[2];
var values_54 = R.Join(new V[] {}, new Results((V)0.0d));
V v_55 = values_54[0];
foreach (var row_56 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_51)))))) {
V v_57 = row_56[0];
V v_58 = row_56[1];
if (R.Truth(R.And((V)(R.Get(v_58, (V)"kind") == v_52), () => R.Or((V)(!R.Truth(v_53)), () => (V)(R.Get(v_58, (V)"boon_id") == R.Concat((V)"core:boon/", v_53)))))) {
var values_59 = R.Join(new V[] {}, new Results((V)(v_55 + (V)1.0d)));
v_55 = values_59[0];
}
}
return R.Join(new V[] {}, new Results(v_55));
return Results.Empty;
}));
V v_60 = V.Nil;
v_60 = new V((RuleFunction)(args_61 => {
V v_62 = args_61[0];
V v_63 = args_61[1];
V v_64 = args_61[2];
V v_65 = args_61[3];
return R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"spawn_supply"), R.Join(new V[] {v_62, v_63, R.Get(v_62, (V)"active_floor_id"), R.Or(v_64, () => (V)3.0d), R.Or(v_65, () => (V)2.0d), new V((RuleFunction)(args_66 => {


return Results.Empty;
}))}, new Results(R.Get(v_62, (V)"player_id")))));
return Results.Empty;
}));
V v_67 = V.Nil;
v_67 = new V((RuleFunction)(args_68 => {
V v_69 = args_68[0];
return R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"spawn_currency"), R.Join(new V[] {v_69, (V)2.0d, R.Get(v_69, (V)"active_floor_id"), (V)3.0d, (V)2.0d, new V((RuleFunction)(args_70 => {


return Results.Empty;
}))}, new Results(R.Get(v_69, (V)"player_id")))));
return Results.Empty;
}));
V v_71 = V.Nil;
v_71 = new V((RuleFunction)(args_72 => {
V v_73 = args_72[0];
V v_74 = args_72[1];
return R.Join(new V[] {}, new Results(R.And(R.Get(v_73, (V)"boon_memory"), () => R.Get(R.Get(v_73, (V)"boon_memory"), R.Concat((V)"core:boon/", v_74)))));
return Results.Empty;
}));
V v_75 = V.Nil;
v_75 = new V((RuleFunction)(args_76 => {
V v_77 = args_76[0];
var values_78 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results(v_77))));
V v_79 = values_78[0];
V v_80 = values_78[1];
var values_81 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_82 = R.Get(v_80, (V)"position");
V key_83 = (V)"x";
var obj_84 = R.Get(v_80, (V)"position");
V key_85 = (V)"y";
R.Set(obj_82, key_83, values_81[0]);
R.Set(obj_84, key_85, values_81[1]);

R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_79)));
var values_86 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_79, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)0.0d))));
V v_87 = values_86[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_79, R.Get(v_87, (V)"id"), (V)"core:boon/emberbrand", (V)"common"}, new Results((V)1.0d)));
R.Call(v_39, R.Join(new V[] {v_79}, new Results((V)"wait")));

R.Call(v_39, R.Join(new V[] {v_79}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_80, (V)"statuses"), (V)"burning"))));
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_79}, new Results(R.Get(v_87, (V)"id"))));
foreach (var row_88 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_79, (V)"entities"))))))) {
V v_89 = row_88[0];
V v_90 = row_88[1];
if (R.Truth((V)(R.Get(v_90, (V)"id") == R.Get(v_87, (V)"id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_79, (V)"entities")}, new Results(v_89)));

break;
}
}
return R.Join(new V[] {v_79}, new Results(v_80));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"idea boons append distinct IDs and native artwork while retaining the old frozen pool"}, new Results(new V((RuleFunction)(args_91 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_11, (V)"order")) >= (V)7.0d), () => R.Call(R.Get(v_9, (V)"validate"), Results.Empty).First), () => (V)((V)R.Length(R.Get(v_9, (V)"order")) == (V)((V)53.0d + (V)R.Length(R.Get(v_11, (V)"order"))))))));
var values_92 = R.Join(new V[] {}, new Results(R.Table()));
V v_93 = values_92[0];
var values_94 = R.Join(new V[] {}, new Results(R.Table()));
V v_95 = values_94[0];
foreach (var row_96 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"normal_order"), Results.Empty)))))) {
V v_97 = row_96[0];
V v_98 = row_96[1];
if (R.Truth((V)(v_97 <= (V)49.0d))) {
var values_99 = R.Join(new V[] {}, new Results(v_98));
var obj_100 = v_95;
V key_101 = (V)((V)R.Length(v_95) + (V)1.0d);
R.Set(obj_100, key_101, values_99[0]);
}
}
var values_102 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"available_boons", v_95))))));
V v_103 = values_102[0];
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_103))));
V v_105 = values_104[0];
R.Call(v_3, R.Join(new V[] {v_105}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, new Results(v_105)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_103, (V)"available_boons")) == (V)49.0d), () => (V)((V)R.Length(R.Call(R.Get(v_9, (V)"normal_order"), Results.Empty).First) == (V)((V)49.0d + (V)R.Length(R.Get(v_11, (V)"order"))))))));
foreach (var row_106 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_11, (V)"order"))))))) {
V v_107 = row_106[0];
V v_108 = row_106[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_93, v_108))))));

var values_109 = R.Join(new V[] {}, new Results((V)true));
var obj_110 = v_93;
V key_111 = v_108;
R.Set(obj_110, key_111, values_109[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results(v_108))).First, (V)"id") == v_108))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))).First, (V)"boons"), v_108), (V)1.0d) == R.Get(R.Get(R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results(v_108))).First, (V)"art"), (V)"index")))));
foreach (var row_112 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_95)))))) {
V v_113 = row_112[0];
V v_114 = row_112[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_114 != v_108))));
}
foreach (var row_115 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_9, (V)"rarities"))))))) {
V v_116 = row_115[0];
V v_117 = row_115[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_9, (V)"offer_text"), R.Join(new V[] {v_108}, new Results(v_117))).First, "find", R.Join(new V[] {R.Get(R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results(v_108))).First, (V)"description"), (V)1.0d}, new Results((V)true)))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/beggars_cap"))).First, (V)"art"), (V)"index") == (V)33.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/overflow_chalice"))).First, (V)"art"), (V)"index") == (V)824.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all new boons occur in normal offers for an eligible class without entering the starter-only fixture pool"}, new Results(new V((RuleFunction)(args_118 => {

var values_119 = R.Join(new V[] {}, new Results(R.Table()));
V v_120 = values_119[0];
var values_121 = R.Join(new V[] {R.Table(new Entry((V)1, (V)"core:actor/adventurer"))}, new Results(R.Table(new Entry((V)"core:actor/adventurer", (V)true))));
V v_122 = values_121[0];
V v_123 = values_121[1];
foreach (var row_124 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_11, (V)"order"))))))) {
V v_125 = row_124[0];
V v_126 = row_124[1];
var values_127 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results(v_126))).First, (V)"eligible_classes")));
V v_128 = values_127[0];
if (R.Truth(R.And(v_128, () => (V)(!R.Truth(R.Get(v_123, R.Get(v_128, (V)1.0d))))))) {
var values_129 = R.Join(new V[] {}, new Results(R.Get(v_128, (V)1.0d)));
var obj_130 = v_122;
V key_131 = (V)((V)R.Length(v_122) + (V)1.0d);
R.Set(obj_130, key_131, values_129[0]);

var values_132 = R.Join(new V[] {}, new Results((V)true));
var obj_133 = v_123;
V key_134 = R.Get(v_128, (V)1.0d);
R.Set(obj_133, key_134, values_132[0]);
}
}
foreach (var row_135 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_122)))))) {
V v_136 = row_135[0];
V v_137 = row_135[1];
{ double end_140 = R.Num((V)320.0d), step_141 = R.Num((V)1.0d);
for (double i_139 = R.Num((V)1.0d); step_141 > 0 ? i_139 <= end_140 : i_139 >= end_140; i_139 += step_141) {
V v_138 = i_139;
var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {v_138, V.Nil}, new Results(R.Table(new Entry((V)"available_boons", R.Call(R.Get(v_9, (V)"normal_order"), Results.Empty).First), new Entry((V)"class_id", v_137))))));
V v_143 = values_142[0];
foreach (var row_144 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"make_offers"), R.Join(new V[] {v_143}, R.Call(R.Get(v_19, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/standard")))))))))) {
V v_145 = row_144[0];
V v_146 = row_144[1];
var values_147 = R.Join(new V[] {}, new Results((V)true));
var obj_148 = v_120;
V key_149 = R.Get(v_146, (V)"boon_id");
R.Set(obj_148, key_149, values_147[0]);
}
}
}
}
foreach (var row_150 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_11, (V)"order"))))))) {
V v_151 = row_150[0];
V v_152 = row_150[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_120, v_152)}, new Results(R.Concat((V)"never offered ", v_152))));
}
var values_153 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_154 = values_153[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_154, (V)"available_boons")) == (V)20.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Beggar cap grants a timed one-hit ward only for a low-health real gold pickup"}, new Results(new V((RuleFunction)(args_155 => {

foreach (var row_156 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)6.0d), new Entry((V)2, (V)12.0d), new Entry((V)3, (V)13.0d)))))))) {
V v_157 = row_156[0];
V v_158 = row_156[1];
var values_159 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"beggars_cap"}, new Results(v_158))));
V v_160 = values_159[0];
V v_161 = values_159[1];

R.Call(v_67, R.Join(new V[] {}, new Results(v_160)));

var values_162 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_160, (V)"rng"), (V)"state")));
V v_163 = values_162[0];
var values_164 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {v_160, (V)"move", (V)1.0d}, new Results((V)0.0d))));
V v_165 = values_164[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_160, (V)"rng"), (V)"state") == v_163), () => (V)(R.Get(v_161, (V)"hp") == v_158)), () => (V)(R.Get(v_160, (V)"currency") == (V)2.0d)))));
if (R.Truth((V)(v_158 <= (V)12.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_161}, new Results((V)"beggars_cap"))).First, (V)"ward") == (V)4.0d), () => (V)(R.Get(R.Call(v_71, R.Join(new V[] {v_161}, new Results((V)"beggars_cap"))).First, (V)"expires_at") == (V)300.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_49, R.Join(new V[] {v_165, (V)"BoonActivated"}, new Results((V)"beggars_cap"))).First == (V)1.0d))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(v_71, R.Join(new V[] {v_161}, new Results((V)"beggars_cap"))).First)))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stored wards consume in catalogue order and leave an unused later ward intact"}, new Results(new V((RuleFunction)(args_166 => {

var values_167 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"beggars_cap"}, new Results((V)6.0d))));
V v_168 = values_167[0];
V v_169 = values_167[1];

R.Call(v_22, R.Join(new V[] {v_168}, new Results((V)"overflow_chalice")));

R.Call(v_22, R.Join(new V[] {v_168}, new Results((V)"betrayal_ward")));
R.Call(v_67, R.Join(new V[] {}, new Results(v_168)));

R.Call(v_60, R.Join(new V[] {v_168}, new Results((V)"food")));

R.Call(v_39, R.Join(new V[] {v_168, (V)"move", (V)1.0d}, new Results((V)0.0d)));
var values_170 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"ward", (V)2.0d), new Entry((V)"expires_at", (V)400.0d))));
var obj_171 = R.Get(v_169, (V)"boon_memory");
V key_172 = (V)"core:boon/betrayal_ward";
R.Set(obj_171, key_172, values_170[0]);
var values_173 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"ward", (V)3.0d), new Entry((V)"expires_at", (V)400.0d))));
var obj_174 = R.Get(v_169, (V)"boon_memory");
V key_175 = (V)"core:boon/overflow_chalice";
R.Set(obj_174, key_175, values_173[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"consume_ward"), R.Join(new V[] {v_168, v_169}, new Results((V)1.0d))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_169}, new Results((V)"betrayal_ward"))).First, (V)"ward") == (V)0.0d), () => (V)(R.Get(R.Call(v_71, R.Join(new V[] {v_169}, new Results((V)"beggars_cap"))).First, (V)"ward") == (V)4.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"consume_ward"), R.Join(new V[] {v_168, v_169}, new Results((V)6.0d))).First == (V)6.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_169}, new Results((V)"beggars_cap"))).First, (V)"ward") == (V)0.0d), () => (V)(R.Get(R.Call(v_71, R.Join(new V[] {v_169}, new Results((V)"overflow_chalice"))).First, (V)"ward") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"consume_ward"), R.Join(new V[] {v_168, v_169}, new Results((V)10.0d))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a real committed enemy impact uses the new ward and keeps damage bookkeeping exact"}, new Results(new V((RuleFunction)(args_176 => {

var values_177 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"beggars_cap"}, new Results((V)6.0d))));
V v_178 = values_177[0];
V v_179 = values_177[1];

R.Call(v_67, R.Join(new V[] {}, new Results(v_178)));

R.Call(v_39, R.Join(new V[] {v_178, (V)"move", (V)1.0d}, new Results((V)0.0d)));
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_178, (V)"core:enemy/mossbound_guard", (V)4.0d, (V)2.0d}, new Results(R.Get(v_178, (V)"clock"))));
var values_180 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {v_178}, new Results((V)"wait"))));
V v_181 = values_180[0];

var values_182 = R.Join(new V[] {}, new Results(R.Get(v_179, (V)"hp")));
V v_183 = values_182[0];
{ double end_186 = R.Num((V)3.0d), step_187 = R.Num((V)1.0d);
for (double i_185 = R.Num((V)1.0d); step_187 > 0 ? i_185 <= end_186 : i_185 >= end_186; i_185 += step_187) {
V v_184 = i_185;
if (R.Truth((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_179}, new Results((V)"beggars_cap"))).First, (V)"ward") == (V)0.0d))) {
break;
}
var values_188 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {v_178}, new Results((V)"wait"))));
v_181 = values_188[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_179}, new Results((V)"beggars_cap"))).First, (V)"ward") == (V)0.0d), () => (V)(R.Get(v_179, (V)"hp") == v_183)))));
var values_189 = R.Join(new V[] {}, new Results((V)false));
V v_190 = values_189[0];
foreach (var row_191 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_181)))))) {
V v_192 = row_191[0];
V v_193 = row_191[1];
if (R.Truth(R.And((V)(R.Get(v_193, (V)"kind") == (V)"DamageBlocked"), () => (V)(R.Get(v_193, (V)"target_id") == R.Get(v_179, (V)"id"))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_193, (V)"ward_absorbed") > (V)0.0d), () => (V)(R.Get(v_193, (V)"actual_loss") == (V)0.0d)))));

var values_194 = R.Join(new V[] {}, new Results((V)true));
v_190 = values_194[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_190)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Overflow Chalice captures only actual wasted healing and a fully used supply gives no ward"}, new Results(new V((RuleFunction)(args_195 => {

foreach (var row_196 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)10.0d), new Entry((V)2, (V)23.0d), new Entry((V)3, (V)24.0d)))))))) {
V v_197 = row_196[0];
V v_198 = row_196[1];
var values_199 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"overflow_chalice"}, new Results(v_198))));
V v_200 = values_199[0];
V v_201 = values_199[1];

var values_202 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {v_200}, new Results((V)"food"))));
V v_203 = values_202[0];
R.Call(v_39, R.Join(new V[] {v_200, (V)"move", (V)1.0d}, new Results((V)0.0d)));
if (R.Truth((V)(v_198 == (V)23.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_203, (V)"collected"), () => (V)(R.Get(v_201, (V)"hp") == (V)24.0d)), () => (V)(R.Get(R.Call(v_71, R.Join(new V[] {v_201}, new Results((V)"overflow_chalice"))).First, (V)"ward") == (V)3.0d)))));
} else if (R.Truth((V)(v_198 == (V)10.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_203, (V)"collected"), () => (V)(R.Get(v_201, (V)"hp") == (V)14.0d)), () => (V)(!R.Truth(R.Call(v_71, R.Join(new V[] {v_201}, new Results((V)"overflow_chalice"))).First))))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_203, (V)"collected"))), () => (V)(!R.Truth(R.Call(v_71, R.Join(new V[] {v_201}, new Results((V)"overflow_chalice"))).First))))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"captured healing respects both rarity-stack cap and finite supply waste"}, new Results(new V((RuleFunction)(args_204 => {

foreach (var row_205 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)100.0d)))))))) {
V v_206 = row_205[0];
V v_207 = row_205[1];
var values_208 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {V.Nil}, new Results((V)23.0d))));
V v_209 = values_208[0];
V v_210 = values_208[1];

R.Call(v_22, R.Join(new V[] {v_209, (V)"overflow_chalice", (V)"common"}, new Results(v_207)));
R.Call(v_60, R.Join(new V[] {v_209}, new Results((V)"food")));

R.Call(v_39, R.Join(new V[] {v_209, (V)"move", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_210}, new Results((V)"overflow_chalice"))).First, (V)"ward") == R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)((V)2.0d * v_207)}, new Results((V)3.0d))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"consume_ward"), R.Join(new V[] {v_209, v_210}, new Results((V)100.0d))).First == R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)((V)2.0d * v_207)}, new Results((V)3.0d))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"consume_ward"), R.Join(new V[] {v_209, v_210}, new Results((V)100.0d))).First == (V)0.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"expired capture cannot block and preview never spends an active ward"}, new Results(new V((RuleFunction)(args_211 => {

var values_212 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"overflow_chalice"}, new Results((V)23.0d))));
V v_213 = values_212[0];
V v_214 = values_212[1];

R.Call(v_60, R.Join(new V[] {v_213}, new Results((V)"food")));

R.Call(v_39, R.Join(new V[] {v_213, (V)"move", (V)1.0d}, new Results((V)0.0d)));
var values_215 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_213))));
V v_216 = values_215[0];
{ double end_219 = R.Num((V)20.0d), step_220 = R.Num((V)1.0d);
for (double i_218 = R.Num((V)1.0d); step_220 > 0 ? i_218 <= end_219 : i_218 >= end_219; i_218 += step_220) {
V v_217 = i_218;
R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_213, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)));
}
}
R.Call(v_3, R.Join(new V[] {v_216}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_213)))));
{ double end_223 = R.Num((V)4.0d), step_224 = R.Num((V)1.0d);
for (double i_222 = R.Num((V)1.0d); step_224 > 0 ? i_222 <= end_223 : i_222 >= end_223; i_222 += step_224) {
V v_221 = i_222;
R.Call(v_39, R.Join(new V[] {v_213}, new Results((V)"wait")));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_7, (V)"consume_ward"), R.Join(new V[] {v_213, v_214}, new Results((V)4.0d))).First == (V)0.0d), () => (V)(R.Get(R.Call(v_71, R.Join(new V[] {v_214}, new Results((V)"overflow_chalice"))).First, (V)"ward") == (V)3.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Road Flask changes future action costs, combines with Bloodrush, and expires on world time"}, new Results(new V((RuleFunction)(args_225 => {

var values_226 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"road_flask"}, new Results((V)10.0d))));
V v_227 = values_226[0];
V v_228 = values_226[1];

R.Call(v_60, R.Join(new V[] {v_227}, new Results((V)"food")));

var values_229 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_227, (V)"rng"), (V)"state")));
V v_230 = values_229[0];
R.Call(v_39, R.Join(new V[] {v_227, (V)"move", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_227, (V)"clock") == (V)100.0d), () => (V)(R.Call(R.Get(v_7, (V)"speed_bonus"), R.Join(new V[] {v_227}, new Results(v_228))).First == (V)20.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_227}, new Results((V)"wait"))).First, (V)"cost") == R.Call(R.Get(v_5, (V)"effective_cost"), R.Join(new V[] {(V)100.0d}, new Results((V)120.0d))).First))));
R.Call(v_22, R.Join(new V[] {v_227}, new Results((V)"bloodrush")));
var values_231 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"speed", (V)15.0d), new Entry((V)"expires_at", (V)300.0d))));
var obj_232 = R.Get(v_228, (V)"boon_memory");
V key_233 = (V)"core:boon/bloodrush";
R.Set(obj_232, key_233, values_231[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"speed_bonus"), R.Join(new V[] {v_227}, new Results(v_228))).First == (V)35.0d))));
while (R.Truth((V)(R.Get(v_227, (V)"clock") <= (V)300.0d))) {
R.Call(v_39, R.Join(new V[] {v_227}, new Results((V)"wait")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(R.Get(v_7, (V)"speed_bonus"), R.Join(new V[] {v_227}, new Results(v_228))).First == (V)0.0d), () => (V)(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_227}, new Results((V)"wait"))).First, (V)"cost") == (V)100.0d)), () => (V)(R.Get(R.Get(v_227, (V)"rng"), (V)"state") == v_230)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an expired large captured ward cannot be renewed by a smaller new supply waste"}, new Results(new V((RuleFunction)(args_234 => {

var values_235 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"overflow_chalice"}, new Results((V)23.0d))));
V v_236 = values_235[0];
V v_237 = values_235[1];

R.Call(v_60, R.Join(new V[] {v_236}, new Results((V)"health")));

R.Call(v_39, R.Join(new V[] {v_236, (V)"move", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_237}, new Results((V)"overflow_chalice"))).First, (V)"ward") == (V)4.0d))));
{ double end_240 = R.Num((V)4.0d), step_241 = R.Num((V)1.0d);
for (double i_239 = R.Num((V)1.0d); step_241 > 0 ? i_239 <= end_240 : i_239 >= end_240; i_239 += step_241) {
V v_238 = i_239;
R.Call(v_39, R.Join(new V[] {v_236}, new Results((V)"wait")));
}
}
var values_242 = R.Join(new V[] {}, new Results((V)23.0d));
var obj_243 = v_237;
V key_244 = (V)"hp";
R.Set(obj_243, key_244, values_242[0]);

R.Call(v_60, R.Join(new V[] {v_236, (V)"food", (V)2.0d}, new Results((V)2.0d)));

R.Call(v_39, R.Join(new V[] {v_236, (V)"move", (-(V)1.0d)}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_237}, new Results((V)"overflow_chalice"))).First, (V)"ward") == (V)3.0d), () => (V)(R.Get(R.Call(v_71, R.Join(new V[] {v_237}, new Results((V)"overflow_chalice"))).First, (V)"expires_at") == (V)900.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"more food refreshes speed without stacking repeat activations, and health is not food"}, new Results(new V((RuleFunction)(args_245 => {

var values_246 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"road_flask"}, new Results((V)10.0d))));
V v_247 = values_246[0];
V v_248 = values_246[1];

R.Call(v_60, R.Join(new V[] {v_247}, new Results((V)"food")));

R.Call(v_60, R.Join(new V[] {v_247}, new Results((V)"food")));
R.Call(v_39, R.Join(new V[] {v_247, (V)"move", (V)1.0d}, new Results((V)0.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_248}, new Results((V)"road_flask"))).First, (V)"speed") == (V)20.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_248}, new Results((V)"road_flask"))).First, (V)"expires_at") == (V)300.0d))));
var values_249 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"road_flask"}, new Results((V)10.0d))));
V v_250 = values_249[0];
V v_251 = values_249[1];

R.Call(v_60, R.Join(new V[] {v_250}, new Results((V)"health")));

R.Call(v_39, R.Join(new V[] {v_250, (V)"move", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(v_71, R.Join(new V[] {v_251}, new Results((V)"road_flask"))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Field Dressing cleanses real Burning, cancels its pending tick, and caps bonus healing"}, new Results(new V((RuleFunction)(args_252 => {

var values_253 = R.Join(new V[] {}, R.Call(v_75, R.Join(new V[] {}, new Results((V)"field_dressing"))));
V v_254 = values_253[0];
V v_255 = values_253[1];

var values_256 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_255, (V)"statuses"), (V)"burning"), (V)"schedule_id")));
V v_257 = values_256[0];
R.Call(v_60, R.Join(new V[] {v_254, (V)"health", (V)6.0d}, new Results((V)6.0d)));

var values_258 = R.Join(new V[] {}, new Results(R.Get(v_255, (V)"hp")));
V v_259 = values_258[0];
var values_260 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {v_254, (V)"move", (-(V)1.0d)}, new Results((V)0.0d))));
V v_261 = values_260[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(R.Get(v_255, (V)"statuses"), (V)"burning"))), () => (V)(R.Get(v_255, (V)"hp") == R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_255, (V)"max_hp")}, new Results((V)(v_259 + (V)13.0d)))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_49, R.Join(new V[] {v_261}, new Results((V)"StatusCleansed"))).First == (V)1.0d), () => (V)(R.Call(v_49, R.Join(new V[] {v_261}, new Results((V)"StatusTick"))).First == (V)0.0d)))));
foreach (var row_262 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_254, (V)"schedule"))))))) {
V v_263 = row_262[0];
V v_264 = row_262[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_264, (V)"id") != v_257))));
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_254))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_254)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"food does not masquerade as a health pickup or cancel a pending burn"}, new Results(new V((RuleFunction)(args_265 => {

var values_266 = R.Join(new V[] {}, R.Call(v_75, R.Join(new V[] {}, new Results((V)"field_dressing"))));
V v_267 = values_266[0];
V v_268 = values_266[1];

var values_269 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_270 = v_268;
V key_271 = (V)"hp";
R.Set(obj_270, key_271, values_269[0]);

R.Call(v_60, R.Join(new V[] {v_267, (V)"food", (V)6.0d}, new Results((V)6.0d)));
var values_272 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {v_267, (V)"move", (-(V)1.0d)}, new Results((V)0.0d))));
V v_273 = values_272[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_49, R.Join(new V[] {v_273}, new Results((V)"StatusCleansed"))).First == (V)0.0d), () => (V)(R.Call(v_49, R.Join(new V[] {v_273}, new Results((V)"Healed"))).First == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_49, R.Join(new V[] {v_273}, new Results((V)"StatusTick"))).First == (V)1.0d), () => R.Get(R.Get(v_268, (V)"statuses"), (V)"burning")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_268, (V)"hp") == (V)((V)12.0d - R.Get(R.Get(R.Get(R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/emberbrand"))).First, (V)"parameters"), (V)"common"), (V)"power"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Ration Reserve pays out only once per food and spends a full-health charge too"}, new Results(new V((RuleFunction)(args_274 => {

var values_275 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"ration_reserve"}, new Results((V)10.0d))));
V v_276 = values_275[0];
V v_277 = values_275[1];

R.Call(v_60, R.Join(new V[] {v_276}, new Results((V)"food")));

R.Call(v_60, R.Join(new V[] {v_276}, new Results((V)"food")));
R.Call(v_39, R.Join(new V[] {v_276, (V)"move", (V)1.0d}, new Results((V)0.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_277, (V)"hp") == (V)18.0d), () => (V)(R.Get(R.Call(v_71, R.Join(new V[] {v_277}, new Results((V)"ration_reserve"))).First, (V)"charges") == (V)1.0d)))));
var values_278 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {v_276}, new Results((V)"wait"))));
V v_279 = values_278[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_277, (V)"hp") == (V)21.0d), () => (V)(R.Get(R.Call(v_71, R.Join(new V[] {v_277}, new Results((V)"ration_reserve"))).First, (V)"charges") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_49, R.Join(new V[] {v_279}, new Results((V)"Healed"))).First == (V)1.0d))));
R.Call(v_39, R.Join(new V[] {v_276}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_277, (V)"hp") == (V)21.0d))));
var values_280 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"ration_reserve"}, new Results((V)23.0d))));
V v_281 = values_280[0];
V v_282 = values_280[1];

R.Call(v_60, R.Join(new V[] {v_281}, new Results((V)"food")));

R.Call(v_39, R.Join(new V[] {v_281, (V)"move", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_282, (V)"hp") == (V)24.0d))));

R.Call(v_39, R.Join(new V[] {v_281}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(v_71, R.Join(new V[] {v_282}, new Results((V)"ration_reserve"))).First, (V)"charges") == (V)0.0d), () => (V)(R.Get(v_282, (V)"hp") == (V)24.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Ration Reserve expires and health pickups cannot replenish its charge"}, new Results(new V((RuleFunction)(args_283 => {

var values_284 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"ration_reserve"}, new Results((V)10.0d))));
V v_285 = values_284[0];
V v_286 = values_284[1];

R.Call(v_60, R.Join(new V[] {v_285}, new Results((V)"food")));

R.Call(v_39, R.Join(new V[] {v_285, (V)"move", (V)1.0d}, new Results((V)0.0d)));
{ double end_289 = R.Num((V)6.0d), step_290 = R.Num((V)1.0d);
for (double i_288 = R.Num((V)1.0d); step_290 > 0 ? i_288 <= end_289 : i_288 >= end_289; i_288 += step_290) {
V v_287 = i_288;
R.Call(v_39, R.Join(new V[] {v_285, (V)"basic_attack", (V)0.0d}, new Results((V)1.0d)));
}
}
var values_291 = R.Join(new V[] {}, new Results(R.Get(v_286, (V)"hp")));
V v_292 = values_291[0];

R.Call(v_39, R.Join(new V[] {v_285}, new Results((V)"wait")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_286, (V)"hp") == v_292))));
var values_293 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"ration_reserve"}, new Results((V)10.0d))));
V v_294 = values_293[0];
V v_295 = values_293[1];

R.Call(v_60, R.Join(new V[] {v_294}, new Results((V)"health")));

R.Call(v_39, R.Join(new V[] {v_294, (V)"move", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(v_71, R.Join(new V[] {v_295}, new Results((V)"ration_reserve"))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Broker seal requires the full chest price and refunds one real purchase without a pickup proc"}, new Results(new V((RuleFunction)(args_296 => {

var values_297 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"brokers_seal"))));
V v_298 = values_297[0];
V v_299 = values_297[1];

R.Call(v_22, R.Join(new V[] {v_298}, new Results((V)"prospectors_heart")));
var values_300 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_301 = v_299;
V key_302 = (V)"hp";
R.Set(obj_301, key_302, values_300[0]);
var values_303 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_chest"), R.Join(new V[] {v_298, (V)"core:chest/standard", (V)3.0d}, new Results((V)2.0d))));
V v_304 = values_303[0];
var values_305 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"boon_id", (V)"core:boon/opening_cut"), new Entry((V)"rarity", (V)"common"))), new Entry((V)2, R.Table(new Entry((V)"boon_id", (V)"core:boon/thorn_mirror"), new Entry((V)"rarity", (V)"common"))), new Entry((V)3, R.Table(new Entry((V)"boon_id", (V)"core:boon/longblade_geometry"), new Entry((V)"rarity", (V)"common"))))));
var obj_306 = v_304;
V key_307 = (V)"offers";
R.Set(obj_306, key_307, values_305[0]);
var values_308 = R.Join(new V[] {}, new Results((V)(R.Get(v_304, (V)"price") - (V)1.0d)));
var obj_309 = v_298;
V key_310 = (V)"currency";
R.Set(obj_309, key_310, values_308[0]);
var values_311 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_298))));
V v_312 = values_311[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_298}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_304, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))).First)))));
R.Call(v_3, R.Join(new V[] {v_312}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_298)))));
var values_313 = R.Join(new V[] {}, new Results(R.Get(v_304, (V)"price")));
var obj_314 = v_298;
V key_315 = (V)"currency";
R.Set(obj_314, key_315, values_313[0]);
var values_316 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_298}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_304, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
V v_317 = values_316[0];
V v_318 = values_316[1];
V v_319 = values_316[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_317}, new Results(v_318)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_298, (V)"currency") == R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_304, (V)"price")}, new Results((V)3.0d))).First), () => R.Get(v_304, (V)"claimed")), () => (V)(R.Get(v_299, (V)"hp") == (V)10.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_49, R.Join(new V[] {v_319}, new Results((V)"CurrencyRefunded"))).First == (V)1.0d), () => (V)(R.Call(v_49, R.Join(new V[] {v_319}, new Results((V)"CurrencyCollected"))).First == (V)0.0d)))));
var values_320 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_298))));
v_312 = values_320[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_298}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_304, (V)"id")), new Entry((V)"offer_index", (V)2.0d))))).First)))));
R.Call(v_3, R.Join(new V[] {v_312}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_298)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Broker refund is capped by price even with uncapped mixed rarity copies"}, new Results(new V((RuleFunction)(args_321 => {

var values_322 = R.Join(new V[] {}, R.Call(v_28, Results.Empty));
V v_323 = values_322[0];

R.Call(v_22, R.Join(new V[] {v_323, (V)"brokers_seal", (V)"common"}, new Results((V)100.0d)));
var values_324 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_chest"), R.Join(new V[] {v_323, (V)"core:chest/standard", (V)3.0d}, new Results((V)2.0d))));
V v_325 = values_324[0];

var values_326 = R.Join(new V[] {}, new Results(R.Get(v_325, (V)"price")));
var obj_327 = v_323;
V key_328 = (V)"currency";
R.Set(obj_327, key_328, values_326[0]);
var values_329 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_323}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_325, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
V v_330 = values_329[0];
V v_331 = values_329[1];

R.Call(rt.Global("assert"), R.Join(new V[] {v_330}, new Results(v_331)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_323, (V)"currency") == R.Get(v_325, (V)"price")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Funeral Coin enriches exactly the finite killed foe drop and requires a later pickup"}, new Results(new V((RuleFunction)(args_332 => {

var values_333 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"funeral_coin"))));
V v_334 = values_333[0];
var values_335 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_334, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)100000.0d))));
V v_336 = values_335[0];

var values_337 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_338 = v_336;
V key_339 = (V)"hp";
R.Set(obj_338, key_339, values_337[0]);
var values_340 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {v_334, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_341 = values_340[0];
var values_342 = Results.Empty;
V v_343 = values_342[0];
foreach (var row_344 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_334, (V)"drops"))))))) {
V v_345 = row_344[0];
V v_346 = row_344[1];
if (R.Truth(R.And((V)(R.Get(v_346, (V)"kind") == (V)"currency"), () => (V)(R.Get(v_346, (V)"killed_entity_id") == R.Get(v_336, (V)"id"))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_343)))));

var values_347 = R.Join(new V[] {}, new Results(v_346));
v_343 = values_347[0];
}
}
var values_348 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_19, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_336, (V)"definition_id")))).First, (V)"currency_drop")));
V v_349 = values_348[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_343, () => (V)(R.Get(v_343, (V)"amount") == (V)(v_349 + (V)3.0d))), () => (V)(R.Get(v_334, (V)"currency") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_49, R.Join(new V[] {v_341}, new Results((V)"CurrencyDropEnriched"))).First == (V)1.0d), () => (V)(R.Call(v_49, R.Join(new V[] {v_341}, new Results((V)"EntityKilled"))).First == (V)1.0d)))));
R.Call(v_39, R.Join(new V[] {v_334, (V)"move", (V)1.0d}, new Results((V)0.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_343, (V)"collected"), () => (V)(R.Get(v_334, (V)"currency") == (V)(v_349 + (V)3.0d))))));
R.Call(v_39, R.Join(new V[] {v_334, (V)"move", (-(V)1.0d)}, new Results((V)0.0d)));

R.Call(v_39, R.Join(new V[] {v_334, (V)"move", (V)1.0d}, new Results((V)0.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_334, (V)"currency") == (V)(v_349 + (V)3.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"reward-exempt revived or called foes cannot farm Funeral Coin gold"}, new Results(new V((RuleFunction)(args_350 => {

var values_351 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"funeral_coin"))));
V v_352 = values_351[0];
var values_353 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_352, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)100000.0d))));
V v_354 = values_353[0];
var values_355 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_356 = v_354;
V key_357 = (V)"hp";
R.Set(obj_356, key_357, values_355[0]);

var values_358 = R.Join(new V[] {}, new Results((V)true));
var obj_359 = v_354;
V key_360 = (V)"reward_exempt";
R.Set(obj_359, key_360, values_358[0]);
var values_361 = R.Join(new V[] {}, R.Call(v_39, R.Join(new V[] {v_352, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_362 = values_361[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_49, R.Join(new V[] {v_362}, new Results((V)"CurrencyDropEnriched"))).First == (V)0.0d), () => (V)((V)R.Length(R.Get(v_352, (V)"drops")) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_49, R.Join(new V[] {v_362, (V)"BoonActivated"}, new Results((V)"funeral_coin"))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new supply state and pending Burning/Echo resume with exact events, RNG and scheduled costs"}, new Results(new V((RuleFunction)(args_363 => {

var values_364 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {V.Nil}, new Results((V)20.0d))));
V v_365 = values_364[0];
V v_366 = values_364[1];
foreach (var row_367 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"road_flask"), new Entry((V)2, (V)"ration_reserve"), new Entry((V)3, (V)"overflow_chalice"), new Entry((V)4, (V)"temporal_echo"), new Entry((V)5, (V)"emberbrand")))))))) {
V v_368 = row_367[0];
V v_369 = row_367[1];
R.Call(v_22, R.Join(new V[] {v_365}, new Results(v_369)));
}
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_365, (V)"core:enemy/mossbound_guard", (V)4.0d, (V)2.0d}, new Results((V)100000.0d)));
R.Call(v_60, R.Join(new V[] {v_365}, new Results((V)"health")));

R.Call(v_39, R.Join(new V[] {v_365, (V)"move", (V)1.0d}, new Results((V)0.0d)));
var values_370 = R.Join(new V[] {}, new Results((V)20.0d));
var obj_371 = v_366;
V key_372 = (V)"hp";
R.Set(obj_371, key_372, values_370[0]);

R.Call(v_60, R.Join(new V[] {v_365, (V)"food", (V)2.0d}, new Results((V)2.0d)));

R.Call(v_39, R.Join(new V[] {v_365, (V)"move", (-(V)1.0d)}, new Results((V)0.0d)));

R.Call(v_39, R.Join(new V[] {v_365, (V)"move", (V)1.0d}, new Results((V)0.0d)));
R.Call(v_39, R.Join(new V[] {v_365, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_365, (V)"delayed_effects")))).First, () => (V)(R.Get(R.Call(v_71, R.Join(new V[] {v_366}, new Results((V)"road_flask"))).First, (V)"speed") == (V)20.0d)))));
var values_373 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_365))))))))));
V v_374 = values_373[0];
foreach (var row_375 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)5, R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))))))) {
V v_376 = row_375[0];
V v_377 = row_375[1];
var values_378 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_365}, new Results(v_377))));
V v_379 = values_378[0];
V v_380 = values_378[1];
V v_381 = values_378[2];

var values_382 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_374}, new Results(v_377))));
V v_383 = values_382[0];
V v_384 = values_382[1];
V v_385 = values_382[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_379, () => v_383)}, new Results(R.Or(v_380, () => v_384))));

R.Call(v_3, R.Join(new V[] {v_381}, new Results(v_385)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_365))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_374)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"invalid actions and repeated previews neither consume supply memory nor advance RNG"}, new Results(new V((RuleFunction)(args_386 => {

var values_387 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {(V)"ration_reserve"}, new Results((V)10.0d))));
V v_388 = values_387[0];

R.Call(v_22, R.Join(new V[] {v_388}, new Results((V)"overflow_chalice")));

R.Call(v_22, R.Join(new V[] {v_388}, new Results((V)"road_flask")));
R.Call(v_60, R.Join(new V[] {v_388}, new Results((V)"food")));

R.Call(v_39, R.Join(new V[] {v_388, (V)"move", (V)1.0d}, new Results((V)0.0d)));
var values_389 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_388))));
V v_390 = values_389[0];
{ double end_393 = R.Num((V)10.0d), step_394 = R.Num((V)1.0d);
for (double i_392 = R.Num((V)1.0d); step_394 > 0 ? i_392 <= end_393 : i_392 >= end_393; i_392 += step_394) {
V v_391 = i_392;
R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_388}, new Results((V)"wait")));

R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_388, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_388}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_388}, new Results(R.Table(new Entry((V)"kind", (V)"cancel"))))).First)))));
}
}
R.Call(v_3, R.Join(new V[] {v_390}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_388)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new supply predicates, art and effect scales reject malformed authored data"}, new Results(new V((RuleFunction)(args_395 => {

V v_396 = V.Nil;
v_396 = new V((RuleFunction)(args_397 => {
V v_398 = args_397[0];
var values_399 = R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/road_flask"))))))));
V v_400 = values_399[0];
R.Call(v_398, R.Join(new V[] {}, new Results(v_400)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_9, (V)"validate"), R.Table(new Entry(R.Get(v_400, (V)"id"), v_400))}, new Results(R.Table(new Entry((V)1, R.Get(v_400, (V)"id")))))).First)))));
return Results.Empty;
}));
R.Call(v_396, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_401 => {
V v_402 = args_401[0];
var values_403 = R.Join(new V[] {}, new Results((V)1078.0d));
var obj_404 = R.Get(v_402, (V)"art");
V key_405 = (V)"index";
R.Set(obj_404, key_405, values_403[0]);
return Results.Empty;
})))));
R.Call(v_396, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_406 => {
V v_407 = args_406[0];
var values_408 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"op", (V)"supply_kind"), new Entry((V)"value", (V)"gold"))));
var obj_409 = v_407;
V key_410 = (V)"predicate";
R.Set(obj_409, key_410, values_408[0]);
return Results.Empty;
})))));
R.Call(v_396, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_411 => {
V v_412 = args_411[0];
var values_413 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"op", (V)"event_amount"), new Entry((V)"field", (V)"secret_raw_state"), new Entry((V)"min", (V)1.0d))));
var obj_414 = v_412;
V key_415 = (V)"predicate";
R.Set(obj_414, key_415, values_413[0]);
return Results.Empty;
})))));
R.Call(v_396, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_416 => {
V v_417 = args_416[0];
var values_418 = R.Join(new V[] {}, new Results((V)"supply_excess"));
var obj_419 = R.Get(R.Get(v_417, (V)"effects"), (V)1.0d);
V key_420 = (V)"scale";
R.Set(obj_419, key_420, values_418[0]);
return Results.Empty;
})))));
R.Call(v_396, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_421 => {
V v_422 = args_421[0];
var values_423 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"cleanse_owner"), new Entry((V)"status", (V)"any"))))));
var obj_424 = v_422;
V key_425 = (V)"effects";
R.Set(obj_424, key_425, values_423[0]);
return Results.Empty;
})))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
