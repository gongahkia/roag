// Static C# port of tests/expansion_biome_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 2bc2712995d77347dae57d78b03b43219aa0c88d3aae079aee0259e183642d8f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_expansion_biome_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rooms"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_12 = values_11[0];
V v_13 = V.Nil;
v_13 = new V((RuleFunction)(args_14 => {
V v_15 = args_14[0];
var values_16 = R.Join(new V[] {}, R.Invoke(v_15, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))));
V v_17 = values_16[0];
return R.Join(new V[] {}, new Results(v_17));
return Results.Empty;
}));
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
V v_22 = args_19[2];
V v_23 = args_19[3];
return R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new_stage"), R.Join(new V[] {v_20}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"root_seed", v_20), new Entry((V)"stage_options", R.Table(new Entry((V)"index", v_21), new Entry((V)"biome_id", v_22), new Entry((V)"route_history", v_23))))))));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
V v_26 = args_24[1];
R.Call(v_25, R.Join(new V[] {(V)"seven biome identities have separate strategies and authored template families"}, new Results(new V((RuleFunction)(args_27 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_2, (V)"validate"), Results.Empty).First, () => (V)((V)R.Length(R.Get(v_2, (V)"order")) == (V)7.0d)))));
var values_28 = R.Join(new V[] {R.Table(), R.Table()}, new Results(R.Table()));
V v_29 = values_28[0];
V v_30 = values_28[1];
V v_31 = values_28[2];
foreach (var row_32 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_33 = row_32[0];
V v_34 = row_32[1];
var values_35 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_34))))));
V v_36 = values_35[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(v_29, R.Get(v_36, (V)"strategy")))), () => (V)(!R.Truth(R.Get(v_30, R.Get(v_36, (V)"room_prefix"))))), () => (V)(!R.Truth(R.Get(v_31, R.Get(v_36, (V)"map_id"))))))));
var values_37 = R.Join(new V[] {(V)true, (V)true}, new Results((V)true));
var obj_38 = v_29;
V key_39 = R.Get(v_36, (V)"strategy");
var obj_40 = v_30;
V key_41 = R.Get(v_36, (V)"room_prefix");
var obj_42 = v_31;
V key_43 = R.Get(v_36, (V)"map_id");
R.Set(obj_38, key_39, values_37[0]);
R.Set(obj_40, key_41, values_37[1]);
R.Set(obj_42, key_43, values_37[2]);
foreach (var row_44 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"start"), new Entry((V)2, (V)"combat"), new Entry((V)3, (V)"treasure"), new Entry((V)4, (V)"landmark"), new Entry((V)5, (V)"elite")))))))) {
V v_45 = row_44[0];
V v_46 = row_44[1];
var values_47 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"for_role"), R.Join(new V[] {v_46}, new Results(v_34))));
V v_48 = values_47[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_48) > (V)0.0d))));
foreach (var row_49 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_48)))))) {
V v_50 = row_49[0];
V v_51 = row_49[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(v_51, (V)"id"), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)R.Length(R.Get(v_36, (V)"room_prefix"))))).First == R.Get(v_36, (V)"room_prefix")))));
}
}
}
return Results.Empty;
})))));
R.Call(v_25, R.Join(new V[] {(V)"new biome 100-seed connected-floor corpora retain full exploration budgets"}, new Results(new V((RuleFunction)(args_52 => {

var values_53 = R.Join(new V[] {}, new Results(R.Table()));
V v_54 = values_53[0];
{ double end_57 = R.Num((V)7.0d), step_58 = R.Num((V)1.0d);
for (double i_56 = R.Num((V)5.0d); step_58 > 0 ? i_56 <= end_57 : i_56 >= end_57; i_56 += step_58) {
V v_55 = i_56;
var values_59 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"order"), v_55)));
V v_60 = values_59[0];
var values_61 = R.Join(new V[] {(V)0.0d, (V)0.0d, (V)0.0d, (V)0.0d, (V)0.0d}, new Results((V)0.0d));
V v_62 = values_61[0];
V v_63 = values_61[1];
V v_64 = values_61[2];
V v_65 = values_61[3];
V v_66 = values_61[4];
V v_67 = values_61[5];
{ double end_70 = R.Num((V)100.0d), step_71 = R.Num((V)1.0d);
for (double i_69 = R.Num((V)1.0d); step_71 > 0 ? i_69 <= end_70 : i_69 >= end_70; i_69 += step_71) {
V v_68 = i_69;
var values_72 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"generate_pair"), R.Join(new V[] {v_68}, new Results(R.Table(new Entry((V)"biome_id", v_60))))));
V v_73 = values_72[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"validate_pair"), R.Join(new V[] {}, new Results(v_73)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)((V)R.Length(R.Get(R.Get(v_73, (V)1.0d), (V)"rooms")) + (V)R.Length(R.Get(R.Get(v_73, (V)2.0d), (V)"rooms"))) == (V)12.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)((V)R.Length(R.Get(R.Get(R.Get(v_73, (V)1.0d), (V)"placements"), (V)"chests")) + (V)R.Length(R.Get(R.Get(R.Get(v_73, (V)2.0d), (V)"placements"), (V)"chests"))) == (V)5.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)((V)R.Length(R.Get(R.Get(R.Get(v_73, (V)1.0d), (V)"placements"), (V)"secrets")) + (V)R.Length(R.Get(R.Get(R.Get(v_73, (V)2.0d), (V)"placements"), (V)"secrets"))) == (V)2.0d))));
foreach (var row_74 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_73)))))) {
V v_75 = row_74[0];
V v_76 = row_74[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_76, (V)"biome_id") == v_60), () => (V)(R.Get(v_76, (V)"strategy") == R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_60))).First, (V)"strategy"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"walkable"), R.Join(new V[] {v_76, R.Get(v_76, (V)"start_x")}, new Results(R.Get(v_76, (V)"start_y"))))));
var values_77 = R.Join(new V[] {}, new Results((V)((V)(v_62 + (V)R.Length(R.Get(R.Get(v_76, (V)"diagnostics"), (V)"attempts"))) - (V)1.0d)));
v_62 = values_77[0];
var values_78 = R.Join(new V[] {}, new Results((V)(v_63 + (V)R.Length(R.Get(v_76, (V)"repairs")))));
v_63 = values_78[0];
if (R.Truth(R.Get(R.Get(v_76, (V)"diagnostics"), (V)"fallback"))) {
var values_79 = R.Join(new V[] {}, new Results((V)(v_64 + (V)1.0d)));
v_64 = values_79[0];
}
foreach (var row_80 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_76, (V)"graph"), (V)"edges"))))))) {
V v_81 = row_80[0];
V v_82 = row_80[1];
if (R.Truth((V)(R.Get(v_82, (V)"kind") == (V)"loop"))) {
var values_83 = R.Join(new V[] {}, new Results((V)(v_65 + (V)1.0d)));
v_65 = values_83[0];
}
}
foreach (var row_84 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_76, (V)"corridors"))))))) {
V v_85 = row_84[0];
V v_86 = row_84[1];
if (R.Truth(R.Get(v_86, (V)"widened"))) {
var values_87 = R.Join(new V[] {}, new Results((V)(v_66 + (V)R.Length(R.Get(v_86, (V)"widened")))));
v_66 = values_87[0];
}
if (R.Truth(R.Get(v_86, (V)"parallel"))) {
var values_88 = R.Join(new V[] {}, new Results((V)(v_67 + (V)1.0d)));
v_67 = values_88[0];
}
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_64 == (V)0.0d), () => (V)(v_63 == (V)0.0d)))));
var values_89 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"loops", v_65), new Entry((V)"widened", v_66), new Entry((V)"parallel", v_67))));
var obj_90 = v_54;
V key_91 = v_60;
R.Set(obj_90, key_91, values_89[0]);
R.Call(rt.Global("print"), R.Join(new V[] {}, R.Invoke((V)"EXPANSION BIOME %s: 100 pairs, %d retries, %d repairs, %d fallbacks, %d loops, %d widened, %d parallel", "format", R.Join(new V[] {v_60, v_62, v_63, v_64, v_65, v_66}, new Results(v_67)))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_54, R.Get(R.Get(v_2, (V)"order"), (V)5.0d)), (V)"widened") > (V)0.0d), () => (V)(R.Get(R.Get(v_54, R.Get(R.Get(v_2, (V)"order"), (V)5.0d)), (V)"parallel") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_54, R.Get(R.Get(v_2, (V)"order"), (V)6.0d)), (V)"parallel") > (V)0.0d), () => (V)(R.Get(R.Get(v_54, R.Get(R.Get(v_2, (V)"order"), (V)6.0d)), (V)"widened") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_54, R.Get(R.Get(v_2, (V)"order"), (V)7.0d)), (V)"widened") > (V)0.0d), () => (V)(R.Get(R.Get(v_54, R.Get(R.Get(v_2, (V)"order"), (V)7.0d)), (V)"parallel") == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_25, R.Join(new V[] {(V)"route choices expose every new biome and never revisit a visited identity"}, new Results(new V((RuleFunction)(args_92 => {

var values_93 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"routes_for"), R.Join(new V[] {(V)1.0d}, new Results(R.Table()))));
V v_94 = values_93[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(v_94) == (V)3.0d), () => (V)(R.Get(R.Get(v_94, (V)1.0d), (V)"biome_id") == R.Get(R.Get(v_2, (V)"order"), (V)2.0d))), () => (V)(R.Get(R.Get(v_94, (V)2.0d), (V)"biome_id") == R.Get(R.Get(v_2, (V)"order"), (V)3.0d))), () => (V)(R.Get(R.Get(v_94, (V)3.0d), (V)"biome_id") == R.Get(R.Get(v_2, (V)"order"), (V)5.0d))))));
var values_95 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(R.Get(v_2, (V)"order"), (V)1.0d), (V)true))));
V v_96 = values_95[0];
foreach (var row_97 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_94)))))) {
V v_98 = row_97[0];
V v_99 = row_97[1];
var values_100 = R.Join(new V[] {}, new Results((V)true));
var obj_101 = v_96;
V key_102 = R.Get(v_99, (V)"biome_id");
R.Set(obj_101, key_102, values_100[0]);
var values_103 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"routes_for"), R.Join(new V[] {(V)2.0d}, new Results(R.Table(new Entry((V)1, R.Get(v_99, (V)"id")))))));
V v_104 = values_103[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_104) == (V)3.0d))));
var values_105 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(R.Get(v_2, (V)"order"), (V)1.0d), (V)true), new Entry(R.Get(v_99, (V)"biome_id"), (V)true))));
V v_106 = values_105[0];
foreach (var row_107 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_104)))))) {
V v_108 = row_107[0];
V v_109 = row_107[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_106, R.Get(v_109, (V)"biome_id")))))));
var values_110 = R.Join(new V[] {}, new Results((V)true));
var obj_111 = v_106;
V key_112 = R.Get(v_109, (V)"biome_id");
R.Set(obj_111, key_112, values_110[0]);
var values_113 = R.Join(new V[] {}, new Results((V)true));
var obj_114 = v_96;
V key_115 = R.Get(v_109, (V)"biome_id");
R.Set(obj_114, key_115, values_113[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_2, (V)"routes_for"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)1, R.Get(v_99, (V)"id")), new Entry((V)2, R.Get(v_109, (V)"id")))))).First) == (V)1.0d))));
}
R.Call(v_26, R.Join(new V[] {v_104}, R.Call(R.Get(v_2, (V)"routes_for"), R.Join(new V[] {(V)2.0d}, new Results(R.Table(new Entry((V)1, R.Get(v_99, (V)"id"))))))));
}
foreach (var row_116 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_117 = row_116[0];
V v_118 = row_116[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_96, v_118)}, new Results(R.Concat(v_118, (V)" unreachable"))));
}
return Results.Empty;
})))));
R.Call(v_25, R.Join(new V[] {(V)"new route destinations create the selected concrete biome and restore exactly"}, new Results(new V((RuleFunction)(args_119 => {

var values_120 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, R.Get(R.Get(v_2, (V)"order"), (V)5.0d)), new Entry((V)3, R.Table(new Entry(1, R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"order"), (V)5.0d))))))))), new Entry((V)2, R.Table(new Entry((V)1, (V)3.0d), new Entry((V)2, R.Get(R.Get(v_2, (V)"order"), (V)6.0d)), new Entry((V)3, R.Table(new Entry((V)1, R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"order"), (V)2.0d)))).First), new Entry(2, R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"order"), (V)6.0d))))))))), new Entry((V)3, R.Table(new Entry((V)1, (V)3.0d), new Entry((V)2, R.Get(R.Get(v_2, (V)"order"), (V)7.0d)), new Entry((V)3, R.Table(new Entry((V)1, R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"order"), (V)3.0d)))).First), new Entry(2, R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"order"), (V)7.0d))))))))))));
V v_121 = values_120[0];
foreach (var row_122 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_121)))))) {
V v_123 = row_122[0];
V v_124 = row_122[1];
var values_125 = R.Join(new V[] {R.Get(v_124, (V)1.0d), R.Get(v_124, (V)2.0d)}, new Results(R.Get(v_124, (V)3.0d)));
V v_126 = values_125[0];
V v_127 = values_125[1];
V v_128 = values_125[2];
var values_129 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {(V)37.0d, v_126, v_127}, new Results(v_128))));
V v_130 = values_129[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_130, (V)"stage"), (V)"biome_id") == v_127), () => (V)(R.Get(R.Get(v_130, (V)"generated_map"), (V)"biome_id") == v_127)), () => R.Invoke(R.Get(R.Get(v_130, (V)"floor_order"), (V)1.0d), "find", R.Join(new V[] {R.Invoke(v_127, "match", R.Join(new V[] {}, new Results((V)"[^/]+$"))).First, (V)1.0d}, new Results((V)true))).First))));
var values_131 = R.Join(new V[] {}, new Results(R.Table()));
V v_132 = values_131[0];

foreach (var row_133 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_127))).First, (V)"enemy_ids"))))))) {
V v_134 = row_133[0];
V v_135 = row_133[1];
var values_136 = R.Join(new V[] {}, new Results((V)true));
var obj_137 = v_132;
V key_138 = v_135;
R.Set(obj_137, key_138, values_136[0]);
}
var values_139 = R.Join(new V[] {}, new Results((V)0.0d));
V v_140 = values_139[0];
foreach (var row_141 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_130, (V)"entities"))))))) {
V v_142 = row_141[0];
V v_143 = row_141[1];
if (R.Truth(R.And((V)(R.Get(v_143, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_143, (V)"id") != R.Get(R.Get(v_130, (V)"stage"), (V)"elite_id"))))) {
if (R.Truth(R.Get(v_143, (V)"squad_kind"))) {
var values_144 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.enemy_squads"))));
V v_145 = values_144[0];
var values_146 = R.Join(new V[] {}, new Results((V)false));
V v_147 = values_146[0];
foreach (var row_148 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_145, (V)"biomes"), R.Invoke(v_127, "match", R.Join(new V[] {}, new Results((V)"[^/]+$"))).First))))))) {
V v_149 = row_148[0];
V v_150 = row_148[1];
if (R.Truth((V)(v_150 == R.Get(v_143, (V)"squad_kind")))) {
var values_151 = R.Join(new V[] {}, new Results((V)true));
v_147 = values_151[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_147, () => R.Get(R.Get(v_145, (V)"roles"), R.Get(v_143, (V)"definition_id")))}, new Results((V)"squad outside its approved biome pool")));
} else if (R.Truth(R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))).First, (V)"roles"), R.Get(v_143, (V)"definition_id")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_143, (V)"ai_id") == (V)"idea_regular")}, new Results((V)"invalid approved encounter controller")));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_132, R.Get(v_143, (V)"definition_id")))));
}
var values_152 = R.Join(new V[] {}, new Results((V)(v_140 + (V)1.0d)));
v_140 = values_152[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_140 > (V)0.0d))));
R.Call(v_26, R.Join(new V[] {R.Call(R.Get(v_8, (V)"snapshot"), R.Join(new V[] {}, new Results(v_130))).First}, R.Call(R.Get(v_8, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"snapshot"), R.Join(new V[] {}, new Results(v_130)))))))));
}
return Results.Empty;
})))));
R.Call(v_25, R.Join(new V[] {(V)"assisted route confirmations reach two distinct new biomes with zero-time handoffs"}, new Results(new V((RuleFunction)(args_153 => {

var values_154 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_155 = values_154[0];
var values_156 = R.Join(new V[] {}, new Results((V)false));
var obj_157 = R.Get(v_155, (V)"meta");
V key_158 = (V)"eligible";
R.Set(obj_157, key_158, values_156[0]);
var values_159 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"player"), R.Join(new V[] {}, new Results(v_155))));
V v_160 = values_159[0];
V v_161 = V.Nil;
v_161 = new V((RuleFunction)(args_162 => {
V v_163 = args_162[0];
V v_164 = args_162[1];
foreach (var row_165 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_166 = row_165[0];
V v_167 = row_165[1];
var values_168 = R.Join(new V[] {(V)(v_163 - R.Get(v_167, (V)1.0d))}, new Results((V)(v_164 - R.Get(v_167, (V)2.0d))));
V v_169 = values_168[0];
V v_170 = values_168[1];
var values_171 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"walkable"), R.Join(new V[] {R.Get(v_155, (V)"generated_map"), v_169}, new Results(v_170))));
V v_172 = values_171[0];
foreach (var row_173 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_155, (V)"entities"))))))) {
V v_174 = row_173[0];
V v_175 = row_173[1];
var values_176 = R.Join(new V[] {}, new Results(R.Get(v_175, (V)"position")));
V v_177 = values_176[0];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_175, (V)"id") != R.Get(v_160, (V)"id")), () => R.Get(v_175, (V)"blocks_movement")), () => (V)(R.Get(v_177, (V)"floor_id") == R.Get(v_155, (V)"active_floor_id"))), () => (V)(R.Get(v_177, (V)"x") == v_169)), () => (V)(R.Get(v_177, (V)"y") == v_170)))) {
var values_178 = R.Join(new V[] {}, new Results((V)false));
v_172 = values_178[0];
}
}
if (R.Truth(v_172)) {
var values_179 = R.Join(new V[] {v_169}, new Results(v_170));
var obj_180 = R.Get(v_160, (V)"position");
V key_181 = (V)"x";
var obj_182 = R.Get(v_160, (V)"position");
V key_183 = (V)"y";
R.Set(obj_180, key_181, values_179[0]);
R.Set(obj_182, key_183, values_179[1]);
return R.Join(new V[] {R.Get(v_167, (V)1.0d)}, new Results(R.Get(v_167, (V)2.0d)));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no prepared approach")));
return Results.Empty;
}));
{ double end_186 = R.Num((V)2.0d), step_187 = R.Num((V)1.0d);
for (double i_185 = R.Num((V)1.0d); step_187 > 0 ? i_185 <= end_186 : i_185 >= end_186; i_185 += step_187) {
V v_184 = i_185;
var values_188 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_155, (V)"generated_map"), (V)"placements"), (V)"stairs"), (V)1.0d)));
V v_189 = values_188[0];
var values_190 = R.Join(new V[] {}, R.Call(v_161, R.Join(new V[] {R.Get(v_189, (V)"x")}, new Results(R.Get(v_189, (V)"y")))));
V v_191 = values_190[0];
V v_192 = values_190[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"submit"), R.Join(new V[] {v_155}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_191), new Entry((V)"dy", v_192)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_155, (V)"active_floor_index") == (V)2.0d))));
var values_193 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"entity"), R.Join(new V[] {v_155}, new Results(R.Get(R.Get(v_155, (V)"stage"), (V)"elite_id")))))));
V v_194 = values_193[0];
var values_195 = R.Join(new V[] {}, R.Call(v_161, R.Join(new V[] {R.Get(R.Get(v_194, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_194, (V)"position"), (V)"y")))));
v_191 = values_195[0];
v_192 = values_195[1];
var values_196 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_194, (V)"hp") + R.Get(v_194, (V)"defense")) + (V)20.0d)));
var obj_197 = v_160;
V key_198 = (V)"damage";
R.Set(obj_197, key_198, values_196[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"submit"), R.Join(new V[] {v_155}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_191), new Entry((V)"dy", v_192)))))));
var values_199 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"entity"), R.Join(new V[] {v_155}, new Results(R.Get(R.Get(v_155, (V)"stage"), (V)"exit_id")))))));
V v_200 = values_199[0];
R.Call(v_161, R.Join(new V[] {R.Get(R.Get(v_200, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_200, (V)"position"), (V)"y"))));
var values_201 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"route_options"), R.Join(new V[] {v_155}, new Results(R.Get(v_200, (V)"id")))))));
V v_202 = values_201[0];
var values_203 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_202, (V)"routes"), (V)3.0d)));
V v_204 = values_203[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_204, () => R.Call(R.Get(v_8, (V)"submit"), R.Join(new V[] {v_155}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_200, (V)"id")), new Entry((V)"route_index", (V)3.0d))))).First))));
var values_205 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"player"), R.Join(new V[] {}, new Results(v_155))));
v_160 = values_205[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_155, (V)"clock") == (V)0.0d), () => (V)(R.Get(R.Get(v_155, (V)"stage"), (V)"index") == (V)(v_184 + (V)1.0d))), () => (V)(R.Get(R.Get(v_155, (V)"stage"), (V)"biome_id") == R.Get(v_204, (V)"biome_id"))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_155, (V)"stage"), (V)"biome_id") == R.Get(R.Get(v_2, (V)"order"), (V)7.0d)), () => (V)(R.Get(R.Get(R.Get(v_155, (V)"stage"), (V)"route_history"), (V)1.0d) == R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"order"), (V)5.0d)))).First)), () => (V)(R.Get(R.Get(R.Get(v_155, (V)"stage"), (V)"route_history"), (V)2.0d) == R.Call(v_13, R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"order"), (V)7.0d)))).First)))));
R.Call(v_26, R.Join(new V[] {R.Call(R.Get(v_8, (V)"snapshot"), R.Join(new V[] {}, new Results(v_155))).First}, R.Call(R.Get(v_8, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"snapshot"), R.Join(new V[] {}, new Results(v_155)))))))));
return Results.Empty;
})))));
R.Call(v_25, R.Join(new V[] {(V)"ordinary legacy-stage actions select Observatory and defeat one encounter"}, new Results(new V((RuleFunction)(args_206 => {

var values_207 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"stage_one"), R.Join(new V[] {(V)3.0d}, new Results((V)3.0d))));
V v_208 = values_207[0];
var values_209 = R.Join(new V[] {}, new Results(R.Get(v_208, (V)"state")));
V v_210 = values_209[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_210, (V)"stage"), (V)"index") == (V)2.0d), () => (V)(R.Get(R.Get(v_210, (V)"stage"), (V)"biome_id") == R.Get(R.Get(v_2, (V)"order"), (V)5.0d))))));
var values_213 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"nearest"), R.Join(new V[] {v_210}, new Results(new V((RuleFunction)(args_211 => {
V v_212 = args_211[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_212, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_212, (V)"id") != R.Get(R.Get(v_210, (V)"stage"), (V)"elite_id"))), () => (V)(R.Get(R.Get(v_212, (V)"position"), (V)"floor_id") == R.Get(v_210, (V)"active_floor_id")))));
return Results.Empty;
}))))))));
V v_214 = values_213[0];
var values_215 = R.Join(new V[] {}, new Results(R.Get(v_214, (V)"id")));
V v_216 = values_215[0];
var values_217 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"fight"), R.Join(new V[] {v_208, v_216}, new Results((V)120.0d))));
V v_218 = values_217[0];
V v_219 = values_217[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_218}, new Results(v_219)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_8, (V)"entity"), R.Join(new V[] {v_210}, new Results(v_216))).First)), () => (V)(!R.Truth(R.Get(v_210, (V)"game_over")))))));
R.Call(rt.Global("print"), R.Join(new V[] {}, R.Invoke((V)"EXPANSION LEGAL seed=3 route=Observatory actions=%d hp=%d defeated=%s", "format", R.Join(new V[] {(V)R.Length(R.Get(v_208, (V)"trace")), R.Get(R.Call(R.Get(v_8, (V)"player"), R.Join(new V[] {}, new Results(v_210))).First, (V)"hp")}, new Results(R.Get(v_214, (V)"definition_id"))))));
return Results.Empty;
})))));
R.Call(v_25, R.Join(new V[] {(V)"an existing two-offer extraction save retains its cached choices"}, new Results(new V((RuleFunction)(args_220 => {

var values_221 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new_stage"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_222 = values_221[0];
var values_223 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"player"), R.Join(new V[] {}, new Results(v_222))));
V v_224 = values_223[0];
var values_225 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"entity"), R.Join(new V[] {v_222}, new Results(R.Get(R.Get(v_222, (V)"stage"), (V)"elite_id")))))));
V v_226 = values_225[0];
var values_227 = R.Join(new V[] {}, new Results((V)false));
var obj_228 = R.Get(v_222, (V)"meta");
V key_229 = (V)"eligible";
R.Set(obj_228, key_229, values_227[0]);
var values_230 = Results.Empty;
V v_231 = values_230[0];
V v_232 = values_230[1];
foreach (var row_233 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_234 = row_233[0];
V v_235 = row_233[1];
var values_236 = R.Join(new V[] {(V)(R.Get(R.Get(v_226, (V)"position"), (V)"x") - R.Get(v_235, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_226, (V)"position"), (V)"y") - R.Get(v_235, (V)2.0d))));
V v_237 = values_236[0];
V v_238 = values_236[1];
if (R.Truth(R.Call(R.Get(v_10, (V)"walkable"), R.Join(new V[] {R.Get(v_222, (V)"generated_map"), v_237}, new Results(v_238))).First)) {
var values_239 = R.Join(new V[] {v_237}, new Results(v_238));
var obj_240 = R.Get(v_224, (V)"position");
V key_241 = (V)"x";
var obj_242 = R.Get(v_224, (V)"position");
V key_243 = (V)"y";
R.Set(obj_240, key_241, values_239[0]);
R.Set(obj_242, key_243, values_239[1]);

var values_244 = R.Join(new V[] {R.Get(v_235, (V)1.0d)}, new Results(R.Get(v_235, (V)2.0d)));
v_231 = values_244[0];
v_232 = values_244[1];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_231)));
var values_245 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_246 = v_224;
V key_247 = (V)"damage";
R.Set(obj_246, key_247, values_245[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"submit"), R.Join(new V[] {v_222}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_231), new Entry((V)"dy", v_232)))))));
var values_248 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"entity"), R.Join(new V[] {v_222}, new Results(R.Get(R.Get(v_222, (V)"stage"), (V)"exit_id")))))));
V v_249 = values_248[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_249, (V)"routes")) == (V)3.0d))));
var values_250 = R.Join(new V[] {}, new Results(V.Nil));
var obj_251 = R.Get(v_249, (V)"routes");
V key_252 = (V)3.0d;
R.Set(obj_251, key_252, values_250[0]);
var values_253 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"snapshot"), R.Join(new V[] {}, new Results(v_222))))));
V v_254 = values_253[0];
var values_255 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"entity"), R.Join(new V[] {v_254}, new Results(R.Get(R.Get(v_254, (V)"stage"), (V)"exit_id")))))));
V v_256 = values_255[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_256, (V)"routes")) == (V)2.0d))));
var values_257 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"player"), R.Join(new V[] {}, new Results(v_254))));
V v_258 = values_257[0];
foreach (var row_259 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_260 = row_259[0];
V v_261 = row_259[1];
var values_262 = R.Join(new V[] {(V)(R.Get(R.Get(v_256, (V)"position"), (V)"x") - R.Get(v_261, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_256, (V)"position"), (V)"y") - R.Get(v_261, (V)2.0d))));
V v_263 = values_262[0];
V v_264 = values_262[1];
if (R.Truth(R.Call(R.Get(v_10, (V)"walkable"), R.Join(new V[] {R.Get(v_254, (V)"generated_map"), v_263}, new Results(v_264))).First)) {
var values_265 = R.Join(new V[] {v_263}, new Results(v_264));
var obj_266 = R.Get(v_258, (V)"position");
V key_267 = (V)"x";
var obj_268 = R.Get(v_258, (V)"position");
V key_269 = (V)"y";
R.Set(obj_266, key_267, values_265[0]);
R.Set(obj_268, key_269, values_265[1]);
break;
}
}
var values_270 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"route_options"), R.Join(new V[] {v_254}, new Results(R.Get(v_256, (V)"id")))))));
V v_271 = values_270[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_271, (V)"routes")) == (V)2.0d), () => (V)(R.Get(R.Get(R.Get(v_271, (V)"routes"), (V)1.0d), (V)"id") == R.Get(R.Get(R.Get(v_249, (V)"routes"), (V)1.0d), (V)"id"))), () => (V)(R.Get(R.Get(R.Get(v_271, (V)"routes"), (V)2.0d), (V)"id") == R.Get(R.Get(R.Get(v_249, (V)"routes"), (V)2.0d), (V)"id"))))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
