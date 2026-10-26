// Static C# port of tests/idea_wraith_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 22abd33ea621cb14f226a501ee9fe411fa5c1cd571a4093f31df4f92c3d55466
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_wraith_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_15 = values_14[0];
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {

var values_18 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_19 = values_18[0];

var values_20 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_19))));
V v_21 = values_20[0];
var values_22 = R.Join(new V[] {(V)8.0d}, new Results((V)7.0d));
var obj_23 = R.Get(v_21, (V)"position");
V key_24 = (V)"x";
var obj_25 = R.Get(v_21, (V)"position");
V key_26 = (V)"y";
R.Set(obj_23, key_24, values_22[0]);
R.Set(obj_25, key_26, values_22[1]);
var values_27 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_19, (V)"core:enemy/hollow_wraith", (V)5.0d, (V)7.0d}, new Results((V)0.0d))));
V v_28 = values_27[0];
R.Call(R.Get(v_9, (V)"recompute"), R.Join(new V[] {}, new Results(v_19)));
return R.Join(new V[] {v_19, v_21}, new Results(v_28));
return Results.Empty;
}));
V v_29 = V.Nil;
v_29 = new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
V v_32 = args_30[1];
var values_33 = R.Join(new V[] {}, new Results((V)0.0d));
V v_34 = values_33[0];

foreach (var row_35 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_31)))))) {
V v_36 = row_35[0];
V v_37 = row_35[1];
if (R.Truth((V)(R.Get(v_37, (V)"kind") == v_32))) {
var values_38 = R.Join(new V[] {}, new Results((V)(v_34 + (V)1.0d)));
v_34 = values_38[0];
}
}

return R.Join(new V[] {}, new Results(v_34));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Wraith announces a pure visible wall-crossing destination and attacks separately"}, new Results(new V((RuleFunction)(args_39 => {

var values_40 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_41 = values_40[0];
V v_42 = values_40[1];
V v_43 = values_40[2];

var values_44 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_41))));
V v_45 = values_44[0];
var values_46 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"choose"), R.Join(new V[] {v_41}, new Results(v_43))));
V v_47 = values_46[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_47, (V)"support"), (V)"kind") == (V)"phase"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_47, (V)"cells"), (V)1.0d), (V)"x") == (V)7.0d), () => (V)(R.Get(R.Get(R.Get(v_47, (V)"cells"), (V)1.0d), (V)"y") == (V)7.0d)))));
R.Call(v_3, R.Join(new V[] {v_45}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_41)))));
var values_48 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_41}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_49 = values_48[0];
V v_50 = values_48[1];
V v_51 = values_48[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_43, (V)"position"), (V)"x") == (V)5.0d), () => (V)(R.Get(R.Get(v_43, (V)"pending_attack"), (V)"due") == (V)111.0d)), () => (V)(R.Get(v_42, (V)"hp") == (V)24.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_29, R.Join(new V[] {v_51}, new Results((V)"WindupStarted"))).First == (V)1.0d), () => (V)(R.Call(v_29, R.Join(new V[] {v_51}, new Results((V)"DamageTaken"))).First == (V)0.0d)))));
var values_52 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_41}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_53 = values_52[0];
V v_54 = values_52[1];
V v_55 = values_52[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_43, (V)"position"), (V)"x") == (V)7.0d), () => (V)(R.Get(v_42, (V)"hp") == (V)24.0d)), () => (V)(R.Call(v_29, R.Join(new V[] {v_55}, new Results((V)"Displaced"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_29, R.Join(new V[] {v_55}, new Results((V)"AttackPerformed"))).First == (V)0.0d), () => (V)(R.Call(v_29, R.Join(new V[] {v_55}, new Results((V)"DamageTaken"))).First == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_41}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_42, (V)"hp") == (V)24.0d), () => R.Get(v_43, (V)"pending_attack")))));
var values_56 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_41}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_57 = values_56[0];
V v_58 = values_56[1];
V v_59 = values_56[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_42, (V)"hp") == (V)22.0d), () => (V)(R.Call(v_29, R.Join(new V[] {v_59}, new Results((V)"AttackPerformed"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"occupying the committed phase landing cancels relocation without an ambush hit"}, new Results(new V((RuleFunction)(args_60 => {

var values_61 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_62 = values_61[0];
V v_63 = values_61[1];
V v_64 = values_61[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_62}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_62}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))))));
V v_66 = values_65[0];
V v_67 = values_65[1];
V v_68 = values_65[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_63, (V)"position"), (V)"x") == (V)7.0d), () => (V)(R.Get(v_63, (V)"hp") == (V)24.0d)), () => (V)(R.Get(R.Get(v_64, (V)"position"), (V)"x") == (V)5.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_29, R.Join(new V[] {v_68}, new Results((V)"PhaseBlocked"))).First == (V)1.0d), () => (V)(R.Call(v_29, R.Join(new V[] {v_68}, new Results((V)"DamageTaken"))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"legal driver recognizes a phase marker as counterable support rather than damage"}, new Results(new V((RuleFunction)(args_69 => {

var values_70 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_71 = values_70[0];
V v_72 = values_70[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_71}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_73 = R.Join(new V[] {}, new Results((V)7.0d));
var obj_74 = R.Get(v_72, (V)"position");
V key_75 = (V)"x";
R.Set(obj_74, key_75, values_73[0]);
var values_76 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_71))));
V v_77 = values_76[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))).First, (V)"escape"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_71), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table()))))).First)))));
R.Call(v_3, R.Join(new V[] {v_77}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_71)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"phase landing excludes stairs, exit cells and hidden destinations"}, new Results(new V((RuleFunction)(args_78 => {

var values_79 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_80 = values_79[0];
V v_81 = values_79[1];
V v_82 = values_79[2];

var values_83 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"map_for"), R.Join(new V[] {}, new Results(v_80))));
V v_84 = values_83[0];
var values_85 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_80, (V)"active_floor_id")), new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)7.0d))));
V v_86 = values_85[0];
var values_88 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_87 => {

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))));
V v_89 = values_88[0];

var values_91 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_90 => {

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))));
V v_92 = values_91[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"phase_landing"), R.Join(new V[] {v_80, v_82, v_86, v_84, v_89}, new Results(v_92)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_11, (V)"phase_landing"), R.Join(new V[] {v_80, v_82, v_86, v_84, v_89}, new Results(new V((RuleFunction)(args_93 => {

return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}))))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_11, (V)"phase_landing"), R.Join(new V[] {v_80, v_82, R.Table(new Entry((V)"floor_id", R.Get(v_86, (V)"floor_id")), new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)7.0d)), v_84, v_89}, new Results(v_92))).First)))));
var values_94 = R.Join(new V[] {}, new Results(R.Get(v_84, (V)"placements")));
V v_95 = values_94[0];
var values_96 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"placements", R.Table(new Entry((V)"stairs", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)7.0d))))))))));
V v_97 = values_96[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_11, (V)"phase_landing"), R.Join(new V[] {v_80, v_82, v_86, v_97, v_89}, new Results(v_92))).First)))));
var values_98 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"controller", (V)"exit"), new Entry((V)"position", v_86))));
var obj_99 = R.Get(v_80, (V)"entities");
V key_100 = (V)((V)R.Length(R.Get(v_80, (V)"entities")) + (V)1.0d);
R.Set(obj_99, key_100, values_98[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_11, (V)"phase_landing"), R.Join(new V[] {v_80, v_82, v_86, v_84, v_89}, new Results(v_92))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_84, (V)"placements") == v_95))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"current-position visibility query matches recompute without changing exploration or RNG"}, new Results(new V((RuleFunction)(args_101 => {

var values_102 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_103 = values_102[0];
V v_104 = values_102[1];

var values_105 = R.Join(new V[] {(V)4.0d}, new Results((V)5.0d));
var obj_106 = R.Get(v_104, (V)"position");
V key_107 = (V)"x";
var obj_108 = R.Get(v_104, (V)"position");
V key_109 = (V)"y";
R.Set(obj_106, key_107, values_105[0]);
R.Set(obj_108, key_109, values_105[1]);
var values_110 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_103))));
V v_111 = values_110[0];

var values_112 = R.Join(new V[] {}, new Results(R.Table()));
V v_113 = values_112[0];
var values_114 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"map_for"), R.Join(new V[] {}, new Results(v_103))));
V v_115 = values_114[0];
{ double end_118 = R.Num(R.Get(v_115, (V)"height")), step_119 = R.Num((V)1.0d);
for (double i_117 = R.Num((V)1.0d); step_119 > 0 ? i_117 <= end_118 : i_117 >= end_118; i_117 += step_119) {
V v_116 = i_117;
{ double end_122 = R.Num(R.Get(v_115, (V)"width")), step_123 = R.Num((V)1.0d);
for (double i_121 = R.Num((V)1.0d); step_123 > 0 ? i_121 <= end_122 : i_121 >= end_122; i_121 += step_123) {
V v_120 = i_121;
var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"can_see"), R.Join(new V[] {v_103, R.Get(v_103, (V)"active_floor_id"), v_120}, new Results(v_116))));
var obj_125 = v_113;
V key_126 = (V)((V)(v_116 * R.Get(v_115, (V)"width")) + v_120);
R.Set(obj_125, key_126, values_124[0]);
}
}
}
}
R.Call(v_3, R.Join(new V[] {v_111}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_103)))));

R.Call(R.Get(v_9, (V)"recompute"), R.Join(new V[] {}, new Results(v_103)));
{ double end_129 = R.Num(R.Get(v_115, (V)"height")), step_130 = R.Num((V)1.0d);
for (double i_128 = R.Num((V)1.0d); step_130 > 0 ? i_128 <= end_129 : i_128 >= end_129; i_128 += step_130) {
V v_127 = i_128;
{ double end_133 = R.Num(R.Get(v_115, (V)"width")), step_134 = R.Num((V)1.0d);
for (double i_132 = R.Num((V)1.0d); step_134 > 0 ? i_132 <= end_133 : i_132 >= end_133; i_132 += step_134) {
V v_131 = i_132;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_113, (V)((V)(v_127 * R.Get(v_115, (V)"width")) + v_131)) == R.Call(R.Get(v_9, (V)"is_visible"), R.Join(new V[] {v_103, R.Get(v_103, (V)"active_floor_id"), v_131}, new Results(v_127))).First))));
}
}
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a marked phase arrival triggers the finite Trapper snare once"}, new Results(new V((RuleFunction)(args_135 => {

var values_136 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_137 = values_136[0];
V v_138 = values_136[1];
V v_139 = values_136[2];
var values_140 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"snare", R.Table(new Entry((V)"floor_id", R.Get(v_137, (V)"active_floor_id")), new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)7.0d))))));
var obj_141 = v_137;
V key_142 = (V)"class_special";
R.Set(obj_141, key_142, values_140[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_137}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_143 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_137}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_144 = values_143[0];
V v_145 = values_143[1];
V v_146 = values_143[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Get(R.Get(v_139, (V)"position"), (V)"x") == (V)7.0d), () => (V)(R.Get(v_139, (V)"hp") == (V)4.0d)), () => (V)(!R.Truth(R.Get(R.Get(v_137, (V)"class_special"), (V)"snare")))), () => R.Get(v_139, (V)"skip_next_turn")), () => (V)(R.Call(v_29, R.Join(new V[] {v_146}, new Results((V)"SnareTriggered"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_137}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_139, (V)"skip_next_turn"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"committed phase save restores exact continuation and rejects forged geometry"}, new Results(new V((RuleFunction)(args_147 => {

var values_148 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_149 = values_148[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_149}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_150 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_149))));
V v_151 = values_150[0];
var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, new Results(v_151))))))));
V v_153 = values_152[0];
var values_154 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_149}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_155 = values_154[0];
V v_156 = values_154[1];
V v_157 = values_154[2];

var values_158 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_153}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_159 = values_158[0];
V v_160 = values_158[1];
V v_161 = values_158[2];
R.Call(v_3, R.Join(new V[] {v_157}, new Results(v_161)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_149))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_153)))));
foreach (var row_162 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_151, (V)"entities"))))))) {
V v_163 = row_162[0];
V v_164 = row_162[1];
if (R.Truth(R.Get(v_164, (V)"pending_attack"))) {
var values_165 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_166 = R.Get(R.Get(R.Get(v_164, (V)"pending_attack"), (V)"target_cells"), (V)1.0d);
V key_167 = (V)"x";
R.Set(obj_166, key_167, values_165[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_151))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stun and displacement cancel phase while playback cannot move simulation through walls"}, new Results(new V((RuleFunction)(args_168 => {

foreach (var row_169 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"stun"), new Entry((V)2, (V)"displace")))))))) {
V v_170 = row_169[0];
V v_171 = row_169[1];
var values_172 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_173 = values_172[0];
V v_174 = values_172[1];
V v_175 = values_172[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_173}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
if (R.Truth((V)(v_171 == (V)"stun"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_173, R.Get(v_175, (V)"id")}, new Results((V)100.0d)))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"displace"), R.Join(new V[] {v_173, R.Get(v_175, (V)"id"), (V)5.0d}, new Results((V)6.0d)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_175, (V)"pending_attack"))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_173}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_175, (V)"position"), (V)"x") == (V)5.0d), () => (V)(R.Get(v_174, (V)"hp") == (V)24.0d)))));
}
var values_176 = R.Join(new V[] {}, R.Call(v_16, Results.Empty));
V v_177 = values_176[0];
V v_178 = values_176[1];
V v_179 = values_176[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_177}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_180 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_177}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_181 = values_180[0];
V v_182 = values_180[1];
V v_183 = values_180[2];
var values_184 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_177))));
V v_185 = values_184[0];

var values_186 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"new"), Results.Empty));
V v_187 = values_186[0];

R.Call(R.Get(v_15, (V)"add"), R.Join(new V[] {v_187, v_183}, new Results(R.Get(v_178, (V)"id"))));
var values_188 = R.Join(new V[] {}, new Results((V)false));
V v_189 = values_188[0];
foreach (var row_190 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_187, (V)"items"))))))) {
V v_191 = row_190[0];
V v_192 = row_190[1];
var values_193 = R.Join(new V[] {}, new Results(R.And(R.Get(v_192, (V)"motions"), () => R.Get(R.Get(v_192, (V)"motions"), R.Get(v_179, (V)"id")))));
V v_194 = values_193[0];
if (R.Truth(v_194)) {
var values_195 = R.Join(new V[] {}, new Results((V)true));
v_189 = values_195[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_194, (V)"snap"))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_189)));

{ double end_198 = R.Num((V)100.0d), step_199 = R.Num((V)1.0d);
for (double i_197 = R.Num((V)1.0d); step_199 > 0 ? i_197 <= end_198 : i_197 >= end_198; i_197 += step_199) {
V v_196 = i_197;
R.Call(R.Get(v_15, (V)"update"), R.Join(new V[] {v_187}, new Results((V)0.02d)));
}
}
R.Call(v_3, R.Join(new V[] {v_185}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_177)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"redirected Wraith art is exactly 416 in the fixed 1-Bit renderer and natural floors actually contain it"}, new Results(new V((RuleFunction)(args_200 => {

var values_201 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_202 = values_201[0];
foreach (var row_203 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_204 = row_203[0];
V v_205 = row_203[1];
var values_206 = R.Join(new V[] {}, new Results((V)0.0d));
V v_207 = values_206[0];
foreach (var row_208 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_202, (V)"packs"), v_205), (V)"bindings"))))))) {
V v_209 = row_208[0];
V v_210 = row_208[1];
if (R.Truth(R.Invoke(R.Get(v_210, (V)"id"), "match", R.Join(new V[] {}, new Results((V)"^core:enemy/hollow_wraith"))).First)) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_210, (V)"sheet") == (V)"onebit"), () => (V)(R.Get(v_210, (V)"index") == (V)416.0d)))));

var values_211 = R.Join(new V[] {}, new Results((V)(v_207 + (V)1.0d)));
v_207 = values_211[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_207 == (V)3.0d))));
}
var values_212 = R.Join(new V[] {}, new Results((V)false));
V v_213 = values_212[0];
{ double end_216 = R.Num((V)100.0d), step_217 = R.Num((V)1.0d);
for (double i_215 = R.Num((V)1.0d); step_217 > 0 ? i_215 <= end_216 : i_215 >= end_216; i_215 += step_217) {
V v_214 = i_215;
var values_218 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_214}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_219 = values_218[0];
foreach (var row_220 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_219, (V)"entities"))))))) {
V v_221 = row_220[0];
V v_222 = row_220[1];
if (R.Truth((V)(R.Get(v_222, (V)"definition_id") == (V)"core:enemy/hollow_wraith"))) {
var values_223 = R.Join(new V[] {}, new Results((V)true));
v_213 = values_223[0];
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_219))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_219)))))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_213}, new Results((V)"Wraith not consumed by actual floor generation")));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
