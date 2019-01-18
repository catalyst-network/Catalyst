using System;

namespace Catalyst.Helpers.Util
{
    public static class TransactionUtils
    {
        /// <summary>
        /// </summary>
        /// <param name="rawSignedTransaction"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string CalculateTransactionHash(string rawSignedTransaction)
        {
            if (string.IsNullOrWhiteSpace(rawSignedTransaction))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(rawSignedTransaction));
            if (string.IsNullOrWhiteSpace(rawSignedTransaction))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(rawSignedTransaction));
            var sha3 = new Sha3Keccack();
            return sha3.CalculateHashFromHex(rawSignedTransaction);
        }
    }
}