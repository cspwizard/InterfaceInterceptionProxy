using System;

namespace InterfaceInterceptionProxy
{
    public class ParamInfo
    {
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

        public object Value { get; set; }
    }
}