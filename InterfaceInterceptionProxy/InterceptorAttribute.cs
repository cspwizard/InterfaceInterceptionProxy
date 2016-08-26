using System;

namespace InterfaceInterceptionProxy
{
    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class InterceptorAttribute : Attribute
    {
        /// <summary>
        /// Interceptor attribute describes the interceptor that will be injected and order in
        /// invocation chain. Smaller order - closer to target method in invocation chain.
        /// </summary>
        /// <param name="order">order in invocation chain</param>
        protected InterceptorAttribute(int order = 0)
        {
            Order = order;
        }

        /// <summary>
        /// <see cref="IInterceptionHandler"/> derived type
        /// </summary>
        public abstract Type InterceptionHandlerType { get; }

        /// <summary>
        /// Place in the stack prior target method call
        /// </summary>
        public int Order { get; private set; }
    }
}