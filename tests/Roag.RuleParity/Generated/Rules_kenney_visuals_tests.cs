// Static C# port of tests/kenney_visuals_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 220f7a088db805108a50d83a6675a4af0c9f03f845dc38e1d1bdb437dea01ea5
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_kenney_visuals_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legacy_kenney_visuals"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_packs"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_slots"))));
V v_8 = values_7[0];
V v_9 = V.Nil;
v_9 = new V((RuleFunction)(args_10 => {
V v_11 = args_10[0];
var values_12 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {v_11}, new Results((V)"rb"))));
V v_13 = values_12[0];

if (R.Truth((V)(!R.Truth(v_13)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_14 = R.Join(new V[] {}, R.Invoke(v_13, "read", R.Join(new V[] {}, new Results((V)"*a"))));
V v_15 = values_14[0];

R.Invoke(v_13, "close", Results.Empty);

return R.Join(new V[] {}, new Results(v_15));
return Results.Empty;
}));
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
V v_20 = args_17[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(v_18, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)8.0d))).First == (V)"\u00c2\u0089PNG\r\n\u001a\n"))));
V v_21 = V.Nil;
v_21 = new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
var values_24 = R.Join(new V[] {}, R.Invoke(v_18, "byte", R.Join(new V[] {v_23}, new Results((V)(v_23 + (V)3.0d)))));
V v_25 = values_24[0];
V v_26 = values_24[1];
V v_27 = values_24[2];
V v_28 = values_24[3];

return R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)((V)(v_25 * (V)256.0d) + v_26) * (V)256.0d) + v_27) * (V)256.0d) + v_28)));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"transparent", v_20), new Entry((V)"getWidth", new V((RuleFunction)(args_29 => {

return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {}, new Results((V)17.0d))));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_30 => {

return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {}, new Results((V)21.0d))));
return Results.Empty;
}))))));
return Results.Empty;
}));
V v_31 = V.Nil;
v_31 = new V((RuleFunction)(args_32 => {
V v_33 = args_32[0];
V v_34 = args_32[1];
V v_35 = args_32[2];
V v_36 = args_32[3];
V v_37 = args_32[4];
V v_38 = args_32[5];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_33), new Entry((V)"y", v_34), new Entry((V)"w", v_35), new Entry((V)"h", v_36), new Entry((V)"pw", v_37), new Entry((V)"ph", v_38))));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_39 => {
V v_40 = args_39[0];
V v_41 = args_39[1];
R.Call(v_40, R.Join(new V[] {(V)"Kenney native packed regions cover classes, enemies, props and all biomes"}, new Results(new V((RuleFunction)(args_42 => {

var values_43 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_9, v_16}, new Results(v_31))))));
V v_44 = values_43[0];
foreach (var row_45 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"actors"))))))) {
V v_46 = row_45[0];
V v_47 = row_45[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_8, (V)"bindings"), v_46))));
var values_48 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_44, "resolve", R.Join(new V[] {}, new Results(v_46))))));
V v_49 = values_48[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_49, (V)"width") == (V)8.0d), () => (V)(R.Get(v_49, (V)"height") == (V)8.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_49, (V)"quad"), (V)"x") == (V)((V)(v_47 % (V)16.0d) * (V)8.0d)), () => (V)(R.Get(R.Get(v_49, (V)"quad"), (V)"y") == (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_47 / (V)16.0d)))).First * (V)8.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_49, (V)"texture"), (V)"transparent"), () => (V)(R.Invoke(v_44, "resolve", R.Join(new V[] {}, new Results(R.Concat(v_46, (V)"/attack")))).First == v_49)))));
}
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"objects"))))))) {
V v_51 = row_50[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_8, (V)"bindings"), v_51), () => R.Invoke(v_44, "resolve", R.Join(new V[] {}, new Results(v_51))).First))));
}
var values_52 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"region"), R.Join(new V[] {}, new Results((V)159.0d))));
V v_53 = values_52[0];
V v_54 = values_52[1];
V v_55 = values_52[2];
V v_56 = values_52[3];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_53 == (V)120.0d), () => (V)(v_54 == (V)72.0d)), () => (V)(v_55 == (V)8.0d)), () => (V)(v_56 == (V)8.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"region")}, new Results((V)160.0d))).First)), () => (V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"region")}, new Results((-(V)1.0d)))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"region")}, new Results((V)1.5d))).First)))));
return Results.Empty;
})))));
R.Call(v_40, R.Join(new V[] {(V)"Kenney reads exact checked source and fails closed on missing or corrupt sheet"}, new Results(new V((RuleFunction)(args_57 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_58 => {


return Results.Empty;
})), v_16}, new Results(v_31))).First == V.Nil))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new"), new V((RuleFunction)(args_59 => {

return R.Join(new V[] {}, new Results((V)"corrupt"));
return Results.Empty;
})), v_16}, new Results(v_31))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new"), v_9, new V((RuleFunction)(args_60 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_61 => {

return R.Join(new V[] {}, new Results((V)129.0d));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_62 => {

return R.Join(new V[] {}, new Results((V)80.0d));
return Results.Empty;
}))))));
return Results.Empty;
}))}, new Results(v_31))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new"), new V((RuleFunction)(args_63 => {
V v_64 = args_63[0];
if (R.Truth((V)(v_64 == (V)"assets/kenney/approved/tiny-dungeon.png"))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
return R.Join(new V[] {}, R.Call(v_9, R.Join(new V[] {}, new Results(v_64))));
return Results.Empty;
})), v_16}, new Results(v_31))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new"), new V((RuleFunction)(args_65 => {
V v_66 = args_65[0];
if (R.Truth((V)(v_66 == (V)"assets/kenney/approved/roguelike-rpg.png"))) {
return R.Join(new V[] {}, new Results((V)"changed"));
}
return R.Join(new V[] {}, R.Call(v_9, R.Join(new V[] {}, new Results(v_66))));
return Results.Empty;
})), v_16}, new Results(v_31))).First)))));
var values_67 = R.Join(new V[] {}, new Results((V)0.0d));
V v_68 = values_67[0];
var values_78 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_9, new V((RuleFunction)(args_69 => {
V v_70 = args_69[0];
V v_71 = args_69[1];
V v_72 = args_69[2];
var values_73 = R.Join(new V[] {}, new Results((V)(v_68 + (V)1.0d)));
v_68 = values_73[0];
var values_74 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_terrain"))));
V v_75 = values_74[0];
var values_76 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_2, (V)"path")), new Entry((V)2, R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))).First, (V)"path")), new Entry((V)3, R.Get(v_75, (V)"path")), new Entry((V)4, R.Get(v_75, (V)"path")), new Entry((V)5, (V)"assets/kenney/approved/tiny-dungeon.png"), new Entry((V)6, (V)"assets/kenney/approved/roguelike-rpg.png"), new Entry((V)7, (V)"assets/kenney/approved/roguelike-rpg.png"), new Entry((V)8, R.Get(v_75, (V)"path")))));
V v_77 = values_76[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_71 == R.Get(v_77, v_68))}, new Results((V)"unexpected uncached native texture")));
if (R.Truth((V)(v_68 <= (V)6.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_72 == R.Get(R.Table(new Entry((V)1, (V)true), new Entry((V)2, (V)false), new Entry((V)3, (V)false), new Entry((V)4, (V)"caves-surfaces"), new Entry((V)5, (V)true), new Entry((V)6, (V)true)), v_68)))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_72))).First == (V)"table"), () => R.Get(v_72, (V)"environment")), () => (V)(R.Get(v_72, (V)"style") == (V)"kenney")), () => (V)(R.Get(v_72, (V)"sheet") == R.Or(R.And((V)(v_68 == (V)7.0d), () => (V)"rpg"), () => (V)"caves"))))));
}
return R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {v_70, v_71}, new Results(v_72))));
return Results.Empty;
}))}, new Results(v_31))));
V v_79 = values_78[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(v_68 == (V)8.0d), () => (V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_79, (V)"environment"), (V)"failures")))).First)))}, new Results((V)"six native pages plus two cached room-material pages")));
{ double end_82 = R.Num((V)20.0d), step_83 = R.Num((V)1.0d);
for (double i_81 = R.Num((V)1.0d); step_83 > 0 ? i_81 <= end_82 : i_81 >= end_82; i_81 += step_83) {
V v_80 = i_81;
foreach (var row_84 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_79, (V)"handles"))))))) {
V v_85 = row_84[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_79, "resolve", R.Join(new V[] {}, new Results(v_85)))));
}
R.Invoke(v_79, "terrain", R.Join(new V[] {R.Table(new Entry((V)"rows", R.Table(new Entry((V)1, (V)"###"), new Entry((V)2, (V)"###"), new Entry((V)3, (V)"###")))), (V)2.0d, (V)2.0d}, new Results((V)"wall")));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_68 == (V)8.0d)}, new Results((V)"resolution must never allocate another texture")));
return Results.Empty;
})))));
R.Call(v_40, R.Join(new V[] {(V)"Kenney connected walls and water select native edges without revealing unknown cells"}, new Results(new V((RuleFunction)(args_86 => {

var values_87 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_9, v_16}, new Results(v_31))))));
V v_88 = values_87[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_2, (V)"wall_masks")) == (V)47.0d))));
var values_89 = R.Join(new V[] {}, new Results(R.Table()));
V v_90 = values_89[0];
foreach (var row_91 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"wall_masks"))))))) {
V v_92 = row_91[0];
V v_93 = row_91[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_2, (V)"canonical"), R.Join(new V[] {}, new Results(v_93))).First == v_93), () => (V)(!R.Truth(R.Get(v_90, v_93)))))));
var values_94 = R.Join(new V[] {}, new Results((V)true));
var obj_95 = v_90;
V key_96 = v_93;
R.Set(obj_95, key_96, values_94[0]);
var values_97 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_88, (V)"wall_regions"), v_93)))));
V v_98 = values_97[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_98, (V)"quad"), (V)"x") >= (V)0.0d), () => (V)((V)(R.Get(R.Get(v_98, (V)"quad"), (V)"x") + (V)16.0d) <= (V)492.0d)), () => (V)((V)(R.Get(R.Get(v_98, (V)"quad"), (V)"y") + (V)16.0d) <= (V)305.0d)))));
}
{ double end_101 = R.Num((V)255.0d), step_102 = R.Num((V)1.0d);
for (double i_100 = R.Num((V)0.0d); step_102 > 0 ? i_100 <= end_101 : i_100 >= end_101; i_100 += step_102) {
V v_99 = i_100;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_90, R.Call(R.Get(v_2, (V)"canonical"), R.Join(new V[] {}, new Results(v_99))).First))));
}
}
var values_103 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rows", R.Table(new Entry((V)1, (V)"###"), new Entry((V)2, (V)"###"), new Entry((V)3, (V)"###"))))));
V v_104 = values_103[0];
var values_105 = R.Join(new V[] {}, R.Invoke(v_88, "terrain", R.Join(new V[] {v_104, (V)2.0d, (V)2.0d}, new Results((V)"wall"))));
V v_106 = values_105[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_106, (V)"wall_mask") == (V)255.0d))));
var values_107 = R.Join(new V[] {}, new Results((V)"#.#"));
var obj_108 = R.Get(v_104, (V)"rows");
V key_109 = (V)1.0d;
R.Set(obj_108, key_109, values_107[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_88, "terrain", R.Join(new V[] {v_104, (V)2.0d, (V)2.0d}, new Results((V)"wall"))).First, (V)"wall_mask") == (V)110.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(v_88, "terrain", R.Join(new V[] {v_104, (V)2.0d, (V)2.0d, (V)"wall"}, new Results(new V((RuleFunction)(args_110 => {
V v_111 = args_110[0];
V v_112 = args_110[1];
return R.Join(new V[] {}, new Results((V)(v_112 != (V)1.0d)));
return Results.Empty;
}))))).First == v_106))));
var values_113 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"##."), new Entry((V)2, (V)"###"), new Entry((V)3, (V)"###"))));
var obj_114 = v_104;
V key_115 = (V)"rows";
R.Set(obj_114, key_115, values_113[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_88, "terrain", R.Join(new V[] {v_104, (V)2.0d, (V)2.0d}, new Results((V)"wall"))).First, (V)"wall_mask") == (V)239.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Invoke(v_88, "terrain", R.Join(new V[] {v_104, (V)2.0d, (V)2.0d, (V)"wall"}, new Results(new V((RuleFunction)(args_116 => {
V v_117 = args_116[0];
V v_118 = args_116[1];
return R.Join(new V[] {}, new Results(R.Or((V)(v_117 != (V)3.0d), () => (V)(v_118 != (V)1.0d))));
return Results.Empty;
}))))).First == v_106)}, new Results((V)"hidden diagonal changed the visible inner-corner art")));
var values_119 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"~~~"), new Entry((V)2, (V)"~~~"), new Entry((V)3, (V)"~~~"))));
var obj_120 = v_104;
V key_121 = (V)"rows";
R.Set(obj_120, key_121, values_119[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_88, "terrain", R.Join(new V[] {v_104, (V)2.0d, (V)2.0d}, new Results((V)"water"))).First, (V)"water_mask") == (V)255.0d))));
var values_122 = R.Join(new V[] {}, new Results((V)"..."));
var obj_123 = R.Get(v_104, (V)"rows");
V key_124 = (V)1.0d;
R.Set(obj_123, key_124, values_122[0]);

var values_125 = R.Join(new V[] {}, new Results((V)".~~"));
var obj_126 = R.Get(v_104, (V)"rows");
V key_127 = (V)2.0d;
R.Set(obj_126, key_127, values_125[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_88, "terrain", R.Join(new V[] {v_104, (V)2.0d, (V)2.0d}, new Results((V)"water"))).First, (V)"water_mask") == (V)38.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(v_88, "terrain", R.Join(new V[] {v_104, (V)2.0d, (V)2.0d}, new Results((V)"floor"))).First == R.Invoke(v_88, "terrain", R.Join(new V[] {v_104, (V)2.0d, (V)2.0d}, new Results((V)"floor"))).First))));
return Results.Empty;
})))));
R.Call(v_40, R.Join(new V[] {(V)"Caves floors use only native ground, never door frames or wall props"}, new Results(new V((RuleFunction)(args_128 => {

var values_129 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_terrain"))));
V v_130 = values_129[0];
var values_131 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_9, v_16}, new Results(v_31))))));
V v_132 = values_131[0];
foreach (var row_133 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_130, (V)"styles"))))))) {
V v_134 = row_133[0];
V v_135 = row_133[1];
var values_136 = R.Join(new V[] {}, new Results(R.Table()));
V v_137 = values_136[0];

foreach (var row_138 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_135, (V)"floor"))))))) {
V v_139 = row_138[0];
V v_140 = row_138[1];
var values_141 = R.Join(new V[] {}, new Results((V)true));
var obj_142 = v_137;
V key_143 = v_140;
R.Set(obj_142, key_143, values_141[0]);
}
var values_144 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rows", R.Table()), new Entry((V)"biome_id", R.Concat((V)"core:biome/", v_134)))));
V v_145 = values_144[0];
{ double end_148 = R.Num((V)20.0d), step_149 = R.Num((V)1.0d);
for (double i_147 = R.Num((V)1.0d); step_149 > 0 ? i_147 <= end_148 : i_147 >= end_148; i_147 += step_149) {
V v_146 = i_147;
{ double end_152 = R.Num((V)20.0d), step_153 = R.Num((V)1.0d);
for (double i_151 = R.Num((V)1.0d); step_153 > 0 ? i_151 <= end_152 : i_151 >= end_152; i_151 += step_153) {
V v_150 = i_151;
var values_154 = R.Join(new V[] {}, R.Invoke(v_132, "terrain", R.Join(new V[] {v_145, v_150, v_146}, new Results((V)"floor"))));
V v_155 = values_154[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_137, R.Get(v_155, (V)"tile_index")), () => (V)(R.Get(v_155, (V)"terrain_pack") == (V)"caves")), () => (V)(R.Get(v_155, (V)"width") == (V)16.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_155, (V)"quad"), (V)"x") == (V)((V)(R.Get(v_155, (V)"tile_index") % (V)29.0d) * (V)17.0d)), () => (V)(R.Get(R.Get(v_155, (V)"quad"), (V)"y") == (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_155, (V)"tile_index") / (V)29.0d)))).First * (V)17.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_155 == R.Invoke(v_132, "terrain", R.Join(new V[] {v_145, v_150, v_146}, new Results((V)"floor"))).First))));
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_137, R.Get(R.Invoke(v_132, "resolve", R.Join(new V[] {}, new Results(R.Concat((V)"core:terrain/", R.Concat(v_134, (V)"/floor/base"))))).First, (V)"tile_index")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_132, "resolve", R.Join(new V[] {}, new Results((V)"core:object/door_closed"))).First, (V)"tile_index") == (V)36.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_132, "resolve", R.Join(new V[] {}, new Results((V)"core:object/stairs_down"))).First, (V)"tile_index") == (V)37.0d))));
return Results.Empty;
})))));
R.Call(v_40, R.Join(new V[] {(V)"all boons have deliberate native icons and matching details"}, new Results(new V((RuleFunction)(args_156 => {

var values_157 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))));
V v_158 = values_157[0];
var values_159 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_160 = values_159[0];
var values_161 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_9, v_16}, new Results(v_31))))));
V v_162 = values_161[0];
var values_163 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_164 = values_163[0];
V v_165 = values_163[1];
foreach (var row_166 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_160, (V)"order"))))))) {
V v_167 = row_166[0];
V v_168 = row_166[1];
var values_169 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_158, (V)"boons"), v_168)}, new Results(R.Concat((V)"unmapped boon ", v_168)))));
V v_170 = values_169[0];
var values_171 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_162, "resolve", R.Join(new V[] {}, new Results(R.Concat(v_168, (V)"/icon")))))));
V v_172 = values_171[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Invoke(v_162, "resolve", R.Join(new V[] {}, new Results(R.Concat(v_168, (V)"/detail")))).First == v_172), () => (V)(R.Get(v_172, (V)"source") == (V)"kenney")))));
if (R.Truth((V)(v_168 == (V)"core:boon/ring_of_reversal"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_172, (V)"width") == (V)8.0d), () => (V)(R.Get(v_172, (V)"tile_index") == (V)89.0d)), () => (V)(R.Get(R.Get(v_172, (V)"quad"), (V)"pw") == (V)128.0d)))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_172, (V)"width") == (V)16.0d), () => (V)(R.Get(v_172, (V)"height") == (V)16.0d)), () => (V)(R.Get(v_172, (V)"symbol_index") == R.Get(v_170, (V)1.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_164, R.Get(v_172, (V)"symbol_index"))))}, new Results(R.Concat((V)"unintentionally shared boon icon ", v_168))));
var values_173 = R.Join(new V[] {}, new Results((V)true));
var obj_174 = v_164;
V key_175 = R.Get(v_172, (V)"symbol_index");
R.Set(obj_174, key_175, values_173[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_172, (V)"quad"), (V)"x") == (V)((V)(R.Get(v_172, (V)"symbol_index") % (V)49.0d) * (V)16.0d)), () => (V)(R.Get(R.Get(v_172, (V)"quad"), (V)"y") == (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_172, (V)"symbol_index") / (V)49.0d)))).First * (V)16.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_172, (V)"quad"), (V)"pw") == (V)784.0d), () => (V)(R.Get(R.Get(v_172, (V)"quad"), (V)"ph") == (V)352.0d)), () => (V)(R.Get(v_172, (V)"motif") != (V)"")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_172, (V)"tile_inset"))), () => (V)(!R.Truth(R.Get(v_172, (V)"ground")))))));
foreach (var row_176 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_172, (V)"tint"))))))) {
V v_177 = row_176[0];
V v_178 = row_176[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_178 >= (V)0.0d), () => (V)(v_178 <= (V)1.0d)))));
}
}
var values_179 = R.Join(new V[] {}, new Results((V)(v_165 + (V)1.0d)));
v_165 = values_179[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_165 == (V)R.Length(R.Get(v_160, (V)"order"))))));
foreach (var row_180 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_158, (V)"boons"))))))) {
V v_181 = row_180[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_160, (V)"definitions"), v_181)}, new Results((V)"orphan boon mapping")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(v_162, "resolve", R.Join(new V[] {}, new Results((V)"core:boon/unknown/icon"))).First == V.Nil))));
return Results.Empty;
})))));
R.Call(v_40, R.Join(new V[] {(V)"all six mechanical terrains have legible native art with exact integer inset scaling"}, new Results(new V((RuleFunction)(args_182 => {

var values_183 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_184 = values_183[0];
var values_185 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))));
V v_186 = values_185[0];
var values_187 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_9, v_16}, new Results(v_31))))));
V v_188 = values_187[0];
var values_189 = R.Join(new V[] {}, new Results(R.Table()));
V v_190 = values_189[0];
foreach (var row_191 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_184, (V)"order"))))))) {
V v_192 = row_191[0];
V v_193 = row_191[1];
var values_194 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_184, (V)"get"), R.Join(new V[] {}, new Results(v_193))).First, (V)"id")));
V v_195 = values_194[0];
var values_196 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Invoke(v_188, "resolve", R.Join(new V[] {}, new Results(v_195))).First}, new Results(R.Concat((V)"unmapped terrain ", v_195)))));
V v_197 = values_196[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_197, (V)"symbol_index") == R.Get(R.Get(R.Get(v_186, (V)"terrain"), v_195), (V)1.0d)), () => (V)(R.Get(v_197, (V)"width") == (V)16.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)((V)((V)40.0d - (V)(R.Get(v_197, (V)"tile_inset") * (V)2.0d)) / R.Get(v_197, (V)"width")) == (V)2.0d), () => (V)(!R.Truth(R.Get(v_190, R.Get(v_197, (V)"symbol_index"))))))));
var values_198 = R.Join(new V[] {}, new Results((V)true));
var obj_199 = v_190;
V key_200 = R.Get(v_197, (V)"symbol_index");
R.Set(obj_199, key_200, values_198[0]);
foreach (var row_201 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_197, (V)"ground"))))))) {
V v_202 = row_201[0];
V v_203 = row_201[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_203 >= (V)0.0d), () => (V)(v_203 < (V)1.0d)))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_188, "resolve", R.Join(new V[] {}, new Results((V)"core:terrain/obscuring_mist"))).First, (V)"motif") == (V)"layered wisps"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_188, "resolve", R.Join(new V[] {}, new Results((V)"core:terrain/barricade"))).First, (V)"motif") == (V)"cross-braced crate"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Invoke(v_188, "resolve", R.Join(new V[] {}, new Results((V)"core:terrain/glazed_ice"))).First, (V)"motif") == (V)"ice star"))));
return Results.Empty;
})))));
R.Call(v_40, R.Join(new V[] {(V)"Kenney supplement rejects missing, corrupt, wrong-sized and out-of-range inputs"}, new Results(new V((RuleFunction)(args_204 => {

var values_205 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))));
V v_206 = values_205[0];
foreach (var row_207 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"missing"), new Entry((V)2, (V)"corrupt")))))))) {
V v_208 = row_207[0];
V v_209 = row_207[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new"), new V((RuleFunction)(args_210 => {
V v_211 = args_210[0];
if (R.Truth((V)(v_211 == R.Get(v_206, (V)"path")))) {
return R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_209 == (V)"corrupt"), () => (V)"broken"), () => V.Nil)));
}
return R.Join(new V[] {}, R.Call(v_9, R.Join(new V[] {}, new Results(v_211))));
return Results.Empty;
})), v_16}, new Results(v_31))).First)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_206, (V)"new"), v_9, new V((RuleFunction)(args_212 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_213 => {

return R.Join(new V[] {}, new Results((V)783.0d));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_214 => {

return R.Join(new V[] {}, new Results((V)352.0d));
return Results.Empty;
}))))));
return Results.Empty;
}))}, new Results(v_31))).First)))));
var values_215 = R.Join(new V[] {}, R.Call(R.Get(v_206, (V)"region"), R.Join(new V[] {}, new Results((V)1077.0d))));
V v_216 = values_215[0];
V v_217 = values_215[1];
V v_218 = values_215[2];
V v_219 = values_215[3];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_216 == (V)768.0d), () => (V)(v_217 == (V)336.0d)), () => (V)(v_218 == (V)16.0d)), () => (V)(v_219 == (V)16.0d)))));
foreach (var row_220 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1078.0d), new Entry((V)3, (V)1.5d), new Entry((V)4, (V)"1")))))))) {
V v_221 = row_220[0];
V v_222 = row_220[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_206, (V)"region")}, new Results(v_222))).First)))));
}
return Results.Empty;
})))));
R.Call(v_40, R.Join(new V[] {(V)"historical mixed fixture cannot be selected by the fixed game resolver"}, new Results(new V((RuleFunction)(args_223 => {

var values_224 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"new_stage"), R.Join(new V[] {}, new Results((V)7.0d))));
V v_225 = values_224[0];

var values_226 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_225))));
V v_227 = values_226[0];
var values_228 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_9, v_16}, new Results(v_31))))));
V v_229 = values_228[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_4, (V)"build"), v_9, v_16}, new Results(new V((RuleFunction)(args_230 => {

return R.Join(new V[] {v_229}, new Results((V)"loaded"));
return Results.Empty;
}))))).First)))));
var values_231 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_visuals"))).First, (V)"new"), R.Join(new V[] {v_9, v_16}, new Results(v_31))));
V v_232 = values_231[0];
var values_234 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"build"), R.Join(new V[] {v_9, v_16}, new Results(new V((RuleFunction)(args_233 => {

return R.Join(new V[] {v_232}, new Results((V)"loaded"));
return Results.Empty;
}))))));
V v_235 = values_234[0];
V v_236 = values_234[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_236 == (V)"loaded"), () => (V)(R.Get(R.Invoke(v_235, "resolve", R.Join(new V[] {}, new Results((V)"core:actor/adventurer"))).First, (V)"source") == (V)"onebit")))));
R.Call(v_41, R.Join(new V[] {v_227}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_225)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
