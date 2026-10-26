// Static C# port of tests/idea_toad_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 8af516f503f6ea4dbbbc9dec61cbc74246bd5516b53655d6d92ea7fa02a0d23a
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_toad_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_11 = values_10[0];
V v_12 = V.Nil;
v_12 = new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
var values_15 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_16 = values_15[0];

var values_17 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_16))));
V v_18 = values_17[0];

var values_19 = R.Join(new V[] {(V)8.0d}, new Results((V)6.0d));
var obj_20 = R.Get(v_18, (V)"position");
V key_21 = (V)"x";
var obj_22 = R.Get(v_18, (V)"position");
V key_23 = (V)"y";
R.Set(obj_20, key_21, values_19[0]);
R.Set(obj_22, key_23, values_19[1]);
var values_24 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_16, (V)"core:enemy/gulp_toad", (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
V v_25 = values_24[0];

var values_26 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_27 = v_25;
V key_28 = (V)"hp";
R.Set(obj_27, key_28, values_26[0]);
var values_29 = Results.Empty;
V v_30 = values_29[0];
if (R.Truth((V)(v_14 == (V)"currency"))) {
var values_32 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_currency"), R.Join(new V[] {v_16, (V)7.0d, R.Get(v_16, (V)"active_floor_id"), (V)9.0d, (V)6.0d}, new Results(new V((RuleFunction)(args_31 => {


return Results.Empty;
}))))));
v_30 = values_32[0];
} else {
var values_34 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_supply"), R.Join(new V[] {v_16, v_14, R.Get(v_16, (V)"active_floor_id"), (V)9.0d, (V)6.0d}, new Results(new V((RuleFunction)(args_33 => {


return Results.Empty;
}))))));
v_30 = values_34[0];
}
return R.Join(new V[] {v_16, v_18, v_25}, new Results(v_30));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Toad chooses a locked visible drop without consuming RNG or mutating the world"}, new Results(new V((RuleFunction)(args_35 => {

var values_36 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {}, new Results((V)"currency"))));
V v_37 = values_36[0];
V v_38 = values_36[1];
V v_39 = values_36[2];
V v_40 = values_36[3];

var values_41 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_37))));
V v_42 = values_41[0];
var values_43 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"choose"), R.Join(new V[] {v_37}, new Results(v_39))));
V v_44 = values_43[0];

R.Call(v_3, R.Join(new V[] {v_42}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_37)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_44, (V)"support"), (V)"kind") == (V)"gulp"), () => (V)(R.Get(R.Get(v_44, (V)"support"), (V)"drop_id") == R.Get(v_40, (V)"id"))), () => (V)(R.Get(R.Get(R.Get(v_44, (V)"cells"), (V)1.0d), (V)"x") == (V)9.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_37}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_39, (V)"pending_attack"), (V)"due") == (V)175.0d), () => (V)(!R.Truth(R.Get(v_40, (V)"carried_by")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_37}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_39, (V)"carried_drop_id") == R.Get(v_40, (V)"id")), () => (V)(R.Get(v_40, (V)"carried_by") == R.Get(v_39, (V)"id"))), () => (V)(!R.Truth(R.Get(v_40, (V)"collected")))), () => (V)(R.Get(v_37, (V)"currency") == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(R.Call(R.Get(v_7, (V)"choose"), R.Join(new V[] {v_37}, new Results(v_39))).First, (V)"support")))}, new Results((V)"one toad cannot swallow a second drop")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"intercepting a committed coin pickup cancels swallowing without creating replacement loot"}, new Results(new V((RuleFunction)(args_45 => {

var values_46 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {}, new Results((V)"currency"))));
V v_47 = values_46[0];
V v_48 = values_46[1];
V v_49 = values_46[2];
V v_50 = values_46[3];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_47}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_51 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_47}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_52 = values_51[0];
V v_53 = values_51[1];
V v_54 = values_51[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(v_50, (V)"collected"), () => (V)(R.Get(v_47, (V)"currency") == (V)7.0d)), () => (V)(!R.Truth(R.Get(v_49, (V)"carried_drop_id")))), () => (V)((V)R.Length(R.Get(v_47, (V)"drops")) == (V)1.0d)))));
var values_55 = R.Join(new V[] {}, new Results((V)false));
V v_56 = values_55[0];

foreach (var row_57 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_54)))))) {
V v_58 = row_57[0];
V v_59 = row_57[1];
if (R.Truth((V)(R.Get(v_59, (V)"kind") == (V)"GulpBlocked"))) {
var values_60 = R.Join(new V[] {}, new Results((V)true));
v_56 = values_60[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_56)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"standing over an unneeded ration protects it without consuming the food"}, new Results(new V((RuleFunction)(args_61 => {

var values_62 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {}, new Results((V)"food"))));
V v_63 = values_62[0];
V v_64 = values_62[1];
V v_65 = values_62[2];
V v_66 = values_62[3];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_64, (V)"hp") == R.Get(v_64, (V)"max_hp")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_63}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_63}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_64, (V)"position"), (V)"x") == (V)9.0d), () => (V)(!R.Truth(R.Get(v_66, (V)"collected")))), () => (V)(!R.Truth(R.Get(v_66, (V)"carried_by")))), () => (V)(!R.Truth(R.Get(v_65, (V)"carried_drop_id")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(R.Call(R.Get(v_7, (V)"choose"), R.Join(new V[] {v_63}, new Results(v_65))).First, (V)"support") == V.Nil)}, new Results((V)"guarded food is not loose reachable loot")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"swallowed coins and supplies retain their original ID and amount until a real kill releases them"}, new Results(new V((RuleFunction)(args_67 => {

foreach (var row_68 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"currency"), new Entry((V)2, (V)"food"), new Entry((V)3, (V)"health")))))))) {
V v_69 = row_68[0];
V v_70 = row_68[1];
var values_71 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {}, new Results(v_70))));
V v_72 = values_71[0];
V v_73 = values_71[1];
V v_74 = values_71[2];
V v_75 = values_71[3];

var values_76 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_77 = v_73;
V key_78 = (V)"hp";
R.Set(obj_77, key_78, values_76[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_72}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_72}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_72}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Get(v_75, (V)"collected"))), () => (V)(R.Get(v_72, (V)"currency") == (V)0.0d)), () => (V)(R.Get(v_73, (V)"hp") == (V)1.0d)))));
var values_79 = R.Join(new V[] {R.Get(v_75, (V)"id")}, new Results(R.Get(v_75, (V)"amount")));
V v_80 = values_79[0];
V v_81 = values_79[1];
var values_82 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_72}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_83 = values_82[0];
V v_84 = values_82[1];
V v_85 = values_82[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_72}, new Results(R.Get(v_74, (V)"id")))).First)), () => (V)(R.Get(v_75, (V)"id") == v_80)), () => (V)(R.Get(v_75, (V)"amount") == v_81)), () => (V)(!R.Truth(R.Get(v_75, (V)"carried_by")))), () => (V)(!R.Truth(R.Get(v_75, (V)"collected")))), () => (V)(R.Get(R.Get(v_75, (V)"position"), (V)"x") == (V)10.0d)))));
var values_86 = R.Join(new V[] {}, new Results((V)0.0d));
V v_87 = values_86[0];

foreach (var row_88 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_85)))))) {
V v_89 = row_88[0];
V v_90 = row_88[1];
if (R.Truth((V)(R.Get(v_90, (V)"kind") == (V)"DropRegurgitated"))) {
var values_91 = R.Join(new V[] {}, new Results((V)(v_87 + (V)1.0d)));
v_87 = values_91[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_87 == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_72}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_75, (V)"collected"))));
if (R.Truth((V)(v_70 == (V)"currency"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_72, (V)"currency") == (V)10.0d))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_73, (V)"hp") == (V)((V)1.0d + v_81)), () => (V)(R.Get(v_72, (V)"currency") == (V)3.0d)))));
}
var values_92 = R.Join(new V[] {R.Get(v_73, (V)"hp")}, new Results(R.Get(v_72, (V)"currency")));
V v_93 = values_92[0];
V v_94 = values_92[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_72}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_73, (V)"hp") == v_93), () => (V)(R.Get(v_72, (V)"currency") == v_94)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a saved gulp and saved cargo each resume with exact drop, reward and RNG state"}, new Results(new V((RuleFunction)(args_95 => {

var values_96 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {}, new Results((V)"currency"))));
V v_97 = values_96[0];
V v_98 = values_96[1];
V v_99 = values_96[2];
V v_100 = values_96[3];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_97}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_101 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_97))))))))));
V v_102 = values_101[0];
var values_103 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_97}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_104 = values_103[0];
V v_105 = values_103[1];
V v_106 = values_103[2];

var values_107 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_102}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_108 = values_107[0];
V v_109 = values_107[1];
V v_110 = values_107[2];
R.Call(v_3, R.Join(new V[] {v_106}, new Results(v_110)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_97))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_102)))));
var values_111 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_97))))))))));
v_102 = values_111[0];
foreach (var row_112 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))))))) {
V v_113 = row_112[0];
V v_114 = row_112[1];
var values_115 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_97}, new Results(v_114))));
V v_116 = values_115[0];
V v_117 = values_115[1];
V v_118 = values_115[2];

var values_119 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_102}, new Results(v_114))));
V v_120 = values_119[0];
V v_121 = values_119[1];
V v_122 = values_119[2];

R.Call(v_3, R.Join(new V[] {v_118}, new Results(v_122)));
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_97))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_102)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"missing, duplicated or falsely collected cargo is rejected without changing source snapshots"}, new Results(new V((RuleFunction)(args_123 => {

var values_124 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {}, new Results((V)"currency"))));
V v_125 = values_124[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_125}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_125}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_126 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_125))));
V v_127 = values_126[0];
var values_128 = R.Join(new V[] {}, new Results((V)true));
var obj_129 = R.Get(R.Get(v_127, (V)"drops"), (V)1.0d);
V key_130 = (V)"collected";
R.Set(obj_129, key_130, values_128[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_127))).First)))));
var values_131 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_125))));
v_127 = values_131[0];

var values_132 = R.Join(new V[] {}, new Results(R.Table()));
var obj_133 = v_127;
V key_134 = (V)"drops";
R.Set(obj_133, key_134, values_132[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_127))).First)))));
var values_135 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_125))));
v_127 = values_135[0];

var values_136 = R.Join(new V[] {}, new Results(R.Get(v_125, (V)"player_id")));
var obj_137 = R.Get(R.Get(v_127, (V)"drops"), (V)1.0d);
V key_138 = (V)"carried_by";
R.Set(obj_137, key_138, values_136[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_127))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_125)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"two committed gulps cannot duplicate one drop and natural generation uses the approved native body"}, new Results(new V((RuleFunction)(args_139 => {

var values_140 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {}, new Results((V)"currency"))));
V v_141 = values_140[0];
V v_142 = values_140[1];
V v_143 = values_140[2];
V v_144 = values_140[3];
var values_145 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_141, (V)"core:enemy/gulp_toad", (V)9.0d, (V)7.0d}, new Results((V)50.0d))));
V v_146 = values_145[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_141}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_147 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_141}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_148 = values_147[0];
V v_149 = values_147[1];
V v_150 = values_147[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_144, (V)"carried_by") == R.Get(v_143, (V)"id")), () => (V)(R.Get(v_143, (V)"carried_drop_id") == R.Get(v_144, (V)"id"))), () => (V)(!R.Truth(R.Get(v_146, (V)"carried_drop_id")))))));
var values_151 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_152 = values_151[0];
V v_153 = values_151[1];
foreach (var row_154 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_150)))))) {
V v_155 = row_154[0];
V v_156 = row_154[1];
if (R.Truth((V)(R.Get(v_156, (V)"kind") == (V)"DropSwallowed"))) {
var values_157 = R.Join(new V[] {}, new Results((V)(v_152 + (V)1.0d)));
v_152 = values_157[0];
} else if (R.Truth((V)(R.Get(v_156, (V)"kind") == (V)"GulpBlocked"))) {
var values_158 = R.Join(new V[] {}, new Results((V)(v_153 + (V)1.0d)));
v_153 = values_158[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_152 == (V)1.0d), () => (V)(v_153 == (V)1.0d)), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_141))))).First))));
var values_159 = R.Join(new V[] {}, new Results((V)false));
V v_160 = values_159[0];
{ double end_163 = R.Num((V)30.0d), step_164 = R.Num((V)1.0d);
for (double i_162 = R.Num((V)1.0d); step_164 > 0 ? i_162 <= end_163 : i_162 >= end_163; i_162 += step_164) {
V v_161 = i_162;
var values_165 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_161}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_166 = values_165[0];
foreach (var row_167 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_166, (V)"entities"))))))) {
V v_168 = row_167[0];
V v_169 = row_167[1];
if (R.Truth((V)(R.Get(v_169, (V)"definition_id") == (V)"core:enemy/gulp_toad"))) {
var values_170 = R.Join(new V[] {}, new Results((V)true));
v_160 = values_170[0];
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_166))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_166)))))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_160)));
var values_171 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_172 = values_171[0];
foreach (var row_173 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_174 = row_173[0];
V v_175 = row_173[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_172, (V)"packs"), v_175), (V)"missing")) == (V)0.0d))));
var values_176 = R.Join(new V[] {}, new Results((V)false));
V v_177 = values_176[0];
foreach (var row_178 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_172, (V)"packs"), v_175), (V)"bindings"))))))) {
V v_179 = row_178[0];
V v_180 = row_178[1];
if (R.Truth((V)(R.Get(v_180, (V)"id") == (V)"core:enemy/gulp_toad"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_180, (V)"index") == (V)422.0d))));

var values_181 = R.Join(new V[] {}, new Results((V)true));
v_177 = values_181[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_177)));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
