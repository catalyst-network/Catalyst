import {OrbitDbApi} from './index';

describe('api functions', () => {
    it('can create a database', () => {
        
        const result = new OrbitDbApi();
        expect(result).not.toBeFalsy(); 
    })
})
