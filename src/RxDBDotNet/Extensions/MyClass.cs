// namespace RxDBDotNet.Extensions;
//
// public class MyClass
// {
//     public static string MyMethod(string originalName)
//     {
//         return $"Hello, {originalName}!";
//     }
// }
//
// public class MyClassType : ObjectType<MyClass>
// {
//     protected override void Configure(IObjectTypeDescriptor<MyClass> descriptor)
//     {
//         descriptor.Field("myMethod")
//             .Argument("newName", arg => arg.Type<NonNullType<StringType>>())
//             .Resolve(ctx =>
//             {
//                 var myClass = ctx.Parent<MyClass>();
//                 var originalName = ctx.ArgumentValue<string>("newName");
//                 return MyClass.MyMethod(originalName);
//             });
//     }
// }
