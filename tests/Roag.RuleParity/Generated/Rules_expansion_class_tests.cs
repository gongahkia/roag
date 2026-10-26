// Static C# port of tests/expansion_class_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 7546c63d86022fa2572ed0a9fe9efcb2d4c5b88413274c60e3859ca636422515
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_expansion_class_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
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
return R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new"), R.Join(new V[] {(V)7.0d, (V)"core:map/test_room"}, new Results(R.Table(new Entry((V)"class_id", v_30))))));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"all registered classes have explicit stats, costs and snapshot identity"}, new Results(new V((RuleFunction)(args_31 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_5, (V)"validate"), Results.Empty).First, () => (V)((V)R.Length(R.Get(v_5, (V)"order")) == (V)15.0d)))));
var values_32 = R.Join(new V[] {}, new Results(R.Table()));
V v_33 = values_32[0];
foreach (var row_34 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_5, (V)"order"))))))) {
V v_35 = row_34[0];
V v_36 = row_34[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_33, v_36))))));

var values_37 = R.Join(new V[] {}, new Results((V)true));
var obj_38 = v_33;
V key_39 = v_36;
R.Set(obj_38, key_39, values_37[0]);
var values_40 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results(v_36))));
V v_41 = values_40[0];
var values_42 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"get"), R.Join(new V[] {}, new Results(v_36))));
V v_43 = values_42[0];
var values_44 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_41))));
V v_45 = values_44[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_45, (V)"definition_id") == v_36), () => (V)(R.Get(v_45, (V)"hp") == R.Get(v_43, (V)"hp"))), () => (V)(R.Get(v_45, (V)"damage") == R.Get(v_43, (V)"damage"))), () => (V)(R.Get(v_45, (V)"speed") == R.Get(v_43, (V)"speed"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_9, (V)"preview"), R.Join(new V[] {v_41, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"cost") == R.Call(R.Get(v_9, (V)"effective_cost"), R.Join(new V[] {R.Get(R.Get(v_43, (V)"costs"), (V)"basic_attack")}, new Results(R.Get(v_45, (V)"speed")))).First))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_41))))))).First}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_41)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(R.Get(v_5, (V)"get"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), (V)1.0d)))).First, (V)"hp") == (V)24.0d), () => (V)(R.Get(R.Get(R.Call(R.Get(v_5, (V)"get"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), (V)1.0d)))).First, (V)"costs"), (V)"dash") == (V)130.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Ranger shot obeys range, diagonal corners and first-target stop"}, new Results(new V((RuleFunction)(args_46 => {

var values_47 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), (V)2.0d)))));
V v_48 = values_47[0];
var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"preview"), R.Join(new V[] {v_48, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_50 = values_49[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_50, (V)"cells")) == (V)4.0d), () => (V)(R.Get(R.Get(R.Get(v_50, (V)"cells"), (V)4.0d), (V)"x") == (V)6.0d)))));
var values_51 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_48, (V)"core:enemy/ruin_skitter", (V)4.0d, (V)2.0d}, new Results((V)0.0d))));
V v_52 = values_51[0];
var values_53 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_48, (V)"core:enemy/mossbound_guard", (V)6.0d, (V)2.0d}, new Results((V)0.0d))));
V v_54 = values_53[0];
var values_55 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_48}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_56 = values_55[0];
V v_57 = values_55[1];
V v_58 = values_55[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_56, () => (V)((V)R.Length(R.Call(v_16, R.Join(new V[] {v_58}, new Results((V)"DamageTaken"))).First) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Call(v_16, R.Join(new V[] {v_58}, new Results((V)"DamageTaken"))).First, (V)1.0d), (V)"target_id") == R.Get(v_52, (V)"id"))), () => (V)(R.Get(v_54, (V)"hp") == R.Get(v_54, (V)"max_hp"))))));
var values_59 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), (V)2.0d)))));
V v_60 = values_59[0];
R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_60, (V)"core:enemy/ruin_skitter", (V)4.0d, (V)4.0d}, new Results((V)0.0d)));
var values_61 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"preview"), R.Join(new V[] {v_60, (V)"basic_attack", (V)1.0d}, new Results((V)1.0d))));
V v_62 = values_61[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_62, (V)"cells"), (V)2.0d), (V)"x") == (V)4.0d), () => (V)(R.Get(R.Get(R.Get(v_62, (V)"cells"), (V)2.0d), (V)"y") == (V)3.0d)))));
var values_63 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_60}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))));
V v_64 = values_63[0];
V v_65 = values_63[1];
V v_66 = values_63[2];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Call(v_16, R.Join(new V[] {v_66}, new Results((V)"DamageTaken"))).First) == (V)0.0d)}, new Results((V)"bow shot crossed a blocked diagonal corner")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Ranger volley pierces two hostiles in one primary batch and evades three cells"}, new Results(new V((RuleFunction)(args_67 => {

var values_68 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), (V)2.0d)))));
V v_69 = values_68[0];
var values_70 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_69, (V)"core:enemy/ruin_skitter", (V)4.0d, (V)2.0d}, new Results((V)0.0d))));
V v_71 = values_70[0];
var values_72 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_69, (V)"core:enemy/mossbound_guard", (V)6.0d, (V)2.0d}, new Results((V)0.0d))));
V v_73 = values_72[0];
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_69}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_75 = values_74[0];
V v_76 = values_74[1];
V v_77 = values_74[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_75)));
var values_78 = R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {v_77}, new Results((V)"DamageBatchResolved"))));
V v_79 = values_78[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(v_79) >= (V)1.0d), () => (V)((V)R.Length(R.Get(R.Get(v_79, (V)1.0d), (V)"outcomes")) == (V)2.0d)), () => (V)(R.Get(R.Get(R.Get(R.Get(v_79, (V)1.0d), (V)"outcomes"), (V)1.0d), (V)"target_id") == R.Get(v_71, (V)"id"))), () => (V)(R.Get(R.Get(R.Get(R.Get(v_79, (V)1.0d), (V)"outcomes"), (V)2.0d), (V)"target_id") == R.Get(v_73, (V)"id"))))));
var values_80 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), (V)2.0d)))));
V v_81 = values_80[0];
var values_82 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"preview"), R.Join(new V[] {v_81, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_83 = values_82[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_83, (V)"valid"), () => (V)(R.Get(v_83, (V)"distance") == (V)3.0d)), () => (V)(R.Get(v_83, (V)"cost") == R.Call(R.Get(v_9, (V)"effective_cost"), R.Join(new V[] {(V)115.0d}, new Results(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_81))).First, (V)"speed")))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_81}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_81))).First, (V)"position"), (V)"x") == (V)5.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Arcanist lightning triggers crystal and fire sigil batches visible targets"}, new Results(new V((RuleFunction)(args_84 => {

var values_85 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), (V)3.0d)))));
V v_86 = values_85[0];
V v_87 = V.Nil;
v_87 = new V((RuleFunction)(args_88 => {
V v_89 = args_88[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_89))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_89));
}
var values_90 = R.Join(new V[] {}, new Results(R.Table()));
V v_91 = values_90[0];
foreach (var row_92 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_89)))))) {
V v_93 = row_92[0];
V v_94 = row_92[1];
var values_95 = R.Join(new V[] {}, R.Call(v_87, R.Join(new V[] {}, new Results(v_94))));
var obj_96 = v_91;
V key_97 = v_93;
R.Set(obj_96, key_97, values_95[0]);
}
return R.Join(new V[] {}, new Results(v_91));
return Results.Empty;
}));
var values_98 = R.Join(new V[] {}, R.Call(v_87, R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/test_room"))))));
var obj_99 = v_86;
V key_100 = (V)"generated_map";
R.Set(obj_99, key_100, values_98[0]);
var values_101 = R.Join(new V[] {}, new Results(R.Get(v_86, (V)"generated_map")));
V v_102 = values_101[0];
var values_103 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_102, (V)"rows"), (V)3.0d), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)3.0d))).First, R.Concat((V)"c", R.Invoke(R.Get(R.Get(v_102, (V)"rows"), (V)3.0d), "sub", R.Join(new V[] {}, new Results((V)5.0d))).First))));
var obj_104 = R.Get(v_102, (V)"rows");
V key_105 = (V)3.0d;
R.Set(obj_104, key_105, values_103[0]);
var values_106 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_86, (V)"core:enemy/mossbound_guard", (V)4.0d, (V)2.0d}, new Results((V)0.0d))));
V v_107 = values_106[0];
var values_108 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_86, (V)"core:enemy/ruin_skitter", (V)5.0d, (V)2.0d}, new Results((V)0.0d))));
V v_109 = values_108[0];
var values_110 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_86}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_111 = values_110[0];
V v_112 = values_110[1];
V v_113 = values_110[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_111, () => (V)((V)R.Length(R.Call(v_16, R.Join(new V[] {v_113}, new Results((V)"TerrainTriggered"))).First) >= (V)1.0d)))));
var values_114 = R.Join(new V[] {(V)false}, new Results((V)false));
V v_115 = values_114[0];
V v_116 = values_114[1];
foreach (var row_117 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {v_113}, new Results((V)"DamageTaken")))))))) {
V v_118 = row_117[0];
V v_119 = row_117[1];
if (R.Truth(R.And((V)(R.Get(v_119, (V)"target_id") == R.Get(v_107, (V)"id")), () => (V)(R.Get(v_119, (V)"damage_type") == (V)"lightning")))) {
var values_120 = R.Join(new V[] {}, new Results((V)true));
v_115 = values_120[0];
} else if (R.Truth(R.And((V)(R.Get(v_119, (V)"target_id") == R.Get(v_109, (V)"id")), () => (V)(R.Get(v_119, (V)"damage_type") == (V)"crystal_arc")))) {
var values_121 = R.Join(new V[] {}, new Results((V)true));
v_116 = values_121[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_115, () => v_116))));
var values_122 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), (V)3.0d)))));
V v_123 = values_122[0];
R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_123, (V)"core:enemy/mossbound_guard", (V)4.0d, (V)2.0d}, new Results((V)0.0d)));
R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_123, (V)"core:enemy/mossbound_guard", (V)5.0d, (V)2.0d}, new Results((V)0.0d)));
var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_123}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_125 = values_124[0];
V v_126 = values_124[1];
V v_127 = values_124[2];
var values_128 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_16, R.Join(new V[] {v_127}, new Results((V)"DamageBatchResolved"))).First, (V)1.0d)));
V v_129 = values_128[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_129, () => (V)((V)R.Length(R.Get(v_129, (V)"outcomes")) == (V)2.0d)), () => (V)(R.Get(R.Get(R.Get(v_129, (V)"outcomes"), (V)1.0d), (V)"damage_type") == (V)"fire")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Berserker keeps deliberate slow costs, broad cleave and cardinal bump"}, new Results(new V((RuleFunction)(args_130 => {

var values_131 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results(R.Get(R.Get(v_5, (V)"order"), (V)4.0d)))));
V v_132 = values_131[0];
var values_133 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_132))));
V v_134 = values_133[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_134, (V)"hp") == (V)32.0d), () => (V)(R.Get(v_134, (V)"defense") == (V)1.0d)), () => (V)(R.Get(v_134, (V)"speed") == (V)75.0d)))));
var values_135 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"preview"), R.Join(new V[] {v_132, (V)"move", (V)1.0d}, new Results((V)0.0d))));
V v_136 = values_135[0];
var values_137 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"preview"), R.Join(new V[] {v_132, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_138 = values_137[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_136, (V)"cost") == R.Call(R.Get(v_9, (V)"effective_cost"), R.Join(new V[] {(V)115.0d}, new Results((V)75.0d))).First), () => (V)(R.Get(v_138, (V)"cost") == R.Call(R.Get(v_9, (V)"effective_cost"), R.Join(new V[] {(V)180.0d}, new Results((V)75.0d))).First)), () => (V)((V)R.Length(R.Get(v_138, (V)"cells")) == (V)5.0d)))));
var values_139 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_132))));
V v_140 = values_139[0];
var values_141 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_132}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)1.0d))))));
V v_142 = values_141[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_142)))));

R.Call(v_3, R.Join(new V[] {v_140}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_132)))));
R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_132, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)0.0d)));
var values_143 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"preview"), R.Join(new V[] {v_132, (V)"move", (V)1.0d}, new Results((V)0.0d))));
V v_144 = values_143[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_144, (V)"bump"), () => (V)(R.Get(v_144, (V)"cost") == R.Call(R.Get(v_9, (V)"effective_cost"), R.Join(new V[] {(V)130.0d}, new Results((V)75.0d))).First)))));
var values_145 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_132}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_146 = values_145[0];
V v_147 = values_145[1];
V v_148 = values_145[2];
var values_149 = R.Join(new V[] {}, new Results((V)false));
V v_150 = values_149[0];
foreach (var row_151 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {v_148}, new Results((V)"DamageTaken")))))))) {
V v_152 = row_151[0];
V v_153 = row_151[1];
if (R.Truth(R.And((V)(R.Get(v_153, (V)"target_id") == (V)2.0d), () => (V)(R.Get(v_153, (V)"source_id") == R.Get(v_132, (V)"player_id"))))) {
var values_154 = R.Join(new V[] {}, new Results((V)true));
v_150 = values_154[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_146, () => v_150))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"every class survives an ordinary-action first encounter on paired floors"}, new Results(new V((RuleFunction)(args_155 => {

foreach (var row_156 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_5, (V)"order"))))))) {
V v_157 = row_156[0];
V v_158 = row_156[1];
var values_159 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"new"), R.Join(new V[] {(V)3.0d}, new Results(R.Table(new Entry((V)"class_id", v_158), new Entry((V)"multifloor", (V)true))))));
V v_160 = values_159[0];
var values_161 = R.Join(new V[] {}, new Results(R.Get(v_160, (V)"state")));
V v_162 = values_161[0];
var values_165 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"nearest"), R.Join(new V[] {v_162}, new Results(new V((RuleFunction)(args_163 => {
V v_164 = args_163[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_164, (V)"controller") == (V)"ai"), () => (V)(R.Get(R.Get(v_164, (V)"position"), (V)"floor_id") == R.Get(v_162, (V)"active_floor_id"))), () => (V)(R.Get(v_164, (V)"id") != R.Get(R.Get(v_162, (V)"stage"), (V)"elite_id")))));
return Results.Empty;
}))))))));
V v_166 = values_165[0];
var values_167 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"fight"), R.Join(new V[] {v_160, R.Get(v_166, (V)"id")}, new Results((V)220.0d))));
V v_168 = values_167[0];
V v_169 = values_167[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_168}, new Results(R.Concat(R.Get(R.Call(R.Get(v_5, (V)"get"), R.Join(new V[] {}, new Results(v_158))).First, (V)"name"), R.Concat((V)": ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_169))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_162))).First, (V)"hp") > (V)0.0d), () => (V)((V)R.Length(R.Get(v_160, (V)"trace")) > (V)0.0d)), () => (V)(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_162))).First, (V)"definition_id") == v_158)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stage route handoff carries class build through a zero-time choice"}, new Results(new V((RuleFunction)(args_170 => {

var values_171 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new_stage"), R.Join(new V[] {(V)4.0d}, new Results(R.Table(new Entry((V)"class_id", R.Get(R.Get(v_5, (V)"order"), (V)2.0d)))))));
V v_172 = values_171[0];
var values_173 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_172))));
V v_174 = values_173[0];
var values_175 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"entity"), R.Join(new V[] {v_172}, new Results(R.Get(R.Get(v_172, (V)"stage"), (V)"elite_id")))));
V v_176 = values_175[0];
foreach (var row_177 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_172, (V)"entities"))))))) {
V v_178 = row_177[0];
V v_179 = row_177[1];
if (R.Truth((V)(R.Get(v_179, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_13, (V)"remove_for_actor"), R.Join(new V[] {v_172}, new Results(R.Get(v_179, (V)"id"))));
}
}
var values_180 = Results.Empty;
V v_181 = values_180[0];
foreach (var row_182 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_183 = row_182[0];
V v_184 = row_182[1];
var values_185 = R.Join(new V[] {(V)(R.Get(R.Get(v_176, (V)"position"), (V)"x") + R.Get(v_184, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_176, (V)"position"), (V)"y") + R.Get(v_184, (V)2.0d))));
V v_186 = values_185[0];
V v_187 = values_185[1];
if (R.Truth(R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {R.Get(v_172, (V)"generated_map"), v_186}, new Results(v_187))).First)) {
var values_188 = R.Join(new V[] {v_186}, new Results(v_187));
var obj_189 = R.Get(v_174, (V)"position");
V key_190 = (V)"x";
var obj_191 = R.Get(v_174, (V)"position");
V key_192 = (V)"y";
R.Set(obj_189, key_190, values_188[0]);
R.Set(obj_191, key_192, values_188[1]);
var values_193 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-R.Get(v_184, (V)1.0d))), new Entry((V)2, (-R.Get(v_184, (V)2.0d))))));
v_181 = values_193[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_181)));
var values_194 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_176, (V)"hp") + R.Get(v_176, (V)"defense")) + (V)2.0d)));
var obj_195 = v_174;
V key_196 = (V)"damage";
R.Set(obj_195, key_196, values_194[0]);
R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_172)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_172}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_181, (V)1.0d)), new Entry((V)"dy", R.Get(v_181, (V)2.0d))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_172, (V)"stage"), (V)"phase") == (V)"extraction"))));
var values_197 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"entity"), R.Join(new V[] {v_172}, new Results(R.Get(R.Get(v_172, (V)"stage"), (V)"exit_id")))));
V v_198 = values_197[0];
foreach (var row_199 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_200 = row_199[0];
V v_201 = row_199[1];
var values_202 = R.Join(new V[] {(V)(R.Get(R.Get(v_198, (V)"position"), (V)"x") + R.Get(v_201, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_198, (V)"position"), (V)"y") + R.Get(v_201, (V)2.0d))));
V v_203 = values_202[0];
V v_204 = values_202[1];
if (R.Truth(R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {R.Get(v_172, (V)"generated_map"), v_203}, new Results(v_204))).First)) {
var values_205 = R.Join(new V[] {v_203}, new Results(v_204));
var obj_206 = R.Get(v_174, (V)"position");
V key_207 = (V)"x";
var obj_208 = R.Get(v_174, (V)"position");
V key_209 = (V)"y";
R.Set(obj_206, key_207, values_205[0]);
R.Set(obj_208, key_209, values_205[1]);

break;
}
}
R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_172)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"route_options"), R.Join(new V[] {v_172}, new Results(R.Get(v_198, (V)"id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_172}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_198, (V)"id")), new Entry((V)"route_index", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_172))).First, (V)"definition_id") == R.Get(R.Get(v_5, (V)"order"), (V)2.0d)), () => (V)(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_172))).First, (V)"damage") == R.Get(v_174, (V)"damage"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_172)))))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
