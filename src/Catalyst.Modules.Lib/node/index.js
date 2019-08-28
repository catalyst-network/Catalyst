const Ipfs = require('ipfs');
const OrbitDb = require('orbit-db');
// OrbitDB uses Pubsub which is an experimental feature
// and need to be turned on manually.
// Note that these options need to be passed to IPFS in
// all examples even if not specified so.
const ipfsOptions = {
    EXPERIMENTAL: {
        pubsub: true
    }
}

// Create IPFS instance
const ipfs = new Ipfs(ipfsOptions);

ipfs.on('error', (e) => console.error(e));
ipfs.on('ready',
    async () => {
        console.info('Ipfs ready!');
    });

const confirmedDeltas = 'confirmed.deltas';

module.exports = {

    createLogDb: async function() {
        await OrbitDb.create(confirmedDeltas,
            'eventlog',
            {
                accessController: {
                    write: ['*']
                }
            });
    },

    addLog: async function(content) {
        const deltaDb = await OrbitDb.log(confirmedDeltas);
        await deltaDb.load();
        const hash = await deltaDb.add(JSON.parse(content));
        return hash;
    },

    getLog: async function(hash) {
        const deltaDb = await OrbitDb.log(confirmedDeltas);
        await deltaDb.load();
        const content = await deltaDb.get(hash).map(e => e.payload.value);
        return content;
    }
}
