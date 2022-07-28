using Blazored.LocalStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretsForMe.Core.Configuration;

public class Credential
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public byte[] AesKey { get; set; }
}
