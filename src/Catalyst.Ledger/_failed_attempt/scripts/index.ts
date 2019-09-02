import OrbitDbApi from './OrbitDbApi'

var api = new OrbitDbApi();

api.InitOrbitDb().then( () => {
    api.createKeyValueStore().then
})
