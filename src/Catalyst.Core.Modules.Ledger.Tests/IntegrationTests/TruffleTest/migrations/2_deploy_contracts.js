var EIP20 = artifacts.require("EIP20");

module.exports = function(deployer) {
  deployer.deploy(EIP20, 100, "TestEIP20", 18, "EIP20");
};