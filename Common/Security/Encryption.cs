using System.Text;
using System.Security.Cryptography;
using System.IO;
using Common.Structures;

namespace Common.Security
{
    public static class Encryption
    {
        public static KeyIV GenerateKeyIV()
        {
            using (RijndaelManaged rM = new RijndaelManaged())
            {
                rM.GenerateKey();
                rM.GenerateIV();
                return new KeyIV { Key = rM.Key, IV = rM.IV };
            }
        }

        public static byte[] EncryptString(string input, byte[] key, byte[] iv)
        {
            if (input.Length > 0  && key != null && iv != null)
            {
                using (RijndaelManaged rm = new RijndaelManaged() { Key = key, IV = iv })
                {
                    ICryptoTransform encryptor = rm.CreateEncryptor(rm.Key, rm.IV);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(cs)) { sw.Write(input);  }
                        }

                        return ms.ToArray();
                    }
                } 
            }

            return null;
        }

        public static string DecryptString(byte[] input, byte[] key, byte[] iv)
        {
            if (input != null && key != null && iv != null)
            {
                using (RijndaelManaged rm = new RijndaelManaged() { Key = key, IV = iv })
                {
                    ICryptoTransform decryptor = rm.CreateDecryptor(rm.Key, rm.IV);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs)) { return sr.ReadToEnd(); }
                        }
                    }
                }
            }

            return null;
        }

        public static byte[] EncryptBytes(ref byte[] input, byte[] key, byte[] iv)
        {
            if (input.Length > 0 && key != null && iv != null)
            {
                using (RijndaelManaged rm = new RijndaelManaged() { Key = key, IV = iv })
                {
                    rm.Padding = PaddingMode.PKCS7;

                    ICryptoTransform encryptor = rm.CreateEncryptor(rm.Key, rm.IV);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) { cs.Write(input, 0, input.Length); cs.Flush(); }

                        return ms.ToArray();
                    }
                }
            }

            return null;
        }

        public static byte[] DecryptBytes(ref byte[] input, byte[] key, byte[] iv)
        {
            if (input != null && key != null && iv != null)
            {
                using (RijndaelManaged rm = new RijndaelManaged() { Key = key, IV = iv })
                {
                    ICryptoTransform decryptor = rm.CreateDecryptor(rm.Key, rm.IV);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write)) { cs.Write(input, 0, input.Length); }

                        return ms.ToArray();
                    }
                }
            }

            return null;
        }

        public static byte[] DecryptBytes(byte[] input, byte[] key, byte[] iv)
        {
            if (input != null && key != null && iv != null)
            {
                using (RijndaelManaged rm = new RijndaelManaged() { Key = key, IV = iv })
                {
                    ICryptoTransform decryptor = rm.CreateDecryptor(rm.Key, rm.IV);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write)) { cs.Write(input, 0, input.Length); }

                        return ms.ToArray();
                    }
                }
            }

            return null;
        }
    }
}
