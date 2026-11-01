// Static C# port of src/rewards.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 7c7ffe3bb3400eb447f01f7d00324d46c7d06b661f88af71f5ebfc20bb55fc3d
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_rewards { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table()));
V v_10 = values_9[0];
V v_11 = V.Nil;
v_11 = new V((RuleFunction)(args_12 => {
V v_13 = args_12[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_13))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_13));
}
var values_14 = R.Join(new V[] {}, new Results(R.Table()));
V v_15 = values_14[0];
foreach (var row_16 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_13)))))) {
V v_17 = row_16[0];
V v_18 = row_16[1];
var values_19 = R.Join(new V[] {}, R.Call(v_11, R.Join(new V[] {}, new Results(v_18))));
var obj_20 = v_15;
V key_21 = v_17;
R.Set(obj_20, key_21, values_19[0]);
}
return R.Join(new V[] {}, new Results(v_15));
return Results.Empty;
}));
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
foreach (var row_25 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"entities"))))))) {
V v_26 = row_25[0];
V v_27 = row_25[1];
if (R.Truth((V)(R.Get(v_27, (V)"id") == R.Get(v_24, (V)"player_id")))) {
return R.Join(new V[] {}, new Results(v_27));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"missing player")));
return Results.Empty;
}));
R.Set(v_10, (V)"next_threshold", new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_29))).First == (V)"number"), () => (V)((V)(v_29 % (V)1.0d) == (V)0.0d)), () => (V)(v_29 >= (V)1.0d))}, new Results((V)"invalid player level")));
var values_30 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"get_progression"), Results.Empty));
V v_31 = values_30[0];
return R.Join(new V[] {}, new Results((V)((V)(R.Get(v_31, (V)"first_threshold") * v_29) + (V)((V)((V)(R.Get(v_31, (V)"threshold_step") * v_29) * (V)(v_29 - (V)1.0d)) / (V)2.0d))));
return Results.Empty;
})));
V v_32 = V.Nil;
v_32 = new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
var values_35 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"get_progression"), Results.Empty));
V v_36 = values_35[0];
var values_37 = R.Join(new V[] {}, new Results((V)0.0d));
V v_38 = values_37[0];
foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_36, (V)"stat_order"))))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
var values_42 = R.Join(new V[] {}, new Results((V)(v_38 + R.Get(R.Get(v_36, (V)"stat_weights"), v_41))));
v_38 = values_42[0];
}
var values_43 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"next_int"), R.Join(new V[] {R.Get(v_34, (V)"rng")}, new Results(v_38))));
V v_44 = values_43[0];
foreach (var row_45 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_36, (V)"stat_order"))))))) {
V v_46 = row_45[0];
V v_47 = row_45[1];
var values_48 = R.Join(new V[] {}, new Results((V)(v_44 - R.Get(R.Get(v_36, (V)"stat_weights"), v_47))));
v_44 = values_48[0];
if (R.Truth((V)(v_44 <= (V)0.0d))) {
return R.Join(new V[] {}, new Results(v_47));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"invalid stat weights")));
return Results.Empty;
}));
V v_49 = V.Nil;
v_49 = new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
V v_52 = args_50[1];
V v_53 = args_50[2];
if (R.Truth((V)(v_52 == (V)"max_health"))) {
var values_54 = R.Join(new V[] {}, new Results(R.Get(v_51, (V)"max_hp")));
V v_55 = values_54[0];
var values_56 = R.Join(new V[] {}, new Results((V)(v_55 + v_53)));
var obj_57 = v_51;
V key_58 = (V)"max_hp";
R.Set(obj_57, key_58, values_56[0]);
var values_59 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_51, (V)"max_hp")}, new Results((V)(R.Get(v_51, (V)"hp") + v_53)))));
var obj_60 = v_51;
V key_61 = (V)"hp";
R.Set(obj_60, key_61, values_59[0]);
return R.Join(new V[] {v_55}, new Results(R.Get(v_51, (V)"max_hp")));
} else if (R.Truth((V)(v_52 == (V)"damage"))) {
var values_62 = R.Join(new V[] {R.Get(v_51, (V)"damage")}, new Results(R.Get(v_51, (V)"slash_damage")));
V v_63 = values_62[0];
V v_64 = values_62[1];
var values_65 = R.Join(new V[] {(V)(v_63 + v_53)}, new Results((V)(v_64 + v_53)));
var obj_66 = v_51;
V key_67 = (V)"damage";
var obj_68 = v_51;
V key_69 = (V)"slash_damage";
R.Set(obj_66, key_67, values_65[0]);
R.Set(obj_68, key_69, values_65[1]);
return R.Join(new V[] {v_63, R.Get(v_51, (V)"damage")}, new Results(R.Table(new Entry((V)"slash_old", v_64), new Entry((V)"slash_new", R.Get(v_51, (V)"slash_damage")))));
} else if (R.Truth((V)(v_52 == (V)"defense"))) {
var values_70 = R.Join(new V[] {}, new Results(R.Get(v_51, (V)"defense")));
V v_71 = values_70[0];
var values_72 = R.Join(new V[] {}, new Results((V)(v_71 + v_53)));
var obj_73 = v_51;
V key_74 = (V)"defense";
R.Set(obj_73, key_74, values_72[0]);
return R.Join(new V[] {v_71}, new Results(R.Get(v_51, (V)"defense")));
} else if (R.Truth((V)(v_52 == (V)"speed"))) {
var values_75 = R.Join(new V[] {}, new Results(R.Get(v_51, (V)"speed")));
V v_76 = values_75[0];
var values_77 = R.Join(new V[] {}, new Results((V)(v_76 + v_53)));
var obj_78 = v_51;
V key_79 = (V)"speed";
R.Set(obj_78, key_79, values_77[0]);
return R.Join(new V[] {v_76}, new Results(R.Get(v_51, (V)"speed")));
} else if (R.Truth((V)(v_52 == (V)"crit_chance"))) {
var values_80 = R.Join(new V[] {}, new Results(R.Get(v_51, (V)"crit_chance_ppm")));
V v_81 = values_80[0];
var values_82 = R.Join(new V[] {}, new Results((V)(v_81 + v_53)));
var obj_83 = v_51;
V key_84 = (V)"crit_chance_ppm";
R.Set(obj_83, key_84, values_82[0]);
return R.Join(new V[] {v_81}, new Results(R.Get(v_51, (V)"crit_chance_ppm")));
} else if (R.Truth((V)(v_52 == (V)"crit_damage"))) {
var values_85 = R.Join(new V[] {}, new Results(R.Get(v_51, (V)"crit_damage_percent")));
V v_86 = values_85[0];
var values_87 = R.Join(new V[] {}, new Results((V)(v_86 + v_53)));
var obj_88 = v_51;
V key_89 = (V)"crit_damage_percent";
R.Set(obj_88, key_89, values_87[0]);
return R.Join(new V[] {v_86}, new Results(R.Get(v_51, (V)"crit_damage_percent")));
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"unknown growth stat")));
return Results.Empty;
}));
R.Set(v_10, (V)"gain_xp", new V((RuleFunction)(args_90 => {
V v_91 = args_90[0];
V v_92 = args_90[1];
V v_93 = args_90[2];
V v_94 = args_90[3];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_92))).First == (V)"number"), () => (V)((V)(v_92 % (V)1.0d) == (V)0.0d)), () => (V)(v_92 > (V)0.0d))}, new Results((V)"invalid XP reward")));
var values_95 = R.Join(new V[] {}, new Results(R.Get(v_91, (V)"progression")));
V v_96 = values_95[0];
var values_97 = R.Join(new V[] {}, new Results(R.Get(v_96, (V)"xp_total")));
V v_98 = values_97[0];
var values_99 = R.Join(new V[] {}, new Results((V)(v_98 + v_92)));
var obj_100 = v_96;
V key_101 = (V)"xp_total";
R.Set(obj_100, key_101, values_99[0]);
R.Call(v_94, R.Join(new V[] {(V)"ExperienceGained", R.Table(new Entry((V)"amount", v_92), new Entry((V)"old_xp", v_98), new Entry((V)"new_xp", R.Get(v_96, (V)"xp_total")), new Entry((V)"target_id", R.Get(v_93, (V)"target_id")), new Entry((V)"target_definition_id", R.Get(v_93, (V)"target_definition_id")), new Entry((V)"floor_id", R.Get(v_93, (V)"floor_id")), new Entry((V)"x", R.Get(v_93, (V)"x")), new Entry((V)"y", R.Get(v_93, (V)"y")))}, new Results(R.Get(v_91, (V)"player_id"))));
var values_102 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results(v_91))));
V v_103 = values_102[0];
while (R.Truth((V)(R.Get(v_96, (V)"xp_total") >= R.Call(R.Get(v_10, (V)"next_threshold"), R.Join(new V[] {}, new Results(R.Get(v_96, (V)"level")))).First))) {
var values_104 = R.Join(new V[] {}, new Results(R.Get(v_96, (V)"level")));
V v_105 = values_104[0];
var values_106 = R.Join(new V[] {}, new Results((V)(v_105 + (V)1.0d)));
var obj_107 = v_96;
V key_108 = (V)"level";
R.Set(obj_107, key_108, values_106[0]);
var values_109 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {}, new Results(v_91))));
V v_110 = values_109[0];
var values_111 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Call(R.Get(v_6, (V)"get_progression"), Results.Empty).First, (V)"growth"), v_110)));
V v_112 = values_111[0];
var values_113 = R.Join(new V[] {}, new Results(R.Get(v_103, (V)"hp")));
V v_114 = values_113[0];
var values_115 = R.Join(new V[] {}, R.Call(v_49, R.Join(new V[] {v_103, v_110}, new Results(v_112))));
V v_116 = values_115[0];
V v_117 = values_115[1];
V v_118 = values_115[2];
R.Call(v_94, R.Join(new V[] {(V)"LevelUp", R.Table(new Entry((V)"old_level", v_105), new Entry((V)"new_level", R.Get(v_96, (V)"level")), new Entry((V)"xp_total", R.Get(v_96, (V)"xp_total")), new Entry((V)"next_threshold", R.Call(R.Get(v_10, (V)"next_threshold"), R.Join(new V[] {}, new Results(R.Get(v_96, (V)"level")))).First), new Entry((V)"stat", v_110), new Entry((V)"target_id", R.Get(v_91, (V)"player_id")))}, new Results(R.Get(v_91, (V)"player_id"))));
var values_119 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"level", R.Get(v_96, (V)"level")), new Entry((V)"stat", v_110), new Entry((V)"amount", v_112), new Entry((V)"old_value", v_116), new Entry((V)"new_value", v_117), new Entry((V)"old_hp", R.Or(R.And((V)(v_110 == (V)"max_health"), () => v_114), () => V.Nil)), new Entry((V)"new_hp", R.Or(R.And((V)(v_110 == (V)"max_health"), () => R.Get(v_103, (V)"hp")), () => V.Nil)), new Entry((V)"target_id", R.Get(v_91, (V)"player_id")))));
V v_120 = values_119[0];
if (R.Truth(v_118)) {
foreach (var row_121 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_118)))))) {
V v_122 = row_121[0];
V v_123 = row_121[1];
var values_124 = R.Join(new V[] {}, new Results(v_123));
var obj_125 = v_120;
V key_126 = v_122;
R.Set(obj_125, key_126, values_124[0]);
}
}
R.Call(v_94, R.Join(new V[] {(V)"StatIncreased", v_120}, new Results(R.Get(v_91, (V)"player_id"))));
}
return Results.Empty;
})));
R.Set(v_10, (V)"on_event", new V((RuleFunction)(args_127 => {
V v_128 = args_127[0];
V v_129 = args_127[1];
V v_130 = args_127[2];
if (R.Truth((V)(R.Get(v_129, (V)"kind") != (V)"EntityKilled"))) {
return Results.Empty;
}
var values_131 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_129, (V)"target_definition_id")))));
V v_132 = values_131[0];
if (R.Truth(R.Or((V)(!R.Truth(v_132)), () => R.Get(R.Get(v_128, (V)"rewarded_kills"), R.Get(v_129, (V)"target_id"))))) {
return Results.Empty;
}
var values_133 = R.Join(new V[] {}, new Results((V)true));
var obj_134 = R.Get(v_128, (V)"rewarded_kills");
V key_135 = R.Get(v_129, (V)"target_id");
R.Set(obj_134, key_135, values_133[0]);
if (R.Truth(R.Get(v_129, (V)"reward_exempt"))) {
return Results.Empty;
}
if (R.Truth(R.And(R.And((V)(R.Get(v_132, (V)"xp_reward") > (V)0.0d), () => (V)(R.Get(v_129, (V)"source_id") == R.Get(v_128, (V)"player_id"))), () => (V)(R.Get(v_129, (V)"source_faction") == (V)"player")))) {
R.Call(R.Get(v_10, (V)"gain_xp"), R.Join(new V[] {v_128, R.Get(v_132, (V)"xp_reward"), v_129}, new Results(v_130)));
}
if (R.Truth((V)(R.Get(v_132, (V)"currency_drop") > (V)0.0d))) {
R.Call(R.Get(v_10, (V)"spawn_currency"), R.Join(new V[] {v_128, R.Get(v_132, (V)"currency_drop"), R.Get(v_129, (V)"floor_id"), R.Get(v_129, (V)"x"), R.Get(v_129, (V)"y"), v_130, R.Get(v_129, (V)"source_id"), V.Nil}, new Results(R.Get(v_129, (V)"target_id"))));
}
if (R.Truth(R.And(R.And(R.Get(v_128, (V)"stage"), () => (V)(R.Get(v_132, (V)"ai") != (V)"hollow_regent")), () => (V)(R.Get(v_132, (V)"ai") != (V)"pursuer")))) {
var values_136 = Results.Empty;
V v_137 = values_136[0];
if (R.Truth(R.Or((V)(R.Get(v_132, (V)"ai") == (V)"warden"), () => (V)(R.Get(v_132, (V)"ai") == (V)"biome_elite")))) {
var values_138 = R.Join(new V[] {}, new Results((V)"health"));
v_137 = values_138[0];
} else {
var values_139 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"derive"), R.Join(new V[] {R.Or(R.Get(v_128, (V)"root_seed"), () => R.Get(v_128, (V)"run_seed"))}, new Results(R.Concat((V)"supply:", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_129, (V)"target_id")))).First)))).First % (V)100.0d)));
V v_140 = values_139[0];
if (R.Truth((V)(v_140 < (V)5.0d))) {
var values_141 = R.Join(new V[] {}, new Results((V)"health"));
v_137 = values_141[0];
} else if (R.Truth((V)(v_140 < (V)27.0d))) {
var values_142 = R.Join(new V[] {}, new Results((V)"food"));
v_137 = values_142[0];
}
}
if (R.Truth(v_137)) {
R.Call(R.Get(v_10, (V)"spawn_supply"), R.Join(new V[] {v_128, v_137, R.Get(v_129, (V)"floor_id"), R.Get(v_129, (V)"x"), R.Get(v_129, (V)"y"), v_130, R.Get(v_129, (V)"source_id")}, new Results(R.Get(v_129, (V)"target_id"))));
}
}
return Results.Empty;
})));
var values_143 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"food", (V)4.0d), new Entry((V)"health", (V)10.0d))));
V v_144 = values_143[0];
R.Set(v_10, (V)"spawn_supply", new V((RuleFunction)(args_145 => {
V v_146 = args_145[0];
V v_147 = args_145[1];
V v_148 = args_145[2];
V v_149 = args_145[3];
V v_150 = args_145[4];
V v_151 = args_145[5];
V v_152 = args_145[6];
V v_153 = args_145[7];
var values_154 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_144, v_147)}, new Results((V)"invalid supply kind"))));
V v_155 = values_154[0];
var values_156 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_146, (V)"next_drop_id")), new Entry((V)"kind", v_147), new Entry((V)"amount", v_155), new Entry((V)"position", R.Table(new Entry((V)"floor_id", v_148), new Entry((V)"x", v_149), new Entry((V)"y", v_150))), new Entry((V)"collected", (V)false), new Entry((V)"killed_entity_id", v_153))));
V v_157 = values_156[0];
var values_158 = R.Join(new V[] {}, new Results((V)(R.Get(v_157, (V)"id") + (V)1.0d)));
var obj_159 = v_146;
V key_160 = (V)"next_drop_id";
R.Set(obj_159, key_160, values_158[0]);
var values_161 = R.Join(new V[] {}, new Results(v_157));
var obj_162 = R.Get(v_146, (V)"drops");
V key_163 = (V)((V)R.Length(R.Get(v_146, (V)"drops")) + (V)1.0d);
R.Set(obj_162, key_163, values_161[0]);
R.Call(v_151, R.Join(new V[] {(V)"SupplyDropped", R.Table(new Entry((V)"drop_id", R.Get(v_157, (V)"id")), new Entry((V)"supply_kind", v_147), new Entry((V)"amount", v_155), new Entry((V)"target_id", v_153), new Entry((V)"floor_id", v_148), new Entry((V)"x", v_149), new Entry((V)"y", v_150))}, new Results(v_152)));
return R.Join(new V[] {}, new Results(v_157));
return Results.Empty;
})));
R.Set(v_10, (V)"spawn_currency", new V((RuleFunction)(args_164 => {
V v_165 = args_164[0];
V v_166 = args_164[1];
V v_167 = args_164[2];
V v_168 = args_164[3];
V v_169 = args_164[4];
V v_170 = args_164[5];
V v_171 = args_164[6];
V v_172 = args_164[7];
V v_173 = args_164[8];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_166))).First == (V)"number"), () => (V)((V)(v_166 % (V)1.0d) == (V)0.0d)), () => (V)(v_166 > (V)0.0d))}, new Results((V)"invalid currency drop")));
var values_174 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_165, (V)"next_drop_id")), new Entry((V)"kind", (V)"currency"), new Entry((V)"amount", v_166), new Entry((V)"position", R.Table(new Entry((V)"floor_id", v_167), new Entry((V)"x", v_168), new Entry((V)"y", v_169))), new Entry((V)"collected", (V)false), new Entry((V)"killed_entity_id", v_173), new Entry((V)"secret_id", v_172))));
V v_175 = values_174[0];
var values_176 = R.Join(new V[] {}, new Results((V)(R.Get(v_175, (V)"id") + (V)1.0d)));
var obj_177 = v_165;
V key_178 = (V)"next_drop_id";
R.Set(obj_177, key_178, values_176[0]);
var values_179 = R.Join(new V[] {}, new Results(v_175));
var obj_180 = R.Get(v_165, (V)"drops");
V key_181 = (V)((V)R.Length(R.Get(v_165, (V)"drops")) + (V)1.0d);
R.Set(obj_180, key_181, values_179[0]);
R.Call(v_170, R.Join(new V[] {(V)"CurrencyDropped", R.Table(new Entry((V)"drop_id", R.Get(v_175, (V)"id")), new Entry((V)"amount", v_166), new Entry((V)"target_id", v_173), new Entry((V)"secret_id", v_172), new Entry((V)"floor_id", v_167), new Entry((V)"x", v_168), new Entry((V)"y", v_169))}, new Results(v_171)));
return R.Join(new V[] {}, new Results(v_175));
return Results.Empty;
})));
R.Set(v_10, (V)"pickup_at", new V((RuleFunction)(args_182 => {
V v_183 = args_182[0];
V v_184 = args_182[1];
V v_185 = args_182[2];
V v_186 = args_182[3];
V v_187 = args_182[4];
foreach (var row_188 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_183, (V)"drops"))))))) {
V v_189 = row_188[0];
V v_190 = row_188[1];
var values_191 = R.Join(new V[] {}, new Results(R.Get(v_190, (V)"position")));
V v_192 = values_191[0];
if (R.Truth(R.And(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_190, (V)"collected"))), () => (V)(!R.Truth(R.Get(v_190, (V)"carried_by")))), () => (V)(R.Get(v_192, (V)"floor_id") == R.Get(R.Get(v_184, (V)"position"), (V)"floor_id"))), () => (V)(R.Get(v_192, (V)"x") == v_185)), () => (V)(R.Get(v_192, (V)"y") == v_186)))) {
if (R.Truth((V)(R.Get(v_190, (V)"kind") == (V)"currency"))) {
var values_193 = R.Join(new V[] {}, new Results(R.Get(v_183, (V)"currency")));
V v_194 = values_193[0];
var values_195 = R.Join(new V[] {}, new Results((V)(v_194 + R.Get(v_190, (V)"amount"))));
var obj_196 = v_183;
V key_197 = (V)"currency";
R.Set(obj_196, key_197, values_195[0]);
var values_198 = R.Join(new V[] {}, new Results((V)true));
var obj_199 = v_190;
V key_200 = (V)"collected";
R.Set(obj_199, key_200, values_198[0]);
R.Call(v_187, R.Join(new V[] {(V)"CurrencyCollected", R.Table(new Entry((V)"drop_id", R.Get(v_190, (V)"id")), new Entry((V)"amount", R.Get(v_190, (V)"amount")), new Entry((V)"old_currency", v_194), new Entry((V)"new_currency", R.Get(v_183, (V)"currency")), new Entry((V)"floor_id", R.Get(v_192, (V)"floor_id")), new Entry((V)"x", v_185), new Entry((V)"y", v_186))}, new Results(R.Get(v_184, (V)"id"))));
} else if (R.Truth(R.And(R.And(R.Get(v_184, (V)"hp"), () => (V)(R.Get(v_184, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_184, (V)"hp") < R.Get(v_184, (V)"max_hp"))))) {
var values_201 = R.Join(new V[] {}, new Results(R.Get(v_184, (V)"hp")));
V v_202 = values_201[0];
var values_203 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_184, (V)"max_hp")}, new Results((V)(R.Get(v_184, (V)"hp") + R.Get(v_190, (V)"amount"))))));
var obj_204 = v_184;
V key_205 = (V)"hp";
R.Set(obj_204, key_205, values_203[0]);
var values_206 = R.Join(new V[] {}, new Results((V)true));
var obj_207 = v_190;
V key_208 = (V)"collected";
R.Set(obj_207, key_208, values_206[0]);
R.Call(v_187, R.Join(new V[] {(V)"SupplyCollected", R.Table(new Entry((V)"drop_id", R.Get(v_190, (V)"id")), new Entry((V)"supply_kind", R.Get(v_190, (V)"kind")), new Entry((V)"amount", R.Get(v_190, (V)"amount")), new Entry((V)"actual_gain", (V)(R.Get(v_184, (V)"hp") - v_202)), new Entry((V)"hp_before", v_202), new Entry((V)"hp_after", R.Get(v_184, (V)"hp")), new Entry((V)"floor_id", R.Get(v_192, (V)"floor_id")), new Entry((V)"x", v_185), new Entry((V)"y", v_186))}, new Results(R.Get(v_184, (V)"id"))));
}
}
}
return Results.Empty;
})));
V v_209 = V.Nil;
v_209 = new V((RuleFunction)(args_210 => {
V v_211 = args_210[0];
V v_212 = args_210[1];
var values_213 = R.Join(new V[] {}, new Results((V)0.0d));
V v_214 = values_213[0];
foreach (var row_215 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_216 = row_215[0];
V v_217 = row_215[1];
var values_218 = R.Join(new V[] {}, new Results((V)(v_214 + R.Get(v_212, v_217))));
v_214 = values_218[0];
}
var values_219 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"next_int"), R.Join(new V[] {v_211}, new Results(v_214))));
V v_220 = values_219[0];
foreach (var row_221 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_222 = row_221[0];
V v_223 = row_221[1];
var values_224 = R.Join(new V[] {}, new Results((V)(v_220 - R.Get(v_212, v_223))));
v_220 = values_224[0];
if (R.Truth((V)(v_220 <= (V)0.0d))) {
return R.Join(new V[] {}, new Results(v_223));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"invalid rarity weights")));
return Results.Empty;
}));
R.Set(v_10, (V)"make_offers", new V((RuleFunction)(args_225 => {
V v_226 = args_225[0];
V v_227 = args_225[1];
V v_228 = args_225[2];
var values_229 = R.Join(new V[] {}, new Results(R.Or(v_228, () => R.Get(v_226, (V)"rng"))));
V v_230 = values_229[0];
var values_231 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_226, (V)"available_boons"), () => R.Call(R.Get(v_2, (V)"starter_order"), Results.Empty).First)));
V v_232 = values_231[0];
var values_233 = R.Join(new V[] {}, new Results(R.Table()));
V v_234 = values_233[0];
var values_235 = Results.Empty;
V v_236 = values_235[0];
foreach (var row_237 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_226, (V)"entities"))))))) {
V v_238 = row_237[0];
V v_239 = row_237[1];
if (R.Truth((V)(R.Get(v_239, (V)"id") == R.Get(v_226, (V)"player_id")))) {
var values_240 = R.Join(new V[] {}, new Results(v_239));
v_236 = values_240[0];

break;
}
}
var values_241 = R.Join(new V[] {}, new Results(R.Or(R.And(v_236, () => R.Get(v_236, (V)"definition_id")), () => (V)"core:actor/adventurer")));
V v_242 = values_241[0];
foreach (var row_243 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_232)))))) {
V v_244 = row_243[0];
V v_245 = row_243[1];
var values_246 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_245))).First, (V)"eligible_classes")));
V v_247 = values_246[0];
var values_248 = R.Join(new V[] {}, new Results((V)(!R.Truth(v_247))));
V v_249 = values_248[0];
foreach (var row_250 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_247, () => R.Table()))))))) {
V v_251 = row_250[0];
V v_252 = row_250[1];
if (R.Truth((V)(v_252 == v_242))) {
var values_253 = R.Join(new V[] {}, new Results((V)true));
v_249 = values_253[0];

break;
}
}
if (R.Truth(v_249)) {
var values_254 = R.Join(new V[] {}, new Results(v_245));
var obj_255 = v_234;
V key_256 = (V)((V)R.Length(v_234) + (V)1.0d);
R.Set(obj_255, key_256, values_254[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_234) >= (V)3.0d)}, new Results((V)"chest pool needs three boons")));
var values_257 = R.Join(new V[] {}, new Results(R.Table()));
V v_258 = values_257[0];
{ double end_261 = R.Num((V)3.0d), step_262 = R.Num((V)1.0d);
for (double i_260 = R.Num((V)1.0d); step_262 > 0 ? i_260 <= end_261 : i_260 >= end_261; i_260 += step_262) {
V v_259 = i_260;
var values_263 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"next_int"), R.Join(new V[] {v_230}, new Results((V)R.Length(v_234)))));
V v_264 = values_263[0];
var values_265 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"boon_id", R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {v_234}, new Results(v_264))).First), new Entry((V)"rarity", R.Call(v_209, R.Join(new V[] {v_230}, new Results(R.Get(v_227, (V)"rarity_weights")))).First))));
var obj_266 = v_258;
V key_267 = v_259;
R.Set(obj_266, key_267, values_265[0]);
}
}
return R.Join(new V[] {}, new Results(v_258));
return Results.Empty;
})));
R.Set(v_10, (V)"validate_chest", new V((RuleFunction)(args_268 => {
V v_269 = args_268[0];
var values_270 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_6, (V)"get_chest"), R.Join(new V[] {}, new Results(R.Get(v_269, (V)"definition_id")))).First}, new Results((V)"unknown chest definition"))));
V v_271 = values_270[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Get(v_269, (V)"controller") == (V)"chest"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_269, (V)"claimed")))).First == (V)"boolean")), () => (V)(R.Get(v_269, (V)"price") == R.Get(v_271, (V)"price"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_269, (V)"offers")))).First == (V)"table")), () => (V)((V)R.Length(R.Get(v_269, (V)"offers")) == (V)3.0d))}, new Results((V)"invalid chest state")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_269, (V)"blocks_movement") == (V)(!R.Truth(R.Get(v_269, (V)"claimed"))))}, new Results((V)"invalid chest collision")));
var values_272 = R.Join(new V[] {}, new Results(R.Table()));
V v_273 = values_272[0];
foreach (var row_274 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_269, (V)"offers"))))))) {
V v_275 = row_274[0];
V v_276 = row_274[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_276, (V)"boon_id")))).First, () => (V)(!R.Truth(R.Get(v_273, R.Get(v_276, (V)"boon_id")))))}, new Results((V)"invalid chest offer")));
var values_277 = R.Join(new V[] {}, new Results((V)true));
var obj_278 = v_273;
V key_279 = R.Get(v_276, (V)"boon_id");
R.Set(obj_278, key_279, values_277[0]);
var values_280 = R.Join(new V[] {}, new Results((V)false));
V v_281 = values_280[0];
foreach (var row_282 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"rarities"))))))) {
V v_283 = row_282[0];
V v_284 = row_282[1];
if (R.Truth((V)(R.Get(v_276, (V)"rarity") == v_284))) {
var values_285 = R.Join(new V[] {}, new Results((V)true));
v_281 = values_285[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_281}, new Results((V)"invalid chest offer rarity")));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_10, (V)"chest_options", new V((RuleFunction)(args_286 => {
V v_287 = args_286[0];
V v_288 = args_286[1];
R.Call(R.Get(v_10, (V)"validate_chest"), R.Join(new V[] {}, new Results(v_288)));
if (R.Truth(R.Get(v_288, (V)"claimed"))) {
return R.Join(new V[] {V.Nil}, new Results((V)"chest already claimed"));
}
var values_289 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results(v_287))));
V v_290 = values_289[0];
var values_291 = R.Join(new V[] {R.Get(v_290, (V)"position")}, new Results(R.Get(v_288, (V)"position")));
V v_292 = values_291[0];
V v_293 = values_291[1];
if (R.Truth(R.Or((V)(R.Get(v_292, (V)"floor_id") != R.Get(v_293, (V)"floor_id")), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_292, (V)"x") - R.Get(v_293, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_292, (V)"y") - R.Get(v_293, (V)"y"))))).First) != (V)1.0d)))) {
return R.Join(new V[] {V.Nil}, new Results((V)"chest is not adjacent"));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"chest_id", R.Get(v_288, (V)"id")), new Entry((V)"name", R.Get(R.Call(R.Get(v_6, (V)"get_chest"), R.Join(new V[] {}, new Results(R.Get(v_288, (V)"definition_id")))).First, (V)"name")), new Entry((V)"price", R.Get(v_288, (V)"price")), new Entry((V)"currency", R.Get(v_287, (V)"currency")), new Entry((V)"shortfall", R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(R.Get(v_288, (V)"price") - R.Get(v_287, (V)"currency"))))).First), new Entry((V)"offers", R.Call(v_11, R.Join(new V[] {}, new Results(R.Get(v_288, (V)"offers")))).First))));
return Results.Empty;
})));
R.Set(v_10, (V)"can_claim", new V((RuleFunction)(args_294 => {
V v_295 = args_294[0];
V v_296 = args_294[1];
V v_297 = args_294[2];
var values_298 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"chest_options"), R.Join(new V[] {v_295}, new Results(v_296))));
V v_299 = values_298[0];
V v_300 = values_298[1];
if (R.Truth((V)(!R.Truth(v_299)))) {
return R.Join(new V[] {(V)false}, new Results(v_300));
}
if (R.Truth(R.Or(R.Or(R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_297))).First != (V)"number"), () => (V)((V)(v_297 % (V)1.0d) != (V)0.0d)), () => (V)(v_297 < (V)1.0d)), () => (V)(v_297 > (V)3.0d)))) {
return R.Join(new V[] {(V)false}, new Results((V)"choose offer 1, 2 or 3"));
}
if (R.Truth((V)(R.Get(v_299, (V)"shortfall") > (V)0.0d))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"need ", R.Concat(R.Get(v_299, (V)"shortfall"), (V)" more currency"))));
}
var values_301 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_299, (V)"offers"), v_297)));
V v_302 = values_301[0];
var values_303 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results(v_295))));
V v_304 = values_303[0];
var values_305 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"boons", R.Call(v_11, R.Join(new V[] {}, new Results(R.Get(v_304, (V)"boons")))).First))));
V v_306 = values_305[0];
var values_307 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(R.Get(v_306, (V)"boons"), R.Get(v_302, (V)"boon_id")), () => R.Get(R.Get(R.Get(v_306, (V)"boons"), R.Get(v_302, (V)"boon_id")), R.Get(v_302, (V)"rarity"))), () => (V)0.0d)));
V v_308 = values_307[0];
var values_309 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_4, (V)"set_stacks"), v_306, R.Get(v_302, (V)"boon_id"), R.Get(v_302, (V)"rarity")}, new Results((V)(v_308 + (V)1.0d)))));
V v_310 = values_309[0];
if (R.Truth((V)(!R.Truth(v_310)))) {
return R.Join(new V[] {(V)false}, new Results((V)"incompatible boon"));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_10, (V)"claim", new V((RuleFunction)(args_311 => {
V v_312 = args_311[0];
V v_313 = args_311[1];
V v_314 = args_311[2];
V v_315 = args_311[3];
var values_316 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"can_claim"), R.Join(new V[] {v_312, v_313}, new Results(v_314))));
V v_317 = values_316[0];
V v_318 = values_316[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_317}, new Results(v_318)));
var values_319 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_313, (V)"offers"), v_314)));
V v_320 = values_319[0];
var values_321 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results(v_312))));
V v_322 = values_321[0];
var values_323 = R.Join(new V[] {}, new Results(R.Get(v_312, (V)"currency")));
V v_324 = values_323[0];
var values_325 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(R.Get(v_322, (V)"boons"), R.Get(v_320, (V)"boon_id")), () => R.Get(R.Get(R.Get(v_322, (V)"boons"), R.Get(v_320, (V)"boon_id")), R.Get(v_320, (V)"rarity"))), () => (V)0.0d)));
V v_326 = values_325[0];
R.Call(R.Get(v_4, (V)"set_stacks"), R.Join(new V[] {v_322, R.Get(v_320, (V)"boon_id"), R.Get(v_320, (V)"rarity")}, new Results((V)(v_326 + (V)1.0d))));
var values_327 = R.Join(new V[] {}, new Results((V)(v_324 - R.Get(v_313, (V)"price"))));
var obj_328 = v_312;
V key_329 = (V)"currency";
R.Set(obj_328, key_329, values_327[0]);
var values_330 = R.Join(new V[] {}, new Results((V)true));
var obj_331 = v_313;
V key_332 = (V)"claimed";
R.Set(obj_331, key_332, values_330[0]);
var values_333 = R.Join(new V[] {}, new Results((V)false));
var obj_334 = v_313;
V key_335 = (V)"blocks_movement";
R.Set(obj_334, key_335, values_333[0]);
R.Call(v_315, R.Join(new V[] {(V)"CurrencySpent", R.Table(new Entry((V)"chest_id", R.Get(v_313, (V)"id")), new Entry((V)"amount", R.Get(v_313, (V)"price")), new Entry((V)"old_currency", v_324), new Entry((V)"new_currency", R.Get(v_312, (V)"currency")))}, new Results(R.Get(v_322, (V)"id"))));
R.Call(v_315, R.Join(new V[] {(V)"BoonGranted", R.Table(new Entry((V)"chest_id", R.Get(v_313, (V)"id")), new Entry((V)"boon_id", R.Get(v_320, (V)"boon_id")), new Entry((V)"rarity", R.Get(v_320, (V)"rarity")), new Entry((V)"old_count", v_326), new Entry((V)"new_count", (V)(v_326 + (V)1.0d)))}, new Results(R.Get(v_322, (V)"id"))));
R.Call(v_315, R.Join(new V[] {(V)"ChestClaimed", R.Table(new Entry((V)"chest_id", R.Get(v_313, (V)"id")), new Entry((V)"boon_id", R.Get(v_320, (V)"boon_id")), new Entry((V)"rarity", R.Get(v_320, (V)"rarity")), new Entry((V)"price", R.Get(v_313, (V)"price")), new Entry((V)"offer_index", v_314), new Entry((V)"floor_id", R.Get(R.Get(v_313, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_313, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_313, (V)"position"), (V)"y")))}, new Results(R.Get(v_322, (V)"id"))));
return R.Join(new V[] {}, new Results(v_320));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_10));
return Results.Empty;
} }
