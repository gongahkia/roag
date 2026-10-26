// Static C# port of tests/idea_boon_handoff_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 37f7a7c0e7d2d5dbb593e5eeda4eddf62a2e3ec81f0bb5898ce3b58c85b369bf
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_boon_handoff_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.idea_boon_handoff_fixture"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {R.Get(v_11, (V)"submit"), R.Get(v_11, (V)"ready")}, new Results(R.Get(v_11, (V)"route")));
V v_13 = values_12[0];
V v_14 = values_12[1];
V v_15 = values_12[2];
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
V v_20 = args_17[2];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_18, R.Get(v_19, (V)"id"), R.Concat((V)"core:boon/", v_20), (V)"rare"}, new Results((V)1.0d)));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"stage handoff preserves remaining boon duration and never revives expired speed or prime charges"}, new Results(new V((RuleFunction)(args_21 => {

var values_22 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_23 = values_22[0];
V v_24 = values_22[1];
V v_25 = values_22[2];

var values_26 = R.Join(new V[] {}, new Results(R.Get(v_23, (V)"clock")));
V v_27 = values_26[0];
foreach (var row_28 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"road_flask"), new Entry((V)2, (V)"bloodrush"), new Entry((V)3, (V)"ration_reserve"), new Entry((V)4, (V)"betrayal_ward"), new Entry((V)5, (V)"fateful_dice")))))))) {
V v_29 = row_28[0];
V v_30 = row_28[1];
R.Call(v_16, R.Join(new V[] {v_23, v_24}, new Results(v_30)));
}
var values_31 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:boon/road_flask", R.Table(new Entry((V)"speed", (V)20.0d), new Entry((V)"expires_at", (V)(v_27 + (V)250.0d)))), new Entry((V)"core:boon/bloodrush", R.Table(new Entry((V)"speed", (V)20.0d), new Entry((V)"expires_at", (V)(v_27 - (V)1.0d)))), new Entry((V)"core:boon/ration_reserve", R.Table(new Entry((V)"charges", (V)1.0d), new Entry((V)"expires_at", (V)(v_27 - (V)1.0d)))), new Entry((V)"core:boon/betrayal_ward", R.Table(new Entry((V)"ward", (V)3.0d), new Entry((V)"expires_at", (V)(v_27 + (V)150.0d)))), new Entry((V)"core:boon/fateful_dice", R.Table(new Entry((V)"face", (V)4.0d), new Entry((V)"roll_action_id", (V)1.0d), new Entry((V)"ward", (V)3.0d), new Entry((V)"expires_at", (V)(v_27 + (V)100.0d)))))));
var obj_32 = v_24;
V key_33 = (V)"boon_memory";
R.Set(obj_32, key_33, values_31[0]);
var values_34 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_23))));
V v_35 = values_34[0];

var values_36 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_23, (V)"rng"), (V)"state")));
V v_37 = values_36[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_23}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", v_25), new Entry((V)"route_index", (V)99.0d))))).First)))));
R.Call(v_3, R.Join(new V[] {v_35}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_23)))));
R.Call(v_15, R.Join(new V[] {v_23}, new Results(v_25)));

var values_38 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_23))));
v_24 = values_38[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_23, (V)"clock") == (V)0.0d), () => (V)(R.Get(R.Get(v_23, (V)"rng"), (V)"state") == v_37)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_24, (V)"boon_memory"), (V)"core:boon/road_flask"), (V)"expires_at") == (V)250.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_24, (V)"boon_memory"), (V)"core:boon/betrayal_ward"), (V)"expires_at") == (V)150.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_24, (V)"boon_memory"), (V)"core:boon/fateful_dice"), (V)"expires_at") == (V)100.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_24, (V)"boon_memory"), (V)"core:boon/fateful_dice"), (V)"face") == (V)4.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_24, (V)"boon_memory"), (V)"core:boon/bloodrush"), (V)"speed") == (V)0.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Get(v_24, (V)"boon_memory"), (V)"core:boon/ration_reserve"), (V)"charges") == (V)0.0d), () => (V)(R.Call(R.Get(v_7, (V)"speed_bonus"), R.Join(new V[] {v_23}, new Results(v_24))).First == (V)20.0d)))));
R.Call(v_3, R.Join(new V[] {R.Get(R.Get(R.Get(v_35, (V)"entities"), (V)1.0d), (V)"boon_memory")}, new Results(R.Get(R.Get(R.Get(R.Get(R.Get(v_23, (V)"retired_stages"), (V)1.0d), (V)"entities"), (V)1.0d), (V)"boon_memory"))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_23))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_23)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"an expiry exactly at the old clock survives the zero-time handoff and expires after the next action"}, new Results(new V((RuleFunction)(args_39 => {

var values_40 = R.Join(new V[] {}, R.Call(v_14, Results.Empty));
V v_41 = values_40[0];
V v_42 = values_40[1];
V v_43 = values_40[2];

R.Call(v_16, R.Join(new V[] {v_41, v_42}, new Results((V)"road_flask")));
var values_44 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:boon/road_flask", R.Table(new Entry((V)"speed", (V)20.0d), new Entry((V)"expires_at", R.Get(v_41, (V)"clock")))))));
var obj_45 = v_42;
V key_46 = (V)"boon_memory";
R.Set(obj_45, key_46, values_44[0]);
R.Call(v_15, R.Join(new V[] {v_41}, new Results(v_43)));

var values_47 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_41))));
v_42 = values_47[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Call(R.Get(v_7, (V)"speed_bonus"), R.Join(new V[] {v_41}, new Results(v_42))).First == (V)20.0d), () => (V)(R.Get(R.Get(R.Get(v_42, (V)"boon_memory"), (V)"core:boon/road_flask"), (V)"expires_at") == (V)0.0d)))));
R.Call(v_13, R.Join(new V[] {v_41}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_7, (V)"speed_bonus"), R.Join(new V[] {v_41}, new Results(v_42))).First == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"actual player Burning gets a new matching status schedule across stages and resumes exactly"}, new Results(new V((RuleFunction)(args_48 => {

var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"burning"), Results.Empty));
V v_50 = values_49[0];
V v_51 = values_49[1];
V v_52 = values_49[2];
var values_53 = R.Join(new V[] {}, new Results(R.Get(v_51, (V)"id")));
V v_54 = values_53[0];

var values_55 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_51, (V)"statuses"), (V)"burning")))))));
V v_56 = values_55[0];
var values_57 = R.Join(new V[] {}, new Results((V)(R.Get(v_56, (V)"next_due") - R.Get(v_50, (V)"clock"))));
V v_58 = values_57[0];

var values_59 = R.Join(new V[] {}, new Results(R.Get(v_51, (V)"hp")));
V v_60 = values_59[0];

var values_61 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_50, (V)"rng"), (V)"state")));
V v_62 = values_61[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_58 > (V)0.0d), () => (V)(v_58 <= (V)100.0d)))));
R.Call(v_15, R.Join(new V[] {v_50}, new Results(v_52)));

var values_63 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_50))));
v_51 = values_63[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_51, (V)"id") != v_54), () => (V)(R.Get(v_51, (V)"hp") == v_60)), () => (V)(R.Get(R.Get(v_50, (V)"rng"), (V)"state") == v_62)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_51, (V)"statuses"), (V)"burning"), (V)"next_due") == v_58))));
var values_64 = R.Join(new V[] {}, new Results((V)false));
V v_65 = values_64[0];
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_50, (V)"schedule"))))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
if (R.Truth((V)(R.Get(v_68, (V)"id") == R.Get(R.Get(R.Get(v_51, (V)"statuses"), (V)"burning"), (V)"schedule_id")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_68, (V)"kind") == (V)"effect"), () => (V)(R.Get(v_68, (V)"actor_id") == R.Get(v_51, (V)"id"))), () => (V)(R.Get(v_68, (V)"due") == v_58)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_68, (V)"floor_id") == R.Get(R.Get(v_51, (V)"position"), (V)"floor_id")))));

var values_69 = R.Join(new V[] {}, new Results((V)true));
v_65 = values_69[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_65}, new Results((V)"traveling Burning lost its work")));
var values_70 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_50))))))))));
V v_71 = values_70[0];
var values_72 = R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {v_50}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_73 = values_72[0];

var values_74 = R.Join(new V[] {}, R.Call(v_13, R.Join(new V[] {v_71}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_75 = values_74[0];
R.Call(v_3, R.Join(new V[] {v_73}, new Results(v_75)));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_50))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_71)))));
var values_76 = R.Join(new V[] {}, new Results((V)0.0d));
V v_77 = values_76[0];
foreach (var row_78 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_73)))))) {
V v_79 = row_78[0];
V v_80 = row_78[1];
if (R.Truth(R.And((V)(R.Get(v_80, (V)"kind") == (V)"StatusTick"), () => (V)(R.Get(v_80, (V)"target_id") == R.Get(v_51, (V)"id"))))) {
var values_81 = R.Join(new V[] {}, new Results((V)(v_77 + (V)1.0d)));
v_77 = values_81[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_77 == (V)1.0d), () => (V)(R.Get(v_51, (V)"hp") == (V)(v_60 - R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results((V)(R.Get(v_56, (V)"power") - R.Get(v_51, (V)"defense"))))).First))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"refreshing expired speed after a rarity trade uses current power rather than the expired larger value"}, new Results(new V((RuleFunction)(args_82 => {

var values_83 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {}, new Results((V)37.0d))));
V v_84 = values_83[0];

var values_85 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_84))));
V v_86 = values_85[0];

var values_87 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_88 = v_86;
V key_89 = (V)"hp";
R.Set(obj_88, key_89, values_87[0]);
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_84, R.Get(v_86, (V)"id"), (V)"core:boon/road_flask", (V)"legendary"}, new Results((V)1.0d)));
var values_90 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_91 = values_90[0];
R.Call(R.Get(v_91, (V)"spawn_supply"), R.Join(new V[] {v_84, (V)"food", R.Get(v_84, (V)"active_floor_id"), (V)3.0d, (V)2.0d, new V((RuleFunction)(args_92 => {


return Results.Empty;
}))}, new Results(R.Get(v_86, (V)"id"))));
R.Call(v_13, R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_86, (V)"boon_memory"), (V)"core:boon/road_flask"), (V)"speed") == (V)25.0d))));
while (R.Truth((V)(R.Get(v_84, (V)"clock") <= (V)300.0d))) {
R.Call(v_13, R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
}
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_84, R.Get(v_86, (V)"id"), (V)"core:boon/road_flask", (V)"legendary"}, new Results((V)0.0d)));
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_84, R.Get(v_86, (V)"id"), (V)"core:boon/road_flask", (V)"common"}, new Results((V)1.0d)));
var values_93 = R.Join(new V[] {}, new Results((V)10.0d));
var obj_94 = v_86;
V key_95 = (V)"hp";
R.Set(obj_94, key_95, values_93[0]);

R.Call(R.Get(v_91, (V)"spawn_supply"), R.Join(new V[] {v_84, (V)"food", R.Get(v_84, (V)"active_floor_id"), (V)2.0d, (V)2.0d, new V((RuleFunction)(args_96 => {


return Results.Empty;
}))}, new Results(R.Get(v_86, (V)"id"))));
R.Call(v_13, R.Join(new V[] {v_84}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-(V)1.0d)), new Entry((V)"dy", (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_86, (V)"boon_memory"), (V)"core:boon/road_flask"), (V)"speed") == (V)10.0d))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
