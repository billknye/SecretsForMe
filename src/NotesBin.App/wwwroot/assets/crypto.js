export async function getDerivedBytes(password, iterations, salt) {
    const enc = new TextEncoder();
    var keyMaterial = await window.crypto.subtle.importKey("raw", enc.encode(password), { name: "PBKDF2" }, false, ["deriveBits", "deriveKey"]);
    const derivedBits = await window.crypto.subtle.deriveBits({
        "name": "PBKDF2",
        salt: salt,
        "iterations": iterations,
        "hash": "SHA-256"
    }, keyMaterial, 256);
    return new Uint8Array(derivedBits, 0, 32);
}
export async function getRsaKeyPair(length) {
    var key = await window.crypto.subtle.generateKey({
        name: "RSA-OAEP",
        modulusLength: length,
        publicExponent: new Uint8Array([0x01, 0x00, 0x01]),
        hash: { name: "SHA-256" }, //can be "SHA-1", "SHA-256", "SHA-384", or "SHA-512"
    }, true, //whether the key is extractable (i.e. can be used in exportKey)
    ["encrypt", "decrypt"] //must be ["encrypt", "decrypt"] or ["wrapKey", "unwrapKey"]
    );
    var publicKey = await window.crypto.subtle.exportKey("spki", key.publicKey);
    var privateKey = await window.crypto.subtle.exportKey("pkcs8", key.privateKey);
    var ret = { publicKey: new Uint8Array(publicKey, 0, publicKey.byteLength), privateKey: new Uint8Array(privateKey, 0, privateKey.byteLength) };
    return ret;
}
export async function getAesKey(length) {
    let key = await window.crypto.subtle.generateKey({
        name: "AES-GCM",
        length: length
    }, true, ["encrypt", "decrypt"]);
    var extracted = await window.crypto.subtle.exportKey("raw", key);
    return new Uint8Array(extracted, 0, extracted.byteLength);
}
export async function aesEncrypt(iv, aesKey, rawData) {
    let key = await window.crypto.subtle.importKey("raw", aesKey.buffer, "AES-GCM", false, ["encrypt", "decrypt"]);
    var encrypted = await window.crypto.subtle.encrypt({ name: "AES-GCM", iv: iv.buffer }, key, rawData.buffer);
    return new Uint8Array(encrypted, 0, encrypted.byteLength);
}
export async function aesDecrypt(iv, aesKey, encryptedData) {
    let key = await window.crypto.subtle.importKey("raw", aesKey.buffer, "AES-GCM", false, ["encrypt", "decrypt"]);
    var decrypted = await window.crypto.subtle.decrypt({ name: "AES-GCM", iv: iv.buffer }, key, encryptedData);
    return new Uint8Array(decrypted, 0, decrypted.byteLength);
}
export async function rsaEncrypt(publicKey, rawData) {
    let key = await window.crypto.subtle.importKey("spki", publicKey.buffer, { name: "RSA-OAEP", hash: "SHA-256" }, false, ["encrypt"]);
    var encrypted = await window.crypto.subtle.encrypt({ name: "RSA-OAEP" }, key, rawData.buffer);
    return new Uint8Array(encrypted, 0, encrypted.byteLength);
}
export async function rsaDecrypt(privateKey, encryptedData) {
    let key = await window.crypto.subtle.importKey("pkcs8", privateKey.buffer, { name: "RSA-OAEP", hash: "SHA-256" }, false, ["decrypt"]);
    var decrypted = await window.crypto.subtle.decrypt({ name: "RSA-OAEP" }, key, encryptedData.buffer);
    return new Uint8Array(decrypted, 0, decrypted.byteLength);
}
//# sourceMappingURL=crypto.js.map