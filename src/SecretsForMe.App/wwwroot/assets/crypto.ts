
export async function getDerivedBytes(password: string, iterations: number, salt: Uint8Array): Promise<Uint8Array> {

    const enc = new TextEncoder();
    var keyMaterial = await window.crypto.subtle.importKey(
        "raw",
        enc.encode(password),
        { name: "PBKDF2" },
        false,
        ["deriveBits", "deriveKey"]
    );

    const derivedBits = await window.crypto.subtle.deriveBits(
        {
            "name": "PBKDF2",
            salt: salt,
            "iterations": iterations,
            "hash": "SHA-256"
        },
        keyMaterial,
        256);

    return new Uint8Array(derivedBits, 0, 32);
}

export async function getRsaKeyPair(length: number): Promise<any> {
    var key = await window.crypto.subtle.generateKey(
        {
            name: "RSA-OAEP",
            modulusLength: length, //can be 1024, 2048, or 4096
            publicExponent: new Uint8Array([0x01, 0x00, 0x01]),
            hash: { name: "SHA-256" }, //can be "SHA-1", "SHA-256", "SHA-384", or "SHA-512"
        },
        true, //whether the key is extractable (i.e. can be used in exportKey)
        ["encrypt", "decrypt"] //must be ["encrypt", "decrypt"] or ["wrapKey", "unwrapKey"]
    );

    var publicKey = await window.crypto.subtle.exportKey("spki", key.publicKey);
    var privateKey = await window.crypto.subtle.exportKey("pkcs8", key.privateKey);

    var ret = { publicKey: new Uint8Array(publicKey, 0, publicKey.byteLength), privateKey: new Uint8Array(privateKey, 0, privateKey.byteLength) };
    return ret;
}

export async function getAesKey(length: number): Promise<Uint8Array> {
    let key = await window.crypto.subtle.generateKey(
        {
            name: "AES-GCM",
            length: length
        },
        true,
        ["encrypt", "decrypt"]
    );

    var extracted = await window.crypto.subtle.exportKey("raw", key);
    return new Uint8Array(extracted, 0, extracted.byteLength);
}

export async function aesEncrypt(iv: Uint8Array, aesKey: Uint8Array, rawData: Uint8Array): Promise<Uint8Array> {
    let key = await window.crypto.subtle.importKey("raw", aesKey.buffer, "AES-GCM", false, ["encrypt", "decrypt"]);
    var encrypted = await window.crypto.subtle.encrypt({ name: "AES-GCM", iv: iv.buffer }, key, rawData.buffer) as ArrayBuffer;
    return new Uint8Array(encrypted, 0, encrypted.byteLength);
}

export async function aesDecrypt(iv: Uint8Array, aesKey: Uint8Array, encryptedData: Uint8Array): Promise<Uint8Array> {
    let key = await window.crypto.subtle.importKey("raw", aesKey.buffer, "AES-GCM", false, ["encrypt", "decrypt"]);
    var decrypted = await window.crypto.subtle.decrypt({ name: "AES-GCM", iv: iv.buffer }, key, encryptedData) as ArrayBuffer;
    return new Uint8Array(decrypted, 0, decrypted.byteLength);
}

export async function rsaEncrypt(publicKey: Uint8Array, rawData: Uint8Array): Promise<Uint8Array> {
    let key = await window.crypto.subtle.importKey("spki", publicKey.buffer, { name: "RSA-OAEP", hash: "SHA-256" }, false, ["encrypt"]);
    var encrypted = await window.crypto.subtle.encrypt({ name: "RSA-OAEP" }, key, rawData.buffer) as ArrayBuffer;
    return new Uint8Array(encrypted, 0, encrypted.byteLength);
}

export async function rsaDecrypt(privateKey: Uint8Array, encryptedData: Uint8Array): Promise<Uint8Array> {
    let key = await window.crypto.subtle.importKey("pkcs8", privateKey.buffer, { name: "RSA-OAEP", hash: "SHA-256" }, false, ["decrypt"]);
    var decrypted = await window.crypto.subtle.decrypt({ name: "RSA-OAEP" }, key, encryptedData.buffer) as ArrayBuffer;
    return new Uint8Array(decrypted, 0, decrypted.byteLength);
}