local T={}
function T.run()
 local a=require('tests.suite').run(true);local b=require('tests.maps').run()
 local c=require('tests.expansion').run();local d=require('tests.campaign').run();local e=require('tests.region').run();local f=require('tests.logistics').run();local g=require('tests.travel').run();local h=require('tests.knowledge').run();local i=require('tests.education').run();local j=require('tests.g01').run();local k=require('tests.g02').run();local l=require('tests.g03').run();local m=require('tests.g04').run();local n=require('tests.g05').run();local o=require('tests.g06').run()
 local result={groups=a.groups+b.groups+c.groups+d.groups+e.groups+f.groups+g.groups+h.groups+i.groups+j.groups+k.groups+l.groups+m.groups+n.groups+o.groups,assertions=a.assertions+b.assertions+c.assertions+d.assertions+e.assertions+f.assertions+g.assertions+h.assertions+i.assertions+j.assertions+k.assertions+l.assertions+m.assertions+n.assertions+o.assertions}
 print('TOTAL: '..result.groups..' groups; '..result.assertions..' assertions.')
 return result
end
return T
