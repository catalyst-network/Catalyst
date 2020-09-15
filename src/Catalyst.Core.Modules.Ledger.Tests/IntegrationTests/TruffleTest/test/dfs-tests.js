const DfsContract = artifacts.require("DfsFileRegistration");

const testAddress = '0x32Be343B94f860124dC4fEe278FDCBD38C102D88';

const filesA = ['QmYjtig7VJQ6XsnUjqqJvj7QaMcCAwtrgNdahSiFofrE7o', 'QmUjNmr8TgJCn1Ao7DvMy4cjoZU15b9bwSCBLE3vwXiwgj'];
const filesB = ['QmcgpsyWgH8Y8ajJz1Cu72KnS5uo2Aa2LpzU7kinSoooo4', 'zdj7WWeQ43G6JJvLWQWZpyHuAMq6uYWRjkBXFad11vE2LHhQ7'];
const filesC = ['QmYjtig7VJQ6XsnUjqqJvj7QaMcCAwtrgNdahSiFofrE7o', 'QmcgpsyWgH8Y8ajJz1Cu72KnS5uo2Aa2LpzU7kinSoooo4', 'not a duplicate']
const filesAB = [...filesA, ...filesB];
const uniqueFilesABC = [...new Set([...filesA, ...filesB,...filesC])];
contract("DfsFileRegistration", function() {
    let catchRevert = require("./exceptions.js").catchRevert;
    let dfs;
    const deploy = async function() {
        dfs = await DfsContract.new();
    };

    describe("register", () => {
        before(deploy);
        it("can register files to new pid", async () => {
            await dfs.registerFilesToExistingOrNewUser(testAddress,filesA);
            const actual = await dfs.listFiles(testAddress);
            assert.deepEqual(filesA, actual, "equal?")

        });

        it("can register more files to existing pid", async () => {
            await dfs.registerFilesToExistingOrNewUser(testAddress,filesB);
            const actual = await dfs.listFiles(testAddress);
            assert.deepEqual(filesAB, actual, "equal?")
        });

        it("can register files without duplicates", async () => {
            await dfs.registerFilesToExistingOrNewUser(testAddress,filesC);
            const actual = await dfs.listFiles(testAddress);
            assert.deepEqual(uniqueFilesABC, actual, "equal?")
        });
    });

    describe("removeUser(address)", () => {
        before(deploy);
        it("can remove existing user'", async () => {
            await dfs.registerFilesToExistingOrNewUser(testAddress,filesA);
            await dfs.removeUser(testAddress);
            assert.equal(await dfs.userExists(testAddress),false)

        });

        it("throws error on remove user that doesn't exist", async () => {
            await catchRevert(dfs.removeUser(testAddress));
        });

        it("files don't persist if user removed then re-added", async () => {
            await dfs.registerFilesToExistingOrNewUser(testAddress,filesA);
            await dfs.removeUser(testAddress);
            await dfs.registerFilesToExistingOrNewUser(testAddress,[]);
            const actual = await dfs.listFiles(testAddress);
            assert.equal(actual.length,0, "length should be 0");
        });
    });

    describe("removeFiles", () => {
        before(deploy);
        it("can remove files", async () => {
            await dfs.registerFilesToExistingOrNewUser(testAddress,filesAB);
            await dfs.removeFiles(testAddress, filesA);
            const actual = await dfs.listFiles(testAddress);

            assert.deepEqual([...filesB].sort(), [...actual].sort(), "equal?")
        });

    });
});
