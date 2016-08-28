using System.Collections.Generic;

namespace InterfaceInterceptionProxy
{
    /// <summary>
    /// Delegate used in case of intercepting method with return value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="paramsInfo"><see cref="ParamInfo"/>Set of parameters passed to intercepted method</param>
    /// <returns>Value provided by intercepted action</returns>
    public delegate T TDelegate<T>(IEnumerable<ParamInfo> paramsInfo);

    /// <summary>
    /// Delegate used in case of intercepting void method
    /// </summary>
    /// <param name="paramsInfo"><see cref="ParamInfo"/>Set of parameters passed to intercepted method</param>
    public delegate void VoidDelegate(IEnumerable<ParamInfo> paramsInfo);

    /// <summary>
    /// Interface that should be implemented by Interception Handler
    /// </summary>
    public interface IInterceptionHandler
    {
        /// <summary>
        /// Interception action for methods that has return value
        /// </summary>
        /// <typeparam name="T">Type of target method Return Value</typeparam>
        /// <param name="interceptedAction">Wrapper around target method</param>
        /// <param name="paramsInfo">Target method parameters information</param>
        /// <returns>Value that will be returned to caller</returns>
        T InterceptingAction<T>(TDelegate<T> interceptedAction, IEnumerable<ParamInfo> paramsInfo);

        /// <summary>
        /// Interception action for void methods
        /// </summary>
        /// <param name="interceptedAction">Wrapper around target method</param>
        /// <param name="paramsInfo">Target method parameters information</param>
        void InterceptingAction(VoidDelegate interceptedAction, IEnumerable<ParamInfo> paramsInfo);
    }
}