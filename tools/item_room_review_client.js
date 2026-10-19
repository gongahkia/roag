/* Focused detail review, based on the frozen sprite-ideas client. Keep that archive intact. */
(function(root){
  'use strict';
  const SCHEMA='roag-item-room-feedback/1',packs=['kenney','onebit'];
  const schema=data=>data.feedback_schema||SCHEMA;
  const votes=['','approve','reject','discuss'];
  const candidates=(card,pack)=>[...new Set(card.art[pack].flatMap(g=>g.cells))];
  function blank(data){
    return {schema:schema(data),proposal_fingerprint:data.fingerprint,audit_mapping_checksum:data.mapping_checksum,
      cards:data.cards.map(c=>({id:c.id,vote:'',priority:'',comment:'',
        art:Object.fromEntries(packs.map(p=>[p,{vote:'',comment:'',preferred:[]}]))}))};
  }
  function checked(data,raw){
    if(!raw||raw.schema!==schema(data))throw Error('Not a feedback file for this review. Earlier sprite, room, enemy and item reviews use separate files.');
    if(raw.proposal_fingerprint!==data.fingerprint||raw.audit_mapping_checksum!==data.mapping_checksum)
      throw Error('Different proposals, source pixels or mapping version. Keep that file; do not combine it with this review automatically.');
    if(!Array.isArray(raw.cards)||raw.cards.length!==data.cards.length)throw Error('Missing review records.');
    const records=new Map();
    for(const r of raw.cards){
      if(!r||records.has(r.id)||!data.cards.some(c=>c.id===r.id))throw Error('Duplicate or unknown idea ID.');
      records.set(r.id,r);
    }
    function text(v){if(typeof v!=='string'||v.length>4000)throw Error('Invalid or overlong comment.');return v;}
    const out=blank(data);
    out.cards=data.cards.map(c=>{
      const r=records.get(c.id);
      if(!votes.includes(r.vote)||!['','next','later'].includes(r.priority))throw Error('Invalid idea decision or priority.');
      if(c.excluded&&(r.vote==='approve'||r.priority))throw Error('Held/excluded groups are not new feature approvals.');
      const art={};
      for(const p of packs){
        const a=r.art&&r.art[p],options=candidates(c,p);
        if(!a||!votes.includes(a.vote)||!Array.isArray(a.preferred)||
          a.preferred.length!==new Set(a.preferred).size||a.preferred.some(k=>!options.includes(k)))
          throw Error('Invalid art decision or sprite preference for '+c.id+' / '+p+'.');
        if(a.vote==='approve'&&(c.excluded||!options.length))throw Error('Cannot approve missing or held artwork.');
        art[p]={vote:a.vote,comment:text(a.comment),preferred:[...a.preferred].sort()};
      }
      return {id:c.id,vote:r.vote,priority:r.priority,comment:text(r.comment),art};
    });
    return out;
  }
  function progress(data,state){
    const defs=new Map(data.cards.map(c=>[c.id,c]));
    const cards=state.cards.filter(c=>!defs.get(c.id).excluded);
    const art=cards.flatMap(r=>packs.filter(p=>candidates(defs.get(r.id),p).length).map(p=>r.art[p]));
    return {total:cards.length,reviewed:cards.filter(r=>r.vote).length,
      approved:cards.filter(r=>r.vote==='approve').length,rejected:cards.filter(r=>r.vote==='reject').length,
      discussed:cards.filter(r=>r.vote==='discuss').length,artTotal:art.length,artReviewed:art.filter(a=>a.vote).length};
  }
  function conflicts(data,state){
    const defs=new Map(data.cards.map(c=>[c.id,c])),out=[];
    for(const p of packs){
      const chosen=new Map();
      for(const r of state.cards){
        if(r.vote!=='approve'||r.art[p].vote!=='approve')continue;
        const c=defs.get(r.id);
        const actorCells=new Set(c.art[p].filter(g=>g.role==='actor').flatMap(g=>g.cells));
        for(const key of r.art[p].preferred.filter(k=>actorCells.has(k))){
          if(chosen.has(key))out.push({pack:p,key,ideas:[chosen.get(key),r.id]});
          else chosen.set(key,r.id);
        }
      }
    }
    return out;
  }
  const API={SCHEMA,blank,checked,candidates,progress,conflicts};
  if(typeof module!=='undefined'&&module.exports)module.exports=API;
  root.SpriteIdeas=API;
  if(typeof document==='undefined')return;
  const data=JSON.parse(document.getElementById('ideas-data').textContent);
  const defs=new Map(data.cards.map(c=>[c.id,c]));
  const $=id=>document.getElementById(id);
  const escape=v=>String(v).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const packName={kenney:'Mixed Kenney',onebit:'Standalone 1-Bit'};
  const key=schema(data)+':'+data.fingerprint;
  let state=blank(data),active=data.cards.find(c=>!c.excluded).id,storageOkay=true;
  try{const saved=localStorage.getItem(key);if(saved)state=checked(data,JSON.parse(saved));}
  catch(e){storageOkay=false;$('message').textContent='Could not restore draft: '+e.message+' Stored data was not automatically overwritten.';}
  const record=id=>state.cards.find(r=>r.id===id);
  const style=document.createElement('style');
  style.textContent=Object.entries(data.sources).map(([name,s])=>{
    const scale=64/s.tile;
    return `.sprite.${name}{background-image:url(${s.image});background-size:${s.width*scale}px ${s.height*scale}px}`;
  }).join('');document.head.append(style);
  $('summary').textContent=data.summary_text||`${data.summary.proposals} proposals · ${data.summary.unused.toLocaleString()} unused cells accounted for · ${data.summary.held_groups} held groups · ${data.summary.needs_counterpart} ideas need a specific 1-Bit counterpart`;
  function save(){
    try{localStorage.setItem(key,JSON.stringify(checked(data,state)));storageOkay=true;}
    catch(_){storageOkay=false;}
    status();
  }
  function status(){
    const p=progress(data,state);
    $('progress').textContent=`${p.reviewed}/${p.total} ideas reviewed · ${p.approved} approve · ${p.rejected} reject · ${p.discussed} discuss · art ${p.artReviewed}/${p.artTotal}`;
    $('storage').textContent=storageOkay?'Draft in this browser · download JSON to send it back':'Draft not safely saved — download before closing';
  }
  function matches(c){
    const category=$('category').value,filter=$('status-filter').value,q=$('search').value.trim().toLowerCase(),r=record(c.id);
    if(category==='all'?c.excluded:c.kind!==category)return false;
    if(filter==='unreviewed'&&r.vote)return false;
    if(['approve','reject','discuss'].includes(filter)&&r.vote!==filter)return false;
    if(filter==='needs-art'&&!c.needs_counterpart)return false;
    if(filter==='next'&&r.priority!=='next')return false;
    return !q||[c.id,c.title,c.idea,c.existing,c.decisions,...c.groups.flatMap(g=>[g.label,...g.cells])].join(' ').toLowerCase().includes(q);
  }
  const visible=()=>data.cards.filter(matches);
  function queue(){
    const shown=visible();
    $('visible-count').textContent=data.queue_note?`${shown.length} shown · ${data.queue_note}`:`${shown.length} groups shown · ${data.summary.proposed_cells.toLocaleString()} proposed cells / ${data.summary.held_cells} held`;
    $('queue').innerHTML=shown.map(c=>{
      const r=record(c.id),mark={approve:'✓',reject:'×',discuss:'?','':'○'}[r.vote];
      return `<button type="button" data-idea="${c.id}" aria-current="${c.id===active}">${mark} ${escape(c.title)}<small>${escape(c.kind)} · ${c.candidate_count} candidate cells${c.needs_counterpart?' · needs 1-Bit art':''}</small></button>`;
    }).join('');
    return shown;
  }
  function votesHtml(scope,id,p,value,held=false,missing=false){
    return '<div class="votes" role="group" aria-label="'+(scope==='idea'?'Idea decision':'Artwork decision')+'">'+
      [['approve','Approve'],['reject','Reject'],['discuss','Discuss']].map(([v,label])=>
      `<button type="button" data-action="vote" data-scope="${scope}" data-id="${id}" data-pack="${p}" data-value="${v}" aria-pressed="${value===v}" ${(v==='approve'&&(held||missing))?'disabled':''}>${label}</button>`).join('')+'</div>';
  }
  function commentHtml(scope,id,p){
    return `<label class="comment">Comments / requested changes<textarea maxlength="4000" data-scope="${scope}" data-id="${id}" data-pack="${p}" placeholder="Change the mechanics, sprite reading, selected cells or assignment."></textarea></label>`;
  }
  function tileHtml(k,id,p,preferred){
    const [sheet,indexText]=k.split(':'),index=Number(indexText),s=data.sources[sheet],scale=64/s.tile;
    const x=index%s.columns*s.stride*scale,y=Math.floor(index/s.columns)*s.stride*scale;
    const others=data.cells[k].proposals.filter(other=>other!==id);
    const used=data.cells[k].usage?.[p]||[],suggested=defs.get(id).suggested?.[p]===k;
    const hint=[others.length?'Also proposed: '+others.map(id=>defs.get(id).title).join('; '):'Click to prefer this cell',used.length?'Already renders: '+used.join(', '):'',suggested?'Suggested starting sprite, not approved':''].filter(Boolean).join('. ');
    return `<button type="button" class="tile" data-action="prefer" data-id="${id}" data-pack="${p}" data-key="${k}" aria-pressed="${preferred.includes(k)}" title="${escape(hint)}"><span class="sprite ${sheet}" style="background-position:-${x}px -${y}px"></span>${escape(k)}${others.length?' ↔':''}${suggested?'<small class="suggested">Suggested</small>':''}${used.length?'<small class="reuse">Already used</small>':''}</button>`;
  }
  function artHtml(c,p){
    const r=record(c.id).art[p],groups=c.art[p],missing=!groups.length;
    const total=candidates(c,p).length;
    return `<section class="art-panel" id="art-${c.id}-${p}" data-pack="${p}"><h3>${packName[p]}</h3>`+
      (c.art_notes?.[p]?`<p class="notice">${escape(c.art_notes[p])}</p>`:'')+
      (missing?'<p class="counterpart">No specific counterpart is curated for this look yet. The idea can be reviewed, but this artwork cannot be approved. Suggest exact 1-Bit cells in your comments.</p>':
      `<p class="caption">${total} candidate cells · click optional preferences. ${p==='kenney'&&groups.some(g=>g.sheet==='onebit')?'Shared 1-Bit candidates render in monochrome within the mixed look.':''} Large thumbnails inspect native pixels, not a running-game capture.</p>`+
      groups.map((g,i)=>{
        const first=g.cells.slice(0,12),rest=g.cells.slice(12);
        return `<div class="group"><h4>${escape(g.label)} <span class="role">${escape(g.role)}</span></h4><div class="tiles">${first.map(k=>tileHtml(k,c.id,p,r.preferred)).join('')}</div>`+
          (rest.length?`<details data-more="${i}" data-pack="${p}"><summary>Show ${rest.length} more cells (${g.cells.length} in this family)</summary><div class="tiles more"></div></details>`:'')+'</div>';
      }).join(''))+
      `<p class="preferred">Preferred: ${escape(r.preferred.length?r.preferred.join(', '):'none — reviewing the pool')}${r.preferred.length?` <button data-action="clear-prefs" data-id="${c.id}" data-pack="${p}">Clear preferences</button>`:''}</p>`+
      votesHtml('art',c.id,p,r.vote,c.excluded,missing)+commentHtml('art',c.id,p)+'</section>';
  }
  function collisionStatus(){
    if(data.review_kind==='items-rooms'){
      const c=defs.get(active),used=[...new Set(c.groups.flatMap(g=>g.cells))].filter(k=>Object.values(data.cells[k].usage).some(ids=>ids.length));
      $('conflicts').textContent=used.length?'Current game reuse: '+used.map(k=>`${k}: ${[...new Set(Object.values(data.cells[k].usage).flat())].join(', ')}`).join('; ')+'. These are alternatives, not unused reservations. Comment if you want a different reading.':
        'These candidate cells have no current direct binding in either Kenney look. Shared candidates across this review remain alternatives, not reserved assignments.';
      return;
    }
    const warnings=conflicts(data,state);
    const current=warnings.filter(w=>w.ideas.includes(active));
    const c=defs.get(active),overlaps=[...new Set(c.groups.flatMap(g=>g.cells))].filter(k=>data.cells[k].proposals.length>1);
    $('conflicts').textContent=current.length?'Selected approved actor-art reuse: '+current.map(w=>`${w.key} in ${w.ideas.map(id=>defs.get(id).title).join(' / ')} (${packName[w.pack]})`).join('; ')+'. Resolve or explicitly comment on intentional sharing.':
      `${overlaps.length} candidate cells also appear in other ideas (↔). They are alternatives, not reserved assignments. ${warnings.length} selected actor-art conflicts across the whole review.`;
  }
  function render(){
    const shown=queue();
    if(!shown.some(c=>c.id===active))active=shown[0]?.id;
    queue();status();
    if(!active){$('content').innerHTML='<p class="empty">No matching ideas. Change the search or filters to continue.</p>';return;}
    const c=defs.get(active),r=record(active),ordinal=shown.findIndex(c=>c.id===active)+1;
    $('content').innerHTML=`<div class="card-nav"><button data-action="previous">← Previous</button><span>${ordinal} / ${shown.length} in this queue</span><button data-action="next">Next →</button></div>
      <article id="idea-${c.id}"><p class="eyebrow">${escape(c.kind)} · work: ${escape(c.effort)} · ${c.candidate_count} ${escape(data.candidate_label||'unused candidate cells')}</p><h2>${escape(c.title)}</h2><p><a href="#art-${c.id}-${$('look').value==='onebit'?'onebit':'kenney'}">See sprites and art decisions ↓</a></p><p class="intent">${escape(c.idea)}</p>
      <div class="context"><div><strong>Already in ROAG / why this differs</strong>${escape(c.existing)}</div><div><strong>Decisions and implementation risks</strong>${escape(c.decisions)}</div></div>`+
      (c.history?`<p class="notice history"><strong>Earlier decision: ${escape(c.history.vote)}.</strong> ${escape(c.history.note)}${c.history.comments.length?'<br>'+c.history.comments.map(escape).join('<br>'):''}</p>`:'')+
      (c.plan?`<section class="plan"><h3>Proposed player flow</h3><dl>${c.plan.map(([label,body])=>`<dt>${escape(label)}</dt><dd>${escape(body)}</dd>`).join('')}</dl><p class="example"><strong>Example: </strong>${escape(c.example)}</p><p class="caption">Numbers and behavior above are proposals for this vote; they have not been added to the game.</p></section>`:'')+
      (c.dependencies.length?`<p class="dependency">Related foundations to resolve first: ${c.dependencies.map(id=>`<button data-idea="${id}">${escape(defs.get(id).title)}</button>`).join(' ')}</p>`:'')+
      `<section class="decision"><h3>${c.excluded?'Held record — no new feature approval':'The idea / gameplay direction'}</h3>`+
      votesHtml('idea',c.id,'',r.vote,c.excluded)+
      (c.excluded?'':`<label class="priority">If approved, when?<select id="priority"><option value="">No priority chosen</option><option value="next">Next batch</option><option value="later">Later / keep in backlog</option></select></label><p><button data-action="approve-shown" ${c.needs_counterpart?'disabled':''}>Approve idea + both art pools shown</button> <button data-action="clear-votes">Clear votes</button></p><p class="caption">The combined button requires art for both looks. It does not approve other ideas or resolve comments/reuse conflicts.</p>`)+
      commentHtml('idea',c.id,'')+'</section><div class="art-grid">'+packs.map(p=>artHtml(c,p)).join('')+'</div><div id="conflicts" class="notice"></div></article>';
    for(const t of $('content').querySelectorAll('textarea'))t.value=target(t.dataset).comment;
    if($('priority'))$('priority').value=r.priority;
    for(const panel of $('content').querySelectorAll('.art-panel'))panel.hidden=$('look').value!=='both'&&panel.dataset.pack!==$('look').value;
    for(const details of $('content').querySelectorAll('details[data-more]'))details.addEventListener('toggle',()=>{
      if(details.open&&!details.dataset.loaded){
        const group=c.art[details.dataset.pack][Number(details.dataset.more)];
        details.querySelector('.more').innerHTML=group.cells.slice(12).map(k=>tileHtml(k,c.id,details.dataset.pack,r.art[details.dataset.pack].preferred)).join('');
        details.dataset.loaded='true';
      }
    });
    collisionStatus();
  }
  function target(d){return d.scope==='art'?record(d.id).art[d.pack]:record(d.id);}
  function move(direction){
    const list=visible(),index=list.findIndex(c=>c.id===active);
    if(list.length){active=list[(index+direction+list.length)%list.length].id;render();$('content').scrollIntoView({block:'start'});}
  }
  function open(id){
    if(!defs.has(id))return;
    if(!visible().some(c=>c.id===id)){$('search').value='';$('category').value=defs.get(id).excluded?'held':'all';$('status-filter').value='all';}
    active=id;render();$('content').scrollIntoView({block:'start'});
  }
  $('queue').addEventListener('click',e=>{const b=e.target.closest('[data-idea]');if(b)open(b.dataset.idea);});
  $('content').addEventListener('click',e=>{
    const link=e.target.closest('[data-idea]');if(link){open(link.dataset.idea);return;}
    const b=e.target.closest('[data-action]');if(!b||b.disabled)return;
    const action=b.dataset.action;
    if(action==='previous'||action==='next'){move(action==='next'?1:-1);return;}
    const r=record(active),c=defs.get(active);
    if(action==='vote'){
      const t=target(b.dataset);t.vote=t.vote===b.dataset.value?'':b.dataset.value;
    }else if(action==='prefer'){
      const a=r.art[b.dataset.pack],index=a.preferred.indexOf(b.dataset.key);
      if(index<0)a.preferred.push(b.dataset.key);else a.preferred.splice(index,1);a.preferred.sort();a.vote='';
    }else if(action==='clear-prefs'){r.art[b.dataset.pack].preferred=[];r.art[b.dataset.pack].vote='';}
    else if(action==='approve-shown'&&!c.excluded&&!c.needs_counterpart){r.vote='approve';for(const p of packs)r.art[p].vote='approve';}
    else if(action==='clear-votes'){r.vote='';for(const p of packs)r.art[p].vote='';}
    const y=window.scrollY,previous=active;
    const expanded=Array.from($('content').querySelectorAll('details[data-more][open]')).map(d=>[d.dataset.pack,d.dataset.more]);
    save();render();
    if(active===previous)for(const [p,i] of expanded){
      const d=$('content').querySelector(`details[data-pack="${p}"][data-more="${i}"]`);
      if(d)d.open=true;
    }
    window.scrollTo(0,y);
  });
  $('content').addEventListener('input',e=>{
    if(e.target.matches('textarea')){target(e.target.dataset).comment=e.target.value;save();}
  });
  $('content').addEventListener('change',e=>{if(e.target.id==='priority'){record(active).priority=e.target.value;save();queue();}});
  for(const id of ['category','status-filter','look'])$(id).addEventListener('change',render);
  $('search').addEventListener('input',render);
  window.addEventListener('keydown',e=>{
    if(e.ctrlKey||e.altKey||e.metaKey||e.shiftKey||e.target.closest('input,textarea,select,button,a'))return;
    if(e.key==='ArrowLeft'||e.key==='ArrowRight'){e.preventDefault();move(e.key==='ArrowRight'?1:-1);}
  });
  $('download').addEventListener('click',()=>{
    try{
      const value={...checked(data,state),exported_at:new Date().toISOString()};
      const url=URL.createObjectURL(new Blob([JSON.stringify(value,null,2)+'\n'],{type:'application/json'}));
      const a=document.createElement('a');a.href=url;a.download=data.feedback_filename||'roag-sprite-ideas-feedback.json';a.click();
      setTimeout(()=>URL.revokeObjectURL(url),1000);
      $('message').textContent='Feedback download requested. Partial reviews are valid; send the JSON file back when ready.';
    }catch(e){$('message').textContent='Could not export: '+e.message;}
  });
  $('import').addEventListener('click',()=>$('import-file').click());
  $('import-file').addEventListener('change',async e=>{
    try{
      const file=e.target.files[0];if(!file)return;
      if(file.size>2*1024*1024)throw Error('Feedback exceeds 2 MiB.');
      const replacement=checked(data,JSON.parse(await file.text()));
      state=replacement;save();render();$('message').textContent='Feedback imported. Earlier review files/storage are unchanged.';
    }catch(err){$('message').textContent='Import rejected: '+err.message+' Current draft is unchanged.';}
    finally{e.target.value='';}
  });
  render();
})(typeof globalThis!=='undefined'?globalThis:this);
