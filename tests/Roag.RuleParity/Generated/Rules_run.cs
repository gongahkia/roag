// Static C# port of tests/run.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 6095e8da885745d7a9e94d5ccd8848d01236fa28e762b16b90ddebef051067bf
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_run { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Concat((V)"./?.lua;./?/init.lua;", R.Get(rt.Global("package"), (V)"path"))));
var obj_2 = rt.Global("package");
V key_3 = (V)"path";
R.Set(obj_2, key_3, values_1[0]);
R.Invoke(R.Get(rt.Global("io"), (V)"stdout"), "setvbuf", R.Join(new V[] {}, new Results((V)"line")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(rt.Global("_G"), (V)"love") == V.Nil)}, new Results((V)"headless tests must not initialize L\u00c3\u0096VE")));
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.camera"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.targeting"))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_21 = values_20[0];
var values_22 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_23 = values_22[0];
var values_24 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_25 = values_24[0];
var values_26 = R.Join(new V[] {(V)0.0d, (V)0.0d}, new Results((V)0.0d));
V v_27 = values_26[0];
V v_28 = values_26[1];
V v_29 = values_26[2];
var values_30 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("os"), (V)"getenv"), R.Join(new V[] {}, new Results((V)"ROAG_TEST_RANGE"))));
V v_31 = values_30[0];
var values_32 = Results.Empty;
V v_33 = values_32[0];
V v_34 = values_32[1];
if (R.Truth(v_31)) {
var values_35 = R.Join(new V[] {}, R.Invoke(v_31, "match", R.Join(new V[] {}, new Results((V)"^(%d+)%-(%d+)$"))));
V v_36 = values_35[0];
V v_37 = values_35[1];
var values_38 = R.Join(new V[] {R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_36))).First}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_37))));
v_33 = values_38[0];
v_34 = values_38[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(v_33, () => v_34), () => (V)(v_33 >= (V)1.0d)), () => (V)(v_34 >= v_33))}, new Results((V)"invalid ROAG_TEST_RANGE")));
}
V v_39 = V.Nil;
v_39 = new V((RuleFunction)(args_40 => {
V v_41 = args_40[0];
V v_42 = args_40[1];
V v_43 = args_40[2];
var values_44 = R.Join(new V[] {}, new Results(R.Or(v_43, () => (V)"root")));
v_43 = values_44[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_41))).First == R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_42))).First)}, new Results(R.Concat(v_43, (V)" type differs"))));
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_41))).First != (V)"table"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_41 == v_42)}, new Results(R.Concat(v_43, (V)" differs"))));

return Results.Empty;
}
foreach (var row_45 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_41)))))) {
V v_46 = row_45[0];
V v_47 = row_45[1];
R.Call(v_39, R.Join(new V[] {v_47, R.Get(v_42, v_46)}, new Results(R.Concat(v_43, R.Concat((V)".", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_46))).First)))));
}
foreach (var row_48 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_42)))))) {
V v_49 = row_48[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_41, v_49) != V.Nil)}, new Results(R.Concat(v_43, R.Concat((V)" extra key ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_49))).First)))));
}
return Results.Empty;
}));
V v_50 = V.Nil;
v_50 = new V((RuleFunction)(args_51 => {
V v_52 = args_51[0];
V v_53 = args_51[1];
var values_54 = R.Join(new V[] {}, new Results((V)(v_29 + (V)1.0d)));
v_29 = values_54[0];
if (R.Truth(R.And(v_31, () => R.Or((V)(v_29 < v_33), () => (V)(v_29 > v_34))))) {
return Results.Empty;
}
var values_55 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(v_53))));
V v_56 = values_55[0];
V v_57 = values_55[1];
if (R.Truth(v_56)) {
var values_58 = R.Join(new V[] {}, new Results((V)(v_27 + (V)1.0d)));
v_27 = values_58[0];

R.Call(rt.Global("print"), R.Join(new V[] {}, new Results(R.Concat((V)"PASS ", v_52))));
} else {
var values_59 = R.Join(new V[] {}, new Results((V)(v_28 + (V)1.0d)));
v_28 = values_59[0];

R.Invoke(R.Get(rt.Global("io"), (V)"stderr"), "write", R.Join(new V[] {}, new Results(R.Concat((V)"FAIL ", R.Concat(v_52, R.Concat((V)": ", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_57))).First, (V)"\n")))))));
}
return Results.Empty;
}));
R.Call(v_50, R.Join(new V[] {(V)"map content identity and validation"}, new Results(new V((RuleFunction)(args_60 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"validate_all"), Results.Empty)));
var values_61 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/test_room"))));
V v_62 = values_61[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_62, (V)"width") == (V)15.0d), () => (V)(R.Get(v_62, (V)"height") == (V)11.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_62, (V)"id") == (V)"core:map/test_room"))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"idle state unchanged"}, new Results(new V((RuleFunction)(args_63 => {

var values_64 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)123.0d))));
V v_65 = values_64[0];
var values_66 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_65))));
V v_67 = values_66[0];
{ double end_70 = R.Num((V)10000.0d), step_71 = R.Num((V)1.0d);
for (double i_69 = R.Num((V)1.0d); step_71 > 0 ? i_69 <= end_70 : i_69 >= end_70; i_69 += step_71) {
V v_68 = i_69;
R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_65)));
}
}
R.Call(v_39, R.Join(new V[] {v_67}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_65)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"four legal cardinal movements cost 100 each"}, new Results(new V((RuleFunction)(args_72 => {

var values_73 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)123.0d))));
V v_74 = values_73[0];
var values_75 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))))));
V v_76 = values_75[0];
foreach (var row_77 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_76)))))) {
V v_78 = row_77[0];
V v_79 = row_77[1];
var values_80 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_74}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_79, (V)1.0d)), new Entry((V)"dy", R.Get(v_79, (V)2.0d)))))));
V v_81 = values_80[0];
V v_82 = values_80[1];
V v_83 = values_80[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_81, () => (V)((V)R.Length(v_83) == (V)2.0d)), () => (V)(R.Get(R.Get(v_83, (V)1.0d), (V)"kind") == (V)"Moved")), () => (V)(R.Get(R.Get(v_83, (V)2.0d), (V)"kind") == (V)"TileEntered")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_83, (V)2.0d), (V)"x") == R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_74))).First, (V)"position"), (V)"x")), () => (V)(R.Get(R.Get(v_83, (V)2.0d), (V)"y") == R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_74))).First, (V)"position"), (V)"y"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_74, (V)"clock") == (V)(v_78 * (V)100.0d)))));
}
var values_84 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_74))).First, (V)"position")));
V v_85 = values_84[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_85, (V)"x") == (V)2.0d), () => (V)(R.Get(v_85, (V)"y") == (V)2.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"wall movement consumes no time"}, new Results(new V((RuleFunction)(args_86 => {

var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)123.0d))));
V v_88 = values_87[0];
var values_89 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_88))));
V v_90 = values_89[0];
var values_91 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_88}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))))));
V v_92 = values_91[0];
V v_93 = values_91[1];
V v_94 = values_91[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(v_92)), () => (V)(v_93 == (V)"wall")), () => (V)((V)R.Length(v_94) == (V)0.0d)))));
R.Call(v_39, R.Join(new V[] {v_90}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_88)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"pending effects survive snapshot and precede ready actor"}, new Results(new V((RuleFunction)(args_95 => {

var values_96 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)88.0d))));
V v_97 = values_96[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_97}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(R.Get(v_11, (V)"enqueue"), R.Join(new V[] {v_97}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", (V)200.0d), new Entry((V)"effect_id", (V)"test:future")))));
var values_98 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_97))))));
V v_99 = values_98[0];
var values_100 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_97}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_101 = values_100[0];
V v_102 = values_100[1];
V v_103 = values_100[2];
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_99}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_105 = values_104[0];
V v_106 = values_104[1];
V v_107 = values_104[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_101, () => v_105))));
R.Call(v_39, R.Join(new V[] {v_103}, new Results(v_107)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_103) == (V)2.0d), () => (V)(R.Get(R.Get(v_103, (V)2.0d), (V)"kind") == (V)"ScheduledEffectResolved")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_103, (V)2.0d), (V)"time") == (V)200.0d), () => (V)(R.Get(v_97, (V)"clock") == (V)200.0d)))));
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_97))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_99)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"empty eight-direction attacks resolve immediately and cost 100"}, new Results(new V((RuleFunction)(args_108 => {

var values_109 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)123.0d))));
V v_110 = values_109[0];
var values_111 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)6, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)7, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)8, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))))));
V v_112 = values_111[0];
foreach (var row_113 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_112)))))) {
V v_114 = row_113[0];
V v_115 = row_113[1];
var values_116 = R.Join(new V[] {}, new Results(R.Get(v_110, (V)"clock")));
V v_117 = values_116[0];
var values_118 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_110}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_115, (V)1.0d)), new Entry((V)"dy", R.Get(v_115, (V)2.0d)))))));
V v_119 = values_118[0];
V v_120 = values_118[1];
V v_121 = values_118[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_119, () => (V)((V)R.Length(v_121) == (V)1.0d)), () => (V)(R.Get(R.Get(v_121, (V)1.0d), (V)"kind") == (V)"AttackPerformed")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_121, (V)1.0d), (V)"time") == v_117), () => (V)(R.Get(v_110, (V)"clock") == (V)(v_114 * (V)100.0d))))));
}
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"targeting cancel consumes no time"}, new Results(new V((RuleFunction)(args_122 => {

var values_123 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)123.0d))));
V v_124 = values_123[0];
var values_125 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_124))));
V v_126 = values_125[0];
var values_127 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_124}, new Results(R.Table(new Entry((V)"kind", (V)"cancel"))))));
V v_128 = values_127[0];
V v_129 = values_127[1];
V v_130 = values_127[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(v_128)), () => (V)(v_129 == (V)"cancelled")), () => (V)((V)R.Length(v_130) == (V)0.0d)))));
R.Call(v_39, R.Join(new V[] {v_126}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_124)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"wait costs 100 and invalid directions do not commit"}, new Results(new V((RuleFunction)(args_131 => {

var values_132 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)123.0d))));
V v_133 = values_132[0];
var values_134 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_133))));
V v_135 = values_134[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_133}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))).First)))));
R.Call(v_39, R.Join(new V[] {v_135}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_133)))));
var values_136 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_133}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_137 = values_136[0];
V v_138 = values_136[1];
V v_139 = values_136[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_137, () => (V)(R.Get(v_133, (V)"clock") == (V)100.0d)), () => (V)(R.Get(R.Get(v_139, (V)1.0d), (V)"kind") == (V)"Waited")))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"scheduler tie order: committed effects, player, stable enemies"}, new Results(new V((RuleFunction)(args_140 => {

var values_141 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"clock", (V)0.0d), new Entry((V)"schedule", R.Table()), new Entry((V)"next_schedule_id", (V)1.0d))));
V v_142 = values_141[0];
R.Call(R.Get(v_11, (V)"enqueue"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)100.0d), new Entry((V)"actor_id", (V)10.0d), new Entry((V)"is_player", (V)false)))));
R.Call(R.Get(v_11, (V)"enqueue"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)100.0d), new Entry((V)"actor_id", (V)1.0d), new Entry((V)"is_player", (V)true)))));
R.Call(R.Get(v_11, (V)"enqueue"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)100.0d), new Entry((V)"actor_id", (V)11.0d), new Entry((V)"is_player", (V)false)))));
R.Call(R.Get(v_11, (V)"enqueue"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", (V)100.0d), new Entry((V)"effect_id", (V)"test:first")))));
R.Call(R.Get(v_11, (V)"enqueue"), R.Join(new V[] {v_142}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", (V)100.0d), new Entry((V)"effect_id", (V)"test:second")))));
var values_143 = R.Join(new V[] {}, new Results(R.Table()));
V v_144 = values_143[0];
{ double end_147 = R.Num((V)5.0d), step_148 = R.Num((V)1.0d);
for (double i_146 = R.Num((V)1.0d); step_148 > 0 ? i_146 <= end_147 : i_146 >= end_147; i_146 += step_148) {
V v_145 = i_146;
var values_149 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_11, (V)"pop"), R.Join(new V[] {}, new Results(v_142))).First, (V)"id")));
var obj_150 = v_144;
V key_151 = (V)((V)R.Length(v_144) + (V)1.0d);
R.Set(obj_150, key_151, values_149[0]);
}
}
R.Call(v_39, R.Join(new V[] {v_144}, new Results(R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)5.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)3.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"RNG same seed and saved state reproduce draws"}, new Results(new V((RuleFunction)(args_152 => {

var values_153 = R.Join(new V[] {R.Call(R.Get(v_9, (V)"new"), R.Join(new V[] {}, new Results((V)987654.0d))).First}, R.Call(R.Get(v_9, (V)"new"), R.Join(new V[] {}, new Results((V)987654.0d))));
V v_154 = values_153[0];
V v_155 = values_153[1];
{ double end_158 = R.Num((V)20.0d), step_159 = R.Num((V)1.0d);
for (double i_157 = R.Num((V)1.0d); step_159 > 0 ? i_157 <= end_158 : i_157 >= end_158; i_157 += step_159) {
V v_156 = i_157;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_9, (V)"next_int"), R.Join(new V[] {v_154}, new Results((V)17.0d))).First == R.Call(R.Get(v_9, (V)"next_int"), R.Join(new V[] {v_155}, new Results((V)17.0d))).First))));
}
}
var values_160 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", R.Get(v_154, (V)"state")))));
V v_161 = values_160[0];
var values_162 = R.Join(new V[] {}, new Results(R.Table()));
V v_163 = values_162[0];
{ double end_166 = R.Num((V)20.0d), step_167 = R.Num((V)1.0d);
for (double i_165 = R.Num((V)1.0d); step_167 > 0 ? i_165 <= end_166 : i_165 >= end_166; i_165 += step_167) {
V v_164 = i_165;
var values_168 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"next_int"), R.Join(new V[] {v_154}, new Results((V)7.0d))));
var obj_169 = v_163;
V key_170 = v_164;
R.Set(obj_169, key_170, values_168[0]);
}
}
var values_171 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new"), R.Join(new V[] {}, new Results(R.Get(v_161, (V)"state")))));
V v_172 = values_171[0];
{ double end_175 = R.Num((V)20.0d), step_176 = R.Num((V)1.0d);
for (double i_174 = R.Num((V)1.0d); step_176 > 0 ? i_174 <= end_175 : i_174 >= end_175; i_174 += step_176) {
V v_173 = i_174;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_9, (V)"next_int"), R.Join(new V[] {v_172}, new Results((V)7.0d))).First == R.Get(v_163, v_173)))));
}
}
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"fixed seed and intents replay state and ordered events"}, new Results(new V((RuleFunction)(args_177 => {

var values_178 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (-(V)1.0d)))), new Entry((V)4, R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))))));
V v_179 = values_178[0];
V v_180 = V.Nil;
v_180 = new V((RuleFunction)(args_181 => {

var values_182 = R.Join(new V[] {R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)77.0d))).First}, new Results(R.Table()));
V v_183 = values_182[0];
V v_184 = values_182[1];
R.Call(R.Get(v_9, (V)"next_int"), R.Join(new V[] {R.Get(v_183, (V)"rng")}, new Results((V)100.0d)));
foreach (var row_185 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_179)))))) {
V v_186 = row_185[0];
V v_187 = row_185[1];
var values_188 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_183}, new Results(v_187))));
V v_189 = values_188[0];
V v_190 = values_188[1];
V v_191 = values_188[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_189)));
foreach (var row_192 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_191)))))) {
V v_193 = row_192[0];
V v_194 = row_192[1];
var values_195 = R.Join(new V[] {}, new Results(v_194));
var obj_196 = v_184;
V key_197 = (V)((V)R.Length(v_184) + (V)1.0d);
R.Set(obj_196, key_197, values_195[0]);
}
}
return R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_183))).First}, new Results(v_184));
return Results.Empty;
}));
var values_198 = R.Join(new V[] {}, R.Call(v_180, Results.Empty));
V v_199 = values_198[0];
V v_200 = values_198[1];
var values_201 = R.Join(new V[] {}, R.Call(v_180, Results.Empty));
V v_202 = values_201[0];
V v_203 = values_201[1];
R.Call(v_39, R.Join(new V[] {v_199}, new Results(v_202)));
R.Call(v_39, R.Join(new V[] {v_200}, new Results(v_203)));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"snapshot restore preserves subsequent simulation"}, new Results(new V((RuleFunction)(args_204 => {

var values_205 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)42.0d))));
V v_206 = values_205[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_206}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(R.Get(v_9, (V)"next_int"), R.Join(new V[] {R.Get(v_206, (V)"rng")}, new Results((V)50.0d)));
var values_207 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_206))));
V v_208 = values_207[0];
var values_209 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, new Results(v_208))));
V v_210 = values_209[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_210 != v_206), () => (V)(R.Get(v_210, (V)"entities") != R.Get(v_206, (V)"entities"))))));
R.Call(v_39, R.Join(new V[] {v_208}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_210)))));
var values_211 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))))));
V v_212 = values_211[0];
foreach (var row_213 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_212)))))) {
V v_214 = row_213[0];
V v_215 = row_213[1];
var values_216 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_206}, new Results(v_215))));
V v_217 = values_216[0];
V v_218 = values_216[1];
V v_219 = values_216[2];
var values_220 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_210}, new Results(v_215))));
V v_221 = values_220[0];
V v_222 = values_220[1];
V v_223 = values_220[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_217, () => v_221))));
R.Call(v_39, R.Join(new V[] {v_219}, new Results(v_223)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_9, (V)"next_int"), R.Join(new V[] {R.Get(v_206, (V)"rng")}, new Results((V)31.0d))).First == R.Call(R.Get(v_9, (V)"next_int"), R.Join(new V[] {R.Get(v_210, (V)"rng")}, new Results((V)31.0d))).First))));
}
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_206))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_210)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"snapshot rejects incompatible content"}, new Results(new V((RuleFunction)(args_224 => {

var values_225 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)1.0d))))));
V v_226 = values_225[0];
var values_227 = R.Join(new V[] {}, new Results((V)"other-content"));
var obj_228 = v_226;
V key_229 = (V)"content_version";
R.Set(obj_228, key_229, values_227[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"restore")}, new Results(v_226))).First)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"authored scrolling map is larger than the camera viewport"}, new Results(new V((RuleFunction)(args_230 => {

var values_231 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/scrolling_room"))));
V v_232 = values_231[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"validate_all"), Results.Empty)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_232, (V)"width") == (V)35.0d), () => (V)(R.Get(v_232, (V)"height") == (V)25.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)(R.Get(v_232, (V)"width") * (V)32.0d) > (V)704.0d), () => (V)((V)(R.Get(v_232, (V)"height") * (V)32.0d) > (V)608.0d)))));
var values_233 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {(V)1.0d}, new Results(R.Get(v_232, (V)"id")))));
V v_234 = values_233[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_234, (V)"map_id") == R.Get(v_232, (V)"id")))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Sword Strike keeps all eight adjacent targets and one recovery"}, new Results(new V((RuleFunction)(args_235 => {

var values_236 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)6, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))))));
V v_237 = values_236[0];
var values_238 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)2.0d))));
V v_239 = values_238[0];
foreach (var row_240 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_237)))))) {
V v_241 = row_240[0];
V v_242 = row_240[1];
var values_243 = R.Join(new V[] {}, new Results(R.Get(v_239, (V)"clock")));
V v_244 = values_243[0];
var values_245 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_239, (V)"basic_attack", R.Get(v_242, (V)1.0d)}, new Results(R.Get(v_242, (V)2.0d)))));
V v_246 = values_245[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_246, (V)"valid"), () => (V)((V)R.Length(R.Get(v_246, (V)"cells")) == (V)1.0d)))));
var values_247 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_239}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_242, (V)1.0d)), new Entry((V)"dy", R.Get(v_242, (V)2.0d)))))));
V v_248 = values_247[0];
V v_249 = values_247[1];
V v_250 = values_247[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_248, () => (V)((V)R.Length(v_250) == (V)1.0d)), () => (V)(R.Get(R.Get(v_250, (V)1.0d), (V)"kind") == (V)"AttackPerformed")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_250, (V)1.0d), (V)"time") == v_244), () => (V)(R.Get(R.Get(v_250, (V)1.0d), (V)"ability_id") == (V)"core:ability/basic_attack")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Get(v_250, (V)1.0d), (V)"target_cells"), (V)1.0d), (V)"x") == R.Get(R.Get(R.Get(v_246, (V)"cells"), (V)1.0d), (V)"x")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Get(v_250, (V)1.0d), (V)"target_cells"), (V)1.0d), (V)"y") == R.Get(R.Get(R.Get(v_246, (V)"cells"), (V)1.0d), (V)"y")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_239, (V)"clock") == (V)(v_241 * (V)100.0d)), () => (V)(R.Get(R.Get(v_239, (V)"last_action"), (V)"cost") == (V)100.0d)))));
}
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Sweep N and NE arcs use counterclockwise-center-clockwise order"}, new Results(new V((RuleFunction)(args_251 => {

var values_252 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_253 = values_252[0];
var values_254 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_253, (V)"sweeping_slash", (V)0.0d}, new Results((-(V)1.0d)))));
V v_255 = values_254[0];
R.Call(v_39, R.Join(new V[] {R.Get(v_255, (V)"cells")}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)1.0d), new Entry((V)"y", (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)1.0d)))))));
var values_256 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_253, (V)"sweeping_slash", (V)1.0d}, new Results((-(V)1.0d)))));
V v_257 = values_256[0];
R.Call(v_39, R.Join(new V[] {R.Get(v_257, (V)"cells")}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d)))))));
var values_258 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_253))));
V v_259 = values_258[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_255, (V)"valid"), () => R.Get(v_257, (V)"valid")))));
R.Call(v_39, R.Join(new V[] {v_259}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_253)))));
var values_260 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_253}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_261 = values_260[0];
V v_262 = values_260[1];
V v_263 = values_260[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_261, () => (V)((V)R.Length(v_263) == (V)1.0d)), () => (V)(R.Get(R.Get(v_263, (V)1.0d), (V)"kind") == (V)"AttackPerformed")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_263, (V)1.0d), (V)"time") == (V)0.0d), () => (V)(R.Get(R.Get(v_263, (V)1.0d), (V)"ability_id") == (V)"core:ability/sweeping_slash")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_253, (V)"clock") == (V)150.0d), () => (V)(R.Get(R.Get(v_253, (V)"last_action"), (V)"cost") == (V)150.0d)))));
foreach (var row_264 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_257, (V)"cells"))))))) {
V v_265 = row_264[0];
V v_266 = row_264[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(R.Get(v_263, (V)1.0d), (V)"target_cells"), v_265), (V)"x") == R.Get(v_266, (V)"x")), () => (V)(R.Get(R.Get(R.Get(R.Get(v_263, (V)1.0d), (V)"target_cells"), v_265), (V)"y") == R.Get(v_266, (V)"y"))))));
}
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"all eight Sweep arcs have three distinct adjacent cells"}, new Results(new V((RuleFunction)(args_267 => {

var values_268 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_269 = values_268[0];
var values_270 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)6, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))))));
V v_271 = values_270[0];
foreach (var row_272 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_271)))))) {
V v_273 = row_272[0];
V v_274 = row_272[1];
var values_275 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_269, (V)"sweeping_slash", R.Get(v_274, (V)1.0d)}, new Results(R.Get(v_274, (V)2.0d)))));
V v_276 = values_275[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_276, (V)"valid"), () => (V)((V)R.Length(R.Get(v_276, (V)"cells")) == (V)3.0d)))));
var values_277 = R.Join(new V[] {}, new Results(R.Table()));
V v_278 = values_277[0];
foreach (var row_279 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_276, (V)"cells"))))))) {
V v_280 = row_279[0];
V v_281 = row_279[1];
var values_282 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_281, (V)"x"), R.Concat((V)",", R.Get(v_281, (V)"y")))));
V v_283 = values_282[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_278, v_283))))));
var values_284 = R.Join(new V[] {}, new Results((V)true));
var obj_285 = v_278;
V key_286 = v_283;
R.Set(obj_285, key_286, values_284[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_281, (V)"x") - (V)2.0d)))).First <= (V)1.0d), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_281, (V)"y") - (V)2.0d)))).First <= (V)1.0d)))));
}
}
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Dash first wall rejects with no state or time change"}, new Results(new V((RuleFunction)(args_287 => {

var values_288 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)4.0d))));
V v_289 = values_288[0];
var values_290 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_289, (V)"dash", (-(V)1.0d)}, new Results((V)0.0d))));
V v_291 = values_290[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_291, (V)"valid"))), () => (V)(R.Get(v_291, (V)"distance") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_291, (V)"cells"), (V)1.0d), (V)"status") == (V)"wall"), () => (V)(R.Get(R.Get(R.Get(v_291, (V)"cells"), (V)2.0d), (V)"status") == (V)"unreachable")))));
var values_292 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_289))));
V v_293 = values_292[0];
var values_294 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_289}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))))));
V v_295 = values_294[0];
V v_296 = values_294[1];
V v_297 = values_294[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(v_295)), () => (V)(v_296 == (V)"wall")), () => (V)((V)R.Length(v_297) == (V)0.0d)))));
R.Call(v_39, R.Join(new V[] {v_293}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_289)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Dash partial path spends 130 once when second tile is wall"}, new Results(new V((RuleFunction)(args_298 => {

var values_299 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)4.0d))));
V v_300 = values_299[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_300}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));
var values_301 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_300, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_302 = values_301[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_302, (V)"valid"), () => (V)(R.Get(v_302, (V)"distance") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_302, (V)"cells"), (V)1.0d), (V)"status") == (V)"open"), () => (V)(R.Get(R.Get(R.Get(v_302, (V)"cells"), (V)2.0d), (V)"status") == (V)"wall")))));
var values_303 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_300}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_304 = values_303[0];
V v_305 = values_303[1];
V v_306 = values_303[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_304, () => (V)(R.Get(v_300, (V)"clock") == (V)230.0d)), () => (V)(R.Get(R.Get(v_300, (V)"last_action"), (V)"cost") == (V)130.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_300, (V)"last_action"), (V)"distance") == (V)1.0d), () => (V)((V)R.Length(v_306) == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_306, (V)2.0d), (V)"kind") == (V)"TileEntered"), () => (V)(R.Get(R.Get(v_306, (V)2.0d), (V)"x") == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_300))).First, (V)"position"), (V)"x") == (V)3.0d), () => (V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_300))).First, (V)"position"), (V)"y") == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(R.Get(v_306, (V)1.0d), (V)"path")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(R.Get(v_306, (V)1.0d), (V)"path"), (V)1.0d), (V)"x") == (V)3.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Dash crosses two open cells and emits one ordered path"}, new Results(new V((RuleFunction)(args_307 => {

var values_308 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)4.0d))));
V v_309 = values_308[0];
var values_310 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_309, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_311 = values_310[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_311, (V)"valid"), () => (V)(R.Get(v_311, (V)"distance") == (V)2.0d)))));
var values_312 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_309}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_313 = values_312[0];
V v_314 = values_312[1];
V v_315 = values_312[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_313, () => (V)(R.Get(v_309, (V)"clock") == (V)130.0d)), () => (V)((V)R.Length(v_315) == (V)3.0d)), () => (V)(R.Get(R.Get(v_315, (V)1.0d), (V)"kind") == (V)"Moved")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_315, (V)2.0d), (V)"kind") == (V)"TileEntered"), () => (V)(R.Get(R.Get(v_315, (V)2.0d), (V)"x") == (V)3.0d)), () => (V)(R.Get(R.Get(v_315, (V)3.0d), (V)"kind") == (V)"TileEntered")), () => (V)(R.Get(R.Get(v_315, (V)3.0d), (V)"x") == (V)4.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_315, (V)1.0d), (V)"time") == (V)0.0d), () => (V)(R.Get(R.Get(v_315, (V)1.0d), (V)"x") == (V)4.0d)), () => (V)(R.Get(R.Get(v_315, (V)1.0d), (V)"y") == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(R.Get(v_315, (V)1.0d), (V)"path"), (V)1.0d), (V)"x") == (V)3.0d), () => (V)(R.Get(R.Get(R.Get(R.Get(v_315, (V)1.0d), (V)"path"), (V)2.0d), (V)"x") == (V)4.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Dash stops after open edge cell when second step is out of bounds"}, new Results(new V((RuleFunction)(args_316 => {

var values_317 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {(V)4.0d}, new Results((V)"core:map/scrolling_room"))));
V v_318 = values_317[0];
var values_319 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_318))).First, (V)"position")));
V v_320 = values_319[0];
var values_321 = R.Join(new V[] {(V)34.0d}, new Results((V)12.0d));
var obj_322 = v_320;
V key_323 = (V)"x";
var obj_324 = v_320;
V key_325 = (V)"y";
R.Set(obj_322, key_323, values_321[0]);
R.Set(obj_324, key_325, values_321[1]);
var values_326 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_318, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_327 = values_326[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_327, (V)"valid"), () => (V)(R.Get(v_327, (V)"distance") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_327, (V)"cells"), (V)1.0d), (V)"status") == (V)"open"), () => (V)(R.Get(R.Get(R.Get(v_327, (V)"cells"), (V)2.0d), (V)"status") == (V)"wall")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_318}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_320, (V)"x") == (V)35.0d), () => (V)(R.Get(v_318, (V)"clock") == (V)130.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Dash accepts all four cardinals with the authored path outcome"}, new Results(new V((RuleFunction)(args_328 => {

var values_329 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d)), new Entry((V)"distance", (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"distance", (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d), new Entry((V)"distance", (V)2.0d))), new Entry((V)4, R.Table(new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d), new Entry((V)"distance", (V)1.0d))))));
V v_330 = values_329[0];
foreach (var row_331 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_330)))))) {
V v_332 = row_331[0];
V v_333 = row_331[1];
var values_334 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {(V)4.0d}, new Results((V)"core:map/scrolling_room"))));
V v_335 = values_334[0];
var values_336 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_335, (V)"dash", R.Get(v_333, (V)"dx")}, new Results(R.Get(v_333, (V)"dy")))));
V v_337 = values_336[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_337, (V)"valid"), () => (V)(R.Get(v_337, (V)"distance") == R.Get(v_333, (V)"distance"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_335}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", R.Get(v_333, (V)"dx")), new Entry((V)"dy", R.Get(v_333, (V)"dy"))))))));
var values_338 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_335))).First, (V)"position")));
V v_339 = values_338[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_339, (V)"x") == (V)((V)3.0d + (V)(R.Get(v_333, (V)"dx") * R.Get(v_333, (V)"distance")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_339, (V)"y") == (V)((V)3.0d + (V)(R.Get(v_333, (V)"dy") * R.Get(v_333, (V)"distance")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_335, (V)"clock") == (V)130.0d), () => (V)(R.Get(R.Get(v_335, (V)"last_action"), (V)"cost") == (V)130.0d)))));
}
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"blocking entities stop move and Dash without bump combat"}, new Results(new V((RuleFunction)(args_340 => {

var values_341 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)5.0d))));
V v_342 = values_341[0];
var values_343 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)2.0d), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_342, (V)"active_floor_id")), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))), new Entry((V)"blocks_movement", (V)true))));
var obj_344 = R.Get(v_342, (V)"entities");
V key_345 = (V)2.0d;
R.Set(obj_344, key_345, values_343[0]);
var values_346 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_347 = v_342;
V key_348 = (V)"next_entity_id";
R.Set(obj_347, key_348, values_346[0]);
var values_349 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_342))));
V v_350 = values_349[0];
var values_351 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_342}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_352 = values_351[0];
V v_353 = values_351[1];
var values_354 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_342}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_355 = values_354[0];
V v_356 = values_354[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(!R.Truth(v_352)), () => (V)(v_353 == (V)"occupied")), () => (V)(!R.Truth(v_355))), () => (V)(v_356 == (V)"occupied")))));
R.Call(v_39, R.Join(new V[] {v_350}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_342)))));
var values_357 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_358 = R.Get(R.Get(R.Get(v_342, (V)"entities"), (V)2.0d), (V)"position");
V key_359 = (V)"x";
R.Set(obj_358, key_359, values_357[0]);
var values_360 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_342, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_361 = values_360[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_361, (V)"valid"), () => (V)(R.Get(v_361, (V)"distance") == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_361, (V)"cells"), (V)2.0d), (V)"status") == (V)"occupied")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_342}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_342))).First, (V)"position"), (V)"x") == (V)3.0d), () => (V)(R.Get(v_342, (V)"clock") == (V)130.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"invalid new intents and preview/cancel spend no time"}, new Results(new V((RuleFunction)(args_362 => {

var values_363 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)5.0d))));
V v_364 = values_363[0];
var values_365 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_364))));
V v_366 = values_365[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_364, (V)"dash", (V)1.0d}, new Results((V)1.0d))).First, (V)"valid"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_364, (V)"sweeping_slash", (V)0.0d}, new Results((V)0.0d))).First, (V)"valid"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_364}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_364}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)0.0d))))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_364}, new Results(R.Table(new Entry((V)"kind", (V)"cancel"))))).First)))));
R.Call(v_39, R.Join(new V[] {v_366}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_364)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"new abilities replay and restore with identical future events"}, new Results(new V((RuleFunction)(args_367 => {

var values_368 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_369 = values_368[0];
V v_370 = V.Nil;
v_370 = new V((RuleFunction)(args_371 => {

var values_372 = R.Join(new V[] {R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)789.0d))).First}, new Results(R.Table()));
V v_373 = values_372[0];
V v_374 = values_372[1];
R.Call(R.Get(v_9, (V)"next_int"), R.Join(new V[] {R.Get(v_373, (V)"rng")}, new Results((V)99.0d)));
foreach (var row_375 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_369)))))) {
V v_376 = row_375[0];
V v_377 = row_375[1];
var values_378 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_373}, new Results(v_377))));
V v_379 = values_378[0];
V v_380 = values_378[1];
V v_381 = values_378[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_379)));
foreach (var row_382 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_381)))))) {
V v_383 = row_382[0];
V v_384 = row_382[1];
var values_385 = R.Join(new V[] {}, new Results(v_384));
var obj_386 = v_374;
V key_387 = (V)((V)R.Length(v_374) + (V)1.0d);
R.Set(obj_386, key_387, values_385[0]);
}
}
return R.Join(new V[] {v_373}, new Results(v_374));
return Results.Empty;
}));
var values_388 = R.Join(new V[] {}, R.Call(v_370, Results.Empty));
V v_389 = values_388[0];
V v_390 = values_388[1];
var values_391 = R.Join(new V[] {}, R.Call(v_370, Results.Empty));
V v_392 = values_391[0];
V v_393 = values_391[1];
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_389))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_392)))));
R.Call(v_39, R.Join(new V[] {v_390}, new Results(v_393)));
var values_394 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_389))))));
V v_395 = values_394[0];
foreach (var row_396 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))))))) {
V v_397 = row_396[0];
V v_398 = row_396[1];
var values_399 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_389}, new Results(v_398))));
V v_400 = values_399[0];
V v_401 = values_399[1];
V v_402 = values_399[2];
var values_403 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_395}, new Results(v_398))));
V v_404 = values_403[0];
V v_405 = values_403[1];
V v_406 = values_403[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_400, () => v_404))));
R.Call(v_39, R.Join(new V[] {v_402}, new Results(v_406)));
}
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_389))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_395)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"camera follows smoothly, clamps, centers small maps, leaves simulation idle"}, new Results(new V((RuleFunction)(args_407 => {

var values_408 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {(V)6.0d}, new Results((V)"core:map/scrolling_room"))));
V v_409 = values_408[0];
var values_410 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_409))));
V v_411 = values_410[0];
var values_412 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)16.0d), new Entry((V)"y", (V)16.0d), new Entry((V)"width", (V)704.0d), new Entry((V)"height", (V)608.0d))));
V v_413 = values_412[0];
var values_414 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {R.Call(R.Get(v_5, (V)"get_map"), R.Join(new V[] {}, new Results(R.Get(v_409, (V)"map_id")))).First, v_413, (V)32.0d, (V)3.0d}, new Results((V)3.0d))));
V v_415 = values_414[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_415, (V)"x") == (V)352.0d), () => (V)(R.Get(v_415, (V)"y") == (V)304.0d)))));
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {v_415, (V)0.06d, (V)25.0d}, new Results((V)20.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_415, (V)"x") > (V)352.0d), () => (V)(R.Get(v_415, (V)"x") < (V)768.0d)), () => (V)(R.Get(v_415, (V)"y") > (V)304.0d)), () => (V)(R.Get(v_415, (V)"y") < (V)496.0d)))));
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {v_415, (V)5.0d, (V)35.0d}, new Results((V)25.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_415, (V)"x") - (V)768.0d)))).First < (V)0.001d), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_415, (V)"y") - (V)496.0d)))).First < (V)0.001d)))));
R.Call(v_39, R.Join(new V[] {v_411}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_409)))));
var values_416 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {R.Call(R.Get(v_5, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/test_room"))).First, v_413, (V)32.0d, (V)2.0d}, new Results((V)2.0d))));
V v_417 = values_416[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_417, (V)"x") == (V)240.0d), () => (V)(R.Get(v_417, (V)"y") == (V)176.0d)))));
var values_418 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"tile_top_left"), R.Join(new V[] {v_417, (V)1.0d}, new Results((V)1.0d))));
V v_419 = values_418[0];
V v_420 = values_418[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_419 == (V)128.0d), () => (V)(v_420 == (V)144.0d)))));
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {v_417, (V)2.0d, (V)14.0d}, new Results((V)10.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_417, (V)"x") == (V)240.0d), () => (V)(R.Get(v_417, (V)"y") == (V)176.0d)))));
var values_421 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {R.Call(R.Get(v_5, (V)"get_map"), R.Join(new V[] {}, new Results(R.Get(v_409, (V)"map_id")))).First, v_413, (V)32.0d, (V)3.0d}, new Results((V)3.0d))));
V v_422 = values_421[0];
var values_423 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {R.Call(R.Get(v_5, (V)"get_map"), R.Join(new V[] {}, new Results(R.Get(v_409, (V)"map_id")))).First, v_413, (V)32.0d, (V)3.0d}, new Results((V)3.0d))));
V v_424 = values_423[0];
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {v_422, (V)0.12d, (V)20.0d}, new Results((V)15.0d)));
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {v_424, (V)0.06d, (V)20.0d}, new Results((V)15.0d)));
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {v_424, (V)0.06d, (V)20.0d}, new Results((V)15.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_422, (V)"x") - R.Get(v_424, (V)"x"))))).First < (V)1e-06d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_422, (V)"y") - R.Get(v_424, (V)"y"))))).First < (V)1e-06d))));
return Results.Empty;
})))));
V v_425 = V.Nil;
v_425 = new V((RuleFunction)(args_426 => {
V v_427 = args_426[0];
V v_428 = args_426[1];
var values_429 = R.Join(new V[] {}, new Results(R.Table()));
V v_430 = values_429[0];
foreach (var row_431 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_427)))))) {
V v_432 = row_431[0];
V v_433 = row_431[1];
if (R.Truth((V)(R.Get(v_433, (V)"kind") == v_428))) {
var values_434 = R.Join(new V[] {}, new Results(v_433));
var obj_435 = v_430;
V key_436 = (V)((V)R.Length(v_430) + (V)1.0d);
R.Set(obj_435, key_436, values_434[0]);
}
}
return R.Join(new V[] {}, new Results(v_430));
return Results.Empty;
}));
R.Call(v_50, R.Join(new V[] {(V)"enemy bump is one basic hit and one recovery without movement"}, new Results(new V((RuleFunction)(args_437 => {

var values_438 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)10.0d))));
V v_439 = values_438[0];
var values_440 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_439, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)1000.0d))));
V v_441 = values_440[0];
var values_442 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_439, (V)"move", (V)1.0d}, new Results((V)0.0d))));
V v_443 = values_442[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_443, (V)"valid"), () => R.Get(v_443, (V)"bump")), () => (V)(R.Get(v_443, (V)"distance") == (V)0.0d)))));
var values_444 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_439}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_445 = values_444[0];
V v_446 = values_444[1];
V v_447 = values_444[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_445, () => (V)(R.Get(v_439, (V)"clock") == (V)100.0d)), () => (V)(R.Get(R.Get(v_439, (V)"last_action"), (V)"kind") == (V)"basic_attack")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_439, (V)"last_action"), (V)"bump"), () => (V)(R.Get(R.Get(v_439, (V)"last_action"), (V)"cost") == (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_439))).First, (V)"position"), (V)"x") == (V)2.0d), () => (V)(R.Get(v_441, (V)"hp") == (V)7.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_447}, new Results((V)"AttackPerformed"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_447}, new Results((V)"DamageTaken"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_447, (V)1.0d), (V)"bump"), () => (V)(R.Get(R.Get(v_447, (V)1.0d), (V)"action_id") == R.Get(R.Get(R.Call(v_425, R.Join(new V[] {v_447}, new Results((V)"DamageTaken"))).First, (V)1.0d), (V)"action_id"))))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"explicit diagonal Sword Strike damages its occupied tile"}, new Results(new V((RuleFunction)(args_448 => {

var values_449 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)10.0d))));
V v_450 = values_449[0];
var values_451 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_450, (V)"core:enemy/ruin_skitter", (V)3.0d, (V)3.0d}, new Results((V)1000.0d))));
V v_452 = values_451[0];
var values_453 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_450}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))));
V v_454 = values_453[0];
V v_455 = values_453[1];
V v_456 = values_453[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_450, (V)"clock") == (V)100.0d), () => (V)(R.Get(v_452, (V)"hp") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_450}, new Results(R.Get(v_452, (V)"id")))).First == V.Nil))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_456, (V)1.0d), (V)"target_x") == (V)3.0d), () => (V)(R.Get(R.Get(v_456, (V)1.0d), (V)"target_y") == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_456}, new Results((V)"DamageTaken"))).First) == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"empty explicit attacks, walls and cancel retain prior semantics with enemies"}, new Results(new V((RuleFunction)(args_457 => {

var values_458 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)11.0d))));
V v_459 = values_458[0];
R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_459, (V)"core:enemy/mossbound_guard", (V)5.0d, (V)2.0d}, new Results((V)1000.0d)));
var values_460 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_459))));
V v_461 = values_460[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_459}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_459}, new Results(R.Table(new Entry((V)"kind", (V)"cancel"))))).First)))));
R.Call(v_39, R.Join(new V[] {v_461}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_459)))));
var values_462 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_459}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (-(V)1.0d)))))));
V v_463 = values_462[0];
V v_464 = values_462[1];
V v_465 = values_462[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_463, () => (V)(R.Get(v_459, (V)"clock") == (V)100.0d)), () => (V)((V)R.Length(v_465) == (V)1.0d)), () => (V)(R.Get(R.Get(v_465, (V)1.0d), (V)"kind") == (V)"AttackPerformed")))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"damage floor, death removal and ordered provenance"}, new Results(new V((RuleFunction)(args_466 => {

var values_467 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)12.0d))));
V v_468 = values_467[0];
var values_469 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_468, (V)"core:enemy/ruin_skitter", (V)3.0d, (V)2.0d}, new Results((V)1000.0d))));
V v_470 = values_469[0];
var values_471 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_472 = v_470;
V key_473 = (V)"hp";
R.Set(obj_472, key_473, values_471[0]);
var values_474 = R.Join(new V[] {}, new Results(R.Get(v_470, (V)"id")));
V v_475 = values_474[0];
var values_476 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_468}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_477 = values_476[0];
V v_478 = values_476[1];
V v_479 = values_476[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_477, () => (V)(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_468}, new Results(v_475))).First == V.Nil)))));
R.Call(v_39, R.Join(new V[] {R.Table(new Entry((V)1, R.Get(R.Get(v_479, (V)1.0d), (V)"kind")), new Entry((V)2, R.Get(R.Get(v_479, (V)2.0d), (V)"kind")), new Entry((V)3, R.Get(R.Get(v_479, (V)3.0d), (V)"kind")), new Entry((V)4, R.Get(R.Get(v_479, (V)4.0d), (V)"kind")), new Entry((V)5, R.Get(R.Get(v_479, (V)5.0d), (V)"kind")))}, new Results(R.Table(new Entry((V)1, (V)"AttackPerformed"), new Entry((V)2, (V)"DamageBatchResolved"), new Entry((V)3, (V)"DamageAttempted"), new Entry((V)4, (V)"DamageTaken"), new Entry((V)5, (V)"Died")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_479, (V)3.0d), (V)"actual_loss") == (V)2.0d), () => (V)(R.Get(R.Get(v_479, (V)3.0d), (V)"hp_after") == (V)0.0d)))));
foreach (var row_480 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_479)))))) {
V v_481 = row_480[0];
V v_482 = row_480[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_482, (V)"source_id") == R.Get(v_468, (V)"player_id")), () => (V)(R.Get(v_482, (V)"action_id") == R.Get(R.Get(v_479, (V)1.0d), (V)"action_id"))))));
}
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"fully blocked damage has no DamageTaken"}, new Results(new V((RuleFunction)(args_483 => {

var values_484 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)13.0d))));
V v_485 = values_484[0];
var values_486 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_485, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)1000.0d))));
V v_487 = values_486[0];
var values_488 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_489 = v_487;
V key_490 = (V)"defense";
R.Set(obj_489, key_490, values_488[0]);
var values_491 = R.Join(new V[] {}, new Results(R.Get(v_487, (V)"hp")));
V v_492 = values_491[0];
var values_493 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_485}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_494 = values_493[0];
V v_495 = values_493[1];
V v_496 = values_493[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_487, (V)"hp") == v_492), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_496}, new Results((V)"DamageAttempted"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_496}, new Results((V)"DamageBlocked"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_496}, new Results((V)"DamageTaken"))).First) == (V)0.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_496}, new Results((V)"Died"))).First) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Sweep commits three target HP outcomes before individual damage records"}, new Results(new V((RuleFunction)(args_497 => {

var values_498 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {(V)14.0d}, new Results((V)"core:map/scrolling_room"))));
V v_499 = values_498[0];
var values_500 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)2.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)3.0d), new Entry((V)2, (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)2.0d))))));
V v_501 = values_500[0];
foreach (var row_502 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_501)))))) {
V v_503 = row_502[0];
V v_504 = row_502[1];
var values_505 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_499, (V)"core:enemy/ruin_skitter", R.Get(v_504, (V)1.0d), R.Get(v_504, (V)2.0d)}, new Results((V)1000.0d))));
V v_506 = values_505[0];
var values_507 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_508 = v_506;
V key_509 = (V)"hp";
R.Set(obj_508, key_509, values_507[0]);
}
var values_510 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_499}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_511 = values_510[0];
V v_512 = values_510[1];
V v_513 = values_510[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_511, () => (V)(R.Get(v_499, (V)"clock") == (V)150.0d)), () => (V)((V)R.Length(R.Get(v_499, (V)"entities")) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_513, (V)1.0d), (V)"kind") == (V)"AttackPerformed"), () => (V)(R.Get(R.Get(v_513, (V)2.0d), (V)"kind") == (V)"DamageBatchResolved")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(R.Get(v_513, (V)2.0d), (V)"outcomes")) == (V)3.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_513}, new Results((V)"Died"))).First) == (V)3.0d)))));
foreach (var row_514 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_513, (V)2.0d), (V)"outcomes"))))))) {
V v_515 = row_514[0];
V v_516 = row_514[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_516, (V)"hp_after") == (V)0.0d), () => (V)(R.Get(v_516, (V)"x") == R.Get(R.Get(v_501, v_515), (V)1.0d))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_513, (V)3.0d), (V)"kind") == (V)"DamageAttempted"))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Dash still rejects first blocker and stops at occupied second tile"}, new Results(new V((RuleFunction)(args_517 => {

var values_518 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)15.0d))));
V v_519 = values_518[0];
R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_519, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)1000.0d)));
var values_520 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_519))));
V v_521 = values_520[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_519}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First)))));
R.Call(v_39, R.Join(new V[] {v_521}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_519)))));
var values_522 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_519}, new Results((V)2.0d))));
V v_523 = values_522[0];
var values_524 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_525 = R.Get(v_523, (V)"position");
V key_526 = (V)"x";
R.Set(obj_525, key_526, values_524[0]);
var values_527 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_519, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_528 = values_527[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_528, (V)"valid"), () => (V)(R.Get(v_528, (V)"distance") == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_528, (V)"cells"), (V)2.0d), (V)"status") == (V)"occupied")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_519}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_519, (V)"clock") == (V)130.0d), () => (V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_519))).First, (V)"position"), (V)"x") == (V)3.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Guard detours around a wall and commits cardinal melee"}, new Results(new V((RuleFunction)(args_529 => {

var values_530 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)16.0d))));
V v_531 = values_530[0];
var values_532 = R.Join(new V[] {(V)2.0d}, new Results((V)3.0d));
var obj_533 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_531))).First, (V)"position");
V key_534 = (V)"x";
var obj_535 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_531))).First, (V)"position");
V key_536 = (V)"y";
R.Set(obj_533, key_534, values_532[0]);
R.Set(obj_535, key_536, values_532[1]);
var values_537 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_531, (V)"core:enemy/mossbound_guard", (V)7.0d}, new Results((V)3.0d))));
V v_538 = values_537[0];
var values_539 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"choose"), R.Join(new V[] {v_531}, new Results(v_538))));
V v_540 = values_539[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_540, (V)"kind") == (V)"move"), () => (V)(R.Get(v_540, (V)"dy") == (-(V)1.0d))))));
var values_541 = R.Join(new V[] {}, new Results((V)false));
V v_542 = values_541[0];
{ double end_545 = R.Num((V)12.0d), step_546 = R.Num((V)1.0d);
for (double i_544 = R.Num((V)1.0d); step_546 > 0 ? i_544 <= end_545 : i_544 >= end_545; i_544 += step_546) {
V v_543 = i_544;
var values_547 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_531}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_548 = values_547[0];
V v_549 = values_547[1];
V v_550 = values_547[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_548)));
foreach (var row_551 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_550)))))) {
V v_552 = row_551[0];
V v_553 = row_551[1];
if (R.Truth(R.And((V)(R.Get(v_553, (V)"kind") == (V)"WindupStarted"), () => (V)(R.Get(v_553, (V)"source_id") == R.Get(v_538, (V)"id"))))) {
var values_554 = R.Join(new V[] {}, new Results((V)true));
v_542 = values_554[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_553, (V)"target_cells")) == (V)1.0d))));
}
}
if (R.Truth(v_542)) {
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_542)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_5, (V)"get_map"), R.Join(new V[] {}, new Results(R.Get(v_531, (V)"map_id")))).First, R.Get(R.Get(v_538, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_538, (V)"position"), (V)"y"))))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Thornspitter respects blocking wall and repositions for lane"}, new Results(new V((RuleFunction)(args_555 => {

var values_556 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_557 = values_556[0];
var values_558 = R.Join(new V[] {(V)3.0d}, new Results((V)3.0d));
var obj_559 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_557))).First, (V)"position");
V key_560 = (V)"x";
var obj_561 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_557))).First, (V)"position");
V key_562 = (V)"y";
R.Set(obj_559, key_560, values_558[0]);
R.Set(obj_561, key_562, values_558[1]);
var values_563 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_557, (V)"core:enemy/thornspitter", (V)7.0d}, new Results((V)3.0d))));
V v_564 = values_563[0];
var values_565 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"choose"), R.Join(new V[] {v_557}, new Results(v_564))));
V v_566 = values_565[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_566, (V)"kind") == (V)"move"))));
var values_567 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_557}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_568 = values_567[0];
V v_569 = values_567[1];
V v_570 = values_567[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_570}, new Results((V)"WindupStarted"))).First) == (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(R.Get(v_564, (V)"position"), (V)"x") != (V)6.0d), () => (V)(R.Get(R.Get(v_564, (V)"position"), (V)"y") != (V)3.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Skitter seeks diagonal flank and uses faster locked windup"}, new Results(new V((RuleFunction)(args_571 => {

var values_572 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)18.0d))));
V v_573 = values_572[0];
var values_574 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_573, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)3.0d))));
V v_575 = values_574[0];
var values_576 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"choose"), R.Join(new V[] {v_573}, new Results(v_575))));
V v_577 = values_576[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_577, (V)"kind") == (V)"windup"), () => (V)(R.Get(v_577, (V)"ability_id") == (V)"core:ability/skitter_stab")))));
var values_578 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_573}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_579 = values_578[0];
V v_580 = values_578[1];
V v_581 = values_578[2];
var values_582 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_581}, new Results((V)"WindupStarted"))));
V v_583 = values_582[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_583) >= (V)1.0d), () => (V)(R.Get(R.Get(v_583, (V)1.0d), (V)"due") == (V)22.0d)))));
var values_584 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_581}, new Results((V)"AttackPerformed"))));
V v_585 = values_584[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_585) >= (V)1.0d), () => (V)(R.Get(R.Get(v_585, (V)1.0d), (V)"time") == (V)22.0d)), () => (V)(R.Get(R.Get(v_585, (V)1.0d), (V)"time") < (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_573))).First, (V)"hp") < R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_573))).First, (V)"max_hp")))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Skitter repositions from cardinal adjacency while Guard attacks"}, new Results(new V((RuleFunction)(args_586 => {

var values_587 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)18.0d))));
V v_588 = values_587[0];
var values_589 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_588, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)2.0d))));
V v_590 = values_589[0];
var values_591 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_588))));
V v_592 = values_591[0];
var values_593 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"choose"), R.Join(new V[] {v_588}, new Results(v_590))));
V v_594 = values_593[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_594, (V)"kind") == (V)"move"), () => (V)(R.Get(v_594, (V)"dx") == (V)0.0d)), () => (V)(R.Get(v_594, (V)"dy") == (V)1.0d)))));
R.Call(v_39, R.Join(new V[] {v_592}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_588)))));
var values_595 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)18.0d))));
V v_596 = values_595[0];
var values_597 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_596, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_598 = values_597[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_15, (V)"choose"), R.Join(new V[] {v_596}, new Results(v_598))).First, (V)"kind") == (V)"windup"))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"spitter target lane stays locked while player moves away"}, new Results(new V((RuleFunction)(args_599 => {

var values_600 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)19.0d))));
V v_601 = values_600[0];
var values_602 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_601, (V)"core:enemy/thornspitter", (V)2.0d}, new Results((V)5.0d))));
V v_603 = values_602[0];
var values_604 = R.Join(new V[] {}, new Results((V)60.0d));
var obj_605 = v_603;
V key_606 = (V)"speed";
R.Set(obj_605, key_606, values_604[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_601}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_601, (V)"clock") == (V)100.0d), () => R.Get(v_603, (V)"pending_attack")), () => (V)(R.Get(R.Get(v_603, (V)"pending_attack"), (V)"due") == (V)150.0d)))));
var values_607 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_603, (V)"pending_attack"), (V)"target_cells")))));
V v_608 = values_607[0];
var values_609 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_601))).First, (V)"hp")));
V v_610 = values_609[0];
var values_611 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_601}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_612 = values_611[0];
V v_613 = values_611[1];
V v_614 = values_611[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_601))).First, (V)"position"), (V)"x") == (V)3.0d), () => (V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_601))).First, (V)"hp") == v_610)))));
var values_615 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_614}, new Results((V)"AttackPerformed"))));
V v_616 = values_615[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_616) == (V)1.0d), () => (V)(R.Get(R.Get(v_616, (V)1.0d), (V)"source_id") == R.Get(v_603, (V)"id"))), () => (V)(R.Get(R.Get(v_616, (V)1.0d), (V)"time") == (V)150.0d)))));
R.Call(v_39, R.Join(new V[] {v_608}, new Results(R.Get(R.Get(v_616, (V)1.0d), (V)"target_cells"))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"committed impact wins timestamp tie; player wins ready enemy tie"}, new Results(new V((RuleFunction)(args_617 => {

var values_618 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)20.0d))));
V v_619 = values_618[0];
var values_620 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_619, (V)"core:enemy/thornspitter", (V)2.0d}, new Results((V)5.0d))));
V v_621 = values_620[0];
var values_622 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_619, (V)"core:enemy/mossbound_guard", (V)7.0d, (V)2.0d}, new Results((V)100.0d))));
V v_623 = values_622[0];
var values_624 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_619}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_625 = values_624[0];
V v_626 = values_624[1];
V v_627 = values_624[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_619, (V)"clock") == (V)100.0d), () => (V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_619))).First, (V)"hp") == (V)22.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_627}, new Results((V)"AttackPerformed"))).First) == (V)1.0d))));
var values_628 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"peek"), R.Join(new V[] {}, new Results(v_619))));
V v_629 = values_628[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_629, (V)"actor_id") == R.Get(v_619, (V)"player_id")), () => (V)(R.Get(v_629, (V)"due") == (V)100.0d)))));
var values_630 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_619}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_631 = values_630[0];
V v_632 = values_630[1];
V v_633 = values_630[2];
var values_634 = R.Join(new V[] {}, new Results((V)false));
V v_635 = values_634[0];
foreach (var row_636 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_633)))))) {
V v_637 = row_636[0];
V v_638 = row_636[1];
if (R.Truth((V)(R.Get(v_638, (V)"source_id") == R.Get(v_623, (V)"id")))) {
var values_639 = R.Join(new V[] {}, new Results((V)true));
v_635 = values_639[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_635, () => (V)(R.Get(v_621, (V)"id") != R.Get(v_623, (V)"id"))))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"equal-time enemy turns keep stable entity sequence"}, new Results(new V((RuleFunction)(args_640 => {

var values_641 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)21.0d))));
V v_642 = values_641[0];
var values_643 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_642, (V)"core:enemy/mossbound_guard", (V)7.0d}, new Results((V)2.0d))));
V v_644 = values_643[0];
var values_645 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_642, (V)"core:enemy/mossbound_guard", (V)9.0d}, new Results((V)2.0d))));
V v_646 = values_645[0];
var values_647 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_642}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_648 = values_647[0];
V v_649 = values_647[1];
V v_650 = values_647[2];
var values_651 = R.Join(new V[] {}, new Results(R.Table()));
V v_652 = values_651[0];
foreach (var row_653 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_650)))))) {
V v_654 = row_653[0];
V v_655 = row_653[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_655, (V)"time") == (V)0.0d), () => (V)(R.Get(v_655, (V)"kind") == (V)"Moved")), () => (V)(R.Get(v_655, (V)"source_id") != R.Get(v_642, (V)"player_id"))))) {
var values_656 = R.Join(new V[] {}, new Results(R.Get(v_655, (V)"source_id")));
var obj_657 = v_652;
V key_658 = (V)((V)R.Length(v_652) + (V)1.0d);
R.Set(obj_657, key_658, values_656[0]);
}
}
R.Call(v_39, R.Join(new V[] {v_652}, new Results(R.Table(new Entry((V)1, R.Get(v_644, (V)"id")), new Entry((V)2, R.Get(v_646, (V)"id"))))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"enemy ranged strike damages an allied blocker before player"}, new Results(new V((RuleFunction)(args_659 => {

var values_660 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)22.0d))));
V v_661 = values_660[0];
var values_662 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_661, (V)"core:enemy/thornspitter", (V)2.0d}, new Results((V)5.0d))));
V v_663 = values_662[0];
var values_664 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_661, (V)"core:enemy/mossbound_guard", (V)2.0d, (V)4.0d}, new Results((V)1000.0d))));
V v_665 = values_664[0];
var values_666 = R.Join(new V[] {R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_661))).First, (V)"hp")}, new Results(R.Get(v_665, (V)"hp")));
V v_667 = values_666[0];
V v_668 = values_666[1];
var values_669 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_661}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_670 = values_669[0];
V v_671 = values_669[1];
V v_672 = values_669[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_665, (V)"hp") == (V)(v_668 - R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(R.Get(v_663, (V)"damage") - R.Get(v_665, (V)"defense"))))).First)), () => (V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_661))).First, (V)"hp") == v_667)))));
var values_673 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_672}, new Results((V)"DamageTaken"))));
V v_674 = values_673[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_674) == (V)1.0d), () => (V)(R.Get(R.Get(v_674, (V)1.0d), (V)"target_id") == R.Get(v_665, (V)"id"))), () => (V)(R.Get(R.Get(v_674, (V)1.0d), (V)"source_id") == R.Get(v_663, (V)"id"))))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"ordinary damage leaves windup active and impact still occurs"}, new Results(new V((RuleFunction)(args_675 => {

var values_676 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)23.0d))));
V v_677 = values_676[0];
var values_678 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_677, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_679 = values_678[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_677}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_679, (V)"pending_attack"), () => (V)(R.Get(R.Get(v_679, (V)"pending_attack"), (V)"due") == (V)114.0d)))));
var values_680 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_677}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_681 = values_680[0];
V v_682 = values_680[1];
V v_683 = values_680[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_679, (V)"hp") == (V)7.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_683}, new Results((V)"WindupCancelled"))).First) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_683}, new Results((V)"AttackPerformed"))).First) >= (V)2.0d), () => (V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_677))).First, (V)"hp") == (V)21.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"stun cancels windup and cannot ghost-hit after restore"}, new Results(new V((RuleFunction)(args_684 => {

var values_685 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)24.0d))));
V v_686 = values_685[0];
var values_687 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_686, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_688 = values_687[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_686}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_689 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"stun"), R.Join(new V[] {v_686, R.Get(v_688, (V)"id"), (V)200.0d, R.Get(v_686, (V)"player_id")}, new Results((V)99.0d))));
V v_690 = values_689[0];
V v_691 = values_689[1];
V v_692 = values_689[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_690, () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_692}, new Results((V)"WindupCancelled"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_688, (V)"pending_attack") == V.Nil))));
var values_693 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_686))))));
V v_694 = values_693[0];
var values_695 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_686))).First, (V)"hp")));
V v_696 = values_695[0];
foreach (var row_697 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_686), new Entry((V)2, v_694)))))))) {
V v_698 = row_697[0];
V v_699 = row_697[1];
var values_700 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_699}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_701 = values_700[0];
V v_702 = values_700[1];
V v_703 = values_700[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_703}, new Results((V)"AttackPerformed"))).First) == (V)0.0d), () => (V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_699))).First, (V)"hp") == v_696)))));
}
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_686))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_694)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"displacement cancels windup and prevents late impact"}, new Results(new V((RuleFunction)(args_704 => {

var values_705 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)25.0d))));
V v_706 = values_705[0];
var values_707 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_706, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_708 = values_707[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_706}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_709 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"displace"), R.Join(new V[] {v_706, R.Get(v_708, (V)"id"), (V)4.0d, (V)2.0d, R.Get(v_706, (V)"player_id")}, new Results((V)100.0d))));
V v_710 = values_709[0];
V v_711 = values_709[1];
V v_712 = values_709[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_710, () => (V)(R.Get(R.Get(v_708, (V)"position"), (V)"x") == (V)4.0d)), () => (V)(R.Get(v_708, (V)"pending_attack") == V.Nil)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_712}, new Results((V)"WindupCancelled"))).First) == (V)1.0d))));
var values_713 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_706))).First, (V)"hp")));
V v_714 = values_713[0];
var values_715 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_706}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_716 = values_715[0];
V v_717 = values_715[1];
V v_718 = values_715[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_718}, new Results((V)"AttackPerformed"))).First) == (V)0.0d), () => (V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_706))).First, (V)"hp") == v_714)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"death cancels pending strike and removes enemy ready work"}, new Results(new V((RuleFunction)(args_719 => {

var values_720 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)26.0d))));
V v_721 = values_720[0];
var values_722 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_721, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_723 = values_722[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_721}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_724 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_725 = v_723;
V key_726 = (V)"hp";
R.Set(obj_725, key_726, values_724[0]);
var values_727 = R.Join(new V[] {}, new Results(R.Get(v_723, (V)"id")));
V v_728 = values_727[0];
var values_729 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_721}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_730 = values_729[0];
V v_731 = values_729[1];
V v_732 = values_729[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_721}, new Results(v_728))).First == V.Nil), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_732}, new Results((V)"Died"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_732}, new Results((V)"WindupCancelled"))).First) == (V)1.0d))));
foreach (var row_733 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_721, (V)"schedule"))))))) {
V v_734 = row_733[0];
V v_735 = row_733[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_735, (V)"actor_id") != v_728))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_721))).First, (V)"hp") == (V)24.0d))));
var values_736 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_721))))));
V v_737 = values_736[0];
var values_738 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_737}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_739 = values_738[0];
V v_740 = values_738[1];
V v_741 = values_738[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_741}, new Results((V)"AttackPerformed"))).First) == (V)0.0d), () => (V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_737))).First, (V)"hp") == (V)24.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"pending windup snapshot restores locked geometry and future ordering"}, new Results(new V((RuleFunction)(args_742 => {

var values_743 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)27.0d))));
V v_744 = values_743[0];
R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_744, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_744}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_7, (V)"pending_strikes"), R.Join(new V[] {}, new Results(v_744))).First) == (V)1.0d))));
foreach (var row_745 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_744, (V)"schedule"))))))) {
V v_746 = row_745[0];
V v_747 = row_745[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(v_747, (V)"actor_id") != (V)2.0d), () => (V)(R.Get(v_747, (V)"kind") != (V)"actor_ready")))));
}
var values_748 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_744))))));
V v_749 = values_748[0];
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"pending_strikes"), R.Join(new V[] {}, new Results(v_744))).First}, R.Call(R.Get(v_7, (V)"pending_strikes"), R.Join(new V[] {}, new Results(v_749)))));
var values_750 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_744}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_751 = values_750[0];
V v_752 = values_750[1];
V v_753 = values_750[2];
var values_754 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_749}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_755 = values_754[0];
V v_756 = values_754[1];
V v_757 = values_754[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_751, () => v_755))));
R.Call(v_39, R.Join(new V[] {v_753}, new Results(v_757)));
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_744))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_749)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"player death ends the run and snapshot stays restorable"}, new Results(new V((RuleFunction)(args_758 => {

var values_759 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)29.0d))));
V v_760 = values_759[0];
var values_761 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_762 = R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_760))).First;
V key_763 = (V)"hp";
R.Set(obj_762, key_763, values_761[0]);
R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_760, (V)"core:enemy/thornspitter", (V)2.0d}, new Results((V)5.0d)));
var values_764 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_760}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_765 = values_764[0];
V v_766 = values_764[1];
V v_767 = values_764[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_765, () => R.Get(v_760, (V)"game_over")), () => (V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_760))).First, (V)"hp") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_767}, new Results((V)"Died"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_760}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))).First)))));
var values_768 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_760))))));
V v_769 = values_768[0];
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_760))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_769)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"demo replay, idle state and presentation playback are deterministic"}, new Results(new V((RuleFunction)(args_770 => {

V v_771 = V.Nil;
v_771 = new V((RuleFunction)(args_772 => {

var values_773 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {(V)28.0d, (V)"core:map/scrolling_room"}, new Results(R.Table(new Entry((V)"demo_enemies", (V)true))))));
V v_774 = values_773[0];
var values_775 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_774))));
V v_776 = values_775[0];
{ double end_779 = R.Num((V)1000.0d), step_780 = R.Num((V)1.0d);
for (double i_778 = R.Num((V)1.0d); step_780 > 0 ? i_778 <= end_779 : i_778 >= end_779; i_778 += step_780) {
V v_777 = i_778;
R.Call(R.Get(v_7, (V)"pending_strikes"), R.Join(new V[] {}, new Results(v_774)));
}
}
R.Call(v_39, R.Join(new V[] {v_776}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_774)))));
var values_781 = R.Join(new V[] {}, new Results(R.Table()));
V v_782 = values_781[0];
{ double end_785 = R.Num((V)3.0d), step_786 = R.Num((V)1.0d);
for (double i_784 = R.Num((V)1.0d); step_786 > 0 ? i_784 <= end_785 : i_784 >= end_785; i_784 += step_786) {
V v_783 = i_784;
var values_787 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_774}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_788 = values_787[0];
V v_789 = values_787[1];
V v_790 = values_787[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_788)));
foreach (var row_791 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_790)))))) {
V v_792 = row_791[0];
V v_793 = row_791[1];
var values_794 = R.Join(new V[] {}, new Results(v_793));
var obj_795 = v_782;
V key_796 = (V)((V)R.Length(v_782) + (V)1.0d);
R.Set(obj_795, key_796, values_794[0]);
}
}
}
return R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_774))).First}, new Results(v_782));
return Results.Empty;
}));
var values_797 = R.Join(new V[] {}, R.Call(v_771, Results.Empty));
V v_798 = values_797[0];
V v_799 = values_797[1];
var values_800 = R.Join(new V[] {}, R.Call(v_771, Results.Empty));
V v_801 = values_800[0];
V v_802 = values_800[1];
R.Call(v_39, R.Join(new V[] {v_798}, new Results(v_801)));
R.Call(v_39, R.Join(new V[] {v_799}, new Results(v_802)));
var values_803 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"new"), Results.Empty));
V v_804 = values_803[0];
R.Call(R.Get(v_17, (V)"add"), R.Join(new V[] {v_804, v_799}, new Results((V)1.0d)));
var values_805 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_798))));
V v_806 = values_805[0];
var values_807 = R.Join(new V[] {}, new Results((V)0.0d));
V v_808 = values_807[0];
while (R.Truth(R.Call(R.Get(v_17, (V)"busy"), R.Join(new V[] {}, new Results(v_804))).First)) {
var values_809 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"current"), R.Join(new V[] {}, new Results(v_804))));
V v_810 = values_809[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_810, (V)"id") >= v_808))));
var values_811 = R.Join(new V[] {}, new Results(R.Get(v_810, (V)"id")));
v_808 = values_811[0];
R.Call(R.Get(v_17, (V)"update"), R.Join(new V[] {v_804}, new Results((V)1.0d)));
}
R.Call(v_39, R.Join(new V[] {v_806}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_798)))));
return Results.Empty;
})))));
V v_812 = V.Nil;
v_812 = new V((RuleFunction)(args_813 => {
V v_814 = args_813[0];
V v_815 = args_813[1];
V v_816 = args_813[2];
V v_817 = args_813[3];
var values_818 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_814, v_815, v_816}, new Results(v_817))));
V v_819 = values_818[0];
R.Call(R.Get(v_11, (V)"remove_for_actor"), R.Join(new V[] {v_814}, new Results(R.Get(v_819, (V)"id"))));
return R.Join(new V[] {}, new Results(v_819));
return Results.Empty;
}));
V v_820 = V.Nil;
v_820 = new V((RuleFunction)(args_821 => {
V v_822 = args_821[0];
V v_823 = args_821[1];
V v_824 = args_821[2];
V v_825 = args_821[3];
R.Call(R.Get(v_7, (V)"set_boon_stacks"), R.Join(new V[] {v_822, R.Get(v_822, (V)"player_id"), R.Concat((V)"core:boon/", v_823), v_824}, new Results(R.Or(v_825, () => (V)1.0d))));
return Results.Empty;
}));
R.Call(v_50, R.Join(new V[] {(V)"boon catalogue, four rarity strengths and invalid references"}, new Results(new V((RuleFunction)(args_826 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_23, (V)"validate"), Results.Empty)));
foreach (var row_827 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"order"))))))) {
V v_828 = row_827[0];
V v_829 = row_827[1];
var values_830 = R.Join(new V[] {}, R.Call(R.Get(v_23, (V)"get"), R.Join(new V[] {}, new Results(v_829))));
V v_831 = values_830[0];
var values_832 = R.Join(new V[] {}, new Results((-(V)1.0d)));
V v_833 = values_832[0];
foreach (var row_834 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"rarities"))))))) {
V v_835 = row_834[0];
V v_836 = row_834[1];
var values_837 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_831, (V)"parameters"), v_836), (V)"power")));
V v_838 = values_837[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_838 > v_833))));
var values_839 = R.Join(new V[] {}, new Results(v_838));
v_833 = values_839[0];
}
}
var values_840 = R.Join(new V[] {}, new Results(R.Table()));
V v_841 = values_840[0];
foreach (var row_842 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"definitions"))))))) {
V v_843 = row_842[0];
V v_844 = row_842[1];
var values_845 = R.Join(new V[] {}, new Results(v_844));
var obj_846 = v_841;
V key_847 = v_843;
R.Set(obj_846, key_847, values_845[0]);
}
var values_848 = R.Join(new V[] {}, new Results(V.Nil));
var obj_849 = v_841;
V key_850 = (V)"core:boon/storm_conductor";
R.Set(obj_849, key_850, values_848[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_23, (V)"validate"), v_841}, new Results(R.Get(v_23, (V)"order")))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"set_boon_stacks"), R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)1.0d))).First, (V)1.0d, (V)"missing:boon", (V)"rare"}, new Results((V)1.0d))).First)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"boon inventory mixed stacks, removal and snapshot"}, new Results(new V((RuleFunction)(args_851 => {

var values_852 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)91.0d))));
V v_853 = values_852[0];
R.Call(v_820, R.Join(new V[] {v_853, (V)"storm_conductor", (V)"common"}, new Results((V)1.0d)));
R.Call(v_820, R.Join(new V[] {v_853, (V)"storm_conductor", (V)"rare"}, new Results((V)2.0d)));
R.Call(v_820, R.Join(new V[] {v_853, (V)"storm_conductor", (V)"legendary"}, new Results((V)1000.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_21, (V)"aggregate"), R.Join(new V[] {(V)"core:boon/storm_conductor", R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_853))).First, (V)"boons"), (V)"core:boon/storm_conductor")}, new Results((V)"chance"))).First == (V)500380000.0d))));
R.Call(v_820, R.Join(new V[] {v_853, (V)"storm_conductor", (V)"rare"}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_853))).First, (V)"boons"), (V)"core:boon/storm_conductor"), (V)"rare") == V.Nil))));
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_853))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_853)))))))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"mixed rarity additive probability, overflow, zero coefficient and seeded rolls"}, new Results(new V((RuleFunction)(args_854 => {

var values_855 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)99.0d))));
V v_856 = values_855[0];
var values_857 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"common", (V)1.0d), new Entry((V)"rare", (V)1.0d), new Entry((V)"legendary", (V)1.0d))));
V v_858 = values_857[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_21, (V)"aggregate"), R.Join(new V[] {(V)"core:boon/storm_conductor", v_858}, new Results((V)"chance"))).First == (V)700000.0d))));
var values_859 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_856, (V)"rng"), (V)"state")));
V v_860 = values_859[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_21, (V)"proc_count"), R.Join(new V[] {v_856, (V)1500000.0d}, new Results((V)0.0d))).First == (V)0.0d), () => (V)(R.Get(R.Get(v_856, (V)"rng"), (V)"state") == v_860)))));
var values_861 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_856))))));
V v_862 = values_861[0];
var values_863 = R.Join(new V[] {(V)false}, new Results((V)false));
V v_864 = values_863[0];
V v_865 = values_863[1];
{ double end_868 = R.Num((V)30.0d), step_869 = R.Num((V)1.0d);
for (double i_867 = R.Num((V)1.0d); step_869 > 0 ? i_867 <= end_868 : i_867 >= end_868; i_867 += step_869) {
V v_866 = i_867;
var values_870 = R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"proc_count"), R.Join(new V[] {v_856, (V)1500000.0d}, new Results((V)1.0d))));
V v_871 = values_870[0];
var values_872 = R.Join(new V[] {}, R.Call(R.Get(v_21, (V)"proc_count"), R.Join(new V[] {v_862, (V)1500000.0d}, new Results((V)1.0d))));
V v_873 = values_872[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_871 == v_873), () => R.Or((V)(v_871 == (V)1.0d), () => (V)(v_871 == (V)2.0d))))));
var values_874 = R.Join(new V[] {R.Or(v_864, () => (V)(v_871 == (V)1.0d))}, new Results(R.Or(v_865, () => (V)(v_871 == (V)2.0d))));
v_864 = values_874[0];
v_865 = values_874[1];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_864, () => v_865))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"all four Storm rarities retain lightning behavior and zero attack coefficient suppresses chance only"}, new Results(new V((RuleFunction)(args_875 => {

var values_876 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"common", (V)50.0d), new Entry((V)"uncommon", (V)17.0d), new Entry((V)"rare", (V)6.0d), new Entry((V)"legendary", (V)2.0d))));
V v_877 = values_876[0];
foreach (var row_878 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"rarities"))))))) {
V v_879 = row_878[0];
V v_880 = row_878[1];
var values_881 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)101.0d))));
V v_882 = values_881[0];
R.Call(v_820, R.Join(new V[] {v_882, (V)"storm_conductor", v_880}, new Results(R.Get(v_877, v_880))));
R.Call(v_812, R.Join(new V[] {v_882, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)2.0d)));
R.Call(v_812, R.Join(new V[] {v_882, (V)"core:enemy/thornspitter", (V)4.0d}, new Results((V)2.0d)));
var values_883 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_882}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_884 = values_883[0];
V v_885 = values_883[1];
V v_886 = values_883[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_886}, new Results((V)"BoonActivated"))).First) >= (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_425, R.Join(new V[] {v_886}, new Results((V)"EffectApplied"))).First, (V)1.0d), (V)"damage_type") == (V)"lightning"))));
}
var values_887 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)102.0d))));
V v_888 = values_887[0];
R.Call(v_820, R.Join(new V[] {v_888, (V)"storm_conductor", (V)"legendary"}, new Results((V)2.0d)));
R.Call(v_820, R.Join(new V[] {v_888, (V)"detonation_bloom"}, new Results((V)"common")));
R.Call(v_812, R.Join(new V[] {v_888, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)2.0d)));
var values_889 = R.Join(new V[] {}, new Results(R.Get(v_5, (V)"ability_proc_coefficient")));
V v_890 = values_889[0];
var values_893 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_891 => {
V v_892 = args_891[0];
if (R.Truth((V)(v_892 == (V)"core:ability/basic_attack"))) {
return R.Join(new V[] {}, new Results((V)0.0d));
}
return R.Join(new V[] {}, R.Call(v_890, R.Join(new V[] {}, new Results(v_892))));
return Results.Empty;
}))));
var obj_894 = v_5;
V key_895 = (V)"ability_proc_coefficient";
R.Set(obj_894, key_895, values_893[0]);
var values_896 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_888}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_897 = values_896[0];
V v_898 = values_896[1];
V v_899 = values_896[2];
var values_900 = R.Join(new V[] {}, new Results(v_890));
var obj_901 = v_5;
V key_902 = (V)"ability_proc_coefficient";
R.Set(obj_901, key_902, values_900[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_897, () => (V)(R.Get(R.Get(v_899, (V)1.0d), (V)"proc_coefficient") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_899}, new Results((V)"EntityKilled"))).First) == (V)1.0d))));
var values_903 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_899}, new Results((V)"BoonActivated"))));
V v_904 = values_903[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_904) == (V)1.0d), () => (V)(R.Get(R.Get(v_904, (V)1.0d), (V)"boon_id") == (V)"core:boon/detonation_bloom")))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"attack contact, empty attack and blocked damage have distinct events"}, new Results(new V((RuleFunction)(args_905 => {

var values_906 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)13.0d))));
V v_907 = values_906[0];
R.Call(v_820, R.Join(new V[] {v_907, (V)"crescent_reach"}, new Results((V)"common")));
var values_908 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_907}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_909 = values_908[0];
V v_910 = values_908[1];
V v_911 = values_908[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_911}, new Results((V)"AttackPerformed"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_911}, new Results((V)"HitConfirmed"))).First) == (V)0.0d)))));
var values_912 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_907, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_913 = values_912[0];
var values_914 = R.Join(new V[] {}, new Results((V)99.0d));
var obj_915 = v_913;
V key_916 = (V)"defense";
R.Set(obj_915, key_916, values_914[0]);
var values_917 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_907}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_918 = values_917[0];
V v_919 = values_917[1];
V v_920 = values_917[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_920}, new Results((V)"HitConfirmed"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_920}, new Results((V)"DamageBlocked"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_920}, new Results((V)"DamageTaken"))).First) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_920, (V)1.0d), (V)"root_action_id") == R.Get(R.Get(v_920, (V)1.0d), (V)"action_id")), () => (V)(R.Get(R.Get(v_920, (V)1.0d), (V)"proc_coefficient") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_920, (V)1.0d), (V)"source_position"), (V)"x") == (V)2.0d))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Storm secondary lightning and Static Footsteps movement use real HP pipeline"}, new Results(new V((RuleFunction)(args_921 => {

var values_922 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_923 = values_922[0];
R.Call(v_820, R.Join(new V[] {v_923, (V)"storm_conductor", (V)"legendary"}, new Results((V)2.0d)));
var values_924 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_923, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)2.0d))));
V v_925 = values_924[0];
var values_926 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_923, (V)"core:enemy/thornspitter", (V)4.0d}, new Results((V)2.0d))));
V v_927 = values_926[0];
var values_928 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_923}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_929 = values_928[0];
V v_930 = values_928[1];
V v_931 = values_928[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_923}, new Results(R.Get(v_925, (V)"id")))).First == V.Nil), () => (V)(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_923}, new Results(R.Get(v_927, (V)"id")))).First == V.Nil)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_931}, new Results((V)"BoonActivated"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_931}, new Results((V)"EffectApplied"))).First) == (V)1.0d))));
var values_932 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_931}, new Results((V)"DamageTaken"))));
V v_933 = values_932[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(v_933) == (V)2.0d), () => (V)(R.Get(R.Get(v_933, (V)2.0d), (V)"damage_type") == (V)"lightning")), () => (V)(R.Get(R.Get(v_933, (V)2.0d), (V)"source_id") == R.Get(v_923, (V)"player_id"))), () => (V)(R.Get(R.Get(v_933, (V)2.0d), (V)"chain_id") == R.Get(R.Get(v_933, (V)1.0d), (V)"chain_id"))))));
var values_934 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)18.0d))));
V v_935 = values_934[0];
R.Call(v_820, R.Join(new V[] {v_935, (V)"static_footsteps", (V)"legendary"}, new Results((V)2.0d)));
var values_936 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_935, (V)"core:enemy/ruin_skitter", (V)4.0d}, new Results((V)2.0d))));
V v_937 = values_936[0];
var values_938 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_935}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_939 = values_938[0];
V v_940 = values_938[1];
V v_941 = values_938[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_941}, new Results((V)"TileEntered"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_941}, new Results((V)"BoonActivated"))).First) >= (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_935}, new Results(R.Get(v_937, (V)"id")))).First == V.Nil))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Thorn Mirror reacts to positive owner damage but not fully blocked damage"}, new Results(new V((RuleFunction)(args_942 => {

V v_943 = V.Nil;
v_943 = new V((RuleFunction)(args_944 => {
V v_945 = args_944[0];
var values_946 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)41.0d))));
V v_947 = values_946[0];
var values_948 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_947))));
V v_949 = values_948[0];
var values_950 = R.Join(new V[] {(V)8.0d, (V)5.0d}, new Results(v_945));
var obj_951 = R.Get(v_949, (V)"position");
V key_952 = (V)"x";
var obj_953 = R.Get(v_949, (V)"position");
V key_954 = (V)"y";
var obj_955 = v_949;
V key_956 = (V)"defense";
R.Set(obj_951, key_952, values_950[0]);
R.Set(obj_953, key_954, values_950[1]);
R.Set(obj_955, key_956, values_950[2]);
R.Call(v_820, R.Join(new V[] {v_947, (V)"thorn_mirror"}, new Results((V)"rare")));
var values_957 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_947, (V)"core:enemy/thornspitter", (V)8.0d}, new Results((V)2.0d))));
V v_958 = values_957[0];
var values_959 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_947}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_960 = values_959[0];
V v_961 = values_959[1];
V v_962 = values_959[2];
return R.Join(new V[] {v_947, v_958}, new Results(v_962));
return Results.Empty;
}));
var values_963 = R.Join(new V[] {}, R.Call(v_943, R.Join(new V[] {}, new Results((V)0.0d))));
V v_964 = values_963[0];
V v_965 = values_963[1];
V v_966 = values_963[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_964))).First, (V)"hp") == (V)22.0d), () => (V)(R.Get(v_965, (V)"hp") == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_966}, new Results((V)"BoonActivated"))).First) == (V)1.0d))));
var values_967 = R.Join(new V[] {}, R.Call(v_943, R.Join(new V[] {}, new Results((V)99.0d))));
V v_968 = values_967[0];
V v_969 = values_967[1];
V v_970 = values_967[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_968))).First, (V)"hp") == (V)24.0d), () => (V)(R.Get(v_969, (V)"hp") == (V)6.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_970}, new Results((V)"DamageTaken"))).First) == (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_970}, new Results((V)"BoonActivated"))).First) == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Resonant Wounds correlates adjacent event-time targets once per pair and chain"}, new Results(new V((RuleFunction)(args_971 => {

V v_972 = V.Nil;
v_972 = new V((RuleFunction)(args_973 => {
V v_974 = args_973[0];
var values_975 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)57.0d))));
V v_976 = values_975[0];
var values_977 = R.Join(new V[] {(V)5.0d}, new Results((V)5.0d));
var obj_978 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_976))).First, (V)"position");
V key_979 = (V)"x";
var obj_980 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_976))).First, (V)"position");
V key_981 = (V)"y";
R.Set(obj_978, key_979, values_977[0]);
R.Set(obj_980, key_981, values_977[1]);
R.Call(v_820, R.Join(new V[] {v_976, (V)"resonant_wounds"}, new Results((V)"common")));
foreach (var row_982 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_974)))))) {
V v_983 = row_982[0];
V v_984 = row_982[1];
R.Call(v_812, R.Join(new V[] {v_976, (V)"core:enemy/mossbound_guard", R.Get(v_984, (V)1.0d)}, new Results(R.Get(v_984, (V)2.0d))));
}
var values_985 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_976}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_986 = values_985[0];
V v_987 = values_985[1];
V v_988 = values_985[2];
return R.Join(new V[] {v_976}, new Results(v_988));
return Results.Empty;
}));
var values_989 = R.Join(new V[] {}, R.Call(v_972, R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)5.0d), new Entry((V)2, (V)4.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)6.0d), new Entry((V)2, (V)4.0d))))))));
V v_990 = values_989[0];
V v_991 = values_989[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_991}, new Results((V)"BoonActivated"))).First) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_991}, new Results((V)"EffectApplied"))).First) == (V)2.0d))));
var values_992 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_425, R.Join(new V[] {v_991}, new Results((V)"DamageBatchResolved"))).First, (V)1.0d)));
V v_993 = values_992[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(R.Get(v_993, (V)"outcomes")) == (V)3.0d), () => (V)(R.Get(R.Get(R.Get(v_993, (V)"outcomes"), (V)1.0d), (V)"hp_after") == (V)8.0d)), () => (V)(R.Get(R.Get(R.Get(v_993, (V)"outcomes"), (V)2.0d), (V)"hp_after") == (V)8.0d)), () => (V)(R.Get(R.Get(R.Get(v_993, (V)"outcomes"), (V)3.0d), (V)"hp_after") == (V)8.0d)))));
var values_994 = R.Join(new V[] {(V)0.0d}, new Results(V.Nil));
V v_995 = values_994[0];
V v_996 = values_994[1];
foreach (var row_997 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_991)))))) {
V v_998 = row_997[0];
V v_999 = row_997[1];
if (R.Truth(R.And((V)(R.Get(v_999, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_999, (V)"damage_type") == (V)"physical")))) {
var values_1000 = R.Join(new V[] {}, new Results(v_998));
v_995 = values_1000[0];
} else if (R.Truth(R.And((V)(R.Get(v_999, (V)"kind") == (V)"BoonActivated"), () => (V)(!R.Truth(v_996))))) {
var values_1001 = R.Join(new V[] {}, new Results(v_998));
v_996 = values_1001[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_996, () => (V)(v_995 < v_996)))));
var values_1002 = R.Join(new V[] {}, R.Call(v_972, R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)6.0d), new Entry((V)2, (V)4.0d))))))));
V v_1003 = values_1002[0];
V v_1004 = values_1002[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1004}, new Results((V)"BoonActivated"))).First) == (V)0.0d))));
var values_1005 = R.Join(new V[] {}, R.Call(v_972, R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)4.0d))))))));
V v_1006 = values_1005[0];
V v_1007 = values_1005[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1007}, new Results((V)"BoonActivated"))).First) == (V)0.0d))));
var values_1008 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1006}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_1009 = values_1008[0];
V v_1010 = values_1008[1];
V v_1011 = values_1008[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1011}, new Results((V)"BoonActivated"))).First) == (V)0.0d))));
var values_1012 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)58.0d))));
V v_1013 = values_1012[0];
var values_1014 = R.Join(new V[] {(V)5.0d}, new Results((V)5.0d));
var obj_1015 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1013))).First, (V)"position");
V key_1016 = (V)"x";
var obj_1017 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1013))).First, (V)"position");
V key_1018 = (V)"y";
R.Set(obj_1015, key_1016, values_1014[0]);
R.Set(obj_1017, key_1018, values_1014[1]);
R.Call(v_820, R.Join(new V[] {v_1013, (V)"resonant_wounds"}, new Results((V)"common")));
R.Call(v_812, R.Join(new V[] {v_1013, (V)"core:enemy/mossbound_guard", (V)5.0d}, new Results((V)4.0d)));
R.Call(v_812, R.Join(new V[] {v_1013, (V)"core:enemy/mossbound_guard", (V)6.0d}, new Results((V)4.0d)));
var values_1019 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1013}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_1020 = values_1019[0];
V v_1021 = values_1019[1];
V v_1022 = values_1019[2];
var values_1023 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1013}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_1024 = values_1023[0];
V v_1025 = values_1023[1];
V v_1026 = values_1023[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1022}, new Results((V)"DamageTaken"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1026}, new Results((V)"DamageTaken"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1022}, new Results((V)"BoonActivated"))).First) == (V)0.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1026}, new Results((V)"BoonActivated"))).First) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Resonant Wounds correlates distinct sibling lightning branches"}, new Results(new V((RuleFunction)(args_1027 => {

var values_1028 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {(V)59.0d}, new Results((V)"core:map/scrolling_room"))));
V v_1029 = values_1028[0];
var values_1030 = R.Join(new V[] {(V)5.0d}, new Results((V)5.0d));
var obj_1031 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1029))).First, (V)"position");
V key_1032 = (V)"x";
var obj_1033 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1029))).First, (V)"position");
V key_1034 = (V)"y";
R.Set(obj_1031, key_1032, values_1030[0]);
R.Set(obj_1033, key_1034, values_1030[1]);
R.Call(v_820, R.Join(new V[] {v_1029, (V)"storm_conductor", (V)"legendary"}, new Results((V)4.0d)));
R.Call(v_820, R.Join(new V[] {v_1029, (V)"resonant_wounds"}, new Results((V)"common")));
R.Call(v_812, R.Join(new V[] {v_1029, (V)"core:enemy/ruin_skitter", (V)6.0d}, new Results((V)5.0d)));
R.Call(v_812, R.Join(new V[] {v_1029, (V)"core:enemy/ruin_skitter", (V)8.0d}, new Results((V)5.0d)));
R.Call(v_812, R.Join(new V[] {v_1029, (V)"core:enemy/ruin_skitter", (V)9.0d}, new Results((V)5.0d)));
var values_1035 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1029}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1036 = values_1035[0];
V v_1037 = values_1035[1];
V v_1038 = values_1035[2];
var values_1039 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_1040 = values_1039[0];
V v_1041 = values_1039[1];
foreach (var row_1042 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_1038}, new Results((V)"BoonActivated")))))))) {
V v_1043 = row_1042[0];
V v_1044 = row_1042[1];
if (R.Truth((V)(R.Get(v_1044, (V)"boon_id") == (V)"core:boon/storm_conductor"))) {
var values_1045 = R.Join(new V[] {}, new Results((V)(v_1040 + (V)1.0d)));
v_1040 = values_1045[0];
}
if (R.Truth((V)(R.Get(v_1044, (V)"boon_id") == (V)"core:boon/resonant_wounds"))) {
var values_1046 = R.Join(new V[] {}, new Results((V)(v_1041 + (V)1.0d)));
v_1041 = values_1046[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_1040 == (V)2.0d), () => (V)(v_1041 == (V)1.0d)))));
var values_1047 = R.Join(new V[] {}, new Results(R.Table()));
V v_1048 = values_1047[0];
foreach (var row_1049 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_1038}, new Results((V)"DamageTaken")))))))) {
V v_1050 = row_1049[0];
V v_1051 = row_1049[1];
if (R.Truth((V)(R.Get(v_1051, (V)"damage_type") == (V)"lightning"))) {
var values_1052 = R.Join(new V[] {}, new Results(v_1051));
var obj_1053 = v_1048;
V key_1054 = (V)((V)R.Length(v_1048) + (V)1.0d);
R.Set(obj_1053, key_1054, values_1052[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_1048) == (V)2.0d), () => (V)(R.Get(R.Get(v_1048, (V)1.0d), (V)"target_id") != R.Get(R.Get(v_1048, (V)2.0d), (V)"target_id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_1048, (V)1.0d), (V)"chain_id") == R.Get(R.Get(v_1048, (V)2.0d), (V)"chain_id")))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Resonant Wounds deduplicates each unordered pair despite repeated later damage"}, new Results(new V((RuleFunction)(args_1055 => {

var values_1056 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)60.0d))));
V v_1057 = values_1056[0];
var values_1058 = R.Join(new V[] {(V)5.0d}, new Results((V)5.0d));
var obj_1059 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1057))).First, (V)"position");
V key_1060 = (V)"x";
var obj_1061 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1057))).First, (V)"position");
V key_1062 = (V)"y";
R.Set(obj_1059, key_1060, values_1058[0]);
R.Set(obj_1061, key_1062, values_1058[1]);
R.Call(v_820, R.Join(new V[] {v_1057, (V)"storm_conductor", (V)"legendary"}, new Results((V)2.0d)));
R.Call(v_820, R.Join(new V[] {v_1057, (V)"resonant_wounds"}, new Results((V)"common")));
foreach (var row_1063 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)5.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)6.0d), new Entry((V)2, (V)4.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)6.0d), new Entry((V)2, (V)5.0d)))))))))) {
V v_1064 = row_1063[0];
V v_1065 = row_1063[1];
var values_1066 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1057, (V)"core:enemy/mossbound_guard", R.Get(v_1065, (V)1.0d)}, new Results(R.Get(v_1065, (V)2.0d)))));
V v_1067 = values_1066[0];
var values_1068 = R.Join(new V[] {(V)50.0d, (V)50.0d}, new Results((V)0.0d));
var obj_1069 = v_1067;
V key_1070 = (V)"hp";
var obj_1071 = v_1067;
V key_1072 = (V)"max_hp";
var obj_1073 = v_1067;
V key_1074 = (V)"defense";
R.Set(obj_1069, key_1070, values_1068[0]);
R.Set(obj_1071, key_1072, values_1068[1]);
R.Set(obj_1073, key_1074, values_1068[2]);
}
var values_1075 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1057}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_1076 = values_1075[0];
V v_1077 = values_1075[1];
V v_1078 = values_1075[2];
var values_1079 = R.Join(new V[] {}, new Results((V)0.0d));
V v_1080 = values_1079[0];
foreach (var row_1081 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_1078}, new Results((V)"BoonActivated")))))))) {
V v_1082 = row_1081[0];
V v_1083 = row_1081[1];
if (R.Truth((V)(R.Get(v_1083, (V)"boon_id") == (V)"core:boon/resonant_wounds"))) {
var values_1084 = R.Join(new V[] {}, new Results((V)(v_1080 + (V)1.0d)));
v_1080 = values_1084[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_1080 == (V)3.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1078}, new Results((V)"DamageTaken"))).First) > (V)3.0d))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Turncoat Spark observes enemy friendly fire and credits player reaction"}, new Results(new V((RuleFunction)(args_1085 => {

var values_1086 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)61.0d))));
V v_1087 = values_1086[0];
var values_1088 = R.Join(new V[] {(V)8.0d}, new Results((V)5.0d));
var obj_1089 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1087))).First, (V)"position");
V key_1090 = (V)"x";
var obj_1091 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1087))).First, (V)"position");
V key_1092 = (V)"y";
R.Set(obj_1089, key_1090, values_1088[0]);
R.Set(obj_1091, key_1092, values_1088[1]);
R.Call(v_820, R.Join(new V[] {v_1087, (V)"turncoat_spark"}, new Results((V)"rare")));
var values_1093 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_1087, (V)"core:enemy/thornspitter", (V)8.0d}, new Results((V)2.0d))));
V v_1094 = values_1093[0];
var values_1095 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1087, (V)"core:enemy/mossbound_guard", (V)8.0d}, new Results((V)4.0d))));
V v_1096 = values_1095[0];
var values_1097 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1087}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_1098 = values_1097[0];
V v_1099 = values_1097[1];
V v_1100 = values_1097[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1096, (V)"hp") == (V)9.0d), () => (V)(R.Get(v_1094, (V)"hp") == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1100}, new Results((V)"BoonActivated"))).First) == (V)1.0d))));
var values_1101 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_1100}, new Results((V)"DamageTaken"))));
V v_1102 = values_1101[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_1102) == (V)2.0d), () => (V)(R.Get(R.Get(v_1102, (V)1.0d), (V)"source_id") == R.Get(v_1094, (V)"id"))), () => (V)(R.Get(R.Get(v_1102, (V)1.0d), (V)"target_id") == R.Get(v_1096, (V)"id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1102, (V)2.0d), (V)"source_id") == R.Get(v_1087, (V)"player_id")), () => (V)(R.Get(R.Get(v_1102, (V)2.0d), (V)"target_id") == R.Get(v_1094, (V)"id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_1102, (V)2.0d), (V)"chain_id") == R.Get(R.Get(v_1102, (V)1.0d), (V)"chain_id")))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Crescent Reach composes geometry and power without mutating base ability"}, new Results(new V((RuleFunction)(args_1103 => {

var values_1104 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)64.0d))));
V v_1105 = values_1104[0];
var values_1106 = R.Join(new V[] {(V)5.0d}, new Results((V)5.0d));
var obj_1107 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1105))).First, (V)"position");
V key_1108 = (V)"x";
var obj_1109 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1105))).First, (V)"position");
V key_1110 = (V)"y";
R.Set(obj_1107, key_1108, values_1106[0]);
R.Set(obj_1109, key_1110, values_1106[1]);
R.Call(v_820, R.Join(new V[] {v_1105, (V)"crescent_reach", (V)"common"}, new Results((V)1.0d)));
R.Call(v_820, R.Join(new V[] {v_1105, (V)"crescent_reach", (V)"rare"}, new Results((V)2.0d)));
var values_1111 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"effective_ability"), R.Join(new V[] {v_1105, (V)"sweeping_slash", (V)1.0d}, new Results((-(V)1.0d)))));
V v_1112 = values_1111[0];
V v_1113 = values_1111[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(v_1112) == (V)4.0d), () => (V)(R.Get(R.Get(v_1112, (V)4.0d), (V)"x") == (V)7.0d)), () => (V)(R.Get(R.Get(v_1112, (V)4.0d), (V)"y") == (V)3.0d)), () => (V)(v_1113 == (V)10.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_19, (V)"cells"), R.Join(new V[] {(V)"sweeping_slash", (V)5.0d, (V)5.0d, (V)1.0d}, new Results((-(V)1.0d)))).First) == (V)3.0d))));
var values_1114 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1105))))));
V v_1115 = values_1114[0];
var values_1116 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"effective_ability"), R.Join(new V[] {v_1115, (V)"sweeping_slash", (V)1.0d}, new Results((-(V)1.0d)))));
V v_1117 = values_1116[0];
V v_1118 = values_1116[1];
R.Call(v_39, R.Join(new V[] {v_1112}, new Results(v_1117)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_1118 == v_1113))));
R.Call(v_820, R.Join(new V[] {v_1115, (V)"crescent_reach", (V)"rare"}, new Results((V)0.0d)));
var values_1119 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"effective_ability"), R.Join(new V[] {v_1115, (V)"sweeping_slash", (V)1.0d}, new Results((-(V)1.0d)))));
V v_1120 = values_1119[0];
V v_1121 = values_1119[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_1120) == (V)4.0d), () => (V)(v_1121 == (V)4.0d)))));
R.Call(v_820, R.Join(new V[] {v_1115, (V)"crescent_reach", (V)"common"}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_7, (V)"effective_ability"), R.Join(new V[] {v_1115, (V)"sweeping_slash", (V)1.0d}, new Results((-(V)1.0d)))).First) == (V)3.0d))));
var values_1122 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)66.0d))));
V v_1123 = values_1122[0];
var values_1124 = R.Join(new V[] {(V)4.0d}, new Results((V)3.0d));
var obj_1125 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1123))).First, (V)"position");
V key_1126 = (V)"x";
var obj_1127 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1123))).First, (V)"position");
V key_1128 = (V)"y";
R.Set(obj_1125, key_1126, values_1124[0]);
R.Set(obj_1127, key_1128, values_1124[1]);
R.Call(v_820, R.Join(new V[] {v_1123, (V)"crescent_reach"}, new Results((V)"common")));
var values_1129 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"effective_ability"), R.Join(new V[] {v_1123, (V)"sweeping_slash", (V)1.0d}, new Results((-(V)1.0d)))));
V v_1130 = values_1129[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_1130) == (V)3.0d))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"two compatible declared ability modifiers compose in stable registry order"}, new Results(new V((RuleFunction)(args_1131 => {

var values_1132 = R.Join(new V[] {}, new Results((V)"core:boon/test_edge"));
V v_1133 = values_1132[0];
var values_1134 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_1133), new Entry((V)"name", (V)"Test Edge"), new Entry((V)"description", (V)"Test-only compatible power modifier"), new Entry((V)"tags", R.Table(new Entry((V)1, (V)"transformation"))), new Entry((V)"triggers", R.Table()), new Entry((V)"scope", R.Table(new Entry((V)"kind", (V)"owner"))), new Entry((V)"stack_policy", (V)"add_power"), new Entry((V)"effects", R.Table()), new Entry((V)"parameters", R.Table(new Entry((V)"common", R.Table(new Entry((V)"chance", (V)0.0d), new Entry((V)"power", (V)2.0d))), new Entry((V)"uncommon", R.Table(new Entry((V)"chance", (V)0.0d), new Entry((V)"power", (V)3.0d))), new Entry((V)"rare", R.Table(new Entry((V)"chance", (V)0.0d), new Entry((V)"power", (V)4.0d))), new Entry((V)"legendary", R.Table(new Entry((V)"chance", (V)0.0d), new Entry((V)"power", (V)5.0d))))), new Entry((V)"modifiers", R.Table(new Entry((V)1, R.Table(new Entry((V)"ability_id", (V)"core:ability/sweeping_slash"), new Entry((V)"kind", (V)"add_damage"))))), new Entry((V)"incompatible", R.Table()))));
V v_1135 = values_1134[0];
var values_1136 = R.Join(new V[] {}, new Results(v_1135));
var obj_1137 = R.Get(v_23, (V)"definitions");
V key_1138 = v_1133;
R.Set(obj_1137, key_1138, values_1136[0]);
var values_1139 = R.Join(new V[] {}, new Results(v_1133));
var obj_1140 = R.Get(v_23, (V)"order");
V key_1141 = (V)((V)R.Length(R.Get(v_23, (V)"order")) + (V)1.0d);
R.Set(obj_1140, key_1141, values_1139[0]);
var values_1148 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1142 => {

var values_1143 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)67.0d))));
V v_1144 = values_1143[0];
R.Call(v_820, R.Join(new V[] {v_1144, (V)"crescent_reach"}, new Results((V)"common")));
R.Call(R.Get(v_7, (V)"set_boon_stacks"), R.Join(new V[] {v_1144, R.Get(v_1144, (V)"player_id"), v_1133, (V)"common"}, new Results((V)1.0d)));
var values_1145 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"effective_ability"), R.Join(new V[] {v_1144, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_1146 = values_1145[0];
V v_1147 = values_1145[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_1146) == (V)4.0d), () => (V)(v_1147 == (V)6.0d)))));
return Results.Empty;
}))))));
V v_1149 = values_1148[0];
V v_1150 = values_1148[1];
var values_1151 = R.Join(new V[] {}, new Results(V.Nil));
var obj_1152 = R.Get(v_23, (V)"definitions");
V key_1153 = v_1133;
R.Set(obj_1152, key_1153, values_1151[0]);
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"order"))));
R.Call(rt.Global("assert"), R.Join(new V[] {v_1149}, new Results(v_1150)));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"declared boon incompatibilities reject a conflicting loadout"}, new Results(new V((RuleFunction)(args_1154 => {

var values_1155 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)65.0d))));
V v_1156 = values_1155[0];
var values_1157 = R.Join(new V[] {}, R.Call(R.Get(v_23, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/crescent_reach"))));
V v_1158 = values_1157[0];
var values_1159 = R.Join(new V[] {}, new Results(R.Get(v_1158, (V)"incompatible")));
V v_1160 = values_1159[0];
var values_1161 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:boon/storm_conductor"))));
var obj_1162 = v_1158;
V key_1163 = (V)"incompatible";
R.Set(obj_1162, key_1163, values_1161[0]);
R.Call(v_820, R.Join(new V[] {v_1156, (V)"storm_conductor"}, new Results((V)"common")));
var values_1164 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_820, v_1156, (V)"crescent_reach"}, new Results((V)"common"))));
V v_1165 = values_1164[0];
var values_1166 = R.Join(new V[] {}, new Results(v_1160));
var obj_1167 = v_1158;
V key_1168 = (V)"incompatible";
R.Set(obj_1167, key_1168, values_1166[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_1165)), () => (V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1156))).First, (V)"boons"), (V)"core:boon/crescent_reach") == V.Nil)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Temporal Echo resolves at 200 before player readiness and restores identically"}, new Results(new V((RuleFunction)(args_1169 => {

var values_1170 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)71.0d))));
V v_1171 = values_1170[0];
R.Call(v_820, R.Join(new V[] {v_1171, (V)"temporal_echo"}, new Results((V)"common")));
var values_1172 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1171, (V)"core:enemy/thornspitter", (V)3.0d}, new Results((V)2.0d))));
V v_1173 = values_1172[0];
var values_1174 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1171}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1175 = values_1174[0];
V v_1176 = values_1174[1];
V v_1177 = values_1174[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1171, (V)"clock") == (V)100.0d), () => (V)(R.Get(v_1173, (V)"hp") == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1177}, new Results((V)"DelayedEffectScheduled"))).First) == (V)1.0d))));
var values_1178 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_425, R.Join(new V[] {v_1177}, new Results((V)"DelayedEffectScheduled"))).First, (V)1.0d)));
V v_1179 = values_1178[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1179, (V)"due") == (V)200.0d), () => (V)((V)R.Length(R.Get(v_1179, (V)"target_cells")) == (V)1.0d)))));
var values_1180 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1171))))));
V v_1181 = values_1180[0];
var values_1182 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1171}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_1183 = values_1182[0];
V v_1184 = values_1182[1];
V v_1185 = values_1182[2];
var values_1186 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1181}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_1187 = values_1186[0];
V v_1188 = values_1186[1];
V v_1189 = values_1186[2];
R.Call(v_39, R.Join(new V[] {v_1185}, new Results(v_1189)));

R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1171))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1181)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1171, (V)"clock") == (V)200.0d), () => (V)(R.Get(v_1173, (V)"hp") == (V)1.0d)))));
var values_1190 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_425, R.Join(new V[] {v_1185}, new Results((V)"DelayedEffectResolved"))).First, (V)1.0d)));
V v_1191 = values_1190[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_1191, () => (V)(R.Get(v_1191, (V)"time") == (V)200.0d)), () => (V)(R.Get(v_1191, (V)"chain_id") == R.Get(v_1179, (V)"chain_id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_1191, (V)"ancestry")) == (V)1.0d), () => (V)(R.Get(R.Get(v_1191, (V)"ancestry"), (V)1.0d) == (V)"1:core:boon/temporal_echo")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_425, R.Join(new V[] {v_1185}, new Results((V)"DamageTaken"))).First, (V)1.0d), (V)"source_id") == R.Get(v_1171, (V)"player_id")))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"locked delayed area hits its current occupant and never advances during idle presentation"}, new Results(new V((RuleFunction)(args_1192 => {

var values_1193 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)72.0d))));
V v_1194 = values_1193[0];
R.Call(v_820, R.Join(new V[] {v_1194, (V)"temporal_echo"}, new Results((V)"rare")));
var values_1195 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1194, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_1196 = values_1195[0];
var values_1197 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1194, (V)"core:enemy/ruin_skitter", (V)4.0d}, new Results((V)2.0d))));
V v_1198 = values_1197[0];
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1194}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1194, (V)"clock") == (V)100.0d), () => (V)((V)R.Length(R.Call(R.Get(v_7, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_1194))).First) == (V)1.0d)))));
var values_1199 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1194))));
V v_1200 = values_1199[0];
{ double end_1203 = R.Num((V)1000.0d), step_1204 = R.Num((V)1.0d);
for (double i_1202 = R.Num((V)1.0d); step_1204 > 0 ? i_1202 <= end_1203 : i_1202 >= end_1203; i_1202 += step_1204) {
V v_1201 = i_1202;
R.Call(R.Get(v_7, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_1194)));

R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1194)));
}
}
R.Call(v_39, R.Join(new V[] {v_1200}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1194)))));
var values_1205 = R.Join(new V[] {(V)4.0d}, new Results((V)3.0d));
var obj_1206 = R.Get(v_1196, (V)"position");
V key_1207 = (V)"x";
var obj_1208 = R.Get(v_1198, (V)"position");
V key_1209 = (V)"x";
R.Set(obj_1206, key_1207, values_1205[0]);
R.Set(obj_1208, key_1209, values_1205[1]);
var values_1210 = R.Join(new V[] {R.Get(v_1196, (V)"hp")}, new Results(R.Get(v_1198, (V)"hp")));
V v_1211 = values_1210[0];
V v_1212 = values_1210[1];
var values_1213 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1194}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_1214 = values_1213[0];
V v_1215 = values_1213[1];
V v_1216 = values_1213[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_1194, (V)"clock") == (V)200.0d), () => (V)(R.Get(v_1196, (V)"hp") == v_1211)), () => (V)(R.Get(v_1198, (V)"hp") < v_1212)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_425, R.Join(new V[] {v_1216}, new Results((V)"DamageTaken"))).First, (V)1.0d), (V)"target_id") == R.Get(v_1198, (V)"id")))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"boon seed replay and plain-data pending state preserve ordered continuation"}, new Results(new V((RuleFunction)(args_1217 => {

V v_1218 = V.Nil;
v_1218 = new V((RuleFunction)(args_1219 => {

var values_1220 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)83.0d))));
V v_1221 = values_1220[0];
var values_1222 = R.Join(new V[] {(V)5.0d}, new Results((V)5.0d));
var obj_1223 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1221))).First, (V)"position");
V key_1224 = (V)"x";
var obj_1225 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1221))).First, (V)"position");
V key_1226 = (V)"y";
R.Set(obj_1223, key_1224, values_1222[0]);
R.Set(obj_1225, key_1226, values_1222[1]);
R.Call(v_820, R.Join(new V[] {v_1221, (V)"storm_conductor", (V)"rare"}, new Results((V)4.0d)));
R.Call(v_820, R.Join(new V[] {v_1221, (V)"temporal_echo", (V)"uncommon"}, new Results((V)1.0d)));
R.Call(v_812, R.Join(new V[] {v_1221, (V)"core:enemy/mossbound_guard", (V)6.0d}, new Results((V)5.0d)));
R.Call(v_812, R.Join(new V[] {v_1221, (V)"core:enemy/ruin_skitter", (V)7.0d}, new Results((V)5.0d)));
var values_1227 = R.Join(new V[] {}, new Results(R.Table()));
V v_1228 = values_1227[0];
foreach (var row_1229 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"wait")))))))))) {
V v_1230 = row_1229[0];
V v_1231 = row_1229[1];
var values_1232 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1221}, new Results(v_1231))));
V v_1233 = values_1232[0];
V v_1234 = values_1232[1];
V v_1235 = values_1232[2];
foreach (var row_1236 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_1235)))))) {
V v_1237 = row_1236[0];
V v_1238 = row_1236[1];
var values_1239 = R.Join(new V[] {}, new Results(v_1238));
var obj_1240 = v_1228;
V key_1241 = (V)((V)R.Length(v_1228) + (V)1.0d);
R.Set(obj_1240, key_1241, values_1239[0]);
}
}
V v_1242 = V.Nil;
v_1242 = new V((RuleFunction)(args_1243 => {
V v_1244 = args_1243[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_1244))).First != (V)"function"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_1244))).First != (V)"userdata")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_1244))).First != (V)"thread")))));
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_1244))).First == (V)"table"))) {
foreach (var row_1245 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1244)))))) {
V v_1246 = row_1245[0];
V v_1247 = row_1245[1];
R.Call(v_1242, R.Join(new V[] {}, new Results(v_1246)));

R.Call(v_1242, R.Join(new V[] {}, new Results(v_1247)));
}
}
return Results.Empty;
}));
R.Call(v_1242, R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1221)))));
return R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1221))).First}, new Results(v_1228));
return Results.Empty;
}));
var values_1248 = R.Join(new V[] {}, R.Call(v_1218, Results.Empty));
V v_1249 = values_1248[0];
V v_1250 = values_1248[1];
var values_1251 = R.Join(new V[] {}, R.Call(v_1218, Results.Empty));
V v_1252 = values_1251[0];
V v_1253 = values_1251[1];
R.Call(v_39, R.Join(new V[] {v_1249}, new Results(v_1252)));

R.Call(v_39, R.Join(new V[] {v_1250}, new Results(v_1253)));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"multi-stage Sword lightning explosion chain retains sibling branches and stops recursion"}, new Results(new V((RuleFunction)(args_1254 => {

var values_1255 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)73.0d))));
V v_1256 = values_1255[0];
var values_1257 = R.Join(new V[] {(V)5.0d}, new Results((V)5.0d));
var obj_1258 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1256))).First, (V)"position");
V key_1259 = (V)"x";
var obj_1260 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1256))).First, (V)"position");
V key_1261 = (V)"y";
R.Set(obj_1258, key_1259, values_1257[0]);
R.Set(obj_1260, key_1261, values_1257[1]);
R.Call(v_820, R.Join(new V[] {v_1256, (V)"storm_conductor", (V)"legendary"}, new Results((V)2.0d)));
R.Call(v_820, R.Join(new V[] {v_1256, (V)"detonation_bloom", (V)"rare"}, new Results((V)1.0d)));
var values_1262 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1256, (V)"core:enemy/mossbound_guard", (V)6.0d}, new Results((V)5.0d))));
V v_1263 = values_1262[0];
var values_1264 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1256, (V)"core:enemy/thornspitter", (V)7.0d}, new Results((V)5.0d))));
V v_1265 = values_1264[0];
var values_1266 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1256, (V)"core:enemy/ruin_skitter", (V)8.0d}, new Results((V)5.0d))));
V v_1267 = values_1266[0];
var values_1268 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_1269 = v_1267;
V key_1270 = (V)"hp";
R.Set(obj_1269, key_1270, values_1268[0]);
var values_1271 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1256}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1272 = values_1271[0];
V v_1273 = values_1271[1];
V v_1274 = values_1271[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_1256}, new Results(R.Get(v_1263, (V)"id")))).First, () => (V)(R.Get(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_1256}, new Results(R.Get(v_1263, (V)"id")))).First, (V)"hp") == (V)5.0d)), () => (V)(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_1256}, new Results(R.Get(v_1265, (V)"id")))).First == V.Nil)), () => (V)(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_1256}, new Results(R.Get(v_1267, (V)"id")))).First == V.Nil)))));
var values_1275 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_1274}, new Results((V)"BoonActivated"))));
V v_1276 = values_1275[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_1276) == (V)2.0d), () => (V)(R.Get(R.Get(v_1276, (V)1.0d), (V)"boon_id") == (V)"core:boon/storm_conductor")), () => (V)(R.Get(R.Get(v_1276, (V)2.0d), (V)"boon_id") == (V)"core:boon/detonation_bloom")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1274}, new Results((V)"EntityKilled"))).First) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1274}, new Results((V)"EffectApplied"))).First) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1274}, new Results((V)"ExperienceGained"))).First) == (V)2.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1274}, new Results((V)"CurrencyDropped"))).First) == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1256, (V)"progression"), (V)"xp_total") == (V)7.0d), () => (V)((V)R.Length(R.Get(v_1256, (V)"drops")) == (V)2.0d)))));
var values_1277 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_1274}, new Results((V)"Died"))));
V v_1278 = values_1277[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1278, (V)1.0d), (V)"target_id") == R.Get(v_1265, (V)"id")), () => (V)(R.Get(R.Get(v_1278, (V)1.0d), (V)"damage_type") == (V)"lightning")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1278, (V)2.0d), (V)"target_id") == R.Get(v_1267, (V)"id")), () => (V)(R.Get(R.Get(v_1278, (V)2.0d), (V)"damage_type") == (V)"explosion")))));
var values_1279 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1274, (V)1.0d), (V)"chain_id")));
V v_1280 = values_1279[0];
foreach (var row_1281 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_1274)))))) {
V v_1282 = row_1281[0];
V v_1283 = row_1281[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1283, (V)"chain_id") == v_1280))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_1274) < (V)100.0d))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"dozens of enemies and high proc activity finish without recursive overflow"}, new Results(new V((RuleFunction)(args_1284 => {

var values_1285 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {(V)79.0d}, new Results((V)"core:map/scrolling_room"))));
V v_1286 = values_1285[0];
var values_1287 = R.Join(new V[] {(V)17.0d}, new Results((V)16.0d));
var obj_1288 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1286))).First, (V)"position");
V key_1289 = (V)"x";
var obj_1290 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1286))).First, (V)"position");
V key_1291 = (V)"y";
R.Set(obj_1288, key_1289, values_1287[0]);
R.Set(obj_1290, key_1291, values_1287[1]);
R.Call(v_820, R.Join(new V[] {v_1286, (V)"storm_conductor", (V)"legendary"}, new Results((V)4.0d)));
R.Call(v_820, R.Join(new V[] {v_1286, (V)"detonation_bloom", (V)"legendary"}, new Results((V)2.0d)));
R.Call(v_820, R.Join(new V[] {v_1286, (V)"resonant_wounds", (V)"rare"}, new Results((V)2.0d)));
var values_1292 = R.Join(new V[] {}, new Results((V)0.0d));
V v_1293 = values_1292[0];
{ double end_1296 = R.Num((V)19.0d), step_1297 = R.Num((V)1.0d);
for (double i_1295 = R.Num((V)14.0d); step_1297 > 0 ? i_1295 <= end_1296 : i_1295 >= end_1296; i_1295 += step_1297) {
V v_1294 = i_1295;
{ double end_1300 = R.Num((V)23.0d), step_1301 = R.Num((V)1.0d);
for (double i_1299 = R.Num((V)18.0d); step_1301 > 0 ? i_1299 <= end_1300 : i_1299 >= end_1300; i_1299 += step_1301) {
V v_1298 = i_1299;
if (R.Truth(R.Call(R.Get(v_5, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_5, (V)"get_map"), R.Join(new V[] {}, new Results(R.Get(v_1286, (V)"map_id")))).First, v_1298}, new Results(v_1294))).First)) {
R.Call(v_812, R.Join(new V[] {v_1286, (V)"core:enemy/ruin_skitter", v_1298}, new Results(v_1294)));
var values_1302 = R.Join(new V[] {}, new Results((V)(v_1293 + (V)1.0d)));
v_1293 = values_1302[0];
}
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_1293 >= (V)30.0d))));
var values_1303 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1286}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1304 = values_1303[0];
V v_1305 = values_1303[1];
V v_1306 = values_1303[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1306}, new Results((V)"BoonActivated"))).First) >= (V)4.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1306}, new Results((V)"DamageTaken"))).First) >= (V)5.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_1306) < (V)10000.0d))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"boon-free kill emits canonical contact, death, XP and one drop"}, new Results(new V((RuleFunction)(args_1307 => {

var values_1308 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)101.0d))));
V v_1309 = values_1308[0];
var values_1310 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1309, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)2.0d))));
V v_1311 = values_1310[0];
var values_1312 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1309}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1313 = values_1312[0];
V v_1314 = values_1312[1];
V v_1315 = values_1312[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1315}, new Results((V)"HitConfirmed"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1315}, new Results((V)"EntityKilled"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1315}, new Results((V)"ExperienceGained"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1315}, new Results((V)"CurrencyDropped"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1309, (V)"progression"), (V)"xp_total") == (V)3.0d), () => (V)((V)R.Length(R.Get(v_1309, (V)"drops")) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_1309, (V)"drops"), (V)1.0d), (V)"killed_entity_id") == R.Get(v_1311, (V)"id")), () => (V)(R.Get(R.Get(R.Get(v_1309, (V)"drops"), (V)1.0d), (V)"amount") == (V)2.0d)))));
var values_1316 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_425, R.Join(new V[] {v_1315}, new Results((V)"EntityKilled"))).First, (V)1.0d)));
V v_1317 = values_1316[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1317, (V)"source_id") == R.Get(v_1309, (V)"player_id")), () => (V)(R.Get(v_1317, (V)"target_id") == R.Get(v_1311, (V)"id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_1317, (V)"x") == (V)3.0d), () => (V)(R.Get(v_1317, (V)"y") == (V)2.0d)), () => (V)(R.Get(v_1317, (V)"root_action_id") == R.Get(R.Get(v_1315, (V)1.0d), (V)"action_id"))))));
var values_1318 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1309}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1319 = values_1318[0];
V v_1320 = values_1318[1];
V v_1321 = values_1318[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1321}, new Results((V)"AttackPerformed"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1321}, new Results((V)"HitConfirmed"))).First) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1309, (V)"progression"), (V)"xp_total") == (V)3.0d), () => (V)((V)R.Length(R.Get(v_1309, (V)"drops")) == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Sweep multi-kills finish HP batch before reward events and award once each"}, new Results(new V((RuleFunction)(args_1322 => {

var values_1323 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)102.0d))));
V v_1324 = values_1323[0];
var values_1325 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1324, (V)"core:enemy/ruin_skitter", (V)2.0d}, new Results((V)3.0d))));
V v_1326 = values_1325[0];
var values_1327 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1324, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)3.0d))));
V v_1328 = values_1327[0];
var values_1329 = R.Join(new V[] {(V)2.0d}, new Results((V)2.0d));
var obj_1330 = v_1326;
V key_1331 = (V)"hp";
var obj_1332 = v_1328;
V key_1333 = (V)"hp";
R.Set(obj_1330, key_1331, values_1329[0]);
R.Set(obj_1332, key_1333, values_1329[1]);
var values_1334 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1324}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))))));
V v_1335 = values_1334[0];
V v_1336 = values_1334[1];
V v_1337 = values_1334[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1337}, new Results((V)"DamageBatchResolved"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1337}, new Results((V)"EntityKilled"))).First) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1337}, new Results((V)"ExperienceGained"))).First) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1337}, new Results((V)"CurrencyDropped"))).First) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1324, (V)"progression"), (V)"xp_total") == (V)6.0d), () => (V)((V)R.Length(R.Get(v_1324, (V)"drops")) == (V)2.0d)))));
var values_1338 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_425, R.Join(new V[] {v_1337}, new Results((V)"DamageBatchResolved"))).First, (V)1.0d)));
V v_1339 = values_1338[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_1339, (V)"outcomes"), (V)1.0d), (V)"hp_after") == (V)0.0d), () => (V)(R.Get(R.Get(R.Get(v_1339, (V)"outcomes"), (V)2.0d), (V)"hp_after") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1339, (V)"id") < R.Get(R.Get(R.Call(v_425, R.Join(new V[] {v_1337}, new Results((V)"ExperienceGained"))).First, (V)1.0d), (V)"id")))));
var values_1340 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_1337}, new Results((V)"EntityKilled"))));
V v_1341 = values_1340[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_1341, (V)1.0d), (V)"target_id") != R.Get(R.Get(v_1341, (V)2.0d), (V)"target_id")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_1324, (V)"rewarded_kills"), R.Get(v_1326, (V)"id")), () => R.Get(R.Get(v_1324, (V)"rewarded_kills"), R.Get(v_1328, (V)"id"))))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"player-owned immediate and delayed boon kills receive XP; enemy friendly fire does not"}, new Results(new V((RuleFunction)(args_1342 => {

var values_1343 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)103.0d))));
V v_1344 = values_1343[0];
R.Call(v_820, R.Join(new V[] {v_1344, (V)"storm_conductor", (V)"legendary"}, new Results((V)2.0d)));
var values_1345 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1344, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_1346 = values_1345[0];
var values_1347 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1344, (V)"core:enemy/ruin_skitter", (V)4.0d}, new Results((V)2.0d))));
V v_1348 = values_1347[0];
var values_1349 = R.Join(new V[] {(V)10.0d}, new Results((V)1.0d));
var obj_1350 = v_1346;
V key_1351 = (V)"hp";
var obj_1352 = v_1348;
V key_1353 = (V)"hp";
R.Set(obj_1350, key_1351, values_1349[0]);
R.Set(obj_1352, key_1353, values_1349[1]);
var values_1354 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1344}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1355 = values_1354[0];
V v_1356 = values_1354[1];
V v_1357 = values_1354[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1357}, new Results((V)"EntityKilled"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_425, R.Join(new V[] {v_1357}, new Results((V)"EntityKilled"))).First, (V)1.0d), (V)"damage_type") == (V)"lightning"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1344, (V)"progression"), (V)"xp_total") == (V)3.0d), () => (V)((V)R.Length(R.Get(v_1344, (V)"drops")) == (V)1.0d)))));
var values_1358 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)104.0d))));
V v_1359 = values_1358[0];
R.Call(v_820, R.Join(new V[] {v_1359, (V)"temporal_echo"}, new Results((V)"common")));
var values_1360 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1359, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)2.0d))));
V v_1361 = values_1360[0];
var values_1362 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_1363 = v_1361;
V key_1364 = (V)"hp";
R.Set(obj_1363, key_1364, values_1362[0]);
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1359}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1359, (V)"progression"), (V)"xp_total") == (V)0.0d), () => (V)(R.Get(v_1361, (V)"hp") == (V)1.0d)))));
var values_1365 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1359}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_1366 = values_1365[0];
V v_1367 = values_1365[1];
V v_1368 = values_1365[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1368}, new Results((V)"EntityKilled"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_425, R.Join(new V[] {v_1368}, new Results((V)"EntityKilled"))).First, (V)1.0d), (V)"time") == (V)200.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_1359, (V)"progression"), (V)"xp_total") == (V)3.0d))));
var values_1369 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)105.0d))));
V v_1370 = values_1369[0];
var values_1371 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_1370, (V)"core:enemy/thornspitter", (V)2.0d}, new Results((V)5.0d))));
V v_1372 = values_1371[0];
var values_1373 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1370, (V)"core:enemy/ruin_skitter", (V)2.0d}, new Results((V)4.0d))));
V v_1374 = values_1373[0];
var values_1375 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_1376 = v_1374;
V key_1377 = (V)"hp";
R.Set(obj_1376, key_1377, values_1375[0]);
var values_1378 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1370}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_1379 = values_1378[0];
V v_1380 = values_1378[1];
V v_1381 = values_1378[2];
var values_1382 = R.Join(new V[] {}, R.Call(v_425, R.Join(new V[] {v_1381}, new Results((V)"EntityKilled"))));
V v_1383 = values_1382[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_1383) == (V)1.0d), () => (V)(R.Get(R.Get(v_1383, (V)1.0d), (V)"source_id") == R.Get(v_1372, (V)"id"))), () => (V)(R.Get(R.Get(v_1383, (V)1.0d), (V)"target_id") == R.Get(v_1374, (V)"id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1370, (V)"progression"), (V)"xp_total") == (V)0.0d), () => (V)((V)R.Length(R.Get(v_1370, (V)"drops")) == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"automatic level overflow uses seeded stat picks and no full heal"}, new Results(new V((RuleFunction)(args_1384 => {

var values_1385 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)106.0d))));
V v_1386 = values_1385[0];
var values_1387 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1386))))));
V v_1388 = values_1387[0];
var values_1389 = R.Join(new V[] {(V)10.0d}, new Results((V)10.0d));
var obj_1390 = R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1386))).First;
V key_1391 = (V)"hp";
var obj_1392 = R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1388))).First;
V key_1393 = (V)"hp";
R.Set(obj_1390, key_1391, values_1389[0]);
R.Set(obj_1392, key_1393, values_1389[1]);
V v_1394 = V.Nil;
v_1394 = new V((RuleFunction)(args_1395 => {
V v_1396 = args_1395[0];
var values_1397 = R.Join(new V[] {}, new Results(R.Table()));
V v_1398 = values_1397[0];
R.Call(R.Get(v_25, (V)"gain_xp"), R.Join(new V[] {v_1396, (V)35.0d, R.Table(new Entry((V)"target_id", (V)99.0d), new Entry((V)"target_definition_id", (V)"test"), new Entry((V)"floor_id", R.Get(v_1396, (V)"active_floor_id")), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))}, new Results(new V((RuleFunction)(args_1399 => {
V v_1400 = args_1399[0];
V v_1401 = args_1399[1];
var values_1402 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_1400), new Entry((V)"fields", v_1401))));
var obj_1403 = v_1398;
V key_1404 = (V)((V)R.Length(v_1398) + (V)1.0d);
R.Set(obj_1403, key_1404, values_1402[0]);
return Results.Empty;
})))));
return R.Join(new V[] {}, new Results(v_1398));
return Results.Empty;
}));
var values_1405 = R.Join(new V[] {R.Call(v_1394, R.Join(new V[] {}, new Results(v_1386))).First}, R.Call(v_1394, R.Join(new V[] {}, new Results(v_1388))));
V v_1406 = values_1405[0];
V v_1407 = values_1405[1];
R.Call(v_39, R.Join(new V[] {v_1406}, new Results(v_1407)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1386, (V)"progression"), (V)"level") == (V)4.0d), () => (V)(R.Get(R.Get(v_1386, (V)"progression"), (V)"xp_total") == (V)35.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"next_level_xp"), R.Join(new V[] {}, new Results(v_1386))).First == (V)50.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_1406) == (V)7.0d), () => (V)(R.Get(R.Get(v_1406, (V)1.0d), (V)"kind") == (V)"ExperienceGained")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1386))).First, (V)"hp") < R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1386))).First, (V)"max_hp")))));
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1386))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1388)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"all six growth stats affect real combat values and speed stays positive"}, new Results(new V((RuleFunction)(args_1408 => {

var values_1409 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"get_progression"), Results.Empty));
V v_1410 = values_1409[0];
var values_1411 = R.Join(new V[] {}, new Results(R.Table()));
V v_1412 = values_1411[0];
foreach (var row_1413 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_1410, (V)"stat_weights"))))))) {
V v_1414 = row_1413[0];
V v_1415 = row_1413[1];
var values_1416 = R.Join(new V[] {}, new Results(v_1415));
var obj_1417 = v_1412;
V key_1418 = v_1414;
R.Set(obj_1417, key_1418, values_1416[0]);
}
var values_1456 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1419 => {

foreach (var row_1420 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1410, (V)"stat_order"))))))) {
V v_1421 = row_1420[0];
V v_1422 = row_1420[1];
var values_1423 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)107.0d))));
V v_1424 = values_1423[0];
var values_1425 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1424))));
V v_1426 = values_1425[0];
var values_1427 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_1428 = v_1426;
V key_1429 = (V)"hp";
R.Set(obj_1428, key_1429, values_1427[0]);
foreach (var row_1430 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_1410, (V)"stat_weights"))))))) {
V v_1431 = row_1430[0];
var values_1432 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_1431 == v_1422), () => (V)1000000.0d), () => (V)1.0d)));
var obj_1433 = R.Get(v_1410, (V)"stat_weights");
V key_1434 = v_1431;
R.Set(obj_1433, key_1434, values_1432[0]);
}
var values_1435 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_1426, v_1422), () => R.Get(v_1426, R.Concat(v_1422, (V)"_ppm")))));
V v_1436 = values_1435[0];
var values_1437 = R.Join(new V[] {}, new Results(R.Table()));
V v_1438 = values_1437[0];
R.Call(R.Get(v_25, (V)"gain_xp"), R.Join(new V[] {v_1424, (V)5.0d, R.Table(new Entry((V)"target_id", (V)9.0d), new Entry((V)"target_definition_id", (V)"test"), new Entry((V)"floor_id", R.Get(v_1424, (V)"active_floor_id")), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))}, new Results(new V((RuleFunction)(args_1439 => {
V v_1440 = args_1439[0];
V v_1441 = args_1439[1];
var values_1442 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_1440), new Entry((V)"fields", v_1441))));
var obj_1443 = v_1438;
V key_1444 = (V)((V)R.Length(v_1438) + (V)1.0d);
R.Set(obj_1443, key_1444, values_1442[0]);
return Results.Empty;
})))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1438, (V)3.0d), (V)"kind") == (V)"StatIncreased"), () => (V)(R.Get(R.Get(R.Get(v_1438, (V)3.0d), (V)"fields"), (V)"stat") == v_1422)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"validate_all"), Results.Empty)));
if (R.Truth((V)(v_1422 == (V)"max_health"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1426, (V)"max_hp") == (V)28.0d), () => (V)(R.Get(v_1426, (V)"hp") == (V)14.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_1438, (V)3.0d), (V)"fields"), (V)"old_hp") == (V)10.0d), () => (V)(R.Get(R.Get(R.Get(v_1438, (V)3.0d), (V)"fields"), (V)"new_hp") == (V)14.0d)))));
} else if (R.Truth((V)(v_1422 == (V)"damage"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1426, (V)"damage") == (V)5.0d), () => (V)(R.Get(v_1426, (V)"slash_damage") == (V)4.0d)))));
var values_1445 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1424, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)2.0d))));
V v_1446 = values_1445[0];
var values_1447 = R.Join(new V[] {}, new Results((V)6.0d));
var obj_1448 = v_1446;
V key_1449 = (V)"hp";
R.Set(obj_1448, key_1449, values_1447[0]);
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1424}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1446, (V)"hp") == (V)1.0d))));
} else if (R.Truth((V)(v_1422 == (V)"defense"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1426, (V)"defense") == (V)1.0d))));
var values_1450 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_1424, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_1451 = values_1450[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1451, (V)"damage") == (V)3.0d))));
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1424}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1424}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1426, (V)"hp") == (V)8.0d))));
} else if (R.Truth((V)(v_1422 == (V)"speed"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1426, (V)"speed") == (V)110.0d), () => (V)(R.Call(R.Get(v_7, (V)"effective_cost"), R.Join(new V[] {(V)100.0d}, new Results(R.Get(v_1426, (V)"speed")))).First == (V)91.0d)))));
var values_1452 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1424}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_1453 = values_1452[0];
V v_1454 = values_1452[1];
V v_1455 = values_1452[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1455, (V)1.0d), (V)"kind") == (V)"Waited"), () => (V)(R.Get(v_1424, (V)"clock") == (V)91.0d)))));
} else if (R.Truth((V)(v_1422 == (V)"crit_chance"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1426, (V)"crit_chance_ppm") == (V)50000.0d))));
} else if (R.Truth((V)(v_1422 == (V)"crit_damage"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1426, (V)"crit_damage_percent") == (V)175.0d))));
}
}
return Results.Empty;
}))))));
V v_1457 = values_1456[0];
V v_1458 = values_1456[1];
foreach (var row_1459 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1412)))))) {
V v_1460 = row_1459[0];
V v_1461 = row_1459[1];
var values_1462 = R.Join(new V[] {}, new Results(v_1461));
var obj_1463 = R.Get(v_1410, (V)"stat_weights");
V key_1464 = v_1460;
R.Set(obj_1463, key_1464, values_1462[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_1457}, new Results(v_1458)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"effective_cost"), R.Join(new V[] {(V)100.0d}, new Results((V)4503599627370496.0d))).First == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"speed gained from a kill affects only future recovery"}, new Results(new V((RuleFunction)(args_1465 => {

var values_1466 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"get_progression"), Results.Empty));
V v_1467 = values_1466[0];
var values_1468 = R.Join(new V[] {}, new Results(R.Table()));
V v_1469 = values_1468[0];
foreach (var row_1470 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_1467, (V)"stat_weights"))))))) {
V v_1471 = row_1470[0];
V v_1472 = row_1470[1];
var values_1473 = R.Join(new V[] {}, new Results(v_1472));
var obj_1474 = v_1469;
V key_1475 = v_1471;
R.Set(obj_1474, key_1475, values_1473[0]);
var values_1476 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_1471 == (V)"speed"), () => (V)1000000.0d), () => (V)1.0d)));
var obj_1477 = R.Get(v_1467, (V)"stat_weights");
V key_1478 = v_1471;
R.Set(obj_1477, key_1478, values_1476[0]);
}
var values_1491 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1479 => {

var values_1480 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)117.0d))));
V v_1481 = values_1480[0];
var values_1482 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1481, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_1483 = values_1482[0];
var values_1484 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_1485 = v_1483;
V key_1486 = (V)"hp";
R.Set(obj_1485, key_1486, values_1484[0]);
var values_1487 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1481}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1488 = values_1487[0];
V v_1489 = values_1487[1];
V v_1490 = values_1487[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_425, R.Join(new V[] {v_1490}, new Results((V)"StatIncreased"))).First, (V)1.0d), (V)"stat") == (V)"speed"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1481))).First, (V)"speed") == (V)110.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1481, (V)"last_action"), (V)"cost") == (V)100.0d), () => (V)(R.Get(v_1481, (V)"clock") == (V)100.0d)))));
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1481}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1481, (V)"last_action"), (V)"cost") == (V)91.0d), () => (V)(R.Get(v_1481, (V)"clock") == (V)191.0d)))));
return Results.Empty;
}))))));
V v_1492 = values_1491[0];
V v_1493 = values_1491[1];
foreach (var row_1494 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1469)))))) {
V v_1495 = row_1494[0];
V v_1496 = row_1494[1];
var values_1497 = R.Join(new V[] {}, new Results(v_1496));
var obj_1498 = R.Get(v_1467, (V)"stat_weights");
V key_1499 = v_1495;
R.Set(obj_1498, key_1499, values_1497[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_1492}, new Results(v_1493)));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"direct critical strikes use seeded rolls and do not crit secondary effects"}, new Results(new V((RuleFunction)(args_1500 => {

var values_1501 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)108.0d))));
V v_1502 = values_1501[0];
var values_1503 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1502))));
V v_1504 = values_1503[0];
var values_1505 = R.Join(new V[] {(V)1000000.0d}, new Results((V)200.0d));
var obj_1506 = v_1504;
V key_1507 = (V)"crit_chance_ppm";
var obj_1508 = v_1504;
V key_1509 = (V)"crit_damage_percent";
R.Set(obj_1506, key_1507, values_1505[0]);
R.Set(obj_1508, key_1509, values_1505[1]);
var values_1510 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1502, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_1511 = values_1510[0];
var values_1512 = R.Join(new V[] {(V)20.0d}, new Results((V)20.0d));
var obj_1513 = v_1511;
V key_1514 = (V)"hp";
var obj_1515 = v_1511;
V key_1516 = (V)"max_hp";
R.Set(obj_1513, key_1514, values_1512[0]);
R.Set(obj_1515, key_1516, values_1512[1]);
var values_1517 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1502))))));
V v_1518 = values_1517[0];
var values_1519 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1502}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1520 = values_1519[0];
V v_1521 = values_1519[1];
V v_1522 = values_1519[2];
var values_1523 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1518}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1524 = values_1523[0];
V v_1525 = values_1523[1];
V v_1526 = values_1523[2];
R.Call(v_39, R.Join(new V[] {v_1522}, new Results(v_1526)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_1522, (V)1.0d), (V)"critical"), () => (V)(R.Get(R.Get(v_1522, (V)1.0d), (V)"raw_damage") == (V)8.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_1502}, new Results(R.Get(v_1511, (V)"id")))).First, (V)"hp") == (V)13.0d))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"currency drops are nonblocking, auto-collect once, and round-trip"}, new Results(new V((RuleFunction)(args_1527 => {

var values_1528 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)109.0d))));
V v_1529 = values_1528[0];
R.Call(v_812, R.Join(new V[] {v_1529, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)2.0d)));
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1529}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
var values_1530 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1529))));
V v_1531 = values_1530[0];
var values_1532 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, new Results(v_1531))));
V v_1533 = values_1532[0];
var values_1534 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1529}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1535 = values_1534[0];
V v_1536 = values_1534[1];
V v_1537 = values_1534[2];
var values_1538 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1533}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1539 = values_1538[0];
V v_1540 = values_1538[1];
V v_1541 = values_1538[2];
R.Call(v_39, R.Join(new V[] {v_1537}, new Results(v_1541)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_1529, (V)"clock") == (V)200.0d), () => (V)(R.Get(v_1529, (V)"currency") == (V)2.0d)), () => R.Get(R.Get(R.Get(v_1529, (V)"drops"), (V)1.0d), (V)"collected")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1537}, new Results((V)"TileEntered"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1537}, new Results((V)"CurrencyCollected"))).First) == (V)1.0d)))));
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1529}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))));
var values_1542 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1529}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1543 = values_1542[0];
V v_1544 = values_1542[1];
V v_1545 = values_1542[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1529, (V)"currency") == (V)2.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1545}, new Results((V)"CurrencyCollected"))).First) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"Dash picks up currency on each traversed cell without an extra action"}, new Results(new V((RuleFunction)(args_1546 => {

var values_1547 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)113.0d))));
V v_1548 = values_1547[0];
var values_1549 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)1.0d), new Entry((V)"kind", (V)"currency"), new Entry((V)"amount", (V)2.0d), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_1548, (V)"active_floor_id")), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))), new Entry((V)"collected", (V)false), new Entry((V)"killed_entity_id", (V)11.0d))), new Entry((V)2, R.Table(new Entry((V)"id", (V)2.0d), new Entry((V)"kind", (V)"currency"), new Entry((V)"amount", (V)3.0d), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_1548, (V)"active_floor_id")), new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)2.0d))), new Entry((V)"collected", (V)false), new Entry((V)"killed_entity_id", (V)12.0d))))));
var obj_1550 = v_1548;
V key_1551 = (V)"drops";
R.Set(obj_1550, key_1551, values_1549[0]);
var values_1552 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_1553 = v_1548;
V key_1554 = (V)"next_drop_id";
R.Set(obj_1553, key_1554, values_1552[0]);
var values_1555 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1548}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1556 = values_1555[0];
V v_1557 = values_1555[1];
V v_1558 = values_1555[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_1548, (V)"clock") == (V)130.0d), () => (V)(R.Get(v_1548, (V)"currency") == (V)5.0d)), () => (V)(R.Get(R.Get(v_1548, (V)"last_action"), (V)"cost") == (V)130.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1558}, new Results((V)"TileEntered"))).First) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1558}, new Results((V)"CurrencyCollected"))).First) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(R.Get(v_1548, (V)"drops"), (V)1.0d), (V)"collected"), () => R.Get(R.Get(R.Get(v_1548, (V)"drops"), (V)2.0d), (V)"collected")))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"chest caches three valid distinct offers; cancel, affordability, claim and traversal"}, new Results(new V((RuleFunction)(args_1559 => {

var values_1560 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)110.0d))));
V v_1561 = values_1560[0];
var values_1562 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_chest"), R.Join(new V[] {v_1561, (V)"core:chest/standard", (V)3.0d}, new Results((V)2.0d))));
V v_1563 = values_1562[0];
var values_1564 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1561))));
V v_1565 = values_1564[0];
var values_1566 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"chest_options"), R.Join(new V[] {v_1561}, new Results(R.Get(v_1563, (V)"id")))))));
V v_1567 = values_1566[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_1567, (V)"offers")) == (V)3.0d), () => (V)(R.Get(v_1567, (V)"price") == (V)5.0d)), () => (V)(R.Get(v_1567, (V)"shortfall") == (V)5.0d)))));
var values_1568 = R.Join(new V[] {}, new Results(R.Table()));
V v_1569 = values_1568[0];
foreach (var row_1570 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1567, (V)"offers"))))))) {
V v_1571 = row_1570[0];
V v_1572 = row_1570[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_23, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_1572, (V)"boon_id")))).First, () => (V)(!R.Truth(R.Get(v_1569, R.Get(v_1572, (V)"boon_id"))))))));
var values_1573 = R.Join(new V[] {}, new Results((V)true));
var obj_1574 = v_1569;
V key_1575 = R.Get(v_1572, (V)"boon_id");
R.Set(obj_1574, key_1575, values_1573[0]);
}
R.Call(v_39, R.Join(new V[] {v_1565}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1561)))));
var values_1576 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"chest_options"), R.Join(new V[] {v_1561}, new Results(R.Get(v_1563, (V)"id")))));
V v_1577 = values_1576[0];
R.Call(v_39, R.Join(new V[] {v_1567}, new Results(v_1577)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1561}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_1563, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))).First)))));
R.Call(v_39, R.Join(new V[] {v_1565}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1561)))));
var values_1578 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_1579 = v_1561;
V key_1580 = (V)"currency";
R.Set(obj_1579, key_1580, values_1578[0]);
var values_1581 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1561}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1582 = values_1581[0];
V v_1583 = values_1581[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_1583 == (V)"chest"), () => (V)(R.Get(v_1561, (V)"clock") == (V)0.0d)))));
var values_1584 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1567, (V)"offers"), (V)1.0d)));
V v_1585 = values_1584[0];
var values_1586 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1561}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_1563, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
V v_1587 = values_1586[0];
V v_1588 = values_1586[1];
V v_1589 = values_1586[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_1587, () => (V)(R.Get(v_1561, (V)"clock") == (V)100.0d)), () => (V)(R.Get(v_1561, (V)"currency") == (V)0.0d)), () => R.Get(v_1563, (V)"claimed")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_1563, (V)"blocks_movement"))), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1589}, new Results((V)"ChestClaimed"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1561))).First, (V)"boons"), R.Get(v_1585, (V)"boon_id")), R.Get(v_1585, (V)"rarity")) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1561}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_1563, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1561}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1561, (V)"currency") == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"elite chest uses free price and favorable validated rarity profile"}, new Results(new V((RuleFunction)(args_1590 => {

var values_1591 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/standard"))));
V v_1592 = values_1591[0];
var values_1593 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/free_elite"))));
V v_1594 = values_1593[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"validate_all"), Results.Empty)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1592, (V)"price") == (V)5.0d), () => (V)(R.Get(v_1594, (V)"price") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_1592, (V)"rarity_weights"), (V)"common") == (V)70.0d), () => (V)(R.Get(R.Get(v_1592, (V)"rarity_weights"), (V)"uncommon") == (V)25.0d)), () => (V)(R.Get(R.Get(v_1592, (V)"rarity_weights"), (V)"rare") == (V)4.0d)), () => (V)(R.Get(R.Get(v_1592, (V)"rarity_weights"), (V)"legendary") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_1594, (V)"rarity_weights"), (V)"common") == (V)20.0d), () => (V)(R.Get(R.Get(v_1594, (V)"rarity_weights"), (V)"uncommon") == (V)45.0d)), () => (V)(R.Get(R.Get(v_1594, (V)"rarity_weights"), (V)"rare") == (V)28.0d)), () => (V)(R.Get(R.Get(v_1594, (V)"rarity_weights"), (V)"legendary") == (V)7.0d)))));
var values_1595 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)111.0d))));
V v_1596 = values_1595[0];
var values_1597 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_chest"), R.Join(new V[] {v_1596, (V)"core:chest/free_elite", (V)3.0d}, new Results((V)2.0d))));
V v_1598 = values_1597[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_7, (V)"chest_options"), R.Join(new V[] {v_1596}, new Results(R.Get(v_1598, (V)"id")))).First, (V)"shortfall") == (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1596}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_1598, (V)"id")), new Entry((V)"offer_index", (V)2.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_1596, (V)"currency") == (V)0.0d), () => R.Get(v_1598, (V)"claimed")), () => (V)(R.Get(v_1596, (V)"clock") == (V)100.0d)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"separate chests can add mixed-rarity stacks of the same boon"}, new Results(new V((RuleFunction)(args_1599 => {

var values_1600 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)114.0d))));
V v_1601 = values_1600[0];
var values_1602 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_chest"), R.Join(new V[] {v_1601, (V)"core:chest/free_elite", (V)3.0d}, new Results((V)2.0d))));
V v_1603 = values_1602[0];
var values_1604 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_chest"), R.Join(new V[] {v_1601, (V)"core:chest/free_elite", (V)2.0d}, new Results((V)3.0d))));
V v_1605 = values_1604[0];
var values_1606 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"boon_id", (V)"core:boon/storm_conductor"), new Entry((V)"rarity", (V)"common"))));
var obj_1607 = R.Get(v_1603, (V)"offers");
V key_1608 = (V)1.0d;
R.Set(obj_1607, key_1608, values_1606[0]);
var values_1609 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"boon_id", (V)"core:boon/storm_conductor"), new Entry((V)"rarity", (V)"legendary"))));
var obj_1610 = R.Get(v_1605, (V)"offers");
V key_1611 = (V)1.0d;
R.Set(obj_1610, key_1611, values_1609[0]);
foreach (var row_1612 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_1603), new Entry((V)2, v_1605)))))))) {
V v_1613 = row_1612[0];
V v_1614 = row_1612[1];
var values_1615 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(R.Get(R.Get(v_1614, (V)"offers"), (V)1.0d), (V)"boon_id"), (V)true))));
V v_1616 = values_1615[0];
{ double end_1619 = R.Num((V)3.0d), step_1620 = R.Num((V)1.0d);
for (double i_1618 = R.Num((V)2.0d); step_1620 > 0 ? i_1618 <= end_1619 : i_1618 >= end_1619; i_1618 += step_1620) {
V v_1617 = i_1618;
if (R.Truth(R.Get(v_1616, R.Get(R.Get(R.Get(v_1614, (V)"offers"), v_1617), (V)"boon_id")))) {
foreach (var row_1621 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"order"))))))) {
V v_1622 = row_1621[0];
V v_1623 = row_1621[1];
if (R.Truth((V)(!R.Truth(R.Get(v_1616, v_1623))))) {
var values_1624 = R.Join(new V[] {}, new Results(v_1623));
var obj_1625 = R.Get(R.Get(v_1614, (V)"offers"), v_1617);
V key_1626 = (V)"boon_id";
R.Set(obj_1625, key_1626, values_1624[0]);
break;
}
}
}
var values_1627 = R.Join(new V[] {}, new Results((V)true));
var obj_1628 = v_1616;
V key_1629 = R.Get(R.Get(R.Get(v_1614, (V)"offers"), v_1617), (V)"boon_id");
R.Set(obj_1628, key_1629, values_1627[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_25, (V)"validate_chest"), R.Join(new V[] {}, new Results(v_1614)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1601}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_1603, (V)"id")), new Entry((V)"offer_index", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1601}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_1605, (V)"id")), new Entry((V)"offer_index", (V)1.0d)))))));
var values_1630 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1601))).First, (V)"boons"), (V)"core:boon/storm_conductor")));
V v_1631 = values_1630[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1631, (V)"common") == (V)1.0d), () => (V)(R.Get(v_1631, (V)"legendary") == (V)1.0d)))));
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1601))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1601)))))))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"committed enemy impact precedes newly ready chest purchaser at same time"}, new Results(new V((RuleFunction)(args_1632 => {

var values_1633 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)115.0d))));
V v_1634 = values_1633[0];
var values_1635 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_chest"), R.Join(new V[] {v_1634, (V)"core:chest/standard", (V)3.0d}, new Results((V)2.0d))));
V v_1636 = values_1635[0];
var values_1637 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_1638 = v_1634;
V key_1639 = (V)"currency";
R.Set(obj_1638, key_1639, values_1637[0]);
var values_1640 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_enemy"), R.Join(new V[] {v_1634, (V)"core:enemy/thornspitter", (V)2.0d}, new Results((V)5.0d))));
V v_1641 = values_1640[0];
var values_1642 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1634))).First, (V)"hp")));
V v_1643 = values_1642[0];
var values_1644 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1634}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_1636, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
V v_1645 = values_1644[0];
V v_1646 = values_1644[1];
V v_1647 = values_1644[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1634, (V)"clock") == (V)100.0d), () => (V)(R.Get(R.Get(v_1634, (V)"last_action"), (V)"cost") == (V)100.0d)))));
var values_1648 = Results.Empty;
V v_1649 = values_1648[0];
V v_1650 = values_1648[1];
V v_1651 = values_1648[2];
foreach (var row_1652 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_1647)))))) {
V v_1653 = row_1652[0];
V v_1654 = row_1652[1];
if (R.Truth(R.And((V)(R.Get(v_1654, (V)"kind") == (V)"WindupStarted"), () => (V)(R.Get(v_1654, (V)"source_id") == R.Get(v_1641, (V)"id"))))) {
var values_1655 = R.Join(new V[] {}, new Results(v_1653));
v_1649 = values_1655[0];
}
if (R.Truth(R.And((V)(R.Get(v_1654, (V)"kind") == (V)"AttackPerformed"), () => (V)(R.Get(v_1654, (V)"source_id") == R.Get(v_1641, (V)"id"))))) {
var values_1656 = R.Join(new V[] {}, new Results(v_1653));
v_1650 = values_1656[0];
}
if (R.Truth(R.And((V)(R.Get(v_1654, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_1654, (V)"source_id") == R.Get(v_1641, (V)"id"))))) {
var values_1657 = R.Join(new V[] {}, new Results(v_1653));
v_1651 = values_1657[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(v_1649, () => v_1650), () => v_1651), () => (V)(v_1649 < v_1650)), () => (V)(v_1650 < v_1651)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_1647, v_1650), (V)"time") == (V)100.0d), () => (V)(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1634))).First, (V)"hp") == (V)(v_1643 - R.Get(v_1641, (V)"damage")))))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"pending delayed credited kill retains XP and drop continuation after restore"}, new Results(new V((RuleFunction)(args_1658 => {

var values_1659 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)116.0d))));
V v_1660 = values_1659[0];
R.Call(v_820, R.Join(new V[] {v_1660, (V)"temporal_echo"}, new Results((V)"common")));
var values_1661 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1660, (V)"core:enemy/ruin_skitter", (V)3.0d}, new Results((V)2.0d))));
V v_1662 = values_1661[0];
var values_1663 = R.Join(new V[] {}, new Results((V)5.0d));
var obj_1664 = v_1662;
V key_1665 = (V)"hp";
R.Set(obj_1664, key_1665, values_1663[0]);
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1660}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
var values_1666 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1660))))));
V v_1667 = values_1666[0];
var values_1668 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1660}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_1669 = values_1668[0];
V v_1670 = values_1668[1];
V v_1671 = values_1668[2];
var values_1672 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1667}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_1673 = values_1672[0];
V v_1674 = values_1672[1];
V v_1675 = values_1672[2];
R.Call(v_39, R.Join(new V[] {v_1671}, new Results(v_1675)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1671}, new Results((V)"ExperienceGained"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1671}, new Results((V)"CurrencyDropped"))).First) == (V)1.0d)))));
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1660))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1667)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"chest offers and critical future remain identical after snapshot restore"}, new Results(new V((RuleFunction)(args_1676 => {

var values_1677 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)112.0d))));
V v_1678 = values_1677[0];
var values_1679 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_chest"), R.Join(new V[] {v_1678, (V)"core:chest/standard", (V)3.0d}, new Results((V)2.0d))));
V v_1680 = values_1679[0];
var values_1681 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_1682 = v_1678;
V key_1683 = (V)"currency";
R.Set(obj_1682, key_1683, values_1681[0]);
var values_1684 = R.Join(new V[] {}, new Results((V)250000.0d));
var obj_1685 = R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_1678))).First;
V key_1686 = (V)"crit_chance_ppm";
R.Set(obj_1685, key_1686, values_1684[0]);
var values_1687 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1678))))));
V v_1688 = values_1687[0];
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"chest_options"), R.Join(new V[] {v_1678}, new Results(R.Get(v_1680, (V)"id")))).First}, R.Call(R.Get(v_7, (V)"chest_options"), R.Join(new V[] {v_1688}, new Results(R.Get(v_1680, (V)"id"))))));
var values_1689 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1678}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_1680, (V)"id")), new Entry((V)"offer_index", (V)3.0d))))));
V v_1690 = values_1689[0];
V v_1691 = values_1689[1];
V v_1692 = values_1689[2];
var values_1693 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1688}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_1680, (V)"id")), new Entry((V)"offer_index", (V)3.0d))))));
V v_1694 = values_1693[0];
V v_1695 = values_1693[1];
V v_1696 = values_1693[2];
R.Call(v_39, R.Join(new V[] {v_1692}, new Results(v_1696)));
var values_1697 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1678, (V)"core:enemy/mossbound_guard", (V)2.0d}, new Results((V)3.0d))));
V v_1698 = values_1697[0];
R.Call(v_812, R.Join(new V[] {v_1688, (V)"core:enemy/mossbound_guard", (V)2.0d}, new Results((V)3.0d)));
var values_1699 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1678}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))))));
V v_1700 = values_1699[0];
V v_1701 = values_1699[1];
V v_1702 = values_1699[2];
var values_1703 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1688}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))))));
V v_1704 = values_1703[0];
V v_1705 = values_1703[1];
V v_1706 = values_1703[2];
R.Call(v_39, R.Join(new V[] {v_1702}, new Results(v_1706)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_1678}, new Results(R.Get(v_1698, (V)"id"))))));
R.Call(v_39, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1678))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1688)))));
return Results.Empty;
})))));
R.Call(v_50, R.Join(new V[] {(V)"ordinary kill, pickup, level, purchase and boon transform form a complete loop"}, new Results(new V((RuleFunction)(args_1707 => {

var values_1708 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_1709 = values_1708[0];
var values_1710 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"spawn_chest"), R.Join(new V[] {v_1709, (V)"core:chest/standard", (V)5.0d}, new Results((V)2.0d))));
V v_1711 = values_1710[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_1711, (V)"offers"), (V)1.0d), (V)"boon_id") == (V)"core:boon/crescent_reach"))));
var values_1712 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1709, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d))));
V v_1713 = values_1712[0];
var values_1714 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_1715 = v_1713;
V key_1716 = (V)"hp";
R.Set(obj_1715, key_1716, values_1714[0]);
var values_1717 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1709}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1718 = values_1717[0];
V v_1719 = values_1717[1];
V v_1720 = values_1717[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1720}, new Results((V)"EntityKilled"))).First) == (V)1.0d), () => (V)(R.Get(R.Get(v_1709, (V)"progression"), (V)"level") == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1720}, new Results((V)"StatIncreased"))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1709}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1709, (V)"currency") == (V)4.0d))));
var values_1721 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1709, (V)"core:enemy/ruin_skitter", (V)4.0d}, new Results((V)2.0d))));
V v_1722 = values_1721[0];
var values_1723 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_1724 = v_1722;
V key_1725 = (V)"hp";
R.Set(obj_1724, key_1725, values_1723[0]);
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1709}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1709}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_1709, (V)"currency") == (V)6.0d))));
var values_1726 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1709))));
V v_1727 = values_1726[0];
var values_1728 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1709}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1729 = values_1728[0];
V v_1730 = values_1728[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_1730 == (V)"chest"))));
R.Call(v_39, R.Join(new V[] {v_1727}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_1709)))));
var values_1731 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1709}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_1711, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
V v_1732 = values_1731[0];
V v_1733 = values_1731[1];
V v_1734 = values_1731[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_1709, (V)"currency") == (V)1.0d), () => (V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1734}, new Results((V)"BoonGranted"))).First) == (V)1.0d)))));
var values_1735 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"effective_ability"), R.Join(new V[] {v_1709, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_1736 = values_1735[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_1736) == (V)4.0d), () => (V)(R.Get(R.Get(v_1736, (V)4.0d), (V)"x") == (V)6.0d)), () => (V)(R.Get(R.Get(v_1736, (V)4.0d), (V)"y") == (V)2.0d)))));
var values_1737 = R.Join(new V[] {}, R.Call(v_812, R.Join(new V[] {v_1709, (V)"core:enemy/ruin_skitter", (V)6.0d}, new Results((V)2.0d))));
V v_1738 = values_1737[0];
var values_1739 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_1740 = v_1738;
V key_1741 = (V)"hp";
R.Set(obj_1740, key_1741, values_1739[0]);
var values_1742 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_1709}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_1743 = values_1742[0];
V v_1744 = values_1742[1];
V v_1745 = values_1742[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_425, R.Join(new V[] {v_1745}, new Results((V)"EntityKilled"))).First) == (V)1.0d), () => (V)(R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_1709}, new Results(R.Get(v_1738, (V)"id")))).First == V.Nil)))));
return Results.Empty;
})))));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.generation_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.exploration_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.stage_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.persistence_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.title_menu_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.pixel_ui_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.acceptance_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.motion_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.boon_expansion_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.biome_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.finale_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.meta_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.presentation_settings_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.run_seed_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.visual_assets_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.visual_packs_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.kenney_visuals_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.kenney_terrain_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.onebit_visuals_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.sprite_audit_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.spectator_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.expansion_floor_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.expansion_terrain_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.expansion_biome_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.expansion_encounter_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.expansion_class_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.expansion_boon_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.review_feature_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.corridor_branch_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.expansion_audio_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.expansion_full_run_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.pickup_aoe_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.enemy_squad_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_enemy_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_wraith_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_revival_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_toad_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_driver_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_cache_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_bone_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_alarm_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_web_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_oath_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_sentinel_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_mirror_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_silt_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_ember_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_duelist_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_geomancer_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_spiritbinder_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_tinkerer_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_stormcaller_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_wayfinder_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_class_integration_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_interaction_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_terrain_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_environment_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.fixed_visuals_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.settings_controls_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.visual_palette_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_boon_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_vow_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_dice_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_boon_handoff_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_pattern_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_control_boon_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_weapon_form_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_cadence_boon_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_support_boon_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_playback_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.visual_juice_tests"))).First, R.Join(new V[] {v_50}, new Results(v_39)));
R.Call(rt.Global("print"), R.Join(new V[] {}, R.Invoke((V)"%d passed, %d failed", "format", R.Join(new V[] {v_27}, new Results(v_28)))));
if (R.Truth((V)(v_28 > (V)0.0d))) {
R.Call(R.Get(rt.Global("os"), (V)"exit"), R.Join(new V[] {}, new Results((V)1.0d)));
}
return Results.Empty;
} }
