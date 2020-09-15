pragma experimental ABIEncoderV2;
pragma solidity ^0.4.24;

//import "truffle/Console.sol";
import "./HitchensUnorderedAddressSet.sol";

contract DfsFileRegistration {

    using HitchensUnorderedAddressSetLib for HitchensUnorderedAddressSetLib.Set;

    HitchensUnorderedAddressSetLib.Set userSet;

    struct UserStruct {
        mapping(bytes32 => CidStruct) cidMap;
        string[] cidList;
    }

    struct CidStruct {
        bool isStored;
        uint index;
    }

     mapping(address => UserStruct) users;

    function registerFilesToExistingOrNewUser(address key, string[] memory cids) public {
        
        if(!userSet.exists(key)){
            userSet.insert(key); 
        }    
        UserStruct storage u = users[key];
        for (uint i=0; i<cids.length; i++){
            bytes32 cidKey = keccak256(abi.encode(cids[i]));
            if(!u.cidMap[cidKey].isStored){
                u.cidMap[cidKey].isStored = true;
                u.cidMap[cidKey].index = u.cidList.length;
                u.cidList.push(cids[i]);
            }  
        }
    }
    
    function removeUser(address key) public {
        userSet.remove(key); // Note that this will fail automatically if the key doesn't exist
        delete users[key];
    }

    function userExists(address key) external view returns(bool) {
        return userSet.exists(key);    
    }
    
    function listFiles(address key) external view returns(string[] memory cids) {
        require(userSet.exists(key), "Can't retrieve a user that doesn't exist.");
        UserStruct storage u = users[key];
        return(u.cidList);
    }

    function removeFiles(address key, string[] memory cids) public {
        require(userSet.exists(key), "Can't retrieve a user that doesn't exist.");
        UserStruct storage u = users[key];
        for (uint i=0; i<cids.length; i++){
            bytes32 cidKey = keccak256(abi.encode(cids[i]));
            if(u.cidMap[cidKey].isStored){
                u.cidMap[cidKey].isStored = false;
                uint index = u.cidMap[cidKey].index;
                u.cidList[index] = u.cidList[u.cidList.length - 1];
		        u.cidMap[keccak256(abi.encode(u.cidList[index]))].index = index;
		        delete u.cidList[u.cidList.length-1];
		        u.cidList.length--;
            }  
        }
    }
}
