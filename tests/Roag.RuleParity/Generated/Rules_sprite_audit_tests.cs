// Static C# port of tests/sprite_audit_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 5d9ad2ba3809e9041fb5bb09b351e7cf9734a45b91960d5e9a65db4682f85bd4
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_sprite_audit_tests { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.sprite_audit"))));
V v_2 = values_1[0];
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_3 => {
V v_4 = args_3[0];
V v_5 = args_3[1];
R.Call(v_4, R.Join(new V[] {(V)"native sprite inventory checks runtime coverage and distinguishes shared/static poses"}, new Results(new V((RuleFunction)(args_6 => {

var values_7 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"collect"), Results.Empty));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))));
V v_10 = values_9[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_8, (V)"requirements")) == (V)((V)((V)491.0d + (V)((V)2.0d * (V)R.Length(R.Get(v_10, (V)"order")))) + (V)R.Length(R.Get(v_10, (V)"cues")))))));
foreach (var row_11 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"packs"))))))) {
V v_12 = row_11[0];
V v_13 = row_11[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_13, (V)"missing")) == (V)0.0d), () => (V)((V)R.Length(R.Get(v_13, (V)"bindings")) == (V)R.Length(R.Get(v_8, (V)"requirements")))))));
foreach (var row_14 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_interaction_content"))).First, (V)"art"))))))) {
V v_15 = row_14[0];
var values_16 = R.Join(new V[] {}, new Results((V)false));
V v_17 = values_16[0];
foreach (var row_18 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_13, (V)"bindings"))))))) {
V v_19 = row_18[0];
V v_20 = row_18[1];
if (R.Truth((V)(R.Get(v_20, (V)"id") == R.Concat((V)"core:object/interaction_", v_15)))) {
var values_21 = R.Join(new V[] {}, new Results((V)true));
v_17 = values_21[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_17}, new Results(R.Concat((V)"missing interaction art: ", v_15))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_13, (V)"actor_count") == (V)65.0d), () => (V)(R.Get(v_13, (V)"static_pose_aliases") == (V)130.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_13, (V)"wall_masks") == (V)47.0d), () => (V)(R.Get(v_13, (V)"water_masks") == (V)47.0d)))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_8, (V)"packs"), (V)"onebit"), (V)"actor_silhouettes") == (V)63.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_8, (V)"packs"), (V)"kenney") == V.Nil), () => (V)(R.Get(R.Get(v_8, (V)"sheets"), (V)"micro") == V.Nil)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(v_8, (V)"packs"), (V)"onebit"), (V)"shared_actors")) == (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Get(R.Get(R.Get(v_8, (V)"packs"), (V)"onebit"), (V)"usage"), (V)"micro"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(R.Get(R.Get(R.Get(v_8, (V)"packs"), (V)"onebit"), (V)"usage"), (V)"caves"), (V)"146"), () => R.Get(R.Get(R.Get(R.Get(R.Get(v_8, (V)"packs"), (V)"onebit"), (V)"usage"), (V)"rpg"), (V)"60")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(R.Get(v_8, (V)"packs"), (V)"onebit"), (V)"usage"), (V)"caves"), (V)"38"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(R.Get(R.Get(R.Get(v_8, (V)"packs"), (V)"onebit"), (V)"usage"), (V)"tiny"), (V)"9"), () => R.Get(R.Get(R.Get(R.Get(R.Get(v_8, (V)"packs"), (V)"onebit"), (V)"usage"), (V)"tiny"), (V)"29")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(R.Get(v_8, (V)"packs"), (V)"onebit"), (V)"usage"), (V)"rpg"), (V)"60"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(R.Get(v_8, (V)"packs"), (V)"onebit"), (V)"usage"), (V)"onebit"), (V)"169"))));
return Results.Empty;
})))));
R.Call(v_4, R.Join(new V[] {(V)"sprite audit is deterministic and read only across repeated scans"}, new Results(new V((RuleFunction)(args_22 => {

var values_23 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"collect"), Results.Empty));
V v_24 = values_23[0];
R.Call(v_5, R.Join(new V[] {v_24}, R.Call(R.Get(v_2, (V)"collect"), Results.Empty)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"json"), R.Join(new V[] {}, new Results(v_24))).First == R.Call(R.Get(v_2, (V)"json"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"collect"), Results.Empty))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Call(R.Get(v_2, (V)"json"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)"quote", (V)"\"\n\\"))))).First == (V)"{\"quote\":\"\\u0022\\u000a\\u005c\"}"))));
return Results.Empty;
})))));
R.Call(v_4, R.Join(new V[] {(V)"saved sprite inventory matches the current resolver mappings"}, new Results(new V((RuleFunction)(args_25 => {

var values_26 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(rt.Global("io"), (V)"open"), R.Join(new V[] {(V)"docs/sprite-audit/inventory.json"}, new Results((V)"rb"))).First}, new Results((V)"missing sprite catalogue; run make sprite-audit"))));
V v_27 = values_26[0];
var values_28 = R.Join(new V[] {}, R.Invoke(v_27, "read", R.Join(new V[] {}, new Results((V)"*a"))));
V v_29 = values_28[0];

R.Invoke(v_27, "close", Results.Empty);
var values_30 = R.Join(new V[] {}, R.Invoke(v_29, "match", R.Join(new V[] {}, new Results((V)"\"mapping_checksum\":%s*\"([^\"]+)\""))));
V v_31 = values_30[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_31 == R.Get(R.Call(R.Get(v_2, (V)"collect"), Results.Empty).First, (V)"mapping_checksum"))}, new Results((V)"stale sprite catalogue; run make sprite-audit and include regenerated files")));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
