// Static C# port of tests/visual_palette_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 1efd233097caab4b8939fd2d350d4a092e23f69a5af5a8a0019bf2254fb9935d
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visual_palette_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_palette"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_shader"))));
V v_6 = values_5[0];
V v_7 = V.Nil;
v_7 = new V((RuleFunction)(args_8 => {

var values_9 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"records", R.Table()), new Entry((V)"color", R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d), new Entry((V)4, (V)1.0d))))));
V v_10 = values_9[0];
R.Set(v_10, (V)"newShader", new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(v_12, "find", R.Join(new V[] {(V)"vec3(gray) * ink", (V)1.0d}, new Results((V)true))).First, () => R.Invoke(v_12, "find", R.Join(new V[] {(V)"pixel.a", (V)1.0d}, new Results((V)true))).First))));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"send", new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
V v_15 = args_13[1];
V v_16 = args_13[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_15 == (V)"ink"))));

var values_17 = R.Join(new V[] {}, new Results(v_16));
var obj_18 = v_14;
V key_19 = (V)"ink";
R.Set(obj_18, key_19, values_17[0]);
return Results.Empty;
}))))));
return Results.Empty;
})));
R.Set(v_10, (V)"getShader", new V((RuleFunction)(args_20 => {

return R.Join(new V[] {}, new Results(R.Get(v_10, (V)"shader")));
return Results.Empty;
})));
R.Set(v_10, (V)"setShader", new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
var values_23 = R.Join(new V[] {}, new Results(v_22));
var obj_24 = v_10;
V key_25 = (V)"shader";
R.Set(obj_24, key_25, values_23[0]);
return Results.Empty;
})));
R.Set(v_10, (V)"setColor", new V((RuleFunction)(args_26 => {
V v_27 = args_26[0];
V v_28 = args_26[1];
V v_29 = args_26[2];
V v_30 = args_26[3];
var values_31 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_27))).First == (V)"table"), () => v_27), () => R.Table(new Entry((V)1, v_27), new Entry((V)2, v_28), new Entry((V)3, v_29), new Entry((V)4, R.Or(v_30, () => (V)1.0d))))));
var obj_32 = v_10;
V key_33 = (V)"color";
R.Set(obj_32, key_33, values_31[0]);
return Results.Empty;
})));
V v_34 = V.Nil;
v_34 = new V((RuleFunction)(args_35 => {
V v_36 = args_35[0];
var values_37 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(R.Get(v_10, (V)"color"), (V)1.0d)), new Entry((V)2, R.Get(R.Get(v_10, (V)"color"), (V)2.0d)), new Entry((V)3, R.Get(R.Get(v_10, (V)"color"), (V)3.0d)), new Entry((V)4, R.Or(R.Get(R.Get(v_10, (V)"color"), (V)4.0d), () => (V)1.0d)))));
V v_38 = values_37[0];
if (R.Truth(R.Get(v_10, (V)"shader"))) {
var values_39 = R.Join(new V[] {}, new Results((V)((V)((V)(R.Get(v_38, (V)1.0d) * (V)0.2126d) + (V)(R.Get(v_38, (V)2.0d) * (V)0.7152d)) + (V)(R.Get(v_38, (V)3.0d) * (V)0.0722d))));
V v_40 = values_39[0];
{ double end_43 = R.Num((V)3.0d), step_44 = R.Num((V)1.0d);
for (double i_42 = R.Num((V)1.0d); step_44 > 0 ? i_42 <= end_43 : i_42 >= end_43; i_42 += step_44) {
V v_41 = i_42;
var values_45 = R.Join(new V[] {}, new Results((V)(v_40 * R.Get(R.Get(R.Get(v_10, (V)"shader"), (V)"ink"), v_41))));
var obj_46 = v_38;
V key_47 = v_41;
R.Set(obj_46, key_47, values_45[0]);
}
}
}
var values_48 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_36), new Entry((V)"color", v_38), new Entry((V)"args", R.Table(new Entry(1, R.Tail(args_35, 1)))))));
var obj_49 = R.Get(v_10, (V)"records");
V key_50 = (V)((V)R.Length(R.Get(v_10, (V)"records")) + (V)1.0d);
R.Set(obj_49, key_50, values_48[0]);
return Results.Empty;
}));
R.Set(v_10, (V)"draw", new V((RuleFunction)(args_51 => {

R.Call(v_34, R.Join(new V[] {(V)"sprite"}, R.Tail(args_51, 0)));
return Results.Empty;
})));
R.Set(v_10, (V)"rectangle", new V((RuleFunction)(args_52 => {

R.Call(v_34, R.Join(new V[] {(V)"rect"}, R.Tail(args_52, 0)));
return Results.Empty;
})));
R.Set(v_10, (V)"print", new V((RuleFunction)(args_53 => {

R.Call(v_34, R.Join(new V[] {(V)"text"}, R.Tail(args_53, 0)));
return Results.Empty;
})));
var values_58 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
return R.Join(new V[] {}, new Results((V)((V)R.Length(v_56) * (V)8.0d)));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_57 => {

return R.Join(new V[] {}, new Results((V)16.0d));
return Results.Empty;
}))))));
V v_59 = values_58[0];
R.Set(v_10, (V)"getFont", new V((RuleFunction)(args_60 => {

return R.Join(new V[] {}, new Results(v_59));
return Results.Empty;
})));
foreach (var row_61 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"setFont"), new Entry((V)2, (V)"push"), new Entry((V)3, (V)"pop"), new Entry((V)4, (V)"translate"), new Entry((V)5, (V)"scale"), new Entry((V)6, (V)"ellipse"), new Entry((V)7, (V)"circle"), new Entry((V)8, (V)"line"), new Entry((V)9, (V)"polygon")))))))) {
V v_62 = row_61[0];
V v_63 = row_61[1];
var values_65 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_64 => {


return Results.Empty;
}))));
var obj_66 = v_10;
V key_67 = v_63;
R.Set(obj_66, key_67, values_65[0]);
}
return R.Join(new V[] {v_10}, new Results(v_59));
return Results.Empty;
}));
V v_68 = V.Nil;
v_68 = new V((RuleFunction)(args_69 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(rt.Global("_G"), (V)"love"))))));
var values_70 = R.Join(new V[] {}, R.Call(v_7, Results.Empty));
V v_71 = values_70[0];
V v_72 = values_70[1];

var values_73 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"graphics", v_71))));
V v_74 = values_73[0];
var values_75 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("loadfile"), R.Join(new V[] {}, new Results((V)"main.lua"))))));
V v_76 = values_75[0];
R.Call(rt.Global("setfenv"), R.Join(new V[] {v_76}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"love", v_74))}, new Results(R.Table(new Entry((V)"__index", rt.Global("_G"))))))));

R.Call(v_76, Results.Empty);
V v_77 = V.Nil;
v_77 = new V((RuleFunction)(args_78 => {
V v_79 = args_78[0];
var values_80 = R.Join(new V[] {}, new Results(R.Table()));
V v_81 = values_80[0];
V v_82 = V.Nil;
v_82 = new V((RuleFunction)(args_83 => {
V v_84 = args_83[0];
if (R.Truth(R.Get(v_81, v_84))) {
return Results.Empty;
}

var values_85 = R.Join(new V[] {}, new Results((V)true));
var obj_86 = v_81;
V key_87 = v_84;
R.Set(obj_86, key_87, values_85[0]);
var values_88 = R.Join(new V[] {}, new Results(R.Table()));
V v_89 = values_88[0];
{ double end_92 = R.Num((V)200.0d), step_93 = R.Num((V)1.0d);
for (double i_91 = R.Num((V)1.0d); step_93 > 0 ? i_91 <= end_92 : i_91 >= end_92; i_91 += step_93) {
V v_90 = i_91;
var values_94 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("debug"), (V)"getupvalue"), R.Join(new V[] {v_84}, new Results(v_90))));
V v_95 = values_94[0];
V v_96 = values_94[1];

if (R.Truth((V)(!R.Truth(v_95)))) {
break;
}
R.Call(v_79, R.Join(new V[] {v_84, v_90, v_95}, new Results(v_96)));
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_96))).First == (V)"function"))) {
var values_97 = R.Join(new V[] {}, new Results(v_96));
var obj_98 = v_89;
V key_99 = (V)((V)R.Length(v_89) + (V)1.0d);
R.Set(obj_98, key_99, values_97[0]);
}
}
}
foreach (var row_100 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_89)))))) {
V v_101 = row_100[0];
V v_102 = row_100[1];
R.Call(v_82, R.Join(new V[] {}, new Results(v_102)));
}
return Results.Empty;
}));
foreach (var row_103 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_74, (V)"draw")), new Entry((V)2, R.Get(v_74, (V)"keypressed"))))))))) {
V v_104 = row_103[0];
V v_105 = row_103[1];
R.Call(v_82, R.Join(new V[] {}, new Results(v_105)));
}
return Results.Empty;
}));
V v_106 = V.Nil;
v_106 = new V((RuleFunction)(args_107 => {
V v_108 = args_107[0];
V v_109 = args_107[1];
var values_110 = R.Join(new V[] {}, new Results((V)false));
V v_111 = values_110[0];
R.Call(v_77, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_112 => {
V v_113 = args_112[0];
V v_114 = args_112[1];
V v_115 = args_112[2];
if (R.Truth((V)(v_115 == v_108))) {
R.Call(R.Get(rt.Global("debug"), (V)"setupvalue"), R.Join(new V[] {v_113, v_114}, new Results(v_109)));

var values_116 = R.Join(new V[] {}, new Results((V)true));
v_111 = values_116[0];
}
return Results.Empty;
})))));
R.Call(rt.Global("assert"), R.Join(new V[] {v_111}, new Results(R.Concat((V)"fixture upvalue missing: ", v_108))));

if (R.Truth(rt.Global("jit"))) {
R.Call(R.Get(rt.Global("jit"), (V)"flush"), Results.Empty);
}
return Results.Empty;
}));
V v_117 = V.Nil;
v_117 = new V((RuleFunction)(args_118 => {
V v_119 = args_118[0];
var values_120 = Results.Empty;
V v_121 = values_120[0];

R.Call(v_77, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_122 => {
V v_123 = args_122[0];
V v_124 = args_122[1];
V v_125 = args_122[2];
V v_126 = args_122[3];
if (R.Truth((V)(v_125 == v_119))) {
var values_127 = R.Join(new V[] {}, new Results(v_126));
v_121 = values_127[0];
}
return Results.Empty;
})))));
return R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {v_121}, new Results(R.Concat((V)"fixture value missing: ", v_119)))));
return Results.Empty;
}));
var values_128 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_129 = values_128[0];
R.Call(v_106, R.Join(new V[] {(V)"state"}, new Results(v_129)));

R.Call(v_106, R.Join(new V[] {(V)"title"}, new Results((V)false)));
R.Call(v_106, R.Join(new V[] {(V)"camera"}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.camera"))).First, (V)"new"), R.Join(new V[] {R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))).First, (V)"map_for"), R.Join(new V[] {}, new Results(v_129))).First, R.Table(new Entry((V)"x", (V)16.0d), new Entry((V)"y", (V)16.0d), new Entry((V)"width", (V)992.0d), new Entry((V)"height", (V)520.0d)), (V)40.0d, (V)2.0d}, new Results((V)2.0d)))));
R.Call(v_106, R.Join(new V[] {(V)"ui_body_font"}, new Results(v_72)));

R.Call(v_106, R.Join(new V[] {(V)"ui_display_font"}, new Results(v_72)));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"g", v_71), new Entry((V)"state", v_129), new Entry((V)"set", v_106), new Entry((V)"get", v_117))));
return Results.Empty;
}));
V v_130 = V.Nil;
v_130 = new V((RuleFunction)(args_131 => {
V v_132 = args_131[0];
V v_133 = args_131[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_132 - v_133)))).First < (V)1e-06d)}, new Results(R.Concat(v_132, R.Concat((V)" ~= ", v_133)))));
return Results.Empty;
}));
V v_134 = V.Nil;
v_134 = new V((RuleFunction)(args_135 => {
V v_136 = args_135[0];
V v_137 = args_135[1];
V v_138 = args_135[2];
{ double end_141 = R.Num((V)3.0d), step_142 = R.Num((V)1.0d);
for (double i_140 = R.Num((V)1.0d); step_142 > 0 ? i_140 <= end_141 : i_140 >= end_141; i_140 += step_142) {
V v_139 = i_140;
R.Call(v_130, R.Join(new V[] {R.Get(R.Get(v_136, (V)"color"), v_139)}, new Results((V)(R.Get(v_137, v_139) * R.Or(v_138, () => (V)1.0d)))));
}
}
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_143 => {
V v_144 = args_143[0];
V v_145 = args_143[1];
R.Call(v_144, R.Join(new V[] {(V)"semantic reward and enemy inks preserve shared handles, atlas geometry and generation ownership"}, new Results(new V((RuleFunction)(args_146 => {

var values_147 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", R.Table()), new Entry((V)"quad", R.Table()), new Entry((V)"width", (V)16.0d), new Entry((V)"height", (V)16.0d), new Entry((V)"tint", R.Table(new Entry((V)1, (V)0.9d), new Entry((V)2, (V)0.9d), new Entry((V)3, (V)0.9d))), new Entry((V)"offset_x", (V)17.0d), new Entry((V)"offset_y", (V)32.0d))));
V v_148 = values_147[0];
var values_150 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"resolve", new V((RuleFunction)(args_149 => {

return R.Join(new V[] {}, new Results(v_148));
return Results.Empty;
}))))));
V v_151 = values_150[0];
var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"resolve"), R.Join(new V[] {v_151}, new Results((V)"core:actor/adventurer"))));
V v_153 = values_152[0];
var values_154 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"resolve"), R.Join(new V[] {v_151}, new Results((V)"core:enemy/mossbound_guard/attack"))));
V v_155 = values_154[0];
var values_156 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"resolve"), R.Join(new V[] {v_151}, new Results((V)"core:boon/emberbrand/icon"))));
V v_157 = values_156[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_153 == v_148), () => (V)(v_155 != v_157)), () => (V)(R.Get(v_148, (V)"ink") == V.Nil)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_155, (V)"ink") == R.Get(v_2, (V)"enemy")), () => (V)(R.Get(v_157, (V)"ink") == R.Get(v_2, (V)"gold"))))));
foreach (var row_158 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_155), new Entry((V)2, v_157)))))))) {
V v_159 = row_158[0];
V v_160 = row_158[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_160, (V)"texture") == R.Get(v_148, (V)"texture")), () => (V)(R.Get(v_160, (V)"quad") == R.Get(v_148, (V)"quad"))), () => (V)(R.Get(v_160, (V)"offset_x") == (V)17.0d)), () => (V)(R.Get(v_160, (V)"offset_y") == (V)32.0d)), () => (V)(R.Get(v_160, (V)"width") == (V)16.0d)), () => (V)(R.Get(v_160, (V)"height") == (V)16.0d)))));
}
{ double end_163 = R.Num((V)1000.0d), step_164 = R.Num((V)1.0d);
for (double i_162 = R.Num((V)1.0d); step_164 > 0 ? i_162 <= end_163 : i_162 >= end_163; i_162 += step_164) {
V v_161 = i_162;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"resolve"), R.Join(new V[] {v_151}, new Results((V)"core:boon/emberbrand/icon"))).First == v_157))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_2, (V)"binding"), R.Join(new V[] {}, new Results((V)"core:object/idea_cache_dormant"))).First == R.Get(v_2, (V)"chest"))}, new Results((V)"dormant mimic must resemble loot")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"binding"), R.Join(new V[] {}, new Results((V)"core:object/chest_open"))).First == R.Get(v_2, (V)"spent")))));
foreach (var row_165 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))).First, (V)"order"))))))) {
V v_166 = row_165[0];
V v_167 = row_165[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"binding"), R.Join(new V[] {}, new Results(R.Concat(v_167, (V)"/icon")))).First == R.Get(v_2, (V)"gold")))));
}
foreach (var row_168 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"health"), new Entry((V)2, (V)"currency"), new Entry((V)3, (V)"food")))))))) {
V v_169 = row_168[0];
V v_170 = row_168[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"binding"), R.Join(new V[] {}, new Results(R.Concat((V)"core:drop/", v_170))))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_2, (V)"binding"), R.Join(new V[] {}, new Results((V)"core:terrain/bramble_bed"))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_2, (V)"binding"), R.Join(new V[] {}, new Results(V.Nil))).First))))));
var values_171 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"resolve", R.Get(v_151, (V)"resolve")))));
V v_172 = values_171[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_2, (V)"resolve"), R.Join(new V[] {v_172}, new Results((V)"core:boon/emberbrand/icon"))).First != v_157)}, new Results((V)"cache crossed resource generations")));
return Results.Empty;
})))));
R.Call(v_144, R.Join(new V[] {(V)"palette shader scopes ink and restores neutral drawing; failed uniform initialization falls back once"}, new Results(new V((RuleFunction)(args_173 => {

var values_174 = R.Join(new V[] {}, R.Call(v_7, Results.Empty));
V v_175 = values_174[0];

var values_176 = R.Join(new V[] {}, new Results(R.Table()));
V v_177 = values_176[0];

var values_178 = R.Join(new V[] {}, new Results(v_177));
var obj_179 = v_175;
V key_180 = (V)"shader";
R.Set(obj_179, key_180, values_178[0]);
var values_181 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"new"), Results.Empty));
V v_182 = values_181[0];

var values_183 = R.Join(new V[] {}, R.Invoke(v_182, "begin", R.Join(new V[] {}, new Results(v_175))));
V v_184 = values_183[0];
V v_185 = values_183[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_185, () => (V)(v_184 == v_177)))));
var values_186 = R.Join(new V[] {}, R.Invoke(v_182, "set_ink", R.Join(new V[] {v_175}, new Results(R.Get(v_2, (V)"hostile")))));
V v_187 = values_186[0];

R.Call(v_145, R.Join(new V[] {R.Get(R.Get(v_175, (V)"shader"), (V)"ink")}, new Results(R.Get(v_2, (V)"hostile"))));
var values_188 = R.Join(new V[] {}, R.Invoke(v_182, "set_ink", R.Join(new V[] {v_175}, new Results(R.Get(v_2, (V)"friendly")))));
V v_189 = values_188[0];

R.Call(v_145, R.Join(new V[] {R.Get(R.Get(v_175, (V)"shader"), (V)"ink")}, new Results(R.Get(v_2, (V)"friendly"))));
R.Invoke(v_182, "set_ink", R.Join(new V[] {v_175}, new Results(v_189)));

R.Call(v_145, R.Join(new V[] {R.Get(R.Get(v_175, (V)"shader"), (V)"ink")}, new Results(R.Get(v_2, (V)"hostile"))));
R.Invoke(v_182, "set_ink", R.Join(new V[] {v_175}, new Results(v_187)));

R.Call(v_145, R.Join(new V[] {R.Get(R.Get(v_175, (V)"shader"), (V)"ink")}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d)))));
R.Invoke(v_182, "finish", R.Join(new V[] {v_175, v_184}, new Results(v_185)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_175, (V)"shader") == v_177))));
var values_190 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_191 = values_190[0];
V v_192 = values_190[1];
var values_197 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"newShader", new V((RuleFunction)(args_193 => {

var values_194 = R.Join(new V[] {}, new Results((V)(v_191 + (V)1.0d)));
v_191 = values_194[0];

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"send", new V((RuleFunction)(args_195 => {

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"uniform unavailable")));
return Results.Empty;
}))))));
return Results.Empty;
}))), new Entry((V)"getShader", new V((RuleFunction)(args_196 => {

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"failed candidate must not activate")));
return Results.Empty;
}))))));
V v_198 = values_197[0];
var values_199 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"new"), Results.Empty));
V v_200 = values_199[0];
{ double end_203 = R.Num((V)30.0d), step_204 = R.Num((V)1.0d);
for (double i_202 = R.Num((V)1.0d); step_204 > 0 ? i_202 <= end_203 : i_202 >= end_203; i_202 += step_204) {
V v_201 = i_202;
var values_207 = R.Join(new V[] {}, R.Invoke(v_200, "begin", R.Join(new V[] {v_198}, new Results(new V((RuleFunction)(args_205 => {

var values_206 = R.Join(new V[] {}, new Results((V)(v_192 + (V)1.0d)));
v_192 = values_206[0];
return Results.Empty;
}))))));
V v_208 = values_207[0];
V v_209 = values_207[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_209)))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_191 == (V)1.0d), () => (V)(v_192 == (V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_144, R.Join(new V[] {(V)"rainbow damage ink is deterministic, gently pulses, spans hues and leaves blocked zeros calm"}, new Results(new V((RuleFunction)(args_210 => {

var values_211 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_212 = values_211[0];

var values_213 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_212))));
V v_214 = values_213[0];
var values_215 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d), new Entry((V)"amount", (V)4.0d), new Entry((V)"age", (V)0.0d))));
V v_216 = values_215[0];

var values_217 = R.Join(new V[] {}, new Results(R.Table()));
V v_218 = values_217[0];

var values_219 = R.Join(new V[] {}, new Results(R.Table()));
V v_220 = values_219[0];
{ double end_223 = R.Num((V)77.0d), step_224 = R.Num((V)1.0d);
for (double i_222 = R.Num((V)0.0d); step_224 > 0 ? i_222 <= end_223 : i_222 >= end_223; i_222 += step_224) {
V v_221 = i_222;
var values_225 = R.Join(new V[] {}, new Results((V)(v_221 / (V)100.0d)));
var obj_226 = v_216;
V key_227 = (V)"age";
R.Set(obj_226, key_227, values_225[0]);
var values_228 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"damage"), R.Join(new V[] {}, new Results(v_216))));
V v_229 = values_228[0];

R.Call(v_145, R.Join(new V[] {v_229}, R.Call(R.Get(v_2, (V)"damage"), R.Join(new V[] {}, new Results(v_216)))));
var values_230 = R.Join(new V[] {}, new Results((V)1.0d));
V v_231 = values_230[0];
foreach (var row_232 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_229)))))) {
V v_233 = row_232[0];
V v_234 = row_232[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_234 >= (V)0.159d), () => (V)(v_234 <= (V)1.0d)))));

if (R.Truth((V)(v_234 > R.Get(v_229, v_231)))) {
var values_235 = R.Join(new V[] {}, new Results(v_233));
v_231 = values_235[0];
}
}
var values_236 = R.Join(new V[] {}, new Results((V)true));
var obj_237 = v_218;
V key_238 = v_231;
R.Set(obj_237, key_238, values_236[0]);

var values_239 = R.Join(new V[] {}, new Results(R.Get(v_229, v_231)));
var obj_240 = v_220;
V key_241 = (V)((V)R.Length(v_220) + (V)1.0d);
R.Set(obj_240, key_241, values_239[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_218, (V)1.0d), () => R.Get(v_218, (V)2.0d)), () => R.Get(v_218, (V)3.0d)))));
var values_242 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
V v_243 = values_242[0];
V v_244 = values_242[1];
foreach (var row_245 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_220)))))) {
V v_246 = row_245[0];
V v_247 = row_245[1];
var values_248 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_243}, new Results(v_247))));
v_243 = values_248[0];

var values_249 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_244}, new Results(v_247))));
v_244 = values_249[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(v_243 >= (V)((V)0.8d - (V)1e-06d)), () => (V)((V)(v_244 - v_243) > (V)0.1d))}, new Results((V)"pulse must be visible without blinking")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"damage"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"amount", (V)0.0d), new Entry((V)"age", (V)999.0d))))).First == R.Get(v_2, (V)"enemy")))));
R.Call(v_145, R.Join(new V[] {R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_212))).First}, new Results(v_214)));
return Results.Empty;
})))));
R.Call(v_144, R.Join(new V[] {(V)"real sprite and tactical draw callbacks keep neutral terrain and separate enemy red from player green"}, new Results(new V((RuleFunction)(args_250 => {

var values_251 = R.Join(new V[] {}, R.Call(v_68, Results.Empty));
V v_252 = values_251[0];

var values_253 = R.Join(new V[] {}, new Results(R.Get(v_252, (V)"state")));
V v_254 = values_253[0];

var values_255 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_254))));
V v_256 = values_255[0];
var values_257 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", R.Table()), new Entry((V)"quad", R.Table()), new Entry((V)"width", (V)16.0d), new Entry((V)"height", (V)16.0d), new Entry((V)"tint", R.Table(new Entry((V)1, (V)0.9d), new Entry((V)2, (V)0.9d), new Entry((V)3, (V)0.9d))))));
V v_258 = values_257[0];
var values_260 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"resolve", new V((RuleFunction)(args_259 => {

return R.Join(new V[] {}, new Results(v_258));
return Results.Empty;
}))))));
V v_261 = values_260[0];
R.Call(R.Get(v_252, (V)"set"), R.Join(new V[] {(V)"visual_assets"}, new Results(v_261)));

R.Call(R.Get(v_252, (V)"set"), R.Join(new V[] {(V)"visible_at"}, new Results(new V((RuleFunction)(args_262 => {

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})))));
var values_263 = R.Join(new V[] {}, R.Call(R.Get(v_252, (V)"get"), R.Join(new V[] {}, new Results((V)"onebit_filter"))));
V v_264 = values_263[0];

var values_265 = R.Join(new V[] {}, R.Invoke(v_264, "begin", R.Join(new V[] {}, new Results(R.Get(v_252, (V)"g")))));
V v_266 = values_265[0];
V v_267 = values_265[1];
foreach (var row_268 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"core:drop/currency"), new Entry((V)2, R.Get(v_2, (V)"gold")))), new Entry((V)2, R.Table(new Entry((V)1, (V)"core:chest/standard"), new Entry((V)2, R.Get(v_2, (V)"chest")))), new Entry((V)3, R.Table(new Entry((V)1, (V)"core:enemy/mossbound_guard"), new Entry((V)2, R.Get(v_2, (V)"enemy")))), new Entry((V)4, R.Table(new Entry((V)1, (V)"core:actor/adventurer"), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))))), new Entry((V)5, R.Table(new Entry((V)1, (V)"core:terrain/bramble_bed"), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d)))))))))))) {
V v_269 = row_268[0];
V v_270 = row_268[1];
R.Call(R.Call(R.Get(v_252, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_visual"))).First, R.Join(new V[] {R.Call(R.Call(R.Get(v_252, (V)"get"), R.Join(new V[] {}, new Results((V)"bound_visual"))).First, R.Join(new V[] {}, new Results(R.Get(v_270, (V)1.0d)))).First, (V)10.0d, (V)20.0d, (V)40.0d, (V)40.0d, (V)0.6d}, new Results((V)true)));
var values_271 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_252, (V)"g"), (V)"records"), (V)R.Length(R.Get(R.Get(v_252, (V)"g"), (V)"records")))));
V v_272 = values_271[0];

R.Call(v_134, R.Join(new V[] {v_272, R.Get(v_270, (V)2.0d)}, new Results((V)0.9d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(R.Get(v_272, (V)"args"), (V)3.0d) == (V)50.0d), () => (V)(R.Get(R.Get(v_272, (V)"args"), (V)6.0d) == (-(V)2.5d))), () => (V)(R.Get(R.Get(v_272, (V)"args"), (V)7.0d) == (V)2.5d)))));
R.Call(v_130, R.Join(new V[] {R.Get(R.Get(v_272, (V)"color"), (V)4.0d)}, new Results((V)0.6d)));

R.Call(v_145, R.Join(new V[] {R.Get(v_264, (V)"ink")}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d)))));
}
foreach (var row_273 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_254, (V)"player_id")), new Entry((V)2, (V)999.0d)))))))) {
V v_274 = row_273[0];
V v_275 = row_273[1];
var values_276 = R.Join(new V[] {}, new Results(R.Table()));
var obj_277 = R.Get(v_252, (V)"g");
V key_278 = (V)"records";
R.Set(obj_277, key_278, values_276[0]);
R.Call(R.Call(R.Get(v_252, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_history_event"))).First, R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"WindupStarted"), new Entry((V)"source_id", v_275), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))))), new Entry((V)"floor_id", R.Get(v_254, (V)"active_floor_id"))))));
var values_279 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_275 == R.Get(v_254, (V)"player_id")), () => R.Get(v_2, (V)"friendly")), () => R.Get(v_2, (V)"hostile"))));
V v_280 = values_279[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_252, (V)"g"), (V)"records")) > (V)0.0d))));
foreach (var row_281 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_252, (V)"g"), (V)"records"))))))) {
V v_282 = row_281[0];
V v_283 = row_281[1];
if (R.Truth((V)((V)(v_282 % (V)2.0d) == (V)0.0d))) {
R.Call(v_134, R.Join(new V[] {v_283}, new Results(v_280)));
}
}
}
foreach (var row_284 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"basic_attack"), new Entry((V)2, (V)"sweeping_slash"), new Entry((V)3, (V)"dash")))))))) {
V v_285 = row_284[0];
V v_286 = row_284[1];
R.Call(R.Get(v_252, (V)"set"), R.Join(new V[] {(V)"targeting"}, new Results(R.Table(new Entry((V)"kind", v_286), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
var values_287 = R.Join(new V[] {}, new Results(R.Table()));
var obj_288 = R.Get(v_252, (V)"g");
V key_289 = (V)"records";
R.Set(obj_288, key_289, values_287[0]);

R.Call(R.Call(R.Get(v_252, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_preview"))).First, Results.Empty);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_252, (V)"g"), (V)"records")) > (V)0.0d))));
foreach (var row_290 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_252, (V)"g"), (V)"records"))))))) {
V v_291 = row_290[0];
V v_292 = row_290[1];
if (R.Truth((V)((V)(v_291 % (V)2.0d) == (V)0.0d))) {
R.Call(v_134, R.Join(new V[] {v_292}, new Results(R.Get(v_2, (V)"friendly"))));
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {(V)"dash"}, new Results((V)"wall"))).First == R.Get(v_2, (V)"blocked")), () => (V)(R.Call(R.Get(v_2, (V)"preview"), R.Join(new V[] {(V)"use_utility"}, new Results((V)"unreachable"))).First == R.Get(v_2, (V)"blocked"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"attack"), R.Join(new V[] {R.Get(v_254, (V)"player_id"), R.Table(new Entry((V)"source_id", (V)999.0d))}, new Results(R.Table(new Entry((V)"owner_id", R.Get(v_254, (V)"player_id")))))).First == R.Get(v_2, (V)"friendly")))));
R.Call(R.Get(v_252, (V)"set"), R.Join(new V[] {(V)"visible_at"}, new Results(new V((RuleFunction)(args_293 => {

return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})))));

var values_294 = R.Join(new V[] {}, new Results(R.Table()));
var obj_295 = R.Get(v_252, (V)"g");
V key_296 = (V)"records";
R.Set(obj_295, key_296, values_294[0]);
R.Call(R.Call(R.Get(v_252, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_cell"))).First, R.Join(new V[] {R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d)), (V)1.0d, (V)1.0d, (V)1.0d, (V)0.7d, (V)true}, new Results(R.Get(v_2, (V)"hostile"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_252, (V)"g"), (V)"records")) == (V)0.0d))));
R.Invoke(v_264, "finish", R.Join(new V[] {R.Get(v_252, (V)"g"), v_266}, new Results(v_267)));
var values_297 = R.Join(new V[] {}, new Results(V.Nil));
var obj_298 = v_264;
V key_299 = (V)"shader";
R.Set(obj_298, key_299, values_297[0]);
R.Call(R.Call(R.Get(v_252, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_visual"))).First, R.Join(new V[] {R.Call(R.Call(R.Get(v_252, (V)"get"), R.Join(new V[] {}, new Results((V)"bound_visual"))).First, R.Join(new V[] {}, new Results((V)"core:drop/health"))).First, (V)10.0d, (V)20.0d, (V)40.0d}, new Results((V)40.0d)));
R.Call(v_134, R.Join(new V[] {R.Get(R.Get(R.Get(v_252, (V)"g"), (V)"records"), (V)R.Length(R.Get(R.Get(v_252, (V)"g"), (V)"records"))), R.Get(v_2, (V)"gold")}, new Results((V)0.9d)));
R.Call(v_145, R.Join(new V[] {R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_254))).First}, new Results(v_256)));
return Results.Empty;
})))));
R.Call(v_144, R.Join(new V[] {(V)"real resolved damage numbers animate their ink while preserving labels, fog and exact run snapshot"}, new Results(new V((RuleFunction)(args_300 => {

var values_301 = R.Join(new V[] {}, R.Call(v_68, Results.Empty));
V v_302 = values_301[0];

var values_303 = R.Join(new V[] {}, new Results(R.Get(v_302, (V)"state")));
V v_304 = values_303[0];
R.Call(R.Get(v_4, (V)"spawn_enemy"), R.Join(new V[] {v_304, (V)"core:enemy/mossbound_guard", (V)3.0d}, new Results((V)2.0d)));
var values_305 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"submit"), R.Join(new V[] {v_304}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_306 = values_305[0];
V v_307 = values_305[1];
V v_308 = values_305[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_306}, new Results(v_307)));
var values_309 = Results.Empty;
V v_310 = values_309[0];
foreach (var row_311 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_308)))))) {
V v_312 = row_311[0];
V v_313 = row_311[1];
if (R.Truth(R.And((V)(R.Get(v_313, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_313, (V)"source_id") == R.Get(v_304, (V)"player_id"))))) {
var values_314 = R.Join(new V[] {}, new Results(v_313));
v_310 = values_314[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_310, () => (V)(R.Get(v_310, (V)"actual_loss") > (V)0.0d)))));
var values_315 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_304))));
V v_316 = values_315[0];

var values_317 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.combat_numbers"))));
V v_318 = values_317[0];

var values_319 = R.Join(new V[] {}, R.Call(R.Get(v_318, (V)"new"), Results.Empty));
V v_320 = values_319[0];
R.Call(R.Get(v_318, (V)"update"), R.Join(new V[] {v_320, R.Table(new Entry((V)"items", R.Table(new Entry((V)1, R.Table(new Entry((V)"event", v_310)))))), (V)0.0d, R.Get(v_304, (V)"player_id")}, new Results(R.Get(v_304, (V)"active_floor_id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_320, (V)"items")) == (V)1.0d))));

var values_321 = R.Join(new V[] {}, new Results((V)true));
var obj_322 = R.Get(R.Get(v_320, (V)"items"), (V)1.0d);
V key_323 = (V)"critical";
R.Set(obj_322, key_323, values_321[0]);
R.Call(R.Get(v_302, (V)"set"), R.Join(new V[] {(V)"combat_numbers"}, new Results(v_320)));

R.Call(R.Get(v_302, (V)"set"), R.Join(new V[] {(V)"visible_at"}, new Results(new V((RuleFunction)(args_324 => {

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})))));
var values_325 = R.Join(new V[] {}, R.Call(R.Get(v_302, (V)"get"), R.Join(new V[] {}, new Results((V)"onebit_filter"))));
V v_326 = values_325[0];

var values_327 = R.Join(new V[] {}, R.Invoke(v_326, "begin", R.Join(new V[] {}, new Results(R.Get(v_302, (V)"g")))));
V v_328 = values_327[0];
V v_329 = values_327[1];
var values_330 = Results.Empty;
V v_331 = values_330[0];
foreach (var row_332 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.18d), new Entry((V)3, (V)0.39d)))))))) {
V v_333 = row_332[0];
V v_334 = row_332[1];
var values_335 = R.Join(new V[] {}, new Results(v_334));
var obj_336 = R.Get(R.Get(v_320, (V)"items"), (V)1.0d);
V key_337 = (V)"age";
R.Set(obj_336, key_337, values_335[0]);

var values_338 = R.Join(new V[] {}, new Results(R.Table()));
var obj_339 = R.Get(v_302, (V)"g");
V key_340 = (V)"records";
R.Set(obj_339, key_340, values_338[0]);

R.Call(R.Call(R.Get(v_302, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_combat_numbers"))).First, Results.Empty);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_302, (V)"g"), (V)"records")) == (V)5.0d))));
foreach (var row_341 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_302, (V)"g"), (V)"records"))))))) {
V v_342 = row_341[0];
V v_343 = row_341[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_343, (V)"args"), (V)1.0d) == R.Concat(R.Get(v_310, (V)"actual_loss"), (V)"!")))));
}
var values_344 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_302, (V)"g"), (V)"records"), (V)5.0d)));
V v_345 = values_344[0];

R.Call(v_134, R.Join(new V[] {v_345}, R.Call(R.Get(v_2, (V)"damage"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_320, (V)"items"), (V)1.0d))))));
{ double end_348 = R.Num((V)4.0d), step_349 = R.Num((V)1.0d);
for (double i_347 = R.Num((V)1.0d); step_349 > 0 ? i_347 <= end_348 : i_347 >= end_348; i_347 += step_349) {
V v_346 = i_347;
R.Call(v_134, R.Join(new V[] {R.Get(R.Get(R.Get(v_302, (V)"g"), (V)"records"), v_346), R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))}, new Results((V)0.025d)));
}
}
if (R.Truth(v_331)) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_331, (V)1.0d) - R.Get(R.Get(v_345, (V)"color"), (V)1.0d))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_331, (V)2.0d) - R.Get(R.Get(v_345, (V)"color"), (V)2.0d))))).First) > (V)0.1d))));
} else {
var values_350 = R.Join(new V[] {}, new Results(R.Get(v_345, (V)"color")));
v_331 = values_350[0];
}
R.Call(v_145, R.Join(new V[] {R.Get(v_326, (V)"ink")}, new Results(R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d)))));
}
R.Call(R.Get(v_302, (V)"set"), R.Join(new V[] {(V)"visible_at"}, new Results(new V((RuleFunction)(args_351 => {

return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})))));

var values_352 = R.Join(new V[] {}, new Results(R.Table()));
var obj_353 = R.Get(v_302, (V)"g");
V key_354 = (V)"records";
R.Set(obj_353, key_354, values_352[0]);
R.Call(R.Call(R.Get(v_302, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_combat_numbers"))).First, Results.Empty);

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_302, (V)"g"), (V)"records")) == (V)0.0d))));
R.Invoke(v_326, "finish", R.Join(new V[] {R.Get(v_302, (V)"g"), v_328}, new Results(v_329)));

R.Call(v_145, R.Join(new V[] {R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(v_304))).First}, new Results(v_316)));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
