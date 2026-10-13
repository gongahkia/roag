local run,result=require("tests.expansion_boon_acceptance").run()
local path=arg[1] or "/var/tmp/roeg-m6-seed50.trace"
local file=assert(io.open(path,"wb"))
file:write("seed\taction\tclock\thp\tcurrency\tintent\tevents\n")
for index,record in ipairs(run.trace) do
    local intent=record.intent
    file:write(table.concat({result.seed,index,record.clock,record.hp,
        record.currency,intent.kind or "",table.concat(record.event_kinds,",")},"\t"),"\n")
end
file:close()
print(("M6 legal chest PASS: seed=%d actions=%d HP=%d clock=%d chest=%d offer=%d %s %s movement=%d hit=%d trace=%s"):
    format(result.seed,result.actions,result.hp,result.clock,result.chest_id,
        result.offer_index,result.boon_id,result.rarity,
        result.movement_activations,result.hit_activations,path))
