using System;

namespace InterfaceInterceptionProxy
{
    /// <summary>
    /// Provide information about parameter passed to intercepted method
    /// </summary>
    public class ParamInfo
    {
        /// <summary>
        /// ParamInfo constructor
        /// </summary>
        /// <param name="name">name of argument</param>
        /// <param name="type">type of argument</param>
        /// <param name="isByRef">is argument passed by reference</param>
        /// <param name="isOut">is argument passed as out</param>
        public ParamInfo(string name, Type type, bool isByRef, bool isOut)
        {
            Name = name;
            Type = type;
            IsByRef = isByRef;
            IsOut = isOut;
        }

        /// <summary>
        /// Gets a value indicating whether the Parameter is passed by reference.
        /// </summary>
        /// <returns>true if the Parameter is passed by reference; otherwise, false.</returns>
        public bool IsByRef { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the Parameter is out parameter.
        /// </summary>
        /// <returns>true if the Parameter is out parameter; otherwise, false.</returns>
        public bool IsOut { get; private set; }

        /// <summary>
        /// Gets a name of the Parameter.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a System.Type of the Parameter
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets or sets value of the parameter passed to intercepted method
        /// </summary>
        public object Value { get; set; }
    }
}