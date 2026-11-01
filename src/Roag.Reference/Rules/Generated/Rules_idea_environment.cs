// Static C# port of src/idea_environment.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 5d56090f16a78e7cf6682f77b497c6ccc022fbec148bf05691c3af96628698a1
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_environment { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_environment_content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_terrain"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_monochrome"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"content", v_2))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)4.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)8.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)16.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)32.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)64.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)128.0d))))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)8.0d), new Entry((V)3, (V)128.0d), new Entry((V)4, (V)0.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)1.0d), new Entry((V)7, (V)2.0d), new Entry((V)8, (V)4.0d), new Entry((V)9, (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)16.0d), new Entry((V)4, (V)8.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)3.0d), new Entry((V)7, (V)2.0d), new Entry((V)8, (V)6.0d), new Entry((V)9, (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)32.0d), new Entry((V)4, (V)8.0d), new Entry((V)5, (V)8.0d), new Entry((V)6, (V)9.0d), new Entry((V)7, (V)8.0d), new Entry((V)8, (V)6.0d), new Entry((V)9, (V)3.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)8.0d), new Entry((V)3, (V)64.0d), new Entry((V)4, (V)0.0d), new Entry((V)5, (V)8.0d), new Entry((V)6, (V)7.0d), new Entry((V)7, (V)8.0d), new Entry((V)8, (V)4.0d), new Entry((V)9, (V)4.0d))))));
V v_14 = values_13[0];
V v_15 = V.Nil;
v_15 = new V((RuleFunction)(args_16 => {
V v_17 = args_16[0];
V v_18 = args_16[1];
return R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_17 / v_18)))).First % (V)2.0d) == (V)1.0d)));
return Results.Empty;
}));
V v_19 = V.Nil;
v_19 = new V((RuleFunction)(args_20 => {
V v_21 = args_20[0];
var values_22 = R.Join(new V[] {}, new Results(R.Table()));
V v_23 = values_22[0];

foreach (var row_24 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_21)))))) {
V v_25 = row_24[0];
var values_26 = R.Join(new V[] {}, new Results(v_25));
var obj_27 = v_23;
V key_28 = (V)((V)R.Length(v_23) + (V)1.0d);
R.Set(obj_27, key_28, values_26[0]);
}

R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_23)));

return R.Join(new V[] {}, new Results(v_23));
return Results.Empty;
}));
V v_29 = V.Nil;
v_29 = new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
V v_32 = args_30[1];
V v_33 = args_30[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(v_32 >= R.Get(v_31, (V)"x")), () => (V)(v_32 < (V)(R.Get(v_31, (V)"x") + R.Get(v_31, (V)"width")))), () => (V)(v_33 >= R.Get(v_31, (V)"y"))), () => (V)(v_33 < (V)(R.Get(v_31, (V)"y") + R.Get(v_31, (V)"height"))))));
return Results.Empty;
}));
V v_34 = V.Nil;
v_34 = new V((RuleFunction)(args_35 => {
V v_36 = args_35[0];
V v_37 = args_35[1];
return R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"derive"), R.Join(new V[] {R.Or(R.Get(v_36, (V)"seed"), () => (V)1.0d)}, new Results(R.Concat(R.Or(R.Get(v_36, (V)"floor_id"), () => (V)""), R.Concat((V)":", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_37))).First))))));
return Results.Empty;
}));
R.Set(v_10, (V)"material", new V((RuleFunction)(args_38 => {
V v_39 = args_38[0];
V v_40 = args_38[1];
return R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_2, (V)"materials"), v_39), R.Or(R.And((V)(v_40 == (V)"onebit"), () => (V)"mono"), () => (V)"mixed"))));
return Results.Empty;
})));
R.Set(v_10, (V)"surfaces", new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
V v_43 = args_41[1];
var values_44 = R.Join(new V[] {}, new Results(R.Table()));
V v_45 = values_44[0];
foreach (var row_46 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"material_order"))))))) {
V v_47 = row_46[0];
V v_48 = row_46[1];
var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"material"), R.Join(new V[] {v_48}, new Results(v_43))));
V v_50 = values_49[0];

var values_51 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"materials"), v_48)));
V v_52 = values_51[0];
if (R.Truth((V)(R.Get(v_50, (V)"sheet") == v_42))) {
foreach (var row_53 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Or(R.Or(R.Get(v_50, (V)"grid"), () => R.Get(v_50, (V)"frame")), () => R.Get(v_50, (V)"border")), () => R.Get(v_4, (V)"masks")), () => R.Table(new Entry((V)1, (V)255.0d))))))))) {
V v_54 = row_53[0];
V v_55 = row_53[1];
var values_56 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"name", v_48), new Entry((V)"kind", R.Get(v_52, (V)"kind")), new Entry((V)"mask", v_55), new Entry((V)"material", v_50), new Entry((V)"slot", (V)R.Length(v_45)))));
var obj_57 = v_45;
V key_58 = (V)((V)R.Length(v_45) + (V)1.0d);
R.Set(obj_57, key_58, values_56[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_45));
return Results.Empty;
})));
R.Set(v_10, (V)"surface_sheet", new V((RuleFunction)(args_59 => {
V v_60 = args_59[0];
V v_61 = args_59[1];
V v_62 = args_59[2];
var values_63 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"sources"), v_61)))));
V v_64 = values_63[0];

var values_65 = R.Join(new V[] {}, R.Invoke(v_60, "clone", Results.Empty));
V v_66 = values_65[0];

var values_67 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_64, (V)"width") / (V)16.0d)))));
V v_68 = values_67[0];
V v_69 = V.Nil;
v_69 = new V((RuleFunction)(args_70 => {
V v_71 = args_70[0];
V v_72 = args_70[1];
V v_73 = args_70[2];
return R.Join(new V[] {}, R.Invoke(v_60, "getPixel", R.Join(new V[] {(V)((V)((V)(v_71 % R.Get(v_64, (V)"columns")) * R.Get(v_64, (V)"stride")) + v_72)}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_71 / R.Get(v_64, (V)"columns"))))).First * R.Get(v_64, (V)"stride")) + v_73)))));
return Results.Empty;
}));
foreach (var row_74 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"surfaces"), R.Join(new V[] {v_61}, new Results(v_62)))))))) {
V v_75 = row_74[0];
V v_76 = row_74[1];
var values_77 = R.Join(new V[] {R.Get(v_76, (V)"material")}, new Results(R.Get(v_76, (V)"mask")));
V v_78 = values_77[0];
V v_79 = values_77[1];

var values_80 = R.Join(new V[] {(V)((V)(R.Get(v_76, (V)"slot") % v_68) * (V)16.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_76, (V)"slot") / v_68)))).First * (V)16.0d)));
V v_81 = values_80[0];
V v_82 = values_80[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)(v_82 + (V)16.0d) <= R.Get(v_64, (V)"height"))}, new Results((V)"environment surface page overflow")));
{ double end_85 = R.Num((V)15.0d), step_86 = R.Num((V)1.0d);
for (double i_84 = R.Num((V)0.0d); step_86 > 0 ? i_84 <= end_85 : i_84 >= end_85; i_84 += step_86) {
V v_83 = i_84;
{ double end_89 = R.Num((V)15.0d), step_90 = R.Num((V)1.0d);
for (double i_88 = R.Num((V)0.0d); step_90 > 0 ? i_88 <= end_89 : i_88 >= end_89; i_88 += step_90) {
V v_87 = i_88;
var values_91 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_78, (V)"center"), () => R.Get(R.Get(v_78, (V)"grid"), (V)5.0d))));
V v_92 = values_91[0];

var values_93 = R.Join(new V[] {v_87}, new Results(v_83));
V v_94 = values_93[0];
V v_95 = values_93[1];
if (R.Truth(R.Get(v_78, (V)"grid"))) {
foreach (var row_96 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_14)))))) {
V v_97 = row_96[0];
V v_98 = row_96[1];
if (R.Truth(R.And(R.And(R.And((V)(v_87 >= R.Get(v_98, (V)4.0d)), () => (V)(v_87 < (V)(R.Get(v_98, (V)4.0d) + (V)8.0d))), () => (V)(v_83 >= R.Get(v_98, (V)5.0d))), () => (V)(v_83 < (V)(R.Get(v_98, (V)5.0d) + (V)8.0d))))) {
var values_99 = R.Join(new V[] {R.Call(v_15, R.Join(new V[] {v_79}, new Results(R.Get(v_98, (V)1.0d)))).First, R.Call(v_15, R.Join(new V[] {v_79}, new Results(R.Get(v_98, (V)2.0d)))).First}, R.Call(v_15, R.Join(new V[] {v_79}, new Results(R.Get(v_98, (V)3.0d)))));
V v_100 = values_99[0];
V v_101 = values_99[1];
V v_102 = values_99[2];

var values_103 = Results.Empty;
V v_104 = values_103[0];
if (R.Truth(R.And((V)(!R.Truth(v_100)), () => (V)(!R.Truth(v_101))))) {
var values_105 = R.Join(new V[] {}, new Results(R.Get(v_98, (V)6.0d)));
v_104 = values_105[0];
} else if (R.Truth((V)(!R.Truth(v_100)))) {
var values_106 = R.Join(new V[] {}, new Results(R.Get(v_98, (V)7.0d)));
v_104 = values_106[0];
} else if (R.Truth((V)(!R.Truth(v_101)))) {
var values_107 = R.Join(new V[] {}, new Results(R.Get(v_98, (V)8.0d)));
v_104 = values_107[0];
} else if (R.Truth(R.And((V)(!R.Truth(v_102)), () => R.Get(v_78, (V)"inner")))) {
var values_108 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_78, (V)"inner"), R.Get(v_98, (V)9.0d))));
v_92 = values_108[0];
} else {
var values_109 = R.Join(new V[] {}, new Results((V)5.0d));
v_104 = values_109[0];
}
if (R.Truth(v_104)) {
var values_110 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_78, (V)"grid"), v_104)));
v_92 = values_110[0];
}
}
}
} else if (R.Truth(R.Get(v_78, (V)"frame"))) {
if (R.Truth(R.And((V)(!R.Truth(R.Call(v_15, R.Join(new V[] {v_79}, new Results((V)1.0d))).First)), () => (V)(v_83 < (V)4.0d)))) {
var values_111 = R.Join(new V[] {}, new Results(R.Get(v_78, (V)"frame")));
v_92 = values_111[0];

var values_112 = R.Join(new V[] {}, new Results((V)8.0d));
v_94 = values_112[0];
}
if (R.Truth(R.And((V)(!R.Truth(R.Call(v_15, R.Join(new V[] {v_79}, new Results((V)4.0d))).First)), () => (V)(v_83 >= (V)12.0d)))) {
var values_113 = R.Join(new V[] {}, new Results(R.Get(v_78, (V)"frame")));
v_92 = values_113[0];

var values_114 = R.Join(new V[] {}, new Results((V)8.0d));
v_94 = values_114[0];
}
if (R.Truth(R.And((V)(!R.Truth(R.Call(v_15, R.Join(new V[] {v_79}, new Results((V)8.0d))).First)), () => (V)(v_87 < (V)4.0d)))) {
var values_115 = R.Join(new V[] {}, new Results(R.Get(v_78, (V)"frame")));
v_92 = values_115[0];

var values_116 = R.Join(new V[] {}, new Results((V)8.0d));
v_95 = values_116[0];
}
if (R.Truth(R.And((V)(!R.Truth(R.Call(v_15, R.Join(new V[] {v_79}, new Results((V)2.0d))).First)), () => (V)(v_87 >= (V)12.0d)))) {
var values_117 = R.Join(new V[] {}, new Results(R.Get(v_78, (V)"frame")));
v_92 = values_117[0];

var values_118 = R.Join(new V[] {}, new Results((V)8.0d));
v_95 = values_118[0];
}
}
var values_119 = R.Join(new V[] {}, R.Call(v_69, R.Join(new V[] {v_92, v_94}, new Results(v_95))));
V v_120 = values_119[0];
V v_121 = values_119[1];
V v_122 = values_119[2];
V v_123 = values_119[3];
if (R.Truth((V)(R.Get(v_76, (V)"kind") == (V)"wall"))) {
foreach (var row_124 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_14)))))) {
V v_125 = row_124[0];
V v_126 = row_124[1];
var values_127 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_126, (V)4.0d) == (V)0.0d), () => v_87), () => (V)((V)15.0d - v_87))));
V v_128 = values_127[0];

var values_129 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_126, (V)5.0d) == (V)0.0d), () => v_83), () => (V)((V)15.0d - v_83))));
V v_130 = values_129[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.Call(v_15, R.Join(new V[] {v_79}, new Results(R.Get(v_126, (V)1.0d)))).First, () => R.Call(v_15, R.Join(new V[] {v_79}, new Results(R.Get(v_126, (V)2.0d)))).First), () => (V)(!R.Truth(R.Call(v_15, R.Join(new V[] {v_79}, new Results(R.Get(v_126, (V)3.0d)))).First))), () => (V)(v_128 < (V)3.0d)), () => (V)(v_130 < (V)3.0d)))) {
var values_131 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_78, (V)"frame"), () => R.Get(R.Get(v_78, (V)"grid"), R.Get(v_126, (V)6.0d)))));
V v_132 = values_131[0];

var values_133 = R.Join(new V[] {}, R.Call(v_69, R.Join(new V[] {v_132, R.Or(R.And((V)(R.Get(v_126, (V)4.0d) == (V)0.0d), () => v_128), () => (V)((V)15.0d - v_128))}, new Results(R.Or(R.And((V)(R.Get(v_126, (V)5.0d) == (V)0.0d), () => v_130), () => (V)((V)15.0d - v_130))))));
v_120 = values_133[0];
v_121 = values_133[1];
v_122 = values_133[2];
v_123 = values_133[3];
}
}
}
if (R.Truth(R.And(R.And(R.And(R.And(R.Get(v_78, (V)"accent"), () => (V)(v_87 >= (V)4.0d)), () => (V)(v_87 < (V)12.0d)), () => (V)(v_83 >= (V)4.0d)), () => (V)(v_83 < (V)12.0d)))) {
var values_134 = R.Join(new V[] {}, R.Call(v_69, R.Join(new V[] {R.Get(v_78, (V)"accent"), v_87}, new Results(v_83))));
V v_135 = values_134[0];
V v_136 = values_134[1];
V v_137 = values_134[2];
V v_138 = values_134[3];
var values_139 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_78, (V)"sheet") == (V)"onebit"), () => (V)0.35d), () => (V)0.4d)));
V v_140 = values_139[0];
var values_141 = R.Join(new V[] {(V)((V)(v_120 * (V)((V)1.0d - (V)(v_138 * v_140))) + (V)((V)(v_135 * v_138) * v_140)), (V)((V)(v_121 * (V)((V)1.0d - (V)(v_138 * v_140))) + (V)((V)(v_136 * v_138) * v_140))}, new Results((V)((V)(v_122 * (V)((V)1.0d - (V)(v_138 * v_140))) + (V)((V)(v_137 * v_138) * v_140))));
v_120 = values_141[0];
v_121 = values_141[1];
v_122 = values_141[2];
}
if (R.Truth((V)(v_62 == (V)"onebit"))) {
if (R.Truth(R.Get(v_78, (V)"water_conversion"))) {
var values_142 = R.Join(new V[] {}, new Results(R.And((V)(v_122 > (V)(v_120 + (V)0.06d)), () => (V)(v_122 > (V)(v_121 * (V)0.9d)))));
V v_143 = values_142[0];

var values_144 = R.Join(new V[] {}, new Results(R.Or(R.And(v_143, () => R.Or(R.And((V)((V)((V)((V)(v_120 * (V)0.2126d) + (V)(v_121 * (V)0.7152d)) + (V)(v_122 * (V)0.0722d)) > (V)0.76d), () => (V)0.8d), () => (V)0.055d)), () => (V)0.75d)));
V v_145 = values_144[0];
var values_146 = R.Join(new V[] {v_145, v_145}, new Results(v_145));
v_120 = values_146[0];
v_121 = values_146[1];
v_122 = values_146[2];
} else {
var values_147 = R.Join(new V[] {}, new Results((V)((V)((V)((V)(v_120 * (V)0.2126d) + (V)(v_121 * (V)0.7152d)) + (V)(v_122 * (V)0.0722d)) * v_123)));
V v_148 = values_147[0];
var values_149 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_76, (V)"kind") == (V)"floor"), () => (V)0.22d), () => (V)0.48d)));
V v_150 = values_149[0];
var values_151 = R.Join(new V[] {(V)((V)0.045d + (V)(v_148 * v_150)), (V)((V)0.045d + (V)(v_148 * v_150))}, new Results((V)((V)0.045d + (V)(v_148 * v_150))));
v_120 = values_151[0];
v_121 = values_151[1];
v_122 = values_151[2];
}
} else if (R.Truth((V)(R.Get(v_76, (V)"kind") == (V)"floor"))) {
var values_152 = R.Join(new V[] {(V)(v_120 * (V)0.72d), (V)(v_121 * (V)0.72d)}, new Results((V)(v_122 * (V)0.72d)));
v_120 = values_152[0];
v_121 = values_152[1];
v_122 = values_152[2];
}
if (R.Truth(R.And(R.Get(v_78, (V)"border"), () => R.Or(R.Or(R.Or(R.And((V)(!R.Truth(R.Call(v_15, R.Join(new V[] {v_79}, new Results((V)1.0d))).First)), () => (V)(v_83 < (V)1.0d)), () => R.And((V)(!R.Truth(R.Call(v_15, R.Join(new V[] {v_79}, new Results((V)4.0d))).First)), () => (V)(v_83 >= (V)15.0d))), () => R.And((V)(!R.Truth(R.Call(v_15, R.Join(new V[] {v_79}, new Results((V)8.0d))).First)), () => (V)(v_87 < (V)1.0d))), () => R.And((V)(!R.Truth(R.Call(v_15, R.Join(new V[] {v_79}, new Results((V)2.0d))).First)), () => (V)(v_87 >= (V)15.0d)))))) {
var values_153 = R.Join(new V[] {(V)0.55d, (V)0.55d}, new Results((V)0.55d));
v_120 = values_153[0];
v_121 = values_153[1];
v_122 = values_153[2];
}
R.Invoke(v_66, "setPixel", R.Join(new V[] {(V)(v_81 + v_87), (V)(v_82 + v_83), v_120, v_121, v_122}, new Results((V)1.0d)));
}
}
}
}
}
return R.Join(new V[] {}, new Results(v_66));
return Results.Empty;
})));
var values_154 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"overgrown_ruins", (V)"ruin_masonry"), new Entry((V)"fungal_hollows", (V)"broken_cavern"), new Entry((V)"sunken_aqueduct", (V)"drowned_sanctum"), new Entry((V)"ashen_bastion", (V)"forge_masonry"), new Entry((V)"glacial_observatory", (V)"observatory"), new Entry((V)"fractured_archive", (V)"archive_library"), new Entry((V)"sundered_forge", (V)"forge_masonry"))));
V v_155 = values_154[0];
R.Set(v_10, (V)"family", new V((RuleFunction)(args_156 => {
V v_157 = args_156[0];
V v_158 = args_156[1];
var values_159 = R.Join(new V[] {}, new Results(R.Or(R.Invoke(R.Or(R.Get(v_157, (V)"biome_id"), () => (V)""), "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))).First, () => (V)"overgrown_ruins")));
V v_160 = values_159[0];

var values_161 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_157}, new Results(R.Get(v_158, (V)"id")))));
V v_162 = values_161[0];
if (R.Truth(R.And(R.And((V)(R.Get(v_158, (V)"role") != (V)"start"), () => (V)(R.Get(v_158, (V)"role") != (V)"elite")), () => (V)(R.Get(v_158, (V)"role") != (V)"exit")))) {
if (R.Truth(R.And(R.Or(R.Or((V)(v_160 == (V)"fungal_hollows"), () => (V)(v_160 == (V)"glacial_observatory")), () => (V)(v_160 == (V)"fractured_archive")), () => (V)((V)(v_162 % (V)4.0d) == (V)0.0d)))) {
return R.Join(new V[] {}, new Results((V)"crypt_sanctuary"));
}
if (R.Truth((V)(v_160 == (V)"overgrown_ruins"))) {
return R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)((V)(v_162 % (V)3.0d) == (V)0.0d), () => (V)"root_garden"), () => R.And((V)((V)(v_162 % (V)2.0d) == (V)0.0d), () => (V)"ruin_carved")), () => (V)"ruin_masonry")));
}
}
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_155, v_160), () => (V)"ruin_masonry")));
return Results.Empty;
})));
R.Set(v_10, (V)"plan", new V((RuleFunction)(args_163 => {
V v_164 = args_163[0];
V v_165 = args_163[1];
var values_166 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rooms", R.Table()), new Entry((V)"props", R.Table()))));
V v_167 = values_166[0];

var values_168 = R.Join(new V[] {}, new Results(R.Table()));
V v_169 = values_168[0];
foreach (var row_170 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_165, (V)"placements"), () => R.Get(R.Get(v_165, (V)"placements"), (V)"secrets")), () => R.Table()))))))) {
V v_171 = row_170[0];
V v_172 = row_170[1];
foreach (var row_173 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, R.Get(v_172, (V)"x")), new Entry((V)2, R.Get(v_172, (V)"y")))), new Entry((V)2, R.Table(new Entry((V)1, R.Get(v_172, (V)"wall_x")), new Entry((V)2, R.Get(v_172, (V)"wall_y"))))))))))) {
V v_174 = row_173[0];
V v_175 = row_173[1];
if (R.Truth(R.And(R.Get(v_175, (V)1.0d), () => R.Get(v_175, (V)2.0d)))) {
var values_176 = R.Join(new V[] {}, new Results((V)true));
var obj_177 = v_169;
V key_178 = R.Concat(R.Get(v_175, (V)1.0d), R.Concat((V)",", R.Get(v_175, (V)2.0d)));
R.Set(obj_177, key_178, values_176[0]);
}
}
}
foreach (var row_179 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_165, (V)"rooms"), () => R.Table()))))))) {
V v_180 = row_179[0];
V v_181 = row_179[1];
var values_182 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"family"), R.Join(new V[] {v_165}, new Results(v_181))));
V v_183 = values_182[0];

var values_184 = Results.Empty;
V v_185 = values_184[0];
foreach (var row_186 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.And(v_164, () => R.Get(v_164, (V)"idea_rooms")), () => R.Get(R.Get(v_164, (V)"idea_rooms"), (V)"nodes")), () => R.Table()))))))) {
V v_187 = row_186[0];
V v_188 = row_186[1];
if (R.Truth(R.And((V)(R.Get(v_188, (V)"floor_id") == R.Get(v_165, (V)"floor_id")), () => R.Call(v_29, R.Join(new V[] {v_181, R.Get(v_188, (V)"x")}, new Results(R.Get(v_188, (V)"y")))).First))) {
if (R.Truth(R.Or((V)(R.Get(v_188, (V)"kind") == (V)"memorial"), () => (V)(R.Get(v_188, (V)"kind") == (V)"offering_font")))) {
var values_189 = R.Join(new V[] {}, new Results((V)"crypt_sanctuary"));
v_183 = values_189[0];
} else if (R.Truth((V)(R.Get(v_188, (V)"kind") == (V)"attunement_forge"))) {
var values_190 = R.Join(new V[] {}, new Results((V)"forge_masonry"));
v_183 = values_190[0];

var values_191 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"workshop_supplies"), new Entry((V)2, (V)"workshop_hearth"))));
v_185 = values_191[0];
} else if (R.Truth((V)(R.Get(v_188, (V)"kind") == (V)"camp"))) {
var values_192 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"camp_table"), new Entry((V)2, (V)"camp_seat"))));
v_185 = values_192[0];
} else if (R.Truth((V)(R.Get(v_188, (V)"kind") == (V)"travelling_trader"))) {
var values_193 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"workshop_supplies"), new Entry((V)2, (V)"camp_seat"))));
v_185 = values_193[0];
}
}
}
var values_194 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"families"), v_183)));
V v_195 = values_194[0];

var values_196 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"room", v_181), new Entry((V)"family", v_183), new Entry((V)"floor", R.Get(v_195, (V)"floor")), new Entry((V)"wall", R.Get(v_195, (V)"wall")), new Entry((V)"water", R.Get(v_195, (V)"water")))));
V v_197 = values_196[0];
if (R.Truth(R.And(R.And(R.Get(v_195, (V)"patch"), () => (V)(R.Get(v_181, (V)"width") >= (V)7.0d)), () => (V)(R.Get(v_181, (V)"height") >= (V)7.0d)))) {
var values_198 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)((V)(R.Get(v_181, (V)"x") + R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_181, (V)"width") / (V)2.0d)))).First) - (V)1.0d)), new Entry((V)"y", (V)((V)(R.Get(v_181, (V)"y") + R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_181, (V)"height") / (V)2.0d)))).First) - (V)1.0d)), new Entry((V)"width", (V)3.0d), new Entry((V)"height", (V)3.0d), new Entry((V)"material", R.Get(v_195, (V)"patch")))));
var obj_199 = v_197;
V key_200 = (V)"patch";
R.Set(obj_199, key_200, values_198[0]);
}
var values_201 = R.Join(new V[] {}, new Results(v_197));
var obj_202 = R.Get(v_167, (V)"rooms");
V key_203 = (V)((V)R.Length(R.Get(v_167, (V)"rooms")) + (V)1.0d);
R.Set(obj_202, key_203, values_201[0]);
var values_204 = R.Join(new V[] {}, new Results(R.Or(v_185, () => R.Get(v_195, (V)"decor"))));
v_185 = values_204[0];
if (R.Truth(R.And((V)(v_183 == (V)"forge_masonry"), () => (V)(!R.Truth(v_185))))) {
var values_205 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"forge_panel"), new Entry((V)2, (V)"prison_bars"))));
v_185 = values_205[0];
}
var values_206 = R.Join(new V[] {}, new Results(R.Table()));
V v_207 = values_206[0];
{ double end_210 = R.Num((V)((V)(R.Get(v_181, (V)"y") + R.Get(v_181, (V)"height")) - (V)1.0d)), step_211 = R.Num((V)1.0d);
for (double i_209 = R.Num(R.Get(v_181, (V)"y")); step_211 > 0 ? i_209 <= end_210 : i_209 >= end_210; i_209 += step_211) {
V v_208 = i_209;
{ double end_214 = R.Num((V)((V)(R.Get(v_181, (V)"x") + R.Get(v_181, (V)"width")) - (V)1.0d)), step_215 = R.Num((V)1.0d);
for (double i_213 = R.Num(R.Get(v_181, (V)"x")); step_215 > 0 ? i_213 <= end_214 : i_213 >= end_214; i_213 += step_215) {
V v_212 = i_213;
if (R.Truth(R.And(R.And((V)(R.Invoke(R.Get(R.Get(v_165, (V)"rows"), v_208), "sub", R.Join(new V[] {v_212}, new Results(v_212))).First == (V)"#"), () => (V)(!R.Truth(R.Get(v_169, R.Concat(v_212, R.Concat((V)",", v_208)))))), () => (V)(!R.Truth(R.And(R.Get(v_165, (V)"studio_visuals"), () => R.Get(R.Get(v_165, (V)"studio_visuals"), R.Concat(v_212, R.Concat((V)",", v_208))))))))) {
var values_216 = R.Join(new V[] {}, new Results((V)false));
V v_217 = values_216[0];

foreach (var row_218 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_219 = row_218[0];
V v_220 = row_218[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_220, (V)3.0d) <= (V)8.0d), () => R.Call(v_29, R.Join(new V[] {v_181, (V)(v_212 + R.Get(v_220, (V)1.0d))}, new Results((V)(v_208 + R.Get(v_220, (V)2.0d))))).First), () => (V)(R.Invoke(R.Get(R.Get(v_165, (V)"rows"), (V)(v_208 + R.Get(v_220, (V)2.0d))), "sub", R.Join(new V[] {(V)(v_212 + R.Get(v_220, (V)1.0d))}, new Results((V)(v_212 + R.Get(v_220, (V)1.0d))))).First == (V)".")))) {
var values_221 = R.Join(new V[] {}, new Results((V)true));
v_217 = values_221[0];
}
}
if (R.Truth(v_217)) {
var values_222 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_212), new Entry((V)"y", v_208), new Entry((V)"score", R.Call(v_34, R.Join(new V[] {v_165}, new Results(R.Concat((V)"decor:", R.Concat(v_212, R.Concat((V)",", v_208)))))).First))));
var obj_223 = v_207;
V key_224 = (V)((V)R.Length(v_207) + (V)1.0d);
R.Set(obj_223, key_224, values_222[0]);
}
}
}
}
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_207}, new Results(new V((RuleFunction)(args_225 => {
V v_226 = args_225[0];
V v_227 = args_225[1];
if (R.Truth((V)(R.Get(v_226, (V)"score") != R.Get(v_227, (V)"score")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_226, (V)"score") < R.Get(v_227, (V)"score"))));
}

if (R.Truth((V)(R.Get(v_226, (V)"y") != R.Get(v_227, (V)"y")))) {
return R.Join(new V[] {}, new Results((V)(R.Get(v_226, (V)"y") < R.Get(v_227, (V)"y"))));
}

return R.Join(new V[] {}, new Results((V)(R.Get(v_226, (V)"x") < R.Get(v_227, (V)"x"))));
return Results.Empty;
})))));
{ double end_230 = R.Num(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)2.0d}, new Results((V)R.Length(v_207)))).First), step_231 = R.Num((V)1.0d);
for (double i_229 = R.Num((V)1.0d); step_231 > 0 ? i_229 <= end_230 : i_229 >= end_230; i_229 += step_231) {
V v_228 = i_229;
var values_232 = R.Join(new V[] {}, new Results(R.Get(v_207, v_228)));
V v_233 = values_232[0];

var values_234 = R.Join(new V[] {}, new Results(R.Get(v_185, (V)((V)(R.Call(v_34, R.Join(new V[] {v_165}, new Results(R.Concat(R.Get(v_181, (V)"id"), R.Concat((V)":", v_228))))).First % (V)R.Length(v_185)) + (V)1.0d))));
V v_235 = values_234[0];
var values_236 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_233, (V)"x")), new Entry((V)"y", R.Get(v_233, (V)"y")), new Entry((V)"id", R.Call(R.Get(v_2, (V)"prop_id"), R.Join(new V[] {}, new Results(v_235))).First), new Entry((V)"family", v_183), new Entry((V)"room_id", R.Get(v_181, (V)"id")))));
var obj_237 = R.Get(v_167, (V)"props");
V key_238 = (V)((V)R.Length(R.Get(v_167, (V)"props")) + (V)1.0d);
R.Set(obj_237, key_238, values_236[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_167));
return Results.Empty;
})));
V v_239 = V.Nil;
v_239 = new V((RuleFunction)(args_240 => {
V v_241 = args_240[0];
V v_242 = args_240[1];
V v_243 = args_240[2];
foreach (var row_244 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_241, (V)"rooms"))))))) {
V v_245 = row_244[0];
V v_246 = row_244[1];
if (R.Truth(R.Call(v_29, R.Join(new V[] {R.Get(v_246, (V)"room"), v_242}, new Results(v_243))).First)) {
return R.Join(new V[] {}, new Results(v_246));
}
}
return Results.Empty;
}));
V v_247 = V.Nil;
v_247 = new V((RuleFunction)(args_248 => {
V v_249 = args_248[0];
V v_250 = args_248[1];
V v_251 = args_248[2];
V v_252 = args_248[3];
V v_253 = args_248[4];
var values_254 = R.Join(new V[] {}, R.Call(v_239, R.Join(new V[] {v_249, v_251}, new Results(v_252))));
V v_255 = values_254[0];

if (R.Truth((V)(!R.Truth(v_255)))) {
return Results.Empty;
}
if (R.Truth(R.And(R.And((V)(v_253 == (V)"floor"), () => R.Get(v_255, (V)"patch")), () => R.Call(v_29, R.Join(new V[] {R.Get(v_255, (V)"patch"), v_251}, new Results(v_252))).First))) {
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_255, (V)"patch"), (V)"material")));
}
return R.Join(new V[] {}, new Results(R.Get(v_255, v_253)));
return Results.Empty;
}));
R.Set(v_10, (V)"new", new V((RuleFunction)(args_256 => {
V v_257 = args_256[0];
V v_258 = args_256[1];
V v_259 = args_256[2];
V v_260 = args_256[3];
V v_261 = args_256[4];
var values_262 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"style", v_257), new Entry((V)"handles", R.Table()), new Entry((V)"materials", R.Table()), new Entry((V)"pages", R.Table()), new Entry((V)"failures", R.Table()), new Entry((V)"plan", R.Table(new Entry((V)"rooms", R.Table()), new Entry((V)"props", R.Table()))))}, new Results(R.Table(new Entry((V)"__index", v_10))))));
V v_263 = values_262[0];
var values_264 = R.Join(new V[] {}, new Results(R.Table()));
V v_265 = values_264[0];

var values_266 = R.Join(new V[] {}, new Results(R.Table()));
V v_267 = values_266[0];

var values_268 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(v_2, (V)"version")), new Entry((V)2, v_257))));
V v_269 = values_268[0];
V v_270 = V.Nil;
v_270 = new V((RuleFunction)(args_271 => {
V v_272 = args_271[0];
if (R.Truth((V)(R.Get(v_267, v_272) == (V)false))) {
return Results.Empty;
}
if (R.Truth(R.Get(v_267, v_272))) {
return R.Join(new V[] {}, new Results(R.Get(v_267, v_272)));
}
var values_273 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"sources"), v_272)));
V v_274 = values_273[0];

var values_275 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_258}, new Results(R.Get(v_274, (V)"path")))));
V v_276 = values_275[0];
V v_277 = values_275[1];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_276)), () => (V)(!R.Truth(v_277))), () => (V)(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_atlas"))).First, (V)"checksum"), R.Join(new V[] {}, new Results(v_277))).First != R.Get(v_274, (V)"checksum"))))) {
var values_278 = R.Join(new V[] {}, new Results(R.Concat((V)"missing/corrupt ", v_272)));
var obj_279 = R.Get(v_263, (V)"failures");
V key_280 = v_272;
R.Set(obj_279, key_280, values_278[0]);

var values_281 = R.Join(new V[] {}, new Results((V)false));
var obj_282 = v_267;
V key_283 = v_272;
R.Set(obj_282, key_283, values_281[0]);

return Results.Empty;
}
var values_284 = R.Join(new V[] {}, new Results(v_277));
var obj_285 = v_267;
V key_286 = v_272;
R.Set(obj_285, key_286, values_284[0]);

return R.Join(new V[] {}, new Results(v_277));
return Results.Empty;
}));
V v_287 = V.Nil;
v_287 = new V((RuleFunction)(args_288 => {
V v_289 = args_288[0];
V v_290 = args_288[1];
var values_291 = R.Join(new V[] {}, new Results(R.Concat(v_289, R.Concat((V)":", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_290))).First))));
V v_292 = values_291[0];

if (R.Truth((V)(R.Get(v_265, v_292) != V.Nil))) {
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_265, v_292), () => V.Nil)));
}
var values_293 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"sources"), v_289)));
V v_294 = values_293[0];

var values_295 = R.Join(new V[] {}, R.Call(v_270, R.Join(new V[] {}, new Results(v_289))));
V v_296 = values_295[0];

if (R.Truth((V)(!R.Truth(v_296)))) {
var values_297 = R.Join(new V[] {}, new Results((V)false));
var obj_298 = v_265;
V key_299 = v_292;
R.Set(obj_298, key_299, values_297[0]);

return Results.Empty;
}
var values_304 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(new V((RuleFunction)(args_300 => {

var values_301 = R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(v_290)), () => v_261), () => R.Get(v_261, v_289))));
V v_302 = values_301[0];
if (R.Truth((V)(!R.Truth(v_302)))) {
var values_303 = R.Join(new V[] {}, R.Call(v_259, R.Join(new V[] {v_296, R.Get(v_294, (V)"path")}, new Results(R.Or(R.Or(R.And(v_290, () => R.Table(new Entry((V)"environment", (V)true), new Entry((V)"sheet", v_289), new Entry((V)"style", v_257))), () => R.And(R.And((V)(v_257 == (V)"onebit"), () => (V)(v_289 != (V)"onebit")), () => (V)"approved-monochrome")), () => (V)false)))));
v_302 = values_303[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(v_302, () => (V)(R.Invoke(v_302, "getWidth", Results.Empty).First == R.Get(v_294, (V)"width"))), () => (V)(R.Invoke(v_302, "getHeight", Results.Empty).First == R.Get(v_294, (V)"height")))}, new Results((V)"wrong environment image dimensions")));
return R.Join(new V[] {}, new Results(v_302));
return Results.Empty;
}))))));
V v_305 = values_304[0];
V v_306 = values_304[1];
if (R.Truth((V)(!R.Truth(v_305)))) {
var values_307 = R.Join(new V[] {}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_306))));
var obj_308 = R.Get(v_263, (V)"failures");
V key_309 = v_292;
R.Set(obj_308, key_309, values_307[0]);

var values_310 = R.Join(new V[] {}, new Results((V)false));
var obj_311 = v_265;
V key_312 = v_292;
R.Set(obj_311, key_312, values_310[0]);

return Results.Empty;
}
var values_313 = R.Join(new V[] {}, new Results(v_306));
var obj_314 = v_265;
V key_315 = v_292;
R.Set(obj_314, key_315, values_313[0]);

var values_316 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_306))));
var obj_317 = R.Get(v_263, (V)"pages");
V key_318 = v_292;
R.Set(obj_317, key_318, values_316[0]);

return R.Join(new V[] {}, new Results(v_306));
return Results.Empty;
}));
V v_319 = V.Nil;
v_319 = new V((RuleFunction)(args_320 => {
V v_321 = args_320[0];
V v_322 = args_320[1];
var values_323 = R.Join(new V[] {}, R.Call(v_287, R.Join(new V[] {v_321}, new Results((V)false))));
V v_324 = values_323[0];

if (R.Truth((V)(!R.Truth(v_324)))) {
return Results.Empty;
}
var values_325 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"sources"), v_321)));
V v_326 = values_325[0];

var values_327 = R.Join(new V[] {(V)((V)(v_322 % R.Get(v_326, (V)"columns")) * R.Get(v_326, (V)"stride"))}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_322 / R.Get(v_326, (V)"columns"))))).First * R.Get(v_326, (V)"stride"))));
V v_328 = values_327[0];
V v_329 = values_327[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)((V)(v_328 + (V)16.0d) <= R.Get(v_326, (V)"width")), () => (V)((V)(v_329 + (V)16.0d) <= R.Get(v_326, (V)"height")))}, new Results((V)"bad environment source cell")));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_324), new Entry((V)"quad", R.Call(v_260, R.Join(new V[] {v_328, v_329, (V)16.0d, (V)16.0d, R.Get(v_326, (V)"width")}, new Results(R.Get(v_326, (V)"height")))).First), new Entry((V)"width", (V)16.0d), new Entry((V)"height", (V)16.0d), new Entry((V)"offset_x", v_328), new Entry((V)"offset_y", v_329), new Entry((V)"source", v_257), new Entry((V)"approved_sheet", v_321), new Entry((V)"tile_index", v_322), new Entry((V)"monochrome", (V)(v_257 == (V)"onebit")))));
return Results.Empty;
}));
foreach (var row_330 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"rpg"), new Entry((V)2, (V)"caves"), new Entry((V)3, (V)"onebit")))))))) {
V v_331 = row_330[0];
V v_332 = row_330[1];
var values_333 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"surfaces"), R.Join(new V[] {v_332}, new Results(v_257))));
V v_334 = values_333[0];
if (R.Truth((V)((V)R.Length(v_334) > (V)0.0d))) {
var values_335 = R.Join(new V[] {}, R.Call(v_287, R.Join(new V[] {v_332}, new Results((V)true))));
V v_336 = values_335[0];

var values_337 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"sources"), v_332)));
V v_338 = values_337[0];

var values_339 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_338, (V)"width") / (V)16.0d)))));
V v_340 = values_339[0];
if (R.Truth(v_336)) {
foreach (var row_341 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_334)))))) {
V v_342 = row_341[0];
V v_343 = row_341[1];
var values_344 = R.Join(new V[] {(V)((V)(R.Get(v_343, (V)"slot") % v_340) * (V)16.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_343, (V)"slot") / v_340)))).First * (V)16.0d)));
V v_345 = values_344[0];
V v_346 = values_344[1];

var values_347 = R.Join(new V[] {}, new Results(R.Get(v_343, (V)"material")));
V v_348 = values_347[0];
var values_349 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_336), new Entry((V)"quad", R.Call(v_260, R.Join(new V[] {v_345, v_346, (V)16.0d, (V)16.0d, R.Get(v_338, (V)"width")}, new Results(R.Get(v_338, (V)"height")))).First), new Entry((V)"width", (V)16.0d), new Entry((V)"height", (V)16.0d), new Entry((V)"offset_x", v_345), new Entry((V)"offset_y", v_346), new Entry((V)"source", v_257), new Entry((V)"approved_sheet", v_332), new Entry((V)"tile_index", R.Or(R.Get(v_348, (V)"center"), () => R.Get(R.Get(v_348, (V)"grid"), (V)5.0d))), new Entry((V)"derived", (V)true), new Entry((V)"material", R.Get(v_343, (V)"name")), new Entry((V)"mask", R.Get(v_343, (V)"mask")), new Entry((V)"monochrome", (V)(v_257 == (V)"onebit")))));
V v_350 = values_349[0];
var values_351 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_263, (V)"materials"), R.Get(v_343, (V)"name")), () => R.Table())));
var obj_352 = R.Get(v_263, (V)"materials");
V key_353 = R.Get(v_343, (V)"name");
R.Set(obj_352, key_353, values_351[0]);

var values_354 = R.Join(new V[] {}, new Results(v_350));
var obj_355 = R.Get(R.Get(v_263, (V)"materials"), R.Get(v_343, (V)"name"));
V key_356 = R.Get(v_343, (V)"mask");
R.Set(obj_355, key_356, values_354[0]);
if (R.Truth((V)(R.Get(v_343, (V)"mask") == (V)255.0d))) {
var values_357 = R.Join(new V[] {}, new Results(v_350));
var obj_358 = R.Get(v_263, (V)"handles");
V key_359 = R.Call(R.Get(v_2, (V)"material_id"), R.Join(new V[] {}, new Results(R.Get(v_343, (V)"name")))).First;
R.Set(obj_358, key_359, values_357[0]);
}
}
}
}
}
foreach (var row_360 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_19, R.Join(new V[] {}, new Results(R.Get(v_2, (V)"props"))))))))) {
V v_361 = row_360[0];
V v_362 = row_360[1];
var values_363 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"props"), v_362)));
V v_364 = values_363[0];

var values_365 = R.Join(new V[] {R.Get(v_364, (V)1.0d)}, new Results(R.Get(v_364, (V)2.0d)));
V v_366 = values_365[0];
V v_367 = values_365[1];
if (R.Truth(R.And((V)(v_257 == (V)"onebit"), () => R.Get(v_364, (V)3.0d)))) {
var values_368 = R.Join(new V[] {(V)"onebit"}, new Results(R.Get(v_364, (V)3.0d)));
v_366 = values_368[0];
v_367 = values_368[1];
}
var values_369 = R.Join(new V[] {}, new Results(R.Or(R.Call(v_319, R.Join(new V[] {v_366}, new Results(v_367))).First, () => R.Call(v_319, R.Join(new V[] {(V)"onebit"}, new Results(R.Or(R.And(R.Invoke(v_362, "match", R.Join(new V[] {}, new Results((V)"column"))).First, () => (V)542.0d), () => (V)3.0d)))).First)));
var obj_370 = R.Get(v_263, (V)"handles");
V key_371 = R.Call(R.Get(v_2, (V)"prop_id"), R.Join(new V[] {}, new Results(v_362))).First;
R.Set(obj_370, key_371, values_369[0]);
}
foreach (var row_372 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_19, R.Join(new V[] {}, new Results(R.Get(v_2, (V)"symbols"))))))))) {
V v_373 = row_372[0];
V v_374 = row_372[1];
var values_375 = R.Join(new V[] {}, R.Call(v_319, R.Join(new V[] {(V)"onebit"}, new Results(R.Get(R.Get(v_2, (V)"symbols"), v_374)))));
var obj_376 = R.Get(v_263, (V)"handles");
V key_377 = R.Call(R.Get(v_2, (V)"symbol_id"), R.Join(new V[] {}, new Results(v_374))).First;
R.Set(obj_376, key_377, values_375[0]);
}
foreach (var row_378 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_19, R.Join(new V[] {}, new Results(v_267)))))))) {
V v_379 = row_378[0];
V v_380 = row_378[1];
var values_381 = R.Join(new V[] {}, new Results(R.Concat(v_380, R.Concat((V)":", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results((V)(R.Get(v_267, v_380) != (V)false)))).First))));
var obj_382 = v_269;
V key_383 = (V)((V)R.Length(v_269) + (V)1.0d);
R.Set(obj_382, key_383, values_381[0]);
}
foreach (var row_384 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_19, R.Join(new V[] {}, new Results(R.Get(v_263, (V)"failures"))))))))) {
V v_385 = row_384[0];
V v_386 = row_384[1];
var values_387 = R.Join(new V[] {}, new Results(v_386));
var obj_388 = v_269;
V key_389 = (V)((V)R.Length(v_269) + (V)1.0d);
R.Set(obj_388, key_389, values_387[0]);
}
var values_390 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_269}, new Results((V)":"))));
var obj_391 = v_263;
V key_392 = (V)"signature";
R.Set(obj_391, key_392, values_390[0]);

return R.Join(new V[] {}, new Results(v_263));
return Results.Empty;
})));
R.Set(v_10, "set_world", new V((RuleFunction)(args_393 => {
V v_394 = args_393[0];
V v_395 = args_393[1];
var values_396 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_395, (V)"generated_map"), () => R.Get(v_395, (V)"final_map"))));
V v_397 = values_396[0];
if (R.Truth((V)(!R.Truth(v_397)))) {
var values_398 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"rooms", R.Table()), new Entry((V)"props", R.Table()))));
var obj_399 = v_394;
V key_400 = (V)"plan";
R.Set(obj_399, key_400, values_398[0]);

return Results.Empty;
}
var values_401 = R.Join(new V[] {}, new Results(R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_397))).First, R.Concat((V)":", R.Concat(R.Get(v_395, (V)"active_floor_id"), R.Concat((V)":", R.Get(v_395, (V)"next_event_id")))))));
V v_402 = values_401[0];
if (R.Truth((V)(v_402 != R.Get(v_394, (V)"plan_key")))) {
var values_403 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"plan"), R.Join(new V[] {v_395}, new Results(v_397))));
var obj_404 = v_394;
V key_405 = (V)"plan";
R.Set(obj_404, key_405, values_403[0]);

var values_406 = R.Join(new V[] {}, new Results(v_402));
var obj_407 = v_394;
V key_408 = (V)"plan_key";
R.Set(obj_407, key_408, values_406[0]);
}
return Results.Empty;
})));
R.Set(v_10, "resolve", new V((RuleFunction)(args_409 => {
V v_410 = args_409[0];
V v_411 = args_409[1];
V v_412 = args_409[2];
V v_413 = args_409[3];
V v_414 = args_409[4];
V v_415 = args_409[5];
var values_416 = R.Join(new V[] {}, new Results(R.Get(v_410, (V)"plan")));
V v_417 = values_416[0];
if (R.Truth((V)((V)R.Length(R.Get(v_417, (V)"rooms")) == (V)0.0d))) {
var values_418 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"plan"), R.Join(new V[] {V.Nil}, new Results(v_411))));
v_417 = values_418[0];
}
var values_419 = R.Join(new V[] {}, R.Call(v_247, R.Join(new V[] {v_417, v_411, v_412, v_413}, new Results(v_414))));
V v_420 = values_419[0];

if (R.Truth(R.Or((V)(!R.Truth(v_420)), () => (V)(!R.Truth(R.Get(R.Get(v_410, (V)"materials"), v_420)))))) {
return Results.Empty;
}
var values_421 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_410, (V)"materials"), v_420)));
V v_422 = values_421[0];

if (R.Truth(R.And(R.Get(v_422, (V)255.0d), () => (V)(!R.Truth(R.Get(v_422, (V)0.0d)))))) {
return R.Join(new V[] {}, new Results(R.Get(v_422, (V)255.0d)));
}
var values_423 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_414 == (V)"wall"), () => (V)"#"), () => R.And((V)(v_414 == (V)"water"), () => (V)"~")), () => (V)".")));
V v_424 = values_423[0];

var values_425 = R.Join(new V[] {}, new Results((V)0.0d));
V v_426 = values_425[0];
foreach (var row_427 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_428 = row_427[0];
V v_429 = row_427[1];
var values_430 = R.Join(new V[] {(V)(v_412 + R.Get(v_429, (V)1.0d))}, new Results((V)(v_413 + R.Get(v_429, (V)2.0d))));
V v_431 = values_430[0];
V v_432 = values_430[1];

var values_433 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_411, (V)"rows"), v_432)));
V v_434 = values_433[0];
if (R.Truth(R.And(R.And(R.And(v_434, () => (V)(v_431 >= (V)1.0d)), () => (V)(v_431 <= (V)R.Length(v_434))), () => R.Or(R.And(v_415, () => (V)(!R.Truth(R.Call(v_415, R.Join(new V[] {v_431}, new Results(v_432))).First))), () => R.And((V)(R.Invoke(v_434, "sub", R.Join(new V[] {v_431}, new Results(v_431))).First == v_424), () => (V)(R.Call(v_247, R.Join(new V[] {v_417, v_411, v_431, v_432}, new Results(v_414))).First == v_420)))))) {
var values_435 = R.Join(new V[] {}, new Results((V)(v_426 + R.Get(v_429, (V)3.0d))));
v_426 = values_435[0];
}
}
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_422, R.Call(R.Get(v_4, (V)"canonical"), R.Join(new V[] {}, new Results(v_426))).First), () => R.Get(v_422, (V)255.0d))));
return Results.Empty;
})));
R.Set(v_10, "prop", new V((RuleFunction)(args_436 => {
V v_437 = args_436[0];
V v_438 = args_436[1];
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_437, (V)"handles"), R.Call(R.Get(v_2, (V)"prop_id"), R.Join(new V[] {}, new Results(v_438))).First)));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_10));
return Results.Empty;
} }
