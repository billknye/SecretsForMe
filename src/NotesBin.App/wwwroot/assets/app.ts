
import { openDB, DBSchema, IDBPDatabase } from './idb/build/index.js';

interface NotesBinDocument {
    id: string,
    etag: string,
    contentType: string,
    blobData: Uint8Array
}

interface NotesBinDb extends DBSchema {
    objects: {
        key: string,
        value: {
            id: string,
            etag: string,
            contentType: string,
            blobData: Uint8Array
        },
        indexes: {}
    };
}

export async function createIndexedDb(): Promise<NotesBinIndexedDb> {
    var instance = new NotesBinIndexedDb();
    await instance.initialize();
    return instance;
}

export function testStatup(): void {
    console.log('test startup');
}

export class NotesBinIndexedDb {

    private db: IDBPDatabase<NotesBinDb>;

    public async initialize(): Promise<void> {
        this.db = await openDB<NotesBinDb>('NotesBin', 1, {
            upgrade(u: IDBPDatabase<NotesBinDb>) {
                u.createObjectStore('objects');
            }
        });
    }

    async getBlob(key: string): Promise<NotesBinDocument> {
        console.log('index get', key);

        var bits = await this.db.get('objects', key);

        console.log('index got', bits);

        if (bits)
            return bits as NotesBinDocument;

        return null;
    }

    async storeBlob(key: string, contentType: string, etag: string, blob: Uint8Array, expectedEtag: string | undefined): Promise<boolean> {
        if (expectedEtag == undefined || expectedEtag == null) {
            var obj: NotesBinDocument = {
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
        var existing = await storeObj.get(key) as NotesBinDocument;

        if (existing == null || (existing as NotesBinDocument).etag == expectedEtag) {
            var obj: NotesBinDocument = {
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

    async removeBlob(key: string, etag: string): Promise<boolean> {
        var tx = this.db.transaction('objects', "readwrite");
        var storeObj = tx.objectStore('objects');
        var existing = await storeObj.get(key) as NotesBinDocument;

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
