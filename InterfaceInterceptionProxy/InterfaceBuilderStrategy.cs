using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GrEmit;
using static GrEmit.GroboIL;

namespace InterfaceInterceptionProxy
{
    /// <summary>
    /// Interface interceptor proxy type builder
    /// </summary>
    public static class InterfaceBuilderStrategy
    {
        static InterfaceBuilderStrategy()
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("InterfaceInterceptionBuilderDynamicTypes"),
                    AssemblyBuilderAccess.RunAndCollect,
                    Enumerable.Empty<CustomAttributeBuilder>(),
                    System.Security.SecurityContextSource.CurrentAppDomain);
            ModuleBuilder = assemblyBuilder.DefineDynamicModule("InterfaceInterceptionBuilderModule", "InterfaceInterceptionBuilder.dll", true);
        }

        private static ModuleBuilder ModuleBuilder { get; set; }

        /// <summary>
        /// Builds Proxy Type that implement an interface, and proxy calls to defined with attribute
        /// methods via defined intercepting handlers
        /// </summary>
        /// <param name="interface">Type to wrap</param>
        /// <param name="implementation">
        /// Type providing interface implementation, intercepting attributes are read from this type
        /// </param>
        /// <returns>
        /// Proxy type with constructor accepting object implementing @interface and intercepting
        /// handlers as parameters
        /// </returns>
        public static Type CreateInterfaceProxy(Type @interface, Type implementation)
        {
            Contract.Ensures(Contract.Result<Type>() != null);
            if (!@interface.IsInterface)
            {
                throw new ArgumentException($"{nameof(@interface)} ({@interface.FullName}) is not an interface.");
            }

            if (!@interface.IsAssignableFrom(implementation))
            {
                throw new ArgumentException($"{nameof(@interface)} ({@interface.FullName}) is not assignable from {nameof(implementation)} ({implementation.FullName})");
            }
            var typeName = $"{@interface.Assembly.GetName().Name}.v{@interface.Assembly.GetName().Version}.{@interface.FullName}";
            var typeBuilder = ModuleBuilder.DefineType($"InterceptorProxy_{typeName}_{Guid.NewGuid().ToString("N")}", TypeAttributes.Class | TypeAttributes.Public);
            typeBuilder.AddInterfaceImplementation(SetupGenericClassArguments(@interface, typeBuilder));

            var methods = new List<MethodPair>();
            AddMethodPairsToList(methods, @interface, implementation);

            var methodInterceptors = new Dictionary<MethodInfo, Type[]>();
            var interceptorFields = new Dictionary<Type, FieldInfo>();

            var methodsCount = methods.Count;
            var attributesHashSet = new HashSet<Type>();

            for (var i = 0; i < methodsCount; i++)
            {
                var method = methods[i];
                var interfaceMethod = method.InterfaceMethod;
                var typeMethod = method.TargetMethod;

                var attributeTypes = Attribute.GetCustomAttributes(typeMethod, typeof(InterceptorAttribute), true)
                            .OrderBy(a => ((InterceptorAttribute)a).Order).Select(a => ((InterceptorAttribute)a).InterceptionHandlerType).ToArray();

                for (int j = 0; j < attributeTypes.Length; j++)
                {
                    if (!typeof(IInterceptionHandler).IsAssignableFrom(attributeTypes[j]))
                    {
                        throw new InvalidOperationException($"Interception handler type should implement {nameof(IInterceptionHandler)} interface");
                    }
                }

                attributesHashSet.UnionWith(attributeTypes);
                methodInterceptors.Add(interfaceMethod, attributeTypes);
            }

            var baseConstructorInfo = typeof(object).GetConstructor(Type.EmptyTypes);

            var intercpetionHandlerType = typeof(IInterceptionHandler);

            var genericInterceptionAction = intercpetionHandlerType.GetMethods().Single(i => i.Name == "InterceptingAction" && i.ReturnType != typeof(void));
            var voidInterceptionAction = intercpetionHandlerType.GetMethods().Single(i => i.Name == "InterceptingAction" && i.ReturnType == typeof(void));

            var att = attributesHashSet.ToList();
            att.Insert(0, implementation);
            var paramArray = att.ToArray();

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, paramArray);
            var concrete = typeBuilder.DefineField("Concrete", @interface, FieldAttributes.Public);

            for (int i = 1; i < paramArray.Length; i++)
            {
                var at = paramArray[i];
                interceptorFields.Add(at, typeBuilder.DefineField(at.Name, intercpetionHandlerType, FieldAttributes.Public));
            }

            GroboIL il;
            using (il = new GroboIL(constructorBuilder))
            {
                il.Ldarg(0);
                il.Call(baseConstructorInfo);
                il.Nop();

                // set concrete
                il.Ldarg(0);
                il.Ldarg(1);
                il.Stfld(concrete);

                // set injected interceptionHandlers
                for (var i = 1; i < paramArray.Length; i++)
                {
                    il.Ldarg(0);
                    il.Ldarg(i + 1);
                    il.Stfld(interceptorFields[paramArray[i]]);
                }

                il.Ret();

                LogIlCode(il);
            }

            for (var i = 0; i < methodsCount; i++)
            {
                var interfaceMethod = methods[i].InterfaceMethod;

                if (interfaceMethod.IsGenericMethod)
                {
                    interfaceMethod = interfaceMethod.GetGenericMethodDefinition();
                }

                DefineMethodOverride(typeBuilder, interfaceMethod, methodInterceptors, interceptorFields, genericInterceptionAction, voidInterceptionAction, concrete);
            }

            return typeBuilder.CreateType();
        }

        private static void AddMethodPairsToList(List<MethodPair> methodPairs, Type @interface, Type target)
        {
            var implementationMapping = target.GetInterfaceMap(@interface);

            var array = new MethodPair[implementationMapping.InterfaceMethods.Length];
            for (int i = 0; i < implementationMapping.InterfaceMethods.Length; i++)
            {
                array[i] = new MethodPair(implementationMapping.InterfaceMethods[i], implementationMapping.TargetMethods[i]);
            }

            methodPairs.AddRange(array);

            foreach (Type subInterface in @interface.GetInterfaces())
            {
                AddMethodPairsToList(methodPairs, subInterface, target);
            }
        }

        private static MethodBuilder DefineMethodInterceptingDelegate(TypeBuilder typeBuilder, MethodInfo overridedMethod, Dictionary<Type, FieldInfo> interceptorFields, MethodInfo genericInterceptionAction, MethodInfo voidInterceptionAction, Type[] genericParameterTypes, MethodBuilder @delegate, int index, Type interceptor)
        {
            ConstructorInfo delegateConstructor;
            MethodInfo interceptorMethod;

            // Define the method
            var method = typeBuilder.DefineMethod($"{overridedMethod.Name}-{interceptor.Name}_{index}_Interceptor",
                                         MethodAttributes.Private | MethodAttributes.HideBySig,
                                         overridedMethod.ReturnType,
                                         new[] { typeof(ParamInfo[]) });

            SetupGenericMethodArguments(overridedMethod, method);

            using (var il = new GroboIL(method))
            {
                if (overridedMethod.ReturnType != typeof(void))
                {
                    interceptorMethod = genericInterceptionAction.MakeGenericMethod(overridedMethod.ReturnType);
                    delegateConstructor = typeof(TDelegate<>).MakeGenericType(overridedMethod.ReturnType).GetConstructor(new[] { typeof(object), typeof(IntPtr) });
                }
                else
                {
                    interceptorMethod = voidInterceptionAction;
                    delegateConstructor = typeof(VoidDelegate).GetConstructor(new[] { typeof(object), typeof(IntPtr) });
                }

                il.Ldarg(0);
                il.Ldfld(interceptorFields[interceptor]);
                il.Ldarg(0);

                if (overridedMethod.IsGenericMethodDefinition)
                {
                    il.Ldftn(@delegate.MakeGenericMethod(genericParameterTypes));
                }
                else
                {
                    il.Ldftn(@delegate);
                }

                il.Newobj(delegateConstructor);
                il.Ldarg(1);
                il.Call(interceptorMethod);
                il.Ret();

                @delegate = method;
            }

            return @delegate;
        }

        private static void DefineMethodOverride(TypeBuilder typeBuilder, MethodInfo overridedMethod, Dictionary<MethodInfo, Type[]> methodInterceptors, Dictionary<Type, FieldInfo> interceptorFields, MethodInfo genericInterceptionAction, MethodInfo voidInterceptionAction, FieldBuilder concreteInstance)
        {
            Type returnType = overridedMethod.ReturnType;

            var argumentTypes = new List<Type>();
            var methodParams = overridedMethod.GetParameters().OrderBy(p => p.Position).ToArray();
            foreach (ParameterInfo parameterInfo in methodParams)
            {
                argumentTypes.Add(parameterInfo.ParameterType);
            }

            var methodBuilder = typeBuilder.DefineMethod(overridedMethod.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.NewSlot,
                    returnType,
                    argumentTypes.ToArray());

            var genericParameterTypes = SetupGenericMethodArguments(overridedMethod, methodBuilder);

            Type[] interceptors;

            if (methodInterceptors.TryGetValue(overridedMethod, out interceptors))
            {
                DefineMethodOverrideWithInterception(typeBuilder, methodBuilder, overridedMethod, interceptorFields, genericInterceptionAction, voidInterceptionAction, concreteInstance, methodParams, genericParameterTypes, interceptors);
            }
            else
            {
                DefineMethodOverrideNoInterception(methodBuilder, overridedMethod, concreteInstance, methodParams);
            }

            typeBuilder.DefineMethodOverride(methodBuilder, overridedMethod);
        }

        private static void DefineMethodOverrideNoInterception(MethodBuilder methodBuilder, MethodInfo overridedMethod, FieldBuilder concreteInstance, ParameterInfo[] methodParams)
        {
            using (var il = new GroboIL(methodBuilder))
            {
                il.Ldarg(0);
                il.Ldfld(concreteInstance);
                foreach (var parameter in methodParams)
                {
                    il.Ldarg(parameter.Position + 1);
                }

                il.Call(overridedMethod);
                il.Ret();

                LogIlCode(il);
            }
        }

        private static void DefineMethodOverrideWithInterception(TypeBuilder typeBuilder, MethodBuilder methodBuilder, MethodInfo overridedMethod, Dictionary<Type, FieldInfo> interceptorFields, MethodInfo genericInterceptionAction, MethodInfo voidInterceptionAction, FieldBuilder concreteInstance, ParameterInfo[] methodParams, Type[] genericParameterTypes, Type[] interceptors)
        {
            var @delegate = GenerateOverloadedMethodDelegate(overridedMethod, typeBuilder, concreteInstance);

            for (var i = 0; i < interceptors.Length; i++)
            {
                var interceptor = interceptors[i];

                @delegate = DefineMethodInterceptingDelegate(typeBuilder, overridedMethod, interceptorFields, genericInterceptionAction, voidInterceptionAction, genericParameterTypes, @delegate, i, interceptor);
            }

            using (var il = new GroboIL(methodBuilder))
            {
                var paramInfoType = typeof(ParamInfo);
                var paramsInfoType = typeof(ParamInfo[]);
                var paramsInfo = il.DeclareLocal(paramsInfoType);
                var paramInfo = il.DeclareLocal(paramInfoType);

                il.Nop();

                il.Ldc_I4(methodParams.Length);
                il.Newarr(typeof(ParamInfo));
                il.Stloc(paramsInfo);

                var paramInfoConstructor = paramInfoType.GetConstructor(new[] { typeof(string), typeof(Type), typeof(bool), typeof(bool) });
                var paramInfoValueSetter = paramInfoType.GetProperty("Value").GetSetMethod();
                var getTypeMethod = typeof(Type).GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) });

                var idx = 0;
                foreach (var parameter in methodParams)
                {
                    // load array at index
                    il.Ldloc(paramsInfo);
                    il.Ldc_I4(idx++);

                    // Load ParamInfo.Name
                    il.Ldstr(parameter.Name);

                    // Load ParamInfo.Type
                    if (parameter.IsOut || parameter.ParameterType.IsByRef)
                    {
                        il.Ldtoken(parameter.ParameterType.GetElementType());
                    }
                    else
                    {
                        il.Ldtoken(parameter.ParameterType);
                    }

                    il.Call(getTypeMethod);

                    // Load ParamInfo.IsByRef
                    if (parameter.ParameterType.IsByRef)
                    {
                        il.Ldc_I4(1);
                    }
                    else
                    {
                        il.Ldc_I4(0);
                    }

                    // Load ParamInfo.IsOut
                    if (parameter.IsOut)
                    {
                        il.Ldc_I4(1);
                    }
                    else
                    {
                        il.Ldc_I4(0);
                    }

                    // instantiate ParamInfo
                    il.Newobj(paramInfoConstructor);
                    il.Stloc(paramInfo);

                    // Set ParamInfo.Value
                    il.Ldloc(paramInfo);
                    il.Ldarg(parameter.Position + 1);
                    if (parameter.IsOut || parameter.ParameterType.IsByRef)
                    {
                        il.Ldobj(parameter.ParameterType.GetElementType());

                        if (parameter.ParameterType.GetElementType().IsValueType || parameter.ParameterType.GetElementType().IsGenericParameter)
                        {
                            il.Box(parameter.ParameterType.GetElementType());
                        }
                    }
                    else
                    {
                        if (parameter.ParameterType.IsValueType || parameter.ParameterType.IsGenericParameter)
                        {
                            il.Box(parameter.ParameterType);
                        }
                    }

                    il.Call(paramInfoValueSetter);
                    il.Nop();

                    // push to array
                    il.Ldloc(paramInfo);
                    il.Stelem(paramInfoType);
                }

                il.Ldarg(0);
                il.Ldloc(paramsInfo);
                if (overridedMethod.IsGenericMethodDefinition)
                {
                    il.Call(@delegate.MakeGenericMethod(genericParameterTypes));
                }
                else
                {
                    il.Call(@delegate);
                }

                idx = 1;
                var paramInfoValueGetter = paramInfoType.GetProperty("Value").GetGetMethod();
                foreach (var parameter in methodParams)
                {
                    if (parameter.IsOut || parameter.ParameterType.IsByRef)
                    {
                        il.Ldarg(idx);
                        il.Ldloc(paramsInfo);
                        il.Ldc_I4(idx - 1);
                        il.Ldelem(paramInfoType);
                        il.Call(paramInfoValueGetter);

                        if (parameter.ParameterType.GetElementType().IsValueType)
                        {
                            il.Unbox_Any(parameter.ParameterType.GetElementType());
                        }

                        il.Stobj(parameter.ParameterType.GetElementType());
                    }

                    idx++;
                }

                il.Ret();

                LogIlCode(il);
            }
        }

        private static MethodBuilder GenerateOverloadedMethodDelegate(MethodInfo methodToIntercept,
                                                                        TypeBuilder typeBuilder,
                                                                FieldInfo concrete)
        {
            // Define the method
            var method = typeBuilder.DefineMethod(methodToIntercept.Name + "-Delegate",
                                         MethodAttributes.Private | MethodAttributes.HideBySig,
                                         methodToIntercept.ReturnType,
                                         new[] { typeof(ParamInfo[]) });

            SetupGenericMethodArguments(methodToIntercept, method);

            // Local for each out/ref parameter
            var parameters = methodToIntercept.GetParameters();

            var locals = new Dictionary<string, Local>();

            using (var il = new GroboIL(method))
            {
                foreach (ParameterInfo parameter in parameters)
                {
                    if (parameter.IsOut || parameter.ParameterType.IsByRef)
                    {
                        locals.Add(parameter.Name, il.DeclareLocal(parameter.ParameterType.GetElementType(), parameter.Name));
                    }
                }

                var paramInfoType = typeof(ParamInfo);
                var paramInfoGetValue = paramInfoType.GetProperty("Value").GetGetMethod();

                // Initialize out parameters to default values
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.IsByRef && !parameters[i].IsOut)
                    {
                        il.Ldarg(1);
                        il.Ldc_I4(i);
                        il.Ldelem(paramInfoType);
                        il.Call(paramInfoGetValue);

                        if (parameters[i].ParameterType.GetElementType().IsValueType)
                        {
                            il.Unbox_Any(parameters[i].ParameterType.GetElementType());
                        }
                        else
                        {
                            il.Castclass(parameters[i].ParameterType.GetElementType());
                        }

                        il.Stloc(locals[parameters[i].Name]);
                    }
                }

                // Load target
                il.Ldarg(0);
                il.Ldfld(concrete);

                // Push call values onto stack
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].IsOut || parameters[i].ParameterType.IsByRef)
                    {
                        il.Ldloca(locals[parameters[i].Name]);
                    }
                    else
                    {
                        il.Ldarg(1);
                        il.Ldc_I4(i);
                        il.Ldelem(paramInfoType);
                        il.Call(paramInfoGetValue);

                        if (parameters[i].ParameterType.IsValueType || parameters[i].ParameterType.IsGenericParameter)
                        {
                            il.Unbox_Any(parameters[i].ParameterType);
                        }
                        else
                        {
                            il.Castclass(parameters[i].ParameterType);
                        }
                    }
                }

                // Call intercepted method
                il.Call(methodToIntercept);

                var paramInfoSetValue = paramInfoType.GetProperty("Value").GetSetMethod();

                // Copy out/ref parameter values back into passed-in parameters array
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].IsOut || parameters[i].ParameterType.IsByRef)
                    {
                        il.Ldarg(1);
                        il.Ldc_I4(i);
                        il.Ldelem(paramInfoType);
                        il.Ldloc(locals[parameters[i].Name]);

                        if (parameters[i].ParameterType.GetElementType().IsValueType)
                        {
                            il.Box(parameters[i].ParameterType.GetElementType());
                        }

                        il.Call(paramInfoSetValue);
                    }
                }

                il.Ret();

                LogIlCode(il);
            }

            return method;
        }

        [Conditional("TEST")]
        private static void LogIlCode(GroboIL il)
        {
            File.AppendAllText("DebugIlOutput.il", il.GetILCode());
        }

        private static GenericTypeParameterBuilder[] SetupGenericArguments(Type[] genericParameterTypes, Func<string[], GenericTypeParameterBuilder[]> func)
        {
            if (genericParameterTypes.Length == 0)
            {
                return null;
            }

            // Extract parameter names
            string[] genericParameterNames = new string[genericParameterTypes.Length];
            for (var i = 0; i < genericParameterTypes.Length; i++)
            {
                genericParameterNames[i] = genericParameterTypes[i].Name;
            }

            // Setup constraints on generic types
            var genericBuilders = func.Invoke(genericParameterNames);

            for (var i = 0; i < genericBuilders.Length; i++)
            {
                if (genericParameterTypes[i].IsGenericParameter)
                {
                    genericBuilders[i].SetGenericParameterAttributes(genericParameterTypes[i].GenericParameterAttributes);

                    var constraints = genericParameterTypes[i].GetGenericParameterConstraints();
                    for (var j = 0; j < constraints.Length; j++)
                    {
                        genericBuilders[i].SetBaseTypeConstraint(constraints[j]);
                    }
                }
            }

            return genericBuilders;
        }

        private static Type SetupGenericClassArguments(Type classToWrap, TypeBuilder typeBuilder)
        {
            if (classToWrap.IsGenericTypeDefinition)
            {
                var builders = SetupGenericArguments(classToWrap.GetGenericArguments(), names => typeBuilder.DefineGenericParameters(names));

                if (builders != null)
                {
                    return classToWrap.MakeGenericType(builders);
                }
            }

            return classToWrap;
        }

        private static Type[] SetupGenericMethodArguments(MethodBase methodToIntercept, MethodBuilder methodBuilder)
        {
            var arguments = methodToIntercept.GetGenericArguments();

            SetupGenericArguments(arguments, names => methodBuilder.DefineGenericParameters(names));

            return arguments;
        }

        private class MethodPair
        {
            public readonly MethodInfo InterfaceMethod;

            public readonly MethodInfo TargetMethod;

            public MethodPair(MethodInfo interfaceMethod, MethodInfo typeMethod)
            {
                InterfaceMethod = interfaceMethod;
                TargetMethod = typeMethod;
            }
        }
    }
}