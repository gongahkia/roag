// Static C# port of src/floors.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 1e24a6c696b971a8cdc56aa43d5e5ea6ba638ed5818914d5f3aee3df2ba45e0d
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_floors { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table()));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_8 = values_7[0];
R.Set(v_6, (V)"map", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
if (R.Truth((V)(v_11 == R.Get(v_10, (V)"active_floor_id")))) {
return R.Join(new V[] {}, new Results(R.Get(v_10, (V)"generated_map")));
}
return R.Join(new V[] {}, new Results(R.And(R.Get(v_10, (V)"inactive_floor_maps"), () => R.Get(R.Get(v_10, (V)"inactive_floor_maps"), v_11))));
return Results.Empty;
})));
R.Set(v_6, (V)"portal", new V((RuleFunction)(args_12 => {
V v_13 = args_12[0];
V v_14 = args_12[1];
V v_15 = args_12[2];
var values_16 = R.Join(new V[] {}, new Results(R.Get(v_13, (V)"generated_map")));
V v_17 = values_16[0];
foreach (var row_18 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.And(v_17, () => R.Get(v_17, (V)"placements")), () => R.Get(R.Get(v_17, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_19 = row_18[0];
V v_20 = row_18[1];
if (R.Truth(R.And((V)(R.Get(v_20, (V)"x") == v_14), () => (V)(R.Get(v_20, (V)"y") == v_15)))) {
return R.Join(new V[] {}, new Results(v_20));
}
}
return Results.Empty;
})));
V v_21 = V.Nil;
v_21 = new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
V v_24 = args_22[1];
V v_25 = args_22[2];
V v_26 = args_22[3];
V v_27 = args_22[4];
foreach (var row_28 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"entities"))))))) {
V v_29 = row_28[0];
V v_30 = row_28[1];
var values_31 = R.Join(new V[] {}, new Results(R.Get(v_30, (V)"position")));
V v_32 = values_31[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_30, (V)"id") != v_27), () => R.Get(v_30, (V)"blocks_movement")), () => v_32), () => (V)(R.Get(v_32, (V)"floor_id") == v_24)), () => (V)(R.Get(v_32, (V)"x") == v_25)), () => (V)(R.Get(v_32, (V)"y") == v_26)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
R.Set(v_6, (V)"landing", new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
V v_35 = args_33[1];
var values_36 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"map"), R.Join(new V[] {v_34}, new Results(R.Get(v_35, (V)"to_floor_id")))));
V v_37 = values_36[0];
if (R.Truth((V)(!R.Truth(v_37)))) {
return R.Join(new V[] {V.Nil}, new Results((V)"stair destination missing"));
}
var values_38 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_35, (V)"to_x")), new Entry((V)"y", R.Get(v_35, (V)"to_y")), new Entry((V)"distance", (V)0.0d))))));
V v_39 = values_38[0];
var values_40 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Concat(R.Get(v_35, (V)"to_x"), R.Concat((V)",", R.Get(v_35, (V)"to_y"))), (V)true))));
V v_41 = values_40[0];
var values_42 = R.Join(new V[] {}, new Results((V)1.0d));
V v_43 = values_42[0];
while (R.Truth((V)(v_43 <= (V)R.Length(v_39)))) {
var values_44 = R.Join(new V[] {}, new Results(R.Get(v_39, v_43)));
V v_45 = values_44[0];
if (R.Truth(R.And(R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_37, R.Get(v_45, (V)"x")}, new Results(R.Get(v_45, (V)"y")))).First, () => (V)(!R.Truth(R.Call(v_21, R.Join(new V[] {v_34, R.Get(v_37, (V)"floor_id"), R.Get(v_45, (V)"x"), R.Get(v_45, (V)"y")}, new Results(R.Get(v_34, (V)"player_id")))).First))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_37, (V)"floor_id")), new Entry((V)"x", R.Get(v_45, (V)"x")), new Entry((V)"y", R.Get(v_45, (V)"y")))));
}
if (R.Truth((V)(R.Get(v_45, (V)"distance") < (V)3.0d))) {
foreach (var row_46 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_8)))))) {
V v_47 = row_46[0];
V v_48 = row_46[1];
var values_49 = R.Join(new V[] {(V)(R.Get(v_45, (V)"x") + R.Get(v_48, (V)1.0d))}, new Results((V)(R.Get(v_45, (V)"y") + R.Get(v_48, (V)2.0d))));
V v_50 = values_49[0];
V v_51 = values_49[1];
var values_52 = R.Join(new V[] {}, new Results(R.Concat(v_50, R.Concat((V)",", v_51))));
V v_53 = values_52[0];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_41, v_53))), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_37, v_50}, new Results(v_51))).First), () => (V)(!R.Truth(R.Call(v_21, R.Join(new V[] {v_34, R.Get(v_37, (V)"floor_id"), v_50, v_51}, new Results(R.Get(v_34, (V)"player_id")))).First))))) {
var values_54 = R.Join(new V[] {}, new Results((V)true));
var obj_55 = v_41;
V key_56 = v_53;
R.Set(obj_55, key_56, values_54[0]);
var values_57 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_50), new Entry((V)"y", v_51), new Entry((V)"distance", (V)(R.Get(v_45, (V)"distance") + (V)1.0d)))));
var obj_58 = v_39;
V key_59 = (V)((V)R.Length(v_39) + (V)1.0d);
R.Set(obj_58, key_59, values_57[0]);
}
}
}
var values_60 = R.Join(new V[] {}, new Results((V)(v_43 + (V)1.0d)));
v_43 = values_60[0];
}
return R.Join(new V[] {V.Nil}, new Results((V)"stair landing blocked"));
return Results.Empty;
})));
R.Set(v_6, (V)"park", new V((RuleFunction)(args_61 => {
V v_62 = args_61[0];
V v_63 = args_61[1];
var values_64 = R.Join(new V[] {}, new Results(R.Table()));
V v_65 = values_64[0];
{ double end_68 = R.Num((V)1.0d), step_69 = R.Num((-(V)1.0d));
for (double i_67 = R.Num((V)R.Length(R.Get(v_62, (V)"schedule"))); step_69 > 0 ? i_67 <= end_68 : i_67 >= end_68; i_67 += step_69) {
V v_66 = i_67;
var values_70 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_62, (V)"schedule"), v_66)));
V v_71 = values_70[0];
if (R.Truth((V)(R.Get(v_71, (V)"floor_id") == v_63))) {
R.Call(R.Get(rt.Global("table"), (V)"insert"), R.Join(new V[] {v_65, (V)1.0d}, R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_62, (V)"schedule")}, new Results(v_66)))));
}
}
}
var values_72 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_62, (V)"floor_pause"), () => R.Table())));
var obj_73 = v_62;
V key_74 = (V)"floor_pause";
R.Set(obj_73, key_74, values_72[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(R.Get(v_62, (V)"floor_pause"), v_63)))}, new Results((V)"floor already paused")));
var values_75 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"suspended_at", R.Get(v_62, (V)"clock")), new Entry((V)"schedule", v_65))));
var obj_76 = R.Get(v_62, (V)"floor_pause");
V key_77 = v_63;
R.Set(obj_76, key_77, values_75[0]);
return Results.Empty;
})));
R.Set(v_6, (V)"resume", new V((RuleFunction)(args_78 => {
V v_79 = args_78[0];
V v_80 = args_78[1];
var values_81 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_79, (V)"floor_pause"), () => R.Get(R.Get(v_79, (V)"floor_pause"), v_80))}, new Results((V)"floor was not paused"))));
V v_82 = values_81[0];
var values_83 = R.Join(new V[] {}, new Results((V)(R.Get(v_79, (V)"clock") - R.Get(v_82, (V)"suspended_at"))));
V v_84 = values_83[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_84 >= (V)0.0d)}, new Results((V)"stage clock moved backwards")));
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_terrain"))).First, (V)"rebase"), R.Join(new V[] {v_79, v_80}, new Results(v_84)));
foreach (var row_85 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_82, (V)"schedule"))))))) {
V v_86 = row_85[0];
V v_87 = row_85[1];
var values_88 = R.Join(new V[] {}, new Results((V)(R.Get(v_87, (V)"due") + v_84)));
var obj_89 = v_87;
V key_90 = (V)"due";
R.Set(obj_89, key_90, values_88[0]);
var values_91 = R.Join(new V[] {}, new Results(v_87));
var obj_92 = R.Get(v_79, (V)"schedule");
V key_93 = (V)((V)R.Length(R.Get(v_79, (V)"schedule")) + (V)1.0d);
R.Set(obj_92, key_93, values_91[0]);
}
var values_94 = R.Join(new V[] {}, new Results(R.And(R.Get(v_79, (V)"class_special"), () => R.Get(R.Get(v_79, (V)"class_special"), (V)"storm"))));
V v_95 = values_94[0];
if (R.Truth(R.And(v_95, () => (V)(R.Get(v_95, (V)"floor_id") == v_80)))) {
var values_96 = R.Join(new V[] {}, new Results((V)(R.Get(v_95, (V)"due") + v_84)));
var obj_97 = v_95;
V key_98 = (V)"due";
R.Set(obj_97, key_98, values_96[0]);
}
foreach (var row_99 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_79, (V)"idea_rooms"), () => R.Get(R.Get(v_79, (V)"idea_rooms"), (V)"nodes")), () => R.Table()))))))) {
V v_100 = row_99[0];
V v_101 = row_99[1];
if (R.Truth(R.And((V)(R.Get(v_101, (V)"floor_id") == v_80), () => R.Get(v_101, (V)"pending")))) {
var values_102 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_101, (V)"pending"), (V)"due") + v_84)));
var obj_103 = R.Get(v_101, (V)"pending");
V key_104 = (V)"due";
R.Set(obj_103, key_104, values_102[0]);
}
}
foreach (var row_105 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_79, (V)"idea_embers"), () => R.Table()))))))) {
V v_106 = row_105[0];
V v_107 = row_105[1];
if (R.Truth((V)(R.Get(v_107, (V)"floor_id") == v_80))) {
var values_108 = R.Join(new V[] {}, new Results((V)(R.Get(v_107, (V)"next_due") + v_84)));
var obj_109 = v_107;
V key_110 = (V)"next_due";
R.Set(obj_109, key_110, values_108[0]);

var values_111 = R.Join(new V[] {}, new Results((V)(R.Get(v_107, (V)"expires") + v_84)));
var obj_112 = v_107;
V key_113 = (V)"expires";
R.Set(obj_112, key_113, values_111[0]);
}
}
foreach (var row_114 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_79, (V)"idea_silt"), () => R.Table()))))))) {
V v_115 = row_114[0];
V v_116 = row_114[1];
if (R.Truth((V)(R.Get(v_116, (V)"floor_id") == v_80))) {
var values_117 = R.Join(new V[] {}, new Results((V)(R.Get(v_116, (V)"expires") + v_84)));
var obj_118 = v_116;
V key_119 = (V)"expires";
R.Set(obj_118, key_119, values_117[0]);
}
}
foreach (var row_120 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_79, (V)"idea_mirrors"), () => R.Table()))))))) {
V v_121 = row_120[0];
V v_122 = row_120[1];
if (R.Truth((V)(R.Get(v_122, (V)"floor_id") == v_80))) {
var values_123 = R.Join(new V[] {}, new Results((V)(R.Get(v_122, (V)"expires") + v_84)));
var obj_124 = v_122;
V key_125 = (V)"expires";
R.Set(obj_124, key_125, values_123[0]);
}
}
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_79, (V)"idea_webs"), () => R.Table()))))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
if (R.Truth((V)(R.Get(v_128, (V)"floor_id") == v_80))) {
var values_129 = R.Join(new V[] {}, new Results((V)(R.Get(v_128, (V)"expires") + v_84)));
var obj_130 = v_128;
V key_131 = (V)"expires";
R.Set(obj_130, key_131, values_129[0]);
}
}
foreach (var row_132 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_79, (V)"entities"))))))) {
V v_133 = row_132[0];
V v_134 = row_132[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_134, (V)"id") != R.Get(v_79, (V)"player_id")), () => R.Get(v_134, (V)"position")), () => (V)(R.Get(R.Get(v_134, (V)"position"), (V)"floor_id") == v_80)))) {
if (R.Truth(R.Get(v_134, (V)"sentry_due"))) {
var values_135 = R.Join(new V[] {}, new Results((V)(R.Get(v_134, (V)"sentry_due") + v_84)));
var obj_136 = v_134;
V key_137 = (V)"sentry_due";
R.Set(obj_136, key_137, values_135[0]);
}
if (R.Truth((V)(R.Get(v_134, (V)"controller") == (V)"geomancer_block"))) {
var values_138 = R.Join(new V[] {}, new Results((V)(R.Get(v_134, (V)"expires") + v_84)));
var obj_139 = v_134;
V key_140 = (V)"expires";
R.Set(obj_139, key_140, values_138[0]);
}
if (R.Truth(R.Get(v_134, (V)"pending_attack"))) {
var values_141 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_134, (V)"pending_attack"), (V)"due") + v_84)));
var obj_142 = R.Get(v_134, (V)"pending_attack");
V key_143 = (V)"due";
R.Set(obj_142, key_143, values_141[0]);
}
if (R.Truth(R.And(R.Get(v_134, (V)"stunned_until"), () => (V)(R.Get(v_134, (V)"stunned_until") > R.Get(v_82, (V)"suspended_at"))))) {
var values_144 = R.Join(new V[] {}, new Results((V)(R.Get(v_134, (V)"stunned_until") + v_84)));
var obj_145 = v_134;
V key_146 = (V)"stunned_until";
R.Set(obj_145, key_146, values_144[0]);
}
foreach (var row_147 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_134, (V)"statuses"), () => R.Table()))))))) {
V v_148 = row_147[0];
V v_149 = row_147[1];
var values_150 = R.Join(new V[] {}, new Results((V)(R.Get(v_149, (V)"next_due") + v_84)));
var obj_151 = v_149;
V key_152 = (V)"next_due";
R.Set(obj_151, key_152, values_150[0]);
}
}
}
var values_153 = R.Join(new V[] {}, new Results(V.Nil));
var obj_154 = R.Get(v_79, (V)"floor_pause");
V key_155 = v_80;
R.Set(obj_154, key_155, values_153[0]);
R.Call(R.Get(v_4, (V)"sort"), R.Join(new V[] {}, new Results(v_79)));
return Results.Empty;
})));
V v_156 = V.Nil;
v_156 = new V((RuleFunction)(args_157 => {
V v_158 = args_157[0];
V v_159 = args_157[1];
var values_160 = R.Join(new V[] {}, new Results(R.Get(v_158, (V)"generated_map")));
V v_161 = values_160[0];
var values_162 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_159, (V)"x")), new Entry((V)"y", R.Get(v_159, (V)"y")), new Entry((V)"distance", (V)0.0d))))));
V v_163 = values_162[0];
var values_164 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Concat(R.Get(v_159, (V)"x"), R.Concat((V)",", R.Get(v_159, (V)"y"))), (V)true))));
V v_165 = values_164[0];
var values_166 = Results.Empty;
V v_167 = values_166[0];
var values_168 = R.Join(new V[] {}, new Results((V)1.0d));
V v_169 = values_168[0];
while (R.Truth((V)(v_169 <= (V)R.Length(v_163)))) {
var values_170 = R.Join(new V[] {}, new Results(R.Get(v_163, v_169)));
V v_171 = values_170[0];
if (R.Truth(R.And((V)(R.Get(v_171, (V)"distance") > (V)0.0d), () => (V)(!R.Truth(R.Call(v_21, R.Join(new V[] {v_158, R.Get(v_161, (V)"floor_id"), R.Get(v_171, (V)"x"), R.Get(v_171, (V)"y")}, new Results(V.Nil))).First))))) {
var values_172 = R.Join(new V[] {}, new Results(v_171));
v_167 = values_172[0];
if (R.Truth((V)(R.Get(v_171, (V)"distance") >= (V)5.0d))) {
return R.Join(new V[] {}, new Results(v_171));
}
}
foreach (var row_173 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_8)))))) {
V v_174 = row_173[0];
V v_175 = row_173[1];
var values_176 = R.Join(new V[] {(V)(R.Get(v_171, (V)"x") + R.Get(v_175, (V)1.0d))}, new Results((V)(R.Get(v_171, (V)"y") + R.Get(v_175, (V)2.0d))));
V v_177 = values_176[0];
V v_178 = values_176[1];
var values_179 = R.Join(new V[] {}, new Results(R.Concat(v_177, R.Concat((V)",", v_178))));
V v_180 = values_179[0];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_165, v_180))), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_161, v_177}, new Results(v_178))).First))) {
var values_181 = R.Join(new V[] {}, new Results((V)true));
var obj_182 = v_165;
V key_183 = v_180;
R.Set(obj_182, key_183, values_181[0]);
var values_184 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_177), new Entry((V)"y", v_178), new Entry((V)"distance", (V)(R.Get(v_171, (V)"distance") + (V)1.0d)))));
var obj_185 = v_163;
V key_186 = (V)((V)R.Length(v_163) + (V)1.0d);
R.Set(obj_185, key_186, values_184[0]);
}
}
var values_187 = R.Join(new V[] {}, new Results((V)(v_169 + (V)1.0d)));
v_169 = values_187[0];
}
return R.Join(new V[] {}, new Results(v_167));
return Results.Empty;
}));
R.Set(v_6, (V)"change", new V((RuleFunction)(args_188 => {
V v_189 = args_188[0];
V v_190 = args_188[1];
V v_191 = args_188[2];
var values_192 = R.Join(new V[] {}, new Results(R.Get(v_189, (V)"active_floor_id")));
V v_193 = values_192[0];
var values_194 = R.Join(new V[] {}, new Results(R.Get(v_190, (V)"to_floor_id")));
V v_195 = values_194[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(v_193 != v_195), () => R.Call(R.Get(v_6, (V)"map"), R.Join(new V[] {v_189}, new Results(v_195))).First)}, new Results((V)"invalid stair link")));
var values_196 = Results.Empty;
V v_197 = values_196[0];
foreach (var row_198 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_189, (V)"entities"))))))) {
V v_199 = row_198[0];
V v_200 = row_198[1];
if (R.Truth((V)(R.Get(v_200, (V)"id") == R.Get(v_189, (V)"player_id")))) {
var values_201 = R.Join(new V[] {}, new Results(v_200));
v_197 = values_201[0];

break;
}
}
foreach (var row_202 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_197, (V)"statuses"), () => R.Table()))))))) {
V v_203 = row_202[0];
V v_204 = row_202[1];
foreach (var row_205 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_189, (V)"schedule"))))))) {
V v_206 = row_205[0];
V v_207 = row_205[1];
if (R.Truth((V)(R.Get(v_207, (V)"id") == R.Get(v_204, (V)"schedule_id")))) {
var values_208 = R.Join(new V[] {}, new Results(v_195));
var obj_209 = v_207;
V key_210 = (V)"floor_id";
R.Set(obj_209, key_210, values_208[0]);

break;
}
}
}
var values_211 = Results.Empty;
V v_212 = values_211[0];
if (R.Truth(R.And(R.Get(v_189, (V)"stage"), () => R.Get(R.Get(v_189, (V)"stage"), (V)"pursuer_id")))) {
foreach (var row_213 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_189, (V)"entities"))))))) {
V v_214 = row_213[0];
V v_215 = row_213[1];
if (R.Truth((V)(R.Get(v_215, (V)"id") == R.Get(R.Get(v_189, (V)"stage"), (V)"pursuer_id")))) {
var values_216 = R.Join(new V[] {}, new Results(v_215));
v_212 = values_216[0];

break;
}
}
}
var values_217 = R.Join(new V[] {}, new Results(R.Or(R.And(v_212, () => R.Get(v_212, (V)"pending_attack")), () => V.Nil)));
V v_218 = values_217[0];
if (R.Truth(v_212)) {
R.Call(R.Get(v_4, (V)"remove_for_actor"), R.Join(new V[] {v_189}, new Results(R.Get(v_212, (V)"id"))));
var values_219 = R.Join(new V[] {}, new Results(V.Nil));
var obj_220 = v_212;
V key_221 = (V)"pending_attack";
R.Set(obj_220, key_221, values_219[0]);
}
R.Call(R.Get(v_6, (V)"park"), R.Join(new V[] {v_189}, new Results(v_193)));
var values_222 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_189, (V)"inactive_floor_maps"), v_195)))));
V v_223 = values_222[0];
var values_224 = R.Join(new V[] {}, new Results(V.Nil));
var obj_225 = R.Get(v_189, (V)"inactive_floor_maps");
V key_226 = v_195;
R.Set(obj_225, key_226, values_224[0]);
var values_227 = R.Join(new V[] {}, new Results(R.Get(v_189, (V)"generated_map")));
var obj_228 = R.Get(v_189, (V)"inactive_floor_maps");
V key_229 = v_193;
R.Set(obj_228, key_229, values_227[0]);
var values_230 = R.Join(new V[] {}, new Results(v_223));
var obj_231 = v_189;
V key_232 = (V)"generated_map";
R.Set(obj_231, key_232, values_230[0]);
var values_233 = R.Join(new V[] {}, new Results(v_195));
var obj_234 = v_189;
V key_235 = (V)"active_floor_id";
R.Set(obj_234, key_235, values_233[0]);
foreach (var row_236 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_189, (V)"floor_order"), () => R.Table()))))))) {
V v_237 = row_236[0];
V v_238 = row_236[1];
if (R.Truth((V)(v_238 == v_195))) {
var values_239 = R.Join(new V[] {}, new Results(v_237));
var obj_240 = v_189;
V key_241 = (V)"active_floor_index";
R.Set(obj_240, key_241, values_239[0]);

break;
}
}
var values_242 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", v_195), new Entry((V)"x", R.Get(v_191, (V)"x")), new Entry((V)"y", R.Get(v_191, (V)"y")))));
var obj_243 = v_197;
V key_244 = (V)"position";
R.Set(obj_243, key_244, values_242[0]);
R.Call(R.Get(v_6, (V)"resume"), R.Join(new V[] {v_189}, new Results(v_195)));
var values_245 = Results.Empty;
V v_246 = values_245[0];
if (R.Truth(v_212)) {
var values_247 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_156, R.Join(new V[] {v_189}, new Results(v_191))).First}, new Results((V)"no pursuer landing on connected floor"))));
V v_248 = values_247[0];
var values_249 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", v_195), new Entry((V)"x", R.Get(v_248, (V)"x")), new Entry((V)"y", R.Get(v_248, (V)"y")))));
var obj_250 = v_212;
V key_251 = (V)"position";
R.Set(obj_250, key_251, values_249[0]);
var values_252 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", v_195), new Entry((V)"x", R.Get(v_248, (V)"x")), new Entry((V)"y", R.Get(v_248, (V)"y")))));
v_246 = values_252[0];
R.Call(R.Get(v_4, (V)"enqueue"), R.Join(new V[] {v_189}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)(R.Get(v_189, (V)"clock") + (V)100.0d)), new Entry((V)"actor_id", R.Get(v_212, (V)"id")), new Entry((V)"is_player", (V)false)))));
}
return R.Join(new V[] {v_246}, new Results(v_218));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
