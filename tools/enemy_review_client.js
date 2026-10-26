/* Approval-page client only. No game code, network requests or runtime bindings. */
(function(root) {
  'use strict';
  const SCHEMA='roag-enemy-combo-feedback/1';
  const votes=['','approve','reject','discuss'];
  const key=cell=>cell.sheet+':'+cell.index;
  function blank(data) {
    return {schema:SCHEMA,proposal_fingerprint:data.fingerprint,
      audit_mapping_checksum:data.audit.mapping_checksum,
      combos:data.combos.map(c=>({id:c.id,vote:'',comment:'',cue:{
        choice:key(c.cue.candidates[0]),vote:'',comment:''}})),
      roles:data.roles.map(r=>({id:r.id,art:Object.fromEntries(['kenney','onebit'].map(pack=>
        [pack,{choice:key(r.candidates[pack][0]),vote:'',comment:''}]))}))};
  }
  function checked(data,raw) {
    if(!raw || raw.schema!==SCHEMA)throw Error('This is not an enemy combination feedback file. Older sprite/room reviews use different files.');
    if(raw.proposal_fingerprint!==data.fingerprint || raw.audit_mapping_checksum!==data.audit.mapping_checksum)
      throw Error('This feedback reviews different proposals or source artwork. Keep the original file and review the current version separately.');
    const text=v=>{if(typeof v!=='string'||v.length>4000)throw Error('Invalid or overlong comment.');return v;};
    function decision(value,options) {
      if(!value||!votes.includes(value.vote))throw Error('Invalid review decision.');
      const out={vote:value.vote,comment:text(value.comment)};
      if(options) {
        if(!options.some(c=>key(c)===value.choice))throw Error('Sprite selection is not a candidate for this role.');
        out.choice=value.choice;
      }
      return out;
    }
    function records(rawItems,defs,convert) {
      if(!Array.isArray(rawItems)||rawItems.length!==defs.length)throw Error('Missing review records.');
      const byId=new Map();
      for(const item of rawItems) {
        if(!item||byId.has(item.id)||!defs.some(d=>d.id===item.id))throw Error('Duplicate or unknown review ID.');
        byId.set(item.id,item);
      }
      return defs.map(d=>convert(d,byId.get(d.id)));
    }
    return {schema:SCHEMA,proposal_fingerprint:data.fingerprint,
      audit_mapping_checksum:data.audit.mapping_checksum,
      combos:records(raw.combos,data.combos,(d,r)=>({id:d.id,...decision(r),cue:decision(r.cue,d.cue.candidates)})),
      roles:records(raw.roles,data.roles,(d,r)=>({id:d.id,art:Object.fromEntries(['kenney','onebit'].map(p=>
        [p,decision(r.art&&r.art[p],d.candidates[p])]))}))};
  }
  function progress(state) {
    const decisions=state.combos.flatMap(c=>[c.vote,c.cue.vote]).concat(
      state.roles.flatMap(r=>[r.art.kenney.vote,r.art.onebit.vote]));
    return {total:decisions.length,reviewed:decisions.filter(Boolean).length,
      approved:decisions.filter(v=>v==='approve').length,
      rejected:decisions.filter(v=>v==='reject').length,
      discussed:decisions.filter(v=>v==='discuss').length};
  }
  const API={SCHEMA,blank,checked,progress,key};
  if(typeof module!=='undefined'&&module.exports)module.exports=API;
  root.EnemyReview=API;
  if(typeof document==='undefined')return;

  const data=JSON.parse(document.getElementById('enemy-data').textContent);
  const roleMap=new Map(data.roles.map(r=>[r.id,r]));
  const $=id=>document.getElementById(id);
  const storageKey=SCHEMA+':'+data.fingerprint;
  const escape=v=>String(v).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const packLabel={kenney:'Mixed Kenney',onebit:'Standalone 1-Bit'};
  let state=blank(data),active=data.combos[0].id,storageOkay=true;
  try { const raw=localStorage.getItem(storageKey);if(raw)state=checked(data,JSON.parse(raw)); }
  catch(error){$('message').textContent='Saved draft could not be loaded: '+error.message+' Your original storage was not overwritten.';}
  const roleState=id=>state.roles.find(r=>r.id===id);
  const comboState=id=>state.combos.find(c=>c.id===id);
  function save() {
    try { localStorage.setItem(storageKey,JSON.stringify(state));storageOkay=true; }
    catch(_){storageOkay=false;}
    status();
  }
  function status() {
    const p=progress(state);
    $('progress').textContent=`${p.reviewed}/${p.total} reviewed · ${p.approved} approve · ${p.rejected} reject · ${p.discussed} discuss`;
    $('storage').textContent=storageOkay?'Draft saved in this browser; download JSON to send it back.':
      'Browser storage unavailable — download JSON before closing.';
    scrollOffset();
    for(const button of $('tabs').querySelectorAll('button'))button.setAttribute('aria-pressed',String(button.dataset.combo===active));
  }
  function scrollOffset() {
    document.documentElement.style.setProperty('--review-toolbar-height',
      (document.querySelector('.toolbar').getBoundingClientRect().height+16)+'px');
  }
  window.addEventListener('resize',scrollOffset);
  function votesHtml(scope,id,pack,value) {
    return '<div class="votes" role="group" aria-label="Review decision">'+
      [['approve','Approve'],['reject','Reject'],['discuss','Discuss']].map(([v,label])=>
        `<button type="button" data-action="vote" data-scope="${scope}" data-id="${id}" data-pack="${pack||''}" data-value="${v}" aria-pressed="${value===v}">${label}</button>`).join('')+'</div>';
  }
  function commentHtml(scope,id,pack) {
    return `<label class="comment">Comments or changes<textarea maxlength="4000" data-scope="${scope}" data-id="${id}" data-pack="${pack||''}" placeholder="Change the sprite, role, effect or rules. Nothing is approved by default."></textarea></label>`;
  }
  function spriteCanvas(cell,size=80) {
    return `<canvas class="sprite" width="${size}" height="${size}" data-sheet="${cell.sheet}" data-index="${cell.index}" aria-label="${escape(data.sources[cell.sheet].label)} cell ${cell.index}"></canvas>`;
  }
  function candidatesHtml(options,current,scope,id,pack) {
    return '<div class="candidates">'+options.map((c,i)=>`<button type="button" class="candidate" data-action="choose" data-scope="${scope}" data-id="${id}" data-pack="${pack||''}" data-choice="${c.key}" aria-pressed="${current===c.key}">
      ${spriteCanvas(c)}<strong>${i===0?'Proposed':'Alternative'} · ${escape(data.sources[c.sheet].label)} #${c.index}</strong>
      <span>${escape(c.label)}</span><small>Currently unbound in both packs${c.other_proposals.length?' · also proposed elsewhere':''}</small></button>`).join('')+'</div>';
  }
  function roleHtml(role) {
    const record=roleState(role.id);
    return `<article class="role" id="role-${role.id}"><h3>${escape(role.name)}</h3><p>${escape(role.behaviour)}</p><p class="overlap"><strong>Existing overlap:</strong> ${escape(role.overlap)}</p><div class="pair">`+
      ['kenney','onebit'].map(pack=>`<section class="art-panel"><h4>${packLabel[pack]} assignment</h4>
        ${candidatesHtml(role.candidates[pack],record.art[pack].choice,'art',role.id,pack)}
        <p class="caption">40px game footprint: <canvas class="sprite at-scale" width="40" height="40" data-role="${role.id}" data-pack="${pack}"></canvas> Full untrimmed source cell. Large thumbnails are for inspection.</p>
        ${votesHtml('art',role.id,pack,record.art[pack].vote)}${commentHtml('art',role.id,pack)}</section>`).join('')+'</div></article>';
  }
  function render() {
    const combo=data.combos.find(c=>c.id===active),record=comboState(active);
    $('tabs').innerHTML=data.combos.map((c,i)=>`<button type="button" data-combo="${c.id}" aria-pressed="${c.id===active}">${i+1}. ${escape(c.name)}</button>`).join('');
    $('content').innerHTML=`<section class="combo"><p class="eyebrow">Combination ${data.combos.indexOf(combo)+1} / 5 · Awaiting your review</p>
      <h2>${escape(combo.name)}</h2><p class="intent">${escape(combo.intent)}</p>
      <div class="pair"><figure><canvas class="formation" width="336" height="216" data-formation="kenney"></canvas><figcaption>Mixed Kenney · native sprites at 40px</figcaption></figure>
      <figure><canvas class="formation" width="336" height="216" data-formation="onebit"></canvas><figcaption>1-Bit · native sprites at 40px</figcaption></figure></div>
      <p class="caption">Formation diagram, not a running-game capture. W marks the support/leader; G marks its partner. The player starts at the left; side approaches stay open. Extra rings, links and facing lines are proposed drawn overlays.</p>
      <h3>Proposed interaction</h3><ul>${combo.rules.map(rule=>'<li>'+escape(rule)+'</li>').join('')}</ul>
      <p><strong>Counterplay:</strong> ${escape(combo.counterplay)}</p><p class="caption">${escape(combo.biomes)}</p>
      <h3>Combination behaviour</h3>${votesHtml('combo',combo.id,'',record.vote)}${commentHtml('combo',combo.id,'')}
      ${combo.roles.map(id=>roleHtml(roleMap.get(id))).join('')}
      <section class="cue"><h3>${escape(combo.cue.name)} · shared effect candidate</h3><p>${escape(combo.cue.purpose)}</p>
      <p class="caption">Both looks already use 1-Bit symbols for effects. These are additional static-art proposals, not published bindings.</p>
      ${candidatesHtml(combo.cue.candidates,record.cue.choice,'cue',combo.id,'')}
      ${votesHtml('cue',combo.id,'',record.cue.vote)}${commentHtml('cue',combo.id,'')}
      </section><div id="conflicts" class="notice"></div>
      <div class="navigation"><button data-action="previous">Previous combination</button><button data-action="next">Next combination</button></div></section>`;
    for(const textarea of $('content').querySelectorAll('textarea'))textarea.value=target(textarea.dataset).comment;
    showConflicts();status();paint();
  }
  function target({scope,id,pack}) {
    if(scope==='art')return roleState(id).art[pack];
    if(scope==='cue')return comboState(id).cue;
    return comboState(id);
  }
  function showConflicts() {
    const conflicts=[];
    for(const pack of ['kenney','onebit']) {
      const used=new Map();
      for(const role of data.roles) {
        const choice=roleState(role.id).art[pack].choice;
        if(used.has(choice))conflicts.push(`${packLabel[pack]}: ${roleMap.get(used.get(choice)).name} and ${role.name} both select ${choice}.`);
        else used.set(choice,role.id);
      }
    }
    $('conflicts').textContent=conflicts.length?'Shared selected actor art: '+conflicts.join(' ')+' Comment if this reuse is intentional.':
      'Selected primary actor silhouettes are distinct. Choices are proposals until you explicitly approve them.';
  }
  $('tabs').addEventListener('click',event=>{const b=event.target.closest('[data-combo]');if(b){active=b.dataset.combo;render();$('content').scrollIntoView();}});
  $('content').addEventListener('click',event=>{
    const b=event.target.closest('[data-action]');if(!b)return;
    const {action}=b.dataset;
    if(action==='previous'||action==='next') {
      const index=data.combos.findIndex(c=>c.id===active);
      active=data.combos[(index+(action==='next'?1:4))%5].id;render();$('content').scrollIntoView();return;
    }
    const record=target(b.dataset);
    if(action==='vote')record.vote=record.vote===b.dataset.value?'':b.dataset.value;
    if(action==='choose') {
      if(record.choice!==b.dataset.choice)record.vote='';
      record.choice=b.dataset.choice;
    }
    save();render();
  });
  $('content').addEventListener('input',event=>{
    if(!event.target.matches('textarea'))return;
    target(event.target.dataset).comment=event.target.value;save();
  });
  function download(value,name) {
    const url=URL.createObjectURL(new Blob([JSON.stringify(value,null,2)+'\n'],{type:'application/json'}));
    const a=document.createElement('a');a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);
  }
  $('export').addEventListener('click',()=>download({...checked(data,state),exported_at:new Date().toISOString()},'roag-enemy-combo-feedback.json'));
  $('import').addEventListener('click',()=>$('import-file').click());
  $('import-file').addEventListener('change',async event=>{
    try {
      const file=event.target.files[0];if(!file)return;
      if(file.size>1024*1024)throw Error('Feedback file is too large.');
      const replacement=checked(data,JSON.parse(await file.text()));
      state=replacement;save();render();$('message').textContent='Feedback imported. Existing sprite/room review storage is unchanged.';
    } catch(error){$('message').textContent='Import rejected: '+error.message+' Your current draft is unchanged.';}
    finally{event.target.value='';}
  });
  const images={};
  function drawCell(ctx,cell,x,y,size) {
    const image=images[cell.sheet],meta=data.sources[cell.sheet];
    if(!image||!image.ready)return;
    ctx.imageSmoothingEnabled=false;
    ctx.drawImage(image.drawable,cell.index%meta.columns*meta.stride,Math.floor(cell.index/meta.columns)*meta.stride,
      meta.tile,meta.tile,x,y,size,size);
  }
  function selectedCell(role,pack) {
    return roleMap.get(role).candidates[pack].find(c=>c.key===roleState(role).art[pack].choice);
  }
  function paint() {
    for(const canvas of document.querySelectorAll('canvas.sprite')) {
      const ctx=canvas.getContext('2d');ctx.clearRect(0,0,canvas.width,canvas.height);
      const c=canvas.dataset.role?selectedCell(canvas.dataset.role,canvas.dataset.pack):
        {sheet:canvas.dataset.sheet,index:Number(canvas.dataset.index)};
      drawCell(ctx,c,0,0,canvas.width);
    }
    const combo=data.combos.find(c=>c.id===active);
    for(const canvas of document.querySelectorAll('canvas.formation')) {
      const ctx=canvas.getContext('2d'),pack=canvas.dataset.formation;
      ctx.fillStyle=pack==='onebit'?'#151515':'#1c2925';ctx.fillRect(0,0,336,216);
      ctx.strokeStyle=pack==='onebit'?'#3c3c3c':'#3b5147';ctx.lineWidth=1;
      for(let y=0;y<5;y++)for(let x=0;x<8;x++)ctx.strokeRect(8+x*40,8+y*40,40,40);
      const support=combo.roster[0];
      if(combo.id==='warded_bodyguards'||combo.id==='healer_rushwings'||combo.id==='captain_and_followers') {
        ctx.strokeStyle=pack==='onebit'?'#aaa':'#82c6dc';ctx.setLineDash([3,3]);
        for(const point of combo.roster.slice(1)) {
          ctx.beginPath();ctx.moveTo(28+support.x*40,28+support.y*40);ctx.lineTo(28+point.x*40,28+point.y*40);ctx.stroke();
          ctx.beginPath();ctx.arc(28+point.x*40,28+point.y*40,19,0,Math.PI*2);ctx.stroke();
        }
        ctx.setLineDash([]);
      }
      const player=data.references.find(r=>r.pack===pack&&r.id==='core:actor/adventurer');
      drawCell(ctx,player,8,88,40);
      for(const point of combo.roster)drawCell(ctx,selectedCell(point.role,pack),8+point.x*40,8+point.y*40,40);
      ctx.fillStyle=pack==='onebit'?'#eee':'#d8e4d4';ctx.font='11px monospace';
      ctx.fillText('P',10,86);
      for(const [index,point]of combo.roster.entries())ctx.fillText(index===0?'W':'G',10+point.x*40,8+point.y*40);
    }
  }
  for(const [name,meta]of Object.entries(data.sources)) {
    const image=new Image();images[name]={ready:false};
    image.onload=()=>{
      const canvas=document.createElement('canvas');canvas.width=meta.width;canvas.height=meta.height;
      const ctx=canvas.getContext('2d');ctx.drawImage(image,0,0);
      const pixels=ctx.getImageData(0,0,meta.width,meta.height);
      for(let i=0;i<pixels.data.length;i+=4) {
        const p=pixels.data;
        if(['micro','tiny','rpg'].includes(name)&&p[i]===34&&p[i+1]===35&&p[i+2]===35)p[i+3]=0;
        if(name==='onebit')p[i]=p[i+1]=p[i+2]=Math.round((p[i]*.2126+p[i+1]*.7152+p[i+2]*.0722)*.9);
      }
      ctx.putImageData(pixels,0,0);images[name]={drawable:canvas,ready:true};paint();
    };
    image.onerror=()=>$('message').textContent='An embedded sprite sheet failed to load: '+meta.label;
    image.src=meta.image;
  }
  $('audit-summary').textContent=`Current game: ${data.audit.requirements} sprite bindings; both Kenney looks resolve all of them. This review adds zero bindings.`;
  $('sheet-table').innerHTML=data.audit.sheets.map(s=>`<tr><td>${escape(data.sources[s.sheet].label)}</td><td>${s.nonempty}</td><td>${s.union}</td><td>${s.unused_union}</td></tr>`).join('');
  $('fingerprint').textContent=data.fingerprint.slice(0,16);
  render();
})(typeof globalThis!=='undefined'?globalThis:this);
