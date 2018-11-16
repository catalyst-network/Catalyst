using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using ADL.Helpers.Math;

namespace ADL.Transaction
{
    public class BasicTransaction : ITx
    {
        public bool bSending { get; set; }
        
        public byte[] ParentBlock { get; set; }

        public UInt160 InputAddress { get; set; }

        public byte[] InputAction { get; set; }

        public byte[] UnlockScript { get; set; }

        public UInt160 OutputAddress { get; set; }

        public Fixed8 OutputAmount { get; set; }

        public byte[] UnlockingProgram { get; set; }

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
            this.InputAddress = fromAddress;
            this.OutputAddress = toAddress;
            this.InputAction = inputAction;
            this.UnlockScript = unlockScript;
            this.OutputAmount = outputAmount;
            this.UnlockingProgram = unlockingProgram;
        }

        // Constructor taking minimal data and filling in the gaps
        public BasicTransaction( UInt160 fromAddress,
                            UInt160 toAddress,
                            Fixed8 amount,
                            Fixed8 fee)
        {
            this.InputAddress = fromAddress;
            this.OutputAddress = toAddress;
        }
    }
}
