using System;
using Dawn;

namespace Catalyst.Node.Common.Helpers.Util
{
    public static class StringUtil
    {
        public static bool StringComparatorException(string stringA, string stringB)
        {
            Guard.Argument(stringA)
               .NotNull()
               .NotEmpty()
               .NotWhiteSpace();
            
            Guard.Argument(stringB)
               .NotNull()
               .NotEmpty()
               .NotWhiteSpace();
            
            if (!stringA.Equals(stringB))
            {
                throw new Exception();
            }

            return false;
        }
    }
}