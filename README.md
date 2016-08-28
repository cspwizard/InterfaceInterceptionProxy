#InterfaceInterceptionProxy

InterfaceInterceptionProxy is a library that contains helpers to generate interface interception proxy in runtime. 

Using GrEmit project by Igor Chevdar as ILGenerator wrapper, direct usage will be replaced by NuGet package reference as soon as bugs in original code will be fixed :)

##Usage



###Example

```
using System;
using System.Collections.Generic;
using InterfaceInterceptionProxy;

namespace ConsoleApplication
{
    public interface ITest
    {
        int Sum(int a, int b);

        void VoidExample(int a, int b);
    }

    public interface ITestInterceptionHandler : IInterceptionHandler
    {
    }

    public class TestInterceptionHandler : ITestInterceptionHandler
    {
        public void InterceptingAction(VoidDelegate interceptedAction, IEnumerable<ParamInfo> paramsInfo)
        {
            // do something before intercepting action execution
            interceptedAction(paramsInfo);

            // do something after intercepting action execution
        }

        public T InterceptingAction<T>(TDelegate<T> interceptedAction, IEnumerable<ParamInfo> paramsInfo)
        {
            // do something before intercepting action execution
            var returnValue = interceptedAction(paramsInfo);

            // do something after intercepting action execution
            return returnValue;
        }
    }

    public class TestInterceptorAttribute : InterceptorAttribute
    {
        public TestInterceptorAttribute() : base(0)
        {
        }

        public override Type InterceptionHandlerType
        {
            get
            {
                return typeof(ITestInterceptionHandler);
            }
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var type = InterfaceProxyBuilder.BuildProxyType<ITest, TestClass>();

            // normally this is done by Dependency Injection container
            var obj = (ITest)Activator.CreateInstance(type, new object[] { new TestClass(), new TestInterceptionHandler() });

            var res = obj.Sum(10, 10);
            obj.VoidExample(10, 10);
        }
    }

    internal class TestClass : ITest
    {
        [TestInterceptor]
        public int Sum(int a, int b)
        {
            return a + b;
        }

        [TestInterceptor]
        public void VoidExample(int a, int b)
        {
        }
    }
}
```
