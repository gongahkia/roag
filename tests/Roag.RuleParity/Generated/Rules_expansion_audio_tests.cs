// Static C# port of tests/expansion_audio_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 20768dd3eeb00c1c6fdc1e202b81a6e543fb997e5b6d09e277f7d325a9b1dc8d
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_expansion_audio_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_audio"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_audio_store"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_audio_samples"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.audio_cues"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_audio_runtime"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_project"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_17 = values_16[0];
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {

var values_20 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"files", R.Table()))));
V v_21 = values_20[0];
R.Set(v_21, "read", new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
V v_24 = args_22[1];
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_23, (V)"files"), v_24)));
return Results.Empty;
})));
R.Set(v_21, "write", new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
V v_27 = args_25[1];
V v_28 = args_25[2];
if (R.Truth(R.Get(v_26, (V)"fail"))) {
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"injected write failure")));
}
var values_29 = R.Join(new V[] {}, new Results(v_28));
var obj_30 = R.Get(v_26, (V)"files");
V key_31 = v_27;
R.Set(obj_30, key_31, values_29[0]);

return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_21, "mkdir", new V((RuleFunction)(args_32 => {
V v_33 = args_32[0];
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_21));
return Results.Empty;
}));
V v_34 = V.Nil;
v_34 = new V((RuleFunction)(args_35 => {

var values_36 = R.Join(new V[] {}, R.Call(v_18, Results.Empty));
V v_37 = values_36[0];

var values_38 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results(v_37))));
V v_39 = values_38[0];
var values_40 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"showcase"), R.Join(new V[] {}, new Results((V)"audio_test"))));
V v_41 = values_40[0];
V v_42 = values_40[1];
foreach (var row_43 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_41, (V)"sounds"))))))) {
V v_44 = row_43[0];
V v_45 = row_43[1];
var values_46 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_39, "import", R.Join(new V[] {v_41, R.Get(v_45, (V)"id"), R.Get(v_45, (V)"name"), R.Get(v_42, R.Get(v_45, (V)"path"))}, new Results(R.Table(new Entry((V)"author", R.Get(v_45, (V)"author")), new Entry((V)"license", R.Get(v_45, (V)"license")), new Entry((V)"tags", R.Get(v_45, (V)"tags")))))))));
V v_47 = values_46[0];
R.Call(v_3, R.Join(new V[] {v_47}, new Results(v_45)));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_39, "save", R.Join(new V[] {}, new Results(v_41)))));
return R.Join(new V[] {v_37, v_39, v_41}, new Results(v_42));
return Results.Empty;
}));
V v_48 = V.Nil;
v_48 = new V((RuleFunction)(args_49 => {

var values_50 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"playing", (V)false), new Entry((V)"volume", (V)0.0d), new Entry((V)"play_count", (V)0.0d))));
V v_51 = values_50[0];
R.Set(v_51, "clone", new V((RuleFunction)(args_52 => {
V v_53 = args_52[0];
return R.Join(new V[] {}, R.Call(v_48, Results.Empty));
return Results.Empty;
})));
R.Set(v_51, "setVolume", new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
var values_57 = R.Join(new V[] {}, new Results(v_56));
var obj_58 = v_55;
V key_59 = (V)"volume";
R.Set(obj_58, key_59, values_57[0]);
return Results.Empty;
})));
R.Set(v_51, "setPitch", new V((RuleFunction)(args_60 => {
V v_61 = args_60[0];
V v_62 = args_60[1];
var values_63 = R.Join(new V[] {}, new Results(v_62));
var obj_64 = v_61;
V key_65 = (V)"pitch";
R.Set(obj_64, key_65, values_63[0]);
return Results.Empty;
})));
R.Set(v_51, "setLooping", new V((RuleFunction)(args_66 => {
V v_67 = args_66[0];
V v_68 = args_66[1];
var values_69 = R.Join(new V[] {}, new Results(v_68));
var obj_70 = v_67;
V key_71 = (V)"loop";
R.Set(obj_70, key_71, values_69[0]);
return Results.Empty;
})));
R.Set(v_51, "play", new V((RuleFunction)(args_72 => {
V v_73 = args_72[0];
var values_74 = R.Join(new V[] {}, new Results((V)true));
var obj_75 = v_73;
V key_76 = (V)"playing";
R.Set(obj_75, key_76, values_74[0]);

var values_77 = R.Join(new V[] {}, new Results((V)(R.Get(v_73, (V)"play_count") + (V)1.0d)));
var obj_78 = v_73;
V key_79 = (V)"play_count";
R.Set(obj_78, key_79, values_77[0]);
return Results.Empty;
})));
R.Set(v_51, "stop", new V((RuleFunction)(args_80 => {
V v_81 = args_80[0];
var values_82 = R.Join(new V[] {}, new Results((V)false));
var obj_83 = v_81;
V key_84 = (V)"playing";
R.Set(obj_83, key_84, values_82[0]);
return Results.Empty;
})));
R.Set(v_51, "pause", new V((RuleFunction)(args_85 => {
V v_86 = args_85[0];
var values_87 = R.Join(new V[] {}, new Results((V)false));
var obj_88 = v_86;
V key_89 = (V)"playing";
R.Set(obj_88, key_89, values_87[0]);
return Results.Empty;
})));
R.Set(v_51, "isPlaying", new V((RuleFunction)(args_90 => {
V v_91 = args_90[0];
return R.Join(new V[] {}, new Results(R.Get(v_91, (V)"playing")));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_51));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"audio cue registry covers class, enemy, elite, terrain, boon, room and music contexts"}, new Results(new V((RuleFunction)(args_92 => {

var values_93 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"list"), Results.Empty));
V v_94 = values_93[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_94) >= (V)260.0d))));
var values_95 = R.Join(new V[] {}, new Results(R.Table()));
V v_96 = values_95[0];
foreach (var row_97 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_94)))))) {
V v_98 = row_97[0];
V v_99 = row_97[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_15, (V)"valid_id"), R.Join(new V[] {}, new Results(R.Get(v_99, (V)"id")))).First, () => (V)(!R.Truth(R.Get(v_96, R.Get(v_99, (V)"id"))))))));

var values_100 = R.Join(new V[] {}, new Results((V)true));
var obj_101 = v_96;
V key_102 = R.Get(v_99, (V)"id");
R.Set(obj_101, key_102, values_100[0]);
}
foreach (var row_103 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:cue/player/move"), new Entry((V)2, (V)"core:cue/ability/ranger_shot"), new Entry((V)3, (V)"core:cue/enemy/mycelial_matriarch/telegraph"), new Entry((V)4, (V)"core:cue/elite/mycelial_matriarch/phase"), new Entry((V)5, (V)"core:cue/terrain/sticky_bog"), new Entry((V)6, (V)"core:cue/boon/storm_conductor"), new Entry((V)7, (V)"core:cue/ui/achievement"), new Entry((V)8, (V)"core:cue/music/explore/sundered_forge")))))))) {
V v_104 = row_103[0];
V v_105 = row_103[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_11, (V)"get"), R.Join(new V[] {}, new Results(v_105))).First}, new Results(R.Concat((V)"missing ", v_105))));
}
var values_106 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rooms", R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)"studio:room/test"), new Entry((V)"name", (V)"Test room"))))))));
V v_107 = values_106[0];
var values_108 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"room_cue"), R.Join(new V[] {R.Get(R.Get(R.Get(v_107, (V)"rooms"), (V)1.0d), (V)"id")}, new Results((V)"music"))));
V v_109 = values_108[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_11, (V)"get"), R.Join(new V[] {v_109}, new Results(v_107))).First, () => R.Call(R.Get(v_11, (V)"get"), R.Join(new V[] {}, new Results(v_109))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"original synthesized WAV samples parse and source IDs remain stable"}, new Results(new V((RuleFunction)(args_110 => {

var values_111 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"showcase"), R.Join(new V[] {}, new Results((V)"audio_test"))));
V v_112 = values_111[0];
V v_113 = values_111[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_112, (V)"sounds")) == (V)8.0d), () => (V)((V)R.Length(R.Get(v_112, (V)"bindings")) >= (V)27.0d)))));
R.Call(R.Get(v_5, (V)"validate"), R.Join(new V[] {v_112}, new Results(new V((RuleFunction)(args_114 => {
V v_115 = args_114[0];
return R.Join(new V[] {}, new Results(R.Get(v_113, v_115)));
return Results.Empty;
})))));
foreach (var row_116 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_112, (V)"sounds"))))))) {
V v_117 = row_116[0];
V v_118 = row_116[1];
var values_119 = R.Join(new V[] {}, new Results(R.Get(v_113, R.Get(v_118, (V)"path"))));
V v_120 = values_119[0];
var values_121 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"format"), R.Join(new V[] {}, new Results(v_120))));
V v_122 = values_121[0];
V v_123 = values_121[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_122 == (V)"wav"), () => (V)(v_123 >= (V)0.08d)), () => (V)(v_123 <= (V)2.01d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_118, (V)"checksum") == R.Call(R.Get(v_5, (V)"checksum"), R.Join(new V[] {}, new Results(v_120))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_5, (V)"published_path"), R.Join(new V[] {}, new Results(v_118))).First, "match", R.Join(new V[] {}, new Results((V)"^assets/studio/audio/")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(R.Get(v_118, (V)"path"), "find", R.Join(new V[] {(V)"%.%./", (V)1.0d}, new Results((V)true))).First)))));
}
var values_124 = R.Join(new V[] {}, R.Invoke(R.Call(R.Get(v_9, (V)"wav"), R.Join(new V[] {}, new Results((V)"step"))).First, "sub", R.Join(new V[] {(V)1.0d}, new Results((-(V)3.0d)))));
V v_125 = values_124[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"format")}, new Results(v_125))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"format")}, new Results((V)"RIFFnot an audio file"))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"audio source save/reopen, immutable replace and torn newest recovery"}, new Results(new V((RuleFunction)(args_126 => {

var values_127 = R.Join(new V[] {}, R.Call(v_34, Results.Empty));
V v_128 = values_127[0];
V v_129 = values_127[1];
V v_130 = values_127[2];
var values_131 = R.Join(new V[] {}, R.Invoke(v_129, "open", R.Join(new V[] {}, new Results((V)"audio_test"))));
V v_132 = values_131[0];
V v_133 = values_131[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_133 == (V)"loaded"), () => (V)(R.Call(R.Get(v_5, (V)"encode"), R.Join(new V[] {}, new Results(v_132))).First == R.Call(R.Get(v_5, (V)"encode"), R.Join(new V[] {}, new Results(v_130))).First)))));
var values_134 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_130, (V)"sounds"), (V)1.0d)));
V v_135 = values_134[0];
var values_136 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_129, "import", R.Join(new V[] {v_130, R.Get(v_135, (V)"id"), R.Get(v_135, (V)"name"), R.Call(R.Get(v_9, (V)"wav"), R.Join(new V[] {}, new Results((V)"hit"))).First}, new Results(R.Table(new Entry((V)"author", R.Get(v_135, (V)"author")), new Entry((V)"license", R.Get(v_135, (V)"license")), new Entry((V)"tags", R.Get(v_135, (V)"tags")))))))));
V v_137 = values_136[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_137, (V)"path") != R.Get(v_135, (V)"path")), () => R.Get(R.Get(v_128, (V)"files"), R.Get(v_135, (V)"path"))))));
var values_138 = R.Join(new V[] {}, new Results(v_137));
var obj_139 = R.Get(v_130, (V)"sounds");
V key_140 = (V)1.0d;
R.Set(obj_139, key_140, values_138[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_129, "save", R.Join(new V[] {}, new Results(v_130)))));
var values_141 = R.Join(new V[] {}, R.Invoke(v_129, "open", R.Join(new V[] {}, new Results((V)"audio_test"))));
V v_142 = values_141[0];
V v_143 = values_141[1];
V v_144 = values_141[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_143 == (V)"loaded"), () => (V)(R.Get(R.Get(R.Get(v_142, (V)"sounds"), (V)1.0d), (V)"path") == R.Get(v_137, (V)"path"))))));
var values_145 = R.Join(new V[] {}, new Results((V)"truncated"));
var obj_146 = R.Get(v_128, (V)"files");
V key_147 = (V)"studio/projects/audio_test/audio-b.pack";
R.Set(obj_146, key_147, values_145[0]);
var values_148 = R.Join(new V[] {}, R.Invoke(v_129, "open", R.Join(new V[] {}, new Results((V)"audio_test"))));
V v_149 = values_148[0];
V v_150 = values_148[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_150 == (V)"recovered previous audio generation"), () => (V)(R.Get(R.Get(R.Get(v_149, (V)"sounds"), (V)1.0d), (V)"path") == R.Get(v_135, (V)"path"))))));
var values_151 = R.Join(new V[] {}, new Results(V.Nil));
var obj_152 = R.Get(v_128, (V)"files");
V key_153 = (V)"studio/projects/audio_test/audio-b.pack";
R.Set(obj_152, key_153, values_151[0]);
var values_154 = R.Join(new V[] {}, new Results((V)true));
var obj_155 = v_128;
V key_156 = (V)"fail";
R.Set(obj_155, key_156, values_154[0]);
var values_157 = R.Join(new V[] {}, R.Invoke(v_129, "save", R.Join(new V[] {}, new Results(v_130))));
V v_158 = values_157[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_158)))));
var values_159 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_129, "open", R.Join(new V[] {}, new Results((V)"audio_test"))))));
V v_160 = values_159[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_160, (V)"sounds"), (V)1.0d), (V)"path") == R.Get(v_135, (V)"path")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"audio metadata validates references, settings, provenance and paths"}, new Results(new V((RuleFunction)(args_161 => {

var values_162 = R.Join(new V[] {}, R.Call(v_34, Results.Empty));
V v_163 = values_162[0];
V v_164 = values_162[1];
V v_165 = values_162[2];
V v_166 = values_162[3];
var values_167 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"encode"), R.Join(new V[] {}, new Results(v_165))));
V v_168 = values_167[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"decode"), R.Join(new V[] {}, new Results(v_168))).First}, new Results(v_165)));
V v_169 = V.Nil;
v_169 = new V((RuleFunction)(args_170 => {
V v_171 = args_170[0];
var values_172 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"decode"), R.Join(new V[] {}, new Results(v_168))));
V v_173 = values_172[0];

R.Call(v_171, R.Join(new V[] {}, new Results(v_173)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"validate"), v_173}, new Results(new V((RuleFunction)(args_174 => {
V v_175 = args_174[0];
return R.Join(new V[] {}, new Results(R.Get(v_166, v_175)));
return Results.Empty;
}))))).First)))));
return Results.Empty;
}));
R.Call(v_169, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_176 => {
V v_177 = args_176[0];
var values_178 = R.Join(new V[] {}, new Results((V)"../escape.wav"));
var obj_179 = R.Get(R.Get(v_177, (V)"sounds"), (V)1.0d);
V key_180 = (V)"path";
R.Set(obj_179, key_180, values_178[0]);
return Results.Empty;
})))));
R.Call(v_169, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_181 => {
V v_182 = args_181[0];
var values_183 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"x"}, new Results((V)513.0d))));
var obj_184 = R.Get(R.Get(v_182, (V)"sounds"), (V)1.0d);
V key_185 = (V)"license";
R.Set(obj_184, key_185, values_183[0]);
return Results.Empty;
})))));
R.Call(v_169, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_186 => {
V v_187 = args_186[0];
var values_188 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_187, (V)"sounds"), (V)1.0d), (V)"id")));
var obj_189 = R.Get(R.Get(v_187, (V)"sounds"), (V)2.0d);
V key_190 = (V)"id";
R.Set(obj_189, key_190, values_188[0]);
return Results.Empty;
})))));
R.Call(v_169, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_191 => {
V v_192 = args_191[0];
var values_193 = R.Join(new V[] {}, new Results((V)"studio:audio/missing"));
var obj_194 = R.Get(R.Get(R.Get(R.Get(v_192, (V)"bindings"), (V)1.0d), (V)"variants"), (V)1.0d);
V key_195 = (V)"asset_id";
R.Set(obj_194, key_195, values_193[0]);
return Results.Empty;
})))));
R.Call(v_169, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_196 => {
V v_197 = args_196[0];
var values_198 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_199 = R.Get(R.Get(v_197, (V)"bindings"), (V)1.0d);
V key_200 = (V)"max_voices";
R.Set(obj_199, key_200, values_198[0]);
return Results.Empty;
})))));
R.Call(v_169, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_201 => {
V v_202 = args_201[0];
var values_203 = R.Join(new V[] {}, new Results((V)"unknown"));
var obj_204 = R.Get(R.Get(R.Get(v_202, (V)"bindings"), (V)1.0d), (V)"transition");
V key_205 = (V)"mode";
R.Set(obj_204, key_205, values_203[0]);
return Results.Empty;
})))));
R.Call(v_169, R.Join(new V[] {}, new Results(new V((RuleFunction)(args_206 => {
V v_207 = args_206[0];
var values_208 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_207, (V)"bindings"), (V)1.0d), (V)"cue_id")));
var obj_209 = R.Get(R.Get(v_207, (V)"bindings"), (V)2.0d);
V key_210 = (V)"cue_id";
R.Set(obj_209, key_210, values_208[0]);
return Results.Empty;
})))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"decode")}, R.Invoke(v_168, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)((V)R.Length(v_168) - (V)6.0d)))))).First)))));
var values_211 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"decode"), R.Join(new V[] {}, new Results(v_168))));
V v_212 = values_211[0];
var values_213 = R.Join(new V[] {}, new Results((V)"corrupt"));
var obj_214 = v_166;
V key_215 = R.Get(R.Get(R.Get(v_212, (V)"sounds"), (V)1.0d), (V)"path");
R.Set(obj_214, key_215, values_213[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"validate"), v_212}, new Results(new V((RuleFunction)(args_216 => {
V v_217 = args_216[0];
return R.Join(new V[] {}, new Results(R.Get(v_166, v_217)));
return Results.Empty;
}))))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"audio publishing refuses unverified source permissions without changing drafts"}, new Results(new V((RuleFunction)(args_218 => {

var values_219 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"showcase"), R.Join(new V[] {}, new Results((V)"audio_test"))));
V v_220 = values_219[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"validate_publish_permissions"), R.Join(new V[] {}, new Results(v_220)))));
var values_221 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"encode"), R.Join(new V[] {}, new Results(v_220))));
V v_222 = values_221[0];
var values_223 = R.Join(new V[] {}, new Results((V)"User supplied; verify rights"));
var obj_224 = R.Get(R.Get(v_220, (V)"sounds"), (V)1.0d);
V key_225 = (V)"license";
R.Set(obj_224, key_225, values_223[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"validate_publish_permissions")}, new Results(v_220))).First)))));
var values_226 = R.Join(new V[] {}, new Results((V)"Original project asset"));
var obj_227 = R.Get(R.Get(v_220, (V)"sounds"), (V)1.0d);
V key_228 = (V)"license";
R.Set(obj_227, key_228, values_226[0]);
var values_229 = R.Join(new V[] {}, new Results((V)""));
var obj_230 = R.Get(R.Get(v_220, (V)"sounds"), (V)1.0d);
V key_231 = (V)"author";
R.Set(obj_230, key_231, values_229[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_5, (V)"validate_publish_permissions")}, new Results(v_220))).First)))));
var values_232 = R.Join(new V[] {}, new Results((V)"ROAG Studio synthesized"));
var obj_233 = R.Get(R.Get(v_220, (V)"sounds"), (V)1.0d);
V key_234 = (V)"author";
R.Set(obj_233, key_234, values_232[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_5, (V)"encode"), R.Join(new V[] {}, new Results(v_220))).First == v_222)}, new Results((V)"permission rejection altered editable source metadata")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"audio publish plan is read-only and complete generations recover safely"}, new Results(new V((RuleFunction)(args_235 => {

var values_236 = R.Join(new V[] {}, R.Call(v_34, Results.Empty));
V v_237 = values_236[0];
V v_238 = values_236[1];
V v_239 = values_236[2];
V v_240 = values_236[3];
var values_241 = R.Join(new V[] {}, new Results(R.Table()));
V v_242 = values_241[0];
V v_243 = V.Nil;
v_243 = new V((RuleFunction)(args_244 => {
V v_245 = args_244[0];
return R.Join(new V[] {}, new Results(R.Get(v_242, v_245)));
return Results.Empty;
}));
var values_246 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"plan_published"), R.Join(new V[] {v_239}, new Results(v_243))));
V v_247 = values_246[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_247, (V)"seq") == (V)1.0d), () => (V)(R.Get(v_242, R.Get(v_247, (V)"path")) == V.Nil)))));
foreach (var row_248 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"sounds"))))))) {
V v_249 = row_248[0];
V v_250 = row_248[1];
var values_251 = R.Join(new V[] {}, new Results(R.Get(v_240, R.Get(v_250, (V)"path"))));
var obj_252 = v_242;
V key_253 = R.Call(R.Get(v_5, (V)"published_path"), R.Join(new V[] {}, new Results(v_250))).First;
R.Set(obj_252, key_253, values_251[0]);
}
var values_254 = R.Join(new V[] {}, new Results(R.Get(v_247, (V)"bytes")));
var obj_255 = v_242;
V key_256 = R.Get(v_247, (V)"path");
R.Set(obj_255, key_256, values_254[0]);
var values_257 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"inspect_published"), R.Join(new V[] {}, new Results(v_243))));
V v_258 = values_257[0];
V v_259 = values_257[1];
V v_260 = values_257[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_259 == (V)"loaded"), () => (V)(R.Get(v_260, (V)"seq") == (V)1.0d)), () => (V)(R.Call(R.Get(v_5, (V)"encode"), R.Join(new V[] {}, new Results(v_258))).First == R.Call(R.Get(v_5, (V)"encode"), R.Join(new V[] {}, new Results(v_239))).First)))));
var values_261 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"plan_published"), R.Join(new V[] {v_239}, new Results(v_243))));
V v_262 = values_261[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_262, (V)"seq") == (V)2.0d), () => (V)(R.Get(v_262, (V)"path") != R.Get(v_247, (V)"path"))))));
var values_263 = R.Join(new V[] {}, new Results((V)"torn"));
var obj_264 = v_242;
V key_265 = R.Get(v_262, (V)"path");
R.Set(obj_264, key_265, values_263[0]);
var values_266 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"inspect_published"), R.Join(new V[] {}, new Results(v_243))));
V v_267 = values_266[0];
V v_268 = values_266[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_267, () => (V)(v_268 == (V)"recovered previous audio generation")))));
var values_269 = R.Join(new V[] {}, new Results(R.Get(v_262, (V)"bytes")));
var obj_270 = v_242;
V key_271 = R.Get(v_262, (V)"path");
R.Set(obj_270, key_271, values_269[0]);
var values_272 = R.Join(new V[] {}, new Results(V.Nil));
var obj_273 = v_242;
V key_274 = R.Call(R.Get(v_5, (V)"published_path"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_239, (V)"sounds"), (V)1.0d)))).First;
R.Set(obj_273, key_274, values_272[0]);
var values_275 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"inspect_published"), R.Join(new V[] {}, new Results(v_243))));
V v_276 = values_275[0];
V v_277 = values_275[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_276)), () => (V)(v_277 == (V)"corrupt audio")))));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_237, (V)"files"), R.Get(R.Get(R.Get(v_239, (V)"sounds"), (V)1.0d), (V)"path"))}, new Results((V)"published damage affected editable original")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"audio event routing and wall-time playback do not mutate game snapshots"}, new Results(new V((RuleFunction)(args_278 => {

var values_279 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"showcase"), R.Join(new V[] {}, new Results((V)"audio_test"))));
V v_280 = values_279[0];
V v_281 = values_279[1];
var values_282 = R.Join(new V[] {}, new Results(R.Table()));
V v_283 = values_282[0];
foreach (var row_284 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_280, (V)"sounds"))))))) {
V v_285 = row_284[0];
V v_286 = row_284[1];
var values_287 = R.Join(new V[] {}, new Results(R.Get(v_281, R.Get(v_286, (V)"path"))));
var obj_288 = v_283;
V key_289 = R.Call(R.Get(v_5, (V)"published_path"), R.Join(new V[] {}, new Results(v_286))).First;
R.Set(obj_288, key_289, values_287[0]);
}
var values_292 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"plan_published"), R.Join(new V[] {v_280}, new Results(new V((RuleFunction)(args_290 => {
V v_291 = args_290[0];
return R.Join(new V[] {}, new Results(R.Get(v_283, v_291)));
return Results.Empty;
}))))));
V v_293 = values_292[0];
var values_294 = R.Join(new V[] {}, new Results(R.Get(v_293, (V)"bytes")));
var obj_295 = v_283;
V key_296 = R.Get(v_293, (V)"path");
R.Set(obj_295, key_296, values_294[0]);
var values_297 = R.Join(new V[] {}, new Results((V)0.0d));
V v_298 = values_297[0];
var values_303 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_299 => {
V v_300 = args_299[0];
return R.Join(new V[] {}, new Results(R.Get(v_283, v_300)));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_301 => {

var values_302 = R.Join(new V[] {}, new Results((V)(v_298 + (V)1.0d)));
v_298 = values_302[0];

return R.Join(new V[] {}, R.Call(v_48, Results.Empty));
return Results.Empty;
}))))));
V v_304 = values_303[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_304, (V)"pack"), () => R.Invoke(R.Get(v_304, (V)"status"), "match", R.Join(new V[] {}, new Results((V)"refreshed"))).First), () => (V)(v_298 == (V)8.0d)))));
var values_305 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_306 = values_305[0];
var values_307 = R.Join(new V[] {}, R.Call(R.Get(v_306, (V)"new"), R.Join(new V[] {(V)7.0d}, new Results((V)"core:map/test_room"))));
V v_308 = values_307[0];
var values_309 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_306, (V)"snapshot"), R.Join(new V[] {}, new Results(v_308))))));
V v_310 = values_309[0];
var values_311 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"Moved"), new Entry((V)"source_id", R.Get(v_308, (V)"player_id")), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d), new Entry((V)"source_position", R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))))));
V v_312 = values_311[0];
var values_313 = R.Join(new V[] {}, R.Invoke(v_304, "event", R.Join(new V[] {v_312}, new Results(v_308))));
V v_314 = values_313[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_314) == (V)1.0d), () => (V)(R.Get(R.Get(v_314, (V)1.0d), (V)"cue_id") == (V)"core:cue/player/move")))));
var values_315 = R.Join(new V[] {}, R.Invoke(v_304, "event", R.Join(new V[] {v_312}, new Results(v_308))));
V v_316 = values_315[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_316) == (V)0.0d)}, new Results((V)"audio cooldown ignored")));
R.Invoke(v_304, "update", R.Join(new V[] {}, new Results((V)0.1d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Invoke(v_304, "event", R.Join(new V[] {v_312}, new Results(v_308))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_304, "set_scene", R.Join(new V[] {}, new Results((V)"core:cue/music/title")))));
R.Invoke(v_304, "update", R.Join(new V[] {}, new Results((V)0.3d)));
var values_317 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_304, (V)"music"), (V)"source")));
V v_318 = values_317[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_318, (V)"volume") > (V)0.0d), () => R.Get(v_318, (V)"loop")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_304, "set_scene", R.Join(new V[] {}, new Results((V)"core:cue/music/title"))).First)))));
R.Invoke(v_304, "set_scene", R.Join(new V[] {}, new Results((V)"core:cue/music/explore/overgrown_ruins")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_304, (V)"retiring_music"))));
{ double end_321 = R.Num((V)4.0d), step_322 = R.Num((V)1.0d);
for (double i_320 = R.Num((V)1.0d); step_322 > 0 ? i_320 <= end_321 : i_320 >= end_321; i_320 += step_322) {
V v_319 = i_320;
R.Invoke(v_304, "update", R.Join(new V[] {}, new Results((V)0.25d)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_304, (V)"retiring_music") == V.Nil))));
var values_323 = R.Join(new V[] {}, R.Invoke(v_304, "refresh", Results.Empty));
V v_324 = values_323[0];
V v_325 = values_323[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_324, () => R.Invoke(v_325, "match", R.Join(new V[] {}, new Results((V)"unchanged"))).First))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_17, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_306, (V)"snapshot"), R.Join(new V[] {}, new Results(v_308))))).First}, new Results(v_310)));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"audio voices, variants, mixer and pause obey presentation-only limits"}, new Results(new V((RuleFunction)(args_326 => {

var values_327 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"showcase"), R.Join(new V[] {}, new Results((V)"audio_test"))));
V v_328 = values_327[0];
V v_329 = values_327[1];
var values_330 = Results.Empty;
V v_331 = values_330[0];
foreach (var row_332 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_328, (V)"bindings"))))))) {
V v_333 = row_332[0];
V v_334 = row_332[1];
if (R.Truth((V)(R.Get(v_334, (V)"cue_id") == (V)"core:cue/player/move"))) {
var values_335 = R.Join(new V[] {}, new Results(v_334));
v_331 = values_335[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_331)));
var values_336 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_337 = v_331;
V key_338 = (V)"cooldown_ms";
R.Set(obj_337, key_338, values_336[0]);

var values_339 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_340 = v_331;
V key_341 = (V)"max_voices";
R.Set(obj_340, key_341, values_339[0]);
var values_342 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"asset_id", R.Get(R.Get(R.Get(v_328, (V)"sounds"), (V)2.0d), (V)"id")), new Entry((V)"weight", (V)2.0d))));
var obj_343 = R.Get(v_331, (V)"variants");
V key_344 = (V)2.0d;
R.Set(obj_343, key_344, values_342[0]);
var values_345 = R.Join(new V[] {}, new Results((V)0.5d));
var obj_346 = R.Get(R.Get(v_328, (V)"mix"), (V)"player");
V key_347 = (V)"volume";
R.Set(obj_346, key_347, values_345[0]);
var values_348 = R.Join(new V[] {}, new Results(R.Table()));
V v_349 = values_348[0];
foreach (var row_350 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_328, (V)"sounds"))))))) {
V v_351 = row_350[0];
V v_352 = row_350[1];
var values_353 = R.Join(new V[] {}, new Results(R.Get(v_329, R.Get(v_352, (V)"path"))));
var obj_354 = v_349;
V key_355 = R.Call(R.Get(v_5, (V)"published_path"), R.Join(new V[] {}, new Results(v_352))).First;
R.Set(obj_354, key_355, values_353[0]);
}
var values_358 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"plan_published"), R.Join(new V[] {v_328}, new Results(new V((RuleFunction)(args_356 => {
V v_357 = args_356[0];
return R.Join(new V[] {}, new Results(R.Get(v_349, v_357)));
return Results.Empty;
}))))));
V v_359 = values_358[0];
var values_360 = R.Join(new V[] {}, new Results(R.Get(v_359, (V)"bytes")));
var obj_361 = v_349;
V key_362 = R.Get(v_359, (V)"path");
R.Set(obj_361, key_362, values_360[0]);
var values_366 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_363 => {
V v_364 = args_363[0];
return R.Join(new V[] {}, new Results(R.Get(v_349, v_364)));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_365 => {

return R.Join(new V[] {}, R.Call(v_48, Results.Empty));
return Results.Empty;
}))))));
V v_367 = values_366[0];
var values_368 = R.Join(new V[] {}, R.Invoke(v_367, "emit", R.Join(new V[] {}, new Results((V)"core:cue/player/move"))));
V v_369 = values_368[0];
V v_370 = values_368[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_369, () => (V)(v_370 == R.Get(R.Get(R.Get(v_328, (V)"sounds"), (V)1.0d), (V)"id"))), () => (V)(R.Get(R.Get(R.Get(R.Get(v_367, (V)"voices"), (V)1.0d), (V)"source"), (V)"volume") == (V)0.5d)))));
var values_371 = R.Join(new V[] {}, R.Invoke(v_367, "emit", R.Join(new V[] {}, new Results((V)"core:cue/player/move"))));
V v_372 = values_371[0];
V v_373 = values_371[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_372)), () => (V)(v_373 == (V)"voice cap")))));
R.Invoke(v_367, "set_paused", R.Join(new V[] {}, new Results((V)true)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(R.Get(R.Get(R.Get(v_367, (V)"voices"), (V)1.0d), (V)"source"), "isPlaying", Results.Empty).First)))));
R.Invoke(v_367, "set_paused", R.Join(new V[] {}, new Results((V)false)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(R.Get(R.Get(R.Get(v_367, (V)"voices"), (V)1.0d), (V)"source"), "isPlaying", Results.Empty)));
R.Invoke(R.Get(R.Get(R.Get(v_367, (V)"voices"), (V)1.0d), (V)"source"), "stop", Results.Empty);
var values_374 = R.Join(new V[] {}, R.Invoke(v_367, "emit", R.Join(new V[] {}, new Results((V)"core:cue/player/move"))));
V v_375 = values_374[0];
V v_376 = values_374[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_375, () => (V)(v_376 == R.Get(R.Get(R.Get(v_328, (V)"sounds"), (V)2.0d), (V)"id"))))));
var values_377 = R.Join(new V[] {}, new Results((V)true));
var obj_378 = R.Get(R.Get(R.Get(v_367, (V)"pack"), (V)"mix"), (V)"player");
V key_379 = (V)"mute";
R.Set(obj_378, key_379, values_377[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Invoke(v_367, "emit", R.Join(new V[] {}, new Results((V)"core:cue/player/move"))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_367, "set_ambience", R.Join(new V[] {}, new Results((V)"core:cue/ambient/overgrown_ruins")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_367, (V)"ambience"), (V)"source"), (V)"loop"))));
R.Invoke(v_367, "set_ambience", R.Join(new V[] {}, new Results(V.Nil)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_367, (V)"ambience") == V.Nil))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"partial published audio overrides one cue and retains original unbound sounds"}, new Results(new V((RuleFunction)(args_380 => {

var values_381 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"showcase"), R.Join(new V[] {}, new Results((V)"audio_test"))));
V v_382 = values_381[0];
V v_383 = values_381[1];
var values_384 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_382, (V)"bindings"), (V)1.0d)));
V v_385 = values_384[0];
var values_386 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_385))));
var obj_387 = v_382;
V key_388 = (V)"bindings";
R.Set(obj_387, key_388, values_386[0]);
var values_389 = R.Join(new V[] {}, new Results(R.Table()));
V v_390 = values_389[0];
foreach (var row_391 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_382, (V)"sounds"))))))) {
V v_392 = row_391[0];
V v_393 = row_391[1];
var values_394 = R.Join(new V[] {}, new Results(R.Get(v_383, R.Get(v_393, (V)"path"))));
var obj_395 = v_390;
V key_396 = R.Call(R.Get(v_5, (V)"published_path"), R.Join(new V[] {}, new Results(v_393))).First;
R.Set(obj_395, key_396, values_394[0]);
}
var values_399 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"plan_published"), R.Join(new V[] {v_382}, new Results(new V((RuleFunction)(args_397 => {
V v_398 = args_397[0];
return R.Join(new V[] {}, new Results(R.Get(v_390, v_398)));
return Results.Empty;
}))))));
V v_400 = values_399[0];
var values_401 = R.Join(new V[] {}, new Results(R.Get(v_400, (V)"bytes")));
var obj_402 = v_390;
V key_403 = R.Get(v_400, (V)"path");
R.Set(obj_402, key_403, values_401[0]);
var values_407 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_404 => {
V v_405 = args_404[0];
return R.Join(new V[] {}, new Results(R.Get(v_390, v_405)));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_406 => {

return R.Join(new V[] {}, R.Call(v_48, Results.Empty));
return Results.Empty;
}))))));
V v_408 = values_407[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_408, (V)"custom_binding_count") == (V)1.0d), () => (V)(R.Get(v_408, (V)"fallback_binding_count") >= (V)20.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_408, "emit", R.Join(new V[] {}, new Results((V)"core:cue/player/move")))));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Invoke(v_408, "emit", R.Join(new V[] {}, new Results((V)"core:cue/enemy/telegraph"))).First}, new Results((V)"an unbound enemy cue became silent under a partial custom pack")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_408, "set_scene", R.Join(new V[] {}, new Results((V)"core:cue/music/explore/fungal_hollows")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_408, (V)"music"), () => R.Invoke(R.Get(R.Get(v_408, (V)"music"), (V)"source"), "isPlaying", Results.Empty).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"audio refresh rejects incompatible newer data and falls back on corruption"}, new Results(new V((RuleFunction)(args_409 => {

var values_410 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"showcase"), R.Join(new V[] {}, new Results((V)"audio_test"))));
V v_411 = values_410[0];
V v_412 = values_410[1];
var values_413 = R.Join(new V[] {}, new Results(R.Table()));
V v_414 = values_413[0];
foreach (var row_415 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_411, (V)"sounds"))))))) {
V v_416 = row_415[0];
V v_417 = row_415[1];
var values_418 = R.Join(new V[] {}, new Results(R.Get(v_412, R.Get(v_417, (V)"path"))));
var obj_419 = v_414;
V key_420 = R.Call(R.Get(v_5, (V)"published_path"), R.Join(new V[] {}, new Results(v_417))).First;
R.Set(obj_419, key_420, values_418[0]);
}
var values_423 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"plan_published"), R.Join(new V[] {v_411}, new Results(new V((RuleFunction)(args_421 => {
V v_422 = args_421[0];
return R.Join(new V[] {}, new Results(R.Get(v_414, v_422)));
return Results.Empty;
}))))));
V v_424 = values_423[0];
var values_425 = R.Join(new V[] {}, new Results(R.Get(v_424, (V)"bytes")));
var obj_426 = v_414;
V key_427 = R.Get(v_424, (V)"path");
R.Set(obj_426, key_427, values_425[0]);
var values_431 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_428 => {
V v_429 = args_428[0];
return R.Join(new V[] {}, new Results(R.Get(v_414, v_429)));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_430 => {

return R.Join(new V[] {}, R.Call(v_48, Results.Empty));
return Results.Empty;
}))))));
V v_432 = values_431[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_432, (V)"pack"), (V)"sounds")) == (V)8.0d))));
var values_433 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"encode"), R.Join(new V[] {}, new Results(v_411))))));
V v_434 = values_433[0];

var values_435 = R.Join(new V[] {}, new Results((V)"future-audio/2"));
var obj_436 = v_434;
V key_437 = (V)"schema";
R.Set(obj_436, key_437, values_435[0]);
var values_438 = R.Join(new V[] {}, R.Call(R.Get(v_17, (V)"encode"), R.Join(new V[] {}, new Results(v_434))));
V v_439 = values_438[0];
var values_440 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_7, (V)"paths"), (V)2.0d)));
V v_441 = values_440[0];
var values_442 = R.Join(new V[] {}, R.Invoke((V)"ROAG-STUDIO-PUBLISHED-AUDIO/1\n2\n%d\n%s\n%s", "format", R.Join(new V[] {(V)R.Length(v_439), R.Call(R.Get(v_5, (V)"checksum"), R.Join(new V[] {}, new Results(v_439))).First}, new Results(v_439))));
var obj_443 = v_414;
V key_444 = v_441;
R.Set(obj_443, key_444, values_442[0]);
var values_445 = R.Join(new V[] {}, R.Invoke(v_432, "refresh", Results.Empty));
V v_446 = values_445[0];
V v_447 = values_445[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(v_446)), () => R.Invoke(v_447, "match", R.Join(new V[] {}, new Results((V)"rejected"))).First), () => (V)((V)R.Length(R.Get(R.Get(v_432, (V)"pack"), (V)"sounds")) == (V)8.0d)))));
var values_451 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_448 => {
V v_449 = args_448[0];
return R.Join(new V[] {}, new Results(R.Get(v_414, v_449)));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_450 => {

return R.Join(new V[] {}, R.Call(v_48, Results.Empty));
return Results.Empty;
}))))));
V v_452 = values_451[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(R.Get(v_452, (V)"status"), "match", R.Join(new V[] {}, new Results((V)"built%-in"))).First, () => (V)((V)R.Length(R.Get(R.Get(v_452, (V)"pack"), (V)"bindings")) >= (V)27.0d)))));
var values_453 = R.Join(new V[] {}, new Results((V)"torn"));
var obj_454 = v_414;
V key_455 = v_441;
R.Set(obj_454, key_455, values_453[0]);
var values_459 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_456 => {
V v_457 = args_456[0];
return R.Join(new V[] {}, new Results(R.Get(v_414, v_457)));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_458 => {

return R.Join(new V[] {}, R.Call(v_48, Results.Empty));
return Results.Empty;
}))))));
V v_460 = values_459[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(R.Get(v_460, (V)"status"), "match", R.Join(new V[] {}, new Results((V)"refreshed"))).First, () => (V)((V)R.Length(R.Get(R.Get(v_460, (V)"pack"), (V)"bindings")) == (V)R.Length(R.Get(v_411, (V)"bindings")))))));
var values_461 = R.Join(new V[] {}, new Results((V)"torn"));
var obj_462 = v_414;
V key_463 = R.Get(v_424, (V)"path");
R.Set(obj_462, key_463, values_461[0]);
var values_467 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_464 => {
V v_465 = args_464[0];
return R.Join(new V[] {}, new Results(R.Get(v_414, v_465)));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_466 => {

return R.Join(new V[] {}, R.Call(v_48, Results.Empty));
return Results.Empty;
}))))));
V v_468 = values_467[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Invoke(R.Get(v_468, (V)"status"), "match", R.Join(new V[] {}, new Results((V)"corrupt"))).First, () => (V)((V)R.Length(R.Get(R.Get(v_468, (V)"pack"), (V)"bindings")) >= (V)27.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"player music and effects volumes update active sounds independently"}, new Results(new V((RuleFunction)(args_469 => {

var values_472 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"new"), R.Join(new V[] {new V((RuleFunction)(args_470 => {

return R.Join(new V[] {}, new Results(V.Nil));
return Results.Empty;
}))}, new Results(new V((RuleFunction)(args_471 => {

return R.Join(new V[] {}, R.Call(v_48, Results.Empty));
return Results.Empty;
}))))));
V v_473 = values_472[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_473, "emit", R.Join(new V[] {}, new Results((V)"core:cue/player/move")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Invoke(v_473, "set_scene", R.Join(new V[] {}, new Results((V)"core:cue/music/explore/overgrown_ruins")))));
var values_474 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_473, (V)"voices"), (V)1.0d)));
V v_475 = values_474[0];
var values_476 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_475, (V)"source"), (V)"volume")));
V v_477 = values_476[0];
var values_478 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_473, (V)"music"), (V)"target")));
V v_479 = values_478[0];
R.Invoke(v_473, "set_user_mix", R.Join(new V[] {(V)0.25d}, new Results((V)0.5d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_475, (V)"source"), (V)"volume") - (V)(v_477 * (V)0.5d))))).First < (V)1e-06d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_473, (V)"music"), (V)"target") - (V)(v_479 * (V)0.25d))))).First < (V)1e-06d))));
{ double end_482 = R.Num((V)20.0d), step_483 = R.Num((V)1.0d);
for (double i_481 = R.Num((V)1.0d); step_483 > 0 ? i_481 <= end_482 : i_481 >= end_482; i_481 += step_483) {
V v_480 = i_481;
R.Invoke(v_473, "update", R.Join(new V[] {}, new Results((V)0.25d)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_473, (V)"music"), (V)"source"), (V)"volume") - (V)(v_479 * (V)0.25d))))).First < (V)1e-06d))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
