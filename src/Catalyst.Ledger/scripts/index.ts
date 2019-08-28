import * as IPFS from 'ipfs';
import OrbitDb from 'orbit-db';

export class OrbitDbApi {

    static confirmedDeltas: string = 'confirmed.deltas';
    static ledger: string = 'ledger';

    ipfs: IPFS
    orbitDb: OrbitDb
    
    /**
     * Initialises connection to Ipfs and provides access to OrbitDb
     */
    constructor() {

        const ipfsOptions = {
            repo: './ipfs-repo',
            EXPERIMENTAL: { pubsub: true },
            preload: { "enabled": false },
            config: {
              Addresses: {
                Swarm: ["/dns4/ws-star.discovery.libp2p.io/tcp/443/wss/p2p-websocket-star"]
              },
              Bootstrap: ["/ip4/10.220.3.64/tcp/4002/ws/ipfs/QmTLJ3rHiqtcitBRhPv8enSHmhZahCF7heYQvKkWvBfGVq"] //connect workshop peers
            }
          }

        this.ipfs = IPFS.createNode(ipfsOptions);
        this.ipfs.on('ready', async () => {
            console.info('ipfs ready');
            this.orbitDb = new OrbitDb(this.ipfs);
        })
        this.orbitDb = OrbitDb.prototype;
    }

    public createLogDb = async () => {
        await this.orbitDb.create(OrbitDbApi.confirmedDeltas,
            'eventlog',
            {
                write: ["*"]
            }) as ICreateOptions;
    };
    
    public addLog = async (content: string) => {
        const deltaDb = await this.orbitDb.log(OrbitDbApi.confirmedDeltas);
        await deltaDb.load();
        const hash = await deltaDb.add(JSON.parse(content));
        return hash;
    };
    
    public getLog = async (hash: string) => {
        const deltaDb = await this.orbitDb.log(OrbitDbApi.confirmedDeltas);
        await deltaDb.load();
        const content = await deltaDb.get(hash);
        return content;
    }

    public createKeyValueStore = async () => {
        //this.orbitDb.create()
    }
}
