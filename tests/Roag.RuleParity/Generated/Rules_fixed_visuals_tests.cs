// Static C# port of tests/fixed_visuals_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 8b2c600eb85074211782aed80296b36b606413edced3623a16d5ce4101eb2e8f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_fixed_visuals_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];

var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_visuals"))));
V v_7 = values_6[0];

var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_packs"))));
V v_9 = values_8[0];
V v_10 = V.Nil;
v_10 = new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
var values_13 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {v_12}, new Results((V)"rb"))));
V v_14 = values_13[0];

if (R.Truth(v_14)) {
var values_15 = R.Join(new V[] {}, R.Invoke(v_14, "read", R.Join(new V[] {}, new Results((V)"*a"))));
V v_16 = values_15[0];

R.Invoke(v_14, "close", Results.Empty);

return R.Join(new V[] {}, new Results(v_16));
}
return Results.Empty;
}));
V v_17 = V.Nil;
v_17 = new V((RuleFunction)(args_18 => {
V v_19 = args_18[0];
V v_20 = args_18[1];
V v_21 = V.Nil;
v_21 = new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
var values_24 = R.Join(new V[] {}, R.Invoke(v_19, "byte", R.Join(new V[] {v_23}, new Results((V)(v_23 + (V)3.0d)))));
V v_25 = values_24[0];
V v_26 = values_24[1];
V v_27 = values_24[2];
V v_28 = values_24[3];

return R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)((V)(v_25 * (V)256.0d) + v_26) * (V)256.0d) + v_27) * (V)256.0d) + v_28)));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", v_20), new Entry((V)"getWidth", new V((RuleFunction)(args_29 => {

return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {}, new Results((V)17.0d))));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_30 => {

return R.Join(new V[] {}, R.Call(v_21, R.Join(new V[] {}, new Results((V)21.0d))));
return Results.Empty;
}))))));
return Results.Empty;
}));
V v_31 = V.Nil;
v_31 = new V((RuleFunction)(args_32 => {
V v_33 = args_32[0];
V v_34 = args_32[1];
V v_35 = args_32[2];
V v_36 = args_32[3];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_33), new Entry((V)"y", v_34), new Entry((V)"w", v_35), new Entry((V)"h", v_36))));
return Results.Empty;
}));
V v_37 = V.Nil;
v_37 = new V((RuleFunction)(args_38 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(rt.Global("_G"), (V)"love"))))));
var values_39 = R.Join(new V[] {}, new Results(R.Table()));
V v_40 = values_39[0];

var values_43 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"love", v_40), new Entry((V)"os", R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"getenv", new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
if (R.Truth((V)(v_42 == (V)"ROAG_SPRITE_PACK"))) {
return R.Join(new V[] {}, new Results((V)"kenney"));
}
return Results.Empty;
}))))}, new Results(R.Table(new Entry((V)"__index", rt.Global("os")))))).First))}, new Results(R.Table(new Entry((V)"__index", rt.Global("_G")))))));
V v_44 = values_43[0];
var values_45 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("loadfile"), R.Join(new V[] {}, new Results((V)"main.lua"))))));
V v_46 = values_45[0];

R.Call(rt.Global("setfenv"), R.Join(new V[] {v_46}, new Results(v_44)));

R.Call(v_46, Results.Empty);
V v_47 = V.Nil;
v_47 = new V((RuleFunction)(args_48 => {
V v_49 = args_48[0];
V v_50 = args_48[1];
V v_51 = args_48[2];
var values_52 = R.Join(new V[] {}, new Results(R.Or(v_51, () => R.Table())));
v_51 = values_52[0];

if (R.Truth(R.Get(v_51, v_49))) {
return Results.Empty;
}

var values_53 = R.Join(new V[] {}, new Results((V)true));
var obj_54 = v_51;
V key_55 = v_49;
R.Set(obj_54, key_55, values_53[0]);

var values_56 = R.Join(new V[] {}, new Results(R.Table()));
V v_57 = values_56[0];
{ double end_60 = R.Num((V)200.0d), step_61 = R.Num((V)1.0d);
for (double i_59 = R.Num((V)1.0d); step_61 > 0 ? i_59 <= end_60 : i_59 >= end_60; i_59 += step_61) {
V v_58 = i_59;
var values_62 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("debug"), (V)"getupvalue"), R.Join(new V[] {v_49}, new Results(v_58))));
V v_63 = values_62[0];
V v_64 = values_62[1];

if (R.Truth((V)(!R.Truth(v_63)))) {
break;
}
R.Call(v_50, R.Join(new V[] {v_49, v_58, v_63}, new Results(v_64)));

if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_64))).First == (V)"function"))) {
var values_65 = R.Join(new V[] {}, new Results(v_64));
var obj_66 = v_57;
V key_67 = (V)((V)R.Length(v_57) + (V)1.0d);
R.Set(obj_66, key_67, values_65[0]);
}
}
}
foreach (var row_68 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_57)))))) {
V v_69 = row_68[0];
V v_70 = row_68[1];
R.Call(v_47, R.Join(new V[] {v_70, v_50}, new Results(v_51)));
}
return Results.Empty;
}));
V v_71 = V.Nil;
v_71 = new V((RuleFunction)(args_72 => {
V v_73 = args_72[0];
foreach (var row_74 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_40, (V)"draw")), new Entry((V)2, R.Get(v_40, (V)"update")), new Entry((V)3, R.Get(v_40, (V)"keypressed"))))))))) {
V v_75 = row_74[0];
V v_76 = row_74[1];
R.Call(v_47, R.Join(new V[] {v_76}, new Results(v_73)));
}
return Results.Empty;
}));
V v_77 = V.Nil;
v_77 = new V((RuleFunction)(args_78 => {
V v_79 = args_78[0];
V v_80 = args_78[1];
var values_81 = R.Join(new V[] {}, new Results((V)false));
V v_82 = values_81[0];

R.Call(v_71, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_83 => {
V v_84 = args_83[0];
V v_85 = args_83[1];
V v_86 = args_83[2];
if (R.Truth((V)(v_86 == v_79))) {
R.Call(R.Get(rt.Global("debug"), (V)"setupvalue"), R.Join(new V[] {v_84, v_85}, new Results(v_80)));

var values_87 = R.Join(new V[] {}, new Results((V)true));
v_82 = values_87[0];
}
return Results.Empty;
})))));
R.Call(rt.Global("assert"), R.Join(new V[] {v_82}, new Results(R.Concat((V)"fixture upvalue missing: ", v_79))));

if (R.Truth(rt.Global("jit"))) {
R.Call(R.Get(rt.Global("jit"), (V)"flush"), Results.Empty);
}
return Results.Empty;
}));
V v_88 = V.Nil;
v_88 = new V((RuleFunction)(args_89 => {
V v_90 = args_89[0];
var values_91 = Results.Empty;
V v_92 = values_91[0];

R.Call(v_71, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_93 => {
V v_94 = args_93[0];
V v_95 = args_93[1];
V v_96 = args_93[2];
V v_97 = args_93[3];
if (R.Truth((V)(v_96 == v_90))) {
var values_98 = R.Join(new V[] {}, new Results(v_97));
v_92 = values_98[0];
}
return Results.Empty;
})))));

return R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {v_92}, new Results(R.Concat((V)"missing ", v_90)))));
return Results.Empty;
}));
return R.Join(new V[] {v_40, v_77}, new Results(v_88));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"actual native loader ignores the retired environment override and cannot select the mixed renderer"}, new Results(new V((RuleFunction)(args_99 => {

var values_100 = R.Join(new V[] {}, R.Call(v_37, Results.Empty));
V v_101 = values_100[0];
V v_102 = values_100[1];
V v_103 = values_100[2];

var values_104 = R.Join(new V[] {}, new Results((V)0.0d));
V v_105 = values_104[0];

var values_106 = R.Join(new V[] {}, new Results(R.Get(v_7, (V)"new")));
V v_107 = values_106[0];
var values_111 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_108 => {
V v_109 = args_108[0];
var values_110 = R.Join(new V[] {}, new Results((V)(v_105 + (V)1.0d)));
v_105 = values_110[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_109 == v_10))));

return R.Join(new V[] {R.Table(new Entry((V)"style", (V)"onebit"))}, new Results((V)"loaded"));
return Results.Empty;
}))));
var obj_112 = v_7;
V key_113 = (V)"new";
R.Set(obj_112, key_113, values_111[0]);
var values_114 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Call(v_103, R.Join(new V[] {}, new Results((V)"load_native_visuals"))).First, v_10}, new Results((V)"kenney"))));
V v_115 = values_114[0];
V v_116 = values_114[1];
V v_117 = values_114[2];

var values_118 = R.Join(new V[] {}, new Results(v_107));
var obj_119 = v_7;
V key_120 = (V)"new";
R.Set(obj_119, key_120, values_118[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {v_115}, new Results(v_116)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_116, (V)"style") == (V)"onebit"), () => (V)(v_117 == (V)"loaded")), () => (V)(v_105 == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_101, (V)"roagVisualPackMode"), Results.Empty).First == (V)"onebit"))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real settings callbacks offer only sound effects and accessibility and cannot save another art style"}, new Results(new V((RuleFunction)(args_121 => {

var values_122 = R.Join(new V[] {}, R.Call(v_37, Results.Empty));
V v_123 = values_122[0];
V v_124 = values_122[1];
V v_125 = values_122[2];

var values_126 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_127 = values_126[0];

var values_128 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_127))));
V v_129 = values_128[0];
var values_130 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.presentation_settings"))).First, (V)"defaults"), Results.Empty));
V v_131 = values_130[0];

var values_132 = R.Join(new V[] {}, new Results(R.Table()));
V v_133 = values_132[0];
R.Set(v_133, "write", new V((RuleFunction)(args_134 => {
V v_135 = args_134[0];
V v_136 = args_134[1];
V v_137 = args_134[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_136 == (V)"presentation-settings.roag"))));

var values_138 = R.Join(new V[] {}, new Results(v_137));
var obj_139 = v_135;
V key_140 = v_136;
R.Set(obj_139, key_140, values_138[0]);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_133, "read", new V((RuleFunction)(args_141 => {
V v_142 = args_141[0];
V v_143 = args_141[1];
return R.Join(new V[] {}, new Results(R.Get(v_142, v_143)));
return Results.Empty;
})));
var values_150 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"getWidth", new V((RuleFunction)(args_144 => {
V v_145 = args_144[0];
V v_146 = args_144[1];
return R.Join(new V[] {}, new Results((V)((V)R.Length(v_146) * (V)7.0d)));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_147 => {

return R.Join(new V[] {}, new Results((V)14.0d));
return Results.Empty;
}))), new Entry((V)"setFilter", new V((RuleFunction)(args_148 => {


return Results.Empty;
}))), new Entry((V)"setFallbacks", new V((RuleFunction)(args_149 => {


return Results.Empty;
}))))));
V v_151 = values_150[0];
var values_152 = R.Join(new V[] {}, new Results(R.Table()));
V v_153 = values_152[0];

var values_165 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"newFont", new V((RuleFunction)(args_154 => {

return R.Join(new V[] {}, new Results(v_151));
return Results.Empty;
}))), new Entry((V)"getFont", new V((RuleFunction)(args_155 => {

return R.Join(new V[] {}, new Results(v_151));
return Results.Empty;
}))), new Entry((V)"getWidth", new V((RuleFunction)(args_156 => {

return R.Join(new V[] {}, new Results((V)1024.0d));
return Results.Empty;
}))), new Entry((V)"getDimensions", new V((RuleFunction)(args_157 => {

return R.Join(new V[] {(V)1024.0d}, new Results((V)640.0d));
return Results.Empty;
}))), new Entry((V)"printf", new V((RuleFunction)(args_158 => {
V v_159 = args_158[0];
var values_160 = R.Join(new V[] {}, new Results((V)true));
var obj_161 = v_153;
V key_162 = v_159;
R.Set(obj_161, key_162, values_160[0]);
return Results.Empty;
}))))}, new Results(R.Table(new Entry((V)"__index", new V((RuleFunction)(args_163 => {

return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_164 => {


return Results.Empty;
}))));
return Results.Empty;
}))))))));
var obj_166 = v_123;
V key_167 = (V)"graphics";
R.Set(obj_166, key_167, values_165[0]);
var values_169 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"setVolume", new V((RuleFunction)(args_168 => {


return Results.Empty;
}))))));
var obj_170 = v_123;
V key_171 = (V)"audio";
R.Set(obj_170, key_171, values_169[0]);
R.Call(v_124, R.Join(new V[] {(V)"state"}, new Results(v_127)));

R.Call(v_124, R.Join(new V[] {(V)"title"}, new Results((V)true)));

R.Call(v_124, R.Join(new V[] {(V)"title_view"}, new Results((V)"settings")));

R.Call(v_124, R.Join(new V[] {(V)"storage"}, new Results(v_133)));
R.Call(v_124, R.Join(new V[] {(V)"presentation_settings"}, new Results(v_131)));

R.Call(v_124, R.Join(new V[] {(V)"settings_page"}, new Results((V)1.0d)));

R.Call(v_124, R.Join(new V[] {(V)"settings_selection"}, new Results((V)1.0d)));
R.Call(v_124, R.Join(new V[] {(V)"ui_body_font"}, new Results(v_151)));

R.Call(v_124, R.Join(new V[] {(V)"ui_menu_font"}, new Results(v_151)));

R.Call(v_124, R.Join(new V[] {(V)"ui_display_font"}, new Results(v_151)));
var values_172 = R.Join(new V[] {}, new Results(v_123));
var obj_173 = rt.Global("_G");
V key_174 = (V)"love";
R.Set(obj_173, key_174, values_172[0]);

var values_179 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(new V((RuleFunction)(args_175 => {

R.Call(R.Get(v_123, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"left")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_131, (V)"master") == (V)90.0d))));
R.Call(R.Get(v_123, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"tab")));

R.Call(R.Get(v_123, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_131, (V)"shake") == (V)false))));
R.Call(R.Get(v_123, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"tab")));

R.Call(R.Get(v_123, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_131, (V)"font") == (V)"sans"))));
R.Call(R.Get(v_123, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"tab")));

R.Call(R.Get(v_123, (V)"keypressed"), R.Join(new V[] {}, new Results((V)"right")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_131, (V)"master") == (V)100.0d))));
foreach (var row_176 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"1"), new Entry((V)2, (V)"2"), new Entry((V)3, (V)"3"), new Entry((V)4, (V)"4"), new Entry((V)5, (V)"5"), new Entry((V)6, (V)"6")))))))) {
V v_177 = row_176[0];
V v_178 = row_176[1];
R.Call(R.Get(v_123, (V)"keypressed"), R.Join(new V[] {}, new Results(v_178)));
}
R.Call(R.Call(v_125, R.Join(new V[] {}, new Results((V)"draw_settings"))).First, Results.Empty);
return Results.Empty;
}))))));
V v_180 = values_179[0];
V v_181 = values_179[1];

var values_182 = R.Join(new V[] {}, new Results(V.Nil));
var obj_183 = rt.Global("_G");
V key_184 = (V)"love";
R.Set(obj_183, key_184, values_182[0]);

R.Call(rt.Global("assert"), R.Join(new V[] {v_180}, new Results(v_181)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.Get(v_153, (V)"SOUND"), () => R.Get(v_153, (V)"EFFECTS")), () => R.Get(v_153, (V)"ACCESSIBILITY")), () => (V)(!R.Truth(R.Get(v_153, (V)"SPRITES")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_123, (V)"roagVisualPackMode"), Results.Empty).First == (V)"onebit"), () => (V)(R.Get(v_133, (V)"visual-pack-a.roag") == V.Nil)))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_127))).First}, new Results(v_129)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real refresh retains good resources after corrupt or missing artwork and never alters the run"}, new Results(new V((RuleFunction)(args_185 => {

var values_186 = R.Join(new V[] {}, R.Call(v_37, Results.Empty));
V v_187 = values_186[0];
V v_188 = values_186[1];
V v_189 = values_186[2];

var values_190 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new_stage"), R.Join(new V[] {}, new Results((V)17.0d))));
V v_191 = values_190[0];

var values_192 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_191))));
V v_193 = values_192[0];

var values_194 = R.Join(new V[] {}, new Results((V)false));
V v_195 = values_194[0];
V v_196 = V.Nil;
v_196 = new V((RuleFunction)(args_197 => {
V v_198 = args_197[0];
if (R.Truth(R.Invoke(v_198, "match", R.Join(new V[] {}, new Results((V)"^assets/studio/"))).First)) {
return Results.Empty;
}
if (R.Truth(R.And((V)(v_198 == R.Get(v_7, (V)"path")), () => v_195))) {
if (R.Truth((V)(v_195 == (V)"missing"))) {
return R.Join(new V[] {}, new Results(V.Nil));
}

return R.Join(new V[] {}, new Results((V)"corrupt"));
}
return R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {}, new Results(v_198))));
return Results.Empty;
}));
V v_199 = V.Nil;
v_199 = new V((RuleFunction)(args_200 => {
V v_201 = args_200[0];
return R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"build"), R.Join(new V[] {v_201, v_17}, new Results(new V((RuleFunction)(args_202 => {
V v_203 = args_202[0];
var values_204 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"new"), v_203, v_17}, new Results(v_31))));
V v_205 = values_204[0];
V v_206 = values_204[1];
V v_207 = values_204[2];
if (R.Truth((V)(!R.Truth(v_205)))) {
return R.Join(new V[] {V.Nil}, new Results(R.Concat((V)"rejected: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_206))).First)));
}
return R.Join(new V[] {v_206}, new Results(v_207));
return Results.Empty;
}))))));
return Results.Empty;
}));
var values_208 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"read", v_196))));
var obj_209 = v_187;
V key_210 = (V)"filesystem";
R.Set(obj_209, key_210, values_208[0]);

var values_212 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"setBackgroundColor", new V((RuleFunction)(args_211 => {


return Results.Empty;
}))))));
var obj_213 = v_187;
V key_214 = (V)"graphics";
R.Set(obj_213, key_214, values_212[0]);

var values_215 = R.Join(new V[] {}, R.Call(v_199, R.Join(new V[] {}, new Results(v_196))));
V v_216 = values_215[0];
R.Call(v_188, R.Join(new V[] {(V)"state"}, new Results(v_191)));

R.Call(v_188, R.Join(new V[] {(V)"title"}, new Results((V)false)));

R.Call(v_188, R.Join(new V[] {(V)"visual_assets"}, new Results(v_216)));

R.Call(v_188, R.Join(new V[] {(V)"build_selected_visuals"}, new Results(v_199)));
var values_217 = R.Join(new V[] {}, R.Call(v_189, R.Join(new V[] {}, new Results((V)"refresh_visuals"))));
V v_218 = values_217[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_218, Results.Empty)));

var values_219 = R.Join(new V[] {}, R.Call(v_189, R.Join(new V[] {}, new Results((V)"visual_assets"))));
V v_220 = values_219[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(v_218, Results.Empty).First, () => R.Invoke(R.Call(R.Get(v_187, (V)"roagVisualRefreshStatus"), Results.Empty).First, "match", R.Join(new V[] {}, new Results((V)"checked"))).First))));
var values_221 = R.Join(new V[] {}, R.Call(v_189, R.Join(new V[] {}, new Results((V)"visual_assets"))));
v_220 = values_221[0];

var values_222 = R.Join(new V[] {}, new Results((V)"corrupt"));
v_195 = values_222[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(v_218, Results.Empty).First)), () => (V)(R.Call(v_189, R.Join(new V[] {}, new Results((V)"visual_assets"))).First == v_220)))));
var values_223 = R.Join(new V[] {}, new Results((V)"missing"));
v_195 = values_223[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(v_218, Results.Empty).First)), () => (V)(R.Call(v_189, R.Join(new V[] {}, new Results((V)"visual_assets"))).First == v_220)))));
var values_224 = R.Join(new V[] {}, new Results((V)false));
v_195 = values_224[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(v_218, Results.Empty).First, () => (V)(R.Call(R.Get(v_187, (V)"roagAssetBacking"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_191))).First, (V)"definition_id")))).First == (V)"onebit")))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_191))).First}, new Results(v_193)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"fixed artwork preserves all existing and additive boon bindings and exactly thirteen approved cross-sheet counterparts"}, new Results(new V((RuleFunction)(args_225 => {

var values_226 = R.Join(new V[] {}, new Results(R.Table()));
V v_227 = values_226[0];

V v_228 = V.Nil;
v_228 = new V((RuleFunction)(args_229 => {
V v_230 = args_229[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_230, "find", R.Join(new V[] {}, new Results((V)"micro%-roguelike"))).First)))));

var values_231 = R.Join(new V[] {}, new Results((V)true));
var obj_232 = v_227;
V key_233 = v_230;
R.Set(obj_232, key_233, values_231[0]);

return R.Join(new V[] {}, R.Call(v_10, R.Join(new V[] {}, new Results(v_230))));
return Results.Empty;
}));
var values_234 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {v_228, v_17}, new Results(v_31))))));
V v_235 = values_234[0];

var values_236 = R.Join(new V[] {}, new Results((V)0.0d));
V v_237 = values_236[0];
foreach (var row_238 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))).First, (V)"collect"), Results.Empty).First, (V)"requirements"))))))) {
V v_239 = row_238[0];
V v_240 = row_238[1];
var values_241 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_235, "resolve", R.Join(new V[] {}, new Results(v_240))))));
V v_242 = values_241[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_242, (V)"source") == (V)"onebit"))));
if (R.Truth((V)(R.Get(R.Get(v_242, (V)"texture"), (V)"path") != R.Get(v_7, (V)"path")))) {
var values_243 = R.Join(new V[] {}, new Results((V)(v_237 + (V)1.0d)));
v_237 = values_243[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_242, (V)"monochrome"))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_237 == (V)13.0d), () => (V)(!R.Truth(R.Call(rt.Global("next"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_235, (V)"environment"), (V)"failures")))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_227, (V)"assets/kenney/approved/tiny-dungeon.png"), () => R.Get(v_227, (V)"assets/kenney/approved/roguelike-rpg.png")), () => R.Get(v_227, (V)"assets/kenney/caves-dungeons/terrain.png")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"monochrome shader compiles once and restores the callers exact graphics state"}, new Results(new V((RuleFunction)(args_244 => {

var values_245 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_shader"))).First, (V)"new"), Results.Empty));
V v_246 = values_245[0];

var values_247 = R.Join(new V[] {}, new Results(R.Table()));
V v_248 = values_247[0];

var values_250 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"send", new V((RuleFunction)(args_249 => {


return Results.Empty;
}))))));
V v_251 = values_250[0];

var values_252 = R.Join(new V[] {}, new Results(v_248));
V v_253 = values_252[0];

var values_254 = R.Join(new V[] {}, new Results((V)0.0d));
V v_255 = values_254[0];
var values_263 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"newShader", new V((RuleFunction)(args_256 => {
V v_257 = args_256[0];
var values_258 = R.Join(new V[] {}, new Results((V)(v_255 + (V)1.0d)));
v_255 = values_258[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_257, "find", R.Join(new V[] {(V)"pixel.a", (V)1.0d}, new Results((V)true)))));

return R.Join(new V[] {}, new Results(v_251));
return Results.Empty;
}))), new Entry((V)"getShader", new V((RuleFunction)(args_259 => {

return R.Join(new V[] {}, new Results(v_253));
return Results.Empty;
}))), new Entry((V)"setShader", new V((RuleFunction)(args_260 => {
V v_261 = args_260[0];
var values_262 = R.Join(new V[] {}, new Results(v_261));
v_253 = values_262[0];
return Results.Empty;
}))))));
V v_264 = values_263[0];
{ double end_267 = R.Num((V)40.0d), step_268 = R.Num((V)1.0d);
for (double i_266 = R.Num((V)1.0d); step_268 > 0 ? i_266 <= end_267 : i_266 >= end_267; i_266 += step_268) {
V v_265 = i_266;
var values_269 = R.Join(new V[] {}, R.Invoke(v_246, "begin", R.Join(new V[] {}, new Results(v_264))));
V v_270 = values_269[0];
V v_271 = values_269[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_253 == v_251), () => (V)(v_270 == v_248)))));

R.Invoke(v_246, "finish", R.Join(new V[] {v_264, v_270}, new Results(v_271)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_253 == v_248))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_255 == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"unsupported monochrome shader warns once and allows unshaded fallback rendering"}, new Results(new V((RuleFunction)(args_272 => {

var values_273 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.onebit_shader"))).First, (V)"new"), Results.Empty));
V v_274 = values_273[0];

var values_275 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_276 = values_275[0];
V v_277 = values_275[1];
var values_282 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"newShader", new V((RuleFunction)(args_278 => {

var values_279 = R.Join(new V[] {}, new Results((V)(v_277 + (V)1.0d)));
v_277 = values_279[0];

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"unsupported renderer")));
return Results.Empty;
}))), new Entry((V)"getShader", new V((RuleFunction)(args_280 => {

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"failed compile must not touch graphics state")));
return Results.Empty;
}))), new Entry((V)"setShader", new V((RuleFunction)(args_281 => {

R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"failed compile must not bind")));
return Results.Empty;
}))))));
V v_283 = values_282[0];
{ double end_286 = R.Num((V)40.0d), step_287 = R.Num((V)1.0d);
for (double i_285 = R.Num((V)1.0d); step_287 > 0 ? i_285 <= end_286 : i_285 >= end_286; i_285 += step_287) {
V v_284 = i_285;
var values_290 = R.Join(new V[] {}, R.Invoke(v_274, "begin", R.Join(new V[] {v_283}, new Results(new V((RuleFunction)(args_288 => {

var values_289 = R.Join(new V[] {}, new Results((V)(v_276 + (V)1.0d)));
v_276 = values_289[0];
return Results.Empty;
}))))));
V v_291 = values_290[0];
V v_292 = values_290[1];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_292)))));

R.Invoke(v_274, "finish", R.Join(new V[] {v_283, v_291}, new Results(v_292)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_277 == (V)1.0d), () => (V)(v_276 == (V)1.0d)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
