// Static C# port of src/idea_stormcaller.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 510c5dcae1c78800338d683105030ecd5c806c43b8d1e8fb39aeb3262997a56c
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_stormcaller { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:actor/stormcaller"), new Entry((V)"cap", (V)3.0d), new Entry((V)"delay", (V)100.0d), new Entry((V)"symbol", (V)615.0d))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_4, (V)"id")), new Entry((V)"name", (V)"Stormcaller"), new Entry((V)"weapon_icon", (V)"spell"), new Entry((V)"description", (V)"Build charge on bolt hits; discharge a locked cross or Dash to ground a ward"), new Entry((V)"hp", (V)21.0d), new Entry((V)"damage", (V)2.0d), new Entry((V)"slash_damage", (V)2.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)100.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)100.0d), new Entry((V)"sweeping_slash", (V)150.0d), new Entry((V)"dash", (V)130.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/stormcaller_bolt"), new Entry((V)"special_id", (V)"core:ability/stormcaller_discharge"), new Entry((V)"basic_name", (V)"Charging Bolt"), new Entry((V)"special_name", (V)"Locked Discharge"), new Entry((V)"dash_name", (V)"Grounding Dash"), new Entry((V)"ui_controls", (V)"F Charge bolt \u00c2\u00b7 R Discharge \u00c2\u00b7 X Ground ward"))));
var obj_6 = v_4;
V key_7 = (V)"definition";
R.Set(obj_6, key_7, values_5[0]);
R.Set(v_4, (V)"on_event", new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
V v_10 = args_8[1];
V v_11 = args_8[2];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_10, (V)"kind") != (V)"HitConfirmed"), () => (V)(R.Get(v_10, (V)"source_id") != R.Get(v_9, (V)"player_id"))), () => (V)(R.Get(v_10, (V)"ability_id") != R.Get(R.Get(v_4, (V)"definition"), (V)"basic_id"))), () => (V)(!R.Truth(R.Get(v_10, (V)"direct_attack")))), () => (V)(R.Get(v_10, (V)"actual_loss") <= (V)0.0d)), () => R.Get(v_10, (V)"class_generated")), () => (V)(R.Get(v_10, (V)"proc_coefficient") <= (V)0.0d)))) {
return Results.Empty;
}
var values_12 = Results.Empty;
V v_13 = values_12[0];

foreach (var row_14 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_9, (V)"entities"))))))) {
V v_15 = row_14[0];
V v_16 = row_14[1];
if (R.Truth((V)(R.Get(v_16, (V)"id") == R.Get(v_9, (V)"player_id")))) {
var values_17 = R.Join(new V[] {}, new Results(v_16));
v_13 = values_17[0];

break;
}
}
if (R.Truth(R.Or((V)(R.Get(v_13, (V)"definition_id") != R.Get(v_4, (V)"id")), () => (V)(R.Get(v_13, (V)"storm_last_action") == R.Get(v_10, (V)"action_id"))))) {
return Results.Empty;
}
var values_18 = R.Join(new V[] {}, new Results(R.Get(v_10, (V)"action_id")));
var obj_19 = v_13;
V key_20 = (V)"storm_last_action";
R.Set(obj_19, key_20, values_18[0]);

var values_21 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_4, (V)"cap")}, new Results((V)(R.Or(R.Get(v_13, (V)"storm_charge"), () => (V)0.0d) + (V)1.0d)))));
var obj_22 = v_13;
V key_23 = (V)"storm_charge";
R.Set(obj_22, key_23, values_21[0]);
R.Call(v_11, R.Join(new V[] {(V)"StormChargeGained"}, new Results(R.Table(new Entry((V)"charge", R.Get(v_13, (V)"storm_charge")), new Entry((V)"floor_id", R.Get(R.Get(v_13, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_13, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_13, (V)"position"), (V)"y"))))));
return Results.Empty;
})));
R.Set(v_4, (V)"ground", new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
V v_26 = args_24[1];
if (R.Truth(R.And((V)(R.Get(v_25, (V)"definition_id") == R.Get(v_4, (V)"id")), () => (V)(R.Or(R.Get(v_25, (V)"storm_charge"), () => (V)0.0d) > (V)0.0d)))) {
var values_27 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_28 = v_25;
V key_29 = (V)"storm_charge";
R.Set(obj_28, key_29, values_27[0]);

var values_30 = R.Join(new V[] {}, new Results((V)true));
var obj_31 = v_25;
V key_32 = (V)"storm_ward";
R.Set(obj_31, key_32, values_30[0]);
R.Call(v_26, R.Join(new V[] {(V)"StormGrounded"}, new Results(R.Table(new Entry((V)"floor_id", R.Get(R.Get(v_25, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_25, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_25, (V)"position"), (V)"y"))))));
}
return Results.Empty;
})));
R.Set(v_4, (V)"absorb", new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
V v_35 = args_33[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_34, (V)"definition_id") == R.Get(v_4, (V)"id")), () => R.Get(v_34, (V)"storm_ward")), () => (V)(v_35 > (V)0.0d)))) {
var values_36 = R.Join(new V[] {}, new Results(V.Nil));
var obj_37 = v_34;
V key_38 = (V)"storm_ward";
R.Set(obj_37, key_38, values_36[0]);

return R.Join(new V[] {}, new Results((V)true));
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_4, (V)"lock", new V((RuleFunction)(args_39 => {
V v_40 = args_39[0];
V v_41 = args_39[1];
V v_42 = args_39[2];
V v_43 = args_39[3];
V v_44 = args_39[4];
var values_45 = R.Join(new V[] {}, new Results(R.Get(v_41, (V)"storm_charge")));
V v_46 = values_45[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_46, () => (V)(v_46 > (V)0.0d)))));
var values_47 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_48 = v_41;
V key_49 = (V)"storm_charge";
R.Set(obj_48, key_49, values_47[0]);
var values_50 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"enqueue"), R.Join(new V[] {v_40}, new Results(R.Table(new Entry((V)"kind", (V)"effect"), new Entry((V)"due", (V)(R.Get(v_40, (V)"clock") + R.Get(v_4, (V)"delay"))), new Entry((V)"actor_id", (V)0.0d), new Entry((V)"attack_id", v_43), new Entry((V)"floor_id", R.Get(R.Get(v_41, (V)"position"), (V)"floor_id")))))));
V v_51 = values_50[0];
var values_52 = R.Join(new V[] {}, new Results(R.Get(v_51, (V)"id")));
var obj_53 = v_51;
V key_54 = (V)"effect_id";
R.Set(obj_53, key_54, values_52[0]);

var values_55 = R.Join(new V[] {}, new Results((V)true));
var obj_56 = v_51;
V key_57 = (V)"idea_storm";
R.Set(obj_56, key_57, values_55[0]);
var values_58 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"schedule_id", R.Get(v_51, (V)"id")), new Entry((V)"due", R.Get(v_51, (V)"due")), new Entry((V)"action_id", v_43), new Entry((V)"owner_id", R.Get(v_41, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_41, (V)"position"), (V)"floor_id")), new Entry((V)"center_x", R.Get(R.Get(v_42, (V)1.0d), (V)"x")), new Entry((V)"center_y", R.Get(R.Get(v_42, (V)1.0d), (V)"y")), new Entry((V)"cells", v_42), new Entry((V)"damage", (V)(R.Get(v_41, (V)"slash_damage") + (V)((V)2.0d * v_46))), new Entry((V)"charge", v_46))));
var obj_59 = R.Get(v_40, (V)"class_special");
V key_60 = (V)"storm";
R.Set(obj_59, key_60, values_58[0]);
R.Call(v_44, R.Join(new V[] {(V)"StormDischargeLocked"}, new Results(R.Table(new Entry((V)"target_cells", v_42), new Entry((V)"due", R.Get(v_51, (V)"due")), new Entry((V)"charge", v_46), new Entry((V)"damage", (V)(R.Get(v_41, (V)"slash_damage") + (V)((V)2.0d * v_46))), new Entry((V)"floor_id", R.Get(R.Get(v_41, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(R.Get(v_41, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_41, (V)"position"), (V)"y"))))));
return Results.Empty;
})));
R.Set(v_4, (V)"resolve", new V((RuleFunction)(args_61 => {
V v_62 = args_61[0];
V v_63 = args_61[1];
V v_64 = args_61[2];
var values_65 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_62, (V)"class_special"), (V)"storm")}, new Results((V)"orphan storm work"))));
V v_66 = values_65[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_66, (V)"schedule_id") == R.Get(v_63, (V)"id")), () => (V)(R.Get(v_66, (V)"due") == R.Get(v_62, (V)"clock"))), () => (V)(R.Get(v_66, (V)"floor_id") == R.Get(v_62, (V)"active_floor_id")))}, new Results((V)"mistimed storm")));
var values_67 = R.Join(new V[] {}, new Results(V.Nil));
var obj_68 = R.Get(v_62, (V)"class_special");
V key_69 = (V)"storm";
R.Set(obj_68, key_69, values_67[0]);

R.Call(v_64, R.Join(new V[] {}, new Results(v_66)));
return Results.Empty;
})));
R.Set(v_4, (V)"validate", new V((RuleFunction)(args_70 => {
V v_71 = args_70[0];
V v_72 = args_70[1];
var values_73 = Results.Empty;
V v_74 = values_73[0];

foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_71, (V)"entities"))))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
if (R.Truth((V)(R.Get(v_77, (V)"id") == R.Get(v_71, (V)"player_id")))) {
var values_78 = R.Join(new V[] {}, new Results(v_77));
v_74 = values_78[0];
}
if (R.Truth(R.Or(R.Or((V)(R.Get(v_77, (V)"storm_charge") != V.Nil), () => (V)(R.Get(v_77, (V)"storm_ward") != V.Nil)), () => (V)(R.Get(v_77, (V)"storm_last_action") != V.Nil)))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_77, (V)"id") == R.Get(v_71, (V)"player_id")), () => (V)(R.Get(v_77, (V)"definition_id") == R.Get(v_4, (V)"id")))}, new Results((V)"storm resource on wrong actor")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_77, (V)"storm_charge") == V.Nil), () => R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_77, (V)"storm_charge")))).First == (V)"number"), () => (V)((V)(R.Get(v_77, (V)"storm_charge") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_77, (V)"storm_charge") >= (V)0.0d)), () => (V)(R.Get(v_77, (V)"storm_charge") <= R.Get(v_4, (V)"cap"))))}, new Results((V)"invalid storm charge")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_77, (V)"storm_ward") == V.Nil), () => (V)(R.Get(v_77, (V)"storm_ward") == (V)true))}, new Results((V)"invalid storm ward")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_77, (V)"storm_last_action") == V.Nil), () => R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_77, (V)"storm_last_action")))).First == (V)"number"), () => (V)((V)(R.Get(v_77, (V)"storm_last_action") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_77, (V)"storm_last_action") >= (V)1.0d)), () => (V)(R.Get(v_77, (V)"storm_last_action") < R.Get(v_71, (V)"next_action_id"))))}, new Results((V)"invalid storm charge history")));
}
}
var values_79 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_71, (V)"class_special"), (V)"storm")));
V v_80 = values_79[0];

var values_81 = R.Join(new V[] {}, new Results((V)false));
V v_82 = values_81[0];
var values_83 = R.Join(new V[] {}, new Results(R.Table()));
V v_84 = values_83[0];

foreach (var row_85 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_71, (V)"schedule"))))))) {
V v_86 = row_85[0];
V v_87 = row_85[1];
var values_88 = R.Join(new V[] {}, new Results(v_87));
var obj_89 = v_84;
V key_90 = (V)((V)R.Length(v_84) + (V)1.0d);
R.Set(obj_89, key_90, values_88[0]);
}
foreach (var row_91 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_71, (V)"floor_pause"), () => R.Table()))))))) {
V v_92 = row_91[0];
V v_93 = row_91[1];
foreach (var row_94 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_93, (V)"schedule"))))))) {
V v_95 = row_94[0];
V v_96 = row_94[1];
var values_97 = R.Join(new V[] {}, new Results(v_96));
var obj_98 = v_84;
V key_99 = (V)((V)R.Length(v_84) + (V)1.0d);
R.Set(obj_98, key_99, values_97[0]);
}
}
foreach (var row_100 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_84)))))) {
V v_101 = row_100[0];
V v_102 = row_100[1];
if (R.Truth(R.Get(v_102, (V)"idea_storm"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(!R.Truth(v_82)), () => v_80), () => (V)(R.Get(v_102, (V)"idea_storm") == (V)true)), () => (V)(R.Get(v_102, (V)"kind") == (V)"effect")), () => (V)(R.Get(v_102, (V)"actor_id") == (V)0.0d)), () => (V)(R.Get(v_102, (V)"effect_id") == R.Get(v_102, (V)"id"))), () => (V)(R.Get(v_102, (V)"id") == R.Get(v_80, (V)"schedule_id"))), () => (V)(R.Get(v_102, (V)"due") == R.Get(v_80, (V)"due"))), () => (V)(R.Get(v_102, (V)"floor_id") == R.Get(v_80, (V)"floor_id")))}, new Results((V)"orphan storm schedule")));

var values_103 = R.Join(new V[] {}, new Results((V)true));
v_82 = values_103[0];
}
}
if (R.Truth(v_80)) {
var values_104 = R.Join(new V[] {}, new Results(R.Get(v_72, R.Get(v_80, (V)"floor_id"))));
V v_105 = values_104[0];

var values_106 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Or(R.Get(R.Or(R.Get(v_71, (V)"floor_pause"), () => R.Table()), R.Get(v_80, (V)"floor_id")), () => R.Table()), (V)"suspended_at"), () => R.Get(v_71, (V)"clock"))));
V v_107 = values_106[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_82, () => (V)(R.Get(v_74, (V)"definition_id") == R.Get(v_4, (V)"id"))), () => (V)(R.Get(v_80, (V)"owner_id") == R.Get(v_74, (V)"id"))), () => v_105), () => (V)(R.Get(v_80, (V)"due") >= v_107)), () => (V)(R.Get(v_80, (V)"due") <= (V)(v_107 + R.Get(v_4, (V)"delay")))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_80, (V)"damage")))).First == (V)"number")), () => (V)((V)(R.Get(v_80, (V)"damage") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_80, (V)"damage") >= (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_80, (V)"charge")))).First == (V)"number")), () => (V)((V)(R.Get(v_80, (V)"charge") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_80, (V)"charge") >= (V)1.0d)), () => (V)(R.Get(v_80, (V)"charge") <= R.Get(v_4, (V)"cap"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_80, (V)"cells")))).First == (V)"table")), () => (V)((V)R.Length(R.Get(v_80, (V)"cells")) >= (V)1.0d)), () => (V)((V)R.Length(R.Get(v_80, (V)"cells")) <= (V)5.0d))}, new Results((V)"invalid locked storm")));
var values_108 = R.Join(new V[] {}, new Results(R.Table()));
V v_109 = values_108[0];

foreach (var row_110 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_80, (V)"cells"))))))) {
V v_111 = row_110[0];
V v_112 = row_110[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_112, (V)"floor_id") == R.Get(v_80, (V)"floor_id")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_112, (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(v_112, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_112, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_112, (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_112, (V)"x") >= (V)1.0d)), () => (V)(R.Get(v_112, (V)"x") <= R.Get(v_105, (V)"width"))), () => (V)(R.Get(v_112, (V)"y") >= (V)1.0d)), () => (V)(R.Get(v_112, (V)"y") <= R.Get(v_105, (V)"height")))}, new Results((V)"invalid storm cell")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_80, (V)"center_x")))).First == (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_80, (V)"center_y")))).First == (V)"number")), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_112, (V)"x") - R.Get(v_80, (V)"center_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_112, (V)"y") - R.Get(v_80, (V)"center_y"))))).First) <= (V)1.0d))}, new Results((V)"invalid storm cross")));
var values_113 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_112, (V)"x"), R.Concat((V)",", R.Get(v_112, (V)"y")))));
V v_114 = values_113[0];

R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_109, v_114)))}, new Results((V)"duplicate storm cell")));

var values_115 = R.Join(new V[] {}, new Results((V)true));
var obj_116 = v_109;
V key_117 = v_114;
R.Set(obj_116, key_117, values_115[0]);
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
