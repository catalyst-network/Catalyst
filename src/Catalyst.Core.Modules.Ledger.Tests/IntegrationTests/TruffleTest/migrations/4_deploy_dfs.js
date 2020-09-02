const DfsContract = artifacts.require("DfsFileRegistration");

module.exports = function(deployer) {
    deployer.deploy(DfsContract);
}
