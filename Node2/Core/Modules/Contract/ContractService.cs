using ADL.Node.Core.Helpers.Services;

namespace ADL.Node.Core.Modules.Contract
{
    public class ContractService : ServiceBase, IContractService
    {
        private IContract Contract;
        private IContractSettings ContractSettings;
        
        /// <summary>
        /// 
        /// </summary>
        public ContractService(IContract contract, IContractSettings contractSettings)
        {
            Contract = contract;
            ContractSettings = contractSettings;
        }
    }
}
