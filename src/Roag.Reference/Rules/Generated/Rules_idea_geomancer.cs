// Static C# port of src/idea_geomancer.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 80ee2ca78e3fa816906b1b1d6bb288dd46326f5fd3001b1829902b95a972f7e9
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_geomancer { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:actor/geomancer"), new Entry((V)"controller", (V)"geomancer_block"), new Entry((V)"block_id", (V)"core:object/geomancer_bulwark"), new Entry((V)"duration", (V)300.0d), new Entry((V)"tiny", (V)100.0d), new Entry((V)"onebit", (V)466.0d))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_6, (V)"id")), new Entry((V)"name", (V)"Geomancer"), new Entry((V)"weapon_icon", (V)"spell"), new Entry((V)"description", (V)"Heavy stone bolt, one temporary bulwark, grounded Dash"), new Entry((V)"hp", (V)24.0d), new Entry((V)"damage", (V)5.0d), new Entry((V)"slash_damage", (V)0.0d), new Entry((V)"defense", (V)1.0d), new Entry((V)"speed", (V)85.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)120.0d), new Entry((V)"sweeping_slash", (V)130.0d), new Entry((V)"dash", (V)130.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/geomancer_bolt"), new Entry((V)"special_id", (V)"core:ability/geomancer_raise"), new Entry((V)"basic_name", (V)"Stone Bolt"), new Entry((V)"special_name", (V)"Raise Bulwark"), new Entry((V)"dash_name", (V)"Grounded Dash"), new Entry((V)"ui_controls", (V)"F Stone Bolt \u00c2\u00b7 R Bulwark \u00c2\u00b7 X Dash"))));
var obj_8 = v_6;
V key_9 = (V)"definition";
R.Set(obj_8, key_9, values_7[0]);
var values_10 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_11 = values_10[0];
R.Set(v_6, (V)"preview", new V((RuleFunction)(args_12 => {
V v_13 = args_12[0];
V v_14 = args_12[1];
V v_15 = args_12[2];
V v_16 = args_12[3];
var values_17 = Results.Empty;
V v_18 = values_17[0];
foreach (var row_19 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_13, (V)"entities"))))))) {
V v_20 = row_19[0];
V v_21 = row_19[1];
if (R.Truth((V)(R.Get(v_21, (V)"id") == R.Get(v_13, (V)"player_id")))) {
var values_22 = R.Join(new V[] {}, new Results(v_21));
v_18 = values_22[0];

break;
}
}
var values_23 = R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_18))).First, (V)"position")));
V v_24 = values_23[0];
if (R.Truth(R.Or(R.Or((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_15 - R.Get(v_24, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_16 - R.Get(v_24, (V)"y"))))).First) != (V)1.0d), () => (V)(!R.Truth(R.Get(R.Get(v_14, (V)"rows"), v_16)))), () => (V)(R.Invoke(R.Get(R.Get(v_14, (V)"rows"), v_16), "sub", R.Join(new V[] {v_15}, new Results(v_15))).First != (V)".")))) {
return R.Join(new V[] {(V)false}, new Results((V)"choose adjacent plain floor"));
}
foreach (var row_25 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_14, (V)"placements"), () => R.Get(R.Get(v_14, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_26 = row_25[0];
V v_27 = row_25[1];
if (R.Truth(R.And((V)(R.Get(v_27, (V)"x") == v_15), () => (V)(R.Get(v_27, (V)"y") == v_16)))) {
return R.Join(new V[] {(V)false}, new Results((V)"stairs must stay clear"));
}
}
var values_28 = R.Join(new V[] {}, new Results(R.Table()));
V v_29 = values_28[0];
V v_30 = V.Nil;
v_30 = new V((RuleFunction)(args_31 => {
V v_32 = args_31[0];
V v_33 = args_31[1];
return R.Join(new V[] {}, new Results((V)((V)((V)(v_33 - (V)1.0d) * R.Get(v_14, (V)"width")) + v_32)));
return Results.Empty;
}));
foreach (var row_34 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_13, (V)"entities"))))))) {
V v_35 = row_34[0];
V v_36 = row_34[1];
var values_37 = R.Join(new V[] {}, new Results(R.Get(v_36, (V)"position")));
V v_38 = values_37[0];
if (R.Truth(R.And((V)(R.Get(v_38, (V)"floor_id") == R.Get(v_14, (V)"floor_id")), () => (V)(R.Get(v_36, (V)"controller") != R.Get(v_6, (V)"controller"))))) {
if (R.Truth(R.And(R.And((V)(R.Get(v_38, (V)"x") == v_15), () => (V)(R.Get(v_38, (V)"y") == v_16)), () => R.Get(v_36, (V)"blocks_movement")))) {
return R.Join(new V[] {(V)false}, new Results((V)"cell occupied"));
}
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.Get(v_36, (V)"blocks_movement"), () => (V)(R.Get(v_36, (V)"controller") != (V)"ai")), () => (V)(R.Get(v_36, (V)"controller") != (V)"player")), () => (V)(R.Get(v_36, (V)"controller") != (V)"door")), () => (V)(R.Get(v_36, (V)"controller") != (V)"review_gate")), () => (V)(R.Get(v_36, (V)"controller") != (V)"exit")))) {
var values_39 = R.Join(new V[] {}, new Results((V)true));
var obj_40 = v_29;
V key_41 = R.Call(v_30, R.Join(new V[] {R.Get(v_38, (V)"x")}, new Results(R.Get(v_38, (V)"y")))).First;
R.Set(obj_40, key_41, values_39[0]);
}
}
}
V v_42 = V.Nil;
v_42 = new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
var values_45 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_24, (V)"x")), new Entry((V)"y", R.Get(v_24, (V)"y")))))));
V v_46 = values_45[0];

var values_47 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Call(v_30, R.Join(new V[] {R.Get(v_24, (V)"x")}, new Results(R.Get(v_24, (V)"y")))).First, (V)true))));
V v_48 = values_47[0];

var values_49 = R.Join(new V[] {}, new Results((V)1.0d));
V v_50 = values_49[0];
while (R.Truth((V)(v_50 <= (V)R.Length(v_46)))) {
var values_51 = R.Join(new V[] {}, new Results(R.Get(v_46, v_50)));
V v_52 = values_51[0];

var values_53 = R.Join(new V[] {}, new Results((V)(v_50 + (V)1.0d)));
v_50 = values_53[0];
foreach (var row_54 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_11)))))) {
V v_55 = row_54[0];
V v_56 = row_54[1];
var values_57 = R.Join(new V[] {(V)(R.Get(v_52, (V)"x") + R.Get(v_56, (V)1.0d))}, new Results((V)(R.Get(v_52, (V)"y") + R.Get(v_56, (V)2.0d))));
V v_58 = values_57[0];
V v_59 = values_57[1];

var values_60 = R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {v_58}, new Results(v_59))));
V v_61 = values_60[0];
var values_62 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"at"), R.Join(new V[] {v_14, v_58}, new Results(v_59))));
V v_63 = values_62[0];
if (R.Truth(R.And(R.And(R.And(R.And(v_63, () => R.Get(v_63, (V)"walkable")), () => (V)(!R.Truth(R.Get(v_29, v_61)))), () => (V)(!R.Truth(R.Get(v_48, v_61)))), () => (V)(v_61 != v_44)))) {
var values_64 = R.Join(new V[] {}, new Results((V)true));
var obj_65 = v_48;
V key_66 = v_61;
R.Set(obj_65, key_66, values_64[0]);

var values_67 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_58), new Entry((V)"y", v_59))));
var obj_68 = v_46;
V key_69 = (V)((V)R.Length(v_46) + (V)1.0d);
R.Set(obj_68, key_69, values_67[0]);
}
}
}
return R.Join(new V[] {}, new Results((V)R.Length(v_46)));
return Results.Empty;
}));
if (R.Truth((V)(R.Call(v_42, R.Join(new V[] {}, R.Call(v_30, R.Join(new V[] {v_15}, new Results(v_16))))).First != (V)(R.Call(v_42, R.Join(new V[] {}, new Results(V.Nil))).First - (V)1.0d)))) {
return R.Join(new V[] {(V)false}, new Results((V)"cannot seal an accessible route"));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
V v_70 = V.Nil;
v_70 = new V((RuleFunction)(args_71 => {
V v_72 = args_71[0];
V v_73 = args_71[1];
{ double end_76 = R.Num((V)1.0d), step_77 = R.Num((-(V)1.0d));
for (double i_75 = R.Num((V)R.Length(v_72)); step_77 > 0 ? i_75 <= end_76 : i_75 >= end_76; i_75 += step_77) {
V v_74 = i_75;
if (R.Truth((V)(R.Get(R.Get(v_72, v_74), (V)"id") == v_73))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {v_72}, new Results(v_74)));
}
}
}
return Results.Empty;
}));
V v_78 = V.Nil;
v_78 = new V((RuleFunction)(args_79 => {
V v_80 = args_79[0];
{ double end_83 = R.Num((V)1.0d), step_84 = R.Num((-(V)1.0d));
for (double i_82 = R.Num((V)R.Length(R.Get(v_80, (V)"entities"))); step_84 > 0 ? i_82 <= end_83 : i_82 >= end_83; i_82 += step_84) {
V v_81 = i_82;
var values_85 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_80, (V)"entities"), v_81)));
V v_86 = values_85[0];
if (R.Truth((V)(R.Get(v_86, (V)"controller") == R.Get(v_6, (V)"controller")))) {
R.Call(v_70, R.Join(new V[] {R.Get(v_80, (V)"schedule")}, new Results(R.Get(v_86, (V)"expiry_schedule_id"))));
foreach (var row_87 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_80, (V)"floor_pause"), () => R.Table()))))))) {
V v_88 = row_87[0];
V v_89 = row_87[1];
R.Call(v_70, R.Join(new V[] {R.Get(v_89, (V)"schedule")}, new Results(R.Get(v_86, (V)"expiry_schedule_id"))));
}
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_80, (V)"entities")}, new Results(v_81)));
}
}
}
return Results.Empty;
}));
R.Set(v_6, (V)"place", new V((RuleFunction)(args_90 => {
V v_91 = args_90[0];
V v_92 = args_90[1];
V v_93 = args_90[2];
V v_94 = args_90[3];
V v_95 = args_90[4];
V v_96 = args_90[5];
R.Call(v_78, R.Join(new V[] {}, new Results(v_91)));
var values_97 = R.Join(new V[] {}, new Results(R.Get(v_91, (V)"next_entity_id")));
V v_98 = values_97[0];

var values_99 = R.Join(new V[] {}, new Results((V)(v_98 + (V)1.0d)));
var obj_100 = v_91;
V key_101 = (V)"next_entity_id";
R.Set(obj_100, key_101, values_99[0]);
var values_102 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_98), new Entry((V)"definition_id", R.Get(v_6, (V)"block_id")), new Entry((V)"controller", R.Get(v_6, (V)"controller")), new Entry((V)"owner_id", R.Get(v_92, (V)"id")), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_92, (V)"position"), (V)"floor_id")), new Entry((V)"x", v_93), new Entry((V)"y", v_94))), new Entry((V)"expires", (V)(R.Get(v_91, (V)"clock") + R.Get(v_6, (V)"duration"))), new Entry((V)"blocks_movement", (V)true), new Entry((V)"blocks_vision", (V)true), new Entry((V)"blocks_projectiles", (V)true), new Entry((V)"boons", R.Table()))));
V v_103 = values_102[0];
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"enqueue"), R.Join(new V[] {v_91}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", R.Get(v_103, (V)"expires")), new Entry((V)"actor_id", (V)0.0d), new Entry((V)"attack_id", v_95), new Entry((V)"floor_id", R.Get(R.Get(v_103, (V)"position"), (V)"floor_id")))))));
V v_105 = values_104[0];
var values_106 = R.Join(new V[] {}, new Results(R.Get(v_105, (V)"id")));
var obj_107 = v_105;
V key_108 = (V)"effect_id";
R.Set(obj_107, key_108, values_106[0]);

var values_109 = R.Join(new V[] {}, new Results((V)true));
var obj_110 = v_105;
V key_111 = (V)"idea_geomancer";
R.Set(obj_110, key_111, values_109[0]);

var values_112 = R.Join(new V[] {}, new Results(R.Get(v_105, (V)"id")));
var obj_113 = v_103;
V key_114 = (V)"expiry_schedule_id";
R.Set(obj_113, key_114, values_112[0]);
var values_115 = R.Join(new V[] {}, new Results(v_103));
var obj_116 = R.Get(v_91, (V)"entities");
V key_117 = (V)((V)R.Length(R.Get(v_91, (V)"entities")) + (V)1.0d);
R.Set(obj_116, key_117, values_115[0]);
R.Call(v_96, R.Join(new V[] {(V)"GeomancerBlockRaised"}, new Results(R.Table(new Entry((V)"target_id", v_98), new Entry((V)"floor_id", R.Get(R.Get(v_103, (V)"position"), (V)"floor_id")), new Entry((V)"x", v_93), new Entry((V)"y", v_94), new Entry((V)"expires", R.Get(v_103, (V)"expires"))))));
return R.Join(new V[] {}, new Results(v_103));
return Results.Empty;
})));
R.Set(v_6, (V)"expire", new V((RuleFunction)(args_118 => {
V v_119 = args_118[0];
V v_120 = args_118[1];
V v_121 = args_118[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_120, (V)"idea_geomancer"))));
foreach (var row_122 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_119, (V)"entities"))))))) {
V v_123 = row_122[0];
V v_124 = row_122[1];
if (R.Truth(R.And((V)(R.Get(v_124, (V)"controller") == R.Get(v_6, (V)"controller")), () => (V)(R.Get(v_124, (V)"expiry_schedule_id") == R.Get(v_120, (V)"id"))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_124, (V)"expires") == R.Get(v_119, (V)"clock")), () => (V)(R.Get(R.Get(v_124, (V)"position"), (V)"floor_id") == R.Get(v_119, (V)"active_floor_id")))}, new Results((V)"mistimed bulwark expiry")));
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_119, (V)"entities")}, new Results(v_123)));
R.Call(v_121, R.Join(new V[] {(V)"GeomancerBlockExpired", R.Table(new Entry((V)"target_id", R.Get(v_124, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_124, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_124, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_124, (V)"position"), (V)"y"))), R.Get(v_124, (V)"owner_id")}, new Results(R.Get(v_120, (V)"attack_id"))));
return Results.Empty;
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"orphaned bulwark expiry")));
return Results.Empty;
})));
R.Set(v_6, (V)"validate", new V((RuleFunction)(args_125 => {
V v_126 = args_125[0];
V v_127 = args_125[1];
var values_128 = R.Join(new V[] {}, new Results(R.Table()));
V v_129 = values_128[0];

foreach (var row_130 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_126, (V)"schedule"))))))) {
V v_131 = row_130[0];
V v_132 = row_130[1];
var values_133 = R.Join(new V[] {}, new Results(v_132));
var obj_134 = v_129;
V key_135 = (V)((V)R.Length(v_129) + (V)1.0d);
R.Set(obj_134, key_135, values_133[0]);
}
foreach (var row_136 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_126, (V)"floor_pause"), () => R.Table()))))))) {
V v_137 = row_136[0];
V v_138 = row_136[1];
foreach (var row_139 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_138, (V)"schedule"))))))) {
V v_140 = row_139[0];
V v_141 = row_139[1];
var values_142 = R.Join(new V[] {}, new Results(v_141));
var obj_143 = v_129;
V key_144 = (V)((V)R.Length(v_129) + (V)1.0d);
R.Set(obj_143, key_144, values_142[0]);
}
}
var values_145 = R.Join(new V[] {(V)0.0d}, new Results(R.Table()));
V v_146 = values_145[0];
V v_147 = values_145[1];
foreach (var row_148 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_126, (V)"entities"))))))) {
V v_149 = row_148[0];
V v_150 = row_148[1];
if (R.Truth((V)(R.Get(v_150, (V)"controller") == R.Get(v_6, (V)"controller")))) {
var values_151 = R.Join(new V[] {}, new Results((V)(v_146 + (V)1.0d)));
v_146 = values_151[0];
var values_152 = Results.Empty;
V v_153 = values_152[0];
V v_154 = values_152[1];
V v_155 = values_152[2];
foreach (var row_156 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_126, (V)"entities"))))))) {
V v_157 = row_156[0];
V v_158 = row_156[1];
if (R.Truth((V)(R.Get(v_158, (V)"id") == R.Get(v_150, (V)"owner_id")))) {
var values_159 = R.Join(new V[] {}, new Results(v_158));
v_153 = values_159[0];

break;
}
}
var values_160 = R.Join(new V[] {}, new Results(R.Get(v_127, R.Get(R.Get(v_150, (V)"position"), (V)"floor_id"))));
v_154 = values_160[0];

var values_161 = R.Join(new V[] {}, new Results(R.And(R.Get(v_126, (V)"floor_pause"), () => R.Get(R.Get(v_126, (V)"floor_pause"), R.Get(R.Get(v_150, (V)"position"), (V)"floor_id")))));
v_155 = values_161[0];
var values_162 = R.Join(new V[] {}, new Results(R.Or(R.And(v_155, () => R.Get(v_155, (V)"suspended_at")), () => R.Get(v_126, (V)"clock"))));
V v_163 = values_162[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_146 <= (V)1.0d), () => v_153), () => (V)(R.Get(v_153, (V)"id") == R.Get(v_126, (V)"player_id"))), () => (V)(R.Get(v_153, (V)"definition_id") == R.Get(v_6, (V)"id"))), () => (V)(R.Get(v_150, (V)"definition_id") == R.Get(v_6, (V)"block_id"))), () => (V)(R.Get(v_150, (V)"hp") == V.Nil)), () => (V)(R.Get(v_150, (V)"blocks_movement") == (V)true)), () => (V)(R.Get(v_150, (V)"blocks_vision") == (V)true)), () => (V)(R.Get(v_150, (V)"blocks_projectiles") == (V)true)), () => v_154), () => R.Get(R.Get(v_154, (V)"rows"), R.Get(R.Get(v_150, (V)"position"), (V)"y"))), () => (V)(R.Invoke(R.Get(R.Get(v_154, (V)"rows"), R.Get(R.Get(v_150, (V)"position"), (V)"y")), "sub", R.Join(new V[] {R.Get(R.Get(v_150, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_150, (V)"position"), (V)"x")))).First == (V)".")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_150, (V)"expires")))).First == (V)"number")), () => (V)((V)(R.Get(v_150, (V)"expires") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_150, (V)"expires") >= v_163)), () => (V)(R.Get(v_150, (V)"expires") <= (V)(v_163 + R.Get(v_6, (V)"duration"))))}, new Results((V)"invalid owned bulwark")));
var values_164 = R.Join(new V[] {}, new Results((V)false));
V v_165 = values_164[0];
foreach (var row_166 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_129)))))) {
V v_167 = row_166[0];
V v_168 = row_166[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_168, (V)"id") == R.Get(v_150, (V)"expiry_schedule_id")), () => (V)(R.Get(v_168, (V)"kind") == (V)"effect")), () => (V)(R.Get(v_168, (V)"actor_id") == (V)0.0d)), () => (V)(R.Get(v_168, (V)"idea_geomancer") == (V)true)), () => (V)(R.Get(v_168, (V)"effect_id") == R.Get(v_168, (V)"id"))), () => (V)(R.Get(v_168, (V)"floor_id") == R.Get(R.Get(v_150, (V)"position"), (V)"floor_id"))), () => (V)(R.Get(v_168, (V)"due") == R.Get(v_150, (V)"expires"))))) {
var values_169 = R.Join(new V[] {}, new Results((V)true));
v_165 = values_169[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_165}, new Results((V)"missing bulwark expiry")));

var values_170 = R.Join(new V[] {}, new Results((V)true));
var obj_171 = v_147;
V key_172 = R.Get(v_150, (V)"expiry_schedule_id");
R.Set(obj_171, key_172, values_170[0]);
}
}
foreach (var row_173 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_129)))))) {
V v_174 = row_173[0];
V v_175 = row_173[1];
if (R.Truth(R.Get(v_175, (V)"idea_geomancer"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_175, (V)"idea_geomancer") == (V)true), () => R.Get(v_147, R.Get(v_175, (V)"id")))}, new Results((V)"orphaned bulwark work")));
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
