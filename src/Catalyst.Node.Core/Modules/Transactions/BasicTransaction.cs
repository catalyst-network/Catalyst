using Catalyst.Node.Core.Helpers.Math;

namespace Catalyst.Node.Core.Modules.Transactions
{
    public class BasicTransaction : ITransaction
    {
        // Constructor taking all the values from a message
        public BasicTransaction(UInt160 fromAddress,
            UInt160 toAddress,
            byte[] inputAction,
            Fixed8 amount,
            Fixed8 fee,
            byte[] unlockScript,
            Fixed8 outputAmount,
            byte[] unlockingProgram)
        {
            InputAddress = fromAddress;
            OutputAddress = toAddress;
            InputAction = inputAction;
            UnlockScript = unlockScript;
            OutputAmount = outputAmount;
            UnlockingProgram = unlockingProgram;
        }

        public bool BSending { get; set; }
        private UInt160 InputAddress { get; }
        private byte[] InputAction { get; }
        private byte[] UnlockScript { get; }
        private UInt160 OutputAddress { get; }
        private Fixed8 OutputAmount { get; }
        private byte[] UnlockingProgram { get; }
    }
}