var Kovan = artifacts.require("kovan/OwnedSet");

module.exports = function(deployer) {
    deployer.deploy(Kovan, ["0x1a2149b4df5cbac970bc38fecc5237800c688c8b"]);
};
