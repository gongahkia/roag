import { parseSave, encodeSave } from './schema.ts';
import type { Save } from './schema.ts';
export const DATABASE='roag-colony', STORE='saves', SLOT='active';
export class SaveStore {
  constructor(private slot=SLOT) {}
  private async open():Promise<IDBDatabase> {
    return new Promise((resolve,reject)=>{
      let request:IDBOpenDBRequest;
      try {request=indexedDB.open(DATABASE,1);}catch(error){reject(error);return;}
      request.onupgradeneeded=()=>{request.result.createObjectStore(STORE);};
      request.onerror=()=>reject(request.error??new Error('Browser storage unavailable'));
      request.onblocked=()=>reject(new Error('Browser storage is blocked by another tab'));
      request.onsuccess=()=>resolve(request.result);
    });
  }
  async load():Promise<Save|null> {
    const db=await this.open();
    try {return await new Promise((resolve,reject)=>{
      const tx=db.transaction(STORE,'readonly');const request=tx.objectStore(STORE).get(this.slot);let result:Save|null=null;
      request.onsuccess=()=>{try{if(request.result!==undefined)result=parseSave(request.result as string);}catch(e){reject(e);}};
      tx.oncomplete=()=>resolve(result);tx.onabort=()=>reject(tx.error??new Error('Save read failed'));tx.onerror=()=>reject(tx.error);
    });}finally{db.close();}
  }
  async commit(save:Save):Promise<void> {
    const text=encodeSave(save); // Validation completes before opening any write transaction.
    const db=await this.open();
    try {await new Promise<void>((resolve,reject)=>{
      const tx=db.transaction(STORE,'readwrite');
      tx.oncomplete=()=>resolve();tx.onabort=()=>reject(tx.error??new Error('Save transaction aborted'));tx.onerror=()=>reject(tx.error);
      try{tx.objectStore(STORE).put(text,this.slot);}catch(error){tx.abort();reject(error);}
    });}finally{db.close();}
  }
}
