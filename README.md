# MsBuild.ProtocolBuffers

MSBuild target for automatic compiling of .proto files into .cs files.

## Compatibility

Compatible with MSBuild 15.0 (Visual Studio 2017, .NET SDK projects, dotnet build)

Compatibility with earlier versions (and thus xbuild also) is conceivable but not implemented.  Pull requests to add this are welcomed.

## Usage

```
Install-Package MsBuild.ProtocolBuffers
```

Then just add .proto files to the project and .proto.cs files will be generated and included alongside them.