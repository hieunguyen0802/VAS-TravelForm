using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace src.Web.Extensions
{
    public class EncryptionService
    {
        private readonly KeyInfo _keyInfo;

        public EncryptionService(KeyInfo keyInfo = null)
        {
            _keyInfo = keyInfo;
        }

        public string Encrypt(string input)
        {
            var enc = DataProtection.EncryptStringToBytes_Aes(input, _keyInfo.Key, _keyInfo.Iv);
            return Convert.ToBase64String(enc);
        }

        public string Decrypt(string cipherText)
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            return DataProtection.DecryptStringFromBytes_Aes(cipherBytes, _keyInfo.Key, _keyInfo.Iv);
        }
    }
}
