local T={}
function T.run()
 local a=require('tests.suite').run(true);local b=require('tests.maps').run()
 local c=require('tests.expansion').run();local d=require('tests.campaign').run();local e=require('tests.region').run();local f=require('tests.logistics').run()
 local result={groups=a.groups+b.groups+c.groups+d.groups+e.groups+f.groups,assertions=a.assertions+b.assertions+c.assertions+d.assertions+e.assertions+f.assertions}
 print('TOTAL: '..result.groups..' groups; '..result.assertions..' assertions.')
 return result
end
return T
