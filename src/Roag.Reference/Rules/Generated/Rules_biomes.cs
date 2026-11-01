// Static C# port of src/biomes.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: ee8e8ff3b2d0772748034044e955606d2f8075a3ea7638782efd1294b895d3b1
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_biomes { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:biome/overgrown_ruins"), new Entry((V)2, (V)"core:biome/fungal_hollows"), new Entry((V)3, (V)"core:biome/sunken_aqueduct"), new Entry((V)4, (V)"core:biome/ashen_bastion"), new Entry((V)5, (V)"core:biome/glacial_observatory"), new Entry((V)6, (V)"core:biome/fractured_archive"), new Entry((V)7, (V)"core:biome/sundered_forge"))));
var obj_4 = v_2;
V key_5 = (V)"order";
R.Set(obj_4, key_5, values_3[0]);
var values_6 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(R.Get(v_2, (V)"order"), (V)1.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_2, (V)"order"), (V)1.0d)), new Entry((V)"name", (V)"Overgrown Ruins"), new Entry((V)"description", (V)"Mossy ruins and broken galleries"), new Entry((V)"strategy", (V)"ruins"), new Entry((V)"elite_id", (V)"core:enemy/thorn_warden"), new Entry((V)"map_id", (V)"core:map/overgrown_ruins"), new Entry((V)"floor_id", (V)"core:floor/overgrown_ruins"), new Entry((V)"enemy_ids", R.Table(new Entry((V)1, (V)"core:enemy/mossbound_guard"), new Entry((V)2, (V)"core:enemy/thornspitter"), new Entry((V)3, (V)"core:enemy/ruin_skitter"))), new Entry((V)"room_prefix", (V)"core:room/"), new Entry((V)"corridor_visual", (V)"c"), new Entry((V)"floor_visual", (V)"s"), new Entry((V)"wall_visual", (V)"w"), new Entry((V)"clue_color", R.Table(new Entry((V)1, (V)0.39d), new Entry((V)2, (V)0.55d), new Entry((V)3, (V)0.3d))))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)2.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_2, (V)"order"), (V)2.0d)), new Entry((V)"name", (V)"Fungal Hollows"), new Entry((V)"description", (V)"Winding tunnels and luminous pockets"), new Entry((V)"strategy", (V)"warrens"), new Entry((V)"elite_id", (V)"core:enemy/mycelial_matriarch"), new Entry((V)"map_id", (V)"core:map/fungal_hollows"), new Entry((V)"floor_id", (V)"core:floor/fungal_hollows"), new Entry((V)"enemy_ids", R.Table(new Entry((V)1, (V)"core:enemy/spore_herald"), new Entry((V)2, (V)"core:enemy/ruin_skitter"), new Entry((V)3, (V)"core:enemy/mossbound_guard"))), new Entry((V)"room_prefix", (V)"core:room/fungal_"), new Entry((V)"corridor_visual", (V)"f"), new Entry((V)"floor_visual", (V)"f"), new Entry((V)"wall_visual", (V)"F"), new Entry((V)"clue_color", R.Table(new Entry((V)1, (V)0.67d), new Entry((V)2, (V)0.54d), new Entry((V)3, (V)0.85d))))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)3.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_2, (V)"order"), (V)3.0d)), new Entry((V)"name", (V)"Sunken Aqueduct"), new Entry((V)"description", (V)"Canal galleries, bridges and flank paths"), new Entry((V)"strategy", (V)"canals"), new Entry((V)"elite_id", (V)"core:enemy/canal_lockmaster"), new Entry((V)"map_id", (V)"core:map/sunken_aqueduct"), new Entry((V)"floor_id", (V)"core:floor/sunken_aqueduct"), new Entry((V)"enemy_ids", R.Table(new Entry((V)1, (V)"core:enemy/canal_lancer"), new Entry((V)2, (V)"core:enemy/cinder_mender"), new Entry((V)3, (V)"core:enemy/thornspitter"))), new Entry((V)"room_prefix", (V)"core:room/aqueduct_"), new Entry((V)"corridor_visual", (V)"b"), new Entry((V)"floor_visual", (V)"b"), new Entry((V)"wall_visual", (V)"W"), new Entry((V)"clue_color", R.Table(new Entry((V)1, (V)0.48d), new Entry((V)2, (V)0.72d), new Entry((V)3, (V)0.74d))))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)4.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_2, (V)"order"), (V)4.0d)), new Entry((V)"name", (V)"Ashen Bastion"), new Entry((V)"description", (V)"Courtyards, ramparts and defended wings"), new Entry((V)"strategy", (V)"courtyards"), new Entry((V)"elite_id", (V)"core:enemy/ash_marshal"), new Entry((V)"map_id", (V)"core:map/ashen_bastion"), new Entry((V)"floor_id", (V)"core:floor/ashen_bastion"), new Entry((V)"enemy_ids", R.Table(new Entry((V)1, (V)"core:enemy/cinder_bellkeeper"), new Entry((V)2, (V)"core:enemy/mossbound_guard"), new Entry((V)3, (V)"core:enemy/ruin_skitter"))), new Entry((V)"room_prefix", (V)"core:room/bastion_"), new Entry((V)"corridor_visual", (V)"t"), new Entry((V)"floor_visual", (V)"t"), new Entry((V)"wall_visual", (V)"T"), new Entry((V)"hub_template_id", (V)"core:room/bastion_gallery"), new Entry((V)"clue_color", R.Table(new Entry((V)1, (V)0.76d), new Entry((V)2, (V)0.52d), new Entry((V)3, (V)0.36d))))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)5.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_2, (V)"order"), (V)5.0d)), new Entry((V)"name", (V)"Glacial Observatory"), new Entry((V)"description", (V)"Starward hubs, icy spokes and side rings"), new Entry((V)"strategy", (V)"observatory"), new Entry((V)"elite_id", (V)"core:enemy/prism_curator"), new Entry((V)"map_id", (V)"core:map/glacial_observatory"), new Entry((V)"floor_id", (V)"core:floor/glacial_observatory"), new Entry((V)"enemy_ids", R.Table(new Entry((V)1, (V)"core:enemy/glass_charger"), new Entry((V)2, (V)"core:enemy/crystal_bombardier"), new Entry((V)3, (V)"core:enemy/vault_lurker"))), new Entry((V)"room_prefix", (V)"core:room/observatory_"), new Entry((V)"corridor_visual", (V)"i"), new Entry((V)"floor_visual", (V)"i"), new Entry((V)"wall_visual", (V)"I"), new Entry((V)"clue_color", R.Table(new Entry((V)1, (V)0.58d), new Entry((V)2, (V)0.82d), new Entry((V)3, (V)0.93d))))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)6.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_2, (V)"order"), (V)6.0d)), new Entry((V)"name", (V)"Fractured Archive"), new Entry((V)"description", (V)"Staggered shelves and alternate reading aisles"), new Entry((V)"strategy", (V)"archive"), new Entry((V)"elite_id", (V)"core:enemy/index_keeper"), new Entry((V)"map_id", (V)"core:map/fractured_archive"), new Entry((V)"floor_id", (V)"core:floor/fractured_archive"), new Entry((V)"enemy_ids", R.Table(new Entry((V)1, (V)"core:enemy/mire_binder"), new Entry((V)2, (V)"core:enemy/pack_chorister"), new Entry((V)3, (V)"core:enemy/gloom_strider"))), new Entry((V)"room_prefix", (V)"core:room/archive_"), new Entry((V)"corridor_visual", (V)"l"), new Entry((V)"floor_visual", (V)"l"), new Entry((V)"wall_visual", (V)"L"), new Entry((V)"clue_color", R.Table(new Entry((V)1, (V)0.8d), new Entry((V)2, (V)0.7d), new Entry((V)3, (V)0.48d))))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)7.0d), R.Table(new Entry((V)"id", R.Get(R.Get(v_2, (V)"order"), (V)7.0d)), new Entry((V)"name", (V)"Sundered Forge"), new Entry((V)"description", (V)"Offset furnace bays and broken flank passages"), new Entry((V)"strategy", (V)"forge"), new Entry((V)"elite_id", (V)"core:enemy/ember_crucible"), new Entry((V)"map_id", (V)"core:map/sundered_forge"), new Entry((V)"floor_id", (V)"core:floor/sundered_forge"), new Entry((V)"enemy_ids", R.Table(new Entry((V)1, (V)"core:enemy/forge_conjurer"), new Entry((V)2, (V)"core:enemy/mossbreaker"), new Entry((V)3, (V)"core:enemy/bastion_bulwark"))), new Entry((V)"room_prefix", (V)"core:room/forge_"), new Entry((V)"corridor_visual", (V)"o"), new Entry((V)"floor_visual", (V)"o"), new Entry((V)"wall_visual", (V)"O"), new Entry((V)"clue_color", R.Table(new Entry((V)1, (V)0.95d), new Entry((V)2, (V)0.6d), new Entry((V)3, (V)0.36d))))))));
var obj_7 = v_2;
V key_8 = (V)"definitions";
R.Set(obj_7, key_8, values_6[0]);
R.Set(v_2, (V)"get", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"definitions"), v_10)));
return Results.Empty;
})));
R.Set(v_2, (V)"for_map", new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
foreach (var row_13 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_14 = row_13[0];
V v_15 = row_13[1];
var values_16 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"definitions"), v_15)));
V v_17 = values_16[0];
if (R.Truth((V)(R.Get(v_17, (V)"map_id") == v_12))) {
return R.Join(new V[] {}, new Results(v_17));
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"routes_for", new V((RuleFunction)(args_18 => {
V v_19 = args_18[0];
V v_20 = args_18[1];
if (R.Truth((V)(v_19 >= (V)3.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)"core:route/gateway"), new Entry((V)"name", (V)"Enter the Final Sanctum"), new Entry((V)"description", (V)"Face the Hollow Regent"))))));
}
var values_21 = R.Join(new V[] {}, new Results(R.Table()));
V v_22 = values_21[0];
if (R.Truth((V)(v_19 == (V)1.0d))) {
var values_23 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Get(R.Get(v_2, (V)"order"), (V)2.0d)), new Entry((V)2, R.Get(R.Get(v_2, (V)"order"), (V)3.0d)), new Entry((V)3, R.Get(R.Get(v_2, (V)"order"), (V)5.0d)))));
v_22 = values_23[0];
} else {
var values_24 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(R.Get(v_2, (V)"order"), (V)1.0d), (V)true))));
V v_25 = values_24[0];
foreach (var row_26 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_20, () => R.Table()))))))) {
V v_27 = row_26[0];
V v_28 = row_26[1];
var values_29 = R.Join(new V[] {}, R.Invoke(v_28, "gsub", R.Join(new V[] {(V)"^core:route/"}, new Results((V)"core:biome/"))));
V v_30 = values_29[0];
var values_31 = R.Join(new V[] {}, new Results((V)true));
var obj_32 = v_25;
V key_33 = v_30;
R.Set(obj_32, key_33, values_31[0]);
}
var values_34 = R.Join(new V[] {}, new Results(R.And(v_20, () => R.Get(v_20, (V)1.0d))));
V v_35 = values_34[0];
var values_36 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_35 == (V)"core:route/fungal_hollows"), () => R.Table(new Entry((V)1, R.Get(R.Get(v_2, (V)"order"), (V)3.0d)), new Entry((V)2, R.Get(R.Get(v_2, (V)"order"), (V)4.0d)), new Entry((V)3, R.Get(R.Get(v_2, (V)"order"), (V)6.0d)))), () => R.And((V)(v_35 == (V)"core:route/sunken_aqueduct"), () => R.Table(new Entry((V)1, R.Get(R.Get(v_2, (V)"order"), (V)2.0d)), new Entry((V)2, R.Get(R.Get(v_2, (V)"order"), (V)4.0d)), new Entry((V)3, R.Get(R.Get(v_2, (V)"order"), (V)7.0d))))), () => R.Table(new Entry((V)1, R.Get(R.Get(v_2, (V)"order"), (V)2.0d)), new Entry((V)2, R.Get(R.Get(v_2, (V)"order"), (V)6.0d)), new Entry((V)3, R.Get(R.Get(v_2, (V)"order"), (V)7.0d))))));
V v_37 = values_36[0];
foreach (var row_38 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_37)))))) {
V v_39 = row_38[0];
V v_40 = row_38[1];
if (R.Truth((V)(!R.Truth(R.Get(v_25, v_40))))) {
var values_41 = R.Join(new V[] {}, new Results(v_40));
var obj_42 = v_22;
V key_43 = (V)((V)R.Length(v_22) + (V)1.0d);
R.Set(obj_42, key_43, values_41[0]);
}
}
}
var values_44 = R.Join(new V[] {}, new Results(R.Table()));
V v_45 = values_44[0];
foreach (var row_46 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_22)))))) {
V v_47 = row_46[0];
V v_48 = row_46[1];
var values_49 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_48))))));
V v_50 = values_49[0];
var values_51 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Invoke(v_48, "gsub", R.Join(new V[] {(V)"^core:biome/"}, new Results((V)"core:route/"))).First), new Entry((V)"biome_id", v_48), new Entry((V)"name", R.Get(v_50, (V)"name")), new Entry((V)"description", R.Get(v_50, (V)"description")))));
var obj_52 = v_45;
V key_53 = (V)((V)R.Length(v_45) + (V)1.0d);
R.Set(obj_52, key_53, values_51[0]);
}
return R.Join(new V[] {}, new Results(v_45));
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_54 => {

var values_55 = R.Join(new V[] {R.Table(), R.Table(), R.Table()}, new Results(R.Table()));
V v_56 = values_55[0];
V v_57 = values_55[1];
V v_58 = values_55[2];
V v_59 = values_55[3];
var values_60 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"ruins", (V)true), new Entry((V)"warrens", (V)true), new Entry((V)"canals", (V)true), new Entry((V)"courtyards", (V)true), new Entry((V)"observatory", (V)true), new Entry((V)"archive", (V)true), new Entry((V)"forge", (V)true))));
V v_61 = values_60[0];
foreach (var row_62 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_63 = row_62[0];
V v_64 = row_62[1];
var values_65 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_64))).First}, new Results((V)"missing biome"))));
V v_66 = values_65[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_56, v_64))), () => (V)(R.Get(v_66, (V)"id") == v_64)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_66, (V)"name")))).First == (V)"string")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_66, (V)"description")))).First == (V)"string")), () => R.Get(v_61, R.Get(v_66, (V)"strategy"))), () => (V)(!R.Truth(R.Get(v_57, R.Get(v_66, (V)"map_id"))))), () => (V)(!R.Truth(R.Get(v_58, R.Get(v_66, (V)"floor_id"))))), () => (V)((V)R.Length(R.Get(v_66, (V)"enemy_ids")) == (V)3.0d)), () => (V)((V)R.Length(R.Get(v_66, (V)"room_prefix")) > (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_66, (V)"elite_id")))).First == (V)"string")), () => (V)(!R.Truth(R.Get(v_59, R.Get(v_66, (V)"elite_id"))))), () => (V)((V)R.Length(R.Get(v_66, (V)"corridor_visual")) == (V)1.0d)), () => (V)((V)R.Length(R.Get(v_66, (V)"floor_visual")) == (V)1.0d)), () => (V)((V)R.Length(R.Get(v_66, (V)"wall_visual")) == (V)1.0d)), () => (V)((V)R.Length(R.Get(v_66, (V)"clue_color")) == (V)3.0d))}, new Results(R.Concat((V)"invalid biome definition: ", v_64))));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(!R.Truth(R.Get(v_66, (V)"hub_template_id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_66, (V)"hub_template_id")))).First == (V)"string"))}, new Results(R.Concat((V)"invalid biome hub template: ", v_64))));
var values_67 = R.Join(new V[] {}, new Results(R.Table()));
V v_68 = values_67[0];
foreach (var row_69 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_66, (V)"enemy_ids"))))))) {
V v_70 = row_69[0];
V v_71 = row_69[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_71))).First == (V)"string"), () => (V)(!R.Truth(R.Get(v_68, v_71))))}, new Results(R.Concat((V)"duplicate biome encounter: ", v_64))));
var values_72 = R.Join(new V[] {}, new Results((V)true));
var obj_73 = v_68;
V key_74 = v_71;
R.Set(obj_73, key_74, values_72[0]);
}
foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_66, (V)"clue_color"))))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_77))).First == (V)"number"), () => (V)(v_77 >= (V)0.0d)), () => (V)(v_77 <= (V)1.0d))}, new Results(R.Concat((V)"invalid biome clue color: ", v_64))));
}
var values_78 = R.Join(new V[] {}, new Results((V)true));
var obj_79 = v_56;
V key_80 = v_64;
R.Set(obj_79, key_80, values_78[0]);

var values_81 = R.Join(new V[] {}, new Results((V)true));
var obj_82 = v_57;
V key_83 = R.Get(v_66, (V)"map_id");
R.Set(obj_82, key_83, values_81[0]);

var values_84 = R.Join(new V[] {}, new Results((V)true));
var obj_85 = v_58;
V key_86 = R.Get(v_66, (V)"floor_id");
R.Set(obj_85, key_86, values_84[0]);
var values_87 = R.Join(new V[] {}, new Results((V)true));
var obj_88 = v_59;
V key_89 = R.Get(v_66, (V)"elite_id");
R.Set(obj_88, key_89, values_87[0]);
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
