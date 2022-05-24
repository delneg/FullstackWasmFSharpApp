# Example F# WASI App


This is an example app to demonstrate the use of F# in WASI using the new https://github.com/SteveSandersonMS/dotnet-wasi-sdk

Here's the introductory video from CNCF Europe WASM Conference [Bringing WebAssembly to the .NET Mainstream - Steve Sanderson, Microsoft](https://www.youtube.com/watch?v=PIeYw7kJUIg)

Shoutout to https://www.strathweb.com/2022/03/running-net-7-apps-on-wasi-on-arm64-mac/
written by http://twitter.com/filip_woj for tips on running it on macOS

Upd: The issue is fixed in Wasi.Sdk > 0.1.1, https://github.com/dotnet/aspnetcore/pull/41123#issuecomment-1135884829

## Preview

![png](https://github.com/delneg/FullstackWasmFSharpApp/blob/master/assets/screenshot.png?raw=true)


## Running Backend

Ensure you have at least .NET 7.0 preview 3 - you can download it here https://dotnet.microsoft.com/en-us/download/dotnet/7.0

Also, please install `wasmtime` - https://wasmtime.dev/

Command to install it from their website is `curl https://wasmtime.dev/install.sh -sSf | bash` - be sure to check the script first (always check what you `sh` from the internet)


Then:

```bash
cd TodoBackend
dotnet build 
wasmtime bin/Debug/net7.0/FullstackWasmFSharpAppBackend.wasm --tcplisten localhost:8080 --env ASPNETCORE_URLS=http://localhost:8080
> info: Microsoft.Hosting.Lifetime
>       Now listening on: http://localhost:8080
```

## Running frontend

Please note, that currently the backend has hard-coded address of "localhost:5000" for CORS to work.

Also, the frontend has hard-coded address of the backend to "http://localhost:8080" for simplicity.

That said, you should be able to run interactive (with .NET Interpreter in WASM) frontend using:

```bash
cd TodoFrontend
dotnet run
```

You can build the portable (i.e. AOT and optimized) version of the frontend which can be deployed as static website with:

```bash
cd TodoFrontend
dotnet publish -o $(pwd)/publish/ -r Portable
```

After that, you can serve it like any other SPA - for example, locally using [Serve](https://github.com/vercel/serve)


```bash
serve publish/wwwroot/ -p 5000
```


#### License

MIT