var Kovan = artifacts.require("kovan/OwnedSet");

module.exports = function(deployer) {
    deployer.deploy(Kovan, ["0xb77aec9f59f9d6f39793289a09aea871932619ed"]);
};
