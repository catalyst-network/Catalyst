namespace ADL.Transaction
{
    public class P2pkh
    {
        private const int Locking_program_hash = 1;


        public bool CheckInput(BasicTransaction tx)
        {
            /*
            tx.UnlockingProgram
            keys::pub_t pk;
            string signature_der_b58;

            if (pk.hash() != addreess)
            {
                get_hash(this_index, sigcodes);
                create_input(h,sigcodes,pk);
            }
            */

            return true;
        }
        
        public void CreateInput(ref BasicTransaction tx)
        {
    
        }
    }
}