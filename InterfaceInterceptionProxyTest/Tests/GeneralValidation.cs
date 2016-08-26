using System;
using InterfaceInterceptionProxy;
using NUnit.Framework;

namespace InterfaceInterceptionProxyTest
{
    public class GeneralValidation
    {
        [Test]
        public void InterfaceBuilderStrategy_CreateInterfaceProxy_Throws_WhenNotInterface()
        {
            Assert.Throws<ArgumentException>(delegate { InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(void), typeof(void)); });
        }

        [Test]
        public void InterfaceBuilderStrategy_CreateInterfaceProxy_Throws_WhenTypeIsNotDerivedFromInterface()
        {
            Assert.Throws<ArgumentException>(delegate { InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(IGenericTest<int>), typeof(TestClass)); });
        }
    }
}