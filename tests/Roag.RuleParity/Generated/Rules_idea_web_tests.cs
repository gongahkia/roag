// Static C# port of tests/idea_web_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: a1b81c998199668b23e8a62eae84693ceaa831dbf15dd71ef3f743e51f9a6a51
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_web_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_webs"))));
V v_11 = values_10[0];
V v_12 = V.Nil;
v_12 = new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
var values_15 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", v_14))))));
V v_16 = values_15[0];

var values_17 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_16))));
V v_18 = values_17[0];

var values_19 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_20 = R.Get(v_18, (V)"position");
V key_21 = (V)"x";
var obj_22 = R.Get(v_18, (V)"position");
V key_23 = (V)"y";
R.Set(obj_20, key_21, values_19[0]);
R.Set(obj_22, key_23, values_19[1]);
var values_24 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_16, (V)"core:enemy/web_weaver", (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
V v_25 = values_24[0];
return R.Join(new V[] {v_16, v_18}, new Results(v_25));
return Results.Empty;
}));
V v_26 = V.Nil;
v_26 = new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
var values_29 = R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {}, new Results(v_28))));
V v_30 = values_29[0];
V v_31 = values_29[1];
V v_32 = values_29[2];
while (R.Truth((V)(R.Get(v_30, (V)"clock") < (V)250.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_30}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_30, (V)"idea_webs")) == (V)1.0d))));

return R.Join(new V[] {v_30, v_31, v_32}, new Results(R.Get(R.Get(v_30, (V)"idea_webs"), (V)1.0d)));
return Results.Empty;
}));
V v_33 = V.Nil;
v_33 = new V((RuleFunction)(args_34 => {
V v_35 = args_34[0];
V v_36 = args_34[1];
var values_37 = R.Join(new V[] {}, new Results((V)0.0d));
V v_38 = values_37[0];

foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_35)))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
if (R.Truth((V)(R.Get(v_41, (V)"kind") == v_36))) {
var values_42 = R.Join(new V[] {}, new Results((V)(v_38 + (V)1.0d)));
v_38 = values_42[0];
}
}

return R.Join(new V[] {}, new Results(v_38));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"Weaver commits a pure five-cell line and spends a finite cast before its anchors appear"}, new Results(new V((RuleFunction)(args_43 => {

var values_44 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_45 = values_44[0];
V v_46 = values_44[1];
V v_47 = values_44[2];

var values_48 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_45))));
V v_49 = values_48[0];

var values_50 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_45}, new Results(v_47))));
V v_51 = values_50[0];
R.Call(v_3, R.Join(new V[] {v_49}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_45)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_51, (V)"support"), (V)"kind") == (V)"web"), () => (V)((V)R.Length(R.Get(v_51, (V)"cells")) == (V)5.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_45}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_47, (V)"pending_attack"), (V)"due") == (V)206.0d), () => (V)(R.Get(R.Get(v_47, (V)"ai_memory"), (V)"web_casts") == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_45, (V)"idea_webs"))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_45}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_45, (V)"idea_webs"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_45}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

var values_52 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_45, (V)"idea_webs"), (V)1.0d)));
V v_53 = values_52[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Get(v_53, (V)"ax") == (V)5.0d), () => (V)(R.Get(v_53, (V)"bx") == (V)9.0d)), () => (V)(R.Get(v_53, (V)"ay") == (V)6.0d)), () => (V)(R.Get(v_53, (V)"by") == (V)6.0d)), () => (V)(R.Get(v_53, (V)"expires") == (V)606.0d)))));
var values_54 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_55 = R.Get(v_47, (V)"ai_memory");
V key_56 = (V)"web_casts";
R.Set(obj_55, key_56, values_54[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_45}, new Results(v_47))).First, (V)"support")))}, new Results((V)"two attempts must exhaust the casting budget")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"walking or Dashing through several web cells adds one recovery delay without damage or RNG draws"}, new Results(new V((RuleFunction)(args_57 => {

foreach (var row_58 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"move"), new Entry((V)2, (V)450.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)"dash"), new Entry((V)2, (V)480.0d)))))))))) {
V v_59 = row_58[0];
V v_60 = row_58[1];
var values_61 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_62 = values_61[0];
V v_63 = values_61[1];
V v_64 = values_61[2];
V v_65 = values_61[3];

var values_66 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_62, (V)"rng"), (V)"state")));
V v_67 = values_66[0];
var values_68 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_62}, new Results(R.Table(new Entry((V)"kind", R.Get(v_60, (V)1.0d)), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_69 = values_68[0];
V v_70 = values_68[1];
V v_71 = values_68[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_62, (V)"clock") == R.Get(v_60, (V)2.0d)), () => (V)(R.Get(v_63, (V)"hp") == (V)24.0d)), () => (V)((V)R.Length(R.Get(v_62, (V)"idea_webs")) == (V)0.0d)), () => (V)(R.Get(R.Get(v_62, (V)"rng"), (V)"state") == v_67)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(v_33, R.Join(new V[] {v_71}, new Results((V)"WebCrossed"))).First == (V)1.0d), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_62))))).First))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an actual Rammer shove through a web delays the already queued player decision exactly once"}, new Results(new V((RuleFunction)(args_72 => {

var values_73 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_74 = values_73[0];
V v_75 = values_73[1];
V v_76 = values_73[2];
V v_77 = values_73[3];
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_74, (V)"core:enemy/heavy_rammer", (V)6.0d, (V)6.0d}, new Results((V)300.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_74}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_78 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_74}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_79 = values_78[0];
V v_80 = values_78[1];
V v_81 = values_78[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(v_75, (V)"position"), (V)"x") == (V)8.0d), () => (V)(R.Get(R.Get(v_75, (V)"position"), (V)"y") == (V)6.0d)), () => (V)(R.Get(v_75, (V)"hp") == (V)22.0d)), () => (V)(R.Get(v_74, (V)"clock") == (V)550.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_33, R.Join(new V[] {v_81}, new Results((V)"WebCrossed"))).First == (V)1.0d), () => (V)((V)R.Length(R.Get(v_74, (V)"idea_webs")) == (V)0.0d)), () => (V)(!R.Truth(R.Get(v_75, (V)"snare_delay")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_74}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d))))))));

R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_74, (V)"clock") == (V)650.0d)}, new Results((V)"forced web delay charged twice")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_74)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a real bow shot cuts an anchor but cannot cut through an enemy killed by that same shot"}, new Results(new V((RuleFunction)(args_82 => {

var values_83 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {}, new Results((V)"core:actor/ranger"))));
V v_84 = values_83[0];
V v_85 = values_83[1];
V v_86 = values_83[2];
V v_87 = values_83[3];
var values_88 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_89 = values_88[0];
V v_90 = values_88[1];
V v_91 = values_88[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(v_33, R.Join(new V[] {v_91}, new Results((V)"WebAnchorCut"))).First == (V)1.0d), () => (V)((V)R.Length(R.Get(v_84, (V)"idea_webs")) == (V)0.0d)), () => (V)(R.Get(v_86, (V)"hp") == (V)4.0d)))));
var values_92 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {}, new Results((V)"core:actor/ranger"))));
v_84 = values_92[0];
v_85 = values_92[1];
v_86 = values_92[2];
v_87 = values_92[3];
var values_93 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_84, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_94 = values_93[0];

var values_95 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_96 = v_94;
V key_97 = (V)"hp";
R.Set(obj_96, key_97, values_95[0]);
var values_98 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_99 = values_98[0];
V v_100 = values_98[1];
V v_101 = values_98[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_84}, new Results(R.Get(v_94, (V)"id")))).First)), () => (V)(R.Call(v_33, R.Join(new V[] {v_101}, new Results((V)"WebAnchorCut"))).First == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(v_84, (V)"idea_webs")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_84, (V)"idea_webs"), (V)1.0d), (V)"id") == R.Get(v_87, (V)"id"))), () => (V)(R.Get(v_86, (V)"hp") == (V)7.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"occupying a locked anchor blocks placement without moving the endpoints or refunding the attempt"}, new Results(new V((RuleFunction)(args_102 => {

var values_103 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_104 = values_103[0];
V v_105 = values_103[1];
V v_106 = values_103[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_104}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_104}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_107 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_104}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_108 = values_107[0];
V v_109 = values_107[1];
V v_110 = values_107[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_105, (V)"position"), (V)"x") == (V)9.0d), () => (V)((V)R.Length(R.Or(R.Get(v_104, (V)"idea_webs"), () => R.Table())) == (V)0.0d)), () => (V)(R.Call(v_33, R.Join(new V[] {v_110}, new Results((V)"WebBlocked"))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_106, (V)"ai_memory"), (V)"web_casts") == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stun cancels a pending weave and killing a caster clears placed lines before a valid save"}, new Results(new V((RuleFunction)(args_111 => {

var values_112 = R.Join(new V[] {}, R.Call(v_12, Results.Empty));
V v_113 = values_112[0];
V v_114 = values_112[1];
V v_115 = values_112[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_113}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"stun"), R.Join(new V[] {v_113, R.Get(v_115, (V)"id")}, new Results((V)500.0d)))));
{ double end_118 = R.Num((V)3.0d), step_119 = R.Num((V)1.0d);
for (double i_117 = R.Num((V)1.0d); step_119 > 0 ? i_117 <= end_118 : i_117 >= end_118; i_117 += step_119) {
V v_116 = i_117;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_113}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_115, (V)"ai_memory"), (V)"web_casts") == (V)1.0d), () => (V)((V)R.Length(R.Or(R.Get(v_113, (V)"idea_webs"), () => R.Table())) == (V)0.0d)))));
var values_120 = R.Join(new V[] {}, R.Call(v_26, R.Join(new V[] {}, new Results((V)"core:actor/ranger"))));
v_113 = values_120[0];
v_114 = values_120[1];
v_115 = values_120[2];

var values_121 = R.Join(new V[] {}, new Results((V)3.0d));
var obj_122 = v_115;
V key_123 = (V)"hp";
R.Set(obj_122, key_123, values_121[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_113}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_113}, new Results(R.Get(v_115, (V)"id")))).First)), () => (V)((V)R.Length(R.Get(v_113, (V)"idea_webs")) == (V)0.0d)), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_113))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"web expiry and restored continuation use world time while idle preview cannot expire or alter it"}, new Results(new V((RuleFunction)(args_124 => {

var values_125 = R.Join(new V[] {}, R.Call(v_26, Results.Empty));
V v_126 = values_125[0];
V v_127 = values_125[1];
V v_128 = values_125[2];
V v_129 = values_125[3];

var values_130 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_126))));
V v_131 = values_130[0];
{ double end_134 = R.Num((V)30.0d), step_135 = R.Num((V)1.0d);
for (double i_133 = R.Num((V)1.0d); step_135 > 0 ? i_133 <= end_134 : i_133 >= end_134; i_133 += step_135) {
V v_132 = i_133;
R.Call(R.Get(v_9, (V)"choose"), R.Join(new V[] {v_126}, new Results(v_128)));

R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_126, (V)"dash", (V)1.0d}, new Results((V)0.0d)));
}
}
R.Call(v_3, R.Join(new V[] {v_131}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_126)))));
var values_136 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_131))))))));
V v_137 = values_136[0];
while (R.Truth((V)(R.Get(v_126, (V)"clock") < (V)700.0d))) {
var values_138 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_126}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_139 = values_138[0];
V v_140 = values_138[1];
V v_141 = values_138[2];

var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_137}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_143 = values_142[0];
V v_144 = values_142[1];
V v_145 = values_142[2];
R.Call(v_3, R.Join(new V[] {v_141}, new Results(v_145)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_126))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_137)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_126, (V)"idea_webs")) == (V)0.0d), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_126))))).First))));
foreach (var row_161 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, new V((RuleFunction)(args_146 => {
V v_147 = args_146[0];
var values_148 = R.Join(new V[] {}, new Results((V)(R.Get(v_147, (V)"ax") + (V)1.0d)));
var obj_149 = v_147;
V key_150 = (V)"ax";
R.Set(obj_149, key_150, values_148[0]);
return Results.Empty;
}))), new Entry((V)2, new V((RuleFunction)(args_151 => {
V v_152 = args_151[0];
var values_153 = R.Join(new V[] {}, new Results((V)300.0d));
var obj_154 = v_152;
V key_155 = (V)"expires";
R.Set(obj_154, key_155, values_153[0]);
return Results.Empty;
}))), new Entry((V)3, new V((RuleFunction)(args_156 => {
V v_157 = args_156[0];
var values_158 = R.Join(new V[] {}, new Results((V)999999.0d));
var obj_159 = v_157;
V key_160 = (V)"source_id";
R.Set(obj_159, key_160, values_158[0]);
return Results.Empty;
})))))))))) {
V v_162 = row_161[0];
V v_163 = row_161[1];
var values_164 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"encode"), R.Join(new V[] {}, new Results(v_131))))));
V v_165 = values_164[0];

R.Call(v_163, R.Join(new V[] {}, new Results(R.Get(R.Get(v_165, (V)"idea_webs"), (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_165))).First)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"frozen-floor web lifetime rebases by time away and ignores records belonging to another floor"}, new Results(new V((RuleFunction)(args_166 => {

var values_167 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"clock", (V)800.0d), new Entry((V)"entities", R.Table()), new Entry((V)"schedule", R.Table()), new Entry((V)"floor_pause", R.Table(new Entry((V)"a", R.Table(new Entry((V)"suspended_at", (V)300.0d), new Entry((V)"schedule", R.Table()))))), new Entry((V)"idea_webs", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", (V)"a"), new Entry((V)"expires", (V)606.0d))), new Entry((V)2, R.Table(new Entry((V)"floor_id", (V)"b"), new Entry((V)"expires", (V)900.0d))))))));
V v_168 = values_167[0];
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))).First, (V)"resume"), R.Join(new V[] {v_168}, new Results((V)"a")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_168, (V)"idea_webs"), (V)1.0d), (V)"expires") == (V)1106.0d), () => (V)((V)(R.Get(R.Get(R.Get(v_168, (V)"idea_webs"), (V)1.0d), (V)"expires") - R.Get(v_168, (V)"clock")) == (V)306.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_168, (V)"idea_webs"), (V)2.0d), (V)"expires") == (V)900.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"natural Weavers use approved native crawler 366, never the rejected mesh or bodies"}, new Results(new V((RuleFunction)(args_169 => {

var values_170 = R.Join(new V[] {}, new Results((V)false));
V v_171 = values_170[0];
{ double end_174 = R.Num((V)100.0d), step_175 = R.Num((V)1.0d);
for (double i_173 = R.Num((V)1.0d); step_175 > 0 ? i_173 <= end_174 : i_173 >= end_174; i_173 += step_175) {
V v_172 = i_173;
var values_176 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_172}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_177 = values_176[0];
foreach (var row_178 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_177, (V)"entities"))))))) {
V v_179 = row_178[0];
V v_180 = row_178[1];
if (R.Truth((V)(R.Get(v_180, (V)"definition_id") == (V)"core:enemy/web_weaver"))) {
var values_181 = R.Join(new V[] {}, new Results((V)true));
v_171 = values_181[0];
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_171)));
var values_182 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_183 = values_182[0];
foreach (var row_184 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_185 = row_184[0];
V v_186 = row_184[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_183, (V)"packs"), v_186), (V)"missing")) == (V)0.0d))));
var values_187 = R.Join(new V[] {}, new Results((V)false));
V v_188 = values_187[0];
foreach (var row_189 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_183, (V)"packs"), v_186), (V)"bindings"))))))) {
V v_190 = row_189[0];
V v_191 = row_189[1];
if (R.Truth(R.Invoke(R.Get(v_191, (V)"id"), "match", R.Join(new V[] {}, new Results((V)"^core:enemy/web_weaver"))).First)) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_191, (V)"index") == (V)366.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_191, (V)"index") != (V)414.0d), () => (V)(R.Get(v_191, (V)"index") != (V)463.0d)), () => (V)(R.Get(v_191, (V)"index") != (V)60.0d)))));

var values_192 = R.Join(new V[] {}, new Results((V)true));
v_188 = values_192[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_188)));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
