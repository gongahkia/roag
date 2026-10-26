// Static C# port of tests/idea_class_integration_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 6b4271a6a35ee8bc3501bed1963a4705aefa16191795a7c554988c4bc54858a3
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_class_integration_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.floors"))));
V v_9 = values_8[0];

var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"geomancer"), new Entry((V)2, (V)"spiritbinder"), new Entry((V)3, (V)"tinkerer"), new Entry((V)4, (V)"stormcaller"), new Entry((V)5, (V)"wayfinder"))));
V v_13 = values_12[0];
R.Call(v_2, R.Join(new V[] {(V)"every new class has readable pure status and contextual placement or command instructions"}, new Results(new V((RuleFunction)(args_14 => {

foreach (var row_15 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_13)))))) {
V v_16 = row_15[0];
V v_17 = row_15[1];
var values_18 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", R.Concat((V)"core:actor/", v_17)))))));
V v_19 = values_18[0];

var values_20 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_19))));
V v_21 = values_20[0];

var values_22 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_19))));
V v_23 = values_22[0];
var values_24 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"status"), R.Join(new V[] {v_19}, new Results(v_21))));
V v_25 = values_24[0];
V v_26 = values_24[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_25))).First == (V)"string"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_26))).First == (V)"string")), () => (V)((V)R.Length(v_25) > (V)0.0d)))));
var values_27 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"preview_hint"), R.Join(new V[] {v_21, (V)"sweeping_slash"}, new Results(R.Table(new Entry((V)"companion_mode", (V)"place"))))));
V v_28 = values_27[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_28 != (V)"Enter to strike \u00c2\u00b7 Esc cancel"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_7, (V)"for_player"), R.Join(new V[] {}, new Results(v_21))).First, (V)"weapon_icon"))));

R.Call(v_3, R.Join(new V[] {v_23}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_19)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"clickable class page controls reach every class and stay inside the actual page bounds"}, new Results(new V((RuleFunction)(args_29 => {

var values_30 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.pixel_ui"))));
V v_31 = values_30[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_31, (V)"class_nav_at"), R.Join(new V[] {(V)200.0d}, new Results((V)510.0d))).First == (-(V)1.0d)), () => (V)(R.Call(R.Get(v_31, (V)"class_nav_at"), R.Join(new V[] {(V)360.0d}, new Results((V)510.0d))).First == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_31, (V)"class_nav_at"), R.Join(new V[] {(V)330.0d}, new Results((V)510.0d))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_31, (V)"class_nav_at"), R.Join(new V[] {(V)360.0d}, new Results((V)540.0d))).First))))));
var values_32 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_7, (V)"order"), (V)1.0d)));
V v_33 = values_32[0];

var values_34 = R.Join(new V[] {}, R.Call(R.Get(v_31, (V)"class_page_target"), R.Join(new V[] {R.Get(v_7, (V)"order"), v_33}, new Results((V)1.0d))));
V v_35 = values_34[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_35 == R.Get(R.Get(v_7, (V)"order"), (V)10.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_31, (V)"class_page_target"), R.Join(new V[] {R.Get(v_7, (V)"order"), v_35}, new Results((-(V)1.0d)))).First == v_33), () => (V)(R.Call(R.Get(v_31, (V)"class_page_target"), R.Join(new V[] {R.Get(v_7, (V)"order"), v_35}, new Results((V)1.0d))).First == v_35)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_31, (V)"class_page_target"), R.Join(new V[] {R.Get(v_7, (V)"order"), R.Get(R.Get(v_7, (V)"order"), (V)5.0d)}, new Results((-(V)1.0d)))).First == R.Get(R.Get(v_7, (V)"order"), (V)5.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_31, (V)"class_page_target"), R.Join(new V[] {R.Get(v_7, (V)"order"), R.Get(R.Get(v_7, (V)"order"), (V)R.Length(R.Get(v_7, (V)"order")))}, new Results((V)1.0d))).First == R.Get(R.Get(v_7, (V)"order"), (V)R.Length(R.Get(v_7, (V)"order")))))));
foreach (var row_36 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"order"))))))) {
V v_37 = row_36[0];
V v_38 = row_36[1];
var values_39 = R.Join(new V[] {}, R.Call(R.Get(v_31, (V)"class_page"), R.Join(new V[] {R.Get(v_7, (V)"order")}, new Results(v_38))));
V v_40 = values_39[0];
V v_41 = values_39[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)(v_41 - v_40) <= (V)8.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"sentry commitment freezes with its floor and resumes at its rebased locked impact"}, new Results(new V((RuleFunction)(args_42 => {

var values_43 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/tinkerer"))))));
V v_44 = values_43[0];

var values_45 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_44))));
V v_46 = values_45[0];

var values_47 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_48 = R.Get(v_46, (V)"position");
V key_49 = (V)"x";
var obj_50 = R.Get(v_46, (V)"position");
V key_51 = (V)"y";
R.Set(obj_48, key_49, values_47[0]);
R.Set(obj_50, key_51, values_47[1]);
var values_52 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_44, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d))));
V v_53 = values_52[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_44}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

var values_54 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))).First, (V)"owned"), R.Join(new V[] {v_44}, new Results((V)"sentry"))));
V v_55 = values_54[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_44, (V)"clock") == (V)168.0d), () => (V)(R.Get(v_55, (V)"sentry_due") == (V)180.0d)))));

R.Call(R.Get(v_9, (V)"park"), R.Join(new V[] {v_44}, new Results(R.Get(v_44, (V)"active_floor_id"))));

var values_56 = R.Join(new V[] {}, new Results((V)(R.Get(v_44, (V)"clock") + (V)1000.0d)));
var obj_57 = v_44;
V key_58 = (V)"clock";
R.Set(obj_57, key_58, values_56[0]);

R.Call(R.Get(v_9, (V)"resume"), R.Join(new V[] {v_44}, new Results(R.Get(v_44, (V)"active_floor_id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_55, (V)"sentry_due") == (V)1180.0d))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_44)))))));

var values_59 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_44}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_60 = values_59[0];
V v_61 = values_59[1];
V v_62 = values_59[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_53, (V)"hp") == (V)8.0d))));

var values_63 = R.Join(new V[] {}, new Results((V)false));
V v_64 = values_63[0];

foreach (var row_65 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_62)))))) {
V v_66 = row_65[0];
V v_67 = row_65[1];
if (R.Truth((V)(R.Get(v_67, (V)"kind") == (V)"DamageTaken"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_67, (V)"time") == (V)1180.0d))));

var values_68 = R.Join(new V[] {}, new Results((V)true));
v_64 = values_68[0];
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_64)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pending storm crosses freeze with their floor and reject a forged non-cross shape"}, new Results(new V((RuleFunction)(args_69 => {

var values_70 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/stormcaller"))))));
V v_71 = values_70[0];

var values_72 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_71))));
V v_73 = values_72[0];

var values_74 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_75 = R.Get(v_73, (V)"position");
V key_76 = (V)"x";
var obj_77 = R.Get(v_73, (V)"position");
V key_78 = (V)"y";
R.Set(obj_75, key_76, values_74[0]);
R.Set(obj_77, key_78, values_74[1]);
R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_71, (V)"core:enemy/mossbound_guard", (V)10.0d, (V)6.0d}, new Results((V)100000.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_71}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

var values_79 = R.Join(new V[] {}, new Results((V)300.0d));
var obj_80 = v_73;
V key_81 = (V)"speed";
R.Set(obj_80, key_81, values_79[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_71}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));

var values_82 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_71, (V)"class_special"), (V)"storm")))));
V v_83 = values_82[0];
var values_84 = R.Join(new V[] {}, new Results(R.Get(v_83, (V)"due")));
V v_85 = values_84[0];

R.Call(R.Get(v_9, (V)"park"), R.Join(new V[] {v_71}, new Results(R.Get(v_71, (V)"active_floor_id"))));

var values_86 = R.Join(new V[] {}, new Results((V)(R.Get(v_71, (V)"clock") + (V)1000.0d)));
var obj_87 = v_71;
V key_88 = (V)"clock";
R.Set(obj_87, key_88, values_86[0]);

R.Call(R.Get(v_9, (V)"resume"), R.Join(new V[] {v_71}, new Results(R.Get(v_71, (V)"active_floor_id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_83, (V)"due") == (V)(v_85 + (V)1000.0d)))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_71)))))));
var values_89 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_71))));
V v_90 = values_89[0];

var values_91 = R.Join(new V[] {}, new Results((V)2.0d));
var obj_92 = R.Get(R.Get(R.Get(R.Get(v_90, (V)"class_special"), (V)"storm"), (V)"cells"), (V)1.0d);
V key_93 = (V)"x";
R.Set(obj_92, key_93, values_91[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_90))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"grounded ward absorbs an actual Burning tick once and preserves the remaining status clock"}, new Results(new V((RuleFunction)(args_94 => {

var values_95 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/stormcaller"))))));
V v_96 = values_95[0];

var values_97 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_96))));
V v_98 = values_97[0];

var values_99 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_100 = R.Get(v_98, (V)"position");
V key_101 = (V)"x";
var obj_102 = R.Get(v_98, (V)"position");
V key_103 = (V)"y";
R.Set(obj_100, key_101, values_99[0]);
R.Set(obj_102, key_103, values_99[1]);
var values_104 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_96, (V)"core:enemy/mossbound_guard", (V)8.0d, (V)6.0d}, new Results((V)0.0d))));
V v_105 = values_104[0];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_96, R.Get(v_105, (V)"id"), (V)"core:boon/emberbrand", (V)"common"}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_96}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_96}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_98, (V)"statuses"), (V)"burning"))));
var values_106 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_107 = v_98;
V key_108 = (V)"storm_charge";
R.Set(obj_107, key_108, values_106[0]);

var values_109 = R.Join(new V[] {}, new Results(R.Get(v_98, (V)"hp")));
V v_110 = values_109[0];
var values_111 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_96}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (V)1.0d))))));
V v_112 = values_111[0];
V v_113 = values_111[1];
V v_114 = values_111[2];
var values_115 = R.Join(new V[] {}, new Results((V)false));
V v_116 = values_115[0];

foreach (var row_117 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_114)))))) {
V v_118 = row_117[0];
V v_119 = row_117[1];
if (R.Truth(R.And((V)(R.Get(v_119, (V)"kind") == (V)"DamageBlocked"), () => (V)(R.Get(v_119, (V)"damage_type") == (V)"fire")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_119, (V)"block_reason") == (V)"grounded ward"))));

var values_120 = R.Join(new V[] {}, new Results((V)true));
v_116 = values_120[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And(v_116, () => (V)(!R.Truth(R.Get(v_98, (V)"storm_ward")))), () => (V)(R.Get(v_98, (V)"hp") == (V)(v_110 - (V)2.0d))), () => (V)(R.Get(R.Get(R.Get(v_98, (V)"statuses"), (V)"burning"), (V)"remaining") == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_98, (V)"statuses"), (V)"burning"), (V)"next_due") == (V)414.0d)), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_96))))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"sentry bolts consume directional reflectors at commitment and resume their fixed turned path"}, new Results(new V((RuleFunction)(args_121 => {

var values_122 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", (V)"core:actor/tinkerer"))))));
V v_123 = values_122[0];

var values_124 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_123))));
V v_125 = values_124[0];

var values_126 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_127 = R.Get(v_125, (V)"position");
V key_128 = (V)"x";
var obj_129 = R.Get(v_125, (V)"position");
V key_130 = (V)"y";
R.Set(obj_127, key_128, values_126[0]);
R.Set(obj_129, key_130, values_126[1]);
var values_131 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_123, (V)"core:enemy/mirror_hexer", (V)10.0d, (V)6.0d}, new Results((V)50.0d))));
V v_132 = values_131[0];
{ double end_135 = R.Num((V)3.0d), step_136 = R.Num((V)1.0d);
for (double i_134 = R.Num((V)1.0d); step_136 > 0 ? i_134 <= end_135 : i_134 >= end_135; i_134 += step_136) {
V v_133 = i_134;
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_123}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_123, (V)"idea_mirrors"), () => (V)((V)R.Length(R.Get(v_123, (V)"idea_mirrors")) == (V)1.0d)))));
var values_137 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_138 = values_137[0];

R.Call(R.Get(v_138, (V)"remove_for_actor"), R.Join(new V[] {v_123}, new Results(R.Get(v_132, (V)"id"))));
R.Call(R.Get(v_138, (V)"enqueue"), R.Join(new V[] {v_123}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)100000.0d), new Entry((V)"actor_id", R.Get(v_132, (V)"id")), new Entry((V)"is_player", (V)false)))));
var values_139 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_123, (V)"core:enemy/mossbound_guard", (V)9.0d, (V)7.0d}, new Results((V)100000.0d))));
V v_140 = values_139[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_123}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))))));
var values_141 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))).First, (V)"owned"), R.Join(new V[] {v_123}, new Results((V)"sentry"))));
V v_142 = values_141[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(v_142, (V)"sentry_reflection"), () => (V)((V)R.Length(R.Get(v_123, (V)"idea_mirrors")) == (V)0.0d)), () => (V)(R.Get(R.Get(R.Get(v_142, (V)"sentry_cells"), (V)2.0d), (V)"x") == (V)9.0d)), () => (V)(R.Get(R.Get(R.Get(v_142, (V)"sentry_cells"), (V)2.0d), (V)"y") == (V)7.0d)))));
var values_143 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_123))));
V v_144 = values_143[0];

var values_145 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"encode"), R.Join(new V[] {}, new Results(v_144))))))));
V v_146 = values_145[0];
var values_147 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_123}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_148 = values_147[0];
V v_149 = values_147[1];
V v_150 = values_147[2];

var values_151 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_146}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_152 = values_151[0];
V v_153 = values_151[1];
V v_154 = values_151[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_140, (V)"hp") == (V)8.0d), () => (V)(R.Get(v_132, (V)"hp") == (V)7.0d)))));

R.Call(v_3, R.Join(new V[] {v_150}, new Results(v_154)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_123))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_146)))));
foreach (var row_155 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_144, (V)"entities"))))))) {
V v_156 = row_155[0];
V v_157 = row_155[1];
if (R.Truth((V)(R.Get(v_157, (V)"companion_kind") == (V)"sentry"))) {
var values_158 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_159 = R.Get(R.Get(v_157, (V)"sentry_cells"), (V)2.0d);
V key_160 = (V)"x";
R.Set(obj_159, key_160, values_158[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"restore")}, new Results(v_144))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"stage handoff builds later chest offers for the actual class and carries a compatible saved run"}, new Results(new V((RuleFunction)(args_161 => {

var values_162 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_163 = values_162[0];

var values_164 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_165 = values_164[0];

var values_166 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_167 = values_166[0];

var values_168 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_169 = values_168[0];
foreach (var row_170 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"order"))))))) {
V v_171 = row_170[0];
V v_172 = row_170[1];
var values_173 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {(V)2.0d}, new Results(R.Table(new Entry((V)"class_id", v_172))))));
V v_174 = values_173[0];

var values_175 = R.Join(new V[] {}, new Results((V)false));
var obj_176 = R.Get(v_174, (V)"meta");
V key_177 = (V)"eligible";
R.Set(obj_176, key_177, values_175[0]);
var values_178 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_174))));
V v_179 = values_178[0];

var values_180 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_174}, new Results(R.Get(R.Get(v_174, (V)"stage"), (V)"elite_id")))));
V v_181 = values_180[0];
foreach (var row_182 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_174, (V)"entities"))))))) {
V v_183 = row_182[0];
V v_184 = row_182[1];
if (R.Truth((V)(R.Get(v_184, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_167, (V)"remove_for_actor"), R.Join(new V[] {v_174}, new Results(R.Get(v_184, (V)"id"))));
}
}
var values_185 = Results.Empty;
V v_186 = values_185[0];
foreach (var row_187 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_188 = row_187[0];
V v_189 = row_187[1];
var values_190 = R.Join(new V[] {(V)(R.Get(R.Get(v_181, (V)"position"), (V)"x") + R.Get(v_189, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_181, (V)"position"), (V)"y") + R.Get(v_189, (V)2.0d))));
V v_191 = values_190[0];
V v_192 = values_190[1];
if (R.Truth(R.Call(R.Get(v_163, (V)"walkable"), R.Join(new V[] {R.Get(v_174, (V)"generated_map"), v_191}, new Results(v_192))).First)) {
var values_193 = R.Join(new V[] {v_191}, new Results(v_192));
var obj_194 = R.Get(v_179, (V)"position");
V key_195 = (V)"x";
var obj_196 = R.Get(v_179, (V)"position");
V key_197 = (V)"y";
R.Set(obj_194, key_195, values_193[0]);
R.Set(obj_196, key_197, values_193[1]);

var values_198 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-R.Get(v_189, (V)1.0d))), new Entry((V)2, (-R.Get(v_189, (V)2.0d))))));
v_186 = values_198[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_186)));

var values_199 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_181, (V)"hp") + R.Get(v_181, (V)"defense")) + (V)2.0d)));
var obj_200 = v_179;
V key_201 = (V)"damage";
R.Set(obj_200, key_201, values_199[0]);

R.Call(R.Get(v_169, (V)"recompute"), R.Join(new V[] {}, new Results(v_174)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_174}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", R.Get(v_186, (V)1.0d)), new Entry((V)"dy", R.Get(v_186, (V)2.0d))))))));

var values_202 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_174}, new Results(R.Get(R.Get(v_174, (V)"stage"), (V)"exit_id")))))));
V v_203 = values_202[0];
foreach (var row_204 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_205 = row_204[0];
V v_206 = row_204[1];
var values_207 = R.Join(new V[] {(V)(R.Get(R.Get(v_203, (V)"position"), (V)"x") + R.Get(v_206, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_203, (V)"position"), (V)"y") + R.Get(v_206, (V)2.0d))));
V v_208 = values_207[0];
V v_209 = values_207[1];
if (R.Truth(R.Call(R.Get(v_163, (V)"walkable"), R.Join(new V[] {R.Get(v_174, (V)"generated_map"), v_208}, new Results(v_209))).First)) {
var values_210 = R.Join(new V[] {v_208}, new Results(v_209));
var obj_211 = R.Get(v_179, (V)"position");
V key_212 = (V)"x";
var obj_213 = R.Get(v_179, (V)"position");
V key_214 = (V)"y";
R.Set(obj_211, key_212, values_210[0]);
R.Set(obj_213, key_214, values_210[1]);

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_174}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", R.Get(v_203, (V)"id")), new Entry((V)"route_index", (V)1.0d)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_174))).First, (V)"definition_id") == v_172), () => (V)(R.Get(R.Get(v_174, (V)"stage"), (V)"index") == (V)2.0d)))));
var values_215 = R.Join(new V[] {}, new Results((V)0.0d));
V v_216 = values_215[0];
foreach (var row_217 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_174, (V)"entities"))))))) {
V v_218 = row_217[0];
V v_219 = row_217[1];
if (R.Truth((V)(R.Get(v_219, (V)"controller") == (V)"chest"))) {
foreach (var row_220 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_219, (V)"offers"))))))) {
V v_221 = row_220[0];
V v_222 = row_220[1];
var values_223 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_165, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_222, (V)"boon_id")))).First, (V)"eligible_classes")));
V v_224 = values_223[0];
if (R.Truth(v_224)) {
var values_225 = R.Join(new V[] {}, new Results((V)false));
V v_226 = values_225[0];

foreach (var row_227 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_224)))))) {
V v_228 = row_227[0];
V v_229 = row_227[1];
if (R.Truth((V)(v_229 == v_172))) {
var values_230 = R.Join(new V[] {}, new Results((V)true));
v_226 = values_230[0];
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {v_226}, new Results(R.Concat(v_172, R.Concat((V)" got ", R.Get(v_222, (V)"boon_id"))))));
}
var values_231 = R.Join(new V[] {}, new Results((V)(v_216 + (V)1.0d)));
v_216 = values_231[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_216 > (V)0.0d), () => R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_174))))).First))));
}
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
