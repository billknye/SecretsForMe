import { openDB } from './idb/build/index.js';
export async function createIndexedDb() {
    var instance = new SecretsForMeIndexedDb();
    await instance.initialize();
    return instance;
}
export class SecretsForMeIndexedDb {
    db;
    async initialize() {
        this.db = await openDB('SecretsForMe', 1, {
            upgrade(u) {
                u.createObjectStore('objects');
            }
        });
    }
    async getBlob(key) {
        var bits = await this.db.get('objects', key);
        if (bits)
            return bits;
        return null;
    }
    async storeBlob(key, hash, blob, expectedHash) {
        if (expectedHash == undefined || expectedHash == null) {
            var obj = {
                blobData: blob,
                hash: hash
            };
            await this.db.put('objects', obj, key);
            return true;
        }
        var tx = this.db.transaction('objects', 'readwrite');
        var storeObj = tx.objectStore('objects');
        var existing = await storeObj.get(key);
        if (existing == null || existing.hash == expectedHash) {
            var obj = {
                blobData: blob,
                hash: hash
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
    async removeBlob(key, hash) {
        var tx = this.db.transaction('objects', "readwrite");
        var storeObj = tx.objectStore('objects');
        var existing = await storeObj.get(key);
        if (existing.hash == hash) {
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