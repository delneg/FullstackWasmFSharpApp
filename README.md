# Example F# WASI App


This is an example app to demonstrate the use of F# in WASI using the new https://github.com/SteveSandersonMS/dotnet-wasi-sdk

Here's the introductory video from CNCF Europe WASM Conference [Bringing WebAssembly to the .NET Mainstream - Steve Sanderson, Microsoft](https://www.youtube.com/watch?v=PIeYw7kJUIg)

Shoutout to https://www.strathweb.com/2022/03/running-net-7-apps-on-wasi-on-arm64-mac/
written by http://twitter.com/filip_woj for tips on running it on macOS

(although I'm still getting `System.PlatformNotSupportedException: System.Net.Quic is not supported on this platform.`)


## Running

Ensure you have at least .NET 7.0 preview 3 - you can download it here https://dotnet.microsoft.com/en-us/download/dotnet/7.0
Also, please install `wasmtime` - https://wasmtime.dev/
Command to install it from their website is `curl https://wasmtime.dev/install.sh -sSf | bash` - be sure to check the script first (always check what you `sh` from the internet)


Then:

```bash
dotnet build
wasmer bin/Debug/net7.0/ExampleFsharpWasiApp.wasm
> Hello from F#
```




#### License

MIT