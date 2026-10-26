// Static C# port of tests/idea_terrain_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 98330596ca9cb79de7feaf9fb09763360329f731cb8677a282f0c64e30e1c1e7
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_terrain_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_11 = values_10[0];

var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))));
V v_13 = values_12[0];

var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_15 = values_14[0];

var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_17 = values_16[0];
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
var values_22 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_20}, new Results(v_21))));
V v_23 = values_22[0];
V v_24 = values_22[1];
V v_25 = values_22[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_23}, new Results(v_24)));

return R.Join(new V[] {}, new Results(v_25));
return Results.Empty;
}));
V v_26 = V.Nil;
v_26 = new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
V v_29 = args_27[1];
var values_30 = R.Join(new V[] {}, new Results((V)0.0d));
V v_31 = values_30[0];

foreach (var row_32 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_28)))))) {
V v_33 = row_32[0];
V v_34 = row_32[1];
if (R.Truth((V)(R.Get(v_34, (V)"kind") == v_29))) {
var values_35 = R.Join(new V[] {}, new Results((V)(v_31 + (V)1.0d)));
v_31 = values_35[0];
}
}

return R.Join(new V[] {}, new Results(v_31));
return Results.Empty;
}));
V v_36 = V.Nil;
v_36 = new V((RuleFunction)(args_37 => {
V v_38 = args_37[0];
V v_39 = args_37[1];
V v_40 = args_37[2];
V v_41 = args_37[3];
V v_42 = args_37[4];
var values_43 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_44 = values_43[0];

var values_45 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_44))));
V v_46 = values_45[0];

var values_47 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_48 = R.Get(v_46, (V)"position");
V key_49 = (V)"x";
var obj_50 = R.Get(v_46, (V)"position");
V key_51 = (V)"y";
R.Set(obj_48, key_49, values_47[0]);
R.Set(obj_50, key_51, values_47[1]);

R.Call(R.Get(v_11, (V)"recompute"), R.Join(new V[] {}, new Results(v_44)));
return R.Join(new V[] {v_44, v_46}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_44, R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_44))).First, v_38, R.Or(v_39, () => (V)8.0d), R.Or(v_40, () => (V)6.0d), v_41}, new Results(v_42))));
return Results.Empty;
}));
V v_52 = V.Nil;
v_52 = new V((RuleFunction)(args_53 => {
V v_54 = args_53[0];
var values_55 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_generated"), R.Join(new V[] {(V)37.0d}, new Results(R.Table(new Entry((V)"class_id", v_54))))));
V v_56 = values_55[0];

var values_57 = R.Join(new V[] {}, new Results((V)false));
var obj_58 = R.Get(v_56, (V)"meta");
V key_59 = (V)"eligible";
R.Set(obj_58, key_59, values_57[0]);
{ double end_62 = R.Num((V)1.0d), step_63 = R.Num((-(V)1.0d));
for (double i_61 = R.Num((V)R.Length(R.Get(v_56, (V)"entities"))); step_63 > 0 ? i_61 <= end_62 : i_61 >= end_62; i_61 += step_63) {
V v_60 = i_61;
var values_64 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_56, (V)"entities"), v_60)));
V v_65 = values_64[0];

if (R.Truth((V)(R.Get(v_65, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_56}, new Results(R.Get(v_65, (V)"id"))));

R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_56, (V)"entities")}, new Results(v_60)));
}
}
}
var values_66 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"version", (V)1.0d), new Entry((V)"nodes", R.Table()))));
var obj_67 = v_56;
V key_68 = (V)"idea_terrain";
R.Set(obj_67, key_68, values_66[0]);

var values_69 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_56))));
V v_70 = values_69[0];

var values_71 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_56))));
V v_72 = values_71[0];
var values_73 = R.Join(new V[] {}, new Results(R.Table()));
V v_74 = values_73[0];

foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_70, (V)"protected_cells"))))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
var values_78 = R.Join(new V[] {}, new Results((V)true));
var obj_79 = v_74;
V key_80 = R.Concat(R.Get(v_77, (V)"x"), R.Concat((V)",", R.Get(v_77, (V)"y")));
R.Set(obj_79, key_80, values_78[0]);
}
foreach (var row_81 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_70, (V)"editable_cells"))))))) {
V v_82 = row_81[0];
V v_83 = row_81[1];
if (R.Truth(R.And(R.And(R.And(R.Call(R.Get(v_7, (V)"optional_cell"), R.Join(new V[] {v_70, R.Get(v_83, (V)"x")}, new Results(R.Get(v_83, (V)"y")))).First, () => R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {v_70, (V)(R.Get(v_83, (V)"x") - (V)1.0d)}, new Results(R.Get(v_83, (V)"y")))).First), () => R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {v_70, (V)(R.Get(v_83, (V)"x") + (V)1.0d)}, new Results(R.Get(v_83, (V)"y")))).First), () => R.Call(R.Get(v_7, (V)"alternate_route"), R.Join(new V[] {v_56, v_70, R.Get(v_83, (V)"x")}, new Results(R.Get(v_83, (V)"y")))).First))) {
var values_84 = R.Join(new V[] {}, new Results((V)true));
V v_85 = values_84[0];
foreach (var row_86 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_56, (V)"entities"))))))) {
V v_87 = row_86[0];
V v_88 = row_86[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_88, (V)"id") != R.Get(v_72, (V)"id")), () => R.Get(v_88, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_88, (V)"position"), (V)"y") == R.Get(v_83, (V)"y"))), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_88, (V)"position"), (V)"x") - R.Get(v_83, (V)"x"))))).First <= (V)1.0d)))) {
var values_89 = R.Join(new V[] {}, new Results((V)false));
v_85 = values_89[0];
}
}
if (R.Truth(v_85)) {
var values_90 = R.Join(new V[] {(V)(R.Get(v_83, (V)"x") - (V)1.0d)}, new Results(R.Get(v_83, (V)"y")));
var obj_91 = R.Get(v_72, (V)"position");
V key_92 = (V)"x";
var obj_93 = R.Get(v_72, (V)"position");
V key_94 = (V)"y";
R.Set(obj_91, key_92, values_90[0]);
R.Set(obj_93, key_94, values_90[1]);

R.Call(R.Get(v_11, (V)"recompute"), R.Join(new V[] {}, new Results(v_56)));

return R.Join(new V[] {v_56, v_72}, new Results(v_70));
}
}
}

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no clear prepared shortcut")));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"terrain inspection, idle previews and rendering time do not advance clocks or consume RNG"}, new Results(new V((RuleFunction)(args_95 => {

var values_96 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results((V)"heat_vent"))));
V v_97 = values_96[0];
V v_98 = values_96[1];
V v_99 = values_96[2];

var values_100 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_97))));
V v_101 = values_100[0];
{ double end_104 = R.Num((V)20.0d), step_105 = R.Num((V)1.0d);
for (double i_103 = R.Num((V)1.0d); step_105 > 0 ? i_103 <= end_104 : i_103 >= end_104; i_103 += step_105) {
V v_102 = i_103;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Call(R.Get(v_5, (V)"interaction_options"), R.Join(new V[] {v_97}, new Results(R.Get(v_99, (V)"id")))).First, (V)"entries")) == (V)0.0d))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_5, (V)"nearby_interactions"), R.Join(new V[] {}, new Results(v_97))).First) >= (V)1.0d))));

R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_97, (V)"move", (V)1.0d}, new Results((V)0.0d)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_97}, new Results(R.Table(new Entry((V)"kind", (V)"interact"), new Entry((V)"node_id", R.Get(v_99, (V)"id")), new Entry((V)"choice_id", (V)"trigger"))))).First)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_97))).First}, new Results(v_101)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_99, (V)"phase") == (V)"cooldown"), () => (V)(!R.Truth(R.Get(v_99, (V)"schedule_id")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"visible vent locks a world-time warning and impact wins its tie with the next decision"}, new Results(new V((RuleFunction)(args_106 => {

var values_107 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results((V)"heat_vent"))));
V v_108 = values_107[0];
V v_109 = values_107[1];
V v_110 = values_107[2];

var values_111 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_108}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_112 = values_111[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_110, (V)"phase") == (V)"warning"), () => (V)(R.Get(v_110, (V)"due") == (V)200.0d)), () => (V)(R.Get(v_108, (V)"clock") == (V)100.0d)), () => (V)(R.Call(v_26, R.Join(new V[] {v_112}, new Results((V)"HeatVentWarned"))).First == (V)1.0d)))));
var values_113 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"pending_strikes"), R.Join(new V[] {}, new Results(v_108))));
V v_114 = values_113[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_114) == (V)1.0d), () => R.Get(R.Get(v_114, (V)1.0d), (V)"terrain_heat")), () => (V)(R.Get(R.Get(v_114, (V)1.0d), (V)"due") == (V)200.0d)))));
var values_115 = R.Join(new V[] {}, new Results(R.Get(v_109, (V)"hp")));
V v_116 = values_115[0];

var values_117 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_108}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_118 = values_117[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_109, (V)"hp") == (V)(v_116 - (V)2.0d)), () => (V)(R.Get(v_108, (V)"clock") == (V)200.0d)), () => (V)(R.Get(v_110, (V)"phase") == (V)"cooldown")), () => (V)(R.Get(v_110, (V)"cooldown_until") == (V)500.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_26, R.Join(new V[] {v_118}, new Results((V)"HeatVentBurst"))).First == (V)1.0d), () => (V)(R.Call(v_26, R.Join(new V[] {v_118}, new Results((V)"DamageTaken"))).First == (V)1.0d)), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_108))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"leaving a locked vent avoids its impact without retargeting or inventing a reaction turn"}, new Results(new V((RuleFunction)(args_119 => {

var values_120 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {(V)"heat_vent", (V)7.0d}, new Results((V)6.0d))));
V v_121 = values_120[0];
V v_122 = values_120[1];
V v_123 = values_120[2];

R.Call(v_18, R.Join(new V[] {v_121}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

var values_124 = R.Join(new V[] {}, new Results(R.Get(v_122, (V)"hp")));
V v_125 = values_124[0];
var values_126 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_121}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))))));
V v_127 = values_126[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_122, (V)"hp") == v_125), () => (V)(R.Get(v_123, (V)"phase") == (V)"cooldown")), () => (V)(R.Call(v_26, R.Join(new V[] {v_127}, new Results((V)"DamageTaken"))).First == (V)0.0d)))));
foreach (var row_128 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_127)))))) {
V v_129 = row_128[0];
V v_130 = row_128[1];
if (R.Truth((V)(R.Get(v_130, (V)"kind") == (V)"HeatVentBurst"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_130, (V)"x") == (V)7.0d), () => (V)(R.Get(v_130, (V)"y") == (V)6.0d)), () => (V)(R.Get(v_130, (V)"time") == (V)200.0d)))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"unseen vents never arm and cooldowns require committed visible-world actions"}, new Results(new V((RuleFunction)(args_131 => {

var values_132 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {(V)"heat_vent", (V)11.0d}, new Results((V)6.0d))));
V v_133 = values_132[0];
V v_134 = values_132[1];
V v_135 = values_132[2];

R.Call(R.Get(v_11, (V)"recompute"), R.Join(new V[] {v_133}, new Results((V)1.0d)));
R.Call(v_18, R.Join(new V[] {v_133}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_135, (V)"phase") == (V)"cooldown"), () => (V)(!R.Truth(R.Get(v_135, (V)"schedule_id")))))));
R.Call(R.Get(v_11, (V)"recompute"), R.Join(new V[] {v_133}, new Results((V)8.0d)));

R.Call(v_18, R.Join(new V[] {v_133}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_135, (V)"phase") == (V)"warning"), () => (V)(R.Get(v_135, (V)"due") == (V)300.0d)))));
var values_136 = R.Join(new V[] {(V)2.0d}, new Results((V)2.0d));
var obj_137 = R.Get(v_134, (V)"position");
V key_138 = (V)"x";
var obj_139 = R.Get(v_134, (V)"position");
V key_140 = (V)"y";
R.Set(obj_137, key_138, values_136[0]);
R.Set(obj_139, key_140, values_136[1]);

R.Call(R.Get(v_11, (V)"recompute"), R.Join(new V[] {v_133}, new Results((V)1.0d)));
var values_141 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_133}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_142 = values_141[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_26, R.Join(new V[] {v_142}, new Results((V)"HeatVentBurst"))).First == (V)0.0d), () => (V)(R.Get(v_135, (V)"phase") == (V)"cooldown")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"vent fire uses ordinary damage, ward, death and enemy friendly-fire handling"}, new Results(new V((RuleFunction)(args_143 => {

var values_144 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {(V)"heat_vent", (V)7.0d}, new Results((V)6.0d))));
V v_145 = values_144[0];
V v_146 = values_144[1];
V v_147 = values_144[2];

var values_148 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_149 = v_146;
V key_150 = (V)"pickup_ward_actions";
R.Set(obj_149, key_150, values_148[0]);
R.Call(v_18, R.Join(new V[] {v_145}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

var values_151 = R.Join(new V[] {}, new Results(R.Get(v_146, (V)"hp")));
V v_152 = values_151[0];

var values_153 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_145}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_154 = values_153[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_146, (V)"hp") == v_152), () => (V)(R.Call(v_26, R.Join(new V[] {v_154}, new Results((V)"WardFlaskBlocked"))).First == (V)1.0d)), () => (V)(R.Get(v_146, (V)"pickup_ward_actions") == (V)0.0d)))));
var values_155 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results((V)"heat_vent"))));
V v_156 = values_155[0];
V v_157 = values_155[1];
V v_158 = values_155[2];

var values_159 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_156, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_160 = values_159[0];
R.Call(v_18, R.Join(new V[] {v_156}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

var values_161 = R.Join(new V[] {}, new Results(R.Get(v_160, (V)"hp")));
V v_162 = values_161[0];

var values_163 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_156}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
v_154 = values_163[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_160, (V)"hp") == (V)(v_162 - R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)((V)2.0d - R.Get(v_160, (V)"defense"))))).First)), () => (V)(R.Call(v_26, R.Join(new V[] {v_154}, new Results((V)"DamageTaken"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a pending heat impact and cooldown pause/rebase with their exact floor time"}, new Results(new V((RuleFunction)(args_164 => {

var values_165 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results((V)"heat_vent"))));
V v_166 = values_165[0];
V v_167 = values_165[1];
V v_168 = values_165[2];

R.Call(v_18, R.Join(new V[] {v_166}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

R.Call(R.Get(v_13, (V)"park"), R.Join(new V[] {v_166}, new Results(R.Get(v_166, (V)"active_floor_id"))));
var values_169 = R.Join(new V[] {}, new Results((V)(R.Get(v_166, (V)"clock") + (V)1000.0d)));
var obj_170 = v_166;
V key_171 = (V)"clock";
R.Set(obj_170, key_171, values_169[0]);

R.Call(R.Get(v_13, (V)"resume"), R.Join(new V[] {v_166}, new Results(R.Get(v_166, (V)"active_floor_id"))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_168, (V)"due") == (V)1200.0d), () => (V)(R.Get(v_168, (V)"cooldown_until") == (V)1000.0d)))));
var values_172 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_166))))))))));
V v_173 = values_172[0];

R.Call(v_3, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {v_166}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))).First}, R.Call(v_18, R.Join(new V[] {v_173}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_166))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_173)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_168, (V)"cooldown_until") == (V)1500.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an entered arrow performs one legal push with normal cost, pickups and motion events"}, new Results(new V((RuleFunction)(args_174 => {

var values_175 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {(V)"push_floor", (V)8.0d, (V)6.0d, (V)1.0d}, new Results((V)0.0d))));
V v_176 = values_175[0];
V v_177 = values_175[1];
V v_178 = values_175[2];

var values_179 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_176}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_180 = values_179[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_177, (V)"position"), (V)"x") == (V)9.0d), () => (V)(R.Get(R.Get(v_177, (V)"position"), (V)"y") == (V)6.0d)), () => (V)(R.Get(v_176, (V)"clock") == (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_26, R.Join(new V[] {v_180}, new Results((V)"PushFloorTriggered"))).First == (V)1.0d), () => (V)(R.Call(v_26, R.Join(new V[] {v_180}, new Results((V)"Displaced"))).First == (V)1.0d)), () => (V)(R.Call(v_26, R.Join(new V[] {v_180}, new Results((V)"TileEntered"))).First == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_176)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"push floors stop at creatures instead of bypassing collision or consuming another action"}, new Results(new V((RuleFunction)(args_181 => {

var values_182 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {(V)"push_floor", (V)8.0d, (V)6.0d, (V)1.0d}, new Results((V)0.0d))));
V v_183 = values_182[0];
V v_184 = values_182[1];
V v_185 = values_182[2];

R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_183, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)6.0d}, new Results((V)100000.0d)));
var values_186 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_183}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_187 = values_186[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_184, (V)"position"), (V)"x") == (V)8.0d), () => (V)(R.Get(v_183, (V)"clock") == (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_26, R.Join(new V[] {v_187}, new Results((V)"PushFloorBlocked"))).First == (V)1.0d), () => (V)(R.Call(v_26, R.Join(new V[] {v_187}, new Results((V)"Displaced"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"cyclic arrows terminate after four pushes without truncating ordinary gameplay work"}, new Results(new V((RuleFunction)(args_188 => {

var values_189 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {(V)"push_floor", (V)8.0d, (V)6.0d, (V)1.0d}, new Results((V)0.0d))));
V v_190 = values_189[0];
V v_191 = values_189[1];
V v_192 = values_189[2];

R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_190, R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_190))).First, (V)"push_floor", (V)9.0d, (V)6.0d, (-(V)1.0d)}, new Results((V)0.0d)));
var values_193 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_190}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_194 = values_193[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(v_26, R.Join(new V[] {v_194}, new Results((V)"PushFloorTriggered"))).First == (V)4.0d), () => (V)(R.Call(v_26, R.Join(new V[] {v_194}, new Results((V)"PushFloorBlocked"))).First == (V)1.0d)), () => (V)(R.Get(v_190, (V)"clock") == (V)100.0d)), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_190))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"enemy movement triggers the same neutral push and preserves strict scheduler ordering"}, new Results(new V((RuleFunction)(args_195 => {

var values_196 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {(V)"push_floor", (V)8.0d, (V)6.0d, (V)0.0d}, new Results((-(V)1.0d)))));
V v_197 = values_196[0];
V v_198 = values_196[1];
V v_199 = values_196[2];

var values_200 = R.Join(new V[] {}, new Results((V)6.0d));
var obj_201 = R.Get(v_198, (V)"position");
V key_202 = (V)"x";
R.Set(obj_201, key_202, values_200[0]);

R.Call(R.Get(v_11, (V)"recompute"), R.Join(new V[] {}, new Results(v_197)));
var values_203 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_197, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)6.0d}, new Results((V)0.0d))));
V v_204 = values_203[0];
var values_205 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_197}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_206 = values_205[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_204, (V)"position"), (V)"x") == (V)8.0d), () => (V)(R.Get(R.Get(v_204, (V)"position"), (V)"y") == (V)5.0d)), () => (V)(R.Call(v_26, R.Join(new V[] {v_206}, new Results((V)"PushFloorTriggered"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_197, (V)"clock") == (V)100.0d), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_197))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Dash crossing an intermediate arrow does not retroactively move the landing actor"}, new Results(new V((RuleFunction)(args_207 => {

var values_208 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {(V)"push_floor", (V)8.0d, (V)6.0d, (V)0.0d}, new Results((-(V)1.0d)))));
V v_209 = values_208[0];
V v_210 = values_208[1];
V v_211 = values_208[2];

var values_212 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_209}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_213 = values_212[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_210, (V)"position"), (V)"x") == (V)9.0d), () => (V)(R.Get(R.Get(v_210, (V)"position"), (V)"y") == (V)6.0d)), () => (V)(R.Call(v_26, R.Join(new V[] {v_213}, new Results((V)"PushFloorTriggered"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"fragile shortcut arms on entry, holds an occupant and permanently closes only after departure"}, new Results(new V((RuleFunction)(args_214 => {

var values_215 = R.Join(new V[] {}, R.Call(v_52, Results.Empty));
V v_216 = values_215[0];
V v_217 = values_215[1];
V v_218 = values_215[2];

var values_219 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_216, v_218, (V)"fragile_crossing", (V)(R.Get(R.Get(v_217, (V)"position"), (V)"x") + (V)1.0d)}, new Results(R.Get(R.Get(v_217, (V)"position"), (V)"y")))));
V v_220 = values_219[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"alternate_route"), R.Join(new V[] {v_216, v_218, R.Get(v_220, (V)"x")}, new Results(R.Get(v_220, (V)"y"))))));

R.Call(v_18, R.Join(new V[] {v_216}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_220, (V)"phase") == (V)"armed"), () => R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {v_218, R.Get(v_220, (V)"x")}, new Results(R.Get(v_220, (V)"y")))).First))));

var values_221 = R.Join(new V[] {}, new Results(R.Get(v_217, (V)"hp")));
V v_222 = values_221[0];
R.Call(v_18, R.Join(new V[] {v_216}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_220, (V)"phase") == (V)"armed"), () => (V)(R.Get(v_217, (V)"hp") == v_222)))));
var values_223 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_216}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_224 = values_223[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_220, (V)"phase") == (V)"closed"), () => (V)(!R.Truth(R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {v_218, R.Get(v_220, (V)"x")}, new Results(R.Get(v_220, (V)"y")))).First))), () => (V)(R.Invoke(R.Get(R.Get(v_218, (V)"rows"), R.Get(v_220, (V)"y")), "sub", R.Join(new V[] {R.Get(v_220, (V)"x")}, new Results(R.Get(v_220, (V)"x")))).First == (V)"~")), () => (V)(R.Get(v_217, (V)"hp") == v_222)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_26, R.Join(new V[] {v_224}, new Results((V)"FragileCollapsed"))).First == (V)1.0d), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_216))))).First))));
var values_225 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_216))))));
V v_226 = values_225[0];

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_226))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_216)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_7, (V)"find"), R.Join(new V[] {v_226}, new Results(R.Get(v_220, (V)"id")))).First, (V)"phase") == (V)"closed"))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Dash can cross a finite fragile shortcut and no occupied cell suffers collapse damage"}, new Results(new V((RuleFunction)(args_227 => {

var values_228 = R.Join(new V[] {}, R.Call(v_52, Results.Empty));
V v_229 = values_228[0];
V v_230 = values_228[1];
V v_231 = values_228[2];

var values_232 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_229, v_231, (V)"fragile_crossing", (V)(R.Get(R.Get(v_230, (V)"position"), (V)"x") + (V)1.0d)}, new Results(R.Get(R.Get(v_230, (V)"position"), (V)"y")))));
V v_233 = values_232[0];

var values_234 = R.Join(new V[] {}, new Results(R.Get(v_230, (V)"hp")));
V v_235 = values_234[0];
var values_236 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {v_229}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_237 = values_236[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_233, (V)"phase") == (V)"closed"), () => (V)(R.Get(v_230, (V)"hp") == v_235)), () => (V)(R.Call(v_26, R.Join(new V[] {v_237}, new Results((V)"FragileArmed"))).First == (V)1.0d)), () => (V)(R.Call(v_26, R.Join(new V[] {v_237}, new Results((V)"FragileCollapsed"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a collected reward retains its valid historical position after its shortcut becomes a gap"}, new Results(new V((RuleFunction)(args_238 => {

var values_239 = R.Join(new V[] {}, R.Call(v_52, Results.Empty));
V v_240 = values_239[0];
V v_241 = values_239[1];
V v_242 = values_239[2];

var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_240, v_242, (V)"fragile_crossing", (V)(R.Get(R.Get(v_241, (V)"position"), (V)"x") + (V)1.0d)}, new Results(R.Get(R.Get(v_241, (V)"position"), (V)"y")))));
V v_244 = values_243[0];
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))).First, (V)"spawn_currency"), R.Join(new V[] {v_240, (V)1.0d, R.Get(v_242, (V)"floor_id"), R.Get(v_244, (V)"x"), R.Get(v_244, (V)"y"), new V((RuleFunction)(args_245 => {


return Results.Empty;
}))}, new Results(R.Get(v_241, (V)"id"))));
R.Call(v_18, R.Join(new V[] {v_240}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_240, (V)"drops"), (V)1.0d), (V)"collected"))));
R.Call(v_18, R.Join(new V[] {v_240}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_244, (V)"phase") == (V)"closed"), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_240))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an uncollected reward holds a vacated shortcut until it can be collected safely"}, new Results(new V((RuleFunction)(args_246 => {

var values_247 = R.Join(new V[] {}, R.Call(v_52, Results.Empty));
V v_248 = values_247[0];
V v_249 = values_247[1];
V v_250 = values_247[2];

var values_251 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_248, v_250, (V)"fragile_crossing", (V)(R.Get(R.Get(v_249, (V)"position"), (V)"x") + (V)1.0d)}, new Results(R.Get(R.Get(v_249, (V)"position"), (V)"y")))));
V v_252 = values_251[0];
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))).First, (V)"spawn_currency"), R.Join(new V[] {v_248, (V)1.0d, R.Get(v_250, (V)"floor_id"), R.Get(v_252, (V)"x"), R.Get(v_252, (V)"y"), new V((RuleFunction)(args_253 => {


return Results.Empty;
}))}, new Results(R.Get(v_249, (V)"id"))));
var values_254 = R.Join(new V[] {}, new Results((V)"armed"));
var obj_255 = v_252;
V key_256 = (V)"phase";
R.Set(obj_255, key_256, values_254[0]);

R.Call(v_18, R.Join(new V[] {v_248}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_252, (V)"phase") == (V)"armed"), () => R.Call(R.Get(v_9, (V)"walkable"), R.Join(new V[] {v_250, R.Get(v_252, (V)"x")}, new Results(R.Get(v_252, (V)"y")))).First))));
R.Call(v_18, R.Join(new V[] {v_248}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));

R.Call(v_18, R.Join(new V[] {v_248}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_252, (V)"phase") == (V)"closed"), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_248))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a placed class snare holds a shortcut until its ordinary replacement releases it"}, new Results(new V((RuleFunction)(args_257 => {

var values_258 = R.Join(new V[] {}, R.Call(v_52, R.Join(new V[] {}, new Results((V)"core:actor/trapper"))));
V v_259 = values_258[0];
V v_260 = values_258[1];
V v_261 = values_258[2];

var values_262 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_259, v_261, (V)"fragile_crossing", (V)(R.Get(R.Get(v_260, (V)"position"), (V)"x") + (V)1.0d)}, new Results(R.Get(R.Get(v_260, (V)"position"), (V)"y")))));
V v_263 = values_262[0];
R.Call(v_18, R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_259, (V)"class_special"), (V)"snare"))));
R.Call(v_18, R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));

R.Call(v_18, R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_263, (V)"phase") == (V)"armed"), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_259))))).First))));
var values_264 = R.Join(new V[] {}, new Results((V)false));
V v_265 = values_264[0];
foreach (var row_266 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_267 = row_266[0];
V v_268 = row_266[1];
var values_269 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_259, (V)"sweeping_slash", R.Get(v_268, (V)1.0d)}, new Results(R.Get(v_268, (V)2.0d)))));
V v_270 = values_269[0];
if (R.Truth(R.And(R.Get(v_270, (V)"valid"), () => R.Or((V)((V)(R.Get(R.Get(v_260, (V)"position"), (V)"x") + R.Get(v_268, (V)1.0d)) != R.Get(v_263, (V)"x")), () => (V)((V)(R.Get(R.Get(v_260, (V)"position"), (V)"y") + R.Get(v_268, (V)2.0d)) != R.Get(v_263, (V)"y")))))) {
R.Call(v_18, R.Join(new V[] {v_259}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", R.Get(v_268, (V)1.0d)), new Entry((V)"dy", R.Get(v_268, (V)2.0d))))));

var values_271 = R.Join(new V[] {}, new Results((V)true));
v_265 = values_271[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_265, () => (V)(R.Get(v_263, (V)"phase") == (V)"closed")), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_259))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"malformed directions, phases, duplicate IDs, changed holes and mismatched heat work reject on restore"}, new Results(new V((RuleFunction)(args_272 => {

var values_321 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_273 => {

var values_274 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {(V)"push_floor", (V)8.0d, (V)6.0d, (V)1.0d}, new Results((V)0.0d))));
V v_275 = values_274[0];
V v_276 = values_274[1];
V v_277 = values_274[2];

var values_278 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_279 = v_277;
V key_280 = (V)"dx";
R.Set(obj_279, key_280, values_278[0]);

return R.Join(new V[] {}, new Results(v_275));
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_281 => {

var values_282 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results((V)"heat_vent"))));
V v_283 = values_282[0];
V v_284 = values_282[1];
V v_285 = values_282[2];

var values_286 = R.Join(new V[] {}, new Results((V)"fire"));
var obj_287 = v_285;
V key_288 = (V)"phase";
R.Set(obj_287, key_288, values_286[0]);

return R.Join(new V[] {}, new Results(v_283));
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_289 => {

var values_290 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results((V)"heat_vent"))));
V v_291 = values_290[0];
V v_292 = values_290[1];
V v_293 = values_290[2];

var values_294 = R.Join(new V[] {}, new Results(v_293));
var obj_295 = R.Get(R.Get(v_291, (V)"idea_terrain"), (V)"nodes");
V key_296 = (V)2.0d;
R.Set(obj_295, key_296, values_294[0]);

return R.Join(new V[] {}, new Results(v_291));
return Results.Empty;
}))), new Entry((V)4, new V((RuleFunction)(args_297 => {

var values_298 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results((V)"heat_vent"))));
V v_299 = values_298[0];
V v_300 = values_298[1];
V v_301 = values_298[2];

R.Call(v_18, R.Join(new V[] {v_299}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

var values_302 = R.Join(new V[] {}, new Results((V)(R.Get(v_301, (V)"due") + (V)1.0d)));
var obj_303 = v_301;
V key_304 = (V)"due";
R.Set(obj_303, key_304, values_302[0]);

return R.Join(new V[] {}, new Results(v_299));
return Results.Empty;
}))), new Entry((V)5, new V((RuleFunction)(args_305 => {

var values_306 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results((V)"heat_vent"))));
V v_307 = values_306[0];
V v_308 = values_306[1];
V v_309 = values_306[2];

var values_310 = R.Join(new V[] {}, new Results((V)99.0d));
var obj_311 = R.Get(v_307, (V)"idea_terrain");
V key_312 = (V)"version";
R.Set(obj_311, key_312, values_310[0]);

return R.Join(new V[] {}, new Results(v_307));
return Results.Empty;
}))), new Entry((V)6, new V((RuleFunction)(args_313 => {

var values_314 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results((V)"heat_vent"))));
V v_315 = values_314[0];
V v_316 = values_314[1];
V v_317 = values_314[2];

R.Call(v_18, R.Join(new V[] {v_315}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

var values_318 = R.Join(new V[] {}, new Results(V.Nil));
var obj_319 = v_315;
V key_320 = (V)"idea_terrain";
R.Set(obj_319, key_320, values_318[0]);

return R.Join(new V[] {}, new Results(v_315));
return Results.Empty;
}))))));
V v_322 = values_321[0];
foreach (var row_323 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_322)))))) {
V v_324 = row_323[0];
V v_325 = row_323[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(v_325, Results.Empty))))).First)))));
}
var values_326 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {(V)1.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_327 = values_326[0];

var values_328 = R.Join(new V[] {}, new Results(V.Nil));
var obj_329 = v_327;
V key_330 = (V)"idea_terrain";
R.Set(obj_329, key_330, values_328[0]);
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_327))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_327)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"optional terrain placement changes no combat RNG, entity, scheduler or geometry bytes"}, new Results(new V((RuleFunction)(args_331 => {

var values_332 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_333 = values_332[0];

var values_334 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"map_for"), R.Join(new V[] {}, new Results(v_333))));
V v_335 = values_334[0];

var values_336 = R.Join(new V[] {}, new Results(V.Nil));
var obj_337 = v_333;
V key_338 = (V)"idea_terrain";
R.Set(obj_337, key_338, values_336[0]);
var values_339 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rng", R.Get(v_333, (V)"rng")), new Entry((V)"entities", R.Get(v_333, (V)"entities")), new Entry((V)"schedule", R.Get(v_333, (V)"schedule")), new Entry((V)"map", v_335), new Entry((V)"clock", R.Get(v_333, (V)"clock")))))));
V v_340 = values_339[0];
R.Call(R.Get(v_7, (V)"populate"), R.Join(new V[] {v_333}, new Results(v_335)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rng", R.Get(v_333, (V)"rng")), new Entry((V)"entities", R.Get(v_333, (V)"entities")), new Entry((V)"schedule", R.Get(v_333, (V)"schedule")), new Entry((V)"map", v_335), new Entry((V)"clock", R.Get(v_333, (V)"clock")))))).First == v_340))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"seven-biome seed corpus preserves all routes after every finite shortcut and exact reopening"}, new Results(new V((RuleFunction)(args_341 => {

var values_342 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_343 = values_342[0];

var values_344 = R.Join(new V[] {}, new Results(R.Table()));
V v_345 = values_344[0];
foreach (var row_346 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_343, (V)"order"))))))) {
V v_347 = row_346[0];
V v_348 = row_346[1];
{ double end_351 = R.Num((V)4.0d), step_352 = R.Num((V)1.0d);
for (double i_350 = R.Num((V)1.0d); step_352 > 0 ? i_350 <= end_351 : i_350 >= end_351; i_350 += step_352) {
V v_349 = i_350;
var values_353 = R.Join(new V[] {}, new Results((V)(v_348 == R.Get(R.Get(v_343, (V)"order"), (V)1.0d))));
V v_354 = values_353[0];

var values_355 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"stage_options", R.Table(new Entry((V)"biome_id", v_348), new Entry((V)"index", R.Or(R.And(v_354, () => (V)1.0d), () => (V)2.0d)), new Entry((V)"route_history", R.Or(R.And(v_354, () => R.Table()), () => R.Table(new Entry(1, new Results(R.Invoke(v_348, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First))))))))));
V v_356 = values_355[0];
var values_357 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_349}, new Results(v_356))));
V v_358 = values_357[0];

var values_359 = R.Join(new V[] {}, new Results(R.Table()));
V v_360 = values_359[0];
foreach (var row_361 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_358, (V)"idea_terrain"), (V)"nodes"))))))) {
V v_362 = row_361[0];
V v_363 = row_361[1];
var values_364 = R.Join(new V[] {}, new Results((V)true));
var obj_365 = v_345;
V key_366 = R.Get(v_363, (V)"kind");
R.Set(obj_365, key_366, values_364[0]);

var values_367 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_360, R.Get(v_363, (V)"floor_id")), () => R.Table())));
var obj_368 = v_360;
V key_369 = R.Get(v_363, (V)"floor_id");
R.Set(obj_368, key_369, values_367[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(v_360, R.Get(v_363, (V)"floor_id")), R.Get(v_363, (V)"kind")))))));

var values_370 = R.Join(new V[] {}, new Results((V)true));
var obj_371 = R.Get(v_360, R.Get(v_363, (V)"floor_id"));
V key_372 = R.Get(v_363, (V)"kind");
R.Set(obj_371, key_372, values_370[0]);
var values_373 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"map"), R.Join(new V[] {v_358}, new Results(R.Get(v_363, (V)"floor_id")))));
V v_374 = values_373[0];
if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"fragile_crossing"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"alternate_route"), R.Join(new V[] {v_358, v_374, R.Get(v_363, (V)"x")}, new Results(R.Get(v_363, (V)"y"))))));
}
if (R.Truth((V)(R.Get(v_363, (V)"kind") == (V)"push_floor"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"alternate_route"), R.Join(new V[] {v_358, v_374, R.Get(v_363, (V)"x")}, new Results(R.Get(v_363, (V)"y"))))));
}
foreach (var row_375 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_374, (V)"rooms"))))))) {
V v_376 = row_375[0];
V v_377 = row_375[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_363, (V)"x") >= R.Get(v_377, (V)"x")), () => (V)(R.Get(v_363, (V)"x") < (V)(R.Get(v_377, (V)"x") + R.Get(v_377, (V)"width")))), () => (V)(R.Get(v_363, (V)"y") >= R.Get(v_377, (V)"y"))), () => (V)(R.Get(v_363, (V)"y") < (V)(R.Get(v_377, (V)"y") + R.Get(v_377, (V)"height")))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_377, (V)"role") != (V)"start"), () => (V)(R.Get(v_377, (V)"role") != (V)"elite")), () => (V)(R.Get(v_377, (V)"role") != (V)"exit")))));
}
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_358))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_358)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_349}, new Results(v_356))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_358)))));
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_345, (V)"heat_vent"), () => R.Get(v_345, (V)"push_floor")), () => R.Get(v_345, (V)"fragile_crossing")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"the fixed renderer resolves all new terrain and the permitted vent derivative preserves neutral pixels and alpha"}, new Results(new V((RuleFunction)(args_378 => {

var values_379 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))));
V v_380 = values_379[0];

var values_381 = R.Join(new V[] {}, R.Call(R.Get(v_380, (V)"collect"), Results.Empty));
V v_382 = values_381[0];
foreach (var row_383 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_382, (V)"packs"))))))) {
V v_384 = row_383[0];
V v_385 = row_383[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_385, (V)"missing")) == (V)0.0d))));
}
foreach (var row_386 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)0.0d), new Entry((V)4, (V)0.5d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.2d), new Entry((V)2, (V)0.3d), new Entry((V)3, (V)0.8d), new Entry((V)4, (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)0.0d), new Entry((V)4, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d), new Entry((V)4, (V)0.2d)))))))))) {
V v_387 = row_386[0];
V v_388 = row_386[1];
var values_389 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_monochrome"))).First, (V)"pixel"), R.Join(new V[] {}, R.Call(rt.Global("unpack"), R.Join(new V[] {}, new Results(v_388))))));
V v_390 = values_389[0];
V v_391 = values_389[1];
V v_392 = values_389[2];
V v_393 = values_389[3];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_390 == v_391), () => (V)(v_391 == v_392)), () => (V)(v_393 == R.Get(v_388, (V)4.0d))), () => R.Or((V)(v_390 == (V)0.07d), () => (V)(v_390 == (V)0.9d))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(R.Get(R.Get(R.Get(v_382, (V)"packs"), (V)"onebit"), (V)"usage"), (V)"tiny"), (V)"9"), () => R.Get(R.Get(R.Get(R.Get(R.Get(v_382, (V)"packs"), (V)"onebit"), (V)"usage"), (V)"tiny"), (V)"29")))));
var values_394 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_visuals"))));
V v_395 = values_394[0];

var values_396 = R.Join(new V[] {}, new Results(R.Table()));
V v_397 = values_396[0];
V v_398 = V.Nil;
v_398 = new V((RuleFunction)(args_399 => {
V v_400 = args_399[0];
var values_401 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {v_400}, new Results((V)"rb"))));
V v_402 = values_401[0];

if (R.Truth(v_402)) {
var values_403 = R.Join(new V[] {}, R.Invoke(v_402, "read", R.Join(new V[] {}, new Results((V)"*a"))));
V v_404 = values_403[0];

R.Invoke(v_402, "close", Results.Empty);

return R.Join(new V[] {}, new Results(v_404));
}
return Results.Empty;
}));
V v_405 = V.Nil;
v_405 = new V((RuleFunction)(args_406 => {
V v_407 = args_406[0];
V v_408 = args_406[1];
V v_409 = args_406[2];
var values_410 = R.Join(new V[] {}, new Results(v_409));
var obj_411 = v_397;
V key_412 = v_408;
R.Set(obj_411, key_412, values_410[0]);
V v_413 = V.Nil;
v_413 = new V((RuleFunction)(args_414 => {
V v_415 = args_414[0];
var values_416 = R.Join(new V[] {}, R.Invoke(v_407, "byte", R.Join(new V[] {v_415}, new Results((V)(v_415 + (V)3.0d)))));
V v_417 = values_416[0];
V v_418 = values_416[1];
V v_419 = values_416[2];
V v_420 = values_416[3];

return R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)((V)(v_417 * (V)256.0d) + v_418) * (V)256.0d) + v_419) * (V)256.0d) + v_420)));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_421 => {

return R.Join(new V[] {}, R.Call(v_413, R.Join(new V[] {}, new Results((V)17.0d))));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_422 => {

return R.Join(new V[] {}, R.Call(v_413, R.Join(new V[] {}, new Results((V)21.0d))));
return Results.Empty;
}))))));
return Results.Empty;
}));
V v_423 = V.Nil;
v_423 = new V((RuleFunction)(args_424 => {
V v_425 = args_424[0];
V v_426 = args_424[1];
V v_427 = args_424[2];
V v_428 = args_424[3];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_425), new Entry((V)"y", v_426), new Entry((V)"w", v_427), new Entry((V)"h", v_428))));
return Results.Empty;
}));
var values_429 = R.Join(new V[] {}, R.Call(R.Get(v_395, (V)"new"), R.Join(new V[] {v_398, v_405}, new Results(v_423))));
V v_430 = values_429[0];
var values_433 = R.Join(new V[] {}, R.Call(R.Get(v_395, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_431 => {
V v_432 = args_431[0];
if (R.Truth((V)(v_432 == R.Get(v_395, (V)"path")))) {
return R.Join(new V[] {}, R.Call(v_398, R.Join(new V[] {}, new Results(v_432))));
}
return Results.Empty;
})), v_405}, new Results(v_423))));
V v_434 = values_433[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_430, "resolve", R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"binding"), R.Join(new V[] {}, new Results((V)"heat_vent_warning"))))).First, (V)"approved_sheet") == (V)"tiny"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_397, (V)"assets/kenney/approved/tiny-dungeon.png") == (V)"approved-monochrome"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Invoke(v_434, "resolve", R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"binding"), R.Join(new V[] {}, new Results((V)"heat_vent_warning"))))).First, (V)"symbol_index") == (V)565.0d), () => (V)(R.Get(v_434, (V)"signature") != R.Get(v_430, (V)"signature"))))));
return Results.Empty;
})))));
V v_435 = V.Nil;
v_435 = new V((RuleFunction)(args_436 => {
V v_437 = args_436[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(rt.Global("_G"), (V)"love"))), () => (V)(!R.Truth(R.Get(rt.Global("_G"), (V)"Visibility")))))));
var values_438 = R.Join(new V[] {}, new Results(R.Table()));
V v_439 = values_438[0];

var values_440 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"love", v_439))}, new Results(R.Table(new Entry((V)"__index", rt.Global("_G")))))));
V v_441 = values_440[0];
var values_442 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("loadfile"), R.Join(new V[] {}, new Results((V)"main.lua"))))));
V v_443 = values_442[0];

R.Call(rt.Global("setfenv"), R.Join(new V[] {v_443}, new Results(v_441)));

R.Call(v_443, Results.Empty);
V v_444 = V.Nil;
v_444 = new V((RuleFunction)(args_445 => {
V v_446 = args_445[0];
V v_447 = args_445[1];
V v_448 = args_445[2];
{ double end_451 = R.Num((V)200.0d), step_452 = R.Num((V)1.0d);
for (double i_450 = R.Num((V)1.0d); step_452 > 0 ? i_450 <= end_451 : i_450 >= end_451; i_450 += step_452) {
V v_449 = i_450;
var values_453 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("debug"), (V)"getupvalue"), R.Join(new V[] {R.Get(v_439, (V)"keypressed")}, new Results(v_449))));
V v_454 = values_453[0];
V v_455 = values_453[1];

if (R.Truth((V)(!R.Truth(v_454)))) {
break;
}
if (R.Truth((V)(v_454 == v_446))) {
if (R.Truth(v_448)) {
R.Call(R.Get(rt.Global("debug"), (V)"setupvalue"), R.Join(new V[] {R.Get(v_439, (V)"keypressed"), v_449}, new Results(v_447)));

if (R.Truth(rt.Global("jit"))) {
R.Call(R.Get(rt.Global("jit"), (V)"flush"), Results.Empty);
}
}
return R.Join(new V[] {}, new Results(v_455));
}
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results(R.Concat((V)"callback upvalue missing: ", v_446))));
return Results.Empty;
}));
R.Call(v_444, R.Join(new V[] {(V)"state", v_437}, new Results((V)true)));

R.Call(v_444, R.Join(new V[] {(V)"title", (V)false}, new Results((V)true)));

R.Call(v_444, R.Join(new V[] {(V)"spectator_enabled", (V)false}, new Results((V)true)));
var values_456 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_457 = values_456[0];
V v_458 = V.Nil;
v_458 = new V((RuleFunction)(args_459 => {

R.Call(R.Get(v_457, (V)"update"), R.Join(new V[] {R.Call(v_444, R.Join(new V[] {}, new Results((V)"playback"))).First}, new Results((V)10.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_439, (V)"roagPlaybackPending"), Results.Empty).First)))));
return Results.Empty;
}));
return R.Join(new V[] {v_439, new V((RuleFunction)(args_460 => {

return R.Join(new V[] {}, R.Call(v_444, R.Join(new V[] {}, new Results((V)"notice"))));
return Results.Empty;
}))}, new Results(v_458));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"real keyboard wait reports a visible heat vent and preserves exact simulation continuation"}, new Results(new V((RuleFunction)(args_461 => {

var values_462 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {}, new Results((V)"heat_vent"))));
V v_463 = values_462[0];
V v_464 = values_462[1];
V v_465 = values_462[2];

var values_466 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_463))))));
V v_467 = values_466[0];
var values_468 = R.Join(new V[] {}, R.Call(v_435, R.Join(new V[] {}, new Results(v_463))));
V v_469 = values_468[0];
V v_470 = values_468[1];
V v_471 = values_468[2];
R.Call(v_18, R.Join(new V[] {v_467}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

R.Call(R.Get(v_469, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"space")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_465, (V)"phase") == (V)"warning"), () => (V)(R.Get(v_463, (V)"clock") == (V)100.0d)), () => R.Invoke(R.Call(v_470, Results.Empty).First, "find", R.Join(new V[] {(V)"Heat vent warning", (V)1.0d}, new Results((V)true))).First))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_463))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_467)))));

R.Call(v_471, Results.Empty);
R.Call(v_18, R.Join(new V[] {v_467}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))));

R.Call(R.Get(v_469, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"a")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_465, (V)"phase") == (V)"cooldown"), () => (V)(R.Get(v_463, (V)"clock") == (V)200.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_463))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_467)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_463))))))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_463)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real keyboard crossing reports a fragile collapse without a global visibility dependency"}, new Results(new V((RuleFunction)(args_472 => {

var values_473 = R.Join(new V[] {}, R.Call(v_52, Results.Empty));
V v_474 = values_473[0];
V v_475 = values_473[1];
V v_476 = values_473[2];

var values_477 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"place"), R.Join(new V[] {v_474, v_476, (V)"fragile_crossing", (V)(R.Get(R.Get(v_475, (V)"position"), (V)"x") + (V)1.0d)}, new Results(R.Get(R.Get(v_475, (V)"position"), (V)"y")))));
V v_478 = values_477[0];
var values_479 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_474))))));
V v_480 = values_479[0];

var values_481 = R.Join(new V[] {}, R.Call(v_435, R.Join(new V[] {}, new Results(v_474))));
V v_482 = values_481[0];
V v_483 = values_481[1];
V v_484 = values_481[2];
R.Call(v_18, R.Join(new V[] {v_480}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));

R.Call(R.Get(v_482, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"d")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_478, (V)"phase") == (V)"armed"), () => R.Invoke(R.Call(v_483, Results.Empty).First, "find", R.Join(new V[] {(V)"Fragile shortcut armed", (V)1.0d}, new Results((V)true))).First))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_474))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_480)))));

R.Call(v_484, Results.Empty);
R.Call(v_18, R.Join(new V[] {v_480}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));

R.Call(R.Get(v_482, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"d")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_478, (V)"phase") == (V)"closed"), () => R.Invoke(R.Call(v_483, Results.Empty).First, "find", R.Join(new V[] {(V)"Fragile shortcut closed", (V)1.0d}, new Results((V)true))).First))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_474))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_480)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_474))))))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_474)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
