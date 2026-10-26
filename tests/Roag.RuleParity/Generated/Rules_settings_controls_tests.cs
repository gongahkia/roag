// Static C# port of tests/settings_controls_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: ddca497b204cceba0e13f14074cda9e1fb56e05f26773158dd63783100c04933
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_settings_controls_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.presentation_settings"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ui_fonts"))));
V v_6 = values_5[0];
V v_7 = V.Nil;
v_7 = new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
V v_10 = args_8[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(rt.Global("_G"), (V)"love") == V.Nil))));
var values_11 = R.Join(new V[] {}, new Results(v_9));
var obj_12 = rt.Global("_G");
V key_13 = (V)"love";
R.Set(obj_12, key_13, values_11[0]);
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(v_10))));
V v_15 = values_14[0];
V v_16 = values_14[1];
var values_17 = R.Join(new V[] {}, new Results(V.Nil));
var obj_18 = rt.Global("_G");
V key_19 = (V)"love";
R.Set(obj_18, key_19, values_17[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {v_15}, new Results(v_16)));
return Results.Empty;
}));
V v_20 = V.Nil;
v_20 = new V((RuleFunction)(args_21 => {

var values_22 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"writes", (V)0.0d), new Entry((V)"font_allocations", (V)0.0d), new Entry((V)"audio_changes", (V)0.0d), new Entry((V)"text", R.Table()), new Entry((V)"rects", R.Table()))));
V v_23 = values_22[0];
V v_24 = V.Nil;
v_24 = new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"points", v_26), new Entry((V)"getWidth", new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
V v_29 = args_27[1];
return R.Join(new V[] {}, new Results((V)((V)((V)R.Length(v_29) * v_26) * (V)0.6d)));
return Results.Empty;
}))), new Entry((V)"getHeight", new V((RuleFunction)(args_30 => {

return R.Join(new V[] {}, new Results(v_26));
return Results.Empty;
}))), new Entry((V)"setFilter", new V((RuleFunction)(args_31 => {


return Results.Empty;
}))), new Entry((V)"setFallbacks", new V((RuleFunction)(args_32 => {


return Results.Empty;
}))))));
return Results.Empty;
}));
var values_33 = R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results((V)12.0d))));
V v_34 = values_33[0];
var values_82 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"graphics", R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"newFont", new V((RuleFunction)(args_35 => {
V v_36 = args_35[0];
V v_37 = args_35[1];
var values_38 = R.Join(new V[] {}, new Results((V)(R.Get(v_23, (V)"font_allocations") + (V)1.0d)));
var obj_39 = v_23;
V key_40 = (V)"font_allocations";
R.Set(obj_39, key_40, values_38[0]);
return R.Join(new V[] {}, R.Call(v_24, R.Join(new V[] {}, new Results(R.Or(v_37, () => v_36)))));
return Results.Empty;
}))), new Entry((V)"getFont", new V((RuleFunction)(args_41 => {

return R.Join(new V[] {}, new Results(v_34));
return Results.Empty;
}))), new Entry((V)"setFont", new V((RuleFunction)(args_42 => {
V v_43 = args_42[0];
var values_44 = R.Join(new V[] {}, new Results(v_43));
v_34 = values_44[0];
return Results.Empty;
}))), new Entry((V)"getDimensions", new V((RuleFunction)(args_45 => {

return R.Join(new V[] {(V)1024.0d}, new Results((V)640.0d));
return Results.Empty;
}))), new Entry((V)"getWidth", new V((RuleFunction)(args_46 => {

return R.Join(new V[] {}, new Results((V)1024.0d));
return Results.Empty;
}))), new Entry((V)"print", new V((RuleFunction)(args_47 => {
V v_48 = args_47[0];
V v_49 = args_47[1];
V v_50 = args_47[2];
var values_51 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"text", v_48), new Entry((V)"x", v_49), new Entry((V)"y", v_50), new Entry((V)"font", v_34))));
var obj_52 = R.Get(v_23, (V)"text");
V key_53 = (V)((V)R.Length(R.Get(v_23, (V)"text")) + (V)1.0d);
R.Set(obj_52, key_53, values_51[0]);
return Results.Empty;
}))), new Entry((V)"printf", new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
V v_57 = args_54[2];
V v_58 = args_54[3];
V v_59 = args_54[4];
var values_60 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"text", v_55), new Entry((V)"x", v_56), new Entry((V)"y", v_57), new Entry((V)"width", v_58), new Entry((V)"align", v_59), new Entry((V)"font", v_34))));
var obj_61 = R.Get(v_23, (V)"text");
V key_62 = (V)((V)R.Length(R.Get(v_23, (V)"text")) + (V)1.0d);
R.Set(obj_61, key_62, values_60[0]);
return Results.Empty;
}))), new Entry((V)"rectangle", new V((RuleFunction)(args_63 => {
V v_64 = args_63[0];
V v_65 = args_63[1];
V v_66 = args_63[2];
V v_67 = args_63[3];
V v_68 = args_63[4];
var values_69 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"mode", v_64), new Entry((V)"x", v_65), new Entry((V)"y", v_66), new Entry((V)"w", v_67), new Entry((V)"h", v_68))));
var obj_70 = R.Get(v_23, (V)"rects");
V key_71 = (V)((V)R.Length(R.Get(v_23, (V)"rects")) + (V)1.0d);
R.Set(obj_70, key_71, values_69[0]);
return Results.Empty;
}))))}, new Results(R.Table(new Entry((V)"__index", new V((RuleFunction)(args_72 => {

return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_73 => {


return Results.Empty;
}))));
return Results.Empty;
}))))))).First), new Entry((V)"audio", R.Table(new Entry((V)"setVolume", new V((RuleFunction)(args_74 => {
V v_75 = args_74[0];
var values_76 = R.Join(new V[] {}, new Results(v_75));
var obj_77 = v_23;
V key_78 = (V)"master";
R.Set(obj_77, key_78, values_76[0]);

var values_79 = R.Join(new V[] {}, new Results((V)(R.Get(v_23, (V)"audio_changes") + (V)1.0d)));
var obj_80 = v_23;
V key_81 = (V)"audio_changes";
R.Set(obj_80, key_81, values_79[0]);
return Results.Empty;
}))))))));
V v_83 = values_82[0];
var values_84 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(rt.Global("loadfile"), R.Join(new V[] {}, new Results((V)"main.lua"))))));
V v_85 = values_84[0];
R.Call(rt.Global("setfenv"), R.Join(new V[] {v_85}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"love", v_83))}, new Results(R.Table(new Entry((V)"__index", rt.Global("_G"))))))));

R.Call(v_85, Results.Empty);
V v_86 = V.Nil;
v_86 = new V((RuleFunction)(args_87 => {
V v_88 = args_87[0];
V v_89 = args_87[1];
V v_90 = args_87[2];
if (R.Truth(R.Get(v_90, v_88))) {
return Results.Empty;
}

var values_91 = R.Join(new V[] {}, new Results((V)true));
var obj_92 = v_90;
V key_93 = v_88;
R.Set(obj_92, key_93, values_91[0]);
var values_94 = R.Join(new V[] {}, new Results(R.Table()));
V v_95 = values_94[0];
{ double end_98 = R.Num((V)200.0d), step_99 = R.Num((V)1.0d);
for (double i_97 = R.Num((V)1.0d); step_99 > 0 ? i_97 <= end_98 : i_97 >= end_98; i_97 += step_99) {
V v_96 = i_97;
var values_100 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("debug"), (V)"getupvalue"), R.Join(new V[] {v_88}, new Results(v_96))));
V v_101 = values_100[0];
V v_102 = values_100[1];

if (R.Truth((V)(!R.Truth(v_101)))) {
break;
}
R.Call(v_89, R.Join(new V[] {v_88, v_96, v_101}, new Results(v_102)));
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_102))).First == (V)"function"))) {
var values_103 = R.Join(new V[] {}, new Results(v_102));
var obj_104 = v_95;
V key_105 = (V)((V)R.Length(v_95) + (V)1.0d);
R.Set(obj_104, key_105, values_103[0]);
}
}
}
foreach (var row_106 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_95)))))) {
V v_107 = row_106[0];
V v_108 = row_106[1];
R.Call(v_86, R.Join(new V[] {v_108, v_89}, new Results(v_90)));
}
return Results.Empty;
}));
V v_109 = V.Nil;
v_109 = new V((RuleFunction)(args_110 => {
V v_111 = args_110[0];
var values_112 = R.Join(new V[] {}, new Results(R.Table()));
V v_113 = values_112[0];
foreach (var row_114 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"draw"), new Entry((V)2, (V)"update"), new Entry((V)3, (V)"keypressed"), new Entry((V)4, (V)"mousepressed"), new Entry((V)5, (V)"mousemoved"), new Entry((V)6, (V)"mousereleased"), new Entry((V)7, (V)"focus"), new Entry((V)8, (V)"wheelmoved")))))))) {
V v_115 = row_114[0];
V v_116 = row_114[1];
R.Call(v_86, R.Join(new V[] {R.Get(v_83, v_116), v_111}, new Results(v_113)));
}
return Results.Empty;
}));
R.Set(v_23, (V)"set", new V((RuleFunction)(args_117 => {
V v_118 = args_117[0];
V v_119 = args_117[1];
var values_120 = R.Join(new V[] {}, new Results((V)false));
V v_121 = values_120[0];
R.Call(v_109, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_122 => {
V v_123 = args_122[0];
V v_124 = args_122[1];
V v_125 = args_122[2];
if (R.Truth((V)(v_125 == v_118))) {
R.Call(R.Get(rt.Global("debug"), (V)"setupvalue"), R.Join(new V[] {v_123, v_124}, new Results(v_119)));

var values_126 = R.Join(new V[] {}, new Results((V)true));
v_121 = values_126[0];
}
return Results.Empty;
})))));
R.Call(rt.Global("assert"), R.Join(new V[] {v_121}, new Results(R.Concat((V)"missing fixture upvalue: ", v_118))));
if (R.Truth(rt.Global("jit"))) {
R.Call(R.Get(rt.Global("jit"), (V)"flush"), Results.Empty);
}
return Results.Empty;
})));
R.Set(v_23, (V)"get", new V((RuleFunction)(args_127 => {
V v_128 = args_127[0];
var values_129 = Results.Empty;
V v_130 = values_129[0];
R.Call(v_109, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_131 => {
V v_132 = args_131[0];
V v_133 = args_131[1];
V v_134 = args_131[2];
V v_135 = args_131[3];
if (R.Truth((V)(v_134 == v_128))) {
var values_136 = R.Join(new V[] {}, new Results(v_135));
v_130 = values_136[0];
}
return Results.Empty;
})))));
return R.Join(new V[] {}, new Results(v_130));
return Results.Empty;
})));
var values_137 = R.Join(new V[] {}, new Results(R.Table()));
V v_138 = values_137[0];
R.Set(v_138, "read", new V((RuleFunction)(args_139 => {
V v_140 = args_139[0];
V v_141 = args_139[1];
return R.Join(new V[] {}, new Results(R.Get(v_140, v_141)));
return Results.Empty;
})));
R.Set(v_138, "write", new V((RuleFunction)(args_142 => {
V v_143 = args_142[0];
V v_144 = args_142[1];
V v_145 = args_142[2];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_144 == R.Get(v_2, (V)"file"))}, new Results((V)"settings must not write saves or profiles")));
if (R.Truth(R.Get(v_23, (V)"fail_write"))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_146 = R.Join(new V[] {}, new Results(v_145));
var obj_147 = v_143;
V key_148 = v_144;
R.Set(obj_147, key_148, values_146[0]);

var values_149 = R.Join(new V[] {}, new Results((V)(R.Get(v_23, (V)"writes") + (V)1.0d)));
var obj_150 = v_23;
V key_151 = (V)"writes";
R.Set(obj_150, key_151, values_149[0]);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results((V)17.0d))));
var obj_153 = v_23;
V key_154 = (V)"state";
R.Set(obj_153, key_154, values_152[0]);

var values_155 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"defaults"), Results.Empty));
var obj_156 = v_23;
V key_157 = (V)"settings";
R.Set(obj_156, key_157, values_155[0]);

var values_158 = R.Join(new V[] {}, new Results(v_138));
var obj_159 = v_23;
V key_160 = (V)"disk";
R.Set(obj_159, key_160, values_158[0]);

var values_161 = R.Join(new V[] {}, new Results(v_83));
var obj_162 = v_23;
V key_163 = (V)"love";
R.Set(obj_162, key_163, values_161[0]);
R.Call(R.Get(v_23, (V)"set"), R.Join(new V[] {(V)"state"}, new Results(R.Get(v_23, (V)"state"))));

R.Call(R.Get(v_23, (V)"set"), R.Join(new V[] {(V)"title"}, new Results((V)true)));

R.Call(R.Get(v_23, (V)"set"), R.Join(new V[] {(V)"title_view"}, new Results((V)"settings")));
R.Call(R.Get(v_23, (V)"set"), R.Join(new V[] {(V)"storage"}, new Results(v_138)));

R.Call(R.Get(v_23, (V)"set"), R.Join(new V[] {(V)"presentation_settings"}, new Results(R.Get(v_23, (V)"settings"))));
R.Call(R.Get(v_23, (V)"set"), R.Join(new V[] {(V)"ui_body_font"}, R.Call(v_24, R.Join(new V[] {}, new Results((V)12.0d)))));

R.Call(R.Get(v_23, (V)"set"), R.Join(new V[] {(V)"ui_display_font"}, R.Call(v_24, R.Join(new V[] {}, new Results((V)24.0d)))));

R.Call(R.Get(v_23, (V)"set"), R.Join(new V[] {(V)"ui_menu_font"}, R.Call(v_24, R.Join(new V[] {}, new Results((V)16.0d)))));
R.Call(R.Get(v_23, (V)"set"), R.Join(new V[] {(V)"audio_runtime"}, new Results(R.Table(new Entry((V)"set_user_mix", new V((RuleFunction)(args_164 => {
V v_165 = args_164[0];
V v_166 = args_164[1];
V v_167 = args_164[2];
var values_168 = R.Join(new V[] {v_166}, new Results(v_167));
var obj_169 = v_23;
V key_170 = (V)"music";
var obj_171 = v_23;
V key_172 = (V)"effects";
R.Set(obj_169, key_170, values_168[0]);
R.Set(obj_171, key_172, values_168[1]);
return Results.Empty;
}))), new Entry((V)"emit", new V((RuleFunction)(args_173 => {


return Results.Empty;
})))))));
return R.Join(new V[] {}, new Results(v_23));
return Results.Empty;
}));
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_174 => {
V v_175 = args_174[0];
V v_176 = args_174[1];
R.Call(v_175, R.Join(new V[] {(V)"volume percentages and all three text sizes persist with existing preference compatibility"}, new Results(new V((RuleFunction)(args_177 => {

var values_188 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"read", new V((RuleFunction)(args_178 => {
V v_179 = args_178[0];
V v_180 = args_178[1];
return R.Join(new V[] {}, new Results(R.Get(v_179, v_180)));
return Results.Empty;
}))), new Entry((V)"write", new V((RuleFunction)(args_181 => {
V v_182 = args_181[0];
V v_183 = args_181[1];
V v_184 = args_181[2];
var values_185 = R.Join(new V[] {}, new Results(v_184));
var obj_186 = v_182;
V key_187 = v_183;
R.Set(obj_186, key_187, values_185[0]);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}))))));
V v_189 = values_188[0];
foreach (var row_190 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"small"), new Entry((V)2, (V)"normal"), new Entry((V)3, (V)"large")))))))) {
V v_191 = row_190[0];
V v_192 = row_190[1];
foreach (var row_193 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)37.0d), new Entry((V)4, (V)99.0d), new Entry((V)5, (V)100.0d)))))))) {
V v_194 = row_193[0];
V v_195 = row_193[1];
var values_196 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"defaults"), Results.Empty));
V v_197 = values_196[0];

var values_198 = R.Join(new V[] {}, new Results(v_192));
var obj_199 = v_197;
V key_200 = (V)"text_size";
R.Set(obj_199, key_200, values_198[0]);
var values_201 = R.Join(new V[] {v_195, v_195}, new Results(v_195));
var obj_202 = v_197;
V key_203 = (V)"master";
var obj_204 = v_197;
V key_205 = (V)"music";
var obj_206 = v_197;
V key_207 = (V)"effects";
R.Set(obj_202, key_203, values_201[0]);
R.Set(obj_204, key_205, values_201[1]);
R.Set(obj_206, key_207, values_201[2]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"save"), R.Join(new V[] {v_189}, new Results(v_197)))));
var values_208 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"load"), R.Join(new V[] {}, new Results(v_189))));
V v_209 = values_208[0];
V v_210 = values_208[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_210 == (V)"loaded"))));

R.Call(v_176, R.Join(new V[] {v_209}, new Results(v_197)));
}
}
foreach (var row_211 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"normal"), new Entry((V)2, (V)"large")))))))) {
V v_212 = row_211[0];
V v_213 = row_211[1];
var values_214 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"defaults"), Results.Empty));
V v_215 = values_214[0];

var values_216 = R.Join(new V[] {}, new Results(v_213));
var obj_217 = v_215;
V key_218 = (V)"text_size";
R.Set(obj_217, key_218, values_216[0]);
var values_219 = R.Join(new V[] {}, R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))).First, (V)"encode"), R.Join(new V[] {}, new Results(v_215))));
var obj_220 = v_189;
V key_221 = R.Get(v_2, (V)"file");
R.Set(obj_220, key_221, values_219[0]);
var values_222 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"load"), R.Join(new V[] {}, new Results(v_189))));
V v_223 = values_222[0];
V v_224 = values_222[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_224 == (V)"loaded"))));

R.Call(v_176, R.Join(new V[] {v_223}, new Results(v_215)));
}
foreach (var row_225 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)100.1d), new Entry((V)3, (V)101.0d), new Entry((V)4, (V)55.5d), new Entry((V)5, (V)"50"), new Entry((V)6, R.Get(rt.Global("math"), (V)"huge")), new Entry((V)7, (V)((V)0.0d / (V)0.0d))))))))) {
V v_226 = row_225[0];
V v_227 = row_225[1];
var values_228 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"defaults"), Results.Empty));
V v_229 = values_228[0];

var values_230 = R.Join(new V[] {}, new Results(v_227));
var obj_231 = v_229;
V key_232 = (V)"music";
R.Set(obj_231, key_232, values_230[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_2, (V)"save"), R.Join(new V[] {v_189}, new Results(v_229))).First)))));
}
var values_233 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"defaults"), Results.Empty));
V v_234 = values_233[0];

var values_235 = R.Join(new V[] {}, new Results((V)"tiny"));
var obj_236 = v_234;
V key_237 = (V)"text_size";
R.Set(obj_236, key_237, values_235[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_2, (V)"save"), R.Join(new V[] {v_189}, new Results(v_234))).First)))));
return Results.Empty;
})))));
R.Call(v_175, R.Join(new V[] {(V)"small normal and large load distinct UI fonts with safe glyph fallback in both faces"}, new Results(new V((RuleFunction)(args_238 => {

R.Call(v_7, R.Join(new V[] {R.Table(new Entry((V)"graphics", R.Table()))}, new Results(new V((RuleFunction)(args_239 => {

var values_240 = R.Join(new V[] {}, new Results(R.Table()));
V v_241 = values_240[0];

var values_242 = R.Join(new V[] {}, new Results(R.Table()));
V v_243 = values_242[0];
var values_245 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_244 => {

return R.Join(new V[] {}, new Results(v_241));
return Results.Empty;
}))));
var obj_246 = R.Get(rt.Global("love"), (V)"graphics");
V key_247 = (V)"getFont";
R.Set(obj_246, key_247, values_245[0]);
var values_269 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_248 => {
V v_249 = args_248[0];
V v_250 = args_248[1];
var values_251 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"points", R.Or(v_250, () => v_249)))));
V v_252 = values_251[0];
R.Set(v_252, "setFilter", new V((RuleFunction)(args_253 => {
V v_254 = args_253[0];
V v_255 = args_253[1];
V v_256 = args_253[2];
var values_257 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_255), new Entry((V)2, v_256))));
var obj_258 = v_254;
V key_259 = (V)"filter";
R.Set(obj_258, key_259, values_257[0]);
return Results.Empty;
})));
R.Set(v_252, "setFallbacks", new V((RuleFunction)(args_260 => {
V v_261 = args_260[0];
V v_262 = args_260[1];
var values_263 = R.Join(new V[] {}, new Results(v_262));
var obj_264 = v_261;
V key_265 = (V)"fallback";
R.Set(obj_264, key_265, values_263[0]);
return Results.Empty;
})));
var values_266 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", v_249), new Entry((V)"face", v_252))));
var obj_267 = v_243;
V key_268 = (V)((V)R.Length(v_243) + (V)1.0d);
R.Set(obj_267, key_268, values_266[0]);

return R.Join(new V[] {}, new Results(v_252));
return Results.Empty;
}))));
var obj_270 = R.Get(rt.Global("love"), (V)"graphics");
V key_271 = (V)"newFont";
R.Set(obj_270, key_271, values_269[0]);
foreach (var row_272 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"pixel"), new Entry((V)2, (V)"sans")))))))) {
V v_273 = row_272[0];
V v_274 = row_272[1];
foreach (var row_275 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"small", R.Table(new Entry((V)1, (V)10.0d), new Entry((V)2, (V)20.0d), new Entry((V)3, (V)14.0d))), new Entry((V)"normal", R.Table(new Entry((V)1, (V)12.0d), new Entry((V)2, (V)24.0d), new Entry((V)3, (V)16.0d))), new Entry((V)"large", R.Table(new Entry((V)1, (V)14.0d), new Entry((V)2, (V)26.0d), new Entry((V)3, (V)18.0d)))))))))) {
V v_276 = row_275[0];
V v_277 = row_275[1];
var values_278 = R.Join(new V[] {}, new Results(R.Table(new Entry(1, R.Call(R.Get(v_6, (V)"load"), R.Join(new V[] {v_274}, new Results(v_276)))))));
V v_279 = values_278[0];
foreach (var row_280 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_279)))))) {
V v_281 = row_280[0];
V v_282 = row_280[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_282, (V)"points") == R.Get(v_277, v_281)), () => (V)(R.Get(v_282, (V)"fallback") == v_241)))));
if (R.Truth((V)(v_274 == (V)"pixel"))) {
R.Call(v_176, R.Join(new V[] {R.Get(v_282, (V)"filter")}, new Results(R.Table(new Entry((V)1, (V)"nearest"), new Entry((V)2, (V)"nearest")))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_282, (V)"filter") == V.Nil))));
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_243) == (V)18.0d))));
return Results.Empty;
})))));
return Results.Empty;
})))));
R.Call(v_175, R.Join(new V[] {(V)"real volume sliders drag clamp render and save without font churn or authoritative changes"}, new Results(new V((RuleFunction)(args_283 => {

var values_284 = R.Join(new V[] {}, R.Call(v_20, Results.Empty));
V v_285 = values_284[0];

var values_286 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_285, (V)"state")))));
V v_287 = values_286[0];
R.Call(v_7, R.Join(new V[] {R.Get(v_285, (V)"love")}, new Results(new V((RuleFunction)(args_288 => {

R.Call(R.Get(rt.Global("love"), (V)"mousepressed"), R.Join(new V[] {(V)164.0d, (V)228.0d}, new Results((V)1.0d)));
R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(V)770.0d}, new Results((V)228.0d)));

R.Call(R.Get(rt.Global("love"), (V)"mousereleased"), R.Join(new V[] {(V)770.0d, (V)228.0d}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_285, (V)"writes") == (V)0.0d), () => (V)(R.Get(R.Get(v_285, (V)"settings"), (V)"master") == (V)100.0d)))));
R.Call(R.Get(rt.Global("love"), (V)"mousepressed"), R.Join(new V[] {(V)625.0d, (V)228.0d}, new Results((V)2.0d)));

R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(V)480.0d}, new Results((V)228.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_285, (V)"settings"), (V)"master") == (V)100.0d))));
foreach (var row_289 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"master"), new Entry((V)2, (V)"music"), new Entry((V)3, (V)"effects")))))))) {
V v_290 = row_289[0];
V v_291 = row_289[1];
var values_292 = R.Join(new V[] {}, new Results((V)((V)228.0d + (V)((V)(v_290 - (V)1.0d) * (V)70.0d))));
V v_293 = values_292[0];
var values_294 = R.Join(new V[] {}, new Results(R.Get(v_285, (V)"writes")));
V v_295 = values_294[0];
R.Call(R.Get(rt.Global("love"), (V)"mousepressed"), R.Join(new V[] {(V)625.0d, v_293}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_285, (V)"settings"), v_291) == (V)50.0d), () => (V)(R.Get(v_285, (V)"writes") == v_295)))));
var values_296 = R.Join(new V[] {}, new Results(R.Get(v_285, (V)"audio_changes")));
V v_297 = values_296[0];
{ double end_300 = R.Num((V)200.0d), step_301 = R.Num((V)1.0d);
for (double i_299 = R.Num((V)1.0d); step_301 > 0 ? i_299 <= end_300 : i_299 >= end_300; i_299 += step_301) {
V v_298 = i_299;
R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(V)625.1d}, new Results(v_293)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_285, (V)"settings"), v_291) == (V)50.0d), () => (V)(R.Get(v_285, (V)"audio_changes") == v_297)))));
R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(-(V)100.0d)}, new Results(v_293)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_285, (V)"settings"), v_291) == (V)0.0d))));
R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(V)2000.0d}, new Results(v_293)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_285, (V)"settings"), v_291) == (V)100.0d))));
R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(V)587.3d}, new Results(v_293)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_285, (V)"settings"), v_291) == (V)37.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_285, (V)"writes") == v_295))));
R.Call(R.Get(rt.Global("love"), (V)"mousereleased"), R.Join(new V[] {(V)587.3d, v_293}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_285, (V)"writes") == (V)(v_295 + (V)1.0d)))));
var values_302 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"load"), R.Join(new V[] {}, new Results(R.Get(v_285, (V)"disk")))));
V v_303 = values_302[0];
V v_304 = values_302[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_304 == (V)"loaded"), () => (V)(R.Get(v_303, v_291) == (V)37.0d)))));
R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(V)480.0d}, new Results(v_293)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_285, (V)"settings"), v_291) == (V)37.0d))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_285, (V)"font_allocations") == (V)0.0d), () => (V)(R.Get(v_285, (V)"master") == (V)0.37d)), () => (V)(R.Get(v_285, (V)"music") == (V)0.37d)), () => (V)(R.Get(v_285, (V)"effects") == (V)0.37d)))));
R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"right")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_285, (V)"settings"), (V)"effects") == (V)47.0d), () => (V)(R.Get(v_285, (V)"font_allocations") == (V)0.0d)))));
R.Call(R.Call(R.Get(v_285, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_settings"))).First, Results.Empty);
var values_305 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_306 = values_305[0];
V v_307 = values_305[1];
foreach (var row_308 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_285, (V)"text"))))))) {
V v_309 = row_308[0];
V v_310 = row_308[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_310, (V)"text") != (V)"+"), () => (V)(R.Get(v_310, (V)"text") != (V)"\u00e2\u0088\u0092")))));
if (R.Truth(R.Invoke(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_310, (V)"text")))).First, "match", R.Join(new V[] {}, new Results((V)"^%d+%%$"))).First)) {
var values_311 = R.Join(new V[] {}, new Results((V)(v_306 + (V)1.0d)));
v_306 = values_311[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_310, (V)"x") == (V)794.0d), () => (V)(R.Get(v_310, (V)"width") == (V)65.0d)), () => (V)(R.Get(v_310, (V)"align") == (V)"right")))));
}
if (R.Truth((V)(R.Get(v_310, (V)"text") == (V)"Music & ambience"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)(R.Get(v_310, (V)"x") + R.Invoke(R.Get(v_310, (V)"font"), "getWidth", R.Join(new V[] {}, new Results(R.Get(v_310, (V)"text")))).First) < (V)480.0d))));
}
}
foreach (var row_312 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_285, (V)"rects"))))))) {
V v_313 = row_312[0];
V v_314 = row_312[1];
if (R.Truth(R.And((V)(R.Get(v_314, (V)"w") == (V)10.0d), () => (V)(R.Get(v_314, (V)"h") == (V)22.0d)))) {
var values_315 = R.Join(new V[] {}, new Results((V)(v_307 + (V)1.0d)));
v_307 = values_315[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_314, (V)"x") >= (V)475.0d), () => (V)((V)(R.Get(v_314, (V)"x") + R.Get(v_314, (V)"w")) < (V)794.0d)))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_306 == (V)3.0d), () => (V)(v_307 == (V)3.0d)))));
return Results.Empty;
})))));
R.Call(v_176, R.Join(new V[] {R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_285, (V)"state")))).First}, new Results(v_287)));
return Results.Empty;
})))));
R.Call(v_175, R.Join(new V[] {(V)"settings navigation commits drags safely and small text cycles and reopens from title and pause"}, new Results(new V((RuleFunction)(args_316 => {

var values_317 = R.Join(new V[] {}, R.Call(v_20, Results.Empty));
V v_318 = values_317[0];

var values_319 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_318, (V)"state")))));
V v_320 = values_319[0];
R.Call(v_7, R.Join(new V[] {R.Get(v_318, (V)"love")}, new Results(new V((RuleFunction)(args_321 => {

R.Call(R.Get(rt.Global("love"), (V)"mousepressed"), R.Join(new V[] {(V)596.0d, (V)228.0d}, new Results((V)1.0d)));

R.Call(R.Get(rt.Global("love"), (V)"focus"), R.Join(new V[] {}, new Results((V)false)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"master") == (V)40.0d), () => (V)(R.Get(v_318, (V)"writes") == (V)1.0d)))));
R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(V)770.0d}, new Results((V)228.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"master") == (V)40.0d))));
R.Call(R.Get(rt.Global("love"), (V)"mousepressed"), R.Join(new V[] {(V)625.0d, (V)298.0d}, new Results((V)1.0d)));

R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"tab")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"music") == (V)50.0d), () => (V)(R.Get(v_318, (V)"writes") == (V)2.0d)))));
R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(V)770.0d}, new Results((V)298.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"music") == (V)50.0d))));
R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"tab")));

R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"down")));
R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"left")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"text_size") == (V)"small"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_2, (V)"load"), R.Join(new V[] {}, new Results(R.Get(v_318, (V)"disk")))).First, (V)"text_size") == (V)"small"))));
R.Call(R.Call(R.Get(v_318, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_settings"))).First, Results.Empty);
var values_322 = R.Join(new V[] {}, new Results((V)false));
V v_323 = values_322[0];
foreach (var row_324 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_318, (V)"text"))))))) {
V v_325 = row_324[0];
V v_326 = row_324[1];
if (R.Truth((V)(R.Get(v_326, (V)"text") == (V)"SMALL"))) {
var values_327 = R.Join(new V[] {}, new Results((V)true));
v_323 = values_327[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_323)));
R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"return")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"text_size") == (V)"normal"))));
R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"right")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"text_size") == (V)"large"))));
R.Call(R.Get(rt.Global("love"), (V)"mousepressed"), R.Join(new V[] {(V)600.0d, (V)298.0d}, new Results((V)1.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"text_size") == (V)"small"))));
R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"escape")));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(rt.Global("love"), (V)"roagUIState"), Results.Empty).First, (V)"page") == (V)"home"))));
R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(V)770.0d}, new Results((V)228.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"master") == (V)40.0d))));
R.Call(R.Get(v_318, (V)"set"), R.Join(new V[] {(V)"title"}, new Results((V)false)));

R.Call(R.Get(v_318, (V)"set"), R.Join(new V[] {(V)"pause_menu"}, new Results((V)true)));

R.Call(R.Get(v_318, (V)"set"), R.Join(new V[] {(V)"pause_settings"}, new Results((V)true)));
R.Call(R.Get(v_318, (V)"set"), R.Join(new V[] {(V)"settings_page"}, new Results((V)1.0d)));

var values_328 = R.Join(new V[] {}, new Results((V)true));
var obj_329 = v_318;
V key_330 = (V)"fail_write";
R.Set(obj_329, key_330, values_328[0]);
R.Call(R.Get(rt.Global("love"), (V)"mousepressed"), R.Join(new V[] {(V)625.0d, (V)368.0d}, new Results((V)1.0d)));

R.Call(R.Get(rt.Global("love"), (V)"mousereleased"), R.Join(new V[] {(V)625.0d, (V)368.0d}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"effects") == (V)50.0d), () => R.Invoke(R.Call(R.Get(v_318, (V)"get"), R.Join(new V[] {}, new Results((V)"settings_message"))).First, "find", R.Join(new V[] {(V)"not saved", (V)1.0d}, new Results((V)true))).First))));
var values_331 = R.Join(new V[] {}, new Results((V)false));
var obj_332 = v_318;
V key_333 = (V)"fail_write";
R.Set(obj_332, key_333, values_331[0]);
R.Call(R.Get(rt.Global("love"), (V)"mousepressed"), R.Join(new V[] {(V)683.0d, (V)368.0d}, new Results((V)1.0d)));

R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"escape")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"effects") == (V)70.0d), () => (V)(!R.Truth(R.Call(R.Get(v_318, (V)"get"), R.Join(new V[] {}, new Results((V)"pause_settings"))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_2, (V)"load"), R.Join(new V[] {}, new Results(R.Get(v_318, (V)"disk")))).First, (V)"effects") == (V)70.0d))));
R.Call(R.Get(rt.Global("love"), (V)"mousemoved"), R.Join(new V[] {(V)770.0d}, new Results((V)368.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_318, (V)"settings"), (V)"effects") == (V)70.0d))));
return Results.Empty;
})))));
R.Call(v_176, R.Join(new V[] {R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_318, (V)"state")))).First}, new Results(v_320)));
return Results.Empty;
})))));
R.Call(v_175, R.Join(new V[] {(V)"inventory groups utility and close instructions in the boon footer with matching click target"}, new Results(new V((RuleFunction)(args_334 => {

var values_335 = R.Join(new V[] {}, R.Call(v_20, Results.Empty));
V v_336 = values_335[0];
{ double end_339 = R.Num((V)7.0d), step_340 = R.Num((V)1.0d);
for (double i_338 = R.Num((V)1.0d); step_340 > 0 ? i_338 <= end_339 : i_338 >= end_339; i_338 += step_340) {
V v_337 = i_338;
R.Call(R.Get(v_4, (V)"set_boon_stacks"), R.Join(new V[] {R.Get(v_336, (V)"state"), R.Get(R.Get(v_336, (V)"state"), (V)"player_id"), R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))).First, (V)"order"), v_337), (V)"common"}, new Results((V)1.0d)));
}
}
var values_341 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_336, (V)"state")))));
V v_342 = values_341[0];
R.Call(R.Get(v_336, (V)"set"), R.Join(new V[] {(V)"title"}, new Results((V)false)));

R.Call(R.Get(v_336, (V)"set"), R.Join(new V[] {(V)"inventory_open"}, new Results((V)true)));
R.Call(v_7, R.Join(new V[] {R.Get(v_336, (V)"love")}, new Results(new V((RuleFunction)(args_343 => {

R.Call(R.Call(R.Get(v_336, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_inventory"))).First, Results.Empty);
var values_344 = R.Join(new V[] {(V)false, (V)false}, new Results((V)false));
V v_345 = values_344[0];
V v_346 = values_344[1];
V v_347 = values_344[2];
foreach (var row_348 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_336, (V)"text"))))))) {
V v_349 = row_348[0];
V v_350 = row_348[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_350, (V)"text")))).First, "find", R.Join(new V[] {(V)"no world time", (V)1.0d}, new Results((V)true))).First)))));
if (R.Truth((V)(R.Get(v_350, (V)"text") == (V)"U \u00c2\u00b7 UTILITY / ITEMS"))) {
var values_351 = R.Join(new V[] {}, new Results((V)true));
v_345 = values_351[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_350, (V)"x") == (V)88.0d), () => (V)(R.Get(v_350, (V)"y") == (V)492.0d)), () => (V)(R.Get(v_350, (V)"width") == (V)488.0d)))));
} else if (R.Truth((V)(R.Get(v_350, (V)"text") == (V)"I / Esc close"))) {
var values_352 = R.Join(new V[] {}, new Results((V)true));
v_346 = values_352[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_350, (V)"x") == (V)88.0d), () => (V)(R.Get(v_350, (V)"y") == (V)518.0d)))));
} else if (R.Truth((V)(R.Invoke(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_350, (V)"text")))).First, "find", R.Join(new V[] {(V)"PAGE ", (V)1.0d}, new Results((V)true))).First == (V)1.0d))) {
var values_353 = R.Join(new V[] {}, new Results((V)true));
v_347 = values_353[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_350, (V)"y") == (V)466.0d))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_345, () => v_346), () => v_347))));
R.Call(R.Get(rt.Global("love"), (V)"mousepressed"), R.Join(new V[] {(V)700.0d, (V)495.0d}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(rt.Global("love"), (V)"roagUIState"), Results.Empty).First, (V)"page") == (V)"inventory"))));
R.Call(R.Get(rt.Global("love"), (V)"mousepressed"), R.Join(new V[] {(V)300.0d, (V)500.0d}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(rt.Global("love"), (V)"roagUIState"), Results.Empty).First, (V)"page") == (V)"interaction"))));
R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"escape")));

R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"i")));

R.Call(R.Get(rt.Global("love"), (V)"keypressed"), R.Join(new V[] {}, new Results((V)"u")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(rt.Global("love"), (V)"roagUIState"), Results.Empty).First, (V)"page") == (V)"interaction"))));
R.Call(R.Call(R.Get(v_336, (V)"get"), R.Join(new V[] {}, new Results((V)"draw_help"))).First, Results.Empty);
foreach (var row_354 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_336, (V)"text"))))))) {
V v_355 = row_354[0];
V v_356 = row_354[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_356, (V)"text")))).First, "find", R.Join(new V[] {(V)"no world time", (V)1.0d}, new Results((V)true))).First)))));
}
return Results.Empty;
})))));
R.Call(v_176, R.Join(new V[] {R.Call(R.Get(v_4, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_336, (V)"state")))).First}, new Results(v_342)));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
