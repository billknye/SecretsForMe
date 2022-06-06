
import { IDBPDatabase, openDB } from './idb/build/esm/index.js';

interface NotesBinDocument {
    etag: string,
    contentType: string,
    blob: Uint8Array
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

    private db: IDBPDatabase;

    public async initialize(): Promise<void> {
        this.db = await openDB('NotesBin', 1, {
            upgrade(u) {
                u.createObjectStore('objects');
            }
        } );
    }

    async getBlob(key: string): Promise<Uint8Array> {
        var bits = await this.db.get('objects', key);
        if (bits instanceof Uint8Array)
            return bits as Uint8Array;

        return null;
    }

    async storeBlob(key: string, contentType: string, etag: string, blob: Uint8Array, expectedEtag: string | undefined): Promise<boolean> {
        if (expectedEtag == undefined || expectedEtag == null) {
            var obj: NotesBinDocument = {
                blob: blob,
                contentType: contentType,
                etag: etag
            };

            await this.db.put('objects', obj, key);
            return true;
        }

        var tx = this.db.transaction('objects', "readwrite");
        var storeObj = tx.objectStore('objects');
        var existing = await storeObj.get(key) as NotesBinDocument;

        if (existing == null || (existing as NotesBinDocument).etag == expectedEtag) {
            var obj: NotesBinDocument = {
                blob: blob,
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
