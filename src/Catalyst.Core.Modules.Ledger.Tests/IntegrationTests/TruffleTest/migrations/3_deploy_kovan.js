var Kovan = artifacts.require("kovan/OwnedSet");

module.exports = function(deployer) {
    deployer.deploy(Kovan, ["0x5fe351dd36e699b1c87b48199a1739d4939fdcbe"]);
};
