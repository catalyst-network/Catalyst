pragma solidity ^0.4.24;

contract DfsFileRegistration {

    mapping(string => string[]) private pid_cids;

    function register_files(string pid, string cid) external payable {
        pid_cids[cid].push(pid);      
    }
    
    function deregister_files(string pid, string cids) external payable {
            pid_cids[cids].push(pid);      
        }
    
}
