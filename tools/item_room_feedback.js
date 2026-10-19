#!/usr/bin/env node
// Read-only importer for the focused review. It cannot apply gameplay changes.
'use strict';
const fs=require('node:fs'),path=require('node:path');
const API=require('./item_room_review_client.js');
function read(filename,data){
  if(fs.statSync(filename).size>2*1024*1024)throw Error('Feedback exceeds 2 MiB.');
  return API.checked(data,JSON.parse(fs.readFileSync(filename,'utf8')));
}
function report(data,state){
  return {
    schema:'roag-item-room-feedback-report/1',proposal_fingerprint:data.fingerprint,
    mapping_checksum:data.mapping_checksum,original_review:data.origin,
    progress:API.progress(data,state),
    note:'New detail votes only. Preserve earlier broad approvals and read every comment. Unanswered details are pending, not rejected or automatically approved. This report implements nothing.',
    reviewed:state.cards.filter(r=>r.vote||r.comment||Object.values(r.art).some(a=>a.vote||a.comment||a.preferred.length)).map(r=>{
      const c=data.cards.find(c=>c.id===r.id);
      return {...r,title:c.title,kind:c.kind,prior_decision:c.history,
        art_pending:['kenney','onebit'].filter(p=>r.art[p].vote!=='approve'),
        selected_existing_bindings:Object.fromEntries(['kenney','onebit'].map(p=>[p,
          r.art[p].preferred.filter(k=>data.cells[k].usage[p].length).map(k=>({cell:k,bindings:data.cells[k].usage[p]}))]))};
    })
  };
}
module.exports={read,report};
if(require.main===module){
  try{
    if(process.argv.length!==3)throw Error('Usage: node tools/item_room_feedback.js /path/to/roag-items-rooms-feedback.json');
    const data=JSON.parse(fs.readFileSync(path.join(__dirname,'../docs/sprite-audit/items-rooms.json'),'utf8'));
    console.log(JSON.stringify(report(data,read(process.argv[2],data)),null,2));
  }catch(error){console.error('Feedback rejected: '+error.message);process.exitCode=1;}
}
