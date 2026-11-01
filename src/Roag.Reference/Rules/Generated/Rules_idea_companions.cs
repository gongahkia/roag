// Static C# port of src/idea_companions.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 22b481d8086555d41e2d383fac6f9230f9932f2455cdf5f0b822a01294ca26f7
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_companions { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_mirrors"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"spirit_id", (V)"core:actor/spiritbinder"), new Entry((V)"controller", (V)"idea_companion"), new Entry((V)"spirit_object", (V)"core:object/bound_spirit"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(v_6, (V)"spirit_object"), (V)417.0d), new Entry((V)"core:ui/storm_charge_effect", (V)615.0d))));
var obj_8 = v_6;
V key_9 = (V)"symbols";
R.Set(obj_8, key_9, values_7[0]);
var values_10 = R.Join(new V[] {}, new Results((V)"core:actor/tinkerer"));
var obj_11 = v_6;
V key_12 = (V)"tinker_id";
R.Set(obj_11, key_12, values_10[0]);

var values_13 = R.Join(new V[] {}, new Results((V)"core:object/bolt_sentry"));
var obj_14 = v_6;
V key_15 = (V)"sentry_object";
R.Set(obj_14, key_15, values_13[0]);
var values_16 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_6, (V)"tinker_id")), new Entry((V)"name", (V)"Tinkerer"), new Entry((V)"weapon_icon", (V)"bow"), new Entry((V)"description", (V)"Hand bolt, one three-charge sentry, utility roll"), new Entry((V)"hp", (V)23.0d), new Entry((V)"damage", (V)3.0d), new Entry((V)"slash_damage", (V)0.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)95.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)100.0d), new Entry((V)"sweeping_slash", (V)160.0d), new Entry((V)"dash", (V)120.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/tinkerer_bolt"), new Entry((V)"special_id", (V)"core:ability/tinkerer_deploy"), new Entry((V)"basic_name", (V)"Hand Bolt"), new Entry((V)"special_name", (V)"Deploy Sentry"), new Entry((V)"dash_name", (V)"Utility Roll"), new Entry((V)"ui_controls", (V)"F Bolt \u00c2\u00b7 R Sentry (3 bolts) \u00c2\u00b7 X Roll"))));
var obj_17 = v_6;
V key_18 = (V)"tinker";
R.Set(obj_17, key_18, values_16[0]);
var values_19 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_6, (V)"spirit_id")), new Entry((V)"name", (V)"Spiritbinder"), new Entry((V)"weapon_icon", (V)"spell"), new Entry((V)"description", (V)"Tether bolt, one commanded spirit, short spirit step"), new Entry((V)"hp", (V)20.0d), new Entry((V)"damage", (V)3.0d), new Entry((V)"slash_damage", (V)3.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)100.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)110.0d), new Entry((V)"sweeping_slash", (V)140.0d), new Entry((V)"dash", (V)120.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/spiritbinder_bolt"), new Entry((V)"special_id", (V)"core:ability/spiritbinder_command"), new Entry((V)"basic_name", (V)"Tether Bolt"), new Entry((V)"special_name", (V)"Command Spirit"), new Entry((V)"dash_name", (V)"Spirit Step"), new Entry((V)"ui_controls", (V)"F Bolt \u00c2\u00b7 R Spirit / command \u00c2\u00b7 X Step"))));
var obj_20 = v_6;
V key_21 = (V)"spirit";
R.Set(obj_20, key_21, values_19[0]);
R.Set(v_6, (V)"owned", new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
V v_24 = args_22[1];
foreach (var row_25 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"entities"))))))) {
V v_26 = row_25[0];
V v_27 = row_25[1];
if (R.Truth(R.And((V)(R.Get(v_27, (V)"controller") == R.Get(v_6, (V)"controller")), () => (V)(R.Get(v_27, (V)"companion_kind") == v_24)))) {
return R.Join(new V[] {}, new Results(v_27));
}
}
return Results.Empty;
})));
V v_28 = V.Nil;
v_28 = new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
V v_32 = args_29[2];
V v_33 = args_29[3];
foreach (var row_34 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_30, (V)"entities"))))))) {
V v_35 = row_34[0];
V v_36 = row_34[1];
var values_37 = R.Join(new V[] {}, new Results(R.Get(v_36, (V)"position")));
V v_38 = values_37[0];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_38, (V)"floor_id") == v_31), () => (V)(R.Get(v_38, (V)"x") == v_32)), () => (V)(R.Get(v_38, (V)"y") == v_33)), () => R.Or(R.Get(v_36, (V)"hp"), () => R.Get(v_36, (V)"blocks_movement"))))) {
return R.Join(new V[] {}, new Results(v_36));
}
}
return Results.Empty;
}));
R.Set(v_6, (V)"cells", new V((RuleFunction)(args_39 => {
V v_40 = args_39[0];
V v_41 = args_39[1];
V v_42 = args_39[2];
V v_43 = args_39[3];
V v_44 = args_39[4];
var values_45 = R.Join(new V[] {}, new Results(R.Get(v_41, (V)"position")));
V v_46 = values_45[0];

var values_47 = R.Join(new V[] {}, R.Call(R.Get(v_44, (V)"map_for"), R.Join(new V[] {}, new Results(v_40))));
V v_48 = values_47[0];

var values_49 = Results.Empty;
V v_50 = values_49[0];
if (R.Truth((V)(R.Get(v_41, (V)"definition_id") == R.Get(v_6, (V)"tinker_id")))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)(R.Get(v_46, (V)"x") + v_42)), new Entry((V)"y", (V)(R.Get(v_46, (V)"y") + v_43)))))));
}
{ double end_53 = R.Num((V)3.0d), step_54 = R.Num((V)1.0d);
for (double i_52 = R.Num((V)1.0d); step_54 > 0 ? i_52 <= end_53 : i_52 >= end_53; i_52 += step_54) {
V v_51 = i_52;
var values_55 = R.Join(new V[] {(V)(R.Get(v_46, (V)"x") + (V)(v_42 * v_51))}, new Results((V)(R.Get(v_46, (V)"y") + (V)(v_43 * v_51))));
V v_56 = values_55[0];
V v_57 = values_55[1];
if (R.Truth(R.And(R.And((V)(v_42 != (V)0.0d), () => (V)(v_43 != (V)0.0d)), () => R.Or((V)(!R.Truth(R.Call(R.Get(v_44, (V)"projectile_passable"), R.Join(new V[] {v_48, (V)(v_56 - v_42), v_57}, new Results(v_40))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_44, (V)"projectile_passable"), R.Join(new V[] {v_48, v_56, (V)(v_57 - v_43)}, new Results(v_40))).First)))))) {
break;
}
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_44, (V)"projectile_passable"), R.Join(new V[] {v_48, v_56, v_57}, new Results(v_40))).First)))) {
break;
}
var values_58 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {v_40, R.Get(v_46, (V)"floor_id"), v_56}, new Results(v_57))));
V v_59 = values_58[0];
if (R.Truth(v_59)) {
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_6, (V)"owned"), R.Join(new V[] {v_40}, new Results((V)"spirit"))).First)))) {
return R.Join(new V[] {}, new Results(R.Or(R.And(v_50, () => R.Table(new Entry((V)1, v_50))), () => R.Table())));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", v_56), new Entry((V)"y", v_57))))));
}
if (R.Truth((V)(v_51 <= (V)2.0d))) {
var values_60 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_56), new Entry((V)"y", v_57))));
v_50 = values_60[0];
}
}
}
return R.Join(new V[] {}, new Results(R.Or(R.And(v_50, () => R.Table(new Entry((V)1, v_50))), () => R.Table())));
return Results.Empty;
})));
R.Set(v_6, (V)"preview", new V((RuleFunction)(args_61 => {
V v_62 = args_61[0];
V v_63 = args_61[1];
V v_64 = args_61[2];
V v_65 = args_61[3];
V v_66 = args_61[4];
var values_67 = R.Join(new V[] {}, new Results(R.Get(v_64, (V)1.0d)));
V v_68 = values_67[0];

var values_69 = R.Join(new V[] {}, new Results(R.Get(v_63, (V)"position")));
V v_70 = values_69[0];

var values_71 = R.Join(new V[] {}, R.Call(R.Get(v_65, (V)"map_for"), R.Join(new V[] {}, new Results(v_62))));
V v_72 = values_71[0];
if (R.Truth(R.Or((V)(!R.Truth(v_68)), () => (V)(!R.Truth(R.Call(v_66, R.Join(new V[] {v_62, R.Get(v_70, (V)"floor_id"), R.Get(v_68, (V)"x")}, new Results(R.Get(v_68, (V)"y")))).First))))) {
return R.Join(new V[] {(V)false}, new Results((V)"choose visible nearby ground"));
}
var values_73 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {v_62, R.Get(v_70, (V)"floor_id"), R.Get(v_68, (V)"x")}, new Results(R.Get(v_68, (V)"y")))));
V v_74 = values_73[0];
if (R.Truth(R.And((V)(R.Get(v_63, (V)"definition_id") == R.Get(v_6, (V)"tinker_id")), () => R.Or(v_74, () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_68, (V)"x") - R.Get(v_70, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_68, (V)"y") - R.Get(v_70, (V)"y"))))).First) != (V)1.0d))))) {
return R.Join(new V[] {(V)false}, new Results((V)"sentry needs empty adjacent cardinal floor"));
}
if (R.Truth(v_74)) {
var values_75 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"owned"), R.Join(new V[] {v_62}, new Results((V)"spirit"))));
V v_76 = values_75[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_74, (V)"faction") != (V)"enemy"), () => (V)(!R.Truth(R.Get(v_74, (V)"hp")))), () => (V)(!R.Truth(v_76))), () => (V)(R.Get(R.Get(v_76, (V)"position"), (V)"floor_id") != R.Get(v_70, (V)"floor_id"))), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_68, (V)"x") - R.Get(R.Get(v_76, (V)"position"), (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_68, (V)"y") - R.Get(R.Get(v_76, (V)"position"), (V)"y"))))))).First != (V)1.0d)))) {
return R.Join(new V[] {(V)false}, new Results((V)"place spirit beside the target first"));
}
var values_77 = R.Join(new V[] {}, new Results(R.Get(v_76, (V)"position")));
V v_78 = values_77[0];
if (R.Truth(R.And(R.And((V)(R.Get(v_78, (V)"x") != R.Get(v_68, (V)"x")), () => (V)(R.Get(v_78, (V)"y") != R.Get(v_68, (V)"y"))), () => R.Or((V)(!R.Truth(R.Call(R.Get(v_65, (V)"projectile_passable"), R.Join(new V[] {v_72, R.Get(v_78, (V)"x"), R.Get(v_68, (V)"y")}, new Results(v_62))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_65, (V)"projectile_passable"), R.Join(new V[] {v_72, R.Get(v_68, (V)"x"), R.Get(v_78, (V)"y")}, new Results(v_62))).First)))))) {
return R.Join(new V[] {(V)false}, new Results((V)"spirit strike corner blocked"));
}
return R.Join(new V[] {(V)true, V.Nil, (V)"strike"}, new Results(v_76));
}
if (R.Truth(R.Or((V)(!R.Truth(R.Call(R.Get(v_65, (V)"walkable"), R.Join(new V[] {v_72, R.Get(v_68, (V)"x")}, new Results(R.Get(v_68, (V)"y")))).First)), () => (V)(R.Invoke(R.Get(R.Get(v_72, (V)"rows"), R.Get(v_68, (V)"y")), "sub", R.Join(new V[] {R.Get(v_68, (V)"x")}, new Results(R.Get(v_68, (V)"x")))).First != (V)".")))) {
return R.Join(new V[] {(V)false}, new Results((V)"needs plain floor"));
}
foreach (var row_79 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_72, (V)"placements"), () => R.Get(R.Get(v_72, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_80 = row_79[0];
V v_81 = row_79[1];
if (R.Truth(R.And((V)(R.Get(v_81, (V)"x") == R.Get(v_68, (V)"x")), () => (V)(R.Get(v_81, (V)"y") == R.Get(v_68, (V)"y"))))) {
return R.Join(new V[] {(V)false}, new Results((V)"stairs must stay clear"));
}
}
return R.Join(new V[] {(V)true, V.Nil}, new Results((V)"place"));
return Results.Empty;
})));
R.Set(v_6, (V)"place", new V((RuleFunction)(args_82 => {
V v_83 = args_82[0];
V v_84 = args_82[1];
V v_85 = args_82[2];
V v_86 = args_82[3];
var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"owned"), R.Join(new V[] {v_83}, new Results((V)"spirit"))));
V v_88 = values_87[0];
if (R.Truth((V)(!R.Truth(v_88)))) {
var values_89 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_83, (V)"next_entity_id")), new Entry((V)"definition_id", R.Get(v_6, (V)"spirit_object")), new Entry((V)"controller", R.Get(v_6, (V)"controller")), new Entry((V)"companion_kind", (V)"spirit"), new Entry((V)"owner_id", R.Get(v_84, (V)"id")), new Entry((V)"faction", (V)"player"), new Entry((V)"boons", R.Table()), new Entry((V)"blocks_movement", (V)false), new Entry((V)"blocks_vision", (V)false), new Entry((V)"blocks_projectiles", (V)false))));
v_88 = values_89[0];
var values_90 = R.Join(new V[] {}, new Results((V)(R.Get(v_83, (V)"next_entity_id") + (V)1.0d)));
var obj_91 = v_83;
V key_92 = (V)"next_entity_id";
R.Set(obj_91, key_92, values_90[0]);

var values_93 = R.Join(new V[] {}, new Results(v_88));
var obj_94 = R.Get(v_83, (V)"entities");
V key_95 = (V)((V)R.Length(R.Get(v_83, (V)"entities")) + (V)1.0d);
R.Set(obj_94, key_95, values_93[0]);
}
var values_96 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_84, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(v_85, (V)"x")), new Entry((V)"y", R.Get(v_85, (V)"y")))));
var obj_97 = v_88;
V key_98 = (V)"position";
R.Set(obj_97, key_98, values_96[0]);
R.Call(v_86, R.Join(new V[] {(V)"SpiritPositioned"}, new Results(R.Table(new Entry((V)"target_id", R.Get(v_88, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_88, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(v_85, (V)"x")), new Entry((V)"y", R.Get(v_85, (V)"y"))))));
return R.Join(new V[] {}, new Results(v_88));
return Results.Empty;
})));
V v_99 = V.Nil;
v_99 = new V((RuleFunction)(args_100 => {
V v_101 = args_100[0];
V v_102 = args_100[1];
V v_103 = args_100[2];
V v_104 = args_100[3];
V v_105 = args_100[4];
var values_106 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"enqueue"), R.Join(new V[] {v_101}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", v_104), new Entry((V)"actor_id", (V)0.0d), new Entry((V)"attack_id", v_105), new Entry((V)"floor_id", R.Get(R.Get(v_102, (V)"position"), (V)"floor_id")))))));
V v_107 = values_106[0];
var values_108 = R.Join(new V[] {}, new Results(R.Get(v_107, (V)"id")));
var obj_109 = v_107;
V key_110 = (V)"effect_id";
R.Set(obj_109, key_110, values_108[0]);

var values_111 = R.Join(new V[] {}, new Results((V)true));
var obj_112 = v_107;
V key_113 = (V)"idea_sentry";
R.Set(obj_112, key_113, values_111[0]);
var values_114 = R.Join(new V[] {v_103, v_104}, new Results(R.Get(v_107, (V)"id")));
var obj_115 = v_102;
V key_116 = (V)"sentry_phase";
var obj_117 = v_102;
V key_118 = (V)"sentry_due";
var obj_119 = v_102;
V key_120 = (V)"sentry_schedule_id";
R.Set(obj_115, key_116, values_114[0]);
R.Set(obj_117, key_118, values_114[1]);
R.Set(obj_119, key_120, values_114[2]);
return Results.Empty;
}));
R.Set(v_6, (V)"deploy", new V((RuleFunction)(args_121 => {
V v_122 = args_121[0];
V v_123 = args_121[1];
V v_124 = args_121[2];
V v_125 = args_121[3];
V v_126 = args_121[4];
var values_127 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"owned"), R.Join(new V[] {v_122}, new Results((V)"sentry"))));
V v_128 = values_127[0];
if (R.Truth(v_128)) {
foreach (var row_129 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_122, (V)"schedule"))))))))) {
V v_130 = row_129[0];
V v_131 = row_129[1];
{ double end_134 = R.Num((V)1.0d), step_135 = R.Num((-(V)1.0d));
for (double i_133 = R.Num((V)R.Length(v_131)); step_135 > 0 ? i_133 <= end_134 : i_133 >= end_134; i_133 += step_135) {
V v_132 = i_133;
if (R.Truth((V)(R.Get(R.Get(v_131, v_132), (V)"id") == R.Get(v_128, (V)"sentry_schedule_id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {v_131}, new Results(v_132)));
}
}
}
}
foreach (var row_136 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_122, (V)"floor_pause"), () => R.Table()))))))) {
V v_137 = row_136[0];
V v_138 = row_136[1];
{ double end_141 = R.Num((V)1.0d), step_142 = R.Num((-(V)1.0d));
for (double i_140 = R.Num((V)R.Length(R.Get(v_138, (V)"schedule"))); step_142 > 0 ? i_140 <= end_141 : i_140 >= end_141; i_140 += step_142) {
V v_139 = i_140;
if (R.Truth((V)(R.Get(R.Get(R.Get(v_138, (V)"schedule"), v_139), (V)"id") == R.Get(v_128, (V)"sentry_schedule_id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_138, (V)"schedule")}, new Results(v_139)));
}
}
}
}
foreach (var row_143 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_122, (V)"entities"))))))) {
V v_144 = row_143[0];
V v_145 = row_143[1];
if (R.Truth((V)(R.Get(v_145, (V)"id") == R.Get(v_128, (V)"id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_122, (V)"entities")}, new Results(v_144)));

break;
}
}
}
var values_146 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_122, (V)"next_entity_id")), new Entry((V)"definition_id", R.Get(v_6, (V)"sentry_object")), new Entry((V)"controller", R.Get(v_6, (V)"controller")), new Entry((V)"companion_kind", (V)"sentry"), new Entry((V)"owner_id", R.Get(v_123, (V)"id")), new Entry((V)"faction", (V)"player"), new Entry((V)"boons", R.Table()), new Entry((V)"blocks_movement", (V)false), new Entry((V)"blocks_vision", (V)false), new Entry((V)"blocks_projectiles", (V)false), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_123, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(v_124, (V)"x")), new Entry((V)"y", R.Get(v_124, (V)"y")))), new Entry((V)"sentry_shots", (V)3.0d))));
V v_147 = values_146[0];
var values_148 = R.Join(new V[] {}, new Results((V)(R.Get(v_147, (V)"id") + (V)1.0d)));
var obj_149 = v_122;
V key_150 = (V)"next_entity_id";
R.Set(obj_149, key_150, values_148[0]);

var values_151 = R.Join(new V[] {}, new Results(v_147));
var obj_152 = R.Get(v_122, (V)"entities");
V key_153 = (V)((V)R.Length(R.Get(v_122, (V)"entities")) + (V)1.0d);
R.Set(obj_152, key_153, values_151[0]);
R.Call(v_99, R.Join(new V[] {v_122, v_147, (V)"commit", (V)(R.Get(v_122, (V)"clock") + (V)100.0d)}, new Results(v_125)));
R.Call(v_126, R.Join(new V[] {(V)"SentryDeployed"}, new Results(R.Table(new Entry((V)"target_id", R.Get(v_147, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_147, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(v_124, (V)"x")), new Entry((V)"y", R.Get(v_124, (V)"y")), new Entry((V)"shots", (V)3.0d)))));
return R.Join(new V[] {}, new Results(v_147));
return Results.Empty;
})));
R.Set(v_6, (V)"resolve", new V((RuleFunction)(args_154 => {
V v_155 = args_154[0];
V v_156 = args_154[1];
V v_157 = args_154[2];
V v_158 = args_154[3];
V v_159 = args_154[4];
V v_160 = args_154[5];
var values_161 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"owned"), R.Join(new V[] {v_155}, new Results((V)"sentry"))));
V v_162 = values_161[0];

R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_162, () => (V)(R.Get(v_162, (V)"sentry_schedule_id") == R.Get(v_156, (V)"id")))}, new Results((V)"orphan sentry work")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(R.Get(v_162, (V)"position"), (V)"floor_id") == R.Get(v_155, (V)"active_floor_id")), () => (V)(R.Get(v_162, (V)"sentry_due") == R.Get(v_155, (V)"clock")))}, new Results((V)"mistimed sentry work")));
if (R.Truth((V)(R.Get(v_162, (V)"sentry_phase") == (V)"impact"))) {
var values_163 = R.Join(new V[] {}, new Results(R.Get(v_162, (V)"sentry_cells")));
V v_164 = values_163[0];
if (R.Truth(R.Call(v_158, R.Join(new V[] {v_155, R.Get(R.Get(v_162, (V)"position"), (V)"floor_id"), R.Get(R.Get(v_162, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_162, (V)"position"), (V)"y")))).First)) {
var values_165 = R.Join(new V[] {}, new Results((V)true));
V v_166 = values_165[0];

foreach (var row_167 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_164)))))) {
V v_168 = row_167[0];
V v_169 = row_167[1];
if (R.Truth((V)(!R.Truth(R.Call(v_158, R.Join(new V[] {v_155, R.Get(v_169, (V)"floor_id"), R.Get(v_169, (V)"x")}, new Results(R.Get(v_169, (V)"y")))).First)))) {
var values_170 = R.Join(new V[] {}, new Results((V)false));
v_166 = values_170[0];

break;
}
}
if (R.Truth(v_166)) {
R.Call(v_160, R.Join(new V[] {v_162, v_164}, new Results(R.Get(v_156, (V)"attack_id"))));
}
}
var values_171 = R.Join(new V[] {}, new Results(V.Nil));
var obj_172 = v_162;
V key_173 = (V)"sentry_cells";
R.Set(obj_172, key_173, values_171[0]);

var values_174 = R.Join(new V[] {}, new Results(V.Nil));
var obj_175 = v_162;
V key_176 = (V)"sentry_reflection";
R.Set(obj_175, key_176, values_174[0]);
if (R.Truth((V)(R.Get(v_162, (V)"sentry_shots") > (V)0.0d))) {
R.Call(v_99, R.Join(new V[] {v_155, v_162, (V)"commit", (V)(R.Get(v_155, (V)"clock") + (V)120.0d)}, new Results(R.Get(v_156, (V)"attack_id"))));
} else {
var values_177 = R.Join(new V[] {}, new Results(V.Nil));
var obj_178 = v_162;
V key_179 = (V)"sentry_phase";
R.Set(obj_178, key_179, values_177[0]);

var values_180 = R.Join(new V[] {}, new Results(V.Nil));
var obj_181 = v_162;
V key_182 = (V)"sentry_due";
R.Set(obj_181, key_182, values_180[0]);

var values_183 = R.Join(new V[] {}, new Results(V.Nil));
var obj_184 = v_162;
V key_185 = (V)"sentry_schedule_id";
R.Set(obj_184, key_185, values_183[0]);
}
} else {
var values_186 = R.Join(new V[] {}, new Results((V)(R.Get(v_162, (V)"sentry_shots") - (V)1.0d)));
var obj_187 = v_162;
V key_188 = (V)"sentry_shots";
R.Set(obj_187, key_188, values_186[0]);
var values_189 = Results.Empty;
V v_190 = values_189[0];
V v_191 = values_189[1];
if (R.Truth(R.Call(v_158, R.Join(new V[] {v_155, R.Get(R.Get(v_162, (V)"position"), (V)"floor_id"), R.Get(R.Get(v_162, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_162, (V)"position"), (V)"y")))).First)) {
foreach (var row_192 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_155, (V)"entities"))))))) {
V v_193 = row_192[0];
V v_194 = row_192[1];
var values_195 = R.Join(new V[] {}, new Results(R.Get(v_194, (V)"position")));
V v_196 = values_195[0];

var values_197 = R.Join(new V[] {}, new Results(R.Get(v_162, (V)"position")));
V v_198 = values_197[0];
var values_199 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_196, (V)"x") - R.Get(v_198, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_196, (V)"y") - R.Get(v_198, (V)"y"))))).First)));
V v_200 = values_199[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_194, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_194, (V)"hp") > (V)0.0d)), () => (V)(!R.Truth(R.Get(v_194, (V)"invulnerable")))), () => (V)(R.Get(v_196, (V)"floor_id") == R.Get(v_198, (V)"floor_id"))), () => R.Or((V)(R.Get(v_196, (V)"x") == R.Get(v_198, (V)"x")), () => (V)(R.Get(v_196, (V)"y") == R.Get(v_198, (V)"y")))), () => (V)(v_200 >= (V)1.0d)), () => (V)(v_200 <= (V)4.0d)), () => R.Call(v_158, R.Join(new V[] {v_155, R.Get(v_196, (V)"floor_id"), R.Get(v_196, (V)"x")}, new Results(R.Get(v_196, (V)"y")))).First))) {
var values_201 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_196, (V)"x") == R.Get(v_198, (V)"x")), () => (V)0.0d), () => R.Or(R.And((V)(R.Get(v_196, (V)"x") > R.Get(v_198, (V)"x")), () => (V)1.0d), () => (-(V)1.0d)))));
V v_202 = values_201[0];

var values_203 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_196, (V)"y") == R.Get(v_198, (V)"y")), () => (V)0.0d), () => R.Or(R.And((V)(R.Get(v_196, (V)"y") > R.Get(v_198, (V)"y")), () => (V)1.0d), () => (-(V)1.0d)))));
V v_204 = values_203[0];
var values_205 = R.Join(new V[] {}, new Results((V)true));
V v_206 = values_205[0];
{ double end_209 = R.Num(v_200), step_210 = R.Num((V)1.0d);
for (double i_208 = R.Num((V)1.0d); step_210 > 0 ? i_208 <= end_209 : i_208 >= end_209; i_208 += step_210) {
V v_207 = i_208;
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_157, (V)"projectile_passable"), R.Join(new V[] {R.Call(R.Get(v_157, (V)"map_for"), R.Join(new V[] {}, new Results(v_155))).First, (V)(R.Get(v_198, (V)"x") + (V)(v_202 * v_207)), (V)(R.Get(v_198, (V)"y") + (V)(v_204 * v_207))}, new Results(v_155))).First)))) {
var values_211 = R.Join(new V[] {}, new Results((V)false));
v_206 = values_211[0];

break;
}
}
}
if (R.Truth(R.And(v_206, () => R.Or(R.Or((V)(!R.Truth(v_190)), () => (V)(v_200 < v_191)), () => R.And((V)(v_200 == v_191), () => (V)(R.Get(v_194, (V)"id") < R.Get(v_190, (V)"id"))))))) {
var values_212 = R.Join(new V[] {v_194}, new Results(v_200));
v_190 = values_212[0];
v_191 = values_212[1];
}
}
}
}
if (R.Truth(v_190)) {
var values_213 = R.Join(new V[] {R.Get(v_162, (V)"position")}, new Results(R.Get(v_190, (V)"position")));
V v_214 = values_213[0];
V v_215 = values_213[1];
var values_216 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_215, (V)"x") == R.Get(v_214, (V)"x")), () => (V)0.0d), () => R.Or(R.And((V)(R.Get(v_215, (V)"x") > R.Get(v_214, (V)"x")), () => (V)1.0d), () => (-(V)1.0d)))));
V v_217 = values_216[0];

var values_218 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_215, (V)"y") == R.Get(v_214, (V)"y")), () => (V)0.0d), () => R.Or(R.And((V)(R.Get(v_215, (V)"y") > R.Get(v_214, (V)"y")), () => (V)1.0d), () => (-(V)1.0d)))));
V v_219 = values_218[0];
var values_220 = R.Join(new V[] {}, new Results(R.Table()));
var obj_221 = v_162;
V key_222 = (V)"sentry_cells";
R.Set(obj_221, key_222, values_220[0]);
{ double end_225 = R.Num(v_191), step_226 = R.Num((V)1.0d);
for (double i_224 = R.Num((V)1.0d); step_226 > 0 ? i_224 <= end_225 : i_224 >= end_225; i_224 += step_226) {
V v_223 = i_224;
var values_227 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_214, (V)"floor_id")), new Entry((V)"x", (V)(R.Get(v_214, (V)"x") + (V)(v_217 * v_223))), new Entry((V)"y", (V)(R.Get(v_214, (V)"y") + (V)(v_219 * v_223))))));
var obj_228 = R.Get(v_162, (V)"sentry_cells");
V key_229 = (V)((V)R.Length(R.Get(v_162, (V)"sentry_cells")) + (V)1.0d);
R.Set(obj_228, key_229, values_227[0]);
}
}
var values_230 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"route"), R.Join(new V[] {v_155, v_162, R.Get(v_162, (V)"sentry_cells")}, new Results((V)true))));
V v_231 = values_230[0];
V v_232 = values_230[1];
var values_233 = R.Join(new V[] {}, new Results(v_231));
var obj_234 = v_162;
V key_235 = (V)"sentry_cells";
R.Set(obj_234, key_235, values_233[0]);

var values_236 = R.Join(new V[] {}, new Results(v_232));
var obj_237 = v_162;
V key_238 = (V)"sentry_reflection";
R.Set(obj_237, key_238, values_236[0]);
if (R.Truth(v_232)) {
R.Call(R.Get(v_4, (V)"consume"), R.Join(new V[] {v_155}, new Results(v_232)));
R.Call(v_159, R.Join(new V[] {(V)"MirrorReflected"}, new Results(R.Table(new Entry((V)"reflection", v_232), new Entry((V)"target_cells", v_231), new Entry((V)"floor_id", R.Get(v_214, (V)"floor_id")), new Entry((V)"x", R.Get(v_232, (V)"x")), new Entry((V)"y", R.Get(v_232, (V)"y"))))));
}
R.Call(v_99, R.Join(new V[] {v_155, v_162, (V)"impact", (V)(R.Get(v_155, (V)"clock") + (V)80.0d)}, new Results(R.Get(v_156, (V)"attack_id"))));
R.Call(v_159, R.Join(new V[] {(V)"SentryCommitted"}, new Results(R.Table(new Entry((V)"target_id", R.Get(v_190, (V)"id")), new Entry((V)"floor_id", R.Get(v_214, (V)"floor_id")), new Entry((V)"x", R.Get(v_214, (V)"x")), new Entry((V)"y", R.Get(v_214, (V)"y")), new Entry((V)"target_cells", R.Get(v_162, (V)"sentry_cells")), new Entry((V)"due", R.Get(v_162, (V)"sentry_due")), new Entry((V)"shots", R.Get(v_162, (V)"sentry_shots"))))));
} else if (R.Truth((V)(R.Get(v_162, (V)"sentry_shots") > (V)0.0d))) {
R.Call(v_159, R.Join(new V[] {(V)"SentryChargeSpent"}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_162, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_162, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_162, (V)"position"), (V)"y")), new Entry((V)"shots", R.Get(v_162, (V)"sentry_shots"))))));
R.Call(v_99, R.Join(new V[] {v_155, v_162, (V)"commit", (V)(R.Get(v_155, (V)"clock") + (V)200.0d)}, new Results(R.Get(v_156, (V)"attack_id"))));
} else {
var values_239 = R.Join(new V[] {}, new Results(V.Nil));
var obj_240 = v_162;
V key_241 = (V)"sentry_phase";
R.Set(obj_240, key_241, values_239[0]);

var values_242 = R.Join(new V[] {}, new Results(V.Nil));
var obj_243 = v_162;
V key_244 = (V)"sentry_due";
R.Set(obj_243, key_244, values_242[0]);

var values_245 = R.Join(new V[] {}, new Results(V.Nil));
var obj_246 = v_162;
V key_247 = (V)"sentry_schedule_id";
R.Set(obj_246, key_247, values_245[0]);
}
}
return Results.Empty;
})));
R.Set(v_6, (V)"validate", new V((RuleFunction)(args_248 => {
V v_249 = args_248[0];
V v_250 = args_248[1];
var values_251 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_252 = values_251[0];
V v_253 = values_251[1];
var values_254 = R.Join(new V[] {}, new Results(R.Table()));
V v_255 = values_254[0];

foreach (var row_256 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_249, (V)"schedule"))))))) {
V v_257 = row_256[0];
V v_258 = row_256[1];
var values_259 = R.Join(new V[] {}, new Results(v_258));
var obj_260 = v_255;
V key_261 = (V)((V)R.Length(v_255) + (V)1.0d);
R.Set(obj_260, key_261, values_259[0]);
}
foreach (var row_262 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_249, (V)"floor_pause"), () => R.Table()))))))) {
V v_263 = row_262[0];
V v_264 = row_262[1];
foreach (var row_265 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_264, (V)"schedule"))))))) {
V v_266 = row_265[0];
V v_267 = row_265[1];
var values_268 = R.Join(new V[] {}, new Results(v_267));
var obj_269 = v_255;
V key_270 = (V)((V)R.Length(v_255) + (V)1.0d);
R.Set(obj_269, key_270, values_268[0]);
}
}
foreach (var row_271 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_249, (V)"entities"))))))) {
V v_272 = row_271[0];
V v_273 = row_271[1];
if (R.Truth((V)(R.Get(v_273, (V)"controller") == R.Get(v_6, (V)"controller")))) {
var values_274 = Results.Empty;
V v_275 = values_274[0];

foreach (var row_276 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_249, (V)"entities"))))))) {
V v_277 = row_276[0];
V v_278 = row_276[1];
if (R.Truth((V)(R.Get(v_278, (V)"id") == R.Get(v_273, (V)"owner_id")))) {
var values_279 = R.Join(new V[] {}, new Results(v_278));
v_275 = values_279[0];

break;
}
}
var values_280 = R.Join(new V[] {}, new Results(R.Get(v_273, (V)"companion_kind")));
V v_281 = values_280[0];

var values_282 = R.Join(new V[] {}, new Results((V)(v_281 == (V)"spirit")));
V v_283 = values_282[0];

var values_284 = R.Join(new V[] {}, new Results((V)(v_281 == (V)"sentry")));
V v_285 = values_284[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.Or(v_283, () => v_285), () => (V)(!R.Truth(R.Get(v_252, v_281)))), () => v_275), () => (V)(R.Get(v_275, (V)"id") == R.Get(v_249, (V)"player_id"))), () => (V)(R.Get(v_275, (V)"definition_id") == R.Or(R.And(v_283, () => R.Get(v_6, (V)"spirit_id")), () => R.Get(v_6, (V)"tinker_id")))), () => (V)(R.Get(v_273, (V)"definition_id") == R.Or(R.And(v_283, () => R.Get(v_6, (V)"spirit_object")), () => R.Get(v_6, (V)"sentry_object")))), () => (V)(R.Get(v_273, (V)"hp") == V.Nil)), () => (V)(R.Get(v_273, (V)"faction") == (V)"player")), () => (V)(R.Get(v_273, (V)"blocks_movement") == (V)false)), () => (V)(R.Get(v_273, (V)"blocks_vision") == (V)false)), () => (V)(R.Get(v_273, (V)"blocks_projectiles") == (V)false))}, new Results((V)"invalid owned companion")));
var values_286 = R.Join(new V[] {}, new Results(R.Get(v_250, R.Get(R.Get(v_273, (V)"position"), (V)"floor_id"))));
V v_287 = values_286[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(v_287, () => R.Get(R.Get(v_287, (V)"rows"), R.Get(R.Get(v_273, (V)"position"), (V)"y"))), () => (V)(R.Invoke(R.Get(R.Get(v_287, (V)"rows"), R.Get(R.Get(v_273, (V)"position"), (V)"y")), "sub", R.Join(new V[] {R.Get(R.Get(v_273, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_273, (V)"position"), (V)"x")))).First == (V)"."))}, new Results((V)"invalid companion ground")));
if (R.Truth(v_285)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_273, (V)"sentry_shots")))).First == (V)"number"), () => (V)((V)(R.Get(v_273, (V)"sentry_shots") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_273, (V)"sentry_shots") >= (V)0.0d)), () => (V)(R.Get(v_273, (V)"sentry_shots") <= (V)3.0d))}, new Results((V)"invalid sentry charges")));
if (R.Truth(R.Get(v_273, (V)"sentry_phase"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.And((V)(R.Get(v_273, (V)"sentry_phase") == (V)"commit"), () => (V)(R.Get(v_273, (V)"sentry_shots") > (V)0.0d)), () => (V)(R.Get(v_273, (V)"sentry_phase") == (V)"impact"))}, new Results((V)"invalid sentry phase")));
var values_288 = R.Join(new V[] {}, new Results((V)false));
V v_289 = values_288[0];

var values_290 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Or(R.Get(R.Or(R.Get(v_249, (V)"floor_pause"), () => R.Table()), R.Get(R.Get(v_273, (V)"position"), (V)"floor_id")), () => R.Table()), (V)"suspended_at"), () => R.Get(v_249, (V)"clock"))));
V v_291 = values_290[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_273, (V)"sentry_due")))).First == (V)"number"), () => (V)(R.Get(v_273, (V)"sentry_due") >= v_291)), () => (V)(R.Get(v_273, (V)"sentry_due") <= (V)(v_291 + (V)200.0d)))}, new Results((V)"invalid sentry due")));
foreach (var row_292 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_255)))))) {
V v_293 = row_292[0];
V v_294 = row_292[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_294, (V)"id") == R.Get(v_273, (V)"sentry_schedule_id")), () => (V)(R.Get(v_294, (V)"kind") == (V)"effect")), () => (V)(R.Get(v_294, (V)"idea_sentry") == (V)true)), () => (V)(R.Get(v_294, (V)"actor_id") == (V)0.0d)), () => (V)(R.Get(v_294, (V)"effect_id") == R.Get(v_294, (V)"id"))), () => (V)(R.Get(v_294, (V)"floor_id") == R.Get(R.Get(v_273, (V)"position"), (V)"floor_id"))), () => (V)(R.Get(v_294, (V)"due") == R.Get(v_273, (V)"sentry_due"))))) {
var values_295 = R.Join(new V[] {}, new Results((V)true));
v_289 = values_295[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_289}, new Results((V)"missing sentry work")));

var values_296 = R.Join(new V[] {}, new Results((V)true));
var obj_297 = v_253;
V key_298 = R.Get(v_273, (V)"sentry_schedule_id");
R.Set(obj_297, key_298, values_296[0]);
if (R.Truth((V)(R.Get(v_273, (V)"sentry_phase") == (V)"impact"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_273, (V)"sentry_cells")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_273, (V)"sentry_cells")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_273, (V)"sentry_cells")) <= (V)4.0d))}, new Results((V)"invalid sentry lane")));
var values_299 = Results.Empty;
V v_300 = values_299[0];
V v_301 = values_299[1];

var values_302 = R.Join(new V[] {}, new Results(R.Get(v_273, (V)"sentry_reflection")));
V v_303 = values_302[0];
if (R.Truth(v_303)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_303, (V)"id")))).First == (V)"number"), () => (V)((V)(R.Get(v_303, (V)"id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_303, (V)"id") >= (V)1.0d)), () => (V)(R.Get(v_303, (V)"id") < R.Get(v_249, (V)"next_action_id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_303, (V)"source_id")))).First == (V)"number")), () => (V)((V)(R.Get(v_303, (V)"source_id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_303, (V)"source_id") >= (V)1.0d)), () => (V)(R.Get(v_303, (V)"source_id") < R.Get(v_249, (V)"next_entity_id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_303, (V)"step")))).First == (V)"number")), () => (V)((V)(R.Get(v_303, (V)"step") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_303, (V)"step") >= (V)1.0d)), () => (V)(R.Get(v_303, (V)"step") < (V)R.Length(R.Get(v_273, (V)"sentry_cells")))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_303, (V)"in_dx")))).First == (V)"number")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_303, (V)"in_dy")))).First == (V)"number")), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_303, (V)"in_dx")))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_303, (V)"in_dy")))).First) == (V)1.0d)), () => (V)(R.Get(v_303, (V)"out_dx") == (-R.Get(v_303, (V)"in_dy")))), () => (V)(R.Get(v_303, (V)"out_dy") == R.Get(v_303, (V)"in_dx")))}, new Results((V)"invalid sentry reflection")));
}
foreach (var row_304 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_273, (V)"sentry_cells"))))))) {
V v_305 = row_304[0];
V v_306 = row_304[1];
var values_307 = R.Join(new V[] {(V)(R.Get(v_306, (V)"x") - R.Get(R.Get(v_273, (V)"position"), (V)"x"))}, new Results((V)(R.Get(v_306, (V)"y") - R.Get(R.Get(v_273, (V)"position"), (V)"y"))));
V v_308 = values_307[0];
V v_309 = values_307[1];
if (R.Truth((V)(v_305 == (V)1.0d))) {
var values_310 = R.Join(new V[] {v_308}, new Results(v_309));
v_300 = values_310[0];
v_301 = values_310[1];

R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_300))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_301))).First) == (V)1.0d)}, new Results((V)"invalid sentry direction")));
}
var values_311 = R.Join(new V[] {(V)(R.Get(R.Get(v_273, (V)"position"), (V)"x") + (V)(v_300 * v_305))}, new Results((V)(R.Get(R.Get(v_273, (V)"position"), (V)"y") + (V)(v_301 * v_305))));
V v_312 = values_311[0];
V v_313 = values_311[1];
if (R.Truth(v_303)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(v_300 == R.Get(v_303, (V)"in_dx")), () => (V)(v_301 == R.Get(v_303, (V)"in_dy")))}, new Results((V)"unlocked sentry reflection")));
if (R.Truth((V)(v_305 == R.Get(v_303, (V)"step")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_306, (V)"x") == R.Get(v_303, (V)"x")), () => (V)(R.Get(v_306, (V)"y") == R.Get(v_303, (V)"y")))}, new Results((V)"moved sentry mirror")));
}
if (R.Truth((V)(v_305 > R.Get(v_303, (V)"step")))) {
var values_314 = R.Join(new V[] {(V)(R.Get(v_303, (V)"x") + (V)(R.Get(v_303, (V)"out_dx") * (V)(v_305 - R.Get(v_303, (V)"step"))))}, new Results((V)(R.Get(v_303, (V)"y") + (V)(R.Get(v_303, (V)"out_dy") * (V)(v_305 - R.Get(v_303, (V)"step"))))));
v_312 = values_314[0];
v_313 = values_314[1];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_306, (V)"floor_id") == R.Get(R.Get(v_273, (V)"position"), (V)"floor_id")), () => (V)(R.Get(v_306, (V)"x") == v_312)), () => (V)(R.Get(v_306, (V)"y") == v_313)), () => (V)(R.Get(v_306, (V)"x") >= (V)1.0d)), () => (V)(R.Get(v_306, (V)"x") <= R.Get(v_287, (V)"width"))), () => (V)(R.Get(v_306, (V)"y") >= (V)1.0d)), () => (V)(R.Get(v_306, (V)"y") <= R.Get(v_287, (V)"height")))}, new Results((V)"unlocked sentry lane")));
}
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_273, (V)"sentry_cells") == V.Nil), () => (V)(R.Get(v_273, (V)"sentry_reflection") == V.Nil))}, new Results((V)"uncommitted sentry lane")));
}
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Get(v_273, (V)"sentry_shots") == (V)0.0d), () => (V)(!R.Truth(R.Get(v_273, (V)"sentry_due")))), () => (V)(!R.Truth(R.Get(v_273, (V)"sentry_schedule_id")))), () => (V)(!R.Truth(R.Get(v_273, (V)"sentry_cells")))), () => (V)(!R.Truth(R.Get(v_273, (V)"sentry_reflection"))))}, new Results((V)"invalid spent sentry")));
}
}
var values_315 = R.Join(new V[] {}, new Results((V)true));
var obj_316 = v_252;
V key_317 = v_281;
R.Set(obj_316, key_317, values_315[0]);
}
}
foreach (var row_318 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_255)))))) {
V v_319 = row_318[0];
V v_320 = row_318[1];
if (R.Truth(R.Get(v_320, (V)"idea_sentry"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_320, (V)"idea_sentry") == (V)true), () => R.Get(v_253, R.Get(v_320, (V)"id")))}, new Results((V)"orphan sentry work")));
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
