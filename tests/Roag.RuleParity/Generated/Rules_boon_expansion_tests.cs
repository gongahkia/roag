// Static C# port of tests/boon_expansion_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 798016198081d1171670a365168ddaf5ad29f3e7b99c3042579da86264664d6d
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_boon_expansion_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_12 = values_11[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
V v_15 = args_13[1];
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
V v_20 = args_17[2];
V v_21 = args_17[3];
R.Call(R.Get(v_4, (V)"set_boon_stacks"), R.Join(new V[] {v_18, R.Get(v_18, (V)"player_id"), R.Concat((V)"core:boon/", v_19), R.Or(v_20, () => (V)"rare")}, new Results(R.Or(v_21, () => (V)1.0d))));
return Results.Empty;
}));
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
V v_25 = args_23[1];
V v_26 = args_23[2];
V v_27 = args_23[3];
var values_28 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"spawn_enemy"), R.Join(new V[] {v_24, R.Or(v_25, () => (V)"core:enemy/mossbound_guard"), v_26}, new Results(v_27))));
V v_29 = values_28[0];
R.Call(R.Get(v_6, (V)"remove_for_actor"), R.Join(new V[] {v_24}, new Results(R.Get(v_29, (V)"id"))));
return R.Join(new V[] {}, new Results(v_29));
return Results.Empty;
}));
V v_30 = V.Nil;
v_30 = new V((RuleFunction)(args_31 => {
V v_32 = args_31[0];
V v_33 = args_31[1];
var values_34 = R.Join(new V[] {}, new Results(R.Table()));
V v_35 = values_34[0];
foreach (var row_36 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_32)))))) {
V v_37 = row_36[0];
V v_38 = row_36[1];
if (R.Truth((V)(R.Get(v_38, (V)"kind") == v_33))) {
var values_39 = R.Join(new V[] {}, new Results(v_38));
var obj_40 = v_35;
V key_41 = (V)((V)R.Length(v_35) + (V)1.0d);
R.Set(obj_40, key_41, values_39[0]);
}
}
return R.Join(new V[] {}, new Results(v_35));
return Results.Empty;
}));
V v_42 = V.Nil;
v_42 = new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
var values_46 = R.Join(new V[] {}, new Results(R.Table()));
V v_47 = values_46[0];
foreach (var row_48 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {v_44}, new Results((V)"BoonActivated")))))))) {
V v_49 = row_48[0];
V v_50 = row_48[1];
if (R.Truth((V)(R.Get(v_50, (V)"boon_id") == R.Concat((V)"core:boon/", v_45)))) {
var values_51 = R.Join(new V[] {}, new Results(v_50));
var obj_52 = v_47;
V key_53 = (V)((V)R.Length(v_47) + (V)1.0d);
R.Set(obj_52, key_53, values_51[0]);
}
}
return R.Join(new V[] {}, new Results(v_47));
return Results.Empty;
}));
R.Call(v_14, R.Join(new V[] {(V)"original twenty boons remain validated and available before unlocks"}, new Results(new V((RuleFunction)(args_54 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_2, (V)"order")) == (V)((V)53.0d + (V)R.Length(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"order")))), () => (V)(R.Get(v_2, (V)"starter_count") == (V)20.0d)), () => R.Call(R.Get(v_2, (V)"validate"), Results.Empty).First))));
var values_55 = R.Join(new V[] {}, new Results(R.Table()));
V v_56 = values_55[0];
foreach (var row_57 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_58 = row_57[0];
V v_59 = row_57[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_56, v_59))))));

var values_60 = R.Join(new V[] {}, new Results((V)true));
var obj_61 = v_56;
V key_62 = v_59;
R.Set(obj_61, key_62, values_60[0]);
var values_63 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_59))))));
V v_64 = values_63[0];
var values_65 = R.Join(new V[] {}, new Results((-(V)1.0d)));
V v_66 = values_65[0];
foreach (var row_67 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_68 = row_67[0];
V v_69 = row_67[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_64, (V)"parameters"), v_69), (V)"power") > v_66))));
var values_70 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_64, (V)"parameters"), v_69), (V)"power")));
v_66 = values_70[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_2, (V)"offer_text"), R.Join(new V[] {v_59}, new Results(v_69))).First) > (V)R.Length(R.Get(v_64, (V)"description"))))));
}
}
var values_71 = R.Join(new V[] {}, new Results(R.Table()));
V v_72 = values_71[0];
{ double end_75 = R.Num((V)600.0d), step_76 = R.Num((V)1.0d);
for (double i_74 = R.Num((V)1.0d); step_76 > 0 ? i_74 <= end_75 : i_74 >= end_75; i_74 += step_76) {
V v_73 = i_74;
var values_77 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results(v_73))));
V v_78 = values_77[0];
var values_79 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"make_offers"), R.Join(new V[] {v_78}, R.Call(R.Get(v_10, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/standard"))))));
V v_80 = values_79[0];
foreach (var row_81 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_80)))))) {
V v_82 = row_81[0];
V v_83 = row_81[1];
var values_84 = R.Join(new V[] {}, new Results((V)true));
var obj_85 = v_72;
V key_86 = R.Get(v_83, (V)"boon_id");
R.Set(obj_85, key_86, values_84[0]);
}
}
}
foreach (var row_87 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_88 = row_87[0];
V v_89 = row_87[1];
if (R.Truth((V)(v_88 <= R.Get(v_2, (V)"starter_count")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_72, v_89)}, new Results(R.Concat(v_89, (V)" never offered"))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_72, v_89)))}, new Results(R.Concat(v_89, (V)" leaked before unlock"))));
}
}
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"mixed rarity new stacks aggregate without cap and serialize"}, new Results(new V((RuleFunction)(args_90 => {

var values_91 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)31.0d))));
V v_92 = values_91[0];
R.Call(v_16, R.Join(new V[] {v_92, (V)"emberbrand", (V)"common"}, new Results((V)100.0d)));
R.Call(v_16, R.Join(new V[] {v_92, (V)"emberbrand", (V)"legendary"}, new Results((V)20.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_12, (V)"aggregate"), R.Join(new V[] {(V)"core:boon/emberbrand", R.Get(R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_92))).First, (V)"boons"), (V)"core:boon/emberbrand")}, new Results((V)"power"))).First == (V)320.0d))));
R.Call(v_15, R.Join(new V[] {R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_92))).First}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_92)))))))));
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"Burning ticks on scheduler once per target and keeps player kill credit"}, new Results(new V((RuleFunction)(args_93 => {

var values_94 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)32.0d))));
V v_95 = values_94[0];
R.Call(v_16, R.Join(new V[] {v_95, (V)"emberbrand"}, new Results((V)"rare")));
var values_96 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_95, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_97 = values_96[0];
var values_98 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_95}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_99 = values_98[0];
V v_100 = values_98[1];
V v_101 = values_98[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_101}, new Results((V)"StatusApplied"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_101}, new Results((V)"StatusTick"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_97, (V)"statuses"), (V)"burning"), (V)"remaining") == (V)2.0d), () => (V)(R.Get(R.Get(R.Get(v_97, (V)"statuses"), (V)"burning"), (V)"next_due") == (V)200.0d)))));
var values_102 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_95))))));
V v_103 = values_102[0];
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_95}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_105 = values_104[0];
V v_106 = values_104[1];
V v_107 = values_104[2];
var values_108 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_103}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_109 = values_108[0];
V v_110 = values_108[1];
V v_111 = values_108[2];
R.Call(v_15, R.Join(new V[] {v_107}, new Results(v_111)));

R.Call(v_15, R.Join(new V[] {R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_95))).First}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_103)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_107}, new Results((V)"StatusTick"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_107}, new Results((V)"EntityKilled"))).First) == (V)0.0d)))));
var values_112 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_95}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_113 = values_112[0];
V v_114 = values_112[1];
V v_115 = values_112[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_115}, new Results((V)"StatusTick"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_115}, new Results((V)"EntityKilled"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_30, R.Join(new V[] {v_115}, new Results((V)"EntityKilled"))).First, (V)1.0d), (V)"source_id") == R.Get(v_95, (V)"player_id")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_95, (V)"progression"), (V)"xp_total") > (V)0.0d), () => (V)((V)R.Length(R.Get(v_95, (V)"drops")) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"entity"), R.Join(new V[] {v_95}, new Results(R.Get(v_97, (V)"id")))).First == V.Nil))));
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"Burning refresh keeps one scheduled tick and stronger mixed rarity power"}, new Results(new V((RuleFunction)(args_116 => {

var values_117 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)320.0d))));
V v_118 = values_117[0];
R.Call(v_16, R.Join(new V[] {v_118, (V)"emberbrand"}, new Results((V)"common")));
var values_119 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_118, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_120 = values_119[0];
var values_121 = R.Join(new V[] {}, new Results((V)40.0d));
var obj_122 = v_120;
V key_123 = (V)"hp";
R.Set(obj_122, key_123, values_121[0]);

var values_124 = R.Join(new V[] {}, new Results((V)40.0d));
var obj_125 = v_120;
V key_126 = (V)"max_hp";
R.Set(obj_125, key_126, values_124[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_118}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_127 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_120, (V)"statuses"), (V)"burning")));
V v_128 = values_127[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_128, (V)"power") == (V)2.0d), () => (V)(R.Get(v_128, (V)"remaining") == (V)2.0d)))));
var values_129 = R.Join(new V[] {}, new Results(R.Get(v_128, (V)"schedule_id")));
V v_130 = values_129[0];
R.Call(v_16, R.Join(new V[] {v_118, (V)"emberbrand"}, new Results((V)"rare")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_118}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_131 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_120, (V)"statuses"), (V)"burning")));
v_128 = values_131[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_128, (V)"power") == (V)6.0d), () => (V)(R.Get(v_128, (V)"remaining") == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_128, (V)"schedule_id") != v_130), () => (V)(R.Get(v_128, (V)"next_due") == (V)300.0d)))));
var values_132 = R.Join(new V[] {}, new Results((V)0.0d));
V v_133 = values_132[0];
foreach (var row_134 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_118, (V)"schedule"))))))) {
V v_135 = row_134[0];
V v_136 = row_134[1];
if (R.Truth(R.And((V)(R.Get(v_136, (V)"kind") == (V)"effect"), () => (V)(R.Get(v_136, (V)"actor_id") == R.Get(v_120, (V)"id"))))) {
var values_137 = R.Join(new V[] {}, new Results((V)(v_133 + (V)1.0d)));
v_133 = values_137[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_133 == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"Emberbrand Burning enables Flashpoint and a credited Grave Dividend kill"}, new Results(new V((RuleFunction)(args_138 => {

var values_139 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)33.0d))));
V v_140 = values_139[0];
var values_141 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_140))));
V v_142 = values_141[0];

var values_143 = R.Join(new V[] {}, new Results((V)18.0d));
var obj_144 = v_142;
V key_145 = (V)"hp";
R.Set(obj_144, key_145, values_143[0]);
R.Call(v_16, R.Join(new V[] {v_140, (V)"longblade_geometry"}, new Results((V)"common")));
R.Call(v_16, R.Join(new V[] {v_140, (V)"emberbrand"}, new Results((V)"rare")));
R.Call(v_16, R.Join(new V[] {v_140, (V)"flashpoint"}, new Results((V)"legendary")));
R.Call(v_16, R.Join(new V[] {v_140, (V)"grave_dividend"}, new Results((V)"rare")));
var values_146 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_140, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_147 = values_146[0];
var values_148 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_140, (V)"core:enemy/mossbound_guard", (V)4.0d}, new Results((V)2.0d))));
V v_149 = values_148[0];
var values_150 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_140}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_151 = values_150[0];
V v_152 = values_150[1];
V v_153 = values_150[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_153}, new Results((V)"StatusApplied"))).First) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_153}, new Results((V)"flashpoint"))).First) >= (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_153}, new Results((V)"EntityKilled"))).First) >= (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_153}, new Results((V)"grave_dividend"))).First) >= (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_142, (V)"hp") > (V)18.0d))));
var values_154 = R.Join(new V[] {}, new Results((V)false));
V v_155 = values_154[0];
foreach (var row_156 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {v_153}, new Results((V)"EntityKilled")))))))) {
V v_157 = row_156[0];
V v_158 = row_156[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_158, (V)"source_id") == R.Get(v_142, (V)"id")))));
foreach (var row_159 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_158, (V)"ancestry"))))))) {
V v_160 = row_159[0];
V v_161 = row_159[1];
if (R.Truth((V)(v_161 == R.Concat(R.Get(v_142, (V)"id"), (V)":core:boon/flashpoint")))) {
var values_162 = R.Join(new V[] {}, new Results((V)true));
v_155 = values_162[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_155}, new Results((V)"Flashpoint did not cause a credited secondary kill")));
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"Opening Cut uses pre-batch full HP and Crowdbreaker counts other living hostiles"}, new Results(new V((RuleFunction)(args_163 => {

var values_164 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)34.0d))));
V v_165 = values_164[0];
R.Call(v_16, R.Join(new V[] {v_165, (V)"opening_cut"}, new Results((V)"rare")));
R.Call(v_16, R.Join(new V[] {v_165, (V)"crowdbreaker"}, new Results((V)"rare")));
var values_166 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_165, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_167 = values_166[0];
R.Call(v_22, R.Join(new V[] {v_165, (V)"core:enemy/mossbound_guard", (V)4.0d}, new Results((V)2.0d)));
var values_168 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_165}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_169 = values_168[0];
V v_170 = values_168[1];
V v_171 = values_168[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_171}, new Results((V)"opening_cut"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_171}, new Results((V)"crowdbreaker"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_30, R.Join(new V[] {v_171}, new Results((V)"DamageTaken"))).First, (V)1.0d), (V)"hp_before") == (V)10.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_30, R.Join(new V[] {v_171}, new Results((V)"DamageTaken"))).First, (V)1.0d), (V)"adjacent_hostile_count") == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_167, (V)"hp") < (V)7.0d))));
var values_172 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_165}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_173 = values_172[0];
V v_174 = values_172[1];
V v_175 = values_172[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_175}, new Results((V)"opening_cut"))).First) == (V)0.0d))));
var values_176 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)340.0d))));
V v_177 = values_176[0];
var values_178 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_179 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_177))).First, (V)"position");
V key_180 = (V)"x";
R.Set(obj_179, key_180, values_178[0]);

var values_181 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_182 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_177))).First, (V)"position");
V key_183 = (V)"y";
R.Set(obj_182, key_183, values_181[0]);
R.Call(v_16, R.Join(new V[] {v_177, (V)"opening_cut"}, new Results((V)"rare")));
var values_184 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_177, (V)"core:enemy/mossbound_guard", (V)4.0d}, new Results((V)4.0d))));
V v_185 = values_184[0];
var values_186 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_177, (V)"core:enemy/mossbound_guard", (V)4.0d}, new Results((V)6.0d))));
V v_187 = values_186[0];
var values_188 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_177}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_189 = values_188[0];
V v_190 = values_188[1];
V v_191 = values_188[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_191}, new Results((V)"opening_cut"))).First) == (V)2.0d))));
var values_192 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_30, R.Join(new V[] {v_191}, new Results((V)"DamageBatchResolved"))).First, (V)1.0d)));
V v_193 = values_192[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_193, (V)"outcomes")) == (V)2.0d), () => (V)(R.Get(R.Get(R.Get(v_193, (V)"outcomes"), (V)1.0d), (V)"hp_before") == (V)10.0d)), () => (V)(R.Get(R.Get(R.Get(v_193, (V)"outcomes"), (V)2.0d), (V)"hp_before") == (V)10.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_185, (V)"hp") < (V)8.0d), () => (V)(R.Get(v_187, (V)"hp") < (V)8.0d)))));
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"Final Spark threshold and Perfect Guard block are distinct from positive damage"}, new Results(new V((RuleFunction)(args_194 => {

var values_195 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)35.0d))));
V v_196 = values_195[0];
R.Call(v_16, R.Join(new V[] {v_196, (V)"final_spark"}, new Results((V)"rare")));
var values_197 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_196))));
V v_198 = values_197[0];
var values_199 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_196, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_200 = values_199[0];
var values_201 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_196}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_202 = values_201[0];
V v_203 = values_201[1];
V v_204 = values_201[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_204}, new Results((V)"final_spark"))).First) == (V)0.0d))));
var values_205 = R.Join(new V[] {}, new Results((V)9.0d));
var obj_206 = v_198;
V key_207 = (V)"hp";
R.Set(obj_206, key_207, values_205[0]);

var values_208 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_209 = v_200;
V key_210 = (V)"hp";
R.Set(obj_209, key_210, values_208[0]);
var values_211 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_196}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_212 = values_211[0];
V v_213 = values_211[1];
V v_214 = values_211[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_214}, new Results((V)"final_spark"))).First) == (V)1.0d))));
var values_215 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)36.0d))));
V v_216 = values_215[0];
var values_217 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_216))));
V v_218 = values_217[0];

var values_219 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_220 = R.Get(v_218, (V)"position");
V key_221 = (V)"x";
R.Set(obj_220, key_221, values_219[0]);

var values_222 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_223 = R.Get(v_218, (V)"position");
V key_224 = (V)"y";
R.Set(obj_223, key_224, values_222[0]);

var values_225 = R.Join(new V[] {}, new Results((V)99.0d));
var obj_226 = v_218;
V key_227 = (V)"defense";
R.Set(obj_226, key_227, values_225[0]);
R.Call(v_16, R.Join(new V[] {v_216, (V)"perfect_guard"}, new Results((V)"rare")));
var values_228 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"spawn_enemy"), R.Join(new V[] {v_216, (V)"core:enemy/thornspitter", (V)8.0d}, new Results((V)2.0d))));
V v_229 = values_228[0];
var values_230 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_216}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_231 = values_230[0];
V v_232 = values_230[1];
V v_233 = values_230[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_233}, new Results((V)"DamageBlocked"))).First) >= (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_233}, new Results((V)"perfect_guard"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_229, (V)"hp") < R.Get(v_229, (V)"max_hp")), () => (V)(R.Get(v_218, (V)"hp") == R.Get(v_218, (V)"max_hp"))))));
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"Harrier reacts to enemy windup once without interrupting ordinary damage"}, new Results(new V((RuleFunction)(args_234 => {

var values_235 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_236 = values_235[0];
var values_237 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_236))));
V v_238 = values_237[0];

var values_239 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_240 = R.Get(v_238, (V)"position");
V key_241 = (V)"x";
R.Set(obj_240, key_241, values_239[0]);

var values_242 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_243 = R.Get(v_238, (V)"position");
V key_244 = (V)"y";
R.Set(obj_243, key_244, values_242[0]);
R.Call(v_16, R.Join(new V[] {v_236, (V)"harriers_response", (V)"legendary"}, new Results((V)2.0d)));
var values_245 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"spawn_enemy"), R.Join(new V[] {v_236, (V)"core:enemy/thornspitter", (V)8.0d}, new Results((V)2.0d))));
V v_246 = values_245[0];
var values_247 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_248 = v_246;
V key_249 = (V)"hp";
R.Set(obj_248, key_249, values_247[0]);

var values_250 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_251 = v_246;
V key_252 = (V)"max_hp";
R.Set(obj_251, key_252, values_250[0]);
var values_253 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_236}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_254 = values_253[0];
V v_255 = values_253[1];
V v_256 = values_253[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_256}, new Results((V)"WindupStarted"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_256}, new Results((V)"harriers_response"))).First) >= (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_256}, new Results((V)"WindupCancelled"))).First) == (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_256}, new Results((V)"AttackPerformed"))).First) >= (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"Pulse Step batches adjacent foes and ignores blocked movement"}, new Results(new V((RuleFunction)(args_257 => {

var values_258 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)38.0d))));
V v_259 = values_258[0];
R.Call(v_16, R.Join(new V[] {v_259, (V)"pulse_step"}, new Results((V)"rare")));
var values_260 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_259, (V)"core:enemy/ruin_skitter", (V)4.0d}, new Results((V)2.0d))));
V v_261 = values_260[0];
var values_262 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_259, (V)"core:enemy/ruin_skitter", (V)2.0d}, new Results((V)3.0d))));
V v_263 = values_262[0];
var values_264 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_265 = values_264[0];
V v_266 = values_264[1];
V v_267 = values_264[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_267}, new Results((V)"pulse_step"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_267}, new Results((V)"DamageBatchResolved"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_261, (V)"hp") == (V)1.0d), () => (V)(R.Get(v_263, (V)"hp") == (V)1.0d)))));
var values_268 = R.Join(new V[] {}, new Results(R.Get(v_259, (V)"clock")));
V v_269 = values_268[0];
var values_270 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_271 = values_270[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_271)), () => (V)(R.Get(v_259, (V)"clock") == v_269)))));
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"Packed Powder uses disjoint triple deaths in the same chain"}, new Results(new V((RuleFunction)(args_272 => {

var values_273 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)39.0d))));
V v_274 = values_273[0];
R.Call(v_16, R.Join(new V[] {v_274, (V)"packed_powder"}, new Results((V)"rare")));
var values_275 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_276 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_274))).First, (V)"position");
V key_277 = (V)"x";
R.Set(obj_276, key_277, values_275[0]);

var values_278 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_279 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_274))).First, (V)"position");
V key_280 = (V)"y";
R.Set(obj_279, key_280, values_278[0]);
foreach (var row_281 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)5.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)6.0d)))))))))) {
V v_282 = row_281[0];
V v_283 = row_281[1];
var values_284 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_274, (V)"core:enemy/ruin_skitter", R.Get(v_283, (V)1.0d)}, new Results(R.Get(v_283, (V)2.0d)))));
V v_285 = values_284[0];

var values_286 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_287 = v_285;
V key_288 = (V)"hp";
R.Set(obj_287, key_288, values_286[0]);
}
var values_289 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_274}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_290 = values_289[0];
V v_291 = values_289[1];
V v_292 = values_289[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_292}, new Results((V)"EntityKilled"))).First) == (V)3.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_292}, new Results((V)"packed_powder"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_292}, new Results((V)"EffectApplied"))).First) == (V)1.0d))));
var values_293 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)40.0d))));
V v_294 = values_293[0];

R.Call(v_16, R.Join(new V[] {v_294, (V)"packed_powder"}, new Results((V)"rare")));
var values_295 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_296 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_294))).First, (V)"position");
V key_297 = (V)"x";
R.Set(obj_296, key_297, values_295[0]);

var values_298 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_299 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_294))).First, (V)"position");
V key_300 = (V)"y";
R.Set(obj_299, key_300, values_298[0]);
foreach (var row_301 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)5.0d)))))))))) {
V v_302 = row_301[0];
V v_303 = row_301[1];
var values_304 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_294, (V)"core:enemy/ruin_skitter", R.Get(v_303, (V)1.0d)}, new Results(R.Get(v_303, (V)2.0d)))));
V v_305 = values_304[0];

var values_306 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_307 = v_305;
V key_308 = (V)"hp";
R.Set(obj_307, key_308, values_306[0]);
}
var values_309 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_294}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_310 = values_309[0];
V v_311 = values_309[1];
V v_312 = values_309[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_312}, new Results((V)"packed_powder"))).First) == (V)0.0d))));
var values_313 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)401.0d))));
V v_314 = values_313[0];
var values_315 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_316 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_314))).First, (V)"position");
V key_317 = (V)"x";
R.Set(obj_316, key_317, values_315[0]);

var values_318 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_319 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_314))).First, (V)"position");
V key_320 = (V)"y";
R.Set(obj_319, key_320, values_318[0]);
R.Call(v_16, R.Join(new V[] {v_314, (V)"packed_powder"}, new Results((V)"rare")));
foreach (var row_321 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)5.0d)))))))))) {
V v_322 = row_321[0];
V v_323 = row_321[1];
var values_324 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_314, (V)"core:enemy/ruin_skitter", R.Get(v_323, (V)1.0d)}, new Results(R.Get(v_323, (V)2.0d)))));
V v_325 = values_324[0];

var values_326 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_327 = v_325;
V key_328 = (V)"hp";
R.Set(obj_327, key_328, values_326[0]);
}
var values_329 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_314}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_330 = values_329[0];
V v_331 = values_329[1];
V v_332 = values_329[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_332}, new Results((V)"packed_powder"))).First) == (V)0.0d))));
var values_333 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_314, (V)"core:enemy/ruin_skitter", (V)4.0d}, new Results((V)6.0d))));
V v_334 = values_333[0];

var values_335 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_336 = v_334;
V key_337 = (V)"hp";
R.Set(obj_336, key_337, values_335[0]);
var values_338 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_314}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))));
V v_339 = values_338[0];
V v_340 = values_338[1];
V v_341 = values_338[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_341}, new Results((V)"packed_powder"))).First) == (V)0.0d))));
foreach (var row_342 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)3.0d)))))))) {
V v_343 = row_342[0];
V v_344 = row_342[1];
var values_345 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)((V)400.0d + v_344)))));
V v_346 = values_345[0];
var values_347 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_348 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_346))).First, (V)"position");
V key_349 = (V)"x";
R.Set(obj_348, key_349, values_347[0]);

var values_350 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_351 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_346))).First, (V)"position");
V key_352 = (V)"y";
R.Set(obj_351, key_352, values_350[0]);
R.Call(v_16, R.Join(new V[] {v_346, (V)"packed_powder"}, new Results((V)"rare")));
R.Call(v_16, R.Join(new V[] {v_346, (V)"detonation_bloom"}, new Results((V)"rare")));
foreach (var row_353 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)5.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)6.0d)))))))))) {
V v_354 = row_353[0];
V v_355 = row_353[1];
var values_356 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_346, (V)"core:enemy/ruin_skitter", R.Get(v_355, (V)1.0d)}, new Results(R.Get(v_355, (V)2.0d)))));
V v_357 = values_356[0];

var values_358 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_359 = v_357;
V key_360 = (V)"hp";
R.Set(obj_359, key_360, values_358[0]);
}
{ double end_363 = R.Num(v_344), step_364 = R.Num((V)1.0d);
for (double i_362 = R.Num((V)1.0d); step_364 > 0 ? i_362 <= end_363 : i_362 >= end_363; i_362 += step_364) {
V v_361 = i_362;
var values_365 = R.Join(new V[] {}, new Results(R.Get(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)5.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)5.0d), new Entry((V)2, (V)5.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)3.0d), new Entry((V)2, (V)6.0d)))), v_361)));
V v_366 = values_365[0];
var values_367 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_346, (V)"core:enemy/ruin_skitter", R.Get(v_366, (V)1.0d)}, new Results(R.Get(v_366, (V)2.0d)))));
V v_368 = values_367[0];

var values_369 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_370 = v_368;
V key_371 = (V)"hp";
R.Set(obj_370, key_371, values_369[0]);
}
}
var values_372 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_346}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_373 = values_372[0];
V v_374 = values_372[1];
V v_375 = values_372[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_375}, new Results((V)"EntityKilled"))).First) == (V)((V)3.0d + v_344)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_375}, new Results((V)"packed_powder"))).First) == R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)3.0d + v_344) / (V)3.0d)))).First))));
}
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"Longblade strikes second cell without changing bump or base ability"}, new Results(new V((RuleFunction)(args_376 => {

var values_377 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)41.0d))));
V v_378 = values_377[0];
var values_379 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"effective_ability"), R.Join(new V[] {v_378, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_380 = values_379[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_380) == (V)1.0d))));
R.Call(v_16, R.Join(new V[] {v_378, (V)"longblade_geometry"}, new Results((V)"rare")));
var values_381 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"effective_ability"), R.Join(new V[] {v_378, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_382 = values_381[0];
V v_383 = values_381[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_382) == (V)2.0d), () => (V)(R.Get(R.Get(v_382, (V)2.0d), (V)"x") == (V)4.0d)), () => (V)(v_383 > R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_378))).First, (V)"damage"))))));
var values_384 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_378, (V)"core:enemy/mossbound_guard", (V)4.0d}, new Results((V)2.0d))));
V v_385 = values_384[0];
var values_386 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_378}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_387 = values_386[0];
V v_388 = values_386[1];
V v_389 = values_386[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_389}, new Results((V)"HitConfirmed"))).First) == (V)1.0d), () => (V)(R.Get(v_385, (V)"hp") < R.Get(v_385, (V)"max_hp"))))));
var values_390 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)42.0d))));
V v_391 = values_390[0];
R.Call(v_16, R.Join(new V[] {v_391, (V)"longblade_geometry"}, new Results((V)"rare")));
var values_392 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_393 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_391))).First, (V)"position");
V key_394 = (V)"y";
R.Set(obj_393, key_394, values_392[0]);
var values_395 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"effective_ability"), R.Join(new V[] {v_391, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_396 = values_395[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_396) == (V)1.0d))));
var values_397 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)43.0d))));
V v_398 = values_397[0];

R.Call(v_16, R.Join(new V[] {v_398, (V)"longblade_geometry"}, new Results((V)"rare")));
R.Call(v_22, R.Join(new V[] {v_398, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d)));
var values_399 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_398}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_400 = values_399[0];
V v_401 = values_399[1];
V v_402 = values_399[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_30, R.Join(new V[] {v_402}, new Results((V)"AttackPerformed"))).First, (V)1.0d), (V)"bump") == (V)true))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_402}, new Results((V)"HitConfirmed"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_4, (V)"effective_ability"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)41.0d))).First, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First) == (V)1.0d))));
var values_403 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)410.0d))));
V v_404 = values_403[0];
var values_405 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_406 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_404))).First, (V)"position");
V key_407 = (V)"x";
R.Set(obj_406, key_407, values_405[0]);

var values_408 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_409 = R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_404))).First, (V)"position");
V key_410 = (V)"y";
R.Set(obj_409, key_410, values_408[0]);
R.Call(v_16, R.Join(new V[] {v_404, (V)"longblade_geometry"}, new Results((V)"common")));
R.Call(v_16, R.Join(new V[] {v_404, (V)"crescent_reach"}, new Results((V)"common")));
var values_411 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"effective_ability"), R.Join(new V[] {v_404, (V)"basic_attack", (V)1.0d}, new Results((-(V)1.0d)))));
V v_412 = values_411[0];
var values_413 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"effective_ability"), R.Join(new V[] {v_404, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_414 = values_413[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_412) == (V)2.0d), () => (V)(R.Get(R.Get(v_412, (V)2.0d), (V)"x") == (V)4.0d)), () => (V)(R.Get(R.Get(v_412, (V)2.0d), (V)"y") == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_414) == (V)4.0d))));
return Results.Empty;
})))));
R.Call(v_14, R.Join(new V[] {(V)"Phantom Lunge hits distinct neighbors of actual full and partial Dash path"}, new Results(new V((RuleFunction)(args_415 => {

var values_416 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)44.0d))));
V v_417 = values_416[0];

R.Call(v_16, R.Join(new V[] {v_417, (V)"phantom_lunge"}, new Results((V)"rare")));
var values_418 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_417, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)3.0d))));
V v_419 = values_418[0];
var values_420 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_417, (V)"core:enemy/mossbound_guard", (V)5.0d}, new Results((V)2.0d))));
V v_421 = values_420[0];
var values_422 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_417}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_423 = values_422[0];
V v_424 = values_422[1];
V v_425 = values_422[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_417))).First, (V)"position"), (V)"x") == (V)4.0d), () => (V)((V)R.Length(R.Call(v_42, R.Join(new V[] {v_425}, new Results((V)"phantom_lunge"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_425}, new Results((V)"DamageTaken"))).First) == (V)2.0d), () => (V)(R.Get(v_419, (V)"hp") < (V)10.0d)), () => (V)(R.Get(v_421, (V)"hp") < (V)10.0d)))));
var values_426 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)45.0d))));
V v_427 = values_426[0];

R.Call(v_16, R.Join(new V[] {v_427, (V)"phantom_lunge"}, new Results((V)"rare")));
R.Call(v_22, R.Join(new V[] {v_427, (V)"core:enemy/mossbound_guard", (V)4.0d}, new Results((V)2.0d)));
var values_428 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_427, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)3.0d))));
V v_429 = values_428[0];
var values_430 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_427, (V)"core:enemy/mossbound_guard", (V)5.0d}, new Results((V)2.0d))));
V v_431 = values_430[0];
var values_432 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_427}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_433 = values_432[0];
V v_434 = values_432[1];
V v_435 = values_432[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_427))).First, (V)"position"), (V)"x") == (V)3.0d), () => (V)((V)R.Length(R.Call(v_30, R.Join(new V[] {v_435}, new Results((V)"DamageTaken"))).First) == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_429, (V)"hp") < (V)10.0d), () => (V)(R.Get(v_431, (V)"hp") == (V)10.0d)))));
var values_436 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)46.0d))));
V v_437 = values_436[0];

R.Call(v_16, R.Join(new V[] {v_437, (V)"phantom_lunge"}, new Results((V)"rare")));
R.Call(v_22, R.Join(new V[] {v_437, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d)));
var values_438 = R.Join(new V[] {}, new Results(R.Get(v_437, (V)"clock")));
V v_439 = values_438[0];
var values_440 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_437}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_441 = values_440[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_441)), () => (V)(R.Get(v_437, (V)"clock") == v_439)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
