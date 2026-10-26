// Static C# port of tests/onebit_visuals_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: a7200391d194e843ec46cac7b05581764ef89ebd7b916e7d97abcb3c8e92fddb
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_onebit_visuals_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_visuals"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_slots"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_packs"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_12 = values_11[0];
V v_13 = V.Nil;
v_13 = new V((RuleFunction)(args_14 => {
V v_15 = args_14[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_15 == R.Get(v_2, (V)"path"))}, new Results((V)"standalone 1-Bit read another art pack")));
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {v_15}, new Results((V)"rb"))))));
V v_17 = values_16[0];

var values_18 = R.Join(new V[] {}, R.Invoke(v_17, "read", R.Join(new V[] {}, new Results((V)"*a"))));
V v_19 = values_18[0];

R.Invoke(v_17, "close", Results.Empty);

return R.Join(new V[] {}, new Results(v_19));
return Results.Empty;
}));
V v_20 = V.Nil;
v_20 = new V((RuleFunction)(args_21 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_22 => {

return R.Join(new V[] {}, new Results((V)784.0d));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_23 => {

return R.Join(new V[] {}, new Results((V)352.0d));
return Results.Empty;
}))))));
return Results.Empty;
}));
V v_24 = V.Nil;
v_24 = new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
V v_27 = args_25[1];
V v_28 = args_25[2];
V v_29 = args_25[3];
V v_30 = args_25[4];
V v_31 = args_25[5];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_26), new Entry((V)"y", v_27), new Entry((V)"w", v_28), new Entry((V)"h", v_29), new Entry((V)"pw", v_30), new Entry((V)"ph", v_31))));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_32 => {
V v_33 = args_32[0];
V v_34 = args_32[1];
R.Call(v_33, R.Join(new V[] {(V)"1-Bit sheet ink and composed surface background are neutral"}, new Results(new V((RuleFunction)(args_35 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_2, (V)"ink"), (V)1.0d) == R.Get(R.Get(v_2, (V)"ink"), (V)2.0d)), () => (V)(R.Get(R.Get(v_2, (V)"ink"), (V)2.0d) == R.Get(R.Get(v_2, (V)"ink"), (V)3.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_2, (V)"background"), (V)1.0d) == R.Get(R.Get(v_2, (V)"background"), (V)2.0d)), () => (V)(R.Get(R.Get(v_2, (V)"background"), (V)2.0d) == R.Get(R.Get(v_2, (V)"background"), (V)3.0d))))));
return Results.Empty;
})))));
R.Call(v_33, R.Join(new V[] {(V)"standalone 1-Bit covers every actor, prop, boon and terrain sprite using only its own PNG"}, new Results(new V((RuleFunction)(args_36 => {

var values_37 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_13, v_20}, new Results(v_24))));
V v_38 = values_37[0];
V v_39 = values_37[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_39 == (V)"loaded"), () => (V)(R.Get(v_38, (V)"style") == (V)"onebit")))));
var values_40 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_41 = values_40[0];
V v_42 = values_40[1];
foreach (var row_43 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"bindings"))))))) {
V v_44 = row_43[0];
if (R.Truth(R.Or(R.Invoke(v_44, "match", R.Join(new V[] {}, new Results((V)"^core:actor/"))).First, () => R.Invoke(v_44, "match", R.Join(new V[] {}, new Results((V)"^core:enemy/"))).First))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Invoke(v_38, "resolve", R.Join(new V[] {}, new Results(v_44))).First}, new Results(R.Concat((V)"missing native 1-Bit actor/pose ", v_44))));
}
}
foreach (var row_45 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"actors"))))))) {
V v_46 = row_45[0];
V v_47 = row_45[1];
var values_48 = R.Join(new V[] {}, R.Invoke(v_38, "resolve", R.Join(new V[] {}, new Results(v_46))));
V v_49 = values_48[0];
var values_50 = R.Join(new V[] {}, new Results(R.Get(v_41, v_47)));
V v_51 = values_50[0];
if (R.Truth(v_51)) {
var values_52 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:enemy/bone_fragment", (V)"core:enemy/bone_splitter"), new Entry((V)"core:enemy/bone_splitter", (V)"core:enemy/bone_fragment"), new Entry((V)"core:actor/duelist", (V)"core:enemy/oath_twin_a"), new Entry((V)"core:enemy/oath_twin_a", (V)"core:actor/duelist"))));
V v_53 = values_52[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_53, v_46) == v_51)}, new Results(R.Concat((V)"unexpected shared actor silhouette ", v_46))));
}
var values_54 = R.Join(new V[] {}, new Results(v_46));
var obj_55 = v_41;
V key_56 = v_47;
R.Set(obj_55, key_56, values_54[0]);

var values_57 = R.Join(new V[] {}, new Results((V)(v_42 + (V)1.0d)));
v_42 = values_57[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_49, (V)"source") == (V)"onebit"), () => (V)(R.Get(v_49, (V)"width") == (V)16.0d)), () => (V)(R.Get(R.Get(v_49, (V)"quad"), (V)"x") == (V)((V)(v_47 % (V)49.0d) * (V)16.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Invoke(v_38, "resolve", R.Join(new V[] {}, new Results(R.Concat(v_46, (V)"/attack")))).First == v_49), () => (V)(R.Invoke(v_38, "resolve", R.Join(new V[] {}, new Results(R.Concat(v_46, (V)"/hurt")))).First == v_49)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_42 == (V)65.0d))));
foreach (var row_58 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"objects"))))))) {
V v_59 = row_58[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(v_38, "resolve", R.Join(new V[] {}, new Results(v_59))).First, () => R.Get(R.Get(v_4, (V)"bindings"), v_59)))));
}
foreach (var row_60 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"order"))))))) {
V v_61 = row_60[0];
V v_62 = row_60[1];
var values_63 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_38, "resolve", R.Join(new V[] {}, new Results(R.Concat(v_62, (V)"/icon")))))));
V v_64 = values_63[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(v_38, "resolve", R.Join(new V[] {}, new Results(R.Concat(v_62, (V)"/detail")))).First == v_64))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_64, (V)"source") == (V)"onebit"), () => (V)(R.Get(R.Get(v_64, (V)"tint"), (V)1.0d) == R.Get(R.Get(v_2, (V)"ink"), (V)1.0d))))));
}
foreach (var row_65 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"order"))))))) {
V v_66 = row_65[0];
V v_67 = row_65[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_38, "resolve", R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(v_67))).First, (V)"id"))))));
}
return Results.Empty;
})))));
R.Call(v_33, R.Join(new V[] {(V)"standalone 1-Bit neighbor lookup, opacity and source composition remain deterministic"}, new Results(new V((RuleFunction)(args_68 => {

var values_69 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_13, v_20}, new Results(v_24))));
V v_70 = values_69[0];
var values_71 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"new_stage"), R.Join(new V[] {}, new Results((V)13.0d))));
V v_72 = values_71[0];

var values_73 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"snapshot"), R.Join(new V[] {}, new Results(v_72))));
V v_74 = values_73[0];
var values_75 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)4.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)8.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)16.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)32.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)64.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)128.0d))))));
V v_76 = values_75[0];
foreach (var row_77 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"wall"), new Entry((V)2, (V)"water")))))))) {
V v_78 = row_77[0];
V v_79 = row_77[1];
var values_80 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_79 == (V)"wall"), () => (V)"#"), () => (V)"~")));
V v_81 = values_80[0];
{ double end_84 = R.Num((V)255.0d), step_85 = R.Num((V)1.0d);
for (double i_83 = R.Num((V)0.0d); step_85 > 0 ? i_83 <= end_84 : i_83 >= end_84; i_83 += step_85) {
V v_82 = i_83;
var values_86 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"."), new Entry((V)2, (V)"."), new Entry((V)3, (V)"."))), new Entry((V)2, R.Table(new Entry((V)1, (V)"."), new Entry((V)2, v_81), new Entry((V)3, (V)"."))), new Entry((V)3, R.Table(new Entry((V)1, (V)"."), new Entry((V)2, (V)"."), new Entry((V)3, (V)"."))))));
V v_87 = values_86[0];
foreach (var row_88 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_76)))))) {
V v_89 = row_88[0];
V v_90 = row_88[1];
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_82 / R.Get(v_90, (V)3.0d))))).First % (V)2.0d) == (V)1.0d))) {
var values_91 = R.Join(new V[] {}, new Results(v_81));
var obj_92 = R.Get(v_87, (V)(R.Get(v_90, (V)2.0d) + (V)2.0d));
V key_93 = (V)(R.Get(v_90, (V)1.0d) + (V)2.0d);
R.Set(obj_92, key_93, values_91[0]);
}
}
var values_94 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rows", R.Table()))));
V v_95 = values_94[0];
foreach (var row_96 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_87)))))) {
V v_97 = row_96[0];
V v_98 = row_96[1];
var values_99 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(v_98))));
var obj_100 = R.Get(v_95, (V)"rows");
V key_101 = v_97;
R.Set(obj_100, key_101, values_99[0]);
}
var values_102 = R.Join(new V[] {}, R.Invoke(v_70, "terrain", R.Join(new V[] {v_95, (V)2.0d, (V)2.0d}, new Results(v_79))));
V v_103 = values_102[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_103, (V)"wall_mask"), () => R.Get(v_103, (V)"water_mask")) == R.Call(R.Get(v_2, (V)"canonical"), R.Join(new V[] {}, new Results(v_82))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(v_70, "terrain", R.Join(new V[] {v_95, (V)2.0d, (V)2.0d, v_79}, new Results(new V((RuleFunction)(args_104 => {

return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}))))).First == R.Or(R.And((V)(v_79 == (V)"wall"), () => R.Get(R.Get(v_70, (V)"walls"), (V)255.0d)), () => R.Get(R.Get(v_70, (V)"waters"), (V)255.0d))))));
}
}
}
R.Call(v_34, R.Join(new V[] {v_74}, R.Call(R.Get(v_12, (V)"snapshot"), R.Join(new V[] {}, new Results(v_72)))));
var values_105 = R.Join(new V[] {}, new Results(R.Table()));
V v_106 = values_105[0];

var values_107 = R.Join(new V[] {}, new Results(R.Table()));
V v_108 = values_107[0];
R.Set(v_108, "getPixel", new V((RuleFunction)(args_109 => {
V v_110 = args_109[0];
V v_111 = args_109[1];
V v_112 = args_109[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_111 >= (V)0.0d), () => (V)(v_112 >= (V)0.0d)), () => (V)(v_111 < (V)784.0d)), () => (V)(v_112 < (V)352.0d)))));

return R.Join(new V[] {(V)1.0d, (V)1.0d, (V)1.0d}, new Results((V)((V)(v_111 + v_112) % (V)2.0d)));
return Results.Empty;
})));
R.Set(v_108, "setPixel", new V((RuleFunction)(args_113 => {
V v_114 = args_113[0];
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"1-Bit source was changed")));
return Results.Empty;
})));
R.Set(v_108, "clone", new V((RuleFunction)(args_115 => {
V v_116 = args_115[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"setPixel", new V((RuleFunction)(args_117 => {
V v_118 = args_117[0];
V v_119 = args_117[1];
V v_120 = args_117[2];
V v_121 = args_117[3];
V v_122 = args_117[4];
V v_123 = args_117[5];
V v_124 = args_117[6];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_106, (V)((V)(v_120 * (V)784.0d) + v_119)))), () => (V)(v_124 == (V)1.0d)))));

var values_125 = R.Join(new V[] {}, new Results((V)true));
var obj_126 = v_106;
V key_127 = (V)((V)(v_120 * (V)784.0d) + v_119);
R.Set(obj_126, key_127, values_125[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And((V)(v_121 >= (V)0.0d), () => (V)(v_121 <= (V)1.0d)), () => (V)(v_122 >= (V)0.0d)), () => (V)(v_122 <= (V)1.0d)), () => (V)(v_123 >= (V)0.0d)), () => (V)(v_123 <= (V)1.0d)))));
return Results.Empty;
}))))));
return Results.Empty;
})));
R.Call(R.Get(v_2, (V)"surface_sheet"), R.Join(new V[] {}, new Results(v_108)));
var values_128 = R.Join(new V[] {}, new Results((V)0.0d));
V v_129 = values_128[0];

foreach (var row_130 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_106)))))) {
V v_131 = row_130[0];
var values_132 = R.Join(new V[] {}, new Results((V)(v_129 + (V)1.0d)));
v_129 = values_132[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_129 == (V)((V)((V)95.0d * (V)16.0d) * (V)16.0d)))));
return Results.Empty;
})))));
R.Call(v_33, R.Join(new V[] {(V)"standalone 1-Bit fails closed on absent, corrupt or wrongly sized art"}, new Results(new V((RuleFunction)(args_133 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_134 => {


return Results.Empty;
})), v_20}, new Results(v_24))).First == V.Nil))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new"), new V((RuleFunction)(args_135 => {

return R.Join(new V[] {}, new Results((V)"broken"));
return Results.Empty;
})), v_20}, new Results(v_24))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new"), v_13, new V((RuleFunction)(args_136 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_137 => {

return R.Join(new V[] {}, new Results((V)783.0d));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_138 => {

return R.Join(new V[] {}, new Results((V)352.0d));
return Results.Empty;
}))))));
return Results.Empty;
}))}, new Results(v_24))).First)))));
return Results.Empty;
})))));
R.Call(v_33, R.Join(new V[] {(V)"fixed 1-Bit construction has no saved-choice dependency and leaves the run intact"}, new Results(new V((RuleFunction)(args_139 => {

var values_140 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"new_stage"), R.Join(new V[] {}, new Results((V)13.0d))));
V v_141 = values_140[0];

var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"snapshot"), R.Join(new V[] {}, new Results(v_141))));
V v_143 = values_142[0];
var values_144 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_13, v_20}, new Results(v_24))));
V v_145 = values_144[0];
var values_147 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"build"), R.Join(new V[] {v_13, v_20}, new Results(new V((RuleFunction)(args_146 => {

return R.Join(new V[] {v_145}, new Results((V)"loaded"));
return Results.Empty;
}))))));
V v_148 = values_147[0];
V v_149 = values_147[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_149 == (V)"loaded"), () => (V)(R.Get(R.Invoke(v_148, "resolve", R.Join(new V[] {}, new Results((V)"core:actor/adventurer"))).First, (V)"source") == (V)"onebit")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_10, (V)"save") == V.Nil), () => (V)(R.Get(v_10, (V)"inspect") == V.Nil)))));
R.Call(v_34, R.Join(new V[] {v_143}, R.Call(R.Get(v_12, (V)"snapshot"), R.Join(new V[] {}, new Results(v_141)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
