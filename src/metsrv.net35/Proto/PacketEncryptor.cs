﻿using System.Linq;
using System.Security.Cryptography;
using Met.Core.Extensions;
using System.IO;

namespace Met.Core.Proto
{
    public class PacketEncryptor
    {
        public const uint ENC_NONE = 0u;
        public const uint ENC_AES256 = 1u;

        private RNGCryptoServiceProvider random = null;
        private RSACryptoServiceProvider rsa = null;
        private byte[] aesKey;

        public PacketEncryptor()
        {
            this.random = new RNGCryptoServiceProvider();
            this.rsa = new RSACryptoServiceProvider();
        }

        public bool Enabled { get; set; }

        public byte[] AesKey
        {
            get { return this.aesKey; }
            set { this.aesKey = value; this.Enabled = false; }
        }

        public bool HasAesKey
        {
            get { return this.aesKey != null; }
        }

        public uint Flags
        {
            get
            {
                if (this.HasAesKey && this.Enabled)
                {
                    return ENC_AES256;
                }
                return ENC_NONE;
            }
        }

        public byte[] GenerateNewAesKey()
        {
            return this.GenerateRandomBytes(32);
        }

        public byte[] RsaEncrypt(string pubKey, byte[] key)
        {
            this.rsa.LoadPublicKeyPEM(pubKey);
            var encryptedData = this.rsa.Encrypt(key, false);
            return encryptedData;
        }

        public byte[] AesEncrypt(byte[] data)
        {
            var iv = this.GenerateRandomBytes(16);
            using (var aes = new AesManaged { Padding = PaddingMode.ISO10126 })
            using (var encryptor = aes.CreateEncryptor(this.AesKey, iv))
            using (var memStream = new MemoryStream())
            {
                memStream.Write(iv, 0, iv.Length);
                using (var cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                }
                return memStream.ToArray();
            }
        }

        public byte[] AesDecrypt(byte[] data)
        {
            var iv = new byte[16];
            var decrypted = new byte[data.Length - iv.Length];
            using (var memStream = new MemoryStream(data))
            {
                memStream.Read(iv, 0, iv.Length);
                // Got the padding mode fixed as well.
                using (var aes = new AesManaged { Padding = PaddingMode.ISO10126 })
                using (var decryptor = aes.CreateDecryptor(this.AesKey, iv))
                using (var cryptoStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Read))
                {
                    cryptoStream.Read(decrypted, 0, decrypted.Length);
                }
            }
            return decrypted;
        }

        public byte[] Encrypt(byte[] data)
        {
            if (!this.Enabled)
            {
                return data;
            }

            return this.AesEncrypt(data);
        }

        private byte[] GenerateRandomBytes(int size)
        {
            var bytes = new byte[size];
            this.random.GetBytes(bytes);
            return bytes;
        }
    }
}
