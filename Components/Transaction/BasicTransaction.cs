using ADL.Helpers.Math;

namespace ADL.Transaction
{
    public class BasicTransaction : ITransaction
    {
        public bool BSending { get; set; }
        
        private UInt160 InputAddress { get; set; }

        private byte[] InputAction { get; set; }

        private byte[] UnlockScript { get; set; }

        private UInt160 OutputAddress { get; set; }

        private Fixed8 OutputAmount { get; set; }

        private byte[] UnlockingProgram { get; set; }

        // Constructor taking all the values from a message
        public BasicTransaction( UInt160 fromAddress,
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
    }
}
