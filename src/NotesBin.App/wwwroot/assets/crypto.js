var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
export function getDerivedBytes(password, iterations, salt) {
    return __awaiter(this, void 0, void 0, function () {
        var enc, keyMaterial, derivedBits;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0:
                    enc = new TextEncoder();
                    return [4 /*yield*/, window.crypto.subtle.importKey("raw", enc.encode(password), { name: "PBKDF2" }, false, ["deriveBits", "deriveKey"])];
                case 1:
                    keyMaterial = _a.sent();
                    return [4 /*yield*/, window.crypto.subtle.deriveBits({
                            "name": "PBKDF2",
                            salt: salt,
                            "iterations": iterations,
                            "hash": "SHA-256"
                        }, keyMaterial, 256)];
                case 2:
                    derivedBits = _a.sent();
                    return [2 /*return*/, new Uint8Array(derivedBits, 0, 32)];
            }
        });
    });
}
export function getRsaKeyPair(length) {
    return __awaiter(this, void 0, void 0, function () {
        var key, publicKey, privateKey, ret;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0: return [4 /*yield*/, window.crypto.subtle.generateKey({
                        name: "RSA-OAEP",
                        modulusLength: length,
                        publicExponent: new Uint8Array([0x01, 0x00, 0x01]),
                        hash: { name: "SHA-256" }, //can be "SHA-1", "SHA-256", "SHA-384", or "SHA-512"
                    }, true, //whether the key is extractable (i.e. can be used in exportKey)
                    ["encrypt", "decrypt"] //must be ["encrypt", "decrypt"] or ["wrapKey", "unwrapKey"]
                    )];
                case 1:
                    key = _a.sent();
                    return [4 /*yield*/, window.crypto.subtle.exportKey("spki", key.publicKey)];
                case 2:
                    publicKey = _a.sent();
                    return [4 /*yield*/, window.crypto.subtle.exportKey("pkcs8", key.privateKey)];
                case 3:
                    privateKey = _a.sent();
                    ret = { publicKey: new Uint8Array(publicKey, 0, publicKey.byteLength), privateKey: new Uint8Array(privateKey, 0, privateKey.byteLength) };
                    return [2 /*return*/, ret];
            }
        });
    });
}
export function getAesKey(length) {
    return __awaiter(this, void 0, void 0, function () {
        var key, extracted;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0: return [4 /*yield*/, window.crypto.subtle.generateKey({
                        name: "AES-GCM",
                        length: length
                    }, true, ["encrypt", "decrypt"])];
                case 1:
                    key = _a.sent();
                    return [4 /*yield*/, window.crypto.subtle.exportKey("raw", key)];
                case 2:
                    extracted = _a.sent();
                    return [2 /*return*/, new Uint8Array(extracted, 0, extracted.byteLength)];
            }
        });
    });
}
export function aesEncrypt(iv, aesKey, rawData) {
    return __awaiter(this, void 0, void 0, function () {
        var key, encrypted;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0: return [4 /*yield*/, window.crypto.subtle.importKey("raw", aesKey.buffer, "AES-GCM", false, ["encrypt", "decrypt"])];
                case 1:
                    key = _a.sent();
                    return [4 /*yield*/, window.crypto.subtle.encrypt({ name: "AES-GCM", iv: iv.buffer }, key, rawData.buffer)];
                case 2:
                    encrypted = _a.sent();
                    return [2 /*return*/, new Uint8Array(encrypted, 0, encrypted.byteLength)];
            }
        });
    });
}
export function aesDecrypt(iv, aesKey, encryptedData) {
    return __awaiter(this, void 0, void 0, function () {
        var key, decrypted;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0: return [4 /*yield*/, window.crypto.subtle.importKey("raw", aesKey.buffer, "AES-GCM", false, ["encrypt", "decrypt"])];
                case 1:
                    key = _a.sent();
                    return [4 /*yield*/, window.crypto.subtle.decrypt({ name: "AES-GCM", iv: iv.buffer }, key, encryptedData)];
                case 2:
                    decrypted = _a.sent();
                    return [2 /*return*/, new Uint8Array(decrypted, 0, decrypted.byteLength)];
            }
        });
    });
}
export function rsaEncrypt(publicKey, rawData) {
    return __awaiter(this, void 0, void 0, function () {
        var key, encrypted;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0: return [4 /*yield*/, window.crypto.subtle.importKey("spki", publicKey.buffer, { name: "RSA-OAEP", hash: "SHA-256" }, false, ["encrypt"])];
                case 1:
                    key = _a.sent();
                    return [4 /*yield*/, window.crypto.subtle.encrypt({ name: "RSA-OAEP" }, key, rawData.buffer)];
                case 2:
                    encrypted = _a.sent();
                    return [2 /*return*/, new Uint8Array(encrypted, 0, encrypted.byteLength)];
            }
        });
    });
}
//# sourceMappingURL=crypto.js.map