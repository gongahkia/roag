// Static C# port of tests/kenney_terrain_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 53770ca6fd8a0d230ad22ea8f0f2613483e08d17ba2b2753f225aa52ff78029f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_kenney_terrain_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_terrain"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_4 = values_3[0];
V v_5 = V.Nil;
v_5 = new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
var values_8 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {v_7}, new Results((V)"rb"))));
V v_9 = values_8[0];

if (R.Truth((V)(!R.Truth(v_9)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_10 = R.Join(new V[] {}, R.Invoke(v_9, "read", R.Join(new V[] {}, new Results((V)"*a"))));
V v_11 = values_10[0];

R.Invoke(v_9, "close", Results.Empty);

return R.Join(new V[] {}, new Results(v_11));
return Results.Empty;
}));
V v_12 = V.Nil;
v_12 = new V((RuleFunction)(args_13 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_14 => {

return R.Join(new V[] {}, new Results((V)492.0d));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_15 => {

return R.Join(new V[] {}, new Results((V)305.0d));
return Results.Empty;
}))))));
return Results.Empty;
}));
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
V v_20 = args_17[2];
V v_21 = args_17[3];
V v_22 = args_17[4];
V v_23 = args_17[5];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_18), new Entry((V)"y", v_19), new Entry((V)"w", v_20), new Entry((V)"h", v_21), new Entry((V)"pw", v_22), new Entry((V)"ph", v_23))));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
R.Call(v_25, R.Join(new V[] {(V)"Caves native source validates 16px regions with 1px gutters and rejects damaged inputs"}, new Results(new V((RuleFunction)(args_26 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_5, v_12}, new Results(v_16)))));
foreach (var row_27 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)38.0d), new Entry((V)3, (V)67.0d), new Entry((V)4, (V)183.0d), new Entry((V)5, (V)320.0d), new Entry((V)6, (V)521.0d)))))))) {
V v_28 = row_27[0];
V v_29 = row_27[1];
var values_30 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"region"), R.Join(new V[] {}, new Results(v_29))));
V v_31 = values_30[0];
V v_32 = values_30[1];
V v_33 = values_30[2];
V v_34 = values_30[3];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_31 == (V)((V)(v_29 % (V)29.0d) * (V)17.0d)), () => (V)(v_32 == (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_29 / (V)29.0d)))).First * (V)17.0d))), () => (V)(v_33 == (V)16.0d)), () => (V)(v_34 == (V)16.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)(v_31 + v_33) <= (V)492.0d), () => (V)((V)(v_32 + v_34) <= (V)305.0d)))));
}
foreach (var row_35 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)522.0d), new Entry((V)3, (V)1.5d), new Entry((V)4, (V)"38")))))))) {
V v_36 = row_35[0];
V v_37 = row_35[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"region")}, new Results(v_37))).First)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new"), new V((RuleFunction)(args_38 => {


return Results.Empty;
})), v_12}, new Results(v_16))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new"), new V((RuleFunction)(args_39 => {

return R.Join(new V[] {}, new Results((V)"broken"));
return Results.Empty;
})), v_12}, new Results(v_16))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new"), v_5, new V((RuleFunction)(args_40 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_41 => {

return R.Join(new V[] {}, new Results((V)491.0d));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_42 => {

return R.Join(new V[] {}, new Results((V)305.0d));
return Results.Empty;
}))))));
return Results.Empty;
}))}, new Results(v_16))).First)))));
return Results.Empty;
})))));
R.Call(v_25, R.Join(new V[] {(V)"all biomes cover every wall/water neighborhood without disclosing unknown geometry"}, new Results(new V((RuleFunction)(args_43 => {

var values_44 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_5, v_12}, new Results(v_16))));
V v_45 = values_44[0];
var values_46 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)4.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)8.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)16.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)32.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)64.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)128.0d))))));
V v_47 = values_46[0];
foreach (var row_48 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"order"))))))) {
V v_49 = row_48[0];
V v_50 = row_48[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"styles"), R.Invoke(v_50, "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))).First))));
foreach (var row_51 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"wall"), new Entry((V)2, (V)"water")))))))) {
V v_52 = row_51[0];
V v_53 = row_51[1];
var values_54 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_53 == (V)"wall"), () => (V)"#"), () => (V)"~")));
V v_55 = values_54[0];
{ double end_58 = R.Num((V)255.0d), step_59 = R.Num((V)1.0d);
for (double i_57 = R.Num((V)0.0d); step_59 > 0 ? i_57 <= end_58 : i_57 >= end_58; i_57 += step_59) {
V v_56 = i_57;
var values_60 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"."), new Entry((V)2, (V)"."), new Entry((V)3, (V)"."))), new Entry((V)2, R.Table(new Entry((V)1, (V)"."), new Entry((V)2, v_55), new Entry((V)3, (V)"."))), new Entry((V)3, R.Table(new Entry((V)1, (V)"."), new Entry((V)2, (V)"."), new Entry((V)3, (V)"."))))));
V v_61 = values_60[0];
foreach (var row_62 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_47)))))) {
V v_63 = row_62[0];
V v_64 = row_62[1];
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_56 / R.Get(v_64, (V)3.0d))))).First % (V)2.0d) == (V)1.0d))) {
var values_65 = R.Join(new V[] {}, new Results(v_55));
var obj_66 = R.Get(v_61, (V)((V)2.0d + R.Get(v_64, (V)2.0d)));
V key_67 = (V)((V)2.0d + R.Get(v_64, (V)1.0d));
R.Set(obj_66, key_67, values_65[0]);
}
}
var values_68 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"biome_id", v_50), new Entry((V)"rows", R.Table()))));
V v_69 = values_68[0];
foreach (var row_70 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_61)))))) {
V v_71 = row_70[0];
V v_72 = row_70[1];
var values_73 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(v_72))));
var obj_74 = R.Get(v_69, (V)"rows");
V key_75 = v_71;
R.Set(obj_74, key_75, values_73[0]);
}
var values_76 = R.Join(new V[] {}, R.Invoke(v_45, "resolve", R.Join(new V[] {v_69, (V)2.0d, (V)2.0d}, new Results(v_53))));
V v_77 = values_76[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_77, (V)"wall_mask"), () => R.Get(v_77, (V)"water_mask")) == R.Call(R.Get(v_2, (V)"canonical"), R.Join(new V[] {}, new Results(v_56))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_77, (V)"terrain_pack") == (V)"caves"), () => (V)(R.Get(v_77, (V)"width") == (V)16.0d)), () => (V)(R.Get(v_77, (V)"height") == (V)16.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_77, (V)"quad"), (V)"x") >= (V)0.0d), () => (V)(R.Get(R.Get(v_77, (V)"quad"), (V)"y") >= (V)0.0d)), () => (V)((V)(R.Get(R.Get(v_77, (V)"quad"), (V)"x") + (V)16.0d) <= (V)492.0d)), () => (V)((V)(R.Get(R.Get(v_77, (V)"quad"), (V)"y") + (V)16.0d) <= (V)305.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(v_45, "resolve", R.Join(new V[] {v_69, (V)2.0d, (V)2.0d, v_53}, new Results(new V((RuleFunction)(args_78 => {

return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}))))).First == R.Or(R.And((V)(v_53 == (V)"wall"), () => R.Get(R.Get(R.Get(v_45, (V)"walls"), R.Get(R.Get(R.Get(v_2, (V)"styles"), R.Invoke(v_50, "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))).First), (V)"wall")), (V)255.0d)), () => R.Get(R.Get(R.Get(v_45, (V)"waters"), R.Get(R.Get(R.Get(v_2, (V)"styles"), R.Invoke(v_50, "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))).First), (V)"water")), (V)255.0d))))));
}
}
}
}
return Results.Empty;
})))));
R.Call(v_25, R.Join(new V[] {(V)"Caves surface assembly preserves native quarter pixels and leaves source untouched"}, new Results(new V((RuleFunction)(args_79 => {

var values_80 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_81 = values_80[0];
V v_82 = values_80[1];
var values_83 = R.Join(new V[] {}, new Results(R.Table()));
V v_84 = values_83[0];
R.Set(v_84, "getPixel", new V((RuleFunction)(args_85 => {
V v_86 = args_85[0];
V v_87 = args_85[1];
V v_88 = args_85[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_87 >= (V)0.0d), () => (V)(v_88 >= (V)0.0d)), () => (V)(v_87 < (V)492.0d)), () => (V)(v_88 < (V)305.0d)))));
return R.Join(new V[] {(V)(v_87 / (V)512.0d), (V)(v_88 / (V)512.0d), (V)0.0d}, new Results((V)1.0d));
return Results.Empty;
})));
R.Set(v_84, "setPixel", new V((RuleFunction)(args_89 => {
V v_90 = args_89[0];
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"native source was mutated")));
return Results.Empty;
})));
R.Set(v_84, "clone", new V((RuleFunction)(args_91 => {
V v_92 = args_91[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"setPixel", new V((RuleFunction)(args_93 => {
V v_94 = args_93[0];
V v_95 = args_93[1];
V v_96 = args_93[2];
V v_97 = args_93[3];
V v_98 = args_93[4];
V v_99 = args_93[5];
V v_100 = args_93[6];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_81, (V)((V)(v_96 * (V)492.0d) + v_95))))}, new Results((V)"overlapping compiled regions")));
var values_101 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_97), new Entry((V)2, v_98), new Entry((V)3, v_99), new Entry((V)4, v_100))));
var obj_102 = v_81;
V key_103 = (V)((V)(v_96 * (V)492.0d) + v_95);
R.Set(obj_102, key_103, values_101[0]);

var values_104 = R.Join(new V[] {}, new Results((V)(v_82 + (V)1.0d)));
v_82 = values_104[0];
return Results.Empty;
}))))));
return Results.Empty;
})));
R.Call(R.Get(v_2, (V)"surface_sheet"), R.Join(new V[] {}, new Results(v_84)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_82 == (V)((V)((V)188.0d * (V)16.0d) * (V)16.0d)), () => (V)((V)R.Length(R.Get(v_2, (V)"masks")) == (V)47.0d)))));
V v_105 = V.Nil;
v_105 = new V((RuleFunction)(args_106 => {
V v_107 = args_106[0];
V v_108 = args_106[1];
V v_109 = args_106[2];
V v_110 = args_106[3];
foreach (var row_111 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"surfaces"))))))) {
V v_112 = row_111[0];
V v_113 = row_111[1];
if (R.Truth(R.And((V)(R.Get(v_113, (V)"family") == v_107), () => (V)(R.Get(v_113, (V)"mask") == v_108)))) {
var values_114 = R.Join(new V[] {(V)((V)((V)(v_112 - (V)1.0d) % (V)29.0d) * (V)16.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_112 - (V)1.0d) / (V)29.0d)))).First * (V)16.0d)));
V v_115 = values_114[0];
V v_116 = values_114[1];
return R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_81, (V)((V)((V)((V)(v_116 + v_110) * (V)492.0d) + v_115) + v_109))))));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"missing surface")));
return Results.Empty;
}));
V v_117 = V.Nil;
v_117 = new V((RuleFunction)(args_118 => {
V v_119 = args_118[0];
V v_120 = args_118[1];
V v_121 = args_118[2];
V v_122 = args_118[3];
var values_123 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"region"), R.Join(new V[] {}, new Results(v_120))));
V v_124 = values_123[0];
V v_125 = values_123[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_119, (V)1.0d) == (V)((V)(v_124 + v_121) / (V)512.0d)), () => (V)(R.Get(v_119, (V)2.0d) == (V)((V)(v_125 + v_122) / (V)512.0d))), () => (V)(R.Get(v_119, (V)4.0d) == (V)1.0d)))));
return Results.Empty;
}));
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"blue"), new Entry((V)2, (V)"green")))))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
var values_129 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"water_materials"), v_128)));
V v_130 = values_129[0];
{ double end_133 = R.Num((V)15.0d), step_134 = R.Num((V)1.0d);
for (double i_132 = R.Num((V)0.0d); step_134 > 0 ? i_132 <= end_133 : i_132 >= end_133; i_132 += step_134) {
V v_131 = i_132;
{ double end_137 = R.Num((V)15.0d), step_138 = R.Num((V)1.0d);
for (double i_136 = R.Num((V)0.0d); step_138 > 0 ? i_136 <= end_137 : i_136 >= end_137; i_136 += step_138) {
V v_135 = i_136;
R.Call(v_117, R.Join(new V[] {R.Call(v_105, R.Join(new V[] {v_128, (V)255.0d, v_135}, new Results(v_131))).First, R.Get(v_130, (V)5.0d), v_135}, new Results(v_131)));
}
}
}
}
R.Call(v_117, R.Join(new V[] {R.Call(v_105, R.Join(new V[] {v_128, (V)0.0d, (V)0.0d}, new Results((V)0.0d))).First, R.Get(v_130, (V)1.0d), (V)0.0d}, new Results((V)0.0d)));
R.Call(v_117, R.Join(new V[] {R.Call(v_105, R.Join(new V[] {v_128, (V)127.0d, (V)0.0d}, new Results((V)0.0d))).First, R.Get(v_130, (V)10.0d), (V)0.0d}, new Results((V)0.0d)));
}
foreach (var row_139 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"stone"), new Entry((V)2, (V)"earth")))))))) {
V v_140 = row_139[0];
V v_141 = row_139[1];
{ double end_144 = R.Num((V)15.0d), step_145 = R.Num((V)1.0d);
for (double i_143 = R.Num((V)0.0d); step_145 > 0 ? i_143 <= end_144 : i_143 >= end_144; i_143 += step_145) {
V v_142 = i_143;
{ double end_148 = R.Num((V)15.0d), step_149 = R.Num((V)1.0d);
for (double i_147 = R.Num((V)0.0d); step_149 > 0 ? i_147 <= end_148 : i_147 >= end_148; i_147 += step_149) {
V v_146 = i_147;
R.Call(v_117, R.Join(new V[] {R.Call(v_105, R.Join(new V[] {v_141, (V)255.0d, v_146}, new Results(v_142))).First, R.Get(R.Get(R.Get(v_2, (V)"wall_materials"), v_141), (V)"center"), (V)8.0d}, new Results((V)8.0d)));
}
}
}
}
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
