### (v0.8.2.0) - TBD

> TODO:
> 1. Test Slack base class since it's now based on System.Text.Json
> 1. Review API Sample with Assembly invoke

#### BREAKING CHANGES

* CLI
  * Added constraint that custom Lambda serializers must derive from `LambdaSharp.Serialization.ILambdaJsonSerializer` or the CLI will emit an error during compilation.

* SDK
  * .NET Core 3.1 assembly
  * Renamed `LambdaJsonSerializer` to `LambdaNewtonsoftJsonSerializer` to make it clear this JSON serializer is based on `Newtonsoft.Json`.
  * Changed `ALambdaFunction.LambdaSerializer` property type to `ILambdaJsonSerializer`.
  * Changed target framework for `LambdaSharp` project to `netcoreapp3.1` which is required by the `Amazon.Lambda.Serialization.SystemTextJson` assembly.
  * Changed target framework for `LambdaSharp.Slack` project to `netcoreapp3.1` which is required by the `LambdaSharp` assembly.

#### Features

* CLI
  * Lambda projects can now explicitly specify one of the standard LambdaSharp serializers without causing an error or warning during compilation. The standard serializers are `LambdaSharp.Serialization.LambdaNewtonsoftJsonSerializer` and `LambdaSharp.Serialization.LambdaNewtonsoftJsonSerializer`.
  * Lambda project can now explicitly specify a custom serializer. The custom serializer must derive from `ILambdaJsonSerializer`.
  * Updated CloudFormation to v22.0.0
  * Update Blazor WebAssembly app template to target .NET 5

* SDK
  * Added new `ALambdaEventFunction<TMessage>` base class for handling CloudWatch events.
  * Optimized cold-start times for by deserializing Amazon Lambda data-structures by using `LambdaSystemTextJsonSerializer`, which is built on _System.Text.Json_. This change avoids jitting the heavy _Newtonsoft.Json_ assembly unless required by the end-user code.
  * Added constructor to customize serializer settings for `LambdaSystemTextJsonSerializer`.
  * Added constructor to customize serializer settings for `LambdaNewtonsoftJsonSerializer`.
  * Added null-aware annotations to _LambdaSharp_ assembly.
  * Added [MD5 algorithm implementation from Mono](https://github.com/mono/mono/blob/master/mcs/class/corlib/System.Security.Cryptography/MD5CryptoServiceProvider.cs) in _LambdaSharp.Logging_ since it is not supported in .NET 5 WebAssembly currently.

* Samples
  * Added `Samples/JsonSerializerSample` module showing how to specify and customize the JSON serializer for a Lambda function.
