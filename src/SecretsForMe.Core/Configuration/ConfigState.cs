﻿namespace SecretsForMe.Core.Configuration;

public enum ConfigState
{
    Unknown = 0,
    Loaded = 1,
    ErrorLoading = 2,
    Ready = 3,
    Empty = 4
}
