using System.Collections.Generic;

namespace InterfaceInterceptionProxy
{
    public delegate T TDelegate<T>(IEnumerable<ParamInfo> paramsInfo);

    public delegate void VoidDelegate(IEnumerable<ParamInfo> paramsInfo);

    public interface IInterceptionHandler
    {
        /// <summary>
        /// Generic interception action
        /// </summary>
        /// <typeparam name="T">Type of target method Return Value</typeparam>
        /// <param name="delegate">Wrapper around target method</param>
        /// <param name="paramsInfo">Target method parameters information</param>
        /// <returns>Value that will be returned to caller</returns>
        T InterceptingAction<T>(TDelegate<T> @delegate, IEnumerable<ParamInfo> paramsInfo);

        /// <summary>
        /// Void interception action
        /// </summary>
        /// <param name="delegate">Wrapper around target method</param>
        /// <param name="paramsInfo">Target method parameters information</param>
        void InterceptingAction(VoidDelegate @delegate, IEnumerable<ParamInfo> paramsInfo);
    }
}