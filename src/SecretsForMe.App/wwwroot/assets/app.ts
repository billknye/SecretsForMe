
import { openDB, IDBPDatabase } from './idb/build/index.js';

interface SecretsForMeDocument {
    id: string,
    etag: string,
    contentType: string,
    symmetricKeyId: string | null,
    blobData: Uint8Array
}

export async function createIndexedDb(storeName: string): Promise<SecretsForMeIndexedDb> {
    var instance = new SecretsForMeIndexedDb(storeName);
    await instance.initialize();
    return instance;
}

export class SecretsForMeIndexedDb {
    private db: IDBPDatabase; // <SecretsForMeDb>;

    constructor(private storeName: string) {
    }

    public async initialize(): Promise<void> {
        const store = this.storeName;

        this.db = await openDB('SecretsForMe', 1, {
            upgrade(u: IDBPDatabase) {
                u.createObjectStore(store);
            }
        });
    }

    async getBlob(key: string): Promise<SecretsForMeDocument> {
        var bits = await this.db.get(this.storeName, key);
        if (bits)
            return bits as SecretsForMeDocument;

        return null;
    }

    async storeBlob(key: string, contentType: string, etag: string, blob: Uint8Array, expectedEtag: string | undefined): Promise<boolean> {
        if (expectedEtag == undefined || expectedEtag == null) {
            var obj: SecretsForMeDocument = {
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
        var existing = await storeObj.get(key) as SecretsForMeDocument;

        if (existing == null || (existing as SecretsForMeDocument).etag == expectedEtag) {
            var obj: SecretsForMeDocument = {
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

    async removeBlob(key: string, etag: string): Promise<boolean> {
        var tx = this.db.transaction(this.storeName, "readwrite");
        var storeObj = tx.objectStore(this.storeName);
        var existing = await storeObj.get(key) as SecretsForMeDocument;

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
