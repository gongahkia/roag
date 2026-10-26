// Static C# port of tests/acceptance_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 02aad879efaac6fcb9979d0fb2ce60638b59cd636af8fd6125a7b18c14955ecf
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_acceptance_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.diagnostics"))));
V v_8 = values_7[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
R.Call(v_10, R.Join(new V[] {(V)"legal generated run buys a boon, defeats Warden and fights in stage two"}, new Results(new V((RuleFunction)(args_12 => {

var values_13 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"stage_one"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, new Results(R.Table()));
V v_16 = values_15[0];
foreach (var row_17 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_14, (V)"checkpoints"))))))) {
V v_18 = row_17[0];
V v_19 = row_17[1];
var values_20 = R.Join(new V[] {}, new Results(v_19));
var obj_21 = v_16;
V key_22 = R.Get(v_19, (V)"name");
R.Set(obj_21, key_22, values_20[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_16, (V)"earned_currency"), (V)"currency") >= (V)5.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_16, (V)"paid_chest_claimed"), (V)"stage") == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_16, (V)"elite_defeated"), (V)"hp") > (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_16, (V)"free_chest_claimed"), (V)"stage") == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_16, (V)"stage_two_entered"), (V)"stage") == (V)2.0d), () => (V)(R.Get(R.Get(v_16, (V)"stage_two_entered"), (V)"clock") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_16, (V)"stage_two_enemy_defeated"), (V)"stage") == (V)2.0d), () => (V)(R.Get(R.Get(v_16, (V)"stage_two_enemy_defeated"), (V)"clock") > (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_14}, new Results((V)"EntityKilled")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_14}, new Results((V)"ExperienceGained")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_14}, new Results((V)"BoonGranted")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_14}, new Results((V)"BoonActivated")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_14}, new Results((V)"StatusApplied")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_14}, new Results((V)"ElitePhaseChanged")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_14}, new Results((V)"StageEntered")))));
var values_23 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_14, (V)"state"), (V)"retired_stages"), (V)1.0d), (V)"generated_map")))));
V v_24 = values_23[0];
var values_25 = R.Join(new V[] {R.Table()}, new Results((V)false));
V v_26 = values_25[0];
V v_27 = values_25[1];
foreach (var row_28 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_14, (V)"trace"))))))) {
V v_29 = row_28[0];
V v_30 = row_28[1];
if (R.Truth((V)(R.Get(R.Get(v_30, (V)"from"), (V)"stage") == (V)1.0d))) {
var values_31 = R.Join(new V[] {}, new Results((V)false));
V v_32 = values_31[0];
foreach (var row_33 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"rooms"))))))) {
V v_34 = row_33[0];
V v_35 = row_33[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(R.Get(v_30, (V)"from"), (V)"x") >= R.Get(v_35, (V)"x")), () => (V)(R.Get(R.Get(v_30, (V)"from"), (V)"x") < (V)(R.Get(v_35, (V)"x") + R.Get(v_35, (V)"width")))), () => (V)(R.Get(R.Get(v_30, (V)"from"), (V)"y") >= R.Get(v_35, (V)"y"))), () => (V)(R.Get(R.Get(v_30, (V)"from"), (V)"y") < (V)(R.Get(v_35, (V)"y") + R.Get(v_35, (V)"height")))))) {
var values_36 = R.Join(new V[] {}, new Results((V)true));
var obj_37 = v_26;
V key_38 = R.Get(v_35, (V)"id");
R.Set(obj_37, key_38, values_36[0]);

var values_39 = R.Join(new V[] {}, new Results((V)true));
v_32 = values_39[0];
}
}
if (R.Truth((V)(!R.Truth(v_32)))) {
var values_40 = R.Join(new V[] {}, new Results((V)true));
v_27 = values_40[0];
}
}
}
var values_41 = R.Join(new V[] {}, new Results((V)0.0d));
V v_42 = values_41[0];
foreach (var row_43 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_26)))))) {
V v_44 = row_43[0];
var values_45 = R.Join(new V[] {}, new Results((V)(v_42 + (V)1.0d)));
v_42 = values_45[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(v_42 >= (V)2.0d), () => v_27)}, new Results((V)"legal guide did not cross distinct rooms and corridor space")));
var values_46 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"replay"), R.Join(new V[] {}, new Results(v_14))));
V v_47 = values_46[0];
R.Call(v_11, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_14, (V)"state")))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_47, (V)"state"))))));
R.Call(v_11, R.Join(new V[] {R.Get(v_14, (V)"events")}, new Results(R.Get(v_47, (V)"events"))));
return Results.Empty;
})))));
R.Call(v_10, R.Join(new V[] {(V)"legal secret and door interactions on generated floors"}, new Results(new V((RuleFunction)(args_48 => {

var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)9.0d))));
V v_50 = values_49[0];
var values_51 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_50, (V)"state"), (V)"secrets"), (V)2.0d)));
V v_52 = values_51[0];
var values_55 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"navigate"), R.Join(new V[] {v_50, new V((RuleFunction)(args_53 => {
V v_54 = args_53[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_54, (V)"x") == R.Get(v_52, (V)"x")), () => (V)(R.Get(v_54, (V)"y") == R.Get(v_52, (V)"y")))));
return Results.Empty;
}))}, new Results((V)220.0d))));
V v_56 = values_55[0];
V v_57 = values_55[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_56}, new Results(v_57)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_52, (V)"revealed"), () => R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_50}, new Results((V)"SecretDiscovered"))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_50, (V)"state"), (V)"currency") >= (V)4.0d))));
var values_58 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_59 = values_58[0];
var values_60 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_59, (V)"state"), (V)"secrets"), (V)1.0d)));
V v_61 = values_60[0];
var values_64 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"navigate"), R.Join(new V[] {v_59, new V((RuleFunction)(args_62 => {
V v_63 = args_62[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_63, (V)"x") == R.Get(v_61, (V)"x")), () => (V)(R.Get(v_63, (V)"y") == R.Get(v_61, (V)"y")))));
return Results.Empty;
}))}, new Results((V)180.0d))));
v_56 = values_64[0];
v_57 = values_64[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_56}, new Results(v_57)));
var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"action"), R.Join(new V[] {v_59}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(R.Get(v_61, (V)"wall_x") - R.Get(v_61, (V)"x"))), new Entry((V)"dy", (V)(R.Get(v_61, (V)"wall_y") - R.Get(v_61, (V)"y"))))))));
v_56 = values_65[0];
v_57 = values_65[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_56}, new Results(v_57)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_61, (V)"revealed"), () => R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {R.Get(v_59, (V)"state")}, new Results(R.Get(v_61, (V)"reward_id")))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_59}, new Results((V)"SecretDiscovered")))));
var values_66 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)13.0d))));
V v_67 = values_66[0];
var values_70 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"nearest"), R.Join(new V[] {R.Get(v_67, (V)"state")}, new Results(new V((RuleFunction)(args_68 => {
V v_69 = args_68[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_69, (V)"controller") == (V)"door"), () => (V)(!R.Truth(R.Get(v_69, (V)"open"))))));
return Results.Empty;
}))))))));
V v_71 = values_70[0];
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"navigate"), R.Join(new V[] {v_67, new V((RuleFunction)(args_72 => {
V v_73 = args_72[0];
return R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"cardinal"), R.Join(new V[] {v_73}, new Results(R.Get(v_71, (V)"position")))));
return Results.Empty;
}))}, new Results((V)100.0d))));
v_56 = values_74[0];
v_57 = values_74[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_56}, new Results(v_57)));
var values_75 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(R.Get(v_67, (V)"state")))).First, (V)"position")));
V v_76 = values_75[0];
var values_77 = R.Join(new V[] {(V)(R.Get(R.Get(v_71, (V)"position"), (V)"x") - R.Get(v_76, (V)"x"))}, new Results((V)(R.Get(R.Get(v_71, (V)"position"), (V)"y") - R.Get(v_76, (V)"y"))));
V v_78 = values_77[0];
V v_79 = values_77[1];
var values_80 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_67, (V)"state"), (V)"clock")));
V v_81 = values_80[0];
var values_82 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"action"), R.Join(new V[] {v_67}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_78), new Entry((V)"dy", v_79))))));
v_56 = values_82[0];
v_57 = values_82[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_56}, new Results(v_57)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_71, (V)"open"), () => (V)(R.Get(R.Get(v_67, (V)"state"), (V)"clock") > v_81)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(v_76, (V)"x") != R.Get(R.Get(v_71, (V)"position"), (V)"x")), () => (V)(R.Get(v_76, (V)"y") != R.Get(R.Get(v_71, (V)"position"), (V)"y"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_67}, new Results((V)"DoorOpened")))));
var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"action"), R.Join(new V[] {v_67}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_78), new Entry((V)"dy", v_79))))));
v_56 = values_83[0];
v_57 = values_83[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_56}, new Results(v_57)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_76, (V)"x") == R.Get(R.Get(v_71, (V)"position"), (V)"x")), () => (V)(R.Get(v_76, (V)"y") == R.Get(R.Get(v_71, (V)"position"), (V)"y"))))));
return Results.Empty;
})))));
R.Call(v_10, R.Join(new V[] {(V)"generated-floor legal controls include Dash Sweep diagonal Sword and Wait"}, new Results(new V((RuleFunction)(args_84 => {

var values_85 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_86 = values_85[0];
var values_87 = Results.Empty;
V v_88 = values_87[0];
foreach (var row_89 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_90 = row_89[0];
V v_91 = row_89[1];
var values_92 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {R.Get(v_86, (V)"state"), (V)"dash", R.Get(v_91, (V)1.0d)}, new Results(R.Get(v_91, (V)2.0d)))));
V v_93 = values_92[0];
if (R.Truth(R.And(R.Get(v_93, (V)"valid"), () => (V)(R.Get(v_93, (V)"distance") > (V)0.0d)))) {
var values_94 = R.Join(new V[] {}, new Results(v_91));
v_88 = values_94[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_88}, new Results((V)"no valid dash from generated spawn")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"action"), R.Join(new V[] {v_86}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", R.Get(v_88, (V)1.0d)), new Entry((V)"dy", R.Get(v_88, (V)2.0d))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"action"), R.Join(new V[] {v_86}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (-(V)1.0d))))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"action"), R.Join(new V[] {v_86}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"action"), R.Join(new V[] {v_86}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_86, (V)"state"), (V)"clock") > (V)0.0d), () => R.Call(R.Get(v_4, (V)"has_event"), R.Join(new V[] {v_86}, new Results((V)"AttackPerformed"))).First))));
R.Call(v_11, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_86, (V)"state")))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_4, (V)"replay"), R.Join(new V[] {}, new Results(v_86))).First, (V)"state"))))));
return Results.Empty;
})))));
R.Call(v_10, R.Join(new V[] {(V)"legal death rejects later actions and a fresh seed can restart"}, new Results(new V((RuleFunction)(args_95 => {

var values_96 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_97 = values_96[0];
{ double end_100 = R.Num((V)120.0d), step_101 = R.Num((V)1.0d);
for (double i_99 = R.Num((V)1.0d); step_101 > 0 ? i_99 <= end_100 : i_99 >= end_100; i_99 += step_101) {
V v_98 = i_99;
if (R.Truth(R.Get(R.Get(v_97, (V)"state"), (V)"game_over"))) {
break;
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"action"), R.Join(new V[] {v_97}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_97, (V)"state"), (V)"game_over"), () => (V)(R.Get(R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(R.Get(v_97, (V)"state")))).First, (V)"hp") == (V)0.0d)))));
var values_102 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_97, (V)"state")))));
V v_103 = values_102[0];
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"action"), R.Join(new V[] {v_97}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_105 = values_104[0];
V v_106 = values_104[1];
V v_107 = values_104[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(v_105)), () => (V)(v_106 == (V)"game over")), () => (V)((V)R.Length(v_107) == (V)0.0d)))));
R.Call(v_11, R.Join(new V[] {v_103}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_97, (V)"state"))))));
var values_108 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_109 = values_108[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(R.Get(v_109, (V)"state"), (V)"game_over"))), () => (V)(R.Get(R.Get(v_109, (V)"state"), (V)"clock") == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_10, R.Join(new V[] {(V)"legal action trace survives full inert serialization and continuation"}, new Results(new V((RuleFunction)(args_110 => {

var values_111 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"stage_one"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_112 = values_111[0];
var values_113 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_112, (V)"state")))))));
V v_114 = values_113[0];
var values_115 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"decode"), R.Join(new V[] {}, new Results(v_114))))));
V v_116 = values_115[0];
R.Call(v_11, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_112, (V)"state")))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_116)))));
foreach (var row_117 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))))))) {
V v_118 = row_117[0];
V v_119 = row_117[1];
var values_120 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {R.Get(v_112, (V)"state")}, new Results(v_119))));
V v_121 = values_120[0];
V v_122 = values_120[1];
V v_123 = values_120[2];
var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_116}, new Results(v_119))));
V v_125 = values_124[0];
V v_126 = values_124[1];
V v_127 = values_124[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_121 == v_125), () => (V)(v_122 == v_126)))));
R.Call(v_11, R.Join(new V[] {v_123}, new Results(v_127)));
}
R.Call(v_11, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_112, (V)"state")))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_116)))));
return Results.Empty;
})))));
R.Call(v_10, R.Join(new V[] {(V)"presentation diagnostics do not change authoritative actions or RNG"}, new Results(new V((RuleFunction)(args_128 => {

var values_129 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {}, new Results((V)31.0d))));
V v_130 = values_129[0];
var values_131 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {}, new Results((V)31.0d))));
V v_132 = values_131[0];
var values_133 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_134 = values_133[0];
{ double end_137 = R.Num((V)8.0d), step_138 = R.Num((V)1.0d);
for (double i_136 = R.Num((V)1.0d); step_138 > 0 ? i_136 <= end_137 : i_136 >= end_137; i_136 += step_138) {
V v_135 = i_136;
var values_139 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))));
V v_140 = values_139[0];
var values_141 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_130}, new Results(v_140))));
V v_142 = values_141[0];
V v_143 = values_141[1];
V v_144 = values_141[2];
var values_145 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_132}, new Results(v_140))));
V v_146 = values_145[0];
V v_147 = values_145[1];
V v_148 = values_145[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_142 == v_146), () => (V)(v_143 == v_147)))));
R.Call(R.Get(v_8, (V)"sample"), R.Join(new V[] {v_134, (V)(v_135 / (V)60.0d), (V)0.2d, (V)2.0d, (V)3.0d}, new Results(R.Table(new Entry((V)"drawcalls", (V)4.0d)))));
R.Call(v_11, R.Join(new V[] {v_144}, new Results(v_148)));
}
}
R.Call(v_11, R.Join(new V[] {R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_130))).First}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_132)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_8, (V)"summary"), R.Join(new V[] {}, new Results(v_134))).First, (V)"samples") == (V)7.0d))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
