using System;

namespace ADL.Token
{
    public abstract class GenericToken : IEquatable<GenericToken>
    {
        private string _address = null;
        
        public string Address
        {
            get
            {
        /*        if (_address == null)
                {
                    _address = Output.ScriptHash.ToAddress();
                }*/
                return _address;
            }
        }

        public bool Equals(GenericToken other)
        {
            bool bEquals = false;

            if (ReferenceEquals(this, other))
            {
                bEquals = true;
            }

            return bEquals;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as GenericToken);
        }

        public override int GetHashCode()
        {
//            return Reference.GetHashCode();
            return 1;
        }
    }
}
