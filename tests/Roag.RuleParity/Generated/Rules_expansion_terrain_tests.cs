// Static C# port of tests/expansion_terrain_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: d3d703475d72d9cd916330e4c00b3ba71c07cc31541fd151ffc701b2797c6a17
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_expansion_terrain_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_world"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_14 = values_13[0];
V v_15 = V.Nil;
v_15 = new V((RuleFunction)(args_16 => {
V v_17 = args_16[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_17))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_17));
}
var values_18 = R.Join(new V[] {}, new Results(R.Table()));
V v_19 = values_18[0];

foreach (var row_20 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_17)))))) {
V v_21 = row_20[0];
V v_22 = row_20[1];
var values_23 = R.Join(new V[] {}, R.Call(v_15, R.Join(new V[] {}, new Results(v_22))));
var obj_24 = v_19;
V key_25 = v_21;
R.Set(obj_24, key_25, values_23[0]);
}

return R.Join(new V[] {}, new Results(v_19));
return Results.Empty;
}));
V v_26 = V.Nil;
v_26 = new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
V v_29 = args_27[1];
V v_30 = args_27[2];
var values_31 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {}, new Results((V)11.0d))));
V v_32 = values_31[0];
var values_33 = R.Join(new V[] {}, new Results((V)false));
var obj_34 = R.Get(v_32, (V)"meta");
V key_35 = (V)"eligible";
R.Set(obj_34, key_35, values_33[0]);
var values_36 = R.Join(new V[] {}, R.Call(v_15, R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/test_room"))))));
V v_37 = values_36[0];
var values_38 = R.Join(new V[] {}, new Results(v_37));
var obj_39 = v_32;
V key_40 = (V)"generated_map";
R.Set(obj_39, key_40, values_38[0]);
if (R.Truth(v_28)) {
var values_41 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_37, (V)"rows"), v_30), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_29 - (V)1.0d)))).First, R.Concat(v_28, R.Invoke(R.Get(R.Get(v_37, (V)"rows"), v_30), "sub", R.Join(new V[] {}, new Results((V)(v_29 + (V)1.0d)))).First))));
var obj_42 = R.Get(v_37, (V)"rows");
V key_43 = v_30;
R.Set(obj_42, key_43, values_41[0]);
}
return R.Join(new V[] {v_32}, new Results(v_37));
return Results.Empty;
}));
V v_44 = V.Nil;
v_44 = new V((RuleFunction)(args_45 => {
V v_46 = args_45[0];
V v_47 = args_45[1];
var values_48 = R.Join(new V[] {}, new Results((V)0.0d));
V v_49 = values_48[0];

foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_46)))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
if (R.Truth((V)(R.Get(v_52, (V)"kind") == v_47))) {
var values_53 = R.Join(new V[] {}, new Results((V)(v_49 + (V)1.0d)));
v_49 = values_53[0];
}
}
return R.Join(new V[] {}, new Results(v_49));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
R.Call(v_55, R.Join(new V[] {(V)"six mechanical terrain symbols have separate collision and sight semantics"}, new Results(new V((RuleFunction)(args_57 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_8, (V)"validate"), Results.Empty).First, () => (V)((V)R.Length(R.Get(v_8, (V)"order")) == (V)6.0d)))));
var values_58 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"b", R.Table(new Entry((V)1, (V)true), new Entry((V)2, (V)false), new Entry((V)3, (V)true))), new Entry((V)"g", R.Table(new Entry((V)1, (V)true), new Entry((V)2, (V)false), new Entry((V)3, (V)true))), new Entry((V)"i", R.Table(new Entry((V)1, (V)true), new Entry((V)2, (V)false), new Entry((V)3, (V)true))), new Entry((V)"B", R.Table(new Entry((V)1, (V)false), new Entry((V)2, (V)true), new Entry((V)3, (V)false))), new Entry((V)"m", R.Table(new Entry((V)1, (V)true), new Entry((V)2, (V)true), new Entry((V)3, (V)true))), new Entry((V)"c", R.Table(new Entry((V)1, (V)true), new Entry((V)2, (V)false), new Entry((V)3, (V)true))))));
V v_59 = values_58[0];
foreach (var row_60 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_59)))))) {
V v_61 = row_60[0];
V v_62 = row_60[1];
var values_63 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {v_61, (V)3.0d}, new Results((V)2.0d))));
V v_64 = values_63[0];
V v_65 = values_63[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"walkable"), R.Join(new V[] {v_65, (V)3.0d}, new Results((V)2.0d))).First == R.Get(v_62, (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"blocks_vision"), R.Join(new V[] {v_65, (V)3.0d}, new Results((V)2.0d))).First == R.Get(v_62, (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_65, (V)3.0d, (V)2.0d}, new Results(v_64))).First == R.Get(v_62, (V)3.0d)))));
}
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"paired generated floors place all six terrain mechanics without affecting combat RNG"}, new Results(new V((RuleFunction)(args_66 => {

foreach (var row_67 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_14, (V)"order"))))))) {
V v_68 = row_67[0];
V v_69 = row_67[1];
{ double end_72 = R.Num((V)100.0d), step_73 = R.Num((V)1.0d);
for (double i_71 = R.Num((V)1.0d); step_73 > 0 ? i_71 <= end_72 : i_71 >= end_72; i_71 += step_73) {
V v_70 = i_71;
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"generate_pair"), R.Join(new V[] {v_70}, new Results(R.Table(new Entry((V)"biome_id", v_69))))));
V v_75 = values_74[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"validate_pair"), R.Join(new V[] {}, new Results(v_75)))));
{ double end_78 = R.Num((V)2.0d), step_79 = R.Num((V)1.0d);
for (double i_77 = R.Num((V)1.0d); step_79 > 0 ? i_77 <= end_78 : i_77 >= end_78; i_77 += step_79) {
V v_76 = i_77;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_75, v_76), (V)"terrain_placements")) == (V)3.0d))));
foreach (var row_80 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_75, v_76), (V)"terrain_placements"))))))) {
V v_81 = row_80[0];
V v_82 = row_80[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"symbol"), R.Join(new V[] {R.Get(v_75, v_76), R.Get(v_82, (V)"x")}, new Results(R.Get(v_82, (V)"y")))).First == R.Get(v_82, (V)"symbol")))));
}
}
}
}
}
}
var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_84 = values_83[0];
var values_85 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_86 = values_85[0];
R.Call(v_56, R.Join(new V[] {R.Get(v_84, (V)"rng")}, new Results(R.Get(v_86, (V)"rng"))));
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"bramble entry deals one canonical damage once per action and idle is safe"}, new Results(new V((RuleFunction)(args_87 => {

var values_88 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"b", (V)3.0d}, new Results((V)2.0d))));
V v_89 = values_88[0];
var values_90 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_89))));
V v_91 = values_90[0];
var values_92 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_89}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_93 = values_92[0];
V v_94 = values_92[1];
V v_95 = values_92[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_93, () => (V)(R.Get(v_91, (V)"hp") == (V)23.0d)), () => (V)(R.Call(v_44, R.Join(new V[] {v_95}, new Results((V)"TerrainTriggered"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_44, R.Join(new V[] {v_95}, new Results((V)"DamageTaken"))).First == (V)1.0d))));
var values_96 = R.Join(new V[] {}, new Results(R.Get(v_91, (V)"hp")));
V v_97 = values_96[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_89}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_91, (V)"hp") == v_97))));
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"bog increases player recovery while ice extends only legal Dash traversal"}, new Results(new V((RuleFunction)(args_98 => {

var values_99 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"g", (V)3.0d}, new Results((V)2.0d))));
V v_100 = values_99[0];
var values_101 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_100, (V)"move", (V)1.0d}, new Results((V)0.0d))));
V v_102 = values_101[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_102, (V)"valid"), () => (V)(R.Get(v_102, (V)"cost") == (V)140.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_100}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_100, (V)"clock") == (V)140.0d))));
var values_103 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"i", (V)3.0d}, new Results((V)2.0d))));
V v_104 = values_103[0];
var values_105 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_104, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_106 = values_105[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_106, (V)"valid"), () => (V)(R.Get(v_106, (V)"distance") == (V)3.0d)), () => (V)(R.Get(v_106, (V)"cost") == (V)130.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_104}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_104))).First, (V)"position"), (V)"x") == (V)5.0d), () => (V)(R.Get(v_104, (V)"clock") == (V)130.0d)))));
var values_107 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"i", (V)3.0d}, new Results((V)2.0d))));
V v_108 = values_107[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_108, (V)"move", (V)1.0d}, new Results((V)0.0d))).First, (V)"distance") == (V)1.0d))));
var values_109 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"i", (V)3.0d}, new Results((V)2.0d))));
V v_110 = values_109[0];
V v_111 = values_109[1];
var values_112 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_111, (V)"rows"), (V)2.0d), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)3.0d))).First, R.Concat((V)"#", R.Invoke(R.Get(R.Get(v_111, (V)"rows"), (V)2.0d), "sub", R.Join(new V[] {}, new Results((V)5.0d))).First))));
var obj_113 = R.Get(v_111, (V)"rows");
V key_114 = (V)2.0d;
R.Set(obj_113, key_114, values_112[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_110, (V)"dash", (V)1.0d}, new Results((V)0.0d))).First, (V)"distance") == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"enemy bramble damage resolves at its move time and bog delays its next readiness"}, new Results(new V((RuleFunction)(args_115 => {

var values_116 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"b", (V)3.0d}, new Results((V)2.0d))));
V v_117 = values_116[0];
var values_118 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_117, (V)"core:enemy/mossbound_guard", (V)4.0d, (V)2.0d}, new Results((V)0.0d))));
V v_119 = values_118[0];
var values_120 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_121 = v_119;
V key_122 = (V)"defense";
R.Set(obj_121, key_122, values_120[0]);
var values_123 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_117}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_124 = values_123[0];
V v_125 = values_123[1];
V v_126 = values_123[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_124, () => (V)(R.Get(R.Get(v_119, (V)"position"), (V)"x") == (V)3.0d)), () => (V)(R.Get(v_119, (V)"hp") == (V)9.0d)))));
var values_127 = Results.Empty;
V v_128 = values_127[0];
foreach (var row_129 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_126)))))) {
V v_130 = row_129[0];
V v_131 = row_129[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_131, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_131, (V)"damage_type") == (V)"bramble")), () => (V)(R.Get(v_131, (V)"target_id") == R.Get(v_119, (V)"id"))))) {
var values_132 = R.Join(new V[] {}, new Results(R.Get(v_131, (V)"time")));
v_128 = values_132[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_128 == (V)0.0d), () => (V)(R.Get(v_117, (V)"clock") == (V)100.0d)))));
var values_133 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"g", (V)3.0d}, new Results((V)2.0d))));
V v_134 = values_133[0];
var values_135 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_134, (V)"core:enemy/mossbound_guard", (V)4.0d, (V)2.0d}, new Results((V)0.0d))));
V v_136 = values_135[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_134}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_136, (V)"position"), (V)"x") == (V)3.0d))));
var values_137 = Results.Empty;
V v_138 = values_137[0];
foreach (var row_139 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_134, (V)"schedule"))))))) {
V v_140 = row_139[0];
V v_141 = row_139[1];
if (R.Truth(R.And((V)(R.Get(v_141, (V)"kind") == (V)"actor_ready"), () => (V)(R.Get(v_141, (V)"actor_id") == R.Get(v_136, (V)"id"))))) {
var values_142 = R.Join(new V[] {}, new Results(R.Get(v_141, (V)"due")));
v_138 = values_142[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_138 == R.Call(R.Get(v_2, (V)"effective_cost"), R.Join(new V[] {(V)140.0d}, new Results(R.Get(v_136, (V)"speed")))).First))));
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"attacking a barricade clears authoritative collision and emits one break"}, new Results(new V((RuleFunction)(args_143 => {

var values_144 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"B", (V)3.0d}, new Results((V)2.0d))));
V v_145 = values_144[0];
V v_146 = values_144[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_145, (V)"move", (V)1.0d}, new Results((V)0.0d))).First, (V)"valid"))))));
var values_147 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_145}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_148 = values_147[0];
V v_149 = values_147[1];
V v_150 = values_147[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_148, () => (V)(R.Call(R.Get(v_8, (V)"symbol"), R.Join(new V[] {v_146, (V)3.0d}, new Results((V)2.0d))).First == (V)".")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_44, R.Join(new V[] {v_150}, new Results((V)"TerrainBroken"))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_4, (V)"walkable"), R.Join(new V[] {v_146, (V)3.0d}, new Results((V)2.0d))).First, () => (V)(!R.Truth(R.Call(R.Get(v_4, (V)"blocks_vision"), R.Join(new V[] {v_146, (V)3.0d}, new Results((V)2.0d))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_145, (V)"move", (V)1.0d}, new Results((V)0.0d))).First, (V)"valid"))));
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"generated barricade break survives a concrete two-floor snapshot"}, new Results(new V((RuleFunction)(args_151 => {

var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_153 = values_152[0];
var values_154 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_153, (V)"generated_map"), (V)"placements"), (V)"stairs"), (V)1.0d)));
V v_155 = values_154[0];
var values_156 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_153))));
V v_157 = values_156[0];
var values_158 = Results.Empty;
V v_159 = values_158[0];
V v_160 = values_158[1];
foreach (var row_161 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_162 = row_161[0];
V v_163 = row_161[1];
var values_164 = R.Join(new V[] {(V)(R.Get(v_155, (V)"x") - R.Get(v_163, (V)1.0d))}, new Results((V)(R.Get(v_155, (V)"y") - R.Get(v_163, (V)2.0d))));
V v_165 = values_164[0];
V v_166 = values_164[1];
var values_167 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"walkable"), R.Join(new V[] {R.Get(v_153, (V)"generated_map"), v_165}, new Results(v_166))));
V v_168 = values_167[0];
foreach (var row_169 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_153, (V)"entities"))))))) {
V v_170 = row_169[0];
V v_171 = row_169[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_171, (V)"id") != R.Get(v_157, (V)"id")), () => R.Get(v_171, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_171, (V)"position"), (V)"floor_id") == R.Get(v_153, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_171, (V)"position"), (V)"x") == v_165)), () => (V)(R.Get(R.Get(v_171, (V)"position"), (V)"y") == v_166)))) {
var values_172 = R.Join(new V[] {}, new Results((V)false));
v_168 = values_172[0];
}
}
if (R.Truth(v_168)) {
var values_173 = R.Join(new V[] {v_165}, new Results(v_166));
var obj_174 = R.Get(v_157, (V)"position");
V key_175 = (V)"x";
var obj_176 = R.Get(v_157, (V)"position");
V key_177 = (V)"y";
R.Set(obj_174, key_175, values_173[0]);
R.Set(obj_176, key_177, values_173[1]);

var values_178 = R.Join(new V[] {R.Get(v_163, (V)1.0d)}, new Results(R.Get(v_163, (V)2.0d)));
v_159 = values_178[0];
v_160 = values_178[1];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_159)));
var values_179 = R.Join(new V[] {}, new Results((V)false));
var obj_180 = R.Get(v_153, (V)"meta");
V key_181 = (V)"eligible";
R.Set(obj_180, key_181, values_179[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_153}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_159), new Entry((V)"dy", v_160)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_153, (V)"active_floor_index") == (V)2.0d))));
var values_182 = Results.Empty;
V v_183 = values_182[0];
foreach (var row_184 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_153, (V)"generated_map"), (V)"terrain_placements"))))))) {
V v_185 = row_184[0];
V v_186 = row_184[1];
if (R.Truth((V)(R.Get(v_186, (V)"symbol") == (V)"B"))) {
var values_187 = R.Join(new V[] {}, new Results(v_186));
v_183 = values_187[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_183)));
var values_188 = Results.Empty;
V v_189 = values_188[0];
V v_190 = values_188[1];
foreach (var row_191 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_192 = row_191[0];
V v_193 = row_191[1];
var values_194 = R.Join(new V[] {(V)(R.Get(v_183, (V)"x") - R.Get(v_193, (V)1.0d))}, new Results((V)(R.Get(v_183, (V)"y") - R.Get(v_193, (V)2.0d))));
V v_195 = values_194[0];
V v_196 = values_194[1];
if (R.Truth(R.Call(R.Get(v_4, (V)"walkable"), R.Join(new V[] {R.Get(v_153, (V)"generated_map"), v_195}, new Results(v_196))).First)) {
var values_197 = R.Join(new V[] {v_195}, new Results(v_196));
v_189 = values_197[0];
v_190 = values_197[1];

var values_198 = R.Join(new V[] {R.Get(v_193, (V)1.0d)}, new Results(R.Get(v_193, (V)2.0d)));
v_159 = values_198[0];
v_160 = values_198[1];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_189)));
var values_199 = R.Join(new V[] {v_189}, new Results(v_190));
var obj_200 = R.Get(v_157, (V)"position");
V key_201 = (V)"x";
var obj_202 = R.Get(v_157, (V)"position");
V key_203 = (V)"y";
R.Set(obj_200, key_201, values_199[0]);
R.Set(obj_202, key_203, values_199[1]);
var values_204 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_153}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_159), new Entry((V)"dy", v_160))))));
V v_205 = values_204[0];
V v_206 = values_204[1];
V v_207 = values_204[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_205, () => (V)(R.Call(v_44, R.Join(new V[] {v_207}, new Results((V)"TerrainBroken"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"symbol"), R.Join(new V[] {R.Get(v_153, (V)"generated_map"), R.Get(v_183, (V)"x")}, new Results(R.Get(v_183, (V)"y")))).First == (V)"."))));
var values_208 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_153))))));
V v_209 = values_208[0];
R.Call(v_56, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_153))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_209)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"validate"), R.Join(new V[] {R.Get(v_209, (V)"generated_map")}, new Results(R.Get(v_209, (V)"secrets"))))));
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"pre-terrain connected-floor snapshots remain loadable as stored maps"}, new Results(new V((RuleFunction)(args_210 => {

var values_211 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rooms"))));
V v_212 = values_211[0];
var values_213 = R.Join(new V[] {}, new Results(R.Get(v_212, (V)"order")));
V v_214 = values_213[0];
var values_215 = R.Join(new V[] {}, new Results(R.Table()));
var obj_216 = v_212;
V key_217 = (V)"order";
R.Set(obj_216, key_217, values_215[0]);
{ double end_220 = R.Num((V)6.0d), step_221 = R.Num((V)1.0d);
for (double i_219 = R.Num((V)1.0d); step_221 > 0 ? i_219 <= end_220 : i_219 >= end_220; i_219 += step_221) {
V v_218 = i_219;
var values_222 = R.Join(new V[] {}, new Results(R.Get(v_214, v_218)));
var obj_223 = R.Get(v_212, (V)"order");
V key_224 = v_218;
R.Set(obj_223, key_224, values_222[0]);
}
}
var values_225 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"new_stage"), (V)29.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_226 = values_225[0];
V v_227 = values_225[1];
var values_228 = R.Join(new V[] {}, new Results(v_214));
var obj_229 = v_212;
V key_230 = (V)"order";
R.Set(obj_229, key_230, values_228[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {v_226}, new Results(v_227)));
var values_231 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_227))));
V v_232 = values_231[0];
var values_233 = R.Join(new V[] {}, new Results((V)"roag-rooms/2"));
var obj_234 = v_232;
V key_235 = (V)"room_content_version";
R.Set(obj_234, key_235, values_233[0]);
var values_236 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_232, (V)"generated_map")))));
V v_237 = values_236[0];
foreach (var row_238 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_232, (V)"inactive_floor_maps"))))))) {
V v_239 = row_238[0];
V v_240 = row_238[1];
var values_241 = R.Join(new V[] {}, new Results(v_240));
var obj_242 = v_237;
V key_243 = (V)((V)R.Length(v_237) + (V)1.0d);
R.Set(obj_242, key_243, values_241[0]);
}
foreach (var row_244 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_237)))))) {
V v_245 = row_244[0];
V v_246 = row_244[1];
var values_247 = R.Join(new V[] {}, new Results(V.Nil));
var obj_248 = v_246;
V key_249 = (V)"terrain_placements";
R.Set(obj_248, key_249, values_247[0]);
var values_250 = R.Join(new V[] {}, new Results(V.Nil));
var obj_251 = v_246;
V key_252 = (V)"breakable_origins";
R.Set(obj_251, key_252, values_250[0]);
var values_253 = R.Join(new V[] {}, new Results(V.Nil));
var obj_254 = v_246;
V key_255 = (V)"broken_barricades";
R.Set(obj_254, key_255, values_253[0]);
foreach (var row_256 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_246, (V)"rows"))))))) {
V v_257 = row_256[0];
V v_258 = row_256[1];
var values_259 = R.Join(new V[] {}, R.Invoke(v_258, "gsub", R.Join(new V[] {(V)"[bgiBmc]"}, new Results((V)"."))));
var obj_260 = R.Get(v_246, (V)"rows");
V key_261 = v_257;
R.Set(obj_260, key_261, values_259[0]);
}
}
var values_262 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, new Results(v_232))));
V v_263 = values_262[0];
R.Call(v_56, R.Join(new V[] {v_232}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_263)))));
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"mist stops ranged AI sight while remaining projectile-passable"}, new Results(new V((RuleFunction)(args_264 => {

var values_265 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"m", (V)2.0d}, new Results((V)4.0d))));
V v_266 = values_265[0];
V v_267 = values_265[1];
var values_268 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_266, (V)"core:enemy/thornspitter", (V)2.0d, (V)5.0d}, new Results((V)100000.0d))));
V v_269 = values_268[0];
var values_270 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"choose"), R.Join(new V[] {v_266}, new Results(v_269))));
V v_271 = values_270[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_271, (V)"kind") != (V)"windup"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_267, (V)2.0d, (V)4.0d}, new Results(v_266)))));
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"resonant crystal arcs credited lightning through canonical damage once"}, new Results(new V((RuleFunction)(args_272 => {

var values_273 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"c", (V)4.0d}, new Results((V)3.0d))));
V v_274 = values_273[0];
V v_275 = values_273[1];
var values_276 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_274, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)100000.0d))));
V v_277 = values_276[0];
var values_278 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_274, (V)"core:enemy/ruin_skitter", (V)4.0d, (V)2.0d}, new Results((V)100000.0d))));
V v_279 = values_278[0];
var values_280 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_274, (V)"core:enemy/ruin_skitter", (V)4.0d, (V)4.0d}, new Results((V)100000.0d))));
V v_281 = values_280[0];
R.Call(R.Get(v_2, (V)"set_boon_stacks"), R.Join(new V[] {v_274, R.Get(v_274, (V)"player_id"), (V)"core:boon/storm_conductor", (V)"legendary"}, new Results((V)2.0d)));
var values_282 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_274}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_283 = values_282[0];
V v_284 = values_282[1];
V v_285 = values_282[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_283, () => (V)(R.Get(v_277, (V)"hp") < R.Get(v_277, (V)"max_hp"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_274}, new Results(R.Get(v_279, (V)"id")))).First == V.Nil))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_281, (V)"hp") == (V)(R.Get(v_281, (V)"max_hp") - (V)1.0d)))));
var values_286 = R.Join(new V[] {}, new Results((V)0.0d));
V v_287 = values_286[0];
foreach (var row_288 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_285)))))) {
V v_289 = row_288[0];
V v_290 = row_288[1];
if (R.Truth(R.And((V)(R.Get(v_290, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_290, (V)"damage_type") == (V)"crystal_arc")))) {
var values_291 = R.Join(new V[] {}, new Results((V)(v_287 + (V)1.0d)));
v_287 = values_291[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_290, (V)"source_id") == R.Get(v_274, (V)"player_id")), () => (V)(R.Get(v_290, (V)"target_id") == R.Get(v_281, (V)"id"))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_287 == (V)1.0d), () => (V)(R.Call(v_44, R.Join(new V[] {v_285}, new Results((V)"TerrainTriggered"))).First == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"crystal secondary lethal awards one ordinary kill and drop"}, new Results(new V((RuleFunction)(args_292 => {

var values_293 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {(V)"c", (V)4.0d}, new Results((V)3.0d))));
V v_294 = values_293[0];
R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_294, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)100000.0d)));
R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_294, (V)"core:enemy/ruin_skitter", (V)4.0d, (V)2.0d}, new Results((V)100000.0d)));
var values_295 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_294, (V)"core:enemy/ruin_skitter", (V)4.0d, (V)4.0d}, new Results((V)100000.0d))));
V v_296 = values_295[0];
var values_297 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_298 = v_296;
V key_299 = (V)"hp";
R.Set(obj_298, key_299, values_297[0]);
R.Call(R.Get(v_2, (V)"set_boon_stacks"), R.Join(new V[] {v_294, R.Get(v_294, (V)"player_id"), (V)"core:boon/storm_conductor", (V)"legendary"}, new Results((V)2.0d)));
var values_300 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_294}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_301 = values_300[0];
V v_302 = values_300[1];
V v_303 = values_300[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_301, () => (V)(R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_294}, new Results(R.Get(v_296, (V)"id")))).First == V.Nil)))));
var values_304 = R.Join(new V[] {(V)0.0d, (V)0.0d}, new Results((V)0.0d));
V v_305 = values_304[0];
V v_306 = values_304[1];
V v_307 = values_304[2];
foreach (var row_308 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_303)))))) {
V v_309 = row_308[0];
V v_310 = row_308[1];
if (R.Truth(R.And((V)(R.Get(v_310, (V)"target_id") == R.Get(v_296, (V)"id")), () => (V)(R.Get(v_310, (V)"kind") == (V)"EntityKilled")))) {
var values_311 = R.Join(new V[] {}, new Results((V)(v_305 + (V)1.0d)));
v_305 = values_311[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_310, (V)"source_id") == R.Get(v_294, (V)"player_id")), () => (V)(R.Get(v_310, (V)"damage_type") == (V)"crystal_arc")))));
} else if (R.Truth(R.And((V)(R.Get(v_310, (V)"target_id") == R.Get(v_296, (V)"id")), () => (V)(R.Get(v_310, (V)"kind") == (V)"ExperienceGained")))) {
var values_312 = R.Join(new V[] {}, new Results((V)(v_306 + (V)1.0d)));
v_306 = values_312[0];
} else if (R.Truth(R.And((V)(R.Get(v_310, (V)"target_id") == R.Get(v_296, (V)"id")), () => (V)(R.Get(v_310, (V)"kind") == (V)"CurrencyDropped")))) {
var values_313 = R.Join(new V[] {}, new Results((V)(v_307 + (V)1.0d)));
v_307 = values_313[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_305 == (V)1.0d), () => (V)(v_306 == (V)1.0d)), () => (V)(v_307 == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_55, R.Join(new V[] {(V)"Studio opts into six terrain tiles without altering older three-tile projects"}, new Results(new V((RuleFunction)(args_314 => {

var values_315 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"new"), R.Join(new V[] {}, new Results((V)"terrain-test"))));
V v_316 = values_315[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_316, (V)"tiles")) == (V)3.0d))));
R.Call(R.Get(v_10, (V)"add_terrain_tiles"), R.Join(new V[] {}, new Results(v_316)));
R.Call(R.Get(v_10, (V)"add_terrain_tiles"), R.Join(new V[] {}, new Results(v_316)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_316, (V)"tiles")) == (V)9.0d))));
var values_317 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"new_room"), R.Join(new V[] {(V)"studio:room/terrain", (V)"Terrain", (V)9.0d}, new Results((V)7.0d))));
V v_318 = values_317[0];
var values_319 = R.Join(new V[] {}, new Results(v_318));
var obj_320 = R.Get(v_316, (V)"rooms");
V key_321 = (V)1.0d;
R.Set(obj_320, key_321, values_319[0]);
R.Call(R.Get(v_10, (V)"paint_tile"), R.Join(new V[] {v_316, v_318, (V)3.0d, (V)3.0d}, new Results((V)"studio:tile/bramble_bed")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(R.Get(v_318, (V)"rows"), (V)3.0d), "sub", R.Join(new V[] {(V)3.0d}, new Results((V)3.0d))).First == (V)"b"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"validate"), R.Join(new V[] {}, new Results(v_316)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
