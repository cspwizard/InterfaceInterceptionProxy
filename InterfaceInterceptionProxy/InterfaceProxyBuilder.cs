using System;
using System.Collections.Generic;

namespace InterfaceInterceptionProxy
{
    /// <summary>
    /// Wrapper around <see cref="InterfaceBuilderStrategy"/> providing caching for built types
    /// </summary>
    public static class InterfaceProxyBuilder
    {
        private static readonly object LockObject = new object();
        private static readonly Dictionary<Tuple<Type, Type>, Type> Types = new Dictionary<Tuple<Type, Type>, Type>();

        /// <summary>
        /// Builds Proxy Type that implement an interface, and proxy calls to defined with attribute methods via
        /// defined intercepting handlers
        /// </summary>
        /// <typeparam name="I">Type to wrap</typeparam>
        /// <typeparam name="T">
        /// Type providing interface implementation, intercepting attributes are read from this type
        /// </typeparam>
        /// <returns>
        /// Proxy type with constructor accepting object implementing @interface and intercepting
        /// handlers as parameters
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Not applicable in this case")]
        public static Type BuildType<I, T>() where T : I where I : class
        {
            Type type = typeof(T);
            Type @interface = typeof(T);
            return BuildType(@interface, type);
        }

        /// <summary>
        /// Builds Proxy Type that implement an interface, and proxy calls to defined with attribute methods via
        /// defined intercepting handlers
        /// </summary>
        /// <param name="interface">Type to wrap</param>
        /// <param name="implementation">
        /// Type providing interface implementation, intercepting attributes are read from this type
        /// </param>
        /// <returns>
        /// Proxy type with constructor accepting object implementing @interface and intercepting
        /// handlers as parameters
        /// </returns>
        public static Type BuildType(Type @interface, Type implementation)
        {
            if (!@interface.IsInterface)
            {
                throw new ArgumentException($"{nameof(@interface)} ({@interface.FullName}) is not an interface.");
            }

            if (!@interface.IsAssignableFrom(implementation))
            {
                throw new ArgumentException($"{nameof(@interface)} ({@interface.FullName}) is not assignable from {nameof(implementation)} ({implementation.FullName})");
            }

            var tuple = new Tuple<Type, Type>(@interface, implementation);
            if (!Types.ContainsKey(tuple))
            {
                lock (LockObject)
                {
                    if (!Types.ContainsKey(tuple))
                    {
                        Types.Add(tuple, InterfaceBuilderStrategy.CreateInterfaceProxy(@interface, implementation));
                    }
                }
            }

            return Types[tuple];
        }
    }
}