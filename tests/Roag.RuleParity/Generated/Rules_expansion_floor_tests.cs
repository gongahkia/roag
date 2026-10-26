// Static C# port of tests/expansion_floor_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: ea54df8fb86ce1053f5830e12eff9ba569baec6288241805025bce15de702375
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_expansion_floor_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_world"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_16 = values_15[0];
V v_17 = V.Nil;
v_17 = new V((RuleFunction)(args_18 => {
V v_19 = args_18[0];
var values_20 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_19, (V)"generated_map"), (V)"placements"), (V)"stairs"), (V)1.0d)));
V v_21 = values_20[0];
var values_22 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_19))));
V v_23 = values_22[0];
foreach (var row_24 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_25 = row_24[0];
V v_26 = row_24[1];
var values_27 = R.Join(new V[] {(V)(R.Get(v_21, (V)"x") - R.Get(v_26, (V)1.0d))}, new Results((V)(R.Get(v_21, (V)"y") - R.Get(v_26, (V)2.0d))));
V v_28 = values_27[0];
V v_29 = values_27[1];
var values_30 = R.Join(new V[] {}, new Results((V)false));
V v_31 = values_30[0];
foreach (var row_32 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_19, (V)"entities"))))))) {
V v_33 = row_32[0];
V v_34 = row_32[1];
if (R.Truth(R.And(R.And(R.And(R.Get(v_34, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_34, (V)"position"), (V)"floor_id") == R.Get(v_19, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_34, (V)"position"), (V)"x") == v_28)), () => (V)(R.Get(R.Get(v_34, (V)"position"), (V)"y") == v_29)))) {
var values_35 = R.Join(new V[] {}, new Results((V)true));
v_31 = values_35[0];

break;
}
}
if (R.Truth(R.And(R.Call(R.Get(v_8, (V)"walkable"), R.Join(new V[] {R.Get(v_19, (V)"generated_map"), v_28}, new Results(v_29))).First, () => (V)(!R.Truth(v_31))))) {
var values_36 = R.Join(new V[] {}, new Results((V)false));
var obj_37 = R.Get(v_19, (V)"meta");
V key_38 = (V)"eligible";
R.Set(obj_37, key_38, values_36[0]);
var values_39 = R.Join(new V[] {v_28}, new Results(v_29));
var obj_40 = R.Get(v_23, (V)"position");
V key_41 = (V)"x";
var obj_42 = R.Get(v_23, (V)"position");
V key_43 = (V)"y";
R.Set(obj_40, key_41, values_39[0]);
R.Set(obj_42, key_43, values_39[1]);
R.Call(R.Get(v_10, (V)"recompute"), R.Join(new V[] {}, new Results(v_19)));
return R.Join(new V[] {R.Get(v_26, (V)1.0d)}, new Results(R.Get(v_26, (V)2.0d)));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"stair has no free approach")));
return Results.Empty;
}));
V v_44 = V.Nil;
v_44 = new V((RuleFunction)(args_45 => {
V v_46 = args_45[0];
V v_47 = args_45[1];
var values_48 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_46))));
V v_49 = values_48[0];
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
var values_53 = R.Join(new V[] {(V)(R.Get(R.Get(v_47, (V)"position"), (V)"x") - R.Get(v_52, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_47, (V)"position"), (V)"y") - R.Get(v_52, (V)2.0d))));
V v_54 = values_53[0];
V v_55 = values_53[1];
var values_56 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"walkable"), R.Join(new V[] {R.Get(v_46, (V)"generated_map"), v_54}, new Results(v_55))));
V v_57 = values_56[0];
foreach (var row_58 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_46, (V)"entities"))))))) {
V v_59 = row_58[0];
V v_60 = row_58[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_60, (V)"id") != R.Get(v_49, (V)"id")), () => R.Get(v_60, (V)"blocks_movement")), () => (V)(R.Get(R.Get(v_60, (V)"position"), (V)"floor_id") == R.Get(v_46, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_60, (V)"position"), (V)"x") == v_54)), () => (V)(R.Get(R.Get(v_60, (V)"position"), (V)"y") == v_55)))) {
var values_61 = R.Join(new V[] {}, new Results((V)false));
v_57 = values_61[0];
}
}
if (R.Truth(v_57)) {
var values_62 = R.Join(new V[] {}, new Results((V)false));
var obj_63 = R.Get(v_46, (V)"meta");
V key_64 = (V)"eligible";
R.Set(obj_63, key_64, values_62[0]);
var values_65 = R.Join(new V[] {v_54}, new Results(v_55));
var obj_66 = R.Get(v_49, (V)"position");
V key_67 = (V)"x";
var obj_68 = R.Get(v_49, (V)"position");
V key_69 = (V)"y";
R.Set(obj_66, key_67, values_65[0]);
R.Set(obj_68, key_69, values_65[1]);
R.Call(R.Get(v_10, (V)"recompute"), R.Join(new V[] {}, new Results(v_46)));
return R.Join(new V[] {R.Get(v_52, (V)1.0d)}, new Results(R.Get(v_52, (V)2.0d)));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no free entity approach")));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_70 => {
V v_71 = args_70[0];
V v_72 = args_70[1];
R.Call(v_71, R.Join(new V[] {(V)"connected biome pairs use twelve rooms, five chests and reciprocal stairs across 100 seeds"}, new Results(new V((RuleFunction)(args_73 => {

foreach (var row_74 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"order"))))))) {
V v_75 = row_74[0];
V v_76 = row_74[1];
{ double end_79 = R.Num((V)100.0d), step_80 = R.Num((V)1.0d);
for (double i_78 = R.Num((V)1.0d); step_80 > 0 ? i_78 <= end_79 : i_78 >= end_79; i_78 += step_80) {
V v_77 = i_78;
var values_81 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"generate_pair"), R.Join(new V[] {v_77}, new Results(R.Table(new Entry((V)"biome_id", v_76))))));
V v_82 = values_81[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"validate_pair"), R.Join(new V[] {}, new Results(v_82)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(R.Get(v_82, (V)1.0d), (V)"rooms")) == (V)6.0d), () => (V)((V)R.Length(R.Get(R.Get(v_82, (V)2.0d), (V)"rooms")) == (V)6.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)((V)R.Length(R.Get(R.Get(R.Get(v_82, (V)1.0d), (V)"placements"), (V)"chests")) + (V)R.Length(R.Get(R.Get(R.Get(v_82, (V)2.0d), (V)"placements"), (V)"chests"))) == (V)5.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)((V)R.Length(R.Get(R.Get(R.Get(v_82, (V)1.0d), (V)"placements"), (V)"secrets")) + (V)R.Length(R.Get(R.Get(R.Get(v_82, (V)2.0d), (V)"placements"), (V)"secrets"))) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_82, (V)1.0d), (V)"floor_id") != R.Get(R.Get(v_82, (V)2.0d), (V)"floor_id")))));
var values_83 = R.Join(new V[] {R.Get(R.Get(R.Get(R.Get(v_82, (V)1.0d), (V)"placements"), (V)"stairs"), (V)1.0d)}, new Results(R.Get(R.Get(R.Get(R.Get(v_82, (V)2.0d), (V)"placements"), (V)"stairs"), (V)1.0d)));
V v_84 = values_83[0];
V v_85 = values_83[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_84, (V)"to_floor_id") == R.Get(R.Get(v_82, (V)2.0d), (V)"floor_id")), () => (V)(R.Get(v_85, (V)"to_floor_id") == R.Get(R.Get(v_82, (V)1.0d), (V)"floor_id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_8, (V)"walkable"), R.Join(new V[] {R.Get(v_82, (V)1.0d), R.Get(v_84, (V)"x")}, new Results(R.Get(v_84, (V)"y")))).First, () => R.Call(R.Get(v_8, (V)"walkable"), R.Join(new V[] {R.Get(v_82, (V)2.0d), R.Get(v_85, (V)"x")}, new Results(R.Get(v_85, (V)"y")))).First))));
}
}
}
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"connected generated stage has one active floor and a frozen lower-floor Warden"}, new Results(new V((RuleFunction)(args_86 => {

var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_88 = values_87[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_88, (V)"active_floor_index") == (V)1.0d), () => (V)(R.Get(R.Get(v_88, (V)"floor_order"), (V)1.0d) == R.Get(v_88, (V)"active_floor_id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_88, (V)"stage"), (V)"elite_floor_id") == R.Get(R.Get(v_88, (V)"floor_order"), (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_88}, new Results(R.Get(R.Get(v_88, (V)"stage"), (V)"elite_id")))).First, (V)"position"), (V)"floor_id") == R.Get(R.Get(v_88, (V)"floor_order"), (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_88, (V)"floor_pause"), R.Get(R.Get(v_88, (V)"floor_order"), (V)2.0d)), (V)"schedule")) > (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_88, (V)"secrets")) == (V)2.0d))));
R.Call(v_72, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_88))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_88)))))))));
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"ordinary stair move costs one recovery and keeps old-floor work frozen"}, new Results(new V((RuleFunction)(args_89 => {

var values_90 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_91 = values_90[0];
var values_92 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_91))));
V v_93 = values_92[0];
V v_94 = values_92[1];
var values_95 = R.Join(new V[] {}, new Results(R.Get(v_91, (V)"active_floor_id")));
V v_96 = values_95[0];
var values_97 = Results.Empty;
V v_98 = values_97[0];
foreach (var row_99 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_91, (V)"entities"))))))) {
V v_100 = row_99[0];
V v_101 = row_99[1];
if (R.Truth(R.And((V)(R.Get(v_101, (V)"controller") == (V)"ai"), () => (V)(R.Get(R.Get(v_101, (V)"position"), (V)"floor_id") == v_96)))) {
var values_102 = R.Join(new V[] {}, new Results(v_101));
v_98 = values_102[0];

break;
}
}
var values_103 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(R.Get(v_98, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_98, (V)"position"), (V)"y")), new Entry((V)"hp", R.Get(v_98, (V)"hp")))));
V v_104 = values_103[0];
var values_105 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_91}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_93), new Entry((V)"dy", v_94))))));
V v_106 = values_105[0];
V v_107 = values_105[1];
V v_108 = values_105[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_106, () => (V)(!R.Truth(v_107))), () => (V)(R.Get(v_91, (V)"active_floor_index") == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_91, (V)"clock") == (V)100.0d), () => (V)(R.Get(R.Get(v_91, (V)"last_action"), (V)"cost") == (V)100.0d)))));
var values_109 = R.Join(new V[] {}, new Results((V)0.0d));
V v_110 = values_109[0];
foreach (var row_111 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_108)))))) {
V v_112 = row_111[0];
V v_113 = row_111[1];
if (R.Truth((V)(R.Get(v_113, (V)"kind") == (V)"FloorChanged"))) {
var values_114 = R.Join(new V[] {}, new Results((V)(v_110 + (V)1.0d)));
v_110 = values_114[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_110 == (V)1.0d), () => R.Get(R.Get(v_91, (V)"floor_pause"), v_96)))));
var values_115 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_91))).First, (V)"floor_pause"), v_96)));
V v_116 = values_115[0];
{ double end_119 = R.Num((V)3.0d), step_120 = R.Num((V)1.0d);
for (double i_118 = R.Num((V)1.0d); step_120 > 0 ? i_118 <= end_119 : i_118 >= end_119; i_118 += step_120) {
V v_117 = i_118;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_91}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_91, (V)"clock") >= (V)400.0d), () => (V)(R.Get(R.Get(v_98, (V)"position"), (V)"x") == R.Get(v_104, (V)"x"))), () => (V)(R.Get(R.Get(v_98, (V)"position"), (V)"y") == R.Get(v_104, (V)"y"))), () => (V)(R.Get(v_98, (V)"hp") == R.Get(v_104, (V)"hp"))))));
R.Call(v_72, R.Join(new V[] {v_116}, new Results(R.Get(R.Get(v_91, (V)"floor_pause"), v_96))));
R.Call(v_72, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_91))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_91)))))))));
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"connected-floor generation and stage creation preserve seed determinism"}, new Results(new V((RuleFunction)(args_121 => {

var values_122 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)29.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_123 = values_122[0];
var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)29.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_125 = values_124[0];
R.Call(v_72, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_123))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_125)))));
var values_126 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"generate_pair"), R.Join(new V[] {(V)29.0d}, new Results(R.Table(new Entry((V)"biome_id", R.Get(R.Get(v_6, (V)"order"), (V)1.0d)))))));
V v_127 = values_126[0];
R.Call(v_72, R.Join(new V[] {v_127}, R.Call(R.Get(v_4, (V)"generate_pair"), R.Join(new V[] {(V)29.0d}, new Results(R.Table(new Entry((V)"biome_id", R.Get(R.Get(v_6, (V)"order"), (V)1.0d))))))));
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"E03 authored stair marker transforms into a real reciprocal generated link"}, new Results(new V((RuleFunction)(args_128 => {

var values_129 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"new"), R.Join(new V[] {}, new Results((V)"stair_fixture"))));
V v_130 = values_129[0];
var values_131 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"new_room"), R.Join(new V[] {(V)"studio:room/stair_hall", (V)"Stair hall", (V)9.0d}, new Results((V)7.0d))));
V v_132 = values_131[0];
var values_133 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"elite"))));
var obj_134 = v_132;
V key_135 = (V)"roles";
R.Set(obj_134, key_135, values_133[0]);

var values_136 = R.Join(new V[] {}, new Results((V)20.0d));
var obj_137 = v_132;
V key_138 = (V)"weight";
R.Set(obj_137, key_138, values_136[0]);

var values_139 = R.Join(new V[] {}, new Results(R.Table()));
var obj_140 = R.Get(v_132, (V)"markers");
V key_141 = (V)"enemy";
R.Set(obj_140, key_141, values_139[0]);
var values_142 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)5.0d), new Entry((V)"y", (V)4.0d))))));
var obj_143 = R.Get(v_132, (V)"markers");
V key_144 = (V)"elite";
R.Set(obj_143, key_144, values_142[0]);
var values_145 = R.Join(new V[] {}, new Results(V.Nil));
var obj_146 = R.Get(v_132, (V)"markers");
V key_147 = (V)"stair";
R.Set(obj_146, key_147, values_145[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"add_marker"), R.Join(new V[] {v_132, (V)"stair", (V)3.0d}, new Results((V)3.0d)))));
var values_148 = R.Join(new V[] {}, new Results(v_132));
var obj_149 = R.Get(v_130, (V)"rooms");
V key_150 = (V)1.0d;
R.Set(obj_149, key_150, values_148[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"validate"), R.Join(new V[] {}, new Results(v_130)))));
var values_151 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"encode"), R.Join(new V[] {}, new Results(v_130))))));
V v_152 = values_151[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(R.Get(v_152, (V)"rooms"), (V)1.0d), (V)"markers"), (V)"stair")) == (V)1.0d))));
var values_153 = R.Join(new V[] {}, new Results((V)false));
V v_154 = values_153[0];
{ double end_157 = R.Num((V)60.0d), step_158 = R.Num((V)1.0d);
for (double i_156 = R.Num((V)1.0d); step_158 > 0 ? i_156 <= end_157 : i_156 >= end_157; i_156 += step_158) {
V v_155 = i_156;
var values_159 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"generate_pair"), R.Join(new V[] {v_155}, new Results(R.Table(new Entry((V)"studio_pack", v_152))))));
V v_160 = values_159[0];
var values_161 = R.Join(new V[] {}, new Results(R.Get(v_160, (V)1.0d)));
V v_162 = values_161[0];
if (R.Truth((V)(R.Get(R.Get(R.Get(v_162, (V)"rooms"), (V)6.0d), (V)"template_id") == R.Get(v_132, (V)"id")))) {
var values_163 = R.Join(new V[] {}, new Results((V)true));
v_154 = values_163[0];
var values_164 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(R.Get(v_162, (V)"rooms"), (V)6.0d), (V)"markers"), (V)"stair"), (V)1.0d)));
V v_165 = values_164[0];
var values_166 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_162, (V)"placements"), (V)"stairs"), (V)1.0d)));
V v_167 = values_166[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_165, (V)"x") == R.Get(v_167, (V)"x")), () => (V)(R.Get(v_165, (V)"y") == R.Get(v_167, (V)"y"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Get(R.Get(v_160, (V)2.0d), (V)"placements"), (V)"stairs"), (V)1.0d), (V)"to_x") == R.Get(v_165, (V)"x")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"validate_pair"), R.Join(new V[] {}, new Results(v_160)))));
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_154}, new Results((V)"authored stair template never entered generated pool")));
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"normal legal movement reaches generated second floor without fixture mutation"}, new Results(new V((RuleFunction)(args_168 => {

var values_169 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"new"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_170 = values_169[0];
var values_171 = R.Join(new V[] {}, new Results(R.Get(v_170, (V)"state")));
V v_172 = values_171[0];
var values_173 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_172, (V)"generated_map"), (V)"placements"), (V)"stairs"), (V)1.0d)));
V v_174 = values_173[0];
{ double end_177 = R.Num((V)150.0d), step_178 = R.Num((V)1.0d);
for (double i_176 = R.Num((V)1.0d); step_178 > 0 ? i_176 <= end_177 : i_176 >= end_177; i_176 += step_178) {
V v_175 = i_176;
if (R.Truth((V)(R.Get(v_172, (V)"active_floor_index") == (V)2.0d))) {
break;
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_172, (V)"game_over")))}, new Results((V)"player died before legal stair entry")));
var values_181 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"path"), R.Join(new V[] {v_172}, new Results(new V((RuleFunction)(args_179 => {
V v_180 = args_179[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_180, (V)"x") == R.Get(v_174, (V)"x")), () => (V)(R.Get(v_180, (V)"y") == R.Get(v_174, (V)"y")))));
return Results.Empty;
}))))));
V v_182 = values_181[0];
R.Call(rt.Global("assert"), R.Join(new V[] {v_182}, new Results((V)"no legal route to stair")));
var values_183 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"action"), R.Join(new V[] {v_170}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_182, (V)1.0d)), new Entry((V)"dy", R.Get(v_182, (V)2.0d)))))));
V v_184 = values_183[0];
V v_185 = values_183[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_184}, new Results(v_185)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_172, (V)"active_floor_index") == (V)2.0d), () => (V)((V)R.Length(R.Get(v_170, (V)"trace")) < (V)150.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_172, (V)"clock") > (V)0.0d), () => (V)(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_172))).First, (V)"hp") > (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"validate_pair"), R.Join(new V[] {R.Table(new Entry((V)1, R.Get(R.Get(v_172, (V)"inactive_floor_maps"), R.Get(R.Get(v_172, (V)"floor_order"), (V)1.0d))), new Entry((V)2, R.Get(v_172, (V)"generated_map")))}, new Results(R.Get(v_172, (V)"secrets"))))));
R.Call(v_72, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_172))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_172)))))))));
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"returning upstairs rebases only floor-local work while stage pressure stays global"}, new Results(new V((RuleFunction)(args_186 => {

var values_187 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_188 = values_187[0];
var values_189 = R.Join(new V[] {}, new Results(R.Get(v_188, (V)"active_floor_id")));
V v_190 = values_189[0];
var values_191 = R.Join(new V[] {}, R.Call(R.Get(v_16, (V)"enqueue"), R.Join(new V[] {v_188}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", (V)1200.0d), new Entry((V)"effect_id", (V)"test:floor_local"))))));
V v_192 = values_191[0];
var values_193 = Results.Empty;
V v_194 = values_193[0];
foreach (var row_195 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_188, (V)"schedule"))))))) {
V v_196 = row_195[0];
V v_197 = row_195[1];
if (R.Truth((V)(R.Get(v_197, (V)"kind") == (V)"stage_pressure"))) {
var values_198 = R.Join(new V[] {}, new Results(v_197));
v_194 = values_198[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_194, () => (V)(R.Get(v_194, (V)"floor_id") == V.Nil)))));
var values_199 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_188))));
V v_200 = values_199[0];
V v_201 = values_199[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_188}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_200), new Entry((V)"dy", v_201)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_188, (V)"floor_pause"), v_190))));
{ double end_204 = R.Num((V)3.0d), step_205 = R.Num((V)1.0d);
for (double i_203 = R.Num((V)1.0d); step_205 > 0 ? i_203 <= end_204 : i_203 >= end_204; i_203 += step_205) {
V v_202 = i_203;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_188}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_188, (V)"clock") == (V)400.0d), () => (V)(R.Get(R.Get(R.Get(v_188, (V)"floor_pause"), v_190), (V)"suspended_at") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_194, (V)"due") == R.Get(R.Get(v_188, (V)"stage"), (V)"next_pressure")))));
var values_206 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_188))));
v_200 = values_206[0];
v_201 = values_206[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_188}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_200), new Entry((V)"dy", v_201)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_188, (V)"active_floor_id") == v_190), () => (V)(R.Get(v_188, (V)"clock") == (V)500.0d)))));
var values_207 = Results.Empty;
V v_208 = values_207[0];
foreach (var row_209 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_188, (V)"schedule"))))))) {
V v_210 = row_209[0];
V v_211 = row_209[1];
if (R.Truth((V)(R.Get(v_211, (V)"id") == R.Get(v_192, (V)"id")))) {
var values_212 = R.Join(new V[] {}, new Results(v_211));
v_208 = values_212[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_208, () => (V)(R.Get(v_208, (V)"floor_id") == v_190)), () => (V)(R.Get(v_208, (V)"due") == (V)1600.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_194, (V)"due") == R.Get(R.Get(v_188, (V)"stage"), (V)"next_pressure")))));
R.Call(v_72, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_188))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_188)))))))));
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"lower-floor Warden extraction hands off to a fresh paired biome without leaking old work"}, new Results(new V((RuleFunction)(args_213 => {

var values_214 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_215 = values_214[0];
var values_216 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_215))));
V v_217 = values_216[0];
V v_218 = values_216[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_215}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_217), new Entry((V)"dy", v_218)))))));
var values_219 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(R.Get(v_215, (V)"floor_order"), (V)1.0d)), new Entry((V)2, R.Get(R.Get(v_215, (V)"floor_order"), (V)2.0d)))));
V v_220 = values_219[0];
var values_221 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_215}, new Results(R.Get(R.Get(v_215, (V)"stage"), (V)"elite_id")))))));
V v_222 = values_221[0];
var values_223 = R.Join(new V[] {}, R.Call(v_44, R.Join(new V[] {v_215}, new Results(v_222))));
v_217 = values_223[0];
v_218 = values_223[1];
var values_224 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_222, (V)"hp") + R.Get(v_222, (V)"defense")) + (V)20.0d)));
var obj_225 = R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_215))).First;
V key_226 = (V)"damage";
R.Set(obj_225, key_226, values_224[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_215}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_217), new Entry((V)"dy", v_218)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_215, (V)"stage"), (V)"phase") == (V)"extraction"))));
var values_227 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_215}, new Results(R.Get(R.Get(v_215, (V)"stage"), (V)"exit_id")))))));
V v_228 = values_227[0];
R.Call(v_44, R.Join(new V[] {v_215}, new Results(v_228)));
var values_229 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"route_options"), R.Join(new V[] {v_215}, new Results(R.Get(v_228, (V)"id")))))));
V v_230 = values_229[0];
var values_231 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_230, (V)"routes"), (V)1.0d)));
V v_232 = values_231[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_215}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_228, (V)"id")), new Entry((V)"route_index", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Get(v_215, (V)"clock") == (V)0.0d), () => (V)(R.Get(R.Get(v_215, (V)"stage"), (V)"index") == (V)2.0d)), () => (V)(R.Get(R.Get(v_215, (V)"stage"), (V)"biome_id") == R.Get(v_232, (V)"biome_id"))), () => R.Get(v_215, (V)"floor_order")), () => (V)((V)R.Length(R.Get(v_215, (V)"retired_stages")) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_215, (V)"floor_order"), (V)1.0d) != R.Get(v_220, (V)1.0d)), () => (V)(R.Get(R.Get(v_215, (V)"floor_order"), (V)2.0d) != R.Get(v_220, (V)2.0d))))));
foreach (var row_233 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"schedule"))))))) {
V v_234 = row_233[0];
V v_235 = row_233[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(!R.Truth(R.Get(v_235, (V)"floor_id"))), () => (V)(R.Get(v_235, (V)"floor_id") == R.Get(v_215, (V)"active_floor_id"))))));
}
R.Call(v_72, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_215))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_215)))))))));
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"a real Temporal Echo stays pending on its source floor through stairs and save"}, new Results(new V((RuleFunction)(args_236 => {

var values_237 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_238 = values_237[0];
R.Call(R.Get(v_2, (V)"set_boon_stacks"), R.Join(new V[] {v_238, R.Get(v_238, (V)"player_id"), (V)"core:boon/temporal_echo", (V)"common"}, new Results((V)1.0d)));
var values_239 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_238))));
V v_240 = values_239[0];
V v_241 = values_239[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_238}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (-v_240)), new Entry((V)"dy", (-v_241))))))));
var values_242 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_238))).First, (V)1.0d)));
V v_243 = values_242[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_243, () => (V)(R.Get(v_243, (V)"boon_id") == (V)"core:boon/temporal_echo")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_238}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_240), new Entry((V)"dy", v_241)))))));
var values_244 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_238, (V)"floor_order"), (V)1.0d)));
V v_245 = values_244[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_238, (V)"active_floor_index") == (V)2.0d), () => R.Get(R.Get(v_238, (V)"delayed_effects"), R.Get(v_243, (V)"id"))))));
var values_246 = Results.Empty;
V v_247 = values_246[0];
foreach (var row_248 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_238, (V)"floor_pause"), v_245), (V)"schedule"))))))) {
V v_249 = row_248[0];
V v_250 = row_248[1];
if (R.Truth((V)(R.Get(v_250, (V)"id") == R.Get(v_243, (V)"id")))) {
var values_251 = R.Join(new V[] {}, new Results(v_250));
v_247 = values_251[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_247, () => (V)(R.Get(v_247, (V)"floor_id") == v_245)), () => (V)(R.Get(v_247, (V)"due") == R.Get(v_243, (V)"due"))))));
var values_252 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_238))))));
V v_253 = values_252[0];
var values_254 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_238}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_255 = values_254[0];
V v_256 = values_254[1];
V v_257 = values_254[2];
var values_258 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_253}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_259 = values_258[0];
V v_260 = values_258[1];
V v_261 = values_258[2];
R.Call(v_72, R.Join(new V[] {v_257}, new Results(v_261)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_238, (V)"delayed_effects"), R.Get(v_243, (V)"id")), () => R.Get(R.Get(v_253, (V)"delayed_effects"), R.Get(v_243, (V)"id"))))));
R.Call(v_72, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_238))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_253)))));
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"locked upper-floor windup keeps its cells and remaining wait after a return"}, new Results(new V((RuleFunction)(args_262 => {

var values_263 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_264 = values_263[0];
var values_265 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_264))));
V v_266 = values_265[0];
V v_267 = values_265[1];
var values_268 = Results.Empty;
V v_269 = values_268[0];
foreach (var row_270 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_264, (V)"entities"))))))) {
V v_271 = row_270[0];
V v_272 = row_270[1];
if (R.Truth(R.And((V)(R.Get(v_272, (V)"controller") == (V)"ai"), () => (V)(R.Get(R.Get(v_272, (V)"position"), (V)"floor_id") == R.Get(v_264, (V)"active_floor_id"))))) {
var values_273 = R.Join(new V[] {}, new Results(v_272));
v_269 = values_273[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_269)));
R.Call(R.Get(v_16, (V)"remove_for_actor"), R.Join(new V[] {v_264}, new Results(R.Get(v_269, (V)"id"))));
var values_274 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_264, (V)"active_floor_id")), new Entry((V)"x", R.Get(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_264))).First, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_264))).First, (V)"position"), (V)"y")))));
V v_275 = values_274[0];
var values_276 = R.Join(new V[] {}, R.Call(R.Get(v_16, (V)"enqueue"), R.Join(new V[] {v_264}, new Results(R.Table(new Entry((V)"kind", (V)"impact"), new Entry((V)"due", (V)1000.0d), new Entry((V)"actor_id", R.Get(v_269, (V)"id")), new Entry((V)"attack_id", (V)999.0d))))));
V v_277 = values_276[0];
var values_278 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schedule_id", R.Get(v_277, (V)"id")), new Entry((V)"sequence", R.Get(v_277, (V)"seq")), new Entry((V)"action_id", (V)999.0d), new Entry((V)"source_id", R.Get(v_269, (V)"id")), new Entry((V)"floor_id", R.Get(v_264, (V)"active_floor_id")), new Entry((V)"source_x", R.Get(R.Get(v_269, (V)"position"), (V)"x")), new Entry((V)"source_y", R.Get(R.Get(v_269, (V)"position"), (V)"y")), new Entry((V)"ability_id", (V)"core:ability/guard_strike"), new Entry((V)"target_cells", R.Table(new Entry((V)1, v_275))), new Entry((V)"ranged", (V)false), new Entry((V)"damage", R.Get(v_269, (V)"damage")), new Entry((V)"due", (V)1000.0d), new Entry((V)"committed_time", (V)0.0d))));
var obj_279 = v_269;
V key_280 = (V)"pending_attack";
R.Set(obj_279, key_280, values_278[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_264}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_266), new Entry((V)"dy", v_267)))))));
{ double end_283 = R.Num((V)2.0d), step_284 = R.Num((V)1.0d);
for (double i_282 = R.Num((V)1.0d); step_284 > 0 ? i_282 <= end_283 : i_282 >= end_283; i_282 += step_284) {
V v_281 = i_282;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_264}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_264, (V)"clock") == (V)300.0d), () => (V)(R.Get(R.Get(v_269, (V)"pending_attack"), (V)"due") == (V)1000.0d)))));
R.Call(v_72, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_264))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_264)))))))));
var values_285 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_264))));
v_266 = values_285[0];
v_267 = values_285[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_264}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_266), new Entry((V)"dy", v_267)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_264, (V)"active_floor_index") == (V)1.0d), () => (V)(R.Get(v_264, (V)"clock") == (V)400.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_269, (V)"pending_attack"), (V)"due") == (V)1300.0d), () => (V)(R.Get(R.Get(R.Get(R.Get(v_269, (V)"pending_attack"), (V)"target_cells"), (V)1.0d), (V)"floor_id") == R.Get(v_275, (V)"floor_id"))), () => (V)(R.Get(R.Get(R.Get(R.Get(v_269, (V)"pending_attack"), (V)"target_cells"), (V)1.0d), (V)"x") == R.Get(v_275, (V)"x"))), () => (V)(R.Get(R.Get(R.Get(R.Get(v_269, (V)"pending_attack"), (V)"target_cells"), (V)1.0d), (V)"y") == R.Get(v_275, (V)"y"))))));
R.Call(v_72, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_264))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_264)))))))));
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"extraction pursuer and deadline survive stair ping-pong without duplication"}, new Results(new V((RuleFunction)(args_286 => {

var values_287 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_288 = values_287[0];
var values_289 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_288))));
V v_290 = values_289[0];
V v_291 = values_289[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_288}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_290), new Entry((V)"dy", v_291)))))));
var values_292 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_288}, new Results(R.Get(R.Get(v_288, (V)"stage"), (V)"elite_id")))))));
V v_293 = values_292[0];
var values_294 = R.Join(new V[] {}, R.Call(v_44, R.Join(new V[] {v_288}, new Results(v_293))));
v_290 = values_294[0];
v_291 = values_294[1];
var values_295 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_288))));
V v_296 = values_295[0];
var values_297 = R.Join(new V[] {(V)999.0d}, new Results((V)999.0d));
var obj_298 = v_296;
V key_299 = (V)"max_hp";
var obj_300 = v_296;
V key_301 = (V)"hp";
R.Set(obj_298, key_299, values_297[0]);
R.Set(obj_300, key_301, values_297[1]);
var values_302 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_293, (V)"hp") + R.Get(v_293, (V)"defense")) + (V)20.0d)));
var obj_303 = v_296;
V key_304 = (V)"damage";
R.Set(obj_303, key_304, values_302[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_288}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_290), new Entry((V)"dy", v_291)))))));
var values_305 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_288, (V)"stage"), (V)"deadline")))));
V v_306 = values_305[0];
{ double end_309 = R.Num((V)40.0d), step_310 = R.Num((V)1.0d);
for (double i_308 = R.Num((V)1.0d); step_310 > 0 ? i_308 <= end_309 : i_308 >= end_309; i_308 += step_310) {
V v_307 = i_308;
if (R.Truth(R.Get(R.Get(v_288, (V)"stage"), (V)"pursuer_id"))) {
break;
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_288}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}
var values_311 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_288, (V)"stage"), (V)"pursuer_id")))));
V v_312 = values_311[0];
var values_313 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_288}, new Results(v_312))))));
V v_314 = values_313[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_288, (V)"clock") >= v_306), () => (V)(R.Get(R.Get(v_314, (V)"position"), (V)"floor_id") == R.Get(v_288, (V)"active_floor_id"))))));
var values_315 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_288))));
v_290 = values_315[0];
v_291 = values_315[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_288}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_290), new Entry((V)"dy", v_291)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_288, (V)"active_floor_index") == (V)1.0d), () => (V)(R.Get(R.Get(v_288, (V)"stage"), (V)"deadline") == v_306)), () => (V)(R.Get(R.Get(v_288, (V)"stage"), (V)"pursuer_id") == v_312)), () => (V)(R.Get(R.Get(v_314, (V)"position"), (V)"floor_id") == R.Get(v_288, (V)"active_floor_id"))))));
var values_316 = R.Join(new V[] {}, new Results((V)0.0d));
V v_317 = values_316[0];
foreach (var row_318 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_288, (V)"entities"))))))) {
V v_319 = row_318[0];
V v_320 = row_318[1];
if (R.Truth(R.Get(v_320, (V)"invulnerable"))) {
var values_321 = R.Join(new V[] {}, new Results((V)(v_317 + (V)1.0d)));
v_317 = values_321[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_317 == (V)1.0d))));
var values_322 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_288))));
v_290 = values_322[0];
v_291 = values_322[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_288}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_290), new Entry((V)"dy", v_291)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_288, (V)"active_floor_index") == (V)2.0d), () => (V)(R.Get(R.Get(v_288, (V)"stage"), (V)"deadline") == v_306)), () => (V)(R.Get(R.Get(v_288, (V)"stage"), (V)"pursuer_id") == v_312)), () => (V)(R.Get(R.Get(v_314, (V)"position"), (V)"floor_id") == R.Get(v_288, (V)"active_floor_id"))))));
R.Call(v_72, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_288))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_288)))))))));
return Results.Empty;
})))));
R.Call(v_71, R.Join(new V[] {(V)"occupied stair landing chooses a reachable neighbor or rejects at zero cost"}, new Results(new V((RuleFunction)(args_323 => {

var values_324 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_325 = values_324[0];
var values_326 = R.Join(new V[] {}, R.Call(v_17, R.Join(new V[] {}, new Results(v_325))));
V v_327 = values_326[0];
V v_328 = values_326[1];
var values_329 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_325, (V)"generated_map"), (V)"placements"), (V)"stairs"), (V)1.0d)));
V v_330 = values_329[0];
var values_331 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_325, (V)"inactive_floor_maps"), R.Get(v_330, (V)"to_floor_id"))));
V v_332 = values_331[0];
V v_333 = V.Nil;
v_333 = new V((RuleFunction)(args_334 => {
V v_335 = args_334[0];
V v_336 = args_334[1];
var values_337 = R.Join(new V[] {}, new Results(R.Get(v_325, (V)"next_entity_id")));
V v_338 = values_337[0];
var values_339 = R.Join(new V[] {}, new Results((V)(v_338 + (V)1.0d)));
var obj_340 = v_325;
V key_341 = (V)"next_entity_id";
R.Set(obj_340, key_341, values_339[0]);
var values_342 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_338), new Entry((V)"controller", (V)"fixture"), new Entry((V)"faction", (V)"neutral"), new Entry((V)"blocks_movement", (V)true), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_332, (V)"floor_id")), new Entry((V)"x", v_335), new Entry((V)"y", v_336))))));
var obj_343 = R.Get(v_325, (V)"entities");
V key_344 = (V)((V)R.Length(R.Get(v_325, (V)"entities")) + (V)1.0d);
R.Set(obj_343, key_344, values_342[0]);
return Results.Empty;
}));
R.Call(v_333, R.Join(new V[] {R.Get(v_330, (V)"to_x")}, new Results(R.Get(v_330, (V)"to_y"))));
var values_345 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {v_325, (V)"move", v_327}, new Results(v_328))));
V v_346 = values_345[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_346, (V)"valid"), () => R.Get(v_346, (V)"landing")), () => R.Or((V)(R.Get(R.Get(v_346, (V)"landing"), (V)"x") != R.Get(v_330, (V)"to_x")), () => (V)(R.Get(R.Get(v_346, (V)"landing"), (V)"y") != R.Get(v_330, (V)"to_y")))))));
{ double end_349 = R.Num((V)3.0d), step_350 = R.Num((V)1.0d);
for (double i_348 = R.Num((-(V)3.0d)); step_350 > 0 ? i_348 <= end_349 : i_348 >= end_349; i_348 += step_350) {
V v_347 = i_348;
{ double end_353 = R.Num((V)3.0d), step_354 = R.Num((V)1.0d);
for (double i_352 = R.Num((-(V)3.0d)); step_354 > 0 ? i_352 <= end_353 : i_352 >= end_353; i_352 += step_354) {
V v_351 = i_352;
if (R.Truth(R.And(R.And((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_351))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_347))).First) <= (V)3.0d), () => R.Or((V)(v_351 != (V)0.0d), () => (V)(v_347 != (V)0.0d))), () => R.Call(R.Get(v_8, (V)"walkable"), R.Join(new V[] {v_332, (V)(R.Get(v_330, (V)"to_x") + v_351)}, new Results((V)(R.Get(v_330, (V)"to_y") + v_347)))).First))) {
R.Call(v_333, R.Join(new V[] {(V)(R.Get(v_330, (V)"to_x") + v_351)}, new Results((V)(R.Get(v_330, (V)"to_y") + v_347))));
}
}
}
}
}
var values_355 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_325))));
V v_356 = values_355[0];
var values_357 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_325}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_327), new Entry((V)"dy", v_328))))));
V v_358 = values_357[0];
V v_359 = values_357[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(v_358)), () => (V)(v_359 == (V)"stair landing blocked")), () => (V)(R.Get(v_325, (V)"clock") == (V)0.0d)))));
R.Call(v_72, R.Join(new V[] {v_356}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_325)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
