class LedgerApi {
    constructor(Ipfs, OrbitDB) {

        this.OrbitDB = OrbitDB
        this.node = new Ipfs({
            preload: { enabled: false },
            repo: './ipfs-data',
            EXPERIMENTAL: { pubsub: true },
            config: {
                Bootstrap: [],
                Addresses: { Swarm: [] }
            }
        })

        this.node.on('error', (e) => { throw (e) })
        this.node.on('ready', this._init.bind(this))
    }

    async _init() {
        this.orbitDb = await this.OrbitDB.createInstance(this.node)
        this.onReady()

        this.ledgerOptions = { accessController: { 
            //write: [this.orbitdb.identity.id] } 
            write: ['*'] } 
        }

        //await this.orbitDB.create('ledger', )
        this.ledger = await this.orbitDb.keyvalue('ledger', this.ledgerOptions)
        this.onLedgerCreated();

        // const owner = 'matthieu'
        // await this.ledger.put(owner, '10000');
        // const balance = await this.ledger.get(owner);
        // this.onBalanceRetrieved(owner, balance);
    }
}

const Ipfs = require('ipfs')
const OrbitDB = require('orbit-db')

module.exports = exports = new LedgerApi(Ipfs, OrbitDB)

// try {
//     const Ipfs = require('ipfs')
//     const OrbitDB = require('orbit-db')

//     module.exports = exports = new Ledger(Ipfs, OrbitDB)
// } catch (e) {
//     window.Ledger = new Ledger(window.Ipfs, window.OrbitDB)
// }
