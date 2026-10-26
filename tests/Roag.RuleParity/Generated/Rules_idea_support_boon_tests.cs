// Static C# port of tests/idea_support_boon_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 94c6d8bb8c43693bf99db05b0c979c8af78e13d99e3bea7dfcc2be07ad8eeeb6
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_support_boon_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_11 = values_10[0];

var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_13 = values_12[0];

var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_support_fixture"))));
V v_17 = values_16[0];
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
var values_22 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_20}, new Results(R.Or(v_21, () => R.Table(new Entry((V)"kind", (V)"wait")))))));
V v_23 = values_22[0];
V v_24 = values_22[1];
V v_25 = values_22[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_23}, new Results(v_24)));

return R.Join(new V[] {}, new Results(v_25));
return Results.Empty;
}));
V v_26 = V.Nil;
v_26 = new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
V v_29 = args_27[1];
var values_30 = R.Join(new V[] {}, new Results((V)0.0d));
V v_31 = values_30[0];

foreach (var row_32 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_28)))))) {
V v_33 = row_32[0];
V v_34 = row_32[1];
if (R.Truth(R.And((V)(R.Get(v_34, (V)"kind") == (V)"BoonActivated"), () => (V)(R.Get(v_34, (V)"boon_id") == R.Concat((V)"core:boon/", v_29))))) {
var values_35 = R.Join(new V[] {}, new Results((V)(v_31 + (V)1.0d)));
v_31 = values_35[0];
}
}

return R.Join(new V[] {}, new Results(v_31));
return Results.Empty;
}));
V v_36 = V.Nil;
v_36 = new V((RuleFunction)(args_37 => {
V v_38 = args_37[0];
V v_39 = args_37[1];
V v_40 = args_37[2];
V v_41 = args_37[3];
R.Call(R.Get(v_7, (V)"set_stacks"), R.Join(new V[] {R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_38))).First, R.Concat((V)"core:boon/", v_39), R.Or(v_40, () => (V)"rare")}, new Results(R.Or(v_41, () => (V)1.0d))));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"fifty new boons retain the fifty-three originals and distinct suitable native icons"}, new Results(new V((RuleFunction)(args_42 => {

var values_43 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))));
V v_44 = values_43[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_44, (V)"order")) == (V)50.0d), () => (V)((V)R.Length(R.Get(v_9, (V)"order")) == (V)103.0d)), () => R.Call(R.Get(v_9, (V)"validate"), Results.Empty).First))));
var values_45 = R.Join(new V[] {}, new Results(R.Table()));
V v_46 = values_45[0];

foreach (var row_47 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_44, (V)"order"))))))) {
V v_48 = row_47[0];
V v_49 = row_47[1];
var values_50 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results(v_49))).First, (V)"art")));
V v_51 = values_50[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_46, R.Get(v_51, (V)"index"))))}, new Results(R.Concat(v_49, R.Concat((V)" duplicates ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_46, R.Get(v_51, (V)"index"))))).First)))));

var values_52 = R.Join(new V[] {}, new Results(v_49));
var obj_53 = v_46;
V key_54 = R.Get(v_51, (V)"index");
R.Set(obj_53, key_54, values_52[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))).First, (V)"boons"), v_49), (V)1.0d) == R.Get(v_51, (V)"index")))));
}
var values_55 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/mist_lullaby"))))))));
V v_56 = values_55[0];

var values_57 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_58 = R.Get(R.Get(v_56, (V)"effects"), (V)1.0d);
V key_59 = (V)"cooldown";
R.Set(obj_58, key_59, values_57[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_9, (V)"validate"), R.Table(new Entry(R.Get(v_56, (V)"id"), v_56))}, new Results(R.Table(new Entry((V)1, R.Get(v_56, (V)"id")))))).First)))));
return Results.Empty;
})))));
foreach (var row_60 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_17, (V)"order"))))))) {
V v_61 = row_60[0];
V v_62 = row_60[1];
R.Call(v_2, R.Join(new V[] {R.Concat(v_62, (V)" activates through a real paid action, pure preview and exact saved continuation")}, new Results(new V((RuleFunction)(args_63 => {

var values_64 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results(v_62))));
V v_65 = values_64[0];

var values_66 = R.Join(new V[] {}, new Results(R.Get(v_65, (V)"state")));
V v_67 = values_66[0];

var values_68 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_67))));
V v_69 = values_68[0];
{ double end_72 = R.Num((V)5.0d), step_73 = R.Num((V)1.0d);
for (double i_71 = R.Num((V)1.0d); step_73 > 0 ? i_71 <= end_72 : i_71 >= end_72; i_71 += step_73) {
V v_70 = i_71;
R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_67, R.Get(R.Get(v_65, (V)"intent"), (V)"kind"), R.Get(R.Get(v_65, (V)"intent"), (V)"dx")}, new Results(R.Get(R.Get(v_65, (V)"intent"), (V)"dy"))));
}
}
R.Call(v_3, R.Join(new V[] {v_69}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_67)))));
R.Call(R.Get(v_17, (V)"check"), R.Join(new V[] {v_65}, R.Call(v_18, R.Join(new V[] {v_67}, new Results(R.Get(v_65, (V)"intent"))))));
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_67))))))))));
V v_75 = values_74[0];
{ double end_78 = R.Num((V)4.0d), step_79 = R.Num((V)1.0d);
for (double i_77 = R.Num((V)1.0d); step_79 > 0 ? i_77 <= end_78 : i_77 >= end_78; i_77 += step_79) {
V v_76 = i_77;
R.Call(v_3, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {}, new Results(v_67))).First}, R.Call(v_18, R.Join(new V[] {}, new Results(v_75)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_67))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_75)))));
}
}
return Results.Empty;
})))));
}
R.Call(v_2, R.Join(new V[] {(V)"Doorward cannot be renewed by walking through or retreating through an open door"}, new Results(new V((RuleFunction)(args_80 => {

var values_81 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"doorward_lamp"))));
V v_82 = values_81[0];

R.Call(R.Get(v_17, (V)"check"), R.Join(new V[] {v_82}, R.Call(v_18, R.Join(new V[] {R.Get(v_82, (V)"state")}, new Results(R.Get(v_82, (V)"intent"))))));
foreach (var row_83 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)1.0d)))))))) {
V v_84 = row_83[0];
V v_85 = row_83[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {R.Get(v_82, (V)"state")}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_85), new Entry((V)"dy", (V)0.0d))))).First}, new Results(R.Get(v_82, (V)"slug")))).First == (V)0.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Secret Apple is finite and full-health recovery cannot create overheal"}, new Results(new V((RuleFunction)(args_86 => {

var values_87 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"secret_apple"))));
V v_88 = values_87[0];

var values_89 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_88, (V)"player"), (V)"max_hp")));
var obj_90 = R.Get(v_88, (V)"player");
V key_91 = (V)"hp";
R.Set(obj_90, key_91, values_89[0]);
R.Call(R.Get(v_17, (V)"install"), R.Join(new V[] {}, new Results(v_88)));

var values_92 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {R.Get(v_88, (V)"state")}, new Results(R.Get(v_88, (V)"intent")))));
V v_93 = values_92[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_26, R.Join(new V[] {v_93}, new Results(R.Get(v_88, (V)"slug")))).First == (V)1.0d), () => (V)(R.Get(R.Get(v_88, (V)"player"), (V)"hp") == R.Get(R.Get(v_88, (V)"player"), (V)"max_hp"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {R.Get(v_88, (V)"state")}, new Results(R.Get(v_88, (V)"intent")))).First}, new Results(R.Get(v_88, (V)"slug")))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Spoils Aegis cannot bypass chest affordability or activate on free elite rewards"}, new Results(new V((RuleFunction)(args_94 => {

var values_95 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"spoils_aegis"))));
V v_96 = values_95[0];

var values_97 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_96, (V)"chest"), (V)"price") - (V)1.0d)));
var obj_98 = R.Get(v_96, (V)"state");
V key_99 = (V)"currency";
R.Set(obj_98, key_99, values_97[0]);
var values_100 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_96, (V)"state")))));
V v_101 = values_100[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {R.Get(v_96, (V)"state")}, new Results(R.Get(v_96, (V)"intent")))).First)))));

R.Call(v_3, R.Join(new V[] {v_101}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_96, (V)"state"))))));
var values_102 = R.Join(new V[] {}, new Results((V)"core:chest/free_elite"));
var obj_103 = R.Get(v_96, (V)"chest");
V key_104 = (V)"definition_id";
R.Set(obj_103, key_104, values_102[0]);

var values_105 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_106 = R.Get(v_96, (V)"chest");
V key_107 = (V)"price";
R.Set(obj_106, key_107, values_105[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {R.Get(v_96, (V)"state")}, new Results(R.Get(v_96, (V)"intent")))).First}, new Results(R.Get(v_96, (V)"slug")))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Mist Lullaby has an exact 400-time cooldown and full health cannot start it"}, new Results(new V((RuleFunction)(args_108 => {

var values_109 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"mist_lullaby"))));
V v_110 = values_109[0];

var values_111 = R.Join(new V[] {R.Get(v_110, (V)"state")}, new Results(R.Get(v_110, (V)"player")));
V v_112 = values_111[0];
V v_113 = values_111[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {}, new Results(v_112))).First}, new Results(R.Get(v_110, (V)"slug")))).First == (V)1.0d))));
{ double end_116 = R.Num((V)3.0d), step_117 = R.Num((V)1.0d);
for (double i_115 = R.Num((V)1.0d); step_117 > 0 ? i_115 <= end_116 : i_115 >= end_116; i_115 += step_117) {
V v_114 = i_115;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {}, new Results(v_112))).First}, new Results(R.Get(v_110, (V)"slug")))).First == (V)0.0d))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_112, (V)"clock") == (V)400.0d), () => (V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {}, new Results(v_112))).First}, new Results(R.Get(v_110, (V)"slug")))).First == (V)1.0d)))));
var values_118 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"mist_lullaby"))));
v_110 = values_118[0];

var values_119 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_110, (V)"player"), (V)"max_hp")));
var obj_120 = R.Get(v_110, (V)"player");
V key_121 = (V)"hp";
R.Set(obj_120, key_121, values_119[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_110, (V)"state")))).First}, new Results(R.Get(v_110, (V)"slug")))).First == (V)0.0d), () => (V)(!R.Truth(R.Get(R.Get(v_110, (V)"player"), (V)"boon_memory")))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ordinary ground and waiting on ice cannot masquerade as a Rime departure"}, new Results(new V((RuleFunction)(args_122 => {

var values_123 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"rime_disk"))));
V v_124 = values_123[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_124, (V)"state")))).First}, new Results(R.Get(v_124, (V)"slug")))).First == (V)0.0d))));
var values_125 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_126 = values_125[0];

R.Call(v_36, R.Join(new V[] {v_126}, new Results((V)"rime_disk")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {v_126}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First}, new Results((V)"rime_disk"))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Vent Mantle preserves the strict locked impact and cannot renew while standing still"}, new Results(new V((RuleFunction)(args_127 => {

var values_128 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"vent_mantle"))));
V v_129 = values_128[0];

var values_130 = R.Join(new V[] {}, new Results(R.Get(v_129, (V)"state")));
V v_131 = values_130[0];
var values_132 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_129, (V)"vent"), (V)"due")));
V v_133 = values_132[0];

R.Call(R.Get(v_17, (V)"check"), R.Join(new V[] {v_129}, R.Call(v_18, R.Join(new V[] {v_131}, new Results(R.Get(v_129, (V)"intent"))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_131, (V)"clock") == v_133), () => (V)(R.Get(R.Get(v_129, (V)"vent"), (V)"phase") == (V)"cooldown")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {}, new Results(v_131))).First}, new Results(R.Get(v_129, (V)"slug")))).First == (V)0.0d))));
var values_134 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_135 = values_134[0];

R.Call(v_36, R.Join(new V[] {v_135}, new Results((V)"vent_mantle")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {v_135}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First}, new Results((V)"vent_mantle"))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Patient Rebuke spends a single charge and cannot be charged by Vow cost or an empty Wait"}, new Results(new V((RuleFunction)(args_136 => {

var values_137 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"patient_rebuke"))));
V v_138 = values_137[0];

R.Call(R.Get(v_17, (V)"check"), R.Join(new V[] {v_138}, R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_138, (V)"state"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_138, (V)"state")))).First}, new Results(R.Get(v_138, (V)"slug")))).First == (V)0.0d))));
var values_139 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_140 = values_139[0];

R.Call(v_36, R.Join(new V[] {v_140}, new Results((V)"patient_rebuke")));

R.Call(v_36, R.Join(new V[] {v_140}, new Results((V)"pilgrims_vow")));
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_140, (V)"core:enemy/mossbound_guard", (V)3.0d, (V)2.0d}, new Results((V)100000.0d)));
R.Call(v_18, R.Join(new V[] {v_140}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"vow", (V)true)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {}, new Results(v_140))).First}, new Results((V)"patient_rebuke"))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Beacon Afterglow previews landing rays without showing the ordinary Dash wake"}, new Results(new V((RuleFunction)(args_141 => {

var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"beacon_afterglow"))));
V v_143 = values_142[0];

var values_144 = R.Join(new V[] {}, new Results(R.Get(v_143, (V)"state")));
V v_145 = values_144[0];

R.Call(v_36, R.Join(new V[] {v_145}, new Results((V)"backtrail_mantle")));
var values_146 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_145))));
V v_147 = values_146[0];

var values_148 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_145, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_149 = values_148[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_149, (V)"beacon_return"), () => (V)(R.Get(v_149, (V)"boon_hint") == (V)"beacon landing rays")), () => (V)((V)R.Length(R.Get(v_149, (V)"boon_cells")) > (V)0.0d)))));
foreach (var row_150 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_149, (V)"boon_cells"))))))) {
V v_151 = row_150[0];
V v_152 = row_150[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_152, (V)"boon_id") == (V)"core:boon/beacon_afterglow"), () => R.Or((V)(R.Get(v_152, (V)"x") == (V)6.0d), () => (V)(R.Get(v_152, (V)"y") == (V)6.0d))))));
}
R.Call(v_3, R.Join(new V[] {v_147}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_145)))));

R.Call(R.Get(v_17, (V)"check"), R.Join(new V[] {v_143}, R.Call(v_18, R.Join(new V[] {v_145}, new Results(R.Get(v_143, (V)"intent"))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_145))).First) == (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {v_145}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First}, new Results(R.Get(v_143, (V)"slug")))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Sentry deployment speed affects subsequent costs while autonomous bolts remain isolated"}, new Results(new V((RuleFunction)(args_153 => {

var values_154 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"sentry_quartermaster"))));
V v_155 = values_154[0];

var values_156 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {R.Get(v_155, (V)"state"), (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_157 = values_156[0];
R.Call(R.Get(v_17, (V)"check"), R.Join(new V[] {v_155}, R.Call(v_18, R.Join(new V[] {R.Get(v_155, (V)"state")}, new Results(R.Get(v_155, (V)"intent"))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_155, (V)"state"), (V)"last_action"), (V)"cost") == R.Get(v_157, (V)"cost")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {R.Get(v_155, (V)"state")}, new Results((V)"wait"))).First, (V)"cost") == R.Call(R.Get(v_5, (V)"effective_cost"), R.Join(new V[] {(V)100.0d}, new Results((V)(R.Get(R.Get(v_155, (V)"player"), (V)"speed") + (V)20.0d)))).First))));
R.Call(v_36, R.Join(new V[] {R.Get(v_155, (V)"state"), (V)"storm_conductor", (V)"legendary"}, new Results((V)100.0d)));
var values_158 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {R.Get(v_155, (V)"state"), (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_159 = values_158[0];
{ double end_162 = R.Num((V)4.0d), step_163 = R.Num((V)1.0d);
for (double i_161 = R.Num((V)1.0d); step_163 > 0 ? i_161 <= end_162 : i_161 >= end_162; i_161 += step_163) {
V v_160 = i_161;
var values_164 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_155, (V)"state")))));
V v_165 = values_164[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_26, R.Join(new V[] {v_165}, new Results((V)"storm_conductor"))).First == (V)0.0d), () => (V)(R.Call(v_26, R.Join(new V[] {v_165}, new Results(R.Get(v_155, (V)"slug")))).First == (V)0.0d)))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_159, (V)"hp") < (V)10.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Spirit Shelter is positioning protection and cannot be renewed by a commanded strike"}, new Results(new V((RuleFunction)(args_166 => {

var values_167 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"spirit_shelter"))));
V v_168 = values_167[0];

R.Call(R.Get(v_17, (V)"check"), R.Join(new V[] {v_168}, R.Call(v_18, R.Join(new V[] {R.Get(v_168, (V)"state")}, new Results(R.Get(v_168, (V)"intent"))))));
var values_169 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {R.Get(v_168, (V)"state"), (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_170 = values_169[0];
var values_171 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {R.Get(v_168, (V)"state")}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_172 = values_171[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_170, (V)"hp") < (V)10.0d), () => (V)(R.Call(v_26, R.Join(new V[] {v_172}, new Results(R.Get(v_168, (V)"slug")))).First == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Grounding Rod cannot create charge from an empty Dash and retains both distinct shields"}, new Results(new V((RuleFunction)(args_173 => {

var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"grounding_rod"))));
V v_175 = values_174[0];

R.Call(R.Get(v_17, (V)"check"), R.Join(new V[] {v_175}, R.Call(v_18, R.Join(new V[] {R.Get(v_175, (V)"state")}, new Results(R.Get(v_175, (V)"intent"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_175, (V)"player"), (V)"storm_charge") == (V)0.0d), () => R.Get(R.Get(v_175, (V)"player"), (V)"storm_ward")), () => (V)(R.Call(R.Get(v_7, (V)"ward_total"), R.Join(new V[] {R.Get(v_175, (V)"state")}, new Results(R.Get(v_175, (V)"player")))).First == (V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {R.Get(v_175, (V)"state")}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))).First}, new Results(R.Get(v_175, (V)"slug")))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Mirror Dividend is one heal on a successful reflected player attack and none on blocked routing"}, new Results(new V((RuleFunction)(args_176 => {

var values_177 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"mirror_dividend"))));
V v_178 = values_177[0];

var values_179 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {R.Get(v_178, (V)"state"), (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_180 = values_179[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {R.Get(v_178, (V)"state")}, new Results(R.Get(v_178, (V)"intent")))).First}, new Results(R.Get(v_178, (V)"slug")))).First == (V)0.0d), () => (V)(R.Get(R.Get(v_178, (V)"player"), (V)"hp") == R.Get(v_178, (V)"hp_before"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_180, (V)"hp") < (V)10.0d), () => (V)((V)R.Length(R.Get(R.Get(v_178, (V)"state"), (V)"idea_mirrors")) == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"uncapped mixed rarity copies aggregate once while healing and wards retain their limits"}, new Results(new V((RuleFunction)(args_181 => {

var values_182 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"mist_lullaby"))));
V v_183 = values_182[0];

R.Call(v_36, R.Join(new V[] {R.Get(v_183, (V)"state"), R.Get(v_183, (V)"slug"), (V)"common"}, new Results((V)100.0d)));

R.Call(v_36, R.Join(new V[] {R.Get(v_183, (V)"state"), R.Get(v_183, (V)"slug"), (V)"legendary"}, new Results((V)100.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_183, (V)"state")))).First}, new Results(R.Get(v_183, (V)"slug")))).First == (V)1.0d), () => (V)(R.Get(R.Get(v_183, (V)"player"), (V)"hp") == R.Get(R.Get(v_183, (V)"player"), (V)"max_hp"))))));
var values_184 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"spirit_shelter"))));
V v_185 = values_184[0];

R.Call(v_36, R.Join(new V[] {R.Get(v_185, (V)"state"), R.Get(v_185, (V)"slug"), (V)"common"}, new Results((V)100.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(v_26, R.Join(new V[] {R.Call(v_18, R.Join(new V[] {R.Get(v_185, (V)"state")}, new Results(R.Get(v_185, (V)"intent")))).First}, new Results(R.Get(v_185, (V)"slug")))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"ward_total"), R.Join(new V[] {R.Get(v_185, (V)"state")}, new Results(R.Get(v_185, (V)"player")))).First == (V)204.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"character status shows active boon wards and expires them without spending or renewing resources"}, new Results(new V((RuleFunction)(args_186 => {

var values_187 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"prepare"), R.Join(new V[] {}, new Results((V)"spirit_shelter"))));
V v_188 = values_187[0];

R.Call(v_18, R.Join(new V[] {R.Get(v_188, (V)"state")}, new Results(R.Get(v_188, (V)"intent"))));
var values_189 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_188, (V)"state")))));
V v_190 = values_189[0];

var values_191 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_presentation"))));
V v_192 = values_191[0];
var values_193 = R.Join(new V[] {}, R.Call(R.Get(v_192, (V)"statuses"), R.Join(new V[] {R.Get(v_188, (V)"player")}, new Results(R.Get(R.Get(v_188, (V)"state"), (V)"clock")))));
V v_194 = values_193[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_194, (V)1.0d), (V)"label") == (V)"Ward 4"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_192, (V)"statuses"), R.Join(new V[] {R.Get(v_188, (V)"player")}, new Results((V)151.0d))).First) == (V)0.0d))));
R.Call(v_3, R.Join(new V[] {v_190}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_188, (V)"state"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"last dice roll has separate visible history even when active status slots are full"}, new Results(new V((RuleFunction)(args_195 => {

var values_196 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_presentation"))));
V v_197 = values_196[0];
var values_198 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"definition_id", (V)"core:actor/stormcaller"), new Entry((V)"storm_charge", (V)3.0d), new Entry((V)"pickup_ward_actions", (V)3.0d), new Entry((V)"supply_focus_actions", (V)3.0d), new Entry((V)"statuses", R.Table(new Entry((V)"burning", R.Table(new Entry((V)"remaining", (V)3.0d))))), new Entry((V)"boons", R.Table(new Entry((V)"core:boon/fateful_dice", R.Table(new Entry((V)"common", (V)1.0d))))), new Entry((V)"boon_memory", R.Table(new Entry((V)"core:boon/fateful_dice", R.Table(new Entry((V)"face", (V)7.0d), new Entry((V)"roll_action_id", (V)1.0d))))))));
V v_199 = values_198[0];
var values_200 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_199))));
V v_201 = values_200[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_197, (V)"statuses"), R.Join(new V[] {v_199}, new Results((V)100.0d))).First) == (V)4.0d))));
var values_202 = R.Join(new V[] {}, R.Call(R.Get(v_197, (V)"last_roll"), R.Join(new V[] {}, new Results(v_199))));
V v_203 = values_202[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_203, (V)"id") == (V)"core:ui/fate_7_icon"), () => (V)(R.Get(v_203, (V)"label") == (V)"Last roll: SKULL")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_199))).First == v_201))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
