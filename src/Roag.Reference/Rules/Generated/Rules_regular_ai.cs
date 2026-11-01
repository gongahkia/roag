// Static C# port of src/regular_ai.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: ec1c0c28655219ab0472c6a23e23486f5e5c229ffcb74b661743ff8799bd96a3
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_regular_ai { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:enemy/mossbreaker", (V)"breaker"), new Entry((V)"core:enemy/gloom_strider", (V)"skirmisher"), new Entry((V)"core:enemy/mire_binder", (V)"denial"), new Entry((V)"core:enemy/pack_chorister", (V)"coordinator"), new Entry((V)"core:enemy/vault_lurker", (V)"ambusher"), new Entry((V)"core:enemy/crystal_bombardier", (V)"artillery"), new Entry((V)"core:enemy/cinder_mender", (V)"support"), new Entry((V)"core:enemy/glass_charger", (V)"charger"), new Entry((V)"core:enemy/forge_conjurer", (V)"summoner"), new Entry((V)"core:enemy/bastion_bulwark", (V)"retaliator"))));
var obj_4 = v_2;
V key_5 = (V)"roles";
R.Set(obj_4, key_5, values_3[0]);
var values_6 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_7 = values_6[0];
V v_8 = V.Nil;
v_8 = new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_10), new Entry((V)"y", v_11))));
return Results.Empty;
}));
V v_12 = V.Nil;
v_12 = new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
V v_15 = args_13[1];
V v_16 = args_13[2];
V v_17 = args_13[3];
var values_18 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", R.Concat((V)"core:ability/", v_14)), new Entry((V)"cells", v_15), new Entry((V)"ranged", v_16))));
V v_19 = values_18[0];
foreach (var row_20 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(v_17, () => R.Table()))))))) {
V v_21 = row_20[0];
V v_22 = row_20[1];
var values_23 = R.Join(new V[] {}, new Results(v_22));
var obj_24 = v_19;
V key_25 = v_21;
R.Set(obj_24, key_25, values_23[0]);
}
return R.Join(new V[] {}, new Results(v_19));
return Results.Empty;
}));
V v_26 = V.Nil;
v_26 = new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
V v_29 = args_27[1];
V v_30 = args_27[2];
V v_31 = args_27[3];
foreach (var row_32 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_28, (V)"entities"))))))) {
V v_33 = row_32[0];
V v_34 = row_32[1];
var values_35 = R.Join(new V[] {}, new Results(R.Get(v_34, (V)"position")));
V v_36 = values_35[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_34, (V)"id") != R.Get(v_29, (V)"id")), () => R.Get(v_34, (V)"hp")), () => (V)(R.Get(v_34, (V)"hp") > (V)0.0d)), () => v_36), () => (V)(R.Get(v_36, (V)"floor_id") == R.Get(R.Get(v_29, (V)"position"), (V)"floor_id"))), () => (V)(R.Get(v_36, (V)"x") == v_30)), () => (V)(R.Get(v_36, (V)"y") == v_31)))) {
return R.Join(new V[] {}, new Results(v_34));
}
}
return Results.Empty;
}));
V v_37 = V.Nil;
v_37 = new V((RuleFunction)(args_38 => {
V v_39 = args_38[0];
V v_40 = args_38[1];
V v_41 = args_38[2];
var values_42 = R.Join(new V[] {}, new Results(R.Table()));
V v_43 = values_42[0];
foreach (var row_44 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_39, (V)"entities"))))))) {
V v_45 = row_44[0];
V v_46 = row_44[1];
var values_47 = R.Join(new V[] {}, new Results(R.Get(v_46, (V)"position")));
V v_48 = values_47[0];
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_46, (V)"id") != R.Get(v_40, (V)"id")), () => (V)(R.Get(v_46, (V)"controller") == (V)"ai")), () => (V)(R.Get(v_46, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_48, (V)"floor_id") == R.Get(R.Get(v_40, (V)"position"), (V)"floor_id"))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_48, (V)"x") - R.Get(R.Get(v_40, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_48, (V)"y") - R.Get(R.Get(v_40, (V)"position"), (V)"y"))))).First) <= v_41)))) {
var values_49 = R.Join(new V[] {}, new Results(v_46));
var obj_50 = v_43;
V key_51 = (V)((V)R.Length(v_43) + (V)1.0d);
R.Set(obj_50, key_51, values_49[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_43}, new Results(new V((RuleFunction)(args_52 => {
V v_53 = args_52[0];
V v_54 = args_52[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_53, (V)"id") < R.Get(v_54, (V)"id"))));
return Results.Empty;
})))));
return R.Join(new V[] {}, new Results(v_43));
return Results.Empty;
}));
V v_55 = V.Nil;
v_55 = new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
V v_58 = args_56[1];
V v_59 = args_56[2];
V v_60 = args_56[3];
V v_61 = args_56[4];
var values_62 = R.Join(new V[] {}, new Results(R.Table()));
V v_63 = values_62[0];
foreach (var row_64 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_61)))))) {
V v_65 = row_64[0];
V v_66 = row_64[1];
var values_67 = R.Join(new V[] {(V)(v_59 + R.Get(v_66, (V)1.0d))}, new Results((V)(v_60 + R.Get(v_66, (V)2.0d))));
V v_68 = values_67[0];
V v_69 = values_67[1];
if (R.Truth(R.Call(v_58, R.Join(new V[] {v_57, v_68}, new Results(v_69))).First)) {
var values_70 = R.Join(new V[] {}, R.Call(v_8, R.Join(new V[] {v_68}, new Results(v_69))));
var obj_71 = v_63;
V key_72 = (V)((V)R.Length(v_63) + (V)1.0d);
R.Set(obj_71, key_72, values_70[0]);
}
}
return R.Join(new V[] {}, new Results(v_63));
return Results.Empty;
}));
R.Set(v_2, (V)"choose", new V((RuleFunction)(args_73 => {
V v_74 = args_73[0];
V v_75 = args_73[1];
V v_76 = args_73[2];
V v_77 = args_73[3];
var values_78 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_2, (V)"roles"), R.Get(v_75, (V)"definition_id"))}, new Results((V)"unknown regular role"))));
V v_79 = values_78[0];
var values_80 = R.Join(new V[] {R.Get(v_76, (V)"position")}, new Results(R.Get(v_75, (V)"position")));
V v_81 = values_80[0];
V v_82 = values_80[1];
var values_83 = R.Join(new V[] {(V)(R.Get(v_81, (V)"x") - R.Get(v_82, (V)"x"))}, new Results((V)(R.Get(v_81, (V)"y") - R.Get(v_82, (V)"y"))));
V v_84 = values_83[0];
V v_85 = values_83[1];
var values_86 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_84))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_85))).First)));
V v_87 = values_86[0];
var values_88 = R.Join(new V[] {}, new Results(R.Get(v_77, (V)"map")));
V v_89 = values_88[0];
var values_90 = R.Join(new V[] {}, new Results(R.Get(v_77, (V)"projectile_passable")));
V v_91 = values_90[0];
var values_92 = R.Join(new V[] {}, new Results(R.And((V)(v_87 <= R.Get(R.Get(v_77, (V)"definition"), (V)"range")), () => R.Call(R.Get(v_77, (V)"clear_line"), R.Join(new V[] {v_74, v_82}, new Results(v_81))).First)));
V v_93 = values_92[0];
V v_94 = V.Nil;
v_94 = new V((RuleFunction)(args_95 => {

return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_77, (V)"next_step"), R.Join(new V[] {v_74, v_75}, R.Call(R.Get(v_77, (V)"adjacent_goals"), R.Join(new V[] {v_74, v_75, v_76}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
return Results.Empty;
}));
if (R.Truth((V)(v_79 == (V)"breaker"))) {
var values_96 = Results.Empty;
V v_97 = values_96[0];
foreach (var row_98 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_7)))))) {
V v_99 = row_98[0];
V v_100 = row_98[1];
var values_101 = R.Join(new V[] {(V)(R.Get(v_82, (V)"x") + R.Get(v_100, (V)1.0d))}, new Results((V)(R.Get(v_82, (V)"y") + R.Get(v_100, (V)2.0d))));
V v_102 = values_101[0];
V v_103 = values_101[1];
if (R.Truth(R.And(R.Get(R.Get(v_89, (V)"rows"), v_103), () => (V)(R.Invoke(R.Get(R.Get(v_89, (V)"rows"), v_103), "sub", R.Join(new V[] {v_102}, new Results(v_102))).First == (V)"B")))) {
var values_104 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_81, (V)"x") - v_102)))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_81, (V)"y") - v_103)))).First)));
V v_105 = values_104[0];
if (R.Truth(R.Or((V)(!R.Truth(v_97)), () => (V)(v_105 < R.Get(v_97, (V)"score"))))) {
var values_106 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_102), new Entry((V)"y", v_103), new Entry((V)"score", v_105))));
v_97 = values_106[0];
}
}
}
if (R.Truth(v_97)) {
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"mossbreaker_crack", R.Table(new Entry(1, R.Call(v_8, R.Join(new V[] {R.Get(v_97, (V)"x")}, new Results(R.Get(v_97, (V)"y"))))))}, new Results((V)false))));
}
if (R.Truth((V)(v_87 == (V)1.0d))) {
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"mossbreaker_cleave", R.Table(new Entry(1, R.Call(v_8, R.Join(new V[] {R.Get(v_81, (V)"x")}, new Results(R.Get(v_81, (V)"y"))))))}, new Results((V)false))));
}
return R.Join(new V[] {}, R.Call(v_94, Results.Empty));
} else if (R.Truth((V)(v_79 == (V)"skirmisher"))) {
if (R.Truth((V)(v_87 <= (V)2.0d))) {
foreach (var row_107 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_7)))))) {
V v_108 = row_107[0];
V v_109 = row_107[1];
var values_110 = R.Join(new V[] {(V)(R.Get(v_82, (V)"x") + R.Get(v_109, (V)1.0d))}, new Results((V)(R.Get(v_82, (V)"y") + R.Get(v_109, (V)2.0d))));
V v_111 = values_110[0];
V v_112 = values_110[1];
if (R.Truth(R.And(R.Call(R.Get(v_77, (V)"free"), R.Join(new V[] {v_74, R.Get(v_75, (V)"id"), v_111}, new Results(v_112))).First, () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_81, (V)"x") - v_111)))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_81, (V)"y") - v_112)))).First) > v_87)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_109, (V)1.0d)), new Entry((V)"dy", R.Get(v_109, (V)2.0d)))));
}
}
}
if (R.Truth(v_93)) {
var values_113 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))))));
V v_114 = values_113[0];
if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_84))).First >= R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_85))).First))) {
var values_115 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))));
var obj_116 = v_114;
V key_117 = (V)2.0d;
R.Set(obj_116, key_117, values_115[0]);

var values_118 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))));
var obj_119 = v_114;
V key_120 = (V)3.0d;
R.Set(obj_119, key_120, values_118[0]);
} else {
var values_121 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))));
var obj_122 = v_114;
V key_123 = (V)2.0d;
R.Set(obj_122, key_123, values_121[0]);

var values_124 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))));
var obj_125 = v_114;
V key_126 = (V)3.0d;
R.Set(obj_125, key_126, values_124[0]);
}
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"gloom_fork", R.Call(v_55, R.Join(new V[] {v_89, v_91, R.Get(v_81, (V)"x"), R.Get(v_81, (V)"y")}, new Results(v_114))).First}, new Results((V)"area"))));
}
return R.Join(new V[] {}, R.Call(v_94, Results.Empty));
} else if (R.Truth((V)(v_79 == (V)"denial"))) {
if (R.Truth(v_93)) {
var values_127 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_128 = values_127[0];
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"mire_ring", R.Call(v_55, R.Join(new V[] {v_89, v_91, R.Get(v_81, (V)"x"), R.Get(v_81, (V)"y")}, new Results(v_128))).First}, new Results((V)"area"))));
}
return R.Join(new V[] {}, R.Call(v_94, Results.Empty));
} else if (R.Truth((V)(v_79 == (V)"coordinator"))) {
if (R.Truth((V)(v_87 == (V)1.0d))) {
var values_129 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_74, v_75}, new Results((V)2.0d))));
V v_130 = values_129[0];
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"pack_chorus", R.Table(new Entry(1, R.Call(v_8, R.Join(new V[] {R.Get(v_81, (V)"x")}, new Results(R.Get(v_81, (V)"y")))))), (V)false}, new Results(R.Table(new Entry((V)"damage_bonus", R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)2.0d}, new Results((V)R.Length(v_130)))).First))))));
}
return R.Join(new V[] {}, R.Call(v_94, Results.Empty));
} else if (R.Truth((V)(v_79 == (V)"ambusher"))) {
if (R.Truth(R.And((V)(v_87 > (V)5.0d), () => (V)(!R.Truth(R.Get(R.Get(v_75, (V)"ai_memory"), (V)"alerted")))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))));
}
if (R.Truth(v_93)) {
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"vault_ambush", R.Call(v_55, R.Join(new V[] {v_89, v_91, R.Get(v_81, (V)"x"), R.Get(v_81, (V)"y")}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)4, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))))))).First, (V)"area"}, new Results(R.Table(new Entry((V)"alert", (V)true))))));
}
var values_131 = R.Join(new V[] {}, R.Call(v_94, Results.Empty));
V v_132 = values_131[0];
var values_133 = R.Join(new V[] {}, new Results((V)true));
var obj_134 = v_132;
V key_135 = (V)"alert";
R.Set(obj_134, key_135, values_133[0]);
return R.Join(new V[] {}, new Results(v_132));
} else if (R.Truth((V)(v_79 == (V)"artillery"))) {
if (R.Truth(v_93)) {
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"crystal_barrage", R.Call(v_55, R.Join(new V[] {v_89, v_91, R.Get(v_81, (V)"x"), R.Get(v_81, (V)"y")}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)2.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)2.0d))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)2.0d)), new Entry((V)2, (V)0.0d))))))).First}, new Results((V)"area"))));
}
return R.Join(new V[] {}, R.Call(v_94, Results.Empty));
} else if (R.Truth((V)(v_79 == (V)"support"))) {
var values_136 = Results.Empty;
V v_137 = values_136[0];
foreach (var row_138 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_74, v_75}, new Results((V)3.0d)))))))) {
V v_139 = row_138[0];
V v_140 = row_138[1];
if (R.Truth(R.And((V)(R.Get(v_140, (V)"hp") < R.Get(v_140, (V)"max_hp")), () => R.Or((V)(!R.Truth(v_137)), () => (V)((V)(R.Get(v_140, (V)"hp") * R.Get(v_137, (V)"max_hp")) < (V)(R.Get(v_137, (V)"hp") * R.Get(v_140, (V)"max_hp"))))))) {
var values_141 = R.Join(new V[] {}, new Results(v_140));
v_137 = values_141[0];
}
}
if (R.Truth(v_137)) {
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"cinder_mend", R.Table(new Entry(1, R.Call(v_8, R.Join(new V[] {R.Get(R.Get(v_137, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_137, (V)"position"), (V)"y")))))), (V)false}, new Results(R.Table(new Entry((V)"support", R.Table(new Entry((V)"kind", (V)"heal"), new Entry((V)"power", (V)3.0d))))))));
}
if (R.Truth((V)(v_87 == (V)1.0d))) {
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"cinder_tap", R.Table(new Entry(1, R.Call(v_8, R.Join(new V[] {R.Get(v_81, (V)"x")}, new Results(R.Get(v_81, (V)"y"))))))}, new Results((V)false))));
}
return R.Join(new V[] {}, R.Call(v_94, Results.Empty));
} else if (R.Truth((V)(v_79 == (V)"charger"))) {
if (R.Truth(R.And(R.And(R.And((V)(v_87 >= (V)2.0d), () => (V)(v_87 <= (V)4.0d)), () => R.Or((V)(v_84 == (V)0.0d), () => (V)(v_85 == (V)0.0d))), () => v_93))) {
var values_142 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_84 == (V)0.0d), () => (V)0.0d), () => (V)(v_84 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_84))).First))));
V v_143 = values_142[0];
var values_144 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_85 == (V)0.0d), () => (V)0.0d), () => (V)(v_85 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_85))).First))));
V v_145 = values_144[0];
var values_146 = R.Join(new V[] {}, new Results(R.Table()));
V v_147 = values_146[0];
{ double end_150 = R.Num(v_87), step_151 = R.Num((V)1.0d);
for (double i_149 = R.Num((V)1.0d); step_151 > 0 ? i_149 <= end_150 : i_149 >= end_150; i_149 += step_151) {
V v_148 = i_149;
var values_152 = R.Join(new V[] {}, R.Call(v_8, R.Join(new V[] {(V)(R.Get(v_82, (V)"x") + (V)(v_143 * v_148))}, new Results((V)(R.Get(v_82, (V)"y") + (V)(v_145 * v_148))))));
var obj_153 = v_147;
V key_154 = (V)((V)R.Length(v_147) + (V)1.0d);
R.Set(obj_153, key_154, values_152[0]);
}
}
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"glass_charge", v_147, (V)"piercing_line"}, new Results(R.Table(new Entry((V)"advance", R.Table(new Entry((V)"dx", v_143), new Entry((V)"dy", v_145), new Entry((V)"max_steps", R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)3.0d}, new Results(v_87))).First))))))));
}
if (R.Truth((V)(v_87 == (V)1.0d))) {
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"glass_jab", R.Table(new Entry(1, R.Call(v_8, R.Join(new V[] {R.Get(v_81, (V)"x")}, new Results(R.Get(v_81, (V)"y"))))))}, new Results((V)false))));
}
return R.Join(new V[] {}, R.Call(v_94, Results.Empty));
} else if (R.Truth((V)(v_79 == (V)"summoner"))) {
var values_155 = R.Join(new V[] {}, R.Call(v_37, R.Join(new V[] {v_74, v_75}, new Results((V)5.0d))));
V v_156 = values_155[0];
if (R.Truth(R.And(R.And((V)(v_87 <= (V)5.0d), () => (V)((V)R.Length(v_156) < (V)3.0d)), () => (V)(R.Or(R.Get(R.Get(v_75, (V)"ai_memory"), (V)"summons"), () => (V)0.0d) < (V)3.0d)))) {
foreach (var row_157 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_7)))))) {
V v_158 = row_157[0];
V v_159 = row_157[1];
var values_160 = R.Join(new V[] {(V)(R.Get(v_82, (V)"x") + R.Get(v_159, (V)1.0d))}, new Results((V)(R.Get(v_82, (V)"y") + R.Get(v_159, (V)2.0d))));
V v_161 = values_160[0];
V v_162 = values_160[1];
if (R.Truth(R.And(R.Call(R.Get(v_77, (V)"free"), R.Join(new V[] {v_74, R.Get(v_75, (V)"id"), v_161}, new Results(v_162))).First, () => (V)(!R.Truth(R.Call(v_26, R.Join(new V[] {v_74, v_75, v_161}, new Results(v_162))).First))))) {
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"forge_call", R.Table(new Entry(1, R.Call(v_8, R.Join(new V[] {v_161}, new Results(v_162))))), (V)false}, new Results(R.Table(new Entry((V)"support", R.Table(new Entry((V)"kind", (V)"summon"), new Entry((V)"enemy_id", (V)"core:enemy/ruin_skitter"), new Entry((V)"max_active", (V)3.0d))))))));
}
}
}
if (R.Truth((V)(v_87 == (V)1.0d))) {
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"forge_tap", R.Table(new Entry(1, R.Call(v_8, R.Join(new V[] {R.Get(v_81, (V)"x")}, new Results(R.Get(v_81, (V)"y"))))))}, new Results((V)false))));
}
return R.Join(new V[] {}, R.Call(v_94, Results.Empty));
} else if (R.Truth((V)(v_79 == (V)"retaliator"))) {
if (R.Truth(R.And((V)(v_87 <= (V)2.0d), () => R.Get(R.Get(v_75, (V)"ai_memory"), (V)"retaliate")))) {
var values_163 = R.Join(new V[] {}, R.Call(v_55, R.Join(new V[] {v_89, v_91, R.Get(v_81, (V)"x"), R.Get(v_81, (V)"y")}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))))));
V v_164 = values_163[0];
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"bulwark_retort", v_164, (V)"area"}, new Results(R.Table(new Entry((V)"clear_retaliate", (V)true))))));
}
if (R.Truth((V)(v_87 == (V)1.0d))) {
return R.Join(new V[] {}, R.Call(v_12, R.Join(new V[] {(V)"bulwark_bash", R.Table(new Entry(1, R.Call(v_8, R.Join(new V[] {R.Get(v_81, (V)"x")}, new Results(R.Get(v_81, (V)"y"))))))}, new Results((V)false))));
}
return R.Join(new V[] {}, R.Call(v_94, Results.Empty));
}
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_165 => {

var values_166 = R.Join(new V[] {}, new Results((V)0.0d));
V v_167 = values_166[0];
foreach (var row_168 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"roles"))))))) {
V v_169 = row_168[0];
V v_170 = row_168[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(v_169, "match", R.Join(new V[] {}, new Results((V)"^core:enemy/[a-z_]+$"))).First, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_170))).First == (V)"string")))));
var values_171 = R.Join(new V[] {}, new Results((V)(v_167 + (V)1.0d)));
v_167 = values_171[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_167 == (V)10.0d)}, new Results((V)"expected exactly ten expansion regulars")));
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
