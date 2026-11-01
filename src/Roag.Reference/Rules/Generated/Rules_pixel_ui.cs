// Static C# port of src/pixel_ui.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: ffab28f34d04743bb265ae3aadc6f39ec6ae0380be8868aca35c6a50543b3c8a
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_pixel_ui { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"colors", R.Table(new Entry((V)"background", R.Table(new Entry((V)1, (V)0.025d), new Entry((V)2, (V)0.025d), new Entry((V)3, (V)0.04d))), new Entry((V)"panel", R.Table(new Entry((V)1, (V)0.12d), new Entry((V)2, (V)0.13d), new Entry((V)3, (V)0.32d))), new Entry((V)"edge", R.Table(new Entry((V)1, (V)0.46d), new Entry((V)2, (V)0.48d), new Entry((V)3, (V)0.63d))), new Entry((V)"light", R.Table(new Entry((V)1, (V)0.75d), new Entry((V)2, (V)0.77d), new Entry((V)3, (V)0.86d))), new Entry((V)"text", R.Table(new Entry((V)1, (V)0.88d), new Entry((V)2, (V)0.89d), new Entry((V)3, (V)0.94d))), new Entry((V)"muted", R.Table(new Entry((V)1, (V)0.52d), new Entry((V)2, (V)0.54d), new Entry((V)3, (V)0.64d))), new Entry((V)"selected", R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.91d), new Entry((V)3, (V)0.48d))))))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Get(v_2, (V)"colors")));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"background", R.Table(new Entry((V)1, (V)0.03d), new Entry((V)2, (V)0.03d), new Entry((V)3, (V)0.03d))), new Entry((V)"panel", R.Table(new Entry((V)1, (V)0.075d), new Entry((V)2, (V)0.075d), new Entry((V)3, (V)0.075d))), new Entry((V)"edge", R.Table(new Entry((V)1, (V)0.42d), new Entry((V)2, (V)0.42d), new Entry((V)3, (V)0.42d))), new Entry((V)"light", R.Table(new Entry((V)1, (V)0.74d), new Entry((V)2, (V)0.74d), new Entry((V)3, (V)0.74d))), new Entry((V)"text", R.Table(new Entry((V)1, (V)0.9d), new Entry((V)2, (V)0.9d), new Entry((V)3, (V)0.9d))), new Entry((V)"muted", R.Table(new Entry((V)1, (V)0.5d), new Entry((V)2, (V)0.5d), new Entry((V)3, (V)0.5d))), new Entry((V)"selected", R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"background", R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)0.0d))), new Entry((V)"panel", R.Table(new Entry((V)1, (V)0.04d), new Entry((V)2, (V)0.06d), new Entry((V)3, (V)0.12d))), new Entry((V)"edge", R.Table(new Entry((V)1, (V)0.85d), new Entry((V)2, (V)0.87d), new Entry((V)3, (V)0.97d))), new Entry((V)"light", R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))), new Entry((V)"text", R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))), new Entry((V)"muted", R.Table(new Entry((V)1, (V)0.78d), new Entry((V)2, (V)0.82d), new Entry((V)3, (V)0.88d))), new Entry((V)"selected", R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.92d), new Entry((V)3, (V)0.35d))))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"background", R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)0.0d))), new Entry((V)"panel", R.Table(new Entry((V)1, (V)0.04d), new Entry((V)2, (V)0.04d), new Entry((V)3, (V)0.04d))), new Entry((V)"edge", R.Table(new Entry((V)1, (V)0.88d), new Entry((V)2, (V)0.88d), new Entry((V)3, (V)0.88d))), new Entry((V)"light", R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))), new Entry((V)"text", R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))), new Entry((V)"muted", R.Table(new Entry((V)1, (V)0.78d), new Entry((V)2, (V)0.78d), new Entry((V)3, (V)0.78d))), new Entry((V)"selected", R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"enabled", (V)false), new Entry((V)"rows", R.Table()), new Entry((V)"age", (V)0.0d), new Entry((V)"frame", (V)0.0d))));
V v_12 = values_11[0];
R.Set(v_2, (V)"update", new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
V v_15 = args_13[1];
var values_16 = R.Join(new V[] {}, new Results((V)(v_15 == (V)true)));
var obj_17 = v_12;
V key_18 = (V)"enabled";
R.Set(obj_17, key_18, values_16[0]);
if (R.Truth((V)(!R.Truth(R.Get(v_12, (V)"enabled"))))) {
var values_19 = R.Join(new V[] {}, new Results(R.Table()));
var obj_20 = v_12;
V key_21 = (V)"rows";
R.Set(obj_20, key_21, values_19[0]);

var values_22 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_23 = v_12;
V key_24 = (V)"age";
R.Set(obj_23, key_24, values_22[0]);

return Results.Empty;
}
var values_25 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results(v_14))));
v_14 = values_25[0];
var values_26 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_12, (V)"age") + v_14)))));
var obj_27 = v_12;
V key_28 = (V)"age";
R.Set(obj_27, key_28, values_26[0]);
foreach (var row_29 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_12, (V)"rows"))))))) {
V v_30 = row_29[0];
V v_31 = row_29[1];
var values_32 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_31, (V)"age") + v_14)))));
var obj_33 = v_31;
V key_34 = (V)"age";
R.Set(obj_33, key_34, values_32[0]);
var values_35 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_31, (V)"press") + v_14)))));
var obj_36 = v_31;
V key_37 = (V)"press";
R.Set(obj_36, key_37, values_35[0]);
}
return Results.Empty;
})));
R.Set(v_2, (V)"begin_frame", new V((RuleFunction)(args_38 => {
V v_39 = args_38[0];
V v_40 = args_38[1];
if (R.Truth((V)(R.Get(v_12, (V)"view") != v_39))) {
var values_41 = R.Join(new V[] {}, new Results(R.Table()));
var obj_42 = v_12;
V key_43 = (V)"rows";
R.Set(obj_42, key_43, values_41[0]);

var values_44 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_45 = v_12;
V key_46 = (V)"age";
R.Set(obj_45, key_46, values_44[0]);

var values_47 = R.Join(new V[] {}, new Results(v_39));
var obj_48 = v_12;
V key_49 = (V)"view";
R.Set(obj_48, key_49, values_47[0]);
}
if (R.Truth((V)(v_40 != V.Nil))) {
R.Call(R.Get(v_2, (V)"update"), R.Join(new V[] {(V)0.0d}, new Results(v_40)));
}
var values_50 = R.Join(new V[] {}, new Results((V)(R.Get(v_12, (V)"frame") + (V)1.0d)));
var obj_51 = v_12;
V key_52 = (V)"frame";
R.Set(obj_51, key_52, values_50[0]);
foreach (var row_53 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_12, (V)"rows"))))))) {
V v_54 = row_53[0];
V v_55 = row_53[1];
if (R.Truth((V)(R.Get(v_55, (V)"frame") < (V)(R.Get(v_12, (V)"frame") - (V)1.0d)))) {
var values_56 = R.Join(new V[] {}, new Results(V.Nil));
var obj_57 = R.Get(v_12, (V)"rows");
V key_58 = v_54;
R.Set(obj_57, key_58, values_56[0]);
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"press", new V((RuleFunction)(args_59 => {

if (R.Truth((V)(!R.Truth(R.Get(v_12, (V)"enabled"))))) {
return Results.Empty;
}
foreach (var row_60 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_12, (V)"rows"))))))) {
V v_61 = row_60[0];
V v_62 = row_60[1];
if (R.Truth(R.And(R.And(R.Get(v_62, (V)"selected"), () => (V)(!R.Truth(R.Get(v_62, (V)"disabled")))), () => (V)(R.Get(v_62, (V)"frame") == R.Get(v_12, (V)"frame"))))) {
var values_63 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_64 = v_62;
V key_65 = (V)"press";
R.Set(obj_64, key_65, values_63[0]);
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"option_feedback", new V((RuleFunction)(args_66 => {
V v_67 = args_66[0];
V v_68 = args_66[1];
V v_69 = args_66[2];
V v_70 = args_66[3];
V v_71 = args_66[4];
V v_72 = args_66[5];
if (R.Truth((V)(!R.Truth(R.Get(v_12, (V)"enabled"))))) {
return R.Join(new V[] {(V)0.0d, (V)0.0d}, new Results((V)0.0d));
}
var values_73 = R.Join(new V[] {}, new Results(R.Concat(v_68, R.Concat((V)":", R.Concat(v_69, R.Concat((V)":", v_70))))));
V v_74 = values_73[0];
var values_75 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_12, (V)"rows"), v_74)));
V v_76 = values_75[0];
if (R.Truth((V)(!R.Truth(v_76)))) {
var values_77 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"age", (V)1.0d), new Entry((V)"press", (V)1.0d))));
v_76 = values_77[0];

var values_78 = R.Join(new V[] {}, new Results(v_76));
var obj_79 = R.Get(v_12, (V)"rows");
V key_80 = v_74;
R.Set(obj_79, key_80, values_78[0]);
}
if (R.Truth(R.And(v_71, () => R.Or((V)(!R.Truth(R.Get(v_76, (V)"selected"))), () => (V)(R.Get(v_76, (V)"label") != v_67))))) {
var values_81 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_82 = v_76;
V key_83 = (V)"age";
R.Set(obj_82, key_83, values_81[0]);

var values_84 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_85 = v_76;
V key_86 = (V)"press";
R.Set(obj_85, key_86, values_84[0]);
}
var values_87 = R.Join(new V[] {v_71, v_72, v_67}, new Results(R.Get(v_12, (V)"frame")));
var obj_88 = v_76;
V key_89 = (V)"selected";
var obj_90 = v_76;
V key_91 = (V)"disabled";
var obj_92 = v_76;
V key_93 = (V)"label";
var obj_94 = v_76;
V key_95 = (V)"frame";
R.Set(obj_88, key_89, values_87[0]);
R.Set(obj_90, key_91, values_87[1]);
R.Set(obj_92, key_93, values_87[2]);
R.Set(obj_94, key_95, values_87[3]);
if (R.Truth(R.Or((V)(!R.Truth(v_71)), () => v_72))) {
return R.Join(new V[] {(V)0.0d, (V)0.0d}, new Results((V)0.0d));
}
var values_96 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)((V)1.0d - (V)(R.Get(v_76, (V)"age") / (V)0.14d))))));
V v_97 = values_96[0];
return R.Join(new V[] {R.Or(R.Or(R.And((V)(R.Get(v_76, (V)"press") < (V)0.09d), () => (V)1.0d), () => R.And((V)(R.Get(v_76, (V)"age") < (V)0.07d), () => (-(V)1.0d))), () => (V)0.0d), R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)((V)4.0d * v_97) * v_97) + (V)0.5d)))).First}, new Results(v_97));
return Results.Empty;
})));
R.Set(v_2, (V)"offer_pose", new V((RuleFunction)(args_98 => {
V v_99 = args_98[0];
if (R.Truth((V)(!R.Truth(R.Get(v_12, (V)"enabled"))))) {
return R.Join(new V[] {(V)0.0d}, new Results((V)1.0d));
}
var values_100 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)((V)(R.Get(v_12, (V)"age") - (V)((V)(v_99 - (V)1.0d) * (V)0.025d)) / (V)0.16d)))))));
V v_101 = values_100[0];
return R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)6.0d * (V)Math.Pow(R.Num((V)((V)1.0d - v_101)), R.Num((V)2.0d))) + (V)0.5d)))).First}, new Results((V)((V)1.0d + (V)((V)0.12d * R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results((V)(R.Get(rt.Global("math"), (V)"pi") * v_101)))).First))));
return Results.Empty;
})));
R.Set(v_2, (V)"set_style", new V((RuleFunction)(args_102 => {
V v_103 = args_102[0];
V v_104 = args_102[1];
var values_105 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_103 == (V)"onebit"), () => R.Or(R.And(v_104, () => v_10), () => v_6)), () => R.Or(R.And(v_104, () => v_8), () => v_4))));
var obj_106 = v_2;
V key_107 = (V)"colors";
R.Set(obj_106, key_107, values_105[0]);
return Results.Empty;
})));
R.Set(v_2, (V)"contains", new V((RuleFunction)(args_108 => {
V v_109 = args_108[0];
V v_110 = args_108[1];
V v_111 = args_108[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_110 >= R.Get(v_109, (V)"x")), () => (V)(v_111 >= R.Get(v_109, (V)"y"))), () => (V)(v_110 < (V)(R.Get(v_109, (V)"x") + R.Get(v_109, (V)"w")))), () => (V)(v_111 < (V)(R.Get(v_109, (V)"y") + R.Get(v_109, (V)"h"))))));
return Results.Empty;
})));
R.Set(v_2, (V)"row", new V((RuleFunction)(args_112 => {
V v_113 = args_112[0];
V v_114 = args_112[1];
V v_115 = args_112[2];
V v_116 = args_112[3];
V v_117 = args_112[4];
V v_118 = args_112[5];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_113), new Entry((V)"y", (V)(v_114 + (V)((V)(v_117 - (V)1.0d) * v_118))), new Entry((V)"w", v_115), new Entry((V)"h", v_116))));
return Results.Empty;
})));
R.Set(v_2, (V)"row_at", new V((RuleFunction)(args_119 => {
V v_120 = args_119[0];
V v_121 = args_119[1];
V v_122 = args_119[2];
V v_123 = args_119[3];
V v_124 = args_119[4];
V v_125 = args_119[5];
V v_126 = args_119[6];
V v_127 = args_119[7];
{ double end_130 = R.Num(v_124), step_131 = R.Num((V)1.0d);
for (double i_129 = R.Num((V)1.0d); step_131 > 0 ? i_129 <= end_130 : i_129 >= end_130; i_129 += step_131) {
V v_128 = i_129;
if (R.Truth(R.Call(R.Get(v_2, (V)"contains"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"row"), R.Join(new V[] {v_120, v_121, v_122, v_123, v_128}, new Results(v_125))).First, v_126}, new Results(v_127))).First)) {
return R.Join(new V[] {}, new Results(v_128));
}
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"center", new V((RuleFunction)(args_132 => {
V v_133 = args_132[0];
V v_134 = args_132[1];
V v_135 = args_132[2];
V v_136 = args_132[3];
V v_137 = args_132[4];
V v_138 = args_132[5];
var values_139 = R.Join(new V[] {}, new Results(R.Get(rt.Global("love"), (V)"graphics")));
V v_140 = values_139[0];
if (R.Truth(v_135)) {
R.Call(R.Get(v_140, (V)"setFont"), R.Join(new V[] {}, new Results(v_135)));
}
R.Call(R.Get(v_140, (V)"setColor"), R.Join(new V[] {}, new Results(R.Or(v_136, () => R.Get(R.Get(v_2, (V)"colors"), (V)"text")))));
R.Call(R.Get(v_140, (V)"printf"), R.Join(new V[] {v_133, R.Or(v_137, () => (V)0.0d), v_134, R.Or(v_138, () => R.Call(R.Get(v_140, (V)"getWidth"), Results.Empty).First)}, new Results((V)"center")));
return Results.Empty;
})));
R.Set(v_2, (V)"dim", new V((RuleFunction)(args_141 => {
V v_142 = args_141[0];
var values_143 = R.Join(new V[] {}, new Results(R.Get(rt.Global("love"), (V)"graphics")));
V v_144 = values_143[0];
R.Call(R.Get(v_144, (V)"setColor"), R.Join(new V[] {(V)0.0d, (V)0.0d, (V)0.0d}, new Results(R.Or(v_142, () => (V)0.6d))));

R.Call(R.Get(v_144, (V)"rectangle"), R.Join(new V[] {(V)"fill", (V)0.0d, (V)0.0d}, R.Call(R.Get(v_144, (V)"getDimensions"), Results.Empty)));
return Results.Empty;
})));
R.Set(v_2, (V)"frame", new V((RuleFunction)(args_145 => {
V v_146 = args_145[0];
V v_147 = args_145[1];
V v_148 = args_145[2];
V v_149 = args_145[3];
V v_150 = args_145[4];
V v_151 = args_145[5];
var values_152 = R.Join(new V[] {}, new Results(R.Get(rt.Global("love"), (V)"graphics")));
V v_153 = values_152[0];
R.Call(R.Get(v_153, (V)"setColor"), R.Join(new V[] {(V)0.0d, (V)0.0d, (V)0.0d}, new Results((V)0.85d)));

R.Call(R.Get(v_153, (V)"rectangle"), R.Join(new V[] {(V)"fill", (V)(v_147 + (V)4.0d), (V)(v_148 + (V)4.0d), v_149}, new Results(v_150)));
R.Call(R.Get(v_153, (V)"setColor"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"colors"), (V)"edge"))));

R.Call(R.Get(v_153, (V)"rectangle"), R.Join(new V[] {(V)"fill", v_147, v_148, v_149}, new Results(v_150)));
R.Call(R.Get(v_153, (V)"setColor"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"colors"), (V)"light"))));

R.Call(R.Get(v_153, (V)"rectangle"), R.Join(new V[] {(V)"fill", (V)(v_147 + (V)2.0d), (V)(v_148 + (V)2.0d), (V)(v_149 - (V)4.0d)}, new Results((V)(v_150 - (V)4.0d))));
R.Call(R.Get(v_153, (V)"setColor"), R.Join(new V[] {(V)0.035d, (V)0.035d}, new Results((V)0.08d)));

R.Call(R.Get(v_153, (V)"rectangle"), R.Join(new V[] {(V)"fill", (V)(v_147 + (V)4.0d), (V)(v_148 + (V)4.0d), (V)(v_149 - (V)8.0d)}, new Results((V)(v_150 - (V)8.0d))));
R.Call(R.Get(v_153, (V)"setColor"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"colors"), (V)"panel"))));

R.Call(R.Get(v_153, (V)"rectangle"), R.Join(new V[] {(V)"fill", (V)(v_147 + (V)6.0d), (V)(v_148 + (V)6.0d), (V)(v_149 - (V)12.0d)}, new Results((V)(v_150 - (V)12.0d))));
if (R.Truth(R.And(v_146, () => (V)(v_146 != (V)"")))) {
if (R.Truth(v_151)) {
R.Call(R.Get(v_153, (V)"setFont"), R.Join(new V[] {}, new Results(v_151)));
}
var values_154 = R.Join(new V[] {}, new Results((V)(R.Invoke(R.Call(R.Get(v_153, (V)"getFont"), Results.Empty).First, "getWidth", R.Join(new V[] {}, new Results(v_146))).First + (V)24.0d)));
V v_155 = values_154[0];
R.Call(R.Get(v_153, (V)"setColor"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"colors"), (V)"panel"))));

R.Call(R.Get(v_153, (V)"rectangle"), R.Join(new V[] {(V)"fill", (V)(v_147 + (V)((V)(v_149 - v_155) / (V)2.0d)), (V)(v_148 - (V)6.0d), v_155}, new Results((V)24.0d)));
R.Call(R.Get(v_2, (V)"center"), R.Join(new V[] {v_146, (V)(v_148 - (V)6.0d), v_151, R.Get(R.Get(v_2, (V)"colors"), (V)"selected"), v_147}, new Results(v_149)));
}
return Results.Empty;
})));
R.Set(v_2, (V)"option", new V((RuleFunction)(args_156 => {
V v_157 = args_156[0];
V v_158 = args_156[1];
V v_159 = args_156[2];
V v_160 = args_156[3];
V v_161 = args_156[4];
V v_162 = args_156[5];
V v_163 = args_156[6];
var values_164 = R.Join(new V[] {}, new Results(R.Get(rt.Global("love"), (V)"graphics")));
V v_165 = values_164[0];
if (R.Truth(v_162)) {
R.Call(R.Get(v_165, (V)"setFont"), R.Join(new V[] {}, new Results(v_162)));
}
var values_166 = R.Join(new V[] {}, R.Call(R.Get(v_165, (V)"getFont"), Results.Empty));
V v_167 = values_166[0];
var values_168 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"option_feedback"), R.Join(new V[] {v_157, v_158, v_159, v_160, v_161}, new Results(v_163))));
V v_169 = values_168[0];
V v_170 = values_168[1];
V v_171 = values_168[2];
var values_172 = R.Join(new V[] {}, new Results((V)(v_159 + v_169)));
v_159 = values_172[0];
R.Call(R.Get(v_165, (V)"setColor"), R.Join(new V[] {}, new Results(R.Or(R.Or(R.And(v_163, () => R.Get(R.Get(v_2, (V)"colors"), (V)"muted")), () => R.And(v_161, () => R.Get(R.Get(v_2, (V)"colors"), (V)"selected"))), () => R.Get(R.Get(v_2, (V)"colors"), (V)"text")))));
R.Call(R.Get(v_165, (V)"printf"), R.Join(new V[] {v_157, v_158, v_159, v_160}, new Results((V)"center")));
if (R.Truth(v_161)) {
var values_173 = R.Join(new V[] {}, R.Invoke(v_167, "getWidth", R.Join(new V[] {}, new Results(v_157))));
V v_174 = values_173[0];
var values_175 = R.Join(new V[] {}, new Results((V)(v_158 + (V)((V)(v_160 - v_174) / (V)2.0d))));
V v_176 = values_175[0];
var values_177 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)4.0d}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Invoke(v_167, "getHeight", Results.Empty).First / (V)4.0d)))))).First + v_170)));
V v_178 = values_177[0];
R.Call(R.Get(v_165, (V)"print"), R.Join(new V[] {(V)">", (V)((V)(v_176 - v_178) - R.Invoke(v_167, "getWidth", R.Join(new V[] {}, new Results((V)">"))).First)}, new Results(v_159)));
R.Call(R.Get(v_165, (V)"print"), R.Join(new V[] {(V)"<", (V)((V)(v_176 + v_174) + v_178)}, new Results(v_159)));
if (R.Truth((V)(v_171 > (V)0.0d))) {
R.Call(R.Get(v_165, (V)"setColor"), R.Join(new V[] {R.Get(R.Get(R.Get(v_2, (V)"colors"), (V)"selected"), (V)1.0d), R.Get(R.Get(R.Get(v_2, (V)"colors"), (V)"selected"), (V)2.0d), R.Get(R.Get(R.Get(v_2, (V)"colors"), (V)"selected"), (V)3.0d)}, new Results((V)(v_171 * (V)0.7d))));
R.Call(R.Get(v_165, (V)"rectangle"), R.Join(new V[] {(V)"fill", v_176, (V)((V)(v_159 + R.Invoke(v_167, "getHeight", Results.Empty).First) + (V)1.0d), v_174}, new Results((V)1.0d)));
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"settings_row", new V((RuleFunction)(args_179 => {
V v_180 = args_179[0];
V v_181 = args_179[1];
V v_182 = args_179[2];
V v_183 = args_179[3];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"row"), R.Join(new V[] {(V)(v_180 + (V)36.0d), (V)(v_181 + (V)166.0d), (V)(v_182 - (V)72.0d), (V)40.0d, v_183}, new Results((V)48.0d))));
return Results.Empty;
})));
R.Set(v_2, (V)"settings_at", new V((RuleFunction)(args_184 => {
V v_185 = args_184[0];
V v_186 = args_184[1];
V v_187 = args_184[2];
V v_188 = args_184[3];
V v_189 = args_184[4];
V v_190 = args_184[5];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"row_at"), R.Join(new V[] {(V)(v_185 + (V)36.0d), (V)(v_186 + (V)166.0d), (V)(v_187 - (V)72.0d), (V)40.0d, v_188, (V)48.0d, v_189}, new Results(v_190))));
return Results.Empty;
})));
R.Set(v_2, (V)"home_at", new V((RuleFunction)(args_191 => {
V v_192 = args_191[0];
V v_193 = args_191[1];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"row_at"), R.Join(new V[] {(V)280.0d, (V)232.0d, (V)464.0d, (V)34.0d, (V)5.0d, (V)42.0d, v_192}, new Results(v_193))));
return Results.Empty;
})));
R.Set(v_2, (V)"class_page", new V((RuleFunction)(args_194 => {
V v_195 = args_194[0];
V v_196 = args_194[1];
var values_197 = R.Join(new V[] {}, new Results((V)1.0d));
V v_198 = values_197[0];
foreach (var row_199 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_195)))))) {
V v_200 = row_199[0];
V v_201 = row_199[1];
if (R.Truth((V)(v_201 == v_196))) {
var values_202 = R.Join(new V[] {}, new Results(v_200));
v_198 = values_202[0];

break;
}
}
var values_203 = R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_198 - (V)1.0d) / (V)9.0d)))).First * (V)9.0d) + (V)1.0d)));
V v_204 = values_203[0];
return R.Join(new V[] {v_204}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)R.Length(v_195)}, new Results((V)(v_204 + (V)8.0d)))));
return Results.Empty;
})));
R.Set(v_2, (V)"class_nav_at", new V((RuleFunction)(args_205 => {
V v_206 = args_205[0];
V v_207 = args_205[1];
if (R.Truth(R.Or((V)(v_207 < (V)500.0d), () => (V)(v_207 >= (V)538.0d)))) {
return Results.Empty;
}
if (R.Truth(R.And((V)(v_206 >= (V)190.0d), () => (V)(v_206 < (V)310.0d)))) {
return R.Join(new V[] {}, new Results((-(V)1.0d)));
}
if (R.Truth(R.And((V)(v_206 >= (V)350.0d), () => (V)(v_206 < (V)480.0d)))) {
return R.Join(new V[] {}, new Results((V)1.0d));
}
return Results.Empty;
})));
R.Set(v_2, (V)"class_page_target", new V((RuleFunction)(args_208 => {
V v_209 = args_208[0];
V v_210 = args_208[1];
V v_211 = args_208[2];
var values_212 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"class_page"), R.Join(new V[] {v_209}, new Results(v_210))));
V v_213 = values_212[0];
var values_214 = R.Join(new V[] {}, new Results((V)(v_213 + (V)(v_211 * (V)9.0d))));
V v_215 = values_214[0];
if (R.Truth(R.Or((V)(v_215 < (V)1.0d), () => (V)(v_215 > (V)R.Length(v_209))))) {
return R.Join(new V[] {}, new Results(v_210));
}
return R.Join(new V[] {}, new Results(R.Get(v_209, v_215)));
return Results.Empty;
})));
R.Set(v_2, (V)"class_at", new V((RuleFunction)(args_216 => {
V v_217 = args_216[0];
V v_218 = args_216[1];
V v_219 = args_216[2];
V v_220 = args_216[3];
var values_221 = R.Join(new V[] {R.Or(v_219, () => (V)1.0d)}, new Results(R.Or(v_220, () => (V)9.0d)));
v_219 = values_221[0];
v_220 = values_221[1];
var values_222 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"row_at"), R.Join(new V[] {(V)190.0d, (V)150.0d, (V)290.0d, (V)32.0d, (V)((V)(v_220 - v_219) + (V)1.0d), (V)36.0d, v_217}, new Results(v_218))));
V v_223 = values_222[0];
return R.Join(new V[] {}, new Results(R.And(v_223, () => (V)((V)(v_223 + v_219) - (V)1.0d))));
return Results.Empty;
})));
R.Set(v_2, (V)"pause_at", new V((RuleFunction)(args_224 => {
V v_225 = args_224[0];
V v_226 = args_224[1];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"row_at"), R.Join(new V[] {(V)306.0d, (V)252.0d, (V)412.0d, (V)38.0d, (V)3.0d, (V)44.0d, v_225}, new Results(v_226))));
return Results.Empty;
})));
R.Set(v_2, (V)"chest_at", new V((RuleFunction)(args_227 => {
V v_228 = args_227[0];
V v_229 = args_227[1];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"row_at"), R.Join(new V[] {(V)102.0d, (V)178.0d, (V)420.0d, (V)62.0d, (V)3.0d, (V)76.0d, v_228}, new Results(v_229))));
return Results.Empty;
})));
R.Set(v_2, (V)"route_at", new V((RuleFunction)(args_230 => {
V v_231 = args_230[0];
V v_232 = args_230[1];
V v_233 = args_230[2];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"row_at"), R.Join(new V[] {(V)180.0d, (V)216.0d, (V)664.0d, (V)54.0d, v_231, (V)64.0d, v_232}, new Results(v_233))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
