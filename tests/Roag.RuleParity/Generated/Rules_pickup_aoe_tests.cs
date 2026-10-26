// Static C# port of tests/pickup_aoe_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 043924bc17a070b9ed28ccb216fc15a376226bdfea980420f0a159ef55eb524b
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_pickup_aoe_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievements"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_15 = values_14[0];
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
var values_20 = R.Join(new V[] {}, new Results(R.Table()));
V v_21 = values_20[0];
foreach (var row_22 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_18)))))) {
V v_23 = row_22[0];
V v_24 = row_22[1];
if (R.Truth((V)(R.Get(v_24, (V)"kind") == v_19))) {
var values_25 = R.Join(new V[] {}, new Results(v_24));
var obj_26 = v_21;
V key_27 = (V)((V)R.Length(v_21) + (V)1.0d);
R.Set(obj_26, key_27, values_25[0]);
}
}
return R.Join(new V[] {}, new Results(v_21));
return Results.Empty;
}));
V v_28 = V.Nil;
v_28 = new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
return R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)9.0d, (V)"core:map/test_room"}, new Results(R.Table(new Entry((V)"class_id", v_30), new Entry((V)"available_boons", R.Call(R.Get(v_13, (V)"pool"), R.Join(new V[] {}, new Results(R.Table()))).First))))));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Sentinel and Alchemist are selectable plain-data classes with distinct area kits"}, new Results(new V((RuleFunction)(args_31 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_7, (V)"validate"), Results.Empty).First, () => (V)((V)R.Length(R.Get(v_7, (V)"order")) == (V)15.0d)))));
var values_32 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"core:actor/sentinel"))));
V v_33 = values_32[0];
var values_34 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_33))));
V v_35 = values_34[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_35, (V)"defense") == (V)2.0d), () => (V)(R.Get(v_35, (V)"max_hp") == (V)29.0d)))));
var values_36 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_33, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_37 = values_36[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_37, (V)"valid"), () => (V)((V)R.Length(R.Get(v_37, (V)"cells")) == (V)8.0d)))));
var values_38 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_33, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)99999.0d))));
V v_39 = values_38[0];
var values_40 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_33, (V)"core:enemy/mossbound_guard", (V)2.0d, (V)3.0d}, new Results((V)99999.0d))));
V v_41 = values_40[0];
var values_42 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_33}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_43 = values_42[0];
V v_44 = values_42[1];
V v_45 = values_42[2];
var values_46 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_16, R.Join(new V[] {v_45}, new Results((V)"DamageBatchResolved"))).First, (V)1.0d)));
V v_47 = values_46[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_47, () => (V)((V)R.Length(R.Get(v_47, (V)"outcomes")) == (V)2.0d)), () => (V)(R.Get(v_39, (V)"hp") < R.Get(v_39, (V)"max_hp"))), () => (V)(R.Get(v_41, (V)"hp") < R.Get(v_41, (V)"max_hp"))))));
var values_48 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"core:actor/alchemist"))));
V v_49 = values_48[0];
var values_50 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_49, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_51 = values_50[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_51, (V)"valid"), () => (V)((V)R.Length(R.Get(v_51, (V)"cells")) >= (V)3.0d)))));
var values_52 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_49, (V)"core:enemy/mossbound_guard", (V)5.0d, (V)2.0d}, new Results((V)99999.0d))));
V v_53 = values_52[0];
var values_54 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_49, (V)"core:enemy/mossbound_guard", (V)6.0d, (V)2.0d}, new Results((V)99999.0d))));
V v_55 = values_54[0];
var values_56 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_49}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_57 = values_56[0];
V v_58 = values_56[1];
V v_59 = values_56[2];
var values_60 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(v_16, R.Join(new V[] {v_59}, new Results((V)"DamageBatchResolved"))).First, (V)1.0d), (V)"outcomes")));
V v_61 = values_60[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_61) == (V)2.0d), () => (V)(R.Get(v_53, (V)"hp") < R.Get(v_53, (V)"max_hp"))), () => (V)(R.Get(v_55, (V)"hp") < R.Get(v_55, (V)"max_hp"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_49)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"food and health pickups wait for injury, heal once and cap at maximum"}, new Results(new V((RuleFunction)(args_62 => {

var values_63 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_64 = values_63[0];
var values_65 = R.Join(new V[] {}, new Results(R.Table()));
V v_66 = values_65[0];
V v_67 = V.Nil;
v_67 = new V((RuleFunction)(args_68 => {
V v_69 = args_68[0];
V v_70 = args_68[1];
var values_71 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_69), new Entry((V)"fields", v_70))));
var obj_72 = v_66;
V key_73 = (V)((V)R.Length(v_66) + (V)1.0d);
R.Set(obj_72, key_73, values_71[0]);
return Results.Empty;
}));
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_supply"), R.Join(new V[] {v_64, (V)"food", R.Get(v_64, (V)"active_floor_id"), (V)3.0d, (V)2.0d}, new Results(v_67))));
V v_75 = values_74[0];
var values_76 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_supply"), R.Join(new V[] {v_64, (V)"health", R.Get(v_64, (V)"active_floor_id"), (V)4.0d, (V)2.0d}, new Results(v_67))));
V v_77 = values_76[0];
var values_78 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_64))));
V v_79 = values_78[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_75, (V)"amount") == (V)4.0d), () => (V)(R.Get(v_77, (V)"amount") == (V)10.0d)))));
var values_80 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_64}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_81 = values_80[0];
V v_82 = values_80[1];
V v_83 = values_80[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_75, (V)"collected"))), () => (V)((V)R.Length(R.Call(v_16, R.Join(new V[] {v_83}, new Results((V)"SupplyCollected"))).First) == (V)0.0d)))));
var values_84 = R.Join(new V[] {}, new Results((V)(R.Get(v_79, (V)"max_hp") - (V)7.0d)));
var obj_85 = v_79;
V key_86 = (V)"hp";
R.Set(obj_85, key_86, values_84[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_64}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))))));
var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_64}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_88 = values_87[0];
V v_89 = values_87[1];
V v_90 = values_87[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_75, (V)"collected"), () => (V)(R.Get(v_79, (V)"hp") == (V)(R.Get(v_79, (V)"max_hp") - (V)3.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_16, R.Join(new V[] {v_90}, new Results((V)"SupplyCollected"))).First, (V)1.0d), (V)"actual_gain") == (V)4.0d))));
var values_91 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_64}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_92 = values_91[0];
V v_93 = values_91[1];
V v_94 = values_91[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_77, (V)"collected"), () => (V)(R.Get(v_79, (V)"hp") == R.Get(v_79, (V)"max_hp"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(v_16, R.Join(new V[] {v_94}, new Results((V)"SupplyCollected"))).First, (V)1.0d), (V)"actual_gain") == (V)3.0d))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_64))))))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_64)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"supply rolls use an independent stable stream and elite drops health"}, new Results(new V((RuleFunction)(args_95 => {

var values_96 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)21.0d))));
V v_97 = values_96[0];
var values_98 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_97, (V)"rng"), (V)"state")));
V v_99 = values_98[0];
var values_100 = R.Join(new V[] {}, new Results(R.Table()));
V v_101 = values_100[0];
V v_102 = V.Nil;
v_102 = new V((RuleFunction)(args_103 => {
V v_104 = args_103[0];
V v_105 = args_103[1];
var values_106 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_104), new Entry((V)"fields", v_105))));
var obj_107 = v_101;
V key_108 = (V)((V)R.Length(v_101) + (V)1.0d);
R.Set(obj_107, key_108, values_106[0]);
return Results.Empty;
}));
var values_109 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"EntityKilled"), new Entry((V)"target_id", (V)991.0d), new Entry((V)"target_definition_id", (V)"core:enemy/thorn_warden"), new Entry((V)"source_id", (V)999.0d), new Entry((V)"source_faction", (V)"enemy"), new Entry((V)"floor_id", R.Get(v_97, (V)"active_floor_id")), new Entry((V)"x", R.Get(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_97))).First, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_97))).First, (V)"position"), (V)"y")))));
V v_110 = values_109[0];
R.Call(R.Get(v_9, (V)"on_event"), R.Join(new V[] {v_97, v_110}, new Results(v_102)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_97, (V)"rng"), (V)"state") == v_99))));
var values_111 = Results.Empty;
V v_112 = values_111[0];
foreach (var row_113 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_97, (V)"drops"))))))) {
V v_114 = row_113[0];
V v_115 = row_113[1];
if (R.Truth(R.And((V)(R.Get(v_115, (V)"killed_entity_id") == (V)991.0d), () => (V)(R.Get(v_115, (V)"kind") == (V)"health")))) {
var values_116 = R.Join(new V[] {}, new Results(v_115));
v_112 = values_116[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_112, () => (V)(R.Get(v_112, (V)"amount") == (V)10.0d)))));
var values_117 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_97, (V)"drops"))));
V v_118 = values_117[0];
R.Call(R.Get(v_9, (V)"on_event"), R.Join(new V[] {v_97, v_110}, new Results(v_102)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_97, (V)"drops")) == v_118))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"canonical elite death leaves a collectible health pickup"}, new Results(new V((RuleFunction)(args_119 => {

var values_120 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)4.0d))));
V v_121 = values_120[0];
var values_122 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_121}, new Results(R.Get(R.Get(v_121, (V)"stage"), (V)"elite_id")))))));
V v_123 = values_122[0];
var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_121))));
V v_125 = values_124[0];
var values_126 = Results.Empty;
V v_127 = values_126[0];
foreach (var row_128 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_129 = row_128[0];
V v_130 = row_128[1];
var values_131 = R.Join(new V[] {(V)(R.Get(R.Get(v_123, (V)"position"), (V)"x") + R.Get(v_130, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_123, (V)"position"), (V)"y") + R.Get(v_130, (V)2.0d))));
V v_132 = values_131[0];
V v_133 = values_131[1];
var values_134 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"walkable"), R.Join(new V[] {R.Get(v_121, (V)"generated_map"), v_132}, new Results(v_133))));
V v_135 = values_134[0];
foreach (var row_136 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_121, (V)"entities"))))))) {
V v_137 = row_136[0];
V v_138 = row_136[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_138, (V)"id") != R.Get(v_125, (V)"id")), () => (V)(R.Get(R.Get(v_138, (V)"position"), (V)"floor_id") == R.Get(v_121, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_138, (V)"position"), (V)"x") == v_132)), () => (V)(R.Get(R.Get(v_138, (V)"position"), (V)"y") == v_133)))) {
var values_139 = R.Join(new V[] {}, new Results((V)false));
v_135 = values_139[0];
}
}
if (R.Truth(v_135)) {
var values_140 = R.Join(new V[] {v_132}, new Results(v_133));
var obj_141 = R.Get(v_125, (V)"position");
V key_142 = (V)"x";
var obj_143 = R.Get(v_125, (V)"position");
V key_144 = (V)"y";
R.Set(obj_141, key_142, values_140[0]);
R.Set(obj_143, key_144, values_140[1]);
var values_145 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-R.Get(v_130, (V)1.0d))), new Entry((V)2, (-R.Get(v_130, (V)2.0d))))));
v_127 = values_145[0];
break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_127}, new Results((V)"elite has no attack approach")));
var values_146 = R.Join(new V[] {}, new Results((V)(R.Get(v_123, (V)"hp") + R.Get(v_123, (V)"defense"))));
var obj_147 = v_125;
V key_148 = (V)"damage";
R.Set(obj_147, key_148, values_146[0]);
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))).First, (V)"recompute"), R.Join(new V[] {}, new Results(v_121)));
var values_149 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_121}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_127, (V)1.0d)), new Entry((V)"dy", R.Get(v_127, (V)2.0d)))))));
V v_150 = values_149[0];
V v_151 = values_149[1];
V v_152 = values_149[2];
var values_153 = R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {v_152}, new Results((V)"SupplyDropped"))));
V v_154 = values_153[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_154) == (V)1.0d), () => (V)(R.Get(R.Get(v_154, (V)1.0d), (V)"supply_kind") == (V)"health")))));
var values_155 = R.Join(new V[] {}, new Results((V)false));
V v_156 = values_155[0];
foreach (var row_157 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_121, (V)"drops"))))))) {
V v_158 = row_157[0];
V v_159 = row_157[1];
if (R.Truth(R.And((V)(R.Get(v_159, (V)"id") == R.Get(R.Get(v_154, (V)1.0d), (V)"drop_id")), () => (V)(R.Get(v_159, (V)"kind") == (V)"health")))) {
var values_160 = R.Join(new V[] {}, new Results((V)true));
v_156 = values_160[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_156)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"new area boons batch secondary hits and are offered only to new kits"}, new Results(new V((RuleFunction)(args_161 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_11, (V)"validate"), Results.Empty).First, () => (V)((V)R.Length(R.Get(v_11, (V)"order")) == (V)((V)53.0d + (V)R.Length(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"order"))))))));
foreach (var row_162 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:boon/aftershock"), new Entry((V)2, (V)"core:boon/reprisal_nova")))))))) {
V v_163 = row_162[0];
V v_164 = row_162[1];
var values_165 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"get"), R.Join(new V[] {}, new Results(v_164))));
V v_166 = values_165[0];
foreach (var row_167 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_11, (V)"rarities"))))))) {
V v_168 = row_167[0];
V v_169 = row_167[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_166, (V)"parameters"), v_169), (V)"power") > (V)0.0d))));
}
}
var values_170 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"core:actor/sentinel"))));
V v_171 = values_170[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_171, R.Get(v_171, (V)"player_id"), (V)"core:boon/aftershock", (V)"common"}, new Results((V)1.0d)));
var values_172 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_171, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)99999.0d))));
V v_173 = values_172[0];
var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_171, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)3.0d}, new Results((V)99999.0d))));
V v_175 = values_174[0];
var values_176 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_171}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_177 = values_176[0];
V v_178 = values_176[1];
V v_179 = values_176[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_16, R.Join(new V[] {v_179}, new Results((V)"BoonActivated"))).First) >= (V)1.0d), () => (V)(R.Get(v_173, (V)"hp") < R.Get(v_173, (V)"max_hp"))), () => (V)(R.Get(v_175, (V)"hp") < R.Get(v_175, (V)"max_hp"))))));
var values_180 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"core:actor/adventurer"))));
V v_181 = values_180[0];
var values_182 = R.Join(new V[] {(V)false}, new Results((V)false));
V v_183 = values_182[0];
V v_184 = values_182[1];
{ double end_187 = R.Num((V)300.0d), step_188 = R.Num((V)1.0d);
for (double i_186 = R.Num((V)1.0d); step_188 > 0 ? i_186 <= end_187 : i_186 >= end_187; i_186 += step_188) {
V v_185 = i_186;
var values_189 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))).First, (V)"new"), R.Join(new V[] {}, new Results(v_185))));
V v_190 = values_189[0];
foreach (var row_191 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"make_offers"), R.Join(new V[] {v_181, R.Call(R.Get(v_15, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/standard"))).First}, new Results(v_190)))))))) {
V v_192 = row_191[0];
V v_193 = row_191[1];
if (R.Truth((V)(R.Get(v_193, (V)"boon_id") == (V)"core:boon/aftershock"))) {
var values_194 = R.Join(new V[] {}, new Results((V)true));
v_183 = values_194[0];
}
}
var values_195 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"make_offers"), R.Join(new V[] {v_171, R.Call(R.Get(v_15, (V)"get_chest"), R.Join(new V[] {}, new Results((V)"core:chest/standard"))).First}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))).First, (V)"new"), R.Join(new V[] {}, new Results(v_185))))));
V v_196 = values_195[0];
foreach (var row_197 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_196)))))) {
V v_198 = row_197[0];
V v_199 = row_197[1];
if (R.Truth((V)(R.Get(v_199, (V)"boon_id") == (V)"core:boon/aftershock"))) {
var values_200 = R.Join(new V[] {}, new Results((V)true));
v_184 = values_200[0];
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_183)), () => v_184))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Reprisal Nova reacts to real incoming harm but never to a full block"}, new Results(new V((RuleFunction)(args_201 => {

var values_202 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"core:actor/sentinel"))));
V v_203 = values_202[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_203, R.Get(v_203, (V)"player_id"), (V)"core:boon/reprisal_nova", (V)"common"}, new Results((V)1.0d)));
var values_204 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_203, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)0.0d))));
V v_205 = values_204[0];
R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_203}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
var values_206 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_203}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_207 = values_206[0];
V v_208 = values_206[1];
V v_209 = values_206[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_16, R.Join(new V[] {v_209}, new Results((V)"BoonActivated"))).First) == (V)1.0d), () => (V)(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_203))).First, (V)"hp") < R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_203))).First, (V)"max_hp"))), () => (V)(R.Get(v_205, (V)"hp") < R.Get(v_205, (V)"max_hp"))))));
var values_210 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results((V)"core:actor/sentinel"))));
V v_211 = values_210[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_211, R.Get(v_211, (V)"player_id"), (V)"core:boon/reprisal_nova", (V)"common"}, new Results((V)1.0d)));
var values_212 = R.Join(new V[] {}, new Results((V)20.0d));
var obj_213 = R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_211))).First;
V key_214 = (V)"defense";
R.Set(obj_213, key_214, values_212[0]);
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_211, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)0.0d)));
R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_211}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
var values_215 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_211}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_216 = values_215[0];
V v_217 = values_215[1];
V v_218 = values_215[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_16, R.Join(new V[] {v_218}, new Results((V)"DamageBlocked"))).First) >= (V)1.0d), () => (V)((V)R.Length(R.Call(v_16, R.Join(new V[] {v_218}, new Results((V)"BoonActivated"))).First) == (V)0.0d)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
