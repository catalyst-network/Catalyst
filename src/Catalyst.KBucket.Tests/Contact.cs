using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Catalyst.KBucket
{
    public class Contact : IContact
    {
        public Contact(string id)
        {
            var key = Encoding.UTF8.GetBytes(id);
            using (var hasher = SHA1.Create())
            {
                Id = hasher.ComputeHash(key);
            }
        }

        public Contact(int id)
            : this(id.ToString(CultureInfo.InvariantCulture)) { }

        public Contact(params byte[] bytes) { Id = bytes; }

        public byte[] Id { get; set; }

        public int Clock { get; set; }
    }
}
