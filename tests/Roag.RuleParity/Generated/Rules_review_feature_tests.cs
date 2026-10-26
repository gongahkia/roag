// Static C# port of tests/review_feature_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 5eb16402aef4f7e6a1cba8d3d00f4ccf46a24d7b170a4e903a922d2a50da216f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_review_feature_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_7 = values_6[0];
V v_8 = V.Nil;
v_8 = new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
foreach (var row_12 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_10, (V)"review_features"))))))) {
V v_13 = row_12[0];
V v_14 = row_12[1];
if (R.Truth(R.And((V)(R.Get(v_14, (V)"kind") == v_11), () => (V)(R.Get(v_14, (V)"floor_id") == R.Get(v_10, (V)"active_floor_id"))))) {
return R.Join(new V[] {}, new Results(v_14));
}
}
return Results.Empty;
}));
V v_15 = V.Nil;
v_15 = new V((RuleFunction)(args_16 => {
V v_17 = args_16[0];
V v_18 = args_16[1];
foreach (var row_19 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_17, (V)"entities"))))))) {
V v_20 = row_19[0];
V v_21 = row_19[1];
if (R.Truth(R.And((V)(R.Get(v_21, (V)"controller") == v_18), () => (V)(R.Get(R.Get(v_21, (V)"position"), (V)"floor_id") == R.Get(v_17, (V)"active_floor_id"))))) {
return R.Join(new V[] {}, new Results(v_21));
}
}
return Results.Empty;
}));
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
V v_25 = args_23[1];
V v_26 = args_23[2];
V v_27 = args_23[3];
var values_28 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_24))));
V v_29 = values_28[0];
foreach (var row_30 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_31 = row_30[0];
V v_32 = row_30[1];
var values_33 = R.Join(new V[] {(V)(v_25 - R.Get(v_32, (V)1.0d))}, new Results((V)(v_26 - R.Get(v_32, (V)2.0d))));
V v_34 = values_33[0];
V v_35 = values_33[1];
if (R.Truth(R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_7, (V)"map_for"), R.Join(new V[] {}, new Results(v_24))).First, v_34}, new Results(v_35))).First)) {
var values_36 = R.Join(new V[] {v_34}, new Results(v_35));
var obj_37 = R.Get(v_29, (V)"position");
V key_38 = (V)"x";
var obj_39 = R.Get(v_29, (V)"position");
V key_40 = (V)"y";
R.Set(obj_37, key_38, values_36[0]);
R.Set(obj_39, key_40, values_36[1]);
var values_41 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_24, (V)"move", R.Get(v_32, (V)1.0d)}, new Results(R.Get(v_32, (V)2.0d)))));
V v_42 = values_41[0];
if (R.Truth(R.And(R.Get(v_42, (V)"valid"), () => R.Or((V)(!R.Truth(v_27)), () => R.Get(v_42, (V)"vault_id"))))) {
return R.Join(new V[] {}, new Results(v_32));
}
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no reachable neighbor for review object")));
return Results.Empty;
}));
V v_43 = V.Nil;
v_43 = new V((RuleFunction)(args_44 => {
V v_45 = args_44[0];
V v_46 = args_44[1];
foreach (var row_47 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_45)))))) {
V v_48 = row_47[0];
V v_49 = row_47[1];
if (R.Truth((V)(R.Get(v_49, (V)"kind") == v_46))) {
return R.Join(new V[] {}, new Results(v_49));
}
}
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"optional vault key, four outcomes and exact saved state"}, new Results(new V((RuleFunction)(args_50 => {

foreach (var row_51 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)7.0d), new Entry((V)2, (V)"clue"))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)"cache"))), new Entry((V)3, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)"ambush"))), new Entry((V)4, R.Table(new Entry((V)1, (V)8.0d), new Entry((V)2, (V)"boon")))))))))) {
V v_52 = row_51[0];
V v_53 = row_51[1];
var values_54 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results(R.Get(v_53, (V)1.0d)))));
V v_55 = values_54[0];
var values_56 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_8, R.Join(new V[] {v_55}, new Results((V)"key"))))));
V v_57 = values_56[0];
var values_58 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_15, R.Join(new V[] {v_55}, new Results((V)"review_vault"))))));
V v_59 = values_58[0];
var values_60 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_55, R.Get(v_57, (V)"x")}, new Results(R.Get(v_57, (V)"y")))));
V v_61 = values_60[0];
var values_62 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_55}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_61, (V)1.0d)), new Entry((V)"dy", R.Get(v_61, (V)2.0d)))))));
V v_63 = values_62[0];
V v_64 = values_62[1];
V v_65 = values_62[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_63}, new Results(v_64)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(v_43, R.Join(new V[] {v_65}, new Results((V)"VaultKeyCollected"))).First, () => R.Get(R.Get(v_55, (V)"review_keys"), R.Get(v_55, (V)"active_floor_id"))))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_55))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_55)))));
var values_66 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_55, R.Get(R.Get(v_59, (V)"position"), (V)"x"), R.Get(R.Get(v_59, (V)"position"), (V)"y")}, new Results((V)true))));
v_61 = values_66[0];
var values_67 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_55}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_61, (V)1.0d)), new Entry((V)"dy", R.Get(v_61, (V)2.0d)))))));
v_63 = values_67[0];
v_64 = values_67[1];
v_65 = values_67[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_63}, new Results(v_64)));
var values_68 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_43, R.Join(new V[] {v_65}, new Results((V)"VaultOpened"))))));
V v_69 = values_68[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_69, (V)"outcome") == R.Get(v_53, (V)2.0d)), () => R.Get(v_59, (V)"opened")), () => (V)(!R.Truth(R.Get(R.Get(v_55, (V)"review_keys"), R.Get(v_55, (V)"active_floor_id"))))))));
if (R.Truth((V)(R.Get(v_53, (V)2.0d) == (V)"boon"))) {
var values_70 = R.Join(new V[] {}, new Results((V)false));
V v_71 = values_70[0];
foreach (var row_72 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_55, (V)"entities"))))))) {
V v_73 = row_72[0];
V v_74 = row_72[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_74, (V)"controller") == (V)"chest"), () => (V)(R.Get(v_74, (V)"price") == (V)0.0d)), () => (V)(R.Get(R.Get(v_74, (V)"position"), (V)"x") == R.Get(R.Get(v_59, (V)"position"), (V)"x"))), () => (V)(R.Get(R.Get(v_74, (V)"position"), (V)"y") == R.Get(R.Get(v_59, (V)"position"), (V)"y"))))) {
var values_75 = R.Join(new V[] {}, new Results((V)true));
v_71 = values_75[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_71}, new Results((V)"boon vault did not provide a free chest")));
} else if (R.Truth((V)(R.Get(v_53, (V)2.0d) == (V)"ambush"))) {
var values_76 = R.Join(new V[] {}, new Results((V)false));
V v_77 = values_76[0];
foreach (var row_78 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_55, (V)"entities"))))))) {
V v_79 = row_78[0];
V v_80 = row_78[1];
if (R.Truth(R.And((V)(R.Get(v_80, (V)"controller") == (V)"ai"), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_80, (V)"position"), (V)"x") - R.Get(R.Get(v_59, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_80, (V)"position"), (V)"y") - R.Get(R.Get(v_59, (V)"position"), (V)"y"))))).First) == (V)1.0d)))) {
var values_81 = R.Join(new V[] {}, new Results((V)true));
v_77 = values_81[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_77}, new Results((V)"ambush vault had no enemy")));
} else {
var values_82 = R.Join(new V[] {}, new Results((V)false));
V v_83 = values_82[0];
foreach (var row_84 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_55, (V)"drops"))))))) {
V v_85 = row_84[0];
V v_86 = row_84[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_86, (V)"kind") == (V)"currency"), () => (V)(R.Get(R.Get(v_86, (V)"position"), (V)"x") == R.Get(R.Get(v_59, (V)"position"), (V)"x"))), () => (V)(R.Get(R.Get(v_86, (V)"position"), (V)"y") == R.Get(R.Get(v_59, (V)"position"), (V)"y"))))) {
var values_87 = R.Join(new V[] {}, new Results((V)true));
v_83 = values_87[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_83}, new Results((V)"reward vault had no currency")));
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_55))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_55)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"closed review objects leave the required elite route reachable"}, new Results(new V((RuleFunction)(args_88 => {

{ double end_91 = R.Num((V)20.0d), step_92 = R.Num((V)1.0d);
for (double i_90 = R.Num((V)1.0d); step_92 > 0 ? i_90 <= end_91 : i_90 >= end_91; i_90 += step_92) {
V v_89 = i_90;
var values_93 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results(v_89))));
V v_94 = values_93[0];
var values_95 = R.Join(new V[] {}, new Results(R.Get(v_94, (V)"generated_map")));
V v_96 = values_95[0];
var values_97 = R.Join(new V[] {}, new Results(R.Table()));
V v_98 = values_97[0];
var values_99 = R.Join(new V[] {}, new Results(R.Table()));
V v_100 = values_99[0];
foreach (var row_101 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_96, (V)"corridor_quests"), () => R.Table()))))))) {
V v_102 = row_101[0];
V v_103 = row_101[1];
foreach (var row_104 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_103, (V)"cells"))))))) {
V v_105 = row_104[0];
V v_106 = row_104[1];
var values_107 = R.Join(new V[] {}, new Results((V)true));
var obj_108 = v_100;
V key_109 = R.Concat(R.Get(v_106, (V)"x"), R.Concat((V)",", R.Get(v_106, (V)"y")));
R.Set(obj_108, key_109, values_107[0]);
}
}
foreach (var row_110 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_94, (V)"entities"))))))) {
V v_111 = row_110[0];
V v_112 = row_110[1];
if (R.Truth(R.And(R.Get(v_112, (V)"blocks_movement"), () => R.Invoke(R.Get(v_112, (V)"controller"), "match", R.Join(new V[] {}, new Results((V)"^review_"))).First))) {
var values_113 = R.Join(new V[] {}, new Results((V)true));
var obj_114 = v_98;
V key_115 = R.Concat(R.Get(R.Get(v_112, (V)"position"), (V)"x"), R.Concat((V)",", R.Get(R.Get(v_112, (V)"position"), (V)"y")));
R.Set(obj_114, key_115, values_113[0]);
}
}
var values_116 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_96, (V)"start_x")), new Entry((V)"y", R.Get(v_96, (V)"start_y")))))));
V v_117 = values_116[0];
var values_118 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Concat(R.Get(v_96, (V)"start_x"), R.Concat((V)",", R.Get(v_96, (V)"start_y"))), (V)true))));
V v_119 = values_118[0];
var values_120 = R.Join(new V[] {}, new Results((V)1.0d));
V v_121 = values_120[0];
while (R.Truth((V)(v_121 <= (V)R.Length(v_117)))) {
var values_122 = R.Join(new V[] {}, new Results(R.Get(v_117, v_121)));
V v_123 = values_122[0];

var values_124 = R.Join(new V[] {}, new Results((V)(v_121 + (V)1.0d)));
v_121 = values_124[0];
foreach (var row_125 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_126 = row_125[0];
V v_127 = row_125[1];
var values_128 = R.Join(new V[] {(V)(R.Get(v_123, (V)"x") + R.Get(v_127, (V)1.0d))}, new Results((V)(R.Get(v_123, (V)"y") + R.Get(v_127, (V)2.0d))));
V v_129 = values_128[0];
V v_130 = values_128[1];
var values_131 = R.Join(new V[] {}, new Results(R.Concat(v_129, R.Concat((V)",", v_130))));
V v_132 = values_131[0];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_119, v_132))), () => (V)(!R.Truth(R.Get(v_98, v_132)))), () => R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_96, v_129}, new Results(v_130))).First))) {
var values_133 = R.Join(new V[] {}, new Results((V)true));
var obj_134 = v_119;
V key_135 = v_132;
R.Set(obj_134, key_135, values_133[0]);

var values_136 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_129), new Entry((V)"y", v_130))));
var obj_137 = v_117;
V key_138 = (V)((V)R.Length(v_117) + (V)1.0d);
R.Set(obj_137, key_138, values_136[0]);
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_119, R.Concat(R.Get(R.Get(v_96, (V)"elite"), (V)"x"), R.Concat((V)",", R.Get(R.Get(v_96, (V)"elite"), (V)"y"))))}, new Results(R.Concat((V)"review objects blocked the main route for seed ", v_89))));
var values_139 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_96, (V)"start_x")), new Entry((V)"y", R.Get(v_96, (V)"start_y")))))));
V v_140 = values_139[0];
var values_141 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Concat(R.Get(v_96, (V)"start_x"), R.Concat((V)",", R.Get(v_96, (V)"start_y"))), (V)true))));
V v_142 = values_141[0];
var values_143 = R.Join(new V[] {}, new Results((V)1.0d));
V v_144 = values_143[0];
while (R.Truth((V)(v_144 <= (V)R.Length(v_140)))) {
var values_145 = R.Join(new V[] {}, new Results(R.Get(v_140, v_144)));
V v_146 = values_145[0];

var values_147 = R.Join(new V[] {}, new Results((V)(v_144 + (V)1.0d)));
v_144 = values_147[0];
foreach (var row_148 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_149 = row_148[0];
V v_150 = row_148[1];
var values_151 = R.Join(new V[] {(V)(R.Get(v_146, (V)"x") + R.Get(v_150, (V)1.0d))}, new Results((V)(R.Get(v_146, (V)"y") + R.Get(v_150, (V)2.0d))));
V v_152 = values_151[0];
V v_153 = values_151[1];
var values_154 = R.Join(new V[] {}, new Results(R.Concat(v_152, R.Concat((V)",", v_153))));
V v_155 = values_154[0];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_142, v_155))), () => R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_96, v_152}, new Results(v_153))).First))) {
var values_156 = R.Join(new V[] {}, new Results((V)true));
var obj_157 = v_142;
V key_158 = v_155;
R.Set(obj_157, key_158, values_156[0]);
var values_159 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_152), new Entry((V)"y", v_153))));
var obj_160 = v_140;
V key_161 = (V)((V)R.Length(v_140) + (V)1.0d);
R.Set(obj_160, key_161, values_159[0]);
}
}
}
foreach (var row_162 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_142)))))) {
V v_163 = row_162[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or(R.Get(v_98, v_163), () => R.Get(v_119, v_163)), () => R.Get(v_100, v_163))}, new Results(R.Concat((V)"review objects isolated a reachable tile for seed ", v_89))));
}
var values_164 = R.Join(new V[] {}, R.Call(v_8, R.Join(new V[] {v_94}, new Results((V)"plate"))));
V v_165 = values_164[0];
if (R.Truth(v_165)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_119, R.Concat(R.Get(v_165, (V)"x"), R.Concat((V)",", R.Get(v_165, (V)"y"))))}, new Results(R.Concat((V)"closed gate hides its own switch for seed ", v_89))));
}
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"plate opens a timed gate and expiration survives Continue"}, new Results(new V((RuleFunction)(args_166 => {

var values_167 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_168 = values_167[0];
var values_169 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_8, R.Join(new V[] {v_168}, new Results((V)"plate"))))));
V v_170 = values_169[0];
var values_171 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_168}, new Results(R.Get(v_170, (V)"gate_id")))))));
V v_172 = values_171[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_172, (V)"controller") == (V)"review_gate"), () => (V)(!R.Truth(R.Get(v_172, (V)"open")))))));
var values_173 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_168, R.Get(v_170, (V)"x")}, new Results(R.Get(v_170, (V)"y")))));
V v_174 = values_173[0];
var values_175 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_168}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_174, (V)1.0d)), new Entry((V)"dy", R.Get(v_174, (V)2.0d)))))));
V v_176 = values_175[0];
V v_177 = values_175[1];
V v_178 = values_175[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_176}, new Results(v_177)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Call(v_43, R.Join(new V[] {v_178}, new Results((V)"TimedGateOpened"))).First, () => R.Get(v_172, (V)"open")), () => (V)(R.Get(v_172, (V)"closes_at") > R.Get(v_168, (V)"clock"))))));
var values_179 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_168))))));
v_168 = values_179[0];

var values_180 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_168}, new Results(R.Get(v_170, (V)"gate_id")))));
v_172 = values_180[0];
{ double end_183 = R.Num((V)4.0d), step_184 = R.Num((V)1.0d);
for (double i_182 = R.Num((V)1.0d); step_184 > 0 ? i_182 <= end_183 : i_182 >= end_183; i_182 += step_184) {
V v_181 = i_182;
R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_168}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(v_172, (V)"open"))), () => R.Get(v_172, (V)"blocks_movement")), () => (V)(R.Get(v_172, (V)"closes_at") == V.Nil)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ore, ward and scenery placement retain their independent states"}, new Results(new V((RuleFunction)(args_185 => {

var values_186 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_187 = values_186[0];
var values_188 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_8, R.Join(new V[] {v_187}, new Results((V)"ore"))))));
V v_189 = values_188[0];
var values_190 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_187))));
V v_191 = values_190[0];
var values_192 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_187, R.Get(v_189, (V)"x")}, new Results(R.Get(v_189, (V)"y")))));
V v_193 = values_192[0];
var values_194 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_187}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_193, (V)1.0d)), new Entry((V)"dy", R.Get(v_193, (V)2.0d)))))));
V v_195 = values_194[0];
V v_196 = values_194[1];
V v_197 = values_194[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_195}, new Results(v_196)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_189, (V)"broken"), () => R.Call(v_43, R.Join(new V[] {v_197}, new Results((V)"OreBroken"))).First))));
var values_198 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_8, R.Join(new V[] {v_187}, new Results((V)"ward"))))));
V v_199 = values_198[0];
var values_200 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {v_187, R.Get(v_199, (V)"x")}, new Results(R.Get(v_199, (V)"y")))));
v_193 = values_200[0];
var values_201 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_187}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_193, (V)1.0d)), new Entry((V)"dy", R.Get(v_193, (V)2.0d)))))));
v_195 = values_201[0];
v_196 = values_201[1];
v_197 = values_201[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_195}, new Results(v_196)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_199, (V)"collected"), () => R.Call(v_43, R.Join(new V[] {v_197}, new Results((V)"WardFlaskCollected"))).First), () => (V)(R.Get(v_191, (V)"pickup_ward_actions") == (V)3.0d)))));
{ double end_204 = R.Num((V)3.0d), step_205 = R.Num((V)1.0d);
for (double i_203 = R.Num((V)1.0d); step_205 > 0 ? i_203 <= end_204 : i_203 >= end_204; i_203 += step_205) {
V v_202 = i_203;
R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_187}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_191, (V)"pickup_ward_actions") == (V)0.0d))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_187))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_187)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new class actions use the committed attack and movement scheduler"}, new Results(new V((RuleFunction)(args_206 => {

var values_207 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"shade"), new Entry((V)2, (V)"hookrunner"), new Entry((V)3, (V)"trapper"))));
V v_208 = values_207[0];
foreach (var row_209 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_208)))))) {
V v_210 = row_209[0];
V v_211 = row_209[1];
var values_212 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)7.0d, (V)"core:map/test_room"}, new Results(R.Table(new Entry((V)"class_id", R.Concat((V)"core:actor/", v_211)))))));
V v_213 = values_212[0];
var values_214 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_213, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_215 = values_214[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_215, (V)"valid"))));
var values_216 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_213}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_217 = values_216[0];
V v_218 = values_216[1];
V v_219 = values_216[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_217}, new Results(v_218)));
if (R.Truth((V)(v_211 == (V)"shade"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(v_43, R.Join(new V[] {v_219}, new Results((V)"DecoyPlaced"))).First, () => R.Get(R.Get(v_213, (V)"class_special"), (V)"decoy")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_213, (V)"dash", (V)1.0d}, new Results((V)0.0d))).First, (V)"swap"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_213}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(v_213, (V)"class_special"), (V)"decoy"))))));
} else if (R.Truth((V)(v_211 == (V)"trapper"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(v_43, R.Join(new V[] {v_219}, new Results((V)"SnarePlaced"))).First, () => R.Get(R.Get(v_213, (V)"class_special"), (V)"snare")))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_43, R.Join(new V[] {v_219}, new Results((V)"AttackPerformed")))));
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_213))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_213)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"hook pulls an ordinary foe while a snare deals damage and skips one move"}, new Results(new V((RuleFunction)(args_220 => {

var values_221 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_222 = values_221[0];
var values_223 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)7.0d, (V)"core:map/test_room"}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/hookrunner"))))));
V v_224 = values_223[0];
var values_225 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_224, (V)"core:enemy/mossbound_guard", (V)5.0d}, new Results((V)2.0d))));
V v_226 = values_225[0];
R.Call(R.Get(v_222, (V)"remove_for_actor"), R.Join(new V[] {v_224}, new Results(R.Get(v_226, (V)"id"))));
var values_227 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_224}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_228 = values_227[0];
V v_229 = values_227[1];
V v_230 = values_227[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_228}, new Results(v_229)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_226, (V)"position"), (V)"x") == (V)3.0d), () => (V)(R.Get(R.Get(v_226, (V)"position"), (V)"y") == (V)2.0d)), () => R.Call(v_43, R.Join(new V[] {v_230}, new Results((V)"Displaced"))).First), () => (V)(R.Get(v_226, (V)"hp") < R.Get(v_226, (V)"max_hp"))))));
var values_231 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)7.0d, (V)"core:map/test_room"}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/trapper"))))));
V v_232 = values_231[0];
var values_233 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_232, (V)"core:enemy/mossbound_guard", (V)5.0d}, new Results((V)2.0d))));
V v_234 = values_233[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_232}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_235 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_232}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
v_228 = values_235[0];
v_229 = values_235[1];
v_230 = values_235[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_228}, new Results(v_229)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Call(v_43, R.Join(new V[] {v_230}, new Results((V)"SnareTriggered"))).First, () => (V)(R.Get(R.Get(v_234, (V)"position"), (V)"x") == (V)3.0d)), () => (V)(R.Get(v_234, (V)"hp") < R.Get(v_234, (V)"max_hp"))), () => R.Get(v_234, (V)"skip_next_turn")))));
var values_236 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_232}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
v_228 = values_236[0];
v_229 = values_236[1];
v_230 = values_236[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_228}, new Results(v_229)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Call(v_43, R.Join(new V[] {v_230}, new Results((V)"SnareHeld"))).First, () => (V)(R.Get(R.Get(v_234, (V)"position"), (V)"x") == (V)3.0d)), () => (V)(!R.Truth(R.Get(v_234, (V)"skip_next_turn")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ward flask consumes one entire hostile hit before ordinary defense"}, new Results(new V((RuleFunction)(args_237 => {

var values_238 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)7.0d))));
V v_239 = values_238[0];
var values_240 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_239))));
V v_241 = values_240[0];
var values_242 = R.Join(new V[] {(V)8.0d}, new Results((V)5.0d));
var obj_243 = R.Get(v_241, (V)"position");
V key_244 = (V)"x";
var obj_245 = R.Get(v_241, (V)"position");
V key_246 = (V)"y";
R.Set(obj_243, key_244, values_242[0]);
R.Set(obj_245, key_246, values_242[1]);
var values_247 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_248 = v_241;
V key_249 = (V)"pickup_ward_actions";
R.Set(obj_248, key_249, values_247[0]);
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_239, (V)"core:enemy/thornspitter", (V)8.0d}, new Results((V)2.0d)));
var values_250 = R.Join(new V[] {}, new Results(R.Get(v_241, (V)"hp")));
V v_251 = values_250[0];
var values_252 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_239}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_253 = values_252[0];
V v_254 = values_252[1];
V v_255 = values_252[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_253}, new Results(v_254)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Call(v_43, R.Join(new V[] {v_255}, new Results((V)"WardFlaskBlocked"))).First, () => (V)(R.Get(v_241, (V)"hp") == v_251)), () => (V)(R.Get(v_241, (V)"pickup_ward_actions") == (V)0.0d)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_239))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_239)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Ring of Reversal primes on escaped windup and spends on that attacker"}, new Results(new V((RuleFunction)(args_256 => {

var values_257 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_258 = values_257[0];
var values_259 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_258))));
V v_260 = values_259[0];
var values_261 = R.Join(new V[] {(V)8.0d}, new Results((V)5.0d));
var obj_262 = R.Get(v_260, (V)"position");
V key_263 = (V)"x";
var obj_264 = R.Get(v_260, (V)"position");
V key_265 = (V)"y";
R.Set(obj_262, key_263, values_261[0]);
R.Set(obj_264, key_265, values_261[1]);
var values_266 = R.Join(new V[] {}, new Results((V)300.0d));
var obj_267 = v_260;
V key_268 = (V)"speed";
R.Set(obj_267, key_268, values_266[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_258, R.Get(v_260, (V)"id"), (V)"core:boon/ring_of_reversal", (V)"rare"}, new Results((V)1.0d)));
var values_269 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_258, (V)"core:enemy/thornspitter", (V)8.0d}, new Results((V)2.0d))));
V v_270 = values_269[0];
var values_271 = R.Join(new V[] {(V)100.0d}, new Results((V)100.0d));
var obj_272 = v_270;
V key_273 = (V)"hp";
var obj_274 = v_270;
V key_275 = (V)"max_hp";
R.Set(obj_272, key_273, values_271[0]);
R.Set(obj_274, key_275, values_271[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_258}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_270, (V)"pending_attack"))));
var values_276 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_258}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))))));
V v_277 = values_276[0];
V v_278 = values_276[1];
V v_279 = values_276[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_277}, new Results(v_278)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(v_43, R.Join(new V[] {v_279}, new Results((V)"RingReversalPrimed"))).First, () => (V)(R.Get(v_260, (V)"reversal_target_id") == R.Get(v_270, (V)"id"))))));
var values_280 = R.Join(new V[] {(V)8.0d}, new Results((V)3.0d));
var obj_281 = R.Get(v_260, (V)"position");
V key_282 = (V)"x";
var obj_283 = R.Get(v_260, (V)"position");
V key_284 = (V)"y";
R.Set(obj_281, key_282, values_280[0]);
R.Set(obj_283, key_284, values_280[1]);
var values_285 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_258}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d)))))));
v_277 = values_285[0];
v_278 = values_285[1];
v_279 = values_285[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_277}, new Results(v_278)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Call(v_43, R.Join(new V[] {v_279}, new Results((V)"RingReversalSpent"))).First, () => (V)(R.Get(v_270, (V)"hp") == (V)92.0d)), () => (V)(!R.Truth(R.Get(v_260, (V)"reversal_target_id")))))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_258))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_258)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"lit brazier clears nearby mist sight only until its clock deadline"}, new Results(new V((RuleFunction)(args_286 => {

var values_287 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/test_room"))));
V v_288 = values_287[0];
var values_289 = R.Join(new V[] {}, new Results(R.Table()));
V v_290 = values_289[0];
foreach (var row_291 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_288, (V)"rows"))))))) {
V v_292 = row_291[0];
V v_293 = row_291[1];
var values_294 = R.Join(new V[] {}, new Results(v_293));
var obj_295 = v_290;
V key_296 = v_292;
R.Set(obj_295, key_296, values_294[0]);
}
var values_297 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"rows", v_290))}, new Results(R.Table(new Entry((V)"__index", v_288))))));
v_288 = values_297[0];
var values_298 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_288, (V)"rows"), (V)5.0d), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)7.0d))).First, R.Concat((V)"m", R.Invoke(R.Get(R.Get(v_288, (V)"rows"), (V)5.0d), "sub", R.Join(new V[] {}, new Results((V)9.0d))).First))));
var obj_299 = R.Get(v_288, (V)"rows");
V key_300 = (V)5.0d;
R.Set(obj_299, key_300, values_298[0]);
var values_301 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"clock", (V)100.0d), new Entry((V)"review_features", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"brazier"), new Entry((V)"floor_id", R.Get(v_288, (V)"floor_id")), new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)5.0d), new Entry((V)"lit_until", (V)300.0d))))), new Entry((V)"entities", R.Table()))));
V v_302 = values_301[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"blocks_vision"), R.Join(new V[] {v_288, (V)8.0d, (V)5.0d}, new Results(v_302))).First)))));
var values_303 = R.Join(new V[] {}, new Results((V)300.0d));
var obj_304 = v_302;
V key_305 = (V)"clock";
R.Set(obj_304, key_305, values_303[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"blocks_vision"), R.Join(new V[] {v_288, (V)8.0d, (V)5.0d}, new Results(v_302)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
