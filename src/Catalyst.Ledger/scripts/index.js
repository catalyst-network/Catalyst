try {
    const ledgerApi = require('./ledger');
    ledgerApi.onReady = () => console.log('orbit-db running with id:', ledgerApi.orbitDb.id)
    ledgerApi.onLedgerCreated = () => console.log('ledger keyvalue store created with id:', ledgerApi.ledger.id)
    ledgerApi.onBalanceRetrieved = 
        (owner, balance) => console.log(`balance of owner ${owner} is: ${balance}`)
} catch(e) {
    console.error(e);
}
