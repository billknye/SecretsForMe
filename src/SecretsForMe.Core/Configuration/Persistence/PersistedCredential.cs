﻿namespace SecretsForMe.Core.Configuration.Persistence;

public record PersistedCredential(Guid Id, string Name, byte[] EncryptedAsymmetricKeyPrivateKey);
