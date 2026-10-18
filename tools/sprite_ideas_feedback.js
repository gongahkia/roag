#!/usr/bin/env node
// Read, validate and summarise user feedback. Never implement, publish or write it.
'use strict';
const fs=require('node:fs');
const path=require('node:path');
const API=require('./sprite_ideas_client.js');
function read(filename,data){
  if(fs.statSync(filename).size>2*1024*1024)throw Error('Feedback exceeds 2 MiB.');
  return API.checked(data,JSON.parse(fs.readFileSync(filename,'utf8')));
}
function report(data,state){
  return {schema:'roag-sprite-ideas-feedback-report/1',
    proposal_fingerprint:data.fingerprint,mapping_checksum:data.mapping_checksum,
    progress:API.progress(data,state),selected_actor_conflicts:API.conflicts(data,state),
    note:'Read all comments and resolve artwork/dependencies before implementation. A proposal or art pool is not a published binding.',
    reviewed:state.cards.filter(r=>r.vote||r.comment||Object.values(r.art).some(a=>a.vote||a.comment||a.preferred.length)).map(r=>{
      const c=data.cards.find(c=>c.id===r.id);
      return {title:c.title,kind:c.kind,excluded:c.excluded,dependencies:c.dependencies,...r,
        art_pending:['kenney','onebit'].filter(p=>r.art[p].vote!=='approve')};
    })};
}
module.exports={read,report};
if(require.main===module){
  try{
    if(process.argv.length!==3)throw Error('Usage: node tools/sprite_ideas_feedback.js /path/to/roag-sprite-ideas-feedback.json');
    const data=JSON.parse(fs.readFileSync(path.join(__dirname,'../docs/sprite-audit/ideas.json'),'utf8'));
    console.log(JSON.stringify(report(data,read(process.argv[2],data)),null,2));
  }catch(error){console.error('Feedback rejected: '+error.message);process.exitCode=1;}
}
