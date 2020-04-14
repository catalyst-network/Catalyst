var EIP20 = artifacts.require("EIP20");

contract("EIP20", function(accounts) {
  // it("should assert true", function(done) {
  //   var e_i_p20 = EIP20.deployed();
  //   assert.isTrue(true);
  //   done();
  // });

  it("should return the balance of token owner", function() {
    var token;
    return EIP20.deployed().then(function(instance){
      token = instance;
      return token.balanceOf.call(accounts[0]);
    }).then(function(result){
      Assert.AreEqual(result.toNumber(), 1000000, 'balance is wrong');
    })
  });

  it("should transfer right token", function() {
    var token;
    return EIP20.deployed().then(function(instance){
      token = instance;
      return token.transfer("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 500000);
    }).then(function(){
      return token.balanceOf.call(accounts[0]);
    }).then(function(result){
      Assert.AreEqual(result.toNumber(), 500000, 'accounts[0] balance is wrong');
      return token.balanceOf.call("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    }).then(function(result){
      Assert.AreEqual(result.toNumber(), 500000, 'accounts[1] balance is wrong');
    })
  });
});
