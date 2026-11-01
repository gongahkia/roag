// Static C# port of src/persistence.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: bd686b11e95f3b3412c586105a4a035a6d2a88ab8ee8ffee4fd95d75de597600
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_persistence { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.world"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results(R.Table()));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, new Results((V)"roag-save/1"));
var obj_14 = v_12;
V key_15 = (V)"format";
R.Set(obj_14, key_15, values_13[0]);
var values_16 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"active-a.roag"), new Entry((V)2, (V)"active-b.roag"))));
var obj_17 = v_12;
V key_18 = (V)"slots";
R.Set(obj_17, key_18, values_16[0]);
var values_19 = R.Join(new V[] {}, new Results((V)((V)((V)32.0d * (V)1024.0d) * (V)1024.0d)));
var obj_20 = v_12;
V key_21 = (V)"max_bytes";
R.Set(obj_20, key_21, values_19[0]);
var values_22 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Table(new Entry((V)1, R.Get(v_4, (V)"version")), new Entry((V)2, R.Get(v_6, (V)"version")), new Entry((V)3, R.Get(v_8, (V)"room_content_version")), new Entry((V)4, R.Get(v_10, (V)"version")))}, new Results((V)"|"))));
var obj_23 = v_12;
V key_24 = (V)"compatibility";
R.Set(obj_23, key_24, values_22[0]);
var values_25 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Table(new Entry((V)1, R.Get(v_4, (V)"version")), new Entry((V)2, R.Get(v_6, (V)"version")), new Entry((V)3, (V)"roag-rooms/2"), new Entry((V)4, R.Get(v_10, (V)"version")))}, new Results((V)"|"))));
var obj_26 = v_12;
V key_27 = (V)"prior_room_compatibility";
R.Set(obj_26, key_27, values_25[0]);
V v_28 = V.Nil;
v_28 = new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
var values_31 = R.Join(new V[] {}, new Results(R.Table()));
V v_32 = values_31[0];
foreach (var row_33 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_30)))))) {
V v_34 = row_33[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_34))).First == (V)"string"), () => R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_34))).First == (V)"number"), () => (V)((V)(v_34 % (V)1.0d) == (V)0.0d)))}, new Results((V)"unsupported table key")));
var values_35 = R.Join(new V[] {}, new Results(v_34));
var obj_36 = v_32;
V key_37 = (V)((V)R.Length(v_32) + (V)1.0d);
R.Set(obj_36, key_37, values_35[0]);
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_32}, new Results(new V((RuleFunction)(args_38 => {
V v_39 = args_38[0];
V v_40 = args_38[1];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_39))).First != R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_40))).First))) {
return R.Join(new V[] {}, new Results((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_39))).First == (V)"number")));
}
return R.Join(new V[] {}, new Results((V)(v_39 < v_40)));
return Results.Empty;
})))));
return R.Join(new V[] {}, new Results(v_32));
return Results.Empty;
}));
V v_41 = V.Nil;
v_41 = new V((RuleFunction)(args_42 => {
V v_43 = args_42[0];
V v_44 = args_42[1];
V v_45 = args_42[2];
V v_46 = args_42[3];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_46 <= (V)100.0d)}, new Results((V)"save nesting limit")));
var values_47 = R.Join(new V[] {}, R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_43))));
V v_48 = values_47[0];
if (R.Truth((V)(v_48 == (V)"nil"))) {
var values_49 = R.Join(new V[] {}, new Results((V)"Z"));
var obj_50 = v_44;
V key_51 = (V)((V)R.Length(v_44) + (V)1.0d);
R.Set(obj_50, key_51, values_49[0]);
} else if (R.Truth((V)(v_48 == (V)"boolean"))) {
var values_52 = R.Join(new V[] {}, new Results(R.Or(R.And(v_43, () => (V)"Y"), () => (V)"N")));
var obj_53 = v_44;
V key_54 = (V)((V)R.Length(v_44) + (V)1.0d);
R.Set(obj_53, key_54, values_52[0]);
} else if (R.Truth((V)(v_48 == (V)"number"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(v_43 == v_43), () => (V)(v_43 != R.Get(rt.Global("math"), (V)"huge"))), () => (V)(v_43 != (-R.Get(rt.Global("math"), (V)"huge"))))}, new Results((V)"nonfinite save number")));
var values_55 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"format"), R.Join(new V[] {(V)"%.17g"}, new Results(v_43))));
V v_56 = values_55[0];
var values_57 = R.Join(new V[] {}, new Results(R.Concat((V)"D", R.Concat((V)R.Length(v_56), R.Concat((V)":", v_56)))));
var obj_58 = v_44;
V key_59 = (V)((V)R.Length(v_44) + (V)1.0d);
R.Set(obj_58, key_59, values_57[0]);
} else if (R.Truth((V)(v_48 == (V)"string"))) {
var values_60 = R.Join(new V[] {}, new Results(R.Concat((V)"S", R.Concat((V)R.Length(v_43), R.Concat((V)":", v_43)))));
var obj_61 = v_44;
V key_62 = (V)((V)R.Length(v_44) + (V)1.0d);
R.Set(obj_61, key_62, values_60[0]);
} else if (R.Truth((V)(v_48 == (V)"table"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_45, v_43)))}, new Results((V)"cyclic save table")));
var values_63 = R.Join(new V[] {}, new Results((V)true));
var obj_64 = v_45;
V key_65 = v_43;
R.Set(obj_64, key_65, values_63[0]);
var values_66 = R.Join(new V[] {}, R.Call(v_28, R.Join(new V[] {}, new Results(v_43))));
V v_67 = values_66[0];
var values_68 = R.Join(new V[] {}, new Results(R.Concat((V)"T", R.Concat((V)R.Length(v_67), (V)":"))));
var obj_69 = v_44;
V key_70 = (V)((V)R.Length(v_44) + (V)1.0d);
R.Set(obj_69, key_70, values_68[0]);
foreach (var row_71 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_67)))))) {
V v_72 = row_71[0];
V v_73 = row_71[1];
R.Call(v_41, R.Join(new V[] {v_73, v_44, v_45}, new Results((V)(v_46 + (V)1.0d))));
R.Call(v_41, R.Join(new V[] {R.Get(v_43, v_73), v_44, v_45}, new Results((V)(v_46 + (V)1.0d))));
}
var values_74 = R.Join(new V[] {}, new Results(V.Nil));
var obj_75 = v_45;
V key_76 = v_43;
R.Set(obj_75, key_76, values_74[0]);
} else {
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results(R.Concat((V)"unsupported save value: ", v_48))));
}
return Results.Empty;
}));
R.Set(v_12, (V)"encode", new V((RuleFunction)(args_77 => {
V v_78 = args_77[0];
var values_79 = R.Join(new V[] {}, new Results(R.Table()));
V v_80 = values_79[0];
R.Call(v_41, R.Join(new V[] {v_78, v_80, R.Table()}, new Results((V)0.0d)));
var values_81 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(v_80))));
V v_82 = values_81[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_82) <= R.Get(v_12, (V)"max_bytes"))}, new Results((V)"save size limit")));
return R.Join(new V[] {}, new Results(v_82));
return Results.Empty;
})));
R.Set(v_12, (V)"decode", new V((RuleFunction)(args_83 => {
V v_84 = args_83[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_84))).First == (V)"string"), () => (V)((V)R.Length(v_84) <= R.Get(v_12, (V)"max_bytes")))}, new Results((V)"invalid save payload size")));
var values_85 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
V v_86 = values_85[0];
V v_87 = values_85[1];
V v_88 = V.Nil;
v_88 = new V((RuleFunction)(args_89 => {

var values_90 = R.Join(new V[] {}, R.Invoke(v_84, "find", R.Join(new V[] {(V)"(%d+):"}, new Results(v_86))));
V v_91 = values_90[0];
V v_92 = values_90[1];
V v_93 = values_90[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(v_91 == v_86), () => v_93), () => (V)((V)R.Length(v_93) <= (V)10.0d))}, new Results((V)"invalid save length")));
var values_94 = R.Join(new V[] {}, new Results((V)(v_92 + (V)1.0d)));
v_86 = values_94[0];
var values_95 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_93))));
V v_96 = values_95[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_96, () => (V)(v_96 <= R.Get(v_12, (V)"max_bytes")))}, new Results((V)"save length limit")));
return R.Join(new V[] {}, new Results(v_96));
return Results.Empty;
}));
var values_97 = Results.Empty;
V v_98 = values_97[0];
var values_127 = R.Join(new V[] {}, new Results(new V((RuleFunction)(args_99 => {
V v_100 = args_99[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_100 <= (V)100.0d)}, new Results((V)"save nesting limit")));
var values_101 = R.Join(new V[] {}, new Results((V)(v_87 + (V)1.0d)));
v_87 = values_101[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_87 <= (V)2000000.0d)}, new Results((V)"save node limit")));
var values_102 = R.Join(new V[] {}, R.Invoke(v_84, "sub", R.Join(new V[] {v_86}, new Results(v_86))));
V v_103 = values_102[0];

var values_104 = R.Join(new V[] {}, new Results((V)(v_86 + (V)1.0d)));
v_86 = values_104[0];
if (R.Truth((V)(v_103 == (V)"Z"))) {
return R.Join(new V[] {}, new Results(V.Nil));
} else if (R.Truth((V)(v_103 == (V)"Y"))) {
return R.Join(new V[] {}, new Results((V)true));
} else if (R.Truth((V)(v_103 == (V)"N"))) {
return R.Join(new V[] {}, new Results((V)false));
} else if (R.Truth(R.Or((V)(v_103 == (V)"S"), () => (V)(v_103 == (V)"D")))) {
var values_105 = R.Join(new V[] {}, R.Call(v_88, Results.Empty));
V v_106 = values_105[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)((V)(v_86 + v_106) - (V)1.0d) <= (V)R.Length(v_84))}, new Results((V)"truncated save value")));
var values_107 = R.Join(new V[] {}, R.Invoke(v_84, "sub", R.Join(new V[] {v_86}, new Results((V)((V)(v_86 + v_106) - (V)1.0d)))));
V v_108 = values_107[0];

var values_109 = R.Join(new V[] {}, new Results((V)(v_86 + v_106)));
v_86 = values_109[0];
if (R.Truth((V)(v_103 == (V)"S"))) {
return R.Join(new V[] {}, new Results(v_108));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.Invoke(v_108, "match", R.Join(new V[] {}, new Results((V)"^[%+%-%d%.eE]+$"))).First}, new Results((V)"invalid save number")));
var values_110 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_108))));
V v_111 = values_110[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(v_111, () => (V)(v_111 == v_111)), () => (V)(v_111 != R.Get(rt.Global("math"), (V)"huge"))), () => (V)(v_111 != (-R.Get(rt.Global("math"), (V)"huge"))))}, new Results((V)"nonfinite save number")));
return R.Join(new V[] {}, new Results(v_111));
} else if (R.Truth((V)(v_103 == (V)"T"))) {
var values_112 = R.Join(new V[] {}, R.Call(v_88, Results.Empty));
V v_113 = values_112[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_113 <= (V)1000000.0d)}, new Results((V)"save table limit")));
var values_114 = R.Join(new V[] {}, new Results(R.Table()));
V v_115 = values_114[0];
{ double end_118 = R.Num(v_113), step_119 = R.Num((V)1.0d);
for (double i_117 = R.Num((V)1.0d); step_119 > 0 ? i_117 <= end_118 : i_117 >= end_118; i_117 += step_119) {
V v_116 = i_117;
var values_120 = R.Join(new V[] {}, R.Call(v_98, R.Join(new V[] {}, new Results((V)(v_100 + (V)1.0d)))));
V v_121 = values_120[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_121))).First == (V)"string"), () => R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_121))).First == (V)"number"), () => (V)((V)(v_121 % (V)1.0d) == (V)0.0d)))}, new Results((V)"invalid save key")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_115, v_121) == V.Nil)}, new Results((V)"duplicate save key")));
var values_122 = R.Join(new V[] {}, R.Call(v_98, R.Join(new V[] {}, new Results((V)(v_100 + (V)1.0d)))));
V v_123 = values_122[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_123 != V.Nil)}, new Results((V)"nil save table value")));
var values_124 = R.Join(new V[] {}, new Results(v_123));
var obj_125 = v_115;
V key_126 = v_121;
R.Set(obj_125, key_126, values_124[0]);
}
}
return R.Join(new V[] {}, new Results(v_115));
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"invalid save tag")));
return Results.Empty;
}))));
v_98 = values_127[0];
var values_128 = R.Join(new V[] {}, R.Call(v_98, R.Join(new V[] {}, new Results((V)0.0d))));
V v_129 = values_128[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_86 == (V)((V)R.Length(v_84) + (V)1.0d))}, new Results((V)"trailing save data")));
return R.Join(new V[] {}, new Results(v_129));
return Results.Empty;
})));
V v_130 = V.Nil;
v_130 = new V((RuleFunction)(args_131 => {
V v_132 = args_131[0];
var values_133 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
V v_134 = values_133[0];
V v_135 = values_133[1];
{ double end_138 = R.Num((V)R.Length(v_132)), step_139 = R.Num((V)1.0d);
for (double i_137 = R.Num((V)1.0d); step_139 > 0 ? i_137 <= end_138 : i_137 >= end_138; i_137 += step_139) {
V v_136 = i_137;
var values_140 = R.Join(new V[] {}, new Results((V)((V)(v_134 + R.Invoke(v_132, "byte", R.Join(new V[] {}, new Results(v_136))).First) % (V)65521.0d)));
v_134 = values_140[0];
var values_141 = R.Join(new V[] {}, new Results((V)((V)(v_135 + v_134) % (V)65521.0d)));
v_135 = values_141[0];
}
}
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"format"), R.Join(new V[] {(V)"%08x"}, new Results((V)((V)(v_135 * (V)65536.0d) + v_134)))));
return Results.Empty;
}));
V v_142 = V.Nil;
v_142 = new V((RuleFunction)(args_143 => {
V v_144 = args_143[0];
V v_145 = args_143[1];
V v_146 = args_143[2];
var values_147 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"encode"), R.Join(new V[] {}, new Results(R.Or(v_146, () => R.Table())))));
V v_148 = values_147[0];
return R.Join(new V[] {}, new Results(R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Table(new Entry((V)1, R.Get(v_12, (V)"format")), new Entry((V)2, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_144))).First), new Entry((V)3, v_145), new Entry((V)4, R.Or(R.And(v_146, () => R.Get(v_146, (V)"schema")), () => (V)"none")), new Entry((V)5, R.Get(v_12, (V)"compatibility")), new Entry((V)6, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results((V)R.Length(v_148)))).First), new Entry(7, R.Call(v_130, R.Join(new V[] {}, new Results(v_148)))))}, new Results((V)"\n"))).First, R.Concat((V)"\n", v_148))));
return Results.Empty;
}));
V v_149 = V.Nil;
v_149 = new V((RuleFunction)(args_150 => {
V v_151 = args_150[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_151))).First == (V)"string"), () => (V)((V)R.Length(v_151) <= (V)(R.Get(v_12, (V)"max_bytes") + (V)512.0d)))}, new Results((V)"invalid save record size")));
var values_152 = R.Join(new V[] {}, new Results(R.Table()));
V v_153 = values_152[0];
var values_154 = R.Join(new V[] {}, new Results((V)1.0d));
V v_155 = values_154[0];
{ double end_158 = R.Num((V)7.0d), step_159 = R.Num((V)1.0d);
for (double i_157 = R.Num((V)1.0d); step_159 > 0 ? i_157 <= end_158 : i_157 >= end_158; i_157 += step_159) {
V v_156 = i_157;
var values_160 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Invoke(v_151, "find", R.Join(new V[] {(V)"\n", v_155}, new Results((V)true))).First}, new Results((V)"truncated save header"))));
V v_161 = values_160[0];
var values_162 = R.Join(new V[] {}, R.Invoke(v_151, "sub", R.Join(new V[] {v_155}, new Results((V)(v_161 - (V)1.0d)))));
var obj_163 = v_153;
V key_164 = v_156;
R.Set(obj_163, key_164, values_162[0]);
var values_165 = R.Join(new V[] {}, new Results((V)(v_161 + (V)1.0d)));
v_155 = values_165[0];
}
}
var values_166 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_153, (V)2.0d)))));
V v_167 = values_166[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(v_167, () => (V)((V)(v_167 % (V)1.0d) == (V)0.0d)), () => (V)(v_167 >= (V)1.0d)), () => (V)(v_167 < (V)9007199254740991.0d))}, new Results((V)"invalid generation")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or((V)(R.Get(v_153, (V)3.0d) == (V)"active"), () => (V)(R.Get(v_153, (V)3.0d) == (V)"terminal")), () => (V)(R.Get(v_153, (V)3.0d) == (V)"pending_terminal"))}, new Results((V)"invalid save kind")));
var values_168 = R.Join(new V[] {}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_153, (V)6.0d)))));
V v_169 = values_168[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(v_169, () => (V)((V)(v_169 % (V)1.0d) == (V)0.0d)), () => (V)(v_169 >= (V)0.0d)), () => (V)(v_169 <= R.Get(v_12, (V)"max_bytes"))), () => (V)((V)((V)((V)R.Length(v_151) - v_155) + (V)1.0d) == v_169))}, new Results((V)"save payload size mismatch")));
var values_170 = R.Join(new V[] {}, R.Invoke(v_151, "sub", R.Join(new V[] {}, new Results(v_155))));
V v_171 = values_170[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(v_130, R.Join(new V[] {}, new Results(v_171))).First == R.Get(v_153, (V)7.0d))}, new Results((V)"save checksum mismatch")));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"format", R.Get(v_153, (V)1.0d)), new Entry((V)"seq", v_167), new Entry((V)"kind", R.Get(v_153, (V)3.0d)), new Entry((V)"schema", R.Get(v_153, (V)4.0d)), new Entry((V)"compatibility", R.Get(v_153, (V)5.0d)), new Entry((V)"snapshot", R.Call(R.Get(v_12, (V)"decode"), R.Join(new V[] {}, new Results(v_171))).First))));
return Results.Empty;
}));
V v_172 = V.Nil;
v_172 = new V((RuleFunction)(args_173 => {
V v_174 = args_173[0];
if (R.Truth((V)(R.Get(v_174, (V)"format") != R.Get(v_12, (V)"format")))) {
return R.Join(new V[] {(V)false}, new Results((V)"incompatible envelope format"));
}
if (R.Truth(R.And((V)(R.Get(v_174, (V)"compatibility") != R.Get(v_12, (V)"compatibility")), () => (V)(R.Get(v_174, (V)"compatibility") != R.Get(v_12, (V)"prior_room_compatibility"))))) {
return R.Join(new V[] {(V)false}, new Results((V)"incompatible content"));
}
if (R.Truth((V)(R.Get(v_174, (V)"kind") == (V)"terminal"))) {
return R.Join(new V[] {}, new Results((V)true));
}
if (R.Truth((V)(R.Get(v_174, (V)"schema") != R.Get(v_2, (V)"snapshot_schema")))) {
return R.Join(new V[] {(V)false}, new Results((V)"incompatible snapshot schema"));
}
var values_175 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"restore")}, new Results(R.Get(v_174, (V)"snapshot")))));
V v_176 = values_175[0];
V v_177 = values_175[1];
if (R.Truth((V)(!R.Truth(v_176)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"invalid save state: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_177))).First)));
}
return R.Join(new V[] {(V)true}, new Results(v_177));
return Results.Empty;
}));
R.Set(v_12, (V)"new", new V((RuleFunction)(args_178 => {
V v_179 = args_178[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_179))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_179, (V)"read")))).First == (V)"function")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_179, (V)"write")))).First == (V)"function"))}, new Results((V)"invalid storage adapter")));
return R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"storage", v_179))}, new Results(R.Table(new Entry((V)"__index", v_12))))));
return Results.Empty;
})));
R.Set(v_12, (V)"inspect", new V((RuleFunction)(args_180 => {
V v_181 = args_180[0];
var values_182 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_183 = values_182[0];
V v_184 = values_182[1];
foreach (var row_185 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_12, (V)"slots"))))))) {
V v_186 = row_185[0];
V v_187 = row_185[1];
var values_188 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_181, (V)"storage"), (V)"read"), R.Get(v_181, (V)"storage")}, new Results(v_187))));
V v_189 = values_188[0];
V v_190 = values_188[1];
if (R.Truth(R.And(v_189, () => (V)(v_190 != V.Nil)))) {
var values_191 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_149}, new Results(v_190))));
V v_192 = values_191[0];
V v_193 = values_191[1];
if (R.Truth(v_192)) {
var values_194 = R.Join(new V[] {}, new Results(v_186));
var obj_195 = v_193;
V key_196 = (V)"slot";
R.Set(obj_195, key_196, values_194[0]);

var values_197 = R.Join(new V[] {}, new Results(v_193));
var obj_198 = v_183;
V key_199 = (V)((V)R.Length(v_183) + (V)1.0d);
R.Set(obj_198, key_199, values_197[0]);
} else {
var values_200 = R.Join(new V[] {}, new Results((V)(v_184 + (V)1.0d)));
v_184 = values_200[0];
}
} else if (R.Truth((V)(!R.Truth(v_189)))) {
var values_201 = R.Join(new V[] {}, new Results((V)(v_184 + (V)1.0d)));
v_184 = values_201[0];
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_183}, new Results(new V((RuleFunction)(args_202 => {
V v_203 = args_202[0];
V v_204 = args_202[1];
if (R.Truth((V)(R.Get(v_203, (V)"seq") != R.Get(v_204, (V)"seq")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_203, (V)"seq") > R.Get(v_204, (V)"seq"))));
}
return R.Join(new V[] {}, new Results((V)(R.Get(v_203, (V)"slot") > R.Get(v_204, (V)"slot"))));
return Results.Empty;
})))));
var values_205 = R.Join(new V[] {}, new Results(R.Get(v_183, (V)1.0d)));
V v_206 = values_205[0];
if (R.Truth((V)(!R.Truth(v_206)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", R.Or(R.And((V)(v_184 > (V)0.0d), () => (V)"corrupt"), () => (V)"absent")), new Entry((V)"invalid", v_184))));
}
var values_207 = R.Join(new V[] {}, R.Call(v_172, R.Join(new V[] {}, new Results(v_206))));
V v_208 = values_207[0];
V v_209 = values_207[1];
if (R.Truth((V)(!R.Truth(v_208)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", R.Or(R.And(R.Invoke(v_209, "find", R.Join(new V[] {(V)"incompatible", (V)1.0d}, new Results((V)true))).First, () => (V)"incompatible"), () => (V)"corrupt")), new Entry((V)"reason", v_209), new Entry((V)"record", v_206), new Entry((V)"invalid", v_184))));
}
var values_210 = R.Join(new V[] {}, new Results((V)(v_184 > (V)0.0d)));
V v_211 = values_210[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"status", R.Or(R.Or(R.And((V)(R.Get(v_206, (V)"kind") == (V)"terminal"), () => (V)"terminal"), () => R.And((V)(R.Get(v_206, (V)"kind") == (V)"pending_terminal"), () => (V)"pending_terminal")), () => R.Or(R.And(v_211, () => (V)"recovered"), () => (V)"active"))), new Entry((V)"record", v_206), new Entry((V)"state", v_209), new Entry((V)"invalid", v_184))));
return Results.Empty;
})));
R.Set(v_12, (V)"load", new V((RuleFunction)(args_212 => {
V v_213 = args_212[0];
var values_214 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"inspect"), R.Join(new V[] {}, new Results(v_213))));
V v_215 = values_214[0];
if (R.Truth(R.Or((V)(R.Get(v_215, (V)"status") == (V)"active"), () => (V)(R.Get(v_215, (V)"status") == (V)"recovered")))) {
return R.Join(new V[] {R.Get(v_215, (V)"state")}, new Results(v_215));
}
return R.Join(new V[] {V.Nil}, new Results(v_215));
return Results.Empty;
})));
V v_216 = V.Nil;
v_216 = new V((RuleFunction)(args_217 => {
V v_218 = args_217[0];
V v_219 = args_217[1];
V v_220 = args_217[2];
var values_221 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"inspect"), R.Join(new V[] {}, new Results(v_218))));
V v_222 = values_221[0];
if (R.Truth((V)(R.Get(v_222, (V)"status") == (V)"incompatible"))) {
return R.Join(new V[] {(V)false}, new Results((V)"incompatible save requires New Run confirmation"));
}
var values_223 = R.Join(new V[] {}, new Results(R.Get(v_222, (V)"record")));
V v_224 = values_223[0];
var values_225 = R.Join(new V[] {}, new Results(R.Or(R.And(v_224, () => (V)((V)3.0d - R.Get(v_224, (V)"slot"))), () => (V)1.0d)));
V v_226 = values_225[0];
var values_227 = R.Join(new V[] {}, new Results(R.Or(R.And(v_224, () => (V)(R.Get(v_224, (V)"seq") + (V)1.0d)), () => (V)1.0d)));
V v_228 = values_227[0];
var values_229 = R.Join(new V[] {}, R.Call(v_142, R.Join(new V[] {v_228, v_219}, new Results(v_220))));
V v_230 = values_229[0];
var values_231 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_12, (V)"slots"), v_226)));
V v_232 = values_231[0];
var values_233 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_218, (V)"storage"), (V)"write"), R.Get(v_218, (V)"storage"), v_232}, new Results(v_230))));
V v_234 = values_233[0];
V v_235 = values_233[1];
if (R.Truth(R.Or((V)(!R.Truth(v_234)), () => (V)(!R.Truth(v_235))))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"save write failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_235))).First)));
}
var values_236 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_218, (V)"storage"), (V)"read"), R.Get(v_218, (V)"storage")}, new Results(v_232))));
V v_237 = values_236[0];
V v_238 = values_236[1];
if (R.Truth(R.Or((V)(!R.Truth(v_237)), () => (V)(v_238 != v_230)))) {
return R.Join(new V[] {(V)false}, new Results((V)"save read-back failed"));
}
var values_239 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_149}, new Results(v_238))));
V v_240 = values_239[0];
V v_241 = values_239[1];
if (R.Truth(R.Or((V)(!R.Truth(v_240)), () => (V)(R.Get(v_241, (V)"seq") != v_228)))) {
return R.Join(new V[] {(V)false}, new Results((V)"save verification failed"));
}
return R.Join(new V[] {(V)true}, new Results(v_228));
return Results.Empty;
}));
R.Set(v_12, (V)"save", new V((RuleFunction)(args_242 => {
V v_243 = args_242[0];
V v_244 = args_242[1];
if (R.Truth(R.Get(v_244, (V)"game_over"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"tombstone"), R.Join(new V[] {}, new Results(v_243))));
}
var values_245 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_244))));
V v_246 = values_245[0];
var values_247 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"restore")}, new Results(v_246))));
V v_248 = values_247[0];
V v_249 = values_247[1];
if (R.Truth((V)(!R.Truth(v_248)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"unstable save boundary: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_249))).First)));
}
return R.Join(new V[] {}, R.Call(v_216, R.Join(new V[] {v_243, (V)"active"}, new Results(v_246))));
return Results.Empty;
})));
R.Set(v_12, (V)"pending_terminal", new V((RuleFunction)(args_250 => {
V v_251 = args_250[0];
V v_252 = args_250[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_252, (V)"game_over")}, new Results((V)"pending terminal requires finished run")));
var values_253 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_252))));
V v_254 = values_253[0];
var values_255 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"restore")}, new Results(v_254))));
V v_256 = values_255[0];
V v_257 = values_255[1];
if (R.Truth((V)(!R.Truth(v_256)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"unstable terminal boundary: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_257))).First)));
}
var values_258 = R.Join(new V[] {}, R.Call(v_216, R.Join(new V[] {v_251, (V)"pending_terminal"}, new Results(v_254))));
V v_259 = values_258[0];
V v_260 = values_258[1];
if (R.Truth((V)(!R.Truth(v_259)))) {
return R.Join(new V[] {(V)false}, new Results(v_260));
}
var values_261 = R.Join(new V[] {}, R.Call(v_216, R.Join(new V[] {v_251, (V)"pending_terminal"}, new Results(v_254))));
V v_262 = values_261[0];
V v_263 = values_261[1];
if (R.Truth((V)(!R.Truth(v_262)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"terminal outbox backup failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_263))).First)));
}
return R.Join(new V[] {(V)true}, new Results(v_263));
return Results.Empty;
})));
R.Set(v_12, (V)"tombstone", new V((RuleFunction)(args_264 => {
V v_265 = args_264[0];
var values_266 = R.Join(new V[] {}, R.Call(v_216, R.Join(new V[] {v_265, (V)"terminal"}, new Results(V.Nil))));
V v_267 = values_266[0];
V v_268 = values_266[1];
if (R.Truth((V)(!R.Truth(v_267)))) {
return R.Join(new V[] {(V)false}, new Results(v_268));
}
var values_269 = R.Join(new V[] {}, R.Call(v_216, R.Join(new V[] {v_265, (V)"terminal"}, new Results(V.Nil))));
V v_270 = values_269[0];
V v_271 = values_269[1];
if (R.Truth((V)(!R.Truth(v_270)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"terminal backup failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_271))).First)));
}
return R.Join(new V[] {(V)true}, new Results(v_271));
return Results.Empty;
})));
R.Set(v_12, (V)"replace", new V((RuleFunction)(args_272 => {
V v_273 = args_272[0];
V v_274 = args_272[1];
var values_275 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"snapshot"), R.Join(new V[] {}, new Results(v_274))));
V v_276 = values_275[0];
var values_277 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"restore")}, new Results(v_276))));
V v_278 = values_277[0];
V v_279 = values_277[1];
if (R.Truth((V)(!R.Truth(v_278)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"unstable new run: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_279))).First)));
}
var values_280 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_12, (V)"inspect"), R.Join(new V[] {}, new Results(v_273))).First, (V)"record")));
V v_281 = values_280[0];
var values_282 = R.Join(new V[] {}, new Results(R.Or(R.And(v_281, () => (V)((V)3.0d - R.Get(v_281, (V)"slot"))), () => (V)1.0d)));
V v_283 = values_282[0];
var values_284 = R.Join(new V[] {}, new Results((V)((V)3.0d - v_283)));
V v_285 = values_284[0];
var values_286 = R.Join(new V[] {}, new Results(R.Or(R.And(v_281, () => (V)(R.Get(v_281, (V)"seq") + (V)1.0d)), () => (V)1.0d)));
V v_287 = values_286[0];
V v_288 = V.Nil;
v_288 = new V((RuleFunction)(args_289 => {
V v_290 = args_289[0];
V v_291 = args_289[1];
var values_292 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_12, (V)"slots"), v_290)));
V v_293 = values_292[0];
var values_294 = R.Join(new V[] {}, R.Call(v_142, R.Join(new V[] {v_291, (V)"active"}, new Results(v_276))));
V v_295 = values_294[0];
var values_296 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_273, (V)"storage"), (V)"write"), R.Get(v_273, (V)"storage"), v_293}, new Results(v_295))));
V v_297 = values_296[0];
V v_298 = values_296[1];
if (R.Truth(R.Or((V)(!R.Truth(v_297)), () => (V)(!R.Truth(v_298))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_299 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(R.Get(v_273, (V)"storage"), (V)"read"), R.Get(v_273, (V)"storage")}, new Results(v_293))));
V v_300 = values_299[0];
V v_301 = values_299[1];
if (R.Truth(R.Or((V)(!R.Truth(v_300)), () => (V)(v_301 != v_295)))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_302 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_149}, new Results(v_301))));
V v_303 = values_302[0];
V v_304 = values_302[1];
return R.Join(new V[] {}, new Results(R.And(v_303, () => (V)(R.Get(v_304, (V)"seq") == v_291))));
return Results.Empty;
}));
if (R.Truth((V)(!R.Truth(R.Call(v_288, R.Join(new V[] {v_283}, new Results(v_287))).First)))) {
return R.Join(new V[] {(V)false}, new Results((V)"new run save failed; prior generation retained"));
}
if (R.Truth((V)(!R.Truth(R.Call(v_288, R.Join(new V[] {v_285}, new Results((V)(v_287 + (V)1.0d)))).First)))) {
return R.Join(new V[] {(V)true}, new Results((V)"new run saved with one verified generation; backup write failed"));
}
var values_305 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"inspect"), R.Join(new V[] {}, new Results(v_273))));
V v_306 = values_305[0];
if (R.Truth(R.Or((V)(R.Get(v_306, (V)"status") != (V)"active"), () => (V)(R.Get(R.Get(v_306, (V)"record"), (V)"seq") != (V)(v_287 + (V)1.0d))))) {
return R.Join(new V[] {(V)false}, new Results((V)"new run verification failed"));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_12));
return Results.Empty;
} }
