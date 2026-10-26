// Static C# port of tests/expansion_full_run_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 74cb8977fb9610f6b3978ac1deaff6f950a325e35076baf2c219cab3c237c929
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_expansion_full_run_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.expansion_legal_playthrough"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievements"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_13 = values_12[0];
R.Call(v_2, R.Join(new V[] {(V)"seed 9 completes six procedural floors and final boss with legal actions"}, new Results(new V((RuleFunction)(args_14 => {

var values_15 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attempt"), R.Join(new V[] {}, new Results((V)9.0d))));
V v_16 = values_15[0];
V v_17 = values_15[1];
V v_18 = values_15[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_16}, new Results(R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_17))).First, R.Concat((V)" after ", R.Concat(R.Or(R.And(v_18, () => (V)R.Length(R.Get(v_18, (V)"trace"))), () => (V)0.0d), (V)" actions"))))));
var values_19 = R.Join(new V[] {}, new Results(R.Get(v_16, (V)"state")));
V v_20 = values_19[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_20, (V)"victory"), () => R.Get(v_20, (V)"game_over")), () => (V)(!R.Truth(R.Call(R.Get(v_9, (V)"entity"), R.Join(new V[] {v_20}, new Results(R.Get(R.Get(v_20, (V)"victory"), (V)"boss_id")))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_16, (V)"stages")) == (V)3.0d), () => (V)((V)R.Length(R.Get(v_16, (V)"purchases")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_16, (V)"trace")) > (V)500.0d)))));
var values_21 = R.Join(new V[] {R.Table(), R.Table()}, new Results((V)0.0d));
V v_22 = values_21[0];
V v_23 = values_21[1];
V v_24 = values_21[2];
foreach (var row_25 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_16, (V)"trace"))))))) {
V v_26 = row_25[0];
V v_27 = row_25[1];
foreach (var row_28 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_27, (V)"event_kinds"))))))) {
V v_29 = row_28[0];
V v_30 = row_28[1];
if (R.Truth((V)(v_30 == (V)"FloorChanged"))) {
var values_31 = R.Join(new V[] {}, new Results(R.Get(v_27, (V)"clock")));
var obj_32 = v_22;
V key_33 = (V)((V)R.Length(v_22) + (V)1.0d);
R.Set(obj_32, key_33, values_31[0]);
}
if (R.Truth((V)(v_30 == (V)"StageEntered"))) {
var values_34 = R.Join(new V[] {}, new Results(R.Get(v_27, (V)"clock")));
var obj_35 = v_23;
V key_36 = (V)((V)R.Length(v_23) + (V)1.0d);
R.Set(obj_35, key_36, values_34[0]);
}
if (R.Truth((V)(v_30 == (V)"Victory"))) {
var values_37 = R.Join(new V[] {}, new Results((V)(v_24 + (V)1.0d)));
v_24 = values_37[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_22) == (V)3.0d), () => (V)((V)R.Length(v_23) == (V)2.0d)), () => (V)(v_24 == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_20, (V)"victory"), (V)"hp") > (V)0.0d), () => (V)(R.Get(R.Get(v_20, (V)"victory"), (V)"world_time") > (V)0.0d)))));
var values_38 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", R.Call(R.Get(v_9, (V)"new_stage"), R.Join(new V[] {(V)9.0d}, new Results(R.Table(new Entry((V)"multifloor", (V)true), new Entry((V)"class_id", R.Get(R.Get(v_13, (V)"order"), (V)1.0d)), new Entry((V)"available_boons", R.Call(R.Get(v_11, (V)"pool"), R.Join(new V[] {}, new Results(R.Table()))).First))))).First), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table()))));
V v_39 = values_38[0];
foreach (var row_40 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_16, (V)"trace"))))))) {
V v_41 = row_40[0];
V v_42 = row_40[1];
var values_43 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"action"), R.Join(new V[] {v_39}, new Results(R.Get(v_42, (V)"intent")))));
V v_44 = values_43[0];
V v_45 = values_43[1];
V v_46 = values_43[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(v_44 == R.Get(v_42, (V)"ok")), () => (V)(v_45 == R.Get(v_42, (V)"reason"))), () => (V)(R.Get(R.Get(v_39, (V)"state"), (V)"clock") == R.Get(v_42, (V)"clock"))), () => (V)((V)R.Length(v_46) == (V)R.Length(R.Get(v_42, (V)"event_kinds"))))}, new Results(R.Concat((V)"replay action ", v_41))));
foreach (var row_47 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_46)))))) {
V v_48 = row_47[0];
V v_49 = row_47[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_49, (V)"kind") == R.Get(R.Get(v_42, (V)"event_kinds"), v_48))}, new Results(R.Concat((V)"replay ordered event ", R.Concat(v_41, R.Concat((V)":", v_48))))));
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_39, (V)"state")))).First}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_20)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all four classes can win through six floors with ordinary submitted actions"}, new Results(new V((RuleFunction)(args_50 => {

foreach (var row_51 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, R.Get(R.Get(v_13, (V)"order"), (V)1.0d)), new Entry((V)2, (V)9.0d))), new Entry((V)2, R.Table(new Entry((V)1, R.Get(R.Get(v_13, (V)"order"), (V)2.0d)), new Entry((V)2, (V)9.0d))), new Entry((V)3, R.Table(new Entry((V)1, R.Get(R.Get(v_13, (V)"order"), (V)3.0d)), new Entry((V)2, (V)45.0d))), new Entry((V)4, R.Table(new Entry((V)1, R.Get(R.Get(v_13, (V)"order"), (V)4.0d)), new Entry((V)2, (V)21.0d)))))))))) {
V v_52 = row_51[0];
V v_53 = row_51[1];
var values_54 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attempt"), R.Join(new V[] {R.Get(v_53, (V)2.0d)}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_53, (V)1.0d)))))));
V v_55 = values_54[0];
V v_56 = values_54[1];
V v_57 = values_54[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_55}, R.Invoke((V)"%s seed %d failed after %d actions: %s", "format", R.Join(new V[] {R.Get(v_53, (V)1.0d), R.Get(v_53, (V)2.0d), R.Or(R.And(v_57, () => (V)R.Length(R.Get(v_57, (V)"trace"))), () => (V)0.0d)}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_56)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_55, (V)"state"), (V)"victory"), () => (V)(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(R.Get(v_55, (V)"state")))).First, (V)"definition_id") == R.Get(v_53, (V)1.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_55, (V)"stages")) == (V)3.0d), () => (V)((V)R.Length(R.Get(v_55, (V)"purchases")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_55, (V)"trace")) > (V)500.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_55, (V)"state"), (V)"victory"), (V)"hp") > (V)0.0d))));
if (R.Truth((V)(R.Get(v_53, (V)1.0d) == R.Get(R.Get(v_13, (V)"order"), (V)2.0d)))) {
var values_58 = R.Join(new V[] {}, new Results((V)false));
V v_59 = values_58[0];
foreach (var row_60 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_55, (V)"events"))))))) {
V v_61 = row_60[0];
V v_62 = row_60[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_62, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_62, (V)"ability_id") == (V)"core:ability/ranger_shot")), () => R.Get(v_62, (V)"direct_attack")), () => (V)(R.Get(v_62, (V)"actual_loss") > (V)0.0d)))) {
var values_63 = R.Join(new V[] {}, new Results(R.Get(v_62, (V)"source_position")));
V v_64 = values_63[0];
var values_65 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_64, (V)"x") - R.Get(v_62, (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_64, (V)"y") - R.Get(v_62, (V)"y"))))))));
V v_66 = values_65[0];
if (R.Truth(R.And((V)(v_66 >= (V)2.0d), () => (V)(v_66 <= (V)4.0d)))) {
var values_67 = R.Join(new V[] {}, new Results((V)true));
v_59 = values_67[0];

break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_59}, new Results((V)"guided Ranger never used its real four-cell firing range")));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Sentinel and Alchemist each reach Victory through ordinary submitted actions"}, new Results(new V((RuleFunction)(args_68 => {

foreach (var row_69 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, R.Get(R.Get(v_13, (V)"order"), (V)5.0d)), new Entry((V)2, (V)28.0d))), new Entry((V)2, R.Table(new Entry((V)1, R.Get(R.Get(v_13, (V)"order"), (V)6.0d)), new Entry((V)2, (V)9.0d)))))))))) {
V v_70 = row_69[0];
V v_71 = row_69[1];
var values_72 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attempt"), R.Join(new V[] {R.Get(v_71, (V)2.0d)}, new Results(R.Table(new Entry((V)"class_id", R.Get(v_71, (V)1.0d)))))));
V v_73 = values_72[0];
V v_74 = values_72[1];
V v_75 = values_72[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_73}, R.Invoke((V)"%s seed %d failed after %d actions: %s", "format", R.Join(new V[] {R.Get(v_71, (V)1.0d), R.Get(v_71, (V)2.0d), R.Or(R.And(v_75, () => (V)R.Length(R.Get(v_75, (V)"trace"))), () => (V)0.0d)}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_74)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.Get(R.Get(v_73, (V)"state"), (V)"victory"), () => (V)(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(R.Get(v_73, (V)"state")))).First, (V)"definition_id") == R.Get(v_71, (V)1.0d))), () => (V)((V)R.Length(R.Get(v_73, (V)"stages")) == (V)3.0d)), () => (V)((V)R.Length(R.Get(v_73, (V)"purchases")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_73, (V)"trace")) > (V)500.0d)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ordinary legal routes defeat all seven unique biome minibosses"}, new Results(new V((RuleFunction)(args_76 => {

var values_77 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"core:biome/overgrown_ruins"), new Entry((V)2, (V)9.0d), new Entry((V)3, (V)1.0d), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)1.0d))))), new Entry((V)2, R.Table(new Entry((V)1, (V)"core:biome/fungal_hollows"), new Entry((V)2, (V)9.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d))))), new Entry((V)3, R.Table(new Entry((V)1, (V)"core:biome/sunken_aqueduct"), new Entry((V)2, (V)21.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, R.Table(new Entry((V)1, (V)2.0d))))), new Entry((V)4, R.Table(new Entry((V)1, (V)"core:biome/glacial_observatory"), new Entry((V)2, (V)21.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, R.Table(new Entry((V)1, (V)3.0d))))), new Entry((V)5, R.Table(new Entry((V)1, (V)"core:biome/ashen_bastion"), new Entry((V)2, (V)9.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d))))), new Entry((V)6, R.Table(new Entry((V)1, (V)"core:biome/fractured_archive"), new Entry((V)2, (V)9.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)3.0d))))), new Entry((V)7, R.Table(new Entry((V)1, (V)"core:biome/sundered_forge"), new Entry((V)2, (V)25.0d), new Entry((V)3, (V)3.0d), new Entry((V)4, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)3.0d))))))));
V v_78 = values_77[0];
foreach (var row_79 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_78)))))) {
V v_80 = row_79[0];
V v_81 = row_79[1];
var values_82 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attempt"), R.Join(new V[] {R.Get(v_81, (V)2.0d)}, new Results(R.Table(new Entry((V)"routes", R.Get(v_81, (V)4.0d)), new Entry((V)"stop_after_stage", R.Get(v_81, (V)3.0d)))))));
V v_83 = values_82[0];
V v_84 = values_82[1];
V v_85 = values_82[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_83}, R.Invoke((V)"%s seed %d failed after %d actions: %s", "format", R.Join(new V[] {R.Get(v_81, (V)1.0d), R.Get(v_81, (V)2.0d), R.Or(R.And(v_85, () => (V)R.Length(R.Get(v_85, (V)"trace"))), () => (V)0.0d)}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_84)))))));
var values_86 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_83, (V)"stages"), (V)R.Length(R.Get(v_83, (V)"stages")))));
V v_87 = values_86[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_87, () => (V)(R.Get(v_87, (V)"biome_id") == R.Get(v_81, (V)1.0d))), () => (V)(R.Get(v_87, (V)"hp") > (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(R.Get(R.Get(v_83, (V)"state"), (V)"stage"), (V)"elite_reward_id"), () => (V)(!R.Truth(R.Call(R.Get(v_9, (V)"entity"), R.Join(new V[] {R.Get(v_83, (V)"state")}, new Results(R.Get(R.Get(R.Get(v_83, (V)"state"), (V)"stage"), (V)"elite_id")))).First)))}, new Results(R.Concat((V)"miniboss still alive after legal elite route: ", R.Get(v_81, (V)1.0d)))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
