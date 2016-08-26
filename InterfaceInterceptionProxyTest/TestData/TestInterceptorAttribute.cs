using System;
using InterfaceInterceptionProxy;

namespace InterfaceInterceptionProxyTest
{
    public interface INewInterceptionHandler : IInterceptionHandler
    {
    }

    public class TestInterceptorAttribute : InterceptorAttribute
    {
        public override Type InterceptionHandlerType
        {
            get
            {
                return typeof(IInterceptionHandler);
            }
        }
    }

    public class TestNewInterceptorAttribute : InterceptorAttribute
    {
        public TestNewInterceptorAttribute() : base(1)
        {
        }

        public override Type InterceptionHandlerType
        {
            get
            {
                return typeof(INewInterceptionHandler);
            }
        }
    }

    public class TestSecondInterceptorAttribute : InterceptorAttribute
    {
        public TestSecondInterceptorAttribute() : base(1)
        {
        }

        public override Type InterceptionHandlerType
        {
            get
            {
                return typeof(IInterceptionHandler);
            }
        }
    }
}