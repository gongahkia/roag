// Static C# port of tests/visual_juice_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 210cd25f8d39a06960ebf3761f0e23cbf6123c3a21444dffe11ec8429b2c1529
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visual_juice_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_juice"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.pixel_ui"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_15 = values_14[0];
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
V v_20 = args_17[2];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)1.0d), new Entry((V)"kind", (V)"AttackPerformed"), new Entry((V)"source_id", v_18), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"from_x", (V)8.0d), new Entry((V)"from_y", (V)8.0d), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)((V)8.0d + v_19)), new Entry((V)"y", (V)((V)8.0d + v_20)))))))));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"recoil follows committed player and enemy attack direction within the existing beat"}, new Results(new V((RuleFunction)(args_21 => {

foreach (var row_22 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d)))))))) {
V v_23 = row_22[0];
V v_24 = row_22[1];
foreach (var row_25 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_26 = row_25[0];
V v_27 = row_25[1];
var values_28 = R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {v_24, R.Get(v_27, (V)1.0d)}, new Results(R.Get(v_27, (V)2.0d)))));
V v_29 = values_28[0];

var values_30 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_29))));
V v_31 = values_30[0];
var values_32 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), Results.Empty));
V v_33 = values_32[0];

R.Call(R.Get(v_7, (V)"add"), R.Join(new V[] {v_33, R.Table(new Entry((V)1, v_29))}, new Results((V)1.0d)));
var values_34 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_33, (V)"items"), (V)1.0d), (V)"duration")));
V v_35 = values_34[0];

var values_36 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_33))));
V v_37 = values_36[0];
var values_38 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attack_offset"), R.Join(new V[] {v_33, v_24, (V)"floor"}, new Results((V)true))));
V v_39 = values_38[0];
V v_40 = values_38[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_39 == (V)0.0d), () => (V)(v_40 == (V)0.0d)))));
R.Call(R.Get(v_7, (V)"update"), R.Join(new V[] {v_33}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_5, (V)"attack_seconds")}, new Results(v_35))).First / (V)2.0d))));
var values_41 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attack_offset"), R.Join(new V[] {v_33, v_24, (V)"floor"}, new Results((V)true))));
v_39 = values_41[0];
v_40 = values_41[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_39 == (V)(R.Get(v_27, (V)1.0d) * (V)3.0d)), () => (V)(v_40 == (V)(R.Get(v_27, (V)2.0d) * (V)3.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_33, (V)"items"), (V)1.0d), (V)"duration") == v_35), () => (V)((V)R.Length(R.Get(v_33, (V)"items")) == (V)1.0d)))));
var values_42 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_33))));
V v_43 = values_42[0];

R.Call(R.Get(v_5, (V)"attack_offset"), R.Join(new V[] {v_33, v_24, (V)"floor"}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_33))).First == v_43))));
R.Call(R.Get(v_7, (V)"update"), R.Join(new V[] {v_33}, new Results(v_35)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"busy"), R.Join(new V[] {}, new Results(v_33))).First)))));
var values_44 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attack_offset"), R.Join(new V[] {v_33, v_24, (V)"floor"}, new Results((V)true))));
v_39 = values_44[0];
v_40 = values_44[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_39 == (V)0.0d), () => (V)(v_40 == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_29))).First == v_31), () => (V)(v_37 != R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_33))).First)))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"recoil excludes windups, hidden-floor history, disabled effects and concurrent actor slides"}, new Results(new V((RuleFunction)(args_45 => {

var values_46 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), Results.Empty));
V v_47 = values_46[0];

var values_48 = R.Join(new V[] {}, R.Call(v_16, R.Join(new V[] {(V)1.0d, (V)1.0d}, new Results((V)0.0d))));
V v_49 = values_48[0];

R.Call(R.Get(v_7, (V)"add"), R.Join(new V[] {v_47, R.Table(new Entry((V)1, v_49))}, new Results((V)1.0d)));

R.Call(R.Get(v_7, (V)"update"), R.Join(new V[] {v_47}, new Results((V)0.04d)));
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)"other"), new Entry((V)3, (V)true))), new Entry((V)2, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)"floor"), new Entry((V)3, (V)true))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)"floor"), new Entry((V)3, (V)false)))))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
var values_53 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attack_offset"), R.Join(new V[] {v_47}, R.Call(rt.Global("unpack"), R.Join(new V[] {}, new Results(v_52))))));
V v_54 = values_53[0];
V v_55 = values_53[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_54 == (V)0.0d), () => (V)(v_55 == (V)0.0d)))));
}
var values_56 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1.0d, R.Table()))));
var obj_57 = R.Get(R.Get(v_47, (V)"items"), (V)1.0d);
V key_58 = (V)"motions";
R.Set(obj_57, key_58, values_56[0]);

var values_59 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attack_offset"), R.Join(new V[] {v_47, (V)1.0d, (V)"floor"}, new Results((V)true))));
V v_60 = values_59[0];
V v_61 = values_59[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_60 == (V)0.0d), () => (V)(v_61 == (V)0.0d)))));
var values_62 = R.Join(new V[] {}, new Results(V.Nil));
var obj_63 = R.Get(R.Get(v_47, (V)"items"), (V)1.0d);
V key_64 = (V)"motions";
R.Set(obj_63, key_64, values_62[0]);

var values_65 = R.Join(new V[] {}, new Results((V)"WindupStarted"));
var obj_66 = v_49;
V key_67 = (V)"kind";
R.Set(obj_66, key_67, values_65[0]);
var values_68 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attack_offset"), R.Join(new V[] {v_47, (V)1.0d, (V)"floor"}, new Results((V)true))));
v_60 = values_68[0];
v_61 = values_68[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_60 == (V)0.0d), () => (V)(v_61 == (V)0.0d)))));
var values_69 = R.Join(new V[] {}, new Results((V)"AttackPerformed"));
var obj_70 = v_49;
V key_71 = (V)"kind";
R.Set(obj_70, key_71, values_69[0]);

var values_72 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)8.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)8.0d))))));
var obj_73 = v_49;
V key_74 = (V)"target_cells";
R.Set(obj_73, key_74, values_72[0]);
var values_75 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"attack_offset"), R.Join(new V[] {v_47, (V)1.0d, (V)"floor"}, new Results((V)true))));
v_60 = values_75[0];
v_61 = values_75[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_60 == (V)0.0d), () => (V)(v_61 == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all fifteen class attacks retain exact events, snapshots and playback duration under recoil sampling"}, new Results(new V((RuleFunction)(args_76 => {

foreach (var row_77 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))).First, (V)"order"))))))) {
V v_78 = row_77[0];
V v_79 = row_77[1];
var values_80 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new"), R.Join(new V[] {(V)17.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", v_79))))));
V v_81 = values_80[0];

var values_82 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_81))));
V v_83 = values_82[0];
var values_84 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_81}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_85 = values_84[0];
V v_86 = values_84[1];
V v_87 = values_84[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_85}, new Results(v_86)));
var values_88 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_81))))));
V v_89 = values_88[0];

var values_90 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_87))));
V v_91 = values_90[0];
var values_92 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), Results.Empty));
V v_93 = values_92[0];

R.Call(R.Get(v_7, (V)"add"), R.Join(new V[] {v_93, v_87}, new Results(R.Get(v_81, (V)"player_id"))));
var values_94 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_93))));
V v_95 = values_94[0];
{ double end_98 = R.Num((V)8.0d), step_99 = R.Num((V)1.0d);
for (double i_97 = R.Num((V)1.0d); step_99 > 0 ? i_97 <= end_98 : i_97 >= end_98; i_97 += step_99) {
V v_96 = i_97;
R.Call(R.Get(v_5, (V)"attack_offset"), R.Join(new V[] {v_93, R.Get(v_81, (V)"player_id"), R.Get(v_81, (V)"active_floor_id")}, new Results((V)true)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_93))).First == v_95))));
{ double end_102 = R.Num((V)100.0d), step_103 = R.Num((V)1.0d);
for (double i_101 = R.Num((V)1.0d); step_103 > 0 ? i_101 <= end_102 : i_101 >= end_102; i_101 += step_103) {
V v_100 = i_101;
R.Call(R.Get(v_5, (V)"attack_offset"), R.Join(new V[] {v_93, R.Get(v_81, (V)"player_id"), R.Get(v_81, (V)"active_floor_id")}, new Results((V)true)));

R.Call(R.Get(v_7, (V)"update"), R.Join(new V[] {v_93}, new Results((V)0.007d)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_7, (V)"busy"), R.Join(new V[] {}, new Results(v_93))).First)), () => (V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_87))).First == v_91)), () => (V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_81))))).First == v_89)))));
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, new Results(v_83))));
V v_105 = values_104[0];

var values_106 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_105}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_107 = values_106[0];
V v_108 = values_106[1];
V v_109 = values_106[2];
R.Call(v_3, R.Join(new V[] {v_87}, new Results(v_109)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_81))).First}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_105)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real currency and food collection produce cosmetic arcs after successful pickup only"}, new Results(new V((RuleFunction)(args_110 => {

var values_111 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_112 = values_111[0];

var values_113 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_112))));
V v_114 = values_113[0];

var values_115 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_116 = v_114;
V key_117 = (V)"hp";
R.Set(obj_116, key_117, values_115[0]);
R.Call(R.Get(v_15, (V)"spawn_currency"), R.Join(new V[] {v_112, (V)3.0d, R.Get(v_112, (V)"active_floor_id"), (V)3.0d, (V)2.0d}, new Results(new V((RuleFunction)(args_118 => {


return Results.Empty;
})))));
R.Call(R.Get(v_15, (V)"spawn_supply"), R.Join(new V[] {v_112, (V)"food", R.Get(v_112, (V)"active_floor_id"), (V)3.0d, (V)2.0d}, new Results(new V((RuleFunction)(args_119 => {


return Results.Empty;
})))));
var values_120 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_121 = values_120[0];

var values_122 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_112))));
V v_123 = values_122[0];
R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_121, R.Table(), R.Get(v_112, (V)"player_id"), R.Get(v_112, (V)"active_floor_id")}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_121, (V)"items")) == (V)0.0d))));
R.Call(v_3, R.Join(new V[] {v_123}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_112)))));
var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_112}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_125 = values_124[0];
V v_126 = values_124[1];
V v_127 = values_124[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_125}, new Results(v_126)));
var values_128 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_112))));
V v_129 = values_128[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_112, (V)"currency") == (V)3.0d), () => (V)(R.Get(v_114, (V)"hp") == (V)14.0d)))));
R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_121, v_127, R.Get(v_112, (V)"player_id"), R.Get(v_112, (V)"active_floor_id")}, new Results((V)true)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_121, (V)"items")) == (V)2.0d), () => (V)(R.Get(R.Get(R.Get(v_121, (V)"items"), (V)1.0d), (V)"binding") == (V)"core:drop/currency")), () => (V)(R.Get(R.Get(R.Get(v_121, (V)"items"), (V)2.0d), (V)"binding") == (V)"core:drop/food")))));
var values_130 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_121, (V)"items"), (V)1.0d)));
V v_131 = values_130[0];

var values_132 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"reward_pose"), R.Join(new V[] {v_131, (V)100.0d, (V)200.0d, (V)450.0d}, new Results((V)578.0d))));
V v_133 = values_132[0];
V v_134 = values_132[1];
V v_135 = values_132[2];
V v_136 = values_132[3];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_133 == (V)100.0d), () => (V)(v_134 == (V)200.0d)), () => (V)(v_135 == (V)18.0d)), () => (V)(v_136 == (V)1.0d)))));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_121, (V)0.24d, R.Get(v_112, (V)"active_floor_id")}, new Results((V)true)));
var values_137 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"reward_pose"), R.Join(new V[] {v_131, (V)100.0d, (V)200.0d, (V)450.0d}, new Results((V)578.0d))));
v_133 = values_137[0];
v_134 = values_137[1];
v_135 = values_137[2];
v_136 = values_137[3];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_133 == (V)275.0d), () => (V)(v_134 == (V)359.0d)), () => (V)(v_135 > (V)8.0d)), () => (V)(v_136 > (V)0.0d)))));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_121, (V)0.3d, R.Get(v_112, (V)"active_floor_id")}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_121, (V)"items")) == (V)0.0d))));
R.Call(v_3, R.Join(new V[] {v_129}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_112)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"unaffordable chest is inert; a real claim creates one boon flight and a short chest hop"}, new Results(new V((RuleFunction)(args_138 => {

var values_139 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_140 = values_139[0];

var values_141 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_chest"), R.Join(new V[] {v_140, (V)"core:chest/standard", (V)3.0d}, new Results((V)2.0d))));
V v_142 = values_141[0];
var values_143 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_144 = values_143[0];

var values_145 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_140}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_142, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
V v_146 = values_145[0];
V v_147 = values_145[1];
V v_148 = values_145[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_146)))));

R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_144, v_148, R.Get(v_140, (V)"player_id"), R.Get(v_140, (V)"active_floor_id")}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_144, (V)"items")) == (V)0.0d))));
var values_149 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_150 = v_140;
V key_151 = (V)"currency";
R.Set(obj_150, key_151, values_149[0]);
var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_140}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_142, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
v_146 = values_152[0];
v_147 = values_152[1];
v_148 = values_152[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_146)));
var values_153 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_140))));
V v_154 = values_153[0];

R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_144, v_148, R.Get(v_140, (V)"player_id"), R.Get(v_140, (V)"active_floor_id")}, new Results((V)true)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_144, (V)"items")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_144, (V)"items"), (V)1.0d), (V)"binding") == R.Concat(R.Get(R.Get(R.Get(v_142, (V)"offers"), (V)1.0d), (V)"boon_id"), (V)"/icon"))))));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_144, (V)0.11d, R.Get(v_140, (V)"active_floor_id")}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_5, (V)"chest_offset"), R.Join(new V[] {v_144, R.Get(v_142, (V)"id")}, new Results((V)true))).First == (-(V)4.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_5, (V)"chest_offset"), R.Join(new V[] {v_144, R.Get(v_142, (V)"id")}, new Results((V)false))).First == (V)0.0d), () => (V)(R.Call(R.Get(v_5, (V)"chest_offset"), R.Join(new V[] {v_144, (V)(R.Get(v_142, (V)"id") + (V)1.0d)}, new Results((V)true))).First == (V)0.0d)))));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_144, (V)0.12d, R.Get(v_140, (V)"active_floor_id")}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_5, (V)"chest_offset"), R.Join(new V[] {v_144, R.Get(v_142, (V)"id")}, new Results((V)true))).First == (V)0.0d))));
R.Call(v_3, R.Join(new V[] {v_154}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_140)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"reward effects are capped, handle-free and discarded on floor change or disable"}, new Results(new V((RuleFunction)(args_155 => {

var values_156 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_157 = values_156[0];

var values_158 = R.Join(new V[] {}, new Results(R.Table()));
V v_159 = values_158[0];
{ double end_162 = R.Num((V)1000.0d), step_163 = R.Num((V)1.0d);
for (double i_161 = R.Num((V)1.0d); step_163 > 0 ? i_161 <= end_162 : i_161 >= end_162; i_161 += step_163) {
V v_160 = i_161;
var values_164 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"CurrencyCollected"), new Entry((V)"source_id", (V)1.0d), new Entry((V)"amount", (V)1.0d), new Entry((V)"floor_id", (V)"a"), new Entry((V)"x", v_160), new Entry((V)"y", (V)2.0d))));
var obj_165 = v_159;
V key_166 = v_160;
R.Set(obj_165, key_166, values_164[0]);
}
}
R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_157, v_159, (V)1.0d, (V)"a"}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_157, (V)"items")) == R.Get(v_5, (V)"reward_limit")))));
foreach (var row_167 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_157, (V)"items"))))))) {
V v_168 = row_167[0];
V v_169 = row_167[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_169, (V)"texture"))), () => (V)(!R.Truth(R.Get(v_169, (V)"quad")))))));
}
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_157, (V)0.0d, (V)"b"}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_157, (V)"items")) == (V)0.0d))));
R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_157, v_159, (V)1.0d, (V)"b"}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_157, (V)"items")) == (V)0.0d)}, new Results((V)"retired floor leaked into view")));
R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_157, v_159, (V)1.0d, (V)"a"}, new Results((V)false)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_157, (V)"items")) == (V)0.0d))));
R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_157, v_159, (V)1.0d, (V)"a"}, new Results((V)true)));

R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_157, (V)0.0d, (V)"a"}, new Results((V)false)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_157, (V)"items")) == (V)0.0d))));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_157, (V)0.01d, (V)"a"}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_157, (V)"items")) == (V)0.0d)}, new Results((V)"reenabling replayed old pickups")));
R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_157, v_159, (V)2.0d, (V)"a"}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_157, (V)"items")) == (V)0.0d)}, new Results((V)"another actor made a player pickup arc")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real direct hits and blocks activate once at their displayed beat without changing events or recovery"}, new Results(new V((RuleFunction)(args_170 => {

foreach (var row_171 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)100.0d)))))))) {
V v_172 = row_171[0];
V v_173 = row_171[1];
var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_175 = values_174[0];

var values_176 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_175, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)10000.0d))));
V v_177 = values_176[0];
var values_178 = R.Join(new V[] {(V)100.0d, (V)100.0d}, new Results(v_173));
var obj_179 = v_177;
V key_180 = (V)"hp";
var obj_181 = v_177;
V key_182 = (V)"max_hp";
var obj_183 = v_177;
V key_184 = (V)"defense";
R.Set(obj_179, key_180, values_178[0]);
R.Set(obj_181, key_182, values_178[1]);
R.Set(obj_183, key_184, values_178[2]);
var values_185 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_175))));
V v_186 = values_185[0];
var values_187 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_175}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_188 = values_187[0];
V v_189 = values_187[1];
V v_190 = values_187[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_188}, new Results(v_189)));
var values_191 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_175))))));
V v_192 = values_191[0];

var values_193 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_190))));
V v_194 = values_193[0];
var values_195 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), Results.Empty));
V v_196 = values_195[0];

R.Call(R.Get(v_7, (V)"add"), R.Join(new V[] {v_196, v_190}, new Results(R.Get(v_175, (V)"player_id"))));

var values_197 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_196))));
V v_198 = values_197[0];
var values_199 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_200 = values_199[0];

R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_200, v_190, R.Get(v_175, (V)"player_id"), R.Get(v_175, (V)"active_floor_id")}, new Results((V)true)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_200, (V)"impacts")) == (V)0.0d)}, new Results((V)"impact appeared ahead of playback")));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_200, (V)0.0d, R.Get(v_175, (V)"active_floor_id"), (V)true, v_196}, new Results(R.Get(v_175, (V)"player_id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_200, (V)"impacts")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_200, (V)"impacts"), (V)1.0d), (V)"kind") == R.Or(R.And((V)(v_173 == (V)0.0d), () => (V)"hit"), () => (V)"blocked"))))));
var values_201 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_200, (V)"impacts"), (V)1.0d)));
V v_202 = values_201[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_202, (V)"dx") == (V)1.0d), () => (V)(R.Get(v_202, (V)"dy") == (V)0.0d)), () => R.Get(v_202, (V)"friendly")))));
R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_200, R.Table(), R.Get(v_175, (V)"player_id"), R.Get(v_175, (V)"active_floor_id")}, new Results((V)true)));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_200, (V)0.05d, R.Get(v_175, (V)"active_floor_id"), (V)true, v_196}, new Results(R.Get(v_175, (V)"player_id"))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_200, (V)"impacts")) == (V)1.0d))));
var values_203 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"impact_pose"), R.Join(new V[] {v_202, (V)3.0d}, new Results((V)40.0d))));
V v_204 = values_203[0];
V v_205 = values_203[1];
V v_206 = values_203[2];
V v_207 = values_203[3];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_204 > (V)0.0d), () => (V)(v_205 == (V)0.0d)), () => (V)(v_206 > (V)0.0d)), () => (V)(v_207 > (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_196))).First == v_198), () => (V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_190))).First == v_194)), () => (V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_175))))).First == v_192)))));
var values_208 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, new Results(v_186))));
V v_209 = values_208[0];

var values_210 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_209}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_211 = values_210[0];
V v_212 = values_210[1];
V v_213 = values_210[2];
R.Call(v_3, R.Join(new V[] {v_190}, new Results(v_213)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_175))).First}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_209)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"sparks exclude zero damage and DOT; blocked rings and accents honor floor and off without replay"}, new Results(new V((RuleFunction)(args_214 => {

var values_215 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), Results.Empty));
V v_216 = values_215[0];

var values_217 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_218 = values_217[0];
R.Call(R.Get(v_7, (V)"add"), R.Join(new V[] {v_216, R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"floor_id", (V)"a"), new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)3.0d), new Entry((V)"actual_loss", (V)2.0d), new Entry((V)"direct_attack", (V)false))))}, new Results((V)1.0d)));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_218, (V)0.0d, (V)"a", (V)true, v_216}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_218, (V)"impacts")) == (V)0.0d))));
var values_219 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), Results.Empty));
v_216 = values_219[0];

R.Call(R.Get(v_7, (V)"add"), R.Join(new V[] {v_216, R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"DamageBlocked"), new Entry((V)"floor_id", (V)"a"), new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)3.0d))))}, new Results((V)1.0d)));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_218, (V)0.0d, (V)"a", (V)true, v_216}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_218, (V)"impacts")) == (V)1.0d))));
var values_220 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"ring_pose"), R.Join(new V[] {R.Get(R.Get(v_218, (V)"impacts"), (V)1.0d)}, new Results((V)40.0d))));
V v_221 = values_220[0];
V v_222 = values_220[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_221 == (V)5.0d), () => (V)(v_222 == (V)1.0d)))));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_218, (V)0.08d, (V)"a", (V)true, v_216}, new Results((V)1.0d)));
var values_223 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"ring_pose"), R.Join(new V[] {R.Get(R.Get(v_218, (V)"impacts"), (V)1.0d)}, new Results((V)40.0d))));
V v_224 = values_223[0];
V v_225 = values_223[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_224 > v_221), () => (V)(v_225 < v_222)))));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_218, (V)0.0d, (V)"a", (V)false, v_216}, new Results((V)1.0d)));

R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_218, (V)0.0d, (V)"a", (V)true, v_216}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_218, (V)"impacts")) == (V)0.0d)}, new Results((V)"effects-on replayed an old hit")));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_218, (V)0.0d, (V)"b", (V)true, v_216}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_218, (V)"impacts")) == (V)0.0d)}, new Results((V)"retired-floor impact leaked")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a real lethal hit retains enemy artwork and one level badge while exact save continuation survives"}, new Results(new V((RuleFunction)(args_226 => {

var values_227 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_228 = values_227[0];

var values_229 = R.Join(new V[] {}, new Results((V)4.0d));
var obj_230 = R.Get(v_228, (V)"progression");
V key_231 = (V)"xp_total";
R.Set(obj_230, key_231, values_229[0]);
var values_232 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_228, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)10000.0d))));
V v_233 = values_232[0];

var values_234 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_235 = v_233;
V key_236 = (V)"hp";
R.Set(obj_235, key_236, values_234[0]);
var values_237 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_228}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_238 = values_237[0];
V v_239 = values_237[1];
V v_240 = values_237[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_238}, new Results(v_239)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_9, (V)"entity"), R.Join(new V[] {v_228}, new Results(R.Get(v_233, (V)"id")))).First)), () => (V)(R.Get(R.Get(v_228, (V)"progression"), (V)"level") == (V)2.0d)))));
var values_241 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_228))));
V v_242 = values_241[0];

var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), Results.Empty));
V v_244 = values_243[0];

R.Call(R.Get(v_7, (V)"add"), R.Join(new V[] {v_244, v_240}, new Results(R.Get(v_228, (V)"player_id"))));
var values_245 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_246 = values_245[0];

R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_246, v_240, R.Get(v_228, (V)"player_id"), R.Get(v_228, (V)"active_floor_id")}, new Results((V)true)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_246, (V)"celebrations")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_246, (V)"celebrations"), (V)1.0d), (V)"kind") == (V)"level")), () => (V)(R.Get(R.Get(R.Get(v_246, (V)"celebrations"), (V)1.0d), (V)"level") == (V)2.0d)))));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_246, (V)0.0d, R.Get(v_228, (V)"active_floor_id"), (V)true, v_244}, new Results(R.Get(v_228, (V)"player_id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_246, (V)"crumbles")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_246, (V)"crumbles"), (V)1.0d), (V)"binding") == R.Get(v_233, (V)"definition_id"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(R.Get(R.Get(v_246, (V)"crumbles"), (V)1.0d), (V)"texture"))), () => (V)(!R.Truth(R.Get(R.Get(R.Get(v_246, (V)"crumbles"), (V)1.0d), (V)"quad")))))));
var values_247 = R.Join(new V[] {R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, new Results(v_242))).First}, R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, new Results(v_242))));
V v_248 = values_247[0];
V v_249 = values_247[1];
{ double end_252 = R.Num((V)60.0d), step_253 = R.Num((V)1.0d);
for (double i_251 = R.Num((V)1.0d); step_253 > 0 ? i_251 <= end_252 : i_251 >= end_252; i_251 += step_253) {
V v_250 = i_251;
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_246, (V)0.01d, R.Get(v_228, (V)"active_floor_id"), (V)true, v_244}, new Results(R.Get(v_228, (V)"player_id"))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)((V)R.Length(R.Get(v_246, (V)"crumbles")) == (V)0.0d), () => (V)((V)R.Length(R.Get(v_246, (V)"impacts")) == (V)0.0d))}, new Results((V)"expired beat respawned effects")));
R.Call(v_3, R.Join(new V[] {v_242}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_228)))));

R.Call(v_3, R.Join(new V[] {v_242}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_248)))));
var values_254 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_248}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_255 = values_254[0];
V v_256 = values_254[1];
V v_257 = values_254[2];

var values_258 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_249}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_259 = values_258[0];
V v_260 = values_258[1];
V v_261 = values_258[2];
R.Call(v_3, R.Join(new V[] {v_257}, new Results(v_261)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_248))).First}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_249)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"actual generated secret discovery celebrates only when its existing playback beat displays"}, new Results(new V((RuleFunction)(args_262 => {

var values_263 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_264 = values_263[0];
foreach (var row_265 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_264, (V)"entities"))))))) {
V v_266 = row_265[0];
V v_267 = row_265[1];
if (R.Truth((V)(R.Get(v_267, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))).First, (V)"remove_for_actor"), R.Join(new V[] {v_264}, new Results(R.Get(v_267, (V)"id"))));
}
}
var values_268 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_264, (V)"secrets"), (V)1.0d)));
V v_269 = values_268[0];

var values_270 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_264))));
V v_271 = values_270[0];
var values_272 = R.Join(new V[] {R.Get(v_269, (V)"x")}, new Results(R.Get(v_269, (V)"y")));
var obj_273 = R.Get(v_271, (V)"position");
V key_274 = (V)"x";
var obj_275 = R.Get(v_271, (V)"position");
V key_276 = (V)"y";
R.Set(obj_273, key_274, values_272[0]);
R.Set(obj_275, key_276, values_272[1]);

R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))).First, (V)"recompute"), R.Join(new V[] {}, new Results(v_264)));
var values_277 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_264}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(R.Get(v_269, (V)"wall_x") - R.Get(v_269, (V)"x"))), new Entry((V)"dy", (V)(R.Get(v_269, (V)"wall_y") - R.Get(v_269, (V)"y"))))))));
V v_278 = values_277[0];
V v_279 = values_277[1];
V v_280 = values_277[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_278}, new Results(v_279)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_269, (V)"revealed"))));
var values_281 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_264))));
V v_282 = values_281[0];

var values_283 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), Results.Empty));
V v_284 = values_283[0];

R.Call(R.Get(v_7, (V)"add"), R.Join(new V[] {v_284, v_280}, new Results(R.Get(v_264, (V)"player_id"))));
var values_285 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_286 = values_285[0];

R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_286, v_280, R.Get(v_264, (V)"player_id"), R.Get(v_264, (V)"active_floor_id")}, new Results((V)true)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_286, (V)"celebrations")) == (V)0.0d))));
while (R.Truth(R.And(R.Call(R.Get(v_7, (V)"current"), R.Join(new V[] {}, new Results(v_284))).First, () => (V)(R.Get(R.Call(R.Get(v_7, (V)"current"), R.Join(new V[] {}, new Results(v_284))).First, (V)"kind") != (V)"SecretDiscovered")))) {
R.Call(R.Get(v_7, (V)"update"), R.Join(new V[] {v_284}, new Results(R.Get(v_284, (V)"remaining"))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"current"), R.Join(new V[] {}, new Results(v_284)))));

R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_286, (V)0.0d, R.Get(v_264, (V)"active_floor_id"), (V)true, v_284}, new Results(R.Get(v_264, (V)"player_id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_286, (V)"celebrations")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_286, (V)"celebrations"), (V)1.0d), (V)"kind") == (V)"secret")))));
var values_287 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"celebration_pose"), R.Join(new V[] {R.Get(R.Get(v_286, (V)"celebrations"), (V)1.0d)}, new Results((V)40.0d))));
V v_288 = values_287[0];
V v_289 = values_287[1];
V v_290 = values_287[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_288 == (V)10.0d), () => (V)(v_289 == (V)1.0d)), () => (V)(v_290 == (V)0.0d)))));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_286, (V)0.81d, R.Get(v_264, (V)"active_floor_id"), (V)true, v_284}, new Results(R.Get(v_264, (V)"player_id"))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_286, (V)"celebrations")) == (V)0.0d))));
R.Call(v_3, R.Join(new V[] {v_282}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_264)))));

R.Call(v_3, R.Join(new V[] {v_282}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, new Results(v_282)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"nine crumble clips cover the original sprite and drift apart with deterministic bounded lifetime"}, new Results(new V((RuleFunction)(args_291 => {

foreach (var row_292 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)16.0d), new Entry((V)2, (V)40.0d)))))))) {
V v_293 = row_292[0];
V v_294 = row_292[1];
var values_295 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"age", (V)0.0d))));
V v_296 = values_295[0];

var values_297 = R.Join(new V[] {}, new Results((V)0.0d));
V v_298 = values_297[0];
{ double end_301 = R.Num((V)9.0d), step_302 = R.Num((V)1.0d);
for (double i_300 = R.Num((V)1.0d); step_302 > 0 ? i_300 <= end_301 : i_300 >= end_301; i_300 += step_302) {
V v_299 = i_300;
var values_303 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"crumble_piece"), R.Join(new V[] {v_296, v_299}, new Results(v_294))));
V v_304 = values_303[0];
V v_305 = values_303[1];
V v_306 = values_303[2];
V v_307 = values_303[3];
V v_308 = values_303[4];
V v_309 = values_303[5];
V v_310 = values_303[6];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_304 >= (V)0.0d), () => (V)(v_305 >= (V)0.0d)), () => (V)((V)(v_304 + v_306) <= v_294)), () => (V)((V)(v_305 + v_307) <= v_294)), () => (V)(v_308 == (V)0.0d)), () => (V)(v_309 == (V)0.0d)), () => (V)(v_310 == (V)1.0d)))));
var values_311 = R.Join(new V[] {}, new Results((V)(v_298 + (V)(v_306 * v_307))));
v_298 = values_311[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_298 == (V)(v_294 * v_294)))));
var values_312 = R.Join(new V[] {}, new Results((V)0.12d));
var obj_313 = v_296;
V key_314 = (V)"age";
R.Set(obj_313, key_314, values_312[0]);
var values_315 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"crumble_piece"), R.Join(new V[] {v_296, (V)1.0d}, new Results(v_294))));
V v_316 = values_315[0];
V v_317 = values_315[1];
V v_318 = values_315[2];
V v_319 = values_315[3];
V v_320 = values_315[4];
V v_321 = values_315[5];
V v_322 = values_315[6];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_320 < (V)0.0d), () => (V)(v_321 < (V)0.0d)), () => (V)(v_322 == (V)0.25d)))));
var values_323 = R.Join(new V[] {}, new Results((V)0.24d));
var obj_324 = v_296;
V key_325 = (V)"age";
R.Set(obj_324, key_325, values_323[0]);

var values_326 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"crumble_piece"), R.Join(new V[] {v_296, (V)9.0d}, new Results(v_294))));
V v_327 = values_326[0];
V v_328 = values_326[1];
V v_329 = values_326[2];
V v_330 = values_326[3];
V v_331 = values_326[4];
V v_332 = values_326[5];
V v_333 = values_326[6];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_333 == (V)0.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"burst crowds and many levels have bounded handle-free feedback; disable and floor changes clear everything"}, new Results(new V((RuleFunction)(args_334 => {

var values_335 = R.Join(new V[] {}, new Results(R.Table()));
V v_336 = values_335[0];

var values_337 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), Results.Empty));
V v_338 = values_337[0];
{ double end_341 = R.Num((V)1000.0d), step_342 = R.Num((V)1.0d);
for (double i_340 = R.Num((V)1.0d); step_342 > 0 ? i_340 <= end_341 : i_340 >= end_341; i_340 += step_342) {
V v_339 = i_340;
var values_343 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"target_id", v_339), new Entry((V)"floor_id", (V)"a"), new Entry((V)"x", v_339), new Entry((V)"y", (V)2.0d), new Entry((V)"actual_loss", (V)1.0d), new Entry((V)"direct_attack", (V)true))));
var obj_344 = v_336;
V key_345 = (V)((V)R.Length(v_336) + (V)1.0d);
R.Set(obj_344, key_345, values_343[0]);
var values_346 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"Died"), new Entry((V)"target_id", v_339), new Entry((V)"target_faction", (V)"enemy"), new Entry((V)"target_definition_id", (V)"core:enemy/mossbound_guard"), new Entry((V)"floor_id", (V)"a"), new Entry((V)"x", v_339), new Entry((V)"y", (V)2.0d))));
var obj_347 = v_336;
V key_348 = (V)((V)R.Length(v_336) + (V)1.0d);
R.Set(obj_347, key_348, values_346[0]);
var values_349 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"LevelUp"), new Entry((V)"source_id", (V)1.0d), new Entry((V)"new_level", v_339), new Entry((V)"source_position", R.Table(new Entry((V)"floor_id", (V)"a"), new Entry((V)"x", (V)1.0d), new Entry((V)"y", (V)2.0d))))));
var obj_350 = v_336;
V key_351 = (V)((V)R.Length(v_336) + (V)1.0d);
R.Set(obj_350, key_351, values_349[0]);
}
}
var values_352 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"event", R.Get(v_336, (V)1.0d)), new Entry((V)"events", v_336), new Entry((V)"duration", (V)0.3d))));
V v_353 = values_352[0];
var values_354 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"items", R.Table(new Entry((V)1, v_353))), new Entry((V)"facings", R.Table()), new Entry((V)"remaining", (V)0.3d))));
V v_355 = values_354[0];
R.Call(R.Get(v_5, (V)"add"), R.Join(new V[] {v_338, v_336, (V)1.0d, (V)"a"}, new Results((V)true)));

R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_338, (V)0.0d, (V)"a", (V)true, v_355}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_338, (V)"impacts")) == R.Get(v_5, (V)"impact_limit")), () => (V)((V)R.Length(R.Get(v_338, (V)"crumbles")) == R.Get(v_5, (V)"crumble_limit"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_338, (V)"celebrations")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_338, (V)"celebrations"), (V)1.0d), (V)"level") == (V)1000.0d)))));
foreach (var row_356 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_338, (V)"impacts")), new Entry((V)2, R.Get(v_338, (V)"crumbles")), new Entry((V)3, R.Get(v_338, (V)"celebrations"))))))))) {
V v_357 = row_356[0];
V v_358 = row_356[1];
foreach (var row_359 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_358)))))) {
V v_360 = row_359[0];
V v_361 = row_359[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_361, (V)"texture"))), () => (V)(!R.Truth(R.Get(v_361, (V)"quad")))))));
}
}
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_338, (V)0.0d, (V)"a", (V)false, v_355}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_338, (V)"impacts")) == (V)0.0d), () => (V)((V)R.Length(R.Get(v_338, (V)"crumbles")) == (V)0.0d)), () => (V)((V)R.Length(R.Get(v_338, (V)"celebrations")) == (V)0.0d)))));
R.Call(R.Get(v_5, (V)"update"), R.Join(new V[] {v_338, (V)0.0d, (V)"b", (V)true, v_355}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_338, (V)"impacts")) == (V)0.0d), () => (V)((V)R.Length(R.Get(v_338, (V)"crumbles")) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"menu selection settles quickly, presses offset without moving hitboxes, and views reset feedback"}, new Results(new V((RuleFunction)(args_362 => {

R.Call(R.Get(v_13, (V)"begin_frame"), R.Join(new V[] {(V)"juice-test"}, new Results((V)true)));
var values_363 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"option_feedback"), R.Join(new V[] {(V)"NEW RUN", (V)280.0d, (V)274.0d, (V)464.0d, (V)true}, new Results((V)false))));
V v_364 = values_363[0];
V v_365 = values_363[1];
V v_366 = values_363[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_364 == (-(V)1.0d)), () => (V)(v_365 == (V)4.0d)), () => (V)(v_366 == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_13, (V)"home_at"), R.Join(new V[] {(V)500.0d}, new Results((V)280.0d))).First == (V)2.0d))));
R.Call(R.Get(v_13, (V)"press"), Results.Empty);

var values_367 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"option_feedback"), R.Join(new V[] {(V)"NEW RUN", (V)280.0d, (V)274.0d, (V)464.0d, (V)true}, new Results((V)false))));
v_364 = values_367[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_364 == (V)1.0d))));
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {(V)0.2d}, new Results((V)true)));

var values_368 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"option_feedback"), R.Join(new V[] {(V)"NEW RUN", (V)280.0d, (V)274.0d, (V)464.0d, (V)true}, new Results((V)false))));
v_364 = values_368[0];
v_365 = values_368[1];
v_366 = values_368[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_364 == (V)0.0d), () => (V)(v_365 == (V)0.0d)), () => (V)(v_366 == (V)0.0d)))));
R.Call(R.Get(v_13, (V)"begin_frame"), R.Join(new V[] {(V)"different-view"}, new Results((V)true)));
var values_369 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"option_feedback"), R.Join(new V[] {(V)"NEW RUN", (V)280.0d, (V)274.0d, (V)464.0d, (V)true}, new Results((V)true))));
v_364 = values_369[0];
v_365 = values_369[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_364 == (V)0.0d), () => (V)(v_365 == (V)0.0d)))));
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {(V)0.0d}, new Results((V)false)));

var values_370 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"option_feedback"), R.Join(new V[] {(V)"NEW RUN", (V)280.0d, (V)274.0d, (V)464.0d, (V)true}, new Results((V)false))));
v_364 = values_370[0];
v_365 = values_370[1];
v_366 = values_370[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_364 == (V)0.0d), () => (V)(v_365 == (V)0.0d)), () => (V)(v_366 == (V)0.0d)), () => (V)(R.Call(R.Get(v_13, (V)"home_at"), R.Join(new V[] {(V)500.0d}, new Results((V)280.0d))).First == (V)2.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"chest offer arrivals are staggered, settle exactly, and honor effects-off"}, new Results(new V((RuleFunction)(args_371 => {

R.Call(R.Get(v_13, (V)"begin_frame"), R.Join(new V[] {(V)"chest-reveal-test"}, new Results((V)true)));
var values_372 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"offer_pose"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_373 = values_372[0];
V v_374 = values_372[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_373 == (V)6.0d), () => (V)(v_374 == (V)1.0d)))));
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {(V)0.08d}, new Results((V)true)));
var values_375 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"offer_pose"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_376 = values_375[0];

var values_377 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"offer_pose"), R.Join(new V[] {}, new Results((V)3.0d))));
V v_378 = values_377[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_378 > v_376))));
var values_379 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"offer_pose"), R.Join(new V[] {}, new Results((V)1.0d))));
v_373 = values_379[0];
v_374 = values_379[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_374 > (V)1.0d), () => (V)(v_374 <= (V)1.12d)))));
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {(V)0.3d}, new Results((V)true)));
{ double end_382 = R.Num((V)3.0d), step_383 = R.Num((V)1.0d);
for (double i_381 = R.Num((V)1.0d); step_383 > 0 ? i_381 <= end_382 : i_381 >= end_382; i_381 += step_383) {
V v_380 = i_381;
var values_384 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"offer_pose"), R.Join(new V[] {}, new Results(v_380))));
v_373 = values_384[0];
v_374 = values_384[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_373 == (V)0.0d), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_374 - (V)1.0d)))).First < (V)1e-10d)))));
}
}
R.Call(R.Get(v_13, (V)"update"), R.Join(new V[] {(V)0.0d}, new Results((V)false)));

var values_385 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"offer_pose"), R.Join(new V[] {}, new Results((V)1.0d))));
v_373 = values_385[0];
v_374 = values_385[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_373 == (V)0.0d), () => (V)(v_374 == (V)1.0d)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
