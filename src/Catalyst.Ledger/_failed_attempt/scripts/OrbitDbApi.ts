import * as IPFS from 'ipfs';
import OrbitDb from 'orbit-db';
import { KeyValueStore } from 'orbit-db-kvstore';
import { Store } from 'orbit-db-store';

const defaultIpfsOptions = {
    repo: './ipfs',
    EXPERIMENTAL: { pubsub: true },
    preload: { "enabled": false },
    // config: {
    //     Addresses: {
    //         Swarm: ["/dns4/ws-star.discovery.libp2p.io/tcp/443/wss/p2p-websocket-star"]
    //     },
    //     Bootstrap: ["/ip4/10.220.3.64/tcp/4002/ws/ipfs/QmTLJ3rHiqtcitBRhPv8enSHmhZahCF7heYQvKkWvBfGVq"] //connect workshop peers
    // }
    config: { Bootstrap: [], Addresses: { Swarm: [] }}
}

export default class OrbitDbApi {

    static confirmedDeltas: string = 'confirmed.deltas';
    static ledger: string = 'ledger';

    ipfs: IPFS
    orbitDb: OrbitDb
    orbitDbInitialised: Boolean

    keyValueStore?: Store

    constructor(ipfsOptions :IPFS.Options = defaultIpfsOptions) {

        this.orbitDbInitialised = false;
        this.ipfs = IPFS.createNode(ipfsOptions)

        this.ipfs.on('error', (e) => { throw (e) })
        this.ipfs.on('ready', this.InitOrbitDb.bind(this))

        this.orbitDb = OrbitDb.prototype;
    }

    public async InitOrbitDb() {
        if(!this.orbitDbInitialised){
            this.orbitDb = await OrbitDb.createInstance(this.ipfs);
            console.info('orbitDb ready');
            this.orbitDbInitialised = true;
        }
    }

    public createLogDb = async () => {
        await this.orbitDb.create(OrbitDbApi.confirmedDeltas,
            'eventlog',
            {
                write: ["*"]
            } as ICreateOptions);
    };
    
    public addLog = async (content: string) => {
        const deltaUpdatesDb = await this.orbitDb.log(OrbitDbApi.confirmedDeltas);
        await deltaUpdatesDb.load();
        const hash = await deltaUpdatesDb.add(JSON.parse(content));
        return hash;
    };
    
    public getLog = async (hash: string) => {
        const deltaUpdatesDb = await this.orbitDb.log(OrbitDbApi.confirmedDeltas);
        await deltaUpdatesDb.load();
        const content = await deltaUpdatesDb.get(hash);
        return content;
    }

    public createKeyValueStore = async () => {
        this.keyValueStore = await this.orbitDb.create(OrbitDbApi.ledger,
            'keyvalue',
            {
                write: ["*"]
            } as ICreateOptions);
        return this.keyValueStore;
    }

    public async addKeyValue(key: string, value: any){
        const keyValueStore = (this.keyValueStore || await this.createKeyValueStore()) as KeyValueStore<any>;
        await keyValueStore.set(key, value);
    }

    
    public async getKeyValue(key: string){
        const keyValueStore = (this.keyValueStore || await this.createKeyValueStore()) as KeyValueStore<any>;
        const value = await keyValueStore.get(key);
        return value;
    }
}
