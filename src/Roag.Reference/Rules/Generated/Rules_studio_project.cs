// Static C# port of src/studio_project.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 8707959728d5f993a9425dbbf7f57813ee644c2d195670f893dcdf3ab725ff02
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_project { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", (V)"roag-studio-project/1"), new Entry((V)"max_image_bytes", (V)((V)((V)8.0d * (V)1024.0d) * (V)1024.0d)))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"bit"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_document"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visual_slots"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table()));
V v_10 = values_9[0];
{ double end_13 = R.Num((V)255.0d), step_14 = R.Num((V)1.0d);
for (double i_12 = R.Num((V)0.0d); step_14 > 0 ? i_12 <= end_13 : i_12 >= end_13; i_12 += step_14) {
V v_11 = i_12;
var values_15 = R.Join(new V[] {}, new Results(v_11));
V v_16 = values_15[0];
{ double end_19 = R.Num((V)8.0d), step_20 = R.Num((V)1.0d);
for (double i_18 = R.Num((V)1.0d); step_20 > 0 ? i_18 <= end_19 : i_18 >= end_19; i_18 += step_20) {
V v_17 = i_18;
var values_21 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Call(R.Get(v_4, (V)"band"), R.Join(new V[] {v_16}, new Results((V)1.0d))).First == (V)1.0d), () => R.Call(R.Get(v_4, (V)"bxor"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"rshift"), R.Join(new V[] {v_16}, new Results((V)1.0d))).First}, new Results((V)3988292384.0d))).First), () => R.Call(R.Get(v_4, (V)"rshift"), R.Join(new V[] {v_16}, new Results((V)1.0d))).First)));
v_16 = values_21[0];
}
}
var values_22 = R.Join(new V[] {}, new Results(v_16));
var obj_23 = v_10;
V key_24 = v_11;
R.Set(obj_23, key_24, values_22[0]);
}
}
var values_25 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"actor", (V)true), new Entry((V)"enemy", (V)true), new Entry((V)"tile", (V)true), new Entry((V)"effect", (V)true), new Entry((V)"ui", (V)true), new Entry((V)"background", (V)true), new Entry((V)"glyph", (V)true), new Entry((V)"shader", (V)true), new Entry((V)"ui_icon", (V)true), new Entry((V)"ui_panel", (V)true), new Entry((V)"ui_background", (V)true), new Entry((V)"bitmap_font", (V)true), new Entry((V)"effect_sprite", (V)true))));
V v_26 = values_25[0];
var values_27 = R.Join(new V[] {}, new Results(R.Get(v_8, (V)"bindings")));
var obj_28 = v_2;
V key_29 = (V)"bindings";
R.Set(obj_28, key_29, values_27[0]);
V v_30 = V.Nil;
v_30 = new V((RuleFunction)(args_31 => {
V v_32 = args_31[0];
V v_33 = args_31[1];
if (R.Truth((V)(!R.Truth(v_32)))) {
R.Call(rt.Global("error"), R.Join(new V[] {v_33}, new Results((V)2.0d)));
}
return Results.Empty;
}));
V v_34 = V.Nil;
v_34 = new V((RuleFunction)(args_35 => {
V v_36 = args_35[0];
V v_37 = args_35[1];
V v_38 = args_35[2];
R.Call(v_30, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_36))).First == (V)"string"), () => (V)((V)R.Length(v_36) <= v_38)), () => (V)(!R.Truth(R.Invoke(v_36, "find", R.Join(new V[] {}, new Results((V)"%z"))).First)))}, new Results(R.Concat(v_37, (V)" must be text"))));
return Results.Empty;
}));
R.Set(v_2, (V)"valid_id", new V((RuleFunction)(args_39 => {
V v_40 = args_39[0];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_40))).First == (V)"string"), () => (V)((V)R.Length(v_40) <= (V)100.0d)), () => (V)(R.Invoke(v_40, "match", R.Join(new V[] {}, new Results((V)"^[a-z][a-z0-9_-]*:[a-z][a-z0-9_/-]*$"))).First != V.Nil)), () => (V)(R.Invoke(v_40, "sub", R.Join(new V[] {}, new Results((-(V)1.0d)))).First != (V)"/")), () => (V)(!R.Truth(R.Invoke(v_40, "find", R.Join(new V[] {(V)"//", (V)1.0d}, new Results((V)true))).First))), () => (V)(!R.Truth(R.Invoke(v_40, "find", R.Join(new V[] {}, new Results((V)"/%."))).First)))));
return Results.Empty;
})));
R.Set(v_2, (V)"valid_project_id", new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
return R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_42))).First == (V)"string"), () => (V)((V)R.Length(v_42) <= (V)48.0d)), () => (V)(R.Invoke(v_42, "match", R.Join(new V[] {}, new Results((V)"^[a-z][a-z0-9_-]*$"))).First != V.Nil))));
return Results.Empty;
})));
R.Set(v_2, (V)"valid_path", new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_44))).First != (V)"string"), () => (V)((V)R.Length(v_44) == (V)0.0d)), () => (V)((V)R.Length(v_44) > (V)180.0d)), () => R.Invoke(v_44, "find", R.Join(new V[] {}, new Results((V)"[\\:%z]"))).First), () => (V)(R.Invoke(v_44, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)1.0d))).First == (V)"/")), () => R.Invoke(v_44, "find", R.Join(new V[] {(V)"//", (V)1.0d}, new Results((V)true))).First))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_45 in R.Iterate(R.Join(new V[] {}, R.Invoke(v_44, "gmatch", R.Join(new V[] {}, new Results((V)"[^/]+")))))) {
V v_46 = row_45[0];
if (R.Truth(R.Or(R.Or((V)(v_46 == (V)"."), () => (V)(v_46 == (V)"..")), () => (V)(!R.Truth(R.Invoke(v_46, "match", R.Join(new V[] {}, new Results((V)"^[%w_.-]+$"))).First))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, (V)"new", new V((RuleFunction)(args_47 => {
V v_48 = args_47[0];
V v_49 = args_47[1];
R.Call(v_30, R.Join(new V[] {R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(v_48))).First}, new Results((V)"invalid project ID")));
R.Call(v_34, R.Join(new V[] {v_49, (V)"project name"}, new Results((V)80.0d)));
R.Call(v_30, R.Join(new V[] {(V)((V)R.Length(v_49) > (V)0.0d)}, new Results((V)"project name required")));
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schema", R.Get(v_2, (V)"schema")), new Entry((V)"id", v_48), new Entry((V)"name", v_49), new Entry((V)"assets", R.Table()))));
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
V v_52 = args_50[1];
V v_53 = args_50[2];
R.Call(v_30, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_51))).First == (V)"table"), () => (V)(R.Get(v_51, (V)"schema") == R.Get(v_2, (V)"schema")))}, new Results((V)"incompatible project schema")));
R.Call(v_30, R.Join(new V[] {R.Call(R.Get(v_2, (V)"valid_project_id"), R.Join(new V[] {}, new Results(R.Get(v_51, (V)"id")))).First}, new Results((V)"invalid project ID")));
R.Call(v_34, R.Join(new V[] {R.Get(v_51, (V)"name"), (V)"project name"}, new Results((V)80.0d)));
R.Call(v_30, R.Join(new V[] {(V)((V)R.Length(R.Get(v_51, (V)"name")) > (V)0.0d)}, new Results((V)"project name required")));
R.Call(v_30, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_51, (V)"assets")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_51, (V)"assets")) <= (V)256.0d))}, new Results((V)"invalid asset list")));
var values_54 = R.Join(new V[] {R.Table(), R.Table()}, new Results(R.Table()));
V v_55 = values_54[0];
V v_56 = values_54[1];
V v_57 = values_54[2];
var values_58 = R.Join(new V[] {}, new Results((V)0.0d));
V v_59 = values_58[0];
var values_60 = R.Join(new V[] {}, new Results((V)0.0d));
V v_61 = values_60[0];
foreach (var row_62 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_51, (V)"assets"))))))) {
V v_63 = row_62[0];
V v_64 = row_62[1];
R.Call(v_30, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_64))).First == (V)"table"), () => R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"id")))).First)}, new Results((V)"invalid asset ID")));
R.Call(v_30, R.Join(new V[] {(V)(!R.Truth(R.Get(v_55, R.Get(v_64, (V)"id"))))}, new Results(R.Concat((V)"duplicate asset ID: ", R.Get(v_64, (V)"id")))));

var values_65 = R.Join(new V[] {}, new Results(v_64));
var obj_66 = v_55;
V key_67 = R.Get(v_64, (V)"id");
R.Set(obj_66, key_67, values_65[0]);
R.Call(v_34, R.Join(new V[] {R.Get(v_64, (V)"name"), (V)"asset name"}, new Results((V)80.0d)));
R.Call(v_30, R.Join(new V[] {(V)((V)R.Length(R.Get(v_64, (V)"name")) > (V)0.0d)}, new Results((V)"asset name required")));
R.Call(v_30, R.Join(new V[] {R.Get(v_26, R.Get(v_64, (V)"kind"))}, new Results((V)"unsupported asset kind")));
R.Call(v_30, R.Join(new V[] {R.Or((V)(R.Get(v_64, (V)"binding") == (V)""), () => R.Get(R.Get(v_2, (V)"bindings"), R.Get(v_64, (V)"binding")))}, new Results((V)"unknown game binding")));
if (R.Truth((V)(R.Get(v_64, (V)"binding") != (V)""))) {
R.Call(v_30, R.Join(new V[] {(V)(!R.Truth(R.Get(v_57, R.Get(v_64, (V)"binding"))))}, new Results(R.Concat((V)"duplicate game binding: ", R.Get(v_64, (V)"binding")))));
var values_68 = R.Join(new V[] {}, new Results((V)true));
var obj_69 = v_57;
V key_70 = R.Get(v_64, (V)"binding");
R.Set(obj_69, key_70, values_68[0]);
}
R.Call(v_30, R.Join(new V[] {R.Or((V)(R.Get(v_64, (V)"filter") == (V)"nearest"), () => (V)(R.Get(v_64, (V)"filter") == (V)"linear"))}, new Results((V)"invalid sampling filter")));
R.Call(v_34, R.Join(new V[] {R.Or(R.Get(v_64, (V)"description"), () => (V)""), (V)"description"}, new Results((V)300.0d)));
R.Call(v_34, R.Join(new V[] {R.Or(R.Get(v_64, (V)"atlas_group"), () => (V)""), (V)"atlas group"}, new Results((V)60.0d)));
R.Call(v_34, R.Join(new V[] {R.Or(R.Get(v_64, (V)"frame_hint"), () => (V)""), (V)"frame hint"}, new Results((V)120.0d)));
R.Call(v_34, R.Join(new V[] {R.Or(R.Get(v_64, (V)"animation_hint"), () => (V)""), (V)"animation hint"}, new Results((V)120.0d)));
var values_71 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_64, (V)"reference_id"), () => (V)"")));
var obj_72 = v_64;
V key_73 = (V)"reference_id";
R.Set(obj_72, key_73, values_71[0]);
R.Call(v_30, R.Join(new V[] {R.Or((V)(R.Get(v_64, (V)"reference_id") == (V)""), () => R.Call(R.Get(v_2, (V)"valid_id"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"reference_id")))).First)}, new Results((V)"invalid reference asset ID")));
R.Call(v_30, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"tags")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_64, (V)"tags")) <= (V)20.0d))}, new Results((V)"invalid tags")));
foreach (var row_74 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"tags"))))))) {
V v_75 = row_74[0];
V v_76 = row_74[1];
R.Call(v_30, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_76))).First == (V)"string"), () => R.Invoke(v_76, "match", R.Join(new V[] {}, new Results((V)"^[a-z0-9_-]+$"))).First), () => (V)((V)R.Length(v_76) <= (V)32.0d))}, new Results((V)"invalid tag")));
}
var values_77 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_64, (V)"document"), () => (V)"")));
var obj_78 = v_64;
V key_79 = (V)"document";
R.Set(obj_78, key_79, values_77[0]);

var values_80 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_64, (V)"derived"), () => (V)"")));
var obj_81 = v_64;
V key_82 = (V)"derived";
R.Set(obj_81, key_82, values_80[0]);
var values_83 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_64, (V)"outline_color"), () => (V)"")));
var obj_84 = v_64;
V key_85 = (V)"outline_color";
R.Set(obj_84, key_85, values_83[0]);
R.Call(v_30, R.Join(new V[] {R.Or((V)(R.Get(v_64, (V)"outline_color") == (V)""), () => R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"outline_color")))).First == (V)"string"), () => R.Invoke(R.Get(v_64, (V)"outline_color"), "match", R.Join(new V[] {}, new Results((V)"^%x%x%x%x%x%x%x%x$"))).First))}, new Results((V)"invalid outline color")));
R.Call(v_30, R.Join(new V[] {R.Or((V)(R.Get(v_64, (V)"outline_color") == (V)""), () => (V)(R.Get(v_64, (V)"document") != (V)""))}, new Results((V)"outline requires an editable document")));
R.Call(v_30, R.Join(new V[] {R.Or((V)(R.Get(v_64, (V)"source") == (V)""), () => R.Call(R.Get(v_2, (V)"valid_path"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"source")))).First)}, new Results((V)"unsafe source path")));
R.Call(v_30, R.Join(new V[] {R.Or((V)(R.Get(v_64, (V)"document") == (V)""), () => R.And(R.Call(R.Get(v_2, (V)"valid_path"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"document")))).First, () => R.Invoke(R.Get(v_64, (V)"document"), "match", R.Join(new V[] {}, new Results((V)"%.rgba$"))).First))}, new Results((V)"unsafe document path")));
R.Call(v_30, R.Join(new V[] {R.Or((V)(R.Get(v_64, (V)"derived") == (V)""), () => R.And(R.Call(R.Get(v_2, (V)"valid_path"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"derived")))).First, () => R.Invoke(R.Get(v_64, (V)"derived"), "match", R.Join(new V[] {}, new Results((V)"%.png$"))).First))}, new Results((V)"unsafe derived path")));
R.Call(v_30, R.Join(new V[] {(V)((V)(R.Get(v_64, (V)"document") == (V)"") == (V)(R.Get(v_64, (V)"derived") == (V)""))}, new Results((V)"document and derived image must be paired")));
if (R.Truth((V)(R.Get(v_64, (V)"source") == (V)""))) {
if (R.Truth((V)(R.Get(v_64, (V)"document") == (V)""))) {
R.Call(v_30, R.Join(new V[] {R.And(R.And((V)(R.Get(v_64, (V)"format") == (V)""), () => (V)(R.Get(v_64, (V)"width") == (V)0.0d)), () => (V)(R.Get(v_64, (V)"height") == (V)0.0d))}, new Results((V)"draft has image metadata")));
}
} else {
R.Call(v_30, R.Join(new V[] {R.And((V)(R.Get(v_64, (V)"format") == (V)"png"), () => R.Invoke(R.Invoke(R.Get(v_64, (V)"source"), "lower", Results.Empty).First, "match", R.Join(new V[] {}, new Results((V)"%.png$"))).First)}, new Results((V)"unsupported source format")));
R.Call(v_30, R.Join(new V[] {(V)(!R.Truth(R.Get(v_56, R.Get(v_64, (V)"source"))))}, new Results((V)"duplicate source path")));

var values_86 = R.Join(new V[] {}, new Results((V)true));
var obj_87 = v_56;
V key_88 = R.Get(v_64, (V)"source");
R.Set(obj_87, key_88, values_86[0]);
}
if (R.Truth(R.Or((V)(R.Get(v_64, (V)"source") != (V)""), () => (V)(R.Get(v_64, (V)"document") != (V)"")))) {
R.Call(v_30, R.Join(new V[] {(V)(R.Get(v_64, (V)"format") == (V)"png")}, new Results((V)"unsupported source format")));
R.Call(v_30, R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"width")))).First == (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"height")))).First == (V)"number")), () => (V)((V)(R.Get(v_64, (V)"width") % (V)1.0d) == (V)0.0d)), () => (V)((V)(R.Get(v_64, (V)"height") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_64, (V)"width") >= (V)1.0d)), () => (V)(R.Get(v_64, (V)"height") >= (V)1.0d)), () => (V)(R.Get(v_64, (V)"width") <= (V)1024.0d)), () => (V)(R.Get(v_64, (V)"height") <= (V)1024.0d)), () => (V)((V)(R.Get(v_64, (V)"width") * R.Get(v_64, (V)"height")) <= (V)1048576.0d))}, new Results((V)"invalid image dimensions")));
R.Call(v_30, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_64, (V)"transparent")))).First == (V)"boolean")}, new Results((V)"invalid alpha intent")));
if (R.Truth(R.And(v_52, () => (V)(R.Get(v_64, (V)"source") != (V)"")))) {
var values_89 = R.Join(new V[] {}, R.Call(v_52, R.Join(new V[] {}, new Results(R.Get(v_64, (V)"source")))));
V v_90 = values_89[0];
R.Call(v_30, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_90))).First == (V)"string")}, new Results(R.Concat((V)"missing source image: ", R.Get(v_64, (V)"source")))));
var values_91 = R.Join(new V[] {}, new Results((V)(v_59 + (V)R.Length(v_90))));
v_59 = values_91[0];
R.Call(v_30, R.Join(new V[] {(V)(v_59 <= (V)((V)((V)64.0d * (V)1024.0d) * (V)1024.0d))}, new Results((V)"project source size limit")));
var values_92 = R.Join(new V[] {}, new Results((V)(v_61 + (V)R.Length(v_90))));
v_61 = values_92[0];
var values_93 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"png_dimensions"), R.Join(new V[] {}, new Results(v_90))));
V v_94 = values_93[0];
V v_95 = values_93[1];
R.Call(v_30, R.Join(new V[] {R.And((V)(v_94 == R.Get(v_64, (V)"width")), () => (V)(v_95 == R.Get(v_64, (V)"height")))}, new Results(R.Concat((V)"source dimensions changed: ", R.Get(v_64, (V)"source")))));
if (R.Truth(v_53)) {
R.Call(v_53, R.Join(new V[] {v_90, R.Get(v_64, (V)"source"), v_94}, new Results(v_95)));
}
}
}
if (R.Truth((V)(R.Get(v_64, (V)"document") != (V)""))) {
R.Call(v_30, R.Join(new V[] {R.And((V)(!R.Truth(R.Get(v_56, R.Get(v_64, (V)"document")))), () => (V)(!R.Truth(R.Get(v_56, R.Get(v_64, (V)"derived")))))}, new Results((V)"duplicate document path")));
var values_96 = R.Join(new V[] {}, new Results((V)true));
var obj_97 = v_56;
V key_98 = R.Get(v_64, (V)"document");
R.Set(obj_97, key_98, values_96[0]);

var values_99 = R.Join(new V[] {}, new Results((V)true));
var obj_100 = v_56;
V key_101 = R.Get(v_64, (V)"derived");
R.Set(obj_100, key_101, values_99[0]);
R.Call(v_30, R.Join(new V[] {R.And((V)(R.Get(v_64, (V)"width") <= R.Get(v_6, (V)"max_dimension")), () => (V)(R.Get(v_64, (V)"height") <= R.Get(v_6, (V)"max_dimension")))}, new Results((V)"editable canvas too large")));
if (R.Truth(v_52)) {
var values_102 = R.Join(new V[] {}, R.Call(v_52, R.Join(new V[] {}, new Results(R.Get(v_64, (V)"document")))));
V v_103 = values_102[0];
R.Call(v_30, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_103))).First == (V)"string")}, new Results(R.Concat((V)"missing editable document: ", R.Get(v_64, (V)"document")))));
var values_104 = R.Join(new V[] {}, new Results((V)(v_61 + (V)R.Length(v_103))));
v_61 = values_104[0];
var values_105 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"decode"), R.Join(new V[] {}, new Results(v_103))));
V v_106 = values_105[0];
R.Call(v_30, R.Join(new V[] {R.And(R.And((V)(R.Get(v_106, (V)"id") == R.Get(v_64, (V)"id")), () => (V)(R.Get(v_106, (V)"width") == R.Get(v_64, (V)"width"))), () => (V)(R.Get(v_106, (V)"height") == R.Get(v_64, (V)"height")))}, new Results((V)"document identity or dimensions mismatch")));
var values_107 = R.Join(new V[] {}, R.Call(v_52, R.Join(new V[] {}, new Results(R.Get(v_64, (V)"derived")))));
V v_108 = values_107[0];
R.Call(v_30, R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_108))).First == (V)"string")}, new Results(R.Concat((V)"missing derived PNG: ", R.Get(v_64, (V)"derived")))));
var values_109 = R.Join(new V[] {}, new Results((V)(v_61 + (V)R.Length(v_108))));
v_61 = values_109[0];
var values_110 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"png_dimensions"), R.Join(new V[] {}, new Results(v_108))));
V v_111 = values_110[0];
V v_112 = values_110[1];
var values_113 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"output_dimensions"), R.Join(new V[] {}, new Results(v_64))));
V v_114 = values_113[0];
V v_115 = values_113[1];
R.Call(v_30, R.Join(new V[] {R.And((V)(v_111 == v_114), () => (V)(v_112 == v_115))}, new Results((V)"derived dimensions mismatch")));
if (R.Truth(v_53)) {
R.Call(v_53, R.Join(new V[] {v_108, R.Get(v_64, (V)"derived"), v_111}, new Results(v_112)));
}
}
}
}
foreach (var row_116 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_51, (V)"assets"))))))) {
V v_117 = row_116[0];
V v_118 = row_116[1];
if (R.Truth((V)(R.Get(v_118, (V)"reference_id") != (V)""))) {
var values_119 = R.Join(new V[] {}, new Results(R.Get(v_55, R.Get(v_118, (V)"reference_id"))));
V v_120 = values_119[0];
R.Call(v_30, R.Join(new V[] {R.And(v_120, () => (V)(v_120 != v_118))}, new Results(R.Concat((V)"missing or self reference asset: ", R.Get(v_118, (V)"reference_id")))));
R.Call(v_30, R.Join(new V[] {R.And((V)(R.Get(v_120, (V)"width") == R.Get(v_118, (V)"width")), () => (V)(R.Get(v_120, (V)"height") == R.Get(v_118, (V)"height")))}, new Results(R.Concat((V)"reference dimensions must match: ", R.Get(v_118, (V)"id")))));
R.Call(v_30, R.Join(new V[] {R.Or((V)(R.Get(v_120, (V)"source") != (V)""), () => (V)(R.Get(v_120, (V)"document") != (V)""))}, new Results(R.Concat((V)"reference has no pixels: ", R.Get(v_118, (V)"reference_id")))));
}
}
if (R.Truth(v_52)) {
R.Call(v_30, R.Join(new V[] {(V)(v_61 <= (V)((V)((V)128.0d * (V)1024.0d) * (V)1024.0d))}, new Results((V)"project referenced-data size limit")));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, (V)"output_dimensions", new V((RuleFunction)(args_121 => {
V v_122 = args_121[0];
if (R.Truth(R.And((V)(R.Get(v_122, (V)"derived") != (V)""), () => (V)(R.Get(v_122, (V)"outline_color") != (V)"")))) {
return R.Join(new V[] {(V)(R.Get(v_122, (V)"width") * (V)2.0d)}, new Results((V)(R.Get(v_122, (V)"height") * (V)2.0d)));
}
return R.Join(new V[] {R.Get(v_122, (V)"width")}, new Results(R.Get(v_122, (V)"height")));
return Results.Empty;
})));
R.Set(v_2, (V)"png_dimensions", new V((RuleFunction)(args_123 => {
V v_124 = args_123[0];
R.Call(v_30, R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_124))).First == (V)"string"), () => (V)((V)R.Length(v_124) >= (V)45.0d)), () => (V)((V)R.Length(v_124) <= R.Get(v_2, (V)"max_image_bytes")))}, new Results((V)"invalid or oversized PNG")));
R.Call(v_30, R.Join(new V[] {R.And((V)(R.Invoke(v_124, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)8.0d))).First == (V)"\u00c2\u0089PNG\r\n\u001a\n"), () => (V)(R.Invoke(v_124, "sub", R.Join(new V[] {(V)13.0d}, new Results((V)16.0d))).First == (V)"IHDR"))}, new Results((V)"not a PNG image")));
V v_125 = V.Nil;
v_125 = new V((RuleFunction)(args_126 => {
V v_127 = args_126[0];
var values_128 = R.Join(new V[] {}, R.Invoke(v_124, "byte", R.Join(new V[] {v_127}, new Results((V)(v_127 + (V)3.0d)))));
V v_129 = values_128[0];
V v_130 = values_128[1];
V v_131 = values_128[2];
V v_132 = values_128[3];
return R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)((V)(v_129 * (V)256.0d) + v_130) * (V)256.0d) + v_131) * (V)256.0d) + v_132)));
return Results.Empty;
}));
R.Call(v_30, R.Join(new V[] {(V)(R.Call(v_125, R.Join(new V[] {}, new Results((V)9.0d))).First == (V)13.0d)}, new Results((V)"invalid PNG header")));
var values_133 = R.Join(new V[] {R.Call(v_125, R.Join(new V[] {}, new Results((V)17.0d))).First}, R.Call(v_125, R.Join(new V[] {}, new Results((V)21.0d))));
V v_134 = values_133[0];
V v_135 = values_133[1];
R.Call(v_30, R.Join(new V[] {R.And(R.And(R.And(R.And((V)(v_134 >= (V)1.0d), () => (V)(v_135 >= (V)1.0d)), () => (V)(v_134 <= (V)1024.0d)), () => (V)(v_135 <= (V)1024.0d)), () => (V)((V)(v_134 * v_135) <= (V)1048576.0d))}, new Results((V)"PNG dimensions out of bounds")));
var values_136 = R.Join(new V[] {(V)9.0d, (V)false}, new Results((V)false));
V v_137 = values_136[0];
V v_138 = values_136[1];
V v_139 = values_136[2];
while (R.Truth((V)(v_137 <= (V)R.Length(v_124)))) {
R.Call(v_30, R.Join(new V[] {(V)((V)(v_137 + (V)11.0d) <= (V)R.Length(v_124))}, new Results((V)"truncated PNG chunk")));
var values_140 = R.Join(new V[] {}, R.Call(v_125, R.Join(new V[] {}, new Results(v_137))));
V v_141 = values_140[0];
R.Call(v_30, R.Join(new V[] {R.And((V)(v_141 <= R.Get(v_2, (V)"max_image_bytes")), () => (V)((V)((V)(v_137 + v_141) + (V)11.0d) <= (V)R.Length(v_124)))}, new Results((V)"invalid PNG chunk size")));
var values_142 = R.Join(new V[] {}, R.Invoke(v_124, "sub", R.Join(new V[] {(V)(v_137 + (V)4.0d)}, new Results((V)(v_137 + (V)7.0d)))));
V v_143 = values_142[0];
R.Call(v_30, R.Join(new V[] {R.Invoke(v_143, "match", R.Join(new V[] {}, new Results((V)"^[A-Za-z][A-Za-z][A-Za-z][A-Za-z]$"))).First}, new Results((V)"invalid PNG chunk type")));
var values_144 = R.Join(new V[] {}, new Results((V)4294967295.0d));
V v_145 = values_144[0];
{ double end_148 = R.Num((V)((V)(v_137 + (V)7.0d) + v_141)), step_149 = R.Num((V)1.0d);
for (double i_147 = R.Num((V)(v_137 + (V)4.0d)); step_149 > 0 ? i_147 <= end_148 : i_147 >= end_148; i_147 += step_149) {
V v_146 = i_147;
var values_150 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"bxor"), R.Join(new V[] {R.Get(v_10, R.Call(R.Get(v_4, (V)"band"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"bxor"), R.Join(new V[] {v_145}, R.Invoke(v_124, "byte", R.Join(new V[] {}, new Results(v_146))))).First}, new Results((V)255.0d))).First)}, R.Call(R.Get(v_4, (V)"rshift"), R.Join(new V[] {v_145}, new Results((V)8.0d))))));
v_145 = values_150[0];
}
}
R.Call(v_30, R.Join(new V[] {(V)((V)(R.Call(R.Get(v_4, (V)"bxor"), R.Join(new V[] {v_145}, new Results((V)4294967295.0d))).First % (V)4294967296.0d) == R.Call(v_125, R.Join(new V[] {}, new Results((V)((V)(v_137 + (V)8.0d) + v_141)))).First)}, new Results((V)"PNG checksum mismatch")));
if (R.Truth((V)(v_137 == (V)9.0d))) {
R.Call(v_30, R.Join(new V[] {R.And((V)(v_143 == (V)"IHDR"), () => (V)(v_141 == (V)13.0d))}, new Results((V)"invalid PNG header")));
}
if (R.Truth((V)(v_143 == (V)"IDAT"))) {
var values_151 = R.Join(new V[] {}, new Results((V)true));
v_138 = values_151[0];
}
if (R.Truth((V)(v_143 == (V)"IEND"))) {
R.Call(v_30, R.Join(new V[] {R.And(R.And((V)(v_141 == (V)0.0d), () => v_138), () => (V)((V)((V)(v_137 + v_141) + (V)12.0d) == (V)((V)R.Length(v_124) + (V)1.0d)))}, new Results((V)"invalid PNG ending")));
var values_152 = R.Join(new V[] {}, new Results((V)true));
v_139 = values_152[0];

break;
}
var values_153 = R.Join(new V[] {}, new Results((V)((V)(v_137 + v_141) + (V)12.0d)));
v_137 = values_153[0];
}
R.Call(v_30, R.Join(new V[] {v_139}, new Results((V)"truncated PNG")));
return R.Join(new V[] {v_134}, new Results(v_135));
return Results.Empty;
})));
V v_154 = V.Nil;
v_154 = new V((RuleFunction)(args_155 => {
V v_156 = args_155[0];
return R.Join(new V[] {}, R.Invoke(R.Invoke(R.Invoke(R.Invoke(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_156))).First, "gsub", R.Join(new V[] {(V)"\\"}, new Results((V)"\\\\"))).First, "gsub", R.Join(new V[] {(V)"\t"}, new Results((V)"\\t"))).First, "gsub", R.Join(new V[] {(V)"\r"}, new Results((V)"\\r"))).First, "gsub", R.Join(new V[] {(V)"\n"}, new Results((V)"\\n"))));
return Results.Empty;
}));
V v_157 = V.Nil;
v_157 = new V((RuleFunction)(args_158 => {
V v_159 = args_158[0];
R.Call(v_30, R.Join(new V[] {(V)(!R.Truth(R.Invoke(v_159, "find", R.Join(new V[] {}, new Results((V)"\\[^\\trn]"))).First))}, new Results((V)"invalid manifest escape")));
return R.Join(new V[] {}, new Results(R.Invoke(v_159, "gsub", R.Join(new V[] {(V)"\\([\\trn])"}, new Results(new V((RuleFunction)(args_160 => {
V v_161 = args_160[0];
return R.Join(new V[] {}, new Results(R.Get(R.Table(new Entry((V)"\\", (V)"\\"), new Entry((V)"t", (V)"\t"), new Entry((V)"r", (V)"\r"), new Entry((V)"n", (V)"\n")), v_161)));
return Results.Empty;
}))))).First));
return Results.Empty;
}));
V v_162 = V.Nil;
v_162 = new V((RuleFunction)(args_163 => {
V v_164 = args_163[0];
var values_165 = R.Join(new V[] {}, new Results(R.Table()));
V v_166 = values_165[0];
foreach (var row_167 in R.Iterate(R.Join(new V[] {}, R.Invoke(R.Concat(v_164, (V)"\t"), "gmatch", R.Join(new V[] {}, new Results((V)"(.-)\t")))))) {
V v_168 = row_167[0];
var values_169 = R.Join(new V[] {}, R.Call(v_157, R.Join(new V[] {}, new Results(v_168))));
var obj_170 = v_166;
V key_171 = (V)((V)R.Length(v_166) + (V)1.0d);
R.Set(obj_170, key_171, values_169[0]);
}
return R.Join(new V[] {}, new Results(v_166));
return Results.Empty;
}));
R.Set(v_2, (V)"encode", new V((RuleFunction)(args_172 => {
V v_173 = args_172[0];
R.Call(R.Get(v_2, (V)"validate"), R.Join(new V[] {}, new Results(v_173)));
var values_174 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"ROAG-STUDIO-MANIFEST/1"), new Entry((V)2, R.Concat((V)"project\t", R.Concat(R.Call(v_154, R.Join(new V[] {}, new Results(R.Get(v_173, (V)"id")))).First, R.Concat((V)"\t", R.Call(v_154, R.Join(new V[] {}, new Results(R.Get(v_173, (V)"name")))).First)))))));
V v_175 = values_174[0];
var values_176 = R.Join(new V[] {}, new Results(R.Table()));
V v_177 = values_176[0];
foreach (var row_178 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_173, (V)"assets"))))))) {
V v_179 = row_178[0];
V v_180 = row_178[1];
var values_181 = R.Join(new V[] {}, new Results(v_180));
var obj_182 = v_177;
V key_183 = (V)((V)R.Length(v_177) + (V)1.0d);
R.Set(obj_182, key_183, values_181[0]);
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {v_177}, new Results(new V((RuleFunction)(args_184 => {
V v_185 = args_184[0];
V v_186 = args_184[1];
return R.Join(new V[] {}, new Results((V)(R.Get(v_185, (V)"id") < R.Get(v_186, (V)"id"))));
return Results.Empty;
})))));
foreach (var row_187 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_177)))))) {
V v_188 = row_187[0];
V v_189 = row_187[1];
var values_190 = R.Join(new V[] {}, new Results(R.Table()));
V v_191 = values_190[0];
foreach (var row_192 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_189, (V)"tags"))))))) {
V v_193 = row_192[0];
V v_194 = row_192[1];
var values_195 = R.Join(new V[] {}, new Results(v_194));
var obj_196 = v_191;
V key_197 = (V)((V)R.Length(v_191) + (V)1.0d);
R.Set(obj_196, key_197, values_195[0]);
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_191)));
var values_198 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"asset"), new Entry((V)2, R.Get(v_189, (V)"id")), new Entry((V)3, R.Get(v_189, (V)"name")), new Entry((V)4, R.Get(v_189, (V)"kind")), new Entry((V)5, R.Get(v_189, (V)"source")), new Entry((V)6, R.Get(v_189, (V)"format")), new Entry((V)7, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_189, (V)"width")))).First), new Entry((V)8, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_189, (V)"height")))).First), new Entry((V)9, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_189, (V)"transparent")))).First), new Entry((V)10, R.Get(v_189, (V)"filter")), new Entry((V)11, R.Get(v_189, (V)"binding")), new Entry((V)12, R.Or(R.Get(v_189, (V)"description"), () => (V)"")), new Entry((V)13, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_191}, new Results((V)","))).First), new Entry((V)14, R.Or(R.Get(v_189, (V)"atlas_group"), () => (V)"")), new Entry((V)15, R.Or(R.Get(v_189, (V)"frame_hint"), () => (V)"")), new Entry((V)16, R.Or(R.Get(v_189, (V)"animation_hint"), () => (V)"")), new Entry((V)17, R.Or(R.Get(v_189, (V)"document"), () => (V)"")), new Entry((V)18, R.Or(R.Get(v_189, (V)"derived"), () => (V)"")))));
V v_199 = values_198[0];
if (R.Truth(R.Or((V)(R.Get(v_189, (V)"outline_color") != (V)""), () => (V)(R.Get(v_189, (V)"reference_id") != (V)"")))) {
var values_200 = R.Join(new V[] {}, new Results(R.Get(v_189, (V)"outline_color")));
var obj_201 = v_199;
V key_202 = (V)((V)R.Length(v_199) + (V)1.0d);
R.Set(obj_201, key_202, values_200[0]);
}
if (R.Truth((V)(R.Get(v_189, (V)"reference_id") != (V)""))) {
var values_203 = R.Join(new V[] {}, new Results(R.Get(v_189, (V)"reference_id")));
var obj_204 = v_199;
V key_205 = (V)((V)R.Length(v_199) + (V)1.0d);
R.Set(obj_204, key_205, values_203[0]);
}
foreach (var row_206 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_199)))))) {
V v_207 = row_206[0];
V v_208 = row_206[1];
var values_209 = R.Join(new V[] {}, R.Call(v_154, R.Join(new V[] {}, new Results(v_208))));
var obj_210 = v_199;
V key_211 = v_207;
R.Set(obj_210, key_211, values_209[0]);
}
var values_212 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_199}, new Results((V)"\t"))));
var obj_213 = v_175;
V key_214 = (V)((V)R.Length(v_175) + (V)1.0d);
R.Set(obj_213, key_214, values_212[0]);
}
return R.Join(new V[] {}, new Results(R.Concat(R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {v_175}, new Results((V)"\n"))).First, (V)"\n")));
return Results.Empty;
})));
R.Set(v_2, (V)"decode", new V((RuleFunction)(args_215 => {
V v_216 = args_215[0];
R.Call(v_30, R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_216))).First == (V)"string"), () => (V)((V)R.Length(v_216) <= (V)((V)1024.0d * (V)1024.0d)))}, new Results((V)"manifest too large")));
var values_217 = R.Join(new V[] {}, new Results(R.Table()));
V v_218 = values_217[0];
foreach (var row_219 in R.Iterate(R.Join(new V[] {}, R.Invoke(R.Concat(v_216, (V)"\n"), "gmatch", R.Join(new V[] {}, new Results((V)"(.-)\n")))))) {
V v_220 = row_219[0];
if (R.Truth((V)((V)R.Length(v_220) > (V)0.0d))) {
var values_221 = R.Join(new V[] {}, new Results(v_220));
var obj_222 = v_218;
V key_223 = (V)((V)R.Length(v_218) + (V)1.0d);
R.Set(obj_222, key_223, values_221[0]);
}
}
R.Call(v_30, R.Join(new V[] {(V)(R.Get(v_218, (V)1.0d) == (V)"ROAG-STUDIO-MANIFEST/1")}, new Results((V)"incompatible manifest format")));
var values_224 = R.Join(new V[] {}, R.Call(v_162, R.Join(new V[] {}, new Results(R.Or(R.Get(v_218, (V)2.0d), () => (V)"")))));
V v_225 = values_224[0];
R.Call(v_30, R.Join(new V[] {R.And((V)((V)R.Length(v_225) == (V)3.0d), () => (V)(R.Get(v_225, (V)1.0d) == (V)"project"))}, new Results((V)"invalid project record")));
var values_226 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {R.Get(v_225, (V)2.0d)}, new Results(R.Get(v_225, (V)3.0d)))));
V v_227 = values_226[0];
{ double end_230 = R.Num((V)R.Length(v_218)), step_231 = R.Num((V)1.0d);
for (double i_229 = R.Num((V)3.0d); step_231 > 0 ? i_229 <= end_230 : i_229 >= end_230; i_229 += step_231) {
V v_228 = i_229;
var values_232 = R.Join(new V[] {}, R.Call(v_162, R.Join(new V[] {}, new Results(R.Get(v_218, v_228)))));
V v_233 = values_232[0];
R.Call(v_30, R.Join(new V[] {R.And(R.Or(R.Or(R.Or((V)((V)R.Length(v_233) == (V)16.0d), () => (V)((V)R.Length(v_233) == (V)18.0d)), () => (V)((V)R.Length(v_233) == (V)19.0d)), () => (V)((V)R.Length(v_233) == (V)20.0d)), () => (V)(R.Get(v_233, (V)1.0d) == (V)"asset"))}, new Results((V)"invalid asset record")));
var values_234 = R.Join(new V[] {}, new Results(R.Table()));
V v_235 = values_234[0];
if (R.Truth((V)(R.Get(v_233, (V)13.0d) != (V)""))) {
foreach (var row_236 in R.Iterate(R.Join(new V[] {}, R.Invoke(R.Get(v_233, (V)13.0d), "gmatch", R.Join(new V[] {}, new Results((V)"[^,]+")))))) {
V v_237 = row_236[0];
var values_238 = R.Join(new V[] {}, new Results(v_237));
var obj_239 = v_235;
V key_240 = (V)((V)R.Length(v_235) + (V)1.0d);
R.Set(obj_239, key_240, values_238[0]);
}
}
var values_241 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_233, (V)2.0d)), new Entry((V)"name", R.Get(v_233, (V)3.0d)), new Entry((V)"kind", R.Get(v_233, (V)4.0d)), new Entry((V)"source", R.Get(v_233, (V)5.0d)), new Entry((V)"format", R.Get(v_233, (V)6.0d)), new Entry((V)"width", R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_233, (V)7.0d)))).First), new Entry((V)"height", R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(R.Get(v_233, (V)8.0d)))).First), new Entry((V)"transparent", (V)(R.Get(v_233, (V)9.0d) == (V)"true")), new Entry((V)"filter", R.Get(v_233, (V)10.0d)), new Entry((V)"binding", R.Get(v_233, (V)11.0d)), new Entry((V)"description", R.Get(v_233, (V)12.0d)), new Entry((V)"tags", v_235), new Entry((V)"atlas_group", R.Get(v_233, (V)14.0d)), new Entry((V)"frame_hint", R.Get(v_233, (V)15.0d)), new Entry((V)"animation_hint", R.Get(v_233, (V)16.0d)), new Entry((V)"document", R.Or(R.Get(v_233, (V)17.0d), () => (V)"")), new Entry((V)"derived", R.Or(R.Get(v_233, (V)18.0d), () => (V)"")), new Entry((V)"outline_color", R.Or(R.Get(v_233, (V)19.0d), () => (V)"")), new Entry((V)"reference_id", R.Or(R.Get(v_233, (V)20.0d), () => (V)"")))));
var obj_242 = R.Get(v_227, (V)"assets");
V key_243 = (V)((V)R.Length(R.Get(v_227, (V)"assets")) + (V)1.0d);
R.Set(obj_242, key_243, values_241[0]);
R.Call(v_30, R.Join(new V[] {R.Or((V)(R.Get(v_233, (V)9.0d) == (V)"true"), () => (V)(R.Get(v_233, (V)9.0d) == (V)"false"))}, new Results((V)"invalid alpha intent")));
}
}
R.Call(R.Get(v_2, (V)"validate"), R.Join(new V[] {}, new Results(v_227)));
return R.Join(new V[] {}, new Results(v_227));
return Results.Empty;
})));
R.Set(v_2, (V)"asset", new V((RuleFunction)(args_244 => {
V v_245 = args_244[0];
V v_246 = args_244[1];
V v_247 = args_244[2];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_245), new Entry((V)"name", v_246), new Entry((V)"kind", v_247), new Entry((V)"source", (V)""), new Entry((V)"format", (V)""), new Entry((V)"width", (V)0.0d), new Entry((V)"height", (V)0.0d), new Entry((V)"transparent", (V)true), new Entry((V)"filter", (V)"nearest"), new Entry((V)"binding", (V)""), new Entry((V)"description", (V)""), new Entry((V)"tags", R.Table()), new Entry((V)"atlas_group", (V)""), new Entry((V)"frame_hint", (V)""), new Entry((V)"animation_hint", (V)""), new Entry((V)"document", (V)""), new Entry((V)"derived", (V)""), new Entry((V)"outline_color", (V)""), new Entry((V)"reference_id", (V)""))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
