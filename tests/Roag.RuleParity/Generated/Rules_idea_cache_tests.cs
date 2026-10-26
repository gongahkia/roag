// Static C# port of tests/idea_cache_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 7bf4b06dfef0f8f219164b6c6000e92dade8e1f8b01d3c53580b079ad4a2eb43
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_cache_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_9 = values_8[0];
V v_10 = V.Nil;
v_10 = new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
var values_13 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", v_12))))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_14))));
V v_16 = values_15[0];

var values_17 = R.Join(new V[] {(V)8.0d}, new Results((V)6.0d));
var obj_18 = R.Get(v_16, (V)"position");
V key_19 = (V)"x";
var obj_20 = R.Get(v_16, (V)"position");
V key_21 = (V)"y";
R.Set(obj_18, key_19, values_17[0]);
R.Set(obj_20, key_21, values_17[1]);
var values_22 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_14, (V)"core:enemy/haunted_cache", (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
V v_23 = values_22[0];
return R.Join(new V[] {v_14, v_16}, new Results(v_23));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"a suspicious cache stays still until approached or injured and its AI is pure"}, new Results(new V((RuleFunction)(args_24 => {

var values_25 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_26 = values_25[0];
V v_27 = values_25[1];
V v_28 = values_25[2];

var values_29 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_26))));
V v_30 = values_29[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_7, (V)"choose"), R.Join(new V[] {v_26}, new Results(v_28))).First, (V)"kind") == (V)"wait"))));

R.Call(v_3, R.Join(new V[] {v_30}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_26)))));
{ double end_33 = R.Num((V)4.0d), step_34 = R.Num((V)1.0d);
for (double i_32 = R.Num((V)1.0d); step_34 > 0 ? i_32 <= end_33 : i_32 >= end_33; i_32 += step_34) {
V v_31 = i_32;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_26}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(!R.Truth(R.Get(R.Get(v_28, (V)"ai_memory"), (V)"alerted"))), () => (V)(!R.Truth(R.Get(v_28, (V)"pending_attack")))), () => (V)(R.Get(R.Get(v_28, (V)"position"), (V)"x") == (V)10.0d)), () => (V)(R.Get(v_27, (V)"hp") == (V)24.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"approaching a cache announces a locked bite instead of dealing an instant ambush hit"}, new Results(new V((RuleFunction)(args_35 => {

var values_36 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_37 = values_36[0];
V v_38 = values_36[1];
V v_39 = values_36[2];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_37}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
var values_40 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_37}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_41 = values_40[0];
V v_42 = values_40[1];
V v_43 = values_40[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_38, (V)"hp") == (V)24.0d), () => R.Get(R.Get(v_39, (V)"ai_memory"), (V)"alerted")), () => (V)(R.Get(R.Get(v_39, (V)"pending_attack"), (V)"due") == (V)370.0d)))));
var values_44 = R.Join(new V[] {}, new Results((V)0.0d));
V v_45 = values_44[0];
foreach (var row_46 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_43)))))) {
V v_47 = row_46[0];
V v_48 = row_46[1];
if (R.Truth((V)(R.Get(v_48, (V)"kind") == (V)"WindupStarted"))) {
var values_49 = R.Join(new V[] {}, new Results((V)(v_45 + (V)1.0d)));
v_45 = values_49[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_45 == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(R.Get(v_39, (V)"pending_attack"), (V)"target_cells"), (V)1.0d), (V)"x") == (V)9.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_37}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_37}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_38, (V)"hp") == (V)24.0d)}, new Results((V)"bite must not follow the moving player")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a real ranged hit wakes the cache but does not grant it a free strike"}, new Results(new V((RuleFunction)(args_50 => {

var values_51 = R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {}, new Results((V)"core:actor/ranger"))));
V v_52 = values_51[0];
V v_53 = values_51[1];
V v_54 = values_51[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_52}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_54, (V)"hp") == (V)5.0d), () => R.Get(R.Get(v_54, (V)"ai_memory"), (V)"alerted")), () => (V)(R.Get(R.Get(v_54, (V)"position"), (V)"x") == (V)9.0d)), () => (V)(R.Get(v_53, (V)"hp") == (V)20.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_54, (V)"pending_attack")))}, new Results((V)"the wake movement is an ordinary scheduled action")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"killing a cache gives one normal enemy reward and never opens a boon-chest menu"}, new Results(new V((RuleFunction)(args_55 => {

var values_56 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_57 = values_56[0];
V v_58 = values_56[1];
V v_59 = values_56[2];

var values_60 = R.Join(new V[] {}, new Results((V)9.0d));
var obj_61 = R.Get(v_58, (V)"position");
V key_62 = (V)"x";
R.Set(obj_61, key_62, values_60[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_57}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_58, (V)"hp") == (V)24.0d), () => R.Get(v_59, (V)"pending_attack")))));
var values_63 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_57}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_64 = values_63[0];
V v_65 = values_63[1];
V v_66 = values_63[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_57}, new Results(R.Get(v_59, (V)"id")))).First)), () => (V)(R.Get(R.Get(v_57, (V)"progression"), (V)"xp_total") == (V)4.0d)), () => (V)((V)R.Length(R.Get(v_57, (V)"drops")) == (V)1.0d)))));
foreach (var row_67 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_66)))))) {
V v_68 = row_67[0];
V v_69 = row_67[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_69, (V)"kind") != (V)"ChestOpened"), () => (V)(R.Get(v_69, (V)"kind") != (V)"BoonGranted")))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_57}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_57, (V)"currency") == (V)3.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"dormant and awakened cache saves preserve exact subsequent events and reject malformed wake state"}, new Results(new V((RuleFunction)(args_70 => {

var values_71 = R.Join(new V[] {}, R.Call(v_10, Results.Empty));
V v_72 = values_71[0];
V v_73 = values_71[1];
V v_74 = values_71[2];
foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)4, R.Table(new Entry((V)"kind", (V)"wait")))))))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
var values_78 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_72))))))))));
V v_79 = values_78[0];
var values_80 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_72}, new Results(v_77))));
V v_81 = values_80[0];
V v_82 = values_80[1];
V v_83 = values_80[2];

var values_84 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_79}, new Results(v_77))));
V v_85 = values_84[0];
V v_86 = values_84[1];
V v_87 = values_84[2];
R.Call(v_3, R.Join(new V[] {v_83}, new Results(v_87)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_72))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_79)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_73, (V)"hp") == (V)22.0d)}, new Results((V)"ordinary committed bite should resolve")));
var values_88 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_72))));
V v_89 = values_88[0];
foreach (var row_90 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_89, (V)"entities"))))))) {
V v_91 = row_90[0];
V v_92 = row_90[1];
if (R.Truth((V)(R.Get(v_92, (V)"id") == R.Get(v_74, (V)"id")))) {
var values_93 = R.Join(new V[] {}, new Results((V)"bad"));
var obj_94 = R.Get(v_92, (V)"ai_memory");
V key_95 = (V)"alerted";
R.Set(obj_94, key_95, values_93[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_89))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"generated caches use reviewed dormant and revealed art and restore without repopulation"}, new Results(new V((RuleFunction)(args_96 => {

var values_97 = R.Join(new V[] {}, new Results((V)false));
V v_98 = values_97[0];
{ double end_101 = R.Num((V)40.0d), step_102 = R.Num((V)1.0d);
for (double i_100 = R.Num((V)1.0d); step_102 > 0 ? i_100 <= end_101 : i_100 >= end_101; i_100 += step_102) {
V v_99 = i_100;
var values_103 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {v_99}, new Results(R.Table(new Entry((V)"multifloor", (V)true))))));
V v_104 = values_103[0];
foreach (var row_105 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_104, (V)"entities"))))))) {
V v_106 = row_105[0];
V v_107 = row_105[1];
if (R.Truth((V)(R.Get(v_107, (V)"definition_id") == (V)"core:enemy/haunted_cache"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(v_107, (V)"ai_memory"), (V)"alerted"))))));

var values_108 = R.Join(new V[] {}, new Results((V)true));
v_98 = values_108[0];
}
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_104))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_104)))))))))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_98)));
var values_109 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty));
V v_110 = values_109[0];
foreach (var row_111 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"onebit")))))))) {
V v_112 = row_111[0];
V v_113 = row_111[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_110, (V)"packs"), v_113), (V)"missing")) == (V)0.0d))));
var values_114 = R.Join(new V[] {}, new Results((V)0.0d));
V v_115 = values_114[0];
foreach (var row_116 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_110, (V)"packs"), v_113), (V)"bindings"))))))) {
V v_117 = row_116[0];
V v_118 = row_116[1];
if (R.Truth((V)(R.Get(v_118, (V)"id") == (V)"core:enemy/haunted_cache"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_118, (V)"index") == (V)417.0d))));

var values_119 = R.Join(new V[] {}, new Results((V)(v_115 + (V)1.0d)));
v_115 = values_119[0];
} else if (R.Truth((V)(R.Get(v_118, (V)"id") == (V)"core:object/idea_cache_dormant"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_118, (V)"index") == (V)303.0d))));

var values_120 = R.Join(new V[] {}, new Results((V)(v_115 + (V)1.0d)));
v_115 = values_120[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_115 == (V)2.0d))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
