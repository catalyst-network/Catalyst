using Catalyst.Node.Core.Helpers.Math;

namespace Catalyst.Node.Core.Modules.Transactions
{
    public class BasicTransaction : ITransaction
    {
        private readonly UInt160 _inputAddress;
        private readonly byte[] _inputAction;
        private readonly Fixed8 _amount;
        private readonly Fixed8 _fee;
        private readonly byte[] _unlockScript;
        private readonly UInt160 _outputAddress;
        private readonly Fixed8 _outputAmount;
        private readonly byte[] _unlockingProgram;
        
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
            _inputAddress = fromAddress;
            _outputAddress = toAddress;
            _inputAction = inputAction;
            _amount = amount;
            _fee = fee;
            _unlockScript = unlockScript;
            _outputAmount = outputAmount;
            _unlockingProgram = unlockingProgram;
        }

        public bool BSending { get; set; }
    }
}