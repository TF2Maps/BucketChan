using System;

namespace BucketChan
{
    public class ChatResponder
    {
        public Action<string> PublicResponder { get; set; }
        public Action<string> PrivateResponder { get; set; }

        public void SendPublic(string text)
        {
            PublicResponder(text);
        }

        public void SendPrivate(string text)
        {
            PrivateResponder(text);
        }
    }
}