// Static C# port of src/terrain.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 5edf053b9b87e55325dbb2d10ab62bf5b85a50bfa288c1b1cbb840dc24ab926f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_terrain { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"version", (V)"roag-terrain/1"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"b"), new Entry((V)2, (V)"g"), new Entry((V)3, (V)"i"), new Entry((V)4, (V)"B"), new Entry((V)5, (V)"m"), new Entry((V)6, (V)"c"))));
var obj_4 = v_2;
V key_5 = (V)"order";
R.Set(obj_4, key_5, values_3[0]);
var values_6 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)".", R.Table(new Entry((V)"id", (V)"core:terrain/floor"), new Entry((V)"name", (V)"Floor"), new Entry((V)"walkable", (V)true), new Entry((V)"blocks_vision", (V)false), new Entry((V)"projectile_passable", (V)true))), new Entry((V)"#", R.Table(new Entry((V)"id", (V)"core:terrain/wall"), new Entry((V)"name", (V)"Wall"), new Entry((V)"walkable", (V)false), new Entry((V)"blocks_vision", (V)true), new Entry((V)"projectile_passable", (V)false))), new Entry((V)"~", R.Table(new Entry((V)"id", (V)"core:terrain/water"), new Entry((V)"name", (V)"Shallow water"), new Entry((V)"walkable", (V)false), new Entry((V)"blocks_vision", (V)false), new Entry((V)"projectile_passable", (V)true))), new Entry((V)"b", R.Table(new Entry((V)"id", (V)"core:terrain/bramble_bed"), new Entry((V)"name", (V)"Bramble bed"), new Entry((V)"walkable", (V)true), new Entry((V)"blocks_vision", (V)false), new Entry((V)"projectile_passable", (V)true), new Entry((V)"contact_damage", (V)1.0d))), new Entry((V)"g", R.Table(new Entry((V)"id", (V)"core:terrain/sticky_bog"), new Entry((V)"name", (V)"Sticky bog"), new Entry((V)"walkable", (V)true), new Entry((V)"blocks_vision", (V)false), new Entry((V)"projectile_passable", (V)true), new Entry((V)"walk_extra", (V)40.0d), new Entry((V)"dash_extra", (V)20.0d))), new Entry((V)"i", R.Table(new Entry((V)"id", (V)"core:terrain/glazed_ice"), new Entry((V)"name", (V)"Glazed ice"), new Entry((V)"walkable", (V)true), new Entry((V)"blocks_vision", (V)false), new Entry((V)"projectile_passable", (V)true), new Entry((V)"dash_extend", (V)true))), new Entry((V)"B", R.Table(new Entry((V)"id", (V)"core:terrain/barricade"), new Entry((V)"name", (V)"Breakable barricade"), new Entry((V)"walkable", (V)false), new Entry((V)"blocks_vision", (V)true), new Entry((V)"projectile_passable", (V)false), new Entry((V)"breakable", (V)true))), new Entry((V)"m", R.Table(new Entry((V)"id", (V)"core:terrain/obscuring_mist"), new Entry((V)"name", (V)"Obscuring mist"), new Entry((V)"walkable", (V)true), new Entry((V)"blocks_vision", (V)true), new Entry((V)"projectile_passable", (V)true))), new Entry((V)"c", R.Table(new Entry((V)"id", (V)"core:terrain/resonant_crystal"), new Entry((V)"name", (V)"Resonant crystal"), new Entry((V)"walkable", (V)true), new Entry((V)"blocks_vision", (V)false), new Entry((V)"projectile_passable", (V)true), new Entry((V)"crystal", (V)true))))));
var obj_7 = v_2;
V key_8 = (V)"definitions";
R.Set(obj_7, key_8, values_6[0]);
R.Set(v_2, (V)"get", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"definitions"), v_10)));
return Results.Empty;
})));
R.Set(v_2, (V)"symbol", new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
V v_13 = args_11[1];
V v_14 = args_11[2];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(!R.Truth(v_12)), () => (V)(v_13 < (V)1.0d)), () => (V)(v_13 > R.Get(v_12, (V)"width"))), () => (V)(v_14 < (V)1.0d)), () => (V)(v_14 > R.Get(v_12, (V)"height"))))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
return R.Join(new V[] {}, R.Invoke(R.Get(R.Get(v_12, (V)"rows"), v_14), "sub", R.Join(new V[] {v_13}, new Results(v_13))));
return Results.Empty;
})));
R.Set(v_2, (V)"at", new V((RuleFunction)(args_15 => {
V v_16 = args_15[0];
V v_17 = args_15[1];
V v_18 = args_15[2];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"symbol"), R.Join(new V[] {v_16, v_17}, new Results(v_18))))));
return Results.Empty;
})));
R.Set(v_2, (V)"is_valid", new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_20))).First != V.Nil)));
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_21 => {

var values_22 = R.Join(new V[] {}, new Results(R.Table()));
V v_23 = values_22[0];
foreach (var row_24 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"definitions"))))))) {
V v_25 = row_24[0];
V v_26 = row_24[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_25))).First == (V)"string"), () => (V)((V)R.Length(v_25) == (V)1.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_26))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_26, (V)"id")))).First == (V)"string")), () => (V)(!R.Truth(R.Get(v_23, R.Get(v_26, (V)"id")))))}, new Results((V)"invalid terrain identity")));
var values_27 = R.Join(new V[] {}, new Results((V)true));
var obj_28 = v_23;
V key_29 = R.Get(v_26, (V)"id");
R.Set(obj_28, key_29, values_27[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_26, (V)"walkable")))).First == (V)"boolean"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_26, (V)"blocks_vision")))).First == (V)"boolean")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_26, (V)"projectile_passable")))).First == (V)"boolean"))}, new Results((V)"invalid terrain collision flags")));
if (R.Truth(R.Get(v_26, (V)"contact_damage"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Get(v_26, (V)"walkable"), () => (V)(R.Get(v_26, (V)"contact_damage") > (V)0.0d)), () => (V)((V)(R.Get(v_26, (V)"contact_damage") % (V)1.0d) == (V)0.0d))}, new Results((V)"invalid contact hazard")));
}
if (R.Truth(R.Or(R.Get(v_26, (V)"walk_extra"), () => R.Get(v_26, (V)"dash_extra")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Get(v_26, (V)"walkable"), () => (V)(R.Or(R.Get(v_26, (V)"walk_extra"), () => (V)0.0d) >= (V)0.0d)), () => (V)(R.Or(R.Get(v_26, (V)"dash_extra"), () => (V)0.0d) >= (V)0.0d))}, new Results((V)"invalid sticky terrain")));
}
if (R.Truth(R.Get(v_26, (V)"dash_extend"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_26, (V)"walkable")}, new Results((V)"ice must be walkable")));
}
if (R.Truth(R.Get(v_26, (V)"breakable"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_26, (V)"walkable")))}, new Results((V)"barricade must block")));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_2, (V)"move_extra", new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
V v_32 = args_30[1];
V v_33 = args_30[2];
var values_34 = R.Join(new V[] {}, new Results((V)0.0d));
V v_35 = values_34[0];
foreach (var row_36 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_33, () => R.Table()))))))) {
V v_37 = row_36[0];
V v_38 = row_36[1];
if (R.Truth((V)(R.Get(v_38, (V)"status") == (V)"open"))) {
var values_39 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"at"), R.Join(new V[] {v_31, R.Get(v_38, (V)"x")}, new Results(R.Get(v_38, (V)"y")))));
V v_40 = values_39[0];
var values_41 = R.Join(new V[] {}, new Results((V)(v_35 + R.Or(R.And((V)(v_32 == (V)"dash"), () => R.Or(R.And(v_40, () => R.Get(v_40, (V)"dash_extra")), () => (V)0.0d)), () => R.Or(R.And(v_40, () => R.Get(v_40, (V)"walk_extra")), () => (V)0.0d)))));
v_35 = values_41[0];
}
}
return R.Join(new V[] {}, new Results(v_35));
return Results.Empty;
})));
R.Set(v_2, (V)"can_extend_dash", new V((RuleFunction)(args_42 => {
V v_43 = args_42[0];
V v_44 = args_42[1];
{ double end_47 = R.Num(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)2.0d}, new Results((V)R.Length(v_44)))).First), step_48 = R.Num((V)1.0d);
for (double i_46 = R.Num((V)1.0d); step_48 > 0 ? i_46 <= end_47 : i_46 >= end_47; i_46 += step_48) {
V v_45 = i_46;
var values_49 = R.Join(new V[] {}, new Results(R.Get(v_44, v_45)));
V v_50 = values_49[0];
if (R.Truth((V)(R.Get(v_50, (V)"status") == (V)"open"))) {
var values_51 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"at"), R.Join(new V[] {v_43, R.Get(v_50, (V)"x")}, new Results(R.Get(v_50, (V)"y")))));
V v_52 = values_51[0];
if (R.Truth(R.And(v_52, () => R.Get(v_52, (V)"dash_extend")))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_2, (V)"on_event", new V((RuleFunction)(args_53 => {
V v_54 = args_53[0];
V v_55 = args_53[1];
V v_56 = args_53[2];
V v_57 = args_53[3];
V v_58 = args_53[4];
V v_59 = args_53[5];
V v_60 = args_53[6];
if (R.Truth((V)(R.Get(v_56, (V)"floor_id") != R.Get(v_55, (V)"floor_id")))) {
return Results.Empty;
}
var values_61 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"symbol"), R.Join(new V[] {v_55, R.Or(R.Or(R.Get(v_56, (V)"x"), () => R.Get(v_56, (V)"target_x")), () => (-(V)1.0d))}, new Results(R.Or(R.Or(R.Get(v_56, (V)"y"), () => R.Get(v_56, (V)"target_y")), () => (-(V)1.0d))))));
V v_62 = values_61[0];
if (R.Truth(R.And((V)(R.Get(v_56, (V)"kind") == (V)"TileEntered"), () => (V)(v_62 == (V)"b")))) {
var values_63 = R.Join(new V[] {}, new Results(R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_56, (V)"action_id")))).First, R.Concat((V)":", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Get(v_56, (V)"source_id")))).First))));
V v_64 = values_63[0];
if (R.Truth(R.Get(v_57, v_64))) {
return Results.Empty;
}
var values_65 = R.Join(new V[] {}, new Results((V)true));
var obj_66 = v_57;
V key_67 = v_64;
R.Set(obj_66, key_67, values_65[0]);
var values_68 = R.Join(new V[] {}, R.Call(v_58, R.Join(new V[] {}, new Results(R.Get(v_56, (V)"source_id")))));
V v_69 = values_68[0];
if (R.Truth(R.And(R.And(v_69, () => R.Get(v_69, (V)"hp")), () => (V)(R.Get(v_69, (V)"hp") > (V)0.0d)))) {
R.Call(v_59, R.Join(new V[] {(V)"TerrainTriggered", R.Table(new Entry((V)"terrain_id", R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results((V)"b"))).First, (V)"id")), new Entry((V)"floor_id", R.Get(v_56, (V)"floor_id")), new Entry((V)"x", R.Get(v_56, (V)"x")), new Entry((V)"y", R.Get(v_56, (V)"y")), new Entry((V)"target_id", R.Get(v_69, (V)"id"))), (V)0.0d}, new Results(R.Get(v_56, (V)"action_id"))));
R.Call(v_60, R.Join(new V[] {v_69, (V)0.0d, R.Get(v_56, (V)"action_id"), (V)1.0d}, new Results((V)"bramble")));
}
} else if (R.Truth((V)(R.Get(v_56, (V)"kind") == (V)"AttackPerformed"))) {
foreach (var row_70 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_56, (V)"target_cells"), () => R.Table()))))))) {
V v_71 = row_70[0];
V v_72 = row_70[1];
if (R.Truth(R.And((V)(R.Get(v_72, (V)"floor_id") == R.Get(v_55, (V)"floor_id")), () => (V)(R.Call(R.Get(v_2, (V)"symbol"), R.Join(new V[] {v_55, R.Get(v_72, (V)"x")}, new Results(R.Get(v_72, (V)"y")))).First == (V)"B")))) {
var values_73 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_72, (V)"x"), R.Concat((V)",", R.Get(v_72, (V)"y")))));
V v_74 = values_73[0];
var values_75 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_55, (V)"rows"), R.Get(v_72, (V)"y")), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_72, (V)"x") - (V)1.0d)))).First, R.Concat((V)".", R.Invoke(R.Get(R.Get(v_55, (V)"rows"), R.Get(v_72, (V)"y")), "sub", R.Join(new V[] {}, new Results((V)(R.Get(v_72, (V)"x") + (V)1.0d)))).First))));
var obj_76 = R.Get(v_55, (V)"rows");
V key_77 = R.Get(v_72, (V)"y");
R.Set(obj_76, key_77, values_75[0]);
var values_78 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_55, (V)"broken_barricades"), () => R.Table())));
var obj_79 = v_55;
V key_80 = (V)"broken_barricades";
R.Set(obj_79, key_80, values_78[0]);
var values_81 = R.Join(new V[] {}, new Results((V)true));
var obj_82 = R.Get(v_55, (V)"broken_barricades");
V key_83 = v_74;
R.Set(obj_82, key_83, values_81[0]);
if (R.Truth(R.Get(v_55, (V)"studio_visuals"))) {
var values_84 = R.Join(new V[] {}, new Results(V.Nil));
var obj_85 = R.Get(v_55, (V)"studio_visuals");
V key_86 = v_74;
R.Set(obj_85, key_86, values_84[0]);
}
R.Call(v_59, R.Join(new V[] {(V)"TerrainBroken", R.Table(new Entry((V)"terrain_id", R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results((V)"B"))).First, (V)"id")), new Entry((V)"floor_id", R.Get(v_72, (V)"floor_id")), new Entry((V)"x", R.Get(v_72, (V)"x")), new Entry((V)"y", R.Get(v_72, (V)"y"))), R.Get(v_56, (V)"source_id")}, new Results(R.Get(v_56, (V)"action_id"))));
}
}
} else if (R.Truth(R.And(R.And((V)(R.Get(v_56, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_56, (V)"damage_type") == (V)"lightning")), () => (V)(R.Or(R.Get(v_56, (V)"actual_loss"), () => (V)0.0d) > (V)0.0d)))) {
var values_87 = Results.Empty;
V v_88 = values_87[0];
{ double end_91 = R.Num((V)(R.Get(v_56, (V)"y") + (V)1.0d)), step_92 = R.Num((V)1.0d);
for (double i_90 = R.Num((V)(R.Get(v_56, (V)"y") - (V)1.0d)); step_92 > 0 ? i_90 <= end_91 : i_90 >= end_91; i_90 += step_92) {
V v_89 = i_90;
{ double end_95 = R.Num((V)(R.Get(v_56, (V)"x") + (V)1.0d)), step_96 = R.Num((V)1.0d);
for (double i_94 = R.Num((V)(R.Get(v_56, (V)"x") - (V)1.0d)); step_96 > 0 ? i_94 <= end_95 : i_94 >= end_95; i_94 += step_96) {
V v_93 = i_94;
if (R.Truth((V)(R.Call(R.Get(v_2, (V)"symbol"), R.Join(new V[] {v_55, v_93}, new Results(v_89))).First == (V)"c"))) {
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_88)), () => (V)(v_89 < R.Get(v_88, (V)"y"))), () => R.And((V)(v_89 == R.Get(v_88, (V)"y")), () => (V)(v_93 < R.Get(v_88, (V)"x")))))) {
var values_97 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_93), new Entry((V)"y", v_89))));
v_88 = values_97[0];
}
}
}
}
}
}
if (R.Truth(v_88)) {
var values_98 = Results.Empty;
V v_99 = values_98[0];
V v_100 = values_98[1];
foreach (var row_101 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_54, (V)"entities"))))))) {
V v_102 = row_101[0];
V v_103 = row_101[1];
var values_104 = R.Join(new V[] {}, new Results(R.Get(v_103, (V)"position")));
V v_105 = values_104[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_103, (V)"id") != R.Get(v_56, (V)"target_id")), () => R.Get(v_103, (V)"hp")), () => (V)(R.Get(v_103, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_103, (V)"faction") != R.Get(v_56, (V)"source_faction"))), () => v_105), () => (V)(R.Get(v_105, (V)"floor_id") == R.Get(v_55, (V)"floor_id"))), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_105, (V)"x") - R.Get(v_88, (V)"x"))))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_105, (V)"y") - R.Get(v_88, (V)"y"))))))).First <= (V)1.0d)))) {
var values_106 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_105, (V)"x") - R.Get(v_88, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_105, (V)"y") - R.Get(v_88, (V)"y"))))).First)));
V v_107 = values_106[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_99)), () => (V)(v_107 < v_100)), () => R.And((V)(v_107 == v_100), () => (V)(R.Get(v_103, (V)"id") < R.Get(v_99, (V)"id")))))) {
var values_108 = R.Join(new V[] {v_103}, new Results(v_107));
v_99 = values_108[0];
v_100 = values_108[1];
}
}
}
if (R.Truth(v_99)) {
R.Call(v_59, R.Join(new V[] {(V)"TerrainTriggered", R.Table(new Entry((V)"terrain_id", R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results((V)"c"))).First, (V)"id")), new Entry((V)"floor_id", R.Get(v_55, (V)"floor_id")), new Entry((V)"x", R.Get(v_88, (V)"x")), new Entry((V)"y", R.Get(v_88, (V)"y")), new Entry((V)"target_id", R.Get(v_99, (V)"id"))), R.Get(v_56, (V)"source_id")}, new Results(R.Get(v_56, (V)"action_id"))));
R.Call(v_60, R.Join(new V[] {v_99, R.Get(v_56, (V)"source_id"), R.Get(v_56, (V)"action_id"), (V)1.0d, (V)"crystal_arc"}, new Results(R.Get(v_56, (V)"source_faction"))));
}
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
