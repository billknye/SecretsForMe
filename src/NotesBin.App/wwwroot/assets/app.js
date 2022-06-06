import { openDB } from './idb/build/index.js';
export async function createIndexedDb() {
    var instance = new NotesBinIndexedDb();
    await instance.initialize();
    return instance;
}
export function testStatup() {
    console.log('test startup');
}
export class NotesBinIndexedDb {
    db;
    async initialize() {
        this.db = await openDB('NotesBin', 1, {
            upgrade(u) {
                u.createObjectStore('objects');
            }
        });
    }
    async getBlob(key) {
        console.log('index get', key);
        var bits = await this.db.get('objects', key);
        console.log('index got', bits);
        if (bits)
            return bits;
        return null;
    }
    async storeBlob(key, contentType, etag, blob, expectedEtag) {
        if (expectedEtag == undefined || expectedEtag == null) {
            var obj = {
                id: key,
                blobData: blob,
                contentType: contentType,
                etag: etag
            };
            await this.db.put('objects', obj, key);
            return true;
        }
        var tx = this.db.transaction('objects', 'readwrite');
        var storeObj = tx.objectStore('objects');
        var existing = await storeObj.get(key);
        if (existing == null || existing.etag == expectedEtag) {
            var obj = {
                id: key,
                blobData: blob,
                contentType: contentType,
                etag: etag
            };
            await storeObj.put(obj, key);
            await tx.done;
            return true;
        }
        else {
            await tx.done;
            return false;
        }
    }
    async removeBlob(key, etag) {
        var tx = this.db.transaction('objects', "readwrite");
        var storeObj = tx.objectStore('objects');
        var existing = await storeObj.get(key);
        if (existing.etag == etag) {
            await storeObj.delete(key);
            await tx.done;
            return true;
        }
        else {
            await tx.done;
            return false;
        }
    }
}
//# sourceMappingURL=app.js.map