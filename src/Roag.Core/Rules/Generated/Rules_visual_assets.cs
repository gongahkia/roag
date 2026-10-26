// Static C# port of src/visual_assets.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 418db29c9a4792b81779cfab04a50939796073d0e4814f7dbdc44b99ae8b013c
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visual_assets { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_project"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_atlas_runtime"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table()));
V v_6 = values_5[0];
R.Set(v_6, (V)"new", new V((RuleFunction)(args_7 => {
V v_8 = args_7[0];
V v_9 = args_7[1];
V v_10 = args_7[2];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"bindings", R.Table()), new Entry((V)"by_id", R.Table()), new Entry((V)"cache", R.Table()), new Entry((V)"image_data", R.Table()), new Entry((V)"has_terrain", (V)false), new Entry((V)"failed", R.Table()), new Entry((V)"status", R.Or(R.And(v_10, () => (V)"generated"), () => (V)"absent")), new Entry((V)"atlas_status", (V)"absent"), new Entry((V)"generated", v_10))}, new Results(R.Table(new Entry((V)"__index", v_6))))));
V v_12 = values_11[0];
if (R.Truth(v_10)) {
var values_13 = R.Join(new V[] {}, new Results((V)true));
var obj_14 = v_12;
V key_15 = (V)"has_terrain";
R.Set(obj_14, key_15, values_13[0]);
}
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_8}, new Results((V)"assets/studio/manifest.txt"))));
V v_17 = values_16[0];
V v_18 = values_16[1];
if (R.Truth(R.Or((V)(!R.Truth(v_17)), () => (V)(!R.Truth(v_18))))) {
return R.Join(new V[] {}, new Results(v_12));
}
var values_19 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"decode")}, new Results(v_18))));
V v_20 = values_19[0];
V v_21 = values_19[1];
if (R.Truth((V)(!R.Truth(v_20)))) {
var values_22 = R.Join(new V[] {}, new Results(R.Concat((V)"invalid manifest: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_21))).First)));
var obj_23 = v_12;
V key_24 = (V)"status";
R.Set(obj_23, key_24, values_22[0]);

return R.Join(new V[] {}, new Results(v_12));
}
var values_25 = R.Join(new V[] {}, new Results(R.Get(v_21, (V)"id")));
var obj_26 = v_12;
V key_27 = (V)"project_id";
R.Set(obj_26, key_27, values_25[0]);
foreach (var row_28 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_21, (V)"assets"))))))) {
V v_29 = row_28[0];
V v_30 = row_28[1];
if (R.Truth((V)(R.Get(v_30, (V)"source") != (V)""))) {
var values_31 = R.Join(new V[] {}, new Results(v_30));
var obj_32 = R.Get(v_12, (V)"by_id");
V key_33 = R.Get(v_30, (V)"id");
R.Set(obj_32, key_33, values_31[0]);
}
if (R.Truth(R.And((V)(R.Get(v_30, (V)"binding") != (V)""), () => (V)(R.Get(v_30, (V)"source") != (V)"")))) {
if (R.Truth(R.Invoke(R.Get(v_30, (V)"binding"), "match", R.Join(new V[] {}, new Results((V)"^core:terrain/"))).First)) {
var values_34 = R.Join(new V[] {}, new Results((V)true));
var obj_35 = v_12;
V key_36 = (V)"has_terrain";
R.Set(obj_35, key_36, values_34[0]);
}
if (R.Truth(R.Get(R.Get(v_12, (V)"bindings"), R.Get(v_30, (V)"binding")))) {
var values_37 = R.Join(new V[] {}, new Results(R.Concat((V)"duplicate binding: ", R.Get(v_30, (V)"binding"))));
var obj_38 = v_12;
V key_39 = (V)"status";
R.Set(obj_38, key_39, values_37[0]);
var values_40 = R.Join(new V[] {}, new Results(R.Table()));
var obj_41 = v_12;
V key_42 = (V)"bindings";
R.Set(obj_41, key_42, values_40[0]);
return R.Join(new V[] {}, new Results(v_12));
}
var values_43 = R.Join(new V[] {}, new Results(v_30));
var obj_44 = R.Get(v_12, (V)"bindings");
V key_45 = R.Get(v_30, (V)"binding");
R.Set(obj_44, key_45, values_43[0]);
}
}
var values_46 = R.Join(new V[] {v_8}, new Results(v_9));
var obj_47 = v_12;
V key_48 = (V)"read";
var obj_49 = v_12;
V key_50 = (V)"make_image";
R.Set(obj_47, key_48, values_46[0]);
R.Set(obj_49, key_50, values_46[1]);
var values_51 = R.Join(new V[] {}, new Results((V)"loaded"));
var obj_52 = v_12;
V key_53 = (V)"status";
R.Set(obj_52, key_53, values_51[0]);
var values_54 = R.Join(new V[] {}, new Results(v_21));
var obj_55 = v_12;
V key_56 = (V)"atlas_project";
R.Set(obj_55, key_56, values_54[0]);
var values_57 = R.Join(new V[] {}, new Results((V)"uninspected"));
var obj_58 = v_12;
V key_59 = (V)"atlas_status";
R.Set(obj_58, key_59, values_57[0]);
return R.Join(new V[] {}, new Results(v_12));
return Results.Empty;
})));
R.Set(v_6, "check_atlas", new V((RuleFunction)(args_60 => {
V v_61 = args_60[0];
if (R.Truth((V)(R.Get(v_61, (V)"atlas_status") == (V)"uninspected"))) {
var values_62 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {R.Get(v_61, (V)"read"), R.Get(v_61, (V)"make_image")}, new Results(R.Get(v_61, (V)"atlas_project")))));
var obj_63 = v_61;
V key_64 = (V)"atlas";
var obj_65 = v_61;
V key_66 = (V)"atlas_status";
R.Set(obj_63, key_64, values_62[0]);
R.Set(obj_65, key_66, values_62[1]);
}
return Results.Empty;
})));
R.Set(v_6, "resolve_id", new V((RuleFunction)(args_67 => {
V v_68 = args_67[0];
V v_69 = args_67[1];
if (R.Truth((V)(!R.Truth(R.Get(R.Get(v_68, (V)"by_id"), v_69))))) {
return R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_68, (V)"generated"), () => R.Invoke(R.Get(v_68, (V)"generated"), "resolve", R.Join(new V[] {}, new Results(v_69))).First), () => V.Nil)));
}
R.Invoke(v_68, "check_atlas", Results.Empty);
if (R.Truth(R.Get(v_68, (V)"atlas"))) {
var values_70 = R.Join(new V[] {}, R.Invoke(R.Get(v_68, (V)"atlas"), "resolve", R.Join(new V[] {}, new Results(v_69))));
V v_71 = values_70[0];
if (R.Truth(v_71)) {
return R.Join(new V[] {}, new Results(v_71));
}
}
var values_72 = R.Join(new V[] {}, R.Invoke(v_68, "get_id", R.Join(new V[] {}, new Results(v_69))));
V v_73 = values_72[0];
if (R.Truth((V)(!R.Truth(v_73)))) {
return R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_68, (V)"generated"), () => R.Invoke(R.Get(v_68, (V)"generated"), "resolve", R.Join(new V[] {}, new Results(v_69))).First), () => V.Nil)));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"texture", v_73), new Entry((V)"width", R.Invoke(v_73, "getWidth", Results.Empty).First), new Entry((V)"height", R.Invoke(v_73, "getHeight", Results.Empty).First), new Entry((V)"source", (V)"loose"), new Entry((V)"offset_x", (V)0.0d), new Entry((V)"offset_y", (V)0.0d))));
return Results.Empty;
})));
R.Set(v_6, "resolve", new V((RuleFunction)(args_74 => {
V v_75 = args_74[0];
V v_76 = args_74[1];
var values_77 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_75, (V)"bindings"), v_76)));
V v_78 = values_77[0];
return R.Join(new V[] {}, new Results(R.Or(R.Or(R.And(v_78, () => R.Invoke(v_75, "resolve_id", R.Join(new V[] {}, new Results(R.Get(v_78, (V)"id")))).First), () => R.And(R.Get(v_75, (V)"generated"), () => R.Invoke(R.Get(v_75, (V)"generated"), "resolve", R.Join(new V[] {}, new Results(v_76))).First)), () => V.Nil)));
return Results.Empty;
})));
R.Set(v_6, "get", new V((RuleFunction)(args_79 => {
V v_80 = args_79[0];
V v_81 = args_79[1];
var values_82 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_80, (V)"bindings"), v_81)));
V v_83 = values_82[0];
if (R.Truth((V)(!R.Truth(v_83)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
if (R.Truth(R.Get(R.Get(v_80, (V)"cache"), v_81))) {
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_80, (V)"cache"), v_81)));
}
if (R.Truth(R.Get(R.Get(v_80, (V)"failed"), v_81))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_84 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_80, (V)"read")}, new Results(R.Concat((V)"assets/studio/", R.Get(v_83, (V)"source"))))));
V v_85 = values_84[0];
V v_86 = values_84[1];
if (R.Truth(R.Or((V)(!R.Truth(v_85)), () => (V)(!R.Truth(v_86))))) {
var values_87 = R.Join(new V[] {}, new Results((V)true));
var obj_88 = R.Get(v_80, (V)"failed");
V key_89 = v_81;
R.Set(obj_88, key_89, values_87[0]);

return R.Join(new V[] {}, new Results(V.Nil));
}
var values_90 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"png_dimensions")}, new Results(v_86))));
V v_91 = values_90[0];
V v_92 = values_90[1];
V v_93 = values_90[2];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_91)), () => (V)(v_92 != R.Get(v_83, (V)"width"))), () => (V)(v_93 != R.Get(v_83, (V)"height"))))) {
var values_94 = R.Join(new V[] {}, new Results((V)true));
var obj_95 = R.Get(v_80, (V)"failed");
V key_96 = v_81;
R.Set(obj_95, key_96, values_94[0]);

return R.Join(new V[] {}, new Results(V.Nil));
}
var values_97 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_80, (V)"make_image"), v_86}, new Results(v_83))));
V v_98 = values_97[0];
V v_99 = values_97[1];
V v_100 = values_97[2];
if (R.Truth(R.Or((V)(!R.Truth(v_98)), () => (V)(!R.Truth(v_99))))) {
var values_101 = R.Join(new V[] {}, new Results((V)true));
var obj_102 = R.Get(v_80, (V)"failed");
V key_103 = v_81;
R.Set(obj_102, key_103, values_101[0]);

return R.Join(new V[] {}, new Results(V.Nil));
}
var values_104 = R.Join(new V[] {}, new Results(v_99));
var obj_105 = R.Get(v_80, (V)"cache");
V key_106 = v_81;
R.Set(obj_105, key_106, values_104[0]);
return R.Join(new V[] {}, new Results(v_99));
return Results.Empty;
})));
R.Set(v_6, "get_id", new V((RuleFunction)(args_107 => {
V v_108 = args_107[0];
V v_109 = args_107[1];
var values_110 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_108, (V)"by_id"), v_109)));
V v_111 = values_110[0];
if (R.Truth((V)(!R.Truth(v_111)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_112 = R.Join(new V[] {}, new Results(R.Concat((V)"id:", v_109)));
V v_113 = values_112[0];
if (R.Truth(R.Get(R.Get(v_108, (V)"cache"), v_113))) {
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_108, (V)"cache"), v_113)));
}
if (R.Truth(R.Get(R.Get(v_108, (V)"failed"), v_113))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_114 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_108, (V)"read")}, new Results(R.Concat((V)"assets/studio/", R.Get(v_111, (V)"source"))))));
V v_115 = values_114[0];
V v_116 = values_114[1];
if (R.Truth(R.Or((V)(!R.Truth(v_115)), () => (V)(!R.Truth(v_116))))) {
var values_117 = R.Join(new V[] {}, new Results((V)true));
var obj_118 = R.Get(v_108, (V)"failed");
V key_119 = v_113;
R.Set(obj_118, key_119, values_117[0]);

return R.Join(new V[] {}, new Results(V.Nil));
}
var values_120 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"png_dimensions")}, new Results(v_116))));
V v_121 = values_120[0];
V v_122 = values_120[1];
V v_123 = values_120[2];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_121)), () => (V)(v_122 != R.Get(v_111, (V)"width"))), () => (V)(v_123 != R.Get(v_111, (V)"height"))))) {
var values_124 = R.Join(new V[] {}, new Results((V)true));
var obj_125 = R.Get(v_108, (V)"failed");
V key_126 = v_113;
R.Set(obj_125, key_126, values_124[0]);

return R.Join(new V[] {}, new Results(V.Nil));
}
var values_127 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_108, (V)"make_image"), v_116}, new Results(v_111))));
V v_128 = values_127[0];
V v_129 = values_127[1];
V v_130 = values_127[2];
if (R.Truth(R.Or((V)(!R.Truth(v_128)), () => (V)(!R.Truth(v_129))))) {
var values_131 = R.Join(new V[] {}, new Results((V)true));
var obj_132 = R.Get(v_108, (V)"failed");
V key_133 = v_113;
R.Set(obj_132, key_133, values_131[0]);

return R.Join(new V[] {}, new Results(V.Nil));
}
var values_134 = R.Join(new V[] {}, new Results(v_129));
var obj_135 = R.Get(v_108, (V)"cache");
V key_136 = v_113;
R.Set(obj_135, key_136, values_134[0]);
if (R.Truth(R.Or((V)(R.Get(v_111, (V)"kind") == (V)"bitmap_font"), () => (V)(R.Get(v_111, (V)"kind") == (V)"glyph")))) {
var values_137 = R.Join(new V[] {}, new Results(v_130));
var obj_138 = R.Get(v_108, (V)"image_data");
V key_139 = v_113;
R.Set(obj_138, key_139, values_137[0]);
}
return R.Join(new V[] {}, new Results(v_129));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
