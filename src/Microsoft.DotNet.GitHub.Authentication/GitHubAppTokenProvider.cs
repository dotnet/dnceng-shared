// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.DotNet.GitHub.Authentication;

public class GitHubAppTokenProvider : IGitHubAppTokenProvider
{
    private readonly ISystemClock _clock;
    private readonly IOptionsMonitor<GitHubTokenProviderOptions> _options;

    public GitHubAppTokenProvider(ISystemClock clock, IOptionsMonitor<GitHubTokenProviderOptions> options = null)
    {
        _clock = clock;
        _options = options;
    }

    public string GetAppToken()
    {
        var options = _options.CurrentValue;
        return GetAppToken(options.GitHubAppId, options.PrivateKey);
    }
    /// <summary>
    /// Get an app token using the <see cref="GitHubTokenProviderOptions"/> corresponding to the specified
    /// <see href="https://docs.microsoft.com/en-us/dotnet/core/extensions/options#named-options-support-using-iconfigurenamedoptions">named option</see>.
    /// </summary>
    public string GetAppToken(string name)
    {
        var options = _options.Get(name);
        return GetAppToken(options.GitHubAppId, options.PrivateKey);
    }

    private string GetAppToken(int gitHubAppId, string privateKey)
    {
        var handler = new JwtSecurityTokenHandler
        {
            SetDefaultTimesOnTokenCreation = false
        };
        
        // Create a new RSA provider with the parameters from the PEM key
        using (var rsa = CreateRsaProviderFromPrivateKey(privateKey))
        {
            var rsaSecurityKey = new RsaSecurityKey(rsa)
            {
                CryptoProviderFactory =
                {
                    // Since we control the lifetime of the key, they can't cache it, since we are about to dispose it
                    CacheSignatureProviders = false
                }
            };
            var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);
            var dsc = new SecurityTokenDescriptor
            {
                IssuedAt = _clock.UtcNow.AddMinutes(-1).UtcDateTime,
                Expires = _clock.UtcNow.AddMinutes(9).UtcDateTime,
                Issuer = gitHubAppId.ToString(),
                SigningCredentials = signingCredentials
            };
            SecurityToken token = handler.CreateToken(dsc);
            return handler.WriteToken(token);
        }
    }

    /// <summary>
    /// Creates an RSA provider from PEM-encoded private key string
    /// </summary>
    /// <param name="privateKey">PEM-encoded private key</param>
    /// <returns>RSACryptoServiceProvider with the private key</returns>
    private static RSA CreateRsaProviderFromPrivateKey(string privateKey)
    {
        // Remove header/footer and decode
        var privateKeyBlob = Convert.FromBase64String(ExportBase64FromPem(privateKey));
        
        // Use RSACryptoServiceProvider and manually set parameters
        using (var memoryStream = new MemoryStream(privateKeyBlob))
        {
            using (var binaryReader = new BinaryReader(memoryStream))
            {
                byte byteValue;
                ushort shortValue;

                // Skip PKCS#1 header
                byteValue = binaryReader.ReadByte();
                if (byteValue != 0x30)
                    throw new Exception("PKCS#1 header not found");

                // Skip length
                shortValue = binaryReader.ReadUInt16();
                
                // Skip version
                byteValue = binaryReader.ReadByte();
                if (byteValue != 0x02)
                    throw new Exception("Version number not found");
                
                shortValue = binaryReader.ReadByte();
                binaryReader.ReadBytes(shortValue);

                // Parse RSA parameters
                RSAParameters rsaParams = new RSAParameters();

                // Read modulus
                byteValue = binaryReader.ReadByte();
                if (byteValue != 0x02)
                    throw new Exception("Modulus not found");
                
                // Read length
                rsaParams.Modulus = GetIntegerParameter(binaryReader);

                // Read public exponent
                byteValue = binaryReader.ReadByte();
                if (byteValue != 0x02)
                    throw new Exception("Public exponent not found");
                
                rsaParams.Exponent = GetIntegerParameter(binaryReader);

                // Read private exponent
                byteValue = binaryReader.ReadByte();
                if (byteValue != 0x02)
                    throw new Exception("Private exponent not found");
                
                rsaParams.D = GetIntegerParameter(binaryReader);

                // Read prime1
                byteValue = binaryReader.ReadByte();
                if (byteValue != 0x02)
                    throw new Exception("Prime1 not found");
                
                rsaParams.P = GetIntegerParameter(binaryReader);

                // Read prime2
                byteValue = binaryReader.ReadByte();
                if (byteValue != 0x02)
                    throw new Exception("Prime2 not found");
                
                rsaParams.Q = GetIntegerParameter(binaryReader);

                // Read exponent1
                byteValue = binaryReader.ReadByte();
                if (byteValue != 0x02)
                    throw new Exception("Exponent1 not found");
                
                rsaParams.DP = GetIntegerParameter(binaryReader);

                // Read exponent2
                byteValue = binaryReader.ReadByte();
                if (byteValue != 0x02)
                    throw new Exception("Exponent2 not found");
                
                rsaParams.DQ = GetIntegerParameter(binaryReader);

                // Read coefficient
                byteValue = binaryReader.ReadByte();
                if (byteValue != 0x02)
                    throw new Exception("Coefficient not found");
                
                rsaParams.InverseQ = GetIntegerParameter(binaryReader);

                // Create RSA provider and import parameters
                var rsa = RSA.Create();
                rsa.ImportParameters(rsaParams);
                return rsa;
            }
        }
    }

    private static byte[] GetIntegerParameter(BinaryReader reader)
    {
        int length = 0;
        int byteValue = reader.ReadByte();
        
        // Handle multi-byte length
        if (byteValue == 0x81)
        {
            length = reader.ReadByte();
        }
        else if (byteValue == 0x82)
        {
            byte[] lengthBytes = reader.ReadBytes(2);
            length = (lengthBytes[0] << 8) | lengthBytes[1];
        }
        else
        {
            length = byteValue;
        }

        byte[] value = reader.ReadBytes(length);
        
        // If the first byte is 0, it's just padding, so remove it
        if (value.Length > 0 && value[0] == 0)
        {
            return value.Skip(1).ToArray();
        }
        
        return value;
    }

    private static string ExportBase64FromPem(string pem)
    {
        // PEM format starts with -----BEGIN RSA PRIVATE KEY----- and ends with -----END RSA PRIVATE KEY-----
        const string beginMarker = "-----BEGIN RSA PRIVATE KEY-----";
        const string endMarker = "-----END RSA PRIVATE KEY-----";
        
        int startIndex = pem.IndexOf(beginMarker);
        if (startIndex < 0)
        {
            throw new ArgumentException("Invalid PEM format: missing begin marker", nameof(pem));
        }
        
        int endIndex = pem.IndexOf(endMarker, startIndex + beginMarker.Length);
        if (endIndex < 0)
        {
            throw new ArgumentException("Invalid PEM format: missing end marker", nameof(pem));
        }
        
        // Extract the Base64 encoded part
        string base64 = pem
            .Substring(startIndex + beginMarker.Length, endIndex - startIndex - beginMarker.Length)
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();
            
        return base64;
    }
}
