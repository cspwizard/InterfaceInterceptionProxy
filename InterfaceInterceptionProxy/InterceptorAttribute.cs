using System;

namespace InterfaceInterceptionProxy
{
    /// <summary>
    /// Base class of attribute used to mark methods that should be intercepted
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class InterceptorAttribute : Attribute
    {
        /// <summary>
        /// Interceptor attribute describes the interceptor that will be injected and order in
        /// invocation chain. Smaller order - closer to target method in invocation chain.
        /// </summary>
        /// <param name="order">order in invocation chain</param>
        protected InterceptorAttribute(int order)
        {
            Order = order;
        }

        /// <summary>
        /// <see cref="IInterceptionHandler"/> derived type, used as interception handler
        /// </summary>
        public abstract Type InterceptionHandlerType { get; }

        /// <summary>
        /// Place in the stack prior target method call
        /// </summary>
        public int Order { get; private set; }
    }
}