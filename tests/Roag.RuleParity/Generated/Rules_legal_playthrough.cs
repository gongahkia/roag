// Static C# port of tests/legal_playthrough.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 86c3045413df399d74690c942a70c5eb86d8541f7946e178779dcd7f813da287
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_legal_playthrough { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table()));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_12 = values_11[0];
V v_13 = V.Nil;
v_13 = new V((RuleFunction)(args_14 => {
V v_15 = args_14[0];
V v_16 = args_14[1];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_15, (V)"x") == R.Get(v_16, (V)"x")), () => (V)(R.Get(v_15, (V)"y") == R.Get(v_16, (V)"y")))));
return Results.Empty;
}));
V v_17 = V.Nil;
v_17 = new V((RuleFunction)(args_18 => {
V v_19 = args_18[0];
V v_20 = args_18[1];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_19, (V)"x") - R.Get(v_20, (V)"x"))))).First <= (V)1.0d), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_19, (V)"y") - R.Get(v_20, (V)"y"))))).First <= (V)1.0d)), () => (V)(!R.Truth(R.Call(v_13, R.Join(new V[] {v_19}, new Results(v_20))).First)))));
return Results.Empty;
}));
V v_21 = V.Nil;
v_21 = new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
V v_24 = args_22[1];
return R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_23, (V)"x") - R.Get(v_24, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_23, (V)"y") - R.Get(v_24, (V)"y"))))).First) == (V)1.0d)));
return Results.Empty;
}));
R.Set(v_10, (V)"new", new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
V v_27 = args_25[1];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {v_26}, new Results(v_27))).First), new Entry((V)"seed", v_26), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table()), new Entry((V)"checkpoints", R.Table()))));
return Results.Empty;
})));
R.Set(v_10, (V)"action", new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
V v_30 = args_28[1];
var values_31 = R.Join(new V[] {}, new Results(R.Get(v_29, (V)"state")));
V v_32 = values_31[0];
var values_33 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_32))));
V v_34 = values_33[0];
var values_35 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(R.Get(v_34, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_34, (V)"position"), (V)"y")), new Entry((V)"hp", R.Get(v_34, (V)"hp")), new Entry((V)"clock", R.Get(v_32, (V)"clock")), new Entry((V)"stage", R.Or(R.And(R.Get(v_32, (V)"stage"), () => R.Get(R.Get(v_32, (V)"stage"), (V)"index")), () => R.And(R.Get(v_32, (V)"finale"), () => R.Get(R.Get(v_32, (V)"finale"), (V)"index")))))));
V v_36 = values_35[0];
var values_37 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_32}, new Results(v_30))));
V v_38 = values_37[0];
V v_39 = values_37[1];
V v_40 = values_37[2];
var values_41 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"intent", v_30), new Entry((V)"ok", v_38), new Entry((V)"reason", v_39), new Entry((V)"from", v_36), new Entry((V)"clock", R.Get(v_32, (V)"clock")), new Entry((V)"hp", R.Get(v_34, (V)"hp")), new Entry((V)"currency", R.Get(v_32, (V)"currency")), new Entry((V)"event_kinds", R.Table()))));
V v_42 = values_41[0];
foreach (var row_43 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_40)))))) {
V v_44 = row_43[0];
V v_45 = row_43[1];
var values_46 = R.Join(new V[] {}, new Results(R.Get(v_45, (V)"kind")));
var obj_47 = R.Get(v_42, (V)"event_kinds");
V key_48 = (V)((V)R.Length(R.Get(v_42, (V)"event_kinds")) + (V)1.0d);
R.Set(obj_47, key_48, values_46[0]);
var values_49 = R.Join(new V[] {}, new Results(v_45));
var obj_50 = R.Get(v_29, (V)"events");
V key_51 = (V)((V)R.Length(R.Get(v_29, (V)"events")) + (V)1.0d);
R.Set(obj_50, key_51, values_49[0]);
}
var values_52 = R.Join(new V[] {}, new Results(v_42));
var obj_53 = R.Get(v_29, (V)"trace");
V key_54 = (V)((V)R.Length(R.Get(v_29, (V)"trace")) + (V)1.0d);
R.Set(obj_53, key_54, values_52[0]);
return R.Join(new V[] {v_38, v_39}, new Results(v_40));
return Results.Empty;
})));
V v_55 = V.Nil;
v_55 = new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
V v_58 = args_56[1];
V v_59 = args_56[2];
V v_60 = args_56[3];
var values_61 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"map_for"), R.Join(new V[] {}, new Results(v_57))));
V v_62 = values_61[0];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_4, (V)"walkable"), R.Join(new V[] {v_62, v_58}, new Results(v_59))).First)))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_63 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"at"), R.Join(new V[] {v_57, R.Get(v_57, (V)"active_floor_id"), v_58}, new Results(v_59))));
V v_64 = values_63[0];
if (R.Truth(R.And(v_64, () => (V)(R.Get(v_64, (V)"kind") == (V)"push_floor")))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_65 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_57, (V)"entities"))))))) {
V v_66 = row_65[0];
V v_67 = row_65[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(v_60, () => (V)(R.Get(v_67, (V)"controller") == (V)"ai")), () => (V)(R.Get(R.Get(v_67, (V)"position"), (V)"floor_id") == R.Get(v_57, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_67, (V)"position"), (V)"x") == v_58)), () => (V)(R.Get(R.Get(v_67, (V)"position"), (V)"y") == v_59)), () => R.Call(R.Get(v_6, (V)"block"), R.Join(new V[] {v_57, v_67, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_57))).First}, new Results((V)true))).First))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.Get(v_67, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_67, (V)"position"), (V)"floor_id") == R.Get(v_57, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_67, (V)"position"), (V)"x") == v_58)), () => (V)(R.Get(R.Get(v_67, (V)"position"), (V)"y") == v_59)), () => (V)(R.Get(v_67, (V)"id") != R.Get(v_57, (V)"player_id"))), () => (V)(R.Get(v_67, (V)"controller") != (V)"ai")), () => (V)(R.Get(v_67, (V)"controller") != (V)"door")))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
R.Set(v_10, (V)"path", new V((RuleFunction)(args_68 => {
V v_69 = args_68[0];
V v_70 = args_68[1];
V v_71 = args_68[2];
var values_72 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_69))).First, (V)"position")));
V v_73 = values_72[0];
if (R.Truth(R.Call(v_70, R.Join(new V[] {}, new Results(v_73))).First)) {
return R.Join(new V[] {}, new Results(R.Table()));
}
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"map_for"), R.Join(new V[] {}, new Results(v_69))));
V v_75 = values_74[0];
V v_76 = V.Nil;
v_76 = new V((RuleFunction)(args_77 => {
V v_78 = args_77[0];
V v_79 = args_77[1];
return R.Join(new V[] {}, new Results((V)((V)((V)(v_79 - (V)1.0d) * R.Get(v_75, (V)"width")) + v_78)));
return Results.Empty;
}));
var values_80 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_73, (V)"x")), new Entry((V)"y", R.Get(v_73, (V)"y")), new Entry((V)"first", V.Nil))))));
V v_81 = values_80[0];
var values_82 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Call(v_76, R.Join(new V[] {R.Get(v_73, (V)"x")}, new Results(R.Get(v_73, (V)"y")))).First, (V)true))));
V v_83 = values_82[0];
var values_84 = R.Join(new V[] {}, new Results((V)1.0d));
V v_85 = values_84[0];
while (R.Truth((V)(v_85 <= (V)R.Length(v_81)))) {
var values_86 = R.Join(new V[] {}, new Results(R.Get(v_81, v_85)));
V v_87 = values_86[0];

var values_88 = R.Join(new V[] {}, new Results((V)(v_85 + (V)1.0d)));
v_85 = values_88[0];
foreach (var row_89 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_90 = row_89[0];
V v_91 = row_89[1];
var values_92 = R.Join(new V[] {(V)(R.Get(v_87, (V)"x") + R.Get(v_91, (V)1.0d))}, new Results((V)(R.Get(v_87, (V)"y") + R.Get(v_91, (V)2.0d))));
V v_93 = values_92[0];
V v_94 = values_92[1];
var values_95 = R.Join(new V[] {}, R.Call(v_76, R.Join(new V[] {v_93}, new Results(v_94))));
V v_96 = values_95[0];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_83, v_96))), () => R.Call(v_55, R.Join(new V[] {v_69, v_93, v_94}, new Results(v_71))).First))) {
var values_97 = R.Join(new V[] {}, new Results((V)true));
var obj_98 = v_83;
V key_99 = v_96;
R.Set(obj_98, key_99, values_97[0]);
var values_100 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_87, (V)"first"), () => v_91)));
V v_101 = values_100[0];
if (R.Truth(R.Call(v_70, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_93), new Entry((V)"y", v_94))))).First)) {
return R.Join(new V[] {}, new Results(v_101));
}
var values_102 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_93), new Entry((V)"y", v_94), new Entry((V)"first", v_101))));
var obj_103 = v_81;
V key_104 = (V)((V)R.Length(v_81) + (V)1.0d);
R.Set(obj_103, key_104, values_102[0]);
}
}
}
return Results.Empty;
})));
V v_105 = V.Nil;
v_105 = new V((RuleFunction)(args_106 => {
V v_107 = args_106[0];
V v_108 = args_106[1];
V v_109 = args_106[2];
V v_110 = args_106[3];
foreach (var row_111 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"pending_strikes"), R.Join(new V[] {}, new Results(v_107)))))))) {
V v_112 = row_111[0];
V v_113 = row_111[1];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_113, (V)"support"))), () => R.Or(R.Or((V)(!R.Truth(v_110)), () => R.Get(v_113, (V)"shove")), () => (V)(R.Or(R.Get(v_113, (V)"damage"), () => R.Get(rt.Global("math"), (V)"huge")) > R.Or(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_107))).First, (V)"defense"), () => (V)0.0d)))))) {
foreach (var row_114 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_113, (V)"target_cells"))))))) {
V v_115 = row_114[0];
V v_116 = row_114[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_116, (V)"floor_id") == R.Get(v_107, (V)"active_floor_id")), () => (V)(R.Get(v_116, (V)"x") == v_108)), () => (V)(R.Get(v_116, (V)"y") == v_109)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
R.Set(v_10, (V)"escape", new V((RuleFunction)(args_117 => {
V v_118 = args_117[0];
V v_119 = args_117[1];
V v_120 = args_117[2];
V v_121 = args_117[3];
V v_122 = args_117[4];
var values_123 = R.Join(new V[] {}, new Results(R.Get(v_118, (V)"state")));
V v_124 = values_123[0];
var values_125 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_124))).First, (V)"position")));
V v_126 = values_125[0];
if (R.Truth((V)(!R.Truth(R.Call(v_105, R.Join(new V[] {v_124, R.Get(v_126, (V)"x"), R.Get(v_126, (V)"y")}, new Results(v_120))).First)))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_127 = Results.Empty;
V v_128 = values_127[0];
V v_129 = values_127[1];
V v_130 = values_127[2];
foreach (var row_131 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_132 = row_131[0];
V v_133 = row_131[1];
var values_134 = R.Join(new V[] {(V)(R.Get(v_126, (V)"x") + R.Get(v_133, (V)1.0d))}, new Results((V)(R.Get(v_126, (V)"y") + R.Get(v_133, (V)2.0d))));
V v_135 = values_134[0];
V v_136 = values_134[1];
var values_137 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_124, (V)"move", R.Get(v_133, (V)1.0d)}, new Results(R.Get(v_133, (V)2.0d)))));
V v_138 = values_137[0];
var values_139 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"at"), R.Join(new V[] {v_124, R.Get(v_124, (V)"active_floor_id"), v_135}, new Results(v_136))));
V v_140 = values_139[0];
if (R.Truth(R.And(R.And(R.And(R.Get(v_138, (V)"valid"), () => (V)(R.Get(v_138, (V)"distance") == (V)1.0d)), () => (V)(!R.Truth(R.And(v_140, () => (V)(R.Get(v_140, (V)"kind") == (V)"push_floor"))))), () => (V)(!R.Truth(R.Call(v_105, R.Join(new V[] {v_124, v_135}, new Results(v_136))).First))))) {
var values_141 = R.Join(new V[] {}, new Results(R.Or(R.And(v_119, () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_135 - R.Get(v_119, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_136 - R.Get(v_119, (V)"y"))))).First)), () => (V)0.0d)));
V v_142 = values_141[0];
var values_143 = R.Join(new V[] {}, new Results(R.Or(R.And(v_119, () => R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)((V)(R.Get(v_133, (V)1.0d) * (V)(R.Get(v_119, (V)"y") - R.Get(v_126, (V)"y"))) - (V)(R.Get(v_133, (V)2.0d) * (V)(R.Get(v_119, (V)"x") - R.Get(v_126, (V)"x"))))))).First), () => (V)0.0d)));
V v_144 = values_143[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_128)), () => (V)(v_142 < v_129)), () => R.And(R.And((V)(v_142 == v_129), () => v_122), () => (V)(v_144 > v_130))))) {
var values_145 = R.Join(new V[] {v_133, v_142}, new Results(v_144));
v_128 = values_145[0];
v_129 = values_145[1];
v_130 = values_145[2];
}
}
}
if (R.Truth(v_128)) {
var values_146 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_118}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_128, (V)1.0d)), new Entry((V)"dy", R.Get(v_128, (V)2.0d)))))));
V v_147 = values_146[0];
return R.Join(new V[] {}, new Results(v_147));
}
if (R.Truth((V)(!R.Truth(v_121)))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_148 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_149 = row_148[0];
V v_150 = row_148[1];
var values_151 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_124, (V)"dash", R.Get(v_150, (V)1.0d)}, new Results(R.Get(v_150, (V)2.0d)))));
V v_152 = values_151[0];
var values_153 = R.Join(new V[] {}, new Results(R.And(R.Get(v_152, (V)"valid"), () => R.Get(R.Get(v_152, (V)"cells"), R.Get(v_152, (V)"distance")))));
V v_154 = values_153[0];
var values_155 = R.Join(new V[] {}, new Results(R.And(v_154, () => R.Call(R.Get(v_8, (V)"at"), R.Join(new V[] {v_124, R.Get(v_124, (V)"active_floor_id"), R.Get(v_154, (V)"x")}, new Results(R.Get(v_154, (V)"y")))).First)));
V v_156 = values_155[0];
if (R.Truth(R.And(R.And(R.And(v_154, () => (V)(R.Get(v_152, (V)"distance") > (V)1.0d)), () => (V)(!R.Truth(R.And(v_156, () => (V)(R.Get(v_156, (V)"kind") == (V)"push_floor"))))), () => (V)(!R.Truth(R.Call(v_105, R.Join(new V[] {v_124, R.Get(v_154, (V)"x")}, new Results(R.Get(v_154, (V)"y")))).First))))) {
var values_157 = R.Join(new V[] {}, new Results(R.Or(R.And(v_119, () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_154, (V)"x") - R.Get(v_119, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_154, (V)"y") - R.Get(v_119, (V)"y"))))).First)), () => (V)0.0d)));
V v_158 = values_157[0];
if (R.Truth(R.Or((V)(!R.Truth(v_128)), () => (V)(v_158 < v_129)))) {
var values_159 = R.Join(new V[] {v_150}, new Results(v_158));
v_128 = values_159[0];
v_129 = values_159[1];
}
}
}
if (R.Truth(v_128)) {
return R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_118}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", R.Get(v_128, (V)1.0d)), new Entry((V)"dy", R.Get(v_128, (V)2.0d)))))));
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_10, (V)"movement_cycle", new V((RuleFunction)(args_160 => {
V v_161 = args_160[0];
V v_162 = args_160[1];
var values_163 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_161, (V)"trace"))));
V v_164 = values_163[0];
if (R.Truth((V)((V)(v_164 - R.Or(v_162, () => (V)0.0d)) < (V)6.0d))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_165 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(R.Get(v_161, (V)"state")))).First, (V)"position")));
V v_166 = values_165[0];
var values_167 = R.Join(new V[] {R.Get(R.Get(v_161, (V)"trace"), v_164)}, new Results(R.Get(R.Get(v_161, (V)"trace"), (V)(v_164 - (V)1.0d))));
V v_168 = values_167[0];
V v_169 = values_167[1];
if (R.Truth(R.Or((V)(R.Get(v_166, (V)"x") != R.Get(R.Get(v_169, (V)"from"), (V)"x")), () => (V)(R.Get(v_166, (V)"y") != R.Get(R.Get(v_169, (V)"from"), (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
{ double end_172 = R.Num((V)5.0d), step_173 = R.Num((V)1.0d);
for (double i_171 = R.Num((V)0.0d); step_173 > 0 ? i_171 <= end_172 : i_171 >= end_172; i_171 += step_173) {
V v_170 = i_171;
var values_174 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_161, (V)"trace"), (V)(v_164 - v_170))));
V v_175 = values_174[0];
var values_176 = R.Join(new V[] {}, new Results(R.Or(R.And((V)((V)(v_170 % (V)2.0d) == (V)0.0d), () => R.Get(v_168, (V)"from")), () => R.Get(v_169, (V)"from"))));
V v_177 = values_176[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(!R.Truth(R.Get(v_175, (V)"ok"))), () => (V)(R.Get(R.Get(v_175, (V)"intent"), (V)"kind") != (V)"move")), () => (V)(R.Get(R.Get(v_175, (V)"from"), (V)"x") != R.Get(v_177, (V)"x"))), () => (V)(R.Get(R.Get(v_175, (V)"from"), (V)"y") != R.Get(v_177, (V)"y"))), () => (V)(R.Get(R.Get(v_175, (V)"from"), (V)"hp") != R.Get(v_175, (V)"hp"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_10, (V)"attack_intent", new V((RuleFunction)(args_178 => {
V v_179 = args_178[0];
V v_180 = args_178[1];
V v_181 = args_178[2];
var values_182 = R.Join(new V[] {}, new Results(R.Get(v_179, (V)"state")));
V v_183 = values_182[0];
if (R.Truth((V)(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_183))).First, (V)"definition_id") == (V)"core:actor/alchemist"))) {
var values_184 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_183, (V)"sweeping_slash", v_180}, new Results(v_181))));
V v_185 = values_184[0];
var values_186 = R.Join(new V[] {}, new Results((V)0.0d));
V v_187 = values_186[0];
if (R.Truth(R.Get(v_185, (V)"valid"))) {
foreach (var row_188 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_183, (V)"entities"))))))) {
V v_189 = row_188[0];
V v_190 = row_188[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_190, (V)"controller") == (V)"ai"), () => (V)(!R.Truth(R.Get(v_190, (V)"invulnerable")))), () => (V)(R.Get(v_190, (V)"hp") > (V)0.0d)), () => (V)(R.Get(R.Get(v_190, (V)"position"), (V)"floor_id") == R.Get(v_183, (V)"active_floor_id"))))) {
foreach (var row_191 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_185, (V)"cells"))))))) {
V v_192 = row_191[0];
V v_193 = row_191[1];
if (R.Truth(R.And((V)(R.Get(v_193, (V)"x") == R.Get(R.Get(v_190, (V)"position"), (V)"x")), () => (V)(R.Get(v_193, (V)"y") == R.Get(R.Get(v_190, (V)"position"), (V)"y"))))) {
var values_194 = R.Join(new V[] {}, new Results((V)(v_187 + (V)1.0d)));
v_187 = values_194[0];

break;
}
}
}
}
}
if (R.Truth((V)(v_187 >= (V)2.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", v_180), new Entry((V)"dy", v_181))));
}
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_180), new Entry((V)"dy", v_181))));
return Results.Empty;
})));
R.Set(v_10, (V)"attack_adjacent", new V((RuleFunction)(args_195 => {
V v_196 = args_195[0];
V v_197 = args_195[1];
var values_198 = R.Join(new V[] {}, new Results(R.Get(v_196, (V)"state")));
V v_199 = values_198[0];
var values_200 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_199))).First, (V)"position")));
V v_201 = values_200[0];
var values_202 = Results.Empty;
V v_203 = values_202[0];
V v_204 = V.Nil;
v_204 = new V((RuleFunction)(args_205 => {
V v_206 = args_205[0];
var values_207 = R.Join(new V[] {(V)(R.Get(R.Get(v_206, (V)"position"), (V)"x") - R.Get(v_201, (V)"x"))}, new Results((V)(R.Get(R.Get(v_206, (V)"position"), (V)"y") - R.Get(v_201, (V)"y"))));
V v_208 = values_207[0];
V v_209 = values_207[1];
var values_210 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"attack_intent"), R.Join(new V[] {v_196, v_208}, new Results(v_209))));
V v_211 = values_210[0];
var values_212 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_199, R.Get(v_211, (V)"kind"), v_208}, new Results(v_209))));
V v_213 = values_212[0];
foreach (var row_214 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_213, (V)"valid"), () => R.Get(v_213, (V)"cells")), () => R.Table()))))))) {
V v_215 = row_214[0];
V v_216 = row_214[1];
if (R.Truth(R.And((V)(R.Get(v_216, (V)"x") == R.Get(R.Get(v_206, (V)"position"), (V)"x")), () => (V)(R.Get(v_216, (V)"y") == R.Get(R.Get(v_206, (V)"position"), (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
foreach (var row_217 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_199, (V)"entities"))))))) {
V v_218 = row_217[0];
V v_219 = row_217[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_219, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_219, (V)"id") != v_197)), () => (V)(!R.Truth(R.Get(v_219, (V)"invulnerable")))), () => (V)(R.Get(R.Get(v_219, (V)"position"), (V)"floor_id") == R.Get(v_199, (V)"active_floor_id"))), () => R.Call(v_17, R.Join(new V[] {v_201}, new Results(R.Get(v_219, (V)"position")))).First), () => R.Call(v_204, R.Join(new V[] {}, new Results(v_219))).First), () => R.Or(R.Or((V)(!R.Truth(v_203)), () => (V)(R.Get(v_219, (V)"hp") < R.Get(v_203, (V)"hp"))), () => R.And((V)(R.Get(v_219, (V)"hp") == R.Get(v_203, (V)"hp")), () => (V)(R.Get(v_219, (V)"id") < R.Get(v_203, (V)"id"))))))) {
var values_220 = R.Join(new V[] {}, new Results(v_219));
v_203 = values_220[0];
}
}
if (R.Truth((V)(!R.Truth(v_203)))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth(R.Call(R.Get(v_6, (V)"block"), R.Join(new V[] {v_199, v_203, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_199))).First}, new Results((V)true))).First)) {
var values_221 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"ward_source"), R.Join(new V[] {v_199}, new Results(v_203))));
V v_222 = values_221[0];
var values_225 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"path"), R.Join(new V[] {v_199, new V((RuleFunction)(args_223 => {
V v_224 = args_223[0];
if (R.Truth(v_222)) {
return R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {v_224}, new Results(R.Get(v_222, (V)"position")))));
}
return R.Join(new V[] {}, new Results(R.And(R.Call(v_17, R.Join(new V[] {v_224}, new Results(R.Get(v_203, (V)"position")))).First, () => (V)(!R.Truth(R.Call(R.Get(v_6, (V)"block"), R.Join(new V[] {v_199, v_203, R.Table(new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_199, (V)"active_floor_id")), new Entry((V)"x", R.Get(v_224, (V)"x")), new Entry((V)"y", R.Get(v_224, (V)"y")))))}, new Results((V)true))).First)))));
return Results.Empty;
}))}, new Results((V)true))));
V v_226 = values_225[0];
if (R.Truth(R.And(v_226, () => R.Get(v_226, (V)1.0d)))) {
return R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_196}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_226, (V)1.0d)), new Entry((V)"dy", R.Get(v_226, (V)2.0d)))))));
}
if (R.Truth(v_222)) {
var values_227 = Results.Empty;
V v_228 = values_227[0];
V v_229 = values_227[1];
var values_230 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_201, (V)"x") - R.Get(R.Get(v_222, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_201, (V)"y") - R.Get(R.Get(v_222, (V)"position"), (V)"y"))))).First)));
V v_231 = values_230[0];
foreach (var row_232 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_233 = row_232[0];
V v_234 = row_232[1];
var values_235 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_199, (V)"move", R.Get(v_234, (V)1.0d)}, new Results(R.Get(v_234, (V)2.0d)))));
V v_236 = values_235[0];
var values_237 = R.Join(new V[] {(V)(R.Get(v_201, (V)"x") + R.Get(v_234, (V)1.0d))}, new Results((V)(R.Get(v_201, (V)"y") + R.Get(v_234, (V)2.0d))));
V v_238 = values_237[0];
V v_239 = values_237[1];
var values_240 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_238 - R.Get(R.Get(v_222, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_239 - R.Get(R.Get(v_222, (V)"position"), (V)"y"))))).First)));
V v_241 = values_240[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.Get(v_236, (V)"valid"), () => (V)(R.Get(v_236, (V)"distance") == (V)1.0d)), () => (V)(v_241 > v_231)), () => (V)(!R.Truth(R.Call(v_105, R.Join(new V[] {v_199, v_238}, new Results(v_239))).First))), () => R.Or((V)(!R.Truth(v_229)), () => (V)(v_241 > v_229))))) {
var values_242 = R.Join(new V[] {v_234}, new Results(v_241));
v_228 = values_242[0];
v_229 = values_242[1];
}
}
if (R.Truth(v_228)) {
return R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_196}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_228, (V)1.0d)), new Entry((V)"dy", R.Get(v_228, (V)2.0d)))))));
}
}
if (R.Truth((V)(!R.Truth(v_222)))) {
return R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_196}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
}
return R.Join(new V[] {}, new Results((V)false));
}
var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_196}, R.Call(R.Get(v_10, (V)"attack_intent"), R.Join(new V[] {v_196, (V)(R.Get(R.Get(v_203, (V)"position"), (V)"x") - R.Get(v_201, (V)"x"))}, new Results((V)(R.Get(R.Get(v_203, (V)"position"), (V)"y") - R.Get(v_201, (V)"y"))))))));
V v_244 = values_243[0];
return R.Join(new V[] {}, new Results(v_244));
return Results.Empty;
})));
R.Set(v_10, (V)"attack_in_reach", new V((RuleFunction)(args_245 => {
V v_246 = args_245[0];
var values_247 = R.Join(new V[] {}, new Results(R.Get(v_246, (V)"state")));
V v_248 = values_247[0];

var values_249 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_248))));
V v_250 = values_249[0];
var values_251 = Results.Empty;
V v_252 = values_251[0];
foreach (var row_253 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"basic_attack"), new Entry((V)2, (V)"sweeping_slash")))))))) {
V v_254 = row_253[0];
V v_255 = row_253[1];
foreach (var row_256 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)6, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_257 = row_256[0];
V v_258 = row_256[1];
var values_259 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_248, v_255, R.Get(v_258, (V)1.0d)}, new Results(R.Get(v_258, (V)2.0d)))));
V v_260 = values_259[0];
if (R.Truth(R.Get(v_260, (V)"valid"))) {
var values_261 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"effective_ability"), R.Join(new V[] {v_248, v_255, R.Get(v_258, (V)1.0d), R.Get(v_258, (V)2.0d)}, new Results((V)true))));
V v_262 = values_261[0];
V v_263 = values_261[1];
V v_264 = values_261[2];
V v_265 = values_261[3];
foreach (var row_266 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_262)))))) {
V v_267 = row_266[0];
V v_268 = row_266[1];
var values_269 = R.Join(new V[] {}, new Results((V)false));
V v_270 = values_269[0];
foreach (var row_271 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_248, (V)"entities"))))))) {
V v_272 = row_271[0];
V v_273 = row_271[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.Get(v_273, (V)"hp"), () => (V)(R.Get(v_273, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_273, (V)"id") != R.Get(v_250, (V)"id"))), () => (V)(R.Get(R.Get(v_273, (V)"position"), (V)"floor_id") == R.Get(v_248, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_273, (V)"position"), (V)"x") == R.Get(v_268, (V)"x"))), () => (V)(R.Get(R.Get(v_273, (V)"position"), (V)"y") == R.Get(v_268, (V)"y"))))) {
var values_274 = R.Join(new V[] {}, new Results((V)true));
v_270 = values_274[0];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_273, (V)"controller") == (V)"ai"), () => (V)(!R.Truth(R.Get(v_273, (V)"invulnerable")))), () => (V)(v_263 > R.Or(R.Get(v_273, (V)"defense"), () => (V)0.0d))), () => (V)(!R.Truth(R.Call(R.Get(v_6, (V)"block"), R.Join(new V[] {v_248, v_273, v_250}, new Results((V)true))).First))), () => R.Or(R.Or((V)(!R.Truth(v_252)), () => (V)(R.Get(v_273, (V)"hp") < R.Get(v_252, (V)"hp"))), () => R.And((V)(R.Get(v_273, (V)"hp") == R.Get(v_252, (V)"hp")), () => (V)(R.Get(v_273, (V)"id") < R.Get(v_252, (V)"id"))))))) {
var values_275 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"hp", R.Get(v_273, (V)"hp")), new Entry((V)"id", R.Get(v_273, (V)"id")), new Entry((V)"dx", R.Get(v_258, (V)1.0d)), new Entry((V)"dy", R.Get(v_258, (V)2.0d)), new Entry((V)"kind", v_255))));
v_252 = values_275[0];
}
}
}
if (R.Truth(R.And(v_270, () => (V)(v_265 == (V)true)))) {
break;
}
}
}
}
}
if (R.Truth(v_252)) {
return R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_246}, new Results(R.Table(new Entry((V)"kind", R.Get(v_252, (V)"kind")), new Entry((V)"dx", R.Get(v_252, (V)"dx")), new Entry((V)"dy", R.Get(v_252, (V)"dy")))))));
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_10, (V)"navigate", new V((RuleFunction)(args_276 => {
V v_277 = args_276[0];
V v_278 = args_276[1];
V v_279 = args_276[2];
var values_280 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_277, (V)"trace"))));
V v_281 = values_280[0];
{ double end_284 = R.Num(v_279), step_285 = R.Num((V)1.0d);
for (double i_283 = R.Num((V)1.0d); step_285 > 0 ? i_283 <= end_284 : i_283 >= end_284; i_283 += step_285) {
V v_282 = i_283;
var values_286 = R.Join(new V[] {}, new Results(R.Get(v_277, (V)"state")));
V v_287 = values_286[0];
var values_288 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_287))).First, (V)"position")));
V v_289 = values_288[0];
if (R.Truth(R.Call(v_278, R.Join(new V[] {}, new Results(v_289))).First)) {
return R.Join(new V[] {}, new Results((V)true));
}
if (R.Truth(R.Get(v_287, (V)"game_over"))) {
return R.Join(new V[] {(V)false}, new Results((V)"dead"));
}
var values_290 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_277, (V)"trace"))));
V v_291 = values_290[0];
var values_292 = R.Join(new V[] {}, new Results((V)((V)(v_291 - v_281) >= (V)6.0d)));
V v_293 = values_292[0];
var values_294 = R.Join(new V[] {R.Get(R.Get(v_277, (V)"trace"), v_291)}, new Results(R.Get(R.Get(v_277, (V)"trace"), (V)(v_291 - (V)1.0d))));
V v_295 = values_294[0];
V v_296 = values_294[1];
if (R.Truth(v_293)) {
var values_297 = R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_289, (V)"x") == R.Get(R.Get(v_295, (V)"from"), (V)"x")), () => (V)(R.Get(v_289, (V)"y") == R.Get(R.Get(v_295, (V)"from"), (V)"y"))), () => (V)(R.Get(R.Get(v_295, (V)"from"), (V)"x") == R.Get(R.Get(v_296, (V)"from"), (V)"x"))), () => (V)(R.Get(R.Get(v_295, (V)"from"), (V)"y") == R.Get(R.Get(v_296, (V)"from"), (V)"y")))));
V v_298 = values_297[0];
var values_299 = R.Join(new V[] {}, new Results(R.Or(v_298, () => R.And((V)(R.Get(v_289, (V)"x") == R.Get(R.Get(v_296, (V)"from"), (V)"x")), () => (V)(R.Get(v_289, (V)"y") == R.Get(R.Get(v_296, (V)"from"), (V)"y"))))));
v_293 = values_299[0];
{ double end_302 = R.Num((V)5.0d), step_303 = R.Num((V)1.0d);
for (double i_301 = R.Num((V)0.0d); step_303 > 0 ? i_301 <= end_302 : i_301 >= end_302; i_301 += step_303) {
V v_300 = i_301;
var values_304 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_277, (V)"trace"), (V)(v_291 - v_300))));
V v_305 = values_304[0];
var values_306 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And(v_298, () => R.Get(v_295, (V)"from")), () => R.And((V)((V)(v_300 % (V)2.0d) == (V)0.0d), () => R.Get(v_295, (V)"from"))), () => R.Get(v_296, (V)"from"))));
V v_307 = values_306[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(!R.Truth(R.Get(v_305, (V)"ok"))), () => (V)(R.Get(R.Get(v_305, (V)"from"), (V)"x") != R.Get(v_307, (V)"x"))), () => (V)(R.Get(R.Get(v_305, (V)"from"), (V)"y") != R.Get(v_307, (V)"y"))), () => (V)(R.Get(R.Get(v_305, (V)"from"), (V)"hp") != R.Get(v_305, (V)"hp"))), () => R.And((V)(!R.Truth(v_298)), () => (V)(R.Get(R.Get(v_305, (V)"intent"), (V)"kind") != (V)"move"))))) {
var values_308 = R.Join(new V[] {}, new Results((V)false));
v_293 = values_308[0];

break;
}
}
}
}
var values_309 = R.Join(new V[] {}, new Results((V)false));
V v_310 = values_309[0];
if (R.Truth(v_293)) {
foreach (var row_311 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_287, (V)"entities"))))))) {
V v_312 = row_311[0];
V v_313 = row_311[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_313, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_313, (V)"hp") > (V)0.0d)), () => (V)(R.Get(R.Get(v_313, (V)"position"), (V)"floor_id") == R.Get(v_287, (V)"active_floor_id"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_313, (V)"position"), (V)"x") - R.Get(v_289, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_313, (V)"position"), (V)"y") - R.Get(v_289, (V)"y"))))).First) <= (V)3.0d)))) {
var values_314 = R.Join(new V[] {}, new Results((V)true));
v_310 = values_314[0];

break;
}
}
}
var values_315 = Results.Empty;
V v_316 = values_315[0];
if (R.Truth(R.And(v_293, () => R.Call(v_105, R.Join(new V[] {v_287, R.Get(v_289, (V)"x")}, new Results(R.Get(v_289, (V)"y")))).First))) {
var values_317 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"path"), R.Join(new V[] {v_287}, new Results(v_278))));
v_316 = values_317[0];
}
if (R.Truth(R.And(v_293, () => R.Or(R.Call(R.Get(v_10, (V)"attack_in_reach"), R.Join(new V[] {}, new Results(v_277))).First, () => R.Call(R.Get(v_10, (V)"attack_adjacent"), R.Join(new V[] {v_277}, new Results(R.And(R.Get(v_287, (V)"stage"), () => R.Get(R.Get(v_287, (V)"stage"), (V)"elite_id"))))).First)))) {

} else if (R.Truth(R.And(v_316, () => R.Get(v_316, (V)1.0d)))) {
var values_318 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_287, (V)"dash", R.Get(v_316, (V)1.0d)}, new Results(R.Get(v_316, (V)2.0d)))));
V v_319 = values_318[0];
var values_320 = R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.And((V)(!R.Truth(v_310)), () => R.Get(v_319, (V)"valid")), () => (V)(R.Get(v_319, (V)"distance") > (V)1.0d)), () => (V)"dash"), () => (V)"move")));
V v_321 = values_320[0];
var values_322 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_277}, new Results(R.Table(new Entry((V)"kind", v_321), new Entry((V)"dx", R.Get(v_316, (V)1.0d)), new Entry((V)"dy", R.Get(v_316, (V)2.0d)))))));
V v_323 = values_322[0];
V v_324 = values_322[1];
if (R.Truth((V)(!R.Truth(v_323)))) {
return R.Join(new V[] {(V)false}, new Results(v_324));
}
} else if (R.Truth(R.Call(R.Get(v_10, (V)"escape"), R.Join(new V[] {}, new Results(v_277))).First)) {

} else if (R.Truth(R.Call(R.Get(v_10, (V)"attack_adjacent"), R.Join(new V[] {v_277}, new Results(R.And(R.Get(v_287, (V)"stage"), () => R.Get(R.Get(v_287, (V)"stage"), (V)"elite_id"))))).First)) {

} else {
var values_325 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"path"), R.Join(new V[] {v_287}, new Results(v_278))));
V v_326 = values_325[0];
if (R.Truth((V)(!R.Truth(v_326)))) {
return R.Join(new V[] {(V)false}, new Results((V)"no path"));
}
var values_327 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_277}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_326, (V)1.0d)), new Entry((V)"dy", R.Get(v_326, (V)2.0d)))))));
V v_328 = values_327[0];
V v_329 = values_327[1];
if (R.Truth((V)(!R.Truth(v_328)))) {
return R.Join(new V[] {(V)false}, new Results(v_329));
}
}
}
}
return R.Join(new V[] {(V)false}, new Results((V)"navigation action limit"));
return Results.Empty;
})));
R.Set(v_10, (V)"fight", new V((RuleFunction)(args_330 => {
V v_331 = args_330[0];
V v_332 = args_330[1];
V v_333 = args_330[2];
V v_334 = args_330[3];
var values_335 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_331, (V)"trace"))));
V v_336 = values_335[0];
{ double end_339 = R.Num(v_333), step_340 = R.Num((V)1.0d);
for (double i_338 = R.Num((V)1.0d); step_340 > 0 ? i_338 <= end_339 : i_338 >= end_339; i_338 += step_340) {
V v_337 = i_338;
var values_341 = R.Join(new V[] {}, new Results(R.Get(v_331, (V)"state")));
V v_342 = values_341[0];
if (R.Truth(R.And(v_334, () => R.Call(v_334, Results.Empty).First))) {
return R.Join(new V[] {}, new Results((V)true));
}
var values_343 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_342}, new Results(v_332))));
V v_344 = values_343[0];
if (R.Truth((V)(!R.Truth(v_344)))) {
return R.Join(new V[] {}, new Results((V)true));
}
var values_345 = R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_6, (V)"ward_source"), R.Join(new V[] {v_342}, new Results(v_344))).First, () => v_344)));
v_344 = values_345[0];
if (R.Truth(R.Get(v_342, (V)"game_over"))) {
return R.Join(new V[] {(V)false}, new Results((V)"dead"));
}
var values_346 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_342))).First, (V)"position")));
V v_347 = values_346[0];
if (R.Truth(R.Call(R.Get(v_10, (V)"escape"), R.Join(new V[] {v_331, R.Get(v_344, (V)"position"), V.Nil}, new Results(R.And((V)(R.Get(v_342, (V)"floor_order") != V.Nil), () => (V)(R.Get(v_344, (V)"definition_id") == (V)"core:enemy/ember_crucible"))))).First)) {

} else if (R.Truth(R.And(R.Get(v_342, (V)"floor_order"), () => R.Call(R.Get(v_10, (V)"movement_cycle"), R.Join(new V[] {v_331}, new Results(v_336))).First))) {
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_10, (V)"attack_in_reach"), R.Join(new V[] {}, new Results(v_331))).First)))) {
var values_348 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_331}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_349 = values_348[0];
V v_350 = values_348[1];
if (R.Truth((V)(!R.Truth(v_349)))) {
return R.Join(new V[] {(V)false}, new Results(v_350));
}
}
} else if (R.Truth(R.And(R.Call(v_17, R.Join(new V[] {v_347}, new Results(R.Get(v_344, (V)"position")))).First, () => (V)(!R.Truth(R.Call(R.Get(v_6, (V)"block"), R.Join(new V[] {v_342, v_344, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_342))).First}, new Results((V)true))).First))))) {
var values_351 = R.Join(new V[] {(V)(R.Get(R.Get(v_344, (V)"position"), (V)"x") - R.Get(v_347, (V)"x"))}, new Results((V)(R.Get(R.Get(v_344, (V)"position"), (V)"y") - R.Get(v_347, (V)"y"))));
V v_352 = values_351[0];
V v_353 = values_351[1];
var values_354 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_331}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_352), new Entry((V)"dy", v_353))))));
V v_355 = values_354[0];
V v_356 = values_354[1];
if (R.Truth((V)(!R.Truth(v_355)))) {
return R.Join(new V[] {(V)false}, new Results(v_356));
}
} else {
var values_359 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"path"), R.Join(new V[] {v_342, new V((RuleFunction)(args_357 => {
V v_358 = args_357[0];
return R.Join(new V[] {}, new Results(R.And(R.Call(v_17, R.Join(new V[] {v_358}, new Results(R.Get(v_344, (V)"position")))).First, () => (V)(!R.Truth(R.Call(R.Get(v_6, (V)"block"), R.Join(new V[] {v_342, v_344, R.Table(new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_342, (V)"active_floor_id")), new Entry((V)"x", R.Get(v_358, (V)"x")), new Entry((V)"y", R.Get(v_358, (V)"y")))))}, new Results((V)true))).First)))));
return Results.Empty;
}))}, new Results((V)true))));
V v_360 = values_359[0];
var values_363 = R.Join(new V[] {}, new Results(R.Or(v_360, () => R.Call(R.Get(v_10, (V)"path"), R.Join(new V[] {v_342}, new Results(new V((RuleFunction)(args_361 => {
V v_362 = args_361[0];
return R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {v_362}, new Results(R.Get(v_344, (V)"position")))));
return Results.Empty;
}))))).First)));
v_360 = values_363[0];
if (R.Truth((V)(!R.Truth(v_360)))) {
return R.Join(new V[] {(V)false}, new Results((V)"no approach"));
}
var values_364 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_360, (V)1.0d), () => R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_360, (V)1.0d)), new Entry((V)"dy", R.Get(v_360, (V)2.0d)))), () => R.Table(new Entry((V)"kind", (V)"wait")))));
V v_365 = values_364[0];
var values_366 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_331}, new Results(v_365))));
V v_367 = values_366[0];
V v_368 = values_366[1];
if (R.Truth((V)(!R.Truth(v_367)))) {
return R.Join(new V[] {(V)false}, new Results(v_368));
}
}
}
}
return R.Join(new V[] {(V)false}, new Results((V)"combat action limit"));
return Results.Empty;
})));
R.Set(v_10, (V)"nearest", new V((RuleFunction)(args_369 => {
V v_370 = args_369[0];
V v_371 = args_369[1];
var values_372 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_370))).First, (V)"position")));
V v_373 = values_372[0];
var values_374 = Results.Empty;
V v_375 = values_374[0];
V v_376 = values_374[1];
foreach (var row_377 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_370, (V)"entities"))))))) {
V v_378 = row_377[0];
V v_379 = row_377[1];
if (R.Truth(R.And(R.And(R.Get(v_379, (V)"position"), () => (V)(R.Get(R.Get(v_379, (V)"position"), (V)"floor_id") == R.Get(v_370, (V)"active_floor_id"))), () => R.Call(v_371, R.Join(new V[] {}, new Results(v_379))).First))) {
var values_380 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_373, (V)"x") - R.Get(R.Get(v_379, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_373, (V)"y") - R.Get(R.Get(v_379, (V)"position"), (V)"y"))))).First)));
V v_381 = values_380[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_375)), () => (V)(v_381 < v_376)), () => R.And((V)(v_381 == v_376), () => (V)(R.Get(v_379, (V)"id") < R.Get(v_375, (V)"id")))))) {
var values_382 = R.Join(new V[] {v_379}, new Results(v_381));
v_375 = values_382[0];
v_376 = values_382[1];
}
}
}
return R.Join(new V[] {}, new Results(v_375));
return Results.Empty;
})));
R.Set(v_10, (V)"has_event", new V((RuleFunction)(args_383 => {
V v_384 = args_383[0];
V v_385 = args_383[1];
foreach (var row_386 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_384, (V)"events"))))))) {
V v_387 = row_386[0];
V v_388 = row_386[1];
if (R.Truth((V)(R.Get(v_388, (V)"kind") == v_385))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
V v_389 = V.Nil;
v_389 = new V((RuleFunction)(args_390 => {
V v_391 = args_390[0];
V v_392 = args_390[1];
var values_393 = R.Join(new V[] {}, new Results(R.Get(v_391, (V)"state")));
V v_394 = values_393[0];
var values_395 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"name", v_392), new Entry((V)"actions", (V)R.Length(R.Get(v_391, (V)"trace"))), new Entry((V)"clock", R.Get(v_394, (V)"clock")), new Entry((V)"hp", R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_394))).First, (V)"hp")), new Entry((V)"currency", R.Get(v_394, (V)"currency")), new Entry((V)"stage", R.Or(R.And(R.Get(v_394, (V)"stage"), () => R.Get(R.Get(v_394, (V)"stage"), (V)"index")), () => R.And(R.Get(v_394, (V)"finale"), () => R.Get(R.Get(v_394, (V)"finale"), (V)"index")))), new Entry((V)"floor_id", R.Get(v_394, (V)"active_floor_id")))));
var obj_396 = R.Get(v_391, (V)"checkpoints");
V key_397 = (V)((V)R.Length(R.Get(v_391, (V)"checkpoints")) + (V)1.0d);
R.Set(obj_396, key_397, values_395[0]);
return Results.Empty;
}));
R.Set(v_10, (V)"stage_one", new V((RuleFunction)(args_398 => {
V v_399 = args_398[0];
V v_400 = args_398[1];
V v_401 = args_398[2];
var values_402 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"new"), R.Join(new V[] {v_399}, new Results(v_401))));
V v_403 = values_402[0];
var values_404 = R.Join(new V[] {}, new Results(R.Get(v_403, (V)"state")));
V v_405 = values_404[0];
R.Call(v_389, R.Join(new V[] {v_403}, new Results((V)"new_run")));
var values_406 = R.Join(new V[] {}, new Results((V)0.0d));
V v_407 = values_406[0];
while (true) {
var values_410 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_10, (V)"nearest"), R.Join(new V[] {v_405}, new Results(new V((RuleFunction)(args_408 => {
V v_409 = args_408[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_409, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_409, (V)"id") != R.Get(R.Get(v_405, (V)"stage"), (V)"elite_id"))), () => (V)(!R.Truth(R.Get(v_409, (V)"invulnerable"))))));
return Results.Empty;
}))))).First}, new Results((V)"no ordinary enemy"))));
V v_411 = values_410[0];
var values_412 = R.Join(new V[] {R.Get(R.Get(v_411, (V)"position"), (V)"x"), R.Get(R.Get(v_411, (V)"position"), (V)"y")}, new Results(R.Get(v_411, (V)"id")));
V v_413 = values_412[0];
V v_414 = values_412[1];
V v_415 = values_412[2];
var values_416 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"fight"), R.Join(new V[] {v_403, v_415}, new Results((V)120.0d))));
V v_417 = values_416[0];
V v_418 = values_416[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_417}, new Results(v_418)));
var values_419 = R.Join(new V[] {}, new Results((V)(v_407 + (V)1.0d)));
v_407 = values_419[0];
var values_422 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_403, new V((RuleFunction)(args_420 => {
V v_421 = args_420[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_421, (V)"x") == v_413), () => (V)(R.Get(v_421, (V)"y") == v_414))));
return Results.Empty;
}))}, new Results((V)150.0d))));
v_417 = values_422[0];
v_418 = values_422[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_417}, new Results(v_418)));
if (R.Truth(R.Or(R.And((V)(R.Get(v_405, (V)"currency") >= (V)5.0d), () => (V)(v_407 >= (V)1.0d)), () => (V)(v_407 >= (V)6.0d)))) break;
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_405, (V)"currency") >= (V)5.0d), () => (V)(!R.Truth(R.Get(v_405, (V)"game_over"))))}, new Results((V)"could not earn chest price")));
R.Call(v_389, R.Join(new V[] {v_403}, new Results((V)"earned_currency")));
var values_428 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_10, (V)"nearest"), R.Join(new V[] {v_405}, new Results(new V((RuleFunction)(args_423 => {
V v_424 = args_423[0];
if (R.Truth(R.Or(R.Or((V)(R.Get(v_424, (V)"controller") != (V)"chest"), () => R.Get(v_424, (V)"claimed")), () => (V)(R.Get(v_424, (V)"price") <= (V)0.0d)))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_425 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_424, (V)"offers"))))))) {
V v_426 = row_425[0];
V v_427 = row_425[1];
if (R.Truth((V)(R.Get(v_427, (V)"boon_id") == (V)"core:boon/emberbrand"))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}))))).First}, new Results((V)"no paid chest with Emberbrand"))));
V v_429 = values_428[0];
var values_432 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_403, new V((RuleFunction)(args_430 => {
V v_431 = args_430[0];
return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {v_431}, new Results(R.Get(v_429, (V)"position")))));
return Results.Empty;
}))}, new Results((V)200.0d))));
V v_433 = values_432[0];
V v_434 = values_432[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_433}, new Results(v_434)));
var values_435 = R.Join(new V[] {R.Get(v_405, (V)"clock")}, new Results(R.Get(v_405, (V)"currency")));
V v_436 = values_435[0];
V v_437 = values_435[1];
var values_438 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"chest_options"), R.Join(new V[] {v_405}, new Results(R.Get(v_429, (V)"id")))))));
V v_439 = values_438[0];
var values_440 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"chest_options"), R.Join(new V[] {v_405}, new Results(R.Get(v_429, (V)"id")))))));
V v_441 = values_440[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(R.Get(v_439, (V)"offers")) == (V)3.0d), () => (V)((V)R.Length(R.Get(v_441, (V)"offers")) == (V)3.0d)), () => (V)(R.Get(v_405, (V)"clock") == v_436)), () => (V)(R.Get(v_405, (V)"currency") == v_437)))));
{ double end_444 = R.Num((V)3.0d), step_445 = R.Num((V)1.0d);
for (double i_443 = R.Num((V)1.0d); step_445 > 0 ? i_443 <= end_444 : i_443 >= end_444; i_443 += step_445) {
V v_442 = i_443;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_439, (V)"offers"), v_442), (V)"boon_id") == R.Get(R.Get(R.Get(v_441, (V)"offers"), v_442), (V)"boon_id")), () => (V)(R.Get(R.Get(R.Get(v_439, (V)"offers"), v_442), (V)"rarity") == R.Get(R.Get(R.Get(v_441, (V)"offers"), v_442), (V)"rarity"))))));
}
}
var values_446 = Results.Empty;
V v_447 = values_446[0];
foreach (var row_448 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_439, (V)"offers"))))))) {
V v_449 = row_448[0];
V v_450 = row_448[1];
if (R.Truth((V)(R.Get(v_450, (V)"boon_id") == (V)"core:boon/emberbrand"))) {
var values_451 = R.Join(new V[] {}, new Results(v_449));
v_447 = values_451[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_447}, new Results((V)"selected chest lost Emberbrand")));
var values_452 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_403}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_429, (V)"id")), new Entry((V)"offer_index", v_447))))));
v_433 = values_452[0];
v_434 = values_452[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_433}, new Results(v_434)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_429, (V)"claimed"), () => (V)(R.Get(v_405, (V)"currency") == (V)(v_437 - R.Get(v_429, (V)"price")))))));
R.Call(v_389, R.Join(new V[] {v_403}, new Results((V)"paid_chest_claimed")));
var values_453 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_405}, new Results(R.Get(R.Get(v_405, (V)"stage"), (V)"elite_id")))).First}, new Results((V)"missing elite"))));
V v_454 = values_453[0];
var values_457 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_403, new V((RuleFunction)(args_455 => {
V v_456 = args_455[0];
return R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {v_456}, new Results(R.Get(v_454, (V)"position")))));
return Results.Empty;
}))}, new Results((V)400.0d))));
v_433 = values_457[0];
v_434 = values_457[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_433}, new Results(v_434)));
R.Call(v_389, R.Join(new V[] {v_403}, new Results((V)"elite_reached")));
var values_458 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"fight"), R.Join(new V[] {v_403, R.Get(v_454, (V)"id")}, new Results((V)150.0d))));
v_433 = values_458[0];
v_434 = values_458[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_433}, new Results(v_434)));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_10, (V)"has_event"), R.Join(new V[] {v_403}, new Results((V)"StatusApplied"))).First}, new Results((V)"earned Emberbrand never ignited a target")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_405, (V)"stage"), (V)"phase") == (V)"extraction"), () => (V)(!R.Truth(R.Get(v_405, (V)"game_over")))))));
R.Call(v_389, R.Join(new V[] {v_403}, new Results((V)"elite_defeated")));
var values_459 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_405}, new Results(R.Get(R.Get(v_405, (V)"stage"), (V)"elite_reward_id")))))));
V v_460 = values_459[0];
var values_463 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_403, new V((RuleFunction)(args_461 => {
V v_462 = args_461[0];
return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {v_462}, new Results(R.Get(v_460, (V)"position")))));
return Results.Empty;
}))}, new Results((V)30.0d))));
v_433 = values_463[0];
v_434 = values_463[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_433}, new Results(v_434)));
var values_464 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_403}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_460, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
v_433 = values_464[0];
v_434 = values_464[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_433}, new Results(v_434)));
R.Call(v_389, R.Join(new V[] {v_403}, new Results((V)"free_chest_claimed")));
var values_465 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_405}, new Results(R.Get(R.Get(v_405, (V)"stage"), (V)"exit_id")))))));
V v_466 = values_465[0];
var values_469 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_403, new V((RuleFunction)(args_467 => {
V v_468 = args_467[0];
return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {v_468}, new Results(R.Get(v_466, (V)"position")))));
return Results.Empty;
}))}, new Results((V)30.0d))));
v_433 = values_469[0];
v_434 = values_469[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_433}, new Results(v_434)));
var values_470 = R.Join(new V[] {}, new Results(R.Get(v_405, (V)"active_floor_id")));
V v_471 = values_470[0];
var values_472 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"route_options"), R.Join(new V[] {v_405}, new Results(R.Get(v_466, (V)"id")))))));
V v_473 = values_472[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_473, (V)"routes")) == (V)3.0d))));
var values_474 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_403}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_466, (V)"id")), new Entry((V)"route_index", R.Or(v_400, () => (V)1.0d)))))));
v_433 = values_474[0];
v_434 = values_474[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_433}, new Results(v_434)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_405, (V)"stage"), (V)"index") == (V)2.0d), () => (V)(R.Get(v_405, (V)"active_floor_id") != v_471)))));
R.Call(v_389, R.Join(new V[] {v_403}, new Results((V)"stage_two_entered")));
var values_477 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_10, (V)"nearest"), R.Join(new V[] {v_405}, new Results(new V((RuleFunction)(args_475 => {
V v_476 = args_475[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_476, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_476, (V)"id") != R.Get(R.Get(v_405, (V)"stage"), (V)"elite_id"))), () => (V)(!R.Truth(R.Get(v_476, (V)"invulnerable"))))));
return Results.Empty;
}))))).First}, new Results((V)"new stage has no ordinary enemy"))));
V v_478 = values_477[0];
var values_479 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"fight"), R.Join(new V[] {v_403, R.Get(v_478, (V)"id")}, new Results((V)120.0d))));
v_433 = values_479[0];
v_434 = values_479[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_433}, new Results(v_434)));
R.Call(v_389, R.Join(new V[] {v_403}, new Results((V)"stage_two_enemy_defeated")));
return R.Join(new V[] {}, new Results(v_403));
return Results.Empty;
})));
R.Set(v_10, (V)"biome_route", new V((RuleFunction)(args_480 => {
V v_481 = args_480[0];
V v_482 = args_480[1];
var values_483 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"stage_one"), R.Join(new V[] {v_481, (V)1.0d}, new Results(v_482))));
V v_484 = values_483[0];
var values_485 = R.Join(new V[] {}, new Results(R.Get(v_484, (V)"state")));
V v_486 = values_485[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_486, (V)"stage"), (V)"biome_id") == (V)"core:biome/fungal_hollows"))));
var values_489 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"nearest"), R.Join(new V[] {v_486}, new Results(new V((RuleFunction)(args_487 => {
V v_488 = args_487[0];
return R.Join(new V[] {}, new Results((V)(R.Get(v_488, (V)"definition_id") == (V)"core:enemy/spore_herald")));
return Results.Empty;
}))))))));
V v_490 = values_489[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"fight"), R.Join(new V[] {v_484, R.Get(v_490, (V)"id")}, new Results((V)160.0d)))));
R.Call(v_389, R.Join(new V[] {v_484}, new Results((V)"spore_herald_defeated")));
var values_491 = R.Join(new V[] {}, new Results(R.Get(v_486, (V)"currency")));
V v_492 = values_491[0];
var values_493 = R.Join(new V[] {R.Get(R.Get(v_490, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_490, (V)"position"), (V)"y")));
V v_494 = values_493[0];
V v_495 = values_493[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_484, new V((RuleFunction)(args_496 => {
V v_497 = args_496[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_497, (V)"x") == v_494), () => (V)(R.Get(v_497, (V)"y") == v_495))));
return Results.Empty;
}))}, new Results((V)150.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_486, (V)"currency") >= (V)(v_492 + R.Get(R.Call(R.Get(v_4, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_490, (V)"definition_id")))).First, (V)"currency_drop"))))));
R.Call(v_389, R.Join(new V[] {v_484}, new Results((V)"fungal_currency_collected")));
var values_498 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_486}, new Results(R.Get(R.Get(v_486, (V)"stage"), (V)"elite_id")))))));
V v_499 = values_498[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_484, new V((RuleFunction)(args_500 => {
V v_501 = args_500[0];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_501, (V)"x") - R.Get(R.Get(v_499, (V)"position"), (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_501, (V)"y") - R.Get(R.Get(v_499, (V)"position"), (V)"y"))))))).First == (V)1.0d)));
return Results.Empty;
}))}, new Results((V)500.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"fight"), R.Join(new V[] {v_484, R.Get(v_499, (V)"id")}, new Results((V)180.0d)))));
R.Call(v_389, R.Join(new V[] {v_484}, new Results((V)"fungal_elite_defeated")));
var values_502 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_486}, new Results(R.Get(R.Get(v_486, (V)"stage"), (V)"exit_id")))))));
V v_503 = values_502[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_484, new V((RuleFunction)(args_504 => {
V v_505 = args_504[0];
return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {v_505}, new Results(R.Get(v_503, (V)"position")))));
return Results.Empty;
}))}, new Results((V)100.0d)))));
var values_506 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"route_options"), R.Join(new V[] {v_486}, new Results(R.Get(v_503, (V)"id")))))));
V v_507 = values_506[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_507, (V)"routes"), (V)2.0d), (V)"biome_id") == (V)"core:biome/ashen_bastion"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_484}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_503, (V)"id")), new Entry((V)"route_index", (V)2.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_486, (V)"stage"), (V)"biome_id") == (V)"core:biome/ashen_bastion"))));
R.Call(v_389, R.Join(new V[] {v_484}, new Results((V)"ashen_stage_entered")));
var values_510 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"nearest"), R.Join(new V[] {v_486}, new Results(new V((RuleFunction)(args_508 => {
V v_509 = args_508[0];
return R.Join(new V[] {}, new Results((V)(R.Get(v_509, (V)"definition_id") == (V)"core:enemy/cinder_bellkeeper")));
return Results.Empty;
}))))))));
V v_511 = values_510[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"fight"), R.Join(new V[] {v_484, R.Get(v_511, (V)"id")}, new Results((V)170.0d)))));
R.Call(v_389, R.Join(new V[] {v_484}, new Results((V)"bellkeeper_defeated")));
return R.Join(new V[] {}, new Results(v_484));
return Results.Empty;
})));
R.Set(v_10, (V)"complete_run", new V((RuleFunction)(args_512 => {
V v_513 = args_512[0];
V v_514 = args_512[1];
var values_515 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"biome_route"), R.Join(new V[] {v_513}, new Results(v_514))));
V v_516 = values_515[0];
var values_517 = R.Join(new V[] {}, new Results(R.Get(v_516, (V)"state")));
V v_518 = values_517[0];
var values_519 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_518}, new Results(R.Get(R.Get(v_518, (V)"stage"), (V)"elite_id")))))));
V v_520 = values_519[0];
var values_523 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_516, new V((RuleFunction)(args_521 => {
V v_522 = args_521[0];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_522, (V)"x") - R.Get(R.Get(v_520, (V)"position"), (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_522, (V)"y") - R.Get(R.Get(v_520, (V)"position"), (V)"y"))))))).First == (V)1.0d)));
return Results.Empty;
}))}, new Results((V)600.0d))));
V v_524 = values_523[0];
V v_525 = values_523[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_524}, new Results(v_525)));
var values_526 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"fight"), R.Join(new V[] {v_516, R.Get(v_520, (V)"id")}, new Results((V)250.0d))));
v_524 = values_526[0];
v_525 = values_526[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_524}, new Results(v_525)));
R.Call(v_389, R.Join(new V[] {v_516}, new Results((V)"third_elite_defeated")));
var values_527 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_518}, new Results(R.Get(R.Get(v_518, (V)"stage"), (V)"elite_reward_id")))));
V v_528 = values_527[0];
if (R.Truth(R.And(v_528, () => (V)(!R.Truth(R.Get(v_528, (V)"claimed")))))) {
var values_531 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_516, new V((RuleFunction)(args_529 => {
V v_530 = args_529[0];
return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {v_530}, new Results(R.Get(v_528, (V)"position")))));
return Results.Empty;
}))}, new Results((V)30.0d))));
v_524 = values_531[0];
v_525 = values_531[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_524}, new Results(v_525)));
var values_532 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_516}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_528, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
v_524 = values_532[0];
v_525 = values_532[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_524}, new Results(v_525)));
}
var values_533 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_518))));
V v_534 = values_533[0];
foreach (var row_535 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_518, (V)"drops"))))))) {
V v_536 = row_535[0];
V v_537 = row_535[1];
var values_538 = R.Join(new V[] {}, new Results(R.Get(v_537, (V)"position")));
V v_539 = values_538[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_534, (V)"hp") < R.Get(v_534, (V)"max_hp")), () => (V)(!R.Truth(R.Get(v_537, (V)"collected")))), () => (V)(!R.Truth(R.Get(v_537, (V)"carried_by")))), () => R.Or((V)(R.Get(v_537, (V)"kind") == (V)"health"), () => (V)(R.Get(v_537, (V)"kind") == (V)"food"))), () => (V)(R.Get(v_539, (V)"floor_id") == R.Get(v_518, (V)"active_floor_id"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_539, (V)"x") - R.Get(R.Get(v_534, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_539, (V)"y") - R.Get(R.Get(v_534, (V)"position"), (V)"y"))))).First) <= (V)2.0d)))) {
var values_542 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_516, new V((RuleFunction)(args_540 => {
V v_541 = args_540[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_541, (V)"x") == R.Get(v_539, (V)"x")), () => (V)(R.Get(v_541, (V)"y") == R.Get(v_539, (V)"y")))));
return Results.Empty;
}))}, new Results((V)30.0d))));
v_524 = values_542[0];
v_525 = values_542[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_524}, new Results(v_525)));
}
}
R.Call(v_389, R.Join(new V[] {v_516}, new Results((V)"third_elite_recovery_collected")));
var values_543 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_518}, new Results(R.Get(R.Get(v_518, (V)"stage"), (V)"exit_id")))))));
V v_544 = values_543[0];
var values_547 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"navigate"), R.Join(new V[] {v_516, new V((RuleFunction)(args_545 => {
V v_546 = args_545[0];
return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {v_546}, new Results(R.Get(v_544, (V)"position")))));
return Results.Empty;
}))}, new Results((V)100.0d))));
v_524 = values_547[0];
v_525 = values_547[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_524}, new Results(v_525)));
var values_548 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"route_options"), R.Join(new V[] {v_518}, new Results(R.Get(v_544, (V)"id")))))));
V v_549 = values_548[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_549, (V)"routes"), (V)1.0d), (V)"id") == (V)"core:route/gateway"))));
var values_550 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_516}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_544, (V)"id")), new Entry((V)"route_index", (V)1.0d))))));
v_524 = values_550[0];
v_525 = values_550[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_524}, new Results(v_525)));
R.Call(v_389, R.Join(new V[] {v_516}, new Results((V)"final_sanctum_entered")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_518, (V)"finale"), () => (V)(!R.Truth(R.Get(v_518, (V)"victory")))))));
var values_551 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"fight"), R.Join(new V[] {v_516, R.Get(R.Get(v_518, (V)"finale"), (V)"boss_id")}, new Results((V)350.0d))));
v_524 = values_551[0];
v_525 = values_551[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_524}, new Results(v_525)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_518, (V)"victory"), () => R.Get(v_518, (V)"game_over")), () => R.Call(R.Get(v_10, (V)"has_event"), R.Join(new V[] {v_516}, new Results((V)"Victory"))).First))));
R.Call(v_389, R.Join(new V[] {v_516}, new Results((V)"victory")));
return R.Join(new V[] {}, new Results(v_516));
return Results.Empty;
})));
R.Set(v_10, (V)"replay", new V((RuleFunction)(args_552 => {
V v_553 = args_552[0];
var values_554 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"new"), R.Join(new V[] {}, new Results(R.Get(v_553, (V)"seed")))));
V v_555 = values_554[0];
foreach (var row_556 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_553, (V)"trace"))))))) {
V v_557 = row_556[0];
V v_558 = row_556[1];
var values_559 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"action"), R.Join(new V[] {v_555}, new Results(R.Get(v_558, (V)"intent")))));
V v_560 = values_559[0];
V v_561 = values_559[1];
V v_562 = values_559[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(v_560 == R.Get(v_558, (V)"ok")), () => (V)(v_561 == R.Get(v_558, (V)"reason")))}, new Results(R.Concat((V)"replay action differs at ", v_557))));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(R.Get(v_555, (V)"state"), (V)"clock") == R.Get(v_558, (V)"clock")), () => (V)((V)R.Length(v_562) == (V)R.Length(R.Get(v_558, (V)"event_kinds"))))}, new Results(R.Concat((V)"replay time/events differ at ", v_557))));
foreach (var row_563 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_562)))))) {
V v_564 = row_563[0];
V v_565 = row_563[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_565, (V)"kind") == R.Get(R.Get(v_558, (V)"event_kinds"), v_564))}, new Results(R.Concat((V)"replay event differs at ", R.Concat(v_557, R.Concat((V)":", v_564))))));
}
}
return R.Join(new V[] {}, new Results(v_555));
return Results.Empty;
})));
R.Set(v_10, (V)"cardinal", new V((RuleFunction)(args_566 => {
V v_567 = args_566[0];
V v_568 = args_566[1];
return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {v_567}, new Results(v_568))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_10));
return Results.Empty;
} }
