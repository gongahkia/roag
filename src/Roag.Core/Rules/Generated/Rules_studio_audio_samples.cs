// Static C# port of src/studio_audio_samples.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 58ee8c40522399ac69f21685e6982e0dda29c5d24b0e8e29bea64b4a3581e2d8
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_studio_audio_samples { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
V v_3 = V.Nil;
v_3 = new V((RuleFunction)(args_4 => {
V v_5 = args_4[0];
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"char"), R.Join(new V[] {(V)(v_5 % (V)256.0d)}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_5 / (V)256.0d)))).First % (V)256.0d)))));
return Results.Empty;
}));
V v_6 = V.Nil;
v_6 = new V((RuleFunction)(args_7 => {
V v_8 = args_7[0];
return R.Join(new V[] {}, new Results(R.Concat(R.Call(v_3, R.Join(new V[] {}, new Results((V)(v_8 % (V)65536.0d)))).First, R.Call(v_3, R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_8 / (V)65536.0d)))))).First)));
return Results.Empty;
}));
R.Set(v_2, (V)"wav", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
var values_11 = R.Join(new V[] {}, new Results((V)22050.0d));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"step", R.Table(new Entry((V)1, (V)0.085d), new Entry((V)2, (V)150.0d), new Entry((V)3, (V)0.2d))), new Entry((V)"hit", R.Table(new Entry((V)1, (V)0.14d), new Entry((V)2, (V)85.0d), new Entry((V)3, (V)0.36d))), new Entry((V)"boon", R.Table(new Entry((V)1, (V)0.28d), new Entry((V)2, (V)660.0d), new Entry((V)3, (V)0.26d))), new Entry((V)"menu", R.Table(new Entry((V)1, (V)0.09d), new Entry((V)2, (V)480.0d), new Entry((V)3, (V)0.18d))), new Entry((V)"enemy", R.Table(new Entry((V)1, (V)0.17d), new Entry((V)2, (V)110.0d), new Entry((V)3, (V)0.26d))), new Entry((V)"terrain", R.Table(new Entry((V)1, (V)0.12d), new Entry((V)2, (V)190.0d), new Entry((V)3, (V)0.24d))), new Entry((V)"music", R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)220.0d), new Entry((V)3, (V)0.13d))), new Entry((V)"ambience", R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)96.0d), new Entry((V)3, (V)0.055d))))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_14, v_10)}, new Results((V)"unknown original audio sample"))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_12 * R.Get(v_16, (V)1.0d)) + (V)0.5d)))));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, new Results(R.Table()));
V v_20 = values_19[0];
{ double end_23 = R.Num((V)(v_18 - (V)1.0d)), step_24 = R.Num((V)1.0d);
for (double i_22 = R.Num((V)0.0d); step_24 > 0 ? i_22 <= end_23 : i_22 >= end_23; i_22 += step_24) {
V v_21 = i_22;
var values_25 = R.Join(new V[] {}, new Results((V)(v_21 / v_12)));
V v_26 = values_25[0];
var values_27 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Or((V)(v_10 == (V)"music"), () => (V)(v_10 == (V)"ambience")), () => (V)((V)0.5d - (V)((V)0.5d * R.Call(R.Get(rt.Global("math"), (V)"cos"), R.Join(new V[] {}, new Results((V)((V)((V)((V)2.0d * R.Get(rt.Global("math"), (V)"pi")) * v_21) / v_18)))).First))), () => (V)Math.Pow(R.Num((V)((V)1.0d - (V)(v_21 / v_18))), R.Num((V)2.0d)))));
V v_28 = values_27[0];
var values_29 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results((V)((V)((V)((V)2.0d * R.Get(rt.Global("math"), (V)"pi")) * R.Get(v_16, (V)2.0d)) * v_26)))));
V v_30 = values_29[0];
if (R.Truth(R.Or((V)(v_10 == (V)"hit"), () => (V)(v_10 == (V)"enemy")))) {
var values_31 = R.Join(new V[] {}, new Results((V)((V)(v_30 * (V)0.7d) + (V)(R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results((V)((V)((V)((V)2.0d * R.Get(rt.Global("math"), (V)"pi")) * (V)(R.Get(v_16, (V)2.0d) * (V)2.03d)) * v_26)))).First * (V)0.3d))));
v_30 = values_31[0];
} else if (R.Truth(R.Or(R.Or((V)(v_10 == (V)"boon"), () => (V)(v_10 == (V)"music")), () => (V)(v_10 == (V)"ambience")))) {
var values_32 = R.Join(new V[] {}, new Results((V)((V)(v_30 * (V)0.65d) + (V)(R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results((V)((V)((V)((V)2.0d * R.Get(rt.Global("math"), (V)"pi")) * (V)(R.Get(v_16, (V)2.0d) * (V)1.5d)) * v_26)))).First * (V)0.35d))));
v_30 = values_32[0];
}
var values_33 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(-(V)1.0d)}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)1.0d}, new Results((V)((V)(v_30 * v_28) * R.Get(v_16, (V)3.0d))))))).First * (V)32767.0d) + (V)0.5d)))));
V v_34 = values_33[0];
if (R.Truth((V)(v_34 < (V)0.0d))) {
var values_35 = R.Join(new V[] {}, new Results((V)(v_34 + (V)65536.0d)));
v_34 = values_35[0];
}
var values_36 = R.Join(new V[] {}, R.Call(v_3, R.Join(new V[] {}, new Results(v_34))));
var obj_37 = v_20;
V key_38 = (V)((V)R.Length(v_20) + (V)1.0d);
R.Set(obj_37, key_38, values_36[0]);
}
}
var values_39 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(v_20))));
V v_40 = values_39[0];
var values_41 = R.Join(new V[] {}, new Results(R.Concat(R.Call(v_3, R.Join(new V[] {}, new Results((V)1.0d))).First, R.Concat(R.Call(v_3, R.Join(new V[] {}, new Results((V)1.0d))).First, R.Concat(R.Call(v_6, R.Join(new V[] {}, new Results(v_12))).First, R.Concat(R.Call(v_6, R.Join(new V[] {}, new Results((V)(v_12 * (V)2.0d)))).First, R.Concat(R.Call(v_3, R.Join(new V[] {}, new Results((V)2.0d))).First, R.Call(v_3, R.Join(new V[] {}, new Results((V)16.0d))).First)))))));
V v_42 = values_41[0];
return R.Join(new V[] {}, new Results(R.Concat((V)"RIFF", R.Concat(R.Call(v_6, R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)4.0d + (V)8.0d) + (V)R.Length(v_42)) + (V)8.0d) + (V)R.Length(v_40))))).First, R.Concat((V)"WAVE", R.Concat((V)"fmt ", R.Concat(R.Call(v_6, R.Join(new V[] {}, new Results((V)R.Length(v_42)))).First, R.Concat(v_42, R.Concat((V)"data", R.Concat(R.Call(v_6, R.Join(new V[] {}, new Results((V)R.Length(v_40)))).First, v_40))))))))));
return Results.Empty;
})));
R.Set(v_2, (V)"showcase", new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
var values_45 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_audio"))));
V v_46 = values_45[0];
var values_47 = R.Join(new V[] {}, R.Call(R.Get(v_46, (V)"new"), R.Join(new V[] {}, new Results(v_44))));
V v_48 = values_47[0];
var values_49 = R.Join(new V[] {}, new Results(R.Table()));
V v_50 = values_49[0];
var values_51 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"step"), new Entry((V)2, (V)"core:cue/player/move"))), new Entry((V)2, R.Table(new Entry((V)1, (V)"hit"), new Entry((V)2, (V)"core:cue/player/damage"))), new Entry((V)3, R.Table(new Entry((V)1, (V)"enemy"), new Entry((V)2, (V)"core:cue/enemy/telegraph"))), new Entry((V)4, R.Table(new Entry((V)1, (V)"terrain"), new Entry((V)2, (V)"core:cue/terrain/step"))), new Entry((V)5, R.Table(new Entry((V)1, (V)"boon"), new Entry((V)2, (V)"core:cue/boon/activate"))), new Entry((V)6, R.Table(new Entry((V)1, (V)"menu"), new Entry((V)2, (V)"core:cue/ui/confirm"))), new Entry((V)7, R.Table(new Entry((V)1, (V)"music"), new Entry((V)2, (V)"core:cue/music/title"))))));
V v_52 = values_51[0];
var values_53 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_54 = values_53[0];
foreach (var row_55 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_54, (V)"order"))))))) {
V v_56 = row_55[0];
V v_57 = row_55[1];
var values_58 = R.Join(new V[] {}, R.Invoke(v_57, "match", R.Join(new V[] {}, new Results((V)"([^/]+)$"))));
V v_59 = values_58[0];
var values_60 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"music"), new Entry((V)2, R.Concat((V)"core:cue/music/explore/", v_59)))));
var obj_61 = v_52;
V key_62 = (V)((V)R.Length(v_52) + (V)1.0d);
R.Set(obj_61, key_62, values_60[0]);
var values_63 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"music"), new Entry((V)2, R.Concat((V)"core:cue/music/elite/", v_59)))));
var obj_64 = v_52;
V key_65 = (V)((V)R.Length(v_52) + (V)1.0d);
R.Set(obj_64, key_65, values_63[0]);
var values_66 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"ambience"), new Entry((V)2, R.Concat((V)"core:cue/ambient/", v_59)))));
var obj_67 = v_52;
V key_68 = (V)((V)R.Length(v_52) + (V)1.0d);
R.Set(obj_67, key_68, values_66[0]);
}
foreach (var row_69 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"final_boss"), new Entry((V)2, (V)"victory"), new Entry((V)3, (V)"defeat")))))))) {
V v_70 = row_69[0];
V v_71 = row_69[1];
var values_72 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"music"), new Entry((V)2, R.Concat((V)"core:cue/music/", v_71)))));
var obj_73 = v_52;
V key_74 = (V)((V)R.Length(v_52) + (V)1.0d);
R.Set(obj_73, key_74, values_72[0]);
}
var values_75 = R.Join(new V[] {}, new Results(R.Table()));
V v_76 = values_75[0];
foreach (var row_77 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_52)))))) {
V v_78 = row_77[0];
V v_79 = row_77[1];
var values_80 = R.Join(new V[] {R.Get(v_79, (V)1.0d)}, new Results(R.Get(v_79, (V)2.0d)));
V v_81 = values_80[0];
V v_82 = values_80[1];
var values_83 = R.Join(new V[] {}, new Results(R.Get(v_76, v_81)));
V v_84 = values_83[0];
if (R.Truth((V)(!R.Truth(v_84)))) {
var values_85 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"wav"), R.Join(new V[] {}, new Results(v_81))));
V v_86 = values_85[0];
var values_87 = R.Join(new V[] {}, new Results(R.Concat((V)"studio:audio/original_", v_81)));
V v_88 = values_87[0];
var values_89 = R.Join(new V[] {}, R.Call(R.Get(v_46, (V)"make_sound"), R.Join(new V[] {v_44, v_88, R.Concat((V)"Original ", R.Concat(v_81, (V)" placeholder")), v_86}, new Results(R.Table(new Entry((V)"author", (V)"ROAG Studio synthesized"), new Entry((V)"license", (V)"Original project asset"), new Entry((V)"tags", (V)"placeholder"))))));
v_84 = values_89[0];
var values_90 = R.Join(new V[] {}, new Results(v_84));
var obj_91 = R.Get(v_48, (V)"sounds");
V key_92 = (V)((V)R.Length(R.Get(v_48, (V)"sounds")) + (V)1.0d);
R.Set(obj_91, key_92, values_90[0]);
var values_93 = R.Join(new V[] {}, new Results(v_86));
var obj_94 = v_50;
V key_95 = R.Get(v_84, (V)"path");
R.Set(obj_94, key_95, values_93[0]);
var values_96 = R.Join(new V[] {}, new Results(v_84));
var obj_97 = v_76;
V key_98 = v_81;
R.Set(obj_97, key_98, values_96[0]);
}
var values_99 = R.Join(new V[] {}, R.Call(R.Get(v_46, (V)"binding"), R.Join(new V[] {v_82}, new Results(R.Get(v_84, (V)"id")))));
var obj_100 = R.Get(v_48, (V)"bindings");
V key_101 = (V)((V)R.Length(R.Get(v_48, (V)"bindings")) + (V)1.0d);
R.Set(obj_100, key_101, values_99[0]);
}
return R.Join(new V[] {v_48}, new Results(v_50));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
