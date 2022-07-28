import { openDB } from './idb/build/index.js';
export async function createIndexedDb(storeName) {
    var instance = new SecretsForMeIndexedDb(storeName);
    await instance.initialize();
    return instance;
}
export function testStatup() {
    console.log('test startup');
}
export class SecretsForMeIndexedDb {
    storeName;
    db; // <SecretsForMeDb>;
    constructor(storeName) {
        this.storeName = storeName;
        console.log('SecretsForMeIndexedDb ctor', this.storeName);
    }
    async initialize() {
        const store = this.storeName;
        this.db = await openDB('SecretsForMe', 1, {
            upgrade(u) {
                u.createObjectStore(store);
            }
        });
    }
    async getBlob(key) {
        //console.log('index get', key);
        var bits = await this.db.get(this.storeName, key);
        //console.log('index got', bits);
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
                symmetricKeyId: null,
                etag: etag
            };
            await this.db.put(this.storeName, obj, key);
            return true;
        }
        var tx = this.db.transaction(this.storeName, 'readwrite');
        var storeObj = tx.objectStore(this.storeName);
        var existing = await storeObj.get(key);
        if (existing == null || existing.etag == expectedEtag) {
            var obj = {
                id: key,
                blobData: blob,
                contentType: contentType,
                symmetricKeyId: null,
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
        var tx = this.db.transaction(this.storeName, "readwrite");
        var storeObj = tx.objectStore(this.storeName);
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