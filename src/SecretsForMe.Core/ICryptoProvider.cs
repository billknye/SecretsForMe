namespace SecretsForMe.Core;

public interface ICryptoProvider
{
    Task Initialize();

    Task<byte[]> DeriveBytes(string password, int iterations, byte[] salt);
    Task<(byte[] publicKey, byte[] privateKey)> CreateRsaKeyPair(int length);
    Task<byte[]> CreateAesKey(int length);

    Task<byte[]> AesEncrypt(byte[] iv, byte[] aesKey, byte[] rawData);
    Task<byte[]> AesDecrypt(byte[] iv, byte[] aesKey, byte[] encryptedData);

    Task<byte[]> RsaEncrypt(byte[] publicKey, byte[] rawData);
    Task<byte[]> RsaDecrypt(byte[] privateKey, byte[] encryptedData);

}
