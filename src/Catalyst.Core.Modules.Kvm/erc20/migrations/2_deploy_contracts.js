var EIP20 = artifacts.require("EIP20");

module.exports = function(deployer) {
  deployer.deploy(EIP20,1000000,"KAT",8,"KAT");
};