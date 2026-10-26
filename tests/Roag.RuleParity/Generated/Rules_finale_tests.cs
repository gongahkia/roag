// Static C# port of tests/finale_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 945e2701303ee99806de08e23cc4bd3e000203fe74298e4bf110de19084a267a
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_finale_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.finale"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_21 = values_20[0];
var values_22 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
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
var values_38 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_36))));
V v_39 = values_38[0];
var values_40 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"map_for"), R.Join(new V[] {}, new Results(v_36))));
V v_41 = values_40[0];
foreach (var row_42 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_23)))))) {
V v_43 = row_42[0];
V v_44 = row_42[1];
var values_45 = R.Join(new V[] {(V)(R.Get(R.Get(v_37, (V)"position"), (V)"x") + R.Get(v_44, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_37, (V)"position"), (V)"y") + R.Get(v_44, (V)2.0d))));
V v_46 = values_45[0];
V v_47 = values_45[1];
var values_48 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_41, v_46}, new Results(v_47))));
V v_49 = values_48[0];
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_36, (V)"entities"))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_52, (V)"id") != R.Get(v_39, (V)"id")), () => R.Get(v_52, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_52, (V)"position"), (V)"x") == v_46)), () => (V)(R.Get(R.Get(v_52, (V)"position"), (V)"y") == v_47)))) {
var values_53 = R.Join(new V[] {}, new Results((V)false));
v_49 = values_53[0];
}
}
if (R.Truth(v_49)) {
var values_54 = R.Join(new V[] {v_46}, new Results(v_47));
var obj_55 = R.Get(v_39, (V)"position");
V key_56 = (V)"x";
var obj_57 = R.Get(v_39, (V)"position");
V key_58 = (V)"y";
R.Set(obj_55, key_56, values_54[0]);
R.Set(obj_57, key_58, values_54[1]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_36)));
return R.Join(new V[] {(-R.Get(v_44, (V)1.0d))}, new Results((-R.Get(v_44, (V)2.0d))));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no approach to final encounter object")));
return Results.Empty;
}));
V v_59 = V.Nil;
v_59 = new V((RuleFunction)(args_60 => {
V v_61 = args_60[0];
V v_62 = args_60[1];
V v_63 = args_60[2];
V v_64 = args_60[3];
var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results(R.Or(v_61, () => (V)5.0d)))));
V v_66 = values_65[0];
{ double end_69 = R.Num((V)3.0d), step_70 = R.Num((V)1.0d);
for (double i_68 = R.Num((V)1.0d); step_70 > 0 ? i_68 <= end_69 : i_68 >= end_69; i_68 += step_70) {
V v_67 = i_68;
foreach (var row_71 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_66, (V)"entities"))))))) {
V v_72 = row_71[0];
V v_73 = row_71[1];
if (R.Truth((V)(R.Get(v_73, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_66}, new Results(R.Get(v_73, (V)"id"))));
}
}
var values_74 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_66}, new Results(R.Get(R.Get(v_66, (V)"stage"), (V)"elite_id")))))));
V v_75 = values_74[0];
var values_76 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_66))));
V v_77 = values_76[0];
var values_78 = R.Join(new V[] {(V)300.0d, (V)300.0d}, new Results((V)100.0d));
var obj_79 = v_77;
V key_80 = (V)"max_hp";
var obj_81 = v_77;
V key_82 = (V)"hp";
var obj_83 = v_77;
V key_84 = (V)"damage";
R.Set(obj_79, key_80, values_78[0]);
R.Set(obj_81, key_82, values_78[1]);
R.Set(obj_83, key_84, values_78[2]);
var values_85 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_66}, new Results(v_75))));
V v_86 = values_85[0];
V v_87 = values_85[1];
var values_88 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_66}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_86), new Entry((V)"dy", v_87))))));
V v_89 = values_88[0];
V v_90 = values_88[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_89}, new Results(v_90)));
if (R.Truth(R.And((V)(v_67 == (V)3.0d), () => v_64))) {
R.Call(v_64, R.Join(new V[] {}, new Results(v_66)));
}
var values_91 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_66}, new Results(R.Get(R.Get(v_66, (V)"stage"), (V)"exit_id")))))));
V v_92 = values_91[0];
R.Call(v_34, R.Join(new V[] {v_66}, new Results(v_92)));
var values_93 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_67 == (V)1.0d), () => R.Or(v_62, () => (V)1.0d)), () => R.And((V)(v_67 == (V)2.0d), () => R.Or(v_63, () => (V)2.0d))), () => (V)1.0d)));
V v_94 = values_93[0];
var values_95 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_66}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_92, (V)"id")), new Entry((V)"route_index", v_94))))));
v_89 = values_95[0];
v_90 = values_95[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_89}, new Results(v_90)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_66, (V)"finale"), () => (V)((V)R.Length(R.Get(v_66, (V)"retired_stages")) == (V)3.0d)))));
return R.Join(new V[] {}, new Results(v_66));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"final arena is reachable, distinct and stable across 100 run seeds"}, new Results(new V((RuleFunction)(args_96 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"validate_map"), R.Join(new V[] {}, new Results(R.Get(v_9, (V)"map"))))));
var values_97 = R.Join(new V[] {}, new Results(R.Get(v_9, (V)"map")));
V v_98 = values_97[0];
var values_99 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)((V)(R.Get(v_98, (V)"start_y") * R.Get(v_98, (V)"width")) + R.Get(v_98, (V)"start_x")), (V)true))));
V v_100 = values_99[0];
var values_101 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, R.Get(v_98, (V)"start_x")), new Entry((V)2, R.Get(v_98, (V)"start_y")))))));
V v_102 = values_101[0];
var values_103 = R.Join(new V[] {}, new Results((V)1.0d));
V v_104 = values_103[0];
while (R.Truth((V)(v_104 <= (V)R.Length(v_102)))) {
var values_105 = R.Join(new V[] {}, new Results(R.Get(v_102, v_104)));
V v_106 = values_105[0];

var values_107 = R.Join(new V[] {}, new Results((V)(v_104 + (V)1.0d)));
v_104 = values_107[0];
foreach (var row_108 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_23)))))) {
V v_109 = row_108[0];
V v_110 = row_108[1];
var values_111 = R.Join(new V[] {(V)(R.Get(v_106, (V)1.0d) + R.Get(v_110, (V)1.0d))}, new Results((V)(R.Get(v_106, (V)2.0d) + R.Get(v_110, (V)2.0d))));
V v_112 = values_111[0];
V v_113 = values_111[1];
var values_114 = R.Join(new V[] {}, new Results((V)((V)(v_113 * R.Get(v_98, (V)"width")) + v_112)));
V v_115 = values_114[0];
if (R.Truth(R.And(R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_98, v_112}, new Results(v_113))).First, () => (V)(!R.Truth(R.Get(v_100, v_115)))))) {
var values_116 = R.Join(new V[] {}, new Results((V)true));
var obj_117 = v_100;
V key_118 = v_115;
R.Set(obj_117, key_118, values_116[0]);

var values_119 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_112), new Entry((V)2, v_113))));
var obj_120 = v_102;
V key_121 = (V)((V)R.Length(v_102) + (V)1.0d);
R.Set(obj_120, key_121, values_119[0]);
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_100, (V)((V)(R.Get(v_98, (V)"boss_y") * R.Get(v_98, (V)"width")) + R.Get(v_98, (V)"boss_x"))), () => (V)((V)R.Length(v_102) > (V)200.0d)))));
{ double end_124 = R.Num((V)100.0d), step_125 = R.Num((V)1.0d);
for (double i_123 = R.Num((V)1.0d); step_125 > 0 ? i_123 <= end_124 : i_123 >= end_124; i_123 += step_125) {
V v_122 = i_123;
var values_126 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"derive"), R.Join(new V[] {v_122}, new Results((V)"finale:core:route/gateway"))));
V v_127 = values_126[0];
var values_128 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_finale"), R.Join(new V[] {v_127}, new Results(R.Table(new Entry((V)"root_seed", v_122), new Entry((V)"route_history", R.Table(new Entry((V)1, (V)"core:route/fungal_hollows"), new Entry((V)2, (V)"core:route/ashen_bastion"), new Entry((V)3, (V)"core:route/gateway"))))))));
V v_129 = values_128[0];
var values_130 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_129}, new Results(R.Get(R.Get(v_129, (V)"finale"), (V)"boss_id")))))));
V v_131 = values_130[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_129, (V)"run_seed") == v_127), () => (V)(R.Get(v_129, (V)"map_id") == R.Get(v_98, (V)"id"))), () => (V)(R.Get(R.Get(v_131, (V)"position"), (V)"x") == R.Get(v_98, (V)"boss_x"))), () => (V)(R.Get(R.Get(v_131, (V)"position"), (V)"y") == R.Get(v_98, (V)"boss_y"))), () => (V)(R.Get(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_129))).First, (V)"position"), (V)"x") == R.Get(v_98, (V)"start_x"))), () => (V)(R.Get(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_129))).First, (V)"position"), (V)"y") == R.Get(v_98, (V)"start_y"))), () => (V)(!R.Truth(R.Get(v_129, (V)"stage")))), () => (V)(!R.Truth(R.Get(v_129, (V)"generated_map")))))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"third exit enters final arena once without winning or leaking schedules"}, new Results(new V((RuleFunction)(args_132 => {

{ double end_135 = R.Num((V)2.0d), step_136 = R.Num((V)1.0d);
for (double i_134 = R.Num((V)1.0d); step_136 > 0 ? i_134 <= end_135 : i_134 >= end_135; i_134 += step_136) {
V v_133 = i_134;
{ double end_139 = R.Num((V)2.0d), step_140 = R.Num((V)1.0d);
for (double i_138 = R.Num((V)1.0d); step_140 > 0 ? i_138 <= end_139 : i_138 >= end_139; i_138 += step_140) {
V v_137 = i_138;
var values_141 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {(V)((V)((V)30.0d + (V)(v_133 * (V)2.0d)) + v_137), v_133}, new Results(v_137))));
V v_142 = values_141[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_142, (V)"finale"), (V)"phase") == (V)"active"), () => (V)(!R.Truth(R.Get(v_142, (V)"game_over")))), () => (V)(!R.Truth(R.Get(v_142, (V)"victory")))), () => (V)((V)R.Length(R.Get(v_142, (V)"retired_stages")) == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_142, (V)"finale"), (V)"route_history"), (V)3.0d) == (V)"core:route/gateway"))));
var values_143 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_142}, new Results(R.Get(R.Get(v_142, (V)"finale"), (V)"boss_id")))));
V v_144 = values_143[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_144, (V)"definition_id") == (V)"core:enemy/hollow_regent"))));
foreach (var row_145 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_142, (V)"schedule"))))))) {
V v_146 = row_145[0];
V v_147 = row_145[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_147, (V)"kind") != (V)"stage_pressure"), () => (V)(R.Get(v_147, (V)"kind") != (V)"stage_deadline")), () => (V)(R.Get(v_147, (V)"actor_id") != R.Get(R.Get(R.Get(R.Get(v_142, (V)"retired_stages"), (V)3.0d), (V)"stage"), (V)"pursuer_id"))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", (V)1.0d), new Entry((V)"route_index", (V)1.0d))))).First)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_142))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_142)))))))));
}
}
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"third-floor delayed Echo stays frozen after final handoff"}, new Results(new V((RuleFunction)(args_148 => {

var values_149 = Results.Empty;
V v_150 = values_149[0];
var values_161 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {(V)39.0d, (V)1.0d, (V)2.0d}, new Results(new V((RuleFunction)(args_151 => {
V v_152 = args_151[0];
var values_153 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_152))));
V v_154 = values_153[0];
var values_155 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_156 = v_154;
V key_157 = (V)"damage";
R.Set(obj_156, key_157, values_155[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_152, R.Get(v_154, (V)"id"), (V)"core:boon/temporal_echo", (V)"legendary"}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_152}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_158 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_152))));
V v_159 = values_158[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_159) > (V)0.0d))));
var values_160 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_159, (V)1.0d), (V)"id")));
v_150 = values_160[0];
return Results.Empty;
}))))));
V v_162 = values_161[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(R.Get(R.Get(v_162, (V)"retired_stages"), (V)3.0d), (V)"delayed_effects"), v_150), () => (V)(!R.Truth(R.Get(R.Get(v_162, (V)"delayed_effects"), v_150)))))));
foreach (var row_163 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_162, (V)"schedule"))))))) {
V v_164 = row_163[0];
V v_165 = row_163[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_165, (V)"id") != v_150))));
}
var values_166 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_162}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_167 = values_166[0];
V v_168 = values_166[1];
V v_169 = values_166[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_24, R.Join(new V[] {v_169}, new Results((V)"DelayedEffectResolved"))).First == (V)0.0d))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_162))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_162)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Regent commits three different locked shapes and phase changes future speed"}, new Results(new V((RuleFunction)(args_170 => {

var values_171 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)41.0d))));
V v_172 = values_171[0];
var values_173 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_172}, new Results(R.Get(R.Get(v_172, (V)"finale"), (V)"boss_id")))));
V v_174 = values_173[0];
var values_175 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_172))));
V v_176 = values_175[0];
var values_177 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_178 = v_176;
V key_179 = (V)"damage";
R.Set(obj_178, key_179, values_177[0]);
var values_180 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_172}, new Results(v_174))));
V v_181 = values_180[0];
V v_182 = values_180[1];
var values_183 = R.Join(new V[] {}, new Results(R.Table()));
V v_184 = values_183[0];
{ double end_187 = R.Num((V)2.0d), step_188 = R.Num((V)1.0d);
for (double i_186 = R.Num((V)0.0d); step_188 > 0 ? i_186 <= end_187 : i_186 >= end_187; i_186 += step_188) {
V v_185 = i_186;
var values_189 = R.Join(new V[] {}, new Results(v_185));
var obj_190 = R.Get(v_174, (V)"ai_memory");
V key_191 = (V)"attacks";
R.Set(obj_190, key_191, values_189[0]);
var values_192 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"choose"), R.Join(new V[] {v_172}, new Results(v_174))));
V v_193 = values_192[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_193, (V)"kind") == (V)"windup"), () => (V)((V)R.Length(R.Get(v_193, (V)"cells")) > (V)0.0d)))));
var values_194 = R.Join(new V[] {}, new Results(R.Get(v_193, (V)"ability_id")));
var obj_195 = v_184;
V key_196 = (V)((V)R.Length(v_184) + (V)1.0d);
R.Set(obj_195, key_196, values_194[0]);
if (R.Truth((V)(v_185 == (V)0.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_193, (V)"ranged") == (V)"piercing_line"))));
}
if (R.Truth((V)(v_185 > (V)0.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_193, (V)"ranged") == (V)"area"))));
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_184, (V)1.0d) == (V)"core:ability/regent_line"), () => (V)(R.Get(v_184, (V)2.0d) == (V)"core:ability/regent_crown")), () => (V)(R.Get(v_184, (V)3.0d) == (V)"core:ability/regent_sigil")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_174, (V)"speed") == R.Get(R.Call(R.Get(v_7, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_174, (V)"definition_id")))).First, (V)"speed")))));
var values_197 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_174, (V)"max_hp") / (V)2.0d)))).First + (V)1.0d)));
var obj_198 = v_174;
V key_199 = (V)"hp";
R.Set(obj_198, key_199, values_197[0]);
var values_200 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_172}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_181), new Entry((V)"dy", v_182))))));
V v_201 = values_200[0];
V v_202 = values_200[1];
V v_203 = values_200[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_174, (V)"phase_two"), () => (V)(R.Get(v_174, (V)"speed") == (V)100.0d)), () => (V)(R.Call(v_24, R.Join(new V[] {v_203}, new Results((V)"RegentPhaseChanged"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Regent pending attack is locked, restores and ordinary damage does not cancel"}, new Results(new V((RuleFunction)(args_204 => {

var values_205 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)42.0d))));
V v_206 = values_205[0];
var values_207 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_206}, new Results(R.Get(R.Get(v_206, (V)"finale"), (V)"boss_id")))));
V v_208 = values_207[0];
var values_209 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_206))));
V v_210 = values_209[0];
var values_211 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_212 = v_210;
V key_213 = (V)"damage";
R.Set(obj_212, key_213, values_211[0]);
var values_214 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_206}, new Results(v_208))));
V v_215 = values_214[0];
V v_216 = values_214[1];
var values_217 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_206}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_218 = values_217[0];
V v_219 = values_217[1];
V v_220 = values_217[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_24, R.Join(new V[] {v_220}, new Results((V)"WindupStarted"))).First >= (V)1.0d), () => R.Get(v_208, (V)"pending_attack")))));
var values_221 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_208, (V)"pending_attack"), (V)"target_cells")))));
V v_222 = values_221[0];
var values_223 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_206))))));
V v_224 = values_223[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_206))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_224)))));
var values_225 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_206}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_215), new Entry((V)"dy", v_216))))));
V v_226 = values_225[0];
V v_227 = values_225[1];
V v_228 = values_225[2];
var values_229 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_224}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_215), new Entry((V)"dy", v_216))))));
V v_230 = values_229[0];
V v_231 = values_229[1];
V v_232 = values_229[2];
R.Call(v_3, R.Join(new V[] {v_228}, new Results(v_232)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_206))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_224)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_24, R.Join(new V[] {v_228}, new Results((V)"WindupCancelled"))).First == (V)0.0d))));
if (R.Truth(R.Get(v_208, (V)"pending_attack"))) {
R.Call(v_3, R.Join(new V[] {v_222}, new Results(R.Get(R.Get(v_208, (V)"pending_attack"), (V)"target_cells"))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"locked Regent cells resolve before the next decision after a dodge"}, new Results(new V((RuleFunction)(args_233 => {

var values_234 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)43.0d))));
V v_235 = values_234[0];
var values_236 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_235}, new Results(R.Get(R.Get(v_235, (V)"finale"), (V)"boss_id")))));
V v_237 = values_236[0];
R.Call(v_34, R.Join(new V[] {v_235}, new Results(v_237)));
var values_238 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_235))));
V v_239 = values_238[0];
var values_240 = R.Join(new V[] {R.Get(R.Get(v_239, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_239, (V)"position"), (V)"y")));
V v_241 = values_240[0];
V v_242 = values_240[1];
var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_235}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_244 = values_243[0];
V v_245 = values_243[1];
V v_246 = values_243[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_24, R.Join(new V[] {v_246}, new Results((V)"WindupStarted"))).First == (V)1.0d), () => R.Get(v_237, (V)"pending_attack")))));
var values_247 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_237, (V)"pending_attack"), (V)"target_cells")))));
V v_248 = values_247[0];
var values_249 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_237, (V)"pending_attack"), (V)"due")));
V v_250 = values_249[0];
var values_251 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_241 == R.Get(R.Get(v_237, (V)"position"), (V)"x")), () => (V)1.0d), () => (V)0.0d)));
V v_252 = values_251[0];
var values_253 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_252 == (V)1.0d), () => R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), () => R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d)))));
V v_254 = values_253[0];
var values_255 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_235}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_254, (V)1.0d)), new Entry((V)"dy", R.Get(v_254, (V)2.0d)))))));
V v_256 = values_255[0];
V v_257 = values_255[1];
V v_258 = values_255[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_256}, new Results(v_257)));
var values_259 = Results.Empty;
V v_260 = values_259[0];
foreach (var row_261 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_258)))))) {
V v_262 = row_261[0];
V v_263 = row_261[1];
if (R.Truth(R.And((V)(R.Get(v_263, (V)"kind") == (V)"AttackPerformed"), () => (V)(R.Get(v_263, (V)"source_id") == R.Get(v_237, (V)"id"))))) {
var values_264 = R.Join(new V[] {}, new Results(v_263));
v_260 = values_264[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_260, () => (V)(R.Get(v_260, (V)"time") == v_250)), () => (V)(v_250 < R.Get(v_235, (V)"clock"))))));
R.Call(v_3, R.Join(new V[] {v_248}, new Results(R.Get(v_260, (V)"target_cells"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(R.Get(v_239, (V)"position"), (V)"x") != v_241), () => (V)(R.Get(R.Get(v_239, (V)"position"), (V)"y") != v_242)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.And((V)(R.Get(R.Get(v_239, (V)"position"), (V)"x") == R.Get(R.Get(v_248, (V)1.0d), (V)"x")), () => (V)(R.Get(R.Get(v_239, (V)"position"), (V)"y") == R.Get(R.Get(v_248, (V)1.0d), (V)"y"))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Regent stun, displacement and death cancel committed windups"}, new Results(new V((RuleFunction)(args_265 => {

foreach (var row_266 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"stun"), new Entry((V)2, (V)"displace"), new Entry((V)3, (V)"death")))))))) {
V v_267 = row_266[0];
V v_268 = row_266[1];
var values_269 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)50.0d))));
V v_270 = values_269[0];
var values_271 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_270}, new Results(R.Get(R.Get(v_270, (V)"finale"), (V)"boss_id")))));
V v_272 = values_271[0];
R.Call(v_34, R.Join(new V[] {v_270}, new Results(v_272)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_270}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_272, (V)"pending_attack"))));
var values_273 = Results.Empty;
V v_274 = values_273[0];
if (R.Truth((V)(v_268 == (V)"stun"))) {
var values_275 = Results.Empty;
V v_276 = values_275[0];
V v_277 = values_275[1];

var values_278 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_270, R.Get(v_272, (V)"id"), (V)150.0d, R.Get(v_270, (V)"player_id")}, new Results((V)999.0d))));
v_276 = values_278[0];
v_277 = values_278[1];
v_274 = values_278[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_276}, new Results(v_277)));
} else if (R.Truth((V)(v_268 == (V)"displace"))) {
var values_279 = Results.Empty;
V v_280 = values_279[0];
V v_281 = values_279[1];

var values_282 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"displace"), R.Join(new V[] {v_270, R.Get(v_272, (V)"id"), (V)(R.Get(R.Get(v_272, (V)"position"), (V)"x") + (V)1.0d), R.Get(R.Get(v_272, (V)"position"), (V)"y"), R.Get(v_270, (V)"player_id")}, new Results((V)999.0d))));
v_280 = values_282[0];
v_281 = values_282[1];
v_274 = values_282[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_280}, new Results(v_281)));
} else {
var values_283 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_270))));
V v_284 = values_283[0];
var values_285 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_286 = v_284;
V key_287 = (V)"damage";
R.Set(obj_286, key_287, values_285[0]);
var values_288 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_272, (V)"position"), (V)"x") - R.Get(R.Get(v_284, (V)"position"), (V)"x"))));
V v_289 = values_288[0];
var values_290 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_272, (V)"position"), (V)"y") - R.Get(R.Get(v_284, (V)"position"), (V)"y"))));
V v_291 = values_290[0];
var values_292 = Results.Empty;
V v_293 = values_292[0];
V v_294 = values_292[1];

var values_295 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_270}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_289), new Entry((V)"dy", v_291))))));
v_293 = values_295[0];
v_294 = values_295[1];
v_274 = values_295[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_293}, new Results(v_294)));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_24, R.Join(new V[] {v_274}, new Results((V)"WindupCancelled"))).First == (V)1.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Burning boss death uses canonical pipeline and exactly one Victory"}, new Results(new V((RuleFunction)(args_296 => {

var values_297 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)55.0d))));
V v_298 = values_297[0];
var values_299 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_298}, new Results(R.Get(R.Get(v_298, (V)"finale"), (V)"boss_id")))));
V v_300 = values_299[0];
var values_301 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_298))));
V v_302 = values_301[0];
var values_303 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_298}, new Results(v_300))));
V v_304 = values_303[0];
V v_305 = values_303[1];
var values_306 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_307 = v_302;
V key_308 = (V)"damage";
R.Set(obj_307, key_308, values_306[0]);

var values_309 = R.Join(new V[] {}, new Results((V)6.0d));
var obj_310 = v_300;
V key_311 = (V)"hp";
R.Set(obj_310, key_311, values_309[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_298, R.Get(v_302, (V)"id"), (V)"core:boon/emberbrand", (V)"legendary"}, new Results((V)1.0d)));
var values_312 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_298}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_304), new Entry((V)"dy", v_305))))));
V v_313 = values_312[0];
V v_314 = values_312[1];
V v_315 = values_312[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And((V)(R.Call(v_24, R.Join(new V[] {v_315}, new Results((V)"StatusApplied"))).First == (V)1.0d), () => (V)(R.Call(v_24, R.Join(new V[] {v_315}, new Results((V)"StatusTick"))).First == (V)1.0d)), () => (V)(R.Call(v_24, R.Join(new V[] {v_315}, new Results((V)"EntityKilled"))).First == (V)1.0d)), () => (V)(R.Call(v_24, R.Join(new V[] {v_315}, new Results((V)"Victory"))).First == (V)1.0d)), () => R.Get(v_298, (V)"victory")), () => (V)(R.Get(R.Get(v_298, (V)"finale"), (V)"phase") == (V)"victory")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_298}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))).First)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_298))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_298)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"delayed Echo can deliver the final blow without duplicate Victory"}, new Results(new V((RuleFunction)(args_316 => {

var values_317 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)56.0d))));
V v_318 = values_317[0];
var values_319 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_318}, new Results(R.Get(R.Get(v_318, (V)"finale"), (V)"boss_id")))));
V v_320 = values_319[0];
var values_321 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_318))));
V v_322 = values_321[0];
var values_323 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_318}, new Results(v_320))));
V v_324 = values_323[0];
V v_325 = values_323[1];
var values_326 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_327 = v_320;
V key_328 = (V)"hp";
R.Set(obj_327, key_328, values_326[0]);

var values_329 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_330 = v_322;
V key_331 = (V)"damage";
R.Set(obj_330, key_331, values_329[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_318, R.Get(v_322, (V)"id"), (V)"core:boon/temporal_echo", (V)"legendary"}, new Results((V)1.0d)));
var values_332 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_318}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_324), new Entry((V)"dy", v_325))))));
V v_333 = values_332[0];
V v_334 = values_332[1];
V v_335 = values_332[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(v_318, (V)"game_over"))), () => (V)((V)R.Length(R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_318))).First) > (V)0.0d)), () => (V)(R.Call(v_24, R.Join(new V[] {v_335}, new Results((V)"Victory"))).First == (V)0.0d)))));
var values_336 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_318}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_337 = values_336[0];
V v_338 = values_336[1];
V v_339 = values_336[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(v_318, (V)"victory"), () => (V)(R.Call(v_24, R.Join(new V[] {v_339}, new Results((V)"DelayedEffectResolved"))).First == (V)1.0d)), () => (V)(R.Call(v_24, R.Join(new V[] {v_339}, new Results((V)"RegentDefeated"))).First == (V)1.0d)), () => (V)(R.Call(v_24, R.Join(new V[] {v_339}, new Results((V)"Victory"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"friendly fire kills Regent through shared area damage pipeline"}, new Results(new V((RuleFunction)(args_340 => {

var values_341 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)57.0d))));
V v_342 = values_341[0];
var values_343 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_342}, new Results(R.Get(R.Get(v_342, (V)"finale"), (V)"boss_id")))));
V v_344 = values_343[0];
var values_345 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_342))));
V v_346 = values_345[0];
var values_347 = R.Join(new V[] {R.Get(R.Get(v_344, (V)"position"), (V)"x")}, new Results((V)(R.Get(R.Get(v_344, (V)"position"), (V)"y") + (V)1.0d)));
var obj_348 = R.Get(v_346, (V)"position");
V key_349 = (V)"x";
var obj_350 = R.Get(v_346, (V)"position");
V key_351 = (V)"y";
R.Set(obj_348, key_349, values_347[0]);
R.Set(obj_350, key_351, values_347[1]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_342)));
var values_352 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_353 = v_344;
V key_354 = (V)"hp";
R.Set(obj_353, key_354, values_352[0]);
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_342}, new Results(R.Get(v_344, (V)"id"))));
var values_355 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_342, (V)"core:enemy/cinder_bellkeeper", R.Get(R.Get(v_344, (V)"position"), (V)"x"), (V)(R.Get(R.Get(v_344, (V)"position"), (V)"y") + (V)3.0d)}, new Results((V)0.0d))));
V v_356 = values_355[0];
var values_357 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_342}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_358 = values_357[0];
V v_359 = values_357[1];
V v_360 = values_357[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_24, R.Join(new V[] {v_360}, new Results((V)"WindupStarted"))).First == (V)1.0d), () => R.Get(v_356, (V)"pending_attack")))));
var values_361 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_342}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_362 = values_361[0];
V v_363 = values_361[1];
V v_364 = values_361[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_342, (V)"victory"), () => (V)(R.Call(v_24, R.Join(new V[] {v_364}, new Results((V)"EntityKilled"))).First == (V)1.0d)), () => (V)(R.Call(v_24, R.Join(new V[] {v_364}, new Results((V)"Victory"))).First == (V)1.0d)))));
foreach (var row_365 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_364)))))) {
V v_366 = row_365[0];
V v_367 = row_365[1];
if (R.Truth((V)(R.Get(v_367, (V)"kind") == (V)"EntityKilled"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_367, (V)"target_id") == R.Get(v_344, (V)"id")), () => (V)(R.Get(v_367, (V)"source_id") == R.Get(v_356, (V)"id"))))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"same-impact player and Regent lethal results in Defeat by HP outcome"}, new Results(new V((RuleFunction)(args_368 => {

var values_369 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)59.0d))));
V v_370 = values_369[0];
var values_371 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_370}, new Results(R.Get(R.Get(v_370, (V)"finale"), (V)"boss_id")))));
V v_372 = values_371[0];
var values_373 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_370))));
V v_374 = values_373[0];
var values_375 = R.Join(new V[] {R.Get(R.Get(v_372, (V)"position"), (V)"x")}, new Results((V)(R.Get(R.Get(v_372, (V)"position"), (V)"y") + (V)1.0d)));
var obj_376 = R.Get(v_374, (V)"position");
V key_377 = (V)"x";
var obj_378 = R.Get(v_374, (V)"position");
V key_379 = (V)"y";
R.Set(obj_376, key_377, values_375[0]);
R.Set(obj_378, key_379, values_375[1]);
var values_380 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_381 = v_374;
V key_382 = (V)"hp";
R.Set(obj_381, key_382, values_380[0]);

var values_383 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_384 = v_372;
V key_385 = (V)"hp";
R.Set(obj_384, key_385, values_383[0]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_370)));
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_370}, new Results(R.Get(v_372, (V)"id"))));
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_370, (V)"core:enemy/cinder_bellkeeper", R.Get(R.Get(v_372, (V)"position"), (V)"x"), (V)(R.Get(R.Get(v_372, (V)"position"), (V)"y") + (V)3.0d)}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_370}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_386 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_370}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_387 = values_386[0];
V v_388 = values_386[1];
V v_389 = values_386[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.Get(v_370, (V)"game_over"), () => (V)(!R.Truth(R.Get(v_370, (V)"victory")))), () => (V)(R.Get(R.Get(v_370, (V)"finale"), (V)"phase") == (V)"boss_defeated")), () => (V)(R.Call(v_24, R.Join(new V[] {v_389}, new Results((V)"EntityKilled"))).First == (V)2.0d)), () => (V)(R.Call(v_24, R.Join(new V[] {v_389}, new Results((V)"Victory"))).First == (V)0.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_370))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_370)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"credited kill chains Storm and Detonation into Regent damage"}, new Results(new V((RuleFunction)(args_390 => {

var values_391 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)63.0d))));
V v_392 = values_391[0];
var values_393 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_392}, new Results(R.Get(R.Get(v_392, (V)"finale"), (V)"boss_id")))));
V v_394 = values_393[0];
var values_395 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_392))));
V v_396 = values_395[0];
var values_397 = R.Join(new V[] {R.Get(R.Get(v_394, (V)"position"), (V)"x")}, new Results((V)(R.Get(R.Get(v_394, (V)"position"), (V)"y") - (V)1.0d)));
var obj_398 = R.Get(v_396, (V)"position");
V key_399 = (V)"x";
var obj_400 = R.Get(v_396, (V)"position");
V key_401 = (V)"y";
R.Set(obj_398, key_399, values_397[0]);
R.Set(obj_400, key_401, values_397[1]);
var values_402 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_403 = v_396;
V key_404 = (V)"damage";
R.Set(obj_403, key_404, values_402[0]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_392)));
var values_405 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_392, (V)"core:enemy/mossbound_guard", (V)(R.Get(R.Get(v_394, (V)"position"), (V)"x") + (V)1.0d), R.Get(R.Get(v_394, (V)"position"), (V)"y")}, new Results((V)0.0d))));
V v_406 = values_405[0];
var values_407 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_408 = v_406;
V key_409 = (V)"hp";
R.Set(obj_408, key_409, values_407[0]);
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_392}, new Results(R.Get(v_406, (V)"id"))));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_392, R.Get(v_396, (V)"id"), (V)"core:boon/storm_conductor", (V)"legendary"}, new Results((V)1.0d)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_392, R.Get(v_396, (V)"id"), (V)"core:boon/detonation_bloom", (V)"legendary"}, new Results((V)1.0d)));
var values_410 = R.Join(new V[] {}, new Results(R.Get(v_394, (V)"hp")));
V v_411 = values_410[0];
var values_412 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_392}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))));
V v_413 = values_412[0];
V v_414 = values_412[1];
V v_415 = values_412[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_413}, new Results(v_414)));
var values_416 = R.Join(new V[] {}, new Results(R.Table()));
V v_417 = values_416[0];
foreach (var row_418 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_415)))))) {
V v_419 = row_418[0];
V v_420 = row_418[1];
if (R.Truth((V)(R.Get(v_420, (V)"kind") == (V)"BoonActivated"))) {
var values_421 = R.Join(new V[] {}, new Results((V)true));
var obj_422 = v_417;
V key_423 = R.Get(v_420, (V)"boon_id");
R.Set(obj_422, key_423, values_421[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(v_417, (V)"core:boon/storm_conductor"), () => R.Get(v_417, (V)"core:boon/detonation_bloom")), () => (V)(R.Get(v_394, (V)"hp") < v_411)), () => (V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_392}, new Results(R.Get(v_406, (V)"id")))).First))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"finale disk-equivalent save restores pending attack and delayed boon effect"}, new Results(new V((RuleFunction)(args_424 => {

var values_425 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)58.0d))));
V v_426 = values_425[0];
var values_427 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_426}, new Results(R.Get(R.Get(v_426, (V)"finale"), (V)"boss_id")))));
V v_428 = values_427[0];
var values_429 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_426))));
V v_430 = values_429[0];
var values_431 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_432 = v_430;
V key_433 = (V)"damage";
R.Set(obj_432, key_433, values_431[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_426, R.Get(v_430, (V)"id"), (V)"core:boon/temporal_echo", (V)"legendary"}, new Results((V)1.0d)));
var values_434 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_426}, new Results(v_428))));
V v_435 = values_434[0];
V v_436 = values_434[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_426}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_435), new Entry((V)"dy", v_436)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_428, (V)"pending_attack"), () => (V)((V)R.Length(R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_426))).First) > (V)0.0d)))));
var values_437 = R.Join(new V[] {}, R.Call(R.Get(v_19, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_426))))));
V v_438 = values_437[0];
var values_439 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_19, (V)"decode"), R.Join(new V[] {}, new Results(v_438))))));
V v_440 = values_439[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_426))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_440)))));
var values_441 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_426}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_442 = values_441[0];
V v_443 = values_441[1];
V v_444 = values_441[2];
var values_445 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_440}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_446 = values_445[0];
V v_447 = values_445[1];
V v_448 = values_445[2];
R.Call(v_3, R.Join(new V[] {v_444}, new Results(v_448)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_426))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_440)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"saved concrete chamber rejects a blocked Regent cell"}, new Results(new V((RuleFunction)(args_449 => {

var values_450 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)60.0d))));
V v_451 = values_450[0];
var values_452 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_451))));
V v_453 = values_452[0];
var values_454 = R.Join(new V[] {}, new Results(R.Get(v_453, (V)"final_map")));
V v_455 = values_454[0];
var values_456 = R.Join(new V[] {R.Get(v_455, (V)"boss_y")}, new Results(R.Get(v_455, (V)"boss_x")));
V v_457 = values_456[0];
V v_458 = values_456[1];
var values_459 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_455, (V)"rows"), v_457), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_458 - (V)1.0d)))).First, R.Concat((V)"#", R.Invoke(R.Get(R.Get(v_455, (V)"rows"), v_457), "sub", R.Join(new V[] {}, new Results((V)(v_458 + (V)1.0d)))).First))));
var obj_460 = R.Get(v_455, (V)"rows");
V key_461 = v_457;
R.Set(obj_460, key_461, values_459[0]);
var values_462 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_453))));
V v_463 = values_462[0];
V v_464 = values_462[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_463)), () => R.Invoke(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_464))).First, "find", R.Join(new V[] {(V)"final chamber", (V)1.0d}, new Results((V)true))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"terminal Victory and Defeat tombstone both save generations"}, new Results(new V((RuleFunction)(args_465 => {

V v_466 = V.Nil;
v_466 = new V((RuleFunction)(args_467 => {

var values_468 = R.Join(new V[] {}, new Results(R.Table()));
V v_469 = values_468[0];
R.Set(v_469, "read", new V((RuleFunction)(args_470 => {
V v_471 = args_470[0];
V v_472 = args_470[1];
return R.Join(new V[] {}, new Results(R.Get(v_471, v_472)));
return Results.Empty;
})));
R.Set(v_469, "write", new V((RuleFunction)(args_473 => {
V v_474 = args_473[0];
V v_475 = args_473[1];
V v_476 = args_473[2];
var values_477 = R.Join(new V[] {}, new Results(v_476));
var obj_478 = v_474;
V key_479 = v_475;
R.Set(obj_478, key_479, values_477[0]);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_469));
return Results.Empty;
}));
foreach (var row_480 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"victory"), new Entry((V)2, (V)"defeat")))))))) {
V v_481 = row_480[0];
V v_482 = row_480[1];
var values_483 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {}, new Results((V)62.0d))));
V v_484 = values_483[0];
var values_485 = R.Join(new V[] {}, R.Call(v_466, Results.Empty));
V v_486 = values_485[0];
var values_487 = R.Join(new V[] {}, R.Call(R.Get(v_19, (V)"new"), R.Join(new V[] {}, new Results(v_486))));
V v_488 = values_487[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_488, "save", R.Join(new V[] {}, new Results(v_484)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_488, "save", R.Join(new V[] {}, new Results(v_484)))));
var values_489 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_484}, new Results(R.Get(R.Get(v_484, (V)"finale"), (V)"boss_id")))));
V v_490 = values_489[0];
var values_491 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_484))));
V v_492 = values_491[0];
var values_493 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_484}, new Results(v_490))));
V v_494 = values_493[0];
V v_495 = values_493[1];
if (R.Truth((V)(v_482 == (V)"victory"))) {
var values_496 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_497 = v_492;
V key_498 = (V)"damage";
R.Set(obj_497, key_498, values_496[0]);
} else {
var values_499 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_500 = v_492;
V key_501 = (V)"hp";
R.Set(obj_500, key_501, values_499[0]);

var values_502 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_503 = v_490;
V key_504 = (V)"damage";
R.Set(obj_503, key_504, values_502[0]);

R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_484}, new Results(R.Get(v_490, (V)"id"))));
R.Call(R.Get(v_15, (V)"enqueue"), R.Join(new V[] {v_484}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", R.Get(v_484, (V)"clock")), new Entry((V)"actor_id", R.Get(v_490, (V)"id")), new Entry((V)"is_player", (V)false)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_484}, new Results(R.Or(R.And((V)(v_482 == (V)"victory"), () => R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_494), new Entry((V)"dy", v_495))), () => R.Table(new Entry((V)"kind", (V)"wait"))))))));
if (R.Truth(R.And((V)(v_482 == (V)"defeat"), () => (V)(!R.Truth(R.Get(v_484, (V)"game_over")))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_484}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_484, (V)"game_over"), () => (V)((V)(R.Get(v_484, (V)"victory") != V.Nil) == (V)(v_482 == (V)"victory"))))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_484))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_484)))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_488, "save", R.Join(new V[] {}, new Results(v_484)))));
var values_505 = R.Join(new V[] {}, R.Call(R.Get(v_19, (V)"new"), R.Join(new V[] {}, new Results(v_486))));
V v_506 = values_505[0];
var values_507 = R.Join(new V[] {}, R.Invoke(v_506, "load", Results.Empty));
V v_508 = values_507[0];
V v_509 = values_507[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_508)), () => (V)(R.Get(v_509, (V)"status") == (V)"terminal")))));
var values_510 = R.Join(new V[] {}, R.Invoke(R.Get(v_486, R.Get(R.Get(v_19, (V)"slots"), (V)2.0d)), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)12.0d))));
var obj_511 = v_486;
V key_512 = R.Get(R.Get(v_19, (V)"slots"), (V)2.0d);
R.Set(obj_511, key_512, values_510[0]);
var values_513 = R.Join(new V[] {}, R.Invoke(v_506, "load", Results.Empty));
v_508 = values_513[0];
v_509 = values_513[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_508)), () => (V)(R.Get(v_509, (V)"status") == (V)"terminal")))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"seed 3 ordinary actions clear three stages and Hollow Regent"}, new Results(new V((RuleFunction)(args_514 => {

var values_515 = R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"complete_run"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_516 = values_515[0];
var values_517 = R.Join(new V[] {}, new Results(R.Get(v_516, (V)"state")));
V v_518 = values_517[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And(R.Get(v_518, (V)"victory"), () => (V)(R.Get(R.Get(v_518, (V)"victory"), (V)"seed") == (V)3.0d)), () => (V)((V)R.Length(R.Get(v_518, (V)"retired_stages")) == (V)3.0d)), () => (V)((V)R.Length(R.Get(v_516, (V)"trace")) >= (V)900.0d)), () => R.Call(R.Get(v_21, (V)"has_event"), R.Join(new V[] {v_516}, new Results((V)"RegentPhaseChanged"))).First), () => R.Call(R.Get(v_21, (V)"has_event"), R.Join(new V[] {v_516}, new Results((V)"Victory"))).First))));
var values_519 = R.Join(new V[] {}, new Results(R.Table()));
V v_520 = values_519[0];
foreach (var row_521 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_516, (V)"events"))))))) {
V v_522 = row_521[0];
V v_523 = row_521[1];
if (R.Truth(R.And((V)(R.Get(v_523, (V)"kind") == (V)"WindupStarted"), () => (V)(R.Get(v_523, (V)"source_id") == R.Get(R.Get(v_518, (V)"finale"), (V)"boss_id"))))) {
var values_524 = R.Join(new V[] {}, new Results((V)true));
var obj_525 = v_520;
V key_526 = R.Get(v_523, (V)"ability_id");
R.Set(obj_525, key_526, values_524[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_520, (V)"core:ability/regent_line"), () => R.Get(v_520, (V)"core:ability/regent_crown")), () => R.Get(v_520, (V)"core:ability/regent_sigil")))));
var values_527 = Results.Empty;
V v_528 = values_527[0];
V v_529 = values_527[1];
V v_530 = values_527[2];
foreach (var row_531 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_516, (V)"events"))))))) {
V v_532 = row_531[0];
V v_533 = row_531[1];
if (R.Truth(R.And((V)(R.Get(v_533, (V)"kind") == (V)"Died"), () => (V)(R.Get(v_533, (V)"target_id") == R.Get(R.Get(v_518, (V)"finale"), (V)"boss_id"))))) {
var values_534 = R.Join(new V[] {}, new Results(v_532));
v_528 = values_534[0];
} else if (R.Truth((V)(R.Get(v_533, (V)"kind") == (V)"RegentDefeated"))) {
var values_535 = R.Join(new V[] {}, new Results(v_532));
v_529 = values_535[0];
} else if (R.Truth((V)(R.Get(v_533, (V)"kind") == (V)"Victory"))) {
var values_536 = R.Join(new V[] {}, new Results(v_532));
v_530 = values_536[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(v_528, () => v_529), () => v_530), () => (V)(v_528 < v_529)), () => (V)(v_529 < v_530)))));
var values_537 = R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"replay"), R.Join(new V[] {}, new Results(v_516))));
V v_538 = values_537[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_518))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_538, (V)"state"))))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
