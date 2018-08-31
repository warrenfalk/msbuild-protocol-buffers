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

## Options

By default, all ```.proto``` files in the project will be included in the code generation.  You can customize this, however with the ```ProtoC``` element in your msbuild.

```xml
<Target Name="ResetProtoC" BeforeTargets="ProtoCalcOutput">
  <ItemGroup>
    <ProtoC Remove="**\*.proto"/>
    <ProtoC Include="proto\**\*.proto"/>
  </ItemGroup>
</Target>
```

By default the output ```.proto.cs``` files are placed alongside the ```.proto``` files in the same directory.
You can override this by specifying what the base of your include is, and the base of your output:

```xml
<Target Name="ResetProtoC" BeforeTargets="ProtoCalcOutput">
  <ItemGroup>
    <ProtoC Remove="**\*.proto"/>
    <ProtoC Include="proto-input\**\*.proto" InputBase="proto-input" OutputBase="proto-output"/>
  </ItemGroup>
</Target>
```

You can also modify the default include path of ```.``` with the ```<ProtoIncludes>``` element:

```xml
<PropertyGroup>
  <ProtoIncludes>.;..\..\my\other\protos</ProtoIncludes>
</PropertyGroup>
```

