// Static C# port of src/visual_juice.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 6c86d2c79d923ce1ab87f8d0267d1619e981d3cb69ba6160e5abc883c664620a
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visual_juice { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"reward_seconds", (V)0.48d), new Entry((V)"reward_limit", (V)16.0d), new Entry((V)"attack_seconds", (V)0.12d), new Entry((V)"impact_seconds", (V)0.18d), new Entry((V)"crumble_seconds", (V)0.24d), new Entry((V)"celebration_seconds", (V)0.8d), new Entry((V)"impact_limit", (V)16.0d), new Entry((V)"crumble_limit", (V)8.0d), new Entry((V)"celebration_limit", (V)4.0d))));
V v_4 = values_3[0];
V v_5 = V.Nil;
v_5 = new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_7 + (V)0.5d)))));
return Results.Empty;
}));
V v_8 = V.Nil;
v_8 = new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_10, (V)"floor_id"), () => R.And(R.Get(v_10, (V)"source_position"), () => R.Get(R.Get(v_10, (V)"source_position"), (V)"floor_id")))));
return Results.Empty;
}));
R.Set(v_4, (V)"attack_offset", new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
V v_13 = args_11[1];
V v_14 = args_11[2];
V v_15 = args_11[3];
var values_16 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_12, (V)"items"), (V)1.0d)));
V v_17 = values_16[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_15)), () => (V)(!R.Truth(v_17))), () => R.And(R.Get(v_17, (V)"motions"), () => R.Get(R.Get(v_17, (V)"motions"), v_13))))) {
return R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
}
var values_18 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_4, (V)"attack_seconds")}, new Results(R.Get(v_17, (V)"duration")))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, new Results((V)(R.Get(v_17, (V)"duration") - R.Get(v_12, (V)"remaining"))));
V v_21 = values_20[0];
if (R.Truth(R.Or(R.Or((V)(v_19 <= (V)0.0d), () => (V)(v_21 <= (V)0.0d)), () => (V)(v_21 >= v_19)))) {
return R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
}
foreach (var row_22 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_2, (V)"current_pose_events"), R.Join(new V[] {}, new Results(v_12))).First, () => R.Table()))))))) {
V v_23 = row_22[0];
V v_24 = row_22[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_24, (V)"kind") == (V)"AttackPerformed"), () => (V)(R.Get(v_24, (V)"source_id") == v_13)), () => (V)(R.Call(v_8, R.Join(new V[] {}, new Results(v_24))).First == v_14)))) {
var values_25 = R.Join(new V[] {}, new Results(R.Get(v_24, (V)"source_position")));
V v_26 = values_25[0];
var values_27 = R.Join(new V[] {R.Or(R.Get(v_24, (V)"from_x"), () => R.And(v_26, () => R.Get(v_26, (V)"x")))}, new Results(R.Or(R.Get(v_24, (V)"from_y"), () => R.And(v_26, () => R.Get(v_26, (V)"y")))));
V v_28 = values_27[0];
V v_29 = values_27[1];
if (R.Truth(R.And(v_28, () => v_29))) {
var values_30 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_31 = values_30[0];
V v_32 = values_30[1];
foreach (var row_33 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Or(R.Get(v_24, (V)"aim_cells"), () => R.Get(v_24, (V)"target_cells")), () => R.Table()))))))) {
V v_34 = row_33[0];
V v_35 = row_33[1];
var values_36 = R.Join(new V[] {(V)((V)(v_31 + R.Get(v_35, (V)"x")) - v_28)}, new Results((V)((V)(v_32 + R.Get(v_35, (V)"y")) - v_29)));
v_31 = values_36[0];
v_32 = values_36[1];
}
var values_37 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_31))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_32))))));
V v_38 = values_37[0];
if (R.Truth((V)(v_38 > (V)0.0d))) {
var values_39 = R.Join(new V[] {}, new Results((V)((V)3.0d * R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results((V)((V)(R.Get(rt.Global("math"), (V)"pi") * v_21) / v_19)))).First)));
V v_40 = values_39[0];
return R.Join(new V[] {R.Call(v_5, R.Join(new V[] {}, new Results((V)((V)(v_40 * v_31) / v_38)))).First}, R.Call(v_5, R.Join(new V[] {}, new Results((V)((V)(v_40 * v_32) / v_38)))));
}
}
}
}
return R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
return Results.Empty;
})));
V v_41 = V.Nil;
v_41 = new V((RuleFunction)(args_42 => {
V v_43 = args_42[0];
V v_44 = args_42[1];
V v_45 = args_42[2];
var values_46 = R.Join(new V[] {}, new Results(v_44));
var obj_47 = v_43;
V key_48 = (V)((V)R.Length(v_43) + (V)1.0d);
R.Set(obj_47, key_48, values_46[0]);
if (R.Truth((V)((V)R.Length(v_43) > v_45))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {v_43}, new Results((V)1.0d)));
}
return Results.Empty;
}));
V v_49 = V.Nil;
v_49 = new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
V v_52 = args_50[1];
V v_53 = args_50[2];
V v_54 = args_50[3];
V v_55 = args_50[4];
V v_56 = args_50[5];
foreach (var row_57 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_51, (V)"celebrations"))))))) {
V v_58 = row_57[0];
V v_59 = row_57[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_59, (V)"kind") == v_52), () => (V)(R.Get(v_59, (V)"x") == v_53)), () => (V)(R.Get(v_59, (V)"y") == v_54)))) {
var values_60 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Or(R.Get(v_59, (V)"level"), () => (V)0.0d)}, new Results(R.Or(v_56, () => (V)0.0d)))));
var obj_61 = v_59;
V key_62 = (V)"level";
R.Set(obj_61, key_62, values_60[0]);

var values_63 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_64 = v_59;
V key_65 = (V)"age";
R.Set(obj_64, key_65, values_63[0]);

return Results.Empty;
}
}
R.Call(v_41, R.Join(new V[] {R.Get(v_51, (V)"celebrations"), R.Table(new Entry((V)"kind", v_52), new Entry((V)"x", v_53), new Entry((V)"y", v_54), new Entry((V)"floor_id", v_55), new Entry((V)"level", v_56), new Entry((V)"age", (V)0.0d))}, new Results(R.Get(v_4, (V)"celebration_limit"))));
return Results.Empty;
}));
V v_66 = V.Nil;
v_66 = new V((RuleFunction)(args_67 => {
V v_68 = args_67[0];
V v_69 = args_67[1];
V v_70 = args_67[2];
V v_71 = args_67[3];
V v_72 = args_67[4];
var values_73 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_74 = values_73[0];
V v_75 = values_73[1];
foreach (var row_76 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_69, (V)"events"), () => R.Table(new Entry((V)1, R.Get(v_69, (V)"event")))))))))) {
V v_77 = row_76[0];
V v_78 = row_76[1];
if (R.Truth(R.And(R.And((V)(R.Call(v_8, R.Join(new V[] {}, new Results(v_78))).First == v_72), () => R.Get(v_78, (V)"x")), () => R.Get(v_78, (V)"y")))) {
if (R.Truth(R.Or(R.And(R.And((V)(R.Get(v_78, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Or(R.Get(v_78, (V)"actual_loss"), () => (V)0.0d) > (V)0.0d)), () => R.Get(v_78, (V)"direct_attack")), () => (V)(R.Get(v_78, (V)"kind") == (V)"DamageBlocked")))) {
var values_79 = R.Join(new V[] {}, new Results(R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_78, (V)"target_id")))).First, R.Concat((V)":", R.Concat(R.Get(v_78, (V)"x"), R.Concat((V)":", R.Get(v_78, (V)"y")))))));
V v_80 = values_79[0];
if (R.Truth((V)(!R.Truth(R.Get(v_74, v_80))))) {
var values_81 = R.Join(new V[] {}, new Results((V)true));
var obj_82 = v_74;
V key_83 = v_80;
R.Set(obj_82, key_83, values_81[0]);
var values_84 = R.Join(new V[] {}, new Results(R.Get(v_78, (V)"source_position")));
V v_85 = values_84[0];
var values_86 = R.Join(new V[] {R.Or(R.And(v_85, () => (V)(R.Get(v_78, (V)"x") - R.Get(v_85, (V)"x"))), () => (V)0.0d)}, new Results(R.Or(R.And(v_85, () => (V)(R.Get(v_78, (V)"y") - R.Get(v_85, (V)"y"))), () => (V)0.0d)));
V v_87 = values_86[0];
V v_88 = values_86[1];
var values_89 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"sqrt"), R.Join(new V[] {}, new Results((V)((V)(v_87 * v_87) + (V)(v_88 * v_88))))));
V v_90 = values_89[0];
if (R.Truth((V)(v_90 > (V)0.0d))) {
var values_91 = R.Join(new V[] {(V)(v_87 / v_90)}, new Results((V)(v_88 / v_90)));
v_87 = values_91[0];
v_88 = values_91[1];
}
R.Call(v_41, R.Join(new V[] {R.Get(v_68, (V)"impacts"), R.Table(new Entry((V)"kind", R.Or(R.And((V)(R.Get(v_78, (V)"kind") == (V)"DamageBlocked"), () => (V)"blocked"), () => (V)"hit")), new Entry((V)"x", R.Get(v_78, (V)"x")), new Entry((V)"y", R.Get(v_78, (V)"y")), new Entry((V)"dx", v_87), new Entry((V)"dy", v_88), new Entry((V)"age", (V)0.0d), new Entry((V)"floor_id", v_72), new Entry((V)"friendly", (V)(R.Get(v_78, (V)"source_id") == v_71)))}, new Results(R.Get(v_4, (V)"impact_limit"))));
}
} else if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_78, (V)"kind") == (V)"Died"), () => (V)(R.Get(v_78, (V)"target_faction") == (V)"enemy")), () => R.Get(v_78, (V)"target_definition_id")), () => (V)(!R.Truth(R.Get(v_75, R.Get(v_78, (V)"target_id"))))))) {
var values_92 = R.Join(new V[] {}, new Results((V)true));
var obj_93 = v_75;
V key_94 = R.Get(v_78, (V)"target_id");
R.Set(obj_93, key_94, values_92[0]);
R.Call(v_41, R.Join(new V[] {R.Get(v_68, (V)"crumbles"), R.Table(new Entry((V)"binding", R.Get(v_78, (V)"target_definition_id")), new Entry((V)"x", R.Get(v_78, (V)"x")), new Entry((V)"y", R.Get(v_78, (V)"y")), new Entry((V)"floor_id", v_72), new Entry((V)"age", (V)0.0d), new Entry((V)"flip", (V)(R.Call(R.Get(v_2, (V)"facing"), R.Join(new V[] {v_70}, new Results(R.Get(v_78, (V)"target_id")))).First == (-(V)1.0d))))}, new Results(R.Get(v_4, (V)"crumble_limit"))));
} else if (R.Truth((V)(R.Get(v_78, (V)"kind") == (V)"SecretDiscovered"))) {
R.Call(v_49, R.Join(new V[] {v_68, (V)"secret", R.Get(v_78, (V)"x"), R.Get(v_78, (V)"y")}, new Results(v_72)));
}
}
}
return Results.Empty;
}));
R.Set(v_4, (V)"new", new V((RuleFunction)(args_95 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"items", R.Table()), new Entry((V)"impacts", R.Table()), new Entry((V)"crumbles", R.Table()), new Entry((V)"celebrations", R.Table()), new Entry((V)"floor_id", V.Nil), new Entry((V)"active_item", V.Nil))));
return Results.Empty;
})));
R.Set(v_4, (V)"update", new V((RuleFunction)(args_96 => {
V v_97 = args_96[0];
V v_98 = args_96[1];
V v_99 = args_96[2];
V v_100 = args_96[3];
V v_101 = args_96[4];
V v_102 = args_96[5];
if (R.Truth(R.Or((V)(!R.Truth(v_100)), () => (V)(R.Get(v_97, (V)"floor_id") != v_99)))) {
var values_103 = R.Join(new V[] {}, new Results(R.Table()));
var obj_104 = v_97;
V key_105 = (V)"items";
R.Set(obj_104, key_105, values_103[0]);

var values_106 = R.Join(new V[] {}, new Results(R.Table()));
var obj_107 = v_97;
V key_108 = (V)"impacts";
R.Set(obj_107, key_108, values_106[0]);

var values_109 = R.Join(new V[] {}, new Results(R.Table()));
var obj_110 = v_97;
V key_111 = (V)"crumbles";
R.Set(obj_110, key_111, values_109[0]);

var values_112 = R.Join(new V[] {}, new Results(R.Table()));
var obj_113 = v_97;
V key_114 = (V)"celebrations";
R.Set(obj_113, key_114, values_112[0]);
var values_115 = R.Join(new V[] {}, new Results(V.Nil));
var obj_116 = v_97;
V key_117 = (V)"active_item";
R.Set(obj_116, key_117, values_115[0]);
}
var values_118 = R.Join(new V[] {}, new Results(v_99));
var obj_119 = v_97;
V key_120 = (V)"floor_id";
R.Set(obj_119, key_120, values_118[0]);
foreach (var row_121 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, R.Get(v_97, (V)"items")), new Entry((V)2, R.Get(v_4, (V)"reward_seconds")))), new Entry((V)2, R.Table(new Entry((V)1, R.Get(v_97, (V)"impacts")), new Entry((V)2, R.Get(v_4, (V)"impact_seconds")))), new Entry((V)3, R.Table(new Entry((V)1, R.Get(v_97, (V)"crumbles")), new Entry((V)2, R.Get(v_4, (V)"crumble_seconds")))), new Entry((V)4, R.Table(new Entry((V)1, R.Get(v_97, (V)"celebrations")), new Entry((V)2, R.Get(v_4, (V)"celebration_seconds"))))))))))) {
V v_122 = row_121[0];
V v_123 = row_121[1];
{ double end_126 = R.Num((V)1.0d), step_127 = R.Num((-(V)1.0d));
for (double i_125 = R.Num((V)R.Length(R.Get(v_123, (V)1.0d))); step_127 > 0 ? i_125 <= end_126 : i_125 >= end_126; i_125 += step_127) {
V v_124 = i_125;
var values_128 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_123, (V)1.0d), v_124)));
V v_129 = values_128[0];

var values_130 = R.Join(new V[] {}, new Results((V)(R.Get(v_129, (V)"age") + R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results(v_98))).First)));
var obj_131 = v_129;
V key_132 = (V)"age";
R.Set(obj_131, key_132, values_130[0]);
if (R.Truth((V)(R.Get(v_129, (V)"age") >= R.Get(v_123, (V)2.0d)))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_123, (V)1.0d)}, new Results(v_124)));
}
}
}
}
if (R.Truth(v_101)) {
var values_133 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_101, (V)"items"), (V)1.0d)));
V v_134 = values_133[0];
if (R.Truth((V)(v_134 != R.Get(v_97, (V)"active_item")))) {
var values_135 = R.Join(new V[] {}, new Results(v_134));
var obj_136 = v_97;
V key_137 = (V)"active_item";
R.Set(obj_136, key_137, values_135[0]);
if (R.Truth(R.And(v_100, () => v_134))) {
R.Call(v_66, R.Join(new V[] {v_97, v_134, v_101, v_102}, new Results(v_99)));
}
}
}
return Results.Empty;
})));
R.Set(v_4, (V)"add", new V((RuleFunction)(args_138 => {
V v_139 = args_138[0];
V v_140 = args_138[1];
V v_141 = args_138[2];
V v_142 = args_138[3];
V v_143 = args_138[4];
R.Call(R.Get(v_4, (V)"update"), R.Join(new V[] {v_139, (V)0.0d, v_142}, new Results(v_143)));
if (R.Truth((V)(!R.Truth(v_143)))) {
return Results.Empty;
}
foreach (var row_144 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_140)))))) {
V v_145 = row_144[0];
V v_146 = row_144[1];
if (R.Truth(R.And((V)(R.Get(v_146, (V)"source_id") == v_141), () => (V)(R.Call(v_8, R.Join(new V[] {}, new Results(v_146))).First == v_142)))) {
var values_147 = Results.Empty;
V v_148 = values_147[0];
V v_149 = values_147[1];
if (R.Truth(R.And((V)(R.Get(v_146, (V)"kind") == (V)"CurrencyCollected"), () => (V)(R.Get(v_146, (V)"amount") > (V)0.0d)))) {
var values_150 = R.Join(new V[] {(V)"core:drop/currency"}, new Results((V)"gold"));
v_148 = values_150[0];
v_149 = values_150[1];
} else if (R.Truth(R.And((V)(R.Get(v_146, (V)"kind") == (V)"SupplyCollected"), () => (V)(R.Or(R.Get(v_146, (V)"actual_gain"), () => (V)0.0d) > (V)0.0d)))) {
var values_151 = R.Join(new V[] {R.Concat((V)"core:drop/", R.Get(v_146, (V)"supply_kind"))}, new Results((V)"health"));
v_148 = values_151[0];
v_149 = values_151[1];
} else if (R.Truth((V)(R.Get(v_146, (V)"kind") == (V)"UtilityCollected"))) {
var values_152 = R.Join(new V[] {R.Concat((V)"core:object/interaction_", R.Get(v_146, (V)"utility_kind"))}, new Results((V)"utility"));
v_148 = values_152[0];
v_149 = values_152[1];
} else if (R.Truth(R.Or((V)(R.Get(v_146, (V)"kind") == (V)"WardFlaskCollected"), () => (V)(R.Get(v_146, (V)"kind") == (V)"VaultKeyCollected")))) {
var values_153 = R.Join(new V[] {R.Concat((V)"core:object/review_", R.Or(R.And((V)(R.Get(v_146, (V)"kind") == (V)"WardFlaskCollected"), () => (V)"ward"), () => (V)"key"))}, new Results((V)"utility"));
v_148 = values_153[0];
v_149 = values_153[1];
} else if (R.Truth(R.Or((V)(R.Get(v_146, (V)"kind") == (V)"ChestClaimed"), () => R.And((V)(R.Get(v_146, (V)"kind") == (V)"BoonGranted"), () => (V)(!R.Truth(R.Get(v_146, (V)"chest_id"))))))) {
var values_154 = R.Join(new V[] {R.Concat(R.Get(v_146, (V)"boon_id"), (V)"/icon")}, new Results((V)"boon"));
v_148 = values_154[0];
v_149 = values_154[1];
}
var values_155 = R.Join(new V[] {}, new Results(R.Get(v_146, (V)"source_position")));
V v_156 = values_155[0];
var values_157 = R.Join(new V[] {R.Or(R.Get(v_146, (V)"x"), () => R.And(v_156, () => R.Get(v_156, (V)"x")))}, new Results(R.Or(R.Get(v_146, (V)"y"), () => R.And(v_156, () => R.Get(v_156, (V)"y")))));
V v_158 = values_157[0];
V v_159 = values_157[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_146, (V)"kind") == (V)"LevelUp"), () => v_158), () => v_159))) {
R.Call(v_49, R.Join(new V[] {v_139, (V)"level", v_158, v_159, v_142}, new Results(R.Get(v_146, (V)"new_level"))));
}
if (R.Truth(R.And(R.And(v_148, () => v_158), () => v_159))) {
var values_160 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"binding", v_148), new Entry((V)"kind", v_149), new Entry((V)"x", v_158), new Entry((V)"y", v_159), new Entry((V)"floor_id", v_142), new Entry((V)"age", (V)0.0d), new Entry((V)"chest_id", R.Or(R.And((V)(R.Get(v_146, (V)"kind") == (V)"ChestClaimed"), () => R.Get(v_146, (V)"chest_id")), () => V.Nil)))));
var obj_161 = R.Get(v_139, (V)"items");
V key_162 = (V)((V)R.Length(R.Get(v_139, (V)"items")) + (V)1.0d);
R.Set(obj_161, key_162, values_160[0]);
if (R.Truth((V)((V)R.Length(R.Get(v_139, (V)"items")) > R.Get(v_4, (V)"reward_limit")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_139, (V)"items")}, new Results((V)1.0d)));
}
}
}
}
return Results.Empty;
})));
R.Set(v_4, (V)"impact_pose", new V((RuleFunction)(args_163 => {
V v_164 = args_163[0];
V v_165 = args_163[1];
V v_166 = args_163[2];
var values_167 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_164, (V)"age") / R.Get(v_4, (V)"impact_seconds"))))));
V v_168 = values_167[0];
var values_169 = R.Join(new V[] {}, new Results((V)((V)(v_165 - (V)3.0d) * (V)0.48d)));
V v_170 = values_169[0];
var values_171 = R.Join(new V[] {R.Get(v_164, (V)"dx")}, new Results(R.Get(v_164, (V)"dy")));
V v_172 = values_171[0];
V v_173 = values_171[1];
if (R.Truth(R.And((V)(v_172 == (V)0.0d), () => (V)(v_173 == (V)0.0d)))) {
var values_174 = R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"cos"), R.Join(new V[] {}, new Results((V)((V)(v_165 * R.Get(rt.Global("math"), (V)"pi")) * (V)0.4d)))).First}, R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results((V)((V)(v_165 * R.Get(rt.Global("math"), (V)"pi")) * (V)0.4d)))));
v_172 = values_174[0];
v_173 = values_174[1];
}
var values_175 = R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"cos"), R.Join(new V[] {}, new Results(v_170))).First}, R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results(v_170))));
V v_176 = values_175[0];
V v_177 = values_175[1];
var values_178 = R.Join(new V[] {}, new Results((V)(v_166 * (V)((V)0.08d + (V)((V)0.3d * v_168)))));
V v_179 = values_178[0];
return R.Join(new V[] {R.Call(v_5, R.Join(new V[] {}, new Results((V)((V)((V)(v_172 * v_176) - (V)(v_173 * v_177)) * v_179)))).First, R.Call(v_5, R.Join(new V[] {}, new Results((V)((V)((V)(v_172 * v_177) + (V)(v_173 * v_176)) * v_179)))).First, R.Or(R.And((V)(v_168 < (V)0.55d), () => (V)2.0d), () => (V)1.0d)}, new Results((V)Math.Pow(R.Num((V)((V)1.0d - v_168)), R.Num((V)2.0d))));
return Results.Empty;
})));
R.Set(v_4, (V)"ring_pose", new V((RuleFunction)(args_180 => {
V v_181 = args_180[0];
V v_182 = args_180[1];
var values_183 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_181, (V)"age") / R.Get(v_4, (V)"impact_seconds"))))));
V v_184 = values_183[0];
return R.Join(new V[] {R.Call(v_5, R.Join(new V[] {}, new Results((V)(v_182 * (V)((V)0.12d + (V)((V)0.22d * v_184)))))).First}, new Results((V)Math.Pow(R.Num((V)((V)1.0d - v_184)), R.Num((V)2.0d))));
return Results.Empty;
})));
R.Set(v_4, (V)"celebration_pose", new V((RuleFunction)(args_185 => {
V v_186 = args_185[0];
V v_187 = args_185[1];
var values_188 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_186, (V)"age") / R.Get(v_4, (V)"celebration_seconds"))))));
V v_189 = values_188[0];
return R.Join(new V[] {R.Call(v_5, R.Join(new V[] {}, new Results((V)(v_187 * (V)((V)0.25d + (V)((V)0.48d * v_189)))))).First, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)((V)((V)1.0d - v_189) * (V)2.0d)))).First}, R.Call(v_5, R.Join(new V[] {}, new Results((V)((V)10.0d * v_189)))));
return Results.Empty;
})));
R.Set(v_4, (V)"crumble_piece", new V((RuleFunction)(args_190 => {
V v_191 = args_190[0];
V v_192 = args_190[1];
V v_193 = args_190[2];
var values_194 = R.Join(new V[] {(V)((V)(v_192 - (V)1.0d) % (V)3.0d)}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_192 - (V)1.0d) / (V)3.0d)))));
V v_195 = values_194[0];
V v_196 = values_194[1];
var values_197 = R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_195 * v_193) / (V)3.0d)))).First}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_196 * v_193) / (V)3.0d)))));
V v_198 = values_197[0];
V v_199 = values_197[1];
var values_200 = R.Join(new V[] {(V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)(v_195 + (V)1.0d) * v_193) / (V)3.0d)))).First - v_198)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)(v_196 + (V)1.0d) * v_193) / (V)3.0d)))).First - v_199)));
V v_201 = values_200[0];
V v_202 = values_200[1];
var values_203 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_191, (V)"age") / R.Get(v_4, (V)"crumble_seconds"))))));
V v_204 = values_203[0];
return R.Join(new V[] {v_198, v_199, v_201, v_202, R.Call(v_5, R.Join(new V[] {}, new Results((V)((V)((V)(v_195 - (V)1.0d) * (V)7.0d) * v_204)))).First, R.Call(v_5, R.Join(new V[] {}, new Results((V)((V)((V)((V)(v_196 - (V)1.0d) * (V)5.0d) * v_204) + (V)((V)((V)3.0d * v_204) * v_204))))).First}, new Results((V)Math.Pow(R.Num((V)((V)1.0d - v_204)), R.Num((V)2.0d))));
return Results.Empty;
})));
R.Set(v_4, (V)"chest_offset", new V((RuleFunction)(args_205 => {
V v_206 = args_205[0];
V v_207 = args_205[1];
V v_208 = args_205[2];
if (R.Truth(v_208)) {
foreach (var row_209 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_206, (V)"items"))))))) {
V v_210 = row_209[0];
V v_211 = row_209[1];
if (R.Truth(R.And((V)(R.Get(v_211, (V)"chest_id") == v_207), () => (V)(R.Get(v_211, (V)"age") < (V)0.22d)))) {
return R.Join(new V[] {}, new Results((-R.Call(v_5, R.Join(new V[] {}, new Results((V)((V)4.0d * R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results((V)((V)(R.Get(rt.Global("math"), (V)"pi") * R.Get(v_211, (V)"age")) / (V)0.22d)))).First)))).First)));
}
}
}
return R.Join(new V[] {}, new Results((V)0.0d));
return Results.Empty;
})));
R.Set(v_4, (V)"reward_pose", new V((RuleFunction)(args_212 => {
V v_213 = args_212[0];
V v_214 = args_212[1];
V v_215 = args_212[2];
V v_216 = args_212[3];
V v_217 = args_212[4];
var values_218 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(R.Get(v_213, (V)"age") / R.Get(v_4, (V)"reward_seconds"))))))));
V v_219 = values_218[0];
var values_220 = R.Join(new V[] {}, new Results((V)((V)(v_219 * v_219) * (V)((V)3.0d - (V)((V)2.0d * v_219)))));
V v_221 = values_220[0];
var values_222 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results((V)(R.Get(rt.Global("math"), (V)"pi") * v_219)))));
V v_223 = values_222[0];
var values_224 = R.Join(new V[] {}, new Results((V)((V)(R.Or(R.And((V)(R.Get(v_213, (V)"kind") == (V)"boon"), () => (V)24.0d), () => (V)18.0d) * (V)((V)1.0d - (V)((V)0.5d * v_221))) + (V)((V)2.0d * v_223))));
V v_225 = values_224[0];
return R.Join(new V[] {R.Call(v_5, R.Join(new V[] {}, new Results((V)(v_214 + (V)((V)(v_216 - v_214) * v_221))))).First, R.Call(v_5, R.Join(new V[] {}, new Results((V)((V)(v_215 + (V)((V)(v_217 - v_215) * v_221)) - (V)((V)30.0d * v_223))))).First, R.Call(v_5, R.Join(new V[] {}, new Results(v_225))).First}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)((V)((V)1.0d - v_219) / (V)0.3d)))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
