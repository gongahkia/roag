// Static C# port of src/studio_audio_store.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 70ef03f780d70057159ad1ff4a0d54ce7dac9f271d221dc9af8efb1978bfef81
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_audio_store { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_audio"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_project"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"paths", R.Table(new Entry((V)1, (V)"assets/studio/audio-a.pack"), new Entry((V)2, (V)"assets/studio/audio-b.pack"))))));
V v_6 = values_5[0];
V v_7 = V.Nil;
v_7 = new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
V v_10 = args_8[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"valid_project_id"), R.Join(new V[] {}, new Results(v_9))).First}, new Results((V)"invalid Studio audio project ID")));
return R.Join(new V[] {}, new Results(R.Concat((V)"studio/projects/", R.Concat(v_9, R.Concat((V)"/audio-", R.Concat(v_10, (V)".pack"))))));
return Results.Empty;
}));
V v_11 = V.Nil;
v_11 = new V((RuleFunction)(args_12 => {
V v_13 = args_12[0];
V v_14 = args_12[1];
V v_15 = args_12[2];
var values_16 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"encode"), R.Join(new V[] {}, new Results(v_14))));
V v_17 = values_16[0];
return R.Join(new V[] {}, new Results(R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {R.Table(new Entry((V)1, R.Concat((V)"ROAG-STUDIO-", R.Concat(v_15, (V)"/1"))), new Entry((V)2, v_13), new Entry((V)3, (V)R.Length(v_17)), new Entry(4, R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, new Results(v_17)))))}, new Results((V)"\n"))).First, R.Concat((V)"\n", v_17))));
return Results.Empty;
}));
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_20))).First == (V)"string"), () => (V)((V)R.Length(v_20) <= (V)270000.0d))}, new Results((V)"invalid audio record size")));
var values_22 = R.Join(new V[] {}, R.Invoke(v_20, "match", R.Join(new V[] {}, new Results((V)"^([^\n]+)\n([^\n]+)\n([^\n]+)\n([^\n]+)\n(.*)$"))));
V v_23 = values_22[0];
V v_24 = values_22[1];
V v_25 = values_22[2];
V v_26 = values_22[3];
V v_27 = values_22[4];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_23 == R.Concat((V)"ROAG-STUDIO-", R.Concat(v_21, (V)"/1")))}, new Results((V)"incompatible audio record")));
var values_28 = R.Join(new V[] {R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_24))).First}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_25))));
v_24 = values_28[0];
v_25 = values_28[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_24, () => (V)((V)(v_24 % (V)1.0d) == (V)0.0d)), () => (V)(v_24 >= (V)1.0d)), () => (V)(v_24 < (V)9007199254740991.0d)), () => v_25), () => (V)((V)(v_25 % (V)1.0d) == (V)0.0d)), () => (V)(v_25 <= (V)262144.0d)), () => (V)((V)R.Length(v_27) == v_25)), () => (V)(R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, new Results(v_27))).First == v_26))}, new Results((V)"corrupt audio record")));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"seq", v_24), new Entry((V)"payload", v_27))));
return Results.Empty;
}));
V v_29 = V.Nil;
v_29 = new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
V v_32 = args_30[1];
V v_33 = args_30[2];
V v_34 = args_30[3];
V v_35 = args_30[4];
var values_36 = R.Join(new V[] {R.Table()}, new Results((V)0.0d));
V v_37 = values_36[0];
V v_38 = values_36[1];
foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_32)))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
var values_42 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_31}, new Results(v_41))));
V v_43 = values_42[0];
V v_44 = values_42[1];
if (R.Truth(R.And(v_43, () => v_44))) {
var values_45 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_18, v_44}, new Results(v_33))));
V v_46 = values_45[0];
V v_47 = values_45[1];
if (R.Truth(v_46)) {
var values_48 = R.Join(new V[] {}, new Results(v_40));
var obj_49 = v_47;
V key_50 = (V)"slot";
R.Set(obj_49, key_50, values_48[0]);

var values_51 = R.Join(new V[] {}, new Results(v_47));
var obj_52 = v_37;
V key_53 = (V)((V)R.Length(v_37) + (V)1.0d);
R.Set(obj_52, key_53, values_51[0]);
} else {
var values_54 = R.Join(new V[] {}, new Results((V)(v_38 + (V)1.0d)));
v_38 = values_54[0];
}
} else if (R.Truth((V)(!R.Truth(v_43)))) {
var values_55 = R.Join(new V[] {}, new Results((V)(v_38 + (V)1.0d)));
v_38 = values_55[0];
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_37}, new Results(new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
V v_58 = args_56[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_57, (V)"seq") > R.Get(v_58, (V)"seq"))));
return Results.Empty;
})))));
foreach (var row_59 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_37)))))) {
V v_60 = row_59[0];
V v_61 = row_59[1];
var values_62 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"decode")}, new Results(R.Get(v_61, (V)"payload")))));
V v_63 = values_62[0];
V v_64 = values_62[1];
if (R.Truth((V)(!R.Truth(v_63)))) {
if (R.Truth((V)(v_60 == (V)1.0d))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"incompatible audio metadata: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_64))).First)}, new Results(v_61));
}
} else {
var values_65 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"validate"), v_64}, new Results(v_34))));
V v_66 = values_65[0];
V v_67 = values_65[1];
if (R.Truth(R.And(v_35, () => (V)(R.Get(v_64, (V)"project_id") != v_35)))) {
var values_68 = R.Join(new V[] {}, new Results((V)false));
v_66 = values_68[0];

var values_69 = R.Join(new V[] {}, new Results((V)"wrong audio project ID"));
v_67 = values_69[0];
}
if (R.Truth(v_66)) {
return R.Join(new V[] {v_64, R.Or(R.And(R.Or((V)(v_60 > (V)1.0d), () => (V)(v_38 > (V)0.0d)), () => (V)"recovered previous audio generation"), () => (V)"loaded")}, new Results(v_61));
}
if (R.Truth(R.And((V)(v_60 == (V)1.0d), () => (V)(!R.Truth(R.Invoke(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_67))).First, "match", R.Join(new V[] {}, new Results((V)"missing/corrupt audio source"))).First))))) {
return R.Join(new V[] {V.Nil, R.Concat((V)"invalid audio metadata: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_67))).First)}, new Results(v_61));
}
var values_70 = R.Join(new V[] {}, new Results((V)(v_38 + (V)1.0d)));
v_38 = values_70[0];
}
}
return R.Join(new V[] {V.Nil}, new Results(R.Or(R.And((V)(v_38 > (V)0.0d), () => (V)"corrupt audio"), () => (V)"absent")));
return Results.Empty;
}));
R.Set(v_6, (V)"new", new V((RuleFunction)(args_71 => {
V v_72 = args_71[0];
return R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"io", v_72))}, new Results(R.Table(new Entry((V)"__index", v_6))))));
return Results.Empty;
})));
R.Set(v_6, "open", new V((RuleFunction)(args_73 => {
V v_74 = args_73[0];
V v_75 = args_73[1];
var values_76 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Call(v_7, R.Join(new V[] {v_75}, new Results((V)"a"))).First), new Entry(2, R.Call(v_7, R.Join(new V[] {v_75}, new Results((V)"b")))))));
V v_77 = values_76[0];
var values_82 = R.Join(new V[] {}, R.Call(v_29, R.Join(new V[] {new V((RuleFunction)(args_78 => {
V v_79 = args_78[0];
return R.Join(new V[] {}, R.Invoke(R.Get(v_74, (V)"io"), "read", R.Join(new V[] {}, new Results(v_79))));
return Results.Empty;
})), v_77, (V)"AUDIO-SOURCE", new V((RuleFunction)(args_80 => {
V v_81 = args_80[0];
return R.Join(new V[] {}, R.Invoke(R.Get(v_74, (V)"io"), "read", R.Join(new V[] {}, new Results(v_81))));
return Results.Empty;
}))}, new Results(v_75))));
V v_83 = values_82[0];
V v_84 = values_82[1];
V v_85 = values_82[2];
if (R.Truth((V)(v_84 == (V)"absent"))) {
var values_86 = R.Join(new V[] {}, new Results((V)"missing audio document"));
v_84 = values_86[0];
}
return R.Join(new V[] {v_83, v_84}, new Results(v_85));
return Results.Empty;
})));
R.Set(v_6, "import", new V((RuleFunction)(args_87 => {
V v_88 = args_87[0];
V v_89 = args_87[1];
V v_90 = args_87[2];
V v_91 = args_87[3];
V v_92 = args_87[4];
V v_93 = args_87[5];
var values_94 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"make_sound"), R.Join(new V[] {R.Get(v_89, (V)"project_id"), v_90, v_91, v_92}, new Results(v_93))));
V v_95 = values_94[0];
var values_99 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(new V((RuleFunction)(args_96 => {

R.Invoke(R.Get(v_88, (V)"io"), "mkdir", R.Join(new V[] {}, new Results(R.Concat((V)"studio/projects/", R.Concat(R.Get(v_89, (V)"project_id"), (V)"/audio")))));
var values_97 = R.Join(new V[] {}, R.Invoke(R.Get(v_88, (V)"io"), "read", R.Join(new V[] {}, new Results(R.Get(v_95, (V)"path")))));
V v_98 = values_97[0];
if (R.Truth(v_98)) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_98 == v_92)}, new Results((V)"immutable audio source path collision")));
} else {
R.Invoke(R.Get(v_88, (V)"io"), "write", R.Join(new V[] {R.Get(v_95, (V)"path")}, new Results(v_92)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Invoke(R.Get(v_88, (V)"io"), "read", R.Join(new V[] {}, new Results(R.Get(v_95, (V)"path")))).First == v_92)}, new Results((V)"audio source read-back failed")));
}
return Results.Empty;
}))))));
V v_100 = values_99[0];
V v_101 = values_99[1];
if (R.Truth((V)(!R.Truth(v_100)))) {
return R.Join(new V[] {V.Nil}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_101))));
}
return R.Join(new V[] {}, new Results(v_95));
return Results.Empty;
})));
R.Set(v_6, "save", new V((RuleFunction)(args_102 => {
V v_103 = args_102[0];
V v_104 = args_102[1];
var values_107 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"validate"), v_104}, new Results(new V((RuleFunction)(args_105 => {
V v_106 = args_105[0];
return R.Join(new V[] {}, R.Invoke(R.Get(v_103, (V)"io"), "read", R.Join(new V[] {}, new Results(v_106))));
return Results.Empty;
}))))));
V v_108 = values_107[0];
V v_109 = values_107[1];
if (R.Truth((V)(!R.Truth(v_108)))) {
return R.Join(new V[] {(V)false}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_109))));
}
var values_110 = R.Join(new V[] {}, R.Invoke(v_103, "open", R.Join(new V[] {}, new Results(R.Get(v_104, (V)"project_id")))));
V v_111 = values_110[0];
V v_112 = values_110[1];
V v_113 = values_110[2];
if (R.Truth(R.And(R.And((V)(!R.Truth(v_111)), () => (V)(v_112 != (V)"missing audio document")), () => (V)(v_112 != (V)"corrupt audio")))) {
return R.Join(new V[] {(V)false}, new Results(v_112));
}
var values_114 = R.Join(new V[] {}, new Results(R.Or(R.And(v_113, () => R.Or(R.And((V)(R.Get(v_113, (V)"slot") == (V)1.0d), () => (V)"b"), () => (V)"a")), () => (V)"a")));
V v_115 = values_114[0];
var values_116 = R.Join(new V[] {}, new Results(R.Or(R.And(v_113, () => (V)(R.Get(v_113, (V)"seq") + (V)1.0d)), () => (V)1.0d)));
V v_117 = values_116[0];
var values_118 = R.Join(new V[] {}, R.Call(v_11, R.Join(new V[] {v_117, v_104}, new Results((V)"AUDIO-SOURCE"))));
V v_119 = values_118[0];
var values_120 = R.Join(new V[] {}, R.Call(v_7, R.Join(new V[] {R.Get(v_104, (V)"project_id")}, new Results(v_115))));
V v_121 = values_120[0];
var values_123 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(new V((RuleFunction)(args_122 => {

R.Invoke(R.Get(v_103, (V)"io"), "mkdir", R.Join(new V[] {}, new Results(R.Concat((V)"studio/projects/", R.Get(v_104, (V)"project_id")))));
R.Invoke(R.Get(v_103, (V)"io"), "write", R.Join(new V[] {v_121}, new Results(v_119)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Invoke(R.Get(v_103, (V)"io"), "read", R.Join(new V[] {}, new Results(v_121))).First == v_119)}, new Results((V)"audio manifest read-back failed")));
return Results.Empty;
}))))));
V v_124 = values_123[0];
V v_125 = values_123[1];
if (R.Truth((V)(!R.Truth(v_124)))) {
return R.Join(new V[] {(V)false}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_125))));
}
var values_126 = R.Join(new V[] {}, R.Invoke(v_103, "open", R.Join(new V[] {}, new Results(R.Get(v_104, (V)"project_id")))));
V v_127 = values_126[0];
V v_128 = values_126[1];
V v_129 = values_126[2];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_127)), () => (V)(!R.Truth(v_129))), () => (V)(R.Get(v_129, (V)"seq") != v_117)))) {
return R.Join(new V[] {(V)false}, new Results(R.Concat((V)"audio verification failed: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_128))).First)));
}
return R.Join(new V[] {(V)true}, new Results(v_117));
return Results.Empty;
})));
R.Set(v_6, (V)"inspect_published", new V((RuleFunction)(args_130 => {
V v_131 = args_130[0];
return R.Join(new V[] {}, R.Call(v_29, R.Join(new V[] {v_131, R.Get(v_6, (V)"paths"), (V)"PUBLISHED-AUDIO"}, new Results(new V((RuleFunction)(args_132 => {
V v_133 = args_132[0];
var values_134 = R.Join(new V[] {}, R.Invoke(v_133, "match", R.Join(new V[] {}, new Results((V)"([^/]+)$"))));
V v_135 = values_134[0];
return R.Join(new V[] {}, new Results(R.And(v_135, () => R.Call(v_131, R.Join(new V[] {}, new Results(R.Concat((V)"assets/studio/audio/", v_135)))).First)));
return Results.Empty;
}))))));
return Results.Empty;
})));
R.Set(v_6, (V)"plan_published", new V((RuleFunction)(args_136 => {
V v_137 = args_136[0];
V v_138 = args_136[1];
R.Call(R.Get(v_2, (V)"validate"), R.Join(new V[] {}, new Results(v_137)));
var values_139 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"inspect_published"), R.Join(new V[] {}, new Results(v_138))));
V v_140 = values_139[0];
V v_141 = values_139[1];
V v_142 = values_139[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or(v_140, () => (V)(v_141 == (V)"absent")), () => (V)(v_141 == (V)"corrupt audio"))}, new Results(R.Concat((V)"refusing to overwrite incompatible published audio: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_141))).First))));
var values_143 = R.Join(new V[] {}, new Results(R.Or(R.And(v_142, () => R.Or(R.And((V)(R.Get(v_142, (V)"slot") == (V)1.0d), () => (V)2.0d), () => (V)1.0d)), () => (V)1.0d)));
V v_144 = values_143[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", R.Get(R.Get(v_6, (V)"paths"), v_144)), new Entry((V)"bytes", R.Call(v_11, R.Join(new V[] {R.Or(R.And(v_142, () => (V)(R.Get(v_142, (V)"seq") + (V)1.0d)), () => (V)1.0d), v_137}, new Results((V)"PUBLISHED-AUDIO"))).First), new Entry((V)"seq", R.Or(R.And(v_142, () => (V)(R.Get(v_142, (V)"seq") + (V)1.0d)), () => (V)1.0d)), new Entry((V)"overwrite", (V)(R.Call(v_138, R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"paths"), v_144)))).First != V.Nil)))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
