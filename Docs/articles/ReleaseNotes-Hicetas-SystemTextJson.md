### (v0.8.2.0) - TBD

#### BREAKING CHANGES

* SDK
  * LambdaSharp base classes for Lambda functions now require the JSON serializer to be provided. The JSON serializer must derive from `LambdaSharp.Serialization.ILambdaJsonSerializer`. The preferred JSON serializer is `LambdaSystemTextJsonSerializer`.
  * Renamed `LambdaJsonSerializer` to `LambdaNewtonsoftJsonSerializer` to make it clear this JSON serializer is based on `Newtonsoft.Json`.
  * Moved `LambdaNewtonsoftJsonSerializer` to its own NuGet package _LambdaSharp.Serialization.NewtonsoftJson_ to remove dependency on the _Newtonsoft.Json_ NuGet package for _LambdaSharp_ assembly.
  * Changed `ALambdaFunction.LambdaSerializer` property type to `ILambdaJsonSerializer`.
  * Changed target framework for `LambdaSharp` project to `netcoreapp3.1` which is required by the `Amazon.Lambda.Serialization.SystemTextJson` assembly.
  * Changed target framework for `LambdaSharp.Slack` project to `netcoreapp3.1` which is required by the `LambdaSharp` assembly.
  * Updated _AWSSDK.Core_ dependencies to version 3.5
  * Changed method signatures in `ALambdaCustomResourceFunction`:
    * Added `System.Threading.CancellationToken` parameter to `ProcessCreateResourceAsync`, `ProcessUpdateResourceAsync`, and `ProcessDeleteResourceAsync`.
  * Changed method signatures in `ALambdaFinalizerFunction`:
    * Renamed `CreateDeployment` to `CreateDeploymentAsync`, `CreateDeployment` to `CreateDeploymentAsync`, and `DeleteDeployment` to `DeleteDeploymentAsync`.
    * Added `System.Threading.CancellationToken` parameter to `CreateDeploymentAsync`, `CreateDeploymentAsync`, and `DeleteDeploymentAsync`.
  * Moved the LambdaSharp namespaces to specialized assemblies:
    * _LambdaSharp.ApiGateway_
    * _LambdaSharp.CloudWatch_
    * _LambdaSharp.CustomResource_
    * _LambdaSharp.Finalizer_
    * _LambdaSharp.Schedule_
    * _LambdaSharp.SimpleNotificationService_
    * _LambdaSharp.SimpleQueueService_

##### NuGet Package Migration Guide

|Base Class                                             |NuGet Package                         |
|-------------------------------------------------------|--------------------------------------|
|ALambdaApiGatewayFunction<TRequest,TResponse>          |LambdaSharp.ApiGateway
|ALambdaCustomResourceFunction<TProperties,TAttributes> |LambdaSharp.CustomResource
|ALambdaEventFunction<TMessage>                         |LambdaSharp.CloudWatch
|ALambdaFinalizerFunction                               |LambdaSharp.Finalizer
|ALambdaFunction                                        |LambdaSharp
|ALambdaFunction<TRequest,TResponse>                    |LambdaSharp
|ALambdaQueueFunction<TMessage>                         |LambdaSharp.SimpleQueueService
|ALambdaScheduleFunction                                |LambdaSharp.Schedule
|ALambdaTopicFunction<TMessage>                         |LambdaSharp.SimpleNotificationService

#### Features

* CLI
  * Added support for self-contained .NET 5 Lambda functions.
  * Update Blazor WebAssembly app template to target .NET 5.
  * Removed dependency on _Amazon.Lambda.Tools_. _Amazon.Lambda.Tools_ is no longer requires to build, publish, or deploy LambdaSharp modules.
  * Updated CloudFormation to v22.0.0.
  * Allowed resource types to have an empty or omitted `Attributes` section.

* SDK
  * Added new `ALambdaEventFunction<TMessage>` base class for handling CloudWatch events.
  * Optimized cold-start times for by deserializing Amazon Lambda data-structures by using `LambdaSystemTextJsonSerializer`, which is built on _System.Text.Json_. This change avoids jitting the heavy _Newtonsoft.Json_ assembly unless required by the end-user code.
  * Added constructor to customize serializer settings for `LambdaSystemTextJsonSerializer`.
  * Added constructor to customize serializer settings for `LambdaNewtonsoftJsonSerializer`.
  * Added null-aware annotations to _LambdaSharp_ assembly.
  * Added [MD5 algorithm implementation from Mono](https://github.com/mono/mono/blob/master/mcs/class/corlib/System.Security.Cryptography/MD5CryptoServiceProvider.cs) in _LambdaSharp.Logging_ since it is not supported in .NET 5 WebAssembly currently.
  * Added logic in `ALambdaCustomResourceFunction` and `ALambdaFinalizerFunction` to trigger the `System.Threading.CancellationToken` 500ms before the function times out.
  * Added `TerminateLambdaInstance()` method, which forces the Lambda instance to terminate and perform a cold start on next invocation. This method should only be used when the processing environment has become corrupted beyond repair.

* Syntax
  * Added `Stack` as the declaration keyword for nested stacks. Previously the keyword was `Nested`, which remains supported for backwards compatibility.

* Samples
  * Added `Samples/JsonSerializerSample` module showing how to specify and customize the JSON serializer for a Lambda function.
  * Added `Samples/LambdaSelfContainedSample` module showing how to build a Lambda function using .NET 5.
