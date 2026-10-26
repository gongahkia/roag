// Static C# port of tests/motion_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: e1fbda7a2abfadc9205d19a73212569fbb6894c28ab76b64643bb010060eed09
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_motion_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.camera"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.playback"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.combat_numbers"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.hit_feedback"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_14 = values_13[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_15 => {
V v_16 = args_15[0];
V v_17 = args_15[1];
R.Call(v_16, R.Join(new V[] {(V)"floating hit numbers use resolved loss once per displayed beat"}, new Results(new V((RuleFunction)(args_18 => {

var values_19 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"new"), Results.Empty));
V v_20 = values_19[0];
var values_21 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"events", R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"target_id", (V)7.0d), new Entry((V)"source_id", (V)1.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)4.0d), new Entry((V)"actual_loss", (V)2.0d), new Entry((V)"critical", (V)false))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"target_id", (V)7.0d), new Entry((V)"source_id", (V)1.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)4.0d), new Entry((V)"actual_loss", (V)3.0d), new Entry((V)"critical", (V)true))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"Died"), new Entry((V)"target_id", (V)7.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)4.0d))))))));
V v_22 = values_21[0];
var values_23 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"items", R.Table(new Entry((V)1, v_22))))));
V v_24 = values_23[0];
R.Call(R.Get(v_10, (V)"update"), R.Join(new V[] {v_20, v_24, (V)0.0d, (V)1.0d}, new Results((V)"floor")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(R.Get(v_20, (V)"items")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_20, (V)"items"), (V)1.0d), (V)"amount") == (V)5.0d)), () => R.Get(R.Get(R.Get(v_20, (V)"items"), (V)1.0d), (V)"critical")), () => R.Get(R.Get(R.Get(v_20, (V)"items"), (V)1.0d), (V)"from_player")))));
R.Call(R.Get(v_10, (V)"update"), R.Join(new V[] {v_20, v_24, (V)0.1d, (V)1.0d}, new Results((V)"floor")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_20, (V)"items")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_20, (V)"items"), (V)1.0d), (V)"age") == (V)0.1d)))));
var values_25 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"event", R.Table(new Entry((V)"kind", (V)"DamageBlocked"), new Entry((V)"target_id", (V)8.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)4.0d))))))));
var obj_26 = v_24;
V key_27 = (V)"items";
R.Set(obj_26, key_27, values_25[0]);
R.Call(R.Get(v_10, (V)"update"), R.Join(new V[] {v_20, v_24, (V)0.0d, (V)1.0d}, new Results((V)"floor")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_20, (V)"items")) == (V)2.0d), () => R.Get(R.Get(R.Get(v_20, (V)"items"), (V)2.0d), (V)"blocked")))));
R.Call(R.Get(v_10, (V)"update"), R.Join(new V[] {v_20, v_24, (V)0.0d, (V)1.0d}, new Results((V)"other-floor")));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(R.Get(v_20, (V)"items")) == (V)0.0d)}, new Results((V)"old floor kept floating damage")));
return Results.Empty;
})))));
V v_28 = V.Nil;
v_28 = new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_30 - v_31)))).First < (V)1e-06d)}, R.Invoke((V)"%.8f ~= %.8f", "format", R.Join(new V[] {v_30}, new Results(v_31)))));
return Results.Empty;
}));
V v_32 = V.Nil;
v_32 = new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
V v_35 = args_33[1];
V v_36 = args_33[2];
V v_37 = args_33[3];
V v_38 = args_33[4];
V v_39 = args_33[5];
V v_40 = args_33[6];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_34), new Entry((V)"kind", (V)"Moved"), new Entry((V)"source_id", v_34), new Entry((V)"time", v_39), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"from_x", v_35), new Entry((V)"from_y", v_36), new Entry((V)"x", v_37), new Entry((V)"y", v_38), new Entry((V)"path", v_40))));
return Results.Empty;
}));
R.Call(v_16, R.Join(new V[] {(V)"enemy facing follows displayed movement legs and persists through vertical motion"}, new Results(new V((RuleFunction)(args_41 => {

var values_42 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_43 = values_42[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_43, R.Table(new Entry((V)1, R.Call(v_32, R.Join(new V[] {(V)8.0d, (V)7.0d, (V)3.0d, (V)6.0d, (V)3.0d}, new Results((V)0.0d))).First), new Entry((V)2, R.Call(v_32, R.Join(new V[] {(V)8.0d, (V)6.0d, (V)3.0d, (V)6.0d, (V)4.0d}, new Results((V)1.0d))).First), new Entry(3, R.Call(v_32, R.Join(new V[] {(V)8.0d, (V)6.0d, (V)4.0d, (V)7.0d, (V)4.0d}, new Results((V)2.0d)))))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_43}, new Results((V)8.0d))).First == (-(V)1.0d)))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_43}, new Results(R.Get(v_8, (V)"tile_seconds"))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_43}, new Results((V)8.0d))).First == (-(V)1.0d))}, new Results((V)"vertical movement reset facing")));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_43}, new Results(R.Get(v_8, (V)"tile_seconds"))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_43}, new Results((V)8.0d))).First == (V)1.0d)}, new Results((V)"next horizontal leg did not turn")));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_43}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_43}, new Results((V)8.0d))).First == (V)1.0d))));
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_43, R.Table(new Entry(1, R.Call(v_32, R.Join(new V[] {(V)8.0d, (V)7.0d, (V)4.0d, (V)6.0d, (V)4.0d}, new Results((V)3.0d)))))}, new Results((V)1.0d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_43}, new Results((V)1.0d)));
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_43, R.Table(new Entry(1, R.Call(v_32, R.Join(new V[] {(V)8.0d, (V)6.0d, (V)4.0d, (V)6.0d, (V)5.0d}, new Results((V)4.0d)))))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_43}, new Results((V)8.0d))).First == (-(V)1.0d)))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_43}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_43}, new Results((V)8.0d))).First == (-(V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"enemy attack facing uses locked cells at its beat and survives idle or Continue"}, new Results(new V((RuleFunction)(args_44 => {

var values_45 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_46 = values_45[0];
var values_47 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"WindupStarted"), new Entry((V)"source_id", (V)8.0d), new Entry((V)"from_x", (V)6.0d), new Entry((V)"from_y", (V)4.0d), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)5.0d), new Entry((V)"y", (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)4.0d))))), new Entry((V)"floor_id", (V)"floor"))));
V v_48 = values_47[0];
var values_49 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"AttackPerformed"), new Entry((V)"source_id", (V)8.0d), new Entry((V)"from_x", (V)6.0d), new Entry((V)"from_y", (V)4.0d), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)4.0d))))))));
V v_50 = values_49[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_46, R.Table(new Entry((V)1, R.Call(v_32, R.Join(new V[] {(V)8.0d, (V)5.0d, (V)4.0d, (V)6.0d, (V)4.0d}, new Results((V)0.0d))).First), new Entry((V)2, v_48), new Entry((V)3, v_50))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_46, (V)8.0d}, new Results(v_48))).First == (V)1.0d)}, new Results((V)"future locked attack turned actor too early")));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_46}, new Results((V)0.08d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_46}, new Results((V)8.0d))).First == (-(V)1.0d)))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_46}, new Results((V)0.55d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_46}, new Results((V)8.0d))).First == (V)1.0d))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_46}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_46}, new Results((V)8.0d))).First == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {R.Call(R.Get(v_8, (V)"new"), Results.Empty).First, (V)8.0d}, new Results(R.Table(new Entry((V)"source_x", (V)6.0d), new Entry((V)"target_cells", R.Get(v_48, (V)"target_cells")))))).First == (-(V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {R.Call(R.Get(v_8, (V)"new"), Results.Empty).First, (V)8.0d, V.Nil}, new Results((-(V)1.0d)))).First == (-(V)1.0d)))));
var values_51 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"source_x", (V)6.0d), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)5.0d), new Entry((V)"y", (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)4.0d))))))));
V v_52 = values_51[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {R.Call(R.Get(v_8, (V)"new"), Results.Empty).First, (V)8.0d, v_52}, new Results((-(V)1.0d)))).First == (-(V)1.0d))}, new Results((V)"symmetric AOE chose an arbitrary direction")));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"grouped enemy windups turn independently and knockback does not turn an actor"}, new Results(new V((RuleFunction)(args_53 => {

var values_54 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_55 = values_54[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_55, R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"WindupStarted"), new Entry((V)"source_id", (V)8.0d), new Entry((V)"from_x", (V)5.0d), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)4.0d))))))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"WindupStarted"), new Entry((V)"source_id", (V)9.0d), new Entry((V)"from_x", (V)5.0d), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)6.0d), new Entry((V)"y", (V)4.0d))))))))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_55}, new Results((V)8.0d))).First == (-(V)1.0d)), () => (V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_55}, new Results((V)9.0d))).First == (V)1.0d)))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_55}, new Results((V)1.0d)));
var values_56 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {(V)8.0d, (V)5.0d, (V)4.0d, (V)6.0d, (V)4.0d}, new Results((V)1.0d))));
V v_57 = values_56[0];
var values_58 = R.Join(new V[] {}, new Results((V)"Displaced"));
var obj_59 = v_57;
V key_60 = (V)"kind";
R.Set(obj_59, key_60, values_58[0]);

var values_61 = R.Join(new V[] {}, new Results((V)8.0d));
var obj_62 = v_57;
V key_63 = (V)"target_id";
R.Set(obj_62, key_63, values_61[0]);

var values_64 = R.Join(new V[] {}, new Results((V)1.0d));
var obj_65 = v_57;
V key_66 = (V)"source_id";
R.Set(obj_65, key_66, values_64[0]);
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_55, R.Table(new Entry((V)1, v_57))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_55}, new Results((V)8.0d))).First == (-(V)1.0d)))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_55}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"facing"), R.Join(new V[] {v_55}, new Results((V)8.0d))).First == (-(V)1.0d)))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"dashed attack borders stay inside tiles and leave interiors and gaps clear at every zoom"}, new Results(new V((RuleFunction)(args_67 => {

var values_68 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.attack_outline"))));
V v_69 = values_68[0];
foreach (var row_70 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)32.0d), new Entry((V)2, (V)40.0d), new Entry((V)3, (V)48.0d), new Entry((V)4, (V)56.0d), new Entry((V)5, (V)64.0d)))))))) {
V v_71 = row_70[0];
V v_72 = row_70[1];
var values_73 = R.Join(new V[] {}, R.Call(R.Get(v_69, (V)"rectangles"), R.Join(new V[] {}, new Results(v_72))));
V v_74 = values_73[0];
var values_75 = R.Join(new V[] {}, new Results(R.Table()));
V v_76 = values_75[0];
foreach (var row_77 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_74)))))) {
V v_78 = row_77[0];
V v_79 = row_77[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_79, (V)1.0d) >= (V)3.0d), () => (V)(R.Get(v_79, (V)2.0d) >= (V)3.0d)), () => (V)((V)(R.Get(v_79, (V)1.0d) + R.Get(v_79, (V)3.0d)) <= (V)(v_72 - (V)3.0d))), () => (V)((V)(R.Get(v_79, (V)2.0d) + R.Get(v_79, (V)4.0d)) <= (V)(v_72 - (V)3.0d))))));
{ double end_82 = R.Num((V)((V)(R.Get(v_79, (V)2.0d) + R.Get(v_79, (V)4.0d)) - (V)1.0d)), step_83 = R.Num((V)1.0d);
for (double i_81 = R.Num(R.Get(v_79, (V)2.0d)); step_83 > 0 ? i_81 <= end_82 : i_81 >= end_82; i_81 += step_83) {
V v_80 = i_81;
{ double end_86 = R.Num((V)((V)(R.Get(v_79, (V)1.0d) + R.Get(v_79, (V)3.0d)) - (V)1.0d)), step_87 = R.Num((V)1.0d);
for (double i_85 = R.Num(R.Get(v_79, (V)1.0d)); step_87 > 0 ? i_85 <= end_86 : i_85 >= end_86; i_85 += step_87) {
V v_84 = i_85;
var values_88 = R.Join(new V[] {}, new Results((V)true));
var obj_89 = v_76;
V key_90 = (V)((V)(v_80 * v_72) + v_84);
R.Set(obj_89, key_90, values_88[0]);
}
}
}
}
}
var values_91 = R.Join(new V[] {}, new Results((V)0.0d));
V v_92 = values_91[0];
{ double end_95 = R.Num((V)(v_72 - (V)4.0d)), step_96 = R.Num((V)1.0d);
for (double i_94 = R.Num((V)3.0d); step_96 > 0 ? i_94 <= end_95 : i_94 >= end_95; i_94 += step_96) {
V v_93 = i_94;
if (R.Truth((V)(!R.Truth(R.Get(v_76, (V)((V)((V)3.0d * v_72) + v_93)))))) {
var values_97 = R.Join(new V[] {}, new Results((V)(v_92 + (V)1.0d)));
v_92 = values_97[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_92 > (V)0.0d)}, new Results((V)"warning border is solid")));
{ double end_100 = R.Num((V)(v_72 - (V)7.0d)), step_101 = R.Num((V)1.0d);
for (double i_99 = R.Num((V)6.0d); step_101 > 0 ? i_99 <= end_100 : i_99 >= end_100; i_99 += step_101) {
V v_98 = i_99;
{ double end_104 = R.Num((V)(v_72 - (V)7.0d)), step_105 = R.Num((V)1.0d);
for (double i_103 = R.Num((V)6.0d); step_105 > 0 ? i_103 <= end_104 : i_103 >= end_104; i_103 += step_105) {
V v_102 = i_103;
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_76, (V)((V)(v_98 * v_72) + v_102))))}, new Results((V)"warning obscures tile interior")));
}
}
}
}
}
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"board token starts, eases through midpoint and snaps to final tile"}, new Results(new V((RuleFunction)(args_106 => {

var values_107 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_108 = values_107[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_108, R.Table(new Entry(1, R.Call(v_32, R.Join(new V[] {(V)3.0d, (V)2.0d, (V)4.0d, (V)3.0d, (V)4.0d}, new Results((V)0.0d)))))}, new Results((V)1.0d)));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_108, (V)3.0d, (V)"floor", (V)3.0d}, new Results((V)4.0d))))).First}, new Results((V)2.0d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_108}, new Results((V)(R.Get(v_8, (V)"tile_seconds") / (V)2.0d))));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_108, (V)3.0d, (V)"floor", (V)3.0d}, new Results((V)4.0d))))).First}, new Results((V)2.5d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_108}, new Results((V)(R.Get(v_8, (V)"tile_seconds") / (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_8, (V)"busy"), R.Join(new V[] {}, new Results(v_108))).First)))));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_108, (V)3.0d, (V)"floor", (V)3.0d}, new Results((V)4.0d))))).First}, new Results((V)3.0d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_108}, new Results((V)10.0d)));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_108, (V)3.0d, (V)"floor", (V)3.0d}, new Results((V)4.0d))))).First}, new Results((V)3.0d)));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"Dash follows both legal legs and partial Dash stops before wall"}, new Results(new V((RuleFunction)(args_109 => {

var values_110 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_111 = values_110[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_111, R.Table(new Entry(1, R.Call(v_32, R.Join(new V[] {(V)1.0d, (V)2.0d, (V)2.0d, (V)4.0d, (V)2.0d, (V)0.0d}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)2.0d)))))))))}, new Results((V)1.0d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_111}, new Results(R.Get(v_8, (V)"tile_seconds"))));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_111, (V)1.0d, (V)"floor", (V)4.0d}, new Results((V)2.0d))))).First}, new Results((V)3.0d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_111}, new Results((V)(R.Get(v_8, (V)"tile_seconds") / (V)2.0d))));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_111, (V)1.0d, (V)"floor", (V)4.0d}, new Results((V)2.0d))))).First}, new Results((V)3.5d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_111}, new Results((V)(R.Get(v_8, (V)"tile_seconds") / (V)2.0d))));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_111, (V)1.0d, (V)"floor", (V)4.0d}, new Results((V)2.0d))))).First}, new Results((V)4.0d)));
var values_112 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"new"), R.Join(new V[] {}, new Results((V)5.0d))));
V v_113 = values_112[0];
var values_114 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"submit"), R.Join(new V[] {v_113}, new Results(R.Table(new Entry((V)"kind", (V)"dash"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_115 = values_114[0];
V v_116 = values_114[1];
V v_117 = values_114[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_115, () => (V)(R.Get(R.Get(v_117, (V)1.0d), (V)"kind") == (V)"Moved")), () => (V)((V)R.Length(R.Get(R.Get(v_117, (V)1.0d), (V)"path")) == (V)2.0d)))));
var values_118 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_119 = values_118[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_119, R.Table(new Entry(1, R.Call(v_32, R.Join(new V[] {(V)1.0d, (V)2.0d, (V)2.0d, (V)3.0d, (V)2.0d, (V)0.0d}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d)))))))))}, new Results((V)1.0d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_119}, new Results((V)1.0d)));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_119, (V)1.0d, (V)"floor", (V)3.0d}, new Results((V)2.0d))))).First}, new Results((V)3.0d)));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"simultaneous actors retain their own origins and bounded beat"}, new Results(new V((RuleFunction)(args_120 => {

var values_121 = R.Join(new V[] {}, new Results(R.Table()));
V v_122 = values_121[0];
{ double end_125 = R.Num((V)60.0d), step_126 = R.Num((V)1.0d);
for (double i_124 = R.Num((V)1.0d); step_126 > 0 ? i_124 <= end_125 : i_124 >= end_125; i_124 += step_126) {
V v_123 = i_124;
var values_127 = R.Join(new V[] {}, R.Call(v_32, R.Join(new V[] {v_123, v_123, (V)3.0d, (V)(v_123 + (V)1.0d), (V)3.0d}, new Results(v_123))));
var obj_128 = v_122;
V key_129 = (V)((V)R.Length(v_122) + (V)1.0d);
R.Set(obj_128, key_129, values_127[0]);
}
}
var values_130 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_131 = values_130[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_131, v_122}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_131, (V)"items")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_131, (V)"items"), (V)1.0d), (V)"duration") == R.Get(v_8, (V)"tile_seconds"))))));
{ double end_134 = R.Num((V)60.0d), step_135 = R.Num((V)1.0d);
for (double i_133 = R.Num((V)1.0d); step_135 > 0 ? i_133 <= end_134 : i_133 >= end_134; i_133 += step_135) {
V v_132 = i_133;
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_131, v_132, (V)"floor", (V)(v_132 + (V)1.0d)}, new Results((V)3.0d))))).First}, new Results(v_132)));
}
}
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_131}, new Results((V)(R.Get(v_8, (V)"tile_seconds") / (V)2.0d))));
{ double end_138 = R.Num((V)60.0d), step_139 = R.Num((V)1.0d);
for (double i_137 = R.Num((V)1.0d); step_139 > 0 ? i_137 <= end_138 : i_137 >= end_138; i_137 += step_139) {
V v_136 = i_137;
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_131, v_136, (V)"floor", (V)(v_136 + (V)1.0d)}, new Results((V)3.0d))))).First}, new Results((V)(v_136 + (V)0.5d))));
}
}
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_131}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_8, (V)"busy"), R.Join(new V[] {}, new Results(v_131))).First)))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"one actor's repeated enemy turns retain intermediate event-time tiles"}, new Results(new V((RuleFunction)(args_140 => {

var values_141 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_142 = values_141[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_142, R.Table(new Entry((V)1, R.Call(v_32, R.Join(new V[] {(V)8.0d, (V)7.0d, (V)3.0d, (V)6.0d, (V)3.0d}, new Results((V)0.0d))).First), new Entry(2, R.Call(v_32, R.Join(new V[] {(V)8.0d, (V)6.0d, (V)3.0d, (V)5.0d, (V)3.0d}, new Results((V)70.0d)))))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_142, (V)"items")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_142, (V)"items"), (V)1.0d), (V)"duration") == (V)0.16d)))));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_142, (V)8.0d, (V)"floor", (V)5.0d}, new Results((V)3.0d))))).First}, new Results((V)7.0d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_142}, new Results((V)0.08d)));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_142, (V)8.0d, (V)"floor", (V)5.0d}, new Results((V)3.0d))))).First}, new Results((V)6.0d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_142}, new Results((V)0.08d)));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_142, (V)8.0d, (V)"floor", (V)5.0d}, new Results((V)3.0d))))).First}, new Results((V)5.0d)));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"movement beats precede telegraph and impact without changing world state"}, new Results(new V((RuleFunction)(args_143 => {

var values_144 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"new"), R.Join(new V[] {(V)28.0d, (V)"core:map/scrolling_room"}, new Results(R.Table(new Entry((V)"demo_enemies", (V)true))))));
V v_145 = values_144[0];
var values_146 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_145))));
V v_147 = values_146[0];
var values_148 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"submit"), R.Join(new V[] {v_145}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_149 = values_148[0];
V v_150 = values_148[1];
V v_151 = values_148[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_149)));
var values_152 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_145))));
V v_153 = values_152[0];
var values_154 = R.Join(new V[] {R.Call(R.Get(v_8, (V)"new"), Results.Empty).First}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_155 = values_154[0];
V v_156 = values_154[1];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_155, v_151}, new Results(R.Get(v_145, (V)"player_id"))));
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_156, v_151}, new Results(R.Get(v_145, (V)"player_id"))));
foreach (var row_157 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)0.01d), new Entry((V)2, (V)0.03d), new Entry((V)3, (V)0.03d), new Entry((V)4, (V)0.5d), new Entry((V)5, (V)1.0d), new Entry((V)6, (V)1.0d), new Entry((V)7, (V)1.0d)))))))) {
V v_158 = row_157[0];
V v_159 = row_157[1];
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_155}, new Results(v_159)));
}
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_156}, new Results((V)10.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(R.Get(v_8, (V)"busy"), R.Join(new V[] {}, new Results(v_155))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_8, (V)"busy"), R.Join(new V[] {}, new Results(v_156))).First))))));
R.Call(v_17, R.Join(new V[] {v_153}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_145)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_147, (V)"clock") != R.Get(v_145, (V)"clock")))));
var values_160 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_161 = values_160[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_161, R.Table(new Entry((V)1, R.Call(v_32, R.Join(new V[] {(V)2.0d, (V)3.0d, (V)3.0d, (V)4.0d, (V)3.0d}, new Results((V)0.0d))).First), new Entry((V)2, R.Table(new Entry((V)"id", (V)3.0d), new Entry((V)"kind", (V)"WindupStarted"), new Entry((V)"source_id", (V)2.0d), new Entry((V)"time", (V)50.0d))), new Entry((V)3, R.Table(new Entry((V)"id", (V)4.0d), new Entry((V)"kind", (V)"AttackPerformed"), new Entry((V)"source_id", (V)2.0d), new Entry((V)"time", (V)80.0d))))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_8, (V)"current"), R.Join(new V[] {}, new Results(v_161))).First, (V)"kind") == (V)"Moved"))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_161}, new Results((V)0.08d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_8, (V)"current"), R.Join(new V[] {}, new Results(v_161))).First, (V)"kind") == (V)"WindupStarted"))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_161}, new Results((V)0.55d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_8, (V)"current"), R.Join(new V[] {}, new Results(v_161))).First, (V)"kind") == (V)"AttackPerformed"))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"displacement without a route snaps safely and restored presentation is clean"}, new Results(new V((RuleFunction)(args_162 => {

var values_163 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_164 = values_163[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_164, R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)4.0d), new Entry((V)"kind", (V)"Displaced"), new Entry((V)"source_id", (V)1.0d), new Entry((V)"target_id", (V)7.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"from_x", (V)2.0d), new Entry((V)"from_y", (V)2.0d), new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)2.0d))))}, new Results((V)1.0d)));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_164, (V)7.0d, (V)"floor", (V)9.0d}, new Results((V)2.0d))))).First}, new Results((V)2.0d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_164}, new Results((V)0.04d)));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_164, (V)7.0d, (V)"floor", (V)9.0d}, new Results((V)2.0d))))).First}, new Results((V)2.0d)));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_164}, new Results((V)0.04d)));
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_164, (V)7.0d, (V)"floor", (V)9.0d}, new Results((V)2.0d))))).First}, new Results((V)9.0d)));
var values_165 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"new"), R.Join(new V[] {}, new Results((V)12.0d))));
V v_166 = values_165[0];
var values_167 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_166))))));
V v_168 = values_167[0];
R.Call(v_17, R.Join(new V[] {R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_166))).First}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_168)))));
var values_169 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_170 = values_169[0];
var values_171 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_6, (V)"player"), R.Join(new V[] {}, new Results(v_168))).First, (V)"position")));
V v_172 = values_171[0];
R.Call(v_28, R.Join(new V[] {R.Call(rt.Global("select"), R.Join(new V[] {(V)1.0d}, R.Call(R.Get(v_8, (V)"position"), R.Join(new V[] {v_170, R.Get(v_168, (V)"player_id"), R.Get(v_172, (V)"floor_id"), R.Get(v_172, (V)"x")}, new Results(R.Get(v_172, (V)"y")))))).First}, new Results(R.Get(v_172, (V)"x"))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"fog-hidden actor motion never exposes an unseen tile"}, new Results(new V((RuleFunction)(args_173 => {

var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_175 = values_174[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_175, R.Table(new Entry(1, R.Call(v_32, R.Join(new V[] {(V)9.0d, (V)20.0d, (V)3.0d, (V)21.0d, (V)3.0d}, new Results((V)0.0d)))))}, new Results((V)1.0d)));
V v_176 = V.Nil;
v_176 = new V((RuleFunction)(args_177 => {
V v_178 = args_177[0];
return R.Join(new V[] {}, new Results((V)(v_178 < (V)10.0d)));
return Results.Empty;
}));
var values_179 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"visible_position"), R.Join(new V[] {v_175, (V)9.0d, (V)"floor", (V)21.0d, (V)3.0d}, new Results(v_176))));
V v_180 = values_179[0];
V v_181 = values_179[1];
V v_182 = values_179[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_182)))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_175}, new Results((V)1.0d)));
var values_183 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_184 = values_183[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_184, R.Table(new Entry(1, R.Call(v_32, R.Join(new V[] {(V)9.0d, (V)9.0d, (V)3.0d, (V)10.0d, (V)3.0d}, new Results((V)0.0d)))))}, new Results((V)1.0d)));
var values_185 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"visible_position"), R.Join(new V[] {v_184, (V)9.0d, (V)"floor", (V)10.0d, (V)3.0d}, new Results(v_176))));
V v_186 = values_185[0];
V v_187 = values_185[1];
V v_188 = values_185[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_188)))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"camera follows visual coordinates with aligned rounded tile offsets"}, new Results(new V((RuleFunction)(args_189 => {

var values_190 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/scrolling_room"))).First, R.Table(new Entry((V)"x", (V)16.0d), new Entry((V)"y", (V)16.0d), new Entry((V)"width", (V)704.0d), new Entry((V)"height", (V)608.0d)), (V)32.0d, (V)3.0d}, new Results((V)3.0d))));
V v_191 = values_190[0];
var values_192 = R.Join(new V[] {}, new Results(R.Get(v_191, (V)"x")));
V v_193 = values_192[0];
R.Call(R.Get(v_2, (V)"update"), R.Join(new V[] {v_191, (V)0.08d, (V)18.5d}, new Results((V)3.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_191, (V)"x") > v_193), () => (V)(R.Get(v_191, (V)"x") < (V)(v_193 + (V)400.0d))))));
var values_194 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"tile_top_left"), R.Join(new V[] {v_191, (V)4.0d}, new Results((V)3.0d))));
V v_195 = values_194[0];
var values_196 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"tile_top_left"), R.Join(new V[] {v_191, (V)5.0d}, new Results((V)3.0d))));
V v_197 = values_196[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)(v_197 - v_195) == (V)32.0d), () => (V)((V)(v_195 % (V)1.0d) == (V)0.0d)))));
R.Call(R.Get(v_2, (V)"update"), R.Join(new V[] {v_191, (V)5.0d, (V)35.0d}, new Results((V)25.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_191, (V)"x") <= (V)768.0d), () => (V)(R.Get(v_191, (V)"y") <= (V)496.0d)))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"camera zoom centers the same tile and changes no world state"}, new Results(new V((RuleFunction)(args_198 => {

var values_199 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/scrolling_room"))));
V v_200 = values_199[0];
var values_201 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)16.0d), new Entry((V)"y", (V)16.0d), new Entry((V)"width", (V)704.0d), new Entry((V)"height", (V)608.0d))));
V v_202 = values_201[0];
var values_203 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {v_200, v_202, (V)32.0d, (V)18.0d}, new Results((V)13.0d))));
V v_204 = values_203[0];
var values_205 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"tile_top_left"), R.Join(new V[] {v_204, (V)18.0d}, new Results((V)13.0d))));
V v_206 = values_205[0];
R.Call(R.Get(v_2, (V)"set_tile"), R.Join(new V[] {v_204, (V)48.0d, (V)18.0d}, new Results((V)13.0d)));
var values_207 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"tile_top_left"), R.Join(new V[] {v_204, (V)18.0d}, new Results((V)13.0d))));
V v_208 = values_207[0];
var values_209 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"tile_top_left"), R.Join(new V[] {v_204, (V)19.0d}, new Results((V)13.0d))));
V v_210 = values_209[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)(v_208 - v_206) == (-(V)8.0d)), () => (V)((V)(v_210 - v_208) == (V)48.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_204, (V)"tile") == (V)48.0d), () => (V)((V)(R.Get(v_204, (V)"x") % (V)1.0d) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"set_tile"), v_204, (V)0.0d, (V)18.0d}, new Results((V)13.0d))).First)))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"positive player damage jolts only the world during its displayed beat"}, new Results(new V((RuleFunction)(args_211 => {

var values_212 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"new"), R.Join(new V[] {(V)12.0d}, new Results((V)"core:map/test_room"))));
V v_213 = values_212[0];
var values_214 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_213))));
V v_215 = values_214[0];
var values_216 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_217 = values_216[0];
var values_218 = R.Join(new V[] {}, new Results(R.Get(v_213, (V)"active_floor_id")));
V v_219 = values_218[0];
var values_220 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"target_id", R.Get(v_213, (V)"player_id")), new Entry((V)"actual_loss", (V)4.0d), new Entry((V)"floor_id", v_219), new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))));
V v_221 = values_220[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_217, R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"WindupStarted"), new Entry((V)"floor_id", v_219))), new Entry((V)2, v_221))}, new Results(R.Get(v_213, (V)"player_id"))));
var values_222 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"new"), Results.Empty));
V v_223 = values_222[0];
R.Call(R.Get(v_12, (V)"update"), R.Join(new V[] {v_223, v_217, (V)0.0d, R.Get(v_213, (V)"player_id")}, new Results(v_219)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_12, (V)"flash_alpha"), R.Join(new V[] {}, new Results(v_223))).First == (V)0.0d))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_217}, new Results((V)0.55d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"current"), R.Join(new V[] {}, new Results(v_217))).First == v_221))));
R.Call(R.Get(v_12, (V)"update"), R.Join(new V[] {v_223, v_217, (V)0.0d, R.Get(v_213, (V)"player_id")}, new Results(v_219)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_12, (V)"flash_alpha"), R.Join(new V[] {}, new Results(v_223))).First > (V)0.0d))));
var values_224 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {R.Call(R.Get(v_4, (V)"get_map"), R.Join(new V[] {}, new Results((V)"core:map/test_room"))).First, R.Table(new Entry((V)"x", (V)16.0d), new Entry((V)"y", (V)16.0d), new Entry((V)"width", (V)704.0d), new Entry((V)"height", (V)608.0d)), (V)32.0d, (V)2.0d}, new Results((V)2.0d))));
V v_225 = values_224[0];
var values_226 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"tile_top_left"), R.Join(new V[] {v_225, (V)2.0d}, new Results((V)2.0d))));
V v_227 = values_226[0];
V v_228 = values_226[1];
var values_229 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"offset"), R.Join(new V[] {}, new Results(v_223))));
V v_230 = values_229[0];
V v_231 = values_229[1];
R.Call(R.Get(v_2, (V)"set_presentation_offset"), R.Join(new V[] {v_225, v_230}, new Results(v_231)));
var values_232 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"tile_top_left"), R.Join(new V[] {v_225, (V)2.0d}, new Results((V)2.0d))));
V v_233 = values_232[0];
V v_234 = values_232[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)(v_233 - v_227) == v_230), () => (V)((V)(v_234 - v_228) == v_231)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_230))).First <= (V)5.0d), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_231))).First <= (V)5.0d)))));
var values_235 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"flash_alpha"), R.Join(new V[] {}, new Results(v_223))));
V v_236 = values_235[0];
R.Call(R.Get(v_12, (V)"update"), R.Join(new V[] {v_223, v_217, (V)0.0d, R.Get(v_213, (V)"player_id")}, new Results(v_219)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(R.Get(v_12, (V)"flash_alpha"), R.Join(new V[] {}, new Results(v_223))).First == v_236)}, new Results((V)"same playback beat must not restart the hit")));
R.Call(R.Get(v_12, (V)"update"), R.Join(new V[] {v_223, v_217, R.Get(v_12, (V)"flash_seconds"), R.Get(v_213, (V)"player_id")}, new Results(v_219)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_12, (V)"flash_alpha"), R.Join(new V[] {}, new Results(v_223))).First == (V)0.0d))));
var values_237 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"offset"), R.Join(new V[] {}, new Results(v_223))));
V v_238 = values_237[0];
V v_239 = values_237[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_238 == (V)0.0d), () => (V)(v_239 == (V)0.0d)))));
R.Call(v_17, R.Join(new V[] {v_215}, R.Call(R.Get(v_6, (V)"snapshot"), R.Join(new V[] {}, new Results(v_213)))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"blocked, enemy and old-floor hits never shake the player"}, new Results(new V((RuleFunction)(args_240 => {

var values_241 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"new"), Results.Empty));
V v_242 = values_241[0];
var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_244 = values_243[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_244, R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"DamageBlocked"), new Entry((V)"target_id", (V)1.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"target_id", (V)2.0d), new Entry((V)"actual_loss", (V)5.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"target_id", (V)1.0d), new Entry((V)"actual_loss", (V)5.0d), new Entry((V)"floor_id", (V)"retired"), new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))))}, new Results((V)1.0d)));
{ double end_247 = R.Num((V)3.0d), step_248 = R.Num((V)1.0d);
for (double i_246 = R.Num((V)1.0d); step_248 > 0 ? i_246 <= end_247 : i_246 >= end_247; i_246 += step_248) {
V v_245 = i_246;
R.Call(R.Get(v_12, (V)"update"), R.Join(new V[] {v_242, v_244, (V)0.0d, (V)1.0d}, new Results((V)"floor")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_12, (V)"flash_alpha"), R.Join(new V[] {}, new Results(v_242))).First == (V)0.0d))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_244}, new Results((V)0.2d)));
}
}
var values_249 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_250 = values_249[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_250, R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"target_id", (V)1.0d), new Entry((V)"actual_loss", (V)4.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"target_id", (V)1.0d), new Entry((V)"actual_loss", (V)9.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))))}, new Results((V)1.0d)));
R.Call(R.Get(v_12, (V)"update"), R.Join(new V[] {v_242, v_250, (V)0.0d, (V)1.0d}, new Results((V)"floor")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_242, (V)"strength") == (V)5.0d), () => (V)(R.Call(R.Get(v_12, (V)"flash_alpha"), R.Join(new V[] {}, new Results(v_242))).First > (V)0.0d)))));
R.Call(R.Get(v_12, (V)"update"), R.Join(new V[] {v_242, v_250, (V)0.0d, (V)1.0d}, new Results((V)"new-floor")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_12, (V)"flash_alpha"), R.Join(new V[] {}, new Results(v_242))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"long corridor ranged lane stays blocked by walls and strict timing is legible"}, new Results(new V((RuleFunction)(args_251 => {

var values_252 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"generate"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_253 = values_252[0];
var values_254 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_255 = values_254[0];
V v_256 = values_254[1];
foreach (var row_257 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_253, (V)"corridors"))))))) {
V v_258 = row_257[0];
V v_259 = row_257[1];
if (R.Truth((V)(R.Get(v_259, (V)"length") >= (V)24.0d))) {
var values_260 = R.Join(new V[] {}, new Results((V)(v_255 + (V)1.0d)));
v_255 = values_260[0];
var values_261 = R.Join(new V[] {}, new Results((V)(v_256 + (V)R.Length(R.Get(v_259, (V)"alcoves")))));
v_256 = values_261[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_255 >= (V)1.0d), () => (V)(v_256 >= (V)1.0d)))));
var values_262 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"new"), R.Join(new V[] {(V)10.0d}, new Results((V)"core:map/test_room"))));
V v_263 = values_262[0];
var values_264 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"spawn_enemy"), R.Join(new V[] {v_263, (V)"core:enemy/thornspitter", (V)9.0d}, new Results((V)2.0d))));
V v_265 = values_264[0];
var values_266 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"submit"), R.Join(new V[] {v_263}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_267 = values_266[0];
V v_268 = values_266[1];
V v_269 = values_266[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_267, () => (V)(R.Get(v_263, (V)"clock") == (V)100.0d)))));
var values_270 = Results.Empty;
V v_271 = values_270[0];
V v_272 = values_270[1];
foreach (var row_273 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_269)))))) {
V v_274 = row_273[0];
V v_275 = row_273[1];
if (R.Truth(R.And((V)(R.Get(v_275, (V)"kind") == (V)"WindupStarted"), () => (V)(R.Get(v_275, (V)"source_id") == R.Get(v_265, (V)"id"))))) {
var values_276 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"index", v_274), new Entry((V)"event", v_275))));
v_271 = values_276[0];
} else if (R.Truth(R.And((V)(R.Get(v_275, (V)"kind") == (V)"AttackPerformed"), () => (V)(R.Get(v_275, (V)"source_id") == R.Get(v_265, (V)"id"))))) {
var values_277 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"index", v_274), new Entry((V)"event", v_275))));
v_272 = values_277[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_271, () => v_272), () => (V)(R.Get(v_271, (V)"index") < R.Get(v_272, (V)"index"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_271, (V)"event"), (V)"due") == R.Get(R.Get(v_272, (V)"event"), (V)"time")), () => (V)(R.Get(R.Get(v_272, (V)"event"), (V)"time") == (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_271, (V)"event"), (V)"target_cells")) == (V)7.0d))));
var values_278 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_279 = values_278[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_279, v_269}, new Results(R.Get(v_263, (V)"player_id"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_8, (V)"current"), R.Join(new V[] {}, new Results(v_279))).First, (V)"kind") == (V)"WindupStarted"))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_279}, new Results((V)0.55d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_8, (V)"current"), R.Join(new V[] {}, new Results(v_279))).First, (V)"kind") == (V)"AttackPerformed"))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"consecutive locked windups share one visible beat without reordering impacts"}, new Results(new V((RuleFunction)(args_280 => {

var values_281 = R.Join(new V[] {}, new Results(R.Table()));
V v_282 = values_281[0];
{ double end_285 = R.Num((V)4.0d), step_286 = R.Num((V)1.0d);
for (double i_284 = R.Num((V)2.0d); step_286 > 0 ? i_284 <= end_285 : i_284 >= end_285; i_284 += step_286) {
V v_283 = i_284;
var values_287 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_283), new Entry((V)"kind", (V)"WindupStarted"), new Entry((V)"source_id", v_283), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"time", (V)100.0d), new Entry((V)"due", (V)200.0d), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", v_283), new Entry((V)"y", (V)3.0d))))))));
var obj_288 = v_282;
V key_289 = (V)((V)R.Length(v_282) + (V)1.0d);
R.Set(obj_288, key_289, values_287[0]);
}
}
var values_290 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)5.0d), new Entry((V)"kind", (V)"AttackPerformed"), new Entry((V)"source_id", (V)2.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"time", (V)200.0d), new Entry((V)"target_cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)3.0d))))))));
var obj_291 = v_282;
V key_292 = (V)((V)R.Length(v_282) + (V)1.0d);
R.Set(obj_291, key_292, values_290[0]);
var values_293 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_294 = values_293[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_294, v_282}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_294, (V)"items")) == (V)2.0d), () => (V)(R.Get(R.Get(R.Get(v_294, (V)"items"), (V)1.0d), (V)"duration") == (V)0.55d)))));
var values_295 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"current_group"), R.Join(new V[] {}, new Results(v_294))));
V v_296 = values_295[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(v_296) == (V)3.0d), () => (V)(R.Get(v_296, (V)1.0d) == R.Get(v_282, (V)1.0d))), () => (V)(R.Get(v_296, (V)2.0d) == R.Get(v_282, (V)2.0d))), () => (V)(R.Get(v_296, (V)3.0d) == R.Get(v_282, (V)3.0d))))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_294}, new Results((V)0.55d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_8, (V)"current"), R.Join(new V[] {}, new Results(v_294))).First == R.Get(v_282, (V)4.0d)), () => (V)(R.Call(R.Get(v_8, (V)"current_group"), R.Join(new V[] {}, new Results(v_294))).First == V.Nil)))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_294}, new Results((V)0.26d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_8, (V)"busy"), R.Join(new V[] {}, new Results(v_294))).First)))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"windup grouping respects intervening events and action boundaries"}, new Results(new V((RuleFunction)(args_297 => {

V v_298 = V.Nil;
v_298 = new V((RuleFunction)(args_299 => {
V v_300 = args_299[0];
V v_301 = args_299[1];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_300), new Entry((V)"kind", (V)"WindupStarted"), new Entry((V)"source_id", v_300), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"time", v_301), new Entry((V)"due", (V)(v_301 + (V)100.0d)))));
return Results.Empty;
}));
var values_302 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_303 = values_302[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_303, R.Table(new Entry((V)1, R.Call(v_298, R.Join(new V[] {(V)2.0d}, new Results((V)0.0d))).First), new Entry((V)2, R.Call(v_298, R.Join(new V[] {(V)3.0d}, new Results((V)40.0d))).First), new Entry((V)3, R.Call(v_32, R.Join(new V[] {(V)4.0d, (V)5.0d, (V)3.0d, (V)6.0d, (V)3.0d}, new Results((V)40.0d))).First), new Entry(4, R.Call(v_298, R.Join(new V[] {(V)5.0d}, new Results((V)40.0d)))))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_303, (V)"items")) == (V)3.0d), () => (V)((V)R.Length(R.Get(R.Get(R.Get(v_303, (V)"items"), (V)1.0d), (V)"events")) == (V)2.0d)))));
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_303, R.Table(new Entry(1, R.Call(v_298, R.Join(new V[] {(V)6.0d}, new Results((V)40.0d)))))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_303, (V)"items")) == (V)4.0d))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"dense same-kind boon cues share a bounded beat, with hits and deaths ordered"}, new Results(new V((RuleFunction)(args_304 => {

var values_305 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)1.0d), new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"time", (V)100.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))))));
V v_306 = values_305[0];
{ double end_309 = R.Num((V)41.0d), step_310 = R.Num((V)1.0d);
for (double i_308 = R.Num((V)2.0d); step_310 > 0 ? i_308 <= end_309 : i_308 >= end_309; i_308 += step_310) {
V v_307 = i_308;
var values_311 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_307), new Entry((V)"kind", (V)"BoonActivated"), new Entry((V)"time", (V)100.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d), new Entry((V)"boon_id", (V)"core:boon/storm_conductor"))));
var obj_312 = v_306;
V key_313 = (V)((V)R.Length(v_306) + (V)1.0d);
R.Set(obj_312, key_313, values_311[0]);
}
}
var values_314 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)42.0d), new Entry((V)"kind", (V)"Died"), new Entry((V)"time", (V)100.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))));
var obj_315 = v_306;
V key_316 = (V)((V)R.Length(v_306) + (V)1.0d);
R.Set(obj_315, key_316, values_314[0]);
var values_317 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_318 = values_317[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_318, v_306}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_318, (V)"items")) == (V)3.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_318, (V)"items"), (V)1.0d), (V)"event") == R.Get(v_306, (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_318, (V)"items"), (V)2.0d), (V)"duration") == R.Get(v_8, (V)"max_cascade_seconds")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_318, (V)"items"), (V)2.0d), (V)"events")) == (V)40.0d))));
foreach (var row_319 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_318, (V)"items"), (V)2.0d), (V)"events"))))))) {
V v_320 = row_319[0];
V v_321 = row_319[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_321 == R.Get(v_306, (V)(v_320 + (V)1.0d))))));
}
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_318}, new Results((V)0.2d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"current"), R.Join(new V[] {}, new Results(v_318))).First == R.Get(v_306, (V)2.0d)))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_318}, new Results(R.Get(v_8, (V)"max_cascade_seconds"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"current"), R.Join(new V[] {}, new Results(v_318))).First == R.Get(v_306, (V)R.Length(v_306))))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"one lethal target's hit and death share a beat without mixing other deaths"}, new Results(new V((RuleFunction)(args_322 => {

var values_323 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)1.0d), new Entry((V)"kind", (V)"DamageTaken"), new Entry((V)"target_id", (V)7.0d), new Entry((V)"time", (V)100.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))));
V v_324 = values_323[0];
var values_325 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)2.0d), new Entry((V)"kind", (V)"Died"), new Entry((V)"target_id", (V)7.0d), new Entry((V)"time", (V)100.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)2.0d))));
V v_326 = values_325[0];
var values_327 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)3.0d), new Entry((V)"kind", (V)"Died"), new Entry((V)"target_id", (V)8.0d), new Entry((V)"time", (V)100.0d), new Entry((V)"floor_id", (V)"floor"), new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)2.0d))));
V v_328 = values_327[0];
var values_329 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"new"), Results.Empty));
V v_330 = values_329[0];
R.Call(R.Get(v_8, (V)"add"), R.Join(new V[] {v_330, R.Table(new Entry((V)1, v_324), new Entry((V)2, v_326), new Entry((V)3, v_328))}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_330, (V)"items")) == (V)2.0d), () => (V)(R.Get(R.Get(R.Get(v_330, (V)"items"), (V)1.0d), (V)"duration") == (V)0.3d)))));
var values_331 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"current_group"), R.Join(new V[] {}, new Results(v_330))));
V v_332 = values_331[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_332) == (V)2.0d), () => (V)(R.Get(v_332, (V)1.0d) == v_324)), () => (V)(R.Get(v_332, (V)2.0d) == v_326)))));
R.Call(R.Get(v_8, (V)"update"), R.Join(new V[] {v_330}, new Results((V)0.3d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_8, (V)"current"), R.Join(new V[] {}, new Results(v_330))).First == v_328))));
return Results.Empty;
})))));
R.Call(v_16, R.Join(new V[] {(V)"generated ranged lanes retain lateral geometry across representative seeds"}, new Results(new V((RuleFunction)(args_333 => {

var values_334 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_335 = values_334[0];
var values_336 = R.Join(new V[] {}, new Results((V)0.0d));
V v_337 = values_336[0];
foreach (var row_338 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(R.Get(v_335, (V)"order"), (V)1.0d)), new Entry((V)2, R.Get(R.Get(v_335, (V)"order"), (V)3.0d))))))))) {
V v_339 = row_338[0];
V v_340 = row_338[1];
{ double end_343 = R.Num((V)20.0d), step_344 = R.Num((V)1.0d);
for (double i_342 = R.Num((V)1.0d); step_344 > 0 ? i_342 <= end_343 : i_342 >= end_343; i_342 += step_344) {
V v_341 = i_342;
var values_345 = R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"generate"), R.Join(new V[] {v_341}, new Results(R.Table(new Entry((V)"biome_id", v_340))))));
V v_346 = values_345[0];
foreach (var row_347 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_346, (V)"placements"), (V)"enemies"))))))) {
V v_348 = row_347[0];
V v_349 = row_347[1];
if (R.Truth(R.Or((V)(R.Get(v_349, (V)"definition_id") == (V)"core:enemy/thornspitter"), () => (V)(R.Get(v_349, (V)"definition_id") == (V)"core:enemy/canal_lancer")))) {
foreach (var row_350 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_351 = row_350[0];
V v_352 = row_350[1];
var values_353 = R.Join(new V[] {(V)0.0d}, new Results((V)false));
V v_354 = values_353[0];
V v_355 = values_353[1];
{ double end_358 = R.Num((V)7.0d), step_359 = R.Num((V)1.0d);
for (double i_357 = R.Num((V)1.0d); step_359 > 0 ? i_357 <= end_358 : i_357 >= end_358; i_357 += step_359) {
V v_356 = i_357;
var values_360 = R.Join(new V[] {(V)(R.Get(v_349, (V)"x") + (V)(v_356 * R.Get(v_352, (V)1.0d)))}, new Results((V)(R.Get(v_349, (V)"y") + (V)(v_356 * R.Get(v_352, (V)2.0d)))));
V v_361 = values_360[0];
V v_362 = values_360[1];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_4, (V)"projectile_passable"), R.Join(new V[] {v_346, v_361}, new Results(v_362))).First)))) {
break;
}
var values_363 = R.Join(new V[] {}, new Results(v_356));
v_354 = values_363[0];
var values_364 = R.Join(new V[] {}, new Results(R.Or(R.Or(v_355, () => R.Call(R.Get(v_4, (V)"walkable"), R.Join(new V[] {v_346, (V)(v_361 - R.Get(v_352, (V)2.0d))}, new Results((V)(v_362 + R.Get(v_352, (V)1.0d))))).First), () => R.Call(R.Get(v_4, (V)"walkable"), R.Join(new V[] {v_346, (V)(v_361 + R.Get(v_352, (V)2.0d))}, new Results((V)(v_362 - R.Get(v_352, (V)1.0d))))).First)));
v_355 = values_364[0];
}
}
if (R.Truth((V)(v_354 >= (V)5.0d))) {
var values_365 = R.Join(new V[] {}, new Results((V)(v_337 + (V)1.0d)));
v_337 = values_365[0];
R.Call(rt.Global("assert"), R.Join(new V[] {v_355}, R.Invoke((V)"no lateral exit %s seed %d enemy %s", "format", R.Join(new V[] {v_340, v_341}, new Results(R.Get(v_349, (V)"id"))))));
}
}
}
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_337 >= (V)100.0d)}, new Results((V)"corpus did not exercise enough ranged lanes")));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
