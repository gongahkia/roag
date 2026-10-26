// Static C# port of src/boons.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: b4b5970123b5498fcf857452a8ea395c4e2ef4417b2be10ea412e1e442d47bcb
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_boons { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_patterns"))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, new Results(R.Table()));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, new Results((V)1000000.0d));
V v_20 = values_19[0];
var values_21 = R.Join(new V[] {}, new Results(R.Table()));
V v_22 = values_21[0];
var values_23 = R.Join(new V[] {}, new Results(R.Table()));
V v_24 = values_23[0];
foreach (var row_25 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_26 = row_25[0];
V v_27 = row_25[1];
foreach (var row_28 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_27))).First, (V)"triggers"))))))) {
V v_29 = row_28[0];
V v_30 = row_28[1];
var values_31 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_22, v_30), () => R.Table())));
V v_32 = values_31[0];
var values_33 = R.Join(new V[] {}, new Results(v_27));
var obj_34 = v_32;
V key_35 = (V)((V)R.Length(v_32) + (V)1.0d);
R.Set(obj_34, key_35, values_33[0]);
var values_36 = R.Join(new V[] {}, new Results(v_32));
var obj_37 = v_22;
V key_38 = v_30;
R.Set(obj_37, key_38, values_36[0]);
}
foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_27))).First, (V)"modifiers"), () => R.Table()))))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
var values_42 = R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_41, (V)"kind") == (V)"weapon_form"), () => R.Get(R.Get(v_16, (V)"weapon_forms"), R.Get(v_41, (V)"form")))));
V v_43 = values_42[0];
if (R.Truth(R.And(v_43, () => R.Get(v_43, (V)"route_first")))) {
var values_44 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_24, R.Get(v_41, (V)"ability_id")), () => R.Table())));
V v_45 = values_44[0];
var values_46 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"boon_id", v_27), new Entry((V)"mode", R.Get(v_43, (V)"route_first")), new Entry((V)"explicit_only", R.Get(v_43, (V)"explicit_only")))));
var obj_47 = v_45;
V key_48 = (V)((V)R.Length(v_45) + (V)1.0d);
R.Set(obj_47, key_48, values_46[0]);
var values_49 = R.Join(new V[] {}, new Results(v_45));
var obj_50 = v_24;
V key_51 = R.Get(v_41, (V)"ability_id");
R.Set(obj_50, key_51, values_49[0]);
}
}
}
V v_52 = V.Nil;
v_52 = new V((RuleFunction)(args_53 => {
V v_54 = args_53[0];
V v_55 = args_53[1];
foreach (var row_56 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_54, (V)"entities"))))))) {
V v_57 = row_56[0];
V v_58 = row_56[1];
if (R.Truth((V)(R.Get(v_58, (V)"id") == v_55))) {
return R.Join(new V[] {}, new Results(v_58));
}
}
return Results.Empty;
}));
V v_59 = V.Nil;
v_59 = new V((RuleFunction)(args_60 => {
V v_61 = args_60[0];
V v_62 = args_60[1];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_61, (V)"x") - R.Get(v_62, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_61, (V)"y") - R.Get(v_62, (V)"y"))))).First)));
return Results.Empty;
}));
V v_63 = V.Nil;
v_63 = new V((RuleFunction)(args_64 => {
V v_65 = args_64[0];
if (R.Truth(R.And(R.Get(v_65, (V)"x"), () => R.Get(v_65, (V)"y")))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_65, (V)"floor_id")), new Entry((V)"x", R.Get(v_65, (V)"x")), new Entry((V)"y", R.Get(v_65, (V)"y")))));
}
if (R.Truth(R.And(R.Get(v_65, (V)"from_x"), () => R.Get(v_65, (V)"from_y")))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_65, (V)"floor_id")), new Entry((V)"x", R.Get(v_65, (V)"from_x")), new Entry((V)"y", R.Get(v_65, (V)"from_y")))));
}
return R.Join(new V[] {}, new Results(R.Get(v_65, (V)"source_position")));
return Results.Empty;
}));
V v_66 = V.Nil;
v_66 = new V((RuleFunction)(args_67 => {
V v_68 = args_67[0];
V v_69 = args_67[1];
V v_70 = args_67[2];
if (R.Truth((V)(R.Get(v_69, (V)"kind") == (V)"owner"))) {
return R.Join(new V[] {}, new Results(R.Or((V)(R.Get(v_70, (V)"source_id") == R.Get(v_68, (V)"id")), () => (V)(R.Get(v_70, (V)"target_id") == R.Get(v_68, (V)"id")))));
}
var values_71 = R.Join(new V[] {}, R.Call(v_63, R.Join(new V[] {}, new Results(v_70))));
V v_72 = values_71[0];
if (R.Truth(R.Or((V)(!R.Truth(v_72)), () => (V)(R.Get(v_72, (V)"floor_id") != R.Get(R.Get(v_68, (V)"position"), (V)"floor_id"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth((V)(R.Get(v_69, (V)"kind") == (V)"floor"))) {
return R.Join(new V[] {}, new Results((V)true));
}
return R.Join(new V[] {}, new Results((V)(R.Call(v_59, R.Join(new V[] {R.Get(v_68, (V)"position")}, new Results(v_72))).First <= R.Get(v_69, (V)"radius"))));
return Results.Empty;
}));
V v_73 = V.Nil;
v_73 = new V((RuleFunction)(args_74 => {
V v_75 = args_74[0];
V v_76 = args_74[1];
return R.Join(new V[] {}, new Results(R.And((V)(v_76 != V.Nil), () => (V)(v_76 != R.Get(v_75, (V)"faction")))));
return Results.Empty;
}));
R.Set(v_18, (V)"backstop", new V((RuleFunction)(args_77 => {
V v_78 = args_77[0];
V v_79 = args_77[1];
V v_80 = args_77[2];
var values_81 = R.Join(new V[] {}, new Results(R.Get(v_79, (V)"position")));
V v_82 = values_81[0];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_16, (V)"axis"), R.Join(new V[] {v_80, (V)"cardinal"}, new Results(v_82))).First)))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_83 = R.Join(new V[] {(V)(R.Get(v_80, (V)"x") - R.Get(v_82, (V)"x"))}, new Results((V)(R.Get(v_80, (V)"y") - R.Get(v_82, (V)"y"))));
V v_84 = values_83[0];
V v_85 = values_83[1];
var values_86 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_84 == (V)0.0d), () => (V)0.0d), () => R.And((V)(v_84 > (V)0.0d), () => (V)1.0d)), () => (-(V)1.0d))));
v_84 = values_86[0];
var values_87 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_85 == (V)0.0d), () => (V)0.0d), () => R.And((V)(v_85 > (V)0.0d), () => (V)1.0d)), () => (-(V)1.0d))));
v_85 = values_87[0];
return R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"map_for"), R.Join(new V[] {}, new Results(v_78))).First, (V)(R.Get(v_80, (V)"x") + v_84), (V)(R.Get(v_80, (V)"y") + v_85)}, new Results(v_78))).First))));
return Results.Empty;
})));
V v_88 = V.Nil;
v_88 = new V((RuleFunction)(args_89 => {
V v_90 = args_89[0];
V v_91 = args_89[1];
if (R.Truth((V)(v_90 > v_91))) {
var values_92 = R.Join(new V[] {v_91}, new Results(v_90));
v_90 = values_92[0];
v_91 = values_92[1];
}
return R.Join(new V[] {}, new Results(R.Concat(v_90, R.Concat((V)":", v_91))));
return Results.Empty;
}));
V v_93 = V.Nil;
v_93 = new V((RuleFunction)(args_94 => {
V v_95 = args_94[0];
V v_96 = args_94[1];
V v_97 = args_94[2];
V v_98 = args_94[3];
V v_99 = args_94[4];
V v_100 = args_94[5];
var values_101 = R.Join(new V[] {}, new Results(R.Table()));
V v_102 = values_101[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(R.Get(v_98, (V)"target_id"))), () => (V)(!R.Truth(R.Get(v_98, (V)"x")))), () => (V)(!R.Truth(R.Get(v_98, (V)"y")))))) {
return R.Join(new V[] {}, new Results(v_102));
}
var values_103 = R.Join(new V[] {}, new Results(R.Get(v_99, (V)"pairs")));
V v_104 = values_103[0];
foreach (var row_105 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_99, (V)"damages"))))))) {
V v_106 = row_105[0];
V v_107 = row_105[1];
var values_108 = R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_107, (V)"x") - R.Get(v_98, (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_107, (V)"y") - R.Get(v_98, (V)"y"))))));
V v_109 = values_108[0];
V v_110 = values_108[1];
var values_111 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_100 == (V)"cardinal"), () => (V)((V)(v_109 + v_110) == (V)1.0d)), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_109}, new Results(v_110))).First == (V)1.0d))));
V v_112 = values_111[0];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_107, (V)"target_id") != R.Get(v_98, (V)"target_id")), () => (V)(R.Get(v_107, (V)"floor_id") == R.Get(v_98, (V)"floor_id"))), () => R.Get(v_107, (V)"target_faction")), () => R.Call(v_73, R.Join(new V[] {v_96}, new Results(R.Get(v_107, (V)"target_faction")))).First), () => v_112))) {
var values_113 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_96, (V)"id"), R.Concat((V)":", R.Concat(v_97, R.Concat((V)":", R.Call(v_88, R.Join(new V[] {R.Get(v_107, (V)"target_id")}, new Results(R.Get(v_98, (V)"target_id")))).First))))));
V v_114 = values_113[0];
if (R.Truth((V)(!R.Truth(R.Get(v_104, v_114))))) {
var values_115 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"key", v_114), new Entry((V)"prior", v_107))));
var obj_116 = v_102;
V key_117 = (V)((V)R.Length(v_102) + (V)1.0d);
R.Set(obj_116, key_117, values_115[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_102));
return Results.Empty;
}));
V v_118 = V.Nil;
v_118 = new V((RuleFunction)(args_119 => {
V v_120 = args_119[0];
V v_121 = args_119[1];
V v_122 = args_119[2];
V v_123 = args_119[3];
V v_124 = args_119[4];
V v_125 = args_119[5];
if (R.Truth(R.Get(v_120, (V)"all"))) {
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_120, (V)"all"))))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
if (R.Truth((V)(!R.Truth(R.Call(v_118, R.Join(new V[] {v_128, v_121, v_122, v_123, v_124}, new Results(v_125))).First)))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
}
if (R.Truth(R.Get(v_120, (V)"any"))) {
foreach (var row_129 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_120, (V)"any"))))))) {
V v_130 = row_129[0];
V v_131 = row_129[1];
if (R.Truth(R.Call(v_118, R.Join(new V[] {v_131, v_121, v_122, v_123, v_124}, new Results(v_125))).First)) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth(R.Get(v_120, (V)"not_"))) {
return R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(v_118, R.Join(new V[] {R.Get(v_120, (V)"not_"), v_121, v_122, v_123, v_124}, new Results(v_125))).First))));
}
var values_132 = R.Join(new V[] {}, new Results(R.Get(v_120, (V)"op")));
V v_133 = values_132[0];
if (R.Truth((V)(v_133 == (V)"on_special_terrain"))) {
var values_134 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))).First, (V)"at"), R.Join(new V[] {v_121, R.Get(v_124, (V)"floor_id"), R.Get(v_124, (V)"x")}, new Results(R.Get(v_124, (V)"y")))));
V v_135 = values_134[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(v_135, () => (V)(R.Get(v_135, (V)"kind") == R.Get(v_120, (V)"value"))), () => (V)false)));
}
if (R.Truth((V)(v_133 == (V)"event_status"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"status") == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"target_entity_hostile"))) {
var values_136 = R.Join(new V[] {}, R.Call(v_52, R.Join(new V[] {v_121}, new Results(R.Get(v_124, (V)"target_id")))));
V v_137 = values_136[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.And(v_137, () => R.Get(v_137, (V)"hp")), () => (V)(R.Get(v_137, (V)"hp") > (V)0.0d)), () => R.Call(v_73, R.Join(new V[] {v_122}, new Results(R.Get(v_137, (V)"faction")))).First), () => (V)false)));
}
if (R.Truth((V)(v_133 == (V)"back_to_wall"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"wall_backed") == (V)true)));
}
if (R.Truth((V)(v_133 == (V)"owner_axis"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_16, (V)"axis"), R.Join(new V[] {v_124, R.Get(v_120, (V)"value")}, new Results(R.Get(v_122, (V)"position")))));
}
if (R.Truth((V)(v_133 == (V)"regular_target"))) {
var values_138 = R.Join(new V[] {}, R.Call(v_52, R.Join(new V[] {v_121}, new Results(R.Get(v_124, (V)"target_id")))));
V v_139 = values_138[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(v_139, () => R.Call(R.Get(v_18, (V)"skull_target"), R.Join(new V[] {}, new Results(v_139))).First), () => (V)false)));
}
if (R.Truth((V)(v_133 == (V)"basic_attack"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"ability_id") == R.Get(R.Call(R.Get(v_10, (V)"for_player"), R.Join(new V[] {}, new Results(v_122))).First, (V)"basic_id"))));
}
if (R.Truth((V)(v_133 == (V)"burning_target"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"target_burning") == (V)true)));
}
if (R.Truth((V)(v_133 == (V)"owner_missing_hp"))) {
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.Get(v_122, (V)"hp"), () => (V)(R.Get(v_122, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_122, (V)"hp") < R.Get(v_122, (V)"max_hp"))), () => (V)false)));
}
if (R.Truth((V)(v_133 == (V)"active_ward"))) {
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_18, (V)"ward_total"), R.Join(new V[] {v_121}, new Results(v_122))).First > (V)0.0d)));
}
if (R.Truth((V)(v_133 == (V)"cooldown_ready"))) {
var values_140 = R.Join(new V[] {}, new Results(R.And(R.Get(v_122, (V)"boon_memory"), () => R.Get(R.Get(v_122, (V)"boon_memory"), v_123))));
V v_141 = values_140[0];
return R.Join(new V[] {}, new Results(R.Or(R.Or((V)(!R.Truth(v_141)), () => (V)(!R.Truth(R.Get(v_141, (V)"expires_at")))), () => (V)(R.Get(v_121, (V)"clock") >= R.Get(v_141, (V)"expires_at")))));
}
if (R.Truth((V)(v_133 == (V)"pattern_axis"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_16, (V)"axis"), R.Join(new V[] {v_124}, new Results(R.Get(v_120, (V)"value")))));
}
if (R.Truth((V)(v_133 == (V)"actual_dash"))) {
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.And((V)(R.Get(v_124, (V)"kind") == (V)"Moved"), () => (V)(R.Get(v_124, (V)"ability_id") == (V)"core:ability/dash")), () => R.Get(v_121, (V)"last_action")), () => (V)(R.Get(R.Get(v_121, (V)"last_action"), (V)"kind") == (V)"dash")), () => (V)false)));
}
if (R.Truth((V)(v_133 == (V)"roll_face"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"fate_face") == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"unique_action"))) {
if (R.Truth((V)(!R.Truth(R.Get(v_124, (V)"root_action_id"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_142 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_125, (V)"actions"), () => R.Table())));
var obj_143 = v_125;
V key_144 = (V)"actions";
R.Set(obj_143, key_144, values_142[0]);
var values_145 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_122, (V)"id"), R.Concat((V)":", R.Concat(v_123, R.Concat((V)":", R.Get(v_124, (V)"root_action_id")))))));
V v_146 = values_145[0];
if (R.Truth(R.Get(R.Get(v_125, (V)"actions"), v_146))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_147 = R.Join(new V[] {}, new Results((V)true));
var obj_148 = R.Get(v_125, (V)"actions");
V key_149 = v_146;
R.Set(obj_148, key_149, values_147[0]);

return R.Join(new V[] {}, new Results((V)true));
}
if (R.Truth((V)(v_133 == (V)"supply_kind"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"supply_kind") == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"supply_excess"))) {
return R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_124, (V)"amount"), () => (V)0.0d) > R.Or(R.Get(v_124, (V)"actual_gain"), () => (V)0.0d))));
}
if (R.Truth((V)(v_133 == (V)"event_amount"))) {
return R.Join(new V[] {}, new Results(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_124, R.Get(v_120, (V)"field"))))).First == (V)"number"), () => (V)(R.Get(v_124, R.Get(v_120, (V)"field")) >= R.Get(v_120, (V)"min")))));
}
if (R.Truth((V)(v_133 == (V)"rewarded_drop"))) {
var values_150 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_124, (V)"target_definition_id")))));
V v_151 = values_150[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And((V)(!R.Truth(R.Get(v_124, (V)"reward_exempt"))), () => v_151), () => (V)(R.Get(v_151, (V)"currency_drop") > (V)0.0d)), () => (V)false)));
}
if (R.Truth((V)(v_133 == (V)"event_kind"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"kind") == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"visible_source"))) {
var values_152 = R.Join(new V[] {}, new Results(R.Get(v_124, (V)"source_position")));
V v_153 = values_152[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(v_153, () => R.Call(R.Get(v_12, (V)"is_visible"), R.Join(new V[] {v_121, R.Get(v_153, (V)"floor_id"), R.Get(v_153, (V)"x")}, new Results(R.Get(v_153, (V)"y")))).First), () => (V)false)));
}
if (R.Truth((V)(v_133 == (V)"visible_event"))) {
var values_154 = R.Join(new V[] {}, R.Call(v_63, R.Join(new V[] {}, new Results(v_124))));
V v_155 = values_154[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(v_155, () => R.Call(R.Get(v_12, (V)"is_visible"), R.Join(new V[] {v_121, R.Get(v_155, (V)"floor_id"), R.Get(v_155, (V)"x")}, new Results(R.Get(v_155, (V)"y")))).First), () => (V)false)));
}
if (R.Truth((V)(v_133 == (V)"terrain_id"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"terrain_id") == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"damage_type"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"damage_type") == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"from_terrain"))) {
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"symbol"), R.Join(new V[] {R.Call(R.Get(v_14, (V)"map"), R.Join(new V[] {v_121}, new Results(R.Get(v_124, (V)"floor_id")))).First, R.Or(R.Get(v_124, (V)"from_x"), () => (-(V)1.0d))}, new Results(R.Or(R.Get(v_124, (V)"from_y"), () => (-(V)1.0d))))).First == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"path_terrain"))) {
var values_156 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"map"), R.Join(new V[] {v_121}, new Results(R.Get(v_124, (V)"floor_id")))));
V v_157 = values_156[0];
foreach (var row_158 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_124, (V)"path"), () => R.Table()))))))) {
V v_159 = row_158[0];
V v_160 = row_158[1];
if (R.Truth(R.And((V)(R.Get(v_160, (V)"floor_id") == R.Get(R.Get(v_122, (V)"position"), (V)"floor_id")), () => (V)(R.Call(R.Get(v_8, (V)"symbol"), R.Join(new V[] {v_157, R.Get(v_160, (V)"x")}, new Results(R.Get(v_160, (V)"y")))).First == R.Get(v_120, (V)"value"))))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth((V)(v_133 == (V)"source_on_terrain"))) {
var values_161 = R.Join(new V[] {}, new Results(R.Get(v_124, (V)"source_position")));
V v_162 = values_161[0];
return R.Join(new V[] {}, new Results(R.And(R.And(v_162, () => (V)(R.Get(v_162, (V)"floor_id") == R.Get(R.Get(v_122, (V)"position"), (V)"floor_id"))), () => (V)(R.Call(R.Get(v_8, (V)"symbol"), R.Join(new V[] {R.Call(R.Get(v_14, (V)"map"), R.Join(new V[] {v_121}, new Results(R.Get(v_162, (V)"floor_id")))).First, R.Get(v_162, (V)"x")}, new Results(R.Get(v_162, (V)"y")))).First == R.Get(v_120, (V)"value")))));
}
if (R.Truth((V)(v_133 == (V)"target_near_terrain"))) {
if (R.Truth(R.Or(R.Or((V)(!R.Truth(R.Get(v_124, (V)"x"))), () => (V)(!R.Truth(R.Get(v_124, (V)"y")))), () => (V)(R.Get(v_124, (V)"floor_id") != R.Get(R.Get(v_122, (V)"position"), (V)"floor_id"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_163 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"map"), R.Join(new V[] {v_121}, new Results(R.Get(v_124, (V)"floor_id")))));
V v_164 = values_163[0];
{ double end_167 = R.Num((V)(R.Get(v_124, (V)"y") + (V)1.0d)), step_168 = R.Num((V)1.0d);
for (double i_166 = R.Num((V)(R.Get(v_124, (V)"y") - (V)1.0d)); step_168 > 0 ? i_166 <= end_167 : i_166 >= end_167; i_166 += step_168) {
V v_165 = i_166;
{ double end_171 = R.Num((V)(R.Get(v_124, (V)"x") + (V)1.0d)), step_172 = R.Num((V)1.0d);
for (double i_170 = R.Num((V)(R.Get(v_124, (V)"x") - (V)1.0d)); step_172 > 0 ? i_170 <= end_171 : i_170 >= end_171; i_170 += step_172) {
V v_169 = i_170;
if (R.Truth((V)(R.Call(R.Get(v_8, (V)"symbol"), R.Join(new V[] {v_164, v_169}, new Results(v_165))).First == R.Get(v_120, (V)"value")))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
}
}
}
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth((V)(v_133 == (V)"primed"))) {
var values_173 = R.Join(new V[] {}, new Results(R.And(R.Get(v_122, (V)"boon_memory"), () => R.Get(R.Get(v_122, (V)"boon_memory"), v_123))));
V v_174 = values_173[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(v_174, () => (V)(R.Or(R.Get(v_174, (V)"charges"), () => (V)0.0d) > (V)0.0d)), () => (V)(R.Or(R.Get(v_174, (V)"expires_at"), () => (-(V)1.0d)) >= R.Get(v_121, (V)"clock"))), () => (V)false)));
}
if (R.Truth((V)(v_133 == (V)"special_attack"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"ability_id") == R.Get(R.Call(R.Get(v_10, (V)"for_player"), R.Join(new V[] {}, new Results(v_122))).First, (V)"special_id"))));
}
if (R.Truth((V)(v_133 == (V)"target_hp_percent_before"))) {
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.Get(v_124, (V)"hp_before"), () => R.Get(v_124, (V)"target_max_hp")), () => (V)((V)(R.Get(v_124, (V)"hp_before") * (V)100.0d) <= (V)(R.Get(v_124, (V)"target_max_hp") * R.Get(v_120, (V)"max")))), () => (V)false)));
}
if (R.Truth((V)(v_133 == (V)"owner_in_locked_cells"))) {
foreach (var row_175 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_124, (V)"target_cells"), () => R.Table()))))))) {
V v_176 = row_175[0];
V v_177 = row_175[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_177, (V)"floor_id") == R.Get(R.Get(v_122, (V)"position"), (V)"floor_id")), () => (V)(R.Get(v_177, (V)"x") == R.Get(R.Get(v_122, (V)"position"), (V)"x"))), () => (V)(R.Get(v_177, (V)"y") == R.Get(R.Get(v_122, (V)"position"), (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth((V)(v_133 == (V)"overlapping_windups"))) {
foreach (var row_178 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_121, (V)"entities"))))))) {
V v_179 = row_178[0];
V v_180 = row_178[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_180, (V)"id") != R.Get(v_124, (V)"source_id")), () => R.Get(v_180, (V)"pending_attack")), () => (V)(R.Get(v_180, (V)"faction") != R.Get(v_122, (V)"faction"))), () => R.Get(v_180, (V)"hp")), () => (V)(R.Get(v_180, (V)"hp") > (V)0.0d)))) {
foreach (var row_181 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_180, (V)"pending_attack"), (V)"target_cells"), () => R.Table()))))))) {
V v_182 = row_181[0];
V v_183 = row_181[1];
foreach (var row_184 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_124, (V)"target_cells"), () => R.Table()))))))) {
V v_185 = row_184[0];
V v_186 = row_184[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_183, (V)"floor_id") == R.Get(v_186, (V)"floor_id")), () => (V)(R.Get(v_183, (V)"x") == R.Get(v_186, (V)"x"))), () => (V)(R.Get(v_183, (V)"y") == R.Get(v_186, (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
}
}
}
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth((V)(v_133 == (V)"approaching_owner"))) {
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_124, (V)"floor_id") == R.Get(R.Get(v_122, (V)"position"), (V)"floor_id")), () => R.Get(v_124, (V)"from_x")), () => R.Get(v_124, (V)"x")), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"x") - R.Get(R.Get(v_122, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"y") - R.Get(R.Get(v_122, (V)"position"), (V)"y"))))).First) < (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"from_x") - R.Get(R.Get(v_122, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"from_y") - R.Get(R.Get(v_122, (V)"position"), (V)"y"))))).First)))));
}
if (R.Truth((V)(v_133 == (V)"aligned_hostiles"))) {
if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_124, (V)"x"))), () => (V)(!R.Truth(R.Get(v_124, (V)"y")))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_187 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_188 = values_187[0];
V v_189 = values_187[1];
foreach (var row_190 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_121, (V)"entities"))))))) {
V v_191 = row_190[0];
V v_192 = row_190[1];
var values_193 = R.Join(new V[] {}, new Results(R.Get(v_192, (V)"position")));
V v_194 = values_193[0];
if (R.Truth(R.And(R.And(R.And(R.Get(v_192, (V)"hp"), () => (V)(R.Get(v_192, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_192, (V)"faction") != R.Get(v_122, (V)"faction"))), () => (V)(R.Get(v_194, (V)"floor_id") == R.Get(v_124, (V)"floor_id"))))) {
if (R.Truth((V)(R.Get(v_194, (V)"y") == R.Get(v_124, (V)"y")))) {
var values_195 = R.Join(new V[] {}, new Results((V)(v_188 + (V)1.0d)));
v_188 = values_195[0];
}
if (R.Truth((V)(R.Get(v_194, (V)"x") == R.Get(v_124, (V)"x")))) {
var values_196 = R.Join(new V[] {}, new Results((V)(v_189 + (V)1.0d)));
v_189 = values_196[0];
}
}
}
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_188}, new Results(v_189))).First >= R.Get(v_120, (V)"min"))));
}
if (R.Truth((V)(v_133 == (V)"event_distance"))) {
var values_197 = R.Join(new V[] {}, R.Call(v_63, R.Join(new V[] {}, new Results(v_124))));
V v_198 = values_197[0];
if (R.Truth(R.Or((V)(!R.Truth(v_198)), () => (V)(R.Get(v_198, (V)"floor_id") != R.Get(R.Get(v_122, (V)"position"), (V)"floor_id"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_199 = R.Join(new V[] {}, R.Call(v_59, R.Join(new V[] {R.Get(v_122, (V)"position")}, new Results(v_198))));
V v_200 = values_199[0];
return R.Join(new V[] {}, new Results(R.And((V)(v_200 >= R.Get(v_120, (V)"min")), () => (V)(v_200 <= R.Get(v_120, (V)"max")))));
}
if (R.Truth((V)(v_133 == (V)"source_owner"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"source_id") == R.Get(v_122, (V)"id"))));
}
if (R.Truth((V)(v_133 == (V)"target_owner"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"target_id") == R.Get(v_122, (V)"id"))));
}
if (R.Truth((V)(v_133 == (V)"source_not_owner"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"source_id") != R.Get(v_122, (V)"id"))));
}
if (R.Truth((V)(v_133 == (V)"target_hostile"))) {
return R.Join(new V[] {}, R.Call(v_73, R.Join(new V[] {v_122}, new Results(R.Get(v_124, (V)"target_faction")))));
}
if (R.Truth((V)(v_133 == (V)"source_hostile"))) {
return R.Join(new V[] {}, R.Call(v_73, R.Join(new V[] {v_122}, new Results(R.Get(v_124, (V)"source_faction")))));
}
if (R.Truth((V)(v_133 == (V)"source_in_radius"))) {
var values_201 = R.Join(new V[] {}, new Results(R.Get(v_124, (V)"source_position")));
V v_202 = values_201[0];
return R.Join(new V[] {}, new Results(R.And(R.And(v_202, () => (V)(R.Get(v_202, (V)"floor_id") == R.Get(R.Get(v_122, (V)"position"), (V)"floor_id"))), () => (V)(R.Call(v_59, R.Join(new V[] {R.Get(v_122, (V)"position")}, new Results(v_202))).First <= R.Get(v_120, (V)"radius")))));
}
if (R.Truth((V)(v_133 == (V)"direct_attack"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"direct_attack") == (V)true)));
}
if (R.Truth((V)(v_133 == (V)"other_boon"))) {
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_124, (V)"boon_id") != V.Nil), () => (V)(R.Get(v_124, (V)"boon_id") != v_123))));
}
if (R.Truth((V)(v_133 == (V)"target_was_full"))) {
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_124, (V)"hp_before") != V.Nil), () => (V)(R.Get(v_124, (V)"hp_before") == R.Get(v_124, (V)"target_max_hp")))));
}
if (R.Truth((V)(v_133 == (V)"owner_hp_percent"))) {
var values_203 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_124, (V)"source_hp_at_attack"), () => R.Get(v_122, (V)"hp"))));
V v_204 = values_203[0];
var values_205 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_124, (V)"source_max_hp_at_attack"), () => R.Get(v_122, (V)"max_hp"))));
V v_206 = values_205[0];
return R.Join(new V[] {}, new Results(R.And(R.And(v_204, () => v_206), () => (V)((V)(v_204 * (V)100.0d) <= (V)(v_206 * R.Get(v_120, (V)"max"))))));
}
if (R.Truth((V)(v_133 == (V)"adjacent_hostiles"))) {
if (R.Truth(R.Get(v_124, (V)"adjacent_hostile_count"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"adjacent_hostile_count") >= R.Get(v_120, (V)"min"))));
}
if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_124, (V)"x"))), () => (V)(!R.Truth(R.Get(v_124, (V)"y")))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_207 = R.Join(new V[] {}, new Results((V)0.0d));
V v_208 = values_207[0];
foreach (var row_209 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_121, (V)"entities"))))))) {
V v_210 = row_209[0];
V v_211 = row_209[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_211, (V)"id") != R.Get(v_122, (V)"id")), () => R.Get(v_211, (V)"hp")), () => (V)(R.Get(v_211, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_211, (V)"faction") != R.Get(v_122, (V)"faction"))), () => (V)(R.Get(R.Get(v_211, (V)"position"), (V)"floor_id") == R.Get(v_124, (V)"floor_id"))), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_211, (V)"position"), (V)"x") - R.Get(v_124, (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_211, (V)"position"), (V)"y") - R.Get(v_124, (V)"y"))))))).First == (V)1.0d)))) {
var values_212 = R.Join(new V[] {}, new Results((V)(v_208 + (V)1.0d)));
v_208 = values_212[0];
}
}
return R.Join(new V[] {}, new Results((V)(v_208 >= R.Get(v_120, (V)"min"))));
}
if (R.Truth((V)(v_133 == (V)"burning_pair"))) {
if (R.Truth((V)(R.Get(v_124, (V)"target_burning") != V.Nil))) {
return R.Join(new V[] {}, new Results(R.And(R.Get(v_124, (V)"target_burning"), () => (V)(R.Or(R.Get(v_124, (V)"adjacent_burning_count"), () => (V)0.0d) > (V)0.0d))));
}
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth((V)(v_133 == (V)"distinct_chain_group"))) {
var values_213 = R.Join(new V[] {}, new Results(R.Get(v_124, R.Or(R.Get(v_120, (V)"field"), () => (V)"target_id"))));
V v_214 = values_213[0];
if (R.Truth((V)(!R.Truth(v_214)))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_215 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_125, (V)"groups"), () => R.Table())));
var obj_216 = v_125;
V key_217 = (V)"groups";
R.Set(obj_216, key_217, values_215[0]);
var values_218 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_122, (V)"id"), R.Concat((V)":", R.Concat(v_123, R.Or(R.And(R.Get(v_120, (V)"field"), () => R.Concat((V)":", R.Get(v_120, (V)"field"))), () => (V)""))))));
V v_219 = values_218[0];
var values_220 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_125, (V)"groups"), v_219), () => R.Table(new Entry((V)"seen", R.Table()), new Entry((V)"count", (V)0.0d)))));
V v_221 = values_220[0];
var values_222 = R.Join(new V[] {}, new Results(v_221));
var obj_223 = R.Get(v_125, (V)"groups");
V key_224 = v_219;
R.Set(obj_223, key_224, values_222[0]);
if (R.Truth(R.Get(R.Get(v_221, (V)"seen"), v_214))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_225 = R.Join(new V[] {}, new Results((V)true));
var obj_226 = R.Get(v_221, (V)"seen");
V key_227 = v_214;
R.Set(obj_226, key_227, values_225[0]);
var values_228 = R.Join(new V[] {}, new Results((V)(R.Get(v_221, (V)"count") + (V)1.0d)));
var obj_229 = v_221;
V key_230 = (V)"count";
R.Set(obj_229, key_230, values_228[0]);
if (R.Truth((V)(R.Get(v_221, (V)"count") < R.Get(v_120, (V)"size")))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_231 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_232 = v_221;
V key_233 = (V)"count";
R.Set(obj_232, key_233, values_231[0]);
return R.Join(new V[] {}, new Results((V)true));
}
if (R.Truth((V)(v_133 == (V)"unique_windup"))) {
if (R.Truth((V)(!R.Truth(R.Get(v_124, (V)"windup_id"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_234 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_125, (V)"windups"), () => R.Table())));
var obj_235 = v_125;
V key_236 = (V)"windups";
R.Set(obj_235, key_236, values_234[0]);
var values_237 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_122, (V)"id"), R.Concat((V)":", R.Concat(v_123, R.Concat((V)":", R.Get(v_124, (V)"windup_id")))))));
V v_238 = values_237[0];
if (R.Truth(R.Get(R.Get(v_125, (V)"windups"), v_238))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_239 = R.Join(new V[] {}, new Results((V)true));
var obj_240 = R.Get(v_125, (V)"windups");
V key_241 = v_238;
R.Set(obj_240, key_241, values_239[0]);
return R.Join(new V[] {}, new Results((V)true));
}
if (R.Truth((V)(v_133 == (V)"ability_id"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"ability_id") == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"positive"))) {
return R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_124, (V)"actual_loss"), () => (V)0.0d) > (V)0.0d)));
}
if (R.Truth((V)(v_133 == (V)"source_target_same_faction"))) {
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_124, (V)"source_id") != R.Get(v_124, (V)"target_id")), () => (V)(R.Get(v_124, (V)"source_faction") != V.Nil)), () => (V)(R.Get(v_124, (V)"source_faction") == R.Get(v_124, (V)"target_faction")))));
}
if (R.Truth((V)(v_133 == (V)"pair_adjacent_damage"))) {
return R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_93, R.Join(new V[] {v_121, v_122, v_123, v_124, v_125}, new Results(R.Get(v_120, (V)"neighborhood")))).First) > (V)0.0d)));
}
if (R.Truth((V)(v_133 == (V)"source_id"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"source_id") == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"target_id"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"target_id") == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"source_faction"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"source_faction") == R.Get(v_120, (V)"value"))));
}
if (R.Truth((V)(v_133 == (V)"target_faction"))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_124, (V)"target_faction") == R.Get(v_120, (V)"value"))));
}
if (R.Truth(R.Or((V)(v_133 == (V)"source_tag"), () => (V)(v_133 == (V)"target_tag")))) {
var values_242 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_133 == (V)"source_tag"), () => R.Get(v_124, (V)"source_tags")), () => R.Get(v_124, (V)"target_tags"))));
V v_243 = values_242[0];
foreach (var row_244 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_243, () => R.Table()))))))) {
V v_245 = row_244[0];
V v_246 = row_244[1];
if (R.Truth((V)(v_246 == R.Get(v_120, (V)"value")))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"unknown predicate operation")));
return Results.Empty;
}));
V v_247 = V.Nil;
v_247 = new V((RuleFunction)(args_248 => {
V v_249 = args_248[0];
V v_250 = args_248[1];
V v_251 = args_248[2];
var values_252 = R.Join(new V[] {}, new Results((V)0.0d));
V v_253 = values_252[0];
foreach (var row_254 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_255 = row_254[0];
V v_256 = row_254[1];
var values_257 = R.Join(new V[] {}, new Results((V)(v_253 + (V)(R.Or(R.Get(v_250, v_256), () => (V)0.0d) * R.Get(R.Get(R.Get(v_249, (V)"parameters"), v_256), v_251)))));
v_253 = values_257[0];
}
return R.Join(new V[] {}, new Results(v_253));
return Results.Empty;
}));
R.Set(v_18, (V)"aggregate", new V((RuleFunction)(args_258 => {
V v_259 = args_258[0];
V v_260 = args_258[1];
V v_261 = args_258[2];
var values_262 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_259))).First}, new Results((V)"unknown boon"))));
V v_263 = values_262[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(v_261 == (V)"chance"), () => (V)(v_261 == (V)"power"))}, new Results((V)"unknown boon parameter")));
return R.Join(new V[] {}, R.Call(v_247, R.Join(new V[] {v_263, v_260}, new Results(v_261))));
return Results.Empty;
})));
R.Set(v_18, (V)"has_pattern", new V((RuleFunction)(args_264 => {
V v_265 = args_264[0];
if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_265, (V)"boons"))), () => (V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(v_265, (V)"boons")))).First))))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_266 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_267 = row_266[0];
V v_268 = row_266[1];
if (R.Truth(R.And(R.And(R.Get(v_265, (V)"boons"), () => R.Get(R.Get(v_265, (V)"boons"), v_268)), () => R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_268))).First, (V)"pattern_preview")))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_18, (V)"preview_patterns", new V((RuleFunction)(args_269 => {
V v_270 = args_269[0];
V v_271 = args_269[1];
V v_272 = args_269[2];
V v_273 = args_269[3];
V v_274 = args_269[4];
V v_275 = args_269[5];
if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_273, (V)"valid"))), () => (V)(!R.Truth(R.Call(R.Get(v_18, (V)"has_pattern"), R.Join(new V[] {}, new Results(v_271))).First))))) {
return Results.Empty;
}
if (R.Truth(R.And(R.And((V)(v_272 != (V)"dash"), () => (V)(v_272 != (V)"wait")), () => (V)(!R.Truth(R.Call(R.Get(v_10, (V)"immediate_attack"), R.Join(new V[] {v_271, R.Or(R.And(R.Get(v_273, (V)"bump"), () => (V)"basic_attack"), () => v_272)}, new Results(v_273))).First))))) {
return Results.Empty;
}
var values_276 = R.Join(new V[] {}, new Results(R.Table()));
V v_277 = values_276[0];
var values_278 = R.Join(new V[] {}, new Results(R.Get(v_271, (V)"position")));
V v_279 = values_278[0];
if (R.Truth(R.Or((V)(v_272 == (V)"dash"), () => (V)(v_272 == (V)"wait")))) {
var values_280 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_273, (V)"beacon_return"), () => R.Get(R.Get(v_273, (V)"cells"), (V)1.0d)), () => v_279)));
V v_281 = values_280[0];
var values_282 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_279, (V)"floor_id")), new Entry((V)"x", R.Get(v_281, (V)"x")), new Entry((V)"y", R.Get(v_281, (V)"y")), new Entry((V)"from_x", R.Get(v_281, (V)"x")), new Entry((V)"from_y", R.Get(v_281, (V)"y")), new Entry((V)"source_position", R.Or(v_275, () => v_279)))));
var obj_283 = v_277;
V key_284 = (V)1.0d;
R.Set(obj_283, key_284, values_282[0]);
} else {
var values_285 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"map_for"), R.Join(new V[] {}, new Results(v_270))));
V v_286 = values_285[0];
foreach (var row_287 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_273, (V)"cells"))))))) {
V v_288 = row_287[0];
V v_289 = row_287[1];
var values_290 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_289, (V)"floor_id"), () => R.Get(v_279, (V)"floor_id"))));
V v_291 = values_290[0];
var values_292 = R.Join(new V[] {}, new Results(R.Or((V)(!R.Truth(v_274)), () => R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_286, R.Get(v_289, (V)"x"), R.Get(v_289, (V)"y")}, new Results(v_270))).First)));
V v_293 = values_292[0];
if (R.Truth(R.And((V)(!R.Truth(v_293)), () => (V)(v_274 != (V)"area")))) {
break;
}
var values_294 = Results.Empty;
V v_295 = values_294[0];
if (R.Truth(R.And(v_293, () => R.Call(R.Get(v_12, (V)"is_visible"), R.Join(new V[] {v_270, v_291, R.Get(v_289, (V)"x")}, new Results(R.Get(v_289, (V)"y")))).First))) {
foreach (var row_296 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_270, (V)"entities"))))))) {
V v_297 = row_296[0];
V v_298 = row_296[1];
var values_299 = R.Join(new V[] {}, new Results(R.Get(v_298, (V)"position")));
V v_300 = values_299[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_298, (V)"id") != R.Get(v_271, (V)"id")), () => R.Get(v_298, (V)"hp")), () => (V)(R.Get(v_298, (V)"hp") > (V)0.0d)), () => v_300), () => (V)(R.Get(v_300, (V)"floor_id") == v_291)), () => (V)(R.Get(v_300, (V)"x") == R.Get(v_289, (V)"x"))), () => (V)(R.Get(v_300, (V)"y") == R.Get(v_289, (V)"y"))))) {
var values_301 = R.Join(new V[] {}, new Results(v_298));
v_295 = values_301[0];

break;
}
}
}
if (R.Truth(R.And(R.And(v_295, () => (V)(R.Get(v_295, (V)"faction") != R.Get(v_271, (V)"faction"))), () => (V)(!R.Truth(R.Get(v_295, (V)"invulnerable")))))) {
var values_302 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", v_291), new Entry((V)"x", R.Get(v_289, (V)"x")), new Entry((V)"y", R.Get(v_289, (V)"y")), new Entry((V)"source_position", R.Or(v_275, () => v_279)))));
var obj_303 = v_277;
V key_304 = (V)((V)R.Length(v_277) + (V)1.0d);
R.Set(obj_303, key_304, values_302[0]);
}
if (R.Truth(R.And(v_295, () => (V)(v_274 == (V)true)))) {
break;
}
}
}
var values_305 = R.Join(new V[] {R.Table(), R.Table()}, new Results(R.Table()));
V v_306 = values_305[0];
V v_307 = values_305[1];
V v_308 = values_305[2];
foreach (var row_309 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_310 = row_309[0];
V v_311 = row_309[1];
var values_312 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_311))));
V v_313 = values_312[0];
var values_314 = R.Join(new V[] {}, new Results(R.Get(v_313, (V)"pattern_preview")));
V v_315 = values_314[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.Get(v_271, (V)"boons"), () => R.Get(R.Get(v_271, (V)"boons"), v_311)), () => v_315), () => R.Or(R.Or(R.Or(R.And(R.And((V)(v_272 == (V)"dash"), () => (V)(!R.Truth(R.Get(v_273, (V)"beacon_return")))), () => (V)(R.Get(v_315, (V)"trigger") == (V)"dash")), () => R.And(R.And((V)(v_272 == (V)"dash"), () => R.Get(v_273, (V)"beacon_return")), () => (V)(R.Get(v_315, (V)"trigger") == (V)"return"))), () => R.And((V)(v_272 == (V)"wait"), () => (V)(R.Get(v_315, (V)"trigger") == (V)"wait"))), () => R.And(R.And((V)(v_272 != (V)"dash"), () => (V)(v_272 != (V)"wait")), () => R.Or((V)(R.Get(v_315, (V)"trigger") == (V)"hit"), () => (V)(R.Get(v_315, (V)"trigger") == (V)"kill"))))), () => R.Or((V)(!R.Truth(R.Get(v_315, (V)"special"))), () => (V)(v_272 == (V)"sweeping_slash"))))) {
var values_316 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_313, (V)"effects"), (V)1.0d)));
V v_317 = values_316[0];
foreach (var row_318 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_277)))))) {
V v_319 = row_318[0];
V v_320 = row_318[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_315, (V)"axis"))), () => R.Call(R.Get(v_16, (V)"axis"), R.Join(new V[] {v_320}, new Results(R.Get(v_315, (V)"axis")))).First))) {
foreach (var row_321 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_16, (V)"cells"), R.Join(new V[] {v_270, R.Get(v_317, (V)"pattern")}, new Results(v_320)))))))) {
V v_322 = row_321[0];
V v_323 = row_321[1];
var values_324 = R.Join(new V[] {}, new Results(R.Concat(v_311, R.Concat((V)":", R.Concat(R.Get(v_323, (V)"x"), R.Concat((V)":", R.Get(v_323, (V)"y")))))));
V v_325 = values_324[0];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_307, v_325))), () => R.Call(R.Get(v_12, (V)"is_visible"), R.Join(new V[] {v_270, R.Get(v_323, (V)"floor_id"), R.Get(v_323, (V)"x")}, new Results(R.Get(v_323, (V)"y")))).First))) {
var values_326 = R.Join(new V[] {}, new Results((V)true));
var obj_327 = v_307;
V key_328 = v_325;
R.Set(obj_327, key_328, values_326[0]);
var values_329 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_323, (V)"floor_id")), new Entry((V)"x", R.Get(v_323, (V)"x")), new Entry((V)"y", R.Get(v_323, (V)"y")), new Entry((V)"boon_id", v_311), new Entry((V)"condition", R.Get(v_315, (V)"trigger")), new Entry((V)"delay", R.Get(v_317, (V)"delay")))));
var obj_330 = v_306;
V key_331 = (V)((V)R.Length(v_306) + (V)1.0d);
R.Set(obj_330, key_331, values_329[0]);
var values_332 = R.Join(new V[] {}, new Results((V)true));
var obj_333 = v_308;
V key_334 = R.Get(v_315, (V)"trigger");
R.Set(obj_333, key_334, values_332[0]);
}
}
}
}
}
}
if (R.Truth((V)((V)R.Length(v_306) > (V)0.0d))) {
var values_335 = R.Join(new V[] {}, new Results(v_306));
var obj_336 = v_273;
V key_337 = (V)"boon_cells";
R.Set(obj_336, key_337, values_335[0]);
var values_338 = R.Join(new V[] {}, new Results(R.Table()));
V v_339 = values_338[0];
foreach (var row_340 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"hit"), new Entry((V)2, (V)"kill"), new Entry((V)3, (V)"dash"), new Entry((V)4, (V)"wait"), new Entry((V)5, (V)"return")))))))) {
V v_341 = row_340[0];
V v_342 = row_340[1];
if (R.Truth(R.Get(v_308, v_342))) {
var values_343 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.And((V)(v_342 == (V)"dash"), () => (V)"Dash wake +150 time"), () => R.And((V)(v_342 == (V)"wait"), () => (V)"Wait rays")), () => R.And((V)(v_342 == (V)"return"), () => (V)"beacon landing rays")), () => R.Concat((V)"on ", v_342))));
var obj_344 = v_339;
V key_345 = (V)((V)R.Length(v_339) + (V)1.0d);
R.Set(obj_344, key_345, values_343[0]);
}
}
var values_346 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_339}, new Results((V)" / "))));
var obj_347 = v_273;
V key_348 = (V)"boon_hint";
R.Set(obj_347, key_348, values_346[0]);
}
return Results.Empty;
})));
R.Set(v_18, (V)"proc_count", new V((RuleFunction)(args_349 => {
V v_350 = args_349[0];
V v_351 = args_349[1];
V v_352 = args_349[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_352))).First == (V)"number"), () => (V)(v_352 >= (V)0.0d)), () => (V)(v_352 <= (V)10.0d))}, new Results((V)"invalid proc coefficient")));
var values_353 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_351 * v_352) + (V)0.5d)))));
V v_354 = values_353[0];
var values_355 = R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_354 / v_20)))).First}, new Results((V)(v_354 % v_20)));
V v_356 = values_355[0];
V v_357 = values_355[1];
if (R.Truth(R.And((V)(v_357 > (V)0.0d), () => (V)(R.Call(R.Get(v_6, (V)"next_int"), R.Join(new V[] {R.Get(v_350, (V)"rng")}, new Results(v_20))).First <= v_357)))) {
return R.Join(new V[] {}, new Results((V)(v_356 + (V)1.0d)));
}
return R.Join(new V[] {}, new Results(v_356));
return Results.Empty;
})));
V v_358 = V.Nil;
v_358 = new V((RuleFunction)(args_359 => {
V v_360 = args_359[0];
foreach (var row_361 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_362 = row_361[0];
V v_363 = row_361[1];
var values_364 = R.Join(new V[] {}, new Results(R.And(R.Get(v_360, (V)"boons"), () => R.Get(R.Get(v_360, (V)"boons"), v_363))));
V v_365 = values_364[0];
if (R.Truth(v_365)) {
foreach (var row_366 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_367 = row_366[0];
V v_368 = row_366[1];
if (R.Truth((V)(R.Or(R.Get(v_365, v_368), () => (V)0.0d) > (V)0.0d))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
R.Set(v_18, (V)"active", new V((RuleFunction)(args_369 => {
V v_370 = args_369[0];
foreach (var row_371 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_370, (V)"entities"))))))) {
V v_372 = row_371[0];
V v_373 = row_371[1];
if (R.Truth(R.Call(v_358, R.Join(new V[] {}, new Results(v_373))).First)) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_18, (V)"set_stacks", new V((RuleFunction)(args_374 => {
V v_375 = args_374[0];
V v_376 = args_374[1];
V v_377 = args_374[2];
V v_378 = args_374[3];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_376))).First}, new Results((V)"unknown boon")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_378))).First == (V)"number"), () => (V)((V)(v_378 % (V)1.0d) == (V)0.0d)), () => (V)(v_378 >= (V)0.0d))}, new Results((V)"invalid boon stack count")));
var values_379 = R.Join(new V[] {}, new Results((V)false));
V v_380 = values_379[0];
foreach (var row_381 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_382 = row_381[0];
V v_383 = row_381[1];
if (R.Truth((V)(v_377 == v_383))) {
var values_384 = R.Join(new V[] {}, new Results((V)true));
v_380 = values_384[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_380}, new Results((V)"invalid boon rarity")));
var values_385 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_375, (V)"boons"), () => R.Table())));
var obj_386 = v_375;
V key_387 = (V)"boons";
R.Set(obj_386, key_387, values_385[0]);
var values_388 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_376))));
V v_389 = values_388[0];
if (R.Truth((V)(v_378 > (V)0.0d))) {
foreach (var row_390 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_391 = row_390[0];
V v_392 = row_390[1];
var values_393 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_375, (V)"boons"), v_392)));
V v_394 = values_393[0];
if (R.Truth(R.And(v_394, () => (V)(v_392 != v_376)))) {
var values_395 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_392))));
V v_396 = values_395[0];
var values_397 = R.Join(new V[] {}, new Results((V)false));
V v_398 = values_397[0];
foreach (var row_399 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_389, (V)"incompatible"), () => R.Table()))))))) {
V v_400 = row_399[0];
V v_401 = row_399[1];
if (R.Truth((V)(v_401 == v_392))) {
var values_402 = R.Join(new V[] {}, new Results((V)true));
v_398 = values_402[0];
}
}
foreach (var row_403 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_396, (V)"incompatible"), () => R.Table()))))))) {
V v_404 = row_403[0];
V v_405 = row_403[1];
if (R.Truth((V)(v_405 == v_376))) {
var values_406 = R.Join(new V[] {}, new Results((V)true));
v_398 = values_406[0];
}
}
if (R.Truth(v_398)) {
foreach (var row_407 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_408 = row_407[0];
V v_409 = row_407[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Or(R.Get(v_394, v_409), () => (V)0.0d) == (V)0.0d)}, new Results((V)"incompatible boons")));
}
}
}
}
}
var values_410 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_375, (V)"boons"), v_376), () => R.Table())));
V v_411 = values_410[0];
if (R.Truth((V)(v_378 == (V)0.0d))) {
var values_412 = R.Join(new V[] {}, new Results(V.Nil));
var obj_413 = v_411;
V key_414 = v_377;
R.Set(obj_413, key_414, values_412[0]);
} else {
var values_415 = R.Join(new V[] {}, new Results(v_378));
var obj_416 = v_411;
V key_417 = v_377;
R.Set(obj_416, key_417, values_415[0]);
}
if (R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(v_411))).First)) {
var values_418 = R.Join(new V[] {}, new Results(v_411));
var obj_419 = R.Get(v_375, (V)"boons");
V key_420 = v_376;
R.Set(obj_419, key_420, values_418[0]);
} else {
var values_421 = R.Join(new V[] {}, new Results(V.Nil));
var obj_422 = R.Get(v_375, (V)"boons");
V key_423 = v_376;
R.Set(obj_422, key_423, values_421[0]);
}
return Results.Empty;
})));
R.Set(v_18, (V)"validate_inventory", new V((RuleFunction)(args_424 => {
V v_425 = args_424[0];
V v_426 = args_424[1];
foreach (var row_427 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_425, (V)"boons"), () => R.Table()))))))) {
V v_428 = row_427[0];
V v_429 = row_427[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_428))).First}, new Results((V)"unknown owned boon")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_429))).First == (V)"table")}, new Results((V)"invalid boon stacks")));
foreach (var row_430 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_429)))))) {
V v_431 = row_430[0];
V v_432 = row_430[1];
var values_433 = R.Join(new V[] {}, new Results((V)false));
V v_434 = values_433[0];
foreach (var row_435 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_436 = row_435[0];
V v_437 = row_435[1];
if (R.Truth((V)(v_431 == v_437))) {
var values_438 = R.Join(new V[] {}, new Results((V)true));
v_434 = values_438[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(v_434, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_432))).First == (V)"number")), () => (V)((V)(v_432 % (V)1.0d) == (V)0.0d)), () => (V)(v_432 > (V)0.0d))}, new Results((V)"invalid boon stacks")));
}
}
foreach (var row_439 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_440 = row_439[0];
V v_441 = row_439[1];
var values_442 = R.Join(new V[] {}, new Results(R.And(R.Get(v_425, (V)"boons"), () => R.Get(R.Get(v_425, (V)"boons"), v_441))));
V v_443 = values_442[0];
if (R.Truth(v_443)) {
foreach (var row_444 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_441))).First, (V)"incompatible"), () => R.Table()))))))) {
V v_445 = row_444[0];
V v_446 = row_444[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.And(R.Get(v_425, (V)"boons"), () => R.Get(R.Get(v_425, (V)"boons"), v_446))))}, new Results((V)"incompatible boons")));
}
}
}
foreach (var row_447 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_425, (V)"boon_memory"), () => R.Table()))))))) {
V v_448 = row_447[0];
V v_449 = row_447[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_448))).First, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_449))).First == (V)"table"))}, new Results((V)"invalid boon memory")));
foreach (var row_450 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"charges"), new Entry((V)2, (V)"expires_at"), new Entry((V)3, (V)"speed"), new Entry((V)4, (V)"ward"), new Entry((V)5, (V)"mark_count"), new Entry((V)6, (V)"mark_target_id")))))))) {
V v_451 = row_450[0];
V v_452 = row_450[1];
var values_453 = R.Join(new V[] {}, new Results(R.Get(v_449, v_452)));
V v_454 = values_453[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(v_454 == V.Nil), () => R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_454))).First == (V)"number"), () => (V)((V)(v_454 % (V)1.0d) == (V)0.0d)), () => (V)(v_454 >= (V)0.0d)))}, new Results((V)"invalid boon memory value")));
}
if (R.Truth(R.Or(R.Or(R.Or(R.Get(v_449, (V)"charges"), () => R.Get(v_449, (V)"speed")), () => R.Get(v_449, (V)"ward")), () => R.Get(v_449, (V)"mark_count")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_449, (V)"expires_at") != V.Nil)}, new Results((V)"boon memory missing expiry")));
}
if (R.Truth(R.Get(v_449, (V)"mark_count"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_449, (V)"mark_target_id") != V.Nil)}, new Results((V)"boon mark missing target")));
}
if (R.Truth((V)(R.Get(v_449, (V)"face") != V.Nil))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_448 == (V)"core:boon/fateful_dice"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_449, (V)"face")))).First == (V)"number")), () => (V)((V)(R.Get(v_449, (V)"face") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_449, (V)"face") >= (V)1.0d)), () => (V)(R.Get(v_449, (V)"face") <= (V)8.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_449, (V)"roll_action_id")))).First == (V)"number")), () => (V)((V)(R.Get(v_449, (V)"roll_action_id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_449, (V)"roll_action_id") >= (V)1.0d))}, new Results((V)"invalid saved dice roll")));
if (R.Truth(v_426)) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_449, (V)"roll_action_id") < R.Get(v_426, (V)"next_action_id"))}, new Results((V)"future saved dice roll")));
}
}
}
return Results.Empty;
})));
R.Set(v_18, (V)"attack_vow", new V((RuleFunction)(args_455 => {
V v_456 = args_455[0];
var values_457 = R.Join(new V[] {}, new Results((V)"core:boon/pilgrims_vow"));
V v_458 = values_457[0];
var values_459 = R.Join(new V[] {}, new Results(R.And(R.Get(v_456, (V)"boons"), () => R.Get(R.Get(v_456, (V)"boons"), v_458))));
V v_460 = values_459[0];
if (R.Truth((V)(!R.Truth(v_460)))) {
return Results.Empty;
}
var values_461 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_458))));
V v_462 = values_461[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"boon_id", v_458), new Entry((V)"hp_cost", R.Get(R.Get(v_462, (V)"attack_payment"), (V)"hp")), new Entry((V)"bonus", R.Call(v_247, R.Join(new V[] {v_462, v_460}, new Results((V)"power"))).First))));
return Results.Empty;
})));
R.Set(v_18, (V)"dice_power", new V((RuleFunction)(args_463 => {
V v_464 = args_463[0];
var values_465 = R.Join(new V[] {}, new Results((V)"core:boon/fateful_dice"));
V v_466 = values_465[0];
var values_467 = R.Join(new V[] {}, new Results(R.And(R.Get(v_464, (V)"boons"), () => R.Get(R.Get(v_464, (V)"boons"), v_466))));
V v_468 = values_467[0];
if (R.Truth(v_468)) {
return R.Join(new V[] {}, R.Call(v_247, R.Join(new V[] {R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_466))).First, v_468}, new Results((V)"power"))));
}
return Results.Empty;
})));
R.Set(v_18, (V)"roll_dice", new V((RuleFunction)(args_469 => {
V v_470 = args_469[0];
V v_471 = args_469[1];
V v_472 = args_469[2];
var values_473 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"dice_power"), R.Join(new V[] {}, new Results(v_471))));
V v_474 = values_473[0];
if (R.Truth((V)(!R.Truth(v_474)))) {
return Results.Empty;
}
var values_475 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"next_int"), R.Join(new V[] {R.Get(v_470, (V)"rng")}, new Results((V)8.0d))));
V v_476 = values_475[0];
var values_477 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_471, (V)"boon_memory"), () => R.Table())));
var obj_478 = v_471;
V key_479 = (V)"boon_memory";
R.Set(obj_478, key_479, values_477[0]);
var values_480 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_471, (V)"boon_memory"), (V)"core:boon/fateful_dice"), () => R.Table())));
V v_481 = values_480[0];
var values_482 = R.Join(new V[] {}, new Results(v_476));
var obj_483 = v_481;
V key_484 = (V)"face";
R.Set(obj_483, key_484, values_482[0]);

var values_485 = R.Join(new V[] {}, new Results(v_472));
var obj_486 = v_481;
V key_487 = (V)"roll_action_id";
R.Set(obj_486, key_487, values_485[0]);
var values_488 = R.Join(new V[] {}, new Results(v_481));
var obj_489 = R.Get(v_471, (V)"boon_memory");
V key_490 = (V)"core:boon/fateful_dice";
R.Set(obj_489, key_490, values_488[0]);
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"face", v_476), new Entry((V)"power", v_474))));
return Results.Empty;
})));
R.Set(v_18, (V)"rebase_stage_memory", new V((RuleFunction)(args_491 => {
V v_492 = args_491[0];
V v_493 = args_491[1];
V v_494 = args_491[2];
foreach (var row_495 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_496 = row_495[0];
V v_497 = row_495[1];
var values_498 = R.Join(new V[] {}, new Results(R.And(R.Get(v_492, (V)"boon_memory"), () => R.Get(R.Get(v_492, (V)"boon_memory"), v_497))));
V v_499 = values_498[0];
if (R.Truth(R.And(v_499, () => R.Get(v_499, (V)"expires_at")))) {
var values_500 = R.Join(new V[] {}, new Results((V)(R.Get(v_499, (V)"expires_at") - v_493)));
V v_501 = values_500[0];
var values_502 = R.Join(new V[] {}, new Results((V)(v_494 + R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results(v_501))).First)));
var obj_503 = v_499;
V key_504 = (V)"expires_at";
R.Set(obj_503, key_504, values_502[0]);
if (R.Truth((V)(v_501 < (V)0.0d))) {
foreach (var row_505 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"charges"), new Entry((V)2, (V)"speed"), new Entry((V)3, (V)"ward"), new Entry((V)4, (V)"mark_count")))))))) {
V v_506 = row_505[0];
V v_507 = row_505[1];
if (R.Truth((V)(R.Get(v_499, v_507) != V.Nil))) {
var values_508 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_509 = v_499;
V key_510 = v_507;
R.Set(obj_509, key_510, values_508[0]);
}
}
}
}
}
return Results.Empty;
})));
var values_511 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"guard", (V)true), new Entry((V)"spitter", (V)true), new Entry((V)"skitter", (V)true), new Entry((V)"spore_herald", (V)true), new Entry((V)"canal_lancer", (V)true), new Entry((V)"cinder_bellkeeper", (V)true), new Entry((V)"expansion_regular", (V)true), new Entry((V)"squad", (V)true), new Entry((V)"idea_regular", (V)true))));
V v_512 = values_511[0];
R.Set(v_18, (V)"skull_target", new V((RuleFunction)(args_513 => {
V v_514 = args_513[0];
var values_515 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_514, (V)"definition_id")))));
V v_516 = values_515[0];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_514, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_514, (V)"faction") == (V)"enemy")), () => (V)(!R.Truth(R.Get(v_514, (V)"reward_exempt")))), () => (V)(!R.Truth(R.Get(v_514, (V)"invulnerable")))), () => v_516), () => (V)(!R.Truth(R.Get(v_516, (V)"special_encounter")))), () => R.Get(v_512, R.Get(v_516, (V)"ai"))), () => (V)false)));
return Results.Empty;
})));
R.Set(v_18, (V)"speed_bonus", new V((RuleFunction)(args_517 => {
V v_518 = args_517[0];
V v_519 = args_517[1];
if (R.Truth((V)(!R.Truth(R.Get(v_519, (V)"boon_memory"))))) {
return R.Join(new V[] {}, new Results((V)0.0d));
}
var values_520 = R.Join(new V[] {}, new Results((V)0.0d));
V v_521 = values_520[0];
foreach (var row_522 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_523 = row_522[0];
V v_524 = row_522[1];
var values_525 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_519, (V)"boon_memory"), v_524)));
V v_526 = values_525[0];
if (R.Truth(R.And(v_526, () => (V)(R.Or(R.Get(v_526, (V)"expires_at"), () => (-(V)1.0d)) >= R.Get(v_518, (V)"clock"))))) {
var values_527 = R.Join(new V[] {}, new Results((V)(v_521 + R.Or(R.Get(v_526, (V)"speed"), () => (V)0.0d))));
v_521 = values_527[0];
}
}
return R.Join(new V[] {}, new Results(v_521));
return Results.Empty;
})));
R.Set(v_18, (V)"consume_ward", new V((RuleFunction)(args_528 => {
V v_529 = args_528[0];
V v_530 = args_528[1];
V v_531 = args_528[2];
if (R.Truth((V)(v_531 <= (V)0.0d))) {
return R.Join(new V[] {}, new Results((V)0.0d));
}
if (R.Truth((V)(!R.Truth(R.Get(v_530, (V)"boon_memory"))))) {
return R.Join(new V[] {}, new Results((V)0.0d));
}
var values_532 = R.Join(new V[] {}, new Results((V)0.0d));
V v_533 = values_532[0];
foreach (var row_534 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_535 = row_534[0];
V v_536 = row_534[1];
var values_537 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_530, (V)"boon_memory"), v_536)));
V v_538 = values_537[0];
if (R.Truth(R.And(R.And(R.And((V)(v_533 < v_531), () => v_538), () => (V)(R.Or(R.Get(v_538, (V)"expires_at"), () => (-(V)1.0d)) >= R.Get(v_529, (V)"clock"))), () => (V)(R.Or(R.Get(v_538, (V)"ward"), () => (V)0.0d) > (V)0.0d)))) {
var values_539 = R.Join(new V[] {}, new Results((V)(v_533 + R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)(v_531 - v_533)}, new Results(R.Get(v_538, (V)"ward")))).First)));
v_533 = values_539[0];
var values_540 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_541 = v_538;
V key_542 = (V)"ward";
R.Set(obj_541, key_542, values_540[0]);
}
}
return R.Join(new V[] {}, new Results(v_533));
return Results.Empty;
})));
R.Set(v_18, (V)"ward_total", new V((RuleFunction)(args_543 => {
V v_544 = args_543[0];
V v_545 = args_543[1];
var values_546 = R.Join(new V[] {}, new Results((V)0.0d));
V v_547 = values_546[0];
foreach (var row_548 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_549 = row_548[0];
V v_550 = row_548[1];
var values_551 = R.Join(new V[] {}, new Results(R.And(R.Get(v_545, (V)"boon_memory"), () => R.Get(R.Get(v_545, (V)"boon_memory"), v_550))));
V v_552 = values_551[0];
if (R.Truth(R.And(v_552, () => (V)(R.Or(R.Get(v_552, (V)"expires_at"), () => (-(V)1.0d)) >= R.Get(v_544, (V)"clock"))))) {
var values_553 = R.Join(new V[] {}, new Results((V)(v_547 + R.Or(R.Get(v_552, (V)"ward"), () => (V)0.0d))));
v_547 = values_553[0];
}
}
return R.Join(new V[] {}, new Results(v_547));
return Results.Empty;
})));
R.Set(v_18, (V)"route_before_form", new V((RuleFunction)(args_554 => {
V v_555 = args_554[0];
V v_556 = args_554[1];
V v_557 = args_554[2];
foreach (var row_558 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_24, v_556), () => R.Table()))))))) {
V v_559 = row_558[0];
V v_560 = row_558[1];
if (R.Truth(R.And(R.And(R.Get(v_555, (V)"boons"), () => R.Get(R.Get(v_555, (V)"boons"), R.Get(v_560, (V)"boon_id"))), () => (V)(!R.Truth(R.And(R.Get(v_560, (V)"explicit_only"), () => (V)(v_557 == (V)false))))))) {
return R.Join(new V[] {}, new Results(R.Get(v_560, (V)"mode")));
}
}
return Results.Empty;
})));
R.Set(v_18, (V)"ability", new V((RuleFunction)(args_561 => {
V v_562 = args_561[0];
V v_563 = args_561[1];
V v_564 = args_561[2];
V v_565 = args_561[3];
V v_566 = args_561[4];
V v_567 = args_561[5];
V v_568 = args_561[6];
V v_569 = args_561[7];
V v_570 = args_561[8];
V v_571 = args_561[9];
var values_572 = R.Join(new V[] {R.Table(), v_566}, new Results(V.Nil));
V v_573 = values_572[0];
V v_574 = values_572[1];
V v_575 = values_572[2];
var values_576 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"map_for"), R.Join(new V[] {}, new Results(v_562))));
V v_577 = values_576[0];
V v_578 = V.Nil;
v_578 = new V((RuleFunction)(args_579 => {
V v_580 = args_579[0];
foreach (var row_581 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_562, (V)"entities"))))))) {
V v_582 = row_581[0];
V v_583 = row_581[1];
var values_584 = R.Join(new V[] {}, new Results(R.Get(v_583, (V)"position")));
V v_585 = values_584[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_583, (V)"id") != R.Get(v_563, (V)"id")), () => R.Get(v_583, (V)"hp")), () => (V)(R.Get(v_583, (V)"hp") > (V)0.0d)), () => v_585), () => (V)(R.Get(v_585, (V)"floor_id") == R.Get(R.Get(v_563, (V)"position"), (V)"floor_id"))), () => (V)(R.Get(v_585, (V)"x") == R.Get(v_580, (V)"x"))), () => (V)(R.Get(v_585, (V)"y") == R.Get(v_580, (V)"y"))))) {
return R.Join(new V[] {}, new Results(v_583));
}
}
return Results.Empty;
}));
V v_586 = V.Nil;
v_586 = new V((RuleFunction)(args_587 => {
V v_588 = args_587[0];
V v_589 = args_587[1];
if (R.Truth(R.And((V)(v_588 == R.Get(R.Get(v_563, (V)"position"), (V)"x")), () => (V)(v_589 == R.Get(R.Get(v_563, (V)"position"), (V)"y"))))) {
return Results.Empty;
}
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_577, v_588, v_589}, new Results(v_562))).First)))) {
return Results.Empty;
}
foreach (var row_590 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_573)))))) {
V v_591 = row_590[0];
V v_592 = row_590[1];
if (R.Truth(R.And((V)(R.Get(v_592, (V)"x") == v_588), () => (V)(R.Get(v_592, (V)"y") == v_589)))) {
return Results.Empty;
}
}
var values_593 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_563, (V)"position"), (V)"floor_id")), new Entry((V)"x", v_588), new Entry((V)"y", v_589))));
var obj_594 = v_573;
V key_595 = (V)((V)R.Length(v_573) + (V)1.0d);
R.Set(obj_594, key_595, values_593[0]);
return Results.Empty;
}));
foreach (var row_596 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_565)))))) {
V v_597 = row_596[0];
V v_598 = row_596[1];
var values_599 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_598, (V)"floor_id")), new Entry((V)"x", R.Get(v_598, (V)"x")), new Entry((V)"y", R.Get(v_598, (V)"y")))));
var obj_600 = v_573;
V key_601 = v_597;
R.Set(obj_600, key_601, values_599[0]);
}
foreach (var row_602 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_603 = row_602[0];
V v_604 = row_602[1];
var values_605 = R.Join(new V[] {}, new Results(R.And(R.Get(v_563, (V)"boons"), () => R.Get(R.Get(v_563, (V)"boons"), v_604))));
V v_606 = values_605[0];
if (R.Truth(v_606)) {
var values_607 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_604))));
V v_608 = values_607[0];
var values_609 = R.Join(new V[] {}, R.Call(v_247, R.Join(new V[] {v_608, v_606}, new Results((V)"power"))));
V v_610 = values_609[0];
foreach (var row_611 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_608, (V)"modifiers"), () => R.Table()))))))) {
V v_612 = row_611[0];
V v_613 = row_611[1];
if (R.Truth((V)(R.Get(v_613, (V)"ability_id") == v_564))) {
if (R.Truth((V)(R.Get(v_613, (V)"kind") == (V)"weapon_form"))) {
var values_614 = R.Join(new V[] {}, R.Call(R.Get(v_16, (V)"weapon_cells"), R.Join(new V[] {v_562, R.Get(v_613, (V)"form"), v_563, v_573, v_565, v_567, v_568, v_570, v_569}, new Results(v_571))));
v_573 = values_614[0];
v_570 = values_614[1];
v_575 = values_614[2];
} else if (R.Truth((V)(R.Get(v_613, (V)"kind") == (V)"forward_cell"))) {
var values_615 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_563, (V)"position"), (V)"x") + (V)(v_567 * (V)2.0d))));
V v_616 = values_615[0];
var values_617 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_563, (V)"position"), (V)"y") + (V)(v_568 * (V)2.0d))));
V v_618 = values_617[0];
var values_619 = R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_4, (V)"walkable"), R.Join(new V[] {v_577, (V)(R.Get(R.Get(v_563, (V)"position"), (V)"x") + v_567)}, new Results((V)(R.Get(R.Get(v_563, (V)"position"), (V)"y") + v_568)))).First, () => R.Call(R.Get(v_4, (V)"walkable"), R.Join(new V[] {v_577, v_616}, new Results(v_618))).First)));
V v_620 = values_619[0];
var values_621 = R.Join(new V[] {}, new Results((V)false));
V v_622 = values_621[0];
foreach (var row_623 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_573)))))) {
V v_624 = row_623[0];
V v_625 = row_623[1];
if (R.Truth(R.And((V)(R.Get(v_625, (V)"x") == v_616), () => (V)(R.Get(v_625, (V)"y") == v_618)))) {
var values_626 = R.Join(new V[] {}, new Results((V)true));
v_622 = values_626[0];
}
}
if (R.Truth(R.And(v_620, () => (V)(!R.Truth(v_622))))) {
var values_627 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_563, (V)"position"), (V)"floor_id")), new Entry((V)"x", v_616), new Entry((V)"y", v_618))));
var obj_628 = v_573;
V key_629 = (V)((V)R.Length(v_573) + (V)1.0d);
R.Set(obj_628, key_629, values_627[0]);
}
} else if (R.Truth(R.And((V)(R.Get(v_613, (V)"kind") == (V)"line_second_cell"), () => (V)(v_569 != (V)false)))) {
var values_630 = R.Join(new V[] {(V)(R.Get(R.Get(v_563, (V)"position"), (V)"x") + v_567)}, new Results((V)(R.Get(R.Get(v_563, (V)"position"), (V)"y") + v_568)));
V v_631 = values_630[0];
V v_632 = values_630[1];
var values_633 = R.Join(new V[] {(V)(v_631 + v_567)}, new Results((V)(v_632 + v_568)));
V v_634 = values_633[0];
V v_635 = values_633[1];
if (R.Truth(R.And(R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_577, v_631, v_632}, new Results(v_562))).First, () => R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_577, v_634, v_635}, new Results(v_562))).First))) {
var values_636 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_563, (V)"position"), (V)"floor_id")), new Entry((V)"x", v_634), new Entry((V)"y", v_635))));
var obj_637 = v_573;
V key_638 = (V)((V)R.Length(v_573) + (V)1.0d);
R.Set(obj_637, key_638, values_636[0]);
}
} else if (R.Truth(R.Or((V)(R.Get(v_613, (V)"kind") == (V)"branch_near"), () => (V)(R.Get(v_613, (V)"kind") == (V)"status_chain")))) {
var values_639 = Results.Empty;
V v_640 = values_639[0];
V v_641 = values_639[1];
foreach (var row_642 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_573)))))) {
V v_643 = row_642[0];
V v_644 = row_642[1];
var values_645 = R.Join(new V[] {}, R.Call(v_578, R.Join(new V[] {}, new Results(v_644))));
V v_646 = values_645[0];
if (R.Truth(v_646)) {
var values_647 = R.Join(new V[] {v_646}, new Results(v_643));
v_640 = values_647[0];
v_641 = values_647[1];

break;
}
}
if (R.Truth(R.And(v_640, () => R.Or((V)(R.Get(v_613, (V)"kind") == (V)"branch_near"), () => R.And(R.Get(v_640, (V)"statuses"), () => R.Get(R.Get(v_640, (V)"statuses"), (V)"burning")))))) {
while (R.Truth((V)((V)R.Length(v_573) > v_641))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {}, new Results(v_573)));
}
var values_648 = R.Join(new V[] {}, new Results(R.Table()));
V v_649 = values_648[0];
foreach (var row_650 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_562, (V)"entities"))))))) {
V v_651 = row_650[0];
V v_652 = row_650[1];
var values_653 = R.Join(new V[] {}, new Results(R.Get(v_652, (V)"position")));
V v_654 = values_653[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_652, (V)"id") != R.Get(v_640, (V)"id")), () => (V)(R.Get(v_652, (V)"id") != R.Get(v_563, (V)"id"))), () => R.Get(v_652, (V)"hp")), () => (V)(R.Get(v_652, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_652, (V)"faction") != R.Get(v_563, (V)"faction"))), () => v_654), () => (V)(R.Get(v_654, (V)"floor_id") == R.Get(R.Get(v_563, (V)"position"), (V)"floor_id"))), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_654, (V)"x") - R.Get(R.Get(v_640, (V)"position"), (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_654, (V)"y") - R.Get(R.Get(v_640, (V)"position"), (V)"y"))))))).First == (V)1.0d)), () => R.Or((V)(R.Get(v_613, (V)"kind") == (V)"branch_near"), () => R.And(R.Get(v_652, (V)"statuses"), () => R.Get(R.Get(v_652, (V)"statuses"), (V)"burning")))))) {
var values_655 = R.Join(new V[] {}, new Results(v_652));
var obj_656 = v_649;
V key_657 = (V)((V)R.Length(v_649) + (V)1.0d);
R.Set(obj_656, key_657, values_655[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_649}, new Results(new V((RuleFunction)(args_658 => {
V v_659 = args_658[0];
V v_660 = args_658[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_659, (V)"id") < R.Get(v_660, (V)"id"))));
return Results.Empty;
})))));
var values_661 = R.Join(new V[] {}, new Results((V)0.0d));
V v_662 = values_661[0];
foreach (var row_663 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_649)))))) {
V v_664 = row_663[0];
V v_665 = row_663[1];
var values_666 = R.Join(new V[] {}, new Results(R.Get(v_665, (V)"position")));
V v_667 = values_666[0];
var values_668 = R.Join(new V[] {}, new Results((V)(R.Get(v_667, (V)"x") - R.Get(R.Get(v_640, (V)"position"), (V)"x"))));
V v_669 = values_668[0];
var values_670 = R.Join(new V[] {}, new Results((V)(R.Get(v_667, (V)"y") - R.Get(R.Get(v_640, (V)"position"), (V)"y"))));
V v_671 = values_670[0];
if (R.Truth(R.Or(R.Or((V)(v_669 == (V)0.0d), () => (V)(v_671 == (V)0.0d)), () => R.And(R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_577, (V)(R.Get(R.Get(v_640, (V)"position"), (V)"x") + v_669), R.Get(R.Get(v_640, (V)"position"), (V)"y")}, new Results(v_562))).First, () => R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_577, R.Get(R.Get(v_640, (V)"position"), (V)"x"), (V)(R.Get(R.Get(v_640, (V)"position"), (V)"y") + v_671)}, new Results(v_562))).First)))) {
R.Call(v_586, R.Join(new V[] {R.Get(v_667, (V)"x")}, new Results(R.Get(v_667, (V)"y"))));
var values_672 = R.Join(new V[] {}, new Results((V)(v_662 + (V)1.0d)));
v_662 = values_672[0];
if (R.Truth(R.And((V)(R.Get(v_613, (V)"kind") == (V)"branch_near"), () => (V)(v_662 >= (V)2.0d)))) {
break;
}
}
}
var values_673 = R.Join(new V[] {}, new Results((V)"piercing_line"));
v_570 = values_673[0];
}
} else if (R.Truth((V)(R.Get(v_613, (V)"kind") == (V)"line_extend"))) {
var values_674 = R.Join(new V[] {}, new Results(R.Get(v_573, (V)R.Length(v_573))));
V v_675 = values_674[0];
var values_676 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_563, (V)"position"), (V)"x") + (V)((V)5.0d * v_567))));
V v_677 = values_676[0];
var values_678 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_563, (V)"position"), (V)"y") + (V)((V)5.0d * v_568))));
V v_679 = values_678[0];
if (R.Truth(R.And(R.And(R.And(R.And(v_675, () => (V)(R.Get(v_675, (V)"x") == v_677)), () => (V)(R.Get(v_675, (V)"y") == v_679)), () => R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_577, v_677, v_679}, new Results(v_562))).First), () => R.Or(R.Or((V)(v_567 == (V)0.0d), () => (V)(v_568 == (V)0.0d)), () => R.And(R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_577, (V)(v_677 + v_567), v_679}, new Results(v_562))).First, () => R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_577, v_677, (V)(v_679 + v_568)}, new Results(v_562))).First))))) {
R.Call(v_586, R.Join(new V[] {(V)(v_677 + v_567)}, new Results((V)(v_679 + v_568))));
}
} else if (R.Truth((V)(R.Get(v_613, (V)"kind") == (V)"area_expand"))) {
var values_680 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_563, (V)"position"), (V)"x") + (V)((V)2.0d * v_567))));
V v_681 = values_680[0];
var values_682 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_563, (V)"position"), (V)"y") + (V)((V)2.0d * v_568))));
V v_683 = values_682[0];
var values_684 = R.Join(new V[] {}, new Results((V)false));
V v_685 = values_684[0];
foreach (var row_686 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_573)))))) {
V v_687 = row_686[0];
V v_688 = row_686[1];
if (R.Truth(R.And((V)(R.Get(v_688, (V)"x") == v_681), () => (V)(R.Get(v_688, (V)"y") == v_683)))) {
var values_689 = R.Join(new V[] {}, new Results((V)true));
v_685 = values_689[0];

break;
}
}
if (R.Truth(R.And(v_685, () => R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_577, v_681, v_683}, new Results(v_562))).First))) {
{ double end_692 = R.Num((V)2.0d), step_693 = R.Num((V)1.0d);
for (double i_691 = R.Num((-(V)2.0d)); step_693 > 0 ? i_691 <= end_692 : i_691 >= end_692; i_691 += step_693) {
V v_690 = i_691;
{ double end_696 = R.Num((V)2.0d), step_697 = R.Num((V)1.0d);
for (double i_695 = R.Num((-(V)2.0d)); step_697 > 0 ? i_695 <= end_696 : i_695 >= end_696; i_695 += step_697) {
V v_694 = i_695;
if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_694))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_690))))).First == (V)2.0d))) {
R.Call(v_586, R.Join(new V[] {(V)(v_681 + v_694)}, new Results((V)(v_683 + v_690))));
}
}
}
}
}
}
} else if (R.Truth((V)(R.Get(v_613, (V)"kind") == (V)"shockwave"))) {
var values_698 = R.Join(new V[] {}, new Results(R.Get(v_573, (V)1.0d)));
V v_699 = values_698[0];
if (R.Truth(v_699)) {
foreach (var row_700 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_701 = row_700[0];
V v_702 = row_700[1];
R.Call(v_586, R.Join(new V[] {(V)(R.Get(v_699, (V)"x") + R.Get(v_702, (V)1.0d))}, new Results((V)(R.Get(v_699, (V)"y") + R.Get(v_702, (V)2.0d)))));
}
}
} else if (R.Truth((V)(R.Get(v_613, (V)"kind") == (V)"add_damage"))) {
var values_703 = R.Join(new V[] {}, new Results((V)(v_574 + v_610)));
v_574 = values_703[0];
}
}
}
}
}
return R.Join(new V[] {v_573, v_574, v_570}, new Results(v_575));
return Results.Empty;
})));
V v_704 = V.Nil;
v_704 = new V((RuleFunction)(args_705 => {
V v_706 = args_705[0];
V v_707 = args_705[1];
var values_708 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_706, (V)"chain_history"), v_707)));
V v_709 = values_708[0];
if (R.Truth((V)(!R.Truth(v_709)))) {
var values_710 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"damages", R.Table()), new Entry((V)"pairs", R.Table()), new Entry((V)"groups", R.Table()), new Entry((V)"windups", R.Table()))));
v_709 = values_710[0];
var values_711 = R.Join(new V[] {}, new Results(v_709));
var obj_712 = R.Get(v_706, (V)"chain_history");
V key_713 = v_707;
R.Set(obj_712, key_713, values_711[0]);
}
return R.Join(new V[] {}, new Results(v_709));
return Results.Empty;
}));
V v_714 = V.Nil;
v_714 = new V((RuleFunction)(args_715 => {
V v_716 = args_715[0];
V v_717 = args_715[1];
foreach (var row_718 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_716, () => R.Table()))))))) {
V v_719 = row_718[0];
V v_720 = row_718[1];
if (R.Truth((V)(v_720 == v_717))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
V v_721 = V.Nil;
v_721 = new V((RuleFunction)(args_722 => {
V v_723 = args_722[0];
V v_724 = args_722[1];
var values_725 = R.Join(new V[] {}, new Results(R.Table()));
V v_726 = values_725[0];
foreach (var row_727 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_723, (V)"ancestry"), () => R.Table()))))))) {
V v_728 = row_727[0];
V v_729 = row_727[1];
var values_730 = R.Join(new V[] {}, new Results(v_729));
var obj_731 = v_726;
V key_732 = (V)((V)R.Length(v_726) + (V)1.0d);
R.Set(obj_731, key_732, values_730[0]);
}
var values_733 = R.Join(new V[] {}, new Results(v_724));
var obj_734 = v_726;
V key_735 = (V)((V)R.Length(v_726) + (V)1.0d);
R.Set(obj_734, key_735, values_733[0]);
return R.Join(new V[] {}, new Results(v_726));
return Results.Empty;
}));
V v_736 = V.Nil;
v_736 = new V((RuleFunction)(args_737 => {
V v_738 = args_737[0];
if (R.Truth((V)(R.Get(v_738, (V)"op") == (V)"pair_adjacent_damage"))) {
return R.Join(new V[] {}, new Results(R.Get(v_738, (V)"neighborhood")));
}
foreach (var row_739 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Or(R.Get(v_738, (V)"all"), () => R.Get(v_738, (V)"any")), () => R.Table()))))))) {
V v_740 = row_739[0];
V v_741 = row_739[1];
var values_742 = R.Join(new V[] {}, R.Call(v_736, R.Join(new V[] {}, new Results(v_741))));
V v_743 = values_742[0];
if (R.Truth(v_743)) {
return R.Join(new V[] {}, new Results(v_743));
}
}
return R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_738, (V)"not_"), () => R.Call(v_736, R.Join(new V[] {}, new Results(R.Get(v_738, (V)"not_")))).First), () => V.Nil)));
return Results.Empty;
}));
R.Set(v_18, (V)"on_event", new V((RuleFunction)(args_744 => {
V v_745 = args_744[0];
V v_746 = args_744[1];
V v_747 = args_744[2];
V v_748 = args_744[3];
if (R.Truth(R.And(R.Get(v_746, (V)"fate_miss"), () => R.Or((V)(R.Get(v_746, (V)"kind") == (V)"AttackPerformed"), () => (V)(R.Get(v_746, (V)"kind") == (V)"AbilityUsed"))))) {
return Results.Empty;
}
if (R.Truth(R.And(R.Get(v_746, (V)"oath_shared_only"), () => (V)(R.Get(v_746, (V)"kind") != (V)"EntityKilled")))) {
return Results.Empty;
}
var values_749 = R.Join(new V[] {}, new Results(R.Get(v_22, R.Get(v_746, (V)"kind"))));
V v_750 = values_749[0];
if (R.Truth((V)(!R.Truth(v_750)))) {
return Results.Empty;
}
var values_751 = R.Join(new V[] {}, R.Call(v_704, R.Join(new V[] {v_745}, new Results(R.Get(v_746, (V)"chain_id")))));
V v_752 = values_751[0];
foreach (var row_753 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_745, (V)"entities"))))))) {
V v_754 = row_753[0];
V v_755 = row_753[1];
foreach (var row_756 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_750)))))) {
V v_757 = row_756[0];
V v_758 = row_756[1];
var values_759 = R.Join(new V[] {}, new Results(R.And(R.Get(v_755, (V)"boons"), () => R.Get(R.Get(v_755, (V)"boons"), v_758))));
V v_760 = values_759[0];
if (R.Truth(v_760)) {
var values_761 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_758))));
V v_762 = values_761[0];
var values_763 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_755, (V)"id"), R.Concat((V)":", v_758))));
V v_764 = values_763[0];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Call(v_714, R.Join(new V[] {R.Get(v_746, (V)"ancestry")}, new Results(v_764))).First)), () => R.Call(v_66, R.Join(new V[] {v_755, R.Get(v_762, (V)"scope")}, new Results(v_746))).First), () => R.Call(v_118, R.Join(new V[] {R.Get(v_762, (V)"predicate"), v_745, v_755, v_758, v_746}, new Results(v_752))).First))) {
var values_765 = R.Join(new V[] {}, new Results(V.Nil));
V v_766 = values_765[0];
var values_767 = R.Join(new V[] {}, R.Call(v_736, R.Join(new V[] {}, new Results(R.Get(v_762, (V)"predicate")))));
V v_768 = values_767[0];
if (R.Truth(v_768)) {
var values_769 = R.Join(new V[] {}, R.Call(v_93, R.Join(new V[] {v_745, v_755, v_758, v_746, v_752}, new Results(v_768))));
v_766 = values_769[0];
}
var values_770 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_762, (V)"stack_policy") == (V)"chance_overflow"), () => R.Call(R.Get(v_18, (V)"proc_count"), R.Join(new V[] {v_745, R.Call(v_247, R.Join(new V[] {v_762, v_760}, new Results((V)"chance"))).First}, new Results(R.Or(R.Get(v_746, (V)"proc_coefficient"), () => (V)1.0d)))).First), () => (V)1.0d)));
V v_771 = values_770[0];
var values_772 = R.Join(new V[] {}, new Results(R.Or(R.And(v_766, () => (V)R.Length(v_766)), () => (V)1.0d)));
V v_773 = values_772[0];
{ double end_776 = R.Num(v_773), step_777 = R.Num((V)1.0d);
for (double i_775 = R.Num((V)1.0d); step_777 > 0 ? i_775 <= end_776 : i_775 >= end_776; i_775 += step_777) {
V v_774 = i_775;
var values_778 = R.Join(new V[] {}, new Results(R.Or(R.And(v_766, () => R.Get(v_766, v_774)), () => V.Nil)));
V v_779 = values_778[0];
if (R.Truth(v_779)) {
var values_780 = R.Join(new V[] {}, new Results((V)true));
var obj_781 = R.Get(v_752, (V)"pairs");
V key_782 = R.Get(v_779, (V)"key");
R.Set(obj_781, key_782, values_780[0]);
}
{ double end_785 = R.Num(v_771), step_786 = R.Num((V)1.0d);
for (double i_784 = R.Num((V)1.0d); step_786 > 0 ? i_784 <= end_785 : i_784 >= end_785; i_784 += step_786) {
V v_783 = i_784;
var values_787 = R.Join(new V[] {}, new Results(R.Get(v_745, (V)"next_activation_id")));
V v_788 = values_787[0];
var values_789 = R.Join(new V[] {}, new Results((V)(v_788 + (V)1.0d)));
var obj_790 = v_745;
V key_791 = (V)"next_activation_id";
R.Set(obj_790, key_791, values_789[0]);
var values_792 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_788), new Entry((V)"boon_id", v_758), new Entry((V)"owner_id", R.Get(v_755, (V)"id")), new Entry((V)"source_faction", R.Get(v_755, (V)"faction")), new Entry((V)"source_position", R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_755, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_755, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_755, (V)"position"), (V)"y")))), new Entry((V)"chain_id", R.Get(v_746, (V)"chain_id")), new Entry((V)"root_action_id", R.Get(v_746, (V)"root_action_id")), new Entry((V)"parent_event_id", R.Get(v_746, (V)"id")), new Entry((V)"ancestry", R.Call(v_721, R.Join(new V[] {v_746}, new Results(v_764))).First), new Entry((V)"power", R.Call(v_247, R.Join(new V[] {v_762, v_760}, new Results((V)"power"))).First), new Entry((V)"event", v_746), new Entry((V)"pair_target_id", R.And(v_779, () => R.Get(R.Get(v_779, (V)"prior"), (V)"target_id"))))));
V v_793 = values_792[0];
R.Call(v_748, R.Join(new V[] {}, new Results(v_793)));
foreach (var row_794 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_762, (V)"effects"))))))) {
V v_795 = row_794[0];
V v_796 = row_794[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_796, (V)"when"))), () => R.Call(v_118, R.Join(new V[] {R.Get(v_796, (V)"when"), v_745, v_755, v_758, v_746}, new Results(v_752))).First))) {
R.Call(v_747, R.Join(new V[] {v_793}, new Results(v_796)));
}
}
}
}
}
}
}
}
}
}
if (R.Truth(R.And((V)(R.Get(v_746, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_746, (V)"actual_loss") > (V)0.0d)))) {
var values_797 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"target_id", R.Get(v_746, (V)"target_id")), new Entry((V)"target_faction", R.Get(v_746, (V)"target_faction")), new Entry((V)"floor_id", R.Get(v_746, (V)"floor_id")), new Entry((V)"x", R.Get(v_746, (V)"x")), new Entry((V)"y", R.Get(v_746, (V)"y")))));
var obj_798 = R.Get(v_752, (V)"damages");
V key_799 = (V)((V)R.Length(R.Get(v_752, (V)"damages")) + (V)1.0d);
R.Set(obj_798, key_799, values_797[0]);
}
return Results.Empty;
})));
R.Set(v_18, (V)"prune_histories", new V((RuleFunction)(args_800 => {
V v_801 = args_800[0];
var values_802 = R.Join(new V[] {}, new Results(R.Table()));
V v_803 = values_802[0];
foreach (var row_804 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_801, (V)"delayed_effects"))))))) {
V v_805 = row_804[0];
V v_806 = row_804[1];
var values_807 = R.Join(new V[] {}, new Results((V)true));
var obj_808 = v_803;
V key_809 = R.Get(R.Get(v_806, (V)"activation"), (V)"chain_id");
R.Set(obj_808, key_809, values_807[0]);
}
foreach (var row_810 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_801, (V)"entities"))))))) {
V v_811 = row_810[0];
V v_812 = row_810[1];
foreach (var row_813 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_812, (V)"statuses"), () => R.Table()))))))) {
V v_814 = row_813[0];
V v_815 = row_813[1];
if (R.Truth(R.Get(v_815, (V)"activation"))) {
var values_816 = R.Join(new V[] {}, new Results((V)true));
var obj_817 = v_803;
V key_818 = R.Get(R.Get(v_815, (V)"activation"), (V)"chain_id");
R.Set(obj_817, key_818, values_816[0]);
}
}
}
foreach (var row_819 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_801, (V)"chain_history"))))))) {
V v_820 = row_819[0];
if (R.Truth((V)(!R.Truth(R.Get(v_803, v_820))))) {
var values_821 = R.Join(new V[] {}, new Results(V.Nil));
var obj_822 = R.Get(v_801, (V)"chain_history");
V key_823 = v_820;
R.Set(obj_822, key_823, values_821[0]);
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_18));
return Results.Empty;
} }
