// Static C# port of tests/expansion_boon_acceptance.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: c9d1dca4bf1d4ad8beff191610a076b1b3f8457b20acbb250845c8c2ee499590
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_expansion_boon_acceptance { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"tests.legal_playthrough"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievements"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table()));
V v_8 = values_7[0];
R.Set(v_8, (V)"run", new V((RuleFunction)(args_9 => {

var values_10 = R.Join(new V[] {}, new Results(R.Table()));
V v_11 = values_10[0];
foreach (var row_12 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"pool"), R.Join(new V[] {}, new Results(R.Table())))))))) {
V v_13 = row_12[0];
V v_14 = row_12[1];
if (R.Truth(R.And((V)(v_14 != (V)"core:boon/ring_of_reversal"), () => (V)(!R.Truth(R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"definitions"), v_14)))))) {
var values_15 = R.Join(new V[] {}, new Results(v_14));
var obj_16 = v_11;
V key_17 = (V)((V)R.Length(v_11) + (V)1.0d);
R.Set(obj_16, key_17, values_15[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_11) == (V)48.0d)}, new Results((V)"historical M6 pool changed")));
var values_18 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new"), R.Join(new V[] {(V)50.0d}, new Results(R.Table(new Entry((V)"meta", R.Call(R.Get(v_6, (V)"new_run"), R.Join(new V[] {(V)true}, new Results((V)"m6-acceptance/50"))).First), new Entry((V)"available_boons", v_11), new Entry((V)"multifloor", (V)true))))));
V v_19 = values_18[0];
var values_20 = R.Join(new V[] {}, new Results(R.Get(v_19, (V)"state")));
V v_21 = values_20[0];
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_24, (V)"controller") == (V)"ai"), () => (V)(R.Get(R.Get(v_24, (V)"position"), (V)"floor_id") == R.Get(v_21, (V)"active_floor_id"))), () => (V)(R.Get(v_24, (V)"id") != R.Get(R.Get(v_21, (V)"stage"), (V)"elite_id"))), () => (V)(!R.Truth(R.Get(v_24, (V)"invulnerable"))))));
return Results.Empty;
}));
{ double end_27 = R.Num((V)7.0d), step_28 = R.Num((V)1.0d);
for (double i_26 = R.Num((V)1.0d); step_28 > 0 ? i_26 <= end_27 : i_26 >= end_27; i_26 += step_28) {
V v_25 = i_26;
if (R.Truth((V)(R.Get(v_21, (V)"currency") >= (V)5.0d))) {
break;
}
var values_29 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"nearest"), R.Join(new V[] {v_21}, new Results(v_22))).First}, new Results((V)"no ordinary foe"))));
V v_30 = values_29[0];
var values_31 = R.Join(new V[] {R.Get(R.Get(v_30, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_30, (V)"position"), (V)"y")));
V v_32 = values_31[0];
V v_33 = values_31[1];
var values_34 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"fight"), R.Join(new V[] {v_19, R.Get(v_30, (V)"id")}, new Results((V)140.0d))));
V v_35 = values_34[0];
V v_36 = values_34[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_35}, new Results(v_36)));
var values_39 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"navigate"), R.Join(new V[] {v_19, new V((RuleFunction)(args_37 => {
V v_38 = args_37[0];
return R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_38, (V)"x") == v_32), () => (V)(R.Get(v_38, (V)"y") == v_33))));
return Results.Empty;
}))}, new Results((V)170.0d))));
v_35 = values_39[0];
v_36 = values_39[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_35}, new Results(v_36)));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_21, (V)"currency") >= (V)5.0d), () => (V)(!R.Truth(R.Get(v_21, (V)"game_over"))))}, new Results((V)"failed to earn chest cost")));
var values_40 = Results.Empty;
V v_41 = values_40[0];
V v_42 = values_40[1];
foreach (var row_43 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_21, (V)"entities"))))))) {
V v_44 = row_43[0];
V v_45 = row_43[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_45, (V)"controller") == (V)"chest"), () => (V)(R.Get(v_45, (V)"price") > (V)0.0d)), () => (V)(R.Get(R.Get(v_45, (V)"position"), (V)"floor_id") == R.Get(v_21, (V)"active_floor_id"))))) {
foreach (var row_46 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_45, (V)"offers"))))))) {
V v_47 = row_46[0];
V v_48 = row_46[1];
if (R.Truth((V)(R.Get(v_48, (V)"boon_id") == (V)"core:boon/rhythm_of_steel"))) {
var values_49 = R.Join(new V[] {v_45}, new Results(v_47));
v_41 = values_49[0];
v_42 = values_49[1];
break;
}
}
}
if (R.Truth(v_41)) {
break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_41, () => v_42)}, new Results((V)"seed-50 rhythm chest missing")));
var values_52 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"navigate"), R.Join(new V[] {v_19, new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
return R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_51, (V)"x") - R.Get(R.Get(v_41, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_51, (V)"y") - R.Get(R.Get(v_41, (V)"position"), (V)"y"))))).First) == (V)1.0d)));
return Results.Empty;
}))}, new Results((V)250.0d))));
V v_53 = values_52[0];
V v_54 = values_52[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_53}, new Results(v_54)));
var values_55 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"chest_options"), R.Join(new V[] {v_21}, new Results(R.Get(v_41, (V)"id")))))));
V v_56 = values_55[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_56, (V)"offers"), v_42), (V)"boon_id") == (V)"core:boon/rhythm_of_steel"))));
var values_57 = R.Join(new V[] {}, new Results(R.Get(v_21, (V)"currency")));
V v_58 = values_57[0];
var values_59 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"action"), R.Join(new V[] {v_19}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_41, (V)"id")), new Entry((V)"offer_index", v_42))))));
v_53 = values_59[0];
v_54 = values_59[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_53}, new Results(v_54)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_41, (V)"claimed"), () => (V)(R.Get(v_21, (V)"currency") == (V)(v_58 - R.Get(v_41, (V)"price")))), () => (V)(R.Get(R.Get(R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_21))).First, (V)"boons"), (V)"core:boon/rhythm_of_steel"), (V)"common") == (V)1.0d)))));
var values_60 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"nearest"), R.Join(new V[] {v_21}, new Results(v_22))))));
V v_61 = values_60[0];
var values_62 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"fight"), R.Join(new V[] {v_19, R.Get(v_61, (V)"id")}, new Results((V)120.0d))));
v_53 = values_62[0];
v_54 = values_62[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_53}, new Results(v_54)));
var values_63 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_64 = values_63[0];
V v_65 = values_63[1];
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_19, (V)"events"))))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
if (R.Truth(R.And((V)(R.Get(v_68, (V)"kind") == (V)"BoonActivated"), () => (V)(R.Get(v_68, (V)"boon_id") == (V)"core:boon/rhythm_of_steel")))) {
if (R.Truth(R.Get(v_68, (V)"trigger_event_id"))) {
foreach (var row_69 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_19, (V)"events"))))))) {
V v_70 = row_69[0];
V v_71 = row_69[1];
if (R.Truth((V)(R.Get(v_71, (V)"id") == R.Get(v_68, (V)"trigger_event_id")))) {
if (R.Truth((V)(R.Get(v_71, (V)"kind") == (V)"Moved"))) {
var values_72 = R.Join(new V[] {}, new Results((V)(v_64 + (V)1.0d)));
v_64 = values_72[0];
} else if (R.Truth((V)(R.Get(v_71, (V)"kind") == (V)"DamageTaken"))) {
var values_73 = R.Join(new V[] {}, new Results((V)(v_65 + (V)1.0d)));
v_65 = values_73[0];
}
break;
}
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(v_64 >= (V)1.0d), () => (V)(v_65 >= (V)1.0d))}, new Results((V)"purchased boon did not prime and hit")));
return R.Join(new V[] {v_19}, new Results(R.Table(new Entry((V)"seed", (V)50.0d), new Entry((V)"chest_id", R.Get(v_41, (V)"id")), new Entry((V)"offer_index", v_42), new Entry((V)"boon_id", R.Get(R.Get(R.Get(v_56, (V)"offers"), v_42), (V)"boon_id")), new Entry((V)"rarity", R.Get(R.Get(R.Get(v_56, (V)"offers"), v_42), (V)"rarity")), new Entry((V)"actions", (V)R.Length(R.Get(v_19, (V)"trace"))), new Entry((V)"hp", R.Get(R.Call(R.Get(v_4, (V)"player"), R.Join(new V[] {}, new Results(v_21))).First, (V)"hp")), new Entry((V)"clock", R.Get(v_21, (V)"clock")), new Entry((V)"movement_activations", v_64), new Entry((V)"hit_activations", v_65))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_8));
return Results.Empty;
} }
