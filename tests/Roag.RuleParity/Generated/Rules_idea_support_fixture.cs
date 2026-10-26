// Static C# port of tests/idea_support_fixture.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: b220fa2ff6ce6803088d56f2c151f33ae4a190e35f8606ebb50b64a87f849b09
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_support_fixture { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"order", R.Table(new Entry((V)1, (V)"doorward_lamp"), new Entry((V)2, (V)"secret_apple"), new Entry((V)3, (V)"spoils_aegis"), new Entry((V)4, (V)"mist_lullaby"), new Entry((V)5, (V)"rime_disk"), new Entry((V)6, (V)"vent_mantle"), new Entry((V)7, (V)"patient_rebuke"), new Entry((V)8, (V)"beacon_afterglow"), new Entry((V)9, (V)"sentry_quartermaster"), new Entry((V)10, (V)"spirit_shelter"), new Entry((V)11, (V)"grounding_rod"), new Entry((V)12, (V)"mirror_dividend"))))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"beacon_afterglow", (V)"wayfinder"), new Entry((V)"sentry_quartermaster", (V)"tinkerer"), new Entry((V)"spirit_shelter", (V)"spiritbinder"), new Entry((V)"grounding_rod", (V)"stormcaller"), new Entry((V)"mirror_dividend", (V)"ranger"))));
V v_14 = values_13[0];
V v_15 = V.Nil;
v_15 = new V((RuleFunction)(args_16 => {
V v_17 = args_16[0];
V v_18 = args_16[1];
var values_19 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_17}, new Results(v_18))));
V v_20 = values_19[0];
V v_21 = values_19[1];
V v_22 = values_19[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_20}, new Results(v_21)));

return R.Join(new V[] {}, new Results(v_22));
return Results.Empty;
}));
V v_23 = V.Nil;
v_23 = new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
{ double end_28 = R.Num((V)1.0d), step_29 = R.Num((-(V)1.0d));
for (double i_27 = R.Num((V)R.Length(R.Get(v_25, (V)"entities"))); step_29 > 0 ? i_27 <= end_28 : i_27 >= end_28; i_27 += step_29) {
V v_26 = i_27;
var values_30 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_25, (V)"entities"), v_26)));
V v_31 = values_30[0];
if (R.Truth((V)(R.Get(v_31, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_8, (V)"remove_for_actor"), R.Join(new V[] {v_25}, new Results(R.Get(v_31, (V)"id"))));

R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_25, (V)"entities")}, new Results(v_26)));
}
}
}
return Results.Empty;
}));
V v_32 = V.Nil;
v_32 = new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
V v_35 = args_33[1];
V v_36 = args_33[2];
foreach (var row_37 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_34, (V)"entities"))))))) {
V v_38 = row_37[0];
V v_39 = row_37[1];
if (R.Truth(R.And(R.And(R.And(R.Get(v_39, (V)"blocks_movement"), () => (V)(R.Get(v_39, (V)"id") != R.Get(v_34, (V)"player_id"))), () => (V)(R.Get(R.Get(v_39, (V)"position"), (V)"x") == v_35)), () => (V)(R.Get(R.Get(v_39, (V)"position"), (V)"y") == v_36)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
V v_40 = V.Nil;
v_40 = new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
{ double end_45 = R.Num((V)20.0d), step_46 = R.Num((V)1.0d);
for (double i_44 = R.Num((V)1.0d); step_46 > 0 ? i_44 <= end_45 : i_44 >= end_45; i_44 += step_46) {
V v_43 = i_44;
var values_47 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_43}, new Results(R.Or(R.And((V)(v_42 == (V)"m"), () => (V)"core:map/fractured_archive"), () => (V)"core:map/glacial_observatory")))));
V v_48 = values_47[0];
R.Call(v_23, R.Join(new V[] {}, new Results(v_48)));

var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"map_for"), R.Join(new V[] {}, new Results(v_48))));
V v_50 = values_49[0];
{ double end_53 = R.Num((V)(R.Get(v_50, (V)"height") - (V)1.0d)), step_54 = R.Num((V)1.0d);
for (double i_52 = R.Num((V)2.0d); step_54 > 0 ? i_52 <= end_53 : i_52 >= end_53; i_52 += step_54) {
V v_51 = i_52;
{ double end_57 = R.Num((V)(R.Get(v_50, (V)"width") - (V)1.0d)), step_58 = R.Num((V)1.0d);
for (double i_56 = R.Num((V)2.0d); step_58 > 0 ? i_56 <= end_57 : i_56 >= end_57; i_56 += step_58) {
V v_55 = i_56;
if (R.Truth(R.And((V)(R.Invoke(R.Get(R.Get(v_50, (V)"rows"), v_51), "sub", R.Join(new V[] {v_55}, new Results(v_55))).First == v_42), () => (V)(!R.Truth(R.Call(v_32, R.Join(new V[] {v_48, v_55}, new Results(v_51))).First))))) {
foreach (var row_59 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_60 = row_59[0];
V v_61 = row_59[1];
var values_62 = R.Join(new V[] {(V)(v_55 + R.Get(v_61, (V)1.0d))}, new Results((V)(v_51 + R.Get(v_61, (V)2.0d))));
V v_63 = values_62[0];
V v_64 = values_62[1];
if (R.Truth(R.And((V)(R.Invoke(R.Get(R.Get(v_50, (V)"rows"), v_64), "sub", R.Join(new V[] {v_63}, new Results(v_63))).First == (V)"."), () => (V)(!R.Truth(R.Call(v_32, R.Join(new V[] {v_48, v_63}, new Results(v_64))).First))))) {
var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_48))));
V v_66 = values_65[0];

var values_67 = R.Join(new V[] {v_55}, new Results(v_51));
var obj_68 = R.Get(v_66, (V)"position");
V key_69 = (V)"x";
var obj_70 = R.Get(v_66, (V)"position");
V key_71 = (V)"y";
R.Set(obj_68, key_69, values_67[0]);
R.Set(obj_70, key_71, values_67[1]);
return R.Join(new V[] {v_48, v_66}, new Results(v_61));
}
}
}
}
}
}
}
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results(R.Concat((V)"no natural ", R.Concat(v_42, (V)" edge in fixed terrain corpus")))));
return Results.Empty;
}));
R.Set(v_12, (V)"prepare", new V((RuleFunction)(args_72 => {
V v_73 = args_72[0];
var values_74 = Results.Empty;
V v_75 = values_74[0];

foreach (var row_76 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_12, (V)"order"))))))) {
V v_77 = row_76[0];
V v_78 = row_76[1];
if (R.Truth((V)(v_78 == v_73))) {
var values_79 = R.Join(new V[] {}, new Results((V)true));
v_75 = values_79[0];
}
}

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_75)));
var values_80 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", R.And(R.Get(v_14, v_73), () => R.Concat((V)"core:actor/", R.Get(v_14, v_73)))))))));
V v_81 = values_80[0];
var values_82 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_81))));
V v_83 = values_82[0];

var values_84 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_85 = R.Get(v_83, (V)"position");
V key_86 = (V)"x";
var obj_87 = R.Get(v_83, (V)"position");
V key_88 = (V)"y";
R.Set(obj_85, key_86, values_84[0]);
R.Set(obj_87, key_88, values_84[1]);

var values_89 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_90 = v_83;
V key_91 = (V)"hp";
R.Set(obj_90, key_91, values_89[0]);
var values_92 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"slug", v_73), new Entry((V)"intent", R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)"boons", R.Table(new Entry((V)1, v_73))))));
V v_93 = values_92[0];
V v_94 = V.Nil;
v_94 = new V((RuleFunction)(args_95 => {
V v_96 = args_95[0];
V v_97 = args_95[1];
V v_98 = args_95[2];
V v_99 = args_95[3];
var values_100 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_81, R.Or(v_99, () => (V)"core:enemy/mossbound_guard"), v_96, v_97}, new Results(R.Or(v_98, () => (V)100000.0d)))));
V v_101 = values_100[0];
var values_102 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_103 = v_101;
V key_104 = (V)"hp";
R.Set(obj_103, key_104, values_102[0]);

var values_105 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_106 = v_101;
V key_107 = (V)"max_hp";
R.Set(obj_106, key_107, values_105[0]);

var values_108 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_93, (V)"enemy"), () => v_101)));
var obj_109 = v_93;
V key_110 = (V)"enemy";
R.Set(obj_109, key_110, values_108[0]);

return R.Join(new V[] {}, new Results(v_101));
return Results.Empty;
}));
if (R.Truth((V)(v_73 == (V)"doorward_lamp"))) {
var values_111 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_door"), R.Join(new V[] {v_81, (V)8.0d}, new Results((V)6.0d))));
var obj_112 = v_93;
V key_113 = (V)"door";
R.Set(obj_112, key_113, values_111[0]);

var values_114 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))));
var obj_115 = v_93;
V key_116 = (V)"intent";
R.Set(obj_115, key_116, values_114[0]);
} else if (R.Truth((V)(v_73 == (V)"secret_apple"))) {
var values_117 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
v_81 = values_117[0];

R.Call(v_23, R.Join(new V[] {}, new Results(v_81)));

var values_118 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_81))));
v_83 = values_118[0];

var values_119 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_120 = v_83;
V key_121 = (V)"hp";
R.Set(obj_120, key_121, values_119[0]);
var values_122 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_81, (V)"secrets"), (V)1.0d)));
var obj_123 = v_93;
V key_124 = (V)"secret";
R.Set(obj_123, key_124, values_122[0]);

var values_125 = R.Join(new V[] {R.Get(R.Get(v_93, (V)"secret"), (V)"x")}, new Results(R.Get(R.Get(v_93, (V)"secret"), (V)"y")));
var obj_126 = R.Get(v_83, (V)"position");
V key_127 = (V)"x";
var obj_128 = R.Get(v_83, (V)"position");
V key_129 = (V)"y";
R.Set(obj_126, key_127, values_125[0]);
R.Set(obj_128, key_129, values_125[1]);
var values_130 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(R.Get(R.Get(v_93, (V)"secret"), (V)"wall_x") - R.Get(R.Get(v_93, (V)"secret"), (V)"x"))), new Entry((V)"dy", (V)(R.Get(R.Get(v_93, (V)"secret"), (V)"wall_y") - R.Get(R.Get(v_93, (V)"secret"), (V)"y"))))));
var obj_131 = v_93;
V key_132 = (V)"intent";
R.Set(obj_131, key_132, values_130[0]);
} else if (R.Truth((V)(v_73 == (V)"spoils_aegis"))) {
var values_133 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_chest"), R.Join(new V[] {v_81, (V)"core:chest/standard", (V)8.0d}, new Results((V)6.0d))));
var obj_134 = v_93;
V key_135 = (V)"chest";
R.Set(obj_134, key_135, values_133[0]);

var values_136 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_93, (V)"chest"), (V)"price")));
var obj_137 = v_81;
V key_138 = (V)"currency";
R.Set(obj_137, key_138, values_136[0]);
var values_139 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"boon_id", (V)"core:boon/opening_cut"), new Entry((V)"rarity", (V)"common"))), new Entry((V)2, R.Table(new Entry((V)"boon_id", (V)"core:boon/thorn_mirror"), new Entry((V)"rarity", (V)"common"))), new Entry((V)3, R.Table(new Entry((V)"boon_id", (V)"core:boon/longblade_geometry"), new Entry((V)"rarity", (V)"common"))))));
var obj_140 = R.Get(v_93, (V)"chest");
V key_141 = (V)"offers";
R.Set(obj_140, key_141, values_139[0]);
var values_142 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(R.Get(v_93, (V)"chest"), (V)"id")), new Entry((V)"offer_index", (V)1.0d))));
var obj_143 = v_93;
V key_144 = (V)"intent";
R.Set(obj_143, key_144, values_142[0]);
} else if (R.Truth(R.Or((V)(v_73 == (V)"mist_lullaby"), () => (V)(v_73 == (V)"rime_disk")))) {
var values_145 = Results.Empty;
V v_146 = values_145[0];

var values_147 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_73 == (V)"mist_lullaby"), () => (V)"m"), () => (V)"i")))));
v_81 = values_147[0];
v_83 = values_147[1];
v_146 = values_147[2];
var values_148 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_149 = v_83;
V key_150 = (V)"hp";
R.Set(obj_149, key_150, values_148[0]);
if (R.Truth((V)(v_73 == (V)"rime_disk"))) {
var values_151 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_146, (V)1.0d)), new Entry((V)"dy", R.Get(v_146, (V)2.0d)))));
var obj_152 = v_93;
V key_153 = (V)"intent";
R.Set(obj_152, key_153, values_151[0]);
}
} else if (R.Truth((V)(v_73 == (V)"vent_mantle"))) {
var values_154 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))).First, (V)"place"), R.Join(new V[] {v_81, R.Call(R.Get(v_6, (V)"map_for"), R.Join(new V[] {}, new Results(v_81))).First, (V)"heat_vent", (V)8.0d}, new Results((V)6.0d))));
var obj_155 = v_93;
V key_156 = (V)"vent";
R.Set(obj_155, key_156, values_154[0]);
R.Call(v_15, R.Join(new V[] {v_81}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
var values_157 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))));
var obj_158 = v_93;
V key_159 = (V)"intent";
R.Set(obj_158, key_159, values_157[0]);
} else if (R.Truth((V)(v_73 == (V)"patient_rebuke"))) {
var values_160 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_161 = v_83;
V key_162 = (V)"defense";
R.Set(obj_161, key_162, values_160[0]);

var values_163 = R.Join(new V[] {}, R.Call(v_94, R.Join(new V[] {(V)8.0d, (V)6.0d}, new Results((V)0.0d))));
V v_164 = values_163[0];
R.Call(R.Get(v_4, (V)"set_stacks"), R.Join(new V[] {v_83, R.Concat((V)"core:boon/", v_73), (V)"rare"}, new Results((V)1.0d)));
{ double end_167 = R.Num((V)3.0d), step_168 = R.Num((V)1.0d);
for (double i_166 = R.Num((V)1.0d); step_168 > 0 ? i_166 <= end_167 : i_166 >= end_167; i_166 += step_168) {
V v_165 = i_166;
R.Call(v_15, R.Join(new V[] {v_81}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
var values_169 = R.Join(new V[] {}, new Results(R.And(R.Get(v_83, (V)"boon_memory"), () => R.Get(R.Get(v_83, (V)"boon_memory"), R.Concat((V)"core:boon/", v_73)))));
V v_170 = values_169[0];
if (R.Truth(R.And(v_170, () => (V)(R.Get(v_170, (V)"charges") == (V)1.0d)))) {
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_83, (V)"boon_memory"), R.Concat((V)"core:boon/", v_73)), (V)"charges") == (V)1.0d))));
R.Call(R.Get(v_8, (V)"remove_for_actor"), R.Join(new V[] {v_81}, new Results(R.Get(v_164, (V)"id"))));
} else if (R.Truth((V)(v_73 == (V)"beacon_afterglow"))) {
R.Call(v_15, R.Join(new V[] {v_81}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))));
R.Call(v_15, R.Join(new V[] {v_81}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(v_94, R.Join(new V[] {(V)6.0d}, new Results((V)5.0d)));

var values_171 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))));
var obj_172 = v_93;
V key_173 = (V)"intent";
R.Set(obj_172, key_173, values_171[0]);
} else if (R.Truth(R.Or((V)(v_73 == (V)"sentry_quartermaster"), () => (V)(v_73 == (V)"spirit_shelter")))) {
var values_174 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"sweeping_slash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))));
var obj_175 = v_93;
V key_176 = (V)"intent";
R.Set(obj_175, key_176, values_174[0]);
} else if (R.Truth((V)(v_73 == (V)"grounding_rod"))) {
R.Call(v_94, R.Join(new V[] {(V)9.0d}, new Results((V)6.0d)));

R.Call(v_15, R.Join(new V[] {v_81}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_83, (V)"storm_charge") == (V)1.0d))));

var values_177 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d))));
var obj_178 = v_93;
V key_179 = (V)"intent";
R.Set(obj_178, key_179, values_177[0]);
} else {
var values_180 = R.Join(new V[] {}, R.Call(v_94, R.Join(new V[] {(V)10.0d, (V)6.0d, (V)50.0d}, new Results((V)"core:enemy/mirror_hexer"))));
var obj_181 = v_93;
V key_182 = (V)"hexer";
R.Set(obj_181, key_182, values_180[0]);
{ double end_185 = R.Num((V)3.0d), step_186 = R.Num((V)1.0d);
for (double i_184 = R.Num((V)1.0d); step_186 > 0 ? i_184 <= end_185 : i_184 >= end_185; i_184 += step_186) {
V v_183 = i_184;
R.Call(v_15, R.Join(new V[] {v_81}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_81, (V)"idea_mirrors")) == (V)1.0d))));
R.Call(R.Get(v_8, (V)"remove_for_actor"), R.Join(new V[] {v_81}, new Results(R.Get(R.Get(v_93, (V)"hexer"), (V)"id"))));
var values_187 = R.Join(new V[] {}, new Results(V.Nil));
var obj_188 = v_93;
V key_189 = (V)"enemy";
R.Set(obj_188, key_189, values_187[0]);

R.Call(v_94, R.Join(new V[] {(V)9.0d}, new Results((V)7.0d)));

var values_190 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))));
var obj_191 = v_93;
V key_192 = (V)"intent";
R.Set(obj_191, key_192, values_190[0]);
}
var values_193 = R.Join(new V[] {}, new Results(v_81));
var obj_194 = v_93;
V key_195 = (V)"state";
R.Set(obj_194, key_195, values_193[0]);

var values_196 = R.Join(new V[] {}, new Results(v_83));
var obj_197 = v_93;
V key_198 = (V)"player";
R.Set(obj_197, key_198, values_196[0]);
R.Call(R.Get(v_4, (V)"set_stacks"), R.Join(new V[] {v_83, R.Concat((V)"core:boon/", v_73), (V)"rare"}, new Results((V)1.0d)));

R.Call(R.Get(v_10, (V)"recompute"), R.Join(new V[] {}, new Results(v_81)));
var values_199 = R.Join(new V[] {}, new Results(R.Get(v_83, (V)"hp")));
var obj_200 = v_93;
V key_201 = (V)"hp_before";
R.Set(obj_200, key_201, values_199[0]);

var values_202 = R.Join(new V[] {}, new Results(R.Get(v_81, (V)"clock")));
var obj_203 = v_93;
V key_204 = (V)"clock_before";
R.Set(obj_203, key_204, values_202[0]);
return R.Join(new V[] {}, new Results(v_93));
return Results.Empty;
})));
R.Set(v_12, (V)"install", new V((RuleFunction)(args_205 => {
V v_206 = args_205[0];
var values_207 = R.Join(new V[] {}, new Results(R.Table()));
var obj_208 = R.Get(v_206, (V)"player");
V key_209 = (V)"boons";
R.Set(obj_208, key_209, values_207[0]);
foreach (var row_210 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_206, (V)"boons"))))))) {
V v_211 = row_210[0];
V v_212 = row_210[1];
R.Call(R.Get(v_4, (V)"set_stacks"), R.Join(new V[] {R.Get(v_206, (V)"player"), R.Concat((V)"core:boon/", v_212), (V)"rare"}, new Results((V)1.0d)));
}
return Results.Empty;
})));
R.Set(v_12, (V)"check", new V((RuleFunction)(args_213 => {
V v_214 = args_213[0];
V v_215 = args_213[1];
var values_216 = R.Join(new V[] {}, new Results((V)0.0d));
V v_217 = values_216[0];
foreach (var row_218 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_215)))))) {
V v_219 = row_218[0];
V v_220 = row_218[1];
if (R.Truth(R.And((V)(R.Get(v_220, (V)"kind") == (V)"BoonActivated"), () => (V)(R.Get(v_220, (V)"boon_id") == R.Concat((V)"core:boon/", R.Get(v_214, (V)"slug")))))) {
var values_221 = R.Join(new V[] {}, new Results((V)(v_217 + (V)1.0d)));
v_217 = values_221[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_217 == (V)1.0d)}, new Results(R.Concat((V)"expected one ", R.Concat(R.Get(v_214, (V)"slug"), R.Concat((V)" activation, got ", v_217))))));
var values_222 = R.Join(new V[] {}, new Results(R.Get(v_214, (V)"player")));
V v_223 = values_222[0];

var values_224 = R.Join(new V[] {}, new Results(R.And(R.Get(v_223, (V)"boon_memory"), () => R.Get(R.Get(v_223, (V)"boon_memory"), R.Concat((V)"core:boon/", R.Get(v_214, (V)"slug"))))));
V v_225 = values_224[0];
if (R.Truth((V)(R.Get(v_214, (V)"slug") == (V)"secret_apple"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_214, (V)"secret"), (V)"revealed"), () => (V)(R.Get(v_223, (V)"hp") == (V)(R.Get(v_214, (V)"hp_before") + (V)7.0d))))));
} else if (R.Truth((V)(R.Get(v_214, (V)"slug") == (V)"mist_lullaby"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_223, (V)"hp") == (V)(R.Get(v_214, (V)"hp_before") + (V)3.0d)), () => (V)(R.Get(v_225, (V)"expires_at") == (V)(R.Get(v_214, (V)"clock_before") + (V)400.0d))))));
} else if (R.Truth((V)(R.Get(v_214, (V)"slug") == (V)"mirror_dividend"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(R.Get(v_214, (V)"state"), (V)"idea_mirrors")) == (V)0.0d), () => (V)(R.Get(v_223, (V)"hp") == (V)(R.Get(v_214, (V)"hp_before") + (V)4.0d))))));
} else if (R.Truth((V)(R.Get(v_214, (V)"slug") == (V)"beacon_afterglow"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_223, (V)"position"), (V)"x") == (V)6.0d), () => (V)(R.Get(R.Get(v_214, (V)"enemy"), (V)"hp") == (V)98.0d)))));
} else if (R.Truth((V)(R.Get(v_214, (V)"slug") == (V)"patient_rebuke"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_225, (V)"charges") == (V)0.0d), () => (V)(R.Get(R.Get(v_214, (V)"enemy"), (V)"hp") == (V)97.0d)))));
} else if (R.Truth((V)(R.Get(v_214, (V)"slug") == (V)"sentry_quartermaster"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_225, (V)"speed") == (V)20.0d))));
} else if (R.Truth((V)(R.Get(v_214, (V)"slug") == (V)"vent_mantle"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_223, (V)"hp") == R.Get(v_214, (V)"hp_before")), () => (V)(R.Get(v_225, (V)"ward") == (V)0.0d)))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_225, () => R.Get(v_225, (V)"ward")), () => (V)(R.Get(v_225, (V)"ward") > (V)0.0d)))));
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_12));
return Results.Empty;
} }
