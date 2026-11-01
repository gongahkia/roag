// Static C# port of src/idea_terrain.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 4b60997168d24dce00d4416f5e231155def738aa2c08502e285b1570458d5501
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_terrain { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"version", (V)1.0d), new Entry((V)"warning_time", (V)100.0d), new Entry((V)"cooldown_time", (V)300.0d), new Entry((V)"heat_damage", (V)2.0d), new Entry((V)"push_cap", (V)4.0d))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"heat_vent", (V)"Heat Vent"), new Entry((V)"push_floor", (V)"Push Floor"), new Entry((V)"fragile_crossing", (V)"Fragile Shortcut"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, new Results(v_14));
var obj_16 = v_10;
V key_17 = (V)"names";
R.Set(obj_16, key_17, values_15[0]);
var values_18 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"heat_vent_cooldown", R.Table(new Entry((V)1, (V)"tiny"), new Entry((V)2, (V)9.0d), new Entry((V)3, (V)565.0d))), new Entry((V)"heat_vent_warning", R.Table(new Entry((V)1, (V)"tiny"), new Entry((V)2, (V)29.0d), new Entry((V)3, (V)565.0d))), new Entry((V)"heat_vent_burst", R.Table(new Entry((V)1, (V)"tiny"), new Entry((V)2, (V)29.0d), new Entry((V)3, (V)565.0d))), new Entry((V)"push_north", R.Table(new Entry((V)1, (V)"onebit"), new Entry((V)2, (V)1003.0d), new Entry((V)3, (V)1003.0d))), new Entry((V)"push_east", R.Table(new Entry((V)1, (V)"onebit"), new Entry((V)2, (V)1004.0d), new Entry((V)3, (V)1004.0d))), new Entry((V)"push_south", R.Table(new Entry((V)1, (V)"onebit"), new Entry((V)2, (V)1005.0d), new Entry((V)3, (V)1005.0d))), new Entry((V)"push_west", R.Table(new Entry((V)1, (V)"onebit"), new Entry((V)2, (V)1006.0d), new Entry((V)3, (V)1006.0d))), new Entry((V)"fragile_intact", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)495.0d), new Entry((V)3, (V)70.0d))), new Entry((V)"fragile_armed", R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)495.0d), new Entry((V)3, (V)600.0d))), new Entry((V)"fragile_closed", R.Table(new Entry((V)1, (V)"caves"), new Entry((V)2, (V)94.0d), new Entry((V)3, (V)600.0d))))));
var obj_19 = v_10;
V key_20 = (V)"art";
R.Set(obj_19, key_20, values_18[0]);
R.Set(v_10, (V)"binding", new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
return R.Join(new V[] {}, new Results(R.Concat((V)"core:terrain/", v_22)));
return Results.Empty;
})));
V v_23 = V.Nil;
v_23 = new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
V v_26 = args_24[1];
return R.Join(new V[] {}, new Results(R.Concat(v_25, R.Concat((V)",", v_26))));
return Results.Empty;
}));
V v_27 = V.Nil;
v_27 = new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
V v_30 = args_28[1];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_29, (V)"x") - R.Get(v_30, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_29, (V)"y") - R.Get(v_30, (V)"y"))))).First)));
return Results.Empty;
}));
V v_31 = V.Nil;
v_31 = new V((RuleFunction)(args_32 => {
V v_33 = args_32[0];
V v_34 = args_32[1];
V v_35 = args_32[2];
foreach (var row_36 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_33, (V)"protected_cells"), () => R.Table()))))))) {
V v_37 = row_36[0];
V v_38 = row_36[1];
if (R.Truth(R.And((V)(R.Get(v_38, (V)"x") == v_34), () => (V)(R.Get(v_38, (V)"y") == v_35)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_33, (V)"corridors"), () => R.Table()))))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
foreach (var row_42 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_41, (V)"cells"))))))) {
V v_43 = row_42[0];
V v_44 = row_42[1];
if (R.Truth(R.And((V)(R.Get(v_44, (V)"x") == v_34), () => (V)(R.Get(v_44, (V)"y") == v_35)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
V v_45 = V.Nil;
v_45 = new V((RuleFunction)(args_46 => {
V v_47 = args_46[0];
V v_48 = args_46[1];
V v_49 = args_46[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_48 >= R.Get(v_47, (V)"x")), () => (V)(v_48 < (V)(R.Get(v_47, (V)"x") + R.Get(v_47, (V)"width")))), () => (V)(v_49 >= R.Get(v_47, (V)"y"))), () => (V)(v_49 < (V)(R.Get(v_47, (V)"y") + R.Get(v_47, (V)"height"))))));
return Results.Empty;
}));
R.Set(v_10, (V)"optional_cell", new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
V v_52 = args_50[1];
V v_53 = args_50[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.Get(R.Get(v_51, (V)"rows"), v_53), () => (V)(R.Invoke(R.Get(R.Get(v_51, (V)"rows"), v_53), "sub", R.Join(new V[] {v_52}, new Results(v_52))).First == (V)".")), () => (V)(!R.Truth(R.Call(v_31, R.Join(new V[] {v_51, v_52}, new Results(v_53))).First)))));
return Results.Empty;
})));
V v_54 = V.Nil;
v_54 = new V((RuleFunction)(args_55 => {
V v_56 = args_55[0];
foreach (var row_57 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_56, (V)"entities"))))))) {
V v_58 = row_57[0];
V v_59 = row_57[1];
if (R.Truth((V)(R.Get(v_59, (V)"id") == R.Get(v_56, (V)"player_id")))) {
return R.Join(new V[] {}, new Results(v_59));
}
}
return Results.Empty;
}));
V v_60 = V.Nil;
v_60 = new V((RuleFunction)(args_61 => {
V v_62 = args_61[0];
V v_63 = args_61[1];
return R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_63 == R.Get(v_62, (V)"active_floor_id")), () => R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_62))).First), () => R.Get(R.Get(v_62, (V)"inactive_floor_maps"), v_63))));
return Results.Empty;
}));
R.Set(v_10, (V)"find", new V((RuleFunction)(args_64 => {
V v_65 = args_64[0];
V v_66 = args_64[1];
foreach (var row_67 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_65, (V)"idea_terrain"), () => R.Get(R.Get(v_65, (V)"idea_terrain"), (V)"nodes")), () => R.Table()))))))) {
V v_68 = row_67[0];
V v_69 = row_67[1];
if (R.Truth((V)(R.Get(v_69, (V)"id") == v_66))) {
return R.Join(new V[] {}, new Results(v_69));
}
}
return Results.Empty;
})));
R.Set(v_10, (V)"at", new V((RuleFunction)(args_70 => {
V v_71 = args_70[0];
V v_72 = args_70[1];
V v_73 = args_70[2];
V v_74 = args_70[3];
foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_71, (V)"idea_terrain"), () => R.Get(R.Get(v_71, (V)"idea_terrain"), (V)"nodes")), () => R.Table()))))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_77, (V)"floor_id") == v_72), () => (V)(R.Get(v_77, (V)"x") == v_73)), () => (V)(R.Get(v_77, (V)"y") == v_74)))) {
return R.Join(new V[] {}, new Results(v_77));
}
}
return Results.Empty;
})));
R.Set(v_10, (V)"closed", new V((RuleFunction)(args_78 => {
V v_79 = args_78[0];
V v_80 = args_78[1];
V v_81 = args_78[2];
V v_82 = args_78[3];
var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"at"), R.Join(new V[] {v_79, v_80, v_81}, new Results(v_82))));
V v_84 = values_83[0];

return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(v_84, () => (V)(R.Get(v_84, (V)"kind") == (V)"fragile_crossing")), () => (V)(R.Get(v_84, (V)"phase") == (V)"closed")), () => (V)false)));
return Results.Empty;
})));
R.Set(v_10, (V)"place", new V((RuleFunction)(args_85 => {
V v_86 = args_85[0];
V v_87 = args_85[1];
V v_88 = args_85[2];
V v_89 = args_85[3];
V v_90 = args_85[4];
V v_91 = args_85[5];
V v_92 = args_85[6];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Get(v_14, v_88), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_87, v_89}, new Results(v_90))).First), () => (V)(!R.Truth(R.Call(R.Get(v_10, (V)"at"), R.Join(new V[] {v_86, R.Get(v_87, (V)"floor_id"), v_89}, new Results(v_90))).First)))}, new Results((V)"invalid special terrain placement")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(v_88 != (V)"fragile_crossing"), () => (V)(v_87 == R.Get(v_86, (V)"generated_map")))}, new Results((V)"fragile crossing requires a concrete generated floor")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(v_88 != (V)"fragile_crossing"), () => (V)(!R.Truth(R.Call(v_31, R.Join(new V[] {v_87, v_89}, new Results(v_90))).First)))}, new Results((V)"protected geometry cannot collapse")));
var values_93 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_86, (V)"idea_terrain"), () => R.Table(new Entry((V)"version", R.Get(v_10, (V)"version")), new Entry((V)"nodes", R.Table())))));
var obj_94 = v_86;
V key_95 = (V)"idea_terrain";
R.Set(obj_94, key_95, values_93[0]);
var values_96 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat(R.Get(v_87, (V)"floor_id"), R.Concat((V)":terrain:", R.Concat(v_88, R.Concat((V)":", R.Call(v_23, R.Join(new V[] {v_89}, new Results(v_90))).First))))), new Entry((V)"kind", v_88), new Entry((V)"floor_id", R.Get(v_87, (V)"floor_id")), new Entry((V)"x", v_89), new Entry((V)"y", v_90), new Entry((V)"phase", R.Or(R.Or(R.And((V)(v_88 == (V)"heat_vent"), () => (V)"cooldown"), () => R.And((V)(v_88 == (V)"push_floor"), () => (V)"ready")), () => (V)"intact")))));
V v_97 = values_96[0];
if (R.Truth((V)(v_88 == (V)"push_floor"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_91))).First == (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_92))).First == (V)"number")), () => (V)((V)(v_91 % (V)1.0d) == (V)0.0d)), () => (V)((V)(v_92 % (V)1.0d) == (V)0.0d)), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_91))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_92))).First) == (V)1.0d))}, new Results((V)"invalid push direction")));
var values_98 = R.Join(new V[] {v_91}, new Results(v_92));
var obj_99 = v_97;
V key_100 = (V)"dx";
var obj_101 = v_97;
V key_102 = (V)"dy";
R.Set(obj_99, key_100, values_98[0]);
R.Set(obj_101, key_102, values_98[1]);
} else if (R.Truth((V)(v_88 == (V)"heat_vent"))) {
var values_103 = R.Join(new V[] {}, new Results(R.Get(v_86, (V)"clock")));
var obj_104 = v_97;
V key_105 = (V)"cooldown_until";
R.Set(obj_104, key_105, values_103[0]);
}
var values_106 = R.Join(new V[] {}, new Results(v_97));
var obj_107 = R.Get(R.Get(v_86, (V)"idea_terrain"), (V)"nodes");
V key_108 = (V)((V)R.Length(R.Get(R.Get(v_86, (V)"idea_terrain"), (V)"nodes")) + (V)1.0d);
R.Set(obj_107, key_108, values_106[0]);

return R.Join(new V[] {}, new Results(v_97));
return Results.Empty;
})));
R.Set(v_10, (V)"alternate_route", new V((RuleFunction)(args_109 => {
V v_110 = args_109[0];
V v_111 = args_109[1];
V v_112 = args_109[2];
V v_113 = args_109[3];
var values_114 = R.Join(new V[] {}, new Results(R.Table()));
V v_115 = values_114[0];
foreach (var row_116 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_110, (V)"entities"))))))) {
V v_117 = row_116[0];
V v_118 = row_116[1];
var values_119 = R.Join(new V[] {}, new Results(R.Get(v_118, (V)"position")));
V v_120 = values_119[0];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_120, (V)"floor_id") == R.Get(v_111, (V)"floor_id")), () => R.Get(v_118, (V)"blocks_movement")), () => (V)(!R.Truth(R.Get(v_118, (V)"hp")))), () => (V)(R.Get(v_118, (V)"controller") != (V)"door")))) {
var values_121 = R.Join(new V[] {}, new Results((V)true));
var obj_122 = v_115;
V key_123 = R.Call(v_23, R.Join(new V[] {R.Get(v_120, (V)"x")}, new Results(R.Get(v_120, (V)"y")))).First;
R.Set(obj_122, key_123, values_121[0]);
}
}
V v_124 = V.Nil;
v_124 = new V((RuleFunction)(args_125 => {
V v_126 = args_125[0];
V v_127 = args_125[1];
var values_128 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_111, (V)"start_x")), new Entry((V)"y", R.Get(v_111, (V)"start_y")))))));
V v_129 = values_128[0];

var values_130 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Call(v_23, R.Join(new V[] {R.Get(v_111, (V)"start_x")}, new Results(R.Get(v_111, (V)"start_y")))).First, (V)true))));
V v_131 = values_130[0];

var values_132 = R.Join(new V[] {}, new Results((V)1.0d));
V v_133 = values_132[0];
while (R.Truth((V)(v_133 <= (V)R.Length(v_129)))) {
var values_134 = R.Join(new V[] {}, new Results(R.Get(v_129, v_133)));
V v_135 = values_134[0];

var values_136 = R.Join(new V[] {}, new Results((V)(v_133 + (V)1.0d)));
v_133 = values_136[0];
foreach (var row_137 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_138 = row_137[0];
V v_139 = row_137[1];
var values_140 = R.Join(new V[] {(V)(R.Get(v_135, (V)"x") + R.Get(v_139, (V)1.0d))}, new Results((V)(R.Get(v_135, (V)"y") + R.Get(v_139, (V)2.0d))));
V v_141 = values_140[0];
V v_142 = values_140[1];

var values_143 = R.Join(new V[] {}, R.Call(v_23, R.Join(new V[] {v_141}, new Results(v_142))));
V v_144 = values_143[0];
if (R.Truth(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_131, v_144))), () => (V)(!R.Truth(R.And(R.And(v_126, () => (V)(v_141 == v_112)), () => (V)(v_142 == v_113))))), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_111, v_141}, new Results(v_142))).First), () => (V)(!R.Truth(R.Get(v_127, v_144)))))) {
var values_145 = R.Join(new V[] {}, new Results((V)true));
var obj_146 = v_131;
V key_147 = v_144;
R.Set(obj_146, key_147, values_145[0]);

var values_148 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_141), new Entry((V)"y", v_142))));
var obj_149 = v_129;
V key_150 = (V)((V)R.Length(v_129) + (V)1.0d);
R.Set(obj_149, key_150, values_148[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_131));
return Results.Empty;
}));
if (R.Truth(R.And((V)(v_112 == R.Get(v_111, (V)"start_x")), () => (V)(v_113 == R.Get(v_111, (V)"start_y"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_151 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table()), new Entry((V)2, v_115)))))))) {
V v_152 = row_151[0];
V v_153 = row_151[1];
var values_154 = R.Join(new V[] {}, R.Call(v_124, R.Join(new V[] {(V)false}, new Results(v_153))));
V v_155 = values_154[0];

if (R.Truth((V)(!R.Truth(R.Get(v_155, R.Call(v_23, R.Join(new V[] {v_112}, new Results(v_113))).First))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_156 = R.Join(new V[] {}, R.Call(v_124, R.Join(new V[] {(V)true}, new Results(v_153))));
V v_157 = values_156[0];
foreach (var row_158 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_155)))))) {
V v_159 = row_158[0];
if (R.Truth(R.And((V)(v_159 != R.Call(v_23, R.Join(new V[] {v_112}, new Results(v_113))).First), () => (V)(!R.Truth(R.Get(v_157, v_159)))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_10, (V)"populate", new V((RuleFunction)(args_160 => {
V v_161 = args_160[0];
V v_162 = args_160[1];
if (R.Truth((V)(!R.Truth(R.Get(v_162, (V)"rooms"))))) {
return Results.Empty;
}
var values_163 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_164 = values_163[0];
V v_165 = values_163[1];
V v_166 = V.Nil;
v_166 = new V((RuleFunction)(args_167 => {
V v_168 = args_167[0];
V v_169 = args_167[1];
var values_170 = R.Join(new V[] {}, new Results((V)true));
var obj_171 = v_164;
V key_172 = R.Call(v_23, R.Join(new V[] {v_168}, new Results(v_169))).First;
R.Set(obj_171, key_172, values_170[0]);
return Results.Empty;
}));
foreach (var row_173 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_161, (V)"entities"))))))) {
V v_174 = row_173[0];
V v_175 = row_173[1];
if (R.Truth((V)(R.Get(R.Get(v_175, (V)"position"), (V)"floor_id") == R.Get(v_162, (V)"floor_id")))) {
R.Call(v_166, R.Join(new V[] {R.Get(R.Get(v_175, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_175, (V)"position"), (V)"y"))));
}
}
foreach (var row_176 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Or(R.Get(v_161, (V)"review_features"), () => R.Table())), new Entry((V)2, R.Or(R.And(R.Get(v_161, (V)"idea_rooms"), () => R.Get(R.Get(v_161, (V)"idea_rooms"), (V)"nodes")), () => R.Table())), new Entry((V)3, R.Or(R.Get(R.Get(v_162, (V)"placements"), (V)"stairs"), () => R.Table())), new Entry((V)4, R.Or(R.Get(R.Get(v_162, (V)"placements"), (V)"secrets"), () => R.Table())), new Entry((V)5, R.Or(R.Get(R.Get(v_162, (V)"placements"), (V)"chests"), () => R.Table())), new Entry((V)6, R.Or(R.Get(R.Get(v_162, (V)"placements"), (V)"doors"), () => R.Table()))))))))) {
V v_177 = row_176[0];
V v_178 = row_176[1];
foreach (var row_179 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_178)))))) {
V v_180 = row_179[0];
V v_181 = row_179[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_181, (V)"floor_id"))), () => (V)(R.Get(v_181, (V)"floor_id") == R.Get(v_162, (V)"floor_id"))))) {
R.Call(v_166, R.Join(new V[] {R.Get(v_181, (V)"x")}, new Results(R.Get(v_181, (V)"y"))));

foreach (var row_182 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_183 = row_182[0];
V v_184 = row_182[1];
R.Call(v_166, R.Join(new V[] {(V)(R.Get(v_181, (V)"x") + R.Get(v_184, (V)1.0d))}, new Results((V)(R.Get(v_181, (V)"y") + R.Get(v_184, (V)2.0d)))));
}
}
}
}
var values_185 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"derive"), R.Join(new V[] {R.Get(v_161, (V)"run_seed")}, new Results(R.Concat(R.Get(v_162, (V)"floor_id"), (V)":optional-terrain")))));
V v_186 = values_185[0];
foreach (var row_187 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_162, (V)"rooms"))))))) {
V v_188 = row_187[0];
V v_189 = row_187[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_189, (V)"role") != (V)"start"), () => (V)(R.Get(v_189, (V)"role") != (V)"elite")), () => (V)(R.Get(v_189, (V)"role") != (V)"exit")))) {
{ double end_192 = R.Num((V)((V)(R.Get(v_189, (V)"y") + R.Get(v_189, (V)"height")) - (V)2.0d)), step_193 = R.Num((V)1.0d);
for (double i_191 = R.Num((V)(R.Get(v_189, (V)"y") + (V)1.0d)); step_193 > 0 ? i_191 <= end_192 : i_191 >= end_192; i_191 += step_193) {
V v_190 = i_191;
{ double end_196 = R.Num((V)((V)(R.Get(v_189, (V)"x") + R.Get(v_189, (V)"width")) - (V)2.0d)), step_197 = R.Num((V)1.0d);
for (double i_195 = R.Num((V)(R.Get(v_189, (V)"x") + (V)1.0d)); step_197 > 0 ? i_195 <= end_196 : i_195 >= end_196; i_195 += step_197) {
V v_194 = i_195;
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_164, R.Call(v_23, R.Join(new V[] {v_194}, new Results(v_190))).First))), () => (V)(R.Invoke(R.Get(R.Get(v_162, (V)"rows"), v_190), "sub", R.Join(new V[] {v_194}, new Results(v_194))).First == (V)".")), () => (V)(R.Call(v_27, R.Join(new V[] {R.Table(new Entry((V)"x", v_194), new Entry((V)"y", v_190))}, new Results(R.Table(new Entry((V)"x", R.Get(v_162, (V)"start_x")), new Entry((V)"y", R.Get(v_162, (V)"start_y")))))).First >= (V)6.0d)))) {
var values_198 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_194), new Entry((V)"y", v_190), new Entry((V)"room", v_189), new Entry((V)"score", R.Call(R.Get(v_4, (V)"derive"), R.Join(new V[] {v_186}, R.Call(v_23, R.Join(new V[] {v_194}, new Results(v_190))))).First))));
var obj_199 = v_165;
V key_200 = (V)((V)R.Length(v_165) + (V)1.0d);
R.Set(obj_199, key_200, values_198[0]);
}
}
}
}
}
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_165}, new Results(new V((RuleFunction)(args_201 => {
V v_202 = args_201[0];
V v_203 = args_201[1];
if (R.Truth((V)(R.Get(v_202, (V)"score") != R.Get(v_203, (V)"score")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_202, (V)"score") < R.Get(v_203, (V)"score"))));
}

if (R.Truth((V)(R.Get(v_202, (V)"y") != R.Get(v_203, (V)"y")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_202, (V)"y") < R.Get(v_203, (V)"y"))));
}

return R.Join(new V[] {}, new Results((V)(R.Get(v_202, (V)"x") < R.Get(v_203, (V)"x"))));
return Results.Empty;
})))));
var values_204 = R.Join(new V[] {}, new Results(R.Table()));
V v_205 = values_204[0];
V v_206 = V.Nil;
v_206 = new V((RuleFunction)(args_207 => {
V v_208 = args_207[0];
foreach (var row_209 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_165)))))) {
V v_210 = row_209[0];
V v_211 = row_209[1];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_164, R.Call(v_23, R.Join(new V[] {R.Get(v_211, (V)"x")}, new Results(R.Get(v_211, (V)"y")))).First))), () => (V)(!R.Truth(R.Get(v_205, R.Get(R.Get(v_211, (V)"room"), (V)"id"))))), () => R.Call(v_208, R.Join(new V[] {}, new Results(v_211))).First))) {
R.Call(v_166, R.Join(new V[] {R.Get(v_211, (V)"x")}, new Results(R.Get(v_211, (V)"y"))));

var values_212 = R.Join(new V[] {}, new Results((V)true));
var obj_213 = v_205;
V key_214 = R.Get(R.Get(v_211, (V)"room"), (V)"id");
R.Set(obj_213, key_214, values_212[0]);

return R.Join(new V[] {}, new Results(v_211));
}
}
return Results.Empty;
}));
var values_216 = R.Join(new V[] {}, R.Call(v_206, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_215 => {

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))))));
V v_217 = values_216[0];
if (R.Truth(v_217)) {
R.Call(R.Get(v_10, (V)"place"), R.Join(new V[] {v_161, v_162, (V)"heat_vent", R.Get(v_217, (V)"x")}, new Results(R.Get(v_217, (V)"y"))));
}
var values_242 = R.Join(new V[] {}, R.Call(v_206, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_218 => {
V v_219 = args_218[0];
foreach (var row_220 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_221 = row_220[0];
V v_222 = row_220[1];
var values_223 = R.Join(new V[] {(V)(R.Get(v_219, (V)"x") + R.Get(v_222, (V)1.0d))}, new Results((V)(R.Get(v_219, (V)"y") + R.Get(v_222, (V)2.0d))));
V v_224 = values_223[0];
V v_225 = values_223[1];
if (R.Truth(R.And(R.And(R.Call(v_45, R.Join(new V[] {R.Get(v_219, (V)"room"), v_224}, new Results(v_225))).First, () => (V)(R.Invoke(R.Get(R.Get(v_162, (V)"rows"), v_225), "sub", R.Join(new V[] {v_224}, new Results(v_224))).First == (V)".")), () => (V)(!R.Truth(R.Get(v_164, R.Call(v_23, R.Join(new V[] {v_224}, new Results(v_225))).First)))))) {
var values_226 = R.Join(new V[] {}, new Results((V)true));
V v_227 = values_226[0];
{ double end_230 = R.Num((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_219, (V)"y")}, new Results(v_225))).First + (V)1.0d)), step_231 = R.Num((V)1.0d);
for (double i_229 = R.Num((V)(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_219, (V)"y")}, new Results(v_225))).First - (V)1.0d)); step_231 > 0 ? i_229 <= end_230 : i_229 >= end_230; i_229 += step_231) {
V v_228 = i_229;
{ double end_234 = R.Num((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Get(v_219, (V)"x")}, new Results(v_224))).First + (V)1.0d)), step_235 = R.Num((V)1.0d);
for (double i_233 = R.Num((V)(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_219, (V)"x")}, new Results(v_224))).First - (V)1.0d)); step_235 > 0 ? i_233 <= end_234 : i_233 >= end_234; i_233 += step_235) {
V v_232 = i_233;
if (R.Truth(R.Or(R.Or((V)(!R.Truth(R.Call(v_45, R.Join(new V[] {R.Get(v_219, (V)"room"), v_232}, new Results(v_228))).First)), () => (V)(R.Invoke(R.Get(R.Get(v_162, (V)"rows"), v_228), "sub", R.Join(new V[] {v_232}, new Results(v_232))).First != (V)".")), () => R.Get(v_164, R.Call(v_23, R.Join(new V[] {v_232}, new Results(v_228))).First)))) {
var values_236 = R.Join(new V[] {}, new Results((V)false));
v_227 = values_236[0];
}
}
}
}
}
if (R.Truth(R.And(v_227, () => R.Call(R.Get(v_10, (V)"alternate_route"), R.Join(new V[] {v_161, v_162, R.Get(v_219, (V)"x")}, new Results(R.Get(v_219, (V)"y")))).First))) {
var values_237 = R.Join(new V[] {R.Get(v_222, (V)1.0d)}, new Results(R.Get(v_222, (V)2.0d)));
var obj_238 = v_219;
V key_239 = (V)"dx";
var obj_240 = v_219;
V key_241 = (V)"dy";
R.Set(obj_238, key_239, values_237[0]);
R.Set(obj_240, key_241, values_237[1]);

return R.Join(new V[] {}, new Results((V)true));
}
}
}
return Results.Empty;
}))))));
V v_243 = values_242[0];
if (R.Truth(v_243)) {
R.Call(R.Get(v_10, (V)"place"), R.Join(new V[] {v_161, v_162, (V)"push_floor", R.Get(v_243, (V)"x"), R.Get(v_243, (V)"y"), R.Get(v_243, (V)"dx")}, new Results(R.Get(v_243, (V)"dy"))));
}
var values_244 = R.Join(new V[] {}, new Results(R.Table()));
V v_245 = values_244[0];
foreach (var row_246 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_162, (V)"editable_cells"), () => R.Table()))))))) {
V v_247 = row_246[0];
V v_248 = row_246[1];
if (R.Truth(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_164, R.Call(v_23, R.Join(new V[] {R.Get(v_248, (V)"x")}, new Results(R.Get(v_248, (V)"y")))).First))), () => (V)(!R.Truth(R.Call(v_31, R.Join(new V[] {v_162, R.Get(v_248, (V)"x")}, new Results(R.Get(v_248, (V)"y")))).First))), () => (V)(R.Invoke(R.Get(R.Get(v_162, (V)"rows"), R.Get(v_248, (V)"y")), "sub", R.Join(new V[] {R.Get(v_248, (V)"x")}, new Results(R.Get(v_248, (V)"x")))).First == (V)".")), () => (V)(R.Call(v_27, R.Join(new V[] {v_248}, new Results(R.Table(new Entry((V)"x", R.Get(v_162, (V)"start_x")), new Entry((V)"y", R.Get(v_162, (V)"start_y")))))).First >= (V)6.0d)))) {
var values_249 = R.Join(new V[] {}, new Results((V)false));
V v_250 = values_249[0];
foreach (var row_251 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_162, (V)"rooms"))))))) {
V v_252 = row_251[0];
V v_253 = row_251[1];
if (R.Truth(R.And(R.Call(v_45, R.Join(new V[] {v_253, R.Get(v_248, (V)"x")}, new Results(R.Get(v_248, (V)"y")))).First, () => R.Or(R.Or((V)(R.Get(v_253, (V)"role") == (V)"start"), () => (V)(R.Get(v_253, (V)"role") == (V)"elite")), () => (V)(R.Get(v_253, (V)"role") == (V)"exit"))))) {
var values_254 = R.Join(new V[] {}, new Results((V)true));
v_250 = values_254[0];
}
}
if (R.Truth((V)(!R.Truth(v_250)))) {
var values_255 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_248, (V)"x")), new Entry((V)"y", R.Get(v_248, (V)"y")), new Entry((V)"score", R.Call(R.Get(v_4, (V)"derive"), R.Join(new V[] {v_186}, new Results(R.Concat((V)"shortcut:", R.Call(v_23, R.Join(new V[] {R.Get(v_248, (V)"x")}, new Results(R.Get(v_248, (V)"y")))).First)))).First))));
var obj_256 = v_245;
V key_257 = (V)((V)R.Length(v_245) + (V)1.0d);
R.Set(obj_256, key_257, values_255[0]);
}
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_245}, new Results(new V((RuleFunction)(args_258 => {
V v_259 = args_258[0];
V v_260 = args_258[1];
if (R.Truth((V)(R.Get(v_259, (V)"score") != R.Get(v_260, (V)"score")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_259, (V)"score") < R.Get(v_260, (V)"score"))));
}

if (R.Truth((V)(R.Get(v_259, (V)"y") != R.Get(v_260, (V)"y")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_259, (V)"y") < R.Get(v_260, (V)"y"))));
}

return R.Join(new V[] {}, new Results((V)(R.Get(v_259, (V)"x") < R.Get(v_260, (V)"x"))));
return Results.Empty;
})))));
foreach (var row_261 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_245)))))) {
V v_262 = row_261[0];
V v_263 = row_261[1];
if (R.Truth((V)(v_262 > (V)24.0d))) {
break;
}
if (R.Truth(R.Call(R.Get(v_10, (V)"alternate_route"), R.Join(new V[] {v_161, v_162, R.Get(v_263, (V)"x")}, new Results(R.Get(v_263, (V)"y")))).First)) {
R.Call(R.Get(v_10, (V)"place"), R.Join(new V[] {v_161, v_162, (V)"fragile_crossing", R.Get(v_263, (V)"x")}, new Results(R.Get(v_263, (V)"y"))));

break;
}
}
return Results.Empty;
})));
R.Set(v_10, (V)"on_event", new V((RuleFunction)(args_264 => {
V v_265 = args_264[0];
V v_266 = args_264[1];
V v_267 = args_264[2];
V v_268 = args_264[3];
V v_269 = args_264[4];
if (R.Truth((V)(R.Get(v_266, (V)"kind") != (V)"TileEntered"))) {
return Results.Empty;
}
var values_270 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"at"), R.Join(new V[] {v_265, R.Get(v_266, (V)"floor_id"), R.Get(v_266, (V)"x")}, new Results(R.Get(v_266, (V)"y")))));
V v_271 = values_270[0];

if (R.Truth((V)(!R.Truth(v_271)))) {
return Results.Empty;
}
var values_272 = Results.Empty;
V v_273 = values_272[0];

foreach (var row_274 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_265, (V)"entities"))))))) {
V v_275 = row_274[0];
V v_276 = row_274[1];
if (R.Truth((V)(R.Get(v_276, (V)"id") == R.Get(v_266, (V)"source_id")))) {
var values_277 = R.Join(new V[] {}, new Results(v_276));
v_273 = values_277[0];

break;
}
}
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_273)), () => (V)(!R.Truth(R.Get(v_273, (V)"hp")))), () => (V)(R.Get(v_273, (V)"hp") <= (V)0.0d)))) {
return Results.Empty;
}
if (R.Truth(R.And((V)(R.Get(v_271, (V)"kind") == (V)"fragile_crossing"), () => (V)(R.Get(v_271, (V)"phase") == (V)"intact")))) {
var values_278 = R.Join(new V[] {}, new Results((V)"armed"));
var obj_279 = v_271;
V key_280 = (V)"phase";
R.Set(obj_279, key_280, values_278[0]);

R.Call(v_268, R.Join(new V[] {(V)"FragileArmed", R.Table(new Entry((V)"floor_id", R.Get(v_271, (V)"floor_id")), new Entry((V)"x", R.Get(v_271, (V)"x")), new Entry((V)"y", R.Get(v_271, (V)"y")), new Entry((V)"target_id", R.Get(v_273, (V)"id")))}, new Results(R.Get(v_273, (V)"id"))));
} else if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_271, (V)"kind") == (V)"push_floor"), () => (V)(R.Get(R.Get(v_273, (V)"position"), (V)"floor_id") == R.Get(v_271, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_273, (V)"position"), (V)"x") == R.Get(v_271, (V)"x"))), () => (V)(R.Get(R.Get(v_273, (V)"position"), (V)"y") == R.Get(v_271, (V)"y"))))) {
var values_281 = R.Join(new V[] {}, new Results(R.Concat((V)"push:", R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_266, (V)"action_id")))).First, R.Concat((V)":", R.Get(v_273, (V)"id"))))));
V v_282 = values_281[0];

var values_283 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_267, v_282), () => (V)0.0d)));
V v_284 = values_283[0];
if (R.Truth((V)(v_284 >= R.Get(v_10, (V)"push_cap")))) {
R.Call(v_268, R.Join(new V[] {(V)"PushFloorBlocked", R.Table(new Entry((V)"floor_id", R.Get(v_271, (V)"floor_id")), new Entry((V)"x", R.Get(v_271, (V)"x")), new Entry((V)"y", R.Get(v_271, (V)"y")), new Entry((V)"target_id", R.Get(v_273, (V)"id")), new Entry((V)"reason", (V)"chain limit"))}, new Results((V)0.0d)));

return Results.Empty;
}
var values_285 = R.Join(new V[] {}, new Results((V)(v_284 + (V)1.0d)));
var obj_286 = v_267;
V key_287 = v_282;
R.Set(obj_286, key_287, values_285[0]);
var values_288 = R.Join(new V[] {}, R.Call(v_269, R.Join(new V[] {v_273, R.Get(v_271, (V)"dx")}, new Results(R.Get(v_271, (V)"dy")))));
V v_289 = values_288[0];
V v_290 = values_288[1];
R.Call(v_268, R.Join(new V[] {R.Or(R.And(v_289, () => (V)"PushFloorTriggered"), () => (V)"PushFloorBlocked"), R.Table(new Entry((V)"floor_id", R.Get(v_271, (V)"floor_id")), new Entry((V)"x", R.Get(v_271, (V)"x")), new Entry((V)"y", R.Get(v_271, (V)"y")), new Entry((V)"target_id", R.Get(v_273, (V)"id")), new Entry((V)"reason", v_290))}, new Results((V)0.0d)));
}
return Results.Empty;
})));
R.Set(v_10, (V)"after_action", new V((RuleFunction)(args_291 => {
V v_292 = args_291[0];
V v_293 = args_291[1];
V v_294 = args_291[2];
foreach (var row_295 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_292, (V)"idea_terrain"), () => R.Get(R.Get(v_292, (V)"idea_terrain"), (V)"nodes")), () => R.Table()))))))) {
V v_296 = row_295[0];
V v_297 = row_295[1];
if (R.Truth((V)(R.Get(v_297, (V)"floor_id") == R.Get(v_292, (V)"active_floor_id")))) {
if (R.Truth(R.And(R.And(R.And(R.And((V)(R.Get(v_297, (V)"kind") == (V)"heat_vent"), () => (V)(R.Get(v_297, (V)"phase") == (V)"cooldown")), () => (V)(R.Get(v_292, (V)"clock") >= R.Get(v_297, (V)"cooldown_until"))), () => (V)(!R.Truth(R.Get(v_292, (V)"game_over")))), () => R.Call(R.Get(v_8, (V)"can_see"), R.Join(new V[] {v_292, R.Get(v_297, (V)"floor_id"), R.Get(v_297, (V)"x")}, new Results(R.Get(v_297, (V)"y")))).First))) {
var values_298 = R.Join(new V[] {}, new Results((V)"warning"));
var obj_299 = v_297;
V key_300 = (V)"phase";
R.Set(obj_299, key_300, values_298[0]);

var values_301 = R.Join(new V[] {}, new Results((V)(R.Get(v_292, (V)"clock") + R.Get(v_10, (V)"warning_time"))));
var obj_302 = v_297;
V key_303 = (V)"due";
R.Set(obj_302, key_303, values_301[0]);

var values_304 = R.Join(new V[] {}, new Results(v_293));
var obj_305 = v_297;
V key_306 = (V)"action_id";
R.Set(obj_305, key_306, values_304[0]);
var values_307 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"enqueue"), R.Join(new V[] {v_292}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", R.Get(v_297, (V)"due")), new Entry((V)"actor_id", (V)0.0d), new Entry((V)"attack_id", v_293), new Entry((V)"floor_id", R.Get(v_297, (V)"floor_id")))))));
V v_308 = values_307[0];
var values_309 = R.Join(new V[] {}, new Results(R.Get(v_308, (V)"id")));
var obj_310 = v_308;
V key_311 = (V)"effect_id";
R.Set(obj_310, key_311, values_309[0]);

var values_312 = R.Join(new V[] {}, new Results(R.Get(v_297, (V)"id")));
var obj_313 = v_308;
V key_314 = (V)"idea_terrain";
R.Set(obj_313, key_314, values_312[0]);

var values_315 = R.Join(new V[] {}, new Results(R.Get(v_308, (V)"id")));
var obj_316 = v_297;
V key_317 = (V)"schedule_id";
R.Set(obj_316, key_317, values_315[0]);
R.Call(v_294, R.Join(new V[] {(V)"HeatVentWarned", R.Table(new Entry((V)"floor_id", R.Get(v_297, (V)"floor_id")), new Entry((V)"x", R.Get(v_297, (V)"x")), new Entry((V)"y", R.Get(v_297, (V)"y")), new Entry((V)"due", R.Get(v_297, (V)"due")), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(v_297, (V)"floor_id")), new Entry((V)"x", R.Get(v_297, (V)"x")), new Entry((V)"y", R.Get(v_297, (V)"y")))))))}, new Results((V)0.0d)));
} else if (R.Truth(R.And((V)(R.Get(v_297, (V)"kind") == (V)"fragile_crossing"), () => (V)(R.Get(v_297, (V)"phase") == (V)"armed")))) {
var values_318 = R.Join(new V[] {}, new Results((V)false));
V v_319 = values_318[0];
V v_320 = V.Nil;
v_320 = new V((RuleFunction)(args_321 => {
V v_322 = args_321[0];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_322, () => (V)(R.Get(v_322, (V)"floor_id") == R.Get(v_297, (V)"floor_id"))), () => (V)(R.Get(v_322, (V)"x") == R.Get(v_297, (V)"x"))), () => (V)(R.Get(v_322, (V)"y") == R.Get(v_297, (V)"y")))));
return Results.Empty;
}));
foreach (var row_323 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_292, (V)"entities"))))))) {
V v_324 = row_323[0];
V v_325 = row_323[1];
var values_326 = R.Join(new V[] {}, new Results(R.Get(v_325, (V)"position")));
V v_327 = values_326[0];
if (R.Truth(R.And(R.Call(v_320, R.Join(new V[] {}, new Results(v_327))).First, () => R.Or(R.Or(R.Get(v_325, (V)"hp"), () => R.Get(v_325, (V)"blocks_movement")), () => (V)(R.Get(v_325, (V)"controller") == (V)"idea_companion"))))) {
var values_328 = R.Join(new V[] {}, new Results((V)true));
v_319 = values_328[0];

break;
}
}
foreach (var row_329 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_292, (V)"drops"))))))) {
V v_330 = row_329[0];
V v_331 = row_329[1];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_331, (V)"collected"))), () => (V)(!R.Truth(R.Get(v_331, (V)"carried_by")))), () => R.Call(v_320, R.Join(new V[] {}, new Results(R.Get(v_331, (V)"position")))).First))) {
var values_332 = R.Join(new V[] {}, new Results((V)true));
v_319 = values_332[0];
}
}
foreach (var row_333 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_292, (V)"idea_corpses"), () => R.Table()))))))) {
V v_334 = row_333[0];
V v_335 = row_333[1];
if (R.Truth(R.Call(v_320, R.Join(new V[] {}, new Results(v_335))).First)) {
var values_336 = R.Join(new V[] {}, new Results((V)true));
v_319 = values_336[0];
}
}
foreach (var row_337 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_292, (V)"enemy_snares"), () => R.Table()))))))) {
V v_338 = row_337[0];
V v_339 = row_337[1];
if (R.Truth(R.Call(v_320, R.Join(new V[] {}, new Results(v_339))).First)) {
var values_340 = R.Join(new V[] {}, new Results((V)true));
v_319 = values_340[0];
}
}
var values_341 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_292, (V)"class_special"), () => R.Table())));
V v_342 = values_341[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Call(v_320, R.Join(new V[] {}, new Results(R.Get(v_342, (V)"beacon")))).First, () => R.Call(v_320, R.Join(new V[] {}, new Results(R.Get(v_342, (V)"decoy")))).First), () => R.Call(v_320, R.Join(new V[] {}, new Results(R.Get(v_342, (V)"snare")))).First), () => R.Call(v_320, R.Join(new V[] {}, new Results(R.And(R.Get(R.Call(v_54, R.Join(new V[] {}, new Results(v_292))).First, (V)"utility"), () => R.Get(R.Get(R.Call(v_54, R.Join(new V[] {}, new Results(v_292))).First, (V)"utility"), (V)"planted"))))).First))) {
var values_343 = R.Join(new V[] {}, new Results((V)true));
v_319 = values_343[0];
}
if (R.Truth((V)(!R.Truth(v_319)))) {
var values_344 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {v_292}, new Results(R.Get(v_297, (V)"floor_id")))));
V v_345 = values_344[0];

var values_346 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_345, (V)"rows"), R.Get(v_297, (V)"y")), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_297, (V)"x") - (V)1.0d)))).First, R.Concat((V)"~", R.Invoke(R.Get(R.Get(v_345, (V)"rows"), R.Get(v_297, (V)"y")), "sub", R.Join(new V[] {}, new Results((V)(R.Get(v_297, (V)"x") + (V)1.0d)))).First))));
var obj_347 = R.Get(v_345, (V)"rows");
V key_348 = R.Get(v_297, (V)"y");
R.Set(obj_347, key_348, values_346[0]);
if (R.Truth(R.Get(v_345, (V)"studio_visuals"))) {
var values_349 = R.Join(new V[] {}, new Results(V.Nil));
var obj_350 = R.Get(v_345, (V)"studio_visuals");
V key_351 = R.Call(v_23, R.Join(new V[] {R.Get(v_297, (V)"x")}, new Results(R.Get(v_297, (V)"y")))).First;
R.Set(obj_350, key_351, values_349[0]);
}
var values_352 = R.Join(new V[] {}, new Results((V)"closed"));
var obj_353 = v_297;
V key_354 = (V)"phase";
R.Set(obj_353, key_354, values_352[0]);

R.Call(v_294, R.Join(new V[] {(V)"FragileCollapsed", R.Table(new Entry((V)"floor_id", R.Get(v_297, (V)"floor_id")), new Entry((V)"x", R.Get(v_297, (V)"x")), new Entry((V)"y", R.Get(v_297, (V)"y")))}, new Results((V)0.0d)));
}
}
}
}
return Results.Empty;
})));
R.Set(v_10, (V)"resolve", new V((RuleFunction)(args_355 => {
V v_356 = args_355[0];
V v_357 = args_355[1];
V v_358 = args_355[2];
V v_359 = args_355[3];
var values_360 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_10, (V)"find"), R.Join(new V[] {v_356}, new Results(R.Get(v_357, (V)"idea_terrain")))).First}, new Results((V)"orphan heat vent"))));
V v_361 = values_360[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Get(v_361, (V)"phase") == (V)"warning"), () => (V)(R.Get(v_361, (V)"schedule_id") == R.Get(v_357, (V)"id"))), () => (V)(R.Get(v_361, (V)"due") == R.Get(v_356, (V)"clock"))), () => (V)(R.Get(v_361, (V)"floor_id") == R.Get(v_356, (V)"active_floor_id")))}, new Results((V)"mistimed heat vent")));
var values_362 = R.Join(new V[] {}, new Results((V)"cooldown"));
var obj_363 = v_361;
V key_364 = (V)"phase";
R.Set(obj_363, key_364, values_362[0]);

var values_365 = R.Join(new V[] {}, new Results((V)(R.Get(v_356, (V)"clock") + R.Get(v_10, (V)"cooldown_time"))));
var obj_366 = v_361;
V key_367 = (V)"cooldown_until";
R.Set(obj_366, key_367, values_365[0]);

var values_368 = R.Join(new V[] {}, new Results(V.Nil));
var obj_369 = v_361;
V key_370 = (V)"schedule_id";
R.Set(obj_369, key_370, values_368[0]);

var values_371 = R.Join(new V[] {}, new Results(V.Nil));
var obj_372 = v_361;
V key_373 = (V)"due";
R.Set(obj_372, key_373, values_371[0]);
if (R.Truth(R.Call(R.Get(v_8, (V)"can_see"), R.Join(new V[] {v_356, R.Get(v_361, (V)"floor_id"), R.Get(v_361, (V)"x")}, new Results(R.Get(v_361, (V)"y")))).First)) {
R.Call(v_358, R.Join(new V[] {(V)"HeatVentBurst", R.Table(new Entry((V)"floor_id", R.Get(v_361, (V)"floor_id")), new Entry((V)"x", R.Get(v_361, (V)"x")), new Entry((V)"y", R.Get(v_361, (V)"y")), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"floor_id", R.Get(v_361, (V)"floor_id")), new Entry((V)"x", R.Get(v_361, (V)"x")), new Entry((V)"y", R.Get(v_361, (V)"y"))))))), (V)0.0d}, new Results(R.Get(v_361, (V)"action_id"))));
var values_374 = R.Join(new V[] {}, new Results(R.Table()));
V v_375 = values_374[0];
foreach (var row_376 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_356, (V)"entities"))))))) {
V v_377 = row_376[0];
V v_378 = row_376[1];
var values_379 = R.Join(new V[] {}, new Results(R.Get(v_378, (V)"position")));
V v_380 = values_379[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.Get(v_378, (V)"hp"), () => (V)(R.Get(v_378, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_380, (V)"floor_id") == R.Get(v_361, (V)"floor_id"))), () => (V)(R.Get(v_380, (V)"x") == R.Get(v_361, (V)"x"))), () => (V)(R.Get(v_380, (V)"y") == R.Get(v_361, (V)"y"))))) {
var values_381 = R.Join(new V[] {}, new Results(v_378));
var obj_382 = v_375;
V key_383 = (V)((V)R.Length(v_375) + (V)1.0d);
R.Set(obj_382, key_383, values_381[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_375}, new Results(new V((RuleFunction)(args_384 => {
V v_385 = args_384[0];
V v_386 = args_384[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_385, (V)"id") < R.Get(v_386, (V)"id"))));
return Results.Empty;
})))));
foreach (var row_387 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_375)))))) {
V v_388 = row_387[0];
V v_389 = row_387[1];
R.Call(v_359, R.Join(new V[] {v_389, R.Get(v_361, (V)"action_id")}, new Results(R.Get(v_10, (V)"heat_damage"))));
}
}
var values_390 = R.Join(new V[] {}, new Results(V.Nil));
var obj_391 = v_361;
V key_392 = (V)"action_id";
R.Set(obj_391, key_392, values_390[0]);
return Results.Empty;
})));
R.Set(v_10, (V)"rebase", new V((RuleFunction)(args_393 => {
V v_394 = args_393[0];
V v_395 = args_393[1];
V v_396 = args_393[2];
foreach (var row_397 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_394, (V)"idea_terrain"), () => R.Get(R.Get(v_394, (V)"idea_terrain"), (V)"nodes")), () => R.Table()))))))) {
V v_398 = row_397[0];
V v_399 = row_397[1];
if (R.Truth(R.And((V)(R.Get(v_399, (V)"floor_id") == v_395), () => (V)(R.Get(v_399, (V)"kind") == (V)"heat_vent")))) {
var values_400 = R.Join(new V[] {}, new Results((V)(R.Get(v_399, (V)"cooldown_until") + v_396)));
var obj_401 = v_399;
V key_402 = (V)"cooldown_until";
R.Set(obj_401, key_402, values_400[0]);

if (R.Truth(R.Get(v_399, (V)"due"))) {
var values_403 = R.Join(new V[] {}, new Results((V)(R.Get(v_399, (V)"due") + v_396)));
var obj_404 = v_399;
V key_405 = (V)"due";
R.Set(obj_404, key_405, values_403[0]);
}
}
}
return Results.Empty;
})));
R.Set(v_10, (V)"visual", new V((RuleFunction)(args_406 => {
V v_407 = args_406[0];
if (R.Truth((V)(R.Get(v_407, (V)"kind") == (V)"heat_vent"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"binding"), R.Join(new V[] {}, new Results(R.Concat((V)"heat_vent_", R.Get(v_407, (V)"phase"))))));
}
if (R.Truth((V)(R.Get(v_407, (V)"kind") == (V)"fragile_crossing"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"binding"), R.Join(new V[] {}, new Results(R.Concat((V)"fragile_", R.Get(v_407, (V)"phase"))))));
}
return R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"binding"), R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.And((V)(R.Get(v_407, (V)"dx") == (V)1.0d), () => (V)"push_east"), () => R.And((V)(R.Get(v_407, (V)"dx") == (-(V)1.0d)), () => (V)"push_west")), () => R.And((V)(R.Get(v_407, (V)"dy") == (V)1.0d), () => (V)"push_south")), () => (V)"push_north")))));
return Results.Empty;
})));
R.Set(v_10, (V)"options", new V((RuleFunction)(args_408 => {
V v_409 = args_408[0];
V v_410 = args_408[1];
var values_411 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"find"), R.Join(new V[] {v_409}, new Results(v_410))));
V v_412 = values_411[0];

if (R.Truth((V)(!R.Truth(v_412)))) {
return Results.Empty;
}
var values_413 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_54, R.Join(new V[] {}, new Results(v_409))).First, (V)"position")));
V v_414 = values_413[0];
if (R.Truth(R.Or(R.Or((V)(R.Get(v_414, (V)"floor_id") != R.Get(v_412, (V)"floor_id")), () => (V)(R.Call(v_27, R.Join(new V[] {v_414}, new Results(v_412))).First > (V)1.0d)), () => (V)(!R.Truth(R.Call(R.Get(v_8, (V)"can_see"), R.Join(new V[] {v_409, R.Get(v_412, (V)"floor_id"), R.Get(v_412, (V)"x")}, new Results(R.Get(v_412, (V)"y")))).First))))) {
return R.Join(new V[] {V.Nil}, new Results((V)"stand beside visible terrain"));
}
var values_415 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(R.Get(v_412, (V)"kind") == (V)"heat_vent"), () => R.Concat((V)"Phase: ", R.Concat(R.Get(v_412, (V)"phase"), R.Concat(R.Or(R.And(R.Get(v_412, (V)"due"), () => R.Concat((V)"; impact at ", R.Get(v_412, (V)"due"))), () => (V)"; warns only while visible"), (V)". Base 2 fire damage. Warning lasts 100 world time, cooldown 300. No guaranteed reaction action.")))), () => R.And((V)(R.Get(v_412, (V)"kind") == (V)"push_floor"), () => (V)"Ending a move on this arrow pushes one legal tile. Walls, creatures, exits and stairs stop it. Maximum four pushes per actor/action; displacement interrupts windups.")), () => (V)"One-use shortcut. Crossing arms it; it becomes a gap only after everyone leaves. An alternate route remains. No collapse damage; no refill on Continue.")));
V v_416 = values_415[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"node_id", v_410), new Entry((V)"title", R.Get(v_14, R.Get(v_412, (V)"kind"))), new Entry((V)"detail", v_416), new Entry((V)"entries", R.Table()))));
return Results.Empty;
})));
R.Set(v_10, (V)"nearby", new V((RuleFunction)(args_417 => {
V v_418 = args_417[0];
var values_419 = R.Join(new V[] {}, new Results(R.Table()));
V v_420 = values_419[0];

var values_421 = R.Join(new V[] {}, new Results(R.Get(R.Call(v_54, R.Join(new V[] {}, new Results(v_418))).First, (V)"position")));
V v_422 = values_421[0];
foreach (var row_423 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_418, (V)"idea_terrain"), () => R.Get(R.Get(v_418, (V)"idea_terrain"), (V)"nodes")), () => R.Table()))))))) {
V v_424 = row_423[0];
V v_425 = row_423[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_425, (V)"floor_id") == R.Get(v_422, (V)"floor_id")), () => (V)(R.Call(v_27, R.Join(new V[] {v_422}, new Results(v_425))).First <= (V)1.0d)), () => R.Call(R.Get(v_8, (V)"can_see"), R.Join(new V[] {v_418, R.Get(v_425, (V)"floor_id"), R.Get(v_425, (V)"x")}, new Results(R.Get(v_425, (V)"y")))).First))) {
var values_426 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"node_id", R.Get(v_425, (V)"id")), new Entry((V)"title", R.Concat(R.Get(v_14, R.Get(v_425, (V)"kind")), R.Concat((V)" \u00c2\u00b7 ", R.Get(v_425, (V)"phase")))), new Entry((V)"x", R.Get(v_425, (V)"x")), new Entry((V)"y", R.Get(v_425, (V)"y")), new Entry((V)"spent", (V)(R.Get(v_425, (V)"phase") == (V)"closed")))));
var obj_427 = v_420;
V key_428 = (V)((V)R.Length(v_420) + (V)1.0d);
R.Set(obj_427, key_428, values_426[0]);
}
}
return R.Join(new V[] {}, new Results(v_420));
return Results.Empty;
})));
V v_429 = V.Nil;
v_429 = new V((RuleFunction)(args_430 => {
V v_431 = args_430[0];
V v_432 = args_430[1];
V v_433 = args_430[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_431))).First == (V)"number"), () => (V)((V)(v_431 % (V)1.0d) == (V)0.0d)), () => (V)(v_431 >= v_432)), () => (V)(v_431 <= v_433))));
return Results.Empty;
}));
R.Set(v_10, (V)"validate", new V((RuleFunction)(args_434 => {
V v_435 = args_434[0];
V v_436 = args_434[1];
var values_437 = R.Join(new V[] {}, new Results(R.Get(v_435, (V)"idea_terrain")));
V v_438 = values_437[0];
if (R.Truth((V)(!R.Truth(v_438)))) {
foreach (var row_439 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_435, (V)"schedule"))))))) {
V v_440 = row_439[0];
V v_441 = row_439[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_441, (V)"idea_terrain")))}, new Results((V)"terrain schedule without store")));
}
foreach (var row_442 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_435, (V)"floor_pause"), () => R.Table()))))))) {
V v_443 = row_442[0];
V v_444 = row_442[1];
foreach (var row_445 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_444, (V)"schedule"))))))) {
V v_446 = row_445[0];
V v_447 = row_445[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_447, (V)"idea_terrain")))}, new Results((V)"paused terrain schedule without store")));
}
}
return R.Join(new V[] {}, new Results((V)true));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_438))).First == (V)"table"), () => (V)(R.Get(v_438, (V)"version") == R.Get(v_10, (V)"version"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_438, (V)"nodes")))).First == (V)"table")), () => (V)((V)R.Length(R.Get(v_438, (V)"nodes")) <= (V)24.0d))}, new Results((V)"invalid optional terrain store")));
var values_448 = R.Join(new V[] {R.Table(), R.Table()}, new Results(R.Table()));
V v_449 = values_448[0];
V v_450 = values_448[1];
V v_451 = values_448[2];
foreach (var row_452 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_438, (V)"nodes"))))))) {
V v_453 = row_452[0];
V v_454 = row_452[1];
var values_455 = R.Join(new V[] {}, new Results(R.Get(v_436, R.Get(v_454, (V)"floor_id"))));
V v_456 = values_455[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(v_456, () => R.Get(v_14, R.Get(v_454, (V)"kind"))), () => R.Call(v_429, R.Join(new V[] {R.Get(v_454, (V)"x"), (V)1.0d}, new Results(R.Get(v_456, (V)"width")))).First), () => R.Call(v_429, R.Join(new V[] {R.Get(v_454, (V)"y"), (V)1.0d}, new Results(R.Get(v_456, (V)"height")))).First), () => (V)(R.Get(v_454, (V)"id") == R.Concat(R.Get(v_454, (V)"floor_id"), R.Concat((V)":terrain:", R.Concat(R.Get(v_454, (V)"kind"), R.Concat((V)":", R.Call(v_23, R.Join(new V[] {R.Get(v_454, (V)"x")}, new Results(R.Get(v_454, (V)"y")))).First)))))), () => (V)(!R.Truth(R.Get(v_449, R.Get(v_454, (V)"id")))))}, new Results((V)"invalid optional terrain identity")));
var values_457 = R.Join(new V[] {}, new Results((V)true));
var obj_458 = v_449;
V key_459 = R.Get(v_454, (V)"id");
R.Set(obj_458, key_459, values_457[0]);

var values_460 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_454, (V)"floor_id"), R.Concat((V)":", R.Call(v_23, R.Join(new V[] {R.Get(v_454, (V)"x")}, new Results(R.Get(v_454, (V)"y")))).First))));
V v_461 = values_460[0];

R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_450, v_461)))}, new Results((V)"overlapping optional terrain")));

var values_462 = R.Join(new V[] {}, new Results((V)true));
var obj_463 = v_450;
V key_464 = v_461;
R.Set(obj_463, key_464, values_462[0]);
var values_465 = R.Join(new V[] {}, R.Invoke(R.Get(R.Get(v_456, (V)"rows"), R.Get(v_454, (V)"y")), "sub", R.Join(new V[] {R.Get(v_454, (V)"x")}, new Results(R.Get(v_454, (V)"x")))));
V v_466 = values_465[0];
if (R.Truth((V)(R.Get(v_454, (V)"kind") == (V)"heat_vent"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(v_466 == (V)"."), () => R.Or((V)(R.Get(v_454, (V)"phase") == (V)"cooldown"), () => (V)(R.Get(v_454, (V)"phase") == (V)"warning"))), () => R.Call(v_429, R.Join(new V[] {R.Get(v_454, (V)"cooldown_until"), (V)0.0d}, new Results((V)4503599627370496.0d))).First)}, new Results((V)"invalid heat vent")));
if (R.Truth((V)(R.Get(v_454, (V)"phase") == (V)"warning"))) {
var values_467 = R.Join(new V[] {}, new Results(R.And(R.Get(v_435, (V)"floor_pause"), () => R.Get(R.Get(v_435, (V)"floor_pause"), R.Get(v_454, (V)"floor_id")))));
V v_468 = values_467[0];

var values_469 = R.Join(new V[] {}, new Results(R.Or(R.And(v_468, () => R.Get(v_468, (V)"suspended_at")), () => R.Get(v_435, (V)"clock"))));
V v_470 = values_469[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Call(v_429, R.Join(new V[] {R.Get(v_454, (V)"due"), v_470}, new Results((V)(v_470 + R.Get(v_10, (V)"warning_time"))))).First, () => R.Call(v_429, R.Join(new V[] {R.Get(v_454, (V)"schedule_id"), (V)1.0d}, new Results((V)(R.Get(v_435, (V)"next_schedule_id") - (V)1.0d)))).First), () => R.Call(v_429, R.Join(new V[] {R.Get(v_454, (V)"action_id"), (V)1.0d}, new Results((V)(R.Get(v_435, (V)"next_action_id") - (V)1.0d)))).First)}, new Results((V)"invalid heat warning")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_451, R.Get(v_454, (V)"schedule_id"))))}, new Results((V)"duplicate heat work")));

var values_471 = R.Join(new V[] {}, new Results(v_454));
var obj_472 = v_451;
V key_473 = R.Get(v_454, (V)"schedule_id");
R.Set(obj_472, key_473, values_471[0]);
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_454, (V)"due") == V.Nil), () => (V)(R.Get(v_454, (V)"schedule_id") == V.Nil)), () => (V)(R.Get(v_454, (V)"action_id") == V.Nil))}, new Results((V)"unexpected heat work")));
}
} else if (R.Truth((V)(R.Get(v_454, (V)"kind") == (V)"push_floor"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(v_466 == (V)"."), () => (V)(R.Get(v_454, (V)"phase") == (V)"ready")), () => R.Call(v_429, R.Join(new V[] {R.Get(v_454, (V)"dx"), (-(V)1.0d)}, new Results((V)1.0d))).First), () => R.Call(v_429, R.Join(new V[] {R.Get(v_454, (V)"dy"), (-(V)1.0d)}, new Results((V)1.0d))).First), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_454, (V)"dx")))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_454, (V)"dy")))).First) == (V)1.0d))}, new Results((V)"invalid push floor")));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(!R.Truth(R.Call(v_31, R.Join(new V[] {v_456, R.Get(v_454, (V)"x")}, new Results(R.Get(v_454, (V)"y")))).First)), () => R.Or(R.And(R.Or((V)(R.Get(v_454, (V)"phase") == (V)"intact"), () => (V)(R.Get(v_454, (V)"phase") == (V)"armed")), () => (V)(v_466 == (V)".")), () => R.And((V)(R.Get(v_454, (V)"phase") == (V)"closed"), () => (V)(v_466 == (V)"~"))))}, new Results((V)"invalid fragile crossing")));
}
}
var values_474 = R.Join(new V[] {}, new Results(R.Table()));
V v_475 = values_474[0];

foreach (var row_476 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_435, (V)"schedule"))))))) {
V v_477 = row_476[0];
V v_478 = row_476[1];
var values_479 = R.Join(new V[] {}, new Results(v_478));
var obj_480 = v_475;
V key_481 = (V)((V)R.Length(v_475) + (V)1.0d);
R.Set(obj_480, key_481, values_479[0]);
}
foreach (var row_482 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_435, (V)"floor_pause"), () => R.Table()))))))) {
V v_483 = row_482[0];
V v_484 = row_482[1];
foreach (var row_485 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_484, (V)"schedule"))))))) {
V v_486 = row_485[0];
V v_487 = row_485[1];
var values_488 = R.Join(new V[] {}, new Results(v_487));
var obj_489 = v_475;
V key_490 = (V)((V)R.Length(v_475) + (V)1.0d);
R.Set(obj_489, key_490, values_488[0]);
}
}
foreach (var row_491 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_475)))))) {
V v_492 = row_491[0];
V v_493 = row_491[1];
if (R.Truth(R.Get(v_493, (V)"idea_terrain"))) {
var values_494 = R.Join(new V[] {}, new Results(R.Get(v_451, R.Get(v_493, (V)"id"))));
V v_495 = values_494[0];

R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_495, () => (V)(R.Get(v_495, (V)"id") == R.Get(v_493, (V)"idea_terrain"))), () => (V)(R.Get(v_493, (V)"kind") == (V)"effect")), () => (V)(R.Get(v_493, (V)"actor_id") == (V)0.0d)), () => (V)(R.Get(v_493, (V)"effect_id") == R.Get(v_493, (V)"id"))), () => (V)(R.Get(v_493, (V)"due") == R.Get(v_495, (V)"due"))), () => (V)(R.Get(v_493, (V)"attack_id") == R.Get(v_495, (V)"action_id"))), () => (V)(R.Get(v_493, (V)"floor_id") == R.Get(v_495, (V)"floor_id")))}, new Results((V)"orphan heat schedule")));
var values_496 = R.Join(new V[] {}, new Results(V.Nil));
var obj_497 = v_451;
V key_498 = R.Get(v_493, (V)"id");
R.Set(obj_497, key_498, values_496[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(v_451))).First))}, new Results((V)"missing heat schedule")));

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_10));
return Results.Empty;
} }
