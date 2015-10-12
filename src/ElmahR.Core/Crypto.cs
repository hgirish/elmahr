namespace ElmahR.Core
{
    public static class Crypto
    {
        public static string DecryptStringAes(string cipherText, string sharedSecret)
        {
            return Elmah.Crypto.DecryptStringAes(cipherText, sharedSecret);
        }
    }
}
