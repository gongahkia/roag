// Static C# port of src/studio_theme.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 890cdd014979127a814ffdf7835ed1de35bccb84f54238e4dcbd352673591922
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_theme { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_project"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", (V)"roag-studio-theme/1"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"hud_panel"), new Entry((V)2, (V)"hp_icon"), new Entry((V)3, (V)"xp_icon"), new Entry((V)4, (V)"currency_icon"), new Entry((V)5, (V)"title_background"), new Entry((V)6, (V)"pause_background"), new Entry((V)7, (V)"bitmap_font"), new Entry((V)8, (V)"hit_effect"), new Entry((V)9, (V)"boon_effect"))));
var obj_8 = v_6;
V key_9 = (V)"slots";
R.Set(obj_8, key_9, values_7[0]);
var values_10 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"hud_panel", R.Table(new Entry((V)"ui", (V)true), new Entry((V)"ui_panel", (V)true))), new Entry((V)"hp_icon", R.Table(new Entry((V)"ui", (V)true), new Entry((V)"ui_icon", (V)true))), new Entry((V)"xp_icon", R.Table(new Entry((V)"ui", (V)true), new Entry((V)"ui_icon", (V)true))), new Entry((V)"currency_icon", R.Table(new Entry((V)"ui", (V)true), new Entry((V)"ui_icon", (V)true))), new Entry((V)"title_background", R.Table(new Entry((V)"background", (V)true), new Entry((V)"ui_background", (V)true))), new Entry((V)"pause_background", R.Table(new Entry((V)"background", (V)true), new Entry((V)"ui_background", (V)true))), new Entry((V)"bitmap_font", R.Table(new Entry((V)"glyph", (V)true), new Entry((V)"bitmap_font", (V)true))), new Entry((V)"hit_effect", R.Table(new Entry((V)"effect", (V)true), new Entry((V)"effect_sprite", (V)true))), new Entry((V)"boon_effect", R.Table(new Entry((V)"effect", (V)true), new Entry((V)"effect_sprite", (V)true))))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, new Results(v_11));
var obj_13 = v_6;
V key_14 = (V)"accepted";
R.Set(obj_13, key_14, values_12[0]);
V v_15 = V.Nil;
v_15 = new V((RuleFunction)(args_16 => {
V v_17 = args_16[0];
V v_18 = args_16[1];
if (R.Truth((V)(!R.Truth(v_17)))) {
R.Call(rt.Global("error"), R.Join(new V[] {v_18}, new Results((V)2.0d)));
}
return Results.Empty;
}));
V v_19 = V.Nil;
v_19 = new V((RuleFunction)(args_20 => {
V v_21 = args_20[0];
V v_22 = args_20[1];
V v_23 = args_20[2];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_21))).First == (V)"number"), () => (V)((V)(v_21 % (V)1.0d) == (V)0.0d)), () => (V)(v_21 >= v_22)), () => (V)(v_21 <= v_23))));
return Results.Empty;
}));
R.Set(v_6, (V)"new", new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
R.Call(v_15, R.Join(new V[] {R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(v_25))).First}, new Results((V)"invalid theme project ID")));
var values_26 = R.Join(new V[] {}, new Results(R.Table()));
V v_27 = values_26[0];

foreach (var row_28 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"slots"))))))) {
V v_29 = row_28[0];
V v_30 = row_28[1];
var values_31 = R.Join(new V[] {}, new Results((V)""));
var obj_32 = v_27;
V key_33 = v_30;
R.Set(obj_32, key_33, values_31[0]);
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", R.Get(v_6, (V)"schema")), new Entry((V)"project_id", v_25), new Entry((V)"name", (V)"Game theme"), new Entry((V)"slots", v_27), new Entry((V)"panel", R.Table(new Entry((V)"left", (V)4.0d), new Entry((V)"right", (V)4.0d), new Entry((V)"top", (V)4.0d), new Entry((V)"bottom", (V)4.0d))), new Entry((V)"font", R.Table(new Entry((V)"characters", (V)"HP0123456789:/"), new Entry((V)"glyph_width", (V)4.0d), new Entry((V)"glyph_height", (V)8.0d), new Entry((V)"advance", (V)5.0d), new Entry((V)"line_height", (V)10.0d), new Entry((V)"spacing", (V)1.0d))), new Entry((V)"background", R.Table(new Entry((V)"mode", (V)"cover"))), new Entry((V)"effects", R.Table(new Entry((V)"hit", R.Table(new Entry((V)"event", (V)"DamageTaken"), new Entry((V)"duration_ms", (V)180.0d), new Entry((V)"scale", (V)1.0d), new Entry((V)"alpha", (V)1.0d), new Entry((V)"attachment", (V)"world"), new Entry((V)"cap", (V)8.0d))), new Entry((V)"boon", R.Table(new Entry((V)"event", (V)"BoonActivated"), new Entry((V)"duration_ms", (V)220.0d), new Entry((V)"scale", (V)1.0d), new Entry((V)"alpha", (V)1.0d), new Entry((V)"attachment", (V)"world"), new Entry((V)"cap", (V)8.0d))))))));
return Results.Empty;
})));
R.Set(v_6, (V)"validate", new V((RuleFunction)(args_34 => {
V v_35 = args_34[0];
V v_36 = args_34[1];
R.Call(v_15, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_35))).First == (V)"table"), () => (V)(R.Get(v_35, (V)"schema") == R.Get(v_6, (V)"schema")))}, new Results((V)"incompatible theme schema")));
R.Call(v_15, R.Join(new V[] {R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"project_id")))).First}, new Results((V)"invalid theme project ID")));
R.Call(v_15, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"name")))).First == (V)"string"), () => (V)((V)R.Length(R.Get(v_35, (V)"name")) > (V)0.0d)), () => (V)((V)R.Length(R.Get(v_35, (V)"name")) <= (V)80.0d)), () => (V)(!R.Truth(R.Invoke(R.Get(v_35, (V)"name"), "find", R.Join(new V[] {}, new Results((V)"[%c]"))).First)))}, new Results((V)"invalid theme name")));
R.Call(v_15, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"slots")))).First == (V)"table")}, new Results((V)"invalid theme slots")));
var values_37 = R.Join(new V[] {}, new Results(R.Table()));
V v_38 = values_37[0];
foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_36, () => R.Table()))))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
var values_42 = R.Join(new V[] {}, new Results(v_41));
var obj_43 = v_38;
V key_44 = R.Get(v_41, (V)"id");
R.Set(obj_43, key_44, values_42[0]);
}
var values_45 = R.Join(new V[] {}, new Results((V)0.0d));
V v_46 = values_45[0];
foreach (var row_47 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"slots"))))))) {
V v_48 = row_47[0];
V v_49 = row_47[1];
R.Call(v_15, R.Join(new V[] {R.Get(v_11, v_48)}, new Results(R.Concat((V)"unknown theme slot: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_48))).First))));

var values_50 = R.Join(new V[] {}, new Results((V)(v_46 + (V)1.0d)));
v_46 = values_50[0];
R.Call(v_15, R.Join(new V[] {R.Or((V)(v_49 == (V)""), () => R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(v_49))).First)}, new Results(R.Concat((V)"invalid asset ID for ", v_48))));
if (R.Truth(R.And(v_36, () => (V)(v_49 != (V)"")))) {
var values_51 = R.Join(new V[] {}, new Results(R.Get(v_38, v_49)));
V v_52 = values_51[0];
R.Call(v_15, R.Join(new V[] {R.And(R.And(v_52, () => R.Get(R.Get(v_11, v_48), R.Get(v_52, (V)"kind"))), () => R.Or((V)(R.Get(v_52, (V)"source") != (V)""), () => (V)(R.Get(v_52, (V)"derived") != (V)"")))}, new Results(R.Concat((V)"missing, empty or wrong-kind asset for ", R.Concat(v_48, R.Concat((V)": ", v_49))))));
R.Call(v_15, R.Join(new V[] {R.And(R.Call(v_19, R.Join(new V[] {R.Get(v_52, (V)"width"), (V)1.0d}, new Results((V)1024.0d))).First, () => R.Call(v_19, R.Join(new V[] {R.Get(v_52, (V)"height"), (V)1.0d}, new Results((V)1024.0d))).First)}, new Results(R.Concat((V)"invalid dimensions for ", v_49))));
}
}
R.Call(v_15, R.Join(new V[] {(V)(v_46 == (V)R.Length(R.Get(v_6, (V)"slots")))}, new Results((V)"missing theme slots")));
var values_53 = R.Join(new V[] {}, new Results(R.Get(v_35, (V)"panel")));
V v_54 = values_53[0];
R.Call(v_15, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_54))).First == (V)"table")}, new Results((V)"missing panel insets")));
foreach (var row_55 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"left"), new Entry((V)2, (V)"right"), new Entry((V)3, (V)"top"), new Entry((V)4, (V)"bottom")))))))) {
V v_56 = row_55[0];
V v_57 = row_55[1];
R.Call(v_15, R.Join(new V[] {R.Call(v_19, R.Join(new V[] {R.Get(v_54, v_57), (V)0.0d}, new Results((V)256.0d))).First}, new Results(R.Concat((V)"invalid panel ", R.Concat(v_57, (V)" inset")))));
}
if (R.Truth(R.And(v_36, () => (V)(R.Get(R.Get(v_35, (V)"slots"), (V)"hud_panel") != (V)"")))) {
var values_58 = R.Join(new V[] {}, new Results(R.Get(v_38, R.Get(R.Get(v_35, (V)"slots"), (V)"hud_panel"))));
V v_59 = values_58[0];
R.Call(v_15, R.Join(new V[] {R.And((V)((V)(R.Get(v_54, (V)"left") + R.Get(v_54, (V)"right")) <= R.Get(v_59, (V)"width")), () => (V)((V)(R.Get(v_54, (V)"top") + R.Get(v_54, (V)"bottom")) <= R.Get(v_59, (V)"height")))}, new Results((V)"nine-slice insets exceed panel image")));
}
var values_60 = R.Join(new V[] {}, new Results(R.Get(v_35, (V)"font")));
V v_61 = values_60[0];
R.Call(v_15, R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_61))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_61, (V)"characters")))).First == (V)"string")), () => (V)((V)R.Length(R.Get(v_61, (V)"characters")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_61, (V)"characters")) <= (V)96.0d))}, new Results((V)"invalid glyph mapping")));
var values_62 = R.Join(new V[] {}, new Results(R.Table()));
V v_63 = values_62[0];
{ double end_66 = R.Num((V)R.Length(R.Get(v_61, (V)"characters"))), step_67 = R.Num((V)1.0d);
for (double i_65 = R.Num((V)1.0d); step_67 > 0 ? i_65 <= end_66 : i_65 >= end_66; i_65 += step_67) {
V v_64 = i_65;
var values_68 = R.Join(new V[] {}, R.Invoke(R.Get(v_61, (V)"characters"), "byte", R.Join(new V[] {}, new Results(v_64))));
V v_69 = values_68[0];
R.Call(v_15, R.Join(new V[] {R.And(R.And((V)(v_69 >= (V)32.0d), () => (V)(v_69 <= (V)126.0d)), () => (V)(!R.Truth(R.Get(v_63, v_69))))}, new Results((V)"duplicate or unsupported glyph")));
var values_70 = R.Join(new V[] {}, new Results((V)true));
var obj_71 = v_63;
V key_72 = v_69;
R.Set(obj_71, key_72, values_70[0]);
}
}
R.Call(v_15, R.Join(new V[] {R.And(R.And(R.And(R.And(R.Call(v_19, R.Join(new V[] {R.Get(v_61, (V)"glyph_width"), (V)1.0d}, new Results((V)128.0d))).First, () => R.Call(v_19, R.Join(new V[] {R.Get(v_61, (V)"glyph_height"), (V)1.0d}, new Results((V)128.0d))).First), () => R.Call(v_19, R.Join(new V[] {R.Get(v_61, (V)"advance"), (V)1.0d}, new Results((V)256.0d))).First), () => R.Call(v_19, R.Join(new V[] {R.Get(v_61, (V)"line_height"), (V)1.0d}, new Results((V)256.0d))).First), () => R.Call(v_19, R.Join(new V[] {R.Get(v_61, (V)"spacing"), (V)0.0d}, new Results((V)64.0d))).First)}, new Results((V)"invalid bitmap font metrics")));
if (R.Truth(R.And(v_36, () => (V)(R.Get(R.Get(v_35, (V)"slots"), (V)"bitmap_font") != (V)"")))) {
var values_73 = R.Join(new V[] {}, new Results(R.Get(v_38, R.Get(R.Get(v_35, (V)"slots"), (V)"bitmap_font"))));
V v_74 = values_73[0];
var values_75 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_74, (V)"width") / R.Get(v_61, (V)"glyph_width"))))));
V v_76 = values_75[0];
var values_77 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_74, (V)"height") / R.Get(v_61, (V)"glyph_height"))))));
V v_78 = values_77[0];
R.Call(v_15, R.Join(new V[] {R.And(R.And((V)(v_76 > (V)0.0d), () => (V)(v_78 > (V)0.0d)), () => (V)((V)(v_76 * v_78) >= (V)R.Length(R.Get(v_61, (V)"characters"))))}, new Results((V)"glyph mapping exceeds sheet dimensions")));
}
R.Call(v_15, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"background")))).First == (V)"table"), () => R.Get(R.Table(new Entry((V)"center", (V)true), new Entry((V)"fit", (V)true), new Entry((V)"cover", (V)true)), R.Get(R.Get(v_35, (V)"background"), (V)"mode")))}, new Results((V)"invalid background mode")));
R.Call(v_15, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"effects")))).First == (V)"table")}, new Results((V)"missing effect presets")));
foreach (var row_79 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"hit", (V)"DamageTaken"), new Entry((V)"boon", (V)"BoonActivated")))))))) {
V v_80 = row_79[0];
V v_81 = row_79[1];
var values_82 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_35, (V)"effects"), v_80)));
V v_83 = values_82[0];
R.Call(v_15, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_83))).First == (V)"table"), () => (V)(R.Get(v_83, (V)"event") == v_81))}, new Results(R.Concat((V)"invalid effect event: ", v_80))));
R.Call(v_15, R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.Call(v_19, R.Join(new V[] {R.Get(v_83, (V)"duration_ms"), (V)40.0d}, new Results((V)1000.0d))).First, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_83, (V)"scale")))).First == (V)"number")), () => (V)(R.Get(v_83, (V)"scale") >= (V)0.25d)), () => (V)(R.Get(v_83, (V)"scale") <= (V)4.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_83, (V)"alpha")))).First == (V)"number")), () => (V)(R.Get(v_83, (V)"alpha") >= (V)0.0d)), () => (V)(R.Get(v_83, (V)"alpha") <= (V)1.0d)), () => R.Call(v_19, R.Join(new V[] {R.Get(v_83, (V)"cap"), (V)1.0d}, new Results((V)32.0d))).First), () => (V)(R.Get(v_83, (V)"attachment") == (V)"world"))}, new Results(R.Concat((V)"invalid effect preset: ", v_80))));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_6, (V)"encode", new V((RuleFunction)(args_84 => {
V v_85 = args_84[0];
R.Call(R.Get(v_6, (V)"validate"), R.Join(new V[] {}, new Results(v_85)));
var values_86 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"encode"), R.Join(new V[] {}, new Results(v_85))));
V v_87 = values_86[0];
R.Call(v_15, R.Join(new V[] {(V)((V)R.Length(v_87) <= (V)65536.0d)}, new Results((V)"theme is too large")));
return R.Join(new V[] {}, new Results(v_87));
return Results.Empty;
})));
R.Set(v_6, (V)"decode", new V((RuleFunction)(args_88 => {
V v_89 = args_88[0];
R.Call(v_15, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_89))).First == (V)"string"), () => (V)((V)R.Length(v_89) <= (V)65536.0d))}, new Results((V)"invalid theme size")));
var values_90 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"decode"), R.Join(new V[] {}, new Results(v_89))));
V v_91 = values_90[0];
R.Call(R.Get(v_6, (V)"validate"), R.Join(new V[] {}, new Results(v_91)));
return R.Join(new V[] {}, new Results(v_91));
return Results.Empty;
})));
R.Set(v_6, (V)"nine_slice", new V((RuleFunction)(args_92 => {
V v_93 = args_92[0];
V v_94 = args_92[1];
V v_95 = args_92[2];
V v_96 = args_92[3];
V v_97 = args_92[4];
R.Call(v_15, R.Join(new V[] {R.And(R.And(R.And(R.Call(v_19, R.Join(new V[] {v_93, (V)1.0d}, new Results((V)4096.0d))).First, () => R.Call(v_19, R.Join(new V[] {v_94, (V)1.0d}, new Results((V)4096.0d))).First), () => R.Call(v_19, R.Join(new V[] {v_96, (V)1.0d}, new Results((V)4096.0d))).First), () => R.Call(v_19, R.Join(new V[] {v_97, (V)1.0d}, new Results((V)4096.0d))).First)}, new Results((V)"invalid panel dimensions")));
var values_98 = R.Join(new V[] {R.Get(v_95, (V)"left"), R.Get(v_95, (V)"right"), R.Get(v_95, (V)"top")}, new Results(R.Get(v_95, (V)"bottom")));
V v_99 = values_98[0];
V v_100 = values_98[1];
V v_101 = values_98[2];
V v_102 = values_98[3];
R.Call(v_15, R.Join(new V[] {R.And((V)((V)(v_99 + v_100) <= v_93), () => (V)((V)(v_101 + v_102) <= v_94))}, new Results((V)"panel insets exceed image")));
var values_103 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d, (V)(v_96 / R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, new Results((V)(v_99 + v_100)))).First)}, new Results((V)(v_97 / R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)1.0d}, new Results((V)(v_101 + v_102)))).First)))));
V v_104 = values_103[0];
var values_105 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)(v_99 * v_104)), new Entry((V)3, (V)(v_96 - (V)(v_100 * v_104))), new Entry((V)4, v_96))));
V v_106 = values_105[0];
var values_107 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)(v_101 * v_104)), new Entry((V)3, (V)(v_97 - (V)(v_102 * v_104))), new Entry((V)4, v_97))));
V v_108 = values_107[0];
var values_109 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, v_99), new Entry((V)3, (V)(v_93 - v_100)), new Entry((V)4, v_93))));
V v_110 = values_109[0];

var values_111 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, v_101), new Entry((V)3, (V)(v_94 - v_102)), new Entry((V)4, v_94))));
V v_112 = values_111[0];
var values_113 = R.Join(new V[] {}, new Results(R.Table()));
V v_114 = values_113[0];
{ double end_117 = R.Num((V)3.0d), step_118 = R.Num((V)1.0d);
for (double i_116 = R.Num((V)1.0d); step_118 > 0 ? i_116 <= end_117 : i_116 >= end_117; i_116 += step_118) {
V v_115 = i_116;
{ double end_121 = R.Num((V)3.0d), step_122 = R.Num((V)1.0d);
for (double i_120 = R.Num((V)1.0d); step_122 > 0 ? i_120 <= end_121 : i_120 >= end_121; i_120 += step_122) {
V v_119 = i_120;
var values_123 = R.Join(new V[] {(V)(R.Get(v_110, (V)(v_119 + (V)1.0d)) - R.Get(v_110, v_119))}, new Results((V)(R.Get(v_112, (V)(v_115 + (V)1.0d)) - R.Get(v_112, v_115))));
V v_124 = values_123[0];
V v_125 = values_123[1];
var values_126 = R.Join(new V[] {(V)(R.Get(v_106, (V)(v_119 + (V)1.0d)) - R.Get(v_106, v_119))}, new Results((V)(R.Get(v_108, (V)(v_115 + (V)1.0d)) - R.Get(v_108, v_115))));
V v_127 = values_126[0];
V v_128 = values_126[1];
if (R.Truth(R.And(R.And(R.And((V)(v_124 > (V)0.0d), () => (V)(v_125 > (V)0.0d)), () => (V)(v_127 > (V)0.0d)), () => (V)(v_128 > (V)0.0d)))) {
var values_129 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"sx", R.Get(v_110, v_119)), new Entry((V)"sy", R.Get(v_112, v_115)), new Entry((V)"sw", v_124), new Entry((V)"sh", v_125), new Entry((V)"x", R.Get(v_106, v_119)), new Entry((V)"y", R.Get(v_108, v_115)), new Entry((V)"w", v_127), new Entry((V)"h", v_128))));
var obj_130 = v_114;
V key_131 = (V)((V)R.Length(v_114) + (V)1.0d);
R.Set(obj_130, key_131, values_129[0]);
}
}
}
}
}
return R.Join(new V[] {}, new Results(v_114));
return Results.Empty;
})));
R.Set(v_6, (V)"background_rect", new V((RuleFunction)(args_132 => {
V v_133 = args_132[0];
V v_134 = args_132[1];
V v_135 = args_132[2];
V v_136 = args_132[3];
V v_137 = args_132[4];
R.Call(v_15, R.Join(new V[] {R.And(R.And(R.And(R.And(R.Get(R.Table(new Entry((V)"center", (V)true), new Entry((V)"fit", (V)true), new Entry((V)"cover", (V)true)), v_133), () => (V)(v_134 > (V)0.0d)), () => (V)(v_135 > (V)0.0d)), () => (V)(v_136 > (V)0.0d)), () => (V)(v_137 > (V)0.0d))}, new Results((V)"invalid background geometry")));
var values_138 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_133 == (V)"center"), () => (V)1.0d), () => R.And((V)(v_133 == (V)"fit"), () => R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)(v_136 / v_134)}, new Results((V)(v_137 / v_135)))).First)), () => R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)(v_136 / v_134)}, new Results((V)(v_137 / v_135)))).First)));
V v_139 = values_138[0];
return R.Join(new V[] {(V)((V)(v_136 - (V)(v_134 * v_139)) / (V)2.0d), (V)((V)(v_137 - (V)(v_135 * v_139)) / (V)2.0d), (V)(v_134 * v_139)}, new Results((V)(v_135 * v_139)));
return Results.Empty;
})));
R.Set(v_6, (V)"glyphs", new V((RuleFunction)(args_140 => {
V v_141 = args_140[0];
V v_142 = args_140[1];
V v_143 = args_140[2];
var values_144 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_142 / R.Get(v_141, (V)"glyph_width"))))));
V v_145 = values_144[0];
var values_146 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_143 / R.Get(v_141, (V)"glyph_height"))))));
V v_147 = values_146[0];
R.Call(v_15, R.Join(new V[] {R.And(R.And((V)(v_145 > (V)0.0d), () => (V)(v_147 > (V)0.0d)), () => (V)((V)(v_145 * v_147) >= (V)R.Length(R.Get(v_141, (V)"characters"))))}, new Results((V)"glyph mapping exceeds sheet")));
var values_148 = R.Join(new V[] {}, new Results(R.Table()));
V v_149 = values_148[0];
{ double end_152 = R.Num((V)R.Length(R.Get(v_141, (V)"characters"))), step_153 = R.Num((V)1.0d);
for (double i_151 = R.Num((V)1.0d); step_153 > 0 ? i_151 <= end_152 : i_151 >= end_152; i_151 += step_153) {
V v_150 = i_151;
var values_154 = R.Join(new V[] {}, R.Invoke(R.Get(v_141, (V)"characters"), "sub", R.Join(new V[] {v_150}, new Results(v_150))));
V v_155 = values_154[0];
var values_156 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)((V)((V)(v_150 - (V)1.0d) % v_145) * R.Get(v_141, (V)"glyph_width"))), new Entry((V)"y", (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_150 - (V)1.0d) / v_145)))).First * R.Get(v_141, (V)"glyph_height"))), new Entry((V)"w", R.Get(v_141, (V)"glyph_width")), new Entry((V)"h", R.Get(v_141, (V)"glyph_height")))));
var obj_157 = v_149;
V key_158 = v_155;
R.Set(obj_157, key_158, values_156[0]);
}
}
return R.Join(new V[] {}, new Results(v_149));
return Results.Empty;
})));
R.Set(v_6, (V)"effect_for", new V((RuleFunction)(args_159 => {
V v_160 = args_159[0];
V v_161 = args_159[1];
V v_162 = args_159[2];
if (R.Truth(R.Or(R.Or(R.Or((V)(!R.Truth(v_162)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_161))).First != (V)"table")), () => (V)(!R.Truth(R.Get(v_161, (V)"x")))), () => (V)(!R.Truth(R.Get(v_161, (V)"y")))))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_163 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(R.Get(v_161, (V)"kind") == (V)"DamageTaken"), () => (V)"hit"), () => R.And((V)(R.Get(v_161, (V)"kind") == (V)"BoonActivated"), () => (V)"boon")), () => V.Nil)));
V v_164 = values_163[0];
if (R.Truth((V)(!R.Truth(v_164)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_165 = R.Join(new V[] {}, new Results(R.Concat(v_164, (V)"_effect")));
V v_166 = values_165[0];
if (R.Truth((V)(R.Get(R.Get(v_160, (V)"slots"), v_166) == (V)""))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
return R.Join(new V[] {R.Get(R.Get(v_160, (V)"effects"), v_164)}, new Results(R.Get(R.Get(v_160, (V)"slots"), v_166)));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
