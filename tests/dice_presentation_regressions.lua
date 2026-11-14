local C=require('src.persistence');local pass,fail=0,0
local function equal(a,b)assert(C.encode(a)==C.encode(b),'values differ')end
local function test(name,fn)local ok,why=xpcall(fn,debug.traceback);if ok then pass=pass+1;print('PASS '..name)else fail=fail+1;print('FAIL '..name..'\n'..why)end end
for _,name in ipairs({'presentation_settings_tests','idea_playback_tests','motion_tests','visual_juice_tests','persistence_tests','expansion_audio_tests'})do require('tests.'..name)(test,equal)end
print(('Presentation regressions: %d passed, %d failed'):format(pass,fail));os.exit(fail==0 and 0 or 1)
