import OrbitDbApi from './OrbitDbApi';

describe('api functions', async () => {
    it('can create a database', async (done) => {
        
        const api = new OrbitDbApi();
        expect(true).toBe(false)
        //expect(api).not.toBeFalsy();

        await api.InitOrbitDb();
        
        await api.createKeyValueStore();
        done();
    })
})
