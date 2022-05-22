# Example F# WASI App


This is an example app to demonstrate the use of F# in WASI using the new https://github.com/SteveSandersonMS/dotnet-wasi-sdk

Here's the introductory video from CNCF Europe WASM Conference [Bringing WebAssembly to the .NET Mainstream - Steve Sanderson, Microsoft](https://www.youtube.com/watch?v=PIeYw7kJUIg)



## Running

Ensure you have at least .NET 7.0 preview 4 - you can download it here https://dotnet.microsoft.com/en-us/download/dotnet/7.0
Also, please install `wasmer` - https://docs.wasmer.io/ecosystem/wasmer/getting-started
Command to install it from their docs is `curl https://get.wasmer.io -sSfL | sh` - be sure to check the script first (always check what you `sh` from the internet)


Then:

```bash
dotnet build
wasmer bin/Debug/net7.0/ExampleFsharpWasiApp.wasm
> Hello from F#
```




#### License

MIT