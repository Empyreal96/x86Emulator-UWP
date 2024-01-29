/**
  Copyright (c) Bashar Astifan
  https://github.com/basharast
*/

using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WUT
{
    public static class LocalDataManager
    {
        private static Crypto Encryption = new Crypto();

        public static async Task SaveData<T>(string fileName, T objectData, bool encrypt = false)
        {

            var localFolder = ApplicationData.Current.LocalFolder;

            var targetFile = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            Encoding unicode = Encoding.Unicode;
            byte[] dictionaryListBytes = unicode.GetBytes(JsonConvert.SerializeObject(objectData));
            if (encrypt)
            {
                dictionaryListBytes = Encryption.Encrypt(dictionaryListBytes);

            }

            using (var outStream = await targetFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                await outStream.WriteAsync(dictionaryListBytes.AsBuffer());
                await outStream.FlushAsync();
                outStream.Dispose();
            }
        }

        public static async Task<T> GetData<T>(string fileName, bool decrypt = false)
        {
            var localFolder = ApplicationData.Current.LocalFolder;

            var targetFileTest = (StorageFile)await localFolder.TryGetItemAsync(fileName);
            if (targetFileTest != null)
            {
                Encoding unicode = Encoding.Unicode;
                byte[] result;
                using (var outStream = await targetFileTest.OpenAsync(FileAccessMode.Read))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        outStream.AsStreamForRead().CopyTo(memoryStream);
                        result = memoryStream.ToArray();
                    }
                    outStream.Dispose();
                }
                if (decrypt)
                {
                    result = Encryption.Decrypt(result);
                }
                string CoreFileContent = unicode.GetString(result);
                var dictionaryList = JsonConvert.DeserializeObject<T>(CoreFileContent);

                if (dictionaryList != null)
                {
                    return dictionaryList;
                }
            }

            return default(T);
        }

        public static async void DeleteData(string fileName)
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                if (localFolder == null)
                {
                    return;
                }

                var targetFileTest = (StorageFile)await localFolder.TryGetItemAsync(fileName);
                if (targetFileTest != null)
                {
                    await targetFileTest.DeleteAsync();
                }
            }
            catch (Exception e)
            {

            }
        }
    }
    public class Crypto
    {
        // Key with 256 and IV with 16 length
        private string AES_Key = "U5IFHRcyJhxabQZKojtQX4u3nxTVqnzFIPev1ANm7mU=";
        private string AES_IV = "15CV1/MOnVI3rY4wk4INBg==";
        private IBuffer m_iv = null;
        private CryptographicKey m_key;

        public Crypto(string csAES_Key = "")
        {
            IBuffer key = Convert.FromBase64String(AES_Key).AsBuffer();
            if (csAES_Key.Length > 0)
            {
                key = Convert.FromBase64String(csAES_Key).AsBuffer();
            }
            m_iv = Convert.FromBase64String(AES_IV).AsBuffer();
            SymmetricKeyAlgorithmProvider provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
            m_key = provider.CreateSymmetricKey(key);
        }

        public byte[] Encrypt(byte[] input)
        {
            IBuffer bufferMsg = CryptographicBuffer.ConvertStringToBinary(Encoding.ASCII.GetString(input), BinaryStringEncoding.Utf8);
            IBuffer bufferEncrypt = CryptographicEngine.Encrypt(m_key, bufferMsg, m_iv);
            return bufferEncrypt.ToArray();
        }

        public byte[] Decrypt(byte[] input)
        {
            IBuffer bufferDecrypt = CryptographicEngine.Decrypt(m_key, input.AsBuffer(), m_iv);
            return bufferDecrypt.ToArray();
        }

        public byte[] EncryptStream(byte[] input)
        {
            IBuffer bufferMsg = CryptographicBuffer.CreateFromByteArray(input);
            IBuffer bufferEncrypt = CryptographicEngine.Encrypt(m_key, bufferMsg, m_iv);
            return bufferEncrypt.ToArray();
        }

        public byte[] DecryptStream(byte[] input)
        {
            IBuffer bufferDecrypt = CryptographicEngine.Decrypt(m_key, input.AsBuffer(), m_iv);
            return bufferDecrypt.ToArray();
        }
    }
}
