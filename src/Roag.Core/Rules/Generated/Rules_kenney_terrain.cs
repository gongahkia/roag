// Static C# port of src/kenney_terrain.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 43fa7c6fd2417ac9e854f58b40f503143264718c00e2015b66841d24dcf6ba16
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_kenney_terrain { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_atlas"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"path", (V)"assets/kenney/caves-dungeons/terrain.png"), new Entry((V)"checksum", (V)"974b7d14df86b1d7"), new Entry((V)"width", (V)492.0d), new Entry((V)"height", (V)305.0d), new Entry((V)"tile", (V)16.0d), new Entry((V)"columns", (V)29.0d), new Entry((V)"stride", (V)17.0d))));
V v_6 = values_5[0];
V v_7 = V.Nil;
v_7 = new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
V v_10 = args_8[1];
return R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_9 / v_10)))).First % (V)2.0d) == (V)1.0d)));
return Results.Empty;
}));
var values_11 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)16.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)4.0d), new Entry((V)3, (V)32.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)8.0d), new Entry((V)3, (V)64.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)8.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)128.0d))))));
V v_12 = values_11[0];
R.Set(v_6, (V)"canonical", new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
var values_15 = R.Join(new V[] {}, new Results((V)(v_14 % (V)16.0d)));
V v_16 = values_15[0];
foreach (var row_17 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_18 = row_17[0];
V v_19 = row_17[1];
if (R.Truth(R.And(R.And(R.Call(v_7, R.Join(new V[] {v_14}, new Results(R.Get(v_19, (V)1.0d)))).First, () => R.Call(v_7, R.Join(new V[] {v_14}, new Results(R.Get(v_19, (V)2.0d)))).First), () => R.Call(v_7, R.Join(new V[] {v_14}, new Results(R.Get(v_19, (V)3.0d)))).First))) {
var values_20 = R.Join(new V[] {}, new Results((V)(v_16 + R.Get(v_19, (V)3.0d))));
v_16 = values_20[0];
}
}
return R.Join(new V[] {}, new Results(v_16));
return Results.Empty;
})));
var values_21 = R.Join(new V[] {}, new Results(R.Table()));
var obj_22 = v_6;
V key_23 = (V)"masks";
R.Set(obj_22, key_23, values_21[0]);
{ double end_26 = R.Num((V)255.0d), step_27 = R.Num((V)1.0d);
for (double i_25 = R.Num((V)0.0d); step_27 > 0 ? i_25 <= end_26 : i_25 >= end_26; i_25 += step_27) {
V v_24 = i_25;
if (R.Truth((V)(R.Call(R.Get(v_6, (V)"canonical"), R.Join(new V[] {}, new Results(v_24))).First == v_24))) {
var values_28 = R.Join(new V[] {}, new Results(v_24));
var obj_29 = R.Get(v_6, (V)"masks");
V key_30 = (V)((V)R.Length(R.Get(v_6, (V)"masks")) + (V)1.0d);
R.Set(obj_29, key_30, values_28[0]);
}
}
}
var values_31 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"overgrown_ruins", R.Table(new Entry((V)"wall", (V)"stone"), new Entry((V)"water", (V)"blue"), new Entry((V)"floor", R.Table(new Entry((V)1, (V)67.0d))))), new Entry((V)"fungal_hollows", R.Table(new Entry((V)"wall", (V)"earth"), new Entry((V)"water", (V)"green"), new Entry((V)"floor", R.Table(new Entry((V)1, (V)212.0d))))), new Entry((V)"sunken_aqueduct", R.Table(new Entry((V)"wall", (V)"stone"), new Entry((V)"water", (V)"blue"), new Entry((V)"floor", R.Table(new Entry((V)1, (V)335.0d), new Entry((V)2, (V)336.0d), new Entry((V)3, (V)337.0d), new Entry((V)4, (V)338.0d))))), new Entry((V)"ashen_bastion", R.Table(new Entry((V)"wall", (V)"stone"), new Entry((V)"water", (V)"blue"), new Entry((V)"floor", R.Table(new Entry((V)1, (V)364.0d), new Entry((V)2, (V)365.0d), new Entry((V)3, (V)366.0d), new Entry((V)4, (V)367.0d))))), new Entry((V)"glacial_observatory", R.Table(new Entry((V)"wall", (V)"stone"), new Entry((V)"water", (V)"blue"), new Entry((V)"floor", R.Table(new Entry((V)1, (V)306.0d), new Entry((V)2, (V)307.0d), new Entry((V)3, (V)308.0d), new Entry((V)4, (V)309.0d))))), new Entry((V)"fractured_archive", R.Table(new Entry((V)"wall", (V)"stone"), new Entry((V)"water", (V)"blue"), new Entry((V)"floor", R.Table(new Entry((V)1, (V)422.0d), new Entry((V)2, (V)423.0d), new Entry((V)3, (V)424.0d), new Entry((V)4, (V)425.0d))))), new Entry((V)"sundered_forge", R.Table(new Entry((V)"wall", (V)"earth"), new Entry((V)"water", (V)"green"), new Entry((V)"floor", R.Table(new Entry((V)1, (V)393.0d), new Entry((V)2, (V)394.0d), new Entry((V)3, (V)395.0d), new Entry((V)4, (V)396.0d))))))));
var obj_32 = v_6;
V key_33 = (V)"styles";
R.Set(obj_32, key_33, values_31[0]);
var values_34 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"stone", R.Table(new Entry((V)"frame", (V)38.0d), new Entry((V)"center", (V)39.0d))), new Entry((V)"earth", R.Table(new Entry((V)"frame", (V)183.0d), new Entry((V)"center", (V)184.0d))))));
var obj_35 = v_6;
V key_36 = (V)"wall_materials";
R.Set(obj_35, key_36, values_34[0]);
var values_37 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"blue", R.Table(new Entry((V)1, (V)290.0d), new Entry((V)2, (V)291.0d), new Entry((V)3, (V)292.0d), new Entry((V)4, (V)319.0d), new Entry((V)5, (V)320.0d), new Entry((V)6, (V)321.0d), new Entry((V)7, (V)348.0d), new Entry((V)8, (V)349.0d), new Entry((V)9, (V)350.0d), new Entry((V)10, (V)322.0d), new Entry((V)11, (V)323.0d), new Entry((V)12, (V)294.0d), new Entry((V)13, (V)293.0d))), new Entry((V)"green", R.Table(new Entry((V)1, (V)295.0d), new Entry((V)2, (V)296.0d), new Entry((V)3, (V)297.0d), new Entry((V)4, (V)324.0d), new Entry((V)5, (V)325.0d), new Entry((V)6, (V)326.0d), new Entry((V)7, (V)353.0d), new Entry((V)8, (V)354.0d), new Entry((V)9, (V)355.0d), new Entry((V)10, (V)327.0d), new Entry((V)11, (V)328.0d), new Entry((V)12, (V)299.0d), new Entry((V)13, (V)298.0d))))));
var obj_38 = v_6;
V key_39 = (V)"water_materials";
R.Set(obj_38, key_39, values_37[0]);
R.Set(v_6, (V)"region", new V((RuleFunction)(args_40 => {
V v_41 = args_40[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_41))).First == (V)"number"), () => (V)((V)(v_41 % (V)1.0d) == (V)0.0d)), () => (V)(v_41 >= (V)0.0d)), () => (V)(v_41 < (V)522.0d))}, new Results((V)"invalid Caves & Dungeons tile index")));
return R.Join(new V[] {(V)((V)(v_41 % (V)29.0d) * (V)17.0d), (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_41 / (V)29.0d)))).First * (V)17.0d), (V)16.0d}, new Results((V)16.0d));
return Results.Empty;
})));
var values_42 = R.Join(new V[] {}, new Results(R.Table()));
V v_43 = values_42[0];
foreach (var row_44 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"stone"), new Entry((V)2, (V)"earth"), new Entry((V)3, (V)"blue"), new Entry((V)4, (V)"green")))))))) {
V v_45 = row_44[0];
V v_46 = row_44[1];
foreach (var row_47 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"masks"))))))) {
V v_48 = row_47[0];
V v_49 = row_47[1];
var values_50 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"family", v_46), new Entry((V)"mask", v_49))));
var obj_51 = v_43;
V key_52 = (V)((V)R.Length(v_43) + (V)1.0d);
R.Set(obj_51, key_52, values_50[0]);
}
}
var values_53 = R.Join(new V[] {}, new Results(v_43));
var obj_54 = v_6;
V key_55 = (V)"surfaces";
R.Set(obj_54, key_55, values_53[0]);
R.Set(v_6, (V)"surface_sheet", new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
var values_58 = R.Join(new V[] {}, R.Invoke(v_57, "clone", Results.Empty));
V v_59 = values_58[0];
V v_60 = V.Nil;
v_60 = new V((RuleFunction)(args_61 => {
V v_62 = args_61[0];
V v_63 = args_61[1];
V v_64 = args_61[2];
var values_65 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"region"), R.Join(new V[] {}, new Results(v_62))));
V v_66 = values_65[0];
V v_67 = values_65[1];
return R.Join(new V[] {}, R.Invoke(v_57, "getPixel", R.Join(new V[] {(V)(v_66 + v_63)}, new Results((V)(v_67 + v_64)))));
return Results.Empty;
}));
var values_68 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)8.0d), new Entry((V)3, (V)128.0d), new Entry((V)4, (V)0.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)1.0d), new Entry((V)7, (V)2.0d), new Entry((V)8, (V)4.0d), new Entry((V)9, (V)10.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)16.0d), new Entry((V)4, (V)8.0d), new Entry((V)5, (V)0.0d), new Entry((V)6, (V)3.0d), new Entry((V)7, (V)2.0d), new Entry((V)8, (V)6.0d), new Entry((V)9, (V)11.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)2.0d), new Entry((V)3, (V)32.0d), new Entry((V)4, (V)8.0d), new Entry((V)5, (V)8.0d), new Entry((V)6, (V)9.0d), new Entry((V)7, (V)8.0d), new Entry((V)8, (V)6.0d), new Entry((V)9, (V)12.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)8.0d), new Entry((V)3, (V)64.0d), new Entry((V)4, (V)0.0d), new Entry((V)5, (V)8.0d), new Entry((V)6, (V)7.0d), new Entry((V)7, (V)8.0d), new Entry((V)8, (V)4.0d), new Entry((V)9, (V)13.0d))))));
V v_69 = values_68[0];
foreach (var row_70 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_43)))))) {
V v_71 = row_70[0];
V v_72 = row_70[1];
var values_73 = R.Join(new V[] {(V)((V)((V)(v_71 - (V)1.0d) % (V)29.0d) * (V)16.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_71 - (V)1.0d) / (V)29.0d)))).First * (V)16.0d)));
V v_74 = values_73[0];
V v_75 = values_73[1];
var values_76 = R.Join(new V[] {R.Get(v_72, (V)"mask")}, new Results(R.Get(v_72, (V)"family")));
V v_77 = values_76[0];
V v_78 = values_76[1];
var values_79 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"wall_materials"), v_78)));
V v_80 = values_79[0];
if (R.Truth(v_80)) {
{ double end_83 = R.Num((V)15.0d), step_84 = R.Num((V)1.0d);
for (double i_82 = R.Num((V)0.0d); step_84 > 0 ? i_82 <= end_83 : i_82 >= end_83; i_82 += step_84) {
V v_81 = i_82;
{ double end_87 = R.Num((V)15.0d), step_88 = R.Num((V)1.0d);
for (double i_86 = R.Num((V)0.0d); step_88 > 0 ? i_86 <= end_87 : i_86 >= end_87; i_86 += step_88) {
V v_85 = i_86;
var values_89 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {R.Get(v_80, (V)"center"), (V)8.0d}, new Results((V)8.0d))));
V v_90 = values_89[0];
V v_91 = values_89[1];
V v_92 = values_89[2];
V v_93 = values_89[3];
if (R.Truth(R.And((V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results((V)1.0d))).First)), () => (V)(v_81 < (V)4.0d)))) {
var values_94 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {R.Get(v_80, (V)"frame"), (V)8.0d}, new Results(v_81))));
v_90 = values_94[0];
v_91 = values_94[1];
v_92 = values_94[2];
v_93 = values_94[3];
}
if (R.Truth(R.And((V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results((V)4.0d))).First)), () => (V)(v_81 >= (V)12.0d)))) {
var values_95 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {R.Get(v_80, (V)"frame"), (V)8.0d}, new Results(v_81))));
v_90 = values_95[0];
v_91 = values_95[1];
v_92 = values_95[2];
v_93 = values_95[3];
}
if (R.Truth(R.And((V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results((V)8.0d))).First)), () => (V)(v_85 < (V)4.0d)))) {
var values_96 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {R.Get(v_80, (V)"frame"), v_85}, new Results((V)8.0d))));
v_90 = values_96[0];
v_91 = values_96[1];
v_92 = values_96[2];
v_93 = values_96[3];
}
if (R.Truth(R.And((V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results((V)2.0d))).First)), () => (V)(v_85 >= (V)12.0d)))) {
var values_97 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {R.Get(v_80, (V)"frame"), v_85}, new Results((V)8.0d))));
v_90 = values_97[0];
v_91 = values_97[1];
v_92 = values_97[2];
v_93 = values_97[3];
}
foreach (var row_98 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_69)))))) {
V v_99 = row_98[0];
V v_100 = row_98[1];
var values_101 = R.Join(new V[] {R.Or(R.And((V)(R.Get(v_100, (V)4.0d) == (V)0.0d), () => v_85), () => (V)((V)15.0d - v_85))}, new Results(R.Or(R.And((V)(R.Get(v_100, (V)5.0d) == (V)0.0d), () => v_81), () => (V)((V)15.0d - v_81))));
V v_102 = values_101[0];
V v_103 = values_101[1];
if (R.Truth(R.And((V)(v_102 < (V)4.0d), () => (V)(v_103 < (V)4.0d)))) {
var values_104 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_100, (V)4.0d) == (V)0.0d), () => v_102), () => (V)((V)15.0d - v_102))));
V v_105 = values_104[0];
var values_106 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_100, (V)5.0d) == (V)0.0d), () => v_103), () => (V)((V)15.0d - v_103))));
V v_107 = values_106[0];
if (R.Truth(R.And((V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results(R.Get(v_100, (V)1.0d)))).First)), () => (V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results(R.Get(v_100, (V)2.0d)))).First))))) {
var values_108 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {R.Get(v_80, (V)"frame"), v_105}, new Results(v_107))));
v_90 = values_108[0];
v_91 = values_108[1];
v_92 = values_108[2];
v_93 = values_108[3];
} else if (R.Truth(R.And(R.And(R.Call(v_7, R.Join(new V[] {v_77}, new Results(R.Get(v_100, (V)1.0d)))).First, () => R.Call(v_7, R.Join(new V[] {v_77}, new Results(R.Get(v_100, (V)2.0d)))).First), () => (V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results(R.Get(v_100, (V)3.0d)))).First))))) {
var values_109 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {R.Get(v_80, (V)"frame"), R.Or(R.And((V)(R.Get(v_100, (V)4.0d) == (V)0.0d), () => (V)((V)3.0d - v_102)), () => (V)((V)12.0d + v_102))}, new Results(R.Or(R.And((V)(R.Get(v_100, (V)5.0d) == (V)0.0d), () => (V)((V)3.0d - v_103)), () => (V)((V)12.0d + v_103))))));
v_90 = values_109[0];
v_91 = values_109[1];
v_92 = values_109[2];
v_93 = values_109[3];
}
}
}
if (R.Truth((V)(v_93 == (V)0.0d))) {
var values_110 = R.Join(new V[] {}, R.Call(v_60, R.Join(new V[] {R.Get(v_80, (V)"center"), (V)8.0d}, new Results((V)8.0d))));
v_90 = values_110[0];
v_91 = values_110[1];
v_92 = values_110[2];
v_93 = values_110[3];
}
R.Invoke(v_59, "setPixel", R.Join(new V[] {(V)(v_74 + v_85), (V)(v_75 + v_81), v_90, v_91, v_92}, new Results(v_93)));
}
}
}
}
} else {
var values_111 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"water_materials"), v_78)));
V v_112 = values_111[0];
foreach (var row_113 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_69)))))) {
V v_114 = row_113[0];
V v_115 = row_113[1];
var values_116 = Results.Empty;
V v_117 = values_116[0];
if (R.Truth(R.And((V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results(R.Get(v_115, (V)1.0d)))).First)), () => (V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results(R.Get(v_115, (V)2.0d)))).First))))) {
var values_118 = R.Join(new V[] {}, new Results(R.Get(v_115, (V)6.0d)));
v_117 = values_118[0];
} else if (R.Truth((V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results(R.Get(v_115, (V)1.0d)))).First)))) {
var values_119 = R.Join(new V[] {}, new Results(R.Get(v_115, (V)7.0d)));
v_117 = values_119[0];
} else if (R.Truth((V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results(R.Get(v_115, (V)2.0d)))).First)))) {
var values_120 = R.Join(new V[] {}, new Results(R.Get(v_115, (V)8.0d)));
v_117 = values_120[0];
} else if (R.Truth((V)(!R.Truth(R.Call(v_7, R.Join(new V[] {v_77}, new Results(R.Get(v_115, (V)3.0d)))).First)))) {
var values_121 = R.Join(new V[] {}, new Results(R.Get(v_115, (V)9.0d)));
v_117 = values_121[0];
} else {
var values_122 = R.Join(new V[] {}, new Results((V)5.0d));
v_117 = values_122[0];
}
{ double end_125 = R.Num((V)(R.Get(v_115, (V)5.0d) + (V)7.0d)), step_126 = R.Num((V)1.0d);
for (double i_124 = R.Num(R.Get(v_115, (V)5.0d)); step_126 > 0 ? i_124 <= end_125 : i_124 >= end_125; i_124 += step_126) {
V v_123 = i_124;
{ double end_129 = R.Num((V)(R.Get(v_115, (V)4.0d) + (V)7.0d)), step_130 = R.Num((V)1.0d);
for (double i_128 = R.Num(R.Get(v_115, (V)4.0d)); step_130 > 0 ? i_128 <= end_129 : i_128 >= end_129; i_128 += step_130) {
V v_127 = i_128;
R.Invoke(v_59, "setPixel", R.Join(new V[] {(V)(v_74 + v_127), (V)(v_75 + v_123)}, R.Call(v_60, R.Join(new V[] {R.Get(v_112, v_117), v_127}, new Results(v_123)))));
}
}
}
}
}
}
}
return R.Join(new V[] {}, new Results(v_59));
return Results.Empty;
})));
R.Set(v_6, (V)"new", new V((RuleFunction)(args_131 => {
V v_132 = args_131[0];
V v_133 = args_131[1];
V v_134 = args_131[2];
var values_135 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_132, R.Join(new V[] {}, new Results(R.Get(v_6, (V)"path")))).First}, new Results((V)"missing Caves & Dungeons sheet"))));
V v_136 = values_135[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_2, (V)"checksum"), R.Join(new V[] {}, new Results(v_136))).First == R.Get(v_6, (V)"checksum"))}, new Results((V)"corrupt Caves & Dungeons sheet")));
var values_137 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_133, R.Join(new V[] {v_136, R.Get(v_6, (V)"path")}, new Results((V)false))).First}, new Results((V)"unreadable Caves & Dungeons sheet"))));
V v_138 = values_137[0];
var values_139 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_133, R.Join(new V[] {v_136, R.Get(v_6, (V)"path")}, new Results((V)"caves-surfaces"))).First}, new Results((V)"unreadable Caves surfaces"))));
V v_140 = values_139[0];
foreach (var row_141 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_138), new Entry((V)2, v_140)))))))) {
V v_142 = row_141[0];
V v_143 = row_141[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Invoke(v_143, "getWidth", Results.Empty).First == R.Get(v_6, (V)"width")), () => (V)(R.Invoke(v_143, "getHeight", Results.Empty).First == R.Get(v_6, (V)"height")))}, new Results((V)"Caves & Dungeons dimensions differ")));
}
var values_144 = R.Join(new V[] {R.Table(), R.Table(), R.Table()}, new Results(R.Table()));
V v_145 = values_144[0];
V v_146 = values_144[1];
V v_147 = values_144[2];
V v_148 = values_144[3];
foreach (var row_149 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_43)))))) {
V v_150 = row_149[0];
V v_151 = row_149[1];
var values_152 = R.Join(new V[] {(V)((V)((V)(v_150 - (V)1.0d) % (V)29.0d) * (V)16.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_150 - (V)1.0d) / (V)29.0d)))).First * (V)16.0d)));
V v_153 = values_152[0];
V v_154 = values_152[1];
var values_155 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_140), new Entry((V)"quad", R.Call(v_134, R.Join(new V[] {v_153, v_154, (V)16.0d, (V)16.0d, R.Get(v_6, (V)"width")}, new Results(R.Get(v_6, (V)"height")))).First), new Entry((V)"width", (V)16.0d), new Entry((V)"height", (V)16.0d), new Entry((V)"offset_x", v_153), new Entry((V)"offset_y", v_154), new Entry((V)"source", (V)"kenney"), new Entry((V)"terrain_pack", (V)"caves"), new Entry((V)"surface_family", R.Get(v_151, (V)"family")), new Entry((V)"wall_mask", R.Or(R.And(R.Get(R.Get(v_6, (V)"wall_materials"), R.Get(v_151, (V)"family")), () => R.Get(v_151, (V)"mask")), () => V.Nil)), new Entry((V)"water_mask", R.Or(R.And(R.Get(R.Get(v_6, (V)"water_materials"), R.Get(v_151, (V)"family")), () => R.Get(v_151, (V)"mask")), () => V.Nil)))));
V v_156 = values_155[0];
var values_157 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(R.Get(v_6, (V)"wall_materials"), R.Get(v_151, (V)"family")), () => v_145), () => v_146)));
V v_158 = values_157[0];
var values_159 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_158, R.Get(v_151, (V)"family")), () => R.Table())));
var obj_160 = v_158;
V key_161 = R.Get(v_151, (V)"family");
R.Set(obj_160, key_161, values_159[0]);
var values_162 = R.Join(new V[] {}, new Results(v_156));
var obj_163 = R.Get(v_158, R.Get(v_151, (V)"family"));
V key_164 = R.Get(v_151, (V)"mask");
R.Set(obj_163, key_164, values_162[0]);
}
var values_165 = R.Join(new V[] {}, new Results(R.Table()));
V v_166 = values_165[0];
foreach (var row_167 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_4, (V)"order"))))))) {
V v_168 = row_167[0];
V v_169 = row_167[1];
var values_170 = R.Join(new V[] {}, R.Invoke(v_169, "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))));
V v_171 = values_170[0];

var values_172 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"styles"), v_171)))));
V v_173 = values_172[0];
var values_174 = R.Join(new V[] {}, new Results(R.Table()));
var obj_175 = v_147;
V key_176 = v_171;
R.Set(obj_175, key_176, values_174[0]);
foreach (var row_177 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_173, (V)"floor"))))))) {
V v_178 = row_177[0];
V v_179 = row_177[1];
if (R.Truth((V)(!R.Truth(R.Get(v_166, v_179))))) {
var values_180 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"region"), R.Join(new V[] {}, new Results(v_179))));
V v_181 = values_180[0];
V v_182 = values_180[1];
var values_183 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_138), new Entry((V)"quad", R.Call(v_134, R.Join(new V[] {v_181, v_182, (V)16.0d, (V)16.0d, R.Get(v_6, (V)"width")}, new Results(R.Get(v_6, (V)"height")))).First), new Entry((V)"width", (V)16.0d), new Entry((V)"height", (V)16.0d), new Entry((V)"offset_x", v_181), new Entry((V)"offset_y", v_182), new Entry((V)"source", (V)"kenney"), new Entry((V)"terrain_pack", (V)"caves"), new Entry((V)"tile_index", v_179))));
var obj_184 = v_166;
V key_185 = v_179;
R.Set(obj_184, key_185, values_183[0]);
}
var values_186 = R.Join(new V[] {}, new Results(R.Get(v_166, v_179)));
var obj_187 = R.Get(v_147, v_171);
V key_188 = (V)((V)R.Length(R.Get(v_147, v_171)) + (V)1.0d);
R.Set(obj_187, key_188, values_186[0]);
}
var values_189 = R.Join(new V[] {}, new Results(R.Concat((V)"core:terrain/", R.Concat(v_171, (V)"/"))));
V v_190 = values_189[0];
var values_191 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_147, v_171), (V)1.0d)));
var obj_192 = v_148;
V key_193 = R.Concat(v_190, (V)"floor/base");
R.Set(obj_192, key_193, values_191[0]);
var values_194 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_145, R.Get(v_173, (V)"wall")), (V)255.0d)));
var obj_195 = v_148;
V key_196 = R.Concat(v_190, (V)"wall/base");
R.Set(obj_195, key_196, values_194[0]);
var values_197 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_146, R.Get(v_173, (V)"water")), (V)255.0d)));
var obj_198 = v_148;
V key_199 = R.Concat(v_190, (V)"water/base");
R.Set(obj_198, key_199, values_197[0]);
}
return R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"native", v_138), new Entry((V)"surface", v_140), new Entry((V)"bindings", v_148), new Entry((V)"walls", v_145), new Entry((V)"waters", v_146), new Entry((V)"floors", v_147), new Entry((V)"signature", R.Concat(R.Get(v_6, (V)"checksum"), (V)":surfaces-1")))}, new Results(R.Table(new Entry((V)"__index", v_6))))));
return Results.Empty;
})));
R.Set(v_6, "resolve", new V((RuleFunction)(args_200 => {
V v_201 = args_200[0];
V v_202 = args_200[1];
V v_203 = args_200[2];
V v_204 = args_200[3];
V v_205 = args_200[4];
V v_206 = args_200[5];
var values_207 = R.Join(new V[] {}, new Results(R.Or(R.Invoke(R.Or(R.Get(v_202, (V)"biome_id"), () => (V)""), "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))).First, () => (V)"overgrown_ruins")));
V v_208 = values_207[0];
if (R.Truth((V)(!R.Truth(R.Get(R.Get(v_6, (V)"styles"), v_208))))) {
var values_209 = R.Join(new V[] {}, new Results((V)"overgrown_ruins"));
v_208 = values_209[0];
}
var values_210 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"styles"), v_208)));
V v_211 = values_210[0];
if (R.Truth((V)(v_205 == (V)"floor"))) {
var values_212 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_201, (V)"floors"), v_208)));
V v_213 = values_212[0];
return R.Join(new V[] {}, new Results(R.Get(v_213, (V)((V)((V)((V)(v_203 * (V)3.0d) + (V)(v_204 * (V)5.0d)) % (V)R.Length(v_213)) + (V)1.0d))));
}
V v_214 = V.Nil;
v_214 = new V((RuleFunction)(args_215 => {
V v_216 = args_215[0];
V v_217 = args_215[1];
var values_218 = R.Join(new V[] {(V)(v_203 + v_216)}, new Results((V)(v_204 + v_217)));
V v_219 = values_218[0];
V v_220 = values_218[1];

var values_221 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_202, (V)"rows"), v_220)));
V v_222 = values_221[0];
if (R.Truth(R.Or(R.Or((V)(v_219 < (V)1.0d), () => (V)(!R.Truth(v_222))), () => (V)(v_219 > (V)R.Length(v_222))))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth(R.And(v_206, () => (V)(!R.Truth(R.Call(v_206, R.Join(new V[] {v_219}, new Results(v_220))).First))))) {
return R.Join(new V[] {}, new Results((V)true));
}
return R.Join(new V[] {}, new Results((V)(R.Invoke(v_222, "sub", R.Join(new V[] {v_219}, new Results(v_219))).First == R.Or(R.And((V)(v_205 == (V)"wall"), () => (V)"#"), () => (V)"~"))));
return Results.Empty;
}));
var values_223 = R.Join(new V[] {}, new Results((V)0.0d));
V v_224 = values_223[0];
foreach (var row_225 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)4.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)8.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)16.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)32.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)64.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)128.0d)))))))))) {
V v_226 = row_225[0];
V v_227 = row_225[1];
if (R.Truth(R.Call(v_214, R.Join(new V[] {R.Get(v_227, (V)1.0d)}, new Results(R.Get(v_227, (V)2.0d)))).First)) {
var values_228 = R.Join(new V[] {}, new Results((V)(v_224 + R.Get(v_227, (V)3.0d))));
v_224 = values_228[0];
}
}
var values_229 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"canonical"), R.Join(new V[] {}, new Results(v_224))));
v_224 = values_229[0];
return R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_205 == (V)"wall"), () => R.Get(R.Get(R.Get(v_201, (V)"walls"), R.Get(v_211, (V)"wall")), v_224)), () => R.Get(R.Get(R.Get(v_201, (V)"waters"), R.Get(v_211, (V)"water")), v_224))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
