### Why do I need to do this?

The node it relies on some SSL functionality, on macOS and Windows the node can handle creating SSL certs on the fly, but on linux (go figure) it doesn't work and you need to DIY. If you want to know why or want to help solve this issue have a look at [this issue](https://github.com/catalyst-network/catalyst.node/issues/2)

### Using Openssl

* Install openssl package for your operating system from [here](https://www.openssl.org/related/binaries.html)
* Generate a private key: 
```
openssl genrsa 2048 > private.pem
```
* Generate the self signed certificate: 
```
openssl req -x509 -new -key private.pem -out public.pem
```
* Create the PFX file: 
```
openssl pkcs12 -export -in public.pem -inkey private.pem -out mycert.pfx
```