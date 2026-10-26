// Static C# port of tests/idea_driver_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: aa0acfd9d6b4079eacecc6fd5dd5d4459cef3863604170dfc5438b42cf0fd582
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_driver_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_7 = values_6[0];
R.Call(v_2, R.Join(new V[] {(V)"combat driver can legally Dash out of a committed box with no safe one-tile escape"}, new Results(new V((RuleFunction)(args_8 => {

var values_9 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_10 = values_9[0];

var values_11 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_10))));
V v_12 = values_11[0];

var values_13 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_14 = R.Get(v_12, (V)"position");
V key_15 = (V)"x";
var obj_16 = R.Get(v_12, (V)"position");
V key_17 = (V)"y";
R.Set(obj_14, key_15, values_13[0]);
R.Set(obj_16, key_17, values_13[1]);
var values_18 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_10, (V)"core:enemy/ember_crucible", (V)10.0d, (V)6.0d}, new Results((V)0.0d))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_21 = R.Get(v_19, (V)"ai_memory");
V key_22 = (V)"attacks";
R.Set(obj_21, key_22, values_20[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_10}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_19, (V)"pending_attack"))));
var values_23 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_10))));
V v_24 = values_23[0];
var values_25 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_10), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table()))));
V v_26 = values_25[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"escape"), R.Join(new V[] {v_26}, new Results(R.Get(v_19, (V)"position")))).First)))));

R.Call(v_3, R.Join(new V[] {v_24}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_10)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"escape"), R.Join(new V[] {v_26, R.Get(v_19, (V)"position"), V.Nil}, new Results((V)true)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(R.Get(R.Get(v_26, (V)"trace"), (V)1.0d), (V)"intent"), (V)"kind") == (V)"dash"), () => (V)(R.Get(R.Get(v_12, (V)"position"), (V)"x") == (V)9.0d)), () => (V)(R.Get(R.Get(v_12, (V)"position"), (V)"y") == (V)6.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_12, (V)"hp") == (V)24.0d), () => (V)(R.Get(v_10, (V)"clock") == (V)230.0d))}, new Results((V)"normal recovery resolves the old box without a hit")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_19, (V)"pending_attack"))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Arcanist can hit an off-axis foe with the real Fire Sigil without chasing a bolt line"}, new Results(new V((RuleFunction)(args_27 => {

var values_28 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/arcanist"))))));
V v_29 = values_28[0];
var values_30 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_29))));
V v_31 = values_30[0];

var values_32 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_33 = R.Get(v_31, (V)"position");
V key_34 = (V)"x";
var obj_35 = R.Get(v_31, (V)"position");
V key_36 = (V)"y";
R.Set(obj_33, key_34, values_32[0]);
R.Set(obj_35, key_36, values_32[1]);
var values_37 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_29, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)7.0d}, new Results((V)100000.0d))));
V v_38 = values_37[0];
var values_39 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_29), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table()))));
V v_40 = values_39[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"attack_in_reach"), R.Join(new V[] {}, new Results(v_40)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(R.Get(v_40, (V)"trace"), (V)1.0d), (V)"intent"), (V)"kind") == (V)"sweeping_slash"), () => (V)(R.Get(v_38, (V)"hp") == (V)8.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_31, (V)"position"), (V)"x") == (V)7.0d), () => (V)(R.Get(R.Get(v_31, (V)"position"), (V)"y") == (V)6.0d)), () => (V)(R.Get(v_29, (V)"clock") == (V)170.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"ranged combat can choose a safe lateral dodge instead of repeating the same axial corridor retreat"}, new Results(new V((RuleFunction)(args_41 => {

V v_42 = V.Nil;
v_42 = new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
var values_45 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_46 = values_45[0];

var values_47 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_46))));
V v_48 = values_47[0];

var values_49 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_50 = R.Get(v_48, (V)"position");
V key_51 = (V)"x";
var obj_52 = R.Get(v_48, (V)"position");
V key_53 = (V)"y";
R.Set(obj_50, key_51, values_49[0]);
R.Set(obj_52, key_53, values_49[1]);
var values_54 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_46, (V)"core:enemy/thornspitter", (V)7.0d, (V)9.0d}, new Results((V)50.0d))));
V v_55 = values_54[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_46}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_55, (V)"pending_attack"))));
var values_56 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_46), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table()))));
V v_57 = values_56[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"escape"), R.Join(new V[] {v_57, R.Get(v_55, (V)"position"), V.Nil, (V)true}, new Results(v_44)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_48, (V)"hp") == (V)24.0d), () => (V)(R.Get(v_46, (V)"clock") == (V)200.0d)))));
return R.Join(new V[] {}, new Results(R.Get(v_48, (V)"position")));
return Results.Empty;
}));
var values_58 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)false))));
V v_59 = values_58[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_59, (V)"x") == (V)7.0d), () => (V)(R.Get(v_59, (V)"y") == (V)5.0d)))));
var values_60 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {}, new Results((V)true))));
V v_61 = values_60[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_61, (V)"x") == (V)8.0d), () => (V)(R.Get(v_61, (V)"y") == (V)6.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an armored travel cycle may ignore harmless hits while a zero-damage Rammer shove still demands escape"}, new Results(new V((RuleFunction)(args_62 => {

var values_63 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_64 = values_63[0];

var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_64))));
V v_66 = values_65[0];

var values_67 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_68 = R.Get(v_66, (V)"position");
V key_69 = (V)"x";
var obj_70 = R.Get(v_66, (V)"position");
V key_71 = (V)"y";
R.Set(obj_68, key_69, values_67[0]);
R.Set(obj_70, key_71, values_67[1]);

var values_72 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_73 = v_66;
V key_74 = (V)"defense";
R.Set(obj_73, key_74, values_72[0]);
var values_75 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_64, (V)"core:enemy/thornspitter", (V)7.0d, (V)9.0d}, new Results((V)50.0d))));
V v_76 = values_75[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_64}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

var values_77 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_64))));
V v_78 = values_77[0];
var values_79 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_64), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table()))));
V v_80 = values_79[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"escape"), R.Join(new V[] {v_80, R.Get(v_76, (V)"position")}, new Results((V)true))).First)))));

R.Call(v_3, R.Join(new V[] {v_78}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_64)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"escape"), R.Join(new V[] {v_80, R.Get(v_76, (V)"position")}, new Results((V)false)))));
var values_81 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
v_64 = values_81[0];

var values_82 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_64))));
v_66 = values_82[0];

var values_83 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_84 = R.Get(v_66, (V)"position");
V key_85 = (V)"x";
var obj_86 = R.Get(v_66, (V)"position");
V key_87 = (V)"y";
R.Set(obj_84, key_85, values_83[0]);
R.Set(obj_86, key_87, values_83[1]);

var values_88 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_89 = v_66;
V key_90 = (V)"defense";
R.Set(obj_89, key_90, values_88[0]);
var values_91 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_64, (V)"core:enemy/heavy_rammer", (V)6.0d, (V)6.0d}, new Results((V)50.0d))));
v_76 = values_91[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_64}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_76, (V)"pending_attack"), (V)"shove"))));
var values_92 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"state", v_64), new Entry((V)"trace", R.Table()), new Entry((V)"events", R.Table()))));
v_80 = values_92[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_7, (V)"escape"), R.Join(new V[] {v_80, R.Get(v_76, (V)"position")}, new Results((V)true))).First, () => (V)((V)R.Length(R.Get(v_80, (V)"trace")) == (V)1.0d)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
