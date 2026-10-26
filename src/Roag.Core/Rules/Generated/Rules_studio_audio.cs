// Static C# port of src/studio_audio.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: c6a6a045cde588dc11754a5a3eec40116411b5daa735a114e220b9c2cd80bee2
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_audio { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_project"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.audio_cues"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", (V)"roag-studio-audio/1"), new Entry((V)"max_sounds", (V)256.0d), new Entry((V)"max_bindings", (V)512.0d), new Entry((V)"max_source_bytes", (V)((V)((V)32.0d * (V)1024.0d) * (V)1024.0d)), new Entry((V)"max_project_bytes", (V)((V)((V)128.0d * (V)1024.0d) * (V)1024.0d)))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"player"), new Entry((V)2, (V)"enemy"), new Entry((V)3, (V)"boss"), new Entry((V)4, (V)"elite"), new Entry((V)5, (V)"class"), new Entry((V)6, (V)"ability"), new Entry((V)7, (V)"boon"), new Entry((V)8, (V)"terrain"), new Entry((V)9, (V)"world"), new Entry((V)10, (V)"room"), new Entry((V)11, (V)"biome"), new Entry((V)12, (V)"ui"), new Entry((V)13, (V)"music"))));
var obj_10 = v_8;
V key_11 = (V)"categories";
R.Set(obj_10, key_11, values_9[0]);
var values_12 = R.Join(new V[] {}, new Results(R.Table()));
V v_13 = values_12[0];

foreach (var row_14 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"categories"))))))) {
V v_15 = row_14[0];
V v_16 = row_14[1];
var values_17 = R.Join(new V[] {}, new Results((V)true));
var obj_18 = v_13;
V key_19 = v_16;
R.Set(obj_18, key_19, values_17[0]);
}
V v_20 = V.Nil;
v_20 = new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
V v_23 = args_21[1];
if (R.Truth((V)(!R.Truth(v_22)))) {
R.Call(rt.Global("error"), R.Join(new V[] {v_23}, new Results((V)2.0d)));
}
return Results.Empty;
}));
V v_24 = V.Nil;
v_24 = new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
V v_27 = args_25[1];
V v_28 = args_25[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_26))).First == (V)"number"), () => (V)((V)(v_26 % (V)1.0d) == (V)0.0d)), () => (V)(v_26 >= v_27)), () => (V)(v_26 <= v_28))));
return Results.Empty;
}));
V v_29 = V.Nil;
v_29 = new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
V v_32 = args_30[1];
V v_33 = args_30[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_31))).First == (V)"number"), () => (V)(v_31 == v_31)), () => (V)(v_31 >= v_32)), () => (V)(v_31 <= v_33))));
return Results.Empty;
}));
R.Set(v_8, (V)"checksum", new V((RuleFunction)(args_34 => {
V v_35 = args_34[0];
var values_36 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
V v_37 = values_36[0];
V v_38 = values_36[1];
{ double end_41 = R.Num((V)R.Length(v_35)), step_42 = R.Num((V)1.0d);
for (double i_40 = R.Num((V)1.0d); step_42 > 0 ? i_40 <= end_41 : i_40 >= end_41; i_40 += step_42) {
V v_39 = i_40;
var values_43 = R.Join(new V[] {}, new Results((V)((V)(v_37 + R.Invoke(v_35, "byte", R.Join(new V[] {}, new Results(v_39))).First) % (V)65521.0d)));
v_37 = values_43[0];

var values_44 = R.Join(new V[] {}, new Results((V)((V)(v_38 + v_37) % (V)65521.0d)));
v_38 = values_44[0];
}
}
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"format"), R.Join(new V[] {(V)"%08x"}, new Results((V)((V)(v_38 * (V)65536.0d) + v_37)))));
return Results.Empty;
})));
R.Set(v_8, (V)"format", new V((RuleFunction)(args_45 => {
V v_46 = args_45[0];
R.Call(v_20, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_46))).First == (V)"string"), () => (V)((V)R.Length(v_46) >= (V)32.0d)), () => (V)((V)R.Length(v_46) <= R.Get(v_8, (V)"max_source_bytes")))}, new Results((V)"audio source must be 32 bytes to 32 MiB")));
if (R.Truth(R.And((V)(R.Invoke(v_46, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)4.0d))).First == (V)"RIFF"), () => (V)(R.Invoke(v_46, "sub", R.Join(new V[] {(V)9.0d}, new Results((V)12.0d))).First == (V)"WAVE")))) {
V v_47 = V.Nil;
v_47 = new V((RuleFunction)(args_48 => {
V v_49 = args_48[0];
var values_50 = R.Join(new V[] {}, R.Invoke(v_46, "byte", R.Join(new V[] {v_49}, new Results((V)(v_49 + (V)1.0d)))));
V v_51 = values_50[0];
V v_52 = values_50[1];

return R.Join(new V[] {}, new Results((V)(v_51 + (V)(v_52 * (V)256.0d))));
return Results.Empty;
}));
V v_53 = V.Nil;
v_53 = new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
var values_56 = R.Join(new V[] {}, R.Invoke(v_46, "byte", R.Join(new V[] {v_55}, new Results((V)(v_55 + (V)3.0d)))));
V v_57 = values_56[0];
V v_58 = values_56[1];
V v_59 = values_56[2];
V v_60 = values_56[3];
return R.Join(new V[] {}, new Results((V)((V)((V)(v_57 + (V)(v_58 * (V)256.0d)) + (V)(v_59 * (V)65536.0d)) + (V)(v_60 * (V)16777216.0d))));
return Results.Empty;
}));
R.Call(v_20, R.Join(new V[] {(V)((V)(R.Call(v_53, R.Join(new V[] {}, new Results((V)5.0d))).First + (V)8.0d) == (V)R.Length(v_46))}, new Results((V)"WAV RIFF length mismatch")));
var values_61 = R.Join(new V[] {}, new Results((V)13.0d));
V v_62 = values_61[0];
V v_63 = values_61[1];
V v_64 = values_61[2];
V v_65 = values_61[3];
V v_66 = values_61[4];
while (R.Truth((V)((V)(v_62 + (V)7.0d) <= (V)R.Length(v_46)))) {
var values_67 = R.Join(new V[] {}, R.Invoke(v_46, "sub", R.Join(new V[] {v_62}, new Results((V)(v_62 + (V)3.0d)))));
V v_68 = values_67[0];

var values_69 = R.Join(new V[] {}, R.Call(v_53, R.Join(new V[] {}, new Results((V)(v_62 + (V)4.0d)))));
V v_70 = values_69[0];
R.Call(v_20, R.Join(new V[] {(V)(v_70 <= (V)((V)((V)R.Length(v_46) - v_62) - (V)7.0d))}, new Results((V)"truncated WAV chunk")));
if (R.Truth((V)(v_68 == (V)"fmt "))) {
R.Call(v_20, R.Join(new V[] {R.And((V)(v_70 >= (V)16.0d), () => (V)(R.Call(v_47, R.Join(new V[] {}, new Results((V)(v_62 + (V)8.0d)))).First == (V)1.0d))}, new Results((V)"only PCM WAV is supported")));
var values_71 = R.Join(new V[] {R.Call(v_47, R.Join(new V[] {}, new Results((V)(v_62 + (V)10.0d)))).First, R.Call(v_53, R.Join(new V[] {}, new Results((V)(v_62 + (V)12.0d)))).First}, R.Call(v_47, R.Join(new V[] {}, new Results((V)(v_62 + (V)22.0d)))));
v_63 = values_71[0];
v_64 = values_71[1];
v_65 = values_71[2];
} else if (R.Truth((V)(v_68 == (V)"data"))) {
var values_72 = R.Join(new V[] {}, new Results(v_70));
v_66 = values_72[0];
}
var values_73 = R.Join(new V[] {}, new Results((V)((V)((V)(v_62 + (V)8.0d) + v_70) + (V)(v_70 % (V)2.0d))));
v_62 = values_73[0];
}
R.Call(v_20, R.Join(new V[] {R.And(R.And(R.And(R.And(R.Or((V)(v_63 == (V)1.0d), () => (V)(v_63 == (V)2.0d)), () => R.Call(v_24, R.Join(new V[] {v_64, (V)8000.0d}, new Results((V)96000.0d))).First), () => R.Or((V)(v_65 == (V)8.0d), () => (V)(v_65 == (V)16.0d))), () => v_66), () => (V)(v_66 > (V)0.0d))}, new Results((V)"unsupported WAV channels, rate, depth or empty data")));
var values_74 = R.Join(new V[] {}, new Results((V)(v_66 / (V)((V)(v_64 * v_63) * (V)(v_65 / (V)8.0d)))));
V v_75 = values_74[0];
R.Call(v_20, R.Join(new V[] {R.And((V)(v_75 >= (V)0.01d), () => (V)(v_75 <= (V)1800.0d))}, new Results((V)"WAV duration outside 0.01\u00e2\u0080\u00931800 seconds")));
return R.Join(new V[] {(V)"wav"}, new Results(v_75));
}
if (R.Truth(R.And((V)(R.Invoke(v_46, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)4.0d))).First == (V)"OggS"), () => R.Invoke(v_46, "find", R.Join(new V[] {(V)"vorbis", (V)1.0d}, new Results((V)true))).First))) {
return R.Join(new V[] {(V)"ogg"}, new Results(V.Nil));
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"only PCM WAV or Ogg Vorbis is supported")));
return Results.Empty;
})));
R.Set(v_8, (V)"source_path", new V((RuleFunction)(args_76 => {
V v_77 = args_76[0];
V v_78 = args_76[1];
V v_79 = args_76[2];
V v_80 = args_76[3];
R.Call(v_20, R.Join(new V[] {R.And(R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(v_77))).First, () => R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(v_78))).First)}, new Results((V)"invalid project or audio ID")));
R.Call(v_20, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_79))).First == (V)"string"), () => R.Invoke(v_79, "match", R.Join(new V[] {}, new Results((V)"^%x%x%x%x%x%x%x%x$"))).First), () => R.Or((V)(v_80 == (V)"wav"), () => (V)(v_80 == (V)"ogg")))}, new Results((V)"invalid audio checksum or format")));
var values_81 = R.Join(new V[] {}, R.Invoke(v_78, "gsub", R.Join(new V[] {(V)"[^%w_%-]"}, new Results((V)"_"))));
V v_82 = values_81[0];
var values_83 = R.Join(new V[] {}, new Results(R.Concat((V)"studio/projects/", R.Concat(v_77, R.Concat((V)"/audio/", R.Concat(v_82, R.Concat((V)"-", R.Concat(v_79, R.Concat((V)".", v_80)))))))));
V v_84 = values_83[0];
R.Call(v_20, R.Join(new V[] {R.Call(R.Get(v_2, (V)"valid_path"), R.Join(new V[] {}, new Results(v_84))).First}, new Results((V)"unsafe audio source path")));
return R.Join(new V[] {}, new Results(v_84));
return Results.Empty;
})));
R.Set(v_8, (V)"published_path", new V((RuleFunction)(args_85 => {
V v_86 = args_85[0];
var values_87 = R.Join(new V[] {}, R.Invoke(R.Get(v_86, (V)"path"), "match", R.Join(new V[] {}, new Results((V)"([^/]+)$"))));
V v_88 = values_87[0];
R.Call(v_20, R.Join(new V[] {R.And(v_88, () => (V)(!R.Truth(R.Invoke(v_88, "find", R.Join(new V[] {}, new Results((V)"%.%."))).First)))}, new Results((V)"invalid published audio basename")));
return R.Join(new V[] {}, new Results(R.Concat((V)"assets/studio/audio/", v_88)));
return Results.Empty;
})));
R.Set(v_8, (V)"validate_publish_permissions", new V((RuleFunction)(args_89 => {
V v_90 = args_89[0];
foreach (var row_91 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_90, (V)"sounds"))))))) {
V v_92 = row_91[0];
V v_93 = row_91[1];
R.Call(v_20, R.Join(new V[] {R.And(R.And((V)(R.Get(v_93, (V)"author") != (V)""), () => (V)(R.Get(v_93, (V)"license") != (V)"")), () => (V)(R.Get(v_93, (V)"license") != (V)"User supplied; verify rights"))}, new Results(R.Concat((V)"complete author/license permission for ", R.Concat(R.Get(v_93, (V)"id"), (V)" before publishing")))));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_8, (V)"make_sound", new V((RuleFunction)(args_94 => {
V v_95 = args_94[0];
V v_96 = args_94[1];
V v_97 = args_94[2];
V v_98 = args_94[3];
V v_99 = args_94[4];
R.Call(v_20, R.Join(new V[] {R.And(R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(v_95))).First, () => R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(v_96))).First)}, new Results((V)"invalid audio identity")));
R.Call(v_20, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_97))).First == (V)"string"), () => (V)((V)R.Length(v_97) > (V)0.0d)), () => (V)((V)R.Length(v_97) <= (V)80.0d)), () => (V)(!R.Truth(R.Invoke(v_97, "find", R.Join(new V[] {}, new Results((V)"[%c]"))).First)))}, new Results((V)"invalid audio name")));
var values_100 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"format"), R.Join(new V[] {}, new Results(v_98))));
V v_101 = values_100[0];
V v_102 = values_100[1];
var values_103 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"checksum"), R.Join(new V[] {}, new Results(v_98))));
V v_104 = values_103[0];
var values_105 = R.Join(new V[] {}, new Results(R.Or(v_99, () => R.Table())));
v_99 = values_105[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_96), new Entry((V)"name", v_97), new Entry((V)"tags", R.Or(R.Get(v_99, (V)"tags"), () => (V)"")), new Entry((V)"author", R.Or(R.Get(v_99, (V)"author"), () => (V)"")), new Entry((V)"source_url", R.Or(R.Get(v_99, (V)"source_url"), () => (V)"")), new Entry((V)"license", R.Or(R.Get(v_99, (V)"license"), () => (V)"")), new Entry((V)"format", v_101), new Entry((V)"bytes", (V)R.Length(v_98)), new Entry((V)"checksum", v_104), new Entry((V)"duration", v_102), new Entry((V)"path", R.Call(R.Get(v_8, (V)"source_path"), R.Join(new V[] {v_95, v_96, v_104}, new Results(v_101))).First))));
return Results.Empty;
})));
R.Set(v_8, (V)"new", new V((RuleFunction)(args_106 => {
V v_107 = args_106[0];
R.Call(v_20, R.Join(new V[] {R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(v_107))).First}, new Results((V)"invalid audio project ID")));
var values_108 = R.Join(new V[] {}, new Results(R.Table()));
V v_109 = values_108[0];
foreach (var row_110 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"categories"))))))) {
V v_111 = row_110[0];
V v_112 = row_110[1];
var values_113 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"volume", (V)1.0d), new Entry((V)"mute", (V)false))));
var obj_114 = v_109;
V key_115 = v_112;
R.Set(obj_114, key_115, values_113[0]);
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", R.Get(v_8, (V)"schema")), new Entry((V)"project_id", v_107), new Entry((V)"sounds", R.Table()), new Entry((V)"bindings", R.Table()), new Entry((V)"mix", v_109))));
return Results.Empty;
})));
R.Set(v_8, (V)"binding", new V((RuleFunction)(args_116 => {
V v_117 = args_116[0];
V v_118 = args_116[1];
R.Call(v_20, R.Join(new V[] {R.Call(R.Get(v_6, (V)"get"), R.Join(new V[] {}, new Results(v_117))).First}, new Results((V)"unknown audio cue")));
var values_119 = R.Join(new V[] {}, new Results(R.Or((V)(R.Invoke(v_117, "find", R.Join(new V[] {(V)"/music/", (V)1.0d}, new Results((V)true))).First != V.Nil), () => (V)(R.Invoke(v_117, "find", R.Join(new V[] {(V)"/ambient/", (V)1.0d}, new Results((V)true))).First != V.Nil))));
V v_120 = values_119[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"cue_id", v_117), new Entry((V)"variants", R.Table(new Entry((V)1, R.Table(new Entry((V)"asset_id", v_118), new Entry((V)"weight", (V)1.0d))))), new Entry((V)"volume", (V)1.0d), new Entry((V)"pitch", (V)1.0d), new Entry((V)"loop", v_120), new Entry((V)"priority", R.Or(R.And(v_120, () => (V)10.0d), () => (V)5.0d)), new Entry((V)"max_voices", R.Or(R.And(v_120, () => (V)1.0d), () => (V)4.0d)), new Entry((V)"cooldown_ms", R.Or(R.And(v_120, () => (V)0.0d), () => (V)60.0d)), new Entry((V)"distance", (V)0.0d), new Entry((V)"transition", R.Table(new Entry((V)"mode", R.Or(R.And(v_120, () => (V)"crossfade"), () => (V)"instant")), new Entry((V)"duration_ms", R.Or(R.And(v_120, () => (V)800.0d), () => (V)0.0d)))))));
return Results.Empty;
})));
R.Set(v_8, (V)"validate", new V((RuleFunction)(args_121 => {
V v_122 = args_121[0];
V v_123 = args_121[1];
V v_124 = args_121[2];
R.Call(v_20, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_122))).First == (V)"table"), () => (V)(R.Get(v_122, (V)"schema") == R.Get(v_8, (V)"schema")))}, new Results((V)"incompatible audio schema")));
R.Call(v_20, R.Join(new V[] {R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(R.Get(v_122, (V)"project_id")))).First}, new Results((V)"invalid audio project ID")));
R.Call(v_20, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_122, (V)"sounds")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_122, (V)"sounds")) <= R.Get(v_8, (V)"max_sounds")))}, new Results((V)"invalid audio library")));
var values_125 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_126 = values_125[0];
V v_127 = values_125[1];
foreach (var row_128 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_122, (V)"sounds"))))))) {
V v_129 = row_128[0];
V v_130 = row_128[1];
R.Call(v_20, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_130))).First == (V)"table"), () => R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(R.Get(v_130, (V)"id")))).First), () => (V)(!R.Truth(R.Get(v_126, R.Get(v_130, (V)"id")))))}, new Results((V)"duplicate or invalid audio ID")));
var values_131 = R.Join(new V[] {}, new Results(v_130));
var obj_132 = v_126;
V key_133 = R.Get(v_130, (V)"id");
R.Set(obj_132, key_133, values_131[0]);
foreach (var row_134 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"name"), new Entry((V)2, (V)"tags"), new Entry((V)3, (V)"author"), new Entry((V)4, (V)"source_url"), new Entry((V)5, (V)"license")))))))) {
V v_135 = row_134[0];
V v_136 = row_134[1];
var values_137 = R.Join(new V[] {}, new Results(R.Get(v_130, v_136)));
V v_138 = values_137[0];
R.Call(v_20, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_138))).First == (V)"string"), () => (V)((V)R.Length(v_138) <= (V)512.0d)), () => (V)(!R.Truth(R.Invoke(v_138, "find", R.Join(new V[] {}, new Results((V)"[%c]"))).First)))}, new Results(R.Concat((V)"invalid audio ", R.Concat(v_136, R.Concat((V)" for ", R.Get(v_130, (V)"id")))))));
}
R.Call(v_20, R.Join(new V[] {(V)((V)R.Length(R.Get(v_130, (V)"name")) > (V)0.0d)}, new Results((V)"audio name required")));
R.Call(v_20, R.Join(new V[] {R.Or((V)(R.Get(v_130, (V)"format") == (V)"wav"), () => (V)(R.Get(v_130, (V)"format") == (V)"ogg"))}, new Results((V)"unsupported audio format")));
R.Call(v_20, R.Join(new V[] {R.And(R.And(R.Call(v_24, R.Join(new V[] {R.Get(v_130, (V)"bytes"), (V)32.0d}, new Results(R.Get(v_8, (V)"max_source_bytes")))).First, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_130, (V)"checksum")))).First == (V)"string")), () => R.Invoke(R.Get(v_130, (V)"checksum"), "match", R.Join(new V[] {}, new Results((V)"^%x%x%x%x%x%x%x%x$"))).First)}, new Results((V)"invalid audio length/checksum")));
var values_139 = R.Join(new V[] {}, new Results((V)(v_127 + R.Get(v_130, (V)"bytes"))));
v_127 = values_139[0];
R.Call(v_20, R.Join(new V[] {(V)(v_127 <= R.Get(v_8, (V)"max_project_bytes"))}, new Results((V)"audio project exceeds 128 MiB")));
R.Call(v_20, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_130, (V)"path")))).First == (V)"string"), () => R.Call(R.Get(v_2, (V)"valid_path"), R.Join(new V[] {}, new Results(R.Get(v_130, (V)"path")))).First), () => (V)(R.Get(v_130, (V)"path") == R.Call(R.Get(v_8, (V)"source_path"), R.Join(new V[] {R.Get(v_122, (V)"project_id"), R.Get(v_130, (V)"id"), R.Get(v_130, (V)"checksum")}, new Results(R.Get(v_130, (V)"format")))).First))}, new Results((V)"unsafe or mismatched audio source path")));
R.Call(v_20, R.Join(new V[] {R.Or((V)(R.Get(v_130, (V)"duration") == V.Nil), () => R.Call(v_29, R.Join(new V[] {R.Get(v_130, (V)"duration"), (V)0.01d}, new Results((V)1800.0d))).First)}, new Results((V)"invalid audio duration")));
if (R.Truth(v_123)) {
var values_140 = R.Join(new V[] {}, R.Call(v_123, R.Join(new V[] {}, new Results(R.Get(v_130, (V)"path")))));
V v_141 = values_140[0];
R.Call(v_20, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_141))).First == (V)"string"), () => (V)((V)R.Length(v_141) == R.Get(v_130, (V)"bytes"))), () => (V)(R.Call(R.Get(v_8, (V)"checksum"), R.Join(new V[] {}, new Results(v_141))).First == R.Get(v_130, (V)"checksum")))}, new Results(R.Concat((V)"missing/corrupt audio source: ", R.Get(v_130, (V)"id")))));
var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"format"), R.Join(new V[] {}, new Results(v_141))));
V v_143 = values_142[0];
R.Call(v_20, R.Join(new V[] {(V)(v_143 == R.Get(v_130, (V)"format"))}, new Results(R.Concat((V)"audio source format mismatch: ", R.Get(v_130, (V)"id")))));
}
}
R.Call(v_20, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_122, (V)"bindings")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_122, (V)"bindings")) <= R.Get(v_8, (V)"max_bindings")))}, new Results((V)"invalid audio bindings")));
var values_144 = R.Join(new V[] {}, new Results(R.Table()));
V v_145 = values_144[0];
foreach (var row_146 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_122, (V)"bindings"))))))) {
V v_147 = row_146[0];
V v_148 = row_146[1];
R.Call(v_20, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_148))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"cue_id")))).First == (V)"string")), () => R.Call(R.Get(v_6, (V)"get"), R.Join(new V[] {R.Get(v_148, (V)"cue_id")}, new Results(v_124))).First), () => (V)(!R.Truth(R.Get(v_145, R.Get(v_148, (V)"cue_id")))))}, new Results((V)"duplicate or unknown audio cue")));
var values_149 = R.Join(new V[] {}, new Results((V)true));
var obj_150 = v_145;
V key_151 = R.Get(v_148, (V)"cue_id");
R.Set(obj_150, key_151, values_149[0]);
R.Call(v_20, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"variants")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_148, (V)"variants")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_148, (V)"variants")) <= (V)8.0d))}, new Results((V)"audio binding requires 1\u00e2\u0080\u00938 variants")));
var values_152 = R.Join(new V[] {}, new Results(R.Table()));
V v_153 = values_152[0];
foreach (var row_154 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"variants"))))))) {
V v_155 = row_154[0];
V v_156 = row_154[1];
R.Call(v_20, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_156))).First == (V)"table"), () => R.Get(v_126, R.Get(v_156, (V)"asset_id"))), () => (V)(!R.Truth(R.Get(v_153, R.Get(v_156, (V)"asset_id"))))), () => R.Call(v_24, R.Join(new V[] {R.Get(v_156, (V)"weight"), (V)1.0d}, new Results((V)100.0d))).First)}, new Results((V)"missing/duplicate audio variant or invalid weight")));
var values_157 = R.Join(new V[] {}, new Results((V)true));
var obj_158 = v_153;
V key_159 = R.Get(v_156, (V)"asset_id");
R.Set(obj_158, key_159, values_157[0]);
}
R.Call(v_20, R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.Call(v_29, R.Join(new V[] {R.Get(v_148, (V)"volume"), (V)0.0d}, new Results((V)2.0d))).First, () => R.Call(v_29, R.Join(new V[] {R.Get(v_148, (V)"pitch"), (V)0.5d}, new Results((V)2.0d))).First), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_148, (V)"loop")))).First == (V)"boolean")), () => R.Call(v_24, R.Join(new V[] {R.Get(v_148, (V)"priority"), (V)0.0d}, new Results((V)10.0d))).First), () => R.Call(v_24, R.Join(new V[] {R.Get(v_148, (V)"max_voices"), (V)1.0d}, new Results((V)16.0d))).First), () => R.Call(v_24, R.Join(new V[] {R.Get(v_148, (V)"cooldown_ms"), (V)0.0d}, new Results((V)5000.0d))).First), () => R.Call(v_24, R.Join(new V[] {R.Get(v_148, (V)"distance"), (V)0.0d}, new Results((V)40.0d))).First)}, new Results((V)"invalid audio playback settings")));
var values_160 = R.Join(new V[] {}, new Results(R.Get(v_148, (V)"transition")));
V v_161 = values_160[0];
R.Call(v_20, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_161))).First == (V)"table"), () => R.Get(R.Table(new Entry((V)"instant", (V)true), new Entry((V)"fade", (V)true), new Entry((V)"crossfade", (V)true)), R.Get(v_161, (V)"mode"))), () => R.Call(v_24, R.Join(new V[] {R.Get(v_161, (V)"duration_ms"), (V)0.0d}, new Results((V)5000.0d))).First)}, new Results((V)"invalid audio transition")));
}
R.Call(v_20, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_122, (V)"mix")))).First == (V)"table")}, new Results((V)"missing audio category mixer")));
foreach (var row_162 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_122, (V)"mix"))))))) {
V v_163 = row_162[0];
V v_164 = row_162[1];
R.Call(v_20, R.Join(new V[] {R.And(R.And(R.And(R.Get(v_13, v_163), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_164))).First == (V)"table")), () => R.Call(v_29, R.Join(new V[] {R.Get(v_164, (V)"volume"), (V)0.0d}, new Results((V)2.0d))).First), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_164, (V)"mute")))).First == (V)"boolean"))}, new Results((V)"invalid audio category mixer")));
}
foreach (var row_165 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"categories"))))))) {
V v_166 = row_165[0];
V v_167 = row_165[1];
R.Call(v_20, R.Join(new V[] {R.Get(R.Get(v_122, (V)"mix"), v_167)}, new Results(R.Concat((V)"missing ", R.Concat(v_167, (V)" mixer")))));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_8, (V)"encode", new V((RuleFunction)(args_168 => {
V v_169 = args_168[0];
R.Call(R.Get(v_8, (V)"validate"), R.Join(new V[] {}, new Results(v_169)));
var values_170 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(v_169))));
V v_171 = values_170[0];
R.Call(v_20, R.Join(new V[] {(V)((V)R.Length(v_171) <= (V)262144.0d)}, new Results((V)"audio metadata exceeds 256 KiB")));
return R.Join(new V[] {}, new Results(v_171));
return Results.Empty;
})));
R.Set(v_8, (V)"decode", new V((RuleFunction)(args_172 => {
V v_173 = args_172[0];
R.Call(v_20, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_173))).First == (V)"string"), () => (V)((V)R.Length(v_173) <= (V)262144.0d))}, new Results((V)"invalid audio metadata length")));
var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"decode"), R.Join(new V[] {}, new Results(v_173))));
V v_175 = values_174[0];
R.Call(R.Get(v_8, (V)"validate"), R.Join(new V[] {}, new Results(v_175)));
return R.Join(new V[] {}, new Results(v_175));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_8));
return Results.Empty;
} }
